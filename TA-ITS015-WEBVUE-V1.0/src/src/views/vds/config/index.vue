<template>
	<div class="opconfig-container">
        <el-alert v-if="unmapped.length && !unmappedDismissed" type="warning" show-icon :close-text="$t('lz.button.base.close')" class="mb5" @close="unmappedDismissed = true">
            <template #title><el-space wrap><span>{{ $t('lz.message.vdsConfig.unmappedWarning', { count: unmapped.length }) }}</span><el-button link type="primary" @click="mappingState.isShowDialog = true">{{ $t('lz.button.vdsConfig.showUnmapped') }}</el-button></el-space></template>
        </el-alert>
        <el-dialog v-model="mappingState.isShowDialog" width="700px" draggable :close-on-click-modal="false" append-to-body>
            <template #header>
                <div style="color: var(--el-color-white)">
                    <el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"><ele-Edit /></el-icon>
                    <span>{{ $t('lz.message.vdsConfig.unmappedWarning', { count: unmapped.length }) }}</span>
                </div>
            </template>
            <vxe-grid class="xGrid-style" v-bind="unmappedGridOptions" :data="unmapped">
                <template #row_lastSeen="{ row }">{{ formatLastSeen(row.lastSeen) }}</template>
                <template #row_map="{ row }">
                    <el-button v-auth="'vdsConfig:add'" link type="primary" @click="mapAid(row.aidEventType)">{{ $t('lz.button.vdsConfig.map') }}</el-button>
                </template>
            </vxe-grid>
            <template #footer><span class="dialog-footer"><el-button @click="mappingState.isShowDialog = false">{{ $t('lz.button.base.close') }}</el-button></span></template>
        </el-dialog>
		<el-card shadow="hover" :body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', height: '100%', alignItems: 'start' }">
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" label-position="left" :inlineMessage="true" label-width="auto" style="flex: 1 1 0%">
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="24" :md="24" :lg="12" :xl="12">
						<el-form-item :label="$t('lz.entity.sysOpConfig.groupCode')" prop="groupCode">
							<el-select v-model="state.queryParams.groupCode" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.sysOpConfig.groupCode') })" clearable class="w100">
								<el-option v-for="item in state.groupCodeList" :label="item.name" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>

					<el-col class="mb5" :xs="24" :sm="24" :md="24" :lg="12" :xl="12">
						<el-form-item :label="`${$t('lz.entity.base.code')} / ${$t('lz.entity.base.name')}`" prop="keyword">
							<el-input
								v-model="state.queryParams.keyword"
								@keyup.enter="handleQuery"
								:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.code') })"
								:maxlength="EntityFieldLength.Length128"
								show-word-limit
								clearable
							/>
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>
			<el-divider style="height: calc(100% - 5px); margin: 0 10px" direction="vertical" />
			<el-row>
				<SearchButton authSearch="vdsConfig:list" @search="handleQuery" @reset="resetQuery" :loading="options.loading" />
			</el-row>
		</el-card>
		<!-- SearchBar: end -->

		<!-- GridTable: begin -->
		<el-card class="full-table" shadow="hover" style="margin-top: 5px">
			<vxe-grid ref="xGridVdsConfig" class="xGrid-style" v-bind="options" v-on="gridEvents">
				<template #toolbar_tools>
                    <el-button v-if="unmapped.length && unmappedDismissed" type="warning" plain @click="mappingState.isShowDialog = true">{{ $t('lz.label.vdsConfig.unmappedCount', { count: unmapped.length }) }}</el-button>
					<el-button v-auth="'vdsConfig:update'" :loading="refreshingWorker" @click="refreshWorker">{{ $t('lz.button.vdsConfig.refreshWorker') }}</el-button>
					<TableHeader @add="handleAdd" :show-delete="state.showBatchDelete" :auth-add="'vdsConfig:add'" :auth-delete="'vdsConfig:delete'" @refresh="handleQuery" @custom-column="handleCustom">
					</TableHeader>
				</template>
				<template #empty>
					<el-empty :image-size="200" />
				</template>
				<template #row_status="{ row }">
					<TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.SysStatusType, row.status)" />
				</template>
				<template #row_groupCode="{ row }">
					{{ state.groupCodeList?.find((group) => group.code == row.groupCode)?.name ?? row.groupCode }}
				</template>
				<template #row_tms="{ row }">
                    <span v-if="tmsType(row.groupCode === 'vds_event_display' ? row.code : row.value)">{{ tmsType(row.groupCode === 'vds_event_display' ? row.code : row.value)?.name }} ({{ row.groupCode === 'vds_event_display' ? row.code : row.value }})</span>
                    <el-tag v-else type="danger">{{ $t('lz.message.vdsConfig.missingTms') }}</el-tag>
                </template>
                <template #row_value="{ row }">{{ isSecret(row.code) ? '••••••••' : row.value }}</template>
				<template #row_record="{ row }">
					<ModifyRecord :data="row" />
				</template>
				<template #row_buttons="{ row }">
					<el-tooltip :content="$t('lz.button.base.copy')" placement="top" :auto-close="1000">
						<el-button icon="ele-CopyDocument" text type="warning" v-auth="'vdsConfig:add'" @click="handleCopy(row)"> </el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.edit')" placement="top" :auto-close="1000">
						<el-button icon="ele-Edit" text type="primary" v-auth="'vdsConfig:update'" @click="handleEdit(row)"> </el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.delete')" placement="top" :auto-close="1000">
						<el-button icon="ele-Delete" text type="danger" v-auth="'vdsConfig:delete'" :disabled="row.isSysConfig || !!deletingId" :loading="deletingId === row.id" @click="handleDelete(row)">
						</el-button>
					</el-tooltip>
				</template>
				<template #header_name> {{ $t('lz.label.vdsConfig.title') }} </template>
			</vxe-grid>
		</el-card>
		<!-- GridTable: end -->

		<!-- Primary components: Begin -->
		<editVdsConfigComp ref="editVdsConfigCompRef" :title="state.title" :operateType="state.operateType" :groups="state.groupCodeList" @handleQuery="handleQuery" />
		<!-- Primary components: End -->
	</div>
</template>

<script lang="ts" setup name="vdsConfig">
import { onMounted, reactive, ref, toRefs } from 'vue';
import { useDateFormat } from '@vueuse/core';
import { ElMessageBox, ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';
import type { VxeGridInstance } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { vdsTimestamp } from '/@/utils/vdsTimestamp';
import { TmsEventTypeApi, StatusEnum } from '/@/api-services/tms';
import type { EventTypeOutput } from '/@/api-services/tms';
import { getAPI } from '/@/utils/axios-utils';
import { VdsConfigApi, VdsConfigurationApi } from '/@/api-services/vds';
import type { VdsConfigOutput, PageVdsConfigInput, VdsConfigGroupOutput, UnmappedEventTypeOutput } from '/@/api-services/vds';
import type { OperateType } from '/@/types/constants/operate';
import ModifyRecord from '/@/components/table/modifyRecord.vue';
import TableHeader from '/@/components/advanced/table-header-operation.vue';
import SearchButton from '/@/components/advanced/search-button-operation.vue';
import TagInfo from '/@/components/advanced/tag-info.vue';
import { EntityFieldLength, DateTimeFormatEnum } from '/@/types/enums/baseEnum';
import { useBaseConfig } from '/@/stores/baseConfig';
import { BaseConfigTypeEnum } from '/@/types/enums';
import editVdsConfigComp from './component/editVdsConfig.vue';
const { t: $t } = useI18n();
const mappingGroup = 'vds_mapping_event_type';
const mappingState = reactive({
    isShowDialog: false,
    unmapped: [] as UnmappedEventTypeOutput[],
    unmappedDismissed: false,
    tmsTypes: [] as EventTypeOutput[],
});
const { unmapped, unmappedDismissed, tmsTypes } = toRefs(mappingState);
const tmsType = (code?: string | null) => tmsTypes.value.find(item => item.code === code && item.status === StatusEnum.NUMBER_1);
const formatLastSeen = (value?: Date) => (value ? useDateFormat(vdsTimestamp(value), DateTimeFormatEnum.DateTimeDefault).value : '—');
/* Danh sách AID chưa ánh xạ đã nạp sẵn trong bộ nhớ: tắt proxy/phân trang, bỏ chiều cao 'auto' để bảng co theo dữ liệu trong dialog. */
const unmappedGridOptions = useVxeTable<UnmappedEventTypeOutput>(
	{
		id: 'vdsConfigUnmappedGrid',
		name: 'vdsConfigUnmappedGrid',
		columns: [
			{ field: 'aidEventType', title: $t('lz.entity.vds.aidEvent'), minWidth: 160, showOverflow: 'tooltip' },
			{ field: 'count', title: $t('lz.entity.vdsConfig.count'), width: 100, align: 'right' },
			{ field: 'lastSeen', title: $t('lz.entity.vdsConfig.lastSeen'), minWidth: 190, slots: { default: 'row_lastSeen' } },
			{ width: 110, slots: { default: 'row_map' } },
		],
	},
	{
		height: '',
		maxHeight: 360,
		border: true,
		stripe: true,
		rowConfig: { keyField: 'aidEventType' },
		proxyConfig: { enabled: false },
		pagerConfig: { enabled: false },
		sortConfig: { remote: false },
		toolbarConfig: { enabled: false },
	}
);
async function loadMappingInfo() {
    const results = await Promise.allSettled([
        getAPI(TmsEventTypeApi).apiTmsTmseventtypeListGet(),
        getAPI(VdsConfigApi).apiVdsVdsconfigUnmappedeventtypesGet(30),
    ]);
    if (results[0].status === 'fulfilled') tmsTypes.value = results[0].value.data.result ?? [];
    if (results[1].status === 'fulfilled') unmapped.value = results[1].value.data.result ?? [];
    if (results.some(result => result.status === 'rejected')) ElMessage.error($t('lz.message.vdsConfig.mappingLoadFailed'));
}
function mapAid(aidEventType?: string | null) {
    if (!aidEventType) return;
    mappingState.isShowDialog = false;
    state.title = $t('lz.label.base.titleAdd', { name: $t('lz.label.vdsConfig.title') });
    state.operateType = 'add';
    editVdsConfigCompRef.value?.openDialog({ groupCode: mappingGroup, code: aidEventType, name: aidEventType, status: 1, orderNo: 100, isSysConfig: false });
}
const refreshingWorker = ref(false);
const refreshWorker = async () => {
	if (refreshingWorker.value) return;
	refreshingWorker.value = true;
	try {
		const response = await getAPI(VdsConfigurationApi).apiVdsVdsconfigurationRefreshPost(0);
		const results = Object.values(response.data.result?.refresh ?? {});
		if (response.data.result?.applied === true)
			ElMessage.success($t('lz.message.vdsConfig.applied'));
		else if (results.length && results.every(value => value === true))
			ElMessage.info($t('lz.message.vdsConfig.refreshSent'));
		else ElMessage.warning($t('lz.message.vdsConfig.refreshPartial'));
	} catch {
		ElMessage.warning($t('lz.message.vdsConfig.refreshFailed'));
	} finally { refreshingWorker.value = false; }
};
const getConfigItemByCode = useBaseConfig().getConfigItemByCode;
const xGridVdsConfig = ref<VxeGridInstance>();
const editVdsConfigCompRef = ref<InstanceType<typeof editVdsConfigComp>>();
const deletingId = ref('');
const isSecret = (code?: string | null) => /password|token|secret|api_key/i.test(code ?? '');
const state = reactive({
	title: '',
	queryParams: { groupCode: '', keyword: '' } as PageVdsConfigInput,
	operateType: 'add' as OperateType,
	showBatchDelete: false,
	groupCodeList: [] as VdsConfigGroupOutput[],
});
const options = useVxeTable<VdsConfigOutput>(
	{
		id: 'vdsConfigGrid',
		name: 'vdsConfigGrid',
		columns: [
			{ field: 'groupCode', title: $t('lz.entity.sysOpConfig.groupCode'), minWidth: 100, showOverflow: 'tooltip', slots: { default: 'row_groupCode' } },
			{ field: 'code', title: $t('lz.entity.base.code'), minWidth: 200, showOverflow: 'tooltip' },
			{ field: 'value', title: $t('lz.entity.sysOpConfig.value'), minWidth: 200, showOverflow: 'tooltip', slots: { default: 'row_value' } },
			{ field: 'tmsEventType', title: $t('lz.entity.vdsConfig.tmsEvent'), minWidth: 220, visible: false, slots: { default: 'row_tms' } },
			{ field: 'name', title: $t('lz.entity.base.name'), minWidth: 200, showOverflow: 'tooltip' },
			{ field: 'status', title: $t('lz.entity.base.status'), width: 110, slots: { default: 'row_status' } },
			{ field: '', title: $t('lz.label.base.audit'), width: 100, slots: { default: 'row_record' } },
			{ title: $t('lz.label.base.action'), fixed: 'right', width: 100, slots: { default: 'row_buttons' } },
		],
	},
	{
		proxyConfig: {
			autoLoad: true,
			sort: false,
			ajax: {
				query: async ({ page }) => {
					try {
						const column = xGridVdsConfig.value?.getColumnByField('tmsEventType');
                        if (column) await ([mappingGroup, 'vds_event_display'].includes(state.queryParams.groupCode ?? '') ? xGridVdsConfig.value?.showColumn(column) : xGridVdsConfig.value?.hideColumn(column));
						const params: PageVdsConfigInput = { ...state.queryParams, page: page.currentPage, pageSize: page.pageSize };
						return await getAPI(VdsConfigApi).apiVdsVdsconfigPagePost(params);
					} catch (cause) {
						ElMessage.error($t('lz.message.vdsConfig.loadFailed'));
						throw cause;
					}
				},
			},
		},
		pagerConfig: { pageSize: 50, pageSizes: [50, 100, 200] },
		toolbarConfig: { export: false },
	}
);
const gridEvents = {};
async function loadGroups() {
	try {
		state.groupCodeList = (await getAPI(VdsConfigApi).apiVdsVdsconfigGroupsGet()).data.result ?? [];
	} catch {
		ElMessage.error($t('lz.message.vdsConfig.groupsLoadFailed'));
	}
}
onMounted(async () => { await Promise.all([loadGroups(), loadMappingInfo()]); });
async function handleQuery() {
	await xGridVdsConfig.value?.commitProxy('reload');
	await Promise.all([loadGroups(), loadMappingInfo()]);
}
async function resetQuery() {
	state.queryParams.groupCode = '';
	state.queryParams.keyword = '';
	await handleQuery();
}
function handleAdd() {
	state.title = $t('lz.label.base.titleAdd', { name: $t('lz.label.vdsConfig.title') });
	state.operateType = 'add';
	editVdsConfigCompRef.value?.openDialog({ groupCode: state.queryParams.groupCode || 'vds_config', status: 1, orderNo: 100, isSysConfig: false });
}
function handleCopy(row: VdsConfigOutput) {
	state.title = $t('lz.label.base.titleCopy', { name: $t('lz.label.vdsConfig.title') });
	state.operateType = 'copy';
	editVdsConfigCompRef.value?.openDialog({ ...row, id: undefined, code: '', isSysConfig: false, value: isSecret(row.code) ? '' : row.value });
}
function handleEdit(row: VdsConfigOutput) {
	state.title = $t('lz.label.base.titleEdit', { name: $t('lz.label.vdsConfig.title') });
	state.operateType = 'edit';
	editVdsConfigCompRef.value?.openDialog(row);
}
async function handleDelete(row: VdsConfigOutput) {
	if (row.isSysConfig || deletingId.value || !row.id) return;
	try {
		await ElMessageBox.confirm($t('lz.message.base.confirmDelete', { name: row.name }), $t('lz.label.base.confirm'), {
			confirmButtonText: $t('lz.button.base.confirm'),
			cancelButtonText: $t('lz.button.base.cancel'),
			type: 'warning',
		});
	} catch {
		return;
	}
	if (deletingId.value) return;
	deletingId.value = row.id;
	try {
		await getAPI(VdsConfigApi).apiVdsVdsconfigDeletePost({ id: row.id });
		ElMessage.success($t('lz.message.base.processSuccess'));
		await handleQuery();
	} catch (cause) {
		ElMessage.error(cause instanceof Error ? cause.message : $t('lz.message.vdsConfig.deleteFailed'));
	} finally {
		deletingId.value = '';
	}
}
function handleCustom() {
	xGridVdsConfig.value?.openCustom();
}
</script>
