<template>
	<div class="sys-account-container">
		<el-card shadow="hover" :body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', height: '100%', alignItems: 'start' }">
					<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true" label-width="auto" style="flex: 1 1 0%" label-position="left" >
						<el-row :gutter="10">
							<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
								<el-form-item :label="$t('lz.entity.base.username')" prop="username">
									<el-input v-model="state.queryParams.username" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.username')})" clearable @keyup.enter.native="handleQuery" />
								</el-form-item>
							</el-col>
							<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
								<el-form-item :label="$t('lz.entity.base.name')" prop="name">
									<el-input v-model="state.queryParams.name" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.name')})" clearable @keyup.enter.native="handleQuery" />
								</el-form-item>
							</el-col>
						</el-row>
					</el-form>

					<el-divider style="height: calc(100% - 5px); margin: 0 10px" direction="vertical" />

					<el-row>
						<SearchButton :auth-search="'sysAccount:page'" @search="handleQuery" @reset="resetQuery" :loading="options.loading" />
					</el-row>
				</el-card>

				<el-card class="full-table" shadow="hover" style="margin-top: 5px; flex: 1">
					<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
						<template #toolbar_buttons>
						</template>
						<template #toolbar_tools>
							<TableHeader @add="handleAdd" :show-delete="false" :auth-add="'sysAccount:add'"
								:auth-delete="'sysAccount:delete'" @refresh="handleQuery" @custom-column="handleCustom"
								@delete="">
							</TableHeader>
						</template>
						<template #empty> 
							<el-empty :image-size="200" />
						</template>
						<!-- // Xử lý roleName có thể là mảng hoặc chuỗi để đảm bảo hiển thị đúng và dễ đọc -->
						<template #row_roleName = "{row}">  
							<span>
     							{{ 
									Array.isArray(row.roleName) 
									? row.roleName.join(', ') 
									: row.roleName?.split(',').map(item => item.trim()).join(', ') 
        						}}
   							 </span>
						</template>
						<template #row_avatar="{ row }">
							<el-avatar :src="state.baseUrl+row.avatar" size="small"> {{ row.username?.slice(0, 1) ?? row.realName?.slice(0, 1) }} </el-avatar>
						</template>
						
						<template #row_accountType="{ row }">
							<TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.SysAccountType, row.accountType)" />
						</template>
						<template #row_status="{ row }">
							<el-switch v-model="row.status" :active-value="1" :inactive-value="0" size="small" @change="changeStatus(row)" v-auth="'sysAccount:setStatus'"/>
						</template>
						<template #row_record="{ row }">
							<ModifyRecord :data="row" />
						</template>
						<template #row_buttons="{ row }">
							<el-tooltip :content="$t('lz.button.base.copy')" placement="top" :auto-close="1000">
								<el-button icon="ele-CopyDocument" text type="warning" v-auth="'sysAccount:add'"
									@click="handleCopy(row)"> </el-button>
							</el-tooltip>
							<el-tooltip :content="$t('lz.button.base.edit')" placement="top" :auto-close="1000">
								<el-button icon="ele-Edit" text type="primary" v-auth="'sysAccount:update'" @click="handleEdit(row)"> </el-button>
							</el-tooltip>
							<el-tooltip :content="$t('lz.button.base.delete')" placement="top" :auto-close="1000">
								<el-button icon="ele-Delete" text type="danger" v-auth="'sysAccount:delete'" @click="handleDelete(row)"> </el-button>
							</el-tooltip>
							<el-tooltip :content="$t('lz.button.sysAccount.resetPassword')" placement="top">
								<el-button icon="ele-RefreshLeft" text type="danger" v-auth="'sysAccount:resetPwd'" @click="resetQueryPwd(row)"> </el-button>
							</el-tooltip>
							<el-tooltip :content="$t('lz.button.sysAccount.unlockLogin')" placement="top">
								<el-button icon="ele-Unlock" text type="primary" v-auth="'sysAccount:unlockLogin'" @click="handleUnlock(row)"> </el-button>
							</el-tooltip>
						</template>
					</vxe-grid>
				</el-card>

		<EditAccount ref="editAccountRef" :title="state.title" :operate-type="state.operateType" @handleQuery="handleQuery" />
		<Setup2FA ref="setup2FARef" :user-data="state.selectedUser" @success="handleQuery" />
	</div>
</template>

<script lang="ts" setup name="sysAccount">
import { onMounted, reactive, ref, onActivated, defineAsyncComponent } from 'vue';
import { ElMessageBox, ElMessage } from 'element-plus';
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import {OperateType} from '/@/types/constants/operate';
import EditAccount from '/@/views/system/account/component/editAccount.vue';
import ModifyRecord from '/@/components/table/modifyRecord.vue';

import { getAPI } from '/@/utils/axios-utils';
import { SysAccountApi,  PageAccountInput, SysUserApi } from '/@/api-services/system';
import { useItsMediaServerStore } from '/@/stores/its/itsMediaServer';
import { AccountOutput } from '/@/api-services/system';
import {useI18n} from 'vue-i18n';
const {t:$t} = useI18n();	

import { BaseConfigTypeEnum } from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';
const getConfigItemByCode = useBaseConfig().getConfigItemByCode;

const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));
const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));
const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));
const xGrid = ref<VxeGridInstance>();
const editAccountRef = ref<InstanceType<typeof EditAccount>>();
const setup2FARef = ref<InstanceType<typeof Setup2FA>>();

const state = reactive({
	queryParams: {
		orgId: "0",
		username: undefined,
		name: undefined,
		posName: undefined,
		phone: undefined,
	},
	selectedUser: null as any,
	// localPageParam: {
	// 	pageSize: 50 as number,
	// 	defaultSort: { field: 'orderNo', order: 'asc', descStr: 'desc' },
	// },
	
	visible: false,
	title: '',
	operateType: 'add' as OperateType,
	baseUrl: '' as string,
});

// Table parameter configuration
const options = useVxeTable<AccountOutput>(
	{
		id: 'sysAccount',
		name: 'Account',
		columns: [
			{ type: 'seq', title: $t('lz.label.base.idx'), width: 60, fixed: 'left' },
			{ field: 'avatar', title: $t('lz.entity.base.avatar'), minWidth: 30, showOverflow: 'tooltip', slots: { default: 'row_avatar' } },
			{ field: 'username', title: $t('lz.entity.base.username'), minWidth: 120, sortable: true, showOverflow: 'tooltip' },
			{ field: 'name', title: $t('lz.entity.base.name'), minWidth: 120, sortable: true, showOverflow: 'tooltip' },
			{ field: 'accountType', title: $t('lz.entity.sysAccount.accountType'), minWidth: 100, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_accountType' } },
			{ field: 'roleName', title: $t('lz.label.system.role'), minWidth: 200, sortable: true,showOverflow: 'tooltip', slots: { default: 'row_roleName' } },
			{ field: 'status', title: $t('lz.entity.base.status'), width: 110, showOverflow: 'tooltip', slots: { default: 'row_status' } },
			{ field: '', title: $t('lz.label.base.audit'), width: 100, showOverflow: 'tooltip', slots: { default: 'row_record' } },
			{ title: $t('lz.label.base.action'), fixed: 'right', width: 200, showOverflow: true, slots: { default: 'row_buttons' } },
		],
	},
	// vxeGrid configuration parameters (can override any parameters here), refer to vxe-table official documentation
	{
		rowConfig: { keyField: 'id' },
		// Proxy configuration
		proxyConfig: { autoLoad: true, ajax: { query: ({ page, sort }) => handleQueryApi(page, sort) } },
		// Toolbar configuration
		toolbarConfig: { export: false },
		
	}
);

// Page initialization
onMounted(async () => {
	// fetchOrgData();
	// Get file server base url
	// Địa chỉ file server dùng chung (chỉ gọi API 1 lần cho toàn app)
	state.baseUrl = await useItsMediaServerStore().ensureLoaded();

});

// Organization structure node switch
onActivated(async () => {
	// await fetchOrgData();
});

// Query API
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
	const params = Object.assign(state.queryParams, { page: page.currentPage, pageSize: page.pageSize, field: sort.field, order: sort.order, descStr: 'desc' }) as PageAccountInput;
	return getAPI(SysAccountApi).apiSystemSysaccountPagePost(params);
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
	state.queryParams.name = undefined;
	state.queryParams.posName = undefined;
	state.queryParams.phone = undefined;
	await xGrid.value?.commitProxy('reload');
};

// Query organization data
// const fetchOrgData = async () => {
// 	var res = await getAPI(SysOrgApi).apiSystemSysorgListGet("0");
// 	state.treeData = res.data.result ?? [];
// };

// Open add page
const handleAdd = () => {
	state.title = $t('lz.label.base.titleAdd',{name: $t('lz.entity.base.account')});
	state.operateType = 'add';

	editAccountRef.value?.openDialog({ id: undefined, birthday: '2000-01-01', sex: 1, orderNo: 100, cardType: 0, cultureLevel: 5 });
};

// Open copy dialog
const handleCopy = (row: any) => {
	state.title = $t('lz.label.base.titleCopy',{name: $t('lz.entity.base.account')});
	state.operateType = 'copy';
	editAccountRef.value?.openDialog(row);
};
// Open edit page
const handleEdit = (row: any) => {
	state.title = $t('lz.label.base.titleEdit',{name: $t('lz.entity.base.account')});
	state.operateType = 'edit';
	editAccountRef.value?.openDialog(row);
};

// Delete
const handleDelete = (row: any) => {
	ElMessageBox.confirm(`${$t('lz.message.base.confirmDelete',{name: row.username})}`, $t('lz.message.base.warning'), {
		confirmButtonText: $t('lz.button.base.confirm'),
		cancelButtonText: $t('lz.button.base.cancel'),
		type: 'warning',
	})
		.then(async () => {
			await getAPI(SysUserApi).apiSystemSysuserDeletePost({ id: row.id });
			await handleQuery();
			ElMessage.success($t('lz.message.base.deleteSuccess'));
		})
		.catch(() => {});
};
// Custom column
function handleCustom() {
	xGrid.value?.openCustom();
}

// Change status
const changeStatus = (row: any) => {
	getAPI(SysUserApi)
		.apiSystemSysuserSetstatusPost({ id: row.id, status: row.status })
		.then(() => {
			ElMessage.success($t('lz.message.base.processSuccess'));
		})
		.catch(() => {
			row.status = row.status == 1 ? 0 : 1;
		});
};

// Reset password
const resetQueryPwd = async (row: any) => {
	ElMessageBox.confirm($t('lz.message.sysAccount.confirmResetPwd',{username: row.username}), $t('lz.message.base.warning'), {
		confirmButtonText: $t('lz.button.base.confirm'),
		cancelButtonText: $t('lz.button.base.cancel'),
		type: 'warning',
	})
		.then(async () => {
			await getAPI(SysAccountApi)
				.apiSystemSysaccountResetpwdPost({ id: row.id })
				.then((res) => {
					ElMessage.success($t('lz.message.sysAccount.resetPwdSuccess',{result: res.data.result}));
				});
		})
		.catch(() => {});
};

// Unlock login
const handleUnlock = async (row: any) => {
	ElMessageBox.confirm($t('lz.message.sysAccount.confirmUnlockLogin',{username: row.username}), $t('lz.message.base.warning'), {
		confirmButtonText: $t('lz.button.base.confirm'),
		cancelButtonText: $t('lz.button.base.cancel'),
		type: 'warning',
	})
		.then(async () => {
			await getAPI(SysAccountApi)
				.apiSystemSysaccountUnlockloginPost({ id: row.id })
				.then(() => {
					ElMessage.success($t('lz.message.sysAccount.unlockLoginSuccess'));
				});
		})
		.catch(() => {});
};

// Table events
const gridEvents: VxeGridListeners<AccountOutput> = {
	// Only effective when pager-config is configured, this event will be triggered when pagination changes
	
};

</script>
