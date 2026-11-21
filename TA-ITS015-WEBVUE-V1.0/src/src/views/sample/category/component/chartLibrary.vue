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
                <v-chart :option="state.chartOption" autoresize style="width: 100%; height: 500px;" />

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
import { reactive, ref } from 'vue';
import { getAPI } from '/@/utils/axios-utils';
import { SampCategoryApi } from '/@/api-services/sample';
import { useI18n } from 'vue-i18n';
const { t: $t } = useI18n();
import { useNow } from '@vueuse/core'; // Import useNow

const now = useNow();

//chart

import { use } from 'echarts/core';
import VChart from 'vue-echarts';
import { LineChart } from 'echarts/charts';
import { TitleComponent, TooltipComponent,LegendComponent,GridComponent,DatasetComponent } from 'echarts/components';
import { CanvasRenderer } from 'echarts/renderers';
use([TitleComponent,LegendComponent, TooltipComponent,LineChart, CanvasRenderer,GridComponent,DatasetComponent]);



interface Props {
	/** the type of operation */
	title: string;
}

const props = defineProps<Props>();
const emits = defineEmits(['handleQuery']);

const ruleFormRef = ref();
const state = reactive({
	isShowDialog: false,
	chartData: {} as any,
    chartOption: {} as any
});

// Open dialog
const openDialog = async () => {

    var data = await getAPI(SampCategoryApi).apiSampleSampcategoryChartGet('',new Date(new Date(now.value.getTime() - 7 * 24 * 60 * 60 * 1000).setHours(0, 0, 0, 0)),new Date(),'','');
	var chartData = data.data.result;

    state.chartOption = {
        title: chartData?.title,
        tooltip: {
            trigger: 'axis'
        },
        legend: chartData?.legend,
        grid: {
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
    console.log(state.chartOption);
	state.isShowDialog = true;


};

// Close dialog
const closeDialog = () => {
	emits('handleQuery');
	state.isShowDialog = false;
};

// Cancel
const cancel = () => {
	state.isShowDialog = false;
};


// Export object
defineExpose({ openDialog });
</script>
