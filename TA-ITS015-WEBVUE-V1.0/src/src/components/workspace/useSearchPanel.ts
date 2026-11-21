import { computed, reactive, ref } from 'vue';
import { useResizeObserver } from '@vueuse/core';

/**
 * State for a search card placed above a list and controlled by a horizontal PanelSplitter
 * (collapse/expand with the buttons, drag or arrow keys to change its height).
 * Same sizing rules as the top filter panel of DataWorkspace (stepPanel/resizePanel):
 * normal = the card's natural height, max = 1.5 × normal (leaving room for the list).
 */
export function useSearchPanel(id: string) {
	const panel = ref<HTMLElement>();
	const visible = ref(true);
	// undefined = follow the natural (auto) height
	const height = ref<number>();
	const normal = ref(44);
	const containerHeight = ref(0);
	// PanelSplitter fills its grid row in DataWorkspace; in a flex column it needs a fixed height.
	const splitterHeight = 28;
	const splitterStyle = { flex: 'none', height: `${splitterHeight}px`, minHeight: `${splitterHeight}px` };

	const min = computed(() => Math.min(44, normal.value));
	const max = computed(() => Math.max(min.value, Math.min(normal.value * 1.5, (containerHeight.value || 600) - 240 - splitterHeight)));
	const size = computed(() => (visible.value ? Math.min(height.value ?? normal.value, max.value) : 0));
	const panelStyle = computed(() => (visible.value && height.value !== undefined ? { height: `${size.value}px`, overflow: 'auto' } : undefined));

	function measure() {
		const element = panel.value;
		if (!element) return;
		containerHeight.value = element.parentElement?.clientHeight ?? 0;
		// A fixed height hides the natural one; it is measured again when the card returns to auto.
		if (visible.value && height.value === undefined && element.offsetHeight > 0) normal.value = element.offsetHeight;
	}

	// el-card passes its component instance to a function ref.
	function setPanel(target: unknown) {
		panel.value = ((target as { $el?: HTMLElement } | null)?.$el ?? (target as HTMLElement | null)) || undefined;
	}

	function setVisible(value: boolean) {
		visible.value = value;
		height.value = undefined;
	}

	function resize(value: number) {
		if (value <= 0) {
			setVisible(false);
			return;
		}
		visible.value = true;
		height.value = Math.abs(value - normal.value) <= 1 ? undefined : value;
	}

	function step(direction: 'grow' | 'shrink') {
		const normalSize = Math.min(normal.value, max.value);
		if (direction === 'grow') {
			if (visible.value && size.value >= max.value - 1) return;
			resize(!visible.value || size.value < normalSize - 1 ? normalSize : max.value);
		} else if (visible.value) {
			if (size.value > normalSize + 1) resize(normalSize);
			else setVisible(false);
		}
	}

	// Re-measure when the card or the page size changes (filters wrap onto more rows).
	useResizeObserver(panel, measure);
	useResizeObserver(() => panel.value?.parentElement, measure);

	return reactive({ id, visible, size, min, max, panelStyle, splitterStyle, setPanel, resize, step });
}
