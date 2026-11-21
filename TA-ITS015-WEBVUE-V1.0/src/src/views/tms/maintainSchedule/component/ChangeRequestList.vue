<template>
  <el-dialog
    v-model="state.visible"
    :title="$t('lz.label.tmsDutySchedule.changeRequestManagement')"
    width="80%"
    top="5vh"
    draggable
    :close-on-click-modal="false"
  >
    <template #header>
      <div style="color: #fff; display: flex; align-items: center; justify-content: space-between;">
        <div style="display: flex; align-items: center;">
          <el-icon size="16" style="margin-right: 8px;">
            <ele-List />
          </el-icon>
          <span>{{ $t('lz.label.tmsDutySchedule.changeRequestManagement') }}</span>
        </div>
        <el-badge
          v-if="state.pendingCount > 0"
          :value="state.pendingCount"
          type="danger"
          style="margin-right: 20px;"
        >
          <el-tag type="warning" effect="dark">{{ $t('lz.label.tmsDutySchedule.pendingApproval') }}</el-tag>
        </el-badge>
      </div>
    </template>

    <el-tabs v-model="state.activeTab" @tab-change="handleTabChange">
      <!-- Tab: My Requests -->
      <el-tab-pane name="myRequests">
        <template #label>
          <span style="display: flex; align-items: center; gap: 6px;">
            <el-icon><ele-User /></el-icon>
            <span>{{ $t('lz.label.tmsDutySchedule.myRequests') }}</span>
            <el-badge
              v-if="state.myRequestsCount > 0"
              :value="state.myRequestsCount"
              type="primary"
            />
          </span>
        </template>
      </el-tab-pane>

      <!-- Tab: Pending Approval -->
      <el-tab-pane name="pendingApproval">
        <template #label>
          <span style="display: flex; align-items: center; gap: 6px;">
            <el-icon><ele-Clock /></el-icon>
            <span>{{ $t('lz.label.tmsDutySchedule.awaitingApproval') }}</span>
            <el-badge
              v-if="state.pendingCount > 0"
              :value="state.pendingCount"
              type="danger"
            />
          </span>
        </template>
      </el-tab-pane>

      <!-- Tab: All -->
      <el-tab-pane name="all">
        <template #label>
          <span style="display: flex; align-items: center; gap: 6px;">
            <el-icon><ele-Menu /></el-icon>
            <span>{{ $t('lz.label.base.all') }}</span>
          </span>
        </template>
      </el-tab-pane>

      <!-- Tab: History -->
      <el-tab-pane name="history">
        <template #label>
          <span style="display: flex; align-items: center; gap: 6px;">
            <el-icon><ele-DocumentCopy /></el-icon>
            <span>{{ $t('lz.label.tmsDutySchedule.history') }}</span>
          </span>
        </template>
      </el-tab-pane>
    </el-tabs>

    <!-- Info Alert for Pending Approval Tab -->
    <el-alert
      v-if="state.activeTab === 'pendingApproval' && state.tableData.length === 0 && !state.loading"
      type="info"
      :closable="false"
      show-icon
      style="margin-bottom: 16px;"
    >
      <template #title>
        <span style="font-size: 13px;">
          <strong>{{ $t('lz.label.tmsDutySchedule.noPendingRequests') }}</strong><br/>
          {{ $t('lz.message.tmsDutySchedule.pendingTabInfo') }}
        </span>
      </template>
    </el-alert>

    <!-- Filter for Change Requests tabs -->
    <div class="filter-section" v-if="state.activeTab !== 'history'">
      <el-form :inline="true" class="filter-form">
        <el-form-item :label="$t('lz.label.tmsDutySchedule.time')">
          <el-date-picker
            v-model="state.dateRange"
            type="daterange"
            range-separator="-"
            :start-placeholder="$t('lz.label.base.fromDate')"
            :end-placeholder="$t('lz.label.tmsDutySchedule.toDate')"
            format="DD/MM/YYYY"
            value-format="YYYY-MM-DD"
            style="width: 240px;"
            clearable
            @change="handleQuery"
          />
        </el-form-item>
        <el-form-item :label="$t('lz.label.tmsDutySchedule.requester')" v-if="state.activeTab !== 'myRequests'">
          <el-select
            v-model="state.queryParams.requesterId"
            :placeholder="$t('lz.label.base.all')"
            style="width: 180px;"
            @change="handleQuery"
            clearable
            filterable
          >
            <el-option
              v-for="emp in state.employeeList"
              :key="emp.username"
              :label="`${emp.name} (${emp.username})`"
              :value="emp.username"
            />
          </el-select>
        </el-form-item>
        <el-form-item :label="$t('lz.entity.base.status')">
          <el-select
            v-model="state.queryParams.status"
            :placeholder="$t('lz.label.base.all')"
            style="width: 150px;"
            @change="handleQuery"
            clearable
          >
            <el-option 
              v-for="item in getConfigDataByCode(BaseConfigTypeEnum.TmsDutyRequestState)" 
              :key="item.code" 
              :label="item.name" 
              :value="item.code" 
            />
          </el-select>
        </el-form-item>
        <el-form-item :label="$t('lz.entity.base.type')">
          <el-select
            v-model="state.queryParams.type"
            :placeholder="$t('lz.label.base.all')"
            style="width: 150px;"
            @change="handleQuery"
            clearable
          >
            <el-option 
              v-for="item in getConfigDataByCode(BaseConfigTypeEnum.TmsDutyRequestType)" 
              :key="item.code" 
              :label="item.name" 
              :value="item.code" 
            />
          </el-select>
        </el-form-item>
        <el-form-item class="mt3">
          <el-button type="primary" @click="handleQuery">
            <el-icon><ele-Search /></el-icon>
            {{ $t('lz.button.base.search') }}
          </el-button>
          <el-button @click="handleResetFilter">
            <el-icon><ele-RefreshLeft /></el-icon>
            {{ $t('lz.button.base.reset') }}
          </el-button>
        </el-form-item>
      </el-form>
    </div>

    <!-- Filter for History tab -->
    <div class="filter-section" v-if="state.activeTab === 'history'">
      <el-form :inline="true" class="filter-form">
        <el-form-item :label="$t('lz.label.tmsDutySchedule.time')">
          <el-date-picker
            v-model="state.historyDateRange"
            type="daterange"
            range-separator="-"
            :start-placeholder="$t('lz.label.base.fromDate')"
            :end-placeholder="$t('lz.label.tmsDutySchedule.toDate')"
            format="DD/MM/YYYY"
            value-format="YYYY-MM-DDTHH:mm:ss"
            style="width: 260px;"
            clearable
            @change="handleQueryHistory"
          />
        </el-form-item>
        <el-form-item :label="$t('lz.label.tmsDutySchedule.changeType')">
          <el-select
            v-model="state.historyQueryParams.actionType"
            :placeholder="$t('lz.label.base.all')"
            style="width: 160px;"
            @change="handleQueryHistory"
            clearable
          >
            <el-option 
              v-for="item in getConfigDataByCode(BaseConfigTypeEnum.TmsDutyAction)" 
              :key="item.code" 
              :label="item.name" 
              :value="item.code" 
            />
          </el-select>
        </el-form-item>
        <el-form-item :label="$t('lz.label.tmsDutySchedule.performer')">
          <el-select
            v-model="state.historyQueryParams.actionBy"
            :placeholder="$t('lz.button.base.selectAll')"
            style="width: 180px;"
            @change="handleQueryHistory"
            clearable
            filterable
          >
            <el-option
              v-for="emp in state.employeeList"
              :key="emp.username"
              :label="`${emp.name} (${emp.username})`"
              :value="emp.username"
            />
          </el-select>
        </el-form-item>
        <el-form-item class="mt3">
          <el-button type="primary" @click="handleQueryHistory">
            <el-icon><ele-Search /></el-icon>
           {{ $t('lz.button.base.search') }}
          </el-button>
          <el-button @click="handleResetHistoryFilter">
            <el-icon><ele-RefreshLeft /></el-icon>
            {{ $t('lz.button.base.reset') }}
          </el-button>
        </el-form-item>
      </el-form>
    </div>

    <!-- Table for Change Requests -->
    <el-table
      v-if="state.activeTab !== 'history'"
      v-loading="state.loading"
      :data="state.tableData"
      stripe
      border
      style="width: 100%;"
      :max-height="400"
    >
      <el-table-column type="index" :label="$t('lz.label.base.idx')" width="60" align="center" />
      
      <el-table-column :label="$t('lz.label.tmsDutySchedule.dutySchedule')" min-width="200">
        <template #default="{ row }">
          <div class="schedule-cell">
            <div class="schedule-name">
              <el-icon class="cell-icon"><ele-Calendar /></el-icon>
              {{ row.dutySchedule?.name || 'N/A' }}
            </div>
            <div class="schedule-time">
              <el-icon class="cell-icon"><ele-Clock /></el-icon>
              {{ formatDateTime(row.dutySchedule?.startTime) }}
            </div>
          </div>
        </template>
      </el-table-column>

      <el-table-column :label="$t('lz.entity.base.type')" width="120" align="center">
        <template #default="{ row }">
          <TagInfo :config-item="getConfigItemByCodeForType(row.type)" />
        </template>
      </el-table-column>

      <el-table-column :label="$t('lz.label.tmsDutySchedule.requester')" width="150">
        <template #default="{ row }">
          <div style="display: flex; align-items: center; gap: 6px;">
            <el-icon><ele-User /></el-icon>
            <span>{{ row.requesterName || row.requesterId || 'N/A' }}</span>
          </div>
        </template>
      </el-table-column>

      <el-table-column :label="$t('lz.label.tmsDutySchedule.targetPerson')" width="150">
        <template #default="{ row }">
          <div v-if="row.targetUserId" style="display: flex; align-items: center; gap: 6px;">
            <el-icon><ele-User /></el-icon>
            <el-tooltip :content="row.targetUserName || $t('lz.label.tmsDutySchedule.noName')" placement="top">
              <span style="cursor: help;">{{ row.targetUserId }}</span>
            </el-tooltip>
          </div>
          <span v-else style="color: var(--el-text-color-placeholder);">—</span>
        </template>
      </el-table-column>

      <el-table-column :label="$t('lz.label.tmsDutySchedule.reason')" min-width="180">
        <template #default="{ row }">
          <el-tooltip :content="row.reason" placement="top" :disabled="!row.reason || row.reason.length < 50">
            <div class="reason-cell">
              {{ row.reason || $t('lz.label.tmsDutySchedule.none') }}
            </div>
          </el-tooltip>
        </template>
      </el-table-column>

      <el-table-column :label="$t('lz.entity.base.status')" width="120" align="center">
        <template #default="{ row }">
          <TagInfo :config-item="getConfigItemByCodeForStatus(row.status)" />
        </template>
      </el-table-column>

      <el-table-column :label="$t('lz.label.base.createDate')" width="150" align="center">
        <template #default="{ row }">
          {{ formatDateTime(row.createTime) }}
        </template>
      </el-table-column>

      <el-table-column :label="$t('lz.label.base.action')" width="250" align="center" fixed="right">
        <template #default="{ row }">
          <div class="action-buttons">
            <!-- View Detail -->
            <el-button
              type="primary"
              size="small"
              link
              @click="handleViewDetail(row)"
            >
              <el-icon><ele-View /></el-icon>
              {{ $t('lz.button.base.detail') }}
            </el-button>

            <!-- Approve (for pending approval tab) -->
            <template v-if="state.activeTab === 'pendingApproval' && row.status === 'pending'">
              <el-button
                type="success"
                size="small"
                link
                @click="handleApprove(row)"
              >
                <el-icon><ele-Check /></el-icon>
                {{ $t('lz.button.base.approve') }}
              </el-button>

              <!-- Reject (for pending approval tab) -->
              <el-button
                type="danger"
                size="small"
                link
                @click="handleReject(row)"
              >
                <el-icon><ele-Close /></el-icon>
                {{ $t('lz.button.base.reject') }}
              </el-button>
            </template>

            <!-- Cancel (for my requests tab) -->
            <el-button
              v-if="state.activeTab === 'myRequests' && row.status === 'pending'"
              type="warning"
              size="small"
              link
              @click="handleCancel(row)"
            >
              <el-icon><ele-CircleClose /></el-icon>
              {{ $t('lz.button.base.cancel') }}
            </el-button>
          </div>
        </template>
      </el-table-column>
    </el-table>

    <!-- Table for History -->
    <el-table
      v-if="state.activeTab === 'history'"
      v-loading="state.historyLoading"
      :data="state.historyData"
      stripe
      border
      style="width: 100%;"
      :max-height="400"
    >
      <el-table-column type="index" :label="$t('lz.label.base.idx')" width="60" align="center" />
      
      <el-table-column :label="$t('lz.label.tmsDutySchedule.time')" width="160">
        <template #default="{ row }">
          <div style="display: flex; align-items: center; gap: 6px;">
            <el-icon size="14" color="var(--el-color-primary)"><ele-Clock /></el-icon>
            <span>{{ formatDateTime(row.createTime) }}</span>
          </div>
        </template>
      </el-table-column>

      <el-table-column :label="$t('lz.label.tmsDutySchedule.changeType')" width="140" align="center">
        <template #default="{ row }">
          <TagInfo :config-item="getConfigItemForAction(row.actionType)" />
        </template>
      </el-table-column>

      <el-table-column :label="$t('lz.label.tmsDutySchedule.actionBy')" width="150">
        <template #default="{ row }">
          <div style="display: flex; align-items: center; gap: 6px;">
            <el-icon><ele-User /></el-icon>
            <span>{{ getEmployeeName(row.actionBy) }}</span>
          </div>
        </template>
      </el-table-column>

      <el-table-column :label="$t('lz.label.tmsDutySchedule.affectedPerson')" width="150">
        <template #default="{ row }">
          <template v-if="row.affectedUserId">
            <div style="display: flex; align-items: center; gap: 6px;">
              <el-icon><ele-UserFilled /></el-icon>
              <span>{{ getEmployeeName(row.affectedUserId) }}</span>
            </div>
          </template>
          <span v-else style="color: var(--el-text-color-placeholder);">—</span>
        </template>
      </el-table-column>

      <el-table-column :label="$t('lz.entity.base.desc')" min-width="200">
        <template #default="{ row }">
          <div class="description-cell">
            <span>{{ row.description || '—' }}</span>
          </div>
        </template>
      </el-table-column>

      <el-table-column :label="$t('lz.label.tmsDutySchedule.reason')" min-width="150">
        <template #default="{ row }">
          <div class="reason-cell">
            <span>{{ row.reason || '—' }}</span>
          </div>
        </template>
      </el-table-column>
    </el-table>

    <!-- Pagination for Change Requests -->
    <div class="pagination-section" v-if="state.activeTab !== 'history' && state.totalCount > 0">
      <el-pagination
        v-model:current-page="state.pagination.pageIndex"
        v-model:page-size="state.pagination.pageSize"
        :page-sizes="[10, 20, 50, 100]"
        :total="state.totalCount"
        layout="total, sizes, prev, pager, next, jumper"
        @size-change="handleSizeChange"
        @current-change="handleCurrentChange"
      />
    </div>

    <!-- Pagination for History -->
    <div class="pagination-section" v-if="state.activeTab === 'history' && state.historyTotalCount > 0">
      <el-pagination
        v-model:current-page="state.historyPagination.pageIndex"
        v-model:page-size="state.historyPagination.pageSize"
        :page-sizes="[10, 20, 50, 100]"
        :total="state.historyTotalCount"
        layout="total, sizes, prev, pager, next, jumper"
        @size-change="handleHistorySizeChange"
        @current-change="handleHistoryCurrentChange"
      />
    </div>

    <!-- Detail Dialog -->
    <el-dialog
      v-model="state.detailVisible"
      :title="$t('lz.label.tmsDutySchedule.requestDetail')"
      width="600px"
      append-to-body
      class="detail-dialog"
    >
      <template #header>
        <div style="display: flex; align-items: center; gap: 10px;color:#fff">
          <el-icon size="20" color="white"><ele-Document /></el-icon>
          {{ $t('lz.label.tmsDutySchedule.changeRequestDetail') }}
          <TagInfo 
            v-if="state.selectedRequest"
            :config-item="getConfigItemByCodeForStatus(state.selectedRequest.status)" 
          />
        </div>
      </template>

      <div v-if="state.selectedRequest" class="detail-content-compact">
        <!-- Row 1: Thông tin lịch trực và loại yêu cầu -->
        <div class="detail-row">
          <div class="detail-card">
            <div class="card-icon"><el-icon><ele-Calendar /></el-icon></div>
            <div class="card-content">
              <span class="card-label">{{ $t('lz.label.tmsDutySchedule.dutySchedule') }}</span>
              <div class="card-value">
                {{ state.selectedRequest.dutySchedule?.name || 'N/A' }}
                <TagInfo
                  v-if="state.selectedRequest.dutySchedule?.status"
                  :config-item="getConfigItemByCode(BaseConfigTypeEnum.TmsDutyState, state.selectedRequest.dutySchedule.status || '')"
                  style="margin-left: 6px;"
                />
              </div>
              <span class="card-sub">
                <el-icon size="12"><ele-Clock /></el-icon>
                {{ formatDateTime(state.selectedRequest.dutySchedule?.startTime) }} - {{ formatDateTime(state.selectedRequest.dutySchedule?.endTime) }}
              </span>
            </div>
          </div>
          <div class="detail-card type-card">
            <div class="card-icon"><el-icon><ele-Tickets /></el-icon></div>
            <div class="card-content">
              <span class="card-label">{{ $t('lz.label.tmsDutySchedule.requestType') }}</span>
              <div class="card-value">
                <TagInfo :config-item="getConfigItemByCodeForType(state.selectedRequest.type)" />
              </div>
            </div>
          </div>
        </div>

        <!-- Row 2: Người yêu cầu và người thay thế -->
        <div class="detail-row persons-row">
          <div class="person-card requester">
            <div class="person-header">
              <el-icon><ele-User /></el-icon>
              <span>{{ $t('lz.label.tmsDutySchedule.requester') }}</span>
            </div>
            <div class="person-info">
              <div class="person-name">{{ state.selectedRequest.requesterName || state.selectedRequest.requesterId || 'N/A' }}</div>
              <div class="person-tags">
                <TagInfo v-if="getRequesterRole()" :config-item="getConfigItemByCode(BaseConfigTypeEnum.TmsDutyPatronRole, (getRequesterRole() || '').toLowerCase())" />
                <TagInfo v-if="getRequesterDetailStatus()" :config-item="getConfigItemByCode(BaseConfigTypeEnum.TmsDutyPatronState, getRequesterDetailStatus() || '')" />
              </div>
            </div>
          </div>
          
          <div class="arrow-icon" v-if="state.selectedRequest.targetUserId">
            <el-icon size="20"><ele-Right /></el-icon>
          </div>
          
          <div class="person-card target" v-if="state.selectedRequest.targetUserId">
            <div class="person-header">
              <el-icon><ele-UserFilled /></el-icon>
              <span>{{ $t('lz.label.tmsDutySchedule.targetPerson') }}</span>
            </div>
            <div class="person-info">
              <div class="person-name">{{ state.selectedRequest.targetUserName || state.selectedRequest.targetUserId || 'N/A' }}</div>
              <div class="person-tags">
                <TagInfo v-if="getTargetRole()" :config-item="getConfigItemByCode(BaseConfigTypeEnum.TmsDutyPatronRole, (getTargetRole() || '').toLowerCase())" />
                <TagInfo v-if="getTargetDetailStatus()" :config-item="getConfigItemByCode(BaseConfigTypeEnum.TmsDutyPatronState, getTargetDetailStatus() || '')" />
              </div>
            </div>
          </div>
        </div>

        <!-- Row 3: Lý do -->
        <div class="detail-row reason-row">
          <div class="reason-card">
            <div class="reason-header">
              <el-icon><ele-EditPen /></el-icon>
              <span>{{ $t('lz.label.tmsDutySchedule.reason') }}</span>
              <span class="create-time">{{ formatDateTime(state.selectedRequest.createTime) }}</span>
            </div>
            <div class="reason-content">{{ state.selectedRequest.reason || $t('lz.label.tmsDutySchedule.none') }}</div>
            <div class="reason-remark" v-if="state.selectedRequest.remark">
              <strong>{{ $t('lz.entity.base.remark') }}:</strong> {{ state.selectedRequest.remark }}
            </div>
          </div>
        </div>

        <!-- Row 4: Phê duyệt (nếu có) -->
        <div class="detail-row approval-row" v-if="state.selectedRequest.approverId || state.selectedRequest.approvedTime">
          <div class="approval-card" :class="{ 'approved': state.selectedRequest.status === 'Approved', 'rejected': state.selectedRequest.status === 'Rejected' }">
            <div class="approval-header">
              <el-icon><ele-CircleCheck /></el-icon>
              <span>{{ $t('lz.label.tmsDutySchedule.approvedBy') }} {{ state.selectedRequest.approverId || '—' }}</span>
              <span class="approval-time" v-if="state.selectedRequest.approvedTime">{{ formatDateTime(state.selectedRequest.approvedTime) }}</span>
            </div>
            <div class="approval-note" v-if="state.selectedRequest.approvalNote">{{ state.selectedRequest.approvalNote }}</div>
          </div>
        </div>
      </div>
    </el-dialog>

    <!-- Approval Dialog -->
    <el-dialog
      v-model="state.approvalVisible"
      :title="state.approvalAction === 'approve' ? $t('lz.label.tmsDutySchedule.approveRequest') : $t('lz.label.tmsDutySchedule.rejectRequest')"
      width="500px"
      append-to-body
    >
      <el-form :model="state.approvalForm" label-width="120px">
        <el-form-item :label="$t('lz.entity.base.remark')">
          <el-input
            v-model="state.approvalForm.approvalNote"
            type="textarea"
            :rows="4"
            :placeholder="state.approvalAction === 'approve' ? $t('lz.label.tmsDutySchedule.approvalNotePlaceholder') : $t('lz.label.tmsDutySchedule.rejectReasonPlaceholder')"
          />
        </el-form-item>
      </el-form>

      <template #footer>
        <el-button @click="state.approvalVisible = false">{{ $t('lz.button.base.cancel') }}</el-button>
        <el-button
          :type="state.approvalAction === 'approve' ? 'success' : 'danger'"
          :loading="state.approvalLoading"
          @click="handleConfirmApproval"
        >
          {{ state.approvalAction === 'approve' ? $t('lz.button.base.approve') : $t('lz.button.base.reject') }}
        </el-button>
      </template>
    </el-dialog>
  </el-dialog>
</template>

<script lang="ts" setup name="ChangeRequestList">
import { reactive, watch } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { getAPI } from '/@/utils/axios-utils';
import { TmsDutyScheduleApi } from '/@/api-services/tms';
import { formatDate } from '/@/utils/formatTime';
import { BaseConfigTypeEnum } from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';
import { defineAsyncComponent } from 'vue';
import { useI18n } from 'vue-i18n';

const { t: $t } = useI18n();
const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));

const props = defineProps<{
  employeeList?: any[];
}>();

const emit = defineEmits(['refresh']);
const getConfigItemByCode = useBaseConfig().getConfigItemByCode;
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;

/**
 * Get default date range (last 7 days)
 * fromDate: 00:00:00, toDate: 23:59:59
 */
const getDefaultDateRange = (): string[] => {
  const today = new Date();
  const sevenDaysAgo = new Date();
  sevenDaysAgo.setDate(today.getDate() - 7);
  
  const formatDateStr = (date: Date, isEndOfDay: boolean = false) => {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    const time = isEndOfDay ? '23:59:59' : '00:00:00';
    return `${year}-${month}-${day}T${time}`;
  };
  
  return [formatDateStr(sevenDaysAgo, false), formatDateStr(today, true)];
};

const state = reactive({
  visible: false,
  loading: false,
  activeTab: 'myRequests' as 'myRequests' | 'pendingApproval' | 'all' | 'history',
  tableData: [] as any[],
  allData: [] as any[], // All data before pagination
  queryParams: {
    viewMode: 1, // 0: All, 1: MyRequests, 2: PendingApproval
    status: '',
    type: '',
    requesterId: ''
  },
  dateRange: getDefaultDateRange(),
  pagination: {
    pageIndex: 1,
    pageSize: 10
  },
  totalCount: 0,
  employeeList: [] as any[],
  myRequestsCount: 0,
  pendingCount: 0,
  detailVisible: false,
  selectedRequest: null as any,
  approvalVisible: false,
  approvalAction: 'approve' as 'approve' | 'reject',
  approvalLoading: false,
  approvalForm: {
    changeRequestId: '',
    status: '',
    approvalNote: ''
  },
  // History tab states
  historyData: [] as any[],
  historyAllData: [] as any[],
  historyLoading: false,
  historyDateRange: getDefaultDateRange(),
  historyQueryParams: {
    actionType: '',
    actionBy: ''
  },
  historyPagination: {
    pageIndex: 1,
    pageSize: 10
  },
  historyTotalCount: 0
});

// Watch for employeeList changes from parent
watch(() => props.employeeList, (newList) => {
  if (newList) {
    state.employeeList = newList;
  }
}, { immediate: true });

const formatDateTime = (dateTime: any) => {
  if (!dateTime) return '';
  return formatDate(new Date(dateTime), 'dd/mm/YYYY HH:MM');
};

/**
 * Get employee name by username
 */
const getEmployeeName = (username: string) => {
  if (!username) return '';
  const emp = state.employeeList.find((e: any) => e.username === username);
  return emp?.name || username;
};

const getTypeLabel = (type: string) => {
  const labels: any = {
    'Absent': $t('lz.label.tmsDutySchedule.actionAbsent'),
    'Swap': $t('lz.label.tmsDutySchedule.actionSwap'),
    'Replace': $t('lz.label.tmsDutySchedule.actionReplace')
  };
  return labels[type] || type;
};

const getTypeTagType = (type: string) => {
  const types: any = {
    'Absent': 'warning',
    'Swap': 'primary',
    'Replace': 'success'
  };
  return types[type] || 'info';
};

// Helper to get config item for change request type
const getConfigItemByCodeForType = (type: string) => {
  // Try from config first
  const configItem = getConfigItemByCode(BaseConfigTypeEnum.TmsDutyRequestType, type?.toLowerCase());
  if (configItem?.name) return configItem;
  
  // Fallback to localized values if config not available
  const typeConfigs: any = {
    'Absent': { code: 'absent', name: $t('lz.label.tmsDutySchedule.actionAbsent'), tagType: 'warning' },
    'Swap': { code: 'swap', name: $t('lz.label.tmsDutySchedule.actionSwap'), tagType: 'primary' },
    'Replace': { code: 'replace', name: $t('lz.label.tmsDutySchedule.actionReplace'), tagType: 'success' }
  };
  return typeConfigs[type] || { code: type, name: type, tagType: 'info' };
};

const getStatusLabel = (status: string) => {
  const configItem = getConfigItemByCode(BaseConfigTypeEnum.TmsDutyRequestState, status?.toLowerCase());
  if (configItem?.name) return configItem.name;
  
  const labels: any = {
    'Pending': $t('lz.label.tmsDutySchedule.statusPending'),
    'Approved': $t('lz.label.tmsDutySchedule.statusApproved'),
    'Rejected': $t('lz.label.tmsDutySchedule.statusRejected'),
    'Cancelled': $t('lz.label.tmsDutySchedule.statusCancelled')
  };
  return labels[status] || status;
};

const getStatusTagType = (status: string) => {
  const configItem = getConfigItemByCode(BaseConfigTypeEnum.TmsDutyRequestState, status?.toLowerCase());
  if (configItem?.tagType) return configItem.tagType;
  
  const types: any = {
    'Pending': 'warning',
    'Approved': 'success',
    'Rejected': 'danger',
    'Cancelled': 'info'
  };
  return types[status] || 'info';
};

// Helper to get config item for change request status
const getConfigItemByCodeForStatus = (status: string) => {
  // Try from config first
  const configItem = getConfigItemByCode(BaseConfigTypeEnum.TmsDutyRequestState, status?.toLowerCase());
  if (configItem?.name) return configItem;
  
  // Fallback to localized values if config not available
  const statusConfigs: any = {
    'Pending': { code: 'pending', name: $t('lz.label.tmsDutySchedule.statusPending'), tagType: 'warning' },
    'Approved': { code: 'approved', name: $t('lz.label.tmsDutySchedule.statusApproved'), tagType: 'success' },
    'Rejected': { code: 'rejected', name: $t('lz.label.tmsDutySchedule.statusRejected'), tagType: 'danger' },
    'Cancelled': { code: 'cancelled', name: $t('lz.label.tmsDutySchedule.statusCancelled'), tagType: 'info' }
  };
  return statusConfigs[status] || { code: status, name: status, tagType: 'info' };
};

// Helper functions for duty schedule detail status
const getDetailStatusLabel = (status: string) => {
  // Try duty state config
  const dutyStateConfig = getConfigItemByCode(BaseConfigTypeEnum.TmsDutyState, status);
  if (dutyStateConfig?.name) return dutyStateConfig.name;
  
  // Try patron state config
  const patronStateConfig = getConfigItemByCode(BaseConfigTypeEnum.TmsDutyPatronState, status);
  if (patronStateConfig?.name) return patronStateConfig.name;
  
  return status || $t('lz.label.base.unknown');
};

const getDetailStatusTagType = (status: string) => {
  // Try duty state config
  const dutyStateConfig = getConfigItemByCode(BaseConfigTypeEnum.TmsDutyState, status);
  if (dutyStateConfig?.tagType) return dutyStateConfig.tagType;
  
  // Try patron state config  
  const patronStateConfig = getConfigItemByCode(BaseConfigTypeEnum.TmsDutyPatronState, status);
  if (patronStateConfig?.tagType) return patronStateConfig.tagType;
  
  return 'info';
};

// Get requester detail info from duty schedule
const getRequesterDetailStatus = () => {
  if (!state.selectedRequest?.dutySchedule?.details || !state.selectedRequest?.requesterId) return null;
  const detail = state.selectedRequest.dutySchedule.details.find(
    (d: any) => d.userId === state.selectedRequest.requesterId
  );
  return detail?.status || null;
};

const getRequesterRole = () => {
  if (!state.selectedRequest?.dutySchedule?.details || !state.selectedRequest?.requesterId) return null;
  const detail = state.selectedRequest.dutySchedule.details.find(
    (d: any) => d.userId === state.selectedRequest.requesterId
  );
  return detail?.role || null;
};

const getRequesterRemark = () => {
  if (!state.selectedRequest?.dutySchedule?.details || !state.selectedRequest?.requesterId) return null;
  const detail = state.selectedRequest.dutySchedule.details.find(
    (d: any) => d.userId === state.selectedRequest.requesterId
  );
  return detail?.remark || null;
};

// Get target user detail info from duty schedule
const getTargetDetailStatus = () => {
  if (!state.selectedRequest?.dutySchedule?.details || !state.selectedRequest?.targetUserId) return null;
  const detail = state.selectedRequest.dutySchedule.details.find(
    (d: any) => d.userId === state.selectedRequest.targetUserId
  );
  return detail?.status || null;
};

const getTargetRole = () => {
  if (!state.selectedRequest?.dutySchedule?.details || !state.selectedRequest?.targetUserId) return null;
  const detail = state.selectedRequest.dutySchedule.details.find(
    (d: any) => d.userId === state.selectedRequest.targetUserId
  );
  return detail?.role || null;
};

const getTargetRemark = () => {
  if (!state.selectedRequest?.dutySchedule?.details || !state.selectedRequest?.targetUserId) return null;
  const detail = state.selectedRequest.dutySchedule.details.find(
    (d: any) => d.userId === state.selectedRequest.targetUserId
  );
  return detail?.remark || null;
};

const handleTabChange = (tabName: string) => {
  state.activeTab = tabName as any;
  
  if (tabName === 'history') {
    // Load history data
    state.historyPagination.pageIndex = 1;
    handleQueryHistory();
    return;
  }
  
  // Update viewMode
  if (tabName === 'myRequests') {
    state.queryParams.viewMode = 1;
    state.queryParams.requesterId = ''; // Reset requester filter for my requests
  } else if (tabName === 'pendingApproval') {
    state.queryParams.viewMode = 2;
  } else {
    state.queryParams.viewMode = 0;
  }
  
  // Reset pagination
  state.pagination.pageIndex = 1;
  handleQuery();
};

const handleQuery = async () => {
  try {
    state.loading = true;

    const queryData: any = {
      viewMode: state.queryParams.viewMode
    };

    if (state.queryParams.status) {
      queryData.status = state.queryParams.status;
    }

    if (state.queryParams.type) {
      queryData.type = state.queryParams.type;
    }

    if (state.queryParams.requesterId) {
      queryData.requesterId = state.queryParams.requesterId;
    }

    // Add date range filter
    if (state.dateRange && state.dateRange.length === 2) {
      queryData.fromDate = state.dateRange[0];
      queryData.toDate = state.dateRange[1];
    }

    console.log('ChangeRequestList - handleQuery queryData:', queryData);
    const response = await getAPI(TmsDutyScheduleApi).apiTmsTmsdutyscheduleGetchangerequestsPost(queryData);
    console.log('ChangeRequestList - handleQuery response:', response.data);
    console.log('ChangeRequestList - handleQuery result count:', response.data?.result?.length || 0);
    
    // Store all data and apply pagination
    state.allData = response.data?.result || [];
    state.totalCount = state.allData.length;
    applyPagination();
    
    console.log('ChangeRequestList - handleQuery tableData:', state.tableData.length, 'rows');
    if (state.tableData.length > 0) {
      console.log('ChangeRequestList - handleQuery first row sample:', {
        id: state.tableData[0].id,
        type: state.tableData[0].type,
        status: state.tableData[0].status,
        requesterId: state.tableData[0].requesterId,
        requesterName: state.tableData[0].requesterName
      });
    }

    // Update counts
    loadCounts();
  } catch (error: any) {
    console.error('Error loading change requests:', error);
    ElMessage.error($t('lz.message.tmsDutySchedule.cannotLoadRequestList'));
  } finally {
    state.loading = false;
  }
};

/**
 * Apply pagination to the data
 */
const applyPagination = () => {
  const start = (state.pagination.pageIndex - 1) * state.pagination.pageSize;
  const end = start + state.pagination.pageSize;
  state.tableData = state.allData.slice(start, end);
};

/**
 * Handle page size change
 */
const handleSizeChange = (size: number) => {
  state.pagination.pageSize = size;
  state.pagination.pageIndex = 1;
  applyPagination();
};

/**
 * Handle page change
 */
const handleCurrentChange = (page: number) => {
  state.pagination.pageIndex = page;
  applyPagination();
};

/**
 * Reset filter and refresh data
 */
const handleResetFilter = () => {
  state.queryParams.status = '';
  state.queryParams.type = '';
  state.queryParams.requesterId = '';
  state.dateRange = [];
  state.pagination.pageIndex = 1;
  handleQuery();
};

// ===== History Tab Functions =====

/**
 * Query history data
 */
const handleQueryHistory = async () => {
  try {
    state.historyLoading = true;
    
    const queryData: any = {};
    
    // Add date range filter
    if (state.historyDateRange && state.historyDateRange.length === 2) {
      queryData.fromDate = new Date(state.historyDateRange[0]);
      queryData.toDate = new Date(state.historyDateRange[1]);
    }
    
    // Add action type filter
    if (state.historyQueryParams.actionType) {
      queryData.actionType = state.historyQueryParams.actionType;
    }
    
    // Add action by filter
    if (state.historyQueryParams.actionBy) {
      queryData.actionBy = state.historyQueryParams.actionBy;
    }
    
    console.log('ChangeRequestList - handleQueryHistory queryData:', queryData);
    const response = await getAPI(TmsDutyScheduleApi).apiTmsTmsdutyscheduleGethistoryPost(queryData);
    console.log('ChangeRequestList - handleQueryHistory response:', response.data);
    
    // Store all data and apply pagination
    state.historyAllData = response.data?.result || [];
    state.historyTotalCount = state.historyAllData.length;
    applyHistoryPagination();
    
  } catch (error: any) {
    console.error('Error loading history:', error);
    ElMessage.error($t('lz.label.tmsDutySchedule.loadHistoryError'));
  } finally {
    state.historyLoading = false;
  }
};

/**
 * Apply pagination to history data
 */
const applyHistoryPagination = () => {
  const start = (state.historyPagination.pageIndex - 1) * state.historyPagination.pageSize;
  const end = start + state.historyPagination.pageSize;
  state.historyData = state.historyAllData.slice(start, end);
};

/**
 * Handle history page size change
 */
const handleHistorySizeChange = (size: number) => {
  state.historyPagination.pageSize = size;
  state.historyPagination.pageIndex = 1;
  applyHistoryPagination();
};

/**
 * Handle history page change
 */
const handleHistoryCurrentChange = (page: number) => {
  state.historyPagination.pageIndex = page;
  applyHistoryPagination();
};

/**
 * Reset history filter
 */
const handleResetHistoryFilter = () => {
  state.historyQueryParams.actionType = '';
  state.historyQueryParams.actionBy = '';
  state.historyDateRange = getDefaultDateRange();
  state.historyPagination.pageIndex = 1;
  handleQueryHistory();
};

/**
 * Get config item for action type (history)
 */
const getConfigItemForAction = (actionType: string) => {
  // Try from config first
  const configItem = getConfigItemByCode(BaseConfigTypeEnum.TmsDutyAction, actionType?.toLowerCase());
  if (configItem?.name) return configItem;
  
  // Fallback to localized values if config not available
  const actionConfigs: Record<string, any> = {
    'Created': { code: 'created', name: $t('lz.label.tmsDutySchedule.actionCreated'), tagType: 'success' },
    'Updated': { code: 'updated', name: $t('lz.label.tmsDutySchedule.actionUpdated'), tagType: 'primary' },
    'StatusChanged': { code: 'status_changed', name: $t('lz.label.tmsDutySchedule.actionStatusChanged'), tagType: 'warning' },
    'UserAdded': { code: 'user_added', name: $t('lz.label.tmsDutySchedule.actionUserAdded'), tagType: 'success' },
    'UserRemoved': { code: 'user_removed', name: $t('lz.label.tmsDutySchedule.actionUserRemoved'), tagType: 'danger' },
    'RoleChanged': { code: 'role_changed', name: $t('lz.label.tmsDutySchedule.actionRoleChanged'), tagType: 'warning' },
    'Approved': { code: 'approved', name: $t('lz.label.tmsDutySchedule.actionApproved'), tagType: 'success' },
    'Rejected': { code: 'rejected', name: $t('lz.label.tmsDutySchedule.actionRejected'), tagType: 'danger' },
    'Cancelled': { code: 'cancelled', name: $t('lz.label.tmsDutySchedule.actionCancelled'), tagType: 'info' },
    'Absent': { code: 'absent', name: $t('lz.label.tmsDutySchedule.actionAbsent'), tagType: 'warning' },
    'Swap': { code: 'swap', name: $t('lz.label.tmsDutySchedule.actionSwap'), tagType: 'primary' },
    'Replace': { code: 'replace', name: $t('lz.label.tmsDutySchedule.actionReplace'), tagType: 'success' }
  };
  return actionConfigs[actionType] || { code: actionType, name: actionType, tagType: 'info' };
};

/**
 * Get action type label
 */
const getActionTypeLabel = (actionType: string) => {
  const configItem = getConfigItemForAction(actionType);
  return configItem?.name || actionType;
};

/**
 * Get action type tag type
 */
const getActionTypeTagType = (actionType: string) => {
  const configItem = getConfigItemForAction(actionType);
  return configItem?.tagType || 'info';
};

// ===== End History Tab Functions =====

const loadCounts = async () => {
  try {
    // Load my requests count
    const myRequests = await getAPI(TmsDutyScheduleApi).apiTmsTmsdutyscheduleGetchangerequestsPost({ viewMode: 1 });
    state.myRequestsCount = myRequests.data?.result?.length || 0;

    // Load pending approval count
    const pendingRequests = await getAPI(TmsDutyScheduleApi).apiTmsTmsdutyscheduleGetchangerequestsPost({ 
      viewMode: 2,
      status: 'Pending'
    });
    state.pendingCount = pendingRequests.data?.result?.length || 0;
  } catch (error) {
    console.error('Error loading counts:', error);
  }
};

const handleViewDetail = (row: any) => {
  state.selectedRequest = row;
  state.detailVisible = true;
};

const handleApprove = (row: any) => {
  state.approvalAction = 'approve';
  state.approvalForm = {
    changeRequestId: row.id,
    status: 'Approved',
    approvalNote: ''
  };
  state.approvalVisible = true;
};

const handleReject = (row: any) => {
  state.approvalAction = 'reject';
  state.approvalForm = {
    changeRequestId: row.id,
    status: 'Rejected',
    approvalNote: ''
  };
  state.approvalVisible = true;
};

const handleConfirmApproval = async () => {
  try {
    state.approvalLoading = true;

    await getAPI(TmsDutyScheduleApi).apiTmsTmsdutyscheduleApprovechangerequestPost(state.approvalForm);

    ElMessage.success(
      state.approvalAction === 'approve' ? $t('lz.message.tmsDutySchedule.requestApproved') : $t('lz.message.tmsDutySchedule.requestRejected')
    );

    state.approvalVisible = false;
    handleQuery();
    emit('refresh');
  } catch (error: any) {
    console.error('Error approving/rejecting request:', error);
    ElMessage.error(error.message || $t('lz.message.base.errorOccurred'));
  } finally {
    state.approvalLoading = false;
  }
};

const handleCancel = async (row: any) => {
  try {
    await ElMessageBox.confirm(
      $t('lz.message.tmsDutySchedule.confirmCancelRequest'),
      $t('lz.message.base.confirmCancel'),
      {
        type: 'warning',
        confirmButtonText: $t('lz.button.base.cancelRequest'),
        cancelButtonText: $t('lz.button.base.no')
      }
    );

    await getAPI(TmsDutyScheduleApi).apiTmsTmsdutyscheduleCancelchangerequestPost({
      changeRequestId: row.id
    });

    ElMessage.success($t('lz.message.tmsDutySchedule.requestCancelled'));
    handleQuery();
    emit('refresh');
  } catch (error: any) {
    if (error !== 'cancel') {
      console.error('Error cancelling request:', error);
      ElMessage.error(error.message || $t('lz.message.base.errorOccurred'));
    }
  }
};

const openDialog = () => {
  state.visible = true;
  // Reset date range to default (last 7 days)
  state.dateRange = getDefaultDateRange();
  handleQuery();
};

defineExpose({
  openDialog
});
</script>

<style lang="scss" scoped>
.filter-section {
  padding-top: 10px;
  padding-left: 10px;
  background: var(--el-fill-color-light);
  border-radius: 6px;
  
  .filter-form {
    display: flex;
    flex-wrap: wrap;
    align-items: flex-start;
    gap: 8px;
    
    .el-form-item {
      margin-bottom: 0;
      margin-right: 0;
    }
  }
}

.schedule-cell {
  display: flex;
  flex-direction: column;
  gap: 4px;

  .schedule-name {
    display: flex;
    align-items: center;
    gap: 6px;
    font-weight: 600;
  }

  .schedule-time {
    display: flex;
    align-items: center;
    gap: 6px;
    font-size: 12px;
    color: var(--el-text-color-secondary);
  }

  .cell-icon {
    color: var(--el-color-primary);
  }
}

.reason-cell {
  overflow: hidden;
  text-overflow: ellipsis;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  line-clamp: 2;
  -webkit-box-orient: vertical;
  line-height: 1.5;
}

.action-buttons {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  justify-content: center;
}

.pagination-section {
  display: flex;
  justify-content: flex-end;
  margin-top: 16px;
  padding-top: 16px;
  border-top: 1px solid var(--el-border-color-lighter);
}

.detail-content-compact {
  .detail-row {
    margin-bottom: 12px;
    
    &:last-child {
      margin-bottom: 0;
    }
  }
  
  // Row 1: Lịch trực & Loại yêu cầu
  .detail-row:first-child {
    display: grid;
    grid-template-columns: 1fr auto;
    gap: 12px;
  }
  
  .detail-card {
    display: flex;
    align-items: flex-start;
    gap: 10px;
    background: var(--el-fill-color-light);
    padding: 12px;
    border-radius: 8px;
    
    .card-icon {
      color: var(--el-color-primary);
      font-size: 18px;
      margin-top: 2px;
    }
    
    .card-content {
      flex: 1;
      
      .card-label {
        display: block;
        font-size: 11px;
        color: var(--el-text-color-secondary);
        margin-bottom: 2px;
      }
      
      .card-value {
        display: flex;
        align-items: center;
        font-size: 14px;
        font-weight: 500;
        color: var(--el-text-color-primary);
      }
      
      .card-sub {
        display: flex;
        align-items: center;
        gap: 4px;
        font-size: 12px;
        color: var(--el-text-color-secondary);
        margin-top: 4px;
      }
    }
    
    &.type-card {
      min-width: 140px;
    }
  }
  
  // Row 2: Persons
  .persons-row {
    display: flex;
    align-items: stretch;
    gap: 12px;
  }
  
  .person-card {
    flex: 1;
    background: var(--el-fill-color-light);
    padding: 12px;
    border-radius: 8px;
    border-left: 3px solid var(--el-color-primary);
    
    &.target {
      border-left-color: var(--el-color-success);
    }
    
    .person-header {
      display: flex;
      align-items: center;
      gap: 6px;
      font-size: 11px;
      color: var(--el-text-color-secondary);
      margin-bottom: 6px;
    }
    
    .person-info {
      .person-name {
        font-size: 14px;
        font-weight: 500;
        color: var(--el-text-color-primary);
        margin-bottom: 6px;
      }
      
      .person-tags {
        display: flex;
        flex-wrap: wrap;
        gap: 4px;
      }
    }
  }
  
  .arrow-icon {
    display: flex;
    align-items: center;
    color: var(--el-text-color-placeholder);
  }
  
  // Row 3: Reason
  .reason-card {
    background: var(--el-fill-color-light);
    padding: 12px;
    border-radius: 8px;
    
    .reason-header {
      display: flex;
      align-items: center;
      gap: 6px;
      font-size: 12px;
      color: var(--el-text-color-secondary);
      margin-bottom: 8px;
      
      .create-time {
        margin-left: auto;
        font-size: 11px;
      }
    }
    
    .reason-content {
      font-size: 14px;
      line-height: 1.6;
      color: var(--el-text-color-primary);
      background: var(--el-fill-color);
      padding: 10px 12px;
      border-radius: 6px;
    }
    
    .reason-remark {
      margin-top: 8px;
      font-size: 13px;
      color: var(--el-text-color-secondary);
    }
  }
  
  // Row 4: Approval
  .approval-card {
    background: var(--el-fill-color-light);
    padding: 12px;
    border-radius: 8px;
    border-left: 3px solid var(--el-text-color-placeholder);
    
    &.approved {
      border-left-color: var(--el-color-success);
      background: var(--el-color-success-light-9);
    }
    
    &.rejected {
      border-left-color: var(--el-color-danger);
      background: var(--el-color-danger-light-9);
    }
    
    .approval-header {
      display: flex;
      align-items: center;
      gap: 6px;
      font-size: 13px;
      font-weight: 500;
      color: var(--el-text-color-primary);
      
      .approval-time {
        margin-left: auto;
        font-size: 11px;
        font-weight: normal;
        color: var(--el-text-color-secondary);
      }
    }
    
    .approval-note {
      margin-top: 8px;
      font-size: 13px;
      color: var(--el-text-color-secondary);
    }
  }
}
</style>
