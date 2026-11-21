<template>
    <div ref="mapContainer" style="height: 100%">
        <!-- Loading indicator -->
        <el-loading v-model="state.isLoading" />
    </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted, watch, reactive } from 'vue';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import '@geoman-io/leaflet-geoman-free';
import '@geoman-io/leaflet-geoman-free/dist/leaflet-geoman.css';
import 'leaflet-rotate';
import { getAPI } from '/@/utils/axios-utils';
import {
    TmsMapApi,
    TmsMapLocationApi,
    TmsEquipmentApi,
    TmsEquipmentTypeApi,
    TmsZoneApi,
    TmsMapDetailApi,
    TmsWorkUnitApi
} from '/@/api-services/tms';
import Image from '/@/assets/toll-road.png';
import { useThemeConfig } from '/@/stores/themeConfig';

const themeConfig = useThemeConfig();

const props = defineProps<{
    mapId?: string;
}>();

const state = reactive({
    isLoading: false,
    isDataLoaded: false,
    MapList: [] as any[],
    EquipmentList: [] as any[],
    EquipmentTypeList: [] as any[],
    ZonesList: [] as any[],
    ZonesDataList: [] as any[],
    WorkUnitList: [] as any[],
    geoJsonData: {
        type: "FeatureCollection",
        features: [] as any[],
    },
    mapViewState: {
        center: [10.89484, 107.47839] as [number, number],
        zoom: 14
    }
});

const mapContainer = ref();
const map = ref<L.Map | null>(null);
const markersLayer = ref<L.LayerGroup>();
const polygonsLayer = ref<L.LayerGroup>();

// Initialize map
const initMap = () => {
    map.value = L.map(mapContainer.value, {
        center: state.mapViewState.center,
        zoom: state.mapViewState.zoom,
        maxZoom: 18,
        crs: L.CRS.EPSG3857,
        zoomControl: false,
        attributionControl: false,
        rotate: true
    });

    // Set base tile pane to lowest z-index
    const tilePane = map.value.getPane('tilePane');
    if (tilePane) {
        tilePane.style.zIndex = '100';
    }

    // Keep map layers below trafficInfo floating modals (1500) and application dialogs (2000+).
    map.value.createPane('polygonsPane');
    map.value.createPane('markersPane');
    map.value.createPane('labelsPane');

    // Preserve the map layer order without entering the overlay z-index range.
    const polygonsPane = map.value.getPane('polygonsPane');
    const markersPane = map.value.getPane('markersPane');
    const labelsPane = map.value.getPane('labelsPane');

    if (polygonsPane) polygonsPane.style.zIndex = '300';
    if (markersPane) markersPane.style.zIndex = '400';
    if (labelsPane) labelsPane.style.zIndex = '500';

    // Add base tile layer
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 18,
        pane: 'tilePane'
    }).addTo(map.value);

    // Initialize layer groups
    markersLayer.value = L.layerGroup().addTo(map.value);
    polygonsLayer.value = L.layerGroup().addTo(map.value);
};

// Fetch all necessary data
const fetchData = async () => {
    try {
        state.isLoading = true;
        state.isDataLoaded = false;

        // Fetch all data using Promise.all for parallel requests
        const [
            mapData,
            equipmentTypeData,
            equipmentData,
            zoneData,
            zoneDataList,
            workUnitData
        ] = await Promise.all([
            getAPI(TmsMapApi).apiTmsTmsmapListGet(),
            getAPI(TmsEquipmentTypeApi).apiTmsTmsequipmenttypeListGet(),
            getAPI(TmsEquipmentApi).apiTmsTmsequipmentListGet(),
            getAPI(TmsZoneApi).apiTmsTmszoneGettreeGet(),
            getAPI(TmsZoneApi).apiTmsTmszoneListGet(),
            getAPI(TmsWorkUnitApi).apiTmsTmsworkunitListGet()
        ]);

        // Update state with fetched data
        state.MapList = mapData?.data?.result || [];
        state.EquipmentTypeList = equipmentTypeData?.data?.result || [];
        state.EquipmentList = equipmentData?.data?.result || [];
        state.ZonesList = zoneData?.data?.result || [];
        state.ZonesDataList = zoneDataList?.data?.result || [];
        state.WorkUnitList = workUnitData?.data?.result || [];

        // If mapId is provided, load map data
        if (props.mapId) {
            await loadMapData(props.mapId);
        }

        state.isDataLoaded = true;
        console.log('Data loaded successfully:', {
            maps: state.MapList.length,
            equipmentTypes: state.EquipmentTypeList.length,
            equipment: state.EquipmentList.length,
            zones: state.ZonesList.length,
            zonesData: state.ZonesDataList.length,
            workUnits: state.WorkUnitList.length
        });

    } catch (error) {
        console.error('Error fetching data:', error);
    } finally {
        state.isLoading = false;
    }
};

// Load map data for specific map ID
const loadMapData = async (mapId: string) => {
    if (!mapId || !map.value) return;

    try {
        state.isLoading = true;

        // Clear existing layers first
        if (markersLayer.value) {
            markersLayer.value.clearLayers();
            map.value.removeLayer(markersLayer.value);
        }
        if (polygonsLayer.value) {
            polygonsLayer.value.clearLayers();
            map.value.removeLayer(polygonsLayer.value);
        }

        // Reinitialize layer groups
        markersLayer.value = L.layerGroup().addTo(map.value);
        polygonsLayer.value = L.layerGroup().addTo(map.value);

        const mapLocation = await getAPI(TmsMapLocationApi).apiTmsTmsmaplocationDatalistIdGet(mapId);
        const mapData = mapLocation?.data?.result?.[0];

        // Áp dụng rotation từ config nếu có
        if (mapData?.config) {
            try {
                const config = JSON.parse(mapData.config);
                if (config.rotation != null && map.value && typeof (map.value as any).setBearing === 'function') {
                    (map.value as any).setBearing(config.rotation);
                }
                // Áp dụng center, zoom từ config nếu có
                if (config.center && config.zoom) {
                    map.value.setView(config.center, config.zoom);
                }
                if (config.minZoom != null) {
                    map.value.setMinZoom(config.minZoom);
                }
                if (config.maxZoom != null) {
                    map.value.setMaxZoom(config.maxZoom);
                }
            } catch (e) {
                console.warn('Invalid config in map location:', e);
            }
        }

        if (mapData?.geoJsonContent) {
            state.geoJsonData = JSON.parse(mapData.geoJsonContent);
            console.log('GeoJSON data loaded:', state.geoJsonData);

            // Calculate bounds from features
            if (state.geoJsonData.features.length > 0) {
                const bounds = L.geoJSON(state.geoJsonData).getBounds();
                map.value.fitBounds(bounds, { padding: [50, 50] });

                // Force render after data is loaded
                renderGeoJson();
            }
        } else {
            state.geoJsonData = { type: "FeatureCollection", features: [] };
        }

    } catch (error) {
        console.error('Error loading map data:', error);
        state.geoJsonData = { type: "FeatureCollection", features: [] };
    } finally {
        state.isLoading = false;
    }
};

// Get item details helper function
const getItemDetails = (id: string, objectType: string | undefined) => {
    console.log(id, objectType);
    try {
        if (!id || !objectType) {
            return { name: 'Unknown', iconUrl: Image };
        }
        console.log(state.EquipmentList)
        switch (objectType) {
            case 'device': {
                const equipment = state.EquipmentList
                    .find(item => item.id === id);

                if (!equipment) return { name: 'Unknown Equipment', iconUrl: Image };

                const equipmentType = state.EquipmentTypeList
                    .find(type => type.id === equipment.equipmentTypeId);
                return {
                    name: equipment.name + ' (Km ' + equipment.kmNumber + '+' + equipment.metNumber + ')',
                    iconUrl: equipmentType?.defaultIcon || Image,
                    type: equipmentType?.name || 'equipment',
                    unit: equipment.unit || '',
                    measurement: equipment.measurement || 0
                };
            }

            case 'zone': {
                const zone = state.ZonesDataList
                    .find(item => item.id === id);

                if (!zone) return { name: '', iconUrl: Image };

                return {
                    name: zone.name,
                    iconUrl: Image,
                    type: zone.type || 'zone',
                    unit: 'm²',
                    measurement: zone.area || 0
                };
            }

            case 'unit': {
                const workUnit = state.WorkUnitList
                    .find(item => item.id === id);

                if (!workUnit) return { name: 'Unknown Work Unit', iconUrl: Image };

                return {
                    name: workUnit.name,
                    iconUrl: Image,
                    type: workUnit.type || 'unit',
                    unit: workUnit.unit || '',
                    measurement: workUnit.measurement || 0
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

// Render GeoJSON data on the map
const renderGeoJson = () => {
    if (!map.value || !state.isDataLoaded) {
        console.log('Map or data not ready:', {
            mapReady: !!map.value,
            dataLoaded: state.isDataLoaded
        });
        return;
    }

    console.log('Rendering GeoJSON with features:', state.geoJsonData.features.length);

    // Clear existing layers
    if (markersLayer.value) {
        markersLayer.value.clearLayers();
        map.value.removeLayer(markersLayer.value);
    }
    if (polygonsLayer.value) {
        polygonsLayer.value.clearLayers();
        map.value.removeLayer(polygonsLayer.value);
    }

    // Reinitialize layer groups
    markersLayer.value = L.layerGroup().addTo(map.value);
    polygonsLayer.value = L.layerGroup().addTo(map.value);

    state.geoJsonData.features.forEach((feature, index) => {
        const id = feature.properties?.id;
        const objectType = feature.properties?.objectType;
        const itemDetails = getItemDetails(id, objectType);

        console.log(`Rendering feature ${index + 1}:`, { id, objectType, itemDetails });

        if (feature.geometry.type === 'Point') {
            const [lng, lat] = feature.geometry.coordinates;

            // Add marker
            L.marker([lat, lng], {
                icon: L.icon({
                    iconUrl: itemDetails.iconUrl,
                    iconSize: [32, 32],
                    iconAnchor: [16, 32]
                })
            }).addTo(markersLayer.value);

            // Add label
            L.marker([lat, lng], {
                icon: L.divIcon({
                    html: `
                        <div class="marker-label-content">
                            <div class="marker-name">${itemDetails.name}</div>
                            ${itemDetails.measurement ?
                            `<div class="marker-measurement">${itemDetails.measurement} ${itemDetails.unit || ''}</div>`
                            : ''}
                        </div>
                    `,
                    className: 'marker-label',
                    iconSize: [200, 40],
                    iconAnchor: [100, -10]
                })
            }).addTo(markersLayer.value);

        } else if (feature.geometry.type === 'Polygon') {
            const coords = feature.geometry.coordinates[0].map(([lng, lat]: number[]) => [lat, lng]);

            // Add polygon
            const polygon = L.polygon(coords, {
                color: feature.properties?.strokeColor || themeConfig.themeConfig.primary,
                weight: feature.properties?.strokeWidth || 2,
                opacity: feature.properties?.strokeOpacity || 1,
                fillColor: themeConfig.themeConfig.primary,
                fillOpacity: feature.properties?.fillOpacity || 0.2
            }).addTo(polygonsLayer.value);

            // Add polygon label at center
            const center = polygon.getBounds().getCenter();
            L.marker(center, {
                icon: L.divIcon({
                    // html: `
                    //     <div class="polygon-label-content">
                    //         <div class="polygon-name">${itemDetails.name}</div>
                    //     </div>
                    // `,
                    className: 'polygon-label',
                    iconSize: [200, 40],
                    iconAnchor: [100, 20]
                })
            }).addTo(polygonsLayer.value);
        }
    });

    // Add layers to map
    if (polygonsLayer.value) polygonsLayer.value.addTo(map.value);
    if (markersLayer.value) markersLayer.value.addTo(map.value);
};

// Update the watch to ensure renderGeoJson is called when data changes
watch(() => state.isDataLoaded, (isLoaded) => {
    if (isLoaded) {
        renderGeoJson();
    }
});

watch(() => state.geoJsonData.features, () => {
    if (state.isDataLoaded) {
        renderGeoJson();
    }
}, { deep: true });

// Watch for changes in mapId prop
watch(() => props.mapId, async (newMapId) => {
    if (newMapId && map.value) {
        await loadMapData(newMapId);
    }
}, { immediate: true });

onMounted(async () => {
    console.log('Component mounted');
    await initMap();
    await fetchData();
});

onUnmounted(() => {
    if (map.value) {
        map.value.remove();
    }
});

// Expose necessary methods and data
defineExpose({
    map,
    state,
    renderGeoJson
});
</script>

<style scoped>
/* Keep all map panes below trafficInfo overlays. */
:deep(.leaflet-pane.leaflet-tile-pane) {
    z-index: 100 !important;
}

:deep(.leaflet-pane.leaflet-polygonsPane) {
    z-index: 300 !important;
}

:deep(.leaflet-pane.leaflet-markersPane) {
    z-index: 400 !important;
}

:deep(.leaflet-pane.leaflet-labelsPane) {
    z-index: 500 !important;
}

:deep(.custom-marker) {
    z-index: 400 !important;
}

:deep(.marker-label),
:deep(.polygon-label) {
    z-index: 500 !important;
}

.marker-label-content,
.polygon-label-content {
    pointer-events: none;
    background: white;
    border: 1px solid #ddd;
    border-radius: 4px;
    padding: 2px 6px;
    font-size: 12px;
    white-space: nowrap;
    box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.marker-name {
    font-weight: bold;
}

.marker-measurement {
    color: #666;
    font-size: 11px;
}

.polygon-label {
    background: transparent;
    border: none;
}

.polygon-label-content {
    background: rgba(255, 255, 255, 0.9);
    border-radius: 4px;
    padding: 2px 6px;
    font-size: 12px;
    font-weight: bold;
    white-space: nowrap;
    box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

/* Add these important styles */
:deep(.leaflet-tile-pane) {
    z-index: 1 !important;
}

:deep(.leaflet-overlay-pane) {
    z-index: 200 !important;
}

:deep(.leaflet-marker-pane) {
    z-index: 400 !important;
}

:deep(.marker-label-content),
:deep(.polygon-label-content) {
    pointer-events: none;
    background: white;
    border: 1px solid #ddd;
    border-radius: 4px;
    padding: 2px 6px;
    font-size: 12px;
    white-space: nowrap;
    box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}
</style>
