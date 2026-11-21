<template>
	<WorkspacePage>
	<div class="telemetry-container">
		<!-- SearchBar: begin -->
		<GridSearchPanel id="vms-telemetry-search">
				<template #filters>
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true" label-width="auto" style="flex: 1 1 0%" label-position="left">
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.code')" prop="equipmentCode">
							<el-input v-model="state.queryParams.equipmentCode" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.code') })" @keyup.enter.native="handleQuery" :maxlength="EntityFieldLength.Length64" show-word-limit clearable class="w100" />
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.tms.equipmentType')" prop="equipmentTypeId">
							<el-select v-model="state.queryParams.equipmentTypeId" filterable clearable class="w100" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.tms.equipmentType') })">
								<el-option v-for="item in activeTypeList" :key="item.id" :label="item.name" :value="item.id" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.label.vmsDeviceStatus.source')" prop="source">
							<el-select v-model="state.queryParams.source" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.label.vmsDeviceStatus.source') })" clearable class="w100">
								<el-option v-for="item in sourceOptions" :key="item.code" :label="item.name" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.status')" prop="succeeded">
							<el-select v-model="state.queryParams.succeeded" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.base.status') })" clearable class="w100">
								<el-option :label="$t('lz.label.vmsDeviceStatus.succeeded')" :value="true" />
								<el-option :label="$t('lz.label.vmsDeviceStatus.failed')" :value="false" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="24" :md="16" :lg="12" :xl="12">
						<el-form-item :label="$t('lz.entity.vmsDeviceStatus.recordTime')" prop="dateRange">
							<el-date-picker
								v-model="state.dateRange"
								type="datetimerange"
								value-format="YYYY-MM-DD HH:mm:ss"
								range-separator="-"
								:start-placeholder="$t('lz.label.base.startTime')"
								:end-placeholder="$t('lz.label.base.endTime')"
								class="w100"
							/>
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>
			<el-divider style="height: calc(100% - 5px); margin: 0 10px" direction="vertical" />
			<el-row>
				<SearchButton authSearch="vmsTelemetry:page" @search="handleQuery" @reset="resetQuery" :loading="options.loading" />
			</el-row>
				</template>
		<!-- SearchBar: end -->

		<!-- GridTable: begin -->
		<el-card class="full-table" shadow="hover">
			<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents" @checkbox-change="selectChangeEvent" @checkbox-all="selectChangeEvent">
				<template #toolbar_buttons>
					<GridSummary :summary="summary" />
				</template>
				<template #toolbar_tools>
					<TableHeader :show-add="false" :show-delete="state.showBatchDelete" :auth-delete="'vmsTelemetry:delete'" @refresh="handleQuery" @custom-column="handleCustom" @delete="handleBatchDelete" />
				</template>
				<template #empty>
					<el-empty :image-size="200" />
				</template>

				<template #row_source="{ row }">
					<el-tag size="small" :type="row.source === 'auto' ? 'info' : 'warning'" effect="plain">
						{{ row.source === 'auto' ? $t('lz.label.vmsDeviceStatus.sourceAuto') : $t('lz.label.vmsDeviceStatus.sourceManual') }}
					</el-tag>
				</template>
				<template #row_resource="{ row }">
					<div class="metric-list">
						<span v-for="(it, i) in getColumnIconItems(row.resourceMetric, 'resourceMetric', $t)" :key="i" class="metric-item" :title="it.title">
							<i class="fa" :class="it.icon" :style="{ color: it.color }"></i>
							<span class="metric-val">{{ it.value }}</span>
						</span>
						<span v-if="!row.resourceMetric" class="metric-empty">-</span>
					</div>
				</template>
				<template #row_environment="{ row }">
					<div class="metric-list">
						<span v-for="(it, i) in getColumnIconItems(row.environmentMetric, 'environmentMetric', $t)" :key="i" class="metric-item" :title="it.title">
							<i class="fa" :class="it.icon" :style="{ color: it.color }"></i>
							<span class="metric-val">{{ it.value }}</span>
						</span>
						<span v-if="!row.environmentMetric" class="metric-empty">-</span>
					</div>
				</template>
				<template #row_diagnostic="{ row }">
					<div class="metric-list">
						<span v-for="(it, i) in getColumnIconItems(row.diagnosticMetric, 'diagnosticMetric', $t)" :key="i" class="metric-item" :title="it.title">
							<i class="fa" :class="it.icon" :style="{ color: it.color }"></i>
							<span class="metric-val">{{ it.value }}</span>
						</span>
						<span v-if="!row.diagnosticMetric" class="metric-empty">-</span>
					</div>
				</template>
				<template #row_succeeded="{ row }">
					<el-tag size="small" :type="row.succeeded ? 'success' : 'danger'" effect="dark">
						{{ row.succeeded ? $t('lz.label.vmsDeviceStatus.succeeded') : $t('lz.label.vmsDeviceStatus.failed') }}
					</el-tag>
				</template>
				<template #row_buttons="{ row }">
					<el-tooltip :content="$t('lz.button.base.view')" placement="top" :auto-close="1000">
						<el-button icon="ele-View" text type="primary" v-auth="'vmsTelemetry:page'" @click="handleView(row)"> </el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.delete')" placement="top" :auto-close="1000">
						<el-button icon="ele-Delete" text type="danger" v-auth="'vmsTelemetry:delete'" @click="handleDelete(row)"> </el-button>
					</el-tooltip>
				</template>
			</vxe-grid>
		</el-card>
		</GridSearchPanel>
		<!-- GridTable: end -->

		<detailTelemetryComp ref="detailRef" :title="$t('lz.label.vmsDeviceStatus.detailTitle')" />
	</div>
	</WorkspacePage>
</template>

<script lang="ts" setup name="vmsTelemetry">
import { computed, onMounted, onActivated, reactive, ref, defineAsyncComponent } from 'vue';
import { useRoute } from 'vue-router';
import { ElMessage, ElMessageBox } from 'element-plus';
import { useI18n } from 'vue-i18n';
import WorkspacePage from '/@/components/workspace/WorkspacePage.vue';
import GridSearchPanel from '/@/components/gridPage/GridSearchPanel.vue';
import GridSummary from '/@/components/gridPage/GridSummary.vue';
import { useGridSummary } from '/@/components/gridPage/useGridSummary';
import { useGridPager } from '/@/components/gridPage/useGridPager';
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { getAPI } from '/@/utils/axios-utils';

import { EquipmentTelemetryApi } from '/@/api-services/vms';
import { PageVmsEquipmentTelemetryInput, PageVmsEquipmentTelemetryOutput } from '/@/api-services/vms';
import { TmsEquipmentTypeApi } from '/@/api-services/tms';
import { EntityFieldLength } from '/@/types/enums/baseEnum';
import { getColumnIconItems } from '/@/views/vms/telemetry/telemetryMetrics';

import detailTelemetryComp from '/@/views/vms/telemetry/component/detailTelemetry.vue';

const { t: $t } = useI18n();
const route = useRoute();

const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));
const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));

const xGrid = ref<VxeGridInstance>();
const { summary, track } = useGridSummary();
const detailRef = ref<InstanceType<typeof detailTelemetryComp>>();

// Nguồn lấy (khớp giá trị BE: auto/manual)
const sourceOptions = [
	{ code: 'auto', name: $t('lz.label.vmsDeviceStatus.sourceAuto') },
	{ code: 'manual', name: $t('lz.label.vmsDeviceStatus.sourceManual') },
];

const state = reactive({
	queryParams: {
		equipmentCode: undefined as any,
		equipmentTypeId: undefined as any,
		source: undefined as any,
		succeeded: undefined as any,
	},
	dateRange: undefined as any, // [start, end] dạng chuỗi 'YYYY-MM-DD HH:mm:ss'
	typeList: [] as any[], // Danh mục loại thiết bị (để hiển thị tên + lọc)
	localPageParam: {
		pageSize: 50 as number,
		defaultSort: { field: 'collectedTime', order: 'desc', descStr: 'desc' } as any,
	},
	showBatchDelete: false,
});

// Loại thiết bị cho bộ lọc: chỉ hiển thị loại chưa bị xóa; typeList gốc vẫn giữ đủ để formatter map tên loại đã xóa
const activeTypeList = computed(() => state.typeList.filter((item) => item.isDelete == null));

// Map ID loại thiết bị -> tên hiển thị
const equipmentTypeName = (id: string) => state.typeList.find((u) => u.id == id)?.name ?? '';

const formatTime = (d: any) => {
	if (!d) return '-';
	const date = new Date(d);
	return isNaN(date.getTime()) ? '-' : date.toLocaleString('vi-VN');
};

const options = useVxeTable<PageVmsEquipmentTelemetryOutput>(
	{
		id: 'vmsTelemetryGrid',
		name: 'vmsTelemetryGrid',
		columns: [
			{ type: 'checkbox', fixed: 'left', width: 45 },
			{ field: 'equipmentCode', title: $t('lz.entity.base.code'), minWidth: 130, showOverflow: 'tooltip', sortable: true },
			{ field: 'equipmentName', title: $t('lz.entity.base.name'), minWidth: 160, showOverflow: 'tooltip', sortable: true },
			{ field: 'equipmentTypeId', title: $t('lz.entity.tms.equipmentType'), minWidth: 90, showOverflow: 'tooltip', formatter: ({ row }: any) => equipmentTypeName(row.equipmentTypeId) },
			{ field: 'resourceMetric', title: $t('lz.label.vmsDeviceStatus.resource'), minWidth: 200, align: 'center', showOverflow: false, className: 'metric-cell', slots: { default: 'row_resource' } },
			{ field: 'environmentMetric', title: $t('lz.label.vmsDeviceStatus.environment'), minWidth: 260, align: 'center', showOverflow: false, className: 'metric-cell', slots: { default: 'row_environment' } },
			{ field: 'diagnosticMetric', title: $t('lz.label.vmsDeviceStatus.diagnostic'), minWidth: 220, align: 'center', showOverflow: false, className: 'metric-cell', slots: { default: 'row_diagnostic' } },
			{ field: 'source', title: $t('lz.label.vmsDeviceStatus.source'), width: 100, slots: { default: 'row_source' } },
			{ field: 'collectedTime', title: $t('lz.entity.vmsDeviceStatus.recordTime'), width: 160, sortable: true, formatter: ({ row }: any) => formatTime(row.collectedTime) },
			{ field: 'succeeded', title: $t('lz.entity.base.status'), width: 100, slots: { default: 'row_succeeded' } },
			{ field: 'message', title: $t('lz.label.vmsDeviceStatus.message'), minWidth: 160, showOverflow: 'tooltip' },
			{ title: $t('lz.label.base.action'), fixed: 'right', width: 100, slots: { default: 'row_buttons' } },
		],
	},
	{
		proxyConfig: { autoLoad: true, ajax: { query: ({ page, sort }) => handleQueryApi(page, sort) } },
		sortConfig: { defaultSort: state.localPageParam.defaultSort },
		pagerConfig: { pageSize: state.localPageParam.pageSize },
		toolbarConfig: { export: false },
	}
);

// Thu gọn thanh phân trang theo chiều rộng bảng.
useGridPager(xGrid, options);

// Quay lại tab/menu (KeepAlive): tải lại trang hiện tại để nhận thay đổi từ tab khác.
// onActivated cũng chạy ngay sau lần mount đầu, khi đó dữ liệu vừa tải nên bỏ qua.
let activated = false;
onActivated(async () => {
	if (!activated) {
		activated = true;
		return;
	}
	await xGrid.value?.commitProxy('query');
});

onMounted(async () => {
	// Lấy danh mục loại thiết bị (bao gồm cả loại đã xóa để formatter map được tên)
	const resType = await getAPI(TmsEquipmentTypeApi).apiTmsTmsequipmenttypeListGet(undefined, undefined, undefined, undefined, undefined, true as any);
	state.typeList = resType.data.result ?? [];

	// Điều hướng từ tab trạng thái thiết bị -> lọc sẵn theo mã thiết bị
	if (route.query.equipmentCode) {
		state.queryParams.equipmentCode = String(route.query.equipmentCode);
	}
	await handleQuery();
});

const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
	const params = {
		equipmentCode: state.queryParams.equipmentCode,
		equipmentTypeId: state.queryParams.equipmentTypeId,
		source: state.queryParams.source,
		succeeded: state.queryParams.succeeded,
		fromDate: state.dateRange?.[0],
		toDate: state.dateRange?.[1],
		page: page.currentPage,
		pageSize: page.pageSize,
		field: sort.field,
		order: sort.order,
		descStr: 'desc',
	} as PageVmsEquipmentTelemetryInput;

	return track(getAPI(EquipmentTelemetryApi).apiVmsEquipmenttelemetryPagePost(params));
};

const handleQuery = async () => {
	options.loading = true;
	await xGrid.value?.commitProxy('reload');
	options.loading = false;
	selectChangeEvent();
};

const resetQuery = async () => {
	state.queryParams.equipmentCode = undefined;
	state.queryParams.equipmentTypeId = undefined;
	state.queryParams.source = undefined;
	state.queryParams.succeeded = undefined;
	state.dateRange = undefined;
	await xGrid.value?.commitProxy('reload');
};

const handleView = (row: any) => {
	// Kèm sẵn tên loại thiết bị để dialog chi tiết hiển thị không cần gọi lại API
	detailRef.value?.openDialog({ ...row, equipmentTypeName: equipmentTypeName(row.equipmentTypeId) });
};

const handleDelete = (row: any) => {
	ElMessageBox.confirm($t('lz.message.base.confirmDelete', { name: row.equipmentCode ?? '' }), $t('lz.label.base.confirm'), {
		confirmButtonText: $t('lz.button.base.confirm'),
		cancelButtonText: $t('lz.button.base.cancel'),
		type: 'warning',
	})
		.then(async () => {
			try {
				await getAPI(EquipmentTelemetryApi).apiVmsEquipmenttelemetryDeletePost({ id: row.id });
				handleQuery();
				ElMessage.success($t('lz.message.base.processSuccess'));
			} catch (error) {
				ElMessage.error($t('lz.message.base.processUnSuccess'));
			}
		})
		.catch(() => {});
};

function handleCustom() {
	xGrid.value?.openCustom();
}

function getCheckedRecords() {
	return xGrid.value?.getCheckboxRecords() ?? [];
}

function selectChangeEvent() {
	state.showBatchDelete = getCheckedRecords().length > 0;
}

const handleBatchDelete = async () => {
	const selectRecords = getCheckedRecords();
	if (!selectRecords.length) return;

	ElMessageBox.confirm($t('lz.message.base.confirmBatchDelete', { total: selectRecords.length }), $t('lz.label.base.confirm'), {
		confirmButtonText: $t('lz.button.base.confirm'),
		cancelButtonText: $t('lz.button.base.cancel'),
		type: 'warning',
	})
		.then(async () => {
			try {
				// Chưa có API batch delete telemetry -> xóa lần lượt từng bản ghi
				await Promise.all(selectRecords.filter((l: any) => l.id).map((l: any) => getAPI(EquipmentTelemetryApi).apiVmsEquipmenttelemetryDeletePost({ id: l.id })));
				handleQuery();
				ElMessage.success($t('lz.message.base.processSuccess'));
			} catch (error) {
				ElMessage.error($t('lz.message.base.processUnSuccess'));
			}
		})
		.catch(() => {});
};

const gridEvents: VxeGridListeners<PageVmsEquipmentTelemetryOutput> = {
	async pageChange({ pageSize }) {
		state.localPageParam.pageSize = pageSize;
	},
	async sortChange({ field, order }) {
		state.localPageParam.defaultSort = { field: field, order: order! };
	},
};
</script>

<style scoped>
.telemetry-container {
	width: 100%;
	height: 100%;
}

/* 3 cột metric: hiển thị dạng icon + giá trị, tự xuống dòng khi nhiều thông số */
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

/* Bỏ ràng buộc 1 dòng của vxe-cell để dòng tự giãn chiều cao theo nội dung */
:deep(.metric-cell .vxe-cell) {
	white-space: normal !important;
	overflow: visible;
	text-overflow: clip;
	max-height: none !important;
}
</style>