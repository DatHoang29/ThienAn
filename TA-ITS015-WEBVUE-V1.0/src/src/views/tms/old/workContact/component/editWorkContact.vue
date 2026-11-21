<template>
	<div class="edit-workcontact-container">
		<el-dialog v-model="state.isShowDialog" draggable :close-on-click-modal="false" width="700px">
			<template #header>
				<div style="color: #fff">
					<el-icon size="16"
						style="margin-right: 3px; display: inline; vertical-align: middle"><ele-Edit /></el-icon>
					<span> {{ props.title }} </span>
				</div>
			</template>
			<el-form :model="state.editForm" ref="editFormRef" label-width="auto" label-position="left">
				<el-row :gutter="10">
					<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.base.code')" prop="code"
						:rules="[{ required: true, message: $t('lz.validation.base.required', {PropertyName: $t('lz.entity.base.code')}), trigger: 'blur' }, { max: EntityFieldLength.Length64, message: $t('lz.validation.base.maxlength', {PropertyName: $t('lz.entity.base.code'), MaxLength: EntityFieldLength.Length64}), trigger: 'blur'}]">
							<el-input v-model="state.editForm.code" :placeholder="$t('lz.label.base.pleaseType', {input: $t('lz.entity.base.code')})" :maxlength="EntityFieldLength.Length64" show-word-limit clearable />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.base.name')" prop="name":rules="[{ required: true, message: $t('lz.validation.base.required', {PropertyName: $t('lz.entity.base.name')}), trigger: 'blur' }, { max: EntityFieldLength.Length128, message: $t('lz.validation.base.maxlength', {PropertyName: $t('lz.entity.base.name'), MaxLength: EntityFieldLength.Length128}), trigger: 'blur'}]">
							<el-input v-model="state.editForm.name" :placeholder="$t('lz.label.base.pleaseType', {input: $t('lz.entity.base.name')})" :maxlength="EntityFieldLength.Length128" show-word-limit clearable />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.base.type')" prop="type" :rules="[{ required: true, message: $t('lz.validation.base.required', {PropertyName: $t('lz.entity.base.type')}), trigger: 'blur' }]">
							<el-select v-model="state.editForm.type" @change="onWorkTypeChange" disabled class="w100">
								<el-option v-for="item in state.contactTypeList" :label="item.name" :value="item.code" :disabled="item.code != TmsWorkContactType.Other" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.base.phone')" prop="phone" :rules="[{ max: EntityFieldLength.Length32, message: $t('lz.validation.base.maxlength', {PropertyName: $t('lz.entity.base.phone'), MaxLength: EntityFieldLength.Length32}), trigger: 'blur'}]">
							<el-input v-model="state.editForm.phone" :placeholder="$t('lz.label.base.pleaseType', {input: $t('lz.entity.base.phone')})" :maxlength="EntityFieldLength.Length32" show-word-limit clearable />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.base.status')" prop="status">
							<el-radio-group v-model="state.editForm.status" v-for="item in getConfigDataByCode(BaseConfigTypeEnum.SysStatusType)" :key="item.code">
								<el-radio :value="parseInt(item.code)" class="mr5">{{ item.name }}</el-radio>
							</el-radio-group>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item :label="$t('lz.entity.base.address')" prop="address" :rules="[{ max: EntityFieldLength.Length128, message: $t('lz.validation.base.maxlength', {PropertyName: $t('lz.entity.base.address'), MaxLength: EntityFieldLength.Length128}), trigger: 'blur'}]">
							<el-input v-model="state.editForm.address" :placeholder="$t('lz.label.base.pleaseType', {input: $t('lz.entity.base.address')})" :maxlength="EntityFieldLength.Length128" show-word-limit clearable />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item :label="$t('lz.entity.base.info')" prop="info" :rules="[{ max: EntityFieldLength.Length128, message: $t('lz.validation.base.maxlength', {PropertyName: $t('lz.entity.base.info'), MaxLength: EntityFieldLength.Length128}), trigger: 'blur'}]">
							<el-input v-model="state.editForm.info" :placeholder="$t('lz.label.base.pleaseType', {input: $t('lz.entity.base.info')})" type="textarea" rows="4" :maxlength="EntityFieldLength.Length128" show-word-limit clearable />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item :label="$t('lz.entity.base.remark')" prop="remark" :rules="[{ max: EntityFieldLength.Length256, message: $t('lz.validation.base.maxlength', {PropertyName: $t('lz.entity.base.remark'), MaxLength: EntityFieldLength.Length256}), trigger: 'blur'}]">
							<el-input v-model="state.editForm.remark" :placeholder="$t('lz.label.base.pleaseType', {input: $t('lz.entity.base.address')})" type="textarea" rows="4" :maxlength="EntityFieldLength.Length256" show-word-limit clearable />
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

<script lang="ts" setup name="tmsEditWorkContact">

import { reactive, ref, onMounted } from 'vue';
import { getAPI } from '/@/utils/axios-utils';
import { OperateType } from '/@/types/constants';
import { ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';

import { TmsWorkContactApi } from '/@/api-services/tms';
import { UpdateTmsWorkContactInput } from '/@/api-services/tms';
import { TmsWorkContactType } from '/@/types/enums';

import { useBaseConfig } from '/@/stores/baseConfig';
import { BaseConfigTypeEnum } from '/@/types/enums';

const getConfigDataByCode = useBaseConfig().getConfigDataByCode;

import { EntityFieldLength } from '/@/types/enums/baseEnum';

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
	editForm: {} as UpdateTmsWorkContactInput,
	contactTypeList: [] as any,
	statusList: [] as any
});

onMounted(async () => {
	state.contactTypeList = getConfigDataByCode(BaseConfigTypeEnum.TmsWorkContactType);
	state.statusList = getConfigDataByCode(BaseConfigTypeEnum.SysStatusType);
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

const onWorkTypeChange = () => { }

const submit = () => {
	editFormRef.value.validate(async (valid: boolean) => {

		if (!valid) return;

		try{
			state.isProcessing = true;

			//Edit
			if (props.operateType == 'edit') {
				await getAPI(TmsWorkContactApi).apiTmsTmsworkcontactUpdatePost(state.editForm);
			}
			//Copy or Add
			else if (props.operateType == 'copy' || props.operateType == 'add') {
				state.editForm.id = '';

				await getAPI(TmsWorkContactApi).apiTmsTmsworkcontactAddPost(state.editForm);
			}

			ElMessage.success($t('lz.message.base.processSuccess'));

			closeDialog();
		}
		catch(error){}
		finally{
			state.isProcessing = false;
		}
	});
};

defineExpose({ openDialog });

</script>
