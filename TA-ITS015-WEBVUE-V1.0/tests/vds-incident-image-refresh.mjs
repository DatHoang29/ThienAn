import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import vm from 'node:vm';
import { createRequire } from 'node:module';
import { fileURLToPath } from 'node:url';

const root = fileURLToPath(new URL('../src/', import.meta.url));
const require = createRequire(path.join(root, 'package.json'));
const vue = require('vue');
const { parse, compileScript } = require('@vue/compiler-sfc');
const ts = require('typescript');
const filename = path.join(root, 'src/views/report/tms/incident/detail.vue');
const { descriptor } = parse(fs.readFileSync(filename, 'utf8'), { filename });
const compiled = compileScript(descriptor, { id: 'incident-image-refresh-regression' });
const source = ts.transpileModule(compiled.content, {
  compilerOptions: { target: ts.ScriptTarget.ES2022, module: ts.ModuleKind.CommonJS },
}).outputText;

const registered = '2026-10-01T03:00:00Z';
const pending = type => ({ pictureType: type, imageSource: 'vds', status: 'PendingCapture', exists: false, equipmentId: 'raw-B' });
const ready = type => ({ ...pending(type), status: 'Ready', exists: true, contentType: 'image/png', base64: 'cG5n' });
const camera = (type, id = 'cctv-1') => ({ pictureType: type, equipmentId: id, exists: true, url: `/camera-${id}-${type}.jpeg` });
const deferred = () => {
  let resolve;
  const promise = new Promise(done => { resolve = done; });
  return { promise, resolve };
};

function harness(images, options = {}) {
  let now = Date.parse(registered);
  let nextTimer = 1;
  const timers = new Map();
  const calls = [];
  const watches = [];
  const cleanup = [];
  const errors = [];
  const route = { query: {} };
  const info = { source: 'vds', validatedDate: registered, equipmentId: ['cctv-1'], ...options.info };
  const cameraList = options.cameras ?? [{ id: 'cctv-1', equipmentTypeId: 'cctv-type' }];
  const messages = [];
  const api = {
    async apiTmsreportTmsincidentreportRenderdetailPost() { return { data: options.report ?? "%PDF-1.7 test" }; },
    async apiTmsTmsincidentGetimagesGet(id, type) {
      calls.push({ id, type });
      return { data: { result: await images(type, calls.filter(call => call.type === type).length) } };
    },
    async apiTmsTmsincidentSinglePost() { return { data: { result: info } }; },
    async apiTmsTmsequipmenttypeListGet() { return { data: { result: [{ id: 'cctv-type', code: 'CCTV' }] } }; },
    async apiTmsTmsequipmentListbyidsGet() { return { data: { result: cameraList } }; },
  };
  const modules = {
    '/@/views/tms/incident/component/incidentVehicleInfo.vue': { default: {} },
    vue: { ...vue, watch: (source, callback) => { watches.push({ source, callback }); }, onBeforeUnmount: callback => cleanup.push(callback) },
    'vue-router': { useRoute: () => route },
    'vue-i18n': { useI18n: () => ({ t: key => key }) },
    'element-plus': { ElMessage: { success() {}, error(message) { messages.push(message); } } },
    '/@/utils/axios-utils': { getAPI: () => api },
    '@vueuse/core': { useDateFormat: () => vue.ref('20261001'), useNow: () => vue.ref(now) },
    'file-saver': { saveAs() {} },
    '/@/api-services/tmsReport': { TmsIncidentReportApi: {} },
    '/@/api-services/tms': { TmsIncidentApi: {}, TmsEquipmentApi: {}, TmsEquipmentTypeApi: {} },
    '/@/stores/its/commonVarsAndFuncs': { useCommonVarsAndFuncs: () => ({ serverHost: 'https://images.example.test', getCctvEquipmentTypeList: async () => [], isCCTVEquipmentType: id => id === 'cctv-type' }) },
    '/@/types/enums/baseEnum': { DateTimeFormatEnum: {} },
    '/@/stores/baseConfig': { useBaseConfig: () => ({ getConfigDataByCode: () => [] }) },
    '/@/types/enums': { BaseConfigTypeEnum: {} },
  };
  class FakeDate extends Date { static now() { return now; } }
  const exports = {};
  const context = vm.createContext({
    exports, require: name => {
      assert.ok(name in modules, `unexpected import ${name}`);
      return modules[name];
    },
    Blob, Date: FakeDate, console: { error: (...args) => errors.push(args) },
    URL: { revokeObjectURL() {}, createObjectURL: () => 'blob:test' },
    setTimeout: (callback, delay) => { const id = nextTimer++; timers.set(id, { callback, at: now + delay }); return id; },
    clearTimeout: id => timers.delete(id),
  });
  vm.runInContext(source, context, { filename });
  const component = exports.default.setup({}, { expose() {} });
  component.activeTab.value = 'images';
  component.state.incidentInfo = info;
  component.state.cameraList = cameraList;
  const tabWatch = watches.find(watch => watch.source === component.activeTab);
  const routeWatch = watches.find(watch => typeof watch.source === 'function');
  return {
    component, calls, timers, errors, messages,
    load: () => component.loadIncidentImages('incident-A', 0),
    close() { component.activeTab.value = 'summary'; tabWatch.callback('summary'); },
    clearRoute() { routeWatch.callback(''); },
    unmount() { cleanup.forEach(callback => callback()); },
    async advance(ms) {
      const target = now + ms;
      while (true) {
        const entry = [...timers.entries()].sort((a, b) => a[1].at - b[1].at).find(([, timer]) => timer.at <= target);
        if (!entry) break;
        timers.delete(entry[0]);
        now = entry[1].at;
        await entry[1].callback();
      }
      now = target;
    },
  };
}

let checks = 0;
async function test(name, run) { await run(); checks++; console.log(`PASS: ${name}`); }

await test('Pending VDS stage becomes visible without reopening the tab or refetching registration/finish', async () => {
  const h = harness((type, count) => type === 2 ? [count === 1 ? pending(2) : ready(2)] : type === 1 ? [ready(1)] : []);
  await h.load();
  assert.equal(h.component.state.images.filter(image => image.imageSource === 'vds' && [2, 3].includes(image.pictureType))[0].exists, false);
  await h.advance(5000);
  assert.equal(h.component.state.images.filter(image => image.imageSource === 'vds' && [2, 3].includes(image.pictureType))[0].exists, true);
  assert.equal(h.component.toIncidentImageUrl(h.component.state.images.filter(image => image.imageSource === 'vds' && [2, 3].includes(image.pictureType))[0]), 'data:image/png;base64,cG5n');
  assert.deepEqual(h.calls.map(call => call.type), [1, 2, 3, 4, 2]);
  assert.equal(h.timers.size, 0);
});

await test('Verification and processing refresh independently and preserve other stage images', async () => {
  const h = harness((type, count) => type === 2 ? [ready(2)] : type === 3 ? [count === 1 ? pending(3) : ready(3)] : [camera(type)],
    { info: { approvedDate: registered } });
  await h.load();
  await h.advance(5000);
  assert.equal(h.component.state.images.filter(image => image.imageSource === 'vds' && [2, 3].includes(image.pictureType)).length, 2);
  assert.ok(h.component.getIncidentImageUrl('cctv-1', 1));
  assert.ok(h.component.getIncidentImageUrl('cctv-1', 4));
  assert.deepEqual(h.calls.map(call => call.type), [1, 2, 3, 4, 3]);
});

await test('Camera fallback replaces a pending VDS card while registration alone still waits for full', async () => {
  const waiting = { ...pending(1), status: 'WaitingForSource', waitUntilUtc: '2026-10-01T03:10:00Z' };
  const h = harness((type, count) => type === 1 ? [waiting] : type === 2 ? [count === 1 ? pending(2) : camera(2)] : []);
  await h.load();
  await h.advance(5000);
  assert.equal(h.component.state.images.filter(image => image.imageSource === 'vds' && [2, 3].includes(image.pictureType)).length, 0);
  assert.ok(h.component.getIncidentImageUrl('cctv-1', 2));
  assert.equal(h.component.state.vdsRegistration.status, 'WaitingForSource');
  await h.advance(5000);
  assert.equal(h.calls.filter(call => call.type === 2).length, 2);
});

await test('Partial CCTV results refresh only the missing stage and retain already captured cameras', async () => {
  const h = harness((type, count) => type === 2 ? [camera(2), ...(count > 1 ? [camera(2, 'cctv-2')] : [{ ...pending(2), imageSource: 'cctv', equipmentId: 'cctv-2' }])] : [camera(type)],
    { cameras: [{ id: 'cctv-1' }, { id: 'cctv-2' }] });
  await h.load();
  await h.advance(5000);
  assert.ok(h.component.getIncidentImageUrl('cctv-1', 2));
  assert.ok(h.component.getIncidentImageUrl('cctv-2', 2));
  assert.equal(h.timers.size, 0);
});

await test('Stage polling stops after its fixed two-minute budget', async () => {
  const h = harness(type => type === 2 ? [pending(2)] : []);
  await h.load();
  await h.advance(120001);
  assert.equal(h.calls.filter(call => call.type === 2).length, 24);
  assert.equal(h.timers.size, 0);
  await h.advance(300000);
  assert.equal(h.calls.filter(call => call.type === 2).length, 24);
});

for (const operation of ['close', 'clearRoute', 'unmount']) {
  await test(`${operation} cancels timers and ignores an in-flight response`, async () => {
    const response = deferred();
    const h = harness((type, count) => type === 2 ? count === 1 ? [pending(2)] : response.promise : []);
    await h.load();
    const advancing = h.advance(5000);
    await Promise.resolve();
    h[operation]();
    response.resolve([ready(2)]);
    await advancing;
    assert.equal(h.component.state.images.filter(image => image.imageSource === 'vds' && [2, 3].includes(image.pictureType)).some(image => image.exists), false);
    assert.equal(h.timers.size, 0);
  });
}

await test('Reopening the same incident invalidates an older image request', async () => {
  const response = deferred();
  const h = harness((type, count) => type === 2 ? count === 1 ? [pending(2)] : count === 2 ? response.promise : [ready(2)] : []);
  await h.load();
  const advancing = h.advance(5000);
  await Promise.resolve();
  h.close();
  h.component.activeTab.value = 'images';
  await h.load();
  response.resolve([pending(2)]);
  await advancing;
  assert.equal(h.component.state.images.filter(image => image.imageSource === 'vds' && [2, 3].includes(image.pictureType))[0].exists, true);
  assert.equal(h.timers.size, 0);
});

await test('A slow refresh does not overlap with another refresh', async () => {
  const response = deferred();
  const h = harness((type, count) => type === 2 ? count === 1 ? [pending(2)] : response.promise : []);
  await h.load();
  const advancing = h.advance(5000);
  await Promise.resolve();
  assert.equal(h.calls.filter(call => call.type === 2).length, 2);
  assert.equal(h.timers.size, 0);
  response.resolve([ready(2)]);
  await advancing;
  assert.equal(h.timers.size, 0);
});

await test('A temporary stage API error preserves images and retries within the existing deadline', async () => {
  const h = harness((type, count) => {
    if (type !== 2) return [];
    if (count === 2) throw new Error('temporary');
    return [count === 1 ? pending(2) : ready(2)];
  });
  await h.load();
  await h.advance(5000);
  assert.equal(h.component.state.images.filter(image => image.imageSource === 'vds' && [2, 3].includes(image.pictureType))[0].status, 'PendingCapture');
  await h.advance(5000);
  assert.equal(h.component.state.images.filter(image => image.imageSource === 'vds' && [2, 3].includes(image.pictureType))[0].exists, true);
  assert.equal(h.errors.length, 1);
});

await test('Metadata finishing before the initial image load does not start an overlapping poll', async () => {
  const response = deferred();
  const h = harness(type => type === 2 ? response.promise : []);
  const loading = h.load();
  await h.component.loadDefaultData('incident-A', 0);
  assert.equal(h.timers.size, 0);
  response.resolve([pending(2)]);
  await loading;
  assert.equal(h.timers.size, 1);
});

await test('Non-VDS incidents and terminal missing VDS stages do not poll', async () => {
  const manual = harness(() => [], { info: { source: 'user', approvedDate: registered } });
  await manual.load();
  assert.equal(manual.timers.size, 0);
  const missing = harness(type => type === 2 ? [{ ...pending(2), status: 'MissingSource' }] : []);
  await missing.load();
  assert.equal(missing.timers.size, 0);
});

await test('Completed partial camera capture does not poll absent cameras', async () => {
  const h = harness(type => type === 2 ? [camera(2)] : [], { cameras: [{ id: 'cctv-1' }, { id: 'cctv-2' }] });
  await h.load();
  assert.equal(h.timers.size, 0);
  await h.advance(120000);
  assert.equal(h.calls.filter(call => call.type === 2).length, 1);
});

await test('A JSON error response cannot be displayed as a blank PDF', async () => {
  const h = harness(() => [], { report: '{"succeeded":false}' });
  await h.component.handleQuery('incident-A', 0);
  assert.equal(h.component.state.pdf, null);
  assert.deepEqual(h.messages, ['lz.message.tmsIncident.reportLoadFailed']);
  assert.equal(h.component.state.isProcessing, false);
});
await test('A PDF response remains visible without an error message', async () => {
  const h = harness(() => []);
  await h.component.handleQuery('incident-A', 0);
  assert.equal(h.component.state.pdf, 'blob:test');
  assert.equal(h.messages.length, 0);
});

console.log(`SUMMARY: ${checks} checks passed. Actual Vue setup with fake API/time; no live server or camera.`);
