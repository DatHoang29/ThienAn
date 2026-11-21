import L from 'leaflet';
import {
    createAnnotationGroupId,
    escapeAnnotationHtml,
    expandAnnotationSelection,
    findAnnotationFeature,
    getAnnotationFeatures,
    getAnnotationZoomScale,
    getAnnotationScaleMode,
    isAnnotationFeature,
    isAnnotationRotateWithMap,
    clampNumber,
    normalizeAngle,
    resolveHtmlScale,
    resolveHtmlWidth,
    resolveImageHeight,
    resolveImageWidth,
    resolveTextFontSize,
    HTML_SCALE_MAX,
    HTML_SCALE_MIN,
    IMAGE_SIZE_MAX,
    IMAGE_SIZE_MIN,
    TEXT_FONT_SIZE_MAX,
    TEXT_FONT_SIZE_MIN,
} from '../helpers/annotationUtils';
import type { AnnotationScaleMode } from '../helpers/annotationUtils';
import { sanitizeAnnotationHtml } from '../helpers/annotationHtml';

/** Tay cầm resize theo 1 chiều được phép hiển thị. */
type AxisHandle = 'width' | 'height';

const isHtmlFeature = (feature: any) => feature?.properties?.itemGroup === 'html';

/** Làm tròn hệ số phóng khối HTML về 3 chữ số thập phân, trong giới hạn cho phép. */
const roundHtmlScale = (value: number) => Math.round(clampNumber(value, HTML_SCALE_MIN, HTML_SCALE_MAX) * 1000) / 1000;

/** Kiểu thao tác đang thực hiện bằng chuột trên annotation. */
type AnnotationDragMode = 'move' | 'rotate' | 'scale' | 'width' | 'height';

interface AnnotationSnapshot {
    feature: any;
    id: string;
    /** Vị trí (container point) lúc bắt đầu thao tác. */
    point: { x: number; y: number };
    rotation: number;
    fontSize: number;
    width: number;
    height: number;
    /** Hệ số phóng riêng của khối HTML (1 với text/ảnh). */
    htmlScale: number;
    /** Hệ số zoom tại thời điểm bắt đầu — dùng quy đổi px màn hình ↔ px đã lưu. */
    zoomScale: number;
}

interface AnnotationSession {
    mode: AnnotationDragMode;
    center: { x: number; y: number };
    startPointer: { x: number; y: number };
    startAngle: number;
    startDistance: number;
    snapshots: AnnotationSnapshot[];
    moved: boolean;
}

/**
 * Composable quản lý các phần tử trang trí tự do trên bản đồ (annotation):
 * nhãn văn bản (itemGroup='text'), hình ảnh (itemGroup='image') và khối HTML (itemGroup='html').
 *
 * Chịu trách nhiệm:
 * 1. RENDER: vẽ annotation bằng divIcon, kèm khung chọn + tay cầm khi ở màn cấu hình.
 * 2. TƯƠNG TÁC: kéo di chuyển, xoay, resize (đều bằng chuột, không cần mở hộp thoại).
 * 3. LIÊN KẾT CỤM: nhiều annotation gộp thành 1 cụm (groupId) — chọn/kéo/xoay/resize
 *    theo cả cụm, và cụm co giãn đồng bộ khi zoom (xem helpers/annotationUtils.ts).
 * 4. CO GIÃN THEO ZOOM: scaleMode 'geo' nhân kích thước theo 2^(zoom - refZoom).
 *
 * Không tự sở hữu state: dùng chung reactive state của LeafletMap.vue (geoJsonData,
 * allGeoJsonData, markersLayer, unsavedChanges, annotationSelection...) giống các
 * composable khác của màn hình này.
 */
export function useMapAnnotations(deps: {
    state: any;
    props: { isEditMode?: boolean };
    locationEditorRef: any;
    /** Đánh dấu các feature vừa thay đổi là chưa lưu + render lại toàn bản đồ. */
    markUnsaved: (ids: string[]) => void;
    t: (key: string, params?: any) => string;
}) {
    const { state, props, locationEditorRef, markUnsaved, t } = deps;

    // #region Selection State

    /** Bảo đảm state có mảng selection (state được khai báo ở LeafletMap.vue). */
    const ensureSelection = (): string[] => {
        if (!Array.isArray(state.annotationSelection)) state.annotationSelection = [];
        return state.annotationSelection;
    };

    const getSelection = (): string[] => [...ensureSelection()];

    const isAnnotationSelected = (id: string) => ensureSelection().some((value) => String(value) === String(id));

    /**
     * Phần tử ĐANG NHẮM: phần tử được bấm gần nhất trong vùng chọn.
     *
     * Cần tách khỏi vùng chọn vì khi bấm vào 1 thành viên của cụm thì cả cụm được chọn —
     * nếu không có focus thì không biết mở hộp thoại sửa nội dung cho thành viên nào.
     */
    const getFocusId = (): string => (typeof state.annotationFocusId === 'string' ? state.annotationFocusId : '');

    const setFocus = (id: string) => {
        state.annotationFocusId = String(id ?? '');
    };

    const isAnnotationFocused = (id: string) => !!getFocusId() && getFocusId() === String(id);

    /** Feature đang nhắm; nếu chưa có mà vùng chọn chỉ có 1 phần tử thì lấy luôn phần tử đó. */
    const getFocusedFeature = (): any => {
        const focusId = getFocusId();
        if (focusId && isAnnotationSelected(focusId)) return findAnnotationFeature(state.geoJsonData?.features, focusId);

        const selectedFeatures = getSelectedFeatures();
        return selectedFeatures.length === 1 ? selectedFeatures[0] : null;
    };

    /** Gán selection mới (đã được mở rộng theo cụm ở phía gọi). */
    const setSelection = (ids: string[]) => {
        state.annotationSelection = Array.from(new Set(ids.map(String)));
    };

    const clearAnnotationSelection = () => {
        if (!ensureSelection().length && !getFocusId()) return;
        state.annotationSelection = [];
        setFocus('');
        refreshAnnotations();
    };

    /** Ctrl/Shift + bấm: thêm/bớt cả cụm của phần tử được bấm vào vùng chọn. */
    const toggleAnnotationSelection = (id: string) => {
        const groupIds = expandAnnotationSelection(state.geoJsonData?.features, id);
        const current = ensureSelection();
        const isRemoving = groupIds.every((groupMemberId) => current.includes(groupMemberId));
        setSelection(isRemoving ? current.filter((value) => !groupIds.includes(value)) : [...current, ...groupIds]);
    };

    /** Danh sách feature đang được chọn (theo thứ tự trong geoJsonData). */
    const getSelectedFeatures = (): any[] => {
        const ids = ensureSelection();
        return getAnnotationFeatures(state.geoJsonData?.features).filter((feature: any) => ids.includes(String(feature.properties?.id)));
    };

    // #endregion

    // #region Ghi dữ liệu (đồng bộ cả geoJsonData và allGeoJsonData)

    /**
     * Cập nhật 1 annotation ở CẢ 2 collection.
     *
     * `geoJsonData` là tập đang hiển thị (đã lọc), `allGeoJsonData` là tập đầy đủ dùng
     * khi lưu. Sau một vòng load lại từ CSDL, 2 tập là 2 object khác nhau nên phải ghi
     * cả hai, nếu không thay đổi sẽ mất khi lưu.
     */
    const updateAnnotation = (id: string, patch: { coordinates?: [number, number]; props?: Record<string, any> }) => {
        [state.geoJsonData, state.allGeoJsonData].forEach((collection: any) => {
            const feature = findAnnotationFeature(collection?.features, id);
            if (!feature) return;
            if (patch.coordinates) feature.geometry.coordinates = [...patch.coordinates];
            if (patch.props) feature.properties = { ...feature.properties, ...patch.props };
        });
    };

    const removeAnnotation = (id: string) => {
        [state.geoJsonData, state.allGeoJsonData].forEach((collection: any) => {
            if (!collection?.features) return;
            collection.features = collection.features.filter((feature: any) => String(feature.properties?.id) !== String(id));
        });
    };

    // #endregion

    // #region Chế độ co giãn theo zoom & liên kết cụm

    /**
     * Đổi chế độ co giãn nhưng GIỮ NGUYÊN kích thước đang thấy trên màn hình:
     * kích thước hiện tại (đã nhân hệ số zoom cũ) được "nướng" lại thành kích thước lưu
     * ứng với mốc zoom mới. Nhờ vậy bật/tắt "co giãn theo zoom" không làm nhảy hình.
     */
    const applyScaleMode = (feature: any, mode: AnnotationScaleMode) => {
        const properties = feature?.properties || {};
        const id = String(properties.id);
        const zoom = Number(state.map?.getZoom?.() ?? 0);
        const factor = getAnnotationZoomScale(properties, zoom);

        const patch: Record<string, any> = { scaleMode: mode, refZoom: mode === 'geo' ? zoom : undefined };

        if (properties.itemGroup === 'text') {
            patch.fontSize = clampNumber(Math.round(resolveTextFontSize(properties) * factor), TEXT_FONT_SIZE_MIN, TEXT_FONT_SIZE_MAX);
        } else if (properties.itemGroup === 'html') {
            // Khối HTML: nướng hệ số zoom vào `scale` để bề rộng khung (bố cục chữ) giữ nguyên.
            patch.scale = roundHtmlScale(resolveHtmlScale(properties) * factor);
        } else {
            patch.width = clampNumber(Math.round(resolveImageWidth(properties) * factor), IMAGE_SIZE_MIN, IMAGE_SIZE_MAX);
            const height = resolveImageHeight(properties);
            if (height) patch.height = clampNumber(Math.round(height * factor), IMAGE_SIZE_MIN, IMAGE_SIZE_MAX);
        }

        updateAnnotation(id, { props: patch });
    };

    /** Bật/tắt co giãn theo zoom cho toàn bộ annotation đang chọn. */
    const setAnnotationSelectionScaleMode = (mode: AnnotationScaleMode) => {
        const features = getSelectedFeatures();
        if (!features.length) return;

        features.forEach((feature) => applyScaleMode(feature, mode));
        markUnsaved(features.map((feature) => String(feature.properties.id)));
    };

    /**
     * Liên kết các annotation đang chọn thành 1 cụm.
     *
     * Cụm bắt buộc dùng scaleMode='geo' với CÙNG một refZoom: khoảng cách pixel giữa các
     * toạ độ lat/lng và kích thước từng phần tử khi đó cùng nhân 2^(zoom - refZoom), nên
     * cả cụm (ví dụ: chữ ở trên + mũi tên ở dưới) zoom lên xuống như một khối.
     */
    const groupAnnotationSelection = () => {
        const features = getSelectedFeatures();
        if (features.length < 2) return;

        const groupId = createAnnotationGroupId();
        const zoom = Number(state.map?.getZoom?.() ?? 0);

        features.forEach((feature) => {
            // Nướng kích thước về mốc zoom hiện tại trước, rồi mới gán cụm → giữ nguyên hình.
            applyScaleMode(feature, 'geo');
            updateAnnotation(String(feature.properties.id), { props: { groupId, scaleMode: 'geo', refZoom: zoom } });
        });

        markUnsaved(features.map((feature) => String(feature.properties.id)));
    };

    /** Bỏ liên kết cụm (giữ nguyên vị trí, kích thước và chế độ co giãn). */
    const ungroupAnnotationSelection = () => {
        const features = getSelectedFeatures().filter((feature) => feature.properties?.groupId);
        if (!features.length) return;

        features.forEach((feature) => updateAnnotation(String(feature.properties.id), { props: { groupId: undefined } }));
        markUnsaved(features.map((feature) => String(feature.properties.id)));
    };

    /**
     * Xoá annotation đang chọn. Giống removeFeatureById của LocationEditor: feature bị loại
     * khỏi geojson và sẽ mất hẳn ở lần lưu tiếp theo.
     */
    const deleteAnnotationSelection = () => {
        const ids = getSelectedFeatures().map((feature) => String(feature.properties.id));
        if (!ids.length) return;

        ids.forEach(removeAnnotation);
        state.annotationSelection = [];
        setFocus('');
        markUnsaved([]);
    };

    /** Mở hộp thoại chỉnh sửa chi tiết của annotation (nội dung, màu, hình dạng...). */
    const openAnnotationEditor = (feature: any) => {
        if (!props.isEditMode || !feature) return;
        if (feature.properties?.itemGroup === 'text') locationEditorRef.value?.openTextEditor?.(feature);
        else if (isHtmlFeature(feature)) locationEditorRef.value?.openHtmlEditor?.(feature);
        else locationEditorRef.value?.openImageEditor?.(feature);
    };

    /**
     * Mở hộp thoại cho phần tử ĐANG NHẮM (dùng bởi thanh công cụ và tay cầm bút chì).
     * Nhờ vậy vẫn sửa được nội dung riêng của từng thành viên khi đang chọn cả cụm.
     */
    const openSelectedAnnotationEditor = () => openAnnotationEditor(getFocusedFeature());

    // #endregion

    // #region Thao tác chuột: kéo / xoay / resize

    let session: AnnotationSession | null = null;
    let refreshFrame: number | null = null;

    const getContainerPoint = (event: PointerEvent) => {
        const map = state.map;
        const point = map.mouseEventToContainerPoint(event as any);
        return { x: point.x, y: point.y };
    };

    const featureContainerPoint = (feature: any) => {
        const [lng, lat] = feature.geometry.coordinates;
        const point = state.map.latLngToContainerPoint([lat, lng]);
        return { x: point.x, y: point.y };
    };

    const containerPointToCoordinates = (point: { x: number; y: number }): [number, number] => {
        const latLng = state.map.containerPointToLatLng([point.x, point.y]);
        return [latLng.lng, latLng.lat];
    };

    /** Gom nhiều lần pointermove vào 1 lần vẽ lại trong mỗi khung hình. */
    const scheduleRefresh = () => {
        if (refreshFrame != null) return;
        refreshFrame = requestAnimationFrame(() => {
            refreshFrame = null;
            refreshAnnotations();
        });
    };

    const buildSnapshots = (features: any[]): AnnotationSnapshot[] => {
        const zoom = Number(state.map?.getZoom?.() ?? 0);
        return features.map((feature) => ({
            feature,
            id: String(feature.properties.id),
            point: featureContainerPoint(feature),
            rotation: Number(feature.properties?.rotation) || 0,
            fontSize: resolveTextFontSize(feature.properties),
            width: isHtmlFeature(feature) ? resolveHtmlWidth(feature.properties) : resolveImageWidth(feature.properties),
            height: isHtmlFeature(feature) ? 0 : resolveImageHeight(feature.properties),
            htmlScale: isHtmlFeature(feature) ? resolveHtmlScale(feature.properties) : 1,
            zoomScale: getAnnotationZoomScale(feature.properties, zoom),
        }));
    };

    /** Tâm phép biến đổi: trọng tâm của cả cụm (1 phần tử → chính nó). */
    const getTransformCenter = (snapshots: AnnotationSnapshot[]) => {
        const total = snapshots.reduce((accumulator, snapshot) => ({ x: accumulator.x + snapshot.point.x, y: accumulator.y + snapshot.point.y }), { x: 0, y: 0 });
        return { x: total.x / snapshots.length, y: total.y / snapshots.length };
    };

    const startSession = (event: PointerEvent, mode: AnnotationDragMode) => {
        const map = state.map;
        if (!map) return;

        const features = getSelectedFeatures();
        if (!features.length) return;

        // Resize theo 1 chiều (chỉ ảnh) không áp dụng cho cụm — tránh làm méo bố cục cụm.
        if ((mode === 'width' || mode === 'height') && features.length > 1) return;

        const snapshots = buildSnapshots(features);
        const center = getTransformCenter(snapshots);
        const startPointer = getContainerPoint(event);

        session = {
            mode,
            center,
            startPointer,
            startAngle: Math.atan2(startPointer.y - center.y, startPointer.x - center.x),
            startDistance: Math.max(4, Math.hypot(startPointer.x - center.x, startPointer.y - center.y)),
            snapshots,
            moved: false,
        };

        map.dragging?.disable();
        document.addEventListener('pointermove', handlePointerMove);
        document.addEventListener('pointerup', handlePointerUp, { once: true });
        document.addEventListener('pointercancel', handlePointerUp, { once: true });
    };

    /** Góc xoay hiện tại của bản đồ (độ); 0 nếu không bật leaflet-rotate. */
    const getMapBearing = (): number => {
        const bearing = Number(state.map?.getBearing?.());
        return Number.isFinite(bearing) ? bearing : 0;
    };

    /** Quay 1 điểm quanh tâm (đơn vị radian) trong hệ toạ độ container. */
    const rotatePoint = (point: { x: number; y: number }, center: { x: number; y: number }, radian: number) => {
        const cos = Math.cos(radian);
        const sin = Math.sin(radian);
        const dx = point.x - center.x;
        const dy = point.y - center.y;
        return { x: center.x + dx * cos - dy * sin, y: center.y + dx * sin + dy * cos };
    };

    /** Nhân kích thước đã lưu của 1 phần tử theo hệ số (dùng cho resize đồng đều). */
    const buildScaledSizeProps = (snapshot: AnnotationSnapshot, factor: number) => {
        if (snapshot.feature.properties?.itemGroup === 'text') {
            return { fontSize: clampNumber(Math.round(snapshot.fontSize * factor), TEXT_FONT_SIZE_MIN, TEXT_FONT_SIZE_MAX) };
        }
        // Khối HTML: phóng cả khối (chữ + ảnh) bằng `scale`, không đổi bố cục xuống dòng.
        if (isHtmlFeature(snapshot.feature)) return { scale: roundHtmlScale(snapshot.htmlScale * factor) };

        const props: Record<string, any> = { width: clampNumber(Math.round(snapshot.width * factor), IMAGE_SIZE_MIN, IMAGE_SIZE_MAX) };
        if (snapshot.height) props.height = clampNumber(Math.round(snapshot.height * factor), IMAGE_SIZE_MIN, IMAGE_SIZE_MAX);
        return props;
    };

    const applyMove = (pointer: { x: number; y: number }) => {
        const deltaX = pointer.x - session!.startPointer.x;
        const deltaY = pointer.y - session!.startPointer.y;

        session!.snapshots.forEach((snapshot) => {
            updateAnnotation(snapshot.id, {
                coordinates: containerPointToCoordinates({ x: snapshot.point.x + deltaX, y: snapshot.point.y + deltaY }),
            });
        });
    };

    const applyRotate = (pointer: { x: number; y: number }) => {
        const center = session!.center;
        const deltaRadian = Math.atan2(pointer.y - center.y, pointer.x - center.x) - session!.startAngle;
        const deltaDegree = (deltaRadian * 180) / Math.PI;

        session!.snapshots.forEach((snapshot) => {
            updateAnnotation(snapshot.id, {
                // Cụm: vừa quay vị trí quanh tâm cụm, vừa quay bản thân từng phần tử.
                coordinates: containerPointToCoordinates(rotatePoint(snapshot.point, center, deltaRadian)),
                props: { rotation: Math.round(normalizeAngle(snapshot.rotation + deltaDegree) * 10) / 10 },
            });
        });
    };

    const applyUniformScale = (pointer: { x: number; y: number }) => {
        const center = session!.center;
        const distance = Math.hypot(pointer.x - center.x, pointer.y - center.y);
        const factor = clampNumber(distance / session!.startDistance, 0.05, 20);

        session!.snapshots.forEach((snapshot) => {
            updateAnnotation(snapshot.id, {
                coordinates: containerPointToCoordinates({
                    x: center.x + (snapshot.point.x - center.x) * factor,
                    y: center.y + (snapshot.point.y - center.y) * factor,
                }),
                props: buildScaledSizeProps(snapshot, factor),
            });
        });
    };

    /**
     * Resize 1 chiều cho ảnh (và bề rộng khung khối HTML): chiếu con trỏ lên trục X/Y RIÊNG của ảnh (đã xoay) rồi
     * quy đổi từ px màn hình về px lưu trữ bằng cách chia hệ số zoom.
     */
    const applyAxisResize = (pointer: { x: number; y: number }, axis: 'width' | 'height') => {
        const snapshot = session!.snapshots[0];
        if (!snapshot) return;

        // Góc trên màn hình = góc riêng (+ góc xoay bản đồ nếu annotation cố định theo bản đồ).
        const screenRotation = snapshot.rotation + (isAnnotationRotateWithMap(snapshot.feature.properties) ? getMapBearing() : 0);
        const radian = (screenRotation * Math.PI) / 180;
        const dx = pointer.x - snapshot.point.x;
        const dy = pointer.y - snapshot.point.y;
        const local = axis === 'width' ? dx * Math.cos(radian) + dy * Math.sin(radian) : -dx * Math.sin(radian) + dy * Math.cos(radian);
        // Tay cầm nằm ở giữa cạnh → khoảng cách tới tâm bằng nửa kích thước.
        const screenSize = Math.abs(local) * 2;
        // Khối HTML còn nhân thêm hệ số phóng riêng → chia cả 2 để ra bề rộng khung đã lưu.
        const storedSize = clampNumber(Math.round(screenSize / ((snapshot.zoomScale || 1) * (snapshot.htmlScale || 1))), IMAGE_SIZE_MIN, IMAGE_SIZE_MAX);

        updateAnnotation(snapshot.id, { props: axis === 'width' ? { width: storedSize } : { height: storedSize } });
    };

    const handlePointerMove = (event: PointerEvent) => {
        if (!session || !state.map) return;

        const pointer = getContainerPoint(event);
        session.moved = true;

        if (session.mode === 'move') applyMove(pointer);
        else if (session.mode === 'rotate') applyRotate(pointer);
        else if (session.mode === 'scale') applyUniformScale(pointer);
        else applyAxisResize(pointer, session.mode);

        scheduleRefresh();
    };

    const handlePointerUp = () => {
        document.removeEventListener('pointermove', handlePointerMove);
        document.removeEventListener('pointerup', handlePointerUp);
        document.removeEventListener('pointercancel', handlePointerUp);

        if (refreshFrame != null) {
            cancelAnimationFrame(refreshFrame);
            refreshFrame = null;
        }

        state.map?.dragging?.enable();

        const currentSession = session;
        session = null;
        if (!currentSession) return;

        if (currentSession.moved) markUnsaved(currentSession.snapshots.map((snapshot) => snapshot.id));
        else refreshAnnotations();
    };

    /** Dọn thao tác đang treo khi component unmount / map bị hủy. */
    const stopAnnotationInteraction = () => {
        if (session) handlePointerUp();
    };

    const handleAnnotationPointerDown = (event: PointerEvent, feature: any) => {
        if (!props.isEditMode || event.button !== 0) return;

        const id = String(feature.properties?.id ?? '');
        if (!id) return;

        const handleTarget = (event.target as HTMLElement | null)?.closest?.('[data-anno-handle]');
        const mode = (handleTarget?.getAttribute('data-anno-handle') as AnnotationDragMode | 'edit' | null) || 'move';

        // Chế độ "di chuyển" bật từ bảng vị trí dùng cơ chế kéo của Leaflet → không tự xử lý.
        if (mode === 'move' && state.currentMode === 'move' && String(state.activeFeatureId) === id) return;

        // Chỉ chặn lan sự kiện. KHÔNG preventDefault ở pointerdown vì Chrome sẽ chặn luôn
        // chuỗi mousedown/mouseup → mất sự kiện dblclick mở hộp thoại. Việc chặn bôi đen
        // văn bản do CSS user-select:none của .map-anno đảm nhiệm.
        event.stopPropagation();

        // Tay cầm bút chì: mở luôn hộp thoại sửa nội dung của CHÍNH phần tử này,
        // kể cả khi đang chọn cả cụm.
        if (mode === 'edit') {
            setFocus(id);
            openAnnotationEditor(feature);
            return;
        }

        // Bấm vào phần tử nào thì phần tử đó thành phần tử đang nhắm (để sửa nội dung riêng).
        const previousFocusId = getFocusId();
        setFocus(id);

        // Ctrl/Shift + bấm: chỉ chọn thêm/bớt, không kéo.
        if (mode === 'move' && (event.ctrlKey || event.shiftKey || event.metaKey)) {
            toggleAnnotationSelection(id);
            refreshAnnotations();
            return;
        }

        // Alt + bấm: tách riêng 1 thành viên ra khỏi cụm để kéo/xoay/resize một mình
        // (không phá liên kết cụm, chỉ giới hạn phạm vi thao tác lần này).
        const isIsolating = mode === 'move' && event.altKey;
        let needsRefresh = previousFocusId !== id;

        if (isIsolating) {
            setSelection([id]);
            needsRefresh = true;
        } else if (!isAnnotationSelected(id)) {
            setSelection(expandAnnotationSelection(state.geoJsonData?.features, id));
            needsRefresh = true;
        }

        // Vẽ lại để chuyển viền "đang nhắm" / khung chọn sang đúng phần tử.
        if (needsRefresh) refreshAnnotations();

        // Nhấp đúp mở hộp thoại (xử lý ở listener dblclick) → không bắt đầu kéo.
        if (event.detail > 1) return;

        startSession(event, mode);
    };

    // #endregion

    // #region Render

    /** Tay cầm chỉ hiện khi ở màn cấu hình và annotation đang được chọn. */
    const buildHandlesHtml = (options: { isSelected: boolean; isFocused: boolean; axisHandles: AxisHandle[]; zoomScale: number }) => {
        if (!options.isSelected) return '';

        // Tay cầm được phản-tỉ-lệ để luôn to bằng nhau trên màn hình, kể cả khi
        // annotation đang được nhân kích thước theo zoom.
        const zoomScale = options.zoomScale;
        const axisHandleTitles: Record<AxisHandle, string> = {
            width: t('lz.label.tmsMapLocation.annotationWidthHandle'),
            height: t('lz.label.tmsMapLocation.annotationHeightHandle'),
        };
        const counterScale = zoomScale && zoomScale !== 1 ? ` style="transform: scale(${(1 / zoomScale).toFixed(4)})"` : '';
        const handle = (name: string, extraClass: string, title: string, inner = '') =>
            `<span class="map-anno__handle map-anno__handle--${extraClass}" data-anno-handle="${name}" title="${escapeAnnotationHtml(title)}"${counterScale}>${inner}</span>`;

        // Biểu tượng bút chì (SVG nội tuyến) cho tay cầm mở hộp thoại sửa nội dung.
        const pencilIcon =
            '<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M3 17.25V21h3.75L17.81 9.94l-3.75-3.75L3 17.25zm17.71-10.21a1 1 0 0 0 0-1.41l-2.34-2.34a1 1 0 0 0-1.41 0l-1.83 1.83 3.75 3.75 1.83-1.83z"/></svg>';

        return (
            '<span class="map-anno__frame" aria-hidden="true"></span>' +
            handle('rotate', 'rotate', t('lz.label.tmsMapLocation.annotationRotateHandle')) +
            handle('scale', 'scale', t('lz.label.tmsMapLocation.annotationScaleHandle')) +
            // Chỉ phần tử đang nhắm mới hiện nút bút chì, tránh rối khi cả cụm được chọn.
            (options.isFocused ? handle('edit', 'edit', t('lz.label.tmsMapLocation.annotationEditHandle'), pencilIcon) : '') +
            options.axisHandles.map((axis) => handle(axis, axis, axisHandleTitles[axis])).join('')
        );
    };

    /** Class chung của lớp bọc: trạng thái chọn / đang nhắm / thuộc cụm / chưa lưu. */
    const buildWrapperClass = (extraClass: string, options: { isSelected: boolean; isFocused: boolean; isGrouped: boolean; hasUnsaved: boolean; isEditMode: boolean }) =>
        [
            'map-anno',
            extraClass,
            options.isEditMode ? 'map-anno--editable' : '',
            options.isSelected ? 'is-selected' : '',
            options.isFocused ? 'is-focused' : '',
            options.isGrouped ? 'is-grouped' : '',
            options.hasUnsaved ? 'is-unsaved' : '',
        ]
            .filter(Boolean)
            .join(' ');

    /**
     * Tạo marker cho annotation và gắn tương tác.
     * `isAnnotation: true` trong options để refreshAnnotations biết layer nào cần vẽ lại.
     */
    const addAnnotationMarker = (feature: any, html: string, zIndexOffset: number) => {
        const id = String(feature.properties.id);
        const [lng, lat] = feature.geometry.coordinates;

        const marker = L.marker([lat, lng], {
            icon: L.divIcon({ className: 'map-anno-wrap', html, iconSize: [0, 0], iconAnchor: [0, 0] }),
            draggable: false,
            // Góc riêng nằm ở .map-anno. Bật "cố định theo bản đồ" thì marker cộng thêm góc
            // xoay bản đồ (leaflet-rotate) — xoay bản đồ thì annotation xoay cùng; tắt thì
            // annotation luôn giữ hướng trên màn hình.
            rotation: 0,
            rotateWithView: isAnnotationRotateWithMap(feature.properties),
            zIndexOffset,
            id,
            isAnnotation: true,
        } as any);
        (marker as any).feature = feature;

        // Tương thích chế độ "di chuyển" cũ bật từ bảng vị trí (dùng kéo của Leaflet).
        if (state.currentMode === 'move' && id === String(state.activeFeatureId)) {
            marker.options.draggable = true;
            marker.on('dragend', (event: any) => {
                const position = event.target.getLatLng();
                updateAnnotation(id, { coordinates: [position.lng, position.lat] });
                markUnsaved([id]);
            });
        }

        state.markersLayer?.addLayer(marker);

        const element = marker.getElement();
        if (!element) return;

        // Ở chế độ theo dõi, annotation chỉ là trang trí: KHÔNG chặn sự kiện (và CSS đặt
        // pointer-events:none) để người dùng vẫn kéo/zoom bản đồ khi con trỏ nằm trên nó.
        if (!props.isEditMode) return;

        // Chế độ cấu hình: chặn lan sự kiện để không pan/zoom/xoay map khi đang thao tác.
        L.DomEvent.disableClickPropagation(element);

        element.addEventListener('pointerdown', (event: PointerEvent) => handleAnnotationPointerDown(event, feature));
        element.addEventListener('dblclick', (event: MouseEvent) => {
            event.preventDefault();
            event.stopPropagation();
            openAnnotationEditor(feature);
        });
    };

    /**
     * Nhãn văn bản tự do (itemGroup='text').
     * Hỗ trợ: nội dung, cỡ chữ, xoay, hình dạng (none|rect|pill|outline|underline), màu chữ,
     * màu nền, in đậm, và co giãn theo zoom (scaleMode/refZoom).
     */
    const renderTextAnnotation = (feature: any) => {
        const properties = feature.properties || {};
        const id = String(properties.id);
        const isSelected = props.isEditMode && isAnnotationSelected(id);
        const isFocused = isSelected && (isAnnotationFocused(id) || ensureSelection().length === 1);
        const zoomScale = getAnnotationZoomScale(properties, state.map?.getZoom?.());

        const text = escapeAnnotationHtml(properties.text ?? properties.name ?? '');
        const rotation = Number(properties.rotation) || 0;
        const shape = properties.shape || 'none';
        const backgroundColor = String(properties.bgColor ?? '').trim();
        const hasUnsaved = state.unsavedChanges.has(id);

        // Style nội tuyến CHỈ gồm cỡ chữ / màu chữ / độ đậm / màu nền. Padding, bo góc và
        // viền do class .map-text-shape--* quyết định để inline không đè lên hình dạng.
        const bodyStyle = [
            `font-size:${resolveTextFontSize(properties)}px`,
            `color:${properties.textColor || '#1f2d3d'}`,
            `font-weight:${properties.bold === false ? 400 : 700}`,
        ];
        if (backgroundColor) bodyStyle.push(`background:${backgroundColor}`);

        const wrapperClass = buildWrapperClass('map-anno--text', {
            isSelected,
            isFocused,
            isGrouped: !!properties.groupId,
            hasUnsaved,
            isEditMode: !!props.isEditMode,
        });
        const html =
            `<div class="${wrapperClass}" style="transform: rotate(${rotation}deg) scale(${zoomScale});">` +
            `<div class="map-anno__body map-text-annotation map-text-shape--${shape}${backgroundColor ? ' has-bg' : ''}" style="${bodyStyle.join('; ')};">` +
            `${text}${hasUnsaved ? '<span class="map-text-unsaved-dot"></span>' : ''}` +
            `</div>` +
            buildHandlesHtml({ isSelected, isFocused, axisHandles: [], zoomScale }) +
            `</div>`;

        addAnnotationMarker(feature, html, 900);
    };

    /**
     * Hình ảnh tự do (itemGroup='image'). Ảnh lưu base64 trong properties.imageUrl nên
     * hiển thị được mà không cần file server.
     * Hỗ trợ: rộng/cao (cao = 0 → tự động theo tỉ lệ), xoay, độ mờ, bo góc, co giãn theo zoom.
     */
    const renderImageAnnotation = (feature: any) => {
        const properties = feature.properties || {};
        const imageUrl = String(properties.imageUrl ?? '').trim();
        if (!imageUrl) return;

        const id = String(properties.id);
        const isSelected = props.isEditMode && isAnnotationSelected(id);
        const isFocused = isSelected && (isAnnotationFocused(id) || ensureSelection().length === 1);
        const zoomScale = getAnnotationZoomScale(properties, state.map?.getZoom?.());

        const rotation = Number(properties.rotation) || 0;
        const opacityValue = Number(properties.opacity);
        const opacity = Number.isFinite(opacityValue) && opacityValue >= 0 && opacityValue <= 1 ? opacityValue : 1;
        const borderRadius = Number(properties.borderRadius) > 0 ? Number(properties.borderRadius) : 0;
        const height = resolveImageHeight(properties);
        const hasUnsaved = state.unsavedChanges.has(id);

        const wrapperClass = buildWrapperClass('map-anno--image', {
            isSelected,
            isFocused,
            isGrouped: !!properties.groupId,
            hasUnsaved,
            isEditMode: !!props.isEditMode,
        });
        const html =
            `<div class="${wrapperClass}" style="transform: rotate(${rotation}deg) scale(${zoomScale}); width:${resolveImageWidth(properties)}px;${height ? ` height:${height}px;` : ''}">` +
            `<div class="map-anno__body map-image-annotation" style="opacity:${opacity};">` +
            `<img src="${escapeAnnotationHtml(imageUrl)}" alt="" draggable="false" style="border-radius:${borderRadius}px;${height ? ' height:100%; object-fit:fill;' : ''}" />` +
            `${hasUnsaved ? '<span class="map-image-unsaved-dot"></span>' : ''}` +
            `</div>` +
            buildHandlesHtml({ isSelected, isFocused, axisHandles: ['width', 'height'], zoomScale }) +
            `</div>`;

        addAnnotationMarker(feature, html, 800);
    };

    /**
     * Khối HTML tự do (itemGroup='html'): chữ định dạng + nhiều ảnh trong cùng 1 phần tử,
     * soạn bằng Jodit. HTML luôn được lọc (sanitize) trước khi đưa lên bản đồ.
     * Kích thước: `width` = bề rộng khung (chữ tự xuống dòng), `scale` = phóng cả khối.
     */
    const renderHtmlAnnotation = (feature: any) => {
        const properties = feature.properties || {};
        const html = sanitizeAnnotationHtml(properties.html);
        if (!html) return;

        const id = String(properties.id);
        const isSelected = !!props.isEditMode && isAnnotationSelected(id);
        const isFocused = isSelected && (isAnnotationFocused(id) || ensureSelection().length === 1);
        const zoomScale = getAnnotationZoomScale(properties, state.map?.getZoom?.());
        const totalScale = zoomScale * resolveHtmlScale(properties);
        const rotation = Number(properties.rotation) || 0;
        const hasUnsaved = state.unsavedChanges.has(id);

        const wrapperClass = buildWrapperClass('map-anno--html', {
            isSelected,
            isFocused,
            isGrouped: !!properties.groupId,
            hasUnsaved,
            isEditMode: !!props.isEditMode,
        });
        const markup =
            `<div class="${wrapperClass}" style="transform: rotate(${rotation}deg) scale(${totalScale}); width:${resolveHtmlWidth(properties)}px;">` +
            `<div class="map-anno__body map-html-annotation">${html}</div>` +
            `${hasUnsaved ? '<span class="map-image-unsaved-dot"></span>' : ''}` +
            buildHandlesHtml({ isSelected, isFocused, axisHandles: ['width'], zoomScale: totalScale }) +
            `</div>`;

        addAnnotationMarker(feature, markup, 850);
    };

    /** Điều phối theo itemGroup; trả về false nếu feature không phải annotation. */
    const renderAnnotationFeature = (feature: any): boolean => {
        if (!isAnnotationFeature(feature)) return false;

        if (feature.properties.itemGroup === 'text') renderTextAnnotation(feature);
        else if (isHtmlFeature(feature)) renderHtmlAnnotation(feature);
        else renderImageAnnotation(feature);
        return true;
    };

    /**
     * Vẽ lại RIÊNG các annotation (không dựng lại toàn bộ marker/polygon của bản đồ).
     * Dùng khi: đổi vùng chọn, kéo/xoay/resize theo từng khung hình, và khi zoom thay đổi
     * (annotation scaleMode='geo' phải đổi kích thước theo mức zoom mới).
     */
    const refreshAnnotations = () => {
        if (!state.map || !state.markersLayer) return;

        state.markersLayer
            .getLayers()
            .filter((layer: any) => layer.options?.isAnnotation)
            .forEach((layer: any) => state.markersLayer.removeLayer(layer));

        getAnnotationFeatures(state.geoJsonData?.features).forEach(renderAnnotationFeature);
    };

    /**
     * Bỏ khỏi vùng chọn những id không còn tồn tại (bị xoá hoặc bị lọc khỏi bản đồ).
     * Gọi trước mỗi lần render toàn bộ để thanh công cụ không trỏ vào feature đã mất.
     */
    const pruneAnnotationSelection = () => {
        const ids = ensureSelection();
        if (!ids.length && !getFocusId()) return;

        const availableIds = getAnnotationFeatures(state.geoJsonData?.features).map((feature: any) => String(feature.properties?.id));
        const nextIds = ids.filter((id) => availableIds.includes(id));
        if (nextIds.length !== ids.length) state.annotationSelection = nextIds;
        if (getFocusId() && !nextIds.includes(getFocusId())) setFocus('');
    };

    // #endregion

    // #region Public API

    return {
        renderAnnotationFeature,
        refreshAnnotations,
        pruneAnnotationSelection,
        stopAnnotationInteraction,
        // Vùng chọn
        getSelectedFeatures,
        getFocusedFeature,
        clearAnnotationSelection,
        toggleAnnotationSelection,
        // Thao tác trên vùng chọn
        groupAnnotationSelection,
        ungroupAnnotationSelection,
        setAnnotationSelectionScaleMode,
        deleteAnnotationSelection,
        openSelectedAnnotationEditor,
        // Tra cứu phụ trợ cho UI
        getAnnotationScaleMode,
    };

    // #endregion
}
