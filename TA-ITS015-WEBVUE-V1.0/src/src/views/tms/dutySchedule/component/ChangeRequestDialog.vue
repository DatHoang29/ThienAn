<template>
  <el-dialog
    v-model="state.visible"
    :title="state.dialogTitle"
    width="650px"
    draggable
    :close-on-click-modal="false"
    @close="handleClose"
  >
    <template #header>
      <div style="color: #fff; display: flex; align-items: center;">
        <el-icon size="16" style="margin-right: 8px;">
          <ele-EditPen />
        </el-icon>
        <span>{{ state.dialogTitle }}</span>
      </div>
    </template>

    <el-form
      ref="formRef"
      :model="state.formData"
      :rules="rules"
      label-width="160px"
      label-position="left"
      :scroll-to-error="true"
    >
      <el-form-item :label="$t('lz.label.tmsDutySchedule.currentSchedule')" class="schedule-form-item">
        <div class="schedule-card">
          <div class="schedule-card-header">
            <div class="schedule-name">
              <el-icon class="name-icon"><ele-Calendar /></el-icon>
              <span class="name-text">{{ props.schedule?.name || $t('lz.label.tmsDutySchedule.noName') }}</span>
            </div>
            <TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.TmsDutyState, props.schedule?.status ?? '')" />
          </div>
          <div class="schedule-card-body">
            <div class="schedule-info-item compact-time">
              <el-icon class="item-icon"><ele-Clock /></el-icon>
              <div class="item-content">
                <span class="item-value">
                  {{ props.schedule?.startTime ? useDateFormat(props.schedule.startTime, DateTimeFormatEnum.DateTimeDefault).value : '' }}
                  <el-icon size="12" style="margin: 0 6px;"><ele-Right /></el-icon>
                  {{ props.schedule?.endTime ? useDateFormat(props.schedule.endTime, DateTimeFormatEnum.DateTimeDefault).value : '' }}
                </span>
              </div>
            </div>
            <div v-if="props.schedule?.details?.length" class="schedule-info-item members-section">
              <el-icon class="item-icon"><ele-User /></el-icon>
              <div class="item-content">
                <span class="item-label">{{ $t('lz.label.tmsDutySchedule.personnelList') }} ({{ props.schedule.details.length }} {{ $t('lz.label.tmsDutySchedule.person') }}):</span>
                <div class="members-tags">
                  <el-tag
                    v-for="(member, index) in props.schedule.details"
                    :key="index"
                    :type="getRoleConfig(member.role)?.tagType || 'info'"
                    :style="getRoleConfig(member.role)?.color ? { backgroundColor: getRoleConfig(member.role).color, borderColor: getRoleConfig(member.role).color, color: '#fff' } : undefined"
                    size="small"
                    effect="plain"
                    style="margin: 2px 4px 2px 0;"
                  >
                    <el-icon v-if="isPrimaryRole(member.role)" style="margin-right: 3px;"><ele-StarFilled /></el-icon>
                    {{ member.userId }}
                  </el-tag>
                </div>
              </div>
            </div>
          </div>
        </div>
      </el-form-item>

      <el-form-item :label="$t('lz.label.tmsDutySchedule.requestType')" prop="type">
        <el-select
          v-model="state.formData.type"
          :placeholder="$t('lz.label.tmsDutySchedule.selectRequestType')"
          style="width: 100%;"
          @change="handleTypeChange"
        >
          <el-option :label="$t('lz.label.tmsDutySchedule.requestAbsentLabel')" value="Absent">
            <div style="display: flex; align-items: center; gap: 8px;">
              <el-icon color="#E6A23C"><ele-CircleClose /></el-icon>
              <span>{{ $t('lz.label.tmsDutySchedule.requestAbsentLabel') }}</span>
            </div>
          </el-option>
          <el-option :label="$t('lz.label.tmsDutySchedule.swapWithOther')" value="Swap">
            <div style="display: flex; align-items: center; gap: 8px;">
              <el-icon color="#409EFF"><ele-Refresh /></el-icon>
              <span>{{ $t('lz.label.tmsDutySchedule.swapWithOther') }}</span>
            </div>
          </el-option>
          <el-option v-if="canUseReplaceFeature" :label="$t('lz.label.tmsDutySchedule.replaceInSchedule')" value="Replace">
            <div style="display: flex; align-items: center; gap: 8px;">
              <el-icon color="#67C23A"><ele-RefreshRight /></el-icon>
              <span>{{ $t('lz.label.tmsDutySchedule.replaceInSchedule') }}</span>
            </div>
          </el-option>
          <el-option :label="$t('lz.label.tmsDutySchedule.transferToOther')" value="Transfer">
            <div style="display: flex; align-items: center; gap: 8px;">
              <el-icon color="#409EFF"><ele-Sort /></el-icon>
              <span>{{ $t('lz.label.tmsDutySchedule.transferToOther') }}</span>
            </div>
          </el-option>
        </el-select>
      </el-form-item>

      <!-- Common: Chọn người liên quan (hiển thị cho tất cả loại yêu cầu) -->
      <el-form-item
        :label="getSelectedPersonLabel"
        prop="targetUserId"
      >
        <el-select
          v-model="state.formData.targetUserId"
          :placeholder="getSelectedPersonPlaceholder"
          style="width: 100%;"
          filterable
          clearable
          @change="handleSelectedPersonChange"
        >
          <el-option
            v-for="user in selectedPersonOptions"
            :key="user.username"
            :label="`${user.name || user.username} (${user.username})`"
            :value="user.username"
          >
            <div style="display: flex; align-items: center; justify-content: space-between;">
              <div style="display: flex; align-items: center; gap: 8px;">
                <el-icon><ele-User /></el-icon>
                <span>{{ user.name || user.username }}</span>
                <el-tooltip :content="user.realName || user.name" placement="top">
                  <span style="color: #999; font-size: 12px; cursor: help;">({{ user.username }})</span>
                </el-tooltip>
              </div>
              <TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.TmsDutyPatronRole, (user.role || '').toLowerCase())" />
            </div>
          </el-option>
        </el-select>
        <div style="margin-top: 8px; font-size: 12px; color: #999;">
          <el-icon size="12"><ele-InfoFilled /></el-icon>
          {{ getSelectedPersonHint }}
        </div>
      </el-form-item>

      <!-- Swap: Chọn người đổi ca cùng (từ danh sách nhân viên khác) -->
      <el-form-item
        v-if="state.formData.type === 'Swap'"
        :label="$t('lz.label.tmsDutySchedule.swapWithPerson')"
        prop="swapWithUserId"
      >
        <el-select
          v-model="state.formData.swapWithUserId"
          :placeholder="$t('lz.label.tmsDutySchedule.selectSwapPerson')"
          style="width: 100%;"
          filterable
          clearable
          @change="handleSwapWithUserChange"
        >
          <el-option
            v-for="user in state.availableUsers"
            :key="user.username"
            :label="`${user.name || user.username} (${user.username})`"
            :value="user.username"
          >
            <div style="display: flex; align-items: center; justify-content: space-between;">
              <div style="display: flex; align-items: center; gap: 8px;">
                <el-icon><ele-User /></el-icon>
                <span>{{ user.name || user.username }}</span>
                <el-tooltip :content="user.realName || user.name" placement="top">
                  <span style="color: #999; font-size: 12px; cursor: help;">({{ user.username }})</span>
                </el-tooltip>
              </div>
            </div>
          </el-option>
        </el-select>
        <div style="margin-top: 8px; font-size: 12px; color: #999;">
          <el-icon size="12"><ele-InfoFilled /></el-icon>
          {{ $t('lz.message.tmsDutySchedule.swapWithPersonHint') }}
        </div>
      </el-form-item>

      <!-- Swap: Chọn lịch trực của người đổi ca cùng -->
      <el-form-item
        v-if="state.formData.type === 'Swap' && state.formData.swapWithUserId"
        :label="$t('lz.label.tmsDutySchedule.targetSchedule')"
        prop="targetScheduleId"
      >
        <el-select
          v-model="state.formData.targetScheduleId"
          :placeholder="$t('lz.label.tmsDutySchedule.selectTargetSchedule')"
          style="width: 100%;"
          filterable
          :loading="state.loadingUserSchedules"
        >
          <template #empty>
            <div style="padding: 20px; text-align: center;">
              <el-empty :description="$t('lz.message.tmsDutySchedule.noOtherSchedule')" :image-size="80" />
            </div>
          </template>
          <el-option
            v-for="schedule in state.targetUserSchedules"
            :key="schedule.id"
            :label="schedule.name"
            :value="schedule.id"
          >
            <div style="display: flex; flex-direction: column; gap: 4px;">
              <div style="display: flex; align-items: center; gap: 8px;">
                <el-icon color="#409EFF" size="14"><ele-Calendar /></el-icon>
                <span style="font-weight: 500;">{{ schedule.name }}</span>
                <el-tag v-if="schedule.status" size="small" :type="getStatusTagType(schedule.status)" effect="plain">
                  {{ getStatusDisplayName(schedule.status) }}
                </el-tag>
              </div>
              <div style="font-size: 12px; color: var(--el-text-color-secondary); padding-left: 22px;">
                <el-icon size="12"><ele-Clock /></el-icon>
                {{ schedule.startTime ? useDateFormat(schedule.startTime, DateTimeFormatEnum.DateTimeDefault).value : '' }}
                <el-icon size="10" style="margin: 0 4px;"><ele-Right /></el-icon>
                {{ schedule.endTime ? useDateFormat(schedule.endTime, DateTimeFormatEnum.DateTimeDefault).value : '' }}
              </div>
            </div>
          </el-option>
        </el-select>
        <div style="margin-top: 8px; font-size: 12px; color: #999;">
          <el-icon size="12"><ele-InfoFilled /></el-icon>
          {{ $t('lz.message.tmsDutySchedule.swapScheduleHint') }}
        </div>
      </el-form-item>

      <!-- Transfer: Chọn lịch trực để chuyển đến -->
      <el-form-item
        v-if="state.formData.type === 'Transfer'"
        :label="$t('lz.label.tmsDutySchedule.transferToSchedule')"
        prop="targetScheduleId"
      >
        <div style="width: 100%;">
          <el-select
            v-model="state.formData.targetScheduleId"
            :placeholder="$t('lz.label.tmsDutySchedule.selectTransferSchedule')"
            style="width: 100%; margin-bottom: 12px;"
            filterable
            :loading="state.loadingSchedules"
            @focus="loadAvailableSchedules"
          >
            <template #empty>
              <div style="padding: 20px; text-align: center;">
                <el-empty :description="$t('lz.message.tmsDutySchedule.noAvailableSchedule')">
                  <el-button type="primary" size="small" @click="handleCreateNewSchedule">
                    <el-icon style="margin-right: 4px;"><ele-Plus /></el-icon>
                    {{ $t('lz.label.tmsDutySchedule.createNewSchedule') }}
                  </el-button>
                </el-empty>
              </div>
            </template>
            <el-option
              v-for="item in state.availableSchedules"
              :key="item.schedule?.id"
              :label="item.schedule?.name"
              :value="item.schedule?.id"
              class="schedule-option"
            >
              <div class="schedule-option-wrapper">
                <div class="option-header">
                  <div class="option-title">
                    <el-icon color="#409EFF" size="16"><ele-Calendar /></el-icon>
                    <span class="option-name">{{ item.schedule?.name }}</span>
                  </div>
                  <div class="option-badges">
                    <el-tag v-if="item.isSamePatrolTeam" type="success" size="small" effect="plain">
                      <el-icon size="12"><ele-Check /></el-icon>
                      {{ $t('lz.label.tmsDutySchedule.sameTeam') }}
                    </el-tag>
                    <el-tag v-if="item.schedule?.status" size="small" :type="getStatusTagType(item.schedule.status)" effect="plain">
                      {{ getStatusDisplayName(item.schedule.status) }}
                    </el-tag>
                  </div>
                </div>
                <div class="option-details">
                  <!-- Time range with duration -->
                  <div class="option-detail-row">
                    <el-icon size="14" color="#909399"><ele-Clock /></el-icon>
                    <span class="detail-text">
                      {{ item.schedule?.startTime ? useDateFormat(item.schedule.startTime, 'DD/MM/YYYY HH:mm').value : '' }}
                      <el-icon size="12" style="margin: 0 4px;"><ele-Right /></el-icon>
                      {{ item.schedule?.endTime ? useDateFormat(item.schedule.endTime, 'DD/MM/YYYY HH:mm').value : '' }}
                      <span v-if="item.schedule?.startTime" class="time-duration">
                        ({{ getScheduleDuration(item.schedule.startTime, item.schedule.endTime) }})
                      </span>
                    </span>
                  </div>
                  <!-- Member count breakdown -->
                  <div class="option-detail-row">
                    <el-icon size="14" color="#409EFF"><ele-User /></el-icon>
                    <span class="detail-text">
                      <strong>{{ item.currentMemberCount ?? item.schedule?.details?.length ?? 0 }}</strong> {{ $t('lz.label.tmsDutySchedule.person') }}
                      <template v-if="item.leaderCount !== undefined || item.memberCount !== undefined">
                        <span class="detail-separator">•</span>
                        <el-icon size="12" color="#E6A23C"><ele-StarFilled /></el-icon>
                        {{ item.leaderCount ?? 0 }} {{ $t('lz.label.tmsDutySchedule.leader') }}
                        <span class="detail-separator">•</span>
                        <el-icon size="12"><ele-User /></el-icon>
                        {{ item.memberCount ?? 0 }} {{ $t('lz.label.tmsDutySchedule.member') }}
                      </template>
                    </span>
                  </div>
                  <!-- Creator info if available -->
                  <div v-if="item.schedule?.createUId" class="option-detail-row">
                    <el-icon size="14" color="#67C23A"><ele-UserFilled /></el-icon>
                    <span class="detail-text">
                      {{ $t('lz.label.tmsDutySchedule.creator') }}: {{ getEmployeeName(item.schedule.createUId) }}
                    </span>
                  </div>
                  <!-- Time conflict warning -->
                  <div v-if="item.hasTimeConflict" class="option-detail-row warning">
                    <el-icon size="14" color="#E6A23C"><ele-Warning /></el-icon>
                    <span class="detail-text">{{ $t('lz.message.tmsDutySchedule.hasTimeConflict') }}</span>
                  </div>
                </div>
              </div>
            </el-option>
          </el-select>

          <el-alert
            v-if="!state.loadingSchedules"
            type="info"
            :closable="false"
            show-icon
            style="margin-top: 0px; margin-bottom: 10px;"
          >
            <template #title>
              <div style="display: flex; align-items: center; justify-content: space-between;">
                <span style="font-size: 13px;">{{ $t('lz.message.tmsDutySchedule.noAvailableSchedule') }}</span>
                <el-button type="primary" size="small" text @click="handleCreateNewSchedule">
                  <el-icon><ele-Plus /></el-icon>
                  {{ $t('lz.label.tmsDutySchedule.createNew') }}
                </el-button>
              </div>
            </template>
          </el-alert>
        </div>
      </el-form-item>

      <!-- Transfer: Chọn vai trò trong lịch mới -->
      <el-form-item
        v-if="state.formData.type === 'Transfer' && state.formData.targetScheduleId"
        :label="$t('lz.label.tmsDutySchedule.desiredRole')"
        prop="newRole"
      >
        <el-radio-group v-model="state.formData.newRole" style="width: 100%;" v-for="value in state">
          <el-radio value="member">
            <div style="display: flex; align-items: center; gap: 8px;">
              <el-icon><ele-User /></el-icon>
              <span>{{ $t('lz.label.tmsDutySchedule.member') }}</span>
            </div>
          </el-radio>
          <el-radio value="leader">
            <div style="display: flex; align-items: center; gap: 8px;">
              <el-icon color="#E6A23C"><ele-UserFilled /></el-icon>
              <span>{{ $t('lz.label.tmsDutySchedule.leader') }}</span>
            </div>
          </el-radio>
        </el-radio-group>
        <div style="margin-top: 8px; font-size: 12px; color: #999;">
          <el-icon size="12"><ele-InfoFilled /></el-icon>
          {{ $t('lz.message.tmsDutySchedule.newRoleHint') }}
        </div>
      </el-form-item>

      <!-- Replace: Chọn cách thay thế -->
      <el-form-item
        v-if="state.formData.type === 'Replace' && state.formData.targetUserId"
        :label="$t('lz.label.tmsDutySchedule.replacer')"
      >
        <el-radio-group v-model="state.replaceMode" @change="handleReplaceModeChange">
          <el-radio value="self">
            <div style="display: flex; align-items: center; gap: 6px;">
              <el-icon color="#67C23A"><ele-UserFilled /></el-icon>
              <span>{{ $t('lz.label.tmsDutySchedule.iWillReplace') }}</span>
            </div>
          </el-radio>
          <el-radio value="specified">
            <div style="display: flex; align-items: center; gap: 6px;">
              <el-icon color="#409EFF"><ele-User /></el-icon>
              <span>{{ $t('lz.label.tmsDutySchedule.specifyReplacer') }}</span>
            </div>
          </el-radio>
        </el-radio-group>
      </el-form-item>

      <el-alert
        v-if="state.formData.type === 'Replace' && state.formData.targetUserId && state.replaceMode === 'self'"
        type="success"
        :closable="false"
        show-icon
        style="margin-bottom: 16px;"
      >
        <template #title>
          <span style="font-size: 13px;">
            {{ $t('lz.message.tmsDutySchedule.youWillReplace', { name: getSelectedUserName(state.formData.targetUserId) }) }}
          </span>
        </template>
      </el-alert>

      <!-- Bước 3: Chọn người thay thế cụ thể (nếu chọn "Chỉ định người khác") -->
      <el-form-item
        v-if="state.formData.type === 'Replace' && state.formData.targetUserId && state.replaceMode === 'specified'"
        :label="$t('lz.label.tmsDutySchedule.selectReplacer')"
        prop="replacerId"
      >
        <el-select
          v-model="state.formData.replacerId"
          :placeholder="$t('lz.label.tmsDutySchedule.selectReplacerPlaceholder')"
          style="width: 100%;"
          filterable
        >
          <el-option
            v-for="user in state.availableUsersForReplace"
            :key="user.username"
            :label="`${user.name} (${user.username})`"
            :value="user.username"
          >
            <div style="display: flex; align-items: center; justify-content: space-between;">
              <div style="display: flex; align-items: center; gap: 8px;">
                <el-icon><ele-User /></el-icon>
                <span>{{ user.name }}</span>
                <el-tooltip :content="user.realName || user.name" placement="top">
                  <span style="color: #999; font-size: 12px; cursor: help;">({{ user.username }})</span>
                </el-tooltip>
              </div>
            </div>
          </el-option>
        </el-select>
        <div style="margin-top: 8px; font-size: 12px; color: #999;">
          <el-icon size="12"><ele-InfoFilled /></el-icon>
          {{ $t('lz.message.tmsDutySchedule.replacerHint', { name: getSelectedUserName(state.formData.targetUserId) }) }}
        </div>
      </el-form-item>

      <el-form-item :label="$t('lz.label.tmsDutySchedule.reason')" prop="reason" class="reason-form-item">
        <el-input
          v-model="state.formData.reason"
          type="textarea"
          :rows="3"
          :placeholder="$t('lz.label.tmsDutySchedule.reasonPlaceholder')"
          maxlength="500"
          show-word-limit
        />
      </el-form-item>

      <el-form-item :label="$t('lz.entity.base.remark')" class="mt15">
        <el-input
          v-model="state.formData.remark"
          type="textarea"
          :rows="2"
          :placeholder="$t('lz.label.tmsDutySchedule.remarkPlaceholder')"
          maxlength="500"
          show-word-limit
        />
      </el-form-item>

      <el-alert
        v-if="state.formData.type === 'Absent'"
        type="warning"
        :closable="false"
        show-icon
      >
        <template #title>
          <span style="font-size: 13px;">
            {{ $t('lz.message.tmsDutySchedule.absentApprovalNote') }}
          </span>
        </template>
      </el-alert>

      <el-alert
        v-if="state.formData.type === 'Swap'"
        type="info"
        :closable="false"
        show-icon
      >
        <template #title>
          <span style="font-size: 13px;">
            {{ $t('lz.message.tmsDutySchedule.swapApprovalNote') }}
          </span>
        </template>
      </el-alert>

      <el-alert
        v-if="state.formData.type === 'Transfer'"
        type="info"
        :closable="false"
        show-icon
      >
        <template #title>
          <span style="font-size: 13px;">
            {{ $t('lz.message.tmsDutySchedule.transferApprovalNote') }}
          </span>
        </template>
      </el-alert>
    </el-form>

    <template #footer>
      <span class="dialog-footer">
        <el-button @click="handleClose">{{ $t('lz.button.base.cancel') }}</el-button>
        <el-button type="primary" :loading="state.loading" @click="handleSubmit">{{ $t('lz.label.tmsDutySchedule.submitRequest') }}</el-button>
      </span>
    </template>
  </el-dialog>
</template>

<script lang="ts" setup name="changeRequestDialog">
import { reactive, ref, watch, computed, nextTick } from 'vue';
import { ElMessage, FormInstance, FormRules } from 'element-plus';
import { useDateFormat } from '@vueuse/core';
import { getAPI } from '/@/utils/axios-utils';
import { TmsDutyScheduleApi } from '/@/api-services/tms';
import type { 
  DutyScheduleOutput, 
  CreateChangeRequestInput,
  GetAvailableSchedulesInput,
  AvailableScheduleOutput,
  GetUpcomingSchedulesInput
} from '/@/api-services/tms';
import { DateTimeFormatEnum } from '/@/types/enums/baseEnum';
import { BaseConfigTypeEnum } from '/@/types/enums';
import { useUserInfo } from '/@/stores/userInfo';
import { useI18n } from 'vue-i18n';
import { useBaseConfig } from '/@/stores/baseConfig';
import TagInfo from '/@/components/advanced/tag-info.vue';

// Get i18n and base config
const { t } = useI18n();
const { getConfigItemByCode, getConfigDataByCode } = useBaseConfig();

// Check if role is the primary role (first role in config, typically 'leader')
const isPrimaryRole = (roleCode: string | null | undefined) => {
  if (!roleCode) return false;
  return 'leader' === roleCode?.toLowerCase();
};

// Get role config by code
const getRoleConfig = (roleCode: string | null | undefined) => {
  if (!roleCode) return null;
  return getConfigItemByCode(BaseConfigTypeEnum.TmsDutyPatronRole, roleCode.toLowerCase());
};

// Get user info store
const userInfoStore = useUserInfo();

interface Props {
  schedule: DutyScheduleOutput | null;
  employeeList: any[];
}

const props = defineProps<Props>();
const emit = defineEmits(['success', 'close', 'create-new-schedule']);

const formRef = ref<FormInstance>();

/**
 * Get status display name from base config
 * @param status The status code (e.g., 'register', 'confirmed')
 * @returns Display name from config or the original status
 */
const getStatusDisplayName = (status: string): string => {
  if (!status) return '';
  const configItem = getConfigItemByCode(BaseConfigTypeEnum.TmsDutyState, status.toLowerCase());
  return configItem?.name || status;
};

const state = reactive({
  visible: false,
  dialogTitle: t('lz.label.tmsDutySchedule.createChangeRequest'),
  loading: false,
  loadingSchedules: false,
  loadingUserSchedules: false,
  replaceMode: 'self' as 'self' | 'specified',
  formData: {
    dutyScheduleId: '',
    // API expects PascalCase: 'Absent' | 'Swap' | 'Replace' | 'Transfer'
    type: 'Absent' as 'Absent' | 'Swap' | 'Replace' | 'Transfer',
    targetUserId: '',
    // For Swap: person to swap with (from other schedule)
    swapWithUserId: '',
    replacerId: '',
    targetScheduleId: '',
    // API expects lowercase: 'leader' | 'member' (per BaseConfig)
    newRole: 'member' as 'leader' | 'member',
    reason: '',
    remark: ''
  },
  availableUsers: [] as any[],
  availableUsersForReplace: [] as any[],
  usersInSchedule: [] as any[],
  availableSchedules: [] as AvailableScheduleOutput[],
  targetUserSchedules: [] as DutyScheduleOutput[]
});

/**
 * Check if current user can use Replace feature
 * Only creator or leader of the schedule can use this feature
 */
const canUseReplaceFeature = computed(() => {
  if (!props.schedule) return false;
  const currentUserId = userInfoStore.userInfos?.account || '';
  
  // Check if current user is the creator
  if (props.schedule.createUId === currentUserId) return true;
  
  // Check if current user is a leader in this schedule
  if (props.schedule.details) {
    const userDetail = props.schedule.details.find((d: any) => d.userId === currentUserId);
    if (userDetail && userDetail.role === 'leader') {
      return true;
    }
  }
  
  return false;
});

/**
 * Computed: Label for selected person field based on request type
 */
const getSelectedPersonLabel = computed(() => {
  switch (state.formData.type) {
    case 'Absent':
      return t('lz.label.tmsDutySchedule.personToAbsent');
    case 'Swap':
      return t('lz.label.tmsDutySchedule.personRequestingSwap');
    case 'Replace':
      return t('lz.label.tmsDutySchedule.personToReplace');
    case 'Transfer':
      return t('lz.label.tmsDutySchedule.personToTransfer');
    default:
      return t('lz.label.tmsDutySchedule.selectedPerson');
  }
});

/**
 * Computed: Placeholder for selected person field based on request type
 */
const getSelectedPersonPlaceholder = computed(() => {
  switch (state.formData.type) {
    case 'Absent':
      return t('lz.label.tmsDutySchedule.selectPersonToAbsent');
    case 'Swap':
      return t('lz.label.tmsDutySchedule.selectPersonRequestingSwap');
    case 'Replace':
      return t('lz.label.tmsDutySchedule.selectPersonToReplace');
    case 'Transfer':
      return t('lz.label.tmsDutySchedule.selectPersonToTransfer');
    default:
      return t('lz.label.tmsDutySchedule.selectPerson');
  }
});

/**
 * Computed: Hint message for selected person field based on request type
 */
const getSelectedPersonHint = computed(() => {
  switch (state.formData.type) {
    case 'Absent':
      return t('lz.message.tmsDutySchedule.absentPersonHint');
    case 'Swap':
      return t('lz.message.tmsDutySchedule.swapHint');
    case 'Replace':
      return t('lz.message.tmsDutySchedule.replacePersonHint');
    case 'Transfer':
      return t('lz.message.tmsDutySchedule.transferPersonHint');
    default:
      return '';
  }
});

/**
 * Computed: Options for selected person dropdown based on request type
 * All types now show users in current schedule (the person making/related to the request)
 * - Absent: person requesting absence (from current schedule)
 * - Swap: person requesting swap (from current schedule) 
 * - Replace: person being replaced (from current schedule)
 * - Transfer: person being transferred (from current schedule)
 */
const selectedPersonOptions = computed(() => {
  // All types use users in current schedule for the primary person selection
  return state.usersInSchedule;
});

/**
 * Handle selected person change
 * Triggers type-specific actions when user selection changes
 */
const handleSelectedPersonChange = () => {
  // For Transfer: reload available schedules when user changes
  if (state.formData.type === 'Transfer') {
    state.formData.targetScheduleId = '';
    loadAvailableSchedules();
  }
  // For Swap: reset swapWithUserId and targetScheduleId when person changes
  else if (state.formData.type === 'Swap') {
    // Don't reset swapWithUserId, just reset targetScheduleId
    state.formData.targetScheduleId = '';
    state.targetUserSchedules = [];
  }
};

/**
 * Handle swap with user change (for Swap type)
 * Load schedules of the selected user to swap with
 */
const handleSwapWithUserChange = () => {
  state.formData.targetScheduleId = '';
  if (state.formData.swapWithUserId) {
    loadTargetUserSchedulesForSwap();
  } else {
    state.targetUserSchedules = [];
  }
};

/**
 * Load schedules of the user to swap with (for Swap type)
 */
const loadTargetUserSchedulesForSwap = async () => {
  if (!state.formData.swapWithUserId) return;
  
  try {
    state.loadingUserSchedules = true;
    state.targetUserSchedules = [];
    
    const requestData: GetUpcomingSchedulesInput = {
      userId: state.formData.swapWithUserId,
      maxCount: 10,
      maxDays: 90,
      activeOnly: true
    };
    
    const response = await getAPI(TmsDutyScheduleApi).apiTmsTmsdutyscheduleGetupcomingPost(requestData);
    
    if (response.data?.result) {
      // Filter out current schedule
      state.targetUserSchedules = response.data.result.filter(
        (schedule: DutyScheduleOutput) => schedule.id !== props.schedule?.id
      );
    }
  } catch (error: any) {
    console.error('Error loading swap target user schedules:', error);
    ElMessage.warning(t('lz.message.tmsDutySchedule.loadUserSchedulesError'));
    state.targetUserSchedules = [];
  } finally {
    state.loadingUserSchedules = false;
  }
};

/**
 * Get user name by userId from users in schedule
 */
const getSelectedUserName = (userId: string) => {
  if (!userId) return '';
  const user = state.usersInSchedule.find((u: any) => u.username === userId);
  return user?.name || userId;
};

const rules: FormRules = {
  type: [{ required: true, message: () => t('lz.message.tmsDutySchedule.selectRequestType'), trigger: 'change' }],
  targetScheduleId: [
    {
      required: true,
      validator: (rule, value, callback) => {
        if ((state.formData.type === 'Transfer' || state.formData.type === 'Swap') && !value) {
          const message = state.formData.type === 'Transfer'
            ? t('lz.message.tmsDutySchedule.targetScheduleRequired')
            : t('lz.message.tmsDutySchedule.targetScheduleRequired');
          callback(new Error(message));
        } else {
          callback();
        }
      },
      trigger: 'change'
    }
  ],
  newRole: [
    {
      required: false,
      validator: (rule, value, callback) => {
        // newRole is optional for Transfer
        callback();
      },
      trigger: 'change'
    }
  ],
  targetUserId: [
    {
      required: true,
      validator: (rule, value, callback) => {
        // For Swap: targetUserId is person from current schedule requesting swap (optional if not leader/creator)
        // For Replace: always require targetUserId (person being replaced)
        if (state.formData.type === 'Replace' && !value) {
          callback(new Error(t('lz.message.tmsDutySchedule.replacePersonRequired')));
        }
        // For Transfer: always require targetUserId (person being transferred)
        else if (state.formData.type === 'Transfer' && !value) {
          callback(new Error(t('lz.message.tmsDutySchedule.transferPersonRequired')));
        } else {
          callback();
        }
      },
      trigger: 'change'
    }
  ],
  swapWithUserId: [
    {
      required: true,
      validator: (rule, value, callback) => {
        // For Swap: always require swapWithUserId (person to swap with from other schedule)
        if (state.formData.type === 'Swap' && !value) {
          callback(new Error(t('lz.message.tmsDutySchedule.swapPersonRequired')));
        } else {
          callback();
        }
      },
      trigger: 'change'
    }
  ],
  replacerId: [
    {
      required: true,
      validator: (rule, value, callback) => {
        // For Replace with specified mode: require replacerId
        if (state.formData.type === 'Replace' && state.replaceMode === 'specified' && !value) {
          callback(new Error(t('lz.message.tmsDutySchedule.targetPersonRequired')));
        } else {
          callback();
        }
      },
      trigger: 'change'
    }
  ],
  reason: [
    { required: true, message: () => t('lz.message.tmsDutySchedule.reasonRequired'), trigger: 'blur' },
    { min: 10, message: () => t('lz.message.tmsDutySchedule.reasonMinLength'), trigger: 'blur' }
  ]
};

const handleTypeChange = () => {
  // Reset type-specific fields but PRESERVE targetUserId across type changes
  // This allows user to select a person once and switch between request types
  const preservedTargetUserId = state.formData.targetUserId;
  
  // Reset common fields
  state.formData.targetScheduleId = '';
  state.formData.newRole = 'member';
  state.formData.replacerId = '';
  state.formData.swapWithUserId = '';
  state.replaceMode = 'self';
  state.targetUserSchedules = [];
  
  // Restore preserved targetUserId
  state.formData.targetUserId = preservedTargetUserId;
  
  // Type-specific actions
  if (state.formData.type === 'Swap') {
    // Swap: swapWithUserId needs to be selected separately
    // No auto-loading needed here
  } else if (state.formData.type === 'Transfer') {
    // Transfer: load available schedules for transfer
    loadAvailableSchedules();
  }
};

/**
 * Handle replace mode change (self or specified)
 * Clear replacerId when switching to self mode
 */
const handleReplaceModeChange = (mode: 'self' | 'specified') => {
  if (mode === 'self') {
    state.formData.replacerId = '';
  } else {
    // Load available users for replace when switching to specified mode
    loadAvailableUsersForReplace();
  }
  // Clear validation errors
  formRef.value?.clearValidate('replacerId');
};

/**
 * Load upcoming schedules that the selected user is participating in (for Swap)
 * Filters out the current schedule to only show different schedules
 * Uses GetUpcoming API to get relevant future schedules
 */
const loadTargetUserSchedules = async () => {
  if (!state.formData.targetUserId) return;
  
  try {
    state.loadingUserSchedules = true;
    state.targetUserSchedules = [];
    
    // Query upcoming schedules where the target user is a member
    // Use GetUpcoming API to get relevant future schedules for swapping
    const requestData: GetUpcomingSchedulesInput = {
      userId: state.formData.targetUserId,
      maxCount: 10, // Get up to 10 upcoming schedules
      maxDays: 90, // Look ahead 90 days (3 months)
      activeOnly: true // Only get active schedules
    };
    
    const response = await getAPI(TmsDutyScheduleApi).apiTmsTmsdutyscheduleGetupcomingPost(requestData);
    
    if (response.data?.result) {
      // Filter out current schedule - only show different schedules for swap
      state.targetUserSchedules = response.data.result.filter(
        (schedule: DutyScheduleOutput) => schedule.id !== props.schedule?.id
      );
    }
  } catch (error: any) {
    console.error('Error loading target user schedules:', error);
    ElMessage.warning(t('lz.message.tmsDutySchedule.loadUserSchedulesError'));
    state.targetUserSchedules = [];
  } finally {
    state.loadingUserSchedules = false;
  }
};

/**
 * Load available schedules for swap that don't conflict with current schedule
 * 
 * Backend API:
 * - Endpoint: POST /api/Tms/TmsDutySchedule/GetAvailableSchedules
 * - Request: GetAvailableSchedulesInput { currentScheduleId, userId, dateRangeDays }
 * - Response: WResultListAvailableScheduleOutput { result: AvailableScheduleOutput[] }
 * 
 * AvailableScheduleOutput includes:
 * - schedule: DutyScheduleOutput (full schedule info)
 * - hasTimeConflict: boolean (conflict detection)
 * - conflictReason: string (why it conflicts)
 * - canJoin: boolean (can user join this schedule)
 * - priority: string (recommendation priority)
 * - currentMemberCount: number (current team size)
 * - isSamePatrolTeam: boolean (same team flag)
 */
const loadAvailableSchedules = async () => {
  if (!props.schedule?.id) return;
  
  // For Transfer: must have selected user to transfer
  const targetUserId = state.formData.targetUserId;
  if (state.formData.type === 'Transfer' && !targetUserId) {
    state.availableSchedules = [];
    return;
  }
  
  try {
    state.loadingSchedules = true;
    
    // Call API to get available schedules for transfer
    // Use the selected user's ID, not the current user
    const requestInput: GetAvailableSchedulesInput = {
      currentScheduleId: props.schedule.id,
      userId: targetUserId || userInfoStore.userInfos?.account || '',
      dateRangeDays: 30 // Look ahead 30 days
    };
    
    const response = await getAPI(TmsDutyScheduleApi).apiTmsTmsdutyscheduleGetavailableschedulesPost(requestInput);
    
    if (response.data?.result) {
      state.availableSchedules = response.data.result;
    } else {
      state.availableSchedules = [];
    }
  } catch (error: any) {
    console.error('Error loading available schedules:', error);
    // If API not implemented yet, show empty list
    state.availableSchedules = [];
    
    // Only show warning if it's a real error, not 404
    if (error.response?.status !== 404) {
      ElMessage.warning(t('lz.message.tmsDutySchedule.loadScheduleError'));
    }
  } finally {
    state.loadingSchedules = false;
  }
};

/**
 * Get status tag type for schedule status
 */
const getStatusTagType = (status: string): 'success' | 'info' | 'warning' | 'danger' => {
  const statusMap: Record<string, 'success' | 'info' | 'warning' | 'danger'> = {
    'Register': 'info',
    'Confirmed': 'primary' as any,
    'InProgress': 'warning',
    'Finished': 'success',
    'Cancelled': 'danger'
  };
  return statusMap[status] || 'info';
};

/**
 * Get schedule duration in human-readable format
 */
const getScheduleDuration = (startTime: string | Date | undefined, endTime: string | Date | undefined): string => {
  if (!startTime || !endTime) return '';
  
  const start = new Date(startTime);
  const end = new Date(endTime);
  const diffMs = end.getTime() - start.getTime();
  
  const hours = Math.floor(diffMs / (1000 * 60 * 60));
  const days = Math.floor(hours / 24);
  const remainingHours = hours % 24;
  
  if (days > 0) {
    return remainingHours > 0 
      ? `${days} ${t('lz.label.base.day')}${remainingHours > 0 ? ' ' + remainingHours + ' ' + t('lz.label.base.hour') : ''}`
      : `${days} ${t('lz.label.base.day')}`;
  }
  return `${hours} ${t('lz.label.base.hour')}`;
};

/**
 * Get employee name by username from employeeList
 */
const getEmployeeName = (username: string): string => {
  if (!username) return '';
  const emp = props.employeeList.find(e => e.username === username);
  return emp?.name || emp?.realName || username;
};

/**
 * Handle create new schedule button click
 */
const handleCreateNewSchedule = () => {
  ElMessage.info({
    message: t('lz.message.tmsDutySchedule.closeDialogToCreateSchedule'),
    duration: 3000
  });
  handleClose();
  // Emit event to parent to open create schedule dialog
  emit('create-new-schedule');
};

const loadAvailableUsers = () => {
  // Get list of users excluding current user
  // Note: userId in this context is actually username (account), not GUID
  const currentUsername = userInfoStore.userInfos?.account || '';
  state.availableUsers = props.employeeList.filter(emp => emp.username !== currentUsername);
};

/**
 * Load users currently in this schedule (for Replace - select who to replace)
 */
const loadUsersInSchedule = () => {
  if (!props.schedule?.details) {
    state.usersInSchedule = [];
    return;
  }
  // Get all users in the schedule - map to match template property names
  state.usersInSchedule = props.schedule.details.map((d: any) => {
    // Try to find full employee info from employeeList
    const empInfo = props.employeeList.find(e => e.username === d.userId);
    return {
      username: d.userId,
      name: empInfo?.name || d.userName || d.userId,
      realName: empInfo?.realName || d.userName,
      role: d.role,
      status: d.status
    };
  });
};

/**
 * Load available users for Replace feature
 * Get list of users excluding current user and the person being replaced
 */
const loadAvailableUsersForReplace = () => {
  const currentUsername = userInfoStore.userInfos?.account || '';
  const targetUserId = state.formData.targetUserId;
  // Filter out current user and the person being replaced
  state.availableUsersForReplace = props.employeeList.filter(emp => 
    emp.username !== currentUsername && emp.username !== targetUserId
  );
};

const openDialog = () => {
  if (!props.schedule?.id) {
    ElMessage.error(t('lz.message.tmsDutySchedule.scheduleInfoNotFound'));
    return;
  }

  state.formData = {
    dutyScheduleId: props.schedule.id,
    type: 'Absent',
    targetUserId: '',
    swapWithUserId: '',
    replacerId: '',
    targetScheduleId: '',
    newRole: 'member',
    reason: '',
    remark: ''
  };

  // Load users in this schedule for Replace feature
  loadUsersInSchedule();
  loadAvailableUsers();
  state.visible = true;
};

/**
 * Open dialog with preset values (type and/or targetUserId)
 * Called from parent component when clicking on specific user actions
 * @param options - Preset options { type, targetUserId }
 */
const openDialogWithPreset = (options?: { type?: 'Absent' | 'Swap' | 'Replace' | 'Transfer'; targetUserId?: string }) => {
  if (!props.schedule?.id) {
    ElMessage.error(t('lz.message.tmsDutySchedule.scheduleInfoNotFound'));
    return;
  }

  state.formData = {
    dutyScheduleId: props.schedule.id,
    type: options?.type || 'Absent',
    targetUserId: options?.targetUserId || '',
    swapWithUserId: '',
    replacerId: '',
    targetScheduleId: '',
    newRole: 'member',
    reason: '',
    remark: ''
  };

  // Load users in this schedule for Replace/Transfer feature
  loadUsersInSchedule();
  loadAvailableUsers();
  state.visible = true;

  // Trigger type change handler after dialog is opened
  nextTick(() => {
    handleTypeChange();
  });
};

/**
 * Submit change request to API
 * 
 * Type Rules:
 * 1. Absent: Type + Reason → Status = "absent"
 * 2. Swap: Type + TargetUserId + TargetScheduleId + Reason → Cross-schedule swap (User A in Schedule X ↔ User B in Schedule Y)
 * 3. Replace: Type + TargetUserId + ReplacerId + Reason → TargetUserId (person being replaced) + ReplacerId (person who replaces)
 * 4. Transfer: Type + TargetScheduleId + NewRole(optional) + Reason → DutyScheduleId changes
 * 
 * API Payload Structure:
 * - dutyScheduleId: GUID of current schedule
 * - type: PascalCase - 'Absent' | 'Swap' | 'Replace' | 'Transfer'
 * - targetUserId: username (account), not GUID - for Swap (person to swap with) / Replace (person being replaced)
 * - replacerId: username (account) - for Replace (person who replaces targetUserId)
 * - targetScheduleId: GUID - for Swap/Transfer
 * - newRole: lowercase 'leader'|'member' - optional for Transfer
 * - reason: required, min 10 chars
 * - remark: optional
 */
const handleSubmit = async () => {
  if (!formRef.value) return;

  try {
    await formRef.value.validate();

    state.loading = true;

    // Note: targetUserId in API is actually username (account), not GUID
    const requestData: CreateChangeRequestInput = {
      dutyScheduleId: state.formData.dutyScheduleId,
      type: state.formData.type,
      reason: state.formData.reason,
      remark: state.formData.remark || undefined
    };

    // Add targetUserId for Absent (if leader/creator specifies who), Swap (swap with person), Replace (person being replaced), or Transfer (person requesting transfer)
    if (state.formData.type === 'Absent') {
      // For Absent:
      // - If targetUserId is specified (leader/creator selecting who to absent), use it
      // - Otherwise, the backend will use the current user as requester
      if (state.formData.targetUserId) {
        requestData.targetUserId = state.formData.targetUserId;
      }
    } else if (state.formData.type === 'Swap') {
      // For Swap:
      // - targetUserId in API = person to swap WITH (from other schedule) = swapWithUserId in form
      // - The person requesting swap (from current schedule) = targetUserId in form (sent as requesterId or handled by backend)
      requestData.targetUserId = state.formData.swapWithUserId;
      // Note: If backend needs to know who is requesting the swap from current schedule,
      // it may use the current user or we may need to add replacerId for this purpose
      if (state.formData.targetUserId) {
        requestData.replacerId = state.formData.targetUserId; // Person from current schedule requesting swap
      }
    } else if (state.formData.type === 'Replace') {
      // For Replace:
      // - targetUserId: person being replaced (selected from schedule members)
      // - replacerId: person who will replace (self or specified)
      requestData.targetUserId = state.formData.targetUserId;
      
      if (state.replaceMode === 'self') {
        // Current user will replace
        requestData.replacerId = userInfoStore.userInfos?.account || '';
      } else {
        // Specified user will replace
        requestData.replacerId = state.formData.replacerId;
      }
    } else if (state.formData.type === 'Transfer') {
      // For Transfer:
      // - targetUserId: person requesting transfer (the user being moved to another schedule)
      // If not selected, use current user
      requestData.targetUserId = state.formData.targetUserId || userInfoStore.userInfos?.account || '';
    }
    
    // Add targetScheduleId for Swap (cross-schedule swap) or Transfer
    if (state.formData.type === 'Swap' || state.formData.type === 'Transfer') {
      requestData.targetScheduleId = state.formData.targetScheduleId;
    }

    // Add newRole for Transfer (optional)
    if (state.formData.type === 'Transfer' && state.formData.newRole) {
      requestData.newRole = state.formData.newRole;
    }

    await getAPI(TmsDutyScheduleApi).apiTmsTmsdutyscheduleCreatechangerequestPost(requestData);

    ElMessage.success(t('lz.message.tmsDutySchedule.requestSentSuccess'));
    emit('success');
    handleClose();
  } catch (error: any) {
    console.error('Error creating change request:', error);
    ElMessage.error(error.message || t('lz.message.tmsDutySchedule.requestError'));
  } finally {
    state.loading = false;
  }
};

const handleClose = () => {
  state.visible = false;
  formRef.value?.resetFields();
  emit('close');
};

watch(() => props.schedule, () => {
  if (props.schedule?.id && state.visible) {
    state.formData.dutyScheduleId = props.schedule.id;
  }
});

// Watch for targetUserId changes to reload available schedules for Transfer
watch(() => state.formData.targetUserId, (newVal) => {
  if (state.formData.type === 'Transfer' && newVal && state.visible) {
    // Reset target schedule when user changes
    state.formData.targetScheduleId = '';
    // Reload available schedules for the selected user
    loadAvailableSchedules();
  }
});

defineExpose({
  openDialog,
  openDialogWithPreset,
  state,
  handleTypeChange
});
</script>

<style lang="scss" scoped>
// Schedule card styling
.schedule-card {
  width: 100%;
  background: linear-gradient(135deg, #f8f9fa 0%, #ffffff 100%);
  border: 2px solid var(--el-border-color-light);
  border-radius: 12px;
  overflow: hidden;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.06);

  .schedule-card-header {
    padding: 10px 14px;
    background: linear-gradient(135deg, var(--el-color-primary-light-9) 0%, #ffffff 100%);
    border-bottom: 1px solid var(--el-border-color-lighter);
    display: flex;
    align-items: center;
    justify-content: space-between;

    .schedule-name {
      display: flex;
      align-items: center;
      gap: 10px;

      .name-icon {
        color: var(--el-color-primary);
        font-size: 18px;
      }

      .name-text {
        font-size: 15px;
        font-weight: 600;
        color: var(--el-text-color-primary);
      }
    }
  }

  .schedule-card-body {
    padding: 8px 14px;

    .schedule-info-item {
      display: flex;
      align-items: flex-start;
      gap: 10px;
      padding: 6px 0;

      &.compact-time {
        align-items: center;
      }

      &:not(:last-child) {
        border-bottom: 1px dashed var(--el-border-color-lighter);
      }

      &.members-section {
        align-items: flex-start;
      }

      .item-icon {
        color: var(--el-color-primary);
        font-size: 16px;
        margin-top: 2px;
        flex-shrink: 0;
      }

      .item-content {
        flex: 1;
        display: flex;
        flex-direction: column;
        gap: 4px;

        .item-label {
          font-size: 12px;
          color: var(--el-text-color-secondary);
          font-weight: 500;
        }

        .item-value {
          font-size: 14px;
          color: var(--el-text-color-primary);
          font-weight: 600;
        }

        .members-tags {
          display: flex;
          flex-wrap: wrap;
          gap: 2px;
          line-height: 1.5;
        }
      }
    }
  }
}

// Transfer schedule option styling
.schedule-option-wrapper {
  padding: 4px 0;

  .option-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    margin-bottom: 8px;

    .option-title {
      display: flex;
      align-items: center;
      gap: 8px;

      .option-name {
        font-size: 14px;
        font-weight: 600;
        color: var(--el-text-color-primary);
      }
    }

    .option-badges {
      display: flex;
      align-items: center;
      gap: 6px;
    }
  }

  .option-details {
    padding-left: 24px;

    .option-detail-row {
      display: flex;
      align-items: center;
      gap: 6px;
      margin-bottom: 4px;
      font-size: 12px;

      &:last-child {
        margin-bottom: 0;
      }

      .detail-text {
        color: var(--el-text-color-secondary);
        display: flex;
        align-items: center;
        gap: 4px;

        strong {
          color: var(--el-color-primary);
          font-weight: 600;
        }

        .detail-separator {
          margin: 0 4px;
          color: var(--el-border-color);
        }
      }

      &.warning .detail-text {
        color: var(--el-color-warning);
        font-weight: 500;
      }
    }
  }
}

.dialog-footer {
  display: flex;
  justify-content: flex-end;
}

// Dialog styling
:deep(.el-dialog) {
  border-radius: 12px;
  overflow: hidden;
}

:deep(.el-dialog__header) {
  background: linear-gradient(135deg, var(--el-color-primary) 0%, var(--el-color-primary-light-3) 100%);
  padding: 18px 24px;
  margin: 0;
  border-radius: 0;
}

:deep(.el-dialog__body) {
  padding: 20px 24px;
  max-height: 70vh;
  overflow-y: auto;
}

:deep(.el-dialog__footer) {
  padding: 16px 24px;
  border-top: 1px solid var(--el-border-color-lighter);
  background-color: var(--el-fill-color-lighter);
}

// Form styling
:deep(.el-form) {
  .el-form-item {
    margin-bottom: 20px;
    
    &.schedule-form-item {
      margin-bottom: 24px;
    }
    
    // Fix reason field validation overlap
    &.reason-form-item {
      margin-bottom: 40px; // Extra space for validation message to prevent overlap with remark field
    }
    
    &:last-child {
      margin-bottom: 0;
    }
  }
  
  .el-form-item__label {
    font-weight: 500;
    color: var(--el-text-color-regular);
    line-height: 32px;
  }
  
  .el-form-item__error {
    padding-top: 6px;
    line-height: 1.4;
    position: relative;
    z-index: 1;
  }
}

// Select and input styling
:deep(.el-select),
:deep(.el-input),
:deep(.el-textarea) {
  .el-input__wrapper {
    box-shadow: 0 0 0 1px var(--el-border-color) inset;
    transition: all 0.2s;
    
    &:hover {
      box-shadow: 0 0 0 1px var(--el-border-color-hover) inset;
    }
    
    &.is-focus {
      box-shadow: 0 0 0 1px var(--el-color-primary) inset;
    }
  }
}

:deep(.el-select-dropdown__item) {
  padding: 12px;
  
  &:hover {
    background-color: var(--el-fill-color-light);
  }
  
  &.selected {
    font-weight: 600;
  }
}

// Alert styling
:deep(.el-alert) {
  border-radius: 8px;
  margin-top: 16px;
  
  .el-alert__title {
    line-height: 1.5;
  }
}

// Radio group styling
:deep(.el-radio-group) {
  display: flex;
  gap: 16px;
  
  .el-radio {
    margin-right: 0;
    
    .el-radio__label {
      padding-left: 8px;
    }
  }
}

// Button styling
:deep(.el-button) {
  border-radius: 6px;
}

// Improve select option display
:deep(.el-select__popper) {
  .el-select-dropdown__item {
    padding: 8px 12px;
    min-height: auto;
    height: auto;
    line-height: normal;
    
    &.schedule-option {
      padding: 12px;
    }
    
    > div {
      width: 100%;
    }
  }
}

// Schedule option styling
.schedule-option-wrapper {
  .time-duration {
    color: var(--el-color-success);
    font-size: 11px;
    font-weight: 500;
    margin-left: 6px;
  }
  
  .option-detail-row {
    &:not(:last-child) {
      margin-bottom: 4px;
    }
    
    .detail-text {
      strong {
        color: var(--el-color-primary);
      }
    }
  }
}

// Scrollbar styling
:deep(.el-dialog__body) {
  &::-webkit-scrollbar {
    width: 6px;
  }
  
  &::-webkit-scrollbar-track {
    background: var(--el-fill-color-lighter);
    border-radius: 3px;
  }
  
  &::-webkit-scrollbar-thumb {
    background: var(--el-border-color-darker);
    border-radius: 3px;
    
    &:hover {
      background: var(--el-text-color-secondary);
    }
  }
}
</style>
