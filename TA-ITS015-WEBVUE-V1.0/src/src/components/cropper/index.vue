<template>
	<div>
		<el-dialog v-model="state.isShowDialog" width="769px" :before-close="onCancel">
			<template #header>
				<div style="color: #fff">
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Edit /> </el-icon>
					<span>{{ props.title }}</span>
				</div>
			</template>
			<div class="cropper-warp">
				<div class="cropper-warp-left">
					<img :src="state.cropperImg" class="cropper-warp-left-img" />
				</div>
				<div class="cropper-warp-right">
					<div class="cropper-warp-right-title">Preview</div>
					<div class="cropper-warp-right-item">
						<div class="cropper-warp-right-value">
							<img :src="state.cropperImgBase64" class="cropper-warp-right-value-img" />
						</div>
						<div class="cropper-warp-right-label">100 x 100</div>
					</div>
					<div class="cropper-warp-right-item">
						<div class="cropper-warp-right-value">
							<img :src="state.cropperImgBase64" class="cropper-warp-right-value-img cropper-size" />
						</div>
						<div class="cropper-warp-right-label">50 x 50</div>
					</div>
				</div>
			</div>
			<template #footer>
				<span class="dialog-footer">
					<el-upload
						ref="uploadSignRef"
						accept=".jpg,.jpeg,.png,.bmp,.gif,.webp,image/jpeg,image/png,image/bmp,image/gif,image/webp"
						:limit="1"
						:show-file-list="false"
						:auto-upload="false"
						:on-change="selectPicture"
						:on-exceed="selectPictureExceed"
						style="display: inline-block; position: absolute; right: 182px"
					>
						<el-button icon="ele-Picture" title="jpg, png, bmp, gif, webp (< 10MB)">Select Image</el-button>
					</el-upload>
					<el-button @click="onCancel">Cancel</el-button>
					<el-button type="primary" @click="onSubmit">Confirm</el-button>
				</span>
			</template>
		</el-dialog>
	</div>
</template>

<script setup lang="ts" name="cropper">
import { reactive, nextTick, ref } from 'vue';
import Cropper from 'cropperjs';
import 'cropperjs/dist/cropper.css';
import { genFileId, ElMessage } from 'element-plus';
import type { UploadInstance, UploadProps, UploadRawFile, UploadFile } from 'element-plus';
import { useI18n } from 'vue-i18n';

const { t: $t } = useI18n();

const props = defineProps({
	title: {
		type: String,
		default: () => '',
	},
});
const emits = defineEmits(['uploadCropperImg']);
const uploadSignRef = ref<UploadInstance>();
// Define variable content
const state = reactive({
	isShowDialog: false,
	cropperImg: '',
	cropperImgBase64: '',
	cropper: '' as RefType,
});

// Open dialog
const openDialog = (imgs: string) => {
	state.cropperImg = imgs;
	state.isShowDialog = true;
	uploadSignRef.value?.clearFiles();
	nextTick(() => {
		initCropper();
	});
};
// Close dialog
const closeDialog = () => {
	if (state.cropper && typeof state.cropper.destroy === 'function') {
		state.cropper.destroy();
	}
	state.isShowDialog = false;
};
// Cancel
const onCancel = () => {
	closeDialog();
};
// Change/Upload
const onSubmit = async () => {
	const img = await getCroppedCanvas();
	emits('uploadCropperImg', { img: img });
	closeDialog();
};
// Initialize cropperjs image cropping
const initCropper = () => {
	const letImg = <HTMLImageElement>document.querySelector('.cropper-warp-left-img');
	if (letImg) {
		letImg.setAttribute('crossOrigin', 'anonymous');
	}
	state.cropper = new Cropper(letImg, {
		viewMode: 1,
		dragMode: 'none',
		initialAspectRatio: 1,
		aspectRatio: 1,
		preview: '.before',
		background: true,
		autoCropArea: 1,
		// zoomOnWheel: false,
		checkCrossOrigin: false,
		crop: () => {
			state.cropperImgBase64 = state.cropper.getCroppedCanvas()!.toDataURL('image/jpeg');
		},
	});
};

// Get cropped image (wrapped as Promise)
const getCroppedCanvas = () => {
	return new Promise((resolve) => {
		state.cropper.getCroppedCanvas().toBlob((blob: any) => {
			resolve(blob);
		});
	});
};

const ALLOWED_EXTENSIONS = ['jpg', 'jpeg', 'png', 'bmp', 'gif', 'webp'];
const ALLOWED_MIME_TYPES = [
	'image/jpeg',
	'image/pjpeg',
	'image/png',
	'image/bmp',
	'image/x-ms-bmp',
	'image/x-bmp',
	'image/gif',
	'image/webp',
];
const MAX_IMAGE_SIZE_MB = 10;

// Validate image file
const validateImageFile = (rawFile: File | UploadRawFile): boolean => {
	const fileName = rawFile.name || '';
	const ext = fileName.substring(fileName.lastIndexOf('.') + 1).toLowerCase();
	const isAllowedExt = ALLOWED_EXTENSIONS.includes(ext);
	const isAllowedMime =
		!rawFile.type ||
		ALLOWED_MIME_TYPES.includes(rawFile.type.toLowerCase()) ||
		(rawFile.type.startsWith('image/') && ALLOWED_EXTENSIONS.includes(rawFile.type.replace(/^image\//, '').toLowerCase()));

	if (!isAllowedExt || !isAllowedMime) {
		const msg = $t('lz.message.cropper.formatIsInvalid');
		ElMessage.error(msg && msg !== 'lz.message.cropper.formatIsInvalid' ? msg : 'Định dạng file không hợp lệ! Vui lòng chọn ảnh: jpg, png, bmp, gif, webp');
		return false;
	}

	const isLt10M = rawFile.size / 1024 / 1024 < MAX_IMAGE_SIZE_MB;
	if (!isLt10M) {
		const msg = $t('lz.message.cropper.sizeIsTooLarge', { size: MAX_IMAGE_SIZE_MB });
		ElMessage.error(msg && msg !== 'lz.message.cropper.sizeIsTooLarge' ? msg : `Dung lượng ảnh phải nhỏ hơn ${MAX_IMAGE_SIZE_MB}MB!`);
		return false;
	}

	return true;
};

// Select image
const selectPicture = async (file: UploadFile) => {
	const rawFile = file?.raw;
	if (!rawFile) return;

	if (!validateImageFile(rawFile)) {
		uploadSignRef.value?.clearFiles();
		return;
	}

	let URL = window.URL || window.webkitURL;
	state.cropperImg = URL.createObjectURL(rawFile);
	if (state.cropper) {
		state.cropper.replace(state.cropperImg);
	}
};

// Execute when the selected image exceeds the quantity limit
const selectPictureExceed: UploadProps['onExceed'] = (files) => {
	uploadSignRef.value!.clearFiles();
	const file = files[0] as UploadRawFile;
	if (!validateImageFile(file)) {
		return;
	}
	file.uid = genFileId();
	uploadSignRef.value!.handleStart(file);
};

// Expose variables
defineExpose({
	openDialog,
});
</script>

<style scoped lang="scss">
.cropper-warp {
	display: flex;
	.cropper-warp-left {
		position: relative;
		display: inline-block;
		height: 350px;
		flex: 1;
		border: 1px solid var(--el-border-color);
		background: var(--el-color-white);
		overflow: hidden;
		background-repeat: no-repeat;
		cursor: move;
		border-radius: var(--el-border-radius-base);
		.cropper-warp-left-img {
			width: 100%;
			height: 100%;
		}
	}
	.cropper-warp-right {
		width: 150px;
		height: 350px;
		.cropper-warp-right-title {
			text-align: center;
			height: 20px;
			line-height: 20px;
		}
		.cropper-warp-right-item {
			margin: 15px 0;
			.cropper-warp-right-value {
				display: flex;
				.cropper-warp-right-value-img {
					width: 100px;
					height: 100px;
					border-radius: var(--el-border-radius-circle);
					margin: auto;
				}
				.cropper-size {
					width: 50px;
					height: 50px;
				}
			}
			.cropper-warp-right-label {
				text-align: center;
				font-size: 12px;
				color: var(--el-text-color-primary);
				height: 30px;
				line-height: 30px;
			}
		}
	}
}
</style>
