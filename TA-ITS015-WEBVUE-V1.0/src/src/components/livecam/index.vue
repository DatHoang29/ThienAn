<template>
    <div class="live-cam" :class="{ 'full-width': fullWidth }">
        <div class="canvas-container">
            <canvas ref="videoCanvas"></canvas>
            <div v-if="errorMessage" class="live-cam__error" role="status">
                <span>{{ errorMessage }}</span>
                <button type="button" @click.stop="start">Thử lại</button>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted, watch } from 'vue';
import JSMpeg from '@cycjimmy/jsmpeg-player'; // pnpm install @cycjimmy/jsmpeg-player

const props = defineProps({
	autoplay: {
		type: Boolean,
		default: false,
	},
	fullWidth: {
		type: Boolean,
		default: true,
	},
	rtspUrl: {
		type: String,
		required: true,
	},
	streamHost: {
		type: String,
		default: 'ws://115.78.1.139:12217',
	},
	streamWidth: {
		type: Number,
		default: 1280,
	},
	streamHeight: {
		type: Number,
		default: 720,
	},
});

const videoCanvas = ref<HTMLCanvasElement | null>(null);
let player: any = null;
const isPlaying = ref(false);
const errorMessage = ref('');
const maxAttempts = 3;
const retryDelays = [1000, 3000];
const sessionId = Math.random().toString(36).slice(2, 10);
let running = false;
let attempt = 0;
let version = 0;
let retryTimer: ReturnType<typeof setTimeout> | null = null;
let firstFrameTimer: ReturnType<typeof setTimeout> | null = null;
let detachSocketListeners: (() => void) | null = null;

// Chỉ log host và trạng thái; không ghi URL RTSP, user/password hoặc raw Error/Event.
const hostOf = (url: string) => {
	try { return new URL(url).host; } catch { return ''; }
};
const logState = (event: string, details: Record<string, unknown> = {}) => {
	console.info('[LiveCam]', { event, sessionId, cameraHost: hostOf(props.rtspUrl), relayHost: hostOf(props.streamHost), attempt, maxAttempts, ...details });
};

const destroyPlayer = () => {
	if (firstFrameTimer !== null) clearTimeout(firstFrameTimer);
	firstFrameTimer = null;
	detachSocketListeners?.();
	detachSocketListeners = null;
	if (player) {
		player.destroy();
		player = null;
	}
	isPlaying.value = false;
};

const initializePlayer = () => {
	if (!running || !videoCanvas.value) return;
	const currentVersion = ++version;
	attempt++;
	let failed = false;
	let bytes = 0;
	let frames = 0;
	let connected = false;
	let receivedMessage = false;
	const startedAt = Date.now();
	const fail = (reason: string, details: Record<string, unknown> = {}) => {
		if (failed || currentVersion !== version || !running) return;
		failed = true;
		logState('failed', { reason, elapsedMs: Date.now() - startedAt, connected, bytes, frames, ...details });
		destroyPlayer();
		if (attempt >= maxAttempts) {
			running = false;
			errorMessage.value = frames > 0 ? 'Mất kết nối camera' : 'Không nhận được hình ảnh camera';
			logState('retry-exhausted', { reason });
			return;
		}
		const delayMs = retryDelays[attempt - 1];
		logState('retry-scheduled', { delayMs });
		retryTimer = setTimeout(() => {
			retryTimer = null;
			if (running && currentVersion === version) initializePlayer();
		}, delayMs);
	};
	logState('connecting');
	try {
		const url = `${props.streamHost}?url=${encodeURIComponent(props.rtspUrl)}`;
		videoCanvas.value.width = props.streamWidth;
		videoCanvas.value.height = props.streamHeight;
		player = new JSMpeg.Player(url, {
			canvas: videoCanvas.value,
			autoplay: true,
			videoBufferSize: 10 * 1024 * 1024,
			disableWebAssembly: true,
			audio: false,
			video: true,
			// Tắt vòng reconnect vô hạn của thư viện; component quản lý số lần thử.
			reconnectInterval: 0,
			onVideoDecode: () => {
				if (failed || currentVersion !== version || !running) return;
				frames++;
				if (frames !== 1) return;
				if (firstFrameTimer !== null) clearTimeout(firstFrameTimer);
				firstFrameTimer = null;
				isPlaying.value = true;
				logState('first-frame', { elapsedMs: Date.now() - startedAt, bytes });
			},
		});
		const socket = player.source?.socket as WebSocket | undefined;
		if (socket) {
			const onOpen = () => {
				connected = true;
				logState('connected', { elapsedMs: Date.now() - startedAt });
			};
			const onMessage = (event: MessageEvent) => {
				const binaryBytes = typeof event.data === 'string' ? 0 : event.data?.byteLength ?? event.data?.size ?? 0;
				bytes += binaryBytes;
				if (receivedMessage) return;
				receivedMessage = true;
				logState('first-message', { type: typeof event.data === 'string' ? 'text' : 'binary', bytes,
					textCode: typeof event.data === 'string' && /^\d{1,8}$/.test(event.data) ? event.data : undefined });
			};
			const onClose = (event: CloseEvent) => fail('socket-closed', { code: event.code, clean: event.wasClean });
			const onError = () => fail('socket-error');
			socket.addEventListener('open', onOpen);
			socket.addEventListener('message', onMessage);
			socket.addEventListener('close', onClose);
			socket.addEventListener('error', onError);
			detachSocketListeners = () => {
				socket.removeEventListener('open', onOpen);
				socket.removeEventListener('message', onMessage);
				socket.removeEventListener('close', onClose);
				socket.removeEventListener('error', onError);
			};
		}
		firstFrameTimer = setTimeout(() => fail(connected ? 'first-frame-timeout' : 'connect-timeout'), 15_000);
	} catch (error) {
		fail('player-error', { errorType: error instanceof Error ? error.name : 'Error' });
	}
};

// On page load
onMounted(() => {
	if (props.autoplay) {
		start();
	}
});

onUnmounted(() => {
	stop();
});

const stop = () => {
	running = false;
	version++;
	if (retryTimer !== null) clearTimeout(retryTimer);
	retryTimer = null;
	destroyPlayer();
}

const start = () => {
	if (running) return;
	running = true;
	attempt = 0;
	errorMessage.value = '';
	initializePlayer();
}

watch(() => [props.rtspUrl, props.streamHost], () => {
	const shouldRestart = running || props.autoplay;
	stop();
	errorMessage.value = '';
	if (shouldRestart) start();
});

defineExpose({
	start,
	stop,
});

</script>

<style scoped lang="scss">
.live-cam {
	position: relative;
	width: 100%;
	height: 100%;
	display: flex;
	justify-content: center;
	align-items: center;
	background-color: #000;
}

.canvas-container {
	position: relative;
	width: 100%;
	height: 100%;
	display: flex;
	justify-content: center;
	align-items: center;
	overflow: hidden;
}

.live-cam__error {
	position: absolute;
	inset: 0;
	z-index: 1;
	display: flex;
	flex-direction: column;
	align-items: center;
	justify-content: center;
	gap: 12px;
	background: rgba(0, 0, 0, 0.75);
	color: #fff;
	font-size: 14px;
}

.live-cam__error button {
	padding: 6px 14px;
	border: 1px solid #888;
	border-radius: 4px;
	background: #252525;
	color: inherit;
	cursor: pointer;
}

canvas {
	width: 100%;
	height: 100%;
	background-color: #000;
	position: absolute;
	top: 0;
	left: 0;
}
</style>
