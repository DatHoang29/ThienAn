<template>
	<div class="tms-map-container">
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
						<el-form-item :label="$t('lz.entity.base.type')" prop="type" :rules="[{ required: true, message: $t('lz.validation.base.required',{PropertyName: $t('lz.entity.tmsMap.type')}), trigger: 'blur' }]">
							<el-select v-model="state.ruleForm.type" 
								:placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.entity.tmsMap.type')})"
								filterable clearable class="w100">
								<el-option v-for="item in state.dataMapTypeList" :key="item.code" :label="item.name"
									:value="item.code" />

							</el-select>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item :label="$t('lz.entity.base.code')" prop="code"
							:rules="[{ required: true, message: $t('lz.validation.base.required',{PropertyName: $t('lz.entity.base.code')}), trigger: 'blur' }]">
							<el-input v-model="state.ruleForm.code" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.code')})"
								clearable />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item :label="$t('lz.entity.base.name')" prop="name"
							:rules="[{ required: true, message: $t('lz.validation.base.required',{PropertyName: $t('lz.entity.base.name')}), trigger: 'blur' }]">
							<el-input v-model="state.ruleForm.name" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.name')})"
								clearable />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item :label="$t('lz.entity.base.url')" prop="url"
							:rules="[{ required: true, message:  $t('lz.validation.base.required',{PropertyName: $t('lz.entity.base.url')}), trigger: 'blur' }]">
							<!-- Hiển thị thẻ upload hình ảnh khi type là 1 -->
							<div v-if="state.ruleForm.type === 'image'" class="image-uploader-wrapper">
								<el-upload ref="uploadRef" class="avatar-uploader"
									:show-file-list="false" :auto-upload="false" accept=".jpg,.png,.svg" action=""
									:limit="1" :on-change="file => handleUploadChange(file, 'url')">
									<div v-if="state.imageUrl" class="image-preview-container">
										<img :src="state.imageUrl" class="avatar" />
										<div class="image-hover-toolbar">
											<el-button link @click.stop="handleZoomImage" class="toolbar-btn">
												<SvgIcon name="ele-ZoomIn" class="upload-icon"/>
											</el-button>
												<el-button link @click.stop="handleReplaceImage" class="toolbar-btn">
												<SvgIcon name="ele-Edit" class="upload-icon"/>
											</el-button>
										</div>
									</div>
									<div v-else class="upload-placeholder">
										<SvgIcon class="avatar-uploader-icon" name="ele-Plus" :size="28" />
									</div>
								</el-upload>
							</div>
							<!-- Hiển thị input nhập đường dẫn khi type là 0 -->
							<el-input v-else v-model="state.ruleForm.url" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.url')})"							
								clearable  type="textarea"/>
						</el-form-item>
					</el-col>
					<!-- Image zoom dialog -->
					<el-dialog v-model="state.isImageZoomDialog" width="50%" center>
						<template #header style="height: 35px;">
							&nbsp 
						</template>
						<div class="image-zoom-container">
							<img :src="state.imageUrl" class="zoom-image" />
						</div>
					</el-dialog>
					<el-col :xs="12" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.base.mode')" prop="mode" :rules="[{ required: true, message: $t('lz.validation.base.required',{PropertyName: $t('lz.entity.base.mode')}), trigger: 'blur' }]">
							<el-select v-model="state.ruleForm.mode"
								:placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.entity.tmsMap.mode')})"
								filterable clearable class="w100">
								<el-option v-for="item in state.dataMapModeList" :key="item.code" :label="item.name"
									:value="item.code" />

							</el-select>
						</el-form-item>
					</el-col>
					<el-col :xs="12" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						
						<el-form-item :label="$t('lz.label.base.isDefault')">
							<el-switch v-model="state.ruleForm.isDefault" :active-value="1" :inactive-value="0" size="small"
							:active-text="$t('lz.button.base.yes')" :inactive-text="$t('lz.button.base.no')"
							/>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.base.width')" prop="width">
							<el-input-number v-model="state.ruleForm.width" :min="0"
							:placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.width')})"	
							class="w100" />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.base.height')" prop="height">
							<el-input-number v-model="state.ruleForm.height" :min="0"
							:placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.height')})"
							class="w100" />
						</el-form-item>
					</el-col>


					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.base.status')">
							<el-radio-group v-model="state.ruleForm.status" v-for="item in getConfigDataByCode(BaseConfigTypeEnum.SysStatusType)" :key="item.value">
								<el-radio :value="parseInt(item.code)" class="mr5">{{ item.name }}</el-radio>
							</el-radio-group>

						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.base.orderNo')" prop="orderNo">
							<el-input-number v-model="state.ruleForm.orderNo" :min="0"						
							:placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.orderNo')})"
							 class="w100"
						   />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item :label="$t('lz.entity.base.remark')">
							<el-input v-model="state.ruleForm.remark"
								:placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.remark')})"
								clearable type="textarea" />
						</el-form-item>
					</el-col>					
				</el-row>
			</el-form>
			<template #footer>
				<span class="dialog-footer">
					<el-button @click="cancel">{{ $t('lz.button.base.cancel') }}</el-button>
					<el-button type="primary" @click="submit">{{ $t('lz.button.base.submit') }}</el-button>
				</span>
			</template>
		</el-dialog>
	</div>
</template>

<script lang="ts" setup name="tmsEditMap">
import { nextTick, onMounted, reactive, ref } from 'vue';
import { getAPI } from '/@/utils/axios-utils';
import { TmsMapApi } from '/@/api-services/tms';
import { SysConfigDataApi } from '/@/api-services/cfgSystem';
import { UpdateMapInput } from '/@/api-services/tms';
import { OperateType } from '/@/types/constants';
import { useI18n } from 'vue-i18n';
import { ElMessage, ElMessageBox, UploadInstance } from 'element-plus';
import { useItsMediaServerStore } from '/@/stores/its/itsMediaServer';
import { BaseConfigTypeEnum } from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;

const uploadRef = ref<UploadInstance>();
const { t: $t } = useI18n();
interface Map {
	operateType: OperateType;
	title: string;
}

const props = defineProps<Map>();
const emits = defineEmits(['handleQuery']);

const ruleFormRef = ref();
const state = reactive({
	isShowDialog: false,
	isImageZoomDialog: false,
	ruleForm: {} as UpdateMapInput,
	dataMapTypeList: [] as any,
	dataMapModeList: [] as any,
	dataIsDefaultList: [] as any,
	file: undefined as any,
	imageUrl: '' as string,
	baseUrl: '' as string,
});

// Method to convert file to base64
// File validation
const beforeUpload = (file: File) => {
	// Return false to prevent auto upload
	return false;
};

// File change handler
const handleUploadChange = (file: any) => {
	uploadRef.value!.clearFiles();

	const reader = new FileReader();
	reader.readAsDataURL(file.raw);

	reader.onload = () => {
		const base64String = reader.result as string;
		state.ruleForm.url = base64String;
		state.imageUrl = base64String;
	};

	reader.onerror = (error) => {
		console.error('Error converting file to base64:', error);
	};
};

const removeImage = () => {
	state.ruleForm.url = '';
};

// Zoom image
const handleZoomImage = () => {
	state.isImageZoomDialog = true;
};

// Replace image
const handleReplaceImage = () => {
	const fileInput = uploadRef.value?.$el?.querySelector('input[type="file"]');
	if (fileInput) {
		fileInput.click();
	}
};

// Page initialization
onMounted(async () => {
	let response = await getAPI(SysConfigDataApi).apiCfgsystemSysconfigdataDatalistCodeGet('tms_map_type');
	state.dataMapTypeList = response.data.result;
	response = await getAPI(SysConfigDataApi).apiCfgsystemSysconfigdataDatalistCodeGet('tms_map_mode');
	state.dataMapModeList = response.data.result;
	const dataIsDefaultList = await getAPI(SysConfigDataApi).apiCfgsystemSysconfigdataDatalistCodeGet('yesno_type');
	state.dataIsDefaultList = dataIsDefaultList.data.result;
	// Địa chỉ file server dùng chung (chỉ gọi API 1 lần cho toàn app)
	state.baseUrl = await useItsMediaServerStore().ensureLoaded();
});
const isBase64 = (str) => {
	// Kiểm tra chuỗi có bắt đầu bằng định dạng Base64 hay không
	const base64Regex = /^data:image\/[a-zA-Z]+;base64,/;
	return base64Regex.test(str);
};
// cancel
const cancel = () => {
	state.isShowDialog = false;
};
const openDialog = (row: any) => {
	state.ruleForm = JSON.parse(JSON.stringify(row));
	state.isShowDialog = true;
		state.imageUrl = state.ruleForm.url
		? (isBase64(state.ruleForm.url) ? state.ruleForm.url : `${state.baseUrl}/${state.ruleForm.url}`)
		: '';

	nextTick(() => {
		ruleFormRef.value?.clearValidate();
	});
};
const closeDialog = () => {
	emits('handleQuery');
	state.isShowDialog = false;
};

// submit
const submit = () => {
	ruleFormRef.value.validate(async (valid: boolean) => {
		if (!valid) return;

		// clone dữ liệu submit để tránh sửa trực tiếp state.ruleForm
		const submitData: any = JSON.parse(JSON.stringify(state.ruleForm));

		// Edit
		if (props.operateType === 'edit') {
			await getAPI(TmsMapApi).apiTmsTmsmapUpdatePost(submitData);
		}

		// Add mới: không hỏi confirm
		else if (props.operateType === 'add') {
			submitData.id = '';
			submitData.oldMapId = '';
			submitData.isCopyLocationConfig = false;

			await getAPI(TmsMapApi).apiTmsTmsmapAddPost(submitData);
		}
		
		// Copy: mới hỏi confirm
		else if (props.operateType === 'copy') {
			submitData.oldMapId = submitData.id;
			submitData.id = '';
			submitData.isCopyLocationConfig = false;

			try {
				await ElMessageBox.confirm(
					$t('lz.message.tmsMap.confirmCopyLocationConfig'),
					$t('lz.label.base.notification'),
					{
						confirmButtonText: $t('lz.button.base.yes'),
						cancelButtonText: $t('lz.button.base.no'),
						type: 'info',
					}
				);

				// người dùng chọn Có
				submitData.isCopyLocationConfig = true;
			} catch {
				// người dùng chọn Không hoặc đóng popup
				submitData.isCopyLocationConfig = false;
			}

			await getAPI(TmsMapApi).apiTmsTmsmapAddPost(submitData);
		}

		ElMessage.success($t('lz.message.base.submitSuccess'));
		closeDialog();
	});
};
// Export object
defineExpose({ openDialog });
</script>

<style lang="scss" scoped>
.image-zoom-container {
	width: 100%;
	max-height: 60vh;
	display: flex;
	align-items: center;
	justify-content: center;
	overflow: auto;

	.zoom-image {
		width: 100%;
		height: auto;
		max-height: 60vh;
		object-fit: contain;
	}
}

.image-uploader-wrapper {
	width: 100%;
}

.avatar-uploader {
	width: 100% !important;
	display: flex !important;
}

.image-preview-container {
	width: 100%;
	height: auto;
	min-height: 120px;
	border: 2px solid #dcdfe6;
	border-radius: 4px;
	display: flex;
	align-items: center;
	justify-content: center;
	position: relative;
	overflow: hidden;
	background: #f5f7fa;
	transition: all 0.3s ease;

	&:hover {
		border-color: #409eff;
		background: #f0f9ff;
		box-shadow: 0 0 12px rgba(64, 158, 255, 0.2);

		.image-hover-toolbar {
			opacity: 1;
			visibility: visible;
		}
	}

	img {
		width: 100%;
		height: auto;
		max-height: 150px;
		object-fit: contain;
		padding: 10px;
		box-sizing: border-box;
	}

	.image-hover-toolbar {
		position: absolute;
		top: 0;
		left: 0;
		right: 0;
		bottom: 0;
		display: flex;
		align-items: center;
		justify-content: center;
		gap: 15px;
		background: rgba(0, 0, 0, 0.3);
		opacity: 0;
		visibility: hidden;
		transition: all 0.3s ease;
		border-radius: 4px;

		.toolbar-btn {
			 border-radius: 50%;
			background: var(--el-color-primary);
			color: var(--el-color-white)!important;
			font-size: 25px!important;
			width: 40px!important;
			height: 40px!important;
			padding: 6px;
			display: inline-flex;
			align-items: center;
			justify-content: center;
			margin-right: 3px;
			cursor: pointer;
			transition: all 0.2s ease;

			&:hover {
				background: var(--el-color-primary);
				transform: scale(1.1);
				box-shadow: 0 2px 12px rgba(0, 0, 0, 0.2);
			}

			:deep(.svg-icon) {
				width: 25px !important;
				height: 25px !important;
			}
		}
	}
}

.upload-placeholder {
	width: 100%;
	height: 120px;
	border: 2px dashed #dcdfe6;
	border-radius: 4px;
	display: flex;
	align-items: center;
	justify-content: center;
	background: #fafafa;
	transition: all 0.3s ease;
	cursor: pointer;

	&:hover {
		border-color: #409eff;
		background: #f0f9ff;
	}

	.avatar-uploader-icon {
		color: #8c939d;
		font-size: 28px;
	}
}

:deep(.avatar-uploader .el-upload) {
	width: 100% !important;
	height: auto;
	border: none !important;
	padding: 0 !important;
}

:deep(.avatar-uploader .el-upload:hover) {
	border-color: transparent !important;
}

:deep(.avatar-uploader .el-upload__content) {
	width: 100%;
	height: 100%;
	display: flex;
	align-items: center;
	justify-content: center;
	flex-direction: column;
}
.upload-icon {
//   border-radius: 50%;
//   background: var(--el-color-primary);
//   color: var(--el-color-white)!important;
//   font-size: 12px!important;
//   width: 25px!important;
//   height: 25px!important;
//   padding: 6px;
//   display: inline-flex;
//   align-items: center;
//   justify-content: center;
//   margin-right: 3px;
}
</style>