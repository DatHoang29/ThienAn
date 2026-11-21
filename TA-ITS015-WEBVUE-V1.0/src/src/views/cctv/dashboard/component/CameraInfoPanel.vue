<template>
	<div class="right-pane-card camera-info-card">
		<div class="camera-info-header">
			<h3>{{ t('lz.label.cctvDashboard.cameraDetail') }}</h3>
			<el-button link type="primary" @click="$emit('close')">{{ t('lz.button.base.close') }}</el-button>
		</div>
		<div class="camera-info-actions">
			<el-button type="primary" @click="$emit('replace')">
				<el-icon><ele-Switch /></el-icon>
				{{ t('lz.button.cctvDashboard.replaceCamera') }}
			</el-button>
			<el-button type="danger" plain @click="$emit('remove', selectedCamera.id)">
				<el-icon><ele-Delete /></el-icon>
				{{ t('lz.button.cctvDashboard.removeFromPreset') }}
			</el-button>
		</div>

		<h4 class="info-section-title">Preset</h4>
		<div class="camera-info-fields">
			<div class="info-row">
				<span class="info-label">{{ t('lz.label.cctvDashboard.presetName') }}</span>
				<span class="info-value">{{ activePreset?.Name || activePreset?.ID || '--' }}</span>
			</div>
			<div class="info-row">
				<span class="info-label">{{ t('lz.label.cctvDashboard.gridPosition') }}</span>
				<span class="info-value">x: {{ selectedCamera.x }}, y: {{ selectedCamera.y }}, w: {{ selectedCamera.w }}, h: {{ selectedCamera.h }}</span>
			</div>
		</div>
		<el-divider />
		<h4 class="info-section-title">Camera</h4>
		<div class="camera-info-fields">
			<div class="info-row">
				<span class="info-label">{{ t('lz.entity.base.name') }}</span>
				<span class="info-value">{{ selectedCamera.name || selectedCamera.id }}</span>
			</div>
			<div class="info-row">
				<span class="info-label">IP</span>
				<span class="info-value">{{ selectedCamera.ip || '--' }}</span>
			</div>
			<div class="info-row">
				<span class="info-label">{{ t('lz.entity.base.desc') }}</span>
				<span class="info-value">{{ selectedCamera.location || '--' }}</span>
			</div>
			<div class="info-row">
				<span class="info-label">{{ t('lz.entity.base.status') }}</span>
				<el-tag :type="selectedCamera.status === 'online' ? 'success' : 'danger'" size="small">
					{{ selectedCamera.status === 'online' ? t('lz.label.cctvDashboard.online') : t('lz.label.cctvDashboard.offline') }}
				</el-tag>
			</div>
			<div class="info-row">
				<span class="info-label">{{ t('lz.label.cctvDashboard.aspectRatio') }}</span>
				<span class="info-value">{{ selectedCamera.aspect || '--' }}</span>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import type { VideoMonitoringItem } from '/@/views/cctv/dashboard/types';
import { useI18n } from 'vue-i18n';

const { t } = useI18n();

type CameraDashboardPreset = {
	ID?: string;
	Name?: string;
};

type CameraDashboardPresetItem = {
	ID?: string;
};

defineProps<{
	selectedCamera: VideoMonitoringItem;
	selectedPresetItem?: CameraDashboardPresetItem;
	activePreset?: CameraDashboardPreset;
}>();

defineEmits<{
	(e: 'close'): void;
	(e: 'replace'): void;
	(e: 'remove', cameraId: string): void;
}>();
</script>

<style scoped lang="scss">
.right-pane-card {
	background: #ffffff;
	border: 1px solid #eaecf0;
	border-radius: 12px;
	padding: 14px;
}

.camera-info-header {
	display: flex;
	align-items: center;
	justify-content: space-between;
	margin-bottom: 12px;

	h3 {
		margin: 0;
		font-size: 16px;
		font-weight: 600;
	}
}

.camera-info-actions {
	display: grid;
	grid-template-columns: 1fr;
	gap: 8px;
	margin-bottom: 16px;

	:deep(.el-button) {
		width: 100%;
		margin-left: 0;
	}
}

.info-section-title {
	margin: 2vh 0;
	font-size: 13px;
	font-weight: 700;
	color: #344054;
	text-transform: uppercase;
}

.camera-info-fields {
	display: grid;
	gap: 10px;
}

.info-row {
	display: flex;
	align-items: center;
	justify-content: space-between;
	gap: 12px;
	padding: 8px 0;
	border-bottom: 1px dashed #eaecf0;
}

.info-row:last-child {
	border-bottom: none;
	padding-bottom: 0;
}

.info-label {
	font-size: 12px;
	color: #475467;
	font-weight: 600;
}

.info-value {
	text-align: right;
	font-size: 13px;
	color: #101828;
	word-break: break-word;
}

</style>
