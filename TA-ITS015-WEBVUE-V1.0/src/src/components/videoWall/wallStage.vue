<template>
	<div class="ws" :style="{ '--cell-w': `${cellW}px`, '--cell-h': `${cellH}px` }">
		<StageViewport ref="viewportRef" v-model:zoom="zoom" :zoomable="zoomable" :min="ZOOM_MIN" :max="ZOOM_MAX" :step="ZOOM_STEP" anchor=".ws-wall"
			:pan-exclude="panExclude" :guide="guide" :guide-label="guideLabel" @resize="(s) => (viewSize = s)" @background-click="emit('backgroundClick')">
			<div class="ws-pad" :style="{ padding: `${PAD_TOP}px ${PAD_X}px ${PAD_BOTTOM}px` }" @click="onPadClick">
				<div class="ws-wall" :style="{ width: `${wallSize.w}px`, height: `${wallSize.h}px` }">
					<div v-for="b in layout" :key="b.blockNo" class="ws-block" :style="{ left: `${b.left}px`, width: `${b.w}px`, height: `${b.h}px` }">
						<!-- Tên khối (+ thông tin thêm) -->
						<div v-if="headH" class="ws-block-head" :style="{ height: `${headH}px` }">
							<span v-if="showBlockName" class="ws-block-name">{{ $t('lz.label.videoWall.blockName', { name: blockLabel(b.blockNo) }) }}</span>
							<slot name="block-meta" :block-no="b.blockNo" :cols="b.cols" :rows="b.rows" />
						</div>

						<!-- Thước cột / hàng -->
						<template v-if="rulers">
							<span v-for="c in b.cols" :key="`rc-${c}`" class="ws-ruler" :class="{ 'is-clickable': rulerClickable }"
								:title="rulerClickable ? $t('lz.label.videoWall.toggleColumn') : $t('lz.label.videoWall.colN', { n: c })"
								:style="{ left: `${rulerSize + (c - 1) * (cellW + gap)}px`, top: `${headH}px`, width: `${cellW}px`, height: `${rulerSize}px` }"
								@click.stop="rulerClickable && emit('rulerClick', b.blockNo, 'col', c - 1)">C{{ c }}</span>
							<span v-for="r in b.rows" :key="`rr-${r}`" class="ws-ruler" :class="{ 'is-clickable': rulerClickable }"
								:title="rulerClickable ? $t('lz.label.videoWall.toggleRow') : $t('lz.label.videoWall.rowN', { n: r })"
								:style="{ left: 0, top: `${headH + rulerSize + (r - 1) * (cellH + gap)}px`, width: `${rulerSize}px`, height: `${cellH}px` }"
								@click.stop="rulerClickable && emit('rulerClick', b.blockNo, 'row', r - 1)">H{{ r }}</span>
						</template>

						<!-- Lưới ô + lớp trên; data-block để trang dùng quy đổi toạ độ thả theo khối -->
						<div class="ws-area" :class="{ 'is-framed': framed }" :data-block="b.blockNo"
							:style="{ left: `${rulerSize}px`, top: `${headH + rulerSize}px`, width: `${b.areaW}px`, height: `${b.areaH}px` }">
							<template v-for="r in b.rows" :key="`r-${r}`">
								<div v-for="c in b.cols" :key="`c-${r}-${c}`" class="ws-cell"
									:style="{ left: `${(c - 1) * (cellW + gap)}px`, top: `${(r - 1) * (cellH + gap)}px`, width: `${cellW}px`, height: `${cellH}px` }">
									<slot name="cell" :block-no="b.blockNo" :row="r - 1" :col="c - 1" :index="b.firstIndex + (r - 1) * b.cols + (c - 1)" />
								</div>
							</template>
							<slot name="overlay" :block-no="b.blockNo" :scale="scale" :scale-y="scaleY" />
						</div>
					</div>
				</div>
			</div>
		</StageViewport>
	</div>
</template>

<script lang="ts" setup name="videoWallWallStage">
import { computed, ref, watch } from 'vue';
import { blockLabel, cellRatioParts } from '/@/stores/its/videoWallTopology';
import StageViewport from './stageViewport.vue';

export interface WallStageBlock {
	blockNo: number;
	cols: number;
	rows: number;
}

interface Props {
	blocks: WallStageBlock[];
	/** Cỡ 1 màn theo pixel logic. Lớp #overlay vẽ theo đơn vị này × scale (ngang) / scaleY (dọc). */
	panelW?: number;
	panelH?: number;
	/** Tỉ lệ ô khi vẽ (VD "3:4"); bỏ trống ⇒ theo panelW : panelH. Ô khác tỉ lệ đơn vị ⇒ trục dọc co giãn riêng (scaleY). */
	cellRatio?: string | null;
	/** Khe giữa 2 màn (px màn hình, không nhân zoom) — 0 = tường liền như thực tế. */
	gap?: number;
	/** Thước C1.. / H1.. quanh lưới. */
	rulers?: boolean;
	/** Bấm thước ⇒ emit rulerClick (vd Phân quyền: chọn / bỏ cả cột / hàng). */
	rulerClickable?: boolean;
	/** Tên khối: 'auto' chỉ khi nhiều khối, 'always' luôn hiện (kèm #block-meta). */
	blockName?: 'auto' | 'always';
	/** Viền + bóng quanh lưới mỗi khối (tường liền). */
	framed?: boolean;
	/** Giới hạn bề rộng 1 ô ở mức vừa khung (px); zoom vẫn phóng vượt mức tối đa. */
	minCellW?: number;
	maxCellW?: number;
	zoomable?: boolean;
	/** Bấm giữ trên phần tử khớp selector thì không kéo nền (phần tử có thao tác kéo riêng). */
	panExclude?: string;
	/** Chú thích ở chip góc trên trái (xem StageViewport); bỏ trống ⇒ không hiện. */
	guide?: string;
	guideLabel?: string;
}
const props = withDefaults(defineProps<Props>(), {
	panelW: 16,
	panelH: 9,
	cellRatio: null,
	gap: 0,
	rulers: false,
	rulerClickable: false,
	blockName: 'auto',
	framed: false,
	minCellW: 0,
	maxCellW: Number.POSITIVE_INFINITY,
	zoomable: true,
	panExclude: '',
	guide: '',
	guideLabel: '',
});
const emit = defineEmits<{
	/** Click nền sân khấu (không trúng ô / phần tử lớp trên, không tính click kết thúc thao tác kéo). */
	(e: 'backgroundClick'): void;
	/** Bấm thước cột / hàng (chỉ khi rulerClickable); index tính từ 0. */
	(e: 'rulerClick', blockNo: number, type: 'col' | 'row', index: number): void;
}>();

const ZOOM_MIN = 1;
const ZOOM_MAX = 5;
const ZOOM_STEP = 1.25;
/** Lề quanh tường, chiều cao dòng tên khối, cỡ thước, khoảng cách tối thiểu giữa 2 khối (px màn hình). */
const PAD_X = 16;
const PAD_TOP = 12;
const PAD_BOTTOM = 16;
const HEAD = 22;
const RULER = 18;
const BLOCK_GAP_MIN = 20;

const viewportRef = ref<InstanceType<typeof StageViewport>>();
const viewSize = ref({ w: 0, h: 0 });
const zoom = ref(ZOOM_MIN);

const showBlockName = computed(() => props.blockName === 'always' || props.blocks.length > 1);
const headH = computed(() => (showBlockName.value ? HEAD : 0));
const rulerSize = computed(() => (props.rulers ? RULER : 0));

/** Chiều cao ô khi vẽ, theo đơn vị ngang (pixel logic): có tỉ lệ ô ⇒ panelW × cao / rộng, không có ⇒ panelH. */
const shapeH = computed(() => {
	const r = cellRatioParts(props.cellRatio);
	return r ? (props.panelW * r.rh) / r.rw : props.panelH;
});

/**
 * Tỉ lệ vừa khung (px màn hình / px logic): lớn nhất mà cả chiều ngang (các khối xếp cạnh nhau) lẫn chiều dọc đều vừa.
 * Khoảng cách khối = 1/4 ô (tối thiểu BLOCK_GAP_MIN) nên giải 2 lần nếu chạm mức tối thiểu.
 */
const fitScale = computed(() => {
	const { w, h } = viewSize.value;
	const blocks = props.blocks.filter((b) => b.cols > 0 && b.rows > 0);
	if (!w || !h || !blocks.length) return 1 / props.panelW;
	const pw = props.panelW;
	const ph = shapeH.value;
	const n = blocks.length;
	const availW = w - PAD_X * 2;
	const availH = h - PAD_TOP - PAD_BOTTOM;
	const cols = blocks.reduce((t, b) => t + b.cols, 0);
	const fixedW = blocks.reduce((t, b) => t + rulerSize.value + (b.cols - 1) * props.gap, 0);
	let byW = (availW - fixedW) / (cols * pw + ((n - 1) * pw) / 4);
	if ((byW * pw) / 4 < BLOCK_GAP_MIN) byW = (availW - fixedW - (n - 1) * BLOCK_GAP_MIN) / (cols * pw);
	const rows = Math.max(...blocks.map((b) => b.rows));
	const byH = (availH - headH.value - rulerSize.value - (rows - 1) * props.gap) / (rows * ph);
	const cell = Math.min(props.maxCellW, Math.max(props.minCellW, Math.min(byW, byH) * pw));
	return Math.max(cell, 1) / pw;
});

/** Tỉ lệ vẽ trục ngang = vừa khung × zoom (zoom 1 = vừa khung). */
const scale = computed(() => fitScale.value * zoom.value);
const cellW = computed(() => props.panelW * scale.value);
const cellH = computed(() => shapeH.value * scale.value);
/** Tỉ lệ vẽ trục dọc (px màn hình / px logic): bằng scale khi ô cùng tỉ lệ với đơn vị panelW : panelH. */
const scaleY = computed(() => cellH.value / props.panelH);

const layout = computed(() => {
	const blockGap = Math.max(BLOCK_GAP_MIN, cellW.value / 4);
	let left = 0;
	let firstIndex = 0;
	return props.blocks.map((b) => {
		const areaW = Math.max(0, b.cols * cellW.value + (b.cols - 1) * props.gap);
		const areaH = Math.max(0, b.rows * cellH.value + (b.rows - 1) * props.gap);
		const block = {
			...b,
			left,
			firstIndex,
			areaW,
			areaH,
			w: rulerSize.value + areaW,
			h: headH.value + rulerSize.value + areaH,
		};
		left += block.w + blockGap;
		firstIndex += b.cols * b.rows;
		return block;
	});
});

const wallSize = computed(() => {
	const last = layout.value[layout.value.length - 1];
	return { w: last ? last.left + last.w : 0, h: Math.max(0, ...layout.value.map((b) => b.h)) };
});

// Đổi bố cục tường (số khối / cột / hàng) ⇒ về vừa khung
watch(
	() => props.blocks.map((b) => `${b.blockNo}:${b.cols}x${b.rows}`).join('|'),
	() => (zoom.value = ZOOM_MIN)
);

/** Click vào khoảng trống trong tường (không phải ô) cũng là click nền. Phần tử lớp trên tự chặn click (stop). */
function onPadClick(e: MouseEvent) {
	if (!(e.target as HTMLElement | null)?.closest('.ws-cell')) emit('backgroundClick');
}

defineExpose({
	/** Phần tử vùng cuộn (khung xem) — đo vị trí con trỏ so với khung. */
	el: computed(() => viewportRef.value?.el as HTMLElement | undefined),
	/** px màn hình / px logic hiện tại (đã nhân zoom) — trục ngang. */
	scale,
	/** Như scale nhưng trục dọc (khác scale khi tỉ lệ ô khác panelW : panelH). */
	scaleY,
	cellW,
	cellH,
	zoom,
});
</script>

<style scoped lang="scss">
@import '/@/theme/its/videoWallStage.scss';

.ws {
	@include vw-stage;
	width: 100%;
	height: 100%;
	min-height: 0;
}
/* margin auto: căn giữa khi tường nhỏ hơn khung, về 0 khi tràn ⇒ cuộn được tới cả mép trái / trên */
.ws-pad {
	margin: auto;
	flex-shrink: 0;
}
.ws-wall {
	position: relative;
}
.ws-block {
	position: absolute;
	top: 0;
}
.ws-block-head {
	display: flex;
	align-items: baseline;
	gap: 8px;
	white-space: nowrap;
	overflow: hidden;
	.ws-block-name {
		font-size: 13px;
		font-weight: 600;
		color: #fff;
	}
	:deep(.ws-block-meta) {
		font-size: 12px;
		color: var(--wg-muted);
	}
}
.ws-ruler {
	position: absolute;
	display: flex;
	align-items: center;
	justify-content: center;
	font-size: 10px;
	color: var(--wg-muted);
	font-variant-numeric: tabular-nums;
	user-select: none;
	&.is-clickable {
		border-radius: 3px;
		cursor: pointer;
		&:hover {
			color: #fff;
			background: rgba(59, 140, 255, 0.25);
		}
	}
}
.ws-area {
	position: absolute;
	&.is-framed {
		border-radius: 2px;
		box-shadow: 0 0 0 1px var(--wg-frame), 0 4px 16px rgba(3, 12, 30, 0.45);
	}
}
.ws-cell {
	position: absolute;
	box-sizing: border-box;
}
</style>
