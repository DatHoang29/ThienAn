import { createRouter, createWebHashHistory, createWebHistory } from 'vue-router';
import NProgress from 'nprogress';
import 'nprogress/nprogress.css';
import pinia from '/@/stores/index';
import { storeToRefs } from 'pinia';
import { useKeepALiveNames } from '/@/stores/keepAliveNames';
import { useRoutesList } from '/@/stores/routesList';
import { useThemeConfig } from '/@/stores/themeConfig';
import { Session } from '/@/utils/storage';
import { staticRoutes, notFoundAndNoPower } from '/@/router/route';
import { initFrontEndControlRoutes } from '/@/router/frontEnd';
import { initBackEndControlRoutes } from '/@/router/backEnd';
import VxeUI from 'vxe-table';
import { useUserInfo } from '/@/stores/userInfo';
import { SUPPORT_LOCALES } from '/@/i18n/i18n';
import { loadLocaleMessages, setI18nLanguage } from '/@/i18n/i18n';
import { i18n } from '../i18n';
import { toPairs } from 'lodash-es';
/**
 * 1. When controlling routes on the front end: isRequestRoutes is false, roles need to be written, and the setFilterRoute method needs to be used.
 * 2. When controlling routes on the back end: isRequestRoutes is true, roles do not need to be written, and the setFilterRoute method does not need to be used.
 * Related methods have been separated into the corresponding `backEnd.ts` and `frontEnd.ts` (they do not affect each other and do not need to be modified in both files simultaneously).
 * Special notes:
 * 1. Front-end control: The route menu is written by the front end (there is no menu management interface, but there is a role management interface), and the role management has a roles attribute that needs to be returned to userInfo.
 * 2. Back-end control: The route menu is returned by the back end (there is a menu management interface and a role management interface).
 */

// Read whether back-end control routing configuration is enabled from `/src/stores/themeConfig.ts`
const storesThemeConfig = useThemeConfig(pinia);
const { themeConfig } = storeToRefs(storesThemeConfig);
const { isRequestRoutes } = themeConfig.value;
const userInfo = useUserInfo();

/**
 * Create a router instance that can be used by the Vue application
 * @method createRouter(options: RouterOptions): Router
 * @link Reference: https://next.router.vuejs.org/zh/api/#createrouter
 */
export const router = createRouter({
	history: createWebHashHistory(),
	/**
	 * Explanation:
	 * 1. notFoundAndNoPower adds 404 and 401 pages by default to prevent continuous prompts of "No match found for location with path 'xxx'".
	 * 2. The notFoundAndNoPower 404 and 401 pages also need to be added in backEnd.ts (back-end controlled routes) and frontEnd.ts (front-end controlled routes).
	 *    To prevent 404 and 401 from being outside the layout, if not set, the 404 and 401 pages will be displayed in full screen.
	 */
	routes: [...notFoundAndNoPower, ...staticRoutes],
});

/**
 * Process a multi-level nested array of routes into a one-dimensional array
 * @param arr The array of route menu data to be passed in
 * @returns Returns the processed one-dimensional route menu array
 */
export function formatFlatteningRoutes(arr: any) {
	if (arr.length <= 0) return false;
	for (let i = 0; i < arr.length; i++) {
		if (arr[i].children) {
			arr = arr.slice(0, i + 1).concat(arr[i].children, arr.slice(i + 1));
		}
	}
	return arr;
}

/**
 * Process a one-dimensional array into a multi-level nested array (only keep two levels: all levels above two are processed into only two levels, keep-alive supports two-level caching)
 * @description isKeepAlive processes the `name` value for caching. The top level is closed, and all are not cached.
 * @link Reference: https://v3.cn.vuejs.org/api/built-in-components.html#keep-alive
 * @param arr The processed one-dimensional route menu array
 * @returns Returns the one-dimensional array reprocessed into the format of `defining dynamic routes (dynamicRoutes)`
 */
export function formatTwoStageRoutes(arr: any) {
	if (arr.length <= 0) return false;
	const newArr: any = [];
	const cacheList: Array<string> = [];
	arr.forEach((v: any) => {
		if (v.path == null || v.path == undefined) return;

		if (v.path === '/') {
			newArr.push({ component: v.component, name: v.name, path: v.path, redirect: v.redirect, meta: v.meta, children: [] });
		} else {
			// Determine if it is a dynamic route (xx/:id/:name), used in tagsView, etc.
			// Fix: https://gitee.com/lyt-top/vue-next-admin/issues/I3YX6G
			if (v.path.indexOf('/:') > -1) {
				v.meta['isDynamic'] = true;
				v.meta['isDynamicPath'] = v.path;
			}
			newArr[0].children.push({ ...v });
			// Store the name value for use in keep-alive include, to achieve route caching
			// Path: /@/layout/routerView/parent.vue
			if (newArr[0].meta.isKeepAlive && v.meta.isKeepAlive) {
				cacheList.push(v.name);
			}
		}
	});
	const stores = useKeepALiveNames(pinia);
	stores.setCacheKeepAlive(cacheList);
	return newArr;
}

// Before loading the route
router.beforeEach(async (to, from, next) => {
	NProgress.configure({ showSpinner: false });

	/** 
	 * SonTH: Check and change language
	 */
	let paramsLocale = themeConfig.value.globalI18n;
	// let paramsLocale = to.params.locale
	if (paramsLocale == undefined) {
		paramsLocale = 'vi-VN';
	}
	// load locale messages
	if (i18n.global.availableLocales.includes(paramsLocale as string)) {
		await loadLocaleMessages(i18n, paramsLocale as string)
	}
	// set i18n language
	setI18nLanguage(i18n, paramsLocale as string)
	/** 
	 * End check and change language
	 */

	if (to.meta.title) NProgress.start();
	const token = Session.get('token');
	//Nếu truy cập login và không có token thì cho phép truy cập
	if ((to.path === '/login' || to.path === '/login2' || to.path === '/login3' || to.path === '/login4') && !token) {
		next();
		NProgress.done();
	} else {
		//Nếu không có token thì chuyển hướng đến login
		if (!token) {
			switch (to.path) {
				case '/login2':
					next(`/login2?redirect=${to.path}&params=${JSON.stringify(to.query ? to.query : to.params)}`);
					break;
				case '/login3':
					next(`/login3?redirect=${to.path}&params=${JSON.stringify(to.query ? to.query : to.params)}`);
					break;
				case '/login4':
					next(`/login4?redirect=${to.path}&params=${JSON.stringify(to.query ? to.query : to.params)}`);
					break;
				default:
					next(`/login?redirect=${to.path}&params=${JSON.stringify(to.query ? to.query : to.params)}`);
			}
			Session.clear();
			NProgress.done();
		} else if (token && to.path === '/login') {
			//Nếu có token và truy cập login thì chuyển hướng đến home
			next('/dashboard/home');
			NProgress.done();
		} else {
			//Nếu có token và không truy cập login thì kiểm tra route
			const storesRoutesList = useRoutesList(pinia);
			const { routesList } = storeToRefs(storesRoutesList);
			// console.log(userInfo.userInfos);
			if (routesList.value.length === 0) {
				if (isRequestRoutes) {
					// Back-end controlled routes: route data initialization to prevent loss during refresh
					await initBackEndControlRoutes();
					//SONTH: Nếu không có route thì chuyển hướng đến defaultMenu
					//Lấy defaultMenu từ userInfo
					// console.log(userInfo.userInfos);
					var defaultMenu = userInfo.userInfos.defaultMenu;
					//Kiểm tra defaultMenu có tồn tại không
					if (defaultMenu && to.path == '/') {
						//Nếu có defaultMenu thì chuyển hướng đến defaultMenu
						to.path = defaultMenu;
					}
					// }
					// to.query prevents loss of parameters when refreshing normal routes with parameters. Dynamic routes (xxx/:id/:name) do not need to be processed.
					next({ path: to.path, query: to.query });
				} else {
					// https://gitee.com/lyt-top/vue-next-admin/issues/I5F1HP
					await initFrontEndControlRoutes();
					next({ path: to.path, query: to.query });
				}
			} else {
				//SONTH: Nếu không có route thì chuyển hướng đến dashboard/home
				if (to.path == '/') {
					next('/dashboard/home');
				} else {
					//Nếu có route thì chuyển hướng đến route
					next();
				}
			}
		}

		//Set theme vxe-table
		if (themeConfig.value.isIsDark) VxeUI.setTheme('dark');
		else VxeUI.setTheme('light'); // set theme to light mode vxe-table

	}
});

// After loading the route
router.afterEach(() => {
	NProgress.done();
});

// Export the router
export default router;
