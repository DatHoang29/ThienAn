<template>
  <WorkspacePage>
  <div class="map-sample-page">    

    <MapControl
      @refresh="handleRefresh"
      @zoom-in="handleZoomIn"
      @zoom-out="handleZoomOut"
      @map-change="handleMapChange"
      @filter-change="handleFilterChange"
      :geo-json-data="state.geoJsonDataFromLeaflet"
      :selected-map-id="selectedMapId"
      :bearing="state.mapBearing"
      @reset-view="handleResetMapView"
      @center-image="handleCenterImage"
      @rotate-left="handleRotateLeft"
      @rotate-right="handleRotateRight"
      @rotate-reset="handleRotateReset"
      @rotate-by-delta="handleRotateByDelta"
      @set-bearing="handleSetBearing"
    />
    
    <LeafletMap
      ref="mapRef"
      :is-edit-mode="true"
      :selected-map-id="selectedMapId"
      :show-location-editor="true"
      :filters="activeFilters"
      @return-geo-json-data="handleGetGeoJsonDataFromLeaflet"
      @bearing-change="handleBearingChange"
      @refresh="handleRefresh"
    />
  </div>
  </WorkspacePage>
</template>

<script setup lang="ts">
import { ref, onMounted, onActivated, reactive } from 'vue'
import { getAPI } from '/@/utils/axios-utils'
import { TmsMapApi } from '/@/api-services/tms'
import LeafletMap from './components/LeafletMap.vue'
import MapControl from './components/MapControl.vue'
import { useMapStore } from '/@/stores/its/mapStore'
import WorkspacePage from '/@/components/workspace/WorkspacePage.vue'

const mapRef = ref<InstanceType<typeof LeafletMap> | null>(null)
const selectedMapId = ref<string | undefined>(undefined)
const activeFilters = ref({})
const mapStore = useMapStore()

const state = reactive({
  geoJsonDataFromLeaflet:[] as any,
  /** Góc xoay hiện tại của bản đồ (độ), đồng bộ từ LeafletMap để vẽ kim la bàn. */
  mapBearing: 0 as number
})

const loadDefaultMap = async () => {
  try {
    const response = await getAPI(TmsMapApi).apiTmsTmsmapListGet()
    const mapList = response?.data?.result || []
    //console.log("loadDefaultMap - mapList ", mapList)
    const defaultMap = mapList.find(map => map.isDefault === 1)
    //console.log("defaultMap - mapList ", defaultMap)
    if (defaultMap && typeof defaultMap.id === 'string') {
      selectedMapId.value = defaultMap.id     
    }
   
  } catch (error) {
    console.error('Error loading default map:', error)
  }
}

const handleGetGeoJsonDataFromLeaflet = (value:any) => {
  if(value){
    state.geoJsonDataFromLeaflet = [...value];   
    //console.log("handleGetGeoJsonDataFromLeaflet - state.geoJsonDataFromLeaflet", state.geoJsonDataFromLeaflet)
  } 
}


const handleZoomIn = () => {
  mapRef.value?.zoomIn()
}

const handleZoomOut = () => {
  mapRef.value?.zoomOut()
}

/**
 * Thay đổi map thì selectedMapId thay đổi theo
 */
const handleMapChange = async (mapId: string | undefined) => {
  console.log("handleMapChange - mapId", mapId)
  // Save current view state before changing maps
  if (mapRef.value && selectedMapId.value) {
    const viewState = mapRef.value.saveViewState()
    // You can save this view state to your backend if needed
  }  
  selectedMapId.value = mapId

  // Sau khi thay đổi map, reset lại filter
  activeFilters.value = {}
}

const handleRefresh = () => {
  if (selectedMapId.value) {
    const currentId = selectedMapId.value
    selectedMapId.value = undefined
    setTimeout(() => {
      selectedMapId.value = currentId
    }, 100)
  }
}

const handleFilterChange = (filters: any) => {
  const currentMode = mapRef.value?.handleGetCurrentMode()
  console.log("handleFilterChange - currentMode", currentMode)

  // Nếu đang trong chế độ edit hoặc move thì không cho filer
  //const currentMode = mapRef.value?.handleGetCurrentMode()
 // console.log("handleFilterChange - currentMode",currentMode)
  // if(currentMode  && 
  //   (currentMode== 'edit' || currentMode == 'move'))
  // {
  //   return;    
  // }
  activeFilters.value = filters
}

/**
 * Reset map về vị trí ban đầu mà không gọi lại API.
 * Chỉ tác động viewport của LeafletMap.
 */
const handleResetMapView = () => {
  mapRef.value?.resetViewToInitial?.();
};

const handleCenterImage = () => {
  mapRef.value?.centerImage?.();
};

const handleRotateLeft = () => {
  mapRef.value?.rotateLeft?.();
};

const handleRotateRight = () => {
  mapRef.value?.rotateRight?.();
};

const handleRotateReset = () => {
  mapRef.value?.rotateReset?.();
};

/** Kéo ngang trên la bàn: xoay tự do theo con trỏ. */
const handleRotateByDelta = (delta: number) => {
  mapRef.value?.rotateByDelta?.(delta);
};

/** Bấm la bàn: xoay bản đồ về hướng tuyệt đối (0 = Bắc, 180 = Nam). */
const handleSetBearing = (bearing: number) => {
  mapRef.value?.setBearing?.(bearing);
};

const handleBearingChange = (bearing: number) => {
  state.mapBearing = bearing;
};

onMounted(async () => {
  loadDefaultMap()
})

// Quay lại tab/menu (KeepAlive): khung bản đồ có thể đã đổi kích thước khi đang ẩn
// ⇒ phát sự kiện resize để Leaflet tính lại kích thước (Leaflet tự lắng nghe window resize).
// onActivated cũng chạy ngay sau lần mount đầu, khi đó bản đồ vừa dựng nên bỏ qua.
let activated = false
onActivated(() => {
  if (!activated) {
    activated = true
    return
  }
  window.dispatchEvent(new Event('resize'))
})
</script>

<style scoped>
.map-sample-page {
  position: relative;
  width: 100%;
  /* Lấp đầy vùng nội dung của WorkspacePage (menu riêng hoặc tab) thay cho calc(100vh) bị tràn khi nằm trong tab */
  flex: 1;
  min-height: 0;
  box-sizing: border-box;
  /* Lề quanh bản đồ để khung viền tách bản đồ khỏi phần còn lại của trang */
  padding: 8px;
  background: var(--el-bg-color-page);
}

/* Khung bản đồ: viền + bo góc; các panel nổi (công cụ, cấu hình) nằm gọn bên trong */
.map-sample-page > :deep(.map-container) {
  border: 1px solid var(--el-border-color);
  border-radius: 12px;
  overflow: hidden;
  box-shadow: 0 2px 12px rgba(15, 23, 42, 0.08);
}

.el-button {
  margin-left: 0px!important;
}
</style>
