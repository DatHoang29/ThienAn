<template>
  <div ref="overlayRef" class="block-map-layer" :aria-label="$t('lz.label.tmsMapLocation.deviceBlocks')">
    <!-- Toạ độ (points/cx/cy) được ghi trực tiếp vào DOM trong syncLayout() để pan/zoom mượt, không re-render Vue mỗi frame -->
    <svg ref="connectorsRef" class="block-map-layer__connectors" aria-hidden="true">
      <g v-for="block in visibleBlocks" :key="`line_${block.id}`" :data-block-line-group="block.id">
        <!-- Đường nối đi qua các điểm bẻ góc (waypoints); không có waypoint = đường thẳng như trước -->
        <polyline data-role="line" class="block-map-layer__connector" :style="getConnectorStyle(block)" />

        <!-- Tay cầm bẻ góc: chỉ hiện ở chế độ cấu hình -->
        <template v-if="isEditMode">
          <!-- Chấm mờ giữa mỗi đoạn: bấm để chèn thêm điểm bẻ góc tại đó -->
          <circle
            v-for="segmentIndex in getSegmentIndexes(block)"
            :key="`mid_${block.id}_${segmentIndex}`"
            data-role="mid"
            r="4"
            class="block-map-layer__mid-handle"
            @pointerdown.stop.prevent="insertWaypoint(block, segmentIndex)"
          />
          <!-- Điểm bẻ góc: kéo để di chuyển, chuột phải để xoá -->
          <circle
            v-for="(waypoint, waypointIndex) in getWaypoints(block)"
            :key="`waypoint_${block.id}_${waypointIndex}`"
            data-role="waypoint"
            r="5"
            class="block-map-layer__waypoint"
            :style="getWaypointStyle(block)"
            @pointerdown.stop.prevent="startWaypointDrag($event, block, waypointIndex)"
            @contextmenu.prevent.stop="removeWaypoint(block, waypointIndex)"
          />
        </template>

        <circle
          data-role="anchor"
          :r="getAnchorRadius(block)"
          class="block-map-layer__anchor"
          :style="getAnchorStyle(block)"
          @pointerdown.stop.prevent="startAnchorDrag($event, block)"
        />
      </g>
    </svg>

    <article
      v-for="(block, blockIndex) in visibleBlocks"
      :key="block.id"
      class="map-block"
      :data-block-id="block.id"
      :class="{
        'map-block--editable': isEditMode,
        'map-block--route': hasRouteLevel(block),
        'map-block--translucent': normalizedConfig.translucent && normalizedConfig.translucentScope === 'block',
        'map-block--translucent-bg': normalizedConfig.translucent && normalizedConfig.translucentScope === 'background',
        'map-block--opaque-on-hover': normalizedConfig.translucent && normalizedConfig.opaqueOnHover,
      }"
      :style="getCardStyle()"
      @pointerdown="startDisplayDrag($event, block)"
    >
      <div v-if="normalizedConfig.showBlockName" class="map-block__name">{{ block.name ? translateName(block.name) : $t('lz.label.tmsMapLocation.defaultBlockName', { index: blockIndex + 1 }) }}</div>
      <template v-for="level in block.levels" :key="level.id">
        <div v-if="isEditMode || getVisibleDevices(level.devices).length" class="map-block__level">
          <div v-if="normalizedConfig.showLevelName && block.levels.length > 1" class="map-block__level-name">{{ translateName(level.name) }}</div>
          <!-- Nhãn Km đầu/cuối nằm TRÊN thanh thiết bị (ngay dưới tên tầng) -->
          <div v-if="isRouteActive(level) && getRouteAxis(level)" class="map-block__route-axis" :style="getRouteAxisStyle(level)" aria-hidden="true">
            <span>{{ getRouteAxis(level)?.from }}</span>
            <span>{{ getRouteAxis(level)?.to }}</span>
          </div>
          <div class="map-block__devices" :class="{ 'map-block__devices--route': isRouteActive(level) }" :style="isRouteActive(level) ? getRouteTrackStyle(level) : undefined">
            <div v-if="isEditMode && !getVisibleDevices(level.devices).length" class="map-block__drop-slot">+</div>
            <div
              v-for="device in getVisibleDevices(level.devices)"
              :key="device.id"
              class="map-block__device-item"
              :data-block-device-id="device.deviceId"
              :class="{ 'map-block__device-item--content': isBlockVmsDevice(device.deviceId), 'map-block__device-item--out-of-range': isRouteOutOfRange(level, device) }"
              :style="isRouteActive(level) ? getRouteItemStyle(level, device) : undefined"
            >
              <div
                class="map-block__device"
                :class="[
                  getDeviceStatusClass(device.deviceId),
                  getBarrierSimClass(device.deviceId),
                  {
                    'map-block__device--interactive': !isEditMode,
                    'map-block__device--content': isBlockVmsDevice(device.deviceId),
                    'map-block__device--circle-background': !isEditMode && props.showIconBackground && !isBlockVmsDevice(device.deviceId),
                    'map-block__device--background-hidden': !isEditMode && !props.showIconBackground && !isBlockVmsDevice(device.deviceId),
                  },
                ]"
                :style="getDeviceStyle(device.deviceId)"
                :title="getDeviceName(device.deviceId)"
                :tabindex="isEditMode ? -1 : 0"
                role="button"
                :aria-label="$t('lz.label.tmsMapLocation.openEquipmentInfo', { name: getDeviceName(device.deviceId) })"
                @pointerdown.stop
                @click.stop="emitDeviceAction('click', device.deviceId)"
                @mouseenter="emitDeviceAction('mouseover', device.deviceId)"
                @mouseleave="emitDeviceAction('mouseout', device.deviceId)"
                @dblclick.stop="emitDeviceAction('dblclick', device.deviceId)"
                @contextmenu.prevent.stop="emitDeviceAction('rightclick', device.deviceId)"
                @keydown="handleDeviceKeydown($event, device.deviceId)"
              >
                <template v-if="isBlockVmsDevice(device.deviceId)">
                  <div v-if="getBlockVmsHtml(device.deviceId)" v-html="getBlockVmsHtml(device.deviceId)" />
                  <span v-else class="map-block__device-fallback">{{ getDeviceName(device.deviceId).slice(0, 1) }}</span>
                </template>
                <template v-else>
                  <img v-if="getDeviceIcon(device.deviceId)" :key="getDeviceIcon(device.deviceId)" :src="getDeviceIcon(device.deviceId)" alt=""
                    :class="{ 'is-flipped': device.flipX }" @error="hideBrokenImage" />
                  <span v-else class="map-block__device-fallback">{{ getDeviceName(device.deviceId).slice(0, 1) }}</span>
                </template>
              </div>
              <span v-if="getBarrierArmBadge(device.deviceId)" class="map-block__barrier-badge"
                :class="`map-block__barrier-badge--${getBarrierArmBadge(device.deviceId)}`" aria-hidden="true">
                <el-icon><ele-Top v-if="getBarrierArmBadge(device.deviceId) === 'open'" /><ele-Minus v-else /></el-icon>
              </span>
              <!-- Đèn tín hiệu giao thông SS: chấm đèn đỏ/xanh ở góc icon (chỉ bản đồ vận hành, sau khi đã điều khiển) -->
              <span v-if="getTrafficLightColor(device.deviceId)" class="map-block__barrier-badge map-block__light-badge"
                :class="`map-block__light-badge--${getTrafficLightColor(device.deviceId)}`" aria-hidden="true" />
              <!-- Quạt: mũi tên chỉ hướng chạy (không xoay icon vì icon có khung viền) -->
              <span v-if="getFanDirectionBadge(device.deviceId)" class="map-block__barrier-badge"
                :class="`map-block__fan-badge--${getFanDirectionBadge(device.deviceId)}`" aria-hidden="true">
                <el-icon>
                  <ele-Right v-if="getFanDirectionBadge(device.deviceId) === 'forward'" />
                  <ele-Back v-else-if="getFanDirectionBadge(device.deviceId) === 'reverse'" />
                  <ele-Minus v-else />
                </el-icon>
              </span>
              <div
                v-if="isBlockVmsDevice(device.deviceId) && getBlockVmsContentImageUrl(device.deviceId)"
                :class="['map-block__vms-content-tooltip', { 'map-block__vms-content-tooltip--lcs': isLcsBlockVmsDevice(device.deviceId) }]"
                role="tooltip"
              >
                <img :src="getBlockVmsContentImageUrl(device.deviceId)" alt="" @error="hideVmsContentTooltip" />
              </div>
              <div v-if="!isEditMode && mapStore.showIconName" class="map-block__device-name" :title="getDeviceName(device.deviceId)">
                {{ getDeviceName(device.deviceId) }}
              </div>
            </div>
          </div>
        </div>
      </template>
      <div v-if="isEditMode && !block.levels.length" class="map-block__empty">{{ $t('lz.label.tmsMapLocation.emptyBlock') }}</div>
    </article>
  </div>
</template>

<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, onUpdated, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import type { Map as LeafletMap } from 'leaflet';
import { useBaseConfig } from '/@/stores/baseConfig';
import { useMapStore } from '/@/stores/its/mapStore';
import { BaseConfigTypeEnum, TmsEquipmentType, TmsTrafficInfoMap } from '/@/types/enums';
import { buildEquipmentMarkerIconByType, isImageEquipmentType, resolveDeviceIconUrl, resolveEquipmentIconSize, resolveVmsContentImageUrl } from '../helpers/markerIcons';
import { cloneBlockConfiguration, createEmptyBlockConfiguration, normalizeBlockConfiguration } from '../blockTypes';
import { getBarrierDeviceKind, useBarrierControlStore } from '/@/stores/its/barrierControl';
import { isTrafficLightDevice, useTrafficLightControlStore } from '/@/stores/its/trafficLightControl';
import { useFanControlStore } from '/@/stores/its/fanControl';
import {
  BLOCK_ANCHOR_DEFAULT_RADIUS,
  BLOCK_ANCHOR_FALLBACK_COLOR,
  BLOCK_LINE_DASH_ARRAY,
  BLOCK_LINE_DEFAULT_WIDTH,
  BLOCK_LINE_FALLBACK_COLOR,
} from '../blockTypes';
import type { BlockConfiguration, BlockDevice, BlockLevel, BlockPosition, MapBlock } from '../blockTypes';
import { computeRouteLayout, formatRouteKm, getEquipmentRoutePosition } from '../helpers/routeLayout';
import type { RouteLayoutResult, RoutePlacedItem } from '../helpers/routeLayout';

type BlockDeviceAction = {
  action: 'click' | 'mouseover' | 'mouseout' | 'dblclick' | 'rightclick';
  itemId: string | number;
  itemGroup: 'device';
  itemData: Record<string, any>;
  geometry?: any;
};

const props = withDefaults(
  defineProps<{
    map: LeafletMap | null;
    config?: BlockConfiguration;
    geoJsonData?: { features?: any[] };
    allGeoJsonData?: { features?: any[] };
    filters?: Record<string, any>;
    mapMode?: TmsTrafficInfoMap;
    isEditMode?: boolean;
    showIconBackground?: boolean;
    iconSize?: number;
  }>(),
  {
    config: () => createEmptyBlockConfiguration(),
    geoJsonData: () => ({ features: [] }),
    allGeoJsonData: () => ({ features: [] }),
    filters: () => ({}),
    mapMode: TmsTrafficInfoMap.Block,
    isEditMode: false,
    showIconBackground: true,
    iconSize: 0,
  }
);
const emit = defineEmits<{
  (event: 'update:config', value: BlockConfiguration): void;
  (event: 'item-action', value: BlockDeviceAction): void;
}>();
const { t: $t, te } = useI18n();
const mapStore = useMapStore();

/**
 * Hiển thị nhãn block/level: nếu chuỗi nhập vào là một i18n key hợp lệ thì dịch,
 * ngược lại giữ nguyên văn bản người dùng nhập.
 * Dùng te() để chuỗi tự do có dấu chấm không bị hiểu nhầm thành key và trả về rỗng.
 */
const translateName = (value?: string | null) => (value && te(value) ? $t(value) : value ?? '');
const baseConfig = useBaseConfig();
const overlayRef = ref<HTMLElement | null>(null);
const connectorsRef = ref<SVGSVGElement | null>(null);
const normalizedConfig = computed(() => normalizeBlockConfiguration(props.config));
const isBlockVmsMode = computed(() => props.mapMode === TmsTrafficInfoMap.BlockVms);

type ScreenPoint = { x: number; y: number };
/** Toạ độ màn hình của từng block: [neo thực tế, ...điểm bẻ góc, card hiển thị]. */
type BlockLayout = Map<string, ScreenPoint[]>;
type ZoomTransform = { scale: number; translateX: number; translateY: number };
type BlockElements = {
  card?: HTMLElement;
  line?: SVGPolylineElement;
  anchor?: SVGCircleElement;
  waypoints: SVGCircleElement[];
  mids: SVGCircleElement[];
};

/** Trùng với transition zoom của Leaflet (.leaflet-zoom-anim) để card/đường nối chạy khớp với tile. */
const ZOOM_ANIMATION_MS = 250;
const MOVING_CLASS = 'block-map-layer--moving';

/** cubic-bezier(0, 0, 0.25, 1) — cùng easing với Leaflet. */
const zoomEasing = (() => {
  const [x1, y1, x2, y2] = [0, 0, 0.25, 1];
  const cx = 3 * x1;
  const bx = 3 * (x2 - x1) - cx;
  const ax = 1 - cx - bx;
  const cy = 3 * y1;
  const by = 3 * (y2 - y1) - cy;
  const ay = 1 - cy - by;
  const sampleX = (u: number) => ((ax * u + bx) * u + cx) * u;
  const sampleY = (u: number) => ((ay * u + by) * u + cy) * u;
  const slopeX = (u: number) => (3 * ax * u + 2 * bx) * u + cx;
  return (t: number) => {
    if (t <= 0) return 0;
    if (t >= 1) return 1;
    let u = t;
    for (let i = 0; i < 8; i++) {
      const error = sampleX(u) - t;
      const slope = slopeX(u);
      if (Math.abs(error) < 1e-5 || Math.abs(slope) < 1e-6) break;
      u -= error / slope;
    }
    return sampleY(Math.min(1, Math.max(0, u)));
  };
})();

/** Chỉ dùng để Vue render lại khi số điểm bẻ góc thay đổi (block là object thường, không reactive). */
const structureTick = ref(0);
let elementCache = new Map<string, BlockElements>();
let currentLayout: BlockLayout = new Map();
let zoomTween: { from: BlockLayout; to: BlockLayout; startedAt: number; frame: number } | null = null;

/** Chuỗi toạ độ của đường nối: neo thực tế → các điểm bẻ góc → card hiển thị. */
const getChainPositions = (block: MapBlock): BlockPosition[] => [block.actualPosition, ...(block.waypoints || []), block.displayPosition];

/** Tính toạ độ màn hình của mọi block MỘT lần; `transform` = đích của animation zoom (nếu có). */
const computeLayout = (transform?: ZoomTransform): BlockLayout => {
  const layout: BlockLayout = new Map();
  const map = props.map;
  if (!map) return layout;

  visibleBlocks.value.forEach((block) => {
    layout.set(
      block.id,
      getChainPositions(block).map((position) => {
        const point = map.latLngToContainerPoint([position.lat, position.lng]);
        return transform
          ? { x: point.x * transform.scale + transform.translateX, y: point.y * transform.scale + transform.translateY }
          : { x: point.x, y: point.y };
      })
    );
  });
  return layout;
};

/** Gom phần tử DOM của từng block sau mỗi lần render, để mỗi frame chỉ việc ghi toạ độ. */
const collectElements = () => {
  const cache = new Map<string, BlockElements>();
  const overlay = overlayRef.value;
  elementCache = cache;
  if (!overlay) return;

  const ensure = (id: string) => {
    if (!cache.has(id)) cache.set(id, { waypoints: [], mids: [] });
    return cache.get(id)!;
  };

  overlay.querySelectorAll<HTMLElement>(':scope > .map-block').forEach((card) => {
    if (card.dataset.blockId) ensure(card.dataset.blockId).card = card;
  });
  overlay.querySelectorAll<SVGGElement>('g[data-block-line-group]').forEach((group) => {
    const id = group.getAttribute('data-block-line-group');
    if (!id) return;
    const entry = ensure(id);
    entry.line = group.querySelector<SVGPolylineElement>('[data-role="line"]') || undefined;
    entry.anchor = group.querySelector<SVGCircleElement>('[data-role="anchor"]') || undefined;
    entry.waypoints = Array.from(group.querySelectorAll<SVGCircleElement>('[data-role="waypoint"]'));
    entry.mids = Array.from(group.querySelectorAll<SVGCircleElement>('[data-role="mid"]'));
  });
};

const setCircle = (circle: SVGCircleElement | undefined, point: ScreenPoint | undefined) => {
  if (!circle || !point) return;
  circle.setAttribute('cx', String(point.x));
  circle.setAttribute('cy', String(point.y));
};

/** Ghi toạ độ trực tiếp vào DOM: card dùng transform (GPU), SVG giữ nguyên độ dày nét/bán kính khi zoom. */
const applyLayout = (layout: BlockLayout) => {
  currentLayout = layout;
  elementCache.forEach((elements, blockId) => {
    const points = layout.get(blockId);
    if (!points?.length) {
      if (elements.card) elements.card.style.visibility = 'hidden';
      return;
    }

    const display = points[points.length - 1];
    if (elements.card) {
      elements.card.style.transform = `translate3d(${Math.round(display.x)}px, ${Math.round(display.y)}px, 0) translate(-50%, -100%)`;
      elements.card.style.visibility = 'visible';
    }
    elements.line?.setAttribute('points', points.map((point) => `${point.x},${point.y}`).join(' '));
    setCircle(elements.anchor, points[0]);
    elements.waypoints.forEach((circle, index) => setCircle(circle, points[index + 1]));
    elements.mids.forEach((circle, index) => {
      const start = points[index];
      const end = points[index + 1];
      if (start && end) setCircle(circle, { x: (start.x + end.x) / 2, y: (start.y + end.y) / 2 });
    });
  });
};

const cancelZoomTween = () => {
  if (zoomTween) cancelAnimationFrame(zoomTween.frame);
  zoomTween = null;
};

/** Ghi ngay vị trí theo view hiện tại — chạy đồng bộ trong event của Leaflet nên không trễ 1 frame so với bản đồ. */
const syncLayout = () => {
  cancelZoomTween();
  applyLayout(computeLayout());
};

const isAnimatingZoom = () => !!(props.map as (LeafletMap & { _animatingZoom?: boolean }) | null)?._animatingZoom;

const refreshPositions = () => {
  // Trong lúc Leaflet chạy animation zoom thì để tween điều khiển, tránh giật về vị trí cũ.
  if (isAnimatingZoom()) return;
  syncLayout();
};

const stepZoomTween = (now: number) => {
  if (!zoomTween) return;
  const { from, to, startedAt } = zoomTween;
  const progress = zoomEasing(Math.min(1, (now - startedAt) / ZOOM_ANIMATION_MS));
  const layout: BlockLayout = new Map();
  to.forEach((targetPoints, blockId) => {
    const startPoints = from.get(blockId);
    layout.set(
      blockId,
      targetPoints.map((target, index) => {
        const start = startPoints?.[index] ?? target;
        return { x: start.x + (target.x - start.x) * progress, y: start.y + (target.y - start.y) * progress };
      })
    );
  });
  applyLayout(layout);
  if (progress < 1) zoomTween.frame = requestAnimationFrame(stepZoomTween);
  else zoomTween = null;
};

/**
 * Leaflet zoom bằng CSS transform affine (scale + translate) trên map pane. Nội suy tuyến
 * tính từng điểm với cùng easing cho ra đúng quỹ đạo của tile, nên card, đường nối và
 * điểm neo chạy khớp với bản đồ; kích thước card, độ dày nét và bán kính neo giữ nguyên.
 */
const handleZoomAnim = (event: any) => {
  const map = props.map;
  if (!map || event?.zoom == null || !event.center) return;

  const scale = map.getZoomScale(event.zoom);
  const currentCenter = map.latLngToContainerPoint(event.center);
  const targetCenter = map.getSize().divideBy(2);
  const target = computeLayout({ scale, translateX: targetCenter.x - currentCenter.x * scale, translateY: targetCenter.y - currentCenter.y * scale });

  // Pinch-zoom (noUpdate) phát zoomanim liên tục theo ngón tay: bám theo ngay, không tween.
  if (event.noUpdate) {
    cancelZoomTween();
    applyLayout(target);
    return;
  }

  // Zoom liên tiếp (lăn chuột nhanh): tween mới bắt đầu từ vị trí đang hiển thị.
  const from = currentLayout;
  cancelZoomTween();
  zoomTween = { from, to: target, startedAt: performance.now(), frame: 0 };
  zoomTween.frame = requestAnimationFrame(stepZoomTween);
};

/** Tắt blur nền của card trong lúc bản đồ di chuyển — backdrop-filter tính lại mỗi frame rất nặng. */
const handleMoveStart = () => overlayRef.value?.classList.add(MOVING_CLASS);
const handleMoveEnd = () => overlayRef.value?.classList.remove(MOVING_CLASS);

const bindMapEvents = (map: LeafletMap | null, action: 'on' | 'off') => {
  map?.[action]('move zoom viewreset resize rotate', refreshPositions);
  map?.[action]('zoomanim', handleZoomAnim);
  map?.[action]('zoomend', syncLayout);
  map?.[action]('movestart', handleMoveStart);
  map?.[action]('moveend', handleMoveEnd);
};

onMounted(() => {
  bindMapEvents(props.map, 'on');
  nextTick(() => {
    collectElements();
    refreshPositions();
  });
});

/** Sau mỗi lần render (đổi cấu hình, filter, trạng thái thiết bị...) gán lại toạ độ cho phần tử mới. */
onUpdated(() => {
  collectElements();
  if (zoomTween || isAnimatingZoom()) applyLayout(currentLayout);
  else syncLayout();
});

onBeforeUnmount(() => {
  cancelZoomTween();
  bindMapEvents(props.map, 'off');
});

watch(
  () => props.map,
  (map, previousMap) => {
    bindMapEvents(previousMap, 'off');
    bindMapEvents(map, 'on');
    refreshPositions();
  }
);

const getDevice = (deviceId: string) => mapStore.equipmentList.find((device: any) => String(device.id) === String(deviceId));
const getDeviceType = (deviceId: string) => {
  const device = getDevice(deviceId);
  return mapStore.equipmentTypeList.find((item: any) => String(item.id) === String(device?.equipmentTypeId));
};
const getDeviceName = (deviceId: string) => getDevice(deviceId)?.name || $t('lz.label.base.unknown');

/** Danh sách thiết bị của 1 tầng còn hiển thị sau khi áp filter. */
const getVisibleDevices = (devices: any[]) => devices.filter((device) => isDeviceVisible(device.deviceId));

/** Feature Point của thiết bị trong 1 FeatureCollection (nếu thiết bị đã được đặt trên bản đồ). */
const findDeviceFeature = (features: any[] | undefined, deviceId: string) =>
  features?.find((feature: any) => feature.geometry?.type === 'Point' && feature.properties?.itemGroup === 'device' && String(feature.properties?.id) === String(deviceId));

/**
 * Thiết bị trong block có được hiển thị theo filter hiện tại hay không.
 *
 * Áp dụng cho CẢ chế độ cấu hình và chế độ theo dõi: bấm ẩn/hiện trên panel filter phải
 * tác động tới thiết bị trong card block giống như tác động tới marker trên bản đồ.
 *
 * Nguồn sự thật là ĐÚNG tập feature bản đồ đang vẽ: `geoJsonData` đã được useMapFilters
 * loại bỏ các thiết bị bị tắt, nhờ vậy card block và marker không bao giờ lệch nhau.
 * Thiết bị được gán vào block nhưng CHƯA đặt vị trí trên bản đồ thì không có feature —
 * khi đó chỉ áp filter theo loại thiết bị để không ẩn oan.
 */
const isDeviceVisible = (deviceId: string) => {
  // Chỉ áp filter theo loại khi tra được thiết bị trong store; nếu equipmentList chưa nạp
  // xong thì không ẩn oan (sẽ tự đúng lại khi store có dữ liệu).
  const device = getDevice(deviceId);
  if (device && Array.isArray(props.filters?.device) && !props.filters.device.some((typeId: string | number) => String(typeId) === String(device.equipmentTypeId))) return false;

  const hasLocationOnMap = !!findDeviceFeature(props.allGeoJsonData?.features, deviceId);
  return hasLocationOnMap ? !!findDeviceFeature(props.geoJsonData?.features, deviceId) : true;
};

/**
 * Block không còn thiết bị nào hiển thị thì ẩn cả card, đường nối và chấm neo.
 * Ở chế độ cấu hình vẫn giữ card để còn kéo vị trí và bố trí thiết bị.
 */
const visibleBlocks = computed(() =>
  normalizedConfig.value.blocks.filter((block) => props.isEditMode || block.levels.some((level) => getVisibleDevices(level.devices).length))
);

/** Icon giống hệt marker đặt lẻ (cùng helper) để thêm vào block hiển thị đúng hình. */
const getDeviceIcon = (deviceId: string) => resolveDeviceIconUrl(deviceId, !!props.isEditMode);

/** Kích thước chung của map được ưu tiên; 0 sẽ dùng cấu hình size của từng loại thiết bị. */
const getDeviceStyle = (deviceId: string) => {
  if (isBlockVmsDevice(deviceId)) return undefined;
  const iconSize = resolveEquipmentIconSize(getDeviceType(deviceId), props.iconSize);
  return { '--map-block-icon-size': `${iconSize}px` };
};

// #region Tầng bố trí theo Km (level.layout = 'route')

/** Độ rộng/cao ước lượng của 1 khung VMS/LCS (nội dung HTML có kích thước tự do) trong thanh Km. */
const ROUTE_VMS_ITEM_WIDTH = 96;
const ROUTE_VMS_ITEM_HEIGHT = 52;
/** Chiều cao nhãn tên thiết bị dưới icon (chỉ khi đang hiện tên) + khoảng cách giữa các hàng. */
const ROUTE_NAME_LABEL_HEIGHT = 21;
const ROUTE_ROW_GAP = 8;

type RouteLevelLayout = { layout: RouteLayoutResult; itemsByDevice: Map<string, RoutePlacedItem> };

const isRouteLevel = (level: BlockLevel) => level.layout === 'route';
const hasRouteLevel = (block: MapBlock) => block.levels.some(isRouteLevel);

/**
 * Bố trí Km của mọi tầng 'route' đang hiển thị (key = level.id). Chiều dài tuyến/hầm tự đo từ Km
 * nhỏ nhất → lớn nhất của các thiết bị còn hiển thị trong tầng (theo filter hiện tại), hoặc theo khoảng cố định của tầng.
 *
 * Các tầng 'route' CÙNG 1 BLOCK dùng CHUNG 1 độ rộng (= độ rộng cần thiết của tầng rộng nhất) và chung lề 2 đầu,
 * để thanh các tầng bằng nhau và cùng 1 Km nằm thẳng hàng giữa các tầng (vd tầng camera cố định + tầng camera xoay).
 */
const routeLayouts = computed(() => {
  const result = new Map<string, RouteLevelLayout>();
  const showName = !props.isEditMode && mapStore.showIconName;
  visibleBlocks.value.forEach((block) => {
    const entries = block.levels.filter(isRouteLevel).map((level) => {
      const devices = getVisibleDevices(level.devices);
      let itemWidth = 40;
      let deviceHeight = 0;
      devices.forEach((device: BlockDevice) => {
        if (isBlockVmsDevice(device.deviceId)) {
          itemWidth = Math.max(itemWidth, ROUTE_VMS_ITEM_WIDTH);
          deviceHeight = Math.max(deviceHeight, ROUTE_VMS_ITEM_HEIGHT);
        } else {
          const size = resolveEquipmentIconSize(getDeviceType(device.deviceId), props.iconSize) + 8;
          itemWidth = Math.max(itemWidth, size);
          deviceHeight = Math.max(deviceHeight, size);
        }
      });
      return {
        level,
        hasDevices: devices.length > 0,
        itemWidth,
        rowHeight: deviceHeight + (showName ? ROUTE_NAME_LABEL_HEIGHT : 0) + ROUTE_ROW_GAP,
        groupRows: level.splitRowsByType === true,
        // Tách dòng theo loại: sắp theo tên loại để thứ tự dòng ổn định; group = id loại thiết bị
        items: (level.splitRowsByType === true
          ? [...devices].sort((a: BlockDevice, b: BlockDevice) => String(getDeviceType(a.deviceId)?.name ?? '').localeCompare(String(getDeviceType(b.deviceId)?.name ?? '')))
          : devices
        ).map((device: BlockDevice) => ({
          key: device.id,
          position: getEquipmentRoutePosition(getDevice(device.deviceId)),
          group: String(getDevice(device.deviceId)?.equipmentTypeId ?? ''),
        })),
      };
    });
    if (!entries.length) return;

    // Lề chung theo thiết bị rộng nhất của block
    const padding = Math.max(...entries.map((entry) => entry.itemWidth)) / 2 + 4;
    const optionsOf = (entry: (typeof entries)[number], extra: { minWidth?: number; maxWidth?: number } = {}) => ({
      itemWidth: entry.itemWidth,
      rowHeight: entry.rowHeight,
      reverse: entry.level.routeReverse === true,
      // Khoảng cố định (zone / cơ sở / nhập tay) ưu tiên hơn tự đo từ thiết bị
      fixedStart: entry.level.rangeFrom,
      fixedEnd: entry.level.rangeTo,
      padding,
      groupRows: entry.groupRows,
      ...extra,
    });

    // Lượt 1: độ rộng cần thiết của từng tầng → lấy độ rộng lớn nhất làm độ rộng chung của block
    const blockWidth = Math.max(0, ...entries.filter((entry) => entry.hasDevices).map((entry) => computeRouteLayout(entry.items, optionsOf(entry)).width));

    // Lượt 2: mọi tầng bố trí lại trên đúng độ rộng chung đó
    entries.forEach((entry) => {
      const layout = computeRouteLayout(entry.items, optionsOf(entry, blockWidth > 0 ? { minWidth: blockWidth, maxWidth: blockWidth } : {}));
      result.set(entry.level.id, { layout, itemsByDevice: new Map(layout.items.map((item) => [item.key, item])) });
    });
  });
  return result;
});

/** Tầng 'route' có ít nhất 1 thiết bị hiển thị → dùng bố trí tuyệt đối; rỗng thì giữ khung thả "+" như cũ. */
const isRouteActive = (level: BlockLevel) => (routeLayouts.value.get(level.id)?.layout.rows ?? 0) > 0;

const getRouteTrackStyle = (level: BlockLevel) => {
  const entry = routeLayouts.value.get(level.id);
  return entry ? { width: `${entry.layout.width}px`, height: `${entry.layout.height}px` } : undefined;
};

const getRouteAxisStyle = (level: BlockLevel) => ({ width: getRouteTrackStyle(level)?.width, marginLeft: 'auto', marginRight: 'auto' });

/** Thiết bị có Km nằm ngoài khoảng của tầng (đã kẹp vào mép): chỉ viền cảnh báo ở chế độ chỉnh sửa. */
const isRouteOutOfRange = (level: BlockLevel, device: BlockDevice) =>
  props.isEditMode === true && routeLayouts.value.get(level.id)?.itemsByDevice.get(device.id)?.outOfRange === true;

const getRouteItemStyle = (level: BlockLevel, device: BlockDevice) => {
  const entry = routeLayouts.value.get(level.id);
  const item = entry?.itemsByDevice.get(device.id);
  if (!entry || !item) return undefined;
  return { left: `${item.centerPx}px`, top: `${item.row * (entry.layout.height / entry.layout.rows)}px` };
};

/** Nhãn Km 2 đầu thanh (đã tính đảo chiều); không đo được chiều dài thì không hiện. */
const getRouteAxis = (level: BlockLevel) => {
  const layout = routeLayouts.value.get(level.id)?.layout;
  if (!layout || layout.startPosition === null || layout.endPosition === null || layout.lengthMeters <= 0) return null;
  const start = formatRouteKm(layout.startPosition);
  const end = formatRouteKm(layout.endPosition);
  return level.routeReverse ? { from: end, to: start } : { from: start, to: end };
};

// #endregion

const barrierStore = useBarrierControlStore();
const trafficLightStore = useTrafficLightControlStore();

/**
 * Màu đèn tín hiệu giao thông SS đã điều khiển ('red' | 'green'); '' nếu không phải đèn SS, chưa điều khiển
 * hoặc đang ở màn thiết lập (hiệu ứng chỉ hiện trên bản đồ vận hành).
 */
const getTrafficLightColor = (deviceId: string) => {
  if (props.isEditMode || !isTrafficLightDevice(deviceId)) return '';
  return trafficLightStore.getLightState(deviceId) || '';
};

/**
 * Tô icon theo trạng thái hệ Barrier sau khi điều khiển (giả lập / thiết bị trả về):
 * barrier mở hoặc đèn xanh → xanh, barrier đóng hoặc đèn đỏ → đỏ. Chưa điều khiển → giữ màu trạng thái thiết bị.
 */
const getBarrierSimClass = (deviceId: string) => {
  if (props.isEditMode) return '';
  // Đèn tín hiệu giao thông SS: tô viền theo màu đèn đã điều khiển (đỏ/xanh)
  const trafficColor = getTrafficLightColor(deviceId);
  if (trafficColor) return trafficColor === 'green' ? 'map-block__device--sim-green' : 'map-block__device--sim-red';
  const kind = getBarrierDeviceKind(deviceId);
  const value = kind === 'barrier' ? barrierStore.getArmState(deviceId) : kind === 'light' ? barrierStore.getLightState(deviceId) : undefined;
  if (value === 'open' || value === 'green') return 'map-block__device--sim-green';
  if (value === 'closed' || value === 'red') return 'map-block__device--sim-red';
  return '';
};

const fanStore = useFanControlStore();

/**
 * Quạt đã có số liệu (đã hover/điều khiển) → huy hiệu mũi tên hướng chạy ở góc icon:
 * 'forward' (→) | 'reverse' (←) | 'stop' (—). Chỉ đọc state, không sinh dữ liệu khi render.
 */
const getFanDirectionBadge = (deviceId: string) => {
  if (props.isEditMode) return '';
  return fanStore.getState(deviceId)?.mode || '';
};

/** Barrier đã có trạng thái → hiện huy hiệu mở/đóng ở góc icon. */
const getBarrierArmBadge = (deviceId: string) => {
  if (props.isEditMode || getBarrierDeviceKind(deviceId) !== 'barrier') return '';
  return barrierStore.getArmState(deviceId) || '';
};

const getDeviceStatusClass = (deviceId: string) => {
  const state = getDevice(deviceId)?.equipmentState;
  const item = baseConfig.getConfigDataByCode(BaseConfigTypeEnum.TmsEquipmentState).find((value: any) => String(value.code) === String(state));
  return item?.value === 'fault' ? 'map-block__device--fault' : item?.value === 'active' ? 'map-block__device--active' : 'map-block__device--inactive';
};

const getSourceFeature = (deviceId: string) => findDeviceFeature(props.geoJsonData?.features, deviceId);

const getBlockVmsFeature = (deviceId: string) => {
  const device = getDevice(deviceId);
  return getSourceFeature(deviceId) || { properties: { id: device?.id || deviceId } };
};

const isBlockVmsDevice = (deviceId: string) => isBlockVmsMode.value && isImageEquipmentType(getDeviceType(deviceId)?.code);
const isLcsBlockVmsDevice = (deviceId: string) => getDeviceType(deviceId)?.code === TmsEquipmentType.LCS;

/** Tái sử dụng khung VMS/LCS của map mode Vms, nhưng để BlockMapLayer tự hiển thị nhãn bên dưới. */
const getBlockVmsHtml = (deviceId: string) => {
  const device = getDevice(deviceId);
  const type = getDeviceType(deviceId);
  const icon = buildEquipmentMarkerIconByType(getBlockVmsFeature(deviceId), device, undefined, type?.code);
  return typeof icon?.options.html === 'string' ? icon.options.html : '';
};

/** Tooltip ảnh lớn có cùng nguồn dữ liệu realtime với popup hover của map mode Vms. */
const getBlockVmsContentImageUrl = (deviceId: string) => resolveVmsContentImageUrl(getBlockVmsFeature(deviceId), undefined);

/** Phát payload cùng cấu trúc với marker chuẩn để trafficInfo dùng lại popover và click modal. */
const emitDeviceAction = (action: BlockDeviceAction['action'], deviceId: string) => {
  if (props.isEditMode) return;

  const feature = getSourceFeature(deviceId);
  const device = getDevice(deviceId);
  const itemId = feature?.properties?.id ?? device?.id ?? deviceId;
  // Thiết bị cùng tầng (theo thứ tự hiển thị) để modal ghép cặp barrier ↔ đèn tín hiệu
  const barrierPeers = normalizedConfig.value.blocks
    .flatMap((block) => block.levels)
    .find((level) => level.devices.some((item) => String(item.deviceId) === String(deviceId)))
    ?.devices.map((item) => String(item.deviceId));

  emit('item-action', {
    action,
    itemId,
    itemGroup: 'device',
    itemData: {
      ...(device || {}),
      ...(feature?.properties || {}),
      id: itemId,
      itemGroup: 'device',
      barrierPeers,
    },
    geometry: feature?.geometry,
  });
};

const handleDeviceKeydown = (event: KeyboardEvent, deviceId: string) => {
  if (event.key !== 'Enter' && event.key !== ' ') return;
  event.preventDefault();
  event.stopPropagation();
  emitDeviceAction('click', deviceId);
};

/** Số đoạn của đường nối (= số chấm giữa) — chỉ dùng để render, toạ độ ghi trong applyLayout. */
const getSegmentIndexes = (block: MapBlock) => {
  void structureTick.value;
  return Array.from({ length: (block.waypoints?.length || 0) + 1 }, (_, index) => index);
};

const getWaypoints = (block: MapBlock) => {
  void structureTick.value;
  return block.waypoints || [];
};

const getConnectorStyle = (block: MapBlock) => ({
  stroke: block.lineColor || BLOCK_LINE_FALLBACK_COLOR,
  strokeWidth: String(block.lineWidth || BLOCK_LINE_DEFAULT_WIDTH),
  strokeDasharray: BLOCK_LINE_DASH_ARRAY[block.lineStyle || 'solid'],
});

const getAnchorStyle = (block: MapBlock) => ({ fill: block.anchorColor || BLOCK_ANCHOR_FALLBACK_COLOR });

const getAnchorRadius = (block: MapBlock) => block.anchorRadius || BLOCK_ANCHOR_DEFAULT_RADIUS;

/** Điểm bẻ góc dùng cùng màu với đường nối để nhìn liền mạch. */
const getWaypointStyle = (block: MapBlock) => ({ fill: block.lineColor || BLOCK_LINE_FALLBACK_COLOR });

/** Vị trí card do applyLayout ghi bằng transform; ở đây chỉ còn biến CSS cho chế độ mờ. */
const getCardStyle = () => ({
  // Độ đậm của chế độ mờ: cả card (map-block--translucent) hoặc chỉ nền (map-block--translucent-bg).
  '--map-block-opacity': String(normalizedConfig.value.translucentOpacity),
  '--map-block-bg-alpha': `${Math.round(normalizedConfig.value.translucentOpacity * 100)}%`,
  // Độ rộng tối thiểu chung của card (tab Cấu hình hiển thị)
  '--map-block-min-width': `${normalizedConfig.value.cardMinWidth}px`,
});

type DragTarget = { kind: 'actual' } | { kind: 'display' } | { kind: 'waypoint'; index: number };
let dragging: { block: MapBlock; target: DragTarget } | null = null;
let pendingPosition: BlockPosition | null = null;
let dragFrame: number | null = null;
let hasMoved = false;

const applyPendingPosition = () => {
  dragFrame = null;
  if (!dragging || !pendingPosition) return;
  const { block, target } = dragging;
  if (target.kind === 'actual') block.actualPosition = pendingPosition;
  else if (target.kind === 'display') block.displayPosition = pendingPosition;
  else if (block.waypoints?.[target.index]) block.waypoints[target.index] = pendingPosition;
  pendingPosition = null;
  syncLayout();
};

const queuePosition = (event: PointerEvent) => {
  if (!dragging || !props.map || !overlayRef.value) return;
  const bounds = overlayRef.value.getBoundingClientRect();
  const latLng = props.map.containerPointToLatLng([event.clientX - bounds.left, event.clientY - bounds.top]);
  pendingPosition = { lat: latLng.lat, lng: latLng.lng };
  hasMoved = true;
  if (dragFrame == null) dragFrame = requestAnimationFrame(applyPendingPosition);
};

const stopDrag = () => {
  if (!dragging) return;
  if (dragFrame != null) {
    cancelAnimationFrame(dragFrame);
    dragFrame = null;
  }
  applyPendingPosition();
  const shouldPersist = hasMoved;
  dragging = null;
  pendingPosition = null;
  props.map?.dragging.enable();
  document.removeEventListener('pointermove', queuePosition);
  document.removeEventListener('pointerup', stopDrag);
  if (shouldPersist) emit('update:config', cloneBlockConfiguration(normalizedConfig.value));
};

const startDrag = (event: PointerEvent, block: MapBlock, target: DragTarget) => {
  if (!props.isEditMode || event.button !== 0) return;
  event.preventDefault();
  event.stopPropagation();
  dragging = { block, target };
  hasMoved = false;
  props.map?.dragging.disable();
  document.addEventListener('pointermove', queuePosition);
  document.addEventListener('pointerup', stopDrag, { once: true });
};

const startDisplayDrag = (event: PointerEvent, block: MapBlock) => {
  if ((event.target as HTMLElement | null)?.closest('.map-block__device')) return;
  startDrag(event, block, { kind: 'display' });
};
const startAnchorDrag = (event: PointerEvent, block: MapBlock) => startDrag(event, block, { kind: 'actual' });
const startWaypointDrag = (event: PointerEvent, block: MapBlock, waypointIndex: number) => startDrag(event, block, { kind: 'waypoint', index: waypointIndex });

/** Chèn 1 điểm bẻ góc tại chấm giữa đoạn được bấm; người dùng kéo tiếp để tạo góc. */
const insertWaypoint = (block: MapBlock, segmentIndex: number) => {
  if (!props.isEditMode || !props.map) return;
  const points = currentLayout.get(block.id);
  const start = points?.[segmentIndex];
  const end = points?.[segmentIndex + 1];
  if (!start || !end) return;
  const latLng = props.map.containerPointToLatLng([(start.x + end.x) / 2, (start.y + end.y) / 2]);
  const waypoints = [...(block.waypoints || [])];
  waypoints.splice(segmentIndex, 0, { lat: latLng.lat, lng: latLng.lng });
  block.waypoints = waypoints;
  structureTick.value += 1;
  emit('update:config', cloneBlockConfiguration(normalizedConfig.value));
};

/** Xoá 1 điểm bẻ góc (chuột phải vào điểm đó). */
const removeWaypoint = (block: MapBlock, waypointIndex: number) => {
  if (!props.isEditMode || !block.waypoints?.length) return;
  block.waypoints = block.waypoints.filter((_, index) => index !== waypointIndex);
  structureTick.value += 1;
  emit('update:config', cloneBlockConfiguration(normalizedConfig.value));
};

const hideBrokenImage = (event: Event) => {
  (event.target as HTMLImageElement).style.display = 'none';
};

const hideVmsContentTooltip = (event: Event) => {
  (event.target as HTMLImageElement).closest('.map-block__vms-content-tooltip')?.setAttribute('style', 'display: none');
};

onBeforeUnmount(stopDrag);
</script>

<style scoped>
.block-map-layer { position: absolute; inset: 0; z-index: 650; pointer-events: none; overflow: hidden; }
.block-map-layer__connectors { position: absolute; inset: 0; width: 100%; height: 100%; overflow: visible; }
.block-map-layer__connector { fill: none; stroke: var(--el-text-color-secondary); stroke-width: 2; stroke-linecap: round; stroke-linejoin: round; opacity: 0.85; pointer-events: none; }
.block-map-layer__anchor { fill: var(--el-text-color-primary); stroke: var(--el-bg-color); stroke-width: 2; pointer-events: all; }
.block-map-layer__anchor:hover { fill: var(--el-color-primary); cursor: grab; }
.block-map-layer__waypoint { stroke: var(--el-bg-color); stroke-width: 2; pointer-events: all; cursor: grab; }
.block-map-layer__waypoint:hover { stroke: var(--el-color-primary); }
.block-map-layer__mid-handle { fill: var(--el-bg-color); stroke: var(--el-color-primary); stroke-width: 2; opacity: 0.55; pointer-events: all; cursor: copy; }
.block-map-layer__mid-handle:hover { opacity: 1; }
.map-block { position: absolute; left: 0; top: 0; visibility: hidden; transform: translate(-50%, -100%); will-change: transform; min-width: var(--map-block-min-width, 160px); max-width: 360px; padding: 8px; border: 1px solid var(--el-border-color-light); border-radius: 12px; background: color-mix(in srgb, var(--el-bg-color) 92%, transparent); box-shadow: var(--el-box-shadow-light); backdrop-filter: blur(5px); pointer-events: auto; }
.map-block--editable { cursor: grab; }
/* Đang pan/zoom: bỏ blur nền (tính lại mỗi frame rất nặng) để chuyển động mượt */
.block-map-layer--moving .map-block { backdrop-filter: none !important; }
/* Nhấp nháy khi bấm "Định vị" ở tab Vị trí (LocationEditor thêm/bỏ class is-located) */
.map-block.is-located, .map-block__device-item.is-located { animation: map-block-located 0.5s ease-in-out 3; }
@keyframes map-block-located {
  0%, 100% { box-shadow: var(--el-box-shadow-light); }
  50% { box-shadow: 0 0 0 4px var(--el-color-primary-light-5), var(--el-box-shadow); }
}
.map-block { transition: opacity 0.2s ease-out, background-color 0.2s ease-out, border-color 0.2s ease-out, box-shadow 0.2s ease-out; }
/* Chế độ mờ "cả block": cả card (nền + icon + chữ) trong suốt theo độ đậm cấu hình; bỏ blur để thấy rõ bản đồ */
.map-block--translucent { opacity: var(--map-block-opacity, 0.5); backdrop-filter: none; }
/* Chế độ mờ "chỉ nền": nền, viền, bóng mờ theo độ đậm; icon thiết bị và chữ vẫn rõ */
.map-block--translucent-bg {
  background: color-mix(in srgb, var(--el-bg-color) var(--map-block-bg-alpha, 50%), transparent);
  border-color: color-mix(in srgb, var(--el-border-color-light) var(--map-block-bg-alpha, 50%), transparent);
  box-shadow: none;
  backdrop-filter: none;
}
/* Tuỳ chọn: rê chuột / focus bàn phím vào card thì hiện rõ lại như bình thường */
.map-block--opaque-on-hover:hover, .map-block--opaque-on-hover:focus-within {
  opacity: 1;
  background: color-mix(in srgb, var(--el-bg-color) 92%, transparent);
  border-color: var(--el-border-color-light);
  box-shadow: var(--el-box-shadow-light);
  backdrop-filter: blur(5px);
}
.map-block--editable:active, .block-map-layer__anchor:active, .block-map-layer__waypoint:active { cursor: grabbing; }
.map-block__name { margin: -1px -1px 7px; padding-bottom: 5px; border-bottom: 1px solid var(--el-border-color-lighter); color: var(--el-text-color-primary); font-size: 12px; font-weight: 700; line-height: 1.25; text-align: center; word-break: break-word; }
.map-block__level + .map-block__level { border-top: 1px solid var(--el-border-color-lighter); margin-top: 6px; padding-top: 6px; }
.map-block__level-name { color: var(--el-text-color-secondary); font-size: 10px; font-weight: 700; letter-spacing: 0.04em; text-transform: uppercase; margin-bottom: 4px; }
.map-block__devices { display: flex; flex-wrap: wrap; justify-content: center; gap: 8px; }
.map-block__device-item { display: flex; flex-direction: column; align-items: center; min-width: 40px; max-width: 120px; }
/* Tầng bố trí theo Km: card rộng theo chiều dài tuyến/hầm (bỏ giới hạn 360px), thiết bị đặt tuyệt đối theo left/top tính trong script */
.map-block--route { max-width: none; width: max-content; }
.map-block__devices--route { position: relative; display: block; margin: 0 auto; }
.map-block__devices--route > .map-block__device-item { position: absolute; transform: translateX(-50%); }
.map-block__route-axis { display: flex; justify-content: space-between; margin-bottom: 3px; color: var(--el-text-color-secondary); font-size: 10px; font-weight: 600; line-height: 1.2; }
.map-block__device-item--content { position: relative; }
.map-block__device-item--out-of-range { outline: 2px dashed var(--el-color-warning); outline-offset: 2px; border-radius: 8px; }
.map-block__device { display: flex; align-items: center; justify-content: center; width: calc(var(--map-block-icon-size, 32px) + 8px); height: calc(var(--map-block-icon-size, 32px) + 8px); border: 2px solid var(--el-color-info); border-radius: 8px; background: var(--el-bg-color); box-shadow: var(--el-box-shadow-lighter); overflow: hidden; }
.map-block__device--interactive { cursor: pointer; transition: transform 0.2s ease-out, box-shadow 0.2s ease-out, background-color 0.2s ease-out; }
.map-block__device--interactive:hover { transform: scale(1.15); background: var(--el-fill-color-light); box-shadow: var(--el-box-shadow); }
.map-block__device--interactive:focus-visible { outline: 2px solid var(--el-color-primary); outline-offset: 2px; }
.map-block__device--active { border-color: var(--el-color-success); }
.map-block__device--fault { border-color: var(--el-color-danger); }
/* Trạng thái hệ Barrier sau điều khiển: viền + quầng sáng xanh/đỏ (ưu tiên hơn màu trạng thái thiết bị) */
.map-block__device.map-block__device--sim-green { border-color: var(--el-color-success); box-shadow: 0 0 0 2px var(--el-color-success-light-5), 0 0 10px var(--el-color-success); }
.map-block__device.map-block__device--sim-red { border-color: var(--el-color-danger); box-shadow: 0 0 0 2px var(--el-color-danger-light-5), 0 0 10px var(--el-color-danger); }
.map-block__device-item { position: relative; }
.map-block__barrier-badge { position: absolute; top: -5px; right: -5px; display: inline-flex; align-items: center; justify-content: center; width: 16px; height: 16px; border: 2px solid var(--el-bg-color); border-radius: 50%; color: #fff; font-size: 10px; pointer-events: none; }
.map-block__barrier-badge--open { background: var(--el-color-success); }
.map-block__barrier-badge--closed { background: var(--el-color-danger); }
/* Chấm đèn tín hiệu SS: tròn có quầng sáng cùng màu đèn */
.map-block__light-badge--red { background: var(--el-color-danger); box-shadow: 0 0 6px 1px var(--el-color-danger); }
.map-block__light-badge--green { background: var(--el-color-success); box-shadow: 0 0 6px 1px var(--el-color-success); }
/* Hướng chạy quạt: thuận = xanh, nghịch = cam, dừng = xám */
.map-block__fan-badge--forward { background: var(--el-color-success); }
.map-block__fan-badge--reverse { background: var(--el-color-warning); }
.map-block__fan-badge--stop { background: var(--el-color-info); }
.map-block__device--inactive { border-color: var(--el-color-info); }
.map-block__device--content { width: auto; height: auto; min-height: 44px; border: 0; border-radius: 0; background: transparent; box-shadow: none; overflow: visible; }
.map-block__device--circle-background { border-radius: 50% !important; }
.map-block__device--background-hidden { border-color: transparent !important; background: transparent !important; box-shadow: none !important; }
.map-block__device--content.map-block__device--interactive:hover { transform: scale(1.05); background: transparent; box-shadow: none; }
.map-block__device--background-hidden.map-block__device--interactive:hover { background: transparent !important; box-shadow: none !important; }
.map-block__device img { width: var(--map-block-icon-size, 32px); height: var(--map-block-icon-size, 32px); object-fit: contain; }
/* Lật ngang riêng ảnh icon — viền trạng thái/nền tròn/hover scale ở khung ngoài không đổi */
.map-block__device img.is-flipped { transform: scaleX(-1); }

.map-block__device-fallback { color: var(--el-text-color-secondary); font-size: 16px; font-weight: 700; }
.map-block__vms-content-tooltip { display: none; position: absolute; bottom: calc(100% + 8px); left: 50%; z-index: 2; padding: 4px; border-radius: 6px; background: var(--el-text-color-primary); box-shadow: var(--el-box-shadow); line-height: 0; pointer-events: none; transform: translateX(-50%); }
.map-block__device-item--content:hover .map-block__vms-content-tooltip { display: block; }
.map-block__vms-content-tooltip img { display: block; width: min(260px, 40vw); height: auto; border-radius: 4px; }
.map-block__vms-content-tooltip--lcs img { width: min(120px, 20vw); }
.map-block__device-name { width: 100%; margin-top: 3px; overflow: hidden; color: var(--el-text-color-regular); font-size: 10px; font-weight: 600; line-height: 1.2; text-align: center; text-overflow: ellipsis; white-space: nowrap; }
.map-block__drop-slot { display: flex; align-items: center; justify-content: center; width: 40px; height: 40px; border: 2px dashed var(--el-border-color); border-radius: 8px; color: var(--el-text-color-secondary); font-size: 18px; }
.map-block__empty { color: var(--el-text-color-secondary); font-size: 11px; text-align: center; white-space: nowrap; }
</style>
