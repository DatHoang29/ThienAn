<template>
	<div class="rpt-management-container">
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
						<el-form-item :label="$t('lz.entity.base.code')" prop="code"
							label-position="left"
							:rules="[{ required: true, message: $t('lz.validation.base.required',{PropertyName: $t('lz.entity.base.code')}), trigger: 'blur' }]">
							<el-input v-model="state.ruleForm.code" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.code')})"
								clearable />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item :label="$t('lz.entity.base.name')" prop="name"
							label-position="left"
							:rules="[{ required: true, message: $t('lz.validation.base.required',{PropertyName: $t('lz.entity.base.name')}), trigger: 'blur' }]">
							<el-input v-model="state.ruleForm.name" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.name')})"
								clearable />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.base.status')" label-position="left">
							<el-radio-group v-model="state.ruleForm.status" v-for="item in getConfigDataByCode(BaseConfigTypeEnum.SysStatusType)" :key="item.code">
								<el-radio :value="parseInt(item.code)" class="mr5">{{ item.name }}</el-radio>
							</el-radio-group>

						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item :label="$t('lz.entity.report.type')" label-position="left">
							<el-select v-model="state.ruleForm.type" filterable clearable class="w100" :placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.entity.report.type')})">
								<el-option v-for="item in getConfigDataByCode(BaseConfigTypeEnum.RptReportType)" :key="item.code" :label="item.name" :value="item.code" />
							</el-select>
						</el-form-item>

					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item :label="$t('lz.entity.report.template')" label-position="left">
							<el-upload ref="uploadReportRef" drag :auto-upload="false" :limit="1" :file="state.ruleForm.file" action="" 
							:on-change="handleUploadChange"
    						:on-exceed="handleUploadExceed"
							>
								<el-icon class="el-icon--upload">
									<ele-UploadFilled />
								</el-icon>
								<div class="el-upload__text" v-html="$t('lz.label.upload.dragFileOrUpload')"></div>
								<template #tip>
									<div class="el-upload__tip">{{ $t('lz.label.upload.pleaseUploadFilesNotExceeding',{value: '10MB'}) }}</div>
								</template>
							</el-upload>
						</el-form-item>
						
					</el-col>
					
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item :label="$t('lz.entity.base.version')" prop="version" label-position="left">
							<el-input v-model="state.ruleForm.version" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.version')})"
								clearable />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item :label="$t('lz.entity.base.remark')" label-position="left">
							<el-input v-model="state.ruleForm.remark"
								:placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.remark')})" clearable type="textarea" />
						</el-form-item>
					</el-col>
				</el-row>
				
			</el-form>
			<template #footer>
				<span class="dialog-footer">
					<el-button @click="cancel">{{  $t('lz.button.base.cancel')  }}</el-button>
					<el-button type="primary" @click="submit">{{ $t('lz.button.base.confirm') }}</el-button>
				</span>
			</template>
		</el-dialog>
	</div>
</template>

<script lang="ts" setup name="rptEditManagement">
import { reactive, ref, onMounted } from 'vue';
import { getAPI } from '/@/utils/axios-utils';
import { RptManagementApi } from '/@/api-services/report';
import { RptmanagementUpdateBody } from '/@/api-services/report';
import { OperateType } from '/@/types/constants';
import { ElMessage } from 'element-plus';
import { SysConfigDataApi } from '/@/api-services/cfgSystem';
import type { UploadInstance } from 'element-plus';
import { BaseConfigTypeEnum } from '/@/types/enums/baseEnum';
import { useBaseConfig } from '/@/stores/baseConfig';
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
const getConfigItemByCode = useBaseConfig().getConfigItemByCode;

import { useI18n } from 'vue-i18n';
const { t: $t } = useI18n();
const uploadReportRef = ref<UploadInstance>();


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
	ruleForm: {} as RptmanagementUpdateBody,
	typeList: [] as any,

});

// Page initialization
onMounted(async () => {
	let resData = await getAPI(SysConfigDataApi).apiCfgsystemSysconfigdataDatalistCodeGet('report_type');
	state.typeList = resData.data.result;
});

// Open dialog
const openDialog = (row: any) => {
	state.ruleForm = JSON.parse(JSON.stringify(row));
	state.isShowDialog = true;
	ruleFormRef.value?.resetFields();
	uploadReportRef.value?.clearFiles();

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

// Get file list through onChange method
const handleUploadChange = (file: any) => {
	// if (state.ruleForm.file) {
    //     uploadReportRef.value?.clearFiles();
    // }
	state.ruleForm.file = file.raw;
};
const handleUploadExceed = (files: any) => {
	uploadReportRef.value?.clearFiles()
	uploadReportRef.value?.handleStart(files[0])
};
// Submit
const submit = () => {
	ruleFormRef.value.validate(async (valid: boolean) => {

		if (!valid) return;

		Object.keys(state.ruleForm).forEach(key => {
		if (state.ruleForm[key] === null) {
			state.ruleForm[key] = undefined;
		}
		});
		
		//Edit
		if (props.operateType == 'edit') {
				await getAPI(RptManagementApi).apiReportRptmanagementUpdatePostForm(state.ruleForm.id,state.ruleForm.file,state.ruleForm.pid,state.ruleForm.code,state.ruleForm.name,state.ruleForm.type,state.ruleForm.status,state.ruleForm.url,undefined,state.ruleForm.remark,state.ruleForm.version,undefined);
		}
		//Copy or Add
		else if (props.operateType == 'copy' || props.operateType == 'add') {
			state.ruleForm.id = '';
			await getAPI(RptManagementApi).apiReportRptmanagementAddPostForm(state.ruleForm.id,state.ruleForm.file,state.ruleForm.pid,state.ruleForm.code,state.ruleForm.name,state.ruleForm.type,state.ruleForm.status,state.ruleForm.url,undefined,state.ruleForm.remark,state.ruleForm.version,undefined);
		}
		ElMessage.success($t('lz.message.base.saveSuccess'));
		closeDialog();
	});
};

// Export object
defineExpose({ openDialog });
</script>
