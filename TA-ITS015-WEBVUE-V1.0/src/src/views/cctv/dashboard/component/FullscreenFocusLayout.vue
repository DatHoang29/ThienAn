<template>
	<section class="fullscreen-focus-layout" :class="{ 'with-ptz': ptzOpen, 'with-playback': playbackOpen }">
		<div class="focus-main" @click="onFocusMainClick">
			<el-button class="fullscreen-back" type="primary" plain @click.stop="$emit('back')">
				<el-icon><ele-ArrowLeft /></el-icon>
				{{ t('lz.button.cctvDashboard.backToGrid') }}
			</el-button>
			<div class="focus-actions" @click.stop>
				<el-tooltip :content="t('lz.label.cctvDashboard.selectCamera')" placement="bottom" :show-after="300"
					:z-index="CameraControlLayer.MESSAGE" :teleported="false">
					<el-dropdown trigger="click" placement="bottom-end" popper-class="cctv-focus-camera-popper"
						:teleported="false" @command="handleSelectCameraCommand" @click.stop>
						<el-button class="camera-control-btn camera-control-btn--icon focus-icon-button" plain>
							<el-icon><ele-Menu /></el-icon>
						</el-button>
						<template #dropdown>
							<el-dropdown-menu class="focus-camera-menu">
								<el-dropdown-item v-if="selectableCameras.length === 0" disabled>
									<div class="focus-camera-empty">{{ t('lz.label.cctvDashboard.noAvailableCamera') }}
									</div>
								</el-dropdown-item>
								<el-dropdown-item v-for="item in selectableCameras" :key="item.id" :command="item.id"
									:class="{ 'is-active': isCameraSelected(item, currentCameraId) }">
									<div class="focus-camera-option">
										<strong>{{ item.name || item.id }}</strong>
										<span>{{ item.ip || item.location || '--' }}</span>
									</div>
								</el-dropdown-item>
							</el-dropdown-menu>
						</template>
					</el-dropdown>
				</el-tooltip>
				<el-dropdown trigger="click" placement="bottom-end" :teleported="false"
					@command="handleStreamChange" @click.stop>
					<el-button class="camera-control-btn camera-control-btn--labeled" plain @click.stop
						:title="t('lz.label.cctvDashboard.selectStream')">
						<el-icon><ele-Connection /></el-icon>
						{{ currentStreamLabel }}
					</el-button>
					<template #dropdown>
						<el-dropdown-menu>
							<el-dropdown-item v-for="opt in streamOptions" :key="opt.code" :command="opt.code"
								:class="{ 'is-active': selectedStream === opt.code }">
								{{ opt.name }}
							</el-dropdown-item>
						</el-dropdown-menu>
					</template>
				</el-dropdown>
				<el-tooltip
					:content="canControlLive ? t('lz.button.cctvDashboard.openPtz') : liveControlDisabledTooltip"
					placement="bottom" :show-after="180" :teleported="false" :z-index="CameraControlLayer.MESSAGE">
					<el-button class="camera-control-btn camera-control-btn--labeled"
						:class="{ 'is-active': focusMode === 'ptz' }" plain :disabled="!canControlLive"
						@click.stop="toggleMode('ptz')">{{ t('lz.label.cctvDashboard.ptz')
						}}</el-button>
				</el-tooltip>
				<el-tooltip :content="canPlayback ? t('lz.button.cctvDashboard.openPlayback') : playbackDisabledTooltip"
					placement="bottom" :show-after="180" :teleported="false" :z-index="CameraControlLayer.MESSAGE">
					<el-button class="camera-control-btn camera-control-btn--labeled"
						:class="{ 'is-active': focusMode === 'playback' }" plain :disabled="!canPlayback"
						@click.stop="toggleMode('playback')">
						<el-icon><ele-VideoPlay /></el-icon>
						{{ t('lz.label.cctvDashboard.playback') }}
					</el-button>
				</el-tooltip>
				<el-tooltip
					:content="canControlLive ? t('lz.button.cctvDashboard.captureSnapshot') : liveControlDisabledTooltip"
					placement="bottom" :show-after="180" :teleported="false" :z-index="CameraControlLayer.MESSAGE">
					<el-button class="camera-control-btn camera-control-btn--labeled" plain :loading="snapshotLoading"
						:disabled="!canControlLive" @click.stop="captureCurrentSnapshot">
						<el-icon><ele-Camera /></el-icon>
						{{ t('lz.label.cctvDashboard.screenshot') }}
					</el-button>
				</el-tooltip>
				<el-tooltip :content="t('lz.label.cctvDashboard.cameraInfo')" placement="bottom" :show-after="180"
					:teleported="false" :z-index="CameraControlLayer.MESSAGE">
					<el-button class="camera-control-btn camera-control-btn--labeled" plain
						@click.stop="openInfoDialog">
						<el-icon><ele-InfoFilled /></el-icon>
						{{ t('lz.label.cctvDashboard.info') }}
					</el-button>
				</el-tooltip>
			</div>
			<CameraTile :camera="camera" camera-only view-mode="focus" :show-controls="focusMode !== 'view'"
				:back-on-click="focusMode === 'view'" :video-url-override="activeVideoUrlOverride"
				:loading="videoLoading" :disable-fallback-resolve="disableTileFallbackResolve"
				:playback-active="isPlaybackActive" :playback-paused="playbackPaused"
				:playback-session-id="playbackSessionId" :playback-speed="playbackSpeed" auto-play
				class="fullscreen-focus-tile" @back-to-grid="$emit('back')" @media-ready="handleFocusMediaReady" @stream-expired="handleStreamExpired"
				@playback-progress="handlePlaybackProgress" />
			<el-tooltip
				:content="ptzOpen ? t('lz.button.cctvDashboard.collapsePtz') : t('lz.button.cctvDashboard.openPtz')"
				placement="left" :show-after="180" :teleported="false" :z-index="CameraControlLayer.MESSAGE">
				<el-button class="panel-toggle is-ptz" :class="{ 'is-open': ptzOpen }" :disabled="!canControlLive"
					@click.stop="togglePtzPanel">
					<el-icon>
						<ele-ArrowRight v-if="ptzOpen" />
						<ele-ArrowLeft v-else />
					</el-icon>
				</el-button>
			</el-tooltip>
			<el-tooltip
				:content="playbackOpen ? t('lz.button.cctvDashboard.collapsePlayback') : t('lz.button.cctvDashboard.openPlayback')"
				placement="top" :show-after="180" :teleported="false" :z-index="CameraControlLayer.MESSAGE">
				<el-button class="panel-toggle is-playback" :class="{ 'is-open': playbackOpen }"
					:disabled="!canPlayback" @click.stop="togglePlaybackPanel">
					<el-icon>
						<ele-ArrowDown v-if="playbackOpen" />
						<ele-ArrowUp v-else />
					</el-icon>
				</el-button>
			</el-tooltip>
		</div>
		<PtzControlPanel v-if="ptzOpen" :camera="camera" class="ptz-area" />
		<PlaybackBar v-if="playbackOpen" ref="playbackBarRef" :camera="camera" :loading="actionLoading"
			:playback-active="isPlaybackActive" class="playback-area" @playback="startPlayback"
			@playback-pause="handlePlaybackPause" @export-video="exportVideo" @snapshot="captureSnapshot"
			@live="backToLive" />
		<el-dialog v-model="infoDialogVisible" width="460px" :append-to-body="false" destroy-on-close
			class="cctv-focus-info-dialog" modal-class="cctv-focus-info-modal"
			:z-index="CameraControlLayer.DIALOG" :modal="true" :close-on-press-escape="true"
			:close-on-click-modal="true">
			<template #header>
				<div style="color: #fff">
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle">
						<ele-InfoFilled />
					</el-icon>
					<span> {{ t('lz.label.cctvDashboard.cameraInfo') }} </span>
				</div>
			</template>
			<div v-if="cameraInfoView" class="camera-info-dialog-body">
				<div class="camera-info-row">
					<span>{{ t('lz.entity.base.name') }}</span>
					<strong>{{ cameraInfoView.name }}</strong>
				</div>
				<div class="camera-info-row">
					<span>IP</span>
					<strong>{{ cameraInfoView.ip }}</strong>
				</div>
				<div class="camera-info-row">
					<span>DeviceId</span>
					<strong>{{ cameraInfoView.deviceId }}</strong>
				</div>
				<div class="camera-info-row">
					<span>{{ t('lz.label.base.location') }}</span>
					<strong>{{ cameraInfoView.location }}</strong>
				</div>
				<div class="camera-info-row">
					<span>{{ t('lz.entity.base.status') }}</span>
					<strong>{{ cameraInfoView.status }}</strong>
				</div>
			</div>
			<template #footer>
				<el-button type="primary" @click="infoDialogVisible = false">{{ t('lz.button.base.close') }}</el-button>
			</template>
		</el-dialog>
	</section>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { ElMessage } from 'element-plus';
import { saveAs } from 'file-saver';
import CameraTile from './CameraTile.vue';
import PlaybackBar from './PlaybackBar.vue';
import PtzControlPanel from './PtzControlPanel.vue';
import type { VideoMonitoringItem } from '/@/views/cctv/dashboard/types';
import {
	getCctvMediaDownloadUrl,
	getCctvPlaybackStreamUrl,
	getCctvSnapshotUrl,
	getCctvWebRtcStreamUrl,
} from '../services/cctvDashboardService';
import '../styles/cameraControlTokens.scss';
import { CameraControlLayer } from '../constants/cameraControlLayers';
import { getCctvDefaultStream, getCctvStreamOptions } from '../services/cctvDashboardConfig';
import { getSelectableCameras, isCameraSelected, shouldEmitCameraReplace, buildCameraInfoView, canUseCameraPlayback, getLiveControlDisabledReason } from '../utils/cameraControlUtils';
import { useCctvDashboardStore } from '/@/stores/cctvDashboard.js'
import { useI18n } from 'vue-i18n';


const cctvDashboardStore = useCctvDashboardStore()
const { t } = useI18n();
const props = defineProps<{
	camera: VideoMonitoringItem;
	cameras?: VideoMonitoringItem[];
	focusMode?: 'view' | 'ptz' | 'playback';
}>();

const emit = defineEmits<{
	(e: 'back'): void;
	(e: 'focus-mode', mode: 'view' | 'ptz' | 'playback'): void;
	(e: 'select-camera', id: string): void;
	(e: 'screenshot', id: string): void;
	(e: 'info', id: string): void;
}>();

const playbackUrl = ref('');
const playbackPaused = ref(false);
const playbackSessionId = ref(0);
const playbackSpeed = ref(1);
const playbackLoading = ref(false);
const exportLoading = ref(false);
const snapshotLoading = ref(false);
const focusStreamUrl = ref('');
const focusStreamLoading = ref(false);
const selectedStream = ref(getCctvDefaultStream());
const streamOptions = computed(() => getCctvStreamOptions());
const currentStreamLabel = computed(() => {
	const opt = streamOptions.value.find((o) => o.code === selectedStream.value);
	return opt?.name || selectedStream.value;
});
const ptzOpen = ref(false);
const playbackOpen = ref(false);
const infoDialogVisible = ref(false);
const playbackBarRef = ref<InstanceType<typeof PlaybackBar> & {
	confirmPlaybackStarted?: () => void;
	syncPlaybackProgress?: (sample: { mediaTimeSec: number; rtpTimestamp?: number; captureTimeMs?: number; final?: boolean; streamEpoch?: number }) => void;
}>();
let focusStreamRequestVersion = 0;
let playbackRequestVersion = 0;
// Yêu cầu playback gần nhất — dùng để xin lại URL khi ticket proxy của URL đang giữ đã hết hạn.
let lastPlaybackPayload: { deviceId: string; fromDate: Date; requestedDate?: Date; snapped?: boolean; speed?: number } | null = null;

const isPlaybackContext = computed(() => playbackLoading.value || Boolean(playbackUrl.value));
const activeVideoUrlOverride = computed(() => (isPlaybackContext.value ? playbackUrl.value : focusStreamUrl.value));
const videoLoading = computed(() => playbackLoading.value || focusStreamLoading.value);
const actionLoading = computed(() => playbackLoading.value || exportLoading.value || snapshotLoading.value);
const disableTileFallbackResolve = computed(() => isPlaybackContext.value || focusStreamLoading.value || Boolean(focusStreamUrl.value));
const focusMode = computed(() => props.focusMode || 'view');
const isPlaybackActive = computed(() => Boolean(playbackUrl.value));
// Khả năng điều khiển theo trạng thái thiết bị:
// - PTZ/Snapshot (live): cần online + Device ID + URL luồng.
// - Playback: chỉ cần Device ID (footage đã ghi vẫn xem được khi offline).
const liveControlReason = computed(() => getLiveControlDisabledReason(props.camera));
const canControlLive = computed(() => liveControlReason.value === '');
const canPlayback = computed(() => canUseCameraPlayback(props.camera));
const liveControlDisabledTooltip = computed(() => {
	switch (liveControlReason.value) {
		case 'no-device-id':
			return t('lz.message.cctvDashboard.controlUnavailableNoDeviceId');
		case 'offline':
			return t('lz.message.cctvDashboard.controlUnavailableOffline');
		case 'no-stream':
			return t('lz.message.cctvDashboard.controlUnavailableNoStream');
		default:
			return '';
	}
});
const playbackDisabledTooltip = computed(() => (canPlayback.value ? '' : t('lz.message.cctvDashboard.controlUnavailableNoDeviceId')));
// Danh sách camera khả dụng (loại trừ placeholder, giữ thứ tự) cho Camera_Selector (R5.5, R5.6)
const selectableCameras = computed(() => getSelectableCameras(props.cameras));
const currentCameraId = computed(() => toSafeString(props.camera.id));
// Dữ liệu hiển thị đã chuẩn hóa cho Info_Dialog (fallback "--", nhãn trạng thái) (R6.2, R6.3)
const cameraInfoView = computed(() => {
	const view = buildCameraInfoView(props.camera);
	if (!view) return null;
	return {
		...view,
		status: props.camera.status === 'online' ? t('lz.label.cctvDashboard.online') : t('lz.label.cctvDashboard.offline'),
	};
});
const toSafeString = (value: unknown, fallback = '') => {
	try {
		if (value === undefined || value === null) return fallback;
		if (typeof value === 'string') return value;
		if (typeof value === 'number' || typeof value === 'boolean') return String(value);
		return fallback;
	} catch {
		return fallback;
	}
};

const getErrorMessage = (error: unknown, fallback = t('lz.message.cctvDashboard.genericError')) => {
	if (error instanceof Error) return error.message || fallback;
	const message = toSafeString((error as { message?: unknown })?.message);
	return message || toSafeString(error, fallback);
};

const showFullscreenMessage = (type: 'success' | 'warning' | 'error', message: string) => {
	ElMessage({
		type,
		message,
		// Tự ẩn trong khoảng 3–5 giây theo R3.4 (4000ms ∈ [3000, 5000]).
		duration: 4000,
		// z-index lớp MESSAGE (10100) áp qua CSS của customClass này (R3.4, R5.8).
		customClass: 'cctv-fullscreen-message',
	});
};

const appendPlaybackSpeed = (url: string, speed?: number) => {
	if (!speed || speed <= 1) return url;
	try {
		const parsedUrl = new URL(url);
		parsedUrl.searchParams.set('speed', String(speed));
		return parsedUrl.toString();
	} catch {
		const separator = url.includes('?') ? '&' : '?';
		return `${url}${separator}speed=${encodeURIComponent(String(speed))}`;
	}
};

const onFocusMainClick = () => {
	if (focusMode.value !== 'view') return;
	emit('back');
};

const toggleMode = (mode: 'ptz' | 'playback') => {
	// Chặn khi thiết bị không đủ điều kiện (defense, ngoài việc nút đã disabled)
	if (mode === 'ptz' && !canControlLive.value) return;
	if (mode === 'playback' && !canPlayback.value) return;
	const closingActiveMode = focusMode.value === mode;
	if (mode !== 'playback' || closingActiveMode) playbackUrl.value = '';
	if (closingActiveMode) {
		ptzOpen.value = false;
		playbackOpen.value = false;
	} else {
		ptzOpen.value = mode === 'ptz';
		playbackOpen.value = mode === 'playback';
	}
	emit('focus-mode', closingActiveMode ? 'view' : mode);
};

const applyFocusModePanels = (value: 'view' | 'ptz' | 'playback') => {
	if (value === 'view') {
		ptzOpen.value = false;
		playbackOpen.value = false;
		return;
	}
	if (value === 'ptz') {
		ptzOpen.value = true;
		playbackOpen.value = false;
		return;
	}
	ptzOpen.value = false;
	playbackOpen.value = true;
};

const togglePtzPanel = () => {
	if (!canControlLive.value) return;
	ptzOpen.value = !ptzOpen.value;
	if (ptzOpen.value && focusMode.value === 'view') emit('focus-mode', 'ptz');
};

const togglePlaybackPanel = () => {
	if (!canPlayback.value) return;
	playbackOpen.value = !playbackOpen.value;
	if (playbackOpen.value && focusMode.value === 'view') emit('focus-mode', 'playback');
};

// Theo dõi yêu cầu thay thế camera đang chờ để phát hiện thất bại (R5.8).
// Khi cha cập nhật prop `camera` sang camera đã chọn => thành công; nếu sau
// REPLACE_TIMEOUT_MS prop vẫn không đổi => coi là thất bại và báo lỗi.
const REPLACE_TIMEOUT_MS = 4000;
const pendingReplaceId = ref('');
let replaceTimer: ReturnType<typeof setTimeout> | null = null;

const clearReplaceTracking = () => {
	if (replaceTimer) {
		clearTimeout(replaceTimer);
		replaceTimer = null;
	}
	pendingReplaceId.value = '';
};

const handleSelectCameraCommand = (id: string | number | object) => {
	const cameraId = toSafeString(id);
	// el-dropdown tự đóng danh sách sau khi phát command (hide-on-click mặc định).
	if (!cameraId) return;
	// Chọn lại đúng camera đang xem => chỉ đóng, không phát yêu cầu thay thế (R5.9).
	if (!shouldEmitCameraReplace(cameraId, currentCameraId.value)) return;
	// Chọn camera khác => phát yêu cầu thay thế và theo dõi kết quả (R5.7, R5.8).
	pendingReplaceId.value = cameraId;
	if (replaceTimer) clearTimeout(replaceTimer);
	replaceTimer = setTimeout(() => {
		if (pendingReplaceId.value === cameraId && currentCameraId.value !== cameraId) {
			showFullscreenMessage('error', t('lz.message.cctvDashboard.switchCameraFailed'));
		}
		clearReplaceTracking();
	}, REPLACE_TIMEOUT_MS);
	emit('select-camera', cameraId);
};

// Mở Info_Dialog của camera đang xem (R6.1, R6.6, R6.7).
// - Chỉ mở khi `buildCameraInfoView` trả khác `null` (có dữ liệu cốt lõi).
// - Nếu `null` (không có camera/dữ liệu) => không mở + cảnh báo (R6.6).
// - Cờ `infoDialogVisible` đơn bảo đảm tối đa một dialog tại một thời điểm (R6.7).
const openInfoDialog = () => {
	if (!cameraInfoView.value) {
		showFullscreenMessage('warning', t('lz.message.cctvDashboard.cameraInfoUnavailable'));
		return;
	}
	infoDialogVisible.value = true;
};

const captureCurrentSnapshot = () => {
	if (!canControlLive.value) return;
	captureSnapshot({
		deviceId: toSafeString(props.camera.deviceId),
		fromDate: new Date(),
	});
};

// Tua càng nhanh, server gửi càng nhiều khung hình/giây -> 1080p giải mã không kịp, hình (và đồng hồ
// bám theo hình) bị tụt lại. Hạ độ phân giải / dùng luồng phụ ở tốc độ cao để giải mã kịp, hình theo
// sát thời gian thực hơn. (Đổi/chỉnh ngưỡng tại đây nếu cần.)
const getPlaybackProfile = (speed: number): { stream?: string; resolution?: string } | undefined => {
	if (speed >= 16) return { stream: 'secondary', resolution: '640x360' };
	if (speed >= 8) return { stream: 'secondary', resolution: '1280x720' };
	if (speed >= 4) return { stream: 'primary', resolution: '1280x720' };
	return undefined; // x1, x2: giữ chất lượng đầy đủ (primary 1920x1080)
};

const startPlayback = async (payload: { deviceId: string; fromDate: Date; requestedDate?: Date; snapped?: boolean; speed?: number }) => {
	lastPlaybackPayload = payload;
	const requestVersion = ++playbackRequestVersion;
	const sessionId = ++playbackSessionId.value;
	playbackLoading.value = true;
	try {
		playbackPaused.value = false;
		const speed = payload.speed || 1;
		playbackSpeed.value = speed;
		playbackUrl.value = '';
		const url = await getCctvPlaybackStreamUrl(
			{
				...props.camera,
				deviceId: payload.deviceId,
			},
			payload.fromDate,
			getPlaybackProfile(speed)
		);
		if (requestVersion === playbackRequestVersion && sessionId === playbackSessionId.value) playbackUrl.value = appendPlaybackSpeed(url, payload.speed);
	} catch (error) {
		if (requestVersion === playbackRequestVersion) showFullscreenMessage('error', getErrorMessage(error, t('lz.message.cctvDashboard.openPlaybackFailed')));
	} finally {
		if (requestVersion === playbackRequestVersion) playbackLoading.value = false;
	}
};

const handlePlaybackPause = (payload: { paused: boolean; atDate?: Date }) => {
	playbackPaused.value = payload.paused;
};

const handleFocusMediaReady = () => {
	if (!isPlaybackActive.value || playbackPaused.value) return;
	playbackBarRef.value?.confirmPlaybackStarted?.();
};

const handlePlaybackProgress = (payload: { id: string; mediaTimeSec: number; rtpTimestamp?: number; captureTimeMs?: number; final?: boolean; streamEpoch?: number; sessionId?: number }) => {
	if (!isPlaybackActive.value) return;
	if (payload.sessionId !== undefined && payload.sessionId !== playbackSessionId.value) return;
	playbackBarRef.value?.syncPlaybackProgress?.({
		mediaTimeSec: payload.mediaTimeSec,
		rtpTimestamp: payload.rtpTimestamp,
		captureTimeMs: payload.captureTimeMs,
		final: payload.final,
		streamEpoch: payload.streamEpoch,
	});
};

const backToLive = () => {
	playbackSessionId.value++;
	playbackUrl.value = '';
	playbackPaused.value = false;
	playbackSpeed.value = 1;
	ptzOpen.value = false;
	playbackOpen.value = false;
	emit('focus-mode', 'view');
};

const resetPlaybackState = () => {
	playbackSessionId.value++;
	playbackUrl.value = '';
	playbackPaused.value = false;
	playbackSpeed.value = 1;
};

const openExternalUrl = (url: string, fileName?: string) => {
	const anchor = document.createElement('a');
	anchor.href = url;
	anchor.rel = 'noopener noreferrer';
	if (fileName) {
		anchor.download = fileName;
	} else {
		anchor.target = '_blank';
	}
	document.body.appendChild(anchor);
	anchor.click();
	anchor.remove();
};

const exportVideo = async (payload: { deviceId: string; fromDate: Date; toDate: Date }) => {
	if (exportLoading.value) return;
	exportLoading.value = true;
	try {
		const url = await getCctvMediaDownloadUrl(
			{
				...props.camera,
				deviceId: payload.deviceId,
			},
			{
				fromDate: payload.fromDate,
				toDate: payload.toDate,
				stream: 'primary',
				resolution: '1920x1080',
			}
		);
		openExternalUrl(url, `camera-${payload.deviceId}-archive.mp4`);
		showFullscreenMessage('success', t('lz.message.cctvDashboard.videoUrlCreated'));
	} catch (error) {
		showFullscreenMessage('error', getErrorMessage(error, t('lz.message.cctvDashboard.videoUrlCreateFailed')));
	} finally {
		exportLoading.value = false;
	}
};

const captureSnapshot = async (payload: { deviceId: string; fromDate: Date }) => {
	if (snapshotLoading.value) return;
	snapshotLoading.value = true;
	try {
		const url = await getCctvSnapshotUrl(
			{
				...props.camera,
				deviceId: payload.deviceId,
			},
			{
				fromDate: payload.fromDate,
				size: '1920x1080',
				format: 'jpg',
			}
		);
		const equipment = cctvDashboardStore.equipments.findLast(item => {
			item.id == payload.deviceId
		})
		console.log(equipment)
		saveAs(url, `snapshot-${new Date().toLocaleString('en-GB')}-${equipment?.name}-${equipment?.ip}.jpg`);
		showFullscreenMessage('success', t('lz.message.cctvDashboard.snapshotSuccess'));
	} catch (error) {
		console.error('[CCTV Snapshot] Không thể chụp snapshot', error);
		showFullscreenMessage('error', getErrorMessage(error, t('lz.message.cctvDashboard.snapshotFailed')));
	} finally {
		snapshotLoading.value = false;
	}
};

// CameraTile báo URL override có ticket Cctv Proxy đã hết hạn -> xin URL mới cho đúng ngữ cảnh đang xem.
const handleStreamExpired = () => {
	if (isPlaybackContext.value) {
		if (lastPlaybackPayload) startPlayback(lastPlaybackPayload);
		return;
	}
	loadFocusStream();
};

const loadFocusStream = async () => {
	const requestVersion = ++focusStreamRequestVersion;
	focusStreamUrl.value = '';
	if (!toSafeString(props.camera?.deviceId)) return;
	focusStreamLoading.value = true;
	try {
		const url = await getCctvWebRtcStreamUrl(props.camera, {
			stream: selectedStream.value,
		});
		if (requestVersion === focusStreamRequestVersion) focusStreamUrl.value = url;
	} catch {
		if (requestVersion === focusStreamRequestVersion) focusStreamUrl.value = '';
	} finally {
		if (requestVersion === focusStreamRequestVersion) focusStreamLoading.value = false;
	}
};

const handleStreamChange = (stream: string) => {
	if (stream === selectedStream.value) return;
	selectedStream.value = stream;
	loadFocusStream();
};

watch(
	() => [props.camera.id, props.camera.deviceId, props.camera.ip].map((value) => toSafeString(value)).join(':'),
	() => {
		// Prop `camera` thay đổi => yêu cầu thay thế (nếu có) đã được cha áp dụng thành công (R5.8).
		if (pendingReplaceId.value) clearReplaceTracking();
		resetPlaybackState();
		applyFocusModePanels(focusMode.value);
		selectedStream.value = getCctvDefaultStream();
		loadFocusStream();
	},
	{ immediate: true }
);

watch(
	() => focusMode.value,
	(value) => {
		applyFocusModePanels(value);
	},
	{ immediate: true, flush: 'post' }
);
</script>

<style scoped lang="scss">
.fullscreen-focus-layout {
	width: 100vw;
	height: 100vh;
	display: grid;
	grid-template-columns: minmax(0, 1fr);
	grid-template-rows: minmax(0, 1fr);
	background: #000000;
	overflow: hidden;
}

.fullscreen-focus-layout.with-ptz {
	grid-template-columns: minmax(0, 10fr) minmax(280px, 2fr);
	grid-template-rows: minmax(0, 1fr);
}

.fullscreen-focus-layout.with-playback {
	grid-template-columns: minmax(0, 1fr);
	grid-template-rows: minmax(0, 1fr) auto;
}

.fullscreen-focus-layout.with-ptz.with-playback {
	grid-template-columns: minmax(0, 10fr) minmax(280px, 2fr);
	grid-template-rows: minmax(0, 1fr) auto;
}

.focus-main {
	position: relative;
	min-width: 0;
	min-height: 0;
}

.fullscreen-back {
	position: absolute;
	top: 16px;
	left: 16px;
	z-index: 3;
	opacity: 0.3;
}

.focus-actions {
	position: absolute;
	top: 16px;
	right: 16px;
	z-index: 5;
	display: flex;
	align-items: center;
	gap: 8px;
	flex-wrap: wrap;

	:deep(.el-button) {
		margin-left: 0;
		background: rgba(15, 23, 42, 0.78);
		border-color: rgba(255, 255, 255, 0.2);
		color: #ffffff;
	}

	// Camera_Selector dùng token nút chỉ-icon chuẩn hóa (R5.2, R7.1)
	.focus-icon-button {
		width: var(--cctv-btn-icon-size);
		height: var(--cctv-btn-icon-size);
		min-width: var(--cctv-btn-icon-size);
		padding: 0;
		border-radius: var(--cctv-btn-radius);
	}
}

.fullscreen-back:hover {
	opacity: 1;
}

.panel-toggle {
	position: absolute;
	z-index: 6;
	width: 36px;
	height: 36px;
	padding: 0;
	display: inline-flex;
	align-items: center;
	justify-content: center;
	border-radius: 50%;
	background: rgba(255, 255, 255, 0.92);
	border-color: rgba(15, 23, 42, 0.18);
	color: #0f172a;
	box-shadow: 0 10px 24px rgba(15, 23, 42, 0.22);
	transition:
		background-color 0.18s ease,
		border-color 0.18s ease,
		box-shadow 0.18s ease,
		color 0.18s ease,
		transform 0.18s ease;

	:deep(.el-icon) {
		font-size: 16px;
	}

	&:hover,
	&:focus-visible {
		background: #ffffff;
		border-color: rgba(59, 130, 246, 0.55);
		color: #2563eb;
		box-shadow: 0 14px 30px rgba(15, 23, 42, 0.32);
	}

	&:active {
		background: #eff6ff;
		box-shadow: 0 8px 18px rgba(15, 23, 42, 0.24);
	}

	&.is-ptz {
		top: 50%;
		right: -18px;
		transform: translateY(-50%);

		:deep(.el-icon) {
			margin-right: 5px;
		}
	}

	&.is-ptz:hover,
	&.is-ptz:focus-visible {
		transform: translate(-4px, -50%);
	}

	&.is-ptz.is-open {
		right: -18px;

		:deep(.el-icon) {
			margin-right: 0px;
		}
	}

	&.is-ptz.is-open:hover,
	&.is-ptz.is-open:focus-visible {
		transform: translate(4px, -50%);
	}

	&.is-playback {
		left: 50%;
		bottom: -18px;
		transform: translateX(-50%);

		:deep(.el-icon) {
			margin-bottom: 5px;
			margin-right: 0;
		}
	}

	&.is-playback:hover,
	&.is-playback:focus-visible {
		transform: translate(-50%, -4px);
	}

	&.is-playback.is-open {
		bottom: -18px;

		:deep(.el-icon) {
			margin-bottom: 0px;
			margin-right: 0;
		}
	}

	&.is-playback.is-open:hover,
	&.is-playback.is-open:focus-visible {
		transform: translate(-50%, 4px);
	}
}

.fullscreen-focus-tile {
	width: 100%;
	height: 100%;
	border-radius: 0;
}

.playback-area {
	grid-column: 1 / -1;
}

.ptz-area {
	min-height: 0;
}

:global(.el-message),
:global(.cctv-fullscreen-message) {
	z-index: 10100 !important; // = CameraControlLayer.MESSAGE (R5.8)
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
	border-bottom: 1px solid var(--el-border-color-lighter, #eef2f7);

	span {
		color: var(--el-text-color-secondary, #909399);
		font-size: 13px;
	}

	strong {
		min-width: 0;
		color: var(--el-text-color-primary, #303133);
		font-size: 13px;
		font-weight: 600;
		overflow-wrap: anywhere;
	}
}

.camera-info-row:last-child {
	border-bottom: 0;
}

:global(.cctv-focus-camera-popper) {
	z-index: 10080 !important; // = CameraControlLayer.DROPDOWN (R5.4)
}

:global(.focus-camera-menu) {
	max-height: 320px;
	overflow-y: auto;
}

:global(.focus-camera-empty) {
	color: var(--el-text-color-placeholder, #98a2b3);
	font-size: 13px;
	line-height: 18px;
	min-width: 220px;
	text-align: center;
	padding: 4px 0;
}

:global(.focus-camera-menu .el-dropdown-menu__item.is-active) {
	background: var(--el-color-primary-light-9);
	color: var(--el-color-primary);
}

:global(.focus-camera-option) {
	display: grid;
	gap: 2px;
	min-width: 220px;
}

:global(.focus-camera-option strong) {
	font-size: 13px;
	line-height: 18px;
}

:global(.focus-camera-option span) {
	color: var(--el-text-color-secondary, #909399);
	font-size: 12px;
	line-height: 16px;
}

:global(.cctv-focus-info-modal) {
	z-index: 10090 !important; // = CameraControlLayer.DIALOG (R6.4)
}

:global(.cctv-focus-info-dialog) {
	z-index: 10090 !important; // = CameraControlLayer.DIALOG (R6.4)
}

@media (max-width: 900px) {

	.fullscreen-focus-layout.with-ptz,
	.fullscreen-focus-layout.with-playback {
		grid-template-columns: 1fr;
		grid-template-rows: minmax(0, 1fr) auto auto;
	}
}
</style>
