<template>
	<div class="tms-map-container">
		<el-card shadow="hover"
			:body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', height: '100%', alignItems: 'start' }">
			<el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true"
				label-width="auto" style="flex: 1 1 0%">
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.startTime')">
							<el-date-picker v-model="state.queryParams.startTime" type="datetime" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.startTime')})" 
							:shortcuts="shortcuts"  :clearable="false"/>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.endTime')">
							<el-date-picker v-model="state.queryParams.endTime" type="datetime" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.endTime')})" 
							:shortcuts="shortcuts" :clearable="false"/>
						</el-form-item>
					</el-col>

					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.tms.direction')">
							<el-select v-model="state.queryParams.direction" :placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.entity.tms.direction')})" 
										clearable @clear="state.queryParams.direction = undefined">
									<el-option v-for="item in state.queryParams.directionList" :label="item.name" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>

					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.tmsTrafficAnalysis.type')">
							<el-select v-model="state.queryParams.vehicleType" :placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.entity.tmsTrafficAnalysis.type')})" 
										clearable @clear="state.queryParams.vehicleType = undefined">
									<el-option v-for="item in state.queryParams.vehicleTypeList" :label="item.name" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
				</el-row>
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.tmsTrafficAnalysis.lengthFrom')">
							<el-input-number v-model="state.queryParams.lengthFrom" 
								:min="0"
								:placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.tmsTrafficAnalysis.lengthFrom')})"
								 clearable
								@keyup.enter.native="handleQuery" />
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.tmsTrafficAnalysis.lengthTo')">
							<el-input-number v-model="state.queryParams.lengthTo" 
								:min="0"
								:placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.tmsTrafficAnalysis.lengthTo')})"
								 clearable
								@keyup.enter.native="handleQuery" />						
						</el-form-item>						
					</el-col>

					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.tms.licensePlate')" 
							:rules="[{max: 10, message: $t('lz.validation.base.maxlength', {PropertyName: $t('lz.entity.tms.licensePlate')})}]">
							<el-input v-model="state.queryParams.licensePlate" 
								:placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.tms.licensePlate')})"
								 clearable
								 :maxlength="20"
								 show-word-limit
								@keyup.enter.native="handleQuery" />						
						</el-form-item>			
					</el-col>

					
				</el-row>
			</el-form>

			<el-divider style="height: calc(100% - 5px); margin: 0 10px" direction="vertical" />
			<el-row>
				<SearchButton :auth-search="'tmsTrafficAnalysis:page'" @search="handleQuery" @reset="resetQuery" :loading="options.loading" />				
			</el-row>
		</el-card>

        <el-card class="full-table" shadow="hover" style="margin-top: 5px">
			<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
				<template #empty>
					<el-empty :image-size="200" />
				</template>
				<template #row_type="{ row }">
					<TagInfo :config-item="state.queryParams.vehicleTypeList.find((item: any) => item.code === row.type)" />
          		</template>		
				<template #row_direction="{ row }">
            		<TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.TmsDirectionType, row.direction)" />
          		</template>		
				<template #row_buttons="{ row }">
					<el-tooltip :content="$t('lz.button.base.detail')" placement="top">
						<el-button icon="ele-View" text type="primary" v-auth="'tmsTrafficAnalysis:view'" @click="handleView(row)"> </el-button>
					</el-tooltip>					
				</template>
				<template #header_name>
				</template>
			</vxe-grid>
		</el-card>		
	<trafficAnalysisViewDetailModal ref="modalRef" :isShowDialog="state.isShowDialog" :defaultValue="state.defaultValue"/>

	</div>
</template>

	

<script lang="ts" setup>
import { onMounted, reactive, ref, defineAsyncComponent } from 'vue';
import { useDateTimeShortCust } from '/@/hooks/dateTimeShortCust';
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { Local } from '/@/utils/storage';
import { getAPI } from '/@/utils/axios-utils';
import { useI18n } from 'vue-i18n';
import { useNow, useDateFormat } from '@vueuse/core'; // Import useNow
import { DATE_TIME_FORMAT } from '/@/types/formats';
import { PageTrafficDataOutput, VdsTrafficDataApi } from '/@/api-services/vds';
import { vehicleCountField } from '/@/views/bigScreen/trafficInfo/component/vehicleCountField';
import { vdsTimestamp } from '/@/utils/vdsTimestamp';
import { ElMessage } from 'element-plus';
type PageTrafficAnalysisOutput = PageTrafficDataOutput & { type?: string | null; length?: number | null; id?: string };
import { useBaseConfig } from '/@/stores/baseConfig';
import { BaseConfigTypeEnum, SysConfigTypeCodeEnum } from '/@/types/enums';
import trafficAnalysisViewDetailModal from '/@/views/modal/trafficAnalysis/trafficAnalysisViewDetailModal.vue';


const { t: $t } = useI18n();
const now = useNow(); // Get the current date and time

const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));

const xGrid = ref<VxeGridInstance>();
const shortcuts = useDateTimeShortCust().map(item => ({ text: $t(item.text), value: item.value }));

const modalRef = ref<InstanceType<typeof trafficAnalysisViewDetailModal> | null>(null);
const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
const getOpConfigItemByCode = useBaseConfig().getOpConfigItemByCode;
const getConfigItemByCode = useBaseConfig().getConfigItemByCode;
const queryForm = ref();

const state = reactive({
	queryParams: {
		startTime: new Date(new Date(now.value.getTime() - 3 * 24 * 60 * 60 * 1000).setHours(0, 0, 0, 0)), // Set to 0:00 three days ago
		endTime: new Date(new Date(now.value).setHours(23, 59, 59, 999)), // Set to today at 23:59:59
		direction: undefined,
		vehicleType: undefined,
		lengthFrom: undefined,
		lengthTo: undefined,
		licensePlate: undefined,
		directionList:[] as any,
		vehicleTypeList:[] as any
	},
	localPageParam: {
		pageSize: 50 as number,
		defaultSort:  { field: 'detectTime', order: 'desc', descStr: 'desc' },
	},
	visible: false,		
	isShowDialog: false,
	defaultValue: {} as Record<string, any>
});

// Local storage parameters
const localPageParamKey = 'localPageParam:rptVdsTrafficAnalysisGrid';
let latestQueryId = 0;
let latestQuery: ReturnType<VdsTrafficDataApi['apiVdsVdstrafficdataPagePost']> | undefined;

// Table parameter configuration
const options = useVxeTable<PageTrafficAnalysisOutput>(
	{
		id: 'rptVdsTrafficAnalysisGrid',
		name: 'TrafficAnalysis',
		columns: [
			{ type: 'checkbox', fixed: 'left', width: 45 },
			{ field: 'detectTime', title: $t('lz.entity.tmsTrafficAnalysis.detectTime'), minWidth: 80, showOverflow: 'tooltip', 
					sortable: false, formatter: ({ cellValue }: any) => useDateFormat(vdsTimestamp(cellValue),DATE_TIME_FORMAT)?.value },
			{ field: 'licensePlate', title: $t('lz.entity.tms.licensePlate'), minWidth: 150, showOverflow: 'tooltip', sortable: false, },
			{ field: 'type', title: $t('lz.entity.tmsTrafficAnalysis.type'), minWidth: 150, showOverflow: 'tooltip', sortable: false, slots: { default: 'row_type' }},
            { field: 'direction', title: $t('lz.entity.tms.direction'), minWidth: 150, showOverflow: 'tooltip', sortable: false, slots: { default: 'row_direction' }},
            { field: 'length', title: $t('lz.entity.tms.length'), minWidth: 150, showOverflow: 'tooltip', sortable: false},
            // Actions
			{ title: $t('lz.label.base.action'), fixed: 'right', width: 100, showOverflow: true, slots: { default: 'row_buttons' } },
        ],
	},
	// vxeGrid configuration parameters (can override any parameters), refer to vxe-table official documentation
	{
		// Proxy configuration
		proxyConfig: { autoLoad: false, ajax: { query: async ({ page, sort }) => await handleQueryApi(page, sort) } },
		// Sorting configuration
		sortConfig: { defaultSort: Local.get(localPageParamKey)?.defaultSort || state.localPageParam.defaultSort },
		// Pagination configuration
		pagerConfig: { pageSize: Local.get(localPageParamKey)?.pageSize || state.localPageParam.pageSize },
		// Toolbar configuration
		toolbarConfig: { export: false }
	}
);


// Page initialization
onMounted(async () => {
	if (Object.keys(useBaseConfig().configList ?? {}).length === 0) await useBaseConfig().setConfigDataList();
	state.queryParams.directionList = getConfigDataByCode(SysConfigTypeCodeEnum.TmsDirectionConfigCode);
	const configured = getConfigDataByCode(SysConfigTypeCodeEnum.TmsVehicleTypeConfigCode) ?? [];
	state.queryParams.vehicleTypeList = ['short', 'medium', 'long', 'other'].map(code => {
		const item = configured.find((item: any) => vehicleCountField(item) === code);
		return { ...item, code, name: item?.name ?? ({ short: 'S', medium: 'M', long: 'L', other: 'Khác' }[code]) };
	});
	state.localPageParam = Local.get(localPageParamKey) || state.localPageParam;
	await handleQuery();
	
});

// Query API
const handleQueryApi =  async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
	const { startTime, endTime, direction, vehicleType, lengthFrom, lengthTo, licensePlate } = state.queryParams;
	if (!(startTime instanceof Date) || !(endTime instanceof Date) || !Number.isFinite(startTime.getTime()) || !Number.isFinite(endTime.getTime())
		|| startTime >= endTime || endTime.getTime() - startTime.getTime() > 60 * 86400000) {
		ElMessage.error('Chọn khoảng thời gian hợp lệ, tối đa 60 ngày.');
		throw new Error('Invalid VDS time range');
	}
	const queryId = ++latestQueryId;
	const request = getAPI(VdsTrafficDataApi).apiVdsVdstrafficdataPagePost({
		page: page.currentPage, pageSize: page.pageSize, fromDate: startTime, toDate: endTime,
		direction, vehicleSize: vehicleType, lengthFrom, lengthTo, licensePlate,
	}).then(response => {
		if (response.data.result?.records) response.data.result.records = response.data.result.records.map(row => ({
			...row, id: String(row.vendorEventId) + '@' + String(row.detectTime), type: row.vehicleSize,
			length: row.lengthMm, ipAddress: row.cameraIp,
		}));
		return response;
	});
	latestQuery = request;
	const response = await request;
	return queryId === latestQueryId ? response : latestQuery!;
};

// Query operation
const handleQuery = async () => {
	options.loading = true;
	try { await xGrid.value?.commitProxy('reload'); }
	finally { options.loading = false; }
};

// Reset operation
const resetQuery = async () => {
	state.queryParams.direction = undefined;
	state.queryParams.vehicleType = undefined;
	state.queryParams.startTime =  new Date(new Date(now.value.getTime() - 3 * 24 * 60 * 60 * 1000).setHours(0, 0, 0, 0));
	state.queryParams.endTime = new Date(new Date(now.value).setHours(23, 59, 59, 999));
	state.queryParams.licensePlate = undefined;
	state.queryParams.lengthFrom = undefined;
	state.queryParams.lengthTo = undefined;

	// Call vxe-grid's commitProxy(reload) method to trigger the table to reload data
	await xGrid.value?.commitProxy('reload');
};

// Event Handle view detail
const handleView = (row: any) => {
	const direction = state.queryParams.directionList.find((item: any) => item.code == row?.direction)
	const vehicleType = state.queryParams.vehicleTypeList.find((item: any) => item.code == row?.type)
	state.defaultValue = {
		...row,
		directionName: direction?.name ?? row.direction ?? '',
		vehicleTypeName: vehicleType?.name ?? row.type ?? '',
	};
	modalRef.value?.showModal(true);
};

// Table events
const gridEvents: VxeGridListeners<PageTrafficAnalysisOutput> = {
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


</script>
