<template>
	<div class="tms-module-container">
		<el-dialog v-model="state.isShowDialog" draggable :close-on-click-modal="false" width="700px">
			<template #header>
				<div style="color: #fff">
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Edit />
					</el-icon>
					<span> {{ props.title }} </span>
				</div>
			</template>
			<el-form :model="state.ruleForm" ref="ruleFormRef" label-width="auto" label-position="left" >
				<el-row :gutter="10">
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item :label="$t('lz.entity.tmsModule.keyName')" prop="keyName"
							:rules="[{ required: true,	 message: $t('lz.validation.base.required',{PropertyName: $t('lz.entity.tmsModule.keyName')}), trigger: 'blur' }]">
							<el-input v-model="state.ruleForm.keyName" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.tmsModule.keyName')})"
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
						<el-form-item :label="$t('lz.label.system.plugin')">
							<el-select v-model="state.ruleForm.pluginId"
								:placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.label.system.plugin')})"
									filterable clearable class="w100">
								
								<el-option 
									v-for="item in props.pluginData" 
									:key="item.id" 
									:label="item.isDelete != null ? `${item.name} (${$t('lz.label.base.deleted')})` : item.name" 
									:value="item.id" 
									:disabled="item.status !== (StatusTypeEnum.Enable as number) || item.isDelete != null"
								>
									<span :style="{ color: item.isDelete != null ? 'var(--el-text-color-secondary)' : 'inherit' }">
										{{ item.isDelete != null ? `${item.name} (${$t('lz.label.base.deleted')})` : item.name }}
									</span>
								</el-option>

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
								:placeholder="$t('lz.entity.base.remark')" clearable type="textarea" />
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

<script lang="ts" setup name="tmsEditModule">
import { reactive, ref } from 'vue';
import { getAPI } from '/@/utils/axios-utils';
import { TmsModulesApi } from '/@/api-services/tms';
import { UpdateTmsModuleInput } from '/@/api-services/tms';
import { OperateType } from '/@/types/constants';
import { ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { SysPluginApi } from '/@/api-services/system';
import { BaseConfigTypeEnum, StatusTypeEnum } from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
const { t: $t } = useI18n();
interface Props {
	/** the type of operation */
	operateType: OperateType;
	title: string;
	pluginData: Array<SysPluginApi>;
}

const props = defineProps<Props>();
const emits = defineEmits(['handleQuery']);

const ruleFormRef = ref();
const state = reactive({
	isShowDialog: false,
	ruleForm: {} as UpdateTmsModuleInput,
});

// Open dialog
const openDialog = (row: any) => {
	state.ruleForm = JSON.parse(JSON.stringify(row));
	state.isShowDialog = true;
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
	ruleFormRef.value.validate(async (valid: boolean) => {
		if (!valid) return;
		//Edit
		if (props.operateType == 'edit') {
			await getAPI(TmsModulesApi).apiTmsTmsmodulesUpdatePost(state.ruleForm);
		}
		//Copy or Add
		else if (props.operateType == 'copy' || props.operateType == 'add') {
			state.ruleForm.id = '';
			await getAPI(TmsModulesApi).apiTmsTmsmodulesAddPost(state.ruleForm);

		}
		ElMessage.success($t('lz.message.base.submitSuccess'));
		closeDialog();
	});
};

// Export object
defineExpose({ openDialog });
</script>
