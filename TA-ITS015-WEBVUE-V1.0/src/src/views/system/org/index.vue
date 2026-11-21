<template>
	<WorkspacePage>
		<div class="sys-org-container">
			<GridSidePanel id="sys-org-mngt-org-tree" :label="$t('lz.entity.system.org')">
				<template #side>
					<OrgTree ref="orgTreeRef" class="workspace-tree" @node-click="handleNodeChange" />
				</template>

				<GridSearchPanel id="sys-orgMngt-org-search">
					<template #filters>
						<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true" label-width="auto" style="flex: 1 1 0%" label-position="left">
							<el-row :gutter="10">
								<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
									<el-form-item :label="$t('lz.entity.base.name')" prop="name">
										<el-input v-model="state.queryParams.name" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.name') })" clearable @keyup.enter="handleQuery" />
									</el-form-item>
								</el-col>
								<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
									<el-form-item :label="$t('lz.entity.base.code')" prop="code">
										<el-input v-model="state.queryParams.code" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.code') })" clearable @keyup.enter="handleQuery" />
									</el-form-item>
								</el-col>
								<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
									<el-form-item :label="$t('lz.entity.base.type')" prop="type">
										<el-select
											v-model="state.queryParams.type"
											filterable
											clearable
											class="w100"
											@clear="state.queryParams.type = undefined"
											:placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.base.type') })"
										>
											<el-option v-for="item in getConfigDataByCode(BaseConfigTypeEnum.SysOrgType)" :key="item.code" :label="item.name" :value="item.code" />
										</el-select>
									</el-form-item>
								</el-col>
							</el-row>
						</el-form>

						<el-divider style="height: calc(100% - 5px); margin: 0 10px" direction="vertical" />

						<el-row>
							<SearchButton :auth-search="'sysOrg:page'" @search="handleQuery" @reset="resetQuery" :loading="options.loading" />
						</el-row>
					</template>

					<el-card class="full-table" shadow="hover">
						<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
							<template #toolbar_buttons>
								<GridSummary :summary="summary" />
								<el-button-group style="padding-left: 12px">
									<el-button type="primary" icon="ele-Expand" @click="handleExpand"> {{ $t('lz.button.base.expandAll') }} </el-button>
									<el-button type="primary" icon="ele-Fold" @click="handleFold"> {{ $t('lz.button.base.collapseAll') }} </el-button>
								</el-button-group>
							</template>
							<template #toolbar_tools>
								<TableHeader
									@add="handleAdd"
									:show-delete="false"
									:auth-add="'sysOrg:add'"
									:auth-delete="'sysOrg:delete'"
									@refresh="handleQuery"
									@custom-column="handleCustom"
									:loading="options.loading"
									@delete=""
								>
								</TableHeader>
							</template>
							<template #empty>
								<el-empty :image-size="200" />
							</template>

							<template #row_type="{ row }">
								<TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.SysOrgType, row.type)" />
							</template>
							<template #row_status="{ row }">
								<TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.SysStatusType, row.status)" />
							</template>
							<template #row_record="{ row }">
								<ModifyRecord :data="row" />
							</template>
							<template #row_buttons="{ row }">
								<el-tooltip :content="$t('lz.button.base.copy')" placement="top" :auto-close="1000">
									<el-button icon="ele-CopyDocument" text type="warning" v-auth="'sysOrg:add'" @click="handleCopy(row)"> </el-button>
								</el-tooltip>
								<el-tooltip :content="$t('lz.button.base.edit')" placement="top" :auto-close="1000">
									<el-button icon="ele-Edit" size="small" text type="primary" @click="handleEdit(row)" v-auth="'sysOrg:update'"></el-button>
								</el-tooltip>
								<el-tooltip :content="$t('lz.button.base.delete')" placement="top" :auto-close="1000">
									<el-button icon="ele-Delete" size="small" text type="danger" @click="handleDelete(row)" v-auth="'sysOrg:delete'"></el-button>
								</el-tooltip>
							</template>
						</vxe-grid>
					</el-card>
				</GridSearchPanel>
			</GridSidePanel>

			<EditOrg ref="editOrgRef" :title="state.title" :operate-type="state.operateType" :orgData="state.treeData" @reload="handleQuery" />
		</div>
	</WorkspacePage>
</template>

<script lang="ts" setup name="sysOrg">
import { onActivated, onMounted, reactive, ref, nextTick, defineAsyncComponent } from 'vue';
import { ElMessageBox, ElMessage } from 'element-plus';
import { VxeGridInstance, VxeGridListeners } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import WorkspacePage from '/@/components/workspace/WorkspacePage.vue';
import GridSearchPanel from '/@/components/gridPage/GridSearchPanel.vue';
import GridSummary from '/@/components/gridPage/GridSummary.vue';
import { useGridSummary } from '/@/components/gridPage/useGridSummary';
import GridSidePanel from '/@/components/gridPage/GridSidePanel.vue';
import OrgTree from './component/orgTree.vue';
import EditOrg from './component/editOrg.vue';
import ModifyRecord from '/@/components/table/modifyRecord.vue';
import { getAPI } from '/@/utils/axios-utils';
import { SysOrgApi } from '/@/api-services/system';
import { SysOrg } from '/@/api-services/system';
import { OperateType } from '/@/types/constants';
import { useI18n } from 'vue-i18n';
const { t: $t } = useI18n();
import { useBaseConfig } from '/@/stores/baseConfig';
import { BaseConfigTypeEnum } from '/@/types/enums';
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
const getConfigItemByCode = useBaseConfig().getConfigItemByCode;
const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));

const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));
const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));

const { summary } = useGridSummary();
const xGrid = ref<VxeGridInstance>();
const editOrgRef = ref<InstanceType<typeof EditOrg>>();
const orgTreeRef = ref<InstanceType<typeof OrgTree>>();
const state = reactive({
	treeData: [] as Array<SysOrg>, // All data of the organization tree
	queryParams: {
		id: '0',
		name: undefined,
		code: undefined,
		type: undefined,
	},
	title: '',
	operateType: 'add' as OperateType,
});

// Table parameter configuration
const options = useVxeTable<SysOrg>(
	{
		id: 'sysOrgMngtOrg',
		name: 'Organization',
		columns: [
			{ type: 'seq', title: $t('lz.label.base.idx'), width: 60, fixed: 'left' },
			{ field: 'name', title: $t('lz.entity.base.name'), minWidth: 200, showOverflow: 'tooltip', treeNode: true, align: 'left', headerAlign: 'center' },
			{ field: 'code', title: $t('lz.entity.base.code'), minWidth: 200, showOverflow: 'tooltip' },
			{ field: 'level', title: $t('lz.entity.system.level'), minWidth: 70, showOverflow: 'tooltip' },
			{ field: 'type', title: $t('lz.entity.base.type'), minWidth: 80, slots: { default: 'row_type' }, showOverflow: 'tooltip' },
			{ field: 'orderNo', title: $t('lz.entity.base.orderNo'), sortable: true, width: 80, showOverflow: 'tooltip' },
			{ field: 'status', title: $t('lz.entity.base.status'), width: 110, sortable: true, showOverflow: 'tooltip', slots: { default: 'row_status' } },
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
		treeConfig: { expandAll: false, reserve: true }, //retain the expanded state of the tree node
	}
);

// Page initialization
onMounted(async () => {
	// Expand all data in the table, do not enable this when the data volume is large
	nextTick(async () => {
		if (state.treeData?.length < 100) await xGrid.value?.setAllTreeExpand(true);
	});
});

const countRows = (rows: { children?: any[] }[]): number => rows.reduce((total, row) => total + 1 + countRows(row.children ?? []), 0);

// Query API
const handleQueryApi = async () => {
	const params = { ...state.queryParams };
	const response = await getAPI(SysOrgApi).apiSystemSysorgListGet(params.id, params.name, params.code, params.type);
	summary.total = countRows(response.data.result ?? []);
	summary.lastLoaded = new Date();
	return response;
};

// Query operation
const handleQuery = async (updateTree: boolean = false) => {
	options.loading = true;
	try {
		await xGrid.value?.commitProxy('query');
		if (updateTree === true) await fetchOrgData();
	} finally {
		options.loading = false;
	}
};

// Reset operation
const resetQuery = async () => {
	state.queryParams.id = '0';
	state.queryParams.name = undefined;
	state.queryParams.code = undefined;
	state.queryParams.type = undefined;
	await xGrid.value?.commitProxy('reload');
	orgTreeRef.value?.setCurrentKey();
	await fetchOrgData();
};

const fetchOrgData = async () => {
	state.treeData = (await orgTreeRef.value?.fetchTreeData(false)) ?? [];
};
const openEditDialog = async (row: any) => {
	await fetchOrgData();
	editOrgRef.value?.openDialog(row);
};

// Open add page
const handleAdd = () => {
	state.title = $t('lz.label.base.titleAdd', { name: $t('lz.entity.system.org') });
	state.operateType = 'add';
	openEditDialog({ status: 1, orderNo: 100, level: 1 });
};

// Open copy dialog
const handleCopy = (row: any) => {
	state.title = $t('lz.label.base.titleCopy', { name: $t('lz.entity.system.org') });
	state.operateType = 'copy';
	openEditDialog(row);
};

// Open edit page
const handleEdit = (row: any) => {
	state.title = $t('lz.label.base.titleEdit', { name: $t('lz.entity.system.org') });
	state.operateType = 'edit';
	openEditDialog(row);
};

// Delete
const handleDelete = (row: any) => {
	ElMessageBox.confirm($t('lz.message.base.confirmDelete', { name: row.name }), $t('lz.label.base.warning'), {
		confirmButtonText: $t('lz.button.base.confirm'),
		cancelButtonText: $t('lz.button.base.cancel'),
		type: 'warning',
	})
		.then(async () => {
			await getAPI(SysOrgApi).apiSystemSysorgDeletePost({ id: row.id });
			handleQuery(true);
			ElMessage.success($t('lz.message.base.deleteSuccess'));
		})
		.catch(() => {});
};

// Custom column
function handleCustom() {
	xGrid.value?.openCustom();
}

// Table events
const gridEvents: VxeGridListeners<SysOrg> = {
	// Only effective when proxy-config.ajax.query is configured, this event will be triggered when manually clicking query
	async proxyQuery() {
		if (state.queryParams.id == '0' && !state.queryParams.name && !state.queryParams.code && !state.queryParams.type) state.treeData = xGrid.value?.getTableData().tableData ?? [];
	},
};

// Tree component click
const handleNodeChange = async (node: any) => {
	state.queryParams.id = node.id;
	state.queryParams.name = undefined;
	state.queryParams.code = undefined;
	state.queryParams.type = undefined;
	orgTreeRef.value?.setCurrentKey(node.id);
	await handleQuery();
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
	await Promise.all([fetchOrgData(), xGrid.value?.commitProxy('query')]);
});
</script>
