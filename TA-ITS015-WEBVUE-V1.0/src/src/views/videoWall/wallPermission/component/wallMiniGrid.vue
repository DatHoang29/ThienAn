<template>
	<span class="wp-mini-wrap" :title="tooltip">
		<!-- Mỗi khối của tường một lưới nhỏ, xếp ngang -->
		<span v-for="b in gridBlocks" :key="b.blockNo!" class="wp-mini" :style="{ gridTemplateColumns: `repeat(${b.cols}, 9px)` }">
			<template v-for="r in b.rows!" :key="`r-${r}`">
				<i v-for="c in b.cols!" :key="`c-${c}-${r}`" :class="{ on: props.isFullAccess || cells.has(`${b.blockNo},${c - 1},${r - 1}`) }" />
			</template>
		</span>
	</span>
</template>

<script lang="ts" setup name="videoWallWallMiniGrid">
import { useI18n } from 'vue-i18n';
import { computed } from 'vue';
import { useVideoWallTopology } from '/@/stores/its/videoWallTopology';
import type { VwWallBlockOutput } from '/@/api-services/videoWall/models';
const { t: $t } = useI18n();
const topology = useVideoWallTopology();

/**
 * Lưới thu nhỏ hiển thị vùng được cấp trên bảng danh sách phân quyền.
 * Config dạng [{"Block":0,"Col":0,"Row":0},...] (cấu hình cũ chưa có Block thuộc khối đầu tiên); toàn quyền thì tô kín.
 * Vẽ theo khối của bố cục mà quyền được cấp (`blocks`); không truyền (dữ liệu cũ chưa gắn bố cục) ⇒ bố cục đang dùng.
 */
interface Props {
	config?: string | null;
	isFullAccess?: boolean | null;
	blocks?: VwWallBlockOutput[] | null;
}
const props = defineProps<Props>();

const gridBlocks = computed(() => (props.blocks?.length ? [...props.blocks].sort((a, b) => (a.blockNo ?? 0) - (b.blockNo ?? 0)) : topology.blocks));

const cells = computed(() => {
	const set = new Set<string>();
	if (!props.config) return set;
	try {
		for (const c of (JSON.parse(props.config) as any[]) ?? []) set.add(`${c.Block ?? c.block ?? 0},${c.Col ?? c.col},${c.Row ?? c.row}`);
	} catch {
		// Config lỗi định dạng: hiển thị lưới trống
	}
	return set;
});

const tooltip = computed(() =>
	props.isFullAccess ? $t('lz.entity.videoWall.fullAccess') : $t('lz.label.videoWall.screensCount', { count: cells.value.size, total: gridBlocks.value.reduce((t, b) => t + (b.cols ?? 0) * (b.rows ?? 0), 0) })
);
</script>

<style scoped lang="scss">
.wp-mini-wrap {
	display: inline-flex;
	gap: 6px;
	vertical-align: middle;
}
.wp-mini {
	display: inline-grid;
	gap: 2px;
	i {
		display: block;
		width: 9px;
		height: 6px;
		border-radius: 1px;
		background: var(--el-fill-color-darker);
		&.on {
			background: var(--el-color-primary);
		}
	}
}
</style>
