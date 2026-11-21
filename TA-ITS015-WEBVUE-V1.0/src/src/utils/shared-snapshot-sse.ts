/** Bridges a tab's fetch-style stream to a same-origin SharedWorker. */
export function fetchSharedSnapshot(url: string, headers: Record<string, string>, signal: AbortSignal,
 log: (event: string, details?: Record<string, unknown>) => void): Promise<Response> {
 if (typeof SharedWorker === 'undefined') {
  log('shared-unsupported');
  // Explicit fallback for older browsers. It retains the HTTP/1 connection limit.
  return fetch(url, { headers, signal, cache: 'no-store' });
 }
 return new Promise((resolve, reject) => {
  if (signal.aborted) { reject(new DOMException('Aborted', 'AbortError')); return; }
  const worker = new SharedWorker(new URL('./snapshot-sse.worker.ts', import.meta.url), { type: 'module', name: 'its-snapshot-sse-v1' });
  const port = worker.port;
  let controller: ReadableStreamDefaultController<Uint8Array>;
  let stopped = false, receivedHeaders = false;
  let connectionId: string | undefined;
  const stop = (error?: Error, reason = 'consumer-abort') => {
   if (stopped) return;
   stopped = true;
   signal.removeEventListener('abort', abort);
   window.removeEventListener('pagehide', pagehide);
   window.removeEventListener('storage', changedIdentity);
   log('shared-close', { connectionId, reason });
   port.postMessage({ type: 'close', reason });
   port.close();
   if (error) { controller.error(error); if (!receivedHeaders) reject(error); }
   else { controller.close(); if (!receivedHeaders) reject(new Error('Shared SSE closed before headers')); }
  };
  const abort = () => {
   const reason = typeof signal.reason === 'string' && ['query-changed', 'deactivated', 'unmounted', 'idle-timeout'].includes(signal.reason)
    ? signal.reason : 'consumer-abort';
   stop(new DOMException('Aborted', 'AbortError'), reason);
  };
  const pagehide = () => stop(new DOMException('Aborted', 'AbortError'), 'pagehide');
  // Reconnect through fetchSSE so it rebuilds current credentials after login/logout/refresh.
  const changedIdentity = (event: StorageEvent) => {
   if (event.key === null || /:(access-token|x-access-token)$/.test(event.key)) stop(new Error('SSE authentication changed'), 'authentication-changed');
  };
  const body = new ReadableStream<Uint8Array>({
   start(value) { controller = value; },
   cancel() { abort(); },
  });
  signal.addEventListener('abort', abort, { once: true });
  window.addEventListener('pagehide', pagehide, { once: true });
  window.addEventListener('storage', changedIdentity);
  worker.onerror = () => stop(new Error('Shared SSE worker failed to start'));
  port.onmessageerror = () => stop(new Error('Shared SSE message failed'));
  port.onmessage = (event: MessageEvent) => {
   if (stopped) return;
   const message = event.data;
   switch (message?.type) {
    case 'joined': connectionId = message.connectionId; log('shared-joined', { connectionId, subscribers: message.subscribers, newConnection: message.newConnection }); break;
    case 'diagnostic': log(message.event, { connectionId: message.connectionId, reason: message.reason, subscribers: message.subscribers }); break;
    case 'ping': port.postMessage({ type: 'pong' }); break;
    case 'headers':
     receivedHeaders = true;
     resolve(new Response([204, 205, 304].includes(message.status) ? null : body, { status: message.status, headers: message.headers }));
     break;
    case 'chunk': controller.enqueue(message.bytes); port.postMessage({ type: 'ack' }); break;
    case 'end': stop(undefined, 'server-ended'); break;
    case 'error': stop(new Error(message.message), 'transport-error'); break;
   }
  };
  port.start();
  port.postMessage({ type: 'open', url: new URL(url, window.location.href).href, headers });
 });
}
