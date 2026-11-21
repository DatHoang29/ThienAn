<template>
	<div class="fullscreen-camera-stage">
		<FullscreenFocusLayout v-if="focusedCamera" :camera="focusedCamera" :cameras="cameras" :focus-mode="focusMode"
			@back="$emit('clear-focus')" @focus-mode="$emit('focus-mode', $event)"
			@select-camera="$emit('replace-focus-camera', $event)"
			@screenshot="$emit('camera-action', 'screenshot', $event)" @info="openCameraInfo" />
		<CameraGrid ref="cameraGridRef" v-else :cameras="cameras" :edit-mode="editMode" fullscreen camera-only
			show-settings @detail="$emit('detail', $event)" @remove="$emit('remove', $event)"
			@add-slot="$emit('add-slot', $event)" @select="$emit('select', $event)"
			@settings="$emit('focus-settings', $event)" @camera-action="handleCameraAction"
			@layout-change="$emit('layout-change', $event)" @layout-dirty="$emit('layout-dirty')" />
		<el-dialog v-model="infoDialogVisible" width="460px" :append-to-body="false" destroy-on-close
			class="cctv-stage-info-dialog" modal-class="cctv-stage-info-modal" :z-index="10070">
			<template #header>
				<div style="color: #fff">
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle">
						<ele-InfoFilled />
					</el-icon>
					<span> {{ t('lz.label.cctvDashboard.cameraInfo') }} </span>
				</div>
			</template>
			<div v-if="infoCamera" class="camera-info-dialog-body">
				<div class="camera-info-row">
					<span>{{ t('lz.entity.base.name') }}</span>
					<strong>{{ infoCamera.name || infoCamera.id || '--' }}</strong>
				</div>
				<div class="camera-info-row">
					<span>IP</span>
					<strong>{{ infoCamera.ip || '--' }}</strong>
				</div>
				<div class="camera-info-row">
					<span>DeviceId</span>
					<strong>{{ infoCamera.deviceId || '--' }}</strong>
				</div>
				<div class="camera-info-row">
					<span>{{ t('lz.label.base.location') }}</span>
					<strong>{{ infoCamera.location || '--' }}</strong>
				</div>
				<div class="camera-info-row">
					<span>{{ t('lz.entity.base.status') }}</span>
					<strong>{{ infoCamera.status || '--' }}</strong>
				</div>
				<div class="camera-info-row">
					<span>{{ t('lz.label.cctvDashboard.cellSize') }}</span>
					<strong>x: {{ infoCamera.x }}, y: {{ infoCamera.y }}, w: {{ infoCamera.w }}, h: {{ infoCamera.h
						}}</strong>
				</div>
			</div>
			<template #footer>
				<el-button type="primary" @click="infoDialogVisible = false">{{ t('lz.button.base.close') }}</el-button>
			</template>
		</el-dialog>
	</div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import CameraGrid from './CameraGrid.vue';
import FullscreenFocusLayout from './FullscreenFocusLayout.vue';
import type { VideoMonitoringItem } from '/@/views/cctv/dashboard/types';
import { useI18n } from 'vue-i18n';

const cameraGridRef = ref<InstanceType<typeof CameraGrid> & { getCurrentLayout?: () => VideoMonitoringItem[] }>();
const { t } = useI18n();

const props = defineProps<{
	cameras: VideoMonitoringItem[];
	focusedCamera?: VideoMonitoringItem;
	focusMode?: 'view' | 'ptz' | 'playback';
	editMode?: boolean;
}>();

const emit = defineEmits<{
	(e: 'detail', id: string): void;
	(e: 'remove', id: string): void;
	(e: 'add-slot', id: string): void;
	(e: 'select', id: string): void;
	(e: 'replace-focus-camera', id: string): void;
	(e: 'focus-settings', id: string): void;
	(e: 'focus-mode', mode: 'view' | 'ptz' | 'playback'): void;
	(e: 'camera-action', action: 'ptz' | 'playback' | 'screenshot' | 'info', id: string): void;
	(e: 'clear-focus'): void;
	(e: 'layout-change', cameras: VideoMonitoringItem[]): void;
	(e: 'layout-dirty'): void;
}>();

const infoDialogVisible = ref(false);
const infoCamera = ref<VideoMonitoringItem | null>(null);

const openCameraInfo = (id: string) => {
	const camera = props.cameras.find((item) => item.id === id && !item.isPlaceholder);
	if (!camera) return;
	infoCamera.value = camera;
	infoDialogVisible.value = true;
};

const handleCameraAction = (action: 'ptz' | 'playback' | 'screenshot' | 'info', id: string) => {
	if (action === 'info') {
		openCameraInfo(id);
		return;
	}
	emit('camera-action', action, id);
};

defineExpose({
	getCurrentLayout: () => cameraGridRef.value?.getCurrentLayout?.(),
});
</script>

<style scoped lang="scss">
.fullscreen-camera-stage {
	width: 100vw;
	height: 100vh;
	overflow: hidden;
	background: #000000;
}

.fullscreen-camera-stage:fullscreen {
	background: #000000;
}

.camera-info-dialog-body {
	display: grid;
	gap: 8px;
}

.camera-info-row {
	display: grid;
	grid-template-columns: 110px minmax(0, 1fr);
	gap: 12px;
	align-items: start;
	padding: 9px 0;
	border-bottom: 1px solid #eef2f7;

	span {
		color: #667085;
		font-size: 13px;
	}

	strong {
		min-width: 0;
		color: #101828;
		font-size: 13px;
		font-weight: 600;
		overflow-wrap: anywhere;
	}
}

.camera-info-row:last-child {
	border-bottom: 0;
}

:global(.cctv-stage-info-modal) {
	z-index: 10070 !important;
}

:global(.cctv-stage-info-dialog) {
	z-index: 10071 !important;
}

</style>
