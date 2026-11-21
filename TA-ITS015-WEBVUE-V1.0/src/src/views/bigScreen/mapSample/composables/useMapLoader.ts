import L from 'leaflet';
import { getAPI } from '/@/utils/axios-utils';
import { TmsMapLocationApi } from '/@/api-services/tms';
import { useMapStore } from '/@/stores/its/mapStore';
import { MapTypeConfigEnum } from '/@/types/enums';
import { getTileProxyUrl, TRANSPARENT_TILE } from '/@/utils/mapTile';
import { parseSavedBounds, getImageSize } from '../helpers/mapViewUtils';
import { normalizeBlockConfiguration } from '../blockTypes';
import { resolveIncidentLineStyle, resolveIncidentZoneGap } from '../helpers/incidentPlacement';
import { normalizeFlowLineColors, resolveFlowLineWidth, resolveZoneBorderWidth } from '../helpers/zoneDisplayStyle';

interface MapViewState {
    center: [number, number];
    zoom: number;
    maxZoom: number;
    minZoom: number;
    rotation?: number;
    boundRadius?: number;
}

/**
 * Composable tải dữ liệu bản đồ (config view, tile online, ảnh, geojson).
 *
 * State của LeafletMap.vue được truyền vào bằng tham chiếu (reactive object)
 * vì rất nhiều field (map, tileLayer, imageOverlay, mapViewState, geoJsonData...)
 * còn được đọc/ghi bởi renderGeoJson, handleSave, editing, watchers...
 */
export function useMapLoader(deps: {
    state: any;
    props: { isEditMode?: boolean; selectedMapId?: string };
    renderGeoJson: (item?: any) => Promise<void>;
    checkShowIconName: () => void;
    clearLayers: () => void;
    returnGeoDataJsonToComponentIndex: () => void;
}) {
    const { state, props, renderGeoJson, checkShowIconName, clearLayers, returnGeoDataJsonToComponentIndex } = deps;
    const mapStore = useMapStore();

    const getMapImageUrl = (rawUrl: unknown): string => {
        const url = String(rawUrl ?? '').trim();
        if (!url) return '';

        // The map editor can persist either a file path or a data/absolute URL.
        if (/^(data:|blob:|https?:\/\/|\/)/i.test(url)) return url;

        const baseUrl = String(mapStore.baseUrl ?? '').replace(/\/+$/, '');
        return baseUrl ? `${baseUrl}/${url.replace(/^\/+/, '')}` : url;
    };

    const appendCacheBuster = (url: string): string => {
        if (/^(data:|blob:)/i.test(url)) return url;
        return `${url}${url.includes('?') ? '&' : '?'}t=${Date.now()}_${Math.random().toString(36).slice(2, 8)}`;
    };

    const getFiniteCenter = (center: unknown): [number, number] | null => {
        if (!Array.isArray(center) || center.length !== 2) return null;

        const lat = Number(center[0]);
        const lng = Number(center[1]);
        return Number.isFinite(lat) && Number.isFinite(lng) ? [lat, lng] : null;
    };

    // Cờ chống ghi đè lẫn nhau khi circular-clamp đang tự pan map
    let isClampingBound = false;

    // Mỗi lần đổi map tạo một request mới. Kết quả của request cũ phải bị bỏ qua
    // nếu API trả về sau khi người dùng đã chuyển sang map khác.
    let latestMapLoadId = 0;

    // #region Tile Layer & Bounds Helpers

    /**
     * Tạo tile layer bằng L.tileLayer thuần (KHÔNG dùng cơ chế safe-tile/queue nữa).
     * - Vẫn định tuyến tile cross-origin qua proxy cùng origin (`/tile-proxy`) để tránh CORS/PNA.
     * - Dùng errorTileUrl trong suốt để tile lỗi không hiện icon vỡ.
     * - Không đếm lỗi / không tự gỡ layer → bản đồ không bị biến mất khi có tile lỗi lẻ.
     */
    const createTileLayer = (url: string, options: L.TileLayerOptions, useProxy = true): L.TileLayer => {
        const finalUrl = useProxy ? getTileProxyUrl(url) : url;
        return L.tileLayer(finalUrl, {
            errorTileUrl: TRANSPARENT_TILE,
            updateWhenIdle: true,
            updateWhenZooming: false,
            ...options,
        });
    };

    /**
     * Thiết lập giới hạn vùng hiển thị theo BÁN KÍNH quanh tâm mặc định.
     *
     * Vì ràng buộc dựa trên KHOẢNG CÁCH (distanceTo) từ tâm hiện tại đến tâm mặc định,
     * nó hoàn toàn bất biến với góc xoay bản đồ (leaflet-rotate bearing).
     *
     * @param defaultCenter  Tâm mặc định của vùng cho phép
     * @param radiusMeters   Bán kính giới hạn (mét)
     */
    const setupCircularBound = (defaultCenter: [number, number], radiusMeters: number) => {
        if (!state.map) return;
        const centerLatLng = L.latLng(defaultCenter[0], defaultCenter[1]);

        const enforce = () => {
            if (isClampingBound || !state.map) return;
            const current = state.map.getCenter();
            const dist = centerLatLng.distanceTo(current);
            if (dist <= radiusMeters) return;

            // Chiếu tâm hiện tại về lại trên vòng tròn bán kính cho phép.
            // Nội suy tuyến tính trên lat/lng — đủ chính xác cho vùng vài km.
            const ratio = radiusMeters / dist;
            const clampedLat = centerLatLng.lat + (current.lat - centerLatLng.lat) * ratio;
            const clampedLng = centerLatLng.lng + (current.lng - centerLatLng.lng) * ratio;

            isClampingBound = true;
            state.map.panTo([clampedLat, clampedLng], { animate: false });
            isClampingBound = false;
        };

        // Gỡ listener cũ (nếu có) rồi gắn lại để tránh trùng
        state.map.off('move', enforce);
        state.map.off('moveend', enforce);
        state.map.on('move', enforce);
        state.map.on('moveend', enforce);
    };

    // #endregion

    // #region Load Bản Đồ Online (Tile)

    /**
     * Load bản đồ online/offline tile.
     *
     * Có 2 chế độ:
     * 1. Edit mode (isEditMode = true)
     *    - Không khóa bounds cứng
     *    - Cho phép người dùng zoom out / pan rộng để cấu hình minZoom, center
     *
     * 2. Runtime mode (isEditMode = false)
     *    - Khóa map theo bounds đã lưu
     *    - Chặn kéo vô tận
     *    - Chặn tile render tiếp ra ngoài vùng cho phép
     *
     * @param map_config  Cấu hình map từ DB/store
     * @param viewState   View hiện tại gồm center, zoom, minZoom, maxZoom
     * @param savedBounds Bounds đã lưu trong config, dùng để khóa map ở runtime
     */
    const loadOnlineMap = async (map_config: any, viewState: MapViewState, savedBounds?: L.LatLngBounds | null, useProxy = true) => {
        state.map!.options.crs = L.CRS.EPSG3857;
        state.imageBounds = null;

        const configuredMinZoom = viewState.minZoom != undefined ? viewState.minZoom : 1;
        const configuredMaxZoom = viewState.maxZoom != undefined ? viewState.maxZoom : 18;

        // Chế độ CẤU HÌNH (edit): zoom KHÔNG bị giới hạn bởi min/max của config.
        // min/max chỉ dùng để GIỚI HẠN ở chế độ VIEW (runtime).
        const isConfigMode = !!props.isEditMode;
        const FREE_MIN_ZOOM = 1; // sàn zoom-out (an toàn với EPSG3857)
        const FREE_MAX_ZOOM = 22; // trần zoom-in tự do
        const mapMinZoom = isConfigMode ? FREE_MIN_ZOOM : configuredMinZoom;
        const mapMaxZoom = isConfigMode ? FREE_MAX_ZOOM : configuredMaxZoom;

        state.map!.setMinZoom(mapMinZoom);
        state.map!.setMaxZoom(mapMaxZoom);

        // Xóa tile layer + image overlay cũ nếu có (tránh layer rác còn nghe 'zoomanim' gây crash khi zoom)
        if (state.tileLayer && state.map?.hasLayer(state.tileLayer)) {
            state.map.removeLayer(state.tileLayer);
            state.tileLayer = null;
        }
        if (state.imageOverlay && state.map?.hasLayer(state.imageOverlay)) {
            state.map.removeLayer(state.imageOverlay);
            state.imageOverlay = null;
        }

        /**
         * MODE CẤU HÌNH:
         * - Không set bounds cho tile layer
         * - Không set maxBounds cho map
         * - Mục tiêu là để user zoom out, pan và chọn minZoom / center mới
         */
        if (props.isEditMode) {
            state.tileLayer = createTileLayer(map_config.url, {
                // Chế độ CẤU HÌNH: KHÔNG ràng buộc tile theo configuredMin/MaxZoom.
                // configuredMaxZoom chỉ là giới hạn VIEW, không liên quan tới việc tải tile.
                // Cho phép tile tải ở mọi mức zoom server có (giống L.tileLayer thuần):
                // - Không set maxNativeZoom → tải TILE THẬT ở zoom cao (không ép upscale).
                // - Chỗ nào server không có tile → để trống (không còn safe-tile gỡ bản đồ).
                minZoom: FREE_MIN_ZOOM,
                maxZoom: FREE_MAX_ZOOM,
                noWrap: true,
                updateWhenIdle: true,
                updateWhenZooming: false,
                keepBuffer: 1,
            }, useProxy);

            state.tileLayer.addTo(state.map!);

            // Bỏ khóa bounds khi đang cấu hình
            state.map!.setMaxBounds(undefined as any);

            const targetZoom = Math.max(mapMinZoom, Math.min(viewState.zoom, mapMaxZoom));

            state.map!.setView(viewState.center, targetZoom, { animate: false });
            return;
        }

        /**
         * MODE RUNTIME:
         * - Nếu config có boundRadius > 0: giới hạn theo BÁN KÍNH quanh center (circular clamp).
         *   Cách này bất biến với góc xoay bản đồ (rotation/bearing).
         * - Nếu không có boundRadius: fallback dùng bounds chữ nhật (tương thích config cũ).
         */
        const boundRadius = viewState.boundRadius ?? 0;

        if (boundRadius > 0) {
            // Không dùng maxBounds chữ nhật (lỗi khi xoay) — render tile toàn bộ, clamp bằng tay
            state.tileLayer = createTileLayer(map_config.url, {
                minZoom: configuredMinZoom,
                maxZoom: configuredMaxZoom,
                noWrap: true,
                updateWhenIdle: true,
                updateWhenZooming: false,
                keepBuffer: 1,
            }, useProxy);
            state.tileLayer.addTo(state.map!);

            // Bỏ maxBounds chữ nhật
            state.map!.setMaxBounds(undefined as any);

            // Thiết lập circular clamp quanh center mặc định
            setupCircularBound(viewState.center, boundRadius);

            const targetZoom = Math.max(configuredMinZoom, Math.min(viewState.zoom, configuredMaxZoom));
            state.map!.setView(viewState.center, targetZoom, { animate: false });
            return;
        }

        /**
         * MODE RUNTIME (fallback config cũ):
         * - Dùng bounds đã lưu để khóa map
         * - Nếu config cũ chưa có bounds thì fallback:
         *   tạm tính bounds từ center + minZoom hiện tại
         */
        let boundedArea = savedBounds;

        if (!boundedArea) {
            state.map!.setView(viewState.center, configuredMinZoom, { animate: false });
            state.map!.invalidateSize();
            boundedArea = state.map!.getBounds().pad(0.03);
        }

        state.tileLayer = createTileLayer(map_config.url, {
            minZoom: configuredMinZoom,
            maxZoom: configuredMaxZoom,
            noWrap: true,
            bounds: boundedArea ?? undefined, // Chỉ cho tile render trong vùng này
            updateWhenIdle: true,
            updateWhenZooming: false,
            keepBuffer: 1,
        }, useProxy);

        state.tileLayer.addTo(state.map!);

        // Khóa map không cho pan ra ngoài bounds
        state.map!.setMaxBounds(boundedArea);
        state.map!.options.maxBoundsViscosity = 1.0;

        const targetZoom = Math.max(configuredMinZoom, Math.min(viewState.zoom, configuredMaxZoom));

        state.map!.setView(viewState.center, targetZoom, { animate: false });
        state.map!.panInsideBounds(boundedArea, { animate: false });
    };

    // #endregion

    // #region Load Bản Đồ Dạng Hình Ảnh

    /**
     * Load bản đồ dạng hình ảnh.
     */
    const loadImageMap = async (map_config: any, viewState: any, hasSavedState: boolean) => {
        const map = state.map as L.Map | null;
        if (!map) return;

        map.options.crs = L.CRS.Simple;
        state.imageBounds = null;

        // Xóa tile layer + image overlay cũ nếu có (tránh layer rác còn nghe 'zoomanim' gây crash khi zoom)
        if (state.tileLayer && map.hasLayer(state.tileLayer)) {
            map.removeLayer(state.tileLayer);
            state.tileLayer = null;
        }
        if (state.imageOverlay && map.hasLayer(state.imageOverlay)) {
            map.removeLayer(state.imageOverlay);
            state.imageOverlay = null;
        }

        // Create and add image overlay
        // 1. Get image url
        const imageUrl = getMapImageUrl(map_config.url);
        if (!imageUrl) throw new Error('Map image URL is empty');

        // 2. Get width and height
        const { width, height } = await getImageSize(imageUrl);
        if (state.map !== map) return;

        // Calculate image bounds
        const bounds = L.latLngBounds([
            [0, 0],
            [height, width],
        ]);
        state.imageBounds = bounds;

        const configuredMinZoom = Number.isFinite(Number(viewState.minZoom)) ? Number(viewState.minZoom) : -3;
        const configuredMaxZoom = Number.isFinite(Number(viewState.maxZoom)) ? Number(viewState.maxZoom) : 3;

        // Chế độ CẤU HÌNH cho phép zoom rộng hơn config để dễ đặt vị trí.
        // Các giá trị này phải được set trước fitBounds, nếu không minZoom mặc định
        // của map (1) có thể làm Leaflet không tính được mức zoom vừa khung ảnh.
        const isConfigMode = !!props.isEditMode;
        const mapMinZoom = isConfigMode ? Math.min(configuredMinZoom - 3, -5) : configuredMinZoom;
        const mapMaxZoom = isConfigMode ? Math.max(configuredMaxZoom + 3, 8) : configuredMaxZoom;
        const savedCenter = getFiniteCenter(viewState.center);
        // Vùng pan của màn cấu hình (ảnh + 2 lần kích thước mỗi phía): tâm nào chọn được ở màn cấu hình
        // thì runtime phải dùng được. Trước đây chỉ nhận tâm NẰM TRONG ảnh → tâm ngoài ảnh (vd vĩ độ âm)
        // bị bỏ qua và runtime luôn fitBounds về giữa ảnh.
        const configPanBounds = bounds.pad(2);
        const hasUsableSavedCenter = hasSavedState && !!savedCenter && configPanBounds.contains(savedCenter);
        let maxBounds = isConfigMode ? configPanBounds : bounds.pad(0.05);

        // Add overlay
        const imageMap = appendCacheBuster(imageUrl);

        const imageOverlay = L.imageOverlay(imageMap, bounds, {
            // rất nên thêm
            crossOrigin: false, // an toàn hơn cho WebView
            interactive: false,
        });
        state.imageOverlay = imageOverlay;

        // Chờ ảnh thực sự load xong rồi mới tính viewport. Gắn listener trước addTo
        // để không bỏ lỡ event trong các trình duyệt/WebView xử lý cache nhanh.
        await new Promise<void>((resolve, reject) => {
            imageOverlay.once('load', () => {
                if (state.map !== map) {
                    resolve();
                    return;
                }

                map.invalidateSize();
                map.setMinZoom(mapMinZoom);
                map.setMaxZoom(mapMaxZoom);
                map.setMaxBounds(undefined as any);

                // Gỡ listener giữ khung nhìn của lần nạp trước (đổi map / nạp lại)
                const previousKeepView = (map as any).__keepConfiguredView;
                if (previousKeepView) map.off('resize', previousKeepView);
                (map as any).__keepConfiguredView = null;

                if (hasUsableSavedCenter) {
                    const savedZoom = Number(viewState.zoom);
                    const targetZoom = Number.isFinite(savedZoom)
                        ? Math.max(mapMinZoom, Math.min(savedZoom, mapMaxZoom))
                        : map.getZoom();
                    map.setView(savedCenter as [number, number], targetZoom, { animate: false });

                    /**
                     * Nới vùng pan cho chứa trọn khung nhìn đã cấu hình. Nếu không, panInsideBounds +
                     * maxBoundsViscosity đẩy tâm vào trong ảnh, và khi khung nhìn rộng hơn vùng pan thì
                     * Leaflet tự căn GIỮA → lệch khỏi vị trí đã thiết lập.
                     * Tính theo KÍCH THƯỚC KHUNG hiện tại và tính lại mỗi lần khung đổi cỡ (resize chạy trước
                     * moveend nên kịp trước khi Leaflet ép pan): lúc nạp khung có thể chưa đủ cỡ, trước đây
                     * vùng pan bị tính quá nhỏ rồi khi khung giãn ra thì bản đồ bị kéo về giữa ảnh.
                     * Dùng nửa đường chéo khung để vẫn đủ rộng khi bản đồ xoay (rotation).
                     */
                    const baseBounds = L.latLngBounds(maxBounds.getSouthWest(), maxBounds.getNorthEast());
                    const configuredCenter = savedCenter as [number, number];
                    const keepConfiguredView = () => {
                        if (state.map !== map || !state.imageBounds) return; // đã chuyển sang map khác
                        const size = map.getSize();
                        const radius = Math.hypot(size.x, size.y) / 2;
                        const centerPoint = map.project(configuredCenter, targetZoom);
                        const viewBounds = L.latLngBounds(
                            map.unproject(centerPoint.subtract([radius, radius]), targetZoom),
                            map.unproject(centerPoint.add([radius, radius]), targetZoom)
                        );
                        maxBounds = L.latLngBounds(baseBounds.getSouthWest(), baseBounds.getNorthEast()).extend(viewBounds);
                        map.setMaxBounds(maxBounds);
                    };
                    keepConfiguredView();
                    map.on('resize', keepConfiguredView);
                    (map as any).__keepConfiguredView = keepConfiguredView;
                } else {
                    // Map mới hoặc config cũ không thuộc hệ tọa độ pixel của ảnh:
                    // fitBounds luôn đưa đúng tâm ảnh vào tâm viewport.
                    map.fitBounds(bounds, { animate: false, padding: [16, 16] });
                }

                map.setMaxBounds(maxBounds);
                map.options.maxBoundsViscosity = 1.0;
                map.panInsideBounds(maxBounds, { animate: false });

                const center = map.getCenter();
                const actualViewState: MapViewState = {
                    ...viewState,
                    center: [center.lat, center.lng],
                    zoom: map.getZoom(),
                    minZoom: mapMinZoom,
                    maxZoom: mapMaxZoom,
                    rotation: typeof viewState.rotation === 'number' ? viewState.rotation : 0,
                };

                // Đồng bộ form/save/reset với viewport thực tế, không dùng center
                // kiểu bản đồ online còn sót lại trong config mặc định.
                // Form (ô "Hiện tại") phải mang min/max ĐÃ CẤU HÌNH, không phải giới hạn nới rộng của
                // màn cấu hình (min - 3 / max + 3): trước đây giá trị nới rộng hiện ở "Hiện tại" rồi bị
                // "Lưu bản đồ" ghi ngược vào CSDL, mỗi lần lưu khoảng zoom lại rộng thêm.
                // initialMapViewState (dùng cho reset view) vẫn giữ giới hạn thực của map.
                state.mapViewState = { ...actualViewState, minZoom: configuredMinZoom, maxZoom: configuredMaxZoom };
                state.initialMapViewState = JSON.parse(JSON.stringify(actualViewState));
                resolve();
            });

            imageOverlay.once('error', (event: any) => {
                reject(new Error(`Không hiển thị được map image: ${event?.error?.message || imageUrl}`));
            });

            imageOverlay.addTo(map);
        });

        // Giới hạn pan trong vùng ảnh (tránh trôi node khi zoom)
        map.options.maxBoundsViscosity = 1.0;

        // Keep Leaflet sized to the container
        if (!('__ro' in map)) {
            const ro = new ResizeObserver(() => map.invalidateSize());
            (map as any).__ro = ro;
            ro.observe(map.getContainer());
        }
    };

    /** Đưa ảnh về giữa viewport và cập nhật view state để có thể lưu ngay. */
    const centerImageMap = () => {
        const map = state.map as L.Map | null;
        const bounds = state.imageBounds as L.LatLngBounds | null;
        if (!map || !bounds) return;

        map.invalidateSize();
        map.fitBounds(bounds, { animate: false, padding: [16, 16] });

        const center = map.getCenter();
        state.mapViewState = {
            ...state.mapViewState,
            center: [center.lat, center.lng],
            zoom: map.getZoom(),
            minZoom: map.getMinZoom(),
            maxZoom: map.getMaxZoom(),
        };
    };

    // #endregion

    // #region Load Dữ Liệu Bản Đồ (Entry Point)

    /**
     * Load dữ liệu bản đồ: config view, tile/ảnh, và geojson.
     */
    const loadMapData = async (mapId: string) => {
        if (!mapId || !state.map) return;

        const mapLoadId = ++latestMapLoadId;
        const mapInstance = state.map;
        const isCurrentMapLoad = () =>
            mapLoadId === latestMapLoadId &&
            props.selectedMapId === mapId &&
            state.map === mapInstance;

        if (!isCurrentMapLoad()) return;

        try {
            // Reset cache filter trước khi load full data
            state.previousFeatureGeoJsonData = [];
            state.previousFeatureGeoJsonDataZone = [];
            state.listIdZoneInEventType = [];
            clearLayers();
            const tmsMapLocationResponse = await getAPI(TmsMapLocationApi).apiTmsTmsmaplocationListGet();
            if (!isCurrentMapLoad()) return;

            const tmsMapLocation = tmsMapLocationResponse?.data?.result?.find((item: any) => item.mapId === mapId);

            if (mapStore.mapList && mapStore.mapList.length < 1) {
                await mapStore.fetchMapList();
                if (!isCurrentMapLoad()) return;
            }
            const map_config = mapStore.mapList.find((item: any) => item.id == mapId);
            if (!map_config) {
                console.error('Map configuration not found');
                return;
            }

            state.currentMapType = map_config?.type || '';

            const mapUrl = map_config.url;
            if (!mapUrl) {
                console.error('Invalid map URL');
                return;
            }

            // Parse saved view state with default values
            let hasSavedState = false;
            let parsedConfig: any = null;
            let savedBounds: L.LatLngBounds | null = null;

            const isImageMap = map_config.type === MapTypeConfigEnum.Image;
            let viewState: MapViewState = {
                // Image map dùng CRS.Simple (tọa độ pixel), không dùng center
                // mặc định của bản đồ online làm viewport ban đầu.
                center: (isImageMap ? [0, 0] : [10.89484, 107.47839]) as [number, number],
                zoom: isImageMap ? 0 : 14,
                maxZoom: isImageMap ? 3 : 18,
                minZoom: isImageMap ? -3 : 1,
                rotation: 0,
            };

            if (tmsMapLocation?.config && tmsMapLocation.config != null) {
                try {
                    parsedConfig = JSON.parse(tmsMapLocation.config);
                    if (parsedConfig && Array.isArray(parsedConfig.center) && parsedConfig.center.length === 2 && typeof parsedConfig.zoom === 'number') {
                        hasSavedState = true;

                        viewState = {
                            center: parsedConfig.center,
                            zoom: parsedConfig.zoom,
                            minZoom: parsedConfig.minZoom != undefined ? parsedConfig.minZoom : parsedConfig.zoom,
                            maxZoom: parsedConfig.maxZoom != undefined ? parsedConfig.maxZoom : parsedConfig.zoom,
                            rotation: parsedConfig.rotation != undefined ? parsedConfig.rotation : 0,
                            boundRadius: parsedConfig.boundRadius != undefined ? parsedConfig.boundRadius : 0,
                        };
                        savedBounds = parseSavedBounds(parsedConfig);
                    }
                } catch (e) {
                    console.warn('Invalid view state in map.remark:', e);
                }
            }

            state.currentMapMode = map_config?.mode || '';
            // Tương thích ngược: các bản đồ cũ chưa có thuộc tính này vẫn hiển thị nền icon.
            state.showIconBackground = typeof parsedConfig?.showIconBackground === 'boolean' ? parsedConfig.showIconBackground : true;
            // Map cũ chưa có thuộc tính này: không khóa zoom.
            state.lockZoom = parsedConfig?.lockZoom === true;
            // Map cũ chưa có thuộc tính này: không khóa xoay.
            state.lockRotation = parsedConfig?.lockRotation === true;
            // Kích thước map-level > 0 sẽ ghi đè size từng equipment type; 0 dùng cấu hình từng loại.
            const configuredIconSize = Number.parseFloat(String(parsedConfig?.iconSize ?? ''));
            state.iconSize = Number.isFinite(configuredIconSize) && configuredIconSize > 0 ? configuredIconSize : 0;
            // Kích thước icon sự kiện sự cố cấp bản đồ:
            const configuredIncidentIconSize = Number.parseFloat(String(parsedConfig?.incidentIconSize ?? ''));
            state.incidentIconSize = Number.isFinite(configuredIncidentIconSize) && configuredIncidentIconSize > 0 ? configuredIncidentIconSize : 0;
            // Map cũ chưa có khoảng hở sẽ dùng 2px; giữ nguyên 0px nếu được cấu hình.
            state.incidentZoneGap = resolveIncidentZoneGap(parsedConfig?.incidentZoneGap);
            // Map cũ chưa có kiểu nét đường nối sự kiện sẽ dùng nét liền.
            state.incidentLineStyle = resolveIncidentLineStyle(parsedConfig?.incidentLineStyle);
            // Map cũ chưa có: viền 1.5px, đường định hướng giữ màu sẵn có ({} = chưa đặt màu nào).
            // Luôn gán lại để đổi sang bản đồ khác không bị "dính" cấu hình của bản đồ trước.
            state.zoneBorderWidth = resolveZoneBorderWidth(parsedConfig?.zoneBorderWidth);
            state.flowLineColors = normalizeFlowLineColors(parsedConfig?.flowLineColors);
            state.flowLineWidth = resolveFlowLineWidth(parsedConfig?.flowLineWidth);
            // Tương thích ngược: bản map đã lưu blockConfig trong config sẽ được
            // đọc làm fallback và tự chuyển sang geoJsonContent ở lần lưu kế tiếp.
            const legacyBlockConfig = parsedConfig?.blockConfig;
            state.blockConfig = normalizeBlockConfiguration(legacyBlockConfig, {
                lat: viewState.center[0],
                lng: viewState.center[1],
            });
            state.blockConfigDirty = false;

            if (map_config.type === 'image') {
                state.mapBackgroundColor = parsedConfig?.backgroundColor || '#d3d3d3';
            } else {
                state.mapBackgroundColor = '#ffffff';
            }

            // asign view state
            state.mapViewState = { ...viewState };

            /**
             * Lưu view gốc để dùng cho chức năng reset-view.
             * Chỉ lưu 1 lần theo đúng cấu hình map lúc vừa load.
             */
            state.initialMapViewState = JSON.parse(JSON.stringify(viewState));

            // Áp dụng rotation (bearing) cho bản đồ
            if (viewState.rotation && state.map && typeof (state.map as any).setBearing === 'function') {
                (state.map as any).setBearing(viewState.rotation);
            }

            // Dispatch theo loại bản đồ: online (tile) hoặc image (hình ảnh)
            switch (map_config.type) {
                case MapTypeConfigEnum.Online: {
                    // Bản đồ online (tile server công cộng, đã bật CORS) → gọi thẳng, BỎ QUA proxy
                    await loadOnlineMap(map_config, viewState, null, false);
                    break;
                }
                case MapTypeConfigEnum.SelfHost: {
                    // Bản đồ tự host (tile server nội bộ) → định tuyến QUA proxy để tránh CORS/PNA
                    await loadOnlineMap(map_config, viewState, null, true);
                    break;
                }
                case MapTypeConfigEnum.Image: {
                    // Image map
                    await loadImageMap(map_config, viewState, hasSavedState);
                    break;
                }
                default:
                    console.error('Unsupported map type:', map_config.type);
                    return;
            }

            if (!isCurrentMapLoad()) return;

            // Khóa/mở zoom theo cấu hình — gọi SAU khi loadOnlineMap/loadImageMap đã set min/max + view
            applyZoomLock();
            applyRotationLock();

            // Load GeoJSON data
            const locationResponse = await getAPI(TmsMapLocationApi).apiTmsTmsmaplocationDatalistIdGet(mapId);
            if (!isCurrentMapLoad()) return;
            const mapLocationData = locationResponse?.data?.result?.[0];
            state.tmsLocationMapId = mapLocationData?.id;

            if (mapLocationData?.geoJsonContent && mapLocationData.geoJsonContent != null) {
                const parsed = JSON.parse(mapLocationData.geoJsonContent);
                // blockConfig nằm cùng FeatureCollection để quản lý đồng bộ với tọa độ map.
                // Ưu tiên dữ liệu GeoJSON mới, fallback config cũ để migration không mất dữ liệu.
                state.blockConfig = normalizeBlockConfiguration(parsed?.blockConfig ?? legacyBlockConfig, {
                    lat: viewState.center[0],
                    lng: viewState.center[1],
                });
                state.allGeoJsonData = {
                    ...parsed,
                    type: 'FeatureCollection',
                    features: parsed.features || [],
                    blockConfig: state.blockConfig,
                };
                state.geoJsonData = {
                    ...state.allGeoJsonData,
                    features: [...state.allGeoJsonData.features],
                };
                await new Promise((resolve) => setTimeout(resolve, 150));
                if (!isCurrentMapLoad()) return;
                await renderGeoJson();
            } else {
                state.allGeoJsonData = {
                    type: 'FeatureCollection',
                    features: [],
                    blockConfig: state.blockConfig,
                };
                state.geoJsonData = {
                    ...state.allGeoJsonData,
                    features: [],
                };
                await renderGeoJson();
            }
            if (!isCurrentMapLoad()) return;
            checkShowIconName();

            // Return Geo data json into component map control
            returnGeoDataJsonToComponentIndex();

            // Đánh dấu "config từ CSDL đã áp dụng xong" cho map hiện tại.
            // LocationEditor dựa vào mốc này để nạp lại CẢ 4 ô lat/lng (hiện tại + điều chỉnh)
            // đúng theo DB. Nếu chỉ dựa vào mapId, watcher sẽ fire sớm khi mapId đã đổi
            // nhưng mapViewState vẫn còn giá trị của map trước đó.
            state.mapConfigVersion = (state.mapConfigVersion ?? 0) + 1;
        } catch (error) {
            if (!isCurrentMapLoad()) return;
            console.error('Failed to load map data:', error);
        }
    };

    // #endregion

    // #region Khóa zoom

    /** Các handler zoom bằng thao tác người dùng (chuột/cảm ứng). */
    const ZOOM_HANDLERS = ['scrollWheelZoom', 'doubleClickZoom', 'touchZoom', 'boxZoom'] as const;

    /**
     * Khóa zoom trên bản đồ chính (runtime) khi config.lockZoom = true:
     * - Tắt zoom bằng lăn chuột / double-click / cảm ứng / kéo khung.
     * - Ép minZoom = maxZoom = mức zoom hiện tại → nút zoom, phím +/-, setView/flyTo đều không đổi được zoom.
     * Chế độ cấu hình (isEditMode) KHÔNG bao giờ khóa để người dùng còn chỉnh khung nhìn.
     * Khi không khóa chỉ bật lại handler; min/max đã được loadOnlineMap/loadImageMap đặt lại theo config.
     */
    const applyZoomLock = () => {
        const map = state.map as any;
        if (!map) return;
        const locked = !props.isEditMode && state.lockZoom === true;

        ZOOM_HANDLERS.forEach((name) => {
            const handler = map[name];
            if (!handler) return;
            if (locked) handler.disable();
            else if (map.options?.[name]) handler.enable();
        });

        if (locked) {
            const zoom = map.getZoom();
            map.setMinZoom(zoom);
            map.setMaxZoom(zoom);
        }
    };

    /**
     * Khóa xoay bản đồ chính khi config.lockRotation = true: tắt xoay bằng cảm ứng của leaflet-rotate.
     * Kéo chuột phải / Ctrl+kéo, la bàn và các nút xoay được chặn ở LeafletMap (isRotationLocked).
     * Góc xoay giữ nguyên góc đã cấu hình (loadMapData đã setBearing trước khi gọi hàm này).
     */
    const applyRotationLock = () => {
        const map = state.map as any;
        if (!map) return;
        const locked = !props.isEditMode && state.lockRotation === true;
        const handler = map.touchRotate;
        if (!handler) return;
        if (locked) handler.disable();
        else if (map.options?.touchRotate) handler.enable();
    };

    // #endregion

    // #region Public API

    return {
        applyZoomLock,
        applyRotationLock,
        loadMapData,
        loadOnlineMap,
        loadImageMap,
        centerImageMap,
    };

    // #endregion
}
