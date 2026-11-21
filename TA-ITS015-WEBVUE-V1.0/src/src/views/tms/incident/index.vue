<template>
	<div class="incident-container">
		<el-card class="margin-bottom--5px" >
			<el-row :gutter="10">
				<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
					<el-form-item :label="$t('lz.label.base.chartDataType')" prop="name">
						<el-select v-model="incidentChartDataType" :placeholder="$t('lz.label.base.pleaseSelect', {input: $t('lz.label.base.chartDataType')})" style="width: 180px;" @change="onChartDataTypeChange">
							<el-option :label="$t('lz.label.base.day')" value="day" />
							<el-option :label="$t('lz.label.base.timeRange')" value="time" />
						</el-select>
					</el-form-item>
				</el-col>
				<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
					<el-form-item>
						<el-checkbox v-model="state.showChartLegend">{{ $t('lz.label.base.showChartLegend') }}</el-checkbox>
					</el-form-item>
				</el-col>
			</el-row>
			
			<el-row :gutter="10">
				<el-col class="mb5" :xs="24" :sm="24" :md="24" :lg="18" :xl="18">
					<div style="height: 250px; width: 100%;">
						<lineChartComp ref="lineChartRef" :dataChangedCount="state.dataChangedCount" :chartData="state.lineChartData" :showLegend="state.showChartLegend" @onChartMounted="onLineChartMounted" />
					</div>
				</el-col>
				<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
					<incidentCountComp ref="incidentCountCompRef" :new="state.incidentCount?.new ?? 0" :valid="state.incidentCount?.valid ?? 0" :inValid="state.incidentCount?.inValid ?? 0" :total="state.incidentCount?.total ?? 0" :chartDataTypeName="state.incidentChartDataTypeName" />
				</el-col>
			</el-row>
		</el-card>
		<el-card shadow="hover"
			:body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', height: '100%', alignItems: 'start' }">
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" label-position="left" :inlineMessage="true"	label-width="auto" style="flex: 1 1 0%">
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.label.base.recordGroup')">
							<el-checkbox-group v-model="selectedGroups" @change="handleQuery">
								<el-checkbox label="event">{{ $t('lz.label.tms.event') }}</el-checkbox>
								<el-checkbox label="incident">{{ $t('lz.label.tms.tmsIncident') }}</el-checkbox>
							</el-checkbox-group>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.name')" prop="name">
							<el-input v-model="state.queryParams.name" :placeholder="$t('lz.label.base.pleaseType', {input: $t('lz.entity.base.name')})" @keyup.enter.native="handleQuery" :maxlength="EntityFieldLength.Length64" show-word-limit clearable class="w100"/>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.tms.eventType')" prop="eventTypeId">
							<el-select v-model="state.queryParams.eventTypeId" :placeholder="$t('lz.label.base.pleaseSelect', {input: $t('lz.entity.tms.eventType')})" filterable clearable class="w100">
								<el-option v-for="item in state.eventTypeList" :label="item.name" :value="item.id" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.tmsIncident.zoneId')" prop="zoneId">
							<el-select v-model="state.queryParams.zoneId" :placeholder="$t('lz.label.base.pleaseSelect', {input: $t('lz.entity.tmsIncident.zoneId')})" filterable clearable class="w100">
								<el-option v-for="item in state.zoneList" :label="item.name" :value="item.id" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.status')" prop="state">
							<el-select v-model="state.queryParams.state" :placeholder="$t('lz.label.base.pleaseSelect', {input: $t('lz.entity.base.status')})" clearable class="w100">
								<el-option v-for="item in state.incidentStatusList" :label="item.name" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.tmsIncident.source')" prop="source">
							<el-select v-model="state.queryParams.source" :placeholder="$t('lz.label.base.pleaseSelect', {input: $t('lz.entity.tmsIncident.source')})" clearable class="w100">
								<el-option v-for="item in state.sourceDetectList" :label="item.name" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.label.tmsIncident.fromPosition')">
							<div class="flex-inline w100">
								<el-input-number
									v-model="state.queryParams.fromKm"
									:min="0"
									controls-position="right"
									placeholder="Km"
									class="w100"
								/>
								<el-input-number
									v-model="state.queryParams.fromMet"
									:min="0"
									controls-position="right"
									placeholder="+Met"
									class="w100"
								/>
							</div>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
							<el-form-item :label="$t('lz.label.tmsIncident.toPosition')">
								<div class="flex-inline w100">
									<el-input-number
										v-model="state.queryParams.toKm"
										:min="0"
										controls-position="right"
										placeholder="Km"
										class="w100"
									/>
									<el-input-number
										v-model="state.queryParams.toMet"
										:min="0"
										controls-position="right"
										placeholder="+Met"
										class="w100"
									/>
								</div>
							</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="12" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.label.base.dateTime')">
							<div class="flex-inline w100">
								<el-date-picker
									v-model="state.queryParams.fromDate"
									type="datetime"
									:disabled-date="(time:any) => time.getTime() > Date.now()"
									:format="DateTimeFormatEnum.DateTimeDefault"
									:value-format="DateTimeFormatEnum.DateTimeValidInput"
									:placeholder="$t('lz.label.base.fromDate')"
									class="w100"
									:clearable="false"
								/>
								<el-date-picker
									v-model="state.queryParams.toDate"
									type="datetime"
									:disabled-date="(time:any) => time.getTime() > Date.now()"
									:format="DateTimeFormatEnum.DateTimeDefault"
									:value-format="DateTimeFormatEnum.DateTimeValidInput"
									:placeholder="$t('lz.label.base.toDate')"
									class="w100"
									:clearable="false"
								/>
							</div>
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>
			<el-divider style="height: calc(100% - 5px); margin: 0 10px" direction="vertical" />
			<el-row>
				<SearchButton authSearch="tmsIncident:list" @search="handleQuery" @reset="resetQuery" :loading="options.loading" />
			</el-row>
		</el-card>

		<el-card class="full-table" shadow="hover" style="margin-top: 5px; ">
			<vxe-grid ref="xGridIncident" class="xGrid-style" v-bind="options" v-on="gridEvents" @checkbox-change="selectChangeEvent" @checkbox-all="selectChangeEvent">
				<template #toolbar_tools>
					<TableHeader @add="handleAdd" :show-delete="state.showBatchDelete" :auth-add="'tmsIncident:add'" :auth-delete="'tmsIncident:delete'" @refresh="handleQuery" @custom-column="handleCustom">
					</TableHeader>
				</template>
				<template #toolbar_buttons>
					<el-button type="primary" plain icon="ele-Monitor" v-auth="'tmsIncident:list'" @click="$router.push('/tms/incidentNew')">{{ $t('lz.label.tms.eventsAndIncidents') }}</el-button>
					
				</template>
				<template #empty>
					<el-empty :image-size="200" />
				</template>
				<template #row_eventTypeId="{ row }">
					<div class="display-flex--horizontal flex-justify--center flex-gap--5px">
						<el-image style="max-width: 60px; max-height: 40px;height: 40px" :src="commonVarsAndFuncs.getFullUrl(state.eventTypeList?.find((item: any) => item.id == row.eventTypeId)?.url)" fit="contain" >
							<template #error>
								<div class="image-error">
									<el-icon :size="30"><Picture /></el-icon>
								</div>
							</template>
						</el-image>
						<p class="display-flex--horizontal flex-align--center">{{ incidentEventTypeDisplay(row, state.eventTypeList) }}</p>
					</div>
				</template>
				<template #row_name="{ row }">
					<el-text tag="b">{{ row.code }}</el-text>
					<br>
					<el-text>{{ row.name }}</el-text>
				</template>
				<template #row_position="{ row }">
					<el-text>{{ state.locationList?.find((item: any) => item.code == row.location)?.name }}</el-text>
					<el-text>Km{{ row.kmNumber ?? 0 }}+{{ row.metNumber ?? 0 }}</el-text>
				</template>
				<template #row_state="{ row }">
					<TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.TmsIncidentStatus, row.state)" />
				</template>
				<template #row_startDate="{ row }">
					<el-text v-if="row.startDate != null">
						{{ useDateFormat(row.startDate, DateTimeFormatEnum.DateDefault) }}
					</el-text>
					<br>
					<el-text v-if="row.startDate != null">
						{{ useDateFormat(row.startDate, DateTimeFormatEnum.TimeDefault) }}
					</el-text>
				</template>
				<template #row_record="{ row }">
					<ModifyRecord :data="row" />
				</template>
				<template #row_buttons="{ row }">
					<el-tooltip :content="$t('lz.button.base.edit')" placement="top" :auto-close="1000">
						<el-button icon="ele-Edit" text type="primary" v-if="row.state == TmsIncidentStateEnum.Registration" v-auth="'tmsIncident:update'" @click="handleEdit(row)"> </el-button>
						<div v-else></div>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.cancel')" placement="top">
						<el-button icon="ele-Delete" text type="danger" v-if="(row.state == TmsIncidentStateEnum.Registration || row.state == TmsIncidentStateEnum.Verified || row.state == TmsIncidentStateEnum.InProgress)" v-auth="'tmsIncident:update'" @click="handleUpdateState(row)"> </el-button>
						<div v-else></div>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.base.view')" placement="top">
						<el-button icon="ele-View" text type="primary" v-auth="'tmsIncident:view'" @click="handleView(row)"> </el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.tmsIncident.timeline')" placement="top">
						<el-button icon="ele-Memo" text type="success" v-auth="'tmsIncident:view'" @click="openDetailPage(row)"> </el-button>
					</el-tooltip>
					<el-tooltip :content="$t('lz.button.tmsIncident.report')" placement="top">
						<el-button icon="ele-Document" text type="success" v-auth="'tmsIncident:view'" @click="openReportPage(row)"> </el-button>
					</el-tooltip>
				</template>
			</vxe-grid>
		</el-card>

		<detailIncidentComp ref="detailIncidentCompRef" :title="state.title" :operateType="state.operateType"/>
		<editIncidentComp ref="editIncidentCompRef" />
	</div>
</template>

<script lang="ts" setup name="tmsIncident">
import { incidentEventTypeDisplay } from '/@/utils/incidentEventTypeDisplay';
	import { onActivated, onMounted, reactive, ref, defineAsyncComponent, watch, nextTick } from 'vue';
	import { ElMessageBox, ElMessage } from 'element-plus';
	import { useI18n } from 'vue-i18n';
	import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
	import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
	import { getAPI } from '/@/utils/axios-utils';
	import { OperateType } from '/@/types/constants/operate';
	import ModifyRecord from '/@/components/table/modifyRecord.vue';
	import { useDateFormat } from '@vueuse/core';

	import { TmsIncidentApi } from '/@/api-services/tms';
	import { PageTmsIncidentInput, PageTmsIncidentOutput } from '/@/api-services/tms';
	import { useTmsStore } from '/@/stores/tmsStore';
	const tmsStore = useTmsStore();

	import { Picture } from '@element-plus/icons-vue';

	import detailIncidentComp from '/@/views/tms/incident/component/detailIncident.vue';
	const detailIncidentCompRef = ref<InstanceType<typeof detailIncidentComp>>();

	import editIncidentComp from '/@/views/tms/incident/component/editIncident.vue';
	const editIncidentCompRef = ref<InstanceType<typeof editIncidentComp>>();

	import { useIncidentMonitor } from '/@/stores/its/incidentMonitor';
	const incidentMonitor = useIncidentMonitor();

	import { useTransporterIncident } from '/@/transporter/hooks/useTransporterIncident';

	import { useCommonVarsAndFuncs } from '/@/stores/its/commonVarsAndFuncs';
	const commonVarsAndFuncs = useCommonVarsAndFuncs();

	import { useBaseConfig } from '/@/stores/baseConfig';
	import { BaseConfigTypeEnum } from '/@/types/enums';
	const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
	const getConfigItemByCode = useBaseConfig().getConfigItemByCode;

	const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));
	
	import { DateTimeFormatEnum, EntityFieldLength } from '/@/types/enums/baseEnum';
	import { TmsIncidentStateEnum, TmsReportRouteEnum } from '/@/types/enums';

	import lineChartComp from '/@/views/tms/incident/component/lineChart.vue';
	const lineChartRef = ref<InstanceType<typeof lineChartComp>>();

	import incidentCountComp from '/@/views/tms/incident/component/incidentCount.vue';

	const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));
	const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));
	const { t: $t } = useI18n();
	const xGridIncident = ref<VxeGridInstance>();
	const selectedGroups = ref(['event', 'incident']);

	const state = reactive({
		queryParams: {
			name: undefined as any,
			eventTypeId: undefined as any,
			zoneId: undefined as any,
			source: undefined as any,
			state: undefined as any,
			fromDate: commonVarsAndFuncs.getISODatetimeFromDays(-90, 'start') as any,
			toDate: commonVarsAndFuncs.getISODatetimeFromDays(1, 'start') as any,
			fromKm: undefined as any,
			fromMet: undefined as any,
			toKm: undefined as any,
			toMet: undefined as any,
			pageSize: 50 as number,
			chartDataType: 'day' as any,
		},
		localPageParam: {
			pageSize: 50 as number,
			defaultSort: { field: 'startDate', order: 'desc', descStr: 'desc' } as any,
		},
		title: '',
		dataChangedCount: 0,
		operateType: 'add' as OperateType,
		showBatchDelete: false,
		enabledBatchDelete: false,
		sourceDetectList: [] as any,
		eventTypeList: [] as any,
		locationList: [] as any,
		laneList: [] as any,
		zoneList: [] as any,
		incidentStatusList: [] as any,
		lineChartData: {} as any,
		incidentCount: {} as any,
		incidentChartDataTypeName: 'lz.label.base.day',
		showChartLegend: true,
	});

	const incidentChartDataType = ref('day');
	
	// Declare the parameters for the grid options
	const options = useVxeTable<PageTmsIncidentOutput>(
		{
			id: 'tmsIncidentGrid',
			name: 'tmsIncidentGrid',
			columns: [
				{ type: 'checkbox', fixed: 'left', width: 45 },

				// Fields
				{ field: 'name', title: $t('lz.entity.base.name'), minWidth: 300, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_name' } },
				{ field: 'eventTypeId', title: $t('lz.entity.tms.eventType'), minWidth: 250, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_eventTypeId' } },
				{ field: 'startDate', title: $t('lz.entity.tmsIncident.startDate'), minWidth: 100, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_startDate' } },
				{ field: 'createdBy', title: $t('lz.entity.base.createBy'), minWidth: 100, showOverflow: 'tooltip' },
				{ field: 'location', title: $t('lz.label.base.location'), minWidth: 100, showOverflow: 'tooltip', slots: { default: 'row_position' } },
				{ field: 'state', title: $t('lz.entity.base.status'), minWidth: 100, showOverflow: 'tooltip', sortable: true, slots: { default: 'row_state' } },

				// Actions
				{ title: $t('lz.label.base.audit'), width: 100, showOverflow: 'tooltip', slots: { default: 'row_record' } },
				{ title: $t('lz.label.base.action'), fixed: 'right', width: 150, showOverflow: true, slots: { default: 'row_buttons' } },
			]
		},
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
				height: 60
			}
		}
	);

	// Page initialization
	onActivated(async () => {
		state.zoneList = (await tmsStore.getZones()).filter(item => item.type == 'zone').sort((a, b) => (a.name ?? '').localeCompare(b.name ?? ''));
		state.eventTypeList = await tmsStore.getEventTypes();
	});
	onMounted(async () => {
		state.sourceDetectList = getConfigDataByCode(BaseConfigTypeEnum.TmsSourceDetectIncident);
		state.locationList = getConfigDataByCode(BaseConfigTypeEnum.TmsLocationType);
		state.incidentStatusList = getConfigDataByCode(BaseConfigTypeEnum.TmsIncidentStatus);

		// TODO: LaneList is temporarily DirectionList => by direction_config_type not tms_lane
		state.laneList = getConfigDataByCode(BaseConfigTypeEnum.TmsDirectionType);

		state.zoneList = (await tmsStore.getZones()).filter(item => item.type == "zone").sort((a:any, b:any) => (a.name ?? '').localeCompare(b.name ?? ''));
		state.eventTypeList = await tmsStore.getEventTypes();

		await handleQuery();
	});

	// Query API
	const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
		const params: PageTmsIncidentInput = {
			...state.queryParams,
			page: page.currentPage,
			pageSize: page.pageSize,
			field: sort.field,
			order: sort.order,
			descStr: 'desc',
			includeEvents: selectedGroups.value.includes('event'),
			includeIncidents: selectedGroups.value.includes('incident'),
		};

		return await getAPI(TmsIncidentApi).apiTmsTmsincidentPagePost(params);
	};

	// Query operation
	const handleQuery = async () => {
		options.loading = true;
		await xGridIncident.value?.commitProxy('reload') ;
		options.loading = false;
		selectChangeEvent();

		getIncidentChart();
		getIncidentCount();
	};

	// Reset operation
	const resetQuery = async () => {
		state.queryParams.name = undefined;
		state.queryParams.eventTypeId = undefined;
		state.queryParams.zoneId = undefined;
		state.queryParams.source = undefined;
		state.queryParams.state = undefined;
		state.queryParams.fromDate = commonVarsAndFuncs.getISODatetimeFromDays(-90, 'start');
		state.queryParams.toDate = commonVarsAndFuncs.getISODatetimeFromDays(1, 'start');
		state.queryParams.fromKm = undefined;
		state.queryParams.fromMet = undefined;
		state.queryParams.toKm = undefined;
		state.queryParams.toMet = undefined;
		selectedGroups.value = ['event', 'incident'];

		await xGridIncident.value?.commitProxy('reload');

		incidentChartDataType.value = 'day';
		getIncidentChart();
		getIncidentCount();
	};

	// Open add dialog
	const handleAdd = () => {
		const title = $t('lz.label.base.titleAdd', {name: $t('lz.label.tms.tmsIncident')});
		incidentMonitor.setIncidentDialogState(true, title);
	};

	// Open edit dialog
	const handleEdit = (row: any) => {
		const title = $t('lz.label.base.titleEdit', {name: $t('lz.label.tms.tmsIncident')});
		incidentMonitor.setIncidentDialogState(true, title, true, row);
	};

	// Open copy dialog
	const handleView = (row: any) => {
		state.title = $t('lz.label.base.titleView', {name: $t('lz.label.tms.tmsIncident')});
		state.operateType = 'view';
		detailIncidentCompRef.value?.openDialog(row);
	};

	// Update the state of a record
	const handleUpdateState = (row: any) => {
		let message = $t('lz.message.tmsIncident.cancelIncidentConfirm', {name:row.name});
		if(row.state == TmsIncidentStateEnum.Registration)
			message = $t('lz.message.tmsIncident.skipIncidentConfirm', {name:row.name});

		ElMessageBox.confirm(message, $t('lz.label.base.confirm'), {
			confirmButtonText: $t('lz.button.base.confirm'),
			cancelButtonText: $t('lz.button.base.cancel'),
			type: 'warning',
		})
		.then(async () => {
			let newState = null;
			if(row.state == TmsIncidentStateEnum.Registration)
				newState = TmsIncidentStateEnum.Skipped;
			else if(row.state == TmsIncidentStateEnum.Verified)
				newState = TmsIncidentStateEnum.Canceled;
			else if(row.state == TmsIncidentStateEnum.InProgress)
				newState = TmsIncidentStateEnum.Canceled;

			if(newState){
				const response = await getAPI(TmsIncidentApi).apiTmsTmsincidentUpdatestatePost({
					id: row.id, 
					state: newState 
				});

				if(response.data.result){
					handleQuery();
					ElMessage.success($t('lz.message.base.processSuccess'));
				}
				else
				ElMessage.success($t('lz.message.base.processUnSuccess'));
			}
			else
				ElMessage.success($t('lz.message.base.invalidData'));
		})
		.catch(() => { });
	};

	// Custom column
	function handleCustom() {
		xGridIncident.value?.openCustom();
	}

	function selectChangeEvent() {
		// state.showBatchDelete = getCheckedRecords().length > 0;
	}

	// Table events
	const gridEvents: VxeGridListeners<PageTmsIncidentOutput> = {
		// Only effective when pager-config is configured, this event will be triggered when pagination changes
		async pageChange({ pageSize }) {
			state.localPageParam.pageSize = pageSize;
		},
		// This event will be triggered when sorting conditions change
		async sortChange({ field, order }) {
			state.localPageParam.defaultSort = { field: field, order: order!, descStr: 'desc' };
		},
	};

	const onChartDataTypeChange = async() => {
		getIncidentChart();
		getIncidentCount();
	};

	const getIncidentChart = async() => {
		const queryParams = { ...state.queryParams, page: 1, pageSize: 1000000, includeEvents: selectedGroups.value.includes('event'), includeIncidents: selectedGroups.value.includes('incident'), chartDataType: incidentChartDataType.value };

		const response = await getAPI(TmsIncidentApi).apiTmsTmsincidentIncidentchartPost(queryParams);
		state.lineChartData = response.data.result;
		state.dataChangedCount++;

		if(incidentChartDataType.value == 'time')
			state.incidentChartDataTypeName = 'lz.label.base.timeRange';
		else
			state.incidentChartDataTypeName = 'lz.label.base.day';
	};

	const getIncidentCount = async() => {
		const queryParams = { ...state.queryParams, page: 1, pageSize: 1000000, includeEvents: selectedGroups.value.includes('event'), includeIncidents: selectedGroups.value.includes('incident'), chartDataType: incidentChartDataType.value };

		const response = await getAPI(TmsIncidentApi).apiTmsTmsincidentCountincidentPost(queryParams);
		state.incidentCount = response.data.result;
	};

	// Open the report page
	const openReportPage = (row: any) =>{
    	window.open(TmsReportRouteEnum.IncidentReportDetail + row.id, '_blank');
	};

	const onLineChartMounted = async() => {
		await nextTick();

		lineChartRef.value?.generateChart();
	};

	// Open the timeline-detail page
	const openDetailPage = (row: any) =>{
		window.open(TmsReportRouteEnum.IncidentTimelineDetail + row.id, '_blank');
	};

	// Lắng nghe sự cố realtime qua mittBus (NATS) => thay cho watch store cũ
	useTransporterIncident(() => {
		handleQuery();
	}, { immediate: true });
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

	.div-incident-count{
		.item{
			margin-top: 5px;
			height: 55px;
			width: 100%;
			color: white;
			border-radius: 5px;
			display: flex;
			flex-direction: row;
			gap: 10px;

			.icon{
				height: 55px;
				width: 55px;
				display: flex;
				justify-content: center;
				align-items: center;
			}

			.content{
				height: 55px;
				display: flex;
				flex-direction: column;
				justify-content: center;
				gap: 5px;

				.title{
					font-size: 14px;
					margin: 0px;
				}

				.amount{
					font-size: 20px;
					margin: 0px;
					font-weight: bold;
				}
			}
		}
	}
</style>
