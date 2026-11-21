<template>
	<div class="tms-trafficplan-container">
		<el-dialog v-model="state.isShowDialog" draggable :close-on-click-modal="false" width="700px">
			<template #header>
				<div style="color: #fff">
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Edit />
					</el-icon>
					<span> {{ props.title }} </span>
				</div>
			</template>			
			<el-form :model="state.ruleForm" ref="ruleFormRef" label-width="auto" label-position="left">
				<el-row :gutter="10" >
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item :label="$t('lz.entity.base.code')" prop="code"
							:rules="[{ required: true, message: $t('lz.validation.base.required', {PropertyName: $t('lz.entity.base.code')}), trigger: 'blur' }]">
							<el-input v-model="state.ruleForm.code" 
									  :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.code')})"	
									  clearable />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item :label="$t('lz.entity.base.name')" prop="name"
							:rules="[{ required: true, message: $t('lz.validation.base.required', {PropertyName: $t('lz.entity.base.name')}), trigger: 'blur' }]">
							<el-input v-model="state.ruleForm.name" 
									 :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.name')})"										
									 clearable />
						</el-form-item>
					</el-col>
							
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item :label="$t('lz.entity.tms.eventType')" prop="eventTypeId"
							:rules="[{ required: true, message: $t('lz.validation.base.required', {PropertyName: $t('lz.entity.tms.eventType')}), trigger: 'blur' }]">
							<el-select v-model="state.ruleForm.eventTypeId" 
										:placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.entity.tms.eventType')})"
										filterable clearable class="w100">
								<el-option v-for="item in filteredTypeList" :key="item.id" :label="item.name" :value="item.id" />
							</el-select>
						</el-form-item>
					</el-col>
					<!-- SonTH - 24/07/2026: Chế độ thực thi quyết định kịch bản có dùng được cho luồng tự động hay không -->
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item :label="$t('lz.entity.tmsTrafficPlan.executionMode')">
							<el-radio-group v-model="state.ruleForm.executionMode">
								<el-radio value="Manual" class="mr5">{{ $t('lz.label.tmsTrafficPlan.executionModeManual') }}</el-radio>
								<el-radio value="Automatic" class="mr5">{{ $t('lz.label.tmsTrafficPlan.executionModeAutomatic') }}</el-radio>
								<el-radio value="Both" class="mr5">{{ $t('lz.label.tmsTrafficPlan.executionModeBoth') }}</el-radio>
							</el-radio-group>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.base.status')">
							<el-radio-group v-model="state.ruleForm.status" v-for="item in getConfigDataByCode(BaseConfigTypeEnum.SysStatusType)" :key="item.value">
								<el-radio :value="parseInt(item.code)" class="mr5">{{ item.name }}</el-radio>
							</el-radio-group>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item :label="$t('lz.entity.base.remark')">
							<el-input v-model="state.ruleForm.remark"
							:placeholder="$t('lz.entity.base.remark')" clearable type="textarea" />
						</el-form-item>
					</el-col>		
				</el-row>
			</el-form>
			<template #footer>
				<span class="dialog-footer">
					<el-button @click="cancel">{{ $t('lz.button.base.cancel') }}</el-button>
					<el-button type="primary" :loading="state.loading" :disabled="state.loading" @click="submit">{{ $t('lz.button.base.confirm') }}</el-button>
				</span>
			</template>
		</el-dialog>
	</div>
</template>

<script lang="ts" setup name="tmsEditTrafficPlan">
import { computed, onMounted, reactive, ref } from 'vue';
import { getAPI } from '/@/utils/axios-utils';
import { TmsTrafficPlanApi, TmsEventTypeApi } from '/@/api-services/tms';
import { UpdateTmsTrafficPlanInput } from '/@/api-services/tms';
import { OperateType } from '/@/types/constants';
import { ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { BaseConfigTypeEnum, StatusTypeEnum } from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
const { t: $t } = useI18n();
interface Props {
	/** the type of operation */
	operateType: OperateType;
	title: string;
}

const props = defineProps<Props>();
const emits = defineEmits(['handleQuery']);

const ruleFormRef = ref();
const state = reactive({
	isShowDialog: false,
	loading: false,
	ruleForm: {} as UpdateTmsTrafficPlanInput,
	TypeList: [] as any[],
	tabActiveTab: 'tab1', // Set default active tab to 'tab1'

});

const filteredTypeList = computed(() => {
	const currentId = state.ruleForm?.eventTypeId;
	if (props.operateType === 'edit' && currentId) {
		return (state.TypeList || []).filter((item: any) => item.status === StatusTypeEnum.Enable || item.id === currentId);
	}
	return (state.TypeList || []).filter((item: any) => item.status === StatusTypeEnum.Enable);
});

const loadTypeList = async () => {
	let resDicData = await getAPI(TmsEventTypeApi).apiTmsTmseventtypeListGet();
	state.TypeList = (resDicData.data.result || []).filter((item: any) => item.code !== 'unknown');
};

onMounted(async () => {
	await loadTypeList();
});

// Open dialog
const openDialog = async (row: any) => {
	if (!state.TypeList || state.TypeList.length === 0) {
		await loadTypeList();
	}
	state.ruleForm = JSON.parse(JSON.stringify(row));
	// SonTH - 24/07/2026: Mặc định Manual để giữ nguyên hành vi cũ với dữ liệu chưa có chế độ thực thi
	state.ruleForm.executionMode = state.ruleForm.executionMode ?? 'Manual';
	state.loading = false;
	state.isShowDialog = true;
	state.tabActiveTab = 'tab1'; // Ensure 'tab1' is set before showing dialog
	ruleFormRef.value?.resetFields();
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
	if (state.loading) return;
	ruleFormRef.value?.validate(async (valid: boolean) => {
		if (!valid) return;
		state.loading = true;
		try {
			//Edit
			if (props.operateType == 'edit') {
				await getAPI(TmsTrafficPlanApi).apiTmsTmstrafficplanUpdatePost(state.ruleForm);
			}
			//Copy or Add
			else if (props.operateType == 'copy' || props.operateType == 'add') {
				state.ruleForm.id = '';
				await getAPI(TmsTrafficPlanApi).apiTmsTmstrafficplanAddPost(state.ruleForm);

			}
			ElMessage.success($t('lz.message.base.submitSuccess'));
			closeDialog();
		} catch (error) {
			console.error(error);
		} finally {
			state.loading = false;
		}
	});
};

// Export object
defineExpose({ openDialog });
</script>
