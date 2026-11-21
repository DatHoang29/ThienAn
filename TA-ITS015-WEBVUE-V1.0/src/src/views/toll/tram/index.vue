<template>
	<div class="toll-tram-container">
		<el-card shadow="hover" :body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', height: '100%', alignItems: 'start' }">
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true" label-width="auto" style="flex: 1 1 0%" @submit.prevent="handleQuery">
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.toll.station')" prop="keyword">
							<el-input v-model="state.queryParams.keyword" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.toll.station') })" clearable @keyup.enter.native="handleQuery" />
						</el-form-item>
					</el-col>
					<!-- <el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.status')" prop="active">
							<el-select v-model="state.queryParams.active" filterable clearable class="w100" @clear="state.queryParams.active = undefined" :placeholder="$t('lz.label.base.selectAll')">
								<el-option v-for="item in getConfigDataByCode(BaseConfigTypeEnum.SysStatusType)" :key="item.code" :label="item.name" :value="parseInt(item.code)" />
							</el-select>
						</el-form-item>
					</el-col> -->
				</el-row>
			</el-form>

			<el-divider style="height: calc(100% - 5px); margin: 0 10px" direction="vertical" />
			<el-row>
				<SearchButton :auth-search="'tollStation:page'" @search="handleQuery" @reset="resetQuery" :loading="options.loading" />
			</el-row>
		</el-card>

		<el-card class="full-table" shadow="hover" style="margin-top: 5px">
			<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options">
				<template #toolbar_tools>
					<TableHeader :show-add="false" :show-delete="false" @refresh="handleQuery" @custom-column="handleCustom">
						<template #prefix>
							<el-button type="warning" icon="ele-Download" :loading="isFetching" @click="handleManualFetch">
								{{ $t('lz.button.toll.manualFetch') }}
							</el-button>
						</template>
					</TableHeader>
				</template>
				<template #empty>
					<el-empty :image-size="200" />
				</template>
				<template #row_status="{ row }">
					<TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.SysStatusType, row.active)" />
				</template>
				<template #row_buttons="{ row }">
					<el-tooltip :content="$t('lz.label.toll.mapWorkUnit')" placement="top" :auto-close="1000">
						<el-button icon="ele-Link" text type="primary" @click="handleMapWorkUnit(row)"> </el-button>
					</el-tooltip>
				</template>
			</vxe-grid>
		</el-card>

		<MapWorkUnitComp ref="mapWorkUnitCompRef" @handleQuery="handleQuery" />
	</div>
</template>

<script lang="ts" setup name="tollStation">
import { reactive, ref, defineAsyncComponent } from 'vue';
import { dayjs } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { VxeGridInstance } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { getAPI } from '/@/utils/axios-utils';
import { TramApi, TramOutput, PageTramInput } from '/@/api-services/toll';
import { BaseConfigTypeEnum } from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';
import { useManualFetchToll } from '/@/transporter/hooks/useManualFetchToll';
import { useTollWorkerStatus } from '/@/transporter/hooks/useTollWorkerStatus';
import MapWorkUnitComp from '/@/views/toll/tram/component/mapWorkUnit.vue';

const getConfigItemByCode = useBaseConfig().getConfigItemByCode;
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));
const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));
const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));

const { t: $t } = useI18n();
const xGrid = ref<VxeGridInstance>();
const mapWorkUnitCompRef = ref<InstanceType<typeof MapWorkUnitComp>>();
const state = reactive({
	queryParams: {
		keyword: undefined as string | undefined,
		stationType: undefined as number | undefined,
		active: undefined as number | undefined,
	},
});

// Cấu hình bảng dữ liệu trạm thu phí
const options = useVxeTable<TramOutput>(
	{
		id: 'tollStation',
		name: 'TollStation',
		columns: [
			{ field: 'stationId', title: $t('lz.entity.toll.stationId'), minWidth: 140, showOverflow: 'tooltip', sortable: true},
			{ field: 'name', title: $t('lz.entity.base.name'), minWidth: 200, showOverflow: 'tooltip', sortable: true},
			// Loại trạm: dựa trên field Type (hệ xử lý ETC), hiển thị tên qua danh mục cấu hình
			{ field: 'type', title: $t('lz.entity.toll.stationType'), width: 140, sortable: true, formatter: ({ cellValue }: any) => getConfigItemByCode(BaseConfigTypeEnum.TollStationType, cellValue)?.name ?? cellValue },
			{ field: 'remark', title: $t('lz.entity.base.remark'), minWidth: 200, showOverflow: 'tooltip'},
			// Đơn vị TMS được gán (TmsWorkUnit, Type = toll)
			{ field: 'refName', title: $t('lz.entity.toll.workUnit'), minWidth: 180, showOverflow: 'tooltip', formatter: ({ row }: any) => (row.refId ? `${row.refCode ?? ''} - ${row.refName ?? ''}` : '') },
			{ field: 'actionClm', title: $t('lz.label.base.action'), fixed: 'right', width: 80, align: 'center', showOverflow: true, slots: { default: 'row_buttons' } },
		],
	},
	{
		proxyConfig: { autoLoad: true, ajax: { query: ({ page, sort }) => handleQueryApi(page, sort) } },
		pagerConfig: { pageSize: 50 },
		rowConfig: { isCurrent: true, isHover: true },
		border: true,
		showOverflow: true,
	}
);

// Gọi API lấy dữ liệu (phân trang)
const handleQueryApi = async (page: any, sort: any) => {
	try {
		return await getAPI(TramApi).apiTollTramPagePost({
			page: page.currentPage,
			pageSize: page.pageSize,
			field: sort?.order ? sort?.field : undefined,
			order: sort?.order ? sort?.order : undefined,
			descStr: 'desc',
			keyword: state.queryParams.keyword,
			stationType: state.queryParams.stationType,
			active: state.queryParams.active,
		} as PageTramInput);
	} catch (error) {
		xGrid.value?.loadData([]);
		return {
			data: {
				result: {
					records: [],
					items: [],
					total: 0,
				},
			},
		};
	}
};

// Tìm kiếm
const handleQuery = async () => {
	options.loading = true;
	await xGrid.value?.commitProxy('reload');
	options.loading = false;
};

// Đặt lại điều kiện tìm kiếm
const resetQuery = async () => {
	state.queryParams.keyword = undefined;
	state.queryParams.stationType = undefined;
	state.queryParams.active = undefined;
	await xGrid.value?.commitProxy('reload');
};

// Mở dialog gán đơn vị TMS cho trạm
const handleMapWorkUnit = (row: TramOutput) => {
	mapWorkUnitCompRef.value?.openDialog(row);
};

// Tuỳ chỉnh cột
function handleCustom() {
	xGrid.value?.openCustom();
}

// Lấy dữ liệu trạm thủ công từ TCS (qua NATS); nhận kết quả -> nạp lại lưới
const { isFetching, trigger } = useManualFetchToll({ onSuccess: () => handleQuery() });
const handleManualFetch = () => trigger(() => getAPI(TramApi).apiTollTramManualfetchPost({}));

// Trạng thái Online/Offline của ITS.Toll.Worker (theo heartbeat NATS mỗi 30s)
const { status: workerStatus, statusTagType: workerStatusTagType } = useTollWorkerStatus();
</script>

<style lang="scss" scoped>
.toll-tram-container {
	height: 100%;
}
.full-table {
	height: 100%;
}
</style>
