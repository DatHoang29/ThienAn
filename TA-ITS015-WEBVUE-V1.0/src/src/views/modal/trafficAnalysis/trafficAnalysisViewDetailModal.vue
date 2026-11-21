<template>
	<el-dialog v-model="dialogVisible" width="min(1100px, 96vw)" top="5vh" class="traffic-analysis-detail-dialog" destroy-on-close>
		<template #header>
			<div class="dialog-heading">
				<el-icon size="18"><ele-View /></el-icon>
				<span>Chi tiết phân tích giao thông</span>
			</div>
		</template>

		<div class="analysis-detail-content">
			<section class="analysis-hero">
				<div>
					<p class="analysis-hero__eyebrow">THÔNG TIN PHƯƠNG TIỆN</p>
					<h2>{{ vehicleTypeValue }}</h2>
					<p>{{ formatDate(detailData.detectTime) }} · {{ equipmentValue }}</p>
				</div>
				<div class="analysis-hero__plate">
					<span>BIỂN SỐ</span>
					<strong>{{ displayValue(detailData.licensePlate) }}</strong>
				</div>
			</section>

			<el-tabs v-model="activeTab" class="analysis-tabs">
				<el-tab-pane label="Thông tin cơ bản" name="basic">
					<el-descriptions :column="2" border class="analysis-descriptions">
						<el-descriptions-item v-for="item in basicDetails" :key="item.label" :label="item.label">
							{{ item.value }}
						</el-descriptions-item>
					</el-descriptions>

					<section class="media-section">
						<div class="section-title">
							<el-icon><Picture /></el-icon>
							<span>Hình ảnh phương tiện</span>
						</div>
						<div class="media-gallery">
							<article v-for="image in state.imageList" :key="image.key" class="media-card">
								<div class="media-card__title">{{ image.title }}</div>
								<div v-if="image.url" class="media-card__image">
									<el-image
										:src="image.url"
										:preview-src-list="imagePreviewUrls"
										:initial-index="getImagePreviewIndex(image.url)"
										:zoom-rate="1.2"
										:max-scale="7"
										:min-scale="0.2"
										preview-teleported
										fit="contain"
										class="analysis-image"
									/>
									<div class="image-zoom-hint"><el-icon><ZoomIn /></el-icon> Bấm ảnh để xem lớn</div>
								</div>
								<div v-else class="image-placeholder">
									<el-icon :size="32"><Picture /></el-icon>
									<span>{{ state.imageLoading ? 'Đang tải ảnh...' : 'Chưa có ảnh' }}</span>
								</div>
							</article>
						</div>
					</section>
				</el-tab-pane>

				<el-tab-pane label="Thông tin khác" name="other">
					<el-descriptions :column="2" border class="analysis-descriptions">
						<el-descriptions-item v-for="item in otherDetails" :key="item.label" :label="item.label" :span="item.span ?? 1">
							{{ item.value }}
						</el-descriptions-item>
					</el-descriptions>
				</el-tab-pane>
			</el-tabs>
		</div>

		<div ref="printRef" class="print-template" aria-hidden="true">
			<div class="print-document">
				<header class="print-header">
					<div>
						<p>HỆ THỐNG GIAO THÔNG THÔNG MINH</p>
						<h1>CHI TIẾT PHÂN TÍCH GIAO THÔNG</h1>
					</div>
					<div class="print-time">In lúc: {{ formatDate(new Date()) }}</div>
				</header>
				<section class="print-hero">
					<div><span>Biển số</span><strong>{{ displayValue(detailData.licensePlate) }}</strong></div>
					<div><span>Loại phương tiện</span><strong>{{ vehicleTypeValue }}</strong></div>
					<div><span>Thời gian phát hiện</span><strong>{{ formatDate(detailData.detectTime) }}</strong></div>
				</section>
				<section class="print-section">
					<h2>Thông tin cơ bản</h2>
					<div class="print-grid">
						<div v-for="item in basicDetails" :key="item.label" class="print-field"><span>{{ item.label }}</span><strong>{{ item.value }}</strong></div>
					</div>
				</section>
				<section class="print-section">
					<h2>Hình ảnh phương tiện</h2>
					<div class="print-images">
						<div v-for="image in state.imageList" :key="image.key" class="print-image-card">
							<p>{{ image.title }}</p>
							<img v-if="image.url" :src="image.url" :alt="image.title" />
							<div v-else class="print-image-placeholder">Chưa có ảnh</div>
						</div>
					</div>
				</section>
				<section class="print-section">
					<h2>Thông tin khác</h2>
					<div class="print-grid">
						<div v-for="item in otherDetails" :key="item.label" class="print-field"><span>{{ item.label }}</span><strong>{{ item.value }}</strong></div>
					</div>
				</section>
			</div>
		</div>

		<template #footer>
			<el-button :icon="Printer" @click="handlePrint">In / lưu PDF</el-button>
			<el-button type="danger" @click="dialogVisible = false">{{ $t('lz.button.base.close') }}</el-button>
		</template>
	</el-dialog>
</template>

<script lang="ts" setup>
import { computed, nextTick, onMounted, onUnmounted, reactive, ref, watch } from 'vue';
import { useDateFormat } from '@vueuse/core';
import { Picture, Printer, ZoomIn } from '@element-plus/icons-vue';
import { VdsMediaApi } from '/@/api-services/vds';
import { vdsTimestamp } from '/@/utils/vdsTimestamp';
import { useBaseConfig } from '/@/stores/baseConfig';
import { BaseConfigTypeEnum } from '/@/types/enums';
import { DATE_TIME_FORMAT } from '/@/types/formats';
import { getAPI } from '/@/utils/axios-utils';

type DetailItem = {
	label: string;
	value: string;
	span?: number;
};

type AnalysisImage = {
	key: string;
	title: string;
	url?: string;
};

const props = withDefaults(
	defineProps<{
		isShowDialog?: boolean;
		defaultValue?: Record<string, any>;
	}>(),
	{
		isShowDialog: false,
		defaultValue: () => ({}),
	}
);

const dialogVisible = ref(false);
const activeTab = ref('basic');
const printRef = ref<HTMLElement>();
const detailData = computed<Record<string, any>>(() => props.defaultValue ?? {});
const baseConfig = useBaseConfig();
const getConfigDataByCode = baseConfig.getConfigDataByCode;
const state = reactive({
	directionList: [] as any[],
	vehicleTypeList: [] as any[],
	imageList: [
		{ key: 'overview', title: 'Ảnh toàn cảnh' },
		{ key: 'vehicle', title: 'Ảnh phương tiện' },
	] as AnalysisImage[],
	imageLoading: false,
});

const displayValue = (value: unknown) => (value === null || value === undefined || value === '' ? '-' : String(value));
const formatDate = (value?: Date | string | null) => (value ? useDateFormat(vdsTimestamp(value), DATE_TIME_FORMAT).value : '-');
const formatPercent = (value?: number | null) => (value == null ? '-' : `${(value * 100).toFixed(1)}%`);
const formatLength = (value?: number | null) => (value == null ? '-' : `${value} mm`);

const getConfigDisplay = (configList: any[], value?: string | null) => {
	if (value === null || value === undefined || value === '') return '-';
	return configList.find((item: any) => String(item.code) === String(value))?.name ?? String(value);
};

const directionValue = computed(() => detailData.value.directionName ?? getConfigDisplay(state.directionList, detailData.value.direction));
const vehicleTypeValue = computed(() => {
	if (detailData.value.vehicleTypeName) return detailData.value.vehicleTypeName;
	if (detailData.value.type) return getConfigDisplay(state.vehicleTypeList, detailData.value.type);
	return displayValue(detailData.value.vehicleType);
});
const equipmentValue = computed(() => displayValue(detailData.value.equipmentName ?? detailData.value.equipment ?? detailData.value.equipmentId));
const speedValue = computed(() => (detailData.value.speed == null ? '-' : `${detailData.value.speed} km/h`));
const imagePreviewUrls = computed(() => state.imageList.flatMap((image) => (image.url ? [image.url] : [])));
const getImagePreviewIndex = (imageUrl: string) => Math.max(imagePreviewUrls.value.indexOf(imageUrl), 0);

const basicDetails = computed<DetailItem[]>(() => [
	{ label: 'Biển số', value: displayValue(detailData.value.licensePlate) },
	{ label: 'Thời gian phát hiện', value: formatDate(detailData.value.detectTime) },
	{ label: 'Loại phương tiện', value: vehicleTypeValue.value },
	{ label: 'Hướng', value: directionValue.value },
	{ label: 'Thiết bị', value: equipmentValue.value },
	{ label: 'Làn đường', value: displayValue(detailData.value.lane) },
	{ label: 'Vị trí', value: displayValue(detailData.value.location) },
	{ label: 'Tốc độ', value: speedValue.value },
	{ label: 'Chiều dài', value: formatLength(detailData.value.length) },
	{ label: 'Chiều rộng', value: formatLength(detailData.value.width) },
	{ label: 'Chiều cao', value: formatLength(detailData.value.height) },
]);

const otherDetails = computed<DetailItem[]>(() => [
	{ label: 'Mã dữ liệu', value: displayValue(detailData.value.id ?? detailData.value.code) },
	{ label: 'Nguồn dữ liệu', value: displayValue(detailData.value.sourceId) },
	{ label: 'Địa chỉ IP', value: displayValue(detailData.value.ipAddress) },
	{ label: 'Khu vực', value: displayValue(detailData.value.zoneId) },
	{ label: 'Kiểu di chuyển', value: displayValue(detailData.value.moveType) },
	{ label: 'Độ tin cậy biển số', value: formatPercent(detailData.value.plateConfidence) },
]);

const loadConfigData = async () => {
	try {
		if (Object.keys(baseConfig.configList ?? {}).length === 0) await baseConfig.setConfigDataList();
		state.directionList = getConfigDataByCode(BaseConfigTypeEnum.TmsDirectionType) ?? [];
		state.vehicleTypeList = getConfigDataByCode(BaseConfigTypeEnum.TmsVehicleTypeConfigCode) ?? [];
	} catch (error) {
		console.error('Không thể tải cấu hình hướng và loại phương tiện.', error);
	}
};

const getAnalysisImage = async (key: string, title: string, kind: 'full' | 'vehicle', record: Record<string, any>): Promise<AnalysisImage> => {
    if (record.vendorEventId === null || record.vendorEventId === undefined || record.vendorEventId === '') return { key, title };
    const eventId = Number(record.vendorEventId);
    const detectTime = vdsTimestamp(record.detectTime);
    if (!Number.isFinite(eventId) || eventId <= 0 || !detectTime || Number.isNaN(new Date(detectTime).getTime())) return { key, title };
    try {
        // Keep the original fractional precision; a JavaScript Date truncates to milliseconds.
        const response = await getAPI(VdsMediaApi).apiVdsVdsmediaFileGet(
            eventId, detectTime as unknown as Date, kind, { responseType: 'blob' });
        const blob = response.data;
        const contentType = String((response.headers as any)?.['content-type'] ?? blob?.type ?? '').toLowerCase();
        // A business error may be returned as HTTP 200 JSON even with responseType=blob.
        if (!(blob instanceof Blob) || blob.size === 0 || contentType.includes('json') || contentType.includes('problem')) return { key, title };
        // Some file stores serve valid images as application/octet-stream. Check the file signature.
        const signature = new Uint8Array(await blob.slice(0, 12).arrayBuffer());
        const isImage = (signature[0] === 0xff && signature[1] === 0xd8 && signature[2] === 0xff)
            || (signature[0] === 0x89 && signature[1] === 0x50 && signature[2] === 0x4e && signature[3] === 0x47)
            || (signature[0] === 0x47 && signature[1] === 0x49 && signature[2] === 0x46)
            || (signature[0] === 0x52 && signature[1] === 0x49 && signature[2] === 0x46 && signature[8] === 0x57 && signature[9] === 0x45 && signature[10] === 0x42 && signature[11] === 0x50);
        if (!isImage) return { key, title };
        const url = await new Promise<string>((resolve, reject) => {
            const reader = new FileReader();
            reader.onload = () => resolve(String(reader.result));
            reader.onerror = () => reject(reader.error);
            reader.readAsDataURL(blob);
        });
        return { key, title, url };
    } catch {
        return { key, title };
    }
};

let imageRequestId = 0;
const clearImages = () => {
 state.imageList = [
  { key: 'overview', title: 'Ảnh toàn cảnh' },
  { key: 'vehicle', title: 'Ảnh phương tiện' },
 ];
};
const loadImageData = async () => {
 const requestId = ++imageRequestId;
 clearImages();
 state.imageLoading = true;
 // Parent props update on the next Vue tick after selecting a row.
 await nextTick();
 if (requestId !== imageRequestId || !dialogVisible.value) return;
 const record = { ...detailData.value };
 try {
  const images = await Promise.all([
   getAnalysisImage('overview', 'Ảnh toàn cảnh', 'full', record),
   getAnalysisImage('vehicle', 'Ảnh phương tiện', 'vehicle', record),
  ]);
  if (requestId === imageRequestId && dialogVisible.value) state.imageList = images;
 } finally {
  if (requestId === imageRequestId) state.imageLoading = false;
 }
};
watch(dialogVisible, visible => {
 if (!visible) {
  ++imageRequestId;
  clearImages();
  state.imageLoading = false;
 }
}, { flush: 'sync' });
onUnmounted(() => { ++imageRequestId; });

const handlePrint = () => {
	const content = printRef.value?.innerHTML;
	if (!content) return;

	const printWindow = window.open('', '_blank', 'width=1100,height=900');
	if (!printWindow) return;

	printWindow.document.write(`<!doctype html>
		<html><head><meta charset="utf-8"><title>Chi tiết phân tích giao thông</title>
		<style>
			@page { size: A4; margin: 14mm; }
			* { box-sizing: border-box; }
			body { color: #172033; font-family: Arial, Helvetica, sans-serif; font-size: 12px; line-height: 1.45; }
			.print-document { max-width: 100%; }
			.print-header { display:flex; justify-content:space-between; align-items:flex-end; border-bottom:2px solid #2563eb; padding-bottom:10px; margin-bottom:16px; }
			.print-header p { margin:0 0 3px; color:#64748b; font-size:10px; font-weight:700; }
			.print-header h1 { margin:0; color:#172554; font-size:20px; }
			.print-time { color:#64748b; font-size:10px; }
			.print-hero { display:grid; grid-template-columns:repeat(3, 1fr); gap:10px; margin-bottom:16px; }
			.print-hero div, .print-field { border:1px solid #dbe3ef; border-radius:5px; padding:8px 10px; }
			.print-hero span, .print-field span { display:block; color:#64748b; font-size:10px; margin-bottom:3px; }
			.print-hero strong, .print-field strong { color:#0f172a; word-break:break-word; }
			.print-section { margin-top:16px; break-inside:avoid; }
			.print-section h2 { margin:0 0 8px; padding-left:8px; border-left:3px solid #2563eb; color:#1e3a8a; font-size:14px; }
			.print-grid { display:grid; grid-template-columns:repeat(2, 1fr); gap:6px; }
			.print-images { display:grid; grid-template-columns:repeat(2, 1fr); gap:12px; }
			.print-image-card { border:1px solid #dbe3ef; border-radius:5px; overflow:hidden; min-height:170px; }
			.print-image-card p { margin:0; padding:7px 10px; background:#f8fafc; font-weight:700; }
			.print-image-card img { display:block; width:100%; height:165px; object-fit:contain; }
			.print-image-placeholder { display:flex; justify-content:center; align-items:center; height:165px; color:#94a3b8; font-style:italic; }
		</style></head><body>${content}</body></html>`);
	printWindow.document.close();
	printWindow.focus();
	window.setTimeout(() => {
		printWindow.print();
		printWindow.close();
	}, 200);
};

const showModal = async (isOpenModal: boolean) => {
	dialogVisible.value = isOpenModal;
	if (isOpenModal) {
		activeTab.value = 'basic';
		await Promise.all([loadConfigData(), loadImageData()]);
	}
};

onMounted(loadConfigData);

defineExpose({ showModal });
</script>

<style scoped>
.dialog-heading {
	display: flex;
	align-items: center;
	gap: 7px;
	color: #fff;
}

.analysis-detail-content {
	padding: 0 4px;
}

.analysis-hero {
	display: flex;
	justify-content: space-between;
	gap: 24px;
	margin-bottom: 18px;
	padding: 20px 22px;
	border-radius: 10px;
	color: #eff6ff;
	background: linear-gradient(120deg, #0f3c78, #2563b4);
}

.analysis-hero__eyebrow {
	margin: 0 0 5px;
	font-size: 11px;
	font-weight: 700;
	letter-spacing: 0.08em;
	color: #bfdbfe;
}

.analysis-hero h2 {
	margin: 0;
	font-size: 22px;
}

.analysis-hero p:last-child {
	margin: 7px 0 0;
	color: #dbeafe;
}

.analysis-hero__plate {
	display: flex;
	flex-direction: column;
	justify-content: center;
	min-width: 160px;
	padding: 10px 15px;
	border: 1px solid rgba(255, 255, 255, 0.35);
	border-radius: 8px;
	background: rgba(15, 23, 42, 0.2);
}

.analysis-hero__plate span {
	font-size: 10px;
	letter-spacing: 0.08em;
	color: #bfdbfe;
}

.analysis-hero__plate strong {
	margin-top: 3px;
	font-size: 21px;
	letter-spacing: 0.05em;
}

.analysis-tabs :deep(.el-tabs__header) {
	margin-bottom: 14px;
}

.analysis-descriptions :deep(.el-descriptions__label) {
	width: 165px;
	font-weight: 600;
	color: #475569;
	background: #f8fafc;
}

.media-section {
	margin-top: 20px;
}

.section-title {
	display: flex;
	align-items: center;
	gap: 7px;
	margin-bottom: 10px;
	font-size: 15px;
	font-weight: 700;
	color: #1e3a8a;
}

.media-gallery {
	display: grid;
	grid-template-columns: repeat(2, minmax(0, 1fr));
	gap: 14px;
}

.media-card {
	overflow: hidden;
	border: 1px solid #dbe3ef;
	border-radius: 8px;
	background: #fff;
}

.media-card__title {
	padding: 9px 12px;
	font-weight: 700;
	color: #334155;
	background: #f8fafc;
}

.media-card__image {
	position: relative;
}

.analysis-image,
.image-placeholder {
	display: flex;
	width: 100%;
	height: 230px;
}

.image-zoom-hint {
	position: absolute;
	bottom: 10px;
	right: 10px;
	display: flex;
	align-items: center;
	gap: 4px;
	padding: 5px 8px;
	border-radius: 4px;
	color: #fff;
	font-size: 12px;
	background: rgba(15, 23, 42, 0.72);
	pointer-events: none;
}

.image-placeholder {
	flex-direction: column;
	align-items: center;
	justify-content: center;
	gap: 8px;
	color: #94a3b8;
	background: #f8fafc;
}

.print-template {
	position: fixed;
	left: -100000px;
	top: 0;
	width: 900px;
	pointer-events: none;
}

@media (max-width: 768px) {
	.analysis-hero {
		flex-direction: column;
		gap: 14px;
	}

	.media-gallery {
		grid-template-columns: 1fr;
	}
}
</style>
