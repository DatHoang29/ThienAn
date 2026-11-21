export interface ScreenPoint {
    x: number;
    y: number;
}

// Khoảng hở giữa toàn bộ icon (kể cả padding) và nét biên zone, tính bằng pixel.
export const INCIDENT_ZONE_GAP_PX = 2;

/** Khoảng hở cấp bản đồ: nhận 0px, giá trị thiếu/không hợp lệ dùng mặc định 2px. */
export const resolveIncidentZoneGap = (value: unknown): number => {
    if (value == null || (typeof value === 'string' && value.trim() === '') || typeof value === 'boolean') return INCIDENT_ZONE_GAP_PX;
    const gap = Number(value);
    return Number.isFinite(gap) && gap >= 0 ? gap : INCIDENT_ZONE_GAP_PX;
};

const closestPoint = (point: ScreenPoint, a: ScreenPoint, b: ScreenPoint): ScreenPoint => {
    const dx = b.x - a.x;
    const dy = b.y - a.y;
    const lengthSquared = dx * dx + dy * dy;
    const t = lengthSquared ? Math.max(0, Math.min(1, ((point.x - a.x) * dx + (point.y - a.y) * dy) / lengthSquared)) : 0;
    return { x: a.x + t * dx, y: a.y + t * dy };
};

const overlapsRing = (point: ScreenPoint, ring: ScreenPoint[], clearance: number): boolean => {
    let inside = false;
    for (let i = 0, j = ring.length - 1; i < ring.length; j = i++) {
        const a = ring[j];
        const b = ring[i];
        if ((a.y > point.y) !== (b.y > point.y) && point.x < ((b.x - a.x) * (point.y - a.y)) / (b.y - a.y) + a.x) {
            inside = !inside;
        }
        const foot = closestPoint(point, a, b);
        if (Math.hypot(point.x - foot.x, point.y - foot.y) < clearance - 0.001) return true;
    }
    return inside;
};

/** Mép chung / chồng mép nhẹ giữa 2 zone (≤ ngưỡng này, pixel) không tính là "đi xuyên" zone. */
const CROSSING_TOLERANCE_PX = 2;
/** Khoảng lấy mẫu dọc đường nối khi kiểm tra đi xuyên zone khác (pixel). */
const CROSSING_SAMPLE_STEP_PX = 3;
const CROSSING_MAX_SAMPLES = 48;

interface RingBox {
    minX: number;
    minY: number;
    maxX: number;
    maxY: number;
}

const ringBox = (ring: ScreenPoint[]): RingBox => ({
    minX: Math.min(...ring.map((p) => p.x)),
    minY: Math.min(...ring.map((p) => p.y)),
    maxX: Math.max(...ring.map((p) => p.x)),
    maxY: Math.max(...ring.map((p) => p.y)),
});

/** Điểm nằm hẳn trong zone: trong polygon và cách mọi cạnh xa hơn ngưỡng (không tính mép chung). */
const isDeepInside = (point: ScreenPoint, ring: ScreenPoint[]): boolean => {
    let inside = false;
    for (let i = 0, j = ring.length - 1; i < ring.length; j = i++) {
        const a = ring[j];
        const b = ring[i];
        if ((a.y > point.y) !== (b.y > point.y) && point.x < ((b.x - a.x) * (point.y - a.y)) / (b.y - a.y) + a.x) inside = !inside;
    }
    if (!inside) return false;
    for (let i = 0, j = ring.length - 1; i < ring.length; j = i++) {
        const foot = closestPoint(point, ring[j], ring[i]);
        if (Math.hypot(point.x - foot.x, point.y - foot.y) <= CROSSING_TOLERANCE_PX) return false;
    }
    return true;
};

/**
 * Đường nối (từ điểm trong zone sự cố) → icon có đi xuyên qua lòng zone khác không, vd icon bị đẩy sang
 * bên kia làn ngược chiều. Bỏ qua đoạn còn nằm trong / sát zone sự cố (zone chồng lấn nhau ở mép).
 */
const leaderCrossesRing = (start: ScreenPoint, candidate: ScreenPoint, ring: ScreenPoint[], box: RingBox, ownRing: ScreenPoint[]): boolean => {
    if (
        Math.max(start.x, candidate.x) < box.minX ||
        Math.min(start.x, candidate.x) > box.maxX ||
        Math.max(start.y, candidate.y) < box.minY ||
        Math.min(start.y, candidate.y) > box.maxY
    ) {
        return false;
    }
    const length = Math.hypot(candidate.x - start.x, candidate.y - start.y);
    const samples = Math.min(CROSSING_MAX_SAMPLES, Math.max(1, Math.ceil(length / CROSSING_SAMPLE_STEP_PX)));
    for (let k = 1; k <= samples; k++) {
        const t = k / samples;
        const p = { x: start.x + (candidate.x - start.x) * t, y: start.y + (candidate.y - start.y) * t };
        if (isDeepInside(p, ring) && !overlapsRing(p, ownRing, CROSSING_TOLERANCE_PX)) return true;
    }
    return false;
};

/**
 * Điểm nằm hẳn trong zone sự cố, gần anchor nhất có thể. Anchor có thể nằm ngay trên mép / góc zone
 * (sự kiện đúng tại điểm nối zone) — khi đó tính "đi xuyên" từ anchor sẽ sai lệch.
 */
const findInnerPoint = (anchor: ScreenPoint, ring: ScreenPoint[], radius: number): ScreenPoint => {
    if (isDeepInside(anchor, ring)) return anchor;
    // Dò quanh anchor theo các vòng tròn lớn dần, lấy điểm "hẳn trong zone" gần nhất
    // (đúng cả với góc nhọn / đầu zone cắt xiên, nơi đi thẳng vào theo pháp tuyến có thể lọt ra ngoài).
    const maxDistance = Math.max(radius * 2, 8 * CROSSING_TOLERANCE_PX);
    for (let distance = CROSSING_TOLERANCE_PX * 1.5; distance <= maxDistance; distance *= 1.5) {
        for (let k = 0; k < 24; k++) {
            const angle = (k * Math.PI) / 12;
            const point = { x: anchor.x + Math.cos(angle) * distance, y: anchor.y + Math.sin(angle) * distance };
            if (isDeepInside(point, ring)) return point;
        }
    }
    return anchor;
};

/**
 * Chọn mép gần vị trí lý trình, dịch theo pháp tuyến ngoài và kiểm tra toàn bộ icon.
 * Hoạt động với cả hai thứ tự đỉnh, zone nghiêng và lõm.
 * Thứ tự ưu tiên: (1) đường nối không đi xuyên zone khác — tránh icon nhảy sang bên kia làn ngược
 * chiều; (2) icon ít đè zone lân cận nhất; (3) gần vị trí lý trình nhất.
 * Các điểm và clearance phải dùng cùng hệ tọa độ pixel, sau khi áp dụng zoom/xoay.
 */
export const findOutsideZonePoint = (
    anchor: ScreenPoint,
    ring: ScreenPoint[],
    clearance: number,
    neighboringRings: ScreenPoint[][] = []
): ScreenPoint => {
    const radius = Math.max(1, Number.isFinite(clearance) ? clearance : 1);
    let twiceArea = 0;
    for (let i = 0; i < ring.length; i++) {
        const a = ring[i];
        const b = ring[(i + 1) % ring.length];
        twiceArea += a.x * b.y - b.x * a.y;
    }
    const sign = twiceArea >= 0 ? 1 : -1;
    // Kiểm tra "đi xuyên" bắt đầu từ 1 điểm hẳn trong zone sự cố: anchor nằm đúng mép nối / mép giáp
    // làn ngược chiều thì zone bên cạnh vẫn phải được xét, nếu không icon sẽ nhảy sang bên kia đường.
    const crossingStart = findInnerPoint(anchor, ring, radius);
    const crossingRings = neighboringRings.filter((other) => other.length >= 3).map((other) => ({ ring: other, box: ringBox(other) }));
    let best: ScreenPoint | undefined;
    let bestCrossings = Infinity;
    let bestCollisions = Infinity;
    let bestDistance = Infinity;
    for (let i = 0; i < ring.length; i++) {
        const a = ring[i];
        const b = ring[(i + 1) % ring.length];
        const dx = b.x - a.x;
        const dy = b.y - a.y;
        const length = Math.hypot(dx, dy);
        if (length < 0.001) continue;
        const normal = { x: (sign * dy) / length, y: (-sign * dx) / length };
        const foot = closestPoint(anchor, a, b);
        let candidate = { x: foot.x + normal.x * radius, y: foot.y + normal.y * radius };
        // Một pháp tuyến tại góc lõm có thể gặp phần khác của chính zone.
        for (let step = 0; step < 12 && overlapsRing(candidate, ring, radius); step++) {
            candidate = { x: candidate.x + normal.x * radius, y: candidate.y + normal.y * radius };
        }
        if (overlapsRing(candidate, ring, radius)) {
            // Mặt phẳng đỡ ngoài cùng bảo đảm khoảng hở ngay cả với polygon lõm sâu.
            const support = Math.max(...ring.map((p) => p.x * normal.x + p.y * normal.y));
            const shift = support + radius + 0.01 - (candidate.x * normal.x + candidate.y * normal.y);
            candidate = { x: candidate.x + normal.x * shift, y: candidate.y + normal.y * shift };
        }
        // Tâm icon nằm trong / ngay trên mép zone khác cũng tính như đi xuyên zone đó.
        const crossings = crossingRings.filter(
            ({ ring: other, box }) =>
                (candidate.x >= box.minX - CROSSING_TOLERANCE_PX &&
                    candidate.x <= box.maxX + CROSSING_TOLERANCE_PX &&
                    candidate.y >= box.minY - CROSSING_TOLERANCE_PX &&
                    candidate.y <= box.maxY + CROSSING_TOLERANCE_PX &&
                    overlapsRing(candidate, other, CROSSING_TOLERANCE_PX)) ||
                leaderCrossesRing(crossingStart, candidate, other, box, ring)
        ).length;
        if (crossings > bestCrossings) continue;
        const collisions = neighboringRings.filter((other) => overlapsRing(candidate, other, radius)).length;
        const distance = Math.hypot(candidate.x - anchor.x, candidate.y - anchor.y);
        if (
            crossings < bestCrossings ||
            collisions < bestCollisions ||
            (collisions === bestCollisions && distance < bestDistance)
        ) {
            best = candidate;
            bestCrossings = crossings;
            bestCollisions = collisions;
            bestDistance = distance;
        }
    }
    // Dữ liệu suy biến không có cạnh: đặt ngoài toàn bộ extent thay vì trả tâm zone.
    return best || { x: anchor.x, y: Math.min(anchor.y, ...ring.map((p) => p.y)) - radius };
};

/** Kiểu nét của đường nối icon sự kiện → vị trí sự kiện trong zone. */
export type IncidentLineStyle = 'solid' | 'dashed' | 'dotted';

/** Mặc định nét liền mạch; map cũ chưa cấu hình cũng dùng giá trị này. */
export const INCIDENT_LINE_DEFAULT_STYLE: IncidentLineStyle = 'solid';

/** dashArray cho L.polyline; `undefined` = nét liền. */
export const INCIDENT_LINE_DASH_ARRAY: Record<IncidentLineStyle, string | undefined> = {
    solid: undefined,
    dashed: '6, 4',
    dotted: '1, 4',
};

export const resolveIncidentLineStyle = (value: unknown): IncidentLineStyle =>
    value === 'dashed' || value === 'dotted' || value === 'solid' ? value : INCIDENT_LINE_DEFAULT_STYLE;

export interface IncidentLayoutItem {
    /** Vị trí sự kiện thật (trong zone). */
    anchor: ScreenPoint;
    /** Biên zone chứa sự kiện. */
    ring: ScreenPoint[];
    /** Bán kính bao icon (đã gồm khoảng hở). */
    radius: number;
    /** Biên các zone khác — tránh đặt icon đè lên. */
    neighboringRings?: ScreenPoint[][];
}

/**
 * Chỉ gom thành chồng khi vị trí gốc của 2 icon cách nhau không quá (tỉ lệ này × bán kính),
 * tức icon đè lên nhau quá nửa — gần như trùng chỗ. Xa hơn thì mỗi sự cố giữ đúng vị trí của nó.
 */
export const INCIDENT_STACK_JOIN_RATIO = 1;
/**
 * Chồng tới số lớp này dùng khoảng so le đầy đủ; đông hơn thì các lớp xếp sát lại để dải
 * không dài quá (giữ chiều dài ≈ chồng 4 lớp). KHÔNG ẩn icon nào — mọi sự cố đều nhìn/bấm được.
 */
export const INCIDENT_STACK_COMPACT_AFTER = 4;
/** Khoảng so le giữa 2 lớp = 0.9 × bán kính ≈ 45% đường kính: mỗi icon lộ ra ~ nửa, đủ nhận loại sự kiện. */
export const INCIDENT_STACK_STEP_RATIO = 0.9;
/** Khoảng so le nhỏ nhất khi chồng đông: mỗi icon vẫn lộ ra ~ 1/4 để nhận diện và bấm được. */
export const INCIDENT_STACK_MIN_STEP_RATIO = 0.5;

export interface IncidentStackLayout {
    /** Vị trí hiển thị của từng icon, theo thứ tự đầu vào. */
    points: ScreenPoint[];
    /** Các nhóm (chỉ số đầu vào); thứ tự trong nhóm = thứ tự lớp, phần tử đầu nằm trên cùng. */
    groups: number[][];
    /** Lớp của từng icon trong nhóm: 0 = trên cùng. */
    layers: number[];
    /** Kích thước nhóm chứa từng icon (1 = đứng riêng). */
    groupSizes: number[];
}

const distance = (a: ScreenPoint, b: ScreenPoint) => Math.hypot(a.x - b.x, a.y - b.y);

/**
 * Vị trí của chồng = vị trí gốc của chính 1 thành viên (gần trung bình vị trí gốc nhất).
 * Vị trí gốc luôn hợp lệ (ngoài zone, sát mép, gần lý trình) nên chồng không bị đẩy đi xa.
 */
const pickClusterPoint = (members: number[], bases: ScreenPoint[]): ScreenPoint => {
    const mean = {
        x: members.reduce((sum, index) => sum + bases[index].x, 0) / members.length,
        y: members.reduce((sum, index) => sum + bases[index].y, 0) / members.length,
    };
    let best = members[0];
    members.forEach((index) => {
        if (distance(bases[index], mean) < distance(bases[best], mean) - 0.001) best = index;
    });
    return bases[best];
};

/**
 * Vị trí từng icon của 1 chồng (theo thứ tự `members`): so le dọc mép zone, căn giữa quanh vị trí chồng.
 * Icon nào nằm bên trái/phải theo đúng vị trí thật dọc mép → đường nối không bắt chéo nhau.
 * Cả dải được đẩy ra ngoài cùng 1 khoảng nếu có icon chạm zone (mép cong/góc zone).
 */
const buildStackPoints = (members: number[], items: IncidentLayoutItem[], radii: number[], bases: ScreenPoint[]): ScreenPoint[] => {
    const point = pickClusterPoint(members, bases);
    if (members.length === 1) return [point];

    const radius = Math.max(...members.map((index) => radii[index]));
    const anchorMean = {
        x: members.reduce((sum, index) => sum + items[index].anchor.x, 0) / members.length,
        y: members.reduce((sum, index) => sum + items[index].anchor.y, 0) / members.length,
    };
    const length = distance(point, anchorMean);
    const normal = length > 0.001 ? { x: (point.x - anchorMean.x) / length, y: (point.y - anchorMean.y) / length } : { x: 0, y: -1 };
    const tangent = { x: -normal.y, y: normal.x };
    const count = members.length;
    const fullStep = radius * INCIDENT_STACK_STEP_RATIO;
    // Chồng đông: thu khoảng so le để dải dài ≈ chồng 4 lớp, nhưng không nhỏ hơn mức tối thiểu.
    const step =
        count > INCIDENT_STACK_COMPACT_AFTER
            ? Math.max(radius * INCIDENT_STACK_MIN_STEP_RATIO, (fullStep * (INCIDENT_STACK_COMPACT_AFTER - 1)) / (count - 1))
            : fullStep;
    const rings = [...new Set(members.map((index) => items[index].ring))].filter((ring) => ring.length >= 3);

    // Thứ tự vị trí dọc mép theo hình chiếu vị trí thật lên tiếp tuyến (hoà thì giữ thứ tự đầu vào).
    const slotOf = new Map<number, number>();
    [...members]
        .sort((a, b) => {
            const delta = (items[a].anchor.x - items[b].anchor.x) * tangent.x + (items[a].anchor.y - items[b].anchor.y) * tangent.y;
            return Math.abs(delta) > 0.001 ? delta : a - b;
        })
        .forEach((member, slot) => slotOf.set(member, slot));

    const slotAt = (slot: number, push: number): ScreenPoint => ({
        x: point.x + tangent.x * (slot - (count - 1) / 2) * step + normal.x * push,
        y: point.y + tangent.y * (slot - (count - 1) / 2) * step + normal.y * push,
    });
    const isClear = (push: number) =>
        Array.from({ length: count }, (_, slot) => slotAt(slot, push)).every((p) => rings.every((ring) => !overlapsRing(p, ring, radius)));

    let push = 0;
    for (let attempt = 0; attempt < 24 && !isClear(push); attempt++) push += radius * 0.25;
    if (!isClear(push)) {
        // Mặt phẳng đỡ ngoài cùng theo pháp tuyến: chắc chắn nằm ngoài mọi zone của nhóm.
        const support = Math.max(...rings.flatMap((ring) => ring.map((p) => p.x * normal.x + p.y * normal.y)));
        push = Math.max(push, support + radius + 0.01 - (point.x * normal.x + point.y * normal.y));
    }

    return members.map((member) => slotAt(slotOf.get(member) ?? 0, push));
};

/**
 * Bố trí icon sự kiện. Mỗi sự kiện có vị trí gốc từ findOutsideZonePoint (ngoài zone, gần lý trình).
 *
 * Chỉ những sự kiện GẦN NHƯ TRÙNG CHỖ mới gom thành 1 chồng xếp lớp: 1 sự kiện chỉ vào chồng khi
 * vị trí gốc của nó cách MỌI thành viên không quá INCIDENT_STACK_JOIN_RATIO × bán kính (không gom
 * "dây chuyền" kéo cả sự kiện ở xa vào). Sự kiện xa hơn giữ đúng vị trí của mình — có thể chạm nhẹ
 * icon bên cạnh nhưng vẫn thấy và bấm được (rê chuột thì icon nổi lên trên).
 * Zoom vào → các icon cách xa nhau hơn → chồng tự tách. Thứ tự đầu vào quyết định lớp trên/dưới.
 */
export const layoutIncidentStacks = (items: IncidentLayoutItem[]): IncidentStackLayout => {
    const radii = items.map((item) => Math.max(1, Number.isFinite(item.radius) ? item.radius : 1));
    const bases = items.map((item, index) => findOutsideZonePoint(item.anchor, item.ring, radii[index], item.neighboringRings || []));
    const isNear = (a: number, b: number) => distance(bases[a], bases[b]) <= Math.min(radii[a], radii[b]) * INCIDENT_STACK_JOIN_RATIO + 0.001;

    const groups: number[][] = [];
    items.forEach((_, index) => {
        let target: number[] | undefined;
        let targetDistance = Infinity;
        groups.forEach((group) => {
            if (!group.every((member) => isNear(member, index))) return;
            const gap = Math.min(...group.map((member) => distance(bases[member], bases[index])));
            if (gap < targetDistance) {
                target = group;
                targetDistance = gap;
            }
        });
        if (target) target.push(index);
        else groups.push([index]);
    });

    const points: ScreenPoint[] = new Array(items.length);
    const layers: number[] = new Array(items.length);
    const groupSizes: number[] = new Array(items.length);
    groups.forEach((members) => {
        const stackPoints = buildStackPoints(members, items, radii, bases);
        members.forEach((member, k) => {
            points[member] = stackPoints[k];
            layers[member] = k;
            groupSizes[member] = members.length;
        });
    });

    return { points, groups: groups.map((members) => [...members]), layers, groupSizes };
};
