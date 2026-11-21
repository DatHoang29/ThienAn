<template>
	<DataWorkspace
		id="monitor"
		class="traffic-monitor w100"
		:title="pageTitle"
		tab="event"
		grid-id="tmsIncidentReviewGrid_event"
		column-layout-version="compact-v2"
		:default-sort="{ field: 'detectTime', order: 'desc' }"
		:controller="monitor.workspace"
		:columns="columns"
		:tab-label="pageLabel"
		:records-label="$t('lz.label.base.vehicle')"
		:navigate-evidence="navigateEvidence"
	>
		<template #tabs><slot name="tabs" /></template>
		<template #detail="{ visible, active }">
			<EventDetails ref="detailsPanel" :visible="visible" :active="active" :monitor="monitor" />
		</template>
		<template #filters="{ visible, hide }">
			<SearchFilterPanel
				id="monitor-search"
				:visible="visible"
				:default-dates="defaultDates"
				:filters="state.filters"
				:saving="state.saving"
				:loading="state.loading"
				@hide="hide()"
				@search="handleQuery"
				@reset="resetQuery"
			>
				<template #default="{ setFilterDate }">
					<div class="filter-grid">
						<el-form-item :label="$t('lz.entity.tms.licensePlate')" class="search-field">
							<el-input v-model="filters.keyword" :prefix-icon="Search" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.tms.licensePlate') })" clearable :maxlength="20" />
						</el-form-item>
						<el-form-item :label="$t('lz.entity.base.startTime')">
							<el-date-picker
								:model-value="filters.dateRange?.[0]"
								type="datetime"
								:format="DateTimeFormatEnum.DateTimeDefault"
								:placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.base.startTime') })"
								@update:model-value="setFilterDate(0, $event)"
							/>
						</el-form-item>
						<el-form-item :label="$t('lz.entity.base.endTime')">
							<el-date-picker
								:model-value="filters.dateRange?.[1]"
								type="datetime"
								:format="DateTimeFormatEnum.DateTimeDefault"
								:placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.base.endTime') })"
								@update:model-value="setFilterDate(1, $event)"
							/>
						</el-form-item>
						<el-form-item :label="$t('lz.label.base.camera')">
							<el-select v-model="filters.equipmentId" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.label.base.camera') })" clearable filterable>
								<el-option v-for="item in state.catalog.equipment" :key="String(item.id)" :label="equipmentDisplay(item)" :value="item.id" />
							</el-select>
						</el-form-item>
						<el-form-item :label="$t('lz.entity.tms.direction')">
							<el-select v-model="filters.direction" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.tms.direction') })" clearable>
								<el-option v-for="item in directions" :key="item.code" :label="item.name" :value="item.code" />
							</el-select>
						</el-form-item>
					</div>
				</template>
				<template #advanced>
					<el-form-item :label="$t('lz.entity.tms.eventType')">
						<el-select v-model="filters.eventType" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.tms.eventType') })" clearable filterable>
							<el-option v-for="item in eventTypeOptions" :key="String(item.id)" :label="item.name || item.code" :value="item.code" />
						</el-select>
					</el-form-item>
					<el-form-item :label="$t('lz.label.base.location')">
						<el-select v-model="filters.zoneId" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.label.base.location') })" clearable filterable>
							<el-option v-for="item in zoneOptions" :key="String(item.id)" :label="item.name || item.code" :value="item.id" />
						</el-select>
					</el-form-item>
					<el-form-item :label="$t('lz.entity.tms.vehicleSize')">
						<el-select v-model="filters.vehicleSize" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.tms.vehicleSize') })" clearable>
							<el-option v-for="item in vehicleSizes" :key="item.code" :label="item.label" :value="item.code" />
						</el-select>
					</el-form-item>
					<el-form-item :label="$t('lz.entity.tms.lengthFrom') + ' (mm)'">
						<el-input-number v-model="filters.lengthFrom" :min="0" controls-position="right" />
					</el-form-item>
					<el-form-item :label="$t('lz.entity.tms.lengthTo') + ' (mm)'">
						<el-input-number v-model="filters.lengthTo" :min="0" controls-position="right" />
					</el-form-item>
					<el-form-item :label="$t('lz.entity.tms.speedFrom') + ' (km/h)'">
						<el-input-number v-model="filters.speedFrom" :min="0" controls-position="right" />
					</el-form-item>
					<el-form-item :label="$t('lz.entity.tms.speedTo') + ' (km/h)'">
						<el-input-number v-model="filters.speedTo" :min="0" controls-position="right" />
					</el-form-item>
				</template>
			</SearchFilterPanel>
		</template>
		<template #plate="{ row }">
			<span class="record-plate">{{ plateOf(row) }}</span>
		</template>
		<template #time="{ row }">
			<strong class="record-time">
				{{ row.detectTime ? useDateFormat(vdsTimestamp(row.detectTime), DateTimeFormatEnum.DateDefault).value : '—' }}
			</strong>
			<span class="record-secondary">{{ row.detectTime ? useDateFormat(vdsTimestamp(row.detectTime), DateTimeFormatEnum.TimeDefault).value : '—' }}</span>
		</template>
		<template #equipment="{ row }">
			<strong class="record-location">{{ equipmentName(row) }}</strong>
			<span class="record-secondary" :title="routePosition(row)">{{ routePosition(row) }}</span>
		</template>
		<template #vehicleType="{ row }">{{ vehicleTypeName(row) }}<template v-if="row.lengthMm != null"> ({{ row.lengthMm }} mm)</template></template>
		<template #eventType="{ row }">
			<span :title="row.aidEventType ? $t('lz.label.tms.aidEventCode', { aid: row.aidEventType }) : undefined">{{ eventTypeName(row) }}</span>
		</template>
		<template #direction="{ row }">
			<TagInfo :config-item="configList(BaseConfigTypeEnum.TmsDirectionType).find((item) => item.code === row.direction)" />
		</template>
		<template #dialogs>
			<CreateIncident
				ref="createIncidentDialog"
				:title="$t('lz.label.tmsIncident.createFromEventTitle')"
				:monitor="monitor"
				@created="monitor.incidentCreated"
				@open-existing="monitor.incidentCreated"
			/>
		</template>
	</DataWorkspace>
</template>

<script setup lang="ts" name="tmsEventMonitor">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { useDateFormat } from '@vueuse/core';
import { DateTimeFormatEnum } from '/@/types/enums/baseEnum';
import type { VxeGridProps } from 'vxe-table';
import { Search } from '@element-plus/icons-vue';
import { BaseConfigTypeEnum } from '/@/types/enums';
import { vdsTimestamp } from '/@/utils/vdsTimestamp';
import TagInfo from '/@/components/advanced/tag-info.vue';
import { defaultFilters } from './eventModel';
import { plateOf } from '../shared/monitorFormat';
import type { EventRow } from './eventModel';
import { useEventMonitor } from './useEventMonitor';
import DataWorkspace from '/@/components/workspace/DataWorkspace.vue';
import EventDetails from './component/detailEvent.vue';
import CreateIncident from './component/createIncident.vue';
import SearchFilterPanel from '/@/components/workspace/SearchFilterPanel.vue';

interface Props {
	title?: string;
	tabLabel?: string;
}

const props = defineProps<Props>();
const detailsPanel = ref<InstanceType<typeof EventDetails>>();
const { t: $t } = useI18n();
const pageLabel = computed(() => props.tabLabel || $t('lz.label.tms.event'));
const pageTitle = computed(() => props.title || pageLabel.value);
const monitor = useEventMonitor();
const state = monitor.state;
defineExpose({ state });
const filters = computed(() => state.filters);
const { search: handleQuery, reset: resetQuery, zoneOptions, configList, vehicleSizes, equipmentDisplay, equipmentName, routePosition, vehicleTypeName, eventTypeName, eventTypeOptions } = monitor;
const directions = computed(() => configList(BaseConfigTypeEnum.TmsDirectionType));

// Table columns
function columns(): NonNullable<VxeGridProps<EventRow>['columns']> {
	return [
		{ type: 'seq', title: $t('lz.label.base.idx'), width: 45, align: 'center' },
		{ field: 'detectTime', title: $t('lz.entity.base.recordTime'), width: 144, slots: { default: 'time' } },
		{ field: 'licensePlate', title: $t('lz.entity.tms.licensePlate'), width: 110, slots: { default: 'plate' } },
		{ field: 'equipmentId', title: $t('lz.label.tms.equipmentPosition'), minWidth: 160, align: 'left', slots: { default: 'equipment' } },
		{ field: 'direction', title: $t('lz.entity.tms.direction'), width: 90, slots: { default: 'direction' } },
		{ field: 'eventType', title: $t('lz.label.tms.detectedEvent'), minWidth: 136, align: 'left', slots: { default: 'eventType' } },
		// Cỡ xe + độ dài gộp một cột: "Xe dài >12m (12500 mm)".
		{ field: 'type', title: `${$t('lz.entity.tms.vehicleSize')} (${$t('lz.entity.tms.length')})`, minWidth: 170, align: 'left', slots: { default: 'vehicleType' } },
		{ field: 'speed', title: $t('lz.entity.tms.speed') + ' (km/h)', width: 110 },
	];
}

// Row presentation and tab-specific actions
const defaultDates = () => defaultFilters().dateRange!;

const navigateEvidence = (direction: -1 | 1) => detailsPanel.value?.navigateEvidence(direction) ?? false;

// Nút "Tạo sự cố" ở panel chi tiết gọi monitor.createIncident(); hộp thoại nằm ở slot dialogs của trang.
const createIncidentDialog = ref<InstanceType<typeof CreateIncident>>();
onMounted(() => monitor.bindCreateDialog((row) => void createIncidentDialog.value?.openDialog(row)));
onBeforeUnmount(() => monitor.bindCreateDialog(null));
</script>

<style lang="scss" src="../shared/monitor.scss"></style>
