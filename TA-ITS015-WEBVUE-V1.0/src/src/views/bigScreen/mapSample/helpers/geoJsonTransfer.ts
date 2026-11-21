/**
 * Định dạng file xuất/nhập GeoJSON của màn cấu hình bản đồ.
 *
 * File là 1 FeatureCollection hợp lệ, kèm các "foreign member" (được chuẩn GeoJSON cho phép):
 * - meta:        thông tin nguồn (bản đồ, loại bản đồ, chế độ, phiên bản định dạng).
 * - mapConfig:   cấu hình cấp bản đồ — khung nhìn (tâm, zoom, xoay, giới hạn vùng) và
 *                hiển thị (icon, sự kiện, nhãn zone, màu nền bản đồ ảnh).
 * - blockConfig: cấu hình chế độ block (block, tầng, thiết bị, kiểu card).
 * - features:    thiết bị, zone, đơn vị, chữ/ảnh/khối HTML trên bản đồ.
 *
 * File cũ chỉ có features (+ blockConfig) vẫn nhập được: meta/mapConfig là tuỳ chọn.
 * Không phụ thuộc Vue/alias để chạy được bằng `node --test`.
 */

export const GEOJSON_TRANSFER_VERSION = 2;

/** Kiểu geometry ứng dụng render được: Point (marker/nhãn/ảnh/HTML) và Polygon (zone). */
export const SUPPORTED_GEOMETRY_TYPES = ['Point', 'Polygon'];

export const ZONE_LABEL_KEYS = ['name', 'kmArrow', 'arrowFrom', 'arrowTo', 'flowArrow', 'speed'] as const;
export type ZoneLabelKey = (typeof ZONE_LABEL_KEYS)[number];

export interface MapViewTransfer {
  latitude?: number;
  longtitude?: number;
  defaultZoom?: number;
  minZoom?: number;
  maxZoom?: number;
  rotation?: number;
  boundRadius?: number;
}

export interface MapDisplayTransfer {
  showIconBackground?: boolean;
  /** Khóa zoom trên bản đồ chính. */
  lockZoom?: boolean;
  /** Khóa xoay bản đồ chính. */
  lockRotation?: boolean;
  iconSize?: number;
  incidentIconSize?: number;
  incidentZoneGap?: number;
  incidentLineStyle?: string;
  /** Độ dày viền zone ở màn giám sát (px). */
  zoneBorderWidth?: number;
  /** Màu đường định hướng theo hướng (mã hướng → màu; '*' = mọi hướng). */
  flowLineColors?: Record<string, string>;
  /** Độ dày đường định hướng (px). */
  flowLineWidth?: number;
  /** Màu nền, chỉ có ý nghĩa với bản đồ dạng ảnh. */
  backgroundColor?: string;
  zoneLabelDisplay?: Partial<Record<ZoneLabelKey, boolean>>;
}

export interface MapConfigTransfer {
  view?: MapViewTransfer;
  display?: MapDisplayTransfer;
}

export interface GeoJsonTransferMeta {
  version?: number;
  mapId?: string;
  mapType?: string;
  mapMode?: string;
  exportedAt?: string;
}

export interface GeoJsonTransferInput {
  features: any[];
  blockConfig?: any;
  mapConfig?: MapConfigTransfer;
  meta?: GeoJsonTransferMeta;
}

export interface ParsedGeoJsonTransfer {
  features: any[];
  /** Số feature bị bỏ vì thiếu/sai geometry. */
  skipped: number;
  /** Số feature bị bỏ vì trùng id với feature đứng trước. */
  duplicates: number;
  blockConfig?: any;
  mapConfig?: MapConfigTransfer;
  meta?: GeoJsonTransferMeta;
}

const VIEW_KEYS: (keyof MapViewTransfer)[] = ['latitude', 'longtitude', 'defaultZoom', 'minZoom', 'maxZoom', 'rotation', 'boundRadius'];
const DISPLAY_NUMBER_KEYS = ['iconSize', 'incidentIconSize', 'incidentZoneGap', 'zoneBorderWidth', 'flowLineWidth'] as const;

const isPlainObject = (value: any) => !!value && typeof value === 'object' && !Array.isArray(value);

const toFiniteNumber = (value: any): number | undefined => {
  if (value === null || value === undefined || value === '') return undefined;
  const numberValue = Number(value);
  return Number.isFinite(numberValue) ? numberValue : undefined;
};

const toOptionalString = (value: any): string | undefined => {
  const text = typeof value === 'string' ? value.trim() : '';
  return text || undefined;
};

/** Bỏ các field undefined để file xuất gọn và so sánh round-trip ổn định. */
const compact = <T extends Record<string, any>>(value: T): T | undefined => {
  const entries = Object.entries(value).filter(([, item]) => item !== undefined);
  return entries.length ? (Object.fromEntries(entries) as T) : undefined;
};

const normalizeView = (value: any): MapViewTransfer | undefined => {
  if (!isPlainObject(value)) return undefined;
  return compact(Object.fromEntries(VIEW_KEYS.map((key) => [key, toFiniteNumber(value[key])])) as MapViewTransfer);
};

const normalizeZoneLabelDisplay = (value: any): MapDisplayTransfer['zoneLabelDisplay'] => {
  if (!isPlainObject(value)) return undefined;
  return compact(Object.fromEntries(ZONE_LABEL_KEYS.map((key) => [key, typeof value[key] === 'boolean' ? value[key] : undefined])));
};

/** Màu đường định hướng từ file: chỉ giữ mã màu hợp lệ; rỗng = không có trong file. */
const normalizeFlowLineColorsTransfer = (value: any): MapDisplayTransfer['flowLineColors'] => {
  if (!isPlainObject(value)) return undefined;
  // Giữ cùng quy tắc với normalizeFlowLineColors (helpers/zoneDisplayStyle.ts); viết lại ở đây
  // vì file này không import module khác để chạy được bằng `node --test`.
  const isColor = (c: string) => /^#[0-9a-f]{3,8}$/i.test(c) || /^rgba?\([\d\s.,%]+\)$/i.test(c);
  const colors: Record<string, string> = {};
  Object.entries(value).forEach(([key, color]) => {
    const k = String(key).trim();
    const c = typeof color === 'string' ? color.trim() : '';
    if (k && c && isColor(c)) colors[k] = c;
  });
  return Object.keys(colors).length ? colors : undefined;
};

const normalizeDisplay = (value: any): MapDisplayTransfer | undefined => {
  if (!isPlainObject(value)) return undefined;
  const display: MapDisplayTransfer = {
    showIconBackground: typeof value.showIconBackground === 'boolean' ? value.showIconBackground : undefined,
    lockZoom: typeof value.lockZoom === 'boolean' ? value.lockZoom : undefined,
    lockRotation: typeof value.lockRotation === 'boolean' ? value.lockRotation : undefined,
    incidentLineStyle: toOptionalString(value.incidentLineStyle),
    backgroundColor: toOptionalString(value.backgroundColor),
    zoneLabelDisplay: normalizeZoneLabelDisplay(value.zoneLabelDisplay),
    flowLineColors: normalizeFlowLineColorsTransfer(value.flowLineColors),
  };
  DISPLAY_NUMBER_KEYS.forEach((key) => {
    const numberValue = toFiniteNumber(value[key]);
    display[key] = numberValue !== undefined && numberValue >= 0 ? numberValue : undefined;
  });
  return compact(display);
};

/** Chuẩn hoá mapConfig từ file: bỏ field sai kiểu, giữ field hợp lệ. */
export const normalizeMapConfigTransfer = (value: any): MapConfigTransfer | undefined => {
  if (!isPlainObject(value)) return undefined;
  return compact({ view: normalizeView(value.view), display: normalizeDisplay(value.display) });
};

const normalizeMeta = (value: any): GeoJsonTransferMeta | undefined => {
  if (!isPlainObject(value)) return undefined;
  return compact({
    version: toFiniteNumber(value.version),
    mapId: toOptionalString(value.mapId),
    mapType: toOptionalString(value.mapType),
    mapMode: toOptionalString(value.mapMode),
    exportedAt: toOptionalString(value.exportedAt),
  });
};

/**
 * Feature hợp lệ: có geometry được hỗ trợ và toạ độ dạng mảng.
 * Bắt buộc lọc vì bảng vị trí đọc thẳng feature.geometry.type — feature thiếu
 * geometry sẽ làm cả khung cấu hình lỗi render.
 */
export const isImportableFeature = (feature: any) =>
  !!feature && SUPPORTED_GEOMETRY_TYPES.includes(feature?.geometry?.type) && Array.isArray(feature?.geometry?.coordinates);

/** Tạo nội dung file xuất. Thứ tự key: meta/cấu hình trước, features (có thể chứa ảnh base64 rất dài) sau cùng. */
export const buildGeoJsonTransfer = (input: GeoJsonTransferInput) => ({
  type: 'FeatureCollection',
  meta: { ...(input.meta || {}), version: GEOJSON_TRANSFER_VERSION },
  mapConfig: normalizeMapConfigTransfer(input.mapConfig) ?? {},
  blockConfig: isPlainObject(input.blockConfig) ? input.blockConfig : { blocks: [] },
  features: Array.isArray(input.features) ? input.features : [],
});

/**
 * Đọc dữ liệu từ file.
 * Chấp nhận FeatureCollection ({features, blockConfig, mapConfig, meta}) và mảng features thuần.
 * blockConfig cũ có thể nằm trong config.blockConfig nên đọc thêm nhánh đó.
 * Trả về null khi file không có danh sách features.
 */
export const parseGeoJsonTransfer = (parsed: any): ParsedGeoJsonTransfer | null => {
  const rawFeatures = Array.isArray(parsed) ? parsed : parsed?.features;
  if (!Array.isArray(rawFeatures)) return null;

  const validFeatures = rawFeatures.filter(isImportableFeature);
  // Trùng id sẽ làm 2 marker cùng trỏ 1 thiết bị và sửa/xoá nhầm nhau → giữ feature đầu tiên.
  const seenIds = new Set<string>();
  const features = validFeatures.filter((feature: any) => {
    const id = feature?.properties?.id;
    if (id === undefined || id === null || id === '') return true;
    const key = `${feature?.properties?.itemGroup ?? ''}:${id}`;
    if (seenIds.has(key)) return false;
    seenIds.add(key);
    return true;
  });

  const rawBlockConfig = Array.isArray(parsed) ? undefined : (parsed?.blockConfig ?? parsed?.config?.blockConfig);

  return {
    features,
    skipped: rawFeatures.length - validFeatures.length,
    duplicates: validFeatures.length - features.length,
    blockConfig: isPlainObject(rawBlockConfig) ? rawBlockConfig : undefined,
    mapConfig: Array.isArray(parsed) ? undefined : normalizeMapConfigTransfer(parsed?.mapConfig),
    meta: Array.isArray(parsed) ? undefined : normalizeMeta(parsed?.meta),
  };
};

/**
 * Bản đồ ảnh dùng toạ độ pixel (CRS.Simple), bản đồ online dùng lat/lng → không dùng chung toạ độ.
 * Chưa biết loại bản đồ nguồn (file cũ) thì coi là tương thích.
 */
export const isSameCoordinateSystem = (sourceMapType?: string, targetMapType?: string) => {
  if (!sourceMapType || !targetMapType) return true;
  return (sourceMapType === 'image') === (targetMapType === 'image');
};
