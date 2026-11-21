<template>
	<div class="sys-oplog-container">
		<el-card shadow="hover" :body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', height: '100%', alignItems: 'start' }">
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true" label-width="auto" style="flex: 1 1 0%">
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.startTime')" prop="name">
							<el-date-picker v-model="state.queryParams.startTime" type="datetime" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.startTime')})" :shortcuts="shortcuts" class="w100" :clearable="false"/>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.log.displayTitle')" prop="displayTitle">
							<el-input v-model="state.queryParams.displayTitle" clearable :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.log.displayTitle')})" @keyup.enter.native="handleQuery" />
						</el-form-item>
					</el-col>
				</el-row>
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.endTime')" prop="code">
							<el-date-picker v-model="state.queryParams.endTime" type="datetime" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.endTime')})" :shortcuts="shortcuts" class="w100" :clearable="false"/>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.account')" prop="account">
							<el-input v-model="state.queryParams.account" clearable :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.account')})" @keyup.enter.native="handleQuery" />
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>

			<el-divider style="height: calc(100% - 5px); margin: 0 10px" direction="vertical" />

			<el-row>
				<SearchButton :loading="options.loading" @reset="resetQuery" @search="handleQuery" :auth-search="'sysOpLog:page'"></SearchButton>
			</el-row>
		</el-card>

		<el-card class="full-table" shadow="hover" style="margin-top: 5px">
			<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents" @cell-dblclick="handleView">
				<template #toolbar_buttons>
				</template>
				<template #toolbar_tools> 
					<TableHeader @refresh="handleQuery" @custom-column="handleCustom"
						@delete="" >
						<template #default>
							<el-button icon="ele-DeleteFilled" type="danger" @click="handleClear" v-auth="'sysOpLog:clear'"> {{ $t('lz.button.base.clear') }} </el-button>
						</template>
					</TableHeader>
				</template>
				<template #empty>
					<el-empty :image-size="200" />
				</template>
				<template #row_logLevel="{ row }">
					<el-tag v-if="row.logLevel === 1">Debug</el-tag>
					<el-tag v-else-if="row.logLevel === 2">Message</el-tag>
					<el-tag v-else-if="row.logLevel === 3">Warning</el-tag>
					<el-tag v-else-if="row.logLevel === 4">Error</el-tag>
					<el-tag v-else>Other</el-tag>
				</template>
				<template #row_status="{ row }">
					<el-tag v-if="row.status === '200'" type="success">{{row.status}}</el-tag>
					<el-tag v-else type="danger">{{row.status}}</el-tag>
				</template>
				<template #row_buttons="{ row }">
					<el-button icon="ele-InfoFilled" text type="primary" @click="handleView({ row })"> {{ $t('lz.button.base.detail') }} </el-button>
				</template>
			</vxe-grid>
		</el-card>

		<el-dialog v-model="state.visible" draggable overflow destroy-on-close>
			<template #header>
				<div style="color: #fff">
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Document /> </el-icon>
					<span> {{ $t('lz.label.log.detail') }} </span>
				</div>
			</template>
			<el-tabs v-model="state.activeTab">
				<el-tab-pane :label="$t('lz.label.log.logMessage')" name="message">
					<el-scrollbar height="calc(100vh - 250px)">
						<pre>{{ state.detail.message }}</pre>
					</el-scrollbar>
				</el-tab-pane>
				<el-tab-pane :label="$t('lz.label.log.requestParameter')" name="request" :lazy="true">
					<el-scrollbar height="calc(100vh - 250px)">
						<vue-json-pretty :data="state.detail.requestParam" showLength showIcon showLineNumber showSelectController />
					</el-scrollbar>
				</el-tab-pane>
				<el-tab-pane :label="$t('lz.label.log.responseContent')" name="return" :lazy="true">
					<el-scrollbar height="calc(100vh - 250px)">
						<vue-json-pretty :data="state.detail.returnResult" showLength showIcon showLineNumber showSelectController />
					</el-scrollbar>
				</el-tab-pane>
			</el-tabs>
		</el-dialog>
	</div>
</template>

<script lang="ts" setup name="sysOpLog">
import { onMounted, reactive, ref, defineAsyncComponent } from 'vue';
import { ElMessage } from 'element-plus';
import { useDateTimeShortCust } from '/@/hooks/dateTimeShortCust';
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { Local } from '/@/utils/storage';
import VueJsonPretty from 'vue-json-pretty';
import 'vue-json-pretty/lib/styles.css';
import { StringToObj } from '/@/utils/json-utils';

import { getAPI } from '/@/utils/axios-utils';
import { SysLogOpApi } from '/@/api-services/system';
import { AppOperationLog, PageLogOpInput } from '/@/api-services/system';
import {ElMessageBox} from 'element-plus';
import { useNow, useDateFormat } from '@vueuse/core'; // Import useNow
import { DATE_TIME_FORMAT } from '/@/types/formats'

import { useI18n } from 'vue-i18n';
const { t: $t } = useI18n();

const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));
const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));

const shortcuts = useDateTimeShortCust().map(item => ({ text: $t(item.text), value: item.value }));
const xGrid = ref<VxeGridInstance>();
const now = useNow(); // Get the current date and time

const state = reactive({
	queryParams: {
		startTime: new Date(new Date(now.value.getTime() - 3 * 24 * 60 * 60 * 1000).setHours(0, 0, 0, 0)), // Set to 0:00 three days ago
		endTime: new Date(new Date(now.value).setHours(23, 59, 59, 999)), // Set to today at 23:59:59
		displayTitle: '',
		account: '',
	},
	localPageParam: {
		pageSize: 50 as number,
		defaultSort: {order: 'desc', descStr: 'desc' },
	},
	visible: false,
	detail: {
		requestParam: undefined,
		returnResult: undefined,
		message: '' as string | null | undefined,
		exception: undefined,
	},
	activeTab: 'message',
});

// Local storage parameters
const localPageParamKey = 'localPageParam:sysOpLog';
// Table parameter configuration
const options = useVxeTable<AppOperationLog>(
	{
		id: 'sysOpLog',
		name: 'Operation Log',
		columns: [
			{ type: 'seq', title: $t('lz.label.base.idx'), width: 60, fixed: 'left' },
			{ field: 'logDateTime', title: $t('lz.entity.log.logTime'), minWidth: 130, fixed: 'left', showOverflow: 'tooltip',formatter: ({ cellValue }: any) => useDateFormat(cellValue,DATE_TIME_FORMAT)?.value  },
			{ field: 'controllerName', title: $t('lz.entity.log.controllerName'), minWidth: 120, showOverflow: 'tooltip' },
			{ field: 'displayTitle', title: $t('lz.entity.log.displayTitle'), minWidth: 150, showOverflow: 'tooltip' },
			{ field: 'actionName', title: $t('lz.entity.log.actionName'), minWidth: 150, showOverflow: 'tooltip' },
			{ field: 'httpMethod', title: $t('lz.entity.log.httpMethod'), minWidth: 90, showOverflow: 'tooltip' },
			{ field: 'account', title: $t('lz.entity.base.account'), minWidth: 100, showOverflow: 'tooltip' },
			{ field: 'elapsed', title: $t('lz.entity.log.elapsedTime'), minWidth: 100, showOverflow: 'tooltip' },
			{ field: 'status', title: $t('lz.entity.base.status'), minWidth: 70, showOverflow: 'tooltip', slots: { default: 'row_status' } },
			{ field: 'remoteIp', title: $t('lz.entity.base.ipAddress'), minWidth: 100, showOverflow: 'tooltip' },
			{ field: 'browser', title: $t('lz.entity.base.browser'), minWidth: 100, showOverflow: 'tooltip' },
			{ title: $t('lz.label.base.action'), fixed: 'right', width: 100, showOverflow: true, slots: { default: 'row_buttons' } },
		],
	},
	// vxeGrid configuration parameters (can override any parameters), refer to vxe-table official documentation
	{
		// Proxy configuration
		proxyConfig: { autoLoad: true, ajax: { query: ({ page, sort }) => handleQueryApi(page, sort) } },
		// Sorting configuration
		sortConfig: { defaultSort: Local.get(localPageParamKey)?.defaultSort || state.localPageParam.defaultSort },
		// Pagination configuration
		pagerConfig: { pageSize: Local.get(localPageParamKey)?.pageSize || state.localPageParam.pageSize },
		// Toolbar configuration
		toolbarConfig: { export: false },
	}
);

// Page initialization
onMounted(async () => {
	 state.localPageParam = Local.get(localPageParamKey) || state.localPageParam;
	// await handleQuery();
});

// Query API
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
	const params = Object.assign(state.queryParams, { page: page.currentPage, pageSize: page.pageSize, field: sort.field, order: sort.order, descStr: 'desc' }) as PageLogOpInput;
	return getAPI(SysLogOpApi).apiSystemSyslogopPagePost(params);
};

// Query operation
const handleQuery = async () => {
	options.loading = true;
	await xGrid.value?.commitProxy('reload') ;
	options.loading = false;
};

// Reset operation
const resetQuery = async () => {
	state.queryParams.startTime = new Date(new Date(now.value.getTime() - 3 * 24 * 60 * 60 * 1000).setHours(0, 0, 0, 0));
	state.queryParams.endTime = new Date(new Date(now.value).setHours(23, 59, 59, 999));
	state.queryParams.displayTitle = '';
	state.queryParams.account = '';
	await xGrid.value?.commitProxy('reload');
};
function handleCustom() {
	xGrid.value?.openCustom();
}
// Table events
const gridEvents: VxeGridListeners<AppOperationLog> = {
	// Only effective for pager-config configuration, this event will be triggered when the pagination changes
	async pageChange({ pageSize }) {
		state.localPageParam.pageSize = pageSize;
		Local.set(localPageParamKey, state.localPageParam);
	},
	// This event will be triggered when the sorting conditions change
	async sortChange({ field, order }) {
		state.localPageParam.defaultSort = { field: field, order: order!, descStr: 'desc' };
		Local.set(localPageParamKey, state.localPageParam);
	},
};

// Clear logs
const handleClear = (row: any) => {
	ElMessageBox.confirm($t('lz.message.base.confirmClearAll'), $t('lz.message.base.warning'), {
		confirmButtonText: $t('lz.button.base.confirm'),
		cancelButtonText: $t('lz.button.base.cancel'),
		type: 'warning',
	})
		.then(async () => {
			options.loading = true;
			await getAPI(SysLogOpApi).apiSystemSyslogopClearPost();
			options.loading = false;
			ElMessage.success($t('lz.message.base.clearSuccess'))
			await handleQuery();
		})
		.catch(() => { });
};


// View details
const handleView = async ({ row }: any) => {
	const { data } = await getAPI(SysLogOpApi).apiSystemSyslogopDetailGet(row.id);
	state.activeTab = 'message';
	state.detail.message = data?.result?.message;
	// If the request parameter is a JSON string, try to convert it to a JSON object
	state.detail.requestParam = StringToObj(data?.result?.requestParam);
	state.detail.returnResult = StringToObj(data?.result?.returnResult);
	state.visible = true;
};
</script>

<style lang="scss" scoped>
pre {
	line-height: 20px;
	white-space: pre-wrap;
	white-space: -moz-pre-wrap;
	white-space: -pre-wrap;
	white-space: -o-pre-wrap;
	word-wrap: break-word;
}
</style>
