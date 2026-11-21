<template>
	<div class="edit-work-detail-container">
		<el-dialog v-model="state.isShowDialog" draggable destroy-on-close width="700px">
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
						<el-form-item :label="$t('lz.label.base.code')" prop="code"
							:rules="[{ required: true, whitespace: true, message: $t('lz.validation.base.required', {PropertyName: $t('lz.entity.base.code')}), trigger: 'blur' }]">
							<el-input v-model.trim="state.ruleForm.code" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.code')})"
								clearable />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item :label="$t('lz.entity.base.name')" prop="name"
							:rules="[{ required: true, whitespace: true, message:  $t('lz.validation.base.required', {PropertyName: $t('lz.entity.base.name')}), trigger: 'blur' }]">
							<el-input v-model.trim="state.ruleForm.name" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.name')})"
								clearable />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.tmsWorkDetail.workType')">
							<el-select v-model="state.ruleForm.workType"
								:placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.entity.tmsWorkDetail.workType')})"
								 filterable clearable class="w100">
								<el-option v-for="item in props.workTypeList" :key="item.code" :label="item.name"
									:value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20"
						v-if="state.ruleForm.workType == 'control'">
						<el-form-item :label="$t('lz.entity.tms.controlType')">
							<el-select v-model="state.ruleForm.controlType" 
								:placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.entity.tms.controlType')})"
								filterable clearable class="w100"
								:disabled="!state.ruleForm.workType || state.ruleForm.workType != 'control'">
								<el-option v-for="item in state.dataControlTypeList" :key="item.code" :label="item.name"
									:value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20" v-else>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.tmsWorkDetail.workMode')">
							<el-select v-model="state.ruleForm.workMode" 
								:placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.entity.tmsWorkDetail.workMode')})"
								filterable clearable class="w100">
								<el-option v-for="item in props.workModeList" :key="item.code" :label="item.name"
									:value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.base.status')">
							<el-radio-group v-model="state.ruleForm.status" v-for="item in getConfigDataByCode(BaseConfigTypeEnum.SysStatusType)" :key="item.code">
								<el-radio :value="parseInt(item.code)" class="mr5">{{ item.name }}</el-radio>
							</el-radio-group>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item :label="$t('lz.entity.base.remark')">
							<el-input v-model="state.ruleForm.remark"
								:placeholder="$t('lz.entity.base.remark')" 
								clearable type="textarea" />
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>
			<template #footer>
				<span class="dialog-footer">
					<el-button @click="cancel">{{ $t('lz.button.base.cancel') }}</el-button>
					<el-button type="primary" @click="submit">{{ $t('lz.button.base.confirm') }}</el-button>
				</span>
			</template>
		</el-dialog>
	</div>
</template>

<script lang="ts" setup name="tmsEditWorkDetail">
import { onMounted, reactive, ref, watch } from 'vue';
import { getAPI } from '/@/utils/axios-utils';
import { TmsWorkDetailApi } from '/@/api-services/tms';
import { SysConfigDataApi } from '/@/api-services/cfgSystem';
import { UpdateWorkDetailInput } from '/@/api-services/tms';
import { OperateType } from '/@/types/constants';
import { ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { useBaseConfig } from '/@/stores/baseConfig';
import { BaseConfigTypeEnum } from '/@/types/enums';
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;

// Internationalization
const { t: $t } = useI18n();

// Props and emits
interface Props {
	operateType: OperateType; // Type of operation
	title: string;
	workTypeList: any;
	workModeList: any;
}
const props = defineProps<Props>();
const emits = defineEmits(['handleQuery']);

// Form and state
const ruleFormRef = ref();
const state = reactive({
	isShowDialog: false,
	ruleForm: {} as UpdateWorkDetailInput,
	dataControlTypeList: [] as Array<any>,
});

// Fetch data on mount
onMounted(async () => {
	try {
		// Fetch Control Type data
		const response = await getAPI(SysConfigDataApi).apiCfgsystemSysconfigdataDatalistCodeGet('tms_control_device_type');
		state.dataControlTypeList = response.data.result || [];
	} catch (error) {
		ElMessage.error($t('lz.message.base.errorFetchingData'));
	}
});

// Watcher to clear controlType when workType changes
watch(
	() => state.ruleForm.workType,
	(newVal) => {
		if (!newVal || newVal !== 'control') {
			state.ruleForm.controlType = ''; // Clear controlType
		}
	}
);

// Open dialog
const openDialog = (row: any) => {
	state.ruleForm = JSON.parse(JSON.stringify(row));
	state.isShowDialog = true;

	// Reset form validation
	ruleFormRef.value?.resetFields();
};

// Close dialog
const closeDialog = () => {
	emits('handleQuery');
	state.isShowDialog = false;
};

// Cancel action
const cancel = () => {
	state.isShowDialog = false;
};

// Submit form
const submit = () => {
	ruleFormRef.value.validate(async (valid: boolean) => {
		if (!valid) return;

		state.ruleForm.code = state.ruleForm.code?.trim();
		state.ruleForm.name = state.ruleForm.name?.trim();
		state.ruleForm.workType = state.ruleForm.workType ? state.ruleForm.workType.trim() : '';
		state.ruleForm.workMode = state.ruleForm.workMode ? state.ruleForm.workMode.trim() : '';
		if (state.ruleForm.workType !== 'control') {
			state.ruleForm.controlType = '';
		}

		try {
			// Edit
			if (props.operateType === 'edit') {
				await getAPI(TmsWorkDetailApi).apiTmsTmsworkdetailUpdatePost(state.ruleForm);
			}
			// Copy or Add
			else if (props.operateType === 'copy' || props.operateType === 'add') {
				state.ruleForm.id = '';
				await getAPI(TmsWorkDetailApi).apiTmsTmsworkdetailAddPost(state.ruleForm);
			}

			ElMessage.success($t('lz.message.base.submitSuccess'));
			closeDialog();
		} catch (error) {
			ElMessage.error($t('lz.message.base.acctionFail'));
		}
	});
};

// Expose methods
defineExpose({ openDialog });
</script>
