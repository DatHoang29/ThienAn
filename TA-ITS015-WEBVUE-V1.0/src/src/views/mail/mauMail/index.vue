<template>
	<div class="mail-template-container">
		<el-card shadow="hover" :body-style="{ padding: '5px 5px 0' }">
			<el-form :model="query" label-position="left" label-width="auto" @submit.prevent="handleQuery">
				<el-row :gutter="10">
					<el-col :xs="24" :sm="6"><el-form-item label="Mã template"><el-input v-model="query.code" clearable @keyup.enter="handleQuery" /></el-form-item></el-col>
					<el-col :xs="24" :sm="8"><el-form-item label="Tên template"><el-input v-model="query.name" clearable @keyup.enter="handleQuery" /></el-form-item></el-col>
					<el-col :xs="24" :sm="6"><el-form-item :label="$t('lz.entity.base.status')"><el-select v-model="query.status" clearable class="w100" :placeholder="$t('lz.label.base.all')"><el-option v-for="item in statusOptions" :key="item.code" :label="item.name" :value="Number(item.code)" /></el-select></el-form-item></el-col>
					<el-col :xs="24" :sm="4"><SearchButton @search="handleQuery" @reset="resetQuery" :loading="options.loading" /></el-col>
				</el-row>
			</el-form>
		</el-card>
		<el-card class="full-table" shadow="hover">
			<vxe-grid ref="gridRef" class="xGrid-style" v-bind="options" @checkbox-all="selectChangeEvent" @checkbox-change="selectChangeEvent">
				<template #toolbar_buttons><span class="page-title">Quản lý template mail</span></template>
				<template #toolbar_tools><TableHeader :show-delete="showBatchDelete" @add="openAdd" @delete="deleteSelected" @refresh="handleQuery" @custom-column="gridRef?.openCustom()" /></template>
				<template #empty><el-empty description="Chưa có template mail" /></template>
				<template #row_status="{ row }"><el-switch :model-value="row.status === StatusTypeEnum.Enable" :loading="busyIds.has(row.id)" @change="toggleStatus(row)" /></template>
				<template #row_record="{ row }"><ModifyRecord :data="row" /></template>
				<template #row_buttons="{ row }">
					<el-tooltip :content="$t('lz.button.base.copy')"><el-button icon="ele-CopyDocument" text type="warning" @click="openDetail(row, 'copy')" :disabled="busyIds.has(row.id)" /></el-tooltip>
					<el-tooltip :content="$t('lz.button.base.edit')"><el-button icon="ele-Edit" text type="primary" @click="openDetail(row, 'edit')" :disabled="busyIds.has(row.id)" /></el-tooltip>
					<el-tooltip :content="$t('lz.button.base.delete')"><el-button icon="ele-Delete" text type="danger" @click="deleteRows([row])" :disabled="busyIds.has(row.id)" /></el-tooltip>
				</template>
			</vxe-grid>
		</el-card>
		<EditMau ref="editRef" :title="dialogTitle" :operateType="operation" :base-url="state.baseUrl" @handleQuery="handleQuery" />
	</div>
</template>

<script lang="ts" setup name="mailMauMail">
import { computed, defineAsyncComponent, onBeforeUnmount, reactive, ref,onMounted } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { useI18n } from 'vue-i18n';
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { getAPI } from '/@/utils/axios-utils';
import { useBaseConfig } from '/@/stores/baseConfig';
import { BaseConfigTypeEnum, StatusTypeEnum } from '/@/types/enums';
import { RptEmailTemplateApi } from '/@/api-services/mail';
import type { MailPageTemplateInput, RptPageEmailTemplateOutput } from '/@/api-services/mail';
import ModifyRecord from '/@/components/table/modifyRecord.vue';
import EditMau from './component/editMau.vue';
import { clearMailTemplateCache } from '../lichGuiMail/component/mailTemplateCache';
import { useItsMediaServerStore } from '/@/stores/its/itsMediaServer';

const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));
const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));
const { t: $t } = useI18n();
const state = reactive({ baseUrl: null as string | null });
const baseConfig = useBaseConfig();
const statusOptions = computed(() => baseConfig.getConfigDataByCode(BaseConfigTypeEnum.SysStatusType) ?? []);
const gridRef = ref<VxeGridInstance>();
const query = reactive<MailPageTemplateInput>({ code: undefined, name: undefined, status: undefined });
const showBatchDelete = ref(false);
const busyIds = reactive(new Set<string>());
const editRef = ref<InstanceType<typeof EditMau>>();
const operation = ref<'add' | 'edit' | 'copy' | 'view'>('add');
let disposed = false;
let detailRequest = 0;
const dialogTitle = computed(() => ({ add: 'Thêm template mail', edit: 'Sửa template mail', copy: 'Sao chép template mail', view: 'Xem template mail' })[operation.value]);
const options = useVxeTable<RptPageEmailTemplateOutput>({
	id: 'mailMauMailGrid', name: 'RptEmailTemplate', columns: [
		{ type: 'checkbox', fixed: 'left', width: 45 },
		{ field: 'code', title: 'Mã template', minWidth: 150, sortable: true, align: 'left', showOverflow: 'tooltip' },
		{ field: 'name', title: 'Tên template', minWidth: 220, sortable: true, align: 'left', showOverflow: 'tooltip' },
		{ field: 'status', title: $t('lz.entity.base.status'), width: 100, align: 'center', slots: { default: 'row_status' } },
		{ field: 'orderNo', title: $t('lz.entity.base.orderNo'), width: 100, sortable: true },
		{ title: $t('lz.label.base.info'), width: 100, slots: { default: 'row_record' } },
		{ title: $t('lz.label.base.action'), fixed: 'right', width: 185, slots: { default: 'row_buttons' } },
	],
}, {
	proxyConfig: { autoLoad: true, ajax: { query: ({ page, sort }) => queryApi(page, sort) } },
	sortConfig: { defaultSort: { field: 'orderNo', order: 'asc' } }, pagerConfig: { pageSize: 50 }, toolbarConfig: { export: false },
});
function queryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) {
	return getAPI(RptEmailTemplateApi).apiMailRptemailtemplatePagePost({ ...query, page: page.currentPage, pageSize: page.pageSize, field: sort.field, order: sort.order, descStr: 'desc' });
}
async function handleQuery() { try { options.loading = true; await gridRef.value?.commitProxy('reload'); } finally { options.loading = false; selectChangeEvent(); } }
function resetQuery() { Object.assign(query, { code: undefined, name: undefined, status: undefined }); return handleQuery(); }
function selectChangeEvent() { showBatchDelete.value = !!gridRef.value?.getCheckboxRecords().length; }
function openAdd() {
	detailRequest++;
	operation.value = 'add';
	void editRef.value?.openDialog({ content: '<p><br></p>', status: StatusTypeEnum.Enable, orderNo: 100 });
}
async function openDetail(row: RptPageEmailTemplateOutput, mode: 'edit' | 'copy' | 'view') {
	if (!row.id || busyIds.has(row.id)) return;
	busyIds.add(row.id);
	const request = ++detailRequest;
	try {
		const response = await getAPI(RptEmailTemplateApi).apiMailRptemailtemplateByidGet(row.id);
		const detail = response.data.result;
		if (!detail || detail.content == null) { ElMessage.error('Không tải được nội dung template'); return; }
		if (disposed || request !== detailRequest) return;
		operation.value = mode;
		await editRef.value?.openDialog({
			id: detail.id, code: detail.code, name: detail.name, content: detail.content,
			status: detail.status, orderNo: detail.orderNo, remark: detail.remark,
		});
	} catch {  } finally { busyIds.delete(row.id); }
}
async function toggleStatus(row: RptPageEmailTemplateOutput) {
	if (!row.id || busyIds.has(row.id)) return; busyIds.add(row.id);
	try { await getAPI(RptEmailTemplateApi).apiMailRptemailtemplateTogglestatePost({ id: row.id, status: row.status === StatusTypeEnum.Enable ? StatusTypeEnum.Disable : StatusTypeEnum.Enable }); clearMailTemplateCache(); ElMessage.success($t('lz.message.base.processSuccess')); await handleQuery(); }
	catch { /* The model value stays unchanged on failure. */ } finally { busyIds.delete(row.id); }
}
function deleteSelected() { return deleteRows(gridRef.value?.getCheckboxRecords() ?? []); }
async function deleteRows(rows: RptPageEmailTemplateOutput[]) {
	const ids = rows.flatMap(row => row.id ? [row.id] : []); if (!ids.length || ids.some(id => busyIds.has(id))) return;
	try { await ElMessageBox.confirm(ids.length === 1 ? $t('lz.message.base.confirmDelete', { name: rows[0].name }) : $t('lz.message.base.confirmBatchDelete', { total: ids.length }), $t('lz.label.base.warning'), { type: 'warning', confirmButtonText: $t('lz.button.base.confirm'), cancelButtonText: $t('lz.button.base.cancel') }); } catch { return; }
	ids.forEach(id => busyIds.add(id));
	try {
		if (ids.length === 1) await getAPI(RptEmailTemplateApi).apiMailRptemailtemplateDeletePost({ id: ids[0] }); else await getAPI(RptEmailTemplateApi).apiMailRptemailtemplateBatchdeletePost(ids.map(id => ({ id })));
		clearMailTemplateCache(); ElMessage.success($t('lz.message.base.deleteSuccess')); await handleQuery();
	} catch { /* API interceptor displays request errors. */ } finally { ids.forEach(id => busyIds.delete(id)); }
}
async function beforeManagerClose(done: () => void) {
	detailRequest++;
	if (editRef.value) await editRef.value.beforeManagerClose(done);
	else done();
}
onMounted(async () => {
	// Địa chỉ file server dùng chung (chỉ gọi API 1 lần cho toàn app)
	state.baseUrl = await useItsMediaServerStore().ensureLoaded();
});
defineExpose({ beforeManagerClose });

onBeforeUnmount(() => { disposed = true; detailRequest++; });
</script>

<style scoped lang="scss">
.mail-template-container { height: 100%; min-height: 500px; display: flex; flex-direction: column; gap: 5px; }
.full-table { flex: 1; display: flex; flex-direction: column; overflow: hidden; }
.page-title { font-weight: 600; }
</style>
