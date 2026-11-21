import { computed, onActivated, onBeforeUnmount, onMounted, reactive, ref } from 'vue';
import type { WorkspaceController, WorkspaceSort, WorkspaceState } from './workspace';

interface TableOptions<Row> {
	tab: string;
	canRead: () => boolean;
	query: (
		page: { currentPage: number; pageSize: number },
		sort: { field?: string; order?: 'asc' | 'desc' | null }
	) => Promise<{ data: { result?: { records?: Row[] | null; total?: number | null } | null } }>;
	reset: () => void;
	reloadOptions?: () => Promise<void>;
}

/** Tìm dòng theo khóa, kể cả dòng con của bảng cây (`children`). */
function findRow<Row extends { id?: string | null }>(rows: Row[], key: string): Row | null {
	for (const row of rows) {
		if (String(row.id) === key) return row;
		const children = (row as { children?: Row[] | null }).children;
		const child = children?.length ? findRow(children, key) : null;
		if (child) return child;
	}
	return null;
}

/** Adapts an API-backed CRUD table to the shared workspace without coupling it to a domain. */
export function useWorkspaceTable<Row extends { id?: string | null }>(options: TableOptions<Row>): WorkspaceController<Row> & { search: () => Promise<void> } {
	const state = reactive({
		tab: options.tab,
		rows: [] as Row[],
		selectedKey: '',
		loading: false,
		saving: false,
		page: 1,
		pageSize: 50,
		total: 0,
		pendingUpdates: false,
		lastLoaded: null as Date | null,
	}) as WorkspaceState<Row>;
	const error = ref('');
	const optionsError = ref('');
	const selected = computed(() => findRow(state.rows, state.selectedKey));
	let sort: Partial<WorkspaceSort> = {};
	let requestId = 0;
	let disposed = false;
	let activated = false;

	async function load() {
		if (!options.canRead() || disposed) return;
		const request = ++requestId;
		state.loading = true;
		error.value = '';
		try {
			const response = await options.query({ currentPage: state.page, pageSize: state.pageSize }, sort);
			if (request !== requestId || disposed) return;
			state.rows = response.data.result?.records ?? [];
			state.total = response.data.result?.total ?? 0;
			if (!state.rows.length && state.page > 1 && state.total > 0) {
				state.page = Math.ceil(state.total / state.pageSize);
				return await load();
			}
			if (!selected.value) state.selectedKey = '';
			state.lastLoaded = new Date();
		} catch (reason) {
			if (request === requestId && !disposed) {
				state.rows = [];
				state.total = 0;
				state.selectedKey = '';
				error.value = reason instanceof Error ? reason.message : String(reason);
			}
			throw reason;
		} finally {
			if (request === requestId && !disposed) state.loading = false;
		}
	}

	async function search() {
		if (state.saving) return;
		state.page = 1;
		await load();
	}
	async function reloadOptions() {
		await options.reloadOptions?.();
	}
	const controller: WorkspaceController<Row> = {
		state,
		selected,
		error,
		optionsError,
		load,
		async selectRow(row) {
			state.selectedKey = row?.id ? String(row.id) : '';
		},
		async reset() {
			options.reset();
			await search();
		},
		async move(direction) {
			const index = state.rows.findIndex((row) => String(row.id) === state.selectedKey);
			const row = state.rows[index + direction];
			if (row) await controller.selectRow(row);
		},
		async changePage(page) {
			state.page = page;
			await load();
		},
		async changeSort(field, order) {
			sort = field && order ? { field, order } : {};
			await search();
		},
		reloadOptions,
		async refresh() {
			await Promise.all([load(), reloadOptions()]);
		},
	};
	onMounted(() => {
		void load().catch(() => {});
	});
	onActivated(() => {
		if (activated) void load().catch(() => {});
		activated = true;
	});
	onBeforeUnmount(() => {
		disposed = true;
		++requestId;
	});
	return Object.assign(controller, { search });
}
