<template>
	<div class="lich-su-hau-kiem-container">
		<el-dialog v-model="state.isShowDialog" draggable :close-on-click-modal="false" width="1250px">
			<template #header>
				<div style="color: #fff">
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Edit /> </el-icon>
					<span> {{ props.title }} </span>
				</div>
			</template>

			<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options">
				<template #toolbar_buttons>
					{{ props.title }}
				</template>
				<template #toolbar_tools>
					<TableHeader :show-delete="false" :show-add="false" :show-custom-column="false" @refresh="handleQuery"> </TableHeader>
				</template>
			</vxe-grid>

			<template #footer>
				<span class="dialog-footer">
					<el-button @click="cancel">{{ $t('lz.button.base.cancel') }}</el-button>
				</span>
			</template>
		</el-dialog>
	</div>
</template>

<script lang="ts" setup name="tollHauKiem">
import { onMounted, reactive, ref, defineAsyncComponent } from 'vue';
import { VxeGridInstance, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { OperateType } from '/@/types/constants';
import { useI18n } from 'vue-i18n';

/* TODO(TOLL): chưa có API TOLL tương ứng - lịch sử hậu kiểm (acs10HauKiem)
import { getAPI } from '/@/utils/axios-utils';
import { GetQueryHistoryPostBikeInput, HkXeMayApi, ListHistotyPostBikeOutput, ZDbLogPostBikeInput } from '/@/api-services/acs10HauKiem';
*/

const { t: $t } = useI18n();
const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));

interface Props {
	/** the type of operation */
	operateType: OperateType;
	title: string;
}

const props = defineProps<Props>();
const emits = defineEmits(['handleQuery']);
const xGrid = ref<VxeGridInstance>();

const state = reactive({
	isShowDialog: false,
	queryParams: {} as any,
});

// Table parameter configuration
const options = useVxeTable<any>(
	{
		id: 'tollHauKiemHistoryDialog',
		name: 'Lich su hau kiem',
		columns: [
			{ field: 'stt', title: $t('lz.label.base.idx'), width: 50, fixed: 'left' },
			{ field: 'createdDated', title: $t('lz.entity.base.updateTime'), minWidth: 120, showOverflow: 'tooltip', sortable: true },
			{ field: 'createdUName', title: $t('lz.entity.base.updateBy'), minWidth: 140, showOverflow: 'tooltip', sortable: true },
			{ field: 'note', title: $t('lz.entity.base.remark'), minWidth: 140, showOverflow: 'tooltip', sortable: true },
		],
	},
	// vxeGrid configuration parameters (any parameter can be overridden here), refer to the official vxe-table documentation
	{
		// Proxy configuration
		proxyConfig: { autoLoad: true, ajax: { query: ({ page, sort }) => handleQueryApi(page, sort) } },
		// Toolbar configuration
		toolbarConfig: { export: false },
		pagerConfig: { enabled: false },
		sortConfig: { remote: false },
		rowConfig: { isCurrent: true, isHover: true },
		height: '300px',
		showFooter: true,
	}
);

// Page initialization
onMounted(async () => {
	// await initData();
});

const countRecords = () => {
	return xGrid?.value?.getTableData().fullData.length ?? 0;
};

// Query operation
const handleQuery = async () => {
	options.loading = true;
	await xGrid.value?.commitProxy('query');
	options.loading = false;
};

// Query API
// TODO(TOLL): chưa có API TOLL tương ứng - lấy lịch sử hậu kiểm, tạm trả về mảng rỗng
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
	/*
	const response = getAPI(HkXeMayApi).apiAcs10haukiemHkxemayListhistorybikePost(state.queryParams);
	return response;
	*/
	// Trả về response rỗng theo đúng dạng mà useVxeTable đọc (data.result)
	return { data: { result: [] as any[] } };
};

// Open dialog
const openDialog = async (row: any, isUpdateLog: any = undefined) => {
	if (isUpdateLog) {
		// Clear old data before fetch new data
		xGrid?.value?.loadData([]);
	}
	const { id } = row;
	state.queryParams.idRecheck = id;
	state.isShowDialog = true;
	options.loading = true;
	if (isUpdateLog) {
		state.queryParams.isUpdateLog = true;
		// TODO(TOLL): chưa có API TOLL tương ứng - ghi nhận log lịch sử hậu kiểm trước khi tải lại
		// const zDbLogPostBikeInput = { id: id } as ZDbLogPostBikeInput;
		// handleUpdateLogHistory(zDbLogPostBikeInput);
		handleQuery();
	} else {
		await handleQuery();
	}
};

// TODO(TOLL): chưa có API TOLL tương ứng - cập nhật log lịch sử hậu kiểm
/*
const handleUpdateLogHistory = async (zDbLogPostBikeInput: any) => {
	getAPI(HkXeMayApi).apiAcs10haukiemHkxemayUpdatehistoryPost(zDbLogPostBikeInput);
};
*/

// Close dialog
const closeDialog = () => {
	emits('handleQuery');
	state.isShowDialog = false;
};

// Cancel
const cancel = () => {
	state.isShowDialog = false;
};

// Export object
defineExpose({ openDialog });
</script>

<style scoped>
:deep(.ta-item-print > .el-form-item__content) {
	flex-wrap: nowrap;
}
</style>
