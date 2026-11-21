import { defineStore } from 'pinia';
import type { MenuOutput } from '/@/api-services/system/models/menu-output';

export const useMenuTabs = defineStore('menuTabs', {
	state: () => ({
		tabsByMenuId: {} as Record<string, MenuOutput[]>,
		/** Yêu cầu mở 1 tab của menu nhóm (liên kết từ trang khác); tabPage của menu đó chọn tab rồi xoá yêu cầu. */
		pendingTab: null as { menuId: string; tab: string } | null,
	}),
	actions: {
		setMenuTabs(tabsByMenuId: Record<string, MenuOutput[]>) {
			this.tabsByMenuId = tabsByMenuId;
		},
		/** Ghi yêu cầu mở tab `tab` (SysMenu.Name hoặc ID) của menu nhóm `menuId`; gọi trước khi điều hướng tới menu nhóm. */
		requestTab(menuId: string, tab: string) {
			this.pendingTab = { menuId, tab };
		},
	},
});
