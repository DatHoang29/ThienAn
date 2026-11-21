<template>
	<div class="layout-navbars-tagsview" :class="{ 'layout-navbars-tagsview-shadow': getThemeConfig.layout === 'classic' }">
		<el-scrollbar ref="scrollbarRef" @wheel.prevent="onHandleScroll">
			<ul class="layout-navbars-tagsview-ul" :class="setTagsStyle" ref="tagsUlRef">
				<li
					v-for="(v, k) in state.tagsViewList"
					:key="k"
					class="layout-navbars-tagsview-ul-li"
					:data-url="v.url"
					:class="{ 'is-active': isActive(v) }"
					@contextmenu.prevent="onContextmenu(v, $event)"
					@mousedown="onMousedownMenu(v, $event)"
					@click="onTagsClick(v, k)"
					:ref="
						(el) => {
							if (el) tagsRefs[k] = el;
						}
					"
				>
					<i class="iconfont icon-webicon318 layout-navbars-tagsview-ul-li-iconfont" v-if="isActive(v)"></i>
					<SvgIcon :name="v.meta.icon" v-if="getThemeConfig.isTagsviewIcon" class="pr5" :size="18" />
					<span>{{ setTagsViewNameI18n(v) }}</span>
					<template v-if="isActive(v)">
						<SvgIcon name="ele-RefreshRight" class="ml5 layout-navbars-tagsview-ul-li-refresh" @click.stop="refreshCurrentTagsView($route.fullPath)" />
						<SvgIcon
							name="ele-Close"
							class="layout-navbars-tagsview-ul-li-icon layout-icon-active"
							v-if="!v.meta.isAffix"
							@click.stop="closeCurrentTagsView(getThemeConfig.isShareTagsView ? v.path : v.url)"
						/>
					</template>
					<SvgIcon
						name="ele-Close"
						class="layout-navbars-tagsview-ul-li-icon layout-icon-three"
						v-if="!v.meta.isAffix"
						@click.stop="closeCurrentTagsView(getThemeConfig.isShareTagsView ? v.path : v.url)"
					/>
				</li>
			</ul>
		</el-scrollbar>
		<Contextmenu :dropdown="state.dropdown" ref="contextmenuRef" @currentContextmenuClick="onCurrentContextmenuClick" />
	</div>
</template>

<script setup lang="ts" name="layoutTagsView">
import { defineAsyncComponent, reactive, onMounted, computed, ref, nextTick, onBeforeUpdate, onBeforeMount, onUnmounted, watch } from 'vue';
import { useRoute, useRouter, onBeforeRouteUpdate } from 'vue-router';
import Sortable from 'sortablejs';
import { collapseItemProps, ElMessage } from 'element-plus';
import { storeToRefs } from 'pinia';
import { useTagsViewRoutes } from '/@/stores/tagsViewRoutes';
import { useThemeConfig } from '/@/stores/themeConfig';
import { useKeepALiveNames } from '/@/stores/keepAliveNames';
import { useRoutesList } from '/@/stores/routesList';
import { Session } from '/@/utils/storage';
import { isObjectValueEqual } from '/@/utils/arrayOperation';
import other from '/@/utils/other';
import mittBus from '/@/utils/mitt';

// Import component
const Contextmenu = defineAsyncComponent(() => import('/@/layout/navBars/tagsView/contextmenu.vue'));

// Define variable content
const tagsRefs = ref<RefType>([]);
const scrollbarRef = ref();
const contextmenuRef = ref();
const tagsUlRef = ref();
const stores = useTagsViewRoutes();
const storesThemeConfig = useThemeConfig();
const storesTagsViewRoutes = useTagsViewRoutes();
const storesRoutesList = useRoutesList();
const { themeConfig } = storeToRefs(storesThemeConfig);
const { tagsViewRoutes } = storeToRefs(storesTagsViewRoutes);
const { routesList } = storeToRefs(storesRoutesList);
const storesKeepALiveNames = useKeepALiveNames();
const route = useRoute();
const router = useRouter();
const state = reactive<TagsViewState>({
	routeActive: '',
	routePath: route.path,
	dropdown: { x: '', y: '' },
	sortable: '',
	tagsRefsIndex: 0,
	tagsViewList: [],
	tagsViewRoutesList: [],
});

// Dynamically set tagsView style
const setTagsStyle = computed(() => {
	return themeConfig.value.tagsStyle;
});
// Get layout configuration information
const getThemeConfig = computed(() => {
	return themeConfig.value;
});
// Set custom tagsView name, custom tagsView name internationalization
const setTagsViewNameI18n = computed(() => {
	return (v: RouteItem) => {
		return other.setTagsViewNameI18n(v);
	};
});
// Set tagsView highlight
const isActive = (v: RouteItem) => {
	if (getThemeConfig.value.isShareTagsView) {
		return v.path === state.routePath;
	} else {
		if ((v.query && Object.keys(v.query).length) || (v.params && Object.keys(v.params).length)) {
			// Normal parameters
			return v.url ? v.url === state.routeActive : v.path === state.routeActive;
		} else {
			// Pass parameters by name, params value, refresh page parameters disappear
			return v.path === state.routePath;
		}
	}
};
// Store tagsViewList in the browser's temporary cache, keep records when the page refreshes
const addBrowserSetSession = (tagsViewList: Array<object>) => {
	Session.set('tagsViewList', tagsViewList);
};
// Get the tagsViewRoutes list from pinia
const getTagsViewRoutes = async () => {
	state.routeActive = await setTagsViewHighlight(route);
	state.routePath = (await route.meta.isDynamic) ? route.meta.isDynamicPath : route.path;
	state.tagsViewList = [];
	state.tagsViewRoutesList = tagsViewRoutes.value;
	initTagsView();
};
// Get route information from pinia: if it is set to fixed (isAffix), initialize display
const initTagsView = async () => {
	if (Session.get('tagsViewList') && getThemeConfig.value.isCacheTagsView) {
		state.tagsViewList = await Session.get('tagsViewList');
	} else {
		await state.tagsViewRoutesList.map((v: RouteItem) => {
			if (v.meta?.isAffix && !v.meta.isHide) {
				v.url = setTagsViewHighlight(v);
				state.tagsViewList.push({ ...v });
				storesKeepALiveNames.addCachedView(v);
			}
		});
		await addTagsView(route.path, <RouteToFrom>route);
	}
	// Initialize the index of the current element (li)
	getTagsRefsIndex(getThemeConfig.value.isShareTagsView ? state.routePath : state.routeActive);
	
};
// Handle multi-tag detail that can be opened, single tag detail (dynamic route (xxx/:id/:name), normal route processing)
const solveAddTagsView = async (path: string, to?: RouteToFrom) => {
	let isDynamicPath = to?.meta?.isDynamic ? to.meta.isDynamicPath : path;
	let current = state.tagsViewList.filter(
		(v: RouteItem) =>
			v.path === isDynamicPath &&
			isObjectValueEqual(to?.meta?.isDynamic ? (v.params ? v.params : null) : v.query ? v.query : null, to?.meta?.isDynamic ? (to?.params ? to?.params : null) : to?.query ? to?.query : null)
	);
	if (current.length <= 0) {
		// Prevent: Avoid app logic that relies on enumerating keys on a component instance. The keys will be empty in production mode to avoid performance overhead.
		let findItem = state.tagsViewRoutesList.find((v: RouteItem) => v.path === isDynamicPath);
		if (!findItem) return false;
		if (findItem.meta.isAffix) return false;
		if (findItem.meta.isLink && !findItem.meta.isIframe) return false;
		to?.meta?.isDynamic ? (findItem.params = to.params) : (findItem.query = to?.query);
		findItem.url = setTagsViewHighlight(findItem);
		state.tagsViewList.push({ ...findItem });
		await storesKeepALiveNames.addCachedView(findItem);
		addBrowserSetSession(state.tagsViewList);
	}
};
// Handle single tag when the second value does not overwrite the first tagsViewList value (Session Storage)
const singleAddTagsView = (path: string, to?: RouteToFrom) => {
	let isDynamicPath = to?.meta?.isDynamic ? to.meta.isDynamicPath : path;
	state.tagsViewList.forEach((v) => {
		if (
			v.path === isDynamicPath &&
			!isObjectValueEqual(to?.meta?.isDynamic ? (v.params ? v.params : null) : v.query ? v.query : null, to?.meta?.isDynamic ? (to?.params ? to?.params : null) : to?.query ? to?.query : null)
		) {
			to?.meta?.isDynamic ? (v.params = to.params) : (v.query = to?.query);
			v.url = setTagsViewHighlight(v);
			addBrowserSetSession(state.tagsViewList);
		}
	});
};
// 1. Add tagsView: not set to hide (isHide) also add to tagsView (can open multi-tag details, single tag details)
const addTagsView = (path: string, to?: RouteToFrom) => {
	// Prevent unable to obtain route information
	nextTick(async () => {
		// Fix: https://gitee.com/lyt-top/vue-next-admin/issues/I3YX6G
		let item: RouteItem;
		if (to?.meta?.isDynamic) {
			// Dynamic route (xxx/:id/:name): different parameters, open multiple tagsview
			if (!getThemeConfig.value.isShareTagsView) await solveAddTagsView(path, to);
			else await singleAddTagsView(path, to);
			if (state.tagsViewList.some((v: RouteItem) => v.path === to?.meta?.isDynamicPath)) {
				// Prevent tagsViewList from not being stored in the browser when first entering the interface (logging in)
				addBrowserSetSession(state.tagsViewList);
				return false;
			}
			item = state.tagsViewRoutesList.find((v: RouteItem) => v.path === to?.meta?.isDynamicPath);
		} else {
			// Normal route: different parameters, open multiple tagsview
			if (!getThemeConfig.value.isShareTagsView) await solveAddTagsView(path, to);
			else await singleAddTagsView(path, to);
			if (state.tagsViewList.some((v: RouteItem) => v.path === path)) {
				// Prevent tagsViewList from not being stored in the browser when first entering the interface (logging in)
				addBrowserSetSession(state.tagsViewList);
				return false;
			}
			item = state.tagsViewRoutesList.find((v: RouteItem) => v.path === path);
		}
		if (!item) return false;
		if (item?.meta?.isLink && !item.meta.isIframe) return false;
		if (to?.meta?.isDynamic) item.params = to?.params ? to?.params : route.params;
		else item.query = to?.query ? to?.query : route.query;
		item.url = setTagsViewHighlight(item);
		await storesKeepALiveNames.addCachedView(item);
		await state.tagsViewList.push({ ...item });
		await addBrowserSetSession(state.tagsViewList);
	});
};
// 2. Refresh current tagsView:
const refreshCurrentTagsView = async (fullPath: string) => {
	const decodeURIPath = decodeURI(fullPath);
	let item: RouteToFrom = {};
	state.tagsViewList.forEach((v: RouteItem) => {
		v.transUrl = transUrlParams(v);
		if (v.transUrl) {
			if (v.transUrl === transUrlParams(v)) item = v;
		} else {
			if (v.path === decodeURIPath) item = v;
		}
	});
	if (!item) return false;
	await storesKeepALiveNames.delCachedView(item);
	mittBus.emit('onTagsViewRefreshRouterView', fullPath);
	if (item.meta?.isKeepAlive) storesKeepALiveNames.addCachedView(item);
};
// 3. Close current tagsView: if set to fixed (isAffix), cannot be closed
const closeCurrentTagsView = (path: string) => {
	state.tagsViewList.map((v: RouteItem, k: number, arr: RouteItems) => {
		if (!v.meta?.isAffix) {
			if (getThemeConfig.value.isShareTagsView ? v.path === path : v.url === path) {
				storesKeepALiveNames.delCachedView(v);
				state.tagsViewList.splice(k, 1);
				setTimeout(() => {
					if (state.tagsViewList.length === k && getThemeConfig.value.isShareTagsView ? state.routePath === path : state.routeActive === path) {
						// Last one and highlighted
						if (arr[arr.length - 1].meta.isDynamic) {
							// Dynamic route (xxx/:id/:name)
							if (k !== arr.length) router.push({ name: arr[k].name, params: arr[k].params });
							else router.push({ name: arr[arr.length - 1].name, params: arr[arr.length - 1].params });
						} else {
							// Normal route
							if (k !== arr.length) router.push({ path: arr[k].path, query: arr[k].query });
							else router.push({ path: arr[arr.length - 1].path, query: arr[arr.length - 1].query });
						}
					} else {
						// Not the last one and highlighted, jump to the next
						if (state.tagsViewList.length !== k && getThemeConfig.value.isShareTagsView ? state.routePath === path : state.routeActive === path) {
							if (arr[k].meta.isDynamic) {
								// Dynamic route (xxx/:id/:name)
								router.push({ name: arr[k].name, params: arr[k].params });
							} else {
								// Normal route
								router.push({ path: arr[k].path, query: arr[k].query });
							}
						}
					}
				}, 0);
			}
		}
	});
	addBrowserSetSession(state.tagsViewList);
};
// 4. Close other tagsView: if set to fixed (isAffix), do not close
const closeOtherTagsView = (path: string) => {
	if (Session.get('tagsViewList')) {
		state.tagsViewList = [];
		Session.get('tagsViewList').map((v: RouteItem) => {
			if (v.meta?.isAffix && !v.meta.isHide) {
				v.url = setTagsViewHighlight(v);
				storesKeepALiveNames.delOthersCachedViews(v);
				state.tagsViewList.push({ ...v });
			}
		});
		addTagsView(path, <RouteToFrom>route);
		addBrowserSetSession(state.tagsViewList);
	}
};
// 5. Close all tagsView: if set to fixed (isAffix), do not close
const closeAllTagsView = () => {
	if (Session.get('tagsViewList')) {
		storesKeepALiveNames.delAllCachedViews();
		state.tagsViewList = [];
		Session.get('tagsViewList').map((v: RouteItem) => {
			if (v.meta?.isAffix && !v.meta.isHide) {
				v.url = setTagsViewHighlight(v);
				state.tagsViewList.push({ ...v });
				router.push({ path: state.tagsViewList[state.tagsViewList.length - 1].path });
			}
		});
		addBrowserSetSession(state.tagsViewList);
	}
};
// 6. Open current page in full screen
const openCurrenFullscreen = async (path: string) => {
	const item = state.tagsViewList.find((v: RouteItem) => (getThemeConfig.value.isShareTagsView ? v.path === path : v.url === path));
	if (item.meta.isDynamic) await router.push({ name: item.name, params: item.params });
	else await router.push({ name: item.name, query: item.query });
	stores.setCurrenFullscreen(true);
};
// Click on the right-click menu of the current item, take the `current clicked route path` to compare with the `tagsView route array`, get the detailed route information of the current clicked item
// Prevent abnormal operations when tagsView is not the current page demonstration
const getCurrentRouteItem = (item: RouteItem): any => {
	let resItem: RouteToFrom = {};
	state.tagsViewList.forEach((v: RouteItem) => {
		v.transUrl = transUrlParams(v);
		if (v.transUrl) {
			// Dynamic route, normal route with parameters
			if (v.transUrl === transUrlParams(v) && v.transUrl === item.commonUrl) resItem = v;
		} else {
			// Route without parameters
			if (v.path === decodeURI(item.path)) resItem = v;
		}
	});
	if (!resItem) return null;
	else return resItem;
};
// Click on the right-click menu of the current item
const onCurrentContextmenuClick = async (item: RouteItem) => {
	item.commonUrl = transUrlParams(item);
	if (!getCurrentRouteItem(item)) return ElMessage({ type: 'warning', message: 'Please enter the path and complete parameters (query, params) correctly' });
	const { path, name, params, query, meta, url } = getCurrentRouteItem(item);
	switch (item.contextMenuClickId) {
		case 0:
			// Refresh current
			if (meta.isDynamic) await router.push({ name, params });
			else await router.push({ path, query });
			refreshCurrentTagsView(route.fullPath);
			break;
		case 1:
			// Close current
			closeCurrentTagsView(getThemeConfig.value.isShareTagsView ? path : url);
			break;
		case 2:
			// Close others
			if (meta.isDynamic) await router.push({ name, params });
			else await router.push({ path, query });
			closeOtherTagsView(path);
			break;
		case 3:
			// Close all
			closeAllTagsView();
			break;
		case 4:
			// Open current page in full screen
			openCurrenFullscreen(getThemeConfig.value.isShareTagsView ? path : url);
			break;
	}
};
// When right-clicking: pass x,y coordinate values to the child component (props)
const onContextmenu = (v: RouteItem, e: MouseEvent) => {
	const { clientX, clientY } = e;
	state.dropdown.x = clientX;
	state.dropdown.y = clientY;
	contextmenuRef.value.openContextmenu(v);
};
// When the mouse is pressed, determine if it is the middle mouse button to close the current tagsView
const onMousedownMenu = (v: RouteItem, e: MouseEvent) => {
	if (!v.meta?.isAffix && e.button === 1) {
		const item = Object.assign({}, { contextMenuClickId: 1, ...v });
		onCurrentContextmenuClick(item);
	}
};
// When the current tagsView item is clicked
const onTagsClick = (v: RouteItem, k: number) => {
	state.tagsRefsIndex = k;
	router.push(v);
	// Auto scroll the clicked tag into view
	tagsViewmoveToCurrentTag();

	// In column layout, collapse/expand menu
	if (getThemeConfig.value.layout === 'columns') {
		const item: RouteItem = routesList.value.find((r: RouteItem) => r.path.indexOf(`/${v.path.split('/')[1]}`) > -1);
		!item.children ? (getThemeConfig.value.isCollapse = true) : (getThemeConfig.value.isCollapse = false);
	}
	//SONTH: set fullscreen
	stores.setCurrenFullscreen(v.meta?.isFullScreen ?? false);
};
// Handle url, when the address bar link has parameters, the tagsview right-click menu refresh function fails, thanks @ZzZz-RIPPER, @dejavuuuuu
const transUrlParams = (v: RouteItem) => {
	let params = v.query && Object.keys(v.query).length > 0 ? v.query : v.params;
	if (!params) return '';
	let path = '';
	for (let [key, value] of Object.entries(params)) {
		if (v.meta?.isDynamic) path += `/${value}`;
		else path += `&${key}=${value}`;
	}
	// Determine if it is a dynamic route (xxx/:id/:name)
	if (v.meta?.isDynamic) {
		return v.isFnClick ? decodeURI(v.path) : `${v.path.split(':')[0]}${path.replace(/^\//, '')}`;
	} else {
		return `${v.path}${path.replace(/^&/, '?')}`;
	}
};
// Handle tagsView highlight (used for multi-tag details, not used for single tag details)
const setTagsViewHighlight = (v: RouteToFrom) => {
	let params = v.query && Object.keys(v.query).length > 0 ? v.query : v.params;
	if (!params || Object.keys(params).length <= 0) return v.path;
	let path = '';
	for (let i in params) {
		path += params[i];
	}
	// Determine if it is a dynamic route (xxx/:id/:name)
	return `${v.meta?.isDynamic ? v.meta.isDynamicPath : v.path}-${path}`;
};
// Mouse wheel scroll
const onHandleScroll = (e: WheelEventType) => {
	scrollbarRef.value.$refs.wrapRef.scrollLeft += e.wheelDelta / 4;
};
// tagsView horizontal scroll: auto scroll so the selected tag is fully visible.
// Query the active <li> directly from the DOM (instead of the tagsRefs array,
// which is cleared in onBeforeUpdate) and defer to the next frame so a newly
// added tag is already rendered before we measure/scroll.
const tagsViewmoveToCurrentTag = () => {
	nextTick(() => {
		// Extra rAF: wait until the freshly pushed <li> is painted in the DOM
		requestAnimationFrame(() => {
			if (!scrollbarRef.value) return false;
			const scrollRefs = scrollbarRef.value.$refs.wrapRef as HTMLElement;
			const ulDom = tagsUlRef.value as HTMLElement;
			if (!scrollRefs || !ulDom) return false;
			// Active <li> (highlighted tag)
			const liDom = ulDom.querySelector<HTMLElement>('.layout-navbars-tagsview-ul-li.is-active');
			if (!liDom) return false;
			const liList = ulDom.querySelectorAll('.layout-navbars-tagsview-ul-li');
			const liFirst = liList[0];
			const liLast = liList[liList.length - 1];
			// Current scrollbar metrics
			const scrollS = scrollRefs.scrollWidth;
			const offsetW = scrollRefs.offsetWidth;
			const scrollL = scrollRefs.scrollLeft;
			// Padding gap kept between the tag and the scroll edges
			const gap = 5;
			if (liDom === liFirst) {
				// Head: scroll fully to the start
				scrollRefs.scrollLeft = 0;
			} else if (liDom === liLast) {
				// Tail (e.g. a newly opened tag): scroll fully to the end
				scrollRefs.scrollLeft = scrollS - offsetW;
			} else {
				// Left/right edges of the active tag relative to the ul
				const tagLeft = liDom.offsetLeft - gap;
				const tagRight = liDom.offsetLeft + liDom.offsetWidth + gap;
				if (tagRight > scrollL + offsetW) {
					// Active tag is cut off on the right -> align its right edge
					scrollRefs.scrollLeft = tagRight - offsetW;
				} else if (tagLeft < scrollL) {
					// Active tag is cut off on the left -> align its left edge
					scrollRefs.scrollLeft = tagLeft;
				}
			}
			// Update scrollbar to prevent it from not appearing
			scrollbarRef.value.update();
		});
	});
};
// Get the index of tagsView: used to handle horizontal scrolling when clicking on tagsView
const getTagsRefsIndex = (path: string | unknown) => {
	nextTick(async () => {
		// await use this method to prevent incomplete data from being obtained from the tagsViewList list
		let tagsViewList = await state.tagsViewList;
		state.tagsRefsIndex = tagsViewList.findIndex((v: RouteItem) => {
			if (getThemeConfig.value.isShareTagsView) {
				return v.path === path;
			} else {
				return v.url === path;
			}
		});
		// Add initialization horizontal scrollbar to move to the corresponding position
		tagsViewmoveToCurrentTag();
	});
};
// Set tagsView to be draggable
const initSortable = async () => {
	const el = <HTMLElement>document.querySelector('.layout-navbars-tagsview-ul');
	if (!el) return false;
	state.sortable.el && state.sortable.destroy();
	state.sortable = Sortable.create(el, {
		animation: 300,
		dataIdAttr: 'data-url',
		disabled: getThemeConfig.value.isSortableTagsView ? false : true,
		onEnd: () => {
			const sortEndList: RouteItem[] = [];
			state.sortable.toArray().map((val: string) => {
				state.tagsViewList.map((v: RouteItem) => {
					if (v.url === val) sortEndList.push({ ...v });
				});
			});
			addBrowserSetSession(sortEndList);
		},
	});
};
// Dragging issue, https://gitee.com/lyt-top/vue-next-admin/issues/I3ZRRI
const onSortableResize = async () => {
	await initSortable();
	if (other.isMobile()) state.sortable.el && state.sortable.destroy();
};
// Before page loads
onBeforeMount(() => {
	// Initialize to prevent dragging when directly accessing on mobile
	onSortableResize();
	// Dragging issue, https://gitee.com/lyt-top/vue-next-admin/issues/I3ZRRI
	window.addEventListener('resize', onSortableResize);
	// Listen for non-current page calls 0 refresh current, 1 close current, 2 close others, 3 close all 4 current page full screen
	mittBus.on('onCurrentContextmenuClick', (data: RouteItem) => {
		// Close tagsView by method click
		data.isFnClick = true;
		onCurrentContextmenuClick(data);
	});
	// Listen for layout configuration interface to open/close dragging
	mittBus.on('openOrCloseSortable', () => {
		initSortable();
	});
	// Listen for layout configuration to open TagsView sharing, to restore default values for demonstration
	mittBus.on('openShareTagsView', () => {
		if (getThemeConfig.value.isShareTagsView) {
			router.push('/home');
			state.tagsViewList = [];
			state.tagsViewRoutesList.map((v: RouteItem) => {
				if (v.meta?.isAffix && !v.meta.isHide) {
					v.url = setTagsViewHighlight(v);
					state.tagsViewList.push({ ...v });
				}
			});
		}
	});
});
// When the page is unloaded
onUnmounted(() => {
	// Cancel non-current page call listener
	mittBus.off('onCurrentContextmenuClick', () => {});
	// Cancel listener for layout configuration interface to open/close dragging
	mittBus.off('openOrCloseSortable', () => {});
	// Cancel listener for layout configuration to open TagsView sharing
	mittBus.off('openShareTagsView', () => {});
	// Cancel window resize listener
	window.removeEventListener('resize', onSortableResize);
});
// When the page updates
onBeforeUpdate(() => {
	tagsRefs.value = [];
});
// When the page loads
onMounted(() => {
	// Initialize the tagsViewRoutes list in pinia
	getTagsViewRoutes();
	initSortable();
});
// When the route updates (component lifecycle hook)
onBeforeRouteUpdate(async (to) => {
	state.routeActive = setTagsViewHighlight(to);
	state.routePath = to.meta.isDynamic ? to.meta.isDynamicPath : to.path;
	await addTagsView(to.path, <RouteToFrom>to);
	getTagsRefsIndex(getThemeConfig.value.isShareTagsView ? state.routePath : state.routeActive);
	stores.setCurrenFullscreen(to.meta.isFullScreen as boolean ?? false);
});
// Listen for route changes, dynamically assign to tagsView
watch(
	() => tagsViewRoutes.value,
	(val) => {
		if (val.length === state.tagsViewRoutesList.length) return false;
		getTagsViewRoutes();
	},
	{
		deep: true,
	}
);
</script>

<style scoped lang="scss">
.layout-navbars-tagsview {
	background-color: var(--el-color-primary-light-9);//SONTH
	// background-color: var(--el-color-white);
	border-bottom: 1px solid var(--next-border-color-light);
	position: relative;
	z-index: auto;
	:deep(.el-scrollbar__wrap) {
		overflow-x: auto !important;
	}
	&-ul {
		list-style: none;
		margin: 0;
		padding: 0;
		height: 34px;
		display: flex;
		align-items: center;
		color: var(--el-text-color-regular);
		font-size: 12px;
		white-space: nowrap;
		padding: 0 5px 0 5px;
		&-li {
			height: 26px;
			line-height: 26px;
			display: flex;
			align-items: center;
			border: 1px solid var(--el-border-color-lighter);
			padding: 0 15px;
			margin-right: 5px;
			border-radius: 2px;
			position: relative;
			z-index: 0;
			cursor: pointer;
			justify-content: space-between;
			background: var(--el-color-white); //SONTH
			&:hover {
				background-color: var(--el-color-primary-light-3);
				color: var(--el-color-white);
				border-color: var(--el-color-primary-light-5);
			}
			&-iconfont {
				position: relative;
				left: -5px;
				font-size: 12px;
			}
			&-icon {
				border-radius: 100%;
				position: relative;
				height: 14px;
				width: 14px;
				text-align: center;
				line-height: 14px;
				right: -5px;
				&:hover {
					color: var(--el-color-white);
					background-color: var(--el-color-primary-light-3);
				}
			}
			.layout-icon-active {
				display: block;
			}
			.layout-icon-three {
				display: none;
			}
		}
		.is-active {
			color: var(--el-color-white);
			background: var(--el-color-primary);
			border-color: var(--el-color-primary);
			transition: border-color 3s ease;
		}
	}

	// Style 2
	.tags-style-two {
		.layout-navbars-tagsview-ul-li {
			border-radius: 33px!important;
		}
		.layout-navbars-tagsview-ul-li:hover {
			border-color: var(--el-border-color-lighter) !important;
		}
	}
	// Style 4
	.tags-style-four {
		.layout-navbars-tagsview-ul-li {
			margin-right: 0 !important;
			border: none !important;
			position: relative;
			border-radius: 3px !important;
			.layout-icon-active {
				display: none;
			}
			.layout-icon-three {
				display: block;
			}
			&:hover {
				background: none !important;
			}
		}
		.is-active {
			background: none !important;
			color: var(--el-color-primary) !important;
		}
	}
	// Style 5
	.tags-style-five {
		align-items: flex-end;
		.tags-style-five-svg {
			mask-image: url('data:image/svg+xml,<svg width="35" height="71" viewBox="0 0 35 71" fill="none" xmlns="http://www.w3.org/2000/svg"><path d="M-0.00012207 0.5C-0.00012207 70.5 -5.89311e-05 70.5 34.9999 70.5H-0.00012207V27.7586V0.5Z" fill="black"/></svg>'),
				url('data:image/svg+xml,<svg width="35" height="70" viewBox="0 0 35 70" fill="none" xmlns="http://www.w3.org/2000/svg"><path d="M35 0C35 70 34.9999 70 0 70H35V27.2586V0Z" fill="black"/></svg>'),
				url("data:image/svg+xml,<svg xmlns='http://www.w3.org/2000/svg'><rect rx='8' width='100%' height='100%' fill='%23F8EAE7'/></svg>");
			mask-size:
				20px 20px,
				20px 20px,
				calc(100% - 29px) calc(100% + 17px);
			mask-position:
				right bottom,
				left bottom,
				center top;
			mask-repeat: no-repeat;
			-webkit-mask-image: url('data:image/svg+xml,<svg width="35" height="71" viewBox="0 0 35 71" fill="none" xmlns="http://www.w3.org/2000/svg"><path d="M-0.00012207 0.5C-0.00012207 70.5 -5.89311e-05 70.5 34.9999 70.5H-0.00012207V27.7586V0.5Z" fill="black"/></svg>'),
				url('data:image/svg+xml,<svg width="35" height="70" viewBox="0 0 35 70" fill="none" xmlns="http://www.w3.org/2000/svg"><path d="M35 0C35 70 34.9999 70 0 70H35V27.2586V0Z" fill="black"/></svg>'),
				url("data:image/svg+xml,<svg xmlns='http://www.w3.org/2000/svg'><rect rx='8' width='100%' height='100%' fill='%23F8EAE7'/></svg>");
			-webkit-mask-size:
				20px 20px,
				20px 20px,
				calc(100% - 29px) calc(100% + 17px);
			-webkit-mask-position:
				right bottom,
				left bottom,
				center top;
			-webkit-mask-repeat: no-repeat;
		}
		.layout-navbars-tagsview-ul-li {
			padding: 0 5px;
			border-width: 15px 27px 15px;
			border-style: solid;
			border-color: transparent;
			margin: 0 -15px;
			.layout-icon-active,
			.layout-navbars-tagsview-ul-li-iconfont,
			.layout-navbars-tagsview-ul-li-refresh {
				display: none;
			}
			.layout-icon-three {
				display: block;
			}
			&:hover {
				@extend .tags-style-five-svg;
				background: var(--el-color-primary-light-7);
				color: unset;
			}
		}
		.is-active {
			@extend .tags-style-five-svg;
			background: var(--el-color-primary-light-2) !important;
			color: var(--next-bg-main-color) !important;
			z-index: 1;
		}
	}
}
.layout-navbars-tagsview-shadow {
	box-shadow: rgb(0 21 41 / 4%) 0px 1px 4px;
}
</style>
