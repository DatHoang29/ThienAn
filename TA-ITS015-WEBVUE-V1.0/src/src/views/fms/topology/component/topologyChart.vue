<!--
  Sơ đồ mạng FMS dạng HÌNH SAO / HÌNH VÒNG (ECharts).
  - star : cây xoè tròn (radial tree), node gốc ở tâm, bấm node để thu/mở nhánh.
  - ring : mọi node xếp trên 1 vòng tròn theo thứ tự cây, đường cong nối đúng quan hệ cha - con.
  - Màu node theo trạng thái thiết bị; node không khớp từ khoá tìm kiếm bị làm mờ.
  - Kéo để di chuyển, cuộn chuột để phóng to/thu nhỏ (roam của ECharts).
  Author: ChuongLP
-->
<template>
	<div ref="chartRef" class="topology-chart"></div>
</template>

<script lang="ts" setup name="fmsTopologyChart">
import { onBeforeUnmount, onMounted, ref, watch } from 'vue';
import * as echarts from 'echarts';
import { useI18n } from 'vue-i18n';

export interface TopoNode {
	name: string;
	ip: string;
	equipmentName?: string | null; // tên thiết bị ITS khớp theo IP (null = ngoài ITS)
	equipmentState?: number | null;
	kmNumber?: number | null;
	metNumber?: number | null;
	children?: TopoNode[];
}

interface Props {
	roots: TopoNode[];
	layout: 'star' | 'ring';
	keyword?: string;
	stateFilter?: string[]; // khoá trạng thái đang lọc (rỗng = không lọc)
	stateKeyOf: (code?: number | null) => string; // khoá trạng thái của node, khớp với stateFilter
	rootName?: string; // tên node gốc ảo khi có nhiều gốc (chỉ dùng cho hình sao)
	colorOf: (code?: number | null) => string; // màu theo trạng thái, chấp nhận dạng var(--el-color-*)
	labelOf: (code?: number | null) => string; // tên trạng thái
	formatKmMet: (km?: number | null, met?: number | null) => string;
}
const props = withDefaults(defineProps<Props>(), {
	keyword: '',
	stateFilter: () => [],
	rootName: 'Network',
});

const { t: $t } = useI18n();
const emits = defineEmits<{ zoom: [value: number] }>();

// Giới hạn zoom & lề khung vẽ: dùng chung cho hình sao / vòng (và khớp với hình cây 30% - 200%)
const ZOOM_MIN = 0.3;
const ZOOM_MAX = 2;
const SERIES_BOX = { top: '12%', bottom: '12%', left: '12%', right: '12%' };
// Hình sao: true = mở hết nhánh, false = chỉ hiện tầng đầu (nút Mở/Thu của toolbar)
let starExpanded = true;

const chartRef = ref<HTMLElement>();
let chart: echarts.ECharts | null = null;
let resizeObserver: ResizeObserver | null = null;
// Node của hình vòng lần vẽ gần nhất (tooltip cạnh tra tên theo id = chỉ số)
let ringNodes: any[] = [];

/* ---------- Helpers ---------- */
// ECharts vẽ canvas nên không hiểu var(--x): đọc giá trị thật từ :root
const cssColor = (c: string, fallback = '#909399') => {
	const m = /^var\((--[^),]+)/.exec(c.trim());
	if (!m) return c;
	return getComputedStyle(document.documentElement).getPropertyValue(m[1]).trim() || fallback;
};

const escapeHtml = (s: string) => String(s ?? '').replace(/[&<>"']/g, (ch) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[ch]!);

const isHit = (n: TopoNode) => {
	const kw = props.keyword.trim().toLowerCase();
	if (!kw) return true;
	return [n.name, n.equipmentName, n.ip].some((v) => (v ?? '').toLowerCase().includes(kw));
};

// Lọc trạng thái: chỉ node thiết bị (có IP) mới có trạng thái
const isStateHit = (n: TopoNode) => !props.stateFilter.length || (!!n.ip && props.stateFilter.includes(props.stateKeyOf(n.equipmentState)));

// Kích thước node: gốc lớn nhất, node có nhánh (switch/router) vừa, thiết bị lá nhỏ
const nodeSize = (n: TopoNode, depth: number) => (depth === 0 ? 22 : n.children?.length ? 15 : 11);

const nodeItem = (n: TopoNode, depth: number) => {
	const hit = isHit(n) && isStateHit(n);
	// Đang lọc trạng thái: node khớp phóng to + quầng sáng đúng màu trạng thái
	const glow = hit && props.stateFilter.length > 0;
	const color = cssColor(n.ip ? props.colorOf(n.equipmentState) : 'var(--el-color-primary)');
	return {
		name: n.name,
		raw: n,
		symbolSize: Math.round(nodeSize(n, depth) * (glow ? 1.5 : 1)),
		itemStyle: {
			color,
			borderColor: cssColor('var(--el-bg-color)', '#ffffff'),
			borderWidth: 1.5,
			opacity: hit ? 1 : 0.15,
			shadowBlur: glow ? 14 : 0,
			shadowColor: color,
		},
		label: { opacity: hit ? 1 : 0.25, fontWeight: (props.keyword.trim() || props.stateFilter.length) && hit ? 700 : 400 },
	};
};

// Tooltip cùng bố cục với thẻ + popover của hình cây:
// - Phần chính: Tên CyberMonitor, IP, Trạng thái
// - Phần chi tiết (ngăn cách bởi vạch): Thiết bị ITS + Lý trình — chỉ hiện khi có; thiết bị ngoài ITS -> không hiện
const tooltipHtml = (n: TopoNode) => {
	const muted = cssColor('var(--el-text-color-secondary)', '#909399');
	const line = cssColor('var(--el-border-color-lighter)', '#ebeef5');
	const primary = cssColor('var(--el-color-primary)', '#409eff');
	const stateColor = cssColor(props.colorOf(n.equipmentState));

	const head = [`<div style="font-weight:600;font-size:13px">${escapeHtml(n.name)}</div>`];
	if (n.ip) {
		head.push(`<div style="font-family:Consolas,'Courier New',monospace;font-size:12px;color:${muted}">${escapeHtml(n.ip)}</div>`);
		head.push(
			`<div style="display:flex;align-items:center;gap:5px;margin-top:2px;font-size:12px">` +
				`<span style="width:8px;height:8px;border-radius:50%;background:${stateColor}"></span>${escapeHtml(props.labelOf(n.equipmentState))}</div>`
		);
	}

	const km = props.formatKmMet(n.kmNumber, n.metNumber);
	if (!n.ip || (!n.equipmentName && !km)) return `<div style="min-width:160px">${head.join('')}</div>`;

	const row = (label: string, value: string) =>
		`<div style="display:grid;grid-template-columns:84px minmax(0,1fr);gap:8px;align-items:center;font-size:12px;min-height:20px">` +
		`<span style="color:${muted}">${label}</span><span style="font-weight:600;white-space:normal">${value}</span></div>`;
	const badge = `<span style="display:inline-block;margin-right:6px;padding:2px 5px;border-radius:4px;font-size:10px;font-weight:700;line-height:1;color:#fff;background:${primary}">ITS</span>`;
	const detail = [
		row(escapeHtml($t('lz.label.fmsTopology.itsEquipment')), n.equipmentName ? badge + escapeHtml(n.equipmentName) : ''),
		row(escapeHtml($t('lz.label.fmsTopology.location')), escapeHtml(km)),
	];
	return (
		`<div style="min-width:220px;max-width:280px">${head.join('')}` +
		`<div style="margin-top:8px;padding-top:8px;border-top:1px solid ${line};display:flex;flex-direction:column;gap:6px">${detail.join('')}</div></div>`
	);
};

/* ---------- Dựng option theo từng kiểu sơ đồ ---------- */
const buildStarSeries = (textColor: string, lineColor: string) => {
	const toTree = (n: TopoNode, depth: number): any => ({
		...nodeItem(n, depth),
		children: (n.children ?? []).map((c) => toTree(c, depth + 1)),
	});
	const root: TopoNode = props.roots.length === 1 ? props.roots[0] : { name: props.rootName, ip: '', children: props.roots };
	return {
		id: 'topology',
		type: 'tree',
		layout: 'radial',
		data: [toTree(root, 0)],
		...SERIES_BOX,
		symbol: 'circle',
		roam: true,
		scaleLimit: { min: ZOOM_MIN, max: ZOOM_MAX },
		initialTreeDepth: starExpanded ? -1 : 1,
		expandAndCollapse: true,
		label: { color: textColor, fontSize: 11 },
		lineStyle: { color: lineColor, width: 1.2, curveness: 0.4 },
		emphasis: { focus: 'descendant' },
		animationDurationUpdate: 500,
	};
};

const buildRingSeries = (textColor: string) => {
	const nodes: any[] = [];
	const links: any[] = [];
	// Duyệt theo thứ tự cây để node cùng nhánh nằm cạnh nhau trên vòng
	const walk = (n: TopoNode, parentId: string | null, depth: number) => {
		const id = String(nodes.length);
		nodes.push({ id, ...nodeItem(n, depth) });
		if (parentId !== null) links.push({ source: parentId, target: id });
		(n.children ?? []).forEach((c) => walk(c, id, depth + 1));
	};
	props.roots.forEach((r) => walk(r, null, 0));
	ringNodes = nodes;
	return {
		id: 'topology',
		type: 'graph',
		layout: 'circular',
		circular: { rotateLabel: true },
		data: nodes,
		links,
		...SERIES_BOX,
		roam: true,
		scaleLimit: { min: ZOOM_MIN, max: ZOOM_MAX },
		label: { show: true, position: 'right', formatter: '{b}', color: textColor, fontSize: 11 },
		lineStyle: { color: 'source', width: 1.2, curveness: 0.3, opacity: 0.6 },
		emphasis: { focus: 'adjacency', lineStyle: { width: 3, opacity: 1 } },
		animationDurationUpdate: 500,
	};
};

const buildOption = () => {
	const textColor = cssColor('var(--el-text-color-regular)', '#606266');
	const lineColor = cssColor('var(--el-border-color-darker)', '#cdd0d6');
	return {
		tooltip: {
			trigger: 'item',
			triggerOn: 'mousemove|click', // hover hoặc nhấp vào node đều mở chi tiết (giống hình cây)
			confine: true,
			formatter: (p: any) => {
				// Cạnh của hình vòng: hiện "cha → con"
				if (p.dataType === 'edge') {
					const s = ringNodes[+p.data.source]?.name ?? '';
					const t = ringNodes[+p.data.target]?.name ?? '';
					return `${escapeHtml(s)} → ${escapeHtml(t)}`;
				}
				return p.data?.raw ? tooltipHtml(p.data.raw) : '';
			},
		},
		series: [props.layout === 'star' ? buildStarSeries(textColor, lineColor) : buildRingSeries(textColor)],
	};
};

/* ---------- Render ---------- */
// full = true: vẽ lại từ đầu (đổi kiểu sơ đồ / dữ liệu / canh giữa) -> reset zoom & vị trí
// full = false: chỉ cập nhật màu/mờ khi gõ tìm kiếm -> giữ nguyên zoom & vị trí đang xem
const render = (full = true) => {
	if (!chart) return;
	if (!props.roots?.length) {
		chart.clear();
		return;
	}
	chart.setOption(buildOption(), { notMerge: full });
	if (full) emits('zoom', 1);
};

// Mức zoom hiện tại của series (ECharts cập nhật option.zoom khi người dùng cuộn chuột)
const currentZoom = (): number => (chart?.getOption() as any)?.series?.[0]?.zoom ?? 1;

/** Phóng to / thu nhỏ theo bước (nút -/+ trên toolbar) */
const zoomBy = (delta: number) => {
	if (!chart || !props.roots?.length) return;
	const next = Math.min(ZOOM_MAX, Math.max(ZOOM_MIN, +(currentZoom() + delta).toFixed(2)));
	chart.setOption({ series: [{ id: 'topology', zoom: next }] });
	emits('zoom', next);
};

/** Hình sao: mở hết / thu về tầng đầu */
const setExpanded = (expanded: boolean) => {
	starExpanded = expanded;
	render(true);
};

/** Canh giữa & về mức zoom mặc định */
const resetView = () => render(true);
defineExpose({ resetView, zoomBy, setExpanded });

watch(() => [props.roots, props.layout], () => render(true));
watch(() => [props.keyword, props.stateFilter.join('|')], () => render(false));

onMounted(() => {
	chart = echarts.init(chartRef.value!);
	// Cuộn chuột trên sơ đồ -> báo mức zoom mới cho toolbar
	const onRoam = (p: any) => p?.zoom != null && emits('zoom', +currentZoom().toFixed(2));
	chart.on('treeroam', onRoam);
	chart.on('graphroam', onRoam);
	render(true);
	resizeObserver = new ResizeObserver(() => chart?.resize());
	resizeObserver.observe(chartRef.value!);
});

onBeforeUnmount(() => {
	resizeObserver?.disconnect();
	chart?.dispose();
	chart = null;
});
</script>

<style scoped>
.topology-chart {
	width: 100%;
	height: 100%;
}
</style>
