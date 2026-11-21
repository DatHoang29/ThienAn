<template>
	<div class="layout-navbars-breadcrumb">
		<SvgIcon class="layout-navbars-breadcrumb-icon" :name="themeConfig.isCollapse ? 'ele-Expand' : 'ele-Fold'" :size="16" @click="onThemeConfigChange" />
		<el-breadcrumb class="layout-navbars-breadcrumb-hide" v-if="isShowBreadcrumb" >
			<transition-group name="breadcrumb">
				<el-breadcrumb-item v-for="(v, k) in state.breadcrumbList" :key="v.path">
					<span v-if="k === state.breadcrumbList.length - 1" class="layout-navbars-breadcrumb-span">
						<SvgIcon :name="v.meta.icon" class="layout-navbars-breadcrumb-iconfont" v-if="themeConfig.isBreadcrumbIcon" />
						<div v-if="v.meta.title">{{ $t(v.meta.title) }}</div>
						<div v-else>{{ v.meta.tagsViewName }}</div>
					</span>
					<a v-else @click.prevent="onBreadcrumbClick(v)" style="display: flex">
						<SvgIcon :name="v.meta.icon" class="layout-navbars-breadcrumb-iconfont" v-if="themeConfig.isBreadcrumbIcon" />{{ $t(v.meta.title) }}
					</a>
				</el-breadcrumb-item>
			</transition-group>
		</el-breadcrumb>
	</div>
</template>

<script setup lang="ts" name="layoutBreadcrumb">
import { reactive, computed, onMounted } from 'vue';
import { onBeforeRouteUpdate, useRoute, useRouter, RouteLocationNormalized } from 'vue-router';
import { Local } from '/@/utils/storage';
import other from '/@/utils/other';
import { storeToRefs } from 'pinia';
import { useThemeConfig } from '/@/stores/themeConfig';
import { useRoutesList } from '/@/stores/routesList';

// Define variable contents
const stores = useRoutesList();
const storesThemeConfig = useThemeConfig();
const { themeConfig } = storeToRefs(storesThemeConfig);
const { routesList } = storeToRefs(stores);
const route = useRoute();
const router = useRouter();
const state = reactive<BreadcrumbState>({
	breadcrumbList: [],
	routeSplit: [],
	routeSplitFirst: '',
	routeSplitIndex: 1,
});

// Dynamically set classic and transverse layouts to not display
const isShowBreadcrumb = computed(() => {
	initRouteSplit(route);
	const { layout, isBreadcrumb } = themeConfig.value;
	if (layout === 'classic' || layout === 'transverse') return false;
	else return isBreadcrumb ? true : false;
});
// When breadcrumb is clicked
const onBreadcrumbClick = (v: RouteItem) => {
	const { redirect, path } = v;
	if (redirect) router.push(redirect);
	// If no redirect is specified, do not navigate
	// else if (path) router.push(path);
};
// Expand/collapse left menu on click
const onThemeConfigChange = () => {
	themeConfig.value.isCollapse = !themeConfig.value.isCollapse;
	setLocalThemeConfig();
};
// Store layout configuration
const setLocalThemeConfig = () => {
	Local.remove('themeConfig');
	Local.set('themeConfig', themeConfig.value);
};
// Process breadcrumb data
const getBreadcrumbList = (arr: RouteItems) => {
	arr.forEach((item: RouteItem) => {
		state.routeSplit.forEach((v: string, k: number, arrs: string[]) => {
			if (state.routeSplitFirst === item.path) {
				state.routeSplitFirst += `/${arrs[state.routeSplitIndex]}`;
				!state.breadcrumbList.find((a) => a.path === item.path) && state.breadcrumbList.push(item);
				state.routeSplitIndex++;
				if (item.children) getBreadcrumbList(item.children);
			}
		});
	});
};
// Split current route string into an array and remove the first empty item
const initRouteSplit = (toRoute: RouteLocationNormalized) => {
	if (!themeConfig.value.isBreadcrumb) return false;
	state.breadcrumbList = [];
	state.routeSplit = toRoute.path.split('/');
	state.routeSplit.shift();
	state.routeSplitFirst = `/${state.routeSplit[0]}`;
	state.routeSplitIndex = 1;
	getBreadcrumbList(routesList.value);
	if (toRoute.name === 'home' || (toRoute.name === 'notFound' && state.breadcrumbList.length > 1)) state.breadcrumbList.shift();
	if (state.breadcrumbList.length > 0) state.breadcrumbList[state.breadcrumbList.length - 1].meta.tagsViewName = other.setTagsViewNameI18n(<RouteToFrom>route);
};
// On page load
onMounted(() => {
	initRouteSplit(route);
});
// On route update
onBeforeRouteUpdate((to) => {
	initRouteSplit(to);
});
</script>

<style scoped lang="scss">
.layout-navbars-breadcrumb {
	flex: 1;
	height: inherit;
	display: flex;
	align-items: center;
	.layout-navbars-breadcrumb-icon {
		cursor: pointer;
		font-size: 18px;
		color: var(--next-bg-topBarColor);
		height: 100%;
		width: 40px;
		opacity: 0.8;
		&:hover {
			opacity: 1;
		}
	}
	.layout-navbars-breadcrumb-span {
		display: flex;
		opacity: 0.9;
		color: var(--next-bg-topBarColor);
	}
	.layout-navbars-breadcrumb-iconfont {
		font-size: 16px;
		margin-right: 5px;
	}
	:deep(.el-breadcrumb__separator) {
		opacity: 0.9;
		color: var(--next-bg-topBarColor);
	}
	:deep(.el-breadcrumb__inner a, .el-breadcrumb__inner.is-link) {
		font-weight: unset !important;
		color: var(--next-bg-topBarColor);
		&:hover {
			color: var(--el-color-primary) !important;
		}
	}
}
</style>
