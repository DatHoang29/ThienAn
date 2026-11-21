<template>
	<WorkspacePage>
		<div class="sys-role-container">
			<GridSearchPanel id="sys-permMngt-role-search">
				<template #filters>
					<el-form :model="state.queryParams" ref="queryForm" :show-message="false" label-position="left" :inlineMessage="true" label-width="auto" style="flex: 1 1 0%">
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
						<SearchButton :auth-search="'sysRole:list'" @search="handleQuery" @reset="resetQuery" :loading="options.loading" />
					</el-row>
				</template>

				<el-card class="full-table" shadow="hover">
					<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
						<template #toolbar_buttons>
							<GridSummary :summary="summary" />
						</template>
						<template #toolbar_tools>
							<TableHeader
								@add="handleAdd"
								:show-delete="false"
								:auth-add="'sysRole:add'"
								:auth-delete="'sysRole:delete'"
								@refresh="handleQuery"
								@custom-column="handleCustom"
								@delete=""
								:loading="options.loading"
							>
							</TableHeader>
						</template>
						<template #empty>
							<el-empty :image-size="200" />
						</template>
						<!-- <template #row_dataScope="{ row }">
					<el-tag v-if="row.dataScope === 1">All Data</el-tag>
					<el-tag v-else-if="row.dataScope === 2">This Department and Below Data</el-tag>
					<el-tag v-else-if="row.dataScope === 3">This Department Data</el-tag>
					<el-tag v-else-if="row.dataScope === 4">Only My Data</el-tag>
				</template> -->
						<template #row_status="{ row }">
							<TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.SysStatusType, row.status)" />
						</template>
						<template #row_record="{ row }">
							<ModifyRecord :data="row" />
						</template>
						<template #row_buttons="{ row }">
							<el-tooltip :content="$t('lz.button.base.copy')" placement="top" :auto-close="1000">
								<el-button icon="ele-CopyDocument" text type="warning" v-auth="'sysRole:add'" @click="handleCopy(row)"> </el-button>
							</el-tooltip>
							<el-tooltip :content="$t('lz.button.base.edit')" placement="top" :auto-close="1000">
								<el-button icon="ele-Edit" size="small" text type="primary" @click="handleEdit(row)" v-auth="'sysRole:update'"></el-button>
							</el-tooltip>
							<el-tooltip :content="$t('lz.button.base.delete')" placement="top" :auto-close="1000">
								<el-button icon="ele-Delete" size="small" text type="danger" @click="handleDelete(row)" v-auth="'sysRole:delete'"></el-button>
							</el-tooltip>
						</template>
					</vxe-grid>
				</el-card>
			</GridSearchPanel>

			<EditRole ref="editRoleRef" :title="state.title" :operateType="state.operateType" @handleQuery="handleQuery" />
		</div>
	</WorkspacePage>
</template>

<script lang="ts" setup name="sysRole">
import { onActivated, reactive, ref, defineAsyncComponent } from 'vue';
import { ElMessageBox, ElMessage } from 'element-plus';
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import WorkspacePage from '/@/components/workspace/WorkspacePage.vue';
import GridSearchPanel from '/@/components/gridPage/GridSearchPanel.vue';
import GridSummary from '/@/components/gridPage/GridSummary.vue';
import { useGridSummary } from '/@/components/gridPage/useGridSummary';
import { useGridPager } from '/@/components/gridPage/useGridPager';
import EditRole from './component/editRole.vue';
import ModifyRecord from '/@/components/table/modifyRecord.vue';
import { getAPI } from '/@/utils/axios-utils';
import { SysRoleApi } from '/@/api-services/system';
import { PageRoleInput, PageRoleOutput } from '/@/api-services/system';
import { OperateType } from '/@/types/constants';

import { BaseConfigTypeEnum } from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';
const getConfigItemByCode = useBaseConfig().getConfigItemByCode;
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
import { useI18n } from 'vue-i18n';
const { t: $t } = useI18n();
const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));
const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));
const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));

const { summary, track } = useGridSummary();
const xGrid = ref<VxeGridInstance>();
const editRoleRef = ref<InstanceType<typeof EditRole>>();

const state = reactive({
	queryParams: {
		name: undefined,
		code: undefined,
		status: undefined,
	},
	localPageParam: {
		pageSize: 50 as number,
		defaultSort: { field: 'orderNo', order: 'asc', descStr: 'desc' } as any,
	},
	visible: false,
	title: '',
	operateType: 'add' as OperateType,
});

// Table parameter configuration
const options = useVxeTable<PageRoleOutput>(
	{
		id: 'sysPermMngtRole',
		name: 'Role',
		columns: [
			{ type: 'seq', title: $t('lz.label.base.idx'), width: 60, fixed: 'left' },
			{ field: 'code', title: $t('lz.entity.base.code'), minWidth: 100, sortable: true, showOverflow: 'tooltip', align: 'left' },
			{ field: 'name', title: $t('lz.entity.base.name'), minWidth: 150, sortable: true, showOverflow: 'tooltip', align: 'left' },
			{ field: 'orderNo', title: $t('lz.entity.base.orderNo'), width: 80, sortable: true, showOverflow: 'tooltip' },
			{ field: 'status', title: $t('lz.entity.base.status'), width: 110, sortable: true, showOverflow: 'tooltip', slots: { default: 'row_status' } },
			{ field: '', title: $t('lz.label.base.audit'), width: 100, showOverflow: 'tooltip', slots: { default: 'row_record' } },
			{ title: $t('lz.label.base.action'), fixed: 'right', width: 150, showOverflow: true, slots: { default: 'row_buttons' } },
		],
	},
	// vxeGrid configuration parameters (can override any parameters), refer to vxe-table official documentation
	{
		// Proxy configuration
		proxyConfig: { autoLoad: true, ajax: { query: ({ page, sort }) => handleQueryApi(page, sort) } },
		// Sorting configuration
		sortConfig: { defaultSort: state.localPageParam.defaultSort },
		// Pagination configuration
		pagerConfig: { pageSize: state.localPageParam.pageSize },
		// Toolbar configuration
		toolbarConfig: { export: false },
		rowConfig: { isCurrent: true, isHover: true },
	}
);

// Query API
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
	const params = { ...state.queryParams, page: page.currentPage, pageSize: page.pageSize, field: sort.field, order: sort.order, descStr: 'desc' } as PageRoleInput;
	return track(getAPI(SysRoleApi).apiSystemSysrolePagePost(params));
};

// Query operation
const handleQuery = async () => {
	options.loading = true;
	try {
		await xGrid.value?.commitProxy('reload');
	} finally {
		options.loading = false;
	}
};

// Reset operation
const resetQuery = async () => {
	state.queryParams.code = undefined;
	state.queryParams.name = undefined;
	state.queryParams.status = undefined;
	await handleQuery();
};

// Open add page
const handleAdd = () => {
	state.title = $t('lz.label.base.titleAdd', { name: $t('lz.label.system.role') });
	state.operateType = 'add';
	editRoleRef.value?.openDialog({ id: undefined, status: 1, orderNo: 100 });
};

// Open copy dialog
const handleCopy = (row: any) => {
	state.title = $t('lz.label.base.titleCopy', { name: $t('lz.label.system.role') });
	state.operateType = 'copy';
	editRoleRef.value?.openDialog(row);
};

// Open edit page
const handleEdit = async (row: any) => {
	state.title = $t('lz.label.base.titleEdit', { name: $t('lz.label.system.role') });
	state.operateType = 'edit';
	editRoleRef.value?.openDialog(row);
};

const handleDelete = (row: any) => {
	ElMessageBox.confirm($t('lz.message.base.confirmDelete', { name: row.name }), $t('lz.message.base.warning'), {
		confirmButtonText: $t('lz.button.base.confirm'),
		cancelButtonText: $t('lz.button.base.cancel'),
		type: 'warning',
	})
		.then(async () => {
			await getAPI(SysRoleApi).apiSystemSysroleDeletePost({ id: row.id });
			await handleQuery();
			ElMessage.success($t('lz.message.base.deleteSuccess'));
		})
		.catch(() => {});
};

// Custom column
function handleCustom() {
	xGrid.value?.openCustom();
}
// Table events
const gridEvents: VxeGridListeners<PageRoleOutput> = {
	// Only effective when pager-config is configured, this event will be triggered when pagination changes
	async pageChange({ pageSize }) {
		state.localPageParam.pageSize = pageSize;
	},
	// This event will be triggered when sorting conditions change
	async sortChange({ field, order }) {
		state.localPageParam.defaultSort = { field: field, order: order!, descStr: 'desc' };
	},
};

useGridPager(xGrid, options);

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
