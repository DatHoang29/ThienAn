/**
 * Bố trí thiết bị của 1 tầng (level) block theo LÝ TRÌNH (Km) — dùng cho tầng "layout = route".
 *
 * Mục đích: 1 block có thể chứa cả hầm/tuyến, thiết bị nằm ngang đúng vị trí tương đối theo Km
 * thay vì tách thành nhiều block chỉ có 1–2 thiết bị.
 *
 * - Chiều dài tuyến/hầm TỰ ĐO = Km lớn nhất − Km nhỏ nhất của thiết bị trong tầng.
 * - Độ rộng thanh = chiều dài × px/mét (có chặn min/max).
 * - Thiết bị trùng/gần vị trí (không đủ chỗ 1 hàng) được XẾP CHỒNG DỌC sang hàng kế tiếp.
 * - Thiết bị chưa có Km đặt ở các hàng cuối, xếp từ trái sang phải.
 *
 * Thuần hàm, không phụ thuộc Vue/alias để chạy được bằng `node --test`.
 */

/** Số px cho mỗi mét chiều dài tuyến (hầm 1 km ≈ 400px). */
export const ROUTE_PX_PER_METER = 0.4;
/** Trần của tỉ lệ tự giãn (px/mét): cặp thiết bị gần nhau hơn ~80m (48px) sẽ xếp chồng dọc. */
export const ROUTE_MAX_ADAPTIVE_PX_PER_METER = 0.6;
export const ROUTE_MIN_WIDTH = 160;
export const ROUTE_MAX_WIDTH = 1200;
/** Khoảng hở giữa 2 thiết bị cùng hàng (px). */
export const ROUTE_ITEM_GAP = 8;
/** Sai số số thực (px) khi so khoảng cách 2 thiết bị với khoảng cách tối thiểu. */
const ROW_EPSILON = 0.01;

export interface RouteLayoutItem {
  /** Khoá duy nhất của thiết bị trong tầng (id dòng thiết bị trong block). */
  key: string;
  /** Lý trình tính bằng mét (km*1000+met); null/undefined = chưa có Km. */
  position?: number | null;
  /** Nhóm dòng (vd id loại thiết bị) — chỉ dùng khi options.groupRows = true. */
  group?: string | null;
}

export interface RouteLayoutOptions {
  /** Độ rộng 1 thiết bị (px) để tính khoảng cách tối thiểu giữa 2 thiết bị cùng hàng. */
  itemWidth: number;
  /** Chiều cao 1 hàng thiết bị (px). */
  rowHeight: number;
  /** Đảo chiều: Km giảm dần từ trái sang phải. */
  reverse?: boolean;
  pxPerMeter?: number;
  /** false = giữ cố định pxPerMeter, không tự giãn theo cặp thiết bị gần nhau. Mặc định bật. */
  adaptive?: boolean;
  minWidth?: number;
  maxWidth?: number;
  itemGap?: number;
  /**
   * Khoảng lý trình cố định (mét) của tầng (zone hầm/tuyến, cấu hình cơ sở hoặc nhập tay).
   * Cần cả hai và fixedEnd > fixedStart; ngược lại bỏ qua và tự đo từ thiết bị.
   * Thiết bị nằm ngoài khoảng được kẹp vào mép thanh và đánh dấu outOfRange.
   */
  fixedStart?: number | null;
  fixedEnd?: number | null;
  /**
   * Lề 2 đầu thanh (px). Mặc định = itemWidth/2 + 4. Các tầng cùng 1 block truyền chung 1 giá trị
   * (theo thiết bị rộng nhất) để cùng 1 Km nằm thẳng hàng giữa các tầng.
   */
  padding?: number;
  /**
   * true = mỗi nhóm (item.group, vd loại thiết bị) nằm ở các dòng RIÊNG trong cùng 1 tầng/1 thanh:
   * nhóm xếp lần lượt theo thứ tự xuất hiện đầu tiên trong danh sách, trong mỗi nhóm vẫn xếp chồng dọc khi quá sát nhau.
   * Km vẫn thẳng hàng giữa các dòng vì dùng chung 1 thanh.
   */
  groupRows?: boolean;
}

export interface RoutePlacedItem {
  key: string;
  /** Toạ độ tâm thiết bị theo trục ngang trong thanh (px). */
  centerPx: number;
  /** Hàng (0 = trên cùng). */
  row: number;
  /** true = thiết bị chưa có Km, đặt ở hàng cuối. */
  unplaced: boolean;
  /** true = Km nằm ngoài khoảng cố định của tầng, đã kẹp vào mép thanh. */
  outOfRange?: boolean;
}

export interface RouteLayoutResult {
  width: number;
  height: number;
  rows: number;
  /** Lý trình đầu/cuối đã đo (mét); null khi không có thiết bị nào có Km. */
  startPosition: number | null;
  endPosition: number | null;
  lengthMeters: number;
  items: RoutePlacedItem[];
}

/** Lý trình (mét) của thiết bị từ KmNumber/MetNumber; chưa nhập cả hai = null. */
export const getEquipmentRoutePosition = (equipment: any): number | null => {
  const km = equipment?.kmNumber;
  const met = equipment?.metNumber;
  if ((km === null || km === undefined || km === '') && (met === null || met === undefined || met === '')) return null;
  const kmValue = Number(km ?? 0);
  const metValue = Number(met ?? 0);
  if (!Number.isFinite(kmValue) || !Number.isFinite(metValue)) return null;
  return kmValue * 1000 + metValue;
};

/** "Km1+250" từ lý trình tính bằng mét. */
export const formatRouteKm = (position: number): string => `Km${Math.floor(position / 1000)}+${Math.round(position % 1000)}`;

const isFinitePosition = (value: unknown): value is number => typeof value === 'number' && Number.isFinite(value);

export const computeRouteLayout = (inputItems: RouteLayoutItem[], options: RouteLayoutOptions): RouteLayoutResult => {
  const pxPerMeter = options.pxPerMeter ?? ROUTE_PX_PER_METER;
  const minWidth = options.minWidth ?? ROUTE_MIN_WIDTH;
  const maxWidth = Math.max(minWidth, options.maxWidth ?? ROUTE_MAX_WIDTH);
  const pitch = options.itemWidth + (options.itemGap ?? ROUTE_ITEM_GAP);
  const pad = options.padding ?? options.itemWidth / 2 + 4;

  const placedInput = inputItems.filter((item) => isFinitePosition(item.position));
  const unplacedInput = inputItems.filter((item) => !isFinitePosition(item.position));

  const hasFixedRange = isFinitePosition(options.fixedStart) && isFinitePosition(options.fixedEnd) && options.fixedEnd > options.fixedStart;
  const rawPositions = placedInput.map((item) => item.position as number);
  // Khoảng cố định: kẹp thiết bị ngoài khoảng vào mép để không làm thanh phình/lệch.
  const positions = hasFixedRange
    ? rawPositions.map((position) => Math.min(options.fixedEnd as number, Math.max(options.fixedStart as number, position)))
    : rawPositions;
  const start = hasFixedRange ? (options.fixedStart as number) : positions.length ? Math.min(...positions) : null;
  const end = hasFixedRange ? (options.fixedEnd as number) : positions.length ? Math.max(...positions) : null;
  const lengthMeters = start !== null && end !== null ? end - start : 0;

  // Tự giãn tỉ lệ để 2 thiết bị gần nhau nhất vẫn đủ chỗ trên cùng 1 hàng (vd cặp cách 100m cần ≥0.48 px/m),
  // nhưng không vượt ROUTE_MAX_ADAPTIVE_PX_PER_METER để 1 cặp quá sát không làm cả tuyến phình ra tối đa.
  // Cặp sát hơn mức đó (hoặc trùng Km) vẫn xếp chồng dọc.
  // Tách dòng theo nhóm thì chỉ cặp CÙNG nhóm mới tranh chỗ trên 1 dòng → tính khoảng cách gần nhất trong từng nhóm.
  const positionsByGroup = new Map<string, number[]>();
  placedInput.forEach((item, index) => {
    const group = options.groupRows ? String(item.group ?? '') : '';
    if (!positionsByGroup.has(group)) positionsByGroup.set(group, []);
    positionsByGroup.get(group)!.push(positions[index]);
  });
  let minGapMeters = Infinity;
  positionsByGroup.forEach((groupPositions) => {
    const distinct = [...new Set(groupPositions)].sort((a, b) => a - b);
    for (let index = 1; index < distinct.length; index++) minGapMeters = Math.min(minGapMeters, distinct[index] - distinct[index - 1]);
  });
  const effectivePxPerMeter =
    options.adaptive === false || !Number.isFinite(minGapMeters)
      ? pxPerMeter
      : Math.max(pxPerMeter, Math.min(pitch / minGapMeters, ROUTE_MAX_ADAPTIVE_PX_PER_METER));

  // ceil (không round) để khoảng cách thực không tụt xuống dưới mức tối thiểu do làm tròn độ rộng
  const width = Math.min(maxWidth, Math.max(minWidth, Math.ceil(lengthMeters * effectivePxPerMeter + pad * 2)));
  const usable = Math.max(0, width - pad * 2);

  // Tâm thiết bị theo Km; chiều dài 0 (1 thiết bị / trùng Km) thì đặt giữa thanh.
  const withCenter = placedInput.map((item, index) => {
    const position = positions[index];
    const ratio = lengthMeters > 0 ? (position - (start as number)) / lengthMeters : 0.5;
    const directed = options.reverse ? 1 - ratio : ratio;
    const group = options.groupRows ? String(item.group ?? '') : '';
    return { key: item.key, centerPx: pad + directed * usable, order: index, group, outOfRange: hasFixedRange && position !== rawPositions[index] };
  });
  withCenter.sort((a, b) => a.centerPx - b.centerPx || a.order - b.order);

  // Xếp hàng tham lam theo từng nhóm: hàng đầu tiên còn đủ chỗ; hết chỗ thì xếp chồng dọc xuống hàng mới (không giới hạn số hàng).
  // Không bật groupRows thì chỉ có 1 nhóm '' = cách xếp cũ. Nhóm sau bắt đầu ngay dưới các dòng của nhóm trước.
  const groupOrder = [...new Set(withCenter.slice().sort((a, b) => a.order - b.order).map((item) => item.group))];
  const items: RoutePlacedItem[] = [];
  let rowOffset = 0;
  groupOrder.forEach((group) => {
    const rowLast: number[] = [];
    withCenter
      .filter((item) => item.group === group)
      .forEach((item) => {
        // Cho phép sai số nhỏ: khoảng cách đúng bằng pitch có thể ra 47.99999… do số thực và không được đẩy xuống hàng dưới.
        let row = rowLast.findIndex((last) => item.centerPx - last >= pitch - ROW_EPSILON);
        if (row === -1) row = rowLast.length;
        rowLast[row] = item.centerPx;
        items.push({ key: item.key, centerPx: item.centerPx, row: rowOffset + row, unplaced: false, ...(item.outOfRange ? { outOfRange: true } : {}) });
      });
    rowOffset += rowLast.length;
  });

  // Thiết bị chưa có Km: các hàng cuối, trái → phải, tràn hàng khi hết độ rộng.
  let unplacedRow = rowOffset;
  let cursor = pad;
  unplacedInput.forEach((item) => {
    if (cursor > pad && cursor > width - pad) {
      unplacedRow += 1;
      cursor = pad;
    }
    items.push({ key: item.key, centerPx: cursor, row: unplacedRow, unplaced: true });
    cursor += pitch;
  });

  const rows = items.length ? Math.max(...items.map((item) => item.row)) + 1 : 0;
  return {
    width,
    height: rows * options.rowHeight,
    rows,
    startPosition: start,
    endPosition: end,
    lengthMeters,
    items,
  };
};
