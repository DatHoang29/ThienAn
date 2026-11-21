<template>
	<div class="cctv-camera-view" :class="`cctv-camera-view--${state.mode}`">
		<!-- Đang hỏi CCTV xem IP của thiết bị có được hỗ trợ hay không -->
		<div v-if="state.mode === 'loading'" class="cctv-camera-view__status">
			<span class="cctv-camera-view__spinner"></span>
			<span>{{ t('lz.label.cctvDashboard.loadingVideo') }}</span>
		</div>

		<!-- Có trong hệ thống CCTV: dùng panel đầy đủ (WebRTC + PTZ + playback + snapshot) -->
		<CctvFocusPanel
			v-else-if="state.mode === 'cctv' && state.camera"
			:key="cameraKey"
			:camera="state.camera"
			:initial-stream-url="state.streamUrl"
			focus-mode="view"
			class="cctv-camera-view__panel"
			@back="handleIgnoredPanelEvent"
			@stream-error="handleCctvStreamError"
		/>

		<!-- Không được CCTV hỗ trợ: giữ nguyên luồng RTSP như trước -->
		<LiveCam v-else-if="state.mode === 'rtsp' && rtspUrl" ref="liveCamRef" :rtsp-url="rtspUrl" :full-width="true" :autoplay="true" />

		<!-- Không có nguồn nào dùng được -->
		<div v-else-if="state.mode !== 'idle'" class="cctv-camera-view__status">
			<el-icon><ele-VideoCamera /></el-icon>
			<span>{{ t('lz.label.cctvDashboard.noVideo') }}</span>
		</div>

		<span
			v-if="showSourceBadge && (state.mode === 'cctv' || state.mode === 'rtsp')"
			class="cctv-camera-view__badge"
			:class="`cctv-camera-view__badge--${state.mode}`"
			:title="sourceTooltip"
		>
			{{ state.mode === 'cctv' ? 'CCTV' : 'RTSP' }}
		</span>
	</div>
</template>

<script setup lang="ts" name="cctvCameraView">
/**
 * CctvCameraView
 *
 * Khung view camera dùng chung: tự quyết định phát qua hệ thống CCTV (VMS/NX) hay qua RTSP.
 *
 * - Gửi IP của thiết bị xuống CCTV để kiểm tra hỗ trợ (xem `resolveCctvCameraSource`).
 * - Được hỗ trợ  -> `CctvFocusPanel`: video WebRTC + PTZ + playback + snapshot.
 * - Không hỗ trợ -> `LiveCam`: luồng RTSP như hiện tại (không thay đổi hành vi cũ).
 *
 * Vòng đời: chỉ render nội dung khi `active` và đã `start()` (hoặc `autoStart`). Khi `stop()`
 * component con bị hủy nên WebRTC / JSMpeg tự dọn trong hook `onBeforeUnmount`/`onUnmounted`
 * của chúng — tránh treo kết nối khi mở nhiều modal camera cùng lúc.
 */
import { computed, onMounted, reactive, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import CctvFocusPanel from './CctvFocusPanel.vue';
import LiveCam from '/@/components/livecam/index.vue';
import {
	clearCctvComponentProbeCache,
	resolveCctvCameraSource,
	getRtspFallbackUrl,
	type CctvCameraSourceMode,
	type CctvCameraSourceReason,
} from './cctvComponent.service';
import type { VideoMonitoringItem } from '/@/views/cctv/dashboard/types';

const { t } = useI18n();

const props = withDefaults(
	defineProps<{
		/** Thiết bị ITS (cần `ip`; dùng thêm `url`, `name`, `kmNumber`, `metNumber`, `equipmentState`) */
		equipment?: any;
		/** Khung view có đang được hiển thị hay không (vd: tab đang mở) */
		active?: boolean;
		/** Tự phát ngay khi mount; đặt `false` nếu muốn tự gọi `start()` */
		autoStart?: boolean;
		/** Hiện nhãn nguồn phát (CCTV / RTSP) ở góc khung */
		showSourceBadge?: boolean;
	}>(),
	{
		equipment: null,
		active: true,
		autoStart: true,
		showSourceBadge: true,
	}
);

const emit = defineEmits<{
	(e: 'source-resolved', payload: { mode: CctvCameraSourceMode; reason: CctvCameraSourceReason }): void;
}>();

const liveCamRef = ref<InstanceType<typeof LiveCam>>();
const started = ref(false);
let resolveVersion = 0;

const state = reactive({
	mode: 'idle' as 'idle' | 'loading' | CctvCameraSourceMode,
	camera: null as VideoMonitoringItem | null,
	reason: '' as CctvCameraSourceReason,
	streamUrl: '',
	fallbackUrl: '',
});

/** Khoá nhận dạng thiết bị: đổi khoá => probe lại nguồn phát. */
const equipmentKey = computed(() => [props.equipment?.id, props.equipment?.ip, props.equipment?.url].map((value) => String(value ?? '')).join('|'));
const cameraKey = computed(() => [state.camera?.id, state.camera?.deviceId, state.camera?.ip].map((value) => String(value ?? '')).join('|'));
const rtspUrl = computed(() => state.fallbackUrl || getRtspFallbackUrl(props.equipment?.url));
const shouldRun = computed(() => started.value && props.active !== false);

const sourceTooltip = computed(() => {
	if (state.mode === 'cctv') return t('lz.entity.tms.liveCamera') + ' — CCTV';
	switch (state.reason) {
		case 'no-ip':
			return 'RTSP — thiết bị không có IP';
		case 'not-registered':
			return 'RTSP — CCTV không quản lý IP này';
		case 'stream-unavailable':
			return 'RTSP — CCTV không cấp được luồng';
		case 'probe-error':
			return 'RTSP — không kiểm tra được CCTV';
		default:
			return 'RTSP';
	}
});

/** `CctvFocusPanel` phát `back` khi click vào video (ở dashboard nghĩa là quay lại lưới) — trong khung nhúng thì bỏ qua. */
const handleIgnoredPanelEvent = () => undefined;

/** Có URL chưa có nghĩa là proxy/NxVMS kết nối được. Lỗi live phải quay về RTSP gốc. */
const handleCctvStreamError = () => {
	if (state.mode !== 'cctv' || !shouldRun.value) return;
	clearCctvComponentProbeCache(props.equipment?.ip);
	state.mode = 'rtsp';
	state.streamUrl = '';
	state.reason = 'stream-unavailable';
	emit('source-resolved', { mode: 'rtsp', reason: state.reason });
};

const resetSource = () => {
	resolveVersion++;
	state.mode = 'idle';
	state.camera = null;
	state.reason = '';
	state.streamUrl = '';
	state.fallbackUrl = '';
};

const resolveSource = async () => {
	const version = ++resolveVersion;
	state.mode = 'loading';
	state.camera = null;
	const source = await resolveCctvCameraSource(props.equipment);
	if (version !== resolveVersion) return;
	state.mode = source.mode;
	state.camera = source.camera;
	state.reason = source.reason;
	state.streamUrl = source.streamUrl;
	state.fallbackUrl = source.fallbackUrl;
	console.info('[cctvComponent] source-resolved', {
		equipmentId: props.equipment?.id, name: props.equipment?.name,
		deviceId: source.device?.deviceId, mode: source.mode, reason: source.reason,
	});
	emit('source-resolved', { mode: source.mode, reason: source.reason });
};

/** Bắt đầu phát (tương thích API cũ của `LiveCam`). */
const start = () => {
	started.value = true;
};

/** Dừng phát và giải phóng kết nối (tương thích API cũ của `LiveCam`). */
const stop = () => {
	started.value = false;
};

/** Probe lại nguồn phát, bỏ qua cache (dùng khi thiết bị vừa được đồng bộ sang CCTV). */
const refresh = async () => {
	clearCctvComponentProbeCache(props.equipment?.ip);
	if (shouldRun.value) await resolveSource();
};

watch(
	[shouldRun, equipmentKey],
	([nextShouldRun]) => {
		if (nextShouldRun) resolveSource();
		else resetSource();
	},
	{ flush: 'post' }
);

onMounted(() => {
	if (props.autoStart) started.value = true;
});

defineExpose({ start, stop, refresh, mode: computed(() => state.mode) });
</script>

<style scoped lang="scss">
.cctv-camera-view {
	position: relative;
	width: 100%;
	height: 100%;
	min-height: 0;
	background-color: #000;
	overflow: hidden;
}

.cctv-camera-view__panel {
	width: 100%;
	height: 100%;
}

.cctv-camera-view__status {
	width: 100%;
	height: 100%;
	min-height: 120px;
	display: flex;
	flex-direction: column;
	align-items: center;
	justify-content: center;
	gap: 8px;
	color: rgba(255, 255, 255, 0.75);
	font-size: 13px;

	:deep(.el-icon) {
		font-size: 26px;
	}
}

.cctv-camera-view__spinner {
	width: 22px;
	height: 22px;
	border: 2px solid rgba(255, 255, 255, 0.25);
	border-top-color: #ffffff;
	border-radius: 50%;
	animation: cctv-camera-view-spin 0.9s linear infinite;
}

@keyframes cctv-camera-view-spin {
	to {
		transform: rotate(360deg);
	}
}

.cctv-camera-view__badge {
	position: absolute;
	top: 6px;
	left: 6px;
	z-index: 4;
	padding: 1px 6px;
	border-radius: 3px;
	font-size: 10px;
	font-weight: 600;
	letter-spacing: 0.4px;
	line-height: 16px;
	pointer-events: none;
	user-select: none;
	color: #ffffff;
	background: rgba(15, 23, 42, 0.55);
	border: 1px solid rgba(255, 255, 255, 0.18);
}

.cctv-camera-view__badge--cctv {
	background: rgba(22, 119, 255, 0.62);
	border-color: rgba(147, 197, 253, 0.55);
}
</style>
