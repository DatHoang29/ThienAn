<template>
	<div class="speed-strip">
		<div class="strip-legend">
			<span class="legend-title">{{ $t('lz.label.tmsZone.legendMaxSpeed') }}:</span>
			<span v-for="bin in BINS" :key="bin.label" class="legend-item">
				<i class="swatch" :style="{ background: bin.color }" />{{ bin.label }}
			</span>
			<span class="legend-item"><i class="swatch swatch--invalid" />{{ $t('lz.label.tmsZone.legendInvalid') }}</span>
			<span class="legend-item"><i class="swatch swatch--default" />{{ $t('lz.label.tmsZone.sourceDirection') }}</span>
			<span class="legend-item"><i class="cam-tick-legend" />{{ $t('lz.entity.tmsZone.vdsCamera') }}</span>
		</div>

		<el-empty v-if="groups.length === 0" :image-size="80" :description="$t('lz.message.tmsZone.noKmZones')" />

		<div v-for="group in groups" :key="group.key" class="strip-group">
			<div class="strip-head">
				<span class="strip-title">{{ group.label }}</span>
				<span class="strip-sub">{{ formatKm(group.from) }} → {{ formatKm(group.to) }} · {{ $t('lz.label.tmsZone.zoneCount', { count: group.items.length }) }}</span>
			</div>
			<div class="strip-track" :style="{ height: `${group.lanes * LANE_H + CAM_H + 6}px` }">
				<div
					v-for="item in group.items"
					:key="item.row.id"
					class="strip-block"
					:class="{
						'is-selected': item.selected,
						'is-default': item.eff.source === 'direction',
						'is-invalid': !item.eff.valid,
						'is-dirty': item.dirty,
					}"
					:style="blockStyle(group, item)"
					@click="emits('toggle', item.row.id)"
					@mouseenter="showTip($event, item)"
					@mousemove="moveTip($event)"
					@mouseleave="tip.visible = false"
				>
					<span v-if="item.widthPct > 7" class="block-label" :style="{ color: labelInk(item) }">
						{{ item.eff.valid ? `${item.eff.min}–${item.eff.max}` : '—' }}
					</span>
				</div>
				<div
					v-for="cam in group.cameras"
					:key="cam.id"
					class="cam-tick"
					:style="{ left: `${pct(group, cam.pos)}%`, top: `${group.lanes * LANE_H + 2}px` }"
					:title="`${cam.code ?? cam.name ?? 'VDS'} · ${formatKm(cam.pos)}`"
				/>
			</div>
			<div class="strip-axis">
				<span v-for="tick in group.ticks" :key="tick" class="axis-tick" :style="{ left: `${pct(group, tick)}%` }">{{ formatKm(tick) }}</span>
			</div>
		</div>

		<div v-if="tip.visible && tip.item" class="strip-tip" :style="{ left: `${tip.x + 12}px`, top: `${tip.y + 12}px` }">
			<div class="tip-title">{{ tip.item.row.name || tip.item.row.code }}</div>
			<div class="tip-row"><span>{{ $t('lz.entity.tms.km') }}</span><b>{{ formatKm(tip.item.span[0]) }} – {{ formatKm(tip.item.span[1]) }}</b></div>
			<div class="tip-row"><span>{{ $t('lz.entity.tmsZone.effectiveSpeed') }}</span><b>{{ tip.item.eff.valid ? `${tip.item.eff.min}–${tip.item.eff.max} km/h` : $t('lz.label.tmsZone.unusable') }}</b></div>
			<div class="tip-row"><span>{{ $t('lz.entity.tmsZone.speedSource') }}</span><b>{{ $t(SPEED_SOURCE_I18N[tip.item.eff.source]) }}</b></div>
			<div class="tip-row"><span>{{ $t('lz.entity.tmsZone.vdsCamera') }}</span><b>{{ tip.item.row.vdsCameras?.length ? tip.item.row.vdsCameras.map(c => c.code ?? c.name).join(', ') : '—' }}</b></div>
			<div v-if="tip.item.dirty" class="tip-dirty">{{ $t('lz.label.tmsZone.onlyDirty') }}</div>
			<div class="tip-hint">{{ $t('lz.label.tmsZone.clickToToggle') }}</div>
		</div>
	</div>
</template>

<script lang="ts" setup name="tmsZoneSpeedStrip">
// Author: KienLT
// Description: Dải trực quan theo Km — mỗi hướng một thanh, mỗi zone một khối tô theo Max hiệu lực.
// Created date: 08/10/2026
//
// Màu: một sắc primary, nhạt → đậm theo Max (ordinal, 5 bậc). Định danh KHÔNG chỉ bằng màu: nhãn min–max trên
// khối đủ rộng, viền đứt = đang dùng mặc định hướng, sọc = chưa có/lệch ngưỡng, tooltip đầy đủ khi rê chuột.
import { computed, reactive } from 'vue';
import { SPEED_BINS as BINS, SPEED_SOURCE_I18N, formatKm, speedBinOf as binOf, type EffectiveSpeed, type ZoneSpeedRow } from '/@/services/tms/zone-speed';

export interface StripItem {
	row: ZoneSpeedRow;
	eff: EffectiveSpeed;
	span: [number, number];
	selected: boolean;
	dirty: boolean;
	direction: string | null;
}

interface Props {
	items: StripItem[];
	directionLabel: (direction: string | null) => string;
}
const props = defineProps<Props>();

interface Emits {
	(e: 'toggle', id: string): void;
}
const emits = defineEmits<Emits>();

const LANE_H = 30;
const CAM_H = 10;

/** Khoảng chia trục "đẹp" (1, 2, 5 × 10^n Km) cho khoảng ~6 vạch. */
const niceStep = (spanMeters: number) => {
	const raw = spanMeters / 6;
	const pow = 10 ** Math.floor(Math.log10(Math.max(raw, 1)));
	const step = [1, 2, 5, 10].map(m => m * pow).find(s => s >= raw) ?? 10 * pow;
	return Math.max(step, 100);
};

const groups = computed(() => {
	const byDirection = new Map<string, StripItem[]>();
	for (const item of props.items) {
		const key = item.direction ?? '—';
		if (!byDirection.has(key)) byDirection.set(key, []);
		byDirection.get(key)!.push(item);
	}

	return [...byDirection.entries()]
		.sort(([a], [b]) => a.localeCompare(b))
		.map(([key, list]) => {
			const sorted = [...list].sort((a, b) => a.span[0] - b.span[0] || a.span[1] - b.span[1]);
			const from = Math.min(...sorted.map(i => i.span[0]));
			const to = Math.max(...sorted.map(i => i.span[1]));
			const total = Math.max(to - from, 1);

			// Xếp làn tham lam: zone chồng khoảng Km lên nhau xuống làn dưới thay vì đè lên nhau.
			const laneEnds: number[] = [];
			const items = sorted.map(item => {
				let lane = laneEnds.findIndex(end => end <= item.span[0]);
				if (lane < 0) { lane = laneEnds.length; laneEnds.push(item.span[1]); }
				else laneEnds[lane] = item.span[1];
				return { ...item, lane, widthPct: ((item.span[1] - item.span[0]) / total) * 100 };
			});

			const cameraMap = new Map<string, { id: string; code?: string | null; name?: string | null; pos: number }>();
			for (const item of sorted)
				for (const cam of item.row.vdsCameras ?? [])
					if (cam.kmNumber != null && !cameraMap.has(cam.id))
						cameraMap.set(cam.id, { id: cam.id, code: cam.code, name: cam.name, pos: cam.kmNumber * 1000 + (cam.metNumber ?? 0) });

			const step = niceStep(total);
			const ticks: number[] = [];
			for (let t = Math.ceil(from / step) * step; t <= to; t += step) ticks.push(t);

			return {
				key, label: props.directionLabel(key === '—' ? null : key),
				from, to, total, items, lanes: Math.max(laneEnds.length, 1), ticks,
				cameras: [...cameraMap.values()].filter(c => c.pos >= from && c.pos <= to),
			};
		});
});

type Group = (typeof groups.value)[number];
type GroupItem = Group['items'][number];

const pct = (group: Group, meters: number) => ((meters - group.from) / group.total) * 100;

const blockStyle = (group: Group, item: GroupItem) => {
	const bin = binOf(item.eff);
	return {
		left: `${pct(group, item.span[0])}%`,
		width: `max(${item.widthPct}%, 4px)`,
		top: `${item.lane * LANE_H}px`,
		height: `${LANE_H - 6}px`,
		background: bin?.color,
	};
};

const labelInk = (item: GroupItem) => binOf(item.eff)?.ink ?? 'var(--el-text-color-secondary)';

const tip = reactive({ visible: false, x: 0, y: 0, item: null as GroupItem | null });
const showTip = (event: MouseEvent, item: GroupItem) => { tip.item = item; tip.visible = true; moveTip(event); };
const moveTip = (event: MouseEvent) => { tip.x = event.clientX; tip.y = event.clientY; };
</script>

<style scoped lang="scss">
.speed-strip {
	display: flex;
	flex-direction: column;
	gap: 14px;
}

.strip-legend {
	display: flex;
	flex-wrap: wrap;
	align-items: center;
	gap: 6px 14px;
	font-size: 12px;
	color: var(--el-text-color-regular);

	.legend-title {
		color: var(--el-text-color-secondary);
	}

	.legend-item {
		display: inline-flex;
		align-items: center;
		gap: 5px;
	}

	.swatch {
		display: inline-block;
		width: 16px;
		height: 10px;
		border-radius: 3px;
	}
}

.swatch--invalid,
.strip-block.is-invalid {
	background: repeating-linear-gradient(135deg, var(--el-fill-color-darker) 0 4px, var(--el-fill-color-light) 4px 8px) !important;
}

.swatch--default {
	background: var(--el-bg-color);
	border: 1.5px dashed var(--el-text-color-secondary);
}

.cam-tick-legend {
	display: inline-block;
	width: 2px;
	height: 10px;
	background: var(--el-color-danger);
}

.strip-group {
	padding: 8px 10px 4px;
	border: 1px solid var(--el-border-color-lighter);
	border-radius: 8px;
	background: var(--el-bg-color);
}

.strip-head {
	display: flex;
	align-items: baseline;
	gap: 10px;
	margin-bottom: 6px;

	.strip-title {
		font-weight: 600;
		color: var(--el-text-color-primary);
	}

	.strip-sub {
		font-size: 12px;
		color: var(--el-text-color-secondary);
	}
}

.strip-track {
	position: relative;
	margin: 0 4px;
}

.strip-block {
	position: absolute;
	box-sizing: border-box;
	border-radius: 4px;
	/* Khe 2px màu nền giữa các khối liền kề để ranh giới zone đọc được. */
	box-shadow: 0 0 0 1px var(--el-bg-color);
	cursor: pointer;
	display: flex;
	align-items: center;
	justify-content: center;
	overflow: hidden;
	transition: box-shadow 0.12s ease, transform 0.12s ease;

	&:hover {
		transform: translateY(-1px);
		box-shadow: 0 0 0 1px var(--el-bg-color), var(--el-box-shadow-light);
	}

	&.is-default {
		border: 1.5px dashed var(--el-bg-color);
	}

	&.is-selected {
		/* Viền chọn màu chữ đậm (không vàng): khối đã tô các sắc primary nên viền primary sẽ lẫn với bậc > 100. */
		box-shadow: 0 0 0 2px var(--el-bg-color), 0 0 0 4px var(--el-text-color-primary);
		z-index: 2;
	}

	&.is-dirty::after {
		content: '';
		position: absolute;
		top: 3px;
		right: 3px;
		width: 6px;
		height: 6px;
		border-radius: 50%;
		background: var(--el-color-warning);
		box-shadow: 0 0 0 1.5px var(--el-bg-color);
	}

	.block-label {
		font-size: 11px;
		font-weight: 600;
		white-space: nowrap;
		pointer-events: none;
	}
}

.cam-tick {
	position: absolute;
	width: 2px;
	height: 10px;
	margin-left: -1px;
	background: var(--el-color-danger);
	border-radius: 1px;
}

.strip-axis {
	position: relative;
	height: 18px;
	margin: 0 4px;
	border-top: 1px solid var(--el-border-color-lighter);

	.axis-tick {
		position: absolute;
		top: 2px;
		transform: translateX(-50%);
		font-size: 10.5px;
		color: var(--el-text-color-secondary);
		white-space: nowrap;
	}
}

.strip-tip {
	position: fixed;
	z-index: 3000;
	min-width: 220px;
	padding: 8px 10px;
	border-radius: 6px;
	background: var(--el-bg-color-overlay);
	box-shadow: var(--el-box-shadow);
	font-size: 12px;
	color: var(--el-text-color-regular);
	pointer-events: none;

	.tip-title {
		font-weight: 600;
		margin-bottom: 4px;
		color: var(--el-text-color-primary);
	}

	.tip-row {
		display: flex;
		justify-content: space-between;
		gap: 12px;

		b {
			color: var(--el-text-color-primary);
			font-weight: 600;
		}
	}

	.tip-dirty {
		margin-top: 4px;
		color: var(--el-color-warning);
	}

	.tip-hint {
		margin-top: 4px;
		color: var(--el-text-color-secondary);
	}
}
</style>
