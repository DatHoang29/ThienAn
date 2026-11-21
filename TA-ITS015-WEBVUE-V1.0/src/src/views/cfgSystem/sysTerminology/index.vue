<template>
	<WorkspacePage>
		<div class="terminology-container">
			<GridSearchPanel id="cfg-sysTerminology-search">
				<template #filters>
					<el-form :model="state.queryParams" ref="queryForm" :show-message="false" label-position="left" :inlineMessage="true" label-width="auto" style="flex: 1 1 0%">
						<el-row :gutter="10">
							<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
								<el-form-item :label="$t('lz.entity.base.lang')" prop="language">
									<el-select v-model="state.queryParams.language" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.base.lang') })" clearable class="w100">
										<el-option :key="item.code" v-for="item in state.languageList" :label="item.name" :value="item.code" />
									</el-select>
								</el-form-item>
							</el-col>
							<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
								<el-form-item :label="$t('lz.entity.base.code')" prop="code">
									<el-input
										v-model="state.queryParams.code"
										:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.code') })"
										@keyup.enter="handleQuery"
										:maxlength="EntityFieldLength.Length256"
										show-word-limit
										clearable
									/>
								</el-form-item>
							</el-col>
							<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
								<el-form-item :label="$t('lz.entity.base.name')" prop="name">
									<el-select v-model="state.queryParams.name" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.base.name') })" clearable class="w100">
										<el-option :key="item.code" v-for="item in state.i18nPrefixList" :label="item.name" :value="item.code" clearable />
									</el-select>
								</el-form-item>
							</el-col>
							<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
								<el-form-item :label="$t('lz.entity.sysTerminolog.value')" prop="value">
									<el-input
										v-model="state.queryParams.value"
										:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.sysTerminolog.value') })"
										@keyup.enter="handleQuery"
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
						<SearchButton authSearch="sysTerminology:list" @search="handleQuery" @reset="resetQuery" :loading="options.loading" />
					</el-row>
				</template>
				<!-- SearchBar: end -->

				<!-- GridTable: begin -->
				<el-card class="full-table" shadow="hover">
					<vxe-grid ref="xGridSysTerminology" class="xGrid-style" v-bind="options" v-on="gridEvents" @checkbox-change="selectChangeEvent" @checkbox-all="selectChangeEvent">
						<template #toolbar_buttons>
							<GridSummary :summary="summary" />
						</template>
						<template #toolbar_tools>
							<TableHeader
								@add="handleAdd"
								:show-delete="state.showBatchDelete"
								:auth-add="'sysTerminology:add'"
								:auth-delete="'sysTerminology:delete'"
								@refresh="handleQuery"
								@custom-column="handleCustom"
								:loading="options.loading"
							>
							</TableHeader>
						</template>
						<template #empty>
							<el-empty :image-size="200" />
						</template>
						<template #row_status="{ row }">
							<TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.SysStatusType, row.status)" />
						</template>
						<template #row_name="{ row }">
							{{ state.i18nPrefixList?.find((item) => item.code == row.name)?.name }}
						</template>
						<template #row_language="{ row }">
							{{ state.languageList?.find((lang) => lang.code == row.lang)?.name }}
						</template>
						<template #row_record="{ row }">
							<ModifyRecord :data="row" />
						</template>
						<template #row_buttons="{ row }">
							<el-tooltip :content="$t('lz.button.base.copy')" placement="top" :auto-close="1000">
								<el-button icon="ele-CopyDocument" text type="warning" v-auth="'sysTerminology:add'" @click="handleCopy(row)"> </el-button>
							</el-tooltip>
							<el-tooltip :content="$t('lz.button.base.edit')" placement="top" :auto-close="1000">
								<el-button icon="ele-Edit" text type="primary" v-auth="'sysTerminology:update'" @click="handleEdit(row)"> </el-button>
							</el-tooltip>
							<el-tooltip :content="$t('lz.button.base.delete')" placement="top" :auto-close="1000">
								<el-button icon="ele-Delete" text type="danger" v-auth="'sysTerminology:delete'" @click="handleDelete(row)"> </el-button>
							</el-tooltip>
						</template>
						<template #header_name>
							{{ $t('lz.label.cfgSystem.sysTerminology') }}
						</template>
					</vxe-grid>
				</el-card>
			</GridSearchPanel>
			<!-- GridTable: end -->

			<!-- Primary components: Begin -->
			<editTerminologyComp ref="editTerminologyCompRef" :title="state.title" :operateType="state.operateType" @handleQuery="handleQuery" />
			<!-- Primary components: End -->
		</div>
	</WorkspacePage>
</template>

<script lang="ts" setup name="sysTerminology">
import WorkspacePage from '/@/components/workspace/WorkspacePage.vue';
import GridSearchPanel from '/@/components/gridPage/GridSearchPanel.vue';
import GridSummary from '/@/components/gridPage/GridSummary.vue';
import { useGridSummary } from '/@/components/gridPage/useGridSummary';
import { useGridPager } from '/@/components/gridPage/useGridPager';

import { onActivated, onMounted, reactive, ref, defineAsyncComponent } from 'vue';
import { ElMessageBox, ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { getAPI } from '/@/utils/axios-utils';
import { OperateType } from '/@/types/constants/operate';
import ModifyRecord from '/@/components/table/modifyRecord.vue';

import { SysTerminologyApi } from '/@/api-services/cfgSystem';
import { PageSysTerminologyInput, PageSysTerminologyOutput } from '/@/api-services/cfgSystem';

import { useBaseConfig } from '/@/stores/baseConfig';
import { BaseConfigTypeEnum } from '/@/types/enums';

import { EntityFieldLength } from '/@/types/enums/baseEnum';

import editTerminologyComp from '/@/views/cfgSystem/sysTerminology/component/editTerminology.vue';
const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
const getConfigItemByCode = useBaseConfig().getConfigItemByCode;
const editTerminologyCompRef = ref<InstanceType<typeof editTerminologyComp>>();

const state = reactive({
	queryParams: {
		name: undefined,
		code: undefined,
		value: undefined,
		language: undefined,
	},
	localPageParam: {
		pageSize: 50 as number,
		defaultSort: { field: 'orderNo', order: 'asc', descStr: 'desc' } as any,
	},
	title: '',
	operateType: 'add' as OperateType,
	showBatchDelete: false,
	statusList: [] as any,
	languageList: [] as any,
	i18nPrefixList: [] as any,
});

const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));
const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));
const { t: $t } = useI18n();
const { summary, track } = useGridSummary();
const xGridSysTerminology = ref<VxeGridInstance>();

const options = useVxeTable<PageSysTerminologyOutput>(
	{
		id: 'sysTerminologyGrid',
		name: 'sysTerminologyGrid',
		columns: [
			{ type: 'checkbox', fixed: 'left', width: 45 },
			{ field: 'lang', title: $t('lz.entity.base.lang'), minWidth: 100, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_language' } },
			{ field: 'name', title: $t('lz.entity.base.name'), minWidth: 100, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_name' } },
			{ field: 'code', title: $t('lz.entity.base.code'), minWidth: 200, showOverflow: 'tooltip', sortable: true },
			{ field: 'value', title: $t('lz.entity.sysTerminolog.value'), minWidth: 200, showOverflow: 'tooltip', sortable: true },
			{ field: 'status', title: $t('lz.entity.base.status'), width: 110, showOverflow: 'tooltip', slots: { default: 'row_status' } },
			{ field: '', title: $t('lz.label.base.audit'), width: 100, showOverflow: 'tooltip', slots: { default: 'row_record' } },
			{ title: $t('lz.label.base.action'), fixed: 'right', width: 100, showOverflow: true, slots: { default: 'row_buttons' } },
		],
	},
	// vxeGrid configuration parameters (any parameter can be overridden here), refer to the official vxe-table documentation
	{
		// Proxy configuration
		proxyConfig: { autoLoad: true, ajax: { query: ({ page, sort }) => handleQueryApi(page, sort) } },

		// Sorting configuration
		sortConfig: { defaultSort: state.localPageParam.defaultSort },

		// Pagination configuration
		pagerConfig: { pageSize: state.localPageParam.pageSize },

		// Toolbar configuration
		toolbarConfig: { export: false },
	}
);

onMounted(() => {
	state.languageList = getConfigDataByCode(BaseConfigTypeEnum.SysLanguageType);
	state.i18nPrefixList = getConfigDataByCode(BaseConfigTypeEnum.SysI18nPrefix);
});

// Query API
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
	const params = { ...state.queryParams, page: page.currentPage, pageSize: page.pageSize, field: sort.field, order: sort.order, descStr: 'desc' } as PageSysTerminologyInput;
	return track(getAPI(SysTerminologyApi).apiCfgsystemSysterminologyPagePost(params));
};

// Query operation
const handleQuery = async () => {
	options.loading = true;
	try {
		await xGridSysTerminology.value?.commitProxy('reload');
	} finally {
		options.loading = false;
	}
	selectChangeEvent();
};

// Reset operation
const resetQuery = async () => {
	state.queryParams.name = undefined;
	state.queryParams.code = undefined;
	state.queryParams.value = undefined;
	state.queryParams.language = undefined;
	await xGridSysTerminology.value?.commitProxy('reload');
};

// Open add dialog
const handleAdd = () => {
	state.title = $t('lz.label.base.titleAdd', { name: $t('lz.label.cfgSystem.sysTerminology') });
	state.operateType = 'add';
	editTerminologyCompRef.value?.openDialog({ status: 1, orderNo: 100, lang: 'vi-VN' });
};

// Open copy dialog
const handleCopy = (row: any) => {
	state.title = $t('lz.label.base.titleCopy', { name: $t('lz.label.cfgSystem.sysTerminology') });
	state.operateType = 'copy';

	editTerminologyCompRef.value?.openDialog(row);
};

// Open edit dialog
const handleEdit = (row: any) => {
	state.title = $t('lz.label.base.titleEdit', { name: $t('lz.label.cfgSystem.sysTerminology') });
	state.operateType = 'edit';

	editTerminologyCompRef.value?.openDialog(row);
};

// Delete
const handleDelete = (row: any) => {
	ElMessageBox.confirm($t('lz.message.base.confirmDelete', { name: row.code }), $t('lz.label.base.confirm'), {
		confirmButtonText: $t('lz.button.base.confirm'),
		cancelButtonText: $t('lz.button.base.cancel'),
		type: 'warning',
	})
		.then(async () => {
			try {
				await getAPI(SysTerminologyApi).apiCfgsystemSysterminologyDeletePost({ id: row.id });
				handleQuery();
				ElMessage.success($t('lz.message.base.processSuccess'));
			} catch (error) {
				ElMessage.error($t('lz.message.base.processUnSuccess'));
			}
		})
		.catch(() => {});
};

// Custom column
function handleCustom() {
	xGridSysTerminology.value?.openCustom();
}

// Get selected records
function getCheckedRecords() {
	return xGridSysTerminology.value?.getCheckboxRecords() ?? [];
}

function selectChangeEvent() {
	state.showBatchDelete = getCheckedRecords().length > 0;
}

// Table events
const gridEvents: VxeGridListeners<PageSysTerminologyOutput> = {
	// Only effective when pager-config is configured, this event will be triggered when pagination changes
	async pageChange({ pageSize }) {
		state.localPageParam.pageSize = pageSize;
	},
	// This event will be triggered when sorting conditions change
	async sortChange({ field, order }) {
		state.localPageParam.defaultSort = { field: field, order: order!, descStr: 'desc' };
	},
};

useGridPager(xGridSysTerminology, options);

// KeepAlive also activates on initial mount; the grid proxy already loads then.
let activated = false;
onActivated(async () => {
	if (!activated) {
		activated = true;
		return;
	}
	await xGridSysTerminology.value?.commitProxy('query');
});
</script>
