<template>
	<div class="toll-lan-container">
		<el-card shadow="hover" :body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', height: '100%', alignItems: 'start' }">
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true" label-width="auto" style="flex: 1 1 0%" @submit.prevent="handleQuery">
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.toll.lane')" prop="keyword" label-position="left">
							<el-input v-model="state.queryParams.keyword" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.toll.lane') })" clearable @keyup.enter.native="handleQuery" />
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.tms.direction')" prop="direction" label-position="left">
							<el-select v-model="state.queryParams.direction" filterable clearable class="w100" @clear="state.queryParams.direction = undefined" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.tms.direction') })">
								<el-option v-for="item in getConfigDataByCode(BaseConfigTypeEnum.TollDirectionList)" :key="item.code" :label="item.name" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>

			<el-divider style="height: calc(100% - 5px); margin: 0 10px" direction="vertical" />
			<el-row>
				<SearchButton :auth-search="'tollLane:page'" @search="handleQuery" @reset="resetQuery" :loading="options.loading" />
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

<script lang="ts" setup name="tollLan">
import { reactive, ref, defineAsyncComponent } from 'vue';
import { dayjs } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { VxeGridInstance } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { getAPI } from '/@/utils/axios-utils';
import { LanApi, LanOutput, PageLanInput } from '/@/api-services/toll';
import { BaseConfigTypeEnum, TollDirectionEnum } from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';
import { useManualFetchToll } from '/@/transporter/hooks/useManualFetchToll';
import { useTollWorkerStatus } from '/@/transporter/hooks/useTollWorkerStatus';

const getConfigItemByCode = useBaseConfig().getConfigItemByCode;
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));
const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));

const { t: $t } = useI18n();
const xGrid = ref<VxeGridInstance>();
const state = reactive({
	queryParams: {
		keyword: undefined as string | undefined,
		stationId: undefined as string | undefined,
		direction: undefined as string | undefined,
	},
});

// Cấu hình bảng dữ liệu làn thu phí
const options = useVxeTable<LanOutput>(
	{
		id: 'tollLan',
		name: 'TollLane',
		columns: [
			{ field: 'laneId', title: $t('lz.entity.toll.laneId'), minWidth: 140, showOverflow: 'tooltip', sortable: true },
			{ field: 'name', title: $t('lz.entity.base.name'), minWidth: 180, showOverflow: 'tooltip', sortable: true },
			// Trạm: hiển thị tên qua danh mục cấu hình theo mã trạm
			{ field: 'stationId', title: $t('lz.entity.tms.station'), minWidth: 160, showOverflow: 'tooltip', sortable: true, formatter: ({ cellValue }: any) => getConfigItemByCode(BaseConfigTypeEnum.TollStationList, cellValue)?.name ?? cellValue },
			// Hướng: DB lưu giá trị TCS ('IN'/'OUT'), config dùng code '1'/'2' → cần normalize trước khi lookup
			{ field: 'direction', title: $t('lz.entity.tms.direction'), width: 130, sortable: true, formatter: ({ cellValue }: any) => {
				if (!cellValue) return '';
				const upper = cellValue?.toString()?.toUpperCase();
				const code = upper === 'IN' ? TollDirectionEnum.In : upper === 'OUT' ? TollDirectionEnum.Out : cellValue;
				return getConfigItemByCode(BaseConfigTypeEnum.TollDirectionList, code)?.name ?? cellValue;
			}},
			{ field: 'remark', title: $t('lz.entity.base.remark'), minWidth: 180, showOverflow: 'tooltip' },
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
		return await getAPI(LanApi).apiTollLanPagePost({
			page: page.currentPage,
			pageSize: page.pageSize,
			field: sort?.order ? sort?.field : undefined,
			order: sort?.order ? sort?.order : undefined,
			descStr: 'desc',
			keyword: state.queryParams.keyword,
			stationId: state.queryParams.stationId,
			direction: state.queryParams.direction,
		} as PageLanInput);
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
	state.queryParams.stationId = undefined;
	state.queryParams.direction = undefined;
	await xGrid.value?.commitProxy('reload');
};

// Tuỳ chỉnh cột
function handleCustom() {
	xGrid.value?.openCustom();
}

// Lấy dữ liệu làn thủ công từ TCS (qua NATS); nhận kết quả -> nạp lại lưới
const { isFetching, trigger } = useManualFetchToll({ onSuccess: () => handleQuery() });
const handleManualFetch = () => trigger(() => getAPI(LanApi).apiTollLanManualfetchPost({}));

// Trạng thái Online/Offline của ITS.Toll.Worker (theo heartbeat NATS mỗi 30s)
const { status: workerStatus, statusTagType: workerStatusTagType } = useTollWorkerStatus();
</script>

<style lang="scss" scoped>
.toll-lan-container {
	height: 100%;
}
.full-table {
	height: 100%;
}
</style>
