import { RouteRecordRaw } from 'vue-router';
import pinia from '/@/stores/index';
import { useUserInfo } from '/@/stores/userInfo';
import { useRequestOldRoutes } from '/@/stores/requestOldRoutes';
import { Session } from '/@/utils/storage';
import { NextLoading } from '/@/utils/loading';
import { dynamicRoutes, notFoundAndNoPower } from '/@/router/route';
import { formatTwoStageRoutes, formatFlatteningRoutes, router } from '/@/router/index';
import { useRoutesList } from '/@/stores/routesList';
import { useTagsViewRoutes } from '/@/stores/tagsViewRoutes';
import { useMenuTabs } from '/@/stores/menuTabs';
import { splitMenuTree } from '/@/utils/menuTabs';
import { createTabPageRouteView } from '/@/router/tabPageView';
import type { TabPageViewLoader } from '/@/router/tabPageView';

import { getAPI } from '/@/utils/axios-utils';
import { SysMenuApi } from '/@/api-services/system';
// import { ElMessage } from 'element-plus';

// Backend controlled routes

/**
 * Get all .vue, .tsx files in the directory
 * @method import.meta.glob
 * @link Reference: https://cn.vitejs.dev/guide/features.html#json
 */
const layouModules: any = import.meta.glob('../layout/routerView/*.{vue,tsx}');
const viewsModules: any = import.meta.glob(['../views/**/*.{vue,tsx}', '!../views/system/old/**', '!../views/cfgSystem/old/**', '!../views/tms/old/**', '!../views/report/tms/old/**', '!../views/vms/old/**', '!../views/report/lcs/old/**', '!../views/report/vms/old/**', '!../views/report/fms/old/**', '!../views/sharedata/old/**']);
const dynamicViewsModules: Record<string, Function> = Object.assign({}, { ...layouModules }, { ...viewsModules });

/**
 * Backend controlled routes: initialization method, prevent route loss when refreshing
 * @method NextLoading Interface loading animation starts executing
 * @method useUserInfo().setUserInfos() Trigger the initialization of user information pinia
 * @method useRequestOldRoutes().setRequestOldRoutes() Store the original route of the interface (unprocessed component), choose to use it according to the needs
 * @method setAddRoute Add dynamic routes
 * @method setFilterMenuAndCacheTagsViewRoutes Set routes to pinia routesList (processed into multi-level nested routes) and cache the one-dimensional array after processing the multi-level nested array
 */
export async function initBackEndControlRoutes() {
	// Interface loading animation starts executing
	if (window.nextLoading === undefined) NextLoading.start();
	// No token, stop executing the next step
	if (!Session.get('token')) return false;
	// Trigger the initialization of user information pinia
	// https://gitee.com/lyt-top/vue-next-admin/issues/I5F1HP
	await useUserInfo().setUserInfos();
	// await useUserInfo().setConstList();
	// await useUserInfo().setDictList();
	// Get route menu data
	const res = await getBackEndControlRoutes();
	const { routes, tabsByMenuId } = splitMenuTree(res ?? []);
	useMenuTabs(pinia).setMenuTabs(tabsByMenuId);
	// Add judgment when there is no login permission
	// https://gitee.com/lyt-top/vue-next-admin/issues/I64HVO
	if (routes.length <= 0) return Promise.resolve(true);
	// Store the original route of the interface (unprocessed component), choose to use it according to the needs
	useRequestOldRoutes().setRequestOldRoutes(res as string[]);

	// Process routes (component), replace the routes of the first top-level children of dynamicRoutes (/@/router/route)
	dynamicRoutes[0].children = [
		// Dùng cây đã tách node `tab`/`btn` (splitMenuTree), không dùng `res` gốc: tab không phải route/menu sidebar.
		...backEndComponent(routes),
		// SonTH: Màn hình quản lý thông báo — truy cập từ chuông thông báo trên topBar.
		// isHide: không cần cấu hình menu backend, vào trực tiếp qua /notification (xem views/notification/index.vue).
		{
			path: '/notification',
			name: 'notification',
			component: () => import('/@/views/notification/index.vue'),
			meta: {
				title: 'lz.label.noti.management',
				isKeepAlive: true,
				isHide: true,
				icon: 'ele-Bell',
			},
		},
	];
	// Add dynamic routes
	await setAddRoute();
	// Set routes to pinia routesList (processed into multi-level nested routes) and cache the one-dimensional array after processing the multi-level nested array
	setFilterMenuAndCacheTagsViewRoutes();
}

/**
 * Set routes to pinia routesList (processed into multi-level nested routes) and cache the one-dimensional array after processing the multi-level nested array
 * @description Used for the display of the left menu and horizontal menu
 * @description Used in tagsView, menu search: unfiltered hidden (isHide)
 */
export async function setFilterMenuAndCacheTagsViewRoutes() {
	const storesRoutesList = useRoutesList(pinia);
	storesRoutesList.setRoutesList(dynamicRoutes[0].children as any);
	setCacheTagsViewRoutes();
}

/**
 * Cache the one-dimensional array after processing the multi-level nested array
 * @description Used in tagsView, menu search: unfiltered hidden (isHide)
 */
export function setCacheTagsViewRoutes() {
	const storesTagsView = useTagsViewRoutes(pinia);
	storesTagsView.setTagsViewRoutes(formatTwoStageRoutes(formatFlatteningRoutes(dynamicRoutes))[0].children);
}

/**
 * Process route format and add capture all routes or 404 Not found routes
 * @description Replace the routes of the first top-level children of dynamicRoutes (/@/router/route)
 * @returns Return the replaced route array
 */
export function setFilterRouteEnd() {
	let filterRouteEnd: any = formatTwoStageRoutes(formatFlatteningRoutes(dynamicRoutes));
	// notFoundAndNoPower prevents 404, 401 from being in the layout layout, if not set, the 404, 401 interface will be displayed full screen
	// Related issue No match found for location with path 'xxx'
	filterRouteEnd[0].children = [...filterRouteEnd[0].children, ...notFoundAndNoPower];
	return filterRouteEnd;
}

/**
 * Add dynamic routes
 * @method router.addRoute
 * @description This loop is for the one-dimensional array of routes of the first top-level children of dynamicRoutes (/@/router/route), not multi-level nested
 * @link Reference: https://next.router.vuejs.org/zh/api/#addroute
 */
export async function setAddRoute() {
	await setFilterRouteEnd().forEach((route: RouteRecordRaw) => {
		router.addRoute(route);
	});
}

/**
 * Request backend route menu interface
 * @description isRequestRoutes is true, then enable backend control routing
 * @returns Return backend route menu data
 */
export async function getBackEndControlRoutes() {
	var res = await getAPI(SysMenuApi).apiSystemSysmenuLoginmenutreeGet();
	// if (res.data.result == undefined || res.data.result.length < 1) {
	// 	ElMessage.error('No menu permissions, please contact the administrator!');
	// 	setTimeout(() => {
	// 		Session.removeToken();
	// 		window.location.reload();
	// 	}, 3000);
	// }
	return res.data.result;
}

/**
 * Re-request backend route menu interface
 * @description Used to refresh the menu in the menu management interface (not tested)
 * @description Path: /src/views/system/menu/component/addMenu.vue
 */
export async function setBackEndControlRefreshRoutes() {
	await getBackEndControlRoutes();
}

/**
 * Backend route component conversion
 * @param routes Backend returned route table array
 * @returns Return the component after processing into a function
 */
export function backEndComponent(routes: any) {
	if (!routes) return;
	return routes.map((item: any) => {
		const component = item.component ? dynamicImport(dynamicViewsModules, item.component as string) : item.component;
		const isTabPage = typeof component === 'function' && component === viewsModules['../views/tabPage/index.vue'];
		return {
			...item,
			path: item.path || '',
			meta: { ...item.meta, menuId: item.id },
			component: isTabPage ? createTabPageRouteView(component as TabPageViewLoader, item) : component,
			children: item.children ? backEndComponent(item.children) : undefined,
		};
	});
}

/**
 * Backend route component conversion function
 * @param dynamicViewsModules Get all .vue, .tsx files in the directory
 * @param component Current item to be processed component
 * @returns Return the component after processing into a function
 */
export function dynamicImport(dynamicViewsModules: Record<string, Function>, component: string) {
	const keys = Object.keys(dynamicViewsModules);
	const matchKeys = keys.filter((key) => {
		const k = key.replace(/..\/views|../, '');
		return k.startsWith(`${component}`) || k.startsWith(`/${component}`);
	});
	if (matchKeys?.length === 1) {
		const matchKey = matchKeys[0];
		return dynamicViewsModules[matchKey];
	}
	if (matchKeys?.length > 1) {
		return false;
	}
}
