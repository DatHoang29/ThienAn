import { ref } from 'vue';
import type { Ref } from 'vue';
import { useMenuTabs } from '/@/stores/menuTabs';
import { useRequestOldRoutes } from '/@/stores/requestOldRoutes';
import { findMenuPage } from '/@/utils/menuTabs';

/**
 * Author: KienLT
 * Description: Tab Sự kiện mở đúng một sự cố bên tab Sự cố. Hai tab là tab menu của khung tab chung (views/tabPage) nên
 * không còn trang chủ riêng để cấp (provide) điều phối: yêu cầu nằm ở biến dùng chung của module này, tab Sự cố tìm theo
 * component trong cây menu rồi nhờ khung tab chọn tab (menuTabs.requestTab).
 * Created date: 21/10/2026 — KienLT 13/11/2026: bỏ provide/inject, chạy được khi menu trỏ thẳng tabPage/index.
 */
export type IncidentFocusTarget = { id: string; code?: string | null; startDate?: Date | string | null };

export type IncidentFocusRequest = IncidentFocusTarget & { nonce: number };

// 'missing': không có tab Sự cố (menu/quyền); 'busy': giữ cho mã cũ đang so sánh, không còn được trả về.
export type IncidentOpenResult = 'opened' | 'missing' | 'busy';

export type IncidentNavigation = {
	request: Readonly<Ref<IncidentFocusRequest | null>>;
	openIncident: (target: IncidentFocusTarget) => IncidentOpenResult;
};

const INCIDENT_TAB_COMPONENT = 'tms/incidentNew/incident/IncidentManage';
const request = ref<IncidentFocusRequest | null>(null);
let nonce = 0;

export function useIncidentNavigation(): IncidentNavigation {
	const menuTabs = useMenuTabs();
	const oldRoutes = useRequestOldRoutes();
	return {
		request,
		openIncident(target) {
			const page = findMenuPage(oldRoutes.requestOldRoutes as any[], INCIDENT_TAB_COMPONENT);
			if (!page?.menuId || !page.tab) return 'missing';
			request.value = { ...target, nonce: ++nonce };
			menuTabs.requestTab(page.menuId, page.tab); // khung tab chung chọn tab rồi tự xoá yêu cầu
			return 'opened';
		},
	};
}
