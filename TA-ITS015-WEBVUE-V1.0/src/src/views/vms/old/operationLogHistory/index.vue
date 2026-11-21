<template>
	<div class="operation-log-container">
		<el-card shadow="hover" :body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', height: '100%', alignItems: 'start' }">
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true"	label-width="auto" style="flex: 1 1 0%" label-position="left">
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.label.fmsEquipmentHistory.equipmentType')">
							<el-select v-model="state.queryParams.equipmentTypeId" @change="handelEquipmentTypeChange"  :placeholder="$t('lz.label.base.pleaseSelect', {input: $t('lz.label.fmsEquipmentHistory.equipmentType')})" clearable class="w100">
								<el-option v-for="item in state.equipmentTypeList" :label="item.name" :value="item.id" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.fmsEquipmentHistory.equipmentId')">
							<el-select v-model="state.queryParams.equipmentId" :placeholder="$t('lz.label.base.pleaseSelect', {input: $t('lz.entity.fmsEquipmentHistory.equipmentId')})" filterable clearable class="w100">
								<el-option v-for="item in state.equipmentFilterList" :label="`${item.name} (${item?.kmNumber ?? 0}Km +${item.metNumber ?? 0})`" :value="item.id" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.label.base.status')">
							<el-select v-model="state.queryParams.state" :placeholder="$t('lz.label.base.pleaseSelect', {input: $t('lz.label.base.status')})" clearable class="w100">
								<el-option v-for="item in state.stateList" :label="item.name" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="24" :md="6" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.tmsSignallog.executeBy')" prop="type">
							<el-input v-model="state.queryParams.executeBy" 
							:placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.tmsSignallog.executeBy')})"
							 clearable @keyup.enter.native="handleQuery" class="w100"/>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.label.base.fromDate')">
							<el-date-picker
								v-model="state.queryParams.fromDate"
								type="datetime"
								:disabled-date="(time: any) => time.getTime() > Date.now()"
								:format="DateTimeFormatEnum.DateTimeDefault"
								:value-format="DateTimeFormatEnum.DateTimeValidInput"
								:placeholder="$t('lz.label.base.pleaseType', {input: $t('lz.label.base.fromDate')})"
								:clearable="false"
								class="w100"
							/>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.label.base.toDate')">
							<el-date-picker
								v-model="state.queryParams.toDate"
								type="datetime"
								:disabled-date="(time: any) => time.getTime() > Date.now()"
								:format="DateTimeFormatEnum.DateTimeDefault"
								:value-format="DateTimeFormatEnum.DateTimeValidInput"
								:placeholder="$t('lz.label.base.pleaseType', {input: $t('lz.label.base.toDate')})"
								:clearable="false"
								class="w100"
							/>
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>

			<el-divider style="height: calc(100% - 5px); margin: 0 10px" direction="vertical" />

			<el-row>
				<el-col>
					<SearchButton authSearch="vmsOperationLogHistory:list" @search="handleQuery" @reset="resetQuery" :loading="state.isProcessing" />
				</el-col>
			</el-row>
		</el-card>

		<el-card class="full-table" shadow="hover" style="margin-top: 5px">
			<vxe-grid ref="xGridVmsOperationLog" class="xGrid-style" v-bind="options" v-on="gridEvents" @checkbox-change="selectChangeEvent" @checkbox-all="selectChangeEvent">
				<template #toolbar_tools>
					<TableHeader @refresh="handleQuery" @custom-column="handleCustom" :show-add="false">
					</TableHeader>
				</template>

				<template #empty>
					<el-empty :image-size="200" />
				</template>

				<template #row_executeTime="{ row }">
					{{ useDateFormat(row.executeTime, DateTimeFormatEnum.DateTimeDefault) }}
				</template>
				
				<template #row_oldData="{ row }">
					<div v-if="row.oldUrl" class="div-img">
						<el-image :src="commonVarsAndFuncs.getFullUrl(row.oldUrl)" fit="contain" >
							<template #error>
								<div class="image-error">
									<el-icon :size="30"><Picture /></el-icon>
								</div>
							</template>
						</el-image>
					</div>
					<div v-else class="div-msg">
						<p v-if="row.oldData"><vue-json-pretty :data="StringToObj(row.oldData)" /></p>
					</div>
				</template>

				<template #row_newData="{ row }">
					<div v-if="row.newUrl" class="div-img">
						<el-image :src="commonVarsAndFuncs.getFullUrl(row.newUrl)" fit="contain" >
							<template #error>
								<div class="image-error">
									<el-icon :size="30"><Picture /></el-icon>
								</div>
							</template>
						</el-image>
					</div>
					<div v-else class="div-msg">
						<p><vue-json-pretty :data="StringToObj(row.newData)" /></p>
					</div>
				</template>

				<template #row_state="{ row }">
					<TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.TmsActionType, row.state)" />
				</template>

				<template #row_record="{ row }">
					<ModifyRecord :data="row" />
				</template>

				<template #row_buttons="{ row }">
					<el-tooltip :content="$t('lz.button.base.view')" placement="top">
						<el-button icon="ele-View" text type="primary" v-auth="'vmsOperationLogHistory:view'" @click="handleView(row)"> </el-button>
					</el-tooltip>
				</template>
			</vxe-grid>
		</el-card>

		<operationLogDetailComp ref="operationLogDetailCompRef" :operateType="state.operateType" :title="state.title" />
	</div>
</template>

<script lang="ts" setup name="vmsOperationLogHistory">

	import { onMounted, reactive, ref, defineAsyncComponent } from 'vue';
	import { useI18n } from 'vue-i18n';
	import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
	import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
	import { getAPI } from '/@/utils/axios-utils';
	import ModifyRecord from '/@/components/table/modifyRecord.vue';
	import { useDateFormat } from '@vueuse/core';
	import { Picture } from '@element-plus/icons-vue';

	import VueJsonPretty from 'vue-json-pretty';
	import 'vue-json-pretty/lib/styles.css';
	import { StringToObj } from '/@/utils/json-utils';

	import { TmsEquipmentApi, TmsEquipmentTypeApi, TmsModulesApi, TmsSignalLogHistoryApi } from '/@/api-services/tms';
	import { PageSignalLogHistoryInput, PageSignalLogHistoryOutput } from '/@/api-services/tms';
	import { DateTimeFormatEnum } from '/@/types/enums/baseEnum';

	import operationLogDetailComp from '/@/views/vms/operationLogHistory/component/detail.vue';
	const operationLogDetailCompRef = ref<InstanceType<typeof operationLogDetailComp>>();

	import { useCommonVarsAndFuncs } from '/@/stores/its/commonVarsAndFuncs';
	const commonVarsAndFuncs = useCommonVarsAndFuncs();

	import { useBaseConfig } from '/@/stores/baseConfig';
	import { BaseConfigTypeEnum } from '/@/types/enums';
	const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
	const getConfigItemByCode = useBaseConfig().getConfigItemByCode;

	const state = reactive({
		title: '',
		operateType: null,
		isProcessing: false,
		showBatchDelete: false,
		queryParams: {
			moduleKey: "VMS",
			equipmentTypeId: null,
			equipmentId: null,
			state: null,
			fromDate: commonVarsAndFuncs.getISODatetimeFromDays(-3, 'start'),
			toDate: commonVarsAndFuncs.getISODatetimeFromDays(0, 'end'),
			executeBy: null
		},
		localPageParam: {
			pageSize: 50 as number,
			defaultSort: { field: 'executeTime', order: 'desc', descStr: 'desc' } as any,
		},
		stateList: [],
		equipmentList: [],
		equipmentFilterList: [],
		equipmentTypeList: [],
	});

	const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));
	const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));
	const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));
	
	const { t: $t } = useI18n();
	const xGridVmsOperationLog = ref<VxeGridInstance>();

	const options = useVxeTable<PageSignalLogHistoryOutput>(
		{
			id: 'vmsOperationLogGrid',
			name: 'vmsOperationLogGrid',
			columns: [
				// Fields
				{ field: 'executeTime', title: $t('lz.entity.tmsSignallog.executeTime'), minWidth: 120, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_executeTime' }},
				{ field: 'equipmentTypeName', title: $t('lz.entity.base.type'), minWidth: 60, showOverflow: 'tooltip', sortable: true  },
				{ field: 'equipmentName', title: $t('lz.entity.tms.equipment'), minWidth: 60, showOverflow: 'tooltip', sortable: true },
				{ field: 'oldData', title: $t('lz.label.tmsSignallog.beforeData'), minWidth: 250, showOverflow: 'tooltip', slots: { default: 'row_oldData' } },
				{ field: 'newData', title: $t('lz.label.tmsSignallog.afterData'), minWidth: 250, showOverflow: 'tooltip', slots: { default: 'row_newData' } },
				{ field: 'state', title: $t('lz.entity.base.status'), minWidth: 100, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_state' } },
				{ field: 'remark', title: $t('lz.entity.base.remark'), minWidth: 200, showOverflow: 'tooltip' },
				{ field: 'executeBy', title: $t('lz.entity.tmsSignallog.executeBy'), minWidth: 90, showOverflow: 'tooltip', sortable: true },

				// Action fields
				// Gán field duy nhất để vxe-table lưu/khôi phục đúng trạng thái ẩn/hiện cột (custom column dùng field làm colId)
				{ field: 'action', title: $t('lz.label.base.action'), fixed: 'right', width: 130, showOverflow: true, slots: { default: 'row_buttons' } },
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
					height: 80
			}
		}
	);

	// Page initialization
	onMounted(async () => {
		let response = await getAPI(TmsModulesApi).apiTmsTmsmodulesListGet('', 'VMS');
		const vmsModule = response.data.result?.[0];

		response = await getAPI(TmsEquipmentTypeApi).apiTmsTmsequipmenttypeListGet('', '', '', vmsModule?.id);
		state.equipmentTypeList = response.data.result;

		response = await getAPI(TmsEquipmentApi).apiTmsTmsequipmentListGet();
		state.equipmentList = response.data.result?.filter(item => state.equipmentTypeList?.some(eqt => eqt.id == item.equipmentTypeId));
		state.equipmentFilterList = state.equipmentList?.sort((a, b) => a.name.localeCompare(b.name));

		state.stateList = getConfigDataByCode(BaseConfigTypeEnum.TmsActionType);
	});

	// Query API
	const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
		const params = Object.assign(
			state.queryParams, 
			{ page: page.currentPage, pageSize: page.pageSize, field: sort.field, order: sort.order, descStr: 'desc' }
		) as PageSignalLogHistoryInput;

		return await getAPI(TmsSignalLogHistoryApi).apiTmsTmssignalloghistoryPagePost(params);
	};

	// Custom column
	function handleCustom() {
		xGridVmsOperationLog.value?.openCustom();
	}

	// Query operation
	const handleQuery = async () => {
		options.loading = true;
		await xGridVmsOperationLog.value?.commitProxy('reload') ;

		options.loading = false;
		selectChangeEvent();
	};

	// Reset operation
	const resetQuery = async () => {
		state.queryParams.equipmentTypeId = null;
		state.queryParams.equipmentId = null;
		state.queryParams.state = null;
		state.queryParams.fromDate = commonVarsAndFuncs.getISODatetimeFromDays(-3, 'start');
		state.queryParams.toDate = commonVarsAndFuncs.getISODatetimeFromDays(0, 'end');

		state.queryParams.executeBy = null;

		await xGridVmsOperationLog.value?.commitProxy('reload');
	};

	const handleView = (row: any) => {
		state.title = $t('lz.label.base.titleView');
		state.operateType = 'view';

		operationLogDetailCompRef.value?.openDialog(row);
	};

	const handelEquipmentTypeChange = (value) => {
		state.queryParams.equipmentId = null;
		state.queryParams.state = null;
		
		state.equipmentFilterList = value?.length > 0
			? state.equipmentList?.filter(equip => equip.equipmentTypeId == value)
			: state.equipmentList;

		state.equipmentFilterList = state.equipmentFilterList?.sort((a, b) => a.name.localeCompare(b.name));
	};

	// Get selected records
	function getCheckedRecords() {
		return xGridVmsOperationLog.value?.getCheckboxRecords() ?? [];
	}

	function selectChangeEvent() {
		state.showBatchDelete = getCheckedRecords().length > 0;
	}

	// Table events
	const gridEvents: VxeGridListeners<PageSignalLogHistoryOutput> = {
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

<style lang="scss">
	.div-img{
		display: flex;
		justify-content: center;
		align-items: center;

		.el-image__error, .el-image__inner, .el-image__placeholder, .el-image__wrapper{
			max-height: 70px !important;
		}
	}

	.div-msg{
		padding: 5px;
	}
</style>