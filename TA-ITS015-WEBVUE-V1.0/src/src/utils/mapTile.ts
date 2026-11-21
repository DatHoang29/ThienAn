import L from 'leaflet';

/**
 * Tiện ích tile bản đồ: định tuyến qua proxy cùng origin + tile layer chống treo.
 *
 * Vì sao cần: trang chạy ở IP public qua HTTP, còn tile server thường nằm ở dải private
 * (vd 10.x.x.x). Chrome chặn request public -> private (Private Network Access / PNA)
 * TRƯỚC cả khi xét CORS, nên không header CORS nào cứu được. Cách duy nhất phía frontend
 * là đẩy mọi request tile qua CÙNG ORIGIN (đường dẫn tương đối) để proxy phía server fetch hộ.
 */

/** GIF trong suốt 1x1 dùng thay cho tile lỗi (không hiện icon vỡ, không retry vô hạn). */
export const TRANSPARENT_TILE = 'data:image/gif;base64,R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7';

let zoomGuardsInstalled = false;

/**
 * Bọc `_animateZoom` của các layer để KHÔNG crash khi `_map` đã null.
 *
 * Vì sao cần: khi zoom (đặc biệt với leaflet-rotate), Leaflet fire sự kiện `zoomanim`
 * tới mọi layer đang đăng ký. Nếu một layer đã bị gỡ khỏi map nhưng listener còn sót
 * (do timing/leaflet-rotate), `_animateZoom` sẽ đọc `this._map._latLngToNewLayerPoint`
 * với `_map` null → ném "Cannot read properties of null" và làm vỡ cả animation.
 *
 * Guard chỉ bỏ qua khi không còn map (không có gì để animate) → an toàn, không đổi hành vi
 * lúc map còn sống. Idempotent, áp 1 lần cho toàn app.
 */
export const installLeafletZoomAnimationGuards = (): void => {
    if (zoomGuardsInstalled) return;
    zoomGuardsInstalled = true;

    const LA = L as any;
    // Thứ tự: patch lớp cha (DivOverlay) trước lớp con (Popup/Tooltip) để tránh bọc trùng.
    const prototypes: any[] = [
        LA.Marker?.prototype,
        LA.ImageOverlay?.prototype,
        LA.DivOverlay?.prototype,
        LA.Popup?.prototype,
        LA.Tooltip?.prototype,
        LA.GridLayer?.prototype, // TileLayer kế thừa GridLayer
    ];

    for (const proto of prototypes) {
        const orig = proto && proto._animateZoom;
        if (typeof orig !== 'function' || orig.__mapGuarded) continue;

        const guarded = function (this: any, e: any) {
            if (!this._map) return; // layer đã rời map → không animate, tránh crash
            try {
                return orig.call(this, e);
            } catch {
                /* nuốt lỗi zoom-anim khi layer đang teardown */
            }
        };
        (guarded as any).__mapGuarded = true;
        proto._animateZoom = guarded;
    }
};

/** Ngưỡng số tile lỗi trước khi dừng tải bản đồ (mặc định). */
export const DEFAULT_MAX_TILE_ERRORS = 40;

/**
 * Chuyển tile URL gốc (từ DB) sang URL proxy CÙNG ORIGIN để tránh CORS/PNA.
 *
 *   - Dev: Vite middleware `/tile-proxy` trong vite.config.ts xử lý.
 *   - Production: web server (nginx/IIS) phục vụ app PHẢI forward `/tile-proxy/*` về tile server.
 *
 * URL tương đối hoặc đã cùng origin với trang thì giữ nguyên (không cần proxy).
 */
export const getTileProxyUrl = (originalUrl: string): string => {
    if (!originalUrl) return originalUrl;

    // Chỉ proxy URL absolute (http:// hoặc https://). URL tương đối -> giữ nguyên.
    let urlObj: URL;
    try {
        urlObj = new URL(originalUrl.replace(/\{[^}]+\}/g, '__PLACEHOLDER__'));
    } catch {
        return originalUrl;
    }

    // Tile đã cùng origin với trang -> không cần proxy.
    if (urlObj.origin === window.location.origin) return originalUrl;

    const origin = urlObj.origin; // vd http://10.10.8.113:8080
    const pathAndQuery = originalUrl.substring(origin.length); // vd /tile/{z}/{x}/{y}.png
    return `/tile-proxy/${encodeURIComponent(origin)}${pathAndQuery}`;
};

/** Số request tile chạy song song tối đa (mặc định). Bằng giới hạn kết nối/host
 *  của trình duyệt với HTTP/1.1 → phần còn lại xếp hàng trong JS (hủy được) thay
 *  vì dồn vào network stack của trình duyệt gây ERR_INSUFFICIENT_RESOURCES. */
export const DEFAULT_MAX_CONCURRENT_TILE_REQUESTS = 6;

export interface SafeTileLayerOptions extends L.TileLayerOptions {
    /** Số tile lỗi tối đa trước khi dừng (mặc định DEFAULT_MAX_TILE_ERRORS). */
    maxTileErrors?: number;
    /** Callback gọi 1 lần khi vượt ngưỡng lỗi (để gỡ layer, báo người dùng...). */
    onTooManyErrors?: (layer: L.TileLayer) => void;
    /** Số request tile chạy song song tối đa (mặc định DEFAULT_MAX_CONCURRENT_TILE_REQUESTS). */
    maxConcurrentRequests?: number;
    /**
     * Có định tuyến tile URL qua proxy cùng origin (`/tile-proxy`) hay không. Mặc định true.
     *   - Bản đồ SelfHost (tile server nội bộ, dải private) → true để tránh CORS/PNA.
     *   - Bản đồ Online (tile server công cộng, đã bật CORS) → false, gọi thẳng cho nhanh.
     */
    useProxy?: boolean;
}

/**
 * TileLayer có GIỚI HẠN SỐ REQUEST SONG SONG.
 *
 * Vì sao cần: Leaflet mặc định phát request cho TẤT CẢ tile trong viewport cùng lúc.
 * Khi định tuyến qua proxy (mỗi tile proxy phải fetch lại từ server gốc nên chậm),
 * lúc pan/zoom liên tục số request dồn lại rất nhanh → Chrome trả
 * `net::ERR_INSUFFICIENT_RESOURCES` vì hết slot request.
 *
 * Cách xử lý: hoãn việc gán `img.src` (thời điểm thật sự phát request) qua 1 hàng đợi
 * trong JS; chỉ cho tối đa `maxConcurrentRequests` request chạy đồng thời. Tile bị
 * Leaflet loại bỏ (pan/zoom) trước khi tới lượt sẽ bị HỦY khỏi hàng đợi → không bao
 * giờ phát request thừa.
 */
const QueuedTileLayer = (L.TileLayer as any).extend({
    initialize(url: string, options: any) {
        (L.TileLayer.prototype as any).initialize.call(this, url, options);
        this._reqQueue = [];
        this._activeReqs = 0;
    },

    createTile(coords: any, done: any) {
        const tile: any = document.createElement('img');

        // Bọc `done` để nhả 1 slot request khi tile load xong/lỗi rồi xử lý tile kế tiếp.
        const releaseThenDone = (err: any, t: any) => {
            if (tile._reqActive) {
                this._activeReqs = Math.max(0, this._activeReqs - 1);
                tile._reqActive = false;
                this._processReqQueue();
            }
            done(err, t);
        };

        L.DomEvent.on(tile, 'load', (L.Util as any).bind((L.TileLayer.prototype as any)._tileOnLoad, this, releaseThenDone, tile));
        L.DomEvent.on(tile, 'error', (L.Util as any).bind((L.TileLayer.prototype as any)._tileOnError, this, releaseThenDone, tile));

        if (this.options.crossOrigin || this.options.crossOrigin === '') {
            tile.crossOrigin = this.options.crossOrigin === true ? '' : this.options.crossOrigin;
        }
        if (typeof this.options.referrerPolicy === 'string') {
            tile.referrerPolicy = this.options.referrerPolicy;
        }
        tile.alt = '';
        tile.setAttribute('role', 'presentation');

        // Chưa gán src ngay — xếp vào hàng đợi, chờ có slot mới phát request.
        tile._reqUrl = this.getTileUrl(coords);
        tile._reqCancelled = false;
        this._reqQueue.push(tile);
        this._processReqQueue();

        return tile;
    },

    /** Phát request cho các tile trong hàng đợi tới khi đạt ngưỡng song song. */
    _processReqQueue() {
        const max = this.options.maxConcurrentRequests || DEFAULT_MAX_CONCURRENT_TILE_REQUESTS;
        while (this._activeReqs < max && this._reqQueue.length > 0) {
            const tile = this._reqQueue.shift();
            if (!tile || tile._reqCancelled) continue; // tile đã bị pan/zoom loại bỏ → bỏ qua
            tile._reqActive = true;
            this._activeReqs++;
            tile.src = tile._reqUrl; // thời điểm thật sự phát request mạng
        }
    },

    /** Khi Leaflet loại tile (pan/zoom): hủy nếu còn trong hàng đợi, hoặc abort nếu đang tải. */
    _removeTile(key: any) {
        const entry = this._tiles[key];
        const tile: any = entry && entry.el;
        if (tile) {
            tile._reqCancelled = true;
            if (tile._reqActive) {
                // Đang tải dở → nhả slot và abort request bằng cách gán ảnh rỗng.
                this._activeReqs = Math.max(0, this._activeReqs - 1);
                tile._reqActive = false;
                tile.src = (L.Util as any).emptyImageUrl;
                this._processReqQueue();
            }
        }
        (L.TileLayer.prototype as any)._removeTile.call(this, key);
    },
});

/**
 * Tạo tile layer kèm cơ chế chống treo:
 * - errorTileUrl trong suốt thay cho tile lỗi.
 * - Giới hạn số request song song (queue) để tránh ERR_INSUFFICIENT_RESOURCES.
 * - Đếm `tileerror`; vượt ngưỡng thì gọi `onTooManyErrors` (1 lần) để dừng phát request.
 *
 * Tránh việc tile lỗi (PNA/CORS/502/mất mạng) khiến Leaflet phát request liên tục gây treo trang.
 */
export const createSafeTileLayer = (url: string, options: SafeTileLayerOptions = {}): L.TileLayer => {
    const { maxTileErrors = DEFAULT_MAX_TILE_ERRORS, onTooManyErrors, maxConcurrentRequests = DEFAULT_MAX_CONCURRENT_TILE_REQUESTS, useProxy = true, ...tileOptions } = options;

    let errorCount = 0;
    let fired = false;

    // Online → dùng URL gốc; SelfHost → định tuyến qua proxy cùng origin.
    const finalUrl = useProxy ? getTileProxyUrl(url) : url;

    const layer = new QueuedTileLayer(finalUrl, {
        errorTileUrl: TRANSPARENT_TILE,
        maxConcurrentRequests,
        // Giảm số tile phát request trong lúc thao tác: chỉ tải khi dừng pan và
        // không tải tile trung gian khi đang zoom → giảm mạnh lượng request dồn.
        updateWhenIdle: true,
        updateWhenZooming: false,
        ...tileOptions,
    }) as L.TileLayer;

    layer.on('tileerror', () => {
        errorCount++;
        if (errorCount < maxTileErrors || fired) return;
        fired = true;
        onTooManyErrors?.(layer);
    });

    return layer;
};
