<template>
	<section
		v-show="visible"
		:id="id"
		class="filter-card workspace-search-panel workspace-panel workspace-filter"
		:class="{ 'workspace-search-inline': layout === 'inline' }"
		:aria-label="$t('lz.button.base.search')"
	>
		<header class="filter-heading workspace-panel-heading">
			<h2>
				<el-icon><Search /></el-icon>
				{{ $t('lz.button.base.search') }}
			</h2>
			<el-button :icon="ArrowLeft" text :aria-label="$t('lz.button.base.close')" @click="emits('hide')" />
		</header>
		<el-form :label-position="layout === 'inline' ? 'left' : 'top'" :label-width="layout === 'inline' ? 'auto' : undefined" size="small" :disabled="saving" @submit.prevent="handleSearch">
			<slot :set-filter-date="setFilterDate" />
			<div v-if="advancedFilters" :id="`${id}-advanced`" class="filter-grid advanced-filters">
				<slot name="advanced" />
			</div>
			<div class="filter-actions">
				<div class="filter-buttons">
					<el-button v-if="$slots.advanced" text :icon="Filter" :aria-expanded="advancedFilters" :aria-controls="`${id}-advanced`" @click="advancedFilters = !advancedFilters">
						{{ advancedFilters ? $t('lz.button.base.collapse') : $t('lz.button.base.moreFilters') }}
					</el-button>
					<el-button :icon="RefreshLeft" @click="handleReset">
						{{ $t('lz.button.base.reset') }}
					</el-button>
					<el-button type="primary" :icon="Search" :loading="loading" :disabled="!canSearch" native-type="submit">
						{{ $t('lz.button.base.search') }}
					</el-button>
				</div>
			</div>
		</el-form>
	</section>
</template>

<script lang="ts" setup name="searchFilterPanel">
import { computed, nextTick, onActivated, onDeactivated, onScopeDispose, ref, watch } from 'vue';
import { watchDebounced } from '@vueuse/core';
import { useI18n } from 'vue-i18n';
import { Search, Filter, RefreshLeft, ArrowLeft } from '@element-plus/icons-vue';

interface Props {
	id: string;
	visible: boolean;
	layout?: 'sidebar' | 'inline';
	defaultDates?: () => [Date, Date];
	filters: Record<string, unknown>;
	saving: boolean;
	loading: boolean;
	autoSearch?: boolean;
	canSearch?: boolean;
}
// Component contract
const AUTO_SEARCH_DELAY = 1500;

const props = withDefaults(defineProps<Props>(), { autoSearch: true, canSearch: true, layout: 'sidebar' });
const emits = defineEmits<{ hide: []; search: []; reset: [] }>();
const { t: $t } = useI18n();
// Search state
const advancedFilters = ref(false);
const filterSnapshot = computed(() => JSON.stringify(props.filters));
let submittedSnapshot = filterSnapshot.value;
let active = true;

// Search and date actions
function handleSearch() {
	if (props.saving || !props.canSearch) return;
	submittedSnapshot = filterSnapshot.value;
	emits('search');
}

async function handleReset() {
	if (props.saving) return;
	emits('reset');
	// Parent reset handlers replace filters and submit immediately.
	await nextTick();
	submittedSnapshot = filterSnapshot.value;
}

function setFilterDate(index: 0 | 1, value: Date | null) {
	if (!value) {
		props.filters.dateRange = null;
		return;
	}
	const dates = (props.filters.dateRange as [Date, Date] | null | undefined) ?? props.defaultDates?.();
	if (!dates) return;
	const range: [Date, Date] = [...dates];
	range[index] = value;
	props.filters.dateRange = range;
}

// Watchers and lifecycle
watchDebounced(
	filterSnapshot,
	() => {
		if (active && props.autoSearch && !props.saving && filterSnapshot.value !== submittedSnapshot) handleSearch();
	},
	{ debounce: AUTO_SEARCH_DELAY }
);

// Enabling the feature or returning to a cached page requires a new filter edit.
watch(
	() => props.autoSearch,
	() => {
		submittedSnapshot = filterSnapshot.value;
	}
);
onActivated(() => {
	active = true;
	submittedSnapshot = filterSnapshot.value;
});
onDeactivated(() => {
	active = false;
});
onScopeDispose(() => {
	active = false;
});
</script>

<style lang="scss">
.workspace-search-panel {
	--workspace-panel-font-increment: var(--filter-font-increment, 0px);
	--workspace-font-increment: var(--filter-font-increment, 0px);
	--el-font-size-small: calc(13px + var(--workspace-panel-font-increment));
	min-width: 0;
	min-height: 0;
	overflow-y: auto;
	scrollbar-width: thin;
	padding: 8px 10px;
}
/* theme/workspace.scss đặt `.workspace-ui .workspace-panel { overflow: hidden }` (độ ưu tiên cao hơn rule trên) ⇒ mở "Lọc thêm"
   bị tràn mà không cuộn được. Tăng độ ưu tiên để khung tìm kiếm cuộn dọc trong chiều cao của nó. */
.workspace-ui .workspace-search-panel.workspace-panel {
	overflow-x: hidden;
	overflow-y: auto;
}
.workspace-search-panel .el-input__inner,
.workspace-search-panel .el-select__wrapper {
	font-size: var(--el-font-size-small);
}
.workspace-search-panel .filter-heading {
	display: flex;
	align-items: center;
	justify-content: space-between;
	gap: 6px;
	padding: 6px 10px;
	margin: -8px -10px 8px;
}
.workspace-search-panel .filter-heading h2 {
	display: flex;
	align-items: center;
	gap: 6px;
}
.workspace-search-panel .filter-heading .el-button {
	padding: 4px;
	min-height: 24px;
}
.workspace-search-panel .el-form {
	display: flex;
	flex-direction: column;
	gap: 12px;
}
.workspace-search-panel .filter-grid {
	display: grid;
	grid-template-columns: minmax(0, 1fr);
	gap: 10px;
}
.workspace-search-panel .filter-grid .el-form-item {
	min-width: 0;
	margin-bottom: 0;
}
.workspace-search-panel .el-form-item__label {
	color: #000;
	font-size: calc(13px + var(--workspace-panel-font-increment, 0px));
	line-height: 1.4;
	margin-bottom: 4px;
}
.workspace-search-panel .filter-grid .el-select,
.workspace-search-panel .filter-grid .el-input-number,
.workspace-search-panel .filter-grid .el-date-editor {
	width: 100%;
	min-width: 0;
}
.workspace-search-panel .el-input__wrapper,
.workspace-search-panel .el-select__wrapper {
	border-radius: 5px;
}
.workspace-search-panel .el-range-input {
	background: transparent;
}
.workspace-search-panel .advanced-filters {
	padding-top: 10px;
	border-top: 1px dashed var(--workspace-border, var(--el-border-color, #dcdfe6));
}
.workspace-search-panel .filter-actions {
	position: sticky;
	bottom: -8px;
	background: var(--workspace-surface, var(--el-bg-color, #fff));
	padding: 8px 0;
	border-top: 1px solid var(--workspace-border, var(--el-border-color, #dcdfe6));
}
.workspace-search-panel .filter-presets {
	display: flex;
	flex-wrap: wrap;
	gap: 6px;
}
.workspace-search-panel .filter-presets button {
	display: flex;
	gap: 6px;
	align-items: center;
	border: 1px solid transparent;
	color: var(--workspace-muted, var(--el-text-color-secondary, #909399));
	background: var(--workspace-subtle, var(--el-fill-color-light, #f5f7fa));
	border-radius: 5px;
	padding: 4px 9px;
	font-size: 11px;
	min-height: 28px;
}
.workspace-search-panel .filter-presets button.active {
	border-color: #cbdcff;
	background: var(--workspace-primary-soft, var(--el-color-primary-light-9, #ecf5ff));
	color: var(--workspace-primary-dark, var(--el-color-primary-dark-2, #337ecc));
	font-weight: 600;
}
.workspace-search-panel .filter-buttons {
	display: flex;
	flex-wrap: wrap;
	gap: 6px;
	align-items: stretch;
}
.workspace-search-panel .filter-buttons .el-button.is-text {
	flex-basis: 100%;
}
.workspace-search-panel .filter-buttons .el-button {
	flex: 1 0 auto;
	min-width: max-content;
	margin: 0;
	padding: 6px 8px;
}
.workspace-search-panel .filter-caption {
	color: var(--workspace-muted, var(--el-text-color-secondary, #909399));
	font-size: 12px;
}
.workspace-search-panel .monitor-content > .filter-card {
	grid-column: 1;
}
@media (max-width: 960px) {
	.workspace-search-panel {
		max-height: 300px;
		flex-shrink: 0;
	}

	.workspace-search-panel .el-form {
		display: grid;
		grid-template-columns: minmax(0, 1fr) auto;
		align-items: end;
	}

	.workspace-search-panel .advanced-filters {
		grid-column: 1/-1;
	}

	.workspace-search-panel .filter-actions {
		position: static;
		grid-column: 2;
		grid-row: 1;
		padding: 0;
		border-top: 0;
	}

	.workspace-search-panel .filter-grid {
		grid-template-columns: repeat(2, minmax(0, 1fr));
	}
}
@media (max-width: 600px) {
	.workspace-search-panel .el-form {
		display: flex;
	}

	.workspace-search-panel .filter-grid {
		grid-template-columns: 1fr;
		gap: 8px;
	}

	.workspace-search-panel {
		padding: 8px;
	}

	.workspace-search-panel .filter-heading {
		margin-left: -8px;
		margin-right: -8px;
		padding-left: 8px;
		padding-right: 8px;
	}
}
.workspace-search-panel.workspace-search-inline {
	max-height: none;
	padding: 6px 8px;
}
.workspace-search-inline .filter-heading {
	display: none;
}
.workspace-search-inline .el-form {
	display: flex;
	flex-direction: row;
	flex-wrap: wrap;
	align-items: flex-start;
	gap: 8px 12px;
}
.workspace-search-inline .filter-grid {
	display: grid;
	flex: 1 1 400px;
	grid-template-columns: repeat(auto-fit, minmax(min(100%, 220px), 1fr));
	gap: 8px 12px;
}
.workspace-search-inline .el-form-item__label {
	line-height: 24px;
	margin-bottom: 0;
}
.workspace-search-inline .el-form-item__content {
	min-width: 0;
}
.workspace-search-inline .filter-actions {
	position: static;
	flex: 0 1 auto;
	min-width: 0;
	max-width: 100%;
	padding: 0;
	border: 0;
	margin-left: auto;
}
.workspace-search-inline .filter-buttons .el-button.is-text {
	flex-basis: auto;
}
</style>
