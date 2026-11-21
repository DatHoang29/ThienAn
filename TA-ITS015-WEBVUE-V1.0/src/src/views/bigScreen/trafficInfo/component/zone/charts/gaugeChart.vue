<template>
    <el-alert v-if="state.error" :title="state.error" type="warning" :closable="false" />
    <div v-if="state.isProcessing" style="text-align: center; padding-top: 20px;">
        <div v-loading="state.isProcessing" :element-loading-text="$t('lz.label.base.loading')">
        </div>
    </div>
    <div ref="chartRef" style="width: 200px; height: 200px; margin: auto"></div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, onUnmounted, onActivated, onDeactivated, watch, nextTick } from 'vue';
import { useThrottleFn } from '@vueuse/core';
import { useVdsSse } from '/@/transporter/hooks/useVdsSse';
import * as echarts from 'echarts';

import { useI18n } from 'vue-i18n';
const { t: $t } = useI18n();

import { getAPI } from '/@/utils/axios-utils';
import { VdsDashboardApi } from '/@/api-services/vds';



const chartRef = ref<HTMLElement | null>(null);
let chartInstance: echarts.ECharts | null = null;

const props = defineProps({
    requestTime: {
        type: Number,
        default: null
    },
    zoneInfo: {
        type: Object,
        default: null
    },
    directionCode: {
        type: String,
        default: null
    }
});

const state = reactive({
    isProcessing: false,
    maxSpeed: 160,
    error: '',
    avgSpeed: null as number | null
});

let requestId = 0;
let disposed = false;
let active = true;
let poll: ReturnType<typeof setInterval> | undefined;
/** Vùng đã có số chưa: chỉ lần tải ĐẦU mới hiện loading/cảnh báo, các lần SSE sau cập nhật âm thầm. */
let loadedZoneId: string | null = null;
/** Giá trị đã vẽ gần nhất (undefined = chưa vẽ); trùng thì không setOption lại. */
let drawnSpeed: number | null | undefined;
const setupChart = async() => {
    if (disposed || !active) return;
    const current = ++requestId;
    if(!props.zoneInfo || !props.directionCode)
        return;

    // Đổi vùng ⇒ coi như tải lần đầu (hiện loading, vẽ lại kim từ đầu).
    const firstLoad = loadedZoneId !== props.zoneInfo.id;
    if (firstLoad) {
        drawnSpeed = undefined;
        state.avgSpeed = null; // không để số của vùng trước hiện trên vùng mới nếu lần tải này lỗi
    }

    try{
        // Loading là một khối CHIẾM CHỖ phía trên đồng hồ: bật ở mỗi lần SSE làm đồng hồ bị đẩy xuống rồi
        // giật lên — đó là phần "reload" của tab tốc độ. Chỉ bật ở lần tải đầu của vùng.
        if (firstLoad) state.isProcessing = true;

        const response = await getAPI(VdsDashboardApi).apiVdsVdsdashboardZoneinfoGet(props.zoneInfo.id);
        if (disposed || !active || current !== requestId) return;
        const zone = response.data.result?.zones?.find(item => item.zoneId === props.zoneInfo.id);
        if (!zone) throw new Error('Không có dữ liệu vùng VDS');
        state.avgSpeed = zone.last1h?.avgSpeed ?? null;
        state.error = '';
        loadedZoneId = props.zoneInfo.id;

        await nextTick(); // Đợi Vue cập nhật DOM xong
    }
    catch(error){
        if (disposed || !active || current !== requestId) return;
        // Đã có số mà lỗi tạm thời ⇒ giữ kim ở giá trị cũ, không bật/tắt cảnh báo làm giật bố cục.
        if (firstLoad) state.error = $t('lz.message.zoneInfo.speedLoadFailed');
        console.error('gaugeChart.vue: setupChart(...)', error);
    }
    finally{
        if (current === requestId) state.isProcessing = false;
    }

    if (!disposed && active && current === requestId && chartRef.value) {
        const existing = echarts.getInstanceByDom(chartRef.value);
        // Số không đổi (đa số lượt SSE: luồng `count` là của TOÀN TUYẾN) ⇒ không đụng tới đồng hồ.
        if (existing && drawnSpeed !== undefined && drawnSpeed === state.avgSpeed) return;

        if (existing && drawnSpeed !== undefined) {
            // Đã vẽ: chỉ đổi giá trị ⇒ ECharts trượt kim tới số mới (valueAnimation), không dựng lại cả đồng hồ.
            existing.setOption({ series: [{ data: [{ value: state.avgSpeed ?? 0 }] }] });
            drawnSpeed = state.avgSpeed;
            return;
        }

        chartInstance = existing || echarts.init(chartRef.value);

        const option = {
            series: [{
                type: 'gauge',
                max: state.maxSpeed, // Giá trị tối đa của gauge
                splitNumber: 8, // Chia thành 5 đoạn (150 / 5 = 30)
                // radius: '75%', // Điều chỉnh kích thước đường kính hình tròn
                // Đường tròn chính thể hiện tốc độ
                axisLine: {
                    lineStyle: {
                        width: 10,
                        color: [
                            [0.27, '#66ccff'], // Tốc độ thấp: 0-40 km/h
                            [0.8, '#ff9966'],  // Tốc độ trung bình: 41-120 km/h
                            [1, '#ff3300']     // Tốc độ cao: 121-150 km/h
                        ]
                    }
                },
                pointer: {
                    itemStyle: {
                        color: 'auto'
                    }
                },
                // Đường kẻ nhỏ thể hiện đơn vị minimum
                axisTick: {
                        distance: -5,
                        length: 5,
                        lineStyle: {
                            color: '#fff',
                            width: 1
                        }
                },
                // Đường kẻ phân khúc các nhóm tốc độ
                splitLine: {
                        distance: -20,
                        length: 20,
                        lineStyle: {
                        color: '#fff',
                        width: 2
                    }
                },
                // Label hiển thị các giá trị tốc độ
                axisLabel: {
                    color: 'inherit',
                    distance: -25,
                    fontSize: 10
                },
                detail: {
                    valueAnimation: true,
                    formatter: (value: number) => state.avgSpeed == null ? '—' : `${value} Km/h`,
                    fontSize: 14,
                    offsetCenter: [0, '70%'], // Đẩy text xuống dưới đáy vòng tròn
                    color: 'inherit'
                },
                data: [{
                    // Giá trị tốc độ hiện tại
                    // name: 'Tốc độ',
                    value: state.avgSpeed ?? 0
                }]
            }]
        };

        chartInstance.setOption(option);
        drawnSpeed = state.avgSpeed;
    }
};

onMounted(() => {
    void setupChart();
    startPoll();
});
onActivated(() => { if (active) return; active = true; void setupChart(); startPoll(); });
onDeactivated(() => { active = false; requestId++; state.isProcessing = false; stopPoll(); });

onUnmounted(() => {
    disposed = true; active = false; requestId++; stopPoll();
    if (chartInstance) {
        chartInstance.dispose();
    }
});

watch(() => [props.requestTime, props.zoneInfo?.id, props.directionCode], () => { void setupChart(); });
const startPoll = () => { if (!poll) poll = setInterval(() => { void setupChart(); }, 60000); };
const stopPoll = () => { if (poll) clearInterval(poll); poll = undefined; };
const refresh = useThrottleFn(() => setupChart(), 1500);
useVdsSse(['count'], refresh);
</script>
