import { defineStore } from 'pinia';
import { getAPI } from '/@/utils/axios-utils';
import { TmsVehicleFilterHistoryApi, type VehicleFilterHistoryTripOutput } from '/@/api-services/tms';
import type { VehicleAlertData, VehicleAlertPayload } from '/@/transporter/hooks/useTransporterVehicleFilter';

/**
 * Nguồn của một thẻ hoạt động. Thêm nguồn mới (vd. sự kiện thiết bị từ FMS) = thêm giá trị ở đây,
 * một hàm push chuyển payload của nguồn đó về {@link ActivityFeedItem}, và một mục trong danh sách nguồn của
 * activityFeed.vue (icon + nhãn). Mọi nguồn hiện chung một danh sách theo thời gian.
 */
export type ActivityFeedSource = 'vehicleFilter' | 'device';

/** Thông tin camera đi kèm bản tin (tên, lý trình) — giao diện dùng trước khi tra danh mục thiết bị. */
export interface ActivityFeedCamera {
	name?: string | null;
	km?: number | null;
	met?: number | null;
}

/**
 * Một THẺ hoạt động. Lọc xe: một CHUYẾN của một biển (các lượt đi tiến theo tuyến trong khoảng nghỉ, cùng
 * <c>tripId</c> do worker gán) — lộ trình A → B → C thay cho vị trí. Giao diện không đọc payload gốc của từng nguồn.
 */
export interface ActivityFeedItem {
	/** `${source}:${tripId}` — lượt mới cùng chuyến gộp vào thẻ đang có. */
	key: string;
	source: ActivityFeedSource;
	/** Lượt đầu chuyến. */
	startTime: Date;
	/** Lượt gần nhất — sắp xếp mới nhất trước theo mốc này. */
	time: Date;
	/** Lọc xe: biển số. Thiết bị: tên thiết bị. */
	title: string;
	/** Lọc xe: mã loại danh sách (cấu hình TmsVehicleFilterType). */
	typeCode?: string | null;
	/** Mã vị trí trên tuyến (cấu hình TmsZoneLocation). */
	locationId?: string | null;
	/** Mã hướng (cấu hình TmsDirectionType). */
	direction?: string | null;
	/** Lộ trình: id camera theo thời gian, đã gộp lượt liền nhau cùng camera. */
	route: string[];
	/** Số lượt khớp trong chuyến. */
	hitCount: number;
	/** Chuyến có lượt bật thông báo (cảnh báo bật — lượt mở chuyến). */
	notify?: boolean;
	/** Có lượt đến qua bản tin trực tiếp — dùng để làm nổi thẻ mới. */
	isLive: boolean;
	/** Id lượt đã gộp (bỏ trùng bản tin lặp). Chỉ lượt trực tiếp; lượt nạp từ lịch sử tính bằng hitCount. */
	hitKeys: string[];
}

/** Số thẻ giữ lại cho mỗi nguồn; thẻ cũ hơn bị bỏ, xem lại đầy đủ ở trang lịch sử. */
const MAX_ITEMS_PER_SOURCE = 100;
/** Cửa sổ nạp lịch sử lọc xe ban đầu (ngày). API cho tối đa 60 ngày. */
export const VEHICLE_FILTER_HISTORY_DAYS = 3;
const INITIAL_PAGE_SIZE = 30;

let vehicleFilterLoad: Promise<void> | null = null;

const text = (value: unknown): string | null => {
	if (value === undefined || value === null) return null;
	const normalized = String(value).trim();
	return normalized || null;
};

const toNumber = (value: unknown): number | null => {
	if (value === undefined || value === null || value === '') return null;
	const parsed = Number(value);
	return Number.isFinite(parsed) ? parsed : null;
};

const toDate = (value: unknown): Date | null => {
	if (value === undefined || value === null || value === '') return null;
	const parsed = new Date(value as string | number | Date);
	return Number.isNaN(parsed.getTime()) ? null : parsed;
};

/** Nối camera vào lộ trình nếu khác camera cuối (camera bắn lại cùng lượt không làm lộ trình dài ra). */
const appendRoute = (route: string[], equipmentId: string | null | undefined): string[] =>
	!equipmentId || route[route.length - 1] === equipmentId ? route : [...route, equipmentId];

/** BE gửi boolean; chấp nhận cả chuỗi để không hiện nhầm cờ thông báo nếu envelope đổi kiểu. */
export const isNotifyRecord = (record: VehicleAlertData): boolean => {
	const value = (record as any)?.notify ?? (record as any)?.Notify;
	return !(value === false || String(value).toLowerCase() === 'false');
};

/**
 * Bảng "hoạt động gần đây" của bigscreen, hiện ở khung camera khi không có camera để xem.
 * Store toàn cục: App.vue nghe NATS và đẩy vào kể cả khi bigscreen chưa mở, nên mở ra là có ngay.
 */
export const useActivityFeed = defineStore('activityFeed', {
	state: () => ({
		/** Mới nhất trước (theo lượt gần nhất của thẻ). */
		items: [] as ActivityFeedItem[],
		/** Camera theo id lấy từ bản tin trực tiếp (tên, Km). */
		cameras: {} as Record<string, ActivityFeedCamera>,
		loaded: {} as Partial<Record<ActivityFeedSource, boolean>>,
		loading: {} as Partial<Record<ActivityFeedSource, boolean>>,
		loadFailed: {} as Partial<Record<ActivityFeedSource, boolean>>,
	}),
	getters: {
		bySource: (state) => (source: ActivityFeedSource) => state.items.filter((item) => item.source === source),
	},
	actions: {
		/**
		 * Thêm thẻ hoặc GỘP vào thẻ cùng key (cùng chuyến): lộ trình nối thêm, khoảng giờ mở rộng, số lượt cộng dồn.
		 * Thẻ nạp từ lịch sử gộp với thẻ trực tiếp đã có thì lấy bản đầy đủ hơn (lộ trình dài hơn, số lượt lớn hơn).
		 */
		push(...incoming: ActivityFeedItem[]) {
			if (!incoming.length) return;

			const byKey = new Map(this.items.map((item) => [item.key, item]));
			for (const item of incoming) {
				const existing = byKey.get(item.key);
				byKey.set(item.key, existing ? mergeItems(existing, item) : item);
			}

			const counts: Partial<Record<ActivityFeedSource, number>> = {};
			this.items = [...byKey.values()]
				.sort((a, b) => b.time.getTime() - a.time.getTime())
				.filter((item) => {
					counts[item.source] = (counts[item.source] ?? 0) + 1;
					return counts[item.source]! <= MAX_ITEMS_PER_SOURCE;
				});
		},

		/** Payload NATS lọc xe — đơn hoặc mảng (useTransporterVehicleAlert đã bỏ lớp bọc). */
		pushVehicleFilterPayload(payload: VehicleAlertPayload) {
			const records = Array.isArray(payload) ? payload : [payload];
			records.forEach((record) => this.pushVehicleFilter(record));
		},

		/** Một bản tin `ta.its.data.vds.filter`: một lượt khớp, gộp vào thẻ của chuyến (tripId). */
		pushVehicleFilter(record: VehicleAlertData) {
			const r = record as any;
			const plate = text(r.plate ?? r.Plate ?? r.licensePlate);
			if (!plate) return;

			const time = toDate(r.detectTime ?? r.DetectTime) ?? new Date();
			const historyId = text(r.historyId ?? r.HistoryId);
			const hitKey = historyId ?? `${plate}-${time.getTime()}`;
			// Bản tin cũ (chưa có tripId) thì mỗi lượt một thẻ như trước.
			const tripId = text(r.tripId ?? r.TripId) ?? hitKey;
			const equipmentId = text(r.equipmentId ?? r.EquipmentId);

			if (equipmentId) {
				this.cameras[equipmentId] = {
					name: text(r.equipmentName ?? r.EquipmentName) ?? this.cameras[equipmentId]?.name,
					km: toNumber(r.km ?? r.Km) ?? this.cameras[equipmentId]?.km,
					met: toNumber(r.met ?? r.Met) ?? this.cameras[equipmentId]?.met,
				};
			}

			this.push({
				key: 'vehicleFilter:' + tripId,
				source: 'vehicleFilter',
				startTime: time,
				time,
				title: plate,
				typeCode: text(r.filterType ?? r.FilterType),
				locationId: text(r.location ?? r.Location),
				direction: text(r.direction ?? r.Direction),
				route: equipmentId ? [equipmentId] : [],
				hitCount: 1,
				notify: isNotifyRecord(record),
				isLive: true,
				hitKeys: [hitKey],
			});
		},

		/** Nạp các CHUYẾN gần nhất từ TmsVehicleFilterHistory (đã gộp ở API); chỉ nạp một lần trừ khi force. */
		async loadVehicleFilterHistory(force = false) {
			if (this.loaded.vehicleFilter && !force) return;
			if (vehicleFilterLoad) return vehicleFilterLoad;

			vehicleFilterLoad = (async () => {
				this.loading.vehicleFilter = true;
				try {
					const now = Date.now();
					const response = await getAPI(TmsVehicleFilterHistoryApi).apiTmsTmsvehiclefilterhistoryTrippagePost({
						fromDate: new Date(now - VEHICLE_FILTER_HISTORY_DAYS * 86_400_000),
						// Lệch giờ giữa máy trạm và máy chủ: lấy dư một phút để không mất lượt vừa ghi.
						toDate: new Date(now + 60_000),
						page: 1,
						pageSize: INITIAL_PAGE_SIZE,
					});
					const trips: VehicleFilterHistoryTripOutput[] = response?.data?.result?.records ?? [];
					this.push(
						...trips
							.filter((trip) => text(trip.tripId) && text(trip.licensePlate))
							.map((trip) => {
								const end = toDate(trip.toTime) ?? new Date(now);
								return {
									key: 'vehicleFilter:' + trip.tripId,
									source: 'vehicleFilter' as const,
									startTime: toDate(trip.fromTime) ?? end,
									time: end,
									title: trip.licensePlate!,
									typeCode: text(trip.type),
									locationId: text(trip.locationId),
									direction: text(trip.direction),
									route: (trip.equipmentIds ?? []).filter(Boolean) as string[],
									hitCount: trip.hitCount ?? 1,
									isLive: false,
									hitKeys: [],
								};
							})
					);
					this.loaded.vehicleFilter = true;
					this.loadFailed.vehicleFilter = false;
				} catch (error) {
					this.loadFailed.vehicleFilter = true;
					console.error('[ActivityFeed] Không nạp được lịch sử lọc xe.', error);
				} finally {
					this.loading.vehicleFilter = false;
					vehicleFilterLoad = null;
				}
			})();

			return vehicleFilterLoad;
		},
	},
});

/** Gộp hai thẻ cùng chuyến. Lượt trực tiếp đã có (cùng hitKey) không cộng lại. */
function mergeItems(existing: ActivityFeedItem, incoming: ActivityFeedItem): ActivityFeedItem {
	// Thẻ lịch sử (đã gộp ở API) đến sau thẻ trực tiếp: lấy bản đầy đủ hơn thay vì cộng dồn hai lần.
	if (!incoming.isLive) {
		return {
			...existing,
			startTime: incoming.startTime < existing.startTime ? incoming.startTime : existing.startTime,
			time: incoming.time > existing.time ? incoming.time : existing.time,
			route: incoming.route.length > existing.route.length ? incoming.route : existing.route,
			hitCount: Math.max(existing.hitCount, incoming.hitCount),
		};
	}

	const newHits = incoming.hitKeys.filter((key) => !existing.hitKeys.includes(key));
	if (!newHits.length) return existing;

	const isLater = incoming.time >= existing.time;
	return {
		...existing,
		// Thông tin mới nhất theo lượt gần nhất (loại danh sách có thể đổi trên web).
		title: isLater ? incoming.title : existing.title,
		typeCode: (isLater ? incoming.typeCode : existing.typeCode) ?? existing.typeCode,
		direction: existing.direction ?? incoming.direction,
		locationId: existing.locationId ?? incoming.locationId,
		startTime: incoming.startTime < existing.startTime ? incoming.startTime : existing.startTime,
		time: isLater ? incoming.time : existing.time,
		// Lượt đến lệch thứ tự không chèn ngược vào lộ trình — chỉ nối khi là lượt mới nhất.
		route: isLater ? incoming.route.reduce(appendRoute, existing.route) : existing.route,
		hitCount: existing.hitCount + newHits.length,
		notify: existing.notify || incoming.notify,
		isLive: true,
		hitKeys: [...existing.hitKeys, ...newHits],
	};
}
