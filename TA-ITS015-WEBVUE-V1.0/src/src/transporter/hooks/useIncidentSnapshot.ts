import { onMounted, onUnmounted, onActivated, onDeactivated, watch } from 'vue';
import { fetchSseApi, logSseDiagnostic } from '/@/utils/fetch-utils';
// SharedWorker owns connection sharing and snapshot replay, keyed by URL and authentication.
export function useIncidentSnapshot(query: () => Record<string, any> | null, receive: (data: any) => void | Promise<void>, kind: 'list' | 'icons' = 'list') {
  let abort: AbortController | undefined;
  let stopWatch = () => {};
  let started = false;
  const start = () => {
    if (started) return;
    started = true;
    stopWatch = watch(() => {
      const value = query();
      return value == null ? null : JSON.stringify(Object.entries(value).filter(([,v]) => v != null).sort(([a],[b]) => a.localeCompare(b)));
    }, (encoded, previous) => {
      const before = Object.fromEntries(previous == null ? [] : JSON.parse(previous));
      const after = Object.fromEntries(encoded == null ? [] : JSON.parse(encoded));
      const changedKeys = [...new Set([...Object.keys(before), ...Object.keys(after)])].filter(key => before[key] !== after[key]);
      logSseDiagnostic('incident-subscription-query', { kind, reason: previous == null ? 'initial' : 'query-changed', changedKeys });
      abort?.abort('query-changed');
      if (encoded == null) return;
      const current = abort = new AbortController();
      const entries = JSON.parse(encoded) as [string, any][];
      const url = ('/api/tms/tmsincidentsnapshot/' + (kind === 'icons' ? 'iconstream' : 'stream') + '?') + new URLSearchParams(entries.map(([k,v]) => [k,String(v)])).toString();
      let busy = false, latest: any, pending = false;
      fetchSseApi<any>(url, { event: 'incident', shareAcrossTabs: true, signal: current.signal,
        reconnect: true, idleTimeout: 60000, retryServerErrors: true, customCatch: true,
        onResult: async data => {
          if (current.signal.aborted || data?.heartbeat || (kind === 'icons' ? !Array.isArray(data?.icons) : !Array.isArray(data?.items))) return;
          latest = { ...data, clientQuery: Object.fromEntries(entries) }; pending = true;
          if (busy) return;
          busy = true;
          try { while (pending && !current.signal.aborted) { pending = false; try { await receive(latest); } catch (error) { if (!current.signal.aborted) console.warn('[Incident snapshot render]', error); } } }
          finally { busy = false; }
        }, onError: error => { if (!current.signal.aborted) console.warn('[Incident snapshot]', error); },
      }).done.catch(error => { if (!current.signal.aborted) console.warn('[Incident snapshot]', error); });
    }, { immediate: true });
  };
  const stop = (reason: string) => {
    if (!started) return;
    logSseDiagnostic('incident-subscription-stop', { kind, reason });
    stopWatch(); abort?.abort(reason); started = false;
  };
  onMounted(start);
  onActivated(start);
  onDeactivated(() => stop('deactivated'));
  onUnmounted(() => stop('unmounted'));
}
