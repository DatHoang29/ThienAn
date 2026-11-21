<!--
  Dialog "Chi tiết & Lịch sử telemetry" của MỘT thiết bị (FmsEquipmentTelemetry).
  - Header: thẻ tóm tắt thiết bị (tên, mã, loại, hãng, trạng thái, mốc thời gian).
  - Tab "Hiện tại": thông số mới nhất theo nhóm (thanh tiến trình cho %, uptime đã định dạng).
  - Tab "Lịch sử": lưới phân trang + lọc từ ngày -> đến ngày + nguồn lấy.
  Author: ChuongLP
-->
<template>
	<el-dialog v-model="state.isShowDialog" draggable :close-on-click-modal="false" width="1380px" class="telemetry-detail-dialog">
		<template #header>
			<div style="color: #fff">
				<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"><ele-View /></el-icon>
				<span> {{ props.title }} </span>
			</div>
		</template>

		<!-- ===== Thẻ tóm tắt thiết bị ===== -->
		<div class="dev-summary" :class="`is-${conn.state}`">
			<div class="sum-main">
				<div class="sum-line1">
					<span class="sum-name">{{ state.row.equipmentName || state.row.equipmentCode || '-' }}</span>
					<el-tag size="small" effect="plain" type="info">{{ state.row.equipmentTypeName || '-' }}</el-tag>
					<span class="sum-status" :class="`status-${conn.state}`"><span class="dot"></span>{{ statusText }}</span>
				</div>
				<div class="sum-line2">
					<span v-if="state.row.vendor"><i class="fa fa-industry"></i> {{ state.row.vendor }}</span>
					<span :class="{ 'time-danger': conn.isStale }">
						<i class="fa fa-clock-o"></i>
						<template v-if="conn.state === 'offline'">{{ $t('lz.label.vmsDeviceStatus.disconnectedFrom', { time: formatTime(conn.since) }) }}</template>
						<template v-else>{{ formatTime(state.row.collectedTime) }}</template>
					</span>
				</div>
			</div>
		</div>

		<el-alert v-if="state.row.message" :title="state.row.message" type="error" :closable="false" show-icon class="mb5" style="margin-top: 8px" />

		<!-- ===== Tabs ===== -->
		<el-tabs v-model="state.activeTab" class="detail-tabs" style="margin-top: 8px">
			<!-- Tab hiện tại -->
			<el-tab-pane :label="$t('lz.label.vmsDeviceStatus.currentStatus')" name="current">
				<div v-if="groups.length" class="metric-groups">
					<div v-for="group in groups" :key="group.col" class="metric-group">
						<div class="group-title">{{ $t(group.labelKey) }}</div>
						<div class="group-items">
							<div v-for="(item, idx) in group.items" :key="idx" class="g-item">
								<template v-if="item.percent !== null">
									<div class="g-line">
										<span class="g-label">{{ item.label }}</span>
										<span class="g-val" :style="{ color: item.color }">{{ item.value }}</span>
									</div>
									<el-progress :percentage="clampPercent(item.percent)" :color="item.color" :show-text="false" :stroke-width="6" />
								</template>
								<template v-else>
									<div class="g-line">
										<span class="g-label">{{ item.label }}</span>
										<span class="g-val plain">{{ item.value }}</span>
									</div>
								</template>
							</div>
						</div>
					</div>
				</div>
				<el-empty v-else :image-size="80" :description="$t('lz.label.base.noData')" />
			</el-tab-pane>

			<!-- Tab lịch sử -->
			<el-tab-pane :label="$t('lz.label.vmsDeviceStatus.historyList')" name="history">
				<el-form :model="state.queryParams" ref="queryForm" :inline="true" label-width="auto" label-position="left" class="mb5">
					<el-form-item :label="$t('lz.entity.vmsDeviceStatus.recordTime')">
						<el-date-picker
							v-model="state.dateRange"
							type="datetimerange"
							value-format="YYYY-MM-DDTHH:mm:ss"
							range-separator="-"
							:start-placeholder="$t('lz.label.base.startTime')"
							:end-placeholder="$t('lz.label.base.endTime')"
							style="width: 600px"
						/>
					</el-form-item>
					<el-form-item :label="$t('lz.label.vmsDeviceStatus.source')">
						<el-select v-model="state.queryParams.source" clearable style="width: 140px" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.label.vmsDeviceStatus.source') })">
							<el-option v-for="item in sourceOptions" :key="item.code" :label="item.name" :value="item.code" />
						</el-select>
					</el-form-item>
					<el-form-item>
						<el-button type="primary" icon="ele-Search" @click="handleQuery">{{ $t('lz.button.base.search') }}</el-button>
						<el-button icon="ele-Refresh" @click="resetQuery">{{ $t('lz.button.base.reset') }}</el-button>
					</el-form-item>
				</el-form>

				<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
					<template #empty>
						<el-empty :image-size="120" />
					</template>
					<template #row_cpu="{ row }">
						<span :style="{ color: usageColor(getMetricPercent(row.resourceMetric, 'cpuUsedPercent')) }">{{ formatMetricByKey(row.resourceMetric, 'cpuUsedPercent', $t) }}</span>
					</template>
					<template #row_ram="{ row }">
						<span :style="{ color: usageColor(getMetricPercent(row.resourceMetric, 'memoryUsedPercent')) }">{{ formatMetricByKey(row.resourceMetric, 'memoryUsedPercent', $t) }}</span>
					</template>
					<template #row_disk="{ row }">
						<span :style="{ color: usageColor(getMetricPercent(row.resourceMetric, 'diskUsedPercent')) }">{{ formatMetricByKey(row.resourceMetric, 'diskUsedPercent', $t) }}</span>
					</template>
					<template #row_source="{ row }">
						<el-tag size="small" :type="row.source === 'auto' ? 'info' : 'warning'" effect="plain">
							{{ row.source === 'auto' ? $t('lz.label.vmsDeviceStatus.sourceAuto') : $t('lz.label.vmsDeviceStatus.sourceManual') }}
						</el-tag>
					</template>
					<template #row_succeeded="{ row }">
						<el-tag size="small" :type="row.succeeded ? 'success' : 'danger'" effect="dark">
							{{ row.succeeded ? $t('lz.label.vmsDeviceStatus.succeeded') : $t('lz.label.vmsDeviceStatus.failed') }}
						</el-tag>
					</template>
				</vxe-grid>
			</el-tab-pane>
		</el-tabs>

		<template #footer>
			<span class="dialog-footer">
				<el-button @click="cancel">{{ $t('lz.button.base.close') }}</el-button>
			</span>
		</template>
	</el-dialog>
</template>

<script lang="ts" setup name="fmsDeviceTelemetryHistory">
import { reactive, computed, ref, nextTick } from 'vue';
import { useI18n } from 'vue-i18n';
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { getAPI } from '/@/utils/axios-utils';
import { useCommonVarsAndFuncs } from '/@/stores/its/commonVarsAndFuncs';
import { FmsEquipmentTelemetryApi } from '/@/api-services/fms';
import { PageEquipmentTelemetryInput, PageEquipmentTelemetryOutput } from '/@/api-services/fms';
import { METRIC_COLUMNS, getCardMetricItems, getConnState, usageColor, getMetricPercent, formatMetricByKey, formatColumnList } from '/@/views/fms/telemetry/telemetryMetrics';

const { t: $t } = useI18n();
const commonVarsAndFuncs = useCommonVarsAndFuncs();

interface Props {
	title: string;
}
const props = defineProps<Props>();

const xGrid = ref<VxeGridInstance>();

const sourceOptions = [
	{ code: 'auto', name: $t('lz.label.vmsDeviceStatus.sourceAuto') },
	{ code: 'manual', name: $t('lz.label.vmsDeviceStatus.sourceManual') },
];

const state = reactive({
	isShowDialog: false,
	activeTab: 'current' as string,
	row: {} as any, // dòng thiết bị + telemetry mới nhất (truyền từ danh sách)
	typeIcon: '' as string,
	dateRange: undefined as any,
	queryParams: {
		source: undefined as any,
	},
});

const conn = computed(() => getConnState(state.row));

const typeIconUrl = computed(() => (state.typeIcon ? commonVarsAndFuncs.getFullUrl(state.typeIcon) : ''));

const statusText = computed(() => {
	switch (conn.value.state) {
		case 'offline':
			return $t('lz.label.vmsDeviceStatus.lostSignal');
		case 'nodata':
			return $t('lz.label.vmsDeviceStatus.noData');
		default:
			return $t('lz.label.vmsDeviceStatus.online');
	}
});

// Gom telemetry mới nhất thành các nhóm (chỉ nhóm có field dữ liệu), dùng item dạng thẻ (kèm % + uptime).
const groups = computed(() =>
	METRIC_COLUMNS.map((c) => ({
		col: c.col,
		labelKey: c.labelKey,
		items: getCardMetricItems(state.row?.[c.col], c.col, $t),
	})).filter((g) => g.items.length)
);

const clampPercent = (p: number | null) => {
	if (p === null) return 0;
	return Math.max(0, Math.min(100, Math.round(p * 10) / 10));
};

const formatTime = (d?: string | Date | null) => {
	if (!d) return '-';
	const date = new Date(d);
	return isNaN(date.getTime()) ? '-' : date.toLocaleString('vi-VN');
};

const options = useVxeTable<PageEquipmentTelemetryOutput>(
	{
		id: 'fmsTelemetryHistoryGrid',
		name: 'fmsTelemetryHistoryGrid',
		columns: [
			{ field: 'collectedTime', title: $t('lz.entity.vmsDeviceStatus.recordTime'), width: 150, sortable: true, formatter: ({ row }: any) => formatTime(row.collectedTime) },
			{ field: 'source', title: $t('lz.label.vmsDeviceStatus.source'), width: 90, slots: { default: 'row_source' } },
			{ field: 'cpu', title: 'CPU', width: 80, align: 'center', slots: { default: 'row_cpu' } },
			{ field: 'ram', title: 'RAM', width: 80, align: 'center', slots: { default: 'row_ram' } },
			{ field: 'disk', title: 'Disk', width: 80, align: 'center', slots: { default: 'row_disk' } },
			{ field: 'uptime', title: $t('lz.entity.vmsDeviceStatus.uptime'), minWidth: 150, showOverflow: 'tooltip', formatter: ({ row }: any) => formatMetricByKey(row.diagnosticMetric, 'uptime', $t) },
			{ field: 'environmentMetric', title: $t('lz.label.vmsDeviceStatus.environment'), minWidth: 180, showOverflow: 'tooltip', formatter: ({ row }: any) => formatColumnList(row.environmentMetric, 'environmentMetric', $t) },
			{ field: 'diagnosticMetric', title: $t('lz.label.vmsDeviceStatus.diagnostic'), minWidth: 220, showOverflow: 'tooltip', formatter: ({ row }: any) => formatColumnList(row.diagnosticMetric, 'diagnosticMetric', $t, { excludeKeys: ['uptime'] }) },
			{ field: 'succeeded', title: $t('lz.entity.base.status'), width: 100, slots: { default: 'row_succeeded' } },
			{ field: 'message', title: $t('lz.label.vmsDeviceStatus.message'), minWidth: 140, showOverflow: 'tooltip' },
		],
	},
	{
		height: 380,
		proxyConfig: { autoLoad: false, ajax: { query: ({ page, sort }) => handleQueryApi(page, sort) } },
		sortConfig: { defaultSort: { field: 'collectedTime', order: 'desc' } },
		pagerConfig: { pageSize: 20, pageSizes: [20, 50, 100] },
		toolbarConfig: { export: false },
	}
);

const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
	const params = {
		equipmentId: state.row.equipmentId,
		source: state.queryParams.source,
		fromDate: state.dateRange?.[0],
		toDate: state.dateRange?.[1],
		page: page.currentPage,
		pageSize: page.pageSize,
		field: sort.field,
		order: sort.order,
		descStr: 'desc',
	} as PageEquipmentTelemetryInput;

	return await getAPI(FmsEquipmentTelemetryApi).apiFmsFmsequipmenttelemetryPagePost(params);
};

const handleQuery = async () => {
	await xGrid.value?.commitProxy('reload');
	await syncGridLayout();
};

const resetQuery = async () => {
	state.dateRange = undefined;
	state.queryParams.source = undefined;
	await xGrid.value?.commitProxy('reload');
	await syncGridLayout();
};

// Ô metric xuống dòng (cao động) -> đồng bộ lại chiều cao sau khi render để lưới không lệch.
const syncGridLayout = async () => {
	await nextTick();
	await nextTick();
	await xGrid.value?.recalculate(true);
};

const gridEvents: VxeGridListeners<PageEquipmentTelemetryOutput> = {
	async pageChange() {
		await syncGridLayout();
	},
	async sortChange() {
		await syncGridLayout();
	},
};

const openDialog = async (row: any) => {
	state.row = JSON.parse(JSON.stringify(row ?? {}));
	state.typeIcon = row?.equipmentTypeIcon ?? '';
	state.activeTab = 'current';
	state.dateRange = undefined;
	state.queryParams.source = undefined;
	state.isShowDialog = true;
	// Chờ dialog render xong grid rồi mới nạp lịch sử theo equipmentId.
	await nextTick();
	await xGrid.value?.commitProxy('reload');
	await syncGridLayout();
};

const cancel = () => {
	state.isShowDialog = false;
};

defineExpose({ openDialog });
</script>

<style scoped>
/* ===== Thẻ tóm tắt ===== */
.dev-summary {
	display: flex;
	align-items: center;
	gap: 12px;
	padding: 12px 14px;
	border: 1px solid var(--el-border-color-light);
	border-radius: 8px;
	background: var(--el-fill-color-lighter);
}
.dev-summary.is-offline {
	border-color: var(--el-color-danger-light-5);
	background: var(--el-color-danger-light-9);
}
.sum-icon {
	width: 44px;
	height: 44px;
	border-radius: 8px;
	display: flex;
	align-items: center;
	justify-content: center;
	background: var(--el-bg-color);
	color: var(--el-color-primary);
	font-size: 22px;
	overflow: hidden;
	flex: 0 0 auto;
}
.sum-icon img {
	width: 100%;
	height: 100%;
	object-fit: contain;
}
.sum-icon.icon-offline {
	color: var(--el-color-danger);
}
.sum-main {
	flex: 1 1 auto;
	min-width: 0;
}
.sum-line1 {
	display: flex;
	align-items: center;
	gap: 8px;
}
.sum-name {
	font-size: 16px;
	font-weight: 700;
	color: var(--el-text-color-primary);
}
.sum-status {
	display: inline-flex;
	align-items: center;
	gap: 5px;
	font-size: 12px;
	color: var(--el-color-success);
	margin-left: auto;
}
.sum-status .dot {
	width: 7px;
	height: 7px;
	border-radius: 50%;
	background: currentColor;
}
.sum-status.status-offline {
	color: var(--el-color-danger);
}
.sum-status.status-nodata {
	color: var(--el-text-color-secondary);
}
.sum-line2 {
	display: flex;
	flex-wrap: wrap;
	gap: 16px;
	margin-top: 6px;
	font-size: 12px;
	color: var(--el-text-color-secondary);
}
.sum-line2 .fa {
	margin-right: 4px;
}
.time-danger {
	color: var(--el-color-danger);
	font-weight: 600;
}

/* ===== Nhóm thông số (tab hiện tại) ===== */
.metric-groups {
	display: grid;
	grid-template-columns: repeat(3, minmax(0, 1fr));
	gap: 14px;
}
@media (max-width: 820px) {
	.metric-groups {
		grid-template-columns: 1fr;
	}
}
.metric-group {
	border: 1px solid var(--el-border-color-lighter);
	border-radius: 8px;
	padding: 10px 12px;
}
.group-title {
	font-weight: 600;
	font-size: 13px;
	color: var(--el-color-primary);
	margin-bottom: 8px;
	padding-bottom: 6px;
	border-bottom: 1px solid var(--el-border-color-lighter);
}
.g-item {
	margin-bottom: 8px;
}
.g-line {
	display: flex;
	align-items: center;
	justify-content: space-between;
	font-size: 13px;
	margin-bottom: 3px;
}
.g-label {
	color: var(--el-text-color-regular);
}
.g-label .fa {
	margin-right: 4px;
	color: var(--el-text-color-secondary);
}
.g-val {
	font-weight: 600;
}
.g-val.plain {
	color: var(--el-text-color-primary);
	font-weight: 500;
}

/* ===== Lưới lịch sử ===== */
.metric-list {
	display: flex;
	flex-wrap: wrap;
	gap: 4px 12px;
	padding: 4px 0;
	width: 100%;
	line-height: 1.6;
}
.metric-item {
	display: inline-flex;
	align-items: center;
	gap: 4px;
	white-space: nowrap;
}
.metric-item .fa {
	font-size: 13px;
}
.metric-val {
	color: var(--el-text-color-regular);
}
.metric-empty {
	color: var(--el-text-color-secondary);
}
:deep(.metric-cell .vxe-cell) {
	white-space: normal !important;
	overflow: visible;
	text-overflow: clip;
	max-height: none !important;
}
</style>
