<template>
	<!-- data-vw-remove-zone: sceneCanvas nhận ra vùng này khi kéo cửa sổ ra ⇒ thả vào đây để gỡ cửa sổ khỏi kịch bản -->
	<div class="source-palette" data-vw-remove-zone :style="{ '--sp-head-h': `${headHeight}px` }">
		<div class="source-palette-remove">
			<el-icon><ele-Delete /></el-icon>
			<span>{{ $t('lz.label.videoWall.dropHereToRemove') }}</span>
		</div>
		<!-- Tiêu đề + ô tìm dính đầu cột khi cuộn danh sách nguồn -->
		<div ref="headRef" class="source-palette-head">
			<div class="source-palette-title">{{ $t('lz.entity.videoWall.source') }}</div>
			<!-- Tìm theo tên / mã / dòng phụ (loại tín hiệu · bộ điều khiển), không phân biệt dấu; đang tìm thì mọi nhóm tự mở -->
			<el-input v-model="keyword" size="small" clearable prefix-icon="ele-Search" :placeholder="$t('lz.label.videoWall.searchSource')" />
			<!-- Loại nguồn đã khai báo: chỉ icon (rê chuột hiện tên loại), số trên badge = số nguồn đã khai báo của loại đó; bấm ⇒ mở nhóm và cuộn tới -->
			<div v-if="declaredTypes.length" class="source-palette-badges">
				<el-tooltip v-for="t in declaredTypes" :key="t.code" :content="t.name" placement="top">
					<el-badge :value="t.count" type="primary">
						<el-button size="small" @click="focusGroup(t.code)">
							<el-icon :style="{ color: sourceTypeColor(t.code) }">
								<ele-Monitor v-if="t.code === VideoWallSourceTypeEnum.LocalSignal" />
								<ele-Camera v-else-if="t.code === VideoWallSourceTypeEnum.IpStream" />
								<!-- <ele-Connection v-else-if="t.code === VideoWallSourceTypeEnum.CascadeLink" /> -->
								<!-- <ele-Picture v-else /> -->
							</el-icon>
						</el-button>
					</el-badge>
				</el-tooltip>
			</div>
		</div>

		<!-- Nhóm theo loại nguồn — thứ tự + tên nhóm lấy từ danh mục videowall_source_type; bấm tiêu đề nhóm để thu gọn / mở rộng -->
		<div v-for="group in groups" :key="group.code" class="source-group" :class="{ 'is-collapsed': isCollapsed(group.code) }" :data-group="group.code">
			<button type="button" class="source-group-header" :aria-expanded="!isCollapsed(group.code)" @click="toggleGroup(group.code)">
				<el-icon class="source-group-arrow"><ele-ArrowRight /></el-icon>
				<el-icon class="source-group-icon" :style="{ color: sourceTypeColor(group.code) }">
					<ele-Monitor v-if="group.code === VideoWallSourceTypeEnum.LocalSignal" />
					<ele-Camera v-else-if="group.code === VideoWallSourceTypeEnum.IpStream" />
					<ele-Picture v-else />
				</el-icon>
				<span class="source-group-name">{{ group.name }}</span>
				<span class="source-group-count">{{ searching ? `${group.items.length}/${group.total}` : group.items.length }}</span>
			</button>
			<!-- Nguồn đã có cửa sổ trong kịch bản: mờ + nhãn "Đang dùng", không kéo được (BE cũng chặn).
			     Thu gọn dùng v-show (giữ DOM) vì ảnh kéo lấy icon từ tiêu đề nhóm. -->
			<div
				v-for="source in group.items"
				v-show="!isCollapsed(group.code)"
				:key="source.id"
				class="source-item"
				:class="{ 'source-item--used': isUsed(source), 'source-item--active': isHighlighted(source) }"
				:data-source-id="source.id"
				:draggable="!isUsed(source)"
				:title="isUsed(source) ? $t('lz.label.videoWall.sourceUsedHint') : undefined"
				@dragstart="onDragStart($event, source)"
			>
				<div class="source-item-color" :style="{ background: sourceTypeColor(group.code) }"></div>
				<div class="source-item-info">
					<span class="source-item-name">{{ source.name || source.code }}</span>
					<span class="source-item-sub">{{ sourceSubText(source) }}</span>
				</div>
				<el-tag v-if="isUsed(source)" class="source-item-used" size="small" type="info" effect="plain">{{ $t('lz.label.videoWall.sourceInUse') }}</el-tag>
				<el-icon v-else class="source-item-grip"><ele-Rank /></el-icon>
			</div>
		</div>
		<div v-if="searching && !groups.length" class="source-palette-empty">
			<el-icon><ele-Search /></el-icon>
			<span>{{ $t('lz.label.base.noData') }}</span>
		</div>
	</div>
</template>

<script lang="ts" setup name="videoWallSourcePalette">
import { computed, nextTick, ref, watch } from 'vue';
import { useElementBounding } from '@vueuse/core';
import { StatusEnum } from '/@/api-services/videoWall';
import type { VwSourceOutput } from '/@/api-services/videoWall';
import { sourceTypeColor } from '/@/utils/videoWall/sourceColors';
import { BaseConfigTypeEnum, VideoWallSourceTypeEnum } from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';
import { useVideoWallController } from '/@/stores/its/videoWallController';

interface Props {
	sources: VwSourceOutput[];
	/** ID nguồn đã có cửa sổ trong kịch bản đang soạn — mỗi nguồn chỉ dùng ở 1 cửa sổ. */
	usedSourceIds?: string[];
	/** Nguồn của cửa sổ đang chọn trên canvas (id hoặc mã) ⇒ tô sáng và cuộn tới. */
	highlightId?: string;
}
const props = withDefaults(defineProps<Props>(), {
	usedSourceIds: () => [],
	highlightId: '',
});

const usedSet = computed(() => new Set(props.usedSourceIds));
const isUsed = (source: VwSourceOutput) => !!source.id && usedSet.value.has(source.id);

const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
const vwCtrl = useVideoWallController();

const keyword = ref('');
/** Chiều cao khối tiêu đề + ô tìm (đã dính đầu cột) ⇒ tiêu đề nhóm dính ngay bên dưới. */
const headRef = ref<HTMLElement>();
const { height: headHeight } = useElementBounding(headRef);
/** Bỏ dấu tiếng Việt để tìm "camera doan" ra "Camera đoạn". */
const normalize = (text: unknown) =>
	String(text ?? '')
		.toLowerCase()
		.normalize('NFD')
		.replace(/[̀-ͯ]/g, '')
		.replace(/đ/g, 'd');
const searchKey = computed(() => normalize(keyword.value.trim()));
const searching = computed(() => !!searchKey.value);
const matches = (s: VwSourceOutput) => normalize(`${s.name ?? ''} ${s.code ?? ''} ${sourceSubText(s)}`).includes(searchKey.value);

/**
 * Nguồn đang bật, nhóm theo loại; bỏ "Liên kết controller" (dây nối, không kéo lên màn hình được).
 * Đang tìm ⇒ mỗi nhóm chỉ giữ nguồn khớp (`total` = tổng của nhóm để hiện "khớp/tổng"); bỏ nhóm rỗng.
 */
const groups = computed(() =>
	(getConfigDataByCode(BaseConfigTypeEnum.VideoWallSourceType) as { code: string; name: string }[])
		.filter((t) => t.code !== VideoWallSourceTypeEnum.CascadeLink)
		.map((t) => {
			const all = props.sources.filter((s) => s.sourceType === t.code && s.status === StatusEnum.NUMBER_1);
			return { code: t.code, name: t.name, total: all.length, items: searching.value ? all.filter(matches) : all };
		})
		.filter((g) => g.items.length > 0)
);

/** Nhóm đang thu gọn (theo mã loại nguồn); mặc định mở hết. Đang tìm thì mở hết để thấy kết quả. */
const collapsed = ref(new Set<string>());
const isCollapsed = (code: string) => !searching.value && collapsed.value.has(code);
const toggleGroup = (code: string) => {
	const next = new Set(collapsed.value);
	if (next.has(code)) next.delete(code);
	else next.add(code);
	collapsed.value = next;
};

/** Loại nguồn khai báo trong danh mục (bỏ Liên kết controller) + số nguồn đang hoạt động của loại đó (badge đầu cột) — không đổi theo ô tìm. */
const declaredTypes = computed(() =>
	(getConfigDataByCode(BaseConfigTypeEnum.VideoWallSourceType) as { code: string; name: string }[])
		.filter((t) => t.code !== VideoWallSourceTypeEnum.CascadeLink)
		.map((t) => ({
			code: t.code,
			name: t.name,
			count: props.sources.filter((s) => s.sourceType === t.code && s.status === StatusEnum.NUMBER_1).length,
		}))
);

/** Bấm badge loại nguồn ⇒ bỏ tìm, mở nhóm đó rồi cuộn tới đầu nhóm (loại không có nhóm kéo thả, vd Liên kết controller ⇒ bỏ qua). */
async function focusGroup(code: string) {
	if (!groups.value.some((g) => g.code === code) && !keyword.value) return;
	keyword.value = '';
	if (collapsed.value.has(code)) toggleGroup(code);
	await nextTick();
	document.querySelector(`.source-group[data-group="${code}"]`)?.scrollIntoView({ block: 'start', behavior: 'smooth' });
}

const isHighlighted = (source: VwSourceOutput) => !!props.highlightId && (source.id === props.highlightId || source.code === props.highlightId);

/** Chọn cửa sổ trên canvas ⇒ mở nhóm chứa nguồn của nó (đang tìm mà không khớp thì bỏ tìm) rồi cuộn tới nguồn đó. */
watch(
	() => props.highlightId,
	async (id) => {
		const source = id ? props.sources.find((s) => s.id === id || s.code === id) : undefined;
		if (!source?.id) return;
		if (searching.value && !matches(source)) keyword.value = '';
		if (source.sourceType && collapsed.value.has(source.sourceType)) toggleGroup(source.sourceType);
		await nextTick();
		document.querySelector(`.source-item[data-source-id="${source.id}"]`)?.scrollIntoView({ block: 'center', behavior: 'smooth' });
	}
);

/** Dòng phụ dưới tên nguồn: loại tín hiệu · bộ điều khiển (mọi loại nguồn; không gắn bộ nào = "Dùng chung"). */
function sourceSubText(source: VwSourceOutput): string {
	const signal =
		source.signalType ||
		(source.sourceType === VideoWallSourceTypeEnum.LocalSignal ? 'HDMI' : source.sourceType === VideoWallSourceTypeEnum.IpStream ? 'RTSP' : '');
	const ctrl = vwCtrl.label(source.controllerId, '—');
	return signal ? `${signal} · ${ctrl}` : ctrl;
}

function onDragStart(event: DragEvent, source: VwSourceOutput) {
	if (isUsed(source)) {
		event.preventDefault();
		return;
	}
	if (event.dataTransfer) {
		event.dataTransfer.setData('text/plain', source.id ?? '');
		event.dataTransfer.effectAllowed = 'copy';
		setWindowDragImage(event, source);
	}
}

/**
 * Ảnh kéo mặc định là cả dòng nguồn (dài, che lưới) ⇒ thay bằng hình cửa sổ sẽ tạo trên canvas: nền màu loại nguồn,
 * icon + tên ở giữa, cỡ bằng 1 ô màn hình đang hiển thị. Phần tử phải nằm trong DOM lúc gọi setDragImage nên gắn tạm
 * vào body (ngoài màn hình) rồi gỡ ngay sau đó.
 */
function setWindowDragImage(event: DragEvent, source: VwSourceOutput) {
	const cell = document.querySelector('.scene-canvas-cell')?.getBoundingClientRect();
	const w = Math.round(Math.min(200, Math.max(96, cell?.width ?? 128)));
	const h = Math.round((w * 9) / 16);

	const ghost = document.createElement('div');
	ghost.style.cssText =
		`position:fixed;top:-1000px;left:-1000px;width:${w}px;height:${h}px;box-sizing:border-box;display:flex;flex-direction:column;` +
		`align-items:center;justify-content:center;gap:4px;padding:4px;border:2px solid rgba(255,255,255,.55);border-radius:6px;` +
		`background:${sourceTypeColor(source.sourceType)};color:#fff;text-shadow:0 1px 3px rgba(0,0,0,.55);` +
		'box-shadow:0 4px 12px rgba(0,0,0,.3);pointer-events:none';
	// Icon nhóm (Monitor / Camera / Picture) lấy từ tiêu đề nhóm đang hiển thị
	const icon = (event.currentTarget as HTMLElement | null)?.closest('.source-group')?.querySelector('.source-group-icon svg')?.cloneNode(true) as SVGElement | undefined;
	if (icon) {
		icon.setAttribute('width', '20');
		icon.setAttribute('height', '20');
		ghost.appendChild(icon);
	}
	const name = document.createElement('span');
	name.style.cssText = 'max-width:100%;font-size:11px;font-weight:600;line-height:1.3;overflow:hidden;text-overflow:ellipsis;white-space:nowrap';
	name.textContent = source.name || source.code || '';
	ghost.appendChild(name);

	document.body.appendChild(ghost);
	event.dataTransfer?.setDragImage(ghost, Math.round(w / 2), Math.round(h / 2));
	setTimeout(() => ghost.remove(), 0);
}
</script>

<style scoped>
.source-palette {
	position: relative;
	min-height: 100%;
	box-sizing: border-box;
	padding: 12px;
}

/* Lớp phủ "thả để gỡ": sceneCanvas bật class is-remove-target khi đang kéo cửa sổ vào cột này */
.source-palette-remove {
	position: absolute;
	inset: 6px;
	z-index: 10;
	display: none;
	flex-direction: column;
	align-items: center;
	justify-content: center;
	gap: 8px;
	border: 2px dashed var(--el-color-danger);
	border-radius: 8px;
	background: var(--el-color-danger-light-9);
	color: var(--el-color-danger);
	font-size: 13px;
	font-weight: 600;
	pointer-events: none;
}

.source-palette-remove .el-icon {
	font-size: 28px;
}

.source-palette.is-remove-target .source-palette-remove {
	display: flex;
}

.source-palette-title {
	font-size: 14px;
	font-weight: 600;
	color: var(--el-text-color-primary);
	margin-bottom: 12px;
}

/* Dính đầu vùng cuộn; lấn ra lề 12px của cột để nền che kín nội dung cuộn bên dưới */
.source-palette-head {
	position: sticky;
	top: 0;
	z-index: 5;
	margin: -12px -12px 12px;
	padding: 12px 12px 10px;
	background: var(--el-bg-color);
	border-bottom: 1px solid var(--el-border-color-lighter);
}

.source-palette-empty {
	display: flex;
	flex-direction: column;
	align-items: center;
	gap: 6px;
	padding: 24px 0;
	font-size: 13px;
	color: var(--el-text-color-secondary);
}

.source-palette-empty .el-icon {
	font-size: 20px;
}

.source-group {
	margin-bottom: 12px;
	/* Cuộn tới nhóm (bấm badge) ⇒ chừa chỗ cho khối tiêu đề dính đầu cột */
	scroll-margin-top: var(--sp-head-h);
}

/* Badge loại nguồn đã khai báo: chừa chỗ phía trên cho số của badge */
.source-palette-badges {
	/* Chia đều bề ngang ô tìm: mỗi loại 1 cột bằng nhau, nút giãn hết cột */
	display: grid;
	grid-auto-flow: column;
	grid-auto-columns: 1fr;
	gap: 16px;
	margin-top: 14px;
}
.source-palette-badges .el-badge,
.source-palette-badges .el-button {
	width: 100%;
}

.source-group.is-collapsed {
	margin-bottom: 4px;
}

/* Tiêu đề nhóm = nút thu gọn / mở rộng: mũi tên xoay · icon màu loại nguồn · tên · số lượng.
   Dính ngay dưới khối tiêu đề + ô tìm khi cuộn ⇒ nhóm dài vẫn thấy đang ở loại nguồn nào, bấm thu gọn được ngay */
.source-group-header {
	position: sticky;
	top: var(--sp-head-h, 0px);
	z-index: 3;
	box-shadow: 0 0 0 4px var(--el-bg-color);
	display: flex;
	align-items: center;
	gap: 6px;
	width: 100%;
	margin-bottom: 6px;
	padding: 6px 8px;
	border: none;
	border-radius: 6px;
	background: var(--el-fill-color-lighter);
	font-size: 12px;
	font-weight: 600;
	color: var(--el-text-color-regular);
	text-align: left;
	cursor: pointer;
	transition: background-color 0.15s;
}

.source-group-header:hover {
	background: var(--el-fill-color);
}

.source-group-arrow {
	flex-shrink: 0;
	font-size: 12px;
	color: var(--el-text-color-secondary);
	transform: rotate(90deg);
	transition: transform 0.2s;
}

.source-group.is-collapsed .source-group-arrow {
	transform: rotate(0deg);
}

.source-group-icon {
	flex-shrink: 0;
	font-size: 14px;
}

.source-group-name {
	flex: 1;
	min-width: 0;
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

.source-group-count {
	flex-shrink: 0;
	min-width: 22px;
	padding: 0 7px;
	border-radius: 999px;
	background: var(--el-bg-color);
	font-size: 11px;
	line-height: 18px;
	text-align: center;
	color: var(--el-text-color-secondary);
}

.source-item {
	display: flex;
	align-items: center;
	gap: 8px;
	padding: 8px 10px;
	border-radius: 6px;
	cursor: grab;
	margin-bottom: 2px;
	transition: background-color 0.15s;
	border: 1px solid transparent;
}

.source-item:hover {
	background: var(--el-fill-color-light);
	border-color: var(--el-border-color-lighter);
}

.source-item:active {
	cursor: grabbing;
	opacity: 0.7;
}

/* Nguồn của cửa sổ đang chọn trên canvas */
.source-item--active,
.source-item--active:hover {
	background: var(--el-color-primary-light-9);
	border-color: var(--el-color-primary);
	box-shadow: 0 0 0 2px var(--el-color-primary-light-7);
	animation: source-item-flash 0.9s ease-out;
}

@keyframes source-item-flash {
	0% {
		box-shadow: 0 0 0 6px var(--el-color-primary-light-5);
	}
	100% {
		box-shadow: 0 0 0 2px var(--el-color-primary-light-7);
	}
}

.source-item-color {
	width: 28px;
	height: 28px;
	border-radius: 6px;
	flex-shrink: 0;
}

.source-item-info {
	flex: 1;
	min-width: 0;
	display: flex;
	flex-direction: column;
}

.source-item-name {
	font-size: 13px;
	color: var(--el-text-color-primary);
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
}

.source-item-sub {
	font-size: 11px;
	color: var(--el-text-color-secondary);
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
}

/* Nguồn đã dùng trong kịch bản: không kéo được */
.source-item--used {
	cursor: not-allowed;
	opacity: 0.55;
}

.source-item--used:hover {
	background: transparent;
	border-color: transparent;
}

.source-item--used:active {
	cursor: not-allowed;
	opacity: 0.55;
}

.source-item-used {
	flex-shrink: 0;
}

.source-item-grip {
	color: var(--el-text-color-placeholder);
	flex-shrink: 0;
}
</style>
