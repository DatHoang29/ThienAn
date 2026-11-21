/**
 * Cấu hình hiển thị zone cấp bản đồ (lưu cùng config bản đồ, áp dụng mọi màn hình):
 * - zoneBorderWidth: độ dày viền zone ở màn giám sát (px).
 * - flowLineWidth: độ dày đường định hướng (px).
 * - flowLineColors: màu đường định hướng (flow-line) THEO HƯỚNG — khoá là mã hướng
 *   (zone.laneId, danh mục TmsDirectionType), khoá FLOW_LINE_ALL_KEY là màu mặc định
 *   cho mọi hướng chưa đặt màu riêng / zone chưa gán hướng.
 */

export const ZONE_BORDER_WIDTH_DEFAULT = 1.5;
export const ZONE_BORDER_WIDTH_MAX = 10;

export const FLOW_LINE_WIDTH_DEFAULT = 2;
export const FLOW_LINE_WIDTH_MIN = 1;
export const FLOW_LINE_WIDTH_MAX = 6;

/** Độ dày đường định hướng hợp lệ 1–6px; trống/sai kiểu = mặc định 2px. */
export const resolveFlowLineWidth = (value: unknown): number => {
	if (value === null || value === undefined || value === '') return FLOW_LINE_WIDTH_DEFAULT;
	const width = Number(value);
	if (!Number.isFinite(width) || width <= 0) return FLOW_LINE_WIDTH_DEFAULT;
	return Math.min(Math.max(width, FLOW_LINE_WIDTH_MIN), FLOW_LINE_WIDTH_MAX);
};

/** Khoá màu mặc định cho mọi hướng. */
export const FLOW_LINE_ALL_KEY = '*';

export type FlowLineColors = Record<string, string>;

/** Độ dày viền hợp lệ 0–10px; trống/sai kiểu = mặc định 1.5px; 0 = ẩn viền. */
export const resolveZoneBorderWidth = (value: unknown): number => {
	if (value === null || value === undefined || value === '') return ZONE_BORDER_WIDTH_DEFAULT;
	const width = Number(value);
	if (!Number.isFinite(width) || width < 0) return ZONE_BORDER_WIDTH_DEFAULT;
	return Math.min(width, ZONE_BORDER_WIDTH_MAX);
};

/** Chỉ nhận mã màu #hex hoặc rgb()/rgba() (dữ liệu có thể đến từ file nhập ngoài). */
const isColorValue = (value: string): boolean => /^#[0-9a-f]{3,8}$/i.test(value) || /^rgba?\([\d\s.,%]+\)$/i.test(value);

/** Chuẩn hoá map màu theo hướng: bỏ khoá rỗng, giá trị không phải mã màu. */
export const normalizeFlowLineColors = (value: unknown): FlowLineColors => {
	if (!value || typeof value !== 'object' || Array.isArray(value)) return {};
	const result: FlowLineColors = {};
	Object.entries(value as Record<string, unknown>).forEach(([key, color]) => {
		const k = String(key).trim();
		const c = typeof color === 'string' ? color.trim() : '';
		if (k && c && isColorValue(c)) result[k] = c;
	});
	return result;
};

/**
 * Màu đường định hướng cho 1 hướng: màu riêng của hướng → màu mặc định mọi hướng → ''.
 * '' nghĩa là chưa cấu hình, nơi gọi tự dùng màu sẵn có của màn hình đó.
 */
export const resolveFlowLineColor = (colors: FlowLineColors | null | undefined, laneId: unknown): string => {
	if (!colors) return '';
	const key = laneId === null || laneId === undefined ? '' : String(laneId);
	return (key && colors[key]) || colors[FLOW_LINE_ALL_KEY] || '';
};
