<template>
	<div class="toll-bang-gia-container">
		<el-row :gutter="8" style="width: 100%; height: 100%; flex: 1">
			<!-- Bảng giá (TollPriceList): begin -->
			<el-col :span="12" :xs="24" class="full-height">
				<el-card shadow="hover" :body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', height: '100%', alignItems: 'start' }">
					<el-form :model="state.queryParamsPriceList" :show-message="false" label-position="left" :inlineMessage="true" label-width="auto" style="flex: 1 1 0%" @submit.prevent="handleQueryPriceList">
						<el-row :gutter="10">
							<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
								<el-form-item :label="$t('lz.entity.toll.priceListCode')">
									<el-input
										v-model="state.queryParamsPriceList.sourceId"
										:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.toll.priceListCode') })"
										@keyup.enter.native="handleQueryPriceList"
										clearable
									/>
								</el-form-item>
							</el-col>
							<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
								<el-form-item :label="$t('lz.entity.toll.priceList')">
									<el-input
										v-model="state.queryParamsPriceList.name"
										:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.toll.priceList') })"
										@keyup.enter.native="handleQueryPriceList"
										clearable
									/>
								</el-form-item>
							</el-col>
						</el-row>
					</el-form>
					<el-divider style="height: calc(100% - 5px); margin: 0 10px" direction="vertical" />
					<el-row>
						<SearchButton :auth-search="'tollPrice:page'" @search="handleQueryPriceList" @reset="resetQueryPriceList" :loading="optionsPriceListGrid.loading" />
					</el-row>
				</el-card>
				<el-card class="full-table" shadow="hover" style="margin-top: 5px">
					<vxe-grid ref="xGridPriceList" class="xGrid-style" v-bind="optionsPriceListGrid" @cell-click="handlePriceListDetailByParent">
						<template #toolbar_buttons>
							<el-icon size="16" style="margin-right: 3px; margin-top: 2px; display: inline; vertical-align: middle"><ele-Collection /></el-icon>
							{{ $t('lz.label.toll.priceList') }}
						</template>
						<template #toolbar_tools>
							<TableHeader :show-add="false" :show-delete="false" @refresh="handleQueryPriceList" @custom-column="handlePriceListCustom">
								<template #prefix>
									<el-tooltip :content="$t('lz.label.toll.manualFetchPriceListTip')" placement="top">
										<el-button type="warning" icon="ele-Download" :loading="isFetching" @click="handleManualFetch">
											{{ $t('lz.button.toll.manualFetch') }}
										</el-button>
									</el-tooltip>
								</template>
							</TableHeader>
						</template>
						<template #empty>
							<el-empty :image-size="200" />
						</template>
						<template #row_active="{ row }">
							<el-tag :type="row.active ? 'success' : 'info'">{{ row.active ? $t('lz.label.base.enable') : $t('lz.label.base.disable') }}</el-tag>
						</template>
					</vxe-grid>
				</el-card>
			</el-col>
			<!-- Bảng giá (TollPriceList): end -->

			<!-- Chi tiết bảng giá (TollPriceListDetail): begin -->
			<el-col :span="12" :xs="24" class="full-height">
				<el-card shadow="hover" :body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', height: '100%', alignItems: 'start' }">
					<el-form :model="state.queryParamsPriceListDetail" :show-message="false" label-position="left" :inlineMessage="true" label-width="auto" style="flex: 1 1 0%" @submit.prevent="handleQueryPriceListDetail">
						<el-row :gutter="10">
							<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
								<el-form-item :label="$t('lz.entity.base.code')">
									<el-input
										v-model="state.queryParamsPriceListDetail.code"
										:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.code') })"
										@keyup.enter.native="handleQueryPriceListDetail"
										clearable
									/>
								</el-form-item>
							</el-col>
							<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="12" :xl="12">
								<el-form-item :label="$t('lz.entity.base.name')">
									<el-input
										v-model="state.queryParamsPriceListDetail.name"
										:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.name') })"
										@keyup.enter.native="handleQueryPriceListDetail"
										clearable
									/>
								</el-form-item>
							</el-col>
						</el-row>
					</el-form>
					<el-divider style="height: calc(100% - 5px); margin: 0 10px" direction="vertical" />
					<el-row>
						<SearchButton :auth-search="'tollPrice:page'" @search="handleQueryPriceListDetail" @reset="resetQueryPriceListDetail" :loading="optionsPriceListDetailGrid.loading" />
					</el-row>
				</el-card>
				<el-card class="full-table" shadow="hover" style="margin-top: 5px">
					<vxe-grid ref="xGridPriceListDetail" class="xGrid-style" v-bind="optionsPriceListDetailGrid">
						<template #toolbar_buttons>
							<el-icon size="16" style="margin-right: 3px; margin-top: 2px; display: inline; vertical-align: middle"><ele-Collection /></el-icon>
							{{ $t('lz.label.toll.priceListDetail') }}
						</template>
						<template #toolbar_tools>
							<TableHeader :show-add="false" :show-delete="false" @refresh="handleQueryPriceListDetail" @custom-column="handlePriceListDetailCustom" />
						</template>
						<template #empty>
							<el-empty :image-size="200" />
						</template>
					</vxe-grid>
				</el-card>
			</el-col>
			<!-- Chi tiết bảng giá (TollPriceListDetail): end -->
		</el-row>
	</div>
</template>

<script lang="ts" setup name="tollPrice">
import { reactive, ref, defineAsyncComponent } from 'vue';
import { dayjs } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { VxeGridInstance } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { getAPI } from '/@/utils/axios-utils';
import { BangGiaApi, BangGiaOutput, PageBangGiaInput, ChiTietBangGiaApi, ChiTietBangGiaOutput, PageChiTietBangGiaInput } from '/@/api-services/toll';
import { useManualFetchToll } from '/@/transporter/hooks/useManualFetchToll';
import { useTollWorkerStatus } from '/@/transporter/hooks/useTollWorkerStatus';

const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));
const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));

const { t: $t } = useI18n();
const xGridPriceList = ref<VxeGridInstance>();
const xGridPriceListDetail = ref<VxeGridInstance>();

const state = reactive({
	queryParamsPriceList: {
		sourceId: undefined as string | undefined,
		name: undefined as string | undefined,
		active: undefined as boolean | undefined,
	},
	queryParamsPriceListDetail: {
		code: undefined as string | undefined,
		name: undefined as string | undefined,
		priceListSourceId: undefined as string | undefined,
		vehicleTypeSourceId: undefined as string | undefined,
		active: undefined as boolean | undefined,
	},
});

const formatDate = ({ cellValue }: any) => (cellValue ? dayjs(cellValue).format('DD/MM/YYYY') : '');

/* Bảng giá (TollPriceList): begin ======================== */

// Cấu hình bảng dữ liệu bảng giá
const optionsPriceListGrid = useVxeTable<BangGiaOutput>(
	{
		id: 'tollPriceListGrid',
		name: 'tollPriceListGrid',
		columns: [
			{ type: 'seq', title: $t('lz.label.base.idx'), width: 60, fixed: 'left' },
			{ field: 'sourceId', title: $t('lz.entity.toll.priceListCode'), width: 120, showOverflow: 'tooltip', sortable: true },
			{ field: 'name', title: $t('lz.entity.toll.priceList'), minWidth: 200, showOverflow: 'tooltip', sortable: true },
			{ field: 'startDate', title: $t('lz.label.base.fromDate'), width: 110, align: 'center', sortable: true, formatter: formatDate },
			{ field: 'endDate', title: $t('lz.label.base.toDate'), width: 110, align: 'center', sortable: true, formatter: formatDate },
			{ field: 'active', title: $t('lz.entity.base.status'), width: 130, align: 'center', slots: { default: 'row_active' } },
			{ field: 'note', title: $t('lz.entity.base.remark'), minWidth: 150, showOverflow: 'tooltip' },
		],
	},
	{
		proxyConfig: { autoLoad: true, ajax: { query: ({ page, sort }) => handleQueryPriceListApi(page, sort) } },
		pagerConfig: { pageSize: 50 },
		rowConfig: { isCurrent: true, isHover: true, keyField: 'id' },
		border: true,
		showOverflow: true,
	}
);

// Gọi API lấy dữ liệu bảng giá (phân trang)
const handleQueryPriceListApi = async (page: any, sort: any) => {
	return await getAPI(BangGiaApi).apiTollBanggiaPagePost({
		page: page.currentPage,
		pageSize: page.pageSize,
		field: sort?.order ? sort?.field : undefined,
		order: sort?.order ? sort?.order : undefined,
		descStr: 'desc',
		sourceId: state.queryParamsPriceList.sourceId,
		name: state.queryParamsPriceList.name,
		active: state.queryParamsPriceList.active,
	} as PageBangGiaInput);
};

// Tìm kiếm bảng giá
const handleQueryPriceList = async () => {
	optionsPriceListGrid.loading = true;
	await xGridPriceList.value?.commitProxy('query');
	optionsPriceListGrid.loading = false;
};

// Đặt lại điều kiện tìm kiếm bảng giá
const resetQueryPriceList = async () => {
	state.queryParamsPriceList.sourceId = undefined;
	state.queryParamsPriceList.name = undefined;
	state.queryParamsPriceList.active = undefined;
	await xGridPriceList.value?.commitProxy('reload');
};

// Chọn 1 bảng giá -> nạp chi tiết bảng giá tương ứng
const handlePriceListDetailByParent = async (scope: any) => {
	state.queryParamsPriceListDetail.priceListSourceId = scope.row.sourceId;
	await handleQueryPriceListDetail();
};

// Tuỳ chỉnh cột
function handlePriceListCustom() {
	xGridPriceList.value?.openCustom();
}

/* Bảng giá (TollPriceList): end ======================== */

/* Chi tiết bảng giá (TollPriceListDetail): begin ======================== */

// Cấu hình bảng dữ liệu chi tiết bảng giá
const optionsPriceListDetailGrid = useVxeTable<ChiTietBangGiaOutput>(
	{
		id: 'tollPriceListDetailGrid',
		name: 'tollPriceListDetailGrid',
		columns: [
			{ type: 'seq', title: $t('lz.label.base.idx'), width: 60, fixed: 'left' },
			{ field: 'code', title: $t('lz.entity.base.code'), minWidth: 140, showOverflow: 'tooltip', sortable: true },
			{ field: 'name', title: $t('lz.entity.base.name'), minWidth: 200, showOverflow: 'tooltip', sortable: true },
			{ field: 'price', title: $t('lz.entity.toll.price'), width: 130, align: 'right', headerAlign: 'center', sortable: true, formatter: ({ cellValue }: any) => (cellValue != null ? Number(cellValue).toLocaleString('vi-VN') : '') },
			{ field: 'vehicleTypeSourceId', title: $t('lz.entity.toll.vehicleTypeCode'), width: 140, showOverflow: 'tooltip' },
			{ field: 'priceListSourceId', title: $t('lz.entity.toll.priceListCode'), width: 140, showOverflow: 'tooltip' },
			{ field: 'note', title: $t('lz.entity.base.remark'), minWidth: 180, showOverflow: 'tooltip' },
		],
	},
	{
		proxyConfig: { autoLoad: false, ajax: { query: ({ page, sort }) => handleQueryPriceListDetailApi(page, sort) } },
		pagerConfig: { pageSize: 50 },
		rowConfig: { isCurrent: true, isHover: true },
		border: true,
		showOverflow: true,
	}
);

// Gọi API lấy dữ liệu chi tiết bảng giá (phân trang) - chỉ khi đã chọn bảng giá
const handleQueryPriceListDetailApi = async (page: any, sort: any) => {
	if ((state.queryParamsPriceListDetail.priceListSourceId ?? '') === '') return;

	return await getAPI(ChiTietBangGiaApi).apiTollChitietbanggiaPagePost({
		page: page.currentPage,
		pageSize: page.pageSize,
		field: sort?.order ? sort?.field : undefined,
		order: sort?.order ? sort?.order : undefined,
		descStr: 'desc',
		code: state.queryParamsPriceListDetail.code,
		name: state.queryParamsPriceListDetail.name,
		priceListSourceId: state.queryParamsPriceListDetail.priceListSourceId,
		vehicleTypeSourceId: state.queryParamsPriceListDetail.vehicleTypeSourceId,
		active: state.queryParamsPriceListDetail.active,
	} as PageChiTietBangGiaInput);
};

// Tìm kiếm chi tiết bảng giá
const handleQueryPriceListDetail = async () => {
	optionsPriceListDetailGrid.loading = true;
	await xGridPriceListDetail.value?.commitProxy('reload');
	optionsPriceListDetailGrid.loading = false;
};

// Đặt lại điều kiện tìm kiếm chi tiết bảng giá (giữ bảng giá đang chọn)
const resetQueryPriceListDetail = async () => {
	state.queryParamsPriceListDetail.code = undefined;
	state.queryParamsPriceListDetail.name = undefined;
	state.queryParamsPriceListDetail.vehicleTypeSourceId = undefined;
	state.queryParamsPriceListDetail.active = undefined;
	await xGridPriceListDetail.value?.commitProxy('reload');
};

// Tuỳ chỉnh cột
function handlePriceListDetailCustom() {
	xGridPriceListDetail.value?.openCustom();
}

/* Chi tiết bảng giá (TollPriceListDetail): end ======================== */

// Lấy dữ liệu bảng giá (danh mục) thủ công từ TCS (qua NATS); nhận kết quả -> nạp lại 2 lưới
const { isFetching, trigger } = useManualFetchToll({
	onSuccess: async () => {
		await handleQueryPriceList();
		await handleQueryPriceListDetail();
	},
});
const handleManualFetch = () => trigger(() => getAPI(BangGiaApi).apiTollBanggiaManualfetchPost({}));

// Trạng thái Online/Offline của ITS.Toll.Worker (theo heartbeat NATS mỗi 30s)
const { status: workerStatus, statusTagType: workerStatusTagType } = useTollWorkerStatus();
</script>

<style lang="scss" scoped>
.toll-bang-gia-container {
	height: 100%;
}
.full-height {
	display: flex;
	flex-direction: column;
	height: 100%;
}
.full-table {
	height: 100%;
}
</style>
