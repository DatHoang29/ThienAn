<template>
	<WorkspacePage>
	<div class="vehicle-registration-container">
		<GridSearchPanel id="tms-vehicle-mngt-vehicle-registrations-search">
				<template #filters>
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" label-position="left" :inlineMessage="true"	label-width="auto" style="flex: 1 1 0%">
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.tms.licensePlate')" prop="licensePlate">
							<el-input v-model="state.queryParams.licensePlate" :placeholder="$t('lz.label.base.pleaseType', {input: $t('lz.entity.tms.licensePlate')})" @keyup.enter.native="handleQuery" :maxlength="EntityFieldLength.Length32" show-word-limit clearable class="w100"/>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.tmsVehicleRegistration.owner')" prop="owner">
							<el-input v-model="state.queryParams.owner" :placeholder="$t('lz.label.base.pleaseType', {input: $t('lz.entity.tmsVehicleRegistration.owner')})" @keyup.enter.native="handleQuery" :maxlength="EntityFieldLength.Length64" show-word-limit clearable class="w100"/>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.tms.vehicleType')" prop="vehicleType">
							<el-input v-model="state.queryParams.vehicleType" :placeholder="$t('lz.label.base.pleaseType', {input: $t('lz.entity.tms.vehicleType')})" @keyup.enter.native="handleQuery" :maxlength="EntityFieldLength.Length64" show-word-limit clearable class="w100"/>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.tmsVehicleRegistration.brand')" prop="brand">
							<el-input v-model="state.queryParams.brand" :placeholder="$t('lz.label.base.pleaseType', {input: $t('lz.entity.tmsVehicleRegistration.brand')})" @keyup.enter.native="handleQuery" :maxlength="EntityFieldLength.Length64" show-word-limit clearable class="w100"/>
						</el-form-item>
					</el-col>
					<!-- <el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.status')" prop="status" >
							<el-select v-model="state.queryParams.status" filterable clearable class="w100" @clear="state.queryParams.status = undefined" :placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.entity.base.status')})" >
								<el-option v-for="item in getConfigDataByCode(BaseConfigTypeEnum.SysStatusType)" :key="item.code" :label="item.name" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col> -->
				</el-row>
			</el-form>
			<el-divider style="height: calc(100% - 5px); margin: 0 10px" direction="vertical" />
			<el-row>
				<SearchButton authSearch="tmsVehicleRegistration:list" @search="handleQuery" @reset="resetQuery" :loading="options.loading" />
			</el-row>
				</template>

		<el-card class="full-table" shadow="hover">
			<vxe-grid ref="xGridVehicleRegistration" class="xGrid-style" v-bind="options" v-on="gridEvents" @checkbox-change="selectChangeEvent" @checkbox-all="selectChangeEvent">
				<template #toolbar_buttons>
					<GridSummary :summary="summary" />
				</template>
				<template #toolbar_tools>
					<TableHeader @add="handleAdd" :show-delete="state.showBatchDelete" :auth-add="'tmsVehicleRegistration:add'" :auth-delete="'tmsVehicleRegistration:delete'" @refresh="handleQuery" @custom-column="handleCustom" @delete="handleBatchDelete">
					</TableHeader>
				</template>
				<template #empty>
					<el-empty :image-size="200" />
				</template>
				<template #row_dataType="{ row }">
					{{ state.dataTypeList?.find(item => item.code == row.dataType)?.name }}
				</template>
				<template #row_expirationDate="{ row }">
					{{ row.expirationDate ? useDateFormat(row.expirationDate, DateTimeFormatEnum.DateDefault) : null }}
				</template>
				<template #row_record="{ row }">
					<ModifyRecord :data="row" />
				</template>
				<template #row_buttons="{ row }">
					<el-tooltip :content="$t('lz.button.base.copy')" placement="top" :auto-close="1000">
						<el-button icon="ele-CopyDocument" text type="warning" v-auth="'tmsVehicleRegistration:add'" @click="handleCopy(row)"> </el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.edit')" placement="top" :auto-close="1000">
						<el-button icon="ele-Edit" text type="primary" v-auth="'tmsVehicleRegistration:update'" @click="handleEdit(row)"> </el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.delete')" placement="top" :auto-close="1000">
						<el-button icon="ele-Delete" text type="danger" v-auth="'tmsVehicleRegistration:delete'" @click="handleDelete(row)"> </el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.view')" placement="top">
						<el-button icon="ele-View" text type="primary" v-auth="'tmsVehicleRegistration:view'" @click="handleView(row)"> </el-button>
					</el-tooltip>
				</template>
				<template #header_name>
					{{ $t('lz.label.tms.tmsVehicleRegistration') }}
				</template>
			</vxe-grid>
		</el-card>
		</GridSearchPanel>

		<editVehicleRegistrationComp ref="editVehicleRegistrationCompRef" :title="state.title" :operateType="state.operateType" @handleQuery="handleQuery" />
		<detailVehicleRegistrationComp ref="detailVehicleRegistrationCompRef" :title="state.title" :operateType="state.operateType" @handleQuery="handleQuery" />
		
	</div>
	</WorkspacePage>
</template>

<script lang="ts" setup name="tmsVehicleRegistration">

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
	import { useDateFormat } from '@vueuse/core'

	import { TmsVehicleRegistrationApi } from '/@/api-services/tms';
	import { PageTmsVehicleRegistrationInput, PageTmsVehicleRegistrationOutput } from '/@/api-services/tms';

	import { useBaseConfig } from '/@/stores/baseConfig';
	import { BaseConfigTypeEnum } from '/@/types/enums';
	
	const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
	
	import { DateTimeFormatEnum, EntityFieldLength } from '/@/types/enums/baseEnum';
 
	import editVehicleRegistrationComp from './component/editVehicleRegistration.vue';
	import detailVehicleRegistrationComp from './component/detailVehicleRegistration.vue';
	import { TmsVehicleRegistrationTypeEnum } from '/@/types/enums';

	const editVehicleRegistrationCompRef = ref<InstanceType<typeof editVehicleRegistrationComp>>();
	const detailVehicleRegistrationCompRef = ref<InstanceType<typeof detailVehicleRegistrationComp>>();

	const state = reactive({
		queryParams: {
			licensePlate: undefined,
			owner: undefined,
			brand: undefined,
			vehicleType: undefined,
			status: undefined,
		},
		localPageParam: {
			pageSize: 50 as number,
			defaultSort: {} as any,
		},
		title: '',
		operateType: 'add' as OperateType,
		showBatchDelete: false,
		dataTypeList: [] as any
	});

	const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));
	const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));
	const { t: $t } = useI18n();
	const { summary, track } = useGridSummary();
	const xGridVehicleRegistration = ref<VxeGridInstance>();
	
	// Table parameter configuration
	const options = useVxeTable<PageTmsVehicleRegistrationOutput>(
		{
			// ID riêng để không dùng chung cấu hình cột với trang gốc tms/vehicleRegistration.
			id: 'tmsVehicleMngtVehicleRegistrations',
			name: 'tmsVehicleRegistrationGrid',
			columns: [
				{ type: 'checkbox', fixed: 'left', width: 45 },
				{ field: 'licensePlate', title: $t('lz.entity.tms.licensePlate'), minWidth: 100, showOverflow: 'tooltip', sortable: true },
				{ field: 'vehicleType', title: $t('lz.entity.tms.vehicleType'), minWidth: 100, showOverflow: 'tooltip', sortable: true },
				{ field: 'brand', title: $t('lz.entity.tmsVehicleRegistration.brand'), minWidth: 100, showOverflow: 'tooltip', sortable: true },
				{ field: 'owner', title: $t('lz.entity.tmsVehicleRegistration.owner'), minWidth: 150, showOverflow: 'tooltip', sortable: true },
				{ field: 'overallDimensions', title: $t('lz.entity.tmsVehicleRegistration.overallDimensions'), width: 130, showOverflow: 'tooltip', sortable: true },
				{ field: 'expirationDate', title: $t('lz.entity.tmsVehicleRegistration.expirationDate'), width: 130, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_expirationDate' } },
				{ field: 'dataType', title: $t('lz.entity.tmsVehicleRegistration.dataType'), width: 130, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_dataType' } },
				{ field: '', title: $t('lz.label.base.audit'), width: 100, showOverflow: 'tooltip', slots: { default: 'row_record' } },
				{ title: $t('lz.label.base.action'), fixed: 'right', width: 130, showOverflow: true, slots: { default: 'row_buttons' } },
			],
		},
		// vxeGrid configuration parameters (any parameter can be overridden here), refer to the official vxe-table documentation
		{
			// Proxy configuration
			proxyConfig: { autoLoad: true, ajax: { query: ({ page, sort }) => handleQueryApi(page, sort) } },

			// Sorting configuration
			sortConfig: { remote: true },

			// Pagination configuration
			pagerConfig: { pageSize: state.localPageParam.pageSize },

			// Toolbar configuration
			toolbarConfig: { export: false },
		}
	);
	// Thu gọn thanh phân trang theo chiều rộng bảng.
	useGridPager(xGridVehicleRegistration, options);

	// Page initialization
	onMounted(async () => {
		state.dataTypeList = getConfigDataByCode(BaseConfigTypeEnum.TmsVehicleRegistrationType);

		await handleQuery();
	});

	// Quay lại tab/menu (KeepAlive): tải lại trang hiện tại để nhận thay đổi từ tab Lọc phương tiện.
	// onActivated cũng chạy ngay sau lần mount đầu, khi đó dữ liệu vừa tải nên bỏ qua.
	let activated = false;
	onActivated(async () => {
		if (!activated) {
			activated = true;
			return;
		}
		await xGridVehicleRegistration.value?.commitProxy('query');
	});

	// Query API
	const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
		// Tạo object mới: không ghi page/sort vào bộ lọc.
		const params = { ...state.queryParams, page: page.currentPage, pageSize: page.pageSize, field: sort?.order ? sort?.field : undefined, order: sort?.order ? sort?.order : undefined, descStr: 'desc' } as PageTmsVehicleRegistrationInput;
		
		return track(getAPI(TmsVehicleRegistrationApi).apiTmsTmsvehicleregistrationPagePost(params));
	};

	// Query operation
	const handleQuery = async () => {
		options.loading = true;
		await xGridVehicleRegistration.value?.commitProxy('reload') ;

		options.loading = false;
		selectChangeEvent();
	};

	// Reset operation
	const resetQuery = async () => {
		state.queryParams.licensePlate = undefined;
		state.queryParams.owner = undefined;
		state.queryParams.brand = undefined;
		state.queryParams.vehicleType = undefined;
		state.queryParams.status = undefined;
		await xGridVehicleRegistration.value?.commitProxy('reload');
	};

	// Open add dialog
	const handleAdd = () => {
		state.title = $t('lz.label.base.titleAdd', {name: $t('lz.label.tms.tmsVehicleRegistration')});
		state.operateType = 'add';
		editVehicleRegistrationCompRef.value?.openDialog({ orderNo: 100, dataType: TmsVehicleRegistrationTypeEnum.Manual });
	};

	// Open copy dialog
	const handleCopy = (row: any) => {
		state.title = $t('lz.label.base.titleCopy', {name: $t('lz.label.tms.tmsVehicleRegistration')});
		state.operateType = 'copy';

		editVehicleRegistrationCompRef.value?.openDialog(row);
	};

	// Open edit dialog
	const handleEdit = (row: any) => {
		state.title = $t('lz.label.base.titleEdit', {name: $t('lz.label.tms.tmsVehicleRegistration')});
		state.operateType = 'edit';

		editVehicleRegistrationCompRef.value?.openDialog(row);
	};

	// Open copy dialog
	const handleView = (row: any) => {
		state.title = $t('lz.label.base.titleView', {name: $t('lz.label.tms.tmsVehicleRegistration')});
		state.operateType = 'view';

		detailVehicleRegistrationCompRef.value?.openDialog(row);
	};

	// Delete
	const handleDelete = (row: any) => {
		ElMessageBox.confirm($t('lz.message.base.confirmDelete', {name:row.licensePlate}), $t('lz.label.base.confirm'), {
			confirmButtonText: $t('lz.button.base.confirm'),
			cancelButtonText: $t('lz.button.base.cancel'),
			type: 'warning',
		})
		.then(async () => {
			try{
				await getAPI(TmsVehicleRegistrationApi).apiTmsTmsvehicleregistrationDeletePost({ id: row.id });
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
				await getAPI(TmsVehicleRegistrationApi).apiTmsTmsvehicleregistrationBatchdeletePost(selectRecords.filter(l => l.id));
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
		xGridVehicleRegistration.value?.openCustom();
	}

	// Get selected records
	function getCheckedRecords() {
		return xGridVehicleRegistration.value?.getCheckboxRecords() ?? [];
	}

	function selectChangeEvent() {
		state.showBatchDelete = getCheckedRecords().length > 0;
	}

	// Table events
	const gridEvents: VxeGridListeners<PageTmsVehicleRegistrationOutput> = {
		// Only valid when pager-config is configured, this event will be triggered when the page changes
		async pageChange({ pageSize }) {
			state.localPageParam.pageSize = pageSize;
		},
		// This event will be triggered when the sorting condition changes
		async sortChange({ field, order }) {
			state.localPageParam.defaultSort = { field: field, order: order!, descStr: 'desc' };
		},
	};
</script>
