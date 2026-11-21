<template>
	<!-- KienLT - 09/11/2026: tab "Theo chuyến" của menu nhóm tabPage (Lịch sử lọc phương tiện); tab "Theo lượt" ở ../index.vue. -->
	<WorkspacePage>
	<div class="vehicle-filter-trip-container">
		<GridSearchPanel id="tms-vehicle-filter-trip-search">
			<template #filters>
				<VehicleFilterSearch :filter="filter" :loading="!!options.loading" @search="handleQuery" @reset="resetQuery" />
			</template>

			<el-card class="full-table" shadow="hover">
				<!-- Các lượt cùng biển đi tiến theo tuyến được gom thành một dòng -->
				<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
					<template #empty>
						<el-empty :image-size="200" />
					</template>
					<template #row_type="{ row }">
						<TagInfo v-if="filter.getFilterTypeItem(row.type)?.name" :config-item="filter.getFilterTypeItem(row.type)" />
						<span v-else>{{ row.type ?? '' }}</span>
					</template>
					<template #row_fromTime="{ row }">
						{{ filter.formatUtcTime(row.fromTime) }}
					</template>
					<template #row_toTime="{ row }">
						{{ filter.formatUtcTime(row.toTime) }}
					</template>
					<template #row_direction="{ row }">
						{{ filter.findName(filter.lists.directionList, row.direction) }}
					</template>
					<!-- Slot (không dùng formatter) để tự vẽ lại khi danh sách camera nạp xong sau dữ liệu bảng -->
					<template #row_route="{ row }">
						{{ filter.formatRoute(row.equipmentIds) }}
					</template>
				</vxe-grid>
			</el-card>
		</GridSearchPanel>
	</div>
	</WorkspacePage>
</template>

<script lang="ts" setup name="tmsVehicleFilterTrip">
	import { onMounted, reactive, ref } from 'vue';
	import { useI18n } from 'vue-i18n';
	import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
	import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
	import TagInfo from '/@/components/advanced/tag-info.vue';
	import WorkspacePage from '/@/components/workspace/WorkspacePage.vue';
	import GridSearchPanel from '/@/components/gridPage/GridSearchPanel.vue';
	import { useGridPager } from '/@/components/gridPage/useGridPager';
	import { getVehicleFilterTrips, type VehicleFilterTripInput, type VehicleFilterTripOutput } from '/@/services/tms/traffic-status';
	import VehicleFilterSearch from '../component/vehicleFilterSearch.vue';
	import { useVehicleFilterHistory } from '../component/useVehicleFilterHistory';

	const { t: $t } = useI18n();
	const filter = useVehicleFilterHistory();
	const xGrid = ref<VxeGridInstance>();

	const state = reactive({ pageSize: 50 as number });

	const options = useVxeTable<VehicleFilterTripOutput>(
		{
			id: 'tmsVehicleFilterTripGrid',
			name: 'tmsVehicleFilterTrip',
			// API chuyến luôn trả mới nhất trước và bỏ qua field/order, nên không cho sắp xếp cột.
			columns: [
				{ field: 'licensePlate', title: $t('lz.entity.tms.licensePlate'), minWidth: 110, showOverflow: 'tooltip', sortable: false },
				{ field: 'type', title: $t('lz.entity.base.type'), minWidth: 110, showOverflow: 'tooltip', sortable: false, slots: { default: 'row_type' }},
				{ field: 'fromTime', title: $t('lz.entity.tmsVehicleFilterHistory.fromTime'), minWidth: 150, showOverflow: 'tooltip', sortable: false, slots: { default: 'row_fromTime' }},
				{ field: 'toTime', title: $t('lz.entity.tmsVehicleFilterHistory.toTime'), minWidth: 150, showOverflow: 'tooltip', sortable: false, slots: { default: 'row_toTime' }},
				{ field: 'hitCount', title: $t('lz.entity.tmsVehicleFilterHistory.hitCount'), width: 90, sortable: false },
				{ field: 'direction', title: $t('lz.entity.tms.direction'), minWidth: 100, showOverflow: 'tooltip', sortable: false, slots: { default: 'row_direction' }},
				{ field: 'equipmentIds', title: $t('lz.entity.tmsVehicleFilterHistory.route'), minWidth: 320, align: 'left', showOverflow: 'tooltip', sortable: false, slots: { default: 'row_route' }},
			],
		},
		{
			proxyConfig: { autoLoad: false, sort: false, ajax: { query: ({ page }) => handleQueryApi(page) } },
			pagerConfig: { pageSize: state.pageSize },
			toolbarConfig: { export: false },
		}
	);
	// Thu gọn thanh phân trang theo chiều rộng bảng.
	useGridPager(xGrid, options);

	onMounted(async () => {
		filter.loadConfigLists();
		// Không chặn bảng: danh sách camera về sau thì cột Lộ trình tự cập nhật (computed).
		filter.loadEquipmentOptions();
		await handleQuery();
	});

	const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams) => {
		const params = {
			...filter.buildFilterParams(),
			page: page.currentPage, pageSize: page.pageSize,
		} as VehicleFilterTripInput;
		return await getVehicleFilterTrips(params);
	};

	const handleQuery = async () => {
		options.loading = true;
		try { await xGrid.value?.commitProxy('reload'); }
		catch (error) { console.error('Không tải được lịch sử lọc phương tiện theo chuyến.', error); }
		finally { options.loading = false; }
	};

	const resetQuery = async () => {
		filter.resetFilters();
		await handleQuery();
	};

	const gridEvents: VxeGridListeners<VehicleFilterTripOutput> = {
		async pageChange({ pageSize }) {
			state.pageSize = pageSize;
		},
	};
</script>
