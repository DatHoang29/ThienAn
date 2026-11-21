import DOMPurify from 'dompurify';

/**
 * Lọc HTML của khối annotation (itemGroup='html') trước khi đưa lên bản đồ.
 *
 * Nội dung được lưu trong geojson của bản đồ (và có thể đến từ file GeoJSON nhập vào),
 * rồi hiển thị cho MỌI người xem màn theo dõi → bắt buộc lọc lúc hiển thị, không chỉ lúc lưu:
 * bỏ script, thuộc tính on*, link javascript:, và cả <style>/form (thẻ <style> chèn vào
 * marker sẽ đổi giao diện toàn trang). Ảnh base64 (data:image) vẫn được giữ.
 */
const SANITIZE_OPTIONS = {
    USE_PROFILES: { html: true },
    FORBID_TAGS: ['style', 'form', 'input', 'button', 'textarea', 'select', 'option', 'iframe', 'object', 'embed', 'link', 'meta', 'base'],
    FORBID_ATTR: ['id', 'name', 'target'],
};

// Bản đồ vẽ lại annotation mỗi khung hình khi kéo → nhớ kết quả theo chuỗi gốc để không lọc lại.
const CACHE_LIMIT = 100;
const sanitizeCache = new Map<string, string>();

export const sanitizeAnnotationHtml = (raw: unknown): string => {
    const html = String(raw ?? '');
    if (!html) return '';

    const cached = sanitizeCache.get(html);
    if (cached != null) return cached;

    const clean = String(DOMPurify.sanitize(html, SANITIZE_OPTIONS));
    if (sanitizeCache.size >= CACHE_LIMIT) sanitizeCache.delete(sanitizeCache.keys().next().value as string);
    sanitizeCache.set(html, clean);
    return clean;
};

/** Văn bản thuần (rút gọn) của khối HTML — dùng làm tên hiển thị trong danh sách vị trí. */
export const getAnnotationHtmlSummary = (raw: unknown, maxLength = 60): string => {
    const container = document.createElement('div');
    container.innerHTML = sanitizeAnnotationHtml(raw);
    const text = (container.textContent || '').replace(/\s+/g, ' ').trim();
    return text.length > maxLength ? `${text.slice(0, maxLength - 1)}…` : text;
};

/** Khối HTML có nội dung hiển thị được không (chữ hoặc ảnh/bảng/đường kẻ). */
export const hasAnnotationHtmlContent = (raw: unknown): boolean => {
    const container = document.createElement('div');
    container.innerHTML = sanitizeAnnotationHtml(raw);
    return !!(container.textContent || '').replace(/\u00a0/g, ' ').trim() || !!container.querySelector('img, table, hr');
};
