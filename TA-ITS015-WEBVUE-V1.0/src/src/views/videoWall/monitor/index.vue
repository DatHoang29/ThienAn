<template>
	<div class="monitor-page" ref="pageRef">
		<!-- Toolbar -->
		<MonitorToolbar v-model="state.activeSceneId" :scenes="state.scenes" :serving-scene-id="state.servingSceneId" :has-changes="isScenePending" :can-apply="!!state.activeSceneId"
			:applying="state.isApplying" :is-fullscreen="isFullscreen" @change-scene="onChangeScene" @apply="onApply" @reset="onReset"
			@screen-cmd="onScreenCmd" @fullscreen="toggleFullscreen" />

		<!-- Main area: chỉ còn canvas.
		     Sidebar nguồn tín hiệu đã bỏ: nó chỉ tồn tại để kéo-thả tạo cửa sổ mới, mà BE
		     chưa có endpoint đẩy layout xuống tường ⇒ Monitor là trang CHỈ XEM + chọn kịch bản. -->
		<div class="monitor-body">
			<div class="monitor-canvas-wrapper" v-loading="state.wallLoading">
				<SceneCanvas :windows="state.windows"
					selected-window-id="" :sources="state.sources" :screens="state.screens" :layout="state.sceneLayout" readonly zoomable />
				<!-- Bố cục đang dùng chưa có kịch bản nào đang bật ⇒ hướng dẫn tạo kịch bản -->
				<div v-if="state.loaded && !state.scenes.length" class="monitor-empty">
					<div class="monitor-empty-card">
						<el-icon class="monitor-empty-icon"><ele-Film /></el-icon>
						<div class="monitor-empty-title">{{ $t('lz.label.videoWall.noSceneTitle') }}</div>
						<div class="monitor-empty-desc">{{ $t('lz.message.videoWall.noSceneHint') }}</div>
						<el-button v-if="scenePagePath" type="primary" icon="ele-Plus" @click="openMenuPage(router, scenePagePath)">{{ $t('lz.button.videoWall.createScene') }}</el-button>
					</div>
				</div>
			</div>
		</div>

		<!-- Status bar: chỉ dùng dữ liệu đã nạp sẵn (bố cục, cửa sổ, kịch bản) -->
		<div class="monitor-statusbar">
			<span v-if="topology.blocks.length" class="monitor-stat">
				<el-icon><ele-Grid /></el-icon>{{ $t('lz.label.videoWall.layoutSummary', { blocks: topology.blocks.length, screens: topology.totalScreens }) }}
			</span>
			<span v-if="state.activeScene" class="monitor-stat">
				<el-icon><ele-CopyDocument /></el-icon>{{ $t('lz.label.videoWall.windowCount', { count: state.windows.length }) }}
			</span>
			<span v-if="activeSceneUpdatedAt" class="monitor-stat">
				<el-icon><ele-Clock /></el-icon>{{ $t('lz.label.videoWall.sceneUpdatedAt', { time: activeSceneUpdatedAt }) }}
			</span>
			<span v-if="isScenePending && servingSceneName" class="monitor-stat is-warn" :title="$t('lz.label.videoWall.previewNotApplied')">
				<el-icon><ele-VideoPlay /></el-icon>{{ $t('lz.label.videoWall.nowServing', { name: servingSceneName }) }}
			</span>
		</div>
	</div>
</template>


<script lang="ts" setup name="videoWallMonitor">
import { computed, onMounted, onActivated, onUnmounted, reactive, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { getAPI } from '/@/utils/axios-utils';
import { VwSceneApi, VwWindowSceneApi, VwSourceApi, VwScreenApi, VwWallTopologyApi, StatusEnum } from '/@/api-services/videoWall';
import type { VwSceneOutput, VwSourceOutput, VwScreenOutput, VwWallTopologyOutput } from '/@/api-services/videoWall';
import { toEditWindow, type EditWindow } from '/@/utils/videoWall/sceneWindow';
import { fetchActiveScene, pickStartScene } from '/@/utils/videoWall/sceneStart';
import SceneCanvas from '/@/views/videoWall/scenes/component/sceneCanvas.vue';
import MonitorToolbar from './component/monitorToolbar.vue';
import mittBus from '/@/utils/mitt';
import { useTransporterVideoWallScene, normalizeVideoWallScenePayload } from '/@/transporter/hooks/useTransporterVideoWallScene';
import { useI18n } from 'vue-i18n';
import { useVideoWallMyPermission } from '/@/stores/its/videoWallMyPermission';
import { useVideoWallTopology } from '/@/stores/its/videoWallTopology';
import { useRequestOldRoutes } from '/@/stores/requestOldRoutes';
import { findMenuPage, openMenuPage } from '/@/utils/menuTabs';
import { formatDate } from '/@/utils/formatTime';
import { useRouter } from 'vue-router';

/** Quyền vùng màn hình: canvas chỉ xem sẽ gạch chéo các ô ngoài vùng được cấp. */
const myPerm = useVideoWallMyPermission();
/** Bố cục đang dùng — chỉ kịch bản thuộc bố cục này mới chọn / áp dụng được. */
const topology = useVideoWallTopology();


/** Số bản ghi tối đa nạp một lần qua `/page`. */
const PAGE_ALL = 500;
const byOrderNo = (a: VwSceneOutput, b: VwSceneOutput) => (a.orderNo ?? 0) - (b.orderNo ?? 0);

const pageRef = ref<HTMLElement>();
const { t: $t } = useI18n();

const state = reactive({
	scenes: [] as VwSceneOutput[],
	activeSceneId: '',
	activeScene: null as VwSceneOutput | null,
	servingSceneId: '',
	windows: [] as EditWindow[],
	sources: [] as VwSourceOutput[],
	screens: [] as VwScreenOutput[],
	isApplying: false,
	/** Bố cục của kịch bản đang xem khi nó thuộc phiên bản bố cục cũ (null = bố cục đang dùng) */
	sceneLayout: null as VwWallTopologyOutput | null,
	/** Đã nạp xong lần đầu ⇒ mới hiện thông báo "Chưa có kịch bản" (tránh nháy lúc đang tải). */
	loaded: false,
	/** Đang nạp bố cục tường + vị trí màn hình + kịch bản ⇒ che canvas bằng loading (tránh vẽ tường thiếu / sai vị trí). */
	wallLoading: true,
});

const router = useRouter();
/**
 * Trang Quản lý kịch bản trong menu BE (menu riêng hoặc tab của menu nhóm); không được cấp menu thì ẩn nút "Tạo kịch bản".
 * Là tab ⇒ openMenuPage nhờ tabPage chọn đúng tab (không qua URL).
 */
const SCENE_PAGE_COMPONENT = 'videoWall/scenes/index';
const scenePagePath = computed(() => findMenuPage(useRequestOldRoutes().requestOldRoutes as any[], SCENE_PAGE_COMPONENT));

const activeSceneUpdatedAt = computed(() => {
	const time = state.activeScene?.updateTime || state.activeScene?.createTime;
	return time ? formatDate(new Date(time), 'dd/mm/YYYY HH:MM') : '';
});

/** Đang xem trước một kịch bản khác kịch bản BE đang phát ⇒ cần bấm "Áp dụng" để kích hoạt. */
const isScenePending = computed(() => !!state.activeSceneId && state.activeSceneId !== state.servingSceneId);
const servingSceneName = computed(() => {
	const scene = state.scenes.find((s) => s.id === state.servingSceneId);
	return scene?.name || scene?.code || '';
});

/* ── API ── */

/** Kịch bản đang bật (Status = 1) của bố cục đang dùng — kịch bản bố cục cũ không áp dụng được nên không đưa vào danh sách chọn. */
async function fetchEnabledScenes(): Promise<VwSceneOutput[]> {
	await topology.load();
	const res = await getAPI(VwSceneApi).apiVideowallVwscenePagePost({
		page: 1,
		pageSize: PAGE_ALL,
		status: StatusEnum.NUMBER_1,
		wallTopologyId: topology.topology?.id || undefined,
		field: 'orderNo',
		order: 'asc',
	});
	return (res.data.result?.records ?? []).sort(byOrderNo);
}

/**
 * Còn kịch bản thì canvas LUÔN hiển thị 1 kịch bản, người dùng không phải tự chọn — quy tắc chung với trang Kịch bản
 * (`pickStartScene`): kịch bản BE đang phát (nguồn sự thật, đúng trên mọi máy trạm) → mặc định → kịch bản đầu tiên.
 * Hết kịch bản ⇒ xoá canvas, hiện thẻ hướng dẫn tạo kịch bản.
 */
async function showStartScene(activeScene?: VwSceneOutput) {
	// Kịch bản BE đang phát có thể không nằm trong nhóm đang bật vừa nạp ⇒ bổ sung vào danh sách để chọn / `loadScene` tra được.
	if (activeScene?.id && !state.scenes.some((s) => s.id === activeScene.id)) state.scenes = [...state.scenes, activeScene].sort(byOrderNo);
	const startScene = pickStartScene(state.scenes, activeScene);
	if (!startScene?.id) {
		state.activeSceneId = '';
		state.activeScene = null;
		state.windows = [];
		state.sceneLayout = null;
		return;
	}
	/* Chỉ coi là "đang phát" khi BE thật sự đang phát kịch bản đó. Nếu BE chưa kích hoạt kịch bản nào,
	   kịch bản mặc định/đầu tiên chỉ là xem trước ⇒ nút "Áp dụng" bật lên để người dùng chủ động đẩy xuống tường. */
	await loadScene(startScene.id, startScene.id === activeScene?.id);
}

/* ── Init ── */
onMounted(async () => {
	myPerm.load(true);
	state.wallLoading = true;
	try {
		// Chờ đủ bố cục tường (khối) + vị trí màn hình rồi mới vẽ canvas; nạp lại bố cục để thấy thay đổi vừa lưu ở trang Màn hình
		const [scenes, sourceRes, screenRes] = await Promise.all([
			fetchEnabledScenes(),
			getAPI(VwSourceApi).apiVideowallVwsourcePagePost({ page: 1, pageSize: PAGE_ALL, field: 'orderNo', order: 'asc' }),
			getAPI(VwScreenApi).apiVideowallVwscreenPagePost({ page: 1, pageSize: PAGE_ALL, field: 'orderNo', order: 'asc' }),
			topology.load(true),
		]);
		state.scenes = scenes;
		state.sources = sourceRes.data.result?.records ?? [];
		state.screens = screenRes.data.result?.records ?? [];

		await showStartScene(await fetchActiveScene());
	} catch {
		ElMessage.error($t('lz.message.videoWall.loadMonitorFailed'));
	} finally {
		state.loaded = true;
		state.wallLoading = false;
	}
});

/* ── Đồng bộ kịch bản đang phát ──
   NGUỒN SỰ THẬT là BE (`GET /vwscene/active`), không còn dùng `localStorage` vì key đó
   chỉ đúng trong một browser. Ba đường cập nhật:
	 1. NATS (đường chính, chạy xuyên máy trạm) — xem mục 4 bên dưới.
	 2. Quay lại trang / tab được focus → hỏi lại BE, bù cho trường hợp NATS mất gói.
	 3. mittBus + custom event → chỉ là dự phòng khi API kích hoạt lỗi (trang Lập lịch /
		Quy tắc ITS chuyển cục bộ trong cùng tab). */

/** Hỏi BE kịch bản đang phát và chuyển nếu khác kịch bản đang mở. */
async function syncActiveSceneFromServer(notify = true) {
	const active = await fetchActiveScene();
	// BE không phát kịch bản nào (kịch bản đã xoá / vừa đổi bố cục) ⇒ bỏ nhãn "Đang phát" cũ
	if (!active?.id) {
		state.servingSceneId = '';
		return;
	}
	await adoptServingScene(active.id, active, notify ? $t('lz.message.videoWall.servingScene', { name: active.name || active.code }) : undefined);
}

/**
 * Ghi nhận kịch bản mà BE đang phát lên tường và nạp lên canvas nếu người dùng không có
 * việc dở.
 *
 * Vì việc chọn kịch bản ở dropdown giờ chỉ là XEM TRƯỚC, nếu người dùng đang xem trước một
 * kịch bản khác (hoặc đang kéo/thả cửa sổ), tuyệt đối không cướp canvas — chỉ cập nhật
 * `servingSceneId` để nút "Áp dụng" và thanh trạng thái phản ánh đúng tường thật.
 */
async function adoptServingScene(sceneId: string, scene?: VwSceneOutput, notifyLabel?: string) {
	if (!sceneId) return;
	// Kịch bản có thể không nằm trong nhóm 'active' đang giữ ⇒ bổ sung để `loadScene` tra được.
	if (scene && !state.scenes.some((s) => s.id === sceneId)) {
		state.scenes = [...state.scenes, scene].sort(byOrderNo);
	}
	// Phải chốt trước khi ghi `servingSceneId`, nếu không `isScenePending` sẽ luôn là true.
	const keepPreview = isScenePending.value;
	state.servingSceneId = sceneId;
	if (keepPreview || sceneId === state.activeSceneId) return;
	await loadScene(sceneId, true);
	if (notifyLabel) ElMessage.info(notifyLabel);
}

/**
 * Nạp lại bố cục tường + vị trí màn hình + quyền vùng: trang được giữ sống (keep-alive) nên
 * thay đổi vừa lưu ở trang Màn hình / Phân quyền không tự về ⇒ nạp lại mỗi lần quay lại trang.
 */
async function refreshWallData() {
	state.wallLoading = true;
	try {
		const [screenRes, sourceRes] = await Promise.all([
			getAPI(VwScreenApi).apiVideowallVwscreenPagePost({ page: 1, pageSize: PAGE_ALL, field: 'orderNo', order: 'asc' }),
			// Nguồn có thể vừa thêm ở trang Nguồn tín hiệu ⇒ nhãn cửa sổ mới đúng
			getAPI(VwSourceApi).apiVideowallVwsourcePagePost({ page: 1, pageSize: PAGE_ALL, field: 'orderNo', order: 'asc' }),
			topology.load(true),
			myPerm.load(true),
		]);
		state.screens = screenRes.data.result?.records ?? [];
		state.sources = sourceRes.data.result?.records ?? [];

		// Danh sách và kịch bản đang phát lấy đúng theo BE (không giữ kịch bản cũ trong bộ nhớ):
		// kịch bản vừa bị xoá / thuộc bố cục cũ (đổi bố cục đã xoá trạng thái phát) phải biến mất.
		const [scenes, active] = await Promise.all([fetchEnabledScenes(), fetchActiveScene()]);
		state.scenes = scenes;
		state.servingSceneId = active?.id ?? '';
		// Đang xem kịch bản còn tồn tại ⇒ nạp lại; chưa có / vừa bị xoá ⇒ đang phát → mặc định → đầu tiên, hết kịch bản thì canvas trống
		if (state.activeSceneId && scenes.some((s) => s.id === state.activeSceneId)) await loadScene(state.activeSceneId);
		else await showStartScene(active);
	} catch {
		/* Giữ dữ liệu đang có; lần quay lại sau sẽ thử lại */
	} finally {
		state.wallLoading = false;
	}
}

// 1. onActivated: khi route quay lại trang Monitor (keep-alive). Lần kích hoạt đầu đi cùng onMounted (đã nạp) ⇒ bỏ qua phần nạp bố cục.
let firstActivation = true;
onActivated(() => {
	if (firstActivation) firstActivation = false;
	else void refreshWallData();
	void syncActiveSceneFromServer();
});

// 2. mittBus: dự phòng trong cùng tab khi không gọi được API kích hoạt
function onSceneActivatedExternally(payload: any) {
	if (!payload?.sceneId) return;
	const scene = state.scenes.find((s) => s.id === payload.sceneId);
	void adoptServingScene(payload.sceneId, scene, $t('lz.message.videoWall.sceneSwitched', { name: payload.sceneName || scene?.name || payload.sceneId }));
}
mittBus.on('videoWallSceneActivated' as any, onSceneActivatedExternally);

// 3. visibilitychange: khi user quay lại tab này
function onVisibilityChange() {
	if (document.visibilityState === 'visible') void syncActiveSceneFromServer();
}
document.addEventListener('visibilitychange', onVisibilityChange);

// 4. Custom event: dự phòng khi trigger từ cùng tab (ITS/Schedule dispatch)
function onCustomSceneChanged(event: Event) {
	const detail = (event as CustomEvent).detail;
	if (!detail?.sceneId) return;
	const scene = state.scenes.find((s) => s.id === detail.sceneId);
	void adoptServingScene(detail.sceneId, scene, $t('lz.message.videoWall.sceneSwitched', { name: detail.sceneName || scene?.name || detail.sceneId }));
}
window.addEventListener('vw-scene-changed', onCustomSceneChanged);

// 5. NATS: BE kích hoạt kịch bản (POST /vwscene/active → publish ta.its.data.videowall.scene).
//    Đây là đường duy nhất chạy được XUYÊN máy trạm.
//    Payload có thể chỉ có `SceneCode` (kích hoạt trực tiếp) hoặc `SceneId` (theo sự kiện).
useTransporterVideoWallScene(async (data) => {
	// Hook chỉ chuyển payload thô; bóc SceneId/Code bằng normalize (BE bọc trong `ActiveValue`).
	const payload = normalizeVideoWallScenePayload(data);

	// Không có cả id lẫn code thì không tra được kịch bản ⇒ bỏ qua để tránh load sai.
	if (!payload.sceneId && !payload.code) {
		return;
	}

	const findLocal = () => state.scenes.find((s) => (payload.sceneId && s.id === payload.sceneId) || (payload.code && s.code === payload.code));

	let scene = findLocal();
	// Chưa có trong danh sách của tab này: kịch bản mới tạo ở máy khác, hoặc BE kích hoạt
	// kịch bản không nằm trong nhóm 'active' mà trang đang giữ.
	// Có id thì lấy trực tiếp (1 request, không phụ thuộc trạng thái); chỉ có code thì
	// buộc phải nạp lại danh sách rồi dò theo code.
	if (!scene) {
		try {
			if (payload.sceneId) {
				const res = await getAPI(VwSceneApi).apiVideowallVwsceneByidGet(payload.sceneId);
				scene = res.data.result ?? undefined;
				// `loadScene` tra trong state.scenes nên phải nạp vào danh sách trước.
				if (scene) state.scenes = [...state.scenes, scene].sort(byOrderNo);
			} else {
				state.scenes = await fetchEnabledScenes();
				scene = findLocal();
			}
		} catch {
			/* Nạp lại thất bại thì bỏ qua, lần kích hoạt sau sẽ thử lại. */
		}
	}
	if (!scene?.id) {
		return;
	}
	await adoptServingScene(scene.id, scene, $t('lz.message.videoWall.sceneSwitched', { name: scene.name || payload.name || payload.code }));
});

onUnmounted(() => {
	mittBus.off('videoWallSceneActivated' as any, onSceneActivatedExternally);
	document.removeEventListener('visibilitychange', onVisibilityChange);
	window.removeEventListener('vw-scene-changed', onCustomSceneChanged);
});

/* ── Load scene ──
   `markServing = true` khi lệnh chuyển đến TỪ BE (nạp lần đầu, NATS, đồng bộ lại) — lúc đó
   canvas và tường đang khớp nhau. Người dùng tự chọn ở dropdown thì để `false` để giữ
   `servingSceneId`, nhờ vậy nút "Áp dụng" bật lên và biết cần gọi API kích hoạt. */
async function loadScene(sceneId: string, markServing = false) {
	const scene = state.scenes.find((s) => s.id === sceneId);
	if (!scene) return;
	state.activeSceneId = sceneId;
	state.activeScene = scene;
	if (markServing) state.servingSceneId = sceneId;
	// BE có thể bỏ qua filter sceneId ⇒ lọc lại ở client.
	const res = await getAPI(VwWindowSceneApi).apiVideowallVwwindowscenePagePost({ page: 1, pageSize: PAGE_ALL, sceneId });
	state.windows = (res.data.result?.records ?? [])
		.filter((w) => w.sceneId === sceneId)
		.map(toEditWindow)
		.sort((a, b) => a.zIndex - b.zIndex || (a.orderNo ?? 0) - (b.orderNo ?? 0));
	// Kịch bản đang phát có thể thuộc phiên bản bố cục cũ ⇒ vẽ theo bố cục của nó để cửa sổ đúng vị trí
	state.sceneLayout = null;
	if (scene.wallTopologyId && scene.wallTopologyId !== topology.topology?.id) {
		const layoutRes = await getAPI(VwWallTopologyApi).apiVideowallVwwalltopologyByidGet(scene.wallTopologyId).catch(() => null);
		state.sceneLayout = layoutRes?.data.result ?? null;
	}
}

/* ── Scene change: CHỈ xem trước, không gửi API ── */
async function onChangeScene(sceneId: string) {
	await loadScene(sceneId);
}

/* ── Apply: đây là NƠI DUY NHẤT gửi lệnh xuống tường ── */
async function onApply() {
	if (state.isApplying || !state.activeSceneId) return;
	// Chốt trước khi await: `isScenePending` sẽ đổi ngay sau khi ghi `servingSceneId`, và
	// NATS phản hồi về giữa lúc chờ cũng có thể sửa state.
	const targetId = state.activeSceneId;

	// Phân quyền vùng màn hình: kích hoạt cả kịch bản chỉ khi MỌI cửa sổ nằm trong vùng được cấp (BE cũng chặn)
	if (!myPerm.isFull && (myPerm.isNone || state.windows.some((w) => !myPerm.isRectAllowed(w.blockNo, w.x, w.y, w.w, w.h)))) {
		ElMessage.warning(myPerm.isNone ? myPerm.deniedMessage() : $t('lz.message.videoWall.sceneOutsideCannotActivate'));
		return;
	}

	/* Hỏi xác nhận TRƯỚC khi bật cờ loading: kích hoạt kịch bản là lệnh đẩy xuống toàn bộ
	   tường và mọi máy trạm đang xem, không nên đổi chỉ vì một cú bấm nhầm.
	   Bấm Hủy ⇒ ElMessageBox reject ⇒ thoát sớm; vì chưa bật `isApplying` nên nút không
	   bị treo ở trạng thái quay. */
	try {
		await ElMessageBox.confirm(
			$t('lz.message.videoWallMonitor.confirmApply', { name: state.activeScene?.name || state.activeScene?.code || '' }),
			$t('lz.message.base.warning'),
			{
				confirmButtonText: $t('lz.button.base.confirm'),
				cancelButtonText: $t('lz.button.base.cancel'),
				type: 'warning',
			}
		);
	} catch {
		return;
	}

	state.isApplying = true;
	try {
		/* Luôn gọi API kích hoạt — kể cả kịch bản ĐANG PHÁT (10/11/2026): sửa kịch bản chỉ ghi DB,
		   bấm Áp dụng mới lên tường. Worker tự chọn: chưa sửa ⇒ gọi lại ô kịch bản trên thiết bị (tức thì);
		   đã sửa ⇒ dựng lại từ DB rồi lưu. Worker broadcast NATS `ta.its.data.videowall.scene` sau khi
		   thiết bị xong; handler NATS ở trên thấy `sceneId === servingSceneId` nên không nạp lại canvas. */
		// BE nhận `Code` (không nhận id). Không gửi kèm eventTypeId — BE sẽ bỏ qua quy tắc.
		const code = state.scenes.find((s) => s.id === targetId)?.code;
		if (!code) throw new Error($t('lz.message.videoWall.sceneNoCode'));
		await getAPI(VwSceneApi).apiVideowallVwsceneActivePost({ code });
		state.servingSceneId = targetId;
		ElMessage.success($t('lz.message.videoWall.appliedToWall'));
	} catch {
		ElMessage.error($t('lz.message.videoWall.applyFailed'));
	} finally {
		state.isApplying = false;
	}
}

/* ── Reset: bỏ kịch bản xem trước, quay về kịch bản BE đang phát ── */
async function onReset() {
	const targetId = state.servingSceneId || state.activeSceneId;
	if (!targetId) return;
	await loadScene(targetId, targetId === state.servingSceneId);
	ElMessage.info($t('lz.message.videoWall.revertedToServing'));
}

/* ── Screen on/off ──
   ⚠️ CHỈ ĐỔI HIỂN THỊ Ở CLIENT. BE chưa có endpoint gửi lệnh nguồn xuống bộ điều khiển
   (`vw-controller-api` chỉ có CRUD), nên KHÔNG set cờ chờ áp dụng — nếu set, nút "Áp dụng"
   sẽ bật lên nhưng không có gì để gửi. */
function onScreenCmd(cmd: string) {
	if (cmd === 'all-on') {
		state.screens.forEach((s) => (s.status = StatusEnum.NUMBER_1));
	} else if (cmd === 'all-off') {
		state.screens.forEach((s) => (s.status = StatusEnum.NUMBER_0));
	}
}

/* ── Fullscreen ── */
/** Theo dõi cả khi thoát bằng phím Esc (không đi qua toggleFullscreen). */
const isFullscreen = ref(false);
function onFullscreenChange() {
	isFullscreen.value = document.fullscreenElement === pageRef.value;
}
document.addEventListener('fullscreenchange', onFullscreenChange);
onUnmounted(() => document.removeEventListener('fullscreenchange', onFullscreenChange));

function toggleFullscreen() {
	if (!document.fullscreenElement) {
		pageRef.value?.requestFullscreen();
	} else {
		document.exitFullscreen();
	}
}
</script>


<style scoped lang="scss">
/* Toolbar (khung trắng) · sân khấu · thanh trạng thái tách nhau bằng khoảng cách, như các trang khác */
.monitor-page {
	display: flex;
	flex-direction: column;
	gap: 8px;
	height: 100%;
	background: var(--el-bg-color-page);
	color: var(--el-text-color-primary);
	overflow: hidden;
}

.monitor-body {
	flex: 1;
	display: flex;
	overflow: hidden;
	min-height: 0;
	border-radius: 10px;
	box-shadow: 0 2px 10px rgba(15, 23, 42, 0.08);
}

/* Toàn màn hình: tường phủ kín, không bo góc / khoảng cách */
.monitor-page:fullscreen {
	gap: 0;
}
.monitor-page:fullscreen .monitor-body {
	border-radius: 0;
	box-shadow: none;
}

/* Sân khấu tối của sceneCanvas phủ kín vùng này (không còn viền xám bao quanh) */
.monitor-canvas-wrapper {
	position: relative;
	flex: 1;
	min-width: 0;
	padding: 0;
	overflow: hidden;
	display: flex;
	align-items: stretch;
}

/* ── Chưa có kịch bản: thẻ hướng dẫn nổi giữa sân khấu, lưới tường vẫn thấy mờ phía sau ── */
.monitor-empty {
	position: absolute;
	inset: 0;
	z-index: 5;
	display: flex;
	align-items: center;
	justify-content: center;
	padding: 16px;
	background: rgba(6, 18, 40, 0.45);
}

.monitor-empty-card {
	display: flex;
	flex-direction: column;
	align-items: center;
	gap: 10px;
	max-width: 420px;
	padding: 24px 28px;
	border: 1px solid rgba(255, 255, 255, 0.12);
	border-radius: 8px;
	background: rgba(13, 35, 72, 0.92);
	box-shadow: 0 8px 24px rgba(0, 0, 0, 0.35);
	text-align: center;
	color: #fff;
}

.monitor-empty-icon {
	font-size: 36px;
	color: #7fb2ff;
}

.monitor-empty-title {
	font-size: 16px;
	font-weight: 600;
}

.monitor-empty-desc {
	font-size: 13px;
	line-height: 1.6;
	color: rgba(255, 255, 255, 0.72);
}

/* Minimal overrides — sceneCanvas handles its own styling */
.monitor-canvas-wrapper :deep(.scene-canvas-hint) {
	color: var(--el-text-color-secondary);
}

/* ── Status bar: dãy chip tóm tắt tường ── */
.monitor-statusbar {
	display: flex;
	align-items: center;
	flex-wrap: wrap;
	gap: 8px;
	padding: 6px 12px;
	background: var(--el-bg-color);
	border: 1px solid var(--el-border-color-lighter);
	border-radius: 10px;
	flex-shrink: 0;
}

.monitor-stat {
	display: inline-flex;
	align-items: center;
	gap: 6px;
	height: 28px;
	padding: 0 12px;
	border: 1px solid var(--el-border-color-lighter);
	border-radius: 14px;
	background: var(--el-fill-color-lighter);
	font-size: 12px;
	color: var(--el-text-color-regular);
	white-space: nowrap;
}

/* Icon tô màu chính để có điểm nhấn; chữ + nền giữ trung tính vì đây là thông tin, không phải trạng thái */
.monitor-stat .el-icon {
	font-size: 14px;
	color: var(--el-color-primary);
}

.monitor-stat.is-warn {
	color: var(--el-color-warning);
	border-color: var(--el-color-warning-light-7);
	background: var(--el-color-warning-light-9);
}

.monitor-stat.is-warn .el-icon {
	color: inherit;
}

/* ── Fullscreen: tường căn giữa theo chiều dọc ── */
.monitor-page:fullscreen :deep(.scene-canvas-cell-label) {
	font-size: 12px;
}

.monitor-page:fullscreen :deep(.scene-canvas-cell-pos) {
	font-size: 11px;
}

.monitor-page:fullscreen :deep(.scene-window-label) {
	font-size: 14px;
}
</style>
