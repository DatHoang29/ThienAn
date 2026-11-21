import { nextTick } from 'vue';

/**
 * Ép số về trong khoảng [min, max] (thiếu biên nào thì bỏ qua biên đó).
 * @example clampNumber(25, { min: 0, max: 23 }) // 23
 */
export function clampNumber(val: number, range: { min?: number; max?: number }): number {
	if (range.max != null && val > range.max) return range.max;
	if (range.min != null && val < range.min) return range.min;
	return val;
}

/**
 * Chặn ngay khi nhập vượt khoảng [min, max] của el-input-number, tự động ép về giá trị biên.
 * (el-input-number mặc định chỉ kẹp về min/max khi blur nên vẫn gõ được số ngoài khoảng.)
 * Dùng với sự kiện `@input` của el-input-number.
 * @param val Giá trị đang nhập
 * @param range Khoảng cho phép `{ min?, max? }`
 * @param inputNumber Instance el-input-number (template ref)
 * @param setValue Gán lại giá trị đã ép vào model
 * @example
 * <el-input-number ref="widthRef" v-model="form.width" :max="3840"
 *   @input="(v) => clampInputNumber(v, { max: 3840 }, widthRef, (n) => (form.width = n))" />
 */
export function clampInputNumber(val: number | null | undefined, range: { min?: number; max?: number }, inputNumber: any, setValue: (v: number) => void) {
	if (val == null) return;
	const clamped = clampNumber(val, range);
	if (clamped === val) return;

	setValue(clamped);
	nextTick(() => {
		const inputEl = inputNumber?.$el?.querySelector('input') as HTMLInputElement | null;
		if (inputEl) {
			inputEl.value = String(clamped);
			inputEl.dispatchEvent(new Event('change', { bubbles: true }));
		}
	});
}

/**
 * Chặn gõ chữ, chỉ cho gõ số (vẫn cho Backspace, Delete, mũi tên, Tab, Enter và tổ hợp Ctrl/Cmd).
 * Dùng với sự kiện `@keydown` của el-input, el-time-picker...
 * @param e Sự kiện keydown
 * @param allow Ký tự được phép gõ thêm ngoài số, VD `':'` cho giờ `HH:mm`
 * @example
 * <el-input @keydown="onlyNumberKeydown" />
 */
export function onlyNumberKeydown(e: KeyboardEvent, allow = '') {
	if (e.key.length !== 1 || e.ctrlKey || e.metaKey) return;
	if (!/\d/.test(e.key) && !allow.includes(e.key)) e.preventDefault();
}

/**
 * Ô giờ HH:mm gõ sai thì trả về giá trị cũ.
 * el-time-picker đọc chữ gõ tay bằng dayjs không chặt: "25:01" ⇒ 01:01 hôm sau, "12:75" ⇒ 13:15 và vẫn lưu vào model.
 * Gắn ở pha capture của thẻ bọc ngoài (chạy trước el-time-picker) cho sự kiện change và Enter.
 * @param e Sự kiện change / keydown của ô input
 * @param oldValue Giá trị đang có của model (HH:mm)
 * @example
 * <div @change.capture="restoreInvalidTime($event, form.time)" @keydown.enter.capture="restoreInvalidTime($event, form.time)">
 *   <el-time-picker v-model="form.time" format="HH:mm" value-format="HH:mm" />
 * </div>
 */
export function restoreInvalidTime(e: Event, oldValue?: string | null) {
	const el = e.target as HTMLInputElement;
	if (el.tagName !== 'INPUT' || !el.value || /^([01]\d|2[0-3]):[0-5]\d$/.test(el.value)) return;
	el.value = oldValue ?? '';
	// Báo cho el-time-picker cập nhật chữ đang gõ trước khi nó đọc giá trị.
	el.dispatchEvent(new Event('input', { bubbles: true }));
}
