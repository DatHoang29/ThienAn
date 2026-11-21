<template>
	<!-- append-to-body: teleport ra body — trên trafficInfo dialog bị nhốt trong containing
	     block backdrop-filter của header bigScreen nếu render tại chỗ (vỡ bố cục) -->
	<el-dialog v-model="visible" width="760px" destroy-on-close align-center top="6vh" append-to-body class="noti-modal">
		<template #header>
			<div class="noti-modal-header">
				<div class="noti-modal-header-title">
					<el-icon :size="18"><ele-Bell /></el-icon>
					<span>{{ $t('lz.label.noti.title') }}</span>
					<span v-if="store.hasUnread" class="noti-modal-header-badge">{{ store.unreadCount }}</span>
				</div>
			</div>
		</template>

		<!-- Bộ lọc + thao tác -->
		<div class="noti-modal-toolbar">
			<el-radio-group v-model="state.filters.read" @change="handleSearch">
				<el-radio-button value="unread">{{ $t('lz.label.noti.unread') }}</el-radio-button>
				<el-radio-button value="read">{{ $t('lz.label.noti.read') }}</el-radio-button>
				<el-radio-button value="all">{{ $t('lz.label.noti.all') }}</el-radio-button>
			</el-radio-group>
			<el-select v-model="state.filters.severity" clearable filterable allow-create default-first-option :placeholder="$t('lz.entity.noti.severity')" class="noti-modal-severity" @change="handleSearch">
				<el-option :label="$t('lz.label.noti.severity.info')" value="Info" />
				<el-option :label="$t('lz.label.noti.severity.warning')" value="Warning" />
				<el-option :label="$t('lz.label.noti.severity.critical')" value="Critical" />
			</el-select>
			<el-input v-model="state.filters.keyword" clearable :placeholder="$t('lz.entity.noti.keyword')" class="noti-modal-keyword" @keyup.enter="handleSearch" @clear="handleSearch">
				<template #prefix><el-icon><ele-Search /></el-icon></template>
			</el-input>
			<el-tooltip :content="$t('lz.button.base.refresh')" placement="top">
				<el-button circle icon="ele-Refresh" :loading="state.loading" @click="load" />
			</el-tooltip>
			<el-button type="primary" plain icon="ele-Checked" :disabled="!store.hasUnread" :loading="state.markingAll" @click="markAllRead">
				{{ $t('lz.label.noti.markAllRead') }}
			</el-button>
		</div>

		<!-- Danh sách -->
		<div v-loading="state.loading" class="noti-modal-list">
			<template v-if="state.items.length">
				<template v-for="group in groups" :key="group.key">
					<div class="noti-modal-group">
						<span>{{ $t(groupLabelKey(group.key)) }}</span>
						<span class="noti-modal-group-count">{{ group.items.length }}</span>
					</div>
					<NotiRow v-for="item in group.items" :key="item.id" :item="item" show-category @open="openDetail" @mark-read="handleMarkRead" />
				</template>
			</template>
			<div v-else-if="!state.loading" class="noti-modal-empty">
				<div class="noti-modal-empty-icon"><el-icon :size="30"><ele-Bell /></el-icon></div>
				<p>{{ $t('lz.label.noti.empty') }}</p>
			</div>
		</div>

		<template #footer>
			<div class="noti-modal-footer">
				<vxe-pager
					size="mini"
					v-model:current-page="state.page"
					v-model:page-size="state.pageSize"
					:total="state.total"
					:page-sizes="[10, 20, 50]"
					:layouts="['PrevJump', 'PrevPage', 'Number', 'NextPage', 'NextJump', 'Sizes', 'Total']"
					@page-change="load"
				/>
				<el-button size="small" type="primary" plain icon="ele-OfficeBuilding" @click="openManagementPage">
					{{ $t('lz.label.noti.openManagement') }}
				</el-button>
			</div>
		</template>

		<NotiDetailDialog ref="detailRef" @read="onItemRead" />
	</el-dialog>
</template>

<script lang="ts" setup name="notiModal">
import { computed, defineAsyncComponent, reactive, ref, watch } from 'vue';
import { useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { getAPI } from '/@/utils/axios-utils';
import { NotiApi } from '/@/api-services/noti';
import type { NotiListOutput, NotiPageInput } from '/@/api-services/noti';
import { useNotification } from '/@/stores/notification';
import { useTransporterNoti } from '/@/transporter/hooks/useTransporterNoti';
import { groupByDate, type NotiDateGroup } from '/@/utils/notiDisplay';

/**
 * Modal "Xem tất cả" mở từ chuông thông báo trên header: danh sách đầy đủ
 * có bộ lọc, phân trang (vxe-pager thống nhất với các bảng của dự án) và
 * xem chi tiết. Vẫn điều hướng sang trang quản lý qua nút ở footer.
 */
const { t: $t } = useI18n();
const router = useRouter();
const store = useNotification();

const NotiRow = defineAsyncComponent(() => import('./notiItem.vue'));
const NotiDetailDialog = defineAsyncComponent(() => import('./notiDetailDialog.vue'));

const props = defineProps<{ modelValue: boolean }>();
const emit = defineEmits<{ (e: 'update:modelValue', value: boolean): void }>();

const visible = computed({
	get: () => props.modelValue,
	set: (value) => emit('update:modelValue', value),
});

const detailRef = ref<InstanceType<typeof NotiDetailDialog>>();

const state = reactive({
	items: [] as NotiListOutput[],
	total: 0,
	page: 1,
	pageSize: 10,
	loading: false,
	markingAll: false,
	filters: {
		// Mặc định là CHƯA ĐỌC — muốn xem cái đã đọc/tất cả thì chuyển tab
		read: 'unread' as 'all' | 'unread' | 'read',
		severity: undefined as string | undefined,
		keyword: '',
	},
});

const groups = computed(() => groupByDate(state.items));

const groupLabelKey = (key: NotiDateGroup) => `lz.label.noti.group.${key}`;

// Mở modal là tải lại danh sách + badge
watch(visible, (value) => {
	if (value) {
		state.page = 1;
		store.fetchUnreadCount();
		load();
	}
});

// Có noti mới qua NATS khi modal đang mở: nạp lại trang hiện tại để danh sách "nhảy"
// dòng mới (badge/chuông đã do store.receive cập nhật rồi).
useTransporterNoti(() => {
	if (!visible.value || state.loading) return;
	load();
});

const buildParams = (): NotiPageInput => ({
	page: state.page,
	pageSize: state.pageSize,
	field: 'sendTime',
	order: 'desc',
	descStr: 'desc',
	keyword: state.filters.keyword || undefined,
	severity: state.filters.severity || undefined,
	// isRead: null = không lọc ("Tất cả") — gửi tường minh null theo contract swagger (boolean | null)
	isRead: state.filters.read === 'all' ? null : state.filters.read === 'read',
});

const load = async () => {
	state.loading = true;
	try {
		const { data } = await getAPI(NotiApi).apiNotifyNotiPagePost(buildParams());
		state.items = data?.result?.records ?? [];
		state.total = data?.result?.total ?? 0;
	} finally {
		state.loading = false;
	}
};

const handleSearch = () => {
	state.page = 1;
	load();
};

const openDetail = (item: NotiListOutput) => {
	detailRef.value?.open(item);
};

// Dialog chi tiết đã tự đánh dấu đã đọc (mutate thẳng vào item) → chỉ cần đồng bộ badge
const onItemRead = () => {
	store.fetchUnreadCount();
};

const handleMarkRead = async (item: NotiListOutput) => {
	await store.markRead([item.id!]);
	item.readTime = item.readTime ?? new Date();
};

const markAllRead = async () => {
	state.markingAll = true;
	try {
		await store.markAllRead();
		await load();
	} finally {
		state.markingAll = false;
	}
};

const openManagementPage = () => {
	visible.value = false;
	router.push('/notification');
};
</script>

<style lang="scss">
// Dialog teleport ra body nên style không scoped
.noti-modal {
	max-width: calc(100vw - 24px);

	// Neo pager vxe vào cùng hệ biến bề mặt của dialog (Element Plus). "Chế độ tối" trong
	// cài đặt giao diện đặt data-vxe-ui-theme="dark" trên <html>, định nghĩa lại biến vxe
	// từ gốc nên trôi vào cả dialog đã teleport ra body (pager nền #121212 trên dialog trắng).
	// Khai báo lại tại đây giúp pager luôn đồng bộ màu với dialog; đổi theme toàn cục vẫn
	// theo vì tham chiếu biến --el-*.
	--vxe-ui-layout-background-color: var(--el-bg-color);
	--vxe-ui-font-color: var(--el-text-color-regular);
	--vxe-ui-font-primary-color: var(--el-color-primary);
	--vxe-ui-font-lighten-color: var(--el-text-color-secondary);
	--vxe-ui-font-darken-color: var(--el-text-color-primary);
	--vxe-ui-font-disabled-color: var(--el-text-color-placeholder);
	--vxe-ui-input-border-color: var(--el-border-color);
	--vxe-ui-input-background-color: var(--el-bg-color);
	--vxe-ui-input-placeholder-color: var(--el-text-color-placeholder);

	// Block dark của vxe còn bật color-scheme: dark kế thừa vào modal — trả về light
	// để điều khiển native (scrollbar...) đồng bộ bề mặt sáng của dialog.
	color-scheme: light;

	.el-dialog__header {
		position: relative; // neo headerbtn (absolute) vào header thay vì cả dialog
		padding: 0 !important;
		margin-right: 0 !important;
	}

	// KienLT - 09/11/2026: preset depthBlueAmber đặt headerbtn top: 0 + height 47px với selector mạnh hơn
	// (html.design-depth-blue-amber ...) ⇒ cộng translateY(-50%) ở đây đẩy nút lên nửa chiều cao. Kéo nút phủ
	// trọn chiều cao header rồi căn giữa bằng flex: đúng với mọi preset, không phụ thuộc chiều cao nút.
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
		padding: 12px 16px 0 !important;
	}

	.el-dialog__footer {
		padding: 6px 16px 12px !important;
		border-top: 1px solid var(--el-border-color-lighter);
	}

	.noti-modal-header {
		display: flex;
		align-items: center;
		gap: 12px;
		padding: 14px 44px 14px 16px;
		color: #ffffff;
		background:
			radial-gradient(90% 160% at 100% 0%, color-mix(in srgb, var(--el-color-primary) 34%, transparent) 0%, transparent 55%),
			linear-gradient(120deg, #0f172a 0%, #16305a 55%, var(--el-color-primary) 100%);

		&-title {
			display: flex;
			align-items: center;
			gap: 9px;
			font-size: 15px;
			font-weight: 700;
		}

		&-badge {
			min-width: 20px;
			padding: 0 7px;
			border-radius: 999px;
			background: var(--el-color-danger);
			color: #ffffff;
			font-size: 11px;
			font-weight: 600;
			line-height: 18px;
			text-align: center;
		}
	}

	.noti-modal-toolbar {
		display: flex;
		flex-wrap: wrap;
		align-items: center;
		gap: 8px;
		padding-bottom: 10px;

		.noti-modal-severity {
			width: 140px;
		}

		.noti-modal-keyword {
			width: 170px;
			margin-left: auto;
		}
	}

	.noti-modal-list {
		min-height: 180px;
		max-height: calc(100vh - 340px);
		overflow: auto;
		border: 1px solid var(--el-border-color-lighter);
		border-radius: 10px;

		.noti-modal-group {
			position: sticky;
			top: 0;
			z-index: 1;
			display: flex;
			align-items: center;
			gap: 8px;
			padding: 7px 14px;
			background: var(--el-fill-color-lighter);
			border-bottom: 1px solid var(--el-border-color-lighter);
			font-size: 11.5px;
			font-weight: 700;
			text-transform: uppercase;
			letter-spacing: 0.6px;
			color: var(--el-text-color-secondary);

			.noti-modal-group-count {
				min-width: 20px;
				padding: 0 6px;
				border-radius: 999px;
				background: var(--el-bg-color);
				border: 1px solid var(--el-border-color-lighter);
				color: var(--el-text-color-secondary);
				font-size: 10.5px;
				line-height: 16px;
				text-align: center;
			}
		}
	}

	.noti-modal-empty {
		display: flex;
		flex-direction: column;
		align-items: center;
		gap: 8px;
		padding: 44px 0;

		&-icon {
			width: 60px;
			height: 60px;
			display: flex;
			align-items: center;
			justify-content: center;
			border-radius: 50%;
			background: var(--el-color-primary-light-9);
			color: var(--el-color-primary);
		}

		p {
			margin: 0;
			font-size: 13px;
			color: var(--el-text-color-secondary);
		}
	}

	.noti-modal-footer {
		display: flex;
		align-items: center;
		justify-content: space-between;
		gap: 10px;
		flex-wrap: wrap;
	}
}
</style>
