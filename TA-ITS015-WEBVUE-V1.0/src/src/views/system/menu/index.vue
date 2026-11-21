<template>
	<WorkspacePage>
		<div class="sys-menu-container">
			<GridSearchPanel id="sys-permMngt-menu-search">
				<template #filters>
					<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true" label-width="auto" style="flex: 1 1 0%" label-position="left">
						<el-row :gutter="10">
							<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
								<el-form-item :label="$t('lz.entity.base.name')" prop="title">
									<el-input v-model="state.queryParams.title" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.name') })" clearable @keyup.enter="handleQuery" />
								</el-form-item>
							</el-col>
							<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
								<el-form-item :label="$t('lz.entity.base.type')" prop="type">
									<el-select
										v-model="state.queryParams.type"
										:placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.base.type') })"
										clearable
										@clear="state.queryParams.type = undefined"
									>
										<el-option v-for="item in state.typeList" :key="item.code" :label="item.name" :value="item.code" />
									</el-select>
								</el-form-item>
							</el-col>
							<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
								<el-form-item :label="$t('lz.entity.base.status')" prop="status">
									<el-select
										v-model="state.queryParams.status"
										filterable
										clearable
										class="w100"
										@clear="state.queryParams.status = undefined"
										:placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.base.status') })"
									>
										<el-option v-for="item in getConfigDataByCode(BaseConfigTypeEnum.SysStatusType)" :key="item.code" :label="item.name" :value="parseInt(item.code)" />
									</el-select>
								</el-form-item>
							</el-col>
						</el-row>
					</el-form>

					<el-divider style="height: calc(100% - 5px); margin: 0 10px" direction="vertical" />

					<el-row>
						<SearchButton :auth-search="'sysMenu:page'" @search="handleQuery" @reset="resetQuery" :loading="options.loading" />
					</el-row>
				</template>

				<el-card class="full-table" shadow="hover">
					<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents" @checkbox-change="selectChangeEvent" @checkbox-all="selectChangeEvent">
						<template #toolbar_buttons>
							<GridSummary :summary="summary" />
							<!-- <el-button type="primary" icon="ele-Plus" @click="handleAdd" v-auth="'sysMenu:add'"> Add </el-button> -->
							<el-button-group style="padding-left: 12px">
								<el-button type="primary" icon="ele-Expand" @click="handleExpand"> {{ $t('lz.button.base.expandAll') }} </el-button>
								<el-button type="primary" icon="ele-Fold" @click="handleFold"> {{ $t('lz.button.base.collapseAll') }} </el-button>
							</el-button-group>
						</template>
						<template #toolbar_tools>
							<TableHeader
								@add="handleAdd"
								:show-delete="state.showBatchDelete"
								:auth-add="'sysMenu:add'"
								:auth-delete="'sysMenu:delete'"
								@refresh="handleQuery"
								@custom-column="handleCustom"
								@delete="handleBatchDelete"
							>
							</TableHeader>
						</template>
						<template #empty>
							<el-empty :image-size="200" />
						</template>
						<template #row_title="{ row }">
							<SvgIcon :name="row.icon" />
							<span class="ml10">{{ $t(row.title) }}</span>
						</template>
						<template #row_type="{ row }">
							<TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.SysMenuType, row.type)" />
						</template>
						<template #row_status="{ row }">
							<TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.SysStatusType, row.status)" />
						</template>
						<template #row_record="{ row }">
							<ModifyRecord :data="row" />
						</template>
						<template #row_buttons="{ row }">
							<el-tooltip :content="$t('lz.button.base.copy')" placement="top" :auto-close="1000">
								<el-button icon="ele-CopyDocument" text type="warning" v-auth="'sysMenu:add'" @click="handleCopy(row)"> </el-button>
							</el-tooltip>
							<el-tooltip :content="$t('lz.button.base.edit')" placement="top" :auto-close="1000">
								<el-button icon="ele-Edit" text type="primary" v-auth="'sysMenu:update'" @click="handleEdit(row)"> </el-button>
							</el-tooltip>
							<el-tooltip :content="$t('lz.button.base.delete')" placement="top" :auto-close="1000">
								<el-button icon="ele-Delete" text type="danger" v-auth="'sysMenu:delete'" @click="handleDelete(row)"> </el-button>
							</el-tooltip>
						</template>
					</vxe-grid>
				</el-card>
			</GridSearchPanel>

			<EditMenu ref="editMenuRef" :title="state.title" :menuData="state.menuData" :typeList="state.typeList" :operateType="state.operateType" @handleQuery="handleQuery" />
		</div>
	</WorkspacePage>
</template>

<script lang="ts" setup name="sysMenu">
import { onActivated, reactive, ref, defineAsyncComponent, onMounted } from 'vue';
import { ElMessageBox, ElMessage } from 'element-plus';
import { VxeGridInstance, VxeGridListeners } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import WorkspacePage from '/@/components/workspace/WorkspacePage.vue';
import GridSearchPanel from '/@/components/gridPage/GridSearchPanel.vue';
import GridSummary from '/@/components/gridPage/GridSummary.vue';
import { useGridSummary } from '/@/components/gridPage/useGridSummary';
import SvgIcon from '/@/components/svgIcon/index.vue';
import { useI18n } from 'vue-i18n';
import EditMenu from './component/editMenu.vue';
import ModifyRecord from '/@/components/table/modifyRecord.vue';

import { getAPI } from '/@/utils/axios-utils';
import { SysMenuApi } from '/@/api-services/system';
import { SysMenu } from '/@/api-services/system';
import { OperateType } from '/@/types/constants';
const { t: $t } = useI18n();
import { BaseConfigTypeEnum } from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';
const getConfigItemByCode = useBaseConfig().getConfigItemByCode;
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));
const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));
const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));
const { summary } = useGridSummary();
const xGrid = ref<VxeGridInstance>();

const editMenuRef = ref<InstanceType<typeof EditMenu>>();
const state = reactive({
	menuData: [] as Array<SysMenu>,
	queryParams: {
		title: undefined,
		type: undefined,
		status: undefined,
	},
	title: '',
	operateType: 'add' as OperateType,
	showBatchDelete: false,
	typeList: [] as any,
});

// Table parameter configuration
const options = useVxeTable<SysMenu>(
	{
		id: 'sysPermMngtMenu',
		name: 'Menu Information',
		columns: [
			{ type: 'checkbox', width: 40, fixed: 'left' },
			{ field: 'title', title: $t('lz.entity.base.name'), minWidth: 200, showOverflow: 'tooltip', treeNode: true, align: 'left', headerAlign: 'center', slots: { default: 'row_title' } },
			{ field: 'type', title: $t('lz.entity.base.type'), minWidth: 90, sortable: true, showOverflow: 'tooltip', slots: { default: 'row_type' } },
			{ field: 'name', title: $t('lz.entity.sysMenu.routeName'), minWidth: 100, sortable: true, showOverflow: 'tooltip' },
			{ field: 'path', title: $t('lz.entity.sysMenu.path'), minWidth: 150, showOverflow: 'tooltip' },
			{ field: 'component', title: $t('lz.entity.sysMenu.componentPath'), minWidth: 150, showOverflow: 'tooltip' },
			{ field: 'permission', title: $t('lz.entity.sysMenu.permissionIdentifier'), minWidth: 150, showOverflow: 'tooltip' },
			{ field: 'orderNo', title: $t('lz.entity.base.orderNo'), width: 80, sortable: true, showOverflow: 'tooltip' },
			{ field: 'status', title: $t('lz.entity.base.status'), width: 110, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_status' } },
			{ field: '', title: $t('lz.label.base.audit'), width: 100, showOverflow: 'tooltip', slots: { default: 'row_record' } },
			{ title: $t('lz.label.base.action'), fixed: 'right', width: 100, showOverflow: true, slots: { default: 'row_buttons' } },
		],
	},
	// vxeGrid configuration parameters (can override any parameters), refer to vxe-table official documentation
	{
		rowConfig: { keyField: 'id' }, //use with reserve
		stripe: false,
		// Multi-select configuration
		checkboxConfig: { range: false },
		// Proxy configuration
		proxyConfig: { autoLoad: true, ajax: { query: () => handleQueryApi() } },
		// Pagination configuration
		pagerConfig: { enabled: false },
		// Toolbar configuration
		toolbarConfig: { export: false },
		// Tree configuration
		treeConfig: { expandAll: false, reserve: true, lazy: true }, //retain the expanded state of the tree node
	}
);

onMounted(async () => {
	state.typeList = getConfigDataByCode(BaseConfigTypeEnum.SysMenuType);
});

const countRows = (rows: { children?: any[] }[]): number => rows.reduce((total, row) => total + 1 + countRows(row.children ?? []), 0);

// Query API
const handleQueryApi = async () => {
	const params = { ...state.queryParams };
	const response = await getAPI(SysMenuApi).apiSystemSysmenuListGet(params.title, params.type);
	summary.total = countRows(response.data.result ?? []);
	summary.lastLoaded = new Date();
	return response;
};

// Query operation
const handleQuery = async () => {
	// Call vxe-grid's commitProxy(query) method to trigger the table to reload data
	options.loading = true;
	try {
		await xGrid.value?.commitProxy('reload');
	} finally {
		options.loading = false;
	}
	state.menuData = xGrid.value?.getTableData().tableData ?? [];
};

// Reset operation
const resetQuery = async () => {
	state.queryParams.title = undefined;
	state.queryParams.type = undefined;
	state.queryParams.status = undefined;
	// Call vxe-grid's commitProxy(reload) method to trigger the table to reload data
	await xGrid.value?.commitProxy('reload');
};

// Open add page
const handleAdd = () => {
	state.title = $t('lz.label.base.titleAdd', { name: $t('lz.label.system.menu') });
	state.operateType = 'add';
	editMenuRef.value?.openDialog({ type: 'dir', isHide: false, isKeepAlive: true, isAffix: false, isIframe: false, isFullscreen: false, status: 1, orderNo: 100 });
};

// Open copy dialog
const handleCopy = (row: any) => {
	state.title = $t('lz.label.base.titleCopy', { name: $t('lz.label.system.menu') });
	state.operateType = 'copy';
	editMenuRef.value?.openDialog(row);
};

// Open edit page
const handleEdit = (row: any) => {
	state.title = $t('lz.label.base.titleEdit', { name: $t('lz.label.system.menu') });
	state.operateType = 'edit';
	editMenuRef.value?.openDialog(row);
};

// Delete current row
const handleDelete = (row: any) => {
	ElMessageBox.confirm($t('lz.message.base.confirmDelete', { name: row.title }), $t('lz.message.base.warning'), {
		confirmButtonText: $t('lz.button.base.confirm'),
		cancelButtonText: $t('lz.button.base.cancel'),
		type: 'warning',
	})
		.then(async () => {
			await getAPI(SysMenuApi).apiSystemSysmenuDeletePost({ id: row.id });
			ElMessage.success($t('lz.message.base.deleteSuccess'));
			await handleQuery();
		})
		.catch(() => {});
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
			await getAPI(SysMenuApi).apiSystemSysmenuBatchdeletePost(selectRecords.filter((l) => l.id));
			handleQuery();
			ElMessage.success($t('lz.message.base.deleteSuccess'));
		})
		.catch(() => {});
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
	state.showBatchDelete = getCheckedRecords().length > 0;
}

// Table events
const gridEvents: VxeGridListeners<SysMenu> = {
	// Only effective when proxy-config.ajax.query is configured, this event will be triggered when manually clicking query
	async proxyQuery() {
		state.menuData = xGrid.value?.getTableData().tableData ?? [];
	},
};

// Expand all
const handleExpand = () => {
	xGrid.value?.setAllTreeExpand(true);
};

// Collapse all
const handleFold = () => {
	xGrid.value?.clearTreeExpand();
};

// KeepAlive gọi onActivated cả lần mount đầu; proxy đã tự tải ở lần đó.
let activated = false;
onActivated(async () => {
	if (!activated) {
		activated = true;
		return;
	}
	await xGrid.value?.commitProxy('query');
});
</script>
