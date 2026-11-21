export interface BlockPosition {
  lat: number;
  lng: number;
}

export interface BlockDevice {
  id: string;
  deviceId: string;
  /** Lật ngang icon thiết bị trong card (vd 2 barrier quay mặt trái/phải). */
  flipX?: boolean;
}

/**
 * Cách bố trí thiết bị trong 1 tầng:
 * - 'flow' (mặc định, không lưu): xếp hàng ngang tự xuống dòng, căn giữa như trước.
 * - 'route': đặt theo lý trình (Km) trên 1 thanh ngang, độ rộng tự đo theo chiều dài tuyến/hầm.
 */
export type BlockLevelLayout = 'flow' | 'route';

export interface BlockLevel {
  id: string;
  name: string;
  devices: BlockDevice[];
  /** Bỏ trống = 'flow'. Xem BlockLevelLayout. */
  layout?: BlockLevelLayout;
  /** Chỉ dùng khi layout = 'route': Km giảm dần từ trái sang phải. */
  routeReverse?: boolean;
  /**
   * Chỉ dùng khi layout = 'route': khoảng lý trình cố định của tầng (mét = km*1000+met).
   * Điền từ zone hầm/tuyến, từ cấu hình cơ sở (base_start/end_position) hoặc nhập tay.
   * Thiếu 1 trong 2 hoặc đầu >= cuối = tự đo từ thiết bị.
   */
  rangeFrom?: number;
  rangeTo?: number;
  /** Zone hầm/tuyến đã chọn để điền khoảng + lọc thiết bị (chỉ để hiển thị lại trong editor). */
  zoneId?: string;
  /** Hướng của tầng (DirectionId thiết bị: 1/2) dùng khi tự điền thiết bị; bỏ trống = mọi hướng. */
  directionId?: number;
  /**
   * Chỉ dùng khi layout = 'route': mỗi LOẠI thiết bị nằm ở dòng riêng trong cùng tầng (cùng 1 thanh, cùng độ rộng,
   * Km thẳng hàng giữa các dòng). Bỏ trống = các loại xếp chung theo Km như cũ.
   */
  splitRowsByType?: boolean;
}

/** Kiểu nét vẽ của đường nối block. */
export type BlockLineStyle = 'solid' | 'dashed' | 'dotted';

/**
 * Phạm vi làm mờ khi bật chế độ mờ:
 * - 'block': mờ cả card (nền + icon thiết bị + chữ).
 * - 'background': chỉ mờ nền/khung card, icon và chữ vẫn rõ.
 */
export type BlockTranslucentScope = 'block' | 'background';

export interface MapBlock {
  id: string;
  name: string;
  /** Thiết bị đại diện cho vị trí thực tế, dùng làm đầu cuối của đường nối. */
  anchorDeviceId?: string;
  /** Tọa độ thực tế khi block chưa neo vào một thiết bị. */
  actualPosition: BlockPosition;
  /** Tọa độ đặt card block trên bản đồ. */
  displayPosition: BlockPosition;
  levels: BlockLevel[];
  /**
   * Màu chấm tròn đánh dấu vị trí thực tế. Bỏ trống = dùng màu theo theme
   * (BLOCK_ANCHOR_FALLBACK_COLOR) để vẫn đúng ở cả dark/light mode.
   */
  anchorColor?: string;
  /** Bán kính chấm tròn vị trí (px). Bỏ trống = BLOCK_ANCHOR_DEFAULT_RADIUS. */
  anchorRadius?: number;
  /** Màu đường nối từ vị trí thực tế tới card block. Bỏ trống = màu theo theme. */
  lineColor?: string;
  /** Độ dày đường nối (px). Bỏ trống = BLOCK_LINE_DEFAULT_WIDTH. */
  lineWidth?: number;
  /** Kiểu nét đường nối. Bỏ trống = 'solid'. */
  lineStyle?: BlockLineStyle;
  /**
   * Các điểm bẻ góc của đường nối, theo thứ tự từ vị trí thực tế → card hiển thị.
   * Rỗng = đường nối thẳng như trước.
   */
  waypoints: BlockPosition[];
}

export interface BlockConfiguration {
  blocks: MapBlock[];
  /** Hiển thị tên tầng (level.name) trong card block trên bản đồ. Cấu hình cũ chưa có = true. */
  showLevelName: boolean;
  /** Hiển thị tên block (tiêu đề card) trên bản đồ. Cấu hình cũ chưa có = true. */
  showBlockName: boolean;
  /** Chế độ mờ (alpha): card block trong suốt một phần để nhìn thấy bản đồ phía sau. Mặc định tắt. */
  translucent: boolean;
  /** Độ đậm của card khi bật chế độ mờ (0.1 → 1). */
  translucentOpacity: number;
  /** Mờ cả block hay chỉ mờ nền (icon/chữ vẫn rõ). Mặc định 'block'. */
  translucentScope: BlockTranslucentScope;
  /** Rê chuột vào card thì hiện rõ lại như bình thường (true) hay vẫn giữ mờ (false). */
  opaqueOnHover: boolean;
  /** Độ rộng tối thiểu của mọi card block (px). Nhỏ lại để card ít thiết bị không chiếm chỗ. Cấu hình cũ = 160. */
  cardMinWidth: number;
}

/** Độ rộng tối thiểu card block: mặc định giữ như cũ (160px), cho phép từ 0 đến độ rộng tối đa của card. */
export const BLOCK_CARD_MIN_WIDTH_DEFAULT = 160;
export const BLOCK_CARD_MIN_WIDTH_MAX = 360;

/** Màu mặc định (theo theme) khi block chưa cấu hình màu riêng. */
export const BLOCK_LINE_FALLBACK_COLOR = 'var(--el-text-color-secondary)';
export const BLOCK_ANCHOR_FALLBACK_COLOR = 'var(--el-text-color-primary)';
export const BLOCK_LINE_DEFAULT_WIDTH = 2;
export const BLOCK_ANCHOR_DEFAULT_RADIUS = 5;
export const BLOCK_TRANSLUCENT_DEFAULT_OPACITY = 0.5;
export const BLOCK_TRANSLUCENT_MIN_OPACITY = 0.1;

/** Mẫu nét vẽ SVG (stroke-dasharray) theo từng kiểu nét. */
export const BLOCK_LINE_DASH_ARRAY: Record<BlockLineStyle, string> = {
  solid: 'none',
  dashed: '8 5',
  dotted: '2 4',
};

const createId = (prefix: string) => `${prefix}_${Date.now()}_${Math.random().toString(36).slice(2, 8)}`;

const toPosition = (value: any, fallback: BlockPosition): BlockPosition => ({
  lat: Number(value?.lat ?? fallback.lat),
  lng: Number(value?.lng ?? fallback.lng),
});

/** Giữ nguyên `undefined` khi người dùng chưa chọn màu để component fallback theo theme. */
const toOptionalColor = (value: any): string | undefined => {
  const color = String(value ?? '').trim();
  return color || undefined;
};

/** Chỉ nhận số dương; ngược lại trả về undefined để dùng giá trị mặc định. */
const toOptionalPositiveNumber = (value: any): number | undefined => {
  const numberValue = Number(value);
  return Number.isFinite(numberValue) && numberValue > 0 ? numberValue : undefined;
};

const toLineStyle = (value: any): BlockLineStyle | undefined => {
  const style = String(value ?? '').trim();
  return style === 'dashed' || style === 'dotted' || style === 'solid' ? style : undefined;
};

/** Bỏ các điểm bẻ góc không hợp lệ (thiếu toạ độ) để không làm hỏng polyline. */
const toWaypoints = (value: any): BlockPosition[] =>
  Array.isArray(value)
    ? value
        .map((point: any) => ({ lat: Number(point?.lat), lng: Number(point?.lng) }))
        .filter((point: BlockPosition) => Number.isFinite(point.lat) && Number.isFinite(point.lng))
    : [];

/** Chỉ giữ khoảng/zone/hướng hợp lệ của tầng 'route'; không có thì không ghi field nào. */
const toRouteLevelExtras = (level: any): Pick<BlockLevel, 'rangeFrom' | 'rangeTo' | 'zoneId' | 'directionId' | 'splitRowsByType'> => {
  const extras: Pick<BlockLevel, 'rangeFrom' | 'rangeTo' | 'zoneId' | 'directionId' | 'splitRowsByType'> = {};
  const from = level?.rangeFrom;
  const to = level?.rangeTo;
  const isNumber = (value: any) => value !== null && value !== '' && value !== undefined && Number.isFinite(Number(value));
  if (isNumber(from) && isNumber(to) && Number(to) > Number(from)) {
    extras.rangeFrom = Math.round(Number(from));
    extras.rangeTo = Math.round(Number(to));
  }
  const zoneId = String(level?.zoneId ?? '').trim();
  if (zoneId) extras.zoneId = zoneId;
  if (isNumber(level?.directionId) && Number(level.directionId) > 0) extras.directionId = Math.round(Number(level.directionId));
  if (level?.splitRowsByType === true) extras.splitRowsByType = true;
  return extras;
};

export const createEmptyBlockConfiguration = (): BlockConfiguration => ({
  blocks: [],
  showLevelName: true,
  showBlockName: true,
  translucent: false,
  translucentOpacity: BLOCK_TRANSLUCENT_DEFAULT_OPACITY,
  translucentScope: 'block',
  opaqueOnHover: true,
  cardMinWidth: BLOCK_CARD_MIN_WIDTH_DEFAULT,
});

/** Độ rộng tối thiểu hợp lệ trong [0, 360]; thiếu/sai = mặc định 160. */
const toCardMinWidth = (value: any): number => {
  const width = Number(value);
  if (value == null || value === '' || !Number.isFinite(width)) return BLOCK_CARD_MIN_WIDTH_DEFAULT;
  return Math.min(BLOCK_CARD_MIN_WIDTH_MAX, Math.max(0, Math.round(width)));
};

/** Độ đậm hợp lệ trong [0.1, 1]; thiếu/sai = mặc định 0.5. */
const toTranslucentOpacity = (value: any): number => {
  const opacity = Number(value);
  if (value == null || value === '' || !Number.isFinite(opacity)) return BLOCK_TRANSLUCENT_DEFAULT_OPACITY;
  return Math.min(1, Math.max(BLOCK_TRANSLUCENT_MIN_OPACITY, opacity));
};

/** Cấu hình hiển thị chung của mọi block (không phụ thuộc danh sách block). */
const normalizeDisplayOptions = (value: any) => ({
  // Chỉ tắt khi đã lưu tường minh false, để cấu hình cũ (chưa có field) giữ nguyên hiển thị
  showLevelName: value?.showLevelName !== false,
  showBlockName: value?.showBlockName !== false,
  translucent: value?.translucent === true,
  translucentOpacity: toTranslucentOpacity(value?.translucentOpacity),
  translucentScope: (value?.translucentScope === 'background' ? 'background' : 'block') as BlockTranslucentScope,
  opaqueOnHover: value?.opaqueOnHover !== false,
  cardMinWidth: toCardMinWidth(value?.cardMinWidth),
});

export const createMapBlock = (position: BlockPosition, index = 1): MapBlock => ({
  id: createId('block'),
  name: `Block ${index}`,
  actualPosition: { ...position },
  displayPosition: { ...position },
  waypoints: [],
  levels: [
    {
      id: createId('level'),
      name: 'Tầng 1',
      devices: [],
    },
  ],
});

/** Chuẩn hóa cấu hình cũ/không đầy đủ trước khi render hoặc chỉnh sửa. */
export const normalizeBlockConfiguration = (value: any, fallback: BlockPosition = { lat: 0, lng: 0 }): BlockConfiguration => {
  const displayOptions = normalizeDisplayOptions(value);
  if (!Array.isArray(value?.blocks)) return { ...createEmptyBlockConfiguration(), ...displayOptions };

  return {
    ...displayOptions,
    blocks: value.blocks.map((block: any, blockIndex: number) => ({
      id: block?.id || createId('block'),
      name: block?.name || `Block ${blockIndex + 1}`,
      anchorDeviceId: block?.anchorDeviceId || undefined,
      actualPosition: toPosition(block?.actualPosition, fallback),
      displayPosition: toPosition(block?.displayPosition, fallback),
      anchorColor: toOptionalColor(block?.anchorColor),
      anchorRadius: toOptionalPositiveNumber(block?.anchorRadius),
      lineColor: toOptionalColor(block?.lineColor),
      lineWidth: toOptionalPositiveNumber(block?.lineWidth),
      lineStyle: toLineStyle(block?.lineStyle),
      waypoints: toWaypoints(block?.waypoints),
      levels: Array.isArray(block?.levels)
        ? block.levels.map((level: any, levelIndex: number) => ({
            id: level?.id || createId('level'),
            name: level?.name || `Tầng ${levelIndex + 1}`,
            // Chỉ ghi khi bật để config cũ / file xuất không phình thêm field
            ...(level?.layout === 'route' ? { layout: 'route' as const, ...(level?.routeReverse === true ? { routeReverse: true } : {}), ...toRouteLevelExtras(level) } : {}),
            devices: Array.isArray(level?.devices)
              ? level.devices
                  .filter((device: any) => device?.deviceId || device?.id)
                  .map((device: any) => ({
                    id: device?.id || createId('device'),
                    deviceId: String(device?.deviceId || device?.id),
                    // Chỉ ghi khi bật để config cũ / file xuất không phình thêm field
                    ...(device?.flipX === true ? { flipX: true } : {}),
                  }))
              : [],
          }))
        : [],
    })),
  };
};

export const cloneBlockConfiguration = (configuration: BlockConfiguration): BlockConfiguration =>
  normalizeBlockConfiguration(JSON.parse(JSON.stringify(configuration)));
