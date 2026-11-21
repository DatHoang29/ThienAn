<template>
	<section class="workspace-empty-panel workspace-panel" role="status" aria-live="polite">
		<div class="workspace-empty-content">
			<div class="workspace-empty-symbol" aria-hidden="true">
				<el-icon :size="30"><Grid /></el-icon>
			</div>
			<h2>{{ $t('lz.label.base.noAvailableTabs') }}</h2>
			<p>{{ $t('lz.message.base.noAvailableTabs') }}</p>
		</div>
	</section>
</template>

<script setup lang="ts">
import { Grid } from '@element-plus/icons-vue';
import { useI18n } from 'vue-i18n';
const { t: $t } = useI18n();
</script>

<style scoped>
.workspace-empty-panel {
	display: grid;
	place-items: center;
	flex: 1;
	min-height: 260px;
	padding: 32px 20px;
	box-shadow: none;
}
.workspace-empty-content {
	width: 100%;
	max-width: 420px;
	padding-bottom: 40px;
	text-align: center;
}
.workspace-empty-symbol {
	display: inline-flex;
	align-items: center;
	justify-content: center;
	width: 64px;
	height: 64px;
	margin-bottom: 20px;
	border: 1px solid var(--workspace-border);
	border-radius: 18px;
	color: var(--workspace-primary);
	background: var(--workspace-primary-soft);
}
.workspace-empty-content h2 {
	margin: 0;
	color: var(--workspace-text);
	font-size: 18px;
	font-weight: 650;
	line-height: 1.4;
}
.workspace-empty-content p {
	margin: 10px 0 0;
	color: var(--workspace-muted);
	font-size: 13px;
	line-height: 1.7;
}
</style>
