<template>
	<div class="sys-file-container">
		<el-dialog v-model="state.isShowDialog" draggable overflow destroy-on-close width="500px">
			<template #header>
				<div style="color: #fff">
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Edit /> </el-icon>
					<span> {{ props.title }} </span>
				</div>
			</template>
			<el-form :model="state.ruleForm" ref="ruleFormRef" label-width="auto">
				<el-row>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item label="File Name" prop="fileName" :rules="[{ required: true, message: 'File name cannot be empty', trigger: 'blur' }]">
							<el-input v-model="state.ruleForm.fileName" placeholder="File Name" clearable />
						</el-form-item>
					</el-col>
				</el-row>
				<el-row>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item label="File Type" prop="fileType">
							<el-select v-model="state.ruleForm.fileType" placeholder="Please select file type" style="margin-bottom: 10px">
								<el-option label="Related Files" value="Related Files" />
								<el-option label="Archived Files" value="Archived Files" />
							</el-select>
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>
			<template #footer>
				<span class="dialog-footer">
					<el-button @click="cancel">Cancel</el-button>
					<el-button type="primary" @click="submit">Confirm</el-button>
				</span>
			</template>
		</el-dialog>
	</div>
</template>

<script lang="ts" setup name="sysEditFile">
import { reactive, ref } from 'vue';
import { ElMessage } from 'element-plus';

import { getAPI } from '/@/utils/axios-utils';
import { SysFileApi } from '/@/api-services/system';
import { FileInput } from '/@/api-services/system';

const props = defineProps({
	title: String,
	sysFileId: Number,
});
const emits = defineEmits(['handleQuery']);
const ruleFormRef = ref();
const state = reactive({
	isShowDialog: false,
	ruleForm: {} as FileInput,
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
		if (state.ruleForm.id != undefined && state.ruleForm.id != "0" && state.ruleForm.id != '') {
			await getAPI(SysFileApi)
				.apiSystemSysfileUpdatePost(state.ruleForm)
				.then((rsp: any) => {
					if (rsp.data.code == 200) {
						ElMessage.success('File information updated successfully!');
					} else {
						ElMessage.error('Failed to update file information: ' + rsp.data.message);
					}
				});
		}
		closeDialog();
	});
};

// Export object
defineExpose({ openDialog });
</script>
