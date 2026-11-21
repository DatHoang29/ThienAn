/**
 * Định nghĩa metric telemetry FMS và các hàm định dạng hiển thị.
 * Mỗi cột JSON trong DB (resourceMetric / environmentMetric / diagnosticMetric) chứa một nhóm thông số.
 * Dịch vụ giám sát (DeviceMonitorService qua Zabbix) chuẩn hoá thông số thành bộ KEY PHẲNG cố định,
 * giá trị ĐÃ KÈM ĐƠN VỊ sẵn (vd "43 %", "4 core", "1024 B"); FE chỉ map key -> tên + icon để hiển thị.
 * Dùng chung cho màn danh sách (index.vue) và dialog chi tiết (detailTelemetry.vue).
 * Author: ChuongLP
 */

/** Hàm dịch i18n tối giản (nhận key trả về chuỗi) */
export type TFunc = (key: string) => string;

export interface MetricDef {
	/** property key trong JSON lưu ở DB (khớp key dịch vụ giám sát ghi ra) */
	key: string;
	/** i18n key tên hiển thị */
	labelKey: string;
	/** class icon FontAwesome 4 (không kèm tiền tố 'fa ') */
	icon?: string;
	/** kiểu định dạng đặc biệt: iface -> trạng thái cổng mạng (0/1/2) */
	kind?: 'iface';
}

export interface MetricColumn {
	/** tên cột trên bản ghi telemetry */
	col: 'resourceMetric' | 'environmentMetric' | 'diagnosticMetric';
	/** i18n key tiêu đề nhóm/cột */
	labelKey: string;
	metrics: MetricDef[];
}

/**
 * Cấu hình 3 cột và danh sách thông số của từng cột.
 * KEY phải khớp CHÍNH XÁC với bộ key do ZabbixMonitorService.BuildNormalizedMetrics ghi ra DB:
 *   resource:    cpuUsedPercent, cpuCores, memoryUsedPercent, memoryTotal, memoryAvailable, diskUsedPercent
 *   environment: temperature, fanSpeed, powerStatus
 *   diagnostic:  uptime, netInErrors, netOutErrors, interfaceAvailable
 */
export const METRIC_COLUMNS: MetricColumn[] = [
	{
		col: 'resourceMetric',
		labelKey: 'lz.label.vmsDeviceStatus.resource',
		metrics: [
			{ key: 'cpuUsedPercent', labelKey: 'lz.entity.vmsDeviceStatus.cpuUsedPercent', icon: 'fa-microchip' },
			{ key: 'cpuCores', labelKey: 'lz.entity.vmsDeviceStatus.cpuCores', icon: 'fa-cogs' },
			{ key: 'memoryUsedPercent', labelKey: 'lz.entity.vmsDeviceStatus.memoryUsedPercent', icon: 'fa-server' },
			{ key: 'memoryTotal', labelKey: 'lz.entity.vmsDeviceStatus.memoryTotal', icon: 'fa-database' },
			{ key: 'memoryAvailable', labelKey: 'lz.entity.vmsDeviceStatus.memoryAvailable', icon: 'fa-database' },
			{ key: 'diskUsedPercent', labelKey: 'lz.entity.vmsDeviceStatus.diskUsedPercent', icon: 'fa-hdd-o' },
		],
	},
	{
		col: 'environmentMetric',
		labelKey: 'lz.label.vmsDeviceStatus.environment',
		metrics: [
			{ key: 'temperature', labelKey: 'lz.entity.vmsDeviceStatus.temperature', icon: 'fa-thermometer-half' },
			{ key: 'fanSpeed', labelKey: 'lz.entity.vmsDeviceStatus.fanSpeed', icon: 'fa-refresh' },
			{ key: 'powerStatus', labelKey: 'lz.entity.vmsDeviceStatus.powerStatus', icon: 'fa-plug' },
		],
	},
	{
		col: 'diagnosticMetric',
		labelKey: 'lz.label.vmsDeviceStatus.diagnostic',
		metrics: [
			{ key: 'uptime', labelKey: 'lz.entity.vmsDeviceStatus.uptime', icon: 'fa-clock-o' },
			{ key: 'netInErrors', labelKey: 'lz.entity.vmsDeviceStatus.netInErrors', icon: 'fa-arrow-down' },
			{ key: 'netOutErrors', labelKey: 'lz.entity.vmsDeviceStatus.netOutErrors', icon: 'fa-arrow-up' },
			{ key: 'interfaceAvailable', labelKey: 'lz.entity.vmsDeviceStatus.interfaceAvailable', icon: 'fa-exchange', kind: 'iface' },
		],
	},
];

/** Tra cứu nhanh định nghĩa metric theo key */
const METRIC_INDEX: Record<string, MetricDef> = {};
METRIC_COLUMNS.forEach((c) => c.metrics.forEach((m) => { if (!METRIC_INDEX[m.key]) METRIC_INDEX[m.key] = m; }));

/** Parse an toàn một cột JSON -> object phẳng { key: value } */
export function parseColumnJson(raw: any): Record<string, any> {
	if (!raw) return {};
	if (typeof raw === 'object') return raw as Record<string, any>;
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

/**
 * Định dạng giá trị metric để hiển thị.
 * Giá trị do dịch vụ giám sát ghi ra ĐÃ KÈM ĐƠN VỊ nên KHÔNG append đơn vị lần nữa.
 * - interfaceAvailable (kind='iface'): 1 -> Kết nối, 2 -> Mất kết nối, khác -> Không rõ.
 * - Giá trị lồng nhau (object/array) từ bản ghi cũ: stringify gọn thay vì để "[object Object]".
 */
export function formatMetricValue(key: string, value: any, t: TFunc): string {
	if (value === undefined || value === null || value === '') return '-';

	const def = METRIC_INDEX[key];

	if (def?.kind === 'iface') {
		const s = String(value).trim();
		if (s === '1') return t('lz.label.vmsDeviceStatus.online');
		if (s === '2') return t('lz.label.vmsDeviceStatus.offline');
		return t('lz.label.vmsDeviceStatus.unknown');
	}

	// Dữ liệu cũ có thể là object/array lồng nhau -> tránh in ra "[object Object]"
	if (typeof value === 'object') {
		try {
			return JSON.stringify(value);
		} catch {
			return '-';
		}
	}

	return `${value}`;
}

/** Sắp xếp key theo đúng thứ tự cấu hình; key lạ (không có trong cấu hình) đưa xuống cuối */
function orderedKeys(obj: Record<string, any>, col: MetricColumn['col']): string[] {
	const ordered = METRIC_COLUMNS.find((c) => c.col === col)?.metrics.map((m) => m.key) ?? [];
	return [...ordered.filter((k) => k in obj), ...Object.keys(obj).filter((k) => !ordered.includes(k))];
}

/** Field CÓ dữ liệu để hiển thị (bỏ null/undefined/chuỗi rỗng) -> tránh hiện icon trống gây rối mắt */
const hasValue = (v: any) => v !== null && v !== undefined && !(typeof v === 'string' && v.trim() === '');

/** Item icon để render trên lưới: icon + giá trị + tooltip (title) + màu theo trạng thái */
export interface MetricIconItem {
	icon: string;   // class FA4 (kèm 'fa-...')
	title: string;  // tên đầy đủ (hiển thị khi hover)
	value: string;  // giá trị đã định dạng
	color: string;  // màu icon (CSS var Element Plus)
}

/**
 * Danh sách item dạng ICON cho MỘT cột JSON (dùng trên lưới thay cho text dài).
 * interfaceAvailable đổi màu theo trạng thái cổng; metric thường icon màu primary.
 */
export function getColumnIconItems(raw: any, col: MetricColumn['col'], t: TFunc): MetricIconItem[] {
	const obj = parseColumnJson(raw);
	return orderedKeys(obj, col).filter((k) => hasValue(obj[k])).map((k) => {
		const def = METRIC_INDEX[k];
		let icon = def?.icon ?? 'fa-info-circle';
		let color = 'var(--el-color-primary)';

		if (def?.kind === 'iface') {
			const s = String(obj[k] ?? '').trim();
			if (s === '1') { icon = 'fa-link'; color = 'var(--el-color-success)'; }
			else if (s === '2') { icon = 'fa-unlink'; color = 'var(--el-color-danger)'; }
			else { icon = 'fa-question-circle'; color = 'var(--el-text-color-secondary)'; }
		}

		return {
			icon,
			title: metricLabel(k, t),
			value: k === 'uptime' ? formatUptime(obj[k], t) : formatMetricValue(k, obj[k], t),
			color,
		};
	});
}

/**
 * Danh sách hiển thị dạng CHỮ của MỘT cột JSON, "Tên: giá trị, ..." (ngăn cách bởi dấu phẩy), KHÔNG icon.
 * - opts.excludeKeys: bỏ qua các key không muốn hiển thị (vd tách uptime ra cột riêng).
 * - uptime được định dạng ngày/giờ/phút/giây.
 */
export function formatColumnList(raw: any, col: MetricColumn['col'], t: TFunc, opts?: { excludeKeys?: string[] }): string {
	const obj = parseColumnJson(raw);
	let keys = orderedKeys(obj, col).filter((k) => hasValue(obj[k]));
	if (opts?.excludeKeys?.length) keys = keys.filter((k) => !opts.excludeKeys!.includes(k));
	if (!keys.length) return '-';
	return keys.map((k) => `${metricLabel(k, t)}: ${k === 'uptime' ? formatUptime(obj[k], t) : formatMetricValue(k, obj[k], t)}`).join(', ');
}

/** Lấy giá trị GỐC của 1 metric theo key trong một cột JSON. */
export function getMetricRaw(raw: any, key: string): any {
	return parseColumnJson(raw)[key];
}

/** Lấy % (số) của 1 metric theo key (dùng tô màu/thanh tiến trình). Null nếu không có. */
export function getMetricPercent(raw: any, key: string): number | null {
	return parseMetricNumber(getMetricRaw(raw, key));
}

/** Định dạng giá trị 1 metric theo key (uptime -> d/h/m/s, iface -> trạng thái). '-' nếu trống. */
export function formatMetricByKey(raw: any, key: string, t: TFunc): string {
	const v = getMetricRaw(raw, key);
	if (!hasValue(v)) return '-';
	return key === 'uptime' ? formatUptime(v, t) : formatMetricValue(key, v, t);
}

/** Trả về danh sách item {label, value} của một cột (dùng cho dialog chi tiết) */
export function getColumnItems(raw: any, col: MetricColumn['col'], t: TFunc): { label: string; value: string }[] {
	const obj = parseColumnJson(raw);
	return orderedKeys(obj, col)
		.filter((k) => hasValue(obj[k]))
		.map((k) => ({ label: metricLabel(k, t), value: k === 'uptime' ? formatUptime(obj[k], t) : formatMetricValue(k, obj[k], t) }));
}

/* ============================================================================
 * BỔ SUNG cho giao diện DẠNG THẺ (card) theo từng thiết bị.
 * Author: ChuongLP
 * ==========================================================================*/

/** Trạng thái kết nối suy ra từ bản ghi telemetry mới nhất của thiết bị. */
export type ConnStateKey = 'online' | 'stale' | 'offline' | 'nodata';

export interface ConnState {
	/** online: có dữ liệu mới & mới nhất | stale: thành công nhưng dữ liệu đã cũ (>ngưỡng) | offline: lần lấy mới nhất lỗi | nodata: chưa từng có telemetry */
	state: ConnStateKey;
	/** true nếu mốc thời gian cần tô đỏ + chấm cảnh báo (stale hoặc offline) */
	isStale: boolean;
	/** thời điểm bắt đầu mất kết nối (chỉ có với offline) */
	since?: string | Date | null;
}

/**
 * Suy ra trạng thái kết nối của thiết bị từ bản ghi telemetry mới nhất.
 * - Chưa có telemetry -> nodata.
 * - Bản ghi mới nhất lỗi (succeeded=false) -> offline (kèm mốc bắt đầu mất kết nối).
 * - Thành công nhưng không cập nhật > ngưỡng (mặc định 1 giờ) -> stale (dữ liệu cũ).
 * - Còn lại -> online.
 */
export function getConnState(row: any, staleThresholdMs = 60 * 60 * 1000): ConnState {
	if (!row || row.collectedTime == null) return { state: 'nodata', isStale: false };

	if (row.succeeded === false) {
		return { state: 'offline', isStale: true, since: row.disconnectedSince ?? row.collectedTime };
	}

	const collected = new Date(row.collectedTime).getTime();
	if (!isNaN(collected) && Date.now() - collected > staleThresholdMs) {
		return { state: 'stale', isStale: true };
	}
	return { state: 'online', isStale: false };
}

/** Trích số (float) đứng đầu chuỗi giá trị đã kèm đơn vị (vd "38.9 %" -> 38.9). Trả null nếu không có số. */
export function parseMetricNumber(value: any): number | null {
	if (value === null || value === undefined || value === '') return null;
	if (typeof value === 'number') return isNaN(value) ? null : value;
	const m = String(value).match(/-?\d+(?:[.,]\d+)?/);
	if (!m) return null;
	const n = Number(m[0].replace(',', '.'));
	return isNaN(n) ? null : n;
}

/**
 * Định dạng UPTIME: DB lưu theo GIÂY (giá trị dạng "12345 s").
 * - < 1 phút: giây
 * - < 1 giờ:  phút + giây
 * - < 1 ngày: giờ + phút + giây
 * - >= 1 ngày: ngày + giờ + phút + giây
 */
export function formatUptime(raw: any, t: TFunc): string {
	const total = parseMetricNumber(raw);
	if (total === null) return raw ? String(raw) : '-';

	let sec = Math.floor(total);
	if (sec < 0) sec = 0;

	const days = Math.floor(sec / 86400);
	const hours = Math.floor((sec % 86400) / 3600);
	const minutes = Math.floor((sec % 3600) / 60);
	const seconds = sec % 60;

	const dU = t('lz.label.vmsDeviceStatus.unitDay');
	const hU = t('lz.label.vmsDeviceStatus.unitHour');
	const mU = t('lz.label.vmsDeviceStatus.unitMinute');
	const sU = t('lz.label.vmsDeviceStatus.unitSecond');

	if (days > 0) return `${days} ${dU} ${hours} ${hU} ${minutes} ${mU} ${seconds} ${sU}`;
	if (hours > 0) return `${hours} ${hU} ${minutes} ${mU} ${seconds} ${sU}`;
	if (minutes > 0) return `${minutes} ${mU} ${seconds} ${sU}`;
	return `${seconds} ${sU}`;
}

/** Màu theo mức % sử dụng tài nguyên: <70 an toàn, 70-89 cảnh báo, >=90 nguy hiểm. */
export function usageColor(percent: number | null): string {
	if (percent === null) return 'var(--el-color-info)';
	if (percent >= 90) return 'var(--el-color-danger)';
	if (percent >= 70) return 'var(--el-color-warning)';
	return 'var(--el-color-success)';
}

/** Các key % sử dụng -> hiển thị bằng thanh tiến trình trên thẻ. */
const PERCENT_KEYS = new Set(['cpuUsedPercent', 'memoryUsedPercent', 'diskUsedPercent']);

/** Item hiển thị chi tiết trên THẺ cho MỘT cột metric. */
export interface CardMetricItem {
	key: string;
	label: string;
	value: string; // giá trị đã định dạng (uptime -> d/h/m/s, iface -> trạng thái)
	icon: string;
	color: string;
	/** % (0..100) nếu là metric dạng thanh tiến trình, ngược lại null */
	percent: number | null;
	/** giá trị gốc phụ trợ hiển thị dưới nhãn (vd tổng RAM) */
	sub?: string;
}

/**
 * Danh sách item để render trên THẺ cho MỘT cột JSON.
 * - metric % (cpu/ram/disk): kèm percent để vẽ thanh tiến trình.
 * - uptime: định dạng theo ngày/giờ/phút/giây.
 * - interfaceAvailable: trạng thái cổng mạng + màu.
 */
export function getCardMetricItems(raw: any, col: MetricColumn['col'], t: TFunc): CardMetricItem[] {
	const obj = parseColumnJson(raw);
	return orderedKeys(obj, col)
		.filter((k) => hasValue(obj[k]))
		.map((k) => {
			const def = METRIC_INDEX[k];
			let icon = def?.icon ?? 'fa-info-circle';
			let color = 'var(--el-color-primary)';
			let value: string;
			let percent: number | null = null;

			if (k === 'uptime') {
				value = formatUptime(obj[k], t);
			} else if (def?.kind === 'iface') {
				value = formatMetricValue(k, obj[k], t);
				const s = String(obj[k] ?? '').trim();
				if (s === '1') {
					icon = 'fa-link';
					color = 'var(--el-color-success)';
				} else if (s === '2') {
					icon = 'fa-unlink';
					color = 'var(--el-color-danger)';
				} else {
					icon = 'fa-question-circle';
					color = 'var(--el-text-color-secondary)';
				}
			} else {
				value = formatMetricValue(k, obj[k], t);
			}

			if (PERCENT_KEYS.has(k)) {
				percent = parseMetricNumber(obj[k]);
				color = usageColor(percent);
			}

			return { key: k, label: metricLabel(k, t), value, icon, color, percent };
		});
}

/** Có ít nhất một field dữ liệu trong cột không (để quyết định hiện chấm báo trên tab). */
export function columnHasData(raw: any, col: MetricColumn['col']): boolean {
	const obj = parseColumnJson(raw);
	return orderedKeys(obj, col).some((k) => hasValue(obj[k]));
}

/** Rút gọn số GB: bỏ số 0 thừa (7.870 -> "7.87", 8.0 -> "8"). */
function shortGb(n: number | null): string | null {
	if (n === null) return null;
	return String(Math.round(n * 100) / 100);
}

/**
 * 3 THANH TÀI NGUYÊN chính cho THẺ: CPU / RAM / Disk (giống mẫu).
 * - Chỉ gồm 3 metric % (gọn gàng), bỏ các dòng phụ (số nhân, tổng, còn trống).
 * - RAM kèm chú thích "(còn trống/tổng G)" để biết dung lượng mà vẫn gọn.
 */
export function getResourceBars(raw: any, t: TFunc): CardMetricItem[] {
	const obj = parseColumnJson(raw);
	const bars: CardMetricItem[] = [];

	const push = (key: string, label: string, sub?: string) => {
		if (!hasValue(obj[key])) return;
		const percent = parseMetricNumber(obj[key]);
		bars.push({
			key,
			label,
			value: formatMetricValue(key, obj[key], t),
			icon: '',
			color: usageColor(percent),
			percent,
			sub,
		});
	};

	push('cpuUsedPercent', 'CPU');

	// RAM: (còn trống/tổng G)
	const avail = shortGb(parseMetricNumber(obj['memoryAvailable']));
	const total = shortGb(parseMetricNumber(obj['memoryTotal']));
	let ramSub: string | undefined;
	if (avail && total) ramSub = `(${avail}/${total}G)`;
	else if (total) ramSub = `(${total}G)`;
	push('memoryUsedPercent', 'RAM', ramSub);

	push('diskUsedPercent', 'Disk');

	return bars;
}

/** Một VÒNG TÀI NGUYÊN trên thẻ: icon ở giữa, % sử dụng, chú thích phụ và tooltip đầy đủ. */
export interface ResourceRingItem {
	key: string;
	/** nhãn ngắn dưới vòng (CPU / RAM / Disk) */
	label: string;
	/** class icon FA4 (kèm 'fa-...') hiển thị giữa vòng */
	icon: string;
	/** % (0..100) hoặc null nếu giá trị không đọc được số */
	percent: number | null;
	/** giá trị gốc đã định dạng (vd "38.9 %") */
	value: string;
	/** màu theo ngưỡng sử dụng (usageColor) */
	color: string;
	/** chú thích phụ dưới nhãn (vd "4 core", "2.1/7.87G") */
	sub?: string;
	/** true khi >= 90% -> hiện biểu tượng cảnh báo */
	warn: boolean;
	/** các dòng tooltip "Tên: giá trị" */
	tooltip: string[];
}

/**
 * 3 VÒNG TÀI NGUYÊN cho THẺ (thay cho 3 thanh): CPU / RAM / Disk.
 * Dữ liệu giống getResourceBars, bổ sung icon, số nhân CPU, dung lượng RAM và tooltip đầy đủ.
 */
export function getResourceRings(raw: any, t: TFunc): ResourceRingItem[] {
	const obj = parseColumnJson(raw);
	const rings: ResourceRingItem[] = [];
	const line = (k: string) => (hasValue(obj[k]) ? `${metricLabel(k, t)}: ${formatMetricValue(k, obj[k], t)}` : null);

	const push = (key: string, label: string, icon: string, sub: string | undefined, extraKeys: string[]) => {
		if (!hasValue(obj[key])) return;
		const percent = parseMetricNumber(obj[key]);
		rings.push({
			key,
			label,
			icon,
			percent,
			value: formatMetricValue(key, obj[key], t),
			color: usageColor(percent),
			sub,
			warn: percent !== null && percent >= 90,
			tooltip: [key, ...extraKeys].map(line).filter((l): l is string => !!l),
		});
	};

	push('cpuUsedPercent', 'CPU', 'fa-microchip', hasValue(obj['cpuCores']) ? String(obj['cpuCores']) : undefined, ['cpuCores']);

	// RAM: "còn trống/tổng G" giống thanh RAM cũ
	const avail = shortGb(parseMetricNumber(obj['memoryAvailable']));
	const total = shortGb(parseMetricNumber(obj['memoryTotal']));
	const ramSub = avail && total ? `${avail}/${total}G` : total ? `${total}G` : undefined;
	push('memoryUsedPercent', 'RAM', 'fa-server', ramSub, ['memoryAvailable', 'memoryTotal']);

	push('diskUsedPercent', 'Disk', 'fa-hdd-o', undefined, []);

	return rings;
}
