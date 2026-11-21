<template>
    <div class="rpt-incident-container">
        <el-card class="rpt-incident-card">
            <el-tabs v-model="activeTab" class="rpt-incident-tabs">
                <el-tab-pane name="summary" :label="$t('lz.label.base.summary')">
                    <el-card class="full-table" shadow="hover" style="margin-top: 5px">
                    <ReportViewer
                            ref="reportViewerRef"
                            :pdf-source="state.pdf"
                            :loading="state.isProcessing"
                            :format-options="getConfigDataByCode(BaseConfigTypeEnum.RptExportFormat)"
                            :default-format="state.queryParams.format"
                            :export-file-name="reportFileName"
                            export-auth="tmsRptIncidentDetail:export"
                            print-auth="tmsRptIncidentDetail:print"
                            @export="handleExport"
                        />
                    </el-card>                    
                </el-tab-pane>
                <el-tab-pane name="details" :label="$t('lz.label.tmsIncident.detailTab')">
                    <div class="incident-details-container">
                        <el-descriptions v-if="state.incidentInfo" :column="2" border>
                            <el-descriptions-item v-for="item in incidentDetails" :key="item.label" :label="item.label">{{ item.value ?? '—' }}</el-descriptions-item>
                        </el-descriptions>
                        <IncidentVehicleInfo :metadata="state.incidentInfo?.metadata" />
                        <el-descriptions v-if="state.incidentInfo" :title="$t('lz.label.tmsIncident.workflow')" :column="2" border>
                            <el-descriptions-item v-for="stage in incidentImageStages" :key="stage.pictureType" :label="stage.label">
                                <div>{{ stage.date ? useDateFormat(stage.date, DateTimeFormatEnum.DateTimeDefaultRevert) : '—' }}</div>
                                <div v-if="stage.user">{{ stage.user }}</div>
                            </el-descriptions-item>
                        </el-descriptions>
                    </div>
                </el-tab-pane>
                <el-tab-pane name="images" :label="$t('lz.label.base.image')">
                    <div v-loading="state.imageLoading" class="incident-images-container">
                        <template v-if="imageCameras.length">
                            <el-card v-for="camera in imageCameras" :key="camera.id" class="margin-bottom--20px" shadow="never">
                                <h2 class="padding-10px text-uppercase text-color--green">{{ commonVarsAndFuncs.formatCameraName(camera.name, camera.kmNumber, camera.metNumber) }}</h2>
                                <div class="incident-image-grid">
                                    <div v-for="stage in incidentImageStages" :key="stage.pictureType" class="text-center incident-image-stage">
                                        <h3 class="text-uppercase bg-color--light-gray padding-10px">{{ stage.label }}</h3>
                                        <el-image
                                            v-if="getIncidentImageUrl(camera.id, stage.pictureType)"
                                            class="incident-image"
                                            :src="getIncidentImageUrl(camera.id, stage.pictureType)"
                                            :zoom-rate="1.2"
                                            :max-scale="7"
                                            :min-scale="0.2"
                                            :preview-src-list="getCameraPreviewImages(camera.id)"
                                            :initial-index="getCameraPreviewIndex(camera.id, stage.pictureType)"
                                            preview-teleported
                                            hide-on-click-modal
                                            fit="cover"
                                        >
                                            <template #error>
                                                <div class="incident-image-empty"><el-empty :image-size="70" /></div>
                                            </template>
                                        </el-image>
                                        <div v-else class="incident-image-empty"><el-empty :image-size="70" :description="imageStatus(imageInfo(camera.id, stage.pictureType))" /></div>
                                        <div v-if="vdsSourceText(camera.id, stage.pictureType)" class="padding-10px">
                                            <el-text type="info"><i class="fa fa-video-camera"></i> {{ vdsSourceText(camera.id, stage.pictureType) }}</el-text>
                                        </div>
                                        <div class="incident-image-meta display-flex--vertical flex-gap--10px flex-justify--center bg-color--light-gray">
                                            <h3 v-if="stage.date">
                                                <el-text type="primary"><i class="fa fa-calendar"></i></el-text>
                                                {{ useDateFormat(stage.date, DateTimeFormatEnum.DateTimeDefaultRevert) }}
                                            </h3>
                                            <h3 v-if="stage.user">
                                                <el-text type="primary"><i class="fa fa-user-circle-o"></i></el-text>
                                                {{ stage.user }}
                                            </h3>
                                        </div>
                                    </div>
                                </div>
                            </el-card>
                        </template>
                        <el-card v-else shadow="never">
                            <el-empty :image-size="100" />
                        </el-card>
                    </div>
                </el-tab-pane>
            </el-tabs>
        </el-card>
    </div>
</template>
  
<script lang="ts" setup>
import IncidentVehicleInfo from '/@/views/tms/incident/component/incidentVehicleInfo.vue';
import { onBeforeUnmount, watch, reactive, ref, computed, defineAsyncComponent } from 'vue';
import { useRoute } from 'vue-router';
import { ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { getAPI } from '/@/utils/axios-utils';
import { useDateFormat, useNow } from '@vueuse/core';
import { saveAs } from 'file-saver';
import { TmsIncidentReportApi } from '/@/api-services/tmsReport';
import { TmsEquipmentApi, TmsZoneApi, TmsIncidentApi, type TmsIncidentImageOutput } from '/@/api-services/tms';
import { useCommonVarsAndFuncs } from '/@/stores/its/commonVarsAndFuncs';
import { DateTimeFormatEnum } from '/@/types/enums/baseEnum';
import { useBaseConfig } from '/@/stores/baseConfig';
import { BaseConfigTypeEnum } from '/@/types/enums';

const commonVarsAndFuncs = useCommonVarsAndFuncs();
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
// Report viewer ref
const ReportViewer = defineAsyncComponent(() => import('/@/components/advanced/report-viewer.vue'));
const reportViewerRef = ref<InstanceType<typeof ReportViewer>>();
const route = useRoute();
const { t: $t } = useI18n();
let incidentRequestId = 0;
const activeTab = ref('summary');
let registrationPoll: ReturnType<typeof setTimeout> | undefined;
const stopRegistrationPoll = () => { if (registrationPoll) clearTimeout(registrationPoll); registrationPoll = undefined; };
let stagePoll: ReturnType<typeof setTimeout> | undefined;
let imageLoadId = 0;
let stagePollDeadline = 0;
const stagePollTimeout = 120000;
const stopStagePoll = () => { if (stagePoll) clearTimeout(stagePoll); stagePoll = undefined; };
let pdfUrl = '';
const replacePdf = (url = '') => {
    if (pdfUrl) URL.revokeObjectURL(pdfUrl);
    pdfUrl = url;
    state.pdf = url || null;
};


const state = reactive({
    isProcessing: false,
    pdf: null as any,
    reportDataId: null,
    exportFormat: null,
    queryParams: {
		incidentId: '' as any,
		reportDataId: null as any,
		format: 'pdf' as any,
	} , 
    incidentInfo: null as any,
    cameraList: [] as any[],
    incidentImageUrls: {} as Record<string, string>,
    imageLoading: false,
    vdsRegistration: null as IncidentImage | null,
    images: [] as IncidentImage[],
    zones: [] as any[],
});

// Additional response metadata belongs to the image API, not the generated incident entity.
type IncidentImage = TmsIncidentImageOutput;

const pictureTypes = [1, 2, 3, 4] as const;
type IncidentPictureType = (typeof pictureTypes)[number];
let imageResults: Partial<Record<IncidentPictureType, IncidentImage[]>> = {};

const incidentImageStages = computed(() => [
    {
        pictureType: 1 as IncidentPictureType,
        label: $t('lz.label.tmsIncident.registrationTimeTitle'),
        date: state.incidentInfo?.createdDate ?? state.incidentInfo?.startDate,
        user: state.incidentInfo?.createdBy,
    },
    {
        pictureType: 2 as IncidentPictureType,
        label: $t('lz.label.tmsIncident.verifiedTimeTitle'),
        date: state.incidentInfo?.validatedDate,
        user: state.incidentInfo?.validatedBy,
    },
    {
        pictureType: 3 as IncidentPictureType,
        label: $t('lz.label.tmsIncident.inProgressTimeTitle'),
        date: state.incidentInfo?.approvedDate,
        user: state.incidentInfo?.approvedBy,
    },
    {
        pictureType: 4 as IncidentPictureType,
        label: $t('lz.label.tmsIncident.finishedTimeTitle'),
        date: state.incidentInfo?.finishedDate,
        user: state.incidentInfo?.finishedBy,
    },
]);

const configName = (group: string, code: unknown) => getConfigDataByCode(group).find((item: any) => String(item.code) === String(code))?.name ?? code ?? '—';
const incidentDetails = computed(() => {
    const i = state.incidentInfo ?? {};
    return [
        { label: $t('lz.entity.base.code'), value: i.code },
        { label: $t('lz.entity.base.name'), value: i.name },
        { label: $t('lz.entity.tmsIncident.eventTypeId'), value: i.eventTypeName || i.eventTypeCode },
        { label: $t('lz.entity.base.status'), value: configName(BaseConfigTypeEnum.TmsIncidentStatus, i.state) },
        { label: $t('lz.entity.tmsIncident.source'), value: configName(BaseConfigTypeEnum.TmsSourceDetectIncident, i.source) },
        { label: $t('lz.entity.tmsIncident.location'), value: configName(BaseConfigTypeEnum.TmsZoneLocation, i.location) },
        { label: $t('lz.entity.tmsIncident.influenceScope'), value: configName(BaseConfigTypeEnum.TmsDirectionType, i.influenceScope) },
        { label: $t('lz.label.base.position'), value: commonVarsAndFuncs.formatPosition(i.kmNumber, i.metNumber) },
        { label: $t('lz.entity.tmsIncident.zoneId'), value: state.zones.map(zone => zone.name).join(', ') || '—' },
        { label: $t('lz.label.base.camera'), value: state.cameraList.map(camera => camera.name).join(', ') || '—' },
        { label: $t('lz.entity.tmsIncident.startDate'), value: i.startDate ? useDateFormat(i.startDate, DateTimeFormatEnum.DateTimeDefaultRevert).value : '—' },
        { label: $t('lz.label.tmsIncident.processMode'), value: ({ manual: $t('lz.label.tmsIncident.manual'), auto: $t('lz.label.tmsIncident.auto'), 'auto-failed': $t('lz.label.tmsIncident.autoFailed'), 'auto-canceled': $t('lz.label.tmsIncident.autoCanceled') } as Record<string, string>)[i.processMode] ?? i.processMode },
        { label: $t('lz.entity.tmsIncident.injuredNumber'), value: i.injuredNumber },
        { label: $t('lz.entity.tmsIncident.vehicleNumber'), value: i.vehicleNumber },
        { label: $t('lz.entity.base.info'), value: i.description },
        { label: $t('lz.entity.base.remark'), value: i.remark },
    ];
});
// One row per camera selected on the incident. Only an incident without cameras falls back to the image's camera.
const imageCameras = computed(() => {
    if (state.cameraList.length) return state.cameraList;
    const cameras = new Map<string, any>();
    for (const image of state.images) {
        if (image.equipmentId && !cameras.has(image.equipmentId))
            cameras.set(image.equipmentId, { id: image.equipmentId, name: image.equipmentName || $t('lz.label.base.camera'), kmNumber: image.kmNumber, metNumber: image.metNumber });
    }
    return [...cameras.values()];
});
const imageInfo = (equipmentId: string, pictureType: IncidentPictureType) =>
    state.images.find(image => image.equipmentId === equipmentId && image.pictureType === pictureType);
const imageStatus = (image?: IncidentImage) => {
    if (image?.status === 'WaitingForSource') return $t('lz.label.tmsIncident.waitingFull');
    if (image?.status === 'PendingCapture') return $t('lz.label.tmsIncident.capturing');
    return $t('lz.label.tmsIncident.noImage');
};
// VDS evidence is shared by every camera row (registration, or a stage that reused it); name its source camera.
const vdsSourceText = (equipmentId: string, pictureType: IncidentPictureType) => {
    const image = imageInfo(equipmentId, pictureType);
    if (image?.imageSource !== 'vds' || !getIncidentImageUrl(equipmentId, pictureType)) return '';
    const position = image.kmNumber != null || image.metNumber != null
        ? ` (${commonVarsAndFuncs.formatPosition(image.kmNumber as number, image.metNumber as number)})` : '';
    return `${$t('lz.label.tmsIncident.vdsImage')}: ${image.equipmentName || $t('lz.label.tmsIncident.detectionCamera')}${position}`;
};

const incidentImageKey = (equipmentId: string | null | undefined, pictureType: IncidentPictureType) =>
    `${String(equipmentId ?? '').trim().toLowerCase()}:${pictureType}`;

const toIncidentImageUrl = (image: IncidentImage) => {
    if (image.base64)
        return image.base64.startsWith('data:') ? image.base64 : `data:${image.contentType || 'image/jpeg'};base64,${image.base64}`;

    if (!image.url)
        return '';

    const serverHost = String(commonVarsAndFuncs.serverHost ?? '').replace(/\/+$/, '');
    const relativeUrl = image.url.replace(/^\/+/, '');
    return serverHost ? `${serverHost}/${relativeUrl}` : image.url;
};

const getIncidentImageUrl = (equipmentId: string | null | undefined, pictureType: IncidentPictureType) =>
    state.incidentImageUrls[incidentImageKey(equipmentId, pictureType)] ?? '';

const getCameraPreviewImages = (equipmentId: string | null | undefined) =>
    pictureTypes.map((pictureType) => getIncidentImageUrl(equipmentId, pictureType)).filter(Boolean);

const getCameraPreviewIndex = (equipmentId: string | null | undefined, pictureType: IncidentPictureType) => {
    const imageUrl = getIncidentImageUrl(equipmentId, pictureType);
    return Math.max(getCameraPreviewImages(equipmentId).indexOf(imageUrl), 0);
};

//Tên file báo cáo khi export
const reportFileName = computed(() => {
	return 'IncidentDetailReport_' + useDateFormat(useNow(), 'YYYYMMDD_HHmmss').value;
});

const handleQuery = async(incidentId: string, requestId: number) => {
    try {
        state.isProcessing = true;
		replacePdf();
		// state.queryParams.fromDate = dateForSearch().startTime;
		// state.queryParams.endTime = dateForSearch().endTime;
		state.queryParams.reportDataId = generateDataId();
        const response = await getAPI(TmsIncidentReportApi).apiTmsreportTmsincidentreportRenderdetailPost(
            { ...state.queryParams, incidentId },
            { responseType: 'blob' }
        );

        const pdf = new Blob([response.data as any], { type: 'application/pdf' });
        if ((await pdf.slice(0, 5).text()) !== '%PDF-') throw new Error($t('lz.message.tmsIncident.reportLoadFailed'));
        if (requestId === incidentRequestId)
            replacePdf(URL.createObjectURL(pdf));

    }
    catch (error) {
        console.error('handleQuery(...)', error);
        if (requestId === incidentRequestId) ElMessage.error($t('lz.message.tmsIncident.reportLoadFailed'));
    }
    finally {
        if (requestId === incidentRequestId) state.isProcessing = false;
    }
};

// Export operation
const handleExport = async (format :string) => {
    try {
        state.isProcessing = true;
        state.queryParams.format = format;
        const response = await getAPI(TmsIncidentReportApi).apiTmsreportTmsincidentreportExportdetailPost(
           state.queryParams,
            { responseType: 'blob' }
        );

        // Get the filename from the Content-Disposition header
        let filename = `${reportFileName.value}.${state.queryParams.format}`; // Use the selected format for the filename
        // Use FileSaver to save the file
        saveAs(new Blob([response.data as any], { type: response.headers['content-type'] }), filename);

        ElMessage.success($t('lz.message.base.exportSuccess'));
    } catch (error) {
        if (error?.response?.status === 0) {
        return;
        } else {
        console.error('Error exporting file:', error);
        ElMessage.error($t('lz.message.base.exportFailed'));
        }
    } finally {
        state.isProcessing = false;
        reportViewerRef.value?.setExporting(false);
    }
};

const handlePrint = async () => {
	//api export pdf
	state.queryParams.format = 'pdf';
	const response = await getAPI(TmsIncidentReportApi).apiTmsreportTmsincidentreportExportdetailPost(
		state.queryParams,
		{ responseType: 'blob' }
	);
	const blob = new Blob([response.data as any], { type: response.headers['content-type'] });
	const url = URL.createObjectURL(blob);
	const printWindow = window.open(url, '_blank');
	printWindow?.print();
	URL.revokeObjectURL(url);	
};

const generateDataId = () => {
    const dateStr = useDateFormat(useNow(), 'YYYYMMDD'); // yyyyMMdd
    const guidStr = 'xxxxxxxxxxxx4xxxyxxxxxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
        const r = Math.random() * 16 | 0, v = c === 'x' ? r : (r & 0x3 | 0x8);
        return v.toString(16);
    });

    return `${dateStr.value}_${guidStr}`;
};

const applyIncidentImages = (lists: { pictureType: IncidentPictureType; images: IncidentImage[] }[]) => {
    lists.forEach(list => { imageResults[list.pictureType] = list.images; });
    const images = Object.values(imageResults).flat() as IncidentImage[];
    state.vdsRegistration = images.find(image => image.pictureType === 1 && image.imageSource === 'vds') ?? null;
    state.images = images;
    const imageUrls: Record<string, string> = {};
    images.forEach(image => {
        if (!image.equipmentId || image.exists === false || !image.pictureType) return;
        const pictureType = image.pictureType as IncidentPictureType;
        if (!pictureTypes.includes(pictureType)) return;
        const imageUrl = toIncidentImageUrl(image);
        if (imageUrl) imageUrls[incidentImageKey(image.equipmentId, pictureType)] = imageUrl;
    });
    state.incidentImageUrls = imageUrls;
};

const fetchIncidentImages = async (incidentId: string, types: readonly IncidentPictureType[]) => {
    const lists = await Promise.all(types.map(async pictureType => {
        try {
            const response = await getAPI(TmsIncidentApi).apiTmsTmsincidentGetimagesGet(incidentId, pictureType);
            return { pictureType, images: (response.data.result ?? []) as IncidentImage[] };
        } catch (error) {
            console.error(`Không thể tải ảnh sự cố ${incidentId}, pictureType ${pictureType}.`, error);
            return null;
        }
    }));
    return lists.filter((list): list is NonNullable<typeof list> => list !== null);
};

const loadIncidentImages = async (incidentId: string, requestId: number) => {
    stopRegistrationPoll();
    stopStagePoll();
    const loadId = ++imageLoadId;
    stagePollDeadline = Date.now() + stagePollTimeout;
    imageResults = {};
    state.incidentImageUrls = {};
    if (!incidentId)
        return;

    state.imageLoading = true;
    try {
        const lists = await fetchIncidentImages(incidentId, pictureTypes);
        if (requestId !== incidentRequestId || loadId !== imageLoadId) return;
        state.imageLoading = false;
        applyIncidentImages(lists);
        scheduleRegistrationPoll(incidentId, requestId, loadId);
        scheduleStagePoll(incidentId, requestId, loadId);
    } finally {
        if (requestId === incidentRequestId && loadId === imageLoadId) state.imageLoading = false;
    }
};

const scheduleRegistrationPoll = (incidentId: string, requestId: number, loadId: number) => {
    stopRegistrationPoll();
    const image = state.vdsRegistration;
    const deadline = Date.parse(String(image?.waitUntilUtc || ''));
    if (activeTab.value !== 'images' || image?.status !== 'WaitingForSource'
        || !Number.isFinite(deadline)) return;
    if (Date.now() >= deadline) {
        image.status = 'MissingSource';
        return;
    }
    registrationPoll = setTimeout(async () => {
        if (requestId !== incidentRequestId || loadId !== imageLoadId || activeTab.value !== 'images') return;
        try {
            const response = await getAPI(TmsIncidentApi).apiTmsTmsincidentGetimagesGet(incidentId, 1);
            if (requestId !== incidentRequestId || loadId !== imageLoadId || activeTab.value !== 'images') return;
            applyIncidentImages([{ pictureType: 1, images: (response.data.result ?? []) as IncidentImage[] }]);
        } catch (error) { console.error('Không thể tải lại ảnh đăng ký VDS.', error); }
        if (requestId === incidentRequestId && loadId === imageLoadId) scheduleRegistrationPoll(incidentId, requestId, loadId);
    }, Math.min(5000, deadline - Date.now()));
};

const pendingStageTypes = () => ([2, 3] as const).filter(pictureType =>
    imageResults[pictureType]?.some(image => image.status === 'PendingCapture' && !image.exists));

const scheduleStagePoll = (incidentId: string, requestId: number, loadId: number) => {
    stopStagePoll();
    if (requestId !== incidentRequestId || loadId !== imageLoadId || activeTab.value !== 'images'
        || state.imageLoading || Date.now() >= stagePollDeadline || !pendingStageTypes().length) return;
    stagePoll = setTimeout(async () => {
        if (requestId !== incidentRequestId || loadId !== imageLoadId || activeTab.value !== 'images'
            || Date.now() >= stagePollDeadline) return;
        const types = pendingStageTypes();
        if (!types.length) return;
        const lists = await fetchIncidentImages(incidentId, types);
        if (requestId !== incidentRequestId || loadId !== imageLoadId || activeTab.value !== 'images') return;
        applyIncidentImages(lists);
        scheduleStagePoll(incidentId, requestId, loadId);
    }, Math.min(5000, stagePollDeadline - Date.now()));
};

watch(activeTab, value => {
    stopRegistrationPoll();
    stopStagePoll();
    imageLoadId++;
    state.imageLoading = false;
    if (value === 'images' && state.queryParams.incidentId)
        void loadIncidentImages(String(state.queryParams.incidentId), incidentRequestId);
});

const loadDefaultData = async(incidentId: string, requestId: number) => {
    if(!incidentId)
        return;

    let response = await getAPI(TmsIncidentApi).apiTmsTmsincidentSinglePost({id: incidentId}) as any;
    if (requestId !== incidentRequestId) return;
    state.incidentInfo = response.data.result;
    
    if (state.incidentInfo?.zoneId) {
        try {
            const zones = await getAPI(TmsZoneApi).apiTmsTmszoneListzonebyidsGet(state.incidentInfo.zoneId);
            if (requestId !== incidentRequestId) return;
            state.zones = zones.data.result ?? [];
        } catch { ElMessage.error($t('lz.message.tmsIncident.zoneLoadFailed')); }
    }
    const equipmentIds = state.incidentInfo?.equipmentId;
    if(equipmentIds?.length > 0){
        await commonVarsAndFuncs.getCctvEquipmentTypeList();
        if (requestId !== incidentRequestId) return;

        response = await getAPI(TmsEquipmentApi).apiTmsTmsequipmentListbyidsGet(equipmentIds);
        if (requestId !== incidentRequestId) return;
        const equipmentList = response.data.result;

        state.cameraList = (equipmentList ?? []).filter((equip: any) => commonVarsAndFuncs.isCCTVEquipmentType(equip.equipmentTypeId));
    }
    if (requestId === incidentRequestId) scheduleStagePoll(incidentId, requestId, imageLoadId);
};

watch(() => route.query.incidentId, value => {
    stopRegistrationPoll();
    stopStagePoll();
    imageLoadId++;
    const incidentId = String(value ?? '').trim();
    const requestId = ++incidentRequestId;
    state.queryParams.incidentId = incidentId;
    state.incidentInfo = null;
    state.cameraList = [];
    state.incidentImageUrls = {};
    state.imageLoading = false;
    state.vdsRegistration = null;
    state.images = [];
    state.zones = [];
    state.isProcessing = false;
    replacePdf();
    if (!incidentId) return;
    void handleQuery(incidentId, requestId);
    void loadDefaultData(incidentId, requestId);
    void loadIncidentImages(incidentId, requestId);
}, { immediate: true });
onBeforeUnmount(() => { incidentRequestId++; imageLoadId++; stopRegistrationPoll(); stopStagePoll(); replacePdf(); });

</script>

<style scoped>
.rpt-incident-container {
    height: 100%;
    display: flex;
    flex-direction: column;
}

.rpt-incident-card {
    height: 100%;
    display: flex;
    flex-direction: column;
    overflow: hidden;
}

:deep(.rpt-incident-card > .el-card__body) {
    flex: 1;
    display: flex;
    flex-direction: column;
    min-height: 0;
}

.rpt-incident-tabs {
    flex: 1;
    display: flex;
    flex-direction: column;
    min-height: 0;
}

:deep(.rpt-incident-tabs .el-tabs__content) {
    flex: 1;
    min-height: 0;
}

:deep(.rpt-incident-tabs .el-tab-pane) {
    height: 100%;
    display: flex;
    flex-direction: column;
    min-height: 0;
}

.full-table {
	flex: 1;
	display: flex;
	flex-direction: column;
	overflow: hidden;
	min-height: 0;
}

.incident-details-container {
    overflow-y: auto;
    padding: 10px;
    white-space: pre-wrap;
}

.incident-images-container {
    max-height: 90vh;
    min-height: 260px;
    padding: 0 10px 60px;
    overflow-y: auto;
}

.incident-image-grid {
    display: grid;
    grid-template-columns: repeat(4, minmax(220px, 1fr));
    gap: 5px;
    overflow-x: auto;
}

.incident-image-stage {
    min-width: 220px;
}

.incident-image {
    display: block;
    width: 100%;
    height: 230px;
}

.incident-image-empty {
    display: flex;
    align-items: center;
    justify-content: center;
    width: 100%;
    height: 230px;
    background-color: var(--el-fill-color-lighter);
}

.incident-image-meta {
    height: 60px;
}

.pdf-container {
	flex: 1;
	overflow: auto;
	min-height: 0;
	display: flex;
	justify-content: center;

}

.pdf-wrapper {
	width: 100%;
	padding: 10px 0;
	display: flex;
	justify-content: center;
	min-height: 0;
}

:deep(.vue-pdf-embed) {
	max-width: 100%;
}

:deep(.vue-pdf-embed canvas) {
	display: block;
	margin: 0 auto;
}
  
.export-container {
	display: flex;
	align-items: center; /* Align items vertically in the center */
    margin-bottom: 5px;
    margin-left: 10px;
}
  
.export-select {
	width: 120px; /* Set a fixed width for the dropdown */
	margin-right: 10px; /* Add some space between the dropdown and the button */
}
</style>
