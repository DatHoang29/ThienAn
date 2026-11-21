<template>
	<div class="source-sidebar" :class="{ 'source-sidebar--collapsed': collapsed }">
		<!-- Toggle button -->
		<div class="source-sidebar-toggle" @click="collapsed = !collapsed">
			<el-icon :size="14">
				<ele-DArrowLeft v-if="collapsed" />
				<ele-DArrowRight v-else />
			</el-icon>
		</div>

		<!-- Content (visible when open) -->
		<template v-if="!collapsed">
			<div class="source-sidebar-header">
				<span class="source-sidebar-title">{{ $t('lz.entity.videoWall.source') }}</span>
				<el-tag size="small" type="info">{{ sources.length }}</el-tag>
			</div>

			<div class="source-sidebar-list">
				<!-- Grouped by source type -->
				<template v-for="group in groupedSources" :key="group.type">
					<div class="source-group-label">
						<el-icon :size="13">
							<ele-Monitor v-if="group.type === VideoWallSourceTypeEnum.LocalSignal" />
							<ele-Camera v-else-if="group.type === VideoWallSourceTypeEnum.IpStream" />
							<ele-Picture v-else />
						</el-icon>
						<span>{{ group.label }} ({{ group.items.length }})</span>
					</div>
					<div
						v-for="source in group.items"
						:key="source.id!"
						class="source-item"
						draggable="true"
						@dragstart="onDragStart($event, source.id ?? '')"
					>
						<div class="source-item-color" :style="{ background: getSourceColor(source.sourceType) }" />
						<div class="source-item-info">
							<span class="source-item-name">{{ source.name }}</span>
							<span class="source-item-meta">{{ source.code }} • {{ source.signalType || source.sourceType }}</span>
						</div>
					</div>
				</template>

				<div v-if="sources.length === 0" class="source-sidebar-empty">
					<el-icon :size="32" color="var(--el-text-color-placeholder)"><ele-FolderOpened /></el-icon>
					<span>{{ $t('lz.label.videoWall.noSource') }}</span>
				</div>
			</div>
		</template>
	</div>
</template>

<script lang="ts" setup name="videoWallSourceSidebar">
import { useI18n } from 'vue-i18n';
import { computed, ref } from 'vue';
import type { VwSourceOutput } from '/@/api-services/videoWall';
import { sourceTypeColor } from '/@/utils/videoWall/sourceColors';
import { BaseConfigTypeEnum, VideoWallSourceTypeEnum } from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';
const { t: $t } = useI18n();

const getConfigDataByCode = useBaseConfig().getConfigDataByCode;

interface Props {
	sources: VwSourceOutput[];
}
const props = defineProps<Props>();

const collapsed = ref(false);

interface SourceGroup {
	type: string;
	label: string;
	items: VwSourceOutput[];
}

/**
 * Nhóm nguồn theo loại — thứ tự + tên nhóm lấy từ danh mục videowall_source_type; loại lạ gom vào "Khác".
 * Bỏ "Liên kết controller": là dây nối giữa các bộ điều khiển, không phải nội dung để đưa lên màn hình.
 */
const groupedSources = computed((): SourceGroup[] => {
	const types = (getConfigDataByCode(BaseConfigTypeEnum.VideoWallSourceType) as { code: string; name: string }[]).filter(
		(t) => t.code !== VideoWallSourceTypeEnum.CascadeLink
	);
	const known = new Set(types.map((t) => t.code));
	const groups: SourceGroup[] = types.map((t) => ({ type: t.code, label: t.name, items: props.sources.filter((s) => s.sourceType === t.code) }));
	const others = props.sources.filter((s) => s.sourceType !== VideoWallSourceTypeEnum.CascadeLink && !known.has(s.sourceType ?? ''));
	if (others.length) groups.push({ type: 'other', label: $t('lz.label.videoWall.other'), items: others });
	return groups.filter((g) => g.items.length > 0);
});

/* Chấm màu theo LOẠI NGUỒN — dùng dải màu nhận diện riêng, tách khỏi màu trạng thái. */
function getSourceColor(sourceType?: string | null): string {
	return sourceTypeColor(sourceType);
}

function onDragStart(event: DragEvent, sourceId: string) {
	event.dataTransfer?.setData('text/plain', sourceId);
	if (event.dataTransfer) {
		event.dataTransfer.effectAllowed = 'copy';
	}
}
</script>

<style scoped>
.source-sidebar {
	width: 240px;
	background: var(--el-bg-color);
	border-left: 1px solid var(--el-border-color);
	display: flex;
	flex-direction: column;
	flex-shrink: 0;
	transition: width 0.2s ease;
	position: relative;
}
.source-sidebar--collapsed {
	width: 28px;
}
.source-sidebar-toggle {
	position: absolute;
	top: 50%;
	left: -14px;
	transform: translateY(-50%);
	width: 28px;
	height: 28px;
	display: flex;
	align-items: center;
	justify-content: center;
	background: var(--el-bg-color);
	border: 1px solid var(--el-border-color);
	border-radius: 50%;
	cursor: pointer;
	z-index: 10;
	transition: background 0.15s;
}
.source-sidebar-toggle:hover {
	background: var(--el-fill-color);
}
.source-sidebar-header {
	display: flex;
	align-items: center;
	justify-content: space-between;
	padding: 12px 12px 8px;
}
.source-sidebar-title {
	font-size: 13px;
	font-weight: 600;
	color: var(--el-text-color-primary);
}
.source-sidebar-list {
	flex: 1;
	overflow-y: auto;
	padding: 0 8px 8px;
}
.source-group-label {
	display: flex;
	align-items: center;
	gap: 6px;
	padding: 8px 6px 4px;
	font-size: 11px;
	font-weight: 600;
	color: var(--el-text-color-secondary);
	text-transform: uppercase;
	letter-spacing: 0.03em;
}
.source-item {
	display: flex;
	align-items: center;
	gap: 10px;
	padding: 8px 10px;
	border-radius: 6px;
	cursor: grab;
	transition: background 0.12s, box-shadow 0.12s;
	margin-bottom: 2px;
}
.source-item:hover {
	background: var(--el-fill-color-light);
	box-shadow: 0 1px 3px rgba(0, 0, 0, 0.06);
}
.source-item:active {
	cursor: grabbing;
	opacity: 0.75;
	transform: scale(0.98);
}
.source-item-color {
	width: 4px;
	height: 28px;
	border-radius: 2px;
	flex-shrink: 0;
}
.source-item-info {
	display: flex;
	flex-direction: column;
	min-width: 0;
	gap: 1px;
}
.source-item-name {
	font-size: 12px;
	font-weight: 500;
	color: var(--el-text-color-primary);
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
}
.source-item-meta {
	font-size: 10px;
	color: var(--el-text-color-placeholder);
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
}
.source-sidebar-empty {
	display: flex;
	flex-direction: column;
	align-items: center;
	gap: 8px;
	padding: 32px 8px;
	font-size: 12px;
	color: var(--el-text-color-placeholder);
}
</style>
