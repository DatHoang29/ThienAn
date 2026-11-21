<template>
	<div class="tms-incident-automation-container">
		<el-card shadow="hover" :body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', height: '100%', alignItems: 'start' }">
			<el-form :model="state.queryParams" ref="queryForm" label-position="left" :show-message="false" :inlineMessage="true" label-width="auto" style="flex: 1 1 0%">
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.tms.eventType')" prop="eventTypeId">
							<el-select
								v-model="state.queryParams.eventTypeId"
								:placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.tms.eventType') })"
								filterable
								clearable
								class="w100"
							>
								<el-option v-for="item in state.eventTypeList" :key="item.id" :label="item.name" :value="item.id" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.tmsIncidentAutomationConfig.isAutoExecuteEnabled')" prop="isAutoExecuteEnabled">
							<el-select v-model="state.queryParams.isAutoExecuteEnabled" clearable class="w100" :placeholder="$t('lz.label.base.pleaseSelect', { input: '' })">
								<el-option :label="$t('lz.label.base.enable')" :value="true" />
								<el-option :label="$t('lz.label.base.disable')" :value="false" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.status')" prop="status">
							<el-select v-model="state.queryParams.status" filterable clearable class="w100" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.base.status') })">
								<el-option v-for="item in getConfigDataByCode(BaseConfigTypeEnum.SysStatusType)" :key="item.code" :label="item.name" :value="parseInt(item.code)" />
							</el-select>
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>

			<el-divider style="height: calc(100% - 5px); margin: 0 10px" direction="vertical" />

			<el-row>
				<SearchButton :auth-search="'tmsIncidentAutomationConfig:page'" @search="handleQuery" @reset="resetQuery" :loading="options.loading" />
			</el-row>
		</el-card>

		<el-card class="full-table" shadow="hover" style="margin-top: 5px">
			<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
				<template #toolbar_tools>
					<TableHeader :show-delete="false" :auth-add="'tmsIncidentAutomationConfig:add'" @add="handleAdd" @refresh="handleQuery" @custom-column="handleCustom" />
				</template>
				<template #empty>
					<el-empty :image-size="200" />
				</template>

				<!-- Bật tự động -->
				<template #row_auto="{ row }">
					<el-tag v-if="row.isAutoExecuteEnabled" type="success" effect="dark">{{ $t('lz.label.base.enable') }}</el-tag>
					<el-tag v-else type="info">{{ $t('lz.label.base.disable') }}</el-tag>
				</template>

				<!-- Thời gian cảnh báo -->
				<template #row_delay="{ row }">
					<span>{{ formatSeconds(row.warningDelaySeconds) }}</span>
				</template>

				<!-- Nguồn phát hiện được phép tự động. Rỗng = mọi nguồn, ghi rõ để không hiểu nhầm là chưa cấu hình. -->
				<template #row_allowedSources="{ row }">
					<template v-if="parseAllowedSources(row.allowedSources).length">
						<el-tag v-for="code in parseAllowedSources(row.allowedSources)" :key="code" size="small" class="mr5" effect="plain">
							{{ getConfigItemByCode(BaseConfigTypeEnum.TmsSourceDetectIncident, code)?.name ?? code }}
						</el-tag>
					</template>
					<el-tag v-else size="small" type="info" effect="plain">{{ $t('lz.label.base.all') }}</el-tag>
				</template>

				<!-- Tự động kết thúc -->
				<template #row_autoFinish="{ row }">
					<el-tag v-if="row.isAutoFinishEnabled" type="warning" effect="plain">{{ formatSeconds(row.autoFinishAfterSeconds) }}</el-tag>
					<span v-else>-</span>
				</template>

				<template #row_unclaimed="{ row }">
                    <el-tag v-if="row.isUnclaimedAutoFinishEnabled" type="warning">{{ formatSeconds(row.unclaimedAutoFinishAfterSeconds) }}</el-tag>
                    <span v-else>-</span>
                </template>
                <template #row_status="{ row }">
					<TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.SysStatusType, row.status)" />
				</template>
				<template #row_record="{ row }">
					<ModifyRecord :data="row" />
				</template>
				<template #row_buttons="{ row }">
					<el-tooltip :content="$t('lz.button.base.edit')" placement="top" :auto-close="1000">
						<el-button icon="ele-Edit" text type="primary" v-auth="'tmsIncidentAutomationConfig:update'" @click="handleEdit(row)" />
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.delete')" placement="top" :auto-close="1000">
						<el-button icon="ele-Delete" text type="danger" v-auth="'tmsIncidentAutomationConfig:delete'" @click="handleDelete(row)" />
					</el-tooltip>
				</template>
			</vxe-grid>
		</el-card>

		<EditIncidentAutomationConfig ref="editRef" :title="state.title" :operateType="state.operateType" @handleQuery="handleQuery" />
	</div>
</template>

<script lang="ts" setup name="tmsIncidentAutomationConfig">
import { onMounted, reactive, ref, defineAsyncComponent } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { Local } from '/@/utils/storage';
import EditIncidentAutomationConfig from './component/editIncidentAutomationConfig.vue';
import ModifyRecord from '/@/components/table/modifyRecord.vue';
import { getAPI } from '/@/utils/axios-utils';
import { TmsIncidentAutomationConfigApi, TmsEventTypeApi } from '/@/api-services/tms';
import type { PageIncidentAutomationConfigOutput, PageIncidentAutomationConfigInput } from '/@/api-services/tms';
import { OperateType } from '/@/types/constants/operate';
import { BaseConfigTypeEnum } from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';

const getConfigItemByCode = useBaseConfig().getConfigItemByCode;
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));
const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));
const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));

const { t: $t } = useI18n();
const xGrid = ref<VxeGridInstance>();
const editRef = ref<InstanceType<typeof EditIncidentAutomationConfig>>();

const state = reactive({
	queryParams: {
		eventTypeId: undefined,
		isAutoExecuteEnabled: undefined,
		status: undefined,
	},
	localPageParam: {
		pageSize: 50 as number,
		defaultSort: {} as any,
	},
	title: '',
	operateType: 'add' as OperateType,
	eventTypeList: [] as any,
});

const localPageParamKey = 'localPageParam:tmsIncidentAutomationConfig';

// Định dạng số giây sang dạng dễ đọc (vd 90 -> 1 phút 30 giây)
const formatSeconds = (value?: number | null) => {
	if (value == null) return '-';
	if (value < 60) return `${value}s`;
	const minutes = Math.floor(value / 60);
	const seconds = value % 60;
	return seconds ? `${minutes}m ${seconds}s` : `${minutes}m`;
};

/**
 * Author: SonTH (port KienLT - 17/10/2026)
 * Description: Tách chuỗi allowedSources của backend (các mã cách nhau bởi dấu phẩy) thành mảng mã.
 *              Rỗng/null nghĩa là cấu hình áp dụng cho MỌI nguồn.
 */
const parseAllowedSources = (value?: string | null): string[] =>
	(value ?? '')
		.split(',')
		.map((code) => code.trim())
		.filter((code) => code.length > 0);

const options = useVxeTable<PageIncidentAutomationConfigOutput>(
	{
		id: 'tmsIncidentAutomationConfig',
		name: 'IncidentAutomationConfig',
		columns: [
			{ field: 'eventTypeName', title: $t('lz.entity.tms.eventType'), minWidth: 180, showOverflow: 'tooltip', sortable: true },
			{ field: 'isAutoExecuteEnabled', title: $t('lz.entity.tmsIncidentAutomationConfig.isAutoExecuteEnabled'), width: 130, slots: { default: 'row_auto' } },
			{
				field: 'triggerState',
				title: $t('lz.entity.tmsIncidentAutomationConfig.triggerState'),
				width: 130,
				showOverflow: 'tooltip',
				formatter: ({ cellValue }: any) => getConfigItemByCode(BaseConfigTypeEnum.TmsIncidentAutomationTriggerState, cellValue)?.name ?? cellValue,
			},
			{ field: 'warningDelaySeconds', title: $t('lz.entity.tmsIncidentAutomationConfig.warningDelaySeconds'), width: 150, slots: { default: 'row_delay' } },
			{ field: 'trafficPlanName', title: $t('lz.entity.tmsTrafficPlan.trafficPlan'), minWidth: 180, showOverflow: 'tooltip' },
			{ field: 'allowedSources', title: $t('lz.entity.tmsIncidentAutomationConfig.allowedSources'), minWidth: 160, showOverflow: 'tooltip', slots: { default: 'row_allowedSources' } },
			{ field: 'isAutoFinishEnabled', title: $t('lz.entity.tmsIncidentAutomationConfig.autoFinishAfterSeconds'), width: 150, slots: { default: 'row_autoFinish' } },
			{ field: 'isUnclaimedAutoFinishEnabled', title: $t('lz.label.tmsIncidentAutomationConfig.finishUnclaimed'), width: 180, slots: { default: 'row_unclaimed' } },
			{ field: 'status', title: $t('lz.entity.base.status'), width: 120, sortable: true, slots: { default: 'row_status' } },
			{ field: '', title: $t('lz.label.base.audit'), width: 100, showOverflow: 'tooltip', slots: { default: 'row_record' } },
			{ title: $t('lz.label.base.action'), fixed: 'right', width: 110, showOverflow: true, slots: { default: 'row_buttons' } },
		],
	},
	{
		proxyConfig: { autoLoad: true, ajax: { query: ({ page, sort }) => handleQueryApi(page, sort) } },
		toolbarConfig: { export: false },
	}
);

onMounted(async () => {
	state.localPageParam = Local.get(localPageParamKey) || state.localPageParam;
	await loadEventTypes();
	await handleQuery();
});

// Nạp danh sách loại sự kiện cho bộ lọc
const loadEventTypes = async () => {
	const res = await getAPI(TmsEventTypeApi).apiTmsTmseventtypeListGet();
	state.eventTypeList = res.data.result ?? [];
};

const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
	const params = Object.assign(state.queryParams, {
		page: page.currentPage,
		pageSize: page.pageSize,
		field: sort?.order ? sort?.field : undefined,
		order: sort?.order ? sort?.order : undefined,
		descStr: 'desc',
	}) as PageIncidentAutomationConfigInput;
	return await getAPI(TmsIncidentAutomationConfigApi).apiTmsTmsincidentautomationconfigPagePost(params);
};

const handleQuery = async () => {
	options.loading = true;
	await xGrid.value?.commitProxy('reload');
	options.loading = false;
};

const resetQuery = async () => {
	state.queryParams.eventTypeId = undefined;
	state.queryParams.isAutoExecuteEnabled = undefined;
	state.queryParams.status = undefined;
	await xGrid.value?.commitProxy('reload');
};

const handleAdd = () => {
	state.title = $t('lz.label.base.titleAdd', { name: $t('lz.entity.tmsIncidentAutomationConfig.automationConfig') });
	state.operateType = 'add';
	editRef.value?.openDialog({ status: 1, warningDelaySeconds: 30, isAutoExecuteEnabled: false, isAutoFinishEnabled: false });
};

const handleEdit = (row: any) => {
	state.title = $t('lz.label.base.titleEdit', { name: $t('lz.entity.tmsIncidentAutomationConfig.automationConfig') });
	state.operateType = 'edit';
	editRef.value?.openDialog(row);
};

const handleDelete = (row: any) => {
	ElMessageBox.confirm($t('lz.message.base.confirmDelete', { name: row.eventTypeName }), $t('lz.message.base.warning'), {
		confirmButtonText: $t('lz.button.base.confirm'),
		cancelButtonText: $t('lz.button.base.cancel'),
		type: 'warning',
	})
		.then(async () => {
			await getAPI(TmsIncidentAutomationConfigApi).apiTmsTmsincidentautomationconfigDeletePost({ id: row.id });
			handleQuery();
			ElMessage.success($t('lz.message.base.deleteSuccess'));
		})
		.catch(() => {});
};

function handleCustom() {
	xGrid.value?.openCustom();
}

const gridEvents: VxeGridListeners<PageIncidentAutomationConfigOutput> = {
	async pageChange({ pageSize }) {
		state.localPageParam.pageSize = pageSize;
		Local.set(localPageParamKey, state.localPageParam);
	},
	async sortChange({ field, order }) {
		state.localPageParam.defaultSort = { field: field, order: order!, descStr: 'desc' };
		Local.set(localPageParamKey, state.localPageParam);
	},
};
</script>
