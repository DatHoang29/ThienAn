<template>
	<div v-html="highlight"></div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
var props = defineProps({
	text: { type: String, default: '' },
	keyword: { type: String, default: '' },
	color: { type: String, default: 'rgba(0,0,0,0.99)' },
	background: { type: String, default: '#FFFF00' },
});

const highlight = computed(() => {
	let result = props.text;
	const regExp = new RegExp(props.keyword, 'g');
	return result.replace(regExp, `<span style="color:${props.color};background-color:${props.background};">${props.keyword}</span>`);
});
</script>
