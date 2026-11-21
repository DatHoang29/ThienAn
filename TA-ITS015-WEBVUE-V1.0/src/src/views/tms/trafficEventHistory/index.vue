<template>
	<div class="tms-map-container">
		<el-card
			shadow="hover"
			:body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', height: '100%', alignItems: 'start' }"
		>
			<el-form :model="state.queryParams" :show-message="false" :inline-message="true" label-width="auto" label-position="left" style="flex: 1 1 0%">
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.startTime')">
							<DatePicker v-model="state.queryParams.startTime" dateFormat="dd/mm/yy" showTime showSeconds size="small" class="ta-input-size" />
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.base.endTime')">
							<DatePicker v-model="state.queryParams.endTime" dateFormat="dd/mm/yy" showTime showSeconds size="small" class="ta-input-size" />
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.tms.direction')">
							<el-select v-model="state.queryParams.direction" class="ta-input-size" size="small" clearable>
								<el-option v-for="item in state.queryParams.directionList" :key="item.code" :label="item.name" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.tms.eventType')">
							<el-select v-model="state.queryParams.eventType" class="ta-input-size" size="small" filterable allow-create clearable
								:placeholder="$t('lz.label.vdsConfig.selectTms')">
								<el-option v-for="item in state.eventTypeList" :key="item.code" :label="item.name" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
				</el-row>
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.tms.licensePlate')">
							<el-input v-model="state.queryParams.licensePlate" class="ta-input-size" size="small" clearable :maxlength="20" show-word-limit @keyup.enter.native="handleQuery" />
						</el-form-item>
					</el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
						<el-form-item :label="$t('lz.entity.tms.equipment')">
							<el-select v-model="state.queryParams.equipmentId" class="ta-input-size" size="small" filterable clearable>
								<el-option v-for="item in state.equipmentList" :key="item.id" :label="getEquipmentDisplay(item)" :value="item.id" />
							</el-select>
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>

			<el-divider style="height: calc(100% - 5px); margin: 0 10px" direction="vertical" />
			<el-row>
				<SearchButton :auth-search="'tmsEventData:page'" :loading="options.loading" @search="handleQuery" @reset="resetQuery" />
			</el-row>
		</el-card>

		<el-card class="full-table" shadow="hover" style="margin-top: 5px">
			<vxe-grid ref="xGrid" class="xGrid-style" v-bind="options" v-on="gridEvents">
				<template #row_vehicleType="{ row }">{{ getVehicleSizeDisplay(row.type) }}</template>
				<template #row_direction="{ row }">{{ getDirectionDisplay(row.direction) }}</template>
				<template #row_equipment="{ row }">{{ getEquipmentDisplayById(row.equipmentId) }}</template>
				<template #row_eventType="{ row }">{{ getEventTypeDisplay(row.eventType, row.aidEventType) }}</template>
				<template #row_buttons="{ row }">
					<el-tooltip :content="$t('lz.button.base.detail')" placement="top">
						<el-button icon="ele-View" text type="primary" v-auth="'tmsEventData:view'" @click="handleView(row)" />
					</el-tooltip>
				</template>
			</vxe-grid>
		</el-card>

		<TrafficEventHistoryDetailModal
			ref="modalRef"
			:default-value="state.defaultValue"
			:event-type-name="getEventTypeDisplay(state.defaultValue.eventType, state.defaultValue.aidEventType)"
			:equipment-name="getEquipmentDisplayById(state.defaultValue.equipmentId)"
		/>
	</div>
</template>

<script lang="ts" setup>
import { defineAsyncComponent, onMounted, reactive, ref } from 'vue';
import { vdsTimestamp } from '/@/utils/vdsTimestamp';
import { useDateFormat, useNow } from '@vueuse/core';
import { useI18n } from 'vue-i18n';
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { TmsEquipmentApi, TmsEquipmentTypeApi, TmsEventTypeApi } from '/@/api-services/tms';
import { PageTrafficDataInput, PageTrafficDataOutput, VdsEventApi } from '/@/api-services/vds';
import { vehicleCountField } from '/@/views/bigScreen/trafficInfo/component/vehicleCountField';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { useBaseConfig } from '/@/stores/baseConfig';
import { BaseConfigTypeEnum, SysConfigTypeCodeEnum } from '/@/types/enums';
import { DATE_TIME_FORMAT } from '/@/types/formats';
import { getAPI } from '/@/utils/axios-utils';
import { Local } from '/@/utils/storage';
import DatePicker from 'primevue/datepicker';
import TrafficEventHistoryDetailModal from './component/trafficEventHistoryDetailModal.vue';

// Bản ghi VDS bổ sung mấy khoá mà lưới và modal chi tiết vốn đọc theo tên cũ. Giữ đúng cách
// trafficAnalysis/index.vue đã làm: KHÔNG sửa lưới/modal theo tên cột VDS, chỉ đắp thêm bí danh khi map.
type PageEventHistoryOutput = PageTrafficDataOutput & {
	id?: string;
	type?: string | null;
	length?: number | null;
	ipAddress?: string | null;
};

// Chỉ camera VDS mới sinh được bản tin sự kiện, nên danh sách thiết bị của trang này lọc theo đúng loại đó.
const vdsEquipmentTypeCode = 'VDS';

const { t: $t } = useI18n();
const now = useNow();
const baseConfig = useBaseConfig();
const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));
const xGrid = ref<VxeGridInstance>();
const modalRef = ref<InstanceType<typeof TrafficEventHistoryDetailModal> | null>(null);
const getConfigDataByCode = baseConfig.getConfigDataByCode;

const state = reactive({
	queryParams: {
		startTime: new Date(new Date(now.value.getTime() - 3 * 24 * 60 * 60 * 1000).setHours(0, 0, 0, 0)),
		endTime: new Date(new Date(now.value).setHours(23, 59, 59, 999)),
		direction: undefined as string | undefined,
		licensePlate: undefined as string | undefined,
		equipmentId: undefined as string | undefined,
		eventType: undefined as string | undefined,
		directionList: [] as any[],
		vehicleSizeList: [] as any[],
	},
	equipmentList: [] as any[],
	eventTypeList: [] as any[],
	localPageParam: {
		pageSize: 50,
		defaultSort: { field: 'detectTime', order: 'desc', descStr: 'desc' },
	},
	defaultValue: {} as PageEventHistoryOutput,
});

const localPageParamKey = 'localPageParam:tmsTrafficEventHistoryGrid';
let latestQueryId = 0;
let latestQuery: ReturnType<VdsEventApi['apiVdsVdseventPagePost']> | undefined;

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

/** Tên TMS; unknown luôn kèm tên AID đã lưu ở thời điểm phát hiện. */
const getEventTypeDisplay = (eventType?: string | null, aidEventType?: string | null) => {
	if (eventType === 'unknown') return aidEventType ? `${$t('lz.label.base.unknown')} (${aidEventType})` : $t('lz.label.base.unknown');
	if (!eventType) return '-';
	const mapped = state.eventTypeList.find((item: any) => String(item.code).toLowerCase() === String(eventType).toLowerCase());
	return mapped?.name ?? String(eventType);
};

const getConfigDisplay = (configType: BaseConfigTypeEnum, value?: string | null) => {
	if (value === null || value === undefined || value === '') return '-';
	const item = (getConfigDataByCode(configType) ?? []).find((configItem: any) => String(configItem.code) === String(value));
	return item?.name ?? String(value);
};

const getDirectionDisplay = (direction?: string | null) => getConfigDisplay(BaseConfigTypeEnum.TmsDirectionType, direction);

/** Cỡ xe VDS suy từ chiều dài: short / medium / long / other. */
const getVehicleSizeDisplay = (vehicleSize?: string | null) => {
	if (!vehicleSize) return '-';
	const item = state.queryParams.vehicleSizeList.find((size: any) => size.code === vehicleSize);
	return item?.name ?? String(vehicleSize);
};

const isVdsEquipmentType = (equipmentType: any) => {
	return String(equipmentType?.code ?? '').trim().toUpperCase() === vdsEquipmentTypeCode;
};

const options = useVxeTable<PageEventHistoryOutput>(
	{
		id: 'tmsTrafficEventHistoryGrid',
		name: 'TrafficEventHistory',
		columns: [
			// sortable: false cho MỌI cột — API VDS luôn sắp cứng detectTime DESC, vendorEventId DESC và bỏ
			// qua field/order. Để sortable: true là hứa với người dùng một thứ không xảy ra.
			{
				field: 'detectTime',
				title: $t('lz.entity.tms.detectTime'),
				minWidth: 165,
				showOverflow: 'tooltip',
				sortable: false,
				formatter: ({ cellValue }: any) => useDateFormat(vdsTimestamp(cellValue), DATE_TIME_FORMAT).value,
			},
			{ field: 'licensePlate', title: $t('lz.entity.tms.licensePlate'), minWidth: 130, showOverflow: 'tooltip', sortable: false },
			{ field: 'aidEventType', title: $t('lz.entity.vds.aidEvent'), minWidth: 160, formatter: ({ row }: any) => row.aidEventType ?? row.eventType ?? '-' },
			{ field: 'eventType', title: $t('lz.entity.tms.eventType'), minWidth: 180, showOverflow: 'tooltip', slots: { default: 'row_eventType' } },
			{ field: 'equipmentId', title: $t('lz.entity.tms.equipment'), minWidth: 220, showOverflow: 'tooltip', slots: { default: 'row_equipment' } },
			{ field: 'type', title: $t('lz.entity.tms.vehicleSize'), minWidth: 150, showOverflow: 'tooltip', sortable: false, slots: { default: 'row_vehicleType' } },
			{ field: 'direction', title: $t('lz.entity.tms.direction'), minWidth: 130, showOverflow: 'tooltip', sortable: false, slots: { default: 'row_direction' } },
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

const loadEventTypeOptions = async () => {
	try {
		const response = await getAPI(TmsEventTypeApi).apiTmsTmseventtypeListGet();
		// Danh mục đã có 'unknown' (migration 20261019_TmsEventType_Add_Unknown) — không tự chèn dòng giả.
		state.eventTypeList = response.data.result ?? [];
	} catch (error) {
		console.error('Không thể tải tên loại sự kiện TMS.', error);
	}
};

const loadFilterOptions = async () => {
	try {
		if (Object.keys(baseConfig.configList ?? {}).length === 0) await baseConfig.setConfigDataList();
	} catch (error) {
		console.error('Không thể tải cấu hình hướng di chuyển.', error);
	}

	try {
		const [equipmentResponse, equipmentTypeResponse] = await Promise.all([
			getAPI(TmsEquipmentApi).apiTmsTmsequipmentListGet(),
			getAPI(TmsEquipmentTypeApi).apiTmsTmsequipmenttypeListGet(),
		]);
		const allowedEquipmentTypeIds = new Set(
			(equipmentTypeResponse.data.result ?? []).filter(isVdsEquipmentType).map((equipmentType: any) => String(equipmentType.id))
		);
		state.equipmentList = [...(equipmentResponse.data.result ?? [])]
			.filter((equipment: any) => allowedEquipmentTypeIds.has(String(equipment.equipmentTypeId)))
			.sort((first: any, second: any) => (first.name ?? '').localeCompare(second.name ?? ''));
	} catch (error) {
		console.error('Không thể tải danh sách thiết bị VDS.', error);
	}

	await loadEventTypeOptions();

	state.queryParams.directionList = getConfigDataByCode(BaseConfigTypeEnum.TmsDirectionType) ?? [];

	// Cỡ xe: dựng đúng như trafficAnalysis để nhãn S/M/L khớp giữa hai trang dù danh mục đổi mã.
	const configuredVehicleTypes = baseConfig.configList?.[SysConfigTypeCodeEnum.TmsVehicleTypeConfigCode] ?? [];
	state.queryParams.vehicleSizeList = ['short', 'medium', 'long', 'other'].map((code) => {
		const item = configuredVehicleTypes.find((vehicleType: any) => vehicleCountField(vehicleType) === code);
		return { ...item, code, name: item?.name ?? { short: 'S', medium: 'M', long: 'L', other: $t('lz.label.base.other') }[code] };
	});
};

onMounted(async () => {
	await loadFilterOptions();
	state.localPageParam = Local.get(localPageParamKey) || state.localPageParam;
	await handleQuery();
});

const handleQueryApi = async (
	page: VxeGridPropTypes.ProxyAjaxQueryPageParams,
	sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams
) => {
	const params: PageTrafficDataInput = {
		page: page.currentPage,
		pageSize: page.pageSize,
		field: sort.field,
		order: sort.order,
		descStr: 'desc',
		fromDate: state.queryParams.startTime,
		toDate: state.queryParams.endTime,
		direction: state.queryParams.direction,
		licensePlate: state.queryParams.licensePlate,
		equipmentId: state.queryParams.equipmentId,
		eventType: state.queryParams.eventType,
	};

	// /api/vds/vdsevent/page tự đặt eventsOnly = true ở phía máy chủ, nên lượt xe bình thường
	// (Normal/Regular) không lẫn vào danh sách sự kiện.
	const queryId = ++latestQueryId;
	const request = getAPI(VdsEventApi).apiVdsVdseventPagePost(params).then(response => {
		const result = response.data.result;
		if (result?.records) {
			result.records = result.records.map((row: PageTrafficDataOutput) => ({
			...row,
			// QuestDB không có cột id; khoá của một bản tin là cặp (vendorEventId, detectTime) — cũng chính
			// là khoá mà /api/vds/vdsevent/detail và /api/vds/vdsmedia/file nhận.
			id: `${row.vendorEventId}@${row.detectTime}`,
			type: row.vehicleSize,
			length: row.lengthMm,
			ipAddress: row.cameraIp,
			}));
		}
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
	state.queryParams.startTime = new Date(new Date(now.value.getTime() - 3 * 24 * 60 * 60 * 1000).setHours(0, 0, 0, 0));
	state.queryParams.endTime = new Date(new Date(now.value).setHours(23, 59, 59, 999));
	state.queryParams.direction = undefined;
	state.queryParams.licensePlate = undefined;
	state.queryParams.equipmentId = undefined;
	state.queryParams.eventType = undefined;
	await handleQuery();
};

const handleView = (row: PageEventHistoryOutput) => {
	state.defaultValue = row;
	modalRef.value?.showModal(true);
};

const gridEvents: VxeGridListeners<PageEventHistoryOutput> = {
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

<style scoped>
.ta-input-size {
	width: 100%;
}
</style>
