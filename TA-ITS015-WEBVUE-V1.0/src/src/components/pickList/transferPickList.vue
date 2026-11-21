<template>
	<!--
		Chọn nhiều mục bằng 2 cột (PickList của PrimeVue): trái = chưa chọn (có ô tìm), phải = đã chọn.
		  - v-model = danh sách ID đã chọn (giữ thứ tự chọn); chuyển bằng nút giữa hoặc bấm đúp.
		  - Kéo cột nút ở giữa sang trái / phải để đổi chiều rộng 2 cột; bấm đúp vào đó để chia đều lại.
		PickList v4 không có ô lọc sẵn ⇒ tự lọc cột trái; nút "chuyển tất cả" chỉ chuyển các mục đang hiện.
	-->
	<div ref="rootRef" class="tpl" :class="{ 'is-resizing': resizing }" :style="{ '--tpl-left': ratio, '--tpl-right': 1 - ratio }"
		@pointerdown="startResize" @dblclick="onDblClick" @mouseover="onTipOver" @mouseleave="tipEl = undefined">
		<PickList :model-value="lists" data-key="id" :scroll-height="scrollHeight" :show-source-controls="false" :show-target-controls="false"
			:move-to-target-props="{ 'data-tip': $t('lz.label.base.pickMoveToTarget') }" :move-all-to-target-props="{ 'data-tip': $t('lz.label.base.pickMoveAllToTarget') }"
			:move-to-source-props="{ 'data-tip': $t('lz.label.base.pickMoveToSource') }" :move-all-to-source-props="{ 'data-tip': $t('lz.label.base.pickMoveAllToSource') }"
			@update:model-value="onListsChange">
			<template #sourceheader>
				<div class="tpl-head">
					<span>{{ sourceTitle }} ({{ source.length }})</span>
					<!-- Ô tìm không phải dữ liệu form: không kích hoạt kiểm tra của form-item bao ngoài -->
					<el-input v-model="keyword" size="small" clearable prefix-icon="ele-Search" :placeholder="searchPlaceholder" :validate-event="false" />
				</div>
			</template>
			<template #targetheader>
				<div class="tpl-head">
					<span>{{ targetTitle }} ({{ target.length }})</span>
				</div>
			</template>
			<template #option="{ option }">
				<div class="tpl-option" :style="{ paddingLeft: !keyword && indentOf ? `${indentOf(option) * 14}px` : undefined }">
					<span v-if="iconOf?.(option)" class="tpl-option-icon">
						<el-icon><component :is="iconOf(option)" /></el-icon>
					</span>
					<span class="tpl-option-text">
						<span class="tpl-option-name">{{ labelOf(option) }}</span>
						<span v-if="subOf?.(option)" class="tpl-option-sub">{{ subOf(option) }}</span>
					</span>
				</div>
			</template>
		</PickList>
		<!-- Chú thích nút chuyển: tooltip mặc định (nền đen) của hệ thống, gắn vào nút đang rê chuột (PickList không cho bọc nút) -->
		<el-tooltip :visible="!!tipEl" :virtual-ref="tipEl" virtual-triggering :content="tipEl?.dataset.tip" placement="top" />
		<!-- Mục cha "dính" đầu cột trái khi cuộn (danh sách dạng cây): bấm để cuộn về mục đó -->
		<Teleport v-if="stickyHost && sticky.items.length" :to="stickyHost">
			<div class="tpl-sticky" :style="{ top: `${sticky.top}px`, width: `${sticky.width}px` }">
				<div v-for="item in sticky.items" :key="item.id!" class="tpl-sticky-row" @click="scrollToItem(item)">
					<div class="tpl-option" :style="{ paddingLeft: indentOf ? `${indentOf(item) * 14}px` : undefined }">
						<span v-if="iconOf?.(item)" class="tpl-option-icon">
							<el-icon><component :is="iconOf(item)" /></el-icon>
						</span>
						<span class="tpl-option-text">
							<span class="tpl-option-name">{{ labelOf(item) }}</span>
						</span>
					</div>
				</div>
			</div>
		</Teleport>
	</div>
</template>

<script lang="ts" setup name="transferPickList">
import { computed, nextTick, onBeforeUnmount, onMounted, reactive, ref, shallowRef, watch } from 'vue';
import PickList from 'primevue/picklist';

type Item = { id?: string | null } & Record<string, any>;

interface Props {
	/** Toàn bộ mục chọn được (có `id`). */
	items: Item[];
	/** ID đã chọn. */
	modelValue?: string[] | null;
	/** Dòng chính / dòng phụ của 1 mục; dòng chính cũng là chuỗi để tìm. */
	labelOf: (item: Item) => string;
	subOf?: (item: Item) => string;
	/** Tên icon (component đăng ký toàn cục, vd 'ele-User') hiện trước mỗi mục; rỗng = không icon. */
	iconOf?: (item: Item) => string | undefined;
	/** Thụt lề (cấp) của mục ở cột trái khi không tìm — vd cây đơn vị đã trải phẳng. */
	indentOf?: (item: Item) => number;
	/** ID mục cha (danh sách dạng cây) ⇒ chuyển mục cha sang / trả về thì kéo theo toàn bộ mục con / cháu. */
	parentOf?: (item: Item) => string | null | undefined;
	sourceTitle: string;
	targetTitle: string;
	searchPlaceholder?: string;
	scrollHeight?: string;
}
const props = withDefaults(defineProps<Props>(), { modelValue: () => [], searchPlaceholder: '', scrollHeight: '260px' });
const emit = defineEmits<{
	(e: 'update:modelValue', ids: string[]): void;
	(e: 'change', ids: string[]): void;
}>();

const keyword = ref('');

/**
 * PickList tìm mục đang chọn bằng so sánh THAM CHIẾU (`findIndexInList` dùng `===`), mà selection của nó
 * nằm trong `data()` nên luôn là reactive proxy. Mục truyền vào là object thường (vd cây đơn vị trải phẳng
 * bằng computed) ⇒ proxy !== object ⇒ không tìm thấy (-1) ⇒ `splice(-1)` chuyển nhầm MỤC CUỐI danh sách.
 * Bọc `reactive()` (trả lại đúng proxy đã cache, không đổi nếu đã là proxy) để 2 bên luôn cùng tham chiếu.
 */
const allItems = computed(() => props.items.filter((i) => !!i.id).map((i) => reactive(i) as Item));
const byId = computed(() => new Map(allItems.value.map((i) => [i.id as string, i])));
/** Cột phải: đầy đủ, không lọc, giữ thứ tự chọn. ID không còn trong danh sách mục (đã xoá) thì bỏ qua. */
const target = computed(() => (props.modelValue ?? []).map((id) => byId.value.get(id)).filter((i): i is Item => !!i));
const source = computed(() => {
	const picked = new Set(props.modelValue ?? []);
	const kw = keyword.value.trim().toLowerCase();
	return allItems.value.filter((i) => !picked.has(i.id as string) && (!kw || `${props.labelOf(i)} ${props.subOf?.(i) ?? ''}`.toLowerCase().includes(kw)));
});
const lists = computed(() => [source.value, target.value]);

/** ID cha → ID các con trực tiếp (theo thứ tự trong `items`); chỉ dựng khi có `parentOf`. */
const childrenMap = computed(() => {
	const map = new Map<string, string[]>();
	if (!props.parentOf) return map;
	for (const i of allItems.value) {
		const pid = props.parentOf(i);
		if (pid) map.set(pid, [...(map.get(pid) ?? []), i.id as string]);
	}
	return map;
});

/** Thêm toàn bộ con / cháu của các mục vừa chuyển sang (bỏ qua mục đã có), theo thứ tự cây, nối cuối danh sách. */
function withDescendants(ids: string[], added: string[]) {
	const out = [...ids];
	const seen = new Set(ids);
	const addSubtree = (id: string) => {
		for (const child of childrenMap.value.get(id) ?? []) {
			if (!seen.has(child)) {
				seen.add(child);
				out.push(child);
			}
			addSubtree(child);
		}
	};
	added.forEach(addSubtree);
	return out;
}

/** Bỏ toàn bộ con / cháu của các mục vừa trả về cột trái. */
function withoutDescendants(ids: string[], removed: string[]) {
	const drop = new Set<string>();
	const collect = (id: string) => {
		for (const child of childrenMap.value.get(id) ?? []) {
			drop.add(child);
			collect(child);
		}
	};
	removed.forEach(collect);
	return drop.size ? ids.filter((id) => !drop.has(id)) : ids;
}

/** Cột phải luôn đầy đủ ⇒ lấy thẳng làm danh sách ID. */
function onListsChange(next: Item[][]) {
	let ids = (next?.[1] ?? []).map((i) => i.id as string).filter(Boolean);
	if (props.parentOf) {
		const prev = new Set(props.modelValue ?? []);
		const added = ids.filter((id) => !prev.has(id));
		if (added.length) ids = withDescendants(ids, added);
		// Trả mục cha về cột trái ⇒ trả luôn con / cháu
		const kept = new Set(ids);
		const removed = [...prev].filter((id) => !kept.has(id));
		if (removed.length) ids = withoutDescendants(ids, removed);
	}
	emit('update:modelValue', ids);
	emit('change', ids);
}

/* ── Kéo cột nút ở giữa để đổi chiều rộng 2 cột ── */
const RATIO_MIN = 0.2;
const RATIO_MAX = 0.8;
const rootRef = ref<HTMLElement>();
/** Tỉ lệ bề rộng cột trái trong phần còn lại (trừ cột nút). */
const ratio = ref(0.5);
const resizing = ref(false);
let controlsW = 0;

const controlsOf = (target: EventTarget | null) => (target as HTMLElement | null)?.closest('.p-picklist-transfer-controls') as HTMLElement | null;

function startResize(e: PointerEvent) {
	const controls = controlsOf(e.target);
	if (!controls || e.button !== 0 || (e.target as HTMLElement).closest('button')) return;
	e.preventDefault();
	controlsW = controls.offsetWidth;
	resizing.value = true;
	window.addEventListener('pointermove', onResize);
	window.addEventListener('pointerup', stopResize, { once: true });
}

function onResize(e: PointerEvent) {
	const rect = rootRef.value?.getBoundingClientRect();
	if (!rect || rect.width <= controlsW) return;
	const next = (e.clientX - rect.left - controlsW / 2) / (rect.width - controlsW);
	ratio.value = Math.min(RATIO_MAX, Math.max(RATIO_MIN, next));
}

function stopResize() {
	resizing.value = false;
	window.removeEventListener('pointermove', onResize);
	window.removeEventListener('pointerup', stopResize);
}

function onDblClick(e: MouseEvent) {
	if (controlsOf(e.target) && !(e.target as HTMLElement).closest('button')) ratio.value = 0.5;
}

/* ── Tooltip nút chuyển: nút đang rê chuột (có data-tip) ── */
const tipEl = shallowRef<HTMLElement>();

function onTipOver(e: MouseEvent) {
	tipEl.value = (e.target as HTMLElement).closest<HTMLElement>('.p-picklist-transfer-controls [data-tip]') ?? undefined;
}

/* ── Mục cha "dính" đầu cột trái khi cuộn ──
   Cột trái là danh sách phẳng (cây đã trải) nên không dùng được position: sticky theo nhánh ⇒
   đo mục đầu tiên đang thấy, hiện chuỗi mục cha của nó (đã cuộn khuất) đè lên đầu khung. */
const stickyHost = ref<HTMLElement | null>(null);
const sticky = reactive({ items: [] as Item[], top: 0, width: 0 });
let sourceListEl: HTMLElement | null = null;

const optionTop = (el: HTMLElement, list: HTMLElement) => el.getBoundingClientRect().top - list.getBoundingClientRect().top + list.scrollTop;

function updateSticky() {
	const list = sourceListEl;
	if (!list || !props.parentOf || keyword.value.trim()) {
		sticky.items = [];
		return;
	}
	const options = Array.from(list.querySelectorAll<HTMLElement>('.p-listbox-option'));
	const rowH = options[0]?.offsetHeight ?? 0;
	const tops = new Map(source.value.map((item, i) => [item.id as string, options[i] ? optionTop(options[i], list) : 0]));
	let chain: Item[] = [];
	// Dải mục cha che mất vài dòng đầu ⇒ đo lại theo chiều cao dải đến khi ổn định
	for (let pass = 0; pass < 3; pass++) {
		const y = list.scrollTop + chain.length * rowH;
		const idx = options.findIndex((o) => optionTop(o, list) + o.offsetHeight > y);
		const next: Item[] = [];
		let pid = idx >= 0 ? props.parentOf(source.value[idx]) : null;
		while (pid) {
			const parent = byId.value.get(pid);
			if (!parent) break;
			// Chỉ mục cha đã cuộn khuất (còn thấy thì không cần dính)
			if (!tops.has(pid) || (tops.get(pid) ?? 0) < y) next.unshift(parent);
			pid = props.parentOf(parent);
		}
		if (next.length === chain.length) {
			chain = next;
			break;
		}
		chain = next;
	}
	sticky.items = chain;
	sticky.top = list.offsetTop;
	sticky.width = list.clientWidth;
}

/** Bấm mục cha đang dính ⇒ cuộn về đúng mục đó. */
function scrollToItem(item: Item) {
	const list = sourceListEl;
	const idx = source.value.findIndex((i) => i.id === item.id);
	const option = list?.querySelectorAll<HTMLElement>('.p-listbox-option')[idx];
	if (list && option) list.scrollTop = optionTop(option, list);
}

onMounted(() => {
	if (!props.parentOf) return;
	sourceListEl = rootRef.value?.querySelector<HTMLElement>('.p-picklist-source-list-container .p-listbox-list-container') ?? null;
	stickyHost.value = (sourceListEl?.parentElement as HTMLElement | null) ?? null;
	sourceListEl?.addEventListener('scroll', updateSticky, { passive: true });
});
watch([source, keyword], () => nextTick(updateSticky));

onBeforeUnmount(() => {
	stopResize();
	sourceListEl?.removeEventListener('scroll', updateSticky);
});

defineExpose({
	/** Xoá ô tìm (vd khi mở lại dialog). */
	reset: () => {
		keyword.value = '';
		ratio.value = 0.5;
		if (sourceListEl) sourceListEl.scrollTop = 0;
	},
});
</script>

<style scoped lang="scss">
.tpl {
	width: 100%;
	/* Màu chủ đạo của hệ thống (Element Plus) thay cho xanh lá mặc định của PrimeVue Aura:
	   biến token của PrimeVue đã tính sẵn ở :root ⇒ phải ghi đè đúng biến của từng component (mục chọn, nút chuyển) */
	--p-listbox-option-focus-background: var(--el-color-primary-light-9);
	--p-listbox-option-focus-color: var(--el-text-color-primary);
	--p-listbox-option-selected-background: var(--el-color-primary-light-8);
	--p-listbox-option-selected-color: var(--el-color-primary);
	--p-listbox-option-selected-focus-background: var(--el-color-primary-light-7);
	--p-listbox-option-selected-focus-color: var(--el-color-primary);
	--p-listbox-checkmark-color: var(--el-color-primary);
	--p-button-primary-background: var(--el-color-primary);
	--p-button-primary-border-color: var(--el-color-primary);
	--p-button-primary-hover-background: var(--el-color-primary-light-3);
	--p-button-primary-hover-border-color: var(--el-color-primary-light-3);
	--p-button-primary-active-background: var(--el-color-primary-dark-2);
	--p-button-primary-active-border-color: var(--el-color-primary-dark-2);
	--p-button-primary-color: #fff;
	--p-button-primary-hover-color: #fff;
	--p-button-primary-active-color: #fff;
	--p-button-primary-focus-ring-color: var(--el-color-primary);
	:deep(.p-picklist) {
		width: 100%;
	}
	/* 2 cột chia phần còn lại theo tỉ lệ kéo */
	:deep(.p-picklist-source-list-container) {
		flex: var(--tpl-left) 1 0%;
		min-width: 0;
	}
	:deep(.p-picklist-target-list-container) {
		flex: var(--tpl-right) 1 0%;
		min-width: 0;
	}
	/* Cột nút ở giữa: tay nắm kéo ngang, vạch dọc phía sau các nút.
	   Lưới 2 cột hai bên vạch: nút chuyển sang (→) cột phải, nút trả về (←) cột trái — nằm gọn trong cột, không đè lên 2 khung */
	:deep(.p-picklist-transfer-controls) {
		position: relative;
		align-self: stretch;
		display: grid;
		grid-template-columns: repeat(2, auto);
		align-content: center;
		column-gap: 8px;
		cursor: col-resize;
		&::before {
			content: '';
			position: absolute;
			top: 8px;
			bottom: 8px;
			left: 50%;
			width: 2px;
			border-radius: 2px;
			transform: translateX(-50%);
			background: var(--el-border-color-lighter);
			pointer-events: none;
			transition: background 0.15s, width 0.15s;
		}
		&:hover::before {
			width: 3px;
			background: var(--el-color-primary);
		}
		.p-button {
			position: relative;
			cursor: pointer;
		}
		.p-button:nth-of-type(-n + 2) {
			grid-column: 2;
		}
		.p-button:nth-of-type(n + 3) {
			grid-column: 1;
		}
	}
	/* Khung cột trái làm mốc cho dải mục cha dính đầu */
	:deep(.p-picklist-source-list-container .p-listbox) {
		position: relative;
	}
	&.is-resizing {
		cursor: col-resize;
		user-select: none;
		:deep(.p-picklist-transfer-controls)::before {
			width: 3px;
			background: var(--el-color-primary);
		}
	}
}
.tpl-head {
	display: flex;
	align-items: center;
	gap: 8px;
	width: 100%;
	font-size: 13px;
	font-weight: 600;
	> span {
		flex-shrink: 0;
	}
	.el-input {
		flex: 1;
		min-width: 0;
	}
}
/* Form-item bao ngoài báo lỗi (chưa chọn mục) ⇒ ô tìm không đỏ theo */
.el-form-item.is-error .tpl-head :deep(.el-input__wrapper:not(.is-focus)) {
	box-shadow: 0 0 0 1px var(--el-input-border-color, var(--el-border-color)) inset;
}
.el-form-item.is-error .tpl-head :deep(.el-input__wrapper.is-focus) {
	box-shadow: 0 0 0 1px var(--el-input-focus-border-color, var(--el-color-primary)) inset;
}
/* Dải mục cha dính đầu cột trái: nền đặc che các mục cuộn bên dưới */
.tpl-sticky {
	position: absolute;
	left: 0;
	z-index: 2;
	background: var(--el-bg-color);
	border-bottom: 1px solid var(--el-border-color-lighter);
	box-shadow: 0 4px 6px -4px rgba(0, 0, 0, 0.15);
	.tpl-sticky-row {
		padding: var(--p-listbox-option-padding, 0.5rem 0.75rem);
		cursor: pointer;
		font-weight: 600;
		&:hover {
			background: var(--el-fill-color-light);
		}
	}
}
.tpl-option {
	display: flex;
	align-items: center;
	gap: 10px;
	min-width: 0;
	line-height: 1.35;
	.tpl-option-icon {
		display: inline-flex;
		align-items: center;
		justify-content: center;
		flex-shrink: 0;
		width: 28px;
		height: 28px;
		border-radius: 8px;
		font-size: 15px;
		color: var(--el-color-primary);
		background: var(--el-color-primary-light-9);
	}
	.tpl-option-text {
		display: flex;
		flex-direction: column;
		flex: 1;
		min-width: 0;
	}
	.tpl-option-name {
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}
	.tpl-option-sub {
		font-size: 12px;
		color: var(--el-text-color-secondary);
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}
}
</style>
