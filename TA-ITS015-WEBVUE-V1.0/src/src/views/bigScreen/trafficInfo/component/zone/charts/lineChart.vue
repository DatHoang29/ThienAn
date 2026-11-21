<template>
    <div ref="chartRef" class="chart-container width-100pc height-100pc" style="min-width: 100%; min-height: 100%;"></div>
</template>
  
<script setup lang="ts">
import { ref, onMounted, onUnmounted, reactive, watch, nextTick } from 'vue';
import * as echarts from 'echarts';
import { dayjs } from 'element-plus';

import { useI18n } from 'vue-i18n';
const { t: $t } = useI18n();



const chartRef = ref<HTMLElement | null>(null);
let chartInstance: echarts.ECharts | null = null;
let resizeObserver: ResizeObserver | null = null;

const emits = defineEmits(['onChartMounted']);

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

/**
 * Chữ ký dữ liệu lần vẽ gần nhất. SSE `count` là luồng của TOÀN TUYẾN nên bắn mỗi khi có xe qua bất kỳ
 * camera nào; phần lớn các lần đó số liệu của vùng đang mở KHÔNG đổi ⇒ bỏ qua để biểu đồ không vẽ lại.
 */
let lastSignature = '';

const generateChart = async() => {

    await nextTick();

    if (!chartRef.value) return;

    const signature = JSON.stringify(props.chartData ?? null);
    const existing = echarts.getInstanceByDom(chartRef.value);
    if (existing && signature === lastSignature) return;

    chartInstance = existing || echarts.init(chartRef.value);
    chartInstance.hideLoading();

    const chartTitle = { ...props.chartData?.title };
    const chartLegend = props.chartData?.legend ?? {};
    const chartXAxis = props.chartData?.xAxis ?? {};
    const chartSeries = props.chartData?.series ?? [];

    if(!chartSeries)
        return;

    if(chartTitle?.text)
        chartTitle.text = $t(chartTitle.text).toUpperCase();

    // if(chartLegend?.data){
    //     chartLegend.formatter = function (evenTypeId: string) {
    //         return state.eventTypeList?.find(item => item.id == evenTypeId)?.name ?? evenTypeId;
    //     };
    // };

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
                // Thay saveAsImage mặc định (tên file cố định = tiêu đề) bằng nút tự dựng: tên file kèm thời điểm lưu tới giây,
                // vd "BIỂU ĐỒ LƯU LƯỢNG GIAO THÔNG 20261112165523.png". Tên feature tự dựng của ECharts phải bắt đầu bằng "my".
                mySaveAsImage: {
                    show: true,
                    title: $t('lz.button.base.download'),
                    icon: 'path://M4.7,22.9L29.3,45.5L54.7,23.4M4.6,43.6L4.6,58L53.8,58L53.8,43.6M29.2,45.1L29.2,0',
                    onclick: saveChartImage,
                },
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

    // Cập nhật TRÊN instance đang có thay vì dispose + init lại: ECharts chỉ chuyển đổi phần dữ liệu đổi
    // (không vẽ lại từ đầu). replaceMerge để series/legend/trục bị bỏ (vd đổi vùng) không còn sót lại.
    chartInstance.setOption(state.chartOption, { replaceMerge: ['series', 'legend', 'xAxis'] });
    lastSignature = signature;
}

/** Lưu biểu đồ thành PNG (nền trắng, bỏ thanh công cụ như saveAsImage mặc định); tên = tiêu đề + thời điểm lưu. */
const saveChartImage = () => {
    if (!chartInstance) return;
    const title = String((state.chartOption as any)?.title?.text ?? '').trim() || 'chart';
    const link = document.createElement('a');
    link.download = `${title} ${dayjs().format('YYYYMMDDHHmmss')}.png`;
    link.href = chartInstance.getDataURL({ type: 'png', pixelRatio: 2, backgroundColor: '#fff', excludeComponents: ['toolbox'] });
    link.click();
};

onMounted(async() => {
    await nextTick();

    if (!chartRef.value) return;
    chartInstance = echarts.getInstanceByDom(chartRef.value) || echarts.init(chartRef.value);
    chartInstance.showLoading({
        textAlign: 'center',
        textVerticalAlign: 'middle',
    });

    // Tự resize khi container đổi kích thước (vd: modal mới mở, tab v-show ẩn -> hiện,
    // panel/modal được resize...) -> tránh chart bị vẽ nhỏ/kẹt kích thước lúc khởi tạo
    // khi container chưa có kích thước thật (0 hoặc đang bị co lại bởi flex/tab ẩn).
    if (typeof ResizeObserver !== 'undefined' && chartRef.value) {
        resizeObserver = new ResizeObserver(() => {
            chartInstance?.resize();
        });
        resizeObserver.observe(chartRef.value);
    }

    await generateChart();

    emits('onChartMounted');
});
  
onUnmounted(() => {
    resizeObserver?.disconnect();
    resizeObserver = null;

    if(chartInstance)
        chartInstance.dispose();
});

// KHÔNG dispose ở đây: dispose + init lại khiến cả biểu đồ vẽ lại từ đầu (kèm animation) mỗi lần SSE
// cập nhật — đó chính là hiện tượng "reload" của popup. generateChart tự bỏ qua khi dữ liệu không đổi.
watch(() => props.dataChangedCount, () => {
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