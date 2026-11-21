<template>
	<WorkspacePage>
	<div class="vehicle-filter-container">
		<GridSearchPanel id="tms-vehicle-mngt-vehicle-filters-search">
				<template #filters>
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true"	label-width="auto" style="flex: 1 1 0%"  label-position="left">
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.label.base.fromDate')" prop="fromDate">
							<el-date-picker
								v-model="state.queryParams.fromDate"
								type="datetime"
								:format="DateTimeFormatEnum.DateTimeDefault"
								:value-format="DateTimeFormatEnum.DateTimeValidInput"
								:placeholder="$t('lz.label.base.fromDate')"
								clearable
								class="w100"
							/>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.tms.licensePlate')" prop="licensePlate">
							<el-input v-model="state.queryParams.licensePlate" :placeholder="$t('lz.label.base.pleaseType', {input: $t('lz.entity.tms.licensePlate')})" @keyup.enter.native="handleQuery" :maxlength="EntityFieldLength.Length32" show-word-limit clearable/>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.type')" prop="type">
							<el-select v-model="state.queryParams.type" :placeholder="$t('lz.label.base.pleaseSelect', {input: $t('lz.entity.base.type')})" clearable class="w100">
								<el-option v-for="item in state.filterTypeList" :label="item.name" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.tmsVehicleFilter.alarm')" prop="alarm">
							<el-select v-model="state.queryParams.alarm" :placeholder="$t('lz.label.base.pleaseSelect', {input: $t('lz.entity.tmsVehicleFilter.alarm')})" clearable class="w100">
								<el-option v-for="item in state.filterWarningList" :label="item.name" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
					
					<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.label.base.toDate')" prop="toDate">
							<el-date-picker
								v-model="state.queryParams.toDate"
								type="datetime"
								:format="DateTimeFormatEnum.DateTimeDefault"
								:value-format="DateTimeFormatEnum.DateTimeValidInput"
								:placeholder="$t('lz.label.base.toDate')"
								clearable
								class="w100"
							/>
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
				<SearchButton authSearch="tmsVehicleFilter:list" @search="handleQuery" @reset="resetQuery" :loading="options.loading" />
			</el-row>
				</template>

		<el-card class="full-table" shadow="hover">
			<vxe-grid ref="xGridVehicleFilter" class="xGrid-style" v-bind="options" v-on="gridEvents" @checkbox-change="selectChangeEvent" @checkbox-all="selectChangeEvent">
				<template #toolbar_buttons>
					<GridSummary :summary="summary" />
				</template>
				<template #toolbar_tools>
					<TableHeader @add="handleAdd" :show-delete="state.showBatchDelete" :auth-add="'tmsVehicleFilter:add'" :auth-delete="'tmsVehicleFilter:delete'" @refresh="handleQuery" @custom-column="handleCustom" @delete="handleBatchDelete">
					</TableHeader>
				</template>
				<template #empty>
					<el-empty :image-size="200" />
				</template>
				<template #row_startDate="{ row }">
					{{ row.startDate ? useDateFormat(row.startDate, DateTimeFormatEnum.DateDefault) : null }}
				</template>
				<template #row_endDate="{ row }">
					{{ row.endDate ? useDateFormat(row.endDate, DateTimeFormatEnum.DateDefault) : null }}
				</template>
				<template #row_type="{ row }">
					{{ state.filterTypeList?.find(item => item.code == row.type)?.name }}
				</template>
				<template #row_alarm="{ row }">
					{{ state.filterWarningList?.find(item => item.code == row.alarm)?.name }}
				</template>
				<template #row_status="{ row }">
					<TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.SysStatusType, row.status)" />
				</template>
				<template #row_record="{ row }">
					<ModifyRecord :data="row" />
				</template>
				<template #row_buttons="{ row }">
					<el-tooltip :content="$t('lz.button.base.copy')" placement="top" :auto-close="1000">
						<el-button icon="ele-CopyDocument" text type="warning" v-auth="'tmsVehicleFilter:add'" @click="handleCopy(row)"> </el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.edit')" placement="top" :auto-close="1000">
						<el-button icon="ele-Edit" text type="primary" v-auth="'tmsVehicleFilter:update'" @click="handleEdit(row)"> </el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.delete')" placement="top" :auto-close="1000">
						<el-button icon="ele-Delete" text type="danger" v-auth="'tmsVehicleFilter:delete'" @click="handleDelete(row)"> </el-button>
					</el-tooltip>
				</template>
				<template #header_name>
					{{ $t('lz.label.tms.tmsVehicleFilter') }}
				</template>
			</vxe-grid>
		</el-card>
		</GridSearchPanel>

		<editVehicleFilterComp ref="editVehicleFilterCompRef" :title="state.title" :operateType="state.operateType" @handleQuery="handleQuery" />
	</div>
	</WorkspacePage>
</template>

<script lang="ts" setup>

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

	import { TmsVehicleFilterApi } from '/@/api-services/tms';
	import { PageTmsVehicleFilterInput, PageTmsVehicleFilterOutput } from '/@/api-services/tms';

	import { useBaseConfig } from '/@/stores/baseConfig';
	import { BaseConfigTypeEnum } from '/@/types/enums';

	import { useCommonVarsAndFuncs } from '/@/stores/its/commonVarsAndFuncs';
	const commonVarsAndFuncs = useCommonVarsAndFuncs();
	
	const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));
	const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
	const getConfigItemByCode = useBaseConfig().getConfigItemByCode;
	
	import { DateTimeFormatEnum, EntityFieldLength } from '/@/types/enums/baseEnum';
	import editVehicleFilterComp from './component/editVehicleFilter.vue';
	import { TmsVehicleFilterTypeEnum, TmsVehicleFilterWarningEnum } from '/@/types/enums';

	const editVehicleFilterCompRef = ref<InstanceType<typeof editVehicleFilterComp>>();

	const state = reactive({
		queryParams: {
			licensePlate: undefined,
			type: undefined,
			alarm: undefined,
			fromDate: commonVarsAndFuncs.getISODatetimeFromDays(-3),
			toDate: commonVarsAndFuncs.getISODatetimeFromDays(0),
			status: undefined,
		},
		localPageParam: {
			pageSize: 50 as number,
			defaultSort: {} as any,
		},
		title: '',
		operateType: 'add' as OperateType,
		showBatchDelete: false,
		filterTypeList: [] as any,
		filterWarningList: [] as any,
	});

	const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));
	const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));
	const { t: $t } = useI18n();
	const { summary, track } = useGridSummary();
	const xGridVehicleFilter = ref<VxeGridInstance>();
	
	const options = useVxeTable<PageTmsVehicleFilterOutput>(
		{
			// ID riêng để không dùng chung cấu hình cột với trang gốc tms/vehicleFilter.
			id: 'tmsVehicleMngtVehicleFilters',
			name: 'tmsVehicleFilterGrid',
			columns: [
				// Fields
				{ type: 'checkbox', fixed: 'left', width: 45 },
				{ field: 'licensePlate', title: $t('lz.entity.tms.licensePlate'), minWidth: 100, showOverflow: 'tooltip', sortable: true },
				{ field: 'type', title: $t('lz.entity.base.type'), minWidth: 100, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_type' }},
				{ field: 'startDate', title: $t('lz.entity.tmsVehicleFilter.startDate'), minWidth: 100, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_startDate' }},
				{ field: 'endDate', title: $t('lz.entity.tmsVehicleFilter.endDate'), minWidth: 100, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_endDate' }},
				{ field: 'alarm', title: $t('lz.entity.tmsVehicleFilter.alarm'), minWidth: 100, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_alarm' }},
				{ field: 'status', title: $t('lz.entity.base.status'), minWidth: 100, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_status' }},

				// Actions
				{ field: '', title: $t('lz.label.base.audit'), width: 100, showOverflow: 'tooltip', slots: { default: 'row_record' } },
				{ title: $t('lz.label.base.action'), fixed: 'right', width: 100, showOverflow: true, slots: { default: 'row_buttons' } },
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
	useGridPager(xGridVehicleFilter, options);

	onMounted(async () => {
		state.filterTypeList = getConfigDataByCode(BaseConfigTypeEnum.TmsVehicleFilterType);
		state.filterWarningList = getConfigDataByCode(BaseConfigTypeEnum.TmsVehicleFilterWarning);

		await handleQuery();
	});

	// Quay lại tab/menu (KeepAlive): tải lại trang hiện tại để nhận thay đổi từ tab Đăng ký phương tiện.
	// onActivated cũng chạy ngay sau lần mount đầu, khi đó dữ liệu vừa tải nên bỏ qua.
	let activated = false;
	onActivated(async () => {
		if (!activated) {
			activated = true;
			return;
		}
		await xGridVehicleFilter.value?.commitProxy('query');
	});

	// Query API
	const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
		// Tạo object mới: không ghi page/sort vào bộ lọc.
		const params = { ...state.queryParams, page: page.currentPage, pageSize: page.pageSize, field: sort?.order ? sort?.field : undefined, order: sort?.order ? sort?.order : undefined, descStr: 'desc' } as PageTmsVehicleFilterInput;
		
		return track(getAPI(TmsVehicleFilterApi).apiTmsTmsvehiclefilterPagePost(params));
	};

	// Query operation
	const handleQuery = async () => {
		options.loading = true;
		await xGridVehicleFilter.value?.commitProxy('reload') ;

		options.loading = false;
		selectChangeEvent();
	};

	// Reset operation
	const resetQuery = async () => {
		state.queryParams.licensePlate = undefined;
		state.queryParams.type = undefined;
		state.queryParams.alarm = undefined;
		state.queryParams.fromDate = undefined;
		state.queryParams.toDate = undefined;
		state.queryParams.status = undefined;

		await xGridVehicleFilter.value?.commitProxy('reload');
	};

	// Open add dialog
	const handleAdd = () => {
		state.title = $t('lz.label.base.titleAdd', {name: $t('lz.label.tms.tmsVehicleFilter')});
		state.operateType = 'add';
		editVehicleFilterCompRef.value?.openDialog({status: 1, orderNo: 100, alarm: TmsVehicleFilterWarningEnum.Alarm, type: TmsVehicleFilterTypeEnum.DenyList});
	};

	// Open copy dialog
	const handleCopy = (row: any) => {
		state.title = $t('lz.label.base.titleCopy', {name: $t('lz.label.tms.tmsVehicleFilter')});
		state.operateType = 'copy';

		editVehicleFilterCompRef.value?.openDialog(row);
	};

	// Open edit dialog
	const handleEdit = (row: any) => {
		state.title = $t('lz.label.base.titleEdit', {name: $t('lz.label.tms.tmsVehicleFilter')});
		state.operateType = 'edit';

		editVehicleFilterCompRef.value?.openDialog(row);
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
				await getAPI(TmsVehicleFilterApi).apiTmsTmsvehiclefilterDeletePost({ id: row.id });
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
				await getAPI(TmsVehicleFilterApi).apiTmsTmsvehiclefilterBatchdeletePost(selectRecords.filter(l => l.id));
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
		xGridVehicleFilter.value?.openCustom();
	}

	// Get selected records
	function getCheckedRecords() {
		return xGridVehicleFilter.value?.getCheckboxRecords() ?? [];
	}

	function selectChangeEvent() {
		state.showBatchDelete = getCheckedRecords().length > 0;
	}

	// Table events
	const gridEvents: VxeGridListeners<PageTmsVehicleFilterOutput> = {
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
<style lang="scss" scoped>
	.flex-inline{
		display: flex;
		align-items: center;
		justify-content: space-between;
		flex-wrap: nowrap;
		gap: 5px;
	}

	.flex-inline > *{
		padding: 0px;
		margin: 0px;
	}
</style>