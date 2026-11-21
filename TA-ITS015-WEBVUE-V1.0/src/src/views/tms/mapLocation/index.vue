@ -1,598 +0,0 @@
<template>
  <div class="tms-map-location-container">
    <el-form :model="state.ruleForm" ref="ruleFormRef" :show-message="false" :inlineMessage="true" label-width="auto">
      <el-card shadow="hover" :body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', height: 'auto', alignItems: 'start' }">
        <el-form :model="state.queryParams" ref="queryForm" :show-message="false" :inlineMessage="true" label-width="auto" style="flex: 1 1 0%">
          <el-row :gutter="10">
            <el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
              <el-form-item :label="$t('lz.label.tmsMap.map')" prop="mapId">
                <el-select v-model="state.queryParams.mapId" filterable clearable class="w100"  @clear="state.queryParams.mapId = undefined">
                  <el-option v-for="item in state.MapList" :key="item.id" :label="item.name" :value="item.id" />
                </el-select>
              </el-form-item>
            </el-col>
          </el-row>
        </el-form>
      </el-card>
      <div class="map-container">
        <div id="map" style="height: 500px;"></div>
      </div>
      <el-card shadow="hover" style="margin-top: 20px;">
       <el-row :gutter="20">
          <el-col :xs="24" :sm="12" :md="8" :lg="6" :xl="6" class="mb5">
            <el-form-item :label="$t('lz.entity.tmsMapLocation.objectType')" prop="objectType">
              <el-select v-model="state.queryParams.objectType" filterable clearable class="w100"  @change="onVmsActionlTypeChange">
                <el-option v-for="item in state.objectType" :key="item.code" :label="item.name" :value="item.code" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :xs="24" :sm="12" :md="8" :lg="6" :xl="6" class="mb5">
            <el-form-item v-if="state.showEquipmentControls" :label="$t('lz.entity.tms.equipmentType')" prop="typeId">
              <el-select v-model="state.queryParams.typeId" filterable clearable class="w100">
                <el-option v-for="item in state.EquipmentList" :key="item.id" :label="item.name" :value="item.id" :disabled="isIdInGeoJson(item.id)" />
              </el-select>
            </el-form-item>
            <el-form-item v-if="state.showWorkUnitControls" :label="$t('lz.label.tms.tmsWorkUnit')" prop="typeId">
              <el-select v-model="state.queryParams.typeId" filterable clearable class="w100">
                <el-option v-for="item in state.EquipmentList" :key="item.id" :label="item.name" :value="item.id" :disabled="isIdInGeoJson(item.id)" />
              </el-select>
            </el-form-item>
            <!-- Render Zone Cascader when objectType is '2' -->
            <el-form-item v-if="state.showZoneControls" :label="$t('lz.label.base.location')">
              <el-cascader
                :options="filteredMenuData"
                :props="{ checkStrictly: false, emitPath: false, value: 'id', label: 'name', children: 'children', disabled: (node) => isIdInGeoJson(node.id) }"
                :placeholder="$t('lz.label.tmsMapLocation.selectZone')"
                clearable
                class="w100"
                v-model="state.queryParams.pid"
                >
                <template #default="{ node, data }" >
                  <span >{{ data.name }}</span>
                  <span v-if="!node.isLeaf" :disabled="isIdInGeoJson(data.pid)"> ({{ data.children.length }}) </span>
                </template>
              </el-cascader>
            </el-form-item>
          </el-col>

          <el-col :xs="24" :sm="12" :md="8" :lg="6" :xl="6" class="mb5">
            <el-form-item v-if="state.showEquipmentControls " :label="$t('lz.entity.base.type')" prop="typeId">
              <el-select v-model="state.queryParams.equipment" filterable clearable class="w100">
                <el-option v-for="item in state.EquipmentData" :key="item.id" :label="item.name" :value="item.id" :disabled="isIdInGeoJson(item.id)" />
              </el-select>
            </el-form-item>
             <el-form-item v-if="state.showWorkUnitControls " :label="$t('lz.label.tms.tmsWorkContact')" prop="typeId">
              <el-select v-model="state.queryParams.WorkContact" filterable clearable class="w100">
                <el-option v-for="item in state.WorkContactList" :key="item.id" :label="item.name" :value="item.id" :disabled="isIdInGeoJson(item.id)" />
              </el-select>
            </el-form-item>
          </el-col>



        </el-row>

        <el-button v-if="state.showEquipmentControls || state.showWorkUnitControls" @click="addMarker">{{ $t('lz.button.tmsMapLocation.addMarker') }}</el-button>
        <el-button v-else @click="startDrawingPolygon">{{ $t('lz.button.tmsMapLocation.drawPolygon') }}</el-button>
        <el-button @click="submit">{{ $t('lz.button.base.submit') }}</el-button>
        <el-button @click="update">{{ $t('lz.button.base.update') }}</el-button>
        <div style="overflow: auto; max-height: 200px; padding: 10px;display: flex;justify-content: space-between;">
          <div>
          <pre>{{ state.geoJsonData }}</pre>
          </div>
          <div style="width:100%;">
            <div v-for="element in textElements" :key="element.id" class="element-content">
          <!-- Input field for content or label text if needed -->
              <el-input v-model="element.label" :placeholder="element.placeholder" disabled  />
              
              <div class="element-position-size">
                <!-- Position controls -->
                <div class="input-group" style="display: flex; justify-content: space-around;">
                  <div>
                    <span class="input-label">{{ $t('lz.label.tmsMapLocation.longitudeAxis') }}</span>
                    <el-input-number 
                      v-model="element.x" 
                      :min="-180" 
                      :max="180" 
                      :step="getStep(element.x)" 
                      @change="updateMarkerPosition(element)" 
                      style="width: 200px;"  
                    />
                  </div> 
                  <div>
                    <span class="input-label">{{ $t('lz.label.tmsMapLocation.latitudeAxis') }}</span>
                    <el-input-number 
                      v-model="element.y" 
                      :min="-180" 
                      :max="180" 
                      :step="getStep(element.y)" 
                      @change="updateMarkerPosition(element)" 
                      style="width: 200px;" 
                    />
                  </div>
                </div>

              </div>
            </div>
          </div>
        </div>
        
      </el-card>

      <!-- Modal for editing/removing markers and polygons -->
      <el-dialog v-model="state.editDialogVisible" :title="$t('lz.label.tmsMapLocation.editFeature')">
        <el-form :model="state.editFeature">
          <el-form-item :label="$t('lz.entity.tmsMapLocation.featureId')">
            <el-input v-model="state.editFeature.id" disabled></el-input>
          </el-form-item>
          <el-form-item v-if="state.editFeature.type === 'Polygon'" :label="$t('lz.entity.tmsMapLocation.strokeColor')">
            <el-color-picker v-model="state.editFeature.strokeColor" />
          </el-form-item>
          <el-form-item v-if="state.editFeature.type === 'Polygon'" :label="$t('lz.entity.tmsMapLocation.strokeWidth')">
            <el-input-number v-model="state.editFeature.strokeWidth" />
          </el-form-item>
          <el-form-item v-if="state.editFeature.type === 'Polygon'" :label="$t('lz.entity.tmsMapLocation.strokeOpacity')">
            <el-slider v-model="state.editFeature.strokeOpacity" :min="0" :max="1" step="0.1" />
          </el-form-item>
          <el-form-item v-if="state.editFeature.type === 'Polygon'" :label="$t('lz.entity.tmsMapLocation.fillColor')">
            <el-color-picker v-model="state.editFeature.fillColor" />
          </el-form-item>
          <el-form-item v-if="state.editFeature.type === 'Polygon'" :label="$t('lz.entity.tmsMapLocation.fillOpacity')">
            <el-slider v-model="state.editFeature.fillOpacity" :min="0" :max="1" step="0.1" />
          </el-form-item>
          <el-form-item v-if="state.editFeature.type === 'Point'" :label="$t('lz.entity.base.status')">
            <el-select v-model="state.editFeature.status" :placeholder="$t('lz.label.base.pleaseSelectStatus')">
              <el-option :label="$t('lz.label.base.active')" value="active"></el-option>
              <el-option :label="$t('lz.label.base.inactive')" value="inactive"></el-option>
            </el-select>
          </el-form-item>
          <el-form-item>
            <el-button type="danger" @click="removeFeature">{{ $t('lz.button.base.delete') }}</el-button>
            <el-button type="primary" @click="saveEdit">{{ $t('lz.button.base.save') }}</el-button>
          </el-form-item>
        </el-form>
      </el-dialog>
    </el-form>
  </div>
</template>

<script lang="ts" setup name="tmsMapLocation">
import { ref,computed, reactive, onMounted, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import Image from '/@/assets/performance.png';
import '@geoman-io/leaflet-geoman-free';
import '@geoman-io/leaflet-geoman-free/dist/leaflet-geoman.css';
import { getAPI } from '/@/utils/axios-utils';
import { SysConfigDataApi } from '/@/api-services/cfgSystem';
import { TmsMapApi, TmsMapLocationApi, TmsEquipmentApi, TmsEquipmentTypeApi,TmsZoneApi,TmsWorkUnitApi,TmsWorkContactApi } from '/@/api-services/tms';
import { UpdateTmsMapLocationInput,UpdateTmsMapDetailInput } from '/@/api-services/tms';
import { TmsEquipmentType,TmsMapLocationRefType } from '/@/types/enums/tmsEnum';
import { ElMessage,ElForm } from 'element-plus';
interface MapLocation {
	title: String,
	menuData: Array<TmsZoneApi>,
}
const { t: $t } = useI18n();
const props = defineProps<MapLocation>();
const ruleFormRef = ref<InstanceType<typeof ElForm>>();
const state = reactive({
  queryParams: {
    name: undefined,
    code: undefined,
    mapId: undefined,
    objectType: undefined,
    equipmentType: undefined,
    typeId: undefined,
    equipment: undefined,
    WorkContact: undefined,
    pid: undefined,
  },
  showEquipmentControls :false,
  showWorkUnitControls :false,
  showZoneControls :false,
  id:'',
  showWorkDetailIdRow: false, // Property to control the specific row visibility
  MapList: [] as any,
  EquipmentList: [] as any,
  Equipment: [] as any,
  EquipmentData: [] as any,
  WorkContactList: [] as any,
  WorkUnitList: [] as any,
  WorkContactListData: [] as any,
  EquipmentTypeList: [] as any,
  ZonesList: [] as any[],
  ZonesDataList: [] as any,
  objectType: [] as any,
  map: null as L.Map | null,
  markersLayer: L.layerGroup() as L.LayerGroup<L.Marker>,
  polygonsLayer: L.layerGroup() as L.LayerGroup<L.Polygon>,
  tileLayer: null as L.TileLayer | null,
  imageOverlay: null as L.ImageOverlay | null,
  ruleForm: {} as UpdateTmsMapLocationInput,
  Mapdetail: {} as UpdateTmsMapDetailInput,
  geoJsonData: {
    type: "FeatureCollection",
    features: [] as GeoJSON.Feature[],
  },
  editDialogVisible: false,
  editFeature: {
    id: '',
    status: '',
    strokeColor: '#000000',
    strokeWidth: 2,
    strokeOpacity: 1,
    fillColor: '#ff0000',
    fillOpacity: 1,
    type: '',
    layer: null as L.Layer | null,
  },
});
  const textElements = ref([]) as any;

// Initialize the map and load data
onMounted(async () => {
  try {
    // Fetch Map List data
    const mapData = await getAPI(TmsMapApi).apiTmsTmsmapListGet();
    state.MapList = mapData?.data?.result || [];
    let response = await getAPI(SysConfigDataApi).apiCfgsystemSysconfigdataDatalistCodeGet('tms_object_type');
    state.objectType = response.data.result;
    // Fetch Equipment Type List data        
    response = await getAPI(TmsWorkContactApi).apiTmsTmsworkcontactListGet();
    state.WorkContactListData = response.data.result;
    response = await getAPI(TmsEquipmentApi).apiTmsTmsequipmentListGet();
    state.Equipment= response.data.result;
    response = await getAPI(TmsWorkUnitApi).apiTmsTmsworkunitListGet();
    state.WorkUnitList= response.data.result;


    const equipmentTypeData = await getAPI(TmsEquipmentTypeApi).apiTmsTmsequipmenttypeListGet();
    state.EquipmentTypeList = equipmentTypeData?.data?.result || [];
    // Fetch Equipment List data and filter by equipmentType
    const zoneData = await getAPI(TmsZoneApi).apiTmsTmszoneGettreeGet();
    state.ZonesList = zoneData?.data?.result || [];
    const zoneDataList = await getAPI(TmsZoneApi).apiTmsTmszoneListGet();
    state.ZonesDataList = zoneDataList?.data?.result || [];
    handleEquipmentTypeChange();
    // Initialize map and set watchers
    initializeMap();
  } catch (error) {
    console.error('Error fetching data:', error);
  }
});
const filteredMenuData = computed(() => {
  return state.ZonesList.filter(item => item.id !== state.queryParams.pid);
});

// Initialize the map
const initializeMap = () => {
  state.markersLayer.clearLayers();
  state.polygonsLayer.clearLayers();
  state.map = L.map('map', {
    center: [10.8368941, 106.6685769],
    zoom: 18,
    maxZoom: 18,
    attributionControl: false // Disable attribution control
  });

  // Define the base layers
  const layers = {};
  let defaultLayer = null;

  state.MapList.forEach(map => {
    layers[map.name] = L.tileLayer(`${map.url}/tile/{z}/{x}/{y}.png`, {
      minZoom: 1,
      maxZoom: 18,
    }).on('add', () => {
      // Set mapId whenever a layer is added
      state.queryParams.mapId = map.id; // This will trigger the watcher
    });

    // Check for the default layer
    if (map.isDefault === 1) {
      defaultLayer = layers[map.name];
      state.queryParams.mapId = map.id; // Set mapId for the default layer
      watchMapIdChange(); // Ensure the watcher is set up
    }
  });

  // Add layers control to allow switching between base layers
  L.control.layers(layers).addTo(state.map);

  // Initialize layer groups for markers and polygons
  state.markersLayer = L.layerGroup().addTo(state.map);
  state.polygonsLayer = L.layerGroup().addTo(state.map);

  // Add layer groups to control to toggle visibility
  const overlayLayers = {
    'Markers Layer': state.markersLayer,
    'Polygons Layer': state.polygonsLayer,
  };
  L.control.layers(null, overlayLayers).addTo(state.map);

  // Check if there is a default layer and add it to the map
  if (defaultLayer) {
    defaultLayer.addTo(state.map);
  }

  // Handle drawing on the map
  state.map.on('pm:create', (e) => {
    const { layer } = e;
    const geoJson = layer.toGeoJSON();
    const id = state.queryParams.pid;

    if (layer instanceof L.Polygon) {
      // Style and properties for the polygon
      const { strokeColor, strokeWidth, strokeOpacity, fillColor, fillOpacity } = geoJson.properties;
      layer.setStyle({
        color: strokeColor || '#000000',
        weight: strokeWidth || 2,
        opacity: strokeOpacity || 1,
        fillColor: fillColor || '#ff0000',
        fillOpacity: fillOpacity || 1
      });
      geoJson.properties = { id, strokeColor, strokeWidth, strokeOpacity, fillColor, fillOpacity };
      state.geoJsonData.features.push(geoJson);
      state.polygonsLayer.addLayer(layer);  // Add to polygons layer group
      layer.on('click', () => showEditModal(layer, id, 'Polygon'));
    } else if (layer instanceof L.Marker) {
      // Properties for the marker
      const { status } = geoJson.properties;
      geoJson.properties = { id, status: status || 'active' };
      state.geoJsonData.features.push(geoJson);
      state.markersLayer.addLayer(layer);  // Add to markers layer group
      layer.on('click', () => showEditModal(layer, id, 'Point'));
    }
  });
};
const  isIdInGeoJson = (id) => {
    // Giả sử GeoJSON được lưu trữ trong state.geoJsonData
    return state.geoJsonData.features.some(feature => feature.properties.id === id);
  }
// Watch mapId changes and load corresponding map data
const watchMapIdChange = () => {
  // Function to handle the logic when mapId changes
  const handleMapIdChange = async (newValue) => {
    // Check if newValue is not null or undefined
    if (newValue != null) {
      const map = state.MapList.find((item) => item.id === newValue);
      if (map) {
        const mapUrl = map.url; // Adjust based on your data structure
        // Check if URL is valid
        if (!mapUrl) {
          console.error('Invalid map URL');
          return;
        }
        // Remove existing layers if they exist
        if (state.tileLayer) {
          state.map?.removeLayer(state.tileLayer);
          state.tileLayer = null;
        }
        if (state.imageOverlay) {
          state.map?.removeLayer(state.imageOverlay);
          state.imageOverlay = null;
        }

        // Determine if the URL is an image or a tile service
        const isImageUrl = /^https?:\/\//i.test(mapUrl);
        if (!isImageUrl) {
          const currentZoom = state.map.getZoom();
          state.map.setMinZoom(currentZoom);
          state.map.setMaxZoom(currentZoom);

          // Add new image overlay layer
          const bounds = state.map?.getBounds() || [[0, 0], [1, 1]];
          state.imageOverlay = L.imageOverlay(mapUrl, bounds, {
            opacity: 0.8 
          }).addTo(state.map as L.Map);
        } else {
          // Add new tile layer
          state.map?.setMaxZoom(18);
          state.map?.setMinZoom(1);
          state.tileLayer = L.tileLayer(`${map.url}/tile/{z}/{x}/{y}.png`, {
            minZoom: 1,
            maxZoom: 18,
          }).addTo(state.map as L.Map);
        }

        // Call API to get map location data
        try {
          const mapLocation = await getAPI(TmsMapLocationApi).apiTmsTmsmaplocationDatalistIdGet(newValue);
          
          // Check if returned data exists
          const mapData = mapLocation.data.result[0];
          
          if (mapData) {
            // If exists, assign GeoJSON value
            state.geoJsonData = mapData.geoJsonContent
              ? JSON.parse(mapData.geoJsonContent)
              : {
                  type: "FeatureCollection",
                  features: [] as GeoJSON.Feature[],
                };          
            state.id = mapData.id;

            // Render GeoJSON after receiving data
            renderGeoJson();
            addTextElementsFromGeoJson(state.geoJsonData);

          } else {
            // Handle when no data is found
            state.geoJsonData = {
              type: "FeatureCollection",
              features: [] as GeoJSON.Feature[],
            };
            addTextElementsFromGeoJson(state.geoJsonData);

            state.markersLayer.clearLayers();
            state.polygonsLayer.clearLayers();
            state.id = null;
            console.error('No map data found for the given mapId');
          }
        } catch (error) {
          console.error('Failed to fetch map data:', error);
          // Handle error when calling API
          state.geoJsonData = {};
          state.id = null;
        }
      } else {
        console.warn('Map not found for the selected ID');
      }
    } else {
      // If newValue is null or undefined, reset state
      console.warn('No valid map ID selected');
      state.geoJsonData = {};
      state.id = null;
    }
  };

  // Watch for changes to mapId
  watch(
    () => state.queryParams.mapId,
    (newValue) => {
      handleMapIdChange(newValue);
    },
    { immediate: true } // Execute immediately on setup
  );

  // Check and handle the initial mapId value if it's already set
  handleMapIdChange(state.queryParams.mapId);
};


// Render GeoJSON data on the map
const renderGeoJson = () => {
  if (!state.geoJsonData || !state.geoJsonData.features) {
    state.geoJsonData = {
      type: "FeatureCollection",
      features: []
    };
  }

  // Clear existing layers
  state.markersLayer.clearLayers();
  state.polygonsLayer.clearLayers();

  // Render GeoJSON data on the map
  L.geoJSON(state.geoJsonData, {
    onEachFeature: (feature, layer) => {
      // Handle Polygon features
      if (feature.geometry.type === 'Polygon') {
        state.polygonsLayer.addLayer(layer);
        
        // Calculate centroid for Polygon
        const coords = feature.geometry.coordinates[0];
        const numCoords = coords.length;
        let latSum = 0, lngSum = 0;

        coords.forEach(coord => {
          lngSum += coord[0];
          latSum += coord[1];
        });

        const centroid = [latSum / numCoords, lngSum / numCoords];

        // Find associated zone and child by feature ID
        const zone = state.ZonesList.find(z => 
          z.children.some(child => child.id === feature.properties.id)
        );
        const zoneChild = zone?.children.find(x => x.id === feature.properties.id);

        // Set up polygon event handlers
        layer.on('click', () => showEditModal(layer, feature.properties.id, 'Polygon'));

        // Display tooltip with zone information on hover
        if (zoneChild) {
          layer.on('mouseover', () => {
            layer.bindTooltip(`
              <strong>Name:</strong> ${zoneChild.name}<br>
              <strong>Location:</strong><br>From: Km ${zoneChild.fromKmNumber}<br>To: Km ${zoneChild.toKmNumber}
              <br><strong>Coordinates:</strong>${feature.geometry.coordinates}
            `, {
              permanent: false,
              direction: 'top',
            }).openTooltip();
          });

          layer.on('mouseout', () => layer.closeTooltip());

          // Create an info marker at the centroid
          const infoDiv = L.divIcon({
            className: 'polygon-info',
            html: `
              <div style="font-size: 12px;color: black;display: flex;">
                ${zoneChild.name} <br>From: Km(${zoneChild.fromKmNumber})<br>To: Km(${zoneChild.toKmNumber})
              </div>`,
            iconSize: [170, 42],
            iconAnchor: [85, 32],
            popupAnchor: [0, -85]
          });
          const infoMarker = L.marker(centroid, { icon: infoDiv });
          state.markersLayer.addLayer(infoMarker);
        }

      } else if (feature.geometry.type === 'Point') {
        // Handle Point features (Equipment or Work Unit)
        const selectedEquipmentId = feature.properties.id;
        const equipment = state.Equipment.find(item => item.id === selectedEquipmentId);
        
        if (equipment) {
          // Get equipment type and icon
          const equipmentType = state.EquipmentTypeList.find(item => item.id === equipment.equipmentTypeId);
          const iconUrl = equipmentType?.defaultIcon || 'default-icon.png';

          const icon = L.divIcon({
            className: 'custom-div-icon',
            html: `
              <div style="text-align: center; background-color: transparent; margin-left:-45px">
                <img src="${iconUrl}" style="width: 32px; height: 32px; display: block; margin-left:20px">
                <div style="font-size: 12px; margin-top: 2px; color: black; text-align: center;">
                  ${equipment.name} Km(${equipment.kmNumber}+${equipment.metNumber})
                </div>
              </div>`,
            iconSize: [32, 42],
            iconAnchor: [16, 32],
            popupAnchor: [0, -32]
          });

          layer.setIcon(icon);
          layer.setLatLng([feature.geometry.coordinates[1], feature.geometry.coordinates[0]]);
          state.markersLayer.addLayer(layer);
          layer.dragging.enable();

          // Event handlers for equipment points
          layer.on('dragend', () => {
            const latlng = layer.getLatLng();
            updateMarkerPositionInGeoJSON(feature.properties.id, latlng);
          });
          layer.on('mouseover', () => {
            layer.bindTooltip(`
              <div style="text-align: left; font-size: 14px; line-height: 1.4;">
                  <strong>Name:</strong> ${equipment.name} <br>
                  <strong>Location:</strong>  Km(${equipment.kmNumber}+${equipment.metNumber}) <br>
                  <strong>Coordinates:</strong> 
                  <br>Latitude: ${feature.geometry.coordinates[1].toFixed(6)}
                  <br>Longitude: ${feature.geometry.coordinates[0].toFixed(6)}
                </div>
            `, { permanent: false, direction: 'top' }).openTooltip();
          });
          layer.on('mouseout', () => layer.closeTooltip());
          layer.on('click', () => showEditModal(layer, feature.properties.id, 'Point'));

        } else {
          // Handle Work Unit as default
          const workunit = state.WorkContactListData.find(item => item.id === selectedEquipmentId);
          const codeWorkUnit = state.WorkUnitList.find(item=> item.id === workunit.refId)
           const iconUrl = Image;
          const icon = L.divIcon({
            className: 'custom-div-icon',
            html: `
              <div style="text-align: center; background-color: transparent; margin-left:-45px">
                <img src="${iconUrl}" style="width: 32px; height: 32px; display: block; margin-left:20px">
                <div style="font-size: 12px; margin-top: 2px; color: black; text-align: center;">
                  ${workunit?.name || 'Unknown'}
                </div>
              </div>`,
            iconSize: [32, 42],
            iconAnchor: [16, 32],
            popupAnchor: [0, -32]
          });

          layer.setIcon(icon);
          layer.setLatLng([feature.geometry.coordinates[1], feature.geometry.coordinates[0]]);
          state.markersLayer.addLayer(layer);
          layer.dragging.enable();

          // Event handlers for work unit points
          layer.on('dragend', () => {
            const latlng = layer.getLatLng();
            updateMarkerPositionInGeoJSON(feature.properties.id, latlng);
          });
          layer.on('mouseover', () => {
            layer.bindTooltip(`
              <div style="text-align: left; font-size: 14px; line-height: 1.4;">
                  <strong>Name:</strong> ${workunit.name} <br>
                  <strong>Coordinates:</strong> 
                  <br>Latitude: ${feature.geometry.coordinates[1].toFixed(6)}
                  <br>Longitude: ${feature.geometry.coordinates[0].toFixed(6)}
                </div>
            `, { permanent: false, direction: 'top' }).openTooltip();
          });
          layer.on('mouseout', () => layer.closeTooltip());
          layer.on('click', () => showEditModal(layer, feature.properties.id, 'Point'));
        }
      }
    },
    style: (feature) => {
      // Style configuration for Polygons
      if (feature.geometry.type === 'Polygon') {
        return {
          color: feature.properties.strokeColor || '#000000',
          weight: feature.properties.strokeWidth || 2,
          opacity: feature.properties.strokeOpacity || 1,
          fillColor: feature.properties.fillColor || '#ff0000',
          fillOpacity: feature.properties.fillOpacity || 1,
        };
      }
      return {}; // Empty style for non-Polygon features
    },
  }).addTo(state.map);
};

const getStep = (value) => {
      if (value === null || value === undefined) return 0.001; // Default step
      const decimalPlaces = value.toString().split('.')[1]?.length || 0;
      return Math.pow(10, -decimalPlaces); // Step based on the number of decimal places
    };
const addTextElementsFromGeoJson = async (geoJsonData) => {
  // Fetch equipment data from the API
  let equipmentData = await getAPI(TmsEquipmentApi).apiTmsTmsequipmentListGet();

  // Check if geoJsonData has features
  if (!geoJsonData || geoJsonData.features.length === 0) {
    // Clear existing text elements if geoJsonData.features is empty
    textElements.value = [];
    return; // Exit the function since there are no features to process
  }
  // Ensure textElements is cleared or ready for new elements
  textElements.value = []; // Clear existing text elements
  // Create a set to keep track of added IDs to avoid duplicates
  const addedIds = new Set();
  geoJsonData.features.forEach((feature, index) => {
    if (feature.geometry.type === 'Point') {
      // Find the equipment type from the EquipmentList based on the feature ID
      const equipment = equipmentData.data.result.find(item => item.id === feature.properties.id);

      // Create the new text element
      const newElement = {
        id: feature.properties.id,
        type: 'text',
        content: equipment ? equipment.description : '', // Use equipment type description if available
        placeholder: 'Enter text',
        x: feature.geometry.coordinates[0], // Only for Point geometry
        y: feature.geometry.coordinates[1], // Only for Point geometry
        label: equipment ? equipment.name : feature.properties.name || `Row ${index + 1}`, // Use equipment name or default label
      };

      // Check for duplicates before adding
      if (!addedIds.has(newElement.id)) {
        textElements.value.push(newElement); // Add the new element to the textElements array
        addedIds.add(newElement.id); // Mark this ID as added
      } else {
        console.warn(`Duplicate element with ID ${newElement.id} not added.`);
      }
    }
  });
};



// Show the edit modal for a feature
const showEditModal = (layer: L.Layer, id: string, type: string) => {
  state.editFeature = {
    id,
    status: state.editFeature.status,
    strokeColor: (layer as any).options.strokeColor || '#000000',
    strokeWidth: (layer as any).options.strokeWidth || 2,
    strokeOpacity: (layer as any).options.strokeOpacity || 1,
    fillColor: (layer as any).options.fillColor || '#ff0000',
    fillOpacity: (layer as any).options.fillOpacity || 1,
    type,
    layer,
  };

  state.editDialogVisible = true;
};

// Save edited feature
const saveEdit = () => {
  const { id, type, layer, ...props } = state.editFeature;
  const feature = state.geoJsonData.features.find((f) => f.properties?.id === id);
  if (feature) {
    Object.assign(feature.properties, props);
    if (type === 'Polygon') {
      const polygonLayer = layer as L.Polygon;
      polygonLayer.setStyle({
        color: props.strokeColor,
        weight: props.strokeWidth,
        opacity: props.strokeOpacity,
        fillColor: props.fillColor,
        fillOpacity: props.fillOpacity,
      });
    }
    if (type === 'Point') {
      // Update marker icon based on status
      const marker = layer as L.Marker;
      const status = props.status;
      
      // Define icons for different statuses
   
    }

    state.editDialogVisible = false;
  }
};

// Remove feature from map and GeoJSON data
const removeFeature = () => {
  const { layer, id, type } = state.editFeature;

  if (type === 'Polygon') {
    state.polygonsLayer.removeLayer(layer as L.Polygon);
  } else if (type === 'Point') {
    state.markersLayer.removeLayer(layer as L.Marker);
  }

  state.geoJsonData.features = state.geoJsonData.features.filter((feature) => feature.properties.id !== id);

  state.editDialogVisible = false;
  renderGeoJson();
};

// Add a new marker to the map
const addMarker = async () => {
  if (state.map) {
    let selectedEquipmentId ="";
    if(state.queryParams.equipment){
      selectedEquipmentId = state.queryParams.equipment;
    }else{
      selectedEquipmentId = state.queryParams.WorkContact;
    }

    if (!selectedEquipmentId) {
      ElMessage.warning('Please select equipment before adding a marker.');
      return;
    }

    const objectType = state.objectType.find(item => item.code === state.queryParams.objectType);
    let iconUrl, name, EquipmentLocation;

    try {
      switch (state.queryParams.objectType) {
        case TmsMapLocationRefType.Equipment: {
          const   equipment= state.Equipment.find(item => item.id === selectedEquipmentId);
          const equipmentType = state.EquipmentTypeList.find(item => item.id === equipment?.equipmentTypeId);
          iconUrl = equipmentType?.defaultIcon ?? 'default-icon.png';
          name = equipment?.name;
          EquipmentLocation = equipment ? `${equipment.kmNumber}+${equipment.metNumber}` : 'Unknown Location';
          break;
        }
        case TmsMapLocationRefType.WorkUnit: {
          iconUrl = Image;
          break;
        }
        default:
          return;
      }
    } catch (error) {
      console.error('Error fetching equipment data:', error);
      return;
    }

    // Create a custom icon for the marker
    const customIcon = L.icon({
      iconUrl: iconUrl,
      iconSize: [32, 32],
      iconAnchor: [16, 32],
    });

    // Create and add marker to the map with the custom icon
    const marker = L.marker(state.map.getCenter(), { icon: customIcon, draggable: true }).addTo(state.map);
    const id = selectedEquipmentId;
    marker['tempPosition'] = marker.getLatLng();

    // Convert marker to GeoJSON and add custom properties
    const geoJson = marker.toGeoJSON();
    geoJson.properties = {
      id,
      type: objectType?.code || 'unknown',
    };
    state.geoJsonData.features.push(geoJson);
    state.markersLayer.addLayer(marker);
    state.queryParams.equipment = id;

    // Define the new element for `textElements`
    const newIndex = textElements.value.length + 1;
    const newElement = {
      id: id,
      name: name,
      type: 'text',
      content: '', // Ensure `content` is defined here
      placeholder: 'Enter text',
      x: marker.getLatLng().lng,
      y: marker.getLatLng().lat,
      label: `Row ${newIndex}`,
    };
    textElements.value.push(newElement);
    // Marker click event to show edit modal
    marker.on('click', () => showEditModal(marker, id, 'Point'));

    // Drag end event to update the position in GeoJSON and UI
    marker.on('dragend', (event) => {
      const marker = event.target;
      const position = marker.getLatLng();
      updateMarkerPositionInGeoJSON(id, position);
      const elementToUpdate = textElements.value.find(el => el.id === newElement.id);
      if (elementToUpdate) {
        elementToUpdate.x = position.lng;
        elementToUpdate.y = position.lat;
      }
    });
  }
};

const updateMarkerPosition = (element) => {
  const marker = state.geoJsonData.features.find(
    (f: any) => f.properties.id === element.id
  );
  if (marker) {
    const newLatLng = L.latLng(element.y, element.x);
    updateMarkerPositionInGeoJSON(marker.properties.id, newLatLng); // Sync with GeoJSON
  }
};


const updateMarkerPositionInGeoJSON = (id: string, newLatLng: L.LatLng) => {
  // Find the feature in GeoJSON that matches the marker's ID
  const feature = state.geoJsonData.features.find(
    (f: any) => f.geometry.type === 'Point' && f.properties.id === id
  );

  // If the feature is found, update the coordinates
  if (feature) {
    // Update GeoJSON coordinates
    feature.geometry.coordinates = [newLatLng.lng, newLatLng.lat];
    console.log(`Marker with ID ${id} updated to new position:`, feature.geometry.coordinates);

    // Update the marker's position on the map
    const marker = state.markersLayer.getLayers().find((layer: any) => layer.options.id === id);
    if (marker) {
      marker.setLatLng(newLatLng);
      console.log(`Marker with ID ${id} moved on the map to:`, newLatLng);
    }

    // Update the associated icon's position if it exists
    const iconMarker = state.markersLayer.getLayers().find((layer: any) => layer.options.id === id && layer instanceof L.Marker);
    if (iconMarker) {
      iconMarker.setLatLng(newLatLng); // Move the icon to the new position
      console.log(`Icon for marker with ID ${id} moved on the map to:`, newLatLng);
    }
  } else {
    console.warn(`Marker with ID ${id} not found in GeoJSON.`);
  }
  update();
};





// Start drawing a new polygon
const startDrawingPolygon = () => {
  if (state.map) {
    (state.map as any).pm.enableDraw('Polygon', {
      snappable: true,
      snapDistance: 20,
    });
  }
};

/// Author: KhoaND
/// Descriptio: chỉnh sửa để load theo Object Type
/// Created date: 11/10/2024
const handleEquipmentTypeChange = () => {
  watch(() => state.queryParams.objectType, async (objectType) => {
    if (objectType) {
      try {
        let equipmentData;
        
        switch (objectType) {
          case TmsMapLocationRefType.Equipment: // Equipment
              equipmentData = await getAPI(TmsEquipmentTypeApi).apiTmsTmsequipmenttypeListGet();
             state.EquipmentList = equipmentData.data.result;
             handleEquipmentChange();
            break;
          case TmsMapLocationRefType.Zone:// Zone
            equipmentData = await getAPI(TmsZoneApi).apiTmsTmszoneGettreeGet();
            state.EquipmentList = equipmentData.data.result;
            break;
          case TmsMapLocationRefType.WorkUnit: // WorkUnit
            equipmentData = await getAPI(TmsWorkUnitApi).apiTmsTmsworkunitListGet();
             state.EquipmentList = equipmentData.data.result;
             handleWorkUnitChange();
            break;  
          default:
            console.warn(`Unhandled equipment type: ${objectType}`);
            return;
        }

       
      } catch (error) {
        console.error('Error fetching equipment data:', error);
      }
    }
  });
};
const onVmsActionlTypeChange = (ctrlType) => {
    toggleControlDisplay(ctrlType);

};
const toggleControlDisplay = (objType: string) => {
    // ---------
    state.showEquipmentControls = objType == TmsMapLocationRefType.Equipment;    
    state.showWorkUnitControls = objType == TmsMapLocationRefType.WorkUnit;
    state.showZoneControls = objType == TmsMapLocationRefType.Zone;
}
const handleEquipmentChange = () => {
  watch(() => state.queryParams.typeId, async (typeId) => {
    if (typeId) {
      try {
        let equipmentData;

        const equipment = state.EquipmentList.find(x=>x.id === state.queryParams.typeId);
        switch (equipment.code) {
          case TmsEquipmentType.Vms: // Equipment
            equipmentData = state.Equipment.filter(x => x.equipmentTypeId === state.queryParams.typeId);
            break;
          case TmsEquipmentType.FCam ||TmsEquipmentType.TCam || TmsEquipmentType.PCam  :// Zone
            equipmentData = state.Equipment.filter(x => x.equipmentTypeId === state.queryParams.typeId);
            break;
          default:
            console.warn(`Unhandled equipment type: ${equipment.code}`);
            return;
        }
        state.EquipmentData = equipmentData;
        renderGeoJson();
      } catch (error) {
        console.error('Error fetching equipment data:', error);
      }
    }
  });
};
const handleWorkUnitChange = () => {
  watch(() => state.queryParams.typeId, async (typeId) => {
    if (typeId) {
      try {
        let workunitcontactData;
        
        const workunitcontact = await getAPI(TmsWorkContactApi).apiTmsTmsworkcontactListGet();
        workunitcontactData = workunitcontact.data.result.filter(x => x.refId === typeId);
        state.WorkContactList = workunitcontactData;
        renderGeoJson();
      } catch (error) {
        console.error('Error fetching equipment data:', error);
      }
    }
  });
};
// Submit the current GeoJSON data
const submit = async () => {
  state.ruleForm = {
    geoJson: JSON.stringify(state.geoJsonData), // Convert geoJsonData to JSON string
    mapId: state.queryParams.mapId,
    name: state.queryParams.mapId, 
    status: 1, 
  };

  try {
    // Validate the form
    await ruleFormRef.value?.validate();

    // Call the API to submit the form
    await getAPI(TmsMapLocationApi).apiTmsTmsmaplocationAddPost(state.ruleForm);
    
    ElMessage.success($t('lz.message.base.updateSuccess'));

    // Fetch map data after successful submission
    const mapId = state.queryParams.mapId;
    if (mapId) {
      const mapLocation = await getAPI(TmsMapLocationApi).apiTmsTmsmaplocationDatalistIdGet(mapId);
      const mapData = mapLocation.data.result[0];

      // Check if map data exists
      for (const feature of state.geoJsonData.features) {
      const { type } = feature.geometry;

      if (type === 'Point') {
        // Xử lý Point
        const [x, y] = feature.geometry.coordinates;
        const mapDetail = {
          name: feature.properties.id,
          code: feature.properties.id,
          mapLocationId: mapData.id,
          refId: feature.properties.type,
          refType: feature.properties.objectType,
          coordX: String(x),
          coordY: String(y),
        };

        // Gọi API để thêm mới MapDetail cho Point
      } else if (type === 'Polygon') {
        // Xử lý Polygon
        const coordinates = feature.geometry.coordinates;
        const mapDetail = {
          name: feature.properties.id,
          code: feature.properties.id,
          mapLocationId: mapData.id,
          refId: feature.properties.type,
          refType: feature.properties.objectType,
          coordinates: JSON.stringify(coordinates), // Lưu toàn bộ tọa độ của Polygon
        };

        // Gọi API để thêm mới MapDetail cho Polygon
      }
    }
    }
  } catch (error) {
    // Handle validation or submission errors
    if (error instanceof Error) {
      ElMessage.error($t('lz.message.base.updateError'));
      console.error('Error submitting form:', error);
    }
  }
};


// update the current GeoJSON data
const update = async () => {
  // Cập nhật ruleForm với các trường cần thiết
  state.ruleForm = {
    geoJson: JSON.stringify(state.geoJsonData), // Chuyển đổi geoJsonData thành chuỗi JSON
    mapId: state.queryParams.mapId,
    name: state.queryParams.mapId, // Điều chỉnh nếu cần
    status: 1, // Điều chỉnh nếu cần
  };

  // Xác thực và gửi form
  try {
    // Xác thực form
    await ruleFormRef.value?.validate();
    state.ruleForm.id = state.id;

    // Gọi API để cập nhật thông tin bản đồ
    await getAPI(TmsMapLocationApi).apiTmsTmsmaplocationUpdatePost(state.ruleForm);

    ElMessage.success($t('lz.message.base.updateSuccess'));
  } catch (error) {
    // Xử lý lỗi xác thực hoặc lỗi khi gửi form
    if (error instanceof Error) {
      ElMessage.error($t('lz.message.base.updateError'));
      console.error('Lỗi khi gửi form:', error);
    }
  }
  renderGeoJson();
};






</script>

<style scoped>
.map-container {
  position: relative;
  width: 100%;
  height: 500px;
}
.custom-div-icon {
  background: none !important; /* Loại bỏ nền */
  border: none !important; /* Loại bỏ viền */
  padding: 0 !important; /* Loại bỏ đệm */
  margin: 0 !important; /* Loại bỏ khoảng cách */
  box-shadow: none !important; /* Loại bỏ bóng */
}
</style>
