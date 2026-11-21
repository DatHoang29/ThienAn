const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const ts = require('typescript');

function harness(query, receive, kind) {
  const lifecycle = {}, requests = [];
  let update;
  const vue = Object.fromEntries(['onMounted', 'onUnmounted', 'onActivated', 'onDeactivated']
    .map(name => [name, fn => { lifecycle[name] = fn; }]));
  vue.watch = (source, callback) => {
    update = () => callback(source());
    update();
    return () => { update = undefined; };
  };
  const source = fs.readFileSync(path.join(__dirname, '../src/transporter/hooks/useIncidentSnapshot.ts'), 'utf8');
  const compiled = ts.transpileModule(source, { compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 } }).outputText;
  const exports = {};
  vm.runInNewContext(compiled, {
    exports, AbortController, URLSearchParams, console,
    require: name => name === 'vue' ? vue : {
      fetchSseApi: (url, options) => { requests.push({ url, options }); return { done: Promise.resolve() }; }
    }
  });
  exports.useIncidentSnapshot(query, receive, kind);
  lifecycle.onMounted();
  return { lifecycle, requests, update: () => update?.() };
}

test('list snapshot ignores old filters, heartbeat and old page contract', async () => {
  let query = { includeEvents: true, includeIncidents: true };
  const received = [];
  const h = harness(() => query, data => received.push(data));
  const old = h.requests[0];
  assert.equal(old.options.shareAcrossTabs, true);
  assert.equal(old.options.reconnect, true);
  query = { includeEvents: false, includeIncidents: true };
  h.update();
  assert.equal(old.options.signal.aborted, true);
  await old.options.onResult({ items: [{ id: 'stale' }] });
  const current = h.requests[1];
  await current.options.onResult({ heartbeat: true });
  await current.options.onResult({ page: { records: [] } });
  await current.options.onResult({ items: [{ id: 'new' }] });
  assert.equal(received.length, 1);
  assert.equal(received[0].items[0].id, 'new');
  assert.equal(received[0].clientQuery.includeEvents, false);
});

test('keep-alive deactivation aborts and activation opens exactly one stream', async () => {
  const received = [];
  const h = harness(() => ({}), data => received.push(data));
  h.lifecycle.onActivated();
  assert.equal(h.requests.length, 1);
  h.lifecycle.onDeactivated();
  assert.equal(h.requests[0].options.signal.aborted, true);
  await h.requests[0].options.onResult({ items: [] });
  assert.equal(received.length, 0);
  h.lifecycle.onActivated();
  assert.equal(h.requests.length, 2);
  h.lifecycle.onUnmounted();
  assert.equal(h.requests[1].options.signal.aborted, true);
});

test('map uses icon stream and null query stops it in edit mode', async () => {
  let query = {};
  const received = [];
  const h = harness(() => query, data => received.push(data), 'icons');
  assert.match(h.requests[0].url, /iconstream/);
  await h.requests[0].options.onResult({ icons: [{ incidentId: 'i' }] });
  assert.equal(received.length, 1);
  query = null;
  h.update();
  assert.equal(h.requests[0].options.signal.aborted, true);
  assert.equal(h.requests.length, 1);
});
