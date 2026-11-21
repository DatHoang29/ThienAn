<template>
	<div class="tms-module-container">
		<el-card shadow="hover"
			:body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', height: '100%', alignItems: 'start' }">
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" label-position="left"  :inlineMessage="true"
				label-width="auto" style="flex: 1 1 0%">
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.tmsModule.keyName')" prop="keyName">
							<el-input v-model="state.queryParams.keyName" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.tmsModule.keyName')})"
								clearable
								@keyup.enter.native="handleQuery" @clear="state.queryParams.keyName = undefined"/>
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
						<el-form-item  :label="$t('lz.label.system.plugin')" prop="pluginId">
							<el-select  v-model="state.queryParams.pluginId" 
							:placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.label.system.plugin')})"
								clearable @clear="state.queryParams.pluginId = undefined">
								<el-option 
									v-for="plugin in state.pluginData" 
									:key="plugin.id" 
									:label="plugin.isDelete != null ? `${plugin.name} (${$t('lz.label.base.deleted')})` : plugin.name" 
									:value="plugin.id" 
								>
									<span :style="{ color: plugin.isDelete != null ? 'var(--el-text-color-secondary)' : 'inherit', textDecoration: plugin.isDelete != null ? 'line-through' : 'none' }">
										{{ plugin.isDelete != null ? `${plugin.name} (${$t('lz.label.base.deleted')})` : plugin.name }}
									</span>
								</el-option>

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
				<SearchButton :auth-search="'tmsModule:page'" @search="handleQuery" @reset="resetQuery" :loading="options.loading" />						
			</el-row>
		</el-card>

		<el-card class="full-table" shadow="hover" style="margin-top: 5px">
			<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents"
				@checkbox-change="selectChangeEvent" @checkbox-all="selectChangeEvent">
				<template #toolbar_tools>
					<TableHeader @add="handleAdd" :show-delete="state.showBatchDelete" :auth-add="'tmsModule:add'"
						:auth-delete="'tmsModule:delete'" @refresh="handleQuery" @custom-column="handleCustom"
						@delete="handleBatchDelete">
					</TableHeader>
				</template>
				<template #empty>
					<el-empty :image-size="200" />
				</template>
				<template #row_plugin="{ row }">
		<span v-if="state.pluginData.find((item:any) => item.id === row.pluginId)">
			{{ state.pluginData.find((item:any) => item.id === row.pluginId)?.name }}
			<span 
				v-if="state.pluginData.find((item:any) => item.id === row.pluginId)?.isDelete != null" 
				style="color: var(--el-color-danger); margin-left: 5px;">
				({{ $t('lz.label.base.deleted') }})
			</span>
		</span>
	</template>
				<template #row_status="{ row }">
					<TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.SysStatusType, row.status)" />
				</template>
				<template #row_record="{ row }">
					<ModifyRecord :data="row" />
				</template>
				<template #row_buttons="{ row }">
					<el-tooltip :content="$t('lz.button.base.copy')" placement="top" :auto-close="1000">
						<el-button icon="ele-CopyDocument" text type="warning" v-auth="'tmsModule:add'"
							@click="handleCopy(row)"> </el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.edit')" placement="top" :auto-close="1000">
						<el-button icon="ele-Edit" text type="primary" v-auth="'tmsModule:update'"
							@click="handleEdit(row)"> </el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.delete')" placement="top" :auto-close="1000">
						<el-button icon="ele-Delete" text type="danger" v-auth="'tmsModule:delete'"
							@click="handleDelete(row)"> </el-button>
					</el-tooltip>

				</template>
				<template #header_name>
				</template>
			</vxe-grid>
		</el-card>

		<EditModule ref="editModuleRef" :title="state.title" :plugin-data="state.pluginData" :operateType="state.operateType" @handleQuery="handleQuery" />
	</div>
</template>

<script lang="ts" setup name="tmsModule">
import { onMounted, reactive, ref, defineAsyncComponent } from 'vue';
import { ElMessageBox, ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { Local } from '/@/utils/storage';
import EditModule from './component/editModule.vue';
import ModifyRecord from '/@/components/table/modifyRecord.vue';
import { getAPI } from '/@/utils/axios-utils';
import { TmsModulesApi } from '/@/api-services/tms';
import { PageTmsModuleInput, PageTmsModuleOutput } from '/@/api-services/tms';
import { OperateType } from '/@/types/constants/operate';
import { SysPluginApi } from '/@/api-services/system';
import { useBaseConfig } from '/@/stores/baseConfig';
import { BaseConfigTypeEnum } from '/@/types/enums'; 
const getConfigItemByCode = useBaseConfig().getConfigItemByCode;
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));
const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));
const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));
const { t: $t } = useI18n();
const xGrid = ref<VxeGridInstance>();
const editModuleRef = ref<InstanceType<typeof EditModule>>();
const state = reactive({
	queryParams: {
		name: undefined,
		keyName: undefined,
		pluginId: undefined,
		status: undefined,
	},
	localPageParam: {
		pageSize: 50 as number,
		defaultSort: {} as any,
	},
	visible: false,
	title: '',
	operateType: 'add' as OperateType,
	pluginData: [] as Array<SysPluginApi>,
	showBatchDelete: false,
});
// Local storage parameters
const localPageParamKey = 'localPageParam:tmsModule';
// Table parameter configuration
const options = useVxeTable<PageTmsModuleOutput>(
	{
		id: 'tmsModule',
		name: 'Modules',
		columns: [
			{ type: 'checkbox', fixed: 'left', width: 45 },
			{ field: 'keyName', title: $t('lz.entity.tmsModule.keyName'), minWidth: 200, showOverflow: 'tooltip', sortable: true,     	},
			{ field: 'name', title: $t('lz.entity.base.name'), minWidth: 200, showOverflow: 'tooltip', sortable: true },
			{ field: 'pluginId', title: $t('lz.label.system.plugin'), minWidth: 200, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_plugin' } },
			{ field: 'status', title: $t('lz.entity.base.status'), width: 120, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_status' } },
			{ field: '', title: $t('lz.label.base.audit'), width: 100, showOverflow: 'tooltip', slots: { default: 'row_record' } },
			{ title: $t('lz.label.base.action'), fixed: 'right', width: 100, showOverflow: true, slots: { default: 'row_buttons' } },
		],
		checkboxConfig: {
            checkMethod: ({ row }) => {
                return !(row.isDelete || row.status === 0);
            }
        }
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

// Page initialization
onMounted(async () => {
	state.localPageParam = Local.get(localPageParamKey) || state.localPageParam;
	await handleQuery();
	// const res = await getAPI(SysPluginApi).apiSystemSyspluginListGet();
	const res = await getAPI(SysPluginApi).apiSystemSyspluginListGet( undefined, undefined, undefined, true as any);
	state.pluginData = res.data.result ?? [];
});

// Query API
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
	const params = Object.assign(state.queryParams, { page: page.currentPage, pageSize: page.pageSize, field: sort?.order ? sort?.field : undefined, order: sort?.order ? sort?.order : undefined, descStr: 'desc' }) as PageTmsModuleInput;
	return await getAPI(TmsModulesApi).apiTmsTmsmodulesPagePost(params);
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
	state.queryParams.keyName = undefined;
	state.queryParams.pluginId = undefined;
	state.queryParams.name = undefined;
	state.queryParams.status = undefined;
	await xGrid.value?.commitProxy('reload');
};

// Open add dialog
const handleAdd = () => {
	state.title = $t('lz.label.base.titleAdd', { name: $t('lz.entity.tms.module') });
	state.operateType = 'add';
	editModuleRef.value?.openDialog({ status: 1, orderNo: 100 });
};

// Open copy dialog
const handleCopy = (row: any) => {
	state.title = $t('lz.label.base.titleCopy', { name: $t('lz.entity.tms.module') });
	state.operateType = 'copy';
	editModuleRef.value?.openDialog(row);
};

// Open edit dialog
const handleEdit = (row: any) => {
	state.title = $t('lz.label.base.titleEdit', { name: $t('lz.entity.tms.module') });
	state.operateType = 'edit';
	editModuleRef.value?.openDialog(row);
};


// Delete
const handleDelete = (row: any) => {
	ElMessageBox.confirm($t('lz.message.base.confirmDelete', { name: row.name }), $t('lz.message.base.warning'), {
		confirmButtonText: $t('lz.button.base.confirm'),
		cancelButtonText: $t('lz.button.base.cancel'),
		type: 'warning',
	})
		.then(async () => {
			await getAPI(TmsModulesApi).apiTmsTmsmodulesDeletePost({ id: row.id });
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
			await getAPI(TmsModulesApi).apiTmsTmsmodulesBatchdeletePost(selectRecords.filter(l => l.id));
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
const gridEvents: VxeGridListeners<PageTmsModuleOutput> = {
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
