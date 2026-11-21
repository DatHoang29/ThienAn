import { watch, onUnmounted } from 'vue';
import { debounce } from 'lodash-es';
import { getAPI } from '/@/utils/axios-utils';
import { VmsControlApi, PreviewInput } from '/@/api-services/vms';
import { VmsActionType } from '/@/types/enums/vmsEnum';
import type { VmsControlDeps } from './types';

/**
 * Hàm thuần gọi API preview VMS, trả về chuỗi base64 (rỗng nếu lỗi/không có).
 * Tách riêng để các màn hình khác (vd: Quản lý nội dung mặc định) tái sử dụng
 * mà không cần toàn bộ state của controlVms.
 */
export async function fetchVmsPreviewBase64(input: PreviewInput): Promise<string> {
	try {
		const res = await getAPI(VmsControlApi).apiVmsVmscontrolPreviewPost(input);
		return res?.data?.result?.base64 || '';
	} catch (e) {
		console.error('VMS preview error:', e);
		return '';
	}
}

/**
 * Preview VMS qua API (ảnh base64) + debounce + watch các trường ảnh hưởng preview.
 */
export function useVmsPreview(deps: VmsControlDeps) {
	const { state, $t } = deps;

	const isApiRenderType = (ctrl: string) => ctrl === VmsActionType.Text || ctrl === VmsActionType.Message || ctrl === VmsActionType.Phrase || ctrl === VmsActionType.Template;

	const buildPreviewInput = (): PreviewInput => {
		const ctrl = state.ruleForm.controlType;
		const w = Number(state.vmsData?.width);
		const h = Number(state.vmsData?.height);
		const size = w > 0 && h > 0 ? `${w}x${h}` : '384x64';

		const input: PreviewInput = { controlType: ctrl, size, brightness: state.ruleForm.brightness ?? 100 };
		if (ctrl === VmsActionType.Template) {
			input.jsonContent = state.previewTemplateJson || '';
		} else {
			input.content1 = state.ruleForm.row1 ?? '';
			input.content2 = state.ruleForm.row2 ?? '';
			input.color = state.ruleForm.textColor;
			input.backColor = state.ruleForm.bgColor;
			input.font = state.ruleForm.selectedFont;
			input.fontSize = state.ruleForm.fontSize != null ? String(state.ruleForm.fontSize) : '';
		}
		return input;
	};

	// Gọi API preview, set src base64 hoặc error message.
	const fetchPreview = async () => {
		const ctrl = state.ruleForm.controlType;
		if (!isApiRenderType(ctrl)) return;
		if (ctrl === VmsActionType.Template && !state.previewTemplateJson) {
			state.preview.src = '';
			state.preview.error = '';
			return;
		}
		state.preview.loading = true;
		state.preview.error = '';
		try {
			const res = await getAPI(VmsControlApi).apiVmsVmscontrolPreviewPost(buildPreviewInput());
			const base64 = res?.data?.result?.base64 || '';
			if (base64) state.preview.src = base64;
			else state.preview.error = $t('lz.message.vms.previewFailed');
		} catch (e) {
			console.error('VMS preview error:', e);
			state.preview.error = $t('lz.message.vms.previewFailed');
		} finally {
			state.preview.loading = false;
		}
	};

	// Hàm xử lý debounce để gọi API preview, tránh gọi quá nhiều lần khi người dùng thay đổi input nhanh.
	// 350ms (trước là 1500ms): nội dung hiện lên ảnh gần như tức thì nhưng vẫn gộp được các phím gõ liên tiếp.
	const PREVIEW_DEBOUNCE_MS = 350;
	const schedulePreview = debounce(() => {
		if (!isApiRenderType(state.ruleForm.controlType)) return;
		fetchPreview();
	}, PREVIEW_DEBOUNCE_MS);

	onUnmounted(() => schedulePreview.cancel());

	// watch dùng để gọi API preview khi người dùng thay đổi input (row1, row2, font, color, bgColor, fontSize, brightness, jsonContent).
	watch(
		() => [
			state.ruleForm.controlType,
			state.ruleForm.row1,
			state.ruleForm.row2,
			state.ruleForm.textColor,
			state.ruleForm.bgColor,
			state.ruleForm.selectedFont,
			state.ruleForm.fontSize,
			state.ruleForm.brightness,
			state.previewTemplateJson,
		],
		() => schedulePreview()
	);

	return { schedulePreview, fetchPreview };
}
