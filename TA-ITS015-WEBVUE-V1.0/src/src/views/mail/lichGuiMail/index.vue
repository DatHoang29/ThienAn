<template>
	<div class="rpt-its-container">
		<el-card shadow="hover"
			:body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', height: '100%', alignItems: 'start' }">
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true"
				label-position="left" style="flex: 1 1 0%" label-width="auto">
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.emailSchedule.scheduleName')">
							<el-input v-model="state.queryParams.name"
								:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.emailSchedule.scheduleName') })"
								clearable @keyup.enter.native="handleQuery" />
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<!-- Lọc lịch CÓ CHỨA báo cáo này. Backend tra sang bảng RptEmailScheduleItem
						     vì một lịch đính kèm nhiều báo cáo. -->
						<el-form-item :label="$t('lz.entity.emailSchedule.report')">
							<el-select v-model="state.queryParams.reportCode" filterable clearable class="w100"
								:placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.emailSchedule.report') })">
								<el-option-group v-for="g in reportGroups" :key="g.code" :label="g.name">
									<el-option v-for="r in g.reports" :key="r.code!" :label="r.name || r.code" :value="r.code!" />
								</el-option-group>
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.emailSchedule.recipients')">
							<el-input v-model="state.queryParams.recipient"
								:placeholder="$t('lz.label.emailSchedule.searchByEmail')" clearable
								@keyup.enter.native="handleQuery" />
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.status')">
							<el-select v-model="state.queryParams.status" clearable class="w100"
								:placeholder="$t('lz.label.base.all')">
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
			<vxe-grid ref="xGridLichGuiMail" class="xGrid-style" v-bind="options" @checkbox-all="selectChangeEvent"
				@checkbox-change="selectChangeEvent" @toggle-row-expand="handleToggleExpand">
				<template #toolbar_buttons>
					<el-button type="primary" icon="ele-Expand" @click="handleExpandAll">
						{{ $t('lz.button.base.expandAll') }}
					</el-button>
					<el-button type="primary" icon="ele-Fold" @click="handleCollapseAll">
						{{ $t('lz.button.base.collapseAll') }}
					</el-button>
				</template>

				<template #toolbar_tools>
					<TableHeader :show-delete="state.showBatchDelete" @add="handleAdd" @delete="handleBatchDelete"
						@refresh="handleQuery" @custom-column="handleCustom" />
				</template>
				<template #empty>
					<el-empty :image-size="160" :description="$t('lz.label.emailSchedule.emptyList')" />
				</template>
				<template #row_name="{ row }">
					<span>{{ row.name }}</span>
				</template>
				<template #expand_content="{ row }">
					<div class="expand-box">
						<div v-if="state.loadingItems[row.id]" class="expand-hint">
							<el-icon class="is-loading"><ele-Loading /></el-icon>
							{{ $t('lz.label.emailSchedule.loadingItems') }}
						</div>
						<el-table v-else-if="state.itemsCache[row.id]?.length" :data="state.itemsCache[row.id]"
							size="small" border>
							<el-table-column type="index" label="#" width="45" align="center" />
							<el-table-column prop="reportName" :label="$t('lz.entity.emailSchedule.report')"
								min-width="180" show-overflow-tooltip />
							<el-table-column :label="$t('lz.entity.emailSchedule.periodOffset')" width="120">
								<template #default="{ row: item }">{{ getConfigItemByCode(BaseConfigTypeEnum.MailPeriod, item.periodOffset)?.name }}</template>
							</el-table-column>
							<el-table-column :label="$t('lz.entity.emailSchedule.format')" width="100"
								align="center">
								<template #default="{ row: item }">
									<el-tag size="small" effect="plain">{{ (item.format || 'pdf').toUpperCase()
									}}</el-tag>
								</template>
							</el-table-column>
						</el-table>

						<el-empty v-else :image-size="60"
							:description="$t('lz.label.emailSchedule.noItem')" />
					</div>
				</template>
				<template #row_reports="{ row }">
					<el-tooltip v-if="row.itemCount" :content="row.reportSummary" placement="top">
						<el-tag size="small" effect="plain">
							{{ $t('lz.label.emailSchedule.reportCount', { count: row.itemCount }) }}
						</el-tag>
					</el-tooltip>
					<el-tag v-else size="small" type="danger" effect="plain">
						{{ $t('lz.label.emailSchedule.noReport') }}
					</el-tag>
				</template>

				<template #row_recipients="{ row }">
					<el-tooltip :content="row.recipients" placement="top">
						<span>{{ shortRecipients(row.recipients) }}</span>
					</el-tooltip>
				</template>

				<template #row_status="{ row }">
					<el-switch :model-value="row.status === StatusEnum.Enable" @change="handleToggleStatus(row)" />
				</template>

				<template #row_next="{ row }">
					<span v-if="row.status === StatusEnum.Enable && row.nextTimeRun" class="next-ok">{{
						useDateFormat(row.nextTimeRun, DATE_TIME_FORMAT) }}</span>
					<span v-else class="next-off">{{ $t('lz.label.emailSchedule.turnedOff') }}</span>
				</template>
				<template #row_last="{ row }">
					<el-tag v-if="row.lastStatus === SuccessEnum.Fail" size="small" type="danger" effect="plain">
						{{ $t('lz.label.emailSchedule.runFailed') }}
					</el-tag>
					<el-tag v-else-if="row.lastStatus === SuccessEnum.Success" size="small" type="success"
						effect="plain">
						{{ $t('lz.label.emailSchedule.runSuccess') }}
					</el-tag>
					<span v-else class="sub">{{ $t('lz.label.emailSchedule.notRunYet') }}</span>
					<div v-if="row.lastTimeRun" class="sub">{{ useDateFormat(row.lastTimeRun, DATE_TIME_FORMAT) }}</div>
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

		<EditLichGuiMail :key="state.templateRevision" ref="editRef" :title="state.title" :operateType="state.operateType" :base-url="state.baseUrl"
			@handleQuery="handleQuery" />
	</div>
</template>

<script lang="ts" setup name="tollLichGuiMail">
import { computed, defineAsyncComponent, onMounted, reactive, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { ElMessage, ElMessageBox } from 'element-plus';
import { VxeGridInstance, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { getAPI } from '/@/utils/axios-utils';
import { OperateType } from '/@/types/constants/operate';
import ModifyRecord from '/@/components/table/modifyRecord.vue';

import { useDateFormat } from '@vueuse/core';
import { DATE_TIME_FORMAT } from '/@/types/formats'

import { RptEmailScheduleApi } from '/@/api-services/mail';
import { useItsMediaServerStore } from '/@/stores/its/itsMediaServer';
import { RptPageEmailScheduleInput, RptPageEmailScheduleOutput, RptEmailScheduleItemOutput } from '/@/api-services/mail';

import { useBaseConfig } from '/@/stores/baseConfig';
import { BaseConfigTypeEnum, StatusTypeEnum } from '/@/types/enums';
import { useMailReportCatalog } from '/@/hooks/useMailReportCatalog';
import type { MailReportOutput } from '/@/api-services/mail';
import type { PluginOutput } from '/@/api-services/system';
import EditLichGuiMail from '/@/views/mail/lichGuiMail/component/editLichGuiMail.vue';

const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));
const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));

const { t: $t } = useI18n();
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
const getConfigItemByCode = useBaseConfig().getConfigItemByCode;
const { loadMailReports, loadMailReportPlugins, groupReports } = useMailReportCatalog();

const xGridLichGuiMail = ref<VxeGridInstance>();
const editRef = ref<InstanceType<typeof EditLichGuiMail>>();

const StatusEnum = { Enable: StatusTypeEnum.Enable, Disable: StatusTypeEnum.Disable };
const SuccessEnum = { Success: 1, Fail: 0 };

const state = reactive({
	baseUrl: null as string | null,
	templateManagerVisible: false,
	templateRevision: 0,
	queryParams: {
		name: undefined,
		reportCode: undefined,
		recipient: undefined,
		status: undefined,
	},
	localPageParam: {
		pageSize: 50 as number,
		defaultSort: { field: 'createTime', order: 'desc', descStr: 'desc' } as any,
	},
	title: '',
	operateType: 'add' as OperateType,
	showBatchDelete: false,
	sendingId: '' as string,
	itemsCache: {} as Record<string, RptEmailScheduleItemOutput[] | undefined>,
	loadingItems: {} as Record<string, boolean>,

	listData: {
		/** Danh mục báo cáo gửi email (RptEmailReport) — ô lọc theo báo cáo, gom theo nhóm SysPlugin. */
		reports: [] as MailReportOutput[],
		plugins: [] as PluginOutput[],
	},
});

// Table parameter configuration
const options = useVxeTable<RptPageEmailScheduleOutput>(
	{
		id: 'tollLichGuiMailGrid',
		name: 'RptEmailSchedule',
		columns: [
			{ type: 'checkbox', fixed: 'left', width: 45 },
			{
				type: 'expand', field: 'name', title: $t('lz.entity.emailSchedule.scheduleName'), minWidth: 220,
				align: 'left', sortable: true, showOverflow: 'tooltip',
				slots: { default: 'row_name', content: 'expand_content' },
			},
			{ field: 'itemCount', title: $t('lz.entity.emailSchedule.report'), width: 120, align: 'center', slots: { default: 'row_reports' } },
			{ field: 'scheduleText', title: $t('lz.entity.emailSchedule.scheduleText'), minWidth: 180, showOverflow: 'tooltip', align: 'left' },
			{ field: 'recipients', title: $t('lz.entity.emailSchedule.recipients'), minWidth: 180, showOverflow: 'tooltip', align: 'left', slots: { default: 'row_recipients' } },
			{ field: 'status', title: $t('lz.entity.base.status'), width: 70, align: 'center', slots: { default: 'row_status' } },
			{ field: 'nextTimeRun', title: $t('lz.entity.emailSchedule.nextTimeRun'), width: 150, align: 'center', slots: { default: 'row_next' } },
			{ title: $t('lz.label.base.info'), width: 100, showOverflow: 'tooltip', slots: { default: 'row_record' } },
			{ title: $t('lz.label.base.action'), fixed: 'right', width: 145, showOverflow: true, slots: { default: 'row_buttons' } },
		],
	},
	{
		proxyConfig: { autoLoad: true, ajax: { query: ({ page, sort }) => handleQueryApi(page, sort) } },
		sortConfig: { defaultSort: state.localPageParam.defaultSort },
		pagerConfig: { pageSize: state.localPageParam.pageSize },
		toolbarConfig: { export: false },
		expandConfig: { accordion: false },
	}
);

onMounted(async () => {
	// Địa chỉ file server dùng chung (chỉ gọi API 1 lần cho toàn app)
	state.baseUrl = await useItsMediaServerStore().ensureLoaded();
});

onMounted(async () => {
	await handleQuery();
	[state.listData.reports, state.listData.plugins] = await Promise.all([loadMailReports(), loadMailReportPlugins()]);
});

/** Ô lọc theo báo cáo: mọi báo cáo chưa xoá (kể cả đang tắt — vẫn có lịch cũ dùng). */
const reportGroups = computed(() => groupReports(state.listData.reports, state.listData.plugins));

// Query API
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
	const params = Object.assign(
		state.queryParams,
		{ page: page.currentPage, pageSize: page.pageSize, field: sort.field, order: sort.order, descStr: 'desc' }
	) as RptPageEmailScheduleInput;

	return await getAPI(RptEmailScheduleApi).apiMailRptemailschedulePagePost(params);
};

// Query operation
const handleQuery = async () => {
	options.loading = true;
	state.itemsCache = {};
	state.loadingItems = {};

	await xGridLichGuiMail.value?.commitProxy('reload');

	options.loading = false;
	selectChangeEvent();
};

// Reset operation
const resetQuery = async () => {
	state.queryParams.name = undefined;
	state.queryParams.reportCode = undefined;
	state.queryParams.recipient = undefined;
	state.queryParams.status = undefined;
	await xGridLichGuiMail.value?.commitProxy('reload');
};

const shortRecipients = (raw?: string | null) => {
	const list = (raw ?? '').split(/[;,]/).map((x) => x.trim()).filter(Boolean);
	if (list.length <= 1) return list[0] ?? '';
	return `${list[0]} +${list.length - 1}`;
};


const loadItems = async (id?: string | null) => {
	if (!id || state.itemsCache[id] || state.loadingItems[id]) return;

	try {
		state.loadingItems[id] = true;
		const res = await getAPI(RptEmailScheduleApi).apiMailRptemailscheduleByidGet(id);
		state.itemsCache[id] = res.data.result?.items ?? [];
	} catch (error) {
	} finally {
		state.loadingItems[id] = false;
	}
};

const handleToggleExpand = async ({ expanded, row }: any) => {
	if (expanded) await loadItems(row?.id);
};

const handleExpandAll = async () => {
	const rows = xGridLichGuiMail.value?.getData() ?? [];
	await xGridLichGuiMail.value?.setAllRowExpand(true);
	await Promise.all(rows.map((row: any) => loadItems(row?.id)));
};

const handleCollapseAll = async () => {
	await xGridLichGuiMail.value?.setAllRowExpand(false);
};

const handleAdd = () => {
	state.title = $t('lz.label.base.titleAdd', { name: $t('lz.entity.emailSchedule.entityName') });
	state.operateType = 'add';
	editRef.value?.openDialog({
		status: StatusEnum.Enable,
		orderNo: 100,
		frequency: 'month',
		dayOfMonth: 1,
		hourOfDay: 8,
		minuteOfHour: 0,
		items: [],
	});
};

// Open copy dialog — nạp chi tiết (kèm danh sách báo cáo) như khi sửa, lưu thì tạo lịch mới
const handleCopy = async (row: any) => {
	try {
		const res = await getAPI(RptEmailScheduleApi).apiMailRptemailscheduleByidGet(row.id);
		state.title = $t('lz.label.base.titleCopy', { name: $t('lz.entity.emailSchedule.entityName') });
		state.operateType = 'copy';
		editRef.value?.openDialog(res.data.result ?? row);
	} catch (error) { }
};

const handleEdit = async (row: any) => {
	try {
		const res = await getAPI(RptEmailScheduleApi).apiMailRptemailscheduleByidGet(row.id);
		state.title = $t('lz.label.base.titleEdit', { name: $t('lz.entity.emailSchedule.entityName') });
		state.operateType = 'edit';
		editRef.value?.openDialog(res.data.result ?? row);
	} catch (error) { }
};

// const handleSendNow = (row: any) => {
// 	ElMessageBox.confirm(`Kết xuất và gửi ngay lịch "${row.name}"?`, 'Xác nhận', {
// 		confirmButtonText: 'Gửi',
// 		cancelButtonText: 'Hủy',
// 		type: 'info',
// 	})
// 		.then(async () => {
// 			try {
// 				state.sendingId = row.id;
// 				const res = await getAPI(RptEmailScheduleApi).apiMailRptemailscheduleSendnowPost({ id: row.id });
// 				// BE trả false khi máy chủ SMTP không nhận thư — lý do chi tiết nằm ở bảng RptEmailLog.
// 				if (res.data.result) ElMessage.success('Đã gửi');
// 				else ElMessage.warning('Gửi không thành công, xem chi tiết ở nhật ký gửi');
// 				await handleQuery();
// 			} catch (error) {
// 			} finally {
// 				state.sendingId = '';
// 			}
// 		})
// 		.catch(() => { });
// };

const handleToggleStatus = async (row: any) => {
	const nextStatus = row.status === StatusEnum.Enable ? StatusEnum.Disable : StatusEnum.Enable;
	try {
		await getAPI(RptEmailScheduleApi).apiMailRptemailscheduleTogglestatePost({ id: row.id, status: nextStatus });
		ElMessage.success(
			nextStatus === StatusEnum.Enable
				? $t('lz.message.emailSchedule.enabled', { name: row.name })
				: $t('lz.message.emailSchedule.disabled', { name: row.name })
		);
		await handleQuery();
	} catch (error) {
		await handleQuery();
	}
};

// Delete
const handleDelete = (row: any) => {
	ElMessageBox.confirm($t('lz.message.base.confirmDelete', { name: row.name }), $t('lz.label.base.warning'), {
		confirmButtonText: $t('lz.button.base.confirm'),
		cancelButtonText: $t('lz.button.base.cancel'),
		type: 'warning',
	})
		.then(async () => {
			try {
				await getAPI(RptEmailScheduleApi).apiMailRptemailscheduleDeletePost({ id: row.id });
				await handleQuery();
				ElMessage.success($t('lz.message.base.deleteSuccess'));
			} catch (error) { }
		})
		.catch(() => { });
};

// Batch delete
const handleBatchDelete = async () => {
	const selectRecords = getCheckedRecords();
	if (selectRecords.length === 0) {
		ElMessage.warning($t('lz.label.base.pleaseSelectItem'));
		return;
	}

	ElMessageBox.confirm(
		$t('lz.message.base.confirmBatchDelete', { total: selectRecords.length }),
		$t('lz.label.base.warning'),
		{
			confirmButtonText: $t('lz.button.base.confirm'),
			cancelButtonText: $t('lz.button.base.cancel'),
			type: 'warning',
		}
	)
		.then(async () => {
			try {
				await getAPI(RptEmailScheduleApi).apiMailRptemailscheduleBatchdeletePost(
					selectRecords.filter((l: any) => l.id).map((l: any) => ({ id: l.id }))
				);
				await handleQuery();
				ElMessage.success($t('lz.message.base.deleteSuccess'));
			} catch (error) { }
		})
		.catch(() => { });
};

// Custom column
function handleCustom() {
	xGridLichGuiMail.value?.openCustom();
}

function getCheckedRecords() {
	return xGridLichGuiMail.value?.getCheckboxRecords() ?? [];
}

function selectChangeEvent() {
	state.showBatchDelete = getCheckedRecords().length > 0;
}
</script>

<style lang="scss" scoped>
.rpt-its-container {
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

.sub {
	font-size: 12px;
	color: var(--el-text-color-secondary);
	line-height: 1.4;
}

.next-ok {
	color: var(--el-color-success);
}

.next-off {
	color: var(--el-text-color-placeholder);
}

/* Thụt lề trái 45px = đúng bề rộng cột checkbox, để vùng bung thẳng hàng với cột Tên lịch */
.expand-box {
	padding: 8px 8px 8px 45px;
	background-color: var(--el-fill-color-light);
}

.expand-hint {
	display: flex;
	align-items: center;
	gap: 6px;
	padding: 6px 0;
	font-size: 12px;
	color: var(--el-text-color-secondary);
}
</style>
