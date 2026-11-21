<template>
	<div>
		<el-card shadow="hover" v-loading="state.isLoading">
			<el-descriptions :title="$t('lz.label.sysInfo.infoSetting')" :column="2" :border="true">
				<template #title>
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Setting /> </el-icon> {{ $t('lz.label.sysInfo.infoSetting') }}
				</template>
				<el-descriptions-item :label="$t('lz.label.sysInfo.systemIcon')" :span="2">
					<el-upload ref="uploadRef" class="avatar-uploader" :showFileList="false" :autoUpload="false" accept=".jpg,.png,.svg" action="" :limit="1" :onChange="handleUploadChange">
						<img v-if="state.formData.sysLogo" :src="state.formData.sysLogo" class="avatar" />
						<SvgIcon v-else class="avatar-uploader-icon" name="ele-Plus" :size="28" />
					</el-upload>
				</el-descriptions-item>
				<el-descriptions-item :label="$t('lz.entity.sysInfo.sysTitle')">
					<el-input v-model="state.formData.sysTitle" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.sysInfo.sysTitle')})" clearable />
				</el-descriptions-item>
				<el-descriptions-item :label="$t('lz.entity.sysInfo.sysViceTitle')">
					<el-input v-model="state.formData.sysViceTitle" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.sysInfo.sysViceTitle')})" clearable />
				</el-descriptions-item>
				<el-descriptions-item :label="$t('lz.entity.sysInfo.sysViceDesc')" :span="2">
					<el-input v-model="state.formData.sysViceDesc" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.sysInfo.sysViceDesc')})" clearable/>
				</el-descriptions-item>
				<el-descriptions-item :label="$t('lz.entity.sysInfo.watermarkContent')" :span="2">
					<el-input v-model="state.formData.sysWatermark" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.sysInfo.watermarkContent')})" clearable />
				</el-descriptions-item>
				<el-descriptions-item :label="$t('lz.entity.sysInfo.sysCopyright')" :span="2">
					<el-input v-model="state.formData.sysCopyright" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.sysInfo.sysCopyright')})" clearable/>
				</el-descriptions-item>
				<template #extra>
					<el-button type="primary" icon="ele-SuccessFilled" @click="onSave">{{$t('lz.button.base.save')}}</el-button>
				</template>
			</el-descriptions>
		</el-card>
	</div>
</template>

<script setup lang="ts" name="sysInfoSetting">
import { nextTick, reactive, ref } from 'vue';
import { ElMessage, UploadInstance } from 'element-plus';
import { fileToBase64 } from '/@/utils/base64Conver';

import { getAPI } from '/@/utils/axios-utils';
import { SysConfigApi } from '/@/api-services';

const uploadRef = ref<UploadInstance>();
const state = reactive({
	isLoading: false,
	file: undefined as any,
	formData: {
		sysLogoBlob: undefined,
		sysLogo: '',
		sysLogoFileName: '',
		sysTitle: '',
		sysViceTitle: '',
		sysViceDesc: '',
		sysWatermark: '',
		sysCopyright: '',
		sysIcp: '',
		sysIcpUrl: '',
		sysSecondVer: false,
		sysCaptcha: true,
	},
});

// Get file list through onChange method
const handleUploadChange = (file: any) => {
	uploadRef.value!.clearFiles();

	state.file = file;
	
	state.formData.sysLogo = URL.createObjectURL(state.file.raw); // Display logo preview
};

// Save
const onSave = async () => {
	// If an icon is selected, convert to base64
	let sysLogoBase64 = '';
	let sysLogoFileName = '';
	if (state.file) {
		sysLogoBase64 = (await fileToBase64(state.file.raw)) as string;
		sysLogoFileName = state.file.raw.name;
	}

	try {
		state.isLoading = true;
		const res = await getAPI(SysConfigApi).apiSystemSysconfigSavesysinfoPost({
			sysLogoBase64: sysLogoBase64,
			sysLogoFileName: sysLogoFileName,
			sysTitle: state.formData.sysTitle,
			sysViceTitle: state.formData.sysViceTitle,
			sysViceDesc: state.formData.sysViceDesc,
			sysWatermark: state.formData.sysWatermark,
			sysCopyright: state.formData.sysCopyright,
		});
		if (res.data!.succeeded !== true) return;

		// Clear file variable
		state.file = undefined;
		// await loadData();
		ElMessage.success('Save successful');

		// Refresh browser
		window.location.reload();
	} finally {
		nextTick(() => {
			state.isLoading = false;
		});
	}
};

// Load data
const loadData = async () => {
	try {
		state.isLoading = true;
		const res = await getAPI(SysConfigApi).apiSystemSysconfigSysinfoGet();
		if (res.data!.succeeded != true) return;

		const result = res.data.result;
		state.formData = {
			sysLogoBlob: undefined,
			sysLogo: result.sysLogo,
			sysLogoFileName: '',
			sysTitle: result.sysTitle,
			sysViceTitle: result.sysViceTitle,
			sysViceDesc: result.sysViceDesc,
			sysWatermark: result.sysWatermark,
			sysCopyright: result.sysCopyright,
			sysIcp: result.sysIcp,
			sysIcpUrl: result.sysIcpUrl,
			sysSecondVer: result.sysSecondVer,
			sysCaptcha: result.sysCaptcha,
		};
	} finally {
		nextTick(() => {
			state.isLoading = false;
		});
	}
};

loadData();
</script>

<style lang="scss" scoped>
.avatar-uploader .avatar {
	width: 100px;
	height: 100px;
	display: block;
	object-fit: contain;
}

:deep(.avatar-uploader) .el-upload {
	border: 1px dashed var(--el-border-color);
	cursor: pointer;
	position: relative;
	overflow: hidden;
	transition: var(--el-transition-duration-fast);
}

:deep(.avatar-uploader) .el-upload:hover {
	border-color: var(--el-color-primary);
}

.el-icon.avatar-uploader-icon {
	color: #8c939d;
	width: 100px;
	height: 100px;
	text-align: center;
}
</style>
