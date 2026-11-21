<template>
	<el-menu
		router
		:default-active="state.defaultActive"
		background-color="transparent"
		:collapse="state.isCollapse"
		:unique-opened="getThemeConfig.isUniqueOpened"
		:collapse-transition="false"
	>
		<template v-for="val in menuLists">
			<el-sub-menu :index="val.path" v-if="val.children && val.children.length > 0" :key="val.path">
				<template #title>
					<SvgIcon :name="val.meta.icon" />
					<AutoScrollText :text="$t(`lz.router.${val.name}`, val.meta.title)" width="80%" />
				</template>
				<SubItem :chil="val.children" />
			</el-sub-menu>
			<template v-else>
				<el-menu-item :index="val.path" :key="val.path" > 
					<SvgIcon :name="val.meta.icon" />
					<template #title v-if="!val.meta.isLink || (val.meta.isLink && val.meta.isIframe)">
						<AutoScrollText :text="$t(`lz.router.${val.name}`, val.meta.title)" width="80%" />
					</template>
					<template #title v-else>
						<a class="w100" @click.prevent="onALinkClick(val)">
							<AutoScrollText :text="$t(`lz.router.${val.name}`, val.meta.title)" width="80%" />
						</a>
					</template>
				</el-menu-item>
			</template>
		</template>
	</el-menu>
</template>

<script setup lang="ts" name="navMenuVertical">
import { defineAsyncComponent, reactive, computed, onMounted, watch } from 'vue';
import { useRoute, onBeforeRouteUpdate, RouteRecordRaw } from 'vue-router';
import { storeToRefs } from 'pinia';
import { useThemeConfig } from '/@/stores/themeConfig';
import other from '/@/utils/other';

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
const storesThemeConfig = useThemeConfig();
const { themeConfig } = storeToRefs(storesThemeConfig);
const route = useRoute();
const state = reactive({
	// Fix: https://gitee.com/lyt-top/vue-next-admin/issues/I3YX6G
	defaultActive: route.meta.isDynamic ? route.meta.isDynamicPath : route.path,
	isCollapse: false,
});

// Get parent menu data
const menuLists = computed(() => {
	return <RouteItems>props.menuList;
});
// Get layout configuration information
const getThemeConfig = computed(() => {
	return themeConfig.value;
});
// Menu highlight (highlight parent when details are shown)
const setParentHighlight = (currentRoute: RouteToFrom) => {
	const { path, meta } = currentRoute;
	const pathSplit = meta?.isDynamic ? meta.isDynamicPath!.split('/') : path!.split('/');
	if (pathSplit.length >= 4 && meta?.isHide) return pathSplit.splice(0, 3).join('/');
	else return path;
};
// Open external link
const onALinkClick = (val: RouteItem) => {
	other.handleOpenLink(val);
};
// When the page loads
onMounted(() => {
	state.defaultActive = setParentHighlight(route);
});
// When the route updates
onBeforeRouteUpdate((to) => {
	// Fix: https://gitee.com/lyt-top/vue-next-admin/issues/I3YX6G
	state.defaultActive = setParentHighlight(to);
	const clientWidth = document.body.clientWidth;
	if (clientWidth < 1000) themeConfig.value.isCollapse = false;
});
// Set menu collapse/expand
watch(
	() => themeConfig.value.isCollapse,
	(isCollapse) => {
		document.body.clientWidth <= 1000 ? (state.isCollapse = false) : (state.isCollapse = isCollapse);
	},
	{
		immediate: true,
	}
);
</script>
