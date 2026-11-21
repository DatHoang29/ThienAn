<template>
  <!-- Nhóm cài đặt dùng chung cho panel cấu hình bản đồ: tiêu đề + biểu tượng, thu gọn được -->
  <section class="settings-section" :class="{ 'is-collapsed': isCollapsed }">
    <div class="settings-section__header">
      <button
        v-if="collapsible"
        type="button"
        class="settings-section__toggle"
        :aria-expanded="!isCollapsed"
        @click="toggle"
      >
        <el-icon class="settings-section__chevron"><ele-ArrowDown /></el-icon>
        <el-icon v-if="icon" class="settings-section__icon"><component :is="icon" /></el-icon>
        <span class="settings-section__title">{{ title }}</span>
        <span v-if="count != null" class="settings-section__count">{{ count }}</span>
      </button>
      <div v-else class="settings-section__toggle is-static">
        <el-icon v-if="icon" class="settings-section__icon"><component :is="icon" /></el-icon>
        <span class="settings-section__title">{{ title }}</span>
        <span v-if="count != null" class="settings-section__count">{{ count }}</span>
      </div>
      <div v-if="$slots.extra" class="settings-section__extra"><slot name="extra" /></div>
    </div>

    <el-collapse-transition>
      <div v-show="!isCollapsed" class="settings-section__body">
        <slot />
      </div>
    </el-collapse-transition>
  </section>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue';

const props = withDefaults(
  defineProps<{
    title: string;
    /** Tên component icon đã đăng ký toàn cục, ví dụ 'ele-Setting'. */
    icon?: string;
    /** Số hiển thị dạng huy hiệu cạnh tiêu đề (vd: số block). */
    count?: number | null;
    collapsible?: boolean;
    /** v-model:collapsed — bỏ trống để component tự giữ trạng thái. */
    collapsed?: boolean;
    defaultCollapsed?: boolean;
  }>(),
  {
    icon: '',
    count: null,
    collapsible: true,
    collapsed: undefined,
    defaultCollapsed: false,
  }
);
const emit = defineEmits<{ (event: 'update:collapsed', value: boolean): void }>();

const innerCollapsed = ref(props.defaultCollapsed);
const isCollapsed = computed(() => props.collapsible && (props.collapsed ?? innerCollapsed.value));

const toggle = () => {
  const next = !isCollapsed.value;
  innerCollapsed.value = next;
  emit('update:collapsed', next);
};
</script>

<style scoped>
.settings-section { border: 1px solid var(--el-border-color-lighter); border-radius: 10px; background: var(--el-bg-color); box-shadow: 0 1px 3px rgba(15, 23, 42, 0.04); transition: border-color 0.2s ease, box-shadow 0.2s ease; }
.settings-section:hover { border-color: var(--el-border-color-light); box-shadow: 0 2px 8px rgba(15, 23, 42, 0.06); }
.settings-section + .settings-section { margin-top: 10px; }
.settings-section__header { display: flex; align-items: center; gap: 8px; padding: 0 10px 0 0; border-radius: 10px 10px 0 0; background: var(--el-fill-color-light); }
.settings-section.is-collapsed .settings-section__header { border-radius: 10px; }
.settings-section__toggle { display: flex; flex: 1; align-items: center; gap: 6px; min-width: 0; padding: 8px 10px; border: 0; background: transparent; color: var(--el-text-color-primary); font-family: inherit; font-size: 14px; font-weight: 700; text-align: left; cursor: pointer; }
.settings-section__toggle.is-static { cursor: default; }
.settings-section__toggle:hover .settings-section__title { color: var(--el-color-primary); }
.settings-section__toggle:focus-visible { outline: 2px solid var(--el-color-primary); outline-offset: -2px; border-radius: 10px; }
.settings-section__chevron { color: var(--el-text-color-secondary); font-size: 12px; transition: transform 0.2s ease; }
.settings-section.is-collapsed .settings-section__chevron { transform: rotate(-90deg); }
.settings-section__icon { color: var(--el-color-primary); }
.settings-section__title { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.settings-section__count { min-width: 18px; padding: 0 6px; border-radius: 999px; background: var(--el-color-primary-light-9); color: var(--el-color-primary); font-size: 11px; line-height: 18px; text-align: center; }
.settings-section__extra { display: flex; flex: 0 0 auto; align-items: center; gap: 6px; }
.settings-section__body { padding: 4px 12px 10px; border-top: 1px solid var(--el-border-color-extra-light); }
</style>
