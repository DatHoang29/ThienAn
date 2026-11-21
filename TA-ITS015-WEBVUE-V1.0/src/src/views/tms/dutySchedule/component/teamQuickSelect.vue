<template>
  <div class="team-quick-select-container">
    <el-dialog 
      v-model="state.isShowDialog" 
      draggable
      width="900px"
      :close-on-click-modal="false"
    >
      <template #header>
        <div style="color: #fff">
          <el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <UserFilled />
          </el-icon>
          <span> {{ $t('lz.label.tmsDutySchedule.selectTeamQuickAdd') }} </span>
        </div>
      </template>
      <div style="display: flex; gap: 15px; margin-bottom: 15px;">
        <el-input 
          v-model="state.searchText" 
          :placeholder="$t('lz.label.tmsDutySchedule.searchTeam')" 
          clearable
          style="flex: 1;"
        >
          <template #prefix>
            <el-icon><Search /></el-icon>
          </template>
        </el-input>
        
        <el-button :icon="Refresh" @click="loadTeamList" :loading="state.loading">
          {{ $t('lz.button.base.refresh') }}
        </el-button>
        <el-button type="primary" :icon="Plus" @click="handleAddTeam">
          {{ $t('lz.label.tmsDutySchedule.addNewTeam') }}
        </el-button>
      </div>
      
      <div class="team-list-container pt10" v-loading="state.loading">
        <el-empty v-if="filteredTeamList.length === 0" :description="$t('lz.label.tmsDutySchedule.noTeamFound')" />
        <div 
          v-for="team in filteredTeamList" 
          :key="team.id"
          class="team-item"
          :class="{ 'active': state.selectedTeamId === team.id }"
          @click="state.selectedTeamId = team.id"
        >
          <div class="team-item-content">
            <el-icon v-if="state.selectedTeamId === team.id" class="team-icon" :size="20"><i class="fa fa-dot-circle-o"></i></el-icon>
            <el-icon v-else class="team-icon" :size="20"><i class="fa fa-circle-o"></i> </el-icon>
            <div class="team-info">
              <div class="team-header">
                <div class="team-name">{{ team.name }}</div>
                <el-button 
                  type="primary" 
                  link 
                  :icon="Edit" 
                  size="small"
                  @click.stop="handleEditTeam(team)"
                >
                  {{ $t('lz.button.base.edit') }}
                </el-button>
              </div>
              
              <!-- Member list -->
              <div class="team-members" v-if="team.memberList && team.memberList.length > 0">
                <el-tag 
                  v-for="(member, index) in team.memberList" 
                  :key="index"
                  :type="getRoleConfig(member.role)?.tagType || 'info'"
                  :style="getRoleConfig(member.role)?.color ? { backgroundColor: getRoleConfig(member.role).color, borderColor: getRoleConfig(member.role).color, color: '#fff' } : undefined"
                  size="small"
                  effect="plain"
                  style="margin-right: 5px; margin-bottom: 5px;"
                >
                  <el-icon v-if="isPrimaryRole(member.role)" style="margin-right: 3px;"><StarFilled /></el-icon>
                  <el-tooltip :content="`Username: ${member.userId}`" placement="top">
                    <span style="cursor: help;">{{ getUserRealName(member.userId) }}</span>
                  </el-tooltip>
                </el-tag>
              </div>
              
              <div class="team-meta">
                <span>{{ $t('lz.label.tmsDutySchedule.memberCount', { count: team.memberCount || 0 }) }}</span>
                <el-tag v-if="team.description" size="small" type="info" style="margin-left: 8px;">
                  {{ team.description }}
                </el-tag>
              </div>
            </div>
          </div>
        </div>
      </div>
      
      <template #footer>
        <el-button @click="state.isShowDialog = false">{{ $t('lz.button.base.cancel') }}</el-button>
        <el-button 
          type="primary" 
          @click="confirmSelection" 
          :disabled="!state.selectedTeamId"
          :loading="state.applying"
        >
          {{ $t('lz.button.base.confirm') }}
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script lang="ts" setup>
import { reactive, computed, onMounted } from 'vue';
import { ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { Search, UserFilled, Edit, Plus, StarFilled, Refresh } from '@element-plus/icons-vue';
import { getAPI } from '/@/utils/axios-utils';
import { TmsPatrolTeamApi } from '/@/api-services/tms';
import { SysUserApi } from '/@/api-services/system';
import { useBaseConfig } from '/@/stores/baseConfig';
import { BaseConfigTypeEnum } from '/@/types/enums';

const { t: $t } = useI18n();
const emits = defineEmits(['select', 'edit', 'add']);

const { getConfigDataByCode, getConfigItemByCode } = useBaseConfig();

// Check if role is the primary role (leader)
const isPrimaryRole = (roleCode: string | undefined) => {
  if (!roleCode) return false;
  return roleCode?.toLowerCase() === 'leader';
};

// Get role config by code
const getRoleConfig = (roleCode: string | undefined) => {
  if (!roleCode) return null;
  return getConfigItemByCode(BaseConfigTypeEnum.TmsDutyPatronRole, roleCode.toLowerCase());
};

const state = reactive({
  isShowDialog: false,
  loading: false,
  applying: false,
  searchText: '',
  teamList: [] as any[],
  selectedTeamId: undefined as string | undefined,
  allUsers: [] as any[],
});

// Computed filtered list
const filteredTeamList = computed(() => {
  if (!state.searchText) return state.teamList;
  const search = state.searchText.toLowerCase();
  return state.teamList.filter(t => 
    t.name?.toLowerCase().includes(search) || 
    t.description?.toLowerCase().includes(search)
  );
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

// Get user real name by username
const getUserRealName = (username: string) => {
  const user = state.allUsers.find((u: any) => u.username === username);
  return user?.realName || user?.name || username;
};

onMounted(() => {
  loadAllUsers();
});

// Load team list
const loadTeamList = async () => {
  state.loading = true;
  try {
    const response = await getAPI(TmsPatrolTeamApi).apiTmsTmspatrolteamListGet(
      undefined, undefined, 1 // Only active teams
    );
    if (response.data?.result) {
      // Process teams with member count and member list
      state.teamList = response.data.result.map(team => ({
        ...team,
        memberCount: team.members?.length || 0,
        memberList: team.members || []
      }));
    }
  } catch (error) {
    console.error('Error loading team list:', error);
    ElMessage.error($t('lz.message.tmsDutySchedule.loadTeamListError'));
  } finally {
    state.loading = false;
  }
};

// Open dialog
const openDialog = async () => {
  state.isShowDialog = true;
  state.selectedTeamId = undefined;
  state.searchText = '';
  await loadTeamList();
};

// Confirm selection
const confirmSelection = async () => {
  if (!state.selectedTeamId) return;
  
  state.applying = true;
  try {
    const selectedTeam = state.teamList.find(t => t.id === state.selectedTeamId);
    
    if (selectedTeam) {
      // Emit selected team with members from API
      // Members already loaded in loadTeamList() and stored in memberList
      emits('select', {
        ...selectedTeam,
        members: selectedTeam.memberList || []
      });
      state.isShowDialog = false;
      // ElMessage.success($t('lz.message.tmsDutySchedule.addedMembersFromTeam'));
    }
  } catch (error) {
    console.error('Error applying team selection:', error);
    ElMessage.error($t('lz.message.tmsDutySchedule.applyTeamError'));
  } finally {
    state.applying = false;
  }
};

// Handle edit team
const handleEditTeam = (team: any) => {
  emits('edit', team);
  state.isShowDialog = false;
};

// Handle add team
const handleAddTeam = () => {
  emits('add');
  state.isShowDialog = false;
};

defineExpose({ openDialog });
</script>

<style scoped lang="scss">
.team-quick-select-container {
  .team-list-container {
    max-height: 500px;
    overflow-y: auto;
    margin-top: 20px;
    .team-item {
      display: flex;
      align-items: flex-start;
      justify-content: space-between;
      padding: 15px;
      border: 1px solid var(--el-border-color-light);
      border-radius: 8px;
      margin-bottom: 12px;
      cursor: pointer;
      transition: all 0.3s;
      
      &:hover {
        border-color: var(--el-color-primary);
        background: var(--el-color-primary-light-9);
        transform: translateY(-2px);
        box-shadow: 0 2px 12px rgba(0, 0, 0, 0.1);
      }
      
      &.active {
        border-color: var(--el-color-primary);
        background: var(--el-color-primary-light-9);
        box-shadow: 0 2px 12px rgba(64, 158, 255, 0.3);
      }
      
      .team-item-content {
        display: flex;
        align-items: flex-start;
        gap: 12px;
        flex: 1;
        min-width: 0;
        
        .team-icon {
          color: var(--el-color-primary);
          background: var(--el-color-primary-light-9);
          padding: 10px;
          border-radius: 50%;
          flex-shrink: 0;
          margin-top: 2px;
        }
        
        .team-info {
          flex: 1;
          min-width: 0;
          
          .team-header {
            display: flex;
            align-items: center;
            justify-content: space-between;
            margin-bottom: 8px;
            
            .team-name {
              font-weight: 600;
              font-size: 15px;
              color: var(--el-text-color-primary);
            }
          }
          
          .team-members {
            padding: 8px 0;
            display: flex;
            flex-wrap: wrap;
            gap: 5px;
          }
          
          .team-meta {
            font-size: 12px;
            color: var(--el-text-color-secondary);
            display: flex;
            align-items: center;
            flex-wrap: wrap;
            gap: 4px;
          }
        }
      }
    }
  }
}
</style>
