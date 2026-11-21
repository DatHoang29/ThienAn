<template>
    <div ref="chartRef" class="chart-container width-100pc height-95pc" style="min-width: 100%; min-height: 95%;"></div>
</template>
  
<script setup lang="ts">
import { ref, onMounted, onUnmounted, reactive, watch, nextTick } from 'vue';
import * as echarts from 'echarts';
import { useResizeObserver } from '@vueuse/core';

import { useI18n } from 'vue-i18n';
const { t: $t } = useI18n();

import { getAPI } from '/@/utils/axios-utils';
import { TmsEventTypeApi } from '/@/api-services/tms';

const chartRef = ref<HTMLElement | null>(null);
let chartInstance: echarts.ECharts | null = null;

const emits = defineEmits(['onChartMounted']);

// Co giãn theo khung chứa (kéo PanelSplitter / thu gọn danh sách bên trái)
useResizeObserver(chartRef, () => chartInstance?.resize());

const state = reactive({
    isProcessing: false,
    eventTypeList: [],
    chartOption: {},
});
  
const props = defineProps({
    dataChangedCount: {
        type: Number,
        default: 0
    },
    chartData: {
        type: Object,
        default: null
    }
});

const generateChart = async() => {

    await nextTick();

    // Bổ sung kiểm tra: Chỉ khởi tạo nếu chartInstance chưa tồn tại
    if (!chartInstance) {
        chartInstance = echarts.init(chartRef.value);
    }
    chartInstance.hideLoading();

    const chartTitle = props.chartData?.title ?? {};
    const chartLegend = props.chartData?.legend ?? {};
    const chartYAxis = props.chartData?.yAxis ?? {};
    let chartSeries = props.chartData?.series ?? [];

    if(!chartSeries)
        return;

    // Bỏ HẲN trạng thái (series) có TỔNG giá trị = 0 -> không hiển thị lên chart lẫn chú thích (legend).
    const seriesHasData = (s: any) =>
        Array.isArray(s?.data) &&
        s.data.some((d: any) => {
            const v = d && typeof d === 'object' ? d.value : d;
            return v; // 0 / null / undefined -> coi như không có dữ liệu
        });
    chartSeries = chartSeries.filter(seriesHasData);

    if(chartSeries[0]?.data?.length > 6){
        chartSeries.forEach((series) => {
            series.barCategoryGap = 10;
            delete series.barWidth;
        });
    }

    if(chartSeries[0]?.data?.length > 12){
        chartSeries.forEach((series) => {
            series.barCategoryGap = 5;
            delete series.barWidth;
        });
    }

    // Chuẩn hóa NHÃN SỐ trên từng đoạn cột (áp cho mọi series trả từ API):
    // - Nổi bật: chữ trắng in đậm + viền chữ tối mảnh để đọc rõ trên mọi màu nền cột.
    // - Ẩn nhãn khi giá trị = 0 / null (đoạn cột rỗng) -> tránh số "0" lửng lơ ở mép.
    // - Hover: viền trắng + đổ bóng quanh đoạn cột đang trỏ cho nổi bật.
    chartSeries.forEach((series: any) => {
        // Chuyển mọi giá trị = 0 thành null NGAY TẠI DỮ LIỆU:
        // ECharts sẽ không vẽ đoạn cột lẫn nhãn cho ô null -> chắc chắn không còn số "0" trên chart.
        if (Array.isArray(series.data)) {
            series.data = series.data.map((d: any) => {
                if (d && typeof d === 'object') {
                    return d.value ? d : { ...d, value: null }; // 0/null -> null (giữ style nếu có)
                }
                return d ? d : null; // số 0 -> null
            });
        }
        series.label = {
            ...(series.label ?? {}),
            show: true,
            position: 'inside',
            color: '#ffffff',
            fontWeight: 'bold',
            fontSize: 12,
            textBorderColor: 'rgba(0, 0, 0, 0.35)',
            textBorderWidth: 2,
            formatter: (p: any) => {
                // data có thể là số hoặc object { value }
                const v = p?.value && typeof p.value === 'object' ? p.value.value : p?.value;
                return v ? v : ''; // 0 / null / undefined -> ẩn nhãn
            },
        };
        series.emphasis = {
            ...(series.emphasis ?? {}),
            focus: 'series',
            itemStyle: {
                ...(series.emphasis?.itemStyle ?? {}),
                borderColor: '#ffffff',
                borderWidth: 2,
                shadowBlur: 6,
                shadowColor: 'rgba(0, 0, 0, 0.3)',
            },
        };
    });

    if(chartTitle?.text)
        chartTitle.text = $t(chartTitle.text).toUpperCase();

    if(chartLegend?.data){
        chartLegend.formatter = function (evenTypeId: string) {
            return state.eventTypeList?.find(item => item.id == evenTypeId)?.name ?? evenTypeId;
        };
    };

    state.chartOption = {
        title: chartTitle,
        tooltip: {
            trigger: 'axis',
            axisPointer: {
                type: 'shadow'
            },
            // Chỉ liệt kê series có giá trị khác 0 (bỏ hẳn dòng giá trị = 0).
            formatter: (params: any) => {
                const list = Array.isArray(params) ? params : [params];
                const getVal = (p: any) => (p?.value && typeof p.value === 'object' ? p.value.value : p?.value);
                const items = list.filter((p) => getVal(p)); // 0 / null / undefined -> loại
                if (!items.length) return '';
                const title = list[0]?.axisValueLabel ?? list[0]?.name ?? '';
                const rows = items
                    .map((p) => `${p.marker}${p.seriesName}<span style="float:right;margin-left:24px;font-weight:bold">${getVal(p)}</span>`)
                    .join('<br/>');
                return `<div style="font-weight:bold;margin-bottom:4px">${title}</div>${rows}`;
            }
        },
         toolbox: {
            feature: {
                saveAsImage: {}
            }
        },
        grid: {
            left: '3%',
            right: '4%',
            bottom: '20%',
            containLabel: true
        },
        xAxis: {
            type: 'value'
        },
        yAxis: chartYAxis,
        legend: {},
        series: chartSeries
    }

    if(state.chartOption?.xAxis?.name)
        state.chartOption.xAxis.name = $t(state.chartOption.xAxis.name);

    // notMerge = true: thay thế toàn bộ option cũ.
    // Nếu merge (mặc định), khi chuyển sang loại thiết bị KHÔNG có dữ liệu (series rỗng),
    // ECharts giữ lại series/yAxis của loại trước đó -> biểu đồ hiển thị nhầm dữ liệu cũ (vd: VMS).
    chartInstance.setOption(state.chartOption, true);
}

onMounted(async() => {
    await nextTick();

    chartInstance = echarts.init(chartRef.value);
    chartInstance.showLoading();

    let response = await getAPI(TmsEventTypeApi).apiTmsTmseventtypeListGet();
    state.eventTypeList = response.data.result;

    emits('onChartMounted');
});
  
onUnmounted(() => {
    if(chartInstance)
        chartInstance.dispose();
});

watch(() => props.dataChangedCount, () => {
    // if(chartInstance)
    //     chartInstance.dispose();

    generateChart();
});

defineExpose({ generateChart });
</script>

<style lang="scss" scoped>
    .chart-container{
        canvas {
            height: 100%;
            width: 100%;
        }
    }
</style>