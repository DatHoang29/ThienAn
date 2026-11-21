<template>
	<div class="window-props">
		<div class="window-props-header">
			<div class="window-props-heading">
				<el-icon class="window-props-heading-icon"><ele-Operation /></el-icon>
				<span class="window-props-title">{{ $t('lz.label.videoWall.windowProps') }}</span>
			</div>
			<el-button type="danger" text size="small" icon="ele-Delete" :title="$t('lz.button.videoWall.deleteWindow')" @click="emit('delete')" />
		</div>
		<!-- Nguồn: chấm màu + tên loại lấy từ danh mục videowall_source_type, cùng màu với cửa sổ trên canvas -->
		<div class="window-props-source">
			<i class="window-props-source-dot" :style="{ backgroundColor: sourceColor }" />
			<span class="window-props-source-name" :title="sourceName">{{ sourceName }}</span>
			<span class="window-props-source-type">· {{ sourceTypeName }}</span>
		</div>

		<div class="window-props-groups">
			<!-- Vị trí -->
			<div class="window-props-group">
				<div class="window-props-caption">{{ $t('lz.label.videoWall.windowPosition') }}</div>
				<!-- Tường nhiều khối: cửa sổ nằm trọn trong 1 khối, đổi khối thì kéo cửa sổ về trong khối mới.
					Ít khối ⇒ segmented (1 lần bấm, thấy hết khối); nhiều khối ⇒ select cho gọn. -->
				<div v-if="topology.isMultiBlock" class="window-props-row">
					<el-segmented v-if="blockOptions.length <= SEGMENTED_MAX" :model-value="window.blockNo" :options="blockOptions" size="small" block :disabled="window.locked"
						class="window-props-block" @change="(val: string | number | boolean) => onBlockChange(Number(val))" />
					<el-select v-else :model-value="window.blockNo" size="small" :disabled="window.locked" class="window-props-block" @change="onBlockChange">
						<el-option v-for="b in blockOptions" :key="b.value" :value="b.value" :label="b.label" />
					</el-select>
				</div>
				<!-- Nhập theo hàng (H) / cột (C) của ô — giống nhãn H · C trên lưới; toạ độ pixel X / Y hiện ở dòng dưới -->
				<div class="window-props-row">
					<div class="window-props-field">
						<span class="window-props-key">H</span>
						<el-input-number
							:model-value="toCells(window.y, PANEL_H) + 1"
							@change="(val: number) => updatePosition('y', val, PANEL_H, maxY)"
							:min="1"
							:max="toCells(maxY, PANEL_H) + 1"
							:step="1"
							:precision="2"
							size="small"
							:disabled="window.locked"
							controls-position="right"
						/>
					</div>
					<div class="window-props-field">
						<span class="window-props-key">C</span>
						<el-input-number
							:model-value="toCells(window.x, PANEL_W) + 1"
							@change="(val: number) => updatePosition('x', val, PANEL_W, maxX)"
							:min="1"
							:max="toCells(maxX, PANEL_W) + 1"
							:step="1"
							:precision="2"
							size="small"
							:disabled="window.locked"
							controls-position="right"
						/>
					</div>
				</div>
				<div class="window-props-hint">{{ cellPosText }}</div>
			</div>

			<!-- Kích thước -->
			<div class="window-props-group">
				<div class="window-props-caption">{{ $t('lz.entity.base.size') }}</div>
				<div class="window-props-row">
					<div class="window-props-field">
						<span class="window-props-key">W</span>
						<el-input-number
							:model-value="window.w"
							@change="(val: number) => updateSize('w', val, PANEL_W, maxW)"
							:min="MIN_W"
							:max="maxW"
							:step="PANEL_W"
							size="small"
							:disabled="window.locked"
							controls-position="right"
						/>
					</div>
					<div class="window-props-field">
						<span class="window-props-key">H</span>
						<el-input-number
							:model-value="window.h"
							@change="(val: number) => updateSize('h', val, PANEL_H, maxH)"
							:min="MIN_H"
							:max="maxH"
							:step="PANEL_H"
							size="small"
							:disabled="window.locked"
							controls-position="right"
						/>
					</div>
				</div>
				<div class="window-props-hint">{{ cellSpanText }}</div>
			</div>

			<!-- Lớp -->
			<div class="window-props-group">
				<div class="window-props-caption">
					{{ $t('lz.label.videoWall.windowLayer') }}
					<el-tooltip :content="$t('lz.label.videoWall.windowLayerHint')" placement="top">
						<el-icon class="window-props-help"><ele-QuestionFilled /></el-icon>
					</el-tooltip>
				</div>
				<div class="window-props-row">
					<div class="window-props-field">
						<span class="window-props-key">Z</span>
						<el-input-number
							:model-value="window.zIndex"
							@change="(val: number) => updateField('zIndex', val)"
							:min="0"
							size="small"
							:disabled="window.locked"
							controls-position="right"
						/>
					</div>
					<!-- Thao tác trực quan thay cho tự nhập số: đưa cửa sổ lên trên cùng / xuống dưới cùng -->
					<el-button-group class="window-props-layer-btns">
						<el-tooltip :content="$t('lz.button.videoWall.bringToFront')" placement="top">
							<el-button size="small" icon="ele-Top" :disabled="window.locked || props.isTopLayer" @click="emit('layer', 'front')" />
						</el-tooltip>
						<el-tooltip :content="$t('lz.button.videoWall.sendToBack')" placement="top">
							<el-button size="small" icon="ele-Bottom" :disabled="window.locked || props.isBottomLayer" @click="emit('layer', 'back')" />
						</el-tooltip>
					</el-button-group>
				</div>
				<div class="window-props-hint">{{ $t('lz.label.videoWall.windowLayerShortHint') }}</div>
			</div>

			<!-- Khóa trên Monitor -->
			<div class="window-props-group window-props-group--inline">
				<div class="window-props-caption">{{ $t('lz.label.videoWall.lockOnMonitor') }}</div>
				<el-switch :model-value="window.locked" @change="emit('toggleLock')" />
			</div>
		</div>
	</div>
</template>

<script lang="ts" setup name="videoWallWindowProperties">
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import type { VwSourceOutput } from '/@/api-services/videoWall';
import { snapPosition, snapSize } from '/@/utils/videoWall/sceneWindow';
import type { EditWindow } from '/@/utils/videoWall/sceneWindow';
import { sourceTypeColor } from '/@/utils/videoWall/sourceColors';
import { useBaseConfig } from '/@/stores/baseConfig';
import { BaseConfigTypeEnum } from '/@/types/enums';
import { blockLabel, useVideoWallTopology } from '/@/stores/its/videoWallTopology';

const { t: $t } = useI18n();
const topology = useVideoWallTopology();

interface Props {
	window: EditWindow;
	source?: VwSourceOutput;
	/** Cửa sổ đã nằm trên cùng / dưới cùng ⇒ khoá nút tương ứng. */
	isTopLayer?: boolean;
	isBottomLayer?: boolean;
}
const props = defineProps<Props>();
const emit = defineEmits<{
	(e: 'update', win: EditWindow): void;
	(e: 'delete'): void;
	(e: 'toggleLock'): void;
	(e: 'layer', dir: 'front' | 'back'): void;
}>();

/** Kích thước 1 ô theo bố cục tường (pixel logic). */
const PANEL_W = computed(() => topology.panelW);
const PANEL_H = computed(() => topology.panelH);
/** Kích thước nhỏ nhất của cửa sổ: 1 ô, khớp giới hạn khi kéo resize trên canvas. */
const MIN_W = PANEL_W;
const MIN_H = PANEL_H;

// Giới hạn trên để cửa sổ luôn nằm trong khối: vượt quá thì el-input-number tự kéo về max, không báo lỗi.
// Không cho nhỏ hơn :min vì el-input-number lỗi khi min > max.
const blockW = computed(() => topology.blockSize(props.window.blockNo).w);
const blockH = computed(() => topology.blockSize(props.window.blockNo).h);
const maxX = computed(() => Math.max(0, blockW.value - props.window.w));
const maxY = computed(() => Math.max(0, blockH.value - props.window.h));
const maxW = computed(() => Math.max(MIN_W.value, blockW.value - props.window.x));
const maxH = computed(() => Math.max(MIN_H.value, blockH.value - props.window.y));

/** Số khối tối đa còn hiển thị dạng segmented trong cột 300px. */
const SEGMENTED_MAX = 4;
const blockOptions = computed(() =>
	topology.blocks.map((b) => ({ value: b.blockNo!, label: $t('lz.label.videoWall.blockName', { name: blockLabel(b.blockNo) }) }))
);

/** Đổi khối: thu nhỏ cửa sổ nếu lớn hơn khối mới rồi kéo về trong khối. */
function onBlockChange(blockNo: number) {
	const { w: bw, h: bh } = topology.blockSize(blockNo);
	const w = Math.min(props.window.w, bw);
	const h = Math.min(props.window.h, bh);
	emit('update', { ...props.window, blockNo, w, h, x: Math.min(props.window.x, bw - w), y: Math.min(props.window.y, bh - h) });
}

const sourceName = computed(() => props.source?.name || props.source?.code || props.window.sourceId || '');
const sourceColor = computed(() => sourceTypeColor(props.source?.sourceType));
const sourceTypeName = computed(() => {
	const type = props.source?.sourceType;
	if (!type) return 'unknown';
	const item = useBaseConfig().getConfigItemByCode(BaseConfigTypeEnum.VideoWallSourceType, type) as { name?: string | null };
	return item?.name || type;
});

/** Quy đổi px ra số ô màn hình, tối đa 2 chữ số lẻ (cửa sổ cũ có thể lệch ô). */
const toCells = (px: number, panel: number) => Number((px / panel).toFixed(2));
/** Dòng gợi ý dưới ô H / C: toạ độ pixel X · Y (kèm tên khối khi tường nhiều khối). */
const cellPosText = computed(() => {
	const pos = `X ${props.window.x} · Y ${props.window.y}`;
	return topology.isMultiBlock ? `${$t('lz.label.videoWall.blockName', { name: blockLabel(props.window.blockNo) })} · ${pos}` : pos;
});
const cellSpanText = computed(() =>
	$t('lz.label.videoWall.windowCellSpan', { cols: toCells(props.window.w, PANEL_W.value), rows: toCells(props.window.h, PANEL_H.value) })
);

function updateField(field: keyof EditWindow, value: number) {
	if (value === undefined || value === null || value === props.window[field]) return;
	emit('update', { ...props.window, [field]: value });
}

/** Nhập vị trí theo ô (bắt đầu từ 1): đổi ra pixel rồi làm tròn theo lưới như khi kéo thả. */
function updatePosition(field: 'x' | 'y', cell: number, panel: number, max: number) {
	if (cell === undefined || cell === null) return;
	updateField(field, snapPosition((cell - 1) * panel, panel, max));
}

/** Nhập kích thước (pixel): làm tròn theo lưới như khi kéo resize. */
function updateSize(field: 'w' | 'h', value: number, panel: number, max: number) {
	if (value === undefined || value === null) return;
	updateField(field, snapSize(value, panel, max));
}
</script>

<style scoped>
/* Panel dọc trong cột cấu hình bên trái */
.window-props {
	padding: 12px 14px;
}

.window-props-header {
	display: flex;
	justify-content: space-between;
	align-items: center;
	gap: 8px;
	margin-bottom: 8px;
}

.window-props-heading {
	display: flex;
	align-items: center;
	gap: 8px;
	min-width: 0;
}

.window-props-heading-icon {
	color: var(--el-color-primary);
}

.window-props-title {
	font-size: 13px;
	font-weight: 600;
	color: var(--el-text-color-primary);
	white-space: nowrap;
}

/* Chip nguồn */
.window-props-source {
	display: flex;
	align-items: center;
	gap: 6px;
	min-width: 0;
	margin-bottom: 10px;
	padding: 4px 10px;
	border-radius: 12px;
	background: var(--el-fill-color-light);
	border: 1px solid var(--el-border-color-lighter);
	font-size: 12px;
}

.window-props-source-dot {
	width: 10px;
	height: 10px;
	border-radius: 50%;
	flex-shrink: 0;
}

.window-props-source-name {
	font-weight: 600;
	color: var(--el-text-color-primary);
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
}

.window-props-source-type {
	color: var(--el-text-color-secondary);
	white-space: nowrap;
}

/* Nhóm trường: Vị trí / Kích thước / Lớp / Khóa — xếp dọc, mỗi nhóm full chiều rộng */
.window-props-groups {
	display: flex;
	flex-direction: column;
	gap: 8px;
}

.window-props-group {
	display: flex;
	flex-direction: column;
	padding: 8px 10px;
	border-radius: 6px;
	background: var(--el-fill-color-light);
	border: 1px solid var(--el-border-color-lighter);
}

/* Khóa: caption và công tắc trên cùng 1 dòng */
.window-props-group--inline {
	flex-direction: row;
	align-items: center;
	justify-content: space-between;
}

.window-props-group--inline .window-props-caption {
	margin-bottom: 0;
}

.window-props-caption {
	margin-bottom: 6px;
	font-size: 11px;
	font-weight: 600;
	letter-spacing: 0.04em;
	text-transform: uppercase;
	color: var(--el-text-color-secondary);
	white-space: nowrap;
}

.window-props-row {
	display: flex;
	align-items: center;
	gap: 10px;
}

.window-props-row + .window-props-row {
	margin-top: 6px;
}

/* 2 trường chia đều chiều rộng panel */
.window-props-field {
	display: flex;
	align-items: center;
	gap: 6px;
	flex: 1;
	min-width: 0;
}

.window-props-key {
	width: 12px;
	flex-shrink: 0;
	font-size: 12px;
	font-weight: 700;
	color: var(--el-text-color-regular);
}

.window-props-field :deep(.el-input-number) {
	flex: 1;
	min-width: 0;
	width: auto;
}

.window-props-block {
	flex: 1;
	min-width: 0;
}

.window-props-help {
	margin-left: 2px;
	vertical-align: -2px;
	cursor: help;
	color: var(--el-text-color-placeholder);
}

.window-props-help:hover {
	color: var(--el-color-primary);
}

.window-props-layer-btns {
	flex-shrink: 0;
}

.window-props-hint {
	margin-top: 4px;
	font-size: 11px;
	color: var(--el-text-color-secondary);
}
</style>
