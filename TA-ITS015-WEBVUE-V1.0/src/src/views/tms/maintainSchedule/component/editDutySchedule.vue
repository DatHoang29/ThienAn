<template>
  <div class="tms-duty-schedule-modal-container">
    <el-dialog 
      v-model="state.isShowDialog" 
      draggable 
      :close-on-click-modal="false" 
      width="800px"
      :destroy-on-close="true"
    >
      <template #header>
        <div style="color: #fff">
          <el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Edit />
          </el-icon>
          <span> {{ props.title }} </span>
        </div>
      </template>
      
      <el-form 
        :model="state.ruleForm" 
        ref="ruleFormRef" 
        label-width="auto"
      >
        <el-alert
          v-if="state.isLimitedEdit"
          :title="$t('lz.label.tmsDutySchedule.limitedEditWarning')"
          type="warning"
          :closable="false"
          style="margin-bottom: 15px;"
        >
          <template #default>
            {{ $t('lz.label.tmsDutySchedule.limitedEditDescription') }}
          </template>
        </el-alert>
        
        <Fieldset :legend="$t('lz.label.tmsDutySchedule.scheduleInfo')" :toggleable="false">
          <el-row :gutter="10">
            <el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb10">
              <el-form-item 
                :label="$t('lz.label.tmsDutySchedule.scheduleName')" 
                prop="name" 
                label-position="left"
                :rules="[{ required: true, message: $t('lz.message.tmsDutySchedule.pleaseInputScheduleName'), trigger: 'blur' }]"
              >
                <el-input 
                  v-model="state.ruleForm.name" 
                  :placeholder="$t('lz.label.tmsDutySchedule.scheduleNamePlaceholder')" 
                  clearable
                />
              </el-form-item>
            </el-col>
            
            <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb10">
              <el-form-item 
                :label="$t('lz.entity.base.startTime')"
                label-position="left"
                prop="startTime" 
                :rules="[{ required: true, message: $t('lz.message.tmsDutySchedule.pleaseSelectTime'), trigger: 'change' }]"
              >
                <el-date-picker 
                  v-model="state.ruleForm.startTime" 
                  type="datetime" 
                  :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.base.startTime') })"
                  format="DD/MM/YYYY HH:mm"
                  style="width: 100%;"
                  :disabled="state.isLimitedEdit"
                />
              </el-form-item>
            </el-col>
            
            <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb10">
              <el-form-item 
                :label="$t('lz.entity.base.endTime')"
                label-position="left"
                prop="endTime" 
                :rules="[{ required: true, message: $t('lz.message.tmsDutySchedule.pleaseSelectTime'), trigger: 'change' }]"
              >
                <el-date-picker 
                  v-model="state.ruleForm.endTime" 
                  type="datetime" 
                  :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.base.endTime') })"
                  format="DD/MM/YYYY HH:mm"
                  style="width: 100%;"
                  :disabled="state.isLimitedEdit"
                />
              </el-form-item>
            </el-col>
            
            <!-- Status (show for both add and edit modes) -->
            <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb10" v-if="!state.isLimitedEdit">
              <el-form-item 
                :label="$t('lz.entity.base.status')" 
                label-position="left"
                prop="status"
                :rules="[{ required: true, message: $t('lz.message.tmsDutySchedule.pleaseSelectStatus'), trigger: 'change' }]"
              >
                <el-select 
                  v-model="state.ruleForm.status" 
                  :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.base.status') })"
                  style="width: 100%;"
                >
                  <el-option
                    v-for="statusConfig in (state.isLimitedEdit ? getConfigDataByCode('tms_duty_state').filter((s: any) => s.code === 'cancel') : getConfigDataByCode('tms_duty_state'))"
                    :key="statusConfig.code"
                    :label="statusConfig.name"
                    :value="statusConfig.code"
                  >
                    <span>{{ statusConfig.name }}</span>
                    <span 
                      v-if="statusConfig.remark" 
                      style="color: var(--el-text-color-secondary); font-size: 12px; margin-left: 8px;"
                    >
                      - {{ statusConfig.remark }}
                    </span>
                  </el-option>
                </el-select>
              </el-form-item>
            </el-col>
          </el-row>
          
          <!-- Remark field (show always, especially in limited edit) -->
          <el-row :gutter="10" v-if="state.isLimitedEdit || props.operateType === 'edit'">
            <el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb10">
              <el-form-item 
                :label="$t('lz.entity.base.remark')"
                label-position="left"
                prop="remark"
              >
                <el-input
                  v-model="state.ruleForm.remark"
                  type="textarea"
                  :rows="3"
                  :placeholder="$t('lz.label.tmsDutySchedule.remarkPlaceholderInput')"
                />
              </el-form-item>
            </el-col>
          </el-row>
        </Fieldset>
        
        <!-- Phân công nhân sự (hidden in limited edit mode) -->
        <Fieldset v-if="!state.isLimitedEdit" :legend="$t('lz.label.tmsDutySchedule.personnelAssignment')" :toggleable="false" style="margin-top: 15px;">
          <!-- Quick actions -->
          <div class="quick-actions-bar mb15">
            <el-button 
              type="primary" 
              :icon="UserFilled" 
              size="small"
              @click="openTeamSelectModal"
            >
              {{ $t('lz.label.tmsDutySchedule.quickAddByTeam') }}
            </el-button>
            <el-button 
              type="info" 
              :icon="Setting" 
              size="small"
              plain
              @click="openTeamManageModal"
            >
              {{ $t('lz.label.tmsDutySchedule.manageTeam') }}
            </el-button>
          </div>
          
          <el-row :gutter="10">
            <!-- Trưởng đội -->
            <el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb10">
              <el-form-item :label="$t('lz.label.tmsDutySchedule.teamLeader')" label-position="left">
                <el-select
                  ref="leaderSelectRef"
                  v-model="state.selectedLeaderIds"
                  multiple
                  filterable
                  :placeholder="$t('lz.label.tmsDutySchedule.selectLeader')"
                  style="width: 100%"
                  @change="handleLeaderChange"
                  @visible-change="handleLeaderVisibleChange"
                >
                  <el-option
                    v-for="user in state.allUsers"
                    :key="user.username"
                    :label="`${user.name} (${user.username})`"
                    :value="user.username"
                  >
                    <div style="display: flex; align-items: center; gap: 8px;">
                      <el-icon><ele-User /></el-icon>
                      <span>{{ user.name }}</span>
                      <el-tooltip :content="user.realName || user.name" placement="top">
                        <span style="color: #999; font-size: 12px; cursor: help;">({{ user.username }})</span>
                      </el-tooltip>
                    </div>
                  </el-option>
                </el-select>
              </el-form-item>
            </el-col>
            
            <!-- Thành viên -->
          <el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb10">
            <el-form-item :label="$t('lz.label.tmsDutySchedule.membersLabel')" label-position="left">
              <el-select
                ref="userSelectRef"
                v-model="state.selectedUserIds"
                multiple
                filterable
                :placeholder="$t('lz.label.tmsDutySchedule.selectMember')"
                style="width: 100%"
                @change="handleUserChange"
                @visible-change="handleUserVisibleChange"
              >
                <el-option
                  v-for="user in state.allUsers"
                  :key="user.username"
                  :label="`${user.name} (${user.username})`"
                  :value="user.username"
                >
                  <div style="display: flex; align-items: center; gap: 8px;">
                    <el-icon><ele-User /></el-icon>
                    <span>{{ user.name }}</span>
                    <el-tooltip :content="user.realName || user.name" placement="top">
                      <span style="color: #999; font-size: 12px; cursor: help;">({{ user.username }})</span>
                    </el-tooltip>
                  </div>
                </el-option>
              </el-select>
            </el-form-item>
          </el-col>
          
          <!-- Save as new team option -->
          <el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
            <el-form-item>
              <el-checkbox v-model="state.saveAsNewTeam" @change="handleSaveAsTeamChange">
                {{ $t('lz.label.tmsDutySchedule.saveAsNewTeam') }}
              </el-checkbox>
            </el-form-item>
            
            <!-- Team name input (show when checkbox is checked) -->
            <el-form-item 
              v-if="state.saveAsNewTeam" 
              :label="$t('lz.label.tmsDutySchedule.newTeamName')"
              prop="newTeamName"
              label-position="left"
              style="margin-top: 10px;"
            >
              <el-input 
                v-model="state.newTeamName" 
                :placeholder="$t('lz.label.tmsDutySchedule.newTeamNamePlaceholder')" 
                clearable
              >
                <template #prepend>
                  <i class="fa fa-group"></i>
                </template>
              </el-input>
            </el-form-item>
          </el-col>
        </el-row>
        </Fieldset>
      </el-form>
      
      <template #footer>
        <span class="dialog-footer">
          <el-button @click="cancel">{{ $t('lz.button.base.cancel') }}</el-button>
          <el-button type="primary" @click="submit" :loading="state.loading">{{ $t('lz.button.base.save') }}</el-button>
        </span>
      </template>
    </el-dialog>
    <TeamQuickSelect 
      ref="teamQuickSelectRef" 
      @select="handleTeamSelect" 
      @edit="handleTeamEditFromQuickSelect"
      @add="handleTeamAddFromQuickSelect"
    />
    
    <!-- Patrol Team Management Component -->
    <PatrolTeamManage ref="patrolTeamManageRef" @refresh="handleTeamRefresh" />
  </div>
</template>

<script lang="ts" setup>
import { reactive, ref, computed } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { UserFilled, Setting } from '@element-plus/icons-vue';

const { t: $t } = useI18n();
import Fieldset from 'primevue/fieldset';
import { getAPI } from '/@/utils/axios-utils';
import { TmsDutyScheduleApi, TmsPatrolTeamApi } from '/@/api-services/tms';
import { SysUserApi } from '/@/api-services/system';
import { useBaseConfig } from '/@/stores/baseConfig';
import type { AddDutyScheduleInput, UpdateDutyScheduleInput, DutyScheduleDetailInput } from '/@/api-services/tms';
import TeamQuickSelect from './teamQuickSelect.vue';
import PatrolTeamManage from './patrolTeamManage.vue';

interface Props {
  operateType: string;
  title: string;
}
const props = defineProps<Props>();
const emits = defineEmits(['handleQuery', 'handleAddSchedule']);

const getConfigItemByCode = useBaseConfig().getConfigItemByCode;
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;

interface User {
  id: string;
  name: string;
  username: string;
  realName?: string;
}

const ruleFormRef = ref();
const leaderSelectRef = ref();
const userSelectRef = ref();
const teamQuickSelectRef = ref();
const patrolTeamManageRef = ref();

const state = reactive({
  isShowDialog: false,
  loading: false,
  showTimeOptions: false,
  isLimitedEdit: false, // Flag for limited edit mode (only name, remark, status to cancel)
  
  ruleForm: {
    id: undefined as string | undefined,
    name: '',
    startTime: new Date(),
    endTime: new Date(),
    patrolTeamId: undefined as string | undefined,
    status: 'register' as string, // Default: register (Đăng ký)
    remark: '' as string | undefined,
  },
  
  // Patrol team selection
  selectedPatrolTeam: undefined as string | undefined,
  teamList: [] as any[],
  
  // User selection with dropdown (using username as key)
  selectedUserIds: [] as string[], // Now stores usernames
  selectedUsers: [] as User[],
  allUsers: [] as User[],
  
  // Leader selection with dropdown (using username as key)
  selectedLeaderIds: [] as string[], // Now stores usernames
  selectedLeaders: [] as User[],
  
  // Save as new team
  saveAsNewTeam: false,
  newTeamName: '',
});

// Helper: Get user full name by username
const getUserFullName = (username: string) => {
  const user = state.allUsers.find(u => u.username === username);
  return user?.name || user?.realName || username;
};

// Load data
const loadTeamList = async () => {
  try {
    const response = await getAPI(TmsPatrolTeamApi).apiTmsTmspatrolteamListGet(
      undefined, undefined, 1
    );
    if (response.data?.result) {
      state.teamList = response.data.result;
    }
  } catch (error) {
    console.error('Error loading team list:', error);
  }
};

const loadAllUsers = async () => {
  try {
    const response = await getAPI(SysUserApi).apiSystemSysuserListPost({});
    if (response.data?.result) {
      state.allUsers = response.data.result.map((u: any) => ({
        id: u.id,
        name: u.realName || u.name || u.username,
        username: u.username,
        realName: u.realName,
      }));
    }
  } catch (error) {
    console.error('Error loading users:', error);
  }
};

const openDialog = async (row?: any) => {
  
  state.isShowDialog = true;
  
  // Check if this is limited edit mode
  state.isLimitedEdit = row?.isLimitedEdit || false;
  
  // Load dropdown data
  await loadTeamList();
  await loadAllUsers();
  
  
  if (props.operateType === 'add') {
    // Check if this is a copy operation
    if (row?.copyFrom) {
      // Copy mode - clone data from existing schedule
      const sourceSchedule = row.copyFrom;
      const now = new Date();
      const startTime = new Date(now.getTime());
      const endTime = new Date(startTime.getTime() + 2 * 60 * 60 * 1000); // +2 hours
      
      state.ruleForm = {
        id: undefined, // New schedule, no ID
        name: sourceSchedule.name ? `${sourceSchedule.name} (Copy)` : '',
        startTime: startTime,
        endTime: endTime,
        patrolTeamId: sourceSchedule.patrolTeamId || undefined,
        status: 'register', // Default for copy mode
        remark: sourceSchedule.remark || '',
      };
      
      // Copy users and leaders from source schedule
      if (sourceSchedule.details && sourceSchedule.details.length > 0) {
        const leaders = sourceSchedule.details.filter((d: any) => d.role === 'leader');
        const members = sourceSchedule.details.filter((d: any) => d.role !== 'leader');
        
        // Find user objects from allUsers by username
        state.selectedLeaders = leaders
          .map((d: any) => state.allUsers.find((u: any) => u.username === d.userId))
          .filter((u: any) => u) as any[];
        
        state.selectedUsers = members
          .map((d: any) => state.allUsers.find((u: any) => u.username === d.userId))
          .filter((u: any) => u) as any[];
        
        // Set selected IDs to usernames
        state.selectedLeaderIds = state.selectedLeaders.map(u => u.username);
        state.selectedUserIds = state.selectedUsers.map(u => u.username);
      } else {
        state.selectedUsers = [];
        state.selectedLeaders = [];
        state.selectedUserIds = [];
        state.selectedLeaderIds = [];
      }
      
      state.selectedPatrolTeam = state.ruleForm.patrolTeamId;
    } else {
      // Normal add mode - auto fill time
      const now = new Date();
      const startTime = row?.startDate ? new Date(row.startDate) : now;
      const endTime = new Date(startTime.getTime() + 2 * 60 * 60 * 1000); // +2 hours
      
      
      state.ruleForm = {
        id: undefined,
        name: '',
        startTime: startTime,
        endTime: endTime,
        patrolTeamId: row?.patrolTeamId || undefined,
        status: 'register', // Default for add mode
        remark: '',
      };
      
      
      state.selectedUsers = [];
      state.selectedLeaders = [];
      state.selectedUserIds = [];
      state.selectedLeaderIds = [];
      state.selectedPatrolTeam = state.ruleForm.patrolTeamId;
    }
  } else {
    // Edit mode - load existing schedule
    
    if (row?.schedule) {
      // Use schedule object passed from parent
      const schedule = row.schedule;
      
      
      const startTime = schedule.startTime ? new Date(schedule.startTime) : new Date();
      const endTime = schedule.endTime ? new Date(schedule.endTime) : new Date();
      
      
      state.ruleForm = {
        id: schedule.id || undefined,
        name: schedule.name || '',
        startTime: startTime,
        endTime: endTime,
        patrolTeamId: schedule.patrolTeamId || undefined,
        status: schedule.status || 'register', // Use existing status or default
        remark: schedule.remark || '',
      };
      
      
      // Load users and leaders from details
      if (schedule.details && schedule.details.length > 0) {
        
        const leaders = schedule.details.filter((d: any) => d.role === 'leader');
        const members = schedule.details.filter((d: any) => d.role !== 'leader');
        
        
        // Find user objects from allUsers by username
        state.selectedLeaders = leaders
          .map((d: any) => state.allUsers.find((u: any) => u.username === d.userId))
          .filter((u: any) => u) as any[];
        
        state.selectedUsers = members
          .map((d: any) => state.allUsers.find((u: any) => u.username === d.userId))
          .filter((u: any) => u) as any[];
        
        
        // Sync usernames for el-select
        state.selectedLeaderIds = state.selectedLeaders.map(u => u.username);
        state.selectedUserIds = state.selectedUsers.map(u => u.username);
        
      } else {
        state.selectedUsers = [];
        state.selectedLeaders = [];
        state.selectedUserIds = [];
        state.selectedLeaderIds = [];
      }
      
      state.selectedPatrolTeam = state.ruleForm.patrolTeamId;
    } else if (row?.id) {
      // Fallback: fetch by ID if only ID is provided
      
      try {
        // Fetch schedule detail from API to get users/leaders
        const response = await getAPI(TmsDutyScheduleApi).apiTmsTmsdutyscheduleGetlistPost({
          // No filters, get all to find by ID
        });
        
        
        const schedule = response.data?.result?.find((s: any) => s.id === row.id);
        
        
        if (schedule) {
          state.ruleForm = {
            id: schedule.id || undefined,
            name: schedule.name || '',
            startTime: schedule.startTime ? new Date(schedule.startTime) : new Date(),
            endTime: schedule.endTime ? new Date(schedule.endTime) : new Date(),
            patrolTeamId: schedule.patrolTeamId || undefined,
            status: schedule.status || 'register',
            remark: schedule.remark || '',
          };
          
          
          // Load users and leaders from details
          if (schedule.details && schedule.details.length > 0) {
            
            const leaders = schedule.details.filter((d: any) => d.role === 'leader');
            const members = schedule.details.filter((d: any) => d.role !== 'leader');
            
            
            // Find user objects from allUsers by username
            state.selectedLeaders = leaders
              .map((d: any) => state.allUsers.find((u: any) => u.username === d.userId))
              .filter((u: any) => u) as any[];
            
            state.selectedUsers = members
              .map((d: any) => state.allUsers.find((u: any) => u.username === d.userId))
              .filter((u: any) => u) as any[];
            
            
            // Sync usernames for el-select
            state.selectedLeaderIds = state.selectedLeaders.map(u => u.username);
            state.selectedUserIds = state.selectedUsers.map(u => u.username);
            
          } else {
            state.selectedUsers = [];
            state.selectedLeaders = [];
          }
          
          state.selectedPatrolTeam = state.ruleForm.patrolTeamId;
        } else {
          console.error('openDialog - Schedule not found by ID:', row.id);
        }
      } catch (error) {
        console.error('Error loading schedule for edit:', error);
        ElMessage.error($t('lz.message.tmsDutySchedule.loadScheduleError'));
      }
    }
  }
};

const cancel = () => {
  state.isShowDialog = false;
  state.selectedUserIds = [];
  state.selectedLeaderIds = [];
  state.selectedUsers = [];
  state.selectedLeaders = [];
  state.saveAsNewTeam = false;
  state.newTeamName = '';
  ruleFormRef.value?.resetFields();
};

const toggleTimeDisplay = () => {
  state.showTimeOptions = !state.showTimeOptions;
};

// Patrol team change - auto fill users from team
const onPatrolTeamChange = async (teamId: string) => {
  if (!teamId) {
    return;
  }
  
  // TODO: Call API to get team members when available
  // For now, just filter from all users (mock logic)
  const teamUsers = state.allUsers.filter((_, index) => index < 5); // Mock: first 5 users
  
  state.selectedUsers = [...teamUsers];
  state.selectedUserIds = teamUsers.map(u => u.username);
};

// Handle user/leader selection from dropdown (now using username)
const handleUserChange = (usernames: string[]) => {
  state.selectedUsers = state.allUsers.filter(u => usernames.includes(u.username));
};

const handleLeaderChange = (usernames: string[]) => {
  state.selectedLeaders = state.allUsers.filter(u => usernames.includes(u.username));
};

// Handle visible change to clear filter text
const handleUserVisibleChange = (visible: boolean) => {
  if (!visible && userSelectRef.value) {
    // Clear filter when dropdown closes
    userSelectRef.value.blur();
  }
};

const handleLeaderVisibleChange = (visible: boolean) => {
  if (!visible && leaderSelectRef.value) {
    // Clear filter when dropdown closes
    leaderSelectRef.value.blur();
  }
};

// Handle save as new team checkbox
const handleSaveAsTeamChange = (checked: boolean) => {
  if (!checked) {
    state.newTeamName = '';
  } else {
    // Auto-generate team name based on schedule name
    if (state.ruleForm.name) {
      state.newTeamName = `${$t('lz.label.tmsDutySchedule.teamPrefix')} ${state.ruleForm.name}`;
    }
  }
};

// Team modal functions
const openTeamSelectModal = () => {
  teamQuickSelectRef.value?.openDialog();
};

const openTeamManageModal = () => {
  patrolTeamManageRef.value?.openDialog();
};

// Handle team selection from quick select
const handleTeamSelect = (team: any) => {
  
  // Get members from either members or memberList
  const members = team.members || team.memberList || [];
  
  if (!members || members.length === 0) {
    ElMessage.warning($t('lz.message.tmsDutySchedule.teamNoMembers'));
    return;
  }
  
  
  // Get user details from members (using username instead of id)
  const notFoundUserIds: string[] = [];
  const teamMembers = members.map((member: any) => {
    // Try to find user by username (userId now contains username)
    const user = state.allUsers.find(u => u.username === member.userId);
    if (!user) {
      notFoundUserIds.push(member.userId || 'unknown');
      console.warn('User not found in allUsers:', member.userId, 'member:', member);
    }
    return user ? {
      ...user,
      role: member.role // Keep the role from team member
    } : null;
  }).filter((u: any) => u !== null); // Filter out null users
  
  
  if (notFoundUserIds.length > 0) {
    ElMessage.warning($t('lz.message.tmsDutySchedule.usersNotFound', { count: notFoundUserIds.length }) + ` (username: ${notFoundUserIds.join(', ')})`);
  }
  
  if (teamMembers.length === 0) {
    ElMessage.error($t('lz.message.tmsDutySchedule.noUsersInSystem'));
    return;
  }
  
  // Separate leaders and members based on role
  const leaders = teamMembers.filter((u: any) => u.role === 'leader');
  const members_filtered = teamMembers.filter((u: any) => u.role === 'member');
  
  // Update state (use username as key)
  state.selectedLeaders = leaders;
  state.selectedLeaderIds = leaders.map((u: any) => u.username);
  state.selectedUsers = members_filtered;
  state.selectedUserIds = members_filtered.map((u: any) => u.username);
  
  ElMessage.success($t('lz.message.tmsDutySchedule.addedFromTeam', { 
    leaderCount: leaders.length, 
    memberCount: members_filtered.length, 
    teamName: team.name 
  }));
};

// Handle team edit from quick select
const handleTeamEditFromQuickSelect = (team: any) => {
  patrolTeamManageRef.value?.openDialog(team);
};

// Handle team add from quick select
const handleTeamAddFromQuickSelect = () => {
  patrolTeamManageRef.value?.openDialog();
};

// Handle refresh from team management
const handleTeamRefresh = async () => {
  await loadTeamList();
};

const submit = async () => {
  if (!ruleFormRef.value) return;
  
  await ruleFormRef.value.validate(async (valid: boolean) => {
    if (!valid) return;
    
    try {
      state.loading = true;
      
      // Build details array from selected users and leaders
      const details: DutyScheduleDetailInput[] = [];
      
      
      // Add leaders (use username as userId)
      state.selectedLeaders.forEach(leader => {
        details.push({
          userId: leader.username, // Use username instead of id
          role: 'leader',
          status: 'join', // Join status
          remark: null,
        });
      });
      
      // Add members (exclude leaders, use username as userId)
      state.selectedUsers.forEach(user => {
        // Only add if not already in leaders
        if (!state.selectedLeaders.find(l => l.username === user.username)) {
          details.push({
            userId: user.username, // Use username instead of id
            role: 'member',
            status: 'join', // Join status
            remark: null,
          });
        }
      });
      
      
      // Create new team if checkbox is checked
      let createdTeamId: string | undefined = undefined;
      if (state.saveAsNewTeam && state.newTeamName && details.length > 0) {
        try {
          
          // Prepare team members data
          const teamMembers = details
            .filter(detail => detail.userId) // Filter out null/undefined userIds
            .map(detail => ({
              userId: detail.userId!,
              role: detail.role || 'member',
              remark: null,
            }));
          
          
          // Create team with members in one API call
          await getAPI(TmsPatrolTeamApi).apiTmsTmspatrolteamAddPost({
            name: state.newTeamName,
            description: `${$t('lz.label.tmsDutySchedule.autoCreatedFromSchedule')} ${state.ruleForm.name}`,
            status: 1,
            members: teamMembers,
          });
          
          
          // Get the newly created team ID by fetching the team list
          const teamListResponse = await getAPI(TmsPatrolTeamApi).apiTmsTmspatrolteamListGet(
            state.newTeamName, undefined, 1
          );
          
          if (teamListResponse.data?.result && teamListResponse.data.result.length > 0) {
            // Find the team by exact name match
            const newTeam = teamListResponse.data.result.find((t: any) => t.name === state.newTeamName);
            if (newTeam?.id) {
              createdTeamId = newTeam.id;
              
              ElMessage.success($t('lz.message.tmsDutySchedule.createTeamSuccess', { 
                name: state.newTeamName, 
                count: teamMembers.length 
              }));
              
              // Reload team list
              await loadTeamList();
            } else {
              ElMessage.warning($t('lz.message.tmsDutySchedule.teamIdNotFound'));
            }
          } else {
            ElMessage.warning($t('lz.message.tmsDutySchedule.cannotGetTeamInfo'));
          }
        } catch (teamError: any) {
          console.error('Error creating team:', teamError);
          ElMessage.warning($t('lz.message.tmsDutySchedule.createTeamError', { 
            error: teamError?.message || $t('lz.message.tmsDutySchedule.unknownError') 
          }));
          // Continue with schedule creation even if team creation fails
        }
      }
      
      if (props.operateType === 'add') {
        // Add new schedule with details
        const params: AddDutyScheduleInput = {
          name: state.ruleForm.name,
          startTime: state.ruleForm.startTime,
          endTime: state.ruleForm.endTime,
          patrolTeamId: createdTeamId || state.selectedPatrolTeam || undefined, // Use newly created team if available
          status: state.ruleForm.status, // Use status from form
          remark: state.ruleForm.remark || undefined,
          details: details.length > 0 ? details : undefined,
        };
        
        await getAPI(TmsDutyScheduleApi).apiTmsTmsdutyscheduleAddPost(params);
        ElMessage.success($t('lz.message.base.createSuccess'));
      } else {
        // Update existing schedule
        // In limited edit mode, only update name, remark, and status
        const params: UpdateDutyScheduleInput = {
          id: state.ruleForm.id,
          name: state.ruleForm.name,
          remark: state.ruleForm.remark || undefined,
          status: state.ruleForm.status,
          // Only include these fields if NOT in limited edit mode
          ...(state.isLimitedEdit ? {} : {
            startTime: state.ruleForm.startTime,
            endTime: state.ruleForm.endTime,
            patrolTeamId: state.selectedPatrolTeam || undefined,
            details: details.length > 0 ? details : undefined,
          }),
        };
        
        await getAPI(TmsDutyScheduleApi).apiTmsTmsdutyscheduleUpdatePost(params);
        ElMessage.success($t('lz.message.base.updateSuccess'));
      }
      
      state.isShowDialog = false;
      emits('handleQuery');
    } catch (error: any) {
      console.error('Error submitting schedule:', error);
      ElMessage.error(error?.message || $t('lz.message.tmsDutySchedule.saveScheduleError'));
    } finally {
      state.loading = false;
    }
  });
};

defineExpose({ openDialog });
</script>

<style scoped lang="scss">
.tms-duty-schedule-modal-container {
  
  :deep(.el-dialog__body) {
    padding: 20px;
    max-height: 70vh;
    overflow-y: auto;
    overflow-x: hidden;
  }
  
  // Fix z-index for dialog
  :deep(.el-dialog) {
    z-index: 2000;
  }
  
  :deep(.el-dialog__footer) {
    padding: 15px 20px;
    border-top: 1px solid var(--el-border-color-lighter);
  }
  
  // PrimeVue Fieldset styling
  :deep(.p-fieldset) {
    margin-bottom: 0;
    border: 1px solid var(--el-border-color-light);
    border-radius: 6px;
    background: var(--el-bg-color);
    overflow: visible !important;
    position: relative;
    z-index: auto;
    
    .p-fieldset-legend {
      background: var(--el-color-primary-light-9);
      border: 1px solid var(--el-color-primary-light-7);
      border-radius: 4px;
      padding: 6px 14px;
      font-weight: 600;
      color: var(--el-color-primary);
      font-size: 13px;
    }
    
    .p-fieldset-content {
      padding: 15px;
      overflow: visible !important;
      position: relative;
      z-index: auto;
    }
  }
  
  // Ensure rows don't block dropdown
  :deep(.el-row) {
    position: relative;
    z-index: auto;
  }
  
  :deep(.el-col) {
    position: relative;
    z-index: auto;
  }
  
  // Standard spacing like other modals
  .mb20 {
    margin-bottom: 20px;
  }
  
  :deep(.el-form-item) {
    margin-bottom: 0;
  }
  
  :deep(.el-form-item__label) {
    font-weight: 500;
    color: var(--el-text-color-regular);
  }
  
  :deep(.el-form-item__content) {
    position: relative;
    z-index: auto;
  }
  
  // Quick actions bar
  .quick-actions-bar {
    display: flex;
    align-items: center;
    padding-bottom: 15px;
    border-bottom: 1px solid var(--el-border-color-lighter);
  }
}

.dialog-footer {
  display: flex;
  justify-content: flex-end;
}
</style>
