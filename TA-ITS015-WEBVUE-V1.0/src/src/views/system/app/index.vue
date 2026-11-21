<template>
	<WorkspacePage>
		<div class="sys-pos-container">
			<GridSearchPanel id="sys-appMngt-app-search">
				<template #filters>
					<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true" label-position="left" label-width="auto" style="flex: 1 1 0%">
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
						<SearchButton :auth-search="'sysApp:page'" @search="handleQuery" @reset="resetQuery" :loading="options.loading" />
					</el-row>
				</template>

				<el-card class="full-table" shadow="hover">
					<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" @checkbox-change="selectChangeEvent" @checkbox-all="selectChangeEvent">
						<template #toolbar_buttons>
							<GridSummary :summary="summary" />
						</template>
						<template #toolbar_tools>
							<TableHeader
								@add="handleAdd"
								:show-delete="state.showBatchDelete"
								:auth-add="'sysApp:add'"
								:auth-delete="'sysApp:delete'"
								@refresh="handleQuery"
								@custom-column="handleCustom"
								@delete="handleBatchDelete"
							>
							</TableHeader>
						</template>
						<template #empty>
							<el-empty :image-size="200" />
						</template>
						<template #row_status="{ row }">
							<TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.SysStatusType, row.status)" />
						</template>
						<template #row_record="{ row }">
							<ModifyRecord :data="row" />
						</template>
						<template #row_buttons="{ row }">
							<el-tooltip :content="$t('lz.button.base.copy')" placement="top" :auto-close="1000">
								<el-button icon="ele-CopyDocument" text type="warning" v-auth="'sysApp:add'" @click="handleCopy(row)"> </el-button>
							</el-tooltip>
							<el-tooltip :content="$t('lz.button.base.edit')" placement="top" :auto-close="1000">
								<el-button icon="ele-Edit" text type="primary" v-auth="'sysApp:update'" @click="handleEdit(row)"> </el-button>
							</el-tooltip>
							<el-tooltip :content="$t('lz.button.base.delete')" placement="top" :auto-close="1000">
								<el-button icon="ele-Delete" text type="danger" v-auth="'sysApp:delete'" @click="handleDelete(row)"> </el-button>
							</el-tooltip>
						</template>
					</vxe-grid>
				</el-card>
			</GridSearchPanel>

			<EditApp ref="editAppRef" :title="state.title" :operateType="state.operateType" @handleQuery="handleQuery" />
		</div>
	</WorkspacePage>
</template>

<script lang="ts" setup name="sysApp">
import { onActivated, reactive, ref, defineAsyncComponent } from 'vue';
import { ElMessageBox, ElMessage } from 'element-plus';
import { VxeGridInstance, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import WorkspacePage from '/@/components/workspace/WorkspacePage.vue';
import GridSearchPanel from '/@/components/gridPage/GridSearchPanel.vue';
import GridSummary from '/@/components/gridPage/GridSummary.vue';
import { useGridSummary } from '/@/components/gridPage/useGridSummary';
import { useGridPager } from '/@/components/gridPage/useGridPager';
import EditApp from './component/editApp.vue';
import ModifyRecord from '/@/components/table/modifyRecord.vue';
import { getAPI } from '/@/utils/axios-utils';
import { SysAppApi } from '/@/api-services/system';
import { PageAppOutput, PageAppInput } from '/@/api-services/system';
import { OperateType } from '/@/types/constants/operate';

import { BaseConfigTypeEnum } from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';
const getConfigItemByCode = useBaseConfig().getConfigItemByCode;
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));
const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));
const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));
import { useI18n } from 'vue-i18n';
const { t: $t } = useI18n();

const { summary, track } = useGridSummary();
const xGrid = ref<VxeGridInstance>();
const editAppRef = ref<InstanceType<typeof EditApp>>();
const state = reactive({
	queryParams: {
		name: undefined,
		code: undefined,
		status: undefined,
	},
	localPageParam: {
		pageSize: 50 as number,
		defaultSort: { field: 'code', order: 'asc', descStr: 'desc' } as any,
	},
	visible: false,
	title: '',
	operateType: 'add' as OperateType,
	showBatchDelete: false,
});
// Table parameter configuration
const options = useVxeTable<PageAppOutput>(
	{
		id: 'sysAppMngtApp',
		name: 'App',
		columns: [
			{ type: 'checkbox', fixed: 'left', width: 45 },
			{ field: 'code', title: $t('lz.entity.base.code'), minWidth: 200, showOverflow: 'tooltip', sortable: true },
			{ field: 'name', title: $t('lz.entity.base.name'), minWidth: 200, showOverflow: 'tooltip', sortable: true },
			{ field: 'linkUrl', title: $t('lz.entity.sysApp.linkUrl'), minWidth: 200, showOverflow: 'tooltip', sortable: true },
			{ field: 'status', title: $t('lz.entity.base.status'), width: 110, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_status' } },
			{ field: '', title: $t('lz.label.base.audit'), width: 100, showOverflow: 'tooltip', slots: { default: 'row_record' } },
			{ title: $t('lz.label.base.action'), fixed: 'right', width: 100, showOverflow: true, slots: { default: 'row_buttons' } },
		],
	},
	// vxeGrid configuration parameters (any parameter can be overridden here), refer to the official vxe-table documentation
	{
		// Proxy configuration
		proxyConfig: { autoLoad: true, ajax: { query: ({ page, sort }) => handleQueryApi(page, sort) } },
		// Toolbar configuration
		sortConfig: { defaultSort: state.localPageParam.defaultSort },
		pagerConfig: { pageSize: state.localPageParam.pageSize },
		toolbarConfig: { export: false },
	}
);

// Query API
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
	// Nếu sort.field bị null/undefined, lấy giá trị defaultSort
	const sortField = sort?.field || state.localPageParam.defaultSort.field;
	const sortOrder = sort?.order || state.localPageParam.defaultSort.order;

	const params = { ...state.queryParams, page: page.currentPage, pageSize: page.pageSize, field: sortField, order: sortOrder, descStr: 'desc' } as PageAppInput;
	return track(getAPI(SysAppApi).apiSystemSysappPagePost(params));
};

// Query operation
const handleQuery = async () => {
	options.loading = true;
	try {
		await xGrid.value?.commitProxy('reload');
	} finally {
		options.loading = false;
	}
	selectChangeEvent();
};

// Reset operation
const resetQuery = async () => {
	state.queryParams.code = undefined;
	state.queryParams.name = undefined;
	state.queryParams.status = undefined;
	await xGrid.value?.commitProxy('reload');
};

// Open add dialog
const handleAdd = () => {
	state.title = $t('lz.label.base.titleAdd', { name: $t('lz.label.system.app') });
	state.operateType = 'add';
	editAppRef.value?.openDialog({ status: 1, orderNo: 100 });
};

// Open copy dialog
const handleCopy = (row: any) => {
	state.title = $t('lz.label.base.titleCopy', { name: $t('lz.label.system.app') });
	state.operateType = 'copy';
	editAppRef.value?.openDialog(row);
};

// Open edit dialog
const handleEdit = (row: any) => {
	state.title = $t('lz.label.base.titleEdit', { name: $t('lz.label.system.app') });
	state.operateType = 'edit';
	editAppRef.value?.openDialog(row);
};

// Delete
const handleDelete = (row: any) => {
	ElMessageBox.confirm($t('lz.message.base.confirmDelete', { name: row.name }), $t('lz.message.base.warning'), {
		confirmButtonText: $t('lz.button.base.confirm'),
		cancelButtonText: $t('lz.button.base.cancel'),
		type: 'warning',
	})
		.then(async () => {
			await getAPI(SysAppApi).apiSystemSysappDeletePost({ id: row.id });
			handleQuery();
			ElMessage.success($t('lz.message.base.deleteSuccess'));
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
			await getAPI(SysAppApi).apiSystemSysappBatchdeletePost(selectRecords.filter((l) => l.id));
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
