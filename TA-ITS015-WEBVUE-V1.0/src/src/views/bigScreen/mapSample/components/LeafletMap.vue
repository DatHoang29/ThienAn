<template>
 <div
    :class="['map-container', { 'is-image-map': state.currentMapType === 'image', 'is-icon-background-hidden': !state.showIconBackground }]"
    :style="{
      ...(state.currentMapType === 'image' ? { '--image-map-bg': state.mapBackgroundColor } : {}),
      '--zone-border-width': `${resolveZoneBorderWidth(state.zoneBorderWidth)}px`,
    }"
  >
 <VdsEventDetail ref="vdsEventDetail" />
<!-- 
    <div class="finish-button-container" v-if="state.currentMode">
          <el-button type="primary" @click="finishEditing">
            <el-icon><Check /></el-icon>
            {{ $t('lz.label.base.finish') }}  {{ state.currentMode === 'edit' ? $t('lz.label.base.editing') : $t('lz.label.base.moving') }}
          </el-button>
    </div> -->

    <!--
      Thanh công cụ cho nhãn văn bản / hình ảnh đang chọn (chỉ ở màn cấu hình).
      Kéo để di chuyển, tay cầm góc trên-trái để xoay, góc dưới-phải để phóng to/thu nhỏ.
      Ctrl/Shift + bấm để chọn nhiều, rồi "Liên kết cụm" cho cả cụm zoom đồng bộ.
    -->
    <div class="annotation-toolbar" v-if="isEditMode && annotationSelectionInfo.count">
      <span class="annotation-toolbar__count">
        {{ $t('lz.label.tmsMapLocation.annotationSelected', { count: annotationSelectionInfo.count }) }}
      </span>
      <el-tooltip :content="$t('lz.label.tmsMapLocation.annotationGroupHint')" placement="bottom">
        <el-button size="small" type="primary" plain :disabled="!annotationSelectionInfo.canGroup"
          @click="groupAnnotationSelection">
          <el-icon><ele-Link /></el-icon>
          <span>{{ $t('lz.button.tmsMapLocation.annotationGroup') }}</span>
        </el-button>
      </el-tooltip>
      <el-button size="small" plain :disabled="!annotationSelectionInfo.canUngroup"
        @click="ungroupAnnotationSelection">
        <el-icon><ele-Remove /></el-icon>
        <span>{{ $t('lz.button.tmsMapLocation.annotationUngroup') }}</span>
      </el-button>
      <el-tooltip :content="$t('lz.label.tmsMapLocation.annotationScaleModeHint')" placement="bottom">
        <el-button size="small" :type="annotationSelectionInfo.isGeoScale ? 'success' : 'info'" plain
          @click="setAnnotationSelectionScaleMode(annotationSelectionInfo.isGeoScale ? 'fixed' : 'geo')">
          <el-icon><ele-ZoomIn /></el-icon>
          <span>{{ annotationSelectionInfo.isGeoScale ? $t('lz.label.tmsMapLocation.annotationScaleGeo') : $t('lz.label.tmsMapLocation.annotationScaleFixed') }}</span>
        </el-button>
      </el-tooltip>
      <!-- Sửa nội dung của PHẦN TỬ ĐANG NHẮM (phần tử vừa bấm), vẫn dùng được khi chọn cả cụm -->
      <el-tooltip :content="$t('lz.label.tmsMapLocation.annotationEditFocusHint')" placement="bottom">
        <el-button size="small" plain v-if="annotationSelectionInfo.canEdit" @click="openSelectedAnnotationEditor">
          <el-icon><ele-Edit /></el-icon>
          <span>{{ $t('lz.button.base.edit') }}{{ annotationSelectionInfo.focusLabel ? `: ${annotationSelectionInfo.focusLabel}` : '' }}</span>
        </el-button>
      </el-tooltip>
      <el-button size="small" type="danger" plain @click="deleteAnnotationSelection">
        <el-icon><ele-Delete /></el-icon>
      </el-button>
      <el-button size="small" text @click="clearAnnotationSelection">
        <el-icon><ele-Close /></el-icon>
      </el-button>
    </div>

    <div id="map" ref="mapContainer"></div>

    <BlockMapLayer
      v-if="state.currentMapMode === TmsTrafficInfoMap.Block || state.currentMapMode === TmsTrafficInfoMap.BlockVms"
      :map="state.map"
      :config="state.blockConfig"
      :geo-json-data="state.geoJsonData"
      :all-geo-json-data="state.allGeoJsonData"
      :filters="filters"
      :map-mode="state.currentMapMode"
      :is-edit-mode="isEditMode"
      :show-icon-background="state.showIconBackground"
      :icon-size="state.iconSize"
      @item-action="emit('item-action', $event)"
      @update:config="handleUpdateBlockConfig"
    />

    <LocationEditor
      v-if="showLocationEditor"
      :geoJsonData="state.geoJsonData"
      :allGeoJsonData="state.allGeoJsonData"
      :previousGeoJsonFeature="state.previousFeatureGeoJsonData"
      :previousFeatureGeoJsonDataZone="state.previousFeatureGeoJsonDataZone"
      :markersLayer="state.markersLayer"
      :polygonsLayer="state.polygonsLayer"
      :highlightLayer="state.highlightLayer"
      :map="state.map"
      ref="locationEditorRef"
      :mapId="selectedMapId"
      :config-version="state.mapConfigVersion"
      :max-zoom="state.mapViewState.maxZoom"
      :min-zoom="state.mapViewState.minZoom"
      :latitude="state.mapViewState.center[0]"
      :longtitude="state.mapViewState.center[1]"
      :default-zoom="state.mapViewState.zoom"
      :rotation="state.mapViewState.rotation"
      :bound-radius="state.mapViewState.boundRadius"
      :current-map-type="state.currentMapType"
      :map-mode="state.currentMapMode"
      :block-config="state.blockConfig"
      :scroll-view-zoom="state.scrollViewZoomSate"
      :map-background-color="state.mapBackgroundColor"
      :zone-label-display="state.zoneLabelDisplay"
      :show-icon-background="state.showIconBackground"
      :lock-zoom="state.lockZoom"
      :lock-rotation="state.lockRotation"
      :icon-size="state.iconSize"
      :incident-icon-size="state.incidentIconSize"
      :incident-zone-gap="state.incidentZoneGap"
      :incident-line-style="state.incidentLineStyle"
      :zone-border-width="state.zoneBorderWidth"
      :flow-line-colors="state.flowLineColors"
      :flow-line-width="state.flowLineWidth"
      @start-editing="handleStartEditing"
      @toggle-zone-label="handleToggleZoneLabel"
      @toggle-icon-background="state.showIconBackground = $event"
      @toggle-lock-zoom="state.lockZoom = $event"
      @toggle-lock-rotation="state.lockRotation = $event"
      @update-icon-flip="handleUpdateIconFlip"
      @update-icon-size="handleUpdateIconSize"
      @update-incident-icon-size="handleUpdateIncidentIconSize"
      @update-incident-zone-gap="handleUpdateIncidentZoneGap"
      @update-incident-line-style="handleUpdateIncidentLineStyle"
      @update-zone-border-width="handleUpdateZoneBorderWidth"
      @update-flow-line-colors="handleUpdateFlowLineColors"
      @update-flow-line-width="handleUpdateFlowLineWidth"
      @start-moving="handleStartMoving"
      @render-map="renderGeoJson"
      @add-item="handleAddItem"     
      @handle-save="handleSave"
      @handle-start-drawing-polygon="handleStartDrawingPolygon"
      @mark-unsaved="handleMarkUnsaved"
      @update-map-background-color="handleUpdateMapBackgroundColor"
      @update-text="handleUpdateText"
      @update-image="handleUpdateImage"
      @update-html="handleUpdateHtml"
      @update-block-config="handleUpdateBlockConfig"
      @import-geojson="handleImportGeoJson"
    />

  </div>
</template>

<script setup lang="ts">
const VdsEventDetail = defineAsyncComponent(() => import('/@/components/vds/VdsEventDetail.vue'));
import { useVdsEventSnapshot } from '/@/transporter/hooks/useVdsEventSnapshot';
import { auth } from '/@/utils/authFunction';
import { liveEventIconFingerprint, toLiveEventIcons } from '../helpers/liveEventPlacement';
// #region IMPORTS

// --- Vue core ---
import { useIncidentSnapshot } from '/@/transporter/hooks/useIncidentSnapshot';
import { logSseDiagnostic } from '/@/utils/fetch-utils';
import { ref, onMounted, onUnmounted, watch, reactive, computed, defineAsyncComponent } from 'vue'
import { useI18n } from 'vue-i18n'

// --- Leaflet + plugins ---
import L from 'leaflet'
import { INCIDENT_LINE_DEFAULT_STYLE, INCIDENT_ZONE_GAP_PX, resolveIncidentLineStyle, resolveIncidentZoneGap } from '../helpers/incidentPlacement';
import type { IncidentLineStyle } from '../helpers/incidentPlacement';
import { FLOW_LINE_WIDTH_DEFAULT, ZONE_BORDER_WIDTH_DEFAULT, normalizeFlowLineColors, resolveFlowLineWidth, resolveZoneBorderWidth } from '../helpers/zoneDisplayStyle';
import type { FlowLineColors } from '../helpers/zoneDisplayStyle';
import 'leaflet/dist/leaflet.css'
// Import 2 thư viện này mới vẽ được polygon trên zone
import '@geoman-io/leaflet-geoman-free';
import '@geoman-io/leaflet-geoman-free/dist/leaflet-geoman.css';
// Import leaflet-rotate để hỗ trợ xoay bản đồ
import 'leaflet-rotate';

// --- UI ---
import { ElMessageBox } from 'element-plus'

// --- API / DTO types ---
import { UpdateTmsMapLocationInput } from '/@/api-services/tms'

// --- Local component / asset ---
import LocationEditor from './LocationEditor.vue'
import BlockMapLayer from './BlockMapLayer.vue'
import { TmsTrafficInfoMap } from '/@/types/enums'
import { createEmptyBlockConfiguration, normalizeBlockConfiguration } from '../blockTypes'
import type { BlockConfiguration } from '../blockTypes'
import type { MapDisplayTransfer } from '../helpers/geoJsonTransfer'
// Lưu ý: Image (toll-road.png) không còn dùng trực tiếp ở đây, đã chuyển vào helpers/markerIcons.ts

// --- Stores ---
import { useMapStore } from '/@/stores/its/mapStore'
// Import broadcast lắng nghe sự kiện realtime qua mittBus (NATS)

import { useTransporterStatusFms } from '/@/transporter/hooks/useTransporterStatusFms'
import { useVdsSse } from '/@/transporter/hooks/useVdsSse'
import { useServerTime } from '/@/stores/serverTimeStore'
import { useMonitorVms } from '/@/stores/its/monitorVms'

// --- Composables nghiệp vụ của LeafletMap (xem thêm mapSample/composables/) ---
import { useZoneStatus } from '../composables/useZoneStatus'
import { useMapRender } from '../composables/useMapRender'
import { useMapFilters } from '../composables/useMapFilters'
import { useMapLoader } from '../composables/useMapLoader'
import { useMapEditing } from '../composables/useMapEditing'
import { installLeafletZoomAnimationGuards } from '/@/utils/mapTile'
// Geometry helpers, marker icon builders và createCustomDivIcon đã chuyển
// vào các composable trên (useMapRender/useMapEditing) — không cần import ở đây nữa.

// --- Utils ---
import { generateRandomNumber } from '/@/utils/randomHelper.js'

// #endregion

// #region TYPES

interface MapViewState {
  center: [number, number];
  zoom: number;
  maxZoom: number,
  minZoom: number,
  rotation?: number,
  boundRadius?: number
}

// #endregion

// #region STORES & COMPOSABLES KHÔNG PHỤ THUỘC STATE

const { t: $t } = useI18n();

// Trạng thái giao thông (màu zone theo condition) — xem composables/useZoneStatus.ts
const { listZoneStatus, loadZoneStatus, applyZoneSnapshot, applyZoneStatusStyle, expireZoneStatuses } = useZoneStatus();

const mapStore = useMapStore()

// Giờ server dùng chung: expireZoneStatuses so hạn stale của zone theo giờ server,
// không dùng giờ máy trạm vì màn hình điều hành có thể lệch giờ.
const serverTime = useServerTime()

// Khai báo lắng nghe sự kiện vms trả về
const vmsMonitor = useMonitorVms();

// #endregion

// #region PROPS / EMITS / REFS

const props = defineProps({
  selectedMapId: {
    type: String,
    default: undefined
  },
  showLocationEditor: {
    type: Boolean,
    default: false
  },
  filters:{
    type:Object
  },
  isEditMode: {
    type: Boolean,
    default: false
  }
})

const emit = defineEmits(['item-action', 'return-geo-json-data', 'refreshMap', 'zone-status-change', 'bearing-change', 'rotation-lock-change'])

const emitZoneStatusChange = () => {
  emit('zone-status-change', listZoneStatus.value.map((item: any) => ({ ...item })));
};

// Khai báo biến ref để truy cập component Location Edit
const locationEditorRef = ref();

// #endregion

// #region REACTIVE STATE

const state = reactive({
  queryParams:{
      itemId: null as any,
      itemGroup: null as any
    },
  editingLayer: null as any, // Store reference to the editing layer
  unsavedChanges: new Set<string>(),
  map: null as L.Map | null as any,
  highlightLayer: null as L.Layer | null,
  markersLayer: ref(L.layerGroup() as L.LayerGroup<L.Marker>),
  polygonsLayer: ref(L.layerGroup() as L.LayerGroup<L.Polygon>),
  tileLayer: null as L.TileLayer | null,
  imageOverlay: null as L.ImageOverlay | null,
  imageBounds: null as L.LatLngBounds | null,
  geoJsonData: {
    type: "FeatureCollection",
    features: [] as any[],
    blockConfig: createEmptyBlockConfiguration(),
  },
  drawingItem: null as any  ,
  currentCoordinates: null as any,
  ruleForm: {} as UpdateTmsMapLocationInput,
  currentMode: null as 'edit' | 'move' | null,
  originalCoordinates: null as any,
  activeFeatureId: null as string | null,
  /**
   * Id các annotation (nhãn văn bản / hình ảnh) đang được chọn trên bản đồ ở màn cấu hình.
   * Bấm vào 1 phần tử đã liên kết cụm sẽ chọn cả cụm — xem composables/useMapAnnotations.ts.
   */
  annotationSelection: [] as string[],
  /**
   * Id phần tử ĐANG NHẮM trong vùng chọn (phần tử được bấm gần nhất). Khi chọn cả cụm,
   * đây là phần tử mà nút "Sửa" / tay cầm bút chì sẽ mở hộp thoại nội dung.
   */
  annotationFocusId: '' as string,
  filterDeviceItems:{} as any,
  previousFeatureGeoJsonData : [] as any | null,
  previousFeatureGeoJsonDataZone : [] as any | null,
  curentFeatureGeoJsonWasFilter : [] as any,
  mapViewState: {
    center: [0, 0] as [number, number],
    zoom: 0,
    maxZoom: 0,
    minZoom: 0,
    editorViewState:0,
    rotation: 0
  } as MapViewState,
  tmsLocationMapId: '' as string | undefined | null,
  mapId: '' as string | undefined | null,
  listIdZoneInEventType: [] as any ,
  randomFourNumber: 1234 as number | undefined,
  listVmsCurrent: [] as any,
  /**
 * Lưu lại vị trí/zoom gốc của map ngay sau khi load xong.
 * Dùng để reset map về trạng thái ban đầu mà không cần gọi lại API.
 */
  initialMapViewState: {
    center: [0, 0] as [number, number],
    zoom: 0,
    maxZoom: 0,
    minZoom: 0,
    rotation: 0
  } as MapViewState,

  /**
   * Biến state dùng để setup màu sắc
   */
  currentMapType: '' as string,
  currentMapMode: '' as string,
  /** Cấu hình chế độ block, được persist trong TmsMapLocation.config. */
  blockConfig: createEmptyBlockConfiguration() as BlockConfiguration,
  blockConfigDirty: false,
  mapBackgroundColor: '#d3d3d3' as string,
  showIconBackground: true,
  /** Khóa zoom trên bản đồ chính (runtime); màn cấu hình vẫn zoom tự do. */
  lockZoom: false,
  /** Khóa xoay bản đồ chính (runtime) ở góc đã cấu hình; màn cấu hình vẫn xoay tự do. */
  lockRotation: false,
  /** Kích thước chung icon thiết bị; 0 = theo từng loại thiết bị. */
  iconSize: 0,
  /** Kích thước chung icon sự kiện sự cố; 0 = theo từng loại sự kiện. */
  incidentIconSize: 0,
  /** Khoảng hở icon sự kiện với mép ngoài zone, đơn vị pixel. */
  incidentZoneGap: INCIDENT_ZONE_GAP_PX,
  /** Kiểu nét đường nối icon sự kiện → vị trí sự kiện; mặc định nét liền. */
  incidentLineStyle: INCIDENT_LINE_DEFAULT_STYLE as IncidentLineStyle,
  /** Độ dày viền zone ở màn giám sát (px); 0 = ẩn viền. */
  zoneBorderWidth: ZONE_BORDER_WIDTH_DEFAULT,
  /** Màu đường định hướng theo hướng (mã hướng → màu; '*' = mọi hướng). */
  flowLineColors: {} as FlowLineColors,
  /** Độ dày đường định hướng (px). */
  flowLineWidth: FLOW_LINE_WIDTH_DEFAULT,

  /**
   * Ẩn/hiện các nhãn/minh hoạ trên màn cấu hình — TÁCH RIÊNG 2 nhóm cấu hình:
   * Nhóm LÝ TRÌNH Km (định vị sự cố):
   * - name: tên zone (ở tâm polygon)
   * - kmArrow: mũi tên lý trình Km (from → to)
   * - arrowFrom: nhãn Km điểm đầu (Km thấp)
   * - arrowTo: nhãn Km điểm đuôi (Km cao)
   * Nhóm CHIỀU DI CHUYỂN (flow):
   * - flowArrow: mũi tên chiều di chuyển của tuyến (theo laneId + flowDirection)
   * - speed: ngưỡng tốc độ min–max nối vào nhãn tên zone
   */
  zoneLabelDisplay: {
    name: true,
    kmArrow: true,
    arrowFrom: true,
    arrowTo: true,
    flowArrow: true,
    speed: true,
  } as { name: boolean; kmArrow: boolean; arrowFrom: boolean; arrowTo: boolean; flowArrow: boolean; speed: boolean },

  /**
   * Biến dùng để scroll chuột zoom in and zoom out
   */
  scrollViewZoomSate:0 as number,
  isRefreshing: false,
  /**
   * Số lần config view được nạp xong từ CSDL (tăng ở cuối loadMapData).
   * Dùng làm mốc để LocationEditor biết khi nào giá trị lat/lng/zoom trong
   * mapViewState là dữ liệu DB của map hiện tại (không phải của map trước đó).
   */
  mapConfigVersion: 0,
  allGeoJsonData: {
    type: "FeatureCollection",
    features: [] as any[],
    blockConfig: createEmptyBlockConfiguration(),
  },
});

// #endregion

// #region COMPUTED

// Dùng để render lại hình
const listVmsCurrentComputed = computed(() => {
  const arrayReturn = [...state.listVmsCurrent]
  return arrayReturn
})

// #endregion

// #region LIFECYCLE HOOKS

//  Chạy khi khởi tạo xong component
onMounted(async () => {
   const startedAt = Date.now();
   let stage = 'map-store';
   logSseDiagnostic('map-load-start', { stage });
   const pending = setInterval(() => logSseDiagnostic('map-load-pending', { stage, elapsedMs: Date.now() - startedAt }), 15000);
   onUnmounted(() => clearInterval(pending));
   // Phím tắt annotation đăng ký TRƯỚC khi tải bản đồ: tải lỗi thì màn cấu hình vẫn
   // dùng được Esc/Delete. Listener được gỡ trong onUnmounted bên dưới.
   document.addEventListener('keydown', handleAnnotationKeydown)
   try {
      await mapStore.loadData();
      logSseDiagnostic('map-store-ready', { elapsedMs: Date.now() - startedAt });
      stage = 'zone-status';
      await loadZoneStatus();
      emitZoneStatusChange();
      // Polygon có thể đã vẽ trước khi snapshot về (lúc đó chưa có trạng thái ⇒ thông thoáng) ⇒ tô lại ngay.
      await refreshVisibleZoneStatusStyles();
      logSseDiagnostic('map-load-ready', { elapsedMs: Date.now() - startedAt });
   } catch (error) {
      logSseDiagnostic('map-load-error', { stage, errorName: (error as Error)?.name });
      throw error;
   } finally {
      clearInterval(pending);
   }
})

/**
 * Phím tắt cho annotation ở màn cấu hình: Esc bỏ chọn, Delete/Backspace xoá phần tử
 * (hoặc cả cụm) đang chọn. Bỏ qua khi con trỏ đang ở trong ô nhập liệu.
 */
const handleAnnotationKeydown = (event: KeyboardEvent) => {
  if (!props.isEditMode || !state.annotationSelection.length) return;

  const target = event.target as HTMLElement | null;
  const tagName = target?.tagName?.toLowerCase();
  if (tagName === 'input' || tagName === 'textarea' || target?.isContentEditable) return;

  if (event.key === 'Escape') {
    clearAnnotationSelection()
    return
  }
  if (event.key === 'Delete' || event.key === 'Backspace') {
    event.preventDefault()
    deleteAnnotationSelection()
  }
}

/** Hàm gỡ listener xoay bản đồ (document/container level). Gán trong setupMapRotateDrag. */
let cleanupRotateDrag: (() => void) | null = null;

/**
 * Dọn dẹp khi component bị huỷ (rời trang/đổi route).
 * Gỡ tile layer + map để HUỶ mọi request tile đang chờ. Nếu không làm bước này,
 * khi tile lỗi (PNA/CORS/502) Leaflet vẫn giữ map sống và tiếp tục phát request
 * tới proxy → trang bị treo, không chuyển trang được.
 */
onUnmounted(() => {
  try {
    document.removeEventListener('keydown', handleAnnotationKeydown)
    // Dừng thao tác kéo/xoay/resize annotation còn treo (listener ở document level).
    stopAnnotationInteraction()

    // Gỡ listener xoay bản đồ (document-level) TRƯỚC khi hủy map để không còn tham chiếu tới map đã remove.
    cleanupRotateDrag?.();

    // Dừng animation xoay đang chạy để callback không chạm vào map đã hủy.
    if (rotateAnimationFrame != null) {
      cancelAnimationFrame(rotateAnimationFrame);
      rotateAnimationFrame = null;
    }

    if (state.tileLayer) {
      state.tileLayer.off();
      if (state.map?.hasLayer(state.tileLayer)) {
        state.map.removeLayer(state.tileLayer);
      }
      state.tileLayer = null;
    }
    if (state.map) {
      state.map.off();
      state.map.remove();
      state.map = null;
    }
  } catch {
    /* ignore lỗi teardown */
  }
})

// #endregion

// #region KHỞI TẠO MAP & ĐIỀU KHIỂN XOAY (rotate)

//Khởi tạo map
const initializeMap = async () => {

  // Chống crash zoom-animation khi layer đã rời map nhưng listener còn sót (leaflet-rotate).
  installLeafletZoomAnimationGuards();

  if (state.map) {
    state.map.remove()
    state.map = null
  }

  // Create new map instance
  state.map = L.map('map', {
    center: [0, 0],
    zoom: 1,
    crs: L.CRS.Simple,
    zoomControl: false,
    attributionControl: false,
    maxZoom: 18,
    minZoom: 1,
    // zoomSnap phải chia hết zoomDelta (0.2) và giữ đúng các mức .5 đã lưu ở config cũ → 0.1
    zoomSnap: 0.1,
    zoomDelta: 0.2, // Mỗi lần zoom in/out (nút, phím +/-) đổi 0.2 mức
    wheelPxPerZoomLevel: 80, // Slower mouse wheel zoom
    rotate: true, // Bật tính năng xoay bản đồ (leaflet-rotate)
    rotateControl: false, // Tắt control xoay mặc định của leaflet-rotate
    shiftKeyRotate: false // Tắt shift+wheel xoay (dùng nút custom)
    // Lưu ý: KHÔNG đặt markerZoomAnimation:false — leaflet-rotate cần marker nằm
    // trong luồng zoom-animation của nó để định vị đúng; tắt đi sẽ khiến toàn bộ
    // marker bị lệch/nhảy khi zoom.
  })

  state.markersLayer = L.layerGroup().addTo(state.map)
  state.polygonsLayer = L.layerGroup().addTo(state.map)

  // handlePmCreate được cung cấp bởi composable useMapEditing (khởi tạo phía dưới);
  // an toàn vì initializeMap chỉ được GỌI (không chạy ngay) sau khi toàn bộ setup() hoàn tất.
  state.map.on('pm:create', handlePmCreate);

   // You can also listen for zoom changes
   state.map.on('zoomend', function() {
      var updatedZoom = state.map.getZoom();
      state.scrollViewZoomSate = updatedZoom
      // Annotation dùng scaleMode='geo' phải đổi kích thước theo mức zoom mới. Chỉ vẽ lại
      // riêng annotation (không dựng lại toàn bộ marker/polygon) để giữ hiệu năng.
      refreshAnnotations()
    });

  // Bấm ra vùng trống trên bản đồ = bỏ chọn annotation. Sự kiện click từ chính annotation
  // đã bị chặn lan (disableClickPropagation) nên không lọt vào đây.
  state.map.on('click', function() {
    if (props.isEditMode) clearAnnotationSelection()
  });

  // Ô Vĩ độ/Kinh độ "điều chỉnh" = TÂM bản đồ sau mỗi lần kéo/zoom xong.
  // Trước đây nghe 'mouseup' và lấy e.latlng = VỊ TRÍ CON TRỎ lúc nhả chuột, nên "tâm" lưu xuống
  // là chỗ nhả chuột chứ không phải tâm khung nhìn → bản đồ chính hiển thị lệch so với lúc thiết lập.
  // Chỉ màn cấu hình cần (runtime không đọc lại giá trị này để lưu).
  state.map.on('moveend', function(){
    if (!props.isEditMode || !state.map) return
    const center = state.map.getCenter()
    state.mapViewState.center = [center.lat, center.lng]
  })

  // Xoay bản đồ bằng chuột: chuột phải kéo hoặc Ctrl+chuột trái kéo
  setupMapRotateDrag(state.map)

  return new Promise(resolve => {
    state.map?.whenReady(() => {
      resolve(true)
    })
  })
}

/**
 * Thiết lập xoay bản đồ bằng kéo chuột:
 * - Chuột phải + kéo: xoay
 * - Ctrl + chuột trái + kéo: xoay
 */
const setupMapRotateDrag = (map: L.Map) => {
  // Nếu đã setup trước đó (re-init map) → gỡ listener cũ để tránh listener rác trỏ vào map đã hủy.
  cleanupRotateDrag?.();

  let isRotating = false;
  let startX = 0;
  let startBearing = 0;

  /**
   * Map còn "sống" và vẫn là map hiện hành hay không.
   * Sau khi map.remove(), Leaflet xóa `_mapPane` → mọi thao tác (setBearing...) đọc
   * `_leaflet_pos` của pane sẽ ném lỗi. Guard này chặn các listener rác (document-level)
   * tác động lên map đã hủy khi rời trang / re-mount.
   */
  const isMapAlive = () => !!state.map && state.map === map && !!(map as any)._mapPane;

  const getStartBearing = () => {
    return typeof (map as any).getBearing === 'function' ? (map as any).getBearing() : 0;
  };

  const container = map.getContainer();

  const onMouseDown = (e: MouseEvent) => {
    if (!isMapAlive()) return;
    if (isRotationLocked()) return; // khóa xoay: bỏ qua kéo xoay (chuột phải vẫn không mở menu)
    // Chuột phải (button=2) hoặc Ctrl+chuột trái (button=0 + ctrlKey)
    const isRightClick = e.button === 2;
    const isCtrlLeftClick = e.button === 0 && e.ctrlKey;

    if (isRightClick || isCtrlLeftClick) {
      e.preventDefault();
      e.stopPropagation();
      isRotating = true;
      startX = e.clientX;
      startBearing = getStartBearing();
      map.dragging?.disable();
      container.style.cursor = 'grabbing';
    }
  };

  const onMouseMove = (e: MouseEvent) => {
    if (!isRotating) return;
    // Map đã bị hủy giữa lúc kéo → dừng, tránh setBearing trên map chết gây crash.
    if (!isMapAlive()) { isRotating = false; return; }
    e.preventDefault();
    const deltaX = e.clientX - startX;
    const newBearing = startBearing + deltaX * 0.5; // 0.5 độ per pixel
    if (typeof (map as any).setBearing === 'function') {
      try {
        (map as any).setBearing(newBearing);
      } catch {
        /* map đang teardown → bỏ qua */
      }
    }
  };

  const onMouseUp = () => {
    if (!isRotating) return;
    isRotating = false;
    if (!isMapAlive()) return;
    map.dragging?.enable();
    container.style.cursor = '';

    // Cập nhật state.rotation
    if (typeof (map as any).getBearing === 'function') {
      state.mapViewState.rotation = (map as any).getBearing();
    }
  };

  const onContextMenu = (e: Event) => e.preventDefault();

  const onRotate = () => {
    if (isMapAlive() && typeof (map as any).getBearing === 'function') {
      state.mapViewState.rotation = (map as any).getBearing();
    }
  };

  // Vô hiệu hóa context menu trên bản đồ (chuột phải)
  container.addEventListener('contextmenu', onContextMenu);
  container.addEventListener('mousedown', onMouseDown);
  document.addEventListener('mousemove', onMouseMove);
  document.addEventListener('mouseup', onMouseUp);

  // Lắng nghe sự kiện rotate để cập nhật state
  map.on('rotate', onRotate);

  // Đăng ký hàm dọn dẹp: gỡ toàn bộ listener document/container để không còn tham
  // chiếu tới map sau khi component unmount hoặc map bị re-init.
  cleanupRotateDrag = () => {
    document.removeEventListener('mousemove', onMouseMove);
    document.removeEventListener('mouseup', onMouseUp);
    try {
      container.removeEventListener('contextmenu', onContextMenu);
      container.removeEventListener('mousedown', onMouseDown);
    } catch {
      /* container có thể đã bị gỡ khỏi DOM */
    }
    cleanupRotateDrag = null;
  };
}

const ROTATE_STEP = 15; // Độ mỗi lần nhấn nút

/** Bản đồ chính đang khóa xoay (màn cấu hình không bao giờ khóa). */
const isRotationLocked = () => !props.isEditMode && state.lockRotation === true;

// Báo trạng thái khóa xoay ra ngoài để la bàn ở màn chính hiện "đã khóa" và ngừng nhận thao tác
watch(() => isRotationLocked(), (locked) => emit('rotation-lock-change', locked), { immediate: true })

const rotateLeft = () => {
  if (isRotationLocked()) return;
  if (!state.map || typeof (state.map as any).getBearing !== 'function') return;
  const currentBearing = (state.map as any).getBearing();
  const newBearing = currentBearing - ROTATE_STEP;
  (state.map as any).setBearing(newBearing);
  state.mapViewState.rotation = (state.map as any).getBearing();
}

const rotateRight = () => {
  if (isRotationLocked()) return;
  if (!state.map || typeof (state.map as any).getBearing !== 'function') return;
  const currentBearing = (state.map as any).getBearing();
  const newBearing = currentBearing + ROTATE_STEP;
  (state.map as any).setBearing(newBearing);
  state.mapViewState.rotation = (state.map as any).getBearing();
}

/**
 * Trả bản đồ về ĐÚNG góc xoay đã cấu hình cho bản đồ hiện tại (TmsMapLocation.rotation),
 * không thay đổi tâm/zoom. Dùng chung cho nút "về góc xoay mặc định" trên panel điều khiển
 * và cho chuột phải trên la bàn.
 */
const rotateReset = () => {
  if (!state.map || typeof (state.map as any).setBearing !== 'function') return;
  setBearing(state.initialMapViewState.rotation || 0);
}

const rotateByDelta = (delta: number) => {
  if (isRotationLocked()) return;
  if (!state.map || typeof (state.map as any).getBearing !== 'function') return;
  const currentBearing = (state.map as any).getBearing();
  (state.map as any).setBearing(currentBearing + delta);
  state.mapViewState.rotation = (state.map as any).getBearing();
}

/** Handle của animation xoay bản đồ; giữ để huỷ khi có lệnh xoay mới. */
let rotateAnimationFrame: number | null = null;

/**
 * Xoay bản đồ tới một góc tuyệt đối (0 = hướng Bắc, 180 = hướng Nam) kèm animation
 * ngắn để người dùng thấy rõ bản đồ đang quay về hướng nào.
 *
 * Chọn chiều quay ngắn nhất; riêng trường hợp lệch đúng 180° thì quay theo chiều dương
 * để chuyển động luôn xác định (không phụ thuộc sai số dấu).
 */
const setBearing = (targetBearing: number, animate = true) => {
  // Khóa xoay: bản đồ đứng yên ở góc đã cấu hình (loader đặt góc trực tiếp, không qua hàm này)
  if (isRotationLocked()) return;
  if (!state.map || typeof (state.map as any).setBearing !== 'function') return;

  const applyBearing = (value: number) => {
    try {
      (state.map as any).setBearing(value);
      state.mapViewState.rotation = (state.map as any).getBearing();
    } catch {
      /* map đang teardown → bỏ qua */
    }
  };

  if (rotateAnimationFrame != null) {
    cancelAnimationFrame(rotateAnimationFrame);
    rotateAnimationFrame = null;
  }

  const startBearing = typeof (state.map as any).getBearing === 'function' ? (state.map as any).getBearing() : 0;
  const rawDelta = ((Number(targetBearing) || 0) - startBearing) % 360;
  let delta = rawDelta > 180 ? rawDelta - 360 : rawDelta < -180 ? rawDelta + 360 : rawDelta;
  if (Math.abs(Math.abs(delta) - 180) < 0.001) delta = 180;

  if (!animate || Math.abs(delta) < 0.5) {
    applyBearing(startBearing + delta);
    return;
  }

  const durationMs = 400;
  const startTime = performance.now();
  const step = () => {
    rotateAnimationFrame = null;
    if (!state.map) return;
    const progress = Math.min(1, (performance.now() - startTime) / durationMs);
    // easeOutCubic: quay nhanh lúc đầu rồi giảm dần, cảm giác dứt khoát nhưng không giật
    const eased = 1 - Math.pow(1 - progress, 3);
    applyBearing(startBearing + delta * eased);
    if (progress < 1) rotateAnimationFrame = requestAnimationFrame(step);
  };
  rotateAnimationFrame = requestAnimationFrame(step);
}

// Phát góc xoay hiện tại ra ngoài để la bàn ở panel điều khiển vẽ đúng hướng kim.
watch(() => state.mapViewState.rotation, (rotation) => {
  emit('bearing-change', Number(rotation) || 0);
}, { immediate: true })

// #endregion

// #region LAYER & VIEWPORT HELPERS (clear layer, reset view, zoom, màu nền)

const clearLayers = () => {
  if (state.tileLayer && state.map) {
    state.map?.removeLayer(state.tileLayer)
    state.tileLayer = null
  }
  if (state.imageOverlay && state.map) {
    state.map?.removeLayer(state.imageOverlay)
    state.imageOverlay = null
  }
  state.imageBounds = null
  if (state.markersLayer) {
    state.markersLayer.clearLayers()
  }
  if (state.polygonsLayer) {
    state.polygonsLayer.clearLayers()
  }
}

/**
 * Reset viewport của bản đồ về đúng vị trí/zoom ban đầu
 * khi map vừa được load xong.
 *
 * Hàm này:
 * - Không gọi API
 * - Không load lại geojson
 * - Không clear layer
 * - Chỉ đưa map về lại center + zoom gốc
 */
const resetViewToInitial = () => {
  if (!state.map) return;

  const initialState = state.initialMapViewState;

  // Kiểm tra dữ liệu reset hợp lệ
  if (
    !initialState ||
    !Array.isArray(initialState.center) ||
    initialState.center.length !== 2
  ) {
    return;
  }

  // Khôi phục min/max zoom đúng theo cấu hình gốc
  if (typeof initialState.minZoom === 'number') {
    state.map.setMinZoom(initialState.minZoom);
  }

  if (typeof initialState.maxZoom === 'number') {
    state.map.setMaxZoom(initialState.maxZoom);
  }

  // Đưa map về đúng vị trí/zoom ban đầu
  state.map.setView(initialState.center, initialState.zoom, {
    animate: true
  });

  // Khôi phục rotation (bearing) về giá trị gốc
  if (typeof initialState.rotation === 'number' && typeof (state.map as any).setBearing === 'function') {
    (state.map as any).setBearing(initialState.rotation);
  }

  // Reset đã khôi phục min/max theo cấu hình → khóa lại zoom (nếu bật) tại mức zoom gốc
  applyZoomLock();
  applyRotationLock();

  // Đồng bộ lại state hiện tại để UI/form không bị lệch
  state.mapViewState = JSON.parse(JSON.stringify(initialState));

  // Nếu đang có maxBounds thì ép map nằm lại trong vùng hợp lệ
  const currentBounds = (state.map as any).options?.maxBounds;
  if (currentBounds) {
    state.map.panInsideBounds(currentBounds, { animate: false });
  }
};

// Lưu trạng thái view hiện tại (center + zoom)
const saveViewState = () => {
  if (!state.map || !props.selectedMapId) return

  const center = state.map.getCenter()
  const zoom = state.map.getZoom()

  return {
    center: [center.lat, center.lng],
    zoom: zoom
  }
}

const zoomIn = () => {
  state.map?.zoomIn()
}

const zoomOut = () => {
  state.map?.zoomOut()
}

/**
 * Hàm cập nhật màu nền (dùng cho map dạng hình ảnh)
 */
const handleUpdateMapBackgroundColor = (color: string) => {
  state.mapBackgroundColor = color || '#d3d3d3';
};

/** Cập nhật kích thước chung của icon thiết bị và render ngay để xem trước. */
const handleUpdateIconSize = (value: number | undefined) => {
  const iconSize = Number(value);
  state.iconSize = Number.isFinite(iconSize) && iconSize > 0 ? iconSize : 0;
  void renderGeoJson();
};

/** Cập nhật kích thước chung của icon sự kiện sự cố và render ngay để xem trước. */
const handleUpdateIncidentIconSize = (value: number | undefined) => {
  const size = Number(value);
  state.incidentIconSize = Number.isFinite(size) && size > 0 ? size : 0;
  void renderGeoJson();
};

/** Cập nhật khoảng hở icon sự kiện với mép zone; được lưu theo từng bản đồ. */
const handleUpdateIncidentZoneGap = (value: number | undefined) => {
  state.incidentZoneGap = resolveIncidentZoneGap(value);
  void renderGeoJson();
};

/** Cập nhật kiểu nét đường nối sự kiện; được lưu theo từng bản đồ. */
const handleUpdateIncidentLineStyle = (value: unknown) => {
  state.incidentLineStyle = resolveIncidentLineStyle(value);
  void renderGeoJson();
};

/** Cập nhật độ dày viền zone (chỉ đổi biến CSS nên không cần vẽ lại); lưu theo từng bản đồ. */
const handleUpdateZoneBorderWidth = (value: unknown) => {
  state.zoneBorderWidth = resolveZoneBorderWidth(value);
};

/** Cập nhật màu đường định hướng theo hướng và vẽ lại để xem trước; lưu theo từng bản đồ. */
const handleUpdateFlowLineColors = (value: unknown) => {
  state.flowLineColors = normalizeFlowLineColors(value);
  void renderGeoJson();
};

/** Cập nhật độ dày đường định hướng và vẽ lại để xem trước; lưu theo từng bản đồ. */
const handleUpdateFlowLineWidth = (value: unknown) => {
  state.flowLineWidth = resolveFlowLineWidth(value);
  void renderGeoJson();
};

/** Đồng bộ cấu hình block từ khung thiết lập hoặc card được kéo trên bản đồ. */
const handleUpdateBlockConfig = (config: BlockConfiguration) => {
  state.blockConfig = config;
  // Block config được persist cùng FeatureCollection trong geoJsonContent.
  state.geoJsonData.blockConfig = config;
  state.allGeoJsonData.blockConfig = config;
  state.blockConfigDirty = true;
  // Danh sách filter phụ thuộc thiết bị đang có trong block -> phát lại payload cho panel filter.
  ReturnGeoDataJsonToComponentIndex();
  // Chỉ được gọi khi thả card/điểm neo hoặc cập nhật editor, không gọi mỗi pointermove.
  // Nhờ đó marker được đồng bộ mà thao tác kéo vẫn mượt.
  void renderGeoJson();
};

/**
 * Áp dụng cấu hình hiển thị cấp bản đồ từ file GeoJSON (field nào thiếu thì giữ nguyên).
 * Màu nền chỉ áp dụng cho bản đồ ảnh — bản đồ online luôn nền trắng.
 */
const applyImportedMapDisplay = (display?: MapDisplayTransfer) => {
  if (!display) return;
  if (typeof display.showIconBackground === 'boolean') state.showIconBackground = display.showIconBackground;
  if (typeof display.lockZoom === 'boolean') state.lockZoom = display.lockZoom;
  if (typeof display.lockRotation === 'boolean') state.lockRotation = display.lockRotation;
  if (display.iconSize !== undefined) state.iconSize = display.iconSize > 0 ? display.iconSize : 0;
  if (display.incidentIconSize !== undefined) state.incidentIconSize = display.incidentIconSize > 0 ? display.incidentIconSize : 0;
  if (display.incidentZoneGap !== undefined) state.incidentZoneGap = resolveIncidentZoneGap(display.incidentZoneGap);
  if (display.incidentLineStyle !== undefined) state.incidentLineStyle = resolveIncidentLineStyle(display.incidentLineStyle);
  if (display.zoneBorderWidth !== undefined) state.zoneBorderWidth = resolveZoneBorderWidth(display.zoneBorderWidth);
  if (display.flowLineColors !== undefined) state.flowLineColors = normalizeFlowLineColors(display.flowLineColors);
  if (display.flowLineWidth !== undefined) state.flowLineWidth = resolveFlowLineWidth(display.flowLineWidth);
  if (display.backgroundColor && state.currentMapType === 'image') state.mapBackgroundColor = display.backgroundColor;
  if (display.zoneLabelDisplay) {
    (Object.keys(state.zoneLabelDisplay) as (keyof typeof state.zoneLabelDisplay)[]).forEach((key) => {
      const value = display.zoneLabelDisplay?.[key];
      if (typeof value === 'boolean') state.zoneLabelDisplay[key] = value;
    });
  }
};

/**
 * Áp dụng dữ liệu GeoJSON vừa nhập từ file (LocationEditor đã validate + xác nhận).
 *
 * Phải xử lý tại đây vì state của bản đồ nằm ở component này:
 * - Xoá cache filter (previousFeatureGeoJsonData / previousFeatureGeoJsonDataZone):
 *   nếu không, các feature đang bị filter ẩn của bản đồ CŨ sẽ được ghép lại vào
 *   geoJsonContent lúc bấm Lưu (xem handleSave) → dữ liệu nhập bị lẫn dữ liệu cũ.
 * - Ghi đè blockConfig: block được lưu cùng FeatureCollection nên phải nạp lại từ
 *   file, ngược lại chế độ block vẫn giữ cấu hình của bản đồ trước khi nhập.
 * - Đánh dấu unsaved ở ĐÚNG state của bản đồ để marker hiện màu "chưa lưu"
 *   (LocationEditor có state riêng, render lại không đọc tới).
 */
const handleImportGeoJson = async (payload: { features?: any[]; blockConfig?: BlockConfiguration; mapDisplay?: MapDisplayTransfer }) => {
  const importedFeatures = Array.isArray(payload?.features) ? payload.features : [];
  if (!importedFeatures.length) return;

  // Cấu hình hiển thị từ file: gán thẳng vào state rồi render 1 lần ở cuối
  // (không gọi các handler update-* vì mỗi handler tự render lại).
  applyImportedMapDisplay(payload?.mapDisplay);

  // Kết thúc edit/move đang mở để không giữ tham chiếu tới layer của dữ liệu cũ.
  if (state.currentMode) finishEditing();

  state.previousFeatureGeoJsonData = [];
  state.previousFeatureGeoJsonDataZone = [];
  state.listIdZoneInEventType = [];

  // Clone 1 lần rồi DÙNG CHUNG object feature cho 2 danh sách (2 mảng khác nhau,
  // phần tử giống nhau) — đúng như loadMapData. Bắt buộc phải chung tham chiếu vì
  // updateMarkerPositionInGeoJSON / sửa polygon chỉ ghi vào geoJsonData và dựa vào
  // tham chiếu chung để allGeoJsonData (dùng khi xuất file) không bị lệch toạ độ.
  const clonedFeatures = JSON.parse(JSON.stringify(importedFeatures));
  state.allGeoJsonData.features = clonedFeatures;
  state.geoJsonData.features = [...clonedFeatures];

  state.unsavedChanges.clear();
  importedFeatures.forEach((feature: any) => {
    const id = feature?.properties?.id;
    if (id) state.unsavedChanges.add(String(id));
  });

  // Fallback toạ độ cho block thiếu vị trí = tâm bản đồ hiện tại.
  const blockConfig = normalizeBlockConfiguration(payload?.blockConfig, {
    lat: state.mapViewState.center[0],
    lng: state.mapViewState.center[1],
  });
  state.blockConfig = blockConfig;
  state.geoJsonData.blockConfig = blockConfig;
  state.allGeoJsonData.blockConfig = blockConfig;
  state.blockConfigDirty = true;

  ReturnGeoDataJsonToComponentIndex();
  await renderGeoJson();
};

/**
 * Bật/tắt hiển thị nhãn mô tả zone (tên zone / nhãn điểm đầu / nhãn điểm đuôi mũi tên).
 * Cập nhật state rồi render lại bản đồ.
 */
const handleToggleZoneLabel = (payload: { key: 'name' | 'kmArrow' | 'arrowFrom' | 'arrowTo' | 'flowArrow' | 'speed'; value: boolean }) => {
  if (!payload || !payload.key) return;
  state.zoneLabelDisplay[payload.key] = payload.value;
  renderGeoJson();
};

/**
 * Cập nhật thuộc tính 1 nhãn văn bản (itemGroup='text') sau khi sửa trong hộp thoại:
 * nội dung/cỡ chữ/xoay/màu/hình dạng. Cập nhật cả geoJsonData & allGeoJsonData rồi
 * đánh dấu chưa lưu + render lại.
 */
/** Lật ngang icon thiết bị đặt lẻ: ghi vào feature.properties.flipX (lưu cùng geoJsonContent). */
const handleUpdateIconFlip = (payload: { id: string; flipX: boolean }) => {
  if (!payload?.id) return;
  const applyTo = (list: any) => {
    const f = list?.features?.find((x: any) =>
      x.geometry?.type === 'Point' && x.properties?.itemGroup === 'device' && String(x.properties?.id) === String(payload.id));
    if (!f) return;
    const nextProps = { ...f.properties };
    if (payload.flipX) nextProps.flipX = true;
    else delete nextProps.flipX;
    f.properties = nextProps;
  };
  applyTo(state.geoJsonData);
  applyTo(state.allGeoJsonData);
  handleMarkUnsaved(payload.id);
};

const handleUpdateText = (payload: { id: string; props: Record<string, any> }) => {
  if (!payload?.id) return;
  const applyTo = (list: any) => {
    const f = list?.features?.find((x: any) => x.geometry?.type === 'Point' && x.properties?.id === payload.id);
    if (f) {
      f.properties = { ...f.properties, ...payload.props };
      // Đồng bộ nhãn hiển thị trong bảng theo nội dung text
      if (payload.props.text != null) f.properties.name = payload.props.text;
    }
  };
  applyTo(state.geoJsonData);
  applyTo(state.allGeoJsonData);
  handleMarkUnsaved(payload.id);
};

/**
 * Cập nhật thuộc tính 1 hình ảnh tự do (itemGroup='image') sau khi sửa trong hộp thoại:
 * ảnh (base64), chiều rộng, xoay, độ mờ, bo góc. Không đổi `name` theo ảnh vì tên do
 * người dùng tự đặt để nhận biết trong danh sách.
 */
const handleUpdateImage = (payload: { id: string; props: Record<string, any> }) => {
  if (!payload?.id) return;
  const applyTo = (list: any) => {
    const f = list?.features?.find((x: any) => x.geometry?.type === 'Point' && x.properties?.id === payload.id);
    if (f) f.properties = { ...f.properties, ...payload.props };
  };
  applyTo(state.geoJsonData);
  applyTo(state.allGeoJsonData);
  handleMarkUnsaved(payload.id);
};

/**
 * Cập nhật 1 khối HTML tự do (itemGroup='html') sau khi sửa trong hộp thoại: nội dung,
 * bề rộng, xoay, co giãn/xoay theo bản đồ. `name` đi kèm payload (đoạn chữ rút gọn của nội dung).
 */
const handleUpdateHtml = (payload: { id: string; props: Record<string, any> }) => {
  if (!payload?.id) return;
  const applyTo = (list: any) => {
    const f = list?.features?.find((x: any) => x.geometry?.type === 'Point' && x.properties?.id === payload.id);
    if (f) f.properties = { ...f.properties, ...payload.props };
  };
  applyTo(state.geoJsonData);
  applyTo(state.allGeoJsonData);
  handleMarkUnsaved(payload.id);
};

// #endregion

// #region HIỂN THỊ NHÃN TÊN ICON (show/hide marker-name)

const checkShowIconName = () => {
  const elements = document.querySelectorAll<HTMLDivElement>('div.marker-name')
  elements.forEach(el => {
    el.style.display = mapStore.showIconName? 'block' : 'none'
  })
}

watch(()=> mapStore.showIconName, (newVal) => {
  if(newVal == null) return;
  checkShowIconName();
})

// #endregion

// #region TỐC ĐỘ ZONE (min–max) — chỉ màn cấu hình

/**
 * Nạp cấu hình tốc độ zone cho nhãn và ô nhập ở tab Vị trí; bỏ bản nháp của lần mở trước.
 * Lỗi chỉ làm mất phần tốc độ, không chặn bản đồ.
 */
const loadZoneSpeed = async () => {
  mapStore.zoneSpeedDrafts = {};
  try {
    await mapStore.fetchZoneSpeed();
  } catch (error) {
    console.warn('[Map] zone speed unavailable', error);
  }
};

// #endregion

// #region EMIT DỮ LIỆU GEOJSON RA COMPONENT CHA (MapControl)

/**
 * Trả dữ liệu GeoDataJson về cho compoent Index để truyền qua component MapControl
 */
const ReturnGeoDataJsonToComponentIndex = () => {
  const dataSource = state.allGeoJsonData?.features?.length ? state.allGeoJsonData : state.geoJsonData
  let isExistZone = false;
  const featureGeoJsonDataNotIncludePolyGon = dataSource?.features.map((feature : any) => {
    if(feature?.properties.itemGroup != 'zone'){
      return feature;
    }else{
      isExistZone = true;
    }
  }).filter((item) => item != undefined)

  if(isExistZone != undefined && isExistZone){
    featureGeoJsonDataNotIncludePolyGon.push({
      properties:{
        itemGroup: "zone"
      }
    })
  }

  // Chế độ block: thiết bị bố trí trong block KHÔNG bắt buộc phải có vị trí (marker) trên
  // bản đồ, nên có thể không tồn tại feature nào cho chúng. Panel filter lại chỉ giữ lại
  // loại thiết bị nào "có dữ liệu trên bản đồ" (xem UpdateItemGroup ở MapControl/controlMap),
  // dẫn tới loại thiết bị chỉ nằm trong block bị loại khỏi danh sách filter -> không lọc được.
  // Vì vậy bổ sung các thiết bị trong blockConfig vào payload gửi ra panel filter.
  const isBlockMode = state.currentMapMode === TmsTrafficInfoMap.Block || state.currentMapMode === TmsTrafficInfoMap.BlockVms;
  if (isBlockMode) {
    const existingDeviceIds = new Set(
      featureGeoJsonDataNotIncludePolyGon
        .filter((feature: any) => feature?.properties?.itemGroup === 'device')
        .map((feature: any) => String(feature?.properties?.id))
    );

    state.blockConfig?.blocks?.forEach((block: any) => {
      block?.levels?.forEach((level: any) => {
        level?.devices?.forEach((device: any) => {
          const deviceId = String(device?.deviceId ?? '').trim();
          if (!deviceId || existingDeviceIds.has(deviceId)) return;
          existingDeviceIds.add(deviceId);
          featureGeoJsonDataNotIncludePolyGon.push({ properties: { id: deviceId, itemGroup: 'device' } });
        });
      });
    });
  }

  emit('return-geo-json-data', featureGeoJsonDataNotIncludePolyGon)
}

// #endregion

// #region COMPOSABLES NGHIỆP VỤ CHÍNH (render → filter → loader → editing)
//
// Thứ tự khởi tạo dưới đây là BẮT BUỘC (không đổi tuỳ ý):
// 1. useMapRender  cần trước tiên vì filters/loader/editing đều phụ thuộc renderGeoJson.
// 2. useMapFilters cần renderGeoJson + checkShowIconName.
// 3. useMapLoader  cần renderGeoJson + checkShowIconName + clearLayers + ReturnGeoDataJsonToComponentIndex.
// 4. useMapEditing cần renderGeoJson + clearHighlight + handleMarkUnsaved (từ useMapRender).
// 5. watch(selectedMapId) bên dưới dùng { immediate: true } nên cần handleSave/loadMapData/
//    applyFilters/initializeMap đã được định nghĩa xong TRƯỚC khi watch này được tạo.

// --- 1. Render dữ liệu GeoJSON lên bản đồ (marker device/unit/VMS, polygon zone + icon sự cố) ---
const {
  renderGeoJson,
  renderMarkerVMS,
  handleMarkUnsaved,
  clearHighlight,
  updateMarkerPositionInGeoJSON,
  // Annotation tự do (nhãn văn bản / hình ảnh): xem composables/useMapAnnotations.ts
  refreshAnnotations,
  getSelectedAnnotations,
  getFocusedAnnotation,
  clearAnnotationSelection,
  groupAnnotationSelection,
  ungroupAnnotationSelection,
  setAnnotationSelectionScaleMode,
  deleteAnnotationSelection,
  openSelectedAnnotationEditor,
  stopAnnotationInteraction,
} = useMapRender({
  state,
  props,
  emit,
  locationEditorRef,
  applyZoneStatusStyle,
  t: $t,
  // Bấm icon sự kiện VDS trực tiếp: mở chi tiết sự kiện (cùng quyền xem như trước).
  onLiveEventClick: (item: any) => { if (auth('tmsIncident:view')) vdsEventDetail.value?.open(item); },
})

/**
 * Thông tin vùng chọn annotation, phục vụ thanh công cụ nổi trên bản đồ.
 * - canGroup: cần ≥ 2 phần tử mới liên kết được thành cụm.
 * - canUngroup: trong vùng chọn có phần tử đang thuộc cụm.
 * - isGeoScale: tất cả đang co giãn theo zoom (dùng để hiện đúng nút bật/tắt).
 * - focusLabel: tên/nội dung phần tử đang nhắm — nút "Sửa" mở hộp thoại của chính nó,
 *   nên vẫn sửa được nội dung riêng từng thành viên khi đang chọn cả cụm.
 */
const annotationSelectionInfo = computed(() => {
  const features = state.annotationSelection.length ? getSelectedAnnotations() : []
  const focused = features.length ? getFocusedAnnotation() : null
  const focusText = String(focused?.properties?.text ?? focused?.properties?.name ?? '').trim()

  return {
    count: features.length,
    canGroup: features.length > 1,
    canUngroup: features.some((feature: any) => !!feature.properties?.groupId),
    isGeoScale: features.length > 0 && features.every((feature: any) => feature.properties?.scaleMode === 'geo'),
    canEdit: !!focused,
    focusLabel: focusText.length > 24 ? `${focusText.slice(0, 24)}…` : focusText,
  }
})

// --- 2. Lọc thiết bị/zone/unit/sự kiện ---
const { applyFilters } = useMapFilters({ state, renderGeoJson, checkShowIconName })
// Watch filter change
watch(
  () => props.filters,
  async (newFilter) => {
    await applyFilters(newFilter)
  },
  { deep: true, immediate: true }
)

// --- 3. Load bản đồ (config view, tile/ảnh, geojson) ---
const { loadMapData, centerImageMap, applyZoomLock, applyRotationLock } = useMapLoader({
  state,
  props,
  renderGeoJson,
  checkShowIconName,
  clearLayers,
  returnGeoDataJsonToComponentIndex: ReturnGeoDataJsonToComponentIndex,
})

// --- 4. Chỉnh sửa bản đồ (edit/move/draw, unsaved tracking, handleSave) ---
const {
  finishEditing,
  handlePmCreate,
  handleStartEditing,
  handleStartMoving,
  clearUnsavedChanges,
  handleSave,
  handleAddItem,
  handleStartDrawingPolygon,
} = useMapEditing({
  state,
  locationEditorRef,
  renderGeoJson,
  returnGeoDataJsonToComponentIndex: ReturnGeoDataJsonToComponentIndex,
  clearHighlight,
  handleMarkUnsaved,
  t: $t,
})

// #endregion

// #region WATCHERS - VÒNG ĐỜI MAP & SỰ KIỆN REALTIME

// Watch selectMapId change — tải lại toàn bộ map khi đổi map hoặc mount lần đầu
watch(() => props.selectedMapId, async (newMapId : any, oldMapId : any) => { 
  // Gen lại random number
  state.randomFourNumber = generateRandomNumber(4);

  state.mapId = newMapId
  if (newMapId !== oldMapId && oldMapId != undefined) 
  {
    // Handle map changes with unsaved changes     
    if (state.unsavedChanges.size > 0 || state.blockConfigDirty)
    {
        state.mapId = oldMapId;
        try {
          await ElMessageBox.confirm(
            $t('lz.message.tmsLocation.confirmUnsavedChange'),
            $t('lz.label.base.unsavedChange'),
            {
              confirmButtonText: $t('lz.button.base.save'),
              cancelButtonText: $t('lz.button.base.cancel'),
              type: 'warning',
            }
          );
          const stringifyGeoJson = JSON.stringify(state.allGeoJsonData?.features?.length ? state.allGeoJsonData : state.geoJsonData)
          await handleSave({geoJson:stringifyGeoJson, status:1, defaultZoom: state.mapViewState.zoom, maxZoom:state.mapViewState.maxZoom, minZoom:state.mapViewState.minZoom, 
                longtitude:state.mapViewState.center[1], latitude:state.mapViewState.center[0]});
        } 
        catch (e) {
          state.unsavedChanges.clear();
          await loadMapData(newMapId);
        }
    } 
  }
  if(newMapId) 
  {
    // Kiểm tra zoneList đã có trong mapStore chưa
    // Nếu chưa gọi lại 1 lần nữa
    if(mapStore && mapStore.zoneList && mapStore.zoneList.length < 1)
    {
      await mapStore.fetchZoneList();
    }
    // Reinitialize map on change
    await initializeMap()
    console.info('[Map load] map-init-ready', newMapId);

    // Kiểm tra đường dẫn lấy hình
    if(mapStore.baseUrl == '' || mapStore.baseUrl == null || mapStore.baseUrl == undefined){
      try {
        await mapStore.fetchSystemSysconfigFileserverGet();
      } 
      catch (error) {
        console.error("Error initializing map fetch file server:", error);
      }
    }

    console.info('[Map load] map-file-server-ready', newMapId);
    // Ảnh VMS là dữ liệu bổ sung: API chậm/lỗi không được chặn bản đồ nền.
    // Chỉ render lại sau khi map đã nạp xong và người dùng vẫn ở map này.
    const vmsRequest = mapStore.fetchVmsCurrentListGet().then(() => true).catch(error => {
      console.warn('[Map load] VMS unavailable', error);
      return false;
    });

    // Lấy danh sách loại sự kiện
    await mapStore.fetchEventTypeData();
    console.info('[Map load] map-event-types-ready', newMapId);

    // Lấy danh sách vùng zone xảy ra sự kiện sự cố.
    await mapStore.fetchIncidentZoneDataExistInTrafficInfo() 
    console.info('[Map load] map-incidents-ready', newMapId);

    // Cấu hình tốc độ zone (nhãn + ô nhập ở tab Vị trí): chỉ màn cấu hình.
    if (props.isEditMode) await loadZoneSpeed();

    // Load lại map
    console.info('[Map load] map-data-start', newMapId);
    await loadMapData(newMapId)
    console.info('[Map load] map-data-finished', newMapId);
    void vmsRequest.then(async loaded => {
      if (loaded && props.selectedMapId === newMapId && state.map) await renderGeoJson();
    }).catch(error => console.warn('[Map load] VMS render failed', error));
  }
  if (Object.keys(state.filterDeviceItems || {}).length > 0) {    
      await applyFilters(state.filterDeviceItems)
  } 
  else if (props.filters && Object.keys(props.filters).length > 0) 
  {     
      await applyFilters(props.filters)
  }
}, { immediate: true })

// Làm hết hạn trạng thái zone (stale) theo giờ server.
// Icon sự cố/sự kiện KHÔNG lọc ở client: BE quyết định hồ sơ nào còn hiển thị.
// Sự cố không có hạn hiển thị; sự kiện đã sang Xác minh/Đang xử lý vẫn được BE trả về dù hạn đã qua.
// Lọc lại ở client từng làm mất icon giữa lúc kịch bản tự động đang chạy.
const eventExpiryTimer = setInterval(async () => {
  if (!props.isEditMode && !state.currentMode && state.map) {
    const changed = expireZoneStatuses();
    if (changed.length) { emitZoneStatusChange(); await refreshVisibleZoneStatusStyles(changed); }
  }
}, 1000);
const disconnectServerTime = serverTime.connect();
onUnmounted(() => { clearInterval(eventExpiryTimer); disconnectServerTime(); });
// renderGeoJson xoá rồi vẽ lại toàn bộ layer và không tự chống gọi chồng: icon sự cố và sự kiện VDS trực tiếp
// cùng yêu cầu vẽ lại nên phải xếp hàng, nếu không lần vẽ sau xoá marker lần trước đang thêm dở.
let renderQueue: Promise<void> = Promise.resolve();
const queueRenderGeoJson = () => (renderQueue = renderQueue.then(() => renderGeoJson()).catch((error) => console.warn('[Map render]', error)));
let iconFingerprint = '';
useIncidentSnapshot(() => props.isEditMode || state.currentMode || !state.map || !state.mapId ? null : ({}), async snapshot => {
  if (props.isEditMode || state.currentMode || !state.map || !state.mapId) return;
  const fingerprint = JSON.stringify(snapshot.icons ?? []);
  if (fingerprint === iconFingerprint && JSON.stringify(mapStore.incidentZoneDataExistInTrafficInfoList) === fingerprint) return;
  iconFingerprint = fingerprint;
  // Log chặng "BE (SSE iconstream, poll DB 3s) → bản đồ". Icon lấy từ DB chứ không từ NATS.
  // BE chỉ trả sự cố nhóm "incident" khi đã Xác nhận/Đang xử lý (State 2/3); nhóm "event" hiện cả lúc Đăng ký.
  console.groupCollapsed(`%c[Map][Incident icons] ${snapshot.icons?.length ?? 0} icon`, 'color:#409EFF;font-weight:bold');
  console.table((snapshot.icons ?? []).map((x: any) => ({
    incidentId: x.incidentId, state: x.state, eventGroup: x.eventGroup,
    viTri: x.kmNumber != null ? `Km${x.kmNumber}+${x.metNumber ?? 0}` : '', zones: (x.listZoneId ?? []).length,
  })));
  console.groupEnd();
  mapStore.incidentZoneDataExistInTrafficInfoList = snapshot.icons ?? [];
  await mapStore.fetchEventTypeData();
  await queueRenderGeoJson();
}, 'icons');

// Đồng bộ equipmentState từ `ta.its.status.fms`, sau đó chỉ thay icon marker bị ảnh hưởng.
useTransporterStatusFms((updates) => {
  const changedEquipmentIds = mapStore.updateEquipmentStatusFromNats(updates);
  // Edit mode hoặc đang chỉnh sửa/di chuyển: không cập nhật marker
  if (props.isEditMode || state.currentMode || !state.map) return;

  changedEquipmentIds.forEach((equipmentId) => {
    const feature = state.geoJsonData.features.find((item: any) =>
      item.geometry?.type === 'Point' &&
      item.properties?.itemGroup === 'device' &&
      String(item.properties?.id) === equipmentId
    );
    if (feature) renderMarkerVMS(false, feature, true);
  });
});

// SQL snapshot is authoritative; redraw existing zone layers without rebuilding the map.
useVdsSse(['zone'], async snapshot => {
  if (props.isEditMode || state.currentMode || !state.map) return;
  applyZoneSnapshot(snapshot);
  emitZoneStatusChange();
  await refreshVisibleZoneStatusStyles();
});

// Lắng nghe broadcast khi có trigger hẹn giờ (cập nhật ảnh VMS).
// Edit mode hiển thị cấu hình, không phải nội dung/runtime state nên không render lại marker.
watch(() => vmsMonitor.isVmsUpdate, async (newValue, oldValue) => {
  // Edit mode hoặc đang chỉnh sửa/di chuyển: không cập nhật VMS content
  if (props.isEditMode || state.currentMode || newValue === oldValue) return;

  state.geoJsonData.features.forEach((feature: any) => {
    if (feature.geometry.type === 'Point') {
      // forceRender=true: bỏ qua guard kiểm tra map mode (VMS/LCS icon phải cập nhật
      // realtime bất kể map đang ở mode nào — tương tự FMS status handler).
      renderMarkerVMS(false, feature, true);
    }
  });
})
// #endregion

// #region PUBLIC API — CÁC HÀM ĐƯỢC EXPOSE RA COMPONENT CHA

/**
 * Get GeoJsonData
 */
const handleGetGeoJsonData = () => {
  return state.geoJsonData.features;
}

/**
 * Get ZoneList
 */
 const handleGetZoneListData = () => {  
  return mapStore.zoneList;
}

/**
 * Get EquipmentList
 */
 const handleEquipmentListData = () => {
  return mapStore.equipmentList;
}

/**
 * Get Unit List
 */
 const handleGetUnitListData = () => {
  return mapStore.workUnitList;
}

/**
 * Get Current Mode
 */
const handleGetCurrentMode = () => {
  return state.currentMode
}

/**
 * Get Geo Json Data From Leaflet
 */
 const handleGetGeoJsonDataFromLeaflet = () => {
  return state.geoJsonData
}

/**
 * Bắt event refresh từ parent (Index.vue)
 */
const handleRefreshTrigger = () => {
  state.isRefreshing = true
}

/**
 * Chỉ áp lại style cho các polygon zone đã render. Không clear layer, không load
 * GeoJSON và không khởi tạo lại Leaflet map.
 */
const refreshVisibleZoneStatusStyles = async (zoneIds?: string[]) => {
  if (props.isEditMode || state.currentMode || !state.map || !state.polygonsLayer) return;

  const visibleZoneIds = new Set(
    state.geoJsonData.features
      .filter((feature: any) => feature.geometry?.type === 'Polygon' && feature.properties?.id != null)
      .map((feature: any) => String(feature.properties.id))
  );
  const targetZoneIds = zoneIds
    ? new Set(zoneIds.map(String).filter((zoneId) => visibleZoneIds.has(zoneId)))
    : visibleZoneIds;

  if (targetZoneIds.size === 0) return;

  const zonePolygons: Array<{ zoneId: string; polygon: any }> = [];
  state.polygonsLayer.eachLayer((layer: any) => {
    const zoneId = layer?.options?.id == null ? '' : String(layer.options.id);
    if (zoneId && targetZoneIds.has(zoneId) && typeof layer?.setStyle === 'function') {
      zonePolygons.push({ zoneId, polygon: layer });
    }
  });

  const flowDurationByZoneId = new Map<string, number>();
  await Promise.all(zonePolygons.map(async ({ zoneId, polygon }) => {
    const itemDetails = mapStore.getItemDetails(zoneId, 'zone');
    const zoneStatusResult = await applyZoneStatusStyle(polygon, itemDetails, state.map);
    if (zoneStatusResult) flowDurationByZoneId.set(zoneId, zoneStatusResult.flowDuration);
  }));

  if (flowDurationByZoneId.size === 0) return;

  state.polygonsLayer.eachLayer((layer: any) => {
    const duration = flowDurationByZoneId.get(String(layer?._zoneId ?? ''));
    if (!duration) return;

    const element = layer.getElement?.();
    if (element) element.style.setProperty('--flow-duration', `${duration}s`);
  });
}

let zoneStatusRefreshPromise: Promise<void> | null = null;

/**
 * Tải lại snapshot zoneStatus rồi chỉ cập nhật màu/tốc độ flow của các zone đang
 * hiển thị. Hàm được expose cho `trafficInfo` khi nhận subject `ta.its.vds.zone`.
 */
const refreshZoneStatus = (): Promise<void> => {
  if (zoneStatusRefreshPromise) return zoneStatusRefreshPromise;

  zoneStatusRefreshPromise = (async () => {
    try {
      await loadZoneStatus();
      emitZoneStatusChange();
      await refreshVisibleZoneStatusStyles();
    } catch (error) {
      console.error('LeafletMap.vue: refreshZoneStatus(...)', error);
    }
  })().finally(() => {
    zoneStatusRefreshPromise = null;
  });

  return zoneStatusRefreshPromise;
}

defineExpose({
  zoomIn,
  zoomOut,
  saveViewState,
  handleGetGeoJsonData,
  handleGetZoneListData,
  handleEquipmentListData,
  handleGetUnitListData,
  handleGetCurrentMode,
  handleGetGeoJsonDataFromLeaflet,
  resetViewToInitial,
  centerImage: centerImageMap,
  handleRefreshTrigger,
  refreshZoneStatus,
  rotateLeft,
  rotateRight,
  rotateReset,
  rotateByDelta,
  setBearing
})

// #endregion

const vdsEventDetail = ref<InstanceType<typeof VdsEventDetail>>();

// KienLT - 11/11/2026: sự kiện VDS trực tiếp (chưa có hồ sơ) đặt icon như sự cố — theo zone + lý trình (mép zone,
// đường nối, gom chồng) thay cho bám vào marker camera. Chế độ Block không vẽ marker cho thiết bị nằm trong card
// nên cách cũ làm sự kiện mất icon. Chỉ vẽ lại bản đồ khi tập icon thật sự đổi (thêm/hết hạn/đổi vị trí).
const { items: liveEvents } = useVdsEventSnapshot(() => (props.isEditMode || state.currentMode || !state.map || !state.mapId ? null : {}));
let liveEventFingerprint = '[]';
watch(
  [liveEvents, () => mapStore.zoneList, () => mapStore.eventTypeList],
  () => {
    const icons = toLiveEventIcons(liveEvents.value, mapStore.zoneList, mapStore.eventTypeList);
    const fingerprint = liveEventIconFingerprint(icons);
    // Giữ bản tin mới nhất cho lần bấm xem chi tiết dù không cần vẽ lại.
    mapStore.liveEventIconList = icons;
    if (fingerprint === liveEventFingerprint) return;
    liveEventFingerprint = fingerprint;
    if (props.isEditMode || state.currentMode || !state.map || !state.mapId) return;
    void queueRenderGeoJson();
  }
);
onUnmounted(() => { mapStore.liveEventIconList = []; });
</script>

<style scoped>
.text-dark{
  color: black
}

.map-container {
  position: relative;
  width: 100%;
  height: 100%;
  z-index: 0;
}

/** 
Css dùng để cho đổi màu nền
*/
.map-container.is-image-map #map {
  background: var(--image-map-bg, #d3d3d3);
}

.map-container.is-image-map :deep(.leaflet-container) {
  background: var(--image-map-bg, #d3d3d3) !important;
}

#map {
  width: 100%;
  height: 100%;
  min-height: 500px;
}


.finish-button-container {
  position: absolute;
  bottom: 10px;
  left: 10px;
  z-index: 1000;
  display: flex;
  gap: 10px;
}

.finish-button-container .el-button {
  display: flex;
  align-items: center;
  gap: 5px;
  padding: 12px 20px;
  font-weight: 600;
  box-shadow: 0 2px 12px 0 rgba(0,0,0,0.1);
}

.finish-button-container .el-icon {
  margin-right: 4px;
}

/* Thanh công cụ nổi cho annotation đang chọn (nhãn văn bản / hình ảnh) */
.annotation-toolbar {
  position: absolute;
  top: 10px;
  left: 50%;
  transform: translateX(-50%);
  z-index: 1000;
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 6px 10px;
  border: 1px solid var(--el-border-color-light);
  border-radius: 8px;
  background: var(--el-bg-color-overlay);
  box-shadow: var(--el-box-shadow-light);
}

.annotation-toolbar__count {
  margin-right: 4px;
  color: var(--el-text-color-regular);
  font-size: 12px;
  font-weight: 600;
  white-space: nowrap;
}

.annotation-toolbar .el-button {
  margin-left: 0;
}

.custom-stack-icon {
  position: relative;
  width: 50px;
  height: 50px;
}
.custom-stack-icon img {
  position: absolute;
  width: 40px;
  height: 40px;
}

.map-container :deep(.leaflet-marker-icon:hover) {
  z-index: 9999999 !important;
}

.map-container :deep(.transparent-tooltip) {
  background: transparent !important;
  border: none !important;
  box-shadow: none !important;
}

.map-container :deep(.transparent-tooltip::before) {
  display: none !important;
}

/* Bảng VMS: marker resize theo zoom (giống device/unit/incident) và không bị che */
.map-container :deep(.custom-div-icon) {
  background: transparent !important;
  border: none !important;
}

/* Tooltip hover cho VMS — hiển thị ảnh nội dung bảng ở kích thước lớn */
.map-container :deep(.incident-marker-icon),
.map-container :deep(.device-marker-icon),
.map-container :deep(.unit-marker-icon) {
   transition: transform 0.2s ease-out, background-color 0.2s ease, box-shadow 0.2s ease;
}

.map-container :deep(.incident-marker-icon:hover),
.map-container :deep(.device-marker-icon:hover),
.map-container :deep(.unit-marker-icon:hover) {
   transform: scale(calc(var(--incident-icon-scale, 1) * 1.5)) !important;
   background-color: rgba(255, 255, 255, 1) !important;
   box-shadow: 0 4px 12px rgba(0,0,0,0.6) !important;
   cursor: pointer;
}

/* Viền phân biệt icon SỰ CỐ với các icon thiết bị/đơn vị khác:
   vành đỏ (danger) + quầng sáng đỏ nhẹ → nổi bật, dễ nhận biết trên bản đồ.
   Đặt SAU rule hover chung để giữ nhận diện đỏ cả khi hover. */
.map-container :deep(.incident-marker-icon) {
   border: 2px solid var(--el-color-danger) !important;
   box-shadow:
      0 0 0 2px rgba(255, 255, 255, 0.9),
      0 0 10px 1px color-mix(in srgb, var(--el-color-danger) 65%, transparent) !important;
}

.map-container :deep(.incident-marker-icon:hover) {
   transform: scale(calc(var(--incident-icon-scale, 1) * 1.5)) !important;
   background-color: rgba(255, 255, 255, 1) !important;
   box-shadow:
      0 0 0 2px rgba(255, 255, 255, 1),
      0 0 16px 2px color-mix(in srgb, var(--el-color-danger) 80%, transparent) !important;
   cursor: pointer;
}

/* Chồng icon sự cố gần nhau (layoutIncidentStacks): các lớp so le dọc mép zone, không đánh số.
   Bóng đổ lệch giúp tách từng lớp; rê chuột vào lớp nào thì lớp đó nổi lên trên (riseOnHover). */
.map-container :deep(.incident-stack-member .incident-marker-icon:not(:hover)) {
   box-shadow:
      0 0 0 2px rgba(255, 255, 255, 0.95),
      -3px 3px 8px rgba(0, 0, 0, 0.35) !important;
}

/* Đoạn highlight đỏ trên centerline tại vị trí sự cố — nhấp nháy
   (keyframes định nghĩa ở khối <style> KHÔNG scoped bên dưới) */
.map-container :deep(.incident-segment-pulse) {
  animation: incident-segment-pulse 1.4s ease-in-out infinite;
}

/* Popup hover hiển thị hình ảnh nội dung bảng VMS ở kích thước lớn */
.map-container :deep(.vms-content-popup .leaflet-popup-content-wrapper) {
  padding: 4px;
  border-radius: 6px;
  background: #0c0f14;
  box-shadow: 0 4px 14px rgba(0, 0, 0, 0.45);
}

.map-container :deep(.vms-content-popup .leaflet-popup-content) {
  margin: 0;
  line-height: 0;
}

.map-container :deep(.vms-content-popup .leaflet-popup-tip) {
  background: #0c0f14;
}

.map-container :deep(.vms-content-tooltip img) {
  display: block;
  width: 260px;
  max-width: 40vw;
  height: auto;
  border-radius: 4px;
}

/* LCS: tooltip nhỏ lại so với VMS (đèn làn nội dung vuông, không cần rộng) */
.map-container :deep(.lcs-content-tooltip img) {
  width: 100px;
  max-width: 20vw;
}

/* Ẩn nền tròn của marker thường nhưng vẫn giữ nguyên icon và kích thước hiển thị. */
.map-container.is-icon-background-hidden :deep(.map-icon-background) {
  background-color: transparent !important;
  box-shadow: none !important;
}

/* ===== Zone polygon (dùng chung cho cấu hình & theo dõi giao thông) ===== */
/* Base: chuyển động mượt khi hover, tạo cảm giác sang, hiện đại */
.map-container :deep(.zone-polygon-fx) {
  transition: filter 0.3s ease, fill-opacity 0.3s ease, stroke-width 0.3s ease, stroke-opacity 0.3s ease;
  stroke-linecap: round;
  stroke-linejoin: round;
}

/* Màn hình cấu hình (mapSample): fill/stroke đã là gradient "kính" (xem defs SVG được
   tiêm ở helpers/mapViewUtils.ts::ensureZoneSvgDefs). CSS ở đây chỉ lo phần chiều sâu:
   quầng sáng phát ra ngoài (glow), phản chiếu sáng nhẹ trên mặt kính, và nhịp "thở". */
.map-container :deep(.zone-polygon-premium) {
  filter: drop-shadow(0 1px 0 rgba(255, 255, 255, 0.35)) drop-shadow(0 0 4px rgba(34, 211, 238, 0.55)) drop-shadow(0 0 10px rgba(34, 211, 238, 0.35));
  animation: zone-premium-breathe 3.2s ease-in-out infinite;
}

.map-container :deep(.zone-polygon-premium:hover) {
  filter: brightness(1.18) saturate(1.25) drop-shadow(0 1px 0 rgba(255, 255, 255, 0.5)) drop-shadow(0 0 12px rgba(34, 211, 238, 1)) drop-shadow(0 0 26px rgba(34, 211, 238, 0.7)) !important;
  stroke-width: 3.4 !important;
  stroke-opacity: 1 !important;
  animation: none;
  cursor: pointer;
}

/* Zone có thay đổi chưa lưu: viền vàng phát sáng + nét đứt chạy (fill đã là gradient hổ phách) */
.map-container :deep(.zone-polygon-premium.is-unsaved) {
  filter: drop-shadow(0 1px 0 rgba(255, 255, 255, 0.3)) drop-shadow(0 0 8px rgba(251, 191, 36, 0.9)) drop-shadow(0 0 18px rgba(251, 191, 36, 0.55)) !important;
  animation: zone-dash-flow 30s linear infinite;
}

/* Màn hình theo dõi giao thông — thiết kế "Route Ribbon": fill MỘT MÀU đồng đều theo màu
   trạng thái, hơi trong (xem applyZoneStatusStyle trong composables/useZoneStatus.ts).
   Viền KHÔNG nằm trên polygon này mà là lớp riêng .zone-outline (xem renderPolygon trong
   composables/useMapRender.ts) luôn vẽ trên nền mọi zone → 2 zone chồng mép không che viền
   nhau. Vì vậy stroke của polygon nền luôn trong suốt.
   Không dùng bóng lệch / hiệu ứng "thở": mép sắc nét, màu đúng cấu hình, nhẹ cho big-screen. */
.map-container :deep(.zone-polygon-status) {
  stroke-opacity: 0 !important;
  /* Màu trạng thái đậm, rực hơn một chút so với màu cấu hình gốc */
  filter: saturate(1.3) brightness(1.04);
  transition: filter 0.2s ease;
}

/* Hover: zone bừng sáng + quầng sáng mềm cùng màu trạng thái */
.map-container :deep(.zone-polygon-status:hover) {
  filter:
    saturate(1.45) brightness(1.14)
    drop-shadow(0 0 10px color-mix(in srgb, var(--zone-color, #22c55e) 75%, transparent));
  cursor: pointer;
}

/* Viền zone: cùng tông màu trạng thái nhưng đậm hơn → hài hoà với nền,
   vẫn tách rõ ranh giới các đoạn. --zone-color gán qua JS, cập nhật theo realtime. */
.map-container :deep(.zone-outline) {
  pointer-events: none;
  stroke: color-mix(in srgb, var(--zone-color, #22c55e) 50%, #0f172a);
  /* Độ dày cấu hình theo bản đồ (thiết lập "Độ dày viền zone"), gán ở phần tử gốc */
  stroke-width: var(--zone-border-width, 1.5px);
  stroke-opacity: 0.9;
  transition: stroke 0.15s ease, stroke-width 0.15s ease, filter 0.15s ease;
}

/* Hover zone: viền dày hơn, tông thẫm cùng màu + quầng sáng màu trạng thái
   → zone nổi khối rõ ràng, không bị gắt như viền trắng */
.map-container :deep(.zone-outline.is-hover) {
  stroke: color-mix(in srgb, var(--zone-color, #22c55e) 35%, #0f172a);
  stroke-width: calc(var(--zone-border-width, 1.5px) + 1.5px);
  stroke-opacity: 1;
  filter: drop-shadow(0 0 4px color-mix(in srgb, var(--zone-color, #22c55e) 90%, transparent));
}

/* ===== Flow-line: vệt "dòng xe chảy" dọc centerline, nối liền qua các zone cùng tuyến.
   Tốc độ chảy (--flow-duration, gán qua JS theo mức độ ùn tắc) là tín hiệu trực quan:
   chảy nhanh = thông thoáng, chảy chậm = ùn tắc — không cần đọc số liệu vẫn nhận biết được. ===== */
.map-container :deep(.zone-flow-line) {
  /* Viền tối mảnh quanh từng gạch → sắc nét, nổi trên nền zone với mọi màu đường
     (quầng trắng cũ làm gạch màu bị nhoè) */
  filter: drop-shadow(0 0 0.6px rgba(15, 23, 42, 0.9)) drop-shadow(0 1px 1px rgba(15, 23, 42, 0.35));
  animation: zone-flow-dash var(--flow-duration, 3s) linear infinite;
}

/* Tôn trọng người dùng bật giảm chuyển động: dừng mọi animation trang trí trên polygon */
@media (prefers-reduced-motion: reduce) {
  .map-container :deep(.zone-polygon-status),
  .map-container :deep(.zone-flow-line) {
    animation: none !important;
  }
}

/* =========================================================================
   ANNOTATION TỰ DO: nhãn văn bản (itemGroup='text'), hình ảnh (itemGroup='image')
   và khối HTML (itemGroup='html')

   Cấu trúc DOM (xem composables/useMapAnnotations.ts):
     .map-anno-wrap            ← element của divIcon (kích thước 0, chỉ làm mốc toạ độ)
       .map-anno               ← lớp bọc: căn tâm + transform rotate/scale
         .map-anno__body       ← phần nhìn thấy (chữ hoặc ảnh)
         .map-anno__frame      ← khung nét đứt khi đang chọn
         .map-anno__handle--*  ← tay cầm xoay / phóng to / resize theo chiều
   ========================================================================= */
.map-container :deep(.map-anno-wrap) {
  background: transparent !important;
  border: none !important;
  overflow: visible;
}

/* Lớp bọc: căn tâm phần tử vào đúng toạ độ điểm, rồi xoay/phóng quanh tâm.
   `translate` để riêng (không nhập vào transform) nên transform chỉ chứa rotate+scale
   do useMapAnnotations sinh ra theo cấu hình và mức zoom. */
.map-container :deep(.map-anno) {
  position: absolute;
  left: 0;
  top: 0;
  translate: -50% -50%;
  transform-origin: center center;
  width: max-content;
  /* Chế độ theo dõi: annotation chỉ để trang trí nên không nhận chuột, người dùng vẫn
     kéo/zoom được bản đồ khi con trỏ đi qua nó. */
  pointer-events: none;
  user-select: none;
}
.map-container :deep(.map-anno--editable) {
  pointer-events: auto;
  cursor: move;
}
.map-container :deep(.map-anno--editable:active) {
  cursor: grabbing;
}

/* Khung nét đứt bao quanh phần tử đang chọn */
.map-container :deep(.map-anno__frame) {
  position: absolute;
  inset: -4px;
  border: 1px dashed var(--el-color-primary);
  border-radius: 4px;
  pointer-events: none;
}
/* Phần tử thuộc cụm: khung đổi màu để nhận biết đang thao tác theo cả cụm */
.map-container :deep(.map-anno.is-grouped.is-selected .map-anno__frame) {
  border-color: var(--el-color-warning);
  border-style: solid;
}
/* Phần tử ĐANG NHẮM trong cụm: khung dày hơn để biết nút "Sửa" đang trỏ vào phần tử nào */
.map-container :deep(.map-anno.is-focused .map-anno__frame) {
  border-color: var(--el-color-primary);
  border-style: solid;
  border-width: 2px;
}

/* Tay cầm: --h là đường kính, các cạnh dùng calc(var(--h)/-2) nên tâm tay cầm luôn
   nằm đúng trên góc/cạnh của phần tử, kể cả khi tay cầm được phản-tỉ-lệ theo zoom. */
.map-container :deep(.map-anno__handle) {
  --h: 14px;
  position: absolute;
  width: var(--h);
  height: var(--h);
  border: 2px solid var(--el-bg-color);
  border-radius: 50%;
  background: var(--el-color-primary);
  box-shadow: 0 1px 4px rgba(0, 0, 0, 0.35);
  pointer-events: auto;
  z-index: 2;
}
.map-container :deep(.map-anno__handle:hover) {
  background: var(--el-color-primary-light-3);
}
/* Góc trên-trái: xoay */
.map-container :deep(.map-anno__handle--rotate) {
  left: calc(var(--h) / -2);
  top: calc(var(--h) / -2);
  background: var(--el-color-warning);
  cursor: grab;
}
/* Góc dưới-phải: phóng to/thu nhỏ đồng đều (cả cụm nếu đang chọn cụm) */
.map-container :deep(.map-anno__handle--scale) {
  right: calc(var(--h) / -2);
  bottom: calc(var(--h) / -2);
  cursor: nwse-resize;
}
/* Góc trên-phải: mở hộp thoại sửa nội dung của CHÍNH phần tử đang nhắm */
.map-container :deep(.map-anno__handle--edit) {
  --h: 18px;
  right: calc(var(--h) / -2);
  top: calc(var(--h) / -2);
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
}
.map-container :deep(.map-anno__handle--edit svg) {
  width: 10px;
  height: 10px;
  fill: var(--el-bg-color);
  pointer-events: none;
}
/* Giữa cạnh phải: chỉ đổi chiều rộng (ảnh, 1 phần tử) */
.map-container :deep(.map-anno__handle--width) {
  right: calc(var(--h) / -2);
  top: 50%;
  margin-top: calc(var(--h) / -2);
  cursor: ew-resize;
}
/* Giữa cạnh dưới: chỉ đổi chiều cao (ảnh, 1 phần tử) */
.map-container :deep(.map-anno__handle--height) {
  bottom: calc(var(--h) / -2);
  left: 50%;
  margin-left: calc(var(--h) / -2);
  cursor: ns-resize;
}

/* ===== Nhãn văn bản tự do (itemGroup='text') — chèn text lên bản đồ ===== */
.map-container :deep(.map-text-annotation) {
  white-space: nowrap;
  line-height: 1.2;
  letter-spacing: 0.2px;
  /* mặc định (shape none): chữ nổi rõ trên nền bản đồ bằng viền sáng quanh chữ */
  text-shadow: 0 0 3px #fff, 0 0 3px #fff, 1px 1px 2px rgba(0, 0, 0, 0.35);
  transition: filter 0.15s ease;
}
.map-container :deep(.map-anno--editable:hover .map-text-annotation) {
  filter: brightness(1.05) drop-shadow(0 0 6px rgba(0, 0, 0, 0.25));
}

/* Hình dạng: khung chữ nhật bo nhẹ */
.map-container :deep(.map-text-shape--rect) {
  padding: 3px 8px;
  border-radius: 4px;
  background: rgba(255, 255, 255, 0.92);
  border: 1px solid rgba(0, 0, 0, 0.15);
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.25);
  text-shadow: none;
}

/* Hình dạng: viên thuốc (pill) bo tròn */
.map-container :deep(.map-text-shape--pill) {
  padding: 3px 12px;
  border-radius: 999px;
  background: rgba(255, 255, 255, 0.92);
  border: 1px solid rgba(0, 0, 0, 0.15);
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.25);
  text-shadow: none;
}

/* Hình dạng: chỉ viền (outline) nền trong suốt */
.map-container :deep(.map-text-shape--outline) {
  padding: 3px 8px;
  border-radius: 4px;
  background: transparent;
  border: 1.5px solid currentColor;
  text-shadow: 0 0 3px #fff, 0 0 3px #fff;
}

/* Hình dạng: gạch chân */
.map-container :deep(.map-text-shape--underline) {
  padding-bottom: 2px;
  border-bottom: 2px solid currentColor;
}

/* Khi có màu nền mà hình dạng là "không khung"/"gạch chân": thêm chút đệm + bo góc để
   nền không dính sát chữ và bỏ viền sáng quanh chữ cho gọn. */
.map-container :deep(.map-text-shape--none.has-bg),
.map-container :deep(.map-text-shape--underline.has-bg) {
  padding: 2px 8px;
  border-radius: 4px;
  text-shadow: none;
}

/* Chấm báo chưa lưu */
.map-container :deep(.map-text-unsaved-dot) {
  display: inline-block;
  width: 6px;
  height: 6px;
  margin-left: 4px;
  vertical-align: middle;
  border-radius: 50%;
  background: #fbbf24;
  box-shadow: 0 0 6px #fbbf24;
}

/* ===== Hình ảnh tự do (itemGroup='image') — chèn ảnh lên bản đồ ===== */
.map-container :deep(.map-image-annotation) {
  width: 100%;
  height: 100%;
  line-height: 0;
  transition: filter 0.15s ease;
}

.map-container :deep(.map-anno--editable:hover .map-image-annotation) {
  filter: brightness(1.05) drop-shadow(0 0 6px rgba(0, 0, 0, 0.35));
}

.map-container :deep(.map-image-annotation img) {
  display: block;
  width: 100%;
  height: auto;
  pointer-events: none;
}

/* Ảnh có thay đổi chưa lưu: viền vàng nét đứt quanh ảnh */
.map-container :deep(.map-anno--image.is-unsaved .map-image-annotation) {
  outline: 2px dashed #fbbf24;
  outline-offset: 2px;
}

.map-container :deep(.map-image-unsaved-dot) {
  position: absolute;
  right: -4px;
  top: -4px;
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: #fbbf24;
  box-shadow: 0 0 6px #fbbf24;
}

/* ===== Khối HTML tự do (itemGroup='html') — chữ + hình soạn bằng Jodit ===== */
.map-container :deep(.map-html-annotation) {
  width: 100%;
  white-space: normal;
  word-break: break-word;
  line-height: 1.35;
  color: #1f2d3d;
  transition: filter 0.15s ease;
}

/* Nội dung chỉ để hiển thị: không nhận chuột (link không điều hướng khi bấm để kéo khối) */
.map-container :deep(.map-html-annotation *) {
  pointer-events: none;
}

.map-container :deep(.map-html-annotation p) {
  margin: 0 0 4px;
}

.map-container :deep(.map-html-annotation img) {
  max-width: 100%;
  height: auto;
  vertical-align: middle;
}

/* theme reset `* { padding: 0 }` làm mất lề danh sách → trả lại như trong trình soạn thảo */
.map-container :deep(.map-html-annotation ul),
.map-container :deep(.map-html-annotation ol) {
  padding-left: 24px;
}

.map-container :deep(.map-html-annotation table) {
  border-collapse: collapse;
}

.map-container :deep(.map-html-annotation td),
.map-container :deep(.map-html-annotation th) {
  padding: 2px 6px;
  border: 1px solid rgba(0, 0, 0, 0.2);
}

.map-container :deep(.map-anno--editable:hover .map-html-annotation) {
  filter: brightness(1.05) drop-shadow(0 0 6px rgba(0, 0, 0, 0.25));
}

.map-container :deep(.map-anno--html.is-unsaved .map-html-annotation) {
  outline: 2px dashed #fbbf24;
  outline-offset: 2px;
}

/* ===== Mũi tên hướng Km (màn cấu hình) — glow nhẹ, không lấn màu polygon ===== */
.map-container :deep(.zone-km-arrow-line) {
  filter: drop-shadow(0 0 1.5px rgba(99, 102, 241, 0.55));
}

/* ===== Mũi tên CHIỀU DI CHUYỂN (flow) — tông hổ phách, glow nhẹ để phân biệt ===== */
.map-container :deep(.zone-flow-arrow-line) {
  filter: drop-shadow(0 0 1.5px rgba(245, 158, 11, 0.6));
}
.map-container :deep(.zone-flow-arrow-head) {
  filter: drop-shadow(0 0 2.5px rgba(245, 158, 11, 0.75));
}

.map-container :deep(.zone-km-arrow-head) {
  filter: drop-shadow(0 0 2.5px rgba(99, 102, 241, 0.7));
}

/* ===== Nhãn tên zone kiểu "glass chip" ===== */
.map-container :deep(.zone-label-premium) {
  background: transparent !important;
  border: none !important;
}

.map-container :deep(.zone-label-chip) {
  position: absolute;
  left: 0;
  top: 0;
  transform: translate(-50%, -50%);
  display: inline-flex;
  align-items: center;
  gap: 6px;
  white-space: nowrap;
  font-size: 11px;
  font-weight: 700;
  letter-spacing: 0.4px;
  color: #f0f9ff;
  padding: 3px 12px;
  border-radius: 999px;
  background: linear-gradient(135deg, rgba(8, 145, 178, 0.92), rgba(12, 74, 110, 0.85));
  border: 1px solid rgba(103, 232, 249, 0.75);
  box-shadow:
    0 2px 12px rgba(6, 30, 60, 0.5),
    0 0 12px color-mix(in srgb, #22d3ee 55%, transparent),
    inset 0 0 0 1px rgba(255, 255, 255, 0.1);
  backdrop-filter: blur(6px);
  -webkit-backdrop-filter: blur(6px);
  text-shadow: 0 1px 2px rgba(0, 0, 0, 0.45);
  pointer-events: none;
}

/*
 * Phần tốc độ min–max trong nhãn zone: "viên" màu hổ phách riêng để tách khỏi tên zone (nền xanh).
 * Riêng zone = đặc; kế thừa mặc định hướng = nhạt + in nghiêng; thiếu/Min > Max = đỏ; đang sửa = viền nét đứt.
 */
.map-container :deep(.zone-label-speed) {
  padding: 1px 8px;
  border-radius: 999px;
  border: 1px solid var(--el-color-warning-light-3);
  background: var(--el-color-warning);
  color: var(--el-text-color-primary);
  font-weight: 700;
  font-variant-numeric: tabular-nums;
  letter-spacing: 0;
  text-shadow: none;
}

.map-container :deep(.zone-label-chip--with-speed) {
  padding-right: 4px;
}

.map-container :deep(.zone-label-speed.is-inherited) {
  border-style: dashed;
  border-color: var(--el-color-warning);
  background: var(--el-color-warning-light-8);
  font-style: italic;
  font-weight: 600;
}

.map-container :deep(.zone-label-speed.is-invalid) {
  border-color: var(--el-color-danger-light-3);
  background: var(--el-color-danger);
  color: var(--el-color-white);
}

.map-container :deep(.zone-label-speed.is-dirty) {
  outline: 2px dashed var(--el-color-white);
  outline-offset: 1px;
}

/* Chấm báo có thay đổi chưa lưu (thay cho dấu *) */
.map-container :deep(.zone-label-dot) {
  width: 7px;
  height: 7px;
  border-radius: 50%;
  background: #fbbf24;
  box-shadow: 0 0 6px #fbbf24;
  animation: zone-label-dot-pulse 1.2s ease-in-out infinite;
}

</style>

<!-- Keyframes để KHÔNG scoped: các animation này áp lên phần tử SVG do Leaflet tạo ở
     runtime (qua :deep). Nếu để trong <style scoped>, Vue sẽ băm tên @keyframes theo
     scope-hash và việc rewrite tên bị lệch giữa Vite dev (CSS chèn qua JS) và bản build
     (CSS tách file) -> dev không chạy hiệu ứng còn build lại chạy. Đưa @keyframes ra khối
     KHÔNG scoped (global, không băm tên) để dev và build hoạt động giống nhau. -->
<style>
@keyframes incident-segment-pulse {
  0%, 100% { opacity: 0.85; }
  50%      { opacity: 0.25; }
}

@keyframes zone-premium-breathe {
  0%, 100% {
    filter: drop-shadow(0 1px 0 rgba(255, 255, 255, 0.3)) drop-shadow(0 0 4px rgba(34, 211, 238, 0.5)) drop-shadow(0 0 9px rgba(34, 211, 238, 0.28));
  }
  50% {
    filter: drop-shadow(0 1px 0 rgba(255, 255, 255, 0.45)) drop-shadow(0 0 7px rgba(34, 211, 238, 0.9)) drop-shadow(0 0 18px rgba(34, 211, 238, 0.55));
  }
}

@keyframes zone-flow-dash {
  to {
    stroke-dashoffset: -30;
  }
}

@keyframes zone-dash-flow {
  to {
    stroke-dashoffset: -1000;
  }
}

@keyframes zone-label-dot-pulse {
  0%, 100% { opacity: 1; }
  50%      { opacity: 0.35; }
}
</style>
