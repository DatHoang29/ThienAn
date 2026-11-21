<template>
	<div class="sys-acslog-container">
		<el-card shadow="hover" :body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', height: '100%', alignItems: 'start' }">
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true" label-width="auto" style="flex: 1 1 0%">
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.startTime')" prop="name">
							<el-date-picker v-model="state.queryParams.startTime" type="datetime" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.startTime')})" :shortcuts="shortcuts"  :clearable="false"/>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.log.logType')" prop="type">
							<el-select v-model="state.queryParams.logType" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.log.logType')})" clearable @clear="state.queryParams.logType = undefined">
								<el-option label="Login" value="login" />
								<el-option label="Logout" value="logout" />
							</el-select>
						</el-form-item>
					</el-col>
					
				</el-row>
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.endTime')" prop="code">
							<el-date-picker v-model="state.queryParams.endTime" type="datetime" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.endTime')})" :shortcuts="shortcuts" :clearable="false"/>
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
				<SearchButton :loading="options.loading" @reset="resetQuery" @search="handleQuery" :auth-search="'sysAcsLog:page'"></SearchButton>
			</el-row>
		</el-card>

		<el-card class="full-table" shadow="hover" style="margin-top: 5px">
			<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
				<template #toolbar_buttons>
				</template>
				<template #toolbar_tools>
					<TableHeader @refresh="handleQuery" @custom-column="handleCustom"
						@delete="" >
						<template #default>
							<el-button icon="ele-DeleteFilled" type="danger" @click="handleClear" v-auth="'sysAcsLog:clear'"> {{ $t('lz.button.base.clear') }} </el-button>
						</template>
					</TableHeader>
				</template>
				
				<template #empty>
					<el-empty :image-size="200" />
				</template>
				<template #row_status="{ row }">
					<el-tag v-if="row.status === '200'" type="success">{{row.status}}</el-tag>
					<el-tag v-else type="danger">{{row.status}}</el-tag>
				</template>
			</vxe-grid>
		</el-card>

	</div>
</template>

<script lang="ts" setup name="sysAcsLog">
import { onMounted, reactive, ref, defineAsyncComponent } from 'vue';
import { ElMessage } from 'element-plus';
import { useDateTimeShortCust } from '/@/hooks/dateTimeShortCust';
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { Local } from '/@/utils/storage';

import { getAPI } from '/@/utils/axios-utils';
import { SysLogAcsApi } from '/@/api-services/system';
import { AppAccessLog, PageLogAcsInput } from '/@/api-services/system';
import {ElMessageBox} from 'element-plus';
import { useI18n } from 'vue-i18n';
import { useNow, useDateFormat } from '@vueuse/core'; // Import useNow
import { DATE_TIME_FORMAT } from '/@/types/formats'
const { t: $t } = useI18n();

const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));
const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));

const xGrid = ref<VxeGridInstance>();
const shortcuts = useDateTimeShortCust().map(item => ({ text: $t(item.text), value: item.value }));
const now = useNow(); // Get the current date and time

const state = reactive({
	queryParams: {
		startTime: new Date(new Date(now.value.getTime() - 3 * 24 * 60 * 60 * 1000).setHours(0, 0, 0, 0)), // Set to 0:00 three days ago
		endTime: new Date(new Date(now.value).setHours(23, 59, 59, 999)), // Set to today at 23:59:59
		logType: undefined,
		account: '',
	},
	localPageParam: {
		pageSize: 50 as number,
		defaultSort: { order: 'desc', descStr: 'desc' },
	},
	title: '',
});

// Local storage parameters
const localPageParamKey = 'localPageParam:sysAcsLog';
// Table parameter configuration
const options = useVxeTable<AppAccessLog>(
	{
		id: 'sysVisLog',
		name: 'Access Log',
		columns: [
			{ type: 'seq', title: $t('lz.label.base.idx'), width: 60, fixed: 'left' },
			{ field: 'logDateTime', title: $t('lz.entity.log.logTime'), minWidth: 130, fixed: 'left', showOverflow: 'tooltip',formatter: ({ cellValue }: any) => useDateFormat(cellValue,DATE_TIME_FORMAT)?.value  },
			{ field: 'displayTitle', title:$t('lz.entity.log.displayTitle'), minWidth: 150, sortable: true, showOverflow: 'tooltip' },
			{ field: 'actionName', title: $t('lz.entity.log.actionName'), minWidth: 150, sortable: true, showOverflow: 'tooltip' },
			{ field: 'logType', title: $t('lz.entity.log.logType'), minWidth: 150, sortable: true, showOverflow: 'tooltip' },
			{ field: 'account', title: $t('lz.entity.base.account'), minWidth: 150, sortable: true, showOverflow: 'tooltip' },
			{ field: 'elapsed', title: $t('lz.entity.log.elapsedTime'), minWidth: 100,  showOverflow: 'tooltip' },
			{ field: 'status', title: $t('lz.entity.base.statusCode'), minWidth: 70, showOverflow: 'tooltip', slots: { default: 'row_status' } },
			{ field: 'remoteIp', title: $t('lz.entity.base.ipAddress'), minWidth: 150, showOverflow: 'tooltip' },
			{ field: 'browser', title: $t('lz.entity.base.browser'), minWidth: 150, showOverflow: 'tooltip' },
		],
	},
	// vxeGrid configuration parameters (can override any parameters here), refer to vxe-table official documentation
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
	const params = Object.assign(state.queryParams, { page: page.currentPage, pageSize: page.pageSize, field: sort.field, order: sort.order, descStr: 'desc' }) as PageLogAcsInput;
	return getAPI(SysLogAcsApi).apiSystemSyslogacsPagePost(params);
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
	state.queryParams.logType = undefined;
	state.queryParams.account = '';
	await xGrid.value?.commitProxy('reload');
};
function handleCustom() {
	xGrid.value?.openCustom();
}
// Table events
const gridEvents: VxeGridListeners<AppAccessLog> = {
	// Only effective for pager-config configuration, this event will be triggered when the pagination changes
	async pageChange({ pageSize }) {
		state.localPageParam.pageSize = pageSize;
		Local.set(localPageParamKey, state.localPageParam);
	},
	// This event will be triggered when the sorting conditions change
	async sortChange({ field, order }: any) {
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
			await getAPI(SysLogAcsApi).apiSystemSyslogacsClearPost();
			options.loading = false;
			ElMessage.success($t('lz.message.base.clearSuccess'))
			
			await handleQuery();
		})
		.catch(() => { });
};


</script>
