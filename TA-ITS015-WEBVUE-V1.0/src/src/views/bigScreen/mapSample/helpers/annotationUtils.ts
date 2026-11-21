/**
 * Tiện ích dùng chung cho các phần tử trang trí tự do (annotation) trên bản đồ:
 * - nhãn văn bản: feature.properties.itemGroup === 'text'
 * - hình ảnh:     feature.properties.itemGroup === 'image'
 * - khối HTML:    feature.properties.itemGroup === 'html' (chữ + hình tự do, soạn bằng Jodit)
 *
 * ===== CÁCH TÍNH KÍCH THƯỚC KHI ZOOM (quan trọng) =====
 * Mỗi annotation có `scaleMode`:
 * - 'fixed': kích thước luôn cố định theo pixel màn hình (giữ nguyên hành vi cũ,
 *   dùng cho các cấu hình đã lưu trước đây khi chưa có trường này).
 * - 'geo'  : kích thước "dính" vào bản đồ. Kích thước lưu trong properties được coi
 *   là kích thước tại mức zoom `refZoom`; khi bản đồ ở zoom Z thì hệ số phóng là
 *   2^(Z - refZoom).
 *
 * Vì sao 'geo' làm cả CỤM zoom đồng bộ: khoảng cách pixel giữa 2 toạ độ lat/lng cũng
 * giãn theo đúng 2^(Z - Z0). Cho nên khi các phần tử trong cụm dùng cùng một refZoom,
 * cả khoảng cách giữa chúng lẫn kích thước bản thân chúng đều nhân cùng một hệ số →
 * cụm (ví dụ: chữ thông tin ở trên + mũi tên ở dưới) giữ nguyên tỉ lệ bố trí khi zoom.
 */

/** Các itemGroup được coi là annotation tự do. */
export const ANNOTATION_ITEM_GROUPS = ['text', 'image', 'html'] as const;
export type AnnotationItemGroup = (typeof ANNOTATION_ITEM_GROUPS)[number];

/** Chế độ co giãn kích thước theo zoom. */
export type AnnotationScaleMode = 'fixed' | 'geo';

/** Chặn trên/dưới hệ số zoom để annotation không biến mất hoặc phình quá lớn. */
export const ANNOTATION_ZOOM_SCALE_MIN = 0.15;
export const ANNOTATION_ZOOM_SCALE_MAX = 8;

/** Giới hạn kích thước khi kéo tay cầm (resize) trực tiếp trên bản đồ. */
export const TEXT_FONT_SIZE_MIN = 6;
export const TEXT_FONT_SIZE_MAX = 400;
export const IMAGE_SIZE_MIN = 8;
export const IMAGE_SIZE_MAX = 4000;

/** Giá trị mặc định khi thiếu dữ liệu. */
export const TEXT_FONT_SIZE_DEFAULT = 14;
export const IMAGE_WIDTH_DEFAULT = 120;
/** Khối HTML: `width` = bề rộng khung (nội dung tự xuống dòng), `scale` = phóng to/thu nhỏ cả khối. */
export const HTML_WIDTH_DEFAULT = 260;
export const HTML_SCALE_MIN = 0.1;
export const HTML_SCALE_MAX = 20;

export const clampNumber = (value: number, min: number, max: number) => Math.min(max, Math.max(min, value));

/** Đưa góc về khoảng [0, 360) để lưu trữ nhất quán. */
export const normalizeAngle = (angle: number) => ((angle % 360) + 360) % 360;

const createId = (prefix: string) => `${prefix}_${Date.now()}_${Math.floor(Math.random() * 1e4)}`;

export const createAnnotationId = (group: AnnotationItemGroup) => createId(group);
export const createAnnotationGroupId = () => createId('annogrp');

/** Feature có phải annotation tự do (điểm text/ảnh) hay không. */
export const isAnnotationFeature = (feature: any): boolean =>
    feature?.geometry?.type === 'Point' && ANNOTATION_ITEM_GROUPS.includes(feature?.properties?.itemGroup);

/**
 * Annotation có "cố định theo bản đồ" không: true = xoay bản đồ thì chữ/ảnh xoay theo;
 * false = luôn giữ hướng trên màn hình. Thiếu field = true (mặc định cố định theo bản đồ).
 */
export const isAnnotationRotateWithMap = (properties: any): boolean => properties?.rotateWithMap !== false;

/** Thiếu/không hợp lệ → 'fixed' để cấu hình cũ giữ nguyên hiển thị như trước. */
export const getAnnotationScaleMode = (properties: any): AnnotationScaleMode => (properties?.scaleMode === 'geo' ? 'geo' : 'fixed');

/**
 * Hệ số phóng kích thước của annotation ở mức zoom hiện tại.
 * 'fixed' → luôn 1. 'geo' → 2^(zoom - refZoom), có chặn biên.
 */
export const getAnnotationZoomScale = (properties: any, zoom: number | null | undefined): number => {
    if (getAnnotationScaleMode(properties) !== 'geo') return 1;

    const refZoom = Number(properties?.refZoom);
    if (!Number.isFinite(refZoom) || !Number.isFinite(Number(zoom))) return 1;

    return clampNumber(Math.pow(2, Number(zoom) - refZoom), ANNOTATION_ZOOM_SCALE_MIN, ANNOTATION_ZOOM_SCALE_MAX);
};

/** Cỡ chữ đã lưu (chưa nhân hệ số zoom). */
export const resolveTextFontSize = (properties: any) => {
    const fontSize = Number(properties?.fontSize);
    return Number.isFinite(fontSize) && fontSize > 0 ? fontSize : TEXT_FONT_SIZE_DEFAULT;
};

/** Chiều rộng ảnh đã lưu (chưa nhân hệ số zoom). */
export const resolveImageWidth = (properties: any) => {
    const width = Number(properties?.width);
    return Number.isFinite(width) && width > 0 ? width : IMAGE_WIDTH_DEFAULT;
};

/** Chiều cao ảnh: 0/không có = tự động theo tỉ lệ ảnh gốc. */
export const resolveImageHeight = (properties: any) => {
    const height = Number(properties?.height);
    return Number.isFinite(height) && height > 0 ? height : 0;
};

/** Hệ số phóng của khối HTML đã lưu (chưa nhân hệ số zoom). */
export const resolveHtmlScale = (properties: any) => {
    const scale = Number(properties?.scale);
    return Number.isFinite(scale) && scale > 0 ? clampNumber(scale, HTML_SCALE_MIN, HTML_SCALE_MAX) : 1;
};

/** Bề rộng khung khối HTML đã lưu (chưa nhân hệ số zoom / scale). */
export const resolveHtmlWidth = (properties: any) => {
    const width = Number(properties?.width);
    return Number.isFinite(width) && width > 0 ? width : HTML_WIDTH_DEFAULT;
};

/** Tất cả annotation trong một FeatureCollection. */
export const getAnnotationFeatures = (features: any[] | undefined): any[] => (features || []).filter(isAnnotationFeature);

/** Annotation theo id (chỉ tìm trong các nhóm annotation). */
export const findAnnotationFeature = (features: any[] | undefined, id: string): any =>
    (features || []).find((feature: any) => isAnnotationFeature(feature) && String(feature.properties?.id) === String(id));

/** Danh sách id các thành viên cùng một cụm (groupId). */
export const getGroupMemberIds = (features: any[] | undefined, groupId: string): string[] =>
    getAnnotationFeatures(features)
        .filter((feature: any) => feature.properties?.groupId && String(feature.properties.groupId) === String(groupId))
        .map((feature: any) => String(feature.properties.id));

/**
 * Mở rộng 1 id thành đủ cụm của nó: bấm vào một phần tử đã liên kết thì chọn cả cụm,
 * nhờ vậy mọi thao tác kéo/xoay/resize luôn tác động lên toàn cụm.
 */
export const expandAnnotationSelection = (features: any[] | undefined, id: string): string[] => {
    const feature = findAnnotationFeature(features, id);
    if (!feature) return [];

    const groupId = feature.properties?.groupId;
    if (!groupId) return [String(feature.properties.id)];

    const memberIds = getGroupMemberIds(features, groupId);
    return memberIds.length ? memberIds : [String(feature.properties.id)];
};

/** Escape HTML để chèn nội dung người dùng vào divIcon an toàn (tránh phá markup/XSS). */
export const escapeAnnotationHtml = (value: any): string =>
    String(value ?? '')
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;');
