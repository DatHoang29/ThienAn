import { RouteRecordRaw } from 'vue-router';

/**
 * Suggestion: The route path should match the folder name for easy file location via the browser address.
 *
 * Explanation of the route meta object parameters
 * meta: {
 *      title:          Menu bar and tagsView bar, menu search name (internationalization)
 *      isLink:        Whether the menu is a hyperlink, conditions for external links: `1. isLink: link address is not empty 2. isIframe: false`
 *      isHide:        Whether to hide this route
 *      isKeepAlive:   Whether to cache component state
 *      isAffix:       Whether to fix it in the tagsView bar
 *      isIframe:      Whether to embed a window, conditions for enabling: `1. isIframe: true 2. isLink: link address is not empty`
 *      roles:         Current route permission identifier, taken from role management. Controls route visibility. Super administrator: admin, normal role: common
 *      icon:          Menu, tagsView icon, for Ali: add `iconfont xxx`, for FontAwesome: add `fa xxx`
 * }
 */

// Extend the RouteMeta interface
declare module 'vue-router' {
	interface RouteMeta {
		title?: string;
		isLink?: string;
		isHide?: boolean;
		isKeepAlive?: boolean;
		isAffix?: boolean;
		isIframe?: boolean;
		roles?: string[];
		icon?: string;
		isFullscreen?: boolean;
	}
}

/**
 * Define dynamic routes
 * To add routes on the frontend, please add them in the `children array` of the top-level node
 * @description Used when isRequestRoutes is not set to true (frontend controls routes), when enabled, the first top-level children's route will be replaced with the route data returned from the API request
 * @description Please refer to the ruleForm in `/@/views/system/menu/component/addMenu.vue`
 * @returns Returns route menu data
 */
export const dynamicRoutes: Array<RouteRecordRaw> = [
	{
		path: '/',
		name: '/',
		component: () => import('/@/layout/index.vue'),
		redirect: '/dashboard/home',
		meta: {
			isKeepAlive: true,
		},
		children: [
			{
				path: '/tms/dutySchedule',
				name: 'tmsDutySchedule',
				component: () => import('/@/views/tms/dutySchedule/index.vue'),
				meta: {
					title: 'lz.label.tmsDutySchedule',
					isKeepAlive: true,
					icon: 'ele-Calendar',
					roles: ['admin', 'common'],
				},
			},
		],
	},

	// {
	// 	path: '/platform/job/dashboard',
	// 	name: 'jobDashboard',
	// 	component: () => import('/@/views/system/job/dashboard.vue'),
	// 	meta: {
	// 		title: 'Task Dashboard',
	// 		isLink: window.__env__.VITE_API_URL + '/schedule',
	// 		isHide: true,
	// 		isKeepAlive: true,
	// 		isAffix: false,
	// 		isIframe: true,
	// 		icon: 'ele-Clock',
	// 	},
	// },
	// {
	// 	path: '/develop/database/visual',
	// 	name: 'databaseVisual',
	// 	component: () => import('/@/views/system/database/component/visualTable.vue'),
	// 	meta: {
	// 		title: 'Database Visualization',
	// 		isHide: true,
	// 		isKeepAlive: true,
	// 		isAffix: false,
	// 		// isIframe: true,
	// 		icon: 'ele-View',
	// 	},
	// },
];

/**
 * Define 404 and 401 pages
 * @link Reference: https://next.router.vuejs.org/zh/guide/essentials/history-mode.html#netlify
 */
export const notFoundAndNoPower = [
	{
		path: '/:path(.*)*',
		name: 'notFound',
		component: () => import('/@/views/error/404.vue'),
		meta: {
			title: 'lz.router.notFound',
			isHide: true,
		},
	},
	{
		path: '/401',
		name: 'noPower',
		component: () => import('/@/views/error/401.vue'),
		meta: {
			title: 'lz.router.noPower',
			isHide: true,
		},
	},
];

/**
 * Define static routes (default routes)
 * Do not modify this route, if adding routes on the frontend, please add them in the `dynamicRoutes array`
 * @description For frontend control, directly modify the routes in dynamicRoutes, backend control does not need modification, when requesting API route data, it will overwrite the content of the first top-level children in dynamicRoutes (fullscreen, not including the route outlet in the layout)
 * @returns Returns route menu data
 */
export const staticRoutes: Array<RouteRecordRaw> = [
	{
		path: '/login',
		name: 'login',
		component: () => import('/@/views/login/index.vue'),
		meta: {
			title: 'Login',
		},
	},
	{
		// MẪU 1 - Ưu tiên 1: Emerald Glassmorphism
		path: '/login2',
		name: 'login2',
		component: () => import('/@/views/login/index-v1.vue'),
		meta: {
			title: 'Login',
		},
	},
	{
		// MẪU 2 - Ưu tiên 2: Split Screen 55/45
		path: '/login3',
		name: 'login3',
		component: () => import('/@/views/login/index-v2.vue'),
		meta: {
			title: 'Login',
		},
	},
	{
		path: '/login4',
		name: 'login4',
		component: () => import('/@/views/login/index4.vue'),
		meta: {
			title: 'Login',
		},
	},
	/**
	 * Note: The ones written here are for fullscreen interfaces, not recommended to write here
	 * Please write in the `dynamicRoutes` route array
	 * SONTH: khai báo các đường dẫn chức năng full-screen tại đây
	 */
	{
		path: '/screen/trafficInfo',
		name: 'screen-trafficInfo',
		component: () => import('/@/views/bigScreen/trafficInfo/index.vue')
	}

];
