<template>
	<div class="vehicle-filter-history-container">
		<el-card shadow="hover" :body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', height: '100%', alignItems: 'start' }">
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true"	label-width="auto" style="flex: 1 1 0%">
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
						<e-col>
							<el-form-item :label="$t('lz.label.base.fromDate')" prop="fromDate">
								<el-date-picker
									v-model="state.queryParams.fromDate"
									type="datetime"
									:disabled-date="(time:any) => time.getTime() > Date.now()"
									:format="DateTimeFormatEnum.DateTimeDefault"
									:value-format="DateTimeFormatEnum.DateTimeValidInput"
									:placeholder="$t('lz.label.base.fromDate')"
									clearable
									class="w100"
								/>
							</el-form-item>
						</e-col>
					</el-col>

					<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.tms.direction')" prop="direction">
							<el-select v-model="state.queryParams.direction" :placeholder="$t('lz.label.base.pleaseSelect', {input: $t('lz.entity.tms.direction')})" filterable clearable class="w100">
								<el-option v-for="item in state.directionList" :label="item.name" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.type')" prop="type">
							<el-select v-model="state.queryParams.type" :placeholder="$t('lz.label.base.pleaseType', {input: $t('lz.entity.base.type')})" filterable clearable class="w100">
								<el-option v-for="item in state.filterTypeList" :label="item.name" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.tms.licensePlate')" prop="licensePlate">
							<el-input v-model="state.queryParams.licensePlate" :placeholder="$t('lz.label.base.pleaseType', {input: $t('lz.entity.tms.licensePlate')})" @keyup.enter.native="handleQuery" :maxlength="EntityFieldLength.Length32" show-word-limit clearable class="w100"/>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
						<e-col>
							<el-form-item :label="$t('lz.label.base.toDate')" prop="toDate">
								<el-date-picker
									v-model="state.queryParams.toDate"
									type="datetime"
									:disabled-date="(time :any) => time.getTime() > Date.now()"
									:format="DateTimeFormatEnum.DateTimeDefault"
									:value-format="DateTimeFormatEnum.DateTimeValidInput"
									:placeholder="$t('lz.label.base.toDate')"
									clearable
									class="w100"
								/>
							</el-form-item>
						</e-col>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.tmsVehicleFilterHistory.equipmentId')" prop="equipmentId">
							<el-select v-model="state.queryParams.equipmentId" :placeholder="$t('lz.label.base.pleaseSelect', {input: $t('lz.entity.tmsVehicleFilterHistory.equipmentId')})" filterable clearable class="w100">
								<el-option v-for="item in vdsCameraList" :key="String(item.id)" :label="cameraLabel(item)" :value="String(item.id)" />
							</el-select>
						</el-form-item>
					</el-col>

				</el-row>
			</el-form>
			<el-divider style="height: calc(100% - 5px); margin: 0 10px" direction="vertical" />
			<el-row>
				<SearchButton authSearch="tmsVehicleFilterHistory:list" @search="handleQuery" @reset="resetQuery" :loading="state.viewMode === 'trip' ? tripOptions.loading : options.loading" />
			</el-row>
		</el-card>

		<el-card class="full-table" shadow="hover" style="margin-top: 5px">
			<!-- Chế độ THEO LƯỢT: mỗi dòng là một lần camera bắt được xe khớp danh sách -->
			<vxe-grid v-if="state.viewMode === 'hit'" ref="xGridVehicleFilter" class="xGrid-style" v-bind="options" v-on="gridEvents" @checkbox-change="selectChangeEvent" @checkbox-all="selectChangeEvent">
				<template #toolbar_buttons>
					<el-segmented v-model="state.viewMode" :options="viewModeOptions" @change="handleViewModeChange" />
				</template>
				<template #empty>
					<el-empty :image-size="200" />
				</template>
				<template #row_detectTime="{ row }">
					{{ formatUtcTime(row.detectTime) }}
				</template>
				<template #row_equipment="{ row }">
					{{ row.equipmentId ? cameraLabelById(row.equipmentId) : '—' }}
				</template>
				<template #row_locationId="{ row }">
					{{ state.locationList?.find((item: any) => item.code == row.locationId)?.name }}
				</template>
				<template #row_direction="{ row }">
					{{ state.directionList?.find((item: any) => item.code == row.direction)?.name }}
				</template>
				<template #row_type="{ row }">
					{{ state.filterTypeList?.find((item: any) => item.code == row.type)?.name }}
				</template>
				<template #row_laneId="{ row }">
					{{ state.laneList?.find((item: any) => item.code == row.laneId)?.name }}
				</template>
				<template #row_record="{ row }">
					<ModifyRecord :data="row" />
				</template>
				<template #row_buttons="{ row }">
					<el-tooltip :content="$t('lz.button.base.delete')" placement="top" :auto-close="1000">
						<el-button icon="ele-Delete" text type="danger" v-auth="'tmsVehicleFilterHistory:delete'"> </el-button>
					</el-tooltip>
				</template>
				<template #header_name>
					{{ $t('lz.label.tms.tmsVehicleFilterHistory') }}
				</template>
			</vxe-grid>

			<!-- Chế độ THEO CHUYẾN: các lượt cùng biển đi tiến theo tuyến được gom thành một dòng -->
			<vxe-grid v-else ref="xGridVehicleTrip" class="xGrid-style" v-bind="tripOptions" v-on="tripGridEvents">
				<template #toolbar_buttons>
					<el-segmented v-model="state.viewMode" :options="viewModeOptions" @change="handleViewModeChange" />
				</template>
				<template #empty>
					<el-empty :image-size="200" />
				</template>
				<template #row_type="{ row }">
					<TagInfo v-if="getFilterTypeItem(row.type)?.name" :config-item="getFilterTypeItem(row.type)" />
					<span v-else>{{ row.type ?? '' }}</span>
				</template>
				<template #row_fromTime="{ row }">
					{{ formatUtcTime(row.fromTime) }}
				</template>
				<template #row_toTime="{ row }">
					{{ formatUtcTime(row.toTime) }}
				</template>
				<template #row_direction="{ row }">
					{{ state.directionList?.find((item: any) => item.code == row.direction)?.name }}
				</template>
				<!-- Slot (không dùng formatter) để tự vẽ lại khi danh sách camera nạp xong sau dữ liệu bảng -->
				<template #row_route="{ row }">
					{{ formatRoute(row.equipmentIds) }}
				</template>
			</vxe-grid>
		</el-card>
	</div>
</template>

<script lang="ts" setup>

	import { onMounted, reactive, ref, computed, nextTick, defineAsyncComponent } from 'vue';
	import { useI18n } from 'vue-i18n';
	import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
	import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
	import ModifyRecord from '/@/components/table/modifyRecord.vue';
	import TagInfo from '/@/components/advanced/tag-info.vue';
	import { useDateFormat } from '@vueuse/core'
	import { ElMessage } from 'element-plus';

	import {
		getVehicleFilterHistory, getVehicleFilterTrips,
		type VehicleFilterHistoryInput, type VehicleFilterHistoryOutput,
		type VehicleFilterTripInput, type VehicleFilterTripOutput,
	} from '/@/services/tms/traffic-status';
	import { vdsTimestamp } from '/@/utils/vdsTimestamp';


	import { useCommonVarsAndFuncs } from '/@/stores/its/commonVarsAndFuncs';
	const commonVarsAndFuncs = useCommonVarsAndFuncs();

	import { useMapStore } from '/@/stores/its/mapStore';
	const mapStore = useMapStore();

	import { useBaseConfig } from '/@/stores/baseConfig';
	import { BaseConfigTypeEnum } from '/@/types/enums';

	const baseConfig = useBaseConfig();
	const getConfigDataByCode = baseConfig.getConfigDataByCode;

	import { DateTimeFormatEnum, EntityFieldLength } from '/@/types/enums/baseEnum';

	/** 'hit' = xem từng lượt (chế độ cũ), 'trip' = xem gom theo chuyến. */
	type ViewMode = 'hit' | 'trip';

	// Chỉ camera VDS mới ghi lượt lọc xe, nên ô lọc Camera chỉ liệt kê đúng loại thiết bị này (giống trang lịch sử sự kiện).
	const vdsEquipmentTypeCode = 'VDS';

	const state = reactive({
		viewMode: 'hit' as ViewMode,
		queryParams: {
			licensePlate: undefined,
			direction: undefined,
			type: undefined,
			equipmentId: undefined as string | undefined,
			fromDate: commonVarsAndFuncs.getISODatetimeFromDays(-3),
			toDate: commonVarsAndFuncs.getISODatetimeFromDays(0),
		},
		localPageParam: {
			pageSize: 50 as number,
			defaultSort: { field: 'detectTime', order: 'desc', descStr: 'desc' } as any,
		},
		tripPageSize: 50 as number,
		title: '',
		showBatchDelete: false,
		locationList: [] as any,
		laneList: [] as any,
		directionList: [] as any,
		filterTypeList: [] as any
	});

	const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));
	const { t: $t } = useI18n();
	const xGridVehicleFilter = ref<VxeGridInstance>();
	const xGridVehicleTrip = ref<VxeGridInstance>();

	const viewModeOptions = computed(() => [
		{ label: $t('lz.label.tmsVehicleFilterHistory.viewByHit'), value: 'hit' },
		{ label: $t('lz.label.tmsVehicleFilterHistory.viewByTrip'), value: 'trip' },
	]);

	/* ------------------------------ Camera: tra tên + lý trình ------------------------------ */

	/** Lý trình dạng "Km X+YYY"; dùng chung cho cột Camera, ô lọc và Lộ trình. */
	const formatKm = (km?: number | null, met?: number | null) => `Km ${km ?? 0}+${String(met ?? 0).padStart(3, '0')}`;

	/** "Tên camera (Km X+YYY)"; thiết bị chưa khai báo lý trình thì chỉ hiện tên. */
	const cameraLabel = (equipment: any) => {
		const name = equipment?.name || equipment?.code || String(equipment?.id ?? '');
		if (equipment?.kmNumber == null && equipment?.metNumber == null) return name;
		return `${name} (${formatKm(equipment.kmNumber, equipment.metNumber)})`;
	};

	/** Tra theo toàn bộ thiết bị (không chỉ VDS) để dòng cũ/thiết bị đã đổi loại vẫn hiện được tên. */
	const equipmentById = computed(() => {
		const map = new Map<string, any>();
		for (const equipment of mapStore.equipmentList ?? []) {
			if (equipment?.id != null) map.set(String(equipment.id), equipment);
		}
		return map;
	});

	/** Id không tra được thì rút gọn 8 ký tự đầu để cột không bị dài. */
	const shortId = (id: string) => (id.length > 8 ? `${id.slice(0, 8)}…` : id);

	const cameraLabelById = (equipmentId?: string | null) => {
		if (!equipmentId) return '—';
		const equipment = equipmentById.value.get(String(equipmentId));
		return equipment ? cameraLabel(equipment) : shortId(String(equipmentId));
	};

	/** Lộ trình camera theo thời gian: "VDS-01 (Km 10+200) → VDS-03 (Km 13+500) → …" (bỏ camera lặp liền kề). */
	const formatRoute = (equipmentIds?: Array<string> | null) => {
		const ids = (equipmentIds ?? []).filter((id, index, all) => !!id && id !== all[index - 1]);
		return ids.length ? ids.map((id) => cameraLabelById(id)).join(' → ') : '—';
	};

	const vdsCameraList = computed(() => {
		const vdsTypeIds = new Set(
			(mapStore.equipmentTypeList ?? [])
				.filter((equipmentType: any) => String(equipmentType?.code ?? '').trim().toUpperCase() === vdsEquipmentTypeCode)
				.map((equipmentType: any) => String(equipmentType.id))
		);
		return (mapStore.equipmentList ?? [])
			.filter((equipment: any) => equipment?.id != null && vdsTypeIds.has(String(equipment.equipmentTypeId)))
			.sort((first: any, second: any) => cameraLabel(first).localeCompare(cameraLabel(second), 'vi'));
	});

	/** Dùng lại danh sách thiết bị của mapStore; chỉ gọi API khi store chưa nạp. */
	const loadEquipmentOptions = async () => {
		try {
			await Promise.all([
				(mapStore.equipmentList?.length ?? 0) === 0 ? mapStore.fetchEquipmentList() : Promise.resolve(),
				(mapStore.equipmentTypeList?.length ?? 0) === 0 ? mapStore.fetchEquipmentTypeList() : Promise.resolve(),
			]);
		} catch (error) {
			console.error('Không tải được danh sách camera VDS.', error);
		}
	};

	/* ------------------------------ Định dạng chung ------------------------------ */

	/** Thời gian từ API là UTC (có thể thiếu hậu tố Z) → hiển thị giờ địa phương. */
	const formatUtcTime = (value?: string | Date | null) =>
		value ? useDateFormat(vdsTimestamp(value as string | Date), DateTimeFormatEnum.DateTimeDefault).value : '';

	const getFilterTypeItem = (code?: string | null) =>
		code ? baseConfig.getConfigItemByCode(BaseConfigTypeEnum.TmsVehicleFilterType, code) : null;

	/* ------------------------------ Bảng THEO LƯỢT ------------------------------ */

	const options = useVxeTable<VehicleFilterHistoryOutput>(
		{
			id: 'tmsVehicleFilterHistoryGrid',
			name: 'tmsVehicleFilterHistory',
			columns: [
				// Fields
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
		// vxeGrid configuration parameters (any parameter can be overridden here), refer to the official vxe-table documentation
		{
			// Proxy configuration
			proxyConfig: { autoLoad: false, ajax: { query: ({ page, sort }) => handleQueryApi(page, sort) } },

			// Sorting configuration
			sortConfig: { defaultSort: state.localPageParam.defaultSort },

			// Pagination configuration
			pagerConfig: { pageSize: state.localPageParam.pageSize },

			// Toolbar configuration
			toolbarConfig: { export: false },
		}
	);

	/* ------------------------------ Bảng THEO CHUYẾN ------------------------------ */

	const tripOptions = useVxeTable<VehicleFilterTripOutput>(
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
			proxyConfig: { autoLoad: false, sort: false, ajax: { query: ({ page }) => handleTripQueryApi(page) } },
			pagerConfig: { pageSize: state.tripPageSize },
			toolbarConfig: { export: false },
		}
	);

	onMounted(async () => {
		state.locationList = getConfigDataByCode(BaseConfigTypeEnum.TmsLocationConfigType);
		state.filterTypeList = getConfigDataByCode(BaseConfigTypeEnum.TmsVehicleFilterType);
		state.directionList = getConfigDataByCode(BaseConfigTypeEnum.TmsDirectionType);
		state.laneList = getConfigDataByCode(BaseConfigTypeEnum.TmsLaneType);

		// Không chặn bảng: danh sách camera về sau thì cột Camera/Lộ trình tự cập nhật (computed).
		loadEquipmentOptions();
		await handleQuery();
	});

	/** Bộ lọc chung cho cả hai chế độ; ném lỗi nếu khoảng thời gian không hợp lệ (tối đa 60 ngày). */
	const buildFilterParams = () => {
		const params = {
			licensePlate: state.queryParams.licensePlate || undefined,
			direction: state.queryParams.direction || undefined,
			type: state.queryParams.type || undefined,
			equipmentId: state.queryParams.equipmentId || undefined,
			fromDate: new Date(state.queryParams.fromDate ?? ''),
			toDate: new Date(state.queryParams.toDate ?? ''),
		};
		if (!state.queryParams.fromDate || !state.queryParams.toDate || !Number.isFinite(params.fromDate.getTime()) || !Number.isFinite(params.toDate.getTime()) || params.toDate <= params.fromDate || params.toDate.getTime() - params.fromDate.getTime() > 60 * 86400000) {
			ElMessage.error($t('lz.validation.tmsVehicleFilterHistory.timeRange'));
			throw new Error($t('lz.validation.tmsVehicleFilterHistory.timeRange'));
		}
		return params;
	};

	// Query API (theo lượt)
	const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
		const params = {
			...buildFilterParams(),
			page: page.currentPage, pageSize: page.pageSize, field: sort.field, order: sort.order,
		} as VehicleFilterHistoryInput;
		return await getVehicleFilterHistory(params);
	};

	// Query API (theo chuyến)
	const handleTripQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams) => {
		const params = {
			...buildFilterParams(),
			page: page.currentPage, pageSize: page.pageSize,
		} as VehicleFilterTripInput;
		return await getVehicleFilterTrips(params);
	};

	// Query operation: nạp lại bảng của chế độ đang xem
	const handleQuery = async () => {
		const isTrip = state.viewMode === 'trip';
		const gridOptions = isTrip ? tripOptions : options;
		gridOptions.loading = true;
		try { await (isTrip ? xGridVehicleTrip : xGridVehicleFilter).value?.commitProxy('reload'); }
		catch (error) { console.error('Không tải được lịch sử lọc phương tiện.', error); }
		finally { gridOptions.loading = false; }
		selectChangeEvent();
	};

	// Đổi chế độ xem: bảng mới vừa được dựng (v-if) nên chờ DOM rồi nạp với cùng bộ lọc.
	const handleViewModeChange = async () => {
		await nextTick();
		await handleQuery();
	};

	// Reset operation
	const resetQuery = async () => {
		state.queryParams.licensePlate = undefined;
		state.queryParams.direction = undefined;
		state.queryParams.type = undefined;
		state.queryParams.equipmentId = undefined;
		state.queryParams.fromDate = commonVarsAndFuncs.getISODatetimeFromDays(-3);
		state.queryParams.toDate = commonVarsAndFuncs.getISODatetimeFromDays(0);
		await handleQuery();
	};

	// Get selected records
	function getCheckedRecords() {
		return xGridVehicleFilter.value?.getCheckboxRecords() ?? [];
	}

	function selectChangeEvent() {
		state.showBatchDelete = getCheckedRecords().length > 0;
	}

	// Table events
	const gridEvents: VxeGridListeners<VehicleFilterHistoryOutput> = {
		// Only valid when pager-config is configured, this event will be triggered when the page changes
		async pageChange({ pageSize }) {
			state.localPageParam.pageSize = pageSize;
		},
		// This event will be triggered when the sorting condition changes
		async sortChange({ field, order }) {
			state.localPageParam.defaultSort = { field: field, order: order!, descStr: 'desc' };
		},
	};

	const tripGridEvents: VxeGridListeners<VehicleFilterTripOutput> = {
		async pageChange({ pageSize }) {
			state.tripPageSize = pageSize;
		},
	};
</script>
