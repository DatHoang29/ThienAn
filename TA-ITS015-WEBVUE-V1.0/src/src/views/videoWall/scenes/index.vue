<template>
	<div class="videowall-scene-container">
	<el-tabs v-model="state.activeTab" class="vw-scene-tabs" :before-leave="onBeforeTabLeave" @tab-change="onTabChange">
	<!-- Kịch bản hiển thị: soạn cửa sổ trên canvas cho kịch bản đang chọn -->
	<el-tab-pane name="editor">
		<template #label>
			<span class="vw-tab-label"><el-icon><ele-ArrowRight /></el-icon>{{ $t('lz.label.videoWall.sceneEditorTab') }}</span>
		</template>
	<div class="scene-editor-page">
		<!-- Header: tiêu đề · trạng thái thay đổi + Bỏ qua / Lưu (như wallLayout; Thêm mới nằm cạnh ô chọn kịch bản) -->
		<div class="scene-editor-header">
			<span class="scene-editor-title">{{ $t('lz.label.videoWall.sceneManagement') }}</span>
			<div v-if="state.activeSceneId" class="scene-editor-header-right">
				<span class="scene-editor-change" :class="{ 'is-dirty': state.isDirty }">
					<i />{{ state.isDirty ? $t('lz.label.videoWall.unsavedChanges') : $t('lz.label.videoWall.noChanges') }}
				</span>
				<!-- Luôn bật: với kịch bản mới chưa lưu, Bỏ qua dùng để bỏ hẳn kịch bản -->
				<el-button icon="ele-RefreshLeft" :disabled="state.isLoading" @click="handleCancel">{{ $t('lz.button.base.cancel') }}</el-button>
				<el-button type="primary" icon="ele-Select" v-auth="'videoWallScene:update'" :disabled="!state.isDirty" :loading="state.isLoading" @click="handleSave">
					{{ $t('lz.button.base.save') }}
				</el-button>
			</div>
		</div>

		<!-- 3 cột: cấu hình (trái) · canvas (giữa) · nguồn (phải); 2 cột bên kéo đổi rộng / thu gọn bằng thanh chia -->
		<div ref="editorBodyRef" class="scene-editor-body">
			<!-- Left: Chọn kịch bản + Thông tin kịch bản + Thuộc tính cửa sổ đang chọn -->
			<div :id="inspectorPanel.id" class="scene-editor-inspector" :style="inspectorPanel.panelStyle">
				<SceneList :scenes="state.scenes" :active-scene-id="isNewScene ? '' : state.activeSceneId" :is-new="isNewScene"
					:current-topology-id="topology.topology?.id" :controller-labels="controllerLabels" :can-set-default="myPerm.isSuperAdmin"
					@select="handleSelectScene" @duplicate="handleDuplicate" @delete="handleDeleteScene"
					@set-default="handleSetDefault" @create="handleCreateNew" />
				<el-divider class="scene-editor-inspector-divider" />
				<template v-if="state.activeSceneId">
					<SceneToolbar :scene-code="state.editingScene.code ?? ''" :scene-name="state.editingScene.name ?? ''" :scene-remark="state.editingScene.remark ?? ''"
						:info-readonly="sceneInfoLocked || isOldLayout" :code-readonly="!isNewScene" :controller-id="state.editingScene.controllerId ?? ''"
						:controllers="vwCtrl.controllers"
						@update:scene-code="handleSceneCodeChange" @update:scene-name="handleSceneNameChange" @update:scene-remark="handleSceneRemarkChange" @update:controller-id="handleSceneControllerChange" />
					<el-divider class="scene-editor-inspector-divider" />
					<WindowProperties v-if="selectedWindow && !isOldLayout" :window="selectedWindow"
						:source="selectedSource"
						@update="handleUpdateWindowProps" :is-top-layer="selectedLayerEdge.top" :is-bottom-layer="selectedLayerEdge.bottom" @layer="handleLayer"
						@delete="handleDeleteSelectedWindow" @toggle-lock="handleToggleLock" />
					<div v-else class="scene-editor-inspector-hint">
						<el-icon size="28"><ele-Pointer /></el-icon>
						<span>{{ isOldLayout ? $t('lz.message.videoWall.sceneOldLayoutReadonly') : $t('lz.label.videoWall.selectWindowHint') }}</span>
					</div>
				</template>
				<div v-else class="scene-editor-inspector-hint">
					<el-icon size="28"><ele-Film /></el-icon>
					<span>{{ $t('lz.label.videoWall.selectSceneHint') }}</span>
				</div>
			</div>

			<PanelSplitter side="left" class="scene-editor-splitter" :label="$t('lz.label.videoWall.sceneInfo')" :controls="inspectorPanel.id"
				:width="inspectorPanel.size" :min="inspectorPanel.min" :max="inspectorPanel.max" :expanded="inspectorPanel.visible"
				:expand-label="$t('lz.label.base.extend')" :collapse-label="$t('lz.button.base.collapse')"
				:resize-hint="$t('lz.label.base.resizePanel', { name: $t('lz.label.videoWall.sceneInfo') })"
				@resize="inspectorPanel.resize" @step="inspectorPanel.step" />

			<!-- Center: Canvas -->
			<div class="scene-editor-center">
				<div class="scene-editor-canvas-area">
					<!-- Kịch bản thuộc phiên bản bố cục khác: vẽ theo bố cục của nó, chỉ xem -->
					<el-alert v-if="isOldLayout" class="scene-editor-old-layout" type="warning" :closable="false" show-icon :title="$t('lz.message.videoWall.sceneOldLayoutReadonly')" />
					<SceneCanvas v-if="state.activeSceneId" :windows="state.editingWindows"
						:selected-window-id="state.selectedWindowId"
						:layout="state.sceneLayout" :readonly="isOldLayout" zoomable
						:sources="state.sources" @select-window="handleSelectWindow"
						@update-window="handleUpdateWindow" @delete-window="handleDeleteWindow"
						@drop-source="handleDropSource" />
					<div v-else class="scene-editor-empty">
						<el-empty :description="$t('lz.label.videoWall.selectSceneHint')" />
					</div>
				</div>
			</div>

			<PanelSplitter side="right" class="scene-editor-splitter" :label="$t('lz.entity.videoWall.source')" :controls="palettePanel.id"
				:width="palettePanel.size" :min="palettePanel.min" :max="palettePanel.max" :expanded="palettePanel.visible"
				:expand-label="$t('lz.label.base.extend')" :collapse-label="$t('lz.button.base.collapse')"
				:resize-hint="$t('lz.label.base.resizePanel', { name: $t('lz.entity.videoWall.source') })"
				@resize="palettePanel.resize" @step="palettePanel.step" />

			<!-- Right: Source Palette -->
			<div :id="palettePanel.id" class="scene-editor-palette" :style="palettePanel.panelStyle">
				<SourcePalette :sources="state.sources" :used-source-ids="usedSourceIds" :highlight-id="selectedWindow?.sourceId ?? ''" />
			</div>
		</div>

	</div>
	</el-tab-pane>
	<!-- Danh sách kịch bản: mọi bố cục, trạng thái Dùng / Không dùng, số cửa sổ, đang phát… — quản lý dạng bảng -->
	<el-tab-pane name="list" lazy>
		<template #label>
			<span class="vw-tab-label"><el-icon><ele-ArrowRight /></el-icon>{{ $t('lz.label.videoWall.sceneListTab') }}</span>
		</template>
		<SceneManageList ref="sceneListRef" :current-topology-id="topology.topology?.id" :can-set-default="myPerm.isSuperAdmin"
			@open="onListOpen" @create="onListCreate" @duplicate="handleDuplicate" @set-default="onListSetDefault" @delete="onListDelete" @batch-delete="onListBatchDelete"
			@changed="onListChanged" />
	</el-tab-pane>
	</el-tabs>

		<!-- Dialog nhân bản kịch bản: cho sửa cả mã và tên kịch bản mới -->
		<el-dialog v-model="state.duplicate.visible" width="460px" :close-on-click-modal="false" draggable>
			<template #header>
				<div style="color: #fff">
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-CopyDocument /> </el-icon>
					<span> {{ $t('lz.label.videoWall.duplicateTitle', { name: state.duplicate.scene?.name ?? '' }) }} </span>
				</div>
			</template>
			<el-form label-width="auto" label-position="left" autocomplete="off" @submit.prevent>
				<el-form-item :label="$t('lz.entity.base.code')" required>
					<el-input v-model="state.duplicate.code" :maxlength="CODE_MAX_LENGTH" clearable
						:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.code') })" />
				</el-form-item>
				<el-form-item :label="$t('lz.entity.base.name')" required>
					<el-input v-model="state.duplicate.name" maxlength="64" clearable
						:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.name') })" />
				</el-form-item>
			</el-form>
			<template #footer>
				<el-button @click="state.duplicate.visible = false">{{ $t('lz.button.base.cancel') }}</el-button>
				<el-button type="primary" :loading="state.duplicate.loading" @click="confirmDuplicate">{{ $t('lz.button.videoWall.duplicate') }}</el-button>
			</template>
		</el-dialog>
	</div>
</template>

<script lang="ts" setup name="videoWallScene">
import { useI18n } from 'vue-i18n';
import { computed, onActivated, onMounted, reactive, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { getAPI } from '/@/utils/axios-utils';
import { VwSceneApi, VwWindowSceneApi, VwSourceApi, VwWallTopologyApi, DefaultEnum, StatusEnum, ActiveScene, SceneWindowVisible } from '/@/api-services/videoWall';
import type { VwSceneOutput, VwSourceOutput, VwWindowSceneOutput, VwAddWindowSceneInput, VwWallTopologyOutput } from '/@/api-services/videoWall';
import { toEditWindow, type EditWindow } from '/@/utils/videoWall/sceneWindow';
import { fetchActiveScene, pickStartScene } from '/@/utils/videoWall/sceneStart';
import { useVideoWallController } from '/@/stores/its/videoWallController';
import { useVideoWallMyPermission } from '/@/stores/its/videoWallMyPermission';
import { useVideoWallTopology } from '/@/stores/its/videoWallTopology';
import SceneList from './component/sceneList.vue';
import SceneCanvas from './component/sceneCanvas.vue';
import SourcePalette from './component/sourcePalette.vue';
import PanelSplitter from '/@/components/workspace/PanelSplitter.vue';
import { useSidePanel } from '/@/components/workspace/useSidePanel';
import WindowProperties from './component/windowProperties.vue';
import SceneToolbar from './component/sceneToolbar.vue';
import SceneManageList from './component/sceneManageList.vue';
const { t: $t } = useI18n();

/** Id giả cho kịch bản đang tạo mới (chưa lưu xuống BE). */
const NEW_SCENE_ID = '__new__';
/** Số bản ghi tối đa nạp một lần qua `/page`. */
const PAGE_ALL = 500;

/** Danh sách bộ điều khiển để gán cho kịch bản — dùng chung qua store. */
const vwCtrl = useVideoWallController();
/** Quyền vùng màn hình của tài khoản hiện tại (hỗ trợ giao diện; BE kiểm tra lại khi lưu). */
const myPerm = useVideoWallMyPermission();
const isWindowAllowed = (w: Pick<EditWindow, 'blockNo' | 'x' | 'y' | 'w' | 'h'>) => myPerm.isRectAllowed(w.blockNo, w.x, w.y, w.w, w.h);
/** Bố cục tường (khối, kích thước ô) — lưới canvas lấy từ đây, không theo từng kịch bản. */
const topology = useVideoWallTopology();

/** 2 cột bên của màn soạn kịch bản: kéo đổi rộng / thu gọn (chừa chỗ cho canvas và cột còn lại). */
const editorBodyRef = ref<HTMLElement>();
const inspectorPanel = useSidePanel('vw-scene-inspector', editorBodyRef, { width: 300, min: 240, maxRatio: 0.35, reserve: 720 });
const palettePanel = useSidePanel('vw-scene-palette', editorBodyRef, { width: 260, min: 200, maxRatio: 0.3, reserve: 760 });

const sceneListRef = ref<InstanceType<typeof SceneManageList>>();

const state = reactive({
	/** Tab đang mở: soạn kịch bản (canvas) hoặc danh sách kịch bản (bảng). */
	activeTab: 'editor' as 'editor' | 'list',
	scenes: [] as VwSceneOutput[],
	sources: [] as VwSourceOutput[],
	activeSceneId: '' as string,
	editingScene: {} as VwSceneOutput,
	editingWindows: [] as EditWindow[],
	selectedWindowId: '' as string,
	isDirty: false,
	/** Đang lưu kịch bản + cửa sổ (hiện loading ở nút Lưu) */
	isLoading: false,
	/** Bố cục của kịch bản đang mở khi nó thuộc phiên bản bố cục khác (null = bố cục đang dùng) */
	sceneLayout: null as VwWallTopologyOutput | null,
	/** Dialog nhân bản kịch bản */
	duplicate: {
		visible: false,
		loading: false,
		scene: null as VwSceneOutput | null,
		code: '',
		name: '',
	},
});

const isNewScene = computed(() => state.activeSceneId === NEW_SCENE_ID);

/** Kịch bản đang mở dựng trên phiên bản bố cục khác bố cục đang dùng ⇒ chỉ xem (BE chặn sửa / áp dụng). */
const isOldLayout = computed(
	() => !!state.activeSceneId && !isNewScene.value && !!topology.topology?.id && state.editingScene.wallTopologyId !== topology.topology.id
);

/** Nạp bố cục của kịch bản đang mở khi nó thuộc bố cục cũ, để canvas vẽ đúng lưới mà toạ độ cửa sổ dựa vào. */
const resolveSceneLayout = async () => {
	const id = state.editingScene.wallTopologyId;
	if (!isOldLayout.value || !id) {
		state.sceneLayout = null;
		return;
	}
	try {
		const res = await getAPI(VwWallTopologyApi).apiVideowallVwwalltopologyByidGet(id);
		state.sceneLayout = res.data.result ?? null;
	} catch {
		state.sceneLayout = null;
	}
};

/** Cửa sổ nằm trọn trong một khối của bố cục đang dùng (dùng khi nhân bản kịch bản bố cục cũ sang bố cục hiện tại). */
const fitsCurrentLayout = (w: VwWindowSceneOutput) => {
	const block = topology.blocks.find((b) => (b.blockNo ?? 0) === (w.blockNo ?? 0));
	if (!block) return false;
	const x = w.x ?? 0;
	const y = w.y ?? 0;
	return x >= 0 && y >= 0 && (w.w ?? 0) > 0 && (w.h ?? 0) > 0 && x + (w.w ?? 0) <= (block.cols ?? 0) * topology.panelW && y + (w.h ?? 0) <= (block.rows ?? 0) * topology.panelH;
};

/**
 * Kịch bản phủ ngoài vùng được phân quyền (VD kịch bản ABCD, user chỉ có vùng A):
 * không sửa được mã / tên / ghi chú (BE chặn), chỉ sửa được cửa sổ trong vùng của mình.
 */
const sceneInfoLocked = computed(() => {
	if (isNewScene.value || myPerm.isFull) return false;
	return myPerm.isNone || state.editingWindows.some((w) => !isWindowAllowed(w));
});

/** Các trường thông tin kịch bản được gửi qua API Update — dùng để biết có cần gọi Update không. */
const sceneInfoKey = (s: Partial<VwSceneOutput>) =>
	JSON.stringify([s.code ?? '', s.name ?? '', s.remark ?? null, s.controllerId ?? null, s.status ?? null]);

const controllerLabels = computed(() => {
	const labels: Record<string, string> = {};
	for (const c of vwCtrl.controllers) if (c.id) labels[c.id] = c.name || c.code || c.id;
	return labels;
});

// Computed: cửa sổ đang chọn
const selectedWindow = computed(() => {
	return state.editingWindows.find((w) => w.id === state.selectedWindowId);
});

/** Nguồn đã có cửa sổ trong kịch bản đang soạn — mỗi nguồn chỉ dùng ở 1 cửa sổ (BE cũng chặn). */
const usedSourceIds = computed(() => [...new Set(state.editingWindows.map((w) => w.sourceId).filter((id): id is string => !!id))]);

/** Cửa sổ đang chọn đã nằm trên cùng / dưới cùng chưa (không cửa sổ nào khác cao hơn / thấp hơn hoặc bằng). */
const selectedLayerEdge = computed(() => {
	const w = selectedWindow.value;
	const others = state.editingWindows.filter((o) => o.id !== w?.id);
	if (!w || !others.length) return { top: true, bottom: true };
	return { top: others.every((o) => o.zIndex < w.zIndex), bottom: others.every((o) => o.zIndex > w.zIndex) };
});

// Computed: source của cửa sổ đang chọn
const selectedSource = computed(() => {
	if (!selectedWindow.value) return undefined;
	return state.sources.find((s) => s.id === selectedWindow.value!.sourceId || s.code === selectedWindow.value!.sourceId);
});

// ── Load data ──
/** Chỉ kịch bản của bố cục đang dùng — kịch bản bố cục cũ ẩn hẳn (đổi bố cục coi như bắt đầu mới). */
const loadScenes = async () => {
	try {
		await topology.load();
		const currentId = topology.topology?.id;
		const res = await getAPI(VwSceneApi).apiVideowallVwscenePagePost({ page: 1, pageSize: PAGE_ALL, wallTopologyId: currentId || undefined, field: 'orderNo', order: 'asc' });
		state.scenes = (res.data.result?.records ?? []).filter((s) => s.wallTopologyId === currentId);
	} catch {
		ElMessage.error($t('lz.message.videoWall.loadScenesFailed'));
	}
};

const loadSources = async () => {
	try {
		const res = await getAPI(VwSourceApi).apiVideowallVwsourcePagePost({ page: 1, pageSize: PAGE_ALL, field: 'orderNo', order: 'asc' });
		state.sources = res.data.result?.records ?? [];
	} catch {
		ElMessage.error($t('lz.message.videoWall.loadSourcesFailed'));
	}
};

/** Cửa sổ của một kịch bản (BE có thể bỏ qua filter sceneId ⇒ lọc lại ở client). */
const fetchSceneWindows = async (sceneId: string): Promise<VwWindowSceneOutput[]> => {
	const res = await getAPI(VwWindowSceneApi).apiVideowallVwwindowscenePagePost({ page: 1, pageSize: PAGE_ALL, sceneId });
	return (res.data.result?.records ?? []).filter((w) => w.sceneId === sceneId);
};

const loadSceneWindows = async (sceneId: string) => {
	try {
		const list = await fetchSceneWindows(sceneId);
		state.editingWindows = list.map(toEditWindow).sort((a, b) => a.zIndex - b.zIndex || (a.orderNo ?? 0) - (b.orderNo ?? 0));
	} catch {
		state.editingWindows = [];
	}
};

const loadControllers = async () => {
	try {
		await vwCtrl.load();
	} catch {
		ElMessage.error($t('lz.message.videoWall.loadControllersFailed'));
	}
};

/** BE `add` trả void ⇒ nạp lại danh sách rồi tra kịch bản vừa tạo theo mã. */
const findSceneIdByCode = async (code: string): Promise<string> => {
	await loadScenes();
	const id = state.scenes.find((s) => s.code === code)?.id;
	if (!id) throw new Error($t('lz.message.videoWall.sceneNotFoundAfterCreate', { code }));
	return id;
};

/** Độ dài tối đa mã kịch bản / mã cửa sổ — khớp BE (Length64). */
const CODE_MAX_LENGTH = 64;

/** Ghép mã cửa sổ `<mã kịch bản>-W<n>`, cắt bớt phần mã kịch bản để tổng không vượt CODE_MAX_LENGTH. */
const makeWindowCode = (sceneCode: string, n: number): string => {
	const suffix = `-W${n}`;
	return `${sceneCode.slice(0, CODE_MAX_LENGTH - suffix.length)}${suffix}`;
};

/** Mã cửa sổ: `<mã kịch bản>-W<n>`, n tiếp nối số lớn nhất đang có. */
const nextWindowCode = (sceneCode: string, used: Set<string>): string => {
	let n = used.size + 1;
	while (used.has(makeWindowCode(sceneCode, n))) n++;
	const code = makeWindowCode(sceneCode, n);
	used.add(code);
	return code;
};

onMounted(async () => {
	// Nạp lại quyền mỗi lần vào trang để nhận ngay thay đổi phân quyền
	await Promise.all([loadScenes(), loadSources(), loadControllers(), myPerm.load(true), topology.load(true)]);
	await selectDefaultScene();
});

/**
 * Vào trang luôn mở sẵn 1 kịch bản theo cùng quy tắc với Giám sát & điều khiển (`pickStartScene`):
 * đang phát → mặc định → kịch bản đầu tiên. Bố cục chưa có kịch bản ⇒ để trống.
 */
const selectDefaultScene = async () => {
	if (state.activeSceneId || !state.scenes.length) return;
	const scene = pickStartScene(state.scenes, await fetchActiveScene());
	if (scene) await handleSelectScene(scene);
};

/** Đóng kịch bản đang mở (đã xoá / không còn thuộc bố cục đang dùng) rồi mở kịch bản mặc định. */
const closeScene = async () => {
	state.activeSceneId = '';
	state.editingScene = {};
	state.editingWindows = [];
	state.selectedWindowId = '';
	state.isDirty = false;
	await selectDefaultScene();
};

/**
 * Trang được giữ sống (keep-alive) ⇒ quay lại trang thì nạp lại bố cục tường, quyền vùng và bộ điều khiển
 * để canvas khớp thay đổi vừa lưu ở trang Màn hình / Phân quyền / Bộ điều khiển.
 * Không đụng tới cửa sổ đang soạn. Lần kích hoạt đầu đi cùng onMounted (đã nạp) ⇒ bỏ qua.
 */
let firstActivation = true;
onActivated(() => {
	if (firstActivation) {
		firstActivation = false;
		return;
	}
	void Promise.all([topology.load(true), myPerm.load(true), vwCtrl.load(true)])
		// Bố cục đang dùng có thể vừa đổi ⇒ nạp lại danh sách; kịch bản đang mở thuộc bố cục cũ thì đóng lại
		.then(async () => {
			await loadScenes();
			if (state.activeSceneId && !isNewScene.value && !state.scenes.some((s) => s.id === state.activeSceneId)) await closeScene();
			await resolveSceneLayout();
		})
		.catch(() => {});
});

/** Hỏi bỏ thay đổi chưa lưu; trả `false` nếu người dùng huỷ. */
const confirmDiscard = async (): Promise<boolean> => {
	if (!state.isDirty) return true;
	try {
		await ElMessageBox.confirm($t('lz.message.videoWall.confirmDiscardChanges'), $t('lz.label.base.warning'), {
			confirmButtonText: $t('lz.button.videoWall.discardChanges'),
			cancelButtonText: $t('lz.button.base.cancel'),
			type: 'warning',
		});
		return true;
	} catch {
		return false;
	}
};

// ── Scene list interactions ──
const handleSelectScene = async (scene: VwSceneOutput) => {
	if (!(await confirmDiscard())) return;
	state.activeSceneId = scene.id ?? '';
	state.editingScene = { ...scene };
	state.selectedWindowId = '';
	state.isDirty = false;
	await Promise.all([loadSceneWindows(state.activeSceneId), resolveSceneLayout()]);
	// Chọn kịch bản ở trang này chỉ để CHỈNH SỬA, không đổi kịch bản đang phát trên tường.
	// Kịch bản đang phát do BE quản (`/vwscene/active`) và chỉ đổi qua hành động kích hoạt.
};

/** Tạo kịch bản mới trên canvas; chỉ ghi xuống BE khi bấm Lưu (cần nhập mã + tên). */
const handleCreateNew = async () => {
	if (!(await confirmDiscard())) return;
	state.activeSceneId = NEW_SCENE_ID;
	state.editingScene = {
		id: '',
		code: '',
		name: $t('lz.label.videoWall.newSceneName'),
		// Tường phân cấp: kịch bản áp cho cả tường qua bộ trung tâm ⇒ không gán bộ điều khiển
		controllerId: null,
		isDefault: DefaultEnum.NUMBER_0,
		status: StatusEnum.NUMBER_1,
		activeScene: ActiveScene.NUMBER_0,
		orderNo: 100,
	};
	state.editingWindows = [];
	state.selectedWindowId = '';
	state.sceneLayout = null;
	state.isDirty = true;
};

const handleDuplicate = async (scene: VwSceneOutput) => {
	// Phân quyền vùng màn hình: có cửa sổ ngoài vùng được cấp ⇒ chặn hẳn, không sao chép một phần
	try {
		const srcWindows = await fetchSceneWindows(scene.id ?? '');
		if (myPerm.isNone || srcWindows.some((w) => !isWindowAllowed(toEditWindow(w)))) {
			ElMessage.warning(myPerm.isNone ? myPerm.deniedMessage() : $t('lz.message.videoWall.sceneOutsideCannotDuplicate'));
			return;
		}
	} catch {
		ElMessage.error($t('lz.message.videoWall.loadWindowsFailed'));
		return;
	}
	// Mở dialog với giá trị gợi ý; người dùng sửa được cả mã và tên trước khi nhân bản
	state.duplicate.scene = scene;
	state.duplicate.code = `${scene.code ?? ''}-COPY`;
	state.duplicate.name = `${scene.name ?? ''} (copy)`;
	state.duplicate.loading = false;
	state.duplicate.visible = true;
};

/** Xác nhận nhân bản: tạo kịch bản mới với mã + tên đã nhập, rồi sao chép toàn bộ cửa sổ. */
const confirmDuplicate = async () => {
	const scene = state.duplicate.scene;
	if (!scene) return;
	const newCode = state.duplicate.code.trim();
	const newName = state.duplicate.name.trim();
	if (!newCode) {
		ElMessage.warning($t('lz.message.videoWall.codeRequired'));
		return;
	}
	if (newCode.length > CODE_MAX_LENGTH) {
		ElMessage.warning($t('lz.validation.base.maxlength', { PropertyName: $t('lz.entity.base.code'), MaxLength: CODE_MAX_LENGTH }));
		return;
	}
	if (!newName) {
		ElMessage.warning($t('lz.message.videoWall.enterSceneName'));
		return;
	}
	state.duplicate.loading = true;
	try {
		// Bản sao luôn thuộc bố cục đang dùng (BE gán). Nhân bản kịch bản bố cục cũ: chỉ chép cửa sổ nằm trọn
		// trong khối cùng số của bố cục hiện tại, bỏ khối cũ (wallBlockId) để BE gắn khối mới theo blockNo.
		const all = await fetchSceneWindows(scene.id ?? '');
		const windows = all.filter(fitsCurrentLayout).map((w) => ({ ...w, wallBlockId: null }));
		const skipped = all.length - windows.length;
		await getAPI(VwSceneApi).apiVideowallVwsceneAddPost({
			...scene,
			id: '',
			code: newCode,
			name: newName,
			isDefault: DefaultEnum.NUMBER_0,
			activeScene: ActiveScene.NUMBER_0,
		});
		const newId = await findSceneIdByCode(newCode);
		const used = new Set<string>();
		for (const w of windows) {
			await getAPI(VwWindowSceneApi).apiVideowallVwwindowsceneAddPost({ ...w, id: '', code: nextWindowCode(newCode, used), sceneId: newId });
		}
		ElMessage.success($t('lz.message.videoWall.duplicated', { name: scene.name }));
		if (skipped > 0) ElMessage.warning($t('lz.message.videoWall.duplicateSkippedWindows', { count: skipped }));
		sceneListRef.value?.reload();
		state.duplicate.visible = false;
		const dup = state.scenes.find((s) => s.id === newId);
		state.isDirty = false;
		if (dup) await handleSelectScene(dup);
	} catch {
		ElMessage.error($t('lz.message.videoWall.duplicateFailed'));
	} finally {
		state.duplicate.loading = false;
	}
};

/** Xoá kịch bản — BE kiểm quyền vùng (mọi cửa sổ phải trong vùng được cấp) và xoá luôn cửa sổ. */
const handleDeleteScene = async (scene: VwSceneOutput) => {
	try {
		await ElMessageBox.confirm($t('lz.message.videoWall.confirmDeleteScene', { name: scene.name }), $t('lz.label.base.warning'), {
			confirmButtonText: $t('lz.button.base.delete'),
			cancelButtonText: $t('lz.button.base.cancel'),
			type: 'warning',
		});
	} catch {
		return;
	}
	try {
		await getAPI(VwSceneApi).apiVideowallVwsceneDeletePost({ id: scene.id! });
	} catch {
		// Lỗi (VD cửa sổ ngoài vùng được cấp) do BE trả về đã hiện qua interceptor như các trang khác
		return;
	}
	await loadScenes();
	if (state.activeSceneId === scene.id) await closeScene();
	ElMessage.success($t('lz.message.base.deleteSuccess'));
};

/** Chỉ một kịch bản mặc định: bỏ cờ ở các kịch bản khác trước, rồi đặt cờ cho kịch bản được chọn. */
const handleSetDefault = async (scene: VwSceneOutput) => {
	// Kịch bản mặc định là cài đặt của cả tường ⇒ chỉ SuperAdmin; BE bỏ cờ ở kịch bản khác trong cùng 1 lần gọi
	if (!myPerm.isSuperAdmin) {
		ElMessage.warning($t('lz.message.videoWall.setDefaultSuperAdminOnly'));
		return;
	}
	try {
		await ElMessageBox.confirm($t('lz.message.videoWall.confirmSetDefault', { name: scene.name }), $t('lz.label.base.warning'), {
			confirmButtonText: $t('lz.button.base.confirm'),
			cancelButtonText: $t('lz.button.base.cancel'),
			type: 'warning',
		});
	} catch {
		return;
	}
	try {
		await getAPI(VwSceneApi).apiVideowallVwsceneSetdefaultPost({ id: scene.id! });
		await loadScenes();
		ElMessage.success($t('lz.message.videoWall.setDefaultSuccess', { name: scene.name }));
	} catch {
		ElMessage.error($t('lz.message.videoWall.setDefaultFailed'));
	}
};

/* ── Tab Danh sách kịch bản ── */

/** Rời tab soạn kịch bản khi còn thay đổi chưa lưu ⇒ hỏi trước. */
const onBeforeTabLeave = async (_name: string | number, oldName: string | number) => (oldName === 'editor' ? confirmDiscard() : true);

/** Đang mở kịch bản từ danh sách ⇒ onTabChange không tự đóng / chọn lại kịch bản. */
let openingFromList = false;

/** Đổi tab ⇒ nạp lại tab vừa mở để thấy thay đổi làm ở tab kia. */
const onTabChange = async (name: string | number) => {
	if (name === 'list') {
		sceneListRef.value?.reload();
		return;
	}
	if (openingFromList) return;
	// Bật / tắt / xoá ở danh sách ⇒ ô chọn kịch bản nạp lại; kịch bản đang mở không còn thì mở kịch bản khác
	await loadScenes();
	if (state.activeSceneId && !isNewScene.value && !state.scenes.some((s) => s.id === state.activeSceneId)) await closeScene();
};

/** Mở kịch bản từ danh sách ⇒ sang tab soạn (đã hỏi bỏ thay đổi khi rời tab). Kịch bản bố cục cũ mở ở chế độ chỉ xem. */
const onListOpen = async (scene: VwSceneOutput) => {
	openingFromList = true;
	try {
		state.isDirty = false;
		state.activeTab = 'editor';
		await loadScenes();
		await handleSelectScene(scene);
	} finally {
		openingFromList = false;
	}
};

/** Thêm mới từ danh sách ⇒ sang tab soạn với kịch bản mới (như nút + cạnh ô chọn kịch bản). */
const onListCreate = async () => {
	openingFromList = true;
	try {
		state.isDirty = false;
		state.activeTab = 'editor';
		await loadScenes();
		await handleCreateNew();
	} finally {
		openingFromList = false;
	}
};

const onListSetDefault = async (scene: VwSceneOutput) => {
	await handleSetDefault(scene);
	sceneListRef.value?.reload();
};

const onListDelete = async (scene: VwSceneOutput) => {
	await handleDeleteScene(scene);
	sceneListRef.value?.reload();
};

/** Xoá nhiều kịch bản — như các trang khác: hỏi 1 lần rồi gọi batchDelete (BE kiểm quyền vùng, xoá luôn cửa sổ). */
const onListBatchDelete = (scenes: VwSceneOutput[]) => {
	ElMessageBox.confirm($t('lz.message.base.confirmBatchDelete', { total: scenes.length }), $t('lz.message.base.warning'), {
		confirmButtonText: $t('lz.button.base.confirm'),
		cancelButtonText: $t('lz.button.base.cancel'),
		type: 'warning',
	})
		.then(async () => {
			await getAPI(VwSceneApi).apiVideowallVwsceneBatchdeletePost(scenes.filter((s) => s.id).map((s) => ({ id: s.id! })));
			sceneListRef.value?.reload();
			await loadScenes();
			if (scenes.some((s) => s.id === state.activeSceneId)) await closeScene();
			ElMessage.success($t('lz.message.base.deleteSuccess'));
		})
		.catch(() => {});
};

const onListChanged = () => loadScenes();

/** Đổi bộ điều khiển sở hữu kịch bản. */
const handleSceneControllerChange = (controllerId: string) => {
	if ((state.editingScene.controllerId ?? '') === controllerId) return;
	state.editingScene.controllerId = controllerId;
	state.isDirty = true;
};

// ── Toolbar actions ──

/**
 * Lưu kịch bản + toàn bộ cửa sổ.
 *
 * BE chưa có endpoint gộp nên KHÔNG nguyên tử. Thứ tự: lưu kịch bản → đối chiếu cửa sổ theo `id`
 * (cập nhật cái còn, thêm cái mới, xoá cái đã bỏ). Kiểm tra cả lô trước để không ghi dở dang.
 */
const handleSave = async () => {
	const scene = state.editingScene;
	if (!scene.code?.trim()) {
		ElMessage.warning($t('lz.message.videoWall.enterSceneCode'));
		return;
	}
	if (!scene.name?.trim()) {
		ElMessage.warning($t('lz.message.videoWall.enterSceneName'));
		return;
	}
	const invalid = state.editingWindows.findIndex((w) => !w.sourceId || !(w.w > 0) || !(w.h > 0));
	if (invalid >= 0) {
		ElMessage.warning($t('lz.message.videoWall.invalidWindow', { index: invalid + 1 }));
		return;
	}
	state.isLoading = true;
	try {
		let sceneId = state.activeSceneId;
		if (isNewScene.value) {
			await getAPI(VwSceneApi).apiVideowallVwsceneAddPost({ ...scene, id: '' });
			sceneId = await findSceneIdByCode(scene.code!);
		} else {
			// Chỉ gọi Update khi thông tin kịch bản thực sự đổi: kịch bản phủ ngoài vùng thì BE chặn Update,
			// nhưng user vẫn phải lưu được thay đổi cửa sổ trong vùng của mình.
			const original = state.scenes.find((s) => s.id === sceneId);
			if (!original || sceneInfoKey(original) !== sceneInfoKey(scene)) {
				await getAPI(VwSceneApi).apiVideowallVwsceneUpdatePost(scene);
			}
		}

		const existing = isNewScene.value ? [] : await fetchSceneWindows(sceneId);
		const existingIds = new Set(existing.map((w) => w.id));
		const usedCodes = new Set(existing.map((w) => w.code ?? ''));
		// Xoá cửa sổ đã bỏ TRƯỚC khi thêm: xoá cửa sổ nguồn X rồi kéo lại nguồn X thì BE không báo trùng nguồn
		const editingIds = new Set(state.editingWindows.map((w) => w.id));
		const stale = existing.filter((w) => !editingIds.has(w.id));
		if (stale.length > 0) {
			await getAPI(VwWindowSceneApi).apiVideowallVwwindowsceneBatchdeletePost(stale.map((w) => ({ id: w.id! })));
		}
		for (const [i, w] of state.editingWindows.entries()) {
			// Bỏ trường chỉ có ở FE trước khi gửi.
			const { locked: _locked, ...body } = w;
			const payload: VwAddWindowSceneInput = { ...body, sceneId, orderNo: i + 1, name: body.name || body.label || undefined };
			if (w.id && existingIds.has(w.id)) {
				// Cửa sổ ngoài vùng được cấp bị khoá trên canvas nên không đổi ⇒ không gửi (BE sẽ từ chối)
				const old = existing.find((e) => e.id === w.id);
				if (old && !isWindowAllowed(toEditWindow(old))) continue;
				await getAPI(VwWindowSceneApi).apiVideowallVwwindowsceneUpdatePost(payload);
			} else {
				// Cửa sổ mới (id tạm `tmp-*`): để BE sinh id, mã theo kịch bản.
				await getAPI(VwWindowSceneApi).apiVideowallVwwindowsceneAddPost({ ...payload, id: '', code: nextWindowCode(scene.code!, usedCodes) });
			}
		}
		state.isDirty = false;
		await loadScenes();
		state.activeSceneId = sceneId;
		state.editingScene = { ...(state.scenes.find((s) => s.id === sceneId) ?? scene) };
		// Nạp lại cửa sổ để lấy id thật của những cửa sổ vừa được tạo
		await loadSceneWindows(sceneId);
		state.selectedWindowId = '';
		ElMessage.success($t('lz.message.base.saveSuccess'));
	} catch {
		ElMessage.error($t('lz.message.base.saveError'));
	} finally {
		state.isLoading = false;
	}
};

const handleCancel = async () => {
	if (state.isDirty) {
		try {
			await ElMessageBox.confirm($t('lz.message.videoWall.confirmDiscardAll'), $t('lz.label.base.warning'), {
				confirmButtonText: $t('lz.button.videoWall.discard'),
				cancelButtonText: $t('lz.button.videoWall.continueEditing'),
				type: 'warning',
			});
		} catch {
			return;
		}
	}
	if (isNewScene.value) {
		// Kịch bản mới chưa lưu ⇒ bỏ hẳn, quay về kịch bản mặc định.
		state.activeSceneId = '';
		state.editingScene = {};
		state.editingWindows = [];
		state.isDirty = false;
		await selectDefaultScene();
	} else if (state.activeSceneId) {
		const original = state.scenes.find((s) => s.id === state.activeSceneId);
		if (original) {
			state.editingScene = { ...original };
			await loadSceneWindows(state.activeSceneId);
		}
	}
	state.selectedWindowId = '';
	state.isDirty = false;
};

const handleSceneCodeChange = (code: string) => {
	if (state.editingScene.code === code) return;
	state.editingScene.code = code;
	state.isDirty = true;
};

/** Ghi chú kịch bản: chuỗi rỗng ⇒ null để BE xoá được ghi chú cũ. */
const handleSceneRemarkChange = (remark: string) => {
	const value = remark?.trim() ? remark : null;
	if ((state.editingScene.remark ?? null) === value) return;
	state.editingScene.remark = value;
	state.isDirty = true;
};

const handleSceneNameChange = (name: string) => {
	if (state.editingScene.name === name) return;
	state.editingScene.name = name;
	state.isDirty = true;
};

// ── Canvas interactions ──
const handleSelectWindow = (windowId: string) => {
	state.selectedWindowId = windowId;
};

const handleUpdateWindow = (updated: EditWindow) => {
	const idx = state.editingWindows.findIndex((w) => w.id === updated.id);
	if (idx >= 0) {
		state.editingWindows[idx] = { ...updated };
		state.isDirty = true;
	}
};

/**
 * Đưa cửa sổ đang chọn lên trên cùng / xuống dưới cùng (biên bản 22/10 mục 1.6).
 * - Lên trên: Z = Z lớn nhất của các cửa sổ khác + 1.
 * - Xuống dưới: Z = Z nhỏ nhất − 1; đã chạm 0 thì đặt 0 và đẩy các cửa sổ khác (thuộc vùng được cấp) lên 1 bậc,
 *   giữ nguyên thứ tự giữa chúng. Cửa sổ ngoài vùng được cấp không bị đụng tới.
 */
const handleLayer = (dir: 'front' | 'back') => {
	const w = selectedWindow.value;
	if (!w) return;
	if (!isWindowAllowed(w)) {
		ElMessage.warning(myPerm.deniedMessage());
		return;
	}
	const others = state.editingWindows.filter((o) => o.id !== w.id);
	if (!others.length) return;

	if (dir === 'front') {
		handleUpdateWindow({ ...w, zIndex: Math.max(...others.map((o) => o.zIndex)) + 1 });
		return;
	}
	const minOther = Math.min(...others.map((o) => o.zIndex));
	if (minOther > 0) {
		handleUpdateWindow({ ...w, zIndex: minOther - 1 });
		return;
	}
	state.editingWindows = state.editingWindows.map((o) => (o.id === w.id ? { ...o, zIndex: 0 } : isWindowAllowed(o) ? { ...o, zIndex: o.zIndex + 1 } : o));
	state.isDirty = true;
};

const handleDeleteWindow = (windowId: string) => {
	const target = state.editingWindows.find((w) => w.id === windowId);
	if (target && !isWindowAllowed(target)) {
		ElMessage.warning(myPerm.deniedMessage());
		return;
	}
	state.editingWindows = state.editingWindows.filter((w) => w.id !== windowId);
	if (state.selectedWindowId === windowId) {
		state.selectedWindowId = '';
	}
	state.isDirty = true;
};

const handleDropSource = (sourceId: string, blockNo: number, x: number, y: number) => {
	// Mỗi nguồn chỉ hiển thị ở 1 cửa sổ trong kịch bản
	if (usedSourceIds.value.includes(sourceId)) {
		const src = state.sources.find((s) => s.id === sourceId);
		ElMessage.warning($t('lz.message.videoWall.sourceAlreadyInScene', { name: src?.name || src?.code || sourceId }));
		return;
	}
	// Cửa sổ mới mặc định 1 ô ⇒ ô thả vào phải thuộc vùng được cấp
	if (!myPerm.isRectAllowed(blockNo, x, y, topology.panelW, topology.panelH)) {
		ElMessage.warning(myPerm.deniedMessage());
		return;
	}
	// Lấy tên nguồn làm nhãn cửa sổ: vừa hiển thị có nghĩa, vừa đáp ứng ràng buộc
	// `Name` khác rỗng của backend khi lưu.
	const srcName = state.sources.find((s) => s.id === sourceId)?.name ?? undefined;
	const newWindow: EditWindow = {
		id: `tmp-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`,
		sceneId: isNewScene.value ? '' : state.activeSceneId,
		sourceId,
		name: srcName,
		blockNo,
		x,
		y,
		w: topology.panelW, // Mặc định rộng 1 ô
		h: topology.panelH, // Mặc định cao 1 ô
		zIndex: Math.max(0, ...state.editingWindows.map((w) => w.zIndex)) + 1,
		label: srcName,
		locked: false,
		visible: SceneWindowVisible.NUMBER_1,
		orderNo: state.editingWindows.length + 1,
	};
	state.editingWindows.push(newWindow);
	state.selectedWindowId = newWindow.id!;
	state.isDirty = true;
};

// ── Properties panel ──
const handleUpdateWindowProps = (updated: EditWindow) => {
	// Sửa số liệu ở bảng thuộc tính: cả vị trí cũ lẫn vị trí mới phải thuộc vùng được cấp
	const current = state.editingWindows.find((w) => w.id === updated.id);
	if ((current && !isWindowAllowed(current)) || !isWindowAllowed(updated)) {
		ElMessage.warning(myPerm.deniedMessage());
		return;
	}
	handleUpdateWindow(updated);
};

const handleDeleteSelectedWindow = () => {
	if (state.selectedWindowId) {
		handleDeleteWindow(state.selectedWindowId);
	}
};

const handleToggleLock = () => {
	if (selectedWindow.value) {
		const updated = { ...selectedWindow.value, locked: !selectedWindow.value.locked };
		handleUpdateWindow(updated);
	}
};
</script>

<style scoped>
.videowall-scene-container {
	display: flex;
	flex-direction: column;
	height: 100%;
	overflow: hidden;
}

/* Thanh tab dạng nút như trang Màn hình / Bố cục tường. Chỉ áp cho thanh tab CỦA TRANG (">"), không lan vào tab lồng bên trong */
.vw-scene-tabs {
	flex: 1;
	min-height: 0;
	display: flex;
	flex-direction: column;
}
.vw-scene-tabs > :deep(.el-tabs__header) {
	margin-bottom: 8px;
}
.vw-scene-tabs > :deep(.el-tabs__content) {
	flex: 1;
	min-height: 0;
}
.vw-scene-tabs > :deep(.el-tabs__content) > .el-tab-pane {
	height: 100%;
}
.vw-scene-tabs > :deep(.el-tabs__header) .el-tabs__nav-wrap::after,
.vw-scene-tabs > :deep(.el-tabs__header) .el-tabs__active-bar {
	display: none;
}
.vw-scene-tabs > :deep(.el-tabs__header) .el-tabs__nav {
	gap: 6px;
}
.vw-scene-tabs > :deep(.el-tabs__header) .el-tabs__item {
	height: 34px;
	padding: 0 16px !important;
	border-radius: 6px;
	color: var(--el-text-color-regular);
	font-weight: 500;
	transition: background 0.15s, color 0.15s;
}
.vw-scene-tabs > :deep(.el-tabs__header) .el-tabs__item:hover:not(.is-active) {
	color: var(--el-color-primary);
	background: var(--el-color-primary-light-9);
}
.vw-scene-tabs > :deep(.el-tabs__header) .el-tabs__item.is-active {
	color: #fff;
	background: var(--el-color-primary);
	box-shadow: 0 2px 6px rgba(var(--el-color-primary-rgb), 0.35);
}

.vw-tab-label {
	display: inline-flex;
	align-items: center;
	gap: 6px;
}
.vw-tab-label .el-icon {
	font-size: 12px;
}

.scene-editor-page {
	display: flex;
	flex-direction: column;
	height: 100%;
	overflow: hidden;
}

.scene-editor-header {
	display: flex;
	justify-content: space-between;
	align-items: center;
	gap: 16px;
	padding: 10px 16px;
	border-bottom: 1px solid var(--el-border-color-light);
	background-color: var(--el-bg-color);
	flex-shrink: 0;
}

.scene-editor-header-right {
	display: flex;
	align-items: center;
	gap: 12px;
	flex-shrink: 0;
}

.scene-editor-header-right .el-button + .el-button {
	margin-left: 0;
}

/* Chấm trạng thái thay đổi — cùng kiểu .wl-change của wallLayout */
.scene-editor-change {
	display: inline-flex;
	align-items: center;
	gap: 6px;
	font-size: 13px;
	color: var(--el-text-color-secondary);
}

.scene-editor-change i {
	width: 8px;
	height: 8px;
	border-radius: 50%;
	background: var(--el-color-success);
}

.scene-editor-change.is-dirty {
	color: var(--el-color-warning);
}

.scene-editor-change.is-dirty i {
	background: var(--el-color-warning);
}

.scene-editor-title {
	font-size: 18px;
	font-weight: 600;
	color: var(--el-text-color-primary);
	white-space: nowrap;
}

.scene-editor-body {
	display: flex;
	flex: 1;
	overflow: hidden;
}

/* Cột cấu hình bên trái: rộng theo thanh chia (không theo nội dung) ⇒ chọn / bỏ chọn cửa sổ không làm canvas đổi kích thước */
.scene-editor-inspector {
	border-right: 1px solid var(--el-border-color-light);
	background-color: var(--el-bg-color);
	overflow-y: auto;
	flex-shrink: 0;
}

.scene-editor-inspector-divider {
	margin: 4px 0;
}

.scene-editor-inspector-hint {
	display: flex;
	flex-direction: column;
	align-items: center;
	gap: 8px;
	padding: 32px 24px;
	text-align: center;
	font-size: 14px;
	color: var(--el-text-color-secondary);
}

.scene-editor-center {
	flex: 1;
	display: flex;
	flex-direction: column;
	overflow: hidden;
	min-width: 0;
	/* background-color: var(--el-bg-color-page); */
}

/* Sân khấu tối của sceneCanvas phủ kín vùng này */
.scene-editor-canvas-area {
	flex: 1;
	overflow: hidden;
	position: relative;
}

/* Cảnh báo kịch bản thuộc bố cục cũ: nổi trên đầu canvas */
.scene-editor-old-layout {
	position: absolute;
	top: 8px;
	left: 50%;
	transform: translateX(-50%);
	z-index: 2;
	width: auto;
	max-width: calc(100% - 24px);
}

.scene-editor-empty {
	display: flex;
	align-items: center;
	justify-content: center;
	height: 100%;
}

.scene-editor-palette {
	border-left: 1px solid var(--el-border-color-light);
	overflow-y: auto;
	flex-shrink: 0;
	background-color: var(--el-bg-color);
}

/* Thanh chia dọc giữa cột bên và canvas */
.scene-editor-splitter {
	flex: none;
	width: 24px;
	align-self: stretch;
}
</style>
