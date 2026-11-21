import L from 'leaflet';

/**
 * Các tiện ích thuần liên quan tới viewport/bounds/ảnh và tạo icon cho bản đồ Leaflet.
 * Không phụ thuộc state/Vue.
 */

/**
 * Đọc bounds đã lưu từ config JSON.
 * Trả về null nếu config cũ chưa có bounds hoặc dữ liệu sai format.
 */
export const parseSavedBounds = (savedState: any): L.LatLngBounds | null => {
    if (
        savedState?.bounds &&
        Array.isArray(savedState.bounds.southWest) &&
        Array.isArray(savedState.bounds.northEast) &&
        savedState.bounds.southWest.length === 2 &&
        savedState.bounds.northEast.length === 2
    ) {
        return L.latLngBounds(savedState.bounds.southWest, savedState.bounds.northEast);
    }

    return null;
};

/**
 * Tính vùng bounds theo center + minZoom.
 *
 * - Tạm thời set map về center + minZoom
 * - Lấy getBounds() của viewport hiện tại
 * - pad thêm một chút để tránh hở mép xám
 * - Sau đó trả map về view cũ
 */
export const buildBoundsByMinZoom = (
    map: L.Map,
    center: [number, number],
    minZoom: number,
    padRatio = 0.03
): L.LatLngBounds => {
    const oldCenter = map.getCenter();
    const oldZoom = map.getZoom();

    map.setView(center, minZoom, { animate: false });
    map.invalidateSize();

    const bounds = map.getBounds().pad(padRatio);

    // Trả lại trạng thái cũ để không làm giật UI
    map.setView(oldCenter, oldZoom, { animate: false });

    return bounds;
};

/**
 * Lấy kích thước (width/height) của hình ảnh theo URL.
 */
export const getImageSize = (url: string): Promise<{ width: number; height: number }> => {
    return new Promise((resolve, reject) => {
        const img = new window.Image();
        img.onload = () => {
            // naturalWidth/naturalHeight are the source dimensions. `width`/`height`
            // can be affected by browser layout rules and may return 0 in some WebViews.
            const width = img.naturalWidth || img.width;
            const height = img.naturalHeight || img.height;

            if (!width || !height) {
                reject(new Error(`Không xác định được kích thước ảnh: ${url}`));
                return;
            }

            resolve({ width, height });
        };
        img.onerror = () => reject(new Error(`Không tải được ảnh: ${url}`));
        // cache-buster so we always get size
        const canAppendQuery = !/^(data:|blob:)/i.test(url);
        img.src = canAppendQuery ? `${url}${url.includes('?') ? '&' : '?'}t=${Date.now()}` : url;
    });
};

/**
 * Tạo DivIcon với nền trắng tròn và tự động resize theo zoom
 * (CSS Variable: --incident-icon-scale).
 * @param iconUrl URL của icon hiển thị
 * @param className Tên class css (ví dụ: 'incident-marker-icon')
 * @param size Kích thước icon (mặc định 32)
 * @param flipX Lật ngang riêng ảnh bên trong (không đụng khung marker → không lệch với leaflet-rotate)
 */
export const createCustomDivIcon = (iconUrl: string, className: string, size: number = 32, fallbackUrl?: string, flipX = false): L.DivIcon => {
    const iconHtml = document.createElement('div');
    iconHtml.className = `${className} map-icon-background`;
    iconHtml.style.cssText = 'background-color:rgba(255,255,255,0.7);border-radius:50%;padding:4px;box-shadow:0 2px 6px rgba(0,0,0,0.3);width:100%;height:100%;display:flex;align-items:center;justify-content:center;transform:scale(var(--incident-icon-scale,1));transition:transform 0.2s ease-out';
    const img = document.createElement('img');
    img.style.cssText = `width:100%;height:100%;object-fit:contain${flipX ? ';transform:scaleX(-1)' : ''}`;
    img.onerror = () => {
        img.onerror = null;
        if (fallbackUrl) img.src = fallbackUrl;
        else img.style.display = 'none';
    };
    img.src = iconUrl;
    iconHtml.appendChild(img);

    return L.divIcon({
        html: iconHtml,
        className: '',
        iconSize: [size, size],
        iconAnchor: [size / 2, size / 2],
        popupAnchor: [0, -(size / 2)],
    });
};

/**
 * ================================================================
 * SVG <defs> cho zone polygon "premium" (màn cấu hình bản đồ - mapSample).
 * Tạo gradient fill/stroke DÙNG CHUNG cho mọi polygon, tiêm 1 LẦN vào <svg>
 * của Leaflet renderer (overlayPane). Nhờ vậy polygon có fill kiểu "kính"
 * (glass) nhiều dải sáng — nhìn sang & có chiều sâu hơn hẳn màu phẳng.
 * ================================================================
 */
const zoneDefsInjectedMaps = new WeakSet<L.Map>();

/** Tạo 1 phần tử <linearGradient> với các stop cho trước, trả về node để append vào <defs>. */
const buildLinearGradient = (id: string, stops: Array<{ offset: string; color: string; opacity: string }>): SVGLinearGradientElement => {
    const NS = 'http://www.w3.org/2000/svg';
    const grad = document.createElementNS(NS, 'linearGradient') as SVGLinearGradientElement;
    grad.setAttribute('id', id);
    // Chéo 135deg (góc trên-trái -> góc dưới-phải) mô phỏng ánh sáng chiếu lên mặt kính
    grad.setAttribute('x1', '0%');
    grad.setAttribute('y1', '0%');
    grad.setAttribute('x2', '100%');
    grad.setAttribute('y2', '100%');
    stops.forEach((s) => {
        const stop = document.createElementNS(NS, 'stop');
        stop.setAttribute('offset', s.offset);
        stop.setAttribute('stop-color', s.color);
        stop.setAttribute('stop-opacity', s.opacity);
        grad.appendChild(stop);
    });
    return grad;
};

/**
 * Đảm bảo <defs> chứa các gradient premium đã tồn tại trong SVG renderer của map.
 * Idempotent + an toàn gọi nhiều lần (no-op nếu SVG chưa sẵn sàng hoặc đã tiêm rồi).
 * Gọi lại sau khi đã add ít nhất 1 polygon vào map (thời điểm SVG renderer chắc chắn tồn tại).
 */
export const ensureZoneSvgDefs = (map: L.Map | null | undefined): void => {
    if (!map || zoneDefsInjectedMaps.has(map)) return;

    const svg = map.getPane('overlayPane')?.querySelector('svg') as SVGSVGElement | null;
    if (!svg) return; // Renderer chưa được tạo (chưa có path nào) — thử lại ở lượt render sau

    if (svg.querySelector('#tac-zone-defs')) {
        zoneDefsInjectedMaps.add(map);
        return;
    }

    const NS = 'http://www.w3.org/2000/svg';
    const defs = document.createElementNS(NS, 'defs');
    defs.setAttribute('id', 'tac-zone-defs');

    // Fill "kính" premium mặc định — cyan sáng → xanh dương sâu → chàm, tạo chiều sâu
    defs.appendChild(
        buildLinearGradient('tac-zone-fill-premium', [
            { offset: '0%', color: '#bff4ff', opacity: '0.6' },
            { offset: '32%', color: '#22d3ee', opacity: '0.32' },
            { offset: '68%', color: '#0284c7', opacity: '0.3' },
            { offset: '100%', color: '#3730a3', opacity: '0.48' },
        ])
    );

    // Fill "kính" khi zone có thay đổi chưa lưu — hổ phách rực
    defs.appendChild(
        buildLinearGradient('tac-zone-fill-unsaved', [
            { offset: '0%', color: '#fff7d6', opacity: '0.65' },
            { offset: '40%', color: '#fbbf24', opacity: '0.38' },
            { offset: '100%', color: '#b45309', opacity: '0.55' },
        ])
    );

    // Viền gradient "glassy edge" — sáng bừng ở giữa, dịu ở 2 đầu
    defs.appendChild(
        buildLinearGradient('tac-zone-stroke-premium', [
            { offset: '0%', color: '#e0fbff', opacity: '1' },
            { offset: '50%', color: '#22d3ee', opacity: '1' },
            { offset: '100%', color: '#67e8f9', opacity: '1' },
        ])
    );

    svg.appendChild(defs);
    zoneDefsInjectedMaps.add(map);
};

