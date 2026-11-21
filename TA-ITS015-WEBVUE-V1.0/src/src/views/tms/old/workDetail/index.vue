<template>
	<div class="tms-work-detail-container">
		<el-card shadow="hover"
			:body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', height: '100%', alignItems: 'start' }">
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true" label-position="left"	
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
							<el-input v-model="state.queryParams.name" 
								:placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.name')})"
								clearable
								@keyup.enter.native="handleQuery" />
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.tmsWorkDetail.workType')" prop="workType">
							<el-select v-model="state.queryParams.workType" 
								:placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.entity.tmsWorkDetail.workType')})"
								filterable clearable class="w100"
								@clear="state.queryParams.workType = undefined">
								<el-option v-for="item in state.workTypeList" :key="item.code" :label="item.name"
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
				<SearchButton :auth-search="'tmsWorkDetail:page'" @search="handleQuery" @reset="resetQuery" :loading="options.loading" />					
			
			</el-row>
		</el-card>

		<el-card class="full-table" shadow="hover" style="margin-top: 5px">
			<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents"
				@checkbox-change="selectChangeEvent" @checkbox-all="selectChangeEvent">

				<template #toolbar_tools>
					<TableHeader @add="handleAdd" :show-delete="state.showBatchDelete" :auth-add="'tmsWorkDetail:add'"
						:auth-delete="'tmsWorkDetail:delete'" @refresh="handleQuery" @custom-column="handleCustom"
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
				<template #row_state="{ row }">
					<el-tag v-if="row.equipmentState === 1" type="success">{{ $t('lz.label.base.active') }}</el-tag>
					<el-tag v-else type="danger">{{ $t('lz.label.base.inactive') }}</el-tag>
				</template>
				<template #row_controlType="{row}">
				<span>{{ state.controlEquipTypeList.find((item: any) => item.code === row.controlType)?.name }}</span>
			</template>
				<template #row_record="{ row }">
					<ModifyRecord :data="row" />
				</template>
				<template #row_buttons="{ row }">
					<el-tooltip :content="$t('lz.button.base.copy')" placement="top" :auto-close="1000">
						<el-button icon="ele-CopyDocument" text type="warning" v-auth="'tmsWorkDetail:add'"
							@click="handleCopy(row)"> </el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.edit')" placement="top" :auto-close="1000">
						<el-button icon="ele-Edit" text type="primary" v-auth="'tmsWorkDetail:update'"
							@click="handleEdit(row)"> </el-button>
					</el-tooltip>
					<el-tooltip :content="row.isInUse ? $t('lz.message.tmsWorkDetail.workDetailInUse') : $t('lz.button.base.delete')" placement="top" :auto-close="1000">
						<el-button icon="ele-Delete" text type="danger" v-auth="'tmsWorkDetail:delete'"
							:disabled="row.isInUse"
							@click="handleDelete(row)"> </el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.view')" placement="top">
						<el-button icon="ele-View" text type="primary" v-auth="'tmsWorkDetail:view'"
							@click="handleView(row)"> </el-button>
					</el-tooltip>

				</template>
				<!-- <template #header_name>
					{{ $t('common.ui.pos.name') }}
				</template> -->
			</vxe-grid>
		</el-card>

		<DetailWorkDetail ref="detailWorkDetailRef" :title="state.title" :operateType="state.operateType" :control-equip-type-list="state.controlEquipTypeList"
			@handleQuery="handleQuery" />
		<EditWorkDetail ref="editWorkDetailRef" :title="state.title" :operateType="state.operateType"
			:workTypeList="state.workTypeList" :workModeList="state.workModeList" @handleQuery="handleQuery" />
	</div>
</template>

<script lang="ts" setup name="tmsWorkDetail">
import { onMounted, reactive, ref, defineAsyncComponent } from 'vue';
import { ElMessageBox, ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { Local } from '/@/utils/storage';
import EditWorkDetail from './component/editWorkDetail.vue';
import DetailWorkDetail from './component/detailWorkDetail.vue';
import ModifyRecord from '/@/components/table/modifyRecord.vue';
import { getAPI } from '/@/utils/axios-utils';
import { SysConfigDataApi, } from '/@/api-services/cfgSystem';
import { TmsWorkDetailApi } from '/@/api-services/tms';
import { PageWorkDetailInput, PageWorkDetailOutput } from '/@/api-services/tms';
import { OperateType } from '/@/types/constants/operate';
import { useBaseConfig } from '/@/stores/baseConfig';
import { BaseConfigTypeEnum } from '/@/types/enums';
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));
const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));

const { t: $t } = useI18n();
const xGrid = ref<VxeGridInstance>();
const editWorkDetailRef = ref<InstanceType<typeof EditWorkDetail>>();
const detailWorkDetailRef = ref<InstanceType<typeof DetailWorkDetail>>();

const state = reactive({
	queryParams: {
		code: undefined,
		name: undefined,
		workType: undefined,
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
	workTypeList: [] as any,
	workModeList: [] as any,
	controlEquipTypeList: [] as any,
});
// Local storage parameters 
const localPageParamKey = 'localPageParam:tmsWorkDetail';
// Table parameter configuration
const options = useVxeTable<PageWorkDetailOutput>(
	{
		id: 'tmsWorkDetail',
		name: 'tmsWorkDetail',
		columns: [
			{ type: 'checkbox', fixed: 'left', width: 45 },
			{ field: 'code', title: $t('lz.entity.base.code'), minWidth: 150, showOverflow: 'tooltip', sortable: true },
			{ field: 'name', title: $t('lz.entity.base.name'), minWidth: 200, showOverflow: 'tooltip', sortable: true, },
			{ field: 'workMode', title: $t('lz.entity.tmsWorkDetail.workMode'), minWidth: 80, showOverflow: 'tooltip', sortable: true, formatter: ({ cellValue }: any) => state.workModeList.find((u: any) => u.code == cellValue)?.name },
			{ field: 'workType', title: $t('lz.entity.tmsWorkDetail.workType'), minWidth: 100, showOverflow: 'tooltip', sortable: true, formatter: ({ cellValue }: any) => state.workTypeList.find((u: any) => u.code == cellValue)?.name },
			{ field: 'controlType', title: $t('lz.entity.tms.controlType'), align: 'center', headerAlign: 'center', minWidth: 80, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_controlType' }},
			{ field: 'status', title: $t('lz.entity.base.status'), width: 150, showOverflow: 'tooltip',sortable: true, slots: { default: 'row_status' } },
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
		// Checkbox configuration: Không cho tick chọn dòng công việc đang được sử dụng
		checkboxConfig: { checkMethod: ({ row }: any) => !row.isInUse },
	}
);

// Load workMode values from local storage on mounted

// Page initialization
onMounted(async () => {
	state.localPageParam = Local.get(localPageParamKey) || state.localPageParam;

	let resDicData = await getAPI(SysConfigDataApi).apiCfgsystemSysconfigdataDatalistCodeGet("tms_work_type");
	state.workTypeList = resDicData.data.result;
	resDicData = await getAPI(SysConfigDataApi).apiCfgsystemSysconfigdataDatalistCodeGet("tms_work_mode");
	state.workModeList = resDicData.data.result;
	const controlEquipType = await getAPI(SysConfigDataApi).apiCfgsystemSysconfigdataDatalistCodeGet('tms_control_device_type');
  	state.controlEquipTypeList = controlEquipType.data.result;
	await handleQuery();

});

const changeMode = async (row: any) => {
	try {
		await getAPI(TmsWorkDetailApi).apiTmsTmsworkdetailUpdatePost(row);
		ElMessage.success($t('lz.message.base.updateSuccess'));
	} catch (error) {
		console.error('Error updating status:', error);
		row.workMode = row.workMode == '1' ? '0' : '1';
		ElMessage.success($t('lz.message.base.updateError'));
	}
};

const changeStatusAuto = async (row: any) => {
	try {
		await getAPI(TmsWorkDetailApi).apiTmsTmsworkdetailUpdatePost(row);
		ElMessage.success($t('lz.message.base.updateSuccess'));
		row.workMode = row.workMode == '2' ? '0' : '2';
	} catch (error) {
		// Log the error for debugging
		console.error('Error updating auto status:', error);
		row.workMode = row.workMode == '2' ? '0' : '2';
		ElMessage.success($t('lz.message.base.updateError'));
	}
};

// Query API
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
	const params = Object.assign(state.queryParams, { page: page.currentPage, pageSize: page.pageSize, field: sort.field, order: sort.order, descStr: 'desc' }) as PageWorkDetailInput;
	return await getAPI(TmsWorkDetailApi).apiTmsTmsworkdetailPagePost(params);
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
	state.queryParams.workType = undefined;
	state.queryParams.status = undefined;
	await xGrid.value?.commitProxy('reload');
};
// Open copy dialog
const handleView = (row: any) => {
	state.title = $t('lz.label.base.titleView');
	state.operateType = 'view';
	detailWorkDetailRef.value?.openDialog(row);
};
// Open add dialog
const handleAdd = () => {
	state.title = $t('lz.label.base.titleAdd', {name: $t('lz.entity.tmsWorkDetail.workDetail')});
	state.operateType = 'add';
	editWorkDetailRef.value?.openDialog({ status: 1, orderNo: 100 });
};

// Open copy dialog
const handleCopy = (row: any) => {
	state.title = $t('lz.label.base.titleCopy', { name: $t('lz.entity.tmsWorkDetail.workDetail') });
	state.operateType = 'copy';
	editWorkDetailRef.value?.openDialog(row);
};

// Open edit dialog
const handleEdit = (row: any) => {
	state.title = $t('lz.label.base.titleEdit', { name: $t('lz.entity.tmsWorkDetail.workDetail') });
	state.operateType = 'edit';
	editWorkDetailRef.value?.openDialog(row);
};


// Delete
const handleDelete = (row: any) => {
	if (row.isInUse) {
		ElMessage.warning($t('lz.message.tmsWorkDetail.workDetailInUse'));
		return;
	}
	ElMessageBox.confirm($t('lz.message.base.confirmDelete', { name: row.name }), $t('lz.message.base.warning'), {
		confirmButtonText: $t('lz.button.base.confirm'),
		cancelButtonText: $t('lz.button.base.cancel'),
		type: 'warning',
	})
		.then(async () => {
			await getAPI(TmsWorkDetailApi).apiTmsTmsworkdetailDeletePost({ id: row.id });
			handleQuery();
			ElMessage.success($t('lz.message.base.deleteSuccess'));
		})
		.catch(() => { });
};

//Batch delete
const handleBatchDelete = async () => {
	const selectRecords = getCheckedRecords();
	if (selectRecords.some((l: any) => l.isInUse)) {
		ElMessage.warning($t('lz.message.tmsWorkDetail.workDetailInUse'));
		return;
	}
	ElMessageBox.confirm($t('lz.message.base.confirmBatchDelete', { total: selectRecords.length }), $t('lz.message.base.warning'), {
		confirmButtonText: $t('lz.button.base.confirm'),
		cancelButtonText: $t('lz.button.base.cancel'),
		type: 'warning',
	})
		.then(async () => {
			await getAPI(TmsWorkDetailApi).apiTmsTmsworkdetailBatchdeletePost(selectRecords.filter(l => l.id));
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
const gridEvents: VxeGridListeners<PageWorkDetailOutput> = {
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
