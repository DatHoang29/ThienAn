<template>
	<div class="layout-footer mb5">
		<div class="layout-footer-warp">
			<div>{{ $t(themeConfig.globalTitle) }} {{ $t(themeConfig.copyright) }}</div>
		</div>
	</div>
</template>

<script setup lang="ts" name="layoutFooter">
import { storeToRefs } from 'pinia';
import { useThemeConfig } from '/@/stores/themeConfig';
import { useI18n } from 'vue-i18n';
const { t } = useI18n();
const storesThemeConfig = useThemeConfig();
const { themeConfig } = storeToRefs(storesThemeConfig);
</script>

<style scoped lang="scss">
.layout-footer {
	width: 100%;
	display: flex;
	position: relative;
	padding-top: 12px;
	background-image: linear-gradient(180deg, transparent 0%, color-mix(in srgb, var(--el-color-primary) 3%, transparent) 100%);

	/* Subtle accent line above the footer */
	&::before {
		content: '';
		position: absolute;
		left: 20%;
		right: 20%;
		top: 0;
		height: 1px;
		background: linear-gradient(90deg, transparent 0%, var(--el-color-primary) 50%, transparent 100%);
		box-shadow: 0 0 4px 0 color-mix(in srgb, var(--el-color-primary) 40%, transparent);
		opacity: 0.4;
		pointer-events: none;
	}

	&-warp {
		margin: auto;
		color: var(--el-text-color-secondary);
		text-align: center;
		animation: error-num 0.3s ease;
	}
}
</style>
