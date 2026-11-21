import { reactive } from 'vue';
import { useMenuTabs } from '/@/stores/menuTabs';
import { useRequestOldRoutes } from '/@/stores/requestOldRoutes';
import { findMenuPage } from '/@/utils/menuTabs';

/**
 * Author: KienLT
 * Description: Điều phối giữa các tab của trang Zone. Các tab là tab menu của khung tab chung (views/tabPage) nên không có
 * trang chủ riêng: trạng thái nằm ở biến dùng chung của module này, tab đích tìm theo component trong cây menu rồi nhờ
 * khung tab chọn tab (menuTabs.requestTab).
 *   - open: nút "Gán thiết bị" / "Tốc độ" trên dòng của tab Danh sách ⇒ mở tab đó với zone chọn sẵn.
 *   - markChanged: tab Danh sách vừa thêm/sửa/xoá zone ⇒ các tab khác nạp lại ở lần được mở kế tiếp.
 * Created date: 13/11/2026
 */
export type ZoneTabKind = 'mapping' | 'speed';

export interface ZoneTabFocus {
	zoneId: string;
	/** Tăng mỗi lần yêu cầu ⇒ chọn lại cùng một zone vẫn kích hoạt watch. */
	nonce: number;
}

/** Component của từng tab (đúng giá trị khai ở SysMenu). */
export const ZONE_TAB_COMPONENTS: Record<ZoneTabKind | 'list', string> = {
	list: 'tms/zone/component/zoneTreeTab',
	mapping: 'tms/zone/component/zoneMappingTab',
	speed: 'tms/zone/component/zoneSpeedTab',
};

const state = reactive({
	focus: { mapping: null, speed: null } as Record<ZoneTabKind, ZoneTabFocus | null>,
	/** Tăng mỗi lần danh sách zone đổi; tab so với bản đã nạp để biết có cần nạp lại. */
	version: 0,
});
let nonce = 0;

export function useZoneTabNavigation() {
	const menuTabs = useMenuTabs();
	const oldRoutes = useRequestOldRoutes();
	/** Chọn tab theo loại; vai trò không được cấp tab đó ⇒ false. */
	const select = (kind: ZoneTabKind | 'list') => {
		const page = findMenuPage(oldRoutes.requestOldRoutes as any[], ZONE_TAB_COMPONENTS[kind]);
		if (!page?.menuId || !page.tab) return false;
		menuTabs.requestTab(page.menuId, page.tab);
		return true;
	};
	return {
		state,
		select,
		open(kind: ZoneTabKind, zoneId: string) {
			state.focus[kind] = { zoneId, nonce: ++nonce };
			if (!select(kind)) state.focus[kind] = null;
		},
		markChanged() {
			state.version++;
		},
	};
}
