<template>
	<div class="sys-pos-container">
		<el-dialog v-model="state.isShowDialog" draggable :close-on-click-modal="false" width="700px">
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
							:label="$t('lz.entity.base.name')"
							prop="name"
							:rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.base.name') }), trigger: 'blur' }]"
						>
							<el-input v-model="state.editForm.name" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.name') })" clearable />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item
							:label="$t('lz.entity.base.code')"
							prop="code"
							:rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.base.code') }), trigger: 'blur' }]"
						>
							<el-input v-model="state.editForm.code" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.code') })" clearable />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.base.status')">
							<el-radio-group v-model="state.editForm.status" v-for="item in getConfigDataByCode(BaseConfigTypeEnum.SysStatusType)" :key="item.value">
								<el-radio :value="parseInt(item.code)" class="mr5">{{ item.name }}</el-radio>
							</el-radio-group>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item :label="$t('lz.entity.base.remark')">
							<el-input v-model="state.editForm.remark" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.remark') })" clearable type="textarea" />
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>
			<template #footer>
				<span class="dialog-footer">
					<el-button @click="cancel">{{ $t('lz.button.base.cancel') }}</el-button>
					<el-button type="primary" :disabled="state.isProcessing" @click="submit">{{ $t('lz.button.base.confirm') }}</el-button>
				</span>
			</template>
		</el-dialog>
	</div>
</template>

<script lang="ts" setup name="sysEditPos">
import { reactive, ref } from 'vue';
import { getAPI } from '/@/utils/axios-utils';
import { SysPosApi } from '/@/api-services/system';
import { UpdatePosInput } from '/@/api-services/system';
import { OperateType } from '/@/types/constants';
import { ElMessage } from 'element-plus';

import { BaseConfigTypeEnum } from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;

import { useI18n } from 'vue-i18n';
const { t: $t } = useI18n();

interface Props {
	/** the type of operation */
	operateType: OperateType;
	title: string;
}

const props = defineProps<Props>();
const emits = defineEmits(['handleQuery']);

const editFormRef = ref();
const state = reactive({
	isShowDialog: false,
	isProcessing: false,
	editForm: {} as UpdatePosInput,
});

// Open dialog
const openDialog = (row: any) => {
	editFormRef.value?.resetFields();
	state.editForm = JSON.parse(JSON.stringify(row));
	state.isShowDialog = true;
};

// Close dialog
const closeDialog = () => {
	emits('handleQuery');
	state.isShowDialog = false;
};

// Cancel
const cancel = () => {
	if (state.isProcessing) return;
	state.isShowDialog = false;
};

// Submit
const submit = () => {
	if (state.isProcessing) return;
	editFormRef.value.validate(async (valid: boolean) => {
		if (!valid || state.isProcessing) return;
		state.isProcessing = true;
		try {
			//Edit
			if (props.operateType == 'edit') {
				await getAPI(SysPosApi).apiSystemSysposUpdatePost(state.editForm);
			}
			//Copy or Add
			else if (props.operateType == 'copy' || props.operateType == 'add') {
				state.editForm.id = '';
				await getAPI(SysPosApi).apiSystemSysposAddPost(state.editForm);
			}
			ElMessage.success($t('lz.message.base.saveSuccess'));
			closeDialog();
		} finally {
			state.isProcessing = false;
		}
	});
};

// Export object
defineExpose({ openDialog });
</script>
