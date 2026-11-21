<template>
	<slot v-if="nested" />
	<main v-else class="management-workspace workspace-ui layout-padding" :aria-label="pageTitle">
		<!-- Chỉ trang có tab mới cần tiêu đề; trang menu đứng riêng đã có tên ở menu/breadcrumb. -->
		<header v-if="$slots.tabs" class="management-header workspace-header">
			<div class="management-heading workspace-heading">
				<SvgIcon v-if="iconName" :name="iconName" />
				<h1>{{ pageTitle }}</h1>
			</div>
			<slot name="tabs" :title="pageTitle" />
		</header>
		<div class="workspace-page-body">
			<slot />
		</div>
	</main>
</template>

<script setup lang="ts" name="workspacePage">
// Vỏ trang dùng chung: là vỏ ngoài cùng thì vẽ <main> + tiêu đề; nằm trong vỏ khác (tab) thì chỉ hiển thị nội dung.
// Nhờ vậy cùng một component tab chạy được cả trong trang Mngt lẫn như một menu độc lập.
import { computed, inject, provide } from 'vue';
import { useRoute } from 'vue-router';
import { useI18n } from 'vue-i18n';
import SvgIcon from '/@/components/svgIcon/index.vue';
import { WORKSPACE_PAGE_KEY } from './workspace';

interface Props {
	/** Khóa dịch dùng khi menu không có `lz.router.<name>` và `meta.title`. */
	fallbackTitle?: string;
	/** Icon ưu tiên; bỏ trống thì dùng icon của menu trong CSDL. */
	icon?: string;
}
const props = defineProps<Props>();

const nested = inject(WORKSPACE_PAGE_KEY, false);
provide(WORKSPACE_PAGE_KEY, true);

const { t: $t } = useI18n();
const route = useRoute();
const pageTitle = computed(() => {
	const fallback = String(route.meta.title || (props.fallbackTitle ? $t(props.fallbackTitle) : ''));
	return $t(`lz.router.${String(route.name ?? '')}`, fallback);
});
const iconName = computed(() => props.icon || String(route.meta.icon || ''));
</script>

<style lang="scss" scoped>
/* Nội dung lấp đầy phần còn lại dưới header; trang con (danh sách gốc) cũng được lấp đầy chiều cao
   như `.layout-parent > div:first-child`, vì <main> của vỏ này không khớp rule chung đó. */
.workspace-page-body {
	display: flex;
	flex: 1;
	flex-direction: column;
	min-width: 0;
	min-height: 0;

	/* Chỉ thẻ gốc <div> của trang danh sách; không đè section như WorkspaceEmpty. */
	> :deep(div) {
		display: flex;
		flex: 1;
		flex-direction: column;
		min-height: 0;
	}
}

@media (max-width: 768px) {
	.management-header {
		align-items: stretch;
		flex-direction: column;
		gap: 4px;
	}
}
</style>
