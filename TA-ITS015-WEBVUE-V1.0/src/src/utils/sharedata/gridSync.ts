import { nextTick } from 'vue';
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table';

type GridPagerState = {
	total: number;
	pageSize: number;
	currentPage: number;
};

type GridProxyReactData<T> = {
	tableData?: T[];
	tablePage?: GridPagerState;
	sortData?: { field?: string; order?: string | null }[];
};

export type VxeGridWithReactData<T = any> = VxeGridInstance<T> & {
	reactData?: GridProxyReactData<T>;
};

export interface SilentGridSyncOptions<T = any, R = any> {
	grid?: VxeGridWithReactData<T>;
	queryApi: (
		page: VxeGridPropTypes.ProxyAjaxQueryPageParams,
		sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams
	) => Promise<R>;
	defaultSort?: { field?: string; order?: any };
	selectChangeEvent?: () => void;
	onAfterSync?: (records: T[], response: R) => void | Promise<void>;
}

/**
 * Đồng bộ lại vxe-grid ở chế độ nền mà không bật mặt nạ loading của bảng hay proxy.
 * Cập nhật thẳng reactData.tableData/tablePage để tránh vxe-grid tự bật tableLoading.
 */
export async function silentSyncGrid<
	T = any,
	R extends { data?: { result?: { records?: T[] | null; total?: number | null } | null } } = any,
>(opts: SilentGridSyncOptions<T, R>): Promise<T[] | undefined> {
	const { grid, queryApi, defaultSort, selectChangeEvent, onAfterSync } = opts;
	if (!grid) return undefined;

	const proxyInfo = grid.getProxyInfo?.();
	const pager = proxyInfo?.pager ?? grid.reactData?.tablePage;
	const pageSize = pager?.pageSize || 50;
	let currentPage = pager?.currentPage || 1;

	const proxySort = proxyInfo?.sort as VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams | undefined;
	const sort = {
		field: proxySort?.field || defaultSort?.field || '',
		order: proxySort?.order || defaultSort?.order || null,
	} as VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams;

	let res = await queryApi({ currentPage, pageSize, total: pager?.total ?? 0 }, sort);
	let result = res?.data?.result;
	let records = (result?.records ?? []) as T[];
	let total = Number(result?.total ?? 0);

	// Khi xóa hết dòng ở trang cuối (> 1), lùi về trang hợp lệ gần nhất và nạp lại.
	if (records.length === 0 && currentPage > 1 && total > 0) {
		const maxPage = Math.max(Math.ceil(total / pageSize), 1);
		if (maxPage < currentPage) {
			currentPage = maxPage;
			if (pager) pager.currentPage = currentPage;
			res = await queryApi({ currentPage, pageSize, total }, sort);
			result = res?.data?.result;
			records = (result?.records ?? []) as T[];
			total = Number(result?.total ?? 0);
		}
	}

	if (pager) {
		pager.total = total;
		pager.currentPage = currentPage;
	}

	if (grid.reactData) {
		grid.reactData.tableData = records;
	} else {
		await grid.loadData?.(records);
	}

	await nextTick();
	selectChangeEvent?.();
	await onAfterSync?.(records, res);
	return records;
}

export interface OptimisticRemoveGridOptions<T = any> {
	grid?: VxeGridWithReactData<T>;
	predicate: (row: T) => boolean;
	summary?: { total: number; lastLoaded: Date | null };
	selectChangeEvent?: () => void;
}

export interface OptimisticRemoveResult {
	removedCount: number;
	rollback: () => Promise<void>;
}

/**
 * Gỡ ngay các dòng khớp điều kiện khỏi trang hiện tại trên vxe-grid và trả về hàm hoàn tác khi API lỗi.
 */
export function optimisticRemoveGridRows<T = any>(opts: OptimisticRemoveGridOptions<T>): OptimisticRemoveResult {
	const { grid, predicate, summary, selectChangeEvent } = opts;
	if (!grid) {
		return { removedCount: 0, rollback: async () => {} };
	}

	const proxyInfo = grid.getProxyInfo?.();
	const currentRecords = ((grid.reactData?.tableData ?? proxyInfo?.data ?? []) as T[]).slice();
	const pager = proxyInfo?.pager ?? grid.reactData?.tablePage;
	const prevPagerTotal = pager?.total ?? 0;
	const prevSummaryTotal = summary?.total ?? 0;

	const removedRows: T[] = [];
	const nextRecords: T[] = [];
	for (const row of currentRecords) {
		if (predicate(row)) removedRows.push(row);
		else nextRecords.push(row);
	}

	if (removedRows.length === 0) {
		return { removedCount: 0, rollback: async () => {} };
	}

	// Bỏ chọn checkbox của các dòng sắp xóa để tránh giữ trạng thái reserve cho bản ghi không còn tồn tại.
	grid.setCheckboxRow?.(removedRows, false);
	if (grid.reactData) {
		grid.reactData.tableData = nextRecords;
	} else {
		void grid.loadData?.(nextRecords);
	}
	if (pager) pager.total = Math.max(0, prevPagerTotal - removedRows.length);
	if (summary) summary.total = Math.max(0, prevSummaryTotal - removedRows.length);
	selectChangeEvent?.();

	const rollback = async () => {
		const restored = currentRecords.slice();
		if (grid.reactData) {
			grid.reactData.tableData = restored;
		} else {
			await grid.loadData?.(restored);
		}
		if (pager) pager.total = prevPagerTotal;
		if (summary) summary.total = prevSummaryTotal;
		await nextTick();
		selectChangeEvent?.();
	};

	return { removedCount: removedRows.length, rollback };
}
