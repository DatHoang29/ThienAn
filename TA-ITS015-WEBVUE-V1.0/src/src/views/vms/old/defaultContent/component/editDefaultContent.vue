<template>
	<el-dialog v-model="state.isShow" width="600px" draggable :close-on-click-modal="false" destroy-on-close class="default-content-dialog">
		<template #header>
			<div style="color: #fff">
				<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle">
					<ele-Edit />
				</el-icon>
				<span>{{ dialogTitle }}</span>
			</div>
		</template>
		<!-- Banner thiết bị: nền trắng, chỉ tên + code + kích thước -->
		<div class="device-banner">
			<div class="device-meta">
				<div class="device-name">{{ state.ruleForm.name || '-' }}</div>
				<div class="device-sub">
					<el-tag size="small" type="info">{{ state.ruleForm.code || '-' }}</el-tag>
					<span class="device-size">{{ previewSize }} px</span>
				</div>
			</div>
		</div>

		<el-form :model="state.ruleForm" ref="ruleFormRef" label-width="auto" label-position="top" class="mt15">
			<!-- ====== Chế độ LCS: chỉ điều khiển bằng hình ảnh (chọn ảnh thư viện hoặc upload mới) ====== -->
			<template v-if="isLcs">
				<el-form-item :label="$t('lz.label.base.image')">
					<el-select
						v-model="state.ruleForm.imgSelect"
						popper-class="simple-img_select"
						:placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.label.base.image') })"
						filterable
						clearable
						class="w100"
						@change="onImageSelectChange"
					>
						<el-option value="">
							<el-upload class="image-uploader" :auto-upload="false" :show-file-list="false" :on-change="onImageChange" :before-upload="beforeImageUpload">
								<el-button>{{ $t('lz.button.base.upload') }} {{ $t('lz.label.base.image') }}</el-button>
							</el-upload>
						</el-option>
						<el-option v-for="item in state.listData.imageList" :key="item.id" :label="item.name" :value="item.id">
							<img :src="buildImageUrl(item.url)" :alt="item.name" class="img-option-thumb" />
							<span class="img-option-name">{{ item.name }} ({{ item.size }}px)</span>
						</el-option>
					</el-select>
				</el-form-item>
			</template>

			<!-- ====== Chế độ VMS: nhập text + font/màu ====== -->
			<template v-else>
				<el-row :gutter="12">
					<el-col :span="24">
						<el-form-item :label="$t('lz.entity.vmsMessage.content1')" prop="row1">
							<el-input v-model="state.ruleForm.row1" clearable @input="schedulePreview">
								<template #prefix><span class="row-badge">1</span></template>
							</el-input>
						</el-form-item>
					</el-col>
					<el-col :span="24">
						<el-form-item :label="$t('lz.entity.vmsMessage.content2')" prop="row2">
							<el-input v-model="state.ruleForm.row2" clearable @input="schedulePreview">
								<template #prefix><span class="row-badge">2</span></template>
							</el-input>
						</el-form-item>
					</el-col>
				</el-row>

				<el-divider content-position="left" class="section-divider">{{ $t('lz.label.vmsMessage.font') }}</el-divider>

				<el-row :gutter="12">
					<el-col :span="12">
						<el-form-item :label="$t('lz.label.vmsMessage.textColor')">
							<el-select v-model="state.ruleForm.textColor" @change="schedulePreview" class="w100">
								<el-option v-for="c in state.listData.fontColorList" :key="c.code" :value="c.code" :label="c.name">
									<span class="color-dot" :style="{ backgroundColor: c.code }"></span>{{ c.name }}
								</el-option>
								<template #prefix><span class="color-dot" :style="{ backgroundColor: state.ruleForm.textColor }"></span></template>
							</el-select>
						</el-form-item>
					</el-col>
					<el-col :span="12">
						<el-form-item :label="$t('lz.label.vmsMessage.backgroundColor')">
							<el-select v-model="state.ruleForm.bgColor" @change="schedulePreview" class="w100">
								<el-option v-for="c in state.listData.bgColorList" :key="c.code" :value="c.code" :label="c.name">
									<span class="color-dot" :style="{ backgroundColor: c.code }"></span>{{ c.name }}
								</el-option>
								<template #prefix><span class="color-dot" :style="{ backgroundColor: state.ruleForm.bgColor }"></span></template>
							</el-select>
						</el-form-item>
					</el-col>
					<el-col :span="12">
						<el-form-item :label="$t('lz.label.vmsMessage.font')">
							<el-select v-model="state.ruleForm.font" @change="schedulePreview" class="w100">
								<el-option v-for="f in state.listData.fontList" :key="f.code" :value="f.code" :label="f.name" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col :span="12">
						<el-form-item :label="$t('lz.label.vmsMessage.fontSize')">
							<el-select v-model="state.ruleForm.fontSize" @change="schedulePreview" class="w100">
								<el-option v-for="s in state.listData.fontSizeList" :key="s.code" :value="s.code" :label="s.name" />
							</el-select>
						</el-form-item>
					</el-col>
				</el-row>
			</template>

			<!-- ====== Preview: mô phỏng bảng LED ====== -->
			<el-form-item :label="$t('lz.label.base.preview')">
				<div class="led-panel w100" v-loading="state.previewLoading" element-loading-background="rgba(0, 0, 0, 0.6)">
					<span class="led-size-chip">{{ previewSize }}</span>
					<img v-if="previewDisplaySrc" :src="previewDisplaySrc" class="led-img" :alt="$t('lz.label.base.preview')" />
					<span v-else class="led-empty">{{ $t('lz.label.base.noData') }}</span>
				</div>
			</el-form-item>
		</el-form>

		<template #footer>
			<el-button @click="state.isShow = false">{{ $t('lz.button.base.cancel') }}</el-button>
			<el-button type="primary" :loading="state.submitting" @click="submit">{{ $t('lz.button.base.confirm') }}</el-button>
		</template>
	</el-dialog>
</template>

<script lang="ts" setup name="vmsEditDefaultContent">
import { reactive, ref, computed, onMounted } from 'vue';
import { ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { debounce } from 'lodash-es';
import { getAPI } from '/@/utils/axios-utils';
import { VmsCurrentApi, VmsImageApi } from '/@/api-services/vms';
import { VmsActionType, VmsImageType, VmsSizeEnum } from '/@/types/enums/vmsEnum';
import { BaseConfigTypeEnum } from '/@/types/enums/baseEnum';
import { useBaseConfig } from '/@/stores/baseConfig';
import { useMapStore } from '/@/stores/its/mapStore';
import { fetchVmsPreviewBase64 } from '/@/components/vms/controlVms/composables/useVmsPreview';
import { TmsEquipmentType } from '/@/types/enums';

const { t: $t } = useI18n();
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
const mapStore = useMapStore();

// Mã loại thiết bị + size cố định LCS (khớp BE: LcsSize trong EventDefaultCommandHandler)
const EQUIPMENT_TYPE_CODE_LCS = TmsEquipmentType.LCS;
const VMS_SIZE = VmsSizeEnum.Vms;
const LCS_SIZE = VmsSizeEnum.Lcs;

interface Props {
	// map equipmentId -> thiết bị (để lấy width/height dựng size preview + equipmentTypeId)
	equipmentMap: Record<string, any>;
	// danh sách loại thiết bị VMS/LCS để xác định thiết bị đang sửa có phải LCS không
	equipmentTypeList: any[];
}
const props = withDefaults(defineProps<Props>(), { equipmentMap: () => ({}), equipmentTypeList: () => [] });

// Thiết bị đang sửa là LCS -> chỉ điều khiển bằng hình ảnh
const isLcs = computed(() => {
	const equipment = props.equipmentMap?.[state.ruleForm.equipmentId];
	const type = props.equipmentTypeList.find((t: any) => t.id === equipment?.equipmentTypeId);
	return type?.code === EQUIPMENT_TYPE_CODE_LCS;
});

interface Emits {
	(e: 'handleQuery'): void;
}
const emit = defineEmits<Emits>();

const ruleFormRef = ref();

const state = reactive<any>({
	isShow: false,
	submitting: false,
	previewLoading: false,
	previewSrc: '',
	ruleForm: {
		id: '',
		equipmentId: '',
		code: '',
		name: '',
		imgSelect: '', // id ảnh đang chọn từ thư viện VmsImage (chế độ LCS); rỗng = ảnh upload mới/chưa chọn
		row1: '',
		row2: '',
		textColor: 'White',
		bgColor: 'Black',
		font: 'Arial',
		fontSize: 14,
	},
	listData: {
		fontColorList: [] as any,
		bgColorList: [] as any,
		fontList: [] as any,
		fontSizeList: [] as any,
		imageList: [] as any, // thư viện ảnh VMS (VmsImage, type = 'image') dùng cho LCS
	},
});

const dialogTitle = computed(() => $t('lz.label.base.titleEdit', { name: $t('lz.label.vms.defaultContent') }));

// Kích thước preview theo thiết bị (fallback: LCS 128x128, VMS 384x64) — giống buildPreviewInput của controlVms
const previewSize = computed(() => {
	const e = props.equipmentMap?.[state.ruleForm.equipmentId];
	const w = Number(e?.width);
	const h = Number(e?.height);
	if (w > 0 && h > 0) return `${w}x${h}`;
	return isLcs.value ? LCS_SIZE : VMS_SIZE;
});

// Ghép URL ảnh để hiển thị: base64 (data:) dùng trực tiếp, còn lại (ảnh thư viện/đã lưu) nối baseUrl
const buildImageUrl = (u?: string) => (u ? (String(u).startsWith('data:') ? u : `${mapStore.baseUrl}/${u}`) : '');
// Nguồn ảnh hiển thị ở preview: LCS ghép baseUrl (ảnh thư viện là path tương đối), VMS là base64 render sẵn
const previewDisplaySrc = computed(() => (isLcs.value ? buildImageUrl(state.previewSrc) : state.previewSrc));

// Parse DefaultRowData -> row1/row2
const parseDefaultRowData = (raw: any) => {
	try {
		const obj = typeof raw === 'string' ? JSON.parse(raw) : raw || {};
		return { row1: obj?.Row1 ?? obj?.row1 ?? '', row2: obj?.Row2 ?? obj?.row2 ?? '' };
	} catch {
		return { row1: '', row2: '' };
	}
};

// Nạp thư viện ảnh đúng loại thiết bị (VMS/LCS). Không có equipmentTypeId -> rỗng.
const loadImageLibrary = async (equipmentTypeId?: string) => {
  if (!equipmentTypeId) { state.listData.imageList = []; return; }
  const res = await getAPI(VmsImageApi).apiVmsVmsimageListPost({ type: VmsImageType.Image, equipmentTypeId });
  state.listData.imageList = res.data?.result ?? [];
};

// Mở dialog edit: nạp dữ liệu row, parse defaultRowData, load thư viện ảnh theo loại thiết bị
const openDialog = async (row: any) => {
    const deviceTypeId = props.equipmentMap?.[row.equipmentId]?.equipmentTypeId;  // Lấy equipmentTypeId của thiết bị đang sửa
	await loadImageLibrary(deviceTypeId);		// Load thư viện ảnh theo loại thiết bị (VMS/LCS) để chọn ảnh đúng loại
	const { row1, row2 } = parseDefaultRowData(row.defaultRowData);
	const rowIsLcs = props.equipmentTypeList.find((t: any) => t.id === props.equipmentMap?.[row.equipmentId]?.equipmentTypeId)?.code === EQUIPMENT_TYPE_CODE_LCS;
	// Nếu defaultUrl khớp 1 ảnh trong thư viện -> chọn lại đúng ảnh đó trong select (chế độ LCS)
	const matchedImage = rowIsLcs ? state.listData.imageList.find((item: any) => item.url === row.defaultUrl) : null;

	state.ruleForm = {
		id: row.id,
		equipmentId: row.equipmentId,
		code: row.code,
		name: row.name,
		imgSelect: matchedImage?.id || '',
		row1,
		row2,
		textColor: row.defaultColor || 'White',
		bgColor: row.defaultBackColor || 'Black',
		font: row.defaultFont || 'Arial',
		fontSize: 14,
	};
	// LCS: hiển thị ảnh hiện có (path tương đối), không gọi API gen ảnh; VMS: gen preview text
	state.previewSrc = rowIsLcs && row.defaultUrl ? row.defaultUrl : '';
	state.isShow = true;
	if (!rowIsLcs) updatePreview();
};

// Chọn ảnh có sẵn từ thư viện VmsImage -> lấy đường dẫn tương đối (chế độ LCS)
const onImageSelectChange = () => {
	const selected = state.listData.imageList.find((item: any) => item.id === state.ruleForm.imgSelect);
	state.previewSrc = selected?.url || '';
};

// Upload ảnh mới -> đọc thành base64 (chế độ LCS), bỏ chọn ảnh thư viện
const onImageChange = (uploadFile: any) => {
	const file = uploadFile?.raw;
	if (!file) return;
	state.ruleForm.imgSelect = '';
	const reader = new FileReader();
	reader.onload = () => {
		state.previewSrc = String(reader.result || '');
	};
	reader.readAsDataURL(file);
};

// Kiểm tra file trước khi upload
const beforeImageUpload = (file: File) => {
	const isImage = file.type.startsWith('image/');
	if (!isImage) ElMessage.error($t('lz.message.vmsMassage.errorBeforeUploadImage'));
	return isImage;
};

// Gọi API preview (base64) - chỉ chế độ VMS (nhập text, gen hình); LCS dùng ảnh trực tiếp
const updatePreview = async () => {
	if (isLcs.value) return;
	state.previewLoading = true;
	state.previewSrc = await fetchVmsPreviewBase64({
		controlType: VmsActionType.Text,
		size: previewSize.value,
		content1: state.ruleForm.row1 ?? '',
		content2: state.ruleForm.row2 ?? '',
		color: state.ruleForm.textColor,
		backColor: state.ruleForm.bgColor,
		font: state.ruleForm.font,
		fontSize: String(state.ruleForm.fontSize ?? ''),
		brightness: 100,
	});
	state.previewLoading = false;
};

const schedulePreview = debounce(updatePreview, 500);

const submit = async () => {
	// Đảm bảo preview được cập nhật mới nhất theo text hiện tại trước khi gửi
    if (!isLcs.value) {
        await updatePreview();
    }
	// Validate tối thiểu theo chế độ
	if (isLcs.value) {
		if (!state.previewSrc) return ElMessage.warning($t('lz.label.base.noData'));
	} else if (!state.ruleForm.row1 && !state.ruleForm.row2) {
		return ElMessage.warning($t('lz.label.base.noData'));
	}

	state.submitting = true;
	try {
		const payload = isLcs.value
			? {
					// LCS: chỉ ảnh, không có nội dung text/font
					equipmentId: state.ruleForm.equipmentId,
					code: state.ruleForm.code,
					name: state.ruleForm.name,
					defaultRowData: '',
					defaultUrl: state.previewSrc, // path thư viện hoặc base64 (BE tự lưu file)
					size: previewSize.value,
					actionType: VmsActionType.Image,
			  }
			: {
					// VMS: JSON nội dung + ảnh render (base64 -> BE lưu file)
					equipmentId: state.ruleForm.equipmentId,
					code: state.ruleForm.code,
					name: state.ruleForm.name,
					defaultRowData: JSON.stringify({ Row1: state.ruleForm.row1, Row2: state.ruleForm.row2 }),
					defaultUrl: state.previewSrc,
					defaultColor: state.ruleForm.textColor,
					defaultBackColor: state.ruleForm.bgColor,
					defaultFont: state.ruleForm.font,
					size: previewSize.value,
					actionType: VmsActionType.Text,
			  };
		await getAPI(VmsCurrentApi).apiVmsVmscurrentAdddefaultcontentPost(payload as any);
		ElMessage.success($t('lz.message.base.processSuccess'));
		state.isShow = false;
		emit('handleQuery');
	} catch (error) {
		ElMessage.error($t('lz.message.base.processUnSuccess'));
	} finally {
		state.submitting = false;
	}
};

// Nạp dữ liệu config (token, font, color) + thư viện ảnh VMS khi component mounted
onMounted(async () => {
	state.listData.fontColorList = getConfigDataByCode(BaseConfigTypeEnum.VmsFontColor);
	state.listData.bgColorList = getConfigDataByCode(BaseConfigTypeEnum.VmsBackgroundColor);
	state.listData.fontList = getConfigDataByCode(BaseConfigTypeEnum.VmsFontName);
	state.listData.fontSizeList = getConfigDataByCode(BaseConfigTypeEnum.VmsFontSize);

	// Đảm bảo có baseUrl để ghép URL ảnh thư viện (chế độ LCS)
	if (!mapStore.baseUrl) {
		try {
			await mapStore.fetchSystemSysconfigFileserverGet();
		} catch (e) {
			console.error('Error fetching file server:', e);
		}
	}

	await loadImageLibrary(); // Load thư viện ảnh mặc định (rỗng) khi mounted, sau đó sẽ load lại khi chọn loại thiết bị
});

defineExpose({ openDialog });
</script>

<style lang="scss">
/* KHÔNG scoped: dropdown el-select teleport ra body, style scoped không với tới.
   Đồng bộ giao diện thẻ chọn ảnh với editEventDefaultContent (LCS). */
.simple-img_select {
	max-height: 50vh;
	overflow-y: auto;

	.el-select-dropdown__list {
		display: grid;
		grid-template-columns: repeat(2, 1fr);
		gap: 8px;
		padding: 10px;
	}

	.el-select-dropdown__item {
		height: auto;
		padding: 10px 8px;
		display: flex;
		flex-direction: column;
		align-items: center;
		gap: 8px;
		line-height: normal;
		border: 1px solid var(--el-border-color-lighter);
		border-radius: 8px;

		&.is-selected,
		&:hover {
			border-color: var(--el-color-primary);
			background: var(--el-color-primary-light-9);
		}

		&:has(.image-uploader) {
			grid-column: 1 / -1;
			flex-direction: row;
			justify-content: center;
		}
	}

	.img-option-thumb {
		width: 100%;
		max-height: 64px;
		object-fit: contain;
		background: var(--el-fill-color-light);
		border-radius: 6px;
		padding: 4px;
	}

	.img-option-name {
		font-size: 12px;
		color: var(--el-text-color-regular);
		text-align: center;
		white-space: normal;
		word-break: break-word;
		width: 100%;
	}
}
</style>

<style scoped lang="scss">
.mt15 {
	margin-top: 15px;
}

/* Banner thiết bị: nền trắng theo theme, không icon, chỉ tên + code */
.device-banner {
	display: flex;
	align-items: center;
	padding: 12px 14px;
	border: 1px solid var(--el-border-color-light);
	border-radius: 8px;
	background: var(--el-bg-color);

	.device-meta {
		flex: 1;
		min-width: 0;
		.device-name {
			font-weight: 600;
			color: var(--el-text-color-primary);
			white-space: nowrap;
			overflow: hidden;
			text-overflow: ellipsis;
		}
		.device-sub {
			margin-top: 4px;
			display: flex;
			align-items: center;
			gap: 8px;
			.device-size {
				font-size: 13px;
				color: var(--el-text-color-secondary);
			}
		}
	}
}

/* Badge số dòng trong input */
.row-badge {
	display: inline-flex;
	align-items: center;
	justify-content: center;
	width: 18px;
	height: 18px;
	border-radius: 50%;
	font-size: 13px;
	font-weight: 600;
	color: var(--el-color-primary);
	background: var(--el-color-primary-light-9);
}

/* Chấm màu trong select màu chữ / màu nền */
.color-dot {
	display: inline-block;
	width: 12px;
	height: 12px;
	margin-right: 6px;
	border-radius: 50%;
	border: 1px solid var(--el-border-color);
	vertical-align: middle;
}

.section-divider {
	margin: 20px 0 16px;
	:deep(.el-divider__text) {
		font-size: 13px;
		color: var(--el-text-color-secondary);
		background-color: var(--el-bg-color);
	}
}

/* Preview mô phỏng bảng LED: nền tối cố định để giống thiết bị thật */
.led-panel {
	position: relative;
	display: flex;
	align-items: center;
	justify-content: center;
	min-height: 110px;
	padding: 14px;
	border-radius: 8px;
	background: var(--el-color-black, #000);
	border: 3px solid var(--el-border-color-darker);
	box-shadow: inset 0 0 12px rgba(0, 0, 0, 0.8);

	.led-size-chip {
		position: absolute;
		top: 6px;
		right: 8px;
		font-size: 13px;
		line-height: 1;
		padding: 3px 6px;
		border-radius: 4px;
		color: var(--el-color-white, #fff);
		background: rgba(255, 255, 255, 0.15);
	}
	.led-img {
		max-width: 100%;
		max-height: 220px;
		object-fit: contain;
		image-rendering: pixelated; /* giữ cảm giác điểm ảnh LED khi phóng to */
	}
	.led-empty {
		color: var(--el-text-color-placeholder);
	}
}
</style>