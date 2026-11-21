<template>
    <div ref="chartRef" style="width: 100%; height: 100%; min-height: 300px;"></div>
</template>

<script lang="ts" setup>
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
    chartOption: {} as any,
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

const mapPieData = (raw: any) => {
    const arr: any[] = Array.isArray(raw) ? raw : [];
    return arr.map((item: any) => ({
        name: item?.name ?? item?.Name ?? '',
        value: item?.value ?? item?.Value ?? 0,
        itemStyle: item?.itemStyle ?? (item?.ItemStyle ? { color: item.ItemStyle.Color ?? item.ItemStyle.color } : undefined)
    }));
};

const generateChart = async() => {

    await nextTick();

    chartInstance = echarts.init(chartRef.value);
    chartInstance.hideLoading();

    const pieData = mapPieData(props.chartData);
    const total = pieData.reduce((sum, item) => sum + (Number(item.value) || 0), 0);

    state.chartOption = {
    textStyle: {
        fontFamily: 'Inter, Helvetica, Arial, sans-serif'
    },
    title: {
        text: $t('lz.label.tmsEquipmentStateChart.stateChartTitle')?.toUpperCase(),
        subtext: '',
        left: 'center',
        bottom: '0'
    },
    tooltip: { trigger: 'item', formatter: '{b}: <b>{c}</b> ({d}%)' },
    legend: { top: '2%', left: 'center', itemWidth: 14, itemHeight: 10 },
    // Tổng số thiết bị hiển thị ở tâm vòng tròn (chi tiết từng trạng thái xem qua tooltip)
    graphic: [
        {
            type: 'text',
            left: 'center',
            top: 'middle',
            style: {
                text: `{v|${total}}`,
                rich: { v: { fontSize: 30, fontWeight: 'bold', fill: '#303133' } },
                textAlign: 'center'
            }
        }
    ],
    series: [
        {
            type: 'pie',
            radius: ['42%', '66%'],
            center: ['50%', '50%'],
            avoidLabelOverlap: false,
            itemStyle: { borderColor: '#fff', borderWidth: 2 },
            label: { show: false, position: 'center' },
            emphasis: {
                scale: true,
                scaleSize: 6,
                label: { show: false }
            },
            labelLine: { show: false },
            data: pieData
        }
    ]
};


    (state.chartOption as any).series[0].data = pieData;

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
