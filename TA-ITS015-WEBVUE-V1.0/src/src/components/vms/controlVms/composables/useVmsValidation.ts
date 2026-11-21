import { computed } from 'vue';
import { ElMessage } from 'element-plus';
import { VmsActionType, VmsControlConfigCodeEnum } from '/@/types/enums/vmsEnum';
import { readOpConfigInt } from './useOpConfig';
import type { VmsControlDeps } from './types';

/**
 * Ràng buộc dữ liệu điều khiển VMS/LCS TRƯỚC KHI chuẩn bị payload gửi ra thiết bị thực tế.
 * Mục tiêu: chặn các trường hợp nhập sai/trùng để tránh gửi nội dung không hợp lệ xuống bảng.
 *
 * Các quy tắc kiểm tra:
 *  1) Độ dài mỗi dòng nội dung (row1/row2 và từng ô text của template) không vượt cấu hình.
 *  2) Màu chữ KHÔNG được trùng màu nền (đọc không ra chữ trên bảng).
 *  3) Cỡ chữ phải là giá trị hợp lệ (thuộc danh sách cấu hình cỡ chữ).
 *
 * Giới hạn độ dài lấy từ SysOpConfig qua readOpConfigInt (./useOpConfig); nếu DB CHƯA cấu hình
 * thì dùng giá trị mặc định (DEFAULT_CONTENT_MAX_LENGTH) để cơ chế vẫn hoạt động.
 *
 * Sử dụng:
 *  - validateContentBeforeSubmit(): gọi khi bấm Xác nhận -> hiện toast lỗi + trả false để CHẶN submit.
 *  - isContentValid (computed): cờ reactive (không toast) -> dùng để DISABLE nút Xác nhận ở component cha.
 */

// Giá trị mặc định khi DB chưa cấu hình giới hạn ký tự mỗi dòng
const DEFAULT_CONTENT_MAX_LENGTH = 40;

// Kết quả đánh giá: null = hợp lệ; ngược lại là thông tin lỗi để dựng thông báo.
type ValidationError = { key: string; params?: Record<string, any> } | null;

export function useVmsValidation(deps: VmsControlDeps) {
	const { state, $t, selectedTemplate } = deps;

	// Giới hạn ký tự mỗi dòng: ưu tiên cấu hình, fallback mặc định nếu chưa cấu hình/không hợp lệ.
	// Hàm đọc cấu hình (ưu tiên nhóm operation_config, không thấy thì quét mọi nhóm) đã tách sang
	// useOpConfig.ts để index.vue và composable này dùng chung một cách đọc.
	const contentMaxLength = computed<number>(() => readOpConfigInt(VmsControlConfigCodeEnum.ContentMaxLength, DEFAULT_CONTENT_MAX_LENGTH));

	// Các loại điều khiển dạng chữ (dùng row1/row2)
	const isTextControl = (ctrl: string) =>
		ctrl === VmsActionType.Text || ctrl === VmsActionType.Message || ctrl === VmsActionType.Phrase;

	// Cỡ chữ hợp lệ = có chọn + thuộc danh sách cấu hình (state.listData.fontSizeList).
	const isFontSizeValid = (): boolean => {
		const list = state.listData?.fontSizeList ?? [];
		const size = state.ruleForm?.fontSize;
		if (size === '' || size === null || size === undefined) return false;
		if (!Array.isArray(list) || list.length === 0) return true; // chưa có danh sách -> không chặn
		return list.some((item: any) => String(item.code) === String(size));
	};

	// Màu chữ trùng màu nền
	const isSameColor = (): boolean => {
		const { textColor, bgColor } = state.ruleForm ?? {};
		return !!textColor && !!bgColor && textColor === bgColor;
	};

	// Lấy danh sách nội dung ô text của template đang chọn
	const getTemplateTextContents = (): string[] => {
		const els = selectedTemplate.value?.elements ?? [];
		return els
			.filter((e: any) => (e?.type || '').toLowerCase() === 'text')
			.map((e: any) => (e?.content ?? '').toString());
	};

	/**
	 * Đánh giá dữ liệu điều khiển theo loại. Trả về null nếu hợp lệ,
	 * ngược lại trả về {key, params} để dựng thông báo lỗi.
	 * KHÔNG gây hiệu ứng phụ (không toast) -> dùng được cho cả computed lẫn submit.
	 */
	const evaluate = (): ValidationError => {
		const ctrl = state.ruleForm.controlType;
		const max = contentMaxLength.value;

		// ===== 1) Nội dung dạng chữ (Text / Message / Phrase) =====
		if (isTextControl(ctrl)) {
			const row1 = (state.ruleForm.row1 ?? '').toString().trim();
			const row2 = (state.ruleForm.row2 ?? '').toString().trim();

			if (!row1 && !row2) return { key: 'lz.validation.vmsControl.contentRequired' };
			if (row1.length > max || row2.length > max) return { key: 'lz.validation.vmsControl.contentMaxLength', params: { max } };
			if (isSameColor()) return { key: 'lz.message.vmsControl.sameColorAlert' };
			if (!isFontSizeValid()) return { key: 'lz.validation.vmsControl.invalidFontSize' };
			return null;
		}

		// ===== 2) Template (nhiều ô text) =====
		if (ctrl === VmsActionType.Template) {
			if (!state.ruleForm.imgTemplate) return { key: 'lz.validation.vmsControl.templateRequired' };
			if (getTemplateTextContents().some((c) => c.trim().length > max)) return { key: 'lz.validation.vmsControl.contentMaxLength', params: { max } };
			if (isSameColor()) return { key: 'lz.message.vmsControl.sameColorAlert' };
			if (!isFontSizeValid()) return { key: 'lz.validation.vmsControl.invalidFontSize' };
			return null;
		}

		// ===== 3) Image / Time / Schedule =====
		if (ctrl === VmsActionType.Image) {
			if (!state.ruleForm.urlImage) return { key: 'lz.validation.vmsControl.imageRequired' };
			return null;
		}
		if (ctrl === VmsActionType.Time) {
			if (!state.ruleForm.imgTemplate) return { key: 'lz.validation.vmsControl.templateRequired' };
			if (!state.ruleForm.startTime || !state.ruleForm.endTime) return { key: 'lz.validation.vmsControl.timeRangeRequired' };
			return null;
		}
		if (ctrl === VmsActionType.Schedule) {
			if (!Array.isArray(state.scheduleImageList) || state.scheduleImageList.length === 0) return { key: 'lz.validation.vmsControl.scheduleRequired' };
			return null;
		}

		return null;
	};

	// Cờ reactive: true nếu dữ liệu hợp lệ (dùng để disable nút Xác nhận ở cha).
	const isContentValid = computed<boolean>(() => evaluate() === null);

	/**
	 * Kiểm tra dữ liệu trước khi submit (có toast lỗi).
	 * @returns true nếu hợp lệ; false + hiển thị ElMessage nếu vi phạm (dừng submit).
	 */
	const validateContentBeforeSubmit = (): boolean => {
		const err = evaluate();
		if (err) {
			ElMessage.error($t(err.key, err.params ?? {}));
			return false;
		}
		return true;
	};

	return {
		contentMaxLength,
		isContentValid,
		isFontSizeValid,
		isSameColor,
		validateContentBeforeSubmit,
	};
}
