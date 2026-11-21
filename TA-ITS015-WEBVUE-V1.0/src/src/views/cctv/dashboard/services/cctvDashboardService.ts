import {
	CctvDeviceApi,
	CctvMediaApi,
	CctvPtzApi,
	DashboardApi,
	DashboardItemApi,
	PtzDirection,
	TemplateApi,
	StatusEnum,
	ZoomDirection,
	type CctvDeviceViewOutput,
	type CctvWebRtcStreamOutput,
	type DashboardItemOutput,
	type DashboardOutput,
	type DashboardTagOutput,
	type TemplateCctvOutput as TemplateOutput,
	type CctvPtzPresetOutput,
} from '/@/api-services/cctv';

import type {
	DashboardCCTVDetailOutput,
	DashboardCCTVOutput,
	DashboardCameraInfo,
	ICctvDashboardTemplate,
	VideoMonitoringItem,
} from '/@/views/cctv/dashboard/types';
import { getAPI, getToken } from '/@/utils/axios-utils';
import { getCctvVendor } from './cctvDashboardConfig';

const asArray = <T>(value: T[] | null | undefined) => (Array.isArray(value) ? value : []);
const cctvRequestOptions = { customCatch: true };

const safeString = (value: unknown, fallback = '') => {
	try {
		if (value === undefined || value === null) return fallback;
		if (typeof value === 'string') return value;
		if (typeof value === 'number' || typeof value === 'boolean') return String(value);
		if (value instanceof Date) return value.toISOString();
		if (typeof value === 'object') {
			const source = value as Record<string, unknown>;
			const primitiveValue = source.id ?? source.ID ?? source.Id ?? source.value ?? source.Value ?? source.code ?? source.Code ?? source.name ?? source.Name ?? source.url ?? source.Url;
			if (primitiveValue !== value) return safeString(primitiveValue, fallback);
		}
		return fallback;
	} catch {
		return fallback;
	}
};

const getApiBaseUrl = () => safeString(window.__env__?.VITE_API_URL).replace(/\/+$/, '');

const safeNumber = (value: unknown, fallback: number) => {
	try {
		const nextValue = Number(value);
		return Number.isFinite(nextValue) ? nextValue : fallback;
	} catch {
		return fallback;
	}
};

const appendQueryParam = (params: URLSearchParams, key: string, value: unknown) => {
	if (value === undefined || value === null || value === '') return;
	if (value instanceof Date) {
		params.set(key, value.toISOString());
		return;
	}
	const text = safeString(value);
	if (text) params.set(key, text);
};

const cctvFetchRaw = async (path: string, query: Record<string, unknown> = {}) => {
	const params = new URLSearchParams();
	Object.entries(query).forEach(([key, value]) => appendQueryParam(params, key, value));
	const queryText = params.toString();
	const response = await fetch(`${getApiBaseUrl()}${path}${queryText ? `?${queryText}` : ''}`, {
		method: 'GET',
		headers: {
			Authorization: `Bearer ${getToken()}`,
			Accept: 'application/json,image/*,*/*',
		},
	});
	if (!response.ok) {
		const text = await response.text().catch(() => '');
		throw new Error(text || response.statusText || 'Request Error');
	}
	return response;
};

const getResponseId = (data: unknown) => {
	const source = data as any;
	return safeString(source?.idDashboard || source?.dashboardId || source?.id || source?.result?.idDashboard || source?.result?.dashboardId || source?.result?.id || source?.result);
};

const normalizeTags = (tags: unknown): string[] => {
	if (Array.isArray(tags)) return tags.map((tag) => safeString(tag)).filter(Boolean);
	if (typeof tags === 'string') {
		return tags
			.split(',')
			.map((tag) => tag.trim())
			.filter(Boolean);
	}
	return [];
};

const serializeTags = (tags: string[]) => tags.filter(Boolean);

const mapDashboardOutput = (dashboard: DashboardOutput): DashboardCCTVOutput => {
	const id = safeString(dashboard.id || dashboard.code);
	const updatedAt = dashboard.updateTime ? new Date(dashboard.updateTime).toISOString() : new Date().toISOString();
	const createdAt = dashboard.createTime ? new Date(dashboard.createTime).toISOString() : updatedAt;
	return {
		id,
		name: safeString(dashboard.name),
		remark: safeString(dashboard.remark),
		tags: normalizeTags((dashboard as any).tagList ?? dashboard.tags),
		createdAt,
		updatedAt,
		cameraCount: safeNumber((dashboard as any).cameraCount ?? (dashboard as any).CameraCount, 0),
	};
};

const mapDeviceOutput = (device: CctvDeviceViewOutput): DashboardCameraInfo => {
	const id = safeString(device.id || device.deviceId || device.ip);
	const name = safeString(device.name).trim() || 'Camera chưa đặt tên';
	return {
		id,
		code: safeString(device.deviceId || id),
		name,
		ip: safeString(device.ip),
		location: safeString(device.remark),
		status: device.equipmentState === 0 ? 'offline' : device.equipmentState === 2 ? 'warning' : 'online',
		aspect: '16:9',
		videoUrl: safeString(device.url),
		x: 0,
		y: 0,
		w: 2,
		h: 2,
		equipmentTypeId: safeString(device.equipmentTypeId),
		equipmentTypeName: safeString(device.equipmentTypeName),
		snapshotUrl: safeString(device.snapshotUrl),
		snapshotTime: device.snapshotTime ? new Date(device.snapshotTime).toISOString() : undefined,
	};
};

const mapTemplateOutput = (template: TemplateOutput): ICctvDashboardTemplate => ({
	ID: safeString(template.id),
	Name: safeString(template.name || template.id),
	Status: template.status === StatusEnum.NUMBER_0 ? 0 : 1,
	Items: asArray(template.items).map((item, index) => ({
		SlotNo: item.slotNo || index + 1,
		X: item.x || 0,
		Y: item.y || 0,
		W: item.w || 1,
		H: item.h || 1,
	})),
});

const getTemplateOutputFromResponse = (data: unknown): TemplateOutput | null => {
	const source = data as any;
	const result = source?.result;
	if (Array.isArray(result)) return (result[0] as TemplateOutput) || null;
	if (result && typeof result === 'object') return result as TemplateOutput;
	if (source?.id || source?.templateId || source?.idTemplate) {
		return {
			...(source as TemplateOutput),
			id: safeString(source.id || source.templateId || source.idTemplate),
		};
	}
	return null;
};

const readValue = (source: Record<string, unknown>, keys: string[]) => {
	for (const key of keys) {
		const value = source[key];
		if (value !== undefined && value !== null && value !== '') return value;
	}
	return undefined;
};

const readString = (source: Record<string, unknown>, keys: string[]) => safeString(readValue(source, keys));
const readNumber = (source: Record<string, unknown>, keys: string[], fallback: number) => {
	return safeNumber(readValue(source, keys), fallback);
};

const mapDashboardItemToCamera = (item: DashboardItemOutput, device?: DashboardCameraInfo): VideoMonitoringItem => {
	const raw = item as Record<string, unknown>;
	const equipmentId = readString(raw, ['equipmentId', 'EquipmentId', 'deviceId', 'DeviceId', 'idITS', 'IdITS', 'itsId', 'ItsId']);
	const itemId = readString(raw, ['id', 'ID', 'Id', 'code', 'Code']);
	return {
		id: itemId || `dashboard-camera-${equipmentId}`,
		sourceCameraId: equipmentId,
		deviceId: device?.code || equipmentId,
		/* DashboardItemOutput đã trả kèm ip, dùng làm nguồn dự phòng khi không tra được device trong danh sách. */
		ip: device?.ip || readString(raw, ['ip', 'Ip', 'IP']),
		name: device?.name || 'Camera chưa đặt tên',
		location: device?.location || '',
		status: device?.status || 'offline',
		aspect: device?.aspect || '16:9',
		videoUrl: device?.videoUrl || '',
		x: readNumber(raw, ['x', 'X'], 0),
		y: readNumber(raw, ['y', 'Y'], 0),
		w: readNumber(raw, ['w', 'W'], 2),
		h: readNumber(raw, ['h', 'H'], 2),
	};
};

const mapDevicesByKnownIds = (devices: DashboardCameraInfo[]) => {
	const result = new Map<string, DashboardCameraInfo>();
	devices.forEach((device) => {
		[device.id, device.code, device.ip].forEach((value) => {
			const key = safeString(value);
			if (key) result.set(key, device);
		});
	});
	return result;
};

// BE bổ sung tham số Ip cho các API CCTV: camera được định danh bằng DeviceId HOẶC Ip.
// Helper lấy cả 2 khoá (bỏ chuỗi rỗng để không gửi query/body rác) và chỉ báo lỗi khi thiếu cả hai.
const getCameraKeys = (camera: VideoMonitoringItem) => ({
	deviceId: safeString(camera.deviceId) || undefined,
	ip: safeString(camera.ip) || undefined,
});

const requireCameraKeys = (camera: VideoMonitoringItem) => {
	const keys = getCameraKeys(camera);
	if (!keys.deviceId && !keys.ip) throw new Error('Không tìm thấy DeviceId/IP camera');
	return keys;
};

export const loadCctvEquipments = async (): Promise<DashboardCameraInfo[]> => {
	const result = await (getAPI(CctvDeviceApi as any) as any).apiCctvCctvdeviceDeviceviewGet(undefined, undefined, cctvRequestOptions);
	const rawDevices = asArray<CctvDeviceViewOutput>(result.data?.result);
	return rawDevices.map(mapDeviceOutput).filter((device) => Boolean(device.id));
};

export const loadCctvDashboardList = async (): Promise<DashboardCCTVOutput[]> => {
	const result = await (getAPI(DashboardApi as any) as any).apiCctvDashboardListGet(undefined, undefined, cctvRequestOptions);
	return asArray<DashboardOutput>(result.data?.result).map(mapDashboardOutput).filter((dashboard) => Boolean(dashboard.id));
};

export const loadCctvDashboardTags = async (): Promise<string[]> => {
	const result = await (getAPI(DashboardApi as any) as any).apiCctvDashboardTaglistGet(undefined, cctvRequestOptions);
	const tags = asArray<DashboardTagOutput>(result.data?.result)
		.map((tag) => safeString(tag.name))
		.filter(Boolean);
	return [...new Set(tags)];
};

export const getCctvDashboardDetail = async (id: string, cachedDevices?: DashboardCameraInfo[]): Promise<DashboardCCTVDetailOutput> => {
	// Tải song song thông tin dashboard + danh sách item. Danh sách thiết bị tái dùng
	// từ cache (store) nếu được truyền vào để tránh gọi lại deviceview mỗi lần mở dashboard.
	const [dashboardResult, itemsResult, loadedDevices] = await Promise.all([
		(getAPI(DashboardApi as any) as any).apiCctvDashboardByidGet(id, cctvRequestOptions),
		(getAPI(DashboardItemApi as any) as any).apiCctvDashboarditemListitemGet(undefined, id, undefined, undefined, cctvRequestOptions),
		cachedDevices && cachedDevices.length ? Promise.resolve(cachedDevices) : loadCctvEquipments(),
	]);
	const devices = loadedDevices;
	const dashboardOutput = dashboardResult.data?.result as DashboardOutput | undefined;
	const dashboard = dashboardOutput ? mapDashboardOutput(dashboardOutput) : null;
	if (!dashboard?.id) {
		throw new Error('Không tải được dashboard');
	}
	const devicesById = mapDevicesByKnownIds(devices);
	const rawItems = asArray<DashboardItemOutput>(itemsResult.data?.result);
	const cameras = rawItems.map((item) => {
		const equipmentId = readString(item as Record<string, unknown>, ['equipmentId', 'EquipmentId', 'deviceId', 'DeviceId', 'idITS', 'IdITS', 'itsId', 'ItsId']);
		const device = devicesById.get(equipmentId);
		return mapDashboardItemToCamera(item, device);
	});
	return { ...dashboard, cameraCount: cameras.length, cameras };
};

export const createCctvDashboard = async (name: string, remark: string, tags: string[]): Promise<DashboardCCTVOutput> => {
	const result = await (getAPI(DashboardApi as any) as any).apiCctvDashboardAddPost({ name, remark, tags: serializeTags(tags) }, cctvRequestOptions);
	const id = getResponseId(result.data);
	if (!id) throw new Error('Không tạo được dashboard');
	return {
		id,
		name,
		remark,
		tags,
		createdAt: new Date().toISOString(),
		updatedAt: new Date().toISOString(),
	};
};

export const updateCctvDashboard = async (dashboard: DashboardCCTVOutput): Promise<DashboardCCTVOutput> => {
	await (getAPI(DashboardApi as any) as any).apiCctvDashboardUpdatePost({
		id: dashboard.id,
		name: dashboard.name,
		remark: dashboard.remark,
		tags: serializeTags(dashboard.tags || []),
	}, cctvRequestOptions);
	return { ...dashboard, updatedAt: new Date().toISOString() };
};

export const deleteCctvDashboard = async (id: string) => {
	await (getAPI(DashboardApi as any) as any).apiCctvDashboardDeletePost({ id }, cctvRequestOptions);
};

export const duplicateCctvDashboard = async (dashboard: DashboardCCTVOutput, customName?: string): Promise<DashboardCCTVOutput> => {
	const name = customName || `${dashboard.name} (Copy)`;
	const result = await (getAPI(DashboardApi as any) as any).apiCctvDashboardCopyPost({ id: dashboard.id, name }, cctvRequestOptions);
	const id = getResponseId(result.data);
	if (!id) throw new Error('Không nhân bản được dashboard');
	return {
		...dashboard,
		id,
		name,
		createdAt: new Date().toISOString(),
		updatedAt: new Date().toISOString(),
	};
};

export const loadCctvDashboardTemplates = async (): Promise<ICctvDashboardTemplate[]> => {
	const result = await (getAPI(TemplateApi as any) as any).apiCctvTemplateTemplatelistGet(undefined, StatusEnum.NUMBER_1, cctvRequestOptions);
	return asArray<TemplateOutput>(result.data?.result).map(mapTemplateOutput).filter((template) => Boolean(template.ID));
};

export const createCctvDashboardTemplate = async (name: string, remark: string, cameras: VideoMonitoringItem[]): Promise<ICctvDashboardTemplate> => {
	const items = cameras.map((camera, index) => ({
		slotNo: camera.slotNo || index + 1,
		x: camera.x,
		y: camera.y,
		w: camera.w,
		h: camera.h,
	}));
	const result = await (getAPI(TemplateApi as any) as any).apiCctvTemplateAddPost({ name, remark, status: StatusEnum.NUMBER_1, items }, cctvRequestOptions);
	const template = getTemplateOutputFromResponse(result.data);
	if (!template?.id) throw new Error('Không tạo được template');
	return mapTemplateOutput({
		name,
		remark,
		status: StatusEnum.NUMBER_1,
		items,
		...template,
	});
};

export const saveCctvDashboardItems = async (dashboardId: string, cameras: VideoMonitoringItem[], options: { omitIds?: boolean } = {}) => {
	const items = cameras
		.filter((camera) => !camera.isPlaceholder)
		.map((camera) => ({
			id: options.omitIds || camera.id.startsWith('dashboard-camera-') ? undefined : camera.id,
			equipmentId: camera.sourceCameraId || camera.id,
			ip: camera.ip || undefined,
			x: camera.x,
			y: camera.y,
			w: camera.w,
			h: camera.h,
		}));
	await (getAPI(DashboardItemApi as any) as any).apiCctvDashboarditemUpdatePost({ dashboardId, items }, cctvRequestOptions);
};

export const createCctvDashboardItems = async (dashboardId: string, cameras: VideoMonitoringItem[]) => {
	const items = cameras
		.filter((camera) => !camera.isPlaceholder)
		.map((camera) => ({
			equipmentId: camera.sourceCameraId || camera.id,
			ip: camera.ip || undefined,
			x: camera.x,
			y: camera.y,
			w: camera.w,
			h: camera.h,
		}));
	await (getAPI(DashboardItemApi as any) as any).apiCctvDashboarditemAddPost({ dashboardId, items }, cctvRequestOptions);
};

export const getCctvStreamSource = async (
	camera: VideoMonitoringItem,
	options: {
		stream?: string;
		resolution?: string;
		fromDate?: Date;
	} = {}
) => {
	const { deviceId, ip } = requireCameraKeys(camera);
	const result = await (getAPI(CctvMediaApi as any) as any).apiCctvCctvmediaWebrtcstreamurlGet(
		deviceId,
		ip,
		options.stream || 'primary',
		options.resolution || '1920x1080',
		options.fromDate,
		getCctvVendor(),
		cctvRequestOptions
	);
	const output = result.data?.result ?? result.data;
	// Hỗ trợ phản hồi URL của BE cũ trong lúc nâng cấp FE/BE.
	if (typeof output === 'string') return { url: output } as CctvWebRtcStreamOutput;
	return (output ?? {}) as CctvWebRtcStreamOutput;
};

/** Chỉ dùng cho player CCTV. URL RTSP do BE trả phải đi qua LiveCam/relay. */
export const getCctvWebRtcStreamUrl = async (
	camera: VideoMonitoringItem,
	options: { stream?: string; resolution?: string; fromDate?: Date } = {}
) => {
	const source = await getCctvStreamSource(camera, options);
	const url = safeString(source.url).trim();
	if (source.source === 'rtsp' || !/^wss?:\/\//i.test(url)) throw new Error('Không lấy được URL stream CCTV');
	return url;
};

/**
 * URL stream do BE trả đi qua Cctv Proxy, kèm `ticket` có hạn (base64url(JSON{ e: unix giây }) + '.' + chữ ký).
 * Trả true nếu ticket đã/sắp hết hạn (trong `skewSeconds`) để FE xin URL mới trước khi kết nối.
 * URL không có ticket hoặc không đọc được -> false (để server tự quyết định).
 */
export const isCctvStreamUrlExpired = (url: string, skewSeconds = 30) => {
	try {
		const ticket = new URL(url).searchParams.get('ticket');
		const payloadPart = ticket?.split('.')[0];
		if (!payloadPart) return false;
		const base64 = payloadPart.replace(/-/g, '+').replace(/_/g, '/');
		const payload = JSON.parse(atob(base64.padEnd(base64.length + ((4 - (base64.length % 4)) % 4), '=')));
		const expiresAt = Number(payload?.e);
		return Number.isFinite(expiresAt) && expiresAt - skewSeconds <= Date.now() / 1000;
	} catch {
		return false;
	}
};

export const getCctvPlaybackStreamUrl = async (
	camera: VideoMonitoringItem,
	fromDate: Date,
	options: { stream?: string; resolution?: string } = {}
) =>
	getCctvWebRtcStreamUrl(camera, {
		stream: options.stream || 'primary',
		resolution: options.resolution || '1920x1080',
		fromDate,
	});

export interface CctvRecordingPeriod {
	serverId: string;
	startTimeMs: number;
	durationMs?: number;
}

export const loadCctvRecordingPeriods = async (
	camera: VideoMonitoringItem,
	options: {
		fromDate: Date;
		toDate: Date;
		detailLevelMs?: number;
		periodType?: string;
	}
): Promise<CctvRecordingPeriod[]> => {
	const { deviceId, ip } = getCameraKeys(camera);
	if (!deviceId && !ip) return [];
	const result = await (getAPI(CctvMediaApi as any) as any).apiCctvCctvmediaFootageGet(
		deviceId,
		ip,
		options.fromDate,
		options.toDate,
		options.detailLevelMs,
		options.periodType,
		getCctvVendor(),
		cctvRequestOptions
	);
	const footage = result.data?.result;
	return asArray(footage?.items).map((item: any) => ({
		serverId: safeString(item.deviceId || deviceId),
		startTimeMs: safeNumber(item.startTimeMs, 0),
		durationMs: item.durationMs ? safeNumber(item.durationMs, 0) : undefined,
	})).filter((p: CctvRecordingPeriod) => p.startTimeMs > 0);
};

export const getCctvMediaDownloadUrl = async (
	camera: VideoMonitoringItem,
	options: {
		fromDate: Date;
		toDate: Date;
		stream?: string;
		resolution?: string;
		videoCodec?: string;
	}
) => {
	const { deviceId, ip } = requireCameraKeys(camera);
	const result = await (getAPI(CctvMediaApi as any) as any).apiCctvCctvmediaMediadownloadurlGet(
		deviceId,
		ip,
		options.stream || 'primary',
		options.resolution || '1920x1080',
		options.fromDate,
		options.toDate,
		options.videoCodec,
		getCctvVendor(),
		cctvRequestOptions
	);
	const url = safeString(result.data?.result?.downloadUrl || result.data?.result?.url || result.data?.result || result.data?.downloadUrl || result.data?.url);
	if (!url) throw new Error('Không lấy được URL tải video');
	return url;
};

export const getCctvSnapshotUrl = async (
	camera: VideoMonitoringItem,
	options: {
		fromDate?: Date;
		size?: string;
		format?: string;
		crop?: string;
		rotation?: number;
	} = {}
) => {
	const { deviceId, ip } = requireCameraKeys(camera);
	const response = await cctvFetchRaw('/api/cctv/cctvmedia/snapshot', {
		DeviceId: deviceId,
		Ip: ip,
		FromDate: options.fromDate,
		Size: options.size || '1920x1080',
		Format: options.format || 'jpg',
		Crop: options.crop,
		Rotation: options.rotation,
		Vendor: getCctvVendor(),
	});
	const contentType = response.headers.get('content-type') || '';
	if (contentType.includes('image/') || contentType.includes('application/octet-stream')) {
		const blob = await response.blob();
		return URL.createObjectURL(blob);
	}
	const text = await response.text();
	let result: any = null;
	try {
		result = JSON.parse(text);
	} catch {
		const directUrl = text.trim();
		if (/^https?:\/\//i.test(directUrl) || /^blob:/i.test(directUrl)) {
			return directUrl;
		}
		throw new Error(`Snapshot response không phải JSON/ảnh hợp lệ: ${directUrl.slice(0, 160)}`);
	}
	const code = safeNumber(result?.code, 200);
	if (code !== 200) {
		throw new Error(safeString(result?.message, 'Không thể chụp snapshot'));
	}
	const snapshotResult = result?.result || {};
	const url = safeString(snapshotResult?.url || snapshotResult?.downloadUrl || snapshotResult?.snapshotUrl || result?.result || result?.url || result?.downloadUrl);
	if (!url) {
		throw new Error('Không lấy được URL ảnh snapshot');
	}
	return url;
};

const getCameraPtzIdentity = (camera: VideoMonitoringItem) => {
	const { deviceId, ip } = requireCameraKeys(camera);
	return {
		deviceId,
		ip,
		vendor: getCctvVendor(),
	};
};

const getPtzDirection = (vector: { panSpeed?: number; tiltSpeed?: number }) => {
	const pan = safeNumber(vector.panSpeed, 0);
	const tilt = safeNumber(vector.tiltSpeed, 0);
	if (pan < 0 && tilt > 0) return PtzDirection.NUMBER_5;
	if (pan > 0 && tilt > 0) return PtzDirection.NUMBER_6;
	if (pan < 0 && tilt < 0) return PtzDirection.NUMBER_7;
	if (pan > 0 && tilt < 0) return PtzDirection.NUMBER_8;
	if (pan < 0) return PtzDirection.NUMBER_1;
	if (pan > 0) return PtzDirection.NUMBER_2;
	if (tilt > 0) return PtzDirection.NUMBER_3;
	if (tilt < 0) return PtzDirection.NUMBER_4;
	return undefined;
};

const getPtzSpeed = (...values: Array<number | undefined>) => Math.max(1, ...values.map((value) => Math.abs(safeNumber(value, 0))));

export const moveCctvPtz = async (
	camera: VideoMonitoringItem,
	vector: {
		panSpeed?: number;
		tiltSpeed?: number;
		zoomSpeed?: number;
	}
) => {
	const identity = getCameraPtzIdentity(camera);
	const ptzDirection = getPtzDirection(vector);
	const zoomSpeed = safeNumber(vector.zoomSpeed, 0);
	const ptzApi = getAPI(CctvPtzApi as any) as any;
	if (ptzDirection !== undefined) {
		await ptzApi.apiCctvCctvptzMoveptzPost(
			{
				...identity,
				direction: ptzDirection,
				speed: getPtzSpeed(vector.panSpeed, vector.tiltSpeed),
			},
			cctvRequestOptions
		);
	}
	if (zoomSpeed !== 0) {
		await ptzApi.apiCctvCctvptzZoomptzPost(
			{
				...identity,
				direction: zoomSpeed > 0 ? ZoomDirection.NUMBER_1 : ZoomDirection.NUMBER_2,
				speed: getPtzSpeed(zoomSpeed),
			},
			cctvRequestOptions
		);
	}
};

export const stopCctvPtz = async (camera: VideoMonitoringItem) => {
	await (getAPI(CctvPtzApi as any) as any).apiCctvCctvptzStopptzPost(getCameraPtzIdentity(camera), cctvRequestOptions);
};

export const loadCctvPtzPresets = async (camera: VideoMonitoringItem) => {
	const identity = getCameraPtzIdentity(camera);
	const result = await (getAPI(CctvPtzApi as any) as any).apiCctvCctvptzGetpresetsGet(identity.deviceId, identity.ip, identity.vendor, cctvRequestOptions);
	return asArray<CctvPtzPresetOutput>(result.data?.result).map((preset) => ({
		id: safeString(preset.presetId),
		name: safeString(preset.name || preset.presetId),
		description: safeString(preset.presetId),
	})).filter((preset) => Boolean(preset.id));
};

export const activateCctvPtzPreset = async (camera: VideoMonitoringItem, presetId: string) => {
	await (getAPI(CctvPtzApi as any) as any).apiCctvCctvptzActivatepresetPost(
		{
			...getCameraPtzIdentity(camera),
			presetId,
		},
		cctvRequestOptions
	);
};

export const deleteCctvPtzPreset = async (camera: VideoMonitoringItem, presetId: string) => {
	await (getAPI(CctvPtzApi as any) as any).apiCctvCctvptzDeletepresetPost(
		{
			...getCameraPtzIdentity(camera),
			presetId,
		},
		cctvRequestOptions
	);
};

export const addCctvPtzPreset = async (camera: VideoMonitoringItem, presetName: string) => {
	const result = await (getAPI(CctvPtzApi as any) as any).apiCctvCctvptzAddpresetPost(
		{
			...getCameraPtzIdentity(camera),
			presetName,
		},
		cctvRequestOptions
	);
	return result.data?.result;
};

export const nudgeCctvPtz = async (
	camera: VideoMonitoringItem,
	direction: PtzDirection,
	options: { isHold?: boolean } = {}
) => {
	await (getAPI(CctvPtzApi as any) as any).apiCctvCctvptzNudgeptzPost(
		{
			...getCameraPtzIdentity(camera),
			direction,
			isHold: options.isHold ?? false,
		},
		cctvRequestOptions
	);
};

export const zoomCctvPtz = async (
	camera: VideoMonitoringItem,
	direction: ZoomDirection,
	options: { isHold?: boolean } = {}
) => {
	await (getAPI(CctvPtzApi as any) as any).apiCctvCctvptzZoomptzPost(
		{
			...getCameraPtzIdentity(camera),
			direction,
			isHold: options.isHold ?? false,
		},
		cctvRequestOptions
	);
};

export const focusCctvPtz = async (
	camera: VideoMonitoringItem,
	direction: 1 | 2
) => {
	await (getAPI(CctvPtzApi as any) as any).apiCctvCctvptzFocusptzPost(
		{
			...getCameraPtzIdentity(camera),
			direction,
		},
		cctvRequestOptions
	);
};
