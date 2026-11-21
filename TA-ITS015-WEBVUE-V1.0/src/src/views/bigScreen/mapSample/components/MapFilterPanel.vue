<template>
	<div class="mf-panel">
		<!-- Header: tiêu đề + tab + tìm kiếm + chọn/bỏ chọn tất cả -->
		<div class="mf-panel__head">
			<div class="mf-panel__title-row">
				<span class="mf-panel__title-icon">
					<el-icon><ele-Filter /></el-icon>
				</span>
				<span class="mf-panel__title">{{ $t('lz.button.tmsMapLocation.filterOnMap') }}</span>
			</div>

			<div class="mf-tabs" role="tablist">
				<button
					v-for="tab in tabs"
					:key="tab.key"
					type="button"
					role="tab"
					class="mf-tab"
					:class="[`mf-tab--${tab.key}`, { 'is-active': activeTab === tab.key }]"
					:aria-selected="activeTab === tab.key ? 'true' : 'false'"
					@click="activeTab = tab.key"
				>
					<el-icon class="mf-tab__icon"><component :is="tab.icon" /></el-icon>
					<span class="mf-tab__label">{{ tab.label }}</span>
					<span class="mf-tab__count" :class="stateClass(tab.checked, tab.total)">{{ tab.checked }}/{{ tab.total }}</span>
				</button>
			</div>

			<div class="mf-panel__tools">
				<el-input
					v-model="keyword"
					size="small"
					clearable
					prefix-icon="ele-Search"
					class="mf-panel__search"
					:placeholder="$t('lz.label.tmsMapLocation.filterSearchPlaceholder')"
				/>
				<!-- Chọn / bỏ chọn tất cả của tab đang mở (checkbox 3 trạng thái) -->
				<el-tooltip :content="allState === 'full' ? $t('lz.button.base.unselectAll') : $t('lz.button.base.selectAll')" placement="top">
					<button
						type="button"
						role="checkbox"
						class="mf-all"
						:class="`is-${allState}`"
						:aria-checked="allState === 'full' ? 'true' : allState === 'partial' ? 'mixed' : 'false'"
						:disabled="activeTabMeta.total === 0"
						@click="toggleAll"
					>
						<span class="mf-all__box">
							<el-icon v-if="allState === 'full'"><ele-Check /></el-icon>
							<span v-else-if="allState === 'partial'" class="mf-all__dash"></span>
						</span>
						<span>{{ $t('lz.label.base.all') }}</span>
					</button>
				</el-tooltip>
			</div>
		</div>

		<!-- Body: danh sách nhóm của tab đang chọn (chỉ phần này cuộn) -->
		<div class="mf-panel__body">
			<section v-for="view in visibleGroups" :key="view.group.value" class="mf-group" :class="{ 'is-open': isOpen(view.group), 'is-flat': isFlat }">
				<!-- Tab chỉ có 1 nhóm (vd: Sự kiện) thì bỏ header nhóm, dùng nút chọn tất cả ở trên -->
				<div v-if="!isFlat" class="mf-group__head" @click="toggleCollapse(view.group)">
					<el-checkbox
						:model-value="isGroupChecked(view.group)"
						:indeterminate="isGroupIndeterminate(view.group)"
						class="mf-group__check"
						@change="(value: any) => emit('group-change', view.group, !!value)"
						@click.stop
					>
						<span class="mf-group__title">
							<span class="mf-group__icon" :class="`is-${themeOf(view.group).key}`">
								<el-icon><component :is="themeOf(view.group).icon" /></el-icon>
							</span>
							<span>{{ view.group.label }}</span>
						</span>
					</el-checkbox>

					<template v-if="!isSingle(view.group)">
						<span class="mf-group__count" :class="stateClass(countOf(view.group), view.group.children.length)">
							{{ countOf(view.group) }}/{{ view.group.children.length }}
						</span>
						<el-icon class="mf-group__arrow"><ele-ArrowDown /></el-icon>
					</template>
				</div>

				<div v-if="!isSingle(view.group) && isOpen(view.group)" class="mf-group__items">
					<button
						v-for="item in view.items"
						:key="item.value"
						type="button"
						role="checkbox"
						class="mf-chip"
						:class="{ 'is-checked': item.checked }"
						:aria-checked="item.checked ? 'true' : 'false'"
						:title="item.label"
						@click="emit('item-change', view.group, item, !item.checked)"
					>
						<span class="mf-chip__media" :class="`is-${themeOf(view.group).key}`">
							<img v-if="iconSrc(item)" :src="iconSrc(item)" alt="" loading="lazy" @error="markFailed(iconSrc(item))" />
							<el-icon v-else><component :is="themeOf(view.group).icon" /></el-icon>
						</span>
						<span class="mf-chip__label">{{ item.label }}</span>
						<span class="mf-chip__tick">
							<el-icon><ele-Check /></el-icon>
						</span>
					</button>
				</div>
			</section>

			<div v-if="visibleGroups.length === 0" class="mf-empty">
				<span class="mf-empty__icon" :class="`is-${activeTab}`">
					<el-icon>
						<ele-Search v-if="keyword.trim()" />
						<component :is="activeTabMeta.icon" v-else />
					</el-icon>
				</span>
				<span>{{ keyword.trim() || activeTab !== 'event' ? $t('lz.label.base.noData') : $t('lz.label.tmsMapLocation.filterNoEvent') }}</span>
			</div>
		</div>

		<!-- Footer: tuỳ chọn hiển thị tên (không phải bộ lọc dữ liệu) -->
		<div v-if="nameToggle" class="mf-panel__foot">
			<span class="mf-foot__icon">
				<el-icon><ele-PriceTag /></el-icon>
			</span>
			<div class="mf-foot__text">
				<span class="mf-foot__title">{{ $t('lz.label.base.nameDesc') }}</span>
				<span class="mf-foot__hint">{{ $t('lz.label.tmsMapLocation.showIconNameHint') }}</span>
			</div>
			<el-switch size="small" :model-value="!!showIconName" @change="(value: any) => emit('update:showIconName', !!value)" />
		</div>
	</div>
</template>

<script setup lang="ts">
/**
 * Panel lọc dữ liệu trên bản đồ (dùng chung cho mapSample và trafficInfo).
 * Chia 2 tab: Đối tượng (thiết bị, đơn vị, khu vực) và Sự kiện.
 * Chỉ hiển thị: nhận danh sách nhóm qua props, báo thay đổi qua events.
 * Component cha tự cập nhật checked/indeterminate và phát filter.
 */
import { computed, reactive, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { useMapStore } from '/@/stores/its/mapStore';
import { useItsMediaServerStore } from '/@/stores/its/itsMediaServer';

export interface MapFilterItem {
	label: string;
	value: string;
	checked: boolean;
	/** Đường dẫn icon trên file server (tương đối hoặc tuyệt đối) */
	icon?: string;
	/** Mã loại đơn vị (work_unit_type) để tra icon khi chưa có `icon` */
	unitType?: string;
}

export interface MapFilterGroup {
	label: string;
	value: string;
	checked: boolean;
	indeterminate: boolean;
	children: MapFilterItem[];
}

type TabKey = 'object' | 'event';

const props = withDefaults(
	defineProps<{
		groups: MapFilterGroup[];
		/** Hiện dòng bật/tắt "Tên mô tả" ở cuối panel */
		nameToggle?: boolean;
		showIconName?: boolean | null;
	}>(),
	{ nameToggle: false, showIconName: false }
);

const emit = defineEmits<{
	'group-change': [group: MapFilterGroup, checked: boolean];
	'item-change': [group: MapFilterGroup, item: MapFilterItem, checked: boolean];
	/** Danh sách mã nhóm của tab hiện tại */
	'check-all': [groupValues: string[]];
	'uncheck-all': [groupValues: string[]];
	'update:showIconName': [value: boolean];
}>();

const { t } = useI18n();
const mapStore = useMapStore();
const media = useItsMediaServerStore();

const keyword = ref('');
const activeTab = ref<TabKey>('object');
const collapsed = reactive<Record<string, boolean>>({});
const failedIcons = reactive<Record<string, boolean>>({});

/* Icon + màu theo nhóm (dùng cho header nhóm và icon dự phòng của mục con) */
const GROUP_THEMES: Record<string, { key: string; icon: string }> = {
	device: { key: 'device', icon: 'ele-Monitor' },
	unit: { key: 'unit', icon: 'ele-OfficeBuilding' },
	zone: { key: 'zone', icon: 'ele-LocationInformation' },
	event: { key: 'event', icon: 'ele-WarningFilled' },
};
const themeOf = (group: MapFilterGroup) => GROUP_THEMES[group.value] || { key: 'other', icon: 'ele-Collection' };

const tabOf = (group: MapFilterGroup): TabKey => (group.value === 'event' ? 'event' : 'object');

const normalize = (text: any) =>
	String(text ?? '')
		.toLowerCase()
		.normalize('NFD')
		.replace(/[̀-ͯ]/g, '')
		.replace(/đ/g, 'd');

const countOf = (group: MapFilterGroup) => (group.children || []).filter((item) => item.checked).length;
const totalOf = (group: MapFilterGroup) => group.children?.length || 0;

// Trạng thái checkbox nhóm tính từ con, không phụ thuộc cờ có sẵn
const isGroupChecked = (group: MapFilterGroup) => totalOf(group) > 0 && countOf(group) === totalOf(group);
const isGroupIndeterminate = (group: MapFilterGroup) => {
	const count = countOf(group);
	return count > 0 && count < totalOf(group);
};

// Nhóm chỉ có 1 con trùng chính nó (vd: zone) thì không cần hiện danh sách con
const isSingle = (group: MapFilterGroup) => group.children?.length === 1 && String(group.children[0].value) === String(group.value);

const stateClass = (checked: number, total: number) => {
	if (checked === 0) return 'is-none';
	return checked === total ? 'is-full' : 'is-partial';
};

const groupsOfTab = (key: TabKey) => props.groups.filter((group) => tabOf(group) === key);

const tabs = computed(() =>
	(
		[
			{ key: 'object', icon: 'ele-Location', label: t('lz.label.tmsMapLocation.filterTabObject') },
			{ key: 'event', icon: 'ele-WarningFilled', label: t('lz.label.tmsMapLocation.event') },
		] as { key: TabKey; icon: string; label: string }[]
	).map((tab) => {
		const groups = groupsOfTab(tab.key);
		return {
			...tab,
			checked: groups.reduce((sum, group) => sum + countOf(group), 0),
			total: groups.reduce((sum, group) => sum + totalOf(group), 0),
		};
	})
);

const activeTabMeta = computed(() => tabs.value.find((tab) => tab.key === activeTab.value) || tabs.value[0]);
const tabGroups = computed(() => groupsOfTab(activeTab.value));
const tabGroupValues = computed(() => tabGroups.value.map((group) => group.value));
const allState = computed<'none' | 'partial' | 'full'>(() => {
	const { checked, total } = activeTabMeta.value;
	if (total === 0 || checked === 0) return 'none';
	return checked === total ? 'full' : 'partial';
});

// Đang chọn hết → bỏ chọn hết; còn lại (chưa chọn / chọn một phần) → chọn hết
const toggleAll = () => {
	if (allState.value === 'full') emit('uncheck-all', tabGroupValues.value);
	else emit('check-all', tabGroupValues.value);
};

// Tab chỉ có đúng 1 nhóm nhiều mục → hiển thị phẳng, không cần header nhóm
const isFlat = computed(() => tabGroups.value.length === 1 && !isSingle(tabGroups.value[0]));

// Tìm kiếm chỉ lọc phần hiển thị; chọn nhóm vẫn áp dụng cho toàn bộ con
const visibleGroups = computed(() => {
	const key = normalize(keyword.value.trim());
	return tabGroups.value
		.filter((group) => totalOf(group) > 0)
		.map((group) => {
			const children = group.children || [];
			if (!key || normalize(group.label).includes(key)) return { group, items: children };
			return { group, items: children.filter((item) => normalize(item.label).includes(key)) };
		})
		.filter((view) => !key || view.items.length > 0);
});

const isOpen = (group: MapFilterGroup) => (isFlat.value || keyword.value.trim() ? true : !collapsed[group.value]);

const toggleCollapse = (group: MapFilterGroup) => {
	if (isSingle(group) || keyword.value.trim()) return;
	collapsed[group.value] = !collapsed[group.value];
};

/** Dựng URL icon từ file server; trả '' để dùng icon dự phòng của nhóm */
const iconSrc = (item: MapFilterItem) => {
	let path = item.icon || '';
	if (!path && item.unitType) {
		path = mapStore.workUnitTypeList?.find((type: any) => String(type.code) === String(item.unitType))?.value || '';
	}
	if (!path) return '';
	const src = media.fileUrl(path, { fallback: '' });
	return src && !failedIcons[src] ? src : '';
};

const markFailed = (src: string) => {
	if (src) failedIcons[src] = true;
};
</script>

<style scoped lang="scss">
.mf-panel {
	--mf-accent: var(--el-color-primary);
	--mf-accent-soft: var(--el-color-primary-light-9);
	--mf-accent-border: var(--el-color-primary-light-7);
	--mf-text: var(--el-text-color-primary);
	--mf-text-2: var(--el-text-color-regular);
	--mf-muted: var(--el-text-color-secondary);
	--mf-line: var(--el-border-color-lighter);

	display: flex;
	flex-direction: column;
	height: min(540px, 75vh);
	background: var(--el-bg-color-overlay);
	color: var(--mf-text);
}

/* Màu theo nhóm */
.is-device {
	--g-color: var(--el-color-primary);
	--g-soft: var(--el-color-primary-light-9);
}
.is-unit {
	--g-color: var(--el-color-success);
	--g-soft: var(--el-color-success-light-9);
}
.is-zone {
	--g-color: var(--el-color-info);
	--g-soft: var(--el-color-info-light-9);
}
.is-event {
	--g-color: var(--el-color-warning);
	--g-soft: var(--el-color-warning-light-9);
}
.is-other,
.is-object {
	--g-color: var(--el-text-color-regular);
	--g-soft: var(--el-fill-color-light);
}

/* ---------- Header ---------- */
.mf-panel__head {
	display: flex;
	flex-direction: column;
	gap: 10px;
	padding: 12px 12px 10px;
	border-bottom: 1px solid var(--mf-line);
	background: linear-gradient(180deg, var(--el-fill-color-lighter) 0%, var(--el-bg-color-overlay) 100%);
}

.mf-panel__title-row {
	display: flex;
	align-items: center;
	gap: 7px;
}

.mf-panel__title-icon {
	display: inline-flex;
	align-items: center;
	justify-content: center;
	width: 28px;
	height: 28px;
	border-radius: 10px;
	background: linear-gradient(135deg, var(--el-color-primary-light-3) 0%, var(--el-color-primary) 100%);
	color: #fff;
	font-size: 15px;
	box-shadow: 0 4px 10px var(--el-color-primary-light-7);
	flex-shrink: 0;
}

.mf-panel__title {
	flex: 1;
	min-width: 0;
	font-size: 14px;
	font-weight: 700;
}

/* Tabs dạng segmented */
.mf-tabs {
	display: grid;
	grid-template-columns: 1fr 1fr;
	gap: 4px;
	padding: 4px;
	border-radius: 10px;
	background: var(--el-fill-color-light);
}

.mf-tab {
	display: flex;
	align-items: center;
	justify-content: center;
	gap: 5px;
	height: 33px;
	padding: 0 7px;
	border: none;
	border-radius: 8px;
	background: transparent;
	color: var(--mf-text-2);
	font-size: 12px;
	font-weight: 600;
	cursor: pointer;
	transition: all 0.18s ease;

	&:hover:not(.is-active) {
		color: var(--mf-text);
	}

	&:focus-visible {
		outline: 2px solid var(--mf-accent-border);
	}

	&.is-active {
		background: var(--el-bg-color-overlay);
		color: var(--mf-text);
		box-shadow: 0 2px 6px rgba(15, 23, 42, 0.1);
	}
}

.mf-tab__icon {
	font-size: 15px;

	.mf-tab--object.is-active & {
		color: var(--mf-accent);
	}

	.mf-tab--event.is-active & {
		color: var(--el-color-warning);
	}
}

.mf-tab__label {
	white-space: nowrap;
}

.mf-tab__count,
.mf-group__count {
	min-width: 35px;
	padding: 2px 7px;
	border-radius: 999px;
	font-size: 12px;
	font-weight: 700;
	text-align: center;
	background: var(--el-fill-color);
	color: var(--mf-text-2);

	&.is-full {
		background: var(--mf-accent-soft);
		color: var(--mf-accent);
	}

	&.is-partial {
		background: var(--el-color-warning-light-9);
		color: var(--el-color-warning-dark-2);
	}
}

.mf-panel__tools {
	display: flex;
	align-items: center;
	gap: 7px;
}

.mf-panel__search {
	flex: 1;
	min-width: 0;
	--el-input-height: 28px;
	--el-input-inner-height: 26px;
	font-size: 12px;

	:deep(.el-input__wrapper) {
		border-radius: 8px;
		box-shadow: 0 0 0 1px var(--mf-line) inset;
	}

	:deep(.el-input__wrapper.is-focus) {
		box-shadow: 0 0 0 1px var(--mf-accent) inset;
	}
}

.mf-all {
	display: inline-flex;
	align-items: center;
	gap: 5px;
	height: 28px;
	padding: 0 10px 0 9px;
	border: 1px solid var(--mf-line);
	border-radius: 8px;
	background: var(--el-bg-color-overlay);
	color: var(--mf-text-2);
	font-size: 12px;
	font-weight: 600;
	white-space: nowrap;
	cursor: pointer;
	flex-shrink: 0;
	transition: all 0.15s ease;

	&:hover:not(:disabled) {
		border-color: var(--mf-accent-border);
		color: var(--mf-text);
	}

	&:focus-visible {
		outline: 2px solid var(--mf-accent-border);
		outline-offset: 1px;
	}

	&.is-full,
	&.is-partial {
		border-color: var(--mf-accent-border);
		background: var(--mf-accent-soft);
		color: var(--mf-text);
	}

	&:disabled {
		cursor: not-allowed;
		opacity: 0.4;
	}
}

.mf-all__box {
	display: inline-flex;
	align-items: center;
	justify-content: center;
	width: 16px;
	height: 16px;
	border: 1px solid var(--el-border-color);
	border-radius: 4px;
	background: var(--el-bg-color-overlay);
	color: #fff;
	font-size: 11px;
	transition: all 0.15s ease;

	.mf-all.is-full &,
	.mf-all.is-partial & {
		border-color: var(--mf-accent);
		background: var(--mf-accent);
	}
}

.mf-all__dash {
	width: 8px;
	height: 2px;
	border-radius: 1px;
	background: var(--el-bg-color-overlay);
}

/* ---------- Body ---------- */
.mf-panel__body {
	flex: 1;
	min-height: 0;
	overflow-y: auto;
	padding: 9px 10px 10px;

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

.mf-group {
	border-radius: 12px;
	transition: background-color 0.15s ease;

	& + & {
		margin-top: 6px;
	}

	&.is-open:not(.is-flat) {
		background: var(--el-fill-color-lighter);
	}
}

.mf-group__head {
	display: flex;
	align-items: center;
	gap: 7px;
	min-height: 39px;
	padding: 0 9px;
	border-radius: 12px;
	cursor: pointer;
	user-select: none;

	&:hover {
		background: var(--el-fill-color-light);
	}
}

.mf-group__check {
	flex: 1;
	min-width: 0;
	height: auto;
	margin-right: 0;

	:deep(.el-checkbox__label) {
		font-size: 12px;
		font-weight: 700;
		color: var(--mf-text);
		line-height: 1.3;
		white-space: normal;
	}

	:deep(.el-checkbox__inner) {
		width: 16px;
		height: 16px;
		border-radius: 4px;
	}

	:deep(.el-checkbox__input.is-checked .el-checkbox__inner),
	:deep(.el-checkbox__input.is-indeterminate .el-checkbox__inner) {
		background-color: var(--mf-accent);
		border-color: var(--mf-accent);
	}

	:deep(.el-checkbox__input.is-checked + .el-checkbox__label) {
		color: var(--mf-text);
	}
}

.mf-group__title {
	display: inline-flex;
	align-items: center;
	gap: 7px;
}

.mf-group__icon {
	display: inline-flex;
	align-items: center;
	justify-content: center;
	width: 25px;
	height: 25px;
	border-radius: 8px;
	background: var(--g-soft);
	color: var(--g-color);
	font-size: 14px;
	flex-shrink: 0;
}

.mf-group__arrow {
	font-size: 12px;
	color: var(--mf-muted);
	transform: rotate(-90deg);
	transition: transform 0.2s ease;

	.mf-group.is-open & {
		transform: rotate(0deg);
	}
}

.mf-group__items {
	display: grid;
	grid-template-columns: repeat(2, minmax(0, 1fr));
	gap: 7px;
	padding: 4px 9px 10px;

	.mf-group.is-flat & {
		padding: 4px 0;
	}
}

/* Chip mục con: icon thật + tên + dấu tích */
.mf-chip {
	position: relative;
	display: flex;
	align-items: center;
	gap: 9px;
	min-height: 42px;
	padding: 5px 26px 5px 5px;
	border: 1px solid var(--mf-line);
	border-radius: 10px;
	background: var(--el-bg-color-overlay);
	color: var(--mf-muted);
	font-size: 12px;
	font-weight: 500;
	text-align: left;
	cursor: pointer;
	transition: all 0.16s ease;

	&:hover {
		border-color: var(--el-border-color);
		box-shadow: 0 2px 8px rgba(15, 23, 42, 0.06);
		color: var(--mf-text-2);
	}

	&:focus-visible {
		outline: 2px solid var(--mf-accent-border);
		outline-offset: 1px;
	}

	&.is-checked {
		border-color: var(--mf-accent-border);
		background: linear-gradient(180deg, var(--el-bg-color-overlay) 0%, var(--el-color-primary-light-9) 100%);
		color: var(--mf-text);
	}
}

.mf-chip__media {
	display: inline-flex;
	align-items: center;
	justify-content: center;
	width: 30px;
	height: 30px;
	border-radius: 8px;
	background: var(--g-soft);
	color: var(--g-color);
	font-size: 15px;
	flex-shrink: 0;
	filter: grayscale(1);
	opacity: 0.55;
	transition: filter 0.16s ease, opacity 0.16s ease;

	img {
		width: 24px;
		height: 24px;
		object-fit: contain;
	}

	.mf-chip.is-checked &,
	.mf-chip:hover & {
		filter: none;
		opacity: 1;
	}
}

.mf-chip__label {
	min-width: 0;
	line-height: 1.25;
	overflow: hidden;
	display: -webkit-box;
	-webkit-line-clamp: 2;
	-webkit-box-orient: vertical;
	word-break: break-word;
}

.mf-chip__tick {
	position: absolute;
	top: 50%;
	right: 8px;
	display: inline-flex;
	align-items: center;
	justify-content: center;
	width: 16px;
	height: 16px;
	margin-top: -8px;
	border: 1px solid var(--el-border-color);
	border-radius: 50%;
	background: var(--el-bg-color-overlay);
	color: transparent;
	font-size: 10px;
	transition: all 0.16s ease;

	.mf-chip.is-checked & {
		border-color: var(--mf-accent);
		background: var(--mf-accent);
		color: #fff;
	}
}

.mf-empty {
	display: flex;
	flex-direction: column;
	align-items: center;
	gap: 7px;
	padding: 34px 0;
	color: var(--mf-muted);
	font-size: 12px;
}

.mf-empty__icon {
	display: inline-flex;
	align-items: center;
	justify-content: center;
	width: 39px;
	height: 39px;
	border-radius: 50%;
	background: var(--g-soft);
	color: var(--g-color);
	font-size: 20px;
}

/* ---------- Footer ---------- */
.mf-panel__foot {
	display: flex;
	align-items: center;
	gap: 9px;
	padding: 10px 12px;
	border-top: 1px solid var(--mf-line);
	background: var(--el-fill-color-lighter);

	:deep(.el-switch.is-checked .el-switch__core) {
		background-color: var(--mf-accent);
		border-color: var(--mf-accent);
	}
}

.mf-foot__icon {
	display: inline-flex;
	align-items: center;
	justify-content: center;
	width: 28px;
	height: 28px;
	border-radius: 8px;
	background: var(--el-fill-color-light);
	color: var(--mf-text-2);
	font-size: 15px;
	flex-shrink: 0;
}

.mf-foot__text {
	flex: 1;
	min-width: 0;
	display: flex;
	flex-direction: column;
	gap: 1px;
}

.mf-foot__title {
	font-size: 12px;
	font-weight: 700;
}

.mf-foot__hint {
	font-size: 11px;
	color: var(--mf-muted);
}
</style>

<style lang="scss">
/* Bỏ padding mặc định của popover để panel tràn viền (tránh khung trắng lồng nhau) */
.el-popover.el-popper.map-filter-popover {
	padding: 0;
	border-radius: 14px;
	border: 1px solid var(--el-border-color-lighter);
	box-shadow: 0 12px 32px rgba(15, 23, 42, 0.16);
	overflow: hidden;
}
</style>
