import { RouteRecordRaw } from 'vue-router';
import { storeToRefs } from 'pinia';
import { formatTwoStageRoutes, formatFlatteningRoutes, router } from '/@/router/index';
import { dynamicRoutes, notFoundAndNoPower } from '/@/router/route';
import pinia from '/@/stores/index';
import { Session } from '/@/utils/storage';
import { useUserInfo } from '/@/stores/userInfo';
import { useTagsViewRoutes } from '/@/stores/tagsViewRoutes';
import { useRoutesList } from '/@/stores/routesList';
import { NextLoading } from '/@/utils/loading';

// Frontend Route Control

/**
 * Frontend Route Control: Initialization method to prevent loss of routes on refresh
 * @method  NextLoading starts the loading animation on the interface
 * @method useUserInfo(pinia).setUserInfos() triggers the initialization of user information in pinia
 * @method setAddRoute adds dynamic routes
 * @method setFilterMenuAndCacheTagsViewRoutes sets recursive filtering of authorized routes into pinia routesList (processed into multi-level nested routes) and caches the one-dimensional array processed from the multi-level nested array
 */
export async function initFrontEndControlRoutes() {
	// Start the loading animation on the interface
	if (window.nextLoading === undefined) NextLoading.start();
	// Stop execution if there is no token
	if (!Session.get('token')) return false;
	// Trigger the initialization of user information in pinia
	// https://gitee.com/lyt-top/vue-next-admin/issues/I5F1HP
	await useUserInfo(pinia).setUserInfos();
	// Add dynamic routes
	await setAddRoute();
	// Set recursive filtering of authorized routes into pinia routesList (processed into multi-level nested routes) and cache the one-dimensional array processed from the multi-level nested array
	setFilterMenuAndCacheTagsViewRoutes();
}

/**
 * Add dynamic routes
 * @method router.addRoute
 * @description This loop is for the one-dimensional array of the first-level children of dynamicRoutes (/@/router/route), not multi-level nested
 * @link Reference: https://next.router.vuejs.org/zh/api/#addroute
 */
export async function setAddRoute() {
	await setFilterRouteEnd().forEach((route: RouteRecordRaw) => {
		router.addRoute(route);
	});
}

/**
 * Delete/Reset routes
 * @method router.removeRoute
 * @description This loop is for the one-dimensional array of the first-level children of dynamicRoutes (/@/router/route), not multi-level nested
 * @link Reference: https://next.router.vuejs.org/zh/api/#push
 */
export async function frontEndsResetRoute() {
	await setFilterRouteEnd().forEach((route: RouteRecordRaw) => {
		const routeName: any = route.name;
		router.hasRoute(routeName) && router.removeRoute(routeName);
	});
}

/**
 * Get the array of routes with the current user's permission identifiers and replace the original routes
 * @description Replace the routes of the first-level children of dynamicRoutes (/@/router/route)
 * @returns Returns the replaced array of routes
 */
export function setFilterRouteEnd() {
	let filterRouteEnd: any = formatTwoStageRoutes(formatFlatteningRoutes(dynamicRoutes));
	// notFoundAndNoPower prevents 404, 401 from being outside the layout, if not set, the 404, 401 interface will display full screen
	// Related issue No match found for location with path 'xxx'
	filterRouteEnd[0].children = [...setFilterRoute(filterRouteEnd[0].children), ...notFoundAndNoPower];
	return filterRouteEnd;
}

/**
 * Get the current user's permission identifiers to compare with the route table (not processed into multi-level nested routes)
 * @description This is mainly used for adding dynamic routes, router.addRoute
 * @link Reference: https://next.router.vuejs.org/zh/api/#addroute
 * @param chil the collection of sub-routes of the first-level children of dynamicRoutes (/@/router/route)
 * @returns Returns the array of routes with the current user's permission identifiers
 */
export function setFilterRoute(chil: any) {
	const stores = useUserInfo(pinia);
	const { userInfos } = storeToRefs(stores);
	let filterRoute: any = [];
	chil.forEach((route: any) => {
		if (route.meta.roles) {
			route.meta.roles.forEach((metaRoles: any) => {
				userInfos.value.roles.forEach((roles: any) => {
					if (metaRoles === roles) filterRoute.push({ ...route });
				});
			});
		}
	});
	return filterRoute;
}

/**
 * Cache the one-dimensional array processed from the multi-level nested array
 * @description Used in tagsView and menu search: not filtered hidden (isHide)
 */
export function setCacheTagsViewRoutes() {
	// Get the authorized routes, otherwise unauthorized routes will also be displayed in tagsView and menu search
	const stores = useUserInfo(pinia);
	const storesTagsView = useTagsViewRoutes(pinia);
	const { userInfos } = storeToRefs(stores);
	let rolesRoutes = setFilterHasRolesMenu(dynamicRoutes, userInfos.value.roles);
	// Add to pinia setTagsViewRoutes
	storesTagsView.setTagsViewRoutes(formatTwoStageRoutes(formatFlatteningRoutes(rolesRoutes))[0].children);
}

/**
 * Set recursive filtering of authorized routes into pinia routesList (processed into multi-level nested routes) and cache the one-dimensional array processed from the multi-level nested array
 * @description Used for displaying the left menu and horizontal menu
 * @description Used in tagsView and menu search: not filtered hidden (isHide)
 */
export function setFilterMenuAndCacheTagsViewRoutes() {
	const stores = useUserInfo(pinia);
	const storesRoutesList = useRoutesList(pinia);
	const { userInfos } = storeToRefs(stores);
	storesRoutesList.setRoutesList(setFilterHasRolesMenu(dynamicRoutes[0].children, userInfos.value.roles));
	setCacheTagsViewRoutes();
}

/**
 * Determine whether the current logged-in user's permission field is included in the route `meta.roles`
 * @param roles User permission identifiers, in the roles array of userInfos (user information) cached in the browser during login
 * @param route The current route item during the loop
 * @returns Returns the route item with permission after comparison
 */
export function hasRoles(roles: any, route: any) {
	if (route.meta && route.meta.roles) return roles.some((role: any) => route.meta.roles.includes(role));
	else return true;
}

/**
 * Get the current user's permission identifiers to compare with the route table and set recursive filtering of authorized routes
 * @param routes Current route children
 * @param roles User permission identifiers, in the roles array of userInfos (user information) cached in the browser during login
 * @returns Returns the array of authorized routes controlled by `meta.roles`
 */
export function setFilterHasRolesMenu(routes: any, roles: any) {
	const menu: any = [];
	routes.forEach((route: any) => {
		const item = { ...route };
		if (hasRoles(roles, item)) {
			if (item.children) item.children = setFilterHasRolesMenu(item.children, roles);
			menu.push(item);
		}
	});
	return menu;
}
