<template>
	<!-- KienLT - 09/11/2026: tab "Theo lượt" của menu nhóm tabPage (Lịch sử lọc phương tiện); tab "Theo chuyến" ở trip/index.vue.
	     Bản trước khi tách tab lưu tại views/tms/old/vehicleFilterHistory. -->
	<WorkspacePage>
	<div class="vehicle-filter-history-container">
		<GridSearchPanel id="tms-vehicle-filter-history-search">
			<template #filters>
				<VehicleFilterSearch :filter="filter" :loading="!!options.loading" @search="handleQuery" @reset="resetQuery" />
			</template>

			<el-card class="full-table" shadow="hover">
				<!-- Mỗi dòng là một lần camera bắt được xe khớp danh sách -->
				<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents" @checkbox-change="selectChangeEvent" @checkbox-all="selectChangeEvent">
					<template #empty>
						<el-empty :image-size="200" />
					</template>
					<template #row_detectTime="{ row }">
						{{ filter.formatUtcTime(row.detectTime) }}
					</template>
					<template #row_equipment="{ row }">
						{{ row.equipmentId ? filter.cameraLabelById(row.equipmentId) : '—' }}
					</template>
					<template #row_locationId="{ row }">
						{{ filter.findName(filter.lists.locationList, row.locationId) }}
					</template>
					<template #row_direction="{ row }">
						{{ filter.findName(filter.lists.directionList, row.direction) }}
					</template>
					<template #row_type="{ row }">
						{{ filter.findName(filter.lists.filterTypeList, row.type) }}
					</template>
					<template #row_laneId="{ row }">
						{{ filter.findName(filter.lists.laneList, row.laneId) }}
					</template>
					<template #row_record="{ row }">
						<ModifyRecord :data="row" />
					</template>
					<template #row_buttons>
						<el-tooltip :content="$t('lz.button.base.delete')" placement="top" :auto-close="1000">
							<el-button icon="ele-Delete" text type="danger" v-auth="'tmsVehicleFilterHistory:delete'"> </el-button>
						</el-tooltip>
					</template>
					<template #header_name>
						{{ $t('lz.label.tms.tmsVehicleFilterHistory') }}
					</template>
				</vxe-grid>
			</el-card>
		</GridSearchPanel>
	</div>
	</WorkspacePage>
</template>

<script lang="ts" setup name="tmsVehicleFilterHistory">
	import { onMounted, reactive, ref } from 'vue';
	import { useI18n } from 'vue-i18n';
	import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
	import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
	import ModifyRecord from '/@/components/table/modifyRecord.vue';
	import WorkspacePage from '/@/components/workspace/WorkspacePage.vue';
	import GridSearchPanel from '/@/components/gridPage/GridSearchPanel.vue';
	import { useGridPager } from '/@/components/gridPage/useGridPager';
	import { getVehicleFilterHistory, type VehicleFilterHistoryInput, type VehicleFilterHistoryOutput } from '/@/services/tms/traffic-status';
	import VehicleFilterSearch from './component/vehicleFilterSearch.vue';
	import { useVehicleFilterHistory } from './component/useVehicleFilterHistory';

	const { t: $t } = useI18n();
	const filter = useVehicleFilterHistory();
	const xGrid = ref<VxeGridInstance>();

	const state = reactive({
		localPageParam: {
			pageSize: 50 as number,
			defaultSort: { field: 'detectTime', order: 'desc', descStr: 'desc' } as any,
		},
		showBatchDelete: false,
	});

	const options = useVxeTable<VehicleFilterHistoryOutput>(
		{
			id: 'tmsVehicleFilterHistoryGrid',
			name: 'tmsVehicleFilterHistory',
			columns: [
				{ type: 'checkbox', fixed: 'left', width: 45 },
				{ field: 'licensePlate', title: $t('lz.entity.tms.licensePlate'), minWidth: 100, showOverflow: 'tooltip', sortable: true },
				{ field: 'type', title: $t('lz.entity.base.type'), minWidth: 100, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_type' }},
				{ field: 'detectTime', title: $t('lz.entity.tms.detectTime'), minWidth: 100, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_detectTime' }},
				// Camera bắt được xe; dòng trước 23/10/2026 không có equipmentId nên hiện "—".
				{ field: 'equipmentId', title: $t('lz.entity.tmsVehicleFilterHistory.equipmentId'), minWidth: 180, showOverflow: 'tooltip', sortable: false, slots: { default: 'row_equipment' }},
				// { field: 'laneId', title: $t('lz.entity.tmsVehicleFilterHistory.laneId'), minWidth: 100, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_laneId' }},
				// { field: 'locationId', title: $t('lz.entity.tmsVehicleFilterHistory.locationId'), minWidth: 100, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_locationId' }},
				{ field: 'direction', title: $t('lz.entity.tms.direction'), minWidth: 100, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_direction' }},
			],
		},
		{
			proxyConfig: { autoLoad: false, ajax: { query: ({ page, sort }) => handleQueryApi(page, sort) } },
			sortConfig: { defaultSort: state.localPageParam.defaultSort },
			pagerConfig: { pageSize: state.localPageParam.pageSize },
			toolbarConfig: { export: false },
		}
	);
	// Thu gọn thanh phân trang theo chiều rộng bảng.
	useGridPager(xGrid, options);

	onMounted(async () => {
		filter.loadConfigLists();
		// Không chặn bảng: danh sách camera về sau thì cột Camera tự cập nhật (computed).
		filter.loadEquipmentOptions();
		await handleQuery();
	});

	const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
		const params = {
			...filter.buildFilterParams(),
			page: page.currentPage, pageSize: page.pageSize, field: sort.field, order: sort.order,
		} as VehicleFilterHistoryInput;
		return await getVehicleFilterHistory(params);
	};

	const handleQuery = async () => {
		options.loading = true;
		try { await xGrid.value?.commitProxy('reload'); }
		catch (error) { console.error('Không tải được lịch sử lọc phương tiện.', error); }
		finally { options.loading = false; }
		selectChangeEvent();
	};

	const resetQuery = async () => {
		filter.resetFilters();
		await handleQuery();
	};

	function selectChangeEvent() {
		state.showBatchDelete = (xGrid.value?.getCheckboxRecords() ?? []).length > 0;
	}

	const gridEvents: VxeGridListeners<VehicleFilterHistoryOutput> = {
		async pageChange({ pageSize }) {
			state.localPageParam.pageSize = pageSize;
		},
		async sortChange({ field, order }) {
			state.localPageParam.defaultSort = { field: field, order: order!, descStr: 'desc' };
		},
	};
</script>
