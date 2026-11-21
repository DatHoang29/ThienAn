import { VmsActionType } from '/@/types/enums/vmsEnum';
import type { VmsControlDeps } from './types';

interface DisplayDeps extends VmsControlDeps {
	// hàm từ useVmsManual (chỉ dùng khi mode manual)
	bindVmsDataToSelect: (ctrlType: string) => void;
	displayInformationProcess: (ctrlType: string) => void;
}

/**
 * Điều khiển hiển thị theo loại (control type): ẩn/hiện vùng nhập liệu,
 * đổi loại điều khiển, chọn message/phrase.
 */
export function useVmsControlDisplay(deps: DisplayDeps) {
	const { state, props, selectedTemplate, bindVmsDataToSelect, displayInformationProcess } = deps;

	const onMessageSelectChange = (value: string) => {
		state.ruleForm.row1 = state.listData.messageList.find((item: any) => item.code == value)?.content1 || '';
		state.ruleForm.row2 = state.listData.messageList.find((item: any) => item.code == value)?.content2 || '';
	};

	// Cập nhật row1 theo wordList1 (câu 1) khi chọn cụm từ.
	const onWordList1SelectChange = () => {
		const temp = [] as any;
		state.ruleForm.wordList1?.forEach((code: any) => {
			const content = state.listData.phraseList?.find((item: any) => item.code == code)?.name;
			temp.push(content ?? code);
		});
		state.ruleForm.row1 = temp?.map((item: any) => item?.trim().toUpperCase())?.join(' ');
	};

	// Cập nhật row2 khi chọn phrase2.
	const onWordList2SelectChange = () => {
		const temp = [] as any;
		state.ruleForm.wordList2?.forEach((code: any) => {
			const content = state.listData.phraseList?.find((item: any) => item.code == code)?.name;
			temp.push(content ?? code);
		});
		state.ruleForm.row2 = temp?.map((item: any) => item?.trim().toUpperCase())?.join(' ');
	};

	// Bật/tắt vùng nhập liệu theo loại điều khiển.
	const toggleControlDisplay = () => {
		const ctrlType = state.ruleForm.controlType;

		state.inputDisplay.imageSelect = ctrlType == VmsActionType.Image;
		state.inputDisplay.templateSelect = ctrlType == VmsActionType.Template || ctrlType == VmsActionType.Time || ctrlType == VmsActionType.Schedule;
		state.inputDisplay.messageSelect = ctrlType == VmsActionType.Message;

		state.inputDisplay.content1Input = ctrlType == VmsActionType.Text || ctrlType == VmsActionType.Message;
		state.inputDisplay.content2Input = ctrlType == VmsActionType.Text || ctrlType == VmsActionType.Message;

		state.inputDisplay.phrase1Select = ctrlType == VmsActionType.Phrase;
		state.inputDisplay.phrase2Select = ctrlType == VmsActionType.Phrase;

		state.inputDisplay.fontSelect = ctrlType == VmsActionType.Text || ctrlType == VmsActionType.Message || ctrlType == VmsActionType.Phrase || ctrlType == VmsActionType.Template;
		state.inputDisplay.fontSizeSelect = state.inputDisplay.fontSelect;
		state.inputDisplay.colorSelect = state.inputDisplay.fontSelect;
		state.inputDisplay.backgroundSelect = state.inputDisplay.fontSelect;

		state.inputDisplay.timeStartPicker = ctrlType == VmsActionType.Time;
		state.inputDisplay.timeEndPicker = ctrlType == VmsActionType.Time;

		state.inputDisplay.brightnessSlider = true;
		state.inputDisplay.priorityInput = true;

		state.inputDisplay.preview = ctrlType == VmsActionType.Text || ctrlType == VmsActionType.Message || ctrlType == VmsActionType.Phrase || ctrlType == VmsActionType.Template;
		state.inputDisplay.previewImage = ctrlType == VmsActionType.Image || ctrlType == VmsActionType.Time;
		state.inputDisplay.previewSchedule = ctrlType == VmsActionType.Schedule;
	};

	// Reset dữ liệu form theo loại.
	const resetControlToDefault = () => {
		state.ruleForm.row1 = '';
		state.ruleForm.row2 = '';
		state.ruleForm.row3 = '';
		state.ruleForm.wordList1 = [];
		state.ruleForm.wordList2 = [];
		state.ruleForm.imgTemplate = '';
		state.ruleForm.urlImage = '';
		state.preview.src = '';
		state.preview.error = '';
		state.previewTemplateJson = '';
		selectedTemplate.value = null;
	};

	// Xử lý khi đổi loại điều khiển: ẩn/hiện vùng nhập liệu, reset dữ liệu, bind + đổ lại thông tin tiến trình (mode manual).
	const onVmsActionlTypeChange = () => {
		toggleControlDisplay();
		resetControlToDefault();
		// mode manual: giữ nguyên hành vi bind + đổ lại thông tin tiến trình
		if (props.mode === 'manual') {
			bindVmsDataToSelect(state.ruleForm.controlType);
			displayInformationProcess(state.ruleForm.controlType);
		}
	};

	// Trả về các hàm để component cha gọi khi cần thiết từ index.vue (controlVms).
	return {
		onMessageSelectChange,
		onWordList1SelectChange,
		onWordList2SelectChange,
		toggleControlDisplay,
		resetControlToDefault,
		onVmsActionlTypeChange,
	};
}
