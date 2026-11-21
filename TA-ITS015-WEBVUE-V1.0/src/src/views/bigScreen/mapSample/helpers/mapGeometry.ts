import L from 'leaflet';
import { TmsZoneKmDirection } from '/@/types/enums';

/**
 * Các hàm tính toán hình học thuần (pure functions) phục vụ định vị icon
 * sự cố trên centerline của polygon zone. Không phụ thuộc vào state/Vue,
 * nhận đầy đủ dữ liệu qua tham số nên dễ test và tái sử dụng.
 */

/**
 * Chiếu một điểm lên polyline để tìm vị trí tỉ lệ t trong [0, 1].
 */
export const projectPointToPath = (point: L.LatLng, path: L.LatLng[]): number => {
    if (path.length < 2) return 0.5;

    let minD = Infinity;
    let bestT = 0.5;

    let totalLength = 0;
    const segmentLengths: number[] = [];
    for (let i = 0; i < path.length - 1; i++) {
        const d = path[i].distanceTo(path[i + 1]);
        segmentLengths.push(d);
        totalLength += d;
    }

    if (totalLength === 0) return 0.5;

    let accumulated = 0;
    for (let i = 0; i < path.length - 1; i++) {
        const p1 = path[i];
        const p2 = path[i + 1];
        const segLen = segmentLengths[i];

        let tSeg = 0;
        if (segLen > 0) {
            const dx = p2.lng - p1.lng;
            const dy = p2.lat - p1.lat;
            const px = point.lng - p1.lng;
            const py = point.lat - p1.lat;
            const dot = px * dx + py * dy;
            const lenSq = dx * dx + dy * dy;
            tSeg = Math.max(0, Math.min(1, dot / lenSq));
        }

        const projPoint = L.latLng(p1.lat + (p2.lat - p1.lat) * tSeg, p1.lng + (p2.lng - p1.lng) * tSeg);

        const d = point.distanceTo(projPoint);
        if (d < minD) {
            minD = d;
            bestT = (accumulated + tSeg * segLen) / totalLength;
        }
        accumulated += segLen;
    }

    return bestT;
};

/**
 * Trích xuất centerline (tim đường) của một polygon dạng hành lang (corridor).
 */
export const getPolygonCenterline = (polygon: L.Polygon): L.LatLng[] => {
    let latLngs = polygon.getLatLngs();
    if (Array.isArray(latLngs[0])) {
        latLngs = latLngs[0] as L.LatLng[];
    } else {
        latLngs = latLngs as L.LatLng[];
    }

    if (!latLngs || latLngs.length < 3) return [];

    const points = [...latLngs];
    if (
        points.length > 1 &&
        points[0].lat === points[points.length - 1].lat &&
        points[0].lng === points[points.length - 1].lng
    ) {
        points.pop();
    }

    const N = points.length;
    if (N < 3) return points;

    // Tìm 2 điểm xa nhau nhất (2 đầu của đoạn đường)
    let maxDist = 0;
    let ext1 = 0;
    let ext2 = 0;
    for (let i = 0; i < N; i++) {
        for (let j = i + 1; j < N; j++) {
            const d = points[i].distanceTo(points[j]);
            if (d > maxDist) {
                maxDist = d;
                ext1 = i;
                ext2 = j;
            }
        }
    }

    if (ext1 > ext2) {
        const temp = ext1;
        ext1 = ext2;
        ext2 = temp;
    }

    // Chia thành 2 chain giữa ext1 và ext2
    const chainA = points.slice(ext1, ext2 + 1);
    const chainB = [...points.slice(ext2, N), ...points.slice(0, ext1 + 1)].reverse();

    const numSamples = 30; // tăng để đường cong mượt hơn
    const centerline: L.LatLng[] = [];

    const getPointAlongChain = (chain: L.LatLng[], t: number): L.LatLng => {
        if (chain.length === 0) return L.latLng(0, 0);
        if (chain.length === 1) return chain[0];

        let totalLen = 0;
        const lens: number[] = [];
        for (let i = 0; i < chain.length - 1; i++) {
            const d = chain[i].distanceTo(chain[i + 1]);
            lens.push(d);
            totalLen += d;
        }

        if (totalLen === 0) return chain[0];

        const targetDist = t * totalLen;
        let accDist = 0;
        for (let i = 0; i < chain.length - 1; i++) {
            const nextDist = accDist + lens[i];
            if (nextDist >= targetDist) {
                const segT = (targetDist - accDist) / lens[i];
                const p1 = chain[i];
                const p2 = chain[i + 1];
                return L.latLng(p1.lat + (p2.lat - p1.lat) * segT, p1.lng + (p2.lng - p1.lng) * segT);
            }
            accDist = nextDist;
        }
        return chain[chain.length - 1];
    };

    for (let i = 0; i <= numSamples; i++) {
        const t = i / numSamples;
        const ptA = getPointAlongChain(chainA, t);
        const ptB = getPointAlongChain(chainB, t);
        centerline.push(L.latLng((ptA.lat + ptB.lat) / 2, (ptA.lng + ptB.lng) / 2));
    }

    // Căn giữa 2 ĐẦU đường tim:
    // Hai điểm đầu/cuối (t=0 và t=1) hiện trùng đúng đỉnh góc xa nhất (ext1/ext2) của
    // polygon nên bị "dính" hẳn về 1 mép -> đường tim (và đường phân cách vẽ theo nó)
    // nhìn lệch sang 1 bên ngay tại đầu tuyến. Thay 2 điểm này bằng cách NGOẠI SUY từ
    // các điểm mẫu bên trong (vốn đã nằm chính giữa lòng đường) để đường tim chạy đúng
    // TRUNG TÂM từ đầu đến cuối, không bị lệch tại 2 đầu mút.
    if (centerline.length >= 3) {
        const c1 = centerline[1];
        const c2 = centerline[2];
        centerline[0] = L.latLng(c1.lat + (c1.lat - c2.lat), c1.lng + (c1.lng - c2.lng));

        const eLast = centerline[centerline.length - 2];
        const ePrev = centerline[centerline.length - 3];
        centerline[centerline.length - 1] = L.latLng(
            eLast.lat + (eLast.lat - ePrev.lat),
            eLast.lng + (eLast.lng - ePrev.lng)
        );
    }

    return centerline;
};

/**
 * Tạo sub-polygon (miếng cắt) từ polygon gốc tại [tStart, tEnd].
 * Kết quả nằm TRONG polygon gốc và theo đúng hình dạng của nó.
 */
export const getPolygonSlice = (polygon: L.Polygon, tStart: number, tEnd: number): L.LatLng[] => {
    const rawLatLngs = polygon.getLatLngs();
    const latLngs = (Array.isArray(rawLatLngs[0]) ? rawLatLngs[0] : rawLatLngs) as L.LatLng[];
    if (!latLngs || latLngs.length < 3) return [];

    const points = [...latLngs];
    if (
        points.length > 1 &&
        points[0].lat === points[points.length - 1].lat &&
        points[0].lng === points[points.length - 1].lng
    ) {
        points.pop();
    }
    const N = points.length;
    if (N < 3) return [];

    // Tìm 2 đỉnh xa nhau nhất — giống getPolygonCenterline
    let maxDist = 0,
        ext1 = 0,
        ext2 = 0;
    for (let i = 0; i < N; i++) {
        for (let j = i + 1; j < N; j++) {
            const d = points[i].distanceTo(points[j]);
            if (d > maxDist) {
                maxDist = d;
                ext1 = i;
                ext2 = j;
            }
        }
    }
    if (ext1 > ext2) {
        const tmp = ext1;
        ext1 = ext2;
        ext2 = tmp;
    }

    // Hai cạnh viền của dải đường (left side & right side)
    const chainA = points.slice(ext1, ext2 + 1);
    const chainB = [...points.slice(ext2, N), ...points.slice(0, ext1 + 1)].reverse();

    const getPointOnChain = (chain: L.LatLng[], t: number): L.LatLng => {
        if (chain.length === 0) return L.latLng(0, 0);
        if (chain.length === 1) return chain[0];
        let totalLen = 0;
        const lens: number[] = [];
        for (let i = 0; i < chain.length - 1; i++) {
            const d = chain[i].distanceTo(chain[i + 1]);
            lens.push(d);
            totalLen += d;
        }
        if (totalLen === 0) return chain[0];
        const target = t * totalLen;
        let acc = 0;
        for (let i = 0; i < chain.length - 1; i++) {
            const next = acc + lens[i];
            if (next >= target) {
                const st = (target - acc) / lens[i];
                return L.latLng(
                    chain[i].lat + (chain[i + 1].lat - chain[i].lat) * st,
                    chain[i].lng + (chain[i + 1].lng - chain[i].lng) * st
                );
            }
            acc = next;
        }
        return chain[chain.length - 1];
    };

    // Lấy các điểm dọc theo cạnh A và cạnh B từ tStart → tEnd
    const steps = 12;
    const sideA: L.LatLng[] = [];
    const sideB: L.LatLng[] = [];
    for (let i = 0; i <= steps; i++) {
        const t = tStart + (tEnd - tStart) * (i / steps);
        sideA.push(getPointOnChain(chainA, t));
        sideB.push(getPointOnChain(chainB, t));
    }
    // Polygon khép kín: sideA xuôi + sideB ngược
    return [...sideA, ...sideB.reverse()];
};

/**
 * Nội suy một điểm tại tỉ lệ t dọc theo path/polyline.
 */
export const getPointAlongPath = (path: L.LatLng[], t: number): L.LatLng => {
    if (path.length === 0) return L.latLng(0, 0);
    if (path.length === 1) return path[0];

    let totalLength = 0;
    const segmentLengths: number[] = [];
    for (let i = 0; i < path.length - 1; i++) {
        const d = path[i].distanceTo(path[i + 1]);
        segmentLengths.push(d);
        totalLength += d;
    }

    if (totalLength === 0) return path[0];

    const targetDistance = t * totalLength;
    let accumulatedDistance = 0;

    for (let i = 0; i < path.length - 1; i++) {
        const nextDist = accumulatedDistance + segmentLengths[i];
        if (nextDist >= targetDistance) {
            const segmentT = (targetDistance - accumulatedDistance) / segmentLengths[i];
            const p1 = path[i];
            const p2 = path[i + 1];
            return L.latLng(p1.lat + (p2.lat - p1.lat) * segmentT, p1.lng + (p2.lng - p1.lng) * segmentT);
        }
        accumulatedDistance = nextDist;
    }

    return path[path.length - 1];
};

/**
 * Xác định hướng centerline so với Km dựa trên vị trí thực tế của tất cả zone cùng laneId.
 *
 * Returns: true nếu centerline[0] ứng với Km CAO (cần reverse tLinear),
 *          false nếu centerline[0] ứng với Km THẤP.
 */
export const determineCenterlineKmAlignment = (
    centerline: L.LatLng[],
    zone: any,
    allZones: any[],
    geoJsonFeatures: any[]
): boolean => {
    if (centerline.length < 2 || !zone) return false;

    const laneId = String(zone.laneId);
    const cl0 = centerline[0];
    const clEnd = centerline[centerline.length - 1];

    // Thu thập tâm polygon + Km trung bình cho tất cả zone cùng laneId
    const zonePoints: { avgKm: number; center: L.LatLng }[] = [];
    for (const z of allZones) {
        if (String(z.laneId) !== laneId) continue;
        const feat = geoJsonFeatures.find((f: any) => f.properties?.id === z.id && f.geometry?.type === 'Polygon');
        if (!feat) continue;
        const coords = feat.geometry.coordinates[0];
        let sumLat = 0,
            sumLng = 0,
            count = 0;
        for (const [lng, lat] of coords) {
            sumLat += lat;
            sumLng += lng;
            count++;
        }
        if (count === 0) continue;
        const avgKm = ((z.fromKmNumber ?? 0) + (z.toKmNumber ?? 0)) / 2;
        zonePoints.push({ avgKm, center: L.latLng(sumLat / count, sumLng / count) });
    }

    // Nếu có đủ zone cùng laneId (>=2), dùng vector Km-thấp → Km-cao
    if (zonePoints.length >= 2) {
        zonePoints.sort((a, b) => a.avgKm - b.avgKm);
        const third = Math.max(1, Math.floor(zonePoints.length / 3));
        const lowGroup = zonePoints.slice(0, third);
        const highGroup = zonePoints.slice(-third);
        const lowCenter = L.latLng(
            lowGroup.reduce((s, p) => s + p.center.lat, 0) / lowGroup.length,
            lowGroup.reduce((s, p) => s + p.center.lng, 0) / lowGroup.length
        );
        const highCenter = L.latLng(
            highGroup.reduce((s, p) => s + p.center.lat, 0) / highGroup.length,
            highGroup.reduce((s, p) => s + p.center.lng, 0) / highGroup.length
        );

        // Vector Km thấp → Km cao
        const dx = highCenter.lng - lowCenter.lng;
        const dy = highCenter.lat - lowCenter.lat;

        // Chiếu cl0 và clEnd lên vector này
        const projCl0 = (cl0.lng - lowCenter.lng) * dx + (cl0.lat - lowCenter.lat) * dy;
        const projClEnd = (clEnd.lng - lowCenter.lng) * dx + (clEnd.lat - lowCenter.lat) * dy;

        // Nếu cl0 gần Km cao hơn → centerline[0] = Km cao → cần reverse tLinear
        return projCl0 > projClEnd;
    }

    // Fallback: chỉ 1 zone cùng laneId - không đủ thông tin → không reverse
    return false;
};

/**
 * Lấy centerline đã được ĐỊNH HƯỚNG theo chiều from → to:
 * phần tử [0] = đầu "from" (fromKm/met), phần tử cuối = đầu "to" (toKm/met).
 *
 * Hướng lấy từ trường cấu hình kmDirection lưu trong properties polygon:
 * - Forward: centerline[0] = from  → giữ nguyên
 * - Reverse: centerline[0] = to    → đảo ngược để [0] = from
 * - Auto: tự suy ra bằng determineCenterlineKmAlignment() (định hướng Km thấp→cao),
 *   sau đó ánh xạ sang from→to dựa trên so sánh fromKm/met và toKm/met.
 *
 * Dùng để vẽ mũi tên minh hoạ hướng VÀ định vị icon sự cố theo đúng from/to.
 */
export const getDirectedCenterline = (
    polygon: L.Polygon,
    kmDirection: string | null | undefined,
    zone: any,
    allZones: any[] = [],
    geoJsonFeatures: any[] = []
): L.LatLng[] => {
    const centerline = getPolygonCenterline(polygon);
    if (centerline.length < 2) return centerline;

    // Cấu hình tường minh → ưu tiên tuyệt đối
    if (kmDirection === TmsZoneKmDirection.Forward) return centerline;
    if (kmDirection === TmsZoneKmDirection.Reverse) return [...centerline].reverse();

    // Auto: định hướng centerline theo Km thấp → cao trước
    const centerlineStartsHigh = determineCenterlineKmAlignment(centerline, zone, allZones, geoJsonFeatures);
    const kmAscending = centerlineStartsHigh ? [...centerline].reverse() : centerline; // [0] = Km thấp

    // Ánh xạ Km thấp→cao sang from→to: nếu fromKm/met <= toKm/met thì from nằm ở đầu Km thấp
    const fromPos = (zone?.fromKmNumber ?? 0) * 1000 + (zone?.fromMetNumber ?? zone?.metNumber ?? 0);
    const toPos = (zone?.toKmNumber ?? 0) * 1000 + (zone?.toMetNumber ?? 0);

    return fromPos <= toPos ? kmAscending : [...kmAscending].reverse();
};

/**
 * Lấy centerline định hướng theo CHIỀU DI CHUYỂN CỦA TUYẾN dựa trên laneId của zone
 * (KHÁC với getDirectedCenterline vốn dựa trên kmDirection để định vị sự cố).
 *
 * Ý nghĩa: dùng cho hiệu ứng "dòng chảy" (flow-line) — chiều chảy đại diện cho chiều
 * di chuyển của làn. Hướng được xác định thuần theo hình học các zone CÙNG laneId
 * (determineCenterlineKmAlignment) theo chiều Km TĂNG DẦN của làn, độc lập hoàn toàn
 * với kmDirection (kmDirection chỉ phục vụ định vị vị trí sự kiện/sự cố).
 *
 * - [0] = đầu Km thấp của làn, [end] = đầu Km cao của làn.
 * - Vì mỗi làn (laneId) tự xác định vector Km thấp→cao từ tập zone của chính nó, các zone
 *   cùng làn sẽ chảy đồng nhất một chiều liền mạch.
 */
export const getLaneFlowCenterline = (
    polygon: L.Polygon,
    zone: any,
    allZones: any[] = [],
    geoJsonFeatures: any[] = [],
    flowDirection: string | null | undefined = TmsZoneKmDirection.Auto
): L.LatLng[] => {
    const centerline = getPolygonCenterline(polygon);
    if (centerline.length < 2) return centerline;

    // BASE = chiều Km TĂNG DẦN của làn (gom zone cùng laneId). Nếu không có laneId thì
    // giữ nguyên thứ tự hình học của centerline làm base.
    let base = centerline;
    if (zone && zone.laneId != null) {
        const startsHigh = determineCenterlineKmAlignment(centerline, zone, allZones, geoJsonFeatures);
        // startsHigh = true → [0] đang ở đầu Km cao → đảo lại để [0] = Km thấp.
        base = startsHigh ? [...centerline].reverse() : centerline;
    }

    // Ghi đè TƯỜNG MINH theo cấu hình flowDirection của zone (thiết lập trong edit mode):
    // - Reverse: đảo chiều base → làn chạy ngược lại (vd lane 2 ngược lane 1).
    // - Forward / Auto: giữ base (chiều Km tăng dần của làn).
    if (flowDirection === TmsZoneKmDirection.Reverse) return [...base].reverse();
    return base;
};

/**
 * Dựng 2 đoạn thẳng tạo thành đầu mũi tên (chevron) tại điểm `to`,
 * hướng đi từ `from` → `to`. Tính trong không gian lat/lng (map dùng
 * CRS.Simple nên lat/lng tỉ lệ tuyến tính → mũi tên đúng hình học và
 * tự xoay theo bản đồ khi dùng leaflet-rotate).
 *
 * @param sizeRatio kích thước cạnh mũi tên so với khoảng cách from→to
 */
export const buildArrowHead = (from: L.LatLng, to: L.LatLng, sizeRatio = 0.6): L.LatLng[][] => {
    const dLat = to.lat - from.lat;
    const dLng = to.lng - from.lng;
    const len = Math.sqrt(dLat * dLat + dLng * dLng);
    if (len === 0) return [];

    const ux = dLng / len; // thành phần theo lng
    const uy = dLat / len; // thành phần theo lat
    const size = len * sizeRatio;

    const rotate = (vx: number, vy: number, angleRad: number): [number, number] => [
        vx * Math.cos(angleRad) - vy * Math.sin(angleRad),
        vx * Math.sin(angleRad) + vy * Math.cos(angleRad),
    ];

    const ang = (150 * Math.PI) / 180; // 2 cánh mũi tên lệch ±150°
    const [lx, ly] = rotate(ux, uy, ang);
    const [rx, ry] = rotate(ux, uy, -ang);

    const pLeft = L.latLng(to.lat + ly * size, to.lng + lx * size);
    const pRight = L.latLng(to.lat + ry * size, to.lng + rx * size);

    return [
        [pLeft, to],
        [to, pRight],
    ];
};

/**
 * Dựng ĐA GIÁC đầu mũi tên dạng "cánh diều" (arrow head có khấc lõm phía sau)
 * tại điểm `to`, hướng đi từ `from` → `to`. Trả về danh sách 4 điểm tạo thành
 * 1 polygon kín (tip → cánh trái → khấc lõm → cánh phải), nhìn mềm mại và rõ
 * hướng hơn kiểu chevron 2 nét.
 *
 * Tính trong không gian lat/lng (map dùng CRS.Simple nên lat/lng tỉ lệ tuyến
 * tính → mũi tên đúng hình học và tự xoay theo bản đồ khi dùng leaflet-rotate).
 *
 * @param lengthRatio chiều dài mũi tên so với khoảng cách from→to (dọc hướng)
 * @param widthRatio  bề rộng nửa cánh so với khoảng cách from→to (ngang hướng)
 * @param notchRatio  độ lõm khấc phía sau so với chiều dài mũi tên (0 = tam giác đặc)
 */
export const buildArrowHeadPolygon = (from: L.LatLng, to: L.LatLng, lengthRatio = 0.38, widthRatio = 0.17, notchRatio = 0.32): L.LatLng[] => {
    const dLat = to.lat - from.lat;
    const dLng = to.lng - from.lng;
    const len = Math.sqrt(dLat * dLat + dLng * dLng);
    if (len === 0) return [];

    // Vector đơn vị theo hướng from→to và vector pháp tuyến (vuông góc).
    const ux = dLng / len; // theo lng
    const uy = dLat / len; // theo lat
    const px = -uy; // pháp tuyến theo lng
    const py = ux; // pháp tuyến theo lat

    const headLen = len * lengthRatio; // chiều dài mũi tên dọc hướng
    const halfWidth = len * widthRatio; // nửa bề rộng cánh
    const notchLen = headLen * notchRatio; // độ lõm khấc phía sau

    // Tâm đáy (lùi headLen từ đỉnh) và điểm khấc lõm (lùi ít hơn tạo hình cánh diều)
    const baseLat = to.lat - uy * headLen;
    const baseLng = to.lng - ux * headLen;
    const notchLat = to.lat - uy * (headLen - notchLen);
    const notchLng = to.lng - ux * (headLen - notchLen);

    const tip = to;
    const leftWing = L.latLng(baseLat + py * halfWidth, baseLng + px * halfWidth);
    const notch = L.latLng(notchLat, notchLng);
    const rightWing = L.latLng(baseLat - py * halfWidth, baseLng - px * halfWidth);

    return [tip, leftWing, notch, rightWing];
};

/**
 * Tính vị trí icon incident trên centerline của zone polygon.
 *
 * Dùng getDirectedCenterline() để lấy centerline đã định hướng from → to
 * (theo trường cấu hình kmDirection). Sau đó đặt icon theo TỈ LỆ VỊ TRÍ
 * của incident giữa fromKm/met (0) và toKm/met (1) → icon nằm đúng phía
 * from/to mà người dùng cấu hình.
 */
export const getIncidentMarkerLatLngAlongZone = (
    polygon: L.Polygon,
    // Giữ tham số cũ để không đổi chữ ký; KHÔNG còn dùng để dịch vị trí (xem ghi chú bên dưới).
    _index: number,
    _total: number,
    incidentItem: any = null,
    zone: any = null,
    geoJsonFeatures: any[] = [],
    allZones: any[] = [],
    kmDirection: string | null | undefined = TmsZoneKmDirection.Auto
): L.LatLng => {
    // Centerline định hướng from → to: [0] = from, [end] = to
    const directed = getDirectedCenterline(polygon, kmDirection, zone, allZones, geoJsonFeatures);
    if (directed.length < 2) {
        return polygon.getBounds().getCenter();
    }

    if (incidentItem && zone) {
        const fromKm = zone.fromKmNumber ?? 0;
        const fromMet = zone.fromMetNumber ?? zone.metNumber ?? 0;
        const toKm = zone.toKmNumber ?? 0;
        const toMet = zone.toMetNumber ?? 0;

        const incKm = incidentItem.KmNumber ?? incidentItem.kmNumber ?? 0;
        const incMet = incidentItem.MetNumber ?? incidentItem.metNumber ?? 0;

        const fromPos = fromKm * 1000 + fromMet;
        const toPos = toKm * 1000 + toMet;
        const incPos = incKm * 1000 + incMet;

        const denom = toPos - fromPos;
        if (denom !== 0) {
            // Tỉ lệ vị trí incident dọc theo from(0) → to(1); tự đúng dù from > to
            let t = (incPos - fromPos) / denom;
            t = Math.max(0, Math.min(1, t));

            // Không dịch vị trí theo thứ tự sự kiện nữa: vị trí phải đúng lý trình. Nhiều sự kiện
            // trùng/sát nhau được gom thành chồng icon xếp lớp (layoutIncidentStacks).
            return getPointAlongPath(directed, t);
        }
    }

    // Không có lý trình: đặt giữa zone; nhiều sự kiện cùng zone sẽ gom thành 1 chồng.
    return getPointAlongPath(directed, 0.5);
};

/**
 * ================================================================
 * LÀM MỊN GÓC GẤP (chỉ để HIỂN THỊ — không sửa toạ độ đã lưu)
 * Thuật toán cắt góc Chaikin: mỗi đỉnh được thay bằng 2 điểm nằm ở 1/4 hai cạnh kề,
 * lặp vài lần → khúc gấp thành đường cong mềm.
 * Đỉnh có góc rẽ LỚN (> maxTurnDeg, vd góc ~90° ở 2 đầu zone — nơi nối với zone kế tiếp)
 * được giữ nguyên để các zone liền kề vẫn khít nhau, không hở khe ở chỗ nối.
 * ================================================================
 */
type SmoothPoint = { lat: number; lng: number; fixed: boolean };

const smoothChaikin = (input: SmoothPoint[], iterations: number, maxTurnDeg: number): SmoothPoint[] => {
    let pts = input;
    const n0 = pts.length;
    if (n0 < 3) return pts;

    // Tính góc rẽ trên mặt phẳng gần đúng (lng nhân cos(vĩ độ) để không méo góc)
    const k = Math.cos(((pts.reduce((sum, p) => sum + p.lat, 0) / n0) * Math.PI) / 180) || 1;
    const turnDeg = (a: SmoothPoint, b: SmoothPoint, c: SmoothPoint): number | null => {
        const x1 = (b.lng - a.lng) * k, y1 = b.lat - a.lat;
        const x2 = (c.lng - b.lng) * k, y2 = c.lat - b.lat;
        const l1 = Math.hypot(x1, y1), l2 = Math.hypot(x2, y2);
        if (!l1 || !l2) return null;
        const cos = Math.max(-1, Math.min(1, (x1 * x2 + y1 * y2) / (l1 * l2)));
        return (Math.acos(cos) * 180) / Math.PI;
    };

    // Đánh dấu đỉnh giữ nguyên: góc rẽ quá lớn, gần như thẳng (< 1°, làm mịn vô ích), hoặc điểm trùng
    pts = pts.map((p, i) => {
        if (p.fixed) return p;
        const prev = pts[(i - 1 + n0) % n0];
        const next = pts[(i + 1) % n0];
        const turn = turnDeg(prev, p, next);
        return { ...p, fixed: turn == null || turn > maxTurnDeg || turn < 1 };
    });

    for (let iter = 0; iter < iterations; iter++) {
        const n = pts.length;
        const out: SmoothPoint[] = [];
        for (let i = 0; i < n; i++) {
            const p = pts[i];
            if (p.fixed) {
                out.push(p);
                continue;
            }
            const prev = pts[(i - 1 + n) % n];
            const next = pts[(i + 1) % n];
            out.push(
                { lat: p.lat + 0.25 * (prev.lat - p.lat), lng: p.lng + 0.25 * (prev.lng - p.lng), fixed: false },
                { lat: p.lat + 0.25 * (next.lat - p.lat), lng: p.lng + 0.25 * (next.lng - p.lng), fixed: false }
            );
        }
        pts = out;
    }
    return pts;
};

/**
 * Làm mịn polygon zone (vòng kín). Nhận/trả về mảng [lat, lng].
 * @param coords      toạ độ [lat, lng] của vòng ngoài (có thể lặp điểm đầu ở cuối)
 * @param iterations  số lần cắt góc (3 = mịn, số điểm tối đa x8)
 * @param maxTurnDeg  góc rẽ lớn hơn ngưỡng này thì giữ nguyên đỉnh
 */
export const smoothPolygonCorners = (coords: number[][], iterations = 3, maxTurnDeg = 50): number[][] => {
    if (!Array.isArray(coords) || coords.length < 4) return coords;
    const ring = coords.map(([lat, lng]) => ({ lat, lng, fixed: false }));
    const first = ring[0];
    const last = ring[ring.length - 1];
    if (first.lat === last.lat && first.lng === last.lng) ring.pop();
    if (ring.length < 4) return coords;
    return smoothChaikin(ring, iterations, maxTurnDeg).map((p) => [p.lat, p.lng]);
};

/**
 * Làm mịn đường hở (vd centerline của flow-line). 2 điểm đầu/cuối luôn giữ nguyên để
 * flow-line các zone liền kề vẫn nối tiếp nhau.
 */
export const smoothPolylineCorners = (latLngs: L.LatLng[], iterations = 3, maxTurnDeg = 50): L.LatLng[] => {
    if (!Array.isArray(latLngs) || latLngs.length < 3) return latLngs;
    const pts = latLngs.map((p, i) => ({ lat: p.lat, lng: p.lng, fixed: i === 0 || i === latLngs.length - 1 }));
    // Đường hở: đỉnh đầu/cuối đã fixed nên phép lấy "đỉnh kề" vòng quanh không ảnh hưởng kết quả
    return smoothChaikin(pts, iterations, maxTurnDeg).map((p) => L.latLng(p.lat, p.lng));
};

/**
 * ================================================================
 * ĐƯỜNG TÂM ĐỂ VẼ (đường định hướng / flow-line) — chạy CHÍNH GIỮA dải zone từ đầu đến cuối.
 * Khác getPolygonCenterline (vẫn dùng cho tính lý trình/vị trí sự cố): hàm đó lấy 2 ĐỈNH xa
 * nhau nhất làm 2 đầu — thường là 2 góc chéo nhau — nên ở mỗi đầu một bên có thêm cạnh ngắn
 * của đầu zone, bên kia không → đường tâm bị kéo lệch về 1 mép gần 2 đầu.
 * Ở đây nhận diện 2 CẠNH ĐẦU (cap) của dải = cặp cạnh có trung điểm xa nhau nhất, rồi đi từ
 * trung điểm cạnh đầu này tới trung điểm cạnh đầu kia, ghép 2 mép theo tỉ lệ độ dài.
 * ================================================================
 */
export const getCorridorDisplayCenterline = (coords: number[][], numSamples = 40): L.LatLng[] => {
    if (!Array.isArray(coords) || coords.length < 4) return [];
    const pts = coords.map(([lat, lng]) => ({ lat, lng }));
    const first = pts[0];
    const last = pts[pts.length - 1];
    if (first.lat === last.lat && first.lng === last.lng) pts.pop();
    const n = pts.length;
    if (n < 4) return [];

    // Mặt phẳng gần đúng (lng nhân cos vĩ độ) đủ chính xác cho 1 zone, nhanh hơn distanceTo
    const k = Math.cos(((pts.reduce((sum, p) => sum + p.lat, 0) / n) * Math.PI) / 180) || 1;
    const dist = (a: { lat: number; lng: number }, b: { lat: number; lng: number }) => Math.hypot((a.lng - b.lng) * k, a.lat - b.lat);
    const mid = (i: number) => {
        const a = pts[i];
        const b = pts[(i + 1) % n];
        return { lat: (a.lat + b.lat) / 2, lng: (a.lng + b.lng) / 2 };
    };

    // Cặp cạnh (a, b) không kề nhau có trung điểm xa nhau nhất = 2 cạnh đầu của dải
    let capA = -1;
    let capB = -1;
    let best = -1;
    for (let i = 0; i < n; i++) {
        const mi = mid(i);
        for (let j = i + 2; j < n; j++) {
            if (i === 0 && j === n - 1) continue; // cạnh cuối kề cạnh đầu
            const d = dist(mi, mid(j));
            if (d > best) {
                best = d;
                capA = i;
                capB = j;
            }
        }
    }
    if (capA < 0) return [];

    // 2 mép dải, cùng bắt đầu ở cạnh đầu capA và kết thúc ở cạnh đầu capB
    const sideA: { lat: number; lng: number }[] = [];
    for (let i = (capA + 1) % n; ; i = (i + 1) % n) {
        sideA.push(pts[i]);
        if (i === capB) break;
    }
    const sideB: { lat: number; lng: number }[] = [];
    for (let i = capA; ; i = (i - 1 + n) % n) {
        sideB.push(pts[i]);
        if (i === (capB + 1) % n) break;
    }

    const pointAt = (chain: { lat: number; lng: number }[], t: number) => {
        const lens: number[] = [];
        let total = 0;
        for (let i = 0; i < chain.length - 1; i++) {
            const d = dist(chain[i], chain[i + 1]);
            lens.push(d);
            total += d;
        }
        if (!total) return chain[0];
        let target = t * total;
        for (let i = 0; i < lens.length; i++) {
            if (target <= lens[i] || i === lens.length - 1) {
                const s = lens[i] ? Math.min(1, target / lens[i]) : 0;
                return {
                    lat: chain[i].lat + (chain[i + 1].lat - chain[i].lat) * s,
                    lng: chain[i].lng + (chain[i + 1].lng - chain[i].lng) * s,
                };
            }
            target -= lens[i];
        }
        return chain[chain.length - 1];
    };

    const centerline: L.LatLng[] = [];
    for (let i = 0; i <= numSamples; i++) {
        const t = i / numSamples;
        const a = pointAt(sideA, t);
        const b = pointAt(sideB, t);
        centerline.push(L.latLng((a.lat + b.lat) / 2, (a.lng + b.lng) / 2));
    }
    return centerline;
};

/** Đảo `line` nếu cần để cùng chiều với `reference` (so khoảng cách 2 đầu). */
export const orientLikeReference = (line: L.LatLng[], reference: L.LatLng[]): L.LatLng[] => {
    if (line.length < 2 || reference.length < 2) return line;
    const ref0 = reference[0];
    const sameDir = line[0].distanceTo(ref0) + line[line.length - 1].distanceTo(reference[reference.length - 1]);
    const reversed = line[line.length - 1].distanceTo(ref0) + line[0].distanceTo(reference[reference.length - 1]);
    return reversed < sameDir ? [...line].reverse() : line;
};

/**
 * Căn lại `line` vào CHÍNH GIỮA polygon `ring` ([lat, lng]) — dùng sau getCorridorDisplayCenterline
 * để đường định hướng luôn nằm giữa dải KHÔNG phụ thuộc cách vẽ 2 đầu zone (đầu xiên, đầu gồm
 * nhiều đỉnh...) và khớp hình đang hiển thị (truyền ring đã làm mịn).
 * 1. Mỗi điểm trong: bắn tia VUÔNG GÓC sang 2 mép, lấy trung điểm 2 giao điểm gần nhất (2 lượt).
 * 2. Bỏ các điểm sát đầu zone mà tia chạm vào CẠNH ĐẦU (cạnh chạy ngang, lệch > 40° so với
 *    hướng đường) — các điểm đó không đo được 2 mép thật nên sẽ bị kéo lệch.
 * 3. CẮT NGẮN 2 đầu một đoạn ≈ nửa bề rộng dải (tối đa 10% chiều dài): vùng sát cạnh đầu
 *    zone (nhất là đầu cắt xiên) không xác định được tâm chuẩn → không vẽ ở đó, giữa 2 zone
 *    liền kề có khoảng ngắt nhỏ thay vì đầu đường bị lệch.
 * Không đủ dữ liệu thì trả nguyên `line`.
 */
export const recenterLineInPolygon = (line: L.LatLng[], ring: number[][]): L.LatLng[] => {
    if (!Array.isArray(line) || line.length < 4 || !Array.isArray(ring) || ring.length < 4) return line;

    const k = Math.cos(((ring.reduce((sum, p) => sum + p[0], 0) / ring.length) * Math.PI) / 180) || 1;
    type P = { x: number; y: number };
    const toP = (lat: number, lng: number): P => ({ x: lng * k, y: lat });
    const poly: P[] = ring.map(([lat, lng]) => toP(lat, lng));
    const edges: { a: P; b: P; dir: P }[] = [];
    for (let i = 0; i < poly.length; i++) {
        const a = poly[i];
        const b = poly[(i + 1) % poly.length];
        const len = Math.hypot(b.x - a.x, b.y - a.y);
        if (len) edges.push({ a, b, dir: { x: (b.x - a.x) / len, y: (b.y - a.y) / len } });
    }

    const cross = (ax: number, ay: number, bx: number, by: number) => ax * by - ay * bx;
    /** Các giao điểm tia P + s·D với biên polygon: tham số s + cạnh bị chạm. */
    const hitsAlong = (p: P, d: P): { s: number; dir: P }[] => {
        const result: { s: number; dir: P }[] = [];
        for (const { a, b, dir } of edges) {
            const ex = b.x - a.x;
            const ey = b.y - a.y;
            const denom = cross(d.x, d.y, ex, ey);
            if (Math.abs(denom) < 1e-18) continue;
            const apx = a.x - p.x;
            const apy = a.y - p.y;
            const s = cross(apx, apy, ex, ey) / denom;
            const u = cross(apx, apy, d.x, d.y) / denom;
            if (u >= -1e-9 && u <= 1 + 1e-9) result.push({ s, dir });
        }
        return result;
    };
    const unit = (x: number, y: number): P | null => {
        const len = Math.hypot(x, y);
        return len ? { x: x / len, y: y / len } : null;
    };

    let pts: P[] = line.map((p) => toP(p.lat, p.lng));
    let widths: (number | null)[] = [];
    const EPS = 1e-12;

    // Cạnh "dọc" (mép dải) lệch hướng đường ≤ 40°; lớn hơn coi là cạnh đầu zone
    const SIDE_MIN_COS = Math.cos((40 * Math.PI) / 180);

    for (let pass = 0; pass < 3; pass++) {
        const next: P[] = [...pts];
        widths = pts.map(() => null);
        for (let i = 1; i < pts.length - 1; i++) {
            const t = unit(pts[i + 1].x - pts[i - 1].x, pts[i + 1].y - pts[i - 1].y);
            if (!t) continue;
            const n = { x: -t.y, y: t.x };
            const hits = hitsAlong(pts[i], n);
            let hPos: { s: number; dir: P } | null = null;
            let hNeg: { s: number; dir: P } | null = null;
            for (const h of hits) {
                if (h.s > EPS && (!hPos || h.s < hPos.s)) hPos = h;
                if (h.s < -EPS && (!hNeg || h.s > hNeg.s)) hNeg = h;
            }
            if (!hPos || !hNeg) continue;
            const isSide = (h: { dir: P }) => Math.abs(h.dir.x * t.x + h.dir.y * t.y) >= SIDE_MIN_COS;
            if (!isSide(hPos) || !isSide(hNeg)) continue; // tia chạm cạnh đầu zone → bỏ điểm này
            const sPos = hPos.s;
            const sNeg = hNeg.s;
            const shift = (sPos + sNeg) / 2;
            next[i] = { x: pts[i].x + n.x * shift, y: pts[i].y + n.y * shift };
            widths[i] = sPos - sNeg;
        }
        pts = next;
    }

    // Bỏ điểm 2 đầu (nằm trên cạnh đầu) + điểm có bề rộng hụt rõ so với trung vị (tia chạm cạnh đầu)
    const measured = widths.filter((w): w is number => w != null).sort((a, b) => a - b);
    if (measured.length < 3) return line;
    const median = measured[Math.floor(measured.length / 2)];
    let kept = pts.filter((_, i) => widths[i] != null && (widths[i] as number) >= median * 0.75);
    if (kept.length < 3) return line;

    // Cắt ngắn 2 đầu: bỏ đoạn sát cạnh đầu zone
    const segLens: number[] = [];
    let total = 0;
    for (let i = 0; i < kept.length - 1; i++) {
        const d = Math.hypot(kept[i + 1].x - kept[i].x, kept[i + 1].y - kept[i].y);
        segLens.push(d);
        total += d;
    }
    if (!total) return line;
    const trim = Math.min(median * 0.5, total * 0.1);
    /** Điểm nằm trên đường, cách điểm đầu một đoạn `dist` (đo theo chiều dài đường). */
    const pointAtDist = (dist: number): { p: P; seg: number } => {
        let acc = 0;
        for (let i = 0; i < segLens.length; i++) {
            if (acc + segLens[i] >= dist) {
                const r = segLens[i] ? (dist - acc) / segLens[i] : 0;
                return { p: { x: kept[i].x + (kept[i + 1].x - kept[i].x) * r, y: kept[i].y + (kept[i + 1].y - kept[i].y) * r }, seg: i };
            }
            acc += segLens[i];
        }
        return { p: kept[kept.length - 1], seg: segLens.length - 1 };
    };
    const head = pointAtDist(trim);
    const tail = pointAtDist(total - trim);
    kept = [head.p, ...kept.slice(head.seg + 1, tail.seg + 1), tail.p];

    return kept.map((p) => L.latLng(p.y, p.x / k));
};
