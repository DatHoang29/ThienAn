<template>
	<main class="management-workspace workspace-ui layout-padding" :aria-label="pageTitle">
		<header class="management-header workspace-header">
			<div class="management-heading workspace-heading">
				<el-icon><DataAnalysis /></el-icon>
				<h1>{{ pageTitle }}</h1>
			</div>
			<nav class="workspace-tabbar" :aria-label="pageTitle">
				<el-tabs v-model="activeTab" class="management-tabs workspace-tabs" :before-leave="beforeTabLeave">
					<el-tab-pane v-for="tab in tabs" :key="tab.key" :name="tab.key" :disabled="!beforeTabLeave()">
						<template #label>
							<span class="management-tab-label workspace-tab-label">
								<SvgIcon v-if="tab.icon" :name="tab.icon" />
								{{ tab.label }}
							</span>
						</template>
					</el-tab-pane>
				</el-tabs>
			</nav>
		</header>
		<section v-if="activeComponent" class="traffic-statistic-tab-panel">
			<KeepAlive :key="cacheKey">
				<component :is="activeComponent" v-if="activeDefinition?.keepAlive" :key="activeDefinition.cacheKey" ref="activePanel" />
			</KeepAlive>
			<component :is="activeComponent" v-if="!activeDefinition?.keepAlive" :key="activeDefinition?.cacheKey" ref="activePanel" />
		</section>
		<WorkspaceEmpty v-else />
	</main>
</template>

<script setup lang="ts" name="tmsRptTrafficStatisticMngt">
import { computed, defineAsyncComponent, getCurrentInstance, ref, watch } from 'vue';
import type { Component } from 'vue';
import { DataAnalysis } from '@element-plus/icons-vue';
import { useRoute } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { auth } from '/@/utils/authFunction';
import SvgIcon from '/@/components/svgIcon/index.vue';
import WorkspaceEmpty from '/@/components/workspace/WorkspaceEmpty.vue';
import { useMenuTabs } from '/@/stores/menuTabs';
import { visibleMenuTabs, resolveViewModule } from '/@/utils/menuTabs';
import { viewModules } from '/@/router/viewModules';

const { t: $t, te } = useI18n();
const route = useRoute();
const pageTitle = computed(() => $t(`lz.router.${String(route.name ?? '')}`, String(route.meta.title || $t('lz.label.tmsRptTrafficStatisticMngt.title'))));
const menuTabs = useMenuTabs();
// The page itself; a tab configured to load this same file would render itself forever.
// Compared by component (not by path) so the folder can be moved without changing this code.
const hostComponent = getCurrentInstance()?.type;
const components = new Map<string, Component>();
const tabs = computed(() => {
	const configuredTabs = menuTabs.tabsByMenuId[String(route.meta.menuId ?? '')] ?? [];
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
				label: te(title) ? $t(title) : title,
				icon: tab.meta?.icon || '',
				component: components.get(componentPath),
				keepAlive: tab.meta?.isKeepAlive !== false,
				cacheKey: `${tab.id}:${componentPath}`,
			},
		];
	});
});
const activeTab = ref('');
const activePanel = ref<{ state?: { loading: boolean; saving: boolean } }>();
const beforeTabLeave = () => !activePanel.value?.state?.loading && !activePanel.value?.state?.saving;
watch(
	tabs,
	(items) => {
		if (!items.some((tab) => tab.key === activeTab.value)) activeTab.value = items[0]?.key ?? '';
	},
	{ immediate: true }
);
const activeDefinition = computed(() => tabs.value.find((tab) => tab.key === activeTab.value));
const activeComponent = computed(() => activeDefinition.value?.component);
// Discard cached instances when a tab is removed, revoked or changes its component/cache policy.
const cacheKey = computed(() => JSON.stringify(tabs.value.map((tab) => [tab.cacheKey, tab.keepAlive]).sort()));
</script>

<style lang="scss" scoped>
// Tabs are standalone list pages; give them the same fill-height layout as `.layout-parent > div:first-child`.
.traffic-statistic-tab-panel {
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

	:deep(.full-table) {
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

@media (max-width: 768px) {
	.management-header {
		align-items: stretch;
		flex-direction: column;
		gap: 4px;
	}
}
</style>
