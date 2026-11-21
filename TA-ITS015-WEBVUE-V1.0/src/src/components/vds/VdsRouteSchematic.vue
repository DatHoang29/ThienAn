<!-- Sơ đồ VDS theo tỷ lệ mét; tô sáng từng khúc xe đi qua và chỉ hiện camera đúng hướng. -->
<template>
	<div class="vds-route-schematic">
		<el-alert v-for="(warning, index) in topology.warnings ?? []" :key="`warning-${index}`" :title="warning" type="warning" :closable="false" class="mb5" />
		<el-alert
			v-for="item in unavailable"
			:key="`unavailable-${item.index}`"
			type="warning"
			:closable="false"
			class="mb5"
			:title="t('lz.message.vdsRoute.unavailableJourney', { number: item.index + 1, segment: item.journey.segmentId || '—' })"
		/>
		<svg v-if="scene" class="route-schematic" :viewBox="scene.viewBox" role="group" :aria-label="t('lz.label.vdsRoute.title')">
			<title>{{ t('lz.label.vdsRoute.title') }}</title>
			<defs>
				<!-- userSpaceOnUse: khúc nằm ngang/dọc có khung bao cao (rộng) 0 ⇒ filter theo objectBoundingBox làm mất nét. -->
				<filter :id="`${uid}-glow`" filterUnits="userSpaceOnUse" :x="scene.box.x" :y="scene.box.y" :width="scene.box.width" :height="scene.box.height">
					<feGaussianBlur stdDeviation="4" result="blur" />
					<feMerge>
						<feMergeNode in="blur" />
						<feMergeNode in="SourceGraphic" />
					</feMerge>
				</filter>
			</defs>
			<rect class="backdrop" :x="scene.box.x" :y="scene.box.y" :width="scene.box.width" :height="scene.box.height" rx="10" />

			<!-- Từng khúc của nhánh: xám khi xe chưa đi qua, sáng theo trạng thái hành trình khi đã đi qua. -->
			<line
				v-for="piece in scene.pieces"
				:key="piece.key"
				class="piece"
				:class="piece.lit ? ['lit', piece.lit.state, { visited: !piece.lit.active }] : undefined"
				:x1="piece.a.x"
				:y1="piece.a.y"
				:x2="piece.b.x"
				:y2="piece.b.y"
				:filter="piece.lit?.active ? `url(#${uid}-glow)` : undefined"
			/>
			<line
				v-for="piece in scene.pieces.filter((item) => item.lit?.active && item.lit.state !== 'invalid')"
				:key="`dots-${piece.key}`"
				class="piece-dots"
				:x1="piece.a.x"
				:y1="piece.a.y"
				:x2="piece.b.x"
				:y2="piece.b.y"
			/>

			<g v-for="junction in scene.junctions" :key="junction.key" class="junction">
				<circle :cx="junction.point.x" :cy="junction.point.y" r="10" />
				<circle :cx="junction.point.x" :cy="junction.point.y" r="4" class="junction-core" />
			</g>
			<g v-for="camera in scene.cameras" :key="camera.key" class="camera" :class="{ endpoint: camera.endpoint }">
				<circle :cx="camera.point.x" :cy="camera.point.y" :r="camera.endpoint ? 8 : 5" />
				<title>{{ camera.title }}</title>
			</g>
			<path v-for="arrow in scene.arrows" :key="arrow.key" class="arrow" :class="[arrow.state, { active: arrow.active }]" :d="arrow.d" />

			<!-- Vùng bấm theo từng hành trình (trong suốt), dùng cho sơ đồ theo xe. -->
			<polyline
				v-for="item in scene.groups"
				:key="`hit-${item.key}`"
				class="hit-target"
				:points="item.points"
				role="button"
				tabindex="0"
				:aria-label="t('lz.button.vdsJourney.select', { number: item.index + 1 })"
				@click="emit('select', item.journey)"
				@keydown.enter.prevent="emit('select', item.journey)"
				@keydown.space.prevent="emit('select', item.journey)"
			>
				<title>{{ item.title }}</title>
			</polyline>

			<g
				v-for="label in scene.labels"
				:key="label.key"
				class="label"
				:class="[label.kind, { selectable: label.journey }]"
				:role="label.journey ? 'button' : undefined"
				:tabindex="label.journey ? 0 : undefined"
				:aria-label="label.journey ? label.text : undefined"
				@click="label.journey && emit('select', label.journey)"
				@keydown.enter.prevent="label.journey && emit('select', label.journey)"
				@keydown.space.prevent="label.journey && emit('select', label.journey)"
			>
				<rect v-if="label.chip" :x="label.x - label.width / 2" :y="label.y - 11" :width="label.width" height="22" rx="11" :class="label.chip" />
				<text :x="label.x" :y="label.y" text-anchor="middle" dominant-baseline="central">{{ label.text }}</text>
			</g>
		</svg>
		<el-empty v-else :description="t('lz.message.vdsRoute.unavailable')" />
		<div class="schematic-footer">
			<el-text v-if="activeSummary" tag="b">{{ activeSummary }}</el-text>
			<span class="spacer" />
			<el-space wrap>
				<el-tag type="success">{{ t('lz.label.base.valid') }}</el-tag>
				<el-tag type="warning">{{ t('lz.label.vdsJourney.overSpeed') }}</el-tag>
				<el-tag type="info">{{ t('lz.label.base.invalid') }}</el-tag>
				<el-tag type="info">{{ t('lz.label.base.unknown') }}</el-tag>
			</el-space>
		</div>
	</div>
</template>

<script setup lang="ts" name="vdsRouteSchematic">
import { computed, useId } from 'vue';
import { useI18n } from 'vue-i18n';
import type { PageJourneyOutput, SegmentTopologyOutput } from '/@/api-services/vds';

const props = defineProps<{
	topology: SegmentTopologyOutput;
	journeys: PageJourneyOutput[];
	activeId?: string;
	trip?: boolean;
	directionNames?: Readonly<Record<string, string>>;
}>();
const emit = defineEmits<{ select: [journey: PageJourneyOutput] }>();
const { t, locale } = useI18n();
const uid = `vds-route-${useId().replace(/[^a-zA-Z0-9_-]/g, '')}`;

type Point = { x: number; y: number };
type Route = NonNullable<SegmentTopologyOutput['routes']>[number];
type Camera = NonNullable<SegmentTopologyOutput['cameras']>[number];
type Segment = NonNullable<SegmentTopologyOutput['segments']>[number];
type State = 'valid' | 'invalid' | 'unknown' | 'overspeed';
/** Một đoạn liên tục của hành trình trên một nhánh, tính bằng mét. */
type Leg = { route: string; lo: number; hi: number };

/** Khung vẽ (px) cho phần đường; nhãn được đặt quanh khung này. */
const DRAW_WIDTH = 940;
const DRAW_MAX_HEIGHT = 460;
/** Khe hở giữa hai khúc liền nhau (px) để thấy rõ từng khúc. */
const PIECE_GAP = 4;

const km = (pos?: number | null) => (pos == null ? '—' : t('lz.label.vdsRoute.km', { value: `${Math.floor(pos / 1000)}+${String(Math.abs(pos % 1000)).padStart(3, '0')}` }));
const formatNumber = (value?: number | null) => (value == null ? '—' : new Intl.NumberFormat(locale.value, { maximumFractionDigits: 1 }).format(value));
const summary = (journey: PageJourneyOutput) =>
	t('lz.label.vdsRoute.metrics', {
		distance: formatNumber(journey.distanceMeter == null ? null : journey.distanceMeter / 1000),
		seconds: formatNumber(journey.travelSeconds),
		speed: formatNumber(journey.avgSpeedKmh),
	});
const stateOf = (journey: PageJourneyOutput): State => (journey.isValid === false ? 'invalid' : journey.isOverSpeed ? 'overspeed' : journey.isValid === true ? 'valid' : 'unknown');
const directionCode = (value?: string | number | null) => (value == null ? '' : String(value).trim());
const directionName = (code?: string) => (code && props.directionNames?.[code]) || code || '';

// Mảng Topology có kiểu rõ ràng (tránh `?? []` làm mất kiểu phần tử).
type Junction = NonNullable<SegmentTopologyOutput['junctions']>[number];
const topoRoutes = computed<Route[]>(() => props.topology.routes ?? []);
const topoJunctions = computed<Junction[]>(() => props.topology.junctions ?? []);
const topoCameras = computed<Camera[]>(() => props.topology.cameras ?? []);
const topoSegments = computed<Segment[]>(() => props.topology.segments ?? []);
const cameraById = computed(() => new Map<string, Camera>(topoCameras.value.filter((c) => c.id).map((c) => [c.id!.toLowerCase(), c])));

/** Hành trình ⇒ đoạn Topology, camera A/B và các chặng (leg) theo từng nhánh. */
const resolved = computed(() => {
	const segments = new Map<string, Segment>(topoSegments.value.filter((s) => s.id).map((s) => [s.id!, s]));
	const cameras = cameraById.value;
	const journeys: PageJourneyOutput[] = props.journeys;
	return journeys.map((journey, index) => {
		const segment = segments.get(journey.segmentId ?? '');
		const from = cameras.get((journey.fromEquipmentId || segment?.fromEquipmentId || '').toLowerCase());
		const to = cameras.get((journey.toEquipmentId || segment?.toEquipmentId || '').toLowerCase());
		const path = segment && ['S', 'T'].includes(segment.kind ?? '') ? (segment.path ?? []) : [];
		const legs: Leg[] = [];
		for (let i = 1; i < path.length; i++) {
			const a = path[i - 1];
			const b = path[i];
			if (a.route && a.route === b.route && a.pos != null && b.pos != null && a.pos !== b.pos) legs.push({ route: a.route, lo: Math.min(a.pos, b.pos), hi: Math.max(a.pos, b.pos) });
		}
		return {
			journey,
			index,
			segment,
			from,
			to,
			path: legs.length ? path : [],
			legs,
			state: stateOf(journey),
			fromDirection: directionCode(from?.direction ?? journey.direction),
			toDirection: directionCode(to?.direction ?? journey.direction),
		};
	});
});
const unavailable = computed(() => resolved.value.filter((item) => !item.legs.length));
const active = computed(
	() =>
		resolved.value.find((item) => item.legs.length && item.journey.id && item.journey.id === props.activeId) ??
		(props.journeys.length === 1 ? resolved.value.find((item) => item.legs.length) : undefined)
);
const activeSummary = computed(() => (active.value ? summary(active.value.journey) : ''));

/** Giải gốc toạ độ (mét) của từng nhánh: điểm neo của nút giao trên hai nhánh phải trùng nhau. */
const geometry = computed(() => {
	const routes = topoRoutes.value.filter((r) => r.code && ['h', 'v'].includes(r.axis ?? '') && Number.isFinite(r.minPos) && Number.isFinite(r.maxPos));
	if (!routes.length) return undefined;
	const byCode = new Map<string, Route>(routes.map((r) => [r.code!, r]));
	// Nhánh ngang: Km tăng sang TRÁI (Km0 phía Hữu Nghị bên phải); nhánh dọc: Km tăng lên trên.
	const local = (code: string, pos: number): Point => (byCode.get(code)?.axis === 'v' ? { x: 0, y: -pos } : { x: -pos, y: 0 });
	const junctions = topoJunctions.value.filter((j) => byCode.has(j.routeA ?? '') && byCode.has(j.routeB ?? '') && j.posA != null && j.posB != null);
	const origins = new Map<string, Point>();
	let nextX = 0;
	for (const seed of routes) {
		if (origins.has(seed.code!)) continue;
		origins.set(seed.code!, { x: 0, y: 0 });
		const group = [seed.code!];
		for (let i = 0; i < group.length; i++)
			for (const j of junctions) {
				const code = group[i];
				if (j.routeA !== code && j.routeB !== code) continue;
				const other = (j.routeA === code ? j.routeB : j.routeA)!;
				if (origins.has(other)) continue;
				const here = local(code, (j.routeA === code ? j.posA : j.posB)!);
				const there = local(other, (j.routeA === code ? j.posB : j.posA)!);
				const origin = origins.get(code)!;
				origins.set(other, { x: origin.x + here.x - there.x, y: origin.y + here.y - there.y });
				group.push(other);
			}
		const xs = group.flatMap((code) => [byCode.get(code)!.minPos!, byCode.get(code)!.maxPos!].map((pos) => origins.get(code)!.x + local(code, pos).x));
		const shift = nextX - Math.min(...xs);
		for (const code of group) origins.get(code)!.x += shift;
		nextX += Math.max(...xs) - Math.min(...xs) + 2000;
	}
	const metre = (code: string, pos: number): Point => {
		const origin = origins.get(code)!;
		const delta = local(code, pos);
		return { x: origin.x + delta.x, y: origin.y + delta.y };
	};
	const corners = routes.flatMap((r) => [metre(r.code!, r.minPos!), metre(r.code!, r.maxPos!)]);
	const minX = Math.min(...corners.map((p) => p.x));
	const minY = Math.min(...corners.map((p) => p.y));
	const width = Math.max(1, Math.max(...corners.map((p) => p.x)) - minX);
	const height = Math.max(1, Math.max(...corners.map((p) => p.y)) - minY);
	const scale = Math.min(DRAW_WIDTH / width, DRAW_MAX_HEIGHT / height);
	const project = (code?: string | null, pos?: number | null): Point | undefined =>
		code && origins.has(code) && pos != null ? { x: (metre(code, pos).x - minX) * scale, y: (metre(code, pos).y - minY) * scale } : undefined;
	return { routes, junctions, project };
});

/** Hướng hiển thị của từng nhánh: theo hành trình đang chọn, rồi theo đa số hành trình, rồi theo lượt rẽ nối tiếp. */
const routeDirections = computed(() => {
	const directions = new Map<string, string>();
	const take = (item?: (typeof resolved.value)[number]) => {
		if (!item) return;
		if (item.from?.route && item.fromDirection && !directions.has(item.from.route)) directions.set(item.from.route, item.fromDirection);
		if (item.to?.route && item.toDirection && !directions.has(item.to.route)) directions.set(item.to.route, item.toDirection);
	};
	take(active.value);
	const votes = new Map<string, Map<string, number>>();
	for (const item of resolved.value.filter((r) => r.legs.length))
		for (const [route, dir] of [
			[item.from?.route, item.fromDirection],
			[item.to?.route, item.toDirection],
		] as const) {
			if (!route || !dir) continue;
			const counter = votes.get(route) ?? new Map<string, number>();
			counter.set(dir, (counter.get(dir) ?? 0) + 1);
			votes.set(route, counter);
		}
	for (const [route, counter] of votes) if (!directions.has(route)) directions.set(route, [...counter].sort((a, b) => b[1] - a[1])[0][0]);
	// Nhánh xe không đi qua: lấy hướng nối tiếp duy nhất của các lượt rẽ từ hướng đang xem.
	const cameras = cameraById.value;
	for (const route of geometry.value?.routes ?? [])
		if (!directions.has(route.code!)) {
			const candidates = new Set<string>();
			for (const segment of topoSegments.value.filter((s) => s.kind === 'T')) {
				const from = cameras.get(segment.fromEquipmentId?.toLowerCase() ?? '');
				const to = cameras.get(segment.toEquipmentId?.toLowerCase() ?? '');
				if (!from?.route || !to || to.route !== route.code) continue;
				if (directions.get(from.route) === directionCode(from.direction)) candidates.add(directionCode(to.direction));
			}
			if (candidates.size === 1) directions.set(route.code!, [...candidates][0]);
		}
	return directions;
});

/** Cụm nhãn giao nhau với hộp nào đã chiếm thì thử vị trí kế tiếp. */
type Box = { x: number; y: number; width: number; height: number };
const overlaps = (a: Box, b: Box) => Math.abs(a.x - b.x) < (a.width + b.width) / 2 + 2 && Math.abs(a.y - b.y) < (a.height + b.height) / 2 + 2;
const textWidth = (text: string, size = 13) => Math.max(20, Array.from(text).length * size * 0.6 + 14);

const scene = computed(() => {
	const geo = geometry.value;
	if (!geo) return undefined;
	const directions = routeDirections.value;
	const activeItem = active.value;
	const startItem = props.trip ? resolved.value[0] : activeItem;
	const endItem = props.trip ? resolved.value[resolved.value.length - 1] : activeItem;
	const drawnItems = resolved.value.filter((item) => item.legs.length);
	const cameraDirection = (camera: Camera) => directionCode(camera.direction);

	// Camera hiển thị: đúng hướng của nhánh; luôn giữ camera A/B của hành trình đang chọn.
	const endpointIds = new Set([startItem?.from?.id, endItem?.to?.id, ...resolved.value.flatMap((item) => [item.from?.id, item.to?.id])].filter(Boolean).map((id) => id!.toLowerCase()));
	const cameras = topoCameras.value.filter(
		(camera) => camera.route && camera.pos != null && (endpointIds.has(camera.id?.toLowerCase() ?? '') || (directions.has(camera.route) && cameraDirection(camera) === directions.get(camera.route)))
	);

	// Nút của từng nhánh = hai đầu tuyến + điểm neo nút giao + camera đang hiện ⇒ các khúc liên tiếp.
	const pieces: { key: string; route: string; lo: number; hi: number; a: Point; b: Point; lit?: { state: State; active: boolean } }[] = [];
	for (const route of geo.routes) {
		const nodes = new Set<number>([route.minPos!, route.maxPos!]);
		for (const j of geo.junctions) {
			if (j.routeA === route.code) nodes.add(j.posA!);
			if (j.routeB === route.code) nodes.add(j.posB!);
		}
		for (const camera of cameras) if (camera.route === route.code) nodes.add(camera.pos!);
		const sorted = [...nodes].sort((a, b) => a - b);
		for (let i = 1; i < sorted.length; i++) {
			const lo = sorted[i - 1];
			const hi = sorted[i];
			const p = geo.project(route.code, lo)!;
			const q = geo.project(route.code, hi)!;
			const length = Math.hypot(q.x - p.x, q.y - p.y);
			const gap = length > PIECE_GAP * 3 ? PIECE_GAP / 2 : 0;
			const ux = (q.x - p.x) / (length || 1);
			const uy = (q.y - p.y) / (length || 1);
			pieces.push({ key: `${route.code}:${lo}-${hi}`, route: route.code!, lo, hi, a: { x: p.x + ux * gap, y: p.y + uy * gap }, b: { x: q.x - ux * gap, y: q.y - uy * gap } });
		}
	}
	// Khúc sáng: thuộc chặng của hành trình đang chọn (sáng rõ) hoặc của hành trình khác (sáng nhạt).
	const covers = (leg: Leg, piece: { route: string; lo: number; hi: number }) => leg.route === piece.route && Math.min(leg.hi, piece.hi) - Math.max(leg.lo, piece.lo) > 1;
	for (const piece of pieces) {
		if (activeItem?.legs.some((leg) => covers(leg, piece))) piece.lit = { state: activeItem.state, active: true };
		else {
			const other = drawnItems.find((item) => item.legs.some((leg) => covers(leg, piece)));
			if (other) piece.lit = { state: other.state, active: props.trip ? !activeItem : !activeItem && props.journeys.length === 1 };
		}
	}

	// Gom các hành trình cùng đường đi + cùng trạng thái (sơ đồ theo xe) để đánh số "n · ×k".
	const groupMap = new Map<string, (typeof drawnItems)[number][]>();
	for (const item of drawnItems) {
		const key = `${props.trip ? item.index : ''}|${item.path.map((p) => `${p.route}:${p.pos}`).join('>')}|${item.state}`;
		groupMap.set(key, [...(groupMap.get(key) ?? []), item]);
	}
	const groups = [...groupMap].map(([key, members]) => {
		const head = members.find((m) => m === activeItem) ?? members[0];
		const vertices = head.path.map((p) => geo.project(p.route, p.pos)!).filter(Boolean);
		return {
			key,
			members,
			journey: head.journey,
			index: head.index,
			state: head.state,
			vertices,
			isActive: head === activeItem,
			points: vertices.map((v) => `${v.x},${v.y}`).join(' '),
			title: t('lz.label.vdsRoute.groupJourneys', { numbers: members.map((m) => m.index + 1).join(', ') }) + ' · ' + summary(head.journey),
		};
	});

	// Mũi tên to tại điểm B, theo hướng chặng cuối — chỉ cho hành trình đang chọn (hoặc khi chỉ có một hành trình).
	const arrows = groups
		.filter((group) => (props.trip ? group.journey === endItem?.journey : group.isActive || groups.length === 1))
		.map((group) => {
			const tip = group.vertices[group.vertices.length - 1];
			const prev = group.vertices[group.vertices.length - 2];
			const length = Math.hypot(tip.x - prev.x, tip.y - prev.y) || 1;
			const ux = (tip.x - prev.x) / length;
			const uy = (tip.y - prev.y) / length;
			const size = group.isActive || groups.length === 1 ? 30 : 20;
			const base = { x: tip.x - ux * size, y: tip.y - uy * size };
			const half = size * 0.55;
			const d = `M ${tip.x + ux * 4} ${tip.y + uy * 4} L ${base.x - uy * half} ${base.y + ux * half} L ${base.x + uy * half} ${base.y - ux * half} Z`;
			return { key: group.key, d, state: group.state, active: group.isActive || groups.length === 1 };
		});

	// ── Nhãn ──
	type Label = Box & { key: string; text: string; kind: string; chip?: string; journey?: PageJourneyOutput };
	const labels: Label[] = [];
	const occupied: Box[] = [
		...pieces.map((p) => ({ x: (p.a.x + p.b.x) / 2, y: (p.a.y + p.b.y) / 2, width: Math.abs(p.b.x - p.a.x) + 14, height: Math.abs(p.b.y - p.a.y) + 14 })),
		...cameras.map((c) => ({ ...geo.project(c.route, c.pos)!, width: 16, height: 16 })),
	];
	const put = (key: string, text: string, kind: string, candidates: Point[], size = 13, chip?: string, journey?: PageJourneyOutput) => {
		const width = textWidth(text, size) + (chip ? 8 : 0);
		const height = chip ? 22 : size + 8;
		// Vị trí đầu tiên không đè; nếu vị trí nào cũng đè thì lấy vị trí đè ÍT nhất (không lấy mặc định ô đầu).
		const hits = (c: Point) => occupied.filter((box) => overlaps({ ...c, width, height }, box)).length;
		const spot = candidates.find((c) => hits(c) === 0) ?? [...candidates].sort((a, b) => hits(a) - hits(b))[0];
		const box = { ...spot, width, height };
		occupied.push(box);
		labels.push({ ...box, key, text, kind, chip, journey });
	};

	// Chip A/B sát camera đầu/cuối của hành trình đang chọn.
	for (const [end, camera] of [
		['start', startItem?.from],
		['end', endItem?.to],
	] as const) {
		const point = camera ? geo.project(camera.route, camera.pos) : undefined;
		if (!point) continue;
		put(
			`chip-${end}`,
			t(`lz.label.vdsRoute.${end}`),
			'endpoint',
			[
				{ x: point.x, y: point.y - 26 },
				{ x: point.x, y: point.y + 26 },
				{ x: point.x - 26, y: point.y },
				{ x: point.x + 26, y: point.y },
				{ x: point.x - 26, y: point.y - 26 },
				{ x: point.x + 26, y: point.y + 26 },
			],
			13,
			(end === 'start' ? startItem : endItem)?.state
		);
	}
	// Nhãn nút giao đặt sớm (trước nhãn camera) ở góc ngoài chữ L.
	for (const junction of geo.junctions) {
		const point = geo.project(junction.routeA, junction.posA)!;
		const text = t('lz.label.vdsRoute.junction', { code: junction.code });
		const w = textWidth(text);
		put(`junction-${junction.code}`, text, 'junction-label', [
			{ x: point.x + w / 2 + 18, y: point.y + 26 },
			{ x: point.x - w / 2 - 18, y: point.y + 26 },
			{ x: point.x + w / 2 + 18, y: point.y - 24 },
			{ x: point.x - w / 2 - 18, y: point.y - 24 },
			{ x: point.x, y: point.y + 46 },
		]);
	}
	// Số thứ tự (sơ đồ theo xe): giữa chặng dài nhất, lệch vuông góc vài px.
	if (props.trip || props.journeys.length > 1)
		for (const group of groups) {
			const legs = group.vertices.slice(1).map((end, i) => ({ start: group.vertices[i], end }));
			const longest = legs.sort((a, b) => Math.hypot(b.end.x - b.start.x, b.end.y - b.start.y) - Math.hypot(a.end.x - a.start.x, a.end.y - a.start.y))[0];
			if (!longest) continue;
			const horizontal = Math.abs(longest.end.y - longest.start.y) < 1;
			const candidates = [0.5, 0.35, 0.65, 0.2, 0.8].flatMap((f) =>
				[-1, 1].flatMap((side) =>
					[22, 40].map((gap) => {
						const x = longest.start.x + (longest.end.x - longest.start.x) * f;
						const y = longest.start.y + (longest.end.y - longest.start.y) * f;
						return horizontal ? { x, y: y + side * gap } : { x: x + side * (gap + 10), y };
					})
				)
			);
			const text = props.trip
				? t('lz.label.vdsJourney.speedChip', { speed: formatNumber(group.journey.avgSpeedKmh), limit: group.journey.speedLimitKmh == null ? '' : ` / ${formatNumber(group.journey.speedLimitKmh)}` })
				: group.members.length > 1
					? t('lz.label.vdsRoute.groupCount', { number: group.index + 1, count: group.members.length })
					: String(group.index + 1);
			const chipPositions = horizontal ? candidates : candidates.map((point) => ({ ...point, x: point.x + (Math.sign(point.x - longest.start.x) * textWidth(text, 12)) / 2 }));
			put(`badge-${group.key}`, text, 'badge', chipPositions, 12, group.state, group.journey);
		}
	// Tên nhánh + hướng ở đầu xa nút giao; Km ở hai đầu mút.
	for (const route of geo.routes) {
		const horizontal = route.axis === 'h';
		const minPoint = geo.project(route.code, route.minPos)!;
		const maxPoint = geo.project(route.code, route.maxPos)!;
		const anchors = geo.junctions.flatMap((j) => (j.routeA === route.code ? [geo.project(route.code, j.posA)!] : j.routeB === route.code ? [geo.project(route.code, j.posB)!] : []));
		const distance = (p: Point) => (anchors.length ? Math.min(...anchors.map((a) => Math.hypot(a.x - p.x, a.y - p.y))) : 0);
		const far = distance(minPoint) >= distance(maxPoint) ? minPoint : maxPoint;
		const direction = directionName(directions.get(route.code!));
		const title = direction ? t('lz.label.vdsRoute.carriageway', { route: route.name || route.code, direction }) : route.name || route.code!;
		const tw = textWidth(title, 14);
		put(
			`route-${route.code}`,
			title,
			'route-name',
			horizontal
				? [
						{ x: far.x + tw / 2, y: far.y + 34 },
						{ x: far.x + tw / 2, y: far.y - 34 },
					]
				: [
						{ x: far.x, y: far.y - 30 },
						{ x: far.x - tw / 2 - 22, y: far.y - 30 },
						{ x: far.x + tw / 2 + 22, y: far.y - 30 },
					],
			14
		);
		for (const [pos, point, other] of [
			[route.minPos!, minPoint, maxPoint],
			[route.maxPos!, maxPoint, minPoint],
		] as const) {
			// Đầu tuyến trùng điểm neo nút giao (CT.10 Km0) thì bỏ nhãn Km: chỉ gây rối tại nút.
			if (anchors.some((a) => Math.hypot(a.x - point.x, a.y - point.y) < 1)) continue;
			const length = Math.hypot(point.x - other.x, point.y - other.y) || 1;
			const ux = (point.x - other.x) / length;
			const uy = (point.y - other.y) / length;
			const text = km(pos);
			const w = textWidth(text);
			put(`km-${route.code}-${pos}`, text, 'km', [
				{ x: point.x + ux * (w / 2 + 16), y: point.y + uy * 20 },
				{ x: point.x + (horizontal ? 0 : w / 2 + 14), y: point.y + (horizontal ? 22 : 0) },
				{ x: point.x - (horizontal ? 0 : w / 2 + 14), y: point.y - (horizontal ? 22 : 0) },
			]);
		}
	}
	// Mã camera: xen kẽ hai bên nhánh khi các camera nằm sát nhau.
	for (const route of geo.routes) {
		const horizontal = route.axis === 'h';
		const onRoute = cameras
			.filter((c) => c.route === route.code)
			.map((c) => ({ camera: c, point: geo.project(c.route, c.pos)! }))
			.sort((a, b) => (horizontal ? a.point.x - b.point.x : a.point.y - b.point.y));
		let side = -1;
		onRoute.forEach(({ camera, point }, i) => {
			const prev = onRoute[i - 1];
			side = prev && (horizontal ? point.x - prev.point.x : point.y - prev.point.y) < 70 ? -side : -1;
			const text = camera.code || camera.name || '';
			const w = textWidth(text, 12);
			// Ưu tiên sát chấm camera; các vị trí dự phòng trượt nhẹ dọc nhánh thay vì kéo xa.
			const near = (s: number, gap: number, slide = 0): Point => (horizontal ? { x: point.x + slide, y: point.y + s * gap } : { x: point.x + s * (w / 2 + gap), y: point.y + slide });
			put(
				`camera-${camera.id}`,
				text,
				endpointIds.has(camera.id?.toLowerCase() ?? '') ? 'camera endpoint' : 'camera',
				[near(side, 22), near(-side, 22), near(side, 22, -30), near(side, 22, 30), near(-side, 22, -30), near(-side, 22, 30), near(side, 40), near(-side, 40)],
				12
			);
		});
	}

	const junctions = geo.junctions.map((j) => ({ key: `j-${j.code}`, point: geo.project(j.routeA, j.posA)! }));
	const cameraMarks = cameras.map((c) => ({
		key: `c-${c.id}`,
		point: geo.project(c.route, c.pos)!,
		endpoint: endpointIds.has(c.id?.toLowerCase() ?? ''),
		title: `${c.name || c.code} · ${km(c.pos)}`,
	}));

	const extents = [
		...pieces.flatMap((p) => [p.a, p.b]),
		...labels.flatMap((l) => [
			{ x: l.x - l.width / 2, y: l.y - l.height / 2 },
			{ x: l.x + l.width / 2, y: l.y + l.height / 2 },
		]),
	];
	const pad = 24;
	const minX = Math.min(...extents.map((p) => p.x)) - pad;
	const minY = Math.min(...extents.map((p) => p.y)) - pad;
	const maxX = Math.max(...extents.map((p) => p.x)) + pad;
	const maxY = Math.max(...extents.map((p) => p.y)) + pad;
	const box = { x: minX, y: minY, width: maxX - minX, height: maxY - minY };
	return { pieces, junctions, cameras: cameraMarks, arrows, groups, labels, box, viewBox: `${box.x} ${box.y} ${box.width} ${box.height}` };
});
</script>

<style scoped>
.route-schematic {
	display: block;
	width: 100%;
	height: auto;
	max-height: 560px;
	color: var(--el-text-color-primary);
}
.backdrop {
	fill: var(--el-fill-color-lighter);
	stroke: var(--el-border-color-lighter);
}
text {
	fill: currentColor;
	font-size: 13px;
	paint-order: stroke;
	stroke: var(--el-fill-color-lighter);
	stroke-width: 3px;
	stroke-linejoin: round;
}

/* Khúc đường: xám đậm khi chưa đi qua; sáng theo trạng thái khi xe đã đi qua. */
.piece {
	stroke: var(--el-text-color-placeholder);
	stroke-width: 12;
	stroke-linecap: round;
}
.piece.lit.valid {
	stroke: var(--el-color-success);
}
.piece.lit.invalid {
	stroke: var(--el-color-info);
	stroke-dasharray: 10 9;
}
.piece.lit.overspeed {
	stroke: var(--el-color-warning);
}
.piece.lit.unknown {
	stroke: var(--el-color-info);
}
.piece.lit {
	stroke-width: 14;
}
.piece.lit.visited {
	stroke-width: 12;
	opacity: 0.45;
}
.piece-dots {
	stroke: var(--el-color-white);
	stroke-width: 3;
	stroke-linecap: round;
	stroke-dasharray: 0.1 12;
	opacity: 0.9;
	pointer-events: none;
}

.junction circle {
	fill: var(--el-bg-color);
	stroke: var(--el-text-color-regular);
	stroke-width: 3;
}
.junction .junction-core {
	fill: var(--el-text-color-regular);
	stroke: none;
}
.camera circle {
	fill: var(--el-bg-color);
	stroke: var(--el-text-color-secondary);
	stroke-width: 2.5;
}
.camera.endpoint circle {
	stroke: var(--el-text-color-primary);
	stroke-width: 3.5;
}

.arrow {
	stroke: var(--el-bg-color);
	stroke-width: 2;
	stroke-linejoin: round;
}
.arrow.valid {
	fill: var(--el-color-success);
}
.arrow.invalid {
	fill: var(--el-color-info);
}
.arrow.overspeed {
	fill: var(--el-color-warning);
}
.arrow.unknown {
	fill: var(--el-color-info);
}
.arrow:not(.active) {
	opacity: 0.6;
}

.hit-target {
	fill: none;
	stroke: transparent;
	stroke-width: 24;
	pointer-events: stroke;
	cursor: pointer;
}
.hit-target:focus {
	outline: none;
	stroke: var(--el-color-primary-light-7);
}

.label.route-name text {
	font-size: 14px;
	font-weight: 700;
}
.label.km text {
	fill: var(--el-text-color-regular);
}
.label.camera text {
	font-size: 12px;
	fill: var(--el-text-color-secondary);
}
.label.camera.endpoint text {
	font-weight: 700;
	fill: var(--el-text-color-primary);
}
.label.junction-label text {
	fill: var(--el-text-color-regular);
	font-style: italic;
}
.label rect.valid {
	fill: var(--el-color-success);
}
.label rect.invalid {
	fill: var(--el-color-info);
}
.label rect.overspeed {
	fill: var(--el-color-warning);
}
.label.selectable {
	cursor: pointer;
}
.label.selectable:focus {
	outline: 2px solid var(--el-color-primary);
}
.label rect.unknown {
	fill: var(--el-color-info);
}
.label.endpoint text,
.label.badge text {
	fill: var(--el-color-white);
	stroke: none;
	font-weight: 700;
}
.label.badge text {
	font-size: 12px;
}

.schematic-footer {
	display: flex;
	align-items: center;
	gap: 8px;
	margin-top: 6px;
}
.schematic-footer .spacer {
	flex: 1;
}
</style>
