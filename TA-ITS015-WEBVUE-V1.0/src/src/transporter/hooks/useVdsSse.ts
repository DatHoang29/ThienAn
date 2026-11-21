import { zoneStatusStreamRequest } from '/@/services/tms/traffic-status';
import { onActivated, onDeactivated, onMounted, onUnmounted } from 'vue';
import { VdsTrafficCountApiAxiosParamCreator, VdsTrafficStatisticApiAxiosParamCreator } from '/@/api-services/vds';
import { buildFetchHeaders, fetchSseApi } from '/@/utils/fetch-utils';

type Kind = 'count' | 'zone' | 'statistic';
type Listener = (data: any, kind: Kind) => void | Promise<void>;
type Stream = { abort: AbortController; listeners: Set<Listener>; latest?: unknown; hasLatest: boolean };
// Separate auth scopes locally; the SharedWorker additionally separates by full URL and headers.
const streams = new Map<string, Stream>();
const authScope = () => {
  const headers = buildFetchHeaders();
  return JSON.stringify([headers.Authorization ?? '', headers['X-Authorization'] ?? '', headers['Accept-Language'] ?? '']);
};
const requestFor = (kind: Kind) => kind === 'count'
  ? VdsTrafficCountApiAxiosParamCreator().apiVdsVdstrafficcountStreamGet(2, 60)
  : kind === 'zone'
    ? zoneStatusStreamRequest()
    : VdsTrafficStatisticApiAxiosParamCreator().apiVdsVdstrafficstatisticStreamGet(2, 60);

function subscribe(kind: Kind, listener: Listener) {
  const key = JSON.stringify([kind, authScope()]);
  let entry = streams.get(key);
  if (!entry) {
    entry = { abort: new AbortController(), listeners: new Set(), hasLatest: false };
    streams.set(key, entry);
    const current = entry;
    void requestFor(kind).then(request => {
      if (current.abort.signal.aborted) return;
      return fetchSseApi<any>(request.url, {
        shareAcrossTabs: true, event: kind, signal: current.abort.signal, reconnect: true, idleTimeout: 60000,
        retryServerErrors: true, customCatch: true,
        onResult: data => {
          current.latest = data;
          current.hasLatest = true;
          void Promise.allSettled([...current.listeners].map(callback => Promise.resolve().then(() => {
            if (!current.abort.signal.aborted && current.listeners.has(callback)) return callback(data, kind);
          })));
        },
        onError: error => { if (!current.abort.signal.aborted) console.warn(`[VDS SSE ${kind}]`, error); },
      }).done;
    }).catch(error => { if (!current.abort.signal.aborted) console.warn(`[VDS SSE ${kind}]`, error); });
  }
  entry.listeners.add(listener);
  if (entry.hasLatest) {
    const current = entry;
    void Promise.resolve().then(() => {
      if (!current.abort.signal.aborted && current.listeners.has(listener)) return listener(current.latest, kind);
    });
  }
  const current = entry;
  return () => {
    current.listeners.delete(listener);
    if (!current.listeners.size) {
      current.abort.abort();
      current.latest = undefined;
      if (streams.get(key) === current) streams.delete(key);
    }
  };
}

export function useVdsSse(kinds: Kind[], handler: Listener) {
  let cleanups: (() => void)[] = [];
  let active = false, busy = false;
  let scope = '';
  let identityTimer: ReturnType<typeof setInterval> | undefined;
  const pending = new Map<Kind, unknown>();
  const listener: Listener = async (data, kind) => {
    pending.set(kind, data);
    if (busy) return;
    busy = true;
    try {
      while (active && pending.size) {
        const [nextKind, nextData] = pending.entries().next().value as [Kind, unknown];
        pending.delete(nextKind);
        await handler(nextData, nextKind);
      }
    } finally { busy = false; }
  };
  const start = () => {
    if (active) return;
    active = true;
    scope = authScope();
    cleanups = kinds.map(kind => subscribe(kind, listener));
    // Local.set does not dispatch a storage event to its own tab. Detect token rotation/logout here.
    identityTimer = setInterval(() => {
      if (authScope() !== scope) { stop(); start(); }
    }, 1000);
  };
  const stop = () => {
    if (!active) return;
    active = false;
    if (identityTimer) clearInterval(identityTimer);
    identityTimer = undefined;
    pending.clear();
    cleanups.forEach(unsubscribe => unsubscribe());
    cleanups = [];
  };
  onMounted(start);
  onActivated(start);
  onDeactivated(stop);
  onUnmounted(stop);
}
