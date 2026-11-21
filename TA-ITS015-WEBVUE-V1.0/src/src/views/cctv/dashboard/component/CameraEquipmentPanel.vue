<template>
	<div class="camera-list-panel">
		<div class="camera-list-header">
			<div class="camera-list-title-row">
				<label class="field-label">{{ title }}</label>
				<el-tooltip :content="t('lz.button.cctvDashboard.refreshCameraList')" placement="top">
					<el-button circle text size="small" :loading="refreshing" @click="$emit('refresh')">
						<el-icon><ele-Refresh /></el-icon>
					</el-button>
				</el-tooltip>
			</div>
			<span class="camera-list-hint" :class="{ 'is-ready': draggingOverGrid }">
				{{ draggingOverGrid ? activeHint : hint }}
			</span>
		</div>

		<div class="camera-list-tools">
			<el-input :model-value="query" size="small" :placeholder="searchPlaceholder" clearable
				@update:model-value="onQueryChange">
				<template #prefix>
					<el-icon><ele-Search /></el-icon>
				</template>
			</el-input>
			<el-popover v-if="equipmentTypes.length > 0" placement="bottom-end" :width="200" trigger="click">
				<template #reference>
					<el-badge :value="selectedType ? '1' : ''" :hidden="!selectedType" type="primary">
						<el-button circle text size="small" class="filter-type-btn">
							<el-icon><ele-Filter /></el-icon>
						</el-button>
					</el-badge>
				</template>
				<div class="camera-type-filter">
					<div class="camera-type-filter-header">
						<span>{{ t('lz.label.cctvDashboard.filterByType') }}</span>
						<el-button v-if="selectedType" link type="primary" size="small" @click="onTypeChange('')">
							{{ t('lz.button.cctvDashboard.clearFilter') }}
						</el-button>
					</div>
					<div class="camera-type-filter-list">
						<div v-for="type in equipmentTypes" :key="type" class="camera-type-filter-item"
							:class="{ 'is-active': selectedType === type }"
							@click="onTypeChange(selectedType === type ? '' : type)">
							{{ type }}
						</div>
					</div>
				</div>
			</el-popover>
		</div>

		<div v-if="loading" class="camera-list-empty">{{ loadingText }}</div>
		<div v-else-if="!equipments.length" class="camera-list-empty">{{ emptyText }}</div>
		<div v-else class="camera-list-items">
			<el-tooltip v-for="equipment in equipments" :key="getEquipmentId(equipment)"
				:content="isEquipmentAdded(getEquipmentId(equipment)) ? addedTooltip : availableTooltip"
				placement="right" :show-after="250">
				<button type="button" class="camera-list-item"
					:class="{ 'is-added': isEquipmentAdded(getEquipmentId(equipment)) }"
					:disabled="addingCameraId === getEquipmentId(equipment)"
					@mousedown.left.prevent="$emit('drag-start', getEquipmentId(equipment), $event)"
					@click="$emit('add', getEquipmentId(equipment))">
					<span class="camera-list-item-icon">
						<el-icon><ele-VideoCamera /></el-icon>
					</span>
					<div class="camera-list-item-info-text">
						<span v-marquee-overflow class="camera-list-item-name">
							<span class="camera-list-item-name-text">{{ getEquipmentName(equipment) }}</span>
						</span>
						<span class="camera-list-item-ip">{{ equipment.ip }}</span>
					</div>

					<span class="camera-list-item-status">
						<el-tag :type="getStatusTagType(equipment.status)" size="small">
							{{ getStatusText(equipment.status) }}
						</el-tag>
					</span>
				</button>
			</el-tooltip>
		</div>
	</div>
</template>

<script setup lang="ts">
import { computed, type Directive } from 'vue';
import type { DashboardCameraInfo } from '/@/views/cctv/dashboard/types';
import { useI18n } from 'vue-i18n';

const { t } = useI18n();

const props = withDefaults(
	defineProps<{
		equipments: DashboardCameraInfo[];
		query: string;
		loading?: boolean;
		refreshing?: boolean;
		addingCameraId?: string;
		draggingOverGrid?: boolean;
		isAdded?: (equipmentId: string) => boolean;
		title?: string;
		hint?: string;
		activeHint?: string;
		searchPlaceholder?: string;
		loadingText?: string;
		emptyText?: string;
		addedTooltip?: string;
		availableTooltip?: string;
		selectedType?: string;
		equipmentTypes?: string[];
	}>(),
	{
		loading: false,
		refreshing: false,
		addingCameraId: '',
		draggingOverGrid: false,
		selectedType: '',
		equipmentTypes: () => [],
	}
);

const title = computed(() => props.title || t('lz.label.cctvDashboard.cameraList'));
const hint = computed(() => props.hint || t('lz.label.cctvDashboard.dragCameraHint'));
const activeHint = computed(() => props.activeHint || t('lz.label.cctvDashboard.dropCameraHint'));
const searchPlaceholder = computed(() => props.searchPlaceholder || t('lz.label.cctvDashboard.searchCamera'));
const loadingText = computed(() => props.loadingText || t('lz.label.cctvDashboard.loadingCameras'));
const emptyText = computed(() => props.emptyText || t('lz.label.cctvDashboard.noMatchingCamera'));
const addedTooltip = computed(() => props.addedTooltip || t('lz.label.cctvDashboard.cameraAdded'));
const availableTooltip = computed(() => props.availableTooltip || t('lz.label.cctvDashboard.cameraAvailable'));

const getEquipmentId = (equipment: DashboardCameraInfo | Record<string, unknown> | null | undefined) => {
	if (!equipment) return '';
	const value = (equipment as any).id ?? (equipment as any).ID ?? (equipment as any).Id;
	try {
		if (typeof value === 'string') return value;
		if (typeof value === 'number' || typeof value === 'boolean') return String(value);
		return '';
	} catch {
		return '';
	}
};

const getEquipmentName = (equipment: DashboardCameraInfo) => {
	const name = typeof equipment.name === 'string' ? equipment.name.trim() : '';
	return name || t('lz.label.cctvDashboard.unnamedCamera');
};

const isEquipmentAdded = (equipmentId: string) => props.isAdded?.(equipmentId) ?? false;

const getStatusTagType = (status: DashboardCameraInfo['status']) => (status === 'online' ? 'success' : status === 'warning' ? 'warning' : 'danger');

const getStatusText = (status: DashboardCameraInfo['status']) => {
	if (status === 'online') return t('lz.label.cctvDashboard.online');
	if (status === 'warning') return t('lz.label.base.warning');
	return t('lz.label.cctvDashboard.offline');
};

const emit = defineEmits<{
	(e: 'update:query', value: string): void;
	(e: 'update:selectedType', value: string): void;
	(e: 'add', equipmentId: string): void;
	(e: 'drag-start', equipmentId: string, event: MouseEvent): void;
	(e: 'refresh'): void;
}>();

const onQueryChange = (value: string | number) => {
	emit('update:query', typeof value === 'string' ? value : String(value || ''));
};

const onTypeChange = (value: string | number | null) => {
	emit('update:selectedType', typeof value === 'string' ? value : String(value || ''));
};

type MarqueeElement = HTMLElement & { __marqueeObserver?: ResizeObserver };

const updateMarqueeOverflow = (el: MarqueeElement) => {
	const textEl = el.querySelector('.camera-list-item-name-text') as HTMLElement | null;
	const contentWidth = textEl?.scrollWidth ?? el.scrollWidth;
	const containerWidth = el.clientWidth;
	const overflowDistance = Math.max(contentWidth - containerWidth, 0);
	const isOverflow = overflowDistance > 1;
	el.classList.toggle('is-overflow', isOverflow);
	if (isOverflow) {
		el.style.setProperty('--marquee-distance', `${overflowDistance}px`);
		return;
	}
	el.style.removeProperty('--marquee-distance');
};

const vMarqueeOverflow: Directive<HTMLElement, void> = {
	mounted(el) {
		const marqueeEl = el as MarqueeElement;
		marqueeEl.__marqueeObserver = new ResizeObserver(() => updateMarqueeOverflow(marqueeEl));
		marqueeEl.__marqueeObserver.observe(marqueeEl);
		requestAnimationFrame(() => updateMarqueeOverflow(marqueeEl));
	},
	updated(el) {
		requestAnimationFrame(() => updateMarqueeOverflow(el as MarqueeElement));
	},
	unmounted(el) {
		const marqueeEl = el as MarqueeElement;
		marqueeEl.__marqueeObserver?.disconnect();
		delete marqueeEl.__marqueeObserver;
	},
};
</script>

<style scoped lang="scss">
.camera-list-panel {
	display: grid;
	grid-template-rows: auto auto minmax(0, 1fr);
	gap: 10px;
	min-width: 0;
	min-height: 0;
	max-height: min(460px, calc(100vh - 260px));
	/* Không dùng overflow: hidden ở đây vì sẽ cắt mất badge của nút lọc (el-badge nhô ra ngoài nút).
	   Việc cuộn danh sách đã do .camera-list-items tự xử lý (overflow-y: auto + max-height). */
	overflow: visible;
}

.field-label {
	font-size: 13px;
	font-weight: 500;
	color: var(--el-text-color-regular);
}

.camera-list-header {
	display: grid;
	gap: 4px;
}

.camera-list-title-row {
	display: flex;
	align-items: center;
	justify-content: space-between;
	gap: 8px;
	min-width: 0;
}

.camera-list-hint {
	font-size: 12px;
	color: var(--el-text-color-secondary);
}

.camera-list-hint.is-ready {
	color: var(--el-color-success);
}

.camera-list-items {
	display: grid;
	align-content: start;
	gap: 8px;
	min-width: 0;
	min-height: 0;
	max-height: 100%;
	overflow-y: auto;
	overflow-x: hidden;
	padding-right: 4px;
}

.camera-list-items :deep(.el-tooltip) {
	display: block;
	width: 100%;
}

.camera-list-items :deep(.el-tooltip__trigger) {
	display: block;
	width: 100% !important;
}

.camera-list-item {
	display: grid !important;
	grid-template-columns: 24px minmax(0, 1fr) auto;
	align-items: center;
	gap: 8px;
	width: 100%;
	border: 1px solid var(--el-border-color-light);
	border-radius: 7px;
	background: var(--el-bg-color);
	color: var(--el-text-color-primary);
	padding: 8px 10px;
	cursor: pointer;
	text-align: left;
	min-width: 0;
	height: auto;
	overflow: visible;
}

.camera-list-item:hover:not(:disabled) {
	border-color: var(--el-border-color);
	background: var(--el-fill-color-light);
}

.camera-list-item.is-added {
	border-color: var(--el-color-primary);
	background: var(--el-color-primary-light-9);
	box-shadow: inset 0 2px 5px rgba(16, 24, 40, 0.16);
	transform: translateY(1px);
}

.camera-list-item.is-added:hover:not(:disabled) {
	border-color: var(--el-color-primary-dark-2);
	background: var(--el-color-primary-light-8);
}

.camera-list-item.is-added .camera-list-item-icon,
.camera-list-item.is-added .camera-list-item-name {
	color: var(--el-color-primary);
}

.camera-list-item:disabled {
	opacity: 0.55;
	cursor: not-allowed;
}

.camera-list-item-icon {
	color: var(--el-text-color-regular);
	display: inline-flex;
	align-items: center;
	justify-content: center;
}

.camera-list-item-info-text {
	display: grid;
	gap: 4px;
	min-width: 0;
	width: 100%;
}

.camera-list-item-name {
	font-size: 14px;
	color: var(--el-text-color-regular);
	display: block;
	min-width: 0;
	line-height: 1.3;
	overflow: hidden;
	white-space: nowrap;
}

.camera-list-item-name-text {
	display: inline-block;
	min-width: 100%;
	padding-right: 20px;
}

.camera-list-item-name.is-overflow .camera-list-item-name-text {
	animation: camera-list-name-marquee 7s linear infinite;
}

.camera-list-item-ip {
	font-size: 12px;
	line-height: 1.25;
	color: var(--el-text-color-secondary);
	word-break: break-all;
	white-space: normal;
}

.camera-list-item-status {
	min-width: 0;
	flex-shrink: 0;
	align-self: center;
	justify-self: end;
}

.camera-list-empty {
	border: 1px dashed var(--el-border-color);
	border-radius: 8px;
	padding: 12px;
	font-size: 13px;
	color: var(--el-text-color-secondary);
	text-align: center;
}

.camera-list-tools {
	display: flex;
	align-items: center;
	gap: 6px;

	.el-input {
		flex: 1;
	}

	.filter-type-btn {
		flex-shrink: 0;
	}
}

.camera-type-filter {
	display: flex;
	flex-direction: column;
	gap: 8px;
}

.camera-type-filter-header {
	display: flex;
	align-items: center;
	justify-content: space-between;
	font-size: 13px;
	font-weight: 600;
	color: var(--el-text-color-primary);
}

.camera-type-filter-list {
	display: flex;
	flex-direction: column;
	gap: 4px;
	max-height: 200px;
	overflow-y: auto;
}

.camera-type-filter-item {
	padding: 6px 10px;
	border-radius: 4px;
	font-size: 13px;
	cursor: pointer;
	color: var(--el-text-color-regular);
	transition: background 0.15s ease, color 0.15s ease;
}

.camera-type-filter-item:hover {
	background: var(--el-fill-color-light, #f5f7fa);
}

.camera-type-filter-item.is-active {
	background: var(--el-color-primary-light-9);
	color: var(--el-color-primary);
	font-weight: 500;
}

@keyframes camera-list-name-marquee {

	0%,
	15% {
		transform: translateX(0);
	}

	85%,
	100% {
		transform: translateX(calc(-1 * var(--marquee-distance, 0px)));
	}
}
</style>
