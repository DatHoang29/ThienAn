<template>
	<div class="sys-difflog-container">
		<el-card shadow="hover" :body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', height: '100%', alignItems: 'start' }">
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true" label-width="auto" style="flex: 1 1 0%">
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.startTime')" prop="name">
							<el-date-picker v-model="state.queryParams.startTime" type="datetime" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.startTime')})" :shortcuts="shortcuts" :clearable="false"/>
						</el-form-item>
					</el-col>
					
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.log.businessData')" prop="code">
							<el-input v-model="state.queryParams.businessData" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.log.businessData')})" :shortcuts="shortcuts" />
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.log.diffType')" prop="type">
							<el-select v-model="state.queryParams.diffType" :placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.entity.log.diffType')})" clearable @clear="state.queryParams.diffType = undefined">
								<el-option label="Add" value="add" />
								<el-option label="Update" value="update" />
								<el-option label="Delete" value="delete" />
							</el-select>
						</el-form-item>
					</el-col>
				</el-row>
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.endTime')" prop="code">
							<el-date-picker v-model="state.queryParams.endTime" type="datetime" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.endTime')})" :shortcuts="shortcuts"  :clearable="false"/>
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
				<SearchButton :loading="options.loading" @reset="resetQuery" @search="handleQuery" :auth-search="'sysDiffLog:page'"></SearchButton>
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
							<el-button icon="ele-DeleteFilled" type="danger" @click="handleClear" v-auth="'sysDiffLog:clear'"> {{ $t('lz.button.base.clear') }} </el-button>
						</template>
					</TableHeader>
				</template>
				<template #empty>
					<el-empty :image-size="200" />
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
			<el-scrollbar height="calc(100vh - 250px)">
				<h3>{{ $t('lz.label.log.logTime') }}</h3>
				 <div class="mt5">{{ useDateFormat(state.detail.createTime,DATE_TIME_FORMAT)?.value }}</div>
				 <el-divider></el-divider>

				<h3>{{ $t('lz.label.log.tableInfo') }}</h3>
				<el-table :data="[state.detail]">
					<el-table-column prop="bussinessData" :label="$t('lz.entity.log.businessData')" />
					<el-table-column prop="diffType" :label="$t('lz.entity.log.diffType')" />
					<el-table-column prop="account" :label="$t('lz.entity.base.account')" />
				</el-table>
				<el-divider></el-divider>

				<h3>{{ $t('lz.label.log.keyReferenceData') }}</h3>
				<el-table :data="state.detail.refId" style="width: 100%">
					<el-table-column prop="key" :label="$t('lz.label.log.key')" >
						<template #default="{ row }">
							<strong>{{ Object.keys(row)[0] }}</strong>
						</template>
					</el-table-column>
					<el-table-column prop="value" :label="$t('lz.label.log.value')">
						<template #default="{ row }">
							{{ row[Object.keys(row)[0]] }}
						</template>
					</el-table-column>
				</el-table>

				<el-divider></el-divider>
				<h3>{{ $t('lz.label.log.dataDifferent') }}</h3>
				<el-table :data="state.detail.list" style="width: 100%">
					<el-table-column prop="key" :label="$t('lz.label.log.detailKey')">
						<template #default="{ row }">
							<strong>{{ row.key }}</strong>
						</template>
					</el-table-column>
					<el-table-column prop="before" :label="$t('lz.label.log.beforeValue')" />
					<el-table-column prop="after" :label="$t('lz.label.log.afterValue')" />
				</el-table>


				
			</el-scrollbar>
		</el-dialog>
	</div>
</template>

<script lang="ts" setup name="sysDiffLog">
import { onMounted, reactive, ref,defineAsyncComponent } from 'vue';
import { ElMessage } from 'element-plus';
import { useDateTimeShortCust } from '/@/hooks/dateTimeShortCust';
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { Local } from '/@/utils/storage';
import { getAPI } from '/@/utils/axios-utils';
import { SysLogDiffApi } from '/@/api-services/system';
import { PageLogDiffInput, AppDiffLog } from '/@/api-services/system';
import {ElMessageBox} from 'element-plus';
import { useI18n } from 'vue-i18n';
import { useNow, useDateFormat } from '@vueuse/core'; // Import useNow
import { DATE_TIME_FORMAT } from '/@/types/formats';
const { t: $t } = useI18n();
const now = useNow(); // Get the current date and time

const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));
const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));

const xGrid = ref<VxeGridInstance>();
const shortcuts = useDateTimeShortCust().map(item => ({ text: $t(item.text), value: item.value }));

const state = reactive({
	queryParams: {
		startTime: new Date(new Date(now.value.getTime() - 3 * 24 * 60 * 60 * 1000).setHours(0, 0, 0, 0)), // Set to 0:00 three days ago
		endTime: new Date(new Date(now.value).setHours(23, 59, 59, 999)), // Set to today at 23:59:59
		diffType: undefined,
		businessData: undefined,
		account: undefined,

	},
	localPageParam: {
		pageSize: 50 as number,
		defaultSort: { order: 'desc', descStr: 'desc' },
	},
	visible: false,
	detail: {
		list: [] as any,
		refId: [] as any,
		bussinessData: undefined,
		diffType: undefined,
		account: undefined,
		createTime: undefined,


	},
});

// Local storage parameters
const localPageParamKey = 'localPageParam:sysDiffLog';
// Table parameter configuration
const options = useVxeTable<AppDiffLog>(
	{
		id: 'sysDiffLog',
		name: 'Difference Log',
		columns: [
			{ type: 'seq', title: $t('lz.label.base.idx'), width: 60, fixed: 'left' },
			{ field: 'createTime', title: $t('lz.entity.log.logTime'), minWidth: 130, fixed: 'left', showOverflow: 'tooltip',formatter: ({ cellValue }: any) => useDateFormat(cellValue,DATE_TIME_FORMAT)?.value  },
			{ field: 'businessData', title: $t('lz.entity.log.businessData'), minWidth: 150, sortable: true, showOverflow: 'tooltip' },
			{ field: 'diffType', title: $t('lz.entity.log.diffType'), minWidth: 150, sortable: true, showOverflow: 'tooltip' },
			{ field: 'refID', title: $t('lz.label.log.refId'), minWidth: 150, showOverflow: 'tooltip' },
			{ field: 'beforeData', title: $t('lz.label.log.beforeValue'), minWidth: 150, showOverflow: 'tooltip' },
			{ field: 'afterData', title: $t('lz.label.log.afterValue'), minWidth: 150, showOverflow: 'tooltip' },
			{ field: 'account', title: $t('lz.entity.base.account'), minWidth: 150, sortable: true, showOverflow: 'tooltip' },
			{ field: 'elapsed', title: $t('lz.entity.log.elapsedTime'), minWidth: 150, sortable: true, showOverflow: 'tooltip' },
			{ field: 'sql', title: $t('lz.label.log.sql'), minWidth: 150, showOverflow: 'tooltip' },
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
	const params = Object.assign(state.queryParams, { page: page.currentPage, pageSize: page.pageSize, field: sort.field, order: sort.order, descStr: 'desc' }) as PageLogDiffInput;
	return getAPI(SysLogDiffApi).apiSystemSyslogdiffPagePost(params);
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
	state.queryParams.diffType = undefined;
	state.queryParams.businessData = undefined;
	state.queryParams.account = undefined;
	await xGrid.value?.commitProxy('reload');
};
function handleCustom() {
	xGrid.value?.openCustom();
}
// Table events
const gridEvents: VxeGridListeners<AppDiffLog> = {
	// Only effective for pager-config configuration, this event will be triggered when pagination changes
	async pageChange({ pageSize }) {
		state.localPageParam.pageSize = pageSize;
		Local.set(localPageParamKey, state.localPageParam);
	},
	// This event will be triggered when sorting conditions change
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
			await getAPI(SysLogDiffApi).apiSystemSyslogdiffClearPost();
			options.loading = false;
			ElMessage.success($t('lz.message.base.clearSuccess'))
			
			await handleQuery();
		})
		.catch(() => { });
};

// View details
const handleView = async ({ row }: any) => {

	  const beforeData = row.beforeData  ? JSON.parse(row.beforeData) : [];
      const afterData = row.afterData ? JSON.parse(row.afterData) : [];

      const keys = new Set([...beforeData.map((item: {}) => Object.keys(item)[0]), ...afterData.map((item: {}) => Object.keys(item)[0])]);

      state.detail.list = Array.from(keys).map(key => {
        const beforeItem = beforeData.find((item: { [x: string]: undefined; }) => item[key] !== undefined) || {};
        const afterItem = afterData.find((item: { [x: string]: undefined; }) => item[key] !== undefined) || {};
        return {
          key: key,
          before: beforeItem[key],
          after: afterItem[key]
        };
      });

	  const refId = row.afterData ? JSON.parse(row.refID) : [];
	  state.detail.refId = refId;

	  state.detail.bussinessData = row.businessData;
	  state.detail.diffType = row.diffType;	
	  state.detail.account = row.account;
	  state.detail.createTime = row.createTime;


	state.visible = true;
};


</script>
