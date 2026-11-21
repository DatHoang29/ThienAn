<template>
	<div class="layout-columns-aside">
		<div class="layout-logo"><img :src="themeConfig.logoUrl" class="layout-logo-medium-img" /></div>
		<el-scrollbar>
			<ul @mouseleave="onColumnsAsideMenuMouseleave()">
				<li
					v-for="(v, k) in state.columnsAsideList"
					:key="k"
					@click="onColumnsAsideMenuClick(v)"
					@mouseenter="onColumnsAsideMenuMouseenter(v, k)"
					:ref="
						(el) => {
							if (el) columnsAsideOffsetTopRefs[k] = el;
						}
					"
					:class="{ 'layout-columns-active': state.liIndex === k, 'layout-columns-hover': state.liHoverIndex === k }"
					:title="$t(`lz.router.${v.name}`, v.meta.title)"
				>
					<div :class="themeConfig.columnsAsideLayout" v-if="!v.meta.isLink || (v.meta.isLink && v.meta.isIframe)">
						<SvgIcon :name="v.meta.icon" />
						<div class="columns-vertical-title font12">
							{{ $t(`lz.router.${v.name}`, v.meta.title) && $t(`lz.router.${v.name}`, v.meta.title).length >= 4 ? $t(`lz.router.${v.name}`, v.meta.title).substr(0, themeConfig.columnsAsideLayout === 'columns-vertical' ? 4 : 3) : $t(`lz.router.${v.name}`, v.meta.title) }}
						</div>
					</div>
					<div :class="themeConfig.columnsAsideLayout" v-else>
						<a :href="v.meta.isLink" target="_blank">
							<SvgIcon :name="v.meta.icon" />
							<div class="columns-vertical-title font12">
								{{ $t(`lz.router.${v.name}`, v.meta.title) && $t(`lz.router.${v.name}`, v.meta.title).length >= 4 ? $t(`lz.router.${v.name}`, v.meta.title).substr(0, themeConfig.columnsAsideLayout === 'columns-vertical' ? 4 : 3) : $t(`lz.router.${v.name}`, v.meta.title) }}
							</div>
						</a>
					</div>
				</li>
				<div ref="columnsAsideActiveRef" :class="themeConfig.columnsAsideStyle"></div>
			</ul>
		</el-scrollbar>
	</div>
</template>

<script setup lang="ts" name="layoutColumnsAside">
import { reactive, ref, onMounted, nextTick, watch, onUnmounted } from 'vue';
import { useRoute, useRouter, onBeforeRouteUpdate, RouteRecordRaw } from 'vue-router';
import { storeToRefs } from 'pinia';
import { useRoutesList } from '/@/stores/routesList';
import { useThemeConfig } from '/@/stores/themeConfig';
import mittBus from '/@/utils/mitt';
// import logoMini from '/@/assets/logo-mini.svg';

// Define variable content
const columnsAsideOffsetTopRefs = ref<RefType>([]);
const columnsAsideActiveRef = ref();
const stores = useRoutesList();
const storesThemeConfig = useThemeConfig();
const { routesList, isColumnsMenuHover, isColumnsNavHover } = storeToRefs(stores);
const { themeConfig } = storeToRefs(storesThemeConfig);
const route = useRoute();
const router = useRouter();
const state = reactive<ColumnsAsideState>({
	columnsAsideList: [],
	liIndex: 0,
	liOldIndex: null,
	liHoverIndex: null,
	liOldPath: null,
	difference: 0,
	routeSplit: [],
});

// Set the position of the highlighted menu
const setColumnsAsideMove = (k: number) => {
	if (k === undefined) return false;
	state.liIndex = k;
	columnsAsideActiveRef.value.style.top = `${columnsAsideOffsetTopRefs.value[k].offsetTop + state.difference}px`;
};
// Menu highlight click event
const onColumnsAsideMenuClick = async (v: RouteItem) => {
	let { path, redirect } = v;
	if (redirect) {
		onColumnsAsideDown(v.k);
		if (route.path.startsWith(redirect)) mittBus.emit('setSendColumnsChildren', setSendChildren(redirect));
		else router.push(redirect);
	} else {
		if (!v.children || v.children?.length === 0) {
			router.push(path);
			onColumnsAsideDown(v.k);
		} else {
			// Show sub-menu
			const resData: MittMenu = setSendChildren(path);
			if (Object.keys(resData).length <= 0) return false;
			onColumnsAsideDown(resData.item?.k);
			mittBus.emit('setSendColumnsChildren', resData);
		}
	}

	// Automatically collapse the menu for a single route
	// https://gitee.com/lyt-top/vue-next-admin/issues/I6HW7H
	if (!v.children || v.children?.length === 0) themeConfig.value.isCollapse = true;
	else if (v.children.length > 1) themeConfig.value.isCollapse = false;
};
// On mouse enter, show the current sub-menu
const onColumnsAsideMenuMouseenter = (v: RouteRecordRaw, k: number) => {
	if (!themeConfig.value.isColumnsMenuHoverPreload) return false;
	let { path } = v;
	state.liOldPath = path;
	state.liOldIndex = k;
	state.liHoverIndex = k;
	mittBus.emit('setSendColumnsChildren', setSendChildren(path));
	stores.setColumnsMenuHover(false);
	stores.setColumnsNavHover(true);
};
// On mouse leave, show the original sub-menu
const onColumnsAsideMenuMouseleave = async () => {
	if (!themeConfig.value.isColumnsMenuHoverPreload) return false;
	await stores.setColumnsNavHover(false);
	// Add a timer to prevent getting the latest value of store.state.routesList
	setTimeout(() => {
		if (!isColumnsMenuHover && !isColumnsNavHover) mittBus.emit('restoreDefault');
	}, 100);
};
// Set the dynamic position of the highlight
const onColumnsAsideDown = (k: number) => {
	nextTick(() => {
		setColumnsAsideMove(k);
	});
};
// Automatically collapse the menu when there is only one route
// https://gitee.com/lyt-top/vue-next-admin/issues/I6UW2I
const setMenuAutoCollaps = (path: string) => {
	const resData: MittMenu = setSendChildren(path);
	// https://gitee.com/lyt-top/vue-next-admin/issues/I6HW7H
	(!resData.item?.redirect && resData.children.length == 0) || (resData.item?.redirect && resData.children.length <= 1)
		? (themeConfig.value.isCollapse = true)
		: (themeConfig.value.isCollapse = false);
	return resData;
};
// Set/filter routes (non-static routes/whether to display in the menu)
const setFilterRoutes = () => {
	state.columnsAsideList = filterRoutesFun(routesList.value);
	const resData: MittMenu = setMenuAutoCollaps(route.path);
	onColumnsAsideDown(resData.item?.k);
	// Delay 500 milliseconds to update, to prevent aside.vue component setSendColumnsChildren from not being registered yet
	setTimeout(() => {
		mittBus.emit('setSendColumnsChildren', resData);
	}, 500);
};
// Pass the current sub-level data to the menu
const setSendChildren = (path: string) => {
	const currentPathSplit = path.split('/');
	let currentData: MittMenu = { children: [] };
	state.columnsAsideList.map((v: RouteItem, k: number) => {
		if (v.path === `/${currentPathSplit[1]}`) {
			v['k'] = k;
			currentData['item'] = { ...v };
			currentData['children'] = [{ ...v }];
			if (v.children) currentData['children'] = v.children;
		}
	});
	return currentData;
};
// Route filtering recursive function
const filterRoutesFun = <T extends RouteItem>(arr: T[]): T[] => {
	return arr
		.filter((item: T) => !item.meta?.isHide)
		.map((item: T) => {
			item = Object.assign({}, item);
			if (item.children) item.children = filterRoutesFun(item.children);
			return item;
		});
};
// When clicking tagsView, find the index of columnsAsideList based on the route to highlight the left menu
const setColumnsMenuHighlight = (path: string) => {
	state.routeSplit = path.split('/');
	state.routeSplit.shift();
	const routeFirst = `/${state.routeSplit[0]}`;
	const currentSplitRoute = state.columnsAsideList.find((v: RouteItem) => v.path === routeFirst);
	if (!currentSplitRoute) return false;
	// Delay to get the value to prevent not being able to retrieve it
	setTimeout(() => {
		onColumnsAsideDown(currentSplitRoute.k);
	}, 0);
};
// On page load
onMounted(() => {
	setFilterRoutes();
	// Destroy variables to prevent retaining the last record when the mouse enters again
	mittBus.on('restoreDefault', () => {
		state.liOldIndex = null;
		state.liOldPath = null;
	});
});
// On page unload
onUnmounted(() => {
	mittBus.off('restoreDefault', () => {});
});
// On route update
onBeforeRouteUpdate((to) => {
	const resData = setMenuAutoCollaps(to.path);
	setColumnsMenuHighlight(to.path);
	mittBus.emit('setSendColumnsChildren', resData);
});
// Listen for changes in layout configuration information, dynamically increase the pixel movement of the menu highlight position
watch(
	[() => themeConfig.value.columnsAsideStyle, isColumnsMenuHover, isColumnsNavHover],
	() => {
		themeConfig.value.columnsAsideStyle === 'columnsRound' ? (state.difference = 3) : (state.difference = 0);
		if (!isColumnsMenuHover.value && !isColumnsNavHover.value) {
			state.liHoverIndex = null;
			mittBus.emit('setSendColumnsChildren', setSendChildren(route.path));
		} else {
			state.liHoverIndex = state.liOldIndex;
			if (!state.liOldPath) return false;
			mittBus.emit('setSendColumnsChildren', setSendChildren(state.liOldPath));
		}
	},
	{
		deep: true,
	}
);
</script>

<style scoped lang="scss">
.layout-columns-aside {
	width: var(--el-columnsMenuWidth);
	height: 100%;
	background: var(--next-bg-columnsMenuBar);
	ul {
		position: relative;
		.layout-columns-active,
		.layout-columns-active a {
			color: var(--next-bg-columnsMenuBarColor) !important;
			transition: 0.3s ease-in-out;
		}
		.layout-columns-hover {
			color: var(--el-color-primary);
			a {
				color: var(--el-color-primary);
			}
		}
		li {
			color: var(--next-bg-columnsMenuBarColor);
			width: 100%;
			height: var(--el-columnsMenuHeight);
			text-align: center;
			display: flex;
			cursor: pointer;
			position: relative;
			z-index: 1;
			&:hover {
				@extend .layout-columns-hover;
			}
			.columns-vertical {
				margin: auto;
				.columns-vertical-title {
					padding-top: 1px;
				}
			}
			.columns-horizontal {
				display: flex;
				height: var(--el-columnsMenuHeight);
				width: 100%;
				align-items: center;
				padding: 0 5px;
				i {
					margin-right: 3px;
				}
				a {
					display: flex;
					.columns-horizontal-title {
						padding-top: 1px;
					}
				}
			}
			a {
				text-decoration: none;
				color: var(--next-bg-columnsMenuBarColor);
			}
		}
		.columns-round {
			background: var(--el-color-primary);
			color: var(--el-color-white);
			position: absolute;
			left: 50%;
			top: 2px;
			height: var(--el-columnsMenuRoundHeight);
			width: var(--el-columnsMenuRoundWidth);
			transform: translateX(-50%);
			z-index: 0;
			transition: 0.5s ease-in-out;
			border-radius: 5px;
		}
		.columns-card {
			@extend .columns-round;
			top: 0;
			height: var(--el-columnsMenuHeight);
			width: 100%;
			border-radius: 0;
		}
	}
}

.layout-logo {
	height: var(--el-columnsLogoHeight);
	display: flex;
	align-items: center;
	justify-content: center;
	animation: logoAnimation 0.3s ease-in-out;
	&-medium-img {
		width: auto;
		height: 80%;
	}
	&:hover {
		img {
			animation: logoAnimation 0.3s ease-in-out;
		}
	}
}

:deep(.el-scrollbar) {
	height: calc(100% - 50px);
}
</style>
