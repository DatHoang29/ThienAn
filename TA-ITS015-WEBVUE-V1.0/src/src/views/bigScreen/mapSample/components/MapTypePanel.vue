<template>
	<div class="mt-panel">
		<div class="mt-panel__head">
			<div class="mt-panel__title-row">
				<span class="mt-panel__title-icon">
					<el-icon><ele-MapLocation /></el-icon>
				</span>
				<span class="mt-panel__title">{{ $t('lz.button.tmsMapLocation.changeMap') }}</span>
				<span class="mt-panel__summary">{{ $t('lz.label.tmsMapLocation.mapCount', { count: items.length }) }}</span>
			</div>

			<el-input
				v-if="items.length > searchThreshold"
				v-model="keyword"
				size="small"
				clearable
				prefix-icon="ele-Search"
				class="mt-panel__search"
				:placeholder="$t('lz.label.tmsMapLocation.mapSearchPlaceholder')"
			/>
		</div>

		<div ref="listRef" class="mt-panel__body" role="radiogroup" :aria-label="$t('lz.button.tmsMapLocation.changeMap')">
			<button
				v-for="item in filteredItems"
				:key="item.value"
				type="button"
				role="radio"
				class="mt-item"
				:class="{ 'is-active': isSelected(item) }"
				:aria-checked="isSelected(item) ? 'true' : 'false'"
				:title="item.label"
				@click="emit('select', item.value)"
			>
				<span class="mt-item__icon" :class="isImage(item) ? 'is-image' : 'is-tile'">
					<el-icon>
						<ele-Picture v-if="isImage(item)" />
						<ele-MapLocation v-else />
					</el-icon>
				</span>
				<span class="mt-item__text">
					<span class="mt-item__name">{{ item.label }}</span>
					<span class="mt-item__meta">{{ isImage(item) ? $t('lz.label.tmsMapLocation.imageMap') : $t('lz.label.tmsMapLocation.tileMap') }}</span>
				</span>
				<span v-if="Number(item.isDefault) === 1" class="mt-item__tag">{{ $t('lz.label.tmsMapLocation.defaultMapTag') }}</span>
				<span class="mt-item__check">
					<el-icon v-if="isSelected(item)"><ele-CircleCheckFilled /></el-icon>
				</span>
			</button>

			<div v-if="filteredItems.length === 0" class="mt-empty">
				<el-icon class="mt-empty__icon"><ele-Search /></el-icon>
				<span>{{ $t('lz.label.base.noData') }}</span>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
/**
 * Panel chọn bản đồ (dạng danh sách thẻ, có tìm kiếm).
 * Chỉ hiển thị: cha truyền danh sách + id đang chọn, nhận event `select`.
 */
import { computed, nextTick, onMounted, ref } from 'vue';

export interface MapTypeOption {
	value: string | number;
	label: string;
	type?: string;
	isDefault?: number | boolean;
	[key: string]: any;
}

const props = withDefaults(
	defineProps<{
		items: MapTypeOption[];
		modelValue?: string | number | null;
		/** Số bản đồ tối thiểu để hiện ô tìm kiếm */
		searchThreshold?: number;
	}>(),
	{ modelValue: null, searchThreshold: 6 }
);

const emit = defineEmits<{
	select: [value: string | number];
}>();

const keyword = ref('');
const listRef = ref<HTMLElement | null>(null);

const normalize = (text: any) =>
	String(text ?? '')
		.toLowerCase()
		.normalize('NFD')
		.replace(/[̀-ͯ]/g, '')
		.replace(/đ/g, 'd');

const isImage = (item: MapTypeOption) => item.type === 'image';
const isSelected = (item: MapTypeOption) => props.modelValue != null && String(item.value) === String(props.modelValue);

const filteredItems = computed(() => {
	const key = normalize(keyword.value.trim());
	if (!key) return props.items;
	return props.items.filter((item) => normalize(item.label).includes(key));
});

/** Cuộn tới bản đồ đang chọn (gọi khi mở popover) */
const reveal = () => {
	// Xoá từ khoá cũ để bản đồ đang chọn luôn hiện trong danh sách
	keyword.value = '';
	nextTick(() => {
		const list = listRef.value;
		const active = list?.querySelector<HTMLElement>('.mt-item.is-active');
		if (!list || !active) return;
		list.scrollTop = active.offsetTop - list.clientHeight / 2 + active.offsetHeight / 2;
	});
};

// Lần mở đầu tiên panel mới được mount nên cũng cuộn ở đây
onMounted(reveal);

defineExpose({ reveal });
</script>

<style scoped lang="scss">
.mt-panel {
	--mt-accent: var(--el-color-primary);
	--mt-accent-soft: var(--el-color-primary-light-9);
	--mt-accent-border: var(--el-color-primary-light-5);
	--mt-text: var(--el-text-color-primary);
	--mt-text-2: var(--el-text-color-regular);
	--mt-muted: var(--el-text-color-secondary);
	--mt-line: var(--el-border-color-lighter);

	display: flex;
	flex-direction: column;
	max-height: min(500px, 68vh);
	background: var(--el-bg-color-overlay);
	color: var(--mt-text);
}

.mt-panel__head {
	display: flex;
	flex-direction: column;
	gap: 9px;
	padding: 12px 12px 9px;
	border-bottom: 1px solid var(--mt-line);
	background: linear-gradient(180deg, var(--el-fill-color-lighter) 0%, var(--el-bg-color-overlay) 100%);
}

.mt-panel__title-row {
	display: flex;
	align-items: center;
	gap: 7px;
}

.mt-panel__title-icon {
	display: inline-flex;
	align-items: center;
	justify-content: center;
	width: 28px;
	height: 28px;
	border-radius: 9px;
	background: var(--mt-accent-soft);
	color: var(--mt-accent);
	font-size: 15px;
	flex-shrink: 0;
}

.mt-panel__title {
	flex: 1;
	min-width: 0;
	font-size: 14px;
	font-weight: 700;
}

.mt-panel__summary {
	padding: 2px 8px;
	border-radius: 999px;
	background: var(--el-fill-color-light);
	color: var(--mt-text-2);
	font-size: 11px;
	font-weight: 600;
	white-space: nowrap;
}

.mt-panel__search {
	:deep(.el-input__wrapper) {
		min-height: 28px;
		border-radius: 8px;
		box-shadow: 0 0 0 1px var(--mt-line) inset;
	}

	:deep(.el-input__inner) {
		font-size: 12px;
	}

	:deep(.el-input__wrapper.is-focus) {
		box-shadow: 0 0 0 1px var(--mt-accent) inset;
	}
}

.mt-panel__body {
	position: relative;
	flex: 1;
	min-height: 0;
	overflow-y: auto;
	padding: 7px 8px;
	display: flex;
	flex-direction: column;
	gap: 3px;

	&::-webkit-scrollbar {
		width: 6px;
	}

	&::-webkit-scrollbar-thumb {
		background: rgba(148, 163, 184, 0.45);
		border-radius: 999px;
	}

	&::-webkit-scrollbar-track {
		background: transparent;
	}
}

.mt-item {
	display: flex;
	align-items: center;
	gap: 11px;
	width: 100%;
	min-height: 48px;
	padding: 7px 10px 7px 7px;
	border: 1px solid transparent;
	border-radius: 10px;
	background: transparent;
	color: var(--mt-text);
	text-align: left;
	cursor: pointer;
	flex-shrink: 0;
	transition: all 0.15s ease;

	&:hover {
		background: var(--el-fill-color-light);
	}

	&:focus-visible {
		outline: 2px solid var(--mt-accent-border);
		outline-offset: 1px;
	}

	&.is-active {
		border-color: var(--mt-accent-border);
		background: var(--mt-accent-soft);
	}
}

.mt-item__icon {
	display: inline-flex;
	align-items: center;
	justify-content: center;
	width: 34px;
	height: 34px;
	border-radius: 10px;
	font-size: 18px;
	flex-shrink: 0;

	&.is-tile {
		background: var(--el-color-primary-light-9);
		color: var(--el-color-primary);
	}

	&.is-image {
		background: var(--el-color-success-light-9);
		color: var(--el-color-success);
	}

	.mt-item.is-active & {
		background: var(--mt-accent);
		color: #fff;
	}
}

.mt-item__text {
	flex: 1;
	min-width: 0;
	display: flex;
	flex-direction: column;
	gap: 2px;
}

.mt-item__name {
	font-size: 13px;
	font-weight: 600;
	line-height: 1.3;
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;

	.mt-item.is-active & {
		font-weight: 700;
	}
}

.mt-item__meta {
	font-size: 11px;
	color: var(--mt-muted);
}

.mt-item__tag {
	padding: 1px 7px;
	border-radius: 999px;
	border: 1px solid var(--el-color-warning-light-7);
	background: var(--el-color-warning-light-9);
	color: var(--el-color-warning-dark-2);
	font-size: 11px;
	font-weight: 600;
	white-space: nowrap;
	flex-shrink: 0;
}

.mt-item__check {
	display: inline-flex;
	width: 20px;
	justify-content: center;
	color: var(--mt-accent);
	font-size: 18px;
	flex-shrink: 0;
}

.mt-empty {
	display: flex;
	flex-direction: column;
	align-items: center;
	gap: 5px;
	padding: 20px 0;
	color: var(--mt-muted);
	font-size: 12px;
}

.mt-empty__icon {
	font-size: 20px;
}
</style>

<style lang="scss">
/* Bỏ padding mặc định của popover để panel tràn viền (tránh khung trắng lồng nhau) */
.el-popover.el-popper.map-type-popover {
	padding: 0;
	border-radius: 12px;
	border: 1px solid var(--el-border-color-lighter);
	box-shadow: 0 12px 32px rgba(15, 23, 42, 0.16);
	overflow: hidden;
}
</style>
