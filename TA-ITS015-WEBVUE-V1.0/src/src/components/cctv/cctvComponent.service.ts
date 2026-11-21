/**
 * cctvComponent.service.ts
 *
 * Helper RIÊNG của bộ component dùng chung trong `src/components/cctv`.
 *
 * Mục đích: cho phép các nơi khác (bigScreen, popup sự cố, ...) hiển thị camera bằng
 * `CctvCameraView.vue` mà KHÔNG cần biết camera đó có được hệ thống CCTV (VMS/NX) hỗ trợ
 * hay không. BE kiểm tra VMS và trả nguồn phát + URL dự phòng theo IP:
 *
 *   - `cctv` : camera có trong CCTV  -> dùng CctvFocusPanel (WebRTC + PTZ + playback + snapshot).
 *   - `rtsp` : không có / không lấy được luồng -> fallback về LiveCam (JSMpeg + RTSP) như cũ.
 *
 * FE không suy luận hỗ trợ CCTV từ deviceview hoặc từ việc dựng được chuỗi URL.
 */

import { useBaseConfig } from '/@/stores/baseConfig';
import type { CctvWebRtcStreamOutput } from '/@/api-services/cctv';
import { getCctvStreamSource } from '/@/views/cctv/dashboard/services/cctvDashboardService';
import { getCctvDefaultStream } from '/@/views/cctv/dashboard/services/cctvDashboardConfig';
import type { VideoMonitoringItem } from '/@/views/cctv/dashboard/types';

// Chỉ ghi metadata; không ghi Axios config, Authorization, URL RTSP hoặc ticket.
const probeErrorDetails = (error: any) => ({
	name: error?.name,
	httpStatus: error?.response?.status,
	code: error?.response?.data?.code ?? error?.code,
	message: safeString(error?.response?.data?.message ?? error?.message)
		.replace(/(?:https?|wss?|rtsp):\/\/\S+/gi, '[URL redacted]'),
});

/** Gộp/cache ngắn quyết định nguồn của BE, không coi là đã kết nối thành công. */
const SOURCE_TTL_MS = 30_000;

/**
 * Id loại thiết bị (TmsEquipmentType.Id) được coi là camera của hệ thống CCTV.
 * Giá trị theo DB hiện hành; vẫn giữ thêm kiểm tra theo `code` để không phụ thuộc tuyệt đối vào Id.
 */
export const CctvCameraEquipmentTypeId = {
	/** Camera PTZ (quay/quét/zoom được) */
	Ptz: '5251dd08-4328-47a3-856f-8291052e6d51',
	/** Camera FIX (cố định) */
	Fix: '437edd08-2208-498e-8853-3d6da2cec3c8',
	/** Camera CCTV */
	Cctv: '05c0dc08-d70e-432a-8bbb-32ffe559023d',
} as const;

const CCTV_CAMERA_EQUIPMENT_TYPE_IDS = new Set<string>(Object.values(CctvCameraEquipmentTypeId).map((id) => id.toLowerCase()));

/** Từ khoá trong `code` của loại thiết bị để nhận diện camera khi Id chưa khớp (môi trường khác DB). */
const CCTV_CAMERA_EQUIPMENT_TYPE_CODE_KEYWORDS = ['cctv', 'ptz', 'fix', 'fcam', 'tcam', 'pcam'];

/** Nguồn phát của khung view camera. */
export type CctvCameraSourceMode = 'cctv' | 'rtsp';

/** Lý do dẫn tới kết luận nguồn phát (phục vụ log/tooltip). */
export type CctvCameraSourceReason = '' | 'no-ip' | 'not-registered' | 'stream-unavailable' | 'probe-error';

/** Thông tin thiết bị lấy từ CCTV (đã chuẩn hoá) — tương ứng 1 record `deviceview`. */
export interface CctvComponentDevice {
	/** Id thiết bị phía ITS (nếu deviceview có trả) */
	id: string;
	/** DeviceId phía VMS/NX — khoá để gọi PTZ/playback/snapshot */
	deviceId: string;
	ip: string;
	name: string;
	/** URL luồng do CCTV cấu hình (nếu có) */
	url: string;
	location: string;
	status: 'online' | 'warning' | 'offline';
	equipmentTypeId: string;
	equipmentTypeName: string;
}

/** Kết quả xác định nguồn phát cho một thiết bị ITS. */
export interface CctvCameraSource {
	mode: CctvCameraSourceMode;
	camera: VideoMonitoringItem | null;
	device: CctvComponentDevice | null;
	reason: CctvCameraSourceReason;
	streamUrl: string;
	fallbackUrl: string;
}

const safeString = (value: unknown, fallback = ''): string => {
	try {
		if (value === undefined || value === null) return fallback;
		if (typeof value === 'string') return value;
		if (typeof value === 'number' || typeof value === 'boolean') return String(value);
		return fallback;
	} catch {
		return fallback;
	}
};

/**
 * Chuẩn hoá IP để so khớp giữa dữ liệu ITS và CCTV: bỏ khoảng trắng, hạ chữ thường,
 * bỏ scheme (`rtsp://`, `http://`), bỏ phần đường dẫn và bỏ cổng (`:554`).
 *
 * @param value IP thô lấy từ thiết bị.
 * @returns Host đã chuẩn hoá, chuỗi rỗng nếu không dùng được.
 */
export const normalizeCctvIp = (value: unknown): string => {
	const text = safeString(value).trim().toLowerCase();
	if (!text) return '';
	const withoutScheme = text.replace(/^[a-z][a-z0-9+.-]*:\/\//, '');
	const host = withoutScheme.split('/')[0].split('@').pop() || '';
	// Chỉ cắt cổng khi có đúng 1 dấu ':' (tránh phá IPv6).
	const colonCount = (host.match(/:/g) || []).length;
	return (colonCount === 1 ? host.split(':')[0] : host).trim();
};

/** Loại thiết bị (theo Id) có phải camera CCTV/PTZ/FIX hay không. */
export const isCctvCameraEquipmentTypeId = (equipmentTypeId: unknown): boolean => {
	const id = safeString(equipmentTypeId).trim().toLowerCase();
	return Boolean(id) && CCTV_CAMERA_EQUIPMENT_TYPE_IDS.has(id);
};

/** Loại thiết bị (theo code) có phải camera hay không — dự phòng khi Id chưa khớp. */
export const isCctvCameraEquipmentTypeCode = (equipmentTypeCode: unknown): boolean => {
	const code = safeString(equipmentTypeCode).trim().toLowerCase();
	if (!code) return false;
	return CCTV_CAMERA_EQUIPMENT_TYPE_CODE_KEYWORDS.some((keyword) => code.includes(keyword));
};

/**
 * Thiết bị có phải camera thuộc hệ thống CCTV (CCTV / PTZ / FIX) hay không.
 * Ưu tiên khớp theo `equipmentTypeId`, dự phòng theo `equipmentTypeCode`.
 */
export const isCctvCameraEquipment = (input: { equipmentTypeId?: unknown; equipmentTypeCode?: unknown } | null | undefined): boolean => {
	if (!input) return false;
	return isCctvCameraEquipmentTypeId(input.equipmentTypeId) || isCctvCameraEquipmentTypeCode(input.equipmentTypeCode);
};

/** Cache theo khoá + gộp request đang bay, tránh probe lặp khi mở nhiều modal cùng lúc. */
const createTtlCache = <T>(ttlMs: number) => {
	const store = new Map<string, { value: T; expiresAt: number }>();
	const inflight = new Map<string, Promise<T>>();
	return {
		async resolve(key: string, factory: () => Promise<T>): Promise<T> {
			const cached = store.get(key);
			if (cached && cached.expiresAt > Date.now()) {
				console.info('[cctvComponent] probe-cache-hit', { key, remainingMs: cached.expiresAt - Date.now() });
				return cached.value;
			}
			const pending = inflight.get(key);
			if (pending) return pending;
			const task: Promise<T> = factory()
				.then((value) => {
					if (inflight.get(key) === task) store.set(key, { value, expiresAt: Date.now() + ttlMs });
					return value;
				})
				.finally(() => {
					if (inflight.get(key) === task) inflight.delete(key);
				});
			inflight.set(key, task);
			return task;
		},
		invalidate(prefix: string) {
			for (const key of store.keys()) if (key.startsWith(prefix)) store.delete(key);
			for (const key of inflight.keys()) if (key.startsWith(prefix)) inflight.delete(key);
		},
		clear() {
			store.clear();
			inflight.clear();
		},
	};
};

const sourceCache = createTtlCache<CctvWebRtcStreamOutput>(SOURCE_TTL_MS);

/** Xoá cache probe (dùng khi người dùng bấm tải lại khung view). */
export const clearCctvComponentProbeCache = (ip?: string) => {
	const key = normalizeCctvIp(ip);
	if (!key) {
		sourceCache.clear();
		return;
	}
	sourceCache.invalidate(`${key}|`);
};

/**
 * Đảm bảo cấu hình vận hành CCTV (`cctv_config`) đã được tải, vì `getCctvVendor()` /
 * `getCctvDefaultStream()` đọc từ store `baseConfig`. Các trang bigScreen không tải sẵn.
 */
export const ensureCctvComponentConfigLoaded = async (): Promise<void> => {
	try {
		const baseConfig = useBaseConfig();
		if (!baseConfig.opConfigList || Object.keys(baseConfig.opConfigList).length === 0) {
			await baseConfig.setOpConfigList();
		}
	} catch (error) {
		console.warn('[cctvComponent] config-failed', probeErrorDetails(error));
	}
};

/**
 * Dựng `VideoMonitoringItem` (kiểu dữ liệu camera dùng chung của bộ component CCTV)
 * từ thiết bị ITS + thiết bị CCTV đã tra được.
 *
 * `videoUrl` bắt buộc có giá trị, nếu không PTZ/Snapshot sẽ bị vô hiệu hoá
 * (xem `getLiveControlDisabledReason` -> 'no-stream'), nên fallback về URL RTSP của ITS.
 */
export const buildCctvCameraFromEquipment = (equipment: any, device?: CctvComponentDevice | null): VideoMonitoringItem => {
	const equipmentId = safeString(equipment?.id);
	const ip = safeString(equipment?.ip) || safeString(device?.ip);
	const kmNumber = equipment?.kmNumber;
	const metNumber = equipment?.metNumber;
	const location =
		kmNumber !== undefined && kmNumber !== null && kmNumber !== ''
			? `Km${kmNumber}+${metNumber ?? 0}`
			: safeString(device?.location);

	return {
		id: equipmentId || safeString(device?.id) || normalizeCctvIp(ip),
		sourceCameraId: equipmentId || safeString(device?.id),
		deviceId: safeString(device?.deviceId),
		ip,
		name: safeString(equipment?.name) || safeString(device?.name) || 'Camera',
		location,
		status: device?.status || (Number(equipment?.equipmentState) === 1 ? 'online' : 'offline'),
		aspect: '16:9',
		videoUrl: safeString(device?.url) || safeString(equipment?.url),
		x: 0,
		y: 0,
		w: 2,
		h: 2,
	};
};

/** Chỉ URL RTSP gốc được gửi vào relay; không đưa URL WebRTC/proxy vào tham số url. */
export const getRtspFallbackUrl = (value: unknown): string => {
	const url = safeString(value).trim();
	return /^rtsps?:\/\//i.test(url) ? url : '';
};

/** Một request xin nguồn tại BE; giữ URL CCTV đã cấp để panel dùng trực tiếp. */
export const resolveCctvCameraSource = async (equipment: any): Promise<CctvCameraSource> => {
	let fallbackUrl = getRtspFallbackUrl(equipment?.url);
	const buildRtspSource = (reason: CctvCameraSourceReason): CctvCameraSource => ({
		mode: 'rtsp',
		camera: buildCctvCameraFromEquipment(equipment),
		device: null,
		reason,
		streamUrl: '',
		fallbackUrl,
	});

	try {
		const ip = normalizeCctvIp(equipment?.ip);
		if (!ip) return buildRtspSource('no-ip');
		await ensureCctvComponentConfigLoaded();
		const stream = getCctvDefaultStream();
		const source = await sourceCache.resolve(`${ip}|${stream}`, () =>
			getCctvStreamSource(buildCctvCameraFromEquipment({ ...equipment, ip }), { stream }));
		fallbackUrl = getRtspFallbackUrl(source.fallbackUrl) || getRtspFallbackUrl(source.source === 'rtsp' ? source.url : '') || fallbackUrl;
		if (source.source === 'rtsp') {
			const reason = source.reason === 'not-registered' ? 'not-registered'
				: source.reason === 'probe-error' ? 'probe-error' : 'stream-unavailable';
			return buildRtspSource(reason);
		}
		const streamUrl = safeString(source.url).trim();
		if (!/^wss?:\/\//i.test(streamUrl) || !source.deviceId) return buildRtspSource('stream-unavailable');
		const device: CctvComponentDevice = {
			id: safeString(equipment?.id), deviceId: source.deviceId, ip,
			name: safeString(equipment?.name), url: streamUrl, location: '',
			status: source.deviceState === 0 || source.deviceState === 4 ? 'offline' : source.deviceState === 2 ? 'warning' : 'online',
			equipmentTypeId: safeString(equipment?.equipmentTypeId), equipmentTypeName: '',
		};
		return { mode: 'cctv', camera: buildCctvCameraFromEquipment(equipment, device), device, reason: '', streamUrl, fallbackUrl };
	} catch (error) {
		console.warn('[cctvComponent] source-probe-failed -> RTSP', { ip: normalizeCctvIp(equipment?.ip), ...probeErrorDetails(error) });
		return buildRtspSource('probe-error');
	}
};
