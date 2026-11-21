import { nextTick, defineAsyncComponent } from 'vue';
import type { App } from 'vue';
import * as svg from '@element-plus/icons-vue';
import router from '/@/router/index';
import pinia from '/@/stores/index';
import { storeToRefs } from 'pinia';
import { useThemeConfig } from '/@/stores/themeConfig';
import { i18n } from '/@/i18n/index';
import { Local } from '/@/utils/storage';
import { verifyUrl } from '/@/utils/toolsValidate';

// Import component
const SvgIcon = defineAsyncComponent(() => import('/@/components/svgIcon/index.vue'));

/**
 * Export globally registered Element Plus SVG icons
 * @param app Vue instance
 * @description Usage: https://element-plus.gitee.io/zh-CN/component/icon.html
 */
export function elSvg(app: App) {
	const icons = svg as any;
	for (const i in icons) {
		app.component(`ele-${icons[i].name}`, icons[i]);
	}
	app.component('SvgIcon', SvgIcon);
}

/**
 * Set browser title internationalization
 * @method const title = useTitle(); ==> title()
 */
export function useTitle() {
	const stores = useThemeConfig(pinia);
	const { themeConfig } = storeToRefs(stores);
	nextTick(() => {
		let webTitle = '';
		let globalTitle: string = themeConfig.value.globalTitle;
		const { path, meta } = router.currentRoute.value;
		if (path === '/login') {
			webTitle = <string>meta.title;
		} else {
			webTitle = setTagsViewNameI18n(router.currentRoute.value);
		}
		// Tránh hiển thị "undefined"/"null" trên tiêu đề khi route (vd: dashboard động) không có tên/i18n hợp lệ.
		// Ưu tiên meta.title, nếu vẫn không có thì fallback "Dashboard".
		if (!webTitle || webTitle === 'undefined' || webTitle === 'null') {
			webTitle = (meta.title as string) || 'Dashboard';
		}
		const appName = window.__env__?.VITE_APP_NAME || '';
		if (globalTitle != null && globalTitle !== '')
			document.title = appName ? `${appName} | ${webTitle} - ${globalTitle}` : `${webTitle} - ${globalTitle}`;
		else
			document.title = appName ? `${appName} | ${webTitle}` : webTitle;

	});
}

/**
 * Set custom tagsView name and internationalization for custom tagsView name
 * @param params tagsViewName in route query or params
 * @returns Returns the current tagsViewName
 */
export function setTagsViewNameI18n(item: any) {
	let tagsViewName: string = '';
	const { query, params, meta } = item;
	// Fix tagsViewName matching other routes containing the following words
	const pattern = /^\{("(vi-VN|en-US)":"[^,]+",?){1,3}}$/;
	if (query?.tagsViewName || params?.tagsViewName) {
		if (pattern.test(query?.tagsViewName) || pattern.test(params?.tagsViewName)) {
			// Internationalization
			const urlTagsParams = (query?.tagsViewName && JSON.parse(query?.tagsViewName)) || (params?.tagsViewName && JSON.parse(params?.tagsViewName));
			// Nếu locale hiện tại không có trong JSON, thử locale còn lại thay vì để undefined.
			tagsViewName = urlTagsParams[i18n.global.locale.value] || Object.values(urlTagsParams)[0] || '';
		} else {
			// Non-internationalization
			tagsViewName = query?.tagsViewName || params?.tagsViewName;
		}
	} else {
		// Non-custom tagsView name
		// SONTH: i18n menu name
		tagsViewName = i18n.global.t(`lz.router.${item.name}`, meta.title);
	}
	// Không bao giờ trả về undefined/null để tránh lọt chuỗi "undefined" vào tiêu đề trang.
	return tagsViewName == null ? '' : String(tagsViewName);
}

/**
 * Image lazy loading
 * @param el DOM target element
 * @param arr List data
 * @description data-xxx attributes are used to store private custom data of the page or application
 */
export const lazyImg = (el: string, arr: EmptyArrayType) => {
	const io = new IntersectionObserver((res) => {
		res.forEach((v: any) => {
			if (v.isIntersecting) {
				const { img, key } = v.target.dataset;
				v.target.src = img;
				v.target.onload = () => {
					io.unobserve(v.target);
					arr[key]['loading'] = false;
				};
			}
		});
	});
	nextTick(() => {
		document.querySelectorAll(el).forEach((img) => io.observe(img));
	});
};

/**
 * Global component size
 * @returns Returns the cached value `globalComponentSize` read from `window.localStorage`
 */
export const globalComponentSize = (): string => {
	const stores = useThemeConfig(pinia);
	const { themeConfig } = storeToRefs(stores);
	return Local.get('themeConfig')?.globalComponentSize || themeConfig.value?.globalComponentSize;
};

/**
 * Deep clone an object
 * @param obj Source object
 * @returns Cloned object
 */
export function deepClone(obj: EmptyObjectType) {
	let newObj: EmptyObjectType;
	try {
		newObj = obj.push ? [] : {};
	} catch (error) {
		newObj = {};
	}
	for (let attr in obj) {
		if (obj[attr] && typeof obj[attr] === 'object') {
			newObj[attr] = deepClone(obj[attr]);
		} else {
			newObj[attr] = obj[attr];
		}
	}
	return newObj;
}

/**
 * Determine if it is a mobile device
 */
export function isMobile() {
	if (navigator.userAgent.match(/('phone|pad|pod|iPhone|iPod|ios|iPad|Android|Mobile|BlackBerry|IEMobile|MQQBrowser|JUC|Fennec|wOSBrowser|BrowserNG|WebOS|Symbian|Windows Phone')/i)) {
		return true;
	} else {
		return false;
	}
}

/**
 * Check if all properties in an array of objects are empty, and delete the current row object if empty
 * @description @Thanks to Da Huang
 * @param list Array of objects
 * @returns Array of objects after removing empty values
 */
export function handleEmpty(list: EmptyArrayType) {
	const arr = [];
	for (const i in list) {
		const d = [];
		for (const j in list[i]) {
			d.push(list[i][j]);
		}
		const leng = d.filter((item) => item === '').length;
		if (leng !== d.length) {
			arr.push(list[i]);
		}
	}
	return arr;
}

/**
 * Open external link
 * @param val Current clicked menu item
 */
export function handleOpenLink(val: RouteItem) {
	const { origin, pathname } = window.location;
	router.push(val.path);
	//SONTH: open link in same tab with _self
	if (verifyUrl(<string>val.meta?.isLink)) window.open(val.meta?.isLink, '_self');
	else window.open(`${origin}${pathname}#${val.meta?.isLink}`, '_self');
}

/**
 * Unified batch export
 * @method elSvg Export globally registered Element Plus SVG icons
 * @method useTitle Set browser title internationalization
 * @method setTagsViewNameI18n Set custom tagsView name and internationalization for custom tagsView name
 * @method lazyImg Image lazy loading
 * @method globalComponentSize() Element Plus global component size
 * @method deepClone Deep clone an object
 * @method isMobile Determine if it is a mobile device
 * @method handleEmpty Check if all properties in an array of objects are empty, and delete the current row object if empty
 * @method handleOpenLink Open external link
 */
const other = {
	elSvg: (app: App) => {
		elSvg(app);
	},
	useTitle: () => {
		useTitle();
	},
	setTagsViewNameI18n(route: RouteToFrom) {
		return setTagsViewNameI18n(route);
	},
	lazyImg: (el: string, arr: EmptyArrayType) => {
		lazyImg(el, arr);
	},
	globalComponentSize: () => {
		return globalComponentSize();
	},
	deepClone: (obj: EmptyObjectType) => {
		return deepClone(obj);
	},
	isMobile: () => {
		return isMobile();
	},
	handleEmpty: (list: EmptyArrayType) => {
		return handleEmpty(list);
	},
	handleOpenLink: (val: RouteItem) => {
		handleOpenLink(val);
	},
};

// Unified batch export
export default other;
