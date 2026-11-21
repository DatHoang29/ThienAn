import { useMapStore } from '/@/stores/its/mapStore';
import { generateRandomNumber } from '/@/utils/randomHelper.js';

/**
 * Composable xử lý lọc thiết bị/zone/unit/sự kiện hiển thị trên bản đồ.
 *
 * Lưu ý: state của LeafletMap.vue được truyền vào bằng tham chiếu (reactive
 * object) vì nhiều field (filterDeviceItems, previousFeatureGeoJsonData,
 * previousFeatureGeoJsonDataZone, listIdZoneInEventType, isRefreshing,
 * geoJsonData) còn được đọc/ghi bởi template, handleSave, loadMapData,
 * renderPolygon... nên không tách rời khỏi state chung của component.
 */
export function useMapFilters(deps: { state: any; renderGeoJson: (item?: any) => Promise<void>; checkShowIconName: () => void }) {
    const { state, renderGeoJson, checkShowIconName } = deps;
    const mapStore = useMapStore();

    // #region Feature Visibility Helpers

    /**
     * Lọc dữ liệu theo item group (device/unit).
     * Ẩn feature không thuộc listIdFilter bằng cách chuyển vào
     * state.previousFeatureGeoJsonData, và phục hồi lại nếu được chọn lại.
     */
    const filterFeaturesByItemGroup = (itemGroup: string, listIdFilter: any[]) => {
        // Get list id in feature json with item group is device
        const listIdInFeatureGeoJson = state.geoJsonData.features
            .map((feature: any) => {
                if (feature?.properties?.itemGroup == itemGroup) {
                    return feature?.properties?.id;
                }
            })
            .filter((item: any) => item != undefined);

        // Get list id in previous feature json with item group is device
        // Check exist object in list previous feature json
        const listIdInPreviousFeatureGeoJson = [] as any;
        if (Object.keys(state.previousFeatureGeoJsonData).length !== 0) {
            state.previousFeatureGeoJsonData.map((feature: any) => {
                if (feature?.properties?.itemGroup == itemGroup) {
                    listIdInPreviousFeatureGeoJson.push(feature?.properties?.id);
                }
            });
        }

        // Filter in geoJsonData
        // Remove item if equipmentId not in listIdFilter
        listIdInFeatureGeoJson.forEach((equipmentId: any) => {
            if (!listIdFilter.includes(equipmentId) && equipmentId != undefined) {
                // remove feature from geoJson
                // 1. Find the index equipment to remove
                const indexToRemove = state.geoJsonData?.features.findIndex(
                    (feature: any) => feature?.properties.id == equipmentId && feature?.properties?.itemGroup == itemGroup
                );

                // 2. Store previouse object remove
                const featureToRemove = state.geoJsonData?.features[indexToRemove];

                // check not exist in previousFeatureGeoJsonData
                // Then add to list previousFeatureGeoJsonData
                if (listIdInPreviousFeatureGeoJson.length > 0 && !listIdInPreviousFeatureGeoJson.includes(featureToRemove?.properties?.id)) {
                    state.previousFeatureGeoJsonData.push(featureToRemove);
                } else if (listIdInPreviousFeatureGeoJson.length == 0) {
                    state.previousFeatureGeoJsonData.push(featureToRemove);
                }

                // 3. remove item from array geoJsonData
                if (indexToRemove > -1) {
                    state.geoJsonData?.features.splice(indexToRemove, 1);
                }
            }
        });

        // Restore items was remove
        if (!state.isRefreshing && listIdInPreviousFeatureGeoJson && listIdInPreviousFeatureGeoJson.length > 0) {
            listIdInPreviousFeatureGeoJson.forEach((equipmentId: any) => {
                if (listIdFilter.includes(equipmentId) && equipmentId != undefined) {
                    // remove feature from geoJson
                    // 1. Find the feature to add
                    const featureToAdd = state.previousFeatureGeoJsonData.find(
                        (feature: any) => feature?.properties.id == equipmentId && feature?.properties?.itemGroup == itemGroup
                    );

                    // 2. Check if featureGeoJson not include id then add to list geoJsonData
                    if (!listIdInFeatureGeoJson.includes(featureToAdd?.properties?.id)) {
                        state.geoJsonData.features.push(featureToAdd);
                    }

                    // 3. Remove into list previousFeatureGeoJsonData
                    if (state.previousFeatureGeoJsonData && state.previousFeatureGeoJsonData.length > 0) {
                        // find index to remove
                        const indexToRemove = state.previousFeatureGeoJsonData.findIndex(
                            (feature: any) => feature?.properties.id == equipmentId && feature?.properties?.itemGroup == itemGroup
                        );

                        // remove from array previous feature
                        state.previousFeatureGeoJsonData.splice(indexToRemove, 1);
                    }
                }
            });
        }
    };

    // #endregion

    // #region Apply Filters (device / zone / unit / event)

    /**
     * Hàm xử lý lọc phần mềm (device/zone/unit/event) rồi render lại map.
     */
    const applyFilters = async (filters: any) => {
        state.randomFourNumber = generateRandomNumber(5);
        state.filterDeviceItems = JSON.parse(JSON.stringify(filters || {}));

        if (Object.keys(state.filterDeviceItems).length === 0) {
            await renderGeoJson();
            checkShowIconName();
            return;
        }

        for (const key in state.filterDeviceItems) {
            switch (key) {
                // --- Lọc theo loại thiết bị (device) ---
                case 'device': {
                    const listIdInEquipmentTypeFilter_device = mapStore.equipmentList
                        .map((equipment: any) => {
                            if (
                                equipment.equipmentTypeId != undefined &&
                                state.filterDeviceItems.device &&
                                state.filterDeviceItems.device.includes(equipment?.equipmentTypeId)
                            ) {
                                return equipment?.id;
                            }
                        })
                        .filter((item: any) => item != undefined);

                    filterFeaturesByItemGroup('device', listIdInEquipmentTypeFilter_device);
                    break;
                }

                // --- Lọc zone (bật/tắt hiển thị toàn bộ vùng zone) ---
                case 'zone': {
                    if (filters?.zone && filters?.zone.length > 0 && filters?.zone[0] == 'zone') {
                        const isHaveAnyPolygonInFeatures = state.geoJsonData?.features?.some((feature: any) => feature.properties.itemGroup == 'zone');

                        if (!isHaveAnyPolygonInFeatures && state.previousFeatureGeoJsonDataZone?.length > 0) {
                            state.geoJsonData.features = [...state.geoJsonData.features, ...state.previousFeatureGeoJsonDataZone];
                            state.previousFeatureGeoJsonDataZone = [];
                        }
                    } else {
                        const listFeaturesPolygon = state.geoJsonData?.features
                            .map((feature: any) => {
                                if (feature?.properties?.itemGroup == 'zone') return feature;
                            })
                            .filter((item: any) => item != undefined);

                        if (state.previousFeatureGeoJsonDataZone.length === 0 && listFeaturesPolygon.length > 0) {
                            state.previousFeatureGeoJsonDataZone = [...listFeaturesPolygon];
                        }

                        state.geoJsonData.features = state.geoJsonData.features.filter((feature: any) => feature?.properties?.itemGroup != 'zone');
                    }
                    break;
                }

                // --- Lọc theo đơn vị (work unit) ---
                case 'unit': {
                    const listIdInWorkUnitFilter_unit = mapStore.workUnitList
                        .map((unit: any) => {
                            if (state.filterDeviceItems.unit && state.filterDeviceItems.unit.includes(unit?.id)) {
                                return unit?.id;
                            }
                        })
                        .filter((item: any) => item != undefined);

                    filterFeaturesByItemGroup('unit', listIdInWorkUnitFilter_unit);
                    break;
                }

                // --- Lọc theo loại sự kiện/sự cố (event) ---
                case 'event': {
                    const listIdZoneInEventType = [...mapStore.incidentZoneDataExistInTrafficInfoList, ...mapStore.liveEventIconList].flatMap((item: any) => {
                        const matched = state.filterDeviceItems.event?.includes(item?.eventTypeId);

                        if (matched) {
                            // Chuẩn hóa listZoneId thành mảng (API có thể trả string, number, hoặc array)
                            if (Array.isArray(item?.listZoneId)) {
                                return item.listZoneId;
                            } else if (item?.listZoneId != null && item?.listZoneId !== '') {
                                const raw = String(item.listZoneId);
                                return raw.includes(',') ? raw.split(',').map((s: string) => s.trim()) : [raw];
                            }
                            return [];
                        }
                        return [];
                    });

                    state.listIdZoneInEventType = [...listIdZoneInEventType];
                    break;
                }
            }
        }

        await renderGeoJson();
        checkShowIconName();
    };

    // #endregion

    // #region Public API

    return {
        applyFilters,
        filterFeaturesByItemGroup,
    };

    // #endregion
}
