<template>
  <div class="tms-topology-container">
    <el-card shadow="never" class="topo-card" :body-style="{ padding: '0' }" v-loading="state.loading">
      <!-- Toolbar -->
      <div class="topo-toolbar">
        <div class="toolbar-left">
          <span class="title">{{ $t('lz.label.fmsTopology.title') }}</span>
          <el-tag v-if="state.lastSync" type="info" size="small" effect="plain" round>
            <el-icon class="mr5"><Clock /></el-icon>{{ $t('lz.label.fmsTopology.lastSync', { time: formatSync(state.lastSync) }) }}
          </el-tag>
          <!-- Đã chuyển topo-legend xuống viewport -->
        </div>

        <!-- Bộ nút cố định cho cả 3 kiểu sơ đồ: không ẩn/hiện theo kiểu -> đổi Cây/Sao/Vòng thanh công cụ không bị nhảy -->
        <div class="toolbar-right">
          <!-- Kiểu sơ đồ: cây / sao / vòng -->
          <el-segmented v-model="state.layout" class="layout-switch" :options="layoutOptions" @change="saveLayout" />

          <el-input
            v-model="state.keyword"
            clearable
            class="kw-input"
            :prefix-icon="Search"
            :placeholder="$t('lz.label.fmsTopology.searchPlaceholder')"
          />

          <!-- Lọc theo trạng thái (chọn nhiều): node khớp được làm nổi theo màu trạng thái, node khác làm mờ -->
          <el-select
            v-model="state.stateFilter"
            multiple
            collapse-tags
            collapse-tags-tooltip
            clearable
            class="state-filter"
            :placeholder="$t('lz.label.fmsTopology.stateFilter')"
          >
            <el-option v-for="opt in stateOptions" :key="opt.key" :label="opt.name" :value="opt.key">
              <span class="state-opt">
                <i class="dot" :style="{ background: opt.color }" />
                <span>{{ opt.name }}</span>
                <span class="cnt">{{ opt.count }}</span>
              </span>
            </el-option>
          </el-select>

          <div class="tool-group">
            <el-button-group>
              <el-button :icon="ZoomOut" @click="zoomBy(-0.15)" :title="$t('lz.label.fmsTopology.zoomOut')" />
              <el-button class="zoom-value" @click="resetView" :title="$t('lz.label.fmsTopology.zoomReset')">{{ Math.round(state.zoom * 100) }}%</el-button>
              <el-button :icon="ZoomIn" @click="zoomBy(0.15)" :title="$t('lz.label.fmsTopology.zoomIn')" />
              <el-button :icon="Aim" @click="centerView" :title="$t('lz.label.fmsTopology.center')" />
            </el-button-group>

            <!-- Hình vòng không có nhánh để thu/mở -> vô hiệu hoá (giữ chỗ) thay vì ẩn -->
            <el-button-group>
              <el-button :icon="Expand" :disabled="state.layout === 'ring'" @click="expandAll" :title="$t('lz.label.fmsTopology.expandAll')" />
              <el-button :icon="Fold" :disabled="state.layout === 'ring'" @click="collapseAll" :title="$t('lz.label.fmsTopology.collapseAll')" />
            </el-button-group>
          </div>

          <div class="tool-group">
            <el-button v-auth="'fmsTopology:manualFetch'" type="warning" :icon="Download" :loading="isFetching" @click="handleManualFetch">{{ $t('lz.button.fmsTopology.manualFetch') }}</el-button>
            <el-button v-auth="'fmsTopology:refresh'" type="primary" :icon="Refresh" @click="loadData">{{ $t('lz.button.base.refresh') }}</el-button>
          </div>
        </div>
      </div>

      <!-- Viewport (zoom + pan) -->
      <div
        ref="viewportRef"
        class="topo-viewport"
        :class="{ grabbing: state.dragging, 'is-chart': state.layout !== 'tree' }"
        @wheel.prevent="onWheel"
        @mousedown="onDragStart"
      >
        <!-- Panel chú thích trạng thái dạng Float -->
        <div class="topo-legend-panel" :class="{ collapsed: !state.legendVisible }" @mousedown.stop @wheel.stop>
          <div class="legend-header" @click="toggleLegend">
            <span>{{ $t('lz.label.fmsTopology.legendTitle') }}</span>
            <el-icon class="legend-toggle">
              <ArrowDown v-if="state.legendVisible" />
              <ArrowUp v-else />
            </el-icon>
          </div>
          <div class="legend-content" v-show="state.legendVisible">
            <!-- Bấm vào dòng trạng thái để bật/tắt lọc (đồng bộ với ô lọc trên thanh công cụ) -->
            <div
              class="lg lg-state"
              v-for="opt in stateOptions"
              :key="opt.key"
              :class="{ active: state.stateFilter.includes(opt.key), muted: hasStateFilter && !state.stateFilter.includes(opt.key) }"
              @click="toggleStateFilter(opt.key)"
            >
              <i class="dot" :style="{ background: opt.color }" />
              <span>{{ opt.name }}</span>
              <span class="cnt">{{ opt.count }}</span>
            </div>
            <div class="lg lg-its">
              <span class="its-badge">ITS</span>
              <span>{{ $t('lz.label.fmsTopology.managedCount') }}</span>
              <span class="cnt">{{ managedCount.managed }}/{{ managedCount.total }}</span>
            </div>
          </div>
        </div>

        <div v-if="state.layout === 'tree'" class="topo-canvas" :style="canvasStyle">
          <OrganizationChart
            v-if="state.tree"
            :value="state.tree"
            collapsible
            v-model:collapsedKeys="state.collapsedKeys"
          >
            <!-- ... giữ nguyên template của OrganizationChart ... -->
            <template #default="{ node }">
              <!-- Thẻ chỉ hiện Tên CyberMonitor + IP + Trạng thái; hover / nhấp mới hiện thêm Thiết bị ITS + Lý trình.
                   Thiết bị ngoài ITS (không có tên ITS lẫn Km) -> không hiện gì thêm. -->
              <el-popover
                placement="right"
                :width="240"
                :trigger="['hover', 'click']"
                :show-after="150"
                :disabled="!hasItsInfo(node.data)"
                popper-class="topo-node-popover"
              >
                <template #reference>
                  <div
                    class="topo-node"
                    :class="{
                      'is-root': !node.data.ip,
                      'has-info': hasItsInfo(node.data),
                      'is-hit': hasFilter && isMatch(node.data),
                      'is-dim': hasFilter && !isMatch(node.data),
                    }"
                    :style="nodeAccentStyle(node.data)"
                  >
                    <div class="node-head">
                      <span class="node-name" :title="node.data.name">{{ node.data.name }}</span>
                    </div>

                    <div class="node-ip" v-if="node.data.ip">{{ node.data.ip }}</div>

                    <div class="node-status" v-if="node.data.ip">
                      <span class="dot" :style="{ background: stateColor(stateItem(node.data.equipmentState)) }" />
                      <span class="txt" :style="{ color: stateTextColor(node.data.equipmentState) }">
                        {{ stateLabel(node.data.equipmentState) }}
                      </span>
                    </div>
                  </div>
                </template>

                <div class="node-detail">
                  <div class="detail-row">
                    <span class="detail-label">{{ $t('lz.label.fmsTopology.itsEquipment') }}</span>
                    <span class="detail-value its-value">
                      <span v-if="node.data.equipmentName" class="its-badge">ITS</span>
                      {{ node.data.equipmentName || '' }}
                    </span>
                  </div>
                  <div class="detail-row">
                    <span class="detail-label">{{ $t('lz.label.fmsTopology.location') }}</span>
                    <span class="detail-value">{{ formatKmMet(node.data.kmNumber, node.data.metNumber) }}</span>
                  </div>
                </div>
              </el-popover>
            </template>
          </OrganizationChart>
        </div>

        <topologyChart
          v-else-if="state.roots.length"
          ref="chartRef"
          class="topo-chart"
          :roots="state.roots"
          :layout="state.layout"
          :keyword="state.keyword"
          :state-filter="state.stateFilter"
          :state-key-of="stateKey"
          :color-of="(code) => stateColor(stateItem(code))"
          :label-of="stateLabel"
          :format-km-met="formatKmMet"
          @zoom="(z) => (state.zoom = z)"
        />

        <el-empty v-if="!state.tree && !state.loading" :description="$t('lz.label.base.noData')" class="topo-empty" />
      </div>
    </el-card>
  </div>
</template>

<script lang="ts" setup name="tmsTopology">
import { computed, nextTick, onMounted, reactive, ref, watch } from 'vue';
import { ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { Refresh, ZoomIn, ZoomOut, Expand, Fold, Monitor, Share, Clock, Aim, Download, ArrowDown, ArrowUp, Search } from '@element-plus/icons-vue';
import OrganizationChart from 'primevue/organizationchart';
import { getAPI } from '/@/utils/axios-utils';
import { FmsTopologyApi } from '/@/api-services/fms';
import { BaseConfigTypeEnum } from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';
import { useManualFetchNats } from '/@/transporter/hooks/useManualFetchNats';
import { TransporterEvent } from '/@/transporter/constants/transporterEvent';
import topologyChart from '/@/views/fms/topology/component/topologyChart.vue';

const { t: $t } = useI18n();

const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
const getConfigItemByCode = useBaseConfig().getConfigItemByCode;

// Khớp với TopologyNodeOutput của BE (camelCase khi serialize JSON)
interface NagiosNode {
  name: string;
  ip: string;
  equipmentName?: string | null; // tên thiết bị ITS khớp theo IP (null = ngoài ITS)
  equipmentState?: number | null; // BE trả "equipmentState", KHÔNG phải "status"
  kmNumber?: number | null;
  metNumber?: number | null;
  children?: NagiosNode[];
}

const viewportRef = ref<HTMLElement>();
const chartRef = ref<InstanceType<typeof topologyChart>>();

// Kiểu sơ đồ đang xem, ghi nhớ giữa các lần vào trang (mặc định: hình cây)
type TopoLayout = 'tree' | 'star' | 'ring';
const LAYOUT_STORAGE_KEY = 'fmsTopologyLayout';
const readLayout = (): TopoLayout => {
  try {
    const v = localStorage.getItem(LAYOUT_STORAGE_KEY);
    return v === 'star' || v === 'ring' ? v : 'tree';
  } catch {
    return 'tree';
  }
};
function saveLayout() {
  try {
    localStorage.setItem(LAYOUT_STORAGE_KEY, state.layout);
  } catch {
    // bỏ qua: không lưu được thì lần sau về mặc định
  }
}
const layoutOptions = computed(() => [
  { label: $t('lz.label.fmsTopology.layoutTree'), value: 'tree' },
  { label: $t('lz.label.fmsTopology.layoutStar'), value: 'star' },
  { label: $t('lz.label.fmsTopology.layoutRing'), value: 'ring' },
]);

// Ghi nhớ trạng thái đóng/mở bảng chú thích giữa các lần vào trang (mặc định: mở)
const LEGEND_STORAGE_KEY = 'fmsTopologyLegendVisible';
const readLegendVisible = (): boolean => {
  try {
    return localStorage.getItem(LEGEND_STORAGE_KEY) !== '0';
  } catch {
    return true;
  }
};

const state = reactive({
  legendVisible: readLegendVisible(),
  layout: readLayout() as TopoLayout,
  keyword: '' as string,
  stateFilter: [] as string[], // khoá trạng thái đang lọc (xem stateKey)
  loading: false,
  roots: [] as NagiosNode[], // dữ liệu gốc từ BE, dùng cho hình sao / vòng
  tree: null as any,
  lastSync: '' as string,
  collapsedKeys: {} as Record<string, boolean>,
  zoom: 1,
  pan: { x: 20, y: 20 },
  dragging: false,
});

// Đóng/mở bảng chú thích
function toggleLegend() {
  state.legendVisible = !state.legendVisible;
  try {
    localStorage.setItem(LEGEND_STORAGE_KEY, state.legendVisible ? '1' : '0');
  } catch {
    // bỏ qua: không lưu được thì chỉ mất ghi nhớ
  }
}

/* ---------- Trạng thái thiết bị: lấy động từ cấu hình TmsEquipmentState ---------- */
const equipStateList = computed(() => getConfigDataByCode(BaseConfigTypeEnum.TmsEquipmentState) || []);

// Lấy config item theo code trạng thái
function stateItem(code?: number | null): any {
  if (code == null) return null;
  return getConfigItemByCode(BaseConfigTypeEnum.TmsEquipmentState, code);
}

// Tên hiển thị của trạng thái (theo cấu hình), fallback "Không xác định"
function stateLabel(code?: number | null): string {
  return stateItem(code)?.name ?? $t('lz.label.fmsTopology.unknown');
}

// Màu theo tagType của config item (success/danger/warning/info...), fallback info.
// Lưu ý: chỉnh lại tên field (tagType/type/color) cho đúng cấu trúc config item của dự án — giống field mà TagInfo đang dùng.
function stateColor(item: any): string {
  const tag = item?.tagType ?? item?.type;
  return tag ? `var(--el-color-${tag})` : 'var(--el-color-info)';
}

// Màu chữ trạng thái: nhấn màu khi trạng thái thuộc nhóm danger
function stateTextColor(code?: number | null): string {
  const tag = stateItem(code)?.tagType ?? stateItem(code)?.type;
  return tag === 'danger' ? 'var(--el-color-danger)' : 'var(--el-text-color-secondary)';
}

// Accent (viền trên) của node card theo trạng thái; node root dùng màu primary (xử lý bằng class is-root).
// --hl-color: màu viền sáng khi node khớp bộ lọc -> lọc theo trạng thái thì node sáng đúng màu trạng thái.
function nodeAccentStyle(data: any) {
  if (!data.ip) return {};
  const color = stateColor(stateItem(data.equipmentState));
  return { borderTopColor: color, ...(hasStateFilter.value ? { '--hl-color': color } : {}) };
}

/* ---------- Tìm kiếm theo tên CyberMonitor / tên thiết bị ITS / IP ---------- */
const hasKeyword = computed(() => !!state.keyword.trim());
function isHit(data: { name?: string; ip?: string; equipmentName?: string | null }): boolean {
  const kw = state.keyword.trim().toLowerCase();
  return [data.name, data.equipmentName, data.ip].some((v) => (v ?? '').toLowerCase().includes(kw));
}

/* ---------- Lọc theo trạng thái ---------- */
// Khoá trạng thái dạng chuỗi (mã cấu hình có thể là số hoặc chuỗi); trạng thái không có trong cấu hình -> "unknown"
const UNKNOWN_STATE = 'unknown';
function stateKey(code?: number | null): string {
  return stateItem(code) ? String(code) : UNKNOWN_STATE;
}

const hasStateFilter = computed(() => state.stateFilter.length > 0);
const hasFilter = computed(() => hasKeyword.value || hasStateFilter.value);

// Chỉ node thiết bị (có IP) mới có trạng thái -> node gốc / mạng ảo không khớp khi đang lọc trạng thái
function isStateHit(data: { ip?: string; equipmentState?: number | null }): boolean {
  return !!data.ip && state.stateFilter.includes(stateKey(data.equipmentState));
}

// Node khớp đồng thời từ khoá (nếu có) và trạng thái (nếu có)
function isMatch(data: any): boolean {
  return (!hasKeyword.value || isHit(data)) && (!hasStateFilter.value || isStateHit(data));
}

function toggleStateFilter(key: string) {
  state.stateFilter = state.stateFilter.includes(key) ? state.stateFilter.filter((k) => k !== key) : [...state.stateFilter, key];
}

// Node thiết bị có thông tin ITS (tên thiết bị ITS hoặc Km) -> mới mở popover chi tiết khi hover / nhấp
function hasItsInfo(data: { ip?: string; equipmentName?: string | null; kmNumber?: number | null; metNumber?: number | null }): boolean {
  return !!data.ip && (!!data.equipmentName || data.kmNumber != null || data.metNumber != null);
}

/* ---------- Số thiết bị (node có IP) ITS đang quản lý ---------- */
const managedCount = computed(() => {
  let managed = 0;
  let total = 0;
  const walk = (n: NagiosNode) => {
    if (n.ip) {
      total++;
      if (n.equipmentName) managed++;
    }
    (n.children || []).forEach(walk);
  };
  state.roots.forEach(walk);
  return { managed, total };
});

/* ---------- Đếm thiết bị theo trạng thái (hiện trong chú thích) ---------- */
// Key = stateKey (mã trạng thái / "unknown"). Chỉ đếm node thiết bị (có IP).
const stateCounts = computed(() => {
  const map = new Map<string, number>();
  const walk = (n: NagiosNode) => {
    if (n.ip) {
      const key = stateKey(n.equipmentState);
      map.set(key, (map.get(key) ?? 0) + 1);
    }
    (n.children || []).forEach(walk);
  };
  state.roots.forEach(walk);
  return map;
});

// Danh sách trạng thái dùng chung cho ô lọc + bảng chú thích (kèm "Không xác định")
const stateOptions = computed(() => [
  ...equipStateList.value.map((item: any) => ({
    key: String(item.code),
    name: item.name,
    color: stateColor(item),
    count: stateCounts.value.get(String(item.code)) ?? 0,
  })),
  { key: UNKNOWN_STATE, name: $t('lz.label.fmsTopology.unknown'), color: 'var(--el-color-info)', count: stateCounts.value.get(UNKNOWN_STATE) ?? 0 },
]);

/* ---------- Hiển thị Km + m dạng "Km100+00" ---------- */
function formatKmMet(km?: number | null, met?: number | null): string {
  if (km == null && met == null) return '';
  return `Km${km ?? 0}+${String(met ?? 0).padStart(2, '0')}`;
}

/* ---------- Transform dữ liệu ---------- */
let keySeq = 0;
const branchKeys: string[] = [];

// Chuyển 1 node NagiosNode thành node OrganizationChart
function toOrgNode(node: NagiosNode): any {
  const key = String(keySeq++);
  const children = (node.children || []).map(toOrgNode);
  if (children.length) branchKeys.push(key);
  return {
    key,
    data: {
      name: node.name,
      ip: node.ip,
      equipmentName: node.equipmentName,
      equipmentState: node.equipmentState,
      kmNumber: node.kmNumber,
      metNumber: node.metNumber,
    },
    children,
  };
}

// Chuyển mảng gốc thành cây OrganizationChart
function buildTree(roots: NagiosNode[]): any {
  keySeq = 0;
  branchKeys.length = 0;
  if (!roots || roots.length === 0) return null;
  if (roots.length === 1) return toOrgNode(roots[0]); // 1 node gốc -> không cần root ảo
  const children = roots.map(toOrgNode); // nhiều node gốc -> tạo root ảo
  branchKeys.push('root');
  return { key: 'root', data: { name: 'Network', ip: '' }, children };
}

/* ---------- Expand / Collapse ---------- */
// Hình cây: qua collapsedKeys của OrganizationChart; hình sao: ECharts tree mở hết / chỉ giữ tầng đầu
function expandAll() {
  if (state.layout === 'star') return chartRef.value?.setExpanded(true);
  state.collapsedKeys = {};
}

function collapseAll() {
  if (state.layout === 'star') return chartRef.value?.setExpanded(false);
  const map: Record<string, boolean> = {};
  branchKeys.forEach((k) => (map[k] = true));
  state.collapsedKeys = map;
}

/* ---------- Zoom / Pan ---------- */
const canvasStyle = computed(() => ({
  transform: `translate(${state.pan.x}px, ${state.pan.y}px) scale(${state.zoom})`,
  transformOrigin: '0 0',
}));

function clamp(v: number, min: number, max: number) {
  return Math.min(max, Math.max(min, v));
}

// Zoom / canh giữa dùng chung cho 3 kiểu: hình sao / vòng chuyển xuống ECharts (topologyChart)
function zoomBy(delta: number) {
  if (state.layout !== 'tree') return chartRef.value?.zoomBy(delta);
  state.zoom = clamp(+(state.zoom + delta).toFixed(2), 0.3, 2);
}

function resetView() {
  if (state.layout !== 'tree') return chartRef.value?.resetView();
  state.zoom = 1;
  state.pan = { x: 20, y: 20 };
}

function centerView() {
  if (state.layout !== 'tree') return chartRef.value?.resetView();
  centerTree();
}

// Đổi kiểu sơ đồ: luôn canh vừa khung để mỗi kiểu mở ra ở vị trí ổn định (không giữ pan/zoom của kiểu trước)
watch(
  () => state.layout,
  (layout) => {
    if (layout === 'tree') centerTree();
  }
);

function onWheel(e: WheelEvent) {
  if (state.layout !== 'tree') return; // hình sao / vòng: ECharts tự xử lý zoom
  const rect = viewportRef.value?.getBoundingClientRect();
  if (!rect) return;
  const cx = e.clientX - rect.left;
  const cy = e.clientY - rect.top;
  const prev = state.zoom;
  const next = clamp(+(prev + (e.deltaY < 0 ? 0.12 : -0.12)).toFixed(2), 0.3, 2);
  const scale = next / prev;
  state.pan.x = cx - (cx - state.pan.x) * scale;
  state.pan.y = cy - (cy - state.pan.y) * scale;
  state.zoom = next;
}

let dragStart = { x: 0, y: 0, px: 0, py: 0 };

function onDragStart(e: MouseEvent) {
  if (state.layout !== 'tree') return;
  if ((e.target as HTMLElement).closest('.p-organizationchart-node-toggle-button')) return;
  state.dragging = true;
  dragStart = { x: e.clientX, y: e.clientY, px: state.pan.x, py: state.pan.y };
  window.addEventListener('mousemove', onDragMove);
  window.addEventListener('mouseup', onDragEnd);
}

function onDragMove(e: MouseEvent) {
  if (!state.dragging) return;
  state.pan.x = dragStart.px + (e.clientX - dragStart.x);
  state.pan.y = dragStart.py + (e.clientY - dragStart.y);
}

function onDragEnd() {
  state.dragging = false;
  window.removeEventListener('mousemove', onDragMove);
  window.removeEventListener('mouseup', onDragEnd);
}

/* ---------- Helpers ---------- */
function formatSync(iso: string): string {
  const d = new Date(iso);
  if (isNaN(d.getTime())) return iso;
  return d.toLocaleString('vi-VN');
}

/* ---------- Load ---------- */
async function loadData() {
  state.loading = true;
  try {
    const res = await getAPI(FmsTopologyApi).apiFmsFmstopologyDataconfigGet();
    const files = res.data.result ?? [];
    const nagios = files[0]?.content;
    state.lastSync = nagios?.lastTimeSync ?? '';
    state.roots = nagios?.roots ?? [];
    state.tree = buildTree(state.roots);
    state.collapsedKeys = {};
    centerTree(); // centerTree tự tính zoom fit, không cần reset zoom trước
  } catch {
    ElMessage.error($t('lz.message.fmsTopology.loadError'));
    state.roots = [];
    state.tree = null;
  } finally {
    state.loading = false;
  }
}

// Canh giữa cây topology trong viewport (sau khi load xong)
async function centerTree() {
  await nextTick();
  const vp = viewportRef.value;
  const canvas = vp?.querySelector('.topo-canvas') as HTMLElement | null;
  if (!vp || !canvas) return;

  const w = canvas.scrollWidth;
  const h = canvas.scrollHeight;
  const fit = Math.min((vp.clientWidth - 40) / w, (vp.clientHeight - 40) / h, 1);
  state.zoom = clamp(+fit.toFixed(2), 0.3, 1);

  state.pan.x = Math.max(20, (vp.clientWidth - w * state.zoom) / 2);
  state.pan.y = 20;
}

/* ---------- Lấy dữ liệu topology thủ công (BE -> NATS -> DeviceMonitor -> phản hồi NATS) ---------- */
const { isFetching, trigger } = useManualFetchNats(TransporterEvent.FmsControl, {
	onSuccess: () => loadData(), // nhận kết quả thành công -> nạp lại sơ đồ từ file
  timeoutMs: 10000, // Thêm dòng này: 15.000 ms (10 giây) thay vì chờ 2 phút
	messages: {
		sent: $t('lz.message.fmsTopology.fetchSent'),
		successTitle: $t('lz.message.fmsTopology.fetchSuccessTitle'),
		failedTitle: $t('lz.message.fmsTopology.fetchFailedTitle'),
		timeout: $t('lz.message.fmsTopology.fetchTimeout'),
	},
});
const handleManualFetch = () => trigger(() => getAPI(FmsTopologyApi).apiFmsFmstopologyManualfetchPost({}));

onMounted(loadData);
</script>

<style scoped lang="scss">
.tms-topology-container {
  padding: 10px;
  height: 100%;
  box-sizing: border-box;
}
.topo-card {
  border: 1px solid var(--el-border-color-light);
}

/* Toolbar */
.topo-toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: 10px;
  padding: 12px 16px;
  border-bottom: 1px solid var(--el-border-color-lighter);
  background: var(--el-fill-color-lighter);

  .toolbar-left {
    display: flex;
    align-items: center;
    gap: 10px;
    .title {
      font-size: 16px;
      font-weight: 600;
      color: var(--el-text-color-primary);
    }
  }
  .toolbar-right {
    display: flex;
    align-items: center;
    flex-wrap: wrap;
    gap: 10px;

    .kw-input {
      width: 220px;
    }

    .state-filter {
      width: 220px;
    }

    /* Mỗi lựa chọn Cây/Sao/Vòng rộng bằng nhau -> con trượt không đổi kích thước khi chuyển */
    .layout-switch :deep(.el-segmented__item) {
      min-width: 96px;
      justify-content: center;
    }

    .tool-group {
      display: flex;
      align-items: center;
      gap: 8px;
      padding-left: 10px;
      border-left: 1px solid var(--el-border-color-lighter);

      .el-button + .el-button {
        margin-left: 0;
      }
    }

    /* % zoom rộng cố định (vd 85% / 100%) để nhóm nút không co giãn */
    .zoom-value {
      width: 58px;
      padding-left: 0;
      padding-right: 0;
      font-variant-numeric: tabular-nums;
    }
  }
}



/* Viewport */
.topo-viewport {
  position: relative;
  width: 100%;
  height: calc(100vh - 200px);
  min-height: 420px;
  overflow: hidden;
  background: radial-gradient(var(--el-border-color-lighter) 1px, transparent 1px);
  background-size: 22px 22px;
  cursor: grab;

  &.grabbing {
    cursor: grabbing;
  }

  /* Hình sao / vòng: ECharts tự lo con trỏ khi kéo */
  &.is-chart {
    cursor: default;
  }
}

.topo-chart {
  position: absolute;
  inset: 0;
}

/* THÊM ĐOẠN SAU VÀO DƯỚI .topo-viewport */
.topo-legend-panel {
  position: absolute;
  bottom: 20px;
  left: 20px;
  background: var(--el-bg-color-overlay);
  border: 1px solid var(--el-border-color-light);
  border-radius: 6px;
  box-shadow: var(--el-box-shadow);
  display: flex;
  flex-direction: column;
  max-height: 250px; /* Giới hạn chiều cao panel */
  width: 200px;
  z-index: 10;
  overflow: hidden;

  .legend-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 8px;
    padding: 10px 12px;
    font-size: 13px;
    font-weight: 600;
    color: var(--el-text-color-primary);
    border-bottom: 1px solid var(--el-border-color-lighter);
    background: var(--el-fill-color-light);
    flex-shrink: 0;
    cursor: pointer;
    user-select: none;

    &:hover {
      background: var(--el-fill-color);
    }

    .legend-toggle {
      color: var(--el-text-color-secondary);
    }
  }

  /* Thu gọn: chỉ còn thanh tiêu đề nhỏ, bấm để mở lại */
  &.collapsed {
    width: auto;

    .legend-header {
      border-bottom: none;
    }
  }

  .legend-content {
    padding: 10px 12px;
    overflow-y: auto; /* Hiển thị thanh cuộn dọc khi quá nhiều items */
    display: flex;
    flex-direction: column;
    gap: 10px;

    /* Styling thanh cuộn cho nhỏ gọn mượt mà */
    &::-webkit-scrollbar {
      width: 5px;
    }
    &::-webkit-scrollbar-thumb {
      background: var(--el-border-color-darker);
      border-radius: 4px;
    }

    .lg {
      display: flex;
      align-items: center;
      gap: 8px;
      font-size: 12px;
      color: var(--el-text-color-regular);

      .dot {
        width: 10px;
        height: 10px;
        border-radius: 50%;
        flex-shrink: 0;
      }

      .cnt {
        margin-left: auto;
        font-weight: 600;
        color: var(--el-text-color-primary);
      }
    }

    /* Dòng trạng thái bấm được để lọc */
    .lg-state {
      margin: -3px -6px;
      padding: 3px 6px;
      border-radius: 4px;
      cursor: pointer;
      user-select: none;
      transition: background-color 0.15s, opacity 0.15s;

      &:hover {
        background: var(--el-fill-color-light);
      }
      &.active {
        background: var(--el-color-primary-light-9);
        font-weight: 600;
      }
      &.muted {
        opacity: 0.45;
      }
    }
  }
}

/* Lựa chọn trong ô lọc trạng thái: chấm màu + tên + số lượng */
.state-opt {
  display: flex;
  align-items: center;
  gap: 8px;
  width: 100%;

  .dot {
    width: 10px;
    height: 10px;
    border-radius: 50%;
    flex-shrink: 0;
  }

  .cnt {
    margin-left: auto;
    padding-left: 12px;
    font-weight: 600;
    color: var(--el-text-color-secondary);
  }
}
/* Nhãn "ITS" dùng chung cho thẻ node và chú thích */
.its-badge {
  flex-shrink: 0;
  padding: 2px 5px;
  border-radius: 4px;
  font-size: 10px;
  font-weight: 700;
  line-height: 1;
  letter-spacing: 0.3px;
  color: #fff;
  background: var(--el-color-primary);
}

/* Popover chi tiết thiết bị: Thiết bị ITS + Lý trình */
.node-detail {
  display: flex;
  flex-direction: column;
  gap: 8px;

  .detail-row {
    display: grid;
    grid-template-columns: 84px minmax(0, 1fr);
    align-items: center;
    gap: 8px;
    min-height: 20px;
    font-size: 12px;
  }

  .detail-label {
    color: var(--el-text-color-secondary);
  }

  .detail-value {
    font-weight: 600;
    color: var(--el-text-color-primary);
    word-break: break-word;
  }

  .its-value {
    display: flex;
    align-items: center;
    gap: 6px;
  }
}

.topo-legend-panel .lg-its {
  padding-top: 8px;
  border-top: 1px dashed var(--el-border-color-lighter);
}

.topo-canvas {
  display: inline-block;
  padding: 10px;
  will-change: transform;
}
.topo-empty {
  position: absolute;
  inset: 0;
  display: flex;
  align-items: center;
  justify-content: center;
}

/* Node card: rộng cố định + căn giữa để các nhánh cân xứng */
.topo-node {
  position: relative;
  display: flex;
  flex-direction: column;
  align-items: center;
  box-sizing: border-box;
  width: 180px;
  min-height: 88px;
  padding: 10px 12px 12px;
  text-align: center;
  border-radius: 10px;
  background: var(--el-bg-color);
  border: 1px solid var(--el-border-color);
  border-top: 3px solid var(--el-border-color-dark); /* fallback, sẽ bị override bởi inline style theo trạng thái */
  box-shadow: var(--el-box-shadow-light);
  transition: transform 0.15s ease, box-shadow 0.15s ease;

  &:hover {
    transform: translateY(-2px);
    box-shadow: var(--el-box-shadow);
  }

  &.is-root {
    justify-content: center;
    min-height: 56px;
    border-top-color: var(--el-color-primary);
    background: var(--el-color-primary-light-9);

    .node-name {
      font-size: 14px;
    }
  }

  /* Tìm kiếm / lọc trạng thái: node khớp viền sáng (màu trạng thái khi lọc trạng thái, primary khi chỉ tìm từ khoá), node không khớp làm mờ */
  &.is-hit {
    box-shadow: 0 0 0 2px var(--hl-color, var(--el-color-primary)), 0 0 16px 2px var(--hl-color, var(--el-color-primary));
  }
  &.is-dim {
    opacity: 0.3;
  }

  /* Có thông tin ITS (tên / Km): con trỏ tay báo hiệu hover / nhấp để xem chi tiết */
  &.has-info {
    cursor: pointer;
  }

  .node-head {
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 6px;
    max-width: 100%;

    .node-icon {
      font-size: 15px;
      color: var(--el-color-primary);
      flex-shrink: 0;
    }
    .node-name {
      font-weight: 600;
      font-size: 13px;
      line-height: 1.3;
      color: var(--el-text-color-primary);
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }
  }

  .node-ip {
    font-family: 'Consolas', 'Courier New', monospace;
    font-size: 12px;
    color: var(--el-text-color-secondary);
    margin-top: 4px;
  }

  .node-status {
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 5px;
    margin-top: auto; /* dòng trạng thái luôn nằm đáy thẻ */
    padding-top: 8px;

    .dot {
      width: 8px;
      height: 8px;
      border-radius: 50%;
      background: var(--el-color-info);
    }
    .txt {
      font-size: 11px;
      color: var(--el-text-color-secondary);
    }
  }
}

/* Tùy biến đường nối + node-content của PrimeVue OrganizationChart */
:deep(.p-organizationchart-node-content) {
  border: none;
  background: transparent;
  padding: 0;
}
:deep(.p-organizationchart-line-down) {
  background: var(--el-border-color-dark);
  width: 2px;
}
:deep(.p-organizationchart-line-left),
:deep(.p-organizationchart-line-right) {
  border-color: var(--el-border-color-dark);
  border-width: 2px;
}
:deep(.p-organizationchart-node-toggle-button) {
  background: var(--el-bg-color);
  border: 1px solid var(--el-border-color);
  color: var(--el-text-color-regular);
}
</style>