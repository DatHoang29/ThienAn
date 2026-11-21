<template>
  <div class="map-controls">
    <!-- Zoom controls-->
    <div class="zoom-controls"> 
      <div>
        <el-tooltip :content="$t('lz.button.tmsMapLocation.zoomIn')"  placement="left">
            <el-button @click="zoomIn" icon="ele-ZoomIn" circle></el-button>
        </el-tooltip>
      </div>     
      <div>        
        <el-tooltip :content="$t('lz.button.tmsMapLocation.zoomOut')"  placement="left">
          <el-button @click="zoomOut" icon="ele-ZoomOut" circle></el-button>
        </el-tooltip>
      </div>
    
    </div>
    <!-- Map type controls-->
    <div class="map-type-controls">
      <el-popover
        ref="mapTypePopoverRef"
        placement="left-start"
        :width="330"
        :offset="10"
        :show-arrow="false"
        trigger="click"
        popper-class="map-type-popover"
        @show="mapTypePanelRef?.reveal()"
      >
        <template #reference>
          <div>
            <el-tooltip :content="$t('lz.button.tmsMapLocation.changeMap')" placement="left">
              <el-button icon="ele-MapLocation" circle></el-button>
            </el-tooltip>
          </div>
        </template>

        <MapTypePanel ref="mapTypePanelRef" :items="mapTypes" :model-value="selectedMapType" @select="onSelectMapType" />
      </el-popover>
    </div>
    <!-- Filter markers-->
    <div class="filter-controls">
      <el-popover
        placement="left-start"
        :width="340"
        :offset="10"
        :show-arrow="false"
        trigger="click"
        popper-class="map-filter-popover"
      >
        <template #reference>
          <div>
            <el-tooltip :content="$t('lz.button.tmsMapLocation.filterOnMap')" placement="left">
              <el-button icon="ele-Filter" circle></el-button>
            </el-tooltip>
          </div>
        </template>

        <MapFilterPanel
          v-model:show-icon-name="mapStore.showIconName"
          name-toggle
          :groups="itemGroupFilters"
          @group-change="onGroupToggle"
          @item-change="onItemToggle"
          @check-all="handleCheckAll"
          @uncheck-all="handleUncheckAll"
        />
      </el-popover>
    </div>
    <div class="refresh-controls">

      <div>
        <!-- Về đúng góc xoay đã cấu hình cho bản đồ (la bàn nằm ở cụm góc trên bên trái) -->
        <el-tooltip :content="$t('lz.button.tmsMapLocation.rotateReset')" placement="left">
          <el-button @click="rotateReset" circle>
            <el-icon><ele-Compass /></el-icon>
          </el-button>
        </el-tooltip>
      </div>
      <div>
        <el-tooltip :content="$t('lz.button.base.refresh')"  placement="left">
          <el-button @click="refreshMap" icon="ele-Refresh" circle></el-button>
       </el-tooltip>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, reactive, watch } from 'vue';
import L from 'leaflet';
import { getTileProxyUrl, TRANSPARENT_TILE } from '/@/utils/mapTile';
import { useMapStore } from '/@/stores/its/mapStore';
import { useI18n } from 'vue-i18n';
import { map } from 'xe-utils';
import MapFilterPanel from '../../mapSample/components/MapFilterPanel.vue';
import MapTypePanel from '../../mapSample/components/MapTypePanel.vue';

const checkNameDesc = ref(true)

// Khai báo để dịch thuật
const { t: $t } = useI18n();

interface Item {
  label: string;
  value: string;
  checked: boolean;
  icon?: string;
  unitType?: string;
}

interface ItemGroup {
  label: string;
  value: string;
  checked: boolean;
  indeterminate: boolean;
  children: Item[];
}

// Khai bao su dung cho filter
let itemGroupFilters =  reactive<ItemGroup[]>([]);

// Bổ sung default mặc định view state nếu ko load đk cấu hình
interface MapViewState {
  center: [number, number];
  zoom: number;
}

const DEFAULT_VIEW_STATE: MapViewState = {
  center: [10.8231, 106.6297],
  zoom: 11
};

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
  geoDataJson:[] as any
})

const mapStore = useMapStore();


const props = defineProps<{
  map: L.Map | null,  
   mapId?: string,
  geoJsonData:  any,
  activeFilters?: Record<string, any>
}>();

const selectedFilter = ref('all');
const selectedMapType = ref('standard');
const mapTypes = ref<any[]>([]);
const mapTypePopoverRef = ref();
const mapTypePanelRef = ref<InstanceType<typeof MapTypePanel>>();
const filterTypes = [
  { value: 'all', label: $t('lz.label.tmsMapLocation.showAllMarkers') },
  { value: 'type1', label: $t('lz.label.tmsMapLocation.filterType1') },
  { value: 'type2', label: $t('lz.label.tmsMapLocation.filterType2') }
];


const emit = defineEmits<{
  'mapId': [mapId: string],
  'zoomIn': [],
  'zoomOut': [],
  'refreshMap': [],
  'filterChange':[ItemGroup:any],
  'rotateReset': []
}>();

const zoomIn = () => {
  // if (props.map) {
  //   props.map.setZoom(props.map.getZoom() + 1);
  // }
  emit('zoomIn');
};

const zoomOut = () => {
  // if (props.map) {
  //   props.map.setZoom(props.map.getZoom() - 1);
  // }
  emit('zoomOut');
};

const filterMarkers = (type: string) => {
  selectedFilter.value = type;
  //console.log(`Filtering markers of type: ${type}`);
};

const refreshMap = () => {
  // if (props.map) {
  //   props.map.setView([10.8231, 106.6297], 11);
  //   console.log("Map refreshed");
  // }
  emit('refreshMap');
};

const rotateReset = () => {
  emit('rotateReset');
};

// Hàm helper hỗ trợ pasre view state config
const parseViewState = (raw: any): MapViewState | null => {
  if (!raw) return null;

  try {
    const parsed = typeof raw === 'string' ? JSON.parse(raw) : raw;

    if (
      parsed &&
      Array.isArray(parsed.center) &&
      parsed.center.length === 2 &&
      typeof parsed.zoom === 'number'
    ) {
      return {
        center: [Number(parsed.center[0]), Number(parsed.center[1])],
        zoom: Number(parsed.zoom)
      };
    }

    return null;
  } catch (error) {
    console.warn('Parse viewState lỗi:', raw, error);
    return null;
  }
};

// Fetch map type
const fetchMapTypes = async () => {
  try {
    //console.log("fetchMapTypes", mapStore.mapList);

    if (mapStore.mapList && mapStore.mapList.length < 1) {
      await mapStore.fetchMapList();
    }

    mapTypes.value = mapStore.mapList.map((map: any, index: number) => {  

      const remarkViewState = parseViewState(map.remark);
      const configViewState = parseViewState(map.config);

      const finalViewState =
        remarkViewState ||
        configViewState ||
        DEFAULT_VIEW_STATE;

      const item = {
        value: map.id,
        label: map.name,
        url: map.url,
        type: map.type,
        icon: '',
        isDefault: map.isDefault,
        viewState: finalViewState
      };     
      return item;
    });

    if (props.mapId) {
      selectedMapType.value = props.mapId;
      return;
    }

    const defaultMap = mapTypes.value.find(map => map.isDefault === 1);
    if (defaultMap) {
      selectedMapType.value = defaultMap.value;
      changeMapType(defaultMap.value);
    }
  } catch (error) {
    console.error('Error fetching map types:', error);
  }
};

/**
 * Xử lý thay đổi theo loại bản đồ
 */
const changeMapType = (mapId: string) => {

  selectedMapType.value = mapId;
  
  // Emit the mapId to parent component
  emit('mapId', mapId);
  
  if (!props.map) return;
  
  const selectedMap = mapTypes.value.find(map => map.value === mapId);
  if (!selectedMap) return;


  // Clear all existing tile layers and image overlays
  props.map.eachLayer((layer) => {
    if (layer instanceof L.TileLayer || layer instanceof L.ImageOverlay) {
      props.map?.removeLayer(layer);
    }
  });

  const isImageUrl = /^https?:\/\//i.test(selectedMap.url);
  if (!isImageUrl) {
    const bounds = props.map.getBounds();
    L.imageOverlay(selectedMap.url, bounds, { opacity: 1 }).addTo(props.map);
  } else {
    // Dùng L.tileLayer thuần (không qua safe-tile). Vẫn định tuyến qua proxy cùng origin để tránh CORS/PNA.
    L.tileLayer(getTileProxyUrl(selectedMap.url), {
      attribution: selectedMap.label,
      errorTileUrl: TRANSPARENT_TILE,
      minZoom: 1,
      maxZoom: 18,
    }).addTo(props.map);
  }

  // Apply saved view state if available
  if (selectedMap.viewState) {
    try {
      props.map.setView(
        selectedMap.viewState.center,
        selectedMap.viewState.zoom,
        { animate: true, duration: 1 }
      );
    } catch (error) {
      console.warn('Error applying saved view state:', error);
      // Fallback to default view
      props.map.setView([10.8231, 106.6297], 11);
    }
  } else {
    // Use default view if no saved state
    props.map.setView([10.8231, 106.6297], 11);
  }
};

/**
 * Chọn bản đồ từ panel: chỉ đổi khi khác bản đồ hiện tại, rồi đóng popover
 */
const onSelectMapType = (mapId: string) => {
  if (String(mapId) !== String(selectedMapType.value)) {
    changeMapType(mapId);
  }
  mapTypePopoverRef.value?.hide?.();
};

/* 
	Begin of filter functions
*/

//Filter tên thiết bị icon trên map
const handleFilterChange = (name: string) => {
  let temp = mapStore.showIconName;  
  mapStore.showIconName = null;
  mapStore.showIconName = temp;
  
}
// Chọn tất cả filter (groupValues: chỉ áp dụng cho các nhóm của tab đang mở)
const handleCheckAll = (groupValues?: string[]) => {
  itemGroupFilters.forEach((group: any) => {
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
  setTimeout(() => handleFilterChange('name'), 2000);
}

// Bỏ chọn tất cả filter (groupValues: chỉ áp dụng cho các nhóm của tab đang mở)
const handleUncheckAll = (groupValues?: string[]) => {
  itemGroupFilters.forEach((group: any) => {
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
  setTimeout(() => handleFilterChange('name'), 2000);
}

// Filter dữ liệu cha
const handleGroupChange = (group: any) => {
 if(group.children && group.children.length > 0){
    group.children.forEach((item: any) => {
      item.checked = group.checked
    })
  }  
  emitFilterChange()
  setTimeout(() => handleFilterChange('name'), 2000);
}

// Filter dữ liệu con
const handleItemChange = (group: any) => {
  const checkedCount = group.children.filter((item: any) => item.checked).length
  group.checked = checkedCount === group.children.length
  group.indeterminate = checkedCount > 0 && checkedCount < group.children.length
  emitFilterChange()
  setTimeout(() => handleFilterChange('name'), 2000);
}


// Panel báo bật/tắt cả nhóm
const onGroupToggle = (group: ItemGroup, checked: boolean) => {
  group.checked = checked
  group.indeterminate = false
  handleGroupChange(group)
}

// Panel báo bật/tắt một mục con
const onItemToggle = (group: ItemGroup, item: Item, checked: boolean) => {
  item.checked = checked
  handleItemChange(group)
}

// filter dữ liệu
const emitFilterChange = () => {
  const filters = itemGroupFilters.reduce((acc: any, group) => {
    //console.log("emitFilterChange", acc)
    acc[group?.value] = group?.children
      .filter((item : any) => item.checked)
      .map((item: any) => item.value)
    return acc
  }, {})
  
  //console.log("emitFilterChange - after", filters)
  emit('filterChange', filters)
}

/* 
	End of filter functions
*/

// Convert data to itemGroupFilters
const transformData = (listItemGroup:any, listEquipmentType:any, listZone:any, listWorkUnit: any, eventTypeList: any) => {
  listItemGroup?.forEach((item: any) => {
    switch(item?.code){
      case 'device':
        //console.log("handleFilterChange - listEquipmentType", listEquipmentType)
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
        itemGroupFilters.push(objItemGroup)
        //console.log("handleFilterChange - itemGroupFilters", itemGroupFilters)
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
        itemGroupFilters.push(objItemGroup_3)
        break;
    }
  })

  // Add Thêm filter sự kiện sự cố
  const objItemGroup:ItemGroup = {
          label : $t('lz.label.tmsMapLocation.event'),
          value : 'event',
          checked: true,
          indeterminate: true,
          children: []
  }   
  eventTypeList.forEach((eventType: any) => {
      const objTemp = {
         label: eventType?.name,
        value: eventType?.id,
        checked: true,
        icon: eventType?.url || ''
      };
      objItemGroup.children.push(objTemp);
  })
  itemGroupFilters.push(objItemGroup)

  //console.log("itemGroupFilters", itemGroupFilters)
  /////////////////////////////////////
}

// function Update Item Group Filters — chỉ giữ lại filter có dữ liệu trên bản đồ
function UpdateItemGroup(listGeoJsonData: any){
  if(!listGeoJsonData || listGeoJsonData.length === 0) return;

  // Lấy tất cả id của device trên bản đồ
  const listDeviceIds = listGeoJsonData
    .filter((f: any) => f?.properties?.itemGroup === 'device')
    .map((f: any) => f?.properties?.id)
    .filter(Boolean)

  // Lấy tất cả id của unit trên bản đồ
  const listUnitIds = listGeoJsonData
    .filter((f: any) => f?.properties?.itemGroup === 'unit')
    .map((f: any) => f?.properties?.id)
    .filter(Boolean)

  // Lấy danh sách itemGroup có trên bản đồ
  const itemGroupsOnMap = new Set(
    listGeoJsonData
      .map((f: any) => f?.properties?.itemGroup)
      .filter(Boolean)
  )

  // Lấy danh sách equipmentTypeId từ device ids
  const listEquipmentTypeId = new Set<string>()
  mapStore.equipmentList.forEach((equipment: any) => {
    if (listDeviceIds.includes(equipment?.id)) {
      listEquipmentTypeId.add(equipment?.equipmentTypeId)
    }
  })

  // Lọc children theo dữ liệu thực có trên bản đồ
  itemGroupFilters.forEach((group: any) => {
    switch (group.value) {
      case 'device':
        group.children = group.children.filter((child: any) =>
          listEquipmentTypeId.has(child.value)
        )
        break
      case 'unit':
        group.children = group.children.filter((child: any) =>
          listUnitIds.includes(child.value)
        )
        break
      case 'zone':
        // Giữ zone nếu có zone trên bản đồ
        if (!itemGroupsOnMap.has('zone')) {
          group.children = []
        }
        break
      case 'event':
        // Chỉ giữ eventType đang có sự cố thực tế trên bản đồ
        const activeEventTypeIds = new Set(
          (mapStore.incidentZoneDataExistInTrafficInfoList || [])
            .map((item: any) => item?.eventTypeId)
            .filter(Boolean)
        )
        if (activeEventTypeIds.size > 0) {
          group.children = group.children.filter((child: any) =>
            activeEventTypeIds.has(child.value)
          )
        } else {
          group.children = []
        }
        break
    }
  })

  // Xóa nhóm không còn children
  const toRemove = itemGroupFilters
    .filter((g: any) => !g.children || g.children.length === 0)
    .map((g: any) => g.value)
  
  toRemove.forEach((val: string) => {
    const idx = itemGroupFilters.findIndex((g: any) => g.value === val)
    if (idx !== -1) itemGroupFilters.splice(idx, 1)
  })
}

// Load data from map store
async function loadData() {
    // reset filter cũ
  itemGroupFilters.splice(0, itemGroupFilters.length)

  if (mapStore.itemGroupList && mapStore.itemGroupList.length < 1) {   
    await mapStore.fetchItemGroupList();   
  } 
  if (mapStore.equipmentTypeList && mapStore.equipmentTypeList.length < 1) {    
    await mapStore.fetchEquipmentTypeList();
  } 
  if (mapStore.workUnitList && mapStore.workUnitList.length < 1) {
    await mapStore.fetchWorkUnitList();
  } 

  if (mapStore.eventTypeList && mapStore.eventTypeList.length < 1) {   
    await mapStore.fetchEventTypeData();   
  }

  state.itemGroup = mapStore.itemGroupList;
  state.equipmentTypeList = mapStore.equipmentTypeList;
  state.workUnitList = mapStore.workUnitList;
  state.eventTypeList = mapStore.eventTypeList;

  //console.log("state.itemGroup:", state.itemGroup);
   //console.log("state.equipmentTypeList:", state.equipmentTypeList);
  // console.log("state.workUnitList:", state.workUnitList);
  // console.log("state.eventTypeList:", state.eventTypeList);
  // console.log("state.zoneList:", state.zoneList);

  transformData(
    state.itemGroup,
    state.equipmentTypeList,
    state.zoneList,
    state.workUnitList,
    state.eventTypeList
  );

if (props.geoJsonData && props.geoJsonData.length > 0) {
    state.geoDataJson = [...props.geoJsonData]
    UpdateItemGroup(state.geoDataJson)
  }

  if (props.activeFilters && Object.keys(props.activeFilters).length > 0) {
    applySavedFiltersToUi(props.activeFilters)
  }

}

/**
 * Hàm apply filter saved vào checkbox UI
 */
const applySavedFiltersToUi = (filters: Record<string, any>) => {
  if (!filters || Object.keys(filters).length === 0) return

  //console.log("applySavedFiltersToUi - filters", filters)
  //console.log("applySavedFiltersToUi - itemGroupFilters", itemGroupFilters)
  itemGroupFilters.forEach((group: any) => {
    const selectedValues = filters[group.value] || []

    if (!Array.isArray(group.children)) return

    group.children.forEach((item: any) => {
      item.checked = selectedValues.includes(item.value)
    })

    const checkedCount = group.children.filter((item: any) => item.checked).length
    group.checked = checkedCount === group.children.length
    group.indeterminate = checkedCount > 0 && checkedCount < group.children.length
  })
}

/**
 * Watch theo dõi geo json data
 */
watch(
  () => props.geoJsonData,
  async  (newVal, oldValue) => {
    if (newVal != oldValue) {
      state.geoDataJson = [...newVal]
      await loadData();   
    }
  },
  { deep: true }
)

/**
 * Watch activeFilters để sync lại UI khi parent restore xong
 */
watch(
  () => props.activeFilters,
  (newVal) => {  
    if (!newVal || itemGroupFilters.length === 0) return
    applySavedFiltersToUi(newVal)
  },
  { deep: true, immediate: true }
)

/**
 * Watch filter thay đổi mapId
 */
watch(
  () => props.mapId,
  async (newMapId) => {
    if (!newMapId) return;
    selectedMapType.value = newMapId;
    await loadData([]);
  },
  { immediate: true }
);

onMounted( async () => {
  await loadData();
  fetchMapTypes();
   if (props.activeFilters && Object.keys(props.activeFilters).length > 0) {
    applySavedFiltersToUi(props.activeFilters)
  }
});


</script>

<style scoped lang="scss">
/* Cụm nút bên phải: một khung nổi có viền, các nhóm nút ngăn bằng vạch mảnh
   (cùng phong cách với cụm công cụ của màn thiết lập bản đồ mapSample) */
.map-controls {
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

  position: absolute;
  top: 80px;
  right: 20px;
  z-index: 1200;
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 5px;
  border: 1px solid var(--el-border-color-light);
  border-radius: 12px;
  background: color-mix(in srgb, var(--el-bg-color-overlay) 92%, transparent);
  backdrop-filter: blur(6px);
  box-shadow: 0 6px 18px rgba(15, 23, 42, 0.18), 0 1px 3px rgba(15, 23, 42, 0.1);

  .zoom-controls,
  .map-type-controls,
  .refresh-controls,
  .filter-controls {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 4px;
  }

  /* Vạch ngăn giữa các nhóm: phóng to/thu nhỏ | bản đồ, bộ lọc | xoay, làm mới */
  .map-type-controls,
  .refresh-controls {
    margin-top: 5px;
    padding-top: 5px;
    border-top: 1px solid var(--el-border-color);
  }

  .filter-controls {
    margin-top: 4px;
  }

  /* Nút phẳng trong khung, nổi màu chủ đạo khi rê chuột */
  .el-button {
    width: 32px;
    height: 32px;
    margin: 0 !important;
    border-color: transparent;
    background: transparent;
    color: var(--el-text-color-regular);
    font-size: 16px;
    transition: background-color 0.2s, color 0.2s, border-color 0.2s, transform 0.2s;

    &:hover,
    &:focus-visible {
      border-color: var(--el-color-primary-light-5);
      background: var(--el-color-primary-light-9);
      color: var(--el-color-primary);
      transform: scale(1.06);
    }
  }
}
</style>
