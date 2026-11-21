 <template>
	<div class="edit-configdata-container">
		<el-dialog v-model="state.isShowDialog" draggable :close-on-click-modal="false" width="750px">
			<template #header>
				<div style="color: #fff">
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Edit /> </el-icon>
					<span> {{ props.title }} </span>
				</div>
			</template>
			<el-form :model="state.editForm" ref="editFormRef" label-width="auto" label-position="left">
				<el-row :gutter="10">
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item
							prop="name"
							:label="$t('lz.entity.base.name')"
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
					<el-col :xs="24" :sm="12" :md="24" :lg="24" :xl="24" class="mb20">
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
							:label="$t('lz.entity.sysConfigData.value')"
							prop="value"
							:rules="[
								{
									max: EntityFieldLength.Length128,
									message: $t('lz.validation.base.maxlength', { PropertyName: $t('lz.entity.sysConfigData.value'), MaxLength: EntityFieldLength.Length128 }),
									trigger: 'blur',
								},
							]"
						>
							<el-input
								v-model="state.editForm.value"
								:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.sysConfigData.value') })"
								:maxlength="EntityFieldLength.Length256"
								show-word-limit
								clearable
							/>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item :label="$t('lz.entity.sysConfigData.tagType')" prop="tabType">
							<el-radio-group v-model="state.editForm.tagType">
								<el-radio value="primary"
									><el-tag type="primary">{{ $t('lz.label.base.primary') }}</el-tag></el-radio
								>
								<el-radio value="success"
									><el-tag type="success">{{ $t('lz.label.base.success') }}</el-tag></el-radio
								>
								<el-radio value="info"
									><el-tag type="info">{{ $t('lz.label.base.info') }}</el-tag></el-radio
								>
								<el-radio value="warning"
									><el-tag type="warning">{{ $t('lz.label.base.warning') }}</el-tag></el-radio
								>
								<el-radio value="danger"
									><el-tag type="danger">{{ $t('lz.label.base.danger') }}</el-tag></el-radio
								>
							</el-radio-group>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item
							:label="$t('lz.entity.sysConfigData.styleSetting')"
							prop="styleSetting"
							:rules="[
								{
									max: EntityFieldLength.Length64,
									message: $t('lz.validation.base.maxlength', { PropertyName: $t('lz.entity.sysConfigData.styleSetting'), MaxLength: EntityFieldLength.Length512 }),
									trigger: 'blur',
								},
							]"
						>
							<el-input
								v-model="state.editForm.styleSetting"
								:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.sysConfigData.styleSetting') })"
								:maxlength="EntityFieldLength.Length512"
								show-word-limit
								clearable
							/>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item
							:label="$t('lz.entity.sysConfigData.classSetting')"
							prop="classSetting"
							:rules="[
								{ max: EntityFieldLength.Length512, message: $t('lz.validation.base.maxlength', { PropertyName: $t('lz.entity.base.name'), MaxLength: EntityFieldLength.Length512 }), trigger: 'blur' },
							]"
						>
							<el-input
								v-model="state.editForm.classSetting"
								:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.sysConfigData.classSetting') })"
								:maxlength="EntityFieldLength.Length512"
								show-word-limit
								clearable
							/>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.base.status')" prop="status">
							<el-radio-group v-model="state.editForm.status" v-for="item in getConfigDataByCode(BaseConfigTypeEnum.SysStatusType)" :key="item.code">
								<el-radio :value="parseInt(item.code)" class="mr5">{{item.name}}</el-radio>
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
							:label="$t('lz.entity.base.remark')"
							prop="remark"
							:rules="[
								{
									max: EntityFieldLength.Length256,
									message: $t('lz.validation.base.maxlength', { PropertyName: $t('lz.entity.base.remark'), MaxLength: EntityFieldLength.Length256 }),
									trigger: 'blur',
								},
							]"
						>
							<el-input
								v-model="state.editForm.remark"
								:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.remark') })"
								type="textarea"
								rows="4"
								:maxlength="EntityFieldLength.Length256"
								show-word-limit
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
import { reactive, ref } from 'vue';
import { ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';

import { getAPI } from '/@/utils/axios-utils';
import { OperateType } from '/@/types/constants';

import { SysConfigDataApi } from '/@/api-services/cfgSystem';
import { UpdateSysConfigDataInput } from '/@/api-services/cfgSystem';
import { EntityFieldLength } from '/@/types/enums/baseEnum';
import { BaseConfigTypeEnum } from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;

const { t: $t } = useI18n();

interface Props {
	operateType: OperateType;
	title: string;
}

const props = defineProps<Props>();
const emits = defineEmits(['handleQuery', 'handleUpdate']);
const editFormRef = ref();

const state = reactive({
	isShowDialog: false,
	isProcessing: false,
	editForm: {} as UpdateSysConfigDataInput,
});

const openDialog = (row: any) => {
	editFormRef.value?.resetFields();

	state.editForm = JSON.parse(JSON.stringify(row));

	if ((state.editForm?.configTypeId?.length ?? 0) == 0) state.editForm.configTypeId = props.configTypeId;

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
				await getAPI(SysConfigDataApi).apiCfgsystemSysconfigdataUpdatePost(state.editForm);
			}
			//Copy or Add
			else if (props.operateType == 'copy' || props.operateType == 'add') {
				state.editForm.id = '';
				await getAPI(SysConfigDataApi).apiCfgsystemSysconfigdataAddPost(state.editForm);
			}

			ElMessage.success($t('lz.message.base.processSuccess'));

			emits('handleUpdate');

			closeDialog();
		} catch (error) {
		} finally {
			state.isProcessing = false;
		}
	});
};

defineExpose({ openDialog });
</script>
