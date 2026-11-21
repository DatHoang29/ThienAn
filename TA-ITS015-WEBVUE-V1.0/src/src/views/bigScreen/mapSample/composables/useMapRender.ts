import incidentFallbackIcon from '/@/assets/incident-fallback.svg';
import L from 'leaflet';
import { useMapStore } from '/@/stores/its/mapStore';
import { useItsMediaServerStore } from '/@/stores/its/itsMediaServer';
import { TmsMapLocationRefType, TmsTrafficInfoMap, TmsZoneKmDirection } from '/@/types/enums';
import { projectPointToPath, getPolygonCenterline, getPolygonSlice, getIncidentMarkerLatLngAlongZone, getDirectedCenterline, getLaneFlowCenterline, buildArrowHeadPolygon, smoothPolygonCorners, smoothPolylineCorners, getCorridorDisplayCenterline, orientLikeReference, recenterLineInPolygon } from '../helpers/mapGeometry';
import { createCustomDivIcon, ensureZoneSvgDefs } from '../helpers/mapViewUtils';
import { buildEquipmentMarkerIconByType, buildDeviceMarkerIcon, buildUnitMarkerIcon, buildVmsContentTooltip, isImageEquipmentType, resolveEventIconSize } from '../helpers/markerIcons';
import { useMapAnnotations } from './useMapAnnotations';
import { INCIDENT_LINE_DASH_ARRAY, layoutIncidentStacks, resolveIncidentLineStyle, resolveIncidentZoneGap } from '../helpers/incidentPlacement';
import type { IncidentLayoutItem, IncidentStackLayout, ScreenPoint } from '../helpers/incidentPlacement';
import { FLOW_LINE_WIDTH_DEFAULT, resolveFlowLineColor, resolveFlowLineWidth } from '../helpers/zoneDisplayStyle';

// Độ dài đoạn highlight đỏ trên centerline khi có sự cố (% chiều dài centerline, 0.04 = 4%)
const INCIDENT_HIGHLIGHT_SPAN = 0.04;

// #region Types

interface ItemDetails {
    name: string;
    iconUrl: string;
    unit?: string;
    measurement?: number;
    type?: string;
    additionalInfo?: any;
}

interface RenderMapContext {
    id: string;
    itemGroups: any;
    hasUnsavedChanges: boolean;
    itemDetails: ItemDetails;
}

/** 1 icon sự kiện đang hiển thị: vị trí thật + marker/đường nối cần đặt lại khi zoom/xoay. */
interface IncidentPlacementEntry {
    anchorLatLng: L.LatLng;
    polygon: L.Polygon;
    zoneId: string;
    iconSize: number;
    marker: L.Marker;
    leader: L.Polyline;
}

// #endregion

/**
 * Composable render dữ liệu GeoJSON lên bản đồ: marker (device/unit/VMS) và
 * polygon (zone, kèm icon sự cố dọc centerline).
 *
 * Cũng sở hữu các hàm liên quan chặt tới việc render lại (clearHighlight,
 * updateMarkerPositionInGeoJSON, handleMarkUnsaved) vì handleMarkUnsaved gọi
 * ngược lại renderGeoJson — nhóm chung ở đây để tránh phụ thuộc vòng với
 * composable useMapEditing.
 *
 * State của LeafletMap.vue được truyền vào bằng tham chiếu (reactive object)
 * vì render đọc/ghi rất nhiều field chung (geoJsonData, unsavedChanges,
 * currentMode, markersLayer, polygonsLayer...).
 */
export function useMapRender(deps: {
    state: any;
    props: { isEditMode?: boolean; selectedMapId?: string };
    emit: (event: string, payload?: any) => void;
    locationEditorRef: any;
    applyZoneStatusStyle: (
        polygon: any,
        itemDetail: any,
        map?: L.Map | null
    ) => Promise<{ statusColor: string; statusKey: string; flowDuration: number } | void>;
    t: (key: string, params?: any) => string;
    /** Bấm icon sự kiện VDS trực tiếp trên bản đồ. */
    onLiveEventClick?: (item: any) => void;
}) {
    const { state, props, emit, locationEditorRef, applyZoneStatusStyle, t, onLiveEventClick } = deps;
    const mapStore = useMapStore();
    const media = useItsMediaServerStore();

    // #region Shared Render Helpers (highlight, geojson sync, unsaved tracking)

    /**
     * Xóa vòng tròn highlight tạm thời (dùng khi di chuyển marker).
     * Dùng chung bởi render (renderGeoJson) và editing (handleStartMoving).
     */
    const clearHighlight = () => {
        if (state.highlightLayer) {
            state.map.removeLayer(state.highlightLayer);
            state.highlightLayer = null;
        }
    };

    /**
     * Cập nhật vị trí marker trong dữ liệu GeoJSON (khi kéo-thả marker).
     */
    const updateMarkerPositionInGeoJSON = (id: string, newLatLng: L.LatLng): void => {
        const feature = state.geoJsonData.features.find((f: any) => f.geometry.type === 'Point' && f.properties?.id === id);
        if (feature) {
            feature.geometry.coordinates = [newLatLng.lng, newLatLng.lat];
        }
    };

    /**
     * Đánh dấu 1 feature có thay đổi chưa lưu, rồi render lại (giữ nguyên layer đang active).
     */
    const handleMarkUnsaved = (id: string) => {
        state.unsavedChanges.add(id);
        renderGeoJson({ activeLayerId: id });
    };

    /**
     * Bản nhiều-id của handleMarkUnsaved: dùng khi 1 thao tác tác động lên cả cụm
     * annotation (kéo/xoay/resize theo cụm) — chỉ render lại đúng 1 lần.
     */
    const markAnnotationsUnsaved = (ids: string[]) => {
        ids.forEach((id) => state.unsavedChanges.add(id));
        renderGeoJson({ activeLayerId: ids[0] });
    };

    /**
     * Annotation tự do (nhãn văn bản / hình ảnh): render + kéo/xoay/resize + liên kết cụm.
     * Khởi tạo tại đây (không phải ở LeafletMap.vue) vì cần markAnnotationsUnsaved của
     * render, mà bản thân render lại cần renderAnnotationFeature — gom chung để tránh
     * phụ thuộc vòng giữa 2 composable.
     */
    const annotations = useMapAnnotations({
        state,
        props,
        locationEditorRef,
        markUnsaved: markAnnotationsUnsaved,
        t,
    });
    const { renderAnnotationFeature, refreshAnnotations, pruneAnnotationSelection } = annotations;

    /**
     * Gắn các event handler (click/hover/dblclick/rightclick) cho marker/polygon,
     * phát ra ngoài qua emit('item-action', ...).
     */
    const addItemAction = (marker: any, feature: any) => {
        const buildPayload = (actionName: string) => ({
            action: actionName,
            itemId: feature.properties?.id,
            itemGroup: feature.properties?.itemGroup,
            itemData: feature.properties,
            geometry: feature.geometry,
        });

        marker.on('click', () => {
            emit('item-action', buildPayload('click') as any);
            // Ở chế độ edit: scroll đến item trong bảng LocationEditor
            if (props.isEditMode && feature.properties?.id) {
                locationEditorRef.value?.scrollToItem(feature.properties.id, feature.properties.itemGroup);
            }
        });
        marker.on('mouseover', () => emit('item-action', buildPayload('mouseover') as any));
        marker.on('dblclick', () => emit('item-action', buildPayload('dblclick') as any));
        marker.on('rightclick', () => emit('item-action', buildPayload('rightclick') as any));
        marker.on('mouseout', () => emit('item-action', buildPayload('mouseout') as any));
    };

    // #endregion

    // #region Incident Icon Layout

    /**
     * Các icon sự kiện đang nằm trên bản đồ. Đặt vị trí CHUNG cho tất cả (thay vì từng icon
     * tự tính) để những sự kiện sát nhau được gom thành 1 chồng xếp lớp ngay cạnh vị trí thật.
     */
    const incidentPlacements: IncidentPlacementEntry[] = [];
    let incidentLayoutMap: L.Map | null = null;
    let incidentLayoutFrame: number | null = null;

    /** Biên (pixel) của mọi zone đang vẽ, theo id — dùng để icon tránh đè lên zone khác. */
    const projectZoneRings = (map: L.Map) => {
        const rings = new Map<string, ScreenPoint[]>();
        (state.geoJsonData?.features || []).forEach((feature: any) => {
            if (feature.geometry?.type !== 'Polygon') return;
            rings.set(
                String(feature.properties?.id),
                feature.geometry.coordinates[0].map(([lng, lat]: number[]) => map.latLngToContainerPoint(L.latLng(lat, lng)))
            );
        });
        return rings;
    };

    const buildIncidentLayoutItem = (map: L.Map, entry: IncidentPlacementEntry, zoneRings: Map<string, ScreenPoint[]>): IncidentLayoutItem => {
        const element = entry.marker.getElement();
        const scale = element ? Number.parseFloat(getComputedStyle(element).getPropertyValue('--incident-icon-scale')) : 1;
        // Bán kính bao cả icon vuông và padding 4px; kể cả khi icon bị scale lúc zoom.
        const radius =
            ((entry.iconSize + 8) / Math.SQRT2) * Math.max(1, Number.isFinite(scale) ? scale : 1) +
            resolveIncidentZoneGap(state.incidentZoneGap) +
            (entry.polygon.options.weight || 0) / 2;
        const neighboringRings: ScreenPoint[][] = [];
        zoneRings.forEach((ring, zoneId) => zoneId !== entry.zoneId && neighboringRings.push(ring));

        return {
            anchor: map.latLngToContainerPoint(entry.anchorLatLng),
            ring: (entry.polygon.getLatLngs()[0] as L.LatLng[]).map((latLng) => map.latLngToContainerPoint(latLng)),
            radius,
            neighboringRings,
        };
    };

    /** Mỗi lớp cao hơn lớp bên dưới 1 khoảng đủ lớn để thắng chênh lệch z-index theo toạ độ y của Leaflet. */
    const INCIDENT_STACK_Z_STEP = 50;
    const INCIDENT_BASE_Z_OFFSET = 1000;

    /**
     * Đặt vị trí + trạng thái xếp lớp cho từng icon:
     * - Đứng riêng: như cũ.
     * - Trong chồng: lớp 0 trên cùng; mọi lớp đều hiện (không ẩn, không đánh số) và có bóng đổ tách lớp.
     *   Luôn reset trạng thái cũ vì nhóm đổi khi zoom.
     */
    const applyIncidentPositions = (map: L.Map, entries: IncidentPlacementEntry[], layout: IncidentStackLayout) => {
        entries.forEach((entry, index) => {
            const point = layout.points[index];
            const displayLatLng = map.containerPointToLatLng(L.point(point.x, point.y));
            const groupSize = layout.groupSizes[index] || 1;
            const layer = layout.layers[index] ?? 0;
            const isStacked = groupSize > 1;

            entry.marker.setLatLng(displayLatLng);
            entry.marker.setZIndexOffset(INCIDENT_BASE_Z_OFFSET + (isStacked ? (groupSize - layer) * INCIDENT_STACK_Z_STEP : 0));
            entry.leader.setLatLngs([entry.anchorLatLng, displayLatLng]);
            entry.marker.getElement()?.classList.toggle('incident-stack-member', isStacked);
        });
    };

    /** Đặt lại vị trí toàn bộ icon sự kiện (gọi khi zoom/xoay/resize hoặc khi có icon mới). */
    const layoutIncidents = () => {
        if (incidentLayoutFrame != null) {
            cancelAnimationFrame(incidentLayoutFrame);
            incidentLayoutFrame = null;
        }
        const map = incidentLayoutMap;
        if (!map || !incidentPlacements.length) return;

        const zoneRings = projectZoneRings(map);
        const entries = [...incidentPlacements];
        applyIncidentPositions(map, entries, layoutIncidentStacks(entries.map((entry) => buildIncidentLayoutItem(map, entry, zoneRings))));
    };

    const registerIncidentPlacement = (entry: IncidentPlacementEntry) => {
        const map = (entry.marker as any)._map as L.Map | null;
        if (!map) return;
        if (incidentLayoutMap && incidentLayoutMap !== map) incidentLayoutMap.off('zoomend rotate resize', layoutIncidents);
        incidentLayoutMap = map;
        map.on('zoomend rotate resize', layoutIncidents);
        if (!incidentPlacements.includes(entry)) incidentPlacements.push(entry);

        // Đặt ngay icon này như khi đứng 1 mình để không lộ ra trong zone; khung hình kế tiếp
        // mới dàn lại tất cả (gom nhiều icon được thêm liên tiếp vào 1 lần tính).
        applyIncidentPositions(map, [entry], layoutIncidentStacks([buildIncidentLayoutItem(map, entry, projectZoneRings(map))]));
        if (incidentLayoutFrame == null) incidentLayoutFrame = requestAnimationFrame(layoutIncidents);
    };

    const unregisterIncidentPlacement = (entry: IncidentPlacementEntry) => {
        const index = incidentPlacements.indexOf(entry);
        if (index >= 0) incidentPlacements.splice(index, 1);
        if (incidentPlacements.length || !incidentLayoutMap) return;
        incidentLayoutMap.off('zoomend rotate resize', layoutIncidents);
        incidentLayoutMap = null;
        if (incidentLayoutFrame != null) cancelAnimationFrame(incidentLayoutFrame);
        incidentLayoutFrame = null;
    };

    // #endregion

    // #region Polygon Rendering (Zone + Incident Icons)

    /** Trọng tâm polygon = trung bình các đỉnh. */
    const getPolygonCentroid = (polygon: L.Polygon): L.LatLngExpression => {
        const latlngs = polygon.getLatLngs()[0] as L.LatLng[];
        let lat = 0,
            lng = 0;
        for (const ll of latlngs) {
            lat += ll.lat;
            lng += ll.lng;
        }
        return [lat / latlngs.length, lng / latlngs.length];
    };

    /**
     * Author: KienLT
     * Description: HTML phần tốc độ của nhãn zone (màn cấu hình), theo ngưỡng hiệu lực — tính cả bản nháp
     *              đang sửa ở tab Vị trí. Kế thừa mặc định hướng thì nhạt + in nghiêng; thiếu ngưỡng / Min > Max
     *              thì báo đỏ; đang sửa chưa lưu thì viền nét đứt.
     * Created date: 09/10/2026
     */
    const buildZoneSpeedHtml = (zoneId: string): string => {
        const speed = mapStore.getEffectiveZoneSpeed(zoneId);
        if (!speed) return '';
        const classes = ['zone-label-speed'];
        if (!speed.valid) classes.push('is-invalid');
        else if (speed.source !== 'zone') classes.push('is-inherited');
        if (mapStore.zoneSpeedDrafts[String(zoneId)]) classes.push('is-dirty');
        const text = speed.valid
            ? `${speed.min}–${speed.max} km/h`
            : speed.min != null && speed.max != null ? `⚠ ${speed.min} > ${speed.max}` : `⚠ ${t('lz.label.tmsMapLocation.zoneSpeedMissing')}`;
        return `<span class="${classes.join(' ')}">${text}</span>`;
    };

    /**
     * Vẽ minh hoạ HƯỚNG TÍNH TOÁN Km của polygon zone (dùng ở màn hình cấu hình):
     * - Đường tim (centerline) nét đứt đã định hướng: đầu [0] = Km thấp (from),
     *   đầu cuối = Km cao (to).
     * - Mũi tên chỉ về phía Km cao (to).
     * - 2 nhãn "F" / "T" kèm giá trị Km+met tại 2 đầu để xác định rõ bên nào là
     *   from Km/met, bên nào là to Km/met.
     * Hướng lấy từ trường feature.properties.kmDirection (Auto/Forward/Reverse).
     */
    const drawZoneKmDirection = (polygon: any, feature: any) => {
        if (!state.map || !state.polygonsLayer) return;
        // Ẩn toàn bộ minh hoạ lý trình Km nếu người dùng tắt (cấu hình riêng với flow).
        if (state.zoneLabelDisplay?.kmArrow === false) return;

        const kmDirection = feature.properties?.kmDirection || TmsZoneKmDirection.Auto;
        const zone = mapStore.zoneList.find((z: any) => z?.id == feature.properties?.id);
        const directed = getDirectedCenterline(polygon, kmDirection, zone, mapStore.zoneList, state.geoJsonData.features);
        if (directed.length < 2) return;

        const rootStyle = getComputedStyle(document.documentElement);
        const successColor = rootStyle.getPropertyValue('--el-color-success')?.trim() || '#67c23a';
        const dangerColor = rootStyle.getPropertyValue('--el-color-danger')?.trim() || '#f56c6c';

        // Đường tim định hướng kiểu "route line": nét LIỀN mượt (không nét đứt để tránh
        // trông giống đường ray xe lửa). Dùng tông TÍM CHÀM (indigo) — trung tính, không
        // trùng màu lỗi (danger/warning) và tương phản với polygon xanh dương.
        // Để MỎNG + hơi trong để khi zoom nhỏ không đè che hết màu polygon bên dưới.
        const arrowLineColor = '#6366f1'; // indigo cho đường tim
        const arrowFillColor = '#6366f1'; // cùng tông cho đầu mũi tên
        const haloColor = '#ffffff'; // quầng trắng mảnh nền

        // Quầng trắng mảnh (nằm dưới) — chỉ tạo viền nhẹ, không quá dày
        state.polygonsLayer.addLayer(
            L.polyline(directed, {
                color: haloColor,
                weight: 3,
                opacity: 0.5,
                lineCap: 'round',
                lineJoin: 'round',
                interactive: false,
            } as any)
        );

        // Đường tim định hướng (nét LIỀN, mảnh, hơi trong để polygon vẫn thấy được)
        state.polygonsLayer.addLayer(
            L.polyline(directed, {
                color: arrowLineColor,
                weight: 1.8,
                opacity: 0.9,
                lineCap: 'round',
                lineJoin: 'round',
                className: 'zone-km-arrow-line',
                interactive: false,
            } as any)
        );

        // Mũi tên đặc (đa giác cánh diều) tại đầu Km cao (to)
        const toPt = directed[directed.length - 1];
        const fromPt = directed[0];
        const backPt = directed[Math.max(0, directed.length - 3)];
        const arrowPts = buildArrowHeadPolygon(backPt, toPt);
        if (arrowPts.length) {
            state.polygonsLayer.addLayer(
                L.polygon(arrowPts, {
                    color: '#fff',
                    weight: 1.5,
                    opacity: 1,
                    fillColor: arrowFillColor,
                    fillOpacity: 1,
                    lineJoin: 'round',
                    className: 'zone-km-arrow-head',
                    interactive: false,
                } as any)
            );
        }

        // Nhãn 2 đầu: F (from - Km thấp) / T (to - Km cao)
        const fmtKm = (km: any, met: any) => (km != null ? `Km${km}+${met || 0}` : '');
        const makeEndLabel = (latlng: L.LatLng, tag: string, text: string, bg: string) => {
            L.marker(latlng, {
                icon: L.divIcon({
                    className: 'zone-km-dir-label',
                    html: `<div style="position:absolute; left:0; top:0; transform:translate(-50%,-50%); white-space:nowrap; font-size:10px; font-weight:700; letter-spacing:0.3px; color:#fff; background:linear-gradient(135deg, ${bg}, ${bg}cc); padding:2px 7px; border-radius:999px; border:1px solid rgba(255,255,255,0.55); box-shadow:0 2px 6px rgba(0,0,0,0.4), 0 0 10px ${bg}80; pointer-events:none;">${tag}${text ? ' ' + text : ''}</div>`,
                    iconSize: [0, 0],
                    iconAnchor: [0, 0],
                }),
                interactive: false,
                zIndexOffset: 1100,
            }).addTo(state.polygonsLayer as any);
        };
        // Ẩn/hiện nhãn điểm đầu (from) và điểm đuôi (to) theo cấu hình hiển thị.
        const labelOpt = state.zoneLabelDisplay || {};
        if (labelOpt.arrowFrom !== false) {
            makeEndLabel(fromPt, '', zone ? fmtKm(zone.fromKmNumber, zone.fromMetNumber) : '', successColor);
        }
        if (labelOpt.arrowTo !== false) {
            makeEndLabel(toPt, '', zone ? fmtKm(zone.toKmNumber, zone.toMetNumber) : '', dangerColor);
        }
    };

    /**
     * Vẽ minh hoạ HƯỚNG DI CHUYỂN của tuyến (flow) trong màn cấu hình — TÁCH RIÊNG với
     * mũi tên lý trình Km ở trên. Chiều lấy theo getLaneFlowCenterline (laneId + flowDirection
     * của zone), giúp người dùng thấy trực quan chiều "chảy" sẽ hiển thị ở màn theo dõi.
     * Dùng tông HỔ PHÁCH (amber) để phân biệt rõ với mũi tên lý trình (indigo).
     * Bật/tắt độc lập qua state.zoneLabelDisplay.flowArrow.
     */
    const drawZoneFlowDirection = (polygon: any, feature: any) => {
        if (!state.map || !state.polygonsLayer) return;
        if (state.zoneLabelDisplay?.flowArrow === false) return;

        const zone = mapStore.zoneList.find((z: any) => z?.id == feature.properties?.id);
        let flow = getLaneFlowCenterline(
            polygon,
            zone,
            mapStore.zoneList,
            state.geoJsonData.features,
            feature.properties?.flowDirection
        );
        if (flow.length < 2) return;
        // Đường tâm "2 cạnh đầu" (chính giữa dải, không lệch ở 2 đầu), cùng chiều với flow
        const coords = feature.geometry?.coordinates?.[0]?.map(([lng, lat]: number[]) => [lat, lng]) || [];
        const displayFlow = orientLikeReference(recenterLineInPolygon(getCorridorDisplayCenterline(coords), coords), flow);
        if (displayFlow.length >= 2) flow = displayFlow;
        const flowWidth = state.flowLineWidth ? resolveFlowLineWidth(state.flowLineWidth) : FLOW_LINE_WIDTH_DEFAULT;

        // Màu cấu hình theo HƯỚNG (laneId) → xem trước đúng màu sẽ hiện ở màn giám sát;
        // chưa cấu hình thì giữ tông hổ phách (amber) mặc định của màn thiết lập.
        const flowLineColor = resolveFlowLineColor(state.flowLineColors, zone?.laneId) || '#f59e0b';
        const haloColor = '#ffffff';

        // Quầng trắng mảnh nền
        state.polygonsLayer.addLayer(
            L.polyline(flow, {
                color: haloColor,
                weight: flowWidth + 1,
                opacity: 0.45,
                lineCap: 'round',
                lineJoin: 'round',
                interactive: false,
            } as any)
        );
        // Đường tim định hướng flow (nét đứt để phân biệt với mũi tên lý trình nét liền)
        state.polygonsLayer.addLayer(
            L.polyline(flow, {
                color: flowLineColor,
                weight: flowWidth,
                opacity: 0.95,
                dashArray: '6, 6',
                lineCap: 'round',
                lineJoin: 'round',
                className: 'zone-flow-arrow-line',
                interactive: false,
            } as any)
        );
        // Đầu mũi tên tại điểm cuối chiều flow
        const toPt = flow[flow.length - 1];
        const backPt = flow[Math.max(0, flow.length - 3)];
        const arrowPts = buildArrowHeadPolygon(backPt, toPt);
        if (arrowPts.length) {
            state.polygonsLayer.addLayer(
                L.polygon(arrowPts, {
                    color: '#fff',
                    weight: 1.5,
                    opacity: 1,
                    fillColor: flowLineColor,
                    fillOpacity: 1,
                    lineJoin: 'round',
                    className: 'zone-flow-arrow-head',
                    interactive: false,
                } as any)
            );
        }
    };

    /**
     * Vẽ lại các vùng zone, icon sự kiện.
     */
    const renderPolygon = async (feature: any, hasUnsavedChanges: boolean, itemDetails: any, incidentSnapshot: any[] = []) => {
        const id = feature.properties?.id;
        const coords = feature.geometry.coordinates[0].map(([lng, lat]: number[]) => [lat, lng]);
        const colorBorderPolygon = getComputedStyle(document.documentElement).getPropertyValue('--el-color-success');
        const displayName =
            feature.properties?.name || (itemDetails?.name && itemDetails.name !== 'Unknown Zone' ? itemDetails.name : '') || t('lz.label.tmsMapLocation.unNamed'); // Lấy tên hiển thị vùng zone

        // Màn hình cấu hình (mapSample) dùng thiết kế "kính premium" (glass) — fill/stroke
        // gradient nhiều lớp thay cho màu phẳng, tạo chiều sâu và cảm giác cao cấp hơn.
        // Màn theo dõi giao thông giữ nguyên logic cũ (màu sẽ được applyZoneStatusStyle ghi đè).
        const isConfigScreen = !!props.isEditMode;
        // Có strokeColor tuỳ biến từ người dùng → dùng màu phẳng đó thay cho gradient mặc định
        // (gradient premium chỉ áp khi zone dùng màu mặc định của hệ thống).
        const hasCustomColor = !!feature.properties?.strokeColor;
        const premiumColor = feature.properties?.strokeColor || '#22d3ee';
        const fillRef = hasUnsavedChanges ? 'url(#tac-zone-fill-unsaved)' : 'url(#tac-zone-fill-premium)';
        const strokeRef = 'url(#tac-zone-stroke-premium)';
        const polygon = L.polygon(coords, {
            color: isConfigScreen ? (hasCustomColor ? premiumColor : strokeRef) : colorBorderPolygon,
            weight: feature.properties?.strokeWidth || (isConfigScreen ? 2.6 : 0.5),
            opacity: feature.properties?.strokeOpacity ?? (isConfigScreen ? 1 : 0.2),
            fillOpacity: feature.properties?.fillOpacity ?? (isConfigScreen ? 1 : 0.3),
            fillColor: isConfigScreen ? (hasCustomColor ? premiumColor : fillRef) : colorBorderPolygon,
            dashArray: hasUnsavedChanges ? '6, 8' : undefined,
            className: isConfigScreen
                ? 'zone-polygon-fx zone-polygon-premium' + (hasUnsavedChanges ? ' is-unsaved' : '')
                : 'zone-polygon-fx zone-polygon-status',
            lineJoin: 'round',
            lineCap: 'round',
            id: feature.properties?.id || undefined,
            smoothFactor: 1,
        } as any);

        // Nếu polygon này đang ở chế độ move thì cho phép kéo-thả
        if (state.currentMode === 'move' && id === state.activeFeatureId) {
            setTimeout(() => {
                (polygon as any).pm.enableLayerDrag();
                polygon.on('pm:dragend', () => {
                    const updatedCoords = (polygon.getLatLngs()[0] as L.LatLng[]).map((latLng: L.LatLng) => [latLng.lng, latLng.lat]);
                    feature.geometry.coordinates = [updatedCoords];
                    state.currentCoordinates = [updatedCoords];
                    handleMarkUnsaved(id);
                });
            }, 0);
        }

        // --- Nhánh Traffic Info: có trạng thái giao thông + icon sự cố dọc centerline ---
        // Chỉ chạy khi KHÔNG ở chế độ cấu hình (mapSample). isEditMode=true là màn
        // hình cấu hình bản đồ; false là màn hình theo dõi giao thông (traffic info).
        if (!props.isEditMode) {
            const selectedZoneId = feature.properties.id;

            // Get zone in mapStore
            const zone = mapStore.zoneList.find((item: any) => item?.id == selectedZoneId);

            // Tô "Route Ribbon" polygon theo trạng thái giao thông (xem composables/useZoneStatus.ts)
            const zoneStatusResult = await applyZoneStatusStyle(polygon, itemDetails, state.map);

            // Add polygon vào layer TRƯỚC — để incident highlight/marker add sau sẽ nằm TRÊN (SVG z-order)
            addItemAction(polygon, feature);
            state.polygonsLayer?.addLayer(polygon);

            // Viền của zone vẽ thành LỚP RIÊNG (không fill, không nhận chuột, không mang id
            // zone nên refreshVisibleZoneStatusStyles bỏ qua). Hai zone song song thường chồng
            // mép lên nhau một chút: nếu viền nằm trên chính polygon thì nền zone vẽ sau sẽ đè
            // mất viền zone vẽ trước. Cuối renderGeoJson, sendZoneFillsToBack() đẩy toàn bộ nền
            // zone xuống dưới cùng → viền của mọi zone luôn nằm trên nền, không bị che.
            const zoneOutline = L.polygon(coords, {
                fill: false,
                // Màu thật lấy từ CSS (.zone-outline: tông đậm của --zone-color); đây chỉ là dự phòng
                color: '#1e293b',
                weight: 1.5,
                opacity: 0.9,
                interactive: false,
                lineJoin: 'round',
                lineCap: 'round',
                smoothFactor: 1.2, // khớp smoothFactor của polygon (useZoneStatus) để viền trùng mép
                className: 'zone-outline',
            } as any);
            state.polygonsLayer?.addLayer(zoneOutline);

            // Liên kết viền ↔ polygon: applyZoneStatusStyle (kể cả khi refresh realtime) gán
            // --zone-color cho cả viền để viền luôn cùng tông màu trạng thái.
            (polygon as any)._zoneOutline = zoneOutline;
            const outlineEl = (zoneOutline as any).getElement?.() as SVGElement | undefined;
            if (outlineEl && zoneStatusResult?.statusColor) outlineEl.style.setProperty('--zone-color', zoneStatusResult.statusColor);

            // Hover zone → viền chuyển trắng (viền là lớp riêng không nhận chuột nên bật class qua JS)
            polygon.on('mouseover', () => (zoneOutline as any).getElement?.()?.classList.add('is-hover'));
            polygon.on('mouseout', () => (zoneOutline as any).getElement?.()?.classList.remove('is-hover'));

            // Vẽ "flow-line" (dòng chảy) dọc centerline: vì các zone liền kề trên cùng
            // tuyến đều có centerline nối tiếp nhau, vệt dash-chạy này nối liền qua các
            // zone tạo cảm giác MỘT TUYẾN ĐƯỜNG liên tục "đang chảy" — tốc độ chảy chậm
            // dần theo mức độ ùn tắc (xem getFlowDurationByStatus trong useZoneStatus.ts).
            //
            // CHIỀU CHẢY: xác định theo LANE (laneId của zone) — chiều di chuyển của tuyến,
            // ĐỘC LẬP với kmDirection. kmDirection chỉ dùng cho định vị vị trí sự kiện/sự cố
            // (getIncidentMarkerLatLngAlongZone bên dưới). getLaneFlowCenterline định hướng
            // theo chiều Km tăng dần của làn (gom zone cùng laneId), nên các zone cùng làn
            // chảy đồng nhất một chiều liền mạch.
            if (zoneStatusResult) {
                const centerline = getLaneFlowCenterline(
                    polygon,
                    zone,
                    mapStore.zoneList,
                    state.geoJsonData.features,
                    feature.properties?.flowDirection
                );
                if (centerline.length >= 2) {
                    // Vẽ theo đường tâm "2 cạnh đầu" (chạy chính giữa dải, không lệch ở 2 đầu), lấy
                    // chiều theo centerline của làn ở trên; làm mịn khúc gấp cùng kiểu với dải zone.
                    // Căn giữa theo đúng hình đang hiển thị (polygon đã làm mịn ở cuối nhánh này)
                    const displayRing = smoothPolygonCorners(coords);
                    const displayCenterline = orientLikeReference(
                        recenterLineInPolygon(getCorridorDisplayCenterline(displayRing), displayRing),
                        centerline
                    );
                    const flowWidth = resolveFlowLineWidth(state.flowLineWidth);
                    const flowLine = L.polyline(smoothPolylineCorners(displayCenterline.length >= 2 ? displayCenterline : centerline), {
                        // Màu theo HƯỚNG của zone (thiết lập "Màu đường định hướng"), mặc định trắng
                        color: resolveFlowLineColor(state.flowLineColors, zone?.laneId) || '#ffffff',
                        // Gạch ngắn, độ dày theo thiết lập "Độ dày đường định hướng". Chu kỳ 6+9 = 15px
                        // khớp keyframe zone-flow-dash (dịch -30px = 2 chu kỳ) nên chạy liền mạch không
                        // giật; đầu tròn làm gạch dài thêm đúng bằng độ dày nên khe hở vẫn còn tới 6px.
                        weight: flowWidth,
                        opacity: 1,
                        dashArray: '6, 9',
                        lineCap: 'round',
                        lineJoin: 'round',
                        className: 'zone-flow-line',
                        interactive: false,
                    } as any);
                    (flowLine as any)._flowDuration = zoneStatusResult.flowDuration;
                    // Lưu zoneId để LeafletMap chỉ cần đổi tốc độ flow của zone này
                    // khi zoneStatus được refresh, không phải render lại toàn bộ GeoJSON.
                    (flowLine as any)._zoneId = String(selectedZoneId);
                    state.polygonsLayer?.addLayer(flowLine);
                    // Gán duration qua CSS var trên phần tử SVG thật (sau khi đã add vào layer)
                    const setFlowDuration = () => {
                        const el = (flowLine as any).getElement && (flowLine as any).getElement();
                        if (el) el.style.setProperty('--flow-duration', `${zoneStatusResult.flowDuration}s`);
                    };
                    setFlowDuration();
                    setTimeout(setFlowDuration, 0);
                }
            }

            const isExistIncidentZoneDataExistInTrafficInfoList = incidentSnapshot && incidentSnapshot.length > 0;

            // Hiển thị icon sự kiện ở mép ngoài của zone tương ứng
            if (isExistIncidentZoneDataExistInTrafficInfoList && state.map) {
                const incidentsInZone = incidentSnapshot.filter((item: any) => {
                    const matchedEventType = !state.filterDeviceItems?.event
                        ? true
                        : state.filterDeviceItems.event.includes(item?.eventTypeId);

                    // Chuẩn hóa listZoneId thành mảng (API có thể trả string, number, hoặc array)
                    let zoneIds: any[] = [];
                    if (Array.isArray(item?.listZoneId)) {
                        zoneIds = item.listZoneId;
                    } else if (item?.listZoneId != null && item?.listZoneId !== '') {
                        // Trường hợp listZoneId là chuỗi "id1,id2" hoặc giá trị đơn lẻ
                        const raw = String(item.listZoneId);
                        zoneIds = raw.includes(',') ? raw.split(',').map((s: string) => s.trim()) : [raw];
                    }

                    // Dùng String() để tránh type mismatch (number vs string)
                    const matchedZone = zoneIds.length > 0 && zoneIds.some((zid: any) => String(zid) === String(feature.properties.id));

                    return matchedEventType && matchedZone;
                });

                if (incidentsInZone.length > 0) {
                    // Không highlight toàn bộ zone nữa — chỉ hiển thị ring xung quanh từng icon sự cố
                    incidentsInZone.forEach((item: any, index: number) => {
                        const incidentId = item?.incidentId || `incident_${feature.properties.id}_${item.eventTypeId}_${index}`;

                        const eventType = mapStore.eventTypeList.find((eventType: any) => eventType?.id == item.eventTypeId);

                        // Hiển thị icon sự kiện theo loại sự kiện
                        // Sự kiện trực tiếp có loại chưa khai trong danh mục thì dùng icon BE gửi kèm bản tin.
                        const iconUrl = media.eventTypeIcon(eventType ?? (item.liveEvent ? { url: item.liveEvent.icon } : eventType), { bust: state.randomFourNumber });
                        const eventIconSize = resolveEventIconSize(eventType, state.incidentIconSize);
                        const iconPolygon = createCustomDivIcon(iconUrl, 'incident-marker-icon', eventIconSize, incidentFallbackIcon);

                        const markerLatLng = getIncidentMarkerLatLngAlongZone(
                            polygon,
                            index,
                            incidentsInZone.length,
                            item,
                            zone,
                            state.geoJsonData.features,
                            mapStore.zoneList,
                            feature.properties?.kmDirection
                        );

                        const incidentFeature = {
                            type: 'Feature',
                            properties: {
                                id: `${incidentId}_${feature.properties.id}`,
                                itemGroup: 'incident',
                                zoneId: feature.properties.id,
                                zoneName: displayName,
                                eventTypeId: item.eventTypeId,
                                eventTypeName: eventType?.name || item.eventTypeName || '',
                                incidentData: item,
                            },
                            geometry: {
                                type: 'Point',
                                coordinates: [markerLatLng.lng, markerLatLng.lat],
                            },
                        };

                        // Highlight tại lý trình gốc; vị trí icon ngoài biên chỉ là tọa độ hiển thị.
                        // Không chiếu icon đã dịch ra ngoài ngược lên centerline vì sẽ sai lý trình.
                        const centerlineForSeg = getPolygonCenterline(polygon);
                        if (centerlineForSeg.length >= 2) {
                            const tCenter = projectPointToPath(markerLatLng, centerlineForSeg);
                            const segSpan = INCIDENT_HIGHLIGHT_SPAN;
                            const tFrom = Math.max(0, tCenter - segSpan);
                            const tTo = Math.min(1, tCenter + segSpan);
                            const sliceCoords = getPolygonSlice(polygon, tFrom, tTo);
                            if (sliceCoords.length > 0) {
                                const incidentHighlight = L.polygon(sliceCoords, {
                                    color: '#CC0000',
                                    weight: 1.5,
                                    opacity: 1,
                                    fillColor: '#FF1515',
                                    fillOpacity: 1,
                                    className: 'incident-segment-pulse',
                                    smoothFactor: 1,
                                } as any);
                                state.polygonsLayer?.addLayer(incidentHighlight);
                            }
                        }

                        const incidentMarker = L.marker(markerLatLng, {
                            icon: iconPolygon,
                            zIndexOffset: 1000,
                            // Rê chuột vào 1 icon trong chồng thì đưa icon đó lên trên cùng để xem rõ.
                            riseOnHover: true,
                            riseOffset: 10000,
                            id: incidentFeature.properties.id,
                        } as any);

                        // Đường nối giữ liên hệ giữa icon ngoài biên và vị trí sự cố trong zone.
                        // Kiểu nét cấu hình theo bản đồ, mặc định nét liền.
                        const incidentLineStyle = resolveIncidentLineStyle(state.incidentLineStyle);
                        const incidentLeader = L.polyline([markerLatLng, markerLatLng], {
                            color: '#CC0000',
                            weight: 1.5,
                            opacity: 0.85,
                            dashArray: INCIDENT_LINE_DASH_ARRAY[incidentLineStyle],
                            lineCap: 'round',
                            interactive: false,
                        });
                        state.polygonsLayer?.addLayer(incidentLeader);

                        // Vị trí icon do layoutIncidents tính chung cho mọi sự kiện để icon gần nhau không đè lên nhau.
                        const placementEntry: IncidentPlacementEntry = {
                            anchorLatLng: markerLatLng,
                            polygon,
                            zoneId: String(id),
                            iconSize: eventIconSize,
                            marker: incidentMarker,
                            leader: incidentLeader,
                        };
                        incidentMarker.on('add', () => registerIncidentPlacement(placementEntry));
                        incidentMarker.on('remove', () => unregisterIncidentPlacement(placementEntry));

                        (incidentMarker as any).feature = incidentFeature;

                        if (item.liveEvent) {
                            // Sự kiện trực tiếp chưa có hồ sơ: bấm mở chi tiết sự kiện VDS, không đi luồng popup sự cố.
                            const label = document.createElement('span');
                            label.textContent = `${incidentFeature.properties.eventTypeName} · ${item.liveEvent.licensePlate ?? item.liveEvent.equipmentName ?? ''}`;
                            incidentMarker.bindTooltip(label);
                            incidentMarker.on('click', () => onLiveEventClick?.(item.liveEvent));
                        } else {
                            addItemAction(incidentMarker, incidentFeature);
                        }

                        state.polygonsLayer?.addLayer(incidentMarker);
                    });
                }
            }

            // Làm mịn các khúc gấp của dải zone — CHỈ đổi hình hiển thị, và làm SAU CÙNG để mọi
            // tính toán ở trên (flow centerline, lý trình + highlight sự cố) vẫn dùng toạ độ gốc.
            // Góc ở 2 đầu zone (nơi nối zone kế tiếp) được giữ nguyên nên các zone vẫn khít nhau.
            try {
                const smoothedCoords = smoothPolygonCorners(coords);
                if (smoothedCoords !== coords) {
                    polygon.setLatLngs(smoothedCoords as any);
                    zoneOutline.setLatLngs(smoothedCoords as any);
                }
            } catch (error) {
                // Lỗi làm mịn chỉ ảnh hưởng hiển thị → giữ hình gốc
            }
        }
        // --- Nhánh mapSample (màn hình cấu hình): chỉ hiển thị nhãn tên ở tâm polygon ---
        else {
            // Nhãn tên zone ở tâm polygon — kiểu "glass chip" cao cấp (ẩn/hiện theo cấu hình);
            // bật "Tốc độ min–max" thì nối thêm ngưỡng, kể cả zone thiếu ngưỡng/Min > Max để người cấu hình thấy mà sửa.
            const showName = state.zoneLabelDisplay?.name !== false;
            const speedHtml = state.zoneLabelDisplay?.speed ? buildZoneSpeedHtml(id) : '';
            if (showName || speedHtml) {
                const nameHtml = showName ? `${displayName}${hasUnsavedChanges ? '<span class="zone-label-dot"></span>' : ''}` : '';
                L.marker(getPolygonCentroid(polygon), {
                    icon: L.divIcon({
                        className: 'zone-label-premium',
                        html: `<div class="zone-label-chip${nameHtml && speedHtml ? ' zone-label-chip--with-speed' : ''}">${nameHtml}${speedHtml}</div>`,
                        iconSize: [0, 0],
                        iconAnchor: [0, 0],
                    }),
                    interactive: false,
                }).addTo(state.polygonsLayer as any);
            }

            // Add polygon vào layer cho mapSample
            addItemAction(polygon, feature);
            state.polygonsLayer?.addLayer(polygon);
            // Tiêm gradient <defs> vào SVG renderer (idempotent) — cần add polygon trước để
            // Leaflet chắc chắn đã tạo <svg> trong overlayPane.
            ensureZoneSvgDefs(state.map);

            // Vẽ minh hoạ hướng tính toán Km (lý trình from → to) của polygon
            drawZoneKmDirection(polygon, feature);
            // Vẽ minh hoạ hướng DI CHUYỂN (flow) của tuyến — tách riêng, bật/tắt độc lập
            drawZoneFlowDirection(polygon, feature);
        }
    };

    // #endregion

    // #region Marker Rendering (Device / Unit / VMS)

    /**
     * Thêm marker (icon + tooltip nhãn) vào map, gắn event drag/click.
     */
    const addMarkerToMap = (feature: any, iconMarker: L.DivIcon | L.Icon<L.IconOptions>, objectRender: RenderMapContext): L.Marker => {
        const id = objectRender.id;
        const hasUnsavedChanges = objectRender.hasUnsavedChanges;
        const itemDetails = objectRender.itemDetails;

        const [lng, lat] = feature.geometry.coordinates;

        const marker = L.marker([lat, lng], {
            icon: iconMarker,
            draggable: false,
            zIndexOffset: 1000,
            id: feature.properties?.id,
        } as any);

        // Xác định đây có phải marker thiết bị hiển thị bằng ảnh (VMS/LCS) không
        // (để đặt nhãn tên & popup cho phù hợp)
        let isImageEquip = false;
        let imageEquipTypeCode: string | undefined;
        if (feature.properties?.itemGroup == TmsMapLocationRefType.Equipment) {
            const equipemnt = mapStore.equipmentList.find((e: any) => e.id == feature.properties.id);
            const equipemntType = mapStore.equipmentTypeList.find((et: any) => et?.id == equipemnt?.equipmentTypeId);
            const currentMap = mapStore.mapList.find((item: any) => item.id === props?.selectedMapId);
            isImageEquip = currentMap?.mode === TmsTrafficInfoMap.Vms && isImageEquipmentType(equipemntType?.code);
            imageEquipTypeCode = equipemntType?.code;
        }

        // Nhãn tên:
        // - Marker VMS/LCS (isImageEquip): nhãn được NHÚNG SẴN trong icon (xem
        //   buildEquipmentImageMarkerIcon) nên KHÔNG bind tooltip riêng ở đây, tránh
        //   nhãn tách rời icon khi kéo-thả.
        // - Marker thường: dùng tooltip permanent như cũ.
        if (!isImageEquip) {
            const tooltipContent = `
    <div class="marker-label-content" style="text-align: center; text-shadow: 1px 1px 2px white, -1px -1px 2px white, 1px -1px 2px white, -1px 1px 2px white;">
      <div class="marker-name text-dark" style="font-size:10px; font-weight: 600; color:black; white-space: nowrap;">${itemDetails.name}${hasUnsavedChanges ? '*' : ''
                }</div>
      ${itemDetails.measurement
                    ? `<div class="marker-measurement" style="font-size:10px; font-weight: 600; color:black;">${itemDetails.measurement} ${itemDetails.unit || ''}</div>`
                    : ''
                }
    </div>
  `;

            marker.bindTooltip(tooltipContent, {
                permanent: true,
                direction: 'top',
                offset: [0, 0],
                className: 'transparent-tooltip',
            });
        }

        // Với marker VMS/LCS: gắn thêm POPUP hover hiển thị hình ảnh nội dung hiện tại
        // ở kích thước lớn. Mở khi rê chuột vào, đóng khi rời ra. Offset dựa vào
        // popupAnchor của icon (đặt sẵn ở top khung) để popup nằm sát ngay trên khung.
        if (isImageEquip) {
            const hoverHtml = buildVmsContentTooltip(feature, state.queryParams.itemId, imageEquipTypeCode);
            if (hoverHtml) {
                marker.bindPopup(hoverHtml, {
                    closeButton: false,
                    autoClose: false,
                    closeOnClick: false,
                    autoPan: false,
                    offset: [0, 0],
                    className: 'vms-content-popup',
                });
                marker.on('mouseover', () => marker.openPopup());
                marker.on('mouseout', () => marker.closePopup());
            }
        }

        // Nếu đang ở chế độ move và đây là feature đang active thì cho phép kéo-thả
        if (state.currentMode === 'move' && id === state.activeFeatureId) {
            marker.options.draggable = true;
            marker.on('dragend', (event: any) => {
                const position = event.target.getLatLng();
                updateMarkerPositionInGeoJSON(id, position);
                handleMarkUnsaved(id);
            });
        }

        addItemAction(marker, feature);
        state.markersLayer?.addLayer(marker);
        return marker;
    };

    /**
     * Thêm marker thiết bị hiển thị bằng ảnh (VMS/LCS) với kích thước cố định theo
     * cấu hình từng loại (xem THÔNG SỐ HIỂN THỊ trong helpers/markerIcons.ts).
     * @param typeCode mã loại thiết bị (TmsEquipmentType.Vms | TmsEquipmentType.LCS)
     */
    /**
     * Render marker tùy biến theo LOẠI thiết bị (nếu có). Việc chọn icon theo
     * equipmentType nằm trong switch-case tại helpers/markerIcons.ts
     * (buildEquipmentMarkerIconByType) để dễ mở rộng loại mới.
     * Chỉ áp dụng khi bản đồ đang ở chế độ VMS.
     *
     * @returns true nếu đã render marker tùy biến (không cần icon device mặc định);
     *          false nếu để luồng gọi tự render icon device thông thường.
     */
    const tryRenderCustomEquipmentMarker = (feature: any, equipemnt: any, equipemntType: any, objectRenderMap: RenderMapContext): boolean => {
        const currentMap = mapStore.mapList.find((item: any) => item.id === props?.selectedMapId);
        // Hiện tại các marker tùy biến chỉ áp dụng cho bản đồ chế độ VMS
        if (currentMap?.mode !== TmsTrafficInfoMap.Vms) return false;

        const label = { name: objectRenderMap.itemDetails?.name, unsaved: objectRenderMap.hasUnsavedChanges };
        const icon = buildEquipmentMarkerIconByType(feature, equipemnt, state.queryParams.itemId, equipemntType?.code, label);
        if (!icon) return false;

        addMarkerToMap(feature, icon, objectRenderMap);
        return true;
    };

    /**
     * Vẽ lại marker của một thiết bị theo dữ liệu hiện tại trong mapStore.
     * Mặc định chỉ xử lý VMS/LCS khi nội dung phát thay đổi; `forceRender` cho phép
     * status NATS cập nhật icon của mọi thiết bị mà không render lại toàn bộ bản đồ.
     */
    const renderMarkerVMS = (_isEditMode: boolean, feature: any, forceRender = false) => {
        if (feature.properties?.itemGroup !== TmsMapLocationRefType.Equipment) return;

        const equipmentId = feature.properties.id;
        const equipment = mapStore.equipmentList.find((item: any) => String(item.id) === String(equipmentId));
        const equipmentType = mapStore.equipmentTypeList.find((item: any) => item?.id === equipment?.equipmentTypeId);
        const currentMap = mapStore.mapList.find((item: any) => item.id === props?.selectedMapId);
        const isImageEquipment = currentMap?.mode === TmsTrafficInfoMap.Vms && isImageEquipmentType(equipmentType?.code);

        if (!forceRender && !isImageEquipment) return;

        state.markersLayer
            .getLayers()
            .filter((layer: any) => layer instanceof L.Marker && String(layer.options?.id) === String(equipmentId))
            .forEach((layer) => state.markersLayer.removeLayer(layer));

        renderMarker(!!props.isEditMode, feature);
    };

    /**
     * Render marker (device/unit/VMS) dựa theo itemGroup của feature.
     */
    const renderMarker = (_isEditMode: boolean, feature: any) => {
        // Annotation tự do (nhãn văn bản / hình ảnh) — do useMapAnnotations vẽ và xử lý
        // kéo/xoay/resize/liên kết cụm. Không dùng icon thiết bị/đơn vị.
        if (renderAnnotationFeature(feature)) return;

        // Chế độ block hiển thị các thiết bị đã được bố trí trong card riêng.
        // Không render lặp marker tại tọa độ thực, nhưng GeoJSON vẫn được giữ để
        // BlockMapLayer vẽ đường nối từ vị trí thực tế đến card hiển thị.
        const isDeviceInBlock =
            (state.currentMapMode === TmsTrafficInfoMap.Block || state.currentMapMode === TmsTrafficInfoMap.BlockVms) &&
            feature.properties?.itemGroup === TmsMapLocationRefType.Equipment &&
            state.blockConfig?.blocks?.some((block: any) =>
                block.levels?.some((level: any) => level.devices?.some((device: any) => String(device.deviceId) === String(feature.properties?.id)))
            );
        if (isDeviceInBlock) return;

        const objectRenderMap: RenderMapContext = {
            id: '',
            itemGroups: null as any,
            hasUnsavedChanges: false as boolean,
            itemDetails: {} as ItemDetails,
        };

        const idFeature = feature.properties?.id;
        const itemGroupInFeature = feature.properties?.itemGroup;

        objectRenderMap.id = idFeature;
        objectRenderMap.itemGroups = itemGroupInFeature;
        objectRenderMap.hasUnsavedChanges = state.unsavedChanges.has(idFeature);
        objectRenderMap.itemDetails = mapStore.getItemDetails(idFeature, itemGroupInFeature);

        // Get icon work_unit_type from config params
        const valueFromSysConfigWorkUnitType = mapStore.workUnitTypeList.find((m: any) => m.code == objectRenderMap.itemDetails?.type)?.value;

        if (valueFromSysConfigWorkUnitType && valueFromSysConfigWorkUnitType != '') {
            objectRenderMap.itemDetails.iconUrl = valueFromSysConfigWorkUnitType;
        }

        // Check lấy icon hiển thị trên map
        let iconMarker;

        if (feature.properties.itemGroup == TmsMapLocationRefType.Equipment) {
            const selectedEquipmentId = feature.properties.id;
            const equipemnt = mapStore.equipmentList.find((equipment: any) => equipment.id == selectedEquipmentId);
            const equipemntType = mapStore.equipmentTypeList.find((equipmentType: any) => equipmentType?.id == equipemnt?.equipmentTypeId);

            // Render tùy biến theo loại thiết bị (VMS/LCS/...) — xem tryRenderCustomEquipmentMarker.
            // Nếu đã render tùy biến thì kết thúc; nếu không thì dùng icon device mặc định.
            if (tryRenderCustomEquipmentMarker(feature, equipemnt, equipemntType, objectRenderMap)) {
                return;
            }
            iconMarker = buildDeviceMarkerIcon(feature, objectRenderMap, {
                isEditMode: !!props.isEditMode,
                randomToken: state.randomFourNumber,
                iconSize: state.iconSize,
            });
        }
        // Nếu là work_unit_type
        else {
            iconMarker = buildUnitMarkerIcon(objectRenderMap, state.randomFourNumber, state.iconSize);
        }
        // Khai báo tạo marker để add vào map
        addMarkerToMap(feature, iconMarker, objectRenderMap);
    };

    // #endregion

    // #region Main Render Loop (Entry Point)

    /**
     * Render map từ dữ liệu GeoJSON: xóa layer cũ, render lại từng feature
     * (marker cho Point, polygon cho zone), và tái áp dụng chế độ edit/move
     * cho layer đang active (nếu có).
     */
    /**
     * Màn theo dõi giao thông: đẩy NỀN của mọi zone xuống dưới cùng của SVG, giữ nguyên thứ
     * tự giữa các nền với nhau. Viền đen (zone-outline), flow-line, mũi tên và highlight sự
     * cố vì thế luôn nằm trên nền của mọi zone, không bị zone vẽ chồng mép che mất.
     */
    const sendZoneFillsToBack = () => {
        const fills: any[] = [];
        state.polygonsLayer?.eachLayer((layer: any) => {
            if (layer?._map && String(layer.options?.className || '').includes('zone-polygon-status')) fills.push(layer);
        });
        // Duyệt ngược: zone cuối bị đẩy xuống trước → thứ tự tương đối giữa các nền không đổi
        for (let i = fills.length - 1; i >= 0; i--) fills[i].bringToBack();
    };

    const renderGeoJson = async (item: any = null) => {
        // Check tồn tại map và các layer
        if (!state.map || !state.markersLayer || !state.polygonsLayer) return;

        state.markersLayer.clearLayers();
        state.polygonsLayer.clearLayers();
        clearHighlight();
        // Vùng chọn annotation có thể trỏ tới feature vừa bị xoá/bị lọc → làm sạch trước khi vẽ.
        pruneAnnotationSelection();

        // Snapshot incident list ngay tại đây để tránh race condition:
        // mapStore có thể update incidentZoneDataExistInTrafficInfoList trong khi
        // renderGeoJson đang await renderPolygon cho zone tiếp theo, khiến các zone
        // sau dùng data khác với zone đầu.
        // KienLT - 11/11/2026: sự kiện VDS trực tiếp (chưa có hồ sơ) vẽ chung với sự cố theo zone + lý trình.
        const incidentSnapshot: any[] = [...(mapStore.incidentZoneDataExistInTrafficInfoList || []), ...(mapStore.liveEventIconList || [])];
        const isEditMode = !!props.isEditMode;

        // Loop trong dữ liệu geoJsonData để setup map
        // Dùng for...of + await để đảm bảo renderPolygon (async) hoàn thành trước khi render feature tiếp theo
        // Tránh race condition: clearLayers() của render mới xóa marker đang được add dở bởi render cũ
        for (const feature of state.geoJsonData.features) {
            try {
                const id = feature.properties?.id;
                const itemGroup = feature.properties?.itemGroup;
                const hasUnsavedChanges = state.unsavedChanges.has(id);
                const itemDetails = mapStore.getItemDetails(id, itemGroup);

                // Get icon work_unit_type
                const valueFromSysConfigWorkUnitType = mapStore.workUnitTypeList.find((m: any) => m.code == itemDetails?.type)?.value;

                if (valueFromSysConfigWorkUnitType && valueFromSysConfigWorkUnitType != '') {
                    itemDetails.iconUrl = valueFromSysConfigWorkUnitType;
                }

                // Xử lý hiển thị marker - Icon thiết bị, item, ...
                if (feature.geometry.type === 'Point') {
                    renderMarker(isEditMode, feature);
                }
                // Xử lý hiển thị polygon - zone
                else if (feature.geometry.type === 'Polygon') {
                    await renderPolygon(feature, hasUnsavedChanges, itemDetails, incidentSnapshot);
                }

                // Nếu đây là feature đang được edit/move (active layer) thì tái áp dụng chế độ tương ứng
                if (feature.properties?.id === item?.activeLayerId && state.currentMode) {
                    setTimeout(() => {
                        state.polygonsLayer.eachLayer((layer: any) => {
                            if (layer.feature?.properties?.id === item?.activeLayerId || layer.options?.id === item?.activeLayerId) {
                                // Update layer with current coordinates if available
                                if (state.currentCoordinates && feature.geometry.type === 'Polygon') {
                                    const latLngs = state.currentCoordinates[0].map(([lng, lat]: number[]) => [lat, lng]);
                                    layer.setLatLngs(latLngs);
                                }

                                // Re-enable the appropriate mode
                                if (state.currentMode === 'edit') {
                                    layer.pm.enable({
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

                                    // Reattach edit event listeners
                                    layer.on('pm:vertexadded pm:vertexremoved pm:vertexclick pm:markerdragend', (e: any) => {
                                        const updatedCoords = e.target.getLatLngs()[0].map((latLng: L.LatLng) => [latLng.lng, latLng.lat]);
                                        feature.geometry.coordinates = [updatedCoords];
                                        state.currentCoordinates = [updatedCoords];
                                        handleMarkUnsaved(item?.activeLayerId);
                                    });
                                } else if (state.currentMode === 'move') {
                                    layer.pm.enable({
                                        allowSelfIntersection: false,
                                        preventMarkerRemoval: true,
                                        draggable: true,
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
                                }

                                state.editingLayer = layer;
                            }
                        });
                    }, 0);
                }
            } catch (error) {
                // ignore lỗi render 1 feature, tiếp tục render các feature khác
            }
        } // end for...of

        if (!isEditMode) {
            try {
                sendZoneFillsToBack();
            } catch (error) {
                // Chỉ ảnh hưởng thứ tự vẽ, không chặn luồng render
            }
        }

        // Tắt trigger refresh
        setTimeout(() => {
            state.isRefreshing = false;
        }, 10);
    };

    // #endregion

    // #region Public API

    return {
        renderGeoJson,
        renderMarkerVMS,
        handleMarkUnsaved,
        clearHighlight,
        updateMarkerPositionInGeoJSON,
        // Annotation tự do (text/image): vẽ lại khi zoom + các thao tác trên vùng chọn.
        refreshAnnotations,
        getSelectedAnnotations: annotations.getSelectedFeatures,
        getFocusedAnnotation: annotations.getFocusedFeature,
        clearAnnotationSelection: annotations.clearAnnotationSelection,
        groupAnnotationSelection: annotations.groupAnnotationSelection,
        ungroupAnnotationSelection: annotations.ungroupAnnotationSelection,
        setAnnotationSelectionScaleMode: annotations.setAnnotationSelectionScaleMode,
        deleteAnnotationSelection: annotations.deleteAnnotationSelection,
        openSelectedAnnotationEditor: annotations.openSelectedAnnotationEditor,
        stopAnnotationInteraction: annotations.stopAnnotationInteraction,
    };

    // #endregion
}
