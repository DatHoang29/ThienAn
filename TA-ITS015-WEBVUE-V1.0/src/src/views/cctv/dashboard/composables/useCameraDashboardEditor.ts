import { computed, onBeforeUnmount, onMounted, reactive, ref, type Ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import {
	dashboardGridConfig,
	normalizeCameraGridItem,
	type DashboardCameraInfo,
	type ICctvDashboardTemplate,
	type ICctvDashboardTemplateSlot,
	type VideoMonitoringItem,
} from '/@/views/cctv/dashboard/types';
import { useI18n } from 'vue-i18n';

/** Kích thước mặc định khi kéo camera mới vào lưới — lấy từ cấu hình server (dashboardGridConfig). */
const getDefaultDroppedItemSize = () => dashboardGridConfig.defaultW;
type RectSnapshot = Pick<DOMRect, 'left' | 'top' | 'right' | 'bottom' | 'width' | 'height'>;

export const getEquipmentId = (equipment: DashboardCameraInfo | Record<string, unknown> | null | undefined) => {
	if (!equipment) return '';
	const value = (equipment as any).id ?? (equipment as any).ID ?? (equipment as any).Id;
	return value ? String(value) : '';
};

export const getCameraSourceId = (camera: VideoMonitoringItem) => camera.sourceCameraId || camera.id;

export const useCameraDashboardEditor = (equipments: Ref<DashboardCameraInfo[]>) => {
	const { t } = useI18n();
	const gridDropRef = ref<HTMLElement>();
	let cachedGridRect: RectSnapshot | null = null;
	let cachedParentRect: RectSnapshot | null = null;
	let lastGhostSignature = '';
	let latestManualDragPointer: null | { x: number; y: number } = null;
	let manualDragFrame = 0;

	const state = reactive({
		editMode: false,
		cameras: [] as VideoMonitoringItem[],
		addingCameraId: '',
		draggingCameraId: '',
		draggingOverGrid: false,
		dragGhost: null as null | { x: number; y: number; w: number; h: number },
		dropSurface: { left: 0, top: 0, width: 0, height: 0 },
		manualDragging: false,
		dragPointer: null as null | { x: number; y: number },
		dragStartPointer: null as null | { x: number; y: number },
		suppressNextPanelClick: false,
		cameraInstanceSeed: 0,
		selectedSlotId: '',
	});

	const gridGhostStyle = computed(() => {
		if (!state.dragGhost) return {};
		const col = dashboardGridConfig.column || 1;
		const row = dashboardGridConfig.row || 1;
		const { x, y, w, h } = state.dragGhost;
		const toPercent = (value: number, total: number) => `${Math.max(0, Math.min(100, (value / total) * 100))}%`;
		return {
			left: toPercent(x, col),
			top: toPercent(y, row),
			width: toPercent(w, col),
			height: toPercent(h, row),
		};
	});

	const gridOverlayStyle = computed(() => {
		const { left, top, width, height } = state.dropSurface;
		if (!width || !height) return { display: 'none' };
		return {
			left: `${left}px`,
			top: `${top}px`,
			width: `${width}px`,
			height: `${height}px`,
		};
	});

	const dragCursorStyle = computed(() => {
		if (!state.dragPointer) return {};
		return {
			left: `${state.dragPointer.x}px`,
			top: `${state.dragPointer.y}px`,
		};
	});

	const isEquipmentAdded = (equipmentId: string) =>
		state.cameras.some((camera) => !camera.isPlaceholder && getCameraSourceId(camera) === equipmentId);

	const toRectSnapshot = (rect: DOMRect): RectSnapshot => ({
		left: rect.left,
		top: rect.top,
		right: rect.right,
		bottom: rect.bottom,
		width: rect.width,
		height: rect.height,
	});

	const setDraggingOverGrid = (value: boolean) => {
		if (state.draggingOverGrid !== value) state.draggingOverGrid = value;
	};

	const setDragPointer = (x: number, y: number) => {
		if (state.dragPointer?.x === x && state.dragPointer?.y === y) return;
		state.dragPointer = { x, y };
	};

	const setDragGhost = (ghost: null | { x: number; y: number; w: number; h: number }) => {
		const signature = ghost ? `${ghost.x}:${ghost.y}:${ghost.w}:${ghost.h}` : '';
		if (signature === lastGhostSignature) return;
		lastGhostSignature = signature;
		state.dragGhost = ghost;
	};

	const clearManualDragFrame = () => {
		if (!manualDragFrame) return;
		window.cancelAnimationFrame(manualDragFrame);
		manualDragFrame = 0;
	};

	const clearDragRuntimeCache = () => {
		cachedGridRect = null;
		cachedParentRect = null;
		latestManualDragPointer = null;
		lastGhostSignature = '';
		clearManualDragFrame();
	};

	const createPlaceholderCamera = (slot: ICctvDashboardTemplateSlot): VideoMonitoringItem => {
		const layout = normalizeCameraGridItem({
			x: slot.X,
			y: slot.Y,
			w: slot.W,
			h: slot.H,
		});
		return {
			id: `template-slot-${slot.SlotNo}`,
			name: t('lz.label.cctvDashboard.selectCamera'),
			location: '',
			status: 'offline',
			aspect: slot.W && slot.H ? `${slot.W}:${slot.H}` : '16:9',
			videoUrl: '',
			...layout,
			isPlaceholder: true,
			slotNo: slot.SlotNo,
		};
	};

	const rectanglesOverlap = (
		first: Pick<VideoMonitoringItem, 'x' | 'y' | 'w' | 'h'>,
		second: Pick<VideoMonitoringItem, 'x' | 'y' | 'w' | 'h'>
	) => first.x < second.x + second.w && first.x + first.w > second.x && first.y < second.y + second.h && first.y + first.h > second.y;

	const clampToGrid = <T extends Pick<VideoMonitoringItem, 'x' | 'y' | 'w' | 'h'>>(item: T): T => normalizeCameraGridItem(item);

	const canPlaceCameraAt = (items: VideoMonitoringItem[], x: number, y: number, w: number, h: number) => {
		const candidate = normalizeCameraGridItem({ x, y, w, h });
		if (candidate.w < 1 || candidate.h < 1 || x < 0 || y < 0) return false;
		if (candidate.x + candidate.w > dashboardGridConfig.column || candidate.y + candidate.h > dashboardGridConfig.row) return false;
		return !items.map(clampToGrid).some((item) => rectanglesOverlap(item, candidate));
	};

	const getNextCameraPosition = (items: VideoMonitoringItem[], width: number, height: number) => {
		const layout = items.map(clampToGrid);
		const target = normalizeCameraGridItem({ x: 0, y: 0, w: width, h: height });
		for (let y = 0; y <= dashboardGridConfig.row - target.h; y++) {
			for (let x = 0; x <= dashboardGridConfig.column - target.w; x++) {
				if (!layout.some((item) => x < item.x + item.w && x + target.w > item.x && y < item.y + item.h && y + target.h > item.y)) {
					return { x, y, w: target.w, h: target.h };
				}
			}
		}
		return null;
	};

	const getNearestCameraPosition = (items: VideoMonitoringItem[], width: number, height: number, preferredX: number, preferredY: number) => {
		const target = normalizeCameraGridItem({ x: preferredX, y: preferredY, w: width, h: height });
		const maxX = Math.max(0, dashboardGridConfig.column - target.w);
		const maxY = Math.max(0, dashboardGridConfig.row - target.h);
		let best: { x: number; y: number; w: number; h: number; score: number } | null = null;
		for (let y = 0; y <= maxY; y++) {
			for (let x = 0; x <= maxX; x++) {
				if (!canPlaceCameraAt(items, x, y, target.w, target.h)) continue;
				const dx = x - Math.max(0, Math.min(preferredX, maxX));
				const dy = y - Math.max(0, Math.min(preferredY, maxY));
				const score = dx * dx + dy * dy;
				if (!best || score < best.score) best = { x, y, w: target.w, h: target.h, score };
			}
		}
		if (!best) return null;
		return { x: best.x, y: best.y, w: best.w, h: best.h };
	};

	const applyTemplateLayout = (template: ICctvDashboardTemplate) => {
		const slots = [...template.Items].sort((a, b) => (a.SlotNo || 0) - (b.SlotNo || 0));
		const realCameras = state.cameras.filter((camera) => !camera.isPlaceholder).map(clampToGrid);
		// Layout đã chuẩn hóa của từng ô template.
		const slotLayouts = slots.map((slot) => normalizeCameraGridItem({ x: slot.X, y: slot.Y, w: slot.W, h: slot.H }));

		const assignedCameraIds = new Set<string>();
		const slotCameras: (VideoMonitoringItem | null)[] = slotLayouts.map(() => null);

		// Pass 1 (GIỮ): camera đang nằm đúng góc trên-trái của một ô → giữ nguyên ô đó.
		slotLayouts.forEach((slotLayout, index) => {
			const match = realCameras.find(
				(camera) => !assignedCameraIds.has(camera.id) && camera.x === slotLayout.x && camera.y === slotLayout.y
			);
			if (match) {
				slotCameras[index] = match;
				assignedCameraIds.add(match.id);
			}
		});

		// Pass 2 (DỒN): các camera còn lại → xếp lần lượt vào các ô còn trống (theo thứ tự đọc của ô).
		const leftoverCameras = realCameras
			.filter((camera) => !assignedCameraIds.has(camera.id))
			.sort((a, b) => a.y - b.y || a.x - b.x);
		let leftoverIndex = 0;
		slotCameras.forEach((assigned, index) => {
			if (assigned || leftoverIndex >= leftoverCameras.length) return;
			const camera = leftoverCameras[leftoverIndex++];
			slotCameras[index] = camera;
			assignedCameraIds.add(camera.id);
		});

		// Dựng kết quả: ô có camera → đặt camera đúng layout ô; ô trống → placeholder.
		const placedCameras: VideoMonitoringItem[] = [];
		const placeholders: VideoMonitoringItem[] = [];
		slots.forEach((slot, index) => {
			const camera = slotCameras[index];
			if (camera) {
				placedCameras.push({ ...camera, ...slotLayouts[index], isPlaceholder: false });
			} else {
				placeholders.push(createPlaceholderCamera(slot));
			}
		});

		// Pass 3 (DƯ): camera nhiều hơn số ô → đặt vào chỗ trống ngoài template, hết chỗ thì bỏ qua.
		const extraCameras = realCameras.filter((camera) => !assignedCameraIds.has(camera.id));
		for (const extraCamera of extraCameras) {
			const nextPosition = getNextCameraPosition(placedCameras, extraCamera.w, extraCamera.h);
			if (!nextPosition) continue;
			placedCameras.push({ ...extraCamera, ...nextPosition, isPlaceholder: false });
			assignedCameraIds.add(extraCamera.id);
		}

		state.cameras = [...placedCameras, ...placeholders].sort((a, b) => a.y - b.y || a.x - b.x);
	};

	const clearTemplatePlaceholders = () => {
		state.cameras = state.cameras.filter((camera) => !camera.isPlaceholder);
		state.selectedSlotId = '';
	};

	const setSelectedSlotForAdd = (slotId: string) => {
		const slot = state.cameras.find((camera) => camera.id === slotId && camera.isPlaceholder);
		if (!slot) {
			ElMessage.warning(t('lz.message.cctvDashboard.addCameraToPlaceholderOnly'));
			return;
		}
		state.selectedSlotId = slotId;
		ElMessage.info(t('lz.message.cctvDashboard.selectCameraToAdd'));
	};

	const addCameraToGrid = (equipment: DashboardCameraInfo, layout: Pick<VideoMonitoringItem, 'x' | 'y' | 'w' | 'h'>, slotToReplace?: VideoMonitoringItem) => {
		const collisionScope = slotToReplace ? state.cameras.filter((camera) => camera.id !== slotToReplace.id) : state.cameras;
		if (!canPlaceCameraAt(collisionScope, layout.x, layout.y, layout.w, layout.h)) {
			ElMessage.warning(t('lz.message.cctvDashboard.invalidCameraPosition'));
			return false;
		}

		const equipmentId = getEquipmentId(equipment);
		if (!equipmentId) {
			ElMessage.warning(t('lz.message.cctvDashboard.cameraIdNotFound'));
			return false;
		}

		const newCamera: VideoMonitoringItem = {
			id: `dashboard-camera-${equipmentId}-${++state.cameraInstanceSeed}`,
			sourceCameraId: equipmentId,
			deviceId: equipment.code || equipmentId,
			ip: equipment.ip || '',
			name: equipment.name || equipment.code || equipmentId,
			location: equipment.location || '',
			status: equipment.status,
			aspect: equipment.aspect || '4:3',
			videoUrl: equipment.videoUrl || '',
			x: layout.x,
			y: layout.y,
			w: layout.w,
			h: layout.h,
			isPlaceholder: false,
		};

		const withoutSlot = slotToReplace ? state.cameras.filter((camera) => camera.id !== slotToReplace.id) : state.cameras;
		state.cameras = [...withoutSlot, newCamera].sort((first, second) => first.y - second.y || first.x - second.x);
		state.selectedSlotId = '';
		return true;
	};

	const readGridSurfaceRect = () => {
		const pane = gridDropRef.value;
		if (!pane) return null;
		const gridEl = pane.querySelector('.camera-grid') as HTMLElement | null;
		return toRectSnapshot((gridEl || pane).getBoundingClientRect());
	};

	const getGridSurfaceRect = () => {
		if (cachedGridRect) return cachedGridRect;
		return readGridSurfaceRect();
	};

	const cacheDropSurfaceBounds = () => {
		const pane = gridDropRef.value;
		if (!pane) return;
		cachedGridRect = readGridSurfaceRect();
		cachedParentRect = toRectSnapshot(pane.getBoundingClientRect());
	};

	const updateDropSurfaceBounds = () => {
		const rect = getGridSurfaceRect();
		if (!rect || !gridDropRef.value) {
			state.dropSurface = { left: 0, top: 0, width: 0, height: 0 };
			return;
		}
		const parentRect = cachedParentRect || toRectSnapshot(gridDropRef.value.getBoundingClientRect());
		const nextSurface = {
			left: rect.left - parentRect.left,
			top: rect.top - parentRect.top,
			width: rect.width,
			height: rect.height,
		};
		if (
			state.dropSurface.left === nextSurface.left &&
			state.dropSurface.top === nextSurface.top &&
			state.dropSurface.width === nextSurface.width &&
			state.dropSurface.height === nextSurface.height
		) {
			return;
		}
		state.dropSurface = {
			...nextSurface,
		};
	};

	const getGridCellAtPoint = (clientX: number, clientY: number) => {
		const rect = getGridSurfaceRect();
		if (!rect) return null;
		if (clientX < rect.left || clientX > rect.right || clientY < rect.top || clientY > rect.bottom) return null;
		const colWidth = rect.width / dashboardGridConfig.column;
		const rowHeight = rect.height / dashboardGridConfig.row;
		const x = Math.min(dashboardGridConfig.column - 1, Math.max(0, Math.floor((clientX - rect.left) / Math.max(colWidth, 1))));
		const y = Math.min(dashboardGridConfig.row - 1, Math.max(0, Math.floor((clientY - rect.top) / Math.max(rowHeight, 1))));
		return { x, y };
	};

	const getDropPlaceholderAtPoint = (clientX: number, clientY: number) => {
		const cell = getGridCellAtPoint(clientX, clientY);
		if (!cell) return null;
		return (
			state.cameras.find(
				(camera) =>
					camera.isPlaceholder &&
					cell.x >= camera.x &&
					cell.x < camera.x + camera.w &&
					cell.y >= camera.y &&
					cell.y < camera.y + camera.h
			) || null
		);
	};

	// Tìm camera thật (không phải placeholder) đang chiếm ô tại điểm thả.
	const getRealCameraAtPoint = (clientX: number, clientY: number) => {
		const cell = getGridCellAtPoint(clientX, clientY);
		if (!cell) return null;
		return (
			state.cameras.find(
				(camera) =>
					!camera.isPlaceholder &&
					cell.x >= camera.x &&
					cell.x < camera.x + camera.w &&
					cell.y >= camera.y &&
					cell.y < camera.y + camera.h
			) || null
		);
	};

	const getDroppedLayoutAtPoint = (clientX: number, clientY: number) => {
		const rect = getGridSurfaceRect();
		if (!rect) return null;
		if (clientX < rect.left || clientX > rect.right || clientY < rect.top || clientY > rect.bottom) return null;
		const colWidth = rect.width / dashboardGridConfig.column;
		const rowHeight = rect.height / dashboardGridConfig.row;
		const itemSize = getDefaultDroppedItemSize();
		const maxX = Math.max(0, dashboardGridConfig.column - itemSize);
		const maxY = Math.max(0, dashboardGridConfig.row - itemSize);
		const x = Math.min(maxX, Math.max(0, Math.floor((clientX - rect.left) / Math.max(colWidth, 1))));
		const y = Math.min(maxY, Math.max(0, Math.floor((clientY - rect.top) / Math.max(rowHeight, 1))));
		return normalizeCameraGridItem({ x, y, w: itemSize, h: itemSize });
	};

	// Thêm camera vào ô trống gần nhất (ưu tiên gần điểm thả), hết chỗ thì báo. Trả về true nếu thêm được.
	const addCameraToEmptySlot = (equipment: DashboardCameraInfo, preferredX?: number, preferredY?: number) => {
		const size = getDefaultDroppedItemSize();
		const nearest =
			preferredX !== undefined && preferredY !== undefined
				? getNearestCameraPosition(state.cameras, size, size, preferredX, preferredY)
				: null;
		const fallback = nearest || getNextCameraPosition(state.cameras, size, size);
		if (!fallback) {
			ElMessage.warning(t('lz.message.cctvDashboard.noSpaceForCamera'));
			return false;
		}
		return addCameraToGrid(equipment, fallback);
	};

	const handleDropByPosition = async (draggedCameraId: string, clientX: number, clientY: number) => {
		if (!draggedCameraId) return;
		const equipment = equipments.value.find((item) => getEquipmentId(item) === draggedCameraId);
		if (!equipment) return;

		// 1. Thả vào ô placeholder trống → điền camera vào ngay.
		const placeholder = getDropPlaceholderAtPoint(clientX, clientY);
		if (placeholder) {
			addCameraToGrid(equipment, { x: placeholder.x, y: placeholder.y, w: placeholder.w, h: placeholder.h }, placeholder);
			return;
		}

		// 2. Thả lên ô đã có camera thật → hỏi người dùng có thay thế không.
		const targetCamera = getRealCameraAtPoint(clientX, clientY);
		if (targetCamera) {
			let replaceConfirmed = false;
			try {
				await ElMessageBox.confirm(
					t('lz.message.cctvDashboard.confirmReplaceCamera', { current: targetCamera.name, replacement: equipment.name }),
					t('lz.button.cctvDashboard.replaceCamera'),
					{
						confirmButtonText: t('lz.button.cctvDashboard.replace'),
						cancelButtonText: t('lz.button.base.no'),
						type: 'warning',
					}
				);
				replaceConfirmed = true;
			} catch {
				replaceConfirmed = false;
			}
			if (replaceConfirmed) {
				// Thay thế camera ngay tại ô đó (giữ nguyên vị trí và kích thước).
				addCameraToGrid(equipment, { x: targetCamera.x, y: targetCamera.y, w: targetCamera.w, h: targetCamera.h }, targetCamera);
				return;
			}
			// Không thay thế → tìm ô trống để thêm vào, hết chỗ thì báo.
			const cell = getGridCellAtPoint(clientX, clientY);
			addCameraToEmptySlot(equipment, cell?.x, cell?.y);
			return;
		}

		// 3. Thả vào vùng trống → tính layout 2x2 tại điểm thả.
		const droppedLayout = getDroppedLayoutAtPoint(clientX, clientY);
		if (!droppedLayout) {
			const cell = getGridCellAtPoint(clientX, clientY);
			addCameraToEmptySlot(equipment, cell?.x, cell?.y);
			return;
		}
		if (addCameraToGrid(equipment, droppedLayout)) return;

		// Không đặt được tại vị trí thả (đè lên camera lân cận) → tìm ô trống gần nhất.
		addCameraToEmptySlot(equipment, droppedLayout.x, droppedLayout.y);
	};

	const onGridDragOver = (event: DragEvent) => {
		if (!state.draggingCameraId) return;
		if (event.dataTransfer) event.dataTransfer.dropEffect = 'copy';
		updateDropSurfaceBounds();
		setDraggingOverGrid(true);

		const placeholder = getDropPlaceholderAtPoint(event.clientX, event.clientY);
		if (placeholder) {
			setDragGhost({ x: placeholder.x, y: placeholder.y, w: placeholder.w, h: placeholder.h });
			return;
		}
		const droppedLayout = getDroppedLayoutAtPoint(event.clientX, event.clientY);
		setDragGhost(droppedLayout ? { x: droppedLayout.x, y: droppedLayout.y, w: droppedLayout.w, h: droppedLayout.h } : null);
	};

	const onGridDragLeave = (event: DragEvent) => {
		const relatedTarget = event.relatedTarget as Node | null;
		if (relatedTarget && gridDropRef.value?.contains(relatedTarget)) return;
		setDraggingOverGrid(false);
		setDragGhost(null);
	};

	const onGridDrop = (event: DragEvent) => {
		event.preventDefault();
		event.stopPropagation();
		setDraggingOverGrid(false);
		setDragGhost(null);
		const draggedCameraId = state.draggingCameraId || event.dataTransfer?.getData('text/plain') || '';
		state.draggingCameraId = '';
		state.dragStartPointer = null;
		handleDropByPosition(draggedCameraId, event.clientX, event.clientY);
		clearDragRuntimeCache();
	};

	const isPointInGrid = (clientX: number, clientY: number) => {
		const rect = getGridSurfaceRect();
		if (!rect) return false;
		return clientX >= rect.left && clientX <= rect.right && clientY >= rect.top && clientY <= rect.bottom;
	};

	const onDocumentDragOverCapture = (event: DragEvent) => {
		if (!state.draggingCameraId) return;
		const inside = isPointInGrid(event.clientX, event.clientY);
		setDraggingOverGrid(inside);
		if (!inside) {
			setDragGhost(null);
			return;
		}
		event.preventDefault();
		onGridDragOver(event);
	};

	const onDocumentDropCapture = (event: DragEvent) => {
		if (!state.draggingCameraId) return;
		if (!isPointInGrid(event.clientX, event.clientY)) {
			state.draggingCameraId = '';
			setDraggingOverGrid(false);
			setDragGhost(null);
			state.dragStartPointer = null;
			clearDragRuntimeCache();
			return;
		}
		onGridDrop(event);
	};

	const processManualDragFrame = () => {
		manualDragFrame = 0;
		if (!state.draggingCameraId || !latestManualDragPointer) return;
		const { x, y } = latestManualDragPointer;
		if (!state.draggingCameraId) return;
		if (!state.manualDragging) {
			const start = state.dragStartPointer;
			if (!start) return;
			const movedX = x - start.x;
			const movedY = y - start.y;
			if (movedX * movedX + movedY * movedY < 16) return;
			state.manualDragging = true;
		}
		updateDropSurfaceBounds();
		setDragPointer(x, y);
		const inside = isPointInGrid(x, y);
		setDraggingOverGrid(inside);
		if (!inside) {
			setDragGhost(null);
			return;
		}
		const placeholder = getDropPlaceholderAtPoint(x, y);
		if (placeholder) {
			setDragGhost({ x: placeholder.x, y: placeholder.y, w: placeholder.w, h: placeholder.h });
			return;
		}
		const droppedLayout = getDroppedLayoutAtPoint(x, y);
		setDragGhost(droppedLayout ? { x: droppedLayout.x, y: droppedLayout.y, w: droppedLayout.w, h: droppedLayout.h } : null);
	};

	const onManualDragMove = (event: MouseEvent) => {
		if (!state.draggingCameraId) return;
		latestManualDragPointer = { x: event.clientX, y: event.clientY };
		if (!manualDragFrame) manualDragFrame = window.requestAnimationFrame(processManualDragFrame);
	};

	const onWindowResize = () => {
		cachedGridRect = null;
		cachedParentRect = null;
		updateDropSurfaceBounds();
	};

	const onManualDragUp = (event: MouseEvent) => {
		if (!state.draggingCameraId) return;
		const cameraId = state.draggingCameraId;
		const wasDragging = state.manualDragging;
		const dropX = event.clientX;
		const dropY = event.clientY;
		clearManualDragFrame();
		state.manualDragging = false;
		setDraggingOverGrid(false);
		setDragGhost(null);
		state.draggingCameraId = '';
		state.dragPointer = null;
		state.dragStartPointer = null;
		if (!wasDragging) {
			clearDragRuntimeCache();
			return;
		}
		state.suppressNextPanelClick = true;
		window.setTimeout(() => {
			state.suppressNextPanelClick = false;
		}, 100);
		if (cameraId && isPointInGrid(dropX, dropY)) handleDropByPosition(cameraId, dropX, dropY);
		clearDragRuntimeCache();
	};

	const startManualDrag = (equipmentId: string, event: MouseEvent) => {
		if (event.button !== 0) return;
		clearDragRuntimeCache();
		cacheDropSurfaceBounds();
		updateDropSurfaceBounds();
		state.manualDragging = false;
		state.draggingCameraId = equipmentId;
		setDragPointer(event.clientX, event.clientY);
		state.dragStartPointer = { x: event.clientX, y: event.clientY };
	};

	const addCameraFromPanel = (equipmentId: string) => {
		if (state.suppressNextPanelClick) {
			state.suppressNextPanelClick = false;
			return;
		}
		if (state.addingCameraId) return;
		const equipment = equipments.value.find((item) => getEquipmentId(item) === equipmentId);
		if (!equipment) return;
		if (!state.selectedSlotId) {
			ElMessage.warning(t('lz.message.cctvDashboard.selectPlaceholderFirst'));
			return;
		}
		const selectedSlot = state.cameras.find((camera) => camera.id === state.selectedSlotId && camera.isPlaceholder);
		if (!selectedSlot) {
			ElMessage.warning(t('lz.message.cctvDashboard.placeholderNoLongerValid'));
			state.selectedSlotId = '';
			return;
		}
		state.addingCameraId = equipmentId;
		addCameraToGrid(equipment, { x: selectedSlot.x, y: selectedSlot.y, w: selectedSlot.w, h: selectedSlot.h }, selectedSlot);
		state.addingCameraId = '';
	};

	const removeCameraLocal = (cameraId: string) => {
		state.cameras = state.cameras.filter((camera) => camera.id !== cameraId);
	};

	const updateCameraLayout = (cameras: VideoMonitoringItem[]) => {
		const layoutById = new Map(cameras.map((camera) => [camera.id, clampToGrid(camera)]));
		if (layoutById.size === 0) return;
		state.cameras = state.cameras.map((camera) => {
			const updated = layoutById.get(camera.id);
			if (!updated) return camera;
			return { ...camera, x: updated.x, y: updated.y, w: updated.w, h: updated.h };
		});
	};

	onMounted(() => {
		updateDropSurfaceBounds();
		document.addEventListener('dragover', onDocumentDragOverCapture, true);
		document.addEventListener('drop', onDocumentDropCapture, true);
		window.addEventListener('mousemove', onManualDragMove);
		window.addEventListener('mouseup', onManualDragUp);
		window.addEventListener('resize', onWindowResize);
	});

	onBeforeUnmount(() => {
		clearDragRuntimeCache();
		document.removeEventListener('dragover', onDocumentDragOverCapture, true);
		document.removeEventListener('drop', onDocumentDropCapture, true);
		window.removeEventListener('mousemove', onManualDragMove);
		window.removeEventListener('mouseup', onManualDragUp);
		window.removeEventListener('resize', onWindowResize);
	});

	return {
		gridDropRef,
		state,
		gridGhostStyle,
		gridOverlayStyle,
		dragCursorStyle,
		getEquipmentId,
		getCameraSourceId,
		isEquipmentAdded,
		applyTemplateLayout,
		clearTemplatePlaceholders,
		setSelectedSlotForAdd,
		addCameraFromPanel,
		removeCameraLocal,
		updateCameraLayout,
		startManualDrag,
		onGridDragOver,
		onGridDragLeave,
		onGridDrop,
	};
};
