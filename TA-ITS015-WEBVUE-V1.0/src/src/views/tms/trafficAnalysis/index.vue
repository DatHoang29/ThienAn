<template>
	<div class="tms-map-container">
		<el-card
			shadow="hover"
			:body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', height: '100%', alignItems: 'start' }"
		>
			<el-form
				ref="queryForm"
				:model="state.queryParams"
				:show-message="false"
				:inline-message="true"
				label-width="auto"
				label-position="left"
				style="flex: 1 1 0%"
			>
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.startTime')">
							<DatePicker
								v-model="state.queryParams.startTime"
								dateFormat="dd/mm/yy"
								showTime
								showSeconds
								size="small"
								class="ta-input-size"
								:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.startTime') })"
							/>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.endTime')">
							<DatePicker
								v-model="state.queryParams.endTime"
								dateFormat="dd/mm/yy"
								showTime
								showSeconds
								size="small"
								class="ta-input-size"
								:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.endTime') })"
							/>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.tms.direction')">
							<el-select
								v-model="state.queryParams.direction"
								class="ta-input-size"
								size="small"
								:placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.tms.direction') })"
								clearable
								@clear="state.queryParams.direction = undefined"
							>
								<el-option v-for="item in state.queryParams.directionList" :key="item.code" :label="item.name" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.tms.vehicleSize')">
							<el-select
								v-model="state.queryParams.vehicleType"
								class="ta-input-size"
								size="small"
								:placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.tms.vehicleSize') })"
								clearable
								@clear="state.queryParams.vehicleType = undefined"
							>
								<el-option v-for="item in state.queryParams.vehicleTypeList" :key="item.code" :label="item.name" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
				</el-row>
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.tms.lengthFrom')">
							<el-input-number
								v-model="state.queryParams.lengthFrom"
								:min="0"
								class="ta-input-size"
								size="small"
								:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.tms.lengthFrom') })"
								clearable
								@keyup.enter.native="handleQuery"
							/>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.tms.lengthTo')">
							<el-input-number
								v-model="state.queryParams.lengthTo"
								:min="0"
								class="ta-input-size"
								size="small"
								:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.tms.lengthTo') })"
								clearable
								@keyup.enter.native="handleQuery"
							/>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item
							:label="$t('lz.entity.tms.licensePlate')"
							:rules="[{ max: 10, message: $t('lz.validation.base.maxlength', { PropertyName: $t('lz.entity.tms.licensePlate') }) }]"
						>
							<el-input
								v-model="state.queryParams.licensePlate"
								class="ta-input-size"
								size="small"
								:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.tms.licensePlate') })"
								clearable
								:maxlength="20"
								show-word-limit
								@keyup.enter.native="handleQuery"
							/>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.tms.equipment')">
							<el-select
								v-model="state.queryParams.equipmentId"
								class="ta-input-size"
								size="small"
								filterable
								clearable
								:placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.tms.equipment') })"
								@clear="state.queryParams.equipmentId = undefined"
							>
								<el-option
									v-for="item in state.equipmentList"
									:key="item.id"
									:label="getEquipmentDisplay(item)"
									:value="item.id"
								/>
							</el-select>
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
					<TagInfo :config-item="state.queryParams.vehicleTypeList.find(item => item.code === row.type)" />
				</template>
				<template #row_direction="{ row }">
					<TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.TmsDirectionType, row.direction)" />
				</template>
				<template #row_equipment="{ row }">
					{{ getEquipmentDisplayById(row.equipmentId) }}
				</template>
				<template #row_buttons="{ row }">
					<el-tooltip :content="$t('lz.button.base.detail')" placement="top">
						<el-button icon="ele-View" text type="primary" v-auth="'tmsTrafficAnalysis:view'" @click="handleView(row)" />
					</el-tooltip>
				</template>
			</vxe-grid>
		</el-card>
		<trafficAnalysisViewDetailModal ref="modalRef" :default-value="state.defaultValue" />
	</div>
</template>

<script lang="ts" setup>
import { defineAsyncComponent, onActivated, onMounted, reactive, ref } from 'vue';
import { vdsTimestamp } from '/@/utils/vdsTimestamp';
import { useDateFormat, useNow } from '@vueuse/core';
import { useI18n } from 'vue-i18n';
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useTmsStore } from '/@/stores/tmsStore';
import { PageTrafficDataInput, PageTrafficDataOutput, VdsTrafficDataApi } from '/@/api-services/vds';
import { vehicleCountField } from '/@/views/bigScreen/trafficInfo/component/vehicleCountField';
type PageTrafficAnalysisOutput = PageTrafficDataOutput & { id?: string; type?: string | null; length?: number | null; ipAddress?: string | null };
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { useBaseConfig } from '/@/stores/baseConfig';
import { BaseConfigTypeEnum, SysConfigTypeCodeEnum } from '/@/types/enums';
import { DATE_TIME_FORMAT } from '/@/types/formats';
import { getAPI } from '/@/utils/axios-utils';
import { Local } from '/@/utils/storage';
import DatePicker from 'primevue/datepicker';
import trafficAnalysisViewDetailModal from '/@/views/modal/trafficAnalysis/trafficAnalysisViewDetailModal.vue';

type TrafficAnalysisQueryInput = PageTrafficDataInput & {
	equipmentId?: string;
};

const trafficEquipmentTypeCodes = ['CCTV', 'VDS'];

const { t: $t } = useI18n();
const now = useNow();
const baseConfig = useBaseConfig();
const tmsStore = useTmsStore();
const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));
const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));
const xGrid = ref<VxeGridInstance>();
const queryForm = ref();
const modalRef = ref<InstanceType<typeof trafficAnalysisViewDetailModal> | null>(null);
const getConfigItemByCode = baseConfig.getConfigItemByCode;

const state = reactive({
	queryParams: {
		startTime: new Date(new Date(now.value.getTime() - 3 * 24 * 60 * 60 * 1000).setHours(0, 0, 0, 0)),
		endTime: new Date(new Date(now.value).setHours(23, 59, 59, 999)),
		direction: undefined as string | undefined,
		vehicleType: undefined as string | undefined,
		lengthFrom: undefined as number | undefined,
		lengthTo: undefined as number | undefined,
		licensePlate: undefined as string | undefined,
		equipmentId: undefined as string | undefined,
		directionList: [] as any[],
		vehicleTypeList: [] as any[],
	},
	equipmentList: [] as any[],
	localPageParam: {
		pageSize: 50,
		defaultSort: { field: 'detectTime', order: 'desc', descStr: 'desc' },
	},
	defaultValue: {} as Record<string, any>,
});

const localPageParamKey = 'localPageParam:tmsTrafficAnalysisGrid';
let latestQueryId = 0;
let latestQuery: ReturnType<VdsTrafficDataApi['apiVdsVdstrafficdataPagePost']> | undefined;

const formatEquipmentPosition = (equipment: any) => {
	if (equipment?.kmNumber == null && equipment?.metNumber == null) return '';
	return `Km ${equipment.kmNumber ?? 0}+${String(equipment.metNumber ?? 0).padStart(3, '0')}`;
};

const getEquipmentDisplay = (equipment: any) => {
	if (!equipment) return '-';
	const name = equipment.name || equipment.code || String(equipment.id ?? '');
	const position = formatEquipmentPosition(equipment);
	return position ? `${name} (${position})` : name;
};

const getEquipmentDisplayById = (equipmentId?: string | null) => {
	if (!equipmentId) return '-';
	const equipment = state.equipmentList.find((item: any) => item.id === equipmentId);
	return equipment ? getEquipmentDisplay(equipment) : String(equipmentId);
};

const hasTrafficEquipmentTypeCode = (equipmentType: any) => {
	const code = String(equipmentType?.code ?? '').toUpperCase();
	return trafficEquipmentTypeCodes.some((allowedCode) => code === allowedCode || code.includes(allowedCode));
};

const options = useVxeTable<PageTrafficAnalysisOutput>(
	{
		id: 'tmsTrafficAnalysisGrid',
		name: 'TrafficAnalysis',
		columns: [
			// { type: 'checkbox', fixed: 'left', width: 45 },
			{
				field: 'detectTime',
				title: $t('lz.entity.tms.detectTime'),
				minWidth: 80,
				showOverflow: 'tooltip',
				sortable: false,
				formatter: ({ cellValue }: any) => useDateFormat(cellValue ? vdsTimestamp(cellValue) : undefined, DATE_TIME_FORMAT).value,
			},
			{ field: 'licensePlate', title: $t('lz.entity.tms.licensePlate'), minWidth: 150, showOverflow: 'tooltip', sortable: false },
			{ field: 'equipmentId', title: $t('lz.entity.tms.equipment'), minWidth: 220, showOverflow: 'tooltip', slots: { default: 'row_equipment' } },
			{ field: 'type', title: $t('lz.entity.tms.vehicleSize'), minWidth: 150, showOverflow: 'tooltip', sortable: false, slots: { default: 'row_type' } },
			{ field: 'direction', title: $t('lz.entity.tms.direction'), minWidth: 150, showOverflow: 'tooltip', sortable: false, slots: { default: 'row_direction' } },
			{ field: 'length', title: $t('lz.entity.tms.length'), minWidth: 150, showOverflow: 'tooltip', sortable: false },
			{ title: $t('lz.label.base.action'), fixed: 'right', width: 100, showOverflow: true, slots: { default: 'row_buttons' } },
		],
	},
	{
		proxyConfig: { autoLoad: false, ajax: { query: async ({ page, sort }) => await handleQueryApi(page, sort) } },
		sortConfig: { defaultSort: Local.get(localPageParamKey)?.defaultSort || state.localPageParam.defaultSort },
		pagerConfig: { pageSize: Local.get(localPageParamKey)?.pageSize || state.localPageParam.pageSize },
		toolbarConfig: { export: false },
	}
);

const loadFilterOptions = async () => {
	try {
		await tmsStore.ensureBaseConfig();
	} catch (error) {
		console.error('Không thể tải cấu hình hướng và loại phương tiện.', error);
	}

	try {
		const [equipmentList, equipmentTypeList] = await Promise.all([
			tmsStore.getEquipment(),
			tmsStore.getEquipmentTypes(),
		]);
		const allowedEquipmentTypeIds = new Set(
			equipmentTypeList
				.filter(hasTrafficEquipmentTypeCode)
				.map((equipmentType: any) => String(equipmentType.id))
		);
		state.equipmentList = equipmentList
			.filter((equipment: any) => allowedEquipmentTypeIds.has(String(equipment.equipmentTypeId)))
			.sort((first: any, second: any) => (first.name ?? '').localeCompare(second.name ?? ''));
	} catch (error) {
		console.error('Không thể tải danh sách thiết bị CCTV/VDS.', error);
	}

	state.queryParams.directionList = baseConfig.configList?.[SysConfigTypeCodeEnum.TmsDirectionConfigCode] ?? [];
	const configured = baseConfig.configList?.[SysConfigTypeCodeEnum.TmsVehicleTypeConfigCode] ?? [];
 state.queryParams.vehicleTypeList = ['short', 'medium', 'long', 'other'].map(code => {
 const item = configured.find(item => vehicleCountField(item) === code);
 return { ...item, code, name: item?.name ?? ({ short: 'S', medium: 'M', long: 'L', other: $t('lz.label.base.other') }[code]) };
 });
};

onActivated(() => { void loadFilterOptions(); });
onMounted(async () => {
	await loadFilterOptions();
	state.localPageParam = Local.get(localPageParamKey) || state.localPageParam;
	await handleQuery();
});

const handleQueryApi = async (
	page: VxeGridPropTypes.ProxyAjaxQueryPageParams,
	sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams
) => {
	const params: TrafficAnalysisQueryInput = {
		page: page.currentPage,
		pageSize: page.pageSize,
		field: sort.field,
		order: sort.order,
		descStr: 'desc',
		fromDate: state.queryParams.startTime,
		toDate: state.queryParams.endTime,
		direction: state.queryParams.direction,
		vehicleSize: state.queryParams.vehicleType,
		lengthFrom: state.queryParams.lengthFrom,
		lengthTo: state.queryParams.lengthTo,
		licensePlate: state.queryParams.licensePlate,
		equipmentId: state.queryParams.equipmentId,
	};

	const queryId = ++latestQueryId;
	const request = getAPI(VdsTrafficDataApi).apiVdsVdstrafficdataPagePost(params).then(response => {
		const result = response.data.result;
		if (result?.records) result.records = result.records.map(row => ({
			...row, id: String(row.vendorEventId) + '@' + String(row.detectTime), type: row.vehicleSize,
			length: row.lengthMm, ipAddress: row.cameraIp,
		}));
		return response;
	});
	latestQuery = request;
	const response = await request;
	return queryId === latestQueryId ? response : latestQuery!;
};

const handleQuery = async () => {
	options.loading = true;
	try {
		await xGrid.value?.commitProxy('reload');
	} finally {
		options.loading = false;
	}
};

const resetQuery = async () => {
	state.queryParams.direction = undefined;
	state.queryParams.vehicleType = undefined;
	state.queryParams.startTime = new Date(new Date(now.value.getTime() - 3 * 24 * 60 * 60 * 1000).setHours(0, 0, 0, 0));
	state.queryParams.endTime = new Date(new Date(now.value).setHours(23, 59, 59, 999));
	state.queryParams.licensePlate = undefined;
	state.queryParams.lengthFrom = undefined;
	state.queryParams.lengthTo = undefined;
	state.queryParams.equipmentId = undefined;
	await handleQuery();
};

const handleView = (row: PageTrafficAnalysisOutput) => {
	const direction = state.queryParams.directionList.find((item: any) => item.code === row.direction);
	const vehicleType = state.queryParams.vehicleTypeList.find((item: any) => item.code === row.type);

	state.defaultValue = {
		...row,
		directionName: direction?.name ?? row.direction ?? '',
		vehicleTypeName: vehicleType?.name ?? row.type ?? '',
		equipmentName: getEquipmentDisplayById(row.equipmentId),
	};
	modalRef.value?.showModal(true);
};

const gridEvents: VxeGridListeners<PageTrafficAnalysisOutput> = {
	async pageChange({ pageSize }) {
		state.localPageParam.pageSize = pageSize;
		Local.set(localPageParamKey, state.localPageParam);
	},
	async sortChange({ field, order }) {
		state.localPageParam.defaultSort = { field, order: order!, descStr: 'desc' };
		Local.set(localPageParamKey, state.localPageParam);
	},
};
</script>
