/** Hàm dịch i18n tối giản (nhận key trả về chuỗi) */
export type TFunc = (key: string) => string;

export interface MetricDef {
	/** property key trong JSON lưu ở DB */
	key: string;
	/** i18n key tên hiển thị */
	labelKey: string;
	/** class icon FontAwesome 4 (không kèm tiền tố 'fa ') */
	icon?: string;
	/** đơn vị hiển thị kèm giá trị thô (nếu không thuộc kind đặc biệt) */
	unit?: string;
	/** kiểu định dạng đặc biệt: door -> mở/đóng, power -> kết nối/mất kết nối */
	kind?: 'door' | 'power';
}

export interface MetricColumn {
	/** tên cột trên bản ghi telemetry */
	col: 'resourceMetric' | 'environmentMetric' | 'diagnosticMetric';
	/** i18n key tiêu đề nhóm/cột */
	labelKey: string;
	metrics: MetricDef[];
}

/** Cấu hình 3 cột và danh sách thông số của từng cột (đúng thứ tự hiển thị mong muốn) */
export const METRIC_COLUMNS: MetricColumn[] = [
	{
		col: 'resourceMetric',
		labelKey: 'lz.label.vmsDeviceStatus.resource',
		metrics: [
			{ key: 'brightness', labelKey: 'lz.entity.vmsDeviceStatus.brightness', icon: 'fa-sun-o' },
			{ key: 'powerStatus', labelKey: 'lz.entity.vmsDeviceStatus.powerStatus', icon: 'fa-power-off', kind: 'power' },
		],
	},
	{
		col: 'environmentMetric',
		labelKey: 'lz.label.vmsDeviceStatus.environment',
		metrics: [
			{ key: 'temperatureIn', labelKey: 'lz.entity.vmsDeviceStatus.temperatureIn', icon: 'fa-thermometer-half', unit: '°C' },
			{ key: 'temperatureOut', labelKey: 'lz.entity.vmsDeviceStatus.temperatureOut', icon: 'fa-thermometer-quarter', unit: '°C' },
			{ key: 'humidityIn', labelKey: 'lz.entity.vmsDeviceStatus.humidityIn', icon: 'fa-tint', unit: '%' },
			{ key: 'humidityOut', labelKey: 'lz.entity.vmsDeviceStatus.humidityOut', icon: 'fa-tint', unit: '%' },
			{ key: 'windSpeed', labelKey: 'lz.entity.vmsDeviceStatus.windSpeed', icon: 'fa-tachometer', unit: 'm/s' },
			{ key: 'windDirection', labelKey: 'lz.entity.vmsDeviceStatus.windDirection', icon: 'fa-compass' },
			{ key: 'gradient', labelKey: 'lz.entity.vmsDeviceStatus.gradient', icon: 'fa-arrows-v' },
		],
	},
	{
		col: 'diagnosticMetric',
		labelKey: 'lz.label.vmsDeviceStatus.diagnostic',
		metrics: [
			{ key: 'pixelErrorCount', labelKey: 'lz.entity.vmsDeviceStatus.pixelErrorCount', icon: 'fa-th' },
			{ key: 'firmwareVersion', labelKey: 'lz.entity.vmsDeviceStatus.firmwareVersion', icon: 'fa-microchip' },
			{ key: 'doorOpen', labelKey: 'lz.entity.vmsDeviceStatus.door', icon: 'fa-lock', kind: 'door' },
		],
	},
];

/** Tra cứu nhanh định nghĩa metric theo key */
const METRIC_INDEX: Record<string, MetricDef> = {};
METRIC_COLUMNS.forEach((c) => c.metrics.forEach((m) => { if (!METRIC_INDEX[m.key]) METRIC_INDEX[m.key] = m; }));

/** Giá trị "1"/"true"/1 => bật/mở */
const isTruthy = (v: any) => v === 1 || v === '1' || v === true || v === 'true';

/** Parse an toàn một cột JSON -> object phẳng { key: value } */
export function parseColumnJson(raw: any): Record<string, any> {
	if (!raw) return {};
	try {
		const obj = JSON.parse(raw);
		return obj && typeof obj === 'object' ? obj : {};
	} catch {
		return {};
	}
}

/** Tên hiển thị của một metric theo key (fallback: trả về chính key nếu chưa định nghĩa) */
export function metricLabel(key: string, t: TFunc): string {
	const def = METRIC_INDEX[key];
	return def ? t(def.labelKey) : key;
}

/** Định dạng giá trị metric theo kiểu tương ứng */
export function formatMetricValue(key: string, value: any, t: TFunc): string {
	if (value === undefined || value === null || value === '') return '-';
	const def = METRIC_INDEX[key];
	if (def?.kind === 'door') return isTruthy(value) ? t('lz.label.vmsDeviceStatus.doorOpen') : t('lz.label.vmsDeviceStatus.doorClosed');
	if (def?.kind === 'power') return isTruthy(value) ? t('lz.label.vmsDeviceStatus.online') : t('lz.label.vmsDeviceStatus.offline');
	return def?.unit ? `${value} ${def.unit}` : `${value}`;
}

/** Sắp xếp key theo đúng thứ tự cấu hình; key lạ (không có trong cấu hình) đưa xuống cuối */
function orderedKeys(obj: Record<string, any>, col: MetricColumn['col']): string[] {
	const ordered = METRIC_COLUMNS.find((c) => c.col === col)?.metrics.map((m) => m.key) ?? [];
	return [...ordered.filter((k) => k in obj), ...Object.keys(obj).filter((k) => !ordered.includes(k))];
}

/** Item icon để render trên lưới: icon + giá trị + tooltip (title) + màu theo trạng thái */
export interface MetricIconItem {
	icon: string;   // class FA4 (kèm 'fa-...')
	title: string;  // tên đầy đủ (hiển thị khi hover)
	value: string;  // giá trị đã định dạng
	color: string;  // màu icon (CSS var Element Plus)
}

/**
 * Danh sách item dạng ICON cho MỘT cột JSON (dùng trên lưới thay cho text dài).
 * door/power đổi icon + màu theo trạng thái; metric thường icon màu primary.
 */
export function getColumnIconItems(raw: any, col: MetricColumn['col'], t: TFunc): MetricIconItem[] {
	const obj = parseColumnJson(raw);
	return orderedKeys(obj, col).map((k) => {
		const def = METRIC_INDEX[k];
		const on = isTruthy(obj[k]);
		let icon = def?.icon ?? 'fa-info-circle';
		let color = 'var(--el-color-primary)';

		if (def?.kind === 'door') {
			icon = on ? 'fa-unlock' : 'fa-lock';
			color = on ? 'var(--el-color-danger)' : 'var(--el-color-success)';
		} else if (def?.kind === 'power') {
			color = on ? 'var(--el-color-success)' : 'var(--el-text-color-secondary)';
		}

		return {
			icon,
			title: metricLabel(k, t),
			value: formatMetricValue(k, obj[k], t),
			color,
		};
	});
}

/**
 * Danh sách hiển thị dạng CHỮ của MỘT cột JSON, "Tên: giá trị, ..." (ngăn cách bởi dấu phẩy).
 * Giữ lại để dùng khi cần hiển thị dạng text.
 */
export function formatColumnList(raw: any, col: MetricColumn['col'], t: TFunc): string {
	const obj = parseColumnJson(raw);
	const keys = orderedKeys(obj, col);
	if (!keys.length) return '-';
	return keys.map((k) => `${metricLabel(k, t)}: ${formatMetricValue(k, obj[k], t)}`).join(', ');
}

/** Trả về danh sách item {label, value} của một cột (dùng cho dialog chi tiết) */
export function getColumnItems(raw: any, col: MetricColumn['col'], t: TFunc): { label: string; value: string }[] {
	const obj = parseColumnJson(raw);
	return orderedKeys(obj, col).map((k) => ({ label: metricLabel(k, t), value: formatMetricValue(k, obj[k], t) }));
}
