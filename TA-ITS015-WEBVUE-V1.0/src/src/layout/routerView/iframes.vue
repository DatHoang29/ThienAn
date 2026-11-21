<template>
	<div class="layout-padding layout-padding-unset layout-iframe">
		<div class="layout-padding-auto layout-padding-view">
			<div class="w100" v-for="v in setIframeList" :key="v.path" v-loading="v.meta.loading" element-loading-background="white">
				<transition-group :name="name">
					<iframe
						:src="`${v.meta.isLink}${v.meta.isLink.indexOf('?') > 0 ? '&' : '?'}token=${getToken()}`"
						:key="v.path"
						frameborder="0"
						height="100%"
						width="100%"
						style="position: absolute"
						:data-url="v.path"
						v-show="getRoutePath === v.path"
						ref="iframeRef"
					/>
				</transition-group>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts" name="layoutIframeView">
import { computed, watch, ref, nextTick } from 'vue';
import { useRoute } from 'vue-router';
import { getToken } from '/@/utils/axios-utils';

// Define the values passed from the parent component
const props = defineProps({
	// Refresh iframe
	refreshKey: {
		type: String,
		default: () => '',
	},
	// Transition animation name
	name: {
		type: String,
		default: () => 'slide-right',
	},
	// Iframe list
	list: {
		type: Array,
		default: () => [],
	},
});

// Define variable content
const iframeRef = ref();
const route = useRoute();

// Process the list, loading only when opened
const setIframeList = computed(() => {
	return (<RouteItems>props.list).filter((v: RouteItem) => v.meta?.isIframeOpen);
});
// Get the current route path of the iframe
const getRoutePath = computed(() => {
	return route.path;
});
// Close iframe loading
const closeIframeLoading = (val: string, item: RouteItem) => {
	nextTick(() => {
		if (!iframeRef.value) return false;
		iframeRef.value.forEach((v: HTMLElement) => {
			if (v.dataset.url === val) {
				v.onload = () => {
					if (item.meta?.isIframeOpen && item.meta.loading) item.meta.loading = false;
				};
			}
		});
	});
};
// Watch for route changes, initialize iframe data to prevent switching issues with multiple iframes
watch(
	() => route.fullPath,
	(val) => {
		const item: any = props.list.find((v: any) => v.path === val);
		if (!item) return false;
		if (!item.meta.isIframeOpen) item.meta.isIframeOpen = true;
		closeIframeLoading(val, item);
	},
	{
		immediate: true,
	}
);
// Watch for changes in iframe refreshKey, used for tagsview right-click menu refresh
watch(
	() => props.refreshKey,
	() => {
		const item: any = props.list.find((v: any) => v.path === route.path);
		if (!item) return false;
		if (item.meta.isIframeOpen) item.meta.isIframeOpen = false;
		setTimeout(() => {
			item.meta.isIframeOpen = true;
			item.meta.loading = true;
			closeIframeLoading(route.fullPath, item);
		});
	},
	{
		deep: true,
	}
);
</script>
