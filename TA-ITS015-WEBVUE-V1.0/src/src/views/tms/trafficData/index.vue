<template>
	<div class="tms-trafficdata-container">
		<!-- Thẻ tìm kiếm thu gọn / kéo đổi chiều cao được như các trang danh sách khác (GridSearchPanel). -->
		<GridSearchPanel id="tms-traffic-data-search">
			<template #filters>
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true" label-width="auto" style="flex: 1 1 0%">
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="24" :md="12" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.label.base.fromDate')" prop="fromDate">
							<el-date-picker
								v-model="state.queryParams.fromDate"
								type="date"
								:disabled-date="(time) => time.getTime() > Date.now()"
								:format="DateTimeFormatEnum.DateDefault"
								:value-format="DateTimeFormatEnum.DateTimeValidInput"
								:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.label.base.fromDate') })"
								clearable
								class="w100"
								@keyup.enter.native="handleQuery"
							/>
						</el-form-item>
					</el-col>

					<el-col class="mb5" :xs="24" :sm="24" :md="12" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.label.base.toDate')" prop="toDate">
							<el-date-picker
								v-model="state.queryParams.toDate"
								type="date"
								:disabled-date="(time) => time.getTime() > Date.now()"
								:format="DateTimeFormatEnum.DateDefault"
								:value-format="DateTimeFormatEnum.DateTimeValidInput"
								:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.label.base.toDate') })"
								clearable
								class="w100"
								@keyup.enter.native="handleQuery"
							/>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.tms.licensePlate')" prop="licensePlate">
							<el-input
								v-model="state.queryParams.licensePlate"
								:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.tms.licensePlate') })"
								clearable
								@keyup.enter.native="handleQuery"
							/>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.tms.equipment')" prop="equipment" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.tms.equipment') })">
							<el-select v-model="state.queryParams.equipmentId" filterable clearable class="w100" @clear="state.queryParams.equipmentId = undefined">
								<el-option v-for="item in state.equipmentList" :key="item.id" :label="item.name" :value="item.id" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.tms.vehicleSize')" prop="type" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.tms.vehicleSize') })">
							<el-select v-model="state.queryParams.vehicleSize" filterable clearable class="w100" @clear="state.queryParams.vehicleSize = undefined">
								<el-option v-for="item in state.vehicleTypeList" :key="item.code" :label="item.name" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.tms.direction')" prop="direction" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.tms.direction') })">
							<el-select v-model="state.queryParams.direction" filterable clearable class="w100" @clear="state.queryParams.direction = undefined">
								<el-option v-for="item in state.directionList" :key="item.code" :label="item.name" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>

			<el-divider style="height: calc(100% - 5px); margin: 0 10px" direction="vertical" />

			<el-row>
				<SearchButton :auth-search="'tmsTrafficData:page'" @search="handleQuery" @reset="resetQuery" :loading="options.loading" />
			</el-row>
			</template>

		<el-card class="full-table" shadow="hover">
			<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
				<template #toolbar_tools>
					<!-- Dữ liệu thô do camera VDS ghi (QuestDB): chỉ xem, không thêm/xoá tay. -->
					<TableHeader :show-add="false" @refresh="handleQuery" @custom-column="handleCustom"> </TableHeader>
				</template>

				<template #empty>
					<el-empty :image-size="200" />
				</template>

				<template #row_equipmentId="{ row }">
					{{ state.equipmentList?.find((item) => item.id == row.equipmentId)?.name }}
				</template>

				<template #row_direction="{ row }">
					{{ state.directionList?.find((item) => item.code == row.direction)?.name }}
				</template>

				<template #row_type="{ row }">
					{{ state.vehicleTypeList?.find((item) => item.code?.trim().toLowerCase() == row.vehicleSize?.trim().toLowerCase())?.name ?? row.vehicleSize }}
				</template>

				<template #row_detectTime="{ row }">
					{{ useDateFormat(vdsTimestamp(row.detectTime), DateTimeFormatEnum.DateTimeDefault) }}
				</template>
				<template #row_record="{ row }">
					<ModifyRecord :data="row" />
				</template>
				<template #row_buttons="{ row }">
					<el-tooltip :content="$t('lz.button.base.view')" placement="top">
						<el-button icon="ele-View" text type="primary" v-auth="'tmsTrafficData:view'" @click="handleView(row)"> </el-button>
					</el-tooltip>
				</template>
				<template #header_name>
					{{ $t('lz.entity.base.name') }}
				</template>
			</vxe-grid>
		</el-card>
		</GridSearchPanel>

		<DetailTrafficData ref="detailTrafficDataRef" :title="state.title" :operateType="state.operateType" @handleQuery="handleQuery" />
	</div>
</template>

<script lang="ts" setup name="tmsTrafficData">
import GridSearchPanel from '/@/components/gridPage/GridSearchPanel.vue';
import { onMounted, reactive, ref, defineAsyncComponent } from 'vue';
import { useI18n } from 'vue-i18n';
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import DetailTrafficData from './component/detailTrafficData.vue';
import ModifyRecord from '/@/components/table/modifyRecord.vue';
import { getAPI } from '/@/utils/axios-utils';
import { TmsEquipmentApi } from '/@/api-services/tms';
import { VdsTrafficDataApi } from '/@/api-services/vds';
import { PageTrafficDataInput, PageTrafficDataOutput } from '/@/api-services/vds';
import { OperateType } from '/@/types/constants/operate';
import { vdsTimestamp } from '/@/utils/vdsTimestamp';
import { useDateFormat } from '@vueuse/core';
import { DateTimeFormatEnum } from '/@/types/enums/baseEnum';

const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));
const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));

const { t: $t } = useI18n();
const xGrid = ref<VxeGridInstance>();
const detailTrafficDataRef = ref<InstanceType<typeof DetailTrafficData>>();

import { useBaseConfig } from '/@/stores/baseConfig';
import { BaseConfigTypeEnum } from '/@/types/enums';

const getConfigDataByCode = useBaseConfig().getConfigDataByCode;

import { useCommonVarsAndFuncs } from '/@/stores/its/commonVarsAndFuncs';
const commonVarsAndFuncs = useCommonVarsAndFuncs();
let latestQueryId = 0;
let latestQuery: ReturnType<VdsTrafficDataApi['apiVdsVdstrafficdataPagePost']> | undefined;

const state = reactive({
	queryParams: {
		licensePlate: undefined,
		equipmentId: undefined,
		vehicleSize: undefined,
		direction: undefined,
		fromDate: commonVarsAndFuncs.getISODatetimeFromDays(-3),
		toDate: commonVarsAndFuncs.getISODatetimeFromDays(0),
	},
	localPageParam: {
		pageSize: 50 as number,
		defaultSort: { field: 'detectTime', order: 'desc', descStr: 'desc' },
	},
	visible: false,
	title: '',
	operateType: 'view' as OperateType,
	equipmentList: [] as any,
	directionList: [] as any,
	vehicleTypeList: [] as any,
});

// Table parameter configuration
const options = useVxeTable<PageTrafficDataOutput>(
	{
		id: 'tmsTrafficDataGrid',
		name: 'tmsTrafficDataGrid',
		columns: [
			{ field: 'licensePlate', title: $t('lz.entity.tms.licensePlate'), minWidth: 50, showOverflow: 'tooltip', sortable: true },
			{ field: 'equipmentId', title: $t('lz.entity.tmsTrafficInfo.equipmentId'), minWidth: 50, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_equipmentId' } },
			{ field: 'direction', title: $t('lz.entity.tms.direction'), minWidth: 50, sortable: true, showOverflow: 'tooltip', slots: { default: 'row_direction' } },
			{ field: 'vehicleSize', title: $t('lz.entity.tms.vehicleSize'), minWidth: 80, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_type' } },
			{ field: 'lengthMm', title: $t('lz.entity.tms.length'), width: 150, showOverflow: 'tooltip' },
			{ field: 'detectTime', title: $t('lz.entity.tms.detectTime'), width: 150, showOverflow: 'tooltip', slots: { default: 'row_detectTime' } },

			{ field: '', title: $t('lz.label.base.audit'), width: 100, showOverflow: 'tooltip', slots: { default: 'row_record' } },
			{ title: $t('lz.label.base.action'), fixed: 'right', width: 130, showOverflow: true, slots: { default: 'row_buttons' } },
		],
	},
	// vxeGrid configuration parameters (any parameter can be overridden here), refer to the official vxe-table documentation
	{
		// Proxy configuration
		proxyConfig: { autoLoad: true, ajax: { query: ({ page, sort }) => handleQueryApi(page, sort) } },

		// Sorting configuration
		sortConfig: { defaultSort: state.localPageParam.defaultSort },

		// Pagination configuration
		pagerConfig: { pageSize: state.localPageParam.pageSize },

		// Toolbar configuration
		toolbarConfig: { export: false },
	}
);

// Page initialization
onMounted(async () => {
	let resDicData = await getAPI(TmsEquipmentApi).apiTmsTmsequipmentListGet();
	state.equipmentList = resDicData.data.result?.sort((a, b) => a.name.localeCompare(b.name));

	state.directionList = getConfigDataByCode(BaseConfigTypeEnum.TmsDirectionType);

	state.vehicleTypeList = getConfigDataByCode(BaseConfigTypeEnum.TmsVehicleTypeConfigCode);

	await handleQuery();
});

// Query API
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
	const params: PageTrafficDataInput = {
		...state.queryParams,
		page: page.currentPage,
		pageSize: page.pageSize,
		field: sort.field,
		order: sort.order,
		descStr: 'desc',
	};
	const queryId = ++latestQueryId;
	const request = getAPI(VdsTrafficDataApi).apiVdsVdstrafficdataPagePost(params);
	latestQuery = request;
	const response = await request;
	return queryId === latestQueryId ? response : latestQuery!;
};

// Query operation
const handleQuery = async () => {
	options.loading = true;
	try {
		await xGrid.value?.commitProxy('reload');
	} finally {
		options.loading = false;
	}
};

// Reset operation
const resetQuery = async () => {
	state.queryParams.licensePlate = undefined;
	state.queryParams.equipmentId = undefined;
	state.queryParams.vehicleSize = undefined;
	state.queryParams.direction = undefined;
	state.queryParams.fromDate = commonVarsAndFuncs.getISODatetimeFromDays(-3);
	state.queryParams.toDate = commonVarsAndFuncs.getISODatetimeFromDays(0);

	await xGrid.value?.commitProxy('reload');
};

// Open copy dialog
const handleView = (row: any) => {
	state.title = $t('lz.label.base.titleView');
	state.operateType = 'view';

	detailTrafficDataRef.value?.openDialog(row);
};

// Custom column
function handleCustom() {
	xGrid.value?.openCustom();
}

// Table events
const gridEvents: VxeGridListeners<PageTrafficDataOutput> = {
	// Only valid when pager-config is configured, this event will be triggered when the page changes
	async pageChange({ pageSize }) {
		state.localPageParam.pageSize = pageSize;
	},
	// This event will be triggered when the sorting condition changes
	async sortChange({ field, order }) {
		state.localPageParam.defaultSort = { field: field, order: order!, descStr: 'desc' };
	},
};
</script>
