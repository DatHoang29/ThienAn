<template>
	<div class="mail-edit-mau-container">
		<el-dialog v-model="state.isShowDialog" width="min(1800px, 98vw)" top="4vh" draggable destroy-on-close append-to-body :close-on-click-modal="false" :before-close="beforeClose">
			<template #header>
				<div class="mail-dialog-header">
					<el-icon size="14" class="mail-dialog-header__icon"><ele-Document /></el-icon>
					<span class="mail-dialog-header__title">{{ props.title.replace(/template mail|template/gi, 'mẫu thư') }}</span>
				</div>
			</template>		
			<el-form ref="ruleFormRef" :model="state.ruleForm" :rules="rules" class="edit-form" label-position="top" :disabled="state.isProcessing || props.operateType === 'view'" @submit.prevent>
				<el-splitter class="edit-layout" :class="{ 'edit-layout--stacked': isCompactLayout }">
					<el-splitter-panel size="33%" min="20%" :resizable="!isCompactLayout">
						<div class="pane">
							<el-divider content-position="left" class="divider"><span class="grp">Thông tin mẫu thư</span></el-divider>
							<el-form-item label="Tên mẫu thư" prop="name">
								<el-input v-model="state.ruleForm.name" placeholder="Nhập tên mẫu thư" :maxlength="EntityFieldLength.Length128" show-word-limit clearable />
							</el-form-item>
							<el-row :gutter="10">
								<el-col :span="14">
									<el-form-item label="Mã mẫu thư" prop="code">
										<el-input v-model="state.ruleForm.code" placeholder="Nhập mã mẫu thư" :maxlength="EntityFieldLength.Length64" show-word-limit clearable :disabled="props.operateType === 'edit'" />
									</el-form-item>
								</el-col>
								<el-col :span="10">
									<el-form-item :label="$t('lz.entity.base.status')">
										<el-radio-group v-model="state.ruleForm.status">
											<el-radio v-for="item in statusOptions" :key="item.code" :value="Number(item.code)">{{ item.name }}</el-radio>
										</el-radio-group>
									</el-form-item>
								</el-col>
							</el-row>
							<el-divider content-position="left" class="divider"><span class="grp">{{ $t('lz.label.base.otherInfo') }}</span></el-divider>
							<el-form-item :label="$t('lz.entity.base.orderNo')">
								<el-input-number v-model="state.ruleForm.orderNo" :min="0" :precision="0" class="w100" controls-position="right" />
							</el-form-item>
							<el-form-item :label="$t('lz.entity.base.remark')">
								<el-input v-model="state.ruleForm.remark" type="textarea" :rows="2" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.remark') })" :maxlength="EntityFieldLength.Length256" show-word-limit />
							</el-form-item>
						</div>
					</el-splitter-panel>
					<el-splitter-panel size="67%" min="35%" :resizable="!isCompactLayout">
						<div class="pane">
							<el-form-item label="Nội dung thư" prop="content" class="content-form-item">
								<div class="mail-content-editor">
									<JoiEditor resizable :buttons="templateEditorButtons" :upload-image="uploadTemplateImage" :allow-upload-image="!!props.baseUrl" :plugins="templateEditorPlugins" :exclude-tools="templateEditorExcludeTools" v-if="state.isShowDialog" :key="state.editorKey" v-model:getHtml="editorHtml" mode="simple" height="65vh" :disable="state.isProcessing || props.operateType === 'view'" placeholder="Nhập nội dung thư..." />
								</div>
							</el-form-item>
							<p class="hint">Nội dung mẫu được dùng để soạn thư trong lịch gửi.</p>
						</div>
					</el-splitter-panel>
				</el-splitter>
			</el-form>
			<template #footer>
				<el-button @click="cancel" :disabled="state.isProcessing">{{ $t('lz.button.base.cancel') }}</el-button>
				<el-button v-if="props.operateType !== 'view'" type="primary" :loading="state.isProcessing" @click="submit">Lưu mẫu thư</el-button>
			</template>
		</el-dialog>
	</div>
</template>

<script lang="ts" setup name="mailEditMau">
import { computed, nextTick, reactive, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { ElMessage, ElMessageBox, ElSplitter, ElSplitterPanel } from 'element-plus';
import { useMediaQuery } from '@vueuse/core';
import 'element-plus/theme-chalk/el-splitter.css';
import type { FormInstance, FormRules } from 'element-plus';
import type { OperateType } from '/@/types/constants/operate';
import { EntityFieldLength } from '/@/types/enums/baseEnum';
import { BaseConfigTypeEnum, StatusTypeEnum } from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';
import { getAPI } from '/@/utils/axios-utils';
import { RptEmailTemplateApi, RptEmailScheduleApi } from '/@/api-services/mail';
import type { MailAddTemplateInput, MailUpdateTemplateInput } from '/@/api-services/mail';
import JoiEditor from '/@/components/joditEditor/index.vue';
import { clearMailTemplateCache } from '../../lichGuiMail/component/mailTemplateCache';


interface Props {
	title: string;
	operateType: OperateType;
	baseUrl: string | null;
}
const props = defineProps<Props>();
const isCompactLayout = useMediaQuery('(max-width: 1199px)');
const emits = defineEmits(['handleQuery']);
const { t: $t } = useI18n();
const baseConfig = useBaseConfig();
const statusOptions = computed(() => baseConfig.getConfigDataByCode(BaseConfigTypeEnum.SysStatusType) ?? []);
const ruleFormRef = ref<FormInstance>();
const state = reactive({
	isShowDialog: false,
	isProcessing: false,
	ruleForm: {} as MailUpdateTemplateInput,
	editorKey: 0,
});
const templateEditorButtons: string[] = [
	'undo', 'redo', '|',
	'bold', 'italic', 'underline', 'strikethrough', 'superscript', 'subscript', '|',
	'font', 'fontsize', 'brush', 'paragraph', '|',
	'ul', 'ol', 'outdent', 'indent', 'align', '|',
	'cut', 'copy', 'paste', 'selectall', '|',
	, 'customUploadImage', 'table', 'hr', '|',
	'eraser', 'copyformat',  'fullsize',
];
const templateEditorExcludeTools: string[] = [];
const templateEditorPlugins: string[] = [
	'enter', 'backspace', 'delete-command', 'hotkeys', 'size', 'placeholder',
	'wrap-nodes', 'dtd', 'toolbar-navigation', 'clean-html', 'clipboard', 'paste',
	'bold', 'font', 'color', 'format-block', 'ordered-list', 'indent', 'justify',
	'redo-undo', 'link', 'image', 'video', 'file', 'table', 'hr', 'symbols',
	'search', 'preview', 'print', 'copyformat', 'source', 'fullsize', 'about',
	'inline-popup', 'select-cells', 'resize-cells', 'resizer',
];
let initialForm = '';
const editorHtml = computed({
	get: () => state.ruleForm.content ?? '',
	set: (value: string) => { state.ruleForm.content = value; },
});
const rules: FormRules = {
	code: [
		{ required: true, whitespace: true, message: 'Vui lòng nhập mã mẫu thư', trigger: 'blur' },
		{ max: EntityFieldLength.Length64, message: 'Mã mẫu thư tối đa 64 ký tự', trigger: 'blur' },
	],
	name: [
		{ required: true, whitespace: true, message: 'Vui lòng nhập tên mẫu thư', trigger: 'blur' },
		{ max: EntityFieldLength.Length128, message: 'Tên mẫu thư tối đa 128 ký tự', trigger: 'blur' },
	],
	content: [{ validator: (_rule, value, callback) => {
		const doc = new DOMParser().parseFromString(value ?? '', 'text/html');
		const text = (doc.body.textContent ?? '').replace(/\u00a0/g, ' ').trim();
		if (!text && !doc.body.querySelector('img, table, hr')) callback(new Error('Vui lòng nhập nội dung thư'));
		else callback();
	}, trigger: 'blur' }],
};

const openDialog = (row: any) => {
	state.ruleForm = JSON.parse(JSON.stringify(row ?? {}));

	if (props.operateType === 'copy') {
		state.ruleForm.id = undefined;
		state.ruleForm.code = '';
		state.ruleForm.name = `${state.ruleForm.name || ''} (bản sao)`.slice(0, EntityFieldLength.Length128);
	}
	state.editorKey++;
	initialForm = JSON.stringify(state.ruleForm);
	state.isShowDialog = true;
	ruleFormRef.value?.clearValidate();
}

const beforeClose = async (done: () => void) => {
	if (state.isProcessing) return;
	if (props.operateType !== 'view' && JSON.stringify(state.ruleForm) !== initialForm) {
		try {
			await ElMessageBox.confirm('Nội dung chưa được lưu. Đóng và bỏ các thay đổi?', $t('lz.label.base.warning'), {
				type: 'warning', confirmButtonText: 'Bỏ thay đổi', cancelButtonText: 'Tiếp tục soạn',
			});
		} catch { return; }
	}
	done();
}
const cancel = () => { 
	return beforeClose(() => { state.isShowDialog = false; });
 };

async function submit() {
	if (state.isProcessing || props.operateType === 'view') return;
	state.isProcessing = true;
	try {
		if (!(await ruleFormRef.value?.validate().catch(() => false))) return;
		const payload: MailAddTemplateInput = {
			code: state.ruleForm.code?.trim(), name: state.ruleForm.name?.trim(), content: state.ruleForm.content,
			status: state.ruleForm.status, orderNo: state.ruleForm.orderNo, remark: state.ruleForm.remark,
		};
		if (props.operateType === 'edit') {
			if (!state.ruleForm.id) { ElMessage.error('Thiếu ID template'); return; }
			await getAPI(RptEmailTemplateApi).apiMailRptemailtemplateUpdatePost({ ...payload, id: state.ruleForm.id });
		} else if (props.operateType === 'add' || props.operateType === 'copy') {
			await getAPI(RptEmailTemplateApi).apiMailRptemailtemplateAddPost(payload);
		} else return;
		clearMailTemplateCache();
		state.isShowDialog = false;
		ElMessage.success($t('lz.message.base.processSuccess'));
		emits('handleQuery');
	} catch { /* Keep the draft open when saving fails; API interceptor displays the error. */ }
	finally { state.isProcessing = false; }
}

async function beforeManagerClose(done: () => void) {
	if (state.isProcessing) return;
	if (state.isShowDialog) await beforeClose(() => { state.isShowDialog = false; });
	if (!state.isShowDialog) done();
}

const uploadTemplateImage = async (file: File): Promise<string> => {
	const { data } = await getAPI(RptEmailScheduleApi).apiMailRptemailscheduleUploadimagePostForm(file);
	const url = data.result?.url;
	console.log("uploadTemplateImage url", url, data.result);
	if (!url) throw new Error('Upload ảnh không trả về URL.');
	return new URL(url.replace(/\\/g, '/'), props.baseUrl ?? undefined).href;
	
};
defineExpose({ openDialog, beforeManagerClose });
</script>

<style scoped lang="scss" src="../../styles/mailEditForm.scss"></style>

<style scoped lang="scss">
.content-form-item :deep(.el-form-item__content) { display: block; }
</style>
