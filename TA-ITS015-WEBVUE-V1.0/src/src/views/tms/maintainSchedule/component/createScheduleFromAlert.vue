<template>
  <el-dialog
    v-model="state.visible"
    :title="$t('lz.label.tmsMaintenanceSchedule.createScheduleFromAlert')"
    width="600px"
    :close-on-click-modal="false"
    @close="handleClose"
  >
    <template #header>
      <div style="color: #fff; display: flex; align-items: center;">
        <el-icon size="16" style="margin-right: 5px;">
          <ele-Calendar />
        </el-icon>
        <span>{{ $t('lz.label.tmsMaintenanceSchedule.createScheduleFromAlert') }}</span>
      </div>
    </template>

    <!-- Alert Info -->
    <el-descriptions :column="1" border style="margin-bottom: 20px;">
      <el-descriptions-item :label="$t('lz.entity.tms.equipment')">
        <strong>{{ state.alertData?.equipmentName }}</strong>
        <span style="margin-left: 10px; color: var(--el-text-color-secondary);">{{ state.alertData?.equipmentCode }}</span>
      </el-descriptions-item>
      <el-descriptions-item :label="$t('lz.label.tmsMaintenanceSchedule.alertMessage')">
        {{ state.alertData?.alertMessage }}
      </el-descriptions-item>
      <el-descriptions-item v-if="state.alertData?.suggestedGuideName" :label="$t('lz.label.tmsMaintenanceSchedule.suggestedGuide')">
        <el-tag type="info" size="small">{{ state.alertData?.suggestedGuideName }}</el-tag>
      </el-descriptions-item>
    </el-descriptions>

    <el-form
      ref="formRef"
      :model="state.form"
      :rules="formRules"
      label-width="140px"
      :disabled="state.submitting"
    >
      <el-form-item :label="$t('lz.label.tmsMaintenanceSchedule.scheduleName')" prop="scheduleName">
        <el-input v-model="state.form.scheduleName" :placeholder="$t('lz.label.tmsMaintenanceSchedule.scheduleNamePlaceholder')" />
      </el-form-item>

      <el-form-item :label="$t('lz.entity.base.desc')" prop="description">
        <el-input v-model="state.form.description" type="textarea" :rows="2" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.desc') })" />
      </el-form-item>

      <el-row :gutter="20">
        <el-col :span="12">
          <el-form-item :label="$t('lz.label.tmsMaintenanceSchedule.scheduledDate')" prop="scheduledDate">
            <el-date-picker
              v-model="state.form.scheduledDate"
              type="date"
              :placeholder="$t('lz.label.tmsMaintenanceSchedule.selectScheduledDate')"
              style="width: 100%;"
              value-format="YYYY-MM-DD"
            />
          </el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item :label="$t('lz.label.tmsMaintenanceSchedule.dueDate')" prop="dueDate">
            <el-date-picker
              v-model="state.form.dueDate"
              type="date"
              :placeholder="$t('lz.label.tmsMaintenanceSchedule.selectDueDate')"
              style="width: 100%;"
              value-format="YYYY-MM-DD"
            />
          </el-form-item>
        </el-col>
      </el-row>

      <el-form-item :label="$t('lz.entity.base.priority')" prop="priority">
        <el-select v-model="state.form.priority" style="width: 100%;">
          <el-option
            v-for="item in priorityOptions"
            :key="item.value"
            :label="item.label"
            :value="item.value"
          />
        </el-select>
      </el-form-item>

      <el-form-item :label="$t('lz.label.tmsMaintenanceSchedule.assignedUsers')" prop="assignedUserIds">
        <el-select
          v-model="state.form.assignedUserIds"
          :placeholder="$t('lz.label.tmsMaintenanceSchedule.selectAssignedUsers')"
          multiple
          filterable
          clearable
          style="width: 100%;"
        >
          <el-option
            v-for="user in props.employeeList"
            :key="user.id"
            :label="user.name || user.username"
            :value="user.username"
          />
        </el-select>
      </el-form-item>

      <el-form-item :label="$t('lz.entity.base.remark')" prop="remark">
        <el-input v-model="state.form.remark" type="textarea" :rows="2" :placeholder="$t('lz.label.tmsDutySchedule.remarkPlaceholder')" />
      </el-form-item>
    </el-form>

    <template #footer>
      <el-button @click="handleClose" :disabled="state.submitting">
        {{ $t('lz.button.base.cancel') }}
      </el-button>
      <el-button type="primary" @click="handleSubmit" :loading="state.submitting">
        {{ $t('lz.label.tmsMaintenanceSchedule.createSchedule') }}
      </el-button>
    </template>
  </el-dialog>
</template>

<script lang="ts" setup>
import { reactive, ref, computed } from 'vue';
import { ElMessage, type FormInstance, type FormRules } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { getAPI } from '/@/utils/axios-utils';
import { TmsMaintenanceAlertApi } from '/@/api-services/tms';
import type { MaintenanceAlertListOutput, CreateScheduleFromAlertInput } from '/@/api-services/tms/models';

const props = defineProps<{
  employeeList: any[];
}>();

const emit = defineEmits<{
  (e: 'success'): void;
}>();

const { t: $t } = useI18n();
const formRef = ref<FormInstance>();

// State
const state = reactive({
  visible: false,
  submitting: false,
  alertData: null as MaintenanceAlertListOutput | null,
  form: {
    scheduleName: '',
    description: '',
    scheduledDate: '',
    dueDate: '',
    priority: 'Medium',
    assignedUserIds: [] as string[],
    remark: '',
  },
});

// Options
const priorityOptions = computed(() => [
  { value: 'High', label: $t('lz.label.tmsMaintenanceSchedule.priorityHigh') },
  { value: 'Medium', label: $t('lz.label.tmsMaintenanceSchedule.priorityMedium') },
  { value: 'Low', label: $t('lz.label.tmsMaintenanceSchedule.priorityLow') },
]);

// Form rules
const formRules = computed<FormRules>(() => ({
  scheduleName: [{ required: true, message: $t('lz.message.tmsMaintenanceSchedule.pleaseInputScheduleName'), trigger: 'blur' }],
  scheduledDate: [{ required: true, message: $t('lz.message.tmsMaintenanceSchedule.pleaseSelectScheduledDate'), trigger: 'change' }],
}));

// Methods
const open = (alertData: MaintenanceAlertListOutput) => {
  state.alertData = alertData;
  // Pre-fill form
  state.form = {
    scheduleName: `${$t('lz.label.tmsMaintenanceSchedule.maintenanceFor')} ${alertData.equipmentName}`,
    description: alertData.alertMessage || '',
    scheduledDate: '',
    dueDate: '',
    priority: alertData.priority || 'Medium',
    assignedUserIds: [],
    remark: '',
  };
  state.visible = true;
};

const handleClose = () => {
  state.visible = false;
  formRef.value?.resetFields();
  state.alertData = null;
};

const handleSubmit = async () => {
  if (!formRef.value || !state.alertData) return;

  try {
    await formRef.value.validate();
    state.submitting = true;

    const input: CreateScheduleFromAlertInput = {
      alertId: state.alertData.id!,
      scheduleName: state.form.scheduleName,
      description: state.form.description || undefined,
      scheduledDate: new Date(state.form.scheduledDate),
      dueDate: state.form.dueDate ? new Date(state.form.dueDate) : undefined,
      priority: state.form.priority,
      assignedUserIds: state.form.assignedUserIds.length > 0 ? state.form.assignedUserIds : undefined,
      remark: state.form.remark || undefined,
    };

    await getAPI(TmsMaintenanceAlertApi).apiTmsTmsmaintenancealertCreateschedulefromalertPost(input);
    ElMessage.success($t('lz.message.tmsMaintenanceSchedule.createScheduleSuccess'));
    handleClose();
    emit('success');
  } catch (error: any) {
    if (error !== false) {
      console.error('Error creating schedule:', error);
      ElMessage.error(error?.message || $t('lz.message.base.acctionFail'));
    }
  } finally {
    state.submitting = false;
  }
};

// Expose methods
defineExpose({
  open,
});
</script>

<style lang="scss" scoped>
</style>
