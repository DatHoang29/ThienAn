<template>
	<div class="sharedata-alertlog-container">
		<el-card shadow="hover"
			:body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', height: '100%', alignItems: 'start' }">
			<el-form class="sd-filter-form" :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true"
				label-width="auto" style="flex: 1 1 0%" label-position="left">
				<el-row :gutter="10">
					<el-col class="mb5 time-range-col" :xs="24" :sm="24" :md="10" :lg="8" :xl="8">
						<el-form-item :label="$t('lz.label.sharedataAlertLog.dateTime')" prop="dateRange">
							<el-date-picker
								v-model="state.dateRange"
								type="datetimerange"
								format="DD/MM/YYYY HH:mm:ss"
								value-format="YYYY-MM-DD HH:mm:ss"
								:start-placeholder="$t('lz.label.sharedataHistory.dateFrom')"
								:end-placeholder="$t('lz.label.sharedataHistory.dateTo')"
								:shortcuts="shortcuts"
								:disabled-date="disabledDate"
								@calendar-change="onCalendarChange"
								class="w100 time-range-picker"
							/>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="7" :lg="5" :xl="5">
						<el-form-item :label="$t('lz.label.sharedataAlertLog.severity')" prop="severity">
							<el-select v-model="state.queryParams.severity" :placeholder="$t('lz.label.sharedataAlertLog.severityPlaceholder')" clearable class="w100">
								<el-option v-for="item in SEVERITY_LIST" :key="item.code" :label="item.name" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="7" :lg="5" :xl="5">
						<el-form-item :label="$t('lz.label.sharedataAlertLog.source')" prop="alertSource">
							<el-select v-model="state.queryParams.alertSource" :placeholder="$t('lz.label.sharedataAlertLog.sourcePlaceholder')" clearable class="w100">
								<el-option v-for="item in SOURCE_LIST" :key="item.code" :label="item.name" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.label.sharedataAlertLog.processingStatus')" prop="acknowledged">
							<el-select v-model="state.queryParams.acknowledged" :placeholder="$t('lz.label.sharedataAlertLog.processingStatusAll')" clearable class="w100">
								<el-option v-for="item in ACK_LIST" :key="String(item.code)" :label="item.name" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.label.sharedataAlertLog.partner')" prop="partnerId">
							<el-select v-model="state.queryParams.partnerId" :placeholder="$t('lz.label.sharedataAlertLog.partnerPlaceholder')" filterable clearable class="w100">
								<el-option v-for="item in state.partnerList" :key="item.id" :label="codeNameLabel(item)" :value="item.id" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5 alert-code-col" :xs="24" :sm="12" :md="8" :lg="8" :xl="8">
						<el-form-item :label="$t('lz.label.sharedataAlertLog.alertCode')" prop="alertCode">
							<el-input class="alert-code-input" v-model="state.queryParams.alertCode" :placeholder="$t('lz.label.sharedataAlertLog.alertCodePlaceholder')" clearable
								@keyup.enter.native="handleQuery" />
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="24" :md="24" :lg="10" :xl="10">
						<el-form-item :label="$t('lz.label.sharedataAlertLog.content')" prop="keyword">
							<el-input v-model="state.queryParams.keyword" :placeholder="$t('lz.label.sharedataAlertLog.contentPlaceholder')" clearable
								@keyup.enter.native="handleQuery" />
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>

			<el-divider style="height: calc(100% - 5px); margin: 0 10px" direction="vertical" />

			<el-row>
				<SearchButton :auth-search="'sharedataAlertLog:page'" @search="handleQuery" @reset="resetQuery" :loading="options.loading" />
			</el-row>
		</el-card>

		<el-card class="full-table" shadow="hover" style="margin-top: 5px">
			<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents" @cell-dblclick="onCellDblclick"
				@checkbox-change="selectChangeEvent" @checkbox-all="selectChangeEvent">
				<!-- Nhật ký cảnh báo do hệ thống sinh ra: không có Thêm / Xóa, chỉ có Xác nhận đã xử lý. -->
				<template #toolbar_tools>
					<TableHeader :show-add="false" :show-delete="false" @refresh="handleQuery" @custom-column="handleCustom">
						<template #prefix>
							<el-tag v-if="state.summary.unackedCount" type="danger" effect="dark" class="mr10">
								{{ $t('lz.label.sharedataAlertLog.unackedCount', { count: state.summary.unackedCount }) }}
							</el-tag>
							<el-button type="success" plain icon="ele-Check" :disabled="!state.hasSelection"
								v-auth="'sharedataAlertLog:update'" @click="handleAckSelected">{{ $t('lz.button.sharedataAlertLog.ackSelected') }}</el-button>
							<el-button type="warning" plain icon="ele-CircleCheck" v-auth="'sharedataAlertLog:update'"
								@click="handleAckAll">{{ $t('lz.button.sharedataAlertLog.ackAll') }}</el-button>
						</template>
					</TableHeader>
				</template>
				<template #empty>
					<el-empty :image-size="200" />
				</template>

				<template #row_severity="{ row }">
					<TagInfo :config-item="getTagItem(SEVERITY_LIST, row.severity)" />
				</template>

				<template #row_source="{ row }">
					<TagInfo :config-item="getTagItem(SOURCE_LIST, row.alertSource)" />
				</template>

				<!-- Đã xử lý thì cho xem ai xác nhận và lúc nào qua tooltip. -->
				<template #row_ack="{ row }">
					<el-tooltip :disabled="!row.acknowledged" placement="top">
						<template #content>
							{{ row.ackBy || '—' }}<br />
							{{ row.ackAt ? formatDateTime(row.ackAt) : '' }}
						</template>
						<el-tag size="small" :type="row.acknowledged ? 'success' : 'warning'">
							{{ row.acknowledged ? $t('lz.label.sharedataAlertLog.ackStatusProcessed') : $t('lz.label.sharedataAlertLog.ackStatusUnprocessed') }}
						</el-tag>
					</el-tooltip>
				</template>

				<template #row_buttons="{ row }">
					<el-tooltip :content="$t('lz.button.sharedataAlertLog.detail')" placement="top" :auto-close="1000">
						<el-button icon="ele-InfoFilled" text type="primary" @click="openDetail(row)"> </el-button>
					</el-tooltip>
					<el-tooltip v-if="!row.acknowledged" :content="$t('lz.button.sharedataAlertLog.ack')" placement="top" :auto-close="1000">
						<el-button icon="ele-Check" text type="success" v-auth="'sharedataAlertLog:update'"
							@click="handleAck(row)"> </el-button>
					</el-tooltip>
				</template>
			</vxe-grid>
		</el-card>

		<RecordDetailDrawer ref="drawerRef" />
	</div>
</template>

<script lang="ts" setup name="sharedataAlertLog">
import { computed, onMounted, reactive, ref, defineAsyncComponent } from 'vue';
import { ElMessageBox, ElMessage } from 'element-plus';
import { useDateFormat } from '@vueuse/core';
import { DateTimeFormatEnum, BaseConfigTypeEnum } from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';
import { useI18n } from 'vue-i18n';
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { Local } from '/@/utils/storage';
import RecordDetailDrawer from './component/alertDetailDrawer.vue';
import { getAPI } from '/@/utils/axios-utils';
import { ShareDataAlertLogApi, ShareDataPartnerApi } from '/@/api-services/shareData';
import { ShareDataPageAlertLogInput, ShareDataPageAlertLogOutput } from '/@/api-services/shareData';
import { useSharedataFeConfig } from '/@/hooks/useSharedataFeConfig';
import { codeNameLabel, partnerNameOf } from '/@/utils/sharedata/labelHelper';
import { formatHistoryDate } from '/@/utils/sharedata/historyRange';

// Mức độ cảnh báo — BaseEnums.AlertSeverity: Warning = 0, Error = 1.
// TODO: chuyển sang bảng cấu hình dùng chung khi danh mục được seed.
const SEVERITY_LIST = computed(() => [
	{ code: 0, name: $t('lz.label.sharedataAlertLog.severityWarning'), tagType: 'warning' },
	{ code: 1, name: $t('lz.label.sharedataAlertLog.severityError'), tagType: 'danger' },
]);

// Nguồn phát sinh — BaseEnums.AlertSource: Session=0, Packet=1, Subscription=2, Protocol=3, Funnel=4.
const SOURCE_LIST = computed(() => [
	{ code: 0, name: $t('lz.label.sharedataAlertLog.sourceSession'), tagType: 'primary' },
	{ code: 1, name: $t('lz.label.sharedataAlertLog.sourcePacket'), tagType: 'info' },
	{ code: 2, name: $t('lz.label.sharedataAlertLog.sourceSubscription'), tagType: 'warning' },
	{ code: 3, name: $t('lz.label.sharedataAlertLog.sourceProtocol'), tagType: 'danger' },
	{ code: 4, name: $t('lz.label.sharedataAlertLog.sourceFunnel'), tagType: 'info' },
]);

// Trạng thái xử lý — cột Acknowledged là boolean nên dùng true/false thay cho enum số.
const ACK_LIST = computed(() => [
	{ code: false, name: $t('lz.label.sharedataAlertLog.ackStatusUnprocessed') },
	{ code: true, name: $t('lz.label.sharedataAlertLog.ackStatusProcessed') },
]);

/** Tra một dòng nhãn để đưa vào TagInfo; trả undefined nếu giá trị không có trong danh mục. */
const getTagItem = (list: any[], value: any) => list.find((i) => i.code === Number(value));

const { t: $t } = useI18n();
const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));
const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));
const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));

const xGrid = ref<VxeGridInstance>();
const drawerRef = ref<InstanceType<typeof RecordDetailDrawer>>();

const feConfig = useSharedataFeConfig();

/** Khoảng thời gian mặc định như Lịch sử chia sẻ: từ 00:00:00 của N ngày trước (history_range_default_days) tới giờ hiện tại. */
function defaultRange(): [string, string] {
	const to = new Date();
	const from = new Date(to.getTime() - feConfig.value.historyRange.defaultDays * 86400000);
	from.setHours(0, 0, 0, 0);
	return [formatHistoryDate(from), formatHistoryDate(to)];
}

/** Ngày đầu tiên vừa chọn trên lịch; dùng để giới hạn ngày còn lại trong history_range_max_days. */
const pickedDate = ref<Date | null>(null);
const onCalendarChange = (value: [Date, Date | null] | null) => {
	pickedDate.value = value?.[0] && !value[1] ? value[0] : null;
};

/** Chặn ngày tương lai và khoảng dài hơn số ngày tối đa (tính cả hai đầu). */
const disabledDate = (date: Date) => {
	const endOfToday = new Date();
	endOfToday.setHours(23, 59, 59, 999);
	if (date.getTime() > endOfToday.getTime()) return true;
	if (!pickedDate.value) return false;
	const maxSpanMs = (feConfig.value.historyRange.maxDays - 1) * 86400000;
	return Math.abs(date.getTime() - pickedDate.value.getTime()) > maxSpanMs;
};

const state = reactive({
	dateRange: defaultRange() as [string, string],
	queryParams: {
		severity: undefined as unknown as number,
		alertSource: undefined as unknown as number,
		partnerId: undefined as unknown as string,
		alertCode: undefined as unknown as string,
		keyword: undefined as unknown as string,
		acknowledged: undefined as unknown as boolean,
	},
	localPageParam: {
		pageSize: 50 as number,
		defaultSort: { field: 'occurredAt', order: 'desc', descStr: 'desc' } as any,
	},
	showBatchDelete: false,
	hasSelection: false,
	partnerList: [] as any,
	summary: { total: 0, warningCount: 0, errorCount: 0, unackedCount: 0 },
});

const shortcuts = computed(() => [
	{ text: $t('lz.label.sharedataAlertLog.range24h'), value: () => [new Date(Date.now() - 86400000), new Date()] },
	{ text: $t('lz.label.sharedataAlertLog.range7d'), value: () => [new Date(Date.now() - 7 * 86400000), new Date()] },
	{ text: $t('lz.label.sharedataAlertLog.range30d'), value: () => [new Date(Date.now() - 30 * 86400000), new Date()] },
]);

/** Định dạng mốc thời gian cho cột lưới; định dạng do useDateFormat lo theo DateTimeFormatEnum. */
const timeFmt = ({ cellValue }: any) => formatDateTime(cellValue);

/** Ngày + giờ đầy đủ; giữ chốt rỗng / không hợp lệ để trả '—' thay vì chuỗi rác. */
function formatDateTime(value?: string | Date | null): string {
	if (!value) return '—';
	const d = new Date(value);
	return Number.isNaN(d.getTime()) ? '—' : useDateFormat(d, DateTimeFormatEnum.DateTimeDefault).value;
}

/** Tra cứu mô tả mã lỗi ESH từ danh mục cấu hình hệ thống (sharedata_error_code). */
function alertCodeDescOf(alertCode?: string | null): string {
	if (!alertCode) return '—';
	const item = useBaseConfig().getConfigItemByCode(BaseConfigTypeEnum.SharedataErrorCode, String(alertCode));
	return item?.name || '—';
}

/* Backend không JOIN sang bảng đối tác nữa (truy vấn gọn hơn và tránh lỗi "Ambiguous column name"
   khi ORDER BY cột trùng tên giữa các bảng), nên tên đối tác được ghép tại FE từ danh mục
   `state.partnerList` đã nạp ở onMounted. */

// Local storage parameters
const localPageParamKey = 'localPageParam:sharedataAlertLog';
// Table parameter configuration
const options = useVxeTable<ShareDataPageAlertLogOutput>(
	{
		id: 'sharedataAlertLog',
		name: 'AlertLog',
		columns: [
			{ type: 'checkbox', fixed: 'left', width: 45 },
			{ type: 'seq', title: $t('lz.label.sharedataAlertLog.seq'), width: 60, fixed: 'left' },
			{ field: 'occurredAt', title: $t('lz.label.sharedataAlertLog.occurredTime'), width: 165, sortable: true, formatter: timeFmt },
			{ field: 'severity', title: $t('lz.label.sharedataAlertLog.severity'), width: 100, sortable: true, slots: { default: 'row_severity' } },
			{ field: 'alertSource', title: $t('lz.label.sharedataAlertLog.source'), width: 130, sortable: true, slots: { default: 'row_source' } },
			{ field: 'alertCode', title: $t('lz.label.sharedataAlertLog.alertCode'), width: 140, showOverflow: 'tooltip' },
			{ field: 'alertCodeDesc', title: $t('lz.label.sharedataAlertLog.alertDesc'), minWidth: 220, showOverflow: 'tooltip', formatter: ({ row }: any) => alertCodeDescOf(row?.alertCode) },
			{ field: 'partnerId', title: $t('lz.label.sharedataAlertLog.partner'), width: 200, showOverflow: 'tooltip', formatter: ({ cellValue }: any) => partnerNameOf(state.partnerList, cellValue, '—') },
			{ field: 'datatypeId', title: $t('lz.label.sharedataAlertLog.datatype'), width: 140, showOverflow: 'tooltip' },
			{ field: 'message', title: $t('lz.label.sharedataAlertLog.content'), minWidth: 260, showOverflow: 'tooltip' },
			{ field: 'acknowledged', title: $t('lz.label.sharedataAlertLog.processingStatus'), width: 130, sortable: true, slots: { default: 'row_ack' } },
			{ title: $t('lz.label.base.action'), fixed: 'right', width: 100, showOverflow: true, slots: { default: 'row_buttons' } },
		],
	},
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

// Page initialization
onMounted(async () => {
	state.localPageParam = Local.get(localPageParamKey) || state.localPageParam;
	const res = await getAPI(ShareDataPartnerApi).apiSharedataSharedatapartnerListGet();
	state.partnerList = res.data.result;
	await handleQuery();
});

/** Gom tham số lọc dùng chung cho truy vấn bảng, thống kê và xác nhận hàng loạt. */
const buildFilter = () => ({
	fromDate: state.dateRange?.[0] ? new Date(state.dateRange[0].replace(' ', 'T')) : undefined,
	toDate: state.dateRange?.[1] ? new Date(state.dateRange[1].replace(' ', 'T')) : undefined,
	severity: state.queryParams.severity,
	alertSource: state.queryParams.alertSource,
	partnerId: state.queryParams.partnerId,
	alertCode: state.queryParams.alertCode,
	keyword: state.queryParams.keyword,
	acknowledged: state.queryParams.acknowledged,
});

// Query API
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
	// Nếu sort.field bị null/undefined, lấy giá trị defaultSort
	const sortField = sort?.field || state.localPageParam.defaultSort.field;
	const sortOrder = sort?.order || state.localPageParam.defaultSort.order;

	const params = Object.assign(buildFilter(), { page: page.currentPage, pageSize: page.pageSize, field: sortField, order: sortOrder, descStr: 'desc' }) as ShareDataPageAlertLogInput;
	const [res] = await Promise.all([
		getAPI(ShareDataAlertLogApi).apiSharedataSharedataalertlogPagePost(params),
		loadSummary(),
	]);
	return res;
};

/** Nạp số liệu tổng hợp cho badge "chưa xử lý" theo đúng bộ lọc đang áp dụng. */
const loadSummary = async () => {
	const filter = buildFilter();
	const res = await getAPI(ShareDataAlertLogApi).apiSharedataSharedataalertlogSummaryPost({
		partnerId: filter.partnerId,
		alertSource: filter.alertSource as any,
		fromDate: filter.fromDate,
		toDate: filter.toDate,
	});
	const result = res.data.result;
	state.summary = {
		total: result?.total ?? 0,
		warningCount: result?.warningCount ?? 0,
		errorCount: result?.errorCount ?? 0,
		unackedCount: result?.unackedCount ?? 0,
	};
};

// Query operation
const handleQuery = async () => {
	options.loading = true;
	await xGrid.value?.commitProxy('reload');
	options.loading = false;
	selectChangeEvent();
};

// Reset operation
const resetQuery = async () => {
	state.queryParams.severity = undefined as unknown as number;
	state.queryParams.alertSource = undefined as unknown as number;
	state.queryParams.partnerId = undefined as unknown as string;
	state.queryParams.alertCode = undefined as unknown as string;
	state.queryParams.keyword = undefined as unknown as string;
	state.queryParams.acknowledged = undefined as unknown as boolean;
	state.dateRange = defaultRange();
	await xGrid.value?.commitProxy('reload');
};

function onCellDblclick({ row }: any) {
	openDetail(row);
}

/**
 * Mở drawer chi tiết. Backend không trả PartnerName nữa nên ghép tên tại đây
 * rồi truyền sang drawer, để drawer không phải tự nạp danh mục đối tác.
 */
function openDetail(row: any) {
	drawerRef.value?.open({ ...row, partnerName: partnerNameOf(row.partnerId) });
}

/** Xác nhận một bản ghi. API nhận mảng ID nên truyền mảng 1 phần tử. */
const handleAck = async (row: any) => {
	await getAPI(ShareDataAlertLogApi).apiSharedataSharedataalertlogAcknowledgePost({ ids: [row.id] });
	handleQuery();
	ElMessage.success($t('lz.message.sharedataAlertLog.ackOneSuccess'));
};

const handleAckSelected = async () => {
	const ids = getCheckedRecords().map((r: any) => r.id).filter(Boolean);
	if (ids.length === 0) return;
	await getAPI(ShareDataAlertLogApi).apiSharedataSharedataalertlogAcknowledgePost({ ids });
	handleQuery();
	ElMessage.success($t('lz.message.sharedataAlertLog.ackMultiSuccess', { count: ids.length }));
};

/** Xác nhận toàn bộ theo bộ lọc đang áp dụng (đối tác + mức độ + mốc thời gian cuối). */
const handleAckAll = () => {
	const filter = buildFilter();
	ElMessageBox.confirm($t('lz.message.sharedataAlertLog.confirmAckAll'), $t('lz.message.base.warning'), {
		confirmButtonText: $t('lz.button.base.confirm'),
		cancelButtonText: $t('lz.button.base.cancel'),
		type: 'warning',
	})
		.then(async () => {
			const res = await getAPI(ShareDataAlertLogApi).apiSharedataSharedataalertlogAcknowledgeallPost({
				partnerId: filter.partnerId,
				severity: filter.severity as any,
				toDate: filter.toDate,
			});
			const count = res.data.result ?? 0;
			handleQuery();
			ElMessage.success(count ? $t('lz.message.sharedataAlertLog.ackAllSuccessWithCount', { count }) : $t('lz.message.sharedataAlertLog.noMoreUnprocessedRecords'));
		})
		.catch(() => { });
};

// Custom column
function handleCustom() {
	xGrid.value?.openCustom();
}
// Get selected records
function getCheckedRecords() {
	return xGrid.value?.getCheckboxRecords() ?? [];
}

function selectChangeEvent() {
	state.hasSelection = getCheckedRecords().length > 0;
}

// Table events
const gridEvents: VxeGridListeners<ShareDataPageAlertLogOutput> = {
	// Only valid when pager-config is configured, this event will be triggered when the page changes
	async pageChange({ pageSize }) {
		state.localPageParam.pageSize = pageSize;
		Local.set(localPageParamKey, state.localPageParam);
	},
	// This event will be triggered when the sorting condition changes
	async sortChange({ field, order }) {
		state.localPageParam.defaultSort = { field: field, order: order!, descStr: 'desc' };
		Local.set(localPageParamKey, state.localPageParam);
	},
};
</script>

<style scoped lang="scss">
/* Chuẩn hóa VXE-Grid Loading & Element Loading — 8-dots spinner bự vừa vặn */
:deep(.vxe-table--loading) {
	.vxe-loading--default-icon {
		display: inline-block;
		width: 50px;
		height: 50px;
		font-size: 0;
		color: transparent;
		background-color: var(--el-color-primary, #409eff);
		mask-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 50 50'%3E%3Ccircle cx='25' cy='6' r='3.4' fill='white' opacity='1'/%3E%3Ccircle cx='38.44' cy='11.56' r='3.4' fill='white' opacity='0.875'/%3E%3Ccircle cx='44' cy='25' r='3.4' fill='white' opacity='0.75'/%3E%3Ccircle cx='38.44' cy='38.44' r='3.4' fill='white' opacity='0.625'/%3E%3Ccircle cx='25' cy='44' r='3.4' fill='white' opacity='0.5'/%3E%3Ccircle cx='11.56' cy='38.44' r='3.4' fill='white' opacity='0.375'/%3E%3Ccircle cx='6' cy='25' r='3.4' fill='white' opacity='0.25'/%3E%3Ccircle cx='11.56' cy='11.56' r='3.4' fill='white' opacity='0.125'/%3E%3C/svg%3E");
		-webkit-mask-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 50 50'%3E%3Ccircle cx='25' cy='6' r='3.4' fill='white' opacity='1'/%3E%3Ccircle cx='38.44' cy='11.56' r='3.4' fill='white' opacity='0.875'/%3E%3Ccircle cx='44' cy='25' r='3.4' fill='white' opacity='0.75'/%3E%3Ccircle cx='38.44' cy='38.44' r='3.4' fill='white' opacity='0.625'/%3E%3Ccircle cx='25' cy='44' r='3.4' fill='white' opacity='0.5'/%3E%3Ccircle cx='11.56' cy='38.44' r='3.4' fill='white' opacity='0.375'/%3E%3Ccircle cx='6' cy='25' r='3.4' fill='white' opacity='0.25'/%3E%3Ccircle cx='11.56' cy='11.56' r='3.4' fill='white' opacity='0.125'/%3E%3C/svg%3E");
		mask-repeat: no-repeat;
		-webkit-mask-repeat: no-repeat;
		mask-position: center;
		-webkit-mask-position: center;
		mask-size: contain;
		-webkit-mask-size: contain;
		animation: sharedata-roll 0.9s linear infinite;
		transform-origin: center;
		will-change: transform;

		&::before {
			display: none;
		}
	}
	.vxe-loading--text {
		font-size: 14px;
		font-weight: 500;
		color: var(--el-color-primary, #409eff);
		padding-top: 10px;
	}
}

:deep(.el-loading-mask) {
	background-color: rgba(255, 255, 255, 0.78);
	backdrop-filter: blur(1px);
	border-radius: inherit;
	transition: opacity 0.25s ease;
	z-index: 100;
}

:deep(.el-loading-spinner) {
	top: 50%;
	transform: translateY(-50%);
	margin-top: 0;
	width: 100%;
	text-align: center;
	position: absolute;

	.circular {
		display: inline-block;
		width: 50px;
		height: 50px;
		background-color: var(--el-color-primary, #409eff);
		mask-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 50 50'%3E%3Ccircle cx='25' cy='6' r='3.4' fill='white' opacity='1'/%3E%3Ccircle cx='38.44' cy='11.56' r='3.4' fill='white' opacity='0.875'/%3E%3Ccircle cx='44' cy='25' r='3.4' fill='white' opacity='0.75'/%3E%3Ccircle cx='38.44' cy='38.44' r='3.4' fill='white' opacity='0.625'/%3E%3Ccircle cx='25' cy='44' r='3.4' fill='white' opacity='0.5'/%3E%3Ccircle cx='11.56' cy='38.44' r='3.4' fill='white' opacity='0.375'/%3E%3Ccircle cx='6' cy='25' r='3.4' fill='white' opacity='0.25'/%3E%3Ccircle cx='11.56' cy='11.56' r='3.4' fill='white' opacity='0.125'/%3E%3C/svg%3E");
		-webkit-mask-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 50 50'%3E%3Ccircle cx='25' cy='6' r='3.4' fill='white' opacity='1'/%3E%3Ccircle cx='38.44' cy='11.56' r='3.4' fill='white' opacity='0.875'/%3E%3Ccircle cx='44' cy='25' r='3.4' fill='white' opacity='0.75'/%3E%3Ccircle cx='38.44' cy='38.44' r='3.4' fill='white' opacity='0.625'/%3E%3Ccircle cx='25' cy='44' r='3.4' fill='white' opacity='0.5'/%3E%3Ccircle cx='11.56' cy='38.44' r='3.4' fill='white' opacity='0.375'/%3E%3Ccircle cx='6' cy='25' r='3.4' fill='white' opacity='0.25'/%3E%3Ccircle cx='11.56' cy='11.56' r='3.4' fill='white' opacity='0.125'/%3E%3C/svg%3E");
		mask-repeat: no-repeat;
		-webkit-mask-repeat: no-repeat;
		mask-position: center;
		-webkit-mask-position: center;
		mask-size: contain;
		-webkit-mask-size: contain;
		animation: sharedata-roll 0.9s linear infinite;
		transform-origin: center;
		will-change: transform;

		.path {
			display: none;
		}
	}

	.el-loading-text {
		color: var(--el-color-primary, #409eff);
		font-size: 14px;
		font-weight: 500;
		letter-spacing: 0.3px;
		margin: 10px 0 0;
		line-height: 1.4;
	}

	&:not(:has(.el-loading-text))::after {
		content: 'Đang tải...';
		display: block;
		color: var(--el-color-primary, #409eff);
		font-size: 14px;
		font-weight: 500;
		letter-spacing: 0.3px;
		margin-top: 10px;
		line-height: 1.4;
	}
}

@keyframes sharedata-roll {
	from {
		transform: rotate(0deg);
	}
	to {
		transform: rotate(360deg);
	}
}

/* Bộ lọc: cột không co nhỏ hơn nội dung (nhãn + ô), ô nhập / ô chọn rộng tối thiểu đủ chữ gợi ý khoảng 14 ký tự; thiếu chỗ thì ô lọc tự xuống hàng thay vì bị bóp */
.sd-filter-form {
	:deep(.el-form-item__content > .el-input) {
		min-width: 120px;
	}

	:deep(.el-form-item__content > .el-select) {
		min-width: 120px;
	}

	/* Chữ gợi ý mã cảnh báo dài */
	:deep(.el-form-item__content > .el-input.alert-code-input) {
		min-width: 160px;
	}
}

/* Ô mã cảnh báo đủ rộng cho chữ gợi ý dài; thiếu chỗ thì các ô sau tự xuống hàng */
.alert-code-col {
	min-width: min(100%, 320px);
}

/* Bộ lọc Thời gian: vừa khít định dạng ngày giờ DD/MM/YYYY HH:mm:ss, icon và chữ cân đối */
.time-range-col {
	min-width: min(100%, 440px);

	:deep(.time-range-picker) {
		min-width: 320px;
		max-width: 320px;

		.el-range-input {
			width: 125px;
			flex: 1 1 auto;
			text-align: center;
		}

		.el-range-separator {
			flex: 0 0 auto;
			padding: 0 4px;
		}
	}
}
</style>
