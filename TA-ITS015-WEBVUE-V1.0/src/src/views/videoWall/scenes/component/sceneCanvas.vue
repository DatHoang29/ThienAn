<template>
	<div class="scene-canvas-root" :class="{ 'scene-canvas--readonly': readonly }">
	<!-- Sân khấu dùng chung (nền, khối, ô, zoom, kéo nền): components/videoWall/wallStage.vue. Ở đây chỉ vẽ nhãn ô + cửa sổ. -->
	<WallStage ref="stageRef" class="scene-canvas" :blocks="stageBlocks" :panel-w="panelW" :panel-h="panelH" :cell-ratio="cellRatio" framed rulers :zoomable="zoomable"
		@dragover.prevent="onDragOver" @drop.prevent="onDrop" @background-click="onBackgroundClick">
		<!-- Ô màn hình (nền) -->
		<template #cell="{ blockNo, row, col, index }">
			<div class="scene-canvas-cell" :class="{ 'scene-canvas-cell--denied': isCellDenied(blockNo, row, col) }"
				:title="isCellDenied(blockNo, row, col) ? $t('lz.label.videoWall.outsidePermittedArea') : undefined">
				<span class="scene-canvas-cell-label">{{ cellLabel(blockNo, row, col, index) }}</span>
				<span v-if="screens" class="scene-canvas-cell-pos">H{{ row + 1 }} · C{{ col + 1 }}</span>
			</div>
		</template>

		<!-- Cửa sổ của khối (toạ độ pixel logic trong khối × scale) -->
		<template #overlay="{ blockNo }">
				<!-- Tooltip thông tin cửa sổ (nguồn · bộ điều khiển · vị trí · kích thước); tắt khi đang kéo / đổi kích thước -->
				<el-tooltip v-for="win in windowsOf(blockNo)" :key="win.id!" placement="top" :show-after="300" :enterable="false" popper-class="vw-info-tip"
					:disabled="interacting || !getWindowLabel(win)">
				<template #content>
					<VwInfoTip :title="getWindowLabel(win)" :swatch="getWindowColor(win.sourceId)" :rows="windowTipRows(win)" />
				</template>
				<div
					:data-win-id="win.id!"
					class="scene-window"
					:class="{
						'scene-window--selected': win.id === selectedWindowId,
						'scene-window--locked': win.locked,
						'scene-window--denied': !readonly && isWinDenied(win),
						'scene-window--covering': overlapState.covering.has(win.id),
						'scene-window--same-color': overlapState.sameColor.has(win.id),
						'scene-window--removing': win.id === removingWinId,
						'scene-window--compact': isCompact(win),
					}"
					:style="getWindowStyle(win)"
					@mousedown.stop="startInteraction($event, win)"
					@click.stop="!readonly && emit('selectWindow', win.id ?? '')"
				>
					<!-- Content: cửa sổ quá nhỏ (theo kích thước đang vẽ) ⇒ chỉ icon, tên nguồn xem qua tooltip (title) -->
					<div class="scene-window-content">
						<div class="scene-window-icon">
							<el-icon v-if="getSourceType(win.sourceId) === VideoWallSourceTypeEnum.LocalSignal" :size="iconSize(win)"><ele-Monitor /></el-icon>
							<el-icon v-else-if="getSourceType(win.sourceId) === VideoWallSourceTypeEnum.IpStream" :size="iconSize(win)"><ele-Camera /></el-icon>
							<el-icon v-else :size="iconSize(win)"><ele-Picture /></el-icon>
						</div>
						<span v-if="!isCompact(win)" class="scene-window-label">{{ getWindowLabel(win) }}</span>
					</div>

					<!-- Resize handles (only for selected window) -->
					<template v-if="!readonly && win.id === selectedWindowId && !win.locked && !isWinDenied(win)">
						<div class="resize-handle resize-handle-n" @mousedown.stop="startResize($event, win, 'n')"></div>
						<div class="resize-handle resize-handle-s" @mousedown.stop="startResize($event, win, 's')"></div>
						<div class="resize-handle resize-handle-e" @mousedown.stop="startResize($event, win, 'e')"></div>
						<div class="resize-handle resize-handle-w" @mousedown.stop="startResize($event, win, 'w')"></div>
						<div class="resize-handle resize-handle-ne" @mousedown.stop="startResize($event, win, 'ne')"></div>
						<div class="resize-handle resize-handle-nw" @mousedown.stop="startResize($event, win, 'nw')"></div>
						<div class="resize-handle resize-handle-se" @mousedown.stop="startResize($event, win, 'se')"></div>
						<div class="resize-handle resize-handle-sw" @mousedown.stop="startResize($event, win, 'sw')"></div>
					</template>

					<!-- Lock indicator -->
					<div v-if="win.locked" class="scene-window-lock">
						<el-icon size="12"><ele-Lock /></el-icon>
					</div>
				</div>
				</el-tooltip>
		</template>
	</WallStage>

	<!-- Đang kéo cửa sổ ra ngoài vùng tường: cửa sổ nhấc theo con trỏ + gợi ý thả vào cột Nguồn tín hiệu để gỡ -->
	<div v-if="dragOut && !dragOut.overZone" class="scene-canvas-remove-hint">
		<el-icon><ele-Delete /></el-icon>
		<span>{{ $t('lz.label.videoWall.dropOutsideToRemove') }}</span>
	</div>
	<Teleport to="body">
		<div v-if="dragOut && dragOutWin" class="scene-window scene-window-ghost" :class="{ 'is-over-zone': dragOut.overZone }"
			:style="{ left: `${dragOut.x}px`, top: `${dragOut.y}px`, width: `${dragOut.w}px`, height: `${dragOut.h}px`, backgroundColor: getWindowColor(dragOutWin.sourceId) }">
			<div class="scene-window-content">
				<div class="scene-window-icon">
					<el-icon v-if="getSourceType(dragOutWin.sourceId) === VideoWallSourceTypeEnum.LocalSignal" size="20"><ele-Monitor /></el-icon>
					<el-icon v-else-if="getSourceType(dragOutWin.sourceId) === VideoWallSourceTypeEnum.IpStream" size="20"><ele-Camera /></el-icon>
					<el-icon v-else size="20"><ele-Picture /></el-icon>
				</div>
				<span class="scene-window-label">{{ getWindowLabel(dragOutWin) }}</span>
			</div>
		</div>
	</Teleport>
	</div>
</template>

<script lang="ts" setup name="videoWallSceneCanvas">
import { ref, computed, onMounted, onUnmounted } from 'vue';
import { ElMessage } from 'element-plus';
import { useVideoWallMyPermission } from '/@/stores/its/videoWallMyPermission';
import { useVideoWallTopology } from '/@/stores/its/videoWallTopology';
import type { VwSourceOutput, VwScreenOutput, VwWallBlockOutput, VwWallTopologyOutput } from '/@/api-services/videoWall';
import { isWindowVisible, snapToCell, type EditWindow } from '/@/utils/videoWall/sceneWindow';
import { VideoWallSourceTypeEnum } from '/@/types/enums';
import { sourceTypeColor } from '/@/utils/videoWall/sourceColors';
import WallStage, { type WallStageBlock } from '/@/components/videoWall/wallStage.vue';
import VwInfoTip, { type VwTipRow } from '/@/components/videoWall/vwInfoTip.vue';
import { useI18n } from 'vue-i18n';
import { useBaseConfig } from '/@/stores/baseConfig';
import { BaseConfigTypeEnum } from '/@/types/enums';
import { useVideoWallController } from '/@/stores/its/videoWallController';
import { blockLabel } from '/@/stores/its/videoWallTopology';

const { t: $t } = useI18n();
const baseConfig = useBaseConfig();
const vwCtrl = useVideoWallController();

interface Props {
	windows: EditWindow[];
	selectedWindowId: string;
	sources: VwSourceOutput[];
	/**
	 * Chế độ chỉ xem: tắt chọn cửa sổ, kéo/di chuyển, resize, thả nguồn và phím Delete.
	 *
	 * Trang Monitor dùng chế độ này vì BE chưa có endpoint "áp dụng layout xuống tường"
	 * (chỉ có `POST /vwscene/active` kích hoạt cả kịch bản) ⇒ cho sửa layout ở đó sẽ tạo
	 * thay đổi không bao giờ gửi được xuống dưới. Sửa layout thực hiện ở trang Kịch bản.
	 */
	readonly?: boolean;
	/**
	 * Màn hình (VwScreen) để đặt nhãn ô theo màn ở đúng (blockNo, gridCol, gridRow), giống lưới Phân quyền.
	 * Không truyền thì giữ nhãn đánh số SCR-xx.
	 */
	screens?: VwScreenOutput[];
	/**
	 * Bố cục để vẽ (bố cục mà kịch bản được dựng trên). Không truyền thì dùng bố cục đang dùng (store).
	 * Kịch bản thuộc phiên bản bố cục cũ phải vẽ theo bố cục của nó, toạ độ cửa sổ mới khớp.
	 */
	layout?: VwWallTopologyOutput | null;
	/** Cho phóng to / thu nhỏ (nút ở góc, lăn chuột) và kéo nền để di chuyển khi đã phóng to. */
	zoomable?: boolean;
}
const props = withDefaults(defineProps<Props>(), {
	readonly: false,
	layout: null,
	zoomable: false,
});
const emit = defineEmits<{
	(e: 'selectWindow', id: string): void;
	(e: 'updateWindow', win: EditWindow): void;
	(e: 'deleteWindow', id: string): void;
	(e: 'dropSource', sourceId: string, blockNo: number, x: number, y: number): void;
}>();

const stageRef = ref<InstanceType<typeof WallStage>>();
/** Khung xem của sân khấu: đo con trỏ ra ngoài canvas, tìm phần tử cửa sổ để kéo mượt. */
const canvasRef = computed(() => stageRef.value?.el as HTMLElement | undefined);

// Phân quyền vùng màn hình: làm mờ ô ngoài vùng, khoá cửa sổ ngoài vùng, chặn kéo / resize ra ngoài vùng.
// Chỉ hỗ trợ giao diện — BE vẫn kiểm tra khi lưu.
const myPerm = useVideoWallMyPermission();
const isWinDenied = (win: EditWindow) => !myPerm.isRectAllowed(win.blockNo, win.x, win.y, win.w, win.h);
const isCellDenied = (blockNo: number, row: number, col: number) => myPerm.loaded && !myPerm.isCellAllowed(blockNo, col, row);

// Bố cục tường: các khối xếp ngang, toạ độ cửa sổ tính trong khối.
const topology = useVideoWallTopology();
const panelW = computed(() => props.layout?.screenWidth || topology.panelW);
const panelH = computed(() => props.layout?.screenHeight || topology.panelH);
/** Hình dạng ô khi vẽ — toạ độ cửa sổ vẫn theo panelW × panelH nên trục dọc co giãn riêng (scaleFactorY). */
const cellRatio = computed(() => props.layout?.cellRatio || topology.cellRatio);
const sourceBlocks = computed<VwWallBlockOutput[]>(() =>
	props.layout?.blocks?.length ? [...props.layout.blocks].sort((a, b) => (a.blockNo ?? 0) - (b.blockNo ?? 0)) : topology.blocks
);

/** Khối cho sân khấu dùng chung (WallStage tự xếp vị trí / tỉ lệ / zoom). */
const stageBlocks = computed<WallStageBlock[]>(() => sourceBlocks.value.map((b) => ({ blockNo: b.blockNo ?? 0, cols: b.cols ?? 0, rows: b.rows ?? 0 })));

/** Kích thước khối theo pixel logic — giới hạn kéo / resize cửa sổ trong khối. */
const blockOf = (blockNo: number) => {
	const b = stageBlocks.value.find((x) => x.blockNo === blockNo);
	return b ? { ...b, w: b.cols * panelW.value, h: b.rows * panelH.value } : undefined;
};

/** px màn hình / px logic (vừa khung × zoom) do WallStage tính — trục ngang / trục dọc (khác nhau khi ô không cùng tỉ lệ đơn vị). */
const scaleFactor = computed(() => stageRef.value?.scale ?? 1);
const scaleFactorY = computed(() => stageRef.value?.scaleY ?? scaleFactor.value);

function onBackgroundClick() {
	if (!props.readonly) emit('selectWindow', '');
}

// Tra màn hình theo vị trí lưới, khoá "block,col,row" — cùng cách tra với lưới Phân quyền
const screenMap = computed(() => {
	const map = new Map<string, VwScreenOutput>();
	for (const s of props.screens ?? []) if (s.gridCol != null && s.gridRow != null) map.set(`${s.blockNo ?? 0},${s.gridCol},${s.gridRow}`, s);
	return map;
});

/** Nhãn ô: tên / mã màn đặt ở ô đó; không truyền danh sách màn thì đánh số SCR-xx liên tục qua các khối. */
function cellLabel(blockNo: number, row: number, col: number, index: number): string {
	if (!props.screens) return `SCR-${String(index + 1).padStart(2, '0')}`;
	const screen = screenMap.value.get(`${blockNo},${col},${row}`);
	return screen?.name || screen?.code || '—';
}

// Sorted windows by zIndex
const sortedWindows = computed(() => {
	return [...props.windows].filter(isWindowVisible).sort((a, b) => a.zIndex - b.zIndex);
});
const windowsOf = (blockNo: number) => sortedWindows.value.filter((w) => w.blockNo === blockNo);

function rectsOverlap(a: EditWindow, b: EditWindow): boolean {
	return a.blockNo === b.blockNo && a.x < b.x + b.w && b.x < a.x + a.w && a.y < b.y + b.h && b.y < a.y + a.h;
}

// Cửa sổ đè lên cửa sổ nằm dưới (đứng trước trong sortedWindows, cùng khối):
//  - covering : làm trong suốt một phần để thấy phía sau
//  - sameColor: có cửa sổ bị đè cùng màu ⇒ trong suốt không phân biệt được, cần viền nổi
const overlapState = computed(() => {
	const list = sortedWindows.value;
	const covering = new Set<string | null | undefined>();
	const sameColor = new Set<string | null | undefined>();
	list.forEach((win, i) => {
		const below = list.slice(0, i).filter((b) => rectsOverlap(win, b));
		if (!below.length) return;
		covering.add(win.id);
		const color = getWindowColor(win.sourceId).toLowerCase();
		if (below.some((b) => getWindowColor(b.sourceId).toLowerCase() === color)) sameColor.add(win.id);
	});
	return { covering, sameColor };
});

// Window style calculation (toạ độ trong khối)
function getWindowStyle(win: EditWindow) {
	const sf = scaleFactor.value;
	const sfy = scaleFactorY.value;
	return {
		left: `${win.x * sf}px`,
		top: `${win.y * sfy}px`,
		width: `${win.w * sf}px`,
		height: `${win.h * sfy}px`,
		zIndex: win.zIndex + 10,
		backgroundColor: getWindowColor(win.sourceId),
	};
}

/* Nền cửa sổ tô theo LOẠI NGUỒN, dùng dải màu nhận diện riêng (không phải màu
   semantic của trạng thái) để không đọc nhầm thành online/offline.
   Dùng màu ĐẬM vì nội dung cửa sổ (icon + nhãn) là chữ trắng — nền nhạt sẽ làm chữ bị nhòe. */
function getWindowColor(sourceId?: string | null): string {
	const source = props.sources.find((s) => s.id === sourceId || s.code === sourceId);
	return sourceTypeColor(source?.sourceType);
}

function getSourceType(sourceId?: string | null): string {
	const source = props.sources.find((s) => s.id === sourceId || s.code === sourceId);
	return source?.sourceType ?? '';
}

/** Đang kéo / đổi kích thước cửa sổ ⇒ tắt tooltip để không che thao tác. */
const interacting = ref(false);

/** Kích thước tối thiểu (px đang vẽ, đã nhân zoom) để hiện cả icon + nhãn; nhỏ hơn ⇒ chỉ icon. */
const COMPACT_MIN_W = 90;
const COMPACT_MIN_H = 48;

const isCompact = (win: EditWindow) => win.w * scaleFactor.value < COMPACT_MIN_W || win.h * scaleFactorY.value < COMPACT_MIN_H;

/** Icon thu theo cửa sổ: bình thường 20px, cửa sổ gọn 16px, rất nhỏ (< 28px) 12px. */
const iconSize = (win: EditWindow) => {
	if (!isCompact(win)) return 20;
	return Math.min(win.w * scaleFactor.value, win.h * scaleFactorY.value) < 28 ? 12 : 16;
};

const sourceOf = (sourceId?: string | null) => props.sources.find((s) => s.id === sourceId || s.code === sourceId);

/** "H1 · C2" hoặc dải "H1–H2 · C1–C3" — các ô màn mà cửa sổ phủ (cùng công thức ô với BE). */
function windowCellRange(win: EditWindow): string {
	const range = (from: number, size: number, cell: number) => {
		const a = Math.floor(from / cell) + 1;
		const b = Math.floor((from + Math.max(size, 1) - 1) / cell) + 1;
		return a === b ? `${a}` : `${a}–${b}`;
	};
	const cells = `H${range(win.y, win.h, panelH.value)} · C${range(win.x, win.w, panelW.value)}`;
	return stageBlocks.value.length > 1 ? `${blockLabel(win.blockNo)} · ${cells}` : cells;
}

/** Số cột × hàng màn (làm tròn 1 chữ số — dữ liệu cũ có thể lệch ô). */
const cellSpan = (size: number, cell: number) => Number((size / cell).toFixed(1));

/**
 * Dòng thông tin trong tooltip cửa sổ — giữ gọn (4 dòng) và chỉ 1 màu ở tiêu đề (= màu loại nguồn của cửa sổ):
 * loại tín hiệu gộp vào dòng Loại nguồn; bộ điều khiển KHÔNG kèm ô màu vì cửa sổ không tô theo bộ điều khiển.
 */
function windowTipRows(win: EditWindow): VwTipRow[] {
	const source = sourceOf(win.sourceId);
	const typeName = source?.sourceType ? baseConfig.getConfigItemName(BaseConfigTypeEnum.VideoWallSourceType, source.sourceType) || source.sourceType : '';
	const ctrlText = [vwCtrl.label(source?.controllerId), source?.inputPort ? $t('lz.label.videoWall.inputPortN', { port: source.inputPort }) : ''].filter(Boolean).join(' · ');
	return [
		{ icon: 'ele-CollectionTag', label: $t('lz.entity.base.code'), value: source?.code || win.sourceId || '' },
		{ icon: 'ele-VideoCamera', label: $t('lz.entity.videoWall.sourceType'), value: [typeName, source?.signalType].filter(Boolean).join(' · ') },
		{ icon: 'ele-Cpu', label: $t('lz.entity.videoWall.controller'), value: ctrlText, show: !!ctrlText },
		{ icon: 'ele-Location', label: $t('lz.label.videoWall.windowPosition'), value: windowCellRange(win) },
		{
			icon: 'ele-FullScreen',
			label: $t('lz.entity.base.size'),
			value: $t('lz.label.videoWall.gridColsRows', { cols: cellSpan(win.w, panelW.value), rows: cellSpan(win.h, panelH.value) }),
		},
		// Trang sửa kịch bản: cửa sổ vượt vùng được cấp ⇒ cảnh báo (chỉ xem thì không cần)
		{ icon: 'ele-WarningFilled', label: $t('lz.entity.base.status'), value: $t('lz.label.videoWall.outsidePermittedArea'), tone: 'warning', show: !props.readonly && isWinDenied(win) },
	];
}

function getWindowLabel(win: EditWindow): string {
	if (win.label) return win.label;
	const source = props.sources.find((s) => s.id === win.sourceId || s.code === win.sourceId);
	return source?.name || source?.code || win.sourceId || '';
}

// ── Drag/Move logic ──
let dragState: {
	type: 'move' | 'resize';
	win: EditWindow;
	/** Kích thước khối chứa cửa sổ (pixel logic) — cửa sổ không ra ngoài khối. */
	blockW: number;
	blockH: number;
	startMouseX: number;
	startMouseY: number;
	startX: number;
	startY: number;
	startW: number;
	startH: number;
	resizeDir: string;
	pendingX?: number;
	pendingY?: number;
	pendingW?: number;
	pendingH?: number;
} | null = null;

/**
 * Kéo cửa sổ ra khỏi canvas (thao tác ngược của kéo nguồn vào): cửa sổ "nhấc" theo con trỏ, thả vào cột
 * Nguồn tín hiệu (phần tử có `data-vw-remove-zone`) thì gỡ cửa sổ, thả chỗ khác thì cửa sổ ở lại vị trí cũ.
 */
const dragOut = ref<null | { winId: string; x: number; y: number; w: number; h: number; overZone: boolean }>(null);
const removingWinId = computed(() => dragOut.value?.winId ?? '');
const dragOutWin = computed(() => (dragOut.value ? props.windows.find((w) => w.id === dragOut.value!.winId) : undefined));
let removeZoneEl: HTMLElement | null = null;

function setRemoveZone(el: HTMLElement | null) {
	if (removeZoneEl === el) return;
	removeZoneEl?.classList.remove('is-remove-target');
	removeZoneEl = el;
	removeZoneEl?.classList.add('is-remove-target');
}

/** Con trỏ nằm ngoài khung canvas (ra vùng panel / danh sách nguồn). */
function isOutsideCanvas(event: MouseEvent): boolean {
	const rect = canvasRef.value?.getBoundingClientRect();
	if (!rect) return false;
	return event.clientX < rect.left || event.clientX > rect.right || event.clientY < rect.top || event.clientY > rect.bottom;
}

/** Cập nhật cửa sổ nhấc theo con trỏ; trả về true khi đang ở ngoài canvas. */
function trackDragOut(event: MouseEvent, winId: string): boolean {
	if (!isOutsideCanvas(event)) {
		dragOut.value = null;
		setRemoveZone(null);
		return false;
	}
	const zone = (document.elementFromPoint(event.clientX, event.clientY) as HTMLElement | null)?.closest('[data-vw-remove-zone]') as HTMLElement | null;
	setRemoveZone(zone);
	// Cỡ nhấc ra = cỡ cửa sổ đang hiển thị, giới hạn để không che cột nguồn
	const rect = canvasRef.value?.querySelector(`[data-win-id="${winId}"]`)?.getBoundingClientRect();
	const w = Math.round(Math.min(200, Math.max(96, rect?.width ?? 128)));
	const h = Math.round(Math.min(120, Math.max(54, rect?.height ?? 72)));
	dragOut.value = { winId, x: event.clientX - w / 2, y: event.clientY - h / 2, w, h, overZone: !!zone };
	return true;
}

function endDragOut() {
	dragOut.value = null;
	setRemoveZone(null);
}

function beginDrag(event: MouseEvent, win: EditWindow, type: 'move' | 'resize', dir: string) {
	const block = blockOf(win.blockNo);
	dragState = {
		type,
		win: { ...win },
		blockW: block?.w ?? 0,
		blockH: block?.h ?? 0,
		startMouseX: event.clientX,
		startMouseY: event.clientY,
		startX: win.x,
		startY: win.y,
		startW: win.w,
		startH: win.h,
		resizeDir: dir,
	};
	interacting.value = true;
	// Disable text selection during drag
	document.body.style.userSelect = 'none';
	document.body.style.webkitUserSelect = 'none';
	document.addEventListener('mousemove', onMouseMove);
	document.addEventListener('mouseup', onMouseUp);
}

function startInteraction(event: MouseEvent, win: EditWindow) {
	if (props.readonly || win.locked) return;
	event.preventDefault(); // Prevent browser text selection / native drag
	emit('selectWindow', win.id ?? '');
	// Cửa sổ nằm ngoài vùng được cấp: chỉ cho chọn để xem, không cho di chuyển
	if (isWinDenied(win)) return;
	beginDrag(event, win, 'move', '');
}

function startResize(event: MouseEvent, win: EditWindow, dir: string) {
	if (props.readonly || isWinDenied(win)) return;
	event.preventDefault(); // Prevent browser text selection / native drag
	emit('selectWindow', win.id ?? '');
	beginDrag(event, win, 'resize', dir);
}

function onMouseMove(event: MouseEvent) {
	if (!dragState) return;
	// Kéo ra ngoài canvas ⇒ cửa sổ nhấc theo con trỏ, trên lưới giữ vị trí hợp lệ gần nhất
	if (dragState.type === 'move' && trackDragOut(event, dragState.win.id ?? '')) return;
	const sf = scaleFactor.value;
	const sfy = scaleFactorY.value;
	const dx = (event.clientX - dragState.startMouseX) / sf;
	const dy = (event.clientY - dragState.startMouseY) / sfy;
	const { blockW, blockH } = dragState;

	let newX = dragState.startX;
	let newY = dragState.startY;
	let newW = dragState.startW;
	let newH = dragState.startH;

	if (dragState.type === 'move') {
		newX = dragState.startX + dx;
		newY = dragState.startY + dy;
	} else {
		const dir = dragState.resizeDir;
		// Cạnh đang kéo không được vượt mép khối: kích thước tối đa = khoảng từ cạnh cố định tới mép khối
		if (dir.includes('e')) newW = Math.min(blockW - dragState.startX, Math.max(panelW.value, dragState.startW + dx));
		if (dir.includes('w')) {
			newW = Math.min(dragState.startX + dragState.startW, Math.max(panelW.value, dragState.startW - dx));
			newX = dragState.startX + (dragState.startW - newW);
		}
		if (dir.includes('s')) newH = Math.min(blockH - dragState.startY, Math.max(panelH.value, dragState.startH + dy));
		if (dir.includes('n')) {
			newH = Math.min(dragState.startY + dragState.startH, Math.max(panelH.value, dragState.startH - dy));
			newY = dragState.startY + (dragState.startH - newH);
		}
	}

	// Apply snap
	newX = snapToCell(newX, panelW.value);
	newY = snapToCell(newY, panelH.value);
	if (dragState.type === 'resize') {
		newW = Math.max(panelW.value, snapToCell(newW, panelW.value));
		newH = Math.max(panelH.value, snapToCell(newH, panelH.value));
	}

	if (dragState.type === 'move') {
		// Clamp within block
		newX = Math.max(0, Math.min(newX, blockW - newW));
		newY = Math.max(0, Math.min(newY, blockH - newH));
	} else if (newX < 0 || newY < 0 || newX + newW > blockW || newY + newH > blockH) {
		// Resize (sau khi snap) ra ngoài khối ⇒ giữ nguyên kích thước hợp lệ gần nhất, không đẩy lệch X/Y
		return;
	}

	// Vị trí / kích thước mới ra ngoài vùng được cấp ⇒ giữ nguyên vị trí hợp lệ gần nhất
	if (!myPerm.isRectAllowed(dragState.win.blockNo, newX, newY, newW, newH)) return;

	// Store pending position (for visual feedback via local override)
	dragState.pendingX = Math.round(newX);
	dragState.pendingY = Math.round(newY);
	dragState.pendingW = Math.round(newW);
	dragState.pendingH = Math.round(newH);

	// Update the DOM element directly for smooth dragging (avoid reactive re-render)
	const el = canvasRef.value?.querySelector(`[data-win-id="${dragState.win.id}"]`) as HTMLElement | null;
	if (el) {
		el.style.left = `${newX * sf}px`;
		el.style.top = `${newY * sfy}px`;
		el.style.width = `${newW * sf}px`;
		el.style.height = `${newH * sfy}px`;
	}
}

function onMouseUp() {
	const out = dragOut.value;
	endDragOut();
	if (out?.overZone) {
		// Thả vào cột Nguồn tín hiệu ⇒ gỡ cửa sổ (trang cha kiểm tra quyền vùng như phím Delete)
		emit('deleteWindow', out.winId);
	} else if (dragState && (dragState.pendingX !== undefined)) {
		const updated: EditWindow = {
			...dragState.win,
			x: dragState.pendingX!,
			y: dragState.pendingY!,
			w: dragState.pendingW!,
			h: dragState.pendingH!,
		};
		emit('updateWindow', updated);
	}
	dragState = null;
	interacting.value = false;
	// Restore text selection
	document.body.style.userSelect = '';
	document.body.style.webkitUserSelect = '';
	document.removeEventListener('mousemove', onMouseMove);
	document.removeEventListener('mouseup', onMouseUp);
}

// ── Drop from palette ──
function onDragOver(event: DragEvent) {
	if (props.readonly) return;
	if (event.dataTransfer) {
		event.dataTransfer.dropEffect = 'copy';
	}
}

function onDrop(event: DragEvent) {
	if (props.readonly) return;
	const sourceId = event.dataTransfer?.getData('text/plain');
	if (!sourceId || !canvasRef.value) return;

	// Validate: sourceId must match a known source
	const validSource = props.sources.find((s) => s.id === sourceId || s.code === sourceId);
	if (!validSource) return; // Reject phantom/garbage drops

	// Khối được thả vào (thả ra ngoài khối thì bỏ qua) ⇒ toạ độ trong khối
	const blockEl = (event.target as HTMLElement | null)?.closest('[data-block]') as HTMLElement | null;
	const block = blockEl ? blockOf(Number(blockEl.dataset.block)) : undefined;
	if (!blockEl || !block) return;
	const rect = blockEl.getBoundingClientRect();
	const dropX = (event.clientX - rect.left) / scaleFactor.value;
	const dropY = (event.clientY - rect.top) / scaleFactorY.value;

	// Snap drop position
	let x = Math.floor(dropX / panelW.value) * panelW.value;
	let y = Math.floor(dropY / panelH.value) * panelH.value;

	x = Math.max(0, Math.min(x, block.w - panelW.value));
	y = Math.max(0, Math.min(y, block.h - panelH.value));

	emit('dropSource', sourceId, block.blockNo, Math.round(x), Math.round(y));
}

// ── Keyboard ──
function onKeyDown(event: KeyboardEvent) {
	if (props.readonly) return;
	if (event.key === 'Delete' && props.selectedWindowId) {
		const win = props.windows.find((w) => w.id === props.selectedWindowId);
		if (win && isWinDenied(win)) {
			ElMessage.warning(myPerm.deniedMessage());
			return;
		}
		emit('deleteWindow', props.selectedWindowId);
	}
}

// ── Lifecycle ──
onMounted(() => {
	topology.load();
	document.addEventListener('keydown', onKeyDown);
});

onUnmounted(() => {
	endDragOut();
	document.removeEventListener('keydown', onKeyDown);
	document.removeEventListener('mousemove', onMouseMove);
	document.removeEventListener('mouseup', onMouseUp);
});
</script>

<style scoped lang="scss">
@import '/@/theme/its/videoWallStage.scss';

/* Sân khấu tối dùng chung với tab Bố cục tường (Màn hình) và Phân quyền vùng */
.scene-canvas-root {
	position: relative;
	width: 100%;
	height: 100%;
}

.scene-canvas-hint {
	position: absolute;
	top: 10px;
	left: 50%;
	transform: translateX(-50%);
	font-size: 12px;
	color: var(--wg-muted);
	z-index: 1;
	pointer-events: none;
}

.scene-canvas-cell {
	@include vw-stage-tile;
	position: relative;
	width: 100%;
	height: 100%;
	border-radius: 0;
	display: flex;
	flex-direction: column;
	gap: 2px;
	align-items: center;
	justify-content: center;
	box-sizing: border-box;
}

.scene-canvas-cell-label {
	max-width: 100%;
	padding: 0 4px;
	box-sizing: border-box;
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
	font-size: 13px;
	color: rgba(232, 240, 251, 0.78);
	user-select: none;
	font-weight: 500;
	letter-spacing: 0.02em;
}

.scene-canvas-cell-pos {
	font-size: 11px;
	color: var(--wg-muted);
	user-select: none;
	white-space: nowrap;
}

.scene-window {
	position: absolute;
	border-radius: 6px;
	cursor: move;
	display: flex;
	align-items: center;
	justify-content: center;
	box-sizing: border-box;
	border: 2px solid rgba(255, 255, 255, 0.55);
	transition: border-color 0.15s, box-shadow 0.15s;
	overflow: hidden;
	box-shadow: 0 2px 8px rgba(0, 0, 0, 0.25);
}

.scene-window--selected {
	border-color: var(--el-color-primary);
	box-shadow: 0 0 0 3px var(--el-color-primary-light-7), 0 4px 12px rgba(0, 0, 0, 0.2);
}

.scene-window--locked {
	cursor: not-allowed;
}

/* Cửa sổ đè lên cửa sổ khác: trong suốt một phần để vẫn thấy hình dạng cửa sổ phía sau. */
.scene-window--covering {
	opacity: 0.75;
}

/* Đè lên cửa sổ CÙNG MÀU: chỉ trong suốt thì không thấy đường biên ⇒ viền trắng + quầng sáng. Cửa sổ đang chọn giữ viền primary. */
.scene-window--same-color:not(.scene-window--selected) {
	border-color: #fff;
	box-shadow: 0 0 0 1px rgba(0, 0, 0, 0.45), 0 0 10px 2px rgba(255, 255, 255, 0.7);
}

/* Phân quyền vùng màn hình: ô ngoài vùng được cấp (sọc chéo) và cửa sổ không được thao tác. */
.scene-canvas-cell--denied {
	@include vw-stage-denied;
}

.scene-window--denied {
	cursor: not-allowed;
	opacity: 0.55;
	filter: grayscale(0.6);
}

/* Đang kéo ra ngoài canvas: chỗ cũ trên lưới chỉ còn khung mờ */
.scene-window--removing {
	opacity: 0.3;
	border-style: dashed;
}

/* Cửa sổ nhấc theo con trỏ (gắn vào body) */
.scene-window-ghost {
	position: fixed;
	z-index: 3000;
	cursor: grabbing;
	pointer-events: none;
	box-shadow: 0 8px 20px rgba(0, 0, 0, 0.35);
	transition: none;
}

.scene-window-ghost.is-over-zone {
	opacity: 0.75;
	border-color: var(--el-color-danger);
}

.scene-canvas-remove-hint {
	position: absolute;
	top: 12px;
	left: 50%;
	z-index: 300;
	transform: translateX(-50%);
	display: inline-flex;
	align-items: center;
	gap: 6px;
	padding: 6px 12px;
	border-radius: 4px;
	background: var(--el-color-danger);
	color: #fff;
	font-size: 12px;
	white-space: nowrap;
	box-shadow: 0 4px 12px rgba(0, 0, 0, 0.3);
	pointer-events: none;
}

/* Chế độ chỉ xem: con trỏ không gợi ý kéo được, và không có hiệu ứng hover/selected. */
.scene-canvas--readonly .scene-window {
	cursor: default;
}

.scene-window-content {
	display: flex;
	flex-direction: column;
	align-items: center;
	justify-content: center;
	gap: 4px;
	color: #fff;
	/* Viền chữ đậm hơn để nhãn luôn nét trên mọi màu nền loại nguồn. */
	text-shadow: 0 1px 3px rgba(0, 0, 0, 0.55);
	pointer-events: none;
	text-align: center;
	padding: 4px;
}

.scene-window-icon {
	color: #fff;
	display: flex;
}

/* Cửa sổ nhỏ: chỉ còn icon, bỏ đệm để icon không bị cắt */
.scene-window--compact .scene-window-content {
	gap: 0;
	padding: 0;
}

.scene-window-label {
	font-size: 13px;
	font-weight: 600;
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
	max-width: 100%;
}

.scene-window-lock {
	position: absolute;
	top: 2px;
	right: 2px;
	color: #fff;
	background: rgba(0, 0, 0, 0.3);
	border-radius: 2px;
	padding: 1px 3px;
}

/* Resize handles */
.resize-handle {
	position: absolute;
	background: var(--el-color-primary);
	border: 1.5px solid var(--el-bg-color);
	border-radius: 2px;
	z-index: 100;
}

.resize-handle-n,
.resize-handle-s {
	left: 50%;
	transform: translateX(-50%);
	width: 8px;
	height: 8px;
	cursor: ns-resize;
}
.resize-handle-n { top: -4px; }
.resize-handle-s { bottom: -4px; }

.resize-handle-e,
.resize-handle-w {
	top: 50%;
	transform: translateY(-50%);
	width: 8px;
	height: 8px;
	cursor: ew-resize;
}
.resize-handle-e { right: -4px; }
.resize-handle-w { left: -4px; }

.resize-handle-ne,
.resize-handle-nw,
.resize-handle-se,
.resize-handle-sw {
	width: 8px;
	height: 8px;
}
.resize-handle-ne { top: -4px; right: -4px; cursor: nesw-resize; }
.resize-handle-nw { top: -4px; left: -4px; cursor: nwse-resize; }
.resize-handle-se { bottom: -4px; right: -4px; cursor: nwse-resize; }
.resize-handle-sw { bottom: -4px; left: -4px; cursor: nesw-resize; }
</style>
