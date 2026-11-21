import assert from 'node:assert/strict';
import fs from 'node:fs';
import vm from 'node:vm';
import { webcrypto } from 'node:crypto';
import { createRequire } from 'node:module';

const require = createRequire(new URL('../src/package.json', import.meta.url));
const ts = require('typescript');
const vue = require('@vue/compiler-sfc');
const root = new URL('../src/src/', import.meta.url);
const load = path => fs.readFileSync(new URL(path, root), 'utf8');
const compile = source => {
  const result = ts.transpileModule(source, { reportDiagnostics: true,
    compilerOptions: { target: ts.ScriptTarget.ES2022, module: ts.ModuleKind.CommonJS } });
  assert.equal(result.diagnostics?.filter(row => row.category === ts.DiagnosticCategory.Error).length ?? 0, 0);
  return result.outputText;
};
for (const path of ['utils/shared-snapshot-sse.ts', 'utils/snapshot-sse.worker.ts', 'utils/fetch-utils.ts', 'transporter/hooks/useIncidentSnapshot.ts']) compile(load(path));
const component = vue.parse(load('views/bigScreen/trafficInfo/component/centerInfo.vue'));
assert.equal(component.errors.length, 0);
compile(component.descriptor.scriptSetup.content);

// Synthetic ports and fetch: this test never opens the browser or calls the application/API.
const requests = [], workerLogs = [];
let sweep, now = 1000;
class TestDate extends Date { static now() { return now; } }
class Port {
  messages = [];
  postMessage(message) { this.messages.push(message); }
  start() {}
  close() {}
  receive(message) { this.onmessage({ data: message }); }
}
const sandbox = vm.createContext({ exports: {}, Headers, URL, TextEncoder, TextDecoder, Uint8Array, AbortController,
  crypto: webcrypto, Date: TestDate, console: { info: (_, row) => workerLogs.push(JSON.parse(row)) },
  setInterval: callback => { sweep = callback; return 1; },
  fetch: async (url, options) => {
    let controller;
    const body = new ReadableStream({ start(value) { controller = value; } });
    options.signal.addEventListener('abort', () => controller.error(new DOMException('Aborted', 'AbortError')), { once: true });
    requests.push({ url, options, controller });
    return new Response(body, { headers: { 'Content-Type': 'text/event-stream' } });
  }
});
vm.runInContext(compile(load('utils/snapshot-sse.worker.ts')), sandbox);
const settle = async () => { for (let i = 0; i < 4; i++) await new Promise(resolve => setImmediate(resolve)); };
const open = (token = 'private-token') => {
  const port = new Port();
  sandbox.onconnect({ ports: [port] });
  port.receive({ type: 'open', url: 'https://fixture.invalid/stream?Name=private-query', headers: { Authorization: `Bearer ${token}` } });
  return port;
};
const first = open();
await settle();
const second = open();
await settle();
assert.equal(requests.length, 1, 'Adding a tab split the shared socket.');
const firstJoin = first.messages.find(row => row.type === 'joined');
const secondJoin = second.messages.find(row => row.type === 'joined');
assert.equal(firstJoin.connectionId, secondJoin.connectionId);
assert.equal(secondJoin.newConnection, false);
assert.equal(requests[0].options.headers.get('X-SSE-Diagnostic-Id'), firstJoin.connectionId);
first.receive({ type: 'close', reason: 'query-changed' });
assert.equal(requests[0].options.signal.aborted, false, 'Closing one subscriber cancelled another tab.');
assert.ok(second.messages.some(row => row.event === 'shared-detached' && row.reason === 'query-changed' && row.subscribers === 1));
requests[0].controller.enqueue(new TextEncoder().encode('event: incident\ndata: {"items":[]}\n\n'));
await settle();
assert.ok(second.messages.some(row => row.type === 'chunk'), 'Remaining tab stopped receiving frames.');
second.receive({ type: 'close', reason: 'pagehide' });
await settle();
assert.equal(requests[0].options.signal.aborted, true);
assert.ok(workerLogs.some(row => row.event === 'shared-socket-abort' && row.reason === 'pagehide' && row.subscribers === 0));
open('other-private-token');
await settle();
now += 121000;
sweep();
await settle();
assert.ok(workerLogs.some(row => row.event === 'shared-socket-abort' && row.reason === 'subscriber-expired'));
assert.ok(workerLogs.every(row => !JSON.stringify(row).includes('private-token') && !JSON.stringify(row).includes('private-query')));

const bridgeLogs = [];
let bridgePort;
const events = new Map();
const bridge = vm.createContext({ exports: {}, URL, ReadableStream, Response, DOMException,
  window: { location: { href: 'https://fixture.invalid/' },
    addEventListener: (name, listener) => events.set(name, listener),
    removeEventListener: (name, listener) => { if (events.get(name) === listener) events.delete(name); } },
  SharedWorker: class { constructor() { this.port = bridgePort = new Port(); } }
});
vm.runInContext(compile(load('utils/shared-snapshot-sse.ts')).replaceAll('import.meta.url', '"https://fixture.invalid/bridge.js"'), bridge);
const controller = new AbortController();
const responsePromise = bridge.exports.fetchSharedSnapshot('/stream', {}, controller.signal, (event, details) => bridgeLogs.push({ event, ...details }));
bridgePort.onmessage({ data: { type: 'joined', connectionId: firstJoin.connectionId, subscribers: 1, newConnection: true } });
bridgePort.onmessage({ data: { type: 'headers', status: 200, headers: [] } });
const response = await responsePromise;
controller.abort('idle-timeout');
await assert.rejects(response.body.getReader().read(), { name: 'AbortError' });
assert.ok(bridgeLogs.some(row => row.event === 'shared-close' && row.reason === 'idle-timeout' && row.connectionId === firstJoin.connectionId));
assert.ok(bridgePort.messages.some(row => row.type === 'close' && row.reason === 'idle-timeout'));
assert.equal(events.size, 0, 'Closing a subscriber leaked page listeners.');
console.log('PASS FE diagnostics: shared join, one-tab close, last-tab abort, expired subscriber, close reason and credential exclusion.');
