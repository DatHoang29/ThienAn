<template>
	<div class="layout-logo" v-if="setShowLogo" @click="onThemeConfigChange">
		<img :src="themeConfig.logoUrl" class="layout-logo-medium-img" v-if="showLogo" />
		<!-- <span>{{ themeConfig.globalTitle }}</span> -->
	</div>
	<div class="layout-logo-size" v-else @click="onThemeConfigChange">
		<img :src="themeConfig.logoUrl" class="layout-logo-size-img" />
	</div>
</template>

<script setup lang="ts" name="layoutLogo">
import { computed } from 'vue';
import { storeToRefs } from 'pinia';
import { useThemeConfig } from '/@/stores/themeConfig';

// Define variable content
const storesThemeConfig = useThemeConfig();
const { themeConfig } = storeToRefs(storesThemeConfig);

// Set logo display. The classic layout shows the logo by default
const setShowLogo = computed(() => {
	let { isCollapse, layout } = themeConfig.value;
	return !isCollapse || layout === 'classic' || document.body.clientWidth < 1000;
});
const showLogo = computed(() => {
	let { layout } = themeConfig.value;
	return layout !== 'columns';
});
// Logo click toggles menu expand/collapse
const onThemeConfigChange = () => {
	if (themeConfig.value.layout === 'transverse') return false;
	themeConfig.value.isCollapse = !themeConfig.value.isCollapse;
};
</script>

<style scoped lang="scss">
.layout-logo {
	width: 220px;
	height: 50px;
	display: flex;
	align-items: center;
	justify-content: center;
	position: relative;
	box-shadow: rgb(0 21 41 / 2%) 0px 1px 4px;
	border-bottom: 1px solid color-mix(in srgb, var(--el-color-primary) 25%, transparent);
	color: var(--el-color-primary);
	font-size: 16px;
	cursor: pointer;
	animation: logoAnimation 0.3s ease-in-out;
	transition: box-shadow 0.3s ease-in-out;
	span {
		white-space: nowrap;
		display: inline-block;
		font-weight: 800;
		font-size: 18px;
	}
	&:hover {
		box-shadow: inset 0 0 16px 0 color-mix(in srgb, var(--el-color-primary) 25%, transparent);
		span {
			color: var(--color-primary-light-2);
		}
		.layout-logo-medium-img {
			filter: drop-shadow(0 0 6px color-mix(in srgb, var(--el-color-primary) 70%, transparent));
		}
	}
	&-medium-img {
		width: 80px; //15%
		margin-right: 5px;
		padding: 3px;
		transition: filter 0.3s ease-in-out;
	}
}
.layout-logo-size {
	width: 100%;
	height: 50px;
	display: flex;
	cursor: pointer;
	animation: logoAnimation 0.3s ease-in-out;
	&-img {
		width: 35px;
		margin: auto;
	}
	&:hover {
		img {
			animation: logoAnimation 0.3s ease-in-out;
		}
	}
}
</style>
