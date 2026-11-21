<!--
  Thẻ (card) giám sát MỘT thiết bị FMS.
  - Header: icon loại thiết bị (ảnh), tên thiết bị, tag loại, trạng thái kết nối (góc phải).
  - Mini-tab: Tài nguyên / Uptime / Môi trường (chấm báo khi tab có dữ liệu).
  - Body thường: tab Tài nguyên = 3 vòng % có icon (CPU/RAM/Disk, hover xem chi tiết); tab khác = danh sách giá trị.
  - Body lỗi: cảnh báo "không đọc được" + "Mất kết nối từ ...".
  - Footer: mốc thời gian (đỏ + chấm cảnh báo nếu dữ liệu cũ) + link Chi tiết/Kiểm tra.
  Author: ChuongLP
-->
<template>
	<div class="device-card" :class="`is-${conn.state}`">
		<!-- Header -->
		<div class="card-head">
			<div class="dev-title">
				<div class="dev-name" :title="row.equipmentName || row.equipmentCode">{{ row.equipmentName || row.equipmentCode || '-' }}</div>
				<el-tag size="small" effect="plain" type="info" class="type-tag">{{ typeName || '-' }}</el-tag>
			</div>
			<div class="dev-status" :class="`status-${conn.state}`">
				<span class="dot"></span>
				<span>{{ statusText }}</span>
			</div>
		</div>

		<!-- Trạng thái LỖI: không đọc được dữ liệu -->
		<div v-if="conn.state === 'offline' || conn.state === 'nodata'" class="card-body error-body">
			<div class="err-msg">
				<i class="fa fa-exclamation-triangle"></i>
				<span>{{ conn.state === 'nodata' ? $t('lz.label.vmsDeviceStatus.noData') : $t('lz.label.vmsDeviceStatus.cannotReadData') }}</span>
			</div>
			<div class="card-foot">
				<span v-if="conn.state === 'offline'" class="foot-time danger">
					{{ $t('lz.label.vmsDeviceStatus.disconnectedFrom', { time: formatTime(conn.since) }) }}
				</span>
				<span v-else class="foot-time muted">{{ $t('lz.label.vmsDeviceStatus.noData') }}</span>
				<a class="foot-link" @click="$emit('detail', row)">{{ $t('lz.label.vmsDeviceStatus.check') }} →</a>
			</div>
		</div>

		<!-- Trạng thái BÌNH THƯỜNG / DỮ LIỆU CŨ -->
		<div v-else class="card-body">
			<!-- Mini tabs -->
			<div class="mini-tabs">
				<span
					v-for="tab in tabs"
					:key="tab.col"
					class="mini-tab"
					:class="{ active: activeTab === tab.col }"
					@click="pickTab(tab.col)"
				>
					{{ $t(tab.labelKey) }}
					<span v-if="tab.hasData" class="tab-dot"></span>
				</span>
			</div>

			<!-- Nội dung tab -->
			<div class="tab-pane">
				<!-- Tab Tài nguyên: 3 vòng % (icon giữa vòng, màu theo ngưỡng <70 / 70-89 / >=90), hover xem đầy đủ -->
				<div v-if="activeTab === 'resourceMetric' && resourceRings.length" class="ring-row">
					<el-tooltip v-for="ring in resourceRings" :key="ring.key" placement="top" :show-after="150">
						<template #content>
							<div v-for="line in ring.tooltip" :key="line">{{ line }}</div>
						</template>
						<div class="ring-item" role="img" :aria-label="ring.tooltip.join(', ')">
							<div class="ring" :style="{ '--ring-color': ring.color }">
								<svg class="ring-svg" viewBox="0 0 64 64" aria-hidden="true">
									<circle class="ring-track" cx="32" cy="32" :r="RING_RADIUS" />
									<circle
										v-if="ringDash(ring.percent) > 0"
										class="ring-value"
										cx="32"
										cy="32"
										:r="RING_RADIUS"
										:stroke-dasharray="`${ringDash(ring.percent)} ${RING_CIRCUMFERENCE}`"
									/>
								</svg>
								<div class="ring-center">
									<i class="fa" :class="ring.icon"></i>
									<span class="ring-num">{{ ring.percent === null ? ring.value : `${Math.round(clampPercent(ring.percent))}%` }}</span>
								</div>
							</div>
							<div class="ring-label">
								{{ ring.label }}
								<i v-if="ring.warn" class="fa fa-exclamation-triangle ring-warn"></i>
							</div>
							<div class="ring-sub">{{ ring.sub || '\u00a0' }}</div>
						</div>
					</el-tooltip>
				</div>
				<template v-else-if="activeTab !== 'resourceMetric' && activeItems.length">
					<div v-for="item in activeItems" :key="item.key" class="metric-row">
						<template v-if="item.percent !== null">
							<div class="metric-line">
								<span class="metric-name">{{ item.label }} <span v-if="item.sub" class="metric-sub">{{ item.sub }}</span></span>
								<span class="metric-num" :style="{ color: item.color }">{{ item.value }}</span>
							</div>
							<el-progress :percentage="clampPercent(item.percent)" :color="item.color" :show-text="false" :stroke-width="6" />
						</template>
						<template v-else>
							<div class="metric-line">
								<span class="metric-name">{{ item.label }}</span>
								<span class="metric-num plain">{{ item.value }}</span>
							</div>
						</template>
					</div>
				</template>
				<div v-else class="pane-empty">{{ $t('lz.label.base.noData') }}</div>
			</div>

			<!-- Footer -->
			<div class="card-foot">
				<span class="foot-time" :class="{ danger: conn.isStale }">
					<span v-if="conn.isStale" class="stale-dot"></span>
					{{ formatTime(row.collectedTime) }}
				</span>
				<a class="foot-link" @click="$emit('detail', row)">{{ $t('lz.label.vmsDeviceStatus.detail') }} →</a>
			</div>
		</div>
	</div>
</template>

<script lang="ts" setup name="fmsDeviceCard">
import { computed, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useCommonVarsAndFuncs } from '/@/stores/its/commonVarsAndFuncs';
import { getConnState, getCardMetricItems, getResourceRings, columnHasData } from '/@/views/fms/telemetry/telemetryMetrics';

const { t: $t } = useI18n();
const commonVarsAndFuncs = useCommonVarsAndFuncs();

interface Props {
	row: any; // 1 dòng EquipmentLatestTelemetryOutput (kèm disconnectedSince runtime)
	typeName?: string; // tên loại thiết bị
	typeIcon?: string; // đường dẫn ảnh icon loại thiết bị (defaultIcon)
}
const props = withDefaults(defineProps<Props>(), {
	typeName: '',
	typeIcon: '',
});

defineEmits<{ (e: 'detail', row: any): void }>();

// Nhãn cột -> nhãn mini-tab theo yêu cầu (Tài nguyên / Uptime / Môi trường)
const TAB_LABELS: Record<string, string> = {
	resourceMetric: 'lz.label.vmsDeviceStatus.tabResource',
	diagnosticMetric: 'lz.label.vmsDeviceStatus.tabUptime',
	environmentMetric: 'lz.label.vmsDeviceStatus.tabEnvironment',
};
// Thứ tự tab hiển thị trên thẻ
const TAB_ORDER: Array<'resourceMetric' | 'diagnosticMetric' | 'environmentMetric'> = ['resourceMetric', 'diagnosticMetric', 'environmentMetric'];

const conn = computed(() => getConnState(props.row));

const typeIconUrl = computed(() => (props.typeIcon ? commonVarsAndFuncs.getFullUrl(props.typeIcon) : ''));

const statusText = computed(() => {
	switch (conn.value.state) {
		case 'online':
			return $t('lz.label.vmsDeviceStatus.online');
		case 'stale':
			return $t('lz.label.vmsDeviceStatus.online');
		case 'offline':
			return $t('lz.label.vmsDeviceStatus.lostSignal');
		default:
			return $t('lz.label.vmsDeviceStatus.noData');
	}
});

const tabs = computed(() =>
	TAB_ORDER.map((col) => ({
		col,
		labelKey: TAB_LABELS[col],
		hasData: columnHasData(props.row?.[col], col),
	}))
);

// Tab đang chọn. Mặc định = tab đầu tiên có dữ liệu (fallback Tài nguyên).
type TabCol = 'resourceMetric' | 'diagnosticMetric' | 'environmentMetric';
const userPicked = ref(false);
const activeTab = ref<TabCol>('resourceMetric');

// Người dùng chủ động chọn tab -> khóa không cho auto-default ghi đè khi refresh.
const pickTab = (col: TabCol) => {
	userPicked.value = true;
	activeTab.value = col;
};

// Khi dữ liệu tới/đổi mà người dùng CHƯA tự chọn -> đặt tab mặc định có dữ liệu.
watch(
	tabs,
	(list) => {
		if (userPicked.value) return;
		activeTab.value = (list.find((t) => t.hasData)?.col as TabCol) ?? 'resourceMetric';
	},
	{ immediate: true, deep: true }
);

// Tab Tài nguyên: 3 vòng CPU/RAM/Disk. Tab khác: liệt kê đầy đủ metric.
const resourceRings = computed(() => getResourceRings(props.row?.resourceMetric, $t));
const activeItems = computed(() => (activeTab.value === 'resourceMetric' ? [] : getCardMetricItems(props.row?.[activeTab.value], activeTab.value, $t)));

// Vòng SVG (viewBox 64): bán kính + chu vi để tính độ dài cung theo %
const RING_RADIUS = 26;
const RING_CIRCUMFERENCE = 2 * Math.PI * RING_RADIUS;
const ringDash = (p: number | null) => (clampPercent(p) / 100) * RING_CIRCUMFERENCE;

const clampPercent = (p: number | null) => {
	if (p === null) return 0;
	return Math.max(0, Math.min(100, Math.round(p * 10) / 10));
};

const formatTime = (d?: string | Date | null) => {
	if (!d) return '-';
	const date = new Date(d);
	if (isNaN(date.getTime())) return '-';
	return date.toLocaleString('vi-VN', { hour: '2-digit', minute: '2-digit', second: '2-digit', day: '2-digit', month: '2-digit', year: 'numeric' });
};
</script>

<style scoped>
.device-card {
	display: flex;
	flex-direction: column;
	border: 1px solid var(--el-border-color-light);
	border-radius: 8px;
	background: var(--el-bg-color);
	overflow: hidden;
	transition: box-shadow 0.2s, border-color 0.2s;
	height: 100%;
}
.device-card:hover {
	box-shadow: var(--el-box-shadow-light);
}
.device-card.is-offline {
	border-color: var(--el-color-danger-light-5);
	background: var(--el-color-danger-light-9);
}

/* ===== Header ===== */
.card-head {
	display: flex;
	align-items: center;
	gap: 10px;
	padding: 10px 12px;
	border-bottom: 1px solid var(--el-border-color-lighter);
}
.dev-icon {
	flex: 0 0 auto;
	width: 34px;
	height: 34px;
	border-radius: 6px;
	display: flex;
	align-items: center;
	justify-content: center;
	background: var(--el-fill-color-light);
	color: var(--el-color-primary);
	font-size: 18px;
	overflow: hidden;
}
.dev-icon img {
	width: 100%;
	height: 100%;
	object-fit: contain;
}
.dev-icon.icon-offline {
	color: var(--el-color-danger);
}
.dev-title {
	flex: 1 1 auto;
	min-width: 0;
	display: flex;
	align-items: center;
	gap: 8px;
}
.dev-name {
	font-weight: 600;
	font-size: 14px;
	color: var(--el-text-color-primary);
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
	max-width: 60%;
}
.type-tag {
	flex: 0 0 auto;
}
.dev-status {
	flex: 0 0 auto;
	display: inline-flex;
	align-items: center;
	gap: 5px;
	font-size: 12px;
	color: var(--el-color-success);
}
.dev-status .dot {
	width: 7px;
	height: 7px;
	border-radius: 50%;
	background: currentColor;
}
.dev-status.status-offline {
	color: var(--el-color-danger);
}
.dev-status.status-nodata {
	color: var(--el-text-color-secondary);
}

/* ===== Body ===== */
.card-body {
	display: flex;
	flex-direction: column;
	flex: 1 1 auto;
	padding: 8px 12px 10px;
}
.mini-tabs {
	display: flex;
	gap: 14px;
	border-bottom: 1px solid var(--el-border-color-lighter);
	margin-bottom: 8px;
}
.mini-tab {
	position: relative;
	padding: 4px 0 6px;
	font-size: 13px;
	color: var(--el-text-color-regular);
	cursor: pointer;
	border-bottom: 2px solid transparent;
	display: inline-flex;
	align-items: center;
	gap: 4px;
}
.mini-tab.active {
	color: var(--el-color-primary);
	border-bottom-color: var(--el-color-primary);
}
.tab-dot {
	width: 6px;
	height: 6px;
	border-radius: 50%;
	background: var(--el-color-success);
}
.tab-pane {
	flex: 1 1 auto;
	min-height: 96px; /* đủ chỗ cho hàng vòng tài nguyên -> đổi tab không làm thẻ nhảy chiều cao */
}

/* ===== Vòng tài nguyên (tab Tài nguyên) ===== */
.ring-row {
	display: grid;
	grid-template-columns: repeat(3, minmax(0, 1fr));
	gap: 6px;
	padding-top: 2px;
}
.ring-item {
	display: flex;
	flex-direction: column;
	align-items: center;
	min-width: 0;
	cursor: default;
}
.ring {
	position: relative;
	width: 60px;
	height: 60px;
}
.ring-svg {
	width: 100%;
	height: 100%;
	transform: rotate(-90deg); /* cung bắt đầu từ đỉnh vòng */
}
.ring-track {
	fill: none;
	stroke: var(--el-fill-color-dark);
	stroke-width: 6;
}
.ring-value {
	fill: none;
	stroke: var(--ring-color);
	stroke-width: 6;
	stroke-linecap: round;
	transition: stroke-dasharray 0.4s ease, stroke 0.4s ease;
}
.ring-center {
	position: absolute;
	inset: 0;
	display: flex;
	flex-direction: column;
	align-items: center;
	justify-content: center;
	gap: 1px;
	line-height: 1;
}
.ring-center .fa {
	font-size: 13px;
	color: var(--el-text-color-secondary);
}
.ring-num {
	font-size: 13px;
	font-weight: 700;
	color: var(--ring-color);
	font-variant-numeric: tabular-nums;
}
.ring-label {
	display: inline-flex;
	align-items: center;
	gap: 4px;
	margin-top: 4px;
	font-size: 12px;
	font-weight: 600;
	color: var(--el-text-color-regular);
}
.ring-warn {
	font-size: 11px;
	color: var(--el-color-danger);
}
.ring-sub {
	max-width: 100%;
	font-size: 11px;
	color: var(--el-text-color-secondary);
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
}
.metric-row {
	margin-bottom: 8px;
}
.metric-line {
	display: flex;
	align-items: center;
	justify-content: space-between;
	font-size: 13px;
	margin-bottom: 3px;
}
.metric-name {
	color: var(--el-text-color-regular);
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
}
.metric-sub {
	color: var(--el-text-color-secondary);
	font-size: 11px;
	margin-left: 2px;
}
.metric-num {
	font-weight: 600;
}
.metric-num.plain {
	color: var(--el-text-color-primary);
	font-weight: 500;
}
.pane-empty {
	color: var(--el-text-color-secondary);
	font-size: 13px;
	padding: 18px 0;
	text-align: center;
}

/* ===== Error body ===== */
.error-body {
	justify-content: space-between;
}
.err-msg {
	flex: 1 1 auto;
	display: flex;
	align-items: center;
	justify-content: center;
	gap: 8px;
	color: var(--el-color-danger);
	font-size: 13px;
	min-height: 78px;
}

/* ===== Footer ===== */
.card-foot {
	display: flex;
	align-items: center;
	justify-content: space-between;
	margin-top: 6px;
	padding-top: 8px;
	border-top: 1px dashed var(--el-border-color-lighter);
	font-size: 12px;
}
.foot-time {
	color: var(--el-text-color-secondary);
	display: inline-flex;
	align-items: center;
	gap: 5px;
}
.foot-time.danger {
	color: var(--el-color-danger);
	font-weight: 600;
}
.foot-time.muted {
	color: var(--el-text-color-secondary);
}
.stale-dot {
	width: 7px;
	height: 7px;
	border-radius: 50%;
	background: var(--el-color-danger);
	animation: stale-blink 1.2s ease-in-out infinite;
}
@keyframes stale-blink {
	0%,
	100% {
		opacity: 1;
	}
	50% {
		opacity: 0.3;
	}
}
.foot-link {
	color: var(--el-color-primary);
	cursor: pointer;
	white-space: nowrap;
}
.foot-link:hover {
	text-decoration: underline;
}
</style>
