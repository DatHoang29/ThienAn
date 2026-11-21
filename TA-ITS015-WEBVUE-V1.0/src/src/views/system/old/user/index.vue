<template>
	<div class="sys-user-container">
		<splitpanes class="default-theme">
			<pane size="15" style="display: flex">
				<OrgTree ref="orgTreeRef" @node-click="handleNodeChange" />
			</pane>
			<pane size="85" style="display: flex; flex-direction: column">
				<el-card shadow="hover" :body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', height: '100%', alignItems: 'start' }">
					<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true" label-width="auto" style="flex: 1 1 0%" label-position="left">
						<el-row :gutter="10">
							<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
								<el-form-item :label="$t('lz.entity.base.username')" prop="username">
									<el-input v-model="state.queryParams.username" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.username')})" clearable @keyup.enter.native="handleQuery" />
								</el-form-item>
							</el-col>
							<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
								<el-form-item :label="$t('lz.entity.base.name')" prop="realName">
									<el-input v-model="state.queryParams.realName" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.name')})" clearable @keyup.enter.native="handleQuery" />
								</el-form-item>
							</el-col>
							<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
								<el-form-item :label="$t('lz.entity.base.phone')" prop="phone">
									<el-input v-model="state.queryParams.phone" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.phone')})" clearable @keyup.enter.native="handleQuery" />
								</el-form-item>
							</el-col>
						</el-row>
					</el-form>

					<el-divider style="height: calc(100% - 5px); margin: 0 10px" direction="vertical" />

					<el-row>
						<SearchButton :auth-search="'sysUser:page'" @search="handleQuery" @reset="resetQuery" :loading="options.loading" />
					</el-row>
				</el-card>

				<el-card class="full-table" shadow="hover" style="margin-top: 5px; flex: 1">
					<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
						<template #toolbar_buttons>
						</template>
						<template #toolbar_tools>
							<TableHeader @add="handleAdd" :show-delete="false" :auth-add="'sysUser:add'"
								:auth-delete="'sysUser:delete'" @refresh="handleQuery" @custom-column="handleCustom"
								@delete="">
							</TableHeader>
						</template>
						<template #empty>
							<el-empty :image-size="200" />
						</template>
						<template #row_avatar="{ row }">
							<el-avatar :src="state.baseUrl+row.avatar" size="small"> {{ row.username?.slice(0, 1) ?? row.realName?.slice(0, 1) }} </el-avatar>
						</template>
						
						<template #row_status="{ row }">
							<el-switch v-model="row.status" :active-value="1" :inactive-value="0" size="small" @change="changeStatus(row)" v-auth="'sysUser:setStatus'" />
						</template>
						<template #row_record="{ row }">
							<ModifyRecord :data="row" />
						</template>
						<template #row_buttons="{ row }">
							<el-tooltip :content="$t('lz.button.base.copy')" placement="top" :auto-close="1000">
								<el-button icon="ele-CopyDocument" text type="warning" v-auth="'sysUser:add'"
									@click="handleCopy(row)"> </el-button>
							</el-tooltip>
							<el-tooltip :content="$t('lz.button.base.edit')" placement="top" :auto-close="1000">
								<el-button icon="ele-Edit" text type="primary" v-auth="'sysUser:update'" @click="handleEdit(row)"> </el-button>
							</el-tooltip>
							<el-tooltip :content="$t('lz.button.base.delete')" placement="top" :auto-close="1000">
								<el-button icon="ele-Delete" text type="danger" v-auth="'sysUser:delete'" @click="handleDelete(row)"> </el-button>
							</el-tooltip>
						</template>
					</vxe-grid>
				</el-card>
			</pane>
		</splitpanes>

		<EditUser ref="editUserRef" :title="state.title" :operate-type="state.operateType" :orgData="state.treeData" @handleQuery="handleQuery" />
	</div>
</template>

<script lang="ts" setup name="sysUser">
import { onMounted, reactive, ref, onActivated, defineAsyncComponent } from 'vue';
import { ElMessageBox, ElMessage } from 'element-plus';
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { Splitpanes, Pane } from 'splitpanes';
import 'splitpanes/dist/splitpanes.css';
import {OperateType} from '/@/types/constants/operate';
import OrgTree from '/@/views/system/org/component/orgTree.vue';
import EditUser from '/@/views/system/user/component/editUser.vue';
import ModifyRecord from '/@/components/table/modifyRecord.vue';

import { getAPI } from '/@/utils/axios-utils';
import { SysUserApi, SysOrgApi, PageUserInput } from '/@/api-services/system';
import { useItsMediaServerStore } from '/@/stores/its/itsMediaServer';
import { SysOrg, UserOutput } from '/@/api-services/system';

import { useI18n } from 'vue-i18n';
const {t:$t} = useI18n();
const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));
const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));

const xGrid = ref<VxeGridInstance>();
const treeRef = ref<InstanceType<typeof OrgTree>>();
const editUserRef = ref<InstanceType<typeof EditUser>>();
const state = reactive({
	treeData: [] as Array<SysOrg>,
	queryParams: {
		orgId: "0",
		username: undefined,
		realName: undefined,
		posName: undefined,
		phone: undefined,
	},
	localPageParam: {
		pageSize: 50 as number,
		defaultSort: { field: '', order: 'desc', descStr: 'desc' } as any,
	},
	visible: false,
	title: '',
	operateType: 'add' as OperateType,
	baseUrl: '' as string,
});

// Table parameter configuration
const options = useVxeTable<UserOutput>(
	{
		id: 'sysUser',
		name: 'Account',
		columns: [
			{ type: 'seq', title: $t('lz.label.base.idx'), width: 60, fixed: 'left' },
			{ field: 'avatar', title: $t('lz.entity.base.avatar'), minWidth: 100, showOverflow: 'tooltip', slots: { default: 'row_avatar' } },
			{ field: 'username', title: $t('lz.entity.base.username'), minWidth: 120, sortable: true, showOverflow: 'tooltip' },
			{ field: 'name', title: $t('lz.entity.base.name'), minWidth: 120, sortable: true, showOverflow: 'tooltip' },
			{ field: 'realName', title: $t('lz.entity.base.realName'), minWidth: 120, sortable: true, showOverflow: 'tooltip' },
			{ field: 'phone', title: $t('lz.entity.base.phone'), sortable: true, minWidth: 120, showOverflow: 'tooltip' },
			{ field: 'email', title: $t('lz.entity.base.email'), minWidth: 120, showOverflow: 'tooltip' },
			{ field: 'orgName', title: $t('lz.entity.system.org'), minWidth: 120, sortable: true, showOverflow: 'tooltip' },
			{ field: 'posName', title: $t('lz.entity.system.pos'), minWidth: 120,sortable: true, showOverflow: 'tooltip' },
			{ field: 'status', title: $t('lz.entity.base.status'), width: 110, showOverflow: 'tooltip', slots: { default: 'row_status' } },
			{ field: '', title: $t('lz.label.base.audit'), width: 100, showOverflow: 'tooltip', slots: { default: 'row_record' } },
			{ title: $t('lz.label.base.action'), fixed: 'right', width: 100, showOverflow: true, slots: { default: 'row_buttons' } },
		],
	},
	// vxeGrid configuration parameters (can override any parameters here), refer to vxe-table official documentation
	{
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
	fetchOrgData();
	// Get file server base url
	// Địa chỉ file server dùng chung (chỉ gọi API 1 lần cho toàn app)
	state.baseUrl = await useItsMediaServerStore().ensureLoaded();
});

// Organization structure node switch
onActivated(async () => {
	await fetchOrgData();
});

// Query API
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
	const params = Object.assign(state.queryParams, { page: page.currentPage, pageSize: page.pageSize, field: sort.field, order: sort.order, descStr: 'desc' }) as PageUserInput;
	return getAPI(SysUserApi).apiSystemSysuserPagePost(params);
};

// Query operation
const handleQuery = async () => {
	options.loading = true;
	await xGrid.value?.commitProxy('reload') ;
	options.loading = false;
};

// Reset operation
const resetQuery = async () => {
	state.queryParams.orgId = "0";
	state.queryParams.username = undefined;
	state.queryParams.realName = undefined;
	state.queryParams.posName = undefined;
	state.queryParams.phone = undefined;
	await xGrid.value?.commitProxy('reload');
};

// Query organization data
const fetchOrgData = async () => {
	var res = await getAPI(SysOrgApi).apiSystemSysorgListGet("0");
	state.treeData = res.data.result ?? [];
};

// Open add page
const handleAdd = () => {
	state.title = $t('lz.label.base.titleAdd', { name: $t('lz.label.system.user') });
	state.operateType = 'add';
	editUserRef.value?.openDialog({ id: undefined, birthday: '01-01-2000', gender: 'unknown', orderNo: 100, cardType: 0 });
};

// Open copy dialog
const handleCopy = (row: any) => {
	state.title = $t('lz.label.base.titleCopy', { name: $t('lz.label.system.user') });
	state.operateType = 'copy';
	editUserRef.value?.openDialog(row);
};
// Open edit page
const handleEdit = (row: any) => {
	state.title = $t('lz.label.base.titleEdit', { name: $t('lz.label.system.user') });
	state.operateType = 'edit';
	editUserRef.value?.openDialog(row);
};

// Delete
const handleDelete = (row: any) => {
	ElMessageBox.confirm($t('lz.message.base.confirmDelete', { name: row.name }), $t('lz.message.base.warning'), {
		confirmButtonText: $t('lz.button.base.confirm'),
		cancelButtonText: $t('lz.button.base.cancel'),
		type: 'warning',
	})
		.then(async () => {
			await getAPI(SysUserApi).apiSystemSysuserDeletePost({ id: row.id });
			handleQuery();
			ElMessage.success($t('lz.message.base.deleteSuccess'));
		})
		.catch(() => { });
};


// Custom column
function handleCustom() {
	xGrid.value?.openCustom();
}
// Table events
const gridEvents: VxeGridListeners<UserOutput> = {
	// Only effective when pager-config is configured, this event will be triggered when pagination changes
	async pageChange({ pageSize }) {
		state.localPageParam.pageSize = pageSize;
	},
	// This event will be triggered when sorting conditions change
	async sortChange({ field, order }) {
		state.localPageParam.defaultSort = { field: field, order: order!, descStr: 'desc' };
	},
};

// Change status
const changeStatus = (row: any) => {
	getAPI(SysUserApi)
		.apiSystemSysuserSetstatusPost({ id: row.id, status: row.status })
		.then(() => {
			ElMessage.success($t('lz.message.base.actionSuccess'));
		})
		.catch(() => {
			row.status = row.status == 1 ? 0 : 1;
		});
};

// Tree component click
const handleNodeChange = async (node: any) => {
	state.queryParams.orgId = node.id +"";
	state.queryParams.username = undefined;
	state.queryParams.realName = undefined;
	state.queryParams.phone = undefined;
	await handleQuery();
};
</script>
