<template>
	<div ref="layout" class="grid-page-side-layout" :class="{ 'grid-page-side-narrow': narrow }" :style="{ '--grid-page-side-size': `${size}px` }">
		<section v-show="visible" :id="id" class="grid-page-side-pane" :aria-label="label">
			<slot name="side" />
		</section>
		<PanelSplitter
			side="left"
			:orientation="narrow ? 'horizontal' : 'vertical'"
			:label="label"
			:controls="id"
			:width="size"
			:min="min"
			:max="max"
			:expanded="visible"
			:expand-label="$t('lz.label.base.extend')"
			:collapse-label="$t('lz.button.base.collapse')"
			:resize-hint="$t('lz.label.base.resizePanel', { name: label })"
			@resize="resize"
			@step="step"
		/>
		<div class="grid-page-side-main">
			<slot />
		</div>
	</div>
</template>

<script setup lang="ts" name="gridSidePanel">
// Vùng bên trái (cây đơn vị...) + thanh chia dọc (thu gọn/kéo/phím mũi tên) + nội dung.
// Màn hình dưới 600px: vùng bên trái chuyển lên trên, thanh chia thành ngang.
import { computed, reactive, ref } from 'vue';
import { useMediaQuery, useResizeObserver } from '@vueuse/core';
import PanelSplitter from '/@/components/workspace/PanelSplitter.vue';

interface Props {
	/** id của vùng bên trái (aria-controls của thanh chia). */
	id: string;
	/** Tên vùng bên trái, hiện trên thanh chia khi thu gọn. */
	label: string;
	/** Chiều rộng mặc định (px). */
	width?: number;
}
const props = withDefaults(defineProps<Props>(), { width: 240 });

const narrow = useMediaQuery('(max-width: 600px)');
const layout = ref<HTMLElement>();
const visible = ref(true);
const width = ref(props.width);
const height = ref(144);
const bounds = reactive({ width: 0, height: 0 });
useResizeObserver(layout, () => {
	bounds.width = layout.value?.clientWidth ?? 0;
	bounds.height = layout.value?.clientHeight ?? 0;
});

// Giữ chỗ tối thiểu cho nội dung: 448px khi nằm ngang, 388px khi xếp dọc.
const min = computed(() => (narrow.value ? 96 : 180));
const max = computed(() =>
	narrow.value
		? Math.max(96, Math.min((bounds.height || 700) * 0.4, (bounds.height || 700) - 388))
		: Math.max(180, Math.min((bounds.width || 1400) * 0.4, (bounds.width || 1400) - 448))
);
const normal = computed(() => Math.min(narrow.value ? 144 : props.width, max.value));
const size = computed(() => (visible.value ? Math.min(Math.max(min.value, narrow.value ? height.value : width.value), max.value) : 0));

function resize(value: number) {
	visible.value = value > 0;
	if (value <= 0) return;
	if (narrow.value) height.value = value;
	else width.value = value;
}

function step(direction: 'grow' | 'shrink') {
	if (direction === 'grow') resize(!visible.value || size.value < normal.value - 1 ? normal.value : max.value);
	else if (visible.value) resize(size.value > normal.value + 1 ? normal.value : 0);
}
</script>

<style lang="scss" scoped>
.grid-page-side-layout {
	display: grid;
	grid-template-columns: var(--grid-page-side-size) 28px minmax(0, 1fr);
	grid-template-rows: minmax(0, 1fr);
	flex: 1;
	height: 100%;
	min-width: 0;
	min-height: 0;
}
.grid-page-side-pane,
.grid-page-side-main {
	display: flex;
	min-width: 0;
	min-height: 0;
}
.grid-page-side-pane {
	grid-column: 1;
	grid-row: 1;

	> :deep(*) {
		flex: 1;
		min-width: 0;
		height: 100%;
	}
}
.grid-page-side-layout > .panel-splitter {
	grid-column: 2;
	grid-row: 1;
}
.grid-page-side-main {
	grid-column: 3;
	grid-row: 1;
	flex-direction: column;
}
.grid-page-side-narrow {
	grid-template-columns: minmax(0, 1fr);
	grid-template-rows: var(--grid-page-side-size) 28px minmax(0, 1fr);

	> .grid-page-side-pane {
		grid-column: 1;
		grid-row: 1;
	}
	> .panel-splitter {
		grid-column: 1;
		grid-row: 2;
	}
	> .grid-page-side-main {
		grid-column: 1;
		grid-row: 3;
	}
}
</style>
