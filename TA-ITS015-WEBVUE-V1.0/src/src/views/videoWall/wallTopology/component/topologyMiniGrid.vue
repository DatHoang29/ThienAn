<template>
	<span class="wt-mini-wrap" :title="tooltip">
		<!-- Mỗi khối một lưới nhỏ, xếp ngang trái → phải như trên tường (giống lưới thu nhỏ của trang Phân quyền) -->
		<span v-for="b in sortedBlocks" :key="b.blockNo!" class="wt-mini-block">
			<span class="wt-mini" :style="{ gridTemplateColumns: `repeat(${b.cols}, ${cell.w}px)` }">
				<template v-for="r in b.rows!" :key="`r-${r}`">
					<i v-for="c in b.cols!" :key="`c-${c}-${r}`" :style="{ height: `${cell.h}px` }" />
				</template>
			</span>
			<small v-if="sortedBlocks.length > 1">{{ blockLabel(b.blockNo) }}</small>
		</span>
	</span>
</template>

<script lang="ts" setup name="videoWallTopologyMiniGrid">
import { computed } from 'vue';
import type { VwWallBlockOutput } from '/@/api-services/videoWall';
import { blockLabel, cellRatioParts } from '/@/stores/its/videoWallTopology';

/** Lưới thu nhỏ của 1 bố cục trên bảng danh sách bố cục: các khối cột × hàng. */
interface Props {
	blocks?: VwWallBlockOutput[] | null;
	/** Tỉ lệ ô của bố cục (VD "3:4"); bỏ trống ⇒ 16:9. */
	cellRatio?: string | null;
}
const props = defineProps<Props>();

const sortedBlocks = computed(() => [...(props.blocks ?? [])].filter((b) => (b.cols ?? 0) > 0 && (b.rows ?? 0) > 0).sort((a, b) => (a.blockNo ?? 0) - (b.blockNo ?? 0)));

/** Ô rộng 9 px (cao theo tỉ lệ ô); tường rộng (nhiều cột) thì thu nhỏ ô để vừa cột bảng. */
const cell = computed(() => {
	const cols = sortedBlocks.value.reduce((t, b) => t + (b.cols ?? 0), 0);
	const w = cols > 24 ? 4 : cols > 14 ? 6 : 9;
	const r = cellRatioParts(props.cellRatio) ?? { rw: 3, rh: 2 };
	return { w, h: Math.max(2, Math.round((w * r.rh) / r.rw)) };
});

// 1 khối ⇒ chỉ "8×4"; nhiều khối ⇒ thêm tên khối "A 8×4 · B 6×1"
const tooltip = computed(() =>
	sortedBlocks.value.map((b) => (sortedBlocks.value.length > 1 ? `${blockLabel(b.blockNo)} ${b.cols}×${b.rows}` : `${b.cols}×${b.rows}`)).join(' · ')
);
</script>

<style scoped lang="scss">
.wt-mini-wrap {
	display: inline-flex;
	align-items: center;
	gap: 6px;
	vertical-align: middle;
}
.wt-mini-block {
	display: inline-flex;
	flex-direction: column;
	align-items: center;
	gap: 2px;
	small {
		font-size: 10px;
		line-height: 1;
		color: var(--el-text-color-secondary);
	}
}
.wt-mini {
	display: inline-grid;
	gap: 2px;
	i {
		display: block;
		border-radius: 1px;
		background: var(--el-color-primary);
	}
}
</style>
