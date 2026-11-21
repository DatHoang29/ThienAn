<template>
	<div class="sys-pos-container">
		<el-card shadow="hover"
			:body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', height: '100%', alignItems: 'start' }">
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true"
				label-position="left" label-width="auto" style="flex: 1 1 0%">
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.rptEmailReport.pluginCode')" prop="pluginCode">
							<el-select v-model="state.queryParams.pluginCode" filterable clearable class="w100"
								@clear="state.queryParams.pluginCode = undefined"
								:placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.rptEmailReport.pluginCode') })">
								<el-option v-for="p in state.listData.plugins" :key="p.code!" :label="p.name || p.code"
									:value="p.code!" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.rptEmailReport.code')" prop="code">
							<el-input v-model="state.queryParams.code"
								:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.rptEmailReport.code') })"
								clearable @keyup.enter.native="handleQuery" />
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.rptEmailReport.name')" prop="name">
							<el-input v-model="state.queryParams.name"
								:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.rptEmailReport.name') })"
								clearable @keyup.enter.native="handleQuery" />
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.status')" prop="status">
							<el-select v-model="state.queryParams.status" filterable clearable class="w100"
								@clear="state.queryParams.status = undefined"
								:placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.base.status') })">
								<el-option v-for="item in getConfigDataByCode(BaseConfigTypeEnum.SysStatusType)"
									:key="item.code" :label="item.name" :value="parseInt(item.code)" />
							</el-select>
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>

			<el-divider style="height: calc(100% - 5px); margin: 0 10px" direction="vertical" />

			<el-row>
				<SearchButton @search="handleQuery" @reset="resetQuery" :loading="options.loading" />
			</el-row>
		</el-card>

		<el-card class="full-table" shadow="hover" style="margin-top: 5px">
			<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" @checkbox-change="selectChangeEvent"
				@checkbox-all="selectChangeEvent">
				<template #toolbar_buttons>
				</template>
				<template #toolbar_tools>
					<TableHeader @add="handleAdd" :show-delete="state.showBatchDelete" @refresh="handleQuery"
						@custom-column="handleCustom" @delete="handleBatchDelete" />
				</template>
				<template #empty>
					<el-empty :image-size="200" />
				</template>
				<template #row_plugin="{ row }">
					<el-tag size="small" effect="plain">{{ pluginNameOf(state.listData.plugins, row.pluginCode) }}</el-tag>
				</template>
				<template #row_status="{ row }">
					<!-- Tắt = ẩn khỏi ô chọn báo cáo của form lịch; lịch đang dùng thì báo cáo bị bỏ qua lúc gửi -->
					<el-switch :model-value="row.status !== StatusTypeEnum.Disable" @change="handleToggleStatus(row)" />
				</template>
				<template #row_record="{ row }">
					<ModifyRecord :data="row" />
				</template>
				<template #row_buttons="{ row }">
					<el-tooltip :content="$t('lz.button.base.copy')" placement="top" :auto-close="1000">
						<el-button icon="ele-CopyDocument" text type="warning" @click="handleCopy(row)" />
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.edit')" placement="top" :auto-close="1000">
						<el-button icon="ele-Edit" text type="primary" @click="handleEdit(row)" />
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.delete')" placement="top" :auto-close="1000">
						<el-button icon="ele-Delete" text type="danger" @click="handleDelete(row)" />
					</el-tooltip>
				</template>
			</vxe-grid>
		</el-card>

		<EditBaoCaoGuiMail ref="editRef" :title="state.title" :operateType="state.operateType"
			:plugins="state.listData.plugins" @handleQuery="handleQuery" />
	</div>
</template>

<script lang="ts" setup name="mailBaoCaoGuiMail">
import { defineAsyncComponent, onMounted, reactive, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { ElMessage, ElMessageBox } from 'element-plus';
import { VxeGridInstance, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { getAPI } from '/@/utils/axios-utils';
import { OperateType } from '/@/types/constants/operate';
import ModifyRecord from '/@/components/table/modifyRecord.vue';

import { RptEmailReportApi } from '/@/api-services/mail';
import type { MailRptPageReportInput, MailPageReportOutput } from '/@/api-services/mail';
import type { PluginOutput } from '/@/api-services/system';

import { useBaseConfig } from '/@/stores/baseConfig';
import { BaseConfigTypeEnum, StatusTypeEnum } from '/@/types/enums';
import { useMailReportCatalog } from '/@/hooks/useMailReportCatalog';
import EditBaoCaoGuiMail from '/@/views/mail/baoCaoGuiMail/component/editBaoCaoGuiMail.vue';

const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));
const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));

const { t: $t } = useI18n();
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
const { loadMailReportPlugins, clearMailReportCache, pluginNameOf } = useMailReportCatalog();

const xGrid = ref<VxeGridInstance>();
const editRef = ref<InstanceType<typeof EditBaoCaoGuiMail>>();

const state = reactive({
	queryParams: {
		code: undefined,
		name: undefined,
		pluginCode: undefined,
		status: undefined,
	},
	title: '',
	operateType: 'add' as OperateType,
	showBatchDelete: false,
	listData: {
		plugins: [] as PluginOutput[],
	},
});

// Table parameter configuration
const options = useVxeTable<MailPageReportOutput>(
	{
		id: 'mailBaoCaoGuiMailGrid',
		name: 'RptEmailReport',
		columns: [
			{ type: 'checkbox', fixed: 'left', width: 45 },
			{ field: 'pluginCode', title: $t('lz.entity.rptEmailReport.pluginCode'), width: 170, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_plugin' } },
			{ field: 'code', title: $t('lz.entity.rptEmailReport.code'), width: 160, showOverflow: 'tooltip', sortable: true },
			{ field: 'name', title: $t('lz.entity.rptEmailReport.name'), minWidth: 220, showOverflow: 'tooltip', sortable: true, align: 'left' },
			{ field: 'detailParams', title: $t('lz.entity.rptEmailReport.detailParams'), minWidth: 220, showOverflow: 'tooltip', sortable: true, align: 'left' },
			{ field: 'orderNo', title: $t('lz.entity.base.orderNo'), width: 90, sortable: true },
			{ field: 'status', title: $t('lz.entity.base.status'), width: 90, sortable: true, slots: { default: 'row_status' } },
			{ field: '', title: $t('lz.label.base.audit'), width: 100, showOverflow: 'tooltip', slots: { default: 'row_record' } },
			{ title: $t('lz.label.base.action'), fixed: 'right', width: 120, showOverflow: true, slots: { default: 'row_buttons' } },
		],
	},
	{
		proxyConfig: { autoLoad: true, ajax: { query: ({ page, sort }) => handleQueryApi(page, sort) } },
		toolbarConfig: { export: false },
	}
);

onMounted(async () => {
	state.listData.plugins = await loadMailReportPlugins();
});

// Query API
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
	const params = Object.assign(state.queryParams, {
		page: page.currentPage,
		pageSize: page.pageSize,
		field: sort.field,
		order: sort.order,
		descStr: 'desc',
	}) as MailRptPageReportInput;
	return await getAPI(RptEmailReportApi).apiMailRptemailreportPagePost(params);
};

// Query operation
const handleQuery = async () => {
	options.loading = true;
	await xGrid.value?.commitProxy('reload');
	options.loading = false;
	selectChangeEvent();
};

// Reset operation
const resetQuery = async () => {
	state.queryParams.code = undefined;
	state.queryParams.name = undefined;
	state.queryParams.pluginCode = undefined;
	state.queryParams.status = undefined;
	await xGrid.value?.commitProxy('reload');
};

/** Danh mục đổi thì bỏ cache để form lịch nạp lại danh sách báo cáo. */
const afterChange = async () => {
	clearMailReportCache();
	await handleQuery();
};

const entityName = () => $t('lz.entity.rptEmailReport.entityName');

// Open add dialog — mặc định nhóm đang lọc (nếu có)
const handleAdd = () => {
	state.title = $t('lz.label.base.titleAdd', { name: entityName() });
	state.operateType = 'add';
	editRef.value?.openDialog({ status: StatusTypeEnum.Enable, orderNo: 100, pluginCode: state.queryParams.pluginCode });
};

// Open copy dialog
const handleCopy = (row: MailPageReportOutput) => {
	state.title = $t('lz.label.base.titleCopy', { name: entityName() });
	state.operateType = 'copy';
	editRef.value?.openDialog(row);
};

// Open edit dialog
const handleEdit = (row: MailPageReportOutput) => {
	state.title = $t('lz.label.base.titleEdit', { name: entityName() });
	state.operateType = 'edit';
	editRef.value?.openDialog(row);
};

// Bật / tắt
const handleToggleStatus = async (row: MailPageReportOutput) => {
	const nextStatus = row.status === StatusTypeEnum.Disable ? StatusTypeEnum.Enable : StatusTypeEnum.Disable;
	await getAPI(RptEmailReportApi).apiMailRptemailreportTogglestatePost({ id: row.id!, status: nextStatus as any });
	ElMessage.success($t('lz.message.base.processSuccess'));
	await afterChange();
};

// Delete — backend chặn khi báo cáo đang được lịch gửi email dùng
const handleDelete = (row: MailPageReportOutput) => {
	ElMessageBox.confirm($t('lz.message.base.confirmDelete', { name: row.name }), $t('lz.message.base.warning'), {
		confirmButtonText: $t('lz.button.base.confirm'),
		cancelButtonText: $t('lz.button.base.cancel'),
		type: 'warning',
	})
		.then(async () => {
			await getAPI(RptEmailReportApi).apiMailRptemailreportDeletePost({ id: row.id! });
			ElMessage.success($t('lz.message.base.deleteSuccess'));
			await afterChange();
		})
		.catch(() => { });
};

// Batch delete
const handleBatchDelete = () => {
	const selectRecords = getCheckedRecords();
	ElMessageBox.confirm($t('lz.message.base.confirmBatchDelete', { total: selectRecords.length }), $t('lz.message.base.warning'), {
		confirmButtonText: $t('lz.button.base.confirm'),
		cancelButtonText: $t('lz.button.base.cancel'),
		type: 'warning',
	})
		.then(async () => {
			await getAPI(RptEmailReportApi).apiMailRptemailreportBatchdeletePost(
				selectRecords.filter((l) => l.id).map((l) => ({ id: l.id! }))
			);
			ElMessage.success($t('lz.message.base.deleteSuccess'));
			await afterChange();
		})
		.catch(() => { });
};

// Custom column
function handleCustom() {
	xGrid.value?.openCustom();
}

// Get selected records
function getCheckedRecords(): MailPageReportOutput[] {
	return xGrid.value?.getCheckboxRecords() ?? [];
}

function selectChangeEvent() {
	state.showBatchDelete = getCheckedRecords().length > 0;
}
</script>
