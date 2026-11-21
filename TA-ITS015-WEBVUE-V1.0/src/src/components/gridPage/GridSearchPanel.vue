<template>
	<div class="grid-page-panel">
		<el-card
			v-show="searchPanel.visible"
			:id="searchPanel.id"
			:ref="searchPanel.setPanel"
			:style="searchPanel.panelStyle"
			shadow="hover"
			:body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', height: '100%', alignItems: 'start' }"
		>
			<slot name="filters" :visible="searchPanel.visible" :hide="hide" />
		</el-card>
		<PanelSplitter
			side="left"
			orientation="horizontal"
			:style="searchPanel.splitterStyle"
			:controls="searchPanel.id"
			:label="$t('lz.button.base.search')"
			:resize-hint="$t('lz.label.base.resizePanel', { name: $t('lz.button.base.search') })"
			:expand-label="$t('lz.label.base.extend')"
			:collapse-label="$t('lz.button.base.collapse')"
			:expanded="searchPanel.visible"
			:width="searchPanel.size"
			:min="searchPanel.min"
			:max="searchPanel.max"
			@resize="searchPanel.resize"
			@step="searchPanel.step"
		/>
		<slot />
	</div>
</template>

<script setup lang="ts" name="gridSearchPanel">
// Thẻ tìm kiếm phía trên + thanh chia ngang (thu gọn/kéo) + nội dung trang (thẻ bảng `full-table`).
// Trang tự giữ form tìm kiếm, nút, vxe-grid và các slot của bảng.
import PanelSplitter from '/@/components/workspace/PanelSplitter.vue';
import { useSearchPanel } from '/@/components/workspace/useSearchPanel';

const props = defineProps<{ id: string }>();
const searchPanel = useSearchPanel(props.id);
const hide = () => searchPanel.resize(0);
</script>

<style lang="scss" scoped>
/* useSearchPanel đo chiều cao vùng cha, nên vùng này phải lấp đầy chỗ trống. */
.grid-page-panel {
	display: flex;
	flex: 1;
	flex-direction: column;
	min-width: 0;
	min-height: 0;

	/* Rule chung chỉ áp cho `.layout-parent > div:first-child`; trang nằm trong tab hoặc pane cần áp lại. */
	:deep(.full-table) {
		/* Cho phép toolbar co giãn theo chiều rộng thẻ bảng (không theo cửa sổ). */
		container-type: inline-size;
		flex: 1;
		display: flex;
		flex-direction: column;
		justify-content: space-between;
		min-height: 0;

		.el-card__body {
			height: 100%;
			display: flex;
			padding: 5px 5px 8px 5px;
			flex-direction: column;
			justify-content: space-between;
		}
	}
}

/* Bảng hẹp (như DataWorkspace dưới 600px): "Tổng cộng" một hàng, nút thao tác xuống hàng dưới bên phải. */
@container (max-width: 600px) {
	.grid-page-panel :deep(.vxe-toolbar) {
		flex-wrap: wrap;
		row-gap: 4px;
	}
	.grid-page-panel :deep(.vxe-toolbar .vxe-buttons--wrapper) {
		flex: 1 0 100%;
	}
	.grid-page-panel :deep(.vxe-toolbar .vxe-tools--wrapper) {
		margin-left: auto;
	}
}
</style>
