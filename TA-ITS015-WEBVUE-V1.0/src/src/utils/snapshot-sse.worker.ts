// Snapshot-only SSE transport. Each port is one subscriber, not the owner of the socket.
// Group by full request (including authentication). Never log the group key or payload.
export {};
type Client = { port: MessagePort; group?: Group; seen: number; pending: number };
type Group = {
 key: string; connectionId: string; aliases: Set<string>; clients: Set<Client>; abort: AbortController;
 headers?: { status: number; headers: [string, string][] }; latest?: string;
};
const groups = new Map<string, Group>();
const clients = new Set<Client>();
const encoder = new TextEncoder();
const MAX_FRAME = 16 * 1024 * 1024;
function send(client: Client, value: unknown) {
 try { client.port.postMessage(value); } catch { detach(client); }
}
function diagnostic(group: Group, event: string, reason: string) {
 const message = { type: 'diagnostic', event, reason, connectionId: group.connectionId, subscribers: group.clients.size };
 console.info('[SSE worker diagnostic]', JSON.stringify(message));
 for (const client of [...group.clients]) send(client, message);
}
function detach(client: Client, reason = 'port-error') {
 const group = client.group;
 client.group = undefined;
 clients.delete(client);
 client.port.close();
 if (group) {
  group.clients.delete(client);
  diagnostic(group, 'shared-detached', reason);
  if (!group.clients.size) {
   diagnostic(group, 'shared-socket-abort', reason);
   group.abort.abort();
   for (const key of group.aliases) if (groups.get(key) === group) groups.delete(key);
   group.latest = undefined;
  }
 }
}
function end(group: Group, error?: string) {
 for (const key of group.aliases) if (groups.get(key) === group) groups.delete(key);
 for (const client of [...group.clients]) {
  send(client, { type: error ? 'error' : 'end', message: error });
  detach(client, error ? 'transport-error' : 'server-ended');
 }
}
function deliver(client: Client, frame: string) {
 if (++client.pending > 64) { send(client, { type: 'error', message: 'SSE consumer is too slow' }); detach(client, 'slow-consumer'); return; }
 send(client, { type: 'chunk', bytes: encoder.encode(frame) });
}
async function run(group: Group, url: string, headers: [string, string][]) {
 try {
  const requestHeaders = new Headers(headers);
  // Add correlation after grouping: a new tab must not create its own socket just for logging.
  requestHeaders.set('X-SSE-Diagnostic-Id', group.connectionId);
  const response = await fetch(url, { headers: requestHeaders, signal: group.abort.signal, cache: 'no-store', credentials: 'omit' });
  group.headers = { status: response.status, headers: [...response.headers.entries()] };
  const refreshed = response.headers.get('access-token');
  if (response.ok && refreshed) {
   const nextHeaders = new Headers(headers);
   nextHeaders.set('Authorization', 'Bearer ' + refreshed);
   nextHeaders.delete('X-Authorization');
   const alias = JSON.stringify([url, [...nextHeaders.entries()].sort(([a], [b]) => a.localeCompare(b))]);
   if (!groups.has(alias)) { group.aliases.add(alias); groups.set(alias, group); }
  }
  for (const client of group.clients) send(client, { type: 'headers', ...group.headers });
  // Refresh headers are a one-time operation; a later subscriber must not restore old tokens.
  group.headers.headers = group.headers.headers.filter(([key]) => !['access-token', 'x-access-token'].includes(key.toLowerCase()));
  if (!response.ok) {
   const body = await response.text();
   for (const client of group.clients) deliver(client, body);
   end(group); return;
  }
  if (!response.body) throw new Error('SSE response has no body');
  const reader = response.body.getReader();
  const decoder = new TextDecoder();
  let buffer = '';
  try {
   while (!group.abort.signal.aborted) {
    const { done, value } = await reader.read();
    if (done) break;
    buffer += decoder.decode(value, { stream: true });
    let delimiter: RegExpExecArray | null;
    // Emit whole SSE frames so a subscriber joining mid-chunk never receives partial JSON.
    while ((delimiter = /\r?\n\r?\n/.exec(buffer))) {
     const frame = buffer.slice(0, delimiter.index + delimiter[0].length);
     buffer = buffer.slice(delimiter.index + delimiter[0].length);
     if (frame.length > MAX_FRAME) throw new Error('SSE frame exceeds size limit');
     if (/(^|\n)data:/.test(frame) && !/"heartbeat"\s*:\s*true/.test(frame)) group.latest = frame;
     for (const client of [...group.clients]) deliver(client, frame);
    }
    if (buffer.length > MAX_FRAME) throw new Error('SSE frame exceeds size limit');
   }
  } finally { await reader.cancel().catch(() => {}); }
  end(group);
 } catch {
  // Do not serialize fetch errors that could contain request credentials.
  end(group, 'Shared SSE connection ended');
 }
}
(globalThis as any).onconnect = (event: MessageEvent) => {
 const port = event.ports[0];
 const client: Client = { port, seen: Date.now(), pending: 0 };
 clients.add(client);
 port.onmessage = (event: MessageEvent) => {
  const message = event.data;
  client.seen = Date.now();
  if (message?.type === 'close') {
   const reason = ['query-changed', 'deactivated', 'unmounted', 'idle-timeout', 'consumer-abort', 'pagehide', 'authentication-changed', 'server-ended', 'transport-error'].includes(message.reason)
    ? message.reason : 'consumer-abort';
   detach(client, reason); return;
  }
  if (message?.type === 'ack') { client.pending = Math.max(0, client.pending - 1); return; }
  if (message?.type !== 'open' || client.group) return;
  const url = new URL(message.url);
  if (!['http:', 'https:'].includes(url.protocol)) { detach(client); return; }
  const headers = [...new Headers(message.headers).entries()].sort(([a], [b]) => a.localeCompare(b));
  // Exact-token isolation: independent logins, tenants, languages and filters never share snapshots.
  const key = JSON.stringify([url.href, headers]);
  let group = groups.get(key);
  const fresh = !group;
  if (!group) {
   const connectionId = 'sse-' + Array.from(crypto.getRandomValues(new Uint8Array(16)), byte => byte.toString(16).padStart(2, '0')).join('');
   group = { key, connectionId, aliases: new Set([key]), clients: new Set(), abort: new AbortController() };
   groups.set(key, group);
  }
  client.group = group;
  group.clients.add(client);
  send(client, { type: 'joined', connectionId: group.connectionId, subscribers: group.clients.size, newConnection: fresh });
  if (group.headers) {
   send(client, { type: 'headers', ...group.headers });
   if (group.latest) deliver(client, group.latest);
  }
  if (fresh) void run(group, url.href, headers);
 };
 port.start();
};
// pagehide normally detaches immediately. Reap ports from killed/frozen tabs as a fallback.
setInterval(() => {
 for (const client of [...clients]) {
  if (Date.now() - client.seen > 120000) { detach(client, 'subscriber-expired'); continue; }
  send(client, { type: 'ping' });
 }
}, 15000);
