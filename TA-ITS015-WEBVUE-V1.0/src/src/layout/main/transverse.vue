<template>
	<el-container class="layout-container flex-center layout-backtop">
		<LayoutHeader />
		<LayoutMain ref="layoutMainRef" />
	</el-container>
</template>

<script setup lang="ts" name="layoutTransverse">
import { defineAsyncComponent, ref, watch, nextTick, onMounted } from 'vue';
import { useRoute } from 'vue-router';
import { storeToRefs } from 'pinia';
import { useThemeConfig } from '/@/stores/themeConfig';

// Import components
const LayoutHeader = defineAsyncComponent(() => import('/@/layout/component/header.vue'));
const LayoutMain = defineAsyncComponent(() => import('/@/layout/component/main.vue'));

// Define variable content
const layoutMainRef = ref<InstanceType<typeof LayoutMain>>();
const storesThemeConfig = useThemeConfig();
const { themeConfig } = storeToRefs(storesThemeConfig);
const route = useRoute();

// Reset scrollbar height and update child scrollbar
const updateScrollbar = () => {
	layoutMainRef.value!.layoutMainScrollbarRef.update();
};
// Reset scrollbar height, since the component is imported asynchronously
const initScrollBarHeight = () => {
	nextTick(() => {
		setTimeout(() => {
			updateScrollbar();
			layoutMainRef.value!.layoutMainScrollbarRef.wrapRef.scrollTop = 0;
		}, 500);
	});
};
// When the page loads
onMounted(() => {
	initScrollBarHeight();
});
// Watch for changes in the route, scroll to the top when switching screens
watch(
	() => route.path,
	() => {
		initScrollBarHeight();
	}
);
// Watch for changes in the themeConfig configuration file, update the height of the menu el-scrollbar
watch(
	() => themeConfig.value.isTagsview,
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
