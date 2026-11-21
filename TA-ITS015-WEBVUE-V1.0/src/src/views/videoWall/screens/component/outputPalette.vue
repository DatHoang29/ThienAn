<template>
	<!-- Cùng kiểu SourcePalette (Quản lý kịch bản): đầu cột dính (tiêu đề, ô tìm, badge theo bộ), nhóm thu gọn / mở rộng, dòng kéo được -->
	<div class="vp" :class="{ 'is-remove-target': removeOver }" :style="{ '--vp-head-h': `${headHeight}px` }">
		<!-- Lớp phủ "thả để đưa ra ngoài lưới": hiện khi đang kéo cổng đang nằm trên lưới -->
		<div v-if="removeActive" class="vp-remove">
			<el-icon><ele-Remove /></el-icon>
			<span>{{ $t('lz.label.videoWall.dropToUnplace') }}</span>
		</div>

		<div ref="headRef" class="vp-head">
			<div class="vp-title">{{ $t('lz.label.videoWall.outputListTitle') }}</div>
			<div class="vp-hint">{{ $t('lz.label.videoWall.dragOutputHint') }}</div>
			<el-input v-model="keyword" size="small" clearable prefix-icon="ele-Search" :placeholder="$t('lz.label.videoWall.searchOutput')" />
			<!-- Badge theo bộ điều khiển: số trên badge = cổng còn ngoài lưới; bấm ⇒ mở nhóm và cuộn tới -->
			<div v-if="groupsAll.length" class="vp-badges">
				<el-tooltip v-for="g in groupsAll" :key="g.id" placement="top"
					:content="$t('lz.label.videoWall.outputBadgeTip', { name: g.label, placed: g.placed, total: g.total })">
					<el-badge :value="g.total - g.placed" :hidden="g.total === g.placed" type="warning">
						<el-button size="small" @click="focusGroup(g.id)">
							<i class="vp-swatch" :style="{ background: vwCtrl.colorOf(g.id) }" />
							<span class="vp-badge-code">{{ vwCtrlCode(vwCtrl.find(g.id)?.code) || g.label }}</span>
						</el-button>
					</el-badge>
				</el-tooltip>
			</div>
		</div>

		<!-- Nhóm theo bộ điều khiển — bấm tiêu đề để thu gọn / mở rộng -->
		<div v-for="group in groups" :key="group.id" class="vp-group" :class="{ 'is-collapsed': isCollapsed(group.id) }" :data-group="group.id"
			:style="{ '--ctrl-color': group.color }">
			<button type="button" class="vp-group-header" :aria-expanded="!isCollapsed(group.id)" @click="toggleGroup(group.id)">
				<el-icon class="vp-group-arrow"><ele-ArrowRight /></el-icon>
				<el-icon v-if="group.id === MISSING" class="vp-group-icon is-missing"><ele-Warning /></el-icon>
				<i v-else class="vp-swatch" :style="{ background: group.color }" />
				<span class="vp-group-name">{{ group.label }}</span>
				<span class="vp-group-count">{{ searching ? `${group.items.length}/${group.total}` : group.id === MISSING ? group.total : `${group.placed}/${group.total}` }}</span>
			</button>
			<div
				v-for="o in group.items"
				v-show="!isCollapsed(group.id)"
				:key="o.id!"
				class="vp-item"
				:class="{
					'vp-item--active': o.id === selectedId,
					'vp-item--placed': o.blockNo != null,
					'vp-item--missing': group.id === MISSING,
					'vp-item--off': o.status === 0,
					'vp-item--dragging': o.id === draggingId,
				}"
				:data-output-id="o.id"
				:draggable="draggable && group.id !== MISSING"
				@dragstart="onDragStart($event, o)"
				@dragend="emit('dragend')"
				@click="emit('select', o)"
			>
				<div class="vp-item-color" :style="{ background: group.color }">
					<el-icon><ele-Monitor /></el-icon>
				</div>
				<div class="vp-item-info">
					<span class="vp-item-name">{{ outputLabel(o) }}</span>
					<span class="vp-item-sub">{{ subText(o, group.id === MISSING) }}</span>
				</div>
				<i v-if="dirtyIds.includes(o.id ?? '')" class="vp-item-dirty" :title="$t('lz.label.videoWall.unsavedChanges')" />
				<el-tag v-if="group.id === MISSING" class="vp-item-tag" size="small" type="danger" effect="plain">{{ $t('lz.label.videoWall.outputMissingTag') }}</el-tag>
				<el-tag v-else-if="o.blockNo != null" class="vp-item-tag" size="small" effect="plain">{{ position(o) }}</el-tag>
				<el-icon v-if="draggable && group.id !== MISSING" class="vp-item-grip"><ele-Rank /></el-icon>
			</div>
		</div>
		<div v-if="!groups.length" class="vp-empty">
			<el-icon><ele-Search /></el-icon>
			<span>{{ $t('lz.label.base.noData') }}</span>
		</div>
	</div>
</template>

<script lang="ts" setup name="videoWallOutputPalette">
import { computed, nextTick, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useElementBounding } from '@vueuse/core';
import type { VwControllerOutputOutput } from '/@/api-services/videoWall';
import { useVideoWallController, vwCtrlCode } from '/@/stores/its/videoWallController';
import { cellLabel, useVideoWallTopology } from '/@/stores/its/videoWallTopology';

interface Props {
	/** Cổng của bộ hiển thị / độc lập (đã bỏ cổng bộ phân phối). */
	outputs: VwControllerOutputOutput[];
	/** Cổng đổi vị trí chưa lưu ⇒ chấm vàng. */
	dirtyIds?: string[];
	selectedId?: string;
	draggingId?: string;
	/** Có quyền sửa ⇒ kéo được. */
	draggable?: boolean;
	/** Đang kéo cổng trên lưới ⇒ hiện lớp phủ "thả để đưa ra ngoài lưới"; removeOver = đang rê trên cột. */
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
	(e: 'select', o: VwControllerOutputOutput): void;
	(e: 'dragstart', ev: DragEvent, o: VwControllerOutputOutput): void;
	(e: 'dragend'): void;
}>();

/** Trạng thái cổng trên thiết bị (VwOutputDeviceState) + id nhóm giả cho cổng mất. */
const DEVICE_MISSING = 2;
const MISSING = '__missing__';

const { t: $t } = useI18n();
const vwCtrl = useVideoWallController();
const topology = useVideoWallTopology();

const outputLabel = (o: VwControllerOutputOutput) => o.displayName || o.name || `#${o.deviceOutputId ?? '?'}`;
const position = (o: VwControllerOutputOutput) => cellLabel(o.blockNo, o.gridRow, o.gridCol, topology.isMultiBlock);
/** Dòng phụ: mã màn đang cắm · vị trí (cổng mất: bộ điều khiển). */
const subText = (o: VwControllerOutputOutput, isMissing: boolean) => {
	const screen = o.screenCode || $t('lz.label.videoWall.noScreen');
	if (isMissing) return `${screen} · ${vwCtrl.label(o.controllerId)}`;
	return screen;
};

const keyword = ref('');
const headRef = ref<HTMLElement>();
const { height: headHeight } = useElementBounding(headRef);
/** Bỏ dấu tiếng Việt để tìm "cong 7" ra "Cổng 7". */
const normalize = (text: unknown) =>
	String(text ?? '')
		.toLowerCase()
		.normalize('NFD')
		.replace(/[̀-ͯ]/g, '')
		.replace(/đ/g, 'd');
const searchKey = computed(() => normalize(keyword.value.trim()));
const searching = computed(() => !!searchKey.value);
const matches = (o: VwControllerOutputOutput) =>
	normalize(`${outputLabel(o)} ${o.name ?? ''} ${o.screenCode ?? ''} ${o.screenName ?? ''} ${vwCtrl.label(o.controllerId)} ${o.blockNo != null ? position(o) : ''}`).includes(
		searchKey.value
	);
const byPort = (a: VwControllerOutputOutput, b: VwControllerOutputOutput) =>
	(a.deviceOutputId ?? 0) - (b.deviceOutputId ?? 0) || outputLabel(a).localeCompare(outputLabel(b));

/** Mọi nhóm theo bộ điều khiển (không lọc tìm) — dùng cho badge và số đếm. */
const groupsAll = computed(() => {
	const map = new Map<string, { id: string; label: string; color: string; placed: number; total: number; all: VwControllerOutputOutput[] }>();
	for (const o of props.outputs) {
		if (o.deviceState === DEVICE_MISSING || !o.controllerId) continue;
		const id = o.controllerId;
		const g = map.get(id) ?? { id, label: vwCtrl.label(id), color: vwCtrl.colorOf(id), placed: 0, total: 0, all: [] };
		g.total++;
		if (o.blockNo != null) g.placed++;
		g.all.push(o);
		map.set(id, g);
	}
	return [...map.values()].sort((a, b) => a.label.localeCompare(b.label));
});

/** Nhóm hiển thị: theo bộ + nhóm "Cổng mất" cuối; đang tìm ⇒ chỉ giữ dòng khớp, bỏ nhóm rỗng. */
const groups = computed(() => {
	const missing = props.outputs.filter((o) => o.deviceState === DEVICE_MISSING);
	const list = [
		...groupsAll.value,
		...(missing.length
			? [{ id: MISSING, label: $t('lz.label.videoWall.outputMissing'), color: 'var(--el-color-danger)', placed: 0, total: missing.length, all: missing }]
			: []),
	];
	return list
		.map((g) => ({ ...g, items: (searching.value ? g.all.filter(matches) : g.all).slice().sort(byPort) }))
		.filter((g) => g.items.length > 0);
});

/** Nhóm đang thu gọn; mặc định mở hết. Đang tìm thì mở hết để thấy kết quả. */
const collapsed = ref(new Set<string>());
const isCollapsed = (id: string) => !searching.value && collapsed.value.has(id);
const toggleGroup = (id: string) => {
	const next = new Set(collapsed.value);
	if (next.has(id)) next.delete(id);
	else next.add(id);
	collapsed.value = next;
};

/** Bấm badge ⇒ bỏ tìm, mở nhóm rồi cuộn tới đầu nhóm. */
async function focusGroup(id: string) {
	keyword.value = '';
	if (collapsed.value.has(id)) toggleGroup(id);
	await nextTick();
	document.querySelector(`.vp-group[data-group="${id}"]`)?.scrollIntoView({ block: 'start', behavior: 'smooth' });
}

/** Chọn ô trên lưới ⇒ mở nhóm chứa cổng (bỏ tìm nếu không khớp) rồi cuộn tới cổng đó. */
watch(
	() => props.selectedId,
	async (id) => {
		const o = id ? props.outputs.find((x) => x.id === id) : undefined;
		if (!o) return;
		if (searching.value && !matches(o)) keyword.value = '';
		const groupId = o.deviceState === DEVICE_MISSING ? MISSING : o.controllerId ?? '';
		if (collapsed.value.has(groupId)) toggleGroup(groupId);
		await nextTick();
		document.querySelector(`.vp-item[data-output-id="${o.id}"]`)?.scrollIntoView({ block: 'nearest', behavior: 'smooth' });
	}
);

function onDragStart(event: DragEvent, o: VwControllerOutputOutput) {
	emit('dragstart', event, o);
	if (!event.defaultPrevented) setTileDragImage(event, o);
}

/**
 * Ảnh kéo mặc định là cả dòng (dài, che lưới) ⇒ thay bằng hình tấm màn sẽ nằm trên lưới: nền tối, vạch trên màu bộ,
 * mã màn + cổng, cỡ bằng 1 ô đang hiển thị. Gắn tạm vào body (ngoài màn hình) rồi gỡ ngay.
 */
function setTileDragImage(event: DragEvent, o: VwControllerOutputOutput) {
	const cell = document.querySelector('.om-cell')?.getBoundingClientRect();
	const w = Math.round(Math.min(200, Math.max(96, cell?.width ?? 128)));
	const h = Math.round(Math.min(160, Math.max(56, cell?.height ?? 72)));

	const ghost = document.createElement('div');
	ghost.style.cssText =
		`position:fixed;top:-1000px;left:-1000px;width:${w}px;height:${h}px;box-sizing:border-box;display:flex;flex-direction:column;` +
		'align-items:center;justify-content:center;gap:2px;padding:4px;border:1px solid rgba(140,190,255,.55);border-radius:4px;' +
		`background:linear-gradient(170deg,#1f3a63 0%,#152b4c 100%);box-shadow:inset 0 3px 0 ${vwCtrl.colorOf(o.controllerId)},0 4px 12px rgba(0,0,0,.35);` +
		'color:#f2f6fc;pointer-events:none';
	const code = document.createElement('span');
	code.style.cssText = 'max-width:100%;font-size:13px;font-weight:600;overflow:hidden;text-overflow:ellipsis;white-space:nowrap';
	code.textContent = o.screenCode || $t('lz.label.videoWall.noScreen');
	const port = document.createElement('span');
	port.style.cssText = 'max-width:100%;font-size:11px;color:rgba(186,208,240,.8);overflow:hidden;text-overflow:ellipsis;white-space:nowrap';
	port.textContent = `${vwCtrlCode(vwCtrl.find(o.controllerId)?.code)} · ${outputLabel(o)}`;
	ghost.append(code, port);

	document.body.appendChild(ghost);
	event.dataTransfer?.setDragImage(ghost, Math.round(w / 2), Math.round(h / 2));
	setTimeout(() => ghost.remove(), 0);
}
</script>

<style scoped lang="scss">
@import './palette.scss';
</style>
