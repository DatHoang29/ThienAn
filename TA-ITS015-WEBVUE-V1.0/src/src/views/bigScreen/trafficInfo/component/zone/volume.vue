<template>
    <div class="volume-info-container">
        <el-alert v-if="state.error" :title="state.error" type="warning" :closable="false" />
        <div class="border-info-cus volume-chart-card">
            <div class="width-100pc height-300px bg-color--white">
             <lineChartComp ref="lineChartCompRef" :dataChangedCount="state.dataChangedCount" :chartData="state.lineChartData" />
            </div>
        </div>      
        <div class="volume-table-row">
            <template v-if="state.zoneInfo">
                <div class="border-content-info width-100pc">
                    <h3 class="text-left color-white ml5">{{ $t('lz.label.zoneInfo.trafficVolumeTable', {direction: state.zoneInfo.name || state.zoneInfo.code || ''}) }}</h3>
                    <table class="table-data table-data--dark width-100pc bg-color--white mt10 text-color--black">                      
                        <tr class="text-color--mute">
                            <th class="text-left">{{ $t('lz.label.zoneInfo.vehicleType') }}</th>
                            <th class="text-center">{{ $t('lz.label.zoneInfo.hourCount') }}</th>
                            <th class="text-center">{{ $t('lz.label.zoneInfo.dayCount') }}</th>
                        </tr>
                        <tr v-for="veh in state.vehicleTypeList">
                            <td class="text-left">
                                <!-- Hiển thị ảnh minh hoạ loại xe (S/M/L); tooltip là tên loại xe, thiếu ảnh thì hiển thị tên -->
                                <el-tooltip v-if="getVehicleImage(veh)" :content="veh.name" placement="top" :show-after="150">
                                    <img class="veh-img" :src="getVehicleImage(veh)" :alt="veh.name" />
                                </el-tooltip>
                                <span v-else>{{ veh.name }}</span>
                            </td>
                            <td class="text-center">
                                {{ 
                                    state.trafficDataHourAgo?.find(
                                        item => item?.vehicleSize?.trim().toLowerCase() == veh?.code.trim().toLowerCase()
                                    )?.vehicleCount ?? '—'
                                }}
                            </td>
                            <td class="text-center">
                                {{ 
                                    state.trafficDataDayAgo?.find(
                                        item => item?.vehicleSize?.trim().toLowerCase() == veh?.code.trim().toLowerCase()
                                    )?.vehicleCount ?? '—'
                                }}
                            </td>
                        </tr>
                    </table>
                </div>
            </template>
        </div>
    </div>
</template>

<script setup>

import { reactive, watch, onMounted, onUnmounted, onActivated, onDeactivated } from 'vue';
import { useThrottleFn } from '@vueuse/core';
import { useVdsSse } from '/@/transporter/hooks/useVdsSse';

import { useI18n } from 'vue-i18n';
const { t: $t } = useI18n();

import { getAPI } from '/@/utils/axios-utils';
import { VdsDashboardApi } from '/@/api-services/vds';

import lineChartComp from './charts/lineChart.vue';
import { vehicleCountField } from '../vehicleCountField';
import { useBaseConfig } from '/@/stores/baseConfig';
import { BaseConfigTypeEnum } from '/@/types/enums';
import vehicleShortImage from '/@/assets/vehicle/S.png';
import vehicleMediumImage from '/@/assets/vehicle/M.png';
import vehicleLongImage from '/@/assets/vehicle/L.png';

const getConfigDataByCode = useBaseConfig().getConfigDataByCode;

/**
 * Ảnh minh hoạ loại xe: cột `value` của cấu hình loại xe chứa tên file (S.png / M.png / L.png),
 * import tĩnh để Vite bundle được đường dẫn thật khi build.
 */
const VEHICLE_IMAGE_MAP = {
    's.png': vehicleShortImage,
    'm.png': vehicleMediumImage,
    'l.png': vehicleLongImage,
};

/** Thứ tự dòng trong bảng phân loại xe: xe ngắn (S) -> trung (M) -> dài (L). */
const VEHICLE_DISPLAY_ORDER = ['s.png', 'm.png', 'l.png'];

/** Chuẩn hoá tên file ảnh ở cột `value` của cấu hình loại xe. */
const getVehicleImageKey = (vehicleType) => String(vehicleType?.value ?? '').trim().toLowerCase();

/** Lấy ảnh loại xe theo tên file đã cấu hình ở cột `value`; không khớp thì trả về '' để hiển thị tên. */
const getVehicleImage = (vehicleType) => VEHICLE_IMAGE_MAP[getVehicleImageKey(vehicleType)] ?? '';

/** Thứ tự hiển thị S -> M -> L, không phụ thuộc thứ tự trả về của cấu hình. */
const getVehicleOrder = (vehicleType) => {
    const orderIndex = VEHICLE_DISPLAY_ORDER.indexOf(getVehicleImageKey(vehicleType));
    // Loại xe chưa gán ảnh S/M/L thì xếp cuối bảng, giữ nguyên thứ tự gốc giữa chúng.
    return orderIndex < 0 ? VEHICLE_DISPLAY_ORDER.length : orderIndex;
};

const state = reactive({
    error: '',
    isProcessing: false,
    requestTime: null,
    zoneInfo: null,
    vehicleTypeList: [],
    directionList: [],
    trafficDataDayAgo: [],
    trafficDataHourAgo: [],

    dataChangedCount: 0,
    lineChartData: {}
});

const props = defineProps({
    requestTime: {
        type: Number,
        default: null
    },
    zoneInfo: {
        type: Object,
        default: null
    }
});

let latestRequestId = 0;
/** Chữ ký chart lần gần nhất đã đẩy xuống lineChart; đổi vùng thì xoá để vùng mới luôn vẽ. */
let lastChartSignature = '';
/** Đã có số của vùng đang mở chưa — có rồi thì lỗi tạm thời không hiện cảnh báo. */
let hasData = false;
let disposed = false;
let active = true;
let poll;
const getTrafficData = async() => {
    if (disposed || !active) return;
    const requestId = ++latestRequestId;
    try{
        state.isProcessing = true;

        state.directionList = getConfigDataByCode(BaseConfigTypeEnum.TmsDirectionType);
        // Copy trước khi sort để không đổi thứ tự mảng gốc trong store cấu hình.
        state.vehicleTypeList = [...getConfigDataByCode(BaseConfigTypeEnum.TmsVehicleTypeConfigCode)]
            .sort((a, b) => getVehicleOrder(a) - getVehicleOrder(b));

        if(state.zoneInfo){

            const response = await getAPI(VdsDashboardApi).apiVdsVdsdashboardZoneinfoGet(state.zoneInfo.id);
            if (disposed || !active || requestId !== latestRequestId) return;
            const zone = response.data.result?.zones?.find(item => item.zoneId === state.zoneInfo.id);
            if (!zone) throw new Error('Không có dữ liệu VDS cho vùng này');
            const rows = block => state.vehicleTypeList.map(vehicle => ({
                direction: state.zoneInfo.laneId,
                vehicleSize: String(vehicle.code),
                vehicleCount: vehicleCountField(vehicle) ? block?.[vehicleCountField(vehicle)] ?? null : null,
            }));
            state.trafficDataHourAgo = rows(zone.last1h);
            state.trafficDataDayAgo = rows(zone.last24h);
            state.error = '';
            hasData = true;

            // SSE `count` là luồng TOÀN TUYẾN: phần lớn lượt cập nhật không đổi số của vùng này. Chỉ báo biểu
            // đồ vẽ lại khi dữ liệu chart thực sự khác, tránh popup "reload" mỗi lần có xe qua camera khác.
            const signature = JSON.stringify(zone.chart ?? null);
            if (signature !== lastChartSignature) {
                lastChartSignature = signature;
                state.lineChartData = zone.chart;
                state.dataChangedCount++;
            }
        }
    }
    catch(error){
        // Lỗi tạm thời (mạng chập chờn) khi ĐÃ có số ⇒ giữ số cũ, không bật/tắt khung cảnh báo làm giật bố cục.
        if (active && requestId === latestRequestId && !hasData) state.error = $t('lz.message.zoneInfo.volumeLoadFailed');
        console.error('volume.vue: getTrafficData(...)', error);
    }
    finally{
        if (requestId === latestRequestId) state.isProcessing = false;
    }
};

watch(() => [props.requestTime, props.zoneInfo?.id], async([, zoneId], previous) => {
    const zoneChanged = !previous || zoneId !== previous[1];
    state.zoneInfo = props.zoneInfo;
    // Chỉ xoá số khi ĐỔI VÙNG. Chuyển tab (requestTime đổi) mà vẫn vùng cũ thì giữ nguyên số và biểu đồ,
    // nếu không bảng/biểu đồ sẽ trống rồi hiện lại — cũng là một kiểu "reload".
    if (zoneChanged) {
        state.lineChartData = {};
        state.trafficDataHourAgo = [];
        state.trafficDataDayAgo = [];
        lastChartSignature = '';
        hasData = false;
        state.error = '';
        // Báo biểu đồ xoá số của vùng cũ (lỗi tải vùng mới thì không còn hiện nhầm biểu đồ vùng trước).
        state.dataChangedCount++;
    }
    await getTrafficData();
}, { immediate: true });
const refresh = useThrottleFn(() => { if (state.zoneInfo) return getTrafficData(); }, 1500);
useVdsSse(['count'], refresh);
const startPoll = () => { if (!poll) poll = setInterval(() => { if (state.zoneInfo) void getTrafficData(); }, 60000); };
const stopPoll = () => { if (poll) clearInterval(poll); poll = undefined; };
onMounted(startPoll);
onActivated(() => { if (active) return; active = true; if (state.zoneInfo) void getTrafficData(); startPoll(); });
onDeactivated(() => { active = false; latestRequestId++; state.isProcessing = false; stopPoll(); });
onUnmounted(() => { disposed = true; active = false; latestRequestId++; stopPoll(); });
</script>

<style scoped lang="scss">
    @import '/@/theme/its/trafficInfo.scss';

    /*
     * Trước đây dùng class utility "display-flex--vertical" = flex-direction:column + flex-wrap:wrap.
     * Với flex-wrap:wrap trên container CÓ CHIỀU CAO CỐ ĐỊNH (height:100%), khi nội dung
     * (chart 300px + bảng dữ liệu) VƯỢT quá chiều cao khả dụng, flexbox không cuộn mà tự
     * NHẢY SANG CỘT MỚI bên phải -> giao diện co lại như chỉ còn "1 dòng" và phần còn lại
     * bị đẩy ra ngoài / chồng lấp. Đây là nguyên nhân của lỗi "chỉ hiển thị 1 dòng".
     * Thay bằng layout cột thường (không wrap) + overflow-y:auto để scroll đủ nội dung.
     */
    .volume-info-container {
        display: flex;
        flex-direction: column;
        gap: 16px;
        height: 100%;
        overflow-y: auto;
        overflow-x: hidden;
        padding: 2px 4px 8px;

        &::-webkit-scrollbar {
            width: 6px;
        }

        &::-webkit-scrollbar-thumb {
            background: color-mix(in srgb, var(--el-color-primary) 30%, transparent);
            border-radius: 3px;
        }
    }

    .volume-chart-card {
        flex: 0 0 auto;
    }

    .volume-table-row {
        flex: 0 0 auto;
        display: flex;
        align-items: flex-start;
        gap: 20px;
        width: 100%;
    }

    /* Ảnh loại xe (S/M/L) thay cho tên loại xe; ảnh đã chứa sẵn chú thích chiều dài */
    .veh-img {
        display: block;
        height: 52px;
        width: auto;
        max-width: 100%;
        object-fit: contain;
    }
</style>
