<template>
	<div class="videowall-screen-layout-container">
		<!-- Sơ đồ ghép màn: kéo màn vào ô / về kho. Gán cổng cho màn ở tab "Sơ đồ gán cổng" (/videoWall/controllerOutput) -->
		<OutputMap mode="screens" />
	</div>
</template>

<script lang="ts" setup name="videoWallScreenLayout">
import OutputMap from '../screens/component/outputMap.vue';
</script>

<style scoped lang="scss">
.videowall-screen-layout-container {
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
