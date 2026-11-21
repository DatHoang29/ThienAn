<template>
    <div class="chart-container width-100pc height-100pc">
        <div ref="chartRef" class="chart-canvas"></div>
        <aside v-if="legendOrient === 'vertical' && showLegend" class="scroll-legend" :aria-label="$t('lz.label.base.chartLegend')" tabindex="0">
            <button v-for="item in legendItems" :key="item.name" type="button" :class="{ inactive: !item.selected }" :title="item.label" :aria-pressed="item.selected" @click="toggleSeries(item)">
                <span class="legend-swatch" :style="{ background: item.color }"></span><span class="legend-label">{{ item.label }}</span>
            </button>
        </aside>
    </div>
</template>
  
<script setup lang="ts">
import { ref, onMounted, onUnmounted, reactive, watch, nextTick } from 'vue';
import * as echarts from 'echarts';

import { useI18n } from 'vue-i18n';
const { t: $t } = useI18n();

import { useTmsStore } from '/@/stores/tmsStore';
const tmsStore = useTmsStore();

const chartRef = ref<HTMLElement | null>(null);
let chartInstance: echarts.ECharts | null = null;
let resizeObserver: ResizeObserver | undefined;
type LegendItem = { name: string; label: string; color: string; selected: boolean };
const legendItems = ref<LegendItem[]>([]);

const emits = defineEmits(['onChartMounted']);

const state = reactive({
    isProcessing: false,
    eventTypeList: [] as { id?: string | number | null; name?: string | null }[],
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
    },
    // Cho phép người dùng ẩn chú giải khi có quá nhiều loại sự kiện
    showLegend: {
        type: Boolean,
        default: true
    },
    legendOrient: {
        type: String,
        default: 'horizontal'
    }
});

// Nhãn thay thế khi BE trả về tên loại sự kiện rỗng/null (bản ghi thiếu event_type_id hoặc trỏ tới
// loại sự kiện đã bị xóa => LEFT JOIN cho ra NULL). Bắt buộc phải chuẩn hóa vì ECharts throw
// "Cannot read properties of null (reading 'name')" ngay trong LegendModel._updateData nếu
// legend.data chứa dù chỉ một phần tử null, làm mất trắng cả biểu đồ.
const normalizeName = (name: any) => (name === null || name === undefined || name === '' ? $t('lz.label.base.unknown') : String(name));
const displayName = (name: string) => {
    const label = String(state.eventTypeList?.find(item => item.id == name)?.name ?? name);
    // Some API labels contain backend documentation rather than a category name.
    if (/EventTypeId|series\.name|ECharts|LEFT JOIN/i.test(label)) return $t('lz.label.base.unclassified');
    return label.replace(/<[^>]*>/g, '').trim() || $t('lz.label.base.unclassified');
};
const htmlEntities: Record<string, string> = { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' };
const escapeHtml = (value: unknown) => String(value ?? '').replace(/[&<>"']/g, char => htmlEntities[char]);
function compactTooltip(params: any) {
    const items = Array.isArray(params) ? params : [params];
    const nonzero = items.filter(item => Number(Array.isArray(item.value) ? item.value[item.value.length - 1] : item.value) > 0);
    const heading = escapeHtml(items[0]?.axisValueLabel ?? items[0]?.name);
    const content = nonzero.map(item => {
        const value = Array.isArray(item.value) ? item.value[item.value.length - 1] : item.value;
        return `<div style="display:flex;align-items:center;gap:8px;padding:3px 0">${item.marker || ''}<span style="flex:1;white-space:normal;overflow-wrap:anywhere">${escapeHtml(displayName(item.seriesName))}</span><b>${escapeHtml(value)}</b></div>`;
    }).join('');
    return `<div style="width:240px;max-height:180px;overflow-y:auto"><strong>${heading}</strong>${content || '<div style="padding-top:6px">' + escapeHtml($t('lz.label.tmsIncident.noIncidents')) + '</div>'}</div>`;
}
function toggleSeries(item: LegendItem) {
    item.selected = !item.selected;
    chartInstance?.dispatchAction({ type: 'legendToggleSelect', name: item.name });
}

const generateChart = async() => {

    await nextTick();

    if(!chartRef.value)
        return;

    // Clone dữ liệu BE để không mutate props.chartData (generateChart có thể được gọi lại nhiều lần,
    // nếu ghi đè trực tiếp thì các chuỗi i18n sẽ bị dịch lặp ở lần chạy sau)
    const chartData = props.chartData ? JSON.parse(JSON.stringify(props.chartData)) : {};

    const chartTitle = chartData.title ?? {};
    const chartLegend = chartData.legend ?? {};
    const chartXAxis = chartData.xAxis ?? {};
    const chartSeries = (Array.isArray(chartData.series) ? chartData.series : []).filter((item: any) => item != null);

    if(chartTitle?.text)
        chartTitle.text = $t(chartTitle.text);

    if(chartXAxis?.name)
        chartXAxis.name = $t(chartXAxis.name);

    if(Array.isArray(chartLegend?.data)){
        // Chuẩn hóa null + bỏ trùng (nhiều bản ghi null sẽ cùng map về một nhãn)
        chartLegend.data = Array.from(new Set(chartLegend.data.map(normalizeName)));
        chartLegend.formatter = function (evenTypeId: string) {
            return state.eventTypeList?.find(item => item.id == evenTypeId)?.name ?? evenTypeId;
        };
    };

    const verticalLegend = props.legendOrient === 'vertical';
    chartLegend.type = 'scroll';
    chartLegend.orient = props.legendOrient;
    chartLegend.show = props.showLegend && !verticalLegend;

    // series.name phải khớp với legend.data, nên chuẩn hóa cùng một cách
    chartSeries.forEach((item: any) => { item.name = normalizeName(item.name); });

    chartInstance ??= echarts.init(chartRef.value);

    state.chartOption = {
        title: chartTitle,
        tooltip: {
            trigger: 'axis',
            // el-card bọc ngoài có overflow: hidden nên tooltip vẽ trong container sẽ bị cắt.
            // Đẩy tooltip ra body + z-index cao để không bị thanh tab phía trên che mất.
            appendTo: 'body',
            extraCssText: 'z-index: 9999;',
            ...(verticalLegend ? { enterable: true, formatter: compactTooltip } : {}),
        },
        grid: {
            // Ẩn chú giải thì lấy lại phần chiều cao đó cho vùng vẽ
            top: verticalLegend ? (chartTitle.show === false ? 16 : 60) : props.showLegend ? '25%' : '15%',
            left: '3%',
            right: '4%',
            bottom: '3%',
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

    chartInstance.setOption(state.chartOption, true);
    const palette = chartInstance.getOption().color as string[];
    const seen = new Set<string>();
    legendItems.value = chartSeries.flatMap((item: any, index: number) => {
        const name = String(item.name);
        if (seen.has(name)) return [];
        seen.add(name);
        return [{ name, label: displayName(name), color: item.lineStyle?.color || item.itemStyle?.color || palette?.[index % palette.length] || '#5470c6', selected: chartLegend.selected?.[name] !== false }];
    });
    await nextTick();
    chartInstance?.resize();
}

onMounted(async() => {
    await nextTick();

    if (chartRef.value) {
        resizeObserver = new ResizeObserver(() => {
            if (chartRef.value?.clientWidth && chartRef.value.clientHeight) chartInstance?.resize();
        });
        resizeObserver.observe(chartRef.value);
    }

    state.eventTypeList = await tmsStore.getEventTypes();

    emits('onChartMounted');
});
  
onUnmounted(() => {
    resizeObserver?.disconnect();
    if(chartInstance){
        chartInstance.dispose();
        chartInstance = null;
    }
});

watch(() => props.dataChangedCount, () => {
    if(chartInstance){
        chartInstance.dispose();
        chartInstance = null;
    }

    generateChart();
});

// Bật/tắt chú giải phải vẽ lại vì grid.top cũng thay đổi theo
watch(() => [props.showLegend, props.legendOrient], () => {
    if(chartInstance){
        chartInstance.dispose();
        chartInstance = null;
    }

    generateChart();
});

defineExpose({ generateChart });
</script>

<style lang="scss" scoped>
    .chart-container { display: flex; gap: 12px; min-width: 0; min-height: 0; overflow: hidden; }
    .chart-canvas { flex: 1; min-width: 0; height: 100%; }
    .scroll-legend { flex: 0 0 clamp(140px, 24%, 220px); min-height: 0; overflow-y: auto; overscroll-behavior: contain; scrollbar-width: thin; padding: 4px 4px 4px 8px; border-left: 1px solid var(--monitor-border, #e0e7f0); }
    .scroll-legend button { display: flex; align-items: center; gap: 7px; width: 100%; border: 0; border-radius: 4px; background: transparent; color: var(--monitor-text, #203047); text-align: left; padding: 5px 3px; font: inherit; font-size: 11px; cursor: pointer; }
    .scroll-legend button:hover, .scroll-legend button:focus-visible { background: var(--monitor-primary-soft, #edf3ff); }
    .scroll-legend button.inactive { opacity: .45; }
    .legend-swatch { flex: 0 0 8px; height: 8px; border-radius: 50%; }
    .legend-label { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
</style>
