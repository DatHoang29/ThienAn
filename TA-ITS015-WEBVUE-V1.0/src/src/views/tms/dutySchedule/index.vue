<template>
  <div class="tms-duty-schedule-container">
    <!-- Filter Card -->
    <el-card shadow="hover" :body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', alignItems: 'start' }">
      <el-form 
        :model="state.queryParams" 
        ref="queryForm" 
        :show-message="false" 
        :inlineMessage="true"
        label-width="auto" 
        style="flex: 1 1 0%"
        label-position="left"
      >
        <el-row :gutter="10">
          <!-- Quick Filter -->
          <el-col class="mb5" :xs="24" :sm="24" :md="24" :lg="24" :xl="24">
            <el-form-item :label="$t('lz.label.tmsDutySchedule.quickFilter')" prop="quickFilter">
              <el-radio-group v-model="state.quickFilter" @change="handleQuickFilter" size="small">
                <el-radio-button label="createdByMe">{{ $t('lz.label.tmsDutySchedule.createdByMe') }}</el-radio-button>
                <el-radio-button label="related">{{ $t('lz.label.tmsDutySchedule.related') }}</el-radio-button>
                <el-radio-button label="all">{{ $t('lz.label.base.all') }}</el-radio-button>
              </el-radio-group>
            </el-form-item>
          </el-col>

          <!-- Search Text -->
          <el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
            <el-form-item :label="$t('lz.button.base.search')" prop="searchText" >
              <el-input 
                v-model="state.searchText" 
                :placeholder="$t('lz.label.tmsDutySchedule.searchPlaceholder')"
                clearable
                @input="handleSearch"
                @keyup.enter.native="handleQuery"
              >
                <template #prefix>
                  <el-icon><ele-Search /></el-icon>
                </template>
              </el-input>
            </el-form-item>
          </el-col>

          <!-- Employee filter - only show when quickFilter is 'all' -->
          <el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6" v-if="state.quickFilter === 'all'">
            <el-form-item :label="$t('lz.label.tmsDutySchedule.employee')" prop="employeeId">
              <el-select 
                v-model="state.queryParams.employeeId" 
                :placeholder="$t('lz.label.tmsDutySchedule.selectEmployee')"
                clearable
                filterable
                @change="handleQuery"
              >
                <el-option
                  v-for="employee in state.employeeList"
                  :key="employee.username"
                  :label="`${employee.name} (${employee.username})`"
                  :value="employee.username"
                />
              </el-select>
            </el-form-item>
          </el-col>

          <!-- Status Filter -->
          <el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
            <el-form-item :label="$t('lz.entity.base.status')" prop="status">
              <el-select 
                v-model="state.queryParams.status" 
                :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.base.status') })"
                clearable
                @change="handleQuery"
              >
                <el-option 
                  v-for="item in getConfigDataByCode(BaseConfigTypeEnum.TmsDutyState)" 
                  :key="item.code" 
                  :label="item.name" 
                  :value="item.code" 
                />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>
      </el-form>

      <el-divider style="height: calc(100% - 5px); margin: 0 10px" direction="vertical" />
      
      <el-row>
        <SearchButton 
          @search="handleQuery" 
          @reset="resetQuery" 
          :loading="options.loading" 
        />
      </el-row>
    </el-card>

    <!-- View Controls Card -->
    <el-card shadow="hover" style="margin-top: 5px; " v-if="state.displayMode === 'calendar'">
      <div class="calendar-controls-wrapper">
        <div class="controls-left">
          <el-segmented v-model="state.displayMode" :options="displayModeOptions" size="default" />
        </div>
        
        <div class="controls-center">
          <div class="current-period">
            <span class="period-text">{{ currentPeriodText }}</span>
          </div>
        </div>

        <div class="controls-right">
          <el-button-group>
            <el-button @click="handlePrev" size="default">
              <el-icon><ele-ArrowLeft /></el-icon>
            </el-button>
            <el-button @click="handleToday" size="default">{{ $t('lz.label.tmsDutySchedule.today') }}</el-button>
            <el-button @click="handleNext" size="default">
              <el-icon><ele-ArrowRight /></el-icon>
            </el-button>
          </el-button-group>

          <el-dropdown @command="changeView" trigger="click" style="margin-left: 10px">
            <el-button size="default">
              {{ getViewLabel(state.currentView) }}
              <el-icon class="el-icon--right"><ele-ArrowDown /></el-icon>
            </el-button>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item command="dayGridMonth">{{ $t('lz.label.base.month') }}</el-dropdown-item>
                <el-dropdown-item command="timeGridWeek">{{ $t('lz.label.tmsDutySchedule.viewWeek') }}</el-dropdown-item>
                <el-dropdown-item command="timeGridDay">{{ $t('lz.label.base.day') }}</el-dropdown-item>
                <el-dropdown-item command="dayGridYear">{{ $t('lz.label.base.year') }}</el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>

          <!-- Change Request Management Button -->
          <el-badge
            :value="state.pendingChangeRequestCount"
            :hidden="state.pendingChangeRequestCount === 0"
            type="danger"
            style="margin-left: 10px;"
          >
            <el-button
              type="warning"
              size="default"
              @click="handleOpenChangeRequestList"
            >
              <el-icon><ele-List /></el-icon>
              {{ $t('lz.label.tmsDutySchedule.changeRequest') }}
            </el-button>
          </el-badge>
        </div>
      </div>
    </el-card>

    <!-- List View Controls -->
    <el-card shadow="hover" style="margin-top: 5px;" v-else>
      <div style="display: flex; justify-content: space-between; align-items: center;">
        <el-segmented v-model="state.displayMode" :options="displayModeOptions" size="default" />
        
        <!-- Change Request Management Button -->
        <el-badge
          :value="state.pendingChangeRequestCount"
          :hidden="state.pendingChangeRequestCount === 0"
          type="danger"
        >
          <el-button
            type="warning"
            size="default"
            @click="handleOpenChangeRequestList"
          >
            <el-icon><ele-List /></el-icon>
            {{ $t('lz.label.tmsDutySchedule.changeRequest') }}
          </el-button>
        </el-badge>
      </div>
    </el-card>

    <!-- Calendar/List View -->
    <el-card class="full-table" shadow="hover" style="margin-top: 5px" v-if="state.displayMode === 'calendar'">
      <!-- Year Grid View -->
      <YearGridView
        v-if="state.currentView === 'dayGridYear'"
        :year="state.currentYear"
        :events="state.filteredSchedules"
        @month-click="handleYearMonthClick"
        @event-click="handleYearEventClick"
        @day-click="handleYearDayClick"
      />
      <!-- Regular Calendar View -->
      <FullCalendar v-else :options="calendarOptions" ref="fullCalendar" />
    </el-card>

    <!-- List View Component -->
    <el-card class="full-table" shadow="hover" style="margin-top: 5px" v-else>
      <ScheduleListView
        :schedules="state.filteredSchedules"
        :team-list="state.teamList"
        :employee-list="state.employeeList"
        :loading="options.loading"
        :date-range="state.queryParams.dateRange"
        :status="state.queryParams.status"
        :show-employee-filter="state.quickFilter === 'all'"
        :employee-id="state.queryParams.employeeId"
        @view-detail="handleViewDetail"
        @edit="handleEditSchedule"
        @delete="handleDeleteSchedule"
        @refresh="handleQuery"
        @date-change="handleListDateChange"
        @status-change="handleListStatusChange"
        @employee-change="handleListEmployeeChange"
      />
    </el-card>

    <!-- Floating Action Button -->
    <el-button
      type="primary"
      circle
      size="large"
      class="fab-button"
      @click="handleAddDetail"
    >
      <el-icon size="24"><ele-Plus /></el-icon>
    </el-button>

    <!-- Detail Dialog -->
    <el-dialog 
      v-model="state.drawerVisible" 
      :title="state.drawerTitle"
      width="1200px"
      draggable
      :close-on-click-modal="false"
    >
      <template #header>
        <div style="color: #fff; display: flex; align-items: center;">
          <el-icon size="16" style="margin-right: 5px;">
            <ele-Calendar />
          </el-icon>
          <span>{{ state.drawerTitle }}</span>
        </div>
      </template>

      <div v-if="state.selectedDetail">
        <!-- Action Buttons and Status Progress -->
        <div class="detail-actions-top">
          <!-- Status Progress - Left Side -->
          <div class="status-progress-inline">
            <div class="status-badge">
              <el-icon class="status-icon"><ele-CircleCheck /></el-icon>
              <span class="status-label">{{ $t('lz.entity.base.status') }}:</span>
              <TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.TmsDutyState, state.selectedDetail.status || '')" />
            </div>
            <el-divider direction="vertical" style="height: 30px; margin: 0 12px;" />
            <div class="process-steps-container">
              <div class="process-steps-mini">
                <div 
                  :class="['process-step-mini', getStepClass('register')]"
                  @click="handleStepClick('register')"
                >
                  <div class="step-circle">
                    <el-icon v-if="getStepClass('register') === 'step-completed'"><ele-Check /></el-icon>
                    <span v-else>1</span>
                  </div>
                  <span class="step-label">{{ $t('lz.label.tmsDutySchedule.statusRegistered') }}</span>
                </div>
                <div class="step-connector" :class="getStepClass('confirm')"></div>
                <div 
                  :class="['process-step-mini', getStepClass('confirm')]"
                  @click="handleStepClick('confirm')"
                >
                  <div class="step-circle">
                    <el-icon v-if="getStepClass('confirm') === 'step-completed'"><ele-Check /></el-icon>
                    <span v-else>2</span>
                  </div>
                  <span class="step-label">{{ $t('lz.label.tmsDutySchedule.statusConfirmed') }}</span>
                </div>
                <div class="step-connector" :class="getStepClass('process')"></div>
                <div 
                  :class="['process-step-mini', getStepClass('process')]"
                  @click="handleStepClick('process')"
                >
                  <div class="step-circle">
                    <el-icon v-if="getStepClass('process') === 'step-completed'"><ele-Check /></el-icon>
                    <span v-else>3</span>
                  </div>
                  <span class="step-label">{{ $t('lz.label.tmsDutySchedule.statusInProgressLabel') }}</span>
                </div>
                <div class="step-connector" :class="getStepClass('finish')"></div>
                <div 
                  :class="['process-step-mini', getStepClass('finish')]"
                  @click="handleStepClick('finish')"
                >
                  <div class="step-circle">
                    <el-icon v-if="getStepClass('finish') === 'step-completed'"><ele-Check /></el-icon>
                    <span v-else>4</span>
                  </div>
                  <span class="step-label">{{ $t('lz.label.tmsDutySchedule.statusCompletedLabel') }}</span>
                </div>
              </div>
            </div>
          </div>

          <!-- Action Buttons - Right Side -->
          <div class="action-buttons-group">
            <!-- Change Request Button (for members only) -->
            <!-- <el-button
              v-if="isCurrentUserMember"
              type="warning"
              @click="handleOpenChangeRequestDialog"
            >
              <el-icon><ele-EditPen /></el-icon>
              Yêu cầu thay đổi
            </el-button> -->
            
            <el-button 
              v-if="canEditOrDelete"
              type="primary" 
              @click="handleEditFromDrawer"
            >
              <el-icon><ele-Edit /></el-icon>
              {{ $t('lz.button.base.edit') }}
            </el-button>
            <el-button type="success" @click="handleCopyFromDrawer">
              <el-icon><ele-CopyDocument /></el-icon>
              {{ $t('lz.button.base.copy') }}
            </el-button>
            
            <!-- Nút Xóa: chỉ hiện khi trạng thái là register (chưa xác nhận) -->
            <el-button 
              v-if="canDelete && state.selectedDetail?.status === 'register'"
              type="danger" 
              @click="handleDeleteFromDrawer"
            >
              <el-icon><ele-Delete /></el-icon>
              {{ $t('lz.button.base.delete') }}
            </el-button>

            <!-- Nút Hủy bỏ: hiện khi trạng thái đã xác nhận trở đi (confirm, process, finish) -->
            <el-button 
              v-if="canEditOrDelete && (state.selectedDetail?.status === 'confirm' || state.selectedDetail?.status === 'process' || state.selectedDetail?.status === 'finish')"
              type="warning" 
              @click="handleCancelSchedule"
            >
              <el-icon><ele-CircleClose /></el-icon>
              {{ $t('lz.button.base.cancel') }}
            </el-button>

            <!-- Nút Xóa vĩnh viễn: chỉ hiện khi trạng thái cancel và có quyền admin -->
            <el-button 
              v-if="canDelete && state.selectedDetail?.status === 'cancel'"
              type="danger" 
              @click="handleDeleteFromDrawer"
            >
              <el-icon><ele-Delete /></el-icon>
              {{ $t('lz.button.base.delete') }}
            </el-button>
          </div>
        </div>

        <!-- Tabs -->
        <el-tabs v-model="state.activeTab" type="border-card" @tab-change="handleDetailTabChange">
          <!-- Tab 1: Chi tiết -->
          <el-tab-pane :label="$t('lz.button.base.detail')" name="info">
            <DetailInfo :detail="state.selectedDetail" :team-list="state.teamList" @refresh="handleRefreshAfterStatusUpdate" />
          </el-tab-pane>

          <!-- Tab 2: Danh sách nhân viên chi tiết -->
          <el-tab-pane :label="$t('lz.label.tmsDutySchedule.employee')" name="users">
            <div v-if="state.selectedDetail.details && state.selectedDetail.details.length > 0">
              <!-- Alert for current user -->
              <!-- <el-alert
                v-if="isCurrentUserMember"
                type="info"
                :closable="false"
                show-icon
                style="margin-bottom: 16px;"
              >
                <template #title>
                  <span style="font-size: 13px;">
                    <strong>Mẹo:</strong> Bạn có thể gửi yêu cầu thay đổi trực tiếp từ bảng dưới đây.
                    Click <strong>"Xin nghỉ"</strong> hoặc <strong>"Đổi ca"</strong> trên hàng của bạn,
                    hoặc <strong>"Thay thế"</strong> để thay thế người khác.
                  </span>
                </template>
              </el-alert> -->

              <!-- Toolbar for leader/creator when schedule is confirmed -->
              <div v-if="canEditOrDelete && isLimitedEditMode" class="employee-toolbar" style="margin-bottom: 12px; display: flex; justify-content: space-between; align-items: center;">
                <el-alert type="warning" :closable="false" show-icon style="flex: 1; margin-right: 12px;">
                  <template #title>
                    <span style="font-size: 12px;">
                      {{ $t('lz.message.tmsDutySchedule.limitedEditModeInfo') }}
                    </span>
                  </template>
                </el-alert>
                <el-button type="primary" @click="handleOpenAddUserDialog">
                  <el-icon><ele-Plus /></el-icon>
                  {{ $t('lz.label.tmsDutySchedule.addEmployee') }}
                </el-button>
              </div>
              
              <el-table 
                :data="sortedEmployeeList" 
                style="width: 100%"
                stripe
              >
              <el-table-column type="index" :label="$t('lz.label.base.idx')" width="60" align="center" />
              <el-table-column prop="userId" :label="$t('lz.entity.base.username')" min-width="150">
                <template #default="{ row }">
                  <el-tag v-if="isPrimaryRole(row.role)" type="success" size="small">
                    <el-icon><ele-StarFilled /></el-icon>
                  </el-tag>
                  <el-tooltip :content="`Username: ${row.userId}`" placement="top">
                    <span style="cursor: help;">{{ getUserRealName(row.userId) }}</span>
                  </el-tooltip>
                </template>
              </el-table-column>
              <el-table-column prop="role" :label="$t('lz.label.tmsDutySchedule.role')" width="120" align="center">
                <template #default="{ row }">
                  <TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.TmsDutyPatronRole, (row.role || '').toLowerCase())" />
                </template>
              </el-table-column>
              <el-table-column prop="status" :label="$t('lz.entity.base.status')" width="120" align="center">
                <template #default="{ row }">
                  <TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.TmsDutyPatronState, row.status || '')" />
                </template>
              </el-table-column>
              <el-table-column prop="createTime" :label="$t('lz.entity.base.updateTime')" width="160">
                <template #default="{ row }">
                  {{ formatDateTime(row.createTime) }}
                </template>
              </el-table-column>
              <el-table-column prop="remark" :label="$t('lz.entity.base.remark')" min-width="150">
                <template #default="{ row }">
                  {{ row.reason || '-' }}
                </template>
              </el-table-column>
              <el-table-column :label="$t('lz.label.base.action')" width="350" align="center" fixed="right">
                <template #default="{ row }">
                  <div class="user-actions">
                    <!-- Debug info -->
                    <!-- Current user: {{ isCurrentUser(row.userId) }}, Member: {{ isCurrentUserMember }} -->
                    
                    <!-- Current user actions -->
                    <template v-if="isCurrentUser(row.userId)">
                      <el-tooltip :content="$t('lz.message.tmsDutySchedule.requestAbsentTooltip')" placement="top">
                        <el-button
                          type="warning"
                          size="small"
                          link
                          @click="handleQuickChangeRequest('Absent', row)"
                        >
                          <el-icon><ele-CircleClose /></el-icon>
                          <span style="margin-left: 2px;">{{ $t('lz.label.tmsDutySchedule.requestAbsent') }}</span>
                        </el-button>
                      </el-tooltip>
                      <el-tooltip :content="$t('lz.message.tmsDutySchedule.requestSwapTooltip')" placement="top">
                        <el-button
                          type="primary"
                          size="small"
                          link
                          @click="handleQuickChangeRequest('Swap', row)"
                        >
                          <el-icon><ele-Sort /></el-icon>
                          <span style="margin-left: 2px;">{{ $t('lz.label.tmsDutySchedule.swapShift') }}</span>
                        </el-button>
                      </el-tooltip>
                      <el-tooltip :content="$t('lz.message.tmsDutySchedule.requestTransferTooltip')" placement="top">
                        <el-button
                          type="info"
                          size="small"
                          link
                          @click="handleQuickChangeRequest('Transfer', row)"
                        >
                          <el-icon><ele-Right /></el-icon>
                          <span style="margin-left: 2px;">{{ $t('lz.label.tmsDutySchedule.transferToOther') }}</span>
                        </el-button>
                      </el-tooltip>
                    </template>
                    
                    <!-- Other users - allow replacement (for members or creator) -->
                    <template v-else-if="isCurrentUserMember">
                      <el-tooltip :content="$t('lz.message.tmsDutySchedule.requestReplaceTooltip', { user: row.userId })" placement="top">
                        <el-button
                          type="success"
                          size="small"
                          link
                          @click="handleQuickChangeRequest('Replace', row)"
                        >
                          <el-icon><ele-RefreshRight /></el-icon>
                          <span style="margin-left: 2px;">{{ $t('lz.label.tmsDutySchedule.replace') }}</span>
                        </el-button>
                      </el-tooltip>
                    </template>
                    
                    <!-- No actions available for normal view -->
                    <template v-else-if="!canEditOrDelete">
                      <span style="color: var(--el-text-color-placeholder); font-size: 12px;">—</span>
                    </template>

                    <!-- Leader/Creator actions when schedule is confirmed -->
                    <template v-if="canEditOrDelete && isLimitedEditMode">
                      <el-divider direction="vertical" v-if="isCurrentUser(row.userId) || isCurrentUserMember" />
                      <el-tooltip :content="$t('lz.label.tmsDutySchedule.changeRole')" placement="top">
                        <el-button
                          type="primary"
                          size="small"
                          link
                          @click="handleOpenChangeRoleDialog(row)"
                        >
                          <el-icon><ele-Switch /></el-icon>
                          <span style="margin-left: 2px;">{{ $t('lz.label.tmsDutySchedule.role') }}</span>
                        </el-button>
                      </el-tooltip>
                      <el-tooltip :content="$t('lz.message.tmsDutySchedule.removeFromSchedule')" placement="top">
                        <el-button
                          type="danger"
                          size="small"
                          link
                          @click="handleRemoveUserFromSchedule(row)"
                        >
                          <el-icon><ele-Delete /></el-icon>
                          <span style="margin-left: 2px;">{{ $t('lz.button.base.delete') }}</span>
                        </el-button>
                      </el-tooltip>
                    </template>
                  </div>
                </template>
              </el-table-column>
            </el-table>
            </div>
            <el-empty 
              v-else 
              :description="$t('lz.label.tmsDutySchedule.noEmployees')"
              :image-size="100"
            />
          </el-tab-pane>
          <!-- Tab 3: History -->
          <el-tab-pane name="history">
            <template #label>
              <span style="display: flex; align-items: center; gap: 6px;">
                <!-- <el-icon><ele-DocumentCopy /></el-icon> -->
                <span>{{ $t('lz.label.tmsDutySchedule.history') }}</span>
              </span>
            </template>
            
            <div v-loading="state.historyLoading">
              <!-- Horizontal Timeline -->
              <div 
                v-if="state.historyData.length > 0"
                class="history-timeline-container"
              >
                <div class="history-timeline-scroll">
                  <div class="history-timeline">
                    <div 
                      v-for="(item, index) in state.historyData" 
                      :key="index"
                      class="timeline-item"
                    >
                      <!-- Timeline dot and line -->
                      <div class="timeline-dot-container">
                        <div class="timeline-line" v-if="index < state.historyData.length - 1"></div>
                        <div class="timeline-dot" :class="getTimelineDotClass(item.actionType)">
                          <el-icon size="12"><ele-Check /></el-icon>
                        </div>
                      </div>
                      
                      <!-- Timeline content card -->
                      <div class="timeline-card">
                        <div class="timeline-header">
                          <TagInfo :config-item="getHistoryActionConfig(item.actionType)" />
                        </div>
                        
                        <div class="timeline-time">
                          <el-icon size="12"><ele-Clock /></el-icon>
                          {{ formatHistoryDateTime(item.createTime) }}
                        </div>
                        
                        <div class="timeline-body">
                          <div class="timeline-row" v-if="item.actionBy">
                            <span class="timeline-label">{{ $t('lz.label.tmsDutySchedule.performer') }}:</span>
                            <span class="timeline-value">
                              <el-icon size="12"><ele-User /></el-icon>
                              {{ getUserRealName(item.actionBy) }}
                            </span>
                          </div>
                          
                          <div class="timeline-row" v-if="item.affectedUserId">
                            <span class="timeline-label">{{ $t('lz.label.tmsDutySchedule.targetLabel') }}:</span>
                            <span class="timeline-value">
                              <el-icon size="12"><ele-UserFilled /></el-icon>
                              {{ getUserRealName(item.affectedUserId) }}
                            </span>
                          </div>
                          
                          <div class="timeline-row" v-if="item.description">
                            <span class="timeline-label">{{ $t('lz.entity.base.desc') }}:</span>
                            <span class="timeline-value">{{ item.description }}</span>
                          </div>
                          
                          <div class="timeline-row" v-if="item.reason">
                            <span class="timeline-label">{{ $t('lz.label.tmsDutySchedule.reason') }}:</span>
                            <span class="timeline-value reason">{{ item.reason }}</span>
                          </div>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>

              <el-empty 
                v-else-if="!state.historyLoading"
                :description="$t('lz.label.tmsDutySchedule.noHistoryData')"
                :image-size="80"
              />
            </div>
          </el-tab-pane>
          <!-- Tab 4: Checklist (placeholder) -->
          <el-tab-pane :label="$t('lz.label.tmsDutySchedule.checklistTab')" name="checklist">
            <el-empty 
              :description="$t('lz.label.tmsDutySchedule.featureDeveloping')"
              :image-size="120"
            >
              <template #image>
                <el-icon size="120" color="var(--el-color-info)">
                  <ele-Document />
                </el-icon>
              </template>
            </el-empty>
          </el-tab-pane>

        </el-tabs>
      </div>
    </el-dialog>

    <!-- Add/Edit Schedule Dialog -->
    <EditDutySchedule
      ref="editScheduleRef"
      :title="state.scheduleDialogTitle"
      :operate-type="state.operateType"
      @handleQuery="handleRefreshAfterStatusUpdate"
    />

    <!-- Change Request Dialog -->
    <ChangeRequestDialog
      ref="changeRequestDialogRef"
      :schedule="state.selectedDetail"
      :employee-list="state.employeeList"
      @success="handleChangeRequestSuccess"
      @create-new-schedule="handleCreateNewScheduleFromSwap"
    />

    <!-- Change Request List Dialog -->
    <ChangeRequestList
      ref="changeRequestListRef"
      :employee-list="state.employeeList"
      @refresh="handleChangeRequestListRefresh"
    />

    <!-- Add User to Schedule Dialog -->
    <el-dialog
      v-model="state.addUserDialogVisible"
      :title="$t('lz.label.tmsDutySchedule.addEmployeeToSchedule')"
      width="500px"
      :close-on-click-modal="false"
      @close="handleCloseAddUserDialog"
    >
      <el-form label-width="120px" :disabled="state.personnelActionLoading">
        <el-form-item :label="$t('lz.label.tmsDutySchedule.employee')" required>
          <el-select
            v-model="state.newUserForm.userId"
            :placeholder="$t('lz.label.tmsDutySchedule.selectEmployee')"
            filterable
            clearable
            style="width: 100%"
          >
            <el-option
              v-for="user in availableUsersForSchedule"
              :key="user.id"
              :label="user.name || user.username"
              :value="user.username"
            >
              <span>{{ user.name || user.username }}</span>
              <span style="color: var(--el-text-color-secondary); margin-left: 8px; font-size: 12px;">
                {{ user.username }}
              </span>
            </el-option>
          </el-select>
        </el-form-item>
        
        <el-form-item :label="$t('lz.label.tmsDutySchedule.role')">
          
          <el-radio-group v-model="state.newUserForm.role">
            <el-radio v-for="item in getConfigDataByCode(BaseConfigTypeEnum.TmsDutyPatronRole)" :key="item.code" :label="item.name"
									:value="item.code">
									{{ item.name }}
								</el-radio>
            <!-- <el-radio value="member">{{ $t('lz.label.tmsDutySchedule.membersLabel') }}</el-radio>
            <el-radio value="leader">{{ $t('lz.label.tmsDutySchedule.leader') }}</el-radio> -->
          </el-radio-group>
        </el-form-item>
        
        <el-form-item :label="$t('lz.entity.base.remark')">
          <el-input
            v-model="state.newUserForm.remark"
            type="textarea"
            :rows="2"
            :placeholder="$t('lz.label.tmsDutySchedule.remarkPlaceholder')"
          />
        </el-form-item>
      </el-form>
      
      <template #footer>
        <el-button @click="handleCloseAddUserDialog" :disabled="state.personnelActionLoading">
          {{ $t('lz.button.base.cancel') }}
        </el-button>
        <el-button
          type="primary"
          @click="handleAddUserToSchedule"
          :loading="state.personnelActionLoading"
        >
          {{ $t('lz.button.base.add') }}
        </el-button>
      </template>
    </el-dialog>

    <!-- Change Role Dialog -->
    <el-dialog
      v-model="state.changeRoleDialogVisible"
      width="450px"
      :close-on-click-modal="false"
      @close="handleCloseChangeRoleDialog"
    >
    <template #header>
				<div style="color: #fff">
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Edit /> </el-icon>
					<span> {{ $t('lz.label.tmsDutySchedule.changeRole') }} </span>
				</div>
			</template>
      <el-form label-width="120px" :disabled="state.personnelActionLoading">
        <el-form-item :label="$t('lz.label.tmsDutySchedule.employee')" label-position="left">
          <span style="font-weight: 500;">{{ getUserRealName(state.changeRoleForm.userId) }}</span>
        </el-form-item>
        
        <el-form-item :label="$t('lz.label.tmsDutySchedule.currentRoleLabel')" label-position="left">
          <TagInfo :config-item="getConfigItemByCode(BaseConfigTypeEnum.TmsDutyPatronRole, (state.changeRoleForm.currentRole || '').toLowerCase())" />
        </el-form-item>
        
        <el-form-item :label="$t('lz.label.tmsDutySchedule.newRoleLabel')" required label-position="left">
          <el-radio-group v-model="state.changeRoleForm.newRole">
            <el-radio 
              v-for="role in getConfigDataByCode(BaseConfigTypeEnum.TmsDutyPatronRole)" 
              :key="role.code" 
              :value="role.code" 
              :disabled="state.changeRoleForm.currentRole === role.code"
            >
              {{ role.name }}
            </el-radio>
          </el-radio-group>
        </el-form-item>
        
        <el-form-item :label="$t('lz.label.tmsDutySchedule.reason')" label-position="left">
          <el-input
            v-model="state.changeRoleForm.reason"
            type="textarea"
            :rows="2"
            :placeholder="$t('lz.label.tmsDutySchedule.remarkPlaceholder')"
          />
        </el-form-item>
      </el-form>
      
      <template #footer>
        <el-button @click="handleCloseChangeRoleDialog" :disabled="state.personnelActionLoading">
          {{ $t('lz.button.base.cancel') }}
        </el-button>
        <el-button
          type="primary"
          @click="handleChangeUserRole"
          :loading="state.personnelActionLoading"
        >
          {{ $t('lz.button.base.save') }}
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script lang="ts" setup name="tmsDutySchedule">
import { onMounted, reactive, ref, computed, defineAsyncComponent, nextTick } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { useI18n } from 'vue-i18n';
import FullCalendar from '@fullcalendar/vue3';
import dayGridPlugin from '@fullcalendar/daygrid';
import timeGridPlugin from '@fullcalendar/timegrid';
import listPlugin from '@fullcalendar/list';
import interactionPlugin from '@fullcalendar/interaction';
import type { CalendarOptions, EventDropArg, DatesSetArg } from '@fullcalendar/core';
import { getAPI } from '/@/utils/axios-utils';
import { TmsDutyScheduleApi, TmsPatrolTeamApi } from '/@/api-services/tms';
import { SysUserApi } from '/@/api-services/system';
import { useUserInfo } from '/@/stores/userInfo';
import { BaseConfigTypeEnum } from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';
import {
  DutyScheduleOutput,
  DutyScheduleInput,
  UpdateDutyScheduleInput,
  PatrolTeamOutput,
  DutyScheduleFilterMode,
} from '/@/api-services/tms/models';
import EditDutySchedule from './component/editDutySchedule.vue';
import ScheduleListView from './component/scheduleListView.vue';
import YearGridView from './component/yearGridView.vue';
import DetailInfo from './component/detailInfo.vue';
import ChangeRequestDialog from './component/changeRequestDialog.vue';
import ChangeRequestList from './component/changeRequestList.vue';

const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));
const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));

const { t: $t } = useI18n();


// User info store
const userInfoStore = useUserInfo();
const getConfigItemByCode = useBaseConfig().getConfigItemByCode;
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;

const fullCalendar = ref();
const editScheduleRef = ref();
const changeRequestDialogRef = ref();
const changeRequestListRef = ref();
const options = reactive({ loading: false });

// State management
const state = reactive({
  queryParams: {
    employeeId: undefined as string | undefined,
    dateRange: [] as string[],
    startDate: undefined as Date | undefined,
    endDate: undefined as Date | undefined,
    status: undefined as string | undefined, // Changed to string only
  },
  teamList: [] as any[],
  employeeList: [] as any[],
  currentView: 'dayGridMonth' as 'dayGridMonth' | 'timeGridWeek' | 'timeGridDay' | 'dayGridYear' | 'listWeek' | 'listMonth',
  currentYear: new Date().getFullYear(),
  currentDate: new Date(),
  calendarApi: null as any,
  drawerVisible: false,
  drawerTitle: '',
  selectedDetail: null as DutyScheduleOutput | null,
  activeTab: 'info',
  scheduleDialogTitle: '',
  operateType: 'add' as 'add' | 'edit',
  
  // Filter states
  quickFilter: 'related' as 'createdByMe' | 'related' | 'all',
  searchText: '',
  displayMode: 'calendar' as 'calendar' | 'list',
  allSchedules: [] as DutyScheduleOutput[],
  filteredSchedules: [] as DutyScheduleOutput[],
  pendingChangeRequestCount: 0,
  
  // Personnel management states
  addUserDialogVisible: false,
  changeRoleDialogVisible: false,
  newUserForm: {
    userId: '',
    role: 'member' as 'leader' | 'member',
    remark: ''
  },
  changeRoleForm: {
    detailId: '',
    userId: '',
    currentRole: '',
    newRole: '' as 'leader' | 'member',
    reason: ''
  },
  personnelActionLoading: false,
  
  // History tab states
  historyData: [] as any[],
  historyLoading: false,
});

// Display mode options for segmented control
const displayModeOptions = computed(() => [
  { label: $t('lz.label.tmsDutySchedule.calendarView'), value: 'calendar', icon: 'Calendar' },
  { label: $t('lz.label.tmsDutySchedule.listView'), value: 'list', icon: 'List' },
]);

/**
 * Computed property for calendar title display
 * Formats the title based on current view type (month, week, day)
 */
const calendarTitle = computed(() => {
  if (!state.calendarApi) return '';
  const view = state.calendarApi.view;
  const start = view.activeStart;
  const end = view.activeEnd;
  
  if (state.currentView === 'dayGridMonth' || state.currentView === 'listMonth') {
    return start.toLocaleDateString('vi-VN', { month: 'long', year: 'numeric' });
  } else if (state.currentView === 'timeGridWeek' || state.currentView === 'listWeek') {
    const startStr = start.toLocaleDateString('vi-VN', { day: 'numeric', month: 'numeric' });
    const endStr = end.toLocaleDateString('vi-VN', { day: 'numeric', month: 'numeric', year: 'numeric' });
    return `${startStr} - ${endStr}`;
  } else {
    return start.toLocaleDateString('vi-VN', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' });
  }
});

/**
 * Get localized label for calendar view type
 * @param view - View type identifier
 * @returns Localized view label
 */
const getViewLabel = (view: string) => {
  const labels: Record<string, string> = {
    'dayGridMonth': $t('lz.label.base.month'),
    'timeGridWeek': $t('lz.label.tmsDutySchedule.weekView'),
    'timeGridDay': $t('lz.label.base.day'),
    'dayGridYear': $t('lz.label.base.year'),
    'listWeek': $t('lz.label.tmsDutySchedule.listView'),
    'listMonth': $t('lz.label.base.month'),
  };
  return labels[view] || $t('lz.label.base.month');
};

/**
 * Computed property for current period display text
 * Formats the period based on view type (year, month, week, day)
 */
const currentPeriodText = computed(() => {
  // Check if in year grid view
  if (state.currentView === 'dayGridYear') {
    return `Năm ${state.currentYear}`;
  }
  
  // Trigger reactivity by accessing state.currentDate
  const _ = state.currentDate;
  
  if (!fullCalendar.value) return '';
  
  const calendarApi = fullCalendar.value.getApi();
  const view = calendarApi.view;
  
  if (view.type === 'dayGridMonth') {
    // Tháng X năm YYYY
    const date = calendarApi.getDate();
    return `Tháng ${date.getMonth() + 1} năm ${date.getFullYear()}`;
  } else if (view.type === 'timeGridWeek') {
    // Tuần từ DD/MM - DD/MM/YYYY
    const start = view.currentStart;
    const end = new Date(view.currentEnd);
    end.setDate(end.getDate() - 1); // End date is exclusive
    
    const startStr = `${start.getDate().toString().padStart(2, '0')}/${(start.getMonth() + 1).toString().padStart(2, '0')}`;
    const endStr = `${end.getDate().toString().padStart(2, '0')}/${(end.getMonth() + 1).toString().padStart(2, '0')}/${end.getFullYear()}`;
    
    return `Tuần: ${startStr} - ${endStr}`;
  } else if (view.type === 'timeGridDay') {
    // Ngày DD/MM/YYYY
    const date = calendarApi.getDate();
    return `Ngày ${date.getDate().toString().padStart(2, '0')}/${(date.getMonth() + 1).toString().padStart(2, '0')}/${date.getFullYear()}`;
  }
  
  return '';
});

// Get patrol team name by ID
/**
 * Get patrol team name by ID
 * @param teamId - The ID of the patrol team
 * @returns The name of the team or 'N/A' if not found
 */
const getPatrolTeamName = (teamId: string | null | undefined) => {
  if (!teamId) return '-';
  const team = state.teamList.find((t: any) => t.id === teamId);
  return team?.name || teamId;
};

// Check if role is the primary role (first role in config, typically 'leader')
/**
 * Check if the given role code is the primary role (first role in config)
 * @param roleCode - The role code to check
 * @returns True if role is the first role in config
 */
const isPrimaryRole = (roleCode: string | undefined) => {
  if (!roleCode) return false;
  const roleConfigs = getConfigDataByCode(BaseConfigTypeEnum.TmsDutyPatronRole);
  return roleConfigs.length > 0 && 'leader' === roleCode.toLowerCase();
};

// Get leaders from details
/**
 * Get leaders from schedule details
 * @param details - Array of duty schedule details
 * @returns Array of leader details
 */
const getLeaders = (details: any[]) => {
  if (!details) return [];
  return details.filter(d => d.role === 'leader');
};

// Check if current user is a member of the selected schedule
/**
 * Check if current user is a member of the selected schedule or the creator
 * @returns True if user is in the details list or is the creator, false otherwise
 */
const isCurrentUserMember = computed(() => {
  if (!state.selectedDetail || !state.selectedDetail.details) return false;
  const currentUserId = userInfoStore.userInfos?.account || '';
  
  // Check if user is the creator
  if (state.selectedDetail.createUId === currentUserId) return true;
  
  // Check if user is in the details list
  return state.selectedDetail.details.some(d => d.userId === currentUserId);
});

// Check if current user can edit/delete (creator or leader)
/**
 * Check if current user can edit or delete the selected schedule
 * User can edit/delete if they are:
 * - The creator of the schedule
 * - OR a leader in the schedule
 * @returns True if user has edit/delete permission
 */
const canEditOrDelete = computed(() => {
  if (!state.selectedDetail) return false;
  
  // Block editing for cancelled schedules
  if (state.selectedDetail.status === 'cancel') return false;
  
  const currentUserId = userInfoStore.userInfos?.account || '';
  
  // Check if current user is the creator
  if (state.selectedDetail.createUId === currentUserId) return true;
  
  // Check if current user is a leader in this schedule
  if (state.selectedDetail.details) {
    const userDetail = state.selectedDetail.details.find((d: any) => d.userId === currentUserId);
    if (userDetail && userDetail.role === 'leader') {
      return true;
    }
  }
  
  return false;
});

/**
 * Check if current user can delete the selected schedule
 * Delete rules:
 * - If status is 'register' (draft): normal canEditOrDelete rules apply
 * - If status is 'confirm', 'process', 'finish': cannot delete, must cancel first
 * - If status is 'cancel': only users with role 111 or 222 can delete
 */
const canDelete = computed(() => {
  if (!state.selectedDetail) return false;
  const status = state.selectedDetail.status;
  
  // If cancelled: only role 111 or 222 can delete
  if (status === 'cancel') {
    const userRoles = userInfoStore.userInfos?.accountType || [];
    return userRoles =='111' || userRoles =='222';
  }
  
  // If confirmed or later (not cancelled): cannot delete, must cancel first
  if (status === 'confirm' || status === 'process' || status === 'finish') {
    return false;
  }
  
  // If draft (register): use normal canEditOrDelete rules
  return canEditOrDelete.value;
});

/**
 * Get tooltip message for delete button when disabled
 */
const getDeleteButtonTooltip = computed(() => {
  if (!state.selectedDetail) return '';
  const status = state.selectedDetail.status;
  
  if (status === 'confirm' || status === 'process' || status === 'finish') {
    return $t('lz.message.tmsDutySchedule.confirmedScheduleCannotDelete');
  }
  
  if (status === 'cancel') {
    return $t('lz.message.tmsDutySchedule.onlyAdminCanDeleteCancelled');
  }
  
  return '';
});

// Check if schedule is confirmed or later (limited edit mode)
/**
 * Check if schedule is in limited edit mode
 * Limited edit mode applies when status is confirmed, in progress, or finished
 * In this mode, only name, remark, and status to cancel can be edited
 * @returns True if schedule should be in limited edit mode
 */
const isLimitedEditMode = computed(() => {
  if (!state.selectedDetail) return false;
  const status = state.selectedDetail.status;
  // If status is 'confirm', 'process', 'finish' - only allow editing name, remark, and status to cancel
  return status === 'confirm' || status === 'process' || status === 'finish';
});

// Check if userId is current user
/**
 * Check if given userId matches current logged-in user
 * @param userId - The userId (username) to check
 * @returns True if userId matches current user
 */
const isCurrentUser = (userId: string) => {
  const currentUserId = userInfoStore.userInfos?.account || '';
  return userId === currentUserId;
};

/**
 * Get members (non-leaders) from schedule details
 * @param details - Array of duty schedule details
 * @returns Array of member details (excluding leaders)
 */
const getMembers = (details: any[]) => {
  if (!details) return [];
  return details.filter(d => d.role !== 'leader');
};

/**
 * Get CSS class for process step based on current status
 * @param stepStatus - The status of the step to check
 * @returns CSS class name for the step (completed, active, pending, cancelled)
 */
const getStepClass = (stepStatus: string) => {
  if (!state.selectedDetail) return 'step-pending';
  
  const statusOrder = ['register', 'confirm', 'process', 'finish'];
  const currentIndex = statusOrder.indexOf(state.selectedDetail.status || '');
  const stepIndex = statusOrder.indexOf(stepStatus);
  
  if (state.selectedDetail.status === 'cancel') {
    return 'step-cancelled';
  }
  
  if (stepIndex < currentIndex) {
    return 'step-completed';
  } else if (stepIndex === currentIndex) {
    return 'step-active';
  } else if (stepIndex === currentIndex + 1) {
    return 'step-pending step-next'; // Next step that can be clicked
  } else {
    return 'step-pending';
  }
};

/**
 * Handle click on process step to change schedule status
 * Only allows progressing to next step in workflow: register -> confirm -> process -> finish
 * @param newStatus - The new status to transition to
 */
const handleStepClick = async (newStatus: string) => {
  if (!state.selectedDetail) return;
  
  const currentStatus = state.selectedDetail.status;
  const statusOrder = ['register', 'confirm', 'process', 'finish'];
  const currentIndex = statusOrder.indexOf(currentStatus || '');
  const newIndex = statusOrder.indexOf(newStatus);
  
  // Don't allow clicking on completed or past steps
  if (newIndex <= currentIndex) {
    return;
  }
  
  // Only allow clicking the next step
  if (newIndex !== currentIndex + 1) {
    ElMessage.warning($t('lz.message.tmsDutySchedule.canOnlyMoveToNextStep'));
    return;
  }
  
  // Get status label from config
  const statusConfig = getConfigItemByCode(BaseConfigTypeEnum.TmsDutyState, newStatus);
  const statusLabel = statusConfig?.name || newStatus;
  
  try {
    await ElMessageBox.confirm(
      `${$t('lz.message.tmsDutySchedule.statusTransitionTo')} "${statusLabel}"?`,
      $t('lz.message.tmsDutySchedule.confirmStatusChangeTitle'),
      {
        confirmButtonText: $t('lz.button.base.confirm'),
        cancelButtonText: $t('lz.button.base.cancel'),
        type: 'warning',
      }
    );
    
    // Update schedule status using dedicated API
    await getAPI(TmsDutyScheduleApi).apiTmsTmsdutyscheduleUpdatestatusPost({
      id: state.selectedDetail.id!,
      status: newStatus,
    });
    
    // Refresh after status change
    await handleRefreshAfterStatusUpdate();
  } catch (error: any) {
    if (error !== 'cancel') {
      console.error('Error updating status:', error);
      ElMessage.error(error?.message || $t('lz.message.tmsDutySchedule.updateStatusError'));
    }
  }
};

/**
 * Handle quick filter change (createdByMe, related, all)
 * Set employeeId to current user by default, except when 'all' filter with specific employee selected
 * Refreshes data based on display mode
 */
const handleQuickFilter = () => {
  // For 'createdByMe' and 'related', always use current user
  if (state.quickFilter === 'related' || state.quickFilter === 'createdByMe') {
    state.queryParams.employeeId = userInfoStore.userInfos?.account || undefined;
  }
  // For 'all', only keep selected employeeId if one was explicitly chosen, otherwise reset to current user
  else if (state.quickFilter === 'all') {
    if (!state.queryParams.employeeId) {
      state.queryParams.employeeId = userInfoStore.userInfos?.account || undefined;
    }
  }
  
  if (state.displayMode === 'calendar') {
    handleQuery();
  } else {
    applyFilters();
  }
};

// Debounce timer for search
let searchTimer: any = null;

/**
 * Handle search input with debounce (500ms)
 * Prevents excessive API calls during typing
 */
const handleSearch = () => {
  // Clear previous timer
  if (searchTimer) {
    clearTimeout(searchTimer);
  }
  
  // Debounce search for 500ms
  searchTimer = setTimeout(() => {
    handleQuery();
  }, 500);
};

/**
 * Apply frontend filters to schedule list
 * Currently only applies search text filter (name and code)
 * Note: quickFilter is handled by backend through filterMode parameter
 */
const applyFilters = () => {
  let filtered = [...state.allSchedules];
  
  // Apply search filter
  if (state.searchText) {
    const searchLower = state.searchText.toLowerCase();
    filtered = filtered.filter(s => 
      s.name?.toLowerCase().includes(searchLower) ||
      s.code?.toLowerCase().includes(searchLower)
    );
  }
  
  state.filteredSchedules = filtered;
};

/**
 * Format date/time for display
 * @param date - Date to format (Date object or ISO string)
 * @returns Formatted date string in vi-VN locale (DD/MM/YYYY HH:mm) or '-' if invalid
 */
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


/**
 * Open detail drawer to view schedule information
 * @param row - The schedule row data to display
 */
const handleViewDetail = async (row: DutyScheduleOutput) => {
  state.selectedDetail = row;
  state.drawerTitle = $t('lz.label.tmsDutySchedule.scheduleDetail');
  state.drawerVisible = true;
  state.activeTab = 'info';
};

/**
 * Open edit dialog for schedule
 * @param row - The schedule to edit
 */
const handleEditSchedule = (row: DutyScheduleOutput) => {
  state.scheduleDialogTitle = $t('lz.button.base.edit') + ' ' + $t('lz.label.tmsDutySchedule.scheduleDetails');
  state.operateType = 'edit';
  
  // Check if schedule is in limited edit mode (confirmed, in progress, or finished)
  const isLimited = row.status === 'confirm' || row.status === 'process' || row.status === 'finish';
  
  // Pass full schedule object with limited edit flag
  editScheduleRef.value?.openDialog({ 
    schedule: row,
    isLimitedEdit: isLimited,
  });
};

/**
 * Delete a schedule with confirmation
 * @param row - The schedule to delete
 */
const handleDeleteSchedule = async (row: DutyScheduleOutput) => {
  try {
    await ElMessageBox.confirm(
      $t('lz.message.tmsDutySchedule.confirmDeleteContent'),
      $t('lz.message.tmsDutySchedule.confirmDelete'),
      {
        confirmButtonText: $t('lz.button.base.delete'),
        cancelButtonText: $t('lz.button.base.cancel'),
        type: 'warning',
      }
    );
    
    if (!row.id) return;
    
    await getAPI(TmsDutyScheduleApi).apiTmsTmsdutyscheduleDeletePost({ id: row.id });
    ElMessage.success($t('lz.message.base.deleteSuccess'));
    await handleQuery();
  } catch (error: any) {
    if (error !== 'cancel') {
      console.error('Error deleting schedule:', error);
      ElMessage.error(error?.message || $t('lz.message.base.acctionFail'));
    }
  }
};

// FullCalendar options
const calendarOptions = computed<CalendarOptions>(() => ({
  plugins: [dayGridPlugin, timeGridPlugin, listPlugin, interactionPlugin],
  initialView: state.currentView,
  headerToolbar: false as const,
  locale: 'vi',
  firstDay: 1, // Monday
  editable: !isLimitedEditMode.value, // Disable drag/drop when status is Confirmed or later
  selectable: true,
  dayMaxEvents: true,
  weekends: true,
  listDayFormat: { weekday: 'short', day: '2-digit', month: '2-digit' } as any,
  listDaySideFormat: false as any,
  views: {
    dayGridYear: {
      type: 'dayGrid',
      duration: { years: 1 },
      buttonText: $t('lz.label.base.year')
    }
  },
  events: async (info: any, successCallback: any, failureCallback: any) => {
    try {
      await loadCalendarEvents(info.start, info.end);
      
      // Map filtered schedules to FullCalendar event format
      const events = state.filteredSchedules.map(schedule => {
        const status: any = schedule.status;
        
        // Get color from config
        const statusConfig = getConfigItemByCode(BaseConfigTypeEnum.TmsDutyState, status);
        const tagType = statusConfig?.tagType || 'info';
        
        // Map tagType to color
        const colorMap: Record<string, string> = {
          'info': 'var(--el-color-info)',
          'primary': 'var(--el-color-primary)',
          'warning': 'var(--el-color-warning)',
          'success': 'var(--el-color-success)',
          'danger': 'var(--el-color-danger)'
        };
        const color = colorMap[tagType] || 'var(--el-color-info)';
        
        return {
          id: schedule.id?.toString(),
          title: schedule.name || $t('lz.label.tmsDutySchedule.noName'),
          start: schedule.startTime,
          end: schedule.endTime,
          backgroundColor: color,
          borderColor: color,
          // Disable drag/drop for cancelled schedules
          editable: schedule.status !== 'cancel',
          extendedProps: {
            schedule: schedule
          }
        };
      });
      
      successCallback(events);
    } catch (error) {
      console.error('FullCalendar events callback - error:', error);
      failureCallback(error);
    }
  },
  dateClick: (arg: any) => {
    handleDateClick(arg);
  },
  eventClick: (info: any) => {
    handleEventClick(info);
  },
  eventDrop: async (info: EventDropArg) => {
    await handleEventDrop(info);
  },
  eventResize: async (info: any) => {
    await handleEventResize(info);
  },
  datesSet: (dateInfo: DatesSetArg) => {
    state.calendarApi = dateInfo.view.calendar;
    state.currentDate = dateInfo.view.calendar.getDate();
    // Update date range when view changes
    if (dateInfo.start && dateInfo.end) {
      state.queryParams.startDate = dateInfo.start;
      state.queryParams.endDate = dateInfo.end;
    }
  },
}));

// Load calendar events
/**
 * Load calendar events for the specified date range
 * Applies quick filter and converts schedules to FullCalendar event format
 * @param start - Start date of the range
 * @param end - End date of the range
 */
const loadCalendarEvents = async (start: Date, end: Date) => {
  try {
    options.loading = true;
    
    // Map quickFilter to DutyScheduleFilterMode
    let filterMode: DutyScheduleFilterMode | undefined = undefined;
    if (state.quickFilter === 'createdByMe') {
      filterMode = DutyScheduleFilterMode.NUMBER_1; // CreatedByMe = 1
    } else if (state.quickFilter === 'related') {
      filterMode = DutyScheduleFilterMode.NUMBER_2; // Related = 2
    } else if (state.quickFilter === 'all') {
      filterMode = DutyScheduleFilterMode.NUMBER_0; // All = 0
    }
    
    // Use POST method with body object
    const response = await getAPI(TmsDutyScheduleApi).apiTmsTmsdutyscheduleGetlistPost({
      name: state.searchText || undefined,
      startTime: start,
      endTime: end,
      userId: state.queryParams.employeeId || undefined,
      status: state.queryParams.status || undefined,
      filterMode: filterMode
    });
    
    if (response.data?.result) {
      state.allSchedules = response.data.result;
      applyFilters();
    }
  } catch (error: any) {
    console.error('Error loading calendar events:', error);
    ElMessage.error(error?.message || $t('lz.message.tmsDutySchedule.loadDataError'));
  } finally {
    options.loading = false;
  }
};

// Handle query
/**
 * Execute main query to load schedules
 * Loads data based on current view mode (calendar or list)
 */
const handleQuery = async () => {
  if (state.currentView === 'dayGridYear') {
    const startOfYear = new Date(state.currentYear, 0, 1);
    const endOfYear = new Date(state.currentYear, 11, 31, 23, 59, 59);
    await loadCalendarEvents(startOfYear, endOfYear);
  } else if (state.calendarApi) {
    state.calendarApi.refetchEvents();
  } else if (fullCalendar.value?.getApi) {
    const api = fullCalendar.value.getApi();
    api.refetchEvents();
  }
};

// Reset query
/**
 * Reset all filters to default values and reload data
 */
const resetQuery = () => {
  state.queryParams.employeeId = undefined;
  state.queryParams.dateRange = [];
  state.queryParams.startDate = undefined;
  state.queryParams.endDate = undefined;
  handleQuery();
};

/**
 * Handle date range change from calendar view
 * @param dates - Array of date strings [startDate, endDate]
 */
const handleDateRangeChange = (dates: string[]) => {
  if (dates && dates.length === 2) {
    state.queryParams.startDate = new Date(dates[0]);
    state.queryParams.endDate = new Date(dates[1]);
    if (fullCalendar.value?.getApi) {
      fullCalendar.value.getApi().gotoDate(dates[0]);
    }
  }
  handleQuery();
};

/**
 * Handle date range change from list view
 * @param dates - Array of date strings [startDate, endDate]
 */
const handleListDateChange = (dates: string[]) => {
  state.queryParams.dateRange = dates || [];
  if (dates && dates.length === 2) {
    state.queryParams.startDate = new Date(dates[0]);
    state.queryParams.endDate = new Date(dates[1]);
  }
  handleQuery();
};

/**
 * Handle status change from list view filter
 * @param status - Selected status value
 */
const handleListStatusChange = (status: any) => {
  state.queryParams.status = status;
  handleQuery();
};

/**
 * Handle employee change from list view filter
 * @param employeeId - Selected employee ID
 */
const handleListEmployeeChange = (employeeId: string | undefined) => {
  state.queryParams.employeeId = employeeId;
  handleQuery();
};

// Navigation handlers
/**
 * Navigate to previous period in calendar
 */
const handlePrev = () => {
  if (state.currentView === 'dayGridYear') {
    state.currentYear -= 1;
    handleQuery();
  } else if (fullCalendar.value?.getApi) {
    const api = fullCalendar.value.getApi();
    api.prev();
    state.currentDate = api.getDate();
    handleQuery();
  }
};

/**
 * Navigate to today's date in calendar
 */
const handleToday = () => {
  if (state.currentView === 'dayGridYear') {
    state.currentYear = new Date().getFullYear();
    handleQuery();
  } else if (fullCalendar.value?.getApi) {
    const api = fullCalendar.value.getApi();
    api.today();
    state.currentDate = api.getDate();
    handleQuery();
  }
};

/**
 * Navigate to next period in calendar
 */
const handleNext = () => {
  if (state.currentView === 'dayGridYear') {
    state.currentYear += 1;
    handleQuery();
  } else if (fullCalendar.value?.getApi) {
    const api = fullCalendar.value.getApi();
    api.next();
    state.currentDate = api.getDate();
    handleQuery();
  }
};

/**
 * Change calendar view mode
 * @param view - View type (dayGridMonth, timeGridWeek, timeGridDay, dayGridYear, listWeek, listMonth)
 */
const changeView = (view: 'dayGridMonth' | 'timeGridWeek' | 'timeGridDay' | 'dayGridYear' | 'listWeek' | 'listMonth') => {
  state.currentView = view;
  if (view === 'dayGridYear') {
    const calendarApi = fullCalendar.value?.getApi();
    if (calendarApi) {
      state.currentYear = calendarApi.getDate().getFullYear();
      state.currentDate = calendarApi.getDate();
    } else {
      state.currentYear = new Date().getFullYear();
      state.currentDate = new Date();
    }
    handleQuery();
  } else if (fullCalendar.value?.getApi) {
    const api = fullCalendar.value.getApi();
    api.changeView(view);
    state.currentDate = api.getDate();
    
    // Force refetch events after view change
    setTimeout(() => {
      const currentApi = fullCalendar.value?.getApi();
      if (currentApi) {
        currentApi.refetchEvents();
      }
    }, 100);
  }
};

/**
 * Handle click on month in year grid view
 * Switches to month view for the selected month
 * @param data - Object containing year and month
 */
const handleYearMonthClick = (data: { year: number; month: number }) => {
  // Switch to month view for selected month
  state.currentView = 'dayGridMonth';
  if (fullCalendar.value?.getApi) {
    const calendarApi = fullCalendar.value.getApi();
    calendarApi.gotoDate(new Date(data.year, data.month - 1, 1));
    calendarApi.changeView('dayGridMonth');
    handleQuery();
  }
};

/**
 * Handle click on event in year grid view
 * Opens detail drawer for the clicked schedule
 * @param event - Event object with id
 */
const handleYearEventClick = (event: any) => {
  const schedule = state.allSchedules.find((s: DutyScheduleOutput) => {
    const sid: any = s.id;
    return sid == event.id;
  });
  
  if (!schedule) {
    ElMessage.error($t('lz.message.tmsDutySchedule.scheduleNotFound'));
    return;
  }
  
  state.selectedDetail = schedule;
  state.drawerTitle = $t('lz.label.tmsDutySchedule.scheduleDetail');
  state.drawerVisible = true;
};

/**
 * Handle click on day in year grid view (show more events)
 * Switches to day view for the selected day
 * @param data - Object containing year, month, day and events
 */
const handleYearDayClick = (data: { year: number; month: number; day: number; events: any[] }) => {
  state.currentView = 'timeGridDay';
  if (fullCalendar.value?.getApi) {
    const calendarApi = fullCalendar.value.getApi();
    calendarApi.gotoDate(new Date(data.year, data.month - 1, data.day));
    calendarApi.changeView('timeGridDay');
    handleQuery();
  }
};

// Date click -> add dialog
/**
 * Handle click on date in calendar
 * Opens add schedule dialog with the selected date pre-filled
 * @param arg - FullCalendar date click argument
 */
const handleDateClick = (arg: any) => {
  state.scheduleDialogTitle = $t('lz.button.base.add');
  state.operateType = 'add';
  editScheduleRef.value?.openDialog({
    startDate: arg.date,
  });
};

/**
 * Handle click on event in calendar
 * Opens detail drawer with schedule information
 * @param info - FullCalendar event click info
 */
const handleEventClick = async (info: any) => {
  try {
    // Get schedule from extendedProps (already stored when creating event)
    const schedule = info.event.extendedProps?.schedule;
    
    if (!schedule) {
      // Fallback: find by ID if extendedProps not available
      const scheduleId = parseInt(info.event.id);
      if (!scheduleId) return;
      
      const found = state.allSchedules.find((s: DutyScheduleOutput) => {
        const sid: any = s.id;
        return sid == scheduleId; // Use == for type coercion
      });
      
      if (!found) {
        ElMessage.error($t('lz.message.tmsDutySchedule.scheduleNotFound'));
        return;
      }
      
      state.selectedDetail = found;
    } else {
      // Use schedule from extendedProps directly
      state.selectedDetail = schedule;
    }
    
    state.drawerTitle = $t('lz.label.tmsDutySchedule.scheduleDetail');
    state.drawerVisible = true;
    state.activeTab = 'info';
  } catch (error: any) {
    console.error('Error loading schedule:', error);
    ElMessage.error(error?.message || $t('lz.message.base.errorFetchingData'));
  }
};

// Handle event drop
/**
 * Handle drag and drop of event in calendar
 * Updates schedule start/end times and refreshes calendar
 * @param info - FullCalendar event drop info
 */
const handleEventDrop = async (info: any) => {
  try {
    const newStart = info.event.start;
    const newEnd = info.event.end || newStart;

    // Get schedule from extendedProps (already stored when creating event)
    let currentSchedule = info.event.extendedProps?.schedule;
    
    if (!currentSchedule) {
      // Fallback: find by ID
      const scheduleId = parseInt(info.event.id);
      if (!scheduleId) {
        info.revert();
        return;
      }
      
      currentSchedule = state.allSchedules.find((s: DutyScheduleOutput) => {
        const sid: any = s.id;
        return sid == scheduleId;
      });
    }
    
    if (!currentSchedule || !currentSchedule.id) {
      info.revert();
      ElMessage.error($t('lz.message.tmsDutySchedule.scheduleNotFound'));
      return;
    }
    
    // Check if schedule is confirmed or later - prevent editing
    const status = currentSchedule.status;
    if (status === 'confirm' || status === 'process' || status === 'finish') {
      info.revert();
      ElMessage.warning($t('lz.message.tmsDutySchedule.cannotChangeTimeConfirmed'));
      return;
    }
    
    // Update schedule with new times, keep all other data including details
    const updateParams: UpdateDutyScheduleInput = {
      id: currentSchedule.id,
      name: currentSchedule.name,
      startTime: newStart,
      endTime: newEnd,
      patrolTeamId: currentSchedule.patrolTeamId,
      status: currentSchedule.status,
      remark: currentSchedule.remark,
      // Keep details to preserve user assignments
      details: currentSchedule.details?.map((d: any) => ({
        userId: d.userId,
        role: d.role,
        status: d.status,
        remark: d.remark,
      })) || [],
    };

    await getAPI(TmsDutyScheduleApi).apiTmsTmsdutyscheduleUpdatePost(updateParams);
    ElMessage.success($t('lz.message.tmsDutySchedule.updateScheduleSuccess'));
    await handleQuery();
  } catch (error: any) {
    console.error('Error updating schedule:', error);
    ElMessage.error(error?.message || $t('lz.message.tmsDutySchedule.updateScheduleError'));
    info.revert();
  }
};

// Handle event resize
/**
 * Handle resize of event in calendar
 * Updates schedule end time and refreshes calendar
 * @param info - FullCalendar event resize info
 */
const handleEventResize = async (info: any) => {
  // Similar to event drop
  await handleEventDrop(info);
};

// Handle drawer close
/**
 * Handle detail drawer close event
 * Clears selected schedule data
 */
const handleDrawerClose = () => {
  state.drawerVisible = false;
  state.selectedDetail = null;
};

/**
 * Handle edit button click from detail drawer
 * Opens edit dialog with current schedule data
 */
const handleEditFromDrawer = () => {
  if (!state.selectedDetail) return;
  
  // Close drawer
  state.drawerVisible = false;
  
  // Open edit dialog
  state.scheduleDialogTitle = $t('lz.button.base.edit') + ' ' + $t('lz.label.tmsDutySchedule.scheduleDetails');
  state.operateType = 'edit';
  // Pass full schedule object with limited edit flag
  editScheduleRef.value?.openDialog({ 
    schedule: state.selectedDetail,
    isLimitedEdit: isLimitedEditMode.value,
  });
};

/**
 * Handle copy button click from detail drawer
 * Opens add dialog with copied schedule data
 */
const handleCopyFromDrawer = () => {
  if (!state.selectedDetail) return;
  
  // Close drawer
  state.drawerVisible = false;
  
  // Open add dialog with copied data
  state.scheduleDialogTitle = $t('lz.label.tmsDutySchedule.copySchedule');
  state.operateType = 'add';
  
  // Pass schedule data to copy
  editScheduleRef.value?.openDialog({ 
    copyFrom: state.selectedDetail 
  });
};

/**
 * Handle cancel button click from detail drawer
 * Shows confirmation and updates schedule status to 'cancel'
 */
const handleCancelSchedule = async () => {
  if (!state.selectedDetail?.id) return;
  
  try {
    await ElMessageBox.confirm(
      $t('lz.message.tmsDutySchedule.confirmCancelSchedule'),
      $t('lz.label.tmsDutySchedule.cancelSchedule'),
      {
        confirmButtonText: $t('lz.button.base.cancel'),
        cancelButtonText: $t('lz.button.base.close'),
        type: 'warning',
      }
    );
    
    // Update schedule status to 'cancel' using dedicated API
    await getAPI(TmsDutyScheduleApi).apiTmsTmsdutyscheduleUpdatestatusPost({
      id: state.selectedDetail.id!,
      status: 'cancel',
      reason: 'User cancelled',
    });
    
    ElMessage.success($t('lz.message.tmsDutySchedule.cancelScheduleSuccess'));
    
    // Update local state
    state.selectedDetail.status = 'cancel';
    
    // Refresh calendar
    await handleQuery();
  } catch (error: any) {
    if (error !== 'cancel') {
      console.error('Error cancelling schedule:', error);
      ElMessage.error(error?.message || $t('lz.message.tmsDutySchedule.cancelScheduleError'));
    }
  }
};

/**
 * Handle delete button click from detail drawer
 * Shows confirmation and deletes the schedule
 */
const handleDeleteFromDrawer = async () => {
  if (!state.selectedDetail?.id) return;
  
  try {
    await ElMessageBox.confirm(
      $t('lz.message.tmsDutySchedule.confirmDeleteContent'),
      $t('lz.message.tmsDutySchedule.confirmDelete'),
      {
        confirmButtonText: $t('lz.button.base.delete'),
        cancelButtonText: $t('lz.button.base.cancel'),
        type: 'warning',
      }
    );
    
    await getAPI(TmsDutyScheduleApi).apiTmsTmsdutyscheduleDeletePost({ id: state.selectedDetail.id });
    ElMessage.success($t('lz.message.base.deleteSuccess'));
    
    // Close drawer
    state.drawerVisible = false;
    state.selectedDetail = null;
    
    // Refresh calendar
    await handleQuery();
  } catch (error: any) {
    if (error !== 'cancel') {
      console.error('Error deleting schedule:', error);
      ElMessage.error(error?.message || $t('lz.message.tmsDutySchedule.deleteScheduleError'));
    }
  }
};

/**
 * Open change request dialog for the selected schedule
 */
const handleOpenChangeRequestDialog = () => {
  if (!state.selectedDetail) {
    ElMessage.warning($t('lz.message.tmsDutySchedule.selectSchedule'));
    return;
  }
  
  changeRequestDialogRef.value?.openDialog();
};

/**
 * Handle quick change request from user table
 * @param type - Type of request (Absent, Swap, Replace, Transfer)
 * @param userDetail - User detail object for the request
 */
const handleQuickChangeRequest = async (type: 'Absent' | 'Swap' | 'Replace' | 'Transfer', userDetail: any) => {
  if (!state.selectedDetail) {
    ElMessage.warning($t('lz.message.tmsDutySchedule.scheduleInfoNotFound'));
    return;
  }

  let confirmTitle = $t('lz.label.tmsDutySchedule.changeRequest');
  
  switch (type) {
    case 'Absent':
      confirmTitle = $t('lz.label.tmsDutySchedule.requestAbsent');
      break;
    case 'Swap':
      confirmTitle = $t('lz.label.tmsDutySchedule.swapShift');
      break;
    case 'Replace':
      confirmTitle = $t('lz.label.tmsDutySchedule.replace');
      break;
    case 'Transfer':
      confirmTitle = $t('lz.label.tmsDutySchedule.transferToOther');
      break;
  }

  try {
    // Use openDialogWithPreset for cleaner preset handling
    // Pre-fill targetUserId with the selected user for ALL request types
    const presetOptions: { type: 'Absent' | 'Swap' | 'Replace' | 'Transfer'; targetUserId?: string } = {
      type: type
    };
    
    // Pass targetUserId for all types when user is specified
    if (userDetail?.userId) {
      presetOptions.targetUserId = userDetail.userId;
    }
    
    changeRequestDialogRef.value?.openDialogWithPreset(presetOptions);
  } catch (error: any) {
    console.error('Error opening change request dialog:', error);
    ElMessage.error({
      message: error.message || $t('lz.message.tmsDutySchedule.openChangeRequestError'),
      duration: 3000,
      showClose: true
    });
  }
};

// ===== Personnel Management Functions =====

/**
 * Open dialog to add a new user to the schedule
 * Only available for leader/creator when schedule is confirmed
 */
const handleOpenAddUserDialog = () => {
  state.newUserForm = {
    userId: '',
    role: 'member',
    remark: ''
  };
  state.addUserDialogVisible = true;
};

/**
 * Close add user dialog
 */
const handleCloseAddUserDialog = () => {
  state.addUserDialogVisible = false;
};

/**
 * Submit add user to schedule
 */
const handleAddUserToSchedule = async () => {
  if (!state.selectedDetail?.id) {
    ElMessage.warning($t('lz.message.tmsDutySchedule.scheduleInfoNotFound'));
    return;
  }
  
  if (!state.newUserForm.userId) {
    ElMessage.warning($t('lz.message.tmsDutySchedule.selectEmployee'));
    return;
  }
  
  // Check if user already exists in schedule
  const existingUser = state.selectedDetail.details?.find(
    (d: any) => d.userId === state.newUserForm.userId
  );
  if (existingUser) {
    ElMessage.warning($t('lz.message.tmsDutySchedule.employeeAlreadyInSchedule'));
    return;
  }
  
  try {
    state.personnelActionLoading = true;
    
    await getAPI(TmsDutyScheduleApi).apiTmsTmsdutyscheduleAdduserPost({
      dutyScheduleId: state.selectedDetail.id,
      userId: state.newUserForm.userId,
      role: state.newUserForm.role,
      remark: state.newUserForm.remark || undefined
    });
    
    ElMessage.success($t('lz.message.tmsDutySchedule.addEmployeeSuccess'));
    state.addUserDialogVisible = false;
    
    // Refresh the schedule detail
    await refreshSelectedDetail();
    
  } catch (error: any) {
    console.error('Error adding user to schedule:', error);
    ElMessage.error({
      message: error.response?.data?.message || $t('lz.message.tmsDutySchedule.addEmployeeError'),
      duration: 3000
    });
  } finally {
    state.personnelActionLoading = false;
  }
};

/**
 * Open dialog to change user's role in the schedule
 * @param row - User detail row from table
 */
const handleOpenChangeRoleDialog = (row: any) => {
  state.changeRoleForm = {
    detailId: row.id,
    userId: row.userId,
    currentRole: row.role,
    newRole: row.role === 'leader' ? 'member' : 'leader',
    reason: ''
  };
  state.changeRoleDialogVisible = true;
};

/**
 * Close change role dialog
 */
const handleCloseChangeRoleDialog = () => {
  state.changeRoleDialogVisible = false;
};

/**
 * Submit change user role
 */
const handleChangeUserRole = async () => {
  if (!state.changeRoleForm.detailId) {
    ElMessage.warning($t('lz.message.tmsDutySchedule.detailNotFound'));
    return;
  }
  
  if (state.changeRoleForm.newRole === state.changeRoleForm.currentRole) {
    ElMessage.warning($t('lz.message.tmsDutySchedule.newRoleMustDifferent'));
    return;
  }
  
  try {
    state.personnelActionLoading = true;
    
    await getAPI(TmsDutyScheduleApi).apiTmsTmsdutyscheduleUpdateuserrolePost({
      detailId: state.changeRoleForm.detailId,
      role: state.changeRoleForm.newRole,
      reason: state.changeRoleForm.reason || undefined
    });
    
    ElMessage.success($t('lz.message.tmsDutySchedule.updateRoleSuccess'));
    state.changeRoleDialogVisible = false;
    
    // Refresh the schedule detail
    await refreshSelectedDetail();
    
  } catch (error: any) {
    console.error('Error updating user role:', error);
    ElMessage.error({
      message: error.response?.data?.message || $t('lz.message.tmsDutySchedule.updateRoleError'),
      duration: 3000
    });
  } finally {
    state.personnelActionLoading = false;
  }
};

/**
 * Remove user from schedule with confirmation
 * @param row - User detail row from table
 */
const handleRemoveUserFromSchedule = async (row: any) => {
  const userName = getUserRealName(row.userId);
  
  try {
    await ElMessageBox.confirm(
      $t('lz.message.tmsDutySchedule.confirmRemoveEmployee', { name: userName }),
      $t('lz.message.tmsDutySchedule.removeEmployeeTitle'),
      {
        confirmButtonText: $t('lz.button.base.delete'),
        cancelButtonText: $t('lz.button.base.cancel'),
        type: 'warning',
        confirmButtonClass: 'el-button--danger'
      }
    );
    
    state.personnelActionLoading = true;
    
    await getAPI(TmsDutyScheduleApi).apiTmsTmsdutyscheduleRemoveuserPost({
      detailId: row.id,
      reason: undefined
    });
    
    ElMessage.success($t('lz.message.tmsDutySchedule.removeEmployeeSuccess', { name: userName }));
    
    // Refresh the schedule detail
    await refreshSelectedDetail();
    
  } catch (error: any) {
    if (error !== 'cancel' && error?.message !== 'cancel') {
      console.error('Error removing user from schedule:', error);
      ElMessage.error({
        message: error.response?.data?.message || $t('lz.message.tmsDutySchedule.removeEmployeeError'),
        duration: 3000
      });
    }
  } finally {
    state.personnelActionLoading = false;
  }
};

/**
 * Refresh the selected schedule detail after personnel changes
 */
const refreshSelectedDetail = async () => {
  if (!state.selectedDetail?.id) return;
  
  try {
    // Reload the schedule by fetching via list API with the specific schedule in time range
    const response = await getAPI(TmsDutyScheduleApi).apiTmsTmsdutyscheduleGetlistPost({
      startTime: state.selectedDetail.startTime ? new Date(state.selectedDetail.startTime) : undefined,
      endTime: state.selectedDetail.endTime ? new Date(state.selectedDetail.endTime) : undefined,
    });
    
    if (response.data?.result) {
      // Find the specific schedule by ID
      const updatedSchedule = response.data.result.find(
        (s: DutyScheduleOutput) => s.id === state.selectedDetail!.id
      );
      if (updatedSchedule) {
        state.selectedDetail = updatedSchedule;
      }
    }
    
    // Also refresh the calendar
    await handleQuery();
    
  } catch (error: any) {
    console.error('Error refreshing schedule detail:', error);
  }
};

/**
 * Get list of available users that are not already in the schedule
 */
const availableUsersForSchedule = computed(() => {
  if (!state.selectedDetail?.details) return state.employeeList;
  
  const existingUserIds = state.selectedDetail.details.map((d: any) => d.userId);
  return state.employeeList.filter((u: any) => !existingUserIds.includes(u.username));
});

// ===== End Personnel Management Functions =====

// ===== History Tab Functions =====

/**
 * Load history for current schedule
 */
const loadScheduleHistory = async () => {
  if (!state.selectedDetail?.id) return;
  
  try {
    state.historyLoading = true;
    
    const response = await getAPI(TmsDutyScheduleApi).apiTmsTmsdutyscheduleGethistoryPost({
      dutyScheduleId: state.selectedDetail.id
    });
    
    state.historyData = response.data?.result || [];
    
  } catch (error: any) {
    console.error('Error loading schedule history:', error);
    state.historyData = [];
  } finally {
    state.historyLoading = false;
  }
};

/**
 * Format datetime for history display
 */
const formatHistoryDateTime = (dateTime: any) => {
  if (!dateTime) return '';
  const date = new Date(dateTime);
  const day = String(date.getDate()).padStart(2, '0');
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const year = date.getFullYear();
  const hours = String(date.getHours()).padStart(2, '0');
  const minutes = String(date.getMinutes()).padStart(2, '0');
  return `${day}/${month}/${year} ${hours}:${minutes}`;
};

/**
 * Get config item for action type (history)
 */
const getHistoryActionConfig = (actionType: string) => {
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
 * Get action type label for history
 */
const getHistoryActionTypeLabel = (actionType: string) => {
  const configItem = getHistoryActionConfig(actionType);
  return configItem?.name || actionType;
};

/**
 * Get action type tag type for history
 */
const getHistoryActionTypeTagType = (actionType: string) => {
  const configItem = getHistoryActionConfig(actionType);
  return configItem?.tagType || 'info';
};

/**
 * Get timeline dot class based on action type
 */
const getTimelineDotClass = (actionType: string) => {
  const tagType = getHistoryActionTypeTagType(actionType);
  const classMap: Record<string, string> = {
    'success': 'dot-success',
    'primary': 'dot-primary',
    'warning': 'dot-warning',
    'danger': 'dot-danger',
    'info': 'dot-info'
  };
  return classMap[tagType] || 'dot-primary';
};

// ===== End History Tab Functions =====

/**
 * Handle detail tab change
 * Load history data when switching to history tab
 */
const handleDetailTabChange = (tabName: string) => {
  if (tabName === 'history') {
    loadScheduleHistory();
  }
};

/**
 * Handle successful change request submission
 * Shows success message and reloads pending count
 */
const handleChangeRequestSuccess = () => {
  // ElMessage.success($t('lz.message.tmsDutySchedule.changeRequestSentSuccess'));
  loadPendingChangeRequestCount();
};

/**
 * Handle create new schedule from swap dialog
 * Closes change request dialog and opens create schedule dialog
 */
const handleCreateNewScheduleFromSwap = () => {
  state.drawerVisible = false;
  state.scheduleDialogTitle = $t('lz.button.base.add');
  state.operateType = 'add';
  
  // Get date from current schedule as suggestion
  let startDate: Date | undefined;
  if (state.selectedDetail?.startTime) {
    startDate = new Date(state.selectedDetail.startTime);
  }
  
  editScheduleRef.value?.openDialog({
    startDate,
  });
};

/**
 * Open change request list dialog
 */
const handleOpenChangeRequestList = () => {
  changeRequestListRef.value?.openDialog();
};

/**
 * Handle refresh when change request list has changes (approve/reject/cancel)
 * Reloads both the schedule data and pending count badge
 */
const handleChangeRequestListRefresh = () => {
  handleQuery();
  loadPendingChangeRequestCount();
};

/**
 * Load count of pending change requests for badge display
 */
const loadPendingChangeRequestCount = async () => {
  try {
    const response = await getAPI(TmsDutyScheduleApi).apiTmsTmsdutyscheduleGetchangerequestsPost({
      viewMode: 2, // PendingApproval
      status: 'Pending'
    });
    state.pendingChangeRequestCount = response.data?.result?.length || 0;
  } catch (error) {
    console.error('Error loading pending change request count:', error);
  }
};

/**
 * Handle refresh after status update from DetailInfo component or EditDutySchedule
 * Reloads calendar events to reflect status changes
 * Also reloads the specific schedule detail if drawer is open
 */
const handleRefreshAfterStatusUpdate = async () => {
  try {
    const scheduleId = state.selectedDetail?.id;
    const isDrawerOpen = state.drawerVisible;
    
    // If detail drawer is open and we have an ID, reload that specific schedule first
    if (scheduleId && isDrawerOpen) {
      // Use queryParams from state for date range
      const queryParams: DutyScheduleInput = {
        startTime: state.queryParams.startDate,
        endTime: state.queryParams.endDate,
      };
      
      const response = await getAPI(TmsDutyScheduleApi).apiTmsTmsdutyscheduleGetlistPost(queryParams);
      if (response.data?.result) {
        const updatedSchedule = response.data.result.find((s: DutyScheduleOutput) => s.id === scheduleId);
        if (updatedSchedule) {
          // Force Vue reactivity by creating new object
          state.selectedDetail = JSON.parse(JSON.stringify(updatedSchedule));
        }
      }
    }
    
    // Refresh calendar/list
    await handleQuery();
    
    // Wait for Vue to complete rendering
    await nextTick();
    
    // For calendar view: refetch events
    if (state.displayMode === 'calendar' && fullCalendar.value?.getApi) {
      fullCalendar.value.getApi().refetchEvents();
    }
  } catch (error: any) {
    console.error('Error refreshing after status update:', error);
    ElMessage.error($t('lz.message.base.errorFetchingData'));
  }
};

/**
 * Handle add new schedule button click
 * Opens add dialog with current view date pre-filled
 */
const handleAddDetail = () => {
  state.scheduleDialogTitle = $t('lz.button.base.add');
  state.operateType = 'add';
  
  // Get current view date range
  let startDate: Date | undefined;
  if (fullCalendar.value?.getApi) {
    const view = fullCalendar.value.getApi().view;
    startDate = view.activeStart;
  }
  
  editScheduleRef.value?.openDialog({
    startDate,
  });
};

// Load team list
/**
 * Load all patrol teams for selection dropdown
 */
const loadTeamList = async () => {
  try {
    const response = await getAPI(TmsPatrolTeamApi).apiTmsTmspatrolteamListGet(
      undefined, // name
      undefined, // code
      1 // status - only active teams
    );
    
    if (response.data?.result) {
      state.teamList = response.data.result.map((team: PatrolTeamOutput) => ({
        id: team.id,
        name: team.name,
        code: team.code,
      }));
    }
  } catch (error: any) {
    console.error('Error loading team list:', error);
  }
};

// Load employee list
/**
 * Load all employees for selection dropdown
 */
const loadEmployeeList = async () => {
  try {
    const response = await getAPI(SysUserApi).apiSystemSysuserListPost({
      username: undefined,
    });
    if (response.data?.result) {
      state.employeeList = response.data.result.map((user: any) => ({
        id: user.id,
        name: user.realName || user.name || user.username,
        username: user.username,
        account: user.account,
        realName: user.realName,
      }));
    }
  } catch (error: any) {
    console.error('Error loading employee list:', error);
  }
};

/**
 * Get user's real name by username
 * @param username - Username to look up
 * @returns User's real name or username if not found
 */
const getUserRealName = (username: string) => {
  const user = state.employeeList.find((u: any) => u.username === username);
  return user?.name || username;
};

/**
 * Sort employee list by role (leader first) then alphabetically by name
 * @returns Sorted employee details list
 */
const sortedEmployeeList = computed(() => {
  if (!state.selectedDetail?.details) return [];
  
  return [...state.selectedDetail.details].sort((a: any, b: any) => {
    // First sort by role: leader (1) before member (0)
    const roleA = a.role === 'leader' ? 0 : 1;
    const roleB = b.role === 'leader' ? 0 : 1;
    
    if (roleA !== roleB) {
      return roleA - roleB;
    }
    
    // Then sort alphabetically by real name
    const nameA = getUserRealName(a.userId).toLowerCase();
    const nameB = getUserRealName(b.userId).toLowerCase();
    return nameA.localeCompare(nameB, 'vi');
  });
});

// Initialize
onMounted(async () => {
  // Load teams and employees
  await Promise.all([
    loadTeamList(),
    loadEmployeeList(),
    loadPendingChangeRequestCount()
  ]);
  
  // Set default date range to current month
  const now = new Date();
  const firstDay = new Date(now.getFullYear(), now.getMonth(), 1);
  const lastDay = new Date(now.getFullYear(), now.getMonth() + 1, 0);
  state.queryParams.dateRange = [
    firstDay.toISOString().split('T')[0],
    lastDay.toISOString().split('T')[0],
  ];
  state.queryParams.startDate = firstDay;
  state.queryParams.endDate = lastDay;
  
  // Wait for calendar to initialize
  await new Promise(resolve => setTimeout(resolve, 100));
  await handleQuery();
});
</script>

<style lang="scss" scoped>
.tms-duty-schedule-container {
  height: calc(100vh - 140px);
  display: flex;
  flex-direction: column;
  overflow: hidden;
  position: relative;
}

// Margin bottom utility
.mb5 {
  margin-bottom: 5px;
}

.full-table {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
  min-height: 0;
}

.full-table :deep(.el-card__body) {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: auto;
  padding: 15px;
  min-height: 0;
}

.full-table :deep(.fc) {
  height: 100% !important;
  min-height: 500px;
}

.full-table :deep(.fc-view-harness) {
  height: 100% !important;
}

/* Form styles consistent with system app */
:deep(.el-form-item) {
  margin-bottom: 5px;
}

:deep(.el-input),
:deep(.el-select) {
  width: 100%;
}

:deep(.el-radio-group) {
  width: 100%;
}

/* Calendar Controls Wrapper */
.calendar-controls-wrapper {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 15px;
  flex-wrap: wrap;
}

.controls-left {
  display: flex;
  align-items: center;
  gap: 10px;
}

.controls-center {
  flex: 1;
  display: flex;
  justify-content: center;
  align-items: center;
  min-width: 200px;
}

.controls-right {
  display: flex;
  align-items: center;
  gap: 10px;
}

.current-period {
  .period-text {
    font-size: 18px;
    font-weight: 600;
    color: var(--el-text-color-primary);
    white-space: nowrap;
  }
}

/* FAB Button */
.fab-button {
  position: fixed;
  bottom: 30px;
  left: 50%;
  transform: translateX(-50%);
  z-index: 999;
  width: 60px;
  height: 60px;
  border-radius: 50%;
  font-size: 24px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
}

.fab-button:hover {
  transform: translateX(-50%) scale(1.1);
  box-shadow: 0 6px 16px rgba(0, 0, 0, 0.2);
}

/* Detail Dialog */
.detail-actions-top {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
  gap: 20px;
  flex-wrap: wrap;
}

.action-buttons-group {
  display: flex;
  flex-shrink: 0;
}

/* Status Progress Inline */
.status-progress-inline {
  display: flex;
  align-items: center;
  gap: 0;
  padding: 8px 16px;
  background: linear-gradient(135deg, #f5f7fa 0%, #ffffff 100%);
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 6px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.04);
  flex: 1;
  min-width: 0;
}

.status-badge {
  display: flex;
  align-items: center;
  gap: 6px;
  flex-shrink: 0;
  
  .status-icon {
    font-size: 16px;
    color: var(--el-color-primary);
  }
  
  .status-label {
    font-size: 13px;
    font-weight: 500;
    color: var(--el-text-color-secondary);
  }
}

.process-steps-container {
  flex: 1;
  min-width: 0;
  overflow-x: auto;
  
  &::-webkit-scrollbar {
    height: 4px;
  }
  
  &::-webkit-scrollbar-track {
    background: transparent;
  }
  
  &::-webkit-scrollbar-thumb {
    background: var(--el-border-color);
    border-radius: 2px;
  }
}

.process-steps-mini {
  display: flex;
  padding-top:3px;
  align-items: flex-start;
  gap: 0;
  min-width: max-content;
}

.process-step-mini {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 4px;
  position: relative;
  cursor: pointer;
  transition: all 0.3s ease;
  
  &:hover:not(.step-completed):not(.step-cancelled) {
    transform: translateY(-2px);
    
    .step-circle {
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
    }
    
    .step-label {
      color: var(--el-color-primary);
    }
  }
  
  // Disable hover for completed and cancelled steps
  &.step-completed,
  &.step-cancelled {
    cursor: not-allowed;
  }
  
  .step-circle {
    width: 24px;
    height: 24px;
    border-radius: 50%;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 12px;
    font-weight: 600;
    background: #E4E7ED;
    color: #909399;
    border: 2px solid #E4E7ED;
    transition: all 0.3s ease;
    z-index: 1;
    
    .el-icon {
      font-size: 14px;
    }
  }
  
  .step-label {
    font-size: 11px;
    color: var(--el-text-color-regular);
    white-space: nowrap;
    font-weight: 500;
  }
  
  // Completed step
  &.step-completed {
    .step-circle {
      background: linear-gradient(135deg, #67C23A 0%, #85CE61 100%);
      border-color: #67C23A;
      color: white;
      box-shadow: 0 2px 6px rgba(103, 194, 58, 0.3);
    }
    
    .step-label {
      color: var(--el-color-success);
      font-weight: 600;
    }
  }
  
  // Active step - different colors for each step
  &.step-active {
    .step-label {
      font-weight: 600;
    }
    
    // Registered (info - grey)
    &:nth-child(1) {
      .step-circle {
        background: linear-gradient(135deg, #909399 0%, #A6A9AD 100%);
        border-color: #909399;
        color: white;
        animation: pulse-step 2s ease-in-out infinite;
        box-shadow: 0 2px 8px rgba(144, 147, 153, 0.4);
      }
      .step-label {
        color: var(--el-color-info);
      }
    }
    
    // Confirmed (primary - blue)
    &:nth-child(3) {
      .step-circle {
        background: linear-gradient(135deg, #409EFF 0%, #66B1FF 100%);
        border-color: #409EFF;
        color: white;
        animation: pulse-step 2s ease-in-out infinite;
        box-shadow: 0 2px 8px rgba(64, 158, 255, 0.4);
      }
      .step-label {
        color: var(--el-color-primary);
      }
    }
    
    // InProgress (warning - orange)
    &:nth-child(5) {
      .step-circle {
        background: linear-gradient(135deg, #E6A23C 0%, #EEBE77 100%);
        border-color: #E6A23C;
        color: white;
        animation: pulse-step 2s ease-in-out infinite;
        box-shadow: 0 2px 8px rgba(230, 162, 60, 0.4);
      }
      .step-label {
        color: var(--el-color-warning);
      }
    }
    
    // Completed (success - green)
    &:nth-child(7) {
      .step-circle {
        background: linear-gradient(135deg, #67C23A 0%, #85CE61 100%);
        border-color: #67C23A;
        color: white;
        animation: pulse-step 2s ease-in-out infinite;
        box-shadow: 0 2px 8px rgba(103, 194, 58, 0.4);
      }
      .step-label {
        color: var(--el-color-success);
      }
    }
  }
  
  // Cancelled step
  &.step-cancelled {
    .step-circle {
      background: #F56C6C;
      border-color: #F56C6C;
      color: white;
      opacity: 0.7;
    }
    
    .step-label {
      color: var(--el-color-danger);
      text-decoration: line-through;
    }
  }
  
  // Pending step - clickable to next status
  &.step-pending {
    .step-circle {
      background: #F5F7FA;
      border-color: #DCDFE6;
      color: #C0C4CC;
    }
    
    .step-label {
      color: var(--el-text-color-placeholder);
    }
    
    // Disable pointer for far future steps
    &:not(.step-next) {
      cursor: not-allowed;
      
      &:hover {
        transform: none;
      }
    }
  }
  
  // Next step that can be clicked
  &.step-next {
    cursor: pointer;
    
    .step-circle {
      border-color: var(--el-color-primary-light-5);
      background: var(--el-fill-color);
      color: var(--el-text-color-secondary);
    }
    
    .step-label {
      color: var(--el-text-color-secondary);
    }
    
    &:hover {
      .step-circle {
        border-color: var(--el-color-primary);
        background: var(--el-color-primary-light-9);
        color: var(--el-color-primary);
        transform: scale(1.05);
      }
      
      .step-label {
        color: var(--el-color-primary);
        font-weight: 600;
      }
    }
  }
}

.step-connector {
  width: 32px;
  height: 2px;
  background: #DCDFE6;
  margin-top: 13px;
  position: relative;
  transition: all 0.3s ease;
  
  &.step-completed {
    background: linear-gradient(90deg, #67C23A 0%, #85CE61 100%);
    box-shadow: 0 1px 3px rgba(103, 194, 58, 0.3);
  }
  
  &.step-active {
    background: linear-gradient(90deg, #DCDFE6 0%, var(--el-color-primary) 100%);
    animation: flow 1.5s ease-in-out infinite;
  }
}

@keyframes pulse-step {
  0%, 100% {
    transform: scale(1);
  }
  50% {
    transform: scale(1.08);
  }
}

@keyframes flow {
  0% {
    background-position: 0% 50%;
  }
  50% {
    background-position: 100% 50%;
  }
  100% {
    background-position: 0% 50%;
  }
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

/* FullCalendar overrides */
:deep(.fc) {
  font-family: inherit;
  height: 100%;
}

:deep(.fc-header-toolbar) {
  margin-bottom: 1em;
}

:deep(.fc-event) {
  cursor: pointer;
}

:deep(.fc-event:hover) {
  opacity: 0.8;
}

:deep(.fc-scrollgrid) {
  border-color: var(--el-border-color) !important;
}

:deep(.fc-col-header-cell) {
  background-color: var(--el-fill-color-light);
}

/* Form Item Label Alignment */
:deep(.el-form-item__label) {
  height: 32px;
  line-height: 32px;
  display: flex;
  align-items: center;
}

:deep(.el-form-item__content) {
  line-height: 32px;
}

/* User Actions in Employee Table */
.user-actions {
  display: flex;
  gap: 8px;
  justify-content: center;
  flex-wrap: wrap;
}

/* Horizontal Timeline Styles */
.history-timeline-container {
  padding: 15px 0;
}

.history-timeline-scroll {
  overflow-x: auto;
  overflow-y: hidden;
  padding-bottom: 15px;
  
  /* Custom scrollbar */
  &::-webkit-scrollbar {
    height: 8px;
  }
  
  &::-webkit-scrollbar-track {
    background: var(--el-fill-color-light);
    border-radius: 4px;
  }
  
  &::-webkit-scrollbar-thumb {
    background: var(--el-color-primary-light-5);
    border-radius: 4px;
    
    &:hover {
      background: var(--el-color-primary);
    }
  }
}

.history-timeline {
  display: flex;
  gap: 20px;
  padding: 10px 20px;
  min-width: max-content;
}

.timeline-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  min-width: 200px;
  max-width: 250px;
}

.timeline-dot-container {
  position: relative;
  display: flex;
  align-items: center;
  width: 100%;
  justify-content: center;
  margin-bottom: 12px;
}

.timeline-line {
  position: absolute;
  left: calc(50% + 14px);
  width: calc(100% + 6px);
  height: 2px;
  background: linear-gradient(90deg, var(--el-color-primary) 0%, var(--el-color-primary-light-5) 100%);
  top: 50%;
  transform: translateY(-50%);
}

.timeline-dot {
  width: 28px;
  height: 28px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #fff;
  z-index: 1;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
  transition: all 0.3s ease;
  
  &:hover {
    transform: scale(1.1);
  }
  
  &.dot-success {
    background: linear-gradient(135deg, #67C23A 0%, #85CE61 100%);
  }
  
  &.dot-primary {
    background: linear-gradient(135deg, #409EFF 0%, #66b1ff 100%);
  }
  
  &.dot-warning {
    background: linear-gradient(135deg, #E6A23C 0%, #EEBE77 100%);
  }
  
  &.dot-danger {
    background: linear-gradient(135deg, #F56C6C 0%, #F89898 100%);
  }
  
  &.dot-info {
    background: linear-gradient(135deg, #909399 0%, #B1B3B8 100%);
  }
}

.timeline-card {
  background: var(--el-bg-color);
  border: 1px solid var(--el-border-color-light);
  border-radius: 8px;
  padding: 12px;
  width: 100%;
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.08);
  transition: all 0.3s ease;
  
  &:hover {
    border-color: var(--el-color-primary-light-5);
    box-shadow: 0 4px 16px rgba(64, 158, 255, 0.15);
    transform: translateY(-2px);
  }
}

.timeline-header {
  display: flex;
  justify-content: center;
  margin-bottom: 8px;
}

.timeline-time {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 4px;
  font-size: 12px;
  color: var(--el-color-primary);
  margin-bottom: 10px;
  padding-bottom: 8px;
  border-bottom: 1px dashed var(--el-border-color-lighter);
}

.timeline-body {
  font-size: 12px;
}

.timeline-row {
  display: flex;
  flex-direction: column;
  margin-bottom: 6px;
  
  &:last-child {
    margin-bottom: 0;
  }
}

.timeline-label {
  color: var(--el-text-color-secondary);
  font-size: 11px;
  margin-bottom: 2px;
}

.timeline-value {
  color: var(--el-text-color-primary);
  display: flex;
  align-items: center;
  gap: 4px;
  
  &.reason {
    font-style: italic;
    color: var(--el-color-warning);
  }
}

/* Responsive */
@media (max-width: 1200px) {
  .calendar-controls-wrapper {
    flex-direction: column;
    align-items: stretch;
    gap: 10px;
  }
  
  .controls-left,
  .controls-center,
  .controls-right {
    width: 100%;
    justify-content: center;
  }
}

@media (max-width: 768px) {
  .tms-duty-schedule-container {
    height: calc(100vh - 100px);
  }
  
  .calendar-controls-wrapper {
    gap: 8px;
  }
}
</style>
