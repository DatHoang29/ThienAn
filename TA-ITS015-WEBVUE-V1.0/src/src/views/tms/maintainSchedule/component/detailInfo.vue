<template>
  <div class="detail-info-container">
    <!-- Basic Information Table -->
    <table class="table-data w100">
      <tr class="title">
        <th colspan="4">
          <p>{{ $t('lz.label.tmsDutySchedule.scheduleCode') }}: {{ detail?.code || '-' }}</p>
        </th>
      </tr>
      <tr class="alt">
        <th style="width: 20%">{{ $t('lz.label.tmsDutySchedule.scheduleName') }}</th>
        <td style="width: 30%">{{ detail?.name || '-' }}</td>
        <th style="width: 20%">{{ $t('lz.label.tmsDutySchedule.patrolTeam') }}</th>
        <td style="width: 30%">
          <div v-if="getPatrolTeamInfo(detail?.patrolTeamId)">
            <span style="font-weight: 500;">{{ getPatrolTeamInfo(detail?.patrolTeamId)?.name }}</span>
            <div v-if="getPatrolTeamInfo(detail?.patrolTeamId)?.description" style="font-size: 12px; color: var(--el-text-color-secondary); margin-top: 2px;">
              {{ getPatrolTeamInfo(detail?.patrolTeamId)?.description }}
            </div>
          </div>
          <span v-else>-</span>
        </td>
      </tr>
      <tr>
        <th>{{ $t('lz.entity.base.startTime') }}</th>
        <td>{{ formatDateTime(detail?.startTime) }}</td>
        <th>{{ $t('lz.entity.base.endTime') }}</th>
        <td>{{ formatDateTime(detail?.endTime) }}</td>
      </tr>
      <tr class="alt" v-if="detail?.remark">
        <th>{{ $t('lz.entity.base.remark') }}</th>
        <td colspan="3">{{ detail.remark }}</td>
      </tr>
    </table>

    <!-- User List -->
    <el-card shadow="hover" v-if="detail?.details && detail?.details.length > 0" style="margin-top: 20px;">
      <template #header>
        <h3 style="margin: 0; font-size: 16px;">
          <el-icon style="margin-right: 5px;"><ele-User /></el-icon>
          {{ $t('lz.label.tmsDutySchedule.employeeList') }}
        </h3>
      </template>

      <!-- Group by role from config -->
      <div v-for="roleConfig in roleConfigs" :key="roleConfig.code" style="margin-bottom: 15px;">
        <template v-if="getMembersByRole(roleConfig.code).length > 0">
          <div style="font-weight: bold; margin-bottom: 8px; display: flex; align-items: center;" :style="{ color: roleConfig.color || 'var(--el-text-color-primary)' }">
            <el-icon v-if="roleConfig.code === 'leader'" style="margin-right: 5px;"><ele-StarFilled /></el-icon>
            <el-icon v-else style="margin-right: 5px;"><ele-UserFilled /></el-icon>
            {{ roleConfig.name }}:
          </div>
          <div style="display: flex; flex-wrap: wrap; gap: 8px">
            <el-tag 
              v-for="member in getMembersByRole(roleConfig.code)" 
              :key="member.userId"
              :type="roleConfig.tagType || 'info'"
              :style="roleConfig.color ? { backgroundColor: roleConfig.color, borderColor: roleConfig.color, color: '#fff' } : undefined"
              effect="plain"
              size="default"
            >
              <el-icon v-if="roleConfig.code === 'leader'" style="margin-right: 3px;"><ele-StarFilled /></el-icon>
              <el-icon v-else style="margin-right: 3px;"><ele-User /></el-icon>
              <el-tooltip :content="`Username: ${getUsername(member.userId)}`" placement="top">
                <span style="cursor: help;">{{ getUserName(member.userId) }}</span>
              </el-tooltip>
            </el-tag>
          </div>
        </template>
      </div>
    </el-card>

    <!-- Timestamps Table -->
    <table class="table-data w100" style="margin-top: 20px;">
      <tr class="alt">
        <th style="width: 20%">{{ $t('lz.entity.base.createBy') }}</th>
        <td style="width: 30%">{{ getUserName(detail?.createUId) }}</td>
        <th style="width: 20%">{{ $t('lz.entity.base.createTime') }}</th>
        <td style="width: 30%">{{ formatDateTime(detail?.createTime) }}</td>
      </tr>
      <tr v-if="detail?.updateUId || detail?.updateTime">
        <th>{{ $t('lz.entity.base.updateBy') }}</th>
        <td>{{ getUserName(detail?.updateUId) || '-' }}</td>
        <th>{{ $t('lz.entity.base.updateTime') }}</th>
        <td>{{ formatDateTime(detail?.updateTime || undefined) }}</td>
      </tr>
    </table>
  </div>
</template>

<script lang="ts" setup name="DetailInfo">
import { useI18n } from 'vue-i18n';


import { defineProps, computed, reactive, onMounted } from 'vue';
import { DutyScheduleOutput } from '/@/api-services/tms';
import { getAPI } from '/@/utils/axios-utils';
import { SysUserApi } from '/@/api-services/system';
import { useBaseConfig } from '/@/stores/baseConfig';
import { BaseConfigTypeEnum } from '/@/types/enums';

const { t: $t } = useI18n();

// Get base config for role display
const { getConfigDataByCode, getConfigItemByCode } = useBaseConfig();

const props = defineProps<{
  detail: DutyScheduleOutput | null;
  teamList?: Array<{ id: string; name: string; description?: string }>;
}>();

// State for user data
const state = reactive({
  allUsers: [] as any[],
});

// Load all users
const loadAllUsers = async () => {
  try {
    const response = await getAPI(SysUserApi).apiSystemSysuserListPost({});
    if (response.data?.result) {
      state.allUsers = response.data.result;
    }
  } catch (error) {
    console.error('Error loading users:', error);
  }
};

onMounted(() => {
  loadAllUsers();
});

// Get patrol team name by ID
const getPatrolTeamName = (teamId: string | null | undefined) => {
  if (!teamId) return '-';
  if (!props.teamList || props.teamList.length === 0) return teamId;
  const team = props.teamList.find((t: any) => t.id === teamId);
  return team?.name || teamId;
};

// Get patrol team info (name + description)
const getPatrolTeamInfo = (teamId: string | null | undefined) => {
  if (!teamId) return null;
  if (!props.teamList || props.teamList.length === 0) return null;
  return props.teamList.find((t: any) => t.id === teamId) || null;
};

// Get role configs from base config
const roleConfigs = computed(() => getConfigDataByCode(BaseConfigTypeEnum.TmsDutyPatronRole) || []);

// Get members by role code
const getMembersByRole = (roleCode: string) => {
  if (!props.detail?.details) return [];
  return props.detail.details.filter(d => d.role?.toLowerCase() === roleCode?.toLowerCase());
};

// Get leader role code from config (first role that is typically 'leader')
const getLeaderRoleCode = computed(() => {
  const leaderConfig = roleConfigs.value.find((r: any) => r.code === 'leader');
  return leaderConfig?.code || 'leader';
});

// Get role display name from config
const getRoleDisplayName = (roleCode: string) => {
  const config = getConfigItemByCode(BaseConfigTypeEnum.TmsDutyPatronRole, roleCode?.toLowerCase());
  return config?.name || roleCode;
};

// Computed: Leaders - using config role code
const leaders = computed(() => {
  if (!props.detail?.details) return [];
  return props.detail.details.filter(d => d.role?.toLowerCase() === getLeaderRoleCode.value?.toLowerCase());
});

// Computed: Members - all non-leader roles
const members = computed(() => {
  if (!props.detail?.details) return [];
  return props.detail.details.filter(d => d.role?.toLowerCase() !== getLeaderRoleCode.value?.toLowerCase());
});

// Format date time
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

// Get status type for tag
const getStatusType = (status: any) => {
  if (status === 'Registered') return 'info';
  if (status === 'Confirmed') return 'primary';
  if (status === 'InProgress') return 'warning';
  if (status === 'Completed') return 'success';
  if (status === 'Cancelled') return 'danger';
  return 'info';
};

// Get status label
const getStatusLabel = (status: any) => {
  if (status === 'Registered') return $t('lz.label.tmsDutySchedule.statusRegistered');
  if (status === 'Confirmed') return $t('lz.label.tmsDutySchedule.statusConfirmed');
  if (status === 'InProgress') return $t('lz.label.tmsDutySchedule.statusInProgress');
  if (status === 'Completed') return $t('lz.label.tmsDutySchedule.statusCompleted');
  if (status === 'Cancelled') return $t('lz.label.tmsDutySchedule.statusCancelled');
  return status || $t('lz.label.base.unknown');
};

// Get step class for process arrows
const getStepClass = (stepStatus: string) => {
  if (!props.detail) return '';
  
  const statusOrder = ['Registered', 'Confirmed', 'InProgress', 'Completed'];
  const currentIndex = statusOrder.indexOf(props.detail.status || '');
  const stepIndex = statusOrder.indexOf(stepStatus);
  
  if (props.detail.status === 'Cancelled') {
    return 'step-cancelled';
  }
  
  if (stepIndex < currentIndex) {
    return 'step-completed';
  } else if (stepIndex === currentIndex) {
    return 'step-active';
  } else {
    return 'step-pending';
  }
};

// Get user name by username (returns real name)
const getUserName = (username: string | null | undefined) => {
  if (!username) return '-';
  const user = state.allUsers.find((u: any) => u.username === username);
  return user?.realName || user?.name || username;
};

// Get username for tooltip
const getUsername = (username: string | null | undefined) => {
  return username || '-';
};
</script>

<style scoped>

/* Status Progress Compact */
.status-progress-compact {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 8px 12px;
  background-color: var(--el-fill-color-lighter);
  border-radius: 6px;
  width: fit-content;
}

.process-steps-mini {
  display: flex;
  gap: 4px;
  align-items: center;
}

.process-step-mini {
  width: 28px;
  height: 28px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 12px;
  font-weight: bold;
  background: #C0C4CC;
  color: white;
  transition: all 0.3s;
  cursor: help;
  position: relative;

  &::after {
    content: '';
    position: absolute;
    right: -6px;
    width: 4px;
    height: 2px;
    background: #C0C4CC;
  }

  &:last-child::after {
    display: none;
  }

  &.step-completed {
    background: linear-gradient(135deg, #67C23A 0%, #85CE61 100%);
    
    &::after {
      background: #67C23A;
    }
  }

  &.step-active {
    background: linear-gradient(135deg, #E6A23C 0%, #EEBE77 100%);
    animation: pulse-mini 1.5s ease-in-out infinite;
    
    &::after {
      background: #E6A23C;
    }
  }

  &.step-cancelled {
    background: #F56C6C;
    opacity: 0.6;
    
    &::after {
      background: #F56C6C;
    }
  }
}

@keyframes pulse-mini {
  0%, 100% {
    transform: scale(1);
    box-shadow: 0 0 0 0 rgba(230, 162, 60, 0.4);
  }
  50% {
    transform: scale(1.1);
    box-shadow: 0 0 0 4px rgba(230, 162, 60, 0);
  }
}

.status-text {
  display: flex;
  align-items: center;
}

/* Table Data Styles */
.w100 {
  width: 100%;
}

.table-data {
  border-collapse: collapse;
  font-size: 14px;
}

.table-data th,
.table-data td {
  border: 1px solid var(--el-border-color-light);
  padding: 10px 12px;
  text-align: left;
}

.table-data th {
  background-color: var(--el-fill-color-light);
  font-weight: 600;
  color: var(--el-text-color-primary);
}

.table-data td {
  color: var(--el-text-color-regular);
}

.table-data tr.title th {
  background: linear-gradient(135deg, var(--el-color-primary) 0%, var(--el-color-primary-light-3) 100%);
  color: white;
  font-size: 15px;
  font-weight: 600;
  text-align: center;
}

.table-data tr.title p {
  margin: 0;
  padding: 5px 0;
}

.table-data tr.alt {
  background-color: var(--el-fill-color-lighter);
}

.table-data tr:hover:not(.title) {
  background-color: var(--el-fill-color);
}
</style>
