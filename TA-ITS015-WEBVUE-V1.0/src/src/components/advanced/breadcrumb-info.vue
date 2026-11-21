<template>
  <div v-if="isShowBreadcrumb" class="layout-navbars-breadcrumb">
    <SvgIcon
      class="layout-navbars-breadcrumb-icon"
      :name="themeConfig.isCollapse ? 'ele-Expand' : 'ele-Fold'"
      :size="16"
      @click="onThemeConfigChange"
    />
    <el-breadcrumb separator="/">
      <!-- Root -->
      <el-breadcrumb-item v-if="breadcrumbList.length > 0">
        <a :to="breadcrumbList[0].path" @click.prevent="onBreadcrumbClick(breadcrumbList[0])" style="display: flex;">
          <SvgIcon :name="breadcrumbList[0].meta?.icon" class="layout-navbars-breadcrumb-iconfont mr3" v-if="themeConfig.isBreadcrumbIcon" /> 
          {{ $t(`lz.router.${breadcrumbList[0].name}`, breadcrumbList[0].meta.title) }}
        </a>
      </el-breadcrumb-item>

      <!-- Dropdown middle -->
      <el-breadcrumb-item v-if="hiddenItems.length > 0">
        <el-dropdown trigger="click">
          <span class="cursor-pointer text-blue-500">...</span>
          <template #dropdown>
            <el-dropdown-menu>
              <el-dropdown-item
                v-for="(item, index) in hiddenItems"
                :key="index"
              >
             
                <SvgIcon :name="item.meta?.icon" class="layout-navbars-breadcrumb-iconfont mr3" v-if="themeConfig.isBreadcrumbIcon" />
                <div>{{ $t(`lz.router.${item.name}`, item.meta?.title) }}</div>
              </el-dropdown-item>
            </el-dropdown-menu>
          </template>
        </el-dropdown>
      </el-breadcrumb-item>

      <!-- Tail items -->
      <el-breadcrumb-item
        v-for="(item, index) in tailItems"
        :key="index"
      >
      <!-- {{ item }} -->
        <span v-if="isLast(item)" style="display: flex;">
          <SvgIcon :name="item.meta?.icon" class="layout-navbars-breadcrumb-iconfont mr3" v-if="themeConfig.isBreadcrumbIcon" />
          <div>{{ $t(`lz.router.${item.name}`, item.meta?.title) }}</div>
        </span>
        <a v-else @click.prevent="onBreadcrumbClick(item)" style="display: flex;">
          <SvgIcon :name="item.meta?.icon" class="layout-navbars-breadcrumb-iconfont mr3" v-if="themeConfig.isBreadcrumbIcon" />
          <div>{{ $t(`lz.router.${item.name}`, item.meta?.title) }}</div>
        </a>
      </el-breadcrumb-item>
    </el-breadcrumb>
  </div>
</template>

<script setup lang="ts">
import { computed, watch, reactive, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { storeToRefs } from 'pinia';
import { useRoutesList } from '/@/stores/routesList';
import { useThemeConfig } from '/@/stores/themeConfig';

const route = useRoute();
const router = useRouter();
const { routesList } = storeToRefs(useRoutesList());
const { themeConfig } = storeToRefs(useThemeConfig());

const state = reactive({
  maxVisible: 4,
});


onMounted(() => {
  state.maxVisible = themeConfig.value.numberBreadcrumb;
  if(state.maxVisible < 1) state.maxVisible = 3;
});

const breadcrumbList = computed(() => {
  return findPath(routesList.value, route.path) || [];
});

const hiddenItems = computed(() => {
  const list = breadcrumbList.value;
  if (list.length <= state.maxVisible) return [];
  return list.slice(1, list.length - (state.maxVisible - 1));
});

const tailItems = computed(() => {
  const list = breadcrumbList.value;
  if (list.length <= state.maxVisible) return list.slice(1);
  return list.slice(list.length - (state.maxVisible - 1));
});

const isShowBreadcrumb = computed(() => {
  const { layout, isBreadcrumb } = themeConfig.value;
  return layout !== 'classic' && layout !== 'transverse' && isBreadcrumb;
});

function findPath(menu: RouteItem[], targetPath: string, stack: RouteItem[] = []): RouteItem[] | null {
  for (const item of menu) {
    const newStack = [...stack, item];
    if (item.path === targetPath) return newStack;
    if (item.children) {
      const found = findPath(item.children, targetPath, newStack);
      if (found) return found;
    }
  }
  return null;
}

function isLast(item: RouteItem) {
  return breadcrumbList.value[breadcrumbList.value.length - 1] === item;
}

function navigateTo(path: string) {
  router.push(path);
}

function onThemeConfigChange() {
  themeConfig.value.isCollapse = !themeConfig.value.isCollapse;
  localStorage.setItem('themeConfig', JSON.stringify(themeConfig.value));
}

// When breadcrumb is clicked
const onBreadcrumbClick = (v: RouteItem) => {
	const { redirect, path } = v;
	if (redirect) router.push(redirect);
	// If no redirect is specified, do not navigate
	// else if (path) router.push(path);
};
</script>

<style scoped lang="scss">
.layout-navbars-breadcrumb {
  flex: 1;
  height: inherit;
  display: flex;
  align-items: center;
  .layout-navbars-breadcrumb-icon {
    cursor: pointer;
    font-size: 18px;
    color: var(--next-bg-topBarColor);
    height: 100%;
    width: 40px;
    opacity: 0.8;
    &:hover {
      opacity: 1;
    }
  }
  :deep(.el-breadcrumb__separator) {
    opacity: 0.9;
    color: var(--next-bg-topBarColor);
  }
  :deep(.el-breadcrumb__inner a, .el-breadcrumb__inner.is-link) {
    font-weight: unset !important;
    color: var(--next-bg-topBarColor);
    &:hover {
      color: var(--el-color-primary) !important;
    }
  }
}
</style>
