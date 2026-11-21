<template>
	<aside class="ptz-panel" :style="{ zoom: panelZoom }">
		<PtzMovePad @move="handlePtzPress" @stop="handlePtzRelease" @center="centerCamera" />

		<div class="ptz-controls">
			<div class="control-row">
				<div>
					<label>{{ t('lz.label.cctvDashboard.zoom') }}</label>
					<div class="button-group">
						<el-button plain :icon="ZoomOut" circle :disabled="!activeCamera?.deviceId"
							@mousedown.prevent="handleZoomPress('out')" @mouseup="handleZoomRelease"
							@mouseleave="handleZoomRelease"></el-button>
						<el-button plain :icon="ZoomIn" circle :disabled="!activeCamera?.deviceId"
							@mousedown.prevent="handleZoomPress('in')" @mouseup="handleZoomRelease"
							@mouseleave="handleZoomRelease"></el-button>
					</div>
				</div>
				<div>
					<label>{{ t('lz.label.cctvDashboard.focus') }}</label>
					<div class="button-group">
						<el-button plain :icon="Minus" circle :disabled="!activeCamera?.deviceId"
							@mousedown.prevent="handleFocusPress('out')" @mouseup="handleFocusRelease"
							@mouseleave="handleFocusRelease"></el-button>
						<el-button plain :icon="Plus" circle :disabled="!activeCamera?.deviceId"
							@mousedown.prevent="handleFocusPress('in')" @mouseup="handleFocusRelease"
							@mouseleave="handleFocusRelease"></el-button>
					</div>
				</div>
			</div>
		</div>

		<section class="preset-section">
			<div class="preset-header">
				<strong>Preset</strong>
				<el-button size="small" type="primary" plain :loading="state.addingPreset" :disabled="!canControl"
					@click="handleAddPreset">
					<el-icon><ele-Plus /></el-icon>
					{{ t('lz.button.base.add') }}
				</el-button>
			</div>
			<div class="preset-list">
				<PtzPresetCard v-for="preset in presets" :key="preset.id" :preset="preset"
					:active="preset.id === activePresetId" @goto="gotoPreset" @remove="removePreset" />
				<el-empty v-if="!state.loadingPresets && !presets.length" class="preset-empty"
					:description="t('lz.label.cctvDashboard.noPresets')" :image-size="56" />
				<div v-if="state.loadingPresets" class="preset-loading">{{ t('lz.label.cctvDashboard.loadingPresets') }}
				</div>
			</div>
		</section>

		<!-- Add Preset Dialog -->
		<el-dialog v-model="state.addPresetDialogVisible" :title="t('lz.label.cctvDashboard.addPreset')" width="380px"
			:append-to-body="false" destroy-on-close :close-on-click-modal="false" class="ptz-preset-dialog">
			<el-form @submit.prevent="confirmAddPreset">
				<el-form-item :label="t('lz.label.cctvDashboard.presetName')">
					<el-input v-model="state.addPresetName" :placeholder="t('lz.label.cctvDashboard.presetName')"
						autofocus @keydown.enter.prevent="confirmAddPreset" />
				</el-form-item>
			</el-form>
			<template #footer>
				<el-button @click="state.addPresetDialogVisible = false">{{ t('lz.button.base.cancel') }}</el-button>
				<el-button type="primary" :loading="state.addingPreset" @click="confirmAddPreset">{{
					t('lz.button.base.confirm') }}</el-button>
			</template>
		</el-dialog>

		<!-- Delete Preset Confirm Dialog -->
		<el-dialog v-model="state.deletePresetDialogVisible" :title="t('lz.label.cctvDashboard.deletePreset')"
			width="380px" :append-to-body="false" destroy-on-close :close-on-click-modal="false"
			class="ptz-preset-dialog">
			<p>{{ t('lz.message.cctvDashboard.confirmDeletePreset', { name: state.deletePresetName }) }}</p>
			<template #footer>
				<el-button @click="state.deletePresetDialogVisible = false">{{ t('lz.button.base.cancel') }}</el-button>
				<el-button type="danger" :loading="state.deletingPreset" @click="confirmDeletePreset">{{
					t('lz.button.base.confirm') }}</el-button>
			</template>
		</el-dialog>
	</aside>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, reactive, ref, watch } from 'vue';
import { ElMessage } from 'element-plus';
import PtzMovePad from '/@/views/cctv/dashboard/component/PtzMovePad.vue';
import PtzPresetCard, { type PtzPreset } from '/@/views/cctv/dashboard/component/PtzPresetCard.vue';
import type { VideoMonitoringItem } from '/@/views/cctv/dashboard/types';
import { Minus, Plus, ZoomIn, ZoomOut } from '@element-plus/icons-vue';
import {
	activateCctvPtzPreset,
	addCctvPtzPreset,
	deleteCctvPtzPreset,
	focusCctvPtz,
	loadCctvPtzPresets,
	nudgeCctvPtz,
	stopCctvPtz,
	zoomCctvPtz,
} from '/@/views/cctv/dashboard/services/cctvDashboardService';
import { PtzDirection, ZoomDirection } from '/@/api-services/cctv';
import { useI18n } from 'vue-i18n';

const { t } = useI18n();

type MoveDirection = 'up' | 'up-right' | 'right' | 'down-right' | 'down' | 'down-left' | 'left' | 'up-left';

const props = defineProps<{
	camera?: VideoMonitoringItem;
	/** DeviceId camera (dùng khi nhúng mà không truyền cả object camera). */
	deviceId?: string;
	/** IP camera — 1 trong 2 (deviceId/ip) phải có. Hiện chỉ nhận làm input; backend sẽ xử lý sau. */
	ip?: string;
	/** Kích thước khung: small = 1/2, medium = 3/4, large = như hiện tại (mặc định). */
	size?: 'small' | 'medium' | 'large';
	/** Hệ số zoom do cha tính theo kích thước modal (co giãn PTZ theo container). */
	zoom?: number;
}>();

// Camera hiệu dụng: ưu tiên object `camera`; nếu không có thì dựng tối thiểu từ deviceId/ip (dùng khi nhúng vào modal/tab).
const activeCamera = computed<VideoMonitoringItem>(() => {
	const passedCamera = props['camera'];
	if (passedCamera) return passedCamera;
	const deviceId = props.deviceId?.trim() || undefined;
	const ip = props.ip?.trim() || undefined;
	return {
		id: deviceId || ip || 'ptz',
		name: '',
		location: '',
		status: 'offline',
		aspect: '16:9',
		videoUrl: '',
		x: 0,
		y: 0,
		w: 0,
		h: 0,
		deviceId,
		ip,
	};
});

/** Kích thước khung (mặc định large). */
const panelSize = computed(() => props.size || 'large');

/** Yêu cầu ≥1 khóa: deviceId hoặc ip. */
const hasCameraKey = computed(() => Boolean(activeCamera.value.deviceId || activeCamera.value.ip));

/** Nhãn phụ ở header: ưu tiên tên, rồi deviceId, rồi ip; không có khóa nào thì báo chưa chọn camera. */
const subtitleText = computed(() => {
	if (!hasCameraKey.value) return t('lz.label.cctvDashboard.noCameraSelected');
	return activeCamera.value.name || activeCamera.value.deviceId || activeCamera.value.ip || t('lz.label.cctvDashboard.noCameraSelected');
});

const activePresetId = ref('');
const presets = ref<PtzPreset[]>([]);
const state = reactive({
	action: '',
	loadingPresets: false,
	addingPreset: false,
	addPresetDialogVisible: false,
	addPresetName: '',
	deletePresetDialogVisible: false,
	deletePresetId: '',
	deletePresetName: '',
	deletingPreset: false,
});

const canControl = computed(() => Boolean(activeCamera.value?.deviceId) && !state.action);

const getCameraStatusLabel = (status?: VideoMonitoringItem['status']) => {
	if (status === 'online') return t('lz.label.cctvDashboard.online');
	if (status === 'warning') return t('lz.label.base.warning');
	if (status === 'offline') return t('lz.label.cctvDashboard.offline');
	return t('lz.label.base.unknown');
};

const withPtzAction = async (action: string, task: () => Promise<void>) => {
	if (!activeCamera.value?.deviceId) {
		ElMessage.warning(t('lz.message.cctvDashboard.cameraDeviceIdNotFound'));
		return;
	}
	if (state.action) return;
	state.action = action;
	try {
		await task();
	} catch (error) {
		ElMessage.error(error instanceof Error ? error.message : String(error));
	} finally {
		state.action = '';
	}
};

// Direction mapping for PTZ nudge API
const directionToPtzDirection: Record<MoveDirection, PtzDirection> = {
	'left': PtzDirection.NUMBER_1,
	'right': PtzDirection.NUMBER_2,
	'up': PtzDirection.NUMBER_3,
	'down': PtzDirection.NUMBER_4,
	'up-left': PtzDirection.NUMBER_5,
	'up-right': PtzDirection.NUMBER_6,
	'down-left': PtzDirection.NUMBER_7,
	'down-right': PtzDirection.NUMBER_8,
};

const refreshPresets = async () => {
	if (!activeCamera.value?.deviceId) {
		presets.value = [];
		activePresetId.value = '';
		return;
	}
	state.loadingPresets = true;
	try {
		presets.value = await loadCctvPtzPresets(activeCamera.value);
		if (!presets.value.some((preset) => preset.id === activePresetId.value)) {
			activePresetId.value = presets.value[0]?.id || '';
		}
	} catch {
		presets.value = [];
		activePresetId.value = '';
	} finally {
		state.loadingPresets = false;
	}
};

const gotoPreset = (id: string) => {
	withPtzAction(`preset-${id}`, async () => {
		if (!activeCamera.value) return;
		await activateCctvPtzPreset(activeCamera.value, id);
		activePresetId.value = id;
	});
};

const removePreset = (id: string) => {
	const preset = presets.value.find((p) => p.id === id);
	if (!preset) return;
	state.deletePresetId = id;
	state.deletePresetName = preset.name;
	state.deletePresetDialogVisible = true;
};

const confirmDeletePreset = async () => {
	const id = state.deletePresetId;
	if (!id || !activeCamera.value) return;
	state.deletingPreset = true;
	try {
		await deleteCctvPtzPreset(activeCamera.value, id);
		const index = presets.value.findIndex((preset) => preset.id === id);
		const nextPresets = presets.value.filter((preset) => preset.id !== id);
		presets.value = nextPresets;
		if (activePresetId.value === id) {
			activePresetId.value = nextPresets[index]?.id || nextPresets[index - 1]?.id || '';
		}
		state.deletePresetDialogVisible = false;
	} catch (error) {
		ElMessage.error(error instanceof Error ? error.message : String(error));
	} finally {
		state.deletingPreset = false;
	}
};

const handleAddPreset = () => {
	if (!activeCamera.value?.deviceId || state.addingPreset) return;
	state.addPresetName = `Preset ${presets.value.length + 1}`;
	state.addPresetDialogVisible = true;
};

const confirmAddPreset = async () => {
	const name = state.addPresetName.trim();
	if (!name || !activeCamera.value?.deviceId) return;
	state.addingPreset = true;
	try {
		await addCctvPtzPreset(activeCamera.value, name);
		await refreshPresets();
		ElMessage.success(t('lz.message.cctvDashboard.addPresetSuccess'));
		state.addPresetDialogVisible = false;
	} catch (error) {
		ElMessage.error(error instanceof Error ? error.message : String(error));
	} finally {
		state.addingPreset = false;
	}
};

// ===== PTZ Move: nhấn = gửi isHold:false (server tự stop), giữ = gửi isHold:true (thả mới stop) =====

let ptzHoldTimer: ReturnType<typeof setTimeout> | null = null;
let ptzIsHolding = false;
let ptzHeartbeat: ReturnType<typeof setInterval> | null = null;

/** Giữ nút lâu: gửi lại lệnh isHold định kỳ, phải nhỏ hơn thời gian BE tự dừng (ContinuousMoveAutoStopMs, mặc định 5s). */
const PTZ_HOLD_HEARTBEAT_MS = 2000;

const handlePtzPress = (direction: MoveDirection) => {
	if (!activeCamera.value?.deviceId || state.action) return;
	const ptzDir = directionToPtzDirection[direction];
	ptzIsHolding = false;

	// Đặt timer: nếu giữ > 200ms → chuyển sang chế độ hold, gửi lại lệnh với isHold=true
	ptzHoldTimer = setTimeout(async () => {
		ptzIsHolding = true;
		if (!activeCamera.value) return;
		const holdCamera = activeCamera.value;
		ptzHeartbeat = setInterval(() => {
			nudgeCctvPtz(holdCamera, ptzDir, { isHold: true }).catch(() => undefined);
		}, PTZ_HOLD_HEARTBEAT_MS);
		try {
			await nudgeCctvPtz(activeCamera.value, ptzDir, { isHold: true });
		} catch { /* silent */ }
	}, 200);

	// Gửi lệnh ngay với isHold=false (server sẽ tự stop theo cấu hình)
	withPtzAction(direction, async () => {
		if (!activeCamera.value) return;
		await nudgeCctvPtz(activeCamera.value, ptzDir, { isHold: false });
	});
};

const handlePtzRelease = () => {
	// Hủy timer nếu chưa kịp hold
	if (ptzHoldTimer) {
		clearTimeout(ptzHoldTimer);
		ptzHoldTimer = null;
	}
	if (ptzHeartbeat) {
		clearInterval(ptzHeartbeat);
		ptzHeartbeat = null;
	}
	// Chỉ gửi stop nếu đang ở chế độ hold
	if (ptzIsHolding && activeCamera.value?.deviceId) {
		stopCctvPtz(activeCamera.value).catch(() => undefined);
	}
	ptzIsHolding = false;
};

// ===== Zoom: nhấn = isHold:false, giữ > 200ms = isHold:true, thả = stop =====

let zoomHoldTimer: ReturnType<typeof setTimeout> | null = null;
let zoomIsHolding = false;
let zoomPressing = false;
let zoomHeartbeat: ReturnType<typeof setInterval> | null = null;

const handleZoomPress = async (mode: 'in' | 'out') => {
	if (!activeCamera.value?.deviceId) return;
	zoomPressing = true;
	zoomIsHolding = false;
	const direction = mode === 'in' ? ZoomDirection.NUMBER_1 : ZoomDirection.NUMBER_2;

	// Timer: giữ > 200ms → gửi lại với isHold=true
	zoomHoldTimer = setTimeout(async () => {
		zoomIsHolding = true;
		if (!activeCamera.value) return;
		const holdCamera = activeCamera.value;
		zoomHeartbeat = setInterval(() => {
			zoomCctvPtz(holdCamera, direction, { isHold: true }).catch(() => undefined);
		}, PTZ_HOLD_HEARTBEAT_MS);
		try { await zoomCctvPtz(activeCamera.value, direction, { isHold: true }); } catch { /* silent */ }
	}, 200);

	// Gửi ngay với isHold=false
	try { await zoomCctvPtz(activeCamera.value, direction, { isHold: false }); } catch (e) {
		ElMessage.error(e instanceof Error ? e.message : String(e));
	}
};

const handleZoomRelease = () => {
	if (!zoomPressing) return;
	zoomPressing = false;
	if (zoomHoldTimer) { clearTimeout(zoomHoldTimer); zoomHoldTimer = null; }
	if (zoomHeartbeat) { clearInterval(zoomHeartbeat); zoomHeartbeat = null; }
	if (zoomIsHolding && activeCamera.value?.deviceId) {
		stopCctvPtz(activeCamera.value).catch(() => undefined);
	}
	zoomIsHolding = false;
};

// Rời màn hình khi đang giữ nút: dừng gửi lặp + gửi Stop (BE vẫn tự dừng nếu Stop không tới được).
onBeforeUnmount(() => {
	handlePtzRelease();
	handleZoomRelease();
	handleFocusRelease();
});

// ===== Focus: nhấn = gửi 1 lần (server tự stop), giữ > 200ms = gửi lặp lại liên tục, thả = stop =====

let focusHoldTimer: ReturnType<typeof setTimeout> | null = null;
let focusRepeatInterval: ReturnType<typeof setInterval> | null = null;
let focusIsHolding = false;
let focusPressing = false;

const handleFocusPress = async (mode: 'in' | 'out') => {
	if (!activeCamera.value?.deviceId) return;
	focusPressing = true;
	focusIsHolding = false;
	const direction: 1 | 2 = mode === 'in' ? 1 : 2;

	// Timer: giữ > 200ms → chuyển sang hold mode, gửi lặp lại mỗi 200ms
	focusHoldTimer = setTimeout(() => {
		focusIsHolding = true;
		focusRepeatInterval = setInterval(async () => {
			if (!activeCamera.value?.deviceId || !focusPressing) {
				if (focusRepeatInterval) { clearInterval(focusRepeatInterval); focusRepeatInterval = null; }
				return;
			}
			try { await focusCctvPtz(activeCamera.value, direction); } catch { /* silent */ }
		}, 200);
	}, 200);

	// Gửi ngay lần đầu
	try { await focusCctvPtz(activeCamera.value, direction); } catch (e) {
		ElMessage.error(e instanceof Error ? e.message : String(e));
	}
};

const handleFocusRelease = () => {
	if (!focusPressing) return;
	focusPressing = false;
	if (focusHoldTimer) { clearTimeout(focusHoldTimer); focusHoldTimer = null; }
	if (focusRepeatInterval) { clearInterval(focusRepeatInterval); focusRepeatInterval = null; }
	if (focusIsHolding && activeCamera.value?.deviceId) {
		stopCctvPtz(activeCamera.value).catch(() => undefined);
	}
	focusIsHolding = false;
};

const centerCamera = () => {
	withPtzAction('stop', async () => {
		if (!activeCamera.value) return;
		await stopCctvPtz(activeCamera.value);
	});
};

watch(() => activeCamera.value?.deviceId, () => refreshPresets(), { immediate: true });

// `zoom` do component cha (CctvFocusPanel) tính theo kích thước modal rồi truyền xuống. Dùng CSS `zoom`
// (không phải transform) nên footprint co theo -> cột lưới `auto` luôn ôm khít khung PTZ, không tràn.
const panelZoom = computed(() => (props.zoom && props.zoom > 0 ? props.zoom : 1));
</script>

<style scoped lang="scss">
.ptz-panel {
	/* Kích thước tự nhiên theo nội dung; cha truyền CSS `zoom` để co giãn theo modal. Footprint co theo
	   nên cột lưới `auto` ở CctvFocusPanel luôn ôm khít khung, không tràn ngang. */
	width: max-content;
	padding: 16px;
	display: grid;
	grid-template-rows: auto auto auto;
	gap: 16px;
	//background: #111827;
	//border: 1px solid rgba(255, 255, 255, 0.08);
	border-radius: 12px;
	color: #ffffff;
}

.panel-header {
	display: flex;
	align-items: flex-start;
	justify-content: space-between;
	gap: 12px;

	h3 {
		margin: 0;
		font-size: 16px;
		line-height: 22px;
	}


}

.panel-header-subtitle {
	display: block;
	margin-top: 2px;
	color: #cbd5e1;
	font-size: 12px;
}

.ptz-controls {
	display: grid;
	align-content: start;
	gap: 12px;

	label {
		color: #cbd5e1;
		font-size: 12px;
		font-weight: 600;
	}
}

.control-row {
	display: flex;
	gap: 24px;
	align-items: flex-start;
	justify-content: space-between;
}

.control-row>div {
	display: grid;
	gap: 10px;
}

.button-group {
	display: flex;
	align-items: center;
	gap: 8px;

	:deep(.el-button) {
		margin-left: 0;
	}
}

.control-note,
.preset-loading {
	color: #94a3b8;
	font-size: 12px;
	line-height: 18px;
}

.preset-section {
	min-height: 0;
	margin-top: 8px;
	display: grid;
	grid-template-rows: auto minmax(0, 1fr);
	gap: 10px;
}

.preset-header {
	display: flex;
	align-items: center;
	justify-content: space-between;
	gap: 10px;

	strong {
		font-size: 13px;
	}
}

.preset-list {
	min-height: 0;
	max-height: 150px;
	display: grid;
	align-content: start;
	gap: 8px;
	overflow: auto;
	padding-right: 2px;
}

.preset-empty {
	padding: 12px 0;

	:deep(.el-empty__description p) {
		color: #cbd5e1;
		font-size: 12px;
	}
}
</style>

<style>
.ptz-preset-dialog .el-dialog__body {
	padding-bottom: 8px;
}

.ptz-preset-dialog .el-dialog__body p {
	margin: 0;
	color: #cbd5e1;
	font-size: 14px;
	line-height: 1.5;
}
</style>
