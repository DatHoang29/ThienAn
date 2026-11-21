<template>
	<el-container class="layout-container flex-center">
		<LayoutHeader />
		<el-container class="layout-main-height-50">
			<LayoutAside />
			<div class="flex-center layout-backtop">
				<LayoutTagsView v-if="isTagsview" />
				<LayoutMain ref="layoutMainRef" />
			</div>
		</el-container>
	</el-container>
</template>

<script setup lang="ts" name="layoutClassic">
import { defineAsyncComponent, computed, ref, watch, nextTick, onMounted } from 'vue';
import { useRoute } from 'vue-router';
import { storeToRefs } from 'pinia';
import { useThemeConfig } from '/@/stores/themeConfig';

// Import components
const LayoutAside = defineAsyncComponent(() => import('/@/layout/component/aside.vue'));
const LayoutHeader = defineAsyncComponent(() => import('/@/layout/component/header.vue'));
const LayoutMain = defineAsyncComponent(() => import('/@/layout/component/main.vue'));
const LayoutTagsView = defineAsyncComponent(() => import('/@/layout/navBars/tagsView/tagsView.vue'));

// Define variable content
const layoutMainRef = ref<InstanceType<typeof LayoutMain>>();
const route = useRoute();
const storesThemeConfig = useThemeConfig();
const { themeConfig } = storeToRefs(storesThemeConfig);

// Determine whether to show tags view
const isTagsview = computed(() => {
	return themeConfig.value.isTagsview;
});
// Reset scrollbar height and update child scrollbar
const updateScrollbar = () => {
	layoutMainRef.value?.layoutMainScrollbarRef.update();
};
// Reset scrollbar height, since the component is imported asynchronously
const initScrollBarHeight = () => {
	nextTick(() => {
		setTimeout(() => {
			updateScrollbar();
			// '!' non-null assertion operator, does not perform runtime checks
			if (layoutMainRef.value) layoutMainRef.value!.layoutMainScrollbarRef.wrapRef.scrollTop = 0;
		}, 500);
	});
};
// When the page loads
onMounted(() => {
	initScrollBarHeight();
});
// Watch for route changes, scroll to top when switching views
watch(
	() => route.path,
	() => {
		initScrollBarHeight();
	}
);
// Watch for changes in themeConfig isTagsview configuration, update menu el-scrollbar height
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
