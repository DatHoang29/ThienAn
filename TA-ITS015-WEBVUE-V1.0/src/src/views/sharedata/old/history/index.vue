<template>
	<div class="esh-page">
		<el-tabs v-model="logTypeTab" class="history-tabs">
			<el-tab-pane name="config" :label="$t('lz.label.sharedataHistory.logConfig')" />
			<el-tab-pane name="transfer" :label="$t('lz.label.sharedataHistory.logTransfer')" />
		</el-tabs>

		<!-- Bộ lọc -->
		<el-card shadow="hover"
			:body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', alignItems: 'start' }">
			<el-form class="sd-filter-form" :model="state.query" ref="queryForm" :show-message="false" label-width="auto" style="flex: 1 1 0%"
				label-position="left">
				<el-row :gutter="10">
					<el-col class="mb5 time-range-col" :xs="24" :sm="24" :md="9" :lg="8" :xl="7">
						<el-form-item :label="$t('lz.label.base.time')" :error="state.rangeError" :show-message="true">
							<el-date-picker
								v-model="state.dateRange"
								type="datetimerange"
								class="w100 time-range-picker"
								format="DD/MM/YYYY HH:mm:ss"
								value-format="YYYY-MM-DD HH:mm:ss"
								:start-placeholder="$t('lz.entity.base.startTime')"
								:end-placeholder="$t('lz.entity.base.endTime')"
								:shortcuts="shortcuts"
								:disabled-date="disableFutureDate"
								:clearable="false"
								@change="handleSearch"
							/>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="8" :md="5" :lg="5" :xl="6">
						<el-form-item :label="$t('lz.label.sharedataHistory.partner')">
							<el-select v-model="state.query.partnerId" clearable class="w100" :placeholder="$t('lz.label.sharedataHistory.partnerAll')">
								<el-option v-for="v in partners" :key="v.id" :label="codeNameLabel(v)" :value="v.id" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="8" :md="5" :lg="5" :xl="5">
						<el-form-item :label="$t('lz.label.sharedataHistory.action')">
							<el-select v-model="state.query.action" clearable class="w100" :placeholder="$t('lz.label.sharedataHistory.actionAll')">
								<el-option v-for="a in actionOptions" :key="a.value" :label="a.label" :value="a.value" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="8" :md="5" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.label.sharedataHistory.result')">
							<el-select v-model="state.query.success" clearable class="w100" :placeholder="$t('lz.label.base.all')">
								<el-option :label="$t('lz.label.sharedataHistory.success')" :value="SUCCESS_OK" />
								<el-option :label="$t('lz.label.sharedataHistory.failed')" :value="SUCCESS_FAIL" />
							</el-select>
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>
			<el-divider style="height: calc(100% - 5px); margin: 0 10px" direction="vertical" />
			<el-row>
				<SearchButton @search="handleSearch" @reset="resetQuery" :loading="options.loading" />
			</el-row>
		</el-card>

		<!-- Thống kê nhanh -->
		<div class="stat-row">
			<el-card shadow="hover" class="stat-card" :body-style="{ padding: '10px 14px' }">
				<div class="stat"><span class="lbl">{{ $t('lz.label.sharedataHistory.totalRecords') }}</span><span class="val">{{ stats.total }}</span></div>
			</el-card>
			<el-card shadow="hover" class="stat-card" :body-style="{ padding: '10px 14px' }">
				<div class="stat"><span class="lbl">{{ $t('lz.label.sharedataHistory.success') }}</span><span class="val text-success">{{ stats.successCount }}</span></div>
			</el-card>
			<el-card shadow="hover" class="stat-card" :body-style="{ padding: '10px 14px' }">
				<div class="stat"><span class="lbl">{{ $t('lz.label.sharedataHistory.failed') }}</span><span class="val text-danger">{{ stats.failedCount }}</span></div>
			</el-card>
			<template v-if="isTransfer">
				<el-card shadow="hover" class="stat-card" :body-style="{ padding: '10px 14px' }">
					<div class="stat"><span class="lbl">{{ $t('lz.label.sharedataHistory.sentPackets') }}</span><span class="val">{{ stats.sentCount }}</span></div>
				</el-card>
				<el-card shadow="hover" class="stat-card" :body-style="{ padding: '10px 14px' }">
					<div class="stat"><span class="lbl">{{ $t('lz.label.sharedataHistory.receivedPackets') }}</span><span class="val">{{ stats.receivedCount }}</span></div>
				</el-card>
			</template>
		</div>

		<!-- Bảng nhật ký -->
		<el-card class="full-table" shadow="hover" style="margin-top: 5px">
			<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" @cell-dblclick="onCellDblclick">
				<template #toolbar_tools>
					<el-tooltip :content="$t('lz.button.base.refresh')" placement="top" :auto-close="1000">
						<el-button icon="ele-Refresh" circle @click="handleQuery" />
					</el-tooltip>
				</template>

				<template #empty><el-empty :description="$t('lz.label.sharedataHistory.noData')" :image-size="140" /></template>

				<template #row_action="{ row }">
					<el-tag size="small" :type="actionTagType(row.action)">{{ actionLabel(row.action) }}</el-tag>
				</template>
				<template #row_status="{ row }">
					<el-tag size="small" :type="Number(row.success) === SUCCESS_FAIL ? 'danger' : 'success'">{{
						Number(row.success) === SUCCESS_FAIL ? $t('lz.label.sharedataHistory.failed') : $t('lz.label.sharedataHistory.success') }}</el-tag>
				</template>
				<template #row_direction="{ row }">
					<el-tag v-if="row.transferDirection !== null && row.transferDirection !== undefined" size="small"
						:type="Number(row.transferDirection) === TRANSFER_SND ? 'success' : 'primary'">{{
							Number(row.transferDirection) === TRANSFER_SND ? $t('lz.label.sharedataHistory.sendOut') : $t('lz.label.sharedataHistory.receiveIn') }}</el-tag>
					<span v-else>—</span>
				</template>
				<template #row_datatype="{ row }">
					<span>{{ datatypeLabel(packetList, row.datatypeId) }}</span>
				</template>
				<template #row_buttons="{ row }">
					<el-tooltip :content="$t('lz.button.base.detail')" placement="top" :auto-close="1000">
						<el-button text type="primary" icon="ele-InfoFilled" @click="openDetail(row)">{{ $t('lz.label.sharedataHistory.detail') }}</el-button>
					</el-tooltip>
				</template>
			</vxe-grid>
		</el-card>

		<ActivityDetailDialog ref="detailDialogRef" />
	</div>
</template>
<script lang="ts" setup name="sharedataHistory">
import { computed, defineAsyncComponent, onMounted, reactive, ref, watch } from 'vue';
import { ElMessage, dayjs } from 'element-plus';
import { VxeGridInstance, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { getAPI } from '/@/utils/axios-utils';
import { ShareDataActivityLogApi, ShareDataPacketApi, ShareDataPartnerApi } from '/@/api-services/shareData';
import type { ShareDataActivityLogOutput, ShareDataPartnerOutput, ShareDataPageActivityLogInput } from '/@/api-services/shareData';
import { DateTimeFormatEnum } from '/@/types/enums';
import ActivityDetailDialog from './component/activityDetailDialog.vue';
import { useI18n } from 'vue-i18n';
import { useDateFormat } from '@vueuse/core';
import { defaultHistoryRange, validateHistoryRange } from '/@/utils/sharedata/historyRange';
import { codeNameLabel, datatypeLabel } from '/@/utils/sharedata/labelHelper';

const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));
const { t: $t } = useI18n();

const LOG_TYPE_CONFIG = 0;
const LOG_TYPE_TRANSFER = 1;

const SUCCESS_FAIL = 0;
const SUCCESS_OK = 1;

const TRANSFER_SND = 0;

const ACTION = {
	Create: 0,
	Update: 1,
	Delete: 2,
	Connect: 3,
	Disconnect: 4,
	Pause: 5,
	Resume: 6,
	Cancel: 7,
	Approve: 8,
	Reject: 9,
	Send: 10,
	Receive: 11,
	Export: 12,
} as const;

const ACTION_LABELS: Record<number, string> = {
	[ACTION.Create]: $t('lz.label.sharedataHistory.actionCreate'),
	[ACTION.Update]: $t('lz.label.sharedataHistory.actionUpdate'),
	[ACTION.Delete]: $t('lz.label.sharedataHistory.actionDelete'),
	[ACTION.Connect]: $t('lz.label.sharedataHistory.actionConnect'),
	[ACTION.Disconnect]: $t('lz.label.sharedataHistory.actionDisconnect'),
	[ACTION.Pause]: $t('lz.label.sharedataHistory.actionPause'),
	[ACTION.Resume]: $t('lz.label.sharedataHistory.actionResume'),
	[ACTION.Cancel]: $t('lz.label.sharedataHistory.actionCancelSub'),
	[ACTION.Approve]: $t('lz.label.sharedataHistory.actionApprove'),
	[ACTION.Reject]: $t('lz.label.sharedataHistory.actionReject'),
	[ACTION.Send]: $t('lz.label.sharedataHistory.actionSend'),
	[ACTION.Receive]: $t('lz.label.sharedataHistory.actionReceive'),
	[ACTION.Export]: $t('lz.label.sharedataHistory.actionExportFile'),
};

const defaultRange = defaultHistoryRange;

const detailDialogRef = ref<InstanceType<typeof ActivityDetailDialog>>();
const xGrid = ref<VxeGridInstance>();
const partners = ref<ShareDataPartnerOutput[]>([]);

const state = reactive({
	logType: LOG_TYPE_CONFIG as number,
	dateRange: defaultRange() as [string, string],
	rangeError: '',
	query: {
		partnerId: '',
		action: undefined as unknown as number,
		success: undefined as unknown as number,
	},
});

const stats = reactive({ total: 0, successCount: 0, failedCount: 0, sentCount: 0, receivedCount: 0 });

const isTransfer = computed(() => state.logType === LOG_TYPE_TRANSFER);
const logTypeTab = computed<string>({
	get: () => (state.logType === LOG_TYPE_TRANSFER ? 'transfer' : 'config'),
	set: (v) => {
		state.logType = v === 'transfer' ? LOG_TYPE_TRANSFER : LOG_TYPE_CONFIG;
	},
});

/**
 * Nhật ký chỉ ghi lại việc đã xảy ra, nên ngày sau hôm nay luôn ra lưới rỗng.
 * Chặn ngay ở lịch để người dùng khỏi chọn rồi tự hỏi vì sao không có dữ liệu.
 */
const disableFutureDate = (date: Date) => date.getTime() > dayjs().endOf('day').valueOf();

const shortcuts = [
	{ text: $t('lz.label.sharedataHistory.range24h'), value: () => [dayjs().subtract(24, 'hour').toDate(), new Date()] },
	{ text: $t('lz.label.sharedataHistory.range7d'), value: () => [dayjs().subtract(7, 'day').startOf('day').toDate(), new Date()] },
	{ text: $t('lz.label.sharedataHistory.range30d'), value: () => [dayjs().subtract(30, 'day').startOf('day').toDate(), new Date()] },
];

/** Danh sách hành động đổi theo tab đang xem (rút gọn Approve/Reject). */
const actionOptions = computed(() =>
	isTransfer.value
		? [
			{ value: ACTION.Send, label: $t('lz.label.sharedataHistory.actionSendPacket') },
			{ value: ACTION.Receive, label: $t('lz.label.sharedataHistory.actionReceivePacket') },
			{ value: ACTION.Export, label: $t('lz.label.sharedataHistory.actionExportFile') },
		]
		: [
			{ value: ACTION.Create, label: $t('lz.label.sharedataHistory.actionCreate') },
			{ value: ACTION.Update, label: $t('lz.label.sharedataHistory.actionUpdate') },
			{ value: ACTION.Delete, label: $t('lz.label.sharedataHistory.actionDelete') },
			{ value: ACTION.Connect, label: $t('lz.label.sharedataHistory.actionConnect') },
			{ value: ACTION.Disconnect, label: $t('lz.label.sharedataHistory.actionDisconnect') },
			{ value: ACTION.Pause, label: $t('lz.label.sharedataHistory.actionPause') },
			{ value: ACTION.Resume, label: $t('lz.label.sharedataHistory.actionResume') },
			{ value: ACTION.Cancel, label: $t('lz.label.sharedataHistory.actionCancelSub') },
		]
);

function actionLabel(action: any): string {
	if (action === null || action === undefined || action === '') return '—';
	return ACTION_LABELS[Number(action)] ?? String(action);
}

function actionTagType(action: any): 'success' | 'warning' | 'danger' | 'info' | 'primary' {
	const a = Number(action);
	if (a === ACTION.Create || a === ACTION.Approve || a === ACTION.Resume || a === ACTION.Connect) return 'success';
	if (a === ACTION.Delete || a === ACTION.Reject || a === ACTION.Cancel || a === ACTION.Disconnect) return 'danger';
	if (a === ACTION.Update || a === ACTION.Pause) return 'warning';
	return 'info';
}

/**
 * Tên gói tin lấy thẳng từ danh sách gói tin (ShareDataPacket).
 * DatatypeId lưu ID gói tin nên tra theo ID; không còn dùng danh mục shareData_type.
 */
const packetList = ref<any[]>([]);

const loadPacketList = async () => {
	if (packetList.value.length) return;
	try {
		const res = await getAPI(ShareDataPacketApi).apiSharedataSharedatapacketListGet();
		packetList.value = res.data.result ?? [];
	} catch {
		packetList.value = [];
	}
};

/** Ngày + giờ đầy đủ; định dạng do useDateFormat lo, chỉ giữ chốt rỗng / không hợp lệ để trả '—'. */
const fmt = (s?: string) => {
	if (!s) return '—';
	const d = new Date(s);
	return Number.isNaN(d.getTime()) ? '—' : useDateFormat(d, DateTimeFormatEnum.DateTimeDefault).value;
};



/** Cột tab "Cấu hình": ai làm gì, trên bản ghi nào, đổi trường nào. */
function configColumns(): VxeGridPropTypes.Columns {
	return [
		{ type: 'seq', title: $t('lz.label.sharedataHistory.seq'), width: 60, fixed: 'left' },
		{ field: 'occurredAt', title: $t('lz.label.sharedataHistory.time'), width: 170, sortable: true, formatter: ({ row }: any) => fmt(row.occurredAt) },
		{ field: 'action', title: $t('lz.label.sharedataHistory.action'), width: 120, slots: { default: 'row_action' } },
		{ field: 'targetName', title: $t('lz.label.sharedataHistory.target'), minWidth: 200, showOverflow: 'tooltip' },
		{ field: 'description', title: $t('lz.label.sharedataHistory.content'), minWidth: 300, showOverflow: 'tooltip' },
		{ field: 'changedFields', title: $t('lz.label.sharedataHistory.changedFields'), minWidth: 180, showOverflow: 'tooltip' },
		{ field: 'operatorName', title: $t('lz.label.sharedataHistory.operator'), width: 140, showOverflow: 'tooltip' },
		{ field: 'success', title: $t('lz.label.sharedataHistory.result'), width: 110, slots: { default: 'row_status' } },
		{ title: $t('lz.label.sharedataHistory.operations'), width: 110, fixed: 'right', slots: { default: 'row_buttons' } },
	];
}

/** Cột tab "Truyền nhận": gói tin gửi/nhận theo đối tác và loại dữ liệu. */
function transferColumns(): VxeGridPropTypes.Columns {
	return [
		{ type: 'seq', title: $t('lz.label.sharedataHistory.seq'), width: 60, fixed: 'left' },
		{ field: 'occurredAt', title: $t('lz.label.sharedataHistory.time'), width: 170, sortable: true, formatter: ({ row }: any) => fmt(row.occurredAt) },
		{ field: 'transferDirection', title: $t('lz.label.sharedataHistory.direction'), width: 100, slots: { default: 'row_direction' } },
		{ field: 'partnerName', title: $t('lz.label.sharedataHistory.partner'), minWidth: 170, showOverflow: 'tooltip' },
		{
			field: 'datatypeId', title: $t('lz.label.sharedataHistory.datatype'), minWidth: 220,
			slots: { default: 'row_datatype' },
			formatter: ({ row }: any) => datatypeLabel(packetList.value, row.datatypeId),
		},
		{ field: 'serialNbr', title: $t('lz.label.sharedataHistory.serial'), width: 90 },
		{ field: 'pduType', title: $t('lz.label.sharedataHistory.pduType'), width: 130, showOverflow: 'tooltip' },
		{ field: 'format', title: $t('lz.label.sharedataHistory.format'), width: 100 },
		{ field: 'byteSize', title: $t('lz.label.sharedataHistory.byteSize'), width: 120 },
		{ field: 'recordCount', title: $t('lz.label.sharedataHistory.recordCount'), width: 110 },
		{ field: 'success', title: $t('lz.label.sharedataHistory.result'), width: 110, slots: { default: 'row_status' } },
		{ title: $t('lz.label.sharedataHistory.operations'), width: 110, fixed: 'right', slots: { default: 'row_buttons' } },
	];
}

const options = useVxeTable<ShareDataActivityLogOutput>(
	{
		id: 'sharedataActivityLog',
		name: 'ShareDataActivityLog',
		columns: configColumns(),
	},
	{
		proxyConfig: { autoLoad: false, ajax: { query: ({ page, sort }) => handleQueryApi(page, sort) } },
		// Nhật ký luôn xem mới nhất trước. Bắt buộc phải có sort mặc định
		sortConfig: { defaultSort: { field: 'occurredAt', order: 'desc' } },
		pagerConfig: { pageSize: 50, pageSizes: [50, 100, 200, 300] },
		toolbarConfig: { export: false },
		rowConfig: { keyField: 'id', isHover: true },
	}
);

function syncColumns() {
	options.columns = (isTransfer.value ? transferColumns() : configColumns()) as any;
}

type HistoryResponse = Pick<Awaited<ReturnType<ShareDataActivityLogApi['apiSharedataSharedataactivitylogPagePost']>>, 'data'>;
let queryVersion = 0;
let latestQuery: Promise<HistoryResponse>;
const emptyHistory = (): HistoryResponse => ({ data: { result: { records: [], total: 0 } } });
const clearStats = () => Object.assign(stats, { total: 0, successCount: 0, failedCount: 0, sentCount: 0, receivedCount: 0 });

function handleQueryApi(
	page: VxeGridPropTypes.ProxyAjaxQueryPageParams,
	sort?: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams
): Promise<HistoryResponse> {
	const version = ++queryVersion;
	const range = validateHistoryRange(state.dateRange);
	state.rangeError = range.errorKey ? $t(range.errorKey) : '';
	if (range.errorKey) {
		ElMessage.warning(state.rangeError);
		clearStats();
		latestQuery = Promise.resolve(emptyHistory());
		return latestQuery;
	}

	/* Capture every filter before awaiting so rows and counters use the same selection. */
	const filters: ShareDataPageActivityLogInput = {
		logType: state.logType,
		partnerId: state.query.partnerId || undefined,
		action: typeof state.query.action === 'number' ? state.query.action : undefined,
		success: typeof state.query.success === 'number' ? state.query.success : undefined,
		fromDate: range.fromDate,
		toDate: range.toDate,
	};
	clearStats();
	latestQuery = (async (): Promise<HistoryResponse> => {
		try {
			const api = getAPI(ShareDataActivityLogApi);
			const res = await api.apiSharedataSharedataactivitylogPagePost({
				...filters, page: page.currentPage, pageSize: page.pageSize,
				field: sort?.field || 'occurredAt', order: sort?.order || 'desc', descStr: 'desc',
			});
			if (version !== queryVersion) return latestQuery;

			if (filters.action !== undefined || filters.success !== undefined) {
				/* Summary API has no action/success filters. Count with the existing page API. */
				const count = async (extra: Partial<ShareDataPageActivityLogInput>) => {
					const response = await api.apiSharedataSharedataactivitylogPagePost({ ...filters, ...extra, page: 1, pageSize: 1 });
					return response.data.result?.total ?? 0;
				};
				const total = res.data.result?.total ?? 0;
				const [successCount, failedCount, sentCount, receivedCount] = await Promise.all([
					filters.success === SUCCESS_FAIL ? 0 : filters.success === SUCCESS_OK ? total : count({ success: SUCCESS_OK }),
					filters.success === SUCCESS_OK ? 0 : filters.success === SUCCESS_FAIL ? total : count({ success: SUCCESS_FAIL }),
					filters.logType === LOG_TYPE_TRANSFER ? count({ transferDirection: 0 }) : 0,
					filters.logType === LOG_TYPE_TRANSFER ? count({ transferDirection: 1 }) : 0,
				]);
				if (version !== queryVersion) return latestQuery;
				Object.assign(stats, { total, successCount, failedCount, sentCount, receivedCount });
			} else {
				const sRes = await api.apiSharedataSharedataactivitylogSummaryPost({
					logType: filters.logType, partnerId: filters.partnerId,
					fromDate: filters.fromDate, toDate: filters.toDate,
				});
				if (version !== queryVersion) return latestQuery;
				const summary = sRes.data.result ?? {};
				Object.assign(stats, {
					total: summary.total ?? 0, successCount: summary.successCount ?? 0,
					failedCount: summary.failedCount ?? 0, sentCount: summary.sentCount ?? 0,
					receivedCount: summary.receivedCount ?? 0,
				});
			}
			return res;
		} catch (error: unknown) {
			if (version !== queryVersion) return latestQuery;
			clearStats();
			ElMessage.error($t('lz.message.sharedataHistory.loadDataFailed'));
			return emptyHistory();
		}
	})();
	return latestQuery;
}

let reloadVersion = 0;
const handleQuery = async () => {
	const version = ++reloadVersion;
	options.loading = true;
	try {
		await xGrid.value?.commitProxy('reload');
	} finally {
		if (version === reloadVersion) options.loading = false;
	}
};

async function handleSearch() {
	if (options.pagerConfig && typeof options.pagerConfig === 'object') {
		(options.pagerConfig as any).currentPage = 1;
	}
	await handleQuery();
}

/** Mở chi tiết: mở modal ngay lập tức, dữ liệu chi tiết và các bước tiến trình được tải đồng thời bằng Promise.all */
function openDetail(row: ShareDataActivityLogOutput) {
	detailDialogRef.value?.open(row);
}

function onCellDblclick({ row }: any) {
	void openDetail(row);
}

async function resetQuery() {
	state.query.partnerId = '';
	state.query.action = undefined as unknown as number;
	state.query.success = undefined as unknown as number;
	state.dateRange = defaultRange();
	await handleSearch();
}

// Đổi tab thì đổi luôn bộ cột và bỏ bộ lọc hành động của tab cũ.
watch(
	() => state.logType,
	async () => {
		state.query.action = undefined as unknown as number;
		syncColumns();
		await handleSearch();
	}
);

onMounted(async () => {
	const res = await getAPI(ShareDataPartnerApi).apiSharedataSharedatapartnerListGet(
		undefined, undefined, undefined, undefined, undefined, true as any
	);
	partners.value = res.data.result ?? [];
	await loadPacketList();
	syncColumns();
	await handleQuery();
});
</script>

<style scoped lang="scss">
.esh-page {
	display: flex;
	flex-direction: column;
	gap: 6px;
	height: 100%;
}

.history-tabs {
	flex-shrink: 0;
	padding-left: 10px;

	:deep(.el-tabs__header) {
		margin: 0;
	}

	:deep(.el-tabs__nav-wrap) {
		margin-bottom: 0;

		&::after {
			height: 1px;
			background-color: var(--el-border-color-lighter);
		}
	}

	:deep(.el-tabs__content) {
		display: none;
		padding: 0;
	}

	:deep(.el-tabs__item) {
		padding: 0 20px;
		height: 38px;
		line-height: 38px;
		transition: color 0.2s ease;

		&.is-active {
			font-weight: 600;
			color: var(--el-color-primary);
		}
	}

	:deep(.el-tabs__active-bar) {
		background-color: var(--el-color-primary);
		height: 2px;
	}
}

.stat-row {
	display: flex;
	gap: 8px;
	flex-wrap: wrap;
}

.stat-card {
	flex: 1 1 160px;
}

.stat {
	display: flex;
	flex-direction: column;
}

.stat .lbl {
	font-size: 12px;
	color: var(--el-text-color-secondary);
}

.stat .val {
	font-size: 20px;
	font-weight: 600;
	color: var(--el-text-color-primary);
}

.text-success {
	color: var(--el-color-success);
}

.text-danger {
	color: var(--el-color-danger);
}

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

/* Bộ lọc: ô nhập / ô chọn rộng tối thiểu đủ chữ gợi ý khoảng 14 ký tự; thiếu chỗ thì ô lọc tự xuống hàng thay vì bị bóp */
.sd-filter-form {
	:deep(.el-form-item__content > .el-input) {
		min-width: 120px;
	}

	:deep(.el-form-item__content > .el-select) {
		min-width: 120px;
	}
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
