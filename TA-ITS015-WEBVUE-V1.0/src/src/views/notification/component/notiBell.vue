<template>
	<div class="noti-bell" :class="{ 'is-dark': dark }">
		<el-popover ref="popoverRef" placement="bottom-end" trigger="click" :width="392" :persistent="false" popper-class="noti-bell-popover" @show="onPopoverShow">
				<template #reference>
				<div class="noti-bell-icon">
					<el-badge :value="store.unreadCount" :max="99" :hidden="!store.hasUnread">
						<el-icon :title="$t('lz.label.noti.title')">
							<ele-Bell />
						</el-icon>
					</el-badge>
				</div>
			</template>

			<!-- Header -->
			<div class="noti-bell-header">
				<div class="noti-bell-header-title">
					<span>{{ $t('lz.label.noti.title') }}</span>
					<span v-if="store.hasUnread" class="noti-bell-header-badge">{{ store.unreadCount }}</span>
				</div>
				<el-tooltip :content="$t('lz.label.noti.markAllRead')" placement="top">
					<el-button class="is-ghost" circle size="small" icon="ele-Checked" :disabled="!store.hasUnread" @click="onMarkAllRead" />
				</el-tooltip>
			</div>

			<!-- Danh sách CHƯA ĐỌC mới nhất — fix cứng chiều cao để popover không giật khi load/rỗng -->
			<el-scrollbar height="430px" v-loading="store.bellLoading">
				<template v-if="store.bellList.length">
					<NotiRow v-for="item in store.bellList" :key="item.id" :item="item" compact @open="openDetail" />
				</template>
				<div v-else-if="!store.bellLoading" class="noti-bell-empty">
					<div class="noti-bell-empty-icon"><el-icon :size="26"><ele-Bell /></el-icon></div>
					<p>{{ $t('lz.label.noti.emptyUnread') }}</p>
				</div>
			</el-scrollbar>

			<!-- Footer -->
			<div class="noti-bell-footer">
				<el-button text type="primary" icon="ele-Right" @click="openModal">{{ $t('lz.label.noti.viewAll') }}</el-button>
			</div>
		</el-popover>

		<!-- Xem tất cả: modal danh sách đầy đủ -->
		<NotiModal v-model="modalVisible" />

		<!-- Chi tiết thông báo -->
		<NotiDetailDialog ref="detailRef" @read="onItemRead" />
	</div>
</template>

<script lang="ts" setup name="notiBell">
import { defineAsyncComponent, onMounted, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import type { NotiListOutput } from '/@/api-services/noti';
import { useNotification } from '/@/stores/notification';
import { useTransporterNoti } from '/@/transporter/hooks/useTransporterNoti';

/**
 * Chuông thông báo trên topBar (mount toàn thời gian phiên đăng nhập):
 * - nhận noti realtime qua NATS (subject ta.its.noti) đưa vào store;
 * - popover danh sách mới nhất;
 * - "Xem tất cả" mở modal danh sách đầy đủ.
 */
const { t: $t } = useI18n();
const store = useNotification();

/** dark: biến thể nền tối (dùng trên header màn hình trafficInfo). */
defineProps<{ dark?: boolean }>();

const NotiRow = defineAsyncComponent(() => import('./notiItem.vue'));
const NotiModal = defineAsyncComponent(() => import('./notiModal.vue'));
const NotiDetailDialog = defineAsyncComponent(() => import('./notiDetailDialog.vue'));

const popoverRef = ref();
const detailRef = ref<InstanceType<typeof NotiDetailDialog>>();
const modalVisible = ref(false);

// Nhận noti realtime qua NATS. Component nằm trên topBar nên không bỏ sót thông báo.
useTransporterNoti((data) => store.receive(data));

onMounted(async () => {
	await store.init();
});

// Mở popover: làm mới danh sách + số chưa đọc
const onPopoverShow = async () => {
	await Promise.all([store.fetchBellList(), store.fetchUnreadCount()]);
};

const closePopover = () => popoverRef.value?.hide();

// Click dòng → mở chi tiết (tự đánh dấu đã đọc)
const openDetail = (item: NotiListOutput) => {
	detailRef.value?.open(item);
};

// Dialog chi tiết đã mutate readTime vào item + store.markRead đồng bộ badge
const onItemRead = () => {
	store.fetchUnreadCount();
};

const onMarkAllRead = async () => {
	await store.markAllRead();
};

// "Xem tất cả" → modal danh sách đầy đủ
const openModal = () => {
	closePopover();
	modalVisible.value = true;
};
</script>

<style scoped lang="scss">
.noti-bell {
	display: flex;
	align-items: center;

	&-icon {
		padding: 0 10px;
		cursor: pointer;
		color: var(--next-bg-topBarColor);
		height: 50px;
		line-height: 50px;
		display: flex;
		align-items: center;

		&:hover {
			background: var(--next-color-user-hover);
			i {
				display: inline-block;
				animation: logoAnimation 0.3s ease-in-out;
			}
		}

		:deep(.el-badge) {
			height: 40px;
			line-height: 40px;
			display: flex;
			align-items: center;
		}

		:deep(.el-badge__content.is-fixed) {
			top: 12px;
		}
	}

	// Biến thể nền tối (trafficInfo): thu nhỏ về 32px để khớp scale của hàng
	// header bigScreen (nút Home/UserInfo cao 28px), tránh vỡ cấu trúc hàng 60px
	&.is-dark &-icon {
		height: 32px;
		line-height: 32px;
		padding: 0 6px;
		color: var(--el-color-white);
		font-size: 18px;

		&:hover {
			background: rgba(255, 255, 255, 0.14);
		}

		:deep(.el-badge__content.is-fixed) {
			top: 3px;
		}
	}
}
</style>

<style lang="scss">
// Popover render ở body nên style không scoped
.noti-bell-popover {
	padding: 0 !important;
	border-radius: 12px;
	overflow: hidden;
	// Đè z-index màn bigScreen (header 1200, map overlay) và bắt buộc Inline style của popper
	z-index: 3000 !important;
	// Viền + bóng rõ ràng để popover không bị "chìm" vào header nền tối của trafficInfo
	border: 1px solid var(--el-border-color-light) !important;
	box-shadow: 0 18px 44px -12px rgba(15, 23, 42, 0.45) !important;

	.noti-bell-header {
		display: flex;
		align-items: center;
		justify-content: space-between;
		padding: 11px 12px 11px 14px;
		border-bottom: 1px solid var(--el-border-color-lighter);
		background:
			radial-gradient(90% 160% at 100% 0%, color-mix(in srgb, var(--el-color-primary) 30%, transparent) 0%, transparent 60%),
			linear-gradient(120deg, #0f172a 0%, #16305a 60%, var(--el-color-primary) 100%);

		&-title {
			display: flex;
			align-items: center;
			gap: 8px;
			font-weight: 700;
			font-size: 14px;
			color: #ffffff;
		}

		&-badge {
			min-width: 20px;
			padding: 0 6px;
			border-radius: 999px;
			background: var(--el-color-danger);
			color: #ffffff;
			font-size: 11px;
			font-weight: 600;
			line-height: 18px;
			text-align: center;
		}

		.el-button.is-ghost {
			background: rgba(255, 255, 255, 0.12);
			border-color: rgba(255, 255, 255, 0.35);
			color: #ffffff;

			&:hover:not(.is-disabled) {
				background: rgba(255, 255, 255, 0.22);
				border-color: rgba(255, 255, 255, 0.55);
				color: #ffffff;
			}

			&.is-disabled {
				background: rgba(255, 255, 255, 0.12);
				border-color: transparent;
				color: rgba(255, 255, 255, 0.4);
			}
		}
	}

	.noti-bell-empty {
		// Cao bằng khung scrollbar (430px) để empty-state luôn canh giữa, popover không bị thu thắt
		height: 430px;
		display: flex;
		flex-direction: column;
		align-items: center;
		justify-content: center;
		gap: 8px;

		&-icon {
			width: 56px;
			height: 56px;
			display: flex;
			align-items: center;
			justify-content: center;
			border-radius: 50%;
			background: var(--el-color-primary-light-9);
			color: var(--el-color-primary);
		}

		p {
			margin: 0;
			font-size: 12.5px;
			color: var(--el-text-color-secondary);
		}
	}

	.noti-bell-footer {
		display: flex;
		justify-content: center;
		padding: 5px 0;
		border-top: 1px solid var(--el-border-color-lighter);
		background: var(--el-fill-color-lighter);

		.el-button {
			width: 100%;
		}
	}
}
</style>
