<template>
	<div class="sysconfig-container" v-loading="optionsConfigTypeGrid.loading">
		<el-row :gutter="8" style="width: 100%; height: 100%; flex: 1">
			<!-- SysConfigType: begin -->
			<el-col :span="12" :xs="24" class="full-height">
				<el-card shadow="hover" :body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', height: '100%', alignItems: 'start' }">
					<el-form :model="state.queryParamsConfigType" :show-message="false" label-position="left" :inlineMessage="true" label-width="auto" style="flex: 1 1 0%">
						<el-row :gutter="10">
							<el-col class="mb5" :xs="24" :sm="24" :md="24" :lg="12" :xl="12">
								<el-form-item :label="$t('lz.entity.base.name')">
									<el-input
										v-model="state.queryParamsConfigType.name"
										:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.name') })"
										@keyup.enter.native="handleQueryConfigType"
										:maxlength="EntityFieldLength.Length64"
										show-word-limit
										clearable
									/>
								</el-form-item>
							</el-col>
							<el-col class="mb5" :xs="24" :sm="24" :md="24" :lg="12" :xl="12">
								<el-form-item :label="$t('lz.entity.base.code')">
									<el-input
										v-model="state.queryParamsConfigType.code"
										:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.code') })"
										@keyup.enter.native="handleQueryConfigType"
										:maxlength="EntityFieldLength.Length128"
										show-word-limit
										clearable
									/>
								</el-form-item>
							</el-col>
						</el-row>
					</el-form>
					<el-divider style="height: calc(100% - 5px); margin: 0 10px" direction="vertical" />
					<el-row>
						<SearchButton :auth-search="'sysConfigType:page'" @search="handleQueryConfigType" @reset="resetQueryConfigType" :loading="optionsConfigTypeGrid.loading" />
					</el-row>
				</el-card>
				<el-card class="full-table" shadow="hover" style="margin-top: 5px">
					<vxe-grid ref="xGridSysConfigType" class="xGrid-style" v-bind="optionsConfigTypeGrid" v-on="gridEventsConfigType" @cell-click="handleConfigDataByParent">
						<template #toolbar_buttons>
							<el-icon size="16" style="margin-right: 3px; margin-top: 2px; display: inline; vertical-align: middle"><ele-Collection /></el-icon>
							{{ $t('lz.label.cfgSystem.sysConfigType') }}
						</template>
						<template #toolbar_tools>
							<TableHeader
								@add="handleAddConfigType"
								:show-delete="false"
								:auth-add="'sysConfigType:add'"
								:auth-delete="'sysConfigType:delete'"
								@refresh="handleQueryConfigType"
								@custom-column="handleConfigTypeCustom"
								@delete=""
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
								<el-button icon="ele-CopyDocument" text type="warning" v-auth="'sysConfigType:add'" @click="handleCopyConfigType(row)"> </el-button>
							</el-tooltip>
							<el-tooltip :content="$t('lz.button.base.edit')" placement="top" :auto-close="1000">
								<el-button icon="ele-Edit" text type="primary" v-auth="'sysConfigType:update'" @click="handleEditConfigType(row)"> </el-button>
							</el-tooltip>
							<el-tooltip :content="$t('lz.button.base.delete')" placement="top" :auto-close="1000">
								<el-button icon="ele-Delete" text type="danger" v-auth="'sysConfigType:delete'" @click="handleDeleteConfigType(row)"> </el-button>
							</el-tooltip>
						</template>
					</vxe-grid>
				</el-card>
			</el-col>
			<!-- SysConfigType: end -->

			<!-- SysConfigData: begin -->
			<el-col :span="12" :xs="24" class="full-height">
				<el-card shadow="hover" :body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', height: '100%', alignItems: 'start' }">
					<el-form :model="state.queryParamsConfigData" label-position="left" :show-message="false" :inlineMessage="true" label-width="auto" style="flex: 1 1 0%">
						<el-row :gutter="10">
							<el-col class="mb5" :xs="24" :sm="24" :md="24" :lg="12" :xl="12">
								<el-form-item :label="$t('lz.entity.base.name')">
									<el-input
										v-model="state.queryParamsConfigData.name"
										:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.name') })"
										@keyup.enter.native="handleQueryConfigData"
										:maxlength="EntityFieldLength.Length64"
										show-word-limit
										clearable
									/>
								</el-form-item>
							</el-col>
							<el-col class="mb5" :xs="24" :sm="24" :md="24" :lg="12" :xl="12">
								<el-form-item :label="$t('lz.entity.base.code')">
									<el-input
										v-model="state.queryParamsConfigData.code"
										:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.code') })"
										@keyup.enter.native="handleQueryConfigData"
										:maxlength="EntityFieldLength.Length256"
										show-word-limit
										clearable
									/>
								</el-form-item>
							</el-col>
						</el-row>
					</el-form>
					<el-divider style="height: calc(100% - 5px); margin: 0 10px" direction="vertical" />
					<el-row>
						<SearchButton :auth-search="'sysConfigType:page'" @search="handleQueryConfigData" @reset="resetQueryConfigData" :loading="optionsConfigDataGrid.loading" />
					</el-row>
				</el-card>
				<el-card class="full-table" shadow="hover" style="margin-top: 5px">
					<vxe-grid ref="xGridSysConfigData" class="xGrid-style" v-bind="optionsConfigDataGrid" v-on="gridEventsConfigData">
						<template #toolbar_buttons>
							<el-icon size="16" style="margin-right: 3px; margin-top: 2px; display: inline; vertical-align: middle"><ele-Collection /></el-icon>
							{{ $t('lz.label.cfgSystem.sysConfigData') }}
						</template>
						<template #toolbar_tools>
							<TableHeader
								@add="handleAddConfigData"
								:show-delete="false"
								:auth-add="'sysConfigType:add'"
								:auth-delete="'sysConfigType:delete'"
								@refresh="handleConfigDataQuery"
								@custom-column="handleConfigDataCustom"
								@delete=""
							>
							</TableHeader>
						</template>
						<template #empty>
							<el-empty :image-size="200" />
						</template>
						<template #row_code="{ row }">
							<el-tag v-if="row.code != null" :type="row.tagType" :style="row.styleSetting" :class="row.classSetting">{{ row.code }}</el-tag>
						</template>
						<template #row_status="{ row }">
							<TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.SysStatusType, row.status)" />
						</template>
						<template #row_record="{ row }">
							<ModifyRecord :data="row" />
						</template>
						<template #row_buttons="{ row }">
							<el-tooltip :content="$t('lz.button.base.copy')" placement="top" :auto-close="1000">
								<el-button icon="ele-CopyDocument" text type="warning" v-auth="'sysConfigType:add'" @click="handleCopyConfigData(row)"> </el-button>
							</el-tooltip>
							<el-tooltip :content="$t('lz.button.base.edit')" placement="top" :auto-close="1000">
								<el-button icon="ele-Edit" text type="primary" v-auth="'sysConfigType:update'" @click="handleEditConfigData(row)"> </el-button>
							</el-tooltip>
							<el-tooltip :content="$t('lz.button.base.delete')" placement="top" :auto-close="1000">
								<el-button icon="ele-Delete" text type="danger" v-auth="'sysConfigType:delete'" @click="handleDeleteConfigData(row)"> </el-button>
							</el-tooltip>
						</template>
					</vxe-grid>
				</el-card>
			</el-col>
			<!-- SysConfigData: end -->
		</el-row>

		<!-- Components: begin -->
		<EditConfigTypeComp ref="editConfigTypeCompRef" :title="state.title" :operate-type="state.operateType" @handleQuery="handleQueryConfigType" @handleUpdate="updateConfigSession" />
		<EditConfigDataComp ref="editConfigDataCompRef" :title="state.title" :operate-type="state.operateType" @handleQuery="handleQueryConfigData" @handleUpdate="updateConfigSession" />
		<!-- Components: end -->
	</div>
</template>

<script lang="ts" setup>
import { onMounted, reactive, ref, defineAsyncComponent, watch } from 'vue';
import { ElMessageBox, ElMessage } from 'element-plus';
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useI18n } from 'vue-i18n';

import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { getAPI } from '/@/utils/axios-utils';

import ModifyRecord from '/@/components/table/modifyRecord.vue';
import EditConfigTypeComp from '/@/views/cfgSystem/sysConfigType/component/editConfigType.vue';
import EditConfigDataComp from '/@/views/cfgSystem/sysConfigType/component/editConfigData.vue';

import { SysConfigTypeApi, SysConfigDataApi } from '/@/api-services/cfgSystem';
import { UpdateSysConfigTypeInput, PageSysConfigTypeInput, UpdateSysConfigDataInput, PageSysConfigDataInput } from '/@/api-services/cfgSystem';
import { OperateType } from '/@/types/constants';
import { useBaseConfig } from '/@/stores/baseConfig';
import { BaseConfigTypeEnum } from '/@/types/enums';

const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
const getConfigItemByCode = useBaseConfig().getConfigItemByCode;

import { EntityFieldLength } from '/@/types/enums/baseEnum';

const { t: $t } = useI18n();
const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));
const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));
const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));
const xGridSysConfigType = ref<VxeGridInstance>();
const xGridSysConfigData = ref<VxeGridInstance>();

const editConfigTypeCompRef = ref<InstanceType<typeof EditConfigTypeComp>>();
const editConfigDataCompRef = ref<InstanceType<typeof EditConfigTypeComp>>();

const state = reactive({
	title: '',
	operateType: 'add' as OperateType,
	statusList: [],

	queryParamsConfigType: {
		name: undefined,
		code: undefined,
	},

	queryParamsConfigData: {
		configTypeId: undefined,
		name: undefined,
		code: undefined,
		value: undefined,
	},

	localPageParamConfigType: {
		pageSize: 50 as number,
		defaultSort: { field: 'orderNo', order: 'asc', descStr: 'desc' } as any,
	},

	localPageParamConfigData: {
		pageSize: 50 as number,
		defaultSort: { field: 'orderNo', order: 'asc', descStr: 'desc' } as any,
	},
});

onMounted(async () => {
	state.statusList = getConfigDataByCode(BaseConfigTypeEnum.SysStatusType);
});

// Global session cache
const updateConfigSession = async () => {
	await useBaseConfig().setConfigDataList();
};

/* SysConfigType: begin ======================== */

// Declare config-options for the SysConfigType grid
const optionsConfigTypeGrid = useVxeTable<UpdateSysConfigTypeInput>(
	{
		id: 'sysConfigTypeGrid',
		name: 'sysConfigTypeGrid',
		columns: [
			{ type: 'seq', title: $t('lz.label.base.idx'), width: 60, fixed: 'left' },
			{ field: 'name', title: $t('lz.entity.base.name'), minWidth: 140, showOverflow: 'tooltip', sortable: true },
			{ field: 'code', title: $t('lz.entity.base.code'), minWidth: 140, showOverflow: 'tooltip', sortable: true },
			{ field: 'status', title: $t('lz.entity.base.status'), width: 110, showOverflow: 'tooltip', slots: { default: 'row_status' } },
			{ field: 'infoClm', title: $t('lz.label.base.audit'), width: 100, showOverflow: 'tooltip', slots: { default: 'row_record' } },
			{ field: 'actionClm', title: $t('lz.label.base.action'), fixed: 'right', width: 100, showOverflow: true, slots: { default: 'row_buttons' } },
		],
	},
	// vxeGrid configuration parameters (can override any parameters), refer to vxe-table official documentation
	{
		// Proxy configuration
		proxyConfig: { autoLoad: true, ajax: { query: ({ page, sort }) => handleQueryConfigTypeApi(page, sort) } },

		// Sorting configuration
		sortConfig: { defaultSort: state.localPageParamConfigType.defaultSort },

		// Pagination configuration
		pagerConfig: { pageSize: state.localPageParamConfigType.pageSize },

		// Toolbar configuration
		toolbarConfig: { export: false },
		rowConfig:{isHover:true, isCurrent: true,keyField:'id'}
	}
);

// Table events
const gridEventsConfigType: VxeGridListeners<UpdateSysConfigTypeInput> = {
	// Only effective when pager-config is configured, this event will be triggered when pagination changes
	async pageChange({ pageSize }) {
		state.localPageParamConfigType.pageSize = pageSize;
	},
	// This event will be triggered when sorting conditions change
	async sortChange({ field, order }) {
		state.localPageParamConfigType.defaultSort = { field: field, order: order!, descStr: 'desc' };
	},
};

// Query API
const handleQueryConfigTypeApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
	const params = Object.assign
	(
		state.queryParamsConfigType,
		{ page: page.currentPage, pageSize: page.pageSize, field: sort.field, order: sort.order, descStr: 'desc' }
	) as PageSysConfigTypeInput;

	return getAPI(SysConfigTypeApi).apiCfgsystemSysconfigtypePagePost(params);
};


// Query operation
const handleQueryConfigType = async () => {
	await xGridSysConfigType.value?.commitProxy('reload') ;
};

const handleConfigDataQuery = async () => {
	await xGridSysConfigData.value?.commitProxy('reload') ;
};

// Reset operation
const resetQueryConfigType = async () => {
	state.queryParamsConfigType.name = undefined;
	state.queryParamsConfigType.code = undefined;
	await xGridSysConfigType.value?.commitProxy('reload');
};

// Open add dialog
const handleAddConfigType = () => {
	state.title = $t('lz.label.base.titleAdd', { name: $t('lz.label.cfgSystem.sysConfigType') });
	state.operateType = 'add';
	editConfigTypeCompRef.value?.openDialog({ status: 1, orderNo: 100 });
};

// Open copy dialog
const handleCopyConfigType = (row: any) => {
	state.title = $t('lz.label.base.titleCopy', { name: $t('lz.label.cfgSystem.sysConfigType') });
	state.operateType = 'copy';

	editConfigTypeCompRef.value?.openDialog(row);
};

// Open edit dialog
const handleEditConfigType = (row: any) => {
	state.title = $t('lz.label.base.titleEdit', { name: $t('lz.label.cfgSystem.sysConfigType') });
	state.operateType = 'edit';
	editConfigTypeCompRef.value?.openDialog(row);
};

// Delete configType
const handleDeleteConfigType = (row: any) => {
	ElMessageBox.confirm($t('lz.message.base.confirmDelete', { name: row.name }), $t('lz.label.base.confirm'), {
		confirmButtonText: $t('lz.button.base.confirm'),
		cancelButtonText: $t('lz.button.base.cancel'),
		type: 'warning',
	})
		.then(async () => {
			try {
				await getAPI(SysConfigTypeApi).apiCfgsystemSysconfigtypeDeletePost({ id: row.id });
				await handleQueryConfigType();
				await updateConfigSession();
				xGridSysConfigData.value?.loadData([]);
				ElMessage.success($t('lz.message.base.deleteSuccess'));
			} catch (error) {
				ElMessage.error($t('lz.message.base.processUnSuccess'));
			}
		})
		.catch(() => {});
};

// List configData by ConfigType
const handleConfigDataByParent = async (scope: any) => {
	state.queryParamsConfigData.configTypeId = scope.row.id;
	await handleQueryConfigData();
};

// Custom column
function handleConfigTypeCustom() {
	xGridSysConfigType.value?.openCustom();
}

// Custom column
function handleConfigDataCustom() {
	xGridSysConfigData.value?.openCustom();
}

/* SysConfigType: end =============================== */

/* SysConfigData: begin =============================== */

// Declare config-options for the SysConfigData
const optionsConfigDataGrid = useVxeTable<UpdateSysConfigDataInput>(
	{
		id: 'sysConfigDataGrid',
		name: 'sysConfigDataGrid',
		columns: [
			{ type: 'seq', title: $t('lz.label.base.idx'), width: 60, fixed: 'left' },
			{ field: 'name', title: $t('lz.entity.base.name'), minWidth: 140, showOverflow: 'tooltip', sortable: true },
			{ field: 'code', title: $t('lz.entity.base.code'), minWidth: 140, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_code' } },
			{ field: 'value', title: $t('lz.entity.sysConfigData.value'), minWidth: 140, showOverflow: 'tooltip', sortable: true },
			{ field: 'status', title: $t('lz.entity.base.status'), width: 110, showOverflow: 'tooltip', slots: { default: 'row_status' } },
			{ field: 'infoClm', title: $t('lz.label.base.audit'), width: 100, showOverflow: 'tooltip', slots: { default: 'row_record' } },
			{ field: 'actionClm', title: $t('lz.label.base.action'), fixed: 'right', width: 100, showOverflow: true, slots: { default: 'row_buttons' } },
		],
	},
	// vxeGrid configuration parameters (can override any parameters), refer to vxe-table official documentation
	{
		// Proxy configuration
		proxyConfig: { autoLoad: false, ajax: { query: ({ page, sort }) => handleQueryConfigDataApi(page, sort) } },

		// Sorting configuration
		sortConfig: { defaultSort: state.localPageParamConfigData.defaultSort },

		// Pagination configuration
		pagerConfig: { pageSize: state.localPageParamConfigData.pageSize },

		// Toolbar configuration
		toolbarConfig: { export: false },
	}
);

// Query API
const handleQueryConfigDataApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
	if ((state.queryParamsConfigData.configTypeId ?? '') === '')
		return;

	const params = Object.assign
	(
		state.queryParamsConfigData,
		{ page: page.currentPage, pageSize: page.pageSize, field: sort.field, order: sort.order, descStr: 'desc' }
	) as PageSysConfigDataInput;

	return getAPI(SysConfigDataApi).apiCfgsystemSysconfigdataPagePost(params);
};

// Query operation
const handleQueryConfigData = async () => {
	await xGridSysConfigData.value?.commitProxy('reload') ;
};

// Reset operation
const resetQueryConfigData = async () => {
	state.queryParamsConfigData.name = undefined;
	state.queryParamsConfigData.value = undefined;
	state.queryParamsConfigData.code = undefined;
	await xGridSysConfigData.value?.commitProxy('reload');
};

// Open add dialog
const handleAddConfigData = () => {
	if (!state.queryParamsConfigData.configTypeId) {
		ElMessage.warning($t('lz.message.sysConfigData.parentIsRequired'));
		return;
	}

	state.title = $t('lz.label.base.titleAdd', { name: $t('lz.label.cfgSystem.sysConfigData') });
	state.operateType = 'add';
	editConfigDataCompRef.value?.openDialog({ status: 1, orderNo: 100, configTypeId: state.queryParamsConfigData.configTypeId, tagType: 'primary' });
};

// Open copy dialog
const handleCopyConfigData = (row: any) => {
	state.title = $t('lz.label.base.titleCopy', { name: $t('lz.label.cfgSystem.sysConfigData') });
	state.operateType = 'copy';

	editConfigDataCompRef.value?.openDialog(row);
};

// Open edit dialog
const handleEditConfigData = (row: any) => {
	state.title = $t('lz.label.base.titleEdit', { name: $t('lz.label.cfgSystem.sysConfigData') });
	state.operateType = 'edit';
	editConfigDataCompRef.value?.openDialog(row);
};

// Delete configData
const handleDeleteConfigData = (row: any) => {
	ElMessageBox.confirm($t('lz.message.base.confirmDelete', { name: row.code }), $t('lz.label.base.confirm'), {
		confirmButtonText: $t('lz.button.base.confirm'),
		cancelButtonText: $t('lz.button.base.cancel'),
		type: 'warning',
	})
		.then(async () => {
			try {
				await getAPI(SysConfigDataApi).apiCfgsystemSysconfigdataDeletePost({ id: row.id });
				await handleQueryConfigData();
				await updateConfigSession();
				ElMessage.success($t('lz.message.base.deleteSuccess'));
			} catch (error) {
				ElMessage.error($t('lz.message.base.processUnSuccess'));
			}
		})
		.catch(() => {});
};

// Table events
const gridEventsConfigData: VxeGridListeners<UpdateSysConfigDataInput> = {
	// Only effective when pager-config is configured, this event will be triggered when pagination changes
	async pageChange({ pageSize }) {
		state.localPageParamConfigData.pageSize = pageSize;
	},
	// This event will be triggered when sorting conditions change
	async sortChange({ field, order }) {
		state.localPageParamConfigData.defaultSort = { field: field, order: order!, descStr: 'desc' };
	},
};

/* SysConfigData: end =============================== */
</script>

<style lang="scss" scoped>
.full-height {
	display: flex;
	flex-direction: column;
	height: 100%;
}
</style>
