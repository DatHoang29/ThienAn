import assert from 'node:assert/strict';
import test from 'node:test';
import { buildGeoJsonTransfer, parseGeoJsonTransfer, isSameCoordinateSystem, GEOJSON_TRANSFER_VERSION } from './geoJsonTransfer.ts';
import { normalizeBlockConfiguration } from '../blockTypes.ts';

const MAP_MODES = ['vms', 'full', 'cctv', 'block', 'block-vms'];
const MAP_TYPES = ['image', 'online', 'offline'];

const point = (id, itemGroup, coordinates, properties = {}) => ({
    type: 'Feature',
    geometry: { type: 'Point', coordinates },
    properties: { id, itemGroup, ...properties },
});

const features = [
    point('dev-1', 'device', [107.1, 10.8]),
    {
        type: 'Feature',
        geometry: { type: 'Polygon', coordinates: [[[107, 10], [107.1, 10], [107.1, 10.1], [107, 10]]] },
        properties: { id: 'zone-1', itemGroup: 'zone', kmDirection: 'reverse', flowDirection: 'forward' },
    },
    point('txt-1', 'text', [107.2, 10.9], { text: 'Hầm 1', rotation: 30, rotateWithMap: false, scaleMode: 'geo', refZoom: 15 }),
    point('img-1', 'image', [107.3, 10.9], { imageUrl: 'data:image/png;base64,AAAA', width: 120, rotateWithMap: true }),
    point('html-1', 'html', [107.4, 10.9], { html: '<p><b>Trạm</b> <img src="data:image/png;base64,BB"></p>', width: 260, scale: 1.5, scaleMode: 'fixed' }),
];

const mapConfig = {
    view: { latitude: 10.85, longtitude: 107.2, defaultZoom: 14, minZoom: 10, maxZoom: 18, rotation: 45, boundRadius: 3000 },
    display: {
        showIconBackground: false,
        iconSize: 28,
        incidentIconSize: 0,
        incidentZoneGap: 0,
        incidentLineStyle: 'dashed',
        backgroundColor: '#123456',
        zoneLabelDisplay: { name: true, kmArrow: false, arrowFrom: true, arrowTo: false, flowArrow: true, speed: false },
    },
};

const blockConfig = normalizeBlockConfiguration({
    showLevelName: false,
    translucent: true,
    translucentOpacity: 0.35,
    translucentScope: 'background',
    opaqueOnHover: false,
    blocks: [{
        id: 'block-1', name: 'Block A', anchorDeviceId: 'dev-1',
        actualPosition: { lat: 10.8, lng: 107.1 }, displayPosition: { lat: 10.81, lng: 107.11 },
        lineStyle: 'dotted', lineWidth: 3, lineColor: '#ff0000', anchorColor: '#00ff00', anchorRadius: 6,
        waypoints: [{ lat: 10.805, lng: 107.105 }],
        levels: [{ id: 'level-1', name: 'Tầng 1', devices: [{ id: 'bd-1', deviceId: 'dev-2' }] }],
    }],
});

/** Mô phỏng đúng đường đi thật: object → JSON text (file) → JSON.parse → parse. */
const roundTrip = (payload) => parseGeoJsonTransfer(JSON.parse(JSON.stringify(buildGeoJsonTransfer(payload))));

test('round-trip giữ nguyên features, mapConfig, blockConfig ở mọi chế độ và loại bản đồ', () => {
    for (const mapMode of MAP_MODES) {
        for (const mapType of MAP_TYPES) {
            const meta = { mapId: 'map-1', mapType, mapMode, exportedAt: '2026-10-23T00:00:00.000Z' };
            const result = roundTrip({ features, blockConfig, mapConfig, meta });
            assert.ok(result, `${mapMode}/${mapType}`);
            assert.deepEqual(result.features, features);
            assert.deepEqual(result.mapConfig, mapConfig);
            assert.deepEqual(normalizeBlockConfiguration(result.blockConfig), blockConfig);
            assert.deepEqual(result.meta, { ...meta, version: GEOJSON_TRANSFER_VERSION });
            assert.equal(result.skipped, 0);
            assert.equal(result.duplicates, 0);
        }
    }
});

test('không có block nào vẫn giữ tuỳ chọn hiển thị block', () => {
    const emptyBlocks = normalizeBlockConfiguration({ blocks: [], translucent: true, translucentScope: 'background', showLevelName: false });
    const result = roundTrip({ features, blockConfig: emptyBlocks });
    assert.deepEqual(normalizeBlockConfiguration(result.blockConfig), emptyBlocks);
});

test('file cũ: chỉ có features / blockConfig trong config / mảng features thuần', () => {
    const legacy = parseGeoJsonTransfer({ type: 'FeatureCollection', features, config: { blockConfig } });
    assert.deepEqual(legacy.features, features);
    assert.deepEqual(normalizeBlockConfiguration(legacy.blockConfig), blockConfig);
    assert.equal(legacy.mapConfig, undefined);
    assert.equal(legacy.meta, undefined);

    const bare = parseGeoJsonTransfer(features);
    assert.deepEqual(bare.features, features);
    assert.equal(bare.blockConfig, undefined);

    assert.equal(parseGeoJsonTransfer({ foo: 1 }), null);
});

test('bỏ feature sai geometry và feature trùng id; mapConfig sai kiểu bị loại', () => {
    const result = parseGeoJsonTransfer({
        features: [...features, point('dev-1', 'device', [1, 1]), { type: 'Feature', geometry: { type: 'LineString', coordinates: [] }, properties: {} }, null],
        mapConfig: { view: { latitude: 'abc', defaultZoom: '12' }, display: { iconSize: -5, showIconBackground: 'yes', incidentLineStyle: 'dotted' } },
    });
    assert.equal(result.features.length, features.length);
    assert.deepEqual(result.features[0].geometry.coordinates, [107.1, 10.8]);
    assert.equal(result.skipped, 2);
    assert.equal(result.duplicates, 1);
    assert.deepEqual(result.mapConfig, { view: { defaultZoom: 12 }, display: { incidentLineStyle: 'dotted' } });
});

test('bản đồ ảnh và bản đồ online không dùng chung toạ độ', () => {
    assert.equal(isSameCoordinateSystem('image', 'image'), true);
    assert.equal(isSameCoordinateSystem('online', 'offline'), true);
    assert.equal(isSameCoordinateSystem('image', 'online'), false);
    assert.equal(isSameCoordinateSystem(undefined, 'image'), true);
});

test('mapConfig.display giữ độ dày viền zone + màu đường định hướng hợp lệ, bỏ màu sai', () => {
  const parsed = parseGeoJsonTransfer({
    type: 'FeatureCollection',
    features: [],
    mapConfig: {
      display: {
        zoneBorderWidth: 2.5,
        flowLineColors: { '*': '#ffffff', LANE_A: '#2563EB', LANE_B: 'rgba(1, 2, 3, 0.5)', BAD: 'red;background:url(x)', EMPTY: '' },
      },
    },
  });
  assert.equal(parsed.mapConfig?.display?.zoneBorderWidth, 2.5);
  assert.deepEqual(parsed.mapConfig?.display?.flowLineColors, { '*': '#ffffff', LANE_A: '#2563EB', LANE_B: 'rgba(1, 2, 3, 0.5)' });
});
