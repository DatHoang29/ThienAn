<template>
  <div class="right-panel" :class="{ 'collapsed': isPanelCollapsed }">
    <!-- Collapse Toggle Button -->
    <div class="collapse-toggle" @click="togglePanel">
      <el-icon>
        <component :is="isPanelCollapsed ? 'ele-ArrowLeft' : 'ele-ArrowRight'" />
      </el-icon>
    </div>

    <!-- Panel Content -->
    <div class="panel-content" v-show="!isPanelCollapsed">
      <el-tabs v-model="activeTab">
        <!-- Table Tab -->
        <el-tab-pane label="Table" name="table">
          <div class="table-container">
            <div class="card-header">
              <span>Location Items</span>
              <div class="search-controls">
                <el-input
                  v-model="tableSearch"
                  placeholder="Search by name or type"
                  clearable
                  @clear="handleSearchClear"
                >
                  <template #prefix>
                    <el-icon><Search /></el-icon>
                  </template>
                </el-input>
              </div>
            </div>
            
            <el-table 
              :data="filteredTableData" 
              style="width: 100%" 
              size="small"
            >
              <el-table-column prop="name" label="Name" min-width="50" />
              <!-- Location Controls -->
              <el-table-column label="Location">
                <template #default="{ row }">
                  <div class="location-controls">
                    <!-- For Point features -->
                    <template v-if="row.geometryType === 'Point'">
                      <el-input-number 
                        v-model="row.lat" 
                        :precision="6"
                        :step="0.00001"
                        size="small"
                        placeholder="Latitude"
                        @change="(val: number) => handleLocationUpdate(row, val, 'lat')"
                      />
                      <el-input-number 
                        v-model="row.lng" 
                        :precision="6"
                        :step="0.00001"
                        size="small"
                        placeholder="Longitude"
                        @change="(val: number) => handleLocationUpdate(row, val, 'lng')"
                      />
                    </template>
                    
                    <!-- For Polygon features -->
                    <template v-else-if="row.geometryType === 'Polygon'">
                      <div class="location-controls">
                        <el-input-number 
                          :model-value="row.topLeft.lat"
                          :precision="6"
                          :step="0.00001"
                          size="small"
                          placeholder="Latitude"
                          @change="(val: number) => handlePolygonUpdate(row, val, 'lat')"
                        />
                        <el-input-number 
                          :model-value="row.topLeft.lng"
                          :precision="6"
                          :step="0.00001"
                          size="small"
                          placeholder="Longitude"
                          @change="(val: number) => handlePolygonUpdate(row, val, 'lng')"
                        />
                      </div>
                    </template>
                  </div>
                </template>
              </el-table-column>

              <!-- Actions -->
              <el-table-column label="Actions" width="250" fixed="right">
                <template #default="{ row }">
                  <div class="action-buttons">
                    <el-tooltip content="Locate on map">
                      <el-button 
                        size="small" 
                        type="primary" 
                        @click="$emit('locate-on-map', row)"
                        :icon="Search"
                      />
                    </el-tooltip>
                    <template v-if="row.geometryType === 'Polygon'">
                      <el-tooltip content="Edit on map">
                        <el-button 
                          size="small" 
                          type="warning" 
                          @click="$emit('start-editing', row)"
                          :icon="Edit"
                        />
                      </el-tooltip>
                    </template>
                    <el-tooltip content="Move on map">
                      <el-button 
                        size="small" 
                        type="info" 
                        @click="$emit('start-moving', row)"
                        :icon="Position"
                      />
                    </el-tooltip>
                    <el-tooltip content="Delete">
                      <el-button 
                        size="small" 
                        type="danger" 
                        @click="$emit('confirm-delete', row)"
                        :icon="Delete"
                      />
                    </el-tooltip>
                  </div>
                </template>
              </el-table-column>
            </el-table>
          </div>
        </el-tab-pane>

        <!-- GeoJSON Tab -->
        <el-tab-pane label="GeoJSON" name="geojson">
          <div class="geojson-container">
            <pre class="geojson-content">{{ JSON.stringify(geoJsonData, null, 2) }}</pre>
          </div>
        </el-tab-pane>
      </el-tabs>
    </div>
  </div>
</template>

<script lang="ts" setup>
import { ref, computed,reactive,watch } from 'vue';
import { Search, Delete, Edit, Position } from '@element-plus/icons-vue';

const props = defineProps({
  geoJsonData: {
    type: Object,
    required: true
  },
  EquipmentList: [] as any[],
  EquipmentTypeList: [] as any[],
  ZoneTypeList: [] as any[],
  WorkUnitTypeList: [] as any[],
  WorkUnitList: [] as any[],
  ZonesList: [] as any[],
  ZonesDataList: [] as any[],
});

const emit = defineEmits([
  'update-location',
  'update-polygon-location',
  'locate-on-map',
  'start-editing',
  'start-moving',
  'confirm-delete'
]);

const state = reactive({
  EquipmentList: ref(props.EquipmentList),
  EquipmentTypeList: ref(props.EquipmentTypeList),
  ZoneTypeList: ref(props.ZoneTypeList),
  WorkUnitTypeList: ref(props.WorkUnitTypeList),
  WorkUnitList: ref(props.WorkUnitList),
  ZonesList: ref(props.ZonesList),
  ZonesDataList: ref(props.ZonesDataList),
})
watch(() => props.EquipmentList, (newVal) => {
  state.EquipmentList = newVal
})

watch(() => props.EquipmentTypeList, (newVal) => {
  state.EquipmentTypeList = newVal
})  
watch(() => props.ZoneTypeList, (newVal) => {
  state.ZoneTypeList = newVal
})
watch(() => props.WorkUnitTypeList, (newVal) => {
  state.WorkUnitTypeList = newVal
})
watch(() => props.WorkUnitList, (newVal) => {
  state.WorkUnitList = newVal
})
watch(() => props.ZonesList, (newVal) => {
  state.ZonesList = newVal
})
watch(() => props.ZonesDataList, (newVal) => {
  state.ZonesDataList = newVal
})

const isPanelCollapsed = ref(false);
const activeTab = ref('table');
const tableSearch = ref('');

// Toggle panel visibility
const togglePanel = () => {
  isPanelCollapsed.value = !isPanelCollapsed.value;
};

// Clear search input
const handleSearchClear = () => {
  tableSearch.value = '';
};

// Transform GeoJSON data into table data
const tableData = computed(() => {
  return props.geoJsonData.features.map((feature: any) => {
    var details = getItemDetails(feature.properties?.id, feature.properties?.itemGroup);
    const baseData = {
      id: feature.properties?.id,
      name: details.name,
      geometryType: feature.geometry.type,
    };

    if (feature.geometry.type === 'Point') {
      const [lng, lat] = feature.geometry.coordinates;
      return {
        ...baseData,
        lat,
        lng
      };
    } else {
      const coordinates = feature.geometry.coordinates[0];
      const [firstLng, firstLat] = coordinates[0];
      return {
        ...baseData,
        topLeft: { lat: firstLat, lng: firstLng }
      };
    }
  });
});
// Enhanced getItemDetails function
interface ItemDetails {
  name: string;
  iconUrl: string;
  unit?: string;
  measurement?: number;
  type?: string;
  additionalInfo?: any;
}
const getItemDetails = (id: string, itemGroup: string | undefined): ItemDetails => {
  try {
    if (!id || !itemGroup) {
      return { name: 'Unknown', iconUrl: Image };
    }
    switch (itemGroup) {
      case 'device': { // Equipment
        const equipment = state.EquipmentList
          .find(item => item.id === id);
        if (!equipment) return { name: 'Unknown Equipment', iconUrl: Image };

        const equipmentType = state.EquipmentTypeList
          .find(type => type.id === equipment.equipmentTypeId);
        return {
          name: equipment.name +' (Km ' + equipment.kmNumber +'+'+ equipment.metNumber +')',
          iconUrl: equipmentType?.defaultIcon || Image,
          type: equipmentType?.name  || 'equipment',
          unit: equipment.unit || '',
          measurement: equipment.measurement || 0,
          additionalInfo: {
            status: equipment.status,
            model: equipment.model,
            serialNumber: equipment.serialNumber
          }
        };
      }

      case 'zone': { // Zones
        const zone = state.ZonesDataList
          .find(item => item.id === id);
        
        if (!zone) return { name: 'Unknown Zone', iconUrl: Image };

        return {
          name: zone.name,
          iconUrl: Image,
          type: zone.type || 'zone',
          unit: 'm²',
          measurement: zone.area || 0,
          additionalInfo: {
            description: zone.description,
            capacity: zone.capacity,
            zoneType: zone.type
          }
        };
      }

      case 'unit': { // Work Units
        const workUnit = state.WorkUnitList
          .find(item => item.id === id);
        
        if (!workUnit) return { name: 'Unknown Work Unit', iconUrl: Image };

        return {
          name: workUnit.name,
          iconUrl: Image,
          type: workUnit.type || 'unit',
          unit: workUnit.unit || '',
          measurement: workUnit.measurement || 0,
          additionalInfo: {
            description: workUnit.description,
            status: workUnit.status
          }
        };
      }

      default:
        return { name: 'Unknown', iconUrl: Image };
    }
  } catch (error) {
    console.error('Error getting item details:', error);
    return { name: 'Unknown', iconUrl: Image };
  }
};



// Filter table data based on search input
const filteredTableData = computed(() => {
  const search = tableSearch.value.toLowerCase().trim();
  if (!search) return tableData.value;
  
  return tableData.value.filter((item: any) => {
    const nameMatch = item.name?.toLowerCase().includes(search);
    const typeMatch = item.geometryType?.toLowerCase().includes(search);
    return nameMatch || typeMatch;
  });
});

// Emit location update events
const handleLocationUpdate = (row: any, value: number, field: 'lat' | 'lng') => {
  emit('update-location', row, value, field);
};

const handlePolygonUpdate = (row: any, value: number, field: 'lat' | 'lng') => {
  emit('update-polygon-location', row, value, field);
};
</script>

<style scoped>
/* Move all relevant styles from the main component */
.right-panel {
  position: absolute;
  top: 0;
  right: 0;
  width: 700px;
  height: 100%;
  background: white;
  border-left: 1px solid #dcdfe6;
  transition: all 0.3s ease;
  z-index: 1000;
  box-shadow: -2px 0 5px rgba(0, 0, 0, 0.1);
  display: flex;
  flex-direction: column;
}

.panel-content {
  flex: 1;
  overflow: hidden;
  display: flex;
  flex-direction: column;
}

:deep(.el-tabs) {
  display: flex;
  flex-direction: column;
  height: 100%;
}

:deep(.el-tabs__content) {
  flex: 1;
  overflow: hidden;
}

:deep(.el-tab-pane) {
  height: 100%;
}

.table-container {
  height: 100%;
  display: flex;
  flex-direction: column;
}

.card-header {
  padding: 10px;
}

:deep(.el-table) {
  flex: 1;
}

.geojson-container {
  height: 100%;
  overflow: auto;
  padding: 10px;
}

.geojson-content {
  margin: 0;
  white-space: pre-wrap;
  word-wrap: break-word;
}
</style>