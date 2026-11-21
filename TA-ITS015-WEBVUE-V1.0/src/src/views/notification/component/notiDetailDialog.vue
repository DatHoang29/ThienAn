<template>
	<!-- append-to-body: teleport ra body — thoát containing block backdrop-filter của
	     header trafficInfo (NotiBell bị nhốt trong header bigScreen) -->
	<el-dialog v-model="visible" width="600px" destroy-on-close align-center append-to-body class="noti-detail" :class="`is-${m.kind}`">
		<template #header>
			<div class="noti-hero">
				<div class="noti-hero-icon">
					<el-icon :size="24"><component :is="m.icon" /></el-icon>
				</div>
				<div class="noti-hero-main">
					<div class="noti-hero-chips">
						<span class="noti-hero-chip is-severity">
							<span class="noti-hero-dot"></span>
							{{ $t(m.labelKey) }}
						</span>
						<span v-if="state.detail.category" class="noti-hero-chip">{{ state.detail.category }}</span>
					</div>
					<div class="noti-hero-title" :title="state.detail.title ?? ''">{{ state.detail.title || '—' }}</div>
				</div>
			</div>
		</template>

		<div v-loading="state.loading" class="noti-body">
			<span class="noti-section-label">{{ $t('lz.entity.noti.content') }}</span>
			<div class="noti-content">{{ state.detail.content || '—' }}</div>

			<!-- Hành trình thông báo: Gửi → Đọc -->
			<div class="noti-flow">
				<div class="noti-flow-node is-done">
					<div class="noti-flow-icon"><el-icon :size="13"><ele-Promotion /></el-icon></div>
					<div class="noti-flow-text">
						<span class="noti-flow-label">{{ $t('lz.entity.noti.sendTime') }}</span>
						<span class="noti-flow-value">{{ formatDateTime(state.detail.sendTime) || '—' }}</span>
					</div>
				</div>
				<div class="noti-flow-line" :class="{ 'is-done': !!state.detail.readTime }"></div>
				<div class="noti-flow-node" :class="state.detail.readTime ? 'is-done' : 'is-waiting'">
					<div class="noti-flow-icon"><el-icon :size="13"><ele-View v-if="state.detail.readTime" /><ele-Bell v-else /></el-icon></div>
					<div class="noti-flow-text">
						<span class="noti-flow-label">{{ $t('lz.entity.noti.readTime') }}</span>
						<span v-if="state.detail.readTime" class="noti-flow-value">{{ formatDateTime(state.detail.readTime) }}</span>
						<el-tag v-else type="danger" size="small" effect="light" round>{{ $t('lz.label.noti.unread') }}</el-tag>
					</div>
				</div>
			</div>
		</div>

		<template #footer>
			<div class="noti-foot">
				<span class="noti-foot-status" :class="state.detail.readTime ? 'is-read' : 'is-unread'">
					<el-icon :size="13"><ele-CircleCheck v-if="state.detail.readTime" /><ele-AlarmClock v-else /></el-icon>
					{{ state.detail.readTime ? $t('lz.label.noti.read') : $t('lz.label.noti.unread') }}
				</span>
				<div class="noti-foot-actions">
					<el-button @click="visible = false">{{ $t('lz.button.base.close') }}</el-button>
					<el-button v-if="!state.detail.readTime" type="primary" icon="ele-Checked" @click="markRead">{{ $t('lz.label.noti.markRead') }}</el-button>
				</div>
			</div>
		</template>
	</el-dialog>
</template>

<script lang="ts" setup name="notiDetailDialog">
import { computed, reactive, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { getAPI } from '/@/utils/axios-utils';
import { NotiApi } from '/@/api-services/noti';
import type { NotiListOutput } from '/@/api-services/noti';
import { useNotification } from '/@/stores/notification';
import { formatDateTime, severityMeta } from '/@/utils/notiDisplay';

/**
 * Dialog chi tiết thông báo dùng chung (popover chuông, modal "Xem tất cả", trang quản lý).
 * Gọi qua ref: detailDialogRef.open(item). Mở chi tiết của noti chưa đọc sẽ tự đánh dấu đã đọc
 * và phát sự kiện `read` để nơi gọi cập nhật lại dòng trong danh sách của mình.
 */
const { t: $t } = useI18n();
const store = useNotification();

const visible = ref(false);
const state = reactive({
	detail: {} as NotiListOutput,
	loading: false,
});

const emit = defineEmits<{
	(e: 'read', item: NotiListOutput): void;
}>();

const m = computed(() => severityMeta(state.detail.severity));

const open = async (item: NotiListOutput) => {
	state.detail = item;
	visible.value = true;

	// Lấy detail đầy đủ từ API (payload NATS/popover có thể thiếu trường)
	state.loading = true;
	try {
		const { data } = await getAPI(NotiApi).apiNotifyNotiDetailPost({ id: item.id! });
		state.detail = data?.result ?? item;
	} catch {
		state.detail = item;
	} finally {
		state.loading = false;
	}

	if (!item.readTime) {
		await store.markRead([item.id!]);
		state.detail.readTime = state.detail.readTime ?? new Date();
		item.readTime = state.detail.readTime;
		emit('read', item);
	}
};

const markRead = async () => {
	if (!state.detail.id || state.detail.readTime) return;
	await store.markRead([state.detail.id]);
	state.detail.readTime = new Date();
	emit('read', state.detail);
};

defineExpose({ open });
</script>

<style lang="scss">
// Dialog teleport ra body nên style không scoped.
// Bố cục "document sheet": hero navy thống nhất noti-modal, nội dung dạng thẻ diễn giải,
// timeline Gửi → Đọc. Màu nhấn theo mức độ qua 2 biến --noti-accent / --noti-accent-soft.
.noti-detail {
	max-width: calc(100vw - 24px);

	--noti-accent: var(--el-color-primary);
	--noti-accent-soft: var(--el-color-primary-light-9);

	&.is-critical {
		--noti-accent: var(--el-color-danger);
		--noti-accent-soft: var(--el-color-danger-light-9);
	}

	&.is-warning {
		--noti-accent: var(--el-color-warning);
		--noti-accent-soft: var(--el-color-warning-light-9);
	}

	&.is-success {
		--noti-accent: var(--el-color-success);
		--noti-accent-soft: var(--el-color-success-light-9);
	}

	.el-dialog__header {
		position: relative; // neo headerbtn (absolute) vào header thay vì cả dialog
		padding: 0 !important;
		margin-right: 0 !important;
	}

	// KienLT - 09/11/2026: như notiModal — preset depthBlueAmber ép top: 0 nên translateY(-50%) đẩy nút lên;
	// nút phủ trọn chiều cao header và căn giữa bằng flex.
	.el-dialog__headerbtn {
		top: 0 !important;
		bottom: 0;
		right: 6px;
		height: auto !important;
		width: 40px;
		transform: none;
		display: flex;
		align-items: center;
		justify-content: center;

		.el-dialog__close {
			color: rgba(255, 255, 255, 0.85);
			font-size: 18px;

			&:hover {
				color: #ffffff;
			}
		}
	}

	.el-dialog__body {
		padding: 16px 20px 18px !important;
	}

	.el-dialog__footer {
		padding: 13px 20px 15px !important;
		border-top: 1px solid var(--el-border-color-lighter);
	}

	// ─── Hero: băng gradient navy → cobalt thống nhất với noti-modal, icon severity nổi trên nền tối ──
	.noti-hero {
		display: flex;
		align-items: center;
		gap: 14px;
		padding: 18px 44px 18px 20px;
		color: #ffffff;
		background:
			radial-gradient(90% 160% at 100% 0%, color-mix(in srgb, var(--el-color-primary) 34%, transparent) 0%, transparent 55%),
			linear-gradient(120deg, #0f172a 0%, #16305a 55%, var(--el-color-primary) 100%);

		&-icon {
			width: 48px;
			height: 48px;
			flex-shrink: 0;
			display: flex;
			align-items: center;
			justify-content: center;
			border-radius: 14px;
			background: var(--noti-accent);
			color: #ffffff;
			box-shadow: 0 6px 14px -4px rgba(15, 23, 42, 0.55), inset 0 0 0 1px rgba(255, 255, 255, 0.25);
		}

		&-main {
			min-width: 0;
		}

		&-chips {
			display: flex;
			flex-wrap: wrap;
			gap: 6px;
			margin-bottom: 7px;
		}

		&-chip {
			display: inline-flex;
			align-items: center;
			gap: 5px;
			padding: 1px 9px;
			border-radius: 999px;
			background: rgba(255, 255, 255, 0.12);
			border: 1px solid rgba(255, 255, 255, 0.22);
			color: rgba(255, 255, 255, 0.88);
			font-size: 11.5px;
			font-weight: 600;
			line-height: 19px;
			white-space: nowrap;

			&.is-severity {
				background: rgba(255, 255, 255, 0.16);
				border-color: transparent;
				color: #ffffff;
			}
		}

		&-dot {
			width: 6px;
			height: 6px;
			flex-shrink: 0;
			border-radius: 50%;
			background: var(--noti-accent);
			box-shadow: 0 0 0 3px rgba(255, 255, 255, 0.15);
		}

		&-title {
			display: -webkit-box;
			-webkit-line-clamp: 2;
			-webkit-box-orient: vertical;
			overflow: hidden;
			font-size: 17px;
			font-weight: 700;
			line-height: 1.4;
			color: #ffffff;
			word-break: break-word;
		}
	}

	// ─── Body ──────────────────────────────────────────────────────────────
	.noti-body {
		min-height: 120px;
	}

	.noti-section-label {
		display: block;
		margin-bottom: 8px;
		font-size: 11px;
		font-weight: 700;
		letter-spacing: 0.8px;
		text-transform: uppercase;
		color: var(--el-text-color-secondary);
	}

	// Nội dung: thẻ diễn giải nền neutral, căn đọc thoáng — màu mức độ chỉ ở hero & timeline
	.noti-content {
		padding: 14px 16px;
		border-radius: 10px;
		background: var(--el-fill-color-light);
		font-size: 14px;
		line-height: 1.85;
		color: var(--el-text-color-primary);
		white-space: pre-wrap;
		word-break: break-word;
	}

	// ─── Hành trình: Gửi → Đọc, mạch nối đứt nét đến khi được đọc ──────────
	.noti-flow {
		display: flex;
		align-items: center;
		margin-top: 18px;
		padding: 13px 16px;
		border-radius: 10px;
		background: var(--el-fill-color-lighter);

		&-node {
			display: flex;
			align-items: center;
			gap: 9px;
			min-width: 0;

			&.is-done .noti-flow-icon {
				background: var(--noti-accent);
				border-color: transparent;
				color: #ffffff;
				box-shadow: 0 3px 8px -2px color-mix(in srgb, var(--noti-accent) 45%, transparent);
			}

			&.is-waiting .noti-flow-icon {
				border-style: dashed;
			}
		}

		&-icon {
			width: 28px;
			height: 28px;
			flex-shrink: 0;
			display: flex;
			align-items: center;
			justify-content: center;
			border-radius: 50%;
			background: var(--el-bg-color);
			border: 1px solid var(--el-border-color-lighter);
			color: var(--el-text-color-placeholder);
		}

		&-text {
			display: flex;
			flex-direction: column;
			gap: 1px;
			min-width: 0;
		}

		&-label {
			font-size: 10.5px;
			color: var(--el-text-color-secondary);
			white-space: nowrap;
		}

		&-value {
			font-size: 12.5px;
			font-weight: 600;
			color: var(--el-text-color-primary);
			font-variant-numeric: tabular-nums;
			white-space: nowrap;
		}

		&-line {
			flex: 1;
			height: 2px;
			min-width: 18px;
			margin: 0 12px;
			border-radius: 2px;
			background: repeating-linear-gradient(90deg, var(--el-border-color) 0 5px, transparent 5px 9px);

			&.is-done {
				background: var(--noti-accent);
			}
		}
	}

	// ─── Footer: pill trạng thái bên trái, nhóm nút bên phải ───────────────
	.noti-foot {
		display: flex;
		align-items: center;
		gap: 10px;
	}

	.noti-foot-status {
		display: inline-flex;
		align-items: center;
		gap: 6px;
		padding: 3px 10px;
		border-radius: 999px;
		font-size: 12px;
		font-weight: 600;

		&.is-unread {
			background: var(--el-color-danger-light-9);
			color: var(--el-color-danger);
		}

		&.is-read {
			background: var(--el-color-success-light-9);
			color: var(--el-color-success);
		}
	}

	.noti-foot-actions {
		display: flex;
		margin-left: auto;
		gap: 8px;
	}

	// Màn hẹp: hành trình xếp dọc theo trục icon
	@media (max-width: 520px) {
		.noti-flow {
			flex-direction: column;
			align-items: flex-start;
			gap: 4px;

			.noti-flow-line {
				flex: none;
				width: 2px;
				height: 14px;
				margin: 3px 0 3px 13px;
				background: repeating-linear-gradient(180deg, var(--el-border-color) 0 4px, transparent 4px 8px);

				&.is-done {
					background: var(--noti-accent);
				}
			}
		}
	}
}
</style>
