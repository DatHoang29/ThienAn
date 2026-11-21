<template>
	<!-- Chọn lưới cột × hàng kiểu "Chèn bảng" của Word: rê / kéo trên ma trận ô rồi thả (hoặc bấm) để chọn -->
	<!-- Popover rộng cố định = ô "Lưới" của panel, mở thẳng xuống dưới ⇒ không tràn sang canvas -->
	<el-popover v-model:visible="state.open" placement="bottom" trigger="click" :width="POPOVER_W" :disabled="props.disabled" popper-class="gsp-popper"
		@show="syncInput" @hide="(state.hover = null), (state.dragging = false)">
		<template #reference>
			<button type="button" class="gsp-trigger" :class="{ 'is-disabled': props.disabled }" :disabled="props.disabled">
				<span class="gsp-trigger-preview" :style="{ gridTemplateColumns: `repeat(${props.cols}, 1fr)`, aspectRatio: previewRatio }">
					<i v-for="n in Math.min(props.cols * props.rows, 512)" :key="n" />
				</span>
				<span class="gsp-trigger-text">{{ $t('lz.label.videoWall.gridColsRows', { cols: props.cols, rows: props.rows }) }}</span>
				<el-icon class="gsp-trigger-arrow"><ele-ArrowDown /></el-icon>
			</button>
		</template>

		<div class="gsp">
			<!-- Đầu popover: lưới đang rê · số màn (trái) — nút Áp dụng (phải); lỗi xuống dòng riêng -->
			<div class="gsp-head">
				<span class="gsp-head-size">
					<b>{{ $t('lz.label.videoWall.gridColsRows', { cols: preview.cols, rows: preview.rows }) }}</b>
					<span>· {{ $t('lz.label.videoWall.gridScreenCount', { count: preview.cols * preview.rows }) }}</span>
				</span>
				<el-button size="small" type="primary" :disabled="!!reason(state.inputCols, state.inputRows)" @click="applyInput">{{ $t('lz.button.videoWall.apply') }}</el-button>
			</div>
			<!-- Nhập thẳng số cột × hàng (chặn theo giới hạn khối / số màn); ma trận nở theo, Enter hoặc Áp dụng để chốt -->
			<div class="gsp-input">
				<el-input-number v-model="state.inputCols" :min="1" :max="capCols" size="small" controls-position="right" @change="onInput" @keyup.enter="applyInput" />
				<span class="gsp-input-x">{{ $t('lz.label.videoWall.gridCols') }} ×</span>
				<el-input-number v-model="state.inputRows" :min="1" :max="capRows" size="small" controls-position="right" @change="onInput" @keyup.enter="applyInput" />
				<span class="gsp-input-x">{{ $t('lz.label.videoWall.gridRows') }}</span>
			</div>
			<div class="gsp-warn" :class="{ 'is-visible': !!previewReason }">{{ previewReason ? reasonText(previewReason) : '&nbsp;' }}</div>
		
			<div class="gsp-grid" :style="{ gridTemplateColumns: `repeat(${shownCols}, minmax(0, ${CELL_MAX}px))` }" @pointerleave="onLeave" @pointerup="onUp">
				<template v-for="r in shownRows" :key="`r-${r}`">
					<span v-for="c in shownCols" :key="`c-${r}-${c}`" class="gsp-cell"
						:class="{
							'is-off': reason(c, r) === 'over',
							'is-target': props.exactCount != null && c * r === props.exactCount,
							'is-current': c <= props.cols && r <= props.rows,
							'is-on': c <= preview.cols && r <= preview.rows,
							'is-bad': !!previewReason && c <= preview.cols && r <= preview.rows,
						}"
						@pointerenter="onEnter(c, r)" @pointerdown.prevent="onDown(c, r)" />
				</template>
			</div>
			<div class="gsp-foot">{{ footText }}</div>
		</div>
	</el-popover>
</template>

<script lang="ts" setup name="videoWallGridSizePicker">
import { computed, reactive } from 'vue';
import { useI18n } from 'vue-i18n';
import { cellRatioParts } from '/@/stores/its/videoWallTopology';

const { t: $t } = useI18n();

/** Bề rộng popover (px) — bằng ô "Lưới" trong panel phải của trang Màn hình. */
const POPOVER_W = 280;
/** Cạnh tối đa 1 ô của ma trận (px); ma trận rộng thì ô tự co cho vừa popover. */
const CELL_MAX = 24;
/** Ma trận tối thiểu luôn hiện; rê tới mép thì nở thêm 1 hàng / cột (như Word) tới giới hạn. */
const MIN_COLS = 10;
const MIN_ROWS = 6;

interface Props {
	cols: number;
	rows: number;
	/** Giới hạn lưới 1 khối — khớp BE VwWallProfile.MaxBlockCols / MaxBlockRows. */
	maxCols?: number;
	maxRows?: number;
	/** Lưới phải có đúng số màn này (tường 1 khối: lưới = tổng màn). */
	exactCount?: number | null;
	/** Số màn tối đa khối được nhận (nhiều khối: phần còn lại sau khi trừ các khối khác, khối bù còn ≥ 2). */
	maxCount?: number | null;
	disabled?: boolean;
	/** Tỉ lệ ô (VD "3:4") cho hình thu nhỏ của nút chọn. */
	cellRatio?: string | null;
}
const props = withDefaults(defineProps<Props>(), { maxCols: 32, maxRows: 16, exactCount: null, maxCount: null, disabled: false, cellRatio: null });

/** Hình thu nhỏ theo tỉ lệ ô (mặc định 16:9). */
const previewRatio = computed(() => {
	const r = cellRatioParts(props.cellRatio) ?? { rw: 16, rh: 9 };
	return `${props.cols * r.rw} / ${props.rows * r.rh}`;
});
const emit = defineEmits<{ (e: 'change', cols: number, rows: number): void }>();

/** Lý do không chọn được lưới cols × rows (null = chọn được). Tổng màn cố định ⇒ lưới phụ thuộc vào nó. */
type Reason = 'over' | 'exact';
function reason(cols: number, rows: number): Reason | null {
	const n = cols * rows;
	if (props.exactCount != null) return n > props.exactCount ? 'over' : n !== props.exactCount ? 'exact' : null;
	if (props.maxCount != null && n > props.maxCount) return 'over';
	return null;
}
/** Số màn tối đa của 1 lưới ⇒ ma trận không cần rộng / cao hơn mức này. */
const limitCount = computed(() => props.exactCount ?? props.maxCount ?? props.maxCols * props.maxRows);
const reasonText = (r: Reason) =>
	r === 'over' ? $t('lz.label.videoWall.gridOverLimit', { max: limitCount.value }) : $t('lz.label.videoWall.gridMustEqualTotal', { total: props.exactCount });
const footText = computed(() =>
	props.exactCount != null
		? $t('lz.label.videoWall.gridPickerHintExact', { total: props.exactCount })
		: $t('lz.label.videoWall.gridPickerHint', { max: limitCount.value })
);

const state = reactive({
	open: false,
	/** Ô đang rê chuột tới (góc dưới phải của vùng chọn). */
	hover: null as null | { cols: number; rows: number },
	/** Đang giữ chuột kéo chọn. */
	dragging: false,
	/** Ô nhập số cột / hàng. */
	inputCols: 1,
	inputRows: 1,
});

const preview = computed(() => state.hover ?? { cols: props.cols, rows: props.rows });
const previewReason = computed(() => (state.hover ? reason(state.hover.cols, state.hover.rows) : null));

// Ma trận hiện rộng hơn vùng đang chọn / đang rê 1 ô để luôn kéo ra thêm được; không vượt giới hạn khối
// và không vượt số màn cho phép (1 hàng chứa tối đa limitCount cột, 1 cột tối đa limitCount hàng)
const capCols = computed(() => Math.max(1, Math.min(props.maxCols, limitCount.value)));
const capRows = computed(() => Math.max(1, Math.min(props.maxRows, limitCount.value)));
const shownCols = computed(() => Math.min(capCols.value, Math.max(MIN_COLS, props.cols + 1, (state.hover?.cols ?? 0) + 1)));
const shownRows = computed(() => Math.min(capRows.value, Math.max(MIN_ROWS, props.rows + 1, (state.hover?.rows ?? 0) + 1)));

function onEnter(cols: number, rows: number) {
	state.hover = { cols, rows };
	state.inputCols = cols;
	state.inputRows = rows;
}

/** Mở popover ⇒ ô nhập bắt đầu từ lưới đang dùng. */
function syncInput() {
	state.inputCols = props.cols;
	state.inputRows = props.rows;
}

/** Gõ số ⇒ xem trước trên ma trận như khi rê chuột. */
function onInput() {
	state.hover = { cols: state.inputCols, rows: state.inputRows };
}

function applyInput() {
	state.hover = { cols: state.inputCols, rows: state.inputRows };
	onUp();
}

function onDown(cols: number, rows: number) {
	state.dragging = true;
	state.hover = { cols, rows };
}

/** Thả chuột (sau khi kéo hoặc bấm) ⇒ chốt lưới; lưới không hợp lệ thì bỏ qua, giữ popover để chọn lại. */
function onUp() {
	state.dragging = false;
	const h = state.hover;
	if (!h || reason(h.cols, h.rows)) return;
	if (h.cols !== props.cols || h.rows !== props.rows) emit('change', h.cols, h.rows);
	state.open = false;
}

function onLeave() {
	if (!state.dragging) state.hover = null;
}
</script>

<style scoped lang="scss">
.gsp-trigger {
	display: flex;
	align-items: center;
	gap: 8px;
	width: 100%;
	height: 32px;
	padding: 0 10px;
	border: 1px solid var(--el-border-color);
	border-radius: var(--el-border-radius-base);
	background: var(--el-fill-color-blank);
	color: var(--el-text-color-regular);
	font-size: 13px;
	cursor: pointer;
	&:hover:not(.is-disabled) {
		border-color: var(--el-color-primary);
	}
	&.is-disabled {
		cursor: not-allowed;
		background: var(--el-disabled-bg-color);
		color: var(--el-disabled-text-color);
	}
}
.gsp-trigger-preview {
	display: grid;
	gap: 1px;
	height: 18px;
	max-width: 56px;
	padding: 1px;
	border-radius: 2px;
	background: #13305c;
	flex-shrink: 0;
	i {
		display: block;
		border-radius: 1px;
		background: #4f86d4;
	}
}
.gsp-trigger-text {
	flex: 1;
	text-align: left;
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
}
.gsp-trigger-arrow {
	color: var(--el-text-color-placeholder);
}

.gsp {
	display: flex;
	flex-direction: column;
	gap: 6px;
	user-select: none;
}
.gsp-head {
	display: flex;
	align-items: center;
	justify-content: space-between;
	gap: 8px;
	font-size: 13px;
	color: var(--el-text-color-primary);
	.el-button {
		flex-shrink: 0;
	}
}
.gsp-head-size {
	min-width: 0;
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
	> span {
		margin-left: 4px;
		color: var(--el-text-color-secondary);
	}
}
.gsp-head-limit {
	flex-shrink: 0;
}
/* Dòng lỗi luôn giữ chỗ ⇒ ma trận không nhảy lên xuống khi rê qua ô sai */
.gsp-input {
	display: flex;
	align-items: center;
	gap: 4px;
	.el-input-number {
		width: 64px;
	}
	.gsp-input-x {
		font-size: 12px;
		color: var(--el-text-color-secondary);
		white-space: nowrap;
	}
}
.gsp-warn {
	min-height: 16px;
	font-size: 12px;
	line-height: 16px;
	color: var(--el-color-danger);
	visibility: hidden;
	&.is-visible {
		visibility: visible;
	}
}
.gsp-grid {
	display: grid;
	justify-content: center;
	gap: 3px;
	touch-action: none;
}
.gsp-cell {
	display: block;
	aspect-ratio: 1;
	border: 1px solid var(--el-border-color);
	border-radius: 2px;
	background: var(--el-fill-color-blank);
	cursor: pointer;
	/* Lưới đang dùng: viền đậm để so với vùng đang rê */
	&.is-current {
		border-color: var(--el-color-primary-light-5);
		background: var(--el-color-primary-light-9);
	}
	/* Góc vượt số màn cho phép: xám, không chọn được */
	&.is-off {
		border-color: var(--el-border-color-lighter);
		background: var(--el-fill-color);
		cursor: not-allowed;
	}
	/* 1 khối: các lưới đúng bằng tổng màn */
	&.is-target {
		background-image: radial-gradient(circle, var(--el-color-success) 2px, transparent 2.5px);
	}
	&.is-on {
		border-color: var(--el-color-primary);
		background-color: var(--el-color-primary-light-5);
	}
	&.is-bad {
		border-color: var(--el-color-danger);
		background: var(--el-color-danger-light-7);
	}
}
.gsp-foot {
	padding-top: 6px;
	border-top: 1px solid var(--el-border-color-lighter);
	font-size: 11px;
	line-height: 1.5;
	color: var(--el-text-color-secondary);
}
</style>
