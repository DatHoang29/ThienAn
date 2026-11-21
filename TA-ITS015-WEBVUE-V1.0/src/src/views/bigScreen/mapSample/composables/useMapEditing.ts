import L from 'leaflet';
import { ElMessage } from 'element-plus';
import { getAPI } from '/@/utils/axios-utils';
import { TmsMapLocationApi } from '/@/api-services/tms';
import { useMapStore } from '/@/stores/its/mapStore';
import { TmsTrafficInfoMap, TmsZoneKmDirection } from '/@/types/enums';
import { buildBoundsByMinZoom } from '../helpers/mapViewUtils';
import { createAnnotationId, HTML_WIDTH_DEFAULT } from '../helpers/annotationUtils';
import { getAnnotationHtmlSummary, hasAnnotationHtmlContent } from '../helpers/annotationHtml';
import { resolveIncidentLineStyle, resolveIncidentZoneGap } from '../helpers/incidentPlacement';
import { normalizeFlowLineColors, resolveFlowLineWidth, resolveZoneBorderWidth } from '../helpers/zoneDisplayStyle';

/**
 * Composable xử lý chỉnh sửa bản đồ: edit/move polygon-marker, vẽ zone mới,
 * theo dõi thay đổi chưa lưu (unsaved changes), và lưu cấu hình map + geojson.
 *
 * clearHighlight/updateMarkerPositionInGeoJSON/handleMarkUnsaved được nhận qua
 * dependency (thay vì định nghĩa lại) vì các hàm này thuộc composable
 * useMapRender — handleMarkUnsaved gọi ngược lại renderGeoJson nên nhóm chung
 * ở render để tránh phụ thuộc vòng.
 *
 * State của LeafletMap.vue được truyền vào bằng tham chiếu (reactive object)
 * vì rất nhiều field (currentMode, editingLayer, geoJsonData, unsavedChanges,
 * mapViewState...) còn được đọc/ghi bởi render, filter, loader, template...
 */
export function useMapEditing(deps: {
    state: any;
    locationEditorRef: any;
    renderGeoJson: (item?: any) => Promise<void>;
    returnGeoDataJsonToComponentIndex: () => void;
    clearHighlight: () => void;
    handleMarkUnsaved: (id: string) => void;
    t: (key: string, params?: any) => string;
}) {
    const { state, locationEditorRef, renderGeoJson, returnGeoDataJsonToComponentIndex, clearHighlight, handleMarkUnsaved, t } = deps;

    // #region Edit/Move Mode Lifecycle

    /**
     * Kết thúc chế độ edit/move hiện tại (nếu có) và render lại map.
     */
    const finishEditing = () => {
        if (!state.currentMode || !state.editingLayer) return;

        if (state.currentMode === 'edit') {
            (state.editingLayer as any).pm?.disable();
        } else if (state.currentMode === 'move') {
            if (state.editingLayer instanceof L.Marker) {
                (state.editingLayer as L.Marker).dragging?.disable();
            } else {
                (state.editingLayer as any).pm?.disable();
            }
        }

        state.currentMode = null;
        state.activeFeatureId = null;
        state.editingLayer = null;
        state.originalCoordinates = null;
        state.currentCoordinates = null;

        renderGeoJson();
    };

    /**
     * Bắt đầu chế độ chỉnh sửa hình dạng (edit vertex) cho 1 polygon zone.
     */
    const handleStartEditing = (row: any) => {
        // Clear any existing editing mode first
        if (state.currentMode) {
            finishEditing();
        }

        state.currentMode = 'edit';
        state.activeFeatureId = row.id;

        const feature = state.geoJsonData.features.find((f: any) => f.properties?.id === row.id);

        if (!feature || !state.map) return;

        // Find the corresponding layer
        let targetLayer: L.Layer | null = null;
        state.polygonsLayer.eachLayer((layer: any) => {
            if (layer.feature?.properties?.id === row.id || layer.options?.id === row.id) {
                targetLayer = layer;
            }
        });
        if (targetLayer && state.map) {
            if (feature.geometry.type === 'Polygon') {
                // Store original coordinates before editing
                state.originalCoordinates = JSON.parse(JSON.stringify(feature.geometry.coordinates));

                // Enable editing with specific styling for edit points
                (targetLayer as any).pm.enable({
                    allowSelfIntersection: false,
                    preventMarkerRemoval: true,
                    mode: 'Edit',
                    vertexStyle: {
                        color: '#ff4444',
                        fillColor: '#ff4444',
                        fillOpacity: 1,
                        radius: 6,
                        weight: 2,
                    },
                    middleMarkerStyle: {
                        color: '#4444ff',
                        fillColor: '#4444ff',
                        fillOpacity: 1,
                        radius: 4,
                        weight: 2,
                    },
                });

                // Store reference to the editing layer
                state.editingLayer = targetLayer;

                // Listen for vertex changes
                (targetLayer as any).on('pm:vertexadded pm:vertexremoved pm:vertexclick pm:markerdragend', (e: any) => {
                    const updatedCoords = e.target.getLatLngs()[0].map((latLng: L.LatLng) => [latLng.lng, latLng.lat]);
                    feature.geometry.coordinates = [updatedCoords];
                    state.currentCoordinates = [updatedCoords];
                    handleMarkUnsaved(row.id);
                });

                // Pan to the feature's center without changing zoom
                const bounds = (targetLayer as any).getBounds();
                state.map.panTo(bounds.getCenter());
            }
        }
    };

    /**
     * Bắt đầu chế độ di chuyển vị trí (drag) cho marker hoặc polygon.
     */
    const handleStartMoving = (row: any) => {
        // Clear any existing editing mode first
        if (state.currentMode) {
            finishEditing();
        }

        state.currentMode = 'move';
        state.activeFeatureId = row.id;

        const feature = state.geoJsonData.features.find((f: any) => f.properties?.id === row.id);
        if (!feature || !state.map) return;

        // Store original coordinates before moving
        state.originalCoordinates = JSON.parse(JSON.stringify(feature.geometry.coordinates));

        // Find the corresponding layer
        let targetLayer: L.Layer | null = null;
        if (feature.geometry.type === 'Point') {
            state.markersLayer.eachLayer((layer: any) => {
                if (layer.feature?.properties?.id === row.id || layer.options?.id === row.id) {
                    targetLayer = layer;
                }
            });
        } else {
            state.polygonsLayer.eachLayer((layer: any) => {
                if (layer.feature?.properties?.id === row.id || layer.options?.id === row.id) {
                    targetLayer = layer;
                }
            });
        }

        if (targetLayer) {
            if (feature.geometry.type === 'Polygon') {
                // Get the bounds of the feature
                const bounds = (targetLayer as any).getBounds?.() || L.latLngBounds((targetLayer as any).getLatLng());

                // Pan to the feature without changing zoom
                state.map.panTo(bounds.getCenter(), {
                    animate: true,
                    duration: 1,
                });

                // Enable dragging for polygon
                (targetLayer as any).pm.enable({
                    draggable: true,
                    allowSelfIntersection: false,
                    preventMarkerRemoval: true,
                    snappable: false,
                    snapDistance: 0,
                    tooltips: false,
                    requireSnapToFinish: false,
                    allowEditing: false,
                    allowRemoval: false,
                    allowRotation: false,
                    allowCutting: false,
                    allowSplitting: false,
                    addVertexOn: 'none',
                    removeVertexOn: 'none',
                    removeVertexPolygon: false,
                });

                // Ensure the layer is added to the map before attaching the event listener
                if (state.map.hasLayer(targetLayer)) {
                    if (state.polygonsLayer && state.polygonsLayer.eachLayer) {
                        state.polygonsLayer.eachLayer((layer: any) => {
                            if (layer.feature?.properties?.id === row.id || layer.options?.id === row.id) {
                                layer?.pm?.enableLayerDrag();
                                layer.on('pm:dragend', (e: any) => {
                                    const updatedCoords = e.target.getLatLngs()[0].map((latLng: L.LatLng) => [latLng.lng, latLng.lat]);
                                    feature.geometry.coordinates = [updatedCoords];
                                    state.currentCoordinates = [updatedCoords];
                                    handleMarkUnsaved(row.id);
                                });
                            }
                        });
                    }
                } else {
                    console.error('Layer is not added to the map');
                }
            } else if (feature.geometry.type === 'Point') {
                if (state.map.hasLayer(targetLayer)) {
                    if (state.markersLayer && state.markersLayer.eachLayer) {
                        state.markersLayer.eachLayer((layer: any) => {
                            if (layer.feature?.properties?.id === row.id || layer.options?.id === row.id) {
                                // Enable dragging for marker.
                                // Marker được render lúc chưa ở chế độ move (draggable=false) nên
                                // handler kéo-thả (layer.dragging) chưa được tạo. Bật draggable rồi
                                // setIcon lại để Leaflet khởi tạo handler, sau đó enable.
                                layer.options.draggable = true;
                                if (!layer.dragging && layer.options.icon) {
                                    layer.setIcon(layer.options.icon);
                                }
                                layer.dragging?.enable();
                                // Add drag event listener
                                layer.on('dragend', (e: any) => {
                                    const position = e.target.getLatLng();
                                    feature.geometry.coordinates = [position.lng, position.lat];
                                    state.currentCoordinates = [position.lng, position.lat];
                                    handleMarkUnsaved(row.id);
                                });
                                state.editingLayer = layer;

                                const latlngMarkerLayer = layer.getLatLng();

                                // Location item
                                state.map?.setView([latlngMarkerLayer.lat, latlngMarkerLayer.lng], state.map?.getZoom());

                                // Add highlight on layer
                                state.highlightLayer = L.circle([latlngMarkerLayer.lat, latlngMarkerLayer.lng], {
                                    color: 'red',
                                    fillColor: '#f03',
                                    fillOpacity: 0.5,
                                    radius: 500,
                                }).addTo(state.map as any);

                                // Xóa highlight sau 200ms
                                setTimeout(() => {
                                    clearHighlight();
                                }, 200);
                            }
                        });
                    }
                } else {
                    console.error('Layer is not added to the map');
                }
            }
        }
    };

    // #endregion

    // #region Vẽ Zone Mới (Leaflet Geoman)

    /**
     * Xử lý khi tạo feature mới thông qua Leaflet Geoman (vẽ polygon zone).
     */
    const handlePmCreate = (e: any): void => {
        const { layer } = e;
        const geoJson = layer.toGeoJSON();
        const id = locationEditorRef.value?.handleGetItemId();
        const name = locationEditorRef.value?.handleGetItemName?.() || '';

        if (!id) {
            layer.remove();
            return;
        }

        if (layer instanceof L.Polygon) {
            const coords = (layer.getLatLngs()[0] as L.LatLng[]).map((latLng: L.LatLng) => [latLng.lng, latLng.lat]);
            geoJson.geometry.coordinates = [coords];
            geoJson.properties = {
                id,
                name,
                itemGroup: locationEditorRef.value?.handleGetItemGroup(),
                strokeColor: 'black',
                strokeWidth: 1,
                strokeOpacity: 1,
                fillOpacity: 0.5,
                // Hướng tính toán chiều Km của polygon (định vị lý trình / sự cố) — mặc định Auto
                kmDirection: TmsZoneKmDirection.Auto,
                // Hướng DI CHUYỂN của tuyến (chiều chảy flow-line) — mặc định Auto (theo laneId)
                flowDirection: TmsZoneKmDirection.Auto,
            };

            state.geoJsonData.features.push(geoJson);
            state.allGeoJsonData.features.push(geoJson);
            state.polygonsLayer.addLayer(layer);

            locationEditorRef.value?.handleItemGroupChange();
        }
    };

    /**
     * Bắt đầu vẽ 1 zone (polygon) mới trên map.
     */
    const handleStartDrawingPolygon = async (item: { typeId: string; group: string }) => {
        if (!state.map) return;

        const selectedItemId = item.typeId;
        if (!selectedItemId) {
            ElMessage.warning(t('lz.label.tmsMapLocation.warningDrawPolygon'));
            return;
        }

        // Tắt tooltip hiển thị "Click to place first vertex"
        state.map.pm.setGlobalOptions({
            tooltips: false,
        });

        state.map.pm.enableDraw('Polygon', {
            snappable: true,
            snapDistance: 20,
        });
    };

    // #endregion

    // #region Thêm Item Mới (device/unit)

    /**
     * Thêm item (device/unit/text/image) mới lên map tại vị trí trung tâm hiện tại, rồi bật chế độ move.
     *
     * itemGroup === 'text': chèn 1 nhãn văn bản tự do (annotation) — không gắn với entity
     * backend nào. id được sinh tại client; nội dung & kiểu hiển thị (chữ, cỡ chữ, xoay,
     * hình dạng, màu) lưu thẳng trong feature.properties và được persist cùng geojson.
     *
     * itemGroup === 'image': chèn 1 hình ảnh tự do — ảnh lưu base64 trong properties.imageUrl.
     *
     * Cả 2 loại đều nhận thêm scaleMode/refZoom: mặc định 'geo' + refZoom = zoom hiện tại,
     * tức kích thước co giãn theo zoom giống như khoảng cách giữa các toạ độ. Nhờ vậy khi
     * liên kết thành cụm (chữ ở trên, mũi tên ở dưới) cả cụm zoom lên xuống như một khối.
     * Xem helpers/annotationUtils.ts.
     */
    const handleAddItem = async (item: any) => {
        if (!state.map) return;

        try {
            const center = state.map.getCenter();
            const currentZoom = Number(state.map.getZoom());

            // --- Nhánh TEXT: nhãn văn bản tự do ---
            if (item.group === 'text') {
                const content = (item.text ?? '').toString().trim();
                if (!content) {
                    ElMessage.warning(t('lz.message.tmsLocation.warningPleaseInputText'));
                    return;
                }
                const textId = item.id || createAnnotationId('text');
                const textFeature = {
                    type: 'Feature',
                    properties: {
                        id: textId,
                        itemGroup: 'text',
                        name: content, // dùng làm nhãn hiển thị trong bảng
                        text: content,
                        fontSize: item.fontSize ?? 14,
                        rotation: item.rotation ?? 0,
                        shape: item.shape ?? 'none',
                        textColor: item.textColor ?? '#1f2d3d',
                        bgColor: item.bgColor ?? '',
                        bold: item.bold ?? true,
                        scaleMode: item.scaleMode === 'fixed' ? 'fixed' : 'geo',
                        refZoom: item.scaleMode === 'fixed' ? undefined : currentZoom,
                        // Mặc định cố định theo bản đồ: xoay bản đồ thì xoay theo.
                        rotateWithMap: item.rotateWithMap !== false,
                    },
                    geometry: {
                        type: 'Point',
                        coordinates: [center.lng, center.lat],
                    },
                };
                state.geoJsonData.features.push(textFeature);
                state.allGeoJsonData.features.push(textFeature);
                state.unsavedChanges.add(textId);
                state.currentMode = 'move';
                state.activeFeatureId = textId;
                state.originalCoordinates = [center.lng, center.lat];
                state.currentCoordinates = [center.lng, center.lat];
                renderGeoJson({ activeLayerId: textId });
                return;
            }

            // --- Nhánh IMAGE: hình ảnh tự do chèn lên bản đồ ---
            // Ảnh được nhúng base64 trong properties.imageUrl nên persist cùng geojson,
            // không cần upload lên file server (giống map dạng ảnh lưu trong TmsMap.url).
            if (item.group === 'image') {
                const imageUrl = (item.imageUrl ?? '').toString().trim();
                if (!imageUrl) {
                    ElMessage.warning(t('lz.message.tmsMapLocation.warningPleaseSelectImage'));
                    return;
                }
                const imageId = item.id || createAnnotationId('image');
                const imageFeature = {
                    type: 'Feature',
                    properties: {
                        id: imageId,
                        itemGroup: 'image',
                        name: item.name || t('lz.label.tmsMapLocation.imageItemGroup'),
                        imageUrl,
                        width: item.width ?? 120,
                        // 0 = chiều cao tự động theo tỉ lệ ảnh gốc
                        height: item.height ?? 0,
                        rotation: item.rotation ?? 0,
                        opacity: item.opacity ?? 1,
                        borderRadius: item.borderRadius ?? 0,
                        scaleMode: item.scaleMode === 'fixed' ? 'fixed' : 'geo',
                        refZoom: item.scaleMode === 'fixed' ? undefined : currentZoom,
                        // Mặc định cố định theo bản đồ: xoay bản đồ thì xoay theo.
                        rotateWithMap: item.rotateWithMap !== false,
                    },
                    geometry: {
                        type: 'Point',
                        coordinates: [center.lng, center.lat],
                    },
                };
                state.geoJsonData.features.push(imageFeature);
                state.allGeoJsonData.features.push(imageFeature);
                state.unsavedChanges.add(imageId);
                state.currentMode = 'move';
                state.activeFeatureId = imageId;
                state.originalCoordinates = [center.lng, center.lat];
                state.currentCoordinates = [center.lng, center.lat];
                renderGeoJson({ activeLayerId: imageId });
                return;
            }

            // --- Nhánh HTML: khối chữ + hình tự do (soạn bằng Jodit) ---
            // HTML lưu trong properties.html; luôn được lọc (sanitize) lúc hiển thị.
            if (item.group === 'html') {
                const html = (item.html ?? '').toString();
                if (!hasAnnotationHtmlContent(html)) {
                    ElMessage.warning(t('lz.message.tmsMapLocation.warningPleaseInputHtml'));
                    return;
                }
                const htmlId = item.id || createAnnotationId('html');
                const htmlFeature = {
                    type: 'Feature',
                    properties: {
                        id: htmlId,
                        itemGroup: 'html',
                        // Tên hiển thị trong bảng vị trí = đoạn chữ thuần rút gọn của nội dung
                        name: item.name || getAnnotationHtmlSummary(html) || t('lz.label.tmsMapLocation.htmlItemGroup'),
                        html,
                        width: item.width ?? HTML_WIDTH_DEFAULT,
                        scale: item.scale ?? 1,
                        rotation: item.rotation ?? 0,
                        scaleMode: item.scaleMode === 'fixed' ? 'fixed' : 'geo',
                        refZoom: item.scaleMode === 'fixed' ? undefined : currentZoom,
                        rotateWithMap: item.rotateWithMap !== false,
                    },
                    geometry: {
                        type: 'Point',
                        coordinates: [center.lng, center.lat],
                    },
                };
                state.geoJsonData.features.push(htmlFeature);
                state.allGeoJsonData.features.push(htmlFeature);
                state.unsavedChanges.add(htmlId);
                state.currentMode = 'move';
                state.activeFeatureId = htmlId;
                state.originalCoordinates = [center.lng, center.lat];
                state.currentCoordinates = [center.lng, center.lat];
                renderGeoJson({ activeLayerId: htmlId });
                return;
            }

            if (!item.id) {
                ElMessage.warning(t('lz.message.tmsLocation.warningPleaseSelectItem'));
                return;
            }

            const newFeature = {
                type: 'Feature',
                properties: {
                    id: item.id,
                    itemGroup: item.group,
                },
                geometry: {
                    type: 'Point',
                    coordinates: [center.lng, center.lat],
                },
            };

            state.geoJsonData.features.push(newFeature);
            state.allGeoJsonData.features.push(newFeature);

            // đánh dấu thay đổi nếu muốn hiện *
            state.unsavedChanges.add(item.id);

            // bật luôn chế độ move cho marker mới thêm
            state.currentMode = 'move';
            state.activeFeatureId = item.id;
            state.originalCoordinates = [center.lng, center.lat];
            state.currentCoordinates = [center.lng, center.lat];

            locationEditorRef.value?.handleResetTypeIdEqualNull();
            locationEditorRef.value?.handleItemGroupChange();

            // render lại để tạo luôn marker + label
            renderGeoJson({ activeLayerId: item.id });
        } catch (error) {
            console.error('Error adding marker:', error);
        }
    };

    // #endregion

    // #region Unsaved Changes & Lưu Cấu Hình

    /**
     * Xóa toàn bộ theo dõi thay đổi chưa lưu, rồi render lại.
     */
    const clearUnsavedChanges = () => {
        state.unsavedChanges.clear();
        state.blockConfigDirty = false;
        renderGeoJson();
    };

    /**
     * Lưu cấu hình map và geojson.
     *
     * Ý nghĩa:
     * - Lưu center, zoom, minZoom, maxZoom
     * - Quan trọng nhất: tính bounds theo minZoom mới user chọn
     * - Bounds này sẽ được runtime dùng để khóa vùng hiển thị map
     */
    const handleSave = async (item: {
        geoJson: string;
        status: number;
        defaultZoom: number;
        maxZoom: number;
        minZoom: number;
        longtitude: number;
        latitude: number;
        rotation?: number;
        boundRadius?: number;
    }) => {
        try {
            // Make sure to finish any active editing before updating
            if (state.currentMode) {
                finishEditing();
            }

            // Save current map view state
            if (state.map) {
                const center = state.map.getCenter();
                const configuredLatitude = Number(item.latitude);
                const configuredLongitude = Number(item.longtitude);

                // Dùng đúng cả latitude và longtitude từ form. Không dùng giá trị
                // center còn lại của Leaflet vì nó có thể thuộc view trước đó.
                const finalCenter: [number, number] = [
                    Number.isFinite(configuredLatitude) ? configuredLatitude : center.lat,
                    Number.isFinite(configuredLongitude) ? configuredLongitude : center.lng,
                ];
                /**
                 * Tính bounds theo minZoom mới.
                 * Đây là vùng sẽ được phép hiển thị ở runtime.
                 */
                const boundsAtMinZoom = buildBoundsByMinZoom(state.map, finalCenter, item.minZoom, 0.03);

                // Assign to mapViewState
                state.mapViewState = {
                    center: finalCenter,
                    zoom: item.defaultZoom,
                    maxZoom: item.maxZoom,
                    minZoom: item.minZoom,
                    rotation: item.rotation ?? state.mapViewState.rotation ?? 0,
                    boundRadius: item.boundRadius ?? state.mapViewState.boundRadius ?? 0,
                };

                const configToSave: any = {
                    ...state.mapViewState,
                    showIconBackground: state.showIconBackground,
                    lockZoom: state.lockZoom === true,
                    lockRotation: state.lockRotation === true,
                    // 0 nghĩa là dùng kích thước cấu hình của từng equipment type.
                    iconSize: Number(state.iconSize) > 0 ? Number(state.iconSize) : 0,
                    // 0 nghĩa là dùng kích thước cấu hình của từng event type.
                    incidentIconSize: Number(state.incidentIconSize) > 0 ? Number(state.incidentIconSize) : 0,
                    incidentZoneGap: resolveIncidentZoneGap(state.incidentZoneGap),
                    incidentLineStyle: resolveIncidentLineStyle(state.incidentLineStyle),
                    zoneBorderWidth: resolveZoneBorderWidth(state.zoneBorderWidth),
                    flowLineColors: normalizeFlowLineColors(state.flowLineColors),
                    flowLineWidth: resolveFlowLineWidth(state.flowLineWidth),
                    bounds: {
                        southWest: [boundsAtMinZoom.getSouthWest().lat, boundsAtMinZoom.getSouthWest().lng],
                        northEast: [boundsAtMinZoom.getNorthEast().lat, boundsAtMinZoom.getNorthEast().lng],
                    },
                };

                if (state.currentMapType === 'image') {
                    configToSave.backgroundColor = state.mapBackgroundColor;
                }

                state.ruleForm = {
                    mapId: state.mapId,
                    name: state.mapId,
                    status: 1,
                    config: JSON.stringify(configToSave),
                };

                // Kiểm tra đang có filter hay không
                const geoJsonDataToSave = JSON.parse(JSON.stringify(state.geoJsonData));
                // Lưu cấu hình block cùng FeatureCollection trong geoJsonContent,
                // không lưu vào config viewport của map nữa.
                if (state.currentMapMode === TmsTrafficInfoMap.Block || state.currentMapMode === TmsTrafficInfoMap.BlockVms) {
                    geoJsonDataToSave.blockConfig = state.blockConfig;
                }
                if (item.geoJson) {
                    if (state.previousFeatureGeoJsonData.length > 0) {
                        geoJsonDataToSave.features = JSON.parse(JSON.stringify([...state.geoJsonData.features, ...state.previousFeatureGeoJsonData]));
                    }
                    // Check nếu zone được filter
                    if (state.previousFeatureGeoJsonDataZone.length > 0) {
                        geoJsonDataToSave.features.push(...state.previousFeatureGeoJsonDataZone);
                    }
                    state.ruleForm.geoJson = JSON.stringify(geoJsonDataToSave);
                }
                await locationEditorRef.value.ruleFormRef?.validate();
                state.ruleForm.id = state.tmsLocationMapId;

                await getAPI(TmsMapLocationApi).apiTmsTmsmaplocationUpdatePost(state.ruleForm);

                // Nạp lại id của record vừa lưu để lần lưu sau update đúng bản ghi (tránh tạo trùng)
                const refreshed = await getAPI(TmsMapLocationApi).apiTmsTmsmaplocationDatalistIdGet(state.mapId);
                state.tmsLocationMapId = refreshed?.data?.result?.[0]?.id ?? state.tmsLocationMapId;

                returnGeoDataJsonToComponentIndex();

                // Tốc độ min/max sửa ở tab Vị trí lưu bằng API riêng của zone, sau khi bản đồ đã lưu.
                // Lỗi ở bước này không làm hỏng phần bản đồ đã lưu; bản nháp được giữ để lưu lại.
                let isZoneSpeedSaved = true;
                try {
                    await useMapStore().saveZoneSpeedDrafts();
                } catch (error) {
                    isZoneSpeedSaved = false;
                    console.error('Error saving zone speed:', error);
                }

                clearUnsavedChanges();
                if (isZoneSpeedSaved) ElMessage.success(t('lz.message.base.saveSuccess'));
                else ElMessage.warning(t('lz.message.tmsMapLocation.zoneSpeedSaveError'));
            }
        } catch (error) {
            if (error instanceof Error) {
                ElMessage.error(t('lz.message.base.saveError'));
                console.error('Error updating:', error);
            }
        }
    };

    // #endregion

    // #region Public API

    return {
        finishEditing,
        handlePmCreate,
        handleStartEditing,
        handleStartMoving,
        clearUnsavedChanges,
        handleSave,
        handleAddItem,
        handleStartDrawingPolygon,
    };

    // #endregion
}
