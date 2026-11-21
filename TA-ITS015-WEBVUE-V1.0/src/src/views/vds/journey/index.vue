<!-- Lộ trình tốc độ trung bình, cây chuyến đi và sơ đồ tuyến VDS. -->
<template>
	<div class="vds-journey-container">
		<!-- Thẻ tìm kiếm thu gọn / kéo đổi chiều cao được như các trang danh sách khác (GridSearchPanel). -->
		<GridSearchPanel id="vds-journey-search">
			<template #filters>
			<el-form :model="query" :show-message="false" :inline-message="true" label-width="auto" label-position="left" style="flex: 1 1 0%">
				<el-row :gutter="10">
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6"
						><el-form-item :label="$t('lz.entity.base.startTime')"
							><DatePicker
								v-model="query.fromDate"
								@update:model-value="followCurrentTime = false"
								:maxDate="query.toDate || undefined"
								hourFormat="24"
								dateFormat="dd/mm/yy"
								showTime
								showSeconds
								size="small"
								class="ta-input-size" /></el-form-item
					></el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6"
						><el-form-item :label="$t('lz.entity.base.endTime')"
							><DatePicker
								v-model="query.toDate"
								@update:model-value="followCurrentTime = false"
								:minDate="query.fromDate || undefined"
								hourFormat="24"
								dateFormat="dd/mm/yy"
								showTime
								showSeconds
								size="small"
								class="ta-input-size" /></el-form-item
					></el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6"
						><el-form-item :label="$t('lz.entity.vds.segment')"
							><el-select v-model="query.segmentId" filterable clearable size="small" class="ta-input-size" :placeholder="$t('lz.label.base.all')"
								><el-option v-for="segment in segments" :key="segment.id!" :value="segment.id!" :label="segment.name || segment.id!" /></el-select></el-form-item
					></el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6"
						><el-form-item :label="$t('lz.label.base.camera')"
							><el-select v-model="query.equipmentId" filterable clearable size="small" class="ta-input-size" :placeholder="$t('lz.label.base.all')"
								><el-option v-for="camera in cameras" :key="camera.id" :value="camera.id" :label="cameraLabel(camera)" /></el-select></el-form-item
					></el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6"
						><el-form-item :label="$t('lz.entity.tms.licensePlate')"
							><el-input v-model="query.licensePlate" clearable size="small" class="ta-input-size" maxlength="32" @keyup.enter="reload" /></el-form-item
					></el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6"
						><el-form-item :label="$t('lz.entity.tms.direction')"
							><el-select v-model="query.direction" clearable size="small" class="ta-input-size" :placeholder="$t('lz.label.base.all')"
								><el-option v-for="direction in directions" :key="direction.code" :value="direction.code" :label="direction.name" /></el-select></el-form-item
					></el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6"
						><el-form-item :label="$t('lz.entity.tms.speedFrom')"
							><el-input-number v-model="query.speedFrom" :min="0" :precision="1" :controls="false" size="small" class="ta-input-size" placeholder="km/h" /></el-form-item
					></el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6"
						><el-form-item :label="$t('lz.entity.tms.speedTo')"
							><el-input-number v-model="query.speedTo" :min="0" :precision="1" :controls="false" size="small" class="ta-input-size" placeholder="km/h" /></el-form-item
					></el-col>
					<el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6"
						><el-form-item :label="$t('lz.label.base.valid')"
							><el-select v-model="query.isValid" clearable size="small" class="ta-input-size" :placeholder="$t('lz.label.base.all')" @clear="query.isValid = undefined"
								><el-option :label="$t('lz.label.base.valid')" :value="true" /><el-option :label="$t('lz.label.base.invalid')" :value="false" /></el-select></el-form-item
					></el-col>
				</el-row>
			</el-form>
			<el-divider style="height: calc(100% - 5px); margin: 0 10px" direction="vertical" />
			<el-row><SearchButton authSearch="vdsJourney:page" :loading="options.loading" @search="reload" @reset="resetQuery" /></el-row>
			</template>
		<el-card class="full-table" shadow="hover">
			<el-alert v-if="error" :title="error" type="error" :closable="false" class="mb5" />
			<vxe-grid ref="grid" class="xGrid-style" v-bind="options">
				<template #toolbar_tools><TableHeader :show-add="false" :show-delete="false" @refresh="reload" @custom-column="grid?.openCustom()" /></template>
				<template #empty><el-empty :image-size="160" :description="$t('lz.message.vdsJourney.empty')" /></template>
				<template #segment="{ row }">{{ isTrip(row) ? '—' : segmentName(row.segmentId) }}</template>
				<template #from="{ row }">{{ cameraName(isTrip(row) ? row.startEquipmentId : row.fromEquipmentId) }}</template>
				<template #to="{ row }">{{ cameraName(isTrip(row) ? row.endEquipmentId : row.toEquipmentId) }}</template>
				<template #fromTime="{ row }">{{ formatTime(row.fromTime) }}</template>
				<template #toTime="{ row }">{{ formatTime(row.toTime) }}</template>
				<template #speed="{ row }">{{ row.avgSpeedKmh == null ? '-' : row.avgSpeedKmh.toFixed(1) }}</template>
				<template #direction="{ row }">{{ isTrip(row) ? directionName(row.startDirection) + ' → ' + directionName(row.endDirection) : directionName(row.direction) }}</template>
				<template #distance="{ row }">{{ row.distanceMeter == null ? '—' : (row.distanceMeter / 1000).toFixed(3) }}</template>
				<template #valid="{ row }">
					<el-space v-if="isTrip(row)" wrap>
						<el-tag v-if="row.hasOverSpeed" type="warning">{{ $t('lz.label.vdsJourney.overSpeed') }}</el-tag>
						<el-tag v-if="row.hasInvalid" type="info">{{ $t('lz.label.base.invalid') }}</el-tag>
						<el-tag v-if="!row.hasOverSpeed && !row.hasInvalid" type="success">{{ $t('lz.label.vdsJourney.normal') }}</el-tag>
					</el-space>
					<el-tag v-else :type="row.isValid === false ? 'info' : row.isOverSpeed ? 'warning' : row.isValid === true ? 'success' : 'info'">{{ statusLabel(row) }}</el-tag>
				</template>
				<template #actions="{ row }">
					<el-tooltip v-if="isTrip(row)" :content="$t('lz.button.vdsJourney.viewRoute')" placement="top">
						<el-button v-auth="'vdsJourney:view'" text type="primary" icon="ele-Guide" :aria-label="$t('lz.button.vdsJourney.viewRoute')" @click="openTrip(row)" />
					</el-tooltip>
					<el-tooltip v-else :content="$t('lz.button.base.view')" placement="top">
						<el-button v-auth="'vdsJourney:view'" text type="primary" icon="ele-View" :aria-label="$t('lz.button.base.view')" @click="openDetail(row)" />
					</el-tooltip>
				</template>
			</vxe-grid>
		</el-card>
		</GridSearchPanel>
		<el-dialog v-model="tripVisible" width="1400px" draggable :close-on-click-modal="false">
			<template #header
				><div style="color: var(--el-color-white)">
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"><ele-Edit /></el-icon
					><span>{{ $t('lz.label.vdsJourney.title', { plate: selectedTrip?.licensePlate || '—' }) }}</span>
				</div></template
			>
			<el-descriptions v-if="selectedTrip" :column="3" border class="journey-block">
				<el-descriptions-item :label="$t('lz.entity.tms.licensePlate')">{{ selectedTrip.licensePlate || '—' }}</el-descriptions-item>
				<el-descriptions-item :label="$t('lz.entity.vdsJourney.direction')"
					>{{ directionName(selectedTrip.startDirection) }} → {{ directionName(selectedTrip.endDirection) }}</el-descriptions-item
				>
				<el-descriptions-item :label="$t('lz.label.base.time')">{{ formatTime(selectedTrip.fromTime) }} — {{ formatTime(selectedTrip.toTime) }}</el-descriptions-item>
				<el-descriptions-item :label="$t('lz.entity.vdsJourney.route')"
					>{{ cameraName(selectedTrip.startEquipmentId) }} → {{ cameraName(selectedTrip.endEquipmentId) }}</el-descriptions-item
				>
				<el-descriptions-item :label="$t('lz.label.vdsJourney.distanceKm')">{{ selectedTrip.distanceMeter == null ? '—' : (selectedTrip.distanceMeter / 1000).toFixed(3) }}</el-descriptions-item>
				<el-descriptions-item :label="$t('lz.label.vdsJourney.averageUnit')"
					>{{ selectedTrip.avgSpeedKmh?.toFixed(1) ?? '—' }} · {{ $t('lz.entity.vdsJourney.count') }}: {{ selectedTrip.journeyCount ?? 0 }}</el-descriptions-item
				>
			</el-descriptions>
			<el-skeleton v-if="topologyLoading" :rows="4" animated class="journey-block" />
			<el-alert v-else-if="topologyError" :title="topologyError" type="warning" :closable="false" class="journey-block" />
			<VdsRouteSchematic
				v-else-if="topology && chainJourneys.length"
				:topology="topology"
				:journeys="chainJourneys"
				:active-id="activeTripId"
				:direction-names="directionNames"
				trip
				class="journey-block"
				@select="selectTripJourney"
			/>
			<el-empty v-else :description="$t('lz.message.vdsRoute.unavailable')" class="journey-block" />
			<div class="journey-section-title">{{ $t('lz.label.vdsJourney.segments') }}</div>
			<vxe-grid
				ref="tripGrid"
				class="xGrid-style"
				v-bind="tripGridOptions"
				:data="tripJourneys"
				@cell-mouseenter="({ row }) => highlightJourney(row)"
				@cell-mouseleave="activeTripId = undefined"
				@cell-click="({ row }) => selectTripJourney(row)"
			>
				<template #trip_segment="{ row }">{{ segmentName(row.segmentId) }}</template>
				<template #trip_fromTime="{ row }">{{ formatTime(row.fromTime) }}</template>
				<template #trip_toTime="{ row }">{{ formatTime(row.toTime) }}</template>
				<template #trip_speed="{ row }">{{ row.avgSpeedKmh?.toFixed(1) ?? '—' }}{{ row.speedLimitKmh == null ? '' : ' / ' + row.speedLimitKmh }}</template>
				<template #trip_status="{ row }">{{ statusLabel(row) }}</template>
			</vxe-grid>
			<template #footer
				><span class="dialog-footer"><el-button type="danger" @click="tripVisible = false">{{ $t('lz.button.base.close') }}</el-button></span></template
			>
		</el-dialog>
		<el-dialog v-model="detailVisible" width="1100px" draggable :close-on-click-modal="false" append-to-body>
			<template #header
				><div style="color: var(--el-color-white)">
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"><ele-Edit /></el-icon><span>{{ $t('lz.label.vdsJourney.detail') }}</span>
				</div></template
			>
			<el-alert v-if="detailError" :title="detailError" type="error" :closable="false" class="journey-block" />
			<el-skeleton v-if="detailLoading" :rows="5" animated />
			<template v-else-if="detail?.journey">
				<el-skeleton v-if="topologyLoading" :rows="4" animated class="journey-block" />
				<el-alert v-else-if="topologyError" :title="topologyError" type="warning" :closable="false" class="journey-block" />
				<VdsRouteSchematic
					v-else-if="topology"
					:topology="topology"
					:journeys="[detail.journey]"
					:active-id="detail.journey.id ?? undefined"
					:direction-names="directionNames"
					class="journey-block"
				/>
				<div class="journey-section-title">{{ $t('lz.label.vdsJourney.journeyInfo') }}</div>
				<el-descriptions :column="2" border class="journey-block">
					<el-descriptions-item :label="$t('lz.entity.tms.licensePlate')">{{ detail.journey.licensePlate || '-' }}</el-descriptions-item>
					<el-descriptions-item :label="$t('lz.entity.vds.segment')">{{ segmentName(detail.journey.segmentId) }}</el-descriptions-item>
					<el-descriptions-item :label="$t('lz.entity.tms.fromCamera')">{{ cameraName(detail.fromRecord?.equipmentId || detail.journey.fromEquipmentId) }}</el-descriptions-item>
					<el-descriptions-item :label="$t('lz.entity.vdsJourney.fromTime')">{{ formatTime(detail.fromRecord?.detectTime || detail.journey.fromTime) }}</el-descriptions-item>
					<el-descriptions-item :label="$t('lz.entity.tms.toCamera')">{{ cameraName(detail.toRecord?.equipmentId || detail.journey.toEquipmentId) }}</el-descriptions-item>
					<el-descriptions-item :label="$t('lz.entity.vdsJourney.toTime')">{{ formatTime(detail.toRecord?.detectTime || detail.journey.toTime) }}</el-descriptions-item>
					<el-descriptions-item :label="$t('lz.entity.vdsJourney.historicDistance')">{{ detail.journey.distanceMeter ?? '-' }} {{ $t('lz.label.base.meterUnit') }}</el-descriptions-item>
					<el-descriptions-item :label="$t('lz.entity.tms.travelTime')">{{ $t('lz.label.vdsJourney.seconds', { value: detail.journey.travelSeconds ?? '—' }) }}</el-descriptions-item>
					<el-descriptions-item :label="$t('lz.entity.tms.averageSpeed')">{{ detail.journey.avgSpeedKmh?.toFixed(1) ?? '-' }} {{ $t('lz.label.tmsTrafficMatrix.speedUnit') }}</el-descriptions-item>
					<el-descriptions-item :label="$t('lz.label.vdsJourney.result')">{{
						detail.journey.isValid === false ? $t('lz.label.base.invalid') : detail.journey.isValid === true ? $t('lz.label.base.valid') : $t('lz.label.base.unknown')
					}}</el-descriptions-item>
					<el-descriptions-item :label="$t('lz.entity.tms.direction')">{{ directionName(detail.journey.direction) }}</el-descriptions-item>
					<el-descriptions-item v-if="detail.journey.invalidReason" :label="$t('lz.entity.base.reason')" :span="2">{{ detail.journey.invalidReason }}</el-descriptions-item>
				</el-descriptions>
				<div class="journey-section-title">{{ $t('lz.label.vdsJourney.incidents') }}</div>
				<el-table :data="detail.incidents ?? []" :empty-text="$t('lz.message.vdsJourney.noIncidents')" border>
					<el-table-column prop="code" :label="$t('lz.entity.base.code')" min-width="140" />
					<el-table-column prop="name" :label="$t('lz.entity.base.name')" min-width="220" />
					<el-table-column prop="state" :label="$t('lz.entity.base.status')" width="120" />
					<el-table-column :label="$t('lz.label.base.startTime')" width="175"
						><template #default="{ row }">{{ formatTime(row.startDate) }}</template></el-table-column
					>
				</el-table>
			</template>
			<template #footer
				><span class="dialog-footer"><el-button type="danger" @click="detailVisible = false">{{ $t('lz.button.base.close') }}</el-button></span></template
			>
		</el-dialog>
	</div>
</template>

<script setup lang="ts" name="vdsJourney">
import GridSearchPanel from '/@/components/gridPage/GridSearchPanel.vue';
import { computed, onMounted, reactive, ref, watch, defineAsyncComponent } from 'vue';
import { ElMessage } from 'element-plus';
import type { VxeGridInstance } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { VdsJourneyApi, VdsSegmentApi } from '/@/api-services/vds';
import type { JourneyDetailOutput, JourneyTripOutput, JourneyInput, PageJourneyOutput, SegmentOutput, SegmentTopologyOutput } from '/@/api-services/vds';
const VdsRouteSchematic = defineAsyncComponent(() => import('/@/components/vds/VdsRouteSchematic.vue'));
import { getAPI } from '/@/utils/axios-utils';
import { vdsTimestamp } from '/@/utils/vdsTimestamp';
import SearchButton from '/@/components/advanced/search-button-operation.vue';
import TableHeader from '/@/components/advanced/table-header-operation.vue';
import DatePicker from 'primevue/datepicker';
import { useBaseConfig } from '/@/stores/baseConfig';
import { BaseConfigTypeEnum } from '/@/types/enums';
import { useI18n } from 'vue-i18n';

const initialQuery = (): JourneyInput => ({
	fromDate: new Date(new Date().setHours(0, 0, 0, 0)),
	toDate: new Date(),
	segmentId: undefined,
	equipmentId: undefined,
	direction: undefined,
	licensePlate: undefined,
	speedFrom: undefined,
	speedTo: undefined,
	isValid: undefined,
});
const query = reactive<JourneyInput>(initialQuery());
// Chỉ tự tiến thời gian với khoảng mặc định; giữ nguyên khoảng người dùng đã chọn.
const followCurrentTime = ref(true);
const { t: $t, locale } = useI18n();
const grid = ref<VxeGridInstance>();
const tripGrid = ref<VxeGridInstance>();

const segments = ref<SegmentOutput[]>([]);
const directions = ref<{ code: string; name: string }[]>([]);
const directionNames = computed(() => Object.fromEntries(directions.value.map((item) => [item.code, item.name])));
const error = ref('');
const detailError = ref('');
const detailLoading = ref(false);
const detailVisible = ref(false);
const detail = ref<JourneyDetailOutput>();
const topology = ref<SegmentTopologyOutput>();
const cameras = computed(() => topology.value?.cameras ?? []);
const cameraLabel = (camera: NonNullable<SegmentTopologyOutput['cameras']>[number]) => camera.name || camera.code || camera.id || '—';
const topologyLoading = ref(true);
const topologyError = ref('');
const tripVisible = ref(false);
const selectedTrip = ref<JourneyTripOutput>();
const activeTripId = ref<string>();
const tripJourneys = computed<PageJourneyOutput[]>(() => selectedTrip.value?.journeys ?? []);
const chainJourneys = computed(() => {
	const byId = new Map(tripJourneys.value.map((journey) => [journey.id, journey]));
	return (selectedTrip.value?.chainJourneyIds ?? []).flatMap((id: string) => {
		const journey = byId.get(id);
		return journey ? [journey] : [];
	});
});
type GridRow = JourneyTripOutput | PageJourneyOutput;
const isTrip = (row: GridRow): row is JourneyTripOutput => 'key' in row;
const statusLabel = (row: PageJourneyOutput) =>
	$t(row.isValid === false ? 'lz.label.base.invalid' : row.isOverSpeed ? 'lz.label.vdsJourney.overSpeed' : row.isValid === true ? 'lz.label.base.valid' : 'lz.label.base.unknown');
function openTrip(row: JourneyTripOutput) {
	selectedTrip.value = row;
	activeTripId.value = undefined;
	tripVisible.value = true;
}
function highlightJourney(row: PageJourneyOutput) {
	activeTripId.value = row.id ?? undefined;
}
function selectTripJourney(row: PageJourneyOutput) {
	highlightJourney(row);
	void openDetail(row);
}
let latestQueryId = 0;
let latestQuery: ReturnType<VdsJourneyApi['apiVdsVdsjourneyTrippagePost']> | undefined;
let detailRequestId = 0;
const cameraMap = computed(() => new Map(cameras.value.map((camera) => [camera.id, camera])));
const cameraName = (id?: string | null) => (cameraMap.value.has(id ?? '') ? cameraLabel(cameraMap.value.get(id ?? '')!) : id || '-');
// Đoạn worker tự sinh (ID = mã, ví dụ T:1_2:VDS-01>VDS-08) không có trong bảng đoạn ⇒ lấy tên từ Topology.
const segmentName = (id?: string | null) =>
	segments.value.find((segment) => segment.id === id)?.name || topology.value?.segments?.find((segment) => segment.id === id)?.name || id || '-';
const directionName = (code?: string | null) => directions.value.find((item) => item.code === code)?.name ?? code ?? '-';
const formatTime = (value?: Date | string | null) => {
	if (!value) return '-';
	const date = new Date(vdsTimestamp(value));
	return Number.isNaN(date.getTime()) ? '-' : new Intl.DateTimeFormat(locale.value, { dateStyle: 'short', timeStyle: 'medium' }).format(date);
};

const options = useVxeTable<GridRow>(
	{
		id: 'vdsJourneyTripGrid',
		name: 'vdsJourneyTripGrid',
		columns: [
			{ field: 'licensePlate', title: $t('lz.entity.tms.licensePlate'), minWidth: 170, treeNode: true },
			{ field: 'direction', title: $t('lz.entity.vdsJourney.direction'), minWidth: 200, slots: { default: 'direction' } },
			{ field: 'journeyCount', title: $t('lz.entity.vdsJourney.count'), width: 95 },
			{ field: 'distanceMeter', title: $t('lz.label.vdsJourney.distanceKm'), width: 110, slots: { default: 'distance' } },
			{ field: 'segmentId', title: $t('lz.entity.vds.segment'), minWidth: 180, showOverflow: 'tooltip', slots: { default: 'segment' } },
			{ field: 'fromEquipmentId', title: $t('lz.entity.tms.fromCamera'), minWidth: 170, showOverflow: 'tooltip', slots: { default: 'from' } },
			{ field: 'toEquipmentId', title: $t('lz.entity.tms.toCamera'), minWidth: 170, showOverflow: 'tooltip', slots: { default: 'to' } },
			{ field: 'fromTime', title: $t('lz.entity.vdsJourney.fromTime'), width: 170, slots: { default: 'fromTime' } },
			{ field: 'toTime', title: $t('lz.entity.vdsJourney.toTime'), width: 170, slots: { default: 'toTime' } },
			{ field: 'avgSpeedKmh', title: $t('lz.label.vdsJourney.averageUnit'), width: 155, slots: { default: 'speed' } },
			{ field: 'isValid', title: $t('lz.label.base.valid'), width: 115, slots: { default: 'valid' } },
			{ title: $t('lz.label.base.action'), fixed: 'right', width: 100, slots: { default: 'actions' } },
		],
	},
	{
		// Bảng cây: tắt stripe của vxe (theo dòng cha) để chỉ còn sọc của theme (theo dòng đang hiện), như trang Menu.
		stripe: false,
		treeConfig: { childrenField: 'journeys', expandAll: false },
		proxyConfig: {
			autoLoad: false,
			sort: false,
			ajax: {
				query: async ({ page }) => {
					const from = query.fromDate && new Date(query.fromDate);
					const to = query.toDate && new Date(query.toDate);
					if (!from || !to || !Number.isFinite(from.getTime()) || !Number.isFinite(to.getTime()) || to <= from || to.getTime() - from.getTime() > 60 * 86400000) {
						ElMessage.warning($t('lz.validation.vdsJourney.range'));
						return { data: { result: { records: [], total: 0 } } };
					}
					if (query.speedFrom != null && query.speedTo != null && query.speedFrom > query.speedTo) {
						ElMessage.warning($t('lz.validation.vdsJourney.speed'));
						return { data: { result: { records: [], total: 0 } } };
					}
					error.value = '';
					try {
						const queryId = ++latestQueryId;
						const request = getAPI(VdsJourneyApi).apiVdsVdsjourneyTrippagePost({ ...query, fromDate: from, toDate: to, page: page.currentPage, pageSize: page.pageSize });
						latestQuery = request;
						const response = await request;
						return queryId === latestQueryId ? response : latestQuery!;
					} catch (cause) {
						error.value = cause instanceof Error ? cause.message : $t('lz.message.vdsJourney.loadError');
						throw cause;
					}
				},
			},
		},
		pagerConfig: { pageSize: 50, pageSizes: [50, 100, 200] },
		toolbarConfig: { export: false },
	},
);

// Danh sách đoạn của chuyến đã có sẵn trong bộ nhớ: tắt proxy/phân trang, bỏ chiều cao 'auto' để bảng co theo dữ liệu trong dialog.
const tripGridOptions = useVxeTable<PageJourneyOutput>(
	{
		id: 'vdsJourneyTripSegmentGrid',
		name: 'vdsJourneyTripSegmentGrid',
		columns: [
			{ type: 'seq', title: $t('lz.label.base.idx'), width: 70 },
			{ field: 'segmentId', title: $t('lz.entity.vds.segment'), minWidth: 180, showOverflow: 'tooltip', slots: { default: 'trip_segment' } },
			{ field: 'fromTime', title: $t('lz.entity.vdsJourney.fromTime'), minWidth: 170, slots: { default: 'trip_fromTime' } },
			{ field: 'toTime', title: $t('lz.entity.vdsJourney.toTime'), minWidth: 170, slots: { default: 'trip_toTime' } },
			{ field: 'avgSpeedKmh', title: $t('lz.label.vdsJourney.averageUnit'), minWidth: 130, slots: { default: 'trip_speed' } },
			{ field: 'isValid', title: $t('lz.entity.base.status'), minWidth: 150, slots: { default: 'trip_status' } },
		],
	},
	{
		height: '',
		// Chuyến thường chỉ vài đoạn: không chừa khoảng trắng của chiều cao tối thiểu mặc định.
		minHeight: 80,
		maxHeight: 360,
		border: true,
		rowConfig: { keyField: 'id', isCurrent: true, isHover: true },
		proxyConfig: { enabled: false },
		pagerConfig: { enabled: false },
		sortConfig: { remote: false },
		toolbarConfig: { enabled: false },
	},
);
// Đồng bộ dòng đang chọn của bảng với đoạn được chọn/di chuột (từ bảng hoặc sơ đồ tuyến).
watch(activeTripId, (id) => {
	const row = id ? tripJourneys.value.find((journey) => journey.id === id) : undefined;
	if (row) void tripGrid.value?.setCurrentRow(row);
	else void tripGrid.value?.clearCurrentRow();
});

async function reload() {
	if (followCurrentTime.value) {
		query.fromDate = new Date(new Date().setHours(0, 0, 0, 0));
		query.toDate = new Date();
	}
	try {
		await grid.value?.commitProxy('reload');
	} catch {
		/* The query handler has already populated the visible error alert. */
	}
}
function resetQuery() {
	Object.assign(query, initialQuery());
	followCurrentTime.value = true;
	void reload();
}

async function openDetail(row: PageJourneyOutput) {
	if (!row.id) return;
	const requestId = ++detailRequestId;
	detailVisible.value = true;
	detailLoading.value = true;
	detailError.value = '';
	detail.value = undefined;
	try {
		const response = await getAPI(VdsJourneyApi).apiVdsVdsjourneyDetailGet(row.id);
		if (requestId !== detailRequestId) return;
		if (response.data.succeeded === false || !response.data.result) throw new Error(response.data.message || $t('lz.message.vdsJourney.detailError'));
		detail.value = response.data.result;
	} catch (cause) {
		if (requestId === detailRequestId) detailError.value = cause instanceof Error ? cause.message : $t('lz.message.vdsJourney.detailError');
	} finally {
		if (requestId === detailRequestId) detailLoading.value = false;
	}
}

async function loadTopology() {
	try {
		const response = await getAPI(VdsSegmentApi).apiVdsVdssegmentTopologyGet();
		if (response.data.succeeded === false || !response.data.result) throw new Error(response.data.message || $t('lz.message.vdsRoute.loadError'));
		topology.value = response.data.result;
	} catch (cause) {
		topologyError.value = cause instanceof Error ? cause.message : $t('lz.message.vdsRoute.loadError');
	} finally {
		topologyLoading.value = false;
	}
}

onMounted(async () => {
	void loadTopology();
	try {
		const baseConfig = useBaseConfig();
		if (Object.keys(baseConfig.configList ?? {}).length === 0) await baseConfig.setConfigDataList();
		directions.value = (baseConfig.getConfigDataByCode(BaseConfigTypeEnum.TmsDirectionType) ?? []).map((item: { code: string | number; name: string }) => ({
			code: String(item.code),
			name: item.name,
		}));
		const response = await getAPI(VdsSegmentApi).apiVdsVdssegmentListGet();
		segments.value = response.data.result ?? [];
	} catch {
		error.value = $t('lz.message.vdsJourney.catalogError');
	}
	await reload();
});
</script>

<style scoped lang="scss">
/* Popup lộ trình / chi tiết: các khối (thông tin, sơ đồ tuyến, bảng) cách nhau đều, mỗi bảng có tiêu đề mục. */
.journey-block {
	margin-bottom: 16px;
}

.journey-section-title {
	display: flex;
	align-items: center;
	gap: 8px;
	margin: 4px 0 10px;
	font-size: var(--el-font-size-base);
	font-weight: 600;
	color: var(--el-text-color-primary);

	&::before {
		content: '';
		width: 3px;
		height: 14px;
		border-radius: 2px;
		background: var(--el-color-primary);
	}
}
</style>
