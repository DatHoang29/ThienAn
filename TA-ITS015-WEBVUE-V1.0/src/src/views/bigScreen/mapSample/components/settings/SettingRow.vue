<template>
  <!-- 1 dòng cài đặt: nhãn + mô tả ngắn bên trái, điều khiển bên phải (hoặc xuống dòng khi `stacked`) -->
  <div class="setting-row" :class="{ 'setting-row--stacked': stacked }">
    <div class="setting-row__text">
      <span class="setting-row__label">{{ label }}</span>
      <small v-if="hint" class="setting-row__hint">{{ hint }}</small>
    </div>
    <div class="setting-row__control"><slot /></div>
  </div>
</template>

<script setup lang="ts">
withDefaults(defineProps<{ label: string; hint?: string; stacked?: boolean }>(), { hint: '', stacked: false });
</script>

<style scoped>
.setting-row { display: flex; align-items: center; justify-content: space-between; gap: 8px 12px; padding: 10px 0; }
.setting-row + .setting-row { border-top: 1px solid var(--el-border-color-extra-light); }
.setting-row--stacked { flex-direction: column; align-items: stretch; }
.setting-row__text { display: flex; flex-direction: column; gap: 3px; min-width: 0; }
.setting-row__label { color: var(--el-text-color-primary); font-size: 14px; font-weight: 600; }
.setting-row__hint { color: var(--el-text-color-secondary); font-size: 12px; line-height: 1.45; }
.setting-row__control { display: flex; flex: 0 0 auto; align-items: center; justify-content: flex-end; gap: 6px; color: var(--el-text-color-secondary); font-size: 13px; }
.setting-row--stacked .setting-row__control { justify-content: flex-start; }
</style>
