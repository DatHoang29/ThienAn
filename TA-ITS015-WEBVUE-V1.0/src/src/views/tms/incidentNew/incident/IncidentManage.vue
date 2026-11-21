<template>
	<DataWorkspace
		id="monitor"
		class="traffic-monitor w100"
		:title="pageTitle"
		tab="incident"
		grid-id="tmsIncidentReviewGrid_incident"
		column-layout-version="compact-v3"
		:default-sort="state.sort"
		:controller="monitor.workspace"
		:columns="columns"
		:tab-label="pageLabel"
		:records-label="$t('lz.label.tms.eventsIncidents')"
		:navigate-evidence="navigateEvidence"
		:refresh-related="refreshRelated"
	>
		<template #tabs><slot name="tabs" /></template>
		<template #detail="{ visible, active }">
			<IncidentDetails ref="detailsPanel" :visible="visible" :active="active" :monitor="monitor" @edit="handleEdit" />
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
						<el-form-item :label="$t('lz.label.base.findRecord')" class="search-field">
							<el-input v-model="filters.keyword" :prefix-icon="Search" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.name') })" clearable :maxlength="64" />
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
						<el-form-item :label="$t('lz.entity.tms.eventType')">
							<el-select v-model="filters.eventTypeId" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.tms.eventType') })" clearable filterable>
								<el-option v-for="item in state.catalog.eventTypes" :key="String(item.id)" :label="item.name || item.code" :value="item.id" />
							</el-select>
						</el-form-item>
						<el-form-item :label="$t('lz.entity.base.status')">
							<el-select v-model="filters.state" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.base.status') })" clearable>
								<el-option v-for="(label, code) in stateLabels" :key="code" :label="label" :value="code" />
							</el-select>
						</el-form-item>
					</div>
				</template>
				<template #advanced>
					<el-form-item :label="$t('lz.label.base.location')">
						<el-select v-model="filters.zoneId" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.label.base.location') })" clearable filterable>
							<el-option v-for="item in zoneOptions" :key="String(item.id)" :label="item.name || item.code" :value="item.id" />
						</el-select>
					</el-form-item>
					<el-form-item :label="$t('lz.label.base.recordGroup')">
						<el-select v-model="filters.incidentGroup">
							<el-option :label="$t('lz.label.tms.eventsAndIncidents')" value="all" />
							<el-option :label="$t('lz.label.tms.event')" value="event" />
							<el-option :label="$t('lz.label.tms.tmsIncident')" value="incident" />
						</el-select>
					</el-form-item>
					<el-form-item :label="$t('lz.entity.tmsIncident.source')">
						<el-select v-model="filters.source" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.tmsIncident.source') })" clearable>
							<el-option v-for="item in sources" :key="item.code" :label="item.name" :value="item.code" />
						</el-select>
					</el-form-item>
					<el-form-item :label="$t('lz.entity.tms.fromKmNumber')">
						<div class="position-filter">
							<el-input-number v-model="filters.fromKm" :min="0" controls-position="right" :placeholder="$t('lz.entity.tms.kmNumber')" />
							<el-input-number v-model="filters.fromMet" :min="0" :max="999" controls-position="right" :placeholder="$t('lz.entity.tms.metNumber')" />
						</div>
					</el-form-item>
					<el-form-item :label="$t('lz.entity.tms.toKmNumber')">
						<div class="position-filter">
							<el-input-number v-model="filters.toKm" :min="0" controls-position="right" :placeholder="$t('lz.entity.tms.kmNumber')" />
							<el-input-number v-model="filters.toMet" :min="0" :max="999" controls-position="right" :placeholder="$t('lz.entity.tms.metNumber')" />
						</div>
					</el-form-item>
				</template>
			</SearchFilterPanel>
		</template>
		<template #record="{ row }">
			<strong class="record-title" :title="row.name">
				{{ row.name || eventName(row) }}
			</strong>
			<span class="record-secondary" :title="row.code">
				{{ row.code || '—' }}
			</span>
		</template>
		<template #time="{ row }">
			<strong class="record-time">
				{{ row.startDate ? useDateFormat(row.startDate, DateTimeFormatEnum.DateDefault).value : '—' }}
			</strong>
			<span class="record-secondary">{{ row.startDate ? useDateFormat(row.startDate, DateTimeFormatEnum.TimeDefault).value : '—' }}</span>
		</template>
		<template #location="{ row }">
			<strong class="record-location">{{ recordPosition(row) }}</strong>
			<span class="record-secondary" :title="incidentLocationCaption(row)">
				{{ incidentLocationCaption(row) }}
			</span>
		</template>
		<template #eventType="{ row }">
			<div class="event-type-cell" :title="eventName(row)">
				<el-image v-if="eventTypeIcon(row)" :src="eventTypeIcon(row)" class="event-type-icon" fit="contain">
					<template #error>
						<el-icon><Warning /></el-icon>
					</template>
				</el-image>
				<el-icon v-else class="event-type-icon">
					<Warning />
				</el-icon>
				<span class="event-type-label">{{ eventName(row) }}</span>
			</div>
		</template>
		<template #state="{ row }">
			<TagInfo v-if="stateConfig(row)" :config-item="stateConfig(row)" />
			<el-tag v-else type="info">
				{{ stateLabels[row.state || ''] || $t('lz.label.base.unknown') }}
			</el-tag>
		</template>
		<template #actions="{ row }">
			<div class="row-actions" @click.stop>
				<el-tooltip v-if="canUpdate && row.state === TmsIncidentStateEnum.Registration" :content="$t('lz.button.base.edit')" placement="top">
					<el-button :icon="Edit" type="primary" text :disabled="state.saving || state.loading" :aria-label="$t('lz.button.base.edit')" @click="handleEdit(row)" />
				</el-tooltip>
				<span class="row-audit" :title="$t('lz.label.base.audit')" :aria-label="$t('lz.label.base.audit')">
					<ModifyRecord :data="{ ...row, id: row.id ?? null }" icon-only />
				</span>
			</div>
		</template>
		<template #record-actions>
			<!-- Đang xem tạm sự cố mở từ tab Sự kiện; đóng (hoặc Làm mới/Tìm kiếm) để quay lại danh sách. -->
			<el-tag
				v-if="state.focus"
				type="warning"
				closable
				:disable-transitions="true"
				:title="$t('lz.label.tmsIncident.reviewFocusBack')"
				@close="clearFocus()"
			>
				{{ $t('lz.label.tmsIncident.reviewFocusedIncident', { code: state.focus.code }) }}
			</el-tag>
		</template>
		<template #header-actions>
			<el-button v-if="auth('tmsIncident:add')" type="primary" :icon="Plus" :disabled="state.saving" @click="handleAdd">
				{{ $t('lz.label.tmsIncident.registrationTitle') }}
			</el-button>
		</template>
		<template #overview="{ active }">
			<OverviewChart ref="overviewChart" :filters="monitor.state.incidentChartFilters" :active="active" />
		</template>
		<template #dialogs>
			<EditIncident v-if="editorMounted" @on-submit-success="handleSubmitSuccess" />
		</template>
	</DataWorkspace>
</template>

<script setup lang="ts" name="tmsIncidentMonitor">
import { ref, computed, nextTick, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { useDateFormat } from '@vueuse/core';
import { DateTimeFormatEnum } from '/@/types/enums/baseEnum';
import type { VxeGridProps } from 'vxe-table';
import { Edit, Warning, Search, Plus } from '@element-plus/icons-vue';
import { TmsIncidentStateEnum, BaseConfigTypeEnum } from '/@/types/enums';
import TagInfo from '/@/components/advanced/tag-info.vue';
import { useIncidentMonitor as useIncidentDialog } from '/@/stores/its/incidentMonitor';
import { useItsMediaServerStore } from '/@/stores/its/itsMediaServer';
import { auth } from '/@/utils/authFunction';
import EditIncident from '/@/views/tms/incident/component/editIncident.vue';
import ModifyRecord from '/@/components/table/modifyRecord.vue';
import { defaultFilters } from './incidentModel';
import type { IncidentRow } from './incidentModel';
import { useIncidentMonitor } from './useIncidentMonitor';
import DataWorkspace from '/@/components/workspace/DataWorkspace.vue';
import IncidentDetails from './component/detailIncident.vue';
import SearchFilterPanel from '/@/components/workspace/SearchFilterPanel.vue';
import OverviewChart from './component/overviewChart.vue';

interface Props {
	title?: string;
	tabLabel?: string;
}

const props = defineProps<Props>();
const { t: $t } = useI18n();
const pageLabel = computed(() => props.tabLabel || $t('lz.label.tms.tmsIncident'));
const pageTitle = computed(() => props.title || pageLabel.value);
const monitor = useIncidentMonitor();
const state = monitor.state;
defineExpose({ state });
const filters = computed(() => state.filters);
const { search: handleQuery, reset: resetQuery, zoneOptions, configList, stateLabels, eventName, recordPosition, locationName, configName, canUpdate, clearFocus } = monitor;
const sources = computed(() => configList(BaseConfigTypeEnum.TmsSourceDetectIncident));

const media = useItsMediaServerStore();
const overviewChart = ref<InstanceType<typeof OverviewChart>>();
const detailsPanel = ref<InstanceType<typeof IncidentDetails>>();
const editorMounted = ref(false);

// Table columns
function columns(): NonNullable<VxeGridProps<IncidentRow>['columns']> {
	return [
		{ type: 'seq', title: $t('lz.label.base.idx'), width: 45, align: 'center' },
		{ field: 'startDate', title: $t('lz.entity.base.recordTime'), width: 150, sortable: true, slots: { default: 'time' } },
		{ field: 'name', title: $t('lz.label.base.nameCode'), minWidth: 200, align: 'left', sortable: true, slots: { default: 'record' } },
		{ field: 'location', title: $t('lz.label.tms.positionDirection'), width: 180, align: 'left', slots: { default: 'location' } },
		{ field: 'eventTypeId', title: $t('lz.entity.tms.eventType'), width: 168, align: 'left', sortable: true, slots: { default: 'eventType' } },
		{ field: 'createdBy', title: $t('lz.entity.base.registeredBy'), width: 115, align: 'left' },
		{ field: 'state', title: $t('lz.entity.base.status'), width: 110, fixed: 'right', sortable: true, slots: { default: 'state' } },
		{ field: 'actions', title: $t('lz.label.base.action'), width: 90, fixed: 'right', align: 'center', slots: { default: 'actions' } },
	];
}

// Row presentation and tab-specific actions
const stateConfig = (row: IncidentRow) => configList(BaseConfigTypeEnum.TmsIncidentStatus).find((item) => String(item.code) === String(row.state));

/** Icon loại sự cố; '' khi chưa có ⇒ template hiện icon cảnh báo thay thế. */
function eventTypeIcon(row: IncidentRow) {
	return media.eventTypeIcon(state.catalog.eventTypes.find((item) => item.id === row.eventTypeId), { fallback: '' });
}

const incidentLocationCaption = (row: IncidentRow) =>
	[locationName(row), configName(BaseConfigTypeEnum.TmsDirectionType, row.influenceScope)].filter((value) => value && value !== '—').join(' · ') || '—';

const defaultDates = () => defaultFilters().dateRange!;

const navigateEvidence = (direction: -1 | 1) => detailsPanel.value?.navigateEvidence(direction) ?? false;

async function refreshOverview(force = true) {
	await overviewChart.value?.refresh(force);
}

async function refreshRelated(force = true) {
	await refreshOverview(force);
}

async function handleSubmitSuccess() {
	await Promise.all([monitor.load(state.selectedKey), refreshOverview(false)]);
}

function handleAdd() {
	return openEditor();
}

function handleEdit(row: IncidentRow) {
	return openEditor(row);
}

async function openEditor(row?: IncidentRow) {
	if (state.saving || (row ? !canUpdate.value : !auth('tmsIncident:add'))) return;
	editorMounted.value = true;
	await nextTick();
	const title = row ? $t('lz.label.base.titleEdit', { name: $t('lz.label.tms.tmsIncident') }) : $t('lz.label.tmsIncident.registrationTitle');
	useIncidentDialog().setIncidentDialogState(true, title, !!row, row ?? null);
}

onMounted(() => {
	void media.ensureLoaded();
});
</script>

<style lang="scss" src="../shared/monitor.scss"></style>

<style lang="scss" src="./incident.scss"></style>
