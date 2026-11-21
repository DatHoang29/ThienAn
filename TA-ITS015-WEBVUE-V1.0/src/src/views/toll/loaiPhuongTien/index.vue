<template>
	<div class="toll-lpt-container">
		<el-card shadow="hover" :body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', height: '100%', alignItems: 'start' }">
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true" label-width="auto" style="flex: 1 1 0%" @submit.prevent="handleQuery">
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.toll.vehicleType')" prop="keyword" label-position="left">
							<el-input v-model="state.queryParams.keyword" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.toll.vehicleType') })" clearable @keyup.enter.native="handleQuery" />
						</el-form-item>
					</el-col>
					<!-- <el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.status')" prop="active">
							<el-select v-model="state.queryParams.active" filterable clearable class="w100" @clear="state.queryParams.active = undefined" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.base.status') })">
								<el-option v-for="item in getConfigDataByCode(BaseConfigTypeEnum.SysStatusType)" :key="item.code" :label="item.name" :value="parseInt(item.code)" />
							</el-select>
						</el-form-item>
					</el-col> -->
				</el-row>
			</el-form>

			<el-divider style="height: calc(100% - 5px); margin: 0 10px" direction="vertical" />
			<el-row>
				<SearchButton :auth-search="'tollVehicle:page'" @search="handleQuery" @reset="resetQuery" :loading="options.loading" />
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
			</vxe-grid>
		</el-card>
	</div>
</template>

<script lang="ts" setup name="tollVehicle">
import { reactive, ref, defineAsyncComponent } from 'vue';
import { dayjs } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { VxeGridInstance } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { getAPI } from '/@/utils/axios-utils';
import { LoaiPhuongTienApi, LoaiPhuongTienOutput, PageLoaiPhuongTienInput } from '/@/api-services/toll';
import { BaseConfigTypeEnum } from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';
import { useManualFetchToll } from '/@/transporter/hooks/useManualFetchToll';
import { useTollWorkerStatus } from '/@/transporter/hooks/useTollWorkerStatus';

const getConfigItemByCode = useBaseConfig().getConfigItemByCode;
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));
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

// Cấu hình bảng dữ liệu loại phương tiện
const options = useVxeTable<LoaiPhuongTienOutput>(
	{
		id: 'tollVehicle',
		name: 'TollVehicleType',
		columns: [
			{ field: 'sourceId', title: $t('lz.entity.toll.vehicleTypeCode'), minWidth: 140, showOverflow: 'tooltip', sortable: true },
			{ field: 'name', title: $t('lz.entity.base.name'), minWidth: 200, showOverflow: 'tooltip', sortable: true, align: 'left' },
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
		return await getAPI(LoaiPhuongTienApi).apiTollLoaiphuongtienPagePost({
			page: page.currentPage,
			pageSize: page.pageSize,
			field: sort?.order ? sort?.field : undefined,
			order: sort?.order ? sort?.order : undefined,
			descStr: 'desc',
			keyword: state.queryParams.keyword,
			groupCode: state.queryParams.groupCode,
			active: state.queryParams.active,
		} as PageLoaiPhuongTienInput);
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

// Lấy dữ liệu loại phương tiện (danh mục) thủ công từ TCS (qua NATS); nhận kết quả -> nạp lại lưới
const { isFetching, trigger } = useManualFetchToll({ onSuccess: () => handleQuery() });
const handleManualFetch = () => trigger(() => getAPI(LoaiPhuongTienApi).apiTollLoaiphuongtienManualfetchPost({}));

// Trạng thái Online/Offline của ITS.Toll.Worker (theo heartbeat NATS mỗi 30s)
const { status: workerStatus, statusTagType: workerStatusTagType } = useTollWorkerStatus();
</script>

<style lang="scss" scoped>
.toll-lpt-container {
	height: 100%;
}
.full-table {
	height: 100%;
}
</style>
