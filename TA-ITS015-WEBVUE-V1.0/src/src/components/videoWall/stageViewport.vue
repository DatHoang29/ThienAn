<template>
	<!--
		Khung xem sân khấu tường màn hình dùng chung cho Video Wall (Quản lý kịch bản, Giám sát & điều khiển, Màn hình):
		  - Lăn chuột / nút góc phải dưới ⇒ phóng to / thu nhỏ quanh con trỏ.
		  - Nội dung lớn hơn khung ⇒ bấm giữ nền rồi kéo để di chuyển; cú click kết thúc thao tác kéo bị nuốt.
		Component chỉ quản lý zoom / cuộn; nội dung tự tính kích thước theo `zoom` (v-model:zoom).
	-->
	<div class="vsv" :class="{ 'is-pannable': zoomable && canPan, 'is-panning': isPanning }">
		<div ref="scrollRef" class="vsv-scroll" @mousedown="startPan" @wheel="onWheel" @click.capture="onClickCapture" @click.self="emit('backgroundClick')">
			<slot />
		</div>

		<!-- Chú thích: chip góc trên trái (ngoài vùng cuộn nên đứng yên khi cuộn / zoom), rê vào hiện nội dung -->
		<el-tooltip v-if="guide" :content="guide" placement="bottom-start" :show-after="150">
			<span class="vsv-guide">
				<el-icon><ele-InfoFilled /></el-icon>{{ guideLabel || $t('lz.label.videoWall.cellLabelGuide') }}
			</span>
		</el-tooltip>

		<!-- Nút zoom: nằm ngoài vùng cuộn để luôn cố định ở góc; chỉ hiện khi rê chuột vào hoặc đang phóng to -->
		<div v-if="zoomable" class="vsv-zoom" :class="{ 'is-active': zoom > min }" :title="$t('lz.label.videoWall.zoomHint')">
			<button type="button" class="vsv-zoom-btn" :disabled="zoom <= min" :title="$t('lz.label.videoWall.zoomOut')" @click="zoomBy(1 / step)">
				<el-icon><ele-Minus /></el-icon>
			</button>
			<button type="button" class="vsv-zoom-value" :disabled="zoom <= min" :title="$t('lz.label.videoWall.zoomFit')" @click="setZoom(min)">
				{{ Math.round(zoom * 100) }}%
			</button>
			<button type="button" class="vsv-zoom-btn" :disabled="zoom >= max" :title="$t('lz.label.videoWall.zoomIn')" @click="zoomBy(step)">
				<el-icon><ele-Plus /></el-icon>
			</button>
		</div>
	</div>
</template>

<script lang="ts" setup name="videoWallStageViewport">
import { nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue';

interface Props {
	/** Mức zoom (1 = vừa khung) — v-model:zoom. */
	zoom: number;
	/** Cho zoom / kéo nền. Tắt thì chỉ còn khung cuộn thường. */
	zoomable?: boolean;
	min?: number;
	max?: number;
	step?: number;
	/**
	 * Selector của phần tử bao đúng kích thước nội dung (vd tường) — dùng để giữ điểm dưới con trỏ khi zoom.
	 * Không truyền thì lấy phần tử con đầu tiên của vùng cuộn.
	 */
	anchor?: string;
	/** Bấm giữ trên phần tử khớp selector này thì không kéo nền (phần tử có thao tác kéo riêng). */
	panExclude?: string;
	/** Nội dung chú thích (tooltip của chip góc trên trái); bỏ trống ⇒ không hiện chip. */
	guide?: string;
	/** Chữ trên chip; mặc định "Cách đọc vị trí ô". */
	guideLabel?: string;
}
const props = withDefaults(defineProps<Props>(), { zoomable: true, min: 1, max: 5, step: 1.25, anchor: '', panExclude: '', guide: '', guideLabel: '' });
const emit = defineEmits<{
	(e: 'update:zoom', zoom: number): void;
	/** Click thẳng vào nền vùng cuộn (không tính click kết thúc thao tác kéo). */
	(e: 'backgroundClick'): void;
	/** Cỡ khung xem (offsetWidth/Height: không đổi khi thanh cuộn hiện ra lúc phóng to ⇒ tỉ lệ vừa khung không nhảy). */
	(e: 'resize', size: { w: number; h: number }): void;
}>();

const scrollRef = ref<HTMLElement>();

const anchorEl = (): HTMLElement | null =>
	(props.anchor ? scrollRef.value?.querySelector(props.anchor) : scrollRef.value?.firstElementChild) as HTMLElement | null;

/* ── Zoom ── */

/**
 * Đổi mức zoom, giữ nguyên điểm nội dung nằm dưới anchor (toạ độ client; mặc định tâm khung).
 * Nội dung căn giữa bằng margin auto nên vị trí đổi theo zoom ⇒ đo lại sau khi vẽ rồi bù cuộn.
 */
function setZoom(next: number, point?: { x: number; y: number }) {
	const el = scrollRef.value;
	const z = Math.min(props.max, Math.max(props.min, Math.round(next * 100) / 100));
	if (!el || z === props.zoom) return;

	const content = anchorEl();
	const view = el.getBoundingClientRect();
	const ax = point?.x ?? view.left + el.clientWidth / 2;
	const ay = point?.y ?? view.top + el.clientHeight / 2;
	const before = content?.getBoundingClientRect();
	// Vị trí tương đối (0..1) của anchor trên nội dung
	const fx = before && before.width ? (ax - before.left) / before.width : 0.5;
	const fy = before && before.height ? (ay - before.top) / before.height : 0.5;

	emit('update:zoom', z);
	nextTick(() => {
		const after = content?.getBoundingClientRect();
		if (!after) return;
		el.scrollLeft += after.left + fx * after.width - ax;
		el.scrollTop += after.top + fy * after.height - ay;
	});
}

const zoomBy = (factor: number) => setZoom(props.zoom * factor);

// Lăn chuột (không cần giữ Ctrl) ⇒ phóng to / thu nhỏ quanh con trỏ
function onWheel(e: WheelEvent) {
	if (!props.zoomable) return;
	e.preventDefault();
	setZoom(props.zoom * (e.deltaY < 0 ? props.step : 1 / props.step), { x: e.clientX, y: e.clientY });
}

/* ── Kéo nền ── */
const isPanning = ref(false);
/** Nội dung đang tràn khung ⇒ con trỏ bàn tay. Đo lại khi khung / nội dung đổi cỡ. */
const canPan = ref(false);
let panState: { x: number; y: number; left: number; top: number; moved: boolean } | null = null;
let suppressClick = false;

const isOverflowing = (el: HTMLElement) => el.scrollWidth > el.clientWidth + 1 || el.scrollHeight > el.clientHeight + 1;

function startPan(e: MouseEvent) {
	suppressClick = false;
	const el = scrollRef.value;
	if (!props.zoomable || !el || e.button !== 0 || !isOverflowing(el)) return;
	if (props.panExclude && (e.target as HTMLElement | null)?.closest(props.panExclude)) return;
	panState = { x: e.clientX, y: e.clientY, left: el.scrollLeft, top: el.scrollTop, moved: false };
	document.addEventListener('mousemove', onPanMove);
	document.addEventListener('mouseup', onPanEnd);
}

function onPanMove(e: MouseEvent) {
	const el = scrollRef.value;
	if (!panState || !el) return;
	const dx = e.clientX - panState.x;
	const dy = e.clientY - panState.y;
	if (!panState.moved && Math.abs(dx) + Math.abs(dy) < 4) return;
	panState.moved = true;
	isPanning.value = true;
	el.scrollLeft = panState.left - dx;
	el.scrollTop = panState.top - dy;
}

function onPanEnd() {
	suppressClick = !!panState?.moved;
	panState = null;
	isPanning.value = false;
	document.removeEventListener('mousemove', onPanMove);
	document.removeEventListener('mouseup', onPanEnd);
}

/** Vừa kéo nền xong ⇒ nuốt cú click kết thúc thao tác, để không bỏ chọn / không bấm nhầm phần tử bên dưới. */
function onClickCapture(e: MouseEvent) {
	if (!suppressClick) return;
	suppressClick = false;
	e.stopPropagation();
	e.preventDefault();
}

/* ── Đo tràn khung ── */
let resizeObserver: ResizeObserver | null = null;
let lastSize = { w: -1, h: -1 };
const measure = () => {
	const el = scrollRef.value;
	canPan.value = !!el && isOverflowing(el);
	if (el && (el.offsetWidth !== lastSize.w || el.offsetHeight !== lastSize.h)) {
		lastSize = { w: el.offsetWidth, h: el.offsetHeight };
		emit('resize', lastSize);
	}
};

onMounted(() => {
	resizeObserver = new ResizeObserver(measure);
	if (scrollRef.value) resizeObserver.observe(scrollRef.value);
	const content = anchorEl();
	if (content) resizeObserver.observe(content);
	measure();
});
watch(
	() => props.zoom,
	() => nextTick(measure)
);

onBeforeUnmount(() => {
	resizeObserver?.disconnect();
	onPanEnd();
});

defineExpose({
	/** Phần tử vùng cuộn — nội dung đo kích thước khung / toạ độ thả theo phần tử này. */
	el: scrollRef,
	setZoom,
	zoomBy,
});
</script>

<style scoped lang="scss">
.vsv {
	position: relative;
	width: 100%;
	height: 100%;
	min-height: 0;
}
.vsv-scroll {
	width: 100%;
	height: 100%;
	overflow: auto;
	/* Nội dung con dùng margin: auto để căn giữa khi nhỏ hơn khung, về 0 khi tràn ⇒ cuộn được tới mép trái / trên */
	display: flex;
	flex-direction: column;
}
.vsv.is-pannable .vsv-scroll {
	cursor: grab;
}
.vsv.is-panning .vsv-scroll {
	cursor: grabbing;
	user-select: none;
}

/* Chip chú thích: kính mờ hợp nền tối của sân khấu */
.vsv-guide {
	position: absolute;
	top: 10px;
	left: 12px;
	z-index: 200;
	display: inline-flex;
	align-items: center;
	gap: 5px;
	padding: 3px 10px 3px 8px;
	border: 1px solid var(--wg-line-strong, rgba(150, 190, 255, 0.28));
	border-radius: 999px;
	background: var(--wg-panel, rgba(8, 24, 52, 0.72));
	font-size: 11px;
	color: var(--wg-muted, rgba(215, 230, 250, 0.75));
	cursor: help;
	backdrop-filter: blur(4px);
	transition: color 0.15s, border-color 0.15s;
	&:hover {
		color: #fff;
		border-color: var(--wg-frame, rgba(150, 190, 255, 0.45));
	}
}
.vsv-zoom {
	position: absolute;
	right: 12px;
	bottom: 12px;
	z-index: 200;
	display: flex;
	align-items: center;
	gap: 2px;
	padding: 3px;
	border-radius: 999px;
	background: rgba(8, 24, 52, 0.72);
	border: 1px solid var(--wg-frame, rgba(150, 190, 255, 0.32));
	box-shadow: 0 4px 14px rgba(3, 12, 30, 0.45);
	backdrop-filter: blur(6px);
	opacity: 0;
	transform: translateY(4px);
	transition: opacity 0.18s, transform 0.18s;
}
.vsv:hover .vsv-zoom,
.vsv-zoom.is-active {
	opacity: 1;
	transform: none;
}
.vsv-zoom-btn,
.vsv-zoom-value {
	height: 24px;
	border: none;
	border-radius: 999px;
	background: transparent;
	color: #e8f0fb;
	cursor: pointer;
	display: inline-flex;
	align-items: center;
	justify-content: center;
	transition: background-color 0.15s, color 0.15s;
	&:hover:not(:disabled) {
		background: rgba(96, 156, 255, 0.22);
		color: #fff;
	}
	&:disabled {
		cursor: default;
		color: var(--wg-muted, rgba(215, 230, 250, 0.75));
		opacity: 0.5;
	}
}
.vsv-zoom-btn {
	width: 24px;
	font-size: 13px;
}
.vsv-zoom-value {
	min-width: 48px;
	padding: 0 6px;
	font-size: 11px;
	font-weight: 600;
	font-variant-numeric: tabular-nums;
	/* Ở 100% không bấm được nhưng vẫn đọc rõ số */
	&:disabled {
		opacity: 1;
	}
}
</style>
