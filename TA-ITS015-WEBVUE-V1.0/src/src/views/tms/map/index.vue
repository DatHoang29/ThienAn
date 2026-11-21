<template>
	<WorkspacePage>
	<div class="tms-map-container">
		<GridSearchPanel id="tms-map-mngt-maps-search">
				<template #filters>
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true" label-position="left"
				label-width="auto" style="flex: 1 1 0%">
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.code')" prop="code">
							<el-input v-model="state.queryParams.code" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.label.base.code')})"
							    clearable
								@keyup.enter.native="handleQuery" />
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.name')" prop="name">
							<el-input v-model="state.queryParams.name" 
								:placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.name')})"
								 clearable
								@keyup.enter.native="handleQuery" />
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.tmsMap.type')" prop="type">
							<el-select v-model="state.queryParams.type" 
								:placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.entity.tmsMap.type')})"
								filterable clearable class="w100"
								@clear="state.queryParams.type = undefined">
								<el-option v-for="item in state.dataMapTypeList" :key="item.code" :label="item.name"
									:value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.tmsMap.mode')" prop="mode">
							<el-select v-model="state.queryParams.mode"
								:placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.entity.tmsMap.mode')})"
								filterable clearable class="w100"
								@clear="state.queryParams.mode = undefined">
								<el-option v-for="item in state.dataMapModeList" :key="item.code" :label="item.name"
									:value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.status')" prop="status" >
							<el-select v-model="state.queryParams.status" filterable clearable class="w100" @clear="state.queryParams.status = undefined" :placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.entity.base.status')})" >
								<el-option v-for="item in getConfigDataByCode(BaseConfigTypeEnum.SysStatusType)" :key="item.code" :label="item.name" :value="parseInt(item.code)" />
							</el-select>
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>

			<el-divider style="height: calc(100% - 5px); margin: 0 10px" direction="vertical" />

			<el-row>
				<SearchButton :auth-search="'tmsMap:page'" @search="handleQuery" @reset="resetQuery" :loading="options.loading" />				
			</el-row>
				</template>

		<el-card class="full-table" shadow="hover">
			<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents"
				@checkbox-change="selectChangeEvent" @checkbox-all="selectChangeEvent">
				<template #toolbar_buttons>
					<GridSummary :summary="summary" />
				</template>
				<!-- <template #row_type="{ row }">
					<span>{{ row.type == 0 ? 'Sự kiện' : 'Sự cố' }}</span>
				</template> -->
				<template #toolbar_tools>
					<TableHeader @add="handleAdd" :show-delete="state.showBatchDelete" :auth-add="'tmsMap:add'"
						:auth-delete="'tmsMap:delete'" @refresh="handleQuery" @custom-column="handleCustom"
						@delete="handleBatchDelete">
					</TableHeader>
				</template>
				<template #empty>
					<el-empty :image-size="200" />
				</template>
				<template #row_isDefault="{ row }">
					<el-switch v-model="row.isDefault" :active-value="1" :inactive-value="0" size="small"
						@change="changeIsDefault(row)" v-auth="'tmsMap:setIsDefault'" />
				</template>
				<template #row_status="{ row }">
					<TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.SysStatusType, row.status)" />

				</template>
				<template #row_record="{ row }">
					<ModifyRecord :data="row" />
				</template>
				<template #row_buttons="{ row }">
					<el-tooltip :content="$t('lz.button.base.copy')" placement="top" :auto-close="1000">
						<el-button icon="ele-CopyDocument" text type="warning" v-auth="'tmsMap:add'"
							@click="handleCopy(row)"> </el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.edit')" placement="top" :auto-close="1000">
						<el-button icon="ele-Edit" text type="primary" v-auth="'tmsMap:update'"
							@click="handleEdit(row)"> </el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.delete')" placement="top" :auto-close="1000">
						<el-button icon="ele-Delete" text type="danger" v-auth="'tmsMap:delete'"
							@click="handleDelete(row)"> </el-button>
					</el-tooltip>
				</template>
				<template #header_name>
				</template>
			</vxe-grid>
		</el-card>
		</GridSearchPanel>

		<EditMap ref="editMapRef" :title="state.title" :operateType="state.operateType" @handleQuery="handleQuery" />
	</div>
	</WorkspacePage>
</template>

<script lang="ts" setup name="tmsMap">
import { onMounted, onActivated, reactive, ref, defineAsyncComponent } from 'vue';
import { ElMessageBox, ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { Local } from '/@/utils/storage';
import EditMap from './component/editMap.vue';
import WorkspacePage from '/@/components/workspace/WorkspacePage.vue';
import GridSearchPanel from '/@/components/gridPage/GridSearchPanel.vue';
import GridSummary from '/@/components/gridPage/GridSummary.vue';
import { useGridSummary } from '/@/components/gridPage/useGridSummary';
import { useGridPager } from '/@/components/gridPage/useGridPager';
import { SysConfigDataApi } from '/@/api-services/cfgSystem';
import ModifyRecord from '/@/components/table/modifyRecord.vue';
import { getAPI } from '/@/utils/axios-utils';
import { TmsMapApi, TmsModulesApi,PageMapOutput } from '/@/api-services/tms';
import { OperateType } from '/@/types/constants/operate';
import { useBaseConfig } from '/@/stores/baseConfig';
import { BaseConfigTypeEnum } from '/@/types/enums';
const getConfigItemByCode = useBaseConfig().getConfigItemByCode;
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));
const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));
const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));
const { t: $t } = useI18n();
const xGrid = ref<VxeGridInstance>();
const { summary, track } = useGridSummary();
const editMapRef = ref<InstanceType<typeof EditMap>>();
const state = reactive({
	queryParams: {
		name: undefined,
		code: undefined,
		type: undefined,
		mode: undefined,
		status: undefined,
	},
	localPageParam: {
		pageSize: 50 as number,
		defaultSort: { field: 'orderNo', order: 'asc', descStr: 'desc' },
	},
	visible: false,
	title: '',
	operateType: 'add' as OperateType,
	showBatchDelete: false,
	ModuleList: [] as any,
	dataMapTypeList: [] as any,
	dataMapModeList: [] as any,

});
// Local storage parameters
const localPageParamKey = 'localPageParam:tmsMap';
// Table parameter configuration
const options = useVxeTable<PageMapOutput>(
	{
		// ID riêng để không dùng chung cấu hình cột với trang gốc tms/map.
		id: 'tmsMapMngtMaps',
		name: 'Map',
		columns: [
			{ type: 'checkbox', fixed: 'left', width: 45 },
			{ field: 'code', title: $t('lz.entity.base.code'), minWidth: 80, showOverflow: 'tooltip', sortable: true, },
			{ field: 'name', title: $t('lz.entity.base.name'), minWidth: 150, showOverflow: 'tooltip', sortable: true, },
			{ field: 'type', title: $t('lz.entity.tmsMap.type'), sortable: true, minWidth: 80, formatter: ({ cellValue }: any) => state.dataMapTypeList.find((u: any) => u.code == cellValue)?.name, showOverflow: 'tooltip' },
			// { field: 'url', title: $t('Đường dẫn hình ảnh map'), minWidth: 200, showOverflow: 'tooltip', sortable: true, },
			// { field: 'width', title: $t('Chiều rộng'), minWidth: 80, showOverflow: 'tooltip', sortable: true, },
			// { field: 'height', title: $t('Chiều rộng'), minWidth: 80, showOverflow: 'tooltip', sortable: true, },
			{ field: 'mode', title: $t('lz.entity.base.mode'), sortable: true, minWidth: 80, formatter: ({ cellValue }: any) => state.dataMapModeList.find((u: any) => u.code == cellValue)?.name, showOverflow: 'tooltip' },
			{ field: 'isDefault', title: $t('lz.entity.base.isDefault'), width: 100, showOverflow: 'tooltip', slots: { default: 'row_isDefault' } },
			{ field: 'status', title: $t('lz.entity.base.status'), width: 110, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_status' } },
			{ field: '', title: $t('lz.label.base.audit'), width: 100, showOverflow: 'tooltip',sortable: true, slots: { default: 'row_record' } },
			{ title: $t('lz.label.base.action'), fixed: 'right', width: 100, showOverflow: true, slots: { default: 'row_buttons' } },
		],
	},
	// vxeGrid configuration parameters (any parameter can be overridden here), refer to the official vxe-table documentation
	{
		// Proxy configuration
		proxyConfig: { autoLoad: true, ajax: { query: ({ page, sort }) => handleQueryApi(page, sort) } },
		// Sorting configuration
		// sortConfig: { defaultSort: Local.get(localPageParamKey)?.defaultSort || state.localPageParam.defaultSort },
		// // Pagination configuration
		// pagerConfig: { pageSize: Local.get(localPageParamKey)?.pageSize || state.localPageParam.pageSize },
		// Toolbar configuration
		toolbarConfig: { export: false },
	}
);
// Thu gọn thanh phân trang theo chiều rộng bảng.
useGridPager(xGrid, options);

// Page initialization
onMounted(async () => {
	state.localPageParam = Local.get(localPageParamKey) || state.localPageParam;
	let resDicData = await getAPI(TmsModulesApi).apiTmsTmsmodulesListGet();
	state.ModuleList = resDicData.data.result;
	let response = await getAPI(SysConfigDataApi).apiCfgsystemSysconfigdataDatalistCodeGet('tms_map_type');
	state.dataMapTypeList = response.data.result;
	response = await getAPI(SysConfigDataApi).apiCfgsystemSysconfigdataDatalistCodeGet('tms_map_mode');
	state.dataMapModeList = response.data.result;
	await handleQuery();

});

// Quay lại tab/menu (KeepAlive): tải lại trang hiện tại để nhận thay đổi từ tab Thiết lập vị trí bản đồ.
// onActivated cũng chạy ngay sau lần mount đầu, khi đó dữ liệu vừa tải nên bỏ qua.
let activated = false;
onActivated(async () => {
	if (!activated) {
		activated = true;
		return;
	}
	await xGrid.value?.commitProxy('query');
});
const changeIsDefault = async (row: any) => {
	try {
		await getAPI(TmsMapApi).apiTmsTmsmapUpdatePost(row);
		ElMessage.success($t( 'lz.message.base.actionSuccess'));
	} catch (error) {
		console.error('Error updating isDefault:', error);
		row.isDefault = row.isDefault == '1' ? '0' : '1';
		ElMessage.error($t('lz.message.base.acctionFail'));
	}
	await handleQuery();

};
// Query API
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
	// Tạo object mới: không ghi page/sort vào bộ lọc.
	const params = { ...state.queryParams, page: page.currentPage, pageSize: page.pageSize, field: sort.field, order: sort.order, descStr: 'desc' } as PageTmsMapInput;
	return track(getAPI(TmsMapApi).apiTmsTmsmapPagePost(params));
};

// Query operation
const handleQuery = async () => {
	options.loading = true;
	await xGrid.value?.commitProxy('reload') ;
	options.loading = false;
	selectChangeEvent();
};

// Reset operation
const resetQuery = async () => {
	state.queryParams.code = undefined;
	state.queryParams.name = undefined;
	state.queryParams.type = undefined;
	state.queryParams.mode = undefined;
	state.queryParams.status = undefined;
	await xGrid.value?.commitProxy('reload');
};

// Open add dialog
const handleAdd = () => {
	state.title = $t('lz.label.base.titleAdd', { name: $t('lz.label.tmsMap.map') });
	state.operateType =  'add';
	editMapRef.value?.openDialog({ status: 1, orderNo: 100, type: 'server', warningType: 0 });

};

// Open copy dialog
const handleCopy = (row: any) => {
	state.title = $t('lz.label.base.titleCopy', { name: $t('lz.label.tmsMap.map') }); 
	state.operateType = 'copy';
	editMapRef.value?.openDialog(row);
};

// Open edit dialog
const handleEdit = (row: any) => {
	state.title =  $t('lz.label.base.titleEdit', { name: $t('lz.label.tmsMap.map') });
	state.operateType = 'edit';
	editMapRef.value?.openDialog(row);
};


// Delete
const handleDelete = (row: any) => {
	ElMessageBox.confirm($t('lz.message.base.confirmDelete', { name: row.name }), $t('lz.message.base.warning'), {
		confirmButtonText: $t('lz.button.base.confirm'),
		cancelButtonText: $t('lz.button.base.cancel'),
		type: 'warning',
	})
		.then(async () => {
			await getAPI(TmsMapApi).apiTmsTmsmapDeletePost({ id: row.id });
			handleQuery();
			ElMessage.success($t('lz.message.base.deleteSuccess'));
		})
		.catch(() => { });
};

//Batch delete
const handleBatchDelete = async () => {
	const selectRecords = getCheckedRecords();
	ElMessageBox.confirm($t('lz.message.base.confirmBatchDelete', { total: selectRecords.length }), $t('lz.message.base.warning'), {
		confirmButtonText: $t('lz.button.base.confirm'),
		cancelButtonText: $t('lz.button.base.cancel'),
		type: 'warning',
	})
		.then(async () => {
			await getAPI(TmsMapApi).apiTmsTmsmapBatchdeletePost(selectRecords.filter(l => l.id));
			handleQuery();
			ElMessage.success($t('lz.message.base.deleteSuccess'));
		})
		.catch(() => { });
}

// Custom column
function handleCustom() {
	xGrid.value?.openCustom();
}
// Get selected records
function getCheckedRecords() {
	return xGrid.value?.getCheckboxRecords() ?? [];
}

function selectChangeEvent() {
	state.showBatchDelete = getCheckedRecords().length > 0;
}

// Table events
const gridEvents: VxeGridListeners<PageTmsMapOutput> = {
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
