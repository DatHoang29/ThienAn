<template>
	<div class="edit-opconfig-container">
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
							:label="$t('lz.entity.sysOpConfig.groupCode')"
							prop="groupCode"
							:rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.sysOpConfig.groupCode') }), trigger: 'blur' }]"
						>
							<el-select v-model="state.editForm.groupCode" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.sysOpConfig.groupCode') })" class="w100">
								<el-option v-for="item in state.groupCodeList" :key="item.code" :label="item.name" :value="item.code" />
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
								{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.sysOpConfig.value') }), trigger: 'blur' },
								{
									max: EntityFieldLength.Length128,
									message: $t('lz.validation.base.maxlength', { PropertyName: $t('lz.entity.sysOpConfig.value'), MaxLength: EntityFieldLength.Length128 }),
									trigger: 'blur',
								},
							]"
						>
							<el-input
								v-model="state.editForm.value"
								:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.sysOpConfig.value') })"
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
								{ max: EntityFieldLength.Length64, message: $t('lz.validation.base.maxlength', { PropertyName: $t('lz.entity.base.name'), MaxLength: EntityFieldLength.Length64 }), trigger: 'blur' },
							]"
						>
							<el-input
								v-model="state.editForm.name"
								:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.name') })"
								:maxlength="EntityFieldLength.Length64"
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
						<el-form-item
							:label="$t('lz.entity.sysOpConfig.isSysConfig')"
							prop="isSysConfig"
							:rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.sysOpConfig.isSysConfig') }), trigger: 'blur' }]"
						>
							<el-radio-group v-model="state.editForm.isSysConfig" v-for="item in state.booleanList" :key="item.code">
								<el-radio :value="item.code" class="mr5">{{ item.name }}</el-radio>
							</el-radio-group>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item :label="$t('lz.entity.base.remark')" prop="remark">
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
import { SysOpConfigApi } from '/@/api-services/cfgSystem';
import { UpdateSysOpConfigInput } from '/@/api-services/cfgSystem';
import { BaseConfigTypeEnum } from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;

const { t: $t } = useI18n();
interface Props {
	operateType: OperateType;
	title: string;
}

const props = defineProps<Props>();
const emits = defineEmits(['handleQuery']);

const editFormRef = ref();
const state = reactive({
	isShowDialog: false,
	isProcessing: false,
	editForm: {} as UpdateSysOpConfigInput,
	groupCodeList: [] as any,
	statusList: [] as any,
});

import { EntityFieldLength, SysConfigTypeCodeEnum } from '/@/types/enums/baseEnum';

onMounted(async () => {
	state.groupCodeList = getConfigDataByCode(BaseConfigTypeEnum.SysOpConfigType);
	state.statusList = getConfigDataByCode(BaseConfigTypeEnum.SysStatusType);
	state.booleanList = getConfigDataByCode(BaseConfigTypeEnum.SysBooleanType);
	state.booleanList.forEach((item: any) => {
		item.code = item.code.toString() == 'true';
	});
});

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
				await getAPI(SysOpConfigApi).apiCfgsystemSysopconfigUpdatePost(state.editForm);
			}
			//Copy or Add
			else if (props.operateType == 'copy' || props.operateType == 'add') {
				state.editForm.id = '';

				await getAPI(SysOpConfigApi).apiCfgsystemSysopconfigAddPost(state.editForm);
			}

			ElMessage.success($t('lz.message.base.processSuccess'));

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
