<template>
	<div class="edit-terminology-container">
		<el-dialog v-model="state.isShowDialog" draggable :close-on-click-modal="false" width="700px">
			<template #header>
				<div style="color: #fff">
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"><ele-Edit /></el-icon>
					<span> {{ props.title }} </span>
				</div>
			</template>
			<el-form :model="state.editForm" ref="editFormRef" label-width="auto" label-position="left">
				<el-row :gutter="10">
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item
							:label="$t('lz.entity.base.lang')"
							prop="lang"
							:rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.base.lang') }), trigger: 'blur' }]"
						>
							<el-select v-model="state.editForm.lang" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.base.lang') })" class="w100">
								<el-option v-for="item in getConfigDataByCode(BaseConfigTypeEnum.SysLanguageType)" :label="item.name" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item
							:label="$t('lz.entity.base.name')"
							prop="name"
							:rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.base.name') }), trigger: 'blur' }]"
						>
							<el-select v-model="state.editForm.name" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.base.name') })" class="w100">
								<el-option v-for="item in getConfigDataByCode(BaseConfigTypeEnum.SysI18nPrefix)" :label="item.name" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item
							:label="$t('lz.entity.base.code')"
							prop="code"
							:rules="[
								{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.base.code') }), trigger: 'blur' },
								{ max: EntityFieldLength.Length256, message: $t('lz.validation.base.maxlength', { PropertyName: $t('lz.entity.base.code'), MaxLength: EntityFieldLength.Length256 }), trigger: 'blur' },
							]"
						>
							<el-input
								v-model="state.editForm.code"
								:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.code') })"
								:maxlength="EntityFieldLength.Length256"
								show-word-limit
								clearable
							/>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item
							:label="$t('lz.entity.sysTerminolog.value')"
							prop="value"
							:rules="[
								{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.sysTerminolog.value') }), trigger: 'blur' },
								{
									max: EntityFieldLength.Length256,
									message: $t('lz.validation.base.maxlength', { PropertyName: $t('lz.entity.sysTerminolog.value'), MaxLength: EntityFieldLength.Length256 }),
									trigger: 'blur',
								},
							]"
						>
							<el-input
								v-model="state.editForm.value"
								:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.sysTerminolog.value') })"
								:maxlength="EntityFieldLength.Length256"
								show-word-limit
								clearable
							/>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.base.status')" prop="status">
							<el-radio-group v-model="state.editForm.status" v-for="item in getConfigDataByCode(BaseConfigTypeEnum.SysStatusType)" :key="item.code">
								<el-radio :value="parseInt(item.code)" class="mr5">{{ item.name }}</el-radio>
							</el-radio-group>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.base.orderNo')" prop="orderNo">
							<el-input-number v-model="state.editForm.orderNo" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.orderNo') })" class="w100" />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item :label="$t('lz.entity.base.remark')">
							<el-input
								v-model="state.editForm.remark"
								type="textarea"
								:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.remark') })"
								prop="remark"
								:rules="[
									{
										max: EntityFieldLength.Length1024,
										message: $t('lz.validation.base.maxlength', { PropertyName: $t('lz.entity.base.remark'), MaxLength: EntityFieldLength.Length1024 }),
										trigger: 'blur',
									},
								]"
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
					<el-button @click="cancel">{{ $t('lz.button.base.cancel') }}</el-button>
					<el-button type="primary" @click="submit" :disabled="state.isProcessing">{{ $t('lz.button.base.confirm') }}</el-button>
				</span>
			</template>
		</el-dialog>
	</div>
</template>

<script lang="ts" setup>
import { reactive, ref, onMounted } from 'vue';
import { getAPI } from '/@/utils/axios-utils';
import { OperateType } from '/@/types/constants';
import { ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';

import { SysTerminologyApi } from '/@/api-services/cfgSystem';
import { UpdateSysTerminologyInput } from '/@/api-services/cfgSystem';
import { useBaseConfig } from '/@/stores/baseConfig';
import { BaseConfigTypeEnum } from '/@/types/enums';
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
const { t: $t } = useI18n();
interface Props {
	operateType: OperateType;
	title: string;
}
import { EntityFieldLength } from '/@/types/enums/baseEnum';

const props = defineProps<Props>();
const emits = defineEmits(['handleQuery']);

const editFormRef = ref();
const state = reactive({
	isShowDialog: false,
	isProcessing: false,
	editForm: {} as UpdateSysTerminologyInput,
	i18nPrefixList: [] as any,
});

onMounted(async () => {});

const openDialog = (row: any) => {
	editFormRef.value?.resetFields();
	state.editForm = JSON.parse(JSON.stringify(row));
	state.isShowDialog = true;
};

const closeDialog = () => {
	emits('handleQuery');
	state.isShowDialog = false;
};

const cancel = () => {
	state.isShowDialog = false;
};

const submit = () => {
	editFormRef.value.validate(async (valid: boolean) => {
		if (!valid) return;

		try {
			state.isProcessing = true;

			//Edit
			if (props.operateType == 'edit') {
				await getAPI(SysTerminologyApi).apiCfgsystemSysterminologyUpdatePost(state.editForm);
			}
			//Copy or Add
			else if (props.operateType == 'copy' || props.operateType == 'add') {
				state.editForm.id = '';

				await getAPI(SysTerminologyApi).apiCfgsystemSysterminologyAddPost(state.editForm);
			}

			ElMessage.success($t('lz.message.base.processSuccess'));

			closeDialog();
		} catch (error) {
		} finally {
			state.isProcessing = false;
		}
	});
};

defineExpose({ openDialog });
</script>
