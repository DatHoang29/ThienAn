<template>
	<WorkspacePage>
	<div class="sys-user-container">
		<GridSidePanel id="sys-user-mngt-users-org" :label="$t('lz.entity.system.org')">
			<template #side>
				<OrgTree ref="orgTreeRef" class="workspace-tree" @node-click="handleNodeChange" />
			</template>
				<GridSearchPanel id="sys-user-mngt-users-search">
				<template #filters>
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
				</template>

				<el-card class="full-table" shadow="hover">
					<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
						<template #toolbar_buttons>
							<GridSummary :summary="summary" />
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
							<el-avatar :src="row.avatar ? state.baseUrl + row.avatar : undefined" size="small"> {{ row.username?.slice(0, 1) ?? row.realName?.slice(0, 1) }} </el-avatar>
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
				</GridSearchPanel>
		</GridSidePanel>

		<EditUser ref="editUserRef" :title="state.title" :operate-type="state.operateType" :orgData="state.treeData" @handleQuery="handleQuery" />
	</div>
	</WorkspacePage>
</template>

<script lang="ts" setup name="sysUser">
import { onMounted, reactive, ref, onActivated, defineAsyncComponent } from 'vue';
import { ElMessageBox, ElMessage } from 'element-plus';
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import {OperateType} from '/@/types/constants/operate';
import OrgTree from '/@/views/system/org/component/orgTree.vue';
import EditUser from './component/editUser.vue';
import ModifyRecord from '/@/components/table/modifyRecord.vue';
import WorkspacePage from '/@/components/workspace/WorkspacePage.vue';
import GridSearchPanel from '/@/components/gridPage/GridSearchPanel.vue';
import GridSidePanel from '/@/components/gridPage/GridSidePanel.vue';
import GridSummary from '/@/components/gridPage/GridSummary.vue';
import { useGridSummary } from '/@/components/gridPage/useGridSummary';
import { useGridPager } from '/@/components/gridPage/useGridPager';

import { getAPI } from '/@/utils/axios-utils';
import { SysUserApi, PageUserInput } from '/@/api-services/system';
import { useItsMediaServerStore } from '/@/stores/its/itsMediaServer';
import { SysOrg, UserOutput } from '/@/api-services/system';

import { useI18n } from 'vue-i18n';
const {t:$t} = useI18n();
const { summary, track } = useGridSummary();
const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));
const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));

const xGrid = ref<VxeGridInstance>();
const orgTreeRef = ref<InstanceType<typeof OrgTree>>();
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
	// Giá trị ban đầu của bảng (sortConfig/pagerConfig).
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
		// ID riêng để không dùng chung cấu hình cột với trang gốc system/user.
		id: 'sysUserMngtUsers',
		name: 'User',
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
// Thu gọn thanh phân trang theo chiều rộng bảng.
useGridPager(xGrid, options);

// Page initialization
// Cây đơn vị (OrgTree) tự tải khi mount; danh sách cho dialog lấy khi mở dialog lần đầu (fetchOrgData).
onMounted(async () => {
	// Get file server base url
	// Địa chỉ file server dùng chung (chỉ gọi API 1 lần cho toàn app)
	state.baseUrl = await useItsMediaServerStore().ensureLoaded();
});

// Quay lại tab/menu (KeepAlive): tải lại cây đơn vị và trang hiện tại của bảng để nhận thay đổi từ nơi khác.
// onActivated cũng chạy ngay sau lần mount đầu, khi đó dữ liệu vừa tải nên bỏ qua.
let activated = false;
onActivated(async () => {
	if (!activated) {
		activated = true;
		return;
	}
	await Promise.all([fetchOrgData(), xGrid.value?.commitProxy('query')]);
});

// Query API
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
	// Tạo object mới: không ghi page/sort vào bộ lọc.
	const params = { ...state.queryParams, page: page.currentPage, pageSize: page.pageSize, field: sort.field, order: sort.order, descStr: 'desc' } as PageUserInput;
	return track(getAPI(SysUserApi).apiSystemSysuserPagePost(params));
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
	orgTreeRef.value?.setCurrentKey();
	await xGrid.value?.commitProxy('reload');
};

// Query organization data
// Tải qua OrgTree: một lần gọi API cập nhật cả cây bên trái lẫn danh sách đơn vị của dialog.
const fetchOrgData = async () => {
	state.treeData = (await orgTreeRef.value?.fetchTreeData(false)) ?? [];
};

// Dialog cần danh sách đơn vị; chỉ tải khi chưa có.
const openEditDialog = async (row: any) => {
	if (!state.treeData.length) await fetchOrgData();
	await editUserRef.value?.openDialog(row);
};

// Open add page
const handleAdd = () => {
	state.title = $t('lz.label.base.titleAdd', { name: $t('lz.label.system.user') });
	state.operateType = 'add';
	openEditDialog({ id: undefined, birthday: '01-01-2000', gender: 'unknown', orderNo: 100, cardType: 0 });
};

// Open copy dialog
const handleCopy = (row: any) => {
	state.title = $t('lz.label.base.titleCopy', { name: $t('lz.label.system.user') });
	state.operateType = 'copy';
	openEditDialog(row);
};
// Open edit page
const handleEdit = (row: any) => {
	state.title = $t('lz.label.base.titleEdit', { name: $t('lz.label.system.user') });
	state.operateType = 'edit';
	openEditDialog(row);
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
			await handleQuery();
			ElMessage.success($t('lz.message.base.deleteSuccess'));
		})
		.catch(() => { });
};


// Custom column
function handleCustom() {
	xGrid.value?.openCustom();
}
// Table events
// Trang gốc lưu pageSize/sort vào localPageParam nhưng không đọc lại; vxe-grid tự giữ trạng thái này.
const gridEvents: VxeGridListeners<UserOutput> = {};

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
