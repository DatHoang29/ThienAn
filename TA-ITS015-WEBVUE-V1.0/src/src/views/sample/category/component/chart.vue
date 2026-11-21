<template>
	<div class="samp-category-chart-container">
		<el-dialog v-model="state.isShowDialog" draggable :close-on-click-modal="false" width="1200px">
			<template #header>
				<div style="color: #fff">
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Edit />
					</el-icon>
					<span> {{ props.title }} </span>
				</div>
			</template>
			<div class="samp-category-chart">
                <div ref="chartRef" class="chart"></div>
            </div>
			<template #footer>
				<span class="dialog-footer">
					<el-button @click="cancel">{{ $t('lz.button.base.close') }}</el-button>
				</span>
			</template>
		</el-dialog>
	</div>
</template>

<script lang="ts" setup name="sampEditCategory">
import { reactive, ref, nextTick } from 'vue';
import { getAPI } from '/@/utils/axios-utils';
import { SampCategoryApi } from '/@/api-services/sample';
import { useI18n } from 'vue-i18n';
const { t: $t } = useI18n();
import { useNow } from '@vueuse/core'; // Import useNow

const chartRef = ref(null);
const now = useNow();

//chart
import * as echarts from 'echarts';


interface Props {
	/** the type of operation */
	title: string;
}

const props = defineProps<Props>();

const state = reactive({
	isShowDialog: false,
    chartOption: {} as any
});

// Open dialog
const openDialog = async () => {

    let data = await getAPI(SampCategoryApi).apiSampleSampcategoryChartGet('',new Date(new Date(now.value.getTime() - 7 * 24 * 60 * 60 * 1000).setHours(0, 0, 0, 0)),new Date(),'','');
	let chartData = data.data.result;
    state.isShowDialog = true;
    nextTick(() => {
        generateChart(chartData);
    });



};

const generateChart = (chartData: any) => {
    const chart = echarts.init(chartRef.value);
    state.chartOption = {
        title: chartData?.title,
        tooltip: {
            trigger: 'axis'
        },
        legend: chartData?.legend,
        grid: {
            top: '20%',
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
        xAxis: chartData?.xAxis,
        yAxis: {
            type: 'value'
        },
        series: chartData?.series,
    }
    chart.setOption(state.chartOption);
    
}


// Cancel
const cancel = () => {
	state.isShowDialog = false;
};

// Export object
defineExpose({ openDialog });
</script>


<style scoped>
.chart {
  width: 100%;
  height: 400px;
}
</style>