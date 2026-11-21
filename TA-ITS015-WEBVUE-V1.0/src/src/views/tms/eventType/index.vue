<template>
	<div class="tms-eventtype-container">
		<el-card shadow="hover"
			:body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', height: '100%', alignItems: 'start' }">
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true" 
				label-width="auto" style="flex: 1 1 0%" label-position="left">
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.name')" prop="name">
							<el-input v-model="state.queryParams.name" 
								:placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.name')})" clearable
								@keyup.enter.native="handleQuery" />
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.tms.module')" prop="moduleId">
							<el-select v-model="state.queryParams.moduleId" 
								:placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.entity.tms.module')})"
								filterable clearable class="w100" @clear="state.queryParams.moduleId = undefined">
								<el-option 
									v-for="item in state.ModuleList" 
									:key="item.id" 
									:label="item.isDelete != null ? `${item.name} (${$t('lz.label.base.deleted')})` : item.name" 
									:value="item.id"
								>
									<span :style="{ color: item.isDelete != null ? 'var(--el-text-color-secondary)' : 'inherit', textDecoration: item.isDelete != null ? 'line-through' : 'none' }">
										{{ item.isDelete != null ? `${item.name} (${$t('lz.label.base.deleted')})` : item.name }}
									</span>
								</el-option>
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.tms.eventType')" prop="type">
							<el-select v-model="state.queryParams.type" 
								:placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.entity.tms.eventType')})"
								filterable clearable class="w100" @clear="state.queryParams.type = undefined">
								<el-option v-for="item in state.typeList" :key="item.code" :label="$t(`lz.param.event_group.${item.code}`, item.name)" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.tmsEventType.warningType')" prop="warningType">
							<el-select v-model="state.queryParams.warningType"
								:placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.entity.tmsEventType.warningType')})"
								filterable clearable class="w100" @clear="state.queryParams.warningType = undefined">
								<el-option v-for="item in state.warningTypeList" :key="item.code" :label="$t(`lz.param.event_warning_type.${item.code}`, item.name)" :value="item.code" />
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
				<SearchButton :auth-search="'tmsEventType:page'" @search="handleQuery" @reset="resetQuery" :loading="options.loading" />					
			</el-row>
		</el-card>

		<el-card class="full-table" shadow="hover" style="margin-top: 5px">
			<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents"
				@checkbox-change="selectChangeEvent" @checkbox-all="selectChangeEvent">
				<template #row_icon="{ row, column }">
					<div class="panel-img">
						<img v-if="row[column.field]" :src="state.baseUrl+'/'+row[column.field]" />
					</div>
				</template>
				<template #row_type="{ row }">
					<span>
						{{ state.typeList.find((item:any) => item.code === row.type) ? $t(`lz.param.event_group.${row.type}`, state.typeList.find((item:any) => item.code === row.type)?.name) : row.type }}
					</span>
				</template>
				<template #row_warningType="{ row }">
					<span>
						{{ state.warningTypeList.find((item:any) => item.code === row.warningType) ? $t(`lz.param.event_warning_type.${row.warningType}`, state.warningTypeList.find((item:any) => item.code === row.warningType)?.name) : row.warningType }}
					</span>
				</template>
				<template #row_module="{ row }">
					<span v-if="state.ModuleList.find((item: any) => item.id === row.moduleId)">
						{{ state.ModuleList.find((item: any) => item.id === row.moduleId)?.name }}
						<span 
							v-if="state.ModuleList.find((item: any) => item.id === row.moduleId)?.isDelete != null" 
							style="color: var(--el-color-danger); margin-left: 5px;">
							({{ $t('lz.label.base.deleted') }})
						</span>
					</span>
				</template>
				<template #toolbar_tools>
					<TableHeader @add="handleAdd" :show-delete="state.showBatchDelete" :auth-add="'tmsEventType:add'"
						:auth-delete="'tmsEventType:delete'" @refresh="handleQuery" @custom-column="handleCustom"
						@delete="handleBatchDelete">
					</TableHeader>
				</template>
				<template #empty>
					<el-empty :image-size="200" />
				</template>
				<template #row_status="{ row }">
					<el-tag v-if="row.status === 1" type="success">{{ $t('lz.label.base.enable') }}</el-tag>
					<el-tag v-else type="danger">{{ $t('lz.label.base.disable') }}</el-tag>
				</template>
				<template #row_record="{ row }">
					<ModifyRecord :data="row" />
				</template>
				<template #row_buttons="{ row }">
					<el-tooltip :content="$t('lz.button.base.copy')" placement="top" :auto-close="1000">
						<el-button icon="ele-CopyDocument" text type="warning" v-auth="'tmsEventType:add'"
							@click="handleCopy(row)"> </el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.edit')" placement="top" :auto-close="1000">
						<el-button icon="ele-Edit" text type="primary" v-auth="'tmsEventType:update'"
							@click="handleEdit(row)"> </el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.delete')" placement="top" :auto-close="1000">
						<el-button icon="ele-Delete" text type="danger" v-auth="'tmsEventType:delete'"
							@click="handleDelete(row)"> </el-button>
					</el-tooltip>

				</template>
				<template #header_name>
				</template>
			</vxe-grid>
		</el-card>

		<EditEventType ref="editEventTypeRef" :typeList="state.typeList" :warningTypeList="state.warningTypeList" :moduleList="state.ModuleList" :title="state.title" :operateType="state.operateType" @handleQuery="handleQuery" />
	</div>
</template>

<script lang="ts" setup name="tmsEventType">
import { computed, onMounted, reactive, ref, defineAsyncComponent } from 'vue';
import { ElMessageBox, ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { Local } from '/@/utils/storage';
import EditEventType from './component/editEventType.vue';
import ModifyRecord from '/@/components/table/modifyRecord.vue';
import { getAPI } from '/@/utils/axios-utils';
import { TmsEventTypeApi, TmsModulesApi } from '/@/api-services/tms';
import { PageEquipmentTypesOutput, PageEquipmentTypesInput } from '/@/api-services/tms';
import { OperateType } from '/@/types/constants/operate';
import { SysConfigDataApi } from '/@/api-services/cfgSystem';
import { useItsMediaServerStore } from '/@/stores/its/itsMediaServer';
import { useBaseConfig } from '/@/stores/baseConfig';
import { BaseConfigTypeEnum, StatusTypeEnum } from '/@/types/enums';
const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));
const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
const { t: $t } = useI18n();
const xGrid = ref<VxeGridInstance>();
const editEventTypeRef = ref<InstanceType<typeof EditEventType>>();
const state = reactive({
	queryParams: {
		name: undefined as any,
		moduleId: undefined as any,
		type: undefined as any,
		warningType: undefined as any,
		status: undefined as any,
	},
	localPageParam: {
		pageSize: 50 as number,
		defaultSort: { field: 'orderNo', order: 'asc', descStr: 'desc' },
	},
	visible: false,
	title: '',
	operateType: 'add' as OperateType,
	showBatchDelete: false,
	ModuleList: [] as any[],
	warningTypeList: [] as any,
	typeList: [] as any,
	baseUrl: '' as string,

});

// Local storage parameters
const localPageParamKey = 'localPageParam:tmsEventType';
// Table parameter configuration
const options = useVxeTable<PageEquipmentTypesOutput>(
	{
		id: 'tmsEventType',
		name: 'EventType',
		columns: [
			{ type: 'checkbox', fixed: 'left', width: 45 },
			{ field: 'url', title: $t('lz.entity.base.icon'), minWidth: 100, showOverflow: 'tooltip', slots: {default: 'row_icon'}	 },
			{ field: 'name', title: $t('lz.entity.base.name'), minWidth: 200, showOverflow: 'tooltip', sortable: true		},
			{ field: 'type', title: $t('lz.entity.base.type'), minWidth: 200, showOverflow: 'tooltip', sortable: true, slots: {default: 'row_type'} },
			{ field: 'warningType', title: $t('lz.entity.tmsEventType.warningType'), minWidth: 200, showOverflow: 'tooltip', sortable: true, slots: {default: 'row_warningType'} },
			{ field: 'moduleId', title: $t('lz.entity.tms.module'), minWidth: 80, sortable: true, slots: { default: 'row_module' }, showOverflow: 'tooltip' },
			{ field: 'size', title: $t('lz.entity.base.size'), minWidth: 90, showOverflow: 'tooltip', sortable: true },
			{ field: 'priority', title: $t('lz.entity.base.priority'), minWidth: 200, showOverflow: 'tooltip', sortable: true },
			{ field: 'status', title: $t('lz.entity.base.status'), width: 110, showOverflow: 'tooltip',sortable: true, slots: { default: 'row_status' } },
			{ field: '', title: $t('lz.label.base.audit'), width: 100, showOverflow: 'tooltip', slots: { default: 'row_record' } },
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
		rowConfig: {height: 75}
	}
);

// Page initialization
onMounted(async () => {
	state.localPageParam = Local.get(localPageParamKey) || state.localPageParam;
	let resDicData = await getAPI(TmsModulesApi).apiTmsTmsmodulesListGet();
	state.ModuleList = resDicData.data.result;

	let resEventType = await getAPI(SysConfigDataApi).apiCfgsystemSysconfigdataDatalistCodeGet('event_group');
	state.typeList = resEventType.data.result;
	let resWarningType = await getAPI(SysConfigDataApi).apiCfgsystemSysconfigdataDatalistCodeGet('event_warning_type');
	state.warningTypeList = resWarningType.data.result;
	// Địa chỉ file server dùng chung (chỉ gọi API 1 lần cho toàn app)
	state.baseUrl = await useItsMediaServerStore().ensureLoaded();

});

// Query API
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
	const params = Object.assign(state.queryParams, { page: page.currentPage, pageSize: page.pageSize, field: sort.field, order: sort.order, descStr: 'desc' }) as PageEquipmentTypesInput;
	return await getAPI(TmsEventTypeApi).apiTmsTmseventtypePagePost(params);
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
	state.queryParams.name = undefined;
	state.queryParams.moduleId = undefined;
	state.queryParams.type = undefined;
	state.queryParams.warningType = undefined;
	state.queryParams.status = undefined;
	await xGrid.value?.commitProxy('reload');
};

// Open add dialog
const handleAdd = () => {
	state.title = $t('lz.label.base.titleAdd', { name: $t('lz.entity.tms.eventType') });
	state.operateType = 'add';
	const incidentType = state.typeList.find((item: any) => String(item.code).trim().toLowerCase() === 'incident');
	editEventTypeRef.value?.openDialog({ status: 1, orderNo: 100, type: incidentType?.code, warningType: undefined });
	
};

// Open copy dialog
const handleCopy = (row: any) => {
	state.title =  $t('lz.label.base.titleCopy', { name: $t('lz.entity.tms.eventType') });
	state.operateType = 'copy';
	editEventTypeRef.value?.openDialog(row);
};

// Open edit dialog
const handleEdit = (row: any) => {
	state.title = $t('lz.label.base.titleAdd', { name: $t('lz.entity.tms.eventType') });
	state.operateType = 'edit';
	editEventTypeRef.value?.openDialog(row);
};


// Delete
const handleDelete = (row: any) => {
	ElMessageBox.confirm($t('lz.message.base.confirmDelete', { name: row.name }), $t('lz.message.base.warning'), {
		confirmButtonText: $t('lz.button.base.confirm'),
		cancelButtonText: $t('lz.button.base.cancel'),
		type: 'warning',
	})
		.then(async () => {
			await getAPI(TmsEventTypeApi).apiTmsTmseventtypeDeletePost({ id: row.id });
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
			await getAPI(TmsEventTypeApi).apiTmsTmseventtypeBatchdeletePost(selectRecords.filter(l => l.id));
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
const gridEvents: VxeGridListeners<PageEquipmentTypesOutput> = {
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
<style scoped>
.panel-img img{
		max-width: 80%;
		max-height: 70px;
		padding: 3px;
	}
</style>