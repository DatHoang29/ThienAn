<template>
	<div class="sys-form-des-container">
		<v-form-designer class="form-designer" ref="vFormDesignRef"></v-form-designer>
	</div>
</template>

<script lang="ts" setup name="sysFormDes">
import { ref } from 'vue';

const vFormDesignRef = ref(null);
</script>

<style lang="scss" scoped>
body {
	margin: 0; // Remove vertical scrollbar from the page
}

.form-designer {
	overflow: unset !important;
}
</style>
