<template>
	<div class="grid-page-records-summary">
		<span class="grid-page-records-count">
			{{ $t('lz.label.base.total') }}: <strong>{{ n(summary.total) }}</strong>
		</span>
		<span v-if="summary.lastLoaded" class="grid-page-records-updated">
			<el-icon aria-hidden="true"><Clock /></el-icon>
			<span>{{ $t('lz.entity.base.lastUpdate') }}:</span>
			<time>{{ lastLoadedTime }}</time>
		</span>
	</div>
</template>

<script setup lang="ts" name="gridSummary">
// "Tổng cộng: N · Cập nhật lần cuối: HH:mm:ss", cùng nội dung với toolbar bảng của DataWorkspace.
// Đặt trong slot `toolbar_buttons` của vxe-grid; dữ liệu lấy từ useGridSummary().
import { useI18n } from 'vue-i18n';
import { useDateFormat } from '@vueuse/core';
import { Clock } from '@element-plus/icons-vue';
import { DateTimeFormatEnum } from '/@/types/enums/baseEnum';

const props = defineProps<{ summary: { total: number; lastLoaded: Date | null } }>();
const { n } = useI18n();
const lastLoadedTime = useDateFormat(() => props.summary.lastLoaded ?? '', DateTimeFormatEnum.TimeDefault);
</script>

<style lang="scss" scoped>
.grid-page-records-summary {
	display: flex;
	align-items: center;
	flex-wrap: wrap;
	gap: 4px 16px;
	min-width: 0;
	color: var(--workspace-muted, var(--el-text-color-secondary));
	font-size: 13px;
	font-weight: 400;
}
.grid-page-records-count {
	white-space: nowrap;

	strong {
		color: var(--workspace-text, var(--el-text-color-primary));
		font-weight: 650;
	}
}
.grid-page-records-updated {
	display: flex;
	align-items: center;
	gap: 4px;
	min-width: 0;

	time {
		white-space: nowrap;
	}
}
</style>
