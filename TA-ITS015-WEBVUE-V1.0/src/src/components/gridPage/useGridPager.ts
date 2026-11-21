import { useResizeObserver } from '@vueuse/core';
import type { MaybeComputedElementRef } from '@vueuse/core';
import type { VxeGridProps } from 'vxe-table';

type PagerLayouts = NonNullable<Exclude<VxeGridProps['pagerConfig'], undefined>['layouts']>;

/** Cùng ngưỡng với DataWorkspace (bản `system/userMngt3`). */
const FULL: PagerLayouts = ['PrevJump', 'PrevPage', 'Number', 'NextPage', 'NextJump', 'Sizes', 'FullJump', 'PageCount', 'Total'];
const MEDIUM: PagerLayouts = ['PrevPage', 'Number', 'NextPage', 'Sizes', 'Jump', 'PageCount'];
const COMPACT: PagerLayouts = ['PrevPage', 'Jump', 'PageCount', 'NextPage', 'Sizes'];

/**
 * Thu gọn thanh phân trang của vxe-grid theo chiều rộng bảng (không theo cửa sổ),
 * nên đúng cả khi bảng bị hẹp do cây đơn vị, panel kéo giãn hoặc tab.
 * ≥ 900px: đầy đủ · 640–899px: bỏ nhảy nhanh/tổng · < 640px: chỉ trước/sau, ô tới trang, số trang, cỡ trang.
 * @param target vxe-grid (ref component) hoặc phần tử bao bảng
 * @param options object trả về từ useVxeTable
 */
export function useGridPager(target: MaybeComputedElementRef, options: Pick<VxeGridProps, 'pagerConfig'>) {
	useResizeObserver(target, ([entry]) => {
		const width = entry?.contentRect.width ?? 0;
		const pager = options.pagerConfig;
		if (!width || !pager) return;
		const layouts = width >= 900 ? FULL : width >= 640 ? MEDIUM : COMPACT;
		const pagerCount = width >= 900 ? 7 : 5;
		// Chỉ gán khi đổi mức, tránh vẽ lại bảng mỗi lần kéo; gán object mới để vxe-grid chắc chắn nhận thay đổi.
		if (pager.layouts !== layouts || pager.pagerCount !== pagerCount) options.pagerConfig = { ...pager, layouts, pagerCount };
	});
}
