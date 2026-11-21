<template>
<div class="map-container">
 <VdsEventDetail ref="vdsEventDetail" />
  <div id="map" style="height: 500px; position: relative;">
      <simplePopupComp ref="simplePopupCompRef" style="position: absolute; z-index: 1000;"/>
      <trafficInfoPopupComp ref="trafficInfoPopupCompRef" />
      <workUnitPopup ref="workUnitPopupRef" />
      <button 
        @click.prevent="refreshMapData" 
        style="position: absolute; bottom: 20px; left: 20px; padding: 10px 15px; font-size: 16px; color: white; background-color: rgba(0, 123, 255, 0.7); border: none; border-radius: 4px; cursor: pointer; transition: background-color 0.3s ease; z-index: 1000;">
        Refresh Map Data
      </button>
    </div>
  </div>
</template>

<script lang="ts" setup>
const VdsEventDetail = defineAsyncComponent(() => import('/@/components/vds/VdsEventDetail.vue'));
import { useVdsEventMap } from '/@/transporter/hooks/useVdsEventMap';
import { ref, computed, reactive, toRaw, onMounted, watch,defineAsyncComponent } from 'vue';
import { useI18n } from 'vue-i18n';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useDateFormat } from '@vueuse/core'
import simplePopupComp from './equipmentInfoPopup.vue';
import Image from '/@/assets/performance.png';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import '@geoman-io/leaflet-geoman-free';
import '@geoman-io/leaflet-geoman-free/dist/leaflet-geoman.css';
import { getAPI } from '/@/utils/axios-utils';
import { SysConfigDataApi } from '/@/api-services/cfgSystem';
import { TmsMapApi, TmsMapLocationApi, TmsEquipmentApi, TmsEquipmentTypeApi, TmsZoneApi,TmsIncidentApi,TmsEventTypeApi,TmsTrafficInfoApi,TmsWorkContactApi  } from '/@/api-services/tms';
import { VmsCurrentApi } from '/@/api-services/vms';
import { UpdateTmsMapLocationInput,PageTmsIncidentOutput,PageTmsIncidentInput } from '/@/api-services/tms';
import { TmsEquipmentType,TmsTrafficInfoMap,TmsMapLocationRefType } from '/@/types/enums/tmsEnum';
import { TmsWorkUnitType } from '/@/types/enums';
import { ElMessageBox,ElMessage, ElForm } from 'element-plus';
import { useBroadcastChannel } from '@vueuse/core';
import ZonePopupComp from './simplePopupZone.vue';

const workUnitPopup = defineAsyncComponent(() => import('./workUnitPopup.vue'));
const workUnitPopupRef = ref<InstanceType<typeof workUnitPopup>>();

const trafficInfoPopupComp = defineAsyncComponent(() => import('./trafficInfoPopup.vue'));
const trafficInfoPopupCompRef = ref<InstanceType<typeof trafficInfoPopupComp>>();

const zonePopupCompRef = ref(null);
const xGridIncident = ref<VxeGridInstance>();
const simplePopupCompRef = ref(null);
const { t: $t } = useI18n();
const ruleFormRef = ref<InstanceType<typeof ElForm>>();
const state = reactive({
  queryParams: {
    name: undefined,
    code: undefined,
    mapId: undefined,
    equipmentType: undefined,
    equipmentId: undefined,
    pid: undefined,
    urlBySocket: undefined,
  },
  localPageParam: {
		pageSize: 50 as number,
		defaultSort: { field: 'orderNo', order: 'asc', descStr: 'desc' },
	},
  id: '',
  showWorkDetailIdRow: false,
  MapList: [] as any[],
  EquipmentList: [] as any[],
  VmsCurrentList: [] as any[],
  EquipmentTypeList: [] as any[],
  Equipment: [] as any[],
  EquipmentData: [] as any[],
  WorkContactList: [] as any[],
  WorkContactListData: [] as any[],
  ZonesList: [] as any[],
  IncidentList: [] as any[],
  IncidentWithZones: [] as any[],
  sourceDetectList: [] as any,
	eventTypeList: [] as any,
	locationList: [] as any,
	laneList: [] as any,
	zoneList: [] as any,
	incidentStatusList: [] as any,
  map: null as L.Map | null,
  markersLayer: L.layerGroup() as L.LayerGroup<L.Marker>,
  polygonsLayer: L.layerGroup() as L.LayerGroup<L.Polygon>,
  tileLayer: null as L.TileLayer | null,
  imageOverlay: null as L.ImageOverlay | null,
  ruleForm: {} as UpdateTmsMapLocationInput,
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


onMounted(async() => {
  const mapData = await getAPI(TmsMapApi).apiTmsTmsmapListGet();
    state.MapList = mapData?.data?.result || [];

    // Fetch Equipment Type List data
    
    const equipmentTypeData = await getAPI(TmsEquipmentTypeApi).apiTmsTmsequipmenttypeListGet();
    state.EquipmentTypeList = equipmentTypeData?.data?.result || [];
 // Fetch Incident List data
    const incidentData = await getAPI(TmsIncidentApi).apiTmsTmsincidentListPost();
    state.IncidentList = incidentData?.data?.result || [];

    const incidentZoneData = await getAPI(TmsTrafficInfoApi).apiTmsTmstrafficinfoCheckincidentexistsPost();
    state.IncidentWithZones = incidentZoneData?.data?.result || [];
    // Fetch Zones List data
    const zoneData = await getAPI(TmsZoneApi).apiTmsTmszoneGettreeGet();
    state.ZonesList = zoneData?.data?.result || [];

    let response = await getAPI(SysConfigDataApi).apiCfgsystemSysconfigdataDatalistCodeGet('tms_source_detect');
		state.sourceDetectList = response.data.result;

		response = await getAPI(SysConfigDataApi).apiCfgsystemSysconfigdataDatalistCodeGet('tms_location');
		state.locationList = response.data.result;

		response = await getAPI(SysConfigDataApi).apiCfgsystemSysconfigdataDatalistCodeGet('tms_lane');
		state.laneList = response.data.result;

		response = await getAPI(SysConfigDataApi).apiCfgsystemSysconfigdataDatalistCodeGet('tms_incident_status');
		state.incidentStatusList = response.data.result;

		response = await getAPI(TmsEventTypeApi).apiTmsTmseventtypeListGet();
		state.eventTypeList = response.data.result;
    response = await getAPI(VmsCurrentApi).apiVmsVmscurrentListGet();
		state.VmsCurrentList = response.data.result;
    response = await getAPI(TmsWorkContactApi).apiTmsTmsworkcontactListGet();
    state.WorkContactListData = response.data.result;
    response = await getAPI(TmsEquipmentApi).apiTmsTmsequipmentListGet();
    state.Equipment= response.data.result;
    handleEquipmentTypeChange();

    // Initialize map and set watchers
    initializeMap();
});
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

const showWorkUnitPopup = async(workUnitId: string) => {
  workUnitPopupRef.value?.toggleWorkUnitPopup(true, {objectId: workUnitId});
}

const showTrafficInfoPopup = (zoneId: string, ) => {
    trafficInfoPopupCompRef.value?.toggleTrafficDataPopup(true, {objectId: zoneId});
}

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
          } else {
            // Handle when no data is found
            state.geoJsonData = {
              type: "FeatureCollection",
              features: [] as GeoJSON.Feature[],
            };
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



const refreshMapData = () => { 
  renderGeoJson();
  };
// setInterval(refreshMapData, 5000);

// Render GeoJSON data on the map
const renderGeoJson = async  () => {
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
    onEachFeature:async  (feature, layer) => {
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

        state.IncidentWithZones.forEach((incidentId) => {
          // Check if the feature's properties id is in the list of incidents
          if (feature.properties.id === incidentId) {
            const iconUrl = 'icon-url.png';
            const iconDiv = L.divIcon({
              className: 'polygon-icon',
              html: `<img src="${iconUrl}" style="width:32px; height:32px; position: absolute; transform: translate(-50%, -50%);" />`,
              iconSize: [32, 32]
            });
                  layer.setStyle({
                    color: '#000000',
                    weight:  2,
                    opacity:  1,
                    fillColor:  '#ff0000',
                    fillOpacity:  1
                  });
            // Create a new marker for each incident
            const iconMarker = L.marker([centroid[0], centroid[1]], { icon: iconDiv });
            iconMarker._zIndex = 1000;

            // Add marker to the markers layer
            state.markersLayer.addLayer(iconMarker);
          }
        });

        // Find associated zone and child by feature ID
        const zone = state.ZonesList.find(z => 
          z.children.some(child => child.id === feature.properties.id)
        );
        const zoneChild = zone?.children.find(x => x.id === feature.properties.id);

        // Set up polygon event handlers
          layer.on('click', () => 
          showTrafficInfoPopup(feature.properties.id)
          );
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
        if (feature.properties.itemGroup == TmsMapLocationRefType.Equipment) {
          // Get equipment type and icon
          const incidentZoneData = await getAPI(TmsTrafficInfoApi).apiTmsTmstrafficinfoGetequipmenttypebyequipmentidEquipmentIdGet(selectedEquipmentId);
          const iconUrl = incidentZoneData.data.result.equipmentState === 1 
            ? incidentZoneData.data.result.equipmentType.activeIcon 
            : incidentZoneData.data.result.equipmentType.inactiveIcon;
          const currentMap = state.MapList.find(item => item.id === state.queryParams.mapId);
          let response = await getAPI(VmsCurrentApi).apiVmsVmscurrentListGet();

		      const VmsCurrentList = response.data.result;
          const currentVms = VmsCurrentList.find(item=> item.equipmentId === feature.properties.id);
          let icon;
          if (currentMap && currentMap.mode === TmsTrafficInfoMap.Vms && incidentZoneData.data.result.equipmentType.code === TmsEquipmentType.Vms ) {
            icon = L.divIcon({
              className: 'custom-div-icon', // Add your custom class here
              html: `
                <div style="text-align: center; background-color: transparent; margin-left: -45px">
                  <svg id="Layer_1" data-name="Layer 1" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 841.89 490.28"
                    style="height: 3vw; width: 6vw; scale: 2.5;">
                    <g>
                        <linearGradient id="SVGID_1_" gradientUnits="userSpaceOnUse" x1="10.124" y1="294.8612" x2="831.7664" y2="294.8612">
                            <stop offset="0.1275" style="stop-color:#6CB0D0" />
                            <stop offset="0.2104" style="stop-color:#9ABDD5" />
                            <stop offset="0.2972" style="stop-color:#BCC6D8" />
                            <stop offset="0.3773" style="stop-color:#D2CCD9" />
                            <stop offset="0.4475" style="stop-color:#DFD0DA" />
                            <stop offset="0.5001" style="stop-color:#E3D1DB" />
                            <stop offset="0.5407" style="stop-color:#E0CFDA" />
                            <stop offset="0.59" style="stop-color:#D6CAD9" />
                            <stop offset="0.6438" style="stop-color:#C6C2D7" />
                            <stop offset="0.7008" style="stop-color:#ADB6D4" />
                            <stop offset="0.7603" style="stop-color:#87A6CF" />
                            <stop offset="0.8211" style="stop-color:#4992C9" />
                            <stop offset="0.8422" style="stop-color:#228BC7" />
                        </linearGradient>
                        <path   style="fill: ${equipment.equipmentState === 1 ? 'url(#SVGID_1_)' : '#E30613'};"
                              d="M786.38,168.79H55.51c-25.07,0-45.39,20.32-45.39,45.39v161.37c0,25.07,20.32,45.39,45.39,45.39h730.87c25.07,0,45.39-20.32,45.39-45.39V214.18C831.77,189.11,811.45,168.79,786.38,168.79z M811.52,375.54c0,13.86-11.28,25.14-25.14,25.14H55.51c-13.86,0-25.14-11.28-25.14-25.14V214.18c0-13.86,11.28-25.14,25.14-25.14h730.87c13.86,0,25.14,11.28,25.14,25.14V375.54z" />
                        
                        <image xlink:href="${state.queryParams.equipmentId === feature.properties.id ? state.queryParams.urlBySocket : (currentVms ? currentVms.url + '?t=' + Math.random() : '')}" x="40" y="200" width="760.88" height="200"></image>
                        <g>
                            <path style="fill:#7CC1A0;" d="M106.13,168.79H55.51c-25.07,0-45.39,20.32-45.39,45.39l0,50.62c0,2.8-2.27,5.06-5.06,5.06S0,267.6,0,264.8v-50.62c0-30.61,24.9-55.52,55.51-55.52h50.62c2.8,0,5.06,2.27,5.06,5.06S108.93,168.79,106.13,168.79z" />
                            <path style="fill:#7CC1A0;" d="M735.76,168.79h50.62c25.07,0,45.39,20.32,45.39,45.39l0,50.62c0,2.8,2.27,5.06,5.06,5.06s5.06-2.27,5.06-5.06v-50.62c0-30.61-24.9-55.52-55.51-55.52h-50.62c-2.8,0-5.06,2.27-5.06,5.06S732.96,168.79,735.76,168.79z" />
                            <path style="fill:#7CC1A0;" d="M106.13,420.93H55.51c-25.07,0-45.39-20.32-45.39-45.39l0-50.62c0-2.8-2.27-5.06-5.06-5.06S0,322.12,0,324.92l0,50.62c0,30.61,24.9,55.52,55.51,55.52h50.62c2.8,0,5.06-2.27,5.06-5.06S108.93,420.93,106.13,420.93z" />
                            <path style="fill:#7CC1A0;" d="M735.76,420.93h50.62c25.07,0,45.39-20.32,45.39-45.39l0-50.62c0-2.8,2.27-5.06,5.06-5.06s5.06,2.27,5.06,5.06v50.62c0,30.61-24.9,55.52-55.51,55.52h-50.62c-2.8,0-5.06-2.27-5.06-5.06S732.96,420.93,735.76,420.93z" />
                        </g>
                    </g>
                </svg>
                  <div style="font-size: 12px; margin-top: 2px; color: black; text-align: center;">
                   ${equipment.name} Km(${equipment.kmNumber}+${equipment.metNumber})
                  </div>
                </div>
              `,
              iconSize: [32, 42],  // Total size of the icon + label
              iconAnchor: [16, 32], // Anchor point of the icon
              popupAnchor: [0, -32] // Anchor point of the popup
            });
          }else{
            icon = L.divIcon({
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
          }
          

          layer.setIcon(icon);
          layer.setLatLng([feature.geometry.coordinates[1], feature.geometry.coordinates[0]]);
          state.markersLayer.addLayer(layer);
          layer.dragging.enable();

          // Event handlers for equipment points
          layer.on('mouseover', () => {
            const stateTag = equipment.equipmentState === 1 
              ? '<span style="color: green; font-weight: bold;">Enabled</span>' 
              : '<span style="color: red; font-weight: bold;">Disabled</span>';

            layer.bindTooltip(`
              <div style="text-align: left; font-size: 14px; line-height: 1.4;">
                ${incidentZoneData.data.result.equipmentType.code === TmsEquipmentType.Vms 
                    ? `<img src="${currentVms.url}" /><br>` 
                    : ''
                }
                <strong>Name:</strong> ${equipment.name} <br>
                <strong>State:</strong> ${stateTag} <br>
                <strong>Location:</strong> Km(${equipment.kmNumber}+${equipment.metNumber}) <br>
                <strong>Coordinates:</strong>
                <br>Latitude: ${feature.geometry.coordinates[1].toFixed(6)}
                <br>Longitude: ${feature.geometry.coordinates[0].toFixed(6)}
              </div>
            `, { permanent: false, direction: 'top' }).openTooltip();
          });

          layer.on('mouseout', () => layer.closeTooltip());
            if(incidentZoneData.data.result.equipmentState === 1){
                layer.on('click', () => 
              simplePopupCompRef?.value.toggleMyPopup(true, {
                pointId: feature.properties.id,
                type: feature.properties.itemGroup
              })
            );
          }
          
        } else {
          // Handle Work Unit as default
          const workunit = state.WorkContactListData.find(item => item.id === selectedEquipmentId);
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
          layer.on('click', () => 
            showWorkUnitPopup(feature.properties.id)
            
          );
        }
      }
    },
    style: (feature) => {
      // Style configuration for Polygons
      if (feature.geometry.type === 'Polygon') {
       state.IncidentWithZones.forEach((incidentId) => {
          // Check if the feature's properties id is in the list of incidents
          if (feature.properties.id === incidentId) {
           return {
              color:  "#000000",
              weight:  2,
              opacity:  1,
              fillColor: "#FF0000",
              fillOpacity:  1,
            };
          }
              return {
                color: feature.properties.strokeColor || '#000000',
                weight: feature.properties.strokeWidth || 2,
                opacity: feature.properties.strokeOpacity || 1,
                fillColor: feature.properties.fillColor || '#ff0000',
                fillOpacity: feature.properties.fillOpacity || 1,
              };
        });

    
      }
      return {}; // Empty style for non-Polygon features
    },
  }).addTo(state.map);
};

const showEditModal = (layer: L.Layer, id: string, type: string) => {
  state.editFeature = {
    id,
    type: type,
    layer,
    status: state.editFeature.status,
    strokeColor: '#000000',
    strokeWidth: 2,
    strokeOpacity: 1,
    fillColor: '#ff0000',
    fillOpacity: 1,
  };
  state.editDialogVisible = true;
};

const handleEquipmentTypeChange = async () => {
  // Fetch Equipment List data
  const equipmentData = await getAPI(TmsEquipmentApi).apiTmsTmsequipmentListGet();
  state.EquipmentList = equipmentData?.data?.result || [];
};

// Call this function to update markers and polygons
const updateGeoJsonData = () => {
  state.geoJsonData.features = [];
  state.markersLayer.eachLayer((layer) => {
    const geoJson = layer.toGeoJSON();
    geoJson.properties = { ...geoJson.properties, id: state.queryParams.pid };
    state.geoJsonData.features.push(geoJson);
  });
  state.polygonsLayer.eachLayer((layer) => {
    const geoJson = layer.toGeoJSON();
    geoJson.properties = { ...geoJson.properties, id: state.queryParams.pid };
    state.geoJsonData.features.push(geoJson);
  });
};

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

const removeFeature = (layer: L.Layer) => {
  if (layer instanceof L.Marker) {
    state.markersLayer.removeLayer(layer);
  } else if (layer instanceof L.Polygon) {
    state.polygonsLayer.removeLayer(layer);
  }
  updateGeoJsonData();
};
const handleFinished = async (row) => {  
  try {
    // Cập nhật trực tiếp giá trị trong đối tượng row
    row.state = "4";
    // Gửi request API để cập nhật thông tin trên server
    await getAPI(TmsIncidentApi).apiTmsTmsincidentUpdatePost(row);
    // Hiển thị thông báo thành công
    this.$message({
      message: 'Cập nhật thành công!',
      type: 'success',
    });
  } catch (error) {
    // Xử lý lỗi và hiển thị thông báo lỗi
    console.error('Cập nhật không thành công:', error);
    this.$message({
      message: 'Có lỗi xảy ra khi cập nhật!',
      type: 'error',
    });
  }
};
// Update the state of a record
	const handleUpdateState = (row: any) => {
		ElMessageBox.confirm($t('Are you sure to change the status of this record to completed?', { name: row.name }), $t('message.common.warn'), {
			confirmButtonText: $t('message.common.confirm'),
			cancelButtonText: $t('message.common.cancel'),
			type: 'warning',
		})
		.then(async () => {
			await getAPI(TmsIncidentApi).apiTmsTmsincidentUpdatestatePost({ id: row.id, state: '5' }); // State = 4: Đã xử lý hoàn tất
			handleQuery();
			ElMessage.success($t('message.common.updateSuccess'));
		})
		.catch(() => { });
	};


// ========================================================

// Begin- Add by DangKhoa
import { useMonitorVms } from '/@/stores/its/monitorVms'; // Import bien toan cuc vao de su dung
import { normalizeVmsPayload, getVmsEquipmentId } from '/@/transporter/hooks/useTransporterVms';

const monitorVms = useMonitorVms();
// End- Add by DangKhoa

watch(() => [monitorVms.url, monitorVms.eqipmentId], () => {

  renderGeoJson();
});
// =========================================================




defineExpose({ renderGeoJson });

// Export renderGeoJson so it can be called from outside

const vdsEventDetail = ref<InstanceType<typeof VdsEventDetail>>();
useVdsEventMap(() => toRaw(state.map) as L.Map | null, item => vdsEventDetail.value?.open(item));
</script>

<style scoped>
.map-container {
  position: relative;
  width: 100%;
  height: 500px;
}
.container {
  display: flex;
  flex-direction: row; /* Arrange children side by side */
  justify-content: space-between; /* Space out the left and right containers */
  align-items: flex-start; /* Align items to the start of the cross axis (top) */
}

.left-container {
  flex: 3; /* Larger width for the left container */
  margin-right: 5px;
}

.right-container {
  flex: 1; /* Smaller width for the right container */
  display: flex;
  flex-direction: column; /* Arrange items in a column */
}
card-container {
  display: flex;
  gap: 20px; /* Space between cards */
}
.main-card {
  margin-top: 5px;
  width: 30%; /* Đảm bảo thẻ chiếm toàn bộ chiều rộng của phần tử chứa */
  max-width: 500px; /* Hoặc chiều rộng tối đa bạn muốn */
}

.card-content {
  margin-top: 5px;
  display: flex;
  flex-direction: column; /* Đặt hướng của các phần tử con thành dọc */
  align-items: center; /* Canh giữa các phần tử con theo chiều ngang */
  text-align: center; /* Canh giữa văn bản trong thẻ */
}

.card-item {
  width: 100%;
  margin-top: 5px;
 /* Đảm bảo thẻ con chiếm toàn bộ chiều rộng của phần tử chứa */
}

.card-text {
  font-size: 18px; /* Điều chỉnh kích thước chữ */
  margin-top: 10px; /* Thêm khoảng cách phía trên văn bản */
}
.xGrid-style {
  height: 338px; /* Hoặc giá trị phù hợp với kích thước mong muốn */
  overflow: auto; /* Để hiển thị thanh cuộn nếu cần */
}
.header {
  padding: 10px 20px;
  background-color: #f5f5f5;
}

.logo {
  height: 50px;
}

.center-content {
  text-align: center;
}

.right-content {
  display: flex;
  align-items: center;
  justify-content: flex-end;
}

.weather-info {
  margin-right: 20px;
}

.el-dropdown-link {
  cursor: pointer;
}
.horizontal-layout {
  display: flex;
  align-items: center;
}

.weather-info {
  display: flex;
  align-items: center;
  margin-left: 10px; /* Optional: Add space between avatar and text */
}

.info-text {
  margin-left: 10px; /* Optional: Add space between avatar and text */
}</style>
