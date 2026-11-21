<template>
	<div class="rpt-infoconfig-container">
		<!-- SearchBar: begin -->
		<el-card shadow="hover" :body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', height: '100%', alignItems: 'start' }">
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" label-position="left" :inlineMessage="true" label-width="auto" style="flex: 1 1 0%">
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.rptInfoConfig.reportCode')" prop="reportCode">
							<el-select
								v-model="state.queryParams.reportCode"
								filterable
								clearable
								class="w100"
								@clear="state.queryParams.reportCode = undefined"
								:placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.rptInfoConfig.reportCode') })"
							>
								<el-option v-for="item in state.reportList" :key="item.code" :label="item.label" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.rptInfoConfig.groupCode')" prop="groupCode">
							<el-select
								v-model="state.queryParams.groupCode"
								filterable
								clearable
								class="w100"
								@clear="state.queryParams.groupCode = undefined"
								:placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.rptInfoConfig.groupCode') })"
							>
								<el-option v-for="item in state.groupCodeList" :key="item.code" :label="item.name" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.code')" prop="code">
							<el-input
								v-model="state.queryParams.code"
								@keyup.enter.native="handleQuery"
								:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.code') })"
								:maxlength="EntityFieldLength.Length128"
								show-word-limit
								clearable
							/>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.rptInfoConfig.value')" prop="value">
							<el-input
								v-model="state.queryParams.value"
								@keyup.enter.native="handleQuery"
								:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.rptInfoConfig.value') })"
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
				<SearchButton :auth-search="'rptInfoConfig:list'" @search="handleQuery" @reset="resetQuery" :loading="options.loading" />
			</el-row>
		</el-card>
		<!-- SearchBar: end -->

		<!-- GridTable: begin -->
		<el-card class="full-table" shadow="hover" style="margin-top: 5px">
			<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents" @checkbox-change="selectChangeEvent" @checkbox-all="selectChangeEvent">
				<template #toolbar_tools>
					<TableHeader
						@add="handleAdd"
						:show-delete="state.showBatchDelete"
						:auth-add="'rptInfoConfig:add'"
						:auth-delete="'rptInfoConfig:delete'"
						@refresh="handleQuery"
						@custom-column="handleCustom"
						@delete="handleBatchDelete"
					>
					</TableHeader>
				</template>
				<template #empty>
					<el-empty :image-size="200" />
				</template>
				<template #row_reportCode="{ row }">
					<el-tag v-if="isGlobalConfig(row)" type="info" disable-transitions>{{ $t('lz.label.rptInfoConfig.globalReport') }}</el-tag>
					<span v-else>{{ row.reportCode }}</span>
				</template>
				<template #row_groupCode="{ row }">
					<TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.RptInfoConfigType, row.groupCode)" />
				</template>
				<template #row_value="{ row }">
					<el-image
						v-if="row.valueType === RptInfoValueTypeEnum.Image && row.value"
						:src="row.value"
						:preview-src-list="[row.value]"
						preview-teleported
						fit="contain"
						style="height: 24px"
					/>
					<span v-else>{{ row.value }}</span>
				</template>
				<template #row_status="{ row }">
					<TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.SysStatusType, row.status)" />
				</template>
				<template #row_record="{ row }">
					<ModifyRecord :data="row" />
				</template>
				<template #row_buttons="{ row }">
					<el-tooltip :content="$t('lz.button.base.copy')" placement="top" :auto-close="1000">
						<el-button icon="ele-CopyDocument" text type="warning" v-auth="'rptInfoConfig:add'" @click="handleCopy(row)"> </el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.edit')" placement="top" :auto-close="1000">
						<el-button icon="ele-Edit" text type="primary" v-auth="'rptInfoConfig:update'" @click="handleEdit(row)"> </el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.delete')" placement="top" :auto-close="1000">
						<el-button icon="ele-Delete" text type="danger" v-auth="'rptInfoConfig:delete'" @click="handleDelete(row)"> </el-button>
					</el-tooltip>
				</template>
			</vxe-grid>
		</el-card>
		<!-- GridTable: end -->

		<!-- Primary components: Begin -->
		<EditInfoConfig ref="editInfoConfigRef" :title="state.title" :operateType="state.operateType" :reportList="state.reportList" @handleQuery="handleQuery" />
		<!-- Primary components: End -->
	</div>
</template>

<script lang="ts" setup name="rptInfoConfig">
import { onMounted, reactive, ref, defineAsyncComponent } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { getAPI } from '/@/utils/axios-utils';
import { OperateType } from '/@/types/constants/operate';
import ModifyRecord from '/@/components/table/modifyRecord.vue';

import { RptInfoConfigApi, RptManagementApi } from '/@/api-services/report';
import { PageRptInfoConfigInput, PageRptInfoConfigOutput } from '/@/api-services/report';

import { BaseConfigTypeEnum, EntityFieldLength, RptInfoConfigEnum, RptInfoValueTypeEnum } from '/@/types/enums/baseEnum';
import { useBaseConfig } from '/@/stores/baseConfig';

import EditInfoConfig from '/@/views/report/base/infoConfig/component/editInfoConfig.vue';

const { t: $t } = useI18n();
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
const getConfigItemByCode = useBaseConfig().getConfigItemByCode;

const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));
const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));
const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));

const xGrid = ref<VxeGridInstance>();
const editInfoConfigRef = ref<InstanceType<typeof EditInfoConfig>>();

const state = reactive({
	title: '',
	queryParams: {
		reportCode: undefined,
		groupCode: undefined,
		code: undefined,
		value: undefined,
	} as any,
	localPageParam: {
		pageSize: 50 as number,
		defaultSort: { field: 'orderNo', order: 'asc', descStr: 'desc' } as any,
	},
	operateType: 'add' as OperateType,
	showBatchDelete: false,
	groupCodeList: [] as any,
	reportList: [] as any,
});

// Table parameter configuration
const options = useVxeTable<PageRptInfoConfigOutput>(
	{
		id: 'rptInfoConfig',
		name: 'Report Info Config',
		columns: [
			{ type: 'checkbox', width: 40, fixed: 'left' },
			{ field: 'reportCode', title: $t('lz.entity.rptInfoConfig.reportCode'), minWidth: 160, sortable: true, showOverflow: 'tooltip', slots: { default: 'row_reportCode' } },
			{ field: 'groupCode', title: $t('lz.entity.rptInfoConfig.groupCode'), minWidth: 130, sortable: true, showOverflow: 'tooltip', slots: { default: 'row_groupCode' } },
			{ field: 'code', title: $t('lz.entity.base.code'), minWidth: 150, sortable: true, showOverflow: 'tooltip', align: 'left' },
			{ field: 'name', title: $t('lz.entity.base.name'), minWidth: 180, sortable: true, showOverflow: 'tooltip', align: 'left' },
			{ field: 'value', title: $t('lz.entity.rptInfoConfig.value'), minWidth: 260, showOverflow: 'tooltip', align: 'left', slots: { default: 'row_value' } },
			{ field: 'valueType', title: $t('lz.entity.rptInfoConfig.valueType'), width: 110, showOverflow: 'tooltip' },
			{ field: 'orderNo', title: $t('lz.entity.base.orderNo'), width: 90, sortable: true, align: 'right' },
			{ field: 'status', title: $t('lz.entity.base.status'), width: 110, fixed: 'right', showOverflow: 'tooltip', slots: { default: 'row_status' } },
			{ field: '', title: $t('lz.label.base.audit'), width: 80, showOverflow: 'tooltip', slots: { default: 'row_record' } },
			{ title: $t('lz.label.base.action'), fixed: 'right', width: 100, showOverflow: true, slots: { default: 'row_buttons' } },
		],
	},
	{
		// Multi-select configuration
		checkboxConfig: { range: false },
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
	state.groupCodeList = getConfigDataByCode(BaseConfigTypeEnum.RptInfoConfigType);
	await loadReportList();
});

// Danh sách báo cáo để chọn phạm vi áp dụng, kèm mục dùng chung
const loadReportList = async () => {
	const globalItem = { code: RptInfoConfigEnum.GlobalReportCode, label: $t('lz.label.rptInfoConfig.globalReport') };
	try {
		const res = await getAPI(RptManagementApi).apiReportRptmanagementListGet();
		const items = (res.data.result ?? []).filter((item: any) => item.code).map((item: any) => ({ code: item.code, label: `${item.code} - ${item.name}` }));

		state.reportList = [globalItem, ...items];
	} catch (error) {
		console.error('loadReportList(...)', error);
		state.reportList = [globalItem];
	}
};

// Cấu hình dùng chung khi ReportCode rỗng hoặc bằng '*'
const isGlobalConfig = (row: any) => !row.reportCode || row.reportCode === RptInfoConfigEnum.GlobalReportCode;

// Query API
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
	const params = Object.assign(state.queryParams, {
		page: page.currentPage,
		pageSize: page.pageSize,
		field: sort.field,
		order: sort.order,
		descStr: 'desc',
	}) as PageRptInfoConfigInput;

	return await getAPI(RptInfoConfigApi).apiReportRptinfoconfigPagePost(params);
};

// Query operation
const handleQuery = async () => {
	options.loading = true;
	await xGrid.value?.commitProxy('reload');
	options.loading = false;
};

// Reset operation
const resetQuery = async () => {
	state.queryParams.reportCode = undefined;
	state.queryParams.groupCode = undefined;
	state.queryParams.code = undefined;
	state.queryParams.value = undefined;
	await xGrid.value?.commitProxy('reload');
};

// Open add dialog
const handleAdd = () => {
	state.title = $t('lz.label.base.titleAdd', { name: $t('lz.label.report.infoConfig') });
	state.operateType = 'add';
	editInfoConfigRef.value?.openDialog({
		reportCode: RptInfoConfigEnum.GlobalReportCode,
		valueType: RptInfoValueTypeEnum.Text,
		status: 1,
		orderNo: 100,
		isSysConfig: false,
	});
};

// Open copy dialog
const handleCopy = (row: any) => {
	state.title = $t('lz.label.base.titleCopy', { name: $t('lz.label.report.infoConfig') });
	state.operateType = 'copy';
	editInfoConfigRef.value?.openDialog(row);
};

// Open edit dialog
const handleEdit = (row: any) => {
	state.title = $t('lz.label.base.titleEdit', { name: $t('lz.label.report.infoConfig') });
	state.operateType = 'edit';
	editInfoConfigRef.value?.openDialog(row);
};

// Delete current row
const handleDelete = (row: any) => {
	ElMessageBox.confirm($t('lz.message.base.confirmDelete', { name: row.name }), $t('lz.message.base.warning'), {
		confirmButtonText: $t('lz.button.base.confirm'),
		cancelButtonText: $t('lz.button.base.cancel'),
		type: 'warning',
	})
		.then(async () => {
			await getAPI(RptInfoConfigApi).apiReportRptinfoconfigDeletePost({ id: row.id });
			ElMessage.success($t('lz.message.base.deleteSuccess'));
			await handleQuery();
		})
		.catch(() => {});
};

// Batch delete
const handleBatchDelete = async () => {
	const selectRecords = getCheckedRecords();
	ElMessageBox.confirm($t('lz.message.base.confirmBatchDelete', { total: selectRecords.length }), $t('lz.message.base.warning'), {
		confirmButtonText: $t('lz.button.base.confirm'),
		cancelButtonText: $t('lz.button.base.cancel'),
		type: 'warning',
	})
		.then(async () => {
			await getAPI(RptInfoConfigApi).apiReportRptinfoconfigBatchdeletePost(selectRecords.filter((item) => item.id).map((item) => ({ id: item.id })));
			ElMessage.success($t('lz.message.base.deleteSuccess'));
			await handleQuery();
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

// Table events
const gridEvents: VxeGridListeners<PageRptInfoConfigOutput> = {
	// Only effective when pager-config is configured, triggered when pagination changes
	async pageChange({ pageSize }) {
		state.localPageParam.pageSize = pageSize;
	},
	// Triggered when sorting conditions change
	async sortChange({ field, order }) {
		state.localPageParam.defaultSort = { field: field, order: order!, descStr: 'desc' };
	},
};
</script>

<style lang="scss" scoped>
.rpt-infoconfig-container {
	height: 100%;
	display: flex;
	flex-direction: column;
}
.full-table {
	flex: 1;
	display: flex;
	flex-direction: column;
	overflow: hidden;
}
</style>
