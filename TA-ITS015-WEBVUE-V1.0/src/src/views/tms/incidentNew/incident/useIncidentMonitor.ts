import { computed, inject, onActivated, onDeactivated, onBeforeUnmount, onMounted, reactive, toRefs, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useDateFormat } from '@vueuse/core';
import { DateTimeFormatEnum } from '/@/types/enums/baseEnum';
import { ElMessage, ElMessageBox } from 'element-plus';
import { TmsIncidentApi } from '/@/api-services/tms';
import type { EquipmentOutput, EventTypeOutput, ZoneOutput } from '/@/api-services/tms';
import { incidentEventTypeDisplay } from '/@/utils/incidentEventTypeDisplay';
import { getAPI } from '/@/utils/axios-utils';
import { auth } from '/@/utils/authFunction';
import { useBaseConfig } from '/@/stores/baseConfig';
import { useTmsStore } from '/@/stores/tmsStore';
import { BaseConfigTypeEnum, TmsIncidentStateEnum } from '/@/types/enums';
import { defaultFilters, rowKey, stateLabelKeys, incidentQuery } from '/@/views/tms/incidentNew/incident/incidentModel';
import { positionOf as formatPosition } from '/@/views/tms/incidentNew/shared/monitorFormat';
import { useIncidentNavigation } from '/@/views/tms/incidentNew/shared/incidentNavigation';
import type { IncidentFocusRequest } from '/@/views/tms/incidentNew/shared/incidentNavigation';
import type { IncidentFilters, IncidentRow } from '/@/views/tms/incidentNew/incident/incidentModel';
import type { ConfigOption } from '/@/views/tms/incidentNew/shared/monitorFormat';
import type { WorkspaceController, WorkspaceSort } from '/@/components/workspace/workspace';

export function useIncidentMonitor() {
	// Dependencies
	const { t: $t, locale } = useI18n();
	const baseConfig = useBaseConfig();
	const tmsStore = useTmsStore();

	const positionOf = (row: Parameters<typeof formatPosition>[0]) => formatPosition(row, $t('lz.label.base.noPosition'));

	const stateLabels = computed(() => Object.fromEntries(Object.entries(stateLabelKeys).map(([state, key]) => [state, $t(key)])));

	// Query, selection and request state
	const state = reactive({
		tab: 'incident',
		filters: defaultFilters(),
		incidentChartFilters: defaultFilters(),
		// Chế độ xem tạm một sự cố mở từ tab Sự kiện: KHÔNG ghi đè bộ lọc của người dùng; Làm mới/Tìm kiếm/Đặt lại sẽ thoát.
		focus: null as { id: string; code: string; dateRange: [Date, Date] } | null,
		sort: { field: 'startDate', order: 'desc' } as WorkspaceSort,
		rows: [] as IncidentRow[],
		selectedKey: '',
		detail: null as IncidentRow | null,
		detailEquipment: [] as EquipmentOutput[],
		detailZones: [] as ZoneOutput[],
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
			eventTypes: [] as ConfigOption[],
			zones: [] as ZoneOutput[],
			equipment: [] as EquipmentOutput[],
		},
	});
	let appliedFilters: IncidentFilters = { ...state.filters };
	const error = computed(() => (state.errorKey ? $t(state.errorKey) : ''));
	const detailError = computed(() => (state.detailErrorKey ? $t(state.detailErrorKey) : ''));
	const configError = computed(() => (state.configErrorKey ? $t(state.configErrorKey) : ''));

	// Derived selection, permissions and catalog labels
	const zoneOptions = computed(() => state.catalog.zones.filter((item) => item.type === 'zone'));
	const canView = computed(() => auth('tmsIncident:view'));
	const canUpdate = computed(() => auth('tmsIncident:update'));
	const selectedIndex = computed(() => state.rows.findIndex((row) => rowKey(row) === state.selectedKey));
	const selected = computed(() => state.rows[selectedIndex.value] ?? null);
	const selectedDetail = computed(() => (state.detail && rowKey(state.detail) === state.selectedKey ? state.detail : selected.value));
	const hasPrevious = computed(() => !state.loading && !state.saving && (selectedIndex.value > 0 || state.page > 1));
	const hasNext = computed(() => !state.loading && !state.saving && (selectedIndex.value < state.rows.length - 1 || state.page * state.pageSize < state.total));

	const configList = (group: string): ConfigOption[] => baseConfig.getConfigDataByCode(group) ?? [];

	const configName = (group: string, code: unknown) => (code == null || code === '' ? '—' : configList(group).find((item: ConfigOption) => String(item.code) === String(code))?.name || String(code));

	// The incident editor stores tms_zone_location; keep older tms_location codes readable.
	const locationName = (row: IncidentRow) => {
		if (!row.location) return '—';
		const item = [...configList(BaseConfigTypeEnum.TmsZoneLocation), ...configList(BaseConfigTypeEnum.TmsLocationType)].find((item: ConfigOption) => String(item.code) === String(row.location));
		return item?.name || row.location;
	};

	// Dùng chung với trang sự cố cũ: loại 'unknown' hiện kèm mã AID gốc trong metadata.
	const eventName = (row: IncidentRow) =>
		incidentEventTypeDisplay(row, state.catalog.eventTypes as EventTypeOutput[]) || row.eventTypeCode || $t('lz.label.base.unclassified');

	const relatedIds = (value?: string | null) =>
		String(value ?? '')
			.split(',')
			.map((id) => id.trim())
			.filter(Boolean);

	const findEquipment = (id: string) => state.detailEquipment.find((item) => item.id === id) ?? state.allEquipment.find((item) => item.id === id);

	const equipmentName = (row: IncidentRow) =>
		relatedIds(row.equipmentId)
			.map((id) => {
				const item = findEquipment(id);
				return item?.name || item?.code || id;
			})
			.join(', ') || '—';

	const recordPosition = (row: IncidentRow) => positionOf(row);

	const equipmentDisplay = (item: EquipmentOutput) => `${item.name || item.code || item.id}${item.kmNumber != null || item.metNumber != null ? ` (${positionOf(item)})` : ''}`;

	const zoneName = (row: IncidentRow) =>
		relatedIds(row.zoneId)
			.map((id) => {
				const item = state.detailZones.find((zone) => zone.id === id) ?? state.catalog.zones.find((zone) => zone.id === id);
				if (!item) return id;
				const from = positionOf({ kmNumber: item.fromKmNumber, metNumber: item.fromMetNumber });
				const to = positionOf({ kmNumber: item.toKmNumber, metNumber: item.toMetNumber });
				return `${item.name || item.code || id}${item.fromKmNumber != null || item.toKmNumber != null ? ` (${from} – ${to})` : ''}`;
			})
			.join(', ') || '—';

	const details = computed(() => {
		const row = selectedDetail.value;
		if (!row) return [];
		const recordTime = row.startDate;
		const basic = [
			{
				label: $t('lz.entity.base.startTime'),
				value: recordTime && Number.isFinite(new Date(recordTime).getTime()) ? useDateFormat(recordTime, DateTimeFormatEnum.DateTimeDefault).value : '—',
			},
			{ label: $t('lz.label.tms.equipmentCamera'), value: equipmentName(row) },
			{ label: $t('lz.entity.tms.roadPosition'), value: recordPosition(row) },
			{
				label: $t('lz.entity.tmsIncident.influenceScope'),
				value: configName(BaseConfigTypeEnum.TmsDirectionType, row.influenceScope),
			},
		];
		return [
			...basic,
			{ label: $t('lz.entity.base.name'), value: row.name || '—' },
			{ label: $t('lz.entity.base.code'), value: row.code || '—' },
			{ label: $t('lz.entity.tms.eventType'), value: eventName(row) },
			{ label: $t('lz.label.base.position'), value: locationName(row) },
			{
				label: $t('lz.entity.tmsIncident.source'),
				value: configName(BaseConfigTypeEnum.TmsSourceDetectIncident, row.source),
			},
			{ label: $t('lz.entity.base.registeredBy'), value: row.createdBy || '—' },
			{ label: $t('lz.label.base.location'), value: zoneName(row) },
			{
				label: $t('lz.entity.tmsIncident.injuredNumber'),
				value: row.injuredNumber == null ? '—' : row.injuredNumber.toLocaleString(locale.value),
			},
			{
				label: $t('lz.entity.tmsIncident.vehicleNumber'),
				value: row.vehicleNumber == null ? '—' : row.vehicleNumber.toLocaleString(locale.value),
			},
		];
	});

	const canCancel = (row: IncidentRow | null) =>
		!!row?.id && canView.value && canUpdate.value && [TmsIncidentStateEnum.Registration, TmsIncidentStateEnum.Verified, TmsIncidentStateEnum.InProgress].includes(row.state as TmsIncidentStateEnum);

	let listRequest = 0;
	let detailRequest = 0;
	let listAbort: AbortController | undefined;
	let catalogRequest = 0;
	let disposed = false;
	let active = true;
	let realtimeRefreshPending = false;

	// API requests
	function queryParams(currentPage = state.page) {
		const focus = state.focus;
		const filters = focus ? { ...defaultFilters(), keyword: focus.code, dateRange: focus.dateRange, incidentGroup: 'all' as const } : appliedFilters;
		return incidentQuery(filters, currentPage, state.pageSize, state.sort);
	}

	async function selectRow(row: IncidentRow | null, force = false, refreshMedia = true) {
		if (state.saving && !force) return;
		const key = row ? rowKey(row) : '';
		const sameRow = key === state.selectedKey;
		if (!force && sameRow) return;
		state.selectedKey = key;
		state.detail = row && canView.value ? { ...row } : null;
		state.detailEquipment = [];
		state.detailZones = [];
		state.detailErrorKey = '';
		const request = ++detailRequest;
		state.detailLoading = false;
		if (force && sameRow && row && refreshMedia) state.detailVersion++;
		if (!row || !canView.value) return;
		// Match incident/detailIncident: page rows contain the record; resolve their related ids.
		state.detailLoading = true;
		const results = await Promise.allSettled([
			row.zoneId ? tmsStore.getZonesByIds(row.zoneId) : Promise.resolve([]),
			row.equipmentId ? tmsStore.getEquipmentByIds(row.equipmentId) : Promise.resolve([]),
		]);
		if (request !== detailRequest || disposed) return;
		const [zones, equipment] = results;
		if (zones.status === 'fulfilled') state.detailZones = zones.value;
		if (equipment.status === 'fulfilled') state.detailEquipment = equipment.value;
		if (results.some((result) => result.status === 'rejected')) state.detailErrorKey = 'lz.message.tmsIncident.reviewDetailLoadFailed';
		state.detailLoading = false;
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
			const response = await getAPI(TmsIncidentApi).apiTmsTmsincidentPagePost(params, { signal: listAbort.signal });
			if (request !== listRequest || disposed) return false;
			const result = response.data.result;
			if (!result) throw new Error($t('lz.message.base.errorFetchingData'));
			state.total = result.total ?? 0;
			state.rows = result.records ?? [];
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
		if (f.fromKm != null && f.toKm != null && f.fromKm * 1000 + (f.fromMet ?? 0) > f.toKm * 1000 + (f.toMet ?? 999)) {
			ElMessage.warning($t('lz.validation.base.valueRange'));
			return false;
		}
		return true;
	}

	async function search() {
		if (state.saving || !validFilters()) return;
		state.focus = null;
		appliedFilters = { ...state.filters, dateRange: state.filters.dateRange ? [...state.filters.dateRange] : null };
		state.incidentChartFilters = { ...appliedFilters };
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

	async function changeSort(field?: string, order?: 'asc' | 'desc' | null) {
		if (state.saving) return;
		state.sort = field && order ? { field, order } : { field: 'startDate', order: 'desc' };
		state.page = 1;
		await load('');
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

	async function cancelIncident(row: IncidentRow) {
		if (state.saving || state.loading || !canCancel(row)) return;
		state.saving = true;
		const skipped = row.state === TmsIncidentStateEnum.Registration;
		try {
			try {
				await ElMessageBox.confirm(
					$t(skipped ? 'lz.message.tmsIncident.skipIncidentConfirm' : 'lz.message.tmsIncident.cancelIncidentConfirm', {
						name: row.name || row.code,
					}),
					$t('lz.label.base.confirm'),
					{
						confirmButtonText: $t('lz.button.base.confirm'),
						cancelButtonText: $t('lz.button.base.cancel'),
						type: 'warning',
					}
				);
			} catch {
				return;
			}
			const response = await getAPI(TmsIncidentApi).apiTmsTmsincidentUpdatestatePost({
				id: row.id,
				state: skipped ? TmsIncidentStateEnum.Skipped : TmsIncidentStateEnum.Canceled,
			});
			if (!response.data.result) throw new Error(response.data.message || $t('lz.message.base.updateError'));
			ElMessage.success($t('lz.message.base.updateSuccess'));
			if (!disposed) {
				state.dataRevision++;
				await load(rowKey(row));
			}
		} catch (reason) {
			ElMessage.error(reason instanceof Error ? reason.message : $t('lz.message.base.updateError'));
		} finally {
			state.saving = false;
		}
	}

	// Shared catalog loading
	async function loadCatalog(force = false) {
		const request = ++catalogRequest;
		const results = await Promise.allSettled([
			tmsStore.ensureBaseConfig(force),
			tmsStore.getEventTypes(force),
			tmsStore.getZones(force),
			Promise.all([tmsStore.getEquipment(force), tmsStore.getEquipmentTypes(force)]),
		]);
		if (request !== catalogRequest || disposed) return;
		const [, eventTypes, zones, devices] = results;
		if (eventTypes.status === 'fulfilled') state.catalog.eventTypes = eventTypes.value;
		if (zones.status === 'fulfilled') state.catalog.zones = zones.value;
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
		// Đang xem tạm một sự cố thì Làm mới = quay lại danh sách theo bộ lọc của người dùng (vẫn giữ dòng nếu còn trong trang).
		if (state.focus) {
			state.focus = null;
			state.page = 1;
		}
		await Promise.all([load(state.selectedKey), loadCatalog(true)]);
	}

	async function clearFocus() {
		if (!state.focus || state.saving) return;
		state.focus = null;
		state.page = 1;
		await load(state.selectedKey);
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
	// Mở một sự cố do tab Sự kiện yêu cầu: xem tạm đúng mã trong ngày bắt đầu (cả nhóm sự kiện lẫn sự cố) rồi chọn dòng đó.
	// Bộ lọc tìm kiếm, biểu đồ và trang của người dùng giữ nguyên để quay lại được.
	const navigation = useIncidentNavigation();
	let handledFocus = 0;
	let mounted = false;

	const pendingFocus = () => {
		const request = navigation?.request.value;
		return request && request.nonce !== handledFocus ? request : null;
	};

	async function focusIncident(request: IncidentFocusRequest) {
		handledFocus = request.nonce;
		const start = request.startDate ? new Date(request.startDate) : new Date();
		const from = Number.isFinite(start.getTime()) ? start : new Date();
		from.setHours(0, 0, 0, 0);
		const to = new Date(from);
		to.setDate(to.getDate() + 1);
		state.focus = { id: request.id, code: request.code || '', dateRange: [from, to] };
		state.page = 1;
		await load(request.id);
	}

	watch(
		() => navigation?.request.value?.nonce,
		() => {
			const request = pendingFocus();
			if (request && mounted && !disposed) void focusIncident(request);
		}
	);

	onMounted(async () => {
		await loadCatalog();
		if (disposed) return;
		mounted = true;
		const request = pendingFocus();
		await (request ? focusIncident(request) : search());
	});
	watch(canView, () => {
		if (selected.value) void selectRow(selected.value, true);
	});
	onBeforeUnmount(() => {
		disposed = true;
		++listRequest;
		++detailRequest;
		++catalogRequest;
		listAbort?.abort();
	});

	const workspace: WorkspaceController<IncidentRow> = {
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
		clearFocus,
		stateLabels,
		selected,
		selectedDetail,
		selectedIndex,
		error,
		detailError,
		configError,
		canView,
		canUpdate,
		hasPrevious,
		hasNext,
		configList,
		configName,
		locationName,
		details,
		zoneOptions,
		eventName,
		equipmentName,
		equipmentDisplay,
		recordPosition,
		zoneName,
		canCancel,
		queryParams,
		selectRow,
		load,
		search,
		reset,
		changePage,
		changeSort,
		move,
		cancelIncident,
		loadCatalog,
	};
}

export type IncidentMonitor = ReturnType<typeof useIncidentMonitor>;
