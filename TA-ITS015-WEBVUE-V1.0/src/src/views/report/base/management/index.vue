<template>
	<div class="rpt-management-container">
		<el-card shadow="hover" :body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', height: '100%', alignItems: 'start' }">
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" label-position="left" :inlineMessage="true" label-width="auto" style="flex: 1 1 0%">
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.name')" prop="title" label-position="left">
							<el-input v-model="state.queryParams.title" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.name')})" clearable @keyup.enter.native="handleQuery" />
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.code')" prop="code" label-position="left">
							<el-input v-model="state.queryParams.code" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.code')})" clearable @keyup.enter.native="handleQuery" />
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.report.type')" prop="type" label-position="left">
							<el-select v-model="state.queryParams.type" filterable clearable class="w100" :placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.entity.report.type')})">
								<el-option v-for="item in getConfigDataByCode(BaseConfigTypeEnum.RptReportType)" :key="item.code" :label="item.name" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>                
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.status')" prop="status" >
							<el-select v-model="state.queryParams.status" filterable clearable class="w100" @clear="state.queryParams.status = undefined" :placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.entity.base.status')})" >
								<el-option v-for="item in getConfigDataByCode(BaseConfigTypeEnum.SysStatusType)" :key="item.code" :label="item.name" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
				</el-row>

			</el-form>

			<el-divider style="height: calc(100% - 5px); margin: 0 10px" direction="vertical" />

			<el-row>
				<SearchButton :auth-search="'rptManagement:list'" @search="handleQuery" @reset="resetQuery" :loading="options.loading" />
			</el-row>
		</el-card>

		<el-card class="full-table" shadow="hover" style="margin-top: 5px">
			<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents" @checkbox-change="selectChangeEvent" @checkbox-all="selectChangeEvent">
				<template #toolbar_buttons>
					<strong>{{ $t('lz.ui.label.rowTotal', {total: state.countData}) }}</strong>
					<el-button-group style="padding-left: 12px">
						<el-button type="primary" icon="ele-Expand" @click="handleExpand"> {{ $t('lz.button.base.expandAll') }} </el-button>
						<el-button type="primary" icon="ele-Fold" @click="handleFold"> {{ $t('lz.button.base.collapseAll') }} </el-button>
					</el-button-group>
				</template>
				<template #toolbar_tools>
					<TableHeader @add="handleAdd" :show-delete="state.showBatchDelete" :auth-add="'rptManagement:add'"
						:auth-delete="'rptManagement:delete'" @refresh="handleQuery" @custom-column="handleCustom"
						@delete="handleBatchDelete">
					</TableHeader>
				</template>
				<template #empty>
					<el-empty :image-size="200" />
				</template>
				<template #row_type="{ row }">
					 {{ getConfigItemByCode(BaseConfigTypeEnum.RptReportType, row.type)?.name }}
				</template>
				<template #row_status="{ row }">
					<TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.SysStatusType, row.status)" />
				</template>
				<template #row_record="{ row }">
					<ModifyRecord :data="row" />
				</template>
				<template #row_lastUpdate="{ row }">
					{{ row.lastUpdate ? useDateFormat(row.lastUpdate,DATE_TIME_FORMAT)?.value  : '' }}
				</template>
				<template #row_buttons="{ row }">
					<el-tooltip :content="$t('lz.button.base.copy')" placement="top" :auto-close="1000">
						<el-button icon="ele-CopyDocument" text type="warning" v-auth="'rptManagement:copy'"
							@click="handleCopy(row)"> </el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.edit')" placement="top" :auto-close="1000">
						<el-button icon="ele-Edit" text type="primary" v-auth="'rptManagement:update'"
							@click="handleEdit(row)"> </el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.delete')" placement="top" :auto-close="1000">
						<el-button icon="ele-Delete" text type="danger" v-auth="'rptManagement:delete'"
							@click="handleDelete(row)"> </el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.report.importAgain')" placement="top">
						<el-button icon="ele-UploadFilled" text type="primary" v-auth="'rptManagement:import'"
							@click="handleReImport(row)"> </el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.download')" placement="top">
						<el-button icon="ele-Download" text type="primary" v-auth="'rptManagement:download'"
							@click="handleDownload(row)"> </el-button>
					</el-tooltip>
				</template>
			</vxe-grid>
		</el-card>

		<EditManagement ref="editManagementRef" :title="state.title" :menuData="state.menuData" :operateType="state.operateType" @handleQuery="handleQuery" />
	</div>
</template>

<script lang="ts" setup name="rptManagement">
import { reactive, ref,defineAsyncComponent,onMounted } from 'vue';
import { ElMessageBox, ElMessage } from 'element-plus';
import { VxeGridInstance, VxeGridListeners } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import EditManagement from '/@/views/report/base/management/component/editManagement.vue';
import ModifyRecord from '/@/components/table/modifyRecord.vue';

import { getAPI } from '/@/utils/axios-utils';
import { RptManagementApi } from '/@/api-services/report';
import { ManagementOutput } from '/@/api-services/report';
import { OperateType } from '/@/types/constants';
import { useDateFormat } from '@vueuse/core';
import { DATE_TIME_FORMAT } from '/@/types/formats';
import { BaseConfigTypeEnum } from '/@/types/enums/baseEnum';
import { useBaseConfig } from '/@/stores/baseConfig';
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
const getConfigItemByCode = useBaseConfig().getConfigItemByCode;

import { useI18n } from 'vue-i18n';
const { t: $t } = useI18n();

const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));
const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));
const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));

const xGrid = ref<VxeGridInstance>();
const editManagementRef = ref<InstanceType<typeof EditManagement>>();
const state = reactive({
	queryParams: {
		title: undefined,
		type: undefined,
		code: undefined,
		status: undefined,
	},
	title: '',
	operateType: 'add' as OperateType,
	showBatchDelete: false,
	countData: 0,
});

// Table parameter configuration
const options = useVxeTable<ManagementOutput>(
	{
		id: 'rptManagement',
		name: 'Report Management Information',
		columns: [
			{ type: 'checkbox', width: 40, fixed: 'left' },
			{ field: 'code', title: $t('lz.entity.base.code'), minWidth: 150, sortable: true, showOverflow: 'tooltip', treeNode: true, align: 'left' },
			{ field: 'name', title: $t('lz.entity.base.name'), minWidth: 180, sortable: true, showOverflow: 'tooltip', treeNode: true, align: 'left' },
			{ field: 'type', title: $t('lz.entity.base.type'), minWidth: 80, sortable: true, showOverflow: 'tooltip', slots: { default: 'row_type' } },
			{ field: 'templateId', title: $t('lz.entity.report.templateId'), minWidth: 100, showOverflow: 'tooltip', align: 'left' },
			{ field: 'url', title: $t('lz.entity.base.url'), minWidth: 230, showOverflow: 'tooltip',align: 'left' },
			{ field: 'version', title: $t('lz.entity.base.version'), minWidth: 50, showOverflow: 'tooltip', align: 'left' },
			{ field: 'lastUpdate', title: $t('lz.entity.base.lastUpdate'), minWidth: 150, sortable: true, showOverflow: 'tooltip', align: 'right', slots: { default: 'row_lastUpdate' } },
			{ field: 'status', title: $t('lz.entity.base.status'), width: 110, sortable: true, showOverflow: 'tooltip', fixed: 'right', slots: { default: 'row_status' } },
			{ field: '', title: $t('lz.label.base.audit'), width: 80, showOverflow: 'tooltip', slots: { default: 'row_record' } },
			{ title: $t('lz.label.base.action'), fixed: 'right', width: 150, showOverflow: true, slots: { default: 'row_buttons' } },
		],
	},
	// vxeGrid configuration parameters (can override any parameters), refer to vxe-table official documentation
	{
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
		treeConfig: { expandAll: false },
	}
);

// Page initialization
onMounted(async () => {
});

// Query API
const handleQueryApi = async () => {
	const params = Object.assign(state.queryParams);
	let rs =  await getAPI(RptManagementApi).apiReportRptmanagementListGet(params.title, params.code, params.type);
	state.countData =  rs.data.result?.length as number
	return rs
};

// Query operation
const handleQuery = async () => {
	// Call vxe-grid's commitProxy(query) method to trigger the table to reload data
	options.loading = true;
	await xGrid.value?.commitProxy('reload') ;
	options.loading = false;
};

// Reset operation
const resetQuery = async () => {
	state.queryParams.title = undefined;
	state.queryParams.type = undefined;
	state.queryParams.code = undefined;
	state.queryParams.status = undefined;
	// Call vxe-grid's commitProxy(reload) method to trigger the table to reload data
	await xGrid.value?.commitProxy('reload');
};

// Open add page
const handleAdd = () => {
	state.title = $t('lz.label.base.titleAdd',{name: $t('lz.label.report.management')});
	state.operateType = 'add';
	editManagementRef.value?.openDialog({status: 1, orderNo: 100 });
};

// Open copy dialog
const handleCopy = (row: any) => {
	state.title = $t('lz.label.base.titleCopy',{name: $t('lz.label.report.management')});
	state.operateType = 'copy';
	editManagementRef.value?.openDialog(row);
};

// Open edit page
const handleEdit = (row: any) => {
	state.title = $t('lz.label.base.titleEdit',{name: $t('lz.label.report.management')});
	state.operateType = 'edit';
	editManagementRef.value?.openDialog(row);
};

// Delete current row
const handleDelete = (row: any) => {
	ElMessageBox.confirm($t('lz.message.base.confirmDelete', { name: row.name }), $t('lz.message.base.warning'), {
		confirmButtonText: $t('lz.button.base.confirm'),
		cancelButtonText: $t('lz.button.base.cancel'),
		type: 'warning',
	})
		.then(async () => {
			await getAPI(RptManagementApi).apiReportRptmanagementDeletePost({ id: row.id });
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
			await getAPI(RptManagementApi).apiReportRptmanagementBatchdeletePost(selectRecords.filter(l => l.id));
			handleQuery();
			ElMessage.success($t('lz.message.base.deleteSuccess'));

		})
		.catch(() => { });
}

const handleReImport = (row: any) => {
	ElMessageBox.confirm($t('lz.message.report.confirmReImport', { name: row.name }), $t('lz.message.base.warning'), {
		confirmButtonText: $t('lz.button.base.confirm'),
		cancelButtonText: $t('lz.button.base.cancel'),
		type: 'warning',
	})
		.then(async () => {
			await getAPI(RptManagementApi).apiReportRptmanagementImporttemplatePost({id: row.id});
			ElMessage.success($t('lz.message.base.actionSuccess'));
			await handleQuery();
		})
		.catch(() => {});
};

const handleDownload = async (row: any) => {
    try {
        const response = await getAPI(RptManagementApi).apiReportRptmanagementDownloadtemplatePost(
            { id: row.id },
            { responseType: 'blob' }
        );
        
        if (response && response.data) {
            const blob = new Blob([response.data], { type: response.headers['content-type'] });
            const url = window.URL.createObjectURL(blob);
            
            // Extract filename from row.url
            let fileName = 'download.xlsx';
            if (row.url) {
                const urlParts = row.url.split('/');
                fileName = urlParts[urlParts.length - 1];
                // Remove any query parameters if present
                fileName = fileName.split('?')[0];
            }

            const link = document.createElement('a');
            link.href = url;
            link.download = fileName;
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
            window.URL.revokeObjectURL(url);
        } else {
            ElMessage.error($t('lz.message.base.failedToDownloadFile'));
        }
    } catch (error) {
        console.error('Error downloading template:', error);
        ElMessage.error($t('lz.message.report.failedToDownloadTemplate'));
    }
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
const gridEvents: VxeGridListeners<ManagementOutput> = {
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



</script>




