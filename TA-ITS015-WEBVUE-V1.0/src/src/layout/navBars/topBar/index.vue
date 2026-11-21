<template>
	<div class="layout-navbars-breadcrumb-index">
		<Logo v-if="setIsShowLogo" />
		<div class="left-section">
			<Breadcrumb />
			<incidentNotificationComp style="margin-top: 0px;" />
		</div>
		<div class="center-section">
			<h1 class="top-bar-title" v-if="width > 650">{{ $t(themeConfig.globalTitle?.toUpperCase()) }}</h1>
		</div>
		<div class="right-section">
			<Horizontal :menuList="state.menuList" v-if="isLayoutTransverse" style="width:40vw" />
			<User />
		</div>
	</div>
</template>


<script setup lang="ts" name="layoutBreadcrumbIndex">
import { defineAsyncComponent, computed, reactive, onMounted, onUnmounted } from 'vue';
import { useRoute } from 'vue-router';
import { storeToRefs } from 'pinia';
import { useRoutesList } from '/@/stores/routesList';
import { useThemeConfig } from '/@/stores/themeConfig';
import mittBus from '/@/utils/mitt';
import { useWindowSize } from '@vueuse/core';
const {width,height} = useWindowSize();
// Import components
const Breadcrumb = defineAsyncComponent(() => import('/@/layout/navBars/topBar/breadcrumb.vue'));
const User = defineAsyncComponent(() => import('/@/layout/navBars/topBar/user.vue'));
const Logo = defineAsyncComponent(() => import('/@/layout/logo/index.vue'));
const Horizontal = defineAsyncComponent(() => import('/@/layout/navMenu/horizontal.vue'));
const incidentNotificationComp = defineAsyncComponent(() => import('/@/views/bigScreen/trafficInfo/component/incidentNotification.vue'));

// Define variables
const stores = useRoutesList();
const storesThemeConfig = useThemeConfig();
const { themeConfig } = storeToRefs(storesThemeConfig);
const { routesList } = storeToRefs(stores);
const route = useRoute();
const state = reactive({
	menuList: [] as RouteItems,
});
// const setBorderSiteColor = useThemeConfig().setBorderSiteColor;
// Set logo visibility
const setIsShowLogo = computed(() => {
	let { isShowLogo, layout } = themeConfig.value;
	return (isShowLogo && layout === 'classic') || (isShowLogo && layout === 'transverse');
});
// Set horizontal navigation menu visibility
const isLayoutTransverse = computed(() => {
	let { layout, isClassicSplitMenu } = themeConfig.value;
	return layout === 'transverse' || (isClassicSplitMenu && layout === 'classic');
});
// Set/filter routes (non-static routes/whether to show in the menu)
const setFilterRoutes = () => {
	let { layout, isClassicSplitMenu } = themeConfig.value;
	if (layout === 'classic' && isClassicSplitMenu) {
		state.menuList = delClassicChildren(filterRoutesFun(routesList.value));
		const resData = setSendClassicChildren(route.path);
		mittBus.emit('setSendClassicChildren', resData);
	} else {
		state.menuList = filterRoutesFun(routesList.value);
	}
};
// Delete children when split menu is set
const delClassicChildren = <T extends ChilType>(arr: T[]): T[] => {
	arr.map((v: T) => {
		if (v.children) delete v.children;
	});
	return arr;
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
// Send current child data to the menu
const setSendClassicChildren = (path: string) => {
	const currentPathSplit = path.split('/');
	let currentData: MittMenu = { children: [] };
	filterRoutesFun(routesList.value).map((v: RouteItem, k: number) => {
		if (v.path === `/${currentPathSplit[1]}`) {
			v['k'] = k;
			currentData['item'] = { ...v };
			currentData['children'] = [{ ...v }];
			if (v.children) currentData['children'] = v.children;
		}
	});
	return currentData;
};


// On page load
onMounted(() => {
	setFilterRoutes();
	mittBus.on('getBreadcrumbIndexSetFilterRoutes', () => {
		setFilterRoutes();
	});
});
// On page unload
onUnmounted(() => {
	mittBus.off('getBreadcrumbIndexSetFilterRoutes', () => {});
});
</script>

<style scoped lang="scss">
.layout-navbars-breadcrumb-index {
	height: 50px;
	display: flex;
	align-items: center;
	justify-content: space-between;
	background: var(--next-bg-topBar);
	position: relative;
	border-bottom: 1px solid var(--next-border-color-light);

	/* Subtle accent line under the header */
	&::after {
		content: '';
		position: absolute;
		left: 0;
		right: 0;
		bottom: -1px;
		height: 2px;
		background: linear-gradient(90deg, transparent 0%, var(--el-color-primary) 50%, transparent 100%);
		box-shadow: 0 0 4px 0 color-mix(in srgb, var(--el-color-primary) 40%, transparent);
		opacity: 0.5;
		pointer-events: none;
	}

	.left-section {
		display: flex;
		align-items: center;
	}
	
	.center-section {
		flex: 1;
		display: flex;
		justify-content: center;
	}
	
	.right-section {
		display: flex;
		align-items: center;
	}
}

.top-bar-title {
	color: var(--el-color-primary);
	font-weight: bold;
	font-size: 20px;
}
</style>
