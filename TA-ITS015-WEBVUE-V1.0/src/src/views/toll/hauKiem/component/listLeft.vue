<template>
	<el-card class="full-table split-left-list" shadow="hover" style="margin-top: 5px">
		<vxe-grid ref="xGridLeft" class="xGrid-style" v-bind="gridTheTrongBai" @cell-click="handleSelectItem" @cell-dblclick="handleCellDblclick">
			<template #toolbar_buttons>
				<div class="toolbar-buttons">
					<strong class="total-text">{{ $t('lz.label.base.total') }}: {{ state.countData }}</strong>

					<!-- TODO(TOLL): chưa có API TOLL tương ứng - xuất Excel danh sách hậu kiểm
					<el-button size="small" class="btn-export" @click="handleExport" v-auth="'tollHauKiem:export'">
						<i class="fa fa-file-excel-o"></i>
						{{ $t('lz.button.base.export') }}
					</el-button>
					-->
				</div>
			</template>
			<template #toolbar_tools>
				<TableHeader :show-delete="false" :show-add="false" @refresh="handleQuery" @custom-column="handleCustom"> </TableHeader>
			</template>

			<template #empty>
				<el-empty :image-size="200" />
			</template>

			<template #date_cell_in="{ row }">
				<div>
					{{ row.transactionDatetime && new Date(row.transactionDatetime).getFullYear() > 1900 ? useDateFormat(row.transactionDatetime, DATE_TIME_FORMAT) : '' }}
				</div>
			</template>
			<template #date_cell_out="{ row }">
				<div>
					{{ row.transactionDatetimeIn && new Date(row.transactionDatetimeIn).getFullYear() > 1900 ? useDateFormat(row.transactionDatetimeIn, DATE_TIME_FORMAT) : '' }}
				</div>
			</template>

			<template #soTienThuTheoGio="{ row }">
				{{ row.price != null ? Number(row.price).toLocaleString('vi-VN') : '' }}
			</template>

			<!-- TODO(TOLL): chưa có API TOLL tương ứng - trạng thái lượt xe (field f0) không có trong ListTransactionOutPut
			<template #tranthailuotxe="{ row }">
				{{ getConfigDataByCode(BaseConfigTypeEnum.TcsTrangThaiHK)?.find((x) => x.code == row.f0)?.name || row.f0 }}
			</template>
			-->
			<template #row_buttons="{ row }">
				<el-tooltip :content="$t('lz.button.base.detail')" placement="top">
					<el-button icon="ele-View" text type="primary" v-auth="'tollHauKiem:detail'" @click="handleDetail(row)"> </el-button>
				</el-tooltip>
			</template>
		</vxe-grid>
		<!-- TODO(TOLL): chưa có API TOLL tương ứng - lịch sử hậu kiểm (viewHistory)
		<ViewModal ref="viewModalRef" :title="state.title" :operateType="state.operateType" @handleQuery="handleQuery" />
		-->
		<DetailViewComp ref="detailViewRef" :leftListRef="xGridLeft" />
	</el-card>
</template>

<script lang="ts" setup name="tollHauKiem">
import { ref, reactive, defineAsyncComponent } from 'vue';
import { VxeGridInstance, VxeGridPropTypes } from 'vxe-table';
import { useI18n } from 'vue-i18n';
import { useDateFormat } from '@vueuse/core';
import { getAPI } from '/@/utils/axios-utils';
import { OperateType } from '/@/types/constants';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { HaukiemTransactionApi, GetQueryPostTransactionInput, ListTransactionOutPut } from '/@/api-services/toll';
import { DATE_TIME_FORMAT } from '/@/types/formats';

/* TODO(TOLL): chưa có API TOLL tương ứng - đánh dấu đã hậu kiểm / cập nhật zdbLog (acs10HauKiem)
import { ListPostBikeOutput, HkXeMayApi, ZDbLogPostBikeInput, CheckPostBikeInput } from '/@/api-services/acs10HauKiem';
*/
/* TODO(TOLL): chưa có API TOLL tương ứng - danh mục trạng thái hậu kiểm (tcs23QuanLy)
import { MdsStatusApi } from '/@/api-services/tcs23QuanLy';
*/
/* TODO(TOLL): chưa có API TOLL tương ứng - xuất Excel (format số nghìn của types/formats + file-saver)
import { NumberFormat } from '/@/types/formats';
import { saveAs } from 'file-saver';
*/
/* TODO(TOLL): chưa có API TOLL tương ứng - lịch sử hậu kiểm (viewHistory)
import ViewModal from './viewHistory.vue';
*/
import DetailViewComp from './detailView.vue';

const props = defineProps({
	title: {
		type: String,
		default: '',
	},
	queryParams: {
		type: Object,
		default: () => {
			return {
				startTime: undefined as any,
				endTime: undefined as any,

				plate: undefined as any, // Plate
				vehicleTypeId: undefined as any, // VehicleTypeId
				tagId: undefined as any, // TagId

				statusChecked: undefined as any, // StatusChecked
				statusId: undefined as any, // StatusId

				userId: undefined as any, // UserId

				stationId: undefined as any, // StationId
				laneId: undefined as any, // LaneId

				transactionId: undefined as any, // TransactionId
				ticketId: undefined as any, // TicketId

				shiftId: undefined as any, // ShiftId
				reasonId: undefined as any, // ReasonId
			};
		},
	},
});

const state = reactive({
	localPageParam: {
		pageSize: 50 as number,
		defaultSort: { field: 'orderNo', order: 'asc' },
	},
	queryParams: props.queryParams,
	oldRow: {} as any,
	oldIndex: undefined as any,
	countData: 0 as number,
	title: '' as string,
	operateType: 'view' as OperateType,
	processedHistoryIds: new Set<string>(),
	statusChecked: undefined as any,
	idSelect: undefined as any,
	test: false as Boolean,
	currentIndex: -1,
	selectImageItem: undefined as any,
});

const emit = defineEmits(['query', 'selectItem', 'count', 'change-row']);

const { t: $t } = useI18n();
const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));
const xGridLeft = ref<VxeGridInstance>();
const detailViewRef = ref<InstanceType<typeof DetailViewComp>>();
let wasSelectedRow = false;

const gridTheTrongBai = useVxeTable<ListTransactionOutPut>(
	{
		id: 'tollHauKiemList',
		name: 'Left List',
		columns: [
			{ type: 'seq', title: $t('lz.label.base.idx'), width: 60, align: 'center' },
			{ field: 'transactionId', title: $t('lz.entity.toll.transactionId'), minWidth: 250, showOverflow: 'tooltip', sortable: true },
			{ field: 'transactionDatetime', title: $t('lz.entity.toll.timeOut'), minWidth: 190, showOverflow: 'tooltip', sortable: true, align: 'center', slots: { default: 'date_cell_in' } },
			{ field: 'transactionDatetimeIn', title: $t('lz.entity.toll.timeIn'), minWidth: 190, showOverflow: 'tooltip', sortable: true, align: 'center', slots: { default: 'date_cell_out' } },
			{ field: 'tagId', title: $t('lz.entity.toll.tagId'), minWidth: 250, align: 'center' },
			{ field: 'plateEdit', title: $t('lz.entity.tms.licensePlate'), minWidth: 140, align: 'center' },
			{ field: 'plate', title: $t('lz.entity.toll.plateRegistered'), minWidth: 140, align: 'center' },
			{ field: 'price', title: $t('lz.entity.toll.price'), minWidth: 140, align: 'center', slots: { default: 'soTienThuTheoGio' } },
			{ field: 'statusCheckedName', title: $t('lz.entity.toll.statusChecked'), minWidth: 170, align: 'center', showOverflow: 'tooltip' },
			{ field: 'ticketId', title: $t('lz.entity.toll.ticketId'), minWidth: 160, align: 'center' },
			{ field: 'stationInName', title: $t('lz.entity.toll.stationIn'), minWidth: 250, align: 'center' },
			{ field: 'stationName', title: $t('lz.entity.toll.stationOut'), minWidth: 250, align: 'center' },
			{ field: 'laneInName', title: $t('lz.entity.toll.laneIn'), minWidth: 140, align: 'center' },
			{ field: 'laneName', title: $t('lz.entity.toll.laneOut'), minWidth: 140, align: 'center' },
			{ field: 'userName', title: $t('lz.entity.toll.staff'), minWidth: 160, align: 'center' },
			{ field: 'statusName', title: $t('lz.entity.base.status'), minWidth: 250, align: 'center' },
			{ title: $t('lz.label.base.action'), fixed: 'right', width: 120, slots: { default: 'row_buttons' } },
		],
	},
	{
		// Proxy configuration
		proxyConfig: { autoLoad: true, ajax: { query: ({ page, sort }) => handleQueryApi(page, sort) } },
		sortConfig: { remote: false },
		toolbarConfig: { export: false },
		rowConfig: { isCurrent: true, isHover: true },
		checkboxConfig: { showHeader: false, checkField: 'option10', checkMethod: () => false, visibleMethod: () => true, strict: true },
		size: 'mini',
	}
);

// Double click a row -> select it and open the detail dialog
const handleCellDblclick = ({ row }: any) => {
	const tableData = getTableData();
	const index = tableData.findIndex((x) => x === row);
	if (index === -1) return;
	selectRow(index);
	emit('change-row', row, index);
	detailViewRef.value?.openDialog(row, index);
};

// Query API
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
	if (!state.queryParams.startTime && !state.queryParams.endTime) {
		const { startTime, endTime } = getDefaultTimeRange();
		state.queryParams.startTime = startTime;
		state.queryParams.endTime = endTime;
	}

	if (
		state.queryParams.startTime &&
		state.queryParams.endTime &&
		new Date(state.queryParams.startTime).getTime() > new Date(state.queryParams.endTime).getTime()
	) {
		state.countData = 0;
		xGridLeft.value?.loadData([]);
		emit('query');
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

	const params = Object.assign({}, state.queryParams, {
		page: page.currentPage,
		pageSize: page.pageSize,
		field: '',
		order: '',
		descStr: '',
	}) as GetQueryPostTransactionInput;

	try {
		const response = await getAPI(HaukiemTransactionApi).apiTollHaukiemtransactionPagePost(params);

		/* TODO(TOLL): chưa có API TOLL tương ứng - danh mục trạng thái hậu kiểm
		const statusChecked = await getAPI(MdsStatusApi).apiTcs23quanlyMdsstatusListbyconditionGet();
		state.statusChecked = statusChecked?.data?.result;
		*/

		state.countData = response?.data?.result?.total ?? 0;
		emit('query');
		return response;
	} catch (error) {
		state.countData = 0;
		xGridLeft.value?.loadData([]);
		emit('query');
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

// Khoảng thời gian mặc định: 00:00:00 - 23:59:59 của ngày hiện tại
const getDefaultTimeRange = () => {
	const startTime = new Date(new Date().setHours(0, 0, 0, 0));
	const endTime = new Date(new Date().setHours(23, 59, 59, 999));
	return { startTime, endTime };
};

const setQueryParams = (queryParams: any) => {
	state.queryParams = queryParams;
};

const setRowIndex = () => {
	selectRow(state.oldIndex, false);
	setTimeout(() => {
		handleScrollToRow(state.oldIndex);
	}, 500);
};

// Query operation
const handleQuery = async (isReload = true) => {
	gridTheTrongBai.loading = true;
	await xGridLeft.value?.commitProxy(isReload ? 'reload' : 'query');
	gridTheTrongBai.loading = false;
};

const handleScrollToRow = (index: number) => {
	const row = xGridLeft.value?.getData()[index];
	if (row && xGridLeft.value) {
		xGridLeft.value.scrollToRow(row);
	}
};
//#region button event

// Handle custom column
const handleCustom = () => {
	xGridLeft.value?.openCustom();
};

//#endregion button event

//#region table event
// When a config type is selected in the grid
const handleSelectItem = async (scope: any) => {
	if (state.oldRow == scope.row) {
		return;
	}
	state.oldRow = scope.row;
	state.oldIndex = scope.rowIndex;

	// TODO(TOLL): chưa có API TOLL tương ứng - đánh dấu lượt xe đã hậu kiểm khi chọn dòng
	/*
	if (scope.row && wasSelectedRow == true) {
		const entity = { ...state.oldRow } as ListPostBikeOutput;
		const { id, option10 } = entity as any;
		state.idSelect = id;
		const zDbLogPostBikeInput = { id: id } as ZDbLogPostBikeInput;
	} else {
		const entity = { ...state.oldRow } as ListPostBikeOutput;
		const { id, option10 } = entity as any;
		// Option10 = "1" => Đã hậu kiểm
		if (option10 != '1' && wasSelectedRow == false) {
			handleCheckPostBike(id);
		}
	}
	*/
	if (scope.row) {
		state.idSelect = (scope.row as any).id;
	}

	wasSelectedRow = true;
	state.test = true;
	emit('selectItem', scope.row, state.oldIndex);
};

//Handle select row auto switch
const selectRow = async (index: number, isAudit: boolean = true) => {
	const tableData = getTableData();

	if (tableData.length > index) {
		const row = tableData[index];

		xGridLeft.value?.setCurrentRow(row);

		// TODO(TOLL): chưa có API TOLL tương ứng - đánh dấu đã hậu kiểm / cập nhật zdbLog khi chuyển dòng
		/*
		const { id, option10 } = row;
		if (option10 != '1') {
			await Promise.all([handleUpdateZdbLog(id), handleCheckPostBike(id)]);
		} else {
			await handleUpdateZdbLog(id);
		}
		*/

		emit('selectItem', row, index);
	} else {
	}
};

// TODO(TOLL): chưa có API TOLL tương ứng - đánh dấu lượt này đã hậu kiểm
/*
const handleCheckPostBike = async (id: any) => {
	if (id) {
		const input = { id: id } as CheckPostBikeInput;
		getAPI(HkXeMayApi).apiAcs10haukiemHkxemayCheckpostbikePost(input);
	}
};
*/

// TODO(TOLL): chưa có API TOLL tương ứng - cập nhật zdbLog (lịch sử hậu kiểm)
/*
const handleUpdateZdbLog = async (id: any) => {
	const zDbLogPostBikeInput = { id: id } as ZDbLogPostBikeInput;
	getAPI(HkXeMayApi).apiAcs10haukiemHkxemayUpdatehistoryPost(zDbLogPostBikeInput);
};
*/

const getTableLength = () => {
	return state.countData;
};

const getTableData = () => {
	return xGridLeft.value?.getData() || [];
};

const getCurrentIndex = () => {
	return xGridLeft.value?.getCurrentRecord() as any;
};

// Clear selected item
const clearSelectItem = () => {
	xGridLeft.value?.clearCurrentRow();
};

// Get selected records
function getCheckedRecords() {
	return xGridLeft.value?.getCheckboxRecords() ?? [];
}

//#endregion table event

// Pop-up xem chi tiết giao dịch
const handleDetail = async (row: any) => {
	if (!row) return;

	state.title = $t('lz.button.base.detail');
	state.operateType = 'view';

	if (!state.idSelect || state.idSelect !== row.id) {
		// TODO(TOLL): chưa có API TOLL tương ứng - cập nhật lịch sử hậu kiểm trước khi mở chi tiết
		// await handleUpdateZdbLog(row.id);
		state.idSelect = row.id;
		wasSelectedRow = false;
		state.test = false;
	}

	detailViewRef.value?.openDialog(row);
};

// TODO(TOLL): chưa có API TOLL tương ứng - xuất Excel danh sách hậu kiểm
/*
const handleExport = async () => {
	try {
		const rs = await getAPI(HkTransactionInApi).tcsTcs23haukiemHktransactioninExportpostinputPost(state.queryParams, { responseType: 'blob' });
		const fileName = `HauKiem_${useDateFormat(useNow(), 'YYYYMMDDHHmmss').value}.xlsx`;
		saveAs(rs.data, fileName);
	} catch (error) {
		console.error('Export error:', error);
	}
};
*/

const clearTableData = () => {
	state.countData = 0;
	xGridLeft.value?.loadData([]);
};

// Expose methods to parent component
defineExpose({
	xGridLeft,
	setQueryParams,
	setRowIndex,
	clearSelectItem,
	getCheckedRecords,
	handleQuery,
	handleScrollToRow,
	getCurrentIndex,
	// Add new methods for auto-switching
	getTableData,
	getTableLength,
	selectRow,
	clearTableData,
});
</script>

<style>
.full-table {
	height: 100%;
}
.toolbar-buttons {
	display: flex;
	align-items: center;
	justify-content: flex-end;
	gap: 10px;
}

/* text total */
.total-text {
	font-weight: 600;
	color: var(--el-text-color-regular);
	font-size: 13px;
}

/* export button */
.btn-export {
	border-radius: 6px;
	background: var(--el-color-success);
	color: #fff;
	border: none;
	font-weight: 500;
	display: flex;
	align-items: center;
	gap: 6px;
	padding: 0 12px;
}

.btn-export:hover {
	background: var(--el-color-success-dark-2);
	transform: translateY(-1px);
	transition: all 0.2s ease;
}
</style>
