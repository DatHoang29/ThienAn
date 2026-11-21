<!--
	Nhật ký gửi mail (RptEmailLog) — chỉ xem. Mỗi dòng là một lần gửi: theo lịch, bấm Gửi ngay, hoặc gửi thử cấu hình.
	Log cũ hơn mail_log_retention_days (SysOpConfig) được job "mail-log-cleanup" dọn hằng ngày.
-->
<template>
	<div class="sys-pos-container">
		<el-card shadow="hover"
			:body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', height: '100%', alignItems: 'start' }">
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true"
				label-position="left" label-width="auto" style="flex: 1 1 0%">
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.mailLog.sentDate')">
							<el-date-picker v-model="state.dateRange" type="daterange" value-format="YYYY-MM-DD"
								format="DD/MM/YYYY" class="w100" unlink-panels
								:start-placeholder="$t('lz.label.mailLog.fromDate')"
								:end-placeholder="$t('lz.label.mailLog.toDate')" />
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<!-- Người nhận: tìm gần đúng trong To / CC / BCC, gợi ý từ sổ địa chỉ (RptEmailAddress) -->
						<el-form-item :label="$t('lz.entity.mailLog.recipients')">
							<el-autocomplete v-model="state.queryParams.recipient" clearable class="w100"
								:fetch-suggestions="queryAddresses" @select="handleQuery" @keyup.enter="handleQuery"
								@clear="state.queryParams.recipient = undefined"
								:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.mailLog.recipients') })" />
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.mailLog.status')">
							<el-select v-model="state.queryParams.status" clearable class="w100"
								@clear="state.queryParams.status = undefined"
								:placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.mailLog.status') })">
								<el-option :value="SuccessEnum.Success" :label="$t('lz.label.mailLog.success')" />
								<el-option :value="SuccessEnum.Fail" :label="$t('lz.label.mailLog.fail')" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.mailLog.triggerType')">
							<el-select v-model="state.queryParams.triggerType" clearable class="w100"
								@clear="state.queryParams.triggerType = undefined"
								:placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.mailLog.triggerType') })">
								<el-option v-for="item in getConfigDataByCode(BaseConfigTypeEnum.MailTriggerType)" :key="item.code"
									:value="Number(item.code)" :label="item.name" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.mailLog.name')">
							<el-input v-model="state.queryParams.name" clearable @keyup.enter.native="handleQuery"
								:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.mailLog.name') })" />
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
			<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options">
				<template #toolbar_buttons>
				</template>
				<template #toolbar_tools>
					<TableHeader :show-add="false" :show-delete="false" @refresh="handleQuery" @custom-column="handleCustom" />
				</template>
				<template #empty>
					<el-empty :image-size="200" />
				</template>
				<template #row_time="{ row }">
					{{ formatTime(row.occurredAt) }}
				</template>
				<template #row_schedule="{ row }">
					<template v-if="row.scheduleId">
						<div>{{ row.scheduleName  }}</div>
					</template>
					<span v-else class="sub">{{ $t('lz.label.mailLog.noSchedule') }}</span>
				</template>
				<template #row_trigger="{ row }">
					<el-tag v-if="row.triggerType != null" size="small" effect="plain"
						:type="getConfigItemByCode(BaseConfigTypeEnum.MailTriggerType, String(row.triggerType))?.tagType">
						{{ getConfigItemByCode(BaseConfigTypeEnum.MailTriggerType, String(row.triggerType))?.name ?? $t('lz.label.mailLog.triggerUnknown') }}
					</el-tag>
				</template>
				<template #row_status="{ row }">
					<el-tag v-if="row.status === SuccessEnum.Success" size="small" type="success" effect="plain">
						{{ $t('lz.label.mailLog.success') }}
					</el-tag>
					<el-tag v-else size="small" type="danger" effect="plain">{{ $t('lz.label.mailLog.fail') }}</el-tag>
				</template>
				<template #row_recipients="{ row }">
					<span>{{ row.recipients }}</span>
					<span v-if="countOf(row.cc) + countOf(row.bcc)" class="sub">
						(+{{ countOf(row.cc) + countOf(row.bcc) }} CC/BCC)
					</span>
				</template>
				<template #row_period="{ row }">
					<span v-if="row.periodFrom">{{ formatDate(row.periodFrom) }} – {{ formatDate(row.periodTo) }}</span>
				</template>
				<template #row_buttons="{ row }">
					<el-tooltip :content="$t('lz.button.base.detail')" placement="top" :auto-close="1000">
						<el-button icon="ele-View" text type="primary" @click="detailRef?.open(row.id)" />
					</el-tooltip>
				</template>
			</vxe-grid>
		</el-card>

		<DetailNhatKyGuiMail ref="detailRef" />
	</div>
</template>

<script lang="ts" setup name="mailNhatKyGuiMail">
import { defineAsyncComponent, reactive, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { useDateFormat } from '@vueuse/core';
import { VxeGridInstance, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { getAPI } from '/@/utils/axios-utils';
import { DATE_TIME_FORMAT } from '/@/types/formats';

import { RptEmailLogApi } from '/@/api-services/mail';
import type { MailLogOutput, MailPageLogInput } from '/@/api-services/mail';
import { useMailReportCatalog } from '/@/hooks/useMailReportCatalog';
import DetailNhatKyGuiMail from './component/detailNhatKyGuiMail.vue';
import { useBaseConfig } from '/@/stores/baseConfig';
import { BaseConfigTypeEnum } from '/@/types/enums';

const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));
const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));

const { t: $t } = useI18n();
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
const getConfigItemByCode = useBaseConfig().getConfigItemByCode;
const { loadMailAddresses, filterMailAddresses } = useMailReportCatalog();

/** BaseEnums.SuccessEnums của BE. */
const SuccessEnum = { Success: 1, Fail: 0 } as const;

const countOf = (text?: string | null) => (text ?? '').split(/[;,\s]+/).filter(Boolean).length;

const xGrid = ref<VxeGridInstance>();
const detailRef = ref<InstanceType<typeof DetailNhatKyGuiMail>>();

const state = reactive({
	queryParams: {
		status: undefined as number | undefined,
		triggerType: undefined as number | undefined,
		name: undefined as string | undefined,
		recipient: undefined as string | undefined,
	},
	/** [từ ngày, đến ngày] dạng YYYY-MM-DD */
	dateRange: null as [string, string] | null,
});

/** Gợi ý ô người nhận từ sổ địa chỉ. */
const queryAddresses = async (keyword: string, cb: (items: { value: string }[]) => void) => {
	const book = await loadMailAddresses();
	cb(filterMailAddresses(book, keyword, [], 20).map((value) => ({ value })));
};

const formatTime = (v?: Date | string | null) => (v ? useDateFormat(v, DATE_TIME_FORMAT).value : '');
const formatDate = (v?: Date | string | null) => (v ? useDateFormat(v, 'DD/MM/YYYY').value : '');

const options = useVxeTable<MailLogOutput>(
	{
		id: 'mailNhatKyGuiMailGrid',
		name: 'RptEmailLog',
		columns: [
			{ type: 'seq', title: $t('lz.label.base.idx'), width: 55, fixed: 'left' },
			{ field: 'occurredAt', title: $t('lz.entity.mailLog.occurredAt'), width: 150, sortable: true, slots: { default: 'row_time' } },
			{ field: 'scheduleId', title: $t('lz.entity.mailLog.name'), minWidth: 180, showOverflow: 'tooltip', align: 'left', slots: { default: 'row_schedule' } },
			{ field: 'triggerType', title: $t('lz.entity.mailLog.triggerType'), width: 130, sortable: true, slots: { default: 'row_trigger' } },
			{ field: 'status', title: $t('lz.entity.mailLog.status'), width: 110, sortable: true, slots: { default: 'row_status' } },
			{ field: 'subject', title: $t('lz.entity.mailLog.subject'), minWidth: 220, showOverflow: 'tooltip', align: 'left' },
			{ field: 'recipients', title: $t('lz.entity.mailLog.recipients'), minWidth: 220, showOverflow: 'tooltip', align: 'left', slots: { default: 'row_recipients' } },
			// { field: 'periodFrom', title: $t('lz.entity.mailLog.period'), width: 190, slots: { default: 'row_period' } },
			{ field: 'errorMessage', title: $t('lz.entity.mailLog.errorMessage'), minWidth: 220, showOverflow: 'tooltip', align: 'left' },
			// { field: 'attempts', title: $t('lz.entity.mailLog.attempts'), width: 90, sortable: true },
			{ title: $t('lz.label.base.action'), fixed: 'right', width: 70, showOverflow: true, slots: { default: 'row_buttons' } },
		],
	},
	{
		proxyConfig: { autoLoad: true, ajax: { query: ({ page, sort }) => handleQueryApi(page, sort) } },
		toolbarConfig: { export: false },
	}
);

const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
	const params: MailPageLogInput = {
		...state.queryParams,
		status: state.queryParams.status as any,
		fromDate: state.dateRange?.[0] ?? null,
		toDate: state.dateRange?.[1] ?? null,
		page: page.currentPage,
		pageSize: page.pageSize,
		field: sort.field,
		order: sort.order,
		descStr: 'desc',
	};
	return await getAPI(RptEmailLogApi).apiMailRptemaillogPagePost(params);
};

const handleQuery = async () => {
	options.loading = true;
	await xGrid.value?.commitProxy('reload');
	options.loading = false;
};

const resetQuery = async () => {
	state.queryParams.status = undefined;
	state.queryParams.triggerType = undefined;
	state.queryParams.name = undefined;
	state.queryParams.recipient = undefined;
	state.dateRange = null;
	await xGrid.value?.commitProxy('reload');
};

function handleCustom() {
	xGrid.value?.openCustom();
}
</script>

<style scoped lang="scss">
.sub {
	font-size: 12px;
	color: var(--el-text-color-secondary);
}
</style>
