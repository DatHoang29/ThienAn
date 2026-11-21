<template>
	<!-- Write the render function variable inside the template tag -->
	<render></render>
</template>

<script lang="ts" setup>
import { ref, watch, h } from 'vue';
// Define the values passed from the parent component
const props = defineProps<{
	fn: any;
}>();
const render = ref();
watch(
	props,
	async () => {
		render.value = h('div', null, props.fn);
	},
	{
		deep: true, // Confirm whether to deeply watch
		immediate: true, // Confirm whether to execute the handler function with the current initial value
	}
);
</script>
