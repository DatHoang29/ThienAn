<template>
	<el-dialog v-model="state.isShow" width="640px" draggable :close-on-click-modal="false" destroy-on-close class="event-default-dialog">
		<template #header>
			<div style="color: #fff">
				<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle">
					<ele-Edit />
				</el-icon>
				<span>{{ dialogTitle }}</span>
			</div>
		</template>
		<el-form :model="state.ruleForm" ref="ruleFormRef" label-width="auto" label-position="top">
			<el-row :gutter="12">
				<el-col :span="12" class="mb20">
					<el-form-item
						:label="$t('lz.entity.vmsEventDefault.eventTypeId')"
						prop="eventTypeId"
						:rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.vmsEventDefault.eventTypeId') }), trigger: 'change' }]"
					>
						<el-select v-model="state.ruleForm.eventTypeId" filterable clearable class="w100" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.vmsEventDefault.eventTypeId') })">
							<el-option v-for="item in props.eventTypeList" :key="item.id" :label="item.name" :value="item.id" />
						</el-select>
					</el-form-item>
				</el-col>
				<el-col :span="12" class="mb20">
					<el-form-item
						:label="$t('lz.entity.vmsEventDefault.equipmentTypeId')"
						prop="equipmentTypeId"
						:rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.vmsEventDefault.equipmentTypeId') }), trigger: 'change' }]"
					>
						<el-select v-model="state.ruleForm.equipmentTypeId" filterable clearable class="w100" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.vmsEventDefault.equipmentTypeId') })" @change="onEquipmentTypeChange">
							<el-option v-for="item in props.equipmentTypeList" :key="item.id" :label="item.name" :value="item.id" />
						</el-select>
					</el-form-item>
				</el-col>
			</el-row>

			<!-- Chế độ phát sự cố & Thời gian hiển thị -->
			<el-row :gutter="12">
				<el-col :span="state.ruleForm.defaultPlayMode === 'append' ? 14 : 24" class="mb20">
					<el-form-item :label="$t('lz.label.vms.playMode')">
						<el-checkbox
							v-model="state.ruleForm.defaultPlayMode"
							true-value="append"
							false-value="override"
						>
							{{ $t('lz.label.vms.playModeAppendCheckbox') }}
						</el-checkbox>
						<el-tooltip :content="$t('lz.label.vms.playModeTooltip')" placement="top">
							<el-icon class="ml5" style="vertical-align: middle; cursor: pointer; color: #909399;"><ele-QuestionFilled /></el-icon>
						</el-tooltip>
					</el-form-item>
				</el-col>
				<el-col :span="10" class="mb20" v-if="state.ruleForm.defaultPlayMode === 'append'">
					<el-form-item :label="$t('lz.label.vms.timeToLive')">
						<el-input-number
							v-model="state.ruleForm.timeToLive"
							:min="5"
							:max="300"
							:step="5"
							controls-position="right"
							class="w100"
						/>
					</el-form-item>
				</el-col>
			</el-row>

			<!-- ====== Chế độ LCS: chọn ảnh có sẵn từ thư viện hình ảnh VMS (hoặc upload mới), size cố định 128x128 ====== -->
			<template v-if="isLcs">
				<el-form-item :label="$t('lz.label.base.image')" class="mb20">
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

			<!-- ====== Chế độ VMS: nhập text + token + font, size cố định 384x64 ====== -->
			<template v-else>
				<!-- Token nội dung động -->
				<el-form-item :label="$t('lz.label.vmsEventDefault.dynamicContent')" class="mb20">
					<div class="token-bar">
						<el-tag v-for="tk in state.listData.tokenList" :key="tk.code" class="token-chip" type="info" effect="plain" @click="insertToken(tk.code)">
							{{ tk.name }}
						</el-tag>
						<span v-if="!state.listData.tokenList.length" class="token-empty">{{ $t('lz.label.base.noData') }}</span>
					</div>
				</el-form-item>

				<el-row :gutter="12">
					<el-col :span="24" class="mb20">
						<el-form-item :label="$t('lz.entity.vmsMessage.content1')">
							<el-input ref="row1Ref" v-model="state.ruleForm.row1" clearable @focus="state.activeField = 'row1'" @input="schedulePreview">
								<template #prefix><span class="row-badge">1</span></template>
							</el-input>
						</el-form-item>
					</el-col>
					<el-col :span="24" class="mb20">
						<el-form-item :label="$t('lz.entity.vmsMessage.content2')">
							<el-input ref="row2Ref" v-model="state.ruleForm.row2" clearable @focus="state.activeField = 'row2'" @input="schedulePreview">
								<template #prefix><span class="row-badge">2</span></template>
							</el-input>
						</el-form-item>
					</el-col>
				</el-row>

				<el-divider content-position="left" class="section-divider">{{ $t('lz.label.vmsMessage.font') }}</el-divider>

				<el-row :gutter="12">
					<el-col :span="12" class="mb20">
						<el-form-item :label="$t('lz.label.vmsMessage.fontSize')">
							<el-select v-model="state.ruleForm.fontSize" @change="schedulePreview" class="w100">
								<el-option v-for="s in state.listData.fontSizeList" :key="s.code" :value="s.code" :label="s.name" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col :span="12" class="mb20">
						<el-form-item :label="$t('lz.label.vmsMessage.textColor')">
							<el-select v-model="state.ruleForm.textColor" @change="schedulePreview" class="w100">
								<el-option v-for="c in state.listData.fontColorList" :key="c.code" :value="c.code" :label="c.name">
									<span class="color-dot" :style="{ backgroundColor: c.code }"></span>{{ c.name }}
								</el-option>
								<template #prefix><span class="color-dot" :style="{ backgroundColor: state.ruleForm.textColor }"></span></template>
							</el-select>
						</el-form-item>
					</el-col>
					<el-col :span="12" class="mb20">
						<el-form-item :label="$t('lz.label.vmsMessage.backgroundColor')">
							<el-select v-model="state.ruleForm.bgColor" @change="schedulePreview" class="w100">
								<el-option v-for="c in state.listData.bgColorList" :key="c.code" :value="c.code" :label="c.name">
									<span class="color-dot" :style="{ backgroundColor: c.code }"></span>{{ c.name }}
								</el-option>
								<template #prefix><span class="color-dot" :style="{ backgroundColor: state.ruleForm.bgColor }"></span></template>
							</el-select>
						</el-form-item>
					</el-col>
					<el-col :span="12" class="mb20">
						<el-form-item :label="$t('lz.label.vmsMessage.font')">
							<el-select v-model="state.ruleForm.font" @change="schedulePreview" class="w100">
								<el-option v-for="f in state.listData.fontList" :key="f.code" :value="f.code" :label="f.name" />
							</el-select>
						</el-form-item>
					</el-col>
				</el-row>
			</template>

			<!-- ====== Preview mô phỏng bảng LED ====== -->
			<el-form-item :label="$t('lz.label.base.preview')" class="mb20">
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

<script lang="ts" setup name="vmsEditEventDefaultContent">
import { reactive, ref, computed, nextTick, onMounted } from 'vue';
import { ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { debounce } from 'lodash-es';
import { getAPI } from '/@/utils/axios-utils';
import { VmsEventDefaultApi, VmsImageApi } from '/@/api-services/vms';
import { VmsActionType, VmsImageType, VmsSizeEnum } from '/@/types/enums/vmsEnum';
import { BaseConfigTypeEnum } from '/@/types/enums/baseEnum';
import { useBaseConfig } from '/@/stores/baseConfig';
import { useMapStore } from '/@/stores/its/mapStore';
import { fetchVmsPreviewBase64 } from '/@/components/vms/controlVms/composables/useVmsPreview';
import { TmsEquipmentType } from '/@/types/enums';

const { t: $t } = useI18n();
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
const mapStore = useMapStore();

interface Props {
	eventTypeList: any[]; // danh sách loại sự kiện sự cố để chọn
	equipmentTypeList: any[]; // danh sách loại thiết bị VMS/LCS để chọn
}
const props = withDefaults(defineProps<Props>(), { eventTypeList: () => [], equipmentTypeList: () => [] });

// Mã loại thiết bị + size cố định theo yêu cầu (khớp BE: VmsSize/LcsSize trong EventDefaultCommandHandler)
const EQUIPMENT_TYPE_CODE_LCS = TmsEquipmentType.LCS;
const VMS_SIZE = VmsSizeEnum.Vms;
const LCS_SIZE = VmsSizeEnum.Lcs;

interface Emits {
	(e: 'handleQuery'): void;
}
const emit = defineEmits<Emits>();

const ruleFormRef = ref();
const row1Ref = ref();
const row2Ref = ref();

const state = reactive<any>({
	isShow: false,
	submitting: false,
	previewLoading: false,
	previewSrc: '',
	operateType: 'add',
	activeField: 'row1', // ô nội dung đang focus (để chèn token)
	ruleForm: {
		id: '',
		eventTypeId: '',
		equipmentTypeId: '',
		imgSelect: '', // id ảnh đang chọn từ thư viện VmsImage (chế độ LCS); rỗng = ảnh upload mới/chưa chọn
		defaultPlayMode: 'override',
		timeToLive: 30,
		row1: '',
		row2: '',
		textColor: 'White',
		bgColor: 'Black',
		font: 'Arial',
		fontSize: 14,
		useForEvent: false,
	},
	listData: {
		tokenList: [] as any[],
		fontColorList: [] as any[],
		bgColorList: [] as any[],
		fontList: [] as any[],
		fontSizeList: [] as any[],
		imageList: [] as any[], // thư viện ảnh VMS (VmsImage, type = 'image') dùng để đăng ký cho LCS
	},
});

const dialogTitle = computed(() =>
	state.operateType === 'add'
		? $t('lz.label.base.titleAdd', { name: $t('lz.label.vms.eventDefaultContent') })
		: $t('lz.label.base.titleEdit', { name: $t('lz.label.vms.eventDefaultContent') })
);

// LCS -> chỉ nhận hình ảnh, size cố định 128x128; VMS -> nhập text, size cố định 384x64
const isLcs = computed(() => {
	const item = props.equipmentTypeList.find((e: any) => e.id === state.ruleForm.equipmentTypeId);
	return item?.code === EQUIPMENT_TYPE_CODE_LCS;
});

const previewSize = computed(() => (isLcs.value ? LCS_SIZE : VMS_SIZE));

// Ghép URL ảnh để hiển thị: base64 (data:) dùng trực tiếp, còn lại (ảnh thư viện/đã lưu) nối baseUrl
const buildImageUrl = (u?: string) => (u ? (String(u).startsWith('data:') ? u : `${mapStore.baseUrl}/${u}`) : '');
const previewDisplaySrc = computed(() => (isLcs.value ? buildImageUrl(state.previewSrc) : state.previewSrc));

// Parse RowData -> row1/row2
const parseRowData = (raw: any) => {
	try {
		const obj = typeof raw === 'string' ? JSON.parse(raw) : raw || {};
		return { row1: obj?.Row1 ?? obj?.row1 ?? '', row2: obj?.Row2 ?? obj?.row2 ?? '' };
	} catch {
		return { row1: '', row2: '' };
	}
};

// Parse DefaultConfig -> lấy fontSize + useForEvent khi sửa
const parseDefaultConfig = (raw: any) => {
	try {
		return typeof raw === 'string' ? JSON.parse(raw) : raw || {};
	} catch {
		return {};
	}
};

const openDialog = (row: any) => {
	if (row) {
		state.operateType = 'edit';
		const { row1, row2 } = parseRowData(row.rowData);
		const cfg = parseDefaultConfig(row.defaultConfig);
		const isRowLcs = props.equipmentTypeList.find((e: any) => e.id === row.equipmentTypeId)?.code === EQUIPMENT_TYPE_CODE_LCS;
		// Nếu url của bản ghi khớp 1 ảnh trong thư viện -> chọn lại đúng ảnh đó trong select
		const matchedImage = isRowLcs ? state.listData.imageList.find((item: any) => item.url === row.url) : null;
		state.ruleForm = {
			id: row.id,
			eventTypeId: row.eventTypeId,
			equipmentTypeId: row.equipmentTypeId,
			imgSelect: matchedImage?.id || '',
			defaultPlayMode: row.defaultPlayMode || 'override',
			timeToLive: row.timeToLive != null ? row.timeToLive : 30,
			row1,
			row2,
			textColor: row.color || 'White',
			bgColor: row.backColor || 'Black',
			font: row.font || 'Arial',
			fontSize: cfg.FontSize || 14,
			useForEvent: !!cfg.UseForEvent,
		};
		// Ảnh hiện có (chế độ LCS) để hiển thị preview ban đầu, không tự gọi lại API gen ảnh
		state.previewSrc = isRowLcs && row.url ? row.url : '';
	} else {
		state.operateType = 'add';
		state.ruleForm = {
			id: '',
			eventTypeId: '',
			equipmentTypeId: '',
			imgSelect: '',
			defaultPlayMode: 'override',
			timeToLive: 30,
			row1: '',
			row2: '',
			textColor: 'White',
			bgColor: 'Black',
			font: 'Arial',
			fontSize: 14,
			useForEvent: false,
		};
		state.previewSrc = '';
	}
	state.isShow = true;
	if (!isLcs.value) updatePreview();
	nextTick(() => ruleFormRef.value?.clearValidate());
};

// Khi đổi loại thiết bị: reset preview/nội dung phù hợp với chế độ mới
const onEquipmentTypeChange = () => {
	state.previewSrc = '';
	state.ruleForm.imgSelect = '';
	loadImageLibrary(state.ruleForm.equipmentTypeId);
	if (!isLcs.value) updatePreview();
};

// Chọn ảnh có sẵn từ thư viện VmsImage -> lấy đường dẫn tương đối (giống updateSelectedImageUrl của controlVms)
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

// Kiểm tra file trước khi upload (giống beforeUpload của controlVms)
const beforeImageUpload = (file: File) => {
	const isImage = file.type.startsWith('image/');
	if (!isImage) ElMessage.error($t('lz.message.vmsMassage.errorBeforeUploadImage'));
	return isImage;
};

// Chèn token vào ô nội dung đang focus (tại vị trí con trỏ)
const insertToken = (token: string) => {
	const field = state.activeField === 'row2' ? 'row2' : 'row1';
	const compRef = field === 'row2' ? row2Ref.value : row1Ref.value;
	const inputEl: HTMLInputElement | undefined = compRef?.input;
	const current: string = state.ruleForm[field] ?? '';

	let start = current.length;
	let end = current.length;
	if (inputEl && typeof inputEl.selectionStart === 'number') {
		start = inputEl.selectionStart;
		end = inputEl.selectionEnd ?? start;
	}
	state.ruleForm[field] = current.slice(0, start) + token + current.slice(end);

	nextTick(() => {
		if (inputEl) {
			const pos = start + token.length;
			inputEl.focus();
			inputEl.setSelectionRange(pos, pos);
		}
		schedulePreview();
	});
};

// Gọi API preview (base64) - tái sử dụng luồng hiện có (chỉ chế độ VMS: nhập text, gen hình)
const updatePreview = async () => {
	if (isLcs.value) return; // LCS dùng ảnh upload, không gen preview text
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

// Submit form: validate + gọi API add/update
const submit = () => {
	ruleFormRef.value?.validate(async (valid: boolean) => {
		if (!valid) return;

		if (isLcs.value) {
			if (!state.previewSrc) return ElMessage.warning($t('lz.label.base.noData'));
		} else if (!state.ruleForm.row1 && !state.ruleForm.row2) {
			return ElMessage.warning($t('lz.label.base.noData'));
		}

		const payload = {
			eventTypeId: state.ruleForm.eventTypeId,
			equipmentTypeId: state.ruleForm.equipmentTypeId,
			size: previewSize.value,
			rowData: isLcs.value ? '' : JSON.stringify({ Row1: state.ruleForm.row1, Row2: state.ruleForm.row2 }),
			url: state.previewSrc,
			font: state.ruleForm.font,
			color: state.ruleForm.textColor,
			backColor: state.ruleForm.bgColor,
			fontSize: String(state.ruleForm.fontSize ?? ''),
			defaultPlayMode: state.ruleForm.defaultPlayMode,
			timeToLive: Number(state.ruleForm.timeToLive) || 30,
			useForEvent: state.ruleForm.useForEvent,
		} as any;

		state.submitting = true;
		try {
			if (state.operateType === 'edit') {
				payload.id = state.ruleForm.id;
				await getAPI(VmsEventDefaultApi).apiVmsVmseventdefaultUpdatePost(payload);
			} else {
				await getAPI(VmsEventDefaultApi).apiVmsVmseventdefaultAddPost(payload);
			}
			ElMessage.success($t('lz.message.base.processSuccess'));
			state.isShow = false;
			emit('handleQuery');
		} catch (error) {
			// Interceptor axios đã hiển thị message lỗi cụ thể từ BE (vd: sự kiện đã có nội dung mặc định) -> không hiện toast chung đè lên
		} finally {
			state.submitting = false;
		}
	});
};


// Nạp thư viện ảnh đúng loại thiết bị (VMS/LCS). Không có equipmentTypeId -> rỗng.
const loadImageLibrary = async (equipmentTypeId?: string) => {
  if (!equipmentTypeId) { state.listData.imageList = []; return; }
  const res = await getAPI(VmsImageApi).apiVmsVmsimageListPost({ type: VmsImageType.Image, equipmentTypeId });
  state.listData.imageList = res.data?.result ?? [];
};


// Nạp dữ liệu config (token, font, color) + thư viện ảnh VMS khi component mounted
onMounted(async () => {
	state.listData.tokenList = getConfigDataByCode(BaseConfigTypeEnum.VmsContentDefault) || [];
	state.listData.fontColorList = getConfigDataByCode(BaseConfigTypeEnum.VmsFontColor) || [];
	state.listData.bgColorList = getConfigDataByCode(BaseConfigTypeEnum.VmsBackgroundColor) || [];
	state.listData.fontList = getConfigDataByCode(BaseConfigTypeEnum.VmsFontName) || [];
	state.listData.fontSizeList = getConfigDataByCode(BaseConfigTypeEnum.VmsFontSize) || [];

	// Đảm bảo có baseUrl để ghép URL ảnh (thư viện + ảnh đã lưu)
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
   Đồng bộ giao diện thẻ chọn ảnh với controlVms (LCS). */
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
.token-bar {
	display: flex;
	flex-wrap: wrap;
	gap: 6px;
	width: 100%;
	max-height: 96px;      /* ~3 hàng */
	overflow-y: auto;
	padding: 2px;
	
	.token-chip {
		cursor: pointer;
		user-select: none;
	}
	.token-chip:hover {
		color: var(--el-color-primary);
		border-color: var(--el-color-primary);
	}
	.token-empty {
		color: var(--el-text-color-placeholder);
	}
}

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
		image-rendering: pixelated;
	}
	.led-empty {
		color: var(--el-text-color-placeholder);
	}
}
</style>
