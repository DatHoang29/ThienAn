<template>
	<div class="vw-dashboard">
		<!-- Hàng trên: Sơ đồ lưới chiếm cả chiều ngang. Hàng dưới: Bộ điều khiển | Thống kê nhanh.
			Mỗi thẻ: số liệu đứng yên, phần nội dung (.vw-dash-scroll) cuộn khi dài. -->
		<div class="vw-dash-grid">
			<!-- ═══ Màn hình: số liệu + sơ đồ lưới theo khối (giống bố cục tường) ═══ -->
			<el-card shadow="hover" class="vw-dash-section vw-dash-section--wide">
				<template #header>
					<div class="vw-dash-section-head">
						<el-icon class="vw-dash-section-icon" style="color: var(--el-color-primary)"><ele-Monitor /></el-icon>
						<span class="vw-dash-section-title">{{ $t('lz.label.videoWall.screenGrid') }}</span>
						<span class="vw-dash-section-sub">{{ topologyText }}</span>
						<!-- Chi tiết ⇒ trang Màn hình (tab Bố cục màn) -->
						<el-button class="vw-dash-section-action" link type="primary" @click="goScreens">
							{{ $t('lz.button.base.detail') }}<el-icon class="el-icon--right"><ele-ArrowRight /></el-icon>
						</el-button>
					</div>
				</template>
				<!-- Chú thích + số liệu gọn 1 dòng (bỏ hàng ô số liệu để sơ đồ tường to hơn):
					chấm góc trên phải = trạng thái kết nối (bật / tắt), ô nét đứt = ô còn trống; mỗi ô màn: nhiệt độ + giờ hoạt động -->
				<div class="vw-dash-legend">
					<span>{{ $t('lz.label.videoWall.totalScreens') }} <b>{{ totalScreens }}</b></span>
					<span><i class="vw-dash-screen-dot is-on" />{{ $t('lz.label.videoWall.screenOn') }} <b>{{ onlineScreens }}</b></span>
					<span><i class="vw-dash-screen-dot" />{{ $t('lz.label.videoWall.screenOff') }} <b>{{ offlineScreens }}</b></span>
					<span><i class="vw-dash-legend-empty" />{{ $t('lz.label.videoWall.emptyCells') }} <b>{{ emptyCells }}</b></span>
					<span class="vw-dash-legend-metric"><el-icon><ele-Odometer /></el-icon>{{ $t('lz.entity.videoWall.temperature') }}</span>
					<span class="vw-dash-legend-metric"><el-icon><ele-Timer /></el-icon>{{ $t('lz.entity.videoWall.runTimeHours') }}</span>
				</div>
				<!-- Sân khấu tường dùng chung (WallStage) như các trang Video Wall khác: lăn chuột để phóng to, kéo nền để di chuyển -->
				<div class="vw-dash-stage">
					<WallStage :blocks="wallBlocks" :cell-ratio="topology.cellRatio" :gap="4" rulers :guide="$t('lz.label.videoWall.cellLabelHint')">
						<template #block-meta="{ cols, rows }">
							<span class="ws-block-meta">{{ cols }}×{{ rows }}</span>
						</template>
						<template #cell="{ blockNo, row, col }">
							<!-- screen = màn đặt ở ô này (không có ⇒ ô trống) -->
							<template v-for="screen in [screenByCell.get(`${blockNo},${row},${col}`)]" :key="`${blockNo}-${row}-${col}`">
								<el-tooltip v-if="screen" placement="top" :show-after="150" popper-class="vw-info-tip">
									<template #content>
										<VwInfoTip :title="screen.name" :dot="isOnline(screen) ? 'on' : 'off'" :rows="screenTipRows(screen)" />
									</template>
									<div class="vw-dash-screen-cell" :class="{ 'vw-dash-screen-cell--on': isOnline(screen) }" :style="{ borderTopColor: vwCtrl.colorOf(screen.controllerId) }">
										<i class="vw-dash-screen-dot" />
										<span class="vw-dash-screen-code">{{ screen.code }}</span>
										<!-- Thông số từ thiết bị (đồng bộ màn): nhiệt độ + giờ hoạt động; vị trí xem ở thước H/C và tooltip -->
										<span class="vw-dash-screen-metric">
											<el-icon><ele-Odometer /></el-icon>{{ tempText(screen) }}
										</span>
										<span class="vw-dash-screen-metric">
											<el-icon><ele-Timer /></el-icon>{{ hoursText(screen) }}
										</span>
									</div>
								</el-tooltip>
								<div v-else class="vw-dash-screen-cell vw-dash-screen-cell--empty" :title="$t('lz.label.videoWall.emptyCellHint')">
									<span class="vw-dash-screen-val">H{{ row + 1 }}·C{{ col + 1 }}</span>
								</div>
							</template>
						</template>
					</WallStage>
				</div>
				<!-- Màn chưa nằm trên tường (khay ngoài lưới / toạ độ ngoài bố cục) không tính vào dashboard ⇒ chỉ nhắc số lượng -->
				<div v-if="unplacedScreens > 0" class="vw-dash-unplaced">
					<el-icon><ele-WarningFilled /></el-icon>
					{{ $t('lz.label.videoWall.unplacedScreensCount', { n: unplacedScreens }) }}
				</div>
			</el-card>

			<!-- ═══ Bộ điều khiển: số liệu + danh sách ═══ -->
			<el-card shadow="hover" class="vw-dash-section">
				<template #header>
					<div class="vw-dash-section-head">
						<el-icon class="vw-dash-section-icon" style="color: #7c3aed"><ele-Cpu /></el-icon>
						<span class="vw-dash-section-title">{{ $t('lz.label.videoWall.controllerStatus') }}</span>
						<!-- Số liệu gọn trên tiêu đề (bỏ hàng ô số liệu): tổng · hoạt động · ngừng -->
						<span class="vw-dash-section-sub">
							{{ $t('lz.label.videoWall.totalControllers') }} <b>{{ totalControllers }}</b> ·
							<span class="vw-dash-sub-ok">{{ $t('lz.label.base.active') }} <b>{{ activeControllers }}</b></span> ·
							<span :class="{ 'vw-dash-sub-bad': totalControllers - activeControllers > 0 }">{{ $t('lz.label.videoWall.stopped') }} <b>{{ totalControllers - activeControllers }}</b></span>
						</span>
					</div>
				</template>
				<div class="vw-dash-scroll">
					<div v-for="(ctrl, idx) in controllers" :key="ctrl.id!" class="vw-dash-ctrl-row" :class="{ 'vw-dash-ctrl-row--striped': idx % 2 === 1 }">
						<el-tag v-bind="vwColorTag(ctrl.colorHex)" size="small" effect="dark" class="vw-dash-ctrl-color">
							{{ vwCtrlCode(ctrl.code) }}
						</el-tag>
						<span class="vw-dash-ctrl-name" :title="ctrl.name ?? ''">{{ ctrl.name }}</span>
						<span class="vw-dash-ctrl-ip">{{ ctrl.ip ? (ctrl.port ? `${ctrl.ip}:${ctrl.port}` : ctrl.ip) : '—' }}</span>
						<span class="vw-dash-ctrl-model" :title="ctrl.model ?? ''">{{ ctrl.model }}</span>
						<el-tag size="small" class="vw-dash-ctrl-status" :type="ctrl.status === StatusEnum.NUMBER_1 ? 'success' : 'danger'">
							{{ ctrl.status === StatusEnum.NUMBER_1 ? $t('lz.label.base.active') : $t('lz.label.videoWall.stopped') }}
						</el-tag>
						<span class="vw-dash-ctrl-ports">{{ VideoWallEnums.OUTPUT_PORTS_PER_CONTROLLER }} {{ $t('lz.label.videoWall.ports') }}</span>
					</div>
				</div>
			</el-card>

			<!-- ═══ Quick stats ═══ -->
			<el-card shadow="hover" class="vw-dash-section">
				<template #header>
					<div class="vw-dash-section-head">
						<el-icon class="vw-dash-section-icon" style="color: var(--el-color-warning)"><ele-DataAnalysis /></el-icon>
						<span class="vw-dash-section-title">{{ $t('lz.label.videoWall.quickStats') }}</span>
					</div>
				</template>
				<div class="vw-dash-scroll vw-dash-stats">
					<div class="vw-dash-stat-row">
						<span class="vw-dash-stat-label">{{ $t('lz.label.videoWall.avgRunTime') }}</span>
						<span class="vw-dash-stat-value">{{ avgRunTime != null ? `${avgRunTime.toFixed(0)}h` : '—' }}</span>
					</div>
					<div class="vw-dash-stat-row">
						<span class="vw-dash-stat-label">{{ $t('lz.label.videoWall.maxRunTime') }}</span>
						<span class="vw-dash-stat-value">{{ maxRunTime != null ? `${maxRunTime}h` : '—' }}</span>
					</div>
					<div class="vw-dash-stat-row">
						<span class="vw-dash-stat-label">{{ $t('lz.label.videoWall.commonResolution') }}</span>
						<span class="vw-dash-stat-value">{{ commonResolution }}</span>
					</div>
					<div class="vw-dash-stat-row">
						<span class="vw-dash-stat-label">{{ $t('lz.label.videoWall.runningScene') }}</span>
						<span class="vw-dash-stat-value">{{ activeSceneName }}</span>
					</div>
					<div class="vw-dash-stat-row">
						<span class="vw-dash-stat-label">{{ $t('lz.label.videoWall.wallLayoutSummary') }}</span>
						<span class="vw-dash-stat-value">{{ topologyText }}</span>
					</div>
				</div>
			</el-card>
		</div>
	</div>
</template>


<script lang="ts" setup name="videoWallDashboard">
import { useI18n } from 'vue-i18n';
import { computed, onActivated, onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';
import { useMenuTabs } from '/@/stores/menuTabs';
import { ElMessage } from 'element-plus';
import { getAPI } from '/@/utils/axios-utils';
import { storeToRefs } from 'pinia';
import { VwScreenApi, VwSceneApi, StatusEnum, DefaultEnum } from '/@/api-services/videoWall';
import type { VwScreenOutput, VwSceneOutput } from '/@/api-services/videoWall';
import { VideoWallEnums } from '/@/types/enums/baseEnum';
import { useVideoWallController, vwColorTag, vwCtrlCode } from '/@/stores/its/videoWallController';
import { blockLabel, cellLabel, useVideoWallTopology } from '/@/stores/its/videoWallTopology';
import VwInfoTip, { type VwTipRow } from '/@/components/videoWall/vwInfoTip.vue';
import WallStage from '/@/components/videoWall/wallStage.vue';
import { isScreenOnline, screenStateTipRow } from '/@/utils/videoWall/screenState';
const { t: $t } = useI18n();

const PAGE_ALL = 500;

const vwCtrl = useVideoWallController();
const { controllers } = storeToRefs(vwCtrl);
const screens = ref<VwScreenOutput[]>([]);
const scenes = ref<VwSceneOutput[]>([]);
/** Bố cục tường (khối) do trang Màn hình cấu hình. */
const topology = useVideoWallTopology();
/** Tóm tắt bố cục: "8×4" (1 khối) hoặc "A 4×4 · B 4×2" (nhiều khối). */
const topologyText = computed(() =>
	topology.blocks.map((b) => `${topology.isMultiBlock ? `${blockLabel(b.blockNo)} ` : ''}${b.cols}×${b.rows}`).join(' · ')
);
/** Vị trí màn hiển thị: "H1 · C2", tường nhiều khối thì kèm khối "A · H1 · C2". */
const positionText = (s: VwScreenOutput) => cellLabel(s.blockNo, s.gridRow, s.gridCol, topology.isMultiBlock);
/** Nút Chi tiết ⇒ menu Màn hình (/videoWallConfig/screens), mở tab Bố cục màn (videoWallScreenLayoutTab). */
const router = useRouter();
const menuTabs = useMenuTabs();
const SCREENS_PATH = '/videoWallConfig/screens';
const goScreens = () => {
	const menuId = router.getRoutes().find((r) => r.path === SCREENS_PATH)?.meta?.menuId;
	if (menuId) menuTabs.requestTab(String(menuId), 'videoWallScreenLayoutTab');
	router.push(SCREENS_PATH);
};

const loadData = async () => {
	try {
		// Dashboard là trang theo dõi trạng thái ⇒ luôn nạp mới danh sách bộ điều khiển.
		const [, scrRes, scnRes] = await Promise.all([
			vwCtrl.load(true),
			getAPI(VwScreenApi).apiVideowallVwscreenPagePost({ page: 1, pageSize: PAGE_ALL }),
			getAPI(VwSceneApi).apiVideowallVwscenePagePost({ page: 1, pageSize: PAGE_ALL, status: StatusEnum.NUMBER_1, field: 'orderNo', order: 'asc' }),
			topology.load(true),
		]);
		// Sắp theo vị trí trên tường: khối trước, rồi hàng, rồi cột.
		screens.value = (scrRes.data.result?.records ?? []).sort(
			(a, b) => (a.blockNo ?? 0) - (b.blockNo ?? 0) || (a.gridRow ?? 0) - (b.gridRow ?? 0) || (a.gridCol ?? 0) - (b.gridCol ?? 0)
		);
		scenes.value = scnRes.data.result?.records ?? [];
	} catch {
		ElMessage.error($t('lz.message.videoWall.loadDashboardFailed'));
	}
};

onMounted(loadData);

/**
 * Trang được keep-alive ⇒ quay lại tab không chạy lại onMounted. Nạp lại mỗi lần kích hoạt
 * (bỏ lần đầu vì onMounted đã nạp) để thấy bố cục / màn hình vừa đổi ở trang Màn hình.
 */
let firstActivation = true;
onActivated(() => {
	if (firstActivation) {
		firstActivation = false;
		return;
	}
	void loadData();
});

/** Trạng thái kết nối (ScreenState) — hàm dùng chung với trang Bố cục màn hình. */
const isOnline = isScreenOnline;

/** Bộ điều khiển còn tồn tại ⇒ "Ctrl2 · OUT 3"; đã bị xoá / chưa gán ⇒ "Chưa gán" (không lộ ID thô). */
const portLabel = (s: VwScreenOutput) => (vwCtrl.find(s.controllerId) ? [vwCtrl.label(s.controllerId), s.outputName].filter(Boolean).join(' · ') : $t('lz.label.videoWall.unassigned'));

/** Dòng thông tin trong tooltip ô màn ở Sơ đồ lưới. */
const screenTipRows = (s: VwScreenOutput): VwTipRow[] => [
	{ icon: 'ele-CollectionTag', label: $t('lz.entity.base.code'), value: s.code },
	{ icon: 'ele-Location', label: $t('lz.entity.videoWall.position'), value: positionText(s) },
	{
		icon: 'ele-Cpu',
		label: $t('lz.entity.videoWall.controller'),
		value: portLabel(s),
		swatch: vwCtrl.find(s.controllerId) ? vwCtrl.colorOf(s.controllerId) : undefined,
		tone: vwCtrl.find(s.controllerId) ? undefined : 'warning',
	},
	{ icon: 'ele-Monitor', label: $t('lz.entity.videoWall.resolution'), value: s.resolution, show: !!s.resolution },
	{ icon: 'ele-Odometer', label: $t('lz.entity.videoWall.temperature'), value: tempText(s) },
	{ icon: 'ele-Timer', label: $t('lz.entity.videoWall.runTimeHours'), value: hoursText(s) },
	screenStateTipRow(s, $t),
];

/** Nhiệt độ / giờ hoạt động do đồng bộ màn ghi (chuỗi số); chưa có ⇒ "—". */
const tempText = (s: VwScreenOutput) => (s.temperature ? `${s.temperature}°C` : '—');
const hoursText = (s: VwScreenOutput) => {
	const h = Number(s.runTimeHours);
	return s.runTimeHours != null && s.runTimeHours !== '' && !Number.isNaN(h) ? `${Math.round(h)}h` : '—';
};

/* ── Summary counts ──
   BE lưu trạng thái màn hình bằng một cờ bật/tắt (`status: StatusEnum`), nên chỉ
   đếm được 2 nhóm. Không có nhóm "cảnh báo" cho tới khi BE định nghĩa `ScreenState`. */
/**
 * Màn đang nằm trên tường: có toạ độ và toạ độ nằm trong bố cục hiện tại (đã sắp khối → hàng → cột khi nạp).
 * Dashboard là trang tổng quan TƯỜNG ⇒ số liệu, bảng chi tiết và thống kê chỉ tính tập này;
 * màn ở khay ngoài lưới / toạ độ ngoài bố cục chỉ được nhắc số lượng dưới sơ đồ.
 */
const wallScreens = computed(() =>
	screens.value.filter((s) => s.gridRow != null && s.gridCol != null && topology.isCellInside(s.blockNo ?? 0, s.gridRow, s.gridCol))
);
const totalScreens = computed(() => wallScreens.value.length);
const onlineScreens = computed(() => wallScreens.value.filter(isOnline).length);
const offlineScreens = computed(() => wallScreens.value.filter((s) => !isOnline(s)).length);
const totalControllers = computed(() => controllers.value.length);
const activeControllers = computed(() => controllers.value.filter((c) => c.status === StatusEnum.NUMBER_1).length);

/* ── Sơ đồ lưới theo khối ──
   Mỗi khối là 1 lưới cột × hàng của bố cục tường; ô có màn thì hiện màn, ô trống hiện vị trí. */
/** Màn theo ô, khoá "khối,hàng,cột". */
const screenByCell = computed(() => {
	const map = new Map<string, VwScreenOutput>();
	for (const s of wallScreens.value) map.set(`${s.blockNo ?? 0},${s.gridRow},${s.gridCol}`, s);
	return map;
});
const wallBlocks = computed(() => {
	const byCell = screenByCell.value;
	return topology.blocks.map((b) => {
		const blockNo = b.blockNo ?? 0;
		const cols = b.cols ?? 0;
		const rows = b.rows ?? 0;
		const cells = [];
		for (let r = 0; r < rows; r++)
			for (let c = 0; c < cols; c++) cells.push({ key: `${blockNo}-${r}-${c}`, row: r, col: c, screen: byCell.get(`${blockNo},${r},${c}`) });
		return { blockNo, cols, rows, cells };
	});
});
const emptyCells = computed(() => wallBlocks.value.reduce((t, b) => t + b.cells.filter((c) => !c.screen).length, 0));
/** Màn không nằm trên tường: ở khay "ngoài lưới" của trang Màn hình, hoặc toạ độ thuộc phần bố cục đã bị cắt. */
const unplacedScreens = computed(() => screens.value.length - wallScreens.value.length);

/* ── Thống kê telemetry ──
   Chỉ dùng 2 chỉ số thiết bị thực sự đọc được: thời gian hoạt động và độ phân giải.
   Nhiệt độ / độ sáng đã bỏ khỏi dashboard vì BE có cột nhưng không có nguồn cập nhật
   từ thiết bị (xem docs/videowall-so-sanh-BE-FE.md, G11).
   Trả `null` khi không có số liệu để giao diện hiển thị '—' thay vì số 0 gây hiểu sai. */
// `RunTimeHours` BE lưu dạng chuỗi ⇒ chỉ lấy giá trị đọc được thành số.
const runTimesWithValue = computed(() =>
	wallScreens.value
		.filter((s) => s.runTimeHours != null && String(s.runTimeHours).trim() !== '')
		.map((s) => Number(s.runTimeHours))
		.filter((n) => Number.isFinite(n))
);
const avgRunTime = computed<number | null>(() => {
	if (runTimesWithValue.value.length === 0) return null;
	return runTimesWithValue.value.reduce((a, b) => a + b, 0) / runTimesWithValue.value.length;
});
const maxRunTime = computed<number | null>(() => (runTimesWithValue.value.length > 0 ? Math.max(...runTimesWithValue.value) : null));

/* Độ phân giải xuất hiện nhiều nhất — cho biết cấu hình chuẩn của tường màn hình. */
const commonResolution = computed<string>(() => {
	const counter = new Map<string, number>();
	for (const s of wallScreens.value) {
		const key = (s.resolution || '').trim();
		if (!key) continue;
		counter.set(key, (counter.get(key) ?? 0) + 1);
	}
	if (counter.size === 0) return '—';
	const [best] = [...counter.entries()].sort((a, b) => b[1] - a[1]);
	// Kèm số màn dùng cấu hình đó khi tường có nhiều loại phân giải khác nhau.
	return counter.size > 1 ? `${best[0]} (${best[1]}/${wallScreens.value.length})` : best[0];
});

/* ── Active scene ── */
const activeSceneName = computed(() => {
	const def = scenes.value.find((s) => s.isDefault === DefaultEnum.NUMBER_1);
	return def?.name || scenes.value[0]?.name || '—';
});
</script>


<style scoped lang="scss">
@import '/@/theme/its/videoWallStage.scss';

/* ── Lưới 2 × 2 thẻ bằng nhau, chia theo tỉ lệ khung (fr) — không chốt chiều cao bằng pixel ── */
.vw-dashboard {
	display: flex;
	flex-direction: column;
	height: 100%;
	padding: 12px;
	box-sizing: border-box;
}
.vw-dash-grid {
	flex: 1;
	min-height: 0;
	display: grid;
	grid-template-columns: repeat(2, minmax(0, 1fr));
	/* Hàng trên (sơ đồ lưới) cao hơn hàng dưới (bộ điều khiển | thống kê) */
	grid-template-rows: minmax(0, 3fr) minmax(0, 2fr);
	gap: 12px;
}
/* Sơ đồ lưới chiếm cả hàng trên */
.vw-dash-section--wide {
	grid-column: 1 / -1;
}
/* Màn hẹp: 1 cột, mỗi thẻ cao bằng nửa khung nhìn, cả trang cuộn */
@media (max-width: 1199px) {
	.vw-dash-grid {
		flex: none;
		grid-template-columns: minmax(0, 1fr);
		grid-template-rows: none;
		grid-auto-rows: 50vh;
	}
}

/* Thẻ: tiêu đề + số liệu đứng yên, phần nội dung giãn hết phần còn lại */
.vw-dash-section {
	display: flex;
	flex-direction: column;
	min-height: 0;
	:deep(.el-card__body) {
		flex: 1;
		min-height: 0;
		display: flex;
		flex-direction: column;
	}
}
/* Nội dung dài ⇒ cuộn trong thẻ */
.vw-dash-scroll {
	flex: 1;
	min-height: 0;
	overflow: auto;
	scrollbar-width: thin;
	scrollbar-color: rgba(148, 163, 184, 0.6) transparent;
	&::-webkit-scrollbar {
		width: 6px;
		height: 6px;
	}
	&::-webkit-scrollbar-thumb {
		border-radius: 999px;
		background: rgba(148, 163, 184, 0.55);
	}
	&::-webkit-scrollbar-thumb:hover {
		background: rgba(100, 116, 139, 0.7);
	}
}
.vw-dash-section-head {
	display: flex;
	align-items: center;
	gap: 8px;
}
.vw-dash-section-icon {
	font-size: 18px;
}
.vw-dash-section-title {
	font-size: 15px;
	font-weight: 600;
	color: var(--el-text-color-primary);
}
.vw-dash-section-sub {
	margin-left: auto;
	font-size: 12px;
	color: var(--el-text-color-secondary);
}

/* Số liệu gọn trên tiêu đề thẻ bộ điều khiển */
.vw-dash-section-sub b {
	color: var(--el-text-color-primary);
	font-variant-numeric: tabular-nums;
}
.vw-dash-sub-ok b {
	color: var(--el-color-success);
}
.vw-dash-sub-bad b {
	color: var(--el-color-danger);
}
.vw-dash-legend {
	display: flex;
	flex-wrap: wrap;
	align-items: center;
	gap: 4px 16px;
	margin: 0 0 8px;
	font-size: 12px;
	color: var(--el-text-color-secondary);
	span {
		display: inline-flex;
		align-items: center;
		gap: 6px;
	}
	b {
		color: var(--el-text-color-primary);
		font-variant-numeric: tabular-nums;
	}
	/* Chú thích 2 chỉ số hiện trong ô màn — đẩy sang phải */
	.vw-dash-legend-metric {
		gap: 4px;
		/* chú thích nhiệt độ (áp chót) đẩy cả cặp sang phải */
		&:nth-last-child(2) {
			margin-left: auto;
		}
	}
	.vw-dash-screen-dot {
		position: static;
	}
	.vw-dash-legend-empty {
		width: 14px;
		height: 10px;
		border: 1px dashed var(--el-border-color-darker);
		border-radius: 2px;
	}
}

/* ── Danh sách bộ điều khiển (sọc) ──
   Mọi hàng dùng chung một khung cột cố định ⇒ các cột thẳng hàng, không phụ thuộc độ dài nội dung.
   Thứ tự: mã | tên | IP | model | trạng thái | số cổng. */
.vw-dash-ctrl-row {
	display: grid;
	grid-template-columns: 56px minmax(0, 1.6fr) minmax(0, 1fr) minmax(0, 1.1fr) 96px 64px;
	align-items: center;
	gap: 12px;
	padding: 10px 12px;
	border-radius: 4px;
}
.vw-dash-ctrl-row--striped {
	background: var(--el-fill-color-light);
}
.vw-dash-ctrl-color {
	max-width: 100%;
	overflow: hidden;
}
.vw-dash-ctrl-name,
.vw-dash-ctrl-ip,
.vw-dash-ctrl-model {
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}
.vw-dash-ctrl-name {
	font-weight: 600;
	font-size: 13px;
	color: var(--el-text-color-primary);
}
.vw-dash-ctrl-ip {
	font-size: 12px;
	color: var(--el-text-color-regular);
}
.vw-dash-ctrl-model {
	font-size: 12px;
	color: var(--el-text-color-secondary);
}
.vw-dash-ctrl-status {
	justify-self: start;
}
.vw-dash-ctrl-ports {
	font-size: 12px;
	color: var(--el-text-color-secondary);
	text-align: right;
}

/* ── Sơ đồ lưới: sân khấu tường dùng chung (WallStage), giãn hết phần còn lại của thẻ ── */
.vw-dash-stage {
	flex: 1;
	min-height: 220px;
	display: flex;
	flex-direction: column;
}
/* Tấm màn trên sân khấu tối — cùng kiểu ô ở trang Màn hình / Phân quyền */
.vw-dash-screen-cell {
	@include vw-stage-tile;
	position: relative;
	box-sizing: border-box;
	width: 100%;
	height: 100%;
	display: flex;
	flex-direction: column;
	align-items: center;
	justify-content: center;
	padding: 2px;
	/* Vạch màu bộ điều khiển ở mép trên — thống nhất với tấm màn ở trang Màn hình / Phân quyền */
	border-top: 3px solid var(--el-border-color);
	cursor: default;
}
/* Ô trống: viền nét đứt, không có màn */
.vw-dash-screen-cell--empty {
	background: var(--wg-empty);
	border: 1px dashed var(--wg-empty-border);
	box-shadow: none;
}
.vw-dash-screen-cell--empty .vw-dash-screen-val {
	font-weight: 400;
	color: var(--wg-muted);
}
/* Chấm trạng thái kết nối góc trên phải: xanh = đang bật, đen = đang tắt (cùng màu thẻ số liệu) */
.vw-dash-screen-dot {
	position: absolute;
	top: 4px;
	right: 4px;
	width: 6px;
	height: 6px;
	border-radius: 50%;
	background: var(--el-text-color-primary);
}
/* Trên sân khấu tối: chấm "đang tắt" vẫn đen như chú thích */
.vw-dash-screen-cell .vw-dash-screen-dot {
	background: #000;
}
.vw-dash-screen-cell--on .vw-dash-screen-dot,
.vw-dash-screen-dot.is-on {
	background: var(--el-color-success);
	box-shadow: 0 0 0 2px var(--el-color-success-light-8);
}
.vw-dash-screen-code {
	max-width: 100%;
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
	font-size: 13px;
	font-weight: 700;
	color: #fff;
}
.vw-dash-screen-val {
	font-size: 11px;
	color: var(--wg-muted);
	white-space: nowrap;
}
/* Nhiệt độ / giờ hoạt động trong ô — cỡ chữ theo bề rộng ô (WallStage đặt --cell-w) */
.vw-dash-screen-metric {
	display: inline-flex;
	align-items: center;
	gap: 3px;
	max-width: 100%;
	overflow: hidden;
	white-space: nowrap;
	font-size: clamp(9px, calc(var(--cell-w, 96px) * 0.1), 12px);
	color: rgba(214, 228, 248, 0.85);
	font-variant-numeric: tabular-nums;
	.el-icon {
		flex: none;
		opacity: 0.7;
	}
}
.vw-dash-unplaced {
	display: flex;
	align-items: center;
	gap: 6px;
	margin-top: 10px;
	font-size: 12px;
	color: var(--el-color-warning);
}

/* ── Stats ── */
.vw-dash-stats {
	display: flex;
	flex-direction: column;
	gap: 12px;
}
.vw-dash-stat-row {
	display: flex;
	align-items: center;
	justify-content: space-between;
	padding: 8px 0;
	border-bottom: 1px dashed var(--el-border-color-lighter);
}
.vw-dash-stat-row:last-child {
	border-bottom: none;
}
.vw-dash-stat-label {
	font-size: 13px;
	color: var(--el-text-color-regular);
}
.vw-dash-stat-value {
	font-size: 14px;
	font-weight: 700;
	color: var(--el-text-color-primary);
}
</style>
