/**
 * templateLayout.ts
 *
 * Logic bố cục (layout) thuần cho trình chỉnh sửa template CCTV.
 * Tách riêng khỏi component .vue để có thể unit-test độc lập (không phụ thuộc DOM/GridStack).
 *
 * Quy ước: grid mặc định 6 cột x 6 hàng, mỗi item là hình vuông (w === h).
 */

/** Số cột mặc định của grid template */
export const GRID_COLUMNS = 6;
/** Số hàng mặc định của grid template */
export const GRID_ROWS = 6;
/** Số item tối đa có thể đặt trong grid (mỗi item nhỏ nhất 1x1) */
export const MAX_SLOTS = GRID_COLUMNS * GRID_ROWS;

/** Hình chữ nhật trong grid (đơn vị: ô) */
export interface LayoutRect {
	x: number;
	y: number;
	w: number;
	h: number;
}

/** 1 slot trong template = vị trí/kích thước kèm số thứ tự hiển thị */
export interface LayoutSlot extends LayoutRect {
	slotNo: number;
}

/**
 * Kiểm tra 2 hình chữ nhật có đè (giao nhau) hay không.
 * Dùng nguyên tắc AABB: 2 rect KHÔNG đè nếu tách rời theo trục X hoặc Y.
 */
export const isOverlapping = (a: LayoutRect, b: LayoutRect): boolean => {
	return a.x < b.x + b.w && a.x + a.w > b.x && a.y < b.y + b.h && a.y + a.h > b.y;
};

/**
 * Kiểm tra 1 hình chữ nhật có nằm trọn trong grid hay không.
 */
export const isWithinGrid = (rect: LayoutRect, cols: number = GRID_COLUMNS, rows: number = GRID_ROWS): boolean => {
	return rect.x >= 0 && rect.y >= 0 && rect.w >= 1 && rect.h >= 1 && rect.x + rect.w <= cols && rect.y + rect.h <= rows;
};

/**
 * Kiểm tra có thể đặt rect (x,y,w,h) vào grid không:
 * vừa phải nằm trong grid, vừa không đè lên bất kỳ item nào trong `others`.
 */
export const canPlaceRect = (
	others: LayoutRect[],
	x: number,
	y: number,
	w: number,
	h: number,
	cols: number = GRID_COLUMNS,
	rows: number = GRID_ROWS,
): boolean => {
	const candidate: LayoutRect = { x, y, w, h };
	if (!isWithinGrid(candidate, cols, rows)) return false;
	return !others.some((item) => isOverlapping(item, candidate));
};

/**
 * Tìm vị trí trống đầu tiên (quét theo thứ tự đọc: trên→dưới, trái→phải)
 * cho 1 item kích thước (w x h). Trả về null nếu không còn chỗ.
 */
export const findFreePosition = (
	existing: LayoutRect[],
	w: number,
	h: number,
	cols: number = GRID_COLUMNS,
	rows: number = GRID_ROWS,
): { x: number; y: number } | null => {
	for (let y = 0; y <= rows - h; y++) {
		for (let x = 0; x <= cols - w; x++) {
			if (canPlaceRect(existing, x, y, w, h, cols, rows)) return { x, y };
		}
	}
	return null;
};

/**
 * Sinh bố cục lưới đều cho `count` camera.
 * Chia thành lưới vuông gần nhất (ceil(sqrt(count)) cột), mỗi item là hình vuông
 * có cạnh = floor(cols_grid / số_cột). Số lượng được giới hạn trong [1, MAX_SLOTS]
 * để không vượt quá sức chứa grid (tránh sinh item tràn ra ngoài).
 */
export const generateEqualGrid = (
	count: number,
	cols: number = GRID_COLUMNS,
	rows: number = GRID_ROWS,
): LayoutSlot[] => {
	const maxSlots = cols * rows;
	const safeCount = Math.max(1, Math.min(Math.floor(count), maxSlots));
	const slotColumns = Math.ceil(Math.sqrt(safeCount));
	const itemSize = Math.max(1, Math.floor(cols / slotColumns));
	return Array.from({ length: safeCount }, (_, index) => {
		const colIndex = index % slotColumns;
		const rowIndex = Math.floor(index / slotColumns);
		return {
			slotNo: index + 1,
			x: colIndex * itemSize,
			y: rowIndex * itemSize,
			w: itemSize,
			h: itemSize,
		};
	});
};

/**
 * Tính kích thước vuông hợp lệ cho 1 item sau khi người dùng resize.
 * - Nếu đang là hình chữ nhật (w !== h): ưu tiên phóng to theo cạnh lớn.
 * - Clamp theo biên grid (không tràn ra ngoài).
 * - Nếu kích thước phóng to gây đè lên item khác thì thu về cạnh nhỏ.
 * Trả về độ dài cạnh (>= 1).
 */
export const resolveSquareSize = (
	rect: LayoutRect,
	others: LayoutRect[],
	cols: number = GRID_COLUMNS,
	rows: number = GRID_ROWS,
): number => {
	const growSize = Math.max(rect.w, rect.h);
	const shrinkSize = Math.min(rect.w, rect.h);
	const maxSize = Math.min(cols - rect.x, rows - rect.y);
	let finalSize = Math.max(1, Math.min(growSize, maxSize));
	if (!canPlaceRect(others, rect.x, rect.y, finalSize, finalSize, cols, rows)) {
		finalSize = Math.max(1, shrinkSize);
	}
	return finalSize;
};

/**
 * Đánh số lại các slot theo thứ tự đọc (trên→dưới, trái→phải).
 * Trả về mảng MỚI đã sắp xếp với slotNo = 1..N (giữ nguyên x/y/w/h).
 */
export const renumberByReadingOrder = (slots: LayoutRect[]): LayoutSlot[] => {
	return [...slots]
		.sort((a, b) => a.y - b.y || a.x - b.x)
		.map((rect, index) => ({
			slotNo: index + 1,
			x: rect.x,
			y: rect.y,
			w: rect.w,
			h: rect.h,
		}));
};

/**
 * Kiểm tra toàn bộ bố cục có hợp lệ không:
 * mọi item nằm trong grid và không có cặp nào đè nhau.
 */
export const isLayoutValid = (
	slots: LayoutRect[],
	cols: number = GRID_COLUMNS,
	rows: number = GRID_ROWS,
): boolean => {
	for (let i = 0; i < slots.length; i++) {
		if (!isWithinGrid(slots[i], cols, rows)) return false;
		for (let j = i + 1; j < slots.length; j++) {
			if (isOverlapping(slots[i], slots[j])) return false;
		}
	}
	return true;
};
