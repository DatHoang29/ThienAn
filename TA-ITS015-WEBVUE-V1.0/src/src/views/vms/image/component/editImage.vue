<template>
	<div class="edit-image-container">
		<el-dialog v-model="state.isShowDialog" draggable :close-on-click-modal="false" width="700px">
			<template #header>
				<div style="color: #fff">
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"><ele-Edit /></el-icon>
					<span> {{ props.title }} </span>
				</div>
			</template>
			<el-form :model="state.editForm" ref="editFormRef" label-width="auto" label-position="left">
				<el-row :gutter="10">
					<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.base.name')" prop="name" :rules="[{ required: true, message: $t('lz.validation.base.required', {PropertyName: $t('lz.entity.base.name')}), trigger: 'blur' }, { max: EntityFieldLength.Length64, message: $t('lz.validation.base.maxlength', {PropertyName: $t('lz.entity.base.name'), MaxLength: EntityFieldLength.Length64}), trigger: 'blur'}]">
							<el-input v-model="state.editForm.name" :placeholder="$t('lz.label.base.pleaseType', {input: $t('lz.entity.base.name')})" :maxlength="EntityFieldLength.Length64" show-word-limit clearable />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.base.code')" prop="code" :rules="[{ required: true, message: $t('lz.validation.base.required', {PropertyName: $t('lz.entity.base.code')}), trigger: 'blur' }, { max: EntityFieldLength.Length64, message: $t('lz.validation.base.maxlength', {PropertyName: $t('lz.entity.base.code'), MaxLength: EntityFieldLength.Length64}), trigger: 'blur'}]">
							<el-input v-model="state.editForm.code" :placeholder="$t('lz.label.base.pleaseType', {input: $t('lz.entity.base.code')})" :maxlength="EntityFieldLength.Length64" show-word-limit clearable />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.vmsImage.type')" prop="type" :rules="[{ required: true, message: $t('lz.validation.base.required', {PropertyName: $t('lz.entity.vmsImage.type')}), trigger: 'blur' }]">
							<el-select v-model="state.editForm.type" class="w100" @change="onImageTypeChange">
								<el-option v-for="item in state.imageTypeList" :label="item.name" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.vmsImage.size')" prop="size" :rules="[{ required: true, message: $t('lz.validation.base.required', {PropertyName: $t('lz.entity.vmsImage.size')}), trigger: 'blur' }]">
							<!-- Kích thước load theo Loại thiết bị; "Tất cả" (null) hiển thị cả icon + image -->
							<el-select v-model="state.editForm.size" :placeholder="$t('lz.label.base.pleaseSelect', {input: $t('lz.entity.vmsImage.size')})" class="w100" @change="onSizeChange">
								<el-option v-for="item in sizeOptions" :key="item.code" :label="item.name" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.base.lang')" prop="lang" :rules="[{ required: true, message: $t('lz.validation.base.required', {PropertyName: $t('lz.entity.base.lang')}), trigger: 'blur' }]">
							<el-select v-model="state.editForm.lang" class="w100">
								<el-option v-for="item in state.languageList" :label="item.name" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.base.status')" prop="status">
							<el-radio-group v-model="state.editForm.status" v-for="item in getConfigDataByCode(BaseConfigTypeEnum.SysStatusType)" :key="item.code">
								<el-radio :value="parseInt(item.code)" class="mr5">{{ item.name }}</el-radio>
							</el-radio-group>
						</el-form-item>
					</el-col>
					
					<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.base.orderNo')" prop="orderNo">
							<el-input-number
								v-model="state.editForm.orderNo"
								:min="0"
								:placeholder="$t('lz.label.base.pleaseType', {input: $t('lz.entity.base.orderNo')})"
								controls-position="right"
								class="w100"
							/>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.label.cctvEquipment.equipmentType')" prop="equipmentTypeId">
							<el-select v-model="equipmentTypeIdProxy" filterable class="w100">
								<!-- "Tất cả" = sentinel, khi lưu sẽ map về null (hình dùng cho mọi loại thiết bị) -->
								<el-option :label="$t('lz.label.base.all')" :value="ALL_EQUIPMENT_TYPE" />
								<el-option v-for="item in state.equipmentTypeList" :key="item.id" :label="item.name" :value="item.id" />
							</el-select>
						</el-form-item>
					</el-col>

					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item :label="$t('lz.entity.base.image')" prop="url"
							:rules="[{ required: true, message: $t('lz.validation.base.required', {PropertyName: $t('lz.entity.base.image')}), trigger: 'change' }]">
							<el-upload
								action=""
								ref="uploadInstanceRef" 
								class="avatar-uploader"
								accept="image/*" 
								:auto-upload="false" 
								:show-file-list="false"
								:limit="1" 
								:on-change="handleUpload"
							>
								<img v-if="state.editForm.url" :src="previewUrl" class="avatar" />
								<SvgIcon v-else class="avatar-uploader-icon" name="ele-Plus" :size="28" />
							</el-upload>
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

<script lang="ts" setup name="vmsEditImage">

	import { reactive, ref, onMounted, computed, watch } from 'vue';
	import { getAPI } from '/@/utils/axios-utils';
	import { OperateType } from '/@/types/constants';
	import { ElMessage, UploadInstance } from 'element-plus';
	import { useI18n } from 'vue-i18n';

	import { VmsImageApi } from '/@/api-services/vms';
	import { UpdateVmsImageInput } from '/@/api-services/vms';

	import { useCommonVarsAndFuncs } from '/@/stores/its/commonVarsAndFuncs';
    const commonVarsAndFuncs = useCommonVarsAndFuncs();
	import { EntityFieldLength } from '/@/types/enums/baseEnum';

	import { useBaseConfig } from '/@/stores/baseConfig';
	import { BaseConfigTypeEnum, TmsEquipmentType } from '/@/types/enums';
import { TmsEquipmentTypeApi } from '/@/api-services/tms';
	
	const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
	// Chỉ các loại thiết bị thuộc module VMS: VMS + LCS
	const ALLOWED_TYPE_CODES = [TmsEquipmentType.Vms, TmsEquipmentType.LCS];
	// Giá trị sentinel cho lựa chọn "Tất cả" (el-select không hiển thị được value null)
	const ALL_EQUIPMENT_TYPE = 'ALL';
	const { t: $t } = useI18n();
	interface Props {
		operateType: OperateType;
		title: string;
	}

	const props = defineProps<Props>();
	const emits = defineEmits(['handleQuery']);
	const uploadInstanceRef = ref<UploadInstance>();

	const editFormRef = ref();
	const state = reactive({
		isShowDialog: false,
		isProcessing: false,
		isShowDialogPreview: false,
		editForm: {} as UpdateVmsImageInput,
		languageList: [] as any,
		imageTypeList: [] as any,
		imgSizeList: [] as any,
		icoSizeList: [] as any,
		uploadFileDim: [] as any,
		statusList: [] as any,
		equipmentTypeList: [] as any,
	});

	// Nguồn ảnh preview: ảnh vừa upload là data URI (base64)/blob -> dùng trực tiếp (KHÔNG ghép serverHost,
	// KHÔNG lowercase vì base64 phân biệt hoa/thường). Ảnh đã lưu trên server (path) -> ghép qua getFullUrl.
	const previewUrl = computed(() => {
		const u = (state.editForm.url ?? '').trim();
		if (!u) return '';
		return /^(data:|blob:)/i.test(u) ? u : commonVarsAndFuncs.getFullUrl(u);
	});

	// Proxy hiển thị cho Loại thiết bị: null/undefined (Tất cả) <-> sentinel 'ALL' để el-select hiển thị đúng.
	// editForm.equipmentTypeId vẫn giữ null khi chọn "Tất cả".
	const equipmentTypeIdProxy = computed({
		get: () => (state.editForm.equipmentTypeId == null ? ALL_EQUIPMENT_TYPE : state.editForm.equipmentTypeId),
		set: (v: any) => { state.editForm.equipmentTypeId = v === ALL_EQUIPMENT_TYPE ? null : v; },
	});

	// Danh sách kích thước load theo Loại thiết bị:
	// - "Tất cả" (equipmentTypeId = null) hoặc chưa xác định -> gộp cả icon (vms_size_icon) + image (vms_size_image)
	// - LCS -> kích thước icon (vms_size_icon); VMS (và mặc định) -> kích thước image (vms_size_image)
	//   (Nếu ánh xạ VMS/LCS ngược với nghiệp vụ, chỉ cần đổi 1 dòng dưới đây)
	const sizeOptions = computed(() => {
		const et = state.equipmentTypeList.find((t: any) => t.id === state.editForm.equipmentTypeId);
		if (!et) return [...(state.icoSizeList ?? []), ...(state.imgSizeList ?? [])];
		if (et.code === TmsEquipmentType.LCS) return state.icoSizeList ?? [];
		return state.imgSizeList ?? [];
	});

	// Đổi Loại thiết bị -> nếu kích thước đang chọn không còn hợp lệ thì xóa + xóa hình để chọn lại
	watch(() => state.editForm.equipmentTypeId, () => {
		const stillValid = sizeOptions.value.some((s: any) => s.code === state.editForm.size);
		if (!stillValid) {
			state.editForm.size = null;
			state.editForm.url = '';
			uploadInstanceRef.value?.clearFiles();
			state.uploadFileDim = [];
		}
	});

	onMounted(async () => {
		state.statusList = getConfigDataByCode(BaseConfigTypeEnum.SysStatusType);
		state.imgSizeList = getConfigDataByCode(BaseConfigTypeEnum.VmsImageSize);
		state.languageList = getConfigDataByCode(BaseConfigTypeEnum.SysLanguageType);
		state.imageTypeList = getConfigDataByCode(BaseConfigTypeEnum.VmsImageType);
		state.icoSizeList = getConfigDataByCode(BaseConfigTypeEnum.VmsIconSize);
		const resType = await getAPI(TmsEquipmentTypeApi).apiTmsTmsequipmenttypeListGet();
		state.equipmentTypeList = (resType.data?.result ?? []).filter((t: any) => t.isDelete == null && ALLOWED_TYPE_CODES.includes(t.code));

	});

	const openDialog = (row: any) => {
		editFormRef.value?.resetFields();
		state.editForm = JSON.parse(JSON.stringify(row));
		// Sao chép: không sao chép hình ảnh, người dùng tự upload hình mới
		if (props.operateType === 'copy') state.editForm.url = '';
		state.isShowDialog = true;
	};

	const closeDialog = () => {
		emits('handleQuery');
		state.isShowDialog = false;
	};

	const cancel = () => {
		state.isShowDialog = false;
	};

	// Handle upload
	const handleUpload = (file: any) => {
		// Clear upload files
		uploadInstanceRef.value?.clearFiles();

		
		// Reset uplad dimensions
		state.uploadFileDim  = [];

		// Get the allowed valid dimensions
		const validDim = state.editForm.size ? state.editForm.size.split('x') : [];
		if(validDim.length != 2){
			ElMessage.warning($t('lz.message.vmsImage.sizeIsRequired'));
			return;
		}

		// Get the allowed valid dimensions name
		const validDimName = state.editForm.type == 'icon' ? state.icoSizeList?.find((item: any) => item.code == state.editForm.size)?.name : state.imgSizeList?.find((item: any) => item.code == state.editForm.size)?.name;
		
		// Read the file for computation and processing
		const reader = new FileReader();
		reader.readAsDataURL(file.raw);

		// When the file reading is complete
		reader.onload = (eve) => {

			// Check the file dimensions
			const image = new Image();
			image.src = eve.target.result;

			image.onload = async() => {
				const { width, height } = image;
				
				const margin = 2;
				if ((width < parseInt(validDim[0]) - margin || width > parseInt(validDim[0]) + margin) || (height < parseInt(validDim[1]) - margin || height > parseInt(validDim[1]) + margin)) {
					state.uploadFileDim = [width, height]; // Use "uploadFileDim" to check the dimensions is valid before submitting to the server in vuejs
					ElMessage.error($t('lz.message.vmsImage.sizeIsInvalid', {size:validDimName}));
				}
				else{
					const base64String = reader.result as string;
					state.editForm.url = base64String;
				}
			};
		};

		reader.onerror = (error) => {
			ElMessage.error(error);
		};
	};

	const onImageTypeChange = (imgType) => {
		// Kích thước load theo Loại thiết bị (không reset theo Loại ảnh). Đổi Loại ảnh -> xóa hình cũ để chọn lại
		state.editForm.url = '';
		uploadInstanceRef.value?.clearFiles();
		state.uploadFileDim = [];
	}

	// Đổi kích thước -> xóa hình đã chọn để người dùng upload hình đúng kích thước mới
	const onSizeChange = () => {
		state.editForm.url = '';
		uploadInstanceRef.value?.clearFiles();
		state.uploadFileDim = [];
	}

	const submit = () => {
		editFormRef.value.validate(async (valid: boolean) => {
			
			if (!valid) return;

			try{
				state.isProcessing = true;

				//Edit
				if (props.operateType == 'edit') {
					await getAPI(VmsImageApi).apiVmsVmsimageUpdatePost(state.editForm);
				}
				//Copy or Add
				else if (props.operateType == 'copy' || props.operateType == 'add') {
					state.editForm.id = '';

					await getAPI(VmsImageApi).apiVmsVmsimageAddPost(state.editForm);
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

<style>
	.avatar-uploader{
		width: 100%;
		height: 200px;
	}

	.avatar-uploader .el-upload{
		width: 100%;
		height: 100%;
	}

	.avatar-uploader .el-upload {
		border: 1px dashed var(--el-border-color);
		border-radius: 6px;
		cursor: pointer;
		position: relative;
		overflow: hidden;
		transition: var(--el-transition-duration-fast);
	}

	.avatar-uploader .el-upload:has(img){
		width: auto;
		height: auto;
	}

	.avatar-uploader .el-upload:hover {
		border-color: var(--el-color-primary);
	}

	.el-icon.avatar-uploader-icon {
		font-size: 28px;
		color: #8c939d;
		display: flex;
		align-items: center;
		justify-content: center;
		width: 100%;
		height: 100%;
	}
</style>