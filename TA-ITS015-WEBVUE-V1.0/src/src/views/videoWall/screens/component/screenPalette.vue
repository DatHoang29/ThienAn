<template>
	<!-- Danh sách màn (cùng kiểu OutputPalette / SourcePalette): nhóm Trong kho / Trên lưới chưa có cổng / Trên lưới có cổng -->
	<div class="vp" :class="{ 'is-remove-target': removeOver }" :style="{ '--vp-head-h': `${headHeight}px` }">
		<!-- Lớp phủ "thả để đưa màn về kho": hiện khi đang kéo màn đang trên lưới -->
		<div v-if="removeActive" class="vp-remove">
			<el-icon><ele-Remove /></el-icon>
			<span>{{ $t('lz.label.videoWall.dropToUnplace') }}</span>
		</div>

		<div ref="headRef" class="vp-head">
			<div class="vp-title">{{ $t('lz.label.videoWall.screenListTitle') }}</div>
			<div class="vp-hint">{{ $t('lz.label.videoWall.dragScreenHint') }}</div>
			<el-input v-model="keyword" size="small" clearable prefix-icon="ele-Search" :placeholder="$t('lz.label.videoWall.searchScreen')" />
			<!-- Badge theo nhóm: số màn của nhóm; bấm ⇒ mở nhóm và cuộn tới -->
			<div class="vp-badges">
				<el-tooltip v-for="g in badgeGroups" :key="g.id" :content="g.label" placement="top">
					<el-badge :value="g.all.length" :hidden="!g.all.length" :type="g.badge">
						<el-button size="small" @click="focusGroup(g.id)">
							<el-icon :style="{ color: g.color }"><component :is="g.icon" /></el-icon>
						</el-button>
					</el-badge>
				</el-tooltip>
			</div>
		</div>

		<div v-for="group in groups" :key="group.id" class="vp-group" :class="{ 'is-collapsed': isCollapsed(group.id) }" :data-screen-group="group.id">
			<button type="button" class="vp-group-header" :aria-expanded="!isCollapsed(group.id)" @click="toggleGroup(group.id)">
				<el-icon class="vp-group-arrow"><ele-ArrowRight /></el-icon>
				<el-icon :style="{ color: group.color }"><component :is="group.icon" /></el-icon>
				<span class="vp-group-name">{{ group.label }}</span>
				<span class="vp-group-count">{{ searching ? `${group.items.length}/${group.all.length}` : group.items.length }}</span>
			</button>
			<div
				v-for="s in group.items"
				v-show="!isCollapsed(group.id)"
				:key="s.id!"
				class="vp-item"
				:class="{
					'vp-item--active': s.id === selectedId,
					'vp-item--placed': s.wallBlockId != null,
					'vp-item--off': s.status === 0,
					'vp-item--dragging': s.id === draggingId,
				}"
				:data-screen-id="s.id"
				:draggable="draggable"
				@dragstart="onDragStart($event, s)"
				@dragend="emit('dragend')"
				@click="emit('select', s)"
			>
				<div class="vp-item-color" :style="{ background: colorOf(s) }">
					<el-icon><ele-Monitor /></el-icon>
				</div>
				<div class="vp-item-info">
					<span class="vp-item-name">{{ s.code }}<template v-if="s.name && s.name !== s.code"> · {{ s.name }}</template></span>
					<span class="vp-item-sub">{{ subText(s) }}</span>
				</div>
				<el-tooltip v-if="isMismatch(s)" :content="$t('lz.label.videoWall.serialMismatchHint', { serial: s.deviceSerialNumber })" placement="top">
					<el-icon class="vp-item-warn"><ele-WarningFilled /></el-icon>
				</el-tooltip>
				<i v-if="dirtyIds.includes(s.id ?? '')" class="vp-item-dirty" :title="$t('lz.label.videoWall.unsavedChanges')" />
				<el-tag v-if="s.wallBlockId != null" class="vp-item-tag" size="small" effect="plain">{{ position(s) }}</el-tag>
				<el-icon v-if="draggable" class="vp-item-grip"><ele-Rank /></el-icon>
			</div>
		</div>
		<div v-if="!groups.length" class="vp-empty">
			<el-icon><ele-Search /></el-icon>
			<span>{{ $t('lz.label.base.noData') }}</span>
		</div>
	</div>
</template>

<script lang="ts" setup name="videoWallScreenPalette">
import { computed, nextTick, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useElementBounding } from '@vueuse/core';
import type { VwControllerOutputOutput, VwScreenOutput } from '/@/api-services/videoWall';
import { useVideoWallController, vwCtrlCode } from '/@/stores/its/videoWallController';
import { cellLabel, useVideoWallTopology } from '/@/stores/its/videoWallTopology';

interface Props {
	/** Màn (bản nháp trên trang): vị trí + cổng gán. */
	screens: VwScreenOutput[];
	/** Cổng ra (để lấy bộ điều khiển / tên cổng của màn). */
	outputs: VwControllerOutputOutput[];
	dirtyIds?: string[];
	selectedId?: string;
	draggingId?: string;
	draggable?: boolean;
	removeActive?: boolean;
	removeOver?: boolean;
}
const props = withDefaults(defineProps<Props>(), {
	dirtyIds: () => [],
	selectedId: '',
	draggingId: '',
	draggable: false,
	removeActive: false,
	removeOver: false,
});
const emit = defineEmits<{
	(e: 'select', s: VwScreenOutput): void;
	(e: 'dragstart', ev: DragEvent, s: VwScreenOutput): void;
	(e: 'dragend'): void;
}>();

/** 'stock' | 'noOutput' | ID bộ điều khiển (màn đã gán cổng chia theo bộ của cổng). */
type GroupId = string;

const { t: $t } = useI18n();
const vwCtrl = useVideoWallController();
const topology = useVideoWallTopology();

const outputById = computed(() => new Map(props.outputs.map((o) => [o.id ?? '', o])));
const outputOf = (s: VwScreenOutput) => (s.controllerOutputId ? outputById.value.get(s.controllerOutputId) : undefined);
const outputLabel = (o: VwControllerOutputOutput) => o.displayName || o.name || `#${o.deviceOutputId ?? '?'}`;
const position = (s: VwScreenOutput) => cellLabel(s.blockNo, s.gridRow, s.gridCol, topology.isMultiBlock);
const colorOf = (s: VwScreenOutput) => (outputOf(s) ? vwCtrl.colorOf(outputOf(s)!.controllerId) : 'var(--el-text-color-placeholder)');
const isMismatch = (s: VwScreenOutput) => !!s.deviceSerialNumber && !!s.serialNumber && s.deviceSerialNumber !== s.serialNumber;
/** Dòng phụ: serial · bộ · cổng (chưa có cổng ⇒ "Chưa có cổng"). */
const subText = (s: VwScreenOutput) => {
	const o = outputOf(s);
	const port = o ? `${vwCtrlCode(vwCtrl.find(o.controllerId)?.code)} · ${outputLabel(o)}` : $t('lz.label.videoWall.screenNoOutput');
	return s.serialNumber ? `${s.serialNumber} · ${port}` : port;
};
const groupOf = (s: VwScreenOutput): GroupId => (s.wallBlockId == null ? 'stock' : outputOf(s)?.controllerId || 'noOutput');

const keyword = ref('');
const headRef = ref<HTMLElement>();
const { height: headHeight } = useElementBounding(headRef);
const normalize = (text: unknown) =>
	String(text ?? '')
		.toLowerCase()
		.normalize('NFD')
		.replace(/[̀-ͯ]/g, '')
		.replace(/đ/g, 'd');
const searchKey = computed(() => normalize(keyword.value.trim()));
const searching = computed(() => !!searchKey.value);
const matches = (s: VwScreenOutput) =>
	normalize(`${s.code ?? ''} ${s.name ?? ''} ${s.serialNumber ?? ''} ${s.deviceSerialNumber ?? ''} ${subText(s)} ${s.wallBlockId ? position(s) : ''}`).includes(searchKey.value);
const byPos = (a: VwScreenOutput, b: VwScreenOutput) =>
	(a.blockNo ?? 0) - (b.blockNo ?? 0) || (a.gridRow ?? 0) - (b.gridRow ?? 0) || (a.gridCol ?? 0) - (b.gridCol ?? 0) || (a.code ?? '').localeCompare(b.code ?? '');

const groupsAll = computed(() => {
	// Màn đã gán cổng: mỗi bộ điều khiển 1 nhóm (màu của bộ, xếp theo mã bộ)
	const controllerIds = [...new Set(props.screens.map(groupOf).filter((id) => id !== 'stock' && id !== 'noOutput'))].sort((a, b) =>
		(vwCtrl.find(a)?.code ?? a).localeCompare(vwCtrl.find(b)?.code ?? b)
	);
	const defs: { id: GroupId; label: string; icon: string; color: string; badge: 'info' | 'warning' | 'primary' }[] = [
		{ id: 'stock', label: $t('lz.label.videoWall.screenGroupStock'), icon: 'ele-Box', color: 'var(--el-text-color-secondary)', badge: 'info' },
		{ id: 'noOutput', label: $t('lz.label.videoWall.screenGroupNoOutput'), icon: 'ele-Warning', color: 'var(--el-color-warning)', badge: 'warning' },
		...controllerIds.map((id) => ({ id, label: vwCtrl.label(id), icon: 'ele-Cpu', color: vwCtrl.colorOf(id), badge: 'primary' as const })),
	];
	return defs.map((d) => ({ ...d, all: props.screens.filter((s) => groupOf(s) === d.id) }));
});
/** Badge đầu danh sách: 3 nhóm gộp (màn đã gán cổng không tách theo bộ). */
const WITH_OUTPUT = 'withOutput';
const badgeGroups = computed(() => {
	const ctrlGroups = groupsAll.value.filter((g) => g.id !== 'stock' && g.id !== 'noOutput');
	return [
		...groupsAll.value.filter((g) => g.id === 'stock' || g.id === 'noOutput'),
		{
			id: WITH_OUTPUT,
			label: $t('lz.label.videoWall.screenGroupWithOutput'),
			icon: 'ele-Connection',
			color: 'var(--el-color-primary)',
			badge: 'primary' as const,
			all: ctrlGroups.flatMap((g) => g.all),
		},
	];
});
const groups = computed(() =>
	groupsAll.value
		.map((g) => ({ ...g, items: (searching.value ? g.all.filter(matches) : g.all).slice().sort(byPos) }))
		.filter((g) => g.items.length > 0)
);

const collapsed = ref(new Set<string>());
const isCollapsed = (id: string) => !searching.value && collapsed.value.has(id);
const toggleGroup = (id: string) => {
	const next = new Set(collapsed.value);
	if (next.has(id)) next.delete(id);
	else next.add(id);
	collapsed.value = next;
};

async function focusGroup(id: string) {
	keyword.value = '';
	// Badge "Đã gán cổng" ⇒ nhóm bộ điều khiển đầu tiên
	if (id === WITH_OUTPUT) id = groupsAll.value.find((g) => g.id !== 'stock' && g.id !== 'noOutput')?.id ?? id;
	if (collapsed.value.has(id)) toggleGroup(id);
	await nextTick();
	document.querySelector(`.vp-group[data-screen-group="${id}"]`)?.scrollIntoView({ block: 'start', behavior: 'smooth' });
}

/** Chọn màn trên lưới ⇒ mở nhóm chứa màn rồi cuộn tới. */
watch(
	() => props.selectedId,
	async (id) => {
		const s = id ? props.screens.find((x) => x.id === id) : undefined;
		if (!s) return;
		if (searching.value && !matches(s)) keyword.value = '';
		const g = groupOf(s);
		if (collapsed.value.has(g)) toggleGroup(g);
		await nextTick();
		document.querySelector(`.vp-item[data-screen-id="${s.id}"]`)?.scrollIntoView({ block: 'nearest', behavior: 'smooth' });
	}
);

function onDragStart(event: DragEvent, s: VwScreenOutput) {
	emit('dragstart', event, s);
	if (!event.defaultPrevented) setTileDragImage(event, s);
}

/** Ảnh kéo = tấm màn sẽ nằm trên lưới (nền tối, vạch trên màu bộ của cổng), cỡ bằng 1 ô đang hiển thị. */
function setTileDragImage(event: DragEvent, s: VwScreenOutput) {
	const cell = document.querySelector('.om-cell')?.getBoundingClientRect();
	const w = Math.round(Math.min(200, Math.max(96, cell?.width ?? 128)));
	const h = Math.round(Math.min(160, Math.max(56, cell?.height ?? 72)));
	const o = outputOf(s);

	const ghost = document.createElement('div');
	ghost.style.cssText =
		`position:fixed;top:-1000px;left:-1000px;width:${w}px;height:${h}px;box-sizing:border-box;display:flex;flex-direction:column;` +
		'align-items:center;justify-content:center;gap:2px;padding:4px;border:1px solid rgba(140,190,255,.55);border-radius:4px;' +
		`background:linear-gradient(170deg,#1f3a63 0%,#152b4c 100%);box-shadow:inset 0 3px 0 ${o ? vwCtrl.colorOf(o.controllerId) : '#7a8699'},0 4px 12px rgba(0,0,0,.35);` +
		'color:#f2f6fc;pointer-events:none';
	const code = document.createElement('span');
	code.style.cssText = 'max-width:100%;font-size:13px;font-weight:600;overflow:hidden;text-overflow:ellipsis;white-space:nowrap';
	code.textContent = s.code || s.name || '';
	const port = document.createElement('span');
	port.style.cssText = 'max-width:100%;font-size:11px;color:rgba(186,208,240,.8);overflow:hidden;text-overflow:ellipsis;white-space:nowrap';
	port.textContent = o ? `${vwCtrlCode(vwCtrl.find(o.controllerId)?.code)} · ${outputLabel(o)}` : $t('lz.label.videoWall.screenNoOutput');
	ghost.append(code, port);

	document.body.appendChild(ghost);
	event.dataTransfer?.setDragImage(ghost, Math.round(w / 2), Math.round(h / 2));
	setTimeout(() => ghost.remove(), 0);
}
</script>

<style scoped lang="scss">
@import './palette.scss';

.vp-item-warn {
	flex-shrink: 0;
	color: var(--el-color-warning);
}
</style>
