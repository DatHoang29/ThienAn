import type { VwScreenOutput } from '/@/api-services/videoWall';

/**
 * Hàm thuần cho tab Bố cục màn hình (trang Màn hình) và trang Bố cục tường.
 *
 * Bố cục: 1..MAX_BLOCKS khối xếp ngang, mỗi khối lưới cols × rows (cấu hình ở trang Bố cục tường).
 * Màn hình mang (blockNo, gridRow, gridCol) tính trong khối; cả 3 rỗng = nằm ở khay "ngoài lưới".
 */

/** Khối đang chỉnh trên giao diện. */
export interface LayoutBlock {
	count: number;
	cols: number;
	rows: number;
}

/** Màn hình đang chỉnh: `key` ổn định cho cả màn mới chưa có id. */
export type LayoutScreen = VwScreenOutput & {
	key: string;
	isNew?: boolean;
};

/** Ô trên lưới. */
export interface LayoutCell {
	block: number;
	row: number;
	col: number;
}

/** Kết quả kiểm tra bố cục: lỗi chặn lưu, cảnh báo chỉ nhắc. `keys` = màn liên quan (bấm để chọn). */
export interface LayoutIssue {
	level: 'error' | 'warning';
	text: string;
	keys?: string[];
}

/** Số khối tối đa — khớp BE VwWallProfile.MaxBlocks. */
export const MAX_BLOCKS = 8;

/** Tiền tố mã màn tạo từ ô trống — khớp BE VwWallProfile.ScreenCodePrefix. */
export const SCREEN_CODE_PREFIX = 'SCR-';

export const cellKey = (block: number, row: number, col: number) => `${block}:${row}:${col}`;

export const hasCell = (s: Pick<VwScreenOutput, 'blockNo' | 'gridRow' | 'gridCol'>) => s.blockNo != null && s.gridRow != null && s.gridCol != null;

/** Màn có nằm trong lưới của các khối không. */
export function isInGrid(s: Pick<VwScreenOutput, 'blockNo' | 'gridRow' | 'gridCol'>, blocks: LayoutBlock[]): boolean {
	if (!hasCell(s)) return false;
	const b = blocks[s.blockNo!];
	return !!b && s.gridRow! >= 0 && s.gridCol! >= 0 && s.gridRow! < b.rows && s.gridCol! < b.cols;
}

/** Kiểu dữ liệu kéo thả màn (key) giữa lưới và danh sách "Màn hình chưa ghép nối". */
export const SCREEN_DRAG_TYPE = 'text/vw-screen';

/**
 * Trải màn lên lưới theo (blockNo, gridRow, gridCol). Màn ngoài lưới, hoặc trùng ô với màn đứng trước
 * (lỗi dữ liệu), vào nhóm chưa ghép nối để người dùng thấy và xếp lại.
 */
export function splitPlacement(screens: LayoutScreen[], blocks: LayoutBlock[]) {
	const cells = new Map<string, LayoutScreen>();
	const tray: LayoutScreen[] = [];
	for (const s of screens) {
		if (!isInGrid(s, blocks)) {
			tray.push(s);
			continue;
		}
		const k = cellKey(s.blockNo!, s.gridRow!, s.gridCol!);
		if (cells.has(k)) tray.push(s);
		else cells.set(k, s);
	}
	return { cells, tray };
}

/** Thứ tự đọc: khối → hàng → cột; màn ngoài lưới xếp cuối theo mã. */
export function readingOrder(a: VwScreenOutput, b: VwScreenOutput): number {
	const big = Number.MAX_SAFE_INTEGER;
	return (
		(a.blockNo ?? big) - (b.blockNo ?? big) ||
		(a.gridRow ?? big) - (b.gridRow ?? big) ||
		(a.gridCol ?? big) - (b.gridCol ?? big) ||
		(a.code ?? '').localeCompare(b.code ?? '')
	);
}

/** Lấp các màn trong `left` vào ô trống theo thứ tự đọc; hết ô thì đưa về khay. */
function fillEmptyCells(left: LayoutScreen[], blocks: LayoutBlock[], taken: Set<string>) {
	blocks.forEach((b, bi) => {
		for (let r = 0; r < b.rows; r++)
			for (let c = 0; c < b.cols; c++) {
				if (!left.length) return;
				if (taken.has(cellKey(bi, r, c))) continue;
				Object.assign(left.shift()!, { blockNo: bi, gridRow: r, gridCol: c });
				taken.add(cellKey(bi, r, c));
			}
	});
	for (const s of left) Object.assign(s, { blockNo: null, gridRow: null, gridCol: null });
}

/** Lấp các màn ngoài lưới vào ô trống, giữ nguyên màn đang có vị trí. */
export function fillTray(screens: LayoutScreen[], blocks: LayoutBlock[]) {
	const taken = new Set(screens.filter((s) => isInGrid(s, blocks)).map((s) => cellKey(s.blockNo!, s.gridRow!, s.gridCol!)));
	const left = screens.filter((s) => !isInGrid(s, blocks)).sort((a, b) => (a.code ?? '').localeCompare(b.code ?? ''));
	fillEmptyCells(left, blocks, taken);
}

/** Mã SCR-xx kế tiếp (lớn hơn số lớn nhất đang dùng) — cùng quy tắc với BE; thêm luôn vào `used`. */
export function nextScreenCode(used: Set<string>): string {
	const seq = (code: string) => (code.toUpperCase().startsWith(SCREEN_CODE_PREFIX) ? parseInt(code.slice(SCREEN_CODE_PREFIX.length), 10) || 0 : 0);
	let next = Math.max(0, ...[...used].map(seq)) + 1;
	let code = '';
	do {
		code = `${SCREEN_CODE_PREFIX}${String(next++).padStart(2, '0')}`;
	} while (used.has(code));
	used.add(code);
	return code;
}
