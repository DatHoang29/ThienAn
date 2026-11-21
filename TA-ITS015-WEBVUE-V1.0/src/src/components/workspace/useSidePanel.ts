import { computed, reactive, ref } from 'vue';
import type { Ref } from 'vue';
import { useResizeObserver } from '@vueuse/core';

interface Options {
	/** Chiều rộng mặc định (px) — cũng là mức "bình thường" khi bấm mở / thu. */
	width: number;
	/** Chiều rộng nhỏ nhất khi đang mở (px). */
	min?: number;
	/** Tỉ lệ tối đa so với khung chứa. */
	maxRatio?: number;
	/** Chừa tối thiểu cho phần nội dung chính (px). */
	reserve?: number;
}

/**
 * Cột bên (trái / phải) điều khiển bằng PanelSplitter dọc: kéo đổi rộng, bấm nút / phím mũi tên để thu gọn / mở.
 * Cùng quy tắc với GridSidePanel nhưng không bọc layout ⇒ dùng được cho cột bên phải và trang có nhiều cột.
 * @param container khung chứa các cột (để tính chiều rộng tối đa)
 */
export function useSidePanel(id: string, container: Ref<HTMLElement | undefined>, options: Options) {
	const visible = ref(true);
	const width = ref(options.width);
	const bounds = ref(0);
	useResizeObserver(container, () => (bounds.value = container.value?.clientWidth ?? 0));

	const min = options.min ?? 200;
	const max = computed(() => {
		const total = bounds.value || 1400;
		return Math.max(min, Math.min(total * (options.maxRatio ?? 0.4), total - (options.reserve ?? 480)));
	});
	const normal = computed(() => Math.min(options.width, max.value));
	const size = computed(() => (visible.value ? Math.min(Math.max(min, width.value), max.value) : 0));
	/** Gắn vào cột: chiều rộng theo `size`, ẩn khi thu gọn. */
	const panelStyle = computed(() => ({ width: `${size.value}px`, minWidth: `${size.value}px`, flex: 'none', display: visible.value ? undefined : 'none' }));

	function resize(value: number) {
		visible.value = value > 0;
		if (value > 0) width.value = value;
	}

	function step(direction: 'grow' | 'shrink') {
		if (direction === 'grow') resize(!visible.value || size.value < normal.value - 1 ? normal.value : max.value);
		else if (visible.value) resize(size.value > normal.value + 1 ? normal.value : 0);
	}

	return reactive({ id, visible, size, min, max, panelStyle, resize, step });
}
