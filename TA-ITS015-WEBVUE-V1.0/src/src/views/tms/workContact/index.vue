<template>
	<WorkspacePage>
	<div class="work-contact-container">
		<GridSearchPanel id="tms-work-unit-mngt-work-contacts-search">
				<template #filters>
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false"   label-position="left" :inlineMessage="true"
				label-width="auto" style="flex: 1 1 0%">
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.code')" prop="code">
							<el-input v-model="state.queryParams.code" :placeholder="$t('lz.label.base.pleaseType', {input: $t('lz.entity.base.code')})"
								@keyup.enter.native="handleQuery" :maxlength="EntityFieldLength.Length64" show-word-limit clearable class="w100"/>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.name')" prop="name">
							<el-input v-model="state.queryParams.name" :placeholder="$t('lz.label.base.pleaseType', {input: $t('lz.entity.base.name')})" @keyup.enter.native="handleQuery" :maxlength="EntityFieldLength.Length128" show-word-limit clearable class="w100"/>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.type')" prop="type">
							<el-select v-model="state.queryParams.type" :placeholder="$t('lz.label.base.pleaseSelect', {input: $t('lz.entity.base.type')})" clearable class="w100">
								<el-option v-for="item in state.contactTypeList" :label="item.name" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.phone')" prop="phone">
							<el-input v-model="state.queryParams.phone" :placeholder="$t('lz.label.base.pleaseType', {input: $t('lz.entity.base.phone')})" @keyup.enter.native="handleQuery" :maxlength="EntityFieldLength.Length32" show-word-limit clearable class="w100"/>
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
				<SearchButton authSearch="tmsWorkContact:list" @search="handleQuery" @reset="resetQuery" :loading="options.loading" />
			</el-row>
				</template>
		
		<el-card class="full-table" shadow="hover">
			<vxe-grid ref="xGridWorkContact" class="xGrid-style" v-bind="options" v-on="gridEvents"
				@checkbox-change="selectChangeEvent" @checkbox-all="selectChangeEvent">
				<template #toolbar_buttons>
					<GridSummary :summary="summary" />
				</template>
				<template #toolbar_tools>
					<TableHeader @add="handleAdd" :show-delete="state.showBatchDelete" :auth-add="'tmsWorkContact:add'"
						:auth-delete="'tmsWorkContact:delete'" @refresh="handleQuery" @custom-column="handleCustom"
						@delete="handleBatchDelete">
					</TableHeader>
				</template>
				<template #empty>
					<el-empty :image-size="200" />
				</template>
				<template #row_type="{ row }">
					{{ state.contactTypeList.find(item => item.code == row.type)?.name }}
				</template>
				<template #row_status="{ row }">
					<TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.SysStatusType, row.status)" />

				</template>
				<template #row_record="{ row }">
					<ModifyRecord :data="row" />
				</template>
				<template #row_buttons="{ row }">
					<el-tooltip :content="$t('lz.button.base.copy')" placement="top" :auto-close="1000">
						<el-button icon="ele-CopyDocument" text type="warning" v-auth="'tmsWorkContact:add'" @click="handleCopy(row)" :disabled="row.type?.toString() != TmsWorkContactType.Other.toString()"> </el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.edit')" placement="top" :auto-close="1000">
						<el-button icon="ele-Edit" text type="primary" v-auth="'tmsWorkContact:update'" @click="handleEdit(row)"> </el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.delete')" placement="top" :auto-close="1000">
						<el-button icon="ele-Delete" text type="danger" v-auth="'tmsWorkContact:delete'" @click="handleDelete(row)" :disabled="row.type?.toString() != TmsWorkContactType.Other.toString()"> </el-button>
					</el-tooltip>
				</template>
				<template #header_name>
					{{ $t('lz.label.tms.tmsWorkContact') }}
				</template>
			</vxe-grid>
		</el-card>
		</GridSearchPanel>

		<editWorkContactComp ref="editWorkContactCompRef" :title="state.title" :operateType="state.operateType" @handleQuery="handleQuery" />
	</div>
	</WorkspacePage>
</template>

<script lang="ts" setup name="tmsWorkContact">
	import { onMounted, onActivated, reactive, ref, defineAsyncComponent } from 'vue';
	import WorkspacePage from '/@/components/workspace/WorkspacePage.vue';
	import GridSearchPanel from '/@/components/gridPage/GridSearchPanel.vue';
	import GridSummary from '/@/components/gridPage/GridSummary.vue';
	import { useGridSummary } from '/@/components/gridPage/useGridSummary';
	import { useGridPager } from '/@/components/gridPage/useGridPager';
	import { ElMessageBox, ElMessage } from 'element-plus';
	import { useI18n } from 'vue-i18n';
	import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
	import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
	import { getAPI } from '/@/utils/axios-utils';
	import { OperateType } from '/@/types/constants/operate';
	import ModifyRecord from '/@/components/table/modifyRecord.vue';

	import { TmsWorkContactApi } from '/@/api-services/tms';
	import { PageTmsWorkContactInput, PageTmsWorkContactOutput } from '/@/api-services/tms';

	import { useBaseConfig } from '/@/stores/baseConfig';
	import { BaseConfigTypeEnum } from '/@/types/enums';

	const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));
	const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
	const getConfigItemByCode = useBaseConfig().getConfigItemByCode;

	import { EntityFieldLength } from '/@/types/enums/baseEnum';

	import editWorkContactComp from './component/editWorkContact.vue';
	import { TmsWorkContactType } from '/@/types/enums';

	const editWorkContactCompRef = ref<InstanceType<typeof editWorkContactComp>>();
	const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));
	const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));
	const xGridWorkContact = ref<VxeGridInstance>();
	const { t: $t } = useI18n();
	const { summary, track } = useGridSummary();

	const state = reactive({
		queryParams: {
			code: undefined,
			name: undefined,
			type: undefined,
			phone: undefined,
			status: undefined
		},
		localPageParam: {
			pageSize: 50 as number,
			defaultSort: {} as any,
		},
		title: '',
		operateType: 'add' as OperateType,
		showBatchDelete: false,
		contactTypeList: [] as any,
		statusList: [] as any
	});

	// Table parameter configuration
	const options = useVxeTable<PageTmsWorkContactOutput>(
		{
			// ID riêng để không dùng chung cấu hình cột với trang gốc tms/workContact.
			id: 'tmsWorkUnitMngtWorkContacts',
			name: 'TmsWorkContact',
			columns: [
				// Fields
				{ type: 'checkbox', fixed: 'left', width: 45 },
				{ field: 'code', title: $t('lz.entity.base.code'), minWidth: 100, showOverflow: 'tooltip', sortable: true },
				{ field: 'name', title: $t('lz.entity.base.name'), minWidth: 100, showOverflow: 'tooltip', sortable: true },
				{ field: 'type', title: $t('lz.entity.base.type'), minWidth: 100, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_type' } },
				{ field: 'phone', title: $t('lz.entity.base.phone'), minWidth: 100, showOverflow: 'tooltip', sortable: true },
				{ field: 'address', title: $t('lz.entity.base.address'), minWidth: 200, showOverflow: 'tooltip', sortable: true },
				{ field: 'info', title: $t('lz.entity.base.info'), minWidth: 200, showOverflow: 'tooltip', sortable: true },
				{ field: 'status', title: $t('lz.entity.base.status'), minWidth: 100, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_status' } },
				// Actions
				{ title: $t('lz.label.base.audit'), width: 100, showOverflow: 'tooltip', slots: { default: 'row_record' } },
				{ title: $t('lz.label.base.action'), fixed: 'right', width: 100, showOverflow: true, slots: { default: 'row_buttons' } },
			],
		},
		{
			// Proxy configuration
			proxyConfig: { autoLoad: true, ajax: { query: ({ page, sort }) => handleQueryApi(page, sort) } },

			// Sorting configuration
			sortConfig: { remote: true },

			// Chỉ cho tick chọn dòng loại "Other" (dòng tự tạo); dòng liên kết (Đơn vị/Person...) không cho chọn để xóa
			checkboxConfig: { checkMethod: ({ row }: any) => row.type?.toString() === TmsWorkContactType.Other.toString() },

			// Pagination configuration
			pagerConfig: { pageSize: state.localPageParam.pageSize },

			// Toolbar configuration
			toolbarConfig: { export: false },
		}
	);
	// Thu gọn thanh phân trang theo chiều rộng bảng.
	useGridPager(xGridWorkContact, options);

	// Page initialization
	onMounted(async () => {
		state.contactTypeList = getConfigDataByCode(BaseConfigTypeEnum.TmsWorkContactType);
		state.statusList = getConfigDataByCode(BaseConfigTypeEnum.SysStatusType);

		await handleQuery();
	});

	// Quay lại tab/menu (KeepAlive): tải lại trang hiện tại để nhận thay đổi từ tab Đơn vị công tác.
	// onActivated cũng chạy ngay sau lần mount đầu, khi đó dữ liệu vừa tải nên bỏ qua.
	let activated = false;
	onActivated(async () => {
		if (!activated) {
			activated = true;
			return;
		}
		await xGridWorkContact.value?.commitProxy('query');
	});

	// Query API
	const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
		// Tạo object mới: không ghi page/sort vào bộ lọc.
		const params = { ...state.queryParams, page: page.currentPage, pageSize: page.pageSize, field: sort?.order ? sort?.field : undefined, order: sort?.order ? sort?.order : undefined, descStr: 'desc' } as PageTmsWorkContactInput;

		return track(getAPI(TmsWorkContactApi).apiTmsTmsworkcontactPagePost(params));
	};

	// Query operation
	const handleQuery = async () => {
		options.loading = true;
		await xGridWorkContact.value?.commitProxy('reload') ;

		options.loading = false;
		selectChangeEvent();
	};
                                                                    
	// Reset operation
	const resetQuery = async () => {
		state.queryParams.code = undefined;
		state.queryParams.name = undefined;
		state.queryParams.type = undefined;
		state.queryParams.phone = undefined;
		state.queryParams.status = undefined;
		await xGridWorkContact.value?.commitProxy('reload');
	};

	// Open add dialog
	const handleAdd = () => {
		state.title = $t('lz.label.base.titleAdd', {name: $t('lz.label.tms.tmsWorkContact')});
		state.operateType = 'add';
		editWorkContactCompRef.value?.openDialog({ status: 1, orderNo: 100, type: TmsWorkContactType.Other });
	};

	// Open copy dialog
	const handleCopy = (row: any) => {
		state.title = $t('lz.label.base.titleCopy', {name: $t('lz.label.tms.tmsWorkContact')});
		state.operateType = 'copy';

		editWorkContactCompRef.value?.openDialog(row);
	};

	// Open edit dialog
	const handleEdit = (row: any) => {
		state.title = $t('lz.label.base.titleEdit', {name: $t('lz.label.tms.tmsWorkContact')});
		state.operateType = 'edit';

		editWorkContactCompRef.value?.openDialog(row);
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
				await getAPI(TmsWorkContactApi).apiTmsTmsworkcontactDeletePost({ id: row.id });
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
		// Chỉ xóa dòng loại "Other"; loại bỏ dòng liên kết (Đơn vị làm việc/Person...) khỏi danh sách xóa
		const selectRecords = getCheckedRecords().filter((row: any) => row.type?.toString() === TmsWorkContactType.Other.toString());

		if (selectRecords.length === 0) {
			ElMessage.warning($t('lz.label.base.noData'));
			return;
		}

		ElMessageBox.confirm($t('lz.message.base.confirmBatchDelete', { total: selectRecords.length }), $t('lz.label.base.confirm'), {
			confirmButtonText: $t('lz.button.base.confirm'),
			cancelButtonText: $t('lz.button.base.cancel'),
			type: 'warning',
		})
		.then(async () => {
			try{
				await getAPI(TmsWorkContactApi).apiTmsTmsworkcontactBatchdeletePost(selectRecords.filter(l => l.id));
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
		xGridWorkContact.value?.openCustom();
	}

	// Get selected records
	function getCheckedRecords() {
		return xGridWorkContact.value?.getCheckboxRecords() ?? [];
	}

	function selectChangeEvent() {
		state.showBatchDelete = getCheckedRecords().length > 0;
	}

	// Table events
	const gridEvents: VxeGridListeners<PageTmsWorkContactOutput> = {
		// Only valid when pager-config is configured, this event will be triggered when the page changes
		async pageChange({ pageSize }) {
			state.localPageParam.pageSize = pageSize;
		},
		// This event will be triggered when the sorting condition changes
		async sortChange({ field, order }) {
			state.localPageParam.defaultSort = { field: field, order: order! };
		},
	};

</script>