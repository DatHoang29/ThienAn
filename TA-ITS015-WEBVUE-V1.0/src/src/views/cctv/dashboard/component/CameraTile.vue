<template>
	<article class="camera-tile"
		:class="[safeCamera.status, { editable: editMode, 'camera-only': cameraOnly, selected }]" @click="onTileClick">
		<div v-if="camera.isPlaceholder" class="placeholder-tile">
			<div class="placeholder-icon" @click.stop="$emit('add-slot', safeCamera.id)">
				<el-icon><ele-VideoCamera /></el-icon>
			</div>
			<div class="placeholder-actions">
				<el-tooltip :content="t('lz.button.cctvDashboard.addCamera')" placement="top" :show-after="300" :hide-after="100"
					:teleported="false" :z-index="CameraControlLayer.MESSAGE">
					<span class="cctv-tooltip-wrapper">
						<el-button class="camera-control-btn camera-control-btn--icon"
							@click.stop="$emit('add-slot', safeCamera.id)">
							<el-icon><ele-Plus /></el-icon>
						</el-button>
					</span>
				</el-tooltip>
				<el-tooltip v-if="editMode" :content="t('lz.button.cctvDashboard.removePlaceholder')" placement="top" :show-after="300" :hide-after="100"
					:teleported="false" :z-index="CameraControlLayer.MESSAGE">
					<span class="cctv-tooltip-wrapper">
						<el-button class="camera-control-btn camera-control-btn--icon is-danger"
							@click.stop="$emit('remove', safeCamera.id)">
							<el-icon><ele-Delete /></el-icon>
						</el-button>
					</span>
				</el-tooltip>
			</div>
		</div>
		<div v-else class="feed">
			<video v-if="shouldRenderVideo" ref="videoRef" :class="mediaClass" :src="nativeVideoSource" loop autoplay
				muted playsinline preload="metadata" @click="onMediaClick"></video>
			<img v-else :class="mediaClass" :src="cameraNotAvailableImageSrc" :alt="t('lz.label.cctvDashboard.noVideo')" @click="onMediaClick" />
			<div v-if="showVideoLoading" class="camera-loading-overlay">
				<span class="camera-loading-spinner"></span>
				<span>{{ t('lz.label.cctvDashboard.loadingVideo') }}</span>
			</div>
			<div v-if="!cameraOnly" class="tile-caption" :title="safeCamera.name">
				<span>{{ safeCamera.name }}</span>
				<small v-if="showLiveBadge" class="status is-live">
					<i></i>
					{{ t('lz.label.cctvDashboard.live') }}
				</small>
			</div>
			<div v-if="cameraOnly && showSettings" class="camera-only-actions">
				<el-tooltip :content="canControlLive ? t('lz.label.cctvDashboard.ptz') : liveControlDisabledTooltip" placement="left" :show-after="300" :hide-after="100" :teleported="false"
					:z-index="CameraControlLayer.MESSAGE">
					<span class="cctv-tooltip-wrapper">
						<el-button class="camera-control-btn camera-control-btn--icon" :disabled="!canControlLive"
							@click.stop="$emit('camera-action', 'ptz', safeCamera.id)">
							<span>PTZ</span>
						</el-button>
					</span>
				</el-tooltip>
				<el-tooltip :content="canPlayback ? t('lz.label.cctvDashboard.playback') : t('lz.message.cctvDashboard.controlUnavailableNoDeviceId')" placement="left" :show-after="300" :hide-after="100" :teleported="false"
					:z-index="CameraControlLayer.MESSAGE">
					<span class="cctv-tooltip-wrapper">
						<el-button class="camera-control-btn camera-control-btn--icon" :disabled="!canPlayback"
							@click.stop="$emit('camera-action', 'playback', safeCamera.id)">
							<el-icon><ele-VideoPlay /></el-icon>
						</el-button>
					</span>
				</el-tooltip>
				<el-tooltip :content="canControlLive ? t('lz.label.cctvDashboard.screenshot') : liveControlDisabledTooltip" placement="left" :show-after="300" :hide-after="100"
					:teleported="false" :z-index="CameraControlLayer.MESSAGE">
					<span class="cctv-tooltip-wrapper">
						<el-button class="camera-control-btn camera-control-btn--icon" :disabled="!canControlLive"
							@click.stop="$emit('camera-action', 'screenshot', safeCamera.id)">
							<el-icon><ele-Camera /></el-icon>
						</el-button>
					</span>
				</el-tooltip>
				<el-tooltip :content="t('lz.label.cctvDashboard.info')" placement="left" :show-after="300" :hide-after="100" :teleported="false"
					:z-index="CameraControlLayer.MESSAGE">
					<span class="cctv-tooltip-wrapper">
						<el-button class="camera-control-btn camera-control-btn--icon"
							@click.stop="$emit('camera-action', 'info', safeCamera.id)">
							<el-icon><ele-InfoFilled /></el-icon>
						</el-button>
					</span>
				</el-tooltip>
			</div>
			<div v-if="cameraOnly" class="camera-only-label">
				<span>{{ safeCamera.name }}</span>
				<small v-if="showLiveBadge" class="status is-live">
					<i></i>
					{{ t('lz.label.cctvDashboard.live') }}
				</small>
				<small v-if="playbackActive" class="playback">{{ t('lz.label.cctvDashboard.playback') }}</small>
			</div>
			<div v-if="!cameraOnly" class="overlay">
				<!-- <h4>{{ camera.name }}</h4> -->

				<div class="tile-actions">

					<el-tooltip :content="t('lz.button.base.detail')" placement="top" :show-after="300" :hide-after="100"
						:teleported="false" :z-index="CameraControlLayer.MESSAGE">
						<span class="cctv-tooltip-wrapper">
							<el-button class="camera-control-btn camera-control-btn--icon"
								@click.stop="$emit('detail', safeCamera.id)"><el-icon><ele-Operation /></el-icon></el-button>
						</span>
					</el-tooltip>
					<el-tooltip v-if="editMode" :content="t('lz.button.base.delete')" placement="top" :show-after="300" :hide-after="100"
						:teleported="false" :z-index="CameraControlLayer.MESSAGE">
						<span class="cctv-tooltip-wrapper">
							<el-button class="camera-control-btn camera-control-btn--icon is-danger"
								@click.stop="$emit('remove', safeCamera.id)"><el-icon><ele-Delete /></el-icon></el-button>
						</span>
					</el-tooltip>
				</div>
			</div>
		</div>
	</article>
</template>

<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, ref, watch } from 'vue';
import { type VideoMonitoringItem } from '/@/views/cctv/dashboard/types';
import cameraNotAvailableImage from '/@/assets/img/camera_unavailable.jpg';
import { getCctvWebRtcStreamUrl, isCctvStreamUrlExpired } from '../services/cctvDashboardService';
import { CameraControlLayer } from '../constants/cameraControlLayers';
import { canUseCameraPlayback, getLiveControlDisabledReason } from '../utils/cameraControlUtils';
import { i18n } from '/@/i18n';

const t = i18n.global.t;
const videoRef = ref<HTMLVideoElement>();
let peerConnection: RTCPeerConnection | null = null;
let websocket: WebSocket | null = null;
let progressFrameHandle = 0;
let progressIntervalTimer: ReturnType<typeof window.setInterval> | null = null;
let progressReporting = false;
let lastProgressEmitMs = 0;
// Metadata của khung hình mới nhất (cập nhật mỗi frame, không throttle) -> dùng để gửi mẫu 'final'
// đúng khung hình đang dừng khi pause, giúp đồng hồ chốt chính xác.
let latestFrameMeta: { mediaTimeSec: number; rtpTimestamp?: number; captureTimeMs?: number } | null = null;
// Tăng mỗi khi một stream MỚI thực sự gắn vào (kết nối lại / đổi tốc độ). Giúp PlaybackBar phân biệt
// "cùng stream nhảy lớn" (tab quay lại -> cộng dồn để đuổi kịp) với "đổi stream" (neo lại từ đầu).
let playbackStreamEpoch = 0;
const pendingIceCandidates: RTCIceCandidateInit[] = [];
let webRtcStartVersion = 0;
let firstFrameTimeout: ReturnType<typeof window.setTimeout> | null = null;
let hasReceivedRemoteStream = false;
let streamResolveVersion = 0;
// Đã dùng lượt thử lại (lấy URL/ticket mới) cho lần kết nối hiện tại chưa — tránh lặp vô hạn khi proxy lỗi.
let streamRetryConsumed = false;
// Đã báo cha xin URL override mới (ticket hết hạn) chưa — reset khi có hình, tránh vòng lặp báo liên tục.
let overrideRefreshRequested = false;
const isPlaybackLoading = ref(false);
const mediaReady = ref(false);
// URL stream đi qua Cctv Proxy kèm ticket có hạn -> chỉ cache ngắn hơn hạn ticket.
const streamUrlCacheMaxAgeMs = 30 * 60 * 1000;
const streamUrlCache = new Map<string, { url: string; cachedAt: number }>();

const props = defineProps<{
	camera: VideoMonitoringItem;
	editMode?: boolean;
	cameraOnly?: boolean;
	viewMode?: 'grid' | 'focus';
	showSettings?: boolean;
	showControls?: boolean;
	backOnClick?: boolean;
	selected?: boolean;
	videoUrlOverride?: string;
	streamResolution?: string;
	streamProfile?: 'primary' | 'secondary';
	preferApiStream?: boolean;
	autoPlay?: boolean;
	loading?: boolean;
	disableFallbackResolve?: boolean;
	playbackActive?: boolean;
	playbackPaused?: boolean;
	playbackSessionId?: number;
	playbackSpeed?: number;
}>();

const toSafeString = (value: unknown, fallback = '') => {
	try {
		if (value === undefined || value === null) return fallback;
		if (typeof value === 'string') return value;
		if (typeof value === 'number' || typeof value === 'boolean') return String(value);
		if (value instanceof Date) return value.toISOString();
		if (typeof value === 'object') {
			const source = value as Record<string, unknown>;
			const primitiveValue = source.id ?? source.ID ?? source.Id ?? source.value ?? source.Value ?? source.code ?? source.Code ?? source.name ?? source.Name ?? source.url ?? source.Url;
			if (primitiveValue !== value) return toSafeString(primitiveValue, fallback);
		}
		return fallback;
	} catch {
		return fallback;
	}
};

const cameraNotAvailableImageSrc =
	typeof cameraNotAvailableImage === 'string'
		? cameraNotAvailableImage
		: toSafeString((cameraNotAvailableImage as unknown as { default?: unknown })?.default);

const safeCamera = computed(() => {
	const status = toSafeString(props.camera?.status, 'offline');
	return {
		...props.camera,
		id: toSafeString(props.camera?.id),
		deviceId: toSafeString(props.camera?.deviceId),
		ip: toSafeString(props.camera?.ip),
		name: toSafeString(props.camera?.name, 'Camera'),
		location: toSafeString(props.camera?.location),
		status: ['online', 'warning', 'offline'].includes(status) ? status : 'offline',
		aspect: toSafeString(props.camera?.aspect, '16:9'),
		videoUrl: toSafeString(props.camera?.videoUrl),
	};
});

// Khả năng điều khiển theo trạng thái thiết bị (PTZ/Snapshot cần online + Device ID + URL;
// Playback chỉ cần Device ID). Dùng để vô hiệu hóa nút tương ứng trên tile camera.
const liveControlReason = computed(() => getLiveControlDisabledReason(safeCamera.value));
const canControlLive = computed(() => liveControlReason.value === '');
const canPlayback = computed(() => canUseCameraPlayback(safeCamera.value));
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

const emit = defineEmits<{
	(e: 'detail', id: string): void;
	(e: 'remove', id: string): void;
	(e: 'add-slot', id: string): void;
	(e: 'select', id: string): void;
	(e: 'settings', id: string): void;
	(e: 'camera-action', action: 'ptz' | 'playback' | 'screenshot' | 'info', id: string): void;
	(e: 'back-to-grid'): void;
	(e: 'media-ready', id: string): void;
	/** URL override (do cha truyền) có ticket proxy đã hết hạn -> cha cần lấy URL mới. */
	(e: 'stream-expired', id: string): void;
	(e: 'stream-error', id: string): void;
	(e: 'playback-progress', payload: { id: string; mediaTimeSec: number; rtpTimestamp?: number; captureTimeMs?: number; final?: boolean; streamEpoch?: number; sessionId?: number }): void;
}>();

const resolvedVideoUrl = ref('');
const cameraVideoUrl = computed(() => resolvedVideoUrl.value);
const safeVideoUrlOverride = computed(() => toSafeString(props.videoUrlOverride).trim());
// Camera chỉ phát được khi có URL override tường minh hoặc có deviceId (để gọi stream NX).
// Thiết bị không có deviceId nghĩa là không được hỗ trợ stream -> hiển thị thumbnail "unavailable",
// không gọi API media để tránh lỗi notFoundByDeviceId.
const hasStreamSource = computed(() => Boolean(safeVideoUrlOverride.value || safeCamera.value.deviceId));
const shouldPlayVideo = computed(() => hasStreamSource.value && Boolean(props.autoPlay || safeVideoUrlOverride.value));
const shouldRenderVideo = computed(() => shouldPlayVideo.value && Boolean(cameraVideoUrl.value));
const isWebSocketUrl = computed(() => /^wss?:\/\//i.test(cameraVideoUrl.value));
const nativeVideoSource = computed(() => (shouldPlayVideo.value && cameraVideoUrl.value && !isWebSocketUrl.value ? cameraVideoUrl.value : ''));
const showVideoLoading = computed(() => Boolean(props.loading || isPlaybackLoading.value));
const mediaClass = computed(() => (props.viewMode === 'focus' && props.showControls ? 'camera-video-focus' : 'camera-video'));
const playbackActive = computed(() => Boolean(props.playbackActive));
const streamResolution = computed(() => props.streamResolution || (props.viewMode === 'focus' ? '1920x1080' : '640x360'));
const streamProfile = computed(() => props.streamProfile || (props.viewMode === 'focus' ? 'primary' : 'secondary'));
const showLiveBadge = computed(() => !playbackActive.value && mediaReady.value && Boolean(cameraVideoUrl.value));
const progressEmitIntervalMs = computed(() => {
	const speed = Math.max(1, Number(props.playbackSpeed) || 1);
	// Cập nhật dày hơn ở tốc độ cao để giá trị đồng hồ luôn sát khung hình đang hiển thị,
	// nhờ vậy lúc pause đồng hồ đóng băng đúng ngay khung hình vừa dừng.
	return Math.max(40, Math.round(120 / speed));
});

const applyPlaybackPaused = () => {
	const video = videoRef.value;
	if (!video || !playbackActive.value) return;
	if (props.playbackPaused) {
		video.pause();
		stopProgressReporting();
		return;
	}
	video.play().catch(() => undefined);
	startProgressReporting();
};

// Báo tiến độ render thực tế của thẻ <video> lên PlaybackBar. mediaTime/currentTime chỉ tăng khi
// frame thật sự được hiển thị và tự dừng khi buffering/giật -> đồng hồ playback bám đúng khung hình
// thay vì chạy theo wall-clock (vốn vẫn tick lúc video đứng hình).
const stopProgressReporting = () => {
	progressReporting = false;
	const video = videoRef.value as (HTMLVideoElement & { cancelVideoFrameCallback?: (handle: number) => void }) | undefined;
	if (video && progressFrameHandle && typeof video.cancelVideoFrameCallback === 'function') {
		video.cancelVideoFrameCallback(progressFrameHandle);
	}
	progressFrameHandle = 0;
	if (progressIntervalTimer) {
		window.clearInterval(progressIntervalTimer);
		progressIntervalTimer = null;
	}
};

const emitPlaybackProgress = (sample: { mediaTimeSec: number; rtpTimestamp?: number; captureTimeMs?: number; final?: boolean }) => {
	if (!Number.isFinite(sample.mediaTimeSec) || sample.mediaTimeSec < 0) return;
	emit('playback-progress', {
		id: safeCamera.value.id,
		mediaTimeSec: sample.mediaTimeSec,
		rtpTimestamp: sample.rtpTimestamp,
		captureTimeMs: sample.captureTimeMs,
		final: sample.final,
		streamEpoch: playbackStreamEpoch,
		sessionId: props.playbackSessionId,
	});
};

const startProgressReporting = () => {
	stopProgressReporting();
	if (!props.playbackActive || props.playbackPaused) return;
	const video = videoRef.value as
		| (HTMLVideoElement & { requestVideoFrameCallback?: (callback: (now: number, metadata: { mediaTime?: number; rtpTimestamp?: number; captureTime?: number }) => void) => number })
		| undefined;
	if (!video) return;
	progressReporting = true;
	lastProgressEmitMs = 0;
	latestFrameMeta = null;
	const reportingSessionId = props.playbackSessionId;
	if (typeof video.requestVideoFrameCallback === 'function') {
		const onFrame = (_now: number, metadata: { mediaTime?: number; rtpTimestamp?: number; captureTime?: number }) => {
			if (!progressReporting) return;
			if (reportingSessionId !== props.playbackSessionId) return;
			const current = videoRef.value as (HTMLVideoElement & { requestVideoFrameCallback?: (callback: (now: number, metadata: { mediaTime?: number; rtpTimestamp?: number; captureTime?: number }) => void) => number }) | undefined;
			if (!current || typeof current.requestVideoFrameCallback !== 'function') return;
			// Lưu metadata khung hình mới nhất mỗi frame (không throttle) cho mẫu 'final' khi pause.
			latestFrameMeta = {
				mediaTimeSec: typeof metadata?.mediaTime === 'number' ? metadata.mediaTime : current.currentTime,
				rtpTimestamp: typeof metadata?.rtpTimestamp === 'number' ? metadata.rtpTimestamp : undefined,
				captureTimeMs: typeof metadata?.captureTime === 'number' ? metadata.captureTime : undefined,
			};
			const nowMs = performance.now();
			if (nowMs - lastProgressEmitMs >= progressEmitIntervalMs.value) {
				lastProgressEmitMs = nowMs;
				emitPlaybackProgress(latestFrameMeta);
			}
			progressFrameHandle = current.requestVideoFrameCallback(onFrame);
		};
		progressFrameHandle = video.requestVideoFrameCallback(onFrame);
		return;
	}
	progressIntervalTimer = window.setInterval(() => {
		if (reportingSessionId !== props.playbackSessionId) {
			stopProgressReporting();
			return;
		}
		const current = videoRef.value;
		if (!current) return;
		latestFrameMeta = { mediaTimeSec: current.currentTime };
		emitPlaybackProgress({ mediaTimeSec: current.currentTime });
	}, progressEmitIntervalMs.value);
};

const getStreamUrlCacheKey = () =>
	[safeCamera.value.deviceId, safeCamera.value.ip, streamProfile.value, streamResolution.value].join('|');

const resolveStreamUrl = async (options: { keepRetryState?: boolean } = {}) => {
	const version = ++streamResolveVersion;
	if (!options.keepRetryState) streamRetryConsumed = false;
	resolvedVideoUrl.value = '';
	isPlaybackLoading.value = false;
	mediaReady.value = false;
	if (!shouldPlayVideo.value) return;
	const overrideUrl = safeVideoUrlOverride.value;
	if (overrideUrl) {
		isPlaybackLoading.value = true;
		resolvedVideoUrl.value = overrideUrl;
		return;
	}
	if (props.disableFallbackResolve) return;
	const fallbackUrl = safeCamera.value.videoUrl.trim();
	if (fallbackUrl && !props.preferApiStream) {
		if (version === streamResolveVersion) resolvedVideoUrl.value = fallbackUrl;
		return;
	}
	if (!safeCamera.value.deviceId) return;
	isPlaybackLoading.value = true;
	try {
		const cacheKey = getStreamUrlCacheKey();
		const cached = streamUrlCache.get(cacheKey);
		if (cached && Date.now() - cached.cachedAt < streamUrlCacheMaxAgeMs) {
			if (version === streamResolveVersion) resolvedVideoUrl.value = cached.url;
			return;
		}
		const url = await getCctvWebRtcStreamUrl(safeCamera.value, {
			stream: streamProfile.value,
			resolution: streamResolution.value,
		});
		streamUrlCache.set(cacheKey, { url, cachedAt: Date.now() });
		if (version === streamResolveVersion) resolvedVideoUrl.value = url;
	} catch {
		if (version === streamResolveVersion) {
			resolvedVideoUrl.value = '';
			emit('stream-error', safeCamera.value.id);
		}
	} finally {
		if (version === streamResolveVersion) isPlaybackLoading.value = false;
	}
};

const clearFirstFrameTimeout = () => {
	if (firstFrameTimeout !== null) window.clearTimeout(firstFrameTimeout);
	firstFrameTimeout = null;
};

const stopWebRtcPlayback = () => {
	webRtcStartVersion++;
	clearFirstFrameTimeout();
	stopProgressReporting();
	if (websocket) {
		websocket.onopen = null;
		websocket.onmessage = null;
		websocket.onerror = null;
		websocket.onclose = null;
		websocket.close();
		websocket = null;
	}
	if (peerConnection) {
		peerConnection.ontrack = null;
		peerConnection.onicecandidate = null;
		peerConnection.onconnectionstatechange = null;
		peerConnection.close();
		peerConnection = null;
	}
	pendingIceCandidates.length = 0;
	hasReceivedRemoteStream = false;
	isPlaybackLoading.value = false;
	mediaReady.value = false;
	const video = videoRef.value;
	if (video) {
		video.srcObject = null;
	}
};

const failWebRtcPlayback = () => {
	stopWebRtcPlayback();
	emit('stream-error', safeCamera.value.id);
};

const stopPlayback = () => {
	stopWebRtcPlayback();
	const video = videoRef.value;
	if (video) {
		video.removeAttribute('src');
		video.load();
	}
};

const emitMediaReadyAfterFirstFrame = (expectedUrl: string) => {
	const video = videoRef.value;
	if (!video) return;
	const startVersion = webRtcStartVersion;
	const markReady = () => {
		if (startVersion !== webRtcStartVersion) return;
		if (expectedUrl && cameraVideoUrl.value !== expectedUrl) return;
		clearFirstFrameTimeout();
		mediaReady.value = true;
		emit('media-ready', safeCamera.value.id);
	};
	const frameCallback = (video as HTMLVideoElement & {
		requestVideoFrameCallback?: (callback: () => void) => number;
	}).requestVideoFrameCallback;
	if (frameCallback) {
		frameCallback.call(video, markReady);
		return;
	}
	if (video.readyState >= HTMLMediaElement.HAVE_CURRENT_DATA) {
		window.setTimeout(markReady, 0);
		return;
	}
	const onReady = () => {
		video.removeEventListener('loadeddata', onReady);
		video.removeEventListener('playing', onReady);
		markReady();
	};
	video.addEventListener('loadeddata', onReady, { once: true });
	video.addEventListener('playing', onReady, { once: true });
};

const attachRemoteStream = (stream: MediaStream) => {
	const video = videoRef.value;
	if (!video) return;
	playbackStreamEpoch++; // stream mới gắn vào -> báo PlaybackBar neo lại đồng hồ
	hasReceivedRemoteStream = true;
	overrideRefreshRequested = false;
	isPlaybackLoading.value = false;
	const expectedUrl = cameraVideoUrl.value;
	video.srcObject = stream;
	if (props.playbackPaused) {
		clearFirstFrameTimeout();
		mediaReady.value = true;
		emit('media-ready', safeCamera.value.id);
		video.pause();
		return;
	}
	video.play().then(() => {
		emitMediaReadyAfterFirstFrame(expectedUrl);
		startProgressReporting();
	}).catch(() => undefined);
};

const ensurePeerConnection = () => {
	if (peerConnection) return peerConnection;
	peerConnection = new RTCPeerConnection({
		iceServers: [],
	});
	peerConnection.ontrack = (event) => {
		const [stream] = event.streams;
		if (stream) {
			attachRemoteStream(stream);
			return;
		}
		const fallbackStream = new MediaStream([event.track]);
		attachRemoteStream(fallbackStream);
	};
	peerConnection.onicecandidate = (event) => {
		if (!event.candidate || !websocket || websocket.readyState !== WebSocket.OPEN) return;
		websocket.send(JSON.stringify({ ice: event.candidate }));
	};
	peerConnection.onconnectionstatechange = () => {
		if (!peerConnection) return;
		if (peerConnection.connectionState === 'failed' || peerConnection.connectionState === 'closed') {
			failWebRtcPlayback();
		}
	};
	return peerConnection;
};

const parseSignalMessage = (data: string): { sdp?: RTCSessionDescriptionInit; ice?: RTCIceCandidateInit; candidate?: RTCIceCandidateInit } | null => {
	const text = data.trim();
	if (!text) return null;
	if (text.startsWith('{') || text.startsWith('[')) {
		try {
			const payload = JSON.parse(text) as Record<string, unknown>;
			if (payload.sdp && typeof payload.sdp === 'object') {
				const sdp = payload.sdp as Record<string, unknown>;
				if (typeof sdp.type === 'string' && typeof sdp.sdp === 'string') {
					return { sdp: { type: sdp.type as 'offer' | 'answer', sdp: sdp.sdp } };
				}
			}
			if (payload.ice && typeof payload.ice === 'object') {
				return { ice: payload.ice as RTCIceCandidateInit };
			}
			if (payload.candidate && typeof payload.candidate === 'object') {
				return { candidate: payload.candidate as RTCIceCandidateInit };
			}
		} catch {
			return null;
		}
	}
	if (text.startsWith('v=0')) {
		return { sdp: { type: 'offer', sdp: text } };
	}
	return null;
};

// NX gửi media playback qua WebRTC; thời gian footage không nằm trong kênh WS signaling.
// Vị trí playback thực tế được suy ra từ tiến độ render của thẻ <video> (xem startProgressReporting).
// URL WebSocket đi qua Cctv Proxy (BE giữ phiên NxVMS, URL kèm ticket) nên không cần cookie/đăng nhập NxVMS ở FE.
const startWebRtcPlayback = async (options: { retryOnConnectError?: boolean } = {}) => {
	stopWebRtcPlayback();
	const startVersion = webRtcStartVersion;
	await nextTick();
	const video = videoRef.value;
	if (!video || !cameraVideoUrl.value) {
		return;
	}
	isPlaybackLoading.value = true;
	if (startVersion !== webRtcStartVersion || !cameraVideoUrl.value) {
		isPlaybackLoading.value = false;
		return;
	}
	if (typeof window.RTCPeerConnection === 'undefined') {
		failWebRtcPlayback();
		return;
	}
	// Ticket trong URL đã hết hạn (URL cache lâu / cha giữ URL cũ) -> xin URL mới thay vì kết nối chắc chắn bị 401.
	// Mỗi chiều chỉ xin 1 lần; nếu URL mới vẫn bị coi là hết hạn (đồng hồ máy lệch) thì cứ kết nối để proxy quyết định.
	if (isCctvStreamUrlExpired(cameraVideoUrl.value)) {
		if (safeVideoUrlOverride.value) {
			if (!overrideRefreshRequested) {
				overrideRefreshRequested = true;
				isPlaybackLoading.value = false;
				emit('stream-expired', safeCamera.value.id);
				return;
			}
		} else if (!streamRetryConsumed) {
			streamRetryConsumed = true;
			streamUrlCache.delete(getStreamUrlCacheKey());
			resolveStreamUrl({ keepRetryState: true });
			return;
		}
	}
	// Báo lỗi sau một lượt thử lại; không để khung nhúng giữ mãi chế độ CCTV khi proxy lỗi.
	const retryConnection = async () => {
		if (startVersion !== webRtcStartVersion) return;
		if (!options.retryOnConnectError || hasReceivedRemoteStream) {
			failWebRtcPlayback();
			return;
		}
		stopWebRtcPlayback();
		streamRetryConsumed = true;
		try {
			if (safeVideoUrlOverride.value) {
				await startWebRtcPlayback({ retryOnConnectError: false });
				return;
			}
			streamUrlCache.delete(getStreamUrlCacheKey());
			await resolveStreamUrl({ keepRetryState: true });
		} catch {
			failWebRtcPlayback();
		}
	};
	// Có SDP/ontrack vẫn có thể không nhận hình (ICE không tới được IP LAN).
	firstFrameTimeout = window.setTimeout(() => {
		if (startVersion === webRtcStartVersion && !mediaReady.value) retryConnection();
	}, 20_000);
	try {
		websocket = new WebSocket(cameraVideoUrl.value);
	} catch {
		retryConnection();
		return;
	}
	websocket.onopen = () => {
		ensurePeerConnection();
	};
	websocket.onmessage = async (event) => {
		try {
			if (typeof event.data !== 'string') return;
			const signal = parseSignalMessage(event.data);
			if (!signal) return;
			const pc = ensurePeerConnection();
			if (signal.sdp) {
				if (signal.sdp.type === 'offer') {
					await pc.setRemoteDescription(new RTCSessionDescription(signal.sdp));
					const answer = await pc.createAnswer();
					await pc.setLocalDescription(answer);
					websocket?.readyState === WebSocket.OPEN &&
						websocket.send(
							JSON.stringify({
								sdp: {
									type: 'answer',
									sdp: pc.localDescription?.sdp || answer.sdp || '',
								},
							})
						);
					for (const iceCandidate of pendingIceCandidates.splice(0)) {
						await pc.addIceCandidate(new RTCIceCandidate(iceCandidate));
					}
					return;
				}
				if (signal.sdp.type === 'answer') {
					await pc.setRemoteDescription(new RTCSessionDescription(signal.sdp));
					return;
				}
			}
			const iceCandidate = signal.ice || signal.candidate;
			if (iceCandidate) {
				try {
					if (pc.remoteDescription) {
						await pc.addIceCandidate(new RTCIceCandidate(iceCandidate));
					} else {
						pendingIceCandidates.push(iceCandidate);
					}
				} catch {
					// Ignore candidates that arrive before remote description is ready.
				}
			}
		} catch {
			retryConnection();
		}
	};
	websocket.onerror = () => {
		retryConnection();
	};
	websocket.onclose = (event) => {
		pendingIceCandidates.length = 0;
		if (!hasReceivedRemoteStream || event.code !== 1000) retryConnection();
	};
};

const startNativePlayback = async () => {
	stopPlayback();
	await nextTick();
	const video = videoRef.value;
	if (!video || !nativeVideoSource.value) return;
	playbackStreamEpoch++; // nguồn mới -> báo PlaybackBar neo lại đồng hồ
	isPlaybackLoading.value = true;
	const expectedUrl = cameraVideoUrl.value;
	video.muted = true;
	video.playsInline = true;
	video.src = nativeVideoSource.value;
	if (props.playbackPaused) {
		isPlaybackLoading.value = false;
		mediaReady.value = true;
		video.pause();
		emit('media-ready', safeCamera.value.id);
		return;
	}
	video.play().then(() => {
		isPlaybackLoading.value = false;
		emitMediaReadyAfterFirstFrame(expectedUrl);
		startProgressReporting();
	}).catch(() => {
		isPlaybackLoading.value = false;
		mediaReady.value = false;
	});
};

const onTileClick = () => {
	if (props.camera.isPlaceholder) return;
	emit('select', safeCamera.value.id);
};

const onMediaClick = (event: MouseEvent) => {
	if (!props.backOnClick || props.showControls) return;
	event.stopPropagation();
	emit('back-to-grid');
};

watch(
	() =>
		[
			safeCamera.value.id,
			safeCamera.value.videoUrl,
			safeCamera.value.deviceId,
			safeCamera.value.ip,
			props.viewMode || '',
			streamResolution.value,
			streamProfile.value,
			props.preferApiStream ? 1 : 0,
			safeVideoUrlOverride.value,
			props.disableFallbackResolve ? 1 : 0,
			shouldPlayVideo.value ? 1 : 0,
		].join(':'),
	() => {
		resolveStreamUrl();
	},
	{ immediate: true, flush: 'post' }
);

watch(
	() => cameraVideoUrl.value,
	() => {
		if (!shouldPlayVideo.value || !cameraVideoUrl.value) {
			stopPlayback();
			return;
		}
		if (isWebSocketUrl.value) {
			startWebRtcPlayback({ retryOnConnectError: !streamRetryConsumed });
			return;
		}
		startNativePlayback();
	},
	{ immediate: true, flush: 'post' }
);

watch(
	() => props.playbackPaused,
	() => {
		applyPlaybackPaused();
	},
	{ flush: 'post' }
);

watch(
	() => props.playbackActive,
	(active) => {
		if (active && !props.playbackPaused && videoRef.value && !videoRef.value.paused) {
			startProgressReporting();
		} else if (!active) {
			stopProgressReporting();
		}
	},
	{ flush: 'post' }
);

watch(
	() => props.playbackSpeed,
	() => {
		if (props.playbackActive && !props.playbackPaused && videoRef.value && !videoRef.value.paused) startProgressReporting();
	},
	{ flush: 'post' }
);

onBeforeUnmount(() => {
	streamResolveVersion++;
	stopPlayback();
});
</script>

<style scoped lang="scss">
.camera-tile {
	border: 1px solid var(--el-border-color-light, #dcdfe6);
	border-radius: var(--el-border-radius-base, 8px);
	background: var(--el-bg-color, #ffffff);
	padding: 6px;
	display: flex;
	flex-direction: column;
	gap: 8px;
	height: 100%;
	min-height: 0;
	overflow: hidden;

	&.editable {
		outline: 2px dashed var(--el-color-primary);
		outline-offset: 2px;
		cursor: move;
	}

	&.camera-only {
		padding: 0;
		border-color: #0f172a;
		background: #0f172a;
	}

	&.selected {
		border-color: var(--el-color-primary);
		box-shadow: 0 0 0 3px var(--el-color-primary-light-8, rgba(46, 144, 250, 0.18));
	}
}

.placeholder-tile {
	width: 100%;
	height: 100%;
	border: 1px dashed var(--el-border-color, #dcdfe6);
	border-radius: 6px;
	background: var(--el-fill-color-lighter, #f8fafc);
	display: grid;
	place-items: center;
	position: relative;
	cursor: move;
}

.placeholder-icon {
	width: 52px;
	height: 52px;
	border-radius: 999px;
	display: inline-grid;
	place-items: center;
	background: var(--el-color-primary-light-9, #e0f2fe);
	color: var(--el-color-primary, #409eff);
	box-shadow: inset 0 0 0 1px var(--el-color-primary-light-7, #bae6fd);
	cursor: pointer;

	:deep(.el-icon) {
		font-size: 24px;
	}
}

.placeholder-actions {
	position: absolute;
	right: 8px;
	top: 8px;
	display: flex;
	align-items: center;
	gap: 6px;

	.cctv-tooltip-wrapper {
		display: inline-flex;
	}
}

.camera-tile.selected .placeholder-tile {
	border-color: var(--el-color-primary);
	background: var(--el-color-primary-light-9, #eff8ff);
}


.feed {
	position: relative;
	min-height: 0;
	flex: 1;
	min-width: 0;
	border-radius: 6px;
	overflow: hidden;
	background:
		linear-gradient(135deg, rgba(148, 163, 184, 0.22), transparent 32%),
		linear-gradient(180deg, #111827, #1f2937);
}

.camera-video {
	width: 100%;
	height: 100%;
	display: block;
	object-fit: cover;
	background: #111827;
}

.camera-video-focus {
	width: 100%;
	height: 100%;
	display: block;
	object-fit: contain;
	background: #111827;
}

.camera-loading-overlay {
	position: absolute;
	inset: 0;
	z-index: 4;
	display: grid;
	place-items: center;
	align-content: center;
	gap: 10px;
	background: rgba(15, 23, 42, 0.58);
	color: #ffffff;
	font-size: 13px;
	font-weight: 600;
	pointer-events: none;
}

.camera-loading-spinner {
	width: 28px;
	height: 28px;
	border-radius: 999px;
	border: 3px solid rgba(255, 255, 255, 0.3);
	border-top-color: #ffffff;
	animation: camera-loading-spin 0.8s linear infinite;
}

@keyframes camera-loading-spin {
	to {
		transform: rotate(360deg);
	}
}

.camera-only .feed {
	min-height: 100%;
	border-radius: 8px;
}

.camera-only-label {
	position: absolute;
	left: 10px;
	right: 10px;
	bottom: 0px;
	display: flex;
	align-items: center;
	justify-content: space-between;
	gap: 10px;
	min-height: 28px;
	padding: 6px 8px;
	border-radius: 6px;
	background: rgba(15, 23, 42, 0.3);
	color: #ffffff;
	pointer-events: none;

	span {
		min-width: 0;
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
		font-size: 12px;
		font-weight: 600;
	}

	small {
		display: inline-flex;
		align-items: center;
		gap: 6px;
		flex-shrink: 0;
		font-size: 10px;
		font-weight: 700;
	}

	i {
		width: 8px;
		height: 8px;
		border-radius: 999px;
		background: #22c55e;
	}

	.warning i {
		background: #f59e0b;
	}

	.offline i {
		background: #ef4444;
	}
}

.camera-only-actions {
	position: absolute;
	top: 6px;
	right: 6px;
	z-index: 2;
	opacity: 0;
	transition: opacity 0.16s ease;
	display: grid;
	gap: 4px;
	max-height: calc(100% - 12px);
	overflow: hidden;

	.cctv-tooltip-wrapper {
		display: inline-flex;
	}

	// Thu nhỏ nút khi cell hẹp
	:deep(.camera-control-btn--icon) {
		width: 28px;
		height: 28px;
		min-width: 28px;
		font-size: 12px;
		
	}
	:deep(.el-icon){
			margin-right: 0 !important;
		}
}

.camera-tile:hover .camera-only-actions {
	opacity: 1;
}

.status {
	height: 20px;
	display: inline-flex;
	align-items: center;
	gap: 5px;
	flex-shrink: 0;
	padding: 0 7px;
	border-radius: 999px;
	background: rgba(15, 23, 42, 0.72);
	color: #ffffff;
	font-size: 9px;
	font-weight: 700;

	i {
		width: 7px;
		height: 7px;
		border-radius: 999px;
		background: #ef4444;
	}
}

.status.is-live {
	background: rgba(127, 29, 29, 0.86);
	color: #ffffff;
}

.tile-caption {
	position: absolute;
	left: 6px;
	right: 6px;
	bottom: 6px;
	z-index: 2;
	display: flex;
	align-items: center;
	justify-content: space-between;
	gap: 8px;
	min-height: 20px;
	padding: 3px 7px;
	border-radius: 6px;
	background: rgba(15, 23, 42, 0.68);
	color: #ffffff;
	pointer-events: none;

	span {
		min-width: 0;
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
		font-size: 11px;
		font-weight: 600;
		line-height: 14px;
	}

	small {
		font-size: 9px;
		font-weight: 700;
	}
}

.overlay {
	position: absolute;
	inset: 0;
	z-index: 3;
	padding: 8px;
	background: rgba(15, 23, 42, 0.42);
	color: #ffffff;
	display: flex;
	flex-direction: column;
	align-items: flex-end;
	justify-content: flex-start;
	gap: 8px;
	opacity: 0;
	transition: opacity 0.18s ease;

	h4,
	p {
		margin: 0;
	}

	h4 {
		font-size: 16px;
	}

	p {
		color: #cbd5e1;
		font-size: 12px;
	}
}

.camera-tile:hover .overlay,
.camera-tile.large .overlay {
	opacity: 1;
}

.tile-actions {
	display: flex;
	align-items: center;
	flex-wrap: wrap;
	gap: 6px;

	.cctv-tooltip-wrapper {
		display: inline-flex;
	}
}

.camera-control-btn.is-danger {
	background: rgba(127, 29, 29, 0.68);
	border-color: rgba(248, 113, 113, 0.36);
	color: #ffffff;
}

.camera-control-btn.is-danger:hover:not(:disabled):not(.is-disabled) {
	background: rgba(185, 28, 28, 0.78);
	border-color: rgba(252, 165, 165, 0.58);
}

footer {
	display: flex;
	justify-content: space-between;
	gap: 12px;
	color: #344054;

	span {
		font-weight: 600;
	}

	small {
		color: #667085;
	}
}
</style>

<!-- Token dùng chung (không scoped) để các lớp camera-control-btn toàn cục áp dụng đúng -->
<style lang="scss">
@use '../styles/cameraControlTokens.scss';
</style>
