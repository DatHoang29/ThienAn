<template>
	<div class="toll-lve-container">
		<el-card shadow="hover" :body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', height: '100%', alignItems: 'start' }">
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true" label-width="auto" style="flex: 1 1 0%" @submit.prevent="handleQuery">
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.toll.ticketType')" prop="keyword" label-position="left">
							<el-input v-model="state.queryParams.keyword" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.toll.ticketType') })" clearable @keyup.enter.native="handleQuery" />
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>

			<el-divider style="height: calc(100% - 5px); margin: 0 10px" direction="vertical" />
			<el-row>
				<SearchButton :auth-search="'tollTicketType:page'" @search="handleQuery" @reset="resetQuery" :loading="options.loading" />
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
			</vxe-grid>
		</el-card>
	</div>
</template>

<script lang="ts" setup name="tollTicketType">
import { reactive, ref, defineAsyncComponent } from 'vue';
import { useI18n } from 'vue-i18n';
import { VxeGridInstance } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { getAPI } from '/@/utils/axios-utils';
import { LoaiVeApi, LoaiVeOutput, PageLoaiVeInput } from '/@/api-services/toll';
import { useManualFetchToll } from '/@/transporter/hooks/useManualFetchToll';
import { useTollWorkerStatus } from '/@/transporter/hooks/useTollWorkerStatus';

const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));
const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));

const { t: $t } = useI18n();
const xGrid = ref<VxeGridInstance>();
const state = reactive({
	queryParams: {
		keyword: undefined as string | undefined,
		groupCode: undefined as string | undefined,
		active: undefined as number | undefined,
	},
});

// Cấu hình bảng dữ liệu loại vé (đồng bộ từ ETC - chỉ đọc)
const options = useVxeTable<LoaiVeOutput>(
	{
		id: 'tollTicketType',
		name: 'TollTicketType',
		columns: [
			{ field: 'sourceId', title: $t('lz.entity.toll.ticketTypeCode'), minWidth: 140, showOverflow: 'tooltip', sortable: true },
			{ field: 'name', title: $t('lz.entity.base.name'), minWidth: 200, showOverflow: 'tooltip', align: 'center' },
			{ field: 'remark', fixed: 'right',  title: $t('lz.entity.base.remark'), minWidth: 180, showOverflow: 'tooltip' },
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
		return await getAPI(LoaiVeApi).apiTollLoaivePagePost({
			page: page.currentPage,
			pageSize: page.pageSize,
			field: sort?.order ? sort?.field : undefined,
			order: sort?.order ? sort?.order : undefined,
			descStr: 'desc',
			keyword: state.queryParams.keyword,
			groupCode: state.queryParams.groupCode,
			active: state.queryParams.active,
		} as PageLoaiVeInput);
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
	state.queryParams.groupCode = undefined;
	state.queryParams.active = undefined;
	await xGrid.value?.commitProxy('reload');
};

// Tuỳ chỉnh cột
function handleCustom() {
	xGrid.value?.openCustom();
}

// Lấy dữ liệu loại vé (danh mục) thủ công từ TCS (qua NATS); nhận kết quả -> nạp lại lưới
const { isFetching, trigger } = useManualFetchToll({ onSuccess: () => handleQuery() });
const handleManualFetch = () => trigger(() => getAPI(LoaiVeApi).apiTollLoaiveManualfetchPost({}));

// Trạng thái Online/Offline của ITS.Toll.Worker (theo heartbeat NATS mỗi 30s)
const { status: workerStatus, statusTagType: workerStatusTagType } = useTollWorkerStatus();
</script>

<style lang="scss" scoped>
.toll-lve-container {
	height: 100%;
}
.full-table {
	height: 100%;
}
</style>
