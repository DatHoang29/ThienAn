<template>
	<div class="layout-parent">
		<router-view v-slot="{ Component }">
			<transition :name="setTransitionName" mode="out-in">
				<keep-alive :include="getKeepAliveNames">
					<component :is="Component" :key="state.refreshRouterViewKey" class="w100" v-show="!isIframePage" />
				</keep-alive>
			</transition>
		</router-view>
		<transition :name="setTransitionName" mode="out-in">
			<Iframes class="w100" v-show="isIframePage" :refreshKey="state.iframeRefreshKey" :name="setTransitionName" :list="state.iframeList" />
		</transition>
	</div>
</template>

<script setup lang="ts" name="layoutParentView">
import { defineAsyncComponent, computed, reactive, onBeforeMount, onUnmounted, nextTick, watch, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { storeToRefs } from 'pinia';
import { useKeepALiveNames } from '/@/stores/keepAliveNames';
import { useThemeConfig } from '/@/stores/themeConfig';
import { Session } from '/@/utils/storage';
import mittBus from '/@/utils/mitt';

// Import component
const Iframes = defineAsyncComponent(() => import('/@/layout/routerView/iframes.vue'));

// Define variable content
const route = useRoute();
const router = useRouter();
const storesKeepAliveNames = useKeepALiveNames();
const storesThemeConfig = useThemeConfig();
const { keepAliveNames, cachedViews } = storeToRefs(storesKeepAliveNames);
const { themeConfig } = storeToRefs(storesThemeConfig);
const state = reactive<ParentViewState>({
	refreshRouterViewKey: '', // Refresh when right-clicking on non-iframe tagsview
	iframeRefreshKey: '', // Refresh when right-clicking on iframe tagsview
	keepAliveNameList: [],
	iframeList: [],
});

// Set main interface transition animation
const setTransitionName = computed(() => {
	return themeConfig.value.animation;
});
// Get component cache list (name values)
const getKeepAliveNames = computed(() => {
	return themeConfig.value.isTagsview ? cachedViews.value : state.keepAliveNameList;
});
// Set iframe show/hide
const isIframePage = computed(() => {
	return route.meta.isIframe;
});
// Get iframe component list (not rendered)
const getIframeListRoutes = async () => {
	router.getRoutes().forEach((v) => {
		if (v.meta.isIframe) {
			v.meta.isIframeOpen = false;
			v.meta.loading = true;
			state.iframeList.push({ ...v });
		}
	});
};
// Before page loads, handle cache, handle route cache when page refreshes
onBeforeMount(() => {
	state.keepAliveNameList = keepAliveNames.value;
	mittBus.on('onTagsViewRefreshRouterView', (fullPath: string) => {
		state.keepAliveNameList = keepAliveNames.value.filter((name: string) => route.name !== name);
		state.refreshRouterViewKey = '';
		state.iframeRefreshKey = '';
		nextTick(() => {
			state.refreshRouterViewKey = fullPath;
			state.iframeRefreshKey = fullPath;
			state.keepAliveNameList = keepAliveNames.value;
		});
	});
});
// When the page loads
onMounted(() => {
	getIframeListRoutes();
	// https://gitee.com/lyt-top/vue-next-admin/issues/I58U75
	// https://gitee.com/lyt-top/vue-next-admin/issues/I59RXK
	// https://gitee.com/lyt-top/vue-next-admin/pulls/40
	nextTick(() => {
		setTimeout(() => {
			if (themeConfig.value.isCacheTagsView) {
				let tagsViewArr: RouteItem[] = Session.get('tagsViewList') || [];
				cachedViews.value = tagsViewArr.filter((item) => item.meta?.isKeepAlive).map((item) => item.name as string);
			}
		}, 0);
	});
});
// When the page unloads
onUnmounted(() => {
	mittBus.off('onTagsViewRefreshRouterView', () => {});
});
// Listen for route changes to prevent the transition animation from disappearing when switching tags in tagsView
// https://toscode.gitee.com/lyt-top/vue-next-admin/pulls/38/files
watch(
	() => route.fullPath,
	() => {
		state.refreshRouterViewKey = decodeURI(route.fullPath);
	},
	{
		immediate: true,
	}
);
</script>
