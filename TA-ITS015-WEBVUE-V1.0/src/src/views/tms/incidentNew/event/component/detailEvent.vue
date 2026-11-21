<template>
	<aside v-show="visible" id="monitor-detail" class="detail-panel workspace-panel" :aria-label="$t('lz.label.base.selectedRecordDetails')">
		<header class="detail-heading workspace-panel-heading">
			<div>
				<div class="detail-heading-meta">
					<span class="eyebrow">{{ $t('lz.label.base.recordDetails') }}</span>
					<span v-if="selected" class="record-counter"> {{ selectedIndex + 1 }} / {{ monitorState.rows.length }} </span>
				</div>
				<h2 :title="selected ? plateOf(selected) : ''">
					{{ selected ? plateOf(selected) : $t('lz.label.base.recordDetails') }}
				</h2>
			</div>
			<div class="record-navigation">
				<el-button
					v-if="selectedDetail && canView"
					:icon="CopyDocument"
					text
					:disabled="plateOf(selectedDetail) === '—'"
					:title="$t('lz.label.tms.copyLicensePlate')"
					:aria-label="$t('lz.label.tms.copyLicensePlate')"
					@click="copyPlate()"
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
					:load="loadEventEvidence"
					:initial-images="eventEvidenceImages()"
					:version="monitorState.detailVersion"
				/>

				<div v-loading="monitorState.detailLoading" class="record-information">
					<dl class="information-grid">
						<div v-for="item in details" :key="item.label">
							<dt>{{ item.label }}</dt>
							<dd>{{ item.value }}</dd>
						</div>
					</dl>

					<p v-if="selectedDetail.description" class="record-note">
						<strong>{{ $t('lz.entity.base.desc') }}</strong>
						{{ selectedDetail.description }}
					</p>
					<p v-if="selectedDetail.remark" class="record-note">
						<strong>{{ $t('lz.entity.base.remark') }}</strong>
						{{ selectedDetail.remark }}
					</p>
				</div>
			</div>
			<div class="secondary-actions">
				<el-button :icon="Document" class="monitor-report-button" @click="openVehicleDetail()">
					{{ $t('lz.label.base.detailPrint') }}
				</el-button>
				<!-- Sự kiện đã có sự cố (TmsIncident.EventId = VendorEventId) thì mở bên tab Sự cố, chưa có thì Tạo sự cố. -->
				<el-button
					v-if="linkedIncident"
					v-auth="'tmsIncident:view'"
					type="warning"
					plain
					:icon="View"
					:title="$t('lz.label.tmsIncident.reviewLinkedIncident', { code: linkedIncident.code || '' })"
					@click="openLinkedIncident()"
				>
					{{ $t('lz.button.tmsIncident.viewIncident') }}
				</el-button>
				<!-- Chỉ loại thuộc nhóm sự cố mới tạo được sự cố: loại nhóm sự kiện / chưa ánh xạ thì nút bị khoá, rê chuột xem lý do. -->
				<span v-else v-auth="'tmsIncident:add'" :title="canCreateIncident(selectedDetail) ? undefined : $t('lz.label.tmsIncident.createNotIncidentType')">
					<el-button
						type="primary"
						plain
						:icon="Plus"
						:loading="linkedLoading"
						:disabled="linkedLoading || !canCreateIncident(selectedDetail)"
						@click="createIncident()"
					>
						{{ $t('lz.button.tmsIncident.createIncident') }}
					</el-button>
				</span>
			</div>
		</template>
	</aside>

	<TrafficAnalysisDetail v-if="viewState.vehicleDialogMounted" ref="vehicleDialog" :default-value="viewState.vehicleDialogData" />
</template>

<script lang="ts">
import { VdsMediaApi } from '/@/api-services/vds';
import { getAPI } from '/@/utils/axios-utils';
import { vdsTimestamp } from '/@/utils/vdsTimestamp';
import type { EventRow } from '../eventModel';
import type { EvidenceImage, EvidenceLoader } from '/@/components/workspace/workspace';

export const eventEvidenceImages = (): EvidenceImage[] => [
	{ key: 'full', titleKey: 'lz.label.tms.fullView' },
	{ key: 'vehicle', titleKey: 'lz.label.base.vehicle' },
];

// VDS owns the composite UTC key and the full/vehicle variants.
export const loadEventEvidence: EvidenceLoader<EventRow> = async (record, { signal, createObjectURL }) => {
	const variants = eventEvidenceImages();
	const eventId = Number(record.vendorEventId);
	if (!Number.isFinite(eventId) || eventId <= 0 || !record.detectTime) return variants;
	const detectTime = vdsTimestamp(record.detectTime) as unknown as Date;
	if (Number.isNaN(new Date(detectTime).getTime())) return variants;
	return await Promise.all(
		variants.map(async (item) => {
			try {
				const response = await getAPI(VdsMediaApi).apiVdsVdsmediaFileGet(eventId, detectTime, item.key, {
					responseType: 'blob',
					signal,
				});
				const blob = response.data as unknown as Blob;
				if (!(blob instanceof Blob) || !blob.size) return item;
				const bytes = new Uint8Array(await blob.slice(0, 12).arrayBuffer());
				const valid =
					(bytes[0] === 0xff && bytes[1] === 0xd8 && bytes[2] === 0xff) ||
					(bytes[0] === 0x89 && bytes[1] === 0x50 && bytes[2] === 0x4e && bytes[3] === 0x47) ||
					(bytes[0] === 0x47 && bytes[1] === 0x49 && bytes[2] === 0x46) ||
					(bytes[0] === 0x52 && bytes[1] === 0x49 && bytes[2] === 0x46 && bytes[8] === 0x57 && bytes[9] === 0x45 && bytes[10] === 0x42 && bytes[11] === 0x50);
				return valid ? { ...item, url: createObjectURL(blob) } : item;
			} catch {
				return item;
			}
		})
	);
};
</script>

<script setup lang="ts" name="tmsEventDetails">
import { computed, nextTick, reactive, ref } from 'vue';
import { Document, CopyDocument, ArrowUp, ArrowDown, Lock, Plus, View } from '@element-plus/icons-vue';
import { BaseConfigTypeEnum } from '/@/types/enums';
import TrafficAnalysisDetail from '/@/views/modal/trafficAnalysis/trafficAnalysisViewDetailModal.vue';
import { useI18n } from 'vue-i18n';
import { ElMessage } from 'element-plus';
import { rowKey } from '../eventModel';
import { plateOf } from '../../shared/monitorFormat';
import type { EventMonitor } from '../useEventMonitor';
import MediaPanel from '/@/components/workspace/MediaPanel.vue';

interface Props {
	visible: boolean;
	active: boolean;
	monitor: EventMonitor;
}
const props = defineProps<Props>();
const { t: $t } = useI18n();
const monitorState = props.monitor.state;
const evidencePanel = ref<{ navigateEvidence: (direction: -1 | 1) => boolean }>();

const { selected, selectedDetail, selectedIndex, detailError, canView, hasPrevious, hasNext, details, selectRow, move, equipmentName, configName, vehicleTypeName, openLinkedIncident, createIncident, canCreateIncident } = props.monitor;
// Chỉ dùng kết quả tra của đúng dòng đang chọn (tránh nút của dòng trước còn sót khi chuyển nhanh).
const linkedIncident = computed(() => (monitorState.linked.key === monitorState.selectedKey ? monitorState.linked.incident : null));
const linkedLoading = computed(() => monitorState.linked.key === monitorState.selectedKey && monitorState.linked.loading);
const viewState = reactive({ vehicleDialogMounted: false, vehicleDialogData: {} as Record<string, unknown> });
const vehicleDialog = ref<InstanceType<typeof TrafficAnalysisDetail>>();

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

async function openVehicleDetail() {
	if (!selectedDetail.value || !canView.value) return;
	viewState.vehicleDialogData = {
		...selectedDetail.value,
		equipmentName: equipmentName(selectedDetail.value),
		directionName: configName(BaseConfigTypeEnum.TmsDirectionType, selectedDetail.value.direction),
		vehicleTypeName: vehicleTypeName(selectedDetail.value),
	};
	viewState.vehicleDialogMounted = true;
	await nextTick();
	vehicleDialog.value?.showModal(true);
}

defineExpose({ navigateEvidence });
</script>
