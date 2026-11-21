<template>
	<el-container class="layout-container">
		<LayoutAside />
		<el-container class="layout-container-view h100">
			<el-scrollbar ref="layoutScrollbarRef" class="layout-backtop">
				<LayoutHeader />
				<LayoutMain ref="layoutMainRef" />
			</el-scrollbar>
		</el-container>
	</el-container>
</template>

<script setup lang="ts" name="layoutDefaults">
import { defineAsyncComponent, watch, onMounted, nextTick, ref } from 'vue';
import { useRoute } from 'vue-router';
import { storeToRefs } from 'pinia';
import { useThemeConfig } from '/@/stores/themeConfig';
import { NextLoading } from '/@/utils/loading';

// Import components
const LayoutAside = defineAsyncComponent(() => import('/@/layout/component/aside.vue'));
const LayoutHeader = defineAsyncComponent(() => import('/@/layout/component/header.vue'));
const LayoutMain = defineAsyncComponent(() => import('/@/layout/component/main.vue'));

// Define variable content
const layoutScrollbarRef = ref<RefType>('');
const layoutMainRef = ref<InstanceType<typeof LayoutMain>>();
const route = useRoute();
const storesThemeConfig = useThemeConfig();
const { themeConfig } = storeToRefs(storesThemeConfig);

// Reset scrollbar height
const updateScrollbar = () => {
	// Update parent scrollbar
	layoutScrollbarRef.value.update();
	// Update child scrollbar
	layoutMainRef.value!.layoutMainScrollbarRef.update();
};
// Reset scrollbar height, since the component is loaded asynchronously
const initScrollBarHeight = () => {
	nextTick(() => {
		setTimeout(() => {
			updateScrollbar();
			layoutScrollbarRef.value.wrapRef.scrollTop = 0;
			if (layoutMainRef.value != undefined) layoutMainRef.value.layoutMainScrollbarRef.wrapRef.scrollTop = 0;
		}, 500);
	});
};
// When the page loads
onMounted(() => {
	initScrollBarHeight();
	NextLoading.done(600);
});
// Watch for route changes, scroll to the top when switching interfaces
watch(
	() => route.path,
	() => {
		initScrollBarHeight();
	}
);
// Watch for changes in themeConfig configuration file, update the height of the menu el-scrollbar
watch(
	() => [themeConfig.value.isTagsview, themeConfig.value.isFixedHeader],
	() => {
		nextTick(() => {
			updateScrollbar();
		});
	},
	{
		deep: true,
	}
);
</script>
