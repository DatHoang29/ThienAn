<template>
	<div class="edit-opconfig-container">
		<el-dialog v-model="state.isShowDialog" draggable :close-on-click-modal="false" :before-close="beforeClose" width="700px">
			<template #header>
				<div style="color: #fff">
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"><ele-Edit /></el-icon>
					<span> {{ props.title }} </span>
				</div>
			</template>
			<el-form :model="state.editForm" ref="editFormRef" label-width="auto" label-position="left" :disabled="state.isProcessing">
				<el-row :gutter="10">
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item
							:label="$t('lz.entity.sysOpConfig.groupCode')"
							prop="groupCode"
							:rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.sysOpConfig.groupCode') }), trigger: 'blur' }]"
						>
							<el-select
								v-model="state.editForm.groupCode"

								:placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.sysOpConfig.groupCode') })"
								class="w100"
							>
								<el-option v-for="item in props.groups" :key="item.code" :label="item.name" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item
							:label="$t('lz.entity.base.code')"
							prop="code"
							:rules="[
								{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.base.code') }), trigger: 'blur' },
								{ max: EntityFieldLength.Length128, message: $t('lz.validation.base.maxlength', { PropertyName: $t('lz.entity.base.code'), MaxLength: EntityFieldLength.Length128 }), trigger: 'blur' },
							]"
						>
						<el-input
								v-model="state.editForm.code"

								:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.code') })"
								:maxlength="EntityFieldLength.Length128"
								show-word-limit
								clearable
							/>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item
							:label="$t('lz.entity.sysOpConfig.value')"
							prop="value"
							:rules="[
								{
									max: EntityFieldLength.Length256,
									message: $t('lz.validation.base.maxlength', { PropertyName: $t('lz.entity.sysOpConfig.value'), MaxLength: EntityFieldLength.Length256 }),
									trigger: 'blur',
								},
							]"
						>
							<el-input
								v-model="state.editForm.value"
								:type="isSecret(state.editForm.code) ? 'password' : 'text'"
								:show-password="isSecret(state.editForm.code)"
								:placeholder="isSecret(state.editForm.code) && state.editForm.id ? $t('lz.label.vdsConfig.keepSecret') : $t('lz.label.base.pleaseType', { input: $t('lz.entity.sysOpConfig.value') })"
								:maxlength="EntityFieldLength.Length256"
								show-word-limit
								clearable
							/>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item
							:label="$t('lz.entity.base.name')"
							prop="name"
							:rules="[
								{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.base.name') }), trigger: 'blur' },
								{ max: EntityFieldLength.Length256, message: $t('lz.validation.base.maxlength', { PropertyName: $t('lz.entity.base.name'), MaxLength: EntityFieldLength.Length256 }), trigger: 'blur' },
							]"
						>
							<el-input
								v-model="state.editForm.name"
								:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.name') })"
								:maxlength="EntityFieldLength.Length256"
								show-word-limit
								clearable
							/>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.base.status')" prop="status">
							<el-radio-group v-model="state.editForm.status">
								<el-radio v-for="item in getConfigDataByCode(BaseConfigTypeEnum.SysStatusType)" :key="item.code" :value="parseInt(item.code)" class="mr5">{{ item.name }}</el-radio>
							</el-radio-group>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.base.orderNo')" prop="orderNo">
							<el-input-number v-model="state.editForm.orderNo" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.orderNo') })" class="w100" />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item
							:label="$t('lz.entity.sysOpConfig.isSysConfig')"
							prop="isSysConfig"
							:rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.sysOpConfig.isSysConfig') }), trigger: 'blur' }]"
						>
							<el-radio-group v-model="state.editForm.isSysConfig">
								<el-radio v-for="item in booleanList" :key="String(item.code)" :value="item.code" class="mr5">{{ item.name }}</el-radio>
							</el-radio-group>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item
							:label="$t('lz.entity.base.remark')"
							prop="remark"
							:rules="[
								{
									max: EntityFieldLength.Length1024,
									message: $t('lz.validation.base.maxlength', { PropertyName: $t('lz.entity.base.remark'), MaxLength: EntityFieldLength.Length1024 }),
									trigger: 'blur',
								},
							]"
						>
							<el-input
								v-model="state.editForm.remark"
								type="textarea"
								:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.remark') })"
								rows="4"
								:maxlength="EntityFieldLength.Length1024"
								show-word-limit
								clearable
							/>
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>
			<template #footer>
				<span class="dialog-footer">
					<el-button @click="cancel" :disabled="state.isProcessing">{{ $t('lz.button.base.cancel') }}</el-button>
					<el-button type="primary" @click="submit" :disabled="state.isProcessing">{{ $t('lz.button.base.confirm') }}</el-button>
				</span>
			</template>
		</el-dialog>
	</div>
</template>

<script lang="ts" setup name="vdsEditConfig">
import { reactive, ref, nextTick } from 'vue';
import { getAPI } from '/@/utils/axios-utils';
import { VdsConfigApi } from '/@/api-services/vds';
import type { VdsConfigOutput, AddVdsConfigInput, UpdateVdsConfigInput, VdsConfigGroupOutput } from '/@/api-services/vds';
import type { OperateType } from '/@/types/constants';
import { ElMessage, type FormInstance } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { BaseConfigTypeEnum } from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';
import { EntityFieldLength } from '/@/types/enums/baseEnum';
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
const { t: $t } = useI18n();
const props = defineProps<{ operateType: OperateType; title: string; groups: VdsConfigGroupOutput[] }>();
const emits = defineEmits(['handleQuery']);
const editFormRef = ref<FormInstance>();
const booleanList = getConfigDataByCode(BaseConfigTypeEnum.SysBooleanType).map((item: any) => ({ ...item, code: String(item.code) === 'true' }));
const state = reactive({ isShowDialog: false, isProcessing: false, editForm: {} as VdsConfigOutput });
const isSecret = (code?: string | null) => /password|token|secret|api_key/i.test(code ?? '');
async function openDialog(row: VdsConfigOutput) {
	state.editForm = { ...row, value: row.id && isSecret(row.code) ? '' : row.value, isSysConfig: !!row.isSysConfig };
	state.isShowDialog = true;
	await nextTick();
	editFormRef.value?.clearValidate();
}
function cancel() {
	if (!state.isProcessing) state.isShowDialog = false;
}
function beforeClose(done: () => void) {
	if (!state.isProcessing) done();
}
async function submit() {
	if (state.isProcessing || !(await editFormRef.value?.validate().catch(() => false))) return;
	state.isProcessing = true;
	try {
		const f = state.editForm;
		const payload: AddVdsConfigInput | UpdateVdsConfigInput = {
			groupCode: f.groupCode,
			code: f.code,
			name: f.name,
			...(!isSecret(f.code) || f.value ? { value: f.value } : {}),
			status: f.status,
			orderNo: f.orderNo,
			remark: f.remark,
			isSysConfig: f.isSysConfig,
		};
		const api = getAPI(VdsConfigApi);
		const response = props.operateType === 'edit'
			? await api.apiVdsVdsconfigUpdatePost({ ...payload, id: f.id })
			: await api.apiVdsVdsconfigAddPost(payload);
		if (response.data.succeeded === false || !response.data.result) throw new Error(response.data.message || $t('lz.message.vdsConfig.saveFailed'));
		ElMessage.success($t('lz.message.base.processSuccess'));
		state.isShowDialog = false;
		emits('handleQuery');
	} catch (cause) {
		ElMessage.error(cause instanceof Error ? cause.message : $t('lz.message.vdsConfig.saveFailed'));
	} finally {
		state.isProcessing = false;
	}
}
defineExpose({ openDialog });
</script>
