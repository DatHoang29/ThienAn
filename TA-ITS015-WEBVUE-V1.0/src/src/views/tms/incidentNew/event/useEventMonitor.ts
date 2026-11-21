import { computed, inject, onActivated, onDeactivated, onBeforeUnmount, onMounted, reactive, toRefs, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useDateFormat } from '@vueuse/core';
import { DateTimeFormatEnum } from '/@/types/enums/baseEnum';
import { vdsTimestamp } from '/@/utils/vdsTimestamp';
import { ElMessage } from 'element-plus';
import { TmsIncidentApi } from '/@/api-services/tms';
import type { EquipmentOutput, EventTypeOutput, IncidentFromVdsEventOutput, PageTmsIncidentOutput, ZoneOutput } from '/@/api-services/tms';
import { incidentDate } from '/@/views/tms/incidentNew/incident/incidentModel';
import { useIncidentNavigation } from '/@/views/tms/incidentNew/shared/incidentNavigation';
import { VdsTrafficDataApi } from '/@/api-services/vds';
import { getAPI } from '/@/utils/axios-utils';
import { auth } from '/@/utils/authFunction';
import { useBaseConfig } from '/@/stores/baseConfig';
import { useTmsStore } from '/@/stores/tmsStore';
import { BaseConfigTypeEnum, TmsZoneTypes } from '/@/types/enums';
import { defaultFilters, rowKey, trafficRow, trafficQuery } from '/@/views/tms/incidentNew/event/eventModel';
import { positionOf as formatPosition } from '/@/views/tms/incidentNew/shared/monitorFormat';
import { vehicleCountField } from '/@/views/bigScreen/trafficInfo/component/vehicleCountField';
import type { EventFilters, EventRow } from '/@/views/tms/incidentNew/event/eventModel';
import type { ConfigOption } from '/@/views/tms/incidentNew/shared/monitorFormat';
import type { WorkspaceController } from '/@/components/workspace/workspace';

export function useEventMonitor() {
	// Dependencies
	const { t: $t } = useI18n();
	const baseConfig = useBaseConfig();
	const tmsStore = useTmsStore();

	const positionOf = (row: Parameters<typeof formatPosition>[0]) => formatPosition(row, $t('lz.label.base.noPosition'));

	// Query, selection and request state
	const state = reactive({
		tab: 'event',
		filters: defaultFilters(),
		rows: [] as EventRow[],
		selectedKey: '',
		detail: null as EventRow | null,
		allEquipment: [] as EquipmentOutput[],
		loading: false,
		detailLoading: false,
		saving: false,
		errorKey: '',
		detailErrorKey: '',
		configErrorKey: '',
		page: 1,
		pageSize: 50,
		total: 0,
		pendingUpdates: false,
		lastLoaded: null as Date | null,
		detailVersion: 0,
		dataRevision: 0,
		catalog: {
			zones: [] as ZoneOutput[],
			equipment: [] as EquipmentOutput[],
			eventTypes: [] as EventTypeOutput[],
		},
		linked: { key: '', loading: false, incident: null as PageTmsIncidentOutput | null },
	});
	let appliedFilters: EventFilters = { ...state.filters };
	const error = computed(() => (state.errorKey ? $t(state.errorKey) : ''));
	const detailError = computed(() => (state.detailErrorKey ? $t(state.detailErrorKey) : ''));
	const configError = computed(() => (state.configErrorKey ? $t(state.configErrorKey) : ''));

	// Derived selection, permissions and catalog labels
	const zoneOptions = computed(() => state.catalog.zones);
	const canView = computed(() => auth('tmsTrafficAnalysis:view'));
	const canViewIncident = computed(() => auth('tmsIncident:view'));
	const navigation = useIncidentNavigation();
	const selectedIndex = computed(() => state.rows.findIndex((row) => rowKey(row) === state.selectedKey));
	const selected = computed(() => state.rows[selectedIndex.value] ?? null);
	const selectedDetail = computed(() => (state.detail && rowKey(state.detail) === state.selectedKey ? state.detail : selected.value));
	const hasPrevious = computed(() => !state.loading && !state.saving && (selectedIndex.value > 0 || state.page > 1));
	const hasNext = computed(() => !state.loading && !state.saving && (selectedIndex.value < state.rows.length - 1 || state.page * state.pageSize < state.total));

	const configList = (group: string): ConfigOption[] => baseConfig.getConfigDataByCode(group) ?? [];

	const configName = (group: string, code: unknown) => (code == null || code === '' ? '—' : baseConfig.getConfigItemByCode(group, String(code))?.name || String(code));

	const relatedIds = (value?: string | null) =>
		String(value ?? '')
			.split(',')
			.map((id) => id.trim())
			.filter(Boolean);

	const findEquipment = (id: string) => state.allEquipment.find((item) => item.id === id);

	const equipmentName = (row: EventRow) =>
		relatedIds(row.equipmentId)
			.map((id) => {
				const item = findEquipment(id);
				return item?.name || item?.code || id;
			})
			.join(', ') ||
		row.cameraIp ||
		'—';

	// Tuyến giống tên sự cố BE (IncidentPromoter): Location của camera tra danh mục tms_zone_location (main_route → "Tuyến CT.01").
	// Camera thiếu Location thì lấy location của zone (đi ngược pid), cuối cùng là tên zone loại 'route'.
	const zoneById = computed(() => new Map(state.catalog.zones.map((zone) => [zone.id, zone])));

	const locationName = (code?: string | null) => (code ? baseConfig.getConfigItemByCode(BaseConfigTypeEnum.TmsZoneLocation, code)?.name || code : '');

	const routeOfZone = (id: string) => {
		const seen = new Set<string>();
		let zone = zoneById.value.get(id);
		let routeZone = '';
		while (zone?.id && !seen.has(zone.id)) {
			if (zone.location) return locationName(zone.location);
			if (!routeZone && zone.type === TmsZoneTypes.Route) routeZone = zone.name || zone.code || '';
			seen.add(zone.id);
			zone = zone.pid ? zoneById.value.get(zone.pid) : undefined;
		}
		return routeZone;
	};

	const routeName = (row: EventRow) => {
		const fromEquipment = relatedIds(row.equipmentId).map((id) => locationName(findEquipment(id)?.location));
		const names = fromEquipment.some(Boolean) ? fromEquipment : relatedIds(row.zoneId).map(routeOfZone);
		return [...new Set(names.filter(Boolean))].join(', ');
	};

	const routeLabel = computed(() => baseConfig.getConfigItemByCode(BaseConfigTypeEnum.TmsZoneType, String(TmsZoneTypes.Route))?.name || $t('lz.label.base.location'));

	// Dòng phụ ô Thiết bị/Vị trí: "Tuyến HN-CL · Km12+300".
	const routePosition = (row: EventRow) => [routeName(row), recordPosition(row)].filter((part) => part && part !== '—').join(' · ') || '—';

	const recordPosition = (row: EventRow) => positionOf(row.kmNumber != null || row.metNumber != null ? row : (findEquipment(String(row.equipmentId ?? '').trim()) ?? row));

	const equipmentDisplay = (item: EquipmentOutput) => `${item.name || item.code || item.id}${item.kmNumber != null || item.metNumber != null ? ` (${positionOf(item)})` : ''}`;

	const zoneName = (row: EventRow) =>
		relatedIds(row.zoneId)
			.map((id) => {
				const item = state.catalog.zones.find((zone) => zone.id === id);
				if (!item) return id;
				const from = positionOf({ kmNumber: item.fromKmNumber, metNumber: item.fromMetNumber });
				const to = positionOf({ kmNumber: item.toKmNumber, metNumber: item.toMetNumber });
				return `${item.name || item.code || id}${item.fromKmNumber != null || item.toKmNumber != null ? ` (${from} – ${to})` : ''}`;
			})
			.join(', ') || '—';

	const vehicleSizes = computed(() =>
		['short', 'medium', 'long', 'other'].map((code) => {
			const item = configList(BaseConfigTypeEnum.TmsVehicleTypeConfigCode).find((item: ConfigOption) => vehicleCountField(item) === code);
			return {
				code,
				label: item?.name ?? ({ short: 'S', medium: 'M', long: 'L', other: $t('lz.label.base.other') } as Record<string, string>)[code],
			};
		})
	);

	const vehicleTypeName = (row: EventRow) => vehicleSizes.value.find((item) => item.code === row.vehicleSize)?.label || row.vehicleSize || '—';

	// EventType là mã TmsEventType đã ánh xạ từ AID; 'unknown' = AID chưa ánh xạ, kèm mã AID gốc.
	const findEventType = (code?: string | null) => (code ? state.catalog.eventTypes.find((item) => item.code?.toLowerCase() === code.toLowerCase()) : undefined);

	const eventTypeName = (row: EventRow) => {
		if (!row.eventType) return '—';
		if (row.eventType.toLowerCase() === 'unknown') {
			const unknown = findEventType('unknown')?.name || $t('lz.label.base.unknown');
			return row.aidEventType ? `${unknown} (${row.aidEventType})` : unknown;
		}
		return findEventType(row.eventType)?.name || row.eventType;
	};

	// Bộ lọc: loại TMS đang dùng, bỏ 'normal' vì tab chỉ hiện sự kiện.
	const eventTypeOptions = computed(() =>
		state.catalog.eventTypes.filter((item) => item.code && item.code.toLowerCase() !== 'normal').sort((a, b) => (a.name ?? a.code ?? '').localeCompare(b.name ?? b.code ?? ''))
	);

	const details = computed(() => {
		const row = selectedDetail.value;
		if (!row) return [];
		const recordTime = row.detectTime && vdsTimestamp(row.detectTime);
		const basic = [
			{
				label: $t('lz.entity.tms.detectTime'),
				value: recordTime && Number.isFinite(new Date(recordTime).getTime()) ? useDateFormat(recordTime, DateTimeFormatEnum.DateTimeDefault).value : '—',
			},
			{ label: $t('lz.label.tms.equipmentCamera'), value: equipmentName(row) },
			{ label: routeLabel.value, value: routeName(row) || '—' },
			{ label: $t('lz.entity.tms.roadPosition'), value: recordPosition(row) },
			{
				label: $t('lz.entity.tms.direction'),
				value: configName(BaseConfigTypeEnum.TmsDirectionType, row.direction),
			},
		];
		return [
			...basic,
			{ label: $t('lz.entity.tms.vehicleSize'), value: vehicleTypeName(row) },
			{ label: $t('lz.entity.tms.vehicleType'), value: row.vehicleType || '—' },
			{ label: $t('lz.entity.tms.speed'), value: row.speed == null ? '—' : `${row.speed} km/h` },
			{ label: $t('lz.entity.tms.vehicleColor'), value: row.vehicleColor || '—' },
			{ label: $t('lz.entity.tms.lane'), value: row.lane || '—' },
			{ label: $t('lz.entity.tms.length'), value: row.lengthMm == null ? '—' : `${row.lengthMm} mm` },
			{ label: $t('lz.entity.base.ipAddress'), value: row.cameraIp || '—' },
			{ label: $t('lz.label.tms.detectedEvent'), value: eventTypeName(row) },
			{ label: $t('lz.label.base.location'), value: zoneName(row) },
			{ label: $t('lz.entity.tms.vdsEventId'), value: String(row.vendorEventId ?? '—') },
		];
	});

	let listRequest = 0;
	let listAbort: AbortController | undefined;
	let catalogRequest = 0;
	let disposed = false;
	let active = true;
	let realtimeRefreshPending = false;

	// API requests
	function queryParams(currentPage = state.page) {
		return trafficQuery(appliedFilters, currentPage, state.pageSize);
	}

	async function selectRow(row: EventRow | null, force = false, refreshMedia = true) {
		if (state.saving && !force) return;
		const key = row ? rowKey(row) : '';
		const sameRow = key === state.selectedKey;
		if (!force && sameRow) return;
		state.selectedKey = key;
		state.detail = row && canView.value ? { ...row } : null;
		state.detailErrorKey = '';
		state.detailLoading = false;
		if (force && sameRow && row && refreshMedia) state.detailVersion++;
		void loadLinkedIncident(state.detail);
	}

	// Sự cố do VDS tạo từ bản tin này: TmsIncident.EventId = VendorEventId, tìm trong ±1 ngày quanh lúc phát hiện.
	let linkRequest = 0;
	let linkAbort: AbortController | undefined;

	async function loadLinkedIncident(row: EventRow | null) {
		const request = ++linkRequest;
		linkAbort?.abort();
		state.linked = { key: row ? rowKey(row) : '', loading: false, incident: null };
		const detectTime = row?.detectTime ? new Date(vdsTimestamp(row.detectTime)) : null;
		if (!row || row.vendorEventId == null || !detectTime || !Number.isFinite(detectTime.getTime()) || !canViewIncident.value) return;
		linkAbort = new AbortController();
		state.linked.loading = true;
		try {
			const day = 24 * 60 * 60 * 1000;
			const response = await getAPI(TmsIncidentApi).apiTmsTmsincidentPagePost(
				{
					page: 1,
					pageSize: 1,
					field: 'startDate',
					order: 'desc',
					eventId: String(row.vendorEventId),
					includeEvents: true,
					includeIncidents: true,
					fromDate: incidentDate(new Date(detectTime.getTime() - day)),
					toDate: incidentDate(new Date(detectTime.getTime() + day)),
				},
				{ signal: linkAbort.signal }
			);
			if (request !== linkRequest || disposed) return;
			state.linked.incident = response.data.result?.records?.[0] ?? null;
		} catch {
			// Không tra được thì coi như chưa có sự cố; nút Tạo sự cố vẫn dùng được.
			if (request === linkRequest && !disposed) state.linked.incident = null;
		} finally {
			if (request === linkRequest && !disposed) state.linked.loading = false;
		}
	}

	function openLinkedIncident() {
		const incident = state.linked.incident;
		if (!incident?.id) return;
		const result = navigation?.openIncident({ id: incident.id, code: incident.code, startDate: incident.startDate }) ?? 'missing';
		if (result === 'missing') ElMessage.warning($t('lz.message.tmsIncident.reviewIncidentTabMissing'));
	}

	// Tạo sự cố từ sự kiện: hộp thoại nằm ở slot dialogs của EventManage, trang đó đăng ký hàm mở vào đây.
	let openCreateDialog: ((row: EventRow) => void) | null = null;

	function bindCreateDialog(open: ((row: EventRow) => void) | null) {
		openCreateDialog = open;
	}

	// Chỉ sự kiện có loại (đã ánh xạ) thuộc nhóm SỰ CỐ mới tạo được sự cố; nhóm sự kiện và 'unknown' thì không (BE kiểm lại).
	function canCreateIncident(row?: EventRow | null) {
		const type = findEventType(row?.eventType);
		return !!type && type.type?.trim().toLowerCase() === 'incident' && type.code?.trim().toLowerCase() !== 'unknown';
	}

	function createIncident() {
		const row = selectedDetail.value;
		if (!row || state.linked.incident || !canCreateIncident(row)) return;
		openCreateDialog?.(row);
	}

	// Sau khi tạo (hoặc bản tin đã có sự cố / chọn mở sự cố đang mở): tra lại liên kết rồi mở bên tab Sự cố.
	async function incidentCreated(result: IncidentFromVdsEventOutput) {
		await loadLinkedIncident(state.detail);
		if (!result.incidentId) return;
		const opened = navigation?.openIncident({ id: result.incidentId, code: result.code, startDate: result.startDate }) ?? 'missing';
		if (opened === 'missing') ElMessage.warning($t('lz.message.tmsIncident.reviewIncidentTabMissing'));
	}

	async function load(preferredKey = state.selectedKey, selectLast = false): Promise<boolean> {
		const request = ++listRequest;
		const incidentRevision = tmsStore.incidentRevision;
		listAbort?.abort();
		listAbort = new AbortController();
		state.loading = true;
		state.errorKey = '';
		try {
			// Like the source pages, send read requests through getAPI; the backend checks access.
			const params = queryParams();
			const response = await getAPI(VdsTrafficDataApi).apiVdsVdstrafficdataPagePost(params, { signal: listAbort.signal });
			if (request !== listRequest || disposed) return false;
			const result = response.data.result;
			if (!result) throw new Error($t('lz.message.base.errorFetchingData'));
			state.total = result.total ?? 0;
			state.rows = (result.records ?? []).map(trafficRow);
			if (!state.rows.length && state.page > 1 && state.total > 0) {
				state.page = Math.max(1, Math.ceil(state.total / state.pageSize));
				return await load(preferredKey, selectLast);
			}
			const target = state.rows.find((row) => rowKey(row) === preferredKey) ?? (selectLast ? state.rows.at(-1) : state.rows[0]) ?? null;
			// A new page response replaces the record even when its selected id has not changed.
			void selectRow(target, true);
			state.pendingUpdates = false;
			if (incidentRevision === tmsStore.incidentRevision) realtimeRefreshPending = false;
			state.lastLoaded = new Date();
			return true;
		} catch {
			if (request !== listRequest || disposed) return false;
			state.rows = [];
			state.total = 0;
			await selectRow(null, true);
			state.errorKey = 'lz.message.base.listLoadFailed';
			return false;
		} finally {
			if (request === listRequest && !disposed) state.loading = false;
		}
	}

	// Search, paging and workflow actions
	function validFilters() {
		const f = state.filters;
		if (f.dateRange && (!f.dateRange.every((date) => date instanceof Date && Number.isFinite(date.getTime())) || f.dateRange[0] > f.dateRange[1])) {
			ElMessage.warning($t('lz.validation.base.startNotAfterEnd'));
			return false;
		}
		if ((f.speedFrom != null && f.speedTo != null && f.speedFrom > f.speedTo) || (f.lengthFrom != null && f.lengthTo != null && f.lengthFrom > f.lengthTo)) {
			ElMessage.warning($t('lz.validation.base.valueRange'));
			return false;
		}
		return true;
	}

	async function search() {
		if (state.saving || !validFilters()) return;
		appliedFilters = { ...state.filters, dateRange: state.filters.dateRange ? [...state.filters.dateRange] : null };
		state.page = 1;
		await selectRow(null, true);
		await load('');
	}

	async function reset() {
		if (!state.saving) {
			state.filters = defaultFilters();
			await search();
		}
	}

	async function changePage(value: number) {
		if (state.saving || state.loading) return;
		state.page = value;
		await load('');
	}

	async function changeSort() {
		// VDS keeps the source page's fixed detectTime descending order.
	}

	async function move(direction: -1 | 1) {
		if (state.saving || state.loading || !state.rows.length) return;
		const nextIndex = selectedIndex.value + direction;
		if (nextIndex >= 0 && nextIndex < state.rows.length) {
			await selectRow(state.rows[nextIndex]);
			return;
		}
		if ((direction === 1 && hasNext.value) || (direction === -1 && hasPrevious.value)) {
			state.page += direction;
			await load('', direction === -1);
		}
	}

	// Shared catalog loading
	async function loadCatalog(force = false) {
		const request = ++catalogRequest;
		const results = await Promise.allSettled([
			tmsStore.ensureBaseConfig(force),
			tmsStore.getZones(force),
			Promise.all([tmsStore.getEquipment(force), tmsStore.getEquipmentTypes(force)]),
			tmsStore.getEventTypes(force),
		]);
		if (request !== catalogRequest || disposed) return;
		const [, zones, devices, eventTypes] = results;
		if (zones.status === 'fulfilled') state.catalog.zones = zones.value;
		if (eventTypes.status === 'fulfilled') state.catalog.eventTypes = eventTypes.value;
		if (devices.status === 'fulfilled') {
			const [equipment, types] = devices.value;
			state.allEquipment = equipment;
			const ids = new Set(types.filter((item) => /CCTV|VDS/i.test(item.code ?? '')).map((item) => item.id));
			state.catalog.equipment = equipment.filter((item) => ids.has(item.equipmentTypeId)).sort((a, b) => (a.name ?? '').localeCompare(b.name ?? ''));
		}
		state.configErrorKey = results.some((result) => result.status === 'rejected') ? 'lz.message.base.catalogLoadFailed' : '';
	}

	async function refresh() {
		tmsStore.invalidate();
		await Promise.all([load(state.selectedKey), loadCatalog(true)]);
	}

	function flushRealtimeRefresh() {
		if (!realtimeRefreshPending || disposed || !active || state.loading || state.saving) return;
		realtimeRefreshPending = false;
		void load(state.selectedKey);
	}

	// Lifecycle
	// Reload through the store's coalesced NATS/reconnect revision.
	watch(
		() => tmsStore.incidentRevision,
		() => {
			realtimeRefreshPending = true;
			flushRealtimeRefresh();
		}
	);
	watch([() => state.loading, () => state.saving], flushRealtimeRefresh);
	watch(
		() => tmsStore.catalogRevision,
		async () => {
			if (!active) return;
			await loadCatalog();
			if (!disposed && selected.value) void selectRow(selected.value, true, false);
		}
	);
	onActivated(() => {
		active = true;
		void loadCatalog();
		flushRealtimeRefresh();
	});
	onDeactivated(() => {
		active = false;
	});
	onMounted(async () => {
		await loadCatalog();
		if (!disposed) await search();
	});
	watch(canView, () => {
		if (selected.value) void selectRow(selected.value, true);
	});
	onBeforeUnmount(() => {
		disposed = true;
		++listRequest;
		++catalogRequest;
		++linkRequest;
		listAbort?.abort();
		linkAbort?.abort();
	});

	const workspace: WorkspaceController<EventRow> = {
		state,
		selected,
		error,
		optionsError: configError,
		selectRow,
		load,
		reset,
		move,
		changeSort,
		changePage,
		reloadOptions: loadCatalog,
		refresh,
	};
	return {
		workspace,
		state,
		...toRefs(state),
		catalog: state.catalog,
		selected,
		selectedDetail,
		selectedIndex,
		error,
		detailError,
		configError,
		canView,
		canViewIncident,
		openLinkedIncident,
		createIncident,
		canCreateIncident,
		bindCreateDialog,
		incidentCreated,
		hasPrevious,
		hasNext,
		configList,
		configName,
		vehicleSizes,
		vehicleTypeName,
		eventTypeName,
		eventTypeOptions,
		details,
		zoneOptions,
		equipmentName,
		equipmentDisplay,
		recordPosition,
		routeName,
		routePosition,
		zoneName,
		queryParams,
		selectRow,
		load,
		search,
		reset,
		changePage,
		changeSort,
		move,
		loadCatalog,
	};
}

export type EventMonitor = ReturnType<typeof useEventMonitor>;
