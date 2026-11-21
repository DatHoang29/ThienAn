<template>
	<component
		:is="isEmbedded ? 'section' : 'main'"
		ref="workspace"
		:data-workspace-tab="tab"
		class="data-workspace workspace-ui"
		:class="{ 'layout-padding': !isEmbedded, 'data-workspace-embedded': isEmbedded, 'data-workspace-filters-top': filtersPlacement === 'top' }"
		tabindex="-1"
		:aria-label="title"
		@pointerdown="focusWorkspace"
	>
		<header v-if="!isEmbedded" class="workspace-page-header workspace-header">
			<div class="workspace-page-heading workspace-heading">
				<el-icon class="heading-symbol"><Monitor /></el-icon>
				<h1>{{ title }}</h1>
			</div>
			<slot name="tabs" />
			<div class="header-actions">
				<el-button :icon="Filter" :aria-expanded="filtersVisible" :aria-controls="`${id}-search`" @click="setPanelVisible('left', !filtersVisible)">
					{{ $t('lz.button.base.search') }}
				</el-button>
				<el-button v-if="slots.detail" :icon="Document" :aria-expanded="detailVisible" :aria-controls="`${id}-detail`" @click="setPanelVisible('right', !detailVisible)">
					{{ $t('lz.label.base.detail') }}
				</el-button>
				<el-button :icon="Refresh" :loading="state.loading" :disabled="state.saving" @click="handleRefresh">
					{{ $t('lz.button.base.refresh') }}
				</el-button>

				<slot name="header-actions" />
			</div>
		</header>

		<slot name="overview" :active="pageActive" />
		<div v-if="configError" class="inline-notice warning">
			<el-icon><Warning /></el-icon>
			<span>{{ configError }}</span>
			<el-button text @click="loadCatalog(true)">
				{{ $t('lz.button.base.refresh') }}
			</el-button>
		</div>
		<div v-if="state.pendingUpdates" class="inline-notice">
			<el-icon><Refresh /></el-icon>
			<span>{{ $t('lz.message.base.updatesAvailable') }}</span>
			<el-button text :disabled="state.saving || state.loading" @click="handleRefresh">
				{{ $t('lz.button.base.refresh') }}
			</el-button>
		</div>

		<div
			:id="`${id}-content`"
			ref="workspaceContent"
			class="workspace-content"
			:class="{ 'workspace-filters-top': filtersPlacement === 'top' }"
			:style="panelStyle"
			role="region"
			:aria-label="$t('lz.label.base.listFor', { name: tabLabel })"
		>
			<slot name="filters" :visible="filtersVisible" :hide="hideFilters" />

			<PanelSplitter
				:expand-label="$t('lz.label.base.extend')"
				:collapse-label="$t('lz.button.base.collapse')"
				side="left"
				:orientation="filtersPlacement === 'top' ? 'horizontal' : 'vertical'"
				:label="$t('lz.button.base.search')"
				:resize-hint="$t('lz.label.base.resizePanel', { name: $t('lz.button.base.search') })"
				:controls="`${id}-search`"
				:expanded="filtersVisible"
				:width="filterPanelSize"
				:min="filterPanelMin"
				:max="filterPanelMax"
				@resize="resizePanel('left', $event)"
				@step="stepPanel('left', $event)"
			/>
			<div class="workspace-body">
				<section ref="recordsPanel" class="records-panel workspace-panel" :aria-label="$t('lz.label.base.listFor', { name: recordsLabel })">
					<header class="records-toolbar workspace-panel-heading">
						<div class="records-summary">
							<span class="records-count"
								>{{ $t('lz.label.base.total') }}: <strong>{{ error ? '—' : n(state.total) }}</strong></span
							>
							<span v-if="state.lastLoaded" class="records-updated">
								<el-icon aria-hidden="true"><Clock /></el-icon>
								<span>{{ $t('lz.entity.base.lastUpdate') }}:</span>
								<time>{{ lastLoadedTime }}</time>
							</span>
							<div v-if="slots['record-tools']" class="records-tools">
								<slot name="record-tools" />
							</div>
						</div>
						<slot name="record-actions" />
						<!-- Chế độ nhúng (tab của khung tab menu chung): không có header riêng ⇒ nút của trang (Đăng ký sự cố…) và
						     Tìm kiếm / Chi tiết / Làm mới nằm ngay trên thanh tiêu đề của bảng. -->
						<template v-if="isEmbedded">
							<!-- Nút của trang giữ cỡ mặc định trong code trang; ở thanh tiêu đề bảng ép về cỡ small cho bằng các nút bên cạnh. -->
							<el-config-provider v-if="slots['header-actions']" size="small">
								<div class="records-page-actions">
									<slot name="header-actions" />
								</div>
							</el-config-provider>
							<el-button class="workspace-config-button" size="small" :icon="Filter" :aria-expanded="filtersVisible" :aria-controls="`${id}-search`" @click="setPanelVisible('left', !filtersVisible)">
								{{ $t('lz.button.base.search') }}
							</el-button>
							<el-button v-if="slots.detail" class="workspace-config-button" size="small" :icon="Document" :aria-expanded="detailVisible" :aria-controls="`${id}-detail`" @click="setPanelVisible('right', !detailVisible)">
								{{ $t('lz.label.base.detail') }}
							</el-button>
							<el-button class="workspace-config-button" size="small" :icon="Refresh" :loading="state.loading" :disabled="state.saving" @click="handleRefresh">
								{{ $t('lz.button.base.refresh') }}
							</el-button>
						</template>
						<el-button class="workspace-config-button" size="small" :icon="Setting" @click="openColumnSettings">
							{{ $t('lz.button.base.columnSetting') }}
						</el-button>
					</header>
					<div class="records-scroll" :aria-busy="state.loading">
						<vxe-grid ref="workspaceGrid" class="xGrid-style" v-bind="gridOptions" :data="state.rows" :loading="state.loading" v-on="gridEvents">
							<template v-for="slotName in columnSlotNames" :key="slotName" #[slotName]="scope">
								<slot :name="slotName" v-bind="scope" />
							</template>
							<template #empty>
								<div class="list-empty" :role="error ? 'alert' : undefined">
									<el-icon :size="28"><component :is="error ? Warning : Search" /></el-icon>
									<h3>
										{{ error ? $t('lz.message.base.errorFetchingData') : $t('lz.label.base.noData') }}
									</h3>
									<p>{{ error || $t('lz.message.base.searchHint') }}</p>
									<el-button v-if="error" :icon="Refresh" @click="load()">
										{{ $t('lz.button.base.retry') }}
									</el-button>
									<el-button v-else @click="resetQuery">
										{{ $t('lz.button.base.reset') }}
									</el-button>
								</div>
							</template>
						</vxe-grid>
					</div>
				</section>

				<PanelSplitter
					v-if="slots.detail"
					:expand-label="$t('lz.label.base.extend')"
					:collapse-label="$t('lz.button.base.collapse')"
					side="right"
					:label="$t('lz.label.base.detail')"
					:resize-hint="$t('lz.label.base.resizePanel', { name: $t('lz.label.base.detail') })"
					:controls="`${id}-detail`"
					:expanded="detailVisible"
					:width="detailPanelSize"
					:min="260"
					:max="detailPanelMax"
					@resize="resizePanel('right', $event)"
					@step="stepPanel('right', $event)"
				/>
				<slot name="detail" :visible="detailVisible" :active="pageActive" />
			</div>
		</div>
		<slot name="dialogs" />
	</component>
</template>

<script setup lang="ts" name="dataWorkspace" generic="Row extends object">
import { computed, inject, nextTick, onActivated, onBeforeUnmount, onDeactivated, onMounted, reactive, ref, useSlots, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { useDateFormat, useResizeObserver } from '@vueuse/core';
import { Clock, Document, Filter, Monitor, Refresh, Search, Setting, Warning } from '@element-plus/icons-vue';
import type { VxeGridInstance, VxeGridListeners, VxeGridProps } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { DateTimeFormatEnum } from '/@/types/enums/baseEnum';
import { useWorkspaceKeyboard, WORKSPACE_PAGE_KEY } from './workspace';
import type { WorkspaceController, WorkspaceSort } from './workspace';
import PanelSplitter from './PanelSplitter.vue';

interface Props {
	id: string;
	title: string;
	embedded?: boolean;
	filtersInitiallyVisible?: boolean;
	filtersPlacement?: 'left' | 'top';
	rowKeyField?: string;
	rowHeight?: number;
	actionField?: string;
	active?: boolean;
	tab: string;
	controller: WorkspaceController<Row>;
	columns: () => NonNullable<VxeGridProps<Row>['columns']>;
	tabLabel: string;
	recordsLabel: string;
	gridId: string;
	columnLayoutVersion?: string;
	defaultSort?: WorkspaceSort;
	refreshRelated?: (force?: boolean) => Promise<void>;
	navigateEvidence?: (direction: -1 | 1) => boolean;
	/** Cấu hình cây VXE cho dữ liệu lồng `children`; bỏ trống với bảng phẳng. */
	treeConfig?: VxeGridProps<Row>['treeConfig'];
	/** Tắt phân trang khi API trả toàn bộ danh sách. */
	pagination?: boolean;
}
const props = withDefaults(defineProps<Props>(), { rowKeyField: 'id', rowHeight: 66, actionField: 'actions', active: true, filtersPlacement: 'left', pagination: true });
// Nằm trong vỏ trang khác (tab của khung tab menu chung / WorkspacePage) ⇒ tự chạy chế độ nhúng: không vẽ <main> + header
// lần hai; nút Làm mới chuyển xuống thanh công cụ của bảng, khung tìm kiếm / chi tiết mở bằng thanh chia.
const nestedInPage = inject(WORKSPACE_PAGE_KEY, false);
const isEmbedded = computed(() => props.embedded || nestedInPage);
const emit = defineEmits<{ selectionChange: [rows: Row[]] }>();

// Dependencies and component state
const { t: $t, n } = useI18n();
const slots = useSlots();
const controller = props.controller;
const state = controller.state;
const { selected, error, optionsError: configError, selectRow, load, reset: resetQuery, move, reloadOptions: loadCatalog, changePage, changeSort } = controller;
const workspace = ref<HTMLElement>();
const workspaceGrid = ref<VxeGridInstance<Row>>();
const recordsPanel = ref<HTMLElement>();
const recordsWidth = ref(0);
const viewState = reactive({ pageActive: true });

const filtersVisible = ref(props.filtersInitiallyVisible ?? false);
const detailVisible = ref(!!slots.detail);
const workspaceContent = ref<HTMLElement>();
const contentWidth = ref(0);
const contentHeight = ref(0);
const initialFilterHeight = ref(44);
const filterHeight = ref(44);
const initialFilterWidth = ref(224);
const initialDetailWidth = ref(380);
const filterWidth = ref(224);
const detailWidth = ref(380);
let gridLayoutFrame = 0;

// Derived presentation
const pageActive = computed(() => props.active && viewState.pageActive);
const lastLoadedTime = useDateFormat(() => state.lastLoaded ?? '', DateTimeFormatEnum.TimeDefault);
// VXE reloads columns when their array changes, which also resets header sorting.
const gridColumns = computed(() => props.columns().map((column) => ({ headerAlign: 'center' as const, ...column })));
const columnSlotNames = computed(() => [...new Set(gridColumns.value.flatMap((column) => (typeof column.slots?.default === 'string' ? [column.slots.default] : [])))]);

// Keep both labeled handles available even when the panels are hidden.
const splitterWidth = 28;
const filterPanelMin = computed(() => (props.filtersPlacement === 'top' ? 44 : 176));
const filterPanelMax = computed(() =>
	props.filtersPlacement === 'top'
		? Math.max(filterPanelMin.value, Math.min(initialFilterHeight.value * 1.5, (contentHeight.value || 600) - 240 - splitterWidth))
		: Math.max(176, Math.min(initialFilterWidth.value * 1.5, (contentWidth.value || 1400) - (detailVisible.value ? 260 : 0) - splitterWidth * 2 - 420))
);
const filterPanelSize = computed(() => (filtersVisible.value ? Math.min(props.filtersPlacement === 'top' ? filterHeight.value : filterWidth.value, filterPanelMax.value) : 0));
const detailPanelMax = computed(() =>
	Math.max(260, Math.min(initialDetailWidth.value * 1.5, (contentWidth.value || 1400) - (props.filtersPlacement === 'top' ? 0 : filterPanelSize.value) - splitterWidth * 2 - 420))
);
const detailPanelSize = computed(() => (detailVisible.value ? Math.min(detailWidth.value, detailPanelMax.value) : 0));

const panelStyle = computed(() => ({
	'--workspace-splitter-width': `${splitterWidth}px`,
	'--filter-panel-width': `${filterPanelSize.value}px`,
	'--filter-panel-height': `${filterPanelSize.value}px`,
	'--detail-panel-width': `${detailPanelSize.value}px`,
	'--detail-splitter-width': slots.detail ? `${splitterWidth}px` : '0px',
	'--filter-font-increment': props.filtersPlacement === 'top' ? '0px' : panelFontIncrement(filterPanelSize.value, initialFilterWidth.value),
	'--detail-font-increment': panelFontIncrement(detailPanelSize.value, initialDetailWidth.value),
}));

// Table configuration and events
syncColumnLayout();

function syncColumnLayout() {
	if (!props.columnLayoutVersion) return;
	const versionKey = `${props.gridId}:columnLayoutVersion`;
	try {
		if (localStorage.getItem(versionKey) === props.columnLayoutVersion) return;
		// Clear widths from the previous layout while preserving visibility and other grids.
		const stored = localStorage.getItem('VXE_CUSTOM_STORE');
		if (stored) {
			const layouts = JSON.parse(stored);
			if (layouts?.[props.gridId]?.resizableData) {
				delete layouts[props.gridId].resizableData;
				localStorage.setItem('VXE_CUSTOM_STORE', JSON.stringify(layouts));
			}
		}
		localStorage.setItem(versionKey, props.columnLayoutVersion);
	} catch {
		// Storage can be unavailable; the grid can still render its default columns.
	}
}

const tableOptions = useVxeTable<Row>(
	{ id: props.gridId, name: props.recordsLabel, columns: [] },
	{
		// The caller owns data loading; the workspace renders rows and delegates table events.
		proxyConfig: { enabled: false },
		toolbarConfig: { enabled: false },
		columnConfig: { resizable: true },
		rowConfig: { keyField: props.rowKeyField, height: props.rowHeight, isCurrent: true, isHover: true },
	}
);
const pagerConfig = computed<NonNullable<VxeGridProps<Row>['pagerConfig']>>(() => ({
	...tableOptions.pagerConfig,
	enabled: props.pagination,
	currentPage: state.page,
	pageSize: state.pageSize,
	total: state.total,
	pagerCount: recordsWidth.value >= 900 ? 7 : 5,
	layouts:
		recordsWidth.value >= 900
			? ['PrevJump', 'PrevPage', 'Number', 'NextPage', 'NextJump', 'Sizes', 'FullJump', 'PageCount', 'Total']
			: recordsWidth.value >= 640
				? ['PrevPage', 'Number', 'NextPage', 'Sizes', 'Jump', 'PageCount']
				: ['PrevPage', 'Jump', 'PageCount', 'NextPage', 'Sizes'],
}));
const gridOptions = computed<VxeGridProps<Row>>(() => ({
	...tableOptions,
	showOverflow: 'tooltip',
	showHeaderOverflow: 'tooltip',
	sortConfig: {
		...tableOptions.sortConfig,
		defaultSort: props.defaultSort,
	},
	pagerConfig: pagerConfig.value,
	seqConfig: { startIndex: props.pagination ? (state.page - 1) * state.pageSize : 0 },
	...(props.treeConfig ? { treeConfig: props.treeConfig } : {}),
	columns: gridColumns.value,
}));
const gridEvents: VxeGridListeners<Row> = {
	cellClick({ row, column }) {
		if (!state.saving && !state.loading && column.field !== props.actionField) void handleView(row);
	},
	sortChange({ field, order }) {
		void changeSort(field, order || null);
	},
	pageChange({ currentPage, pageSize }) {
		if (state.saving || state.loading) return;
		state.pageSize = pageSize;
		void changePage(currentPage);
	},
	checkboxChange: emitSelection,
	checkboxAll: emitSelection,
};

// Dòng được chọn bằng checkbox trên trang hiện tại.
function emitSelection() {
	emit('selectionChange', workspaceGrid.value?.getCheckboxRecords() ?? []);
}

async function clearSelection() {
	await workspaceGrid.value?.clearCheckboxRow();
	await workspaceGrid.value?.clearCheckboxReserve();
	emitSelection();
}

async function setTreeExpand(expanded: boolean) {
	if (expanded) await workspaceGrid.value?.setAllTreeExpand(true);
	else await workspaceGrid.value?.clearTreeExpand();
}

defineExpose({ clearSelection, setTreeExpand });

// Display formatting
function panelFontIncrement(width: number, initial: number) {
	// Increase text by at most 2px as a panel grows to 1.5x its initial width.
	return `${Math.min(2, Math.max(0, (width / initial - 1) * 4)).toFixed(2)}px`;
}

// Record navigation and panel actions
function openColumnSettings() {
	void workspaceGrid.value?.openCustom();
}

function handleView(row: Row) {
	if (state.saving || state.loading) return;
	if (slots.detail && !detailVisible.value) setPanelVisible('right', true);
	return selectRow(row);
}

function hideFilters() {
	setPanelVisible('left', false);
}

function focusWorkspace(event: PointerEvent) {
	const target = event.target as HTMLElement;
	if (!target.closest('button, a, input, textarea, select, [role="separator"], [role="tab"], [role="combobox"], [role="dialog"], [contenteditable="true"]'))
		workspace.value?.focus({ preventScroll: true });
}

// Data refresh
async function handleRefresh() {
	await Promise.all([controller.refresh(), props.refreshRelated?.(false)]);
}

function handleKeydown(event: KeyboardEvent) {
	const target = event.target as HTMLElement;
	if (
		event.defaultPrevented ||
		target.closest('input, textarea, select, .scroll-legend, [contenteditable="true"], [role="separator"], [role="combobox"], [role="dialog"], [role="tab"]') ||
		event.altKey ||
		event.metaKey
	)
		return;
	if (event.key === 'ArrowLeft' || event.key === 'ArrowRight') {
		if (event.ctrlKey || event.shiftKey) return;
		if (props.navigateEvidence?.(event.key === 'ArrowLeft' ? -1 : 1)) event.preventDefault();
		return;
	}
	if (event.key === 'ArrowUp' || event.key === 'ArrowDown') {
		if (event.ctrlKey || event.shiftKey) return;
		event.preventDefault();
		const direction = event.key === 'ArrowUp' ? -1 : 1;
		void (props.treeConfig ? moveInTree(direction) : move(direction));
	}
}

// Bảng cây: di chuyển theo thứ tự các dòng đang hiển thị (chỉ đi vào node đang mở rộng).
function visibleTreeRows(rows: Row[]): Row[] {
	const childrenField = props.treeConfig?.childrenField || props.treeConfig?.children || 'children';
	return rows.flatMap((row) => {
		const children = (row as Record<string, unknown>)[childrenField] as Row[] | null | undefined;
		return [row, ...(children?.length && workspaceGrid.value?.isTreeExpandByRow(row) ? visibleTreeRows(children) : [])];
	});
}

async function moveInTree(direction: -1 | 1) {
	const rows = visibleTreeRows(state.rows);
	const index = rows.findIndex((row) => String((row as Record<string, unknown>)[props.rowKeyField]) === state.selectedKey);
	const row = rows[index + direction];
	if (row) await selectRow(row);
}

// Panel sizing and grid layout
function panelState(side: 'left' | 'right') {
	return side === 'left'
		? {
				visible: filtersVisible,
				size: props.filtersPlacement === 'top' ? filterHeight : filterWidth,
				initial: props.filtersPlacement === 'top' ? initialFilterHeight : initialFilterWidth,
				actual: filterPanelSize,
				max: filterPanelMax,
			}
		: {
				visible: detailVisible,
				size: detailWidth,
				initial: initialDetailWidth,
				actual: detailPanelSize,
				max: detailPanelMax,
			};
}

function setPanelVisible(side: 'left' | 'right', visible: boolean) {
	const panel = panelState(side);
	panel.visible.value = visible;
	panel.size.value = panel.initial.value;
}

function stepPanel(side: 'left' | 'right', direction: 'grow' | 'shrink') {
	const panel = panelState(side);
	const normal = Math.min(panel.initial.value, panel.max.value);
	if (direction === 'grow') {
		if (panel.visible.value && panel.actual.value >= panel.max.value - 1) return;
		const width = !panel.visible.value || panel.actual.value < normal - 1 ? normal : panel.max.value;
		resizePanel(side, width);
	} else if (panel.visible.value) {
		if (panel.actual.value > normal + 1) resizePanel(side, normal);
		else setPanelVisible(side, false);
	}
}

function resizePanel(side: 'left' | 'right', width: number) {
	const panel = panelState(side);
	if (width <= 0) {
		setPanelVisible(side, false);
		return;
	}
	panel.visible.value = true;
	panel.size.value = width;
}

function scheduleGridLayout() {
	if (gridLayoutFrame) return;
	gridLayoutFrame = requestAnimationFrame(() => {
		gridLayoutFrame = 0;
		void nextTick().then(() => workspaceGrid.value?.recalculate());
	});
}

function syncContentSize() {
	const content = workspaceContent.value;
	if (!pageActive.value || !content || content.clientWidth <= 0 || content.clientHeight <= 0) return;
	contentWidth.value = content.clientWidth;
	contentHeight.value = content.clientHeight;
	if (recordsPanel.value?.clientWidth) recordsWidth.value = recordsPanel.value.clientWidth;
	if (props.filtersPlacement === 'top') {
		const previousHeight = initialFilterHeight.value;
		const nextHeight = contentWidth.value < 480 ? 144 : contentWidth.value < 640 ? 112 : contentWidth.value < 900 ? 80 : 44;
		initialFilterHeight.value = nextHeight;
		filterHeight.value = filterHeight.value === previousHeight ? nextHeight : Math.min(filterHeight.value, filterPanelMax.value);
	}
	scheduleGridLayout();
}

// Watchers and lifecycle
useResizeObserver(workspaceContent, syncContentSize);
useResizeObserver(recordsPanel, () => {
	const width = recordsPanel.value?.clientWidth;
	if (!pageActive.value || !width) return;
	recordsWidth.value = width;
	scheduleGridLayout();
});
onMounted(() => {
	syncContentSize();
	initialFilterWidth.value = window.innerWidth <= 1200 ? 204 : 224;
	initialDetailWidth.value = window.innerWidth <= 1200 ? 300 : Math.min(380, Math.max(300, (contentWidth.value - initialFilterWidth.value - splitterWidth * 2) * 0.29));
	filterWidth.value = initialFilterWidth.value;
	detailWidth.value = initialDetailWidth.value;
});

onBeforeUnmount(() => {
	if (gridLayoutFrame) cancelAnimationFrame(gridLayoutFrame);
});

useWorkspaceKeyboard(workspace, props.tab, state, pageActive, handleKeydown);
onActivated(async () => {
	viewState.pageActive = true;
	await nextTick();
	syncContentSize();
});
onDeactivated(() => {
	viewState.pageActive = false;
});
watch([filterPanelSize, detailPanelSize], scheduleGridLayout);
watch([gridColumns, () => props.defaultSort], async () => {
	await nextTick();
	// The controller may restore a default sort when the user clears a column.
	const sort = props.defaultSort;
	if (sort) await workspaceGrid.value?.sort(sort.field, sort.order);
});
watch([() => state.selectedKey, () => state.rows], async () => {
	await nextTick();
	if (selected.value) {
		await workspaceGrid.value?.setCurrentRow(selected.value);
		await workspaceGrid.value?.scrollToRow(selected.value);
	} else await workspaceGrid.value?.clearCurrentRow();
});
// Giữ danh sách chọn của trang cha khớp với các dòng đang hiển thị sau khi tải lại/đổi trang.
watch(
	() => state.rows,
	async () => {
		await nextTick();
		emitSelection();
	}
);
watch(
	() => state.dataRevision,
	() => {
		void props.refreshRelated?.();
	}
);
</script>

<style lang="scss">
.data-workspace .header-actions {
	display: flex;
	align-items: center;
	gap: 8px;
	margin-left: auto;
	flex-shrink: 0;
}
/* Nút của trang đặt trên thanh tiêu đề của bảng (chế độ nhúng): cùng cỡ với các nút công cụ bên cạnh. */
.data-workspace .records-page-actions {
	display: flex;
	align-items: center;
	gap: 6px;
}
.data-workspace .records-page-actions .el-button {
	flex-shrink: 0;
}
.data-workspace .inline-notice {
	display: flex;
	flex-shrink: 0;
	gap: 8px;
	align-items: center;
	background: var(--workspace-primary-soft);
	color: var(--workspace-primary-dark);
	border-radius: 6px;
	padding: 3px 10px;
	font-size: 12px;
}
.data-workspace .inline-notice > span {
	flex: 1;
}
.data-workspace .inline-notice.warning {
	background: var(--workspace-warning-soft);
	color: var(--workspace-warning);
}
.data-workspace .workspace-content {
	display: grid;
	flex: 1;
	grid-template-columns: var(--filter-panel-width, 0px) var(--workspace-splitter-width, 28px) minmax(0, 1fr);
	grid-template-rows: minmax(0, 1fr);
	gap: 0;
	min-height: 360px;
	min-width: 0;
}
.data-workspace .workspace-content > .filter-card {
	grid-column: 1;
}
.data-workspace .workspace-content > .panel-splitter {
	grid-column: 2;
}
.data-workspace .workspace-content > .workspace-body {
	grid-column: 3;
}
.data-workspace .workspace-body {
	display: grid;
	min-width: 0;
	min-height: 0;
	grid-template-columns: minmax(0, 1fr) var(--detail-splitter-width, 28px) var(--detail-panel-width, 380px);
	grid-template-rows: minmax(0, 1fr);
	gap: 0;
}
.data-workspace .workspace-body > .records-panel {
	grid-column: 1;
}
.data-workspace .workspace-body > .panel-splitter {
	grid-column: 2;
}
.data-workspace .workspace-body > .detail-panel {
	grid-column: 3;
}
.data-workspace .records-panel {
	display: flex;
	flex-direction: column;
}
.data-workspace .records-toolbar {
	justify-content: space-between;
	gap: 8px;
}
.data-workspace .records-summary {
	display: flex;
	align-items: center;
	flex-wrap: wrap;
	gap: 4px 16px;
	min-width: 0;
	color: var(--workspace-muted);
	font-size: 13px;
	font-weight: 400;
}
.data-workspace .records-count {
	white-space: nowrap;
}
.data-workspace .records-count strong {
	color: var(--workspace-text);
	font-weight: 650;
}
.data-workspace .records-updated {
	display: flex;
	align-items: center;
	flex-wrap: wrap;
	gap: 4px;
	min-width: 0;
	color: var(--workspace-muted);
	font-size: 13px;
	font-weight: 400;
}
.data-workspace .records-updated time {
	white-space: nowrap;
}
.data-workspace .records-tools {
	display: flex;
	align-items: center;
	flex-shrink: 0;
	gap: 8px;
}
.data-workspace .workspace-config-button {
	flex-shrink: 0;
}
.data-workspace .records-summary {
	margin-right: auto;
}
.data-workspace.data-workspace-embedded {
	position: relative;
	flex: 1;
	min-height: 0;
	padding: 0;
	overflow: hidden;
	background: transparent;
}
.data-workspace .records-scroll {
	display: flex;
	flex-direction: column;
	flex: 1;
	min-height: 0;
	overflow: hidden;
}
.data-workspace .records-scroll > .xGrid-style {
	flex: 1;
	min-height: 0;
}
.data-workspace .vxe-pager--wrapper {
	display: flex;
	align-items: center;
	justify-content: flex-end;
	flex-wrap: nowrap;
	min-width: 0;
	white-space: nowrap;
}
.data-workspace .vxe-pager--wrapper > * {
	flex-shrink: 0;
}
.data-workspace .records-scroll .vxe-body--column {
	font-size: 14px;
}
.data-workspace .records-scroll .vxe-table .vxe-body--column .vxe-cell {
	word-break: normal;
	overflow-wrap: normal;
}
.data-workspace .list-empty {
	display: flex;
	flex-direction: column;
	align-items: center;
	justify-content: center;
	text-align: center;
	height: 100%;
	min-height: 180px;
	padding: 20px;
	gap: 10px;
	color: var(--workspace-muted);
}
.data-workspace .list-empty h3 {
	color: var(--workspace-text);
	font-size: 15px;
}
.data-workspace .list-empty p {
	font-size: 12px;
	max-width: 300px;
}
@media (max-width: 1200px) {
	.data-workspace .header-actions {
		flex-wrap: wrap;
		justify-content: flex-end;
	}
}
@media (max-width: 960px) {
	.data-workspace .workspace-page-header {
		flex-wrap: wrap;
		gap: 6px 12px;
	}

	.data-workspace .workspace-tabbar {
		padding-left: 12px;
	}

	.data-workspace .workspace-content {
		display: flex;
		flex-direction: column;
	}

	.data-workspace .workspace-body {
		grid-template-columns: 1fr;
		grid-template-rows: auto;
		gap: 8px;
		flex: initial;
	}

	.data-workspace .workspace-body > .detail-panel {
		grid-column: 1;
	}

	.data-workspace .records-panel {
		height: 460px;
	}
}
@media (max-width: 600px) {
	.data-workspace .workspace-page-header {
		flex-wrap: wrap;
		gap: 6px;
	}

	.data-workspace .header-actions {
		margin-left: auto;
	}

	.data-workspace .header-actions .el-button {
		padding: 6px 8px;
	}
}
.data-workspace {
	--workspace-panel-font-increment: 0px;
}
.data-workspace h1,
.data-workspace h2,
.data-workspace h3,
.data-workspace p {
	margin: 0;
}
.data-workspace h2 {
	font-size: 13px;
	font-weight: 650;
}
.data-workspace:focus {
	outline: none;
}
@media (max-width: 960px) {
	.data-workspace {
		position: relative;
		height: auto;
		min-height: 100%;
		overflow: visible;
	}
}
.data-workspace .workspace-content.workspace-filters-top {
	display: grid;
	grid-template-columns: minmax(0, 1fr);
	grid-template-rows: var(--filter-panel-height, 44px) var(--workspace-splitter-width, 28px) minmax(0, 1fr);
	min-height: 360px;
}
.data-workspace .workspace-filters-top > .filter-card {
	grid-column: 1;
	grid-row: 1;
	max-height: none;
}
.data-workspace .workspace-filters-top > .panel-splitter {
	grid-column: 1;
	grid-row: 2;
}
.data-workspace .workspace-filters-top > .workspace-body {
	grid-column: 1;
	grid-row: 3;
	grid-template-rows: minmax(0, 1fr);
}
.data-workspace .workspace-filters-top .records-panel {
	height: auto;
}
.data-workspace.data-workspace-embedded.data-workspace-filters-top {
	min-height: 0;
	height: 100%;
}
@media (max-width: 600px) {
	.data-workspace-filters-top .records-toolbar {
		flex-wrap: wrap;
		justify-content: flex-end;
	}
	.data-workspace-filters-top .records-summary {
		flex: 1 0 100%;
	}
}
</style>
