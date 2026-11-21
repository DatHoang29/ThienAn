<template>
	<div class="videowall-controller-output-container">
		<!-- Sơ đồ gán cổng: kéo cổng thả lên màn / gỡ cổng. Đặt màn vào ô ở tab "Sơ đồ ghép màn" (/videoWall/screenLayout) -->
		<OutputMap mode="outputs" />
	</div>
</template>

<script lang="ts" setup name="videoWallControllerOutput">
import OutputMap from '../screens/component/outputMap.vue';
</script>

<style scoped lang="scss">
.videowall-controller-output-container {
	height: 100%;
	display: flex;
	flex-direction: column;
	padding: 10px;
	box-sizing: border-box;
	> * {
		flex: 1;
		min-height: 0;
	}
}
</style>
