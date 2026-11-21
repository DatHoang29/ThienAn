<template>
  <div class="tms-duty-swap-modal-container">
    <el-dialog v-model="state.isShowDialog" draggable :close-on-click-modal="false" width="900px">
      <template #header>
        <div style="color: #fff">
          <el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Switch />
          </el-icon>
          <span> {{ $t('lz.label.tmsDutySchedule.swapTitle') }} </span>
        </div>
      </template>
      
      <div class="swap-container">
        <!-- Current Schedule Info -->
        <el-card shadow="hover" style="margin-bottom: 15px;">
          <template #header>
            <div style="display: flex; justify-content: space-between; align-items: center;">
              <span>{{ $t('lz.label.tmsDutySchedule.currentScheduleInfo') }}</span>
            </div>
          </template>
          
          <el-descriptions :column="2" border>
            <el-descriptions-item :label="$t('lz.label.base.week')">{{ state.currentSchedule?.weekNumber }}</el-descriptions-item>
            <el-descriptions-item :label="$t('lz.entity.tms.patrolTeam')">{{ state.currentSchedule?.teamName }}</el-descriptions-item>
            <el-descriptions-item :label="$t('lz.label.base.day')">{{ formatDate(state.currentSwapDetail?.date) }}</el-descriptions-item>
            <el-descriptions-item :label="$t('lz.entity.tms.shift')">{{ state.currentSwapDetail?.shiftName }}</el-descriptions-item>
            <el-descriptions-item :label="$t('lz.label.base.dateTime')">{{ state.currentSwapDetail?.startTime }} - {{ state.currentSwapDetail?.endTime }}</el-descriptions-item>
            <el-descriptions-item :label="$t('lz.label.base.location')">{{ state.currentSwapDetail?.locationName }}</el-descriptions-item>
          </el-descriptions>
          
          <div style="margin-top: 15px;">
            <strong>{{ $t('lz.label.tmsDutySchedule.currentEmployee') }}</strong>
            <div style="margin-top: 5px;">
              <el-tag v-for="emp in state.currentSwapDetail?.employees" :key="emp.id" style="margin-right: 5px;">
                {{ emp.fullName }}
                <el-icon v-if="emp.isLeader" style="margin-left: 4px;"><ele-Crown /></el-icon>
              </el-tag>
            </div>
          </div>
        </el-card>
        
        <!-- Swap Form -->
        <el-form :model="state.swapForm" ref="swapFormRef" label-width="auto">
          <el-row :gutter="10">
            <el-col :xs="24" :sm="12">
              <el-form-item :label="$t('lz.entity.tmsDutySchedule.swapType')" prop="swapType" :rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.tmsDutySchedule.swapType') }), trigger: 'change' }]">
                <el-select v-model="state.swapForm.swapType" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.tmsDutySchedule.swapType') })" class="w100" @change="onSwapTypeChange">
                  <el-option value="direct" :label="$t('lz.label.tmsDutySchedule.directSwap')" />
                  <el-option value="temporary" :label="$t('lz.label.tmsDutySchedule.temporaryReplacement')" />
                  <el-option value="permanent" :label="$t('lz.label.tmsDutySchedule.permanentReplacement')" />
                </el-select>
              </el-form-item>
            </el-col>
            
            <el-col :xs="24" :sm="12">
              <el-form-item :label="$t('lz.entity.tmsDutySchedule.requestingEmployee')" prop="requestingEmployeeId" :rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.tms.employee') }), trigger: 'change' }]">
                <el-select v-model="state.swapForm.requestingEmployeeId" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.tms.employee') })" class="w100">
                  <el-option v-for="emp in state.currentSwapDetail?.employees" :key="emp.id" :label="emp.fullName" :value="emp.id" />
                </el-select>
              </el-form-item>
            </el-col>
          </el-row>
          
          <el-row :gutter="10" v-if="state.swapForm.swapType === 'direct'">
            <el-col :xs="24" :sm="12">
              <el-form-item :label="$t('lz.entity.tmsDutySchedule.swapDate')" prop="targetDate" :rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.label.base.day') }), trigger: 'change' }]">
                <el-date-picker v-model="state.swapForm.targetDate" type="date" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.tmsDutySchedule.swapDate') })" class="w100" />
              </el-form-item>
            </el-col>
            
            <el-col :xs="24" :sm="12">
              <el-form-item :label="$t('lz.entity.tmsDutySchedule.targetShift')" prop="targetShiftId" :rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.tms.shift') }), trigger: 'change' }]">
                <el-select v-model="state.swapForm.targetShiftId" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.tms.shift') })" class="w100" @change="onTargetShiftChange">
                  <el-option v-for="shift in state.shiftList" :key="shift.id" :label="shift.name" :value="shift.id" />
                </el-select>
              </el-form-item>
            </el-col>
          </el-row>
          
          <el-row :gutter="10" v-if="state.swapForm.swapType === 'direct' && state.targetSwapDetail">
            <el-col :xs="24">
              <el-form-item :label="$t('lz.entity.tmsDutySchedule.targetEmployee')" prop="targetEmployeeId" :rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.tms.employee') }), trigger: 'change' }]">
                <el-select v-model="state.swapForm.targetEmployeeId" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.tms.employee') })" class="w100">
                  <el-option v-for="emp in state.targetSwapDetail?.employees" :key="emp.id" :label="emp.fullName" :value="emp.id" />
                </el-select>
              </el-form-item>
            </el-col>
          </el-row>
          
          <el-row :gutter="10" v-if="state.swapForm.swapType !== 'direct'">
            <el-col :xs="24">
              <el-form-item :label="$t('lz.entity.tmsDutySchedule.replacementEmployee')" prop="replacementEmployeeId" :rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.tms.employee') }), trigger: 'change' }]">
                <el-select v-model="state.swapForm.replacementEmployeeId" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.tmsDutySchedule.replacementEmployee') })" class="w100" filterable>
                  <el-option v-for="emp in state.availableEmployees" :key="emp.id" :label="emp.name" :value="emp.id" />
                </el-select>
              </el-form-item>
            </el-col>
          </el-row>
          
          <el-row :gutter="10">
            <el-col :xs="24">
              <el-form-item :label="$t('lz.entity.tmsDutySchedule.swapReason')" prop="reason" :rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.base.reason') }), trigger: 'blur' }]">
                <el-input v-model="state.swapForm.reason" type="textarea" :rows="3" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.tmsDutySchedule.swapReason') })" />
              </el-form-item>
            </el-col>
          </el-row>
        </el-form>
        
        <!-- Swap History -->
        <el-divider content-position="left">{{ $t('lz.label.tmsDutySchedule.swapHistory') }}</el-divider>
        
        <vxe-grid ref="historyGrid" v-bind="historyOptions" :row-config="{ height: 40 }">
          <template #swap_type_cell="{ row }">
            <el-tag :type="getSwapTypeTag(row.swapType)">{{ getSwapTypeText(row.swapType) }}</el-tag>
          </template>
          
          <template #status_cell="{ row }">
            <el-tag :type="getStatusTag(row.status)">{{ getStatusText(row.status) }}</el-tag>
          </template>
          
          <template #empty>
            <el-empty :image-size="100" :description="$t('lz.label.tmsDutySchedule.noSwapHistory')" />
          </template>
        </vxe-grid>
      </div>
      
      <template #footer>
        <span class="dialog-footer">
          <el-button @click="cancel">{{ $t('lz.button.base.cancel') }}</el-button>
          <el-button type="primary" @click="submitSwapRequest" :loading="state.loading">{{ $t('lz.button.base.sendRequest') }}</el-button>
        </span>
      </template>
    </el-dialog>
  </div>
</template>

<script lang="ts" setup>
import { useI18n } from 'vue-i18n';


import { reactive, ref} from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';

const { t: $t } = useI18n();

const emits = defineEmits(['handleQuery']);
const swapFormRef = ref();

// Khai báo global mockData cho typescript
// eslint-disable-next-line @typescript-eslint/no-explicit-any
declare global {
  interface Window {
    mockData: any[];
  }
}

const state = reactive({
  isShowDialog: false,
  loading: false,
  currentSchedule: null as any,
  currentSwapDetail: null as any,
  targetSwapDetail: null as any,
  swapForm: {
    swapType: 'direct',
    requestingEmployeeId: '',
    targetDate: new Date(),
    targetShiftId: '',
    targetEmployeeId: '',
    replacementEmployeeId: '',
    reason: '',
  },
  shiftList: [
    { id: 'shift1', name: 'Ca 1 (06:00 - 14:00)' },
    { id: 'shift2', name: 'Ca 2 (14:00 - 22:00)' },
    { id: 'shift3', name: 'Ca 3 (22:00 - 06:00)' },
  ],
  availableEmployees: [
    { id: 'emp1', name: 'Nguyễn Văn A' },
    { id: 'emp2', name: 'Trần Văn B' },
    { id: 'emp3', name: 'Lê Văn C' },
    { id: 'emp4', name: 'Phạm Văn D' },
    { id: 'emp5', name: 'Hoàng Văn E' },
  ],
  swapHistory: [
    {
      id: 'swap1',
      requestDate: '2025-06-10',
      requestingEmployee: 'Nguyễn Văn A',
      targetEmployee: 'Trần Văn B',
      swapType: 'direct',
      reason: 'Có việc đột xuất',
      status: 'approved',
      approvedBy: 'Quản lý',
      approvedDate: '2025-06-10',
    },
    {
      id: 'swap2',
      requestDate: '2025-06-08',
      requestingEmployee: 'Lê Văn C',
      targetEmployee: 'Phạm Văn D',
      swapType: 'temporary',
      reason: 'Ốm đau',
      status: 'pending',
    },
  ],
});

// History grid options
const historyOptions = useVxeTable<any>(
  {
    id: 'swapHistory',
    name: 'SwapHistory',
    columns: [
      { field: 'requestDate', title: $t('lz.entity.base.requestDate'), minWidth: 120, showOverflow: 'tooltip' },
      { field: 'requestingEmployee', title: $t('lz.entity.base.requestedBy'), minWidth: 150, showOverflow: 'tooltip' },
      { field: 'targetEmployee', title: $t('lz.entity.tmsDutySchedule.targetEmployee'), minWidth: 150, showOverflow: 'tooltip' },
      { field: 'swapType', title: $t('lz.entity.tmsDutySchedule.swapType'), minWidth: 120, showOverflow: 'tooltip', slots: { default: 'swap_type_cell' } },
      { field: 'reason', title: $t('lz.entity.base.reason'), minWidth: 200, showOverflow: 'tooltip' },
      { field: 'status', title: $t('lz.entity.base.status'), minWidth: 100, showOverflow: 'tooltip', slots: { default: 'status_cell' } },
      { field: 'approvedBy', title: $t('lz.entity.base.approvedBy'), minWidth: 120, showOverflow: 'tooltip' },
      { field: 'approvedDate', title: $t('lz.entity.base.approvedDate'), minWidth: 120, showOverflow: 'tooltip' },
    ],
  },
  {
    proxyConfig: { autoLoad: false },
    toolbarConfig: { export: false },
    checkboxConfig: { highlight: true },
  }
);

const openDialog = (schedule: any) => {
  state.currentSchedule = schedule;
  
  // Get first detail for demo
  if (schedule.details && schedule.details.length > 0) {
    state.currentSwapDetail = schedule.details[0];
  }
  
  state.swapForm = {
    swapType: 'direct',
    requestingEmployeeId: '',
    targetDate: new Date(),
    targetShiftId: '',
    targetEmployeeId: '',
    replacementEmployeeId: '',
    reason: '',
  };
  
  state.isShowDialog = true;
  swapFormRef.value?.resetFields();
  
  // Load history data
  historyOptions.data = state.swapHistory;
};

const cancel = () => {
  state.isShowDialog = false;
};

const onSwapTypeChange = () => {
  // Reset form when swap type changes
  state.swapForm.targetEmployeeId = '';
  state.swapForm.replacementEmployeeId = '';
  state.targetSwapDetail = null;
};

const onTargetShiftChange = () => {
  // Find target detail based on date and shift
  if (state.swapForm.targetDate && state.swapForm.targetShiftId) {
    const targetDate = state.swapForm.targetDate.toISOString().split('T')[0];
    const shift = state.shiftList.find(s => s.id === state.swapForm.targetShiftId);
    
    // Mock: find a detail with the same date and shift
    state.targetSwapDetail = {
      id: 'target-detail',
      date: targetDate,
      shiftName: shift?.name || '',
      startTime: shift?.name?.split('(')[1]?.split('-')[0]?.trim() || '',
      endTime: shift?.name?.split('(')[1]?.split('-')[1]?.replace(')', '')?.trim() || '',
      employees: [
        { id: 'emp3', fullName: 'Lê Văn C', isLeader: false },
        { id: 'emp4', fullName: 'Phạm Văn D', isLeader: true },
      ]
    };
  }
};

const submitSwapRequest = () => {
  swapFormRef.value.validate((valid: boolean) => {
    if (!valid) return;
    
    ElMessageBox.confirm(
      $t('lz.message.tmsDutySchedule.confirmSwapRequest'),
      $t('lz.label.base.confirmRequest'),
      {
        confirmButtonText: $t('lz.button.base.send'),
        cancelButtonText: $t('lz.button.base.cancel'),
        type: 'warning',
      }
    ).then(() => {
      state.loading = true;
      
      // Simulate API call
      setTimeout(() => {
        const newSwap = {
          id: Date.now().toString(),
          requestDate: new Date().toISOString().split('T')[0],
          requestingEmployee: state.currentSwapDetail?.employees?.find((emp: any) => emp.id === state.swapForm.requestingEmployeeId)?.fullName || '',
          targetEmployee: state.swapForm.swapType === 'direct' 
            ? state.targetSwapDetail?.employees?.find((emp: any) => emp.id === state.swapForm.targetEmployeeId)?.fullName || ''
            : state.availableEmployees?.find((emp: any) => emp.id === state.swapForm.replacementEmployeeId)?.name || '',
          swapType: state.swapForm.swapType,
          reason: state.swapForm.reason,
          status: 'pending',
        };
        
        state.swapHistory.unshift(newSwap);
        historyOptions.data = state.swapHistory;
        
        state.loading = false;
        state.isShowDialog = false;
        
        ElMessage.success($t('lz.message.tmsDutySchedule.swapRequestSuccess'));
        emits('handleQuery');
      }, 1000);
    }).catch(() => {
      // User cancelled
    });
  });
};

const formatDate = (dateStr: string): string => {
  if (!dateStr) return '';
  const date = new Date(dateStr);
  return date.toLocaleDateString('vi-VN');
};

const getSwapTypeTag = (type: string): string => {
  switch (type) {
    case 'direct': return 'primary';
    case 'temporary': return 'warning';
    case 'permanent': return 'danger';
    default: return 'info';
  }
};

const getSwapTypeText = (type: string): string => {
  switch (type) {
    case 'direct': return $t('lz.label.tmsDutySchedule.directSwap');
    case 'temporary': return $t('lz.label.tmsDutySchedule.temporaryReplacement');
    case 'permanent': return $t('lz.label.tmsDutySchedule.permanentReplacement');
    default: return $t('lz.label.base.unknown');
  }
};

const getStatusTag = (status: string): string => {
  switch (status) {
    case 'pending': return 'warning';
    case 'approved': return 'success';
    case 'rejected': return 'danger';
    case 'completed': return 'info';
    default: return 'info';
  }
};

const getStatusText = (status: string): string => {
  switch (status) {
    case 'pending': return $t('lz.label.tmsDutySchedule.pendingApproval');
    case 'approved': return $t('lz.label.tmsDutySchedule.approved');
    case 'rejected': return $t('lz.label.tmsDutySchedule.rejected');
    case 'completed': return $t('lz.label.tmsDutySchedule.completed');
    default: return $t('lz.label.base.unknown');
  }
};

defineExpose({ openDialog });
</script>

<style scoped>
.tms-duty-swap-modal-container {
  /* Style cho modal */
  position: relative;
}

.swap-container {
  max-height: 600px;
  overflow-y: auto;
}

.dialog-footer {
  display: flex;
  justify-content: flex-end;
  gap: 10px;
}
</style>