import { computed } from 'vue';
import { ElMessage } from 'element-plus';
import { getAPI } from '/@/utils/axios-utils';
import { VmsTemplateApi } from '/@/api-services/vms';
import { VmsActionType } from '/@/types/enums/vmsEnum';
import type { VmsControlDeps } from './types';

/**
 * Logic nội dung điều khiển: định dạng chữ, ảnh, template, danh sách lập lịch.
 */
export function useVmsContent(deps: VmsControlDeps) {
	const { state, $t, selectedTemplate } = deps;

	//============ ĐỊNH DẠNG CHỮ & PREVIEW HELPER =============================

	const updatePreview = () => {
		state.showWarning = state.ruleForm.textColor === state.ruleForm.bgColor;
		if (state.ruleForm.controlType === VmsActionType.Template) syncTemplateJson();
	};

	const formatRowCase = (row: 'row1' | 'row2', mode: 'upper' | 'title' | 'lower') => {
		const current: string = state.ruleForm[row] || '';
		if (mode === 'upper') state.ruleForm[row] = current.toUpperCase();
		else if (mode === 'lower') state.ruleForm[row] = current.toLowerCase();
		else state.ruleForm[row] = current.replace(/\S+/g, (w) => w.charAt(0).toUpperCase() + w.slice(1).toLowerCase());
	};

	const formatTemplateElementCase = (el: any, mode: 'upper' | 'title' | 'lower') => {
		const current: string = el.content || '';
		if (mode === 'upper') el.content = current.toUpperCase();
		else if (mode === 'lower') el.content = current.toLowerCase();
		else el.content = current.replace(/\S+/g, (w) => w.charAt(0).toUpperCase() + w.slice(1).toLowerCase());
		syncTemplateJson();
	};

	//============ IMAGE =============================

	const handleImageUpload = (fileList: any) => {
		if (!fileList || fileList.length === 0) {
			state.ruleForm.urlImage = null;
			return;
		}
		uploadImage(fileList);
	};

	const uploadImage = (file: any) => {
		const reader = new FileReader();
		reader.onload = (e) => {
			state.ruleForm.urlImage = e.target?.result as string;
		};
		reader.readAsDataURL(file.raw || '');
	};

	const beforeUpload = (file: File) => {
		const isImage = file.type.startsWith('image/');
		if (!isImage) ElMessage.error($t('lz.message.vmsMassage.errorBeforeUploadImage'));
		return isImage;
	};

	const updateSelectedImageUrl = () => {
		const selectedItem = state.listData.imageList.find((item: any) => item.id === state.ruleForm.imgTemplate);
		state.ruleForm.urlImage = selectedItem?.url;
	};

	//============ TEMPLATE =============================

	const updateSelectedTemplateUrl = async () => {
		const selectedItem = state.listData.templateList.find((item: any) => item.id === state.ruleForm.imgTemplate);
		state.ruleForm.urlImage = selectedItem?.preview;

		state.previewTemplateJson = '';
		selectedTemplate.value = null;
		if (!selectedItem) return;
		try {
			const res = await getAPI(VmsTemplateApi).apiVmsVmstemplateDatalistIdGet(selectedItem.id);
			const list = res?.data?.result;
			const design = Array.isArray(list) ? (list.find((x: any) => x.code === selectedItem.code) ?? list[0]) : null;
			state.previewTemplateJson = design?.jsonData || '';
			if (state.previewTemplateJson) {
				selectedTemplate.value = JSON.parse(state.previewTemplateJson);
				if (!selectedTemplate.value.backColor) selectedTemplate.value.backColor = state.ruleForm.bgColor || 'Black';
				syncTemplateJson();
			}
		} catch (e) {
			console.error('Load template jsonData error:', e);
		}
	};

	// Áp nền + style chung cho mọi ô text -> JSON -> preview.
	const syncTemplateJson = () => {
		if (!selectedTemplate.value) return;
		selectedTemplate.value.backColor = state.ruleForm.bgColor;
		(selectedTemplate.value.elements || []).forEach((el: any) => {
			if ((el.type || '').toLowerCase() === 'text') {
				el.color = state.ruleForm.textColor;
				el.font = state.ruleForm.selectedFont;
				el.fontSize = String(state.ruleForm.fontSize);
			}
		});
		state.previewTemplateJson = JSON.stringify(selectedTemplate.value);
	};

	const textElements = computed(() => selectedTemplate.value?.elements?.filter((e: any) => e.type === 'text') || []);

	//============ ROUTER CHỌN ẢNH/TEMPLATE/TIMER/SCHEDULE =============================

	const handleImageTemplateSelectChange = (selectedId: string) => {
		switch (state.ruleForm.controlType) {
			case VmsActionType.Image:
				updateSelectedImageUrl();
				break;
			case VmsActionType.Template:
				updateSelectedTemplateUrl();
				break;
			case VmsActionType.Time:
				handleTimerChange(selectedId);
				break;
			case VmsActionType.Schedule:
				handleSheduleChange(selectedId);
				break;
		}
	};

	const handleTimerChange = (selectedId: string) => {
		const selectedItem = state.listData.templateList.find((item: any) => item.id === selectedId);
		if (selectedItem) state.ruleForm.urlImage = selectedItem.preview;
	};

	const handleSheduleChange = (selectedId: string) => {
		const selectedItem = state.listData.templateList.find((item: any) => item.id === selectedId);
		if (selectedItem) {
			state.scheduleImageList.push({ id: selectedItem.id, preview: selectedItem.preview, timeToLive: 5 });
		}
	};

	const removeAt = (idx: any) => {
		state.scheduleImageList.splice(idx, 1);
	};
	const onMove = () => true;
	const moveItem = (fromIndex: number, toIndex: number) => {
		if (fromIndex < 0 || fromIndex >= state.scheduleImageList.length || toIndex < 0 || toIndex >= state.scheduleImageList.length) return;
		const item = state.scheduleImageList[fromIndex];
		state.scheduleImageList.splice(fromIndex, 1);
		state.scheduleImageList.splice(toIndex, 0, item);
	};

	return {
		updatePreview,
		formatRowCase,
		formatTemplateElementCase,
		handleImageUpload,
		uploadImage,
		beforeUpload,
		updateSelectedImageUrl,
		updateSelectedTemplateUrl,
		syncTemplateJson,
		textElements,
		handleImageTemplateSelectChange,
		handleTimerChange,
		handleSheduleChange,
		removeAt,
		onMove,
		moveItem,
	};
}
