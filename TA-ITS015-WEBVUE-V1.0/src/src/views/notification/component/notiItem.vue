<template>
	<div class="noti-row" :class="{ 'is-unread': !item.readTime, 'is-compact': compact }" @click="$emit('open', item)">
		<div class="noti-row-icon" :style="{ background: m.bg, color: m.color }">
			<el-icon :size="compact ? 14 : 17"><component :is="m.icon" /></el-icon>
		</div>
		<div class="noti-row-main">
			<div class="noti-row-top">
				<span v-if="!item.readTime" class="noti-row-dot" />
				<span class="noti-row-title" :title="item.title ?? ''">{{ item.title || '—' }}</span>
				<span v-if="showCategory && item.category" class="noti-row-cat">{{ item.category }}</span>
				<span class="noti-row-time">{{ formatDateTime(item.sendTime) }}</span>
			</div>
			<div class="noti-row-bottom">
				<span class="noti-row-content" :title="item.content ?? ''">{{ item.content }}</span>
				<el-button
					v-if="!compact && !item.readTime"
					class="noti-row-read"
					link
					type="primary"
					icon="ele-Checked"
					:title="$t('lz.label.noti.markRead')"
					@click.stop="$emit('mark-read', item)"
				/>
			</div>
		</div>
	</div>
</template>

<script lang="ts" setup name="notiRow">
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import type { NotiListOutput } from '/@/api-services/noti';
import { formatDateTime, severityMeta } from '/@/utils/notiDisplay';

/**
 * Một dòng thông báo dùng chung cho popover chuông, modal "Xem tất cả" và trang quản lý
 * để cả 3 nơi có cùng một bố cục thống nhất.
 * - compact: variant gọn cho popover (font/padding nhỏ, nút đánh dấu đã đọc dạng chữ).
 * - showCategory: hiện chip phân loại (modal + trang quản lý).
 */
const props = defineProps<{
	item: NotiListOutput;
	compact?: boolean;
	showCategory?: boolean;
}>();

defineEmits<{
	(e: 'open', item: NotiListOutput): void;
	(e: 'mark-read', item: NotiListOutput): void;
}>();

const { t: $t } = useI18n();

const m = computed(() => severityMeta(props.item.severity));
</script>

<style lang="scss" scoped>
.noti-row {
	position: relative;
	display: flex;
	align-items: flex-start;
	gap: 11px;
	padding: 11px 14px;
	cursor: pointer;
	border-bottom: 1px solid var(--el-border-color-extra-light);
	transition: background 0.15s;

	&:last-child {
		border-bottom: none;
	}

	&:hover {
		background: var(--el-fill-color-light);

		.noti-row-read {
			opacity: 1;
		}
	}

	&.is-unread {
		background: color-mix(in srgb, var(--el-color-primary) 4%, var(--el-bg-color));
		box-shadow: inset 3px 0 0 var(--el-color-primary);

		&:hover {
			background: color-mix(in srgb, var(--el-color-primary) 8%, var(--el-bg-color));
		}

		.noti-row-title {
			font-weight: 700;
			color: var(--el-text-color-primary);
		}
	}

	&.is-compact {
		padding: 9px 12px;
		gap: 9px;
	}

	&-icon {
		width: 34px;
		height: 34px;
		flex-shrink: 0;
		display: flex;
		align-items: center;
		justify-content: center;
		border-radius: 9px;
		margin-top: 1px;

		.is-compact & {
			width: 30px;
			height: 30px;
		}
	}

	&-main {
		flex: 1;
		min-width: 0;
		display: flex;
		flex-direction: column;
		gap: 3px;
	}

	&-top {
		display: flex;
		align-items: center;
		gap: 6px;
		min-width: 0;
	}

	&-dot {
		width: 7px;
		height: 7px;
		flex-shrink: 0;
		border-radius: 50%;
		background: var(--el-color-danger);
		box-shadow: 0 0 0 3px color-mix(in srgb, var(--el-color-danger) 16%, transparent);
	}

	&-title {
		font-size: 13.5px;
		color: var(--el-text-color-regular);
		white-space: nowrap;
		overflow: hidden;
		text-overflow: ellipsis;

		.is-compact & {
			font-size: 13px;
		}
	}

	&-cat {
		flex-shrink: 0;
		padding: 0 8px;
		border-radius: 999px;
		background: var(--el-fill-color-light);
		border: 1px solid var(--el-border-color-lighter);
		color: var(--el-text-color-secondary);
		font-size: 11px;
		line-height: 18px;
		white-space: nowrap;
	}

	&-time {
		margin-left: auto;
		flex-shrink: 0;
		font-size: 11.5px;
		color: var(--el-text-color-placeholder);
		font-variant-numeric: tabular-nums;
		white-space: nowrap;
	}

	&-bottom {
		display: flex;
		align-items: center;
		gap: 8px;
		min-width: 0;
	}

	&-content {
		flex: 1;
		min-width: 0;
		font-size: 12.5px;
		color: var(--el-text-color-secondary);
		white-space: nowrap;
		overflow: hidden;
		text-overflow: ellipsis;

		.is-compact & {
			font-size: 12px;
		}
	}

	&-read {
		flex-shrink: 0;
		opacity: 0;
		transition: opacity 0.15s;
	}
}
</style>
