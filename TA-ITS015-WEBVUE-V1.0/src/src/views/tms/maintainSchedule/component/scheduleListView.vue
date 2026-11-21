<template>
  <div class="schedule-list-view">
    <vxe-grid 
      ref="xGrid" 
      class="xGrid-style" 
      v-bind="gridOptions" 
      v-on="gridEvents"
    >
      <template #toolbar_buttons>
        <div style="display: flex; align-items: center; gap: 10px; padding: 5px 0;">
          <el-date-picker
            v-model="dateRange"
            type="daterange"
            range-separator="-"
            :start-placeholder="$t('lz.label.base.fromDate')"
            :end-placeholder="$t('lz.label.tmsDutySchedule.toDate')"
            format="DD/MM/YYYY"
            value-format="YYYY-MM-DD"
            @change="handleDateRangeChange"
            size="small"
          />
        </div>
      </template>

      <template #toolbar_tools>
        <TableHeader 
          :show-add="false"
          :show-delete="false"
          @refresh="handleRefresh" 
          @custom-column="handleCustomColumn"
          :loading="loading"
        />
      </template>

      <template #empty>
        <el-empty :image-size="200" :description="$t('lz.label.base.noData')" />
      </template>
      <template #code_default="{ row }">
        <el-link type="primary" @click="handleViewDetail(row)">
          {{ row.code || '-' }}
        </el-link>
      </template>
      <template #name_default="{ row }">
        <!-- {<el-link type="primary" @click="handleViewDetail(row)"> -->
          {{ row.name || '-' }}
        <!-- </el-link>} -->
      </template>

      <template #startTime_default="{ row }">
        {{ formatDateTime(row.startTime) }}
      </template>

      <template #endTime_default="{ row }">
        {{ formatDateTime(row.endTime) }}
      </template>

      <template #patrolTeamId_default="{ row }">
        {{ getPatrolTeamName(row.patrolTeamId) }}
      </template>

      <template #status_default="{ row }">
        <TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.TmsDutyState, row.status || '')" />
      </template>

      <template #currentUserStatus_default="{ row }">
        <template v-if="employeeFilter">
          <el-tag 
            v-if="getCurrentUserStatus(row)"
            size="small" 
            :type="getCurrentUserStatus(row)?.tagType || undefined" 
            :effect="getCurrentUserStatus(row)?.effect || 'plain'"
            :style="getCurrentUserStatus(row)?.style || ''"
          >
            <el-icon size="12">
              <ele-Check v-if="getCurrentUserStatus(row)?.icon === 'Check'" />
              <ele-CircleClose v-else-if="getCurrentUserStatus(row)?.icon === 'CircleClose'" />
              <ele-Sort v-else-if="getCurrentUserStatus(row)?.icon === 'Sort'" />
              <ele-RefreshRight v-else-if="getCurrentUserStatus(row)?.icon === 'RefreshRight'" />
              <ele-Switch v-else-if="getCurrentUserStatus(row)?.icon === 'Switch'" />
            </el-icon>
            <span style="margin-left: 4px;">{{ getCurrentUserStatus(row)?.label }}</span>
          </el-tag>
          <span v-else style="color: var(--el-text-color-placeholder); font-size: 12px;">-</span>
        </template>
        <span v-else style="color: var(--el-text-color-placeholder); font-size: 12px;">-</span>
      </template>

      <template #personnel_default="{ row }">
        <div style="display: flex; flex-direction: row; gap: 4px;">
          <!-- Hiển thị theo nhóm vai trò -->
          <div style="display: flex; align-items: center; gap: 6px; flex-wrap: nowrap;">
            <el-tag 
              v-for="roleGroup in getPersonnelByRole(row)" 
              :key="roleGroup.role"
              size="small" 
              :type="roleGroup.tagType"
              :style="roleGroup.tagColor ? { backgroundColor: roleGroup.tagColor, borderColor: roleGroup.tagColor, color: '#fff' } : undefined"
              effect="plain"
            >
              <el-icon size="12" v-if="roleGroup.icon">
                <component :is="roleGroup.icon" />
              </el-icon>
              <span :style="{ marginLeft: roleGroup.icon ? '4px' : '0' }">
                {{ roleGroup.count }} {{ roleGroup.label }}
              </span>
            </el-tag>
          </div>
        </div>
      </template>

      <template #row_buttons="{ row }">
        <el-tooltip :content="$t('lz.button.base.detail')" placement="top">
          <el-button icon="ele-View" text type="primary" @click="handleViewDetail(row)"></el-button>
        </el-tooltip>
        <el-tooltip :content="$t('lz.button.base.edit')" placement="top" v-if="canEditOrDelete(row)">
          <el-button icon="ele-Edit" text type="primary" @click="handleEdit(row)"></el-button>
        </el-tooltip>
        <el-tooltip :content="$t('lz.button.base.delete')" placement="top" v-if="canEditOrDelete(row)">
          <el-button icon="ele-Delete" text type="danger" @click="handleDelete(row)"></el-button>
        </el-tooltip>
      </template>
    </vxe-grid>
  </div>
</template>

<script lang="ts" setup name="scheduleListView">
import { reactive, ref, watch, defineAsyncComponent } from 'vue';
import { VxeGridInstance, VxeGridListeners } from 'vxe-table';
import { DutyScheduleOutput } from '/@/api-services/tms';
import { useI18n } from 'vue-i18n';
import { useUserInfo } from '/@/stores/userInfo';
import { BaseConfigTypeEnum } from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';

const { t: $t } = useI18n();
const userInfoStore = useUserInfo();
const getConfigItemByCode = useBaseConfig().getConfigItemByCode;
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;

const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));
const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));

const props = defineProps<{
  schedules: DutyScheduleOutput[];
  teamList: any[];
  employeeList?: any[];
  loading?: boolean;
  dateRange?: string[];
  status?: any;
  showEmployeeFilter?: boolean;
  employeeId?: string;
}>();

const emit = defineEmits(['view-detail', 'edit', 'delete', 'refresh', 'date-change', 'status-change', 'employee-change']);

const xGrid = ref<VxeGridInstance>();
const dateRange = ref<string[]>(props.dateRange || []);
const statusFilter = ref(props.status);
const employeeFilter = ref(props.employeeId);

// Grid Options
const gridOptions = reactive({
  border: true,
  resizable: true,
  showOverflow: true,
  loading: false,
  height: 'auto',
  rowConfig: {
    keyField: 'id',
    isHover: true,
  },
  columnConfig: {
    resizable: true,
  },
  data: [] as DutyScheduleOutput[],
  columns: [
    { type: 'seq' as const, width: 60, title: $t('lz.label.base.idx'), fixed: 'left' as const },
    { field: 'code', title: $t('lz.label.tmsDutySchedule.scheduleCode'), minWidth: 120, fixed: 'left' as const, slots: {default: 'code_default'} },
    { field: 'name', title: $t('lz.label.tmsDutySchedule.scheduleName'), minWidth: 200, slots: { default: 'name_default' } },
    { field: 'startTime', title: $t('lz.entity.base.startTime'), width: 160, slots: { default: 'startTime_default' } },
    { field: 'endTime', title: $t('lz.entity.base.endTime'), width: 160, slots: { default: 'endTime_default' } },
    { field: 'patrolTeamId', title: $t('lz.label.tmsDutySchedule.patrolTeam'), width: 150, slots: { default: 'patrolTeamId_default' } },
    { field: 'personnel', title: $t('lz.label.tmsDutySchedule.employee'), minWidth: 160, slots: { default: 'personnel_default' } },
    { field: 'currentUserStatus', title: $t('lz.label.tmsDutySchedule.myScheduleStatus'), width: 140, slots: { default: 'currentUserStatus_default' } },
    { field: 'status', title: $t('lz.label.tmsDutySchedule.scheduleStatus'),align: 'center',  width: 120, slots: { default: 'status_default' } },
	{ title: $t('lz.label.base.action'), fixed: 'right' as const, width: 100, showOverflow: true, slots: { default: 'row_buttons' } },
  ],
  toolbarConfig: {
    slots: {
      buttons: 'toolbar_buttons',
      tools: 'toolbar_tools',
    },
  },
});

const gridEvents: VxeGridListeners = {
  toolbarToolClick() {
    // Handle toolbar clicks if needed
  },
};

// Watch for schedule changes
watch(() => props.schedules, (newSchedules) => {
  gridOptions.data = newSchedules || [];
  if (xGrid.value) {
    xGrid.value.reloadData(newSchedules || []);
  }
}, { immediate: true, deep: true });

// Watch for loading state
watch(() => props.loading, (newLoading) => {
  gridOptions.loading = newLoading || false;
});

// Watch for date range changes from parent
watch(() => props.dateRange, (newRange) => {
  if (newRange) {
    dateRange.value = newRange;
  }
});

// Watch for status changes from parent
watch(() => props.status, (newStatus) => {
  statusFilter.value = newStatus;
});

// Watch for employee changes from parent
watch(() => props.employeeId, (newEmployeeId) => {
  console.log('scheduleListView - employeeId changed:', newEmployeeId);
  employeeFilter.value = newEmployeeId;
});

// Helper functions
const formatDateTime = (date: Date | string | undefined) => {
  if (!date) return '-';
  const d = new Date(date);
  return d.toLocaleString('vi-VN', { 
    day: '2-digit', 
    month: '2-digit', 
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  });
};

const getPatrolTeamName = (teamId: string | null | undefined) => {
  if (!teamId) return '-';
  const team = props.teamList.find((t: any) => t.id === teamId);
  return team?.name || teamId;
};

/**
 * Get personnel grouped by role with dynamic config labels
 * @param row - Schedule row
 * @returns Array of role groups with count, label, icon, and styling
 */
const getPersonnelByRole = (row: DutyScheduleOutput) => {
  if (!row.details || row.details.length === 0) return [];
  
  // Get all role configs
  const roleConfigs = getConfigDataByCode(BaseConfigTypeEnum.TmsDutyPatronRole);
  
  // Group personnel by role
  const roleGroups: any[] = [];
  
  // Count by each role from config
  roleConfigs.forEach((roleConfig: any, index: number) => {
    const count = (row.details || []).filter((d: any) => 
      (d.role || '').toLowerCase() === (roleConfig.code || '').toLowerCase()
    ).length;
    
    if (count > 0) {
      // Use index to determine tag type and icon (first role is typically leader)
      // Or use color from config if available
      roleGroups.push({
        role: roleConfig.code,
        count: count,
        label: roleConfig.name || roleConfig.code,
        tagType: roleConfig.tagType,
        tagColor: roleConfig.color || undefined,
        icon: roleConfig.code == 'leader' ? 'ele-StarFilled' : 'ele-User'
      });
    }
  });
  
  return roleGroups;
};

/**
 * Get current filtered user's status in this schedule
 * @param row - Schedule row
 * @returns Status object with label, icon, and styling, or null if user not found
 */
const getCurrentUserStatus = (row: DutyScheduleOutput) => {
  if (!employeeFilter.value || !row.details || row.details.length === 0) {
    return null;
  }
  
  // Find the filtered user in this schedule's details
  const userDetail = row.details.find((d: any) => d.userId === employeeFilter.value);
  
  if (!userDetail) {
    return null;
  }
  
  // Get user's status
  const status = userDetail.status || 'active';
  
  // Get status config from base config
  const statusConfig = getConfigItemByCode(BaseConfigTypeEnum.TmsDutyPatronState, status);
  
  if (!statusConfig) {
    return null;
  }
  
  // Map icon names based on status code
  const iconMap: Record<string, string> = {
    'active': 'Check',
    'absent': 'CircleClose',
    'swap': 'Sort',
    'replace': 'RefreshRight',
    'transfer': 'Switch'
  };
  
  return {
    label: statusConfig.name || status,
    tagType: statusConfig.tagType || '',
    effect: 'plain',
    icon: iconMap[status] || 'Check',
    style: statusConfig.color ? `border-color: ${statusConfig.color}; color: ${statusConfig.color};` : ''
  };
};

// Check if current user can edit/delete (creator or leader)
const canEditOrDelete = (row: DutyScheduleOutput) => {
  const currentUserId = userInfoStore.userInfos?.account || '';
  
  // Check if current user is the creator
  if (row.createUId === currentUserId) return true;
  
  // Check if current user is a leader in this schedule
  if (row.details) {
    const userDetail = row.details.find((d: any) => d.userId === currentUserId);
    if (userDetail && userDetail.role === 'leader') {
      return true;
    }
  }
  
  return false;
};

// Event handlers
const handleViewDetail = (row: DutyScheduleOutput) => {
  emit('view-detail', row);
};

const handleEdit = (row: DutyScheduleOutput) => {
  emit('edit', row);
};

const handleDelete = (row: DutyScheduleOutput) => {
  emit('delete', row);
};

const handleRefresh = () => {
  emit('refresh');
};

const handleDateRangeChange = (value: string[]) => {
  emit('date-change', value);
};

// Handle custom column
const handleCustomColumn = () => {
  xGrid.value?.openCustom();
};
</script>

<style scoped>
.schedule-list-view {
  width: 100%;
  height: 100%;
}
</style>
