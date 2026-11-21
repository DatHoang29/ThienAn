<template>
	<WorkspacePage>
	<div class="message-container">
		<GridSearchPanel id="vms-message-search">
				<template #filters>
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true"	label-width="auto" style="flex: 1 1 0%" label-position="left">
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="24" :md="6" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.code')" prop="code">
							<el-input v-model="state.queryParams.code" :placeholder="$t('lz.label.base.pleaseType', {input: $t('lz.entity.base.code')})" @keyup.enter.native="handleQuery" :maxlength="EntityFieldLength.Length64" show-word-limit clearable class="w100"/>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="24" :md="6" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.name')" prop="name">
							<el-input v-model="state.queryParams.name" :placeholder="$t('lz.label.base.pleaseType', {input: $t('lz.entity.base.name')})" @keyup.enter.native="handleQuery" :maxlength="EntityFieldLength.Length64" show-word-limit clearable class="w100"/>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="24" :md="6" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.type')" prop="type">
							<el-select v-model="state.queryParams.type" :placeholder="$t('lz.label.base.pleaseSelect', {input: $t('lz.entity.base.type')})" clearable class="w100">
								<el-option v-for="item in state.messageTypeList" :label="item.name" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="24" :md="6" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.lang')" prop="lang">
							<el-select v-model="state.queryParams.lang" :placeholder="$t('lz.label.base.pleaseSelect', {input: $t('lz.entity.base.lang')})" clearable class="w100">
								<el-option v-for="item in state.languageList" :label="item.name" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="24" :md="12" :lg="12" :xl="12">
						<el-form-item :label="$t('lz.entity.vmsMessage.content1')" prop="content1">
							<el-input v-model="state.queryParams.content1" :placeholder="$t('lz.label.base.pleaseType', {input: $t('lz.entity.vmsMessage.content1')})" @keyup.enter.native="handleQuery" :maxlength="EntityFieldLength.Length256" show-word-limit clearable class="w100"/>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="24" :md="12" :lg="12" :xl="12">
						<el-form-item :label="$t('lz.entity.vmsMessage.content2')" prop="content2">
							<el-input v-model="state.queryParams.content2" :placeholder="$t('lz.label.base.pleaseType', {input: $t('lz.entity.vmsMessage.content2')})" @keyup.enter.native="handleQuery" :maxlength="EntityFieldLength.Length256" show-word-limit clearable class="w100"/>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.status')" prop="status" >
							<el-select v-model="state.queryParams.status" filterable clearable class="w100" @clear="state.queryParams.status = undefined" :placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.entity.base.status')})" >
								<el-option v-for="item in getConfigDataByCode(BaseConfigTypeEnum.SysStatusType)" :key="item.code" :label="item.name" :value="parseInt(item.code)" />
							</el-select>
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>
			<el-divider style="height: calc(100% - 5px); margin: 0 10px" direction="vertical" />
			<el-row>
				<SearchButton authSearch="vmsMessage:list" @search="handleQuery" @reset="resetQuery" :loading="options.loading" />
			</el-row>
				</template>
		<!-- SearchBar: end -->

		<!-- GridTable: begin -->
		<el-card class="full-table" shadow="hover">
			<vxe-grid ref="xGridVmsMessage" class="xGrid-style" v-bind="options" v-on="gridEvents" @checkbox-change="selectChangeEvent" @checkbox-all="selectChangeEvent">
				<template #toolbar_buttons>
					<GridSummary :summary="summary" />
				</template>
				<template #toolbar_tools>
					<TableHeader @add="handleAdd" :show-delete="state.showBatchDelete" :auth-add="'vmsMessage:add'" :auth-delete="'vmsMessage:delete'" @refresh="handleQuery" @custom-column="handleCustom" @delete="handleBatchDelete">
					</TableHeader>
				</template>
				<template #empty>
					<el-empty :image-size="200" />
				</template>
				<template #row_code="{ row }">
					<el-tag class="text-h1 text-upper" size="large" type="primary">
						<b>{{ row.code }}</b>
					</el-tag>
				</template>
				<template #row_name="{ row }">
					<el-text class="text-h1 text-upper" type="danger"><b>{{ row.name }}</b></el-text>
					<br>
					<el-text class="text-h2">
						<span>{{ $t('lz.entity.vmsMessage.content1') }}: </span>
						<b class=" text-upper">{{ row.content1 }}</b>
					</el-text>
					<br v-if="row.type == '1'">
					<el-text class="text-h2" v-if="row.type == '1'">
						<span>{{ $t('lz.entity.vmsMessage.content2') }}: </span>
						<b class="text-upper">{{ row.content2 }}</b>
					</el-text>
				</template>
				<template #row_image="{ row }">
					<div class="panel-msg">
						<b class=" text-upper text-h2">{{ row.content1 }}</b>
						<b class=" text-upper text-h2">{{ row.content2 }}</b>
					</div>
				</template>
				<template #row_type="{ row }">
					{{ state.messageTypeList?.find((item: any) => item.code == row.type)?.name }}
				</template>
				<template #row_lang="{ row }">
					{{ state.languageList?.find((item: any) => item.code == row.lang)?.name }}
				</template>
				<template #row_status="{ row }">
					<TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.SysStatusType, row.status)" />
				</template>
				<template #row_record="{ row }">
					<ModifyRecord :data="row" />
				</template>
				<template #row_buttons="{ row }">
					<el-tooltip :content="$t('lz.button.base.copy')" placement="top" :auto-close="1000">
						<el-button icon="ele-CopyDocument" text type="warning" v-auth="'vmsMessage:add'" @click="handleCopy(row)"> </el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.edit')" placement="top" :auto-close="1000">
						<el-button icon="ele-Edit" text type="primary" v-auth="'vmsMessage:update'" @click="handleEdit(row)"> </el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.delete')" placement="top" :auto-close="1000">
						<el-button icon="ele-Delete" text type="danger" v-auth="'vmsMessage:delete'" @click="handleDelete(row)"> </el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.view')" placement="top">
						<el-button icon="ele-View" text type="primary" v-auth="'tmsVehicleRegistration:view'" @click="handleView(row)"> </el-button>
					</el-tooltip>
				</template>
				<template #header_name>
					{{ $t('fe.vmsMessage.title.list') }}
				</template>
			</vxe-grid>
		</el-card>
		</GridSearchPanel>
		<!-- GridTable: end -->

		<!-- Primary components: Begin -->
		<editMessageComp ref="editMessageCompRef" :title="state.title" :operateType="state.operateType" @handleQuery="handleQuery" />
		<detailMessageComp ref="detailMessageCompRef" :title="state.title" :operateType="state.operateType" @handleQuery="handleQuery" />
		<!-- Primary components: End -->
		
	</div>
	</WorkspacePage>
</template>

<script lang="ts" setup name="vmsMessage">
	import { onMounted, onActivated, reactive, ref, defineAsyncComponent } from 'vue';
	import { ElMessageBox, ElMessage } from 'element-plus';
	import { useI18n } from 'vue-i18n';
	import WorkspacePage from '/@/components/workspace/WorkspacePage.vue';
	import GridSearchPanel from '/@/components/gridPage/GridSearchPanel.vue';
	import GridSummary from '/@/components/gridPage/GridSummary.vue';
	import { useGridSummary } from '/@/components/gridPage/useGridSummary';
	import { useGridPager } from '/@/components/gridPage/useGridPager';
	import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
	import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
	import { getAPI } from '/@/utils/axios-utils';
	import { OperateType } from '/@/types/constants/operate';
	import ModifyRecord from '/@/components/table/modifyRecord.vue';

	import { VmsMessageApi } from '/@/api-services/vms';
	import { PageVmsMessageInput, PageVmsMessageOutput } from '/@/api-services/vms';

	import editMessageComp from '/@/views/vms/message/component/editMessage.vue';
	import detailMessageComp from '/@/views/vms/message/component/detailMessage.vue';

	import { useBaseConfig } from '/@/stores/baseConfig';
	import { BaseConfigTypeEnum } from '/@/types/enums';
	
	const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));
	const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
	const getConfigItemByCode = useBaseConfig().getConfigItemByCode;

	import { EntityFieldLength } from '/@/types/enums/baseEnum';

	const editMessageCompRef = ref<InstanceType<typeof editMessageComp>>();
	const detailMessageCompRef = ref<InstanceType<typeof detailMessageComp>>();

	const state = reactive({
		queryParams: {
			name: undefined,
			code: undefined,
			type: undefined,
			lang: undefined,
			content1: undefined,
			content2: undefined,
			status: undefined,
		},
		localPageParam: {
			pageSize: 50 as number,
			defaultSort: { field: 'orderNo', order: 'asc', descStr: 'desc' } as any,
		},
		title: '',
		operateType: 'add' as OperateType,
		showBatchDelete: false,
		languageList: [] as any,
		messageTypeList: [] as any,
		statusList: [] as any,
	});

	const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));
	const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));
	const { t: $t } = useI18n();
	const xGridVmsMessage = ref<VxeGridInstance>();
	const { summary, track } = useGridSummary();
	
	// Table parameter configuration
	const options = useVxeTable<PageVmsMessageOutput>(
		{
			id: 'vmsMessageGrid',
			name: 'vmsMessageGrid',
			columns: [
				{ type: 'checkbox', fixed: 'left', width: 45 },

				// Fields
				{ field: 'code', title: $t('lz.entity.base.code'), minWidth: 150, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_code' } },
				{ field: 'name', title: $t('lz.entity.base.name'), minWidth: 200, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_name' } },
				{ field: 'type', title: $t('lz.entity.base.type'), width: 80, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_type' } },
				{ field: '', title: $t('lz.entity.base.image'), minWidth: 200, showOverflow: 'tooltip', slots: { default: 'row_image' } },
				{ field: 'status', title: $t('lz.entity.base.status'), width: 110, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_status' } },

				// Action fields
				{ title: $t('lz.label.base.audit'), width: 100, showOverflow: 'tooltip', slots: { default: 'row_record' } },
				{ title: $t('lz.label.base.action'), fixed: 'right', width: 130, showOverflow: true, slots: { default: 'row_buttons' } },
			],
		},
		// vxeGrid configuration parameters (any parameter can be overridden here), refer to the official vxe-table documentation
		{
			// Proxy configuration
			proxyConfig: { autoLoad: true, ajax: { query: ({ page, sort }) => handleQueryApi(page, sort) } },

			// Sorting configuration
			sortConfig: { defaultSort: state.localPageParam.defaultSort },

			// Pagination configuration
			pagerConfig: { pageSize: state.localPageParam.pageSize },

			// Toolbar configuration
			toolbarConfig: { export: false },

			rowConfig: {
				height: 120
			}
		}
	);

	// Thu gọn thanh phân trang theo chiều rộng bảng.
	useGridPager(xGridVmsMessage, options);

	// Quay lại tab/menu (KeepAlive): tải lại trang hiện tại để nhận thay đổi từ tab khác.
	// onActivated cũng chạy ngay sau lần mount đầu, khi đó dữ liệu vừa tải nên bỏ qua.
	let activated = false;
	onActivated(async () => {
		if (!activated) {
			activated = true;
			return;
		}
		await xGridVmsMessage.value?.commitProxy('query');
	});

	// Page initialization
	onMounted(async () => {
		state.statusList = getConfigDataByCode(BaseConfigTypeEnum.SysStatusType);
		state.messageTypeList = getConfigDataByCode(BaseConfigTypeEnum.VmsMessageType);
		state.languageList = getConfigDataByCode(BaseConfigTypeEnum.SysLanguageType);
	});

	// Query API
	const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
		// Tạo object mới: không ghi page/sort vào bộ lọc.
		const params = { ...state.queryParams, page: page.currentPage, pageSize: page.pageSize, field: sort.field, order: sort.order, descStr: 'desc' } as PageVmsMessageInput;
		
		return track(getAPI(VmsMessageApi).apiVmsVmsmessagePagePost(params));
	};

	// Query operation
	const handleQuery = async () => {
		options.loading = true;
		await xGridVmsMessage.value?.commitProxy('reload') ;

		options.loading = false;
		selectChangeEvent();
	};

	// Reset operation
	const resetQuery = async () => {
		state.queryParams.name = undefined;
		state.queryParams.code = undefined;
		state.queryParams.type = undefined;
		state.queryParams.lang = undefined;
		state.queryParams.content1 = undefined;
		state.queryParams.content2 = undefined;
		state.queryParams.status = undefined;
		await xGridVmsMessage.value?.commitProxy('reload');
	};

	// Open add dialog
	const handleAdd = () => {
		state.title = $t('lz.label.base.titleAdd', {name: $t('lz.label.vms.vmsMessage')});
		state.operateType = 'add';
		editMessageCompRef.value?.openDialog({ status: 1, orderNo: 100, type: '0', lang: 'vi-VN' });
	};

	// Open copy dialog
	const handleCopy = (row: any) => {
		state.title = $t('lz.label.base.titleCopy', {name: $t('lz.label.vms.vmsMessage')});
		state.operateType = 'copy';

		editMessageCompRef.value?.openDialog(row);
	};

	// Open edit dialog
	const handleEdit = (row: any) => {
		state.title = $t('lz.label.base.titleEdit', {name: $t('lz.label.vms.vmsMessage')});
		state.operateType = 'edit';

		editMessageCompRef.value?.openDialog(row);
	};

	// Open view dialog
	const handleView = (row: any) => {
		state.title = $t('lz.label.base.titleView', {name: $t('lz.label.vms.vmsMessage')});
		state.operateType = 'view';

		detailMessageCompRef.value?.openDialog(row);
	};

	// Delete
	const handleDelete = (row: any) => {
		ElMessageBox.confirm($t('lz.message.base.confirmDelete', {name:row.name}), $t('lz.label.base.confirm'), {
			confirmButtonText: $t('lz.button.base.confirm'),
			cancelButtonText: $t('lz.button.base.cancel'),
			type: 'warning',
		})
		.then(async () => {
			try{
				await getAPI(VmsMessageApi).apiVmsVmsmessageDeletePost({ id: row.id });
				handleQuery();
				ElMessage.success($t('lz.message.base.processSuccess'));
			}
			catch(error){
				ElMessage.success($t('lz.message.base.processUnSuccess'));
			}
		})
		.catch(() => { });
	};

	//Batch delete
	const handleBatchDelete = async () => {
		const selectRecords = getCheckedRecords();

		ElMessageBox.confirm($t('lz.message.base.confirmBatchDelete', { total: selectRecords.length }), $t('lz.label.base.confirm'), {
			confirmButtonText: $t('lz.button.base.confirm'),
			cancelButtonText: $t('lz.button.base.cancel'),
			type: 'warning',
		})
		.then(async () => {
			try{
				await getAPI(VmsMessageApi).apiVmsVmsmessageBatchdeletePost(selectRecords.filter(l => l.id));
				handleQuery();
				ElMessage.success($t('lz.message.base.processSuccess'));
			}
			catch(error){
				ElMessage.success($t('lz.message.base.processUnSuccess'));
			}
		})
		.catch(() => { });
	}

	// Custom column
	function handleCustom() {
		xGridVmsMessage.value?.openCustom();
	}

	// Get selected records
	function getCheckedRecords() {
		return xGridVmsMessage.value?.getCheckboxRecords() ?? [];
	}

	function selectChangeEvent() {
		state.showBatchDelete = getCheckedRecords().length > 0;
	}

	// Table events
	const gridEvents: VxeGridListeners<PageVmsMessageOutput> = {
		// Only effective when pager-config is configured, this event will be triggered when pagination changes
		async pageChange({ pageSize }) {
			state.localPageParam.pageSize = pageSize;
		},
		// This event will be triggered when sorting conditions change
		async sortChange({ field, order }) {
			state.localPageParam.defaultSort = { field: field, order: order!, descStr: 'desc' };
		},
	};
</script>

<style scoped>
	.text-h1{
		font-size: large;
	}

	.text-upper{
		text-transform: uppercase;
	}

	.text-h2{
		font-size: 14px;
	}

	.panel-msg{
		background: black;
		display: flex;
		flex-direction: column;
		padding: 5px;
	}

	.panel-msg :first-child{
		color: red;
	}

	.panel-msg :last-child{
		color: #39e600;
	}
</style>