<template>
	<div class="template-preview" :class="[`is-${size}`, { 'has-meta': showMeta }]">
		<div class="template-preview__thumb">
			<div class="template-preview__grid" :style="gridStyle">
				<div
					v-for="item in sortedItems"
					:key="item.SlotNo"
					class="template-preview__item"
						:class="getItemTone(item)"
					:style="getItemStyle(item)"
				>
					<span v-if="showSlotNo" class="template-preview__slot-no">{{ item.SlotNo }}</span>
				</div>
			</div>
		</div>
		<div v-if="showMeta" class="template-preview__meta">
			<div class="template-preview__title">{{ title }}</div>
			<div class="template-preview__subtitle">{{ subtitle }}</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import {
	dashboardGridConfig,
	type ICctvDashboardTemplate,
	type ICctvDashboardTemplateSlot,
} from '/@/views/cctv/dashboard/types';
import { getFullGridStyle } from '/@/views/cctv/templates/utils/templatePresentation';
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';

const { t } = useI18n();

const props = withDefaults(
	defineProps<{
		template: ICctvDashboardTemplate;
		size?: 'sm' | 'md';
		showMeta?: boolean;
		showSlotNo?: boolean;
	}>(),
	{
		size: 'md',
		showMeta: true,
		showSlotNo: false,
	}
);

const title = computed(() => props.template.Name || props.template.ID);
const subtitle = computed(() => t('lz.label.cctvDashboard.cameraCount', { count: props.template.Items.length }));
const sortedItems = computed(() => [...props.template.Items].sort((left, right) => right.W * right.H - left.W * left.H || left.SlotNo - right.SlotNo));
const largestArea = computed(() => Math.max(...props.template.Items.map((item) => item.W * item.H), 1));
const gridStyle = computed(() => getFullGridStyle(dashboardGridConfig.column, dashboardGridConfig.row));

const getItemStyle = (item: ICctvDashboardTemplateSlot) => ({
	gridColumn: `${item.X + 1} / span ${Math.max(item.W, 1)}`,
	gridRow: `${item.Y + 1} / span ${Math.max(item.H, 1)}`,
});

const getItemTone = (item: ICctvDashboardTemplateSlot) => (item.W * item.H === largestArea.value && largestArea.value > 1 ? 'primary' : 'default');
</script>

<style scoped lang="scss">
.template-preview {
	display: flex;
	align-items: center;
	gap: 12px;
	min-width: 0;
}

.template-preview__thumb {
	flex-shrink: 0;
	padding: 6px;
	// border-radius: 14px;
	background: linear-gradient(180deg, #f8fafc 0%, #eef2ff 100%);
	border: 1px solid #dbe4ff;
	box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.8);
}

.template-preview__grid {
	display: grid;
	gap: 3px;
	width: 72px;
	height: 72px;
}

.template-preview__item {
	display: flex;
	align-items: center;
	justify-content: center;
	min-width: 0;
	min-height: 0;
	border-radius: 0px;
	border: 1px solid rgba(37, 99, 235, 0.18);
	background: linear-gradient(135deg, rgba(59, 130, 246, 0.18), rgba(37, 99, 235, 0.08));
	color: #1d4ed8;
	font-size: 10px;
	font-weight: 700;
	box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.75);
	overflow: hidden;
}

.template-preview__item.primary {
	background: linear-gradient(135deg, rgba(15, 118, 110, 0.22), rgba(13, 148, 136, 0.14));
	border-color: rgba(15, 118, 110, 0.24);
	color: #0f766e;
}

.template-preview__item.secondary {
	background: linear-gradient(135deg, rgba(245, 158, 11, 0.2), rgba(251, 191, 36, 0.12));
	border-color: rgba(217, 119, 6, 0.2);
	color: #b45309;
}

.template-preview__slot-no {
	line-height: 1;
}

.template-preview__meta {
	display: grid;
	gap: 2px;
	min-width: 0;
	flex: 1;
}

.template-preview__title {
	font-size: 13px;
	font-weight: 600;
	color: #0f172a;
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
}

.template-preview__subtitle {
	font-size: 12px;
	color: #64748b;
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
}

.template-preview.is-sm {
	gap: 10px;
}

.template-preview.is-sm .template-preview__thumb {
	padding: 5px;
	border-radius: 12px;
}

.template-preview.is-sm .template-preview__grid {
	width: 60px;
	height: 60px;
	gap: 2px;
}

.template-preview.is-sm .template-preview__item {
	border-radius: 6px;
	font-size: 9px;
}

.template-preview.is-sm .template-preview__title {
	font-size: 12px;
}

.template-preview.is-sm .template-preview__subtitle {
	font-size: 11px;
}
</style>
