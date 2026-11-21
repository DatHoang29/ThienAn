<template>
	<div class="samp-category-container">
		<el-card shadow="hover"
			:body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', height: '100%', alignItems: 'start' }">
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true"
				label-width="auto" style="flex: 1 1 0%">
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.name')" prop="name">
							<el-input v-model="state.queryParams.name"
								:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.name') })"
								clearable @keyup.enter.native="handleQuery" />
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.sample.description')" prop="description">
							<el-input v-model="state.queryParams.description"
								:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.sample.description') })"
								clearable @keyup.enter.native="handleQuery" />
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>

			<el-divider style="height: calc(100% - 5px); margin: 0 10px" direction="vertical" />

			<el-row>
				<SearchButton :auth-search="'sampCategory:page'" @search="handleQuery" @reset="resetQuery"
					:loading="options.loading" />
			</el-row>
		</el-card>

		<el-card class="full-table" shadow="hover" style="margin-top: 5px">
			<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents"
				@checkbox-change="selectChangeEvent" @checkbox-all="selectChangeEvent">
				<template #toolbar_buttons>
					<!-- <el-button type="primary" icon="ele-Plus" @click="handleAdd" v-auth="'sysMenu:add'"> Add </el-button> -->
					<el-button type="primary" icon="ele-Chart" @click="handleChart"> {{
						$t('lz.button.base.chart') }} </el-button>
					<el-button type="primary" icon="ele-Chart" @click="handleChartLibrary"> {{
						$t('lz.button.base.chart') }} (Thư viện)
					</el-button>


				</template>
				<template #toolbar_tools>
					<TableHeader @add="handleAdd" :show-delete="state.showBatchDelete" :auth-add="'sampCategory:add'"
						:auth-delete="'sampCategory:delete'" @refresh="handleQuery" @custom-column="handleCustom"
						@delete="handleBatchDelete">
						<template #prefix>
							<el-button type="" size="small" @click="handleExport" class="mr3" v-auth="'sampCategory:export'"><i
									class="fa fa-file-excel-o"></i>{{
										$t('lz.button.base.exportExcel') }}</el-button>
						</template>
					</TableHeader>
				</template>
				<template #empty>
					<el-empty :image-size="200" />
				</template>
				<template #row_status="{ row }">
					<el-tag v-if="row.status === 1" type="success">{{ $t('lz.param.status_type.1') }}</el-tag>
					<el-tag v-else type="danger">{{ $t('lz.param.status_type.0') }}</el-tag>
				</template>
				<template #row_record="{ row }">
					<ModifyRecord :data="row" />
				</template>
				<template #row_buttons="{ row }">
					<el-tooltip :content="$t('lz.button.base.copy')" placement="top" :auto-close="1000">
						<el-button icon="ele-CopyDocument" text type="warning" v-auth="'sampCategory:add'"
							@click="handleCopy(row)"> </el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.label.base.edit')" placement="top">
						<el-button icon="ele-Edit" text type="primary" v-auth="'sampCategory:update'"
							@click="handleEdit(row)"> </el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.delete')" placement="top" :auto-close="1000">
						<el-button icon="ele-Delete" text type="danger" v-auth="'sampCategory:delete'"
							@click="handleDelete(row)"> </el-button>
					</el-tooltip>

				</template>
			</vxe-grid>
		</el-card>

		<EditCategory ref="editCategoryRef" :title="state.title" :operateType="state.operateType"
			@handleQuery="handleQuery" />
		<ChartLibraryCategory ref="chartLibraryRef" :title="state.title" />
		<ChartCategory ref="chartRef" :title="state.title" />



	</div>
</template>


<script lang="ts" setup name="sampCategory">
import { onMounted, reactive, ref, defineAsyncComponent } from 'vue';
import { ElMessageBox, ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import EditCategory from '/@/views/sample/category/component/editCategory.vue';
import ChartLibraryCategory from './component/chartLibrary.vue';
import ChartCategory from './component/chart.vue';


import ModifyRecord from '/@/components/table/modifyRecord.vue';
import { getAPI } from '/@/utils/axios-utils';
import { SampCategoryApi } from '/@/api-services/sample';
import { PageCategoryInput, PageCategoryOutput } from '/@/api-services/sample';
import { OperateType } from '/@/types/constants/operate';
import { saveAs } from 'file-saver';
const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));
const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));

const { t: $t } = useI18n();
const xGrid = ref<VxeGridInstance>();
const editCategoryRef = ref<InstanceType<typeof EditCategory>>();

const chartRef = ref<InstanceType<typeof ChartCategory>>();
const chartLibraryRef = ref<InstanceType<typeof ChartLibraryCategory>>();

const state = reactive({
	queryParams: {
		name: undefined,

		description: undefined,
	},
	localPageParam: {
		pageSize: 50 as number,
		defaultSort: { field: 'name', order: 'asc', descStr: 'asc' } as any,
	},
	visible: false,
	title: '',
	operateType: 'add' as OperateType,
	showBatchDelete: false,
});

// Table parameter configuration
const options = useVxeTable<PageCategoryOutput>(
	{
		id: 'sampCategory',
		name: 'Category',
		columns: [
			{ type: 'checkbox', fixed: 'left', width: 45 },
			{ field: 'name', title: $t('lz.entity.base.name'), minWidth: 200, showOverflow: 'tooltip', sortable: true, },
			{ field: 'description', title: $t('lz.entity.sample.description'), minWidth: 200, showOverflow: 'tooltip', sortable: true },
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

// Page initialization
onMounted(async () => {
	state.localPageParam = state.localPageParam;
	await handleQuery();
});

// Query API
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
	const params = Object.assign(state.queryParams, { page: page.currentPage, pageSize: page.pageSize, field: sort.field, order: sort.order, descStr: 'desc' }) as PageCategoryInput;
	return await getAPI(SampCategoryApi).apiSampleSampcategoryPagePost(params);
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
	state.queryParams.description = undefined;
	state.queryParams.name = undefined;

	await xGrid.value?.commitProxy('reload');
};

// Open add dialog
const handleAdd = () => {
	state.title = $t('lz.label.base.titleAdd', { name: $t('lz.label.sample.category') });
	state.operateType = 'add';
	editCategoryRef.value?.openDialog({ status: 1, orderNo: 100 });
};

// Open copy dialog
const handleCopy = (row: any) => {
	state.title = $t('lz.label.base.titleCopy', { name: $t('lz.label.sample.category') });
	state.operateType = 'copy';
	editCategoryRef.value?.openDialog(row);
};

// Open edit dialog
const handleEdit = (row: any) => {
	state.title = $t('lz.label.base.titleEdit', { name: $t('lz.label.sample.category') });
	state.operateType = 'edit';
	editCategoryRef.value?.openDialog(row);
};

const handleChart = () => {
	state.title = $t('lz.label.base.titleChart', { name: $t('lz.label.sample.category') });
	chartRef.value?.openDialog();
}


const handleExport = async () => {
	//Lưu ý có responseType: 'blob'
	const rs = await getAPI(SampCategoryApi).apiSampleSampcategoryExportPost(state.queryParams, { responseType: 'blob' });
	//Dat ten cho file excel tra ve
    const fileName = "Category.xlsx";
	//Sử dụng saveAs của file-saver để lưu file
	saveAs(new Blob([rs.data as any], { type: rs.headers['content-type'] }), fileName);

}

const handleChartLibrary = () => {
	state.title = $t('lz.label.base.titleChart', { name: $t('lz.label.sample.category') });
	chartLibraryRef.value?.openDialog();
}


// Delete
const handleDelete = (row: any) => {

	ElMessageBox.confirm($t('lz.message.base.confirmDelete', { name: row.name }), $t('lz.message.base.warning'), {
		confirmButtonText: $t('lz.button.base.confirm'),
		cancelButtonText: $t('lz.button.base.cancel'),
		type: 'warning',
	})
		.then(async () => {
			await getAPI(SampCategoryApi).apiSampleSampcategoryDeletePost({ id: row.id });
			ElMessage.success($t('lz.message.base.deleteSuccess'));
			await handleQuery();

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
			await getAPI(SampCategoryApi).apiSampleSampcategoryBatchdeletePost(selectRecords.filter(l => l.id));
			ElMessage.success($t('lz.message.base.deleteSuccess'));
			await handleQuery();


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
const gridEvents: VxeGridListeners<PageCategoryOutput> = {
	// Only valid when pager-config is configured, this event will be triggered when the page changes
	async pageChange({ pageSize }) {
		state.localPageParam.pageSize = pageSize;
	},
	// This event will be triggered when the sorting condition changes
	async sortChange({ field, order }) {
		state.localPageParam.defaultSort = { field: field, order: order!, descStr: 'desc' };
	},
};
</script>
