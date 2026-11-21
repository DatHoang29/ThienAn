import { defineStore } from 'pinia';
import { onScopeDispose, ref, shallowRef } from 'vue';
import { cloneDeep } from 'lodash-es';
import { TmsEquipmentApi, TmsEquipmentTypeApi, TmsEventTypeApi, TmsIncidentApi, TmsWorkExecuteApi, TmsWorkExecuteHistoryApi, TmsZoneApi } from '/@/api-services/tms';
import type { IncidentChartInput, IncidentCountInput, TmsZonePositionInput } from '/@/api-services/tms';
import { SysOrgApi, SysUserApi } from '/@/api-services/system';
import type { SysOrg } from '/@/api-services/system';
import { accessTokenKey, axiosInstance, getAPI } from '/@/utils/axios-utils';
import { Local, Session } from '/@/utils/storage';
import mittBus from '/@/utils/mitt';
import { TransporterEvent } from '/@/transporter/constants/transporterEvent';
import { TmsIncidentStateEnum } from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';
import { useItsMediaServerStore } from '/@/stores/its/itsMediaServer';

type CacheGroup = 'catalog' | 'incident' | 'system';
type CacheEntry = { value: unknown; expiresAt: number; group: CacheGroup };
type PendingRequest = { promise: Promise<unknown>; controller: AbortController; group: CacheGroup };
const CATALOG_TTL = 5 * 60_000;
const INCIDENT_TTL = 60_000;
const MAX_ENTRIES = 128;

/** Shared TMS reference data and short-lived workflow data; never persist to localStorage. */
export const useTmsStore = defineStore('tmsStore', () => {
	const cache = shallowRef<Record<string, CacheEntry>>({});
	const catalogRevision = ref(0);
	const incidentRevision = ref(0);
	const pending = new Map<string, PendingRequest>();
	const baseConfig = useBaseConfig();
	const contextKey = () => `${Local.get(accessTokenKey) || Session.get('token') || ''}:${Local.get('themeConfig')?.globalI18n || ''}`;
	let context = contextKey();
	let lastResume = Date.now();
	let lastCatalogRead = 0;
	let configExpiresAt = Object.keys(baseConfig.configList ?? {}).length ? Date.now() + CATALOG_TTL : 0;
	let realtimeTimer: ReturnType<typeof setTimeout> | undefined;
	const realtimeGroups = new Set<CacheGroup>();
	let disposed = false;

	function invalidate(group?: CacheGroup) {
		if (group) realtimeGroups.delete(group); else realtimeGroups.clear();
		cache.value = Object.fromEntries(Object.entries(cache.value).filter(([, entry]) => group && entry.group !== group));
		for (const [key, request] of pending) {
			if (!group || request.group === group) { pending.delete(key); request.controller.abort(); }
		}
		if (!group || group === 'catalog') catalogRevision.value++;
		if (!group || group === 'incident') incidentRevision.value++;
	}
	function ensureContext() {
		// Match the credentials and language used by the Axios request interceptor.
		const next = contextKey();
		if (next !== context) { context = next; configExpiresAt = 0; invalidate(); }
	}
	async function read<T>(key: string, group: CacheGroup, ttl: number, loader: (signal: AbortSignal) => Promise<T>, force = false): Promise<T> {
		ensureContext();
		// Component disposal must not abort a request shared by another consumer.
		const current = pending.get(key);
		if (current) return cloneDeep(await current.promise) as T;
		const entry = cache.value[key];
		if (!force && entry && entry.expiresAt > Date.now()) return cloneDeep(entry.value) as T;
		if (entry) { const next = { ...cache.value }; delete next[key]; cache.value = next; }
		const controller = new AbortController();
		const requestContext = context;
		const request: PendingRequest = { controller, group, promise: Promise.resolve() };
		request.promise = loader(controller.signal).then(value => {
			ensureContext();
			if (controller.signal.aborted || pending.get(key) !== request || requestContext !== context) throw new Error('TMS cache invalidated');
			const now = Date.now();
			const entries = Object.entries(cache.value).filter(([cachedKey, cached]) => cachedKey !== key && cached.expiresAt > now);
			cache.value = { ...Object.fromEntries(entries.slice(-(MAX_ENTRIES - 1))), [key]: { value: cloneDeep(value), expiresAt: now + ttl, group } };
			if (group === 'catalog') lastCatalogRead = now;
			return value;
		}).catch(error => {
			// Existing consumers can join the fresh request after a cache invalidation.
			if (!disposed && controller.signal.aborted && requestContext === context) return read(key, group, ttl, loader);
			throw error;
		}).finally(() => { if (pending.get(key) === request) pending.delete(key); });
		pending.set(key, request);
		return cloneDeep(await request.promise) as T;
	}
	const getEventTypes = (force = false) => read('catalog:eventTypes', 'catalog', CATALOG_TTL, async signal => (await getAPI(TmsEventTypeApi).apiTmsTmseventtypeListGet(undefined, undefined, undefined, undefined, undefined, { signal })).data.result ?? [], force);
	const getZones = (force = false) => read('catalog:zones', 'catalog', CATALOG_TTL, async signal => (await getAPI(TmsZoneApi).apiTmsTmszoneListGet(undefined, undefined, undefined, undefined, undefined, undefined, undefined, undefined, undefined, { signal })).data.result ?? [], force);
	const getEquipment = (force = false) => read('catalog:equipment', 'catalog', CATALOG_TTL, async signal => (await getAPI(TmsEquipmentApi).apiTmsTmsequipmentListGet(undefined, undefined, undefined, undefined, undefined, undefined, undefined, undefined, undefined, undefined, { signal })).data.result ?? [], force);
	const getEquipmentTypes = (force = false) => read('catalog:equipmentTypes', 'catalog', CATALOG_TTL, async signal => (await getAPI(TmsEquipmentTypeApi).apiTmsTmsequipmenttypeListGet(undefined, undefined, undefined, undefined, undefined, undefined, { signal })).data.result ?? [], force);
	// Địa chỉ file server lấy từ store media dùng chung (giữ hàm cho code cũ)
	const getFileServer = (force = false) => useItsMediaServerStore().ensureLoaded(force);
	// Cache only display names, rather than the full user directory and personal details.
	function getHistoryUserNames(force = false) {
		return read('catalog:historyUserNames', 'catalog', CATALOG_TTL, async signal => {
			const response = await getAPI(SysUserApi).apiSystemSysuserListPost({}, { signal });
			const names: Record<string, string> = {};

			for (const user of response.data.result ?? []) {
				const name = user.realName?.trim() || user.name?.trim() || user.username?.trim();
				if (!name) continue;

				for (const value of [user.id, user.username]) {
					const key = value?.trim().toLowerCase();
					if (key) names[key] = name;
				}
			}
			return names;
		}, force);
	}

	function getHistoryOrganizationNames(force = false) {
		return read('catalog:historyOrganizationNames', 'catalog', CATALOG_TTL, async signal => {
			const response = await getAPI(SysOrgApi).apiSystemSysorgListGet('0', undefined, undefined, undefined, undefined, { signal });
			const names: Record<string, string> = {};

			function collectOrganizationNames(organizations: SysOrg[]) {
				for (const organization of organizations) {
					const key = organization.id?.trim().toLowerCase();
					const name = organization.name?.trim();
					if (key && name) names[key] = name;
					if (organization.children?.length) collectOrganizationNames(organization.children);
				}
			}

			collectOrganizationNames(response.data.result ?? []);
			return names;
		}, force);
	}
	const ensureBaseConfig = (force = false) => read('catalog:baseConfig', 'catalog', CATALOG_TTL, async () => {
		if (force || !configExpiresAt || configExpiresAt <= Date.now() || !Object.keys(baseConfig.configList ?? {}).length) await baseConfig.setConfigDataList();
		configExpiresAt = Date.now() + CATALOG_TTL;
		return true;
	}, force || !Object.keys(baseConfig.configList ?? {}).length);
	const canonicalIds = (ids: string) => [...new Set(ids.split(',').map(id => id.trim()).filter(Boolean))].sort();
	async function getZonesByIds(ids: string, force = false) {
		ensureContext();
		const keys = canonicalIds(ids);
		if (!keys.length) return [];
		const all = cache.value['catalog:zones'];
		if (!force && all?.expiresAt > Date.now()) {
			const found = (await getZones()).filter(item => keys.includes(String(item.id)));
			if (found.length === keys.length) return found;
		}
		return read(`catalog:zoneIds:${keys.join(',')}`, 'catalog', CATALOG_TTL, async signal => (await getAPI(TmsZoneApi).apiTmsTmszoneListzonebyidsGet(keys.join(','), { signal })).data.result ?? [], force);
	}
	async function getEquipmentByIds(ids: string, force = false) {
		ensureContext();
		const keys = canonicalIds(ids);
		if (!keys.length) return [];
		const all = cache.value['catalog:equipment'];
		if (!force && all?.expiresAt > Date.now()) {
			const found = (await getEquipment()).filter(item => keys.includes(String(item.id)));
			if (found.length === keys.length) return found;
		}
		return read(`catalog:equipmentIds:${keys.join(',')}`, 'catalog', CATALOG_TTL, async signal => (await getAPI(TmsEquipmentApi).apiTmsTmsequipmentListbyidsGet(keys.join(','), { signal })).data.result ?? [], force);
	}
	const getIncidentHistory = (id: string, state?: string | null, force = false) => read(`incident:history:${id}:${state || ''}`, 'incident', INCIDENT_TTL, async signal => {
		const response = state === TmsIncidentStateEnum.Finished
			? await getAPI(TmsWorkExecuteHistoryApi).apiTmsTmsworkexecutehistoryListbyconditionsPost({ incidentId: id }, { signal })
			: await getAPI(TmsWorkExecuteApi).apiTmsTmsworkexecuteListworkexecutebyconditionsPost({ incidentId: id }, { signal });
		return response.data.result ?? [];
	}, force);
	const queryKey = (params: object) => JSON.stringify(Object.fromEntries(Object.entries(params).filter(([, value]) => value !== undefined).sort(([left], [right]) => left.localeCompare(right))));
	const getZonesByPosition = (params: TmsZonePositionInput, force = false) => read(`catalog:position:${queryKey(params)}`, 'catalog', CATALOG_TTL, async signal => (await getAPI(TmsZoneApi).apiTmsTmszoneListzonebypositionPost(params, { signal })).data.result ?? [], force);
	const getEquipmentByZones = (ids: string[], force = false) => {
		const zoneIds = [...new Set(ids)].sort();
		if (!zoneIds.length) return Promise.resolve([]);
		return read(`catalog:equipmentZones:${zoneIds.join(',')}`, 'catalog', CATALOG_TTL, async signal => (await getAPI(TmsEquipmentApi).apiTmsTmsequipmentListbyzonelistPost({ zoneIds }, { signal })).data.result ?? [], force);
	};
	const getIncidentChart = (params: IncidentChartInput, force = false) => read(`incident:chart:${queryKey(params)}`, 'incident', INCIDENT_TTL, async signal => {
		const result = (await getAPI(TmsIncidentApi).apiTmsTmsincidentIncidentchartPost(params, { signal })).data.result;
		if (!result) throw new Error('Missing incident chart');
		return result;
	}, force);
	const getIncidentCount = (params: IncidentCountInput, force = false) => read(`incident:count:${queryKey(params)}`, 'incident', INCIDENT_TTL, async signal => {
		const result = (await getAPI(TmsIncidentApi).apiTmsTmsincidentCountincidentPost(params, { signal })).data.result;
		if (!result) throw new Error('Missing incident count');
		return result;
	}, force);

	// Successful writes invalidate dependent data even from the original TMS screens.
	const interceptor = axiosInstance.interceptors.response.use(response => {
		if (response.data?.result === false || response.data?.errors || (response.data?.code !== undefined && response.data.code !== 200)) return response;
		const path = (response.config.url || '').split('?')[0].toLowerCase();
		const match = path.match(/\/api\/tms\/tms(eventtype|equipmenttype|equipment|zoneequipment|zone|incident|workexecute|workexecutehistory)\/(add|update|delete|batch(?:add|update|delete)|save|import|execute|finish|approve|verify|cancel|automap|confirmmapping)/);
		if (match && response.config.method?.toLowerCase() !== 'get') invalidate(['incident', 'workexecute', 'workexecutehistory'].includes(match[1]) ? 'incident' : 'catalog');
		if (/\/api\/cfgsystem\/sysconfig(?:data|type)\/(add|update|delete|batch(?:add|update|delete)|import)/.test(path)) { configExpiresAt = 0; invalidate('catalog'); }
		if (response.config.method?.toLowerCase() !== 'get' && /\/api\/system\/sys(?:user|org)\/(?:add|update|delete|batch(?:add|update|delete)|import)/.test(path)) invalidate('catalog');
		return response;
	});
	function queueInvalidation(group: CacheGroup) {
		realtimeGroups.add(group);
		if (realtimeTimer) return;
		// Coalesce broadcasts so a burst does not trigger one refresh per message.
		realtimeTimer = setTimeout(() => {
			realtimeTimer = undefined;
			for (const queued of realtimeGroups) invalidate(queued);
			realtimeGroups.clear();
		}, 300);
	}
	const incidentChanged = () => queueInvalidation('incident');
	const catalogChanged = () => queueInvalidation('catalog');
	const refreshAll = () => { configExpiresAt = 0; invalidate(); };
	const reconnected = (status: { type?: string }) => { if (status?.type === 'reconnect') refreshAll(); };
	const resume = () => {
		if (document.visibilityState === 'hidden') return;
		ensureContext();
		const now = Date.now();
		if (now - lastResume > INCIDENT_TTL) incidentChanged();
		if ((lastCatalogRead && now - lastCatalogRead >= CATALOG_TTL) || Object.values(cache.value).some(entry => entry.group === 'catalog' && entry.expiresAt <= now)) catalogChanged();
		lastResume = now;
	};
	mittBus.on(TransporterEvent.DataIncident as any, incidentChanged);
	mittBus.on(TransporterEvent.DataTmsZone as any, catalogChanged);
	mittBus.on(TransporterEvent.Refresh as any, refreshAll);
	mittBus.on('event-nats-status' as any, reconnected);
	window.addEventListener('focus', resume);
	document.addEventListener('visibilitychange', resume);
	onScopeDispose(() => {
		disposed = true;
		if (realtimeTimer) clearTimeout(realtimeTimer);
		axiosInstance.interceptors.response.eject(interceptor);
		mittBus.off(TransporterEvent.DataIncident as any, incidentChanged);
		mittBus.off(TransporterEvent.DataTmsZone as any, catalogChanged);
		mittBus.off(TransporterEvent.Refresh as any, refreshAll);
		mittBus.off('event-nats-status' as any, reconnected);
		window.removeEventListener('focus', resume);
		document.removeEventListener('visibilitychange', resume);
		invalidate();
	});
	return {
		cache,
		catalogRevision,
		incidentRevision,
		ensureBaseConfig,
		getEventTypes,
		getZones,
		getEquipment,
		getEquipmentTypes,
		getZonesByIds,
		getEquipmentByIds,
		getZonesByPosition,
		getEquipmentByZones,
		getFileServer,
		getHistoryUserNames,
		getHistoryOrganizationNames,
		getIncidentHistory,
		getIncidentChart,
		getIncidentCount,
		invalidate,
		$reset: refreshAll,
	};
});
