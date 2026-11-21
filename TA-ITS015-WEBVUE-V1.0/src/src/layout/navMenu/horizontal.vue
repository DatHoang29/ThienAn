<template>
	<div class="el-menu-horizontal-warp">
		<el-menu router :default-active="state.defaultActive" background-color="transparent" mode="horizontal">
			<template v-for="val in menuLists">
				<el-sub-menu :index="val.path" v-if="val.children && val.children.length > 0" :key="val.path">
					<template #title>
						<SvgIcon :name="val.meta.icon" />
						<AutoScrollText :text="$t(`lz.router.${val.name}`, val.meta.title)" width="80%" />
					</template>
					<SubItem :chil="val.children" />
				</el-sub-menu>
				<template v-else>
					<el-menu-item :index="val.path" :key="val.path">
						<template #title v-if="!val.meta.isLink || (val.meta.isLink && val.meta.isIframe)">
							<SvgIcon :name="val.meta.icon" />
							<AutoScrollText :text="$t(`lz.router.${val.name}`, val.meta.title)" width="80%" />
						</template>
						<template #title v-else>
							<a class="w100" @click.prevent="onALinkClick(val)">
								<SvgIcon :name="val.meta.icon" />
								<AutoScrollText :text="$t(`lz.router.${val.name}`, val.meta.title)" width="80%" />
							</a>
						</template>
					</el-menu-item>
				</template>
			</template>
		</el-menu>
	</div>
</template>

<script setup lang="ts" name="navMenuHorizontal">
import { defineAsyncComponent, reactive, computed, onBeforeMount } from 'vue';
import { useRoute, onBeforeRouteUpdate, RouteRecordRaw } from 'vue-router';
import { storeToRefs } from 'pinia';
import { useRoutesList } from '/@/stores/routesList';
import { useThemeConfig } from '/@/stores/themeConfig';
import other from '/@/utils/other';
import mittBus from '/@/utils/mitt';

// Import components
const SubItem = defineAsyncComponent(() => import('/@/layout/navMenu/subItem.vue'));
const AutoScrollText = defineAsyncComponent(() => import('/@/components/advanced/auto-scroll-text.vue'));

// Define values passed from the parent component
const props = defineProps({
	// Menu list
	menuList: {
		type: Array<RouteRecordRaw>,
		default: () => [],
	},
});

// Define variable content
const stores = useRoutesList();
const storesThemeConfig = useThemeConfig();
const { routesList } = storeToRefs(stores);
const { themeConfig } = storeToRefs(storesThemeConfig);
const route = useRoute();
const state = reactive({
	defaultActive: '' as string | undefined,
});

// Get parent menu data
const menuLists = computed(() => {
	return <RouteItems>props.menuList;
});
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
// Send current child data to the menu
const setSendClassicChildren = (path: string) => {
	const currentPathSplit = path.split('/');
	let currentData: MittMenu = { children: [] };
	filterRoutesFun(routesList.value).map((v, k) => {
		if (v.path === `/${currentPathSplit[1]}`) {
			v['k'] = k;
			currentData['item'] = { ...v };
			currentData['children'] = [{ ...v }];
			if (v.children) currentData['children'] = v.children;
		}
	});
	return currentData;
};
// Set the current route highlight
const setCurrentRouterHighlight = (currentRoute: RouteToFrom) => {
	const { path, meta } = currentRoute;
	if (themeConfig.value.layout === 'classic') {
		state.defaultActive = `/${path?.split('/')[1]}`;
	} else {
		const pathSplit = meta?.isDynamic ? meta.isDynamicPath!.split('/') : path!.split('/');
		if (pathSplit.length >= 4 && meta?.isHide) state.defaultActive = pathSplit.splice(0, 3).join('/');
		else state.defaultActive = path;
	}
};
// Open external link
const onALinkClick = (val: RouteItem) => {
	other.handleOpenLink(val);
};
// Before page load
onBeforeMount(() => {
	setCurrentRouterHighlight(route);
});
// When the route updates
onBeforeRouteUpdate((to) => {
	// Fix: https://gitee.com/lyt-top/vue-next-admin/issues/I3YX6G
	setCurrentRouterHighlight(to);
	// Fix the issue where the left navigation menu data does not change after clicking tagsView when the classic layout is enabled with split menu
	let { layout, isClassicSplitMenu } = themeConfig.value;
	if (layout === 'classic' && isClassicSplitMenu) {
		mittBus.emit('setSendClassicChildren', setSendClassicChildren(to.path));
	}
});
</script>

<style scoped lang="scss">
.el-menu-horizontal-warp {
	flex: 1;
	overflow: hidden;
	margin-right: 30px;
	:deep(.el-scrollbar__bar.is-vertical) {
		display: none;
	}
	:deep(a) {
		width: 100%;
	}
	.el-menu.el-menu--horizontal {
		display: flex;
		height: 100%;
		width: 100%;
		box-sizing: border-box;
	}
}
</style>
