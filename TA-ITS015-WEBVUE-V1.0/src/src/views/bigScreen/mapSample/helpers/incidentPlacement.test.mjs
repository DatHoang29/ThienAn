import assert from 'node:assert/strict';
import test from 'node:test';
import { findOutsideZonePoint, INCIDENT_STACK_COMPACT_AFTER, INCIDENT_ZONE_GAP_PX, layoutIncidentStacks, resolveIncidentLineStyle } from './incidentPlacement.ts';

const points = (pairs) => pairs.map(([x, y]) => ({ x, y }));
const rectangle = points([[0, 0], [300, 0], [300, 40], [0, 40]]);
const inside = (p, ring) => {
    let crossings = 0;
    for (let i = 0; i < ring.length; i++) {
        const a = ring[i], b = ring[(i + 1) % ring.length];
        if ((a.y <= p.y && b.y > p.y) || (b.y <= p.y && a.y > p.y)) {
            const x = a.x + ((p.y - a.y) * (b.x - a.x)) / (b.y - a.y);
            if (x > p.x) crossings++;
        }
    }
    return crossings % 2 === 1;
};
const assertClear = (center, ring, radius) => {
    assert.ok(Number.isFinite(center.x) && Number.isFinite(center.y));
    assert.equal(inside(center, ring), false, 'icon center must be outside');
    // Kiểm tra độc lập toàn bộ đĩa bao icon, gồm nhiều mức bán kính.
    for (const fraction of [0.25, 0.5, 0.99]) {
        for (let degrees = 0; degrees < 360; degrees++) {
            const angle = degrees * Math.PI / 180;
            const p = { x: center.x + radius * fraction * Math.cos(angle), y: center.y + radius * fraction * Math.sin(angle) };
            assert.equal(inside(p, ring), false, `icon intersects zone at ${degrees} degrees`);
        }
    }
};
const rotate = (p, angle) => ({ x: p.x * Math.cos(angle) - p.y * Math.sin(angle), y: p.x * Math.sin(angle) + p.y * Math.cos(angle) });

test('full icon clears the edge for either winding and closed rings', () => {
    for (const ring of [rectangle, [...rectangle].reverse(), [...rectangle, rectangle[0]]]) {
        for (const anchor of [{ x: 150, y: 20 }, { x: 0, y: 20 }, { x: 300, y: 20 }]) {
            for (const size of [16, 32, 80, 160]) {
                const radius = (size + 8) / Math.SQRT2 + INCIDENT_ZONE_GAP_PX;
                assertClear(findOutsideZonePoint(anchor, ring, radius), ring, radius);
            }
        }
    }
});

test('pixel clearance survives rotation and zoom', () => {
    for (const scale of [0.05, 0.25, 1, 4, 20]) {
        for (const angle of [0.2, Math.PI / 2, 2.7]) {
            const ring = rectangle.map((p) => rotate({ x: p.x * scale, y: p.y * scale }, angle));
            const anchor = rotate({ x: 150 * scale, y: 20 * scale }, angle);
            assertClear(findOutsideZonePoint(anchor, ring, 36), ring, 36);
        }
    }
});

test('concave zones and repeated vertices cannot pull icons into the zone', () => {
    const ring = points([[0, 0], [400, 0], [400, 400], [300, 400], [300, 100], [100, 100], [100, 400], [0, 400], [0, 400]]);
    for (const anchor of [{ x: 200, y: 50 }, { x: 50, y: 350 }, { x: 100, y: 100 }, { x: 200, y: 200 }]) {
        for (const radius of [8, 36, 120, 600]) assertClear(findOutsideZonePoint(anchor, ring, radius), ring, radius);
    }
});

test('adjacent lane selects the free outer edge and remains deterministic', () => {
    const neighboring = points([[0, -50], [300, -50], [300, 0], [0, 0]]);
    const anchor = { x: 150, y: 20 };
    const result = findOutsideZonePoint(anchor, rectangle, 30, [neighboring]);
    assert.ok(result.y >= 70, 'choose the free long edge of the corresponding lane');
    assertClear(result, rectangle, 30);
    assertClear(result, neighboring, 30);
    assert.deepEqual(findOutsideZonePoint(anchor, rectangle, 30, [neighboring]), result);
    assert.deepEqual(anchor, { x: 150, y: 20 }, 'physical incident anchor stays unchanged');
});

test('degenerate geometry still returns a finite exterior position', () => {
    const anchor = { x: 20, y: 20 };
    for (const ring of [[], [anchor], [anchor, anchor], points([[0, 0], [200, 0], [0, 0]])]) {
        const result = findOutsideZonePoint(anchor, ring, 30);
        assert.ok(Number.isFinite(result.x) && Number.isFinite(result.y));
        assert.notDeepEqual(result, anchor);
    }
});

const dist = (a, b) => Math.hypot(a.x - b.x, a.y - b.y);

test('a lone incident keeps the single-icon position', () => {
    const anchor = { x: 150, y: 20 };
    const result = layoutIncidentStacks([{ anchor, ring: rectangle, radius: 30 }]);
    assert.deepEqual(result.points[0], findOutsideZonePoint(anchor, rectangle, 30));
    assert.deepEqual(result.groups, [[0]]);
    assert.deepEqual(result.groupSizes, [1]);
});

test('nearby incidents form ONE stack next to the real location, every layer outside the zone', () => {
    // 6 sự kiện dồn trong 20px của cùng 1 làn: vị trí gốc gần như trùng nhau.
    const radius = 20;
    const items = [0, 4, 8, 12, 16, 20].map((offset) => ({ anchor: { x: 140 + offset, y: 20 }, ring: rectangle, radius }));
    const result = layoutIncidentStacks(items);
    assert.equal(result.groups.length, 1, 'gom thành 1 chồng');
    assert.deepEqual(result.groups[0], [0, 1, 2, 3, 4, 5], 'thứ tự lớp = thứ tự đầu vào');
    assert.deepEqual(result.layers, [0, 1, 2, 3, 4, 5], 'không ẩn icon nào');
    result.points.forEach((point) => assertClear(point, rectangle, radius));

    // Chồng nằm sát vị trí thật: không trôi xa hơn (bán kính + nửa dải lớp) so với vị trí gốc.
    const bases = items.map((item) => findOutsideZonePoint(item.anchor, rectangle, radius));
    const meanBase = { x: bases.reduce((s, p) => s + p.x, 0) / bases.length, y: bases.reduce((s, p) => s + p.y, 0) / bases.length };
    result.points.forEach((point) => assert.ok(dist(point, meanBase) <= radius * 4, `layer drifted too far (${dist(point, meanBase)})`));

    // Mọi lớp so le, mỗi lớp lộ ra 1 phần (không trùng khít, không tách rời); chồng đông xếp sát hơn
    // nhưng dải không dài hơn chồng ${INCIDENT_STACK_COMPACT_AFTER} lớp.
    for (let k = 1; k < result.points.length; k++) {
        const gap = dist(result.points[k], result.points[k - 1]);
        assert.ok(gap >= radius * 0.5 - 0.01 && gap < radius * 2, `layer gap ${gap}`);
    }
    const spread = dist(result.points[0], result.points[result.points.length - 1]);
    assert.ok(spread <= radius * 0.9 * (INCIDENT_STACK_COMPACT_AFTER - 1) + 0.01, `stack too long (${spread})`);
    assert.deepEqual(layoutIncidentStacks(items), result, 'layout is deterministic');
});

test('incidents far apart stay separate single icons', () => {
    const radius = 18;
    const result = layoutIncidentStacks([
        { anchor: { x: 30, y: 20 }, ring: rectangle, radius },
        { anchor: { x: 270, y: 20 }, ring: rectangle, radius },
    ]);
    assert.equal(result.groups.length, 2);
    assert.deepEqual(result.groupSizes, [1, 1]);
    assert.deepEqual(result.layers, [0, 0]);
});

test('zooming in splits the stack', () => {
    const radius = 18;
    const anchors = [{ x: 140, y: 20 }, { x: 150, y: 20 }];
    const zoomedOut = layoutIncidentStacks(anchors.map((anchor) => ({ anchor, ring: rectangle, radius })));
    assert.equal(zoomedOut.groups.length, 1);
    const scale = 8;
    const ring = rectangle.map((p) => ({ x: p.x * scale, y: p.y * scale }));
    const zoomedIn = layoutIncidentStacks(anchors.map((anchor) => ({ anchor: { x: anchor.x * scale, y: anchor.y * scale }, ring, radius })));
    assert.equal(zoomedIn.groups.length, 2);
});

test('stacks stay outside rotated and concave zones', () => {
    const concave = points([[0, 0], [400, 0], [400, 400], [300, 400], [300, 100], [100, 100], [100, 400], [0, 400]]);
    for (const angle of [0, 0.4, Math.PI / 2, 2.5]) {
        const ring = concave.map((p) => rotate(p, angle));
        for (const target of [{ x: 200, y: 50 }, { x: 100, y: 120 }, { x: 50, y: 380 }]) {
            const items = [0, 3, 6].map((d) => ({ anchor: rotate({ x: target.x + d, y: target.y }, angle), ring, radius: 24 }));
            layoutIncidentStacks(items).points.forEach((point) => assertClear(point, ring, 24));
        }
    }
});

test('incidents in adjacent zones are stacked together instead of overlapping', () => {
    const upper = points([[0, -40], [300, -40], [300, 0], [0, 0]]);
    const radius = 18;
    const result = layoutIncidentStacks([
        { anchor: { x: 150, y: 20 }, ring: rectangle, radius, neighboringRings: [upper] },
        { anchor: { x: 152, y: -20 }, ring: upper, radius, neighboringRings: [rectangle] },
        { anchor: { x: 154, y: 20 }, ring: rectangle, radius, neighboringRings: [upper] },
    ]);
    // Icon thuộc các chồng khác nhau không đè nhau quá nửa (đè quá nửa thì đã phải gom).
    result.groups.forEach((group, a) => result.groups.forEach((other, b) => {
        if (b <= a) return;
        group.forEach((i) => other.forEach((j) => assert.ok(dist(result.points[i], result.points[j]) > radius - 0.01)));
    }));
    assertClear(result.points[0], rectangle, radius);
    assertClear(result.points[2], rectangle, radius);
});

test('incidents only partly overlapping keep their own positions (no stacking)', () => {
    const radius = 20;
    const items = [{ x: 120, y: 20 }, { x: 150, y: 20 }].map((anchor) => ({ anchor, ring: rectangle, radius }));
    const result = layoutIncidentStacks(items);
    assert.equal(result.groups.length, 2, 'cách 1.5 bán kính thì không gom');
    items.forEach((item, i) => assert.deepEqual(result.points[i], findOutsideZonePoint(item.anchor, rectangle, radius)));
});

test('no chain stacking: a row of incidents is not pulled into one long stack', () => {
    const radius = 20;
    // 6 sự kiện cách đều 16px: từng cặp kề nhau rất gần nhưng hai đầu cách 80px.
    const items = [0, 1, 2, 3, 4, 5].map((i) => ({ anchor: { x: 100 + i * 16, y: 20 }, ring: rectangle, radius }));
    const result = layoutIncidentStacks(items);
    assert.ok(result.groups.length >= 3, `bị gom dây chuyền (${result.groups.length} nhóm)`);
    result.groups.forEach((group) => group.forEach((i) => group.forEach((j) =>
        assert.ok(Math.abs(items[i].anchor.x - items[j].anchor.x) <= radius + 0.01, 'mọi thành viên trong chồng phải gần nhau'))));
    // Mỗi icon không trôi xa vị trí gốc của nó.
    result.points.forEach((point, i) => assert.ok(dist(point, findOutsideZonePoint(items[i].anchor, rectangle, radius)) <= radius * 2));
});

test('left/right order inside a stack follows the real positions (leader lines do not cross)', () => {
    const radius = 20;
    const result = layoutIncidentStacks([
        { anchor: { x: 160, y: 20 }, ring: rectangle, radius },
        { anchor: { x: 150, y: 20 }, ring: rectangle, radius },
        { anchor: { x: 145, y: 20 }, ring: rectangle, radius },
    ]);
    assert.equal(result.groups.length, 1);
    assert.ok(result.points[2].x < result.points[1].x && result.points[1].x < result.points[0].x);
    assert.deepEqual(result.layers, [0, 1, 2], 'lớp trên/dưới vẫn theo thứ tự đầu vào');
});

test('incident line style defaults to solid', () => {
    for (const value of [undefined, null, '', 'zigzag', 1]) assert.equal(resolveIncidentLineStyle(value), 'solid');
    for (const value of ['solid', 'dashed', 'dotted']) assert.equal(resolveIncidentLineStyle(value), value);
});

test('icon stays on its own carriageway side at a bend near a zone joint (does not jump across the opposite lane)', () => {
    // Đường 2 chiều uốn cong: zone sự cố là làn dưới, ngay chỗ nối với zone kế tiếp cùng làn;
    // làn ngược chiều nằm phía trên. Trước đây icon bị đẩy vượt qua làn ngược chiều lên phía trên.
    const roadCenter = (s) => ({ x: s, y: 60 * Math.tanh((s - 200) / 50) });
    const tangentAt = (s) => {
        const a = roadCenter(s - 0.5), b = roadCenter(s + 0.5);
        const l = Math.hypot(b.x - a.x, b.y - a.y);
        return { x: (b.x - a.x) / l, y: (b.y - a.y) / l };
    };
    // d > 0: phía dưới (bên phải chiều đi), d < 0: phía trên
    const offset = (s, d) => {
        const c = roadCenter(s), t = tangentAt(s);
        return { x: c.x - t.y * d, y: c.y + t.x * d };
    };
    const band = (s0, s1, d0, d1) => {
        const a = [], b = [];
        for (let s = s0; s <= s1; s += 80) {
            a.push(offset(s, d0));
            b.push(offset(s, d1));
        }
        if ((s1 - s0) % 80) {
            a.push(offset(s1, d0));
            b.push(offset(s1, d1));
        }
        return [...a, ...b.reverse()];
    };
    const width = 30;
    const zone = band(150, 350, -1, width);
    const previousZone = band(-50, 150, -1, width);
    const oppositeLanes = [band(-100, 150, -width, 1), band(150, 400, -width, 1)];
    const radius = 40;
    for (const s of [150, 151, 160]) {
        const anchor = offset(s, width / 2);
        const icon = findOutsideZonePoint(anchor, zone, radius, [previousZone, ...oppositeLanes]);
        for (let k = 1; k <= 50; k++) {
            const p = { x: anchor.x + ((icon.x - anchor.x) * k) / 50, y: anchor.y + ((icon.y - anchor.y) * k) / 50 };
            for (const lane of oppositeLanes) assert.equal(inside(p, lane), false, `leader crosses the opposite lane (anchor s=${s})`);
        }
    }
});

test('incident exactly at a zone joint on a bend stays below the divider when zoomed in (does not jump above the opposite lane)', () => {
    // Dữ liệu dựng từ trường hợp lỗi thật: sự kiện đúng lý trình đầu zone (Km 26+0) → điểm neo ngoại suy
    // rơi ĐÚNG lên mép giáp làn ngược chiều; ở mức zoom lớn icon từng bị đẩy lên trên làn ngược chiều.
    const zone = points([[1093,7],[1143,57],[1194,104],[1246,145],[1298,179],[1351,205],[1403,224],[1455,237],[1507,246],[1558,252],[1608,256],[1659,259],[1709,261],[1760,262],[1810,262],[1860,263],[1910,263],[1880,239],[1830,239],[1780,238],[1730,237],[1680,236],[1630,233],[1580,230],[1530,225],[1480,217],[1430,206],[1380,191],[1330,169],[1280,140],[1230,103],[1180,59],[1130,10],[1080,-40]]);
    const previousZone = points([[310,-215],[509,-213],[609,-209],[707,-201],[804,-183],[899,-148],[947,-120],[995,-84],[1044,-41],[1093,7],[1080,-40],[1030,-86],[980,-126],[930,-158],[880,-183],[780,-213],[680,-228],[580,-235],[480,-238],[280,-240]]);
    const oppositeBefore = points([[280,-240],[480,-238],[580,-235],[680,-228],[780,-213],[880,-183],[930,-158],[980,-126],[1030,-86],[1080,-40],[1097,-57],[1046,-104],[994,-145],[942,-179],[889,-205],[785,-237],[682,-252],[581,-259],[480,-262],[280,-264]]);
    const oppositeAfter = points([[1080,-40],[1130,10],[1180,59],[1230,103],[1280,140],[1330,169],[1380,191],[1430,206],[1480,217],[1530,225],[1580,230],[1630,233],[1680,236],[1730,237],[1780,238],[1830,239],[1880,239],[1880,215],[1830,215],[1780,214],[1731,213],[1681,212],[1631,209],[1582,206],[1533,201],[1484,194],[1436,183],[1388,168],[1341,148],[1293,120],[1245,84],[1196,41],[1147,-7],[1097,-57]]);
    const divider = zone.slice(17).reverse(); // mép giáp làn ngược chiều, x tăng dần
    const dividerY = (x) => {
        for (let i = 0; i < divider.length - 1; i++) {
            const a = divider[i], b = divider[i + 1];
            if (x >= a.x && x <= b.x) return a.y + ((x - a.x) * (b.y - a.y)) / (b.x - a.x);
        }
        return x < divider[0].x ? divider[0].y : divider[divider.length - 1].y;
    };
    const icon = findOutsideZonePoint({ x: 1077, y: -38 }, zone, 31, [previousZone, oppositeBefore, oppositeAfter]);
    assert.ok(icon.y > dividerY(icon.x), `icon jumped above the opposite lane: ${JSON.stringify(icon)}`);
});
