<template>
  <div class="maintenance-alert-tab">
    <!-- Status Tabs -->
    <el-card shadow="hover" :body-style="{ padding: '10px' }">
      <div style="display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 10px;">
        <el-radio-group v-model="state.filter.status" @change="handleQuery" size="small">
          <el-radio-button label="">
            {{ $t('lz.label.base.all') }}
            <el-badge :value="state.statusCounts.total" :max="99" class="status-badge" />
          </el-radio-button>
          <el-radio-button label="Pending">
            {{ $t('lz.label.tmsMaintenanceSchedule.alertStatusPending') }}
            <el-badge :value="state.statusCounts.pending" :max="99" type="danger" class="status-badge" />
          </el-radio-button>
          <el-radio-button label="Acknowledged">
            {{ $t('lz.label.tmsMaintenanceSchedule.alertStatusAcknowledged') }}
            <el-badge :value="state.statusCounts.acknowledged" :max="99" type="warning" class="status-badge" />
          </el-radio-button>
          <el-radio-button label="Scheduled">
            {{ $t('lz.label.tmsMaintenanceSchedule.alertStatusScheduled') }}
            <el-badge :value="state.statusCounts.scheduled" :max="99" type="primary" class="status-badge" />
          </el-radio-button>
          <el-radio-button label="Dismissed">
            {{ $t('lz.label.tmsMaintenanceSchedule.alertStatusDismissed') }}
            <el-badge :value="state.statusCounts.dismissed" :max="99" type="info" class="status-badge" />
          </el-radio-button>
        </el-radio-group>

        <div style="display: flex; gap: 10px;">
          <!-- Alert Type Filter -->
          <el-select
            v-model="state.filter.alertType"
            :placeholder="$t('lz.label.tmsMaintenanceSchedule.selectAlertType')"
            clearable
            style="width: 150px;"
            @change="handleQuery"
          >
            <el-option
              v-for="item in alertTypeOptions"
              :key="item.value"
              :label="item.label"
              :value="item.value"
            />
          </el-select>

          <!-- Priority Filter -->
          <el-select
            v-model="state.filter.priority"
            :placeholder="$t('lz.label.tmsMaintenanceSchedule.selectPriority')"
            clearable
            style="width: 120px;"
            @change="handleQuery"
          >
            <el-option
              v-for="item in priorityOptions"
              :key="item.value"
              :label="item.label"
              :value="item.value"
            />
          </el-select>

          <!-- Config Rules Button -->
          <el-button type="info" @click="handleOpenRuleConfig">
            <el-icon><ele-Setting /></el-icon>
            {{ $t('lz.label.tmsMaintenanceSchedule.alertRuleConfig') }}
          </el-button>
        </div>
      </div>
    </el-card>

    <!-- Data Table -->
    <el-card shadow="hover" style="margin-top: 10px;" :body-style="{ padding: '10px' }">
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
            @refresh="handleQuery"
            @custom-column="handleCustomColumn"
            :loading="state.loading"
          />
        </template>

        <template #empty>
          <el-empty :image-size="200" :description="$t('lz.label.base.noData')" />
        </template>

        <template #equipment_default="{ row }">
          <div>
            <strong>{{ row.equipmentName }}</strong>
            <div style="font-size: 12px; color: var(--el-text-color-secondary);">{{ row.equipmentCode }}</div>
          </div>
        </template>

        <template #alertType_default="{ row }">
          <el-tag :type="getAlertTypeTagType(row.alertType)" size="small">
            {{ getAlertTypeLabel(row.alertType) }}
          </el-tag>
        </template>

        <template #priority_default="{ row }">
          <el-tag :type="getPriorityType(row.priority)" size="small">
            {{ getPriorityLabel(row.priority) }}
          </el-tag>
        </template>

        <template #status_default="{ row }">
          <el-tag :type="getStatusTagType(row.status)" size="small">
            {{ getStatusLabel(row.status) }}
          </el-tag>
        </template>

        <template #errorCount_default="{ row }">
          <el-tag v-if="row.errorCount" type="danger" size="small">{{ row.errorCount }}</el-tag>
          <span v-else>-</span>
        </template>

        <template #createdTime_default="{ row }">
          {{ formatDateTime(row.createdTime) }}
        </template>

        <template #row_buttons="{ row }">
          <template v-if="row.status === 'Pending'">
            <el-button type="warning" link size="small" @click="handleAcknowledge(row)">
              <el-icon><ele-Check /></el-icon>
              {{ $t('lz.label.tmsMaintenanceSchedule.acknowledge') }}
            </el-button>
            <el-button type="info" link size="small" @click="handleDismiss(row)">
              <el-icon><ele-Close /></el-icon>
              {{ $t('lz.label.tmsMaintenanceSchedule.dismiss') }}
            </el-button>
          </template>
          <template v-if="row.status === 'Pending' || row.status === 'Acknowledged'">
            <el-button type="primary" link size="small" @click="handleCreateSchedule(row)">
              <el-icon><ele-Calendar /></el-icon>
              {{ $t('lz.label.tmsMaintenanceSchedule.createSchedule') }}
            </el-button>
          </template>
          <template v-if="row.status === 'Scheduled' || row.status === 'Dismissed'">
            <span style="color: var(--el-text-color-placeholder); font-size: 12px;">—</span>
          </template>
        </template>
      </vxe-grid>

      <!-- Pagination -->
      <div style="display: flex; justify-content: flex-end; margin-top: 10px;">
        <el-pagination
          v-model:current-page="state.pagination.page"
          v-model:page-size="state.pagination.pageSize"
          :page-sizes="[10, 20, 50, 100]"
          :total="state.pagination.total"
          layout="total, sizes, prev, pager, next, jumper"
          @size-change="handleQuery"
          @current-change="handleQuery"
        />
      </div>
    </el-card>

    <!-- Create Schedule From Alert Dialog -->
    <CreateScheduleFromAlert
      ref="createScheduleDialogRef"
      :employee-list="state.employeeList"
      @success="handleCreateScheduleSuccess"
    />

    <!-- Alert Rule Config Dialog -->
    <el-dialog
      v-model="state.ruleConfigVisible"
      :title="$t('lz.label.tmsMaintenanceSchedule.alertRuleConfig')"
      width="900px"
      :close-on-click-modal="false"
    >
      <template #header>
        <div style="color: #fff; display: flex; align-items: center;">
          <el-icon size="16" style="margin-right: 5px;">
            <ele-Setting />
          </el-icon>
          <span>{{ $t('lz.label.tmsMaintenanceSchedule.alertRuleConfig') }}</span>
        </div>
      </template>

      <!-- Rule List -->
      <div style="margin-bottom: 15px; display: flex; justify-content: flex-end;">
        <el-button type="primary" @click="handleAddRule">
          <el-icon><ele-Plus /></el-icon>
          {{ $t('lz.button.base.add') }}
        </el-button>
      </div>

      <vxe-grid
        ref="xGridRules"
        class="xGrid-style"
        v-bind="ruleGridOptions"
      >
        <template #empty>
          <el-empty :image-size="100" :description="$t('lz.label.base.noData')" />
        </template>

        <template #equipmentTypeName_default="{ row }">
          {{ row.equipmentTypeName || $t('lz.label.tmsMaintenanceSchedule.allTypes') }}
        </template>

        <template #ruleCondition_default="{ row }">
          ≥ {{ row.errorThreshold }} {{ $t('lz.label.tmsMaintenanceSchedule.errorsIn') }} {{ row.periodDays }} {{ $t('lz.label.tmsMaintenanceSchedule.days') }}
        </template>

        <template #alertPriority_default="{ row }">
          <el-tag :type="getPriorityType(row.alertPriority)" size="small">
            {{ getPriorityLabel(row.alertPriority) }}
          </el-tag>
        </template>

        <template #isActive_default="{ row }">
          <el-switch
            v-model="row.isActive"
            :loading="row._loading"
            @change="handleToggleRuleActive(row)"
          />
        </template>

        <template #rule_buttons="{ row }">
          <el-button type="primary" link size="small" @click="handleEditRule(row)">
            <el-icon><ele-Edit /></el-icon>
          </el-button>
          <el-button type="danger" link size="small" @click="handleDeleteRule(row)">
            <el-icon><ele-Delete /></el-icon>
          </el-button>
        </template>
      </vxe-grid>

      <template #footer>
        <el-button @click="state.ruleConfigVisible = false">
          {{ $t('lz.button.base.close') }}
        </el-button>
      </template>
    </el-dialog>

    <!-- Add/Edit Rule Dialog -->
    <el-dialog
      v-model="state.ruleDialogVisible"
      :title="state.ruleDialogTitle"
      width="550px"
      :close-on-click-modal="false"
      @close="handleCloseRuleDialog"
    >
      <template #header>
        <div style="color: #fff; display: flex; align-items: center;">
          <el-icon size="16" style="margin-right: 5px;">
            <ele-Document />
          </el-icon>
          <span>{{ state.ruleDialogTitle }}</span>
        </div>
      </template>

      <el-form
        ref="ruleFormRef"
        :model="state.ruleForm"
        :rules="ruleFormRules"
        label-width="140px"
        label-position="left"
        :disabled="state.ruleSubmitting"
      >
        <el-form-item :label="$t('lz.label.tmsMaintenanceSchedule.ruleName')" prop="name">
          <el-input v-model="state.ruleForm.name" :placeholder="$t('lz.label.tmsMaintenanceSchedule.ruleNamePlaceholder')" />
        </el-form-item>

        <el-form-item :label="$t('lz.entity.tms.equipmentType')" prop="equipmentTypeId">
          <el-select
            v-model="state.ruleForm.equipmentTypeId"
            :placeholder="$t('lz.label.tmsMaintenanceSchedule.allTypes')"
            clearable
            filterable
            style="width: 100%;"
          >
            <el-option
              v-for="item in state.equipmentTypeList"
              :key="item.id"
              :label="item.name"
              :value="item.id"
            />
          </el-select>
        </el-form-item>

        <el-form-item :label="$t('lz.label.tmsMaintenanceSchedule.manufacturer')" prop="manufacturer">
          <el-input v-model="state.ruleForm.manufacturer" :placeholder="$t('lz.label.tmsMaintenanceSchedule.allManufacturers')" />
        </el-form-item>

        <el-row :gutter="20" class="mb20">
          <el-col :span="12">
            <el-form-item :label="$t('lz.label.tmsMaintenanceSchedule.errorThreshold')" prop="errorThreshold">
              <el-input-number v-model="state.ruleForm.errorThreshold" :min="1" :max="100" style="width: 100%;" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item :label="$t('lz.label.tmsMaintenanceSchedule.periodDays')" prop="periodDays">
              <el-input-number v-model="state.ruleForm.periodDays" :min="1" :max="365" style="width: 100%;" />
            </el-form-item>
          </el-col>
        </el-row>

        <el-form-item :label="$t('lz.label.tmsMaintenanceSchedule.alertPriority')" prop="alertPriority">
          <el-select v-model="state.ruleForm.alertPriority" style="width: 100%;">
            <el-option
              v-for="item in priorityOptions"
              :key="item.value"
              :label="item.label"
              :value="item.value"
            />
          </el-select>
        </el-form-item>

        <el-form-item :label="$t('lz.label.tmsMaintenanceSchedule.suggestedGuide')" prop="suggestedGuideId">
          <el-select
            v-model="state.ruleForm.suggestedGuideId"
            :placeholder="$t('lz.label.tmsMaintenanceSchedule.selectGuide')"
            clearable
            filterable
            style="width: 100%;"
          >
            <el-option
              v-for="item in state.guideList"
              :key="item.id"
              :label="item.name"
              :value="item.id"
            />
          </el-select>
        </el-form-item>

        <el-form-item :label="$t('lz.label.tmsMaintenanceSchedule.ruleActive')" prop="isActive">
          <el-switch v-model="state.ruleForm.isActive" />
        </el-form-item>

        <el-form-item :label="$t('lz.entity.base.remark')" prop="remark">
          <el-input v-model="state.ruleForm.remark" type="textarea" :rows="2" />
        </el-form-item>
      </el-form>

      <template #footer>
        <el-button @click="handleCloseRuleDialog" :disabled="state.ruleSubmitting">
          {{ $t('lz.button.base.cancel') }}
        </el-button>
        <el-button type="primary" @click="handleSubmitRule" :loading="state.ruleSubmitting">
          {{ $t('lz.button.base.save') }}
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script lang="ts" setup>
import { reactive, ref, computed, onMounted, defineAsyncComponent } from 'vue';
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { getAPI } from '/@/utils/axios-utils';
import { TmsMaintenanceAlertApi, TmsMaintenanceAlertRuleApi, TmsEquipmentTypeApi, TmsMaintenanceGuideApi } from '/@/api-services/tms';
import { SysUserApi } from '/@/api-services/system';
import type { MaintenanceAlertListOutput, MaintenanceAlertRuleListOutput } from '/@/api-services/tms/models';
import CreateScheduleFromAlert from './createScheduleFromAlert.vue';
import { VxeGridInstance, VxeGridListeners } from 'vxe-table';

const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));

const emit = defineEmits<{
  (e: 'update:pending-count', count: number): void;
}>();

const { t: $t } = useI18n();
const xGrid = ref<VxeGridInstance>();
const xGridRules = ref<VxeGridInstance>();
const ruleFormRef = ref<FormInstance>();
const createScheduleDialogRef = ref();

// State
const state = reactive({
  loading: false,
  tableData: [] as MaintenanceAlertListOutput[],
  employeeList: [] as any[],
  equipmentTypeList: [] as any[],
  guideList: [] as any[],
  filter: {
    status: '' as string,
    alertType: undefined as string | undefined,
    priority: undefined as string | undefined,
  },
  pagination: {
    page: 1,
    pageSize: 20,
    total: 0,
  },
  statusCounts: {
    total: 0,
    pending: 0,
    acknowledged: 0,
    scheduled: 0,
    dismissed: 0,
  },
  // Rule config
  ruleConfigVisible: false,
  ruleLoading: false,
  ruleList: [] as (MaintenanceAlertRuleListOutput & { _loading?: boolean })[],
  ruleDialogVisible: false,
  ruleDialogTitle: '',
  ruleOperateType: 'add' as 'add' | 'edit',
  ruleSubmitting: false,
  ruleForm: {
    id: '',
    name: '',
    equipmentTypeId: undefined as string | undefined,
    manufacturer: '',
    errorThreshold: 3,
    periodDays: 1,
    alertPriority: 'Medium',
    suggestedGuideId: undefined as string | undefined,
    isActive: true,
    remark: '',
  },
});

// Grid options for main alerts table
const gridOptions = reactive({
  id: 'maintenanceAlert',
  border: true,
  stripe: true,
  round: true,
  showOverflow: true,
  loading: false,
  height: 600,
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
  data: [] as MaintenanceAlertListOutput[],
  columns: [
    { type: 'seq' as const, width: 60, align: 'center' as const, title: $t('lz.label.base.idx') },
    { field: 'equipmentName', title: $t('lz.entity.tms.equipment'), minWidth: 180, slots: { default: 'equipment_default' } },
    { field: 'alertMessage', title: $t('lz.label.tmsMaintenanceSchedule.alertMessage'), minWidth: 200 },
    { field: 'alertType', title: $t('lz.label.tmsMaintenanceSchedule.alertType'), width: 130, align: 'center' as const, slots: { default: 'alertType_default' } },
    { field: 'priority', title: $t('lz.entity.base.priority'), width: 100, align: 'center' as const, slots: { default: 'priority_default' } },
    { field: 'status', title: $t('lz.entity.base.status'), width: 120, align: 'center' as const, slots: { default: 'status_default' } },
    { field: 'errorCount', title: $t('lz.label.tmsMaintenanceSchedule.errorCount'), width: 100, align: 'center' as const, slots: { default: 'errorCount_default' } },
    { field: 'createdTime', title: $t('lz.entity.base.createTime'), width: 160, slots: { default: 'createdTime_default' } },
    { title: $t('lz.label.base.action'), width: 200, align: 'center' as const, fixed: 'right' as const, slots: { default: 'row_buttons' } },
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

// Handle custom column for grid
const handleCustomColumn = () => {
  xGrid.value?.openCustom();
};

// Grid options for rules table
const ruleGridOptions = reactive({
  id: 'maintenanceAlertRule',
  border: true,
  stripe: true,
  round: true,
  showOverflow: true,
  loading: false,
  maxHeight: 350,
  rowConfig: {
    keyField: 'id',
    isHover: true,
  },
  columnConfig: {
    isCurrent: false,
  },
  data: [] as (MaintenanceAlertRuleListOutput & { _loading?: boolean })[],
  columns: [
    { type: 'seq' as const, width: 60, align: 'center' as const, title: $t('lz.label.base.idx') },
    { field: 'name', title: $t('lz.label.tmsMaintenanceSchedule.ruleName'), minWidth: 150 },
    { field: 'equipmentTypeName', title: $t('lz.entity.tms.equipmentType'), width: 120, slots: { default: 'equipmentTypeName_default' } },
    { title: $t('lz.label.tmsMaintenanceSchedule.ruleCondition'), width: 180, slots: { default: 'ruleCondition_default' } },
    { field: 'alertPriority', title: $t('lz.entity.base.priority'), width: 100, align: 'center' as const, slots: { default: 'alertPriority_default' } },
    { field: 'isActive', title: $t('lz.entity.base.status'), width: 100, align: 'center' as const, slots: { default: 'isActive_default' } },
    { title: $t('lz.label.base.action'), width: 120, align: 'center' as const, fixed: 'right' as const, slots: { default: 'rule_buttons' } },
  ],
});

// Options
const alertTypeOptions = computed(() => [
  { value: 'RuleTriggered', label: $t('lz.label.tmsMaintenanceSchedule.alertTypeRule') },
  { value: 'ManualRequest', label: $t('lz.label.tmsMaintenanceSchedule.alertTypeManual') },
  { value: 'ScheduleDue', label: $t('lz.label.tmsMaintenanceSchedule.alertTypeScheduleDue') },
]);

const priorityOptions = computed(() => [
  { value: 'High', label: $t('lz.label.tmsMaintenanceSchedule.priorityHigh') },
  { value: 'Medium', label: $t('lz.label.tmsMaintenanceSchedule.priorityMedium') },
  { value: 'Low', label: $t('lz.label.tmsMaintenanceSchedule.priorityLow') },
]);

// Form rules
const ruleFormRules = computed<FormRules>(() => ({
  name: [{ required: true, message: $t('lz.message.tmsMaintenanceSchedule.pleaseInputRuleName'), trigger: 'blur' }],
  errorThreshold: [{ required: true, message: $t('lz.message.tmsMaintenanceSchedule.pleaseInputErrorThreshold'), trigger: 'blur' }],
  periodDays: [{ required: true, message: $t('lz.message.tmsMaintenanceSchedule.pleaseInputPeriodDays'), trigger: 'blur' }],
}));

// Methods
const getAlertTypeLabel = (type: string) => {
  const option = alertTypeOptions.value.find(o => o.value === type);
  return option?.label || type;
};

const getAlertTypeTagType = (type: string): 'danger' | 'warning' | 'info' => {
  switch (type) {
    case 'RuleTriggered': return 'danger';
    case 'ManualRequest': return 'warning';
    case 'ScheduleDue': return 'info';
    default: return 'info';
  }
};

const getPriorityLabel = (priority: string) => {
  const option = priorityOptions.value.find(o => o.value === priority);
  return option?.label || priority;
};

const getPriorityType = (priority: string): 'danger' | 'warning' | 'success' | 'info' => {
  switch (priority) {
    case 'High': return 'danger';
    case 'Medium': return 'warning';
    case 'Low': return 'success';
    default: return 'info';
  }
};

const getStatusLabel = (status: string) => {
  switch (status) {
    case 'Pending': return $t('lz.label.tmsMaintenanceSchedule.alertStatusPending');
    case 'Acknowledged': return $t('lz.label.tmsMaintenanceSchedule.alertStatusAcknowledged');
    case 'Scheduled': return $t('lz.label.tmsMaintenanceSchedule.alertStatusScheduled');
    case 'Dismissed': return $t('lz.label.tmsMaintenanceSchedule.alertStatusDismissed');
    default: return status;
  }
};

const getStatusTagType = (status: string): 'danger' | 'warning' | 'primary' | 'info' => {
  switch (status) {
    case 'Pending': return 'danger';
    case 'Acknowledged': return 'warning';
    case 'Scheduled': return 'primary';
    case 'Dismissed': return 'info';
    default: return 'info';
  }
};

const formatDateTime = (date: Date | string | undefined) => {
  if (!date) return '-';
  const d = new Date(date);
  return d.toLocaleString('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
};

const loadEmployeeList = async () => {
  try {
    const res = await getAPI(SysUserApi).apiSystemSysuserListPost({});
    if (res.data.succeeded) {
      state.employeeList = res.data.result || [];
    }
  } catch (error) {
    console.error('Error loading employees:', error);
  }
};

const loadEquipmentTypes = async () => {
  try {
    const res = await getAPI(TmsEquipmentTypeApi).apiTmsTmsequipmenttypeListGet();
    if (res.data.succeeded) {
      state.equipmentTypeList = res.data.result || [];
    }
  } catch (error) {
    console.error('Error loading equipment types:', error);
  }
};

const loadGuideList = async () => {
  try {
    const res = await getAPI(TmsMaintenanceGuideApi).apiTmsTmsmaintenanceguideGetlistPost({ status: 1 });
    if (res.data.succeeded) {
      state.guideList = res.data.result || [];
    }
  } catch (error) {
    console.error('Error loading guides:', error);
  }
};

const loadStatusCounts = async () => {
  try {
    const res = await getAPI(TmsMaintenanceAlertApi).apiTmsTmsmaintenancealertGetcountbystatusGet();
    if (res.data.succeeded && res.data.result) {
      const data = res.data.result;
      state.statusCounts = {
        total: (data.pendingCount || 0) + (data.acknowledgedCount || 0) + (data.scheduledCount || 0) + (data.dismissedCount || 0),
        pending: data.pendingCount || 0,
        acknowledged: data.acknowledgedCount || 0,
        scheduled: data.scheduledCount || 0,
        dismissed: data.dismissedCount || 0,
      };
      // Emit pending count to parent
      emit('update:pending-count', state.statusCounts.pending);
    }
  } catch (error) {
    console.error('Error loading status counts:', error);
  }
};

const handleQuery = async () => {
  state.loading = true;
  gridOptions.loading = true;
  try {
    const res = await getAPI(TmsMaintenanceAlertApi).apiTmsTmsmaintenancealertGetlistPost({
      page: state.pagination.page,
      pageSize: state.pagination.pageSize,
      status: state.filter.status || undefined,
      alertType: state.filter.alertType || undefined,
      priority: state.filter.priority || undefined,
    });
    if (res.data.succeeded && res.data.result) {
      state.tableData = res.data.result.records || [];
      state.pagination.total = res.data.result.total || 0;
      // Sync data to grid
      gridOptions.data = state.tableData;
    }
    // Also refresh status counts
    await loadStatusCounts();
  } catch (error: any) {
    console.error('Error loading alerts:', error);
    ElMessage.error(error?.message || $t('lz.message.base.acctionFail'));
  } finally {
    state.loading = false;
    gridOptions.loading = false;
  }
};

const handleAcknowledge = async (row: MaintenanceAlertListOutput) => {
  try {
    await getAPI(TmsMaintenanceAlertApi).apiTmsTmsmaintenancealertAcknowledgePost({ id: row.id! });
    ElMessage.success($t('lz.message.tmsMaintenanceSchedule.acknowledgeSuccess'));
    await handleQuery();
  } catch (error: any) {
    console.error('Error acknowledging alert:', error);
    ElMessage.error(error?.message || $t('lz.message.base.acctionFail'));
  }
};

const handleDismiss = async (row: MaintenanceAlertListOutput) => {
  try {
    const { value } = await ElMessageBox.prompt(
      $t('lz.message.tmsMaintenanceSchedule.dismissReasonPrompt'),
      $t('lz.label.tmsMaintenanceSchedule.dismiss'),
      {
        confirmButtonText: $t('lz.button.base.confirm'),
        cancelButtonText: $t('lz.button.base.cancel'),
        inputPlaceholder: $t('lz.label.tmsMaintenanceSchedule.dismissReasonPlaceholder'),
      }
    );

    await getAPI(TmsMaintenanceAlertApi).apiTmsTmsmaintenancealertDismissPost({
      id: row.id!,
      reason: value,
    });
    ElMessage.success($t('lz.message.tmsMaintenanceSchedule.dismissSuccess'));
    await handleQuery();
  } catch (error: any) {
    if (error !== 'cancel') {
      console.error('Error dismissing alert:', error);
      ElMessage.error(error?.message || $t('lz.message.base.acctionFail'));
    }
  }
};

const handleCreateSchedule = (row: MaintenanceAlertListOutput) => {
  createScheduleDialogRef.value?.open(row);
};

const handleCreateScheduleSuccess = async () => {
  await handleQuery();
};

// Rule Config Methods
const handleOpenRuleConfig = async () => {
  state.ruleConfigVisible = true;
  await loadRuleList();
};

const loadRuleList = async () => {
  state.ruleLoading = true;
  ruleGridOptions.loading = true;
  try {
    const res = await getAPI(TmsMaintenanceAlertRuleApi).apiTmsTmsmaintenancealertruleGetlistPost({});
    if (res.data.succeeded && res.data.result) {
      const result = res.data.result;
      const records = 'records' in result ? result.records : Array.isArray(result) ? result : [];
      state.ruleList = (records || []).map((item: any) => ({ ...item, _loading: false }));
      // Sync data to grid
      ruleGridOptions.data = state.ruleList;
    }
  } catch (error: any) {
    console.error('Error loading rules:', error);
    ElMessage.error(error?.message || $t('lz.message.base.acctionFail'));
  } finally {
    state.ruleLoading = false;
    ruleGridOptions.loading = false;
  }
};

const resetRuleForm = () => {
  state.ruleForm = {
    id: '',
    name: '',
    equipmentTypeId: undefined,
    manufacturer: '',
    errorThreshold: 3,
    periodDays: 1,
    alertPriority: 'Medium',
    suggestedGuideId: undefined,
    isActive: true,
    remark: '',
  };
};

const handleAddRule = () => {
  resetRuleForm();
  state.ruleOperateType = 'add';
  state.ruleDialogTitle = $t('lz.label.tmsMaintenanceSchedule.addRule');
  state.ruleDialogVisible = true;
};

const handleEditRule = (row: MaintenanceAlertRuleListOutput) => {
  state.ruleOperateType = 'edit';
  state.ruleDialogTitle = $t('lz.label.tmsMaintenanceSchedule.editRule');
  state.ruleForm = {
    id: row.id || '',
    name: row.name || '',
    equipmentTypeId: row.equipmentTypeId || undefined,
    manufacturer: row.manufacturer || '',
    errorThreshold: row.errorThreshold || 3,
    periodDays: row.periodDays || 1,
    alertPriority: row.alertPriority || 'Medium',
    suggestedGuideId: row.suggestedGuideId || undefined,
    isActive: row.isActive ?? true,
    remark: '',
  };
  state.ruleDialogVisible = true;
};

const handleDeleteRule = async (row: MaintenanceAlertRuleListOutput) => {
  try {
    await ElMessageBox.confirm(
      $t('lz.message.tmsMaintenanceSchedule.confirmDeleteRule'),
      $t('lz.message.tmsDutySchedule.confirmDelete'),
      {
        confirmButtonText: $t('lz.button.base.delete'),
        cancelButtonText: $t('lz.button.base.cancel'),
        type: 'warning',
      }
    );

    await getAPI(TmsMaintenanceAlertRuleApi).apiTmsTmsmaintenancealertruleDeletePost({ id: row.id! });
    ElMessage.success($t('lz.message.base.deleteSuccess'));
    await loadRuleList();
  } catch (error: any) {
    if (error !== 'cancel') {
      console.error('Error deleting rule:', error);
      ElMessage.error(error?.message || $t('lz.message.base.acctionFail'));
    }
  }
};

const handleToggleRuleActive = async (row: MaintenanceAlertRuleListOutput & { _loading?: boolean }) => {
  row._loading = true;
  try {
    if (row.isActive) {
      await getAPI(TmsMaintenanceAlertRuleApi).apiTmsTmsmaintenancealertruleActivateIdPost(row.id!);
    } else {
      await getAPI(TmsMaintenanceAlertRuleApi).apiTmsTmsmaintenancealertruleDeactivateIdPost(row.id!);
    }
    ElMessage.success($t('lz.message.base.updateSuccess'));
  } catch (error: any) {
    // Revert the change
    row.isActive = !row.isActive;
    console.error('Error toggling rule:', error);
    ElMessage.error(error?.message || $t('lz.message.base.acctionFail'));
  } finally {
    row._loading = false;
  }
};

const handleCloseRuleDialog = () => {
  state.ruleDialogVisible = false;
  ruleFormRef.value?.resetFields();
};

const handleSubmitRule = async () => {
  if (!ruleFormRef.value) return;

  try {
    await ruleFormRef.value.validate();
    state.ruleSubmitting = true;

    if (state.ruleOperateType === 'add') {
      await getAPI(TmsMaintenanceAlertRuleApi).apiTmsTmsmaintenancealertruleAddPost({
        name: state.ruleForm.name,
        equipmentTypeId: state.ruleForm.equipmentTypeId || undefined,
        manufacturer: state.ruleForm.manufacturer || undefined,
        errorThreshold: state.ruleForm.errorThreshold,
        periodDays: state.ruleForm.periodDays,
        alertPriority: state.ruleForm.alertPriority,
        suggestedGuideId: state.ruleForm.suggestedGuideId || undefined,
        isActive: state.ruleForm.isActive,
        remark: state.ruleForm.remark || undefined,
      });
      ElMessage.success($t('lz.message.base.createSuccess'));
    } else {
      await getAPI(TmsMaintenanceAlertRuleApi).apiTmsTmsmaintenancealertruleUpdatePost({
        id: state.ruleForm.id,
        name: state.ruleForm.name,
        equipmentTypeId: state.ruleForm.equipmentTypeId || undefined,
        manufacturer: state.ruleForm.manufacturer || undefined,
        errorThreshold: state.ruleForm.errorThreshold,
        periodDays: state.ruleForm.periodDays,
        alertPriority: state.ruleForm.alertPriority,
        suggestedGuideId: state.ruleForm.suggestedGuideId || undefined,
        isActive: state.ruleForm.isActive,
        remark: state.ruleForm.remark || undefined,
      });
      ElMessage.success($t('lz.message.base.updateSuccess'));
    }

    handleCloseRuleDialog();
    await loadRuleList();
  } catch (error: any) {
    if (error !== false) {
      console.error('Error saving rule:', error);
      ElMessage.error(error?.message || $t('lz.message.base.acctionFail'));
    }
  } finally {
    state.ruleSubmitting = false;
  }
};

// Init
onMounted(async () => {
  await Promise.all([
    loadEmployeeList(),
    loadEquipmentTypes(),
    loadGuideList(),
  ]);
  await handleQuery();
});
</script>

<style lang="scss" scoped>
.maintenance-alert-tab {
  height: 100%;
}

.status-badge {
  margin-left: 5px;
  
  :deep(.el-badge__content) {
    font-size: 10px;
  }
}
</style>
