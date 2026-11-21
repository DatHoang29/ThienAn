/**
 * Đóng panel PrimeVue DatePicker khi bấm ra ngoài, dùng cho các DatePicker nằm trong modal có chặn
 * nổi bọt (bubble) sự kiện chuột.
 *
 * Bối cảnh: PrimeVue DatePicker tự đóng panel bằng một listener `mousedown` ở giai đoạn BUBBLE trên
 * `document`. Các modal của big screen (`InfoPanel`, `DraggableModal`) dùng `@mousedown.stop` để không
 * kéo modal khi người dùng tương tác với nội dung, nên sự kiện không bao giờ lên tới `document` và panel
 * lịch không bao giờ tự đóng.
 *
 * Cách xử lý: nơi dùng đăng ký listener `mousedown` ở giai đoạn CAPTURE trên `document`
 * (`addEventListener('mousedown', handler, true)`) — giai đoạn capture chạy trước mọi `stopPropagation()`
 * ở bubble — rồi gọi {@link closeOutsideDatePickers} để hạ cờ `overlayVisible` của các picker đang mở.
 */

/** Phần bề mặt của PrimeVue DatePicker mà module này cần (không phụ thuộc kiểu nội bộ của PrimeVue). */
export interface DatePickerLike {
	/** Phần tử gốc (ô input + icon) của picker. */
	$el?: HTMLElement | null;
	/** Panel lịch; có thể được teleport ra ngoài cây component khi `appendTo="body"`. */
	overlay?: HTMLElement | null;
	/** Cờ hiển thị panel (thuộc `data()` của DatePicker nên gán được từ bên ngoài). */
	overlayVisible?: boolean;
}

/** Class gốc của panel DatePicker (PrimeVue 4) — dùng để nhận diện panel đã teleport. */
const DATE_PICKER_PANEL_SELECTOR = '.p-datepicker-panel';

/**
 * Điểm bấm có nằm trong phạm vi của picker (ô input hoặc panel lịch) hay không.
 *
 * Kiểm tra theo 3 lớp để không phụ thuộc một chi tiết nội bộ duy nhất của PrimeVue:
 * phần tử gốc `$el`, tham chiếu `overlay`, và cuối cùng là class panel (trường hợp panel được
 * teleport ra `body` nên không nằm trong `$el`).
 *
 * @param picker - Instance DatePicker (hoặc `null`/`undefined` nếu chưa mount).
 * @param target - `event.target` của sự kiện chuột.
 * @returns `true` nếu điểm bấm thuộc picker -> KHÔNG được đóng panel.
 */
export function isPointerInsideDatePicker(picker: DatePickerLike | null | undefined, target: EventTarget | null): boolean {
	if (!picker || !target || !(target instanceof Node)) return false;
	const root = picker.$el;
	if (root && root.contains(target)) return true;
	const overlay = picker.overlay;
	if (overlay && overlay.contains(target)) return true;
	return target instanceof Element && Boolean(target.closest(DATE_PICKER_PANEL_SELECTOR));
}

/**
 * Đóng mọi picker đang mở mà điểm bấm nằm ngoài phạm vi của nó.
 *
 * Bỏ qua picker chưa mount hoặc đang đóng, nên gọi được vô điều kiện trong listener chuột.
 *
 * @param pickers - Danh sách instance DatePicker cần kiểm tra.
 * @param target - `event.target` của sự kiện chuột.
 * @returns Số picker vừa bị đóng (hữu ích cho kiểm thử/chẩn đoán).
 */
export function closeOutsideDatePickers(pickers: (DatePickerLike | null | undefined)[], target: EventTarget | null): number {
	let closed = 0;
	pickers.forEach((picker) => {
		if (!picker || !picker.overlayVisible) return;
		if (isPointerInsideDatePicker(picker, target)) return;
		picker.overlayVisible = false;
		closed += 1;
	});
	return closed;
}
