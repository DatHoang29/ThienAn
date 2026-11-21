<!--
  Chi tiết một bản ghi trạng thái thiết bị (VmsEquipmentTelemetry).
  Hiển thị thông tin chung + các thông số đọc được, gom theo 3 cột JSON
  (resourceMetric / environmentMetric / diagnosticMetric).
  Author: ChuongLP
-->
<template>
	<el-dialog v-model="state.isShowDialog" draggable :close-on-click-modal="false" width="720px">
		<template #header>
			<div style="color: #fff">
				<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"><ele-View /></el-icon>
				<span> {{ props.title }} </span>
			</div>
		</template>

		<!-- Thông tin chung -->
		<el-descriptions :column="2" border size="small">
			<el-descriptions-item :label="$t('lz.entity.base.code')">{{ state.row.equipmentCode || '-' }}</el-descriptions-item>
			<el-descriptions-item :label="$t('lz.entity.base.name')">{{ state.row.equipmentName || '-' }}</el-descriptions-item>
			<el-descriptions-item :label="$t('lz.entity.tms.equipmentType')">{{ state.row.equipmentTypeName || '-' }}</el-descriptions-item>
			<el-descriptions-item :label="$t('lz.label.vmsDeviceStatus.vendor')">{{ state.row.vendor || '-' }}</el-descriptions-item>
			<el-descriptions-item :label="$t('lz.label.vmsDeviceStatus.source')">
				{{ state.row.source === 'auto' ? $t('lz.label.vmsDeviceStatus.sourceAuto') : $t('lz.label.vmsDeviceStatus.sourceManual') }}
			</el-descriptions-item>
			<el-descriptions-item :label="$t('lz.entity.vmsDeviceStatus.recordTime')">{{ formatTime(state.row.collectedTime) }}</el-descriptions-item>
			<el-descriptions-item :label="$t('lz.entity.base.status')">
				<el-tag :type="state.row.succeeded ? 'success' : 'danger'" size="small" effect="dark">
					{{ state.row.succeeded ? $t('lz.label.vmsDeviceStatus.succeeded') : $t('lz.label.vmsDeviceStatus.failed') }}
				</el-tag>
			</el-descriptions-item>
			<el-descriptions-item v-if="state.row.message" :label="$t('lz.label.vmsDeviceStatus.message')" :span="2">
				<span style="color: var(--el-color-danger)">{{ state.row.message }}</span>
			</el-descriptions-item>
		</el-descriptions>

		<!-- Thông số theo nhóm -->
		<template v-for="group in groups" :key="group.key">
			<el-divider content-position="left">{{ $t(group.labelKey) }}</el-divider>
			<el-descriptions :column="2" border size="small">
				<el-descriptions-item v-for="meta in group.fields" :key="meta.key" :label="$t(meta.labelKey)">
					<el-tag v-if="meta.type === 'door'" :type="isTruthy(stats[meta.key]) ? 'danger' : 'success'" size="small" effect="dark">
						{{ isTruthy(stats[meta.key]) ? $t('lz.label.vmsDeviceStatus.doorOpen') : $t('lz.label.vmsDeviceStatus.doorClosed') }}
					</el-tag>
					<el-tag v-else-if="meta.type === 'power'" :type="isTruthy(stats[meta.key]) ? 'success' : 'info'" size="small" effect="dark">
						{{ isTruthy(stats[meta.key]) ? $t('lz.label.vmsDeviceStatus.online') : $t('lz.label.vmsDeviceStatus.offline') }}
					</el-tag>
					<template v-else>{{ formatValue(stats[meta.key], meta.unit) }}</template>
				</el-descriptions-item>
			</el-descriptions>
		</template>

		<el-empty v-if="!groups.length" :image-size="80" :description="$t('lz.label.base.noData')" />

		<template #footer>
			<span class="dialog-footer">
				<el-button @click="cancel">{{ $t('lz.button.base.close') }}</el-button>
			</span>
		</template>
	</el-dialog>
</template>

<script lang="ts" setup name="vmsTelemetryDetail">
import { reactive, computed } from 'vue';
import { useI18n } from 'vue-i18n';

const { t: $t } = useI18n();

interface Props {
	title: string;
}
const props = defineProps<Props>();

// Cấu trúc nhóm hiển thị — mỗi nhóm ứng với MỘT cột JSON từ BE
// (col = tên cột trên bản ghi telemetry: resourceMetric/environmentMetric/diagnosticMetric)
const GROUPS: { key: string; col: string; labelKey: string; fields: { key: string; labelKey: string; unit?: string; type?: 'door' | 'power' }[] }[] = [
	{
		key: 'resource',
		col: 'resourceMetric',
		labelKey: 'lz.label.vmsDeviceStatus.resource',
		fields: [
			{ key: 'brightness', labelKey: 'lz.entity.vmsDeviceStatus.brightness' },
			{ key: 'powerStatus', labelKey: 'lz.entity.vmsDeviceStatus.powerStatus', type: 'power' },
		],
	},
	{
		key: 'environment',
		col: 'environmentMetric',
		labelKey: 'lz.label.vmsDeviceStatus.environment',
		fields: [
			{ key: 'temperatureIn', labelKey: 'lz.entity.vmsDeviceStatus.temperatureIn', unit: '°C' },
			{ key: 'temperatureOut', labelKey: 'lz.entity.vmsDeviceStatus.temperatureOut', unit: '°C' },
			{ key: 'humidityIn', labelKey: 'lz.entity.vmsDeviceStatus.humidityIn', unit: '%' },
			{ key: 'humidityOut', labelKey: 'lz.entity.vmsDeviceStatus.humidityOut', unit: '%' },
			{ key: 'windSpeed', labelKey: 'lz.entity.vmsDeviceStatus.windSpeed', unit: 'm/s' },
			{ key: 'windDirection', labelKey: 'lz.entity.vmsDeviceStatus.windDirection' },
			{ key: 'gradient', labelKey: 'lz.entity.vmsDeviceStatus.gradient' },
		],
	},
	{
		key: 'diagnostic',
		col: 'diagnosticMetric',
		labelKey: 'lz.label.vmsDeviceStatus.diagnostic',
		fields: [
			{ key: 'pixelErrorCount', labelKey: 'lz.entity.vmsDeviceStatus.pixelErrorCount' },
			{ key: 'firmwareVersion', labelKey: 'lz.entity.vmsDeviceStatus.firmwareVersion' },
			{ key: 'doorOpen', labelKey: 'lz.entity.vmsDeviceStatus.door', type: 'door' },
		],
	},
];

const state = reactive({
	isShowDialog: false,
	row: {} as any,
});

// Gom 3 cột JSON (resourceMetric/environmentMetric/diagnosticMetric) thành map phẳng { statKey: value }
const stats = computed<Record<string, any>>(() => {
	const flat: Record<string, any> = {};
	['resourceMetric', 'environmentMetric', 'diagnosticMetric'].forEach((col) => {
		const raw = state.row?.[col];
		if (!raw) return;
		try {
			const obj = JSON.parse(raw);
			if (obj && typeof obj === 'object') Object.assign(flat, obj);
		} catch {
			/* bỏ qua cột JSON lỗi định dạng */
		}
	});
	return flat;
});

// Chỉ hiển thị field có giá trị, gom theo nhóm
const groups = computed(() =>
	GROUPS.map((g) => ({
		...g,
		fields: g.fields.filter((f) => stats.value[f.key] !== undefined && stats.value[f.key] !== null),
	})).filter((g) => g.fields.length)
);

const isTruthy = (v: any) => v === 1 || v === '1' || v === true || v === 'true';

const formatValue = (v: any, unit?: string) => {
	if (v === undefined || v === null || v === '') return '-';
	return unit ? `${v} ${unit}` : `${v}`;
};

const formatTime = (d?: string | Date | null) => {
	if (!d) return '-';
	const date = new Date(d);
	return isNaN(date.getTime()) ? '-' : date.toLocaleString('vi-VN');
};

const openDialog = (row: any) => {
	state.row = JSON.parse(JSON.stringify(row ?? {}));
	state.isShowDialog = true;
};

const cancel = () => {
	state.isShowDialog = false;
};

defineExpose({ openDialog });
</script>
