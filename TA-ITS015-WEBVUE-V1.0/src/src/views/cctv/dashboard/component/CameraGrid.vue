<template>
	<section class="camera-grid-wrap" :class="{ fullscreen }">
		<div ref="stageRef" class="camera-stage" :style="stageStyle">
			<div ref="gridRef" class="grid-stack camera-grid" :class="{ editing: editMode }" :style="gridStyleVars">
				<div v-for="camera in normalizedCameras" :key="camera.id" class="grid-stack-item" :gs-id="camera.id"
					:gs-x="camera.x" :gs-y="camera.y" :gs-w="camera.w" :gs-h="camera.h"
					:gs-min-w="dashboardGridConfig.minW"
					:gs-min-h="dashboardGridConfig.minH" :gs-max-w="getCameraMaxSize(camera)"
					:gs-max-h="getCameraMaxSize(camera)">
					<div class="grid-stack-item-content">
						<CameraTile :camera="camera" :edit-mode="editMode" :camera-only="cameraOnly" view-mode="grid"
							:show-settings="showSettings" :show-controls="showControls"
							:stream-resolution="getCameraStreamResolution(camera)"
							:stream-profile="getCameraStreamProfile(camera)"
							:prefer-api-stream="fullscreen"
							:selected="camera.id === selectedSlotId" auto-play
							@detail="$emit('detail', $event)" @remove="$emit('remove', $event)"
							@add-slot="$emit('add-slot', $event)" @select="$emit('select', $event)"
							@settings="$emit('settings', $event)"
							@camera-action="(action, id) => $emit('camera-action', action, id)" />
					</div>
				</div>
			</div>
		</div>
	</section>
</template>

<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue';
import { GridStack, type GridStackNode } from 'gridstack';
import 'gridstack/dist/gridstack.min.css';
import { dashboardGridConfig, normalizeCameraGridItem, type VideoMonitoringItem } from '/@/views/cctv/dashboard/types';
import CameraTile from './CameraTile.vue';
import { useI18n } from 'vue-i18n';

const props = defineProps<{
	cameras: VideoMonitoringItem[];
	editMode?: boolean;
	fullscreen?: boolean;
	cameraOnly?: boolean;
	showSettings?: boolean;
	showControls?: boolean;
	selectedSlotId?: string;
	liveLayoutSync?: boolean;
}>();

const emit = defineEmits<{
	(e: 'detail', id: string): void;
	(e: 'remove', id: string): void;
	(e: 'add-slot', id: string): void;
	(e: 'select', id: string): void;
	(e: 'settings', id: string): void;
	(e: 'camera-action', action: 'ptz' | 'playback' | 'screenshot' | 'info', id: string): void;
	(e: 'layout-change', cameras: VideoMonitoringItem[]): void;
	(e: 'layout-dirty'): void;
}>();

const { t } = useI18n();
const viewport = reactive({ width: window.innerWidth, height: window.innerHeight });
const stageContainer = reactive({ width: 0, height: 0 });
const stageRef = ref<HTMLElement>();
const gridRef = ref<HTMLElement>();
let grid: GridStack | null = null;
let resizeObserver: ResizeObserver | null = null;
let lastEmittedLayoutSignature = '';
let initializingGrid = false;
let isUnmounted = false;
let initGridVersion = 0;
let syncingSquareResize = false;
let activeDragId = '';
let activeResizeId = '';
let pendingLayoutCommitFrame = 0;
let pendingLayoutCommitTimer = 0;
const resizingStartSizes = new Map<string, number>();

type GridLayoutSnapshot = Pick<VideoMonitoringItem, 'x' | 'y' | 'w' | 'h'> & { id: string };

let resizingStartLayout: GridLayoutSnapshot[] = [];
let lastStableLayout: GridLayoutSnapshot[] = [];

const getLayoutSignature = (items: VideoMonitoringItem[]) => items.map((camera) => `${camera.id}:${camera.x}:${camera.y}:${camera.w}:${camera.h}`).join('|');
const getIdSignature = (items: VideoMonitoringItem[]) => items.map((camera) => camera.id).join('|');

const displayAspectRatio = 16 / 9;

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

const normalizedCameras = computed(() =>
	props.cameras.map((camera) => {
		const layout = normalizeCameraGridItem(camera);
		return {
			...camera,
			...layout,
			id: toSafeString(camera.id, `camera-${props.cameras.indexOf(camera)}`),
			sourceCameraId: toSafeString(camera.sourceCameraId),
			deviceId: toSafeString(camera.deviceId),
			ip: toSafeString(camera.ip),
			name: toSafeString(camera.name, 'Camera'),
			location: toSafeString(camera.location),
			status: ['online', 'warning', 'offline'].includes(toSafeString(camera.status)) ? toSafeString(camera.status) : 'offline',
			aspect: toSafeString(camera.aspect, '16:9'),
			videoUrl: toSafeString(camera.videoUrl),
			isPlaceholder: Boolean(camera.isPlaceholder),
		};
	})
);

const stageStyle = computed(() => {
	return {
		width: `${stageSize.value.width}px`,
		height: `${stageSize.value.height}px`,
	};
});

const stageSize = computed(() => {
	const parentWidth = stageContainer.width || stageRef.value?.parentElement?.clientWidth || viewport.width;
	const parentHeight = stageContainer.height || stageRef.value?.parentElement?.clientHeight || 0;
	const availableWidth = props.fullscreen ? viewport.width : parentWidth;
	const availableHeight = props.fullscreen ? viewport.height : parentHeight > 0 ? parentHeight : Math.max(320, viewport.height - 220);
	let width = availableWidth;
	let height = Math.round(width / displayAspectRatio);
	if (height > availableHeight) {
		height = availableHeight;
		width = Math.round(height * displayAspectRatio);
	}
	return {
		width: Math.max(1, Math.min(Math.floor(width), Math.floor(availableWidth))),
		height: Math.max(1, Math.min(Math.floor(height), Math.floor(availableHeight))),
	};
});

const cellMetrics = computed(() => {
	const colWidth = Math.max(1, stageSize.value.width / dashboardGridConfig.column);
	const rowHeight = Math.max(1, stageSize.value.height / dashboardGridConfig.row);
	return { colWidth, rowHeight };
});

const gridStyleVars = computed(() => {
	return {
		'--grid-col-size': `${cellMetrics.value.colWidth}px`,
		'--grid-row-size': `${cellMetrics.value.rowHeight}px`,
	};
});

const cameraStreamResolutionLevels = [
	{ minCell: 1, resolution: '256x144' },
	{ minCell: 2, resolution: '384x216' },
	{ minCell: 3, resolution: '960x540' },
	{ minCell: 4, resolution: '960x540' },
	{ minCell: 5, resolution: '1280x720' },
	{ minCell: 6, resolution: '1920x1080' },
];

const getCameraStreamResolution = (camera: VideoMonitoringItem) => {
	if (!props.fullscreen) return '384x216';
	const cellSpan = Math.max(1, Math.min(dashboardGridConfig.column, Math.max(camera.w || 1, camera.h || 1)));
	const level = [...cameraStreamResolutionLevels].reverse().find((item) => cellSpan >= item.minCell);
	return level?.resolution || '640x360';
};

const getCameraStreamProfile = (_camera: VideoMonitoringItem) => {
	return 'secondary';
};

const responsiveRowHeight = () => {
	return cellMetrics.value.rowHeight;
};

const updateResponsiveMetrics = () => {
	if (!grid || !(grid as any).opts) return;
	grid.cellHeight(responsiveRowHeight());
	grid.margin(dashboardGridConfig.margin);
};

const updateStageContainerSize = () => {
	const parent = stageRef.value?.parentElement || gridRef.value?.parentElement;
	if (!parent) return;
	const nextWidth = parent.clientWidth;
	const nextHeight = parent.clientHeight;
	if (stageContainer.width === nextWidth && stageContainer.height === nextHeight) return;
	stageContainer.width = nextWidth;
	stageContainer.height = nextHeight;
};

const getCurrentGridNodes = () => (((grid as any)?.engine?.nodes || []) as GridStackNode[]);

const findGridItemElement = (cameraId: string) => {
	if (!gridRef.value) return null;
	return (Array.from(gridRef.value.children).find((element) => element instanceof HTMLElement && element.getAttribute('gs-id') === cameraId) as HTMLElement | undefined) || null;
};

const getGridNodeById = (cameraId: string) => getCurrentGridNodes().find((node) => toSafeString(node.id) === cameraId);

const nodeToLayout = (node: GridStackNode): GridLayoutSnapshot => ({
	id: toSafeString(node.id),
	x: node.x || 0,
	y: node.y || 0,
	w: node.w || dashboardGridConfig.defaultW,
	h: node.h || dashboardGridConfig.defaultH,
});

const updateLastStableLayout = () => {
	lastStableLayout = normalizedCameras.value.map((camera) => {
		const item = normalizeCameraGridItem(camera);
		return {
			id: item.id,
			x: item.x,
			y: item.y,
			w: item.w,
			h: item.h,
		};
	});
};

const getLayoutSnapshot = (): GridLayoutSnapshot[] => {
	const nodes = getCurrentGridNodes();
	if (nodes.length) {
		return nodes.map((node) => nodeToLayout(node));
	}
	return normalizedCameras.value.map((camera) => normalizeCameraGridItem(camera));
};

const getCameraMaxSize = (camera: VideoMonitoringItem) => {
	const item = normalizeCameraGridItem(camera);
	return Math.min(dashboardGridConfig.column - item.x, dashboardGridConfig.row - item.y);
};

const isLayoutWithinGrid = (layout: Pick<VideoMonitoringItem, 'x' | 'y' | 'w' | 'h'>[]) =>
	layout.every((item) => item.x >= 0 && item.y >= 0 && item.x + item.w <= dashboardGridConfig.column && item.y + item.h <= dashboardGridConfig.row);

const isOverlapping = (first: Pick<VideoMonitoringItem, 'x' | 'y' | 'w' | 'h'>, second: Pick<VideoMonitoringItem, 'x' | 'y' | 'w' | 'h'>) =>
	first.x < second.x + second.w && first.x + first.w > second.x && first.y < second.y + second.h && first.y + first.h > second.y;

const hasLayoutOverlap = (layout: Pick<VideoMonitoringItem, 'x' | 'y' | 'w' | 'h'>[]) => {
	for (let index = 0; index < layout.length; index++) {
		for (let nextIndex = index + 1; nextIndex < layout.length; nextIndex++) {
			if (isOverlapping(layout[index], layout[nextIndex])) return true;
		}
	}
	return false;
};

const isLayoutValid = (layout: Pick<VideoMonitoringItem, 'x' | 'y' | 'w' | 'h'>[]) => isLayoutWithinGrid(layout) && !hasLayoutOverlap(layout);

const canPlaceLayoutItem = (item: GridLayoutSnapshot, placedItems: GridLayoutSnapshot[]) => {
	if (item.x < 0 || item.y < 0) return false;
	if (item.x + item.w > dashboardGridConfig.column || item.y + item.h > dashboardGridConfig.row) return false;
	return !placedItems.some((placed) => isOverlapping(item, placed));
};

const getPlacementCandidates = (preferredX: number, preferredY: number, width: number, height: number) => {
	const maxX = Math.max(0, dashboardGridConfig.column - width);
	const maxY = Math.max(0, dashboardGridConfig.row - height);
	const candidates: Array<{ x: number; y: number; score: number }> = [];
	for (let y = 0; y <= maxY; y++) {
		for (let x = 0; x <= maxX; x++) {
			const deltaX = x - preferredX;
			const deltaY = y - preferredY;
			// Prefer nearby slots in every direction so widgets can shift horizontally/diagonally.
			const score = deltaX * deltaX + deltaY * deltaY + y * 0.01 + x * 0.001;
			candidates.push({ x, y, score });
		}
	}
	return candidates.sort((first, second) => first.score - second.score);
};

const resolveLayoutWithNearestFit = (layout: GridLayoutSnapshot[], anchorId = '') => {
	if (!layout.length) return layout;
	const byId = new Map(layout.map((item) => [item.id, item]));
	const preferredOrder = [
		...(anchorId ? [anchorId] : []),
		...lastStableLayout.map((item) => item.id),
		...layout.map((item) => item.id),
	].filter((id, index, array) => Boolean(id) && array.indexOf(id) === index && byId.has(id));
	const placed: GridLayoutSnapshot[] = [];
	for (const id of preferredOrder) {
		const current = byId.get(id);
		if (!current) continue;
		const preferredX = Math.max(0, Math.min(current.x, dashboardGridConfig.column - current.w));
		const preferredY = Math.max(0, Math.min(current.y, dashboardGridConfig.row - current.h));
		const candidates = getPlacementCandidates(preferredX, preferredY, current.w, current.h);
		const matched = candidates.find((candidate) =>
			canPlaceLayoutItem(
				{
					...current,
					x: candidate.x,
					y: candidate.y,
				},
				placed
			)
		);
		if (!matched) return null;
		placed.push({
			...current,
			x: matched.x,
			y: matched.y,
		});
	}
	return placed;
};

const tryResolveInvalidGridLayout = (anchorId = '') => {
	const currentLayout = getCurrentGridNodes().map((node) => nodeToLayout(node));
	if (isLayoutValid(currentLayout)) return true;
	const resolvedLayout = resolveLayoutWithNearestFit(currentLayout, anchorId);
	if (!resolvedLayout) return false;
	restoreLayoutSnapshot(resolvedLayout);
	const updatedLayout = getCurrentGridNodes().map((node) => nodeToLayout(node));
	return isLayoutValid(updatedLayout);
};

const restoreLayoutSnapshot = (layout: GridLayoutSnapshot[]) => {
	if (!grid || !layout.length) return;
	syncingSquareResize = true;
	grid.batchUpdate();
	layout.forEach((item) => {
		const targetNode = getCurrentGridNodes().find((currentNode) => toSafeString(currentNode.id) === item.id);
		if (!targetNode?.el) return;
		grid?.update(targetNode.el, item);
	});
	grid.batchUpdate(false);
	syncingSquareResize = false;
};

const restoreResizeStartLayout = () => {
	restoreLayoutSnapshot(resizingStartLayout);
};

const clearActiveDragOnNextFrame = () => {
	window.requestAnimationFrame(() => {
		activeDragId = '';
	});
};

const clearPendingLayoutCommit = () => {
	if (pendingLayoutCommitFrame) {
		window.cancelAnimationFrame(pendingLayoutCommitFrame);
		pendingLayoutCommitFrame = 0;
	}
	if (pendingLayoutCommitTimer) {
		window.clearTimeout(pendingLayoutCommitTimer);
		pendingLayoutCommitTimer = 0;
	}
};

const hasPendingLayoutCommit = () => Boolean(pendingLayoutCommitFrame || pendingLayoutCommitTimer);

const scheduleLayoutCommit = () => {
	clearPendingLayoutCommit();
	pendingLayoutCommitFrame = window.requestAnimationFrame(() => {
		pendingLayoutCommitFrame = 0;
		pendingLayoutCommitTimer = window.setTimeout(() => {
			pendingLayoutCommitTimer = 0;
			if (isUnmounted || !grid) return;
			commitLayout(getCurrentGridNodes(), { emitChange: true, markDirty: true });
		}, 0);
	});
};

const compactGridLayout = () => {
	if (!grid || syncingSquareResize) return;
	if (!dashboardGridConfig.float) grid.compact('list');
};

const getSquareResizeSize = (node: GridStackNode) => {
	const id = toSafeString(node.id);
	const startSize = resizingStartSizes.get(id) || Math.max(node.w || 1, node.h || 1);
	const width = node.w || startSize;
	const height = node.h || startSize;
	const isGrowing = width > startSize || height > startSize;
	const isShrinking = width < startSize || height < startSize;
	const rawSize = isGrowing ? Math.max(width, height) : isShrinking ? Math.min(width, height) : startSize;
	const minSize = dashboardGridConfig.minW;
	const maxSize = Math.min(dashboardGridConfig.column - (node.x || 0), dashboardGridConfig.row - (node.y || 0));
	return Math.max(minSize, Math.min(rawSize, maxSize));
};

const forceSquareResize = (el: HTMLElement, shouldEmit = false) => {
	if (!grid || syncingSquareResize) return;
	const node = (el as HTMLElement & { gridstackNode?: GridStackNode }).gridstackNode;
	if (!node) return;
	const currentLayout = getCurrentGridNodes().map((currentNode) =>
		nodeToLayout(currentNode)
	);
	if (!isLayoutValid(currentLayout)) {
		if (tryResolveInvalidGridLayout(activeResizeId)) {
			if (shouldEmit) commitLayout(getCurrentGridNodes(), { emitChange: props.liveLayoutSync, markDirty: true });
			return;
		}
		restoreResizeStartLayout();
		if (shouldEmit) commitLayout(getCurrentGridNodes(), { emitChange: props.liveLayoutSync, markDirty: true });
		return;
	}
	const layout = {
		x: node.x ?? 0,
		y: node.y ?? 0,
		w: getSquareResizeSize(node),
		h: getSquareResizeSize(node),
	};
	if (!isLayoutWithinGrid([{ x: layout.x, y: layout.y, w: layout.w, h: layout.h }])) {
		restoreResizeStartLayout();
		if (shouldEmit) commitLayout(getCurrentGridNodes(), { emitChange: props.liveLayoutSync, markDirty: true });
		return;
	}
	syncingSquareResize = true;
	grid.update(el, layout);
	syncingSquareResize = false;
	const nextLayout = getCurrentGridNodes().map((currentNode) =>
		nodeToLayout(currentNode)
	);
	if (!isLayoutValid(nextLayout)) {
		if (tryResolveInvalidGridLayout(activeResizeId)) {
			if (shouldEmit) commitLayout(getCurrentGridNodes(), { emitChange: props.liveLayoutSync, markDirty: true });
			return;
		}
		restoreResizeStartLayout();
		if (shouldEmit) commitLayout(getCurrentGridNodes(), { emitChange: props.liveLayoutSync, markDirty: true });
		return;
	}
	if (shouldEmit) commitLayout(getCurrentGridNodes(), { emitChange: props.liveLayoutSync, markDirty: true });
};

const getGridLayoutCameras = (nodes: GridStackNode[] = getCurrentGridNodes()) => {
	const byId = new Map(nodes.map((node) => [toSafeString(node.id), node]));
	return normalizedCameras.value.map((camera) => {
		const node = byId.get(camera.id);
		if (!node) return camera;
		return {
			...normalizeCameraGridItem(camera),
			x: node.x ?? camera.x,
			y: node.y ?? camera.y,
			w: node.w ?? camera.w,
			h: node.h ?? camera.h,
		};
	}).map(normalizeCameraGridItem);
};

const commitLayout = (nodes: GridStackNode[] = getCurrentGridNodes(), options: { emitChange?: boolean; markDirty?: boolean } = {}) => {
	const currentLayout = getCurrentGridNodes().map((node) => nodeToLayout(node));
	if (!isLayoutValid(currentLayout)) return normalizedCameras.value;
	const nextCameras = getGridLayoutCameras(nodes);
	const nextSignature = getLayoutSignature(nextCameras);
	if (nextSignature === getLayoutSignature(normalizedCameras.value)) return nextCameras;
	lastStableLayout = nextCameras.map((camera) => ({
		id: camera.id,
		x: camera.x,
		y: camera.y,
		w: camera.w,
		h: camera.h,
	}));
	lastEmittedLayoutSignature = nextSignature;
	if (options.markDirty) emit('layout-dirty');
	if (options.emitChange) {
		emit('layout-change', nextCameras);
	}
	return nextCameras;
};

const removeStaleGridWidgets = () => {
	if (!grid) return;
	const nextIds = new Set(normalizedCameras.value.map((camera) => camera.id));
	const staleNodes = getCurrentGridNodes().filter((node) => !nextIds.has(toSafeString(node.id)));
	if (!staleNodes.length) return;
	syncingSquareResize = true;
	try {
		grid.batchUpdate();
		staleNodes.forEach((node) => {
			if (!node.el) return;
			(grid as any).removeWidget(node.el, false, false);
		});
		grid.batchUpdate(false);
	} finally {
		syncingSquareResize = false;
	}
};

const syncGridWidgets = async (options: { preserveExistingLayout?: boolean } = {}) => {
	if (!grid) {
		initGrid();
		return;
	}
	const version = initGridVersion;
	await nextTick();
	if (isUnmounted || version !== initGridVersion || !grid || !gridRef.value) {
		return;
	}
	initializingGrid = true;
	syncingSquareResize = true;
	try {
		removeStaleGridWidgets();
		grid.batchUpdate();
		normalizedCameras.value.forEach((camera) => {
			const item = normalizeCameraGridItem(camera);
			const el = findGridItemElement(item.id);
			if (!el) {
				return;
			}
			let node = getGridNodeById(item.id);
			const isNewWidget = !node;
			if (!node) {
				(grid as any).makeWidget(el);
				node = (el as HTMLElement & { gridstackNode?: GridStackNode }).gridstackNode || getGridNodeById(item.id);
			}
			if (!node?.el) {
				return;
			}
			if (options.preserveExistingLayout && !isNewWidget) {
				// Still update maxW/maxH for existing widgets in case position changed via drag
				const currentMaxSize = Math.min(dashboardGridConfig.column - (node.x || 0), dashboardGridConfig.row - (node.y || 0));
				if (node.maxW !== currentMaxSize || node.maxH !== currentMaxSize) {
					grid?.update(node.el, { maxW: currentMaxSize, maxH: currentMaxSize });
				}
				return;
			}
			const maxSize = Math.min(dashboardGridConfig.column - item.x, dashboardGridConfig.row - item.y);
			const nextLayout = {
				x: item.x,
				y: item.y,
				w: item.w,
				h: item.h,
				minW: dashboardGridConfig.minW,
				minH: dashboardGridConfig.minH,
				maxW: maxSize,
				maxH: maxSize,
			};
			if (
				node.x !== nextLayout.x ||
				node.y !== nextLayout.y ||
				node.w !== nextLayout.w ||
				node.h !== nextLayout.h ||
				node.maxW !== nextLayout.maxW ||
				node.maxH !== nextLayout.maxH
			) {
				grid?.update(node.el, nextLayout);
			}
		});
		grid.batchUpdate(false);
		updateResponsiveMetrics();
	} finally {
		syncingSquareResize = false;
		initializingGrid = false;
	}
};

const initGrid = async () => {
	const version = ++initGridVersion;
	initializingGrid = true;
	await nextTick();
	if (isUnmounted || version !== initGridVersion || !gridRef.value) {
		initializingGrid = false;
		return;
	}
	if (grid) {
		const oldGrid = grid;
		grid = null;
		oldGrid.destroy(false);
	}
	grid = GridStack.init(
		{
			column: dashboardGridConfig.column,
			cellHeight: responsiveRowHeight(),
			margin: dashboardGridConfig.margin,
			minRow: dashboardGridConfig.minRow,
			maxRow: dashboardGridConfig.maxRow,
			float: dashboardGridConfig.float,
			animate: dashboardGridConfig.animate,
			staticGrid: !props.editMode,
			draggable: {
				handle: '.grid-stack-item',
				cancel: '.placeholder-action, .el-button, button, input, textarea, select, option, video, .camera-only',
			},
			resizable: { handles: 'se' },
		},
		gridRef.value
	);
	if (isUnmounted || version !== initGridVersion) {
		const oldGrid = grid;
		grid = null;
		oldGrid?.destroy(false);
		initializingGrid = false;
		return;
	}
	grid.on('change', (_event, nodes) => {
		if (initializingGrid || syncingSquareResize || activeDragId || activeResizeId) return;
		if (props.liveLayoutSync) commitLayout(nodes, { emitChange: true });
	});
	grid.on('dragstart', (_event, el) => {
		activeDragId = el?.gridstackNode?.id ? toSafeString(el.gridstackNode.id) : '__dragging__';
	});
	grid.on('resizestart', (_event, el) => {
		const node = el.gridstackNode;
		if (!node?.id) return;
		activeResizeId = toSafeString(node.id);
		resizingStartLayout = getLayoutSnapshot();
		resizingStartSizes.set(activeResizeId, Math.max(node.w || 1, node.h || 1));
	});
	grid.on('resizestop', (_event, el) => {
		forceSquareResize(el, false);
		const node = el.gridstackNode;
		const anchorId = node?.id ? toSafeString(node.id) : '';
		if (node?.id) resizingStartSizes.delete(toSafeString(node.id));
		activeResizeId = '';
		resizingStartLayout = [];
		compactGridLayout();
		// Update maxW/maxH for all nodes after resize (positions may have shifted)
		getCurrentGridNodes().forEach((resizedNode) => {
			if (!resizedNode.el) return;
			const newMax = Math.min(dashboardGridConfig.column - (resizedNode.x || 0), dashboardGridConfig.row - (resizedNode.y || 0));
			if (resizedNode.maxW !== newMax || resizedNode.maxH !== newMax) {
				grid?.update(resizedNode.el, { maxW: newMax, maxH: newMax });
			}
		});
		const layoutAfterResize = getCurrentGridNodes().map((currentNode) => nodeToLayout(currentNode));
		if (!isLayoutValid(layoutAfterResize)) {
			if (tryResolveInvalidGridLayout(anchorId)) {
				scheduleLayoutCommit();
				return;
			}
			if (lastStableLayout.length) restoreLayoutSnapshot(lastStableLayout);
			scheduleLayoutCommit();
			return;
		}
		scheduleLayoutCommit();
	});
	grid.on('dragstop', (_event, el) => {
		const anchorId = el?.gridstackNode?.id ? toSafeString(el.gridstackNode.id) : '';
		compactGridLayout();
		// Update maxW/maxH for all nodes after drag (positions may have shifted)
		getCurrentGridNodes().forEach((node) => {
			if (!node.el) return;
			const newMax = Math.min(dashboardGridConfig.column - (node.x || 0), dashboardGridConfig.row - (node.y || 0));
			if (node.maxW !== newMax || node.maxH !== newMax) {
				grid?.update(node.el, { maxW: newMax, maxH: newMax });
			}
		});
		const layoutAfterDrag = getCurrentGridNodes().map((currentNode) => nodeToLayout(currentNode));
		if (!isLayoutValid(layoutAfterDrag)) {
			if (tryResolveInvalidGridLayout(anchorId)) {
				scheduleLayoutCommit();
				clearActiveDragOnNextFrame();
				return;
			}
			if (lastStableLayout.length) restoreLayoutSnapshot(lastStableLayout);
			scheduleLayoutCommit();
			clearActiveDragOnNextFrame();
			return;
		}
		scheduleLayoutCommit();
		clearActiveDragOnNextFrame();
	});
	if (!resizeObserver) {
		resizeObserver = new ResizeObserver(() => {
			updateStageContainerSize();
			nextTick(() => updateResponsiveMetrics());
		});
		if (stageRef.value?.parentElement) resizeObserver.observe(stageRef.value.parentElement);
		if (stageRef.value) resizeObserver.observe(stageRef.value);
	}
	await nextTick();
	initializingGrid = false;
	updateLastStableLayout();
};

const updateViewport = () => {
	viewport.width = window.innerWidth;
	viewport.height = window.innerHeight;
	updateStageContainerSize();
	nextTick(() => updateResponsiveMetrics());
};

watch(
	() => props.editMode,
	(value) => {
		if (!grid || !(grid as any).opts) return;
		grid.setStatic(!value);
	},
	{ flush: 'post' }
);

watch(
	() => getIdSignature(normalizedCameras.value),
	() => {
		removeStaleGridWidgets();
	},
	{ flush: 'pre' }
);

watch(
	() => getLayoutSignature(normalizedCameras.value),
	(value) => {
		updateLastStableLayout();
		if (value === lastEmittedLayoutSignature) {
			lastEmittedLayoutSignature = '';
			return;
		}
		syncGridWidgets({ preserveExistingLayout: hasPendingLayoutCommit() || Boolean(activeDragId || activeResizeId) });
	},
	{ flush: 'post' }
);

watch(
	() => props.fullscreen,
	() => {
		initGrid();
	},
	{ flush: 'post' }
);

watch(
	() => props.cameraOnly,
	() => {
		initGrid();
	},
	{ flush: 'post' }
);

watch(
	() => `${stageSize.value.width}:${stageSize.value.height}`,
	() => nextTick(() => updateResponsiveMetrics()),
	{ flush: 'post' }
);

onMounted(() => {
	isUnmounted = false;
	updateStageContainerSize();
	initGrid();
	window.addEventListener('resize', updateViewport);
});

onBeforeUnmount(() => {
	isUnmounted = true;
	initGridVersion++;
	clearPendingLayoutCommit();
	window.removeEventListener('resize', updateViewport);
	if (resizeObserver) resizeObserver.disconnect();
	resizeObserver = null;
	if (grid) {
		const oldGrid = grid;
		grid = null;
		oldGrid.destroy(false);
	}
});

defineExpose({
	getCurrentLayout: () => {
		clearPendingLayoutCommit();
		return commitLayout(getCurrentGridNodes(), { emitChange: false });
	},
});
</script>

<style scoped lang="scss">
.camera-grid-wrap {
	display: grid;
	place-items: center;
	gap: 14px;
	width: 100%;
	height: 100%;
	max-width: 100%;
	max-height: 100%;
	min-width: 0;
	min-height: 0;
	overflow: hidden;
}

.grid-note,
.fullscreen-note {
	border-radius: 8px;
	background: var(--el-fill-color-light);
	border: 1px solid var(--el-border-color-light);
	padding: 12px 16px;
	color: var(--el-text-color-regular);
	font-size: 13px;
	line-height: 18px;
}

.camera-stage {
	width: 100%;
	max-width: 100%;
	max-height: 100%;
	overflow: hidden;
	border-radius: 8px;
	background: #ffffff;
	border: 1px solid var(--el-border-color-lighter, #ebeef5);
	box-sizing: border-box;
}

.camera-grid {
	width: 100%;
	height: 100%;
	min-height: 100%;
	border-radius: 8px;
	overflow: hidden;
	background: #ffffff;
}

.grid-stack-item-content {
	inset: 0 !important;
	overflow: hidden;
	border-radius: 8px;
}

.editing {
	background-color: #ffffff;
	background-image: linear-gradient(var(--el-border-color-lighter, #e5e7eb) 1px, transparent 1px), linear-gradient(90deg, var(--el-border-color-lighter, #e5e7eb) 1px, transparent 1px);
	background-size: var(--grid-col-size, calc(100% / 6)) var(--grid-row-size, clamp(4.5em, 10vh, 12vh));
	border: 1px dashed var(--el-border-color, #cbd5e1);
}

.camera-grid.editing :deep(.grid-stack-item.ui-resizable-autohide .ui-resizable-handle),
.camera-grid.editing :deep(.ui-resizable-handle) {
	display: block !important;
	opacity: 1;
	z-index: 4;
}

.fullscreen {
	min-height: 100vh;
	padding: 0;
	background: #000000;

	.camera-stage {
		border-radius: 0;
		border-color: #000000;
		background: #000000;
	}

	.camera-grid {
		min-height: 100%;
		background: #000000;
	}

	.camera-grid.editing {
		background-color: #000000;
	}
}
</style>
