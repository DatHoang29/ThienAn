import { reactive } from 'vue';

type PageResponse = { data: { result?: { total?: number | null } | null } };

/**
 * Tổng số bản ghi và thời điểm tải gần nhất cho `GridSummary`.
 * Bọc lời gọi API trang trong `track(...)`: kết quả trả về giữ nguyên cho proxy của vxe-grid.
 */
export function useGridSummary() {
	const summary = reactive({ total: 0, lastLoaded: null as Date | null });

	async function track<T extends PageResponse>(request: Promise<T>): Promise<T> {
		const response = await request;
		summary.total = response.data.result?.total ?? 0;
		summary.lastLoaded = new Date();
		return response;
	}

	return { summary, track };
}
