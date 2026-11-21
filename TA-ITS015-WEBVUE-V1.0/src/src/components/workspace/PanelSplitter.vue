<template>
	<div
		class="panel-splitter"
		:class="{ dragging, collapsed: !expanded, 'panel-splitter-horizontal': horizontal }"
		role="separator"
		tabindex="0"
		:aria-orientation="horizontal ? 'horizontal' : 'vertical'"
		:aria-label="label"
		:aria-controls="controls"
		:aria-valuemin="0"
		:aria-valuemax="Math.round(max)"
		:aria-valuenow="Math.round(width)"
		:title="resizeHint"
		@pointerdown="startDrag"
		@pointermove="drag"
		@pointerup="stopDrag"
		@pointercancel="cancelDrag"
		@lostpointercapture="stopDrag"
		@keydown="onKeydown"
		@click="onClick"
	>
		<div class="splitter-buttons">
			<div v-for="button in buttons" v-show="button.enabled" :key="button.direction" class="splitter-button-slot">
				<button
					type="button"
					:class="{ 'with-label': !expanded && button.direction === 'grow' }"
					:data-direction="button.direction"
					:disabled="!button.enabled"
					:aria-hidden="!button.enabled"
					:aria-controls="controls"
					:aria-label="button.label"
					:title="button.label"
					@click.stop="onClick($event, button.direction)"
					@keydown.stop
				>
					<span v-if="!expanded && button.direction === 'grow'" class="splitter-label" aria-hidden="true">
						{{ label }}
					</span>
					<el-icon><component :is="button.icon" /></el-icon>
				</button>
			</div>
		</div>
	</div>
</template>

<script lang="ts" setup name="workspacePanelSplitter">
import { computed, ref } from 'vue';
import { ArrowDown, ArrowLeft, ArrowRight, ArrowUp } from '@element-plus/icons-vue';

type Direction = 'grow' | 'shrink';

interface Props {
	side: 'left' | 'right';
	orientation?: 'vertical' | 'horizontal';
	label: string;
	controls: string;
	width: number;
	min: number;
	max: number;
	expanded: boolean;
	expandLabel: string;
	collapseLabel: string;
	resizeHint?: string;
}

const props = defineProps<Props>();
const emits = defineEmits<{ resize: [width: number]; step: [direction: Direction] }>();
const horizontal = computed(() => props.orientation === 'horizontal');

// Pointer state
const dragging = ref(false);
let pointerId: number | undefined;
let startPosition = 0;
let startWidth = 0;
let pressedDirection: Direction | undefined;
let moved = false;

// Available panel actions
const canGrow = computed(() => !props.expanded || props.width < props.max - 1);
const canShrink = computed(() => props.expanded);
const buttons = computed(() => {
	const growLabel = props.expandLabel;
	const shrinkLabel = props.collapseLabel;
	return (
		[
			{ icon: horizontal.value ? ArrowUp : ArrowLeft, direction: props.side === 'left' ? 'shrink' : 'grow' },
			{ icon: horizontal.value ? ArrowDown : ArrowRight, direction: props.side === 'left' ? 'grow' : 'shrink' },
		] as { icon: typeof ArrowLeft; direction: Direction }[]
	).map((button) => ({
		...button,
		enabled: button.direction === 'grow' ? canGrow.value : canShrink.value,
		label: button.direction === 'grow' ? growLabel : shrinkLabel,
	}));
});

function updateWidth(width: number) {
	// Keep a small snap zone so a panel can be dragged completely out of view.
	const snapSize = horizontal.value ? Math.min(80, props.min * 0.5) : 80;
	emits('resize', width < snapSize ? 0 : Math.max(props.min, Math.min(props.max, width)));
}

function startDrag(event: PointerEvent) {
	if (event.button !== 0) return;
	const button = (event.target as HTMLElement).closest('button');
	if (button?.disabled) return;
	event.preventDefault();
	event.stopPropagation();
	pointerId = event.pointerId;
	startPosition = horizontal.value ? event.clientY : event.clientX;
	startWidth = props.width;
	pressedDirection = button?.dataset.direction as Direction | undefined;
	moved = false;
	dragging.value = true;
	(event.currentTarget as HTMLElement).setPointerCapture(event.pointerId);
}

function drag(event: PointerEvent) {
	if (!dragging.value || event.pointerId !== pointerId) return;
	const delta = ((horizontal.value ? event.clientY : event.clientX) - startPosition) * (props.side === 'left' ? 1 : -1);
	if (Math.abs(delta) > 3) {
		moved = true;
		updateWidth(startWidth + delta);
	}
}

function stopDrag(event: PointerEvent) {
	if (event.pointerId !== pointerId) return;
	dragging.value = false;
	pointerId = undefined;
	const handle = event.currentTarget as HTMLElement;
	if (handle.hasPointerCapture(event.pointerId)) handle.releasePointerCapture(event.pointerId);
}

function cancelDrag(event: PointerEvent) {
	pressedDirection = undefined;
	moved = true;
	stopDrag(event);
}

function onClick(event: MouseEvent, direction?: Direction) {
	const action = direction ?? pressedDirection;
	if ((event.detail === 0 || !moved) && action && (action === 'grow' ? canGrow.value : canShrink.value)) emits('step', action);
	pressedDirection = undefined;
	moved = false;
}

function onKeydown(event: KeyboardEvent) {
	const decreaseKey = horizontal.value ? 'ArrowUp' : 'ArrowLeft';
	const increaseKey = horizontal.value ? 'ArrowDown' : 'ArrowRight';
	if (![decreaseKey, increaseKey, 'Home', 'End', 'Enter', ' '].includes(event.key)) return;
	event.preventDefault();
	event.stopPropagation();
	if (event.key === 'Enter' || event.key === ' ') {
		emits('step', props.expanded ? 'shrink' : 'grow');
		return;
	}
	if (event.key === 'Home') {
		emits('resize', 0);
		return;
	}
	if (event.key === 'End') {
		emits('resize', props.max);
		return;
	}
	const delta = (event.key === increaseKey ? 24 : -24) * (props.side === 'left' ? 1 : -1);
	updateWidth(props.width === 0 && delta > 0 ? props.min : props.width + delta);
}
</script>

<style lang="scss" scoped>
.panel-splitter {
	position: relative;
	display: flex;
	align-items: center;
	justify-content: center;
	min-height: 0;
	cursor: col-resize;
	touch-action: none;
	user-select: none;
	&::before {
		content: '';
		position: absolute;
		top: 8px;
		bottom: 8px;
		width: 2px;
		border-radius: 2px;
		background: var(--workspace-border, var(--el-border-color, #dcdfe6));
		pointer-events: none;
	}
	&:hover::before,
	&.dragging::before {
		width: 3px;
		background: var(--workspace-primary, var(--el-color-primary, #409eff));
	}
	&:focus-visible {
		outline: 2px solid var(--workspace-primary, var(--el-color-primary, #409eff));
		outline-offset: -2px;
	}
	.splitter-buttons {
		position: relative;
		z-index: 1;
		display: flex;
		flex-direction: column;
		align-items: center;
		gap: 5px;
		flex: none;
	}
	.splitter-button-slot {
		position: relative;
		width: 22px;
		height: 25px;
	}
	.splitter-label {
		writing-mode: vertical-rl;
		transform: rotate(180deg);
		white-space: nowrap;
		font-size: 12px;
		font-weight: 600;
		line-height: 1;
		pointer-events: none;
	}
	button {
		display: grid;
		place-items: center;
		width: 22px;
		height: 25px;
		padding: 0;
		border: 1px solid var(--workspace-border, var(--el-border-color, #dcdfe6));
		border-radius: 4px;
		background: var(--workspace-surface, var(--el-bg-color, #fff));
		color: #303133;
		cursor: col-resize;
	}
	button.with-label {
		display: flex;
		flex-direction: column;
		align-items: center;
		gap: 5px;
		height: auto;
		padding-top: 9px;
	}
	&.collapsed .splitter-button-slot {
		height: auto;
	}
	button.with-label .el-icon {
		flex: 0 0 23px;
		height: 23px;
	}
	button:hover:not(:disabled) {
		color: var(--workspace-primary, var(--el-color-primary, #409eff));
		background: var(--workspace-primary-soft, var(--el-color-primary-light-9, #ecf5ff));
		border-color: var(--workspace-primary, var(--el-color-primary, #409eff));
	}
	&.collapsed button.with-label:not(:disabled) {
		color: var(--workspace-surface, var(--el-bg-color, #fff));
		background: var(--workspace-primary, var(--el-color-primary, #409eff));
		border-color: var(--workspace-primary-dark, var(--el-color-primary-dark-2, #337ecc));
	}
	&.collapsed button.with-label:not(:disabled):hover {
		background: var(--workspace-primary-dark, var(--el-color-primary-dark-2, #337ecc));
	}
	button:disabled {
		visibility: hidden;
		pointer-events: none;
	}
	&.panel-splitter-horizontal {
		min-width: 0;
		cursor: row-resize;
		&::before {
			top: auto;
			bottom: auto;
			left: 8px;
			right: 8px;
			width: auto;
			height: 2px;
		}
		&:hover::before,
		&.dragging::before {
			width: auto;
			height: 3px;
		}
		.splitter-buttons {
			flex-direction: row;
		}
		.splitter-button-slot,
		button {
			width: 25px;
			height: 22px;
		}
		&.collapsed .splitter-button-slot {
			width: auto;
			height: 22px;
		}
		button {
			cursor: row-resize;
		}
		button.with-label {
			width: auto;
			flex-direction: row;
			justify-content: center;
			gap: 6px;
			height: 22px;
			padding: 0 8px;
			line-height: 1;
		}
		button.with-label .el-icon {
			flex: 0 0 12px;
			width: 12px;
			height: 12px;
		}
		.splitter-label {
			writing-mode: horizontal-tb;
			transform: none;
		}
	}
	.el-icon {
		display: inline-flex;
		align-items: center;
		justify-content: center;
		line-height: 1;
		font-size: 11px;
	}
	.el-icon :deep(svg) {
		display: block;
	}
}
</style>
