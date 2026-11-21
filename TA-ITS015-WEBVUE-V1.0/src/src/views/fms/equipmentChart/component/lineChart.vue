<template>
    <div ref="chartRef" class="chart-container width-100pc height-100pc" style="min-width: 100%; min-height: 100%;"></div>
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

    chartInstance = echarts.init(chartRef.value);
    chartInstance.hideLoading();

    const chartTitle = props.chartData?.title ?? {};
    const chartLegend = props.chartData?.legend ?? {};
    const chartXAxis = props.chartData?.xAxis ?? {};
    const chartSeries = props.chartData?.series ?? [];

    if(!chartSeries)
        return;

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
            trigger: 'axis'
        },
        grid: {
            top: '15%',
            left: '3%',
            right: '4%',
            bottom: '15%',
            containLabel: true
        },
        toolbox: {
            feature: {
                saveAsImage: {}
            }
        },
        yAxis: {
            type: 'value',
            minInterval: 1
        },
        xAxis: chartXAxis,
        legend: chartLegend,
        series: chartSeries,
    }

    if(state.chartOption?.xAxis?.name)
        state.chartOption.xAxis.name = $t(state.chartOption.xAxis.name);

    chartInstance.setOption(state.chartOption);
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
    if(chartInstance)
        chartInstance.dispose();

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