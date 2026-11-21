<template>
  <div class="map-controls">
    <!-- Khung 1: chọn bản đồ + bộ lọc đối tượng -->
    <div class="map-card map-card--select">
      <!-- Chọn bản đồ: dùng chung panel MapTypePanel với màn trafficInfo -->
      <el-popover
        ref="mapTypePopoverRef"
        placement="bottom-start"
        :width="330"
        :offset="8"
        :show-arrow="false"
        trigger="click"
        popper-class="map-type-popover"
        @show="mapTypePanelRef?.reveal()"
      >
        <template #reference>
          <button type="button" class="map-picker" :title="$t('lz.button.tmsMapLocation.changeMap')">
            <span class="map-picker__icon" :class="selectedMap?.type === 'image' ? 'is-image' : 'is-tile'">
              <el-icon>
                <ele-Picture v-if="selectedMap?.type === 'image'" />
                <ele-MapLocation v-else />
              </el-icon>
            </span>
            <span class="map-picker__text">
              <span class="map-picker__name">{{ selectedMap?.name || $t('lz.button.tmsMapLocation.changeMap') }}</span>
              <span v-if="selectedMap" class="map-picker__meta">
                {{ selectedMap.type === 'image' ? $t('lz.label.tmsMapLocation.imageMap') : $t('lz.label.tmsMapLocation.tileMap') }}
                <span v-if="Number(selectedMap.isDefault) === 1" class="map-option__tag">{{ $t('lz.label.tmsMapLocation.defaultMapTag') }}</span>
              </span>
            </span>
            <el-icon class="map-picker__arrow"><ele-ArrowDown /></el-icon>
          </button>
        </template>

        <MapTypePanel ref="mapTypePanelRef" :items="mapTypeItems" :model-value="queryParams.mapId" @select="onSelectMap" />
      </el-popover>

      <span class="map-card__divider map-card__divider--vertical"></span>

      <!-- Filter Dropdown -->
      <el-popover
        placement="right-start"
        :width="340"
        :offset="10"
        :show-arrow="false"
        trigger="click"
        popper-class="map-filter-popover"
      >
        <template #reference>
          <div>
            <el-tooltip :content="$t('lz.button.tmsMapLocation.filterOnMap')" placement="right">
              <el-button icon="ele-Filter" circle class="map-tool map-tool--accent"></el-button>
            </el-tooltip>
          </div>
        </template>

        <MapFilterPanel
          :groups="state.listFilter"
          @group-change="onGroupToggle"
          @item-change="onItemToggle"
          @check-all="handleCheckAll"
          @uncheck-all="handleUncheckAll"
        />
      </el-popover>
    </div>

    <!-- Khung 2: công cụ điều khiển bản đồ, chia nhóm bằng vạch ngăn -->
    <div class="map-card map-card--tools">
      <!-- Nhóm phóng to / thu nhỏ -->
      <el-tooltip :content="$t('lz.button.tmsMapLocation.zoomIn')" placement="right">
        <el-button @click="$emit('zoom-in')" icon="ele-ZoomIn" circle class="map-tool"></el-button>
      </el-tooltip>
      <el-tooltip :content="$t('lz.button.tmsMapLocation.zoomOut')" placement="right">
        <el-button @click="$emit('zoom-out')" icon="ele-ZoomOut" circle class="map-tool"></el-button>
      </el-tooltip>

      <span class="map-card__divider"></span>

      <!-- Nhóm làm mới / về khung nhìn ban đầu -->
      <el-tooltip :content="$t('lz.button.base.refresh')" placement="right">
        <el-button @click="$emit('refresh')" icon="ele-Refresh" circle class="map-tool"></el-button>
      </el-tooltip>
      <el-tooltip :content="$t('lz.button.base.refreshInitial')" placement="right">
        <el-button @click="$emit('reset-view')" icon="ele-Position" circle class="map-tool"></el-button>
      </el-tooltip>
      <el-tooltip v-if="isSelectedImageMap" :content="$t('lz.button.tmsMapLocation.centerImage')" placement="right">
        <el-button @click="$emit('center-image')" icon="ele-Position" circle class="map-tool"></el-button>
      </el-tooltip>

      <span class="map-card__divider"></span>

      <!-- Nhóm xoay bản đồ -->
      <el-tooltip :content="$t('lz.button.tmsMapLocation.rotateLeft')" placement="right">
        <el-button @click="$emit('rotate-left')" circle class="map-tool">
          <el-icon><ele-RefreshLeft /></el-icon>
        </el-button>
      </el-tooltip>
      <el-tooltip :content="$t('lz.button.tmsMapLocation.rotateRight')" placement="right">
        <el-button @click="$emit('rotate-right')" circle class="map-tool">
          <el-icon><ele-RefreshRight /></el-icon>
        </el-button>
      </el-tooltip>

      <!-- La bàn: bấm đảo Bắc ⇄ Nam, kéo ngang xoay tự do, chuột phải về góc cấu hình -->
      <MapCompassControl
        :bearing="props.bearing"
        tooltip-placement="right"
        @set-bearing="(value: number) => $emit('set-bearing', value)"
        @rotate-by-delta="(value: number) => $emit('rotate-by-delta', value)"
        @reset="$emit('rotate-reset')"
      />

      <!-- Về đúng góc xoay đã cấu hình cho bản đồ -->
      <el-tooltip :content="$t('lz.button.tmsMapLocation.rotateReset')" placement="right">
        <el-button @click="$emit('rotate-reset')" circle class="map-tool">
          <el-icon><ele-Compass /></el-icon>
        </el-button>
      </el-tooltip>

    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted, onActivated, watch } from 'vue'
import { useMapStore } from '/@/stores/its/mapStore'
import MapCompassControl from './MapCompassControl.vue'
import MapFilterPanel from './MapFilterPanel.vue'
import MapTypePanel from './MapTypePanel.vue'
import { useI18n } from 'vue-i18n'

interface Item {
  label: string;
  value: string;
  checked: boolean;
}

interface ItemGroup {
  label: string;
  value: string;
  checked: boolean;
  indeterminate: boolean;
  children: Item[];
}

const emit = defineEmits(
  ['refresh', 'zoom-in', 'zoom-out', 'map-change', 'filter-change',
  'get-geo-json-data', 'get-current-mode',  'reset-view', 'center-image',
  'rotate-left', 'rotate-right', 'rotate-reset', 'rotate-by-delta', 'set-bearing']
)

const mapStore = useMapStore()
const { t } = useI18n()

const queryParams = reactive({
  mapId: undefined,
})

const props = defineProps({
  geoJsonData: [] as any,
  selectedMapId: '' as any,
  /** Góc xoay hiện tại của bản đồ (độ) — dùng để vẽ kim la bàn. */
  bearing: {
    type: Number,
    default: 0
  }
})

const state = reactive({
  itemGroup: [] as any,
  itemList: [] as any,
  equipmentTypeList: [] as any,
  equipmentList: [] as any,
  zoneList: [] as any,
  zoneTypeList: [] as any,
  workUnitTypeList: [] as any,
  workUnitList: [] as any,
  eventTypeList: [] as any,
  geoDataJson:[] as any,
  listFilter:[] as any,
  mapList: [] as any,
})

const isSelectedImageMap = computed(() => {
  const mapId = queryParams.mapId ?? props.selectedMapId;
  return state.mapList.some((item: any) => String(item.id) === String(mapId) && item.type === 'image');
});

// Panel báo bật/tắt cả nhóm
const onGroupToggle = (group: any, checked: boolean) => {
  group.checked = checked
  handleGroupChange(group)
}

// Panel báo bật/tắt một mục con
const onItemToggle = (group: any, item: any, checked: boolean) => {
  item.checked = checked
  handleItemChange(group)
}

let itemGroups : ItemGroup[] = [];

const handleGroupChange = (group: any) => {
  if (group.children && group.children.length > 0) {
    group.children.forEach((item: any) => {
      item.checked = group.checked
    })
  }
  group.indeterminate = false
  emitFilterChange()
}

const handleItemChange = (group: any) => {
  const checkedCount = group.children.filter((item: any) => item.checked).length
  group.checked = checkedCount === group.children.length
  group.indeterminate = checkedCount > 0 && checkedCount < group.children.length
  emitFilterChange()
}

// groupValues: chỉ áp dụng cho các nhóm của tab đang mở trên panel
const handleCheckAll = (groupValues?: string[]) => {
  state.listFilter.forEach((group: any) => {
    if (groupValues && !groupValues.includes(group.value)) return
    group.checked = true
    group.indeterminate = false
    if (group.children && group.children.length > 0) {
      group.children.forEach((item: any) => {
        item.checked = true
      })
    }
  })
  emitFilterChange()
}

const handleUncheckAll = (groupValues?: string[]) => {
  state.listFilter.forEach((group: any) => {
    if (groupValues && !groupValues.includes(group.value)) return
    group.checked = false
    group.indeterminate = false
    if (group.children && group.children.length > 0) {
      group.children.forEach((item: any) => {
        item.checked = false
      })
    }
  })
  emitFilterChange()
}

const emitFilterChange = () => {
  const filters = state.listFilter.reduce((acc: any, element: any) => {
    acc[element?.value] = (element?.children || [])
      .filter((item: any) => item.checked)
      .map((item: any) => item.value)
    return acc
  }, {})

  emit('filter-change', filters)
}

const fetchMapList = async () => {
  try {
    state.mapList = await mapStore.getMapList();
  } catch (error) {
    console.error('Error fetching map list:', error)
  }
}

const handleMapChange = () => {
  emit('map-change', queryParams.mapId)
}

/* ===== Chọn bản đồ qua MapTypePanel (giống màn trafficInfo) ===== */
const mapTypePopoverRef = ref()
const mapTypePanelRef = ref<InstanceType<typeof MapTypePanel>>()

/** Danh sách bản đồ theo định dạng của MapTypePanel */
const mapTypeItems = computed(() =>
  state.mapList.map((item: any) => ({
    value: item.id,
    label: item.name,
    type: item.type,
    isDefault: item.isDefault,
  }))
)

/** Bản đồ đang chọn: hiển thị tên + loại trên nút mở panel */
const selectedMap = computed(() =>
  state.mapList.find((item: any) => queryParams.mapId != null && String(item.id) === String(queryParams.mapId))
)

const onSelectMap = (mapId: any) => {
  if (String(mapId) !== String(queryParams.mapId)) {
    queryParams.mapId = mapId
    handleMapChange()
  }
  mapTypePopoverRef.value?.hide?.()
}

const transformData = (listItemGroup:any, listEquipmentType:any, listZone:any, listWorkUnit: any, eventTypeList: any) => {
  listItemGroup?.forEach((item: any) => {
    switch(item?.code){
      case 'device':
        const objItemGroup:ItemGroup = {
          label : item?.name,
          value : item?.code,
          checked: true,
          indeterminate: true,
          children: []
        }
        listEquipmentType.forEach((equipment: any) => {
            const objTemp = {
              label: equipment?.name,
              value: equipment?.id,
              checked: true,
              icon: equipment?.defaultIcon || equipment?.activeIcon || ''
            };
            objItemGroup.children.push(objTemp);
        })
       itemGroups.push(objItemGroup)
        break;
      case 'zone':
        // Khu vực (zone) luôn hiển thị: không đưa vào bộ lọc vì icon sự kiện
        // được vẽ theo zone, ẩn zone sẽ làm mất luôn sự kiện.
        break;
      case 'unit':
        const objItemGroup_3:ItemGroup = {
          label :  item?.name,
          value : item?.code,
          checked: true,
          indeterminate: true,
          children: []
        }
        listWorkUnit.forEach((equipment: any) => {
            const objTemp = {
              label: equipment?.name,
              value: equipment?.id,
              checked: true,
              unitType: equipment?.type
            };
            objItemGroup_3.children.push(objTemp);
        })
        itemGroups.push(objItemGroup_3)
        break;
    }
  })

  // Them filter su kien su co
  const objItemGroupEvent:ItemGroup = {
    label : t('lz.label.tmsMapLocation.event'),
    value : 'event',
    checked: true,
    indeterminate: true,
    children: []
  }
  eventTypeList.forEach((eventType: any) => {
    objItemGroupEvent.children.push({
      label: eventType?.name,
      value: eventType?.id,
      checked: true,
      icon: eventType?.url || ''
    });
  })
  itemGroups.push(objItemGroupEvent)

  state.listFilter = [...itemGroups];
}

function UpdateItemGroup(listGeoJsonData: any){
  if(listGeoJsonData && listGeoJsonData.length > 0)
  {
    const copyListGeoJsonDat = JSON.parse(JSON.stringify(listGeoJsonData))
    const listItemGroupInListGeoJsonData = copyListGeoJsonDat.map((geoDataJson:any) =>
    {
      if(geoDataJson?.properties?.itemGroup !== undefined){
        return geoDataJson?.properties?.itemGroup
      }
    })

    const listItemGroupInListGeoJsonDataUnique = [...new Set(listItemGroupInListGeoJsonData)]

    const listDevice = listGeoJsonData.map((geoDataJson:any) =>
    {
      if(geoDataJson?.properties?.itemGroup == 'device'){
        return geoDataJson?.properties?.id
      }
    })
    .filter((item:any) => item != undefined)

    const listUnit = listGeoJsonData.map((geoDataJson:any) =>
    {
      if(geoDataJson?.properties?.itemGroup == 'unit'){
         return geoDataJson?.properties?.id
      }
    })
    .filter((item:any) => item != undefined)

    const listEuipmentTypeId = [] as any
    mapStore.equipmentList.forEach((equipment) =>
    {
      if(listDevice.includes(equipment?.id) && !listEuipmentTypeId.includes(equipment?.equipmentTypeId))
      {
        listEuipmentTypeId.push(equipment?.equipmentTypeId)
      }
    })

    const listitemGroupsCopy = JSON.parse(JSON.stringify(itemGroups))

    const listFilterUpdate = listitemGroupsCopy.map((item:any) =>
    {
       if(item.value == 'device' && listEuipmentTypeId.length > 0  && item.children.length > 0){
         item.children = item.children.filter((itemChildren:any) => {
           if(listEuipmentTypeId.includes(itemChildren?.value)){
               return itemChildren
             }
         })
         return item
       }
       if(item.value == 'unit' && listUnit.length > 0  && item.children.length > 0){
         item.children = item.children.filter((itemChildren:any) => {
             if(listUnit.includes(itemChildren?.value)){
               return itemChildren
             }
         })
         return item
       }
       if(item.value == 'zone' && listItemGroupInListGeoJsonDataUnique.includes(item.value)){
         return item
       }
       
    }).filter((item: any) => item!=undefined)
    state.listFilter = [...listFilterUpdate]
  }
  else{
    state.listFilter = []
  }
}

async function loadData() {
    await mapStore.fetchItemGroupList();
    state.itemGroup = mapStore.itemGroupList;

    await mapStore.fetchEquipmentTypeList();
    state.equipmentTypeList = mapStore.equipmentTypeList;

    await mapStore.fetchWorkUnitList();
    state.workUnitList = mapStore.workUnitList;

    await mapStore.fetchEventTypeData();
    state.eventTypeList = mapStore.eventTypeList;

    transformData(state.itemGroup,  state.equipmentTypeList, state.zoneList, state.workUnitList, state.eventTypeList);
}

watch(() => props.geoJsonData,(newVal, oldValue)=>{
  if(newVal != oldValue){
    UpdateItemGroup(newVal)
  }
})

onMounted(async () => {
  await loadData()
  await fetchMapList()
  queryParams.mapId = props.selectedMapId
  emitFilterChange()
})

// Quay lại tab/menu (KeepAlive): bản đồ có thể vừa được thêm/sửa ở trang Bản đồ (tms/map),
// mapStore lại giữ cache danh sách ⇒ tải lại từ API. Lần kích hoạt đầu ngay sau mount nên bỏ qua.
let activated = false
onActivated(async () => {
  if (!activated) {
    activated = true
    return
  }
  try {
    await mapStore.fetchMapList()
    state.mapList = mapStore.mapList
  } catch (error) {
    console.error('Error fetching map list:', error)
  }
})

watch(
  () => props.selectedMapId,
  (newVal) => {
    queryParams.mapId = newVal
  },
  { immediate: true }
)

</script>

<style scoped lang="scss">
.map-controls {
  position: absolute;
  top: 16px;
  left: 16px;
  z-index: 1000;
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 10px;
  /* Vùng trống giữa các khung KHÔNG chặn thao tác (drag/pan) trên bản đồ */
  pointer-events: none;
}

/* ===== Khung nổi trên bản đồ: viền + nền mờ để tách rõ khỏi nền bản đồ ===== */
.map-card {
  opacity: 0.55;
  transition: opacity 0.2s ease;

  &:hover,
  &:focus-within {
    opacity: 1;
  }

  @media (hover: none) {
    opacity: 1;
  }

  @media (prefers-reduced-motion: reduce) {
    transition: none;
  }

  pointer-events: auto;
  display: flex;
  align-items: center;
  padding: 5px;
  border: 1px solid var(--el-border-color-light);
  border-radius: 12px;
  background: color-mix(in srgb, var(--el-bg-color-overlay) 92%, transparent);
  backdrop-filter: blur(6px);
  box-shadow: 0 6px 18px rgba(15, 23, 42, 0.14), 0 1px 3px rgba(15, 23, 42, 0.08);
}

/* Khung chọn bản đồ: vạch màu chủ đạo bên trái làm điểm nhấn */
.map-card--select {
  gap: 6px;
  padding-left: 8px;
  border-left: 3px solid var(--el-color-primary);
}

/* Khung công cụ: xếp dọc */
.map-card--tools {
  flex-direction: column;
  gap: 4px;
}

.map-card__divider {
  display: block;
  flex-shrink: 0;
  width: 22px;
  height: 1px;
  margin: 2px 0;
  background: var(--el-border-color);
}

.map-card__divider--vertical {
  width: 1px;
  height: 22px;
  margin: 0 2px;
}

/* ===== Nút mở panel chọn bản đồ: icon + tên + loại bản đồ ===== */
.map-picker {
  display: flex;
  align-items: center;
  gap: 10px;
  width: 220px;
  padding: 4px 8px 4px 4px;
  border: 1px solid transparent;
  border-radius: 8px;
  background: transparent;
  color: var(--el-text-color-primary);
  text-align: left;
  cursor: pointer;
  transition: background-color 0.15s, border-color 0.15s;

  &:hover,
  &:focus-visible {
    border-color: var(--el-color-primary-light-5);
    background: var(--el-fill-color-light);
    outline: none;
  }
}

.map-picker__icon {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 30px;
  height: 30px;
  border-radius: 9px;
  font-size: 16px;
  flex-shrink: 0;

  &.is-tile {
    background: var(--el-color-primary-light-9);
    color: var(--el-color-primary);
  }

  &.is-image {
    background: var(--el-color-success-light-9);
    color: var(--el-color-success);
  }
}

.map-picker__text {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 1px;
}

.map-picker__name {
  font-size: 13px;
  font-weight: 600;
  line-height: 1.3;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.map-picker__meta {
  display: flex;
  align-items: center;
  gap: 6px;
  color: var(--el-text-color-secondary);
  font-size: 12px;
  line-height: 16px;
}

.map-picker__arrow {
  color: var(--el-text-color-secondary);
  font-size: 13px;
  flex-shrink: 0;
}

/* ===== Nút công cụ: dạng phẳng trong khung, nổi màu khi rê chuột ===== */
.map-tool.el-button,
.map-card--tools :deep(.map-compass) {
  width: 30px;
  height: 30px;
  margin-left: 0 !important;
  border-color: transparent;
  background: transparent;
  color: var(--el-text-color-regular);
  font-size: 16px;
  transition: background-color 0.2s, color 0.2s, border-color 0.2s, transform 0.2s;
}

.map-tool.el-button:hover,
.map-tool.el-button:focus-visible,
.map-card--tools :deep(.map-compass:hover) {
  border-color: var(--el-color-primary-light-5);
  background: var(--el-color-primary-light-9);
  color: var(--el-color-primary);
  transform: scale(1.06);
}

/* Nút lọc: luôn có nền nhạt màu chủ đạo để dễ nhận ra */
.map-tool--accent.el-button {
  border-color: var(--el-color-primary-light-7);
  background: var(--el-color-primary-light-9);
  color: var(--el-color-primary);
}

/* Nhãn "Mặc định" của bản đồ */
.map-option__tag {
  padding: 0 6px;
  border-radius: 999px;
  border: 1px solid var(--el-color-warning-light-7);
  background: var(--el-color-warning-light-9);
  color: var(--el-color-warning-dark-2);
  font-size: 10px;
  line-height: 16px;
  font-weight: 600;
}
</style>
