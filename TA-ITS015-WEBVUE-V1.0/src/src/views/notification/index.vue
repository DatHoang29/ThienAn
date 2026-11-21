<template>
	<div class="noti-page">
		<!-- ─── Hero ─────────────────────────────────────────────────────────── -->
		<div class="noti-hero">
			<div class="noti-hero-left">
				<div class="noti-hero-icon">
					<el-icon :size="26"><ele-Bell /></el-icon>
				</div>
				<div class="noti-hero-text">
					<div class="noti-hero-title">{{ $t('lz.label.noti.management') }}</div>
					<div class="noti-hero-subtitle">{{ $t('lz.label.noti.heroSubtitle') }}</div>
				</div>
			</div>
			<div class="noti-hero-actions">
				<el-tooltip :content="$t('lz.button.base.refresh')" placement="top">
					<el-button class="is-ghost" circle icon="ele-Refresh" :loading="state.loading" @click="handleRefresh" />
				</el-tooltip>
				<el-button class="is-cta" icon="ele-Checked" :disabled="store.unreadCount === 0" :loading="state.marking" @click="handleMarkAllRead">
					{{ $t('lz.label.noti.markAllRead') }}
				</el-button>
			</div>
		</div>

		<!-- ─── Stats ────────────────────────────────────────────────────────── -->
		<div class="noti-stats">
			<button type="button" class="noti-stat" :class="{ 'is-danger': store.unreadCount > 0 }" @click="applyReadFilter('unread')">
				<div class="noti-stat-icon is-danger">
					<el-icon :size="20"><ele-Bell /></el-icon>
				</div>
				<div class="noti-stat-text">
					<div class="noti-stat-value">{{ store.unreadCount }}</div>
					<div class="noti-stat-label">{{ $t('lz.label.noti.stat.unread') }}</div>
				</div>
			</button>
			<button type="button" class="noti-stat" @click="applyTodayFilter">
				<div class="noti-stat-icon is-primary">
					<el-icon :size="20"><ele-Clock /></el-icon>
				</div>
				<div class="noti-stat-text">
					<div class="noti-stat-value">{{ state.statToday }}</div>
					<div class="noti-stat-label">{{ $t('lz.label.noti.stat.today') }}</div>
				</div>
			</button>
			<button type="button" class="noti-stat" @click="resetFilters">
				<div class="noti-stat-icon is-navy">
					<el-icon :size="20"><ele-Files /></el-icon>
				</div>
				<div class="noti-stat-text">
					<div class="noti-stat-value">{{ state.statTotal }}</div>
					<div class="noti-stat-label">{{ $t('lz.label.noti.stat.total') }}</div>
				</div>
			</button>
		</div>

		<!-- ─── Toolbar ──────────────────────────────────────────────────────── -->
		<div class="noti-toolbar">
			<el-radio-group v-model="state.filters.read" @change="handleSearch">
				<el-radio-button value="unread">{{ $t('lz.label.noti.unread') }}</el-radio-button>
				<el-radio-button value="read">{{ $t('lz.label.noti.read') }}</el-radio-button>
				<el-radio-button value="all">{{ $t('lz.label.noti.all') }}</el-radio-button>
			</el-radio-group>
			<el-select v-model="state.filters.severity" clearable filterable allow-create default-first-option :placeholder="$t('lz.entity.noti.severity')" class="noti-toolbar-severity" @change="handleSearch">
				<el-option :label="$t('lz.label.noti.severity.info')" value="Info" />
				<el-option :label="$t('lz.label.noti.severity.warning')" value="Warning" />
				<el-option :label="$t('lz.label.noti.severity.critical')" value="Critical" />
			</el-select>
			<el-input v-model="state.filters.category" clearable :placeholder="$t('lz.entity.noti.category')" class="noti-toolbar-input" @keyup.enter="handleSearch" @clear="handleSearch">
				<template #prefix><el-icon><ele-CollectionTag /></el-icon></template>
			</el-input>
			<el-date-picker v-model="state.filters.range" type="datetimerange" :format="DateTimeFormatEnum.DateTimeDefault" :start-placeholder="$t('lz.entity.base.startTime')" :end-placeholder="$t('lz.entity.base.endTime')" class="noti-toolbar-range" @change="handleSearch" />
			<el-input v-model="state.filters.keyword" clearable :placeholder="$t('lz.entity.noti.keyword')" class="noti-toolbar-input is-search" @keyup.enter="handleSearch" @clear="handleSearch">
				<template #prefix><el-icon><ele-Search /></el-icon></template>
			</el-input>
		</div>

		<!-- ─── Danh sách ────────────────────────────────────────────────────── -->
		<div v-loading="state.loading" class="noti-body">
			<template v-if="state.items.length">
				<template v-for="group in groups" :key="group.key">
					<div class="noti-group">
						<span>{{ $t(groupLabelKey(group.key)) }}</span>
						<span class="noti-group-count">{{ group.items.length }}</span>
					</div>
					<NotiRow v-for="item in group.items" :key="item.id" :item="item" show-category @open="openDetail" @mark-read="handleMarkRead" />
				</template>
			</template>
			<div v-else-if="!state.loading" class="noti-empty">
				<div class="noti-empty-icon"><el-icon :size="42"><ele-Bell /></el-icon></div>
				<p class="noti-empty-text">{{ $t('lz.label.noti.empty') }}</p>
				<el-button text type="primary" icon="ele-Refresh" @click="resetFilters">{{ $t('lz.label.noti.viewAll') }}</el-button>
			</div>
		</div>

		<!-- ─── Phân trang (vxe-pager thống nhất với các bảng của dự án) ─────── -->
		<div class="noti-pager">
			<vxe-pager
				size="mini"
				v-model:current-page="state.page"
				v-model:page-size="state.pageSize"
				:total="state.total"
				:page-sizes="[10, 20, 50, 100]"
				:layouts="['PrevJump', 'PrevPage', 'Number', 'NextPage', 'NextJump', 'Sizes', 'Total']"
				@page-change="handlePageChange"
			/>
		</div>

		<!-- ─── Chi tiết thông báo ───────────────────────────────────────────── -->
		<NotiDetailDialog ref="detailRef" @read="onItemRead" />
	</div>
</template>

<script lang="ts" setup name="sysNoti">
import { computed, defineAsyncComponent, onMounted, reactive, ref } from 'vue';
import { dayjs } from 'element-plus';
import { DateTimeFormatEnum } from '/@/types/enums/baseEnum';
import { useI18n } from 'vue-i18n';
import { Local } from '/@/utils/storage';
import { getAPI } from '/@/utils/axios-utils';
import { NotiApi } from '/@/api-services/noti';
import type { NotiListOutput, NotiPageInput } from '/@/api-services/noti';
import { useNotification } from '/@/stores/notification';
import { useTransporterNoti } from '/@/transporter/hooks/useTransporterNoti';
import { groupByDate, type NotiDateGroup } from '/@/utils/notiDisplay';

const { t: $t } = useI18n();

const store = useNotification();

const NotiRow = defineAsyncComponent(() => import('./component/notiItem.vue'));
const NotiDetailDialog = defineAsyncComponent(() => import('./component/notiDetailDialog.vue'));

const localPageParamKey = 'localPageParam:sysNoti';
const detailRef = ref<InstanceType<typeof NotiDetailDialog>>();

const state = reactive({
	items: [] as NotiListOutput[],
	total: 0,
	page: 1,
	pageSize: Local.get(localPageParamKey)?.pageSize ?? 20,
	loading: false,
	marking: false,
	statToday: 0,
	statTotal: 0,
	filters: {
		read: 'all' as 'all' | 'unread' | 'read',
		severity: undefined as string | undefined,
		category: '',
		keyword: '',
		range: [dayjs().subtract(30, 'day').toDate(), dayjs().toDate()] as [Date, Date] | null,
	},
});

// ─── Hiển thị ───────────────────────────────────────────────────────────────

const groups = computed(() => groupByDate(state.items));

const groupLabelKey = (key: NotiDateGroup) => `lz.label.noti.group.${key}`;

// ─── Truy vấn ───────────────────────────────────────────────────────────────

const buildParams = (): NotiPageInput => ({
	page: state.page,
	pageSize: state.pageSize,
	field: 'sendTime',
	order: 'desc',
	descStr: 'desc',
	keyword: state.filters.keyword || undefined,
	category: state.filters.category || undefined,
	severity: state.filters.severity || undefined,
	// isRead: null = không lọc ("Tất cả") — gửi tường minh null theo contract swagger (boolean | null)
	isRead: state.filters.read === 'all' ? null : state.filters.read === 'read',
	fromTime: state.filters.range?.[0] ?? undefined,
	toTime: state.filters.range?.[1] ?? undefined,
});

const loadList = async () => {
	state.loading = true;
	try {
		const { data } = await getAPI(NotiApi).apiNotifyNotiPagePost(buildParams());
		state.items = data?.result?.records ?? [];
		state.total = data?.result?.total ?? 0;
	} finally {
		state.loading = false;
	}
};

/** Đếm nhanh cho 2 tile thống kê (pageSize=1, chỉ cần total). */
const loadStats = async () => {
	try {
		const [todayRes, totalRes] = await Promise.all([
			getAPI(NotiApi).apiNotifyNotiPagePost({ page: 1, pageSize: 1, fromTime: dayjs().startOf('day').toDate() }),
			getAPI(NotiApi).apiNotifyNotiPagePost({ page: 1, pageSize: 1 }),
		]);
		state.statToday = todayRes.data?.result?.total ?? 0;
		state.statTotal = totalRes.data?.result?.total ?? 0;
	} catch (error) {
		console.error('[Noti] loadStats error:', error);
	}
};

const handleSearch = () => {
	state.page = 1;
	loadList();
};

// vxe-pager phát page-change cho cả đổi trang và đổi số dòng/trang
const handlePageChange = () => {
	Local.set(localPageParamKey, { pageSize: state.pageSize });
	loadList();
};

const handleRefresh = async () => {
	await Promise.all([loadList(), loadStats(), store.fetchUnreadCount()]);
};

const applyReadFilter = (read: 'all' | 'unread' | 'read') => {
	state.filters.read = read;
	handleSearch();
};

const applyTodayFilter = () => {
	state.filters.range = [dayjs().startOf('day').toDate(), dayjs().endOf('day').toDate()];
	handleSearch();
};

const resetFilters = () => {
	state.filters.read = 'all';
	state.filters.severity = undefined;
	state.filters.category = '';
	state.filters.keyword = '';
	state.filters.range = null;
	handleSearch();
};

// ─── Đánh dấu đã đọc ────────────────────────────────────────────────────────

const handleMarkRead = async (item: NotiListOutput) => {
	await store.markRead([item.id!]);
	item.readTime = item.readTime ?? new Date();
};

const handleMarkAllRead = async () => {
	state.marking = true;
	try {
		await store.markAllRead();
		await Promise.all([loadList(), loadStats()]);
	} finally {
		state.marking = false;
	}
};

// ─── Xem chi tiết ───────────────────────────────────────────────────────────

const openDetail = (item: NotiListOutput) => {
	detailRef.value?.open(item);
};

// Dialog chi tiết đã mutate readTime vào item + store.markRead đồng bộ badge
const onItemRead = () => {
	store.fetchUnreadCount();
};

// ─── Khởi tạo ───────────────────────────────────────────────────────────────

onMounted(async () => {
	await Promise.all([loadList(), loadStats(), store.init()]);
});

// Nhận noti mới qua NATS (subject ta.its.noti): tự nạp lại danh sách + thống kê để
// trang quản lý luôn hiển thị thông tin mới mà không cần F5 (badge do store.receive lo).
useTransporterNoti(() => {
	if (state.loading) return;
	loadList();
	loadStats();
});
</script>

<style lang="scss" scoped>
.noti-page {
	display: flex;
	flex-direction: column;
	gap: 10px;
	height: 100%;
}

// ─── Hero ───────────────────────────────────────────────────────────────────
.noti-hero {
	display: flex;
	align-items: center;
	justify-content: space-between;
	gap: 12px;
	padding: 16px 20px;
	border-radius: 10px;
	color: #ffffff;
	background:
		radial-gradient(90% 160% at 100% 0%, color-mix(in srgb, var(--el-color-primary) 34%, transparent) 0%, transparent 55%),
		linear-gradient(120deg, #0f172a 0%, #16305a 55%, var(--el-color-primary) 100%);
	box-shadow: 0 8px 20px -10px color-mix(in srgb, var(--el-color-primary) 55%, transparent);

	&-left {
		display: flex;
		align-items: center;
		gap: 14px;
		min-width: 0;
	}

	&-icon {
		width: 48px;
		height: 48px;
		flex-shrink: 0;
		display: flex;
		align-items: center;
		justify-content: center;
		border-radius: 12px;
		background: rgba(255, 255, 255, 0.14);
		border: 1px solid rgba(255, 255, 255, 0.22);
	}

	&-title {
		font-size: 18px;
		font-weight: 700;
		letter-spacing: 0.2px;
	}

	&-subtitle {
		margin-top: 2px;
		font-size: 12.5px;
		color: rgba(255, 255, 255, 0.72);
	}

	&-actions {
		display: flex;
		align-items: center;
		gap: 8px;
		flex-shrink: 0;

		.is-ghost {
			background: rgba(255, 255, 255, 0.12);
			border-color: rgba(255, 255, 255, 0.35);
			color: #ffffff;

			&:hover {
				background: rgba(255, 255, 255, 0.22);
				border-color: rgba(255, 255, 255, 0.55);
				color: #ffffff;
			}
		}

		.is-cta {
			background: #ffffff;
			border-color: #ffffff;
			color: #0f172a;
			font-weight: 600;

			&:hover:not(.is-disabled) {
				background: rgba(255, 255, 255, 0.88);
				border-color: rgba(255, 255, 255, 0.88);
				color: #0f172a;
			}

			&.is-disabled {
				background: rgba(255, 255, 255, 0.25);
				border-color: transparent;
				color: rgba(255, 255, 255, 0.65);
			}
		}
	}
}

// ─── Stats ──────────────────────────────────────────────────────────────────
.noti-stats {
	display: grid;
	grid-template-columns: repeat(3, minmax(0, 1fr));
	gap: 10px;
}

.noti-stat {
	display: flex;
	align-items: center;
	gap: 12px;
	padding: 12px 16px;
	border: 1px solid var(--el-border-color-lighter);
	border-radius: 10px;
	background: var(--el-bg-color);
	cursor: pointer;
	text-align: left;
	transition: box-shadow 0.2s, transform 0.2s, border-color 0.2s;

	&:hover {
		border-color: color-mix(in srgb, var(--el-color-primary) 40%, var(--el-border-color-lighter));
		box-shadow: 0 6px 16px -8px color-mix(in srgb, var(--el-color-primary) 45%, transparent);
		transform: translateY(-1px);
	}

	&.is-danger .noti-stat-value {
		color: var(--el-color-danger);
	}

	&-icon {
		width: 40px;
		height: 40px;
		flex-shrink: 0;
		display: flex;
		align-items: center;
		justify-content: center;
		border-radius: 10px;

		&.is-danger {
			background: var(--el-color-danger-light-9);
			color: var(--el-color-danger);
		}

		&.is-primary {
			background: var(--el-color-primary-light-9);
			color: var(--el-color-primary);
		}

		&.is-navy {
			background: #0f172a;
			color: #e2e8f0;
		}
	}

	&-value {
		font-size: 20px;
		font-weight: 700;
		line-height: 1.15;
		color: var(--el-text-color-primary);
		font-variant-numeric: tabular-nums;
	}

	&-label {
		font-size: 12px;
		color: var(--el-text-color-secondary);
	}
}

// ─── Toolbar ────────────────────────────────────────────────────────────────
.noti-toolbar {
	display: flex;
	flex-wrap: wrap;
	align-items: center;
	gap: 8px;
	padding: 10px 12px;
	border: 1px solid var(--el-border-color-lighter);
	border-radius: 10px;
	background: var(--el-bg-color);

	&-severity {
		width: 150px;
	}

	&-input {
		width: 170px;

		&.is-search {
			width: 200px;
			margin-left: auto;
		}
	}

	&-range {
		max-width: 360px;
	}
}

// ─── Danh sách ──────────────────────────────────────────────────────────────
.noti-body {
	flex: 1;
	min-height: 200px;
	overflow: auto;
	border: 1px solid var(--el-border-color-lighter);
	border-radius: 10px;
	background: var(--el-bg-color);

	.noti-group {
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

		.noti-group-count {
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

// ─── Empty ──────────────────────────────────────────────────────────────────
.noti-empty {
	display: flex;
	flex-direction: column;
	align-items: center;
	justify-content: center;
	gap: 10px;
	padding: 60px 0;

	&-icon {
		width: 84px;
		height: 84px;
		display: flex;
		align-items: center;
		justify-content: center;
		border-radius: 50%;
		background: var(--el-color-primary-light-9);
		color: var(--el-color-primary);
	}

	&-text {
		margin: 0;
		color: var(--el-text-color-secondary);
	}
}

// ─── Pager ──────────────────────────────────────────────────────────────────
.noti-pager {
	display: flex;
	justify-content: flex-end;
	padding: 2px;
}

// Mobile
@media (max-width: 768px) {
	.noti-stats {
		grid-template-columns: 1fr;
	}

	.noti-toolbar-input.is-search {
		margin-left: 0;
	}
}
</style>
