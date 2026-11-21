<template>
  <div class="tms-maintenance-schedule-container">
    <!-- Main Tabs for Module Features -->
    <el-tabs v-model="state.mainTab" type="border-card" class="main-feature-tabs">
      <!-- Tab 1: Maintenance Schedule -->
      <el-tab-pane :label="$t('lz.label.tmsMaintenanceSchedule.tabDutySchedule')" name="schedule">
        <MaintenanceScheduleTab />
      </el-tab-pane>

      <!-- Tab 2: Maintenance Guide -->
      <el-tab-pane :label="$t('lz.label.tmsMaintenanceSchedule.tabMaintenanceGuide')" name="guide">
        <MaintenanceGuideTab />
      </el-tab-pane>

      <!-- Tab 3: Maintenance Alert with Badge -->
      <el-tab-pane name="alert">
        <template #label>
          <el-badge :value="state.pendingAlertCount" :hidden="state.pendingAlertCount === 0" type="danger" :offset="[10, 0]">
            <span>{{ $t('lz.label.tmsMaintenanceSchedule.tabMaintenanceAlert') }}</span>
          </el-badge>
        </template>
        <MaintenanceAlertTab @update:pending-count="handleUpdatePendingAlertCount" />
      </el-tab-pane>
    </el-tabs>
  </div>
</template>

<script setup lang="ts" name="tmsMaintainSchedule">
import { reactive } from 'vue';
import { useI18n } from 'vue-i18n';
import MaintenanceScheduleTab from './component/maintenanceScheduleTab.vue';
import MaintenanceGuideTab from './component/maintenanceGuideTab.vue';
import MaintenanceAlertTab from './component/maintenanceAlertTab.vue';

const { t: $t } = useI18n();

// State management
const state = reactive({
  mainTab: 'schedule' as 'schedule' | 'guide' | 'alert',
  pendingAlertCount: 0,
});

/**
 * Handle update pending alert count from MaintenanceAlertTab
 */
const handleUpdatePendingAlertCount = (count: number) => {
  state.pendingAlertCount = count;
};
</script>

<style scoped lang="scss">
.tms-maintenance-schedule-container {
  height: calc(100vh - 84px);
  padding: 0;
  background: var(--el-bg-color-page);
}

/* Main feature tabs styling */
.main-feature-tabs {
  height: 100%;
  
  :deep(.el-tabs__content) {
    height: calc(100% - 40px);
    overflow: auto;
    padding: 10px;
  }
  
  :deep(.el-tab-pane) {
    height: 100%;
  }
}
</style>
