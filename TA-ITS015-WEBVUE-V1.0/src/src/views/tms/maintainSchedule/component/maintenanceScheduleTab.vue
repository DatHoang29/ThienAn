<template>
  <div class="maintenance-schedule-tab">
    <!-- Filter Card -->
    <el-card shadow="hover" :body-style="{ padding: '5px 5px 0 5px', display: 'flex', width: '100%', alignItems: 'start' }">
      <el-form 
        :model="state.queryParams" 
        ref="queryForm" 
        :show-message="false" 
        :inlineMessage="true"
        label-width="auto" 
        label-position="left"
        style="flex: 1 1 0%"
      >
        <el-row :gutter="10">
          <!-- Quick Filter -->
          <el-col class="mb5" :xs="24" :sm="24" :md="24" :lg="24" :xl="24">
            <el-form-item :label="$t('lz.label.tmsMaintenanceSchedule.quickFilter')" prop="quickFilter">
              <el-radio-group v-model="state.quickFilter" @change="handleQuickFilter" size="small">
                <el-radio-button label="createdByMe">{{ $t('lz.label.tmsMaintenanceSchedule.createdByMe') }}</el-radio-button>
                <el-radio-button label="related">{{ $t('lz.label.tmsMaintenanceSchedule.assignedToMe') }}</el-radio-button>
                <el-radio-button label="all">{{ $t('lz.label.base.all') }}</el-radio-button>
              </el-radio-group>
            </el-form-item>
          </el-col>

          <!-- Search Text -->
          <el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
            <el-form-item :label="$t('lz.button.base.search')" prop="searchText">
              <el-input 
                v-model="state.searchText" 
                :placeholder="$t('lz.label.tmsMaintenanceSchedule.searchPlaceholder')"
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

          <!-- Status Filter -->
          <el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
            <el-form-item :label="$t('lz.entity.base.status')" prop="status">
              <el-select 
                v-model="state.queryParams.status" 
                :placeholder="$t('lz.label.tmsMaintenanceSchedule.selectStatus')"
                clearable
                @change="handleQuery"
              >
                <el-option 
                  v-for="item in statusOptions" 
                  :key="item.value" 
                  :label="item.label" 
                  :value="item.value" 
                />
              </el-select>
            </el-form-item>
          </el-col>

          <!-- Priority Filter -->
          <el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
            <el-form-item :label="$t('lz.entity.base.priority')" prop="priority">
              <el-select 
                v-model="state.queryParams.priority" 
                :placeholder="$t('lz.label.tmsMaintenanceSchedule.selectPriority')"
                clearable
                @change="handleQuery"
              >
                <el-option 
                  v-for="item in priorityOptions" 
                  :key="item.value" 
                  :label="item.label" 
                  :value="item.value" 
                />
              </el-select>
            </el-form-item>
          </el-col>

          <!-- Equipment Type Filter -->
          <el-col class="mb5" :xs="24" :sm="12" :md="8" :lg="6" :xl="6">
            <el-form-item :label="$t('lz.entity.tms.equipmentType')" prop="equipmentTypeId">
              <el-select 
                v-model="state.queryParams.equipmentTypeId" 
                :placeholder="$t('lz.label.tmsMaintenanceSchedule.selectEquipmentType')"
                clearable
                filterable
                @change="handleQuery"
              >
                <el-option
                  v-for="type in state.equipmentTypeList"
                  :key="type.id"
                  :label="type.name"
                  :value="type.id"
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
          :loading="state.loading" 
        />
      </el-row>
    </el-card>

    <!-- View Controls Card - Calendar Mode -->
    <el-card shadow="hover" style="margin-top: 5px;" v-if="state.displayMode === 'calendar'">
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
            <el-button @click="handleToday" size="default">{{ $t('lz.label.tmsMaintenanceSchedule.today') }}</el-button>
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
                <el-dropdown-item command="timeGridWeek">{{ $t('lz.label.base.week') }}</el-dropdown-item>
                <el-dropdown-item command="dayGridYear">{{ $t('lz.label.base.year') }}</el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>

          <el-button type="primary" style="margin-left: 10px" @click="handleAddSchedule">
            <el-icon><ele-Plus /></el-icon>
            {{ $t('lz.label.tmsMaintenanceSchedule.addSchedule') }}
          </el-button>
        </div>
      </div>
    </el-card>

    <!-- View Controls Card - List Mode -->
    <el-card shadow="hover" style="margin-top: 5px;" v-else>
      <div style="display: flex; justify-content: space-between; align-items: center;">
        <el-segmented v-model="state.displayMode" :options="displayModeOptions" size="default" />
        
        <el-button type="primary" @click="handleAddSchedule">
          <el-icon><ele-Plus /></el-icon>
          {{ $t('lz.label.tmsMaintenanceSchedule.addSchedule') }}
        </el-button>
      </div>
    </el-card>

    <!-- Calendar View -->
    <el-card class="full-table" shadow="hover" style="margin-top: 5px" v-if="state.displayMode === 'calendar'">
      <!-- Year Grid View -->
      <YearGridView
        v-if="state.currentView === 'dayGridYear'"
        :year="state.currentYear"
        :events="calendarEvents"
        @month-click="handleYearMonthClick"
        @event-click="handleYearEventClick"
        @day-click="handleYearDayClick"
      />
      <!-- Regular Calendar View -->
      <FullCalendar v-else :options="calendarOptions" ref="fullCalendar" />
    </el-card>

    <!-- List View -->
    <el-card class="full-table" shadow="hover" style="margin-top: 5px" v-else>
      <vxe-grid 
        ref="xGrid" 
        class="xGrid-style" 
        v-bind="gridOptions" 
        v-on="gridEvents"
      >
        <template #toolbar_tools>
          <TableHeader 
            :show-add="false"
            :show-delete="false"
            @refresh="loadSchedules" 
            @custom-column="handleCustomColumn"
            :loading="state.loading"
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

        <template #scheduledDate_default="{ row }">
          {{ formatDate(row.scheduledDate) }}
        </template>

        <template #dueDate_default="{ row }">
          {{ formatDate(row.dueDate) }}
        </template>

        <template #priority_default="{ row }">
          <el-tag :type="getPriorityTagType(row.priority)" size="small">
            {{ getPriorityLabel(row.priority) }}
          </el-tag>
        </template>

        <template #status_default="{ row }">
          <el-tag :type="getStatusTagType(row.status)" size="small">
            {{ getStatusLabel(row.status) }}
          </el-tag>
        </template>

        <template #equipmentCount_default="{ row }">
          <!-- <el-badge :value="row.completedEquipmentCount || 0" type="success" :max="99"> -->
            <span style="padding-right: 15px;">{{ row.completedEquipmentCount || 0 }} / {{ row.equipmentCount || 0 }}</span>
          <!-- </el-badge> -->
        </template>

        <template #taskCount_default="{ row }">
          <!-- <el-badge :value="row.completedTaskCount || 0" type="success" :max="99"> -->
            <span style="padding-right: 15px;">{{ row.completedTaskCount || 0  }} / {{ row.taskCount || 0 }}</span>
          <!-- </el-badge> -->
        </template>

        <template #row_buttons="{ row }">
          <el-tooltip :content="$t('lz.button.base.detail')" placement="top">
            <el-button icon="ele-View" text type="primary" @click="handleViewDetail(row)"></el-button>
          </el-tooltip>
          <el-tooltip :content="$t('lz.button.base.edit')" placement="top" :auto-close="1000">
            <el-button icon="ele-Edit" text type="primary" @click="handleEditSchedule(row)"></el-button>
          </el-tooltip>
          <el-tooltip :content="$t('lz.button.base.delete')" placement="top" :auto-close="1000">
            <el-button icon="ele-Delete" text type="danger" @click="handleDeleteSchedule(row)"></el-button>
          </el-tooltip>
        </template>
      </vxe-grid>
    </el-card>

    <!-- Detail Dialog -->
    <el-dialog 
      v-model="state.detailDialogVisible" 
      :title="state.detailDialogTitle"
      width="1000px"
      draggable
      :close-on-click-modal="false"
      class="detail-dialog"
    >
      <template #header>
        <div class="dialog-header-content">
          <div class="header-left">
            <el-icon size="20" class="header-icon"><ele-Calendar /></el-icon>
            <div class="header-info">
              <span class="dialog-title">{{ state.detailDialogTitle }}</span>
              <span class="schedule-code" v-if="state.selectedSchedule?.code">
                {{ state.selectedSchedule.code }}
              </span>
            </div>
          </div>
          <div class="header-right" v-if="state.selectedSchedule">
            <el-tag :type="getStatusTagType(state.selectedSchedule.status)" size="medium" effect="light">
              {{ getStatusLabel(state.selectedSchedule.status) }}
            </el-tag>
          </div>
        </div>
      </template>

      <div v-if="state.selectedSchedule" v-loading="state.detailLoading">
        <!-- Action Buttons and Status Progress (same as dutySchedule) -->
        <div class="detail-actions-top">
          <!-- Status Progress - Left Side -->
          <div class="status-progress-inline">
            <div class="status-badge">
              <el-icon class="status-icon"><ele-CircleCheck /></el-icon>
              <span class="status-label">{{ $t('lz.entity.base.status') }}:</span>
              <TagInfo :config-item="getMaintenanceStatusConfig(state.selectedSchedule.status)" />
            </div>
            <el-divider direction="vertical" style="height: 30px; margin: 0 12px;" />
            <div class="process-steps-container">
              <div class="process-steps-mini">
                <div 
                :class="['process-step-mini', getMaintenanceStepClass('register')]"
                @click="handleMaintenanceStepClick('register')"
              >
                <div class="step-circle">
                  <el-icon v-if="getMaintenanceStepClass('register') === 'step-completed'"><ele-Check /></el-icon>
                  <span v-else>1</span>
                </div>
                <span class="step-label">{{ getStatusLabel('register') }}</span>
              </div>
              <div class="step-connector" :class="getMaintenanceStepClass('confirm')"></div>
              <div 
                :class="['process-step-mini', getMaintenanceStepClass('confirm')]"
                @click="handleMaintenanceStepClick('confirm')"
              >
                <div class="step-circle">
                  <el-icon v-if="getMaintenanceStepClass('confirm') === 'step-completed'"><ele-Check /></el-icon>
                  <span v-else>2</span>
                </div>
                <span class="step-label">{{ getStatusLabel('confirm') }}</span>
              </div>
              <div class="step-connector" :class="getMaintenanceStepClass('process')"></div>
              <div 
                :class="['process-step-mini', getMaintenanceStepClass('process')]"
                @click="handleMaintenanceStepClick('process')"
              >
                <div class="step-circle">
                  <el-icon v-if="getMaintenanceStepClass('process') === 'step-completed'"><ele-Check /></el-icon>
                  <span v-else>3</span>
                </div>
                <span class="step-label">{{ getStatusLabel('process') }}</span>
              </div>
              <div class="step-connector" :class="getMaintenanceStepClass('finish')"></div>
              <div 
                :class="['process-step-mini', getMaintenanceStepClass('finish')]"
                @click="handleMaintenanceStepClick('finish')"
              >
                <div class="step-circle">
                  <el-icon v-if="getMaintenanceStepClass('finish') === 'step-completed'"><ele-Check /></el-icon>
                  <span v-else>4</span>
                </div>
                <span class="step-label">{{ getStatusLabel('finish') }}</span>
              </div>
            </div>
          </div>
          </div>

          <!-- Action Buttons - Right Side (matching dutySchedule logic) -->
          <div class="action-buttons-group">
            <!-- Edit: visible when user can edit/delete (creator or leader, not cancelled) -->
            <el-button 
              v-if="canEditOrDelete"
              type="primary" 
              @click="handleEditSchedule(state.selectedSchedule)"
            >
              <el-icon><ele-Edit /></el-icon>
              {{ $t('lz.button.base.edit') }}
            </el-button>
            
            <!-- Copy: always visible -->
            <el-button type="success" @click="handleCopySchedule">
              <el-icon><ele-CopyDocument /></el-icon>
              {{ $t('lz.button.base.copy') }}
            </el-button>
            
            <!-- Delete: only when status is Register (not confirmed yet) -->
            <el-button 
              v-if="canDelete && isDraftScheduleStatus(state.selectedSchedule?.status)"
              type="danger" 
              @click="handleDeleteSchedule()"
            >
              <el-icon><ele-Delete /></el-icon>
              {{ $t('lz.button.base.delete') }}
            </el-button>

            <!-- Cancel: allowed when not Cancel yet -->
            <el-button 
              v-if="canEditOrDelete && normalizeScheduleStatusKey(state.selectedSchedule?.status) !== 'cancel'"
              type="warning" 
              @click="handleCancelSchedule"
            >
              <el-icon><ele-CircleClose /></el-icon>
              {{ $t('lz.button.base.cancel') }}
            </el-button>

            <!-- Permanent Delete: only when status is Cancelled and user is admin (role 111 or 222) -->
            <el-button 
              v-if="canDelete && normalizeScheduleStatusKey(state.selectedSchedule?.status) === 'cancel'"
              type="danger" 
              @click="handleDeleteSchedule()"
            >
              <el-icon><ele-Delete /></el-icon>
              {{ $t('lz.button.base.delete') }}
            </el-button>
          </div>
        </div>

        <!-- Summary Cards -->
        <div class="summary-cards">
          <div class="summary-card priority-card" :class="'priority-' + (normalizePriorityKey(state.selectedSchedule.priority) || 'medium').toLowerCase()">
            <div class="card-icon">
              <el-icon size="24"><ele-Flag /></el-icon>
            </div>
            <div class="card-content">
              <span class="card-label">{{ $t('lz.entity.base.priority') }}</span>
              <span class="card-value">{{ getPriorityLabel(state.selectedSchedule.priority) }}</span>
            </div>
          </div>
          
          <div class="summary-card">
            <div class="card-icon equipment-icon">
              <el-icon size="24"><ele-Monitor /></el-icon>
            </div>
            <div class="card-content">
              <span class="card-label">{{ $t('lz.label.tmsMaintenanceSchedule.equipmentList') }}</span>
              <span class="card-value">
                {{ state.selectedSchedule.completedEquipmentCount || 0 }}/{{ state.selectedSchedule.equipmentCount || 0 }}
              </span>
            </div>
          </div>
          
          <div class="summary-card">
            <div class="card-icon personnel-icon">
              <el-icon size="24"><ele-User /></el-icon>
            </div>
            <div class="card-content">
              <span class="card-label">{{ $t('lz.label.tmsMaintenanceSchedule.personnelList') }}</span>
              <span class="card-value">
                {{ state.selectedSchedule.completedTaskCount || 0 }}/{{ state.selectedSchedule.taskCount || 0 }}
              </span>
            </div>
          </div>
          
          <div class="summary-card">
            <div class="card-icon date-icon">
              <el-icon size="24"><ele-Timer /></el-icon>
            </div>
            <div class="card-content">
              <span class="card-label">{{ $t('lz.label.tmsMaintenanceSchedule.scheduledDate') }}</span>
              <span class="card-value">{{ formatDate(state.selectedSchedule.scheduledDate) }}</span>
            </div>
          </div>
        </div>

        <el-tabs v-model="state.detailActiveTab" type="border-card" class="detail-tabs">
          <!-- Tab 1: Basic Info -->
          <el-tab-pane name="info">
            <template #label>
              <span class="tab-label">
                <el-icon><ele-Document /></el-icon>
                {{ $t('lz.button.base.detail') }}
              </span>
            </template>
            <el-descriptions :column="2" border>
              <el-descriptions-item :label="$t('lz.entity.base.code')">
                {{ state.selectedSchedule.code }}
              </el-descriptions-item>
              <el-descriptions-item :label="$t('lz.label.tmsMaintenanceSchedule.name')">
                {{ state.selectedSchedule.name }}
              </el-descriptions-item>
              <el-descriptions-item :label="$t('lz.label.tmsMaintenanceSchedule.scheduledDate')">
                {{ formatDate(state.selectedSchedule.scheduledDate) }}
              </el-descriptions-item>
              <el-descriptions-item :label="$t('lz.label.tmsMaintenanceSchedule.dueDate')">
                {{ formatDate(state.selectedSchedule.dueDate) }}
              </el-descriptions-item>
              <el-descriptions-item :label="$t('lz.label.tmsMaintenanceSchedule.reminderDays')">
                {{ state.selectedSchedule.reminderDaysBefore || '-' }} {{ $t('lz.label.tmsMaintenanceSchedule.days') }}
              </el-descriptions-item>
              <el-descriptions-item :label="$t('lz.label.tmsMaintenanceSchedule.completedDate')">
                {{ formatDate(state.selectedSchedule.completedDate) || '-' }}
              </el-descriptions-item>
              <el-descriptions-item :label="$t('lz.entity.base.createBy')" :span="2">
                {{ state.selectedSchedule.createUserName || '-' }}
              </el-descriptions-item>
              <el-descriptions-item :label="$t('lz.entity.base.remark')" :span="2">
                {{ state.selectedSchedule.remark || '-' }}
              </el-descriptions-item>
            </el-descriptions>
          </el-tab-pane>

          <!-- Tab 2: Equipment List -->
          <el-tab-pane name="equipments">
            <template #label>
              <span class="tab-label">
                <el-icon><ele-Monitor /></el-icon>
                {{ $t('lz.label.tmsMaintenanceSchedule.equipmentList') }}
                <el-badge :value="state.selectedSchedule.equipmentCount" type="primary" :offset="[8, 0]" />
              </span>
            </template>
            <el-table :data="state.selectedSchedule.equipments || []" border stripe max-height="400">
              <el-table-column type="index" width="60" align="center" />
              <el-table-column prop="equipmentCode" :label="$t('lz.label.tmsMaintenanceSchedule.equipmentCode')" width="120" />
              <el-table-column prop="equipmentName" :label="$t('lz.label.tmsMaintenanceSchedule.equipmentName')" min-width="180" />
              <el-table-column :label="$t('lz.entity.tms.equipmentType')" width="150">
                <template #default="{ row }">
                  {{ row.equipmentTypeName || (row.equipmentTypeId ? getEquipmentTypeNameById(row.equipmentTypeId) : '-') }}
                </template>
              </el-table-column>
              <!-- <el-table-column label="Ghi chú kiểm tra" min-width="200">
                <template #default="{ row }">
                  <span>{{ row.checkedNote || '-' }}</span>
                </template>
              </el-table-column> -->
              <el-table-column :label="$t('lz.entity.tms.equipmentState')" width="130" align="center">
                <template #default="{ row }">
                  <TagInfo v-if="row.equipmentState !== undefined && row.equipmentState !== null" 
                    :config-item="getConfigItemByCode(BaseConfigTypeEnum.TmsEquipmentState, String(row.equipmentState))" />
                  <span v-else style="color: var(--el-text-color-placeholder);">-</span>
                </template>
              </el-table-column>
              <el-table-column prop="status" :label="$t('lz.entity.base.status')" width="120" align="center">
                <template #default="{ row }">
                  <el-tag :type="getEquipmentStatusTagType(row.status)" size="small">
                    {{ getEquipmentStatusLabel(row.status) }}
                  </el-tag>
                </template>
              </el-table-column>
              <el-table-column :label="$t('lz.label.base.action')" width="140" align="center" fixed="right">
                <template #default="{ row }">
                  <el-tooltip content="Kiểm tra" placement="top">
                    <el-button type="primary" link size="small" :disabled="!canUpdateExecutionStatus" @click="openEquipmentCheckDialog(row)">
                      <el-icon><ele-Edit /></el-icon>
                    </el-button>
                  </el-tooltip>
                  <el-tooltip v-if="row.maintenanceGuideId" :content="$t('lz.label.tmsMaintenanceSchedule.viewGuide')" placement="top">
                    <el-button type="primary" link size="small" @click="handlePreviewGuideFromEquipment(row.maintenanceGuideId)">
                      <el-icon><ele-View /></el-icon>
                    </el-button>
                  </el-tooltip>
                  <span v-else style="color: var(--el-text-color-placeholder);">-</span>
                </template>
              </el-table-column>
            </el-table>
          </el-tab-pane>

          <!-- Tab 3: Personnel (Tasks) -->
          <el-tab-pane name="personnel">
            <template #label>
              <span class="tab-label">
                <el-icon><ele-User /></el-icon>
                {{ $t('lz.label.tmsMaintenanceSchedule.personnelList') }}
                <el-badge :value="state.selectedSchedule.taskCount" type="primary" :offset="[8, 0]" />
              </span>
            </template>
            
            <!-- Toolbar for personnel management -->
            <div v-if="canManagePersonnel && isLimitedEditMode" class="personnel-toolbar" style="margin-bottom: 12px; display: flex; justify-content: space-between; align-items: center;">
              <el-alert type="warning" :closable="false" show-icon style="flex: 1; margin-right: 12px;">
                <template #title>
                  <span style="font-size: 12px;">
                    {{ $t('lz.message.tmsMaintenanceSchedule.limitedEditModeInfo') }}
                  </span>
                </template>
              </el-alert>
              <el-button type="primary" size="small" @click="handleOpenAddPersonnelDialog">
                <el-icon><ele-Plus /></el-icon>
                {{ $t('lz.label.tmsMaintenanceSchedule.addPersonnel') }}
              </el-button>
            </div>
            <div v-else-if="canManagePersonnel" class="personnel-toolbar" style="margin-bottom: 12px; display: flex; justify-content: flex-end;">
              <el-button type="primary" size="small" @click="handleOpenAddPersonnelDialog">
                <el-icon><ele-Plus /></el-icon>
                {{ $t('lz.label.tmsMaintenanceSchedule.addPersonnel') }}
              </el-button>
            </div>

            <el-table 
              :data="state.selectedSchedule.tasks || []" 
              border 
              stripe 
              max-height="400"
              v-if="state.selectedSchedule.tasks && state.selectedSchedule.tasks.length > 0"
            >
              <el-table-column type="index" width="60" align="center" :label="$t('lz.label.base.idx')" />
              <el-table-column prop="assignedUserName" :label="$t('lz.label.tmsMaintenanceSchedule.assignedUser')" min-width="150">
                <template #default="{ row }">
                  <span>{{ row.assignedUserName || row.assignedUserId || '-' }}</span>
                </template>
              </el-table-column>
              <el-table-column prop="role" :label="$t('lz.label.tmsMaintenanceSchedule.role')" width="120" align="center">
                <template #default="{ row }">
                  <TagInfo v-if="row.role" :config-item="getConfigItemByCode(BaseConfigTypeEnum.TmsDutyPatronRole, (row.role || '').toLowerCase())" />
                  <span v-else>-</span>
                </template>
              </el-table-column>
              <el-table-column prop="taskName" :label="$t('lz.label.tmsMaintenanceSchedule.taskName')" min-width="180">
                <template #default="{ row }">
                  {{ row.taskName || '-' }}
                </template>
              </el-table-column>
              <el-table-column prop="status" :label="$t('lz.entity.base.status')" width="120" align="center">
                <template #default="{ row }">
                  <el-tag :type="getTaskStatusTagType(row.status)" size="small">
                    {{ getTaskStatusLabel(row.status) }}
                  </el-tag>
                </template>
              </el-table-column>
              <el-table-column :label="$t('lz.label.base.action')" width="170" align="center" fixed="right" v-if="canManagePersonnel">
                <template #default="{ row }">
                  <el-tooltip content="Cập nhật trạng thái" placement="top">
                    <el-button type="success" size="small" link :disabled="!canUpdateExecutionStatus" @click="openTaskStatusDialog(row)">
                      <el-icon><ele-Edit /></el-icon>
                    </el-button>
                  </el-tooltip>
                  <el-tooltip :content="$t('lz.label.tmsMaintenanceSchedule.changeRole')" placement="top">
                    <el-button type="primary" size="small" link @click="handleOpenChangeRoleDialog(row)">
                      <el-icon><ele-Switch /></el-icon>
                    </el-button>
                  </el-tooltip>
                  <el-tooltip :content="$t('lz.button.base.delete')" placement="top" :auto-close="1000">
                    <el-button type="danger" size="small" link @click="handleRemovePersonnel(row)">
                      <el-icon><ele-Delete /></el-icon>
                    </el-button>
                  </el-tooltip>
                </template>
              </el-table-column>
            </el-table>
          <el-empty v-else :description="$t('lz.label.tmsMaintenanceSchedule.noPersonnel')" />
          </el-tab-pane>

          <!-- Tab 4: History -->
          <el-tab-pane name="history">
            <template #label>
              <span class="tab-label">
                <el-icon><ele-Clock /></el-icon>
                {{ $t('lz.label.base.history') }}
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
                            <span class="timeline-label">{{ $t('lz.label.tmsMaintenanceSchedule.performer') }}:</span>
                            <span class="timeline-value">
                              <el-icon size="12"><ele-User /></el-icon>
                              {{ getUserRealName(item.actionBy) }}
                            </span>
                          </div>
                          
                          <div class="timeline-row" v-if="item.affectedUserId">
                            <span class="timeline-label">{{ $t('lz.label.tmsMaintenanceSchedule.targetLabel') }}:</span>
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
                            <span class="timeline-label">{{ $t('lz.label.tmsMaintenanceSchedule.reason') }}:</span>
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
                :description="$t('lz.label.tmsMaintenanceSchedule.noHistory')"
                :image-size="80"
              />
            </div>
          </el-tab-pane>
        </el-tabs>
      </div>

      <template #footer>
        <el-button @click="state.detailDialogVisible = false">{{ $t('lz.button.base.close') }}</el-button>
      </template>
    </el-dialog>

    <!-- Equipment Check Modal (Tab 2) -->
    <el-dialog
      v-model="state.equipmentCheckDialogVisible"
      :title="$t('lz.label.tmsMaintenanceSchedule.equipmentInspection')"
      width="520px"
      draggable
      :close-on-click-modal="false"
    >
      <el-form label-width="140px" :disabled="state.equipmentCheckSaving || !canUpdateExecutionStatus">
        <el-form-item :label="$t('lz.entity.tms.equipment')">
          <span>{{ state.equipmentCheckForm.equipmentName || '-' }}</span>
        </el-form-item>
        <el-form-item :label="$t('lz.entity.base.status')">
          <el-radio-group v-model="state.equipmentCheckForm.status">
            <el-radio
              v-for="item in equipmentStatusOptions"
              :key="item.value"
              :label="item.value"
            >
              {{ item.label }}
            </el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item :label="$t('lz.entity.base.remark')">
          <el-input
            v-model="state.equipmentCheckForm.checkedNote"
            type="textarea"
            :rows="4"
            clearable
            :placeholder="$t('lz.entity.tmsMaintenanceSchedule.inspectionRemarks')"
          />
        </el-form-item>
      </el-form>

      <template #footer>
        <el-button @click="state.equipmentCheckDialogVisible = false">{{ $t('lz.button.base.cancel') }}</el-button>
        <el-button type="primary" :loading="state.equipmentCheckSaving" :disabled="!canUpdateExecutionStatus" @click="submitEquipmentCheck">
          {{ $t('lz.button.base.confirm') }}
        </el-button>
      </template>
    </el-dialog>

    <!-- Floating Action Button -->
    <el-button
      type="primary"
      circle
      size="large"
      class="fab-button"
      @click="handleAddSchedule"
    >
      <el-icon size="24"><ele-Plus /></el-icon>
    </el-button>

    <!-- Add/Edit Schedule Dialog -->
    <el-dialog 
      v-model="state.editDialogVisible" 
      :title="state.editDialogTitle"
      width="800px"
      draggable
      :close-on-click-modal="false"
    >
      <template #header>
        <div style="color: #fff">
          <el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle">
            <ele-Edit />
          </el-icon>
          <span>{{ state.editDialogTitle }}</span>
        </div>
      </template>
      <el-form 
        ref="formRef"
        :model="state.formData" 
        :rules="formRules" 
        label-width="auto"
        label-position="left"
        :disabled="state.formLoading"
      >
        <Fieldset :legend="$t('lz.label.tmsMaintenanceSchedule.scheduleInfo')" :toggleable="false">
          <el-row :gutter="20">
            <el-col :span="24" class="mb10">
              <el-form-item :label="$t('lz.label.tmsMaintenanceSchedule.name')" prop="name">
                <el-input v-model="state.formData.name" :placeholder="$t('lz.label.tmsMaintenanceSchedule.namePlaceholder')" />
              </el-form-item>
            </el-col>
          </el-row>
          
          <el-row :gutter="10">
            <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb10">
              <el-form-item :label="$t('lz.label.tmsMaintenanceSchedule.scheduledDate')" prop="scheduledDate">
                <el-date-picker
                  v-model="state.formData.scheduledDate"
                  type="date"
                  :placeholder="$t('lz.label.tmsMaintenanceSchedule.selectScheduledDate')"
                  style="width: 100%"
                  value-format="YYYY-MM-DD"
                />
              </el-form-item>
            </el-col>
            <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb10">
              <el-form-item :label="$t('lz.label.tmsMaintenanceSchedule.dueDate')" prop="dueDate">
                <el-date-picker
                  v-model="state.formData.dueDate"
                  type="date"
                  :placeholder="$t('lz.label.tmsMaintenanceSchedule.selectDueDate')"
                  style="width: 100%"
                  value-format="YYYY-MM-DD"
                />
              </el-form-item>
            </el-col>
          </el-row>

          <el-row :gutter="10">
            <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb10">
              <el-form-item :label="$t('lz.entity.base.priority')" prop="priority">
                <el-select v-model="state.formData.priority" :placeholder="$t('lz.label.tmsMaintenanceSchedule.selectPriority')" style="width: 100%">
                  <el-option v-for="item in priorityOptions" :key="item.value" :label="item.label" :value="item.value" />
                </el-select>
              </el-form-item>
            </el-col>
            <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb10">
              <el-form-item :label="$t('lz.label.tmsMaintenanceSchedule.reminderDays')" prop="reminderDaysBefore">
                <el-input-number v-model="state.formData.reminderDaysBefore" :min="0" :max="30" style="width: 100%" />
              </el-form-item>
            </el-col>
          </el-row>

          <el-row :gutter="10" v-if="state.operateType === 'edit'">
            <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb10">
              <el-form-item :label="$t('lz.entity.base.status')" prop="status">
                <el-select v-model="state.formData.status" :placeholder="$t('lz.label.tmsMaintenanceSchedule.selectStatus')" style="width: 100%">
                  <el-option v-for="item in statusOptions" :key="item.value" :label="item.label" :value="item.value" />
                </el-select>
              </el-form-item>
            </el-col>
          </el-row>

          <el-row :gutter="10">
            <el-col :span="24" class="mb10">
              <el-form-item :label="$t('lz.entity.base.remark')" prop="remark">
                <el-input v-model="state.formData.remark" type="textarea" :rows="2" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.remark') })" />
              </el-form-item>
            </el-col>
          </el-row>
        </Fieldset>

        <!-- Personnel Assignment Section -->
        <Fieldset :legend="$t('lz.label.tmsMaintenanceSchedule.personnelAssignment')" :toggleable="false" style="margin-top: 15px;">
          <!-- Quick actions -->
          <div class="quick-actions-bar mb15">
            <el-button type="primary" size="small" @click="handleOpenTeamQuickSelect">
              <el-icon><ele-UserFilled /></el-icon>
              {{ $t('lz.label.tmsMaintenanceSchedule.quickAddByTeam') }}
            </el-button>
            <el-button type="info" size="small" plain @click="handleOpenTeamManage">
              <el-icon><ele-Setting /></el-icon>
              {{ $t('lz.label.tmsMaintenanceSchedule.manageTeam') }}
            </el-button>
          </div>

          <el-row :gutter="10">
            <!-- Team Leader -->
            <el-col :span="24" class="mb10">
              <el-form-item :label="$t('lz.label.tmsMaintenanceSchedule.teamLeader')" prop="leaders">
                <el-select
                  v-model="state.selectedLeaderIds"
                  multiple
                  filterable
                  :placeholder="$t('lz.label.tmsMaintenanceSchedule.selectLeader')"
                  style="width: 100%"
                  @change="handleLeaderChange"
                >
                  <el-option
                    v-for="user in state.employeeList"
                    :key="user.id"
                    :label="`${user.name || user.username}`"
                    :value="user.id"
                  >
                    <div style="display: flex; align-items: center; gap: 8px;">
                      <el-icon><ele-User /></el-icon>
                      <span>{{ user.name || user.username }}</span>
                      <span style="color: #999; font-size: 12px;">({{ user.username }})</span>
                    </div>
                  </el-option>
                </el-select>
              </el-form-item>
            </el-col>

            <!-- Members -->
            <el-col :span="24" class="mb10">
              <el-form-item :label="$t('lz.label.tmsMaintenanceSchedule.members')" prop="members">
                <el-select
                  v-model="state.selectedMemberIds"
                  multiple
                  filterable
                  :placeholder="$t('lz.label.tmsMaintenanceSchedule.selectMembers')"
                  style="width: 100%"
                  @change="handleMemberChange"
                >
                  <el-option
                    v-for="user in state.employeeList"
                    :key="user.id"
                    :label="`${user.name || user.username}`"
                    :value="user.id"
                  >
                    <div style="display: flex; align-items: center; gap: 8px;">
                      <el-icon><ele-User /></el-icon>
                      <span>{{ user.name || user.username }}</span>
                      <span style="color: #999; font-size: 12px;">({{ user.username }})</span>
                    </div>
                  </el-option>
                </el-select>
              </el-form-item>
            </el-col>
          </el-row>
        </Fieldset>

        <!-- Equipment Selection Section -->
        <Fieldset :legend="$t('lz.label.tmsMaintenanceSchedule.equipmentSelection')" :toggleable="false" style="margin-top: 15px;">

        <div v-loading="state.equipmentLoading" style="max-height: 300px; overflow-y: auto;">
          <el-table
            :data="state.selectedEquipments"
            border
            stripe
            max-height="280"
            style="width: 100%; margin-bottom: 15px;"
            v-if="state.selectedEquipments.length > 0"
          >
            <el-table-column type="index" width="50" align="center" />
            <el-table-column :label="$t('lz.label.tmsMaintenanceSchedule.equipmentName')" min-width="150">
              <template #default="{ row }">
                {{ row.equipmentName || row.equipmentId }}
              </template>
            </el-table-column>
            <el-table-column :label="$t('lz.entity.tms.equipmentType')" width="120">
              <template #default="{ row }">
                {{ row.equipmentTypeName || (row.equipmentTypeId ? getEquipmentTypeNameById(row.equipmentTypeId) : '-') }}
              </template>
            </el-table-column>
            <el-table-column :label="$t('lz.entity.tms.equipmentState')" width="130" align="center">
              <template #default="{ row }">
                <TagInfo v-if="row.equipmentState !== undefined && row.equipmentState !== null" 
                  :config-item="getConfigItemByCode(BaseConfigTypeEnum.TmsEquipmentState, String(row.equipmentState))" />
                <span v-else>-</span>
              </template>
            </el-table-column>
            <el-table-column :label="$t('lz.label.tmsMaintenanceSchedule.maintenanceGuide')" min-width="200">
              <template #default="{ row }">
                <div style="display: flex; align-items: center;">
                  <el-select
                    v-model="row.maintenanceGuideId"
                    :placeholder="$t('lz.label.tmsMaintenanceSchedule.selectGuide')"
                    clearable
                    size="small"
                    style="flex: 1;"
                    :loading="state.guideLoading"
                  >
                    <el-option
                      v-for="guide in getGuidesForEquipmentByTypeId(row.equipmentTypeId)"
                      :key="guide.id"
                        :label="guide.name || guide.code || ''"
                        :value="String(guide.id)"
                    />
                  </el-select>
                  <el-tooltip v-if="row.maintenanceGuideId" :content="$t('lz.label.tmsMaintenanceSchedule.previewGuide')" placement="top">
                    <el-button type="primary" size="small" link @click="handlePreviewGuide(row.maintenanceGuideId)">
                      <el-icon><ele-View /></el-icon>
                    </el-button>
                  </el-tooltip>
                  <el-tooltip v-if="getGuidesForEquipmentByTypeId(row.equipmentTypeId).length === 0" :content="$t('lz.label.tmsMaintenanceSchedule.createGuide')" placement="top">
                    <el-button type="success" size="small" link @click="handleCreateGuideForEquipment(row)">
                      <el-icon><ele-Plus /></el-icon>
                    </el-button>
                  </el-tooltip>
                </div>
              </template>
            </el-table-column>
            <el-table-column width="50" align="center">
              <template #default="{ $index }">
                <el-button type="danger" size="small" link @click="handleRemoveEquipment($index)">
                  <el-icon><ele-Delete /></el-icon>
                </el-button>
              </template>
            </el-table-column>
          </el-table>

          <el-button type="primary" plain size="small" @click="handleOpenEquipmentSelector">
            <el-icon><ele-Plus /></el-icon>
            {{ $t('lz.label.tmsMaintenanceSchedule.addEquipment') }}
          </el-button>
        </div>
        </Fieldset>
      </el-form>

      <template #footer>
        <el-button @click="state.editDialogVisible = false" :disabled="state.formLoading">
          {{ $t('lz.button.base.cancel') }}
        </el-button>
        <el-button type="primary" @click="handleSaveSchedule" :loading="state.formLoading">
          {{ $t('lz.button.base.save') }}
        </el-button>
      </template>
    </el-dialog>

    <!-- Equipment Selector Dialog -->
    <el-dialog
      v-model="state.equipmentSelectorVisible"
      :title="$t('lz.label.tmsMaintenanceSchedule.selectEquipment')"
      width="850px"
      :close-on-click-modal="false"
    >
      <template #header>
        <div style="color: #fff; display: flex; align-items: center;">
          <el-icon size="16" style="margin-right: 5px;"><ele-Monitor /></el-icon>
          <span>{{ $t('lz.label.tmsMaintenanceSchedule.selectEquipment') }}</span>
        </div>
      </template>
      
      <div style="margin-bottom: 15px; display: flex; flex-wrap: wrap; gap: 10px; align-items: center;">
        <el-select
          v-model="state.equipmentSelectorTypeId"
          :placeholder="$t('lz.label.tmsMaintenanceSchedule.selectEquipmentType')"
          clearable
          filterable
          style="width: 200px;"
          @change="handleEquipmentSelectorQuery"
          @clear="handleEquipmentSelectorQuery"
        >
          <el-option
            v-for="item in state.equipmentTypeList"
            :key="item.id"
            :label="item.name"
            :value="item.id"
          />
        </el-select>
        <el-select
          v-model="state.equipmentSelectorStateFilter"
          :placeholder="$t('lz.label.tmsMaintenanceSchedule.filterByState')"
          clearable
          filterable
          style="width: 180px;"
          @change="handleEquipmentSelectorQuery"
          @clear="handleEquipmentSelectorQuery"
        >
          <el-option
            v-for="item in getConfigDataByCode(BaseConfigTypeEnum.TmsEquipmentState)"
            :key="item.code"
            :label="item.name"
            :value="item.code"
          />
        </el-select>
        <el-input
          v-model="state.equipmentSearchText"
          :placeholder="$t('lz.label.tmsMaintenanceSchedule.searchEquipment')"
          clearable
          style="width: 200px;"
          @keyup.enter="handleEquipmentSelectorQuery"
          @clear="handleEquipmentSelectorQuery"
        >
          <template #prefix>
            <el-icon><ele-Search /></el-icon>
          </template>
        </el-input>
        <el-button type="primary" @click="handleEquipmentSelectorQuery">
          <el-icon><ele-Search /></el-icon>
          {{ $t('lz.button.base.search') }}
        </el-button>
        <el-button @click="handleEquipmentSelectorReset">
          <el-icon><ele-Refresh /></el-icon>
          {{ $t('lz.button.base.reset') }}
        </el-button>
      </div>
      
      <vxe-grid
        ref="equipmentSelectorGrid"
        class="xGrid-style"
        v-bind="equipmentSelectorGridOptions"
        v-on="equipmentSelectorGridEvents"
        @checkbox-change="onEquipmentGridCheck"
        @checkbox-all="onEquipmentGridCheckAll"
      >
        <template #empty>
          <el-empty :image-size="100" :description="$t('lz.label.base.noData')" />
        </template>
        <template #row_equip_type="{ row }">
          {{ getEquipmentTypeNameById(row.equipmentTypeId) }}
        </template>
        <template #row_equip_state="{ row }">
          <TagInfo v-if="row.equipmentState !== undefined && row.equipmentState !== null" :config-item="getConfigItemByCode(BaseConfigTypeEnum.TmsEquipmentState, String(row.equipmentState))" />
          <span v-else>-</span>
        </template>
        <template #row_alert="{ row }">
          <el-tooltip v-if="getEquipmentAlerts(row.id).length > 0" :content="`${$t('lz.label.tmsMaintenanceSchedule.viewAlert')} (${getEquipmentAlerts(row.id).length})`" placement="top">
            <el-badge :value="getEquipmentAlerts(row.id).length" :max="9" type="danger" :offset="[-5, 5]">
              <el-button type="warning" size="small" link @click.stop="handlePreviewEquipmentAlert(row)">
                <el-icon><ele-Warning /></el-icon>
              </el-button>
            </el-badge>
          </el-tooltip>
        </template>
      </vxe-grid>
      
      <template #footer>
        <el-button @click="state.equipmentSelectorVisible = false">{{ $t('lz.button.base.cancel') }}</el-button>
        <el-button type="primary" @click="handleConfirmEquipmentSelection">
          {{ $t('lz.button.base.confirm') }} ({{ state.tempSelectedEquipments.length }})
        </el-button>
      </template>
    </el-dialog>

    <!-- Add Personnel Dialog -->
    <el-dialog
      v-model="state.addPersonnelDialogVisible"
      :title="$t('lz.label.tmsMaintenanceSchedule.addPersonnel')"
      width="500px"
      :close-on-click-modal="false"
      @close="handleCloseAddPersonnelDialog"
    >
      <template #header>
        <div style="color: #fff; display: flex; align-items: center;">
          <el-icon size="16" style="margin-right: 5px;"><ele-Plus /></el-icon>
          <span>{{ $t('lz.label.tmsMaintenanceSchedule.addPersonnel') }}</span>
        </div>
      </template>
      <el-form label-width="120px" :disabled="state.personnelActionLoading">
        <el-form-item :label="$t('lz.label.tmsMaintenanceSchedule.employee')" required>
          <el-select
            v-model="state.newPersonnelForm.userId"
            :placeholder="$t('lz.label.tmsMaintenanceSchedule.selectEmployee')"
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
        
        <el-form-item :label="$t('lz.label.tmsMaintenanceSchedule.role')">
          <el-radio-group v-model="state.newPersonnelForm.role">
            <el-radio 
              v-for="item in getConfigDataByCode(BaseConfigTypeEnum.TmsDutyPatronRole)" 
              :key="item.code" 
              :value="item.code"
            >
              {{ item.name }}
            </el-radio>
          </el-radio-group>
        </el-form-item>

        <el-form-item :label="$t('lz.label.tmsMaintenanceSchedule.taskName')">
          <el-input
            v-model="state.newPersonnelForm.taskName"
            :placeholder="$t('lz.label.tmsMaintenanceSchedule.taskNamePlaceholder')"
          />
        </el-form-item>
        
        <el-form-item :label="$t('lz.entity.base.desc')">
          <el-input
            v-model="state.newPersonnelForm.description"
            type="textarea"
            :rows="2"
            :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.desc') })"
          />
        </el-form-item>
      </el-form>
      
      <template #footer>
        <el-button @click="handleCloseAddPersonnelDialog" :disabled="state.personnelActionLoading">
          {{ $t('lz.button.base.cancel') }}
        </el-button>
        <el-button
          type="primary"
          @click="handleAddPersonnelToSchedule"
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
        <div style="color: #fff; display: flex; align-items: center;">
          <el-icon size="16" style="margin-right: 5px;"><ele-Edit /></el-icon>
          <span>{{ $t('lz.label.tmsMaintenanceSchedule.changeRole') }}</span>
        </div>
      </template>
      <el-form label-width="120px" :disabled="state.personnelActionLoading">
        <el-form-item :label="$t('lz.label.tmsMaintenanceSchedule.employee')">
          <span style="font-weight: 500;">{{ getUserRealName(state.changeRoleForm.userId) }}</span>
        </el-form-item>
        
        <el-form-item :label="$t('lz.label.tmsMaintenanceSchedule.currentRole')">
          <TagInfo v-if="state.changeRoleForm.currentRole" :config-item="getConfigItemByCode(BaseConfigTypeEnum.TmsDutyPatronRole, state.changeRoleForm.currentRole)" />
          <span v-else>-</span>
        </el-form-item>
        
        <el-form-item :label="$t('lz.label.tmsMaintenanceSchedule.newRole')" required>
          <el-radio-group v-model="state.changeRoleForm.newRole">
            <el-radio 
              v-for="role in getConfigDataByCode(BaseConfigTypeEnum.TmsDutyPatronRole)" 
              :key="role.code" 
              :value="role.code" 
              :disabled="state.changeRoleForm.currentRole.toLowerCase() === role.code.toLowerCase()"
            >
              {{ role.name }}
            </el-radio>
          </el-radio-group>
        </el-form-item>
        
        <el-form-item :label="$t('lz.label.tmsMaintenanceSchedule.reason')">
          <el-input
            v-model="state.changeRoleForm.reason"
            type="textarea"
            :rows="2"
            :placeholder="$t('lz.label.tmsMaintenanceSchedule.reasonPlaceholder')"
          />
        </el-form-item>
      </el-form>
      
      <template #footer>
        <el-button @click="handleCloseChangeRoleDialog" :disabled="state.personnelActionLoading">
          {{ $t('lz.button.base.cancel') }}
        </el-button>
        <el-button
          type="primary"
          @click="handleChangePersonnelRole"
          :loading="state.personnelActionLoading"
        >
          {{ $t('lz.button.base.save') }}
        </el-button>
      </template>
    </el-dialog>

    <!-- Task Status Dialog (Personnel) -->
    <el-dialog
      v-model="state.taskStatusDialogVisible"
      :title="$t('lz.label.base.updateStatus')"
      width="450px"
      draggable
      :close-on-click-modal="false"
    >
      <el-form label-width="130px" :disabled="state.taskStatusSaving || !canUpdateExecutionStatus">
        <el-form-item :label="$t('lz.entity.tms.employee')">
          <span style="font-weight: 500;">{{ state.taskStatusForm.assignedUserName || state.taskStatusForm.assignedUserId || '-' }}</span>
        </el-form-item>
        <el-form-item :label="$t('lz.entity.base.status')" required>
          <el-radio-group v-model="state.taskStatusForm.status">
            <el-radio
              v-for="item in taskStatusOptions"
              :key="item.value"
              :label="item.value"
            >
              {{ item.label }}
            </el-radio>
          </el-radio-group>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="state.taskStatusDialogVisible = false" :disabled="state.taskStatusSaving">
          {{ $t('lz.button.base.cancel') }}
        </el-button>
        <el-button type="primary" @click="submitTaskStatus" :loading="state.taskStatusSaving" :disabled="!canUpdateExecutionStatus">
          {{ $t('lz.button.base.save') }}
        </el-button>
      </template>
    </el-dialog>

    <!-- Team Quick Select Component -->
    <TeamQuickSelect 
      ref="teamQuickSelectRef" 
      @select="handleTeamSelect"
    />
    
    <!-- Patrol Team Management Component -->
    <PatrolTeamManage ref="patrolTeamManageRef" @refresh="handleTeamRefresh" />

    <!-- Equipment Alert Preview Dialog -->
    <el-dialog
      v-model="state.equipmentAlertPreviewVisible"
      :title="$t('lz.label.tmsMaintenanceSchedule.equipmentAlerts')"
      width="700px"
      :close-on-click-modal="true"
      class="alert-preview-dialog"
    >
      <template #header>
        <div style="color: #fff; display: flex; align-items: center;">
          <el-icon size="18" style="margin-right: 8px;"><ele-Warning /></el-icon>
          <span>{{ $t('lz.label.tmsMaintenanceSchedule.equipmentAlerts') }}</span>
          <span v-if="state.selectedEquipmentForAlert" style="margin-left: 8px; opacity: 0.8;">
            - {{ state.selectedEquipmentForAlert.name }}
          </span>
        </div>
      </template>
      
      <div v-loading="state.equipmentAlertPreviewLoading">
        <!-- Equipment Info Summary -->
        <div v-if="state.selectedEquipmentForAlert" class="equipment-info-summary" style="margin-bottom: 15px; padding: 12px; background: var(--el-fill-color-light); border-radius: 8px;">
          <div style="display: flex; gap: 20px; flex-wrap: wrap;">
            <div>
              <span style="color: var(--el-text-color-secondary); font-size: 12px;">{{ $t('lz.label.tmsMaintenanceSchedule.equipmentCode') }}</span>
              <div style="font-weight: 500;">{{ state.selectedEquipmentForAlert.code || '-' }}</div>
            </div>
            <div>
              <span style="color: var(--el-text-color-secondary); font-size: 12px;">{{ $t('lz.entity.tms.equipmentType') }}</span>
              <div style="font-weight: 500;">{{ getEquipmentTypeNameById(state.selectedEquipmentForAlert.equipmentTypeId) }}</div>
            </div>
            <div>
              <span style="color: var(--el-text-color-secondary); font-size: 12px;">{{ $t('lz.entity.tms.equipmentState') }}</span>
              <div>
                <TagInfo v-if="state.selectedEquipmentForAlert.equipmentState !== undefined && state.selectedEquipmentForAlert.equipmentState !== null" 
                  :config-item="getConfigItemByCode(BaseConfigTypeEnum.TmsEquipmentState, String(state.selectedEquipmentForAlert.equipmentState))" />
                <span v-else>-</span>
              </div>
            </div>
          </div>
        </div>

        <!-- Alerts List -->
        <div v-if="state.selectedEquipmentAlerts.length > 0">
          <el-table :data="state.selectedEquipmentAlerts" border stripe max-height="400" style="width: 100%;">
            <el-table-column type="index" width="50" align="center" />
            <el-table-column :label="$t('lz.label.tmsMaintenanceSchedule.alertType')" width="140" align="center">
              <template #default="{ row }">
                <el-tag :type="getAlertTypeTagType(row.alertType)" size="small">
                  {{ getAlertTypeLabel(row.alertType) }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column :label="$t('lz.label.tmsMaintenanceSchedule.alertMessage')" min-width="200">
              <template #default="{ row }">
                {{ row.alertMessage || '-' }}
              </template>
            </el-table-column>
            <el-table-column :label="$t('lz.entity.base.priority')" width="100" align="center">
              <template #default="{ row }">
                <el-tag :type="getPriorityTagType(row.priority)" size="small">
                  {{ getPriorityLabel(row.priority) }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column :label="$t('lz.entity.base.status')" width="120" align="center">
              <template #default="{ row }">
                <el-tag :type="getAlertStatusTagType(row.status)" size="small">
                  {{ getAlertStatusLabel(row.status) }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column :label="$t('lz.entity.base.createTime')" width="150" align="center">
              <template #default="{ row }">
                {{ formatAlertDateTime(row.createdTime) }}
              </template>
            </el-table-column>
          </el-table>
        </div>
        <el-empty v-else :description="$t('lz.label.tmsMaintenanceSchedule.noAlerts')" :image-size="80" />
      </div>
      
      <template #footer>
        <el-button @click="state.equipmentAlertPreviewVisible = false">{{ $t('lz.button.base.close') }}</el-button>
      </template>
    </el-dialog>

    <!-- Guide Preview Dialog -->
    <GuidePreviewDialog
      v-model="state.guidePreviewVisible"
      :guide-id="state.guidePreviewId"
      :guide-data="state.selectedGuide"
      :equipment-type-list="state.equipmentTypeList"
    />

    <!-- Quick Create Guide Dialog -->
    <MaintenanceGuideFormDialog
      v-model="state.quickGuideDialogVisible"
      operate-type="add"
      :initial-data="state.quickGuideInitialData"
      :equipment-type-list="state.equipmentTypeList"
      @success="handleQuickGuideSuccess"
    />
  </div>
</template>

<script setup lang="ts">
import { reactive, ref, computed, onMounted, defineAsyncComponent, nextTick } from 'vue';
import { useI18n } from 'vue-i18n';
import { ElMessage, ElMessageBox } from 'element-plus';
import { getAPI } from '/@/utils/axios-utils';
import { TmsMaintenanceScheduleApi, TmsEquipmentTypeApi, TmsPatrolTeamApi, TmsEquipmentApi, TmsMaintenanceGuideApi, TmsMaintenanceAlertApi } from '/@/api-services/tms';
import { SysUserApi } from '/@/api-services/system';
import { 
  MaintenanceScheduleOutput,
  MaintenanceScheduleInput,
  MaintenanceScheduleFilterMode,
  AddMaintenanceScheduleInput,
  UpdateMaintenanceScheduleInput,
  MaintenanceHistoryQueryInput,
  MaintenanceTaskOutput,
  AddTaskToScheduleInput,
  RemoveTaskFromScheduleInput,
  ScheduleEquipmentInput,
  MaintenanceAlertListOutput,
} from '/@/api-services/tms/models';
import { BaseConfigTypeEnum } from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';
import { useUserInfo } from '/@/stores/userInfo';
import FullCalendar from '@fullcalendar/vue3';
import dayGridPlugin from '@fullcalendar/daygrid';
import timeGridPlugin from '@fullcalendar/timegrid';
import interactionPlugin from '@fullcalendar/interaction';
import type { CalendarOptions } from '@fullcalendar/core';

const SearchButton = defineAsyncComponent(() => import('/@/components/advanced/search-button-operation.vue'));
const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));
import YearGridView from './yearGridView.vue';
import TeamQuickSelect from '/@/views/tms/dutySchedule/component/teamQuickSelect.vue';
import PatrolTeamManage from '/@/views/tms/dutySchedule/component/patrolTeamManage.vue';
import MaintenanceGuideFormDialog from './maintenanceGuideFormDialog.vue';
import GuidePreviewDialog from './guidePreviewDialog.vue';
import Fieldset from 'primevue/fieldset';
import { VxeGridInstance, VxeGridListeners, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { PageEquipmentInput, PageEquipmentOutput } from '/@/api-services/tms';

const { t: $t } = useI18n();
const userInfoStore = useUserInfo();
const baseConfigStore = useBaseConfig();
const { getConfigDataByCode, getConfigItemByCode } = baseConfigStore;
const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));

const fullCalendar = ref();
const xGrid = ref<VxeGridInstance>();
const formRef = ref();
const teamQuickSelectRef = ref();
const patrolTeamManageRef = ref();

// State
const state = reactive({
  loading: false,
  detailLoading: false,
  formLoading: false,
  historyLoading: false,
  
  queryParams: {
    status: undefined as string | undefined,
    priority: undefined as string | undefined,
    equipmentTypeId: undefined as string | undefined,
  },
  quickFilter: 'createdByMe' as 'createdByMe' | 'related' | 'all',
  searchText: '',
  displayMode: 'calendar' as 'calendar' | 'list',
  currentView: 'dayGridMonth' as string,
  currentDate: new Date(),
  currentYear: new Date().getFullYear(),
  
  scheduleList: [] as MaintenanceScheduleOutput[],
  equipmentTypeList: [] as any[],
  
  // Detail dialog
  detailDialogVisible: false,
  detailDialogTitle: '',
  selectedSchedule: null as MaintenanceScheduleOutput | null,
  detailActiveTab: 'info',
  historyData: [] as any[],
  
  // Edit dialog
  editDialogVisible: false,
  editDialogTitle: '',
  operateType: 'add' as 'add' | 'edit',
  formData: {
    id: undefined as string | undefined,
    name: '',
    scheduledDate: '',
    dueDate: '',
    priority: 'medium',
    reminderDaysBefore: 3,
    status: 'register',
    remark: '',
    // Personnel for new schedule
    tasks: [] as any[],
  },
  
  // Personnel selection in edit form
  selectedLeaderIds: [] as string[],
  selectedMemberIds: [] as string[],
  
  // Personnel management
  employeeList: [] as any[],
  teamList: [] as any[],
  addPersonnelDialogVisible: false,
  changeRoleDialogVisible: false,
  personnelActionLoading: false,
  newPersonnelForm: {
    userId: '',
    role: 'member',
    taskName: '',
    description: '',
    remark: ''
  },
  changeRoleForm: {
    taskId: '',
    userId: '',
    currentRole: '',
    newRole: '' as string,
    reason: '',
    // preserve task content when changing role
    scheduleEquipmentId: '' as string,
    taskName: '' as string,
    description: '' as string,
  },

  // Task status modal (Tab 3)
  taskStatusDialogVisible: false,
  taskStatusSaving: false,
  taskStatusForm: {
    taskId: '' as string,
    assignedUserId: '' as string,
    assignedUserName: '' as string,
    status: 'pending' as 'pending' | 'finish' | 'skipped',
  },
  
  // Equipment selection in edit form
  equipmentList: [] as any[],
  guideList: [] as any[],
  selectedEquipments: [] as { 
    equipmentId: string; 
    equipmentName?: string;
    equipmentCode?: string;
    equipmentTypeId?: string;
    equipmentTypeName?: string;
    equipmentState?: number | string;
    maintenanceGuideId?: string; 
    remark?: string 
  }[],
  equipmentLoading: false,
  guideLoading: false,
  
  // Equipment selector dialog
  equipmentSelectorVisible: false,
  equipmentSelectorTypeId: undefined as string | undefined,
  equipmentSelectorStateFilter: undefined as string | undefined,
  equipmentSearchText: '',
  tempSelectedEquipments: [] as any[],
  
  // Guide preview dialog
  guidePreviewVisible: false,
  guidePreviewLoading: false,
  guidePreviewId: undefined as string | undefined,
  selectedGuide: null as any,
  
  // Equipment alerts map (equipmentId -> alerts[])
  equipmentAlertsMap: new Map<string, MaintenanceAlertListOutput[]>(),
  
  // Equipment alert preview dialog
  equipmentAlertPreviewVisible: false,
  equipmentAlertPreviewLoading: false,
  selectedEquipmentAlerts: [] as MaintenanceAlertListOutput[],
  selectedEquipmentForAlert: null as any,
  
  // Quick create guide dialog
  quickGuideDialogVisible: false,
  quickGuideInitialData: undefined as any,
  // Store the equipment row that triggered quick guide creation
  pendingEquipmentForGuide: null as any,

  // Equipment check modal (Tab 2)
  equipmentCheckDialogVisible: false,
  equipmentCheckSaving: false,
  equipmentCheckForm: {
    scheduleEquipmentId: '' as string,
    equipmentName: '' as string,
    status: 'pending' as 'pending' | 'checked' | 'skipped',
    checkedNote: '' as string,
  },
});

// Options
const statusOptions = computed(() => {
  const cfg = getConfigDataByCode(BaseConfigTypeEnum.TmsMaintenanceScheduleStatus) || [];
  return cfg.map(i => ({ value: i.code, label: i.name }));
});

const priorityOptions = computed(() => {
  const cfg = getConfigDataByCode(BaseConfigTypeEnum.TmsMaintenancePriority) || [];
  return cfg.map(i => ({ value: i.code, label: i.name }));
});

const frequencyOptions = computed(() => {
  const cfg = getConfigDataByCode(BaseConfigTypeEnum.TmsMaintenanceFrequency) || [];
  return cfg.map(i => ({ value: i.code, label: i.name }));
});

const equipmentStatusOptions = computed(() => {
  const cfg = getConfigDataByCode(BaseConfigTypeEnum.TmsMaintenanceEquipmentStatus) || [];
  return cfg.map(i => ({ value: i.code, label: i.name }));
});

const taskStatusOptions = computed(() => {
  const cfg = getConfigDataByCode(BaseConfigTypeEnum.TmsMaintenanceTaskStatus) || [];
  return cfg.map(i => ({ value: i.code, label: i.name }));
});

const alertTypeOptions = computed(() => {
  const cfg = getConfigDataByCode(BaseConfigTypeEnum.TmsMaintenanceAlertType) || [];
  return cfg.map(i => ({ value: i.code, label: i.name }));
});

const alertStatusOptions = computed(() => {
  const cfg = getConfigDataByCode(BaseConfigTypeEnum.TmsMaintenanceAlertStatus) || [];
  return cfg.map(i => ({ value: i.code, label: i.name }));
});

const displayModeOptions = computed(() => [
  { label: $t('lz.label.tmsMaintenanceSchedule.calendarView'), value: 'calendar' },
  { label: $t('lz.label.tmsMaintenanceSchedule.listView'), value: 'list' },
]);

// Grid Options for vxe-grid
const gridOptions = reactive({
  id: 'maintenanceSchedule',
  border: true,
  resizable: true,
  showOverflow: true,
  loading: false,
  height: 580,
  rowConfig: {
    keyField: 'id',
    isHover: true,
  },
  columnConfig: {
    resizable: true,
    isCurrent: false,
  },
  customConfig: {
    storage: {
      visible: true,
      resizable: true,
    },
  },
  data: [] as MaintenanceScheduleOutput[],
  columns: [
    { type: 'seq' as const, width: 60, title: $t('lz.label.base.idx'), fixed: 'left' as const },
    { field: 'code', title: $t('lz.entity.base.code'), minWidth: 140, fixed: 'left' as const, slots: { default: 'code_default' } },
    { field: 'name', title: $t('lz.label.tmsMaintenanceSchedule.name'), minWidth: 200 },
    { field: 'scheduledDate', title: $t('lz.label.tmsMaintenanceSchedule.scheduledDate'), width: 120, align: 'center' as const, slots: { default: 'scheduledDate_default' } },
    { field: 'dueDate', title: $t('lz.label.tmsMaintenanceSchedule.dueDate'), width: 120, align: 'center' as const, slots: { default: 'dueDate_default' } },
    { field: 'priority', title: $t('lz.entity.base.priority'), width: 100, align: 'center' as const, slots: { default: 'priority_default' } },
    { field: 'status', title: $t('lz.entity.base.status'), width: 120, align: 'center' as const, slots: { default: 'status_default' } },
    { field: 'equipmentCount', title: $t('lz.label.tmsMaintenanceSchedule.equipmentCount'), width: 100, align: 'center' as const, slots: { default: 'equipmentCount_default' } },
    { field: 'taskCount', title: $t('lz.label.tmsMaintenanceSchedule.taskCount'), width: 100, align: 'center' as const, slots: { default: 'taskCount_default' } },
    { title: $t('lz.label.base.action'),align:'center', fixed: 'right' as const, width: 120, showOverflow: true, slots: { default: 'row_buttons' } },
  ],
  
  toolbarConfig: {
    slots: {
      tools: 'toolbar_tools',
    },
  },
});

const gridEvents: VxeGridListeners = {
  toolbarToolClick() {
    // Handle toolbar clicks if needed
  },
};

// Equipment Selector Grid Options - using useVxeTable hook with pagination like equipment page
const equipmentSelectorGridOptions = useVxeTable<PageEquipmentOutput>(
  {
    id: 'equipmentSelector',
    name: 'EquipmentSelector',
    columns: [
      { type: 'checkbox', fixed: 'left', width: 45 },
      { field: 'code', title: $t('lz.entity.base.code'), minWidth: 120, showOverflow: 'tooltip', sortable: true },
      { field: 'name', title: $t('lz.entity.base.name'), minWidth: 150, showOverflow: 'tooltip', sortable: true },
      { field: 'equipmentTypeId', title: $t('lz.entity.tms.equipmentType'), minWidth: 150, showOverflow: 'tooltip', slots: { default: 'row_equip_type' } },
      { field: 'equipmentState', title: $t('lz.entity.tms.equipmentState'), width: 130, showOverflow: 'tooltip', slots: { default: 'row_equip_state' } },
      { title: $t('lz.label.base.action'), fixed: 'right', width: 80, showOverflow: true, slots: { default: 'row_alert' } },
    ],
  },
  {
    height: 350,
    proxyConfig: { 
      autoLoad: false, 
      ajax: { 
        query: ({ page, sort }) => handleEquipmentSelectorQueryApi(page, sort) 
      } 
    },
    sortConfig: { defaultSort: { field: 'code', order: 'asc' } },
    pagerConfig: { pageSize: 20, pageSizes: [20, 50, 100] },
    toolbarConfig: { enabled: false },
  }
);

// Equipment Selector Grid Events
const equipmentSelectorGridEvents: VxeGridListeners = {};

// Equipment Selector Query API
const handleEquipmentSelectorQueryApi = async (
  page: VxeGridPropTypes.ProxyAjaxQueryPageParams, 
  sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams
) => {
  const params: PageEquipmentInput = {
    page: page.currentPage,
    pageSize: page.pageSize,
    field: sort.field || 'code',
    order: sort.order || 'asc',
    descStr: 'desc',
    name: state.equipmentSearchText || undefined,
    code: state.equipmentSearchText || undefined,
    equipmentTypeId: state.equipmentSelectorTypeId || undefined,
    equipmentState: state.equipmentSelectorStateFilter || undefined,
  };
  return await getAPI(TmsEquipmentApi).apiTmsTmsequipmentPagePost(params);
};

// Equipment Selector Query
const handleEquipmentSelectorQuery = async () => {
  await equipmentSelectorGrid.value?.commitProxy('reload') ;
};

// Equipment Selector Reset
const handleEquipmentSelectorReset = async () => {
  state.equipmentSearchText = '';
  state.equipmentSelectorTypeId = undefined;
  state.equipmentSelectorStateFilter = undefined;
  await equipmentSelectorGrid.value?.commitProxy('reload');
};

// Form rules
const formRules = {
  name: [{ required: true, message: $t('lz.message.tmsMaintenanceSchedule.pleaseInputName'), trigger: 'blur' }],
  scheduledDate: [{ required: true, message: $t('lz.message.tmsMaintenanceSchedule.pleaseSelectDate'), trigger: 'change' }],
  priority: [{ required: true, message: $t('lz.message.tmsMaintenanceSchedule.pleaseSelectPriority'), trigger: 'change' }],
};

// Calendar options
const calendarOptions = computed<CalendarOptions>(() => ({
  plugins: [dayGridPlugin, timeGridPlugin, interactionPlugin],
  initialView: state.currentView,
  locale: 'vi',
  headerToolbar: false,
  height: 'calc(100vh - 350px)',
  events: state.scheduleList.map(schedule => ({
    id: schedule.id || undefined,
    title: schedule.name || '',
    start: schedule.scheduledDate,
    end: schedule.dueDate || undefined,
    backgroundColor: getStatusColor(schedule.status),
    borderColor: getStatusColor(schedule.status),
    extendedProps: { schedule },
  })),
  dateClick: (arg: any) => {
    handleDateClick(arg);
  },
  eventClick: (info: any) => {
    handleViewDetail(info.event.extendedProps.schedule);
  },
  datesSet: (info: any) => {
    state.currentDate = info.view.currentStart;
    loadSchedules();
  },
}));

// Computed
const currentPeriodText = computed(() => {
  if (state.currentView === 'dayGridYear') {
    return `Năm ${state.currentYear}`;
  }
  const date = state.currentDate;
  const monthNames = [$t('lz.label.base.monthNumber', { month: 1 }), $t('lz.label.base.monthNumber', { month: 2 }), $t('lz.label.base.monthNumber', { month: 3 }), $t('lz.label.base.monthNumber', { month: 4 }), $t('lz.label.base.monthNumber', { month: 5 }), $t('lz.label.base.monthNumber', { month: 6 }),
    $t('lz.label.base.monthNumber', { month: 7 }), $t('lz.label.base.monthNumber', { month: 8 }), $t('lz.label.base.monthNumber', { month: 9 }), $t('lz.label.base.monthNumber', { month: 10 }), $t('lz.label.base.monthNumber', { month: 11 }), $t('lz.label.base.monthNumber', { month: 12 })];
  return `${monthNames[date.getMonth()]} ${date.getFullYear()}`;
});

// Calendar events for YearGridView
const calendarEvents = computed(() => {
  return state.scheduleList.map(schedule => ({
    id: schedule.id,
    name: schedule.name || '',
    status: normalizeScheduleStatusKey(schedule.status) || 'register',
    priority: normalizePriorityKey(schedule.priority) || 'medium',
    startTime: schedule.scheduledDate,
    endTime: schedule.dueDate || schedule.scheduledDate,
    start: schedule.scheduledDate,
    end: schedule.dueDate || schedule.scheduledDate,
    creatorName: (schedule as any).creatorName || schedule.createUId || '',
    extendedProps: { schedule },
  }));
});

// Maintenance status helper functions
const getMaintenanceStatusConfig = (status?: string | null) => {
  const s = normalizeScheduleStatusKey(status) || 'register';
  const statusConfigs: Record<string, any> = {
    'register': { code: 'register', tagType: 'info' },
    'confirm': { code: 'confirm', tagType: 'primary' },
    'process': { code: 'process', tagType: 'warning' },
    'finish': { code: 'finish', tagType: 'success' },
    'cancel': { code: 'cancel', tagType: 'info' },
    'overdue': { code: 'overdue', tagType: 'danger' },
  };
  const option = statusOptions.value.find(o => o.value === s);
  return statusConfigs[s]
    ? { ...statusConfigs[s], name: option?.label || s }
    : { code: s, name: option?.label || s, tagType: 'info' };
};

const getMaintenanceStepClass = (step: string) => {
  if (!state.selectedSchedule) return '';
  
  const statusOrder = ['register', 'confirm', 'process', 'finish'];
  const rawStatus = normalizeScheduleStatusKey(state.selectedSchedule.status) || 'register';
  const currentStatus = rawStatus === 'overdue' ? 'process' : rawStatus;
  const currentIndex = statusOrder.indexOf(currentStatus);
  
  if (currentStatus === 'cancel') return 'step-cancelled';
  
  const stepMap: Record<string, number> = {
    'register': 0,
    'confirm': 1,
    'process': 2,
    'finish': 3,
  };
  
  const stepIndex = stepMap[step.toLowerCase()];
  
  if (stepIndex < currentIndex) return 'step-completed';
  if (stepIndex === currentIndex) return 'step-active';
  return 'step-pending';
};

const handleMaintenanceStepClick = async (newStatus: string) => {
  if (!state.selectedSchedule?.id) return;
  
  const rawStatus = normalizeScheduleStatusKey(state.selectedSchedule.status) || 'register';
  const currentStatus = rawStatus === 'overdue' ? 'process' : rawStatus;

  if (currentStatus === newStatus) return;
  if (rawStatus === 'cancel' || rawStatus === 'finish') {
    ElMessage.warning($t('lz.message.tmsMaintenanceSchedule.cannotChangeStatus'));
    return;
  }
  
  // Validate status transition
  const statusOrder = ['register', 'confirm', 'process', 'finish'];
  const currentIndex = statusOrder.indexOf(currentStatus);
  const newIndex = statusOrder.indexOf(newStatus);
  
  // Can only go forward one step at a time
  if (newIndex !== currentIndex + 1) {
    ElMessage.warning($t('lz.message.tmsMaintenanceSchedule.invalidStatusTransition'));
    return;
  }
  
  try {
    await ElMessageBox.confirm(
      $t('lz.message.tmsMaintenanceSchedule.confirmStatusChange', { status: getStatusLabel(newStatus) }),
      $t('lz.message.base.warning'),
      {
        confirmButtonText: $t('lz.button.base.confirm'),
        cancelButtonText: $t('lz.button.base.cancel'),
        type: 'warning',
      }
    );
    
    state.detailLoading = true;
    
    await getAPI(TmsMaintenanceScheduleApi).apiTmsTmsmaintenancescheduleUpdatePost({
      id: state.selectedSchedule.id,
      name: state.selectedSchedule.name || '',
      scheduledDate: new Date(state.selectedSchedule.scheduledDate || new Date()),
      status: newStatus,
    });
    
    // Update local state
    state.selectedSchedule.status = newStatus;
    
    ElMessage.success($t('lz.message.base.updateSuccess'));
    loadSchedules();
  } catch (error: any) {
    if (error !== 'cancel') {
      console.error('Error changing status:', error);
      ElMessage.error(error?.message || $t('lz.message.base.error'));
    }
  } finally {
    state.detailLoading = false;
  }
};

// Methods
const getViewLabel = (view: string) => {
  const labels: Record<string, string> = {
    'dayGridMonth': $t('lz.label.base.month'),
    'timeGridWeek': $t('lz.label.base.week'),
    'dayGridYear': $t('lz.label.base.year'),
  };
  return labels[view] || view;
};

const getPriorityColor = (priority?: string | null) => {
  const colors: Record<string, string> = {
    'high': '#F56C6C',
    'medium': '#E6A23C',
    'low': '#67C23A',
  };
  const key = normalizePriorityKey(priority) || 'medium';
  return colors[key] || '#409EFF';
};

const getPriorityTagType = (priority?: string | null) => {
  const types: Record<string, string> = {
    'high': 'danger',
    'medium': 'warning',
    'low': 'success',
  };
  return types[normalizePriorityKey(priority) || ''] || 'info';
};

const getPriorityLabel = (priority?: string | null) => {
  const key = normalizePriorityKey(priority);
  const option = priorityOptions.value.find(o => o.value === key);
  return option?.label || key || '-';
};

const getFrequencyLabel = (frequency?: string | null) => {
  const raw = (frequency || '').toString().trim();
  if (!raw) return '-';
  const key = raw.toLowerCase();
  const option = frequencyOptions.value.find(o => o.value === key);
  return option?.label || raw;
};

const handleOpenDocument = (doc: any) => {
  if (doc.url || doc.fileUrl) {
    window.open(doc.url || doc.fileUrl, '_blank');
  } else {
    ElMessage.info($t('lz.message.tmsMaintenanceSchedule.documentNotAvailable'));
  }
};

const getStatusTagType = (status?: string | null) => {
  const types: Record<string, string> = {
    'register': 'info',
    'confirm': 'primary',
    'process': 'warning',
    'finish': 'success',
    'cancel': 'info',
    'overdue': 'danger',
  };
  return types[normalizeScheduleStatusKey(status) || ''] || 'info';
};

const getStatusColor = (status?: string | null) => {
  const colors: Record<string, string> = {
    'register': 'var(--el-color-info)',
    'confirm': 'var(--el-color-primary)',
    'process': 'var(--el-color-warning)',
    'finish': 'var(--el-color-success)',
    'cancel': 'var(--el-color-info)',
    'overdue': 'var(--el-color-danger)',
  };
  return colors[normalizeScheduleStatusKey(status) || 'register'] || 'var(--el-color-info)';
};

const getStatusLabel = (status?: string | null) => {
  const key = normalizeScheduleStatusKey(status);
  const option = statusOptions.value.find(o => o.value === key);
  return option?.label || key || '-';
};

const getEquipmentStatusTagType = (status?: string | null) => {
  const types: Record<string, string> = {
    'pending': 'info',
    'checked': 'success',
    'skipped': 'warning',
  };
  return types[normalizeEquipmentStatusKey(status) || ''] || 'info';
};

const getEquipmentStatusLabel = (status?: string | null) => {
  const key = normalizeEquipmentStatusKey(status);
  const option = equipmentStatusOptions.value.find(o => o.value === (key || ''));
  return option?.label || key || '-';
};

const getTaskStatusTagType = (status?: string | null) => {
  const key = normalizeTaskStatusKey(status);
  const types: Record<string, string> = {
    'pending': 'info',
    'finish': 'success',
    'skipped': 'warning',
  };
  return types[key || ''] || 'info';
};

const getTaskStatusLabel = (status?: string | null) => {
  const key = normalizeTaskStatusKey(status);
  const option = taskStatusOptions.value.find(o => o.value === (key || ''));
  return option?.label || key || '-';
};

const formatDate = (date?: Date | string | null) => {
  if (!date) return '';
  const d = new Date(date);
  return d.toLocaleDateString('vi-VN');
};

const formatDateTime = (date?: Date | string | null) => {
  if (!date) return '';
  const d = new Date(date);
  return d.toLocaleString('vi-VN');
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
  const raw = (actionType || '').toString();
  const normalized = raw.trim().toLowerCase();

  // Try from config first
  const configItem = getConfigItemByCode(BaseConfigTypeEnum.TmsDutyAction, normalized);
  if (configItem?.name) return configItem;
  
  // Fallback to localized values if config not available
  const actionConfigs: Record<string, any> = {
    // canonical keys (only those we already have translation keys for)
    'create': { code: 'create', name: $t('lz.label.tmsMaintenanceSchedule.actionCreated'), tagType: 'success' },
    'update': { code: 'update', name: $t('lz.label.tmsMaintenanceSchedule.actionUpdated'), tagType: 'primary' },
    'status_change': { code: 'status_change', name: $t('lz.label.tmsMaintenanceSchedule.actionStatusChanged'), tagType: 'warning' },
    'complete_task': { code: 'complete_task', name: $t('lz.label.tmsMaintenanceSchedule.actionCompleted'), tagType: 'success' },
    'cancel': { code: 'cancel', name: $t('lz.label.tmsMaintenanceSchedule.actionCancelled'), tagType: 'info' },

    // legacy fallbacks (TitleCase)
    'Created': { code: 'create', name: $t('lz.label.tmsMaintenanceSchedule.actionCreated'), tagType: 'success' },
    'Updated': { code: 'update', name: $t('lz.label.tmsMaintenanceSchedule.actionUpdated'), tagType: 'primary' },
    'StatusChanged': { code: 'status_change', name: $t('lz.label.tmsMaintenanceSchedule.actionStatusChanged'), tagType: 'warning' },
    'UserAdded': { code: 'user_added', name: $t('lz.label.tmsMaintenanceSchedule.actionUserAdded'), tagType: 'success' },
    'UserRemoved': { code: 'user_removed', name: $t('lz.label.tmsMaintenanceSchedule.actionUserRemoved'), tagType: 'danger' },
    'RoleChanged': { code: 'role_changed', name: $t('lz.label.tmsMaintenanceSchedule.actionRoleChanged'), tagType: 'warning' },
    'Completed': { code: 'complete_task', name: $t('lz.label.tmsMaintenanceSchedule.actionCompleted'), tagType: 'success' },
    'Cancelled': { code: 'cancel', name: $t('lz.label.tmsMaintenanceSchedule.actionCancelled'), tagType: 'info' },
  };

  return actionConfigs[normalized] || actionConfigs[raw] || { code: normalized || raw, name: raw, tagType: 'info' };
};

/**
 * Get action type tag type for history
 */
const getHistoryActionTypeTagType = (actionType: string) => {
  const configItem = getHistoryActionConfig(actionType);
  return configItem?.tagType || 'info';
};

/**
 * Get equipment alerts from the map
 */
const getEquipmentAlerts = (equipmentId: string): MaintenanceAlertListOutput[] => {
  return state.equipmentAlertsMap.get(equipmentId) || [];
};

/**
 * Load equipment alerts from API
 */
const loadEquipmentAlerts = async () => {
  try {
    // Fetch all pending and acknowledged alerts
    const res = await getAPI(TmsMaintenanceAlertApi).apiTmsTmsmaintenancealertGetlistPost({
      page: 1,
      pageSize: 1000, // Get all alerts
      status: 'pending', // Only pending alerts
    });
    
    if (res.data.succeeded && res.data.result?.records) {
      // Group alerts by equipmentId
      const alertsMap = new Map<string, MaintenanceAlertListOutput[]>();
      for (const alert of res.data.result.records) {
        if (alert.equipmentId) {
          const existingAlerts = alertsMap.get(alert.equipmentId) || [];
          existingAlerts.push(alert);
          alertsMap.set(alert.equipmentId, existingAlerts);
        }
      }
      state.equipmentAlertsMap = alertsMap;
    }
  } catch (error) {
    console.error('Error loading equipment alerts:', error);
  }
};

/**
 * Preview equipment alerts (open alert dialog)
 */
const handlePreviewEquipmentAlert = async (equipment: any) => {
  if (!equipment?.id) return;
  
  state.selectedEquipmentForAlert = equipment;
  state.selectedEquipmentAlerts = getEquipmentAlerts(equipment.id);
  state.equipmentAlertPreviewVisible = true;
  
  // If no alerts in cache, try to fetch from API
  if (state.selectedEquipmentAlerts.length === 0) {
    state.equipmentAlertPreviewLoading = true;
    try {
      const res = await getAPI(TmsMaintenanceAlertApi).apiTmsTmsmaintenancealertGetlistPost({
        page: 1,
        pageSize: 100,
        equipmentId: equipment.id,
        status: 'pending',
      });
      
      if (res.data.succeeded && res.data.result?.records) {
        state.selectedEquipmentAlerts = res.data.result.records;
      }
    } catch (error) {
      console.error('Error loading equipment alerts:', error);
      ElMessage.error($t('lz.message.base.acctionFail'));
    } finally {
      state.equipmentAlertPreviewLoading = false;
    }
  }
};

/**
 * Get alert type tag type
 */
const getAlertTypeTagType = (alertType: string) => {
  const key = normalizeAlertTypeKey(alertType);
  const typeMap: Record<string, string> = {
    // canonical
    'rule_triggered': 'danger',
    'schedule_due': 'warning',
    'manual_request': 'info',
  };
  return typeMap[key] || 'info';
};

/**
 * Get alert type label
 */
const getAlertTypeLabel = (alertType: string) => {
  const key = normalizeAlertTypeKey(alertType);
  const option = alertTypeOptions.value.find(o => o.value === key);
  return option?.label || alertType;
};

/**
 * Get alert status tag type
 */
const getAlertStatusTagType = (status: string) => {
  const key = normalizeAlertStatusKey(status);
  const statusMap: Record<string, string> = {
    // canonical
    'pending': 'danger',
    'acknowledged': 'warning',
    'schedule': 'primary',
    'dismissed': 'info',
  };
  return statusMap[key] || 'info';
};

/**
 * Get alert status label
 */
const getAlertStatusLabel = (status: string) => {
  const key = normalizeAlertStatusKey(status);
  const option = alertStatusOptions.value.find(o => o.value === key);
  return option?.label || status;
};

/**
 * Format date time for alert display
 */
const formatAlertDateTime = (dateTime: any) => {
  if (!dateTime) return '-';
  const date = new Date(dateTime);
  return date.toLocaleString('vi-VN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  });
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

// Equipment helper functions
const getEquipmentName = (equipmentId: string) => {
  const equipment = state.equipmentList.find(e => e.id === equipmentId);
  return equipment?.name || equipmentId;
};

const getEquipmentTypeName = (equipmentId: string) => {
  const equipment = state.equipmentList.find(e => e.id === equipmentId);
  if (!equipment?.equipmentTypeId) return '-';
  const equipmentType = state.equipmentTypeList.find(t => t.id === equipment.equipmentTypeId);
  return equipmentType?.name || '-';
};

const getGuidesForEquipment = (equipmentId: string) => {
  const equipment = state.equipmentList.find(e => e.id === equipmentId);
  if (!equipment) return [];
  // Filter guides by equipment type
  return state.guideList.filter(g => g.equipmentTypeId === equipment.equipmentTypeId);
};

// Get guides for equipment by equipmentTypeId directly
const getGuidesForEquipmentByTypeId = (equipmentTypeId?: string) => {
  if (!equipmentTypeId) return [];
  const typeId = String(equipmentTypeId);
  return state.guideList.filter(g => String(g.equipmentTypeId) === typeId);
};

// Get equipment type name by equipmentTypeId (not equipmentId)
const getEquipmentTypeNameById = (equipmentTypeId: string) => {
  if (!equipmentTypeId) return '-';
  const equipmentType = state.equipmentTypeList.find(t => t.id === equipmentTypeId);
  return equipmentType?.name || '-';
};

const loadAllGuides = async () => {
  try {
    state.guideLoading = true;
    const response = await getAPI(TmsMaintenanceGuideApi).apiTmsTmsmaintenanceguideGetlistPost({});
    const raw = response.data?.result || [];
    state.guideList = (Array.isArray(raw) ? raw : []).map((g: any) => {
      const id = firstDefined<string>(g?.id, g?.ID, g?.Id);
      const name = firstDefined<string>(g?.name, g?.Name);
      const equipmentTypeId = firstDefined<string>(g?.equipmentTypeId, g?.EquipmentTypeId);
      return {
        ...g,
        id: id != null ? String(id) : undefined,
        name,
        equipmentTypeId: equipmentTypeId != null ? String(equipmentTypeId) : undefined,
      };
    });
  } catch (error) {
    console.error('Error loading guides:', error);
    state.guideList = [];
  } finally {
    state.guideLoading = false;
  }
};

const handleOpenEquipmentSelector = async () => {
  state.equipmentSelectorVisible = true;
  state.tempSelectedEquipments = [];
  state.equipmentSearchText = '';
  state.equipmentSelectorTypeId = undefined;
  state.equipmentSelectorStateFilter = undefined;
  
  // Load equipment types if not loaded
  if (state.equipmentTypeList.length === 0) {
    try {
      const typeResponse = await getAPI(TmsEquipmentTypeApi).apiTmsTmsequipmenttypeListGet();
      state.equipmentTypeList = typeResponse.data?.result || [];
    } catch (error) {
      console.error('Error loading equipment types:', error);
    }
  }
  
  // Load guides if not loaded
  if (state.guideList.length === 0) {
    await loadAllGuides();
  }
  
  // Load equipment alerts
  await loadEquipmentAlerts();
  
  // Load equipment list via grid proxy (with pagination)
  await nextTick();
  await equipmentSelectorGrid.value?.commitProxy('reload');
};

const handleEquipmentSelectionChange = (selection: any[]) => {
  state.tempSelectedEquipments = selection;
};

// VXE Grid selection handlers for equipment selector
const equipmentSelectorGrid = ref<VxeGridInstance>();
const onEquipmentGridCheck = () => {
  const records = equipmentSelectorGrid.value?.getCheckboxRecords() || [];
  state.tempSelectedEquipments = records;
};
const onEquipmentGridCheckAll = () => {
  const records = equipmentSelectorGrid.value?.getCheckboxRecords() || [];
  state.tempSelectedEquipments = records;
};

const handleConfirmEquipmentSelection = () => {
  // Add selected equipments to the form
  state.tempSelectedEquipments.forEach(equipment => {
    // Check if already added
    if (!state.selectedEquipments.find(e => e.equipmentId === equipment.id)) {
      // Get equipment type name from the type list
      const equipmentTypeName = equipment.equipmentTypeId 
        ? state.equipmentTypeList.find(t => t.id === equipment.equipmentTypeId)?.name 
        : undefined;
      
      state.selectedEquipments.push({
        equipmentId: equipment.id,
        equipmentName: equipment.name,
        equipmentCode: equipment.code,
        equipmentTypeId: equipment.equipmentTypeId != null ? String(equipment.equipmentTypeId) : undefined,
        equipmentTypeName: equipmentTypeName,
        equipmentState: equipment.equipmentState, // Store current state for historical record
        maintenanceGuideId: undefined,
        remark: undefined
      });
    }
  });
  state.equipmentSelectorVisible = false;
  ElMessage.success($t('lz.message.tmsMaintenanceSchedule.equipmentAddedSuccess'));
};

const handleRemoveEquipment = (index: number) => {
  state.selectedEquipments.splice(index, 1);
};

// Create new guide for equipment type - Open quick create dialog
const handleCreateGuideForEquipment = (equipment: any) => {
  // Set initial data for the guide dialog
  state.quickGuideInitialData = {
    equipmentTypeId: equipment.equipmentTypeId || '',
  };
  state.pendingEquipmentForGuide = equipment;
  state.quickGuideDialogVisible = true;
};

// Handle quick guide creation success
const handleQuickGuideSuccess = async () => {
  // Reload guides list
  await loadAllGuides();
  
  // If there's a pending equipment, try to auto-assign the newly created guide
  if (state.pendingEquipmentForGuide) {
    const equipment = state.pendingEquipmentForGuide;
    // Find guides for this equipment type
    const guidesForType = state.guideList.filter((g: any) => String(g.equipmentTypeId) === String(equipment.equipmentTypeId));
    
    if (guidesForType.length > 0) {
      // Find the equipment in selectedEquipments and assign the first guide
      const selectedEquip = state.selectedEquipments.find(e => e.equipmentId === equipment.equipmentId);
      if (selectedEquip) {
        // Auto-select the newest guide (first one in the filtered list, usually sorted by createdTime desc)
        selectedEquip.maintenanceGuideId = guidesForType[0].id != null ? String(guidesForType[0].id) : undefined;
        ElMessage.success($t('lz.message.tmsMaintenanceSchedule.guideAutoAssigned'));
      }
    }
  }
  
  state.pendingEquipmentForGuide = null;
};

// Get guide name from guide list
const getGuideName = (guideId: string) => {
  const guide = state.guideList.find((g: any) => g.id === guideId);
  return guide?.name || '-';
};

// Guide Preview from Equipment tab in detail dialog
const handlePreviewGuideFromEquipment = (guideId: string) => {
  state.guidePreviewId = guideId;
  // Try to find guide in already loaded list, otherwise dialog will load from API
  state.selectedGuide = state.guideList.find((g: any) => g.id === guideId) || null;
  state.guidePreviewVisible = true;
};

// Guide Preview
const handlePreviewGuide = async (guideId: string) => {
  try {
    state.guidePreviewLoading = true;
    state.guidePreviewVisible = true;
    
    // Find guide from already loaded list
    const guide = state.guideList.find(g => g.id === guideId);
    if (guide) {
      state.selectedGuide = guide;
    } else {
      // Try to load from API if not found in list
      const response = await getAPI(TmsMaintenanceGuideApi).apiTmsTmsmaintenanceguideGetlistPost({});
      const guides = response.data?.result || [];
      state.selectedGuide = guides.find((g: any) => g.id === guideId) || null;
    }
  } catch (error) {
    console.error('Error loading guide:', error);
    ElMessage.error($t('lz.message.base.errorFetchingData'));
    state.guidePreviewVisible = false;
  } finally {
    state.guidePreviewLoading = false;
  }
};

// API calls
const loadSchedules = async () => {
  try {
    state.loading = true;
    
    let filterMode: MaintenanceScheduleFilterMode | undefined;
    if (state.quickFilter === 'createdByMe') {
      filterMode = MaintenanceScheduleFilterMode.NUMBER_1;
    } else if (state.quickFilter === 'related') {
      filterMode = MaintenanceScheduleFilterMode.NUMBER_2;
    } else {
      filterMode = MaintenanceScheduleFilterMode.NUMBER_0;
    }
    
    const input: MaintenanceScheduleInput = {
      name: state.searchText || undefined,
      status: state.queryParams.status || undefined,
      priority: state.queryParams.priority || undefined,
      equipmentTypeId: state.queryParams.equipmentTypeId || undefined,
      filterMode,
    };
    
    // Add date range for calendar/year view
    if (state.displayMode === 'calendar') {
      if (state.currentView === 'dayGridYear') {
        // For year view, load whole year data
        input.fromDate = new Date(state.currentYear, 0, 1);
        input.toDate = new Date(state.currentYear, 11, 31);
      } else if (fullCalendar.value?.getApi) {
        const view = fullCalendar.value.getApi().view;
        input.fromDate = view.activeStart;
        input.toDate = view.activeEnd;
      }
    }
    
    const response = await getAPI(TmsMaintenanceScheduleApi).apiTmsTmsmaintenancescheduleGetlistPost(input);
    state.scheduleList = response.data?.result || [];
    
    // Sync data to grid
    gridOptions.data = state.scheduleList;
  } catch (error: any) {
    console.error('Error loading schedules:', error);
    ElMessage.error(error?.message || $t('lz.message.base.errorFetchingData'));
  } finally {
    state.loading = false;
  }
};

const loadEquipmentTypes = async () => {
  try {
    const response = await getAPI(TmsEquipmentTypeApi).apiTmsTmsequipmenttypeListGet();
    state.equipmentTypeList = response.data?.result || [];
  } catch (error) {
    console.error('Error loading equipment types:', error);
  }
};

const loadScheduleHistory = async (scheduleId: string) => {
  try {
    state.historyLoading = true;
    const input: MaintenanceHistoryQueryInput = {
      maintenanceScheduleId: scheduleId,
    };
    const response = await getAPI(TmsMaintenanceScheduleApi).apiTmsTmsmaintenancescheduleGethistoryPost(input);
    state.historyData = response.data?.result || [];
  } catch (error) {
    console.error('Error loading history:', error);
    state.historyData = [];
  } finally {
    state.historyLoading = false;
  }
};

// Handlers
const handleQuery = () => {
  loadSchedules();
};

const handleSearch = () => {
  // Debounce search
  loadSchedules();
};

const handleQuickFilter = () => {
  loadSchedules();
};

const resetQuery = () => {
  state.queryParams = {
    status: undefined,
    priority: undefined,
    equipmentTypeId: undefined,
  };
  state.searchText = '';
  state.quickFilter = 'related';
  loadSchedules();
};

const handlePrev = () => {
  if (state.currentView === 'dayGridYear') {
    state.currentYear--;
    loadSchedules();
  } else {
    fullCalendar.value?.getApi()?.prev();
  }
};

const handleNext = () => {
  if (state.currentView === 'dayGridYear') {
    state.currentYear++;
    loadSchedules();
  } else {
    fullCalendar.value?.getApi()?.next();
  }
};

const handleToday = () => {
  if (state.currentView === 'dayGridYear') {
    state.currentYear = new Date().getFullYear();
    loadSchedules();
  } else {
    fullCalendar.value?.getApi()?.today();
  }
};

// Year view handlers
const handleYearMonthClick = (month: number) => {
  // Switch to month view and navigate to that month
  const year = state.currentYear;
  state.currentView = 'dayGridMonth';
  nextTick(() => {
    const api = fullCalendar.value?.getApi();
    if (api) {
      api.changeView('dayGridMonth');
      api.gotoDate(new Date(year, month - 1, 1));
    }
  });
};

const handleYearEventClick = (event: any) => {
  // Handle event click from year view
  const schedule = event.extendedProps?.schedule || state.scheduleList.find(s => s.id === event.id);
  if (schedule) {
    handleViewDetail(schedule);
  }
};

const handleYearDayClick = (year: number, month: number, day: number) => {
  // Navigate to that day in month view
  state.currentView = 'dayGridMonth';
  nextTick(() => {
    const api = fullCalendar.value?.getApi();
    if (api) {
      api.changeView('dayGridMonth');
      api.gotoDate(new Date(year, month - 1, day));
    }
  });
};

const changeView = (view: string) => {
  state.currentView = view;
  if (view === 'dayGridYear') {
    // Year view uses custom component, reload data for full year
    loadSchedules();
  } else {
    fullCalendar.value?.getApi()?.changeView(view);
  }
};

const handleViewDetail = async (schedule: MaintenanceScheduleOutput) => {
  state.selectedSchedule = schedule;
  state.detailDialogTitle = schedule.name || $t('lz.label.tmsMaintenanceSchedule.scheduleDetail');
  state.detailActiveTab = 'info';
  state.detailDialogVisible = true;
  
  // Load full detail if needed
  if (schedule.id) {
    try {
      state.detailLoading = true;
      const response = await getAPI(TmsMaintenanceScheduleApi).apiTmsTmsmaintenancescheduleGetbyidIdGet(schedule.id);
      if (response.data?.result) {
        state.selectedSchedule = normalizeScheduleOutput(response.data.result);
      }
    } catch (error) {
      console.error('Error loading schedule detail:', error);
    } finally {
      state.detailLoading = false;
    }
    
    // Load history
    loadScheduleHistory(schedule.id);
  }
};

const firstDefined = <T>(...values: Array<T | undefined | null>) => {
  for (const v of values) {
    if (v !== undefined && v !== null) return v;
  }
  return undefined;
};

const normalizeKey = (s?: string | null) => (s ?? '').toString();

const normalizeScheduleStatusKey = (status?: string | null) => normalizeKey(status).trim().toLowerCase();

const normalizePriorityKey = (priority?: string | null) => normalizeKey(priority);

const normalizeRoleKey = (role?: string | null) => normalizeKey(role);

const normalizeEquipmentStatusKey = (status?: string | null) => normalizeKey(status);

const normalizeTaskStatusKey = (status?: string | null) => normalizeKey(status);

const normalizeAlertStatusKey = (status?: string | null) => normalizeKey(status);

const normalizeAlertTypeKey = (alertType?: string | null) => normalizeKey(alertType);

const isDraftScheduleStatus = (status?: string | null) => {
  const s = normalizeScheduleStatusKey(status);
  if (!s) return false;
  // canonical keys
  return s === 'register';
};

const normalizeScheduleEquipment = (eq: any) => {
  const scheduleEquipmentId = firstDefined<string>(
    eq?.scheduleEquipmentId,
    eq?.ScheduleEquipmentId,
    eq?.id,
    eq?.ID
  );

  const equipmentId = firstDefined<string>(eq?.equipmentId, eq?.EquipmentId) || '';

  const equipmentName = firstDefined<string>(eq?.equipmentName, eq?.EquipmentName);
  const equipmentCode = firstDefined<string>(eq?.equipmentCode, eq?.EquipmentCode, eq?.code, eq?.Code);

  const equipmentTypeId = firstDefined<string>(
    eq?.equipmentTypeIdSnapshot,
    eq?.EquipmentTypeIdSnapshot,
    eq?.equipmentTypeId,
    eq?.EquipmentTypeId
  );

  const equipmentTypeName = firstDefined<string>(
    eq?.equipmentTypeNameSnapshot,
    eq?.EquipmentTypeNameSnapshot,
    eq?.equipmentTypeName,
    eq?.EquipmentTypeName
  );

  const equipmentState = firstDefined<any>(
    eq?.equipmentStateSnapshot,
    eq?.EquipmentStateSnapshot,
    eq?.equipmentState,
    eq?.EquipmentState
  );

  const maintenanceGuideId = firstDefined<string>(eq?.maintenanceGuideId, eq?.MaintenanceGuideId);
  const remark = firstDefined<string>(eq?.remark, eq?.Remark);

  const status = normalizeEquipmentStatusKey(firstDefined<string>(eq?.status, eq?.Status));

  const isChecked = firstDefined<boolean>(eq?.isChecked, eq?.IsChecked) ?? false;
  const checkedNote = firstDefined<string>(eq?.checkedNote, eq?.CheckedNote);
  const checkedAt = firstDefined<any>(eq?.checkedAt, eq?.CheckedAt);
  const checkedBy = firstDefined<string>(eq?.checkedBy, eq?.CheckedBy);

  return {
    ...eq,
    scheduleEquipmentId,
    equipmentId,
    equipmentName,
    equipmentCode,
    equipmentTypeId: equipmentTypeId != null ? String(equipmentTypeId) : undefined,
    equipmentTypeName,
    equipmentState,
    maintenanceGuideId: maintenanceGuideId != null ? String(maintenanceGuideId) : undefined,
    remark,
    status,
    isChecked,
    checkedNote,
    checkedAt,
    checkedBy,
  };
};

const normalizeScheduleTask = (task: any) => {
  const id = firstDefined<string>(task?.id, task?.ID);
  const maintenanceScheduleId = firstDefined<string>(task?.maintenanceScheduleId, task?.MaintenanceScheduleId);
  const scheduleEquipmentId = firstDefined<string>(task?.scheduleEquipmentId, task?.ScheduleEquipmentId);
  const assignedUserId = firstDefined<string>(task?.assignedUserId, task?.AssignedUserId);
  const assignedUserName = firstDefined<string>(task?.assignedUserName, task?.AssignedUserName);
  const role = normalizeRoleKey(firstDefined<string>(task?.role, task?.Role));
  const taskName = firstDefined<string>(task?.taskName, task?.TaskName);
  const description = firstDefined<string>(task?.description, task?.Description);
  const status = normalizeTaskStatusKey(firstDefined<string>(task?.status, task?.Status));
  const remark = firstDefined<string>(task?.remark, task?.Remark);

  return {
    ...task,
    id,
    maintenanceScheduleId,
    scheduleEquipmentId,
    assignedUserId,
    assignedUserName,
    role,
    taskName,
    description,
    status,
    remark,
  };
};

const normalizeScheduleOutput = (schedule: any) => {
  if (!schedule) return schedule;

  const equipments = Array.isArray(schedule.equipments)
    ? schedule.equipments
    : Array.isArray(schedule.Equipments)
      ? schedule.Equipments
      : [];

  const tasks = Array.isArray(schedule.tasks)
    ? schedule.tasks
    : Array.isArray(schedule.Tasks)
      ? schedule.Tasks
      : [];

  const normalizedTasks = tasks.map(normalizeScheduleTask);
  const normalizedEquipments = equipments.map(normalizeScheduleEquipment);

  const taskCount = firstDefined<number>(schedule?.taskCount, schedule?.TaskCount) ?? normalizedTasks.length;
  const completedTaskCount = firstDefined<number>(schedule?.completedTaskCount, schedule?.CompletedTaskCount) ?? normalizedTasks.filter((t: any) => {
    const st = normalizeTaskStatusKey(firstDefined<string>(t?.status, t?.Status));
    return st === 'finish' || st === 'skipped';
  }).length;

  const equipmentCount = firstDefined<number>(schedule?.equipmentCount, schedule?.EquipmentCount) ?? normalizedEquipments.length;
  const completedEquipmentCount = firstDefined<number>(schedule?.completedEquipmentCount, schedule?.CompletedEquipmentCount) ?? normalizedEquipments.filter((e: any) => {
    const st = normalizeEquipmentStatusKey(firstDefined<string>(e?.status, e?.Status));
    return st === 'checked' || st === 'skipped';
  }).length;

  return {
    ...schedule,
    id: firstDefined<string>(schedule?.id, schedule?.ID),
    code: firstDefined<string>(schedule?.code, schedule?.Code),
    name: firstDefined<string>(schedule?.name, schedule?.Name),
    status: normalizeScheduleStatusKey(firstDefined<string>(schedule?.status, schedule?.Status)),
    createUId: firstDefined<string>(schedule?.createUId, schedule?.CreateUId),
    createUserName: firstDefined<string>(schedule?.createUserName, schedule?.CreateUserName),
    scheduledDate: firstDefined<any>(schedule?.scheduledDate, schedule?.ScheduledDate),
    dueDate: firstDefined<any>(schedule?.dueDate, schedule?.DueDate),
    completedDate: firstDefined<any>(schedule?.completedDate, schedule?.CompletedDate),
    remark: firstDefined<string>(schedule?.remark, schedule?.Remark),
    priority: normalizePriorityKey(firstDefined<string>(schedule?.priority, schedule?.Priority)),
    reminderDaysBefore: firstDefined<number>(schedule?.reminderDaysBefore, schedule?.ReminderDaysBefore),

    equipments: normalizedEquipments,
    tasks: normalizedTasks,
    equipmentCount,
    completedEquipmentCount,
    taskCount,
    completedTaskCount,
  };
};

// Handle date click on calendar to create new schedule
const handleDateClick = async (arg: any) => {
  if (state.guideList.length === 0) {
    await loadAllGuides();
  }
  state.operateType = 'add';
  state.editDialogTitle = $t('lz.label.tmsMaintenanceSchedule.addSchedule');
  const clickedDate = arg.date;
  const formattedDate = clickedDate.toISOString().split('T')[0];
  state.formData = {
    id: undefined,
    name: '',
    scheduledDate: formattedDate,
    dueDate: '',
    priority: 'medium',
    reminderDaysBefore: 3,
    status: 'register',
    remark: '',
    tasks: [],
  };
  state.selectedLeaderIds = [];
  state.selectedMemberIds = [];
  state.selectedEquipments = [];
  state.editDialogVisible = true;
};

// Handle custom column for grid
const handleCustomColumn = () => {
  xGrid.value?.openCustom();
};

const handleAddSchedule = async () => {
  if (state.guideList.length === 0) {
    await loadAllGuides();
  }
  state.operateType = 'add';
  state.editDialogTitle = $t('lz.label.tmsMaintenanceSchedule.addSchedule');
  state.formData = {
    id: undefined,
    name: '',
    scheduledDate: '',
    dueDate: '',
    priority: 'medium',
    reminderDaysBefore: 3,
    status: 'register',
    remark: '',
    tasks: [],
  };
  state.selectedLeaderIds = [];
  state.selectedMemberIds = [];
  state.selectedEquipments = [];
  state.editDialogVisible = true;
};

const handleEditSchedule = async (schedule: MaintenanceScheduleOutput | null) => {
  if (!schedule) return;

  if (state.guideList.length === 0) {
    await loadAllGuides();
  }

  let scheduleDetail: any = schedule;
  try {
    state.formLoading = true;
    // If coming from list/calendar item, it may not include equipments/tasks
    if (schedule.id) {
      const response = await getAPI(TmsMaintenanceScheduleApi).apiTmsTmsmaintenancescheduleGetbyidIdGet(schedule.id);
      if (response.data?.result) {
        scheduleDetail = response.data.result;
      }
    }
  } catch (error) {
    console.error('Error loading schedule detail for edit:', error);
  } finally {
    state.formLoading = false;
  }

  // Ensure schedule equipments are normalized (snapshot-first)
  scheduleDetail = normalizeScheduleOutput(scheduleDetail);
  
  state.operateType = 'edit';
  state.editDialogTitle = $t('lz.label.tmsMaintenanceSchedule.editSchedule');
  state.formData = {
    id: scheduleDetail.id || undefined,
    name: scheduleDetail.name || '',
    scheduledDate: scheduleDetail.scheduledDate ? new Date(scheduleDetail.scheduledDate).toISOString().split('T')[0] : '',
    dueDate: scheduleDetail.dueDate ? new Date(scheduleDetail.dueDate).toISOString().split('T')[0] : '',
    priority: scheduleDetail.priority || 'medium',
    reminderDaysBefore: scheduleDetail.reminderDaysBefore || 3,
    status: scheduleDetail.status || 'register',
    remark: scheduleDetail.remark || '',
    tasks: [],
  };
  
  // Populate selected leaders and members from existing tasks
  state.selectedLeaderIds = [];
  state.selectedMemberIds = [];
  
  // Populate selected equipments from existing schedule
  state.selectedEquipments = (scheduleDetail.equipments || []).map((raw: any) => {
    const eq = normalizeScheduleEquipment(raw);
    const resolvedTypeName = eq.equipmentTypeName || (eq.equipmentTypeId ? getEquipmentTypeNameById(eq.equipmentTypeId) : undefined);
    return {
      equipmentId: eq.equipmentId,
      equipmentName: eq.equipmentName,
      equipmentCode: eq.equipmentCode,
      equipmentTypeId: eq.equipmentTypeId,
      equipmentTypeName: resolvedTypeName,
      equipmentState: eq.equipmentState,
      maintenanceGuideId: eq.maintenanceGuideId != null ? String(eq.maintenanceGuideId) : undefined,
      remark: eq.remark,
    };
  });
  
  if (scheduleDetail.tasks && scheduleDetail.tasks.length > 0) {
    scheduleDetail.tasks.forEach((task: any) => {
      if (task.role === 'leader' && task.assignedUserId) {
        state.selectedLeaderIds.push(task.assignedUserId);
      } else if (task.assignedUserId) {
        state.selectedMemberIds.push(task.assignedUserId);
      }
    });
  }
  
  state.editDialogVisible = true;
  state.detailDialogVisible = false;
};

/**
 * Handle copy schedule button click
 * Opens add dialog with copied schedule data
 */
const handleCopySchedule = async () => {
  if (!state.selectedSchedule) return;

  if (state.guideList.length === 0) {
    await loadAllGuides();
  }
  
  // Close detail dialog
  state.detailDialogVisible = false;
  
  // Open add dialog with copied data
  state.operateType = 'add';
  state.editDialogTitle = $t('lz.label.tmsMaintenanceSchedule.copySchedule');
  state.formData = {
    id: undefined,
    name: (state.selectedSchedule.name || '') + ' - ' + $t('lz.button.base.copy'),
    scheduledDate: '',
    dueDate: '',
    priority: state.selectedSchedule.priority || 'medium',
    reminderDaysBefore: state.selectedSchedule.reminderDaysBefore || 3,
    status: 'register',
    remark: state.selectedSchedule.remark || '',
    tasks: [],
  };
  
  // Copy leaders and members
  state.selectedLeaderIds = [];
  state.selectedMemberIds = [];
  if (state.selectedSchedule.tasks && state.selectedSchedule.tasks.length > 0) {
    state.selectedSchedule.tasks.forEach((task: any) => {
      if (task.role === 'leader' && task.assignedUserId) {
        state.selectedLeaderIds.push(task.assignedUserId);
      } else if (task.assignedUserId) {
        state.selectedMemberIds.push(task.assignedUserId);
      }
    });
  }
  
  // Copy equipments (snapshot-first)
  const normalized = normalizeScheduleOutput(state.selectedSchedule);
  state.selectedEquipments = (normalized.equipments || []).map((raw: any) => {
    const eq = normalizeScheduleEquipment(raw);
    const resolvedTypeName = eq.equipmentTypeName || (eq.equipmentTypeId ? getEquipmentTypeNameById(eq.equipmentTypeId) : undefined);
    return {
      equipmentId: eq.equipmentId,
      equipmentName: eq.equipmentName,
      equipmentCode: eq.equipmentCode,
      equipmentTypeId: eq.equipmentTypeId,
      equipmentTypeName: resolvedTypeName,
      equipmentState: eq.equipmentState,
      maintenanceGuideId: eq.maintenanceGuideId != null ? String(eq.maintenanceGuideId) : undefined,
      remark: eq.remark,
    };
  });
  
  state.editDialogVisible = true;
};

/**
 * Handle cancel schedule button click
 * Shows confirmation and updates schedule status to 'Cancelled'
 */
const handleCancelSchedule = async () => {
  if (!state.selectedSchedule?.id) return;
  
  try {
    await ElMessageBox.confirm(
      $t('lz.message.tmsMaintenanceSchedule.confirmCancelSchedule'),
      $t('lz.label.tmsMaintenanceSchedule.cancelSchedule'),
      {
        confirmButtonText: $t('lz.button.base.confirm'),
        cancelButtonText: $t('lz.button.base.close'),
        type: 'warning',
      }
    );
    
    // Update schedule status to 'Cancelled'
    await getAPI(TmsMaintenanceScheduleApi).apiTmsTmsmaintenancescheduleUpdatestatusPost({
      id: state.selectedSchedule.id!,
      status: 'cancel',
    });
    
    ElMessage.success($t('lz.message.tmsMaintenanceSchedule.cancelScheduleSuccess'));
    
    // Update local state
    state.selectedSchedule.status = 'cancel';
    
    // Refresh list
    await loadSchedules();
  } catch (error: any) {
    if (error !== 'cancel') {
      console.error('Error cancelling schedule:', error);
      ElMessage.error(error?.message || $t('lz.message.tmsMaintenanceSchedule.cancelScheduleError'));
    }
  }
};

/**
 * Handle delete schedule button click
 * Shows confirmation and deletes the schedule
 */
const handleDeleteSchedule = async (schedule?: MaintenanceScheduleOutput | null) => {
  const targetSchedule = schedule || state.selectedSchedule;
  if (!targetSchedule?.id) return;
  
  try {
    await ElMessageBox.confirm(
      $t('lz.message.tmsMaintenanceSchedule.confirmDeleteSchedule'),
      $t('lz.label.tmsMaintenanceSchedule.deleteSchedule'),
      {
        confirmButtonText: $t('lz.button.base.delete'),
        cancelButtonText: $t('lz.button.base.close'),
        type: 'error',
      }
    );
    
    await getAPI(TmsMaintenanceScheduleApi).apiTmsTmsmaintenancescheduleDeletePost({
      id: targetSchedule.id!,
    });
    
    ElMessage.success($t('lz.message.tmsMaintenanceSchedule.deleteScheduleSuccess'));
    
    // Close dialog and refresh list
    state.detailDialogVisible = false;
    await loadSchedules();
  } catch (error: any) {
    if (error !== 'cancel') {
      console.error('Error deleting schedule:', error);
      ElMessage.error(error?.message || $t('lz.message.tmsMaintenanceSchedule.deleteScheduleError'));
    }
  }
};

const handleSaveSchedule = async () => {
  try {
    await formRef.value?.validate();
    state.formLoading = true;
    
    // Build tasks from selected leaders and members
    const tasks: any[] = [];
    
    // Add leaders
    state.selectedLeaderIds.forEach(userId => {
      tasks.push({
        assignedUserId: userId,
        role: 'leader',
        taskName: $t('lz.label.tmsMaintenanceSchedule.maintenanceTask'),
      });
    });
    
    // Add members
    state.selectedMemberIds.forEach(userId => {
      tasks.push({
        assignedUserId: userId,
        role: 'member',
        taskName: $t('lz.label.tmsMaintenanceSchedule.maintenanceTask'),
      });
    });
    
    if (state.operateType === 'add') {
      // Build equipments array
      const equipments: ScheduleEquipmentInput[] = state.selectedEquipments.map(eq => ({
        equipmentId: eq.equipmentId,
        maintenanceGuideId: eq.maintenanceGuideId || undefined,
        remark: eq.remark || undefined,
      }));
      
      const input: AddMaintenanceScheduleInput = {
        name: state.formData.name,
        scheduledDate: new Date(state.formData.scheduledDate),
        dueDate: state.formData.dueDate ? new Date(state.formData.dueDate) : undefined,
        priority: state.formData.priority,
        reminderDaysBefore: state.formData.reminderDaysBefore,
        remark: state.formData.remark || undefined,
        tasks: tasks.length > 0 ? tasks : undefined,
        equipments: equipments.length > 0 ? equipments : undefined,
      };
      await getAPI(TmsMaintenanceScheduleApi).apiTmsTmsmaintenancescheduleAddPost(input);
      ElMessage.success($t('lz.message.base.createSuccess'));
    } else {
      // Build equipments array for update
      const equipments: ScheduleEquipmentInput[] = state.selectedEquipments.map(eq => ({
        equipmentId: eq.equipmentId,
        maintenanceGuideId: eq.maintenanceGuideId || undefined,
        remark: eq.remark || undefined,
      }));
      
      const input: UpdateMaintenanceScheduleInput = {
        id: state.formData.id!,
        name: state.formData.name,
        scheduledDate: new Date(state.formData.scheduledDate),
        dueDate: state.formData.dueDate ? new Date(state.formData.dueDate) : undefined,
        priority: state.formData.priority,
        reminderDaysBefore: state.formData.reminderDaysBefore,
        status: state.formData.status,
        remark: state.formData.remark || undefined,
        tasks: tasks.length > 0 ? tasks : undefined,
        equipments: equipments.length > 0 ? equipments : undefined,
      };
      await getAPI(TmsMaintenanceScheduleApi).apiTmsTmsmaintenancescheduleUpdatePost(input);
      ElMessage.success($t('lz.message.base.updateSuccess'));
    }
    
    state.editDialogVisible = false;
    // Reset personnel selection
    state.selectedLeaderIds = [];
    state.selectedMemberIds = [];
    loadSchedules();
  } catch (error: any) {
    if (error !== 'cancel') {
      console.error('Error saving schedule:', error);
      ElMessage.error(error?.message || $t('lz.message.base.saveError'));
    }
  } finally {
    state.formLoading = false;
  }
};

// ===== Personnel Management Functions =====

/**
 * Computed: Check if current user can edit or delete schedule
 * User can edit/delete if they are:
 * - The creator of the schedule
 * - OR a leader in the schedule (assigned as leader in tasks)
 * Note: createUId stores the account/username, not the UUID
 * Status rules (matching dutySchedule):
 * - Cancelled: cannot edit
 * - Other statuses: check user permission
 */
const canEditOrDelete = computed(() => {
  if (!state.selectedSchedule) return false;
  
  // Block editing for cancelled schedules
  if (normalizeScheduleStatusKey(state.selectedSchedule.status) === 'cancel') return false;
  
  const currentUserId = userInfoStore.userInfos?.account || '';
  
  // Check if current user is the creator
  if (state.selectedSchedule.createUId === currentUserId) return true;
  
  // Check if current user is a leader in this schedule
  if (state.selectedSchedule.tasks) {
    const userTask = state.selectedSchedule.tasks.find(
      (t: any) => t.assignedUserId === currentUserId || t.assignedUserId === userInfoStore.userInfos?.id
    );
    if (userTask && normalizeRoleKey(userTask.role) === 'leader') {
      return true;
    }
  }
  
  return false;
});

/**
 * Computed: Check if current user can delete schedule
 * Delete rules:
 * - If status is 'Scheduled' (draft): creator or leader can delete
 * - If status is 'Process': cannot delete, must cancel first
 * - If status is 'Completed' or 'Cancelled': only admin can delete
 */
const canDelete = computed(() => {
  if (!state.selectedSchedule) return false;
  const status = normalizeScheduleStatusKey(state.selectedSchedule.status);
  const currentUserId = userInfoStore.userInfos?.account || '';
  
  // If cancelled: only role 111 or 222 can delete
  if (status === 'cancel') {
    const userRoles = userInfoStore.userInfos?.accountType || [];
    return userRoles === '111' || userRoles === '222';
  }
  
  // If already started/confirmed/finished/overdue: cannot delete, must cancel first
  if (status === 'confirm' || status === 'process' || status === 'overdue' || status === 'finish') {
    return false;
  }
  
  // Only allow delete in initial register status
  if (status !== 'register') return false;
  return state.selectedSchedule.createUId === currentUserId || canEditOrDelete.value;
});

/**
 * Computed: Check if schedule is in limited edit mode
 * When status is Process/Finish, only allow adding/removing personnel, not full edit
 */
const isLimitedEditMode = computed(() => {
  if (!state.selectedSchedule) return false;
  const s = normalizeScheduleStatusKey(state.selectedSchedule.status);
  return s === 'process' || s === 'overdue' || s === 'finish';
});

/**
 * Computed: Check if current user can manage personnel
 */
const canManagePersonnel = computed(() => {
  if (!state.selectedSchedule) return false;
  const currentUserId = userInfoStore.userInfos?.account || '';
  // Allow if user is creator or is a leader or schedule is in editable state
  return state.selectedSchedule.createUId === currentUserId || 
         canEditOrDelete.value ||
         normalizeScheduleStatusKey(state.selectedSchedule.status) === 'register' ||
         normalizeScheduleStatusKey(state.selectedSchedule.status) === 'confirm' ||
         normalizeScheduleStatusKey(state.selectedSchedule.status) === 'process' ||
         normalizeScheduleStatusKey(state.selectedSchedule.status) === 'overdue';
});

/**
 * Computed: Only allow updating execution statuses when schedule is Process
 * (equipment check + personnel task status)
 */
const canUpdateExecutionStatus = computed(() => {
  const s = normalizeScheduleStatusKey(state.selectedSchedule?.status);
  return s === 'process' || s === 'overdue';
});

/**
 * Computed: Get available users not already assigned to schedule
 */
const availableUsersForSchedule = computed(() => {
  if (!state.selectedSchedule?.tasks) return state.employeeList;
  
  const existingUserIds = state.selectedSchedule.tasks.map((t: any) => t.assignedUserId);
  return state.employeeList.filter((u: any) => !existingUserIds.includes(u.id));
});

/**
 * Get user real name from employee list
 */
const getUserRealName = (userId: string) => {
  const user = state.employeeList.find((u: any) => u.username === userId || u.id === userId);
  return user?.name || user?.username || userId;
};

/**
 * Load employee list from API
 */
const loadEmployeeList = async () => {
  try {
    const response = await getAPI(SysUserApi).apiSystemSysuserListPost({});
    if (response.data?.result) {
      state.employeeList = response.data.result;
    }
  } catch (error) {
    console.error('Error loading employee list:', error);
  }
};

/**
 * Load patrol team list from API
 */
const loadTeamList = async () => {
  try {
    const response = await getAPI(TmsPatrolTeamApi).apiTmsTmspatrolteamListGet();
    if (response.data?.result) {
      state.teamList = response.data.result;
    }
  } catch (error) {
    console.error('Error loading team list:', error);
  }
};

/**
 * Open team quick select modal
 */
const handleOpenTeamQuickSelect = () => {
  teamQuickSelectRef.value?.openDialog();
};

/**
 * Open patrol team management modal
 */
const handleOpenTeamManage = () => {
  patrolTeamManageRef.value?.openDialog();
};

/**
 * Handle team selection from quick select
 */
const handleTeamSelect = (team: any) => {
  if (!team) return;
  
  // Get team members and assign to leaders/members
  const members = team.members || [];
  
  state.selectedLeaderIds = [];
  state.selectedMemberIds = [];
  
  members.forEach((member: any) => {
    if (member.role === 'leader') {
      state.selectedLeaderIds.push(member.userId);
    } else {
      state.selectedMemberIds.push(member.userId);
    }
  });
  
  ElMessage.success($t('lz.message.tmsMaintenanceSchedule.teamSelectedSuccess', { name: team.name }));
};

/**
 * Handle team refresh after management
 */
const handleTeamRefresh = () => {
  loadTeamList();
};

/**
 * Handle leader selection change
 */
const handleLeaderChange = (value: string[]) => {
  // Remove leaders from members if they exist
  state.selectedMemberIds = state.selectedMemberIds.filter(id => !value.includes(id));
};

/**
 * Handle member selection change
 */
const handleMemberChange = (value: string[]) => {
  // Remove members from leaders if they exist
  state.selectedLeaderIds = state.selectedLeaderIds.filter(id => !value.includes(id));
};

/**
 * Open add personnel dialog
 */
const handleOpenAddPersonnelDialog = () => {
  state.newPersonnelForm = {
    userId: '',
    role: 'member',
    taskName: '',
    description: '',
    remark: ''
  };
  state.addPersonnelDialogVisible = true;
};

/**
 * Close add personnel dialog
 */
const handleCloseAddPersonnelDialog = () => {
  state.addPersonnelDialogVisible = false;
};

/**
 * Add personnel to schedule
 */
const handleAddPersonnelToSchedule = async () => {
  if (!state.selectedSchedule?.id) {
    ElMessage.warning($t('lz.message.tmsMaintenanceSchedule.scheduleNotFound'));
    return;
  }
  
  if (!state.newPersonnelForm.userId) {
    ElMessage.warning($t('lz.message.tmsMaintenanceSchedule.selectEmployee'));
    return;
  }
  
  try {
    state.personnelActionLoading = true;
    
    await getAPI(TmsMaintenanceScheduleApi).apiTmsTmsmaintenancescheduleAddtaskPost({
      maintenanceScheduleId: state.selectedSchedule.id,
      assignedUserId: state.newPersonnelForm.userId,
      role: state.newPersonnelForm.role || undefined,
      taskName: state.newPersonnelForm.taskName || undefined,
      description: state.newPersonnelForm.description || undefined,
      remark: state.newPersonnelForm.remark || undefined
    });
    
    ElMessage.success($t('lz.message.tmsMaintenanceSchedule.addPersonnelSuccess'));
    state.addPersonnelDialogVisible = false;
    
    // Refresh schedule detail
    await refreshSelectedSchedule();
    
  } catch (error: any) {
    console.error('Error adding personnel:', error);
    ElMessage.error({
      message: error.response?.data?.message || $t('lz.message.tmsMaintenanceSchedule.addPersonnelError'),
      duration: 3000
    });
  } finally {
    state.personnelActionLoading = false;
  }
};

/**
 * Open change role dialog
 */
const handleOpenChangeRoleDialog = (row: MaintenanceTaskOutput) => {
  const currentRole = (row.role || '').toLowerCase();
  state.changeRoleForm = {
    taskId: row.id || '',
    userId: row.assignedUserId || '',
    currentRole: currentRole,
    newRole: currentRole === 'leader' ? 'member' : 'leader',
    reason: '',
    scheduleEquipmentId: (row as any).scheduleEquipmentId || (row as any).ScheduleEquipmentId || '',
    taskName: (row as any).taskName || (row as any).TaskName || '',
    description: (row as any).description || (row as any).Description || '',
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
 * Change personnel role - Note: This may need a dedicated API endpoint
 * For now, we'll remove and re-add with new role
 */
const handleChangePersonnelRole = async () => {
  if (!state.changeRoleForm.taskId) {
    ElMessage.warning($t('lz.message.tmsMaintenanceSchedule.taskNotFound'));
    return;
  }
  
  if (state.changeRoleForm.newRole === state.changeRoleForm.currentRole) {
    ElMessage.warning($t('lz.message.tmsMaintenanceSchedule.roleMustBeDifferent'));
    return;
  }
  
  try {
    state.personnelActionLoading = true;
    
    // Remove existing task
    await getAPI(TmsMaintenanceScheduleApi).apiTmsTmsmaintenancescheduleRemovetaskPost({
      taskId: state.changeRoleForm.taskId
    });
    
    // Re-add with new role
    await getAPI(TmsMaintenanceScheduleApi).apiTmsTmsmaintenancescheduleAddtaskPost({
      maintenanceScheduleId: state.selectedSchedule?.id || '',
      assignedUserId: state.changeRoleForm.userId,
      role: state.changeRoleForm.newRole,
      scheduleEquipmentId: state.changeRoleForm.scheduleEquipmentId || undefined,
      taskName: state.changeRoleForm.taskName || undefined,
      description: state.changeRoleForm.description || undefined,
      remark: state.changeRoleForm.reason || undefined
    });
    
    ElMessage.success($t('lz.message.tmsMaintenanceSchedule.updateRoleSuccess'));
    state.changeRoleDialogVisible = false;
    
    // Refresh schedule detail
    await refreshSelectedSchedule();
    
  } catch (error: any) {
    console.error('Error changing role:', error);
    ElMessage.error({
      message: error.response?.data?.message || $t('lz.message.tmsMaintenanceSchedule.updateRoleError'),
      duration: 3000
    });
  } finally {
    state.personnelActionLoading = false;
  }
};

const getTaskStatusKey = (row: any): 'pending' | 'finish' | 'skipped' => {
  const raw = (firstDefined<string>(row?.status, row?.Status) || '').toString().trim();
  const key = normalizeTaskStatusKey(raw);

  if (key === 'finish') return 'finish';
  if (key === 'skipped') return 'skipped';
  return 'pending';
};

/**
 * Open task status dialog (Tab 3)
 */
const openTaskStatusDialog = (row: any) => {
  if (!canUpdateExecutionStatus.value) {
    ElMessage.warning($t('lz.message.tmsMaintenanceSchedule.updateOnlyInProgress'));
    return;
  }
  const taskId = firstDefined<string>(row?.id, row?.ID);
  if (!taskId) return;

  state.taskStatusForm.taskId = String(taskId);
  state.taskStatusForm.assignedUserId = row?.assignedUserId || row?.AssignedUserId || '';
  state.taskStatusForm.assignedUserName = row?.assignedUserName || row?.AssignedUserName || getUserRealName(state.taskStatusForm.assignedUserId);
  state.taskStatusForm.status = getTaskStatusKey(row);

  state.taskStatusDialogVisible = true;
};

/**
 * Submit task status update (Tab 3)
 */
const submitTaskStatus = async () => {
  if (!state.taskStatusForm.taskId) return;
  if (!canUpdateExecutionStatus.value) {
    ElMessage.warning($t('lz.message.tmsMaintenanceSchedule.updateOnlyInProgress'));
    return;
  }

  try {
    state.taskStatusSaving = true;

    await getAPI(TmsMaintenanceScheduleApi).apiTmsTmsmaintenancescheduleUpdatetaskstatusPost({
      taskId: state.taskStatusForm.taskId,
      status: state.taskStatusForm.status,
    } as any);

    ElMessage.success($t('lz.message.base.saveSuccess'));
    state.taskStatusDialogVisible = false;
    await refreshSelectedSchedule();
  } catch (error: any) {
    console.error('Error updating task status:', error);
    ElMessage.error(error?.response?.data?.message || $t('lz.message.base.error'));
  } finally {
    state.taskStatusSaving = false;
  }
};

/**
 * Remove personnel from schedule
 */
const handleRemovePersonnel = async (row: MaintenanceTaskOutput) => {
  const userName = getUserRealName(row.assignedUserId || '');
  
  try {
    await ElMessageBox.confirm(
      $t('lz.message.tmsMaintenanceSchedule.confirmRemovePersonnel', { name: userName }),
      $t('lz.message.tmsMaintenanceSchedule.removePersonnelTitle'),
      {
        confirmButtonText: $t('lz.button.base.delete'),
        cancelButtonText: $t('lz.button.base.cancel'),
        type: 'warning',
        confirmButtonClass: 'el-button--danger'
      }
    );
    
    state.personnelActionLoading = true;
    
    await getAPI(TmsMaintenanceScheduleApi).apiTmsTmsmaintenancescheduleRemovetaskPost({
      taskId: row.id || ''
    });
    
    ElMessage.success($t('lz.message.tmsMaintenanceSchedule.removePersonnelSuccess', { name: userName }));
    
    // Refresh schedule detail
    await refreshSelectedSchedule();
    
  } catch (error: any) {
    if (error !== 'cancel' && error?.message !== 'cancel') {
      console.error('Error removing personnel:', error);
      ElMessage.error({
        message: error.response?.data?.message || $t('lz.message.tmsMaintenanceSchedule.removePersonnelError'),
        duration: 3000
      });
    }
  } finally {
    state.personnelActionLoading = false;
  }
};

/**
 * Refresh selected schedule detail
 */
const refreshSelectedSchedule = async () => {
  if (!state.selectedSchedule?.id) return;
  
  try {
    const response = await getAPI(TmsMaintenanceScheduleApi).apiTmsTmsmaintenancescheduleGetbyidIdGet(state.selectedSchedule.id);
    if (response.data?.result) {
      state.selectedSchedule = normalizeScheduleOutput(response.data.result);
    }
    // Also refresh the main list
    await loadSchedules();
  } catch (error) {
    console.error('Error refreshing schedule:', error);
  }
};

const getEquipmentCheckStatusKey = (row: any): 'pending' | 'checked' | 'skipped' => {
  const status = normalizeEquipmentStatusKey(firstDefined<string>(row?.status, row?.Status));
  if (status === 'skipped') return 'skipped';
  if (status === 'checked') return 'checked';
  if (!!row?.isChecked) return 'checked';
  return 'pending';
};

/**
 * Open equipment check modal (Tab 2)
 */
const openEquipmentCheckDialog = (row: any) => {
  if (!canUpdateExecutionStatus.value) {
    ElMessage.warning($t('lz.message.tmsMaintenanceSchedule.updateOnlyInProgress'));
    return;
  }
  const scheduleEquipmentId = firstDefined<string>(row?.scheduleEquipmentId, row?.ScheduleEquipmentId, row?.id, row?.ID);
  if (!scheduleEquipmentId) return;

  state.equipmentCheckForm.scheduleEquipmentId = scheduleEquipmentId;
  state.equipmentCheckForm.equipmentName = row?.equipmentName || '';
  state.equipmentCheckForm.status = getEquipmentCheckStatusKey(row);
  state.equipmentCheckForm.checkedNote = row?.checkedNote != null ? String(row.checkedNote) : '';
  state.equipmentCheckDialogVisible = true;
};

/**
 * Submit equipment check modal
 */
const submitEquipmentCheck = async () => {
  if (!state.equipmentCheckForm.scheduleEquipmentId) return;
  if (!canUpdateExecutionStatus.value) {
    ElMessage.warning($t('lz.message.tmsMaintenanceSchedule.updateOnlyInProgress'));
    return;
  }
  try {
    state.equipmentCheckSaving = true;

    const payload: any = {
      scheduleEquipmentId: state.equipmentCheckForm.scheduleEquipmentId,
      // backward-compatible fields
      isChecked: state.equipmentCheckForm.status === 'checked',
      checkedNote: state.equipmentCheckForm.checkedNote || undefined,
      // new 3-state status (lower-case key/value)
      status: state.equipmentCheckForm.status,
    };

    await getAPI(TmsMaintenanceScheduleApi).apiTmsTmsmaintenancescheduleConfirmequipmentcheckedPost(payload);
    ElMessage.success($t('lz.message.base.saveSuccess'));
    state.equipmentCheckDialogVisible = false;
    await refreshSelectedSchedule();
  } catch (error: any) {
    console.error('Error confirming equipment checked:', error);
    ElMessage.error(error?.response?.data?.message || $t('lz.message.base.error'));
  } finally {
    state.equipmentCheckSaving = false;
  }
};

// ===== End Personnel Management Functions =====

// Lifecycle
onMounted(() => {
  loadEquipmentTypes();
  loadSchedules();
  loadEmployeeList();
  loadTeamList();
});
</script>

<style scoped lang="scss">
.maintenance-schedule-tab {
  .calendar-controls-wrapper {
    display: flex;
    justify-content: space-between;
    align-items: center;
    flex-wrap: wrap;
    gap: 10px;
    
    .controls-left {
      display: flex;
      align-items: center;
    }
    
    .controls-center {
      display: flex;
      align-items: center;
      justify-content: center;
      flex: 1;
      
      .current-period {
        .period-text {
          font-weight: 600;
          font-size: 18px;
          color: var(--el-text-color-primary);
        }
      }
    }
    
    .controls-right {
      display: flex;
      align-items: center;
      gap: 10px;
    }
  }
  
  .period-text {
    font-weight: 600;
    font-size: 16px;
    color: var(--el-text-color-primary);
    min-width: 150px;
    text-align: center;
  }
  
  .detail-header {
    display: flex;
    gap: 20px;
    margin-bottom: 16px;
    padding: 12px;
    background: var(--el-fill-color-light);
    border-radius: 8px;
    
    .status-info,
    .priority-info {
      display: flex;
      align-items: center;
      gap: 8px;
      
      .label {
        font-weight: 500;
        color: var(--el-text-color-secondary);
      }
    }
  }
  
  .year-view-placeholder {
    min-height: 400px;
    display: flex;
    align-items: center;
    justify-content: center;
  }
}

/* Detail Dialog Styles */
.detail-dialog {
  .dialog-header-content {
    display: flex;
    justify-content: space-between;
    align-items: center;
    width: 100%;
    
    .header-left {
      display: flex;
      align-items: center;
      gap: 12px;
      
      .header-icon {
        color: #fff;
      }
      
      .header-info {
        display: flex;
        flex-direction: column;
        gap: 2px;
        
        .dialog-title {
          font-size: 16px;
          font-weight: 600;
          color: #fff;
        }
        
        .schedule-code {
          font-size: 12px;
          color: rgba(255, 255, 255, 0.8);
        }
      }
    }
    
    .header-right {
      margin-right: 30px;
    }
  }
}

/* Detail Actions Top (same as dutySchedule) */
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

/* Status Progress Inline (same as dutySchedule) */
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
  padding-top: 3px;
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
  
  &.step-active {
    .step-label {
      font-weight: 600;
    }
    
    // Scheduled (primary - blue)
    &:nth-child(1) {
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
    
    // Process (warning - orange)
    &:nth-child(3) {
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
    &:nth-child(5) {
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
  
  &.step-pending {
    .step-circle {
      background: #F5F7FA;
      border-color: #DCDFE6;
      color: #C0C4CC;
    }
    
    .step-label {
      color: var(--el-text-color-placeholder);
    }
  }
  
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

@keyframes pulse-step {
  0%, 100% {
    transform: scale(1);
    box-shadow: 0 0 0 0 rgba(230, 162, 60, 0.4);
  }
  50% {
    transform: scale(1.05);
    box-shadow: 0 0 0 6px rgba(230, 162, 60, 0);
  }
}

/* Summary Cards */
.summary-cards {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 16px;
  margin-bottom: 20px;
  
  @media (max-width: 900px) {
    grid-template-columns: repeat(2, 1fr);
  }
  
  @media (max-width: 500px) {
    grid-template-columns: 1fr;
  }
}

.summary-card {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 16px;
  background: var(--el-bg-color);
  border: 1px solid var(--el-border-color-light);
  border-radius: 8px;
  transition: all 0.3s ease;
  
  &:hover {
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.08);
    transform: translateY(-2px);
  }
  
  .card-icon {
    width: 48px;
    height: 48px;
    border-radius: 10px;
    display: flex;
    align-items: center;
    justify-content: center;
    background: var(--el-color-primary-light-9);
    color: var(--el-color-primary);
    
    &.equipment-icon {
      background: rgba(103, 194, 58, 0.1);
      color: #67C23A;
    }
    
    &.personnel-icon {
      background: rgba(64, 158, 255, 0.1);
      color: #409EFF;
    }
    
    &.date-icon {
      background: rgba(230, 162, 60, 0.1);
      color: #E6A23C;
    }
  }
  
  .card-content {
    display: flex;
    flex-direction: column;
    gap: 4px;
    
    .card-label {
      font-size: 12px;
      color: var(--el-text-color-secondary);
    }
    
    .card-value {
      font-size: 18px;
      font-weight: 600;
      color: var(--el-text-color-primary);
    }
  }
  
  &.priority-card {
    &.priority-critical .card-icon {
      background: rgba(245, 108, 108, 0.1);
      color: #F56C6C;
    }
    
    &.priority-high .card-icon {
      background: rgba(230, 162, 60, 0.1);
      color: #E6A23C;
    }
    
    &.priority-medium .card-icon {
      background: rgba(64, 158, 255, 0.1);
      color: #409EFF;
    }
    
    &.priority-low .card-icon {
      background: rgba(144, 147, 153, 0.1);
      color: #909399;
    }
  }
}

/* Detail Tabs Styles */
.detail-tabs {
  .tab-label {
    display: flex;
    align-items: center;
    gap: 6px;
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

/* Guide Preview Dialog Styles */
.guide-preview-content {
  max-height: 70vh;
  overflow-y: auto;
}

.guide-header-card {
  background: linear-gradient(135deg, var(--el-color-primary-light-9) 0%, #fff 100%);
  border-radius: 12px;
  padding: 20px;
  margin-bottom: 20px;
  border: 1px solid var(--el-border-color-lighter);
}

.guide-title-section {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 16px;
  flex-wrap: wrap;
  gap: 12px;
}

.guide-name {
  margin: 0;
  font-size: 18px;
  font-weight: 600;
  color: var(--el-text-color-primary);
  flex: 1;
}

.guide-meta {
  display: flex;
  gap: 8px;
  flex-shrink: 0;
}

.guide-stats {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
  gap: 12px;
}

.stat-item {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 10px;
  background: #fff;
  border-radius: 8px;
  border: 1px solid var(--el-border-color-lighter);
}

.stat-icon {
  color: var(--el-color-primary);
  flex-shrink: 0;
}

.stat-info {
  display: flex;
  flex-direction: column;
}

.stat-label {
  font-size: 11px;
  color: var(--el-text-color-secondary);
  line-height: 1.2;
}

.stat-value {
  font-size: 13px;
  font-weight: 500;
  color: var(--el-text-color-primary);
  line-height: 1.4;
}

.guide-section {
  margin-bottom: 20px;
  
  &:last-child {
    margin-bottom: 0;
  }
}

.section-header {
  display: flex;
  align-items: center;
  gap: 8px;
  padding-bottom: 10px;
  margin-bottom: 12px;
  border-bottom: 1px solid var(--el-border-color-lighter);
  font-size: 14px;
  font-weight: 600;
  color: var(--el-text-color-primary);
  
  .checklist-count {
    margin-left: auto;
  }
}

.section-content {
  &.description-content {
    padding: 14px;
    background: var(--el-fill-color-light);
    border-radius: 8px;
    line-height: 1.6;
    color: var(--el-text-color-regular);
    font-size: 13px;
  }
}

.checklist-items {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.checklist-item {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 10px 14px;
  background: var(--el-fill-color-lighter);
  border-radius: 8px;
  transition: all 0.2s ease;
  
  &:hover {
    background: var(--el-fill-color-light);
  }
  
  &.required {
    border-left: 3px solid var(--el-color-danger);
  }
  
  .item-index {
    width: 24px;
    height: 24px;
    display: flex;
    align-items: center;
    justify-content: center;
    background: var(--el-color-primary);
    color: #fff;
    border-radius: 50%;
    font-size: 12px;
    font-weight: 600;
    flex-shrink: 0;
  }
  
  .item-task {
    flex: 1;
    font-size: 13px;
    color: var(--el-text-color-primary);
  }
  
  .required-tag {
    flex-shrink: 0;
  }
}

.document-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(180px, 1fr));
  gap: 12px;
}

.document-card {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 16px;
  background: var(--el-fill-color-lighter);
  border-radius: 10px;
  border: 1px solid var(--el-border-color-lighter);
  cursor: pointer;
  transition: all 0.2s ease;
  position: relative;
  
  &:hover {
    background: var(--el-color-primary-light-9);
    border-color: var(--el-color-primary-light-5);
    transform: translateY(-2px);
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.08);
    
    .open-icon {
      opacity: 1;
    }
  }
  
  .doc-icon {
    color: var(--el-color-primary);
    margin-bottom: 8px;
  }
  
  .doc-name {
    font-size: 12px;
    color: var(--el-text-color-primary);
    text-align: center;
    word-break: break-word;
    max-width: 100%;
    line-height: 1.4;
  }
  
  .open-icon {
    position: absolute;
    top: 8px;
    right: 8px;
    color: var(--el-color-primary);
    opacity: 0;
    transition: opacity 0.2s ease;
  }
}

/* PrimeVue Fieldset styling - same as dutySchedule */
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

/* Ensure rows don't block dropdown */
:deep(.el-row) {
  position: relative;
  z-index: auto;
}

:deep(.el-col) {
  position: relative;
  z-index: auto;
}

/* Standard spacing for Fieldset */
.mb10 {
  margin-bottom: 10px;
}

.mb15 {
  margin-bottom: 15px;
}
</style>
