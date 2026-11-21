<template>
	<div class="sys-pos-container">
		<el-dialog v-model="state.isShowDialog" draggable :close-on-click-modal="false" width="700px">
			<template #header>
				<div style="color: #fff">
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Edit />
					</el-icon>
					<span> {{ props.title }} </span>
				</div>
			</template>
			<el-form :model="state.ruleForm" ref="ruleFormRef" label-width="auto" label-position="left">
				<el-row :gutter="10">
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item :label="$t('lz.entity.rptEmailReport.pluginCode')" prop="pluginCode" :rules="[
							{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.rptEmailReport.pluginCode') }), trigger: 'change' },
						]">
							<el-select v-model="state.ruleForm.pluginCode" filterable class="w100"
								:placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.rptEmailReport.pluginCode') })">
								<el-option v-for="p in props.plugins" :key="p.code!" :label="p.name || p.code"
									:value="p.code!">
									<span>{{ p.name }} - {{ p.code }}</span>
								</el-option>
							</el-select>
						</el-form-item>
					</el-col>

					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item :label="$t('lz.entity.rptEmailReport.code')" prop="code" :rules="[
							{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.rptEmailReport.code') }), trigger: 'blur' },
							{ max: EntityFieldLength.Length64, message: $t('lz.validation.base.maxlength', { PropertyName: $t('lz.entity.rptEmailReport.code'), MaxLength: EntityFieldLength.Length64 }), trigger: 'blur' },
						]">
							<el-input v-model="state.ruleForm.code"
								:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.rptEmailReport.code') })"
								:maxlength="EntityFieldLength.Length64" clearable />
							<div class="hint">{{ $t('lz.label.rptEmailReport.codeHint') }}</div>
						</el-form-item>
					</el-col>

					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item :label="$t('lz.entity.rptEmailReport.name')" prop="name" :rules="[
							{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.rptEmailReport.name') }), trigger: 'blur' },
							{ max: EntityFieldLength.Length128, message: $t('lz.validation.base.maxlength', { PropertyName: $t('lz.entity.rptEmailReport.name'), MaxLength: EntityFieldLength.Length128 }), trigger: 'blur' },
						]">
							<el-input v-model="state.ruleForm.name"
								:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.rptEmailReport.name') })"
								:maxlength="EntityFieldLength.Length128" clearable />
						</el-form-item>
					</el-col>

					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.base.status')">
							<el-radio-group v-model="state.ruleForm.status">
								<el-radio v-for="item in getConfigDataByCode(BaseConfigTypeEnum.SysStatusType)"
									:key="item.code" :value="parseInt(item.code)" class="mr5">{{ item.name }}</el-radio>
							</el-radio-group>
						</el-form-item>
					</el-col>

					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.base.orderNo')" prop="orderNo">
							<el-input-number v-model="state.ruleForm.orderNo" :min="0" class="w100"
								controls-position="right" />
						</el-form-item>
					</el-col>

					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<!-- Chưa dùng ở backend — chỉ kiểm tra là JSON hợp lệ để dữ liệu nhập sẵn không hỏng về sau -->
						<el-form-item :label="$t('lz.entity.rptEmailReport.detailParams')" prop="detailParams"
							:rules="[{ validator: jsonValidator, trigger: 'blur' }]">
							<el-input v-model="state.ruleForm.detailParams" type="textarea" :rows="4" class="mono"
								:placeholder="$t('lz.label.rptEmailReport.detailParamsHint')" />
						</el-form-item>
					</el-col>

					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item :label="$t('lz.entity.base.remark')" prop="remark" :rules="[
							{ max: EntityFieldLength.Length256, message: $t('lz.validation.base.maxlength', { PropertyName: $t('lz.entity.base.remark'), MaxLength: EntityFieldLength.Length256 }), trigger: 'blur' },
						]">
							<el-input v-model="state.ruleForm.remark" type="textarea" :rows="2"
								:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.remark') })"
								:maxlength="EntityFieldLength.Length256" show-word-limit />
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>
			<template #footer>
				<span class="dialog-footer">
					<el-button @click="cancel">{{ $t('lz.button.base.cancel') }}</el-button>
					<el-button type="primary" @click="submit" :disabled="state.isProcessing">{{
						$t('lz.button.base.confirm') }}</el-button>
				</span>
			</template>
		</el-dialog>
	</div>
</template>

<script lang="ts" setup name="mailEditBaoCaoGuiMail">
import { reactive, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { ElMessage } from 'element-plus';
import { getAPI } from '/@/utils/axios-utils';
import { OperateType } from '/@/types/constants';
import { EntityFieldLength } from '/@/types/enums/baseEnum';
import { BaseConfigTypeEnum } from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';

import { RptEmailReportApi } from '/@/api-services/mail';
import type { MailRptUpdateReportInput } from '/@/api-services/mail';
import type { PluginOutput } from '/@/api-services/system';

interface Props {
	/** the type of operation */
	operateType: OperateType;
	title: string;
	/** Danh sách nhóm (SysPlugin) — trang cha nạp sẵn. */
	plugins: PluginOutput[];
}
const props = defineProps<Props>();
const emits = defineEmits(['handleQuery']);

const { t: $t } = useI18n();
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;

const ruleFormRef = ref();
const state = reactive({
	isShowDialog: false,
	isProcessing: false,
	ruleForm: {} as MailRptUpdateReportInput,
});

const jsonValidator = (_rule: unknown, value: string, callback: (error?: Error) => void) => {
	if (!value?.trim()) return callback();
	try {
		JSON.parse(value);
		callback();
	} catch {
		callback(new Error($t('lz.validation.rptEmailReport.invalidJson')));
	}
};

// Open dialog: reset validate trước rồi mới gán dữ liệu, tránh giữ lỗi của lần mở trước
const openDialog = (row: any) => {
	ruleFormRef.value?.resetFields();
	state.ruleForm = JSON.parse(JSON.stringify(row ?? {}));
	state.isShowDialog = true;
};

// Close dialog
const closeDialog = () => {
	emits('handleQuery');
	state.isShowDialog = false;
};

// Cancel
const cancel = () => {
	state.isShowDialog = false;
};

// Submit
const submit = () => {
	ruleFormRef.value.validate(async (valid: boolean) => {
		if (!valid) return;
		try {
			state.isProcessing = true;
			if (props.operateType == 'edit') {
				await getAPI(RptEmailReportApi).apiMailRptemailreportUpdatePost(state.ruleForm);
			} else if (props.operateType == 'copy' || props.operateType == 'add') {
				state.ruleForm.id = '';
				await getAPI(RptEmailReportApi).apiMailRptemailreportAddPost(state.ruleForm);
			}
			ElMessage.success($t('lz.message.base.saveSuccess'));
			closeDialog();
		} catch (error) {
		} finally {
			state.isProcessing = false;
		}
	});
};

// Export object
defineExpose({ openDialog });
</script>

<style scoped lang="scss">
.hint {
	font-size: 12px;
	color: var(--el-text-color-secondary);
	line-height: 1.5;
}
.opt-sub {
	float: right;
	margin-left: 12px;
	font-size: 12px;
	color: var(--el-text-color-secondary);
}
.mono :deep(textarea) {
	font-family: Consolas, 'Courier New', monospace;
}
</style>
