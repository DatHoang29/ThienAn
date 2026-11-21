<template>
	<section class="traffic-overview" :aria-label="$t('lz.label.tms.eventsIncidentsChart')">
		<header class="chart-toolbar">
			<div class="chart-heading">
				<el-icon><DataLine /></el-icon>
				<h2>{{ $t('lz.label.base.statistic') }}</h2>
			</div>
			<dl v-if="count && !countError" class="overview-counts" :aria-label="$t('lz.label.tms.eventsIncidentsStatistics')">
				<div>
					<dt>{{ $t('lz.label.base.total') }}</dt>
					<dd>{{ count.total ?? 0 }}</dd>
				</div>
				<div class="warning">
					<dt>{{ $t('lz.label.base.unprocessed') }}</dt>
					<dd>{{ count._new ?? count.new ?? 0 }}</dd>
				</div>
				<div class="success">
					<dt>{{ $t('lz.label.base.valid') }}</dt>
					<dd>{{ count.valid ?? 0 }}</dd>
				</div>
				<div class="danger">
					<dt>{{ $t('lz.label.base.invalid') }}</dt>
					<dd>{{ count.inValid ?? 0 }}</dd>
				</div>
			</dl>
			<span v-else class="chart-count-error" :role="countError ? 'alert' : undefined">
				{{ countError || (loading ? $t('lz.label.base.loading') : $t('lz.label.base.noStatistics')) }}
				<el-button v-if="countError" text size="small" @click="refresh(true)">
					{{ $t('lz.button.base.refresh') }}
				</el-button>
			</span>
			<div class="chart-controls">
				<el-radio-group v-if="expanded" v-model="chartDataType" size="small" :aria-label="$t('lz.label.base.chartDataType')" @change="refresh(false)">
					<el-radio-button value="day">{{ $t('lz.label.base.day') }}</el-radio-button>
					<el-radio-button value="time">{{ $t('lz.label.base.timeRange') }}</el-radio-button>
				</el-radio-group>
				<el-checkbox v-if="expanded" v-model="showLegend" size="small">
					{{ $t('lz.label.base.showChartLegend') }}
				</el-checkbox>
				<el-button text size="small" :icon="expanded ? ArrowUp : ArrowDown" :aria-expanded="expanded" aria-controls="traffic-chart-body" @click="expanded = !expanded">
					{{ expanded ? $t('lz.button.base.collapse') : $t('lz.button.base.chart') }}
				</el-button>
			</div>
		</header>
		<div v-if="expanded" id="traffic-chart-body" v-loading="loading" class="chart-body" :aria-busy="loading">
			<span class="chart-date">{{ dateCaption }}</span>
			<div class="chart-plot">
				<div v-if="chartError || !hasData" class="chart-empty" :role="chartError ? 'alert' : undefined">
					<span>
						{{ chartError || (loading ? $t('lz.label.base.loading') : $t('lz.label.base.noDataInTimeRange')) }}
					</span>
					<el-button v-if="chartError" text :icon="Refresh" @click="refresh(true)">
						{{ $t('lz.button.base.retry') }}
					</el-button>
				</div>
				<IncidentLineChart
					v-else
					ref="lineChart"
					:chart-data="displayChart"
					:data-changed-count="dataChangedCount"
					:show-legend="showLegend"
					legend-orient="vertical"
					@on-chart-mounted="onChartMounted"
				/>
			</div>
		</div>
	</section>
</template>

<script lang="ts" setup name="tmsIncidentOverviewChart">
import { computed, nextTick, onBeforeUnmount, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useDateFormat } from '@vueuse/core';
import { DateTimeFormatEnum } from '/@/types/enums/baseEnum';
import { DataLine, ArrowUp, ArrowDown, Refresh } from '@element-plus/icons-vue';
import type { IncidentChartOutput, IncidentCountOutput } from '/@/api-services/tms';
import { useTmsStore } from '/@/stores/tmsStore';
import IncidentLineChart from '/@/views/tms/incident/component/lineChart.vue';
import { incidentQuery } from '../incidentModel';
import type { IncidentFilters } from '../incidentModel';

interface Props {
	filters: IncidentFilters;
	active: boolean;
}

const props = defineProps<Props>();
const { t: $t } = useI18n();
const tmsStore = useTmsStore();
const expanded = ref(false);
const chartDataType = ref('day');
const showLegend = ref(true);
const lineChart = ref<InstanceType<typeof IncidentLineChart>>();
const dataChangedCount = ref(0);
const loading = ref(false);
const chartErrorKey = ref('');
const chartError = computed(() => (chartErrorKey.value ? $t(chartErrorKey.value) : ''));
const countErrorKey = ref('');
const countError = computed(() => (countErrorKey.value ? $t(countErrorKey.value) : ''));
const chart = ref<IncidentChartOutput | null>(null);
const displayChart = computed(() => (chart.value ? { ...chart.value, title: { ...chart.value.title, show: false } } : null));
const count = ref<(IncidentCountOutput & { new?: number }) | null>(null);
const hasData = computed(() => chart.value?.series?.some((series) => series?.data?.length));
const dateCaption = computed(() => props.filters.dateRange?.map((date) => useDateFormat(date, DateTimeFormatEnum.DateDefault).value).join(' – ') || $t('lz.label.base.allTime'));
let requestId = 0;

async function onChartMounted() {
	await nextTick();
	lineChart.value?.generateChart();
}

async function refresh(force = true) {
	if (!props.active) return;
	const request = ++requestId;
	loading.value = false;
	chartErrorKey.value = '';
	countErrorKey.value = '';
	chart.value = null;
	count.value = null;
	const range = props.filters.dateRange;
	if (range && (!range.every((date) => date instanceof Date && Number.isFinite(date.getTime())) || range[0] > range[1])) {
		chartErrorKey.value = 'lz.validation.base.timeRange';
		return;
	}
	loading.value = true;
	// Same filters, group flags, page size and chart mode as incident/getIncidentChart/getIncidentCount.
	const params = { ...incidentQuery(props.filters, 1, 1000000), chartDataType: chartDataType.value };
	const countParams = incidentQuery(props.filters, 1, 1000000);
	const results = await Promise.allSettled([expanded.value ? tmsStore.getIncidentChart(params, force) : Promise.resolve(null), tmsStore.getIncidentCount(countParams, force)]);
	if (request !== requestId) return;
	const [chartResult, countResult] = results;
	if (chartResult.status === 'fulfilled' && chartResult.value) {
		chart.value = chartResult.value;
		dataChangedCount.value++;
	} else if (expanded.value) chartErrorKey.value = 'lz.message.base.chartLoadFailed';
	if (countResult.status === 'fulfilled') count.value = countResult.value;
	else countErrorKey.value = 'lz.message.base.statisticsLoadFailed';
	loading.value = false;
}

watch(
	() => [props.filters, props.active, expanded.value, tmsStore.incidentRevision],
	() => {
		if (!props.active) {
			++requestId;
			loading.value = false;
			return;
		}
		void refresh(false);
	},
	{ immediate: true }
);
onBeforeUnmount(() => {
	++requestId;
});
defineExpose({ refresh });
</script>
