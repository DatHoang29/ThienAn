<template>
	<div class="ptz-move-pad" :aria-label="t('lz.label.cctvDashboard.manualControl')">
		<svg class="pad-svg" viewBox="0 0 220 220" role="img">
			<circle class="pad-shell" cx="110" cy="110" r="104" />
			<g>
				<path
					v-for="item in directions"
					:key="item.key"
					class="pad-segment"
					:d="item.path"
					:title="item.label"
					@mousedown.prevent="onPress(item.key)"
					@mouseup="onRelease"
					@mouseleave="onRelease"
				/>
			</g>
			<g class="pad-labels">
				<text v-for="item in directions" :key="`${item.key}-label`" :x="item.labelX" :y="item.labelY">
					{{ item.icon }}
				</text>
			</g>
			<circle class="pad-center-ring" cx="110" cy="110" r="42" />
			<polygon class="inner-marker top" points="110,66 117,78 103,78" />
			<polygon class="inner-marker right" points="154,110 142,117 142,103" />
			<polygon class="inner-marker bottom" points="110,154 117,142 103,142" />
			<polygon class="inner-marker left" points="66,110 78,117 78,103" />
		</svg>
		<button class="pad-center" type="button" @click="$emit('center')">
			<el-icon><ele-Aim /></el-icon>
		</button>
	</div>
</template>

<script setup lang="ts">
import { useI18n } from 'vue-i18n';

const { t } = useI18n();
type MoveDirection = 'up' | 'up-right' | 'right' | 'down-right' | 'down' | 'down-left' | 'left' | 'up-left';

const center = 110;
const outerRadius = 104;
const innerRadius = 47;
const segmentSize = 45;

const polarPoint = (radius: number, angle: number) => {
	const radian = ((angle - 90) * Math.PI) / 180;
	return {
		x: center + radius * Math.cos(radian),
		y: center + radius * Math.sin(radian),
	};
};

const annularSegmentPath = (angle: number) => {
	const start = angle - segmentSize / 2;
	const end = angle + segmentSize / 2;
	const outerStart = polarPoint(outerRadius, start);
	const outerEnd = polarPoint(outerRadius, end);
	const innerEnd = polarPoint(innerRadius, end);
	const innerStart = polarPoint(innerRadius, start);

	return [
		`M ${outerStart.x.toFixed(2)} ${outerStart.y.toFixed(2)}`,
		`A ${outerRadius} ${outerRadius} 0 0 1 ${outerEnd.x.toFixed(2)} ${outerEnd.y.toFixed(2)}`,
		`L ${innerEnd.x.toFixed(2)} ${innerEnd.y.toFixed(2)}`,
		`A ${innerRadius} ${innerRadius} 0 0 0 ${innerStart.x.toFixed(2)} ${innerStart.y.toFixed(2)}`,
		'Z',
	].join(' ');
};

const createDirection = (key: MoveDirection, label: string, icon: string, angle: number) => {
	const labelPoint = polarPoint(76, angle);
	return {
		key,
		label,
		icon,
		path: annularSegmentPath(angle),
		labelX: labelPoint.x,
		labelY: labelPoint.y + 5,
	};
};

const directions = [
	createDirection('up', t('lz.label.cctvDashboard.moveUp'), '▲', 0),
	createDirection('up-right', t('lz.label.cctvDashboard.moveUpRight'), '⌝', 45),
	createDirection('right', t('lz.label.cctvDashboard.moveRight'), '►', 90),
	createDirection('down-right', t('lz.label.cctvDashboard.moveDownRight'), '⌟', 135),
	createDirection('down', t('lz.label.cctvDashboard.moveDown'), '▼', 180),
	createDirection('down-left', t('lz.label.cctvDashboard.moveDownLeft'), '⌞', 225),
	createDirection('left', t('lz.label.cctvDashboard.moveLeft'), '◄', 270),
	createDirection('up-left', t('lz.label.cctvDashboard.moveUpLeft'), '⌜', 315),
];

const emit = defineEmits<{
	(e: 'move', direction: MoveDirection): void;
	(e: 'stop'): void;
	(e: 'center'): void;
}>();

let pressing = false;

const onPress = (direction: MoveDirection) => {
	pressing = true;
	emit('move', direction);
};

const onRelease = () => {
	if (!pressing) return;
	pressing = false;
	emit('stop');
};
</script>

<style scoped lang="scss">
.ptz-move-pad {
	position: relative;
	width: 210px;
	aspect-ratio: 1;
	margin: 0 auto;
	border-radius: 50%;
	background: #1b1b1b;
	box-shadow:
		inset 0 0 0 2px rgba(255, 255, 255, 0.05),
		0 16px 36px rgba(0, 0, 0, 0.32);
	overflow: hidden;
}
.pad-svg {
	position: absolute;
	inset: 0;
	width: 100%;
	height: 100%;
	display: block;
}
.pad-shell {
	fill: #1b1b1b;
	stroke: rgba(255, 255, 255, 0.06);
	stroke-width: 2;
}
.pad-title {
	position: absolute;
	top: 6px;
	left: 52px;
	right: 52px;
	z-index: 3;
	color: #d7e9ff;
	font-size: 11px;
	font-weight: 700;
	text-align: center;
	pointer-events: none;
}
.pad-title::before,
.pad-title::after {
	content: '';
	position: absolute;
	top: 5px;
	width: 30px;
	height: 2px;
	background: rgba(255, 255, 255, 0.28);
}
.pad-title::before {
	right: 100%;
	margin-right: 10px;
}
.pad-title::after {
	left: 100%;
	margin-left: 10px;
}
.pad-segment {
	fill: #575757;
	stroke: #242424;
	stroke-width: 2;
	cursor: pointer;
	transition:
		fill 0.14s ease,
		stroke 0.14s ease;
}
.pad-segment:hover {
	fill: #777777;
	stroke: rgba(255, 255, 255, 0.22);
}
.pad-labels {
	pointer-events: none;

	text {
		fill: #edf2f7;
		font-size: 19px;
		font-weight: 800;
		text-anchor: middle;
		dominant-baseline: middle;
	}
}
.pad-center-ring {
	fill: #1f1f1f;
	stroke: rgba(255, 255, 255, 0.08);
	stroke-width: 1;
}
.pad-center {
	position: absolute;
	inset: 72px;
	z-index: 4;
	display: grid;
	place-items: center;
	border: 8px solid #1f1f1f;
	border-radius: 50%;
	background: #6b6b6b;
	color: #e5e7eb;
	cursor: pointer;
	box-shadow: 0 0 0 1px rgba(255, 255, 255, 0.06);
	transition:
		background 0.14s ease,
		color 0.14s ease;
}
.pad-center:hover {
	background: #7d7d7d;
	color: #ffffff;
}
.inner-marker {
	fill: #d1d5db;
	opacity: 0.8;
	pointer-events: none;
}
</style>
