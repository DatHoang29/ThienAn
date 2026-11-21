<template>
	<component :is="layouts[themeConfig.layout]" />
</template>

<script setup lang="ts" name="layout">
import { onBeforeMount, onUnmounted, defineAsyncComponent } from 'vue';
import { storeToRefs } from 'pinia';
import { useThemeConfig } from '/@/stores/themeConfig';
import { Local } from '/@/utils/storage';
import mittBus from '/@/utils/mitt';

// Import components
const layouts: any = {
	defaults: defineAsyncComponent(() => import('/@/layout/main/defaults.vue')),
	classic: defineAsyncComponent(() => import('/@/layout/main/classic.vue')),
	transverse: defineAsyncComponent(() => import('/@/layout/main/transverse.vue')),
	columns: defineAsyncComponent(() => import('/@/layout/main/columns.vue')),
};

// Define variable content
const storesThemeConfig = useThemeConfig();
const { themeConfig } = storeToRefs(storesThemeConfig);

// 20240117 Maximum window width
let maxClientWidth = document.body.clientWidth;

// When the window size changes (adapt to mobile)
const onLayoutResize = () => {
	if (!Local.get('oldLayout')) Local.set('oldLayout', themeConfig.value.layout);
	const clientWidth = document.body.clientWidth;

	// 20240117 Maximum window width > current width, do not trigger layoutMobileResize event
	if (maxClientWidth > clientWidth) return;
	maxClientWidth = clientWidth;

	if (clientWidth < 1000) {
		themeConfig.value.isCollapse = false;
		mittBus.emit('layoutMobileResize', {
			layout: 'defaults',
			clientWidth,
		});
	} else {
		mittBus.emit('layoutMobileResize', {
			layout: Local.get('oldLayout') ? Local.get('oldLayout') : themeConfig.value.layout,
			clientWidth,
		});
	}
};

// Before the page loads
onBeforeMount(() => {
	onLayoutResize();
	window.addEventListener('resize', onLayoutResize);
});

// When the page is unloaded
onUnmounted(() => {
	window.removeEventListener('resize', onLayoutResize);
});
</script>
