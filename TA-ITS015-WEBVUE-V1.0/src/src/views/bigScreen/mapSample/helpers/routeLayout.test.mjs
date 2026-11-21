import assert from 'node:assert/strict';
import test from 'node:test';
import { computeRouteLayout, formatRouteKm, getEquipmentRoutePosition, ROUTE_MAX_WIDTH, ROUTE_MIN_WIDTH } from './routeLayout.ts';

const OPTIONS = { itemWidth: 40, rowHeight: 56 };
const byKey = (result, key) => result.items.find((item) => item.key === key);

test('getEquipmentRoutePosition: km*1000+met, thiếu cả hai = null', () => {
    assert.equal(getEquipmentRoutePosition({ kmNumber: 3, metNumber: 250 }), 3250);
    assert.equal(getEquipmentRoutePosition({ kmNumber: 3 }), 3000);
    assert.equal(getEquipmentRoutePosition({ metNumber: 40 }), 40);
    assert.equal(getEquipmentRoutePosition({}), null);
    assert.equal(getEquipmentRoutePosition(undefined), null);
});

test('formatRouteKm', () => {
    assert.equal(formatRouteKm(1250), 'Km1+250');
    assert.equal(formatRouteKm(0), 'Km0+0');
});

test('chiều dài tự đo từ Km nhỏ nhất đến lớn nhất, độ rộng tỉ lệ theo chiều dài', () => {
    const result = computeRouteLayout(
        [
            { key: 'a', position: 1000 },
            { key: 'b', position: 2000 },
            { key: 'c', position: 1500 },
        ],
        OPTIONS
    );
    assert.equal(result.startPosition, 1000);
    assert.equal(result.endPosition, 2000);
    assert.equal(result.lengthMeters, 1000);
    // 1000m * 0.4 + 2*24 = 448
    assert.equal(result.width, 448);
    assert.ok(byKey(result, 'a').centerPx < byKey(result, 'c').centerPx);
    assert.ok(byKey(result, 'c').centerPx < byKey(result, 'b').centerPx);
    assert.equal(byKey(result, 'c').centerPx, 224);
    assert.equal(result.rows, 1);
    assert.equal(result.height, 56);
});

test('độ rộng bị chặn trong [min, max]', () => {
    const short = computeRouteLayout([{ key: 'a', position: 0 }, { key: 'b', position: 10 }], OPTIONS);
    assert.equal(short.width, ROUTE_MIN_WIDTH);
    const long = computeRouteLayout([{ key: 'a', position: 0 }, { key: 'b', position: 100000 }], OPTIONS);
    assert.equal(long.width, ROUTE_MAX_WIDTH);
});

test('thiết bị trùng Km xếp chồng dọc, mỗi thiết bị một hàng', () => {
    const result = computeRouteLayout(
        [
            { key: 'a', position: 500 },
            { key: 'b', position: 500 },
            { key: 'c', position: 500 },
        ],
        OPTIONS
    );
    assert.deepEqual(
        result.items.map((item) => item.row).sort(),
        [0, 1, 2]
    );
    assert.equal(result.rows, 3);
    assert.equal(result.height, 168);
    // chiều dài 0 → tất cả đặt giữa thanh
    assert.equal(new Set(result.items.map((item) => item.centerPx)).size, 1);
});

test('thiết bị đủ xa nhau thì cùng 1 hàng, gần nhau thì xuống hàng kế', () => {
    const result = computeRouteLayout(
        [
            { key: 'a', position: 0 },
            { key: 'b', position: 20 }, // 8px — gần hơn pitch 48px
            { key: 'c', position: 2000 },
        ],
        OPTIONS
    );
    assert.equal(byKey(result, 'a').row, 0);
    assert.equal(byKey(result, 'b').row, 1);
    assert.equal(byKey(result, 'c').row, 0);
});

test('đảo chiều: Km giảm dần từ trái sang phải', () => {
    const items = [
        { key: 'a', position: 0 },
        { key: 'b', position: 1000 },
    ];
    const normal = computeRouteLayout(items, OPTIONS);
    const reversed = computeRouteLayout(items, { ...OPTIONS, reverse: true });
    assert.ok(byKey(normal, 'a').centerPx < byKey(normal, 'b').centerPx);
    assert.ok(byKey(reversed, 'a').centerPx > byKey(reversed, 'b').centerPx);
    assert.equal(reversed.startPosition, 0);
});

test('thiết bị chưa có Km nằm ở hàng cuối, trái → phải, không đè thiết bị đã có Km', () => {
    const result = computeRouteLayout(
        [
            { key: 'a', position: 0 },
            { key: 'b', position: 1000 },
            { key: 'x', position: null },
            { key: 'y' },
        ],
        OPTIONS
    );
    const x = byKey(result, 'x');
    const y = byKey(result, 'y');
    assert.equal(x.unplaced, true);
    assert.equal(y.unplaced, true);
    assert.equal(x.row, 1);
    assert.equal(y.row, 1);
    assert.ok(x.centerPx < y.centerPx);
    assert.equal(byKey(result, 'a').unplaced, false);
    assert.equal(result.rows, 2);
});

test('chỉ có thiết bị chưa có Km: không đo được chiều dài', () => {
    const result = computeRouteLayout([{ key: 'x' }, { key: 'y' }], OPTIONS);
    assert.equal(result.startPosition, null);
    assert.equal(result.endPosition, null);
    assert.equal(result.lengthMeters, 0);
    assert.equal(result.width, ROUTE_MIN_WIDTH);
});

test('danh sách rỗng', () => {
    const result = computeRouteLayout([], OPTIONS);
    assert.equal(result.rows, 0);
    assert.equal(result.height, 0);
    assert.deepEqual(result.items, []);
});

test('blockTypes: layout route/routeReverse được giữ, config cũ không phình thêm field', async () => {
    const { normalizeBlockConfiguration } = await import('../blockTypes.ts');
    const config = normalizeBlockConfiguration({
        blocks: [
            {
                id: 'b1',
                levels: [
                    { id: 'l1', name: 'Hầm trái', layout: 'route', routeReverse: true, devices: [{ id: 'd1', deviceId: '1' }] },
                    { id: 'l2', name: 'Cũ', devices: [{ id: 'd2', deviceId: '2' }] },
                    { id: 'l3', name: 'Rác', layout: 'xyz', routeReverse: true, devices: [] },
                ],
            },
        ],
    });
    const [route, legacy, junk] = config.blocks[0].levels;
    assert.equal(route.layout, 'route');
    assert.equal(route.routeReverse, true);
    assert.equal('layout' in legacy, false);
    assert.equal('routeReverse' in legacy, false);
    assert.equal('layout' in junk, false);
    assert.equal('routeReverse' in junk, false);
});

test('19 camera thực tế (cặp đầu cách 100m, còn lại 120m) nằm trọn 1 hàng nhờ tự giãn tỉ lệ', () => {
    const km = [121983, 122083, 122203, 122323, 122443, 122563, 122683, 122803, 122923, 123043, 123163, 123283, 123403, 123523, 123643, 123763, 123883, 124003, 124123];
    const result = computeRouteLayout(km.map((position, index) => ({ key: `d${index}`, position })), OPTIONS);
    assert.equal(result.rows, 1);
    assert.ok(result.width <= ROUTE_MAX_WIDTH);
    // mọi cặp liền kề cách nhau ≥ 48px (40px thiết bị + 8px hở)
    const centers = result.items.map((item) => item.centerPx).sort((a, b) => a - b);
    for (let i = 1; i < centers.length; i++) assert.ok(centers[i] - centers[i - 1] >= 48 - 0.01);
});

test('adaptive=false giữ tỉ lệ cố định: cặp 100m (40px) xuống hàng 2', () => {
    const result = computeRouteLayout([{ key: 'a', position: 0 }, { key: 'b', position: 100 }, { key: 'c', position: 2000 }], { ...OPTIONS, adaptive: false });
    assert.equal(byKey(result, 'b').row, 1);
});

test('cặp sát hơn trần tự giãn (50m < 80m) vẫn xếp chồng dọc, tuyến không phình quá mức', () => {
    const result = computeRouteLayout([{ key: 'a', position: 0 }, { key: 'b', position: 50 }, { key: 'c', position: 2000 }], OPTIONS);
    assert.equal(byKey(result, 'b').row, 1);
    // trần 0.6 px/m: 2000*0.6+48 = 1248 → chặn ở ROUTE_MAX_WIDTH
    assert.equal(result.width, ROUTE_MAX_WIDTH);
});

test('khoảng cố định: thanh dài đúng khoảng, không phụ thuộc Km thiết bị', () => {
    const result = computeRouteLayout([{ key: 'a', position: 1500 }, { key: 'b', position: 1700 }], { ...OPTIONS, fixedStart: 1000, fixedEnd: 2000 });
    assert.equal(result.startPosition, 1000);
    assert.equal(result.endPosition, 2000);
    assert.equal(result.lengthMeters, 1000);
    assert.equal(result.width, 448);
    assert.equal(byKey(result, 'a').centerPx, 224);
    assert.equal(byKey(result, 'a').outOfRange, undefined);
});

test('khoảng cố định: thiết bị ngoài khoảng bị kẹp vào mép và đánh dấu outOfRange', () => {
    const result = computeRouteLayout([{ key: 'a', position: 500 }, { key: 'b', position: 2500 }, { key: 'c', position: 1500 }], { ...OPTIONS, fixedStart: 1000, fixedEnd: 2000 });
    assert.equal(byKey(result, 'a').outOfRange, true);
    assert.equal(byKey(result, 'b').outOfRange, true);
    assert.equal(byKey(result, 'c').outOfRange, undefined);
    assert.equal(byKey(result, 'a').centerPx, 24);
    assert.equal(byKey(result, 'b').centerPx, 424);
});

test('khoảng cố định không hợp lệ (đầu >= cuối hoặc thiếu) thì tự đo như cũ', () => {
    const items = [{ key: 'a', position: 0 }, { key: 'b', position: 1000 }];
    const auto = computeRouteLayout(items, OPTIONS);
    assert.equal(computeRouteLayout(items, { ...OPTIONS, fixedStart: 2000, fixedEnd: 1000 }).width, auto.width);
    assert.equal(computeRouteLayout(items, { ...OPTIONS, fixedStart: 100 }).width, auto.width);
    assert.equal(computeRouteLayout(items, { ...OPTIONS, fixedStart: null, fixedEnd: null }).lengthMeters, 1000);
});

test('khoảng cố định + đảo chiều: Km nhỏ ở bên phải', () => {
    const result = computeRouteLayout([{ key: 'a', position: 1000 }, { key: 'b', position: 2000 }], { ...OPTIONS, fixedStart: 1000, fixedEnd: 2000, reverse: true });
    assert.ok(byKey(result, 'a').centerPx > byKey(result, 'b').centerPx);
});

test('blockTypes: rangeFrom/rangeTo/zoneId/directionId chỉ giữ khi hợp lệ và layout=route', async () => {
    const { normalizeBlockConfiguration } = await import('../blockTypes.ts');
    const config = normalizeBlockConfiguration({
        blocks: [
            {
                id: 'b1',
                levels: [
                    { id: 'l1', layout: 'route', rangeFrom: 121970, rangeTo: 124230, zoneId: 'z1', directionId: 2, devices: [] },
                    { id: 'l2', layout: 'route', rangeFrom: 5000, rangeTo: 1000, directionId: 0, devices: [] },
                    { id: 'l3', layout: 'route', rangeFrom: 1000, devices: [] },
                    { id: 'l4', rangeFrom: 1, rangeTo: 2, zoneId: 'z', directionId: 1, devices: [] },
                ],
            },
        ],
    });
    const [ok, reversedRange, missingTo, notRoute] = config.blocks[0].levels;
    assert.deepEqual([ok.rangeFrom, ok.rangeTo, ok.zoneId, ok.directionId], [121970, 124230, 'z1', 2]);
    for (const key of ['rangeFrom', 'rangeTo', 'directionId']) assert.equal(key in reversedRange, false);
    for (const key of ['rangeFrom', 'rangeTo']) assert.equal(key in missingTo, false);
    for (const key of ['rangeFrom', 'rangeTo', 'zoneId', 'directionId']) assert.equal(key in notRoute, false);
});

test('nhiều tầng cùng khoảng Km: ép chung độ rộng + lề thì cùng 1 Km nằm thẳng hàng', () => {
    const range = { fixedStart: 1000, fixedEnd: 3000 };
    const levelA = [{ key: 'a1', position: 1500 }, { key: 'a2', position: 2500 }];
    const levelB = [{ key: 'b1', position: 1500 }];
    // tầng A có thiết bị rộng 56px, tầng B chỉ 40px → nếu để mặc định, lề và độ rộng khác nhau
    const widthA = computeRouteLayout(levelA, { itemWidth: 56, rowHeight: 56, ...range });
    const aloneB = computeRouteLayout(levelB, { itemWidth: 40, rowHeight: 56, ...range });
    assert.notEqual(widthA.width, aloneB.width);

    const padding = 56 / 2 + 4;
    const blockWidth = Math.max(widthA.width, aloneB.width);
    const a = computeRouteLayout(levelA, { itemWidth: 56, rowHeight: 56, ...range, padding, minWidth: blockWidth, maxWidth: blockWidth });
    const b = computeRouteLayout(levelB, { itemWidth: 40, rowHeight: 56, ...range, padding, minWidth: blockWidth, maxWidth: blockWidth });
    assert.equal(a.width, blockWidth);
    assert.equal(b.width, blockWidth);
    assert.equal(byKey(a, 'a1').centerPx, byKey(b, 'b1').centerPx);
});

test('padding mặc định không đổi khi không truyền (tương thích ngược)', () => {
    const items = [{ key: 'a', position: 0 }, { key: 'b', position: 1000 }];
    assert.equal(computeRouteLayout(items, OPTIONS).width, computeRouteLayout(items, { ...OPTIONS, padding: 24 }).width);
});

test('groupRows: mỗi loại 1 dòng riêng trong cùng thanh, Km vẫn thẳng hàng, không bật thì như cũ', () => {
    const items = [
        { key: 'c1', position: 0, group: 'cam' },
        { key: 'c2', position: 1000, group: 'cam' },
        { key: 'p1', position: 0, group: 'ptz' }, // cùng Km với c1 nhưng khác loại → dòng khác
        { key: 'p2', position: 500, group: 'ptz' },
    ];
    const off = computeRouteLayout(items, OPTIONS);
    assert.equal(byKey(off, 'c1').row, 0);
    assert.equal(byKey(off, 'p1').row, 1); // không nhóm: trùng Km phải xếp chồng
    assert.equal(byKey(off, 'p2').row, 0); // không nhóm: p2 chen vào hàng 0

    const on = computeRouteLayout(items, { ...OPTIONS, groupRows: true });
    assert.equal(on.rows, 2);
    assert.equal(byKey(on, 'c1').row, 0);
    assert.equal(byKey(on, 'c2').row, 0);
    assert.equal(byKey(on, 'p1').row, 1);
    assert.equal(byKey(on, 'p2').row, 1);
    assert.equal(byKey(on, 'c1').centerPx, byKey(on, 'p1').centerPx); // cùng Km → cùng x
    assert.equal(on.width, off.width);
});

test('groupRows: trong 1 nhóm quá sát vẫn xếp chồng, nhóm sau nằm dưới mọi dòng của nhóm trước; thiết bị chưa có Km ở cuối', () => {
    const items = [
        { key: 'a1', position: 0, group: 'a' },
        { key: 'a2', position: 10, group: 'a' }, // sát a1 → dòng 2 của nhóm a
        { key: 'a3', position: 5000, group: 'a' }, // kéo dài thanh để 10m thật sự sát nhau
        { key: 'b1', position: 0, group: 'b' },
        { key: 'x', group: 'b' }, // chưa có Km
    ];
    const result = computeRouteLayout(items, { ...OPTIONS, groupRows: true });
    assert.equal(byKey(result, 'a1').row, 0);
    assert.equal(byKey(result, 'a2').row, 1);
    assert.equal(byKey(result, 'a3').row, 0);
    assert.equal(byKey(result, 'b1').row, 2);
    assert.equal(byKey(result, 'x').row, 3);
    assert.equal(result.rows, 4);
});

test('blockTypes: splitRowsByType chỉ giữ khi true và layout=route', async () => {
    const { normalizeBlockConfiguration } = await import('../blockTypes.ts');
    const config = normalizeBlockConfiguration({
        blocks: [{ id: 'b', levels: [
            { id: 'l1', layout: 'route', splitRowsByType: true, devices: [] },
            { id: 'l2', layout: 'route', splitRowsByType: false, devices: [] },
            { id: 'l3', splitRowsByType: true, devices: [] },
        ] }],
    });
    const [on, off, notRoute] = config.blocks[0].levels;
    assert.equal(on.splitRowsByType, true);
    assert.equal('splitRowsByType' in off, false);
    assert.equal('splitRowsByType' in notRoute, false);
});

test('groupRows: độ giãn thanh chỉ tính khoảng cách trong cùng nhóm, không để loại khác làm phình', () => {
    // cam cách nhau 120m; ptz cách cam chỉ 20m nhưng khác loại (dòng khác) → không được kéo thanh giãn
    const items = [
        { key: 'c1', position: 0, group: 'cam' },
        { key: 'c2', position: 120, group: 'cam' },
        { key: 'c3', position: 2000, group: 'cam' },
        { key: 'p1', position: 20, group: 'ptz' },
    ];
    const grouped = computeRouteLayout(items, { ...OPTIONS, groupRows: true });
    const mixed = computeRouteLayout(items, OPTIONS);
    assert.ok(grouped.width < mixed.width);
    assert.equal(grouped.rows, 2);
});
