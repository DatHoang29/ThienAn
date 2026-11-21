<template>
	<i v-if="isShowIconSvg" class="el-icon" :style="setIconSvgStyle">
		<component :is="getIconName" />
	</i>
	<div v-else-if="isShowIconImg" :style="setIconImgOutStyle">
		<img :src="getIconName" :style="setIconSvgInsStyle" />
	</div>
	<i v-else :class="getIconName" :style="setIconSvgStyle" />
</template>

<script setup lang="ts" name="svgIcon">
import { computed } from 'vue';

// Define the values passed from the parent component
const props = defineProps({
	// SVG icon component name
	name: {
		type: String,
	},
	// SVG size
	size: {
		type: Number,
		default: () => 14,
	},
	// SVG color
	color: {
		type: String,
	},
});

// Online links and local import address prefixes
// https://gitee.com/lyt-top/vue-next-admin/issues/I62OVL
const linesString = ['https', 'http', '/src', '/assets', 'data:image', window.__env__.VITE_PUBLIC_PATH];

// Get the icon name
const getIconName = computed(() => {
	return props?.name;
});
// Used to determine the visibility of the SVG icons provided by Element Plus
const isShowIconSvg = computed(() => {
	return props?.name?.startsWith('ele-');
});
// Used to determine the visibility of online links, local imports, and other icons
const isShowIconImg = computed(() => {
	return linesString.find((str) => props.name?.startsWith(str));
});
// Set the icon style
const setIconSvgStyle = computed(() => {
	return `font-size: ${props.size}px;color: ${props.color};`;
});
// Set the image style
const setIconImgOutStyle = computed(() => {
	return `width: ${props.size}px;height: ${props.size}px;display: inline-block;overflow: hidden;`;
});
// Set the image style
// https://gitee.com/lyt-top/vue-next-admin/issues/I59ND0
const setIconSvgInsStyle = computed(() => {
	const filterStyle: string[] = [];
	const compatibles: string[] = ['-webkit', '-ms', '-o', '-moz'];
	compatibles.forEach((j) => filterStyle.push(`${j}-filter: drop-shadow(${props.color} ${props.size}px 0);`));
	return `width: ${props.size}px;height: ${props.size}px;position: relative;left: -${props.size}px;${filterStyle.join('')}`;
});
</script>
