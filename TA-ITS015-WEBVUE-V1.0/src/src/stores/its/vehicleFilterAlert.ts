import { defineStore } from 'pinia';
import { getAPI } from '/@/utils/axios-utils';
import { StatusEnum, TmsVehicleFilterApi, type TmsVehicleFilterOutput } from '/@/api-services/tms';
import { TmsVehicleFilterWarningEnum } from '/@/types/enums/tmsEnum';
import type { VehicleAlertData, VehicleAlertPayload } from '/@/transporter/hooks/useTransporterVehicleFilter';
import { isNotifyRecord } from '/@/stores/its/activityFeed';

export const VEHICLE_FILTER_ALERT_DURATION_MS = 10_000;
/** Cửa sổ dùng lại danh sách đầy đủ khi phải tra cứu dự phòng (tránh gọi dồn khi một loạt bản tin về cùng lúc). */
const FILTER_LIST_FALLBACK_TTL_MS = 15_000;

export interface VehicleFilterAlertItem {
	id: string;
	licensePlate: string;
	type: string;
	detectedAt: Date;
	expiresAt: number;
}

/** Các promise đang bay, gom theo biển số để nhiều bản tin cùng biển chỉ gọi API một lần. */
const pendingLookups = new Map<string, Promise<TmsVehicleFilterOutput | null>>();
let fallbackListCache: { fetchedAt: number; items: TmsVehicleFilterOutput[] } | null = null;
let fallbackListPromise: Promise<TmsVehicleFilterOutput[]> | null = null;

const pick = (value: VehicleAlertData, ...keys: string[]): unknown => {
	for (const key of keys) {
		if (value?.[key] !== undefined && value[key] !== null) return value[key];
	}

	return undefined;
};

/** So khớp được cả 51A-123.45, 51A 12345 và 51A12345. */
const normalizeLicensePlate = (value: unknown): string =>
	String(value ?? '')
		.trim()
		.toUpperCase()
		.replace(/[\s.\-_]/g, '');

const asNullableString = (value: unknown): string | null => {
	if (value === undefined || value === null) return null;
	const normalized = String(value).trim();
	return normalized || null;
};

const asDate = (value: unknown, fallback = new Date()): Date => {
	if (value === undefined || value === null || value === '') return fallback;

	const numericValue = typeof value === 'number' && value < 1_000_000_000_000 ? value * 1000 : value;
	const parsed = new Date(numericValue as string | number | Date);
	return Number.isNaN(parsed.getTime()) ? fallback : parsed;
};

/** Lấy danh sách phương tiện lọc đang bật; dùng cache ngắn vì chỉ chạy khi tra theo biển số không ra. */
const loadEnabledFilterList = async (): Promise<TmsVehicleFilterOutput[]> => {
	const now = Date.now();
	if (fallbackListCache && now - fallbackListCache.fetchedAt < FILTER_LIST_FALLBACK_TTL_MS) {
		return fallbackListCache.items;
	}
	if (fallbackListPromise) return fallbackListPromise;

	fallbackListPromise = getAPI(TmsVehicleFilterApi)
		.apiTmsTmsvehiclefilterListGet(undefined, undefined, undefined, undefined, undefined, StatusEnum.NUMBER_1)
		.then((response) => response.data.result ?? []);

	try {
		const items = await fallbackListPromise;
		fallbackListCache = { fetchedAt: Date.now(), items };
		return items;
	} finally {
		fallbackListPromise = null;
	}
};

/**
 * Theo dõi cảnh báo phương tiện sinh từ subject `ta.its.data.vds.filter`.
 *
 * BE phát MỌI lượt khớp kèm cờ `notify`: chỉ lượt MỞ CHUYẾN (A → B → C → D cùng biển trong khoảng nghỉ
 * vds_vehicle_filter_trip_gap) khi alarm bật mới có `notify = true` — toast chỉ hiện các lượt đó, mỗi chuyến
 * một lần; các lượt nối chuyến chỉ vào "Hoạt động gần đây" (store activityFeed, App.vue đẩy vào).
 * BE đã tự ghi TmsVehicleFilterHistory, nên store KHÔNG ghi history.
 * API chỉ được gọi để lấy cấu hình mới nhất của biển số (loại cảnh báo, cờ alarm) thay vì
 * tin vào payload NATS; nếu tra cứu lỗi hoặc không có kết quả thì vẫn hiển thị theo payload.
 */
export const useVehicleFilterAlert = defineStore('vehicleFilterAlert', {
	state: () => ({
		alerts: [] as VehicleFilterAlertItem[],
		nowTick: Date.now(),
		alertSequence: 0,
		/** Số bản tin bị bỏ vì cấu hình mới nhất đã tắt alarm — dùng để soi khi vận hành thắc mắc. */
		suppressedByAlarmOffCount: 0,
	}),
	actions: {
		/**
		 * Lấy cấu hình mới nhất của một biển số: ưu tiên truy vấn thẳng theo biển số,
		 * nếu không ra (thường do khác định dạng biển) thì đối chiếu trên danh sách đang bật.
		 * Trả null khi không tra được — phía gọi phải coi đây là "không rõ", không phải "bị loại".
		 */
		async fetchLatestFilter(licensePlate: string): Promise<TmsVehicleFilterOutput | null> {
			const normalizedPlate = normalizeLicensePlate(licensePlate);
			if (!normalizedPlate) return null;

			const pending = pendingLookups.get(normalizedPlate);
			if (pending) return pending;

			const lookup = (async (): Promise<TmsVehicleFilterOutput | null> => {
				const matchPlate = (items: TmsVehicleFilterOutput[]) =>
					items.find((item) => normalizeLicensePlate(item.licensePlate) === normalizedPlate) ?? null;

				try {
					const response = await getAPI(TmsVehicleFilterApi).apiTmsTmsvehiclefilterListGet(
						licensePlate,
						undefined,
						undefined,
						undefined,
						undefined,
						StatusEnum.NUMBER_1
					);
					const exactMatch = matchPlate(response.data.result ?? []);
					if (exactMatch) return exactMatch;

					return matchPlate(await loadEnabledFilterList());
				} catch (error) {
					console.error(
						`[VehicleFilterAlert] Không tra được cấu hình mới nhất cho biển số ${licensePlate}; hiển thị theo dữ liệu NATS.`,
						error
					);
					return null;
				}
			})();

			pendingLookups.set(normalizedPlate, lookup);

			try {
				return await lookup;
			} finally {
				pendingLookups.delete(normalizedPlate);
			}
		},

		addAlert(licensePlate: string, type: string, detectedAt: Date, sourceId?: string | null) {
			this.alertSequence += 1;
			const createdAt = Date.now();
			// Giữ historyId để đối chiếu log, nhưng vẫn nối sequence cho chắc key là duy nhất.
			const prefix = sourceId ?? normalizeLicensePlate(licensePlate);

			this.alerts.unshift({
				id: `${prefix}-${createdAt}-${this.alertSequence}`,
				licensePlate,
				type,
				detectedAt,
				expiresAt: createdAt + VEHICLE_FILTER_ALERT_DURATION_MS,
			});
		},

		dismissAlert(id: string) {
			this.alerts = this.alerts.filter((item) => item.id !== id);
		},

		getRemainingSeconds(item: VehicleFilterAlertItem): number {
			return Math.max(0, Math.ceil((item.expiresAt - this.nowTick) / 1000));
		},

		getRemainingPercent(item: VehicleFilterAlertItem): number {
			const remainingMs = item.expiresAt - this.nowTick;
			return Math.min(100, Math.max(0, (remainingMs / VEHICLE_FILTER_ALERT_DURATION_MS) * 100));
		},

		/** Cập nhật countdown và loại bỏ alert hết 10 giây. */
		tick() {
			this.nowTick = Date.now();
			this.alerts = this.alerts.filter((item) => item.expiresAt > this.nowTick);
		},

		async processRecord(record: VehicleAlertData) {
			const licensePlate = asNullableString(pick(record, 'plate', 'Plate', 'licensePlate', 'LicensePlate'));
			if (!licensePlate) return;

			const detectedAt = asDate(
				pick(record, 'detectTime', 'DetectTime', 'occurredUtc', 'OccurredUtc', 'fromTime', 'FromTime', 'time', 'Time')
			);
			const historyId = asNullableString(pick(record, 'historyId', 'HistoryId'));
			const filter = await this.fetchLatestFilter(licensePlate);

			// Chỉ bỏ bản tin khi tra được cấu hình mới nhất và cấu hình đó đã tắt alarm.
			if (filter && String(filter.alarm) === TmsVehicleFilterWarningEnum.NoAlarm) {
				this.suppressedByAlarmOffCount += 1;
				return;
			}

			const type =
				asNullableString(filter?.type) ??
				asNullableString(pick(record, 'filterType', 'FilterType', 'type', 'Type')) ??
				'';

			this.addAlert(licensePlate, type, detectedAt, historyId);
		},

		/** Xử lý cả payload đơn và payload mảng do useTransporterVehicleAlert trả về. */
		async processVdsData(payload: VehicleAlertPayload) {
			const records = Array.isArray(payload) ? payload : [payload];

			// Chỉ lượt mở chuyến (notify) mới hiện toast — dự án ~2 km một camera, báo mọi lượt thì toast bật liên tục.
			await Promise.all(records.filter(isNotifyRecord).map((record) => this.processRecord(record)));
		},
	},
});
