<template>
	<el-dialog v-model="dialogVisible" width="min(1100px, 96vw)" top="5vh" class="traffic-event-detail-dialog" destroy-on-close>
		<template #header>
			<div class="dialog-heading">
				<el-icon size="18"><ele-View /></el-icon>
				<span>{{ $t('lz.label.tmsTrafficEventHistory.detailTitle') }}</span>
			</div>
		</template>

		<div class="event-detail-content">
			<section class="event-hero">
				<div>
					<p class="event-hero__eyebrow">{{ $t('lz.label.tmsTrafficEventHistory.heading') }}</p>
					<h2>{{ eventTypeDisplay }}</h2>
					<p>{{ formatDate(eventData.detectTime) }} · {{ equipmentName }}</p>
				</div>
				<div class="event-hero__plate">
					<span>{{ $t('lz.entity.tms.licensePlate') }}</span>
					<strong>{{ displayValue(eventData.licensePlate) }}</strong>
				</div>
			</section>

			<el-tabs v-model="activeTab" class="event-tabs">
				<el-tab-pane :label="$t('lz.label.base.basicInfo')" name="basic">
					<el-descriptions :column="2" border class="event-descriptions">
						<el-descriptions-item v-for="item in basicDetails" :key="item.label" :label="item.label">
							{{ item.value }}
						</el-descriptions-item>
					</el-descriptions>

					<section class="media-section">
						<div class="section-title">
							<el-icon><Picture /></el-icon>
							<span>{{ $t('lz.label.base.relatedImages') }}</span>
						</div>
						<div class="media-gallery">
							<article v-for="image in imageItems" :key="image.key" class="media-card">
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
										class="event-image"
									/>
									<div class="image-zoom-hint"><el-icon><ZoomIn /></el-icon> {{ $t('lz.label.base.enlargeImageHint') }}</div>
								</div>
								<div v-else class="image-placeholder">
									<el-icon :size="32"><Picture /></el-icon>
									<span>{{ state.imageLoading ? $t('lz.label.base.loadingImage') : $t('lz.label.base.noImage') }}</span>
								</div>
							</article>
						</div>
					</section>
				</el-tab-pane>

				<el-tab-pane :label="$t('lz.label.base.otherInfo')" name="other">
					<el-descriptions :column="2" border class="event-descriptions">
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
						<p>{{ $t('lz.label.tms.systemName') }}</p>
						<h1>{{ $t('lz.label.tmsTrafficEventHistory.detailTitle') }}</h1>
					</div>
					<div class="print-time">{{ $t('lz.label.base.printedAt') }} {{ formatDate(new Date()) }}</div>
				</header>
				<section class="print-hero">
					<div><span>{{ $t('lz.entity.tms.eventType') }}</span><strong>{{ eventTypeDisplay }}</strong></div>
					<div><span>{{ $t('lz.entity.tms.licensePlate') }}</span><strong>{{ displayValue(eventData.licensePlate) }}</strong></div>
					<div><span>{{ $t('lz.entity.tms.detectTime') }}</span><strong>{{ formatDate(eventData.detectTime) }}</strong></div>
				</section>
				<section class="print-section">
					<h2>{{ $t('lz.label.base.basicInfo') }}</h2>
					<div class="print-grid">
						<div v-for="item in basicDetails" :key="item.label" class="print-field"><span>{{ item.label }}</span><strong>{{ item.value }}</strong></div>
					</div>
				</section>
				<section class="print-section">
					<h2>{{ $t('lz.label.base.relatedImages') }}</h2>
					<div class="print-images">
						<div v-for="image in imageItems" :key="image.key" class="print-image-card">
							<p>{{ image.title }}</p>
							<img v-if="image.url" :src="image.url" :alt="image.title" />
							<div v-else class="print-image-placeholder">{{ $t('lz.label.base.noImage') }}</div>
						</div>
					</div>
				</section>
				<section class="print-section">
					<h2>{{ $t('lz.label.base.otherInfo') }}</h2>
					<div class="print-grid">
						<div v-for="item in otherDetails" :key="item.label" class="print-field"><span>{{ item.label }}</span><strong>{{ item.value }}</strong></div>
					</div>
				</section>
			</div>
		</div>

		<template #footer>
			<el-button :icon="Printer" @click="handlePrint">{{ $t('lz.button.base.printPdf') }}</el-button>
			<el-button type="danger" @click="dialogVisible = false">{{ $t('lz.button.base.close') }}</el-button>
		</template>
	</el-dialog>
</template>

<script lang="ts" setup>
import { useI18n } from 'vue-i18n';


import { computed, nextTick, onMounted, onUnmounted, reactive, ref, watch } from 'vue';
import { useDateFormat } from '@vueuse/core';
import { Picture, Printer, ZoomIn } from '@element-plus/icons-vue';
import { TmsZoneApi } from '/@/api-services/tms';
import { VdsEventApi, VdsMediaApi } from '/@/api-services/vds';
import type { PageTrafficDataOutput } from '/@/api-services/vds';
import { vdsTimestamp } from '/@/utils/vdsTimestamp';
import { useBaseConfig } from '/@/stores/baseConfig';
import { BaseConfigTypeEnum } from '/@/types/enums';
import { DATE_TIME_FORMAT } from '/@/types/formats';
import { getAPI } from '/@/utils/axios-utils';

const { t: $t } = useI18n();

/** Bản ghi sự kiện VDS kèm mấy bí danh mà trang danh sách đắp thêm khi map (xem index.vue). */
type EventHistoryRow = PageTrafficDataOutput & {
	id?: string;
	type?: string | null;
	length?: number | null;
	ipAddress?: string | null;
};

type EventImage = {
	key: string;
	title: string;
	url?: string;
};

type DetailItem = {
	label: string;
	value: string;
	span?: number;
};

const props = withDefaults(
	defineProps<{
		defaultValue?: EventHistoryRow;
		eventTypeName?: string;
		equipmentName?: string;
		images?: EventImage[];
	}>(),
	{
		defaultValue: () => ({}),
		eventTypeName: '-',
		equipmentName: '-',
		images: () => [],
	}
);

const dialogVisible = ref(false);
const activeTab = ref('basic');
const printRef = ref<HTMLElement>();
const eventData = computed(() => props.defaultValue as Record<string, any>);
const baseConfig = useBaseConfig();
const getConfigDataByCode = baseConfig.getConfigDataByCode;
const state = reactive({
	directionList: [] as any[],
	vehicleTypeList: [] as any[],
	zoneList: [] as any[],
	imageList: [] as EventImage[],
	imageLoading: false,
	incidentCodes: [] as string[],
});

const displayValue = (value: unknown) => (value === null || value === undefined || value === '' ? '-' : String(value));
const formatDate = (value?: Date | string | null) => (value ? useDateFormat(vdsTimestamp(value), DATE_TIME_FORMAT).value : '-');

const locationValue = computed(() => {
	if (eventData.value.location) return displayValue(eventData.value.location);
	if (eventData.value.kmNumber == null && eventData.value.metNumber == null) return '-';
	return `Km ${eventData.value.kmNumber ?? 0}+${String(eventData.value.metNumber ?? 0).padStart(3, '0')}`;
});

const eventTypeDisplay = computed(() => props.eventTypeName || displayValue(eventData.value.eventType));
const speedValue = computed(() => (eventData.value.speed == null ? '-' : `${eventData.value.speed} km/h`));
const getConfigDisplay = (configList: any[], value?: string | null) => {
	if (value === null || value ===undefined || value === '') return '-';
	return configList.find((item: any) => String(item.code) === String(value))?.name ?? String(value);
};
const directionValue = computed(() => getConfigDisplay(state.directionList, eventData.value.direction));
const vehicleTypeValue = computed(() => getConfigDisplay(state.vehicleTypeList, eventData.value.vehicleType));
const zoneValue = computed(() => {
	const zoneIds = String(eventData.value.zoneId ?? '')
		.split(',')
		.map((zoneId) => zoneId.trim())
		.filter(Boolean);
	if (!zoneIds.length) return '-';

	return zoneIds
		.map((zoneId) => state.zoneList.find((zone: any) => String(zone.id) === zoneId)?.name ?? zoneId)
		.join(', ');
});
const imageItems = computed<EventImage[]>(() =>
	props.images.length
		? props.images
		: state.imageList.length
			? state.imageList
		: [
				{ key: 'full', title: $t('lz.label.tms.fullViewImage') },
				{ key: 'plate', title: $t('lz.label.tms.licensePlateImage') },
			]
);
const imagePreviewUrls = computed(() => imageItems.value.flatMap((image) => (image.url ? [image.url] : [])));
const getImagePreviewIndex = (imageUrl: string) => Math.max(imagePreviewUrls.value.indexOf(imageUrl), 0);

/** Cỡ xe VDS suy từ chiều dài. */
const vehicleSizeValue = computed(() => {
	const size = String(eventData.value.type ?? eventData.value.vehicleSize ?? '').toLowerCase();
	if (!size) return '-';
	return ({ short: $t('lz.label.tms.vehicleSizeShort'), medium: $t('lz.label.tms.vehicleSizeMedium'), long: $t('lz.label.tms.vehicleSizeLong'), other: $t('lz.label.base.other') } as Record<string, string>)[size] ?? size;
});

/** Chiều dài xe VDS lưu bằng MILIMÉT; quy về mét cho người đọc. */
const vehicleLengthValue = computed(() => {
	const millimeter = eventData.value.length ?? eventData.value.lengthMm;
	return millimeter == null ? '-' : `${(Number(millimeter) / 1000).toFixed(2)} m`;
});

const basicDetails = computed<DetailItem[]>(() => [
	{ label: $t('lz.entity.tms.eventType'), value: eventTypeDisplay.value },
	{ label: $t('lz.entity.tms.detectTime'), value: formatDate(eventData.value.detectTime) },
	{ label: $t('lz.entity.tms.equipment'), value: props.equipmentName || displayValue(eventData.value.equipmentId) },
	{ label: $t('lz.label.base.position'), value: locationValue.value },
	{ label: $t('lz.entity.tms.direction'), value: directionValue.value },
	{ label: $t('lz.entity.tms.lane'), value: displayValue(eventData.value.lane) },
	{ label: $t('lz.entity.tms.licensePlate'), value: displayValue(eventData.value.licensePlate) },
	{ label: $t('lz.entity.tms.vehicleType'), value: vehicleTypeValue.value },
	{ label: $t('lz.entity.tms.speed'), value: speedValue.value },
	{ label: $t('lz.entity.tms.vehicleColor'), value: displayValue(eventData.value.vehicleColor) },
]);

// Chỉ giữ những trường VDS THỰC SỰ có. Bản trước còn plateColor/plateType/plateConfidence/vehicleBrand/
// speedThreshold/alarmType/conditionCode/incidentBlockReason/occurredUtc/remark — đó là cột của bảng sự
// kiện TMS cũ, bảng thô VDS trên QuestDB không lưu, nên chúng chỉ hiện một cột toàn dấu gạch.
const otherDetails = computed<DetailItem[]>(() => [
	{ label: $t('lz.entity.tms.vdsEventId'), value: displayValue(eventData.value.vendorEventId) },
	{ label: $t('lz.entity.base.ipAddress'), value: displayValue(eventData.value.ipAddress ?? eventData.value.cameraIp) },
	{ label: $t('lz.label.base.location'), value: zoneValue.value },
	{ label: $t('lz.entity.tms.vehicleSize'), value: vehicleSizeValue.value },
	{ label: $t('lz.entity.tms.vehicleLength'), value: vehicleLengthValue.value },
	{ label: $t('lz.label.tmsTrafficEventHistory.relatedIncident'), value: state.incidentCodes.length ? state.incidentCodes.join(', ') : '-', span: 2 },
]);

const loadConfigData = async () => {
	if (Object.keys(baseConfig.configList ?? {}).length === 0) await baseConfig.setConfigDataList();
	state.directionList = getConfigDataByCode(BaseConfigTypeEnum.TmsDirectionType) ?? [];
	state.vehicleTypeList = getConfigDataByCode(BaseConfigTypeEnum.TmsVehicleTypeConfigCode) ?? [];
};

const loadZoneData = async () => {
	const zoneIdList = String(eventData.value.zoneId ?? '').trim();
	state.zoneList = [];
	if (!zoneIdList) return;

	try {
		const response = await getAPI(TmsZoneApi).apiTmsTmszoneListzonebyidsGet(zoneIdList);
		state.zoneList = response.data.result ?? [];
	} catch (error) {
		console.error('Không thể tải thông tin khu vực của sự kiện.', error);
	}
};

/**
 * Khoá kép của một bản tin VDS: (vendorEventId, detectTime). QuestDB không có cột id nên cả
 * <c>/api/vds/vdsevent/detail</c> lẫn <c>/api/vds/vdsmedia/file</c> đều nhận đúng cặp này.
 */
const mediaKey = () => {
	if (eventData.value.vendorEventId === null || eventData.value.vendorEventId === undefined || eventData.value.vendorEventId === '') return undefined;
	const eventId = Number(eventData.value.vendorEventId);
	const detectTime = eventData.value.detectTime;
	if (!Number.isFinite(eventId) || eventId <= 0 || !detectTime) return undefined;

	// KHÔNG dùng new Date(detectTime). Hai lý do, cả hai đều làm máy chủ không tìm thấy bản ghi vì
	// VdsRecordReader so KHỚP TUYỆT ĐỐI `DetectTime == utc`:
	//   1. Múi giờ: JSON trả về mốc KHÔNG có hậu tố Z ("2026-09-10T07:33:35.123456"). Theo chuẩn
	//      ECMAScript, dạng ngày-giờ thiếu offset được hiểu là GIỜ MÁY, nên new Date(...) cộng thêm 7
	//      tiếng — tra một mốc không tồn tại.
	//   2. Độ chính xác: client sinh tự động gọi .toISOString() cho tham số kiểu Date, chỉ giữ 3 chữ số
	//      MILI giây, còn QuestDB lưu tới MICRO giây. Cắt mất phần đuôi là mất luôn dòng cần tìm.
	// vdsTimestamp giữ nguyên chuỗi gốc và chỉ thêm Z. Truyền CHUỖI để client dùng nguyên văn (nó chỉ
	// gọi toISOString khi tham số là Date) — đúng cách trafficAnalysisViewDetailModal đang làm và chạy được.
	return { eventId, detectTime: vdsTimestamp(detectTime as unknown as string) as unknown as Date };
};

// Chặn kết quả CŨ ghi đè kết quả mới khi người dùng bấm xem liên tiếp nhiều dòng: lượt nào không còn là
// lượt mới nhất thì bỏ kết quả.
let imageRequestId = 0;

const getEventImage = async (kind: string, title: string): Promise<EventImage> => {
	const key = mediaKey();
	if (!key) return { key: kind, title };

	try {
		// responseType blob: máy chủ trả về FILE (PhysicalFileResult), không phải JSON base64 như bảng sự
		// kiện TMS cũ. Ảnh phải đi qua axios chứ không gắn thẳng vào <img src> vì endpoint đòi Bearer token.
		const response = await getAPI(VdsMediaApi).apiVdsVdsmediaFileGet(key.eventId, key.detectTime, kind, {
			responseType: 'blob',
		});

		// Furion có thể trả HTTP 200 + JSON lỗi dưới dạng Blob. Kiểm chữ ký ảnh để
		// chấp nhận cả file thật có MIME application/octet-stream.
		const contentType = String((response.headers as any)?.['content-type'] ?? (response.data as Blob)?.type ?? '').toLowerCase();
		const blob = response.data as unknown as Blob;
		if (!(blob instanceof Blob) || blob.size === 0 || contentType.includes('json') || contentType.includes('problem')) return { key: kind, title };
		const signature = new Uint8Array(await blob.slice(0, 12).arrayBuffer());
		const isImage = (signature[0] === 0xff && signature[1] === 0xd8 && signature[2] === 0xff)
			|| (signature[0] === 0x89 && signature[1] === 0x50 && signature[2] === 0x4e && signature[3] === 0x47)
			|| (signature[0] === 0x47 && signature[1] === 0x49 && signature[2] === 0x46)
			|| (signature[0] === 0x52 && signature[1] === 0x49 && signature[2] === 0x46 && signature[8] === 0x57 && signature[9] === 0x45 && signature[10] === 0x42 && signature[11] === 0x50);
		if (!isImage) return { key: kind, title };

		// Đổi sang data URL thay vì objectURL: không phải nhớ revoke, và el-image dùng lại được cho cả
		// khung xem lớn lẫn bản in (window.open không đọc được blob: của tab gốc).
		const url = await new Promise<string>((resolve, reject) => {
			const reader = new FileReader();
			reader.onload = () => resolve(String(reader.result));
			reader.onerror = () => reject(reader.error);
			reader.readAsDataURL(blob);
		});
		return { key: kind, title, url };
	} catch {
		// 404 là trạng thái HỢP LỆ: kho media chỉ có ảnh của những loại/kind mà thiết bị thực sự gửi kèm.
		return { key: kind, title };
	}
};

const loadImageData = async () => {
	if (props.images.length) return;

	const requestId = ++imageRequestId;
	state.imageList = [];
	state.imageLoading = true;

	// Props của cha chỉ cập nhật ở tick sau khi bấm xem một dòng; chờ một tick để đọc đúng dòng vừa chọn.
	await nextTick();
	if (requestId !== imageRequestId || !dialogVisible.value) return;

	try {
		const images = await Promise.all([
			getEventImage('full', $t('lz.label.tms.fullViewImage')),
			getEventImage('vehicle', $t('lz.label.tms.vehicleImage')),
			getEventImage('plate', $t('lz.label.tms.licensePlateImage')),
		]);
		if (requestId === imageRequestId && dialogVisible.value) state.imageList = images;
	} finally {
		if (requestId === imageRequestId) state.imageLoading = false;
	}
};

/** Sự cố đã được tạo từ bản tin này — VDS trả kèm trong endpoint chi tiết. */
const loadIncidentData = async () => {
	state.incidentCodes = [];
	const key = mediaKey();
	if (!key) return;

	try {
		const response = await getAPI(VdsEventApi).apiVdsVdseventDetailGet(key.eventId, key.detectTime);
		state.incidentCodes = (response.data.result?.incidents ?? [])
			.map((incident: any) => String(incident?.code ?? incident?.id ?? '').trim())
			.filter(Boolean);
	} catch (error) {
		console.error('Không thể tải sự cố liên quan của bản tin VDS.', error);
	}
};

const handlePrint = () => {
	const content = printRef.value?.innerHTML;
	if (!content) return;

	const printWindow = window.open('', '_blank', 'width=1100,height=900');
	if (!printWindow) return;

	printWindow.document.write(`<!doctype html>
		<html><head><meta charset="utf-8"><title>${$t('lz.label.tmsTrafficEventHistory.detailTitle')}</title>
		<style>
			@page { size: A4; margin: 14mm; }
			* { box-sizing: border-box; }
			body { color: #172033; font-family: Arial, Helvetica, sans-serif; font-size: 12px; line-height: 1.45; }
			.print-document { max-width: 100%; }
			.print-header { display:flex; justify-content:space-between; align-items:flex-end; border-bottom:2px solid #2563eb; padding-bottom:10px; margin-bottom:16px; }
			.print-header p { margin:0 0 3px; color:#64748b; font-size:10px; font-weight:700; text-transform:uppercase; }
			.print-header h1 { margin:0; color:#172554; font-size:20px; text-transform:uppercase; }
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
		await Promise.all([loadConfigData(), loadZoneData(), loadImageData(), loadIncidentData()]);
	}
};

// Đóng hộp thoại thì bỏ kết quả đang chờ và xoá ảnh: người dùng đóng bằng dấu X hay bấm ra ngoài đều
// không đi qua showModal, nên không dọn ở đây thì lượt xem dòng khác sẽ thấy ảnh của dòng cũ.
watch(
	dialogVisible,
	(visible) => {
		if (visible) return;
		++imageRequestId;
		state.imageList = [];
		state.imageLoading = false;
		state.incidentCodes = [];
	},
	{ flush: 'sync' }
);

onMounted(loadConfigData);
onUnmounted(() => {
	++imageRequestId;
});

defineExpose({ showModal });
</script>

<style scoped>
.dialog-heading {
	display: flex;
	align-items: center;
	gap: 7px;
	color: #fff;
}

.event-detail-content {
	padding: 0 4px;
}

.event-hero {
	display: flex;
	justify-content: space-between;
	gap: 24px;
	margin-bottom: 18px;
	padding: 20px 22px;
	border-radius: 10px;
	color: #eff6ff;
	background: linear-gradient(120deg, #0f3c78, #2563b4);
}

.event-hero__eyebrow {
	text-transform: uppercase;
	margin: 0 0 5px;
	font-size: 11px;
	font-weight: 700;
	letter-spacing: 0.08em;
	color: #bfdbfe;
}

.event-hero h2 {
	margin: 0;
	font-size: 22px;
}

.event-hero p:last-child {
	margin: 7px 0 0;
	color: #dbeafe;
}

.event-hero__plate {
	display: flex;
	flex-direction: column;
	justify-content: center;
	min-width: 160px;
	padding: 10px 15px;
	border: 1px solid rgba(255, 255, 255, 0.35);
	border-radius: 8px;
	background: rgba(15, 23, 42, 0.2);
}

.event-hero__plate span {
	text-transform: uppercase;
	font-size: 10px;
	letter-spacing: 0.08em;
	color: #bfdbfe;
}

.event-hero__plate strong {
	margin-top: 3px;
	font-size: 21px;
	letter-spacing: 0.05em;
}

.event-tabs :deep(.el-tabs__header) {
	margin-bottom: 14px;
}

.event-descriptions :deep(.el-descriptions__label) {
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

.event-image,
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
	.event-hero {
		flex-direction: column;
		gap: 14px;
	}

	.media-gallery {
		grid-template-columns: 1fr;
	}
}
</style>
