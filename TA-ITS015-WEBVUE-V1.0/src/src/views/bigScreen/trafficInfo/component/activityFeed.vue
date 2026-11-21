<template>
	<div class="activity-feed">
		<!-- Lọc nhanh theo nguồn (giống ô tick Sự kiện/Sự cố bên bảng cạnh): mọi nguồn hiện CHUNG một danh sách theo
		     thời gian, phân biệt bằng icon đầu dòng; bỏ tick nguồn nào thì ẩn thẻ của nguồn đó. -->
		<div class="activity-feed__legend">
			<el-checkbox-group v-model="selectedSources" class="feed-source-filter">
				<el-checkbox v-for="source in activeSources" :key="source.key" :value="source.key">
					<span class="feed-legend">
						<span class="feed-source-icon"><el-icon><component :is="source.icon" /></el-icon></span>
						<span>{{ source.label }}</span>
					</span>
				</el-checkbox>
			</el-checkbox-group>
			<span v-if="items.length" class="feed-legend__count">{{ items.length }}</span>
			<el-tooltip :content="$t('lz.button.base.refresh')" placement="top">
				<el-button circle size="small" icon="ele-Refresh" class="feed-refresh" :loading="isLoading" @click="reload(true)" />
			</el-tooltip>
		</div>

		<div v-if="isLoading && !items.length" class="activity-feed__state">
			{{ $t('lz.label.base.loading') }}
		</div>
		<div v-else-if="!items.length" class="activity-feed__state">
			<el-icon class="activity-feed__state-icon"><ele-List /></el-icon>
			<span>{{ loadFailed ? $t('lz.label.tmsActivityFeed.loadFailed') : $t('lz.label.tmsActivityFeed.empty', { days: VEHICLE_FILTER_HISTORY_DAYS }) }}</span>
		</div>
		<el-scrollbar v-else class="activity-feed__list">
			<transition-group name="feed-item" tag="ul">
				<li v-for="item in items" :key="item.key" class="feed-item" :style="{ '--feed-type-color': typeColor(item) }">
					<div class="feed-item__row">
						<span class="feed-source-icon" :title="sourceOf(item).label"><el-icon><component :is="sourceOf(item).icon" /></el-icon></span>
						<span class="feed-item__title">{{ item.title }}</span>
						<span v-if="item.typeCode" class="feed-type-chip">{{ typeName(item) }}</span>
						<el-icon v-if="item.notify" class="feed-item__alert" :title="$t('lz.label.tmsActivityFeed.alerted')"><ele-BellFilled /></el-icon>
						<span class="feed-item__time" :title="timeTitle(item)">{{ timeText(item) }}</span>
					</div>
					<!-- Lộ trình A → B → C (camera + Km) thay cho vị trí; chuyến chưa có camera (dữ liệu cũ) thì không có dòng này. -->
					<div v-if="item.route.length" class="feed-item__route" :title="routeText(item)">{{ routeText(item) }}</div>
					<div v-if="metaText(item)" class="feed-item__meta" :title="metaText(item)">{{ metaText(item) }}</div>
				</li>
			</transition-group>
		</el-scrollbar>
	</div>
</template>

<script setup lang="ts" name="bigScreenActivityFeed">
import { computed, markRaw, onMounted, ref, type Component } from 'vue';
import { Local } from '/@/utils/storage';
import { useI18n } from 'vue-i18n';
import { useDateFormat } from '@vueuse/core';
import { Cpu, Van } from '@element-plus/icons-vue';
import { useBaseConfig } from '/@/stores/baseConfig';
import { useMapStore } from '/@/stores/its/mapStore';
import { BaseConfigTypeEnum } from '/@/types/enums';
import { useActivityFeed, VEHICLE_FILTER_HISTORY_DAYS, type ActivityFeedItem, type ActivityFeedSource } from '/@/stores/its/activityFeed';

/**
 * Author: KienLT
 * Description: Bảng hoạt động gần đây ở khung camera của bigscreen, hiện khi không có camera để xem.
 *              Mọi nguồn (lọc xe, sự kiện thiết bị FMS…) hiện CHUNG một danh sách theo thời gian, phân biệt bằng
 *              icon đầu dòng; dữ liệu do store activityFeed gom.
 * Created date: 22/10/2026
 */

const { t: $t } = useI18n();
const baseConfig = useBaseConfig();
const getConfigItemByCode = baseConfig.getConfigItemByCode;
const feed = useActivityFeed();
const mapStore = useMapStore();

interface FeedSourceDef {
	key: ActivityFeedSource;
	label: string;
	icon: Component;
	/** Loại cấu hình của mã typeCode (tên + màu nhãn); không có thì dòng không có nhãn loại. */
	typeConfig?: string;
	/** Nạp dữ liệu ban đầu; nguồn chỉ có bản tin trực tiếp thì bỏ trống. */
	load?: (force: boolean) => Promise<void>;
	/** Hiện trong chú giải: bật khi nguồn đã có dữ liệu đẩy vào store. */
	enabled: boolean;
}

// Thêm nguồn mới = thêm một mục ở đây + một hàm push trong store activityFeed. FMS bật khi có luồng đẩy vào.
const sources = computed<FeedSourceDef[]>(() => [
	{
		key: 'vehicleFilter',
		label: $t('lz.label.tmsActivityFeed.vehicleFilter'),
		icon: markRaw(Van),
		typeConfig: BaseConfigTypeEnum.TmsVehicleFilterType,
		load: (force) => feed.loadVehicleFilterHistory(force),
		enabled: true,
	},
	{
		key: 'device',
		label: $t('lz.label.tmsActivityFeed.device'),
		icon: markRaw(Cpu),
		enabled: false,
	},
]);

const activeSources = computed(() => sources.value.filter((s) => s.enabled || feed.bySource(s.key).length > 0));
const sourceOf = (item: ActivityFeedItem) => sources.value.find((s) => s.key === item.source) ?? sources.value[0];

// Nguồn đang ẩn (bỏ tick). Lưu nguồn BỊ ẨN thay vì nguồn được chọn: nguồn mới thêm sau (FMS) mặc định vẫn hiện.
// Nhớ theo máy người xem; đọc/ghi lỗi (chế độ riêng tư…) thì coi như không ẩn gì.
const HIDDEN_SOURCES_KEY = 'bigScreen.activityFeed.hiddenSources';
const readHidden = (): ActivityFeedSource[] => {
	try {
		const value = Local.get(HIDDEN_SOURCES_KEY);
		return Array.isArray(value) ? value : [];
	} catch {
		return [];
	}
};
const hiddenSources = ref<ActivityFeedSource[]>(readHidden());
const selectedSources = computed<ActivityFeedSource[]>({
	get: () => activeSources.value.map((s) => s.key).filter((key) => !hiddenSources.value.includes(key)),
	set: (keys) => {
		hiddenSources.value = activeSources.value.map((s) => s.key).filter((key) => !keys.includes(key));
		try {
			Local.set(HIDDEN_SOURCES_KEY, hiddenSources.value);
		} catch {
			/* không lưu được thì chỉ mất ghi nhớ giữa các lần mở */
		}
	},
});

const items = computed(() => feed.items.filter((item) => !hiddenSources.value.includes(item.source)));
const isLoading = computed(() => activeSources.value.some((s) => feed.loading[s.key]));
const loadFailed = computed(() => activeSources.value.some((s) => feed.loadFailed[s.key]));

const reload = (force = true) => {
	for (const source of activeSources.value) void source.load?.(force);
};

const isToday = (date: Date) => date.toDateString() === new Date().toDateString();
const formatShort = (date: Date) => useDateFormat(date, isToday(date) ? 'HH:mm:ss' : 'DD/MM HH:mm').value;
const formatFull = (date: Date) => useDateFormat(date, 'DD/MM/YYYY HH:mm:ss').value;

// Màu theo loại lấy từ cấu hình của nguồn (lọc xe: TagType success = xe ưu tiên, danger = xe cấm…); thiếu thì primary.
const TAG_TYPES = new Set(['primary', 'success', 'warning', 'danger', 'info']);
const typeColor = (item: ActivityFeedItem) => {
	const config = sourceOf(item).typeConfig;
	const tagType = config ? getConfigItemByCode(config, item.typeCode as string)?.tagType : undefined;
	return `var(--el-color-${TAG_TYPES.has(tagType) ? tagType : 'primary'})`;
};
const typeName = (item: ActivityFeedItem) => {
	const config = sourceOf(item).typeConfig;
	return config ? baseConfig.getConfigItemName(config, item.typeCode as string) : item.typeCode;
};

const kmText = (km?: number | null, met?: number | null) => `Km ${km ?? 0}+${String(met ?? 0).padStart(3, '0')}`;

/** Nhãn một camera trên lộ trình: "VDS-01 (Km10+200)". Bản tin trực tiếp mang sẵn tên/Km, thiếu thì tra danh mục thiết bị. */
const cameraLabel = (equipmentId: string): string => {
	const live = feed.cameras[equipmentId];
	const equipment = mapStore.equipmentList?.find((e: any) => String(e?.id) === equipmentId);
	const name = live?.name ?? equipment?.name ?? equipment?.code ?? equipmentId.slice(0, 8);
	const km = live?.km ?? equipment?.kmNumber;
	const met = live?.met ?? equipment?.metNumber;
	return km != null ? `${name} (${kmText(km, met)})` : name;
};

/** Lộ trình của chuyến: camera theo thời gian, nối bằng mũi tên. */
const routeText = (item: ActivityFeedItem) => item.route.map(cameraLabel).join(' → ');

/** Tuyến · hướng · số lượt. */
const metaText = (item: ActivityFeedItem) =>
	[
		getConfigItemByCode(BaseConfigTypeEnum.TmsZoneLocation, item.locationId as string)?.name,
		getConfigItemByCode(BaseConfigTypeEnum.TmsDirectionType, item.direction as string)?.name,
		item.hitCount > 1 ? $t('lz.label.tmsActivityFeed.hitCount', { count: item.hitCount }) : null,
	]
		.filter(Boolean)
		.join(' · ');

/** Giờ của thẻ: một lượt ⇒ một mốc; chuyến nhiều lượt ⇒ "đầu → cuối". */
const timeText = (item: ActivityFeedItem) =>
	item.startTime.getTime() === item.time.getTime()
		? formatShort(item.time)
		: `${formatShort(item.startTime)} → ${formatShort(item.time)}`;
const timeTitle = (item: ActivityFeedItem) =>
	item.startTime.getTime() === item.time.getTime()
		? formatFull(item.time)
		: `${formatFull(item.startTime)} → ${formatFull(item.time)}`;

onMounted(() => {
	reload(false);
	// Thiết bị dùng để tra km camera khi bản tin thiếu; bigscreen thường đã nạp sẵn, chưa có thì nạp một lần.
	if (!mapStore.equipmentList?.length) void mapStore.fetchEquipmentList().catch(() => undefined);
});
</script>

<style scoped lang="scss">
.activity-feed {
	flex: 1;
	min-height: 160px;
	display: flex;
	flex-direction: column;
	gap: 6px;
	margin-top: 4px;
	min-width: 0;
}

/* Chú giải nguồn ở trên danh sách (không phải tab: mọi nguồn hiện chung). */
.activity-feed__legend {
	display: flex;
	align-items: center;
	gap: 10px;
	font-size: 12px;
	color: var(--el-color-white);

	.feed-legend {
		display: inline-flex;
		align-items: center;
		gap: 5px;
	}

	/* Ô tick lọc nguồn: chữ trắng trên nền tối kể cả khi đã chọn (Element Plus mặc định tô màu primary),
	   cùng kiểu ô tick Sự kiện/Sự cố của bảng bên cạnh. */
	/* Dạng chip bật/tắt, đồng bộ bộ lọc Sự kiện/Sự cố/Đang diễn ra của bảng bên cạnh:
	   ẩn ô vuông checkbox; bật = nền tint primary + viền + icon sáng; tắt = viền mờ, icon xám. */
	.feed-source-filter {
		display: flex;
		flex-wrap: wrap;
		gap: 6px;

		:deep(.el-checkbox) {
			height: 26px;
			margin-right: 0;
			padding: 0 10px 0 3px;
			border-radius: 999px;
			border: 1px solid rgba(255, 255, 255, 0.16);
			background: rgba(255, 255, 255, 0.04);
			transition: background-color 0.2s ease, border-color 0.2s ease, box-shadow 0.2s ease;

			&:hover {
				border-color: color-mix(in srgb, var(--el-color-primary) 60%, transparent);
				background: rgba(255, 255, 255, 0.08);
			}

			&.is-checked {
				border-color: color-mix(in srgb, var(--el-color-primary) 70%, transparent);
				background: color-mix(in srgb, var(--el-color-primary) 18%, transparent);
				box-shadow: 0 0 12px -4px color-mix(in srgb, var(--el-color-primary) 70%, transparent);
			}
		}

		:deep(.el-checkbox__input) {
			position: absolute;
			opacity: 0;
			pointer-events: none;
		}

		:deep(.el-checkbox__label) {
			padding-left: 0;
			font-size: 12px;
			font-weight: 600;
			letter-spacing: 0.3px;
			color: rgba(232, 242, 255, 0.55) !important;
		}

		:deep(.el-checkbox.is-checked .el-checkbox__label) {
			color: #fff !important;
		}

		/* Icon nguồn trong chip: tắt thì xám, bật thì đầy màu primary */
		:deep(.el-checkbox .feed-source-icon) {
			background: rgba(255, 255, 255, 0.14);
			transition: background-color 0.2s ease;
		}

		:deep(.el-checkbox.is-checked .feed-source-icon) {
			background: var(--el-color-primary);
		}
	}

	.feed-legend__count {
		min-width: 18px;
		padding: 0 5px;
		font-size: 11px;
		line-height: 16px;
		text-align: center;
		border-radius: 999px;
		background: color-mix(in srgb, var(--el-color-primary) 45%, transparent);
	}

	/* Cùng kiểu nút làm mới của bảng sự kiện/sự cố bên cạnh (centerInfo.vue .refresh-btn): CHỈ ICON, không
	   khung/nền, icon xám trắng; hover icon sáng xanh và xoay. */
	.feed-refresh {
		margin-left: auto;
		width: 22px;
		height: 22px;
		padding: 0;
		border: none;
		background: transparent !important;
		box-shadow: none;
		color: #c9d6e8;
		transition: color 0.2s ease;

		:deep(.el-icon) {
			font-size: 15px;
		}

		&:hover,
		&:focus-visible {
			color: var(--el-color-primary-light-3);
		}

		&:hover :deep(.el-icon) {
			transform: rotate(90deg);
			transition: transform 0.3s ease;
		}
	}
}

/* Icon nguồn (lọc xe / thiết bị…): ô tròn nhỏ, dùng cả ở chú giải lẫn đầu mỗi dòng. */
.feed-source-icon {
	flex: none;
	display: inline-flex;
	align-items: center;
	justify-content: center;
	width: 20px;
	height: 20px;
	font-size: 12px;
	border-radius: 50%;
	color: var(--el-color-white);
	background: var(--el-color-primary);
}

.activity-feed__state {
	flex: 1;
	display: flex;
	flex-direction: column;
	align-items: center;
	justify-content: center;
	gap: 10px;
	padding: 0 12px;
	text-align: center;
	font-size: 12px;
	color: var(--el-text-color-secondary);
	border: 1px dashed color-mix(in srgb, var(--el-color-primary) 28%, transparent);
	border-radius: 8px;
	background: color-mix(in srgb, var(--el-color-primary) 3%, transparent);

	.activity-feed__state-icon {
		font-size: 32px;
		color: color-mix(in srgb, var(--el-color-primary) 55%, transparent);
	}
}

.activity-feed__list {
	flex: 1;
	min-height: 0;

	ul {
		margin: 0;
		padding: 0;
		list-style: none;
		display: flex;
		flex-direction: column;
		gap: 4px;
	}
}

/* Cùng bảng màu với bảng sự kiện/sự cố bên cạnh (centerInfo.vue — bảng đó cố định mã màu $grid-*): nền trắng,
   sọc xanh xám nhạt, chữ xanh đậm. Giữ đúng mã để hai bảng đồng nhất trên nền tối của bigscreen. */
$feed-white: var(--el-color-white);
$feed-stripe: #eef3fa;
$feed-navy: #1d3350;
$feed-muted: #5b6b82;
$feed-line: rgba(120, 140, 170, 0.35);
$feed-flash: #dbe7fb;

.feed-item {
	position: relative;
	padding: 6px 8px 6px 11px;
	border-radius: 6px;
	background: $feed-white;
	border: 1px solid $feed-line;

	/* Vạch trái theo màu loại danh sách (xe ưu tiên / xe cấm…), đặt qua biến --feed-type-color của từng dòng. */
	&::before {
		content: '';
		position: absolute;
		left: 0;
		top: 6px;
		bottom: 6px;
		width: 3px;
		border-radius: 2px;
		background: var(--feed-type-color, var(--el-color-primary));
	}

	/* Nhãn loại: nền đặc theo màu loại, chữ trắng — phân biệt rõ xe ưu tiên / xe cấm. */
	.feed-type-chip {
		padding: 0 6px;
		font-size: 11px;
		line-height: 18px;
		white-space: nowrap;
		border-radius: 4px;
		color: var(--el-color-white);
		background: var(--feed-type-color, var(--el-color-primary));
	}

	/* Lượt đã hiện thông báo (cờ cảnh báo bật, qua cửa chống dội). */
	.feed-item__alert {
		font-size: 13px;
		color: var(--el-color-warning);
	}

	/* Sọc chẵn/lẻ giống bảng sự kiện/sự cố bên cạnh. */
	&:nth-child(even) {
		background: $feed-stripe;
	}

	.feed-item__row {
		display: flex;
		align-items: center;
		gap: 6px;
		min-width: 0;
	}

	.feed-item__title {
		font-weight: 600;
		letter-spacing: 0.4px;
		color: $feed-navy;
		white-space: nowrap;
	}

	.feed-item__time {
		margin-left: auto;
		font-size: 11px;
		font-variant-numeric: tabular-nums;
		color: $feed-muted;
		white-space: nowrap;
	}

	/* Lộ trình A → B → C: dòng chính thứ hai của thẻ, cắt '…' khi dài (rê chuột xem đủ). */
	.feed-item__route {
		margin-top: 3px;
		font-size: 12px;
		font-weight: 500;
		color: $feed-navy;
		white-space: nowrap;
		overflow: hidden;
		text-overflow: ellipsis;
	}

	.feed-item__meta {
		margin-top: 2px;
		font-size: 11px;
		color: $feed-muted;
		white-space: nowrap;
		overflow: hidden;
		text-overflow: ellipsis;
	}
}

/* Dòng mới chèn lên đầu: trượt nhẹ và loé nền một lần. */
.feed-item-enter-active {
	transition: transform 0.25s ease, opacity 0.25s ease;
	animation: feed-item-flash 1.6s ease-out;
}

.feed-item-enter-from {
	opacity: 0;
	transform: translateY(-6px);
}

.feed-item-move {
	transition: transform 0.25s ease;
}

/* Chỉ khai mốc đầu: kết thúc về đúng nền của dòng (thường hay sọc). */
@keyframes feed-item-flash {
	0% {
		background: $feed-flash;
	}
}
</style>
