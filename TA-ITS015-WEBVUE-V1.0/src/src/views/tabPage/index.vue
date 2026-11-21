<template>
	<WorkspacePage class="tab-page">
		<template #tabs="{ title }">
			<WorkspaceTabBar v-model="activeTab" :tabs="tabs" :aria-label="title" />
		</template>
		<section v-if="activeComponent" class="management-tab-panel">
			<KeepAlive :key="cacheKey">
				<component :is="activeComponent" v-if="activeDefinition?.keepAlive" :key="activeDefinition.cacheKey" />
			</KeepAlive>
			<component :is="activeComponent" v-if="!activeDefinition?.keepAlive" :key="activeDefinition?.cacheKey" />
		</section>
		<WorkspaceEmpty v-else />
	</WorkspacePage>
</template>

<script setup lang="ts" name="tabPageView">
import { computed, defineAsyncComponent, getCurrentInstance, ref, watch } from 'vue';
import type { Component } from 'vue';
import { useRoute } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { auth } from '/@/utils/authFunction';
import WorkspaceEmpty from '/@/components/workspace/WorkspaceEmpty.vue';
import WorkspacePage from '/@/components/workspace/WorkspacePage.vue';
import WorkspaceTabBar from '/@/components/workspace/WorkspaceTabBar.vue';
import { useMenuTabs } from '/@/stores/menuTabs';
import { visibleMenuTabs, resolveViewModule } from '/@/utils/menuTabs';
import { viewModules } from '/@/router/viewModules';

const { t: $t, te } = useI18n();
const route = useRoute();
const props = defineProps<{ menuId?: string }>();
// Router truyền ID cố định cho từng menu; instance đã cache không theo menu đang mở.
const menuId = String(props.menuId ?? route.meta.menuId ?? '');
const menuTabs = useMenuTabs();
// Tab trỏ về chính trang này sẽ tự render vô hạn; so sánh theo component để có thể đổi vị trí thư mục.
const hostComponent = getCurrentInstance()?.type;
const components = new Map<string, Component>();
const tabs = computed(() => {
	const configuredTabs = menuTabs.tabsByMenuId[menuId] ?? [];
	return visibleMenuTabs(configuredTabs, auth).flatMap((tab) => {
		const loader = resolveViewModule(viewModules, tab.component);
		if (!loader) return [];
		const componentPath = tab.component as string;
		if (!components.has(componentPath))
			components.set(
				componentPath,
				defineAsyncComponent(async () => {
					const view = (await loader()).default;
					return view === hostComponent ? WorkspaceEmpty : view;
				})
			);
		const title = tab.meta?.title || tab.name || '';
		return [
			{
				key: tab.id as string,
				name: tab.name || '',
				label: te(title) ? $t(title) : title,
				icon: tab.meta?.icon || '',
				component: components.get(componentPath),
				keepAlive: tab.meta?.isKeepAlive !== false,
				cacheKey: `${menuId}:${tab.id}:${componentPath}`,
			},
		];
	});
});
const activeTab = ref('');
watch(
	tabs,
	(items) => {
		if (!items.some((tab) => tab.key === activeTab.value)) activeTab.value = items[0]?.key ?? '';
	},
	{ immediate: true }
);
// Liên kết từ trang khác yêu cầu mở 1 tab của menu này (menuTabs.requestTab) ⇒ chọn tab rồi xoá yêu cầu.
// Dùng 1 lần, không đưa lên URL ⇒ sau đó người dùng đổi tab / sang menu khác bình thường.
watch(
	[() => menuTabs.pendingTab, tabs],
	([pending]) => {
		if (!pending || pending.menuId !== menuId) return;
		const key = tabs.value.find((item) => item.key === pending.tab || item.name === pending.tab)?.key;
		if (!key) return;
		activeTab.value = key;
		menuTabs.pendingTab = null;
	},
	{ immediate: true }
);
const activeDefinition = computed(() => tabs.value.find((tab) => tab.key === activeTab.value));
const activeComponent = computed(() => activeDefinition.value?.component);
// Hủy cache khi tab bị gỡ, mất quyền hoặc đổi component/chính sách cache.
const cacheKey = computed(() => JSON.stringify(tabs.value.map((tab) => [tab.cacheKey, tab.keepAlive]).sort()));
</script>

<style lang="scss" scoped>
/* Kiểu dáng header + thanh tab nằm ở components/workspace/WorkspaceTabBar.vue (dùng chung với Zone, Sự kiện - Sự cố). */
/* Tab là trang danh sách gốc; cho trang lấp đầy chiều cao như `.layout-parent > div:first-child`. */
.management-tab-panel {
	display: flex;
	flex: 1;
	flex-direction: column;
	min-height: 0;

	> :deep(div) {
		display: flex;
		flex: 1;
		flex-direction: column;
		min-height: 0;
	}
}
</style>
