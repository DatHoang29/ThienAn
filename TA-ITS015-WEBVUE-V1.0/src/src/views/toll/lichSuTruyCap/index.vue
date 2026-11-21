
<template>
	<div class="toll-acslog-container">
		<el-card shadow="hover" :body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', height: '100%', alignItems: 'start' }">
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true" label-width="auto" style="flex: 1 1 0%" @submit.prevent="handleQuery">
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.startTime')" prop="startTime">
							<el-date-picker v-model="state.queryParams.startTime" type="datetime" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.startTime') })" :shortcuts="shortcuts" clearable class="w100" />
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.log.actionName')" prop="actionName">
							<el-select v-model="state.queryParams.actionName" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.log.actionName') })" clearable class="w100" @clear="state.queryParams.actionName = undefined">
								<el-option label="Login" value="Login" />
								<el-option label="Logout" value="Logout" />
							</el-select>
						</el-form-item>
					</el-col>
				</el-row>
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.endTime')" prop="endTime">
							<el-date-picker v-model="state.queryParams.endTime" type="datetime" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.endTime') })" :shortcuts="shortcuts" clearable class="w100" />
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.account')" prop="account">
							<el-input v-model="state.queryParams.account" clearable :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.account') })" @keyup.enter.native="handleQuery" />
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>

			<el-divider style="height: calc(100% - 5px); margin: 0 10px" direction="vertical" />

			<el-row>
				<SearchButton :loading="options.loading" @reset="resetQuery" @search="handleQuery" />
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
				<template #row_action="{ row }">
					<el-tag v-if="row.actionName === 'Login'" type="success">{{ row.actionName }}</el-tag>
					<el-tag v-else type="info">{{ row.actionName }}</el-tag>
				</template>
			</vxe-grid>
		</el-card>
	</div>
</template>

<script lang="ts" setup name="tollAccessHistory">
import { reactive, ref, defineAsyncComponent } from 'vue';
import { dayjs, ElMessage } from 'element-plus';
import { useDateTimeShortCust } from '/@/hooks/dateTimeShortCust';
import { VxeGridInstance } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { useI18n } from 'vue-i18n';
import { useNow } from '@vueuse/core';
import { getAPI } from '/@/utils/axios-utils';
import { LichSuThaoTacApi, LichSuThaoTacOutput, PageLichSuThaoTacInput } from '/@/api-services/toll';
import { useManualFetchToll } from '/@/transporter/hooks/useManualFetchToll';
import { useTollWorkerStatus } from '/@/transporter/hooks/useTollWorkerStatus';

const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));
const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));

const { t: $t } = useI18n();
const xGrid = ref<VxeGridInstance>();
const shortcuts = useDateTimeShortCust().map((item) => ({ text: $t(item.text), value: item.value }));
const now = useNow();

const state = reactive({
	queryParams: {
		startTime: new Date(new Date(now.value.getTime() - 3 * 24 * 60 * 60 * 1000).setHours(0, 0, 0, 0)) as Date | undefined,
		endTime: new Date(new Date(now.value).setHours(23, 59, 59, 999)) as Date | undefined,
		actionName: undefined as string | undefined,
		account: undefined as string | undefined,
	},
});

// Cấu hình bảng lịch sử truy cập (đồng bộ từ TCS - appaccesslog/accesshistorytcs)
const options = useVxeTable<LichSuThaoTacOutput>(
	{
		id: 'tollAccessHistory',
		name: 'TollAccessHistory',
		columns: [
			{ type: 'seq', title: $t('lz.label.base.idx'), width: 70, fixed: 'left' },
			{
				field: 'logDateTime',
				title: $t('lz.entity.log.logTime'),
				minWidth: 160,
				sortable: true,
				showOverflow: 'tooltip',
				formatter: ({ cellValue }: any) => (cellValue ? dayjs(cellValue).format('DD/MM/YYYY HH:mm:ss') : ''),
			},
			{ field: 'displayTitle', title: $t('lz.entity.log.displayTitle'), minWidth: 180, sortable: true, showOverflow: 'tooltip' },
			{
				field: 'actionName',
				title: $t('lz.entity.log.actionName'),
				width: 130,
				sortable: true,
				slots: { default: 'row_action' },
			},
			{ field: 'account', title: $t('lz.entity.base.account'), minWidth: 160, sortable: true, showOverflow: 'tooltip' },
			{
				field: 'lastSyncTime',
				title: $t('lz.entity.toll.lastSyncTime'),
				minWidth: 160,
				sortable: true,
				showOverflow: 'tooltip',
				formatter: ({ cellValue }: any) => (cellValue ? dayjs(cellValue).format('DD/MM/YYYY HH:mm:ss') : ''),
			},
		],
	},
	{
		proxyConfig: { autoLoad: true, ajax: { query: ({ page, sort }) => handleQueryApi(page, sort) } },
		pagerConfig: { pageSize: 50 },
	}
);

// Validate khoảng thời gian — dùng chung cho handleQuery và handleManualFetch (tương tự Toll Quản lý > Hậu kiểm)
const validateTimeRange = (): boolean => {
	if (!state.queryParams.startTime) {
		ElMessage.warning($t('lz.validation.base.required', { PropertyName: $t('lz.entity.base.startTime') }));
		return false;
	}
	if (!state.queryParams.endTime) {
		ElMessage.warning($t('lz.validation.base.required', { PropertyName: $t('lz.entity.base.endTime') }));
		return false;
	}
	if (new Date(state.queryParams.startTime) > new Date(state.queryParams.endTime)) {
		ElMessage.warning($t('lz.validation.toll.greaterThanStartTime', { EndTime: $t('lz.entity.base.endTime'), StartTime: $t('lz.entity.base.startTime') }));
		return false;
	}
	return true;
};

// Gọi API lấy dữ liệu (phân trang)
const handleQueryApi = async (page: any, sort: any) => {
	if (!state.queryParams.startTime || !state.queryParams.endTime) {
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
	if (new Date(state.queryParams.startTime).getTime() > new Date(state.queryParams.endTime).getTime()) {
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
	try {
		return await getAPI(LichSuThaoTacApi).apiTollLichsuthaotacPagePost({
			page: page.currentPage,
			pageSize: page.pageSize,
			field: sort?.order ? sort?.field : undefined,
			order: sort?.order ? sort?.order : undefined,
			descStr: 'desc',
			startTime: state.queryParams.startTime,
			endTime: state.queryParams.endTime,
			actionName: state.queryParams.actionName,
			account: state.queryParams.account,
		} as PageLichSuThaoTacInput);
	} catch (error) {
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
	if (!validateTimeRange()) {
		xGrid.value?.loadData([]);
		return;
	}
	options.loading = true;
	await xGrid.value?.commitProxy('reload');
	options.loading = false;
};

// Đặt lại điều kiện tìm kiếm
const resetQuery = async () => {
	state.queryParams.startTime = new Date(new Date(now.value.getTime() - 3 * 24 * 60 * 60 * 1000).setHours(0, 0, 0, 0));
	state.queryParams.endTime = new Date(new Date(now.value).setHours(23, 59, 59, 999));
	state.queryParams.actionName = undefined;
	state.queryParams.account = undefined;
	await xGrid.value?.commitProxy('reload');
};

// Tuỳ chỉnh cột
function handleCustom() {
	xGrid.value?.openCustom();
}

// Trạng thái Online/Offline của ITS.Toll.Worker (theo heartbeat NATS mỗi 30s)
const { status: workerStatus, statusTagType: workerStatusTagType } = useTollWorkerStatus();

// Lấy lịch sử truy cập thủ công từ TCS (qua NATS); cần khoảng thời gian; nhận kết quả → nạp lại lưới
const { isFetching, trigger } = useManualFetchToll({ onSuccess: () => handleQuery() });
const handleManualFetch = () => {
	if (!validateTimeRange()) return;

	trigger(() =>
		getAPI(LichSuThaoTacApi).apiTollLichsuthaotacManualfetchPost({
			fromDate: state.queryParams.startTime,
			toDate: state.queryParams.endTime,
		})
	);
};
</script>

<style lang="scss" scoped>
.toll-acslog-container {
	height: 100%;
}
.full-table {
	height: 100%;
}
</style>
