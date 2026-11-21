<template>
  <div class="patrol-team-manage-container">
    <!-- Team Management Modal -->
    <el-dialog 
      v-model="state.isShowDialog" 
      width="1200px"
      :close-on-click-modal="false"
      draggable
    >
      <template #header>
        <div style="color: #fff">
          <el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <UserFilled />
          </el-icon>
          <span> {{ $t('lz.label.tmsDutySchedule.patrolTeamManagement') }} </span>
        </div>
      </template>
      <div class="team-manage-toolbar">
        <el-input 
          v-model="state.searchText" 
          :placeholder="$t('lz.label.tmsDutySchedule.searchTeamPlaceholder')" 
          clearable
          style="width: 300px;"
        >
          <template #prefix>
            <el-icon><Search /></el-icon>
          </template>
        </el-input>
        <div style="display: flex; gap: 10px;">
          <el-button :icon="Refresh" @click="loadTeamList" :loading="state.loading">
            {{ $t('lz.button.base.refresh') }}
          </el-button>
          <el-button type="primary" :icon="Plus" @click="openTeamDialog()">{{ $t('lz.label.tmsDutySchedule.addNewTeam') }}</el-button>
        </div>
      </div>
      
      <el-table 
        :data="filteredTeamList" 
        stripe 
        style="width: 100%; margin-top: 15px;"
        v-loading="state.loading"
      >
        <el-table-column type="index" :label="$t('lz.label.base.idx')" width="60" align="center" />
        <el-table-column prop="name" :label="$t('lz.label.tmsDutySchedule.teamName')" min-width="150" />
        <el-table-column :label="$t('lz.label.tmsDutySchedule.teamMembers')" min-width="350">
          <template #default="{ row }">
            <div v-if="row.memberList && row.memberList.length > 0" class="member-tags-container">
              <el-tag 
                v-for="(member, idx) in row.memberList" 
                :key="idx"
                :type="getRoleConfig(member.role)?.tagType || 'info'"
                :style="getRoleConfig(member.role)?.color ? { backgroundColor: getRoleConfig(member.role).color, borderColor: getRoleConfig(member.role).color, color: '#fff' } : undefined"
                size="small"
                effect="plain"
                style="margin: 2px;"
              >
                <el-icon v-if="isPrimaryRole(member.role)" style="margin-right: 3px;" size="12"><StarFilled /></el-icon>
                <el-icon v-else style="margin-right: 3px;" size="12"><User /></el-icon>
                {{ member.userName || member.userId }}
              </el-tag>
            </div>
            <span v-else style="color: #999; font-size: 12px;">{{ $t('lz.label.tmsDutySchedule.noMembers') }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="description" :label="$t('lz.entity.base.desc')" min-width="180" show-overflow-tooltip />
        <el-table-column :label="$t('lz.entity.base.status')" width="120" align="center">
          <template #default="{ row }">
            <el-tag :type="row.status === 1 || row.status === '1' ? 'success' : 'danger'">
              {{ row.status === 1 || row.status === '1' ? $t('lz.label.base.active') : $t('lz.label.tmsDutySchedule.statusInactiveTeam') }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column :label="$t('lz.label.tmsDutySchedule.operations')" width="230" align="center" fixed="right">
          <template #default="{ row }">
            <div style="display: flex; justify-content: center; gap: 8px;">
              <el-button size="small" type="primary" :icon="Edit" @click="openTeamDialog(row)">{{ $t('lz.button.base.edit') }}</el-button>
              <el-button size="small" type="danger" :icon="Delete" @click="deleteTeam(row)">{{ $t('lz.button.base.delete') }}</el-button>
            </div>
          </template>
        </el-table-column>
      </el-table>
    </el-dialog>
    
    <!-- Team Edit Dialog -->
    <el-dialog 
      v-model="state.showTeamEditDialog" 
      width="700px"
      :close-on-click-modal="false"
      draggable
    >
      <template #header>
        <div style="color: #fff">
          <el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <Edit />
          </el-icon>
          <span> {{ state.editMode === 'add' ? $t('lz.label.tmsDutySchedule.addPatrolTeam') : $t('lz.label.tmsDutySchedule.editPatrolTeam') }} </span>
        </div>
      </template>
      <div style="max-height: 600px; overflow-y: auto; padding-right: 10px;">
        <el-form 
          :model="state.teamForm" 
          ref="teamFormRef" 
          label-width="150px"
          label-position="left"
          :rules="teamFormRules"
        >
          <el-row :gutter="10">
          <el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb15">
            <el-form-item :label="$t('lz.label.tmsDutySchedule.teamName')" prop="name">
              <el-input 
                v-model="state.teamForm.name" 
                :placeholder="$t('lz.label.tmsDutySchedule.teamNamePlaceholder')" 
                clearable
              />
            </el-form-item>
          </el-col>
          
          <el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb15">
            <el-form-item :label="$t('lz.entity.base.desc')" prop="description">
              <el-input 
                v-model="state.teamForm.description" 
                type="textarea"
                :rows="2"
                :placeholder="$t('lz.label.tmsDutySchedule.descriptionPlaceholder')" 
                clearable
              />
            </el-form-item>
          </el-col>
          
          <el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb15">
            <el-form-item :label="$t('lz.entity.base.status')" prop="status">
              <el-radio-group v-model="state.teamForm.status">
                <el-radio label="1">{{ $t('lz.label.base.active') }}</el-radio>
                <el-radio label="0">{{ $t('lz.label.tmsDutySchedule.statusInactive') }}</el-radio>
              </el-radio-group>
            </el-form-item>
          </el-col>
          
          <el-divider  content-position="center" style="margin: 15px 0;">
            <div style="color: #b1b3b8; font-size: 13px;">{{ $t('lz.label.tmsDutySchedule.memberList') }}</div>
          </el-divider>
          
          <!-- Member list section (available in both add and edit mode) -->
          <el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb10">
            <el-button icon="ele-Plus" type="primary" plain size="small" @click="addMemberRow">
              {{ $t('lz.label.tmsDutySchedule.addMember') }}
            </el-button>
            <span style="font-size: 11px; color: gray; padding-left: 5px;">
              {{ $t('lz.label.tmsDutySchedule.memberSelectionHint') }}
            </span>
          </el-col>
          
          <el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb10">
            <div v-if="state.teamForm.memberList && state.teamForm.memberList.length > 0">
              <el-row :gutter="10" v-for="(member, k) in state.teamForm.memberList" :key="k" style="margin-bottom: 8px;">
                <el-col :xs="24" :sm="14" :md="14" :lg="14" :xl="14">
                  <el-form-item 
                    :prop="`memberList[${k}].userId`" 
                    :rules="[{ required: true, message: $t('lz.message.tmsDutySchedule.pleaseSelectUser'), trigger: 'change' }]"
                    style="margin-bottom: 0;"
                  >
                    <template #label>
                      <el-button 
                        icon="ele-Delete" 
                        type="danger" 
                        circle 
                        plain 
                        size="small" 
                        @click="deleteMemberRow(k)"
                        style="padding: 4px;"
                      />
                      <span style="margin-left: 5px; font-size: 13px;">{{ $t('lz.label.tmsDutySchedule.user') }}</span>
                    </template>
                    <el-select 
                      v-model="state.teamForm.memberList[k].userId" 
                      :placeholder="$t('lz.label.tmsDutySchedule.selectUser')" 
                      filterable
                      clearable
                      size="small"
                    >
                      <el-option 
                        v-for="user in state.userList" 
                        :key="user.username" 
                        :label="`${user.realName || user.name} (${user.name})`" 
                        :value="user.username"
                      />
                    </el-select>
                  </el-form-item>
                </el-col>
                
                <el-col :xs="24" :sm="10" :md="10" :lg="10" :xl="10">
                  <el-form-item 
                    :label="$t('lz.label.tmsDutySchedule.role')" 
                    :prop="`memberList[${k}].role`" 
                    :rules="[{ required: true, message: $t('lz.label.tmsDutySchedule.selectRole'), trigger: 'change' }]"
                    style="margin-bottom: 0;"
                    label-width="60px"
                  >
                    <el-select 
                      v-model="state.teamForm.memberList[k].role" 
                      :placeholder="$t('lz.label.tmsDutySchedule.selectRole')"
                      size="small"
                      class="w100"
                    >
                      <el-option 
                        v-for="roleConfig in roleConfigs" 
                        :key="roleConfig.code" 
                        :label="roleConfig.name" 
                        :value="roleConfig.code" 
                      />
                    </el-select>
                  </el-form-item>
                </el-col>
              </el-row>
            </div>
            <el-empty 
              v-else
              :image-size="60" 
              :description="$t('lz.label.tmsDutySchedule.noMembersYet')"
            />
          </el-col>
        </el-row>
        </el-form>
      </div>
      
      <template #footer>
        <el-button @click="state.showTeamEditDialog = false">{{ $t('lz.button.base.cancel') }}</el-button>
        <el-button type="primary" @click="submitTeam" :loading="state.submitting">
          {{ state.editMode === 'add' ? $t('lz.label.tmsDutySchedule.add') : $t('lz.label.tmsDutySchedule.update') }}
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script lang="ts" setup>
import { reactive, ref, computed, onMounted } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { Search, Plus, Edit, Delete, Refresh, StarFilled, User } from '@element-plus/icons-vue';
import { getAPI } from '/@/utils/axios-utils';
import { TmsPatrolTeamApi } from '/@/api-services/tms';
import { useBaseConfig } from '/@/stores/baseConfig';
import { BaseConfigTypeEnum } from '/@/types/enums';

const { t: $t } = useI18n();
const { getConfigDataByCode, getConfigItemByCode } = useBaseConfig();

// Get role configs for dropdown
const roleConfigs = computed(() => getConfigDataByCode(BaseConfigTypeEnum.TmsDutyPatronRole) || []);

// Check if role is the primary role (leader)
const isPrimaryRole = (roleCode: string | null | undefined) => {
  if (!roleCode) return false;
  return roleCode?.toLowerCase() === 'leader';
};

// Get role config by code
const getRoleConfig = (roleCode: string | null | undefined) => {
  if (!roleCode) return null;
  return getConfigItemByCode(BaseConfigTypeEnum.TmsDutyPatronRole, roleCode.toLowerCase());
};

import { SysUserApi } from '/@/api-services/system';
import type { 
  AddPatrolTeamInput, 
  UpdatePatrolTeamInput, 
  PatrolTeamMemberOutput,
  AddMemberToTeamInput,
  RemoveMemberFromTeamInput,
  UpdateMemberRoleInput
} from '/@/api-services/tms';
import type { UserOutput } from '/@/api-services/system';
import { lo } from 'element-plus/es/locale';

const emits = defineEmits(['refresh']);

const teamFormRef = ref();

// Define member interface (matching API structure)
interface TeamMember {
  id?: string; // Member ID (for edit/delete)
  userId?: string;
  userName?: string;
  role?: string; // Changed from position to role to match API
}

const state = reactive({
  isShowDialog: false,
  loading: false,
  submitting: false,
  searchText: '',
  teamList: [] as any[],
  userList: [] as UserOutput[],
  
  // Team edit dialog
  showTeamEditDialog: false,
  editMode: 'add' as 'add' | 'edit',
  teamForm: {
    id: undefined as string | undefined,
    name: '',
    description: '',
    status: '1',
    memberList: [] as TeamMember[],
    originalMemberIds: [] as string[], // Track original member IDs for comparison
  },
});

// Load user list on component mount
onMounted(async () => {
  await loadUserList();
});


// Load user list
const loadUserList = async () => {
  try {
    const response = await getAPI(SysUserApi).apiSystemSysuserListPost({});
    if (response.data?.result) {
      state.userList = response.data.result;
    }
  } catch (error) {
    console.error('Error loading user list:', error);
  }
};



const teamFormRules = computed(() => ({
  name: [
    { required: true, message: $t('lz.message.tmsDutySchedule.pleaseInputTeamName'), trigger: 'blur' },
    { min: 2, max: 100, message: $t('lz.message.tmsDutySchedule.teamNameLength'), trigger: 'blur' },
  ],
}));

// Computed filtered list
const filteredTeamList = computed(() => {
  if (!state.searchText) return state.teamList;
  const search = state.searchText.toLowerCase();
  return state.teamList.filter(t => 
    t.name?.toLowerCase().includes(search) || 
    t.description?.toLowerCase().includes(search)
  );
});

// Load team list with members
const loadTeamList = async () => {
  state.loading = true;
  try {
    const response = await getAPI(TmsPatrolTeamApi).apiTmsTmspatrolteamListGet(
      undefined, undefined, undefined
    );
    if (response.data?.result) {
      // Members are now included in the team response
      state.teamList = response.data.result.map((team: any) => {
        const memberList: TeamMember[] = [];
        
        // Get members from API response (members property in PatrolTeamOutput)
        if (team.members && Array.isArray(team.members)) {
          team.members.forEach((member: PatrolTeamMemberOutput) => {
            // Find user info from userList
            const user = state.userList.find(u => u.id === member.userId);
            memberList.push({
              id: member.id || undefined,
              userId: member.userId || undefined,
              userName: user ? (user.realName || user.name || '') : (member.userId || ''),
              role: member.role || 'member',
            });
          });
        }
        
        return {
          ...team,
          memberList,
          memberCount: memberList.length,
        };
      });
    }
  } catch (error) {
    console.error('Error loading team list:', error);
    ElMessage.error($t('lz.message.tmsDutySchedule.loadTeamListError'));
  } finally {
    state.loading = false;
  }
};

// Open main dialog
const openDialog = async () => {
  state.isShowDialog = true;
  state.searchText = '';
  await loadTeamList();
};

// Open team edit dialog
const openTeamDialog = (team?: any) => {
  state.showTeamEditDialog = true;
  
  if (team) {
    // Edit mode
    state.editMode = 'edit';
    state.teamForm = {
      id: team.id,
      name: team.name || '',
      description: team.description || '',
      status: String(team.status ?? 1), // Convert number to string for radio button
      memberList: team.memberList ? JSON.parse(JSON.stringify(team.memberList)) : [],
      originalMemberIds: team.memberList ? team.memberList.map((m: TeamMember) => m.id).filter(Boolean) : [],
    };
  } else {
    // Add mode
    state.editMode = 'add';
    state.teamForm = {
      id: undefined,
      name: '',
      description: '',
      status: '1',
      memberList: [],
      originalMemberIds: [],
    };
  }
  
  // Clear validation
  setTimeout(() => {
    teamFormRef.value?.clearValidate();
  }, 100);
};

// Add member row
const addMemberRow = () => {
  if (!state.teamForm.memberList) state.teamForm.memberList = [];
  state.teamForm.memberList.push({
    id: undefined,
    userId: undefined,
    userName: undefined,
    role: 'member', // Default to member
  });
};

// Delete member row
const deleteMemberRow = (index: number) => {
  state.teamForm.memberList.splice(index, 1);
};

// Submit team (add or update)
const submitTeam = async () => {
  if (!teamFormRef.value) return;
  
  await teamFormRef.value.validate(async (valid: boolean) => {
    if (!valid) return;
    
    state.submitting = true;
    try {
      if (state.editMode === 'add') {
        // Add new team with members in one API call
        const memberList = state.teamForm.memberList
          .filter(m => m.userId) // Filter out empty members
          .map(m => ({
            userId: m.userId!,
            role: m.role || 'member',
            remark: null,
          }));
        
        const params: AddPatrolTeamInput = {
          name: state.teamForm.name,
          description: state.teamForm.description,
          status: parseInt(state.teamForm.status) as any, // Convert string to number
          members: memberList.length > 0 ? memberList : undefined,
        };
        
        await getAPI(TmsPatrolTeamApi).apiTmsTmspatrolteamAddPost(params);
        ElMessage.success(memberList.length > 0 
          ? $t('lz.message.tmsDutySchedule.addTeamWithMembersSuccess', { count: memberList.length })
          : $t('lz.message.tmsDutySchedule.addTeamSuccess')
        );
      } else {
        // Update existing team
        const teamId = state.teamForm.id!;
        
        const params: UpdatePatrolTeamInput = {
          id: teamId,
          name: state.teamForm.name,
          description: state.teamForm.description,
          status: parseInt(state.teamForm.status) as any, // Convert string to number
        };
        
        await getAPI(TmsPatrolTeamApi).apiTmsTmspatrolteamUpdatePost(params);
        
        // Now handle members separately using the new APIs
        // Get current member IDs in the form
        const currentMemberIds = state.teamForm.memberList
          .map(m => m.id)
          .filter(Boolean) as string[];
        
        // Find members to delete (in original but not in current)
        const membersToDelete = state.teamForm.originalMemberIds.filter(
          id => !currentMemberIds.includes(id)
        );
        
        // Delete removed members
        for (const memberId of membersToDelete) {
          const removeParams: RemoveMemberFromTeamInput = {
            memberId: memberId,
          };
          await getAPI(TmsPatrolTeamApi).apiTmsTmspatrolteamRemovememberPost(removeParams);
        }
        
        // Add or update members
        for (const member of state.teamForm.memberList) {
          if (member.userId) {
            if (member.id) {
              // Update existing member role
              const updateParams: UpdateMemberRoleInput = {
                memberId: member.id,
                role: member.role || 'member',
              };
              await getAPI(TmsPatrolTeamApi).apiTmsTmspatrolteamUpdatememberrolePost(updateParams);
            } else {
              // Add new member
              const addParams: AddMemberToTeamInput = {
                patrolTeamId: teamId,
                userId: member.userId,
                role: member.role || 'member',
              };
              await getAPI(TmsPatrolTeamApi).apiTmsTmspatrolteamAddmemberPost(addParams);
            }
          }
        }
        
        ElMessage.success($t('lz.message.tmsDutySchedule.updateTeamSuccess'));
      }
      
      state.showTeamEditDialog = false;
      await loadTeamList();
      emits('refresh');
    } catch (error: any) {
      console.error('Error submitting team:', error);
      ElMessage.error(error?.message || $t('lz.message.tmsDutySchedule.saveTeamError'));
    } finally {
      state.submitting = false;
    }
  });
};

// Delete team
const deleteTeam = async (team: any) => {
  try {
    await ElMessageBox.confirm(
      $t('lz.message.tmsDutySchedule.confirmDeleteTeam', { name: team.name }),
      $t('lz.message.tmsDutySchedule.confirmDelete'),
      {
        confirmButtonText: $t('lz.button.base.delete'),
        cancelButtonText: $t('lz.button.base.cancel'),
        type: 'warning',
      }
    );
    
    state.loading = true;
    await getAPI(TmsPatrolTeamApi).apiTmsTmspatrolteamDeletePost({ id: team.id });
    ElMessage.success($t('lz.message.tmsDutySchedule.deleteTeamSuccess'));
    await loadTeamList();
    emits('refresh');
  } catch (error: any) {
    if (error !== 'cancel') {
      console.error('Error deleting team:', error);
      ElMessage.error(error?.message || $t('lz.message.tmsDutySchedule.deleteTeamError'));
    }
  } finally {
    state.loading = false;
  }
};

defineExpose({ openDialog });
</script>

<style scoped lang="scss">
.patrol-team-manage-container {
  .team-manage-toolbar {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 15px;
  }
  
  :deep(.el-table) {
    .el-button + .el-button {
      margin-left: 8px;
    }
  }
  
  // Member tags container for table cells
  .member-tags-container {
    display: flex;
    flex-wrap: wrap;
    gap: 4px;
    padding: 4px 0;
    max-height: 80px;
    overflow-y: auto;
  }
  
  // Custom scrollbar for dialog content
  :deep(.el-dialog__body) {
    padding: 15px 20px;
    
    // Webkit browsers (Chrome, Safari, Edge)
    & > div::-webkit-scrollbar {
      width: 6px;
    }
    
    & > div::-webkit-scrollbar-track {
      background: #f1f1f1;
      border-radius: 3px;
    }
    
    & > div::-webkit-scrollbar-thumb {
      background: #888;
      border-radius: 3px;
      
      &:hover {
        background: #555;
      }
    }
  }
  
  // Reduce spacing for compact layout
  :deep(.el-form-item) {
    &.mb15 {
      margin-bottom: 15px;
    }
    
    &.mb10 {
      margin-bottom: 10px;
    }
  }
}
</style>
