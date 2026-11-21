import { onMounted } from 'vue';
import { useVdsSse } from './useVdsSse';

/** Zone snapshots now arrive over module SSE; existing consumers refresh their scoped API data. */
export function useTransporterVdsZone(handler: (payload?: any) => void | Promise<void>, options: { immediate?: boolean } = {}) {
    useVdsSse(['zone'], handler);
    onMounted(() => { if (options.immediate) void handler(); });
}