<template>
	<div class="tms-trafficplan-container">
		<el-card shadow="hover"
			:body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', height: '100%', alignItems: 'start' }">
			<el-form :model="state.queryParams" ref="queryForm" label-position="left" :show-message="false" :inlineMessage="true"
				label-width="auto" style="flex: 1 1 0%">
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.code')" prop="code">
							<el-input v-model="state.queryParams.code" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.code')})"
							clearable
								@keyup.enter.native="handleQuery" />
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.name')" prop="name">
							<el-input v-model="state.queryParams.name" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.name')})"
								clearable
								@keyup.enter.native="handleQuery" />
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.tms.eventType')" prop="eventTypeId">
							<el-select v-model="state.queryParams.eventTypeId" 
							:placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.entity.tms.eventType')})"
							filterable 
							clearable 
							class="w100"
							 @clear="state.queryParams.eventTypeId = undefined">
								<el-option v-for="item in filteredTypeList" :key="item.id" :label="item.name" :value="item.id" />
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
				<SearchButton :auth-search="'tmsTrafficPlan:page'" @search="handleQuery" @reset="resetQuery" :loading="options.loading" />				
			</el-row>
		</el-card>

		<el-card class="full-table" shadow="hover" style="margin-top: 5px">
			<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents"
				@checkbox-change="selectChangeEvent" @checkbox-all="selectChangeEvent">
				
				<template #toolbar_tools>
					<TableHeader @add="handleAdd" :show-delete="state.showBatchDelete" :auth-add="'tmsTrafficPlan:add'"
						:auth-delete="'tmsTrafficPlan:delete'" @refresh="handleQuery" @custom-column="handleCustom"
						@delete="handleBatchDelete">
					</TableHeader>
				</template>
				<template #empty>
					<el-empty :image-size="200" />
				</template>
				<template #row_state="{ row }">
					<el-tag v-if="row.equipmentState === 1" type="success"> {{ $t('lz.label.base.active') }}</el-tag>
					<el-tag v-else type="danger">{{ $t('lz.label.base.inactive') }}</el-tag>
				</template>
				<template #row_status="{ row }">
					<TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.SysStatusType, row.status)" />

				</template>
				<!-- SonTH - 24/07/2026: Hiển thị chế độ thực thi của kịch bản -->
				<template #row_executionMode="{ row }">
					<el-tag v-if="row.executionMode === 'Automatic'" type="success" effect="dark">{{ $t('lz.label.tmsTrafficPlan.executionModeAutomatic') }}</el-tag>
					<el-tag v-else-if="row.executionMode === 'Both'" type="warning" effect="dark">{{ $t('lz.label.tmsTrafficPlan.executionModeBoth') }}</el-tag>
					<el-tag v-else type="info">{{ $t('lz.label.tmsTrafficPlan.executionModeManual') }}</el-tag>
				</template>
				<template #row_record="{ row }">
					<ModifyRecord :data="row" />
				</template>
				<template #row_buttons="{ row }">
					<el-tooltip :content="$t('lz.button.base.copy')" placement="top" :auto-close="1000">
						<el-button icon="ele-CopyDocument" text type="warning" v-auth="'tmsTrafficPlan:add'"
							@click="handleCopy(row)"> </el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.edit')" placement="top" :auto-close="1000">
						<el-button icon="ele-Edit" text type="primary" v-auth="'tmsTrafficPlan:update'"
							@click="handleEdit(row)"> </el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.delete')" placement="top" :auto-close="1000">
						<el-button icon="ele-Delete" text type="danger" v-auth="'tmsTrafficPlan:delete'"
							@click="handleDelete(row)"> </el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.view')" placement="top">
						<el-button icon="ele-View" text type="primary" v-auth="'tmsTrafficPlan:view'"
							@click="handleView(row)"> </el-button>
					</el-tooltip>
				</template>
			</vxe-grid>
		</el-card>

		<DetailTrafficPlan ref="detailTrafficPlanRef" :title="state.title" :operateType="state.operateType" @handleQuery="handleQuery" />
		<EditTrafficPlan ref="editTrafficPlanRef" :title="state.title" :operateType="state.operateType" @handleQuery="handleQuery" />
	</div>
</template>

<script lang="ts" setup name="tmsTrafficPlan">
import { computed, onMounted, reactive, ref, defineAsyncComponent } from 'vue';
import { ElMessageBox, ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { Local } from '/@/utils/storage';
import EditTrafficPlan from './component/editTrafficPlan.vue';
import DetailTrafficPlan from './component/detailTrafficPlan.vue';
import ModifyRecord from '/@/components/table/modifyRecord.vue';
import { getAPI } from '/@/utils/axios-utils';
import { TmsTrafficPlanApi, TmsEventTypeApi } from '/@/api-services/tms';
import { PageTmsTrafficPlanOutput } from '/@/api-services/tms';
import { OperateType } from '/@/types/constants/operate';
import { BaseConfigTypeEnum, StatusTypeEnum } from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';
const getConfigItemByCode = useBaseConfig().getConfigItemByCode;
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));
const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));
const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));
const { t: $t } = useI18n();
const xGrid = ref<VxeGridInstance>();
const editTrafficPlanRef = ref<InstanceType<typeof EditTrafficPlan>>();
const detailTrafficPlanRef = ref<InstanceType<typeof DetailTrafficPlan>>();

const state = reactive({
	queryParams: {
		code:undefined,
		name: undefined,
		eventTypeId: undefined,
		status: undefined,
	},
	localPageParam: {
		pageSize: 50 as number,
		defaultSort: {} as any,
	},
	visible: false,
	title: '',
	operateType: 'add' as OperateType,
	showBatchDelete: false,
	TypeList: [] as any[],

});

const filteredTypeList = computed(() => {
	return (state.TypeList || []).filter((item: any) => item.status === StatusTypeEnum.Enable);
});
// Local storage parameters
const localPageParamKey = 'localPageParam:tmsTrafficPlan';
// Table parameter configuration
const options = useVxeTable<PageTmsTrafficPlanOutput>(
	{
		id: 'tmsTrafficPlan', 
		name: 'tmsTrafficPlan',
		columns: [
			{ type: 'checkbox', fixed: 'left', width: 45 },
			{ field: 'code', title: $t('lz.entity.base.code'), minWidth: 150, showOverflow: 'tooltip', sortable: true },
			{ field: 'name', title: $t('lz.entity.base.name'), minWidth: 200, showOverflow: 'tooltip', sortable: true, },
			{ field: 'eventTypeId', title: $t('lz.entity.tms.eventType'), minWidth: 150,sortable: true, formatter: ({ cellValue }: any) => state.TypeList.find((u: any) => u.id == cellValue)?.name, showOverflow: 'tooltip' },
			// SonTH - 24/07/2026: Cột chế độ thực thi để phân biệt kịch bản dùng cho thủ công / tự động
			{ field: 'executionMode', title: $t('lz.entity.tmsTrafficPlan.executionMode'), width: 130, sortable: true, slots: { default: 'row_executionMode' } },
			// { field: 'remark', title: $t('message.ui.remark'), minWidth: 80, showOverflow: 'tooltip', sortable: true,},
			{ field: 'status', title: $t('lz.label.base.status'), width: 150, showOverflow: 'tooltip',sortable: true, slots: { default: 'row_status' } },
			{ field: '', title: $t('lz.label.base.audit'), width: 100, showOverflow: 'tooltip', slots: { default: 'row_record' } },
			{ title: $t('lz.label.base.action'), fixed: 'right', width: 150, showOverflow: true, slots: { default: 'row_buttons' } },
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

// Page initialization
onMounted(async () => {
	state.localPageParam = Local.get(localPageParamKey) || state.localPageParam;
	const resEventType = await getAPI(TmsEventTypeApi).apiTmsTmseventtypeListGet();
	state.TypeList = resEventType.data.result;
	await handleQuery();

});

// Query API
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
	const params = Object.assign(state.queryParams, { page: page.currentPage, pageSize: page.pageSize, field: sort?.order ? sort?.field : undefined, order: sort?.order ? sort?.order : undefined, descStr: 'desc' }) as PagetmsTrafficPlanInput;
	return await getAPI(TmsTrafficPlanApi).apiTmsTmstrafficplanPagePost(params);
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
	state.queryParams.eventTypeId = undefined;
	state.queryParams.status = undefined;
	await xGrid.value?.commitProxy('reload');
};
// Open copy dialog
	const handleView = (row: any) => {
		state.title = $t('lz.label.base.titleView');
		state.operateType = 'view';

		detailTrafficPlanRef.value?.openDialog({row, step: 1 });
	};
// Open add dialog
const handleAdd = () => {
	state.title = $t('lz.label.base.titleAdd', { name: $t('lz.entity.tmsTrafficPlan.trafficPlan') });
	state.operateType = 'add';
	editTrafficPlanRef.value?.openDialog({ status: 1, orderNo: 100 });
};

// Open copy dialog
const handleCopy = (row: any) => {
	state.title = $t('lz.label.base.titleCopy', { name: $t('lz.entity.tmsTrafficPlan.trafficPlan') });
	state.operateType = 'copy';
	editTrafficPlanRef.value?.openDialog(row);
};

// Open edit dialog
const handleEdit = (row: any) => {
	state.title = $t('lz.label.base.titleEdit', { name: $t('lz.entity.tmsTrafficPlan.trafficPlan') });
	state.operateType = 'edit';
	editTrafficPlanRef.value?.openDialog(row);
};


// Delete
const handleDelete = (row: any) => {
	ElMessageBox.confirm($t('lz.message.base.confirmDelete', { name: row.name }), $t('lz.message.base.warning'), {
		confirmButtonText: $t('lz.button.base.confirm'),
		cancelButtonText: $t('lz.button.base.cancel'),
		type: 'warning',
	})
		.then(async () => {
			await getAPI(TmsTrafficPlanApi).apiTmsTmstrafficplanDeletePost({ id: row.id });
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
			await getAPI(TmsTrafficPlanApi).apiTmsTmstrafficplanBatchdeletePost(selectRecords.filter(l => l.id));
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
const gridEvents: VxeGridListeners<PageTmsTrafficPlanOutput> = {
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
