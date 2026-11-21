<template>
	<aside v-show="visible" id="monitor-detail" class="detail-panel workspace-panel" :aria-label="$t('lz.label.base.selectedRecordDetails')">
		<header class="detail-heading workspace-panel-heading">
			<div>
				<div class="detail-heading-meta">
					<span class="eyebrow">{{ $t('lz.label.base.recordDetails') }}</span>
					<span v-if="selected" class="record-counter"> {{ selectedIndex + 1 }} / {{ monitorState.rows.length }} </span>
				</div>
				<h2 :title="selected ? selected.name || eventName(selected) : ''">
					{{ selected ? selected.name || eventName(selected) : $t('lz.label.base.recordDetails') }}
				</h2>
			</div>
			<div class="record-navigation">
				<el-button
					v-if="selectedDetail && canView"
					:icon="CopyDocument"
					text
					:disabled="!selectedDetail.code"
					:title="$t('lz.label.tmsIncident.reviewCopyIncidentCode')"
					:aria-label="$t('lz.label.tmsIncident.reviewCopyIncidentCode')"
					@click="copyIncidentCode()"
				/>
				<el-button :icon="ArrowUp" :disabled="!hasPrevious || !monitorState.rows.length" :aria-label="$t('lz.label.base.previousRecord')" @click="move(-1)" />
				<el-button :icon="ArrowDown" :disabled="!hasNext || !monitorState.rows.length" :aria-label="$t('lz.label.base.nextRecord')" @click="move(1)" />
			</div>
		</header>
		<div v-if="!selected" class="detail-empty">
			<el-icon :size="44"><Document /></el-icon>
			<h3>{{ $t('lz.label.base.selectRecord') }}</h3>
		</div>
		<div v-else-if="!canView" class="detail-empty">
			<el-icon :size="40"><Lock /></el-icon>
			<h3>{{ $t('lz.label.base.noDetailAccess') }}</h3>
			<p>{{ $t('lz.message.base.detailAccessHint') }}</p>
		</div>
		<template v-else-if="selectedDetail">
			<div class="detail-scroll">
				<div v-if="detailError" class="detail-error" role="alert">
					{{ detailError }}
					<el-button text @click="selectRow(selected, true)">
						{{ $t('lz.button.base.refresh') }}
					</el-button>
				</div>
				<MediaPanel
					:file-name="'TMS_' + String(selectedDetail.code || selectedDetail.vendorEventId || 'evidence')"
					:key="monitorState.selectedKey"
					ref="evidencePanel"
					:record="selectedDetail"
					:record-key="rowKey(selectedDetail)"
					:load="loadEvidence"
					:stages="incidentEvidenceStages"
					thumbnails
					:base-url="String(common.serverHost ?? '')"
					:version="monitorState.detailVersion"
				/>
				<div class="detail-subtabs">
					<div class="detail-tab-buttons" role="tablist" :aria-label="$t('lz.entity.base.info')">
						<button type="button" role="tab" :aria-selected="state.detailTab === 'info'" :class="{ active: state.detailTab === 'info' }" @click="state.detailTab = 'info'">
							<el-icon><Document /></el-icon>
							{{ $t('lz.entity.base.info') }}
						</button>
						<button
							type="button"
							role="tab"
							:aria-selected="state.detailTab === 'history'"
							:class="{ active: state.detailTab === 'history' }"
							:title="$t('lz.label.tmsIncident.historyTitle')"
							:aria-label="$t('lz.label.tmsIncident.historyTitle')"
							@click="openHistory"
						>
							<el-icon><Clock /></el-icon>
							{{ $t('lz.label.base.history') }}
						</button>
					</div>
					<div class="detail-status" :aria-label="$t('lz.entity.base.status')">
						<TagInfo v-if="stateConfig(selectedDetail)" :config-item="stateConfig(selectedDetail)" />
						<el-tag v-else type="info">
							{{ stateLabels[selectedDetail.state || ''] || $t('lz.label.base.unknown') }}
						</el-tag>
					</div>
				</div>
				<div v-if="state.detailTab === 'info'" v-loading="monitorState.detailLoading" class="record-information">
					<div v-if="plateOf(selectedDetail) !== '—'" class="plate-summary">
						<span>{{ $t('lz.entity.tms.licensePlate') }}</span>
						<strong>{{ plateOf(selectedDetail) }}</strong>
						<el-button :icon="CopyDocument" text :aria-label="$t('lz.label.tms.copyLicensePlate')" @click="copyPlate()" />
					</div>
					<dl class="information-grid">
						<div v-for="item in details" :key="item.label">
							<dt>{{ item.label }}</dt>
							<dd>{{ item.value }}</dd>
						</div>
					</dl>
					<div v-if="vehicleMetadata.length" class="metadata-section">
						<h3>{{ $t('lz.label.tms.vehicleInfo') }}</h3>
						<dl class="information-grid">
							<div v-for="item in vehicleMetadata" :key="item.key">
								<dt>{{ $t(item.label) }}</dt>
								<dd>{{ item.value }}</dd>
							</div>
						</dl>
					</div>
					<p v-if="selectedDetail.description" class="record-note">
						<strong>{{ $t('lz.entity.base.desc') }}</strong>
						{{ selectedDetail.description }}
					</p>
					<p v-if="selectedDetail.remark" class="record-note">
						<strong>{{ $t('lz.entity.base.remark') }}</strong>
						{{ selectedDetail.remark }}
					</p>
				</div>
				<IncidentHistory
					v-if="state.historyOpened"
					v-show="state.detailTab === 'history'"
					:key="monitorState.selectedKey"
					:record="selectedDetail"
					:version="monitorState.detailVersion"
					:active="active && state.detailTab === 'history' && visible"
					@open-full="openRelated(TmsReportRouteEnum.IncidentTimelineDetail)"
				/>
			</div>
			<div class="secondary-actions">
				<el-button :icon="Edit" v-if="canUpdate && selectedDetail.state === TmsIncidentStateEnum.Registration" :disabled="monitorState.saving" @click="emits('edit', selectedDetail)">
					{{ $t('lz.button.base.edit') }}
				</el-button>
				<el-button :icon="Printer" class="monitor-report-button" @click="openRelated(TmsReportRouteEnum.IncidentReportDetail)">
					{{ $t('lz.label.base.reportPrint') }}
				</el-button>
				<el-button v-if="canCancel(selectedDetail)" :icon="Close" type="danger" plain :disabled="monitorState.saving || monitorState.loading" @click="cancelIncident(selectedDetail)">
					{{ selectedDetail.state === TmsIncidentStateEnum.Registration ? $t('lz.button.tmsIncident.skip') : $t('lz.button.base.cancel') }}
				</el-button>
			</div>
		</template>
	</aside>
</template>

<script lang="ts">
import { TmsIncidentApi } from '/@/api-services/tms';
import type { EquipmentOutput } from '/@/api-services/tms';
import { getAPI } from '/@/utils/axios-utils';
import type { IncidentRow } from '../incidentModel';
import type { EvidenceImage, EvidenceLoader, EvidenceLoadOptions, EvidenceStage } from '/@/components/workspace/workspace';

export const incidentEvidenceStages: EvidenceStage[] = [
	{ value: 1, labelKey: 'lz.label.tmsIncident.reviewDetectionStage' },
	{ value: 2, labelKey: 'lz.label.tmsIncident.verifiedState' },
	{ value: 3, labelKey: 'lz.label.tmsIncident.reviewHandlingStage' },
	{ value: 4, labelKey: 'lz.label.tmsIncident.finishedState' },
];

// TMS owns capture stages, camera data and source-status interpretation.
export const loadIncidentEvidence = async (record: IncidentRow, { stage, signal, baseUrl }: EvidenceLoadOptions, equipment: EquipmentOutput[] = []): Promise<EvidenceImage[]> => {
	if (!record.id) return [];
	const response = await getAPI(TmsIncidentApi).apiTmsTmsincidentGetimagesGet(record.id, stage, { signal });
	const images = [...(response.data.result ?? [])];
	// Keep every associated camera selectable even when this stage has no capture for it.
	const cameraIds = [
		...new Set(
			String(record.equipmentId ?? '')
				.split(',')
				.map((id) => id.trim())
				.filter(Boolean)
		),
	];
	for (const equipmentId of cameraIds) {
		if (!images.some((image) => image.equipmentId === equipmentId)) images.push({ equipmentId, exists: false });
	}

	const fullUrl = (url?: string | null) => (!url ? undefined : /^(https?:|data:|blob:)/i.test(url) ? url : String(baseUrl ?? '').replace(/\/+$/, '') + '/' + url.replace(/^\/+/, ''));
	const equipmentById = new Map(equipment.map((item) => [String(item.id ?? '').trim(), item]));

	return images.map((image, index) => ({
		key: String(image.equipmentId ?? index) + ':' + stage,
		title:
			equipmentById.get(String(image.equipmentId ?? '').trim())?.name?.trim() ||
			equipmentById.get(String(image.equipmentId ?? '').trim())?.code?.trim() ||
			image.equipmentName?.trim() ||
			image.equipmentId?.trim() ||
			undefined,
		titleKey: 'lz.label.base.cameraNumber',
		titleParams: { index: index + 1 },
		url:
			image.exists === false
				? undefined
				: image.base64
					? image.base64.startsWith('data:')
						? image.base64
						: 'data:' + (image.contentType || 'image/jpeg') + ';base64,' + image.base64
					: fullUrl(image.url),
		statusKey:
			image.status === 'WaitingForSource'
				? 'lz.label.base.waitingForDeviceImage'
				: image.status === 'PendingCapture'
					? 'lz.label.base.capturingImage'
					: 'lz.label.base.noStageImage',
	}));
};
</script>

<script setup lang="ts" name="tmsIncidentDetails">
import { computed, defineAsyncComponent, reactive, ref, watch } from 'vue';
import { Document, CopyDocument, ArrowUp, ArrowDown, Lock, Clock, Edit, Printer, Close } from '@element-plus/icons-vue';
import TagInfo from '/@/components/advanced/tag-info.vue';
import { incidentMetadataRows } from '/@/utils/incidentMetadata';
import { useCommonVarsAndFuncs } from '/@/stores/its/commonVarsAndFuncs';
import { useTmsStore } from '/@/stores/tmsStore';
import { BaseConfigTypeEnum, TmsIncidentStateEnum, TmsReportRouteEnum } from '/@/types/enums';

import { useI18n } from 'vue-i18n';
import { ElMessage } from 'element-plus';
import { rowKey } from '../incidentModel';
import { plateOf } from '../../shared/monitorFormat';
import type { IncidentMonitor } from '../useIncidentMonitor';
import MediaPanel from '/@/components/workspace/MediaPanel.vue';

interface Props {
	visible: boolean;
	active: boolean;
	monitor: IncidentMonitor;
}
const props = defineProps<Props>();
const { t: $t } = useI18n();
const monitorState = props.monitor.state;
const evidencePanel = ref<{ navigateEvidence: (direction: -1 | 1) => boolean }>();
const emits = defineEmits<{ edit: [row: IncidentRow] }>();
const common = useCommonVarsAndFuncs();
const tmsStore = useTmsStore();
const IncidentHistory = defineAsyncComponent(async () => (await import('./incidentHistory.vue')).default);
const { selected, selectedDetail, selectedIndex, detailError, canView, canUpdate, hasPrevious, hasNext, stateLabels, details, configList, eventName, selectRow, move, canCancel, cancelIncident } =
	props.monitor;
const state = reactive({ detailTab: 'info', historyOpened: false });
const vehicleMetadata = computed(() => incidentMetadataRows(selectedDetail.value?.metadata));

const stateConfig = (row: IncidentRow) => configList(BaseConfigTypeEnum.TmsIncidentStatus).find((item) => String(item.code) === String(row.state));

const loadEvidence: EvidenceLoader<IncidentRow> = async (record, options) => {
	// Resolve the same equipment names used by the record details, including cameras with no capture.
	const equipment = record.equipmentId ? await tmsStore.getEquipmentByIds(record.equipmentId).catch(() => monitorState.allEquipment) : monitorState.allEquipment;
	if (options.signal.aborted) return [];
	return loadIncidentEvidence(record, options, equipment);
};

function navigateEvidence(direction: -1 | 1): boolean {
	if (!props.visible || !props.active || !canView.value || !selectedDetail.value) return false;
	return evidencePanel.value?.navigateEvidence(direction) ?? false;
}

async function copyPlate() {
	if (!selectedDetail.value || plateOf(selectedDetail.value) === '—') return;
	await copyText(plateOf(selectedDetail.value));
}

async function copyText(value: string) {
	try {
		await navigator.clipboard.writeText(value);
		ElMessage.success($t('lz.message.base.copySuccess'));
	} catch {
		ElMessage.error($t('lz.message.base.copyFailed'));
	}
}

async function copyIncidentCode() {
	if (selectedDetail.value?.code) await copyText(selectedDetail.value.code);
}

function openRelated(route: TmsReportRouteEnum) {
	if (selectedDetail.value?.id && canView.value) window.open(route + encodeURIComponent(selectedDetail.value.id), '_blank', 'noopener');
}

function openHistory() {
	if (!selectedDetail.value || !canView.value) return;
	state.historyOpened = true;
	state.detailTab = 'history';
}

watch([() => monitorState.selectedKey, () => monitorState.rows], () => {
	state.detailTab = 'info';
	state.historyOpened = false;
});

defineExpose({ navigateEvidence });
</script>
