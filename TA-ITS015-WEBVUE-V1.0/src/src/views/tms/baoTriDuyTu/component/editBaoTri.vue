<template>
  <div class="tms-lichsu-tuanduong-modal-container">
    <el-dialog v-model="state.isShowDialog" draggable :close-on-click-modal="false" width="600px">
      <template #header>
        <div>
          <el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle">
            <ele-Edit />
          </el-icon>
          <span>{{ props.title }}</span>
        </div>
      </template>
      <el-form :model="state.ruleForm" ref="ruleFormRef" label-width="auto">
        <el-row :gutter="10">
          <template v-if="props.multi">
            <el-col :xs="24" class="mb20" v-show="false">
              <el-form-item :label="$t('lz.label.base.week')" prop="week" :rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.label.base.week') }), trigger: 'blur' }]">
                <el-input v-model="state.ruleForm.week" :placeholder="$t('lz.label.tms.scheduleWeekExample')" clearable />
              </el-form-item>
            </el-col>
            <el-col :xs="24" class="mb20">
              <el-form-item :label="$t('lz.label.base.weekdays')" prop="dates" :rules="[{ required: true, message: $t('lz.validation.base.selectAtLeast', { count: 1, PropertyName: $t('lz.label.base.day') }), trigger: 'change' }]">
                <el-date-picker v-model="state.ruleForm.dates" type="dates" :placeholder="$t('lz.label.tms.selectWeekdays')" class="w100" />
              </el-form-item>
            </el-col>
            <el-col :xs="24" class="mb20">
              <el-form-item :label="$t('lz.entity.tms.shift')" prop="shifts" :rules="[{ required: true, message: $t('lz.validation.base.selectAtLeast', { count: 1, PropertyName: $t('lz.entity.tms.shift') }), trigger: 'change' }]">
                <el-select v-model="state.ruleForm.shifts" multiple filterable clearable class="w100" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.tms.shift') })">
                  <el-option v-for="item in state.shiftList" :key="item.id" :label="item.name" :value="item.name" />
                </el-select>
              </el-form-item>
            </el-col>
            <el-col :xs="24" class="mb20">
              <el-form-item :label="$t('lz.entity.tms.employee')" prop="employees" :rules="[{ required: true, message: $t('lz.validation.base.selectAtLeast', { count: 1, PropertyName: $t('lz.entity.tms.employee') }), trigger: 'change' }]">
                <el-select v-model="state.ruleForm.employees" multiple filterable clearable class="w100" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.tms.employee') })">
                  <el-option v-for="item in state.employeeList" :key="item.id" :label="item.name" :value="item" />
                </el-select>
              </el-form-item>
            </el-col>
          </template>
          <template v-else>
            <el-col :xs="24" class="mb20" v-if="false">
              <el-form-item :label="$t('lz.label.base.week')" prop="week" :rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.label.base.week') }), trigger: 'blur' }]">
                <el-input v-model="state.ruleForm.week" :placeholder="$t('lz.label.tms.scheduleWeekExample')" clearable />
              </el-form-item>
            </el-col>
            <el-col :xs="24" class="mb20">
              <el-form-item :label="$t('lz.label.base.day')" prop="date" :rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.label.base.day') }), trigger: 'change' }]">
                <el-date-picker v-model="state.ruleForm.date" type="date" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.label.base.day') })" class="w100" />
              </el-form-item>
            </el-col>
            <el-col :xs="12" class="mb20">
              <el-form-item :label="$t('lz.entity.base.startTime')" prop="timeFrom" :rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.base.startTime') }), trigger: 'change' }]">
                <el-time-picker v-model="state.ruleForm.timeFrom" :placeholder="$t('lz.entity.base.startTime')" class="w100" format="HH:mm" value-format="HH:mm" />
              </el-form-item>
            </el-col>
            <el-col :xs="12" class="mb20">
              <el-form-item :label="$t('lz.entity.base.endTime')" prop="timeTo" :rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.base.endTime') }), trigger: 'change' }]">
                <el-time-picker v-model="state.ruleForm.timeTo" :placeholder="$t('lz.entity.base.endTime')" class="w100" format="HH:mm" value-format="HH:mm" />
              </el-form-item>
            </el-col>
            <el-col :xs="24" class="mb20">
              <el-form-item :label="$t('lz.entity.tms.relatedEquipment')" prop="device">
                <el-input v-model="state.ruleForm.device" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.tms.relatedEquipment') })" clearable />
              </el-form-item>
            </el-col>
            <el-col :xs="24" class="mb20">
              <el-form-item :label="$t('lz.entity.base.remark')" prop="note">
                <el-input v-model="state.ruleForm.note" type="textarea" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.remark') })" clearable />
              </el-form-item>
            </el-col>
            <el-col :xs="24" class="mb20">
              <div v-for="(task, idx) in state.ruleForm.tasks" :key="idx" style="margin-bottom: 10px; border: 1px solid #eee; padding: 10px; border-radius: 6px;">
                <el-form-item :label="`Công việc ${idx+1}`" :prop="`tasks.${idx}.name`" :rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.tms.task') }), trigger: 'change' }]">
                  <el-select v-model="task.name" filterable clearable class="w100" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.tmsWorkDetail.workDetail') })">
                    <el-option v-for="item in state.taskList" :key="item" :label="item" :value="item" />
                  </el-select>
                </el-form-item>
                <el-form-item :label="$t('lz.entity.tms.assignedEmployee')" :prop="`tasks.${idx}.employees`" :rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.tms.employee') }), trigger: 'change' }]">
                  <el-select v-model="task.employees" multiple filterable clearable class="w100" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.tms.employee') })">
                    <el-option v-for="item in state.employeeList" :key="item.id" :label="item.name" :value="item.id" />
                  </el-select>
                </el-form-item>
                <el-button type="danger" size="small" @click="removeTask(idx)" v-if="state.ruleForm.tasks.length > 1">{{ $t('lz.button.base.delete') }}</el-button>
              </div>
              <el-button type="primary" size="small" @click="addTask">{{ $t('lz.button.tms.addTask') }}</el-button>
            </el-col>
          </template>
        </el-row>
      </el-form>
      <template #footer>
        <span class="dialog-footer">
          <el-button @click="cancel">{{ $t('lz.button.base.cancel') }}</el-button>
          <el-button type="primary" @click="submit">{{ $t('lz.button.base.save') }}</el-button>
        </span>
      </template>
    </el-dialog>
  </div>
</template>

<script lang="ts" setup>
import { useI18n } from 'vue-i18n';


import { reactive, ref } from 'vue';
import { ElMessage } from 'element-plus';

const { t: $t } = useI18n();

interface Props {
  operateType: string;
  title: string;
  multi?: boolean;
}
const props = defineProps<Props>();
const emits = defineEmits(['handleQuery']);
const ruleFormRef = ref();

// Khai báo global mockData cho typescript
// eslint-disable-next-line @typescript-eslint/no-explicit-any
declare global {
  interface Window {
    mockData: any[];
  }
}

const state = reactive({
  isShowDialog: false,
  ruleForm: {
    id: undefined,
    week: '',
    date: '',
    timeFrom: '',
    timeTo: '',
    employees: [],
    device: '',
    note: '',
    dates: [],
    shifts: [],
    tasks: [ { name: '', employees: [] } ],
  },
  employeeList: [
    { id: 1, name: '[cuonglv]Lê Văn Cường' },
    { id: 2, name: '[nhattv] Trần Văn Nhật' },
    { id: 3, name: '[tuanha] Hoàng Anh Tuấn' },
  ],
  shiftList: [
    { id: 1, name: 'Ca 1' },
    { id: 2, name: 'Ca 2' },
    { id: 3, name: 'Ca 3' },
  ],
  taskList: [
    'Kiểm tra máy bơm',
    'Bảo dưỡng van',
    'Vệ sinh hệ thống',
    'Kiểm tra điện',
    'Bảo dưỡng thiết bị',
    'Kiểm tra cảm biến',
  ],
});

const openDialog = (row: any) => {
  if (props.multi) {
    state.ruleForm = {
      id: undefined,
      week: row?.week || '',
      dates: [],
      shifts: [],
      employees: [],
      date: '',
      timeFrom: '',
      timeTo: '',
      device: '',
      note: '',
      tasks: [ { name: '', employees: [] } ],
    };
  } else if (row) {
    let timeFrom = '', timeTo = '';
    if (row.time && row.time.includes(' - ')) {
      [timeFrom, timeTo] = row.time.split(' - ');
    }
    let tasks = [];
    if (Array.isArray(row.tasks) && row.tasks.length > 0) {
      if (typeof row.tasks[0] === 'string') {
        tasks = row.tasks.map((t: string) => ({ name: t, employees: [] }));
      } else {
        tasks = row.tasks.map((t: any) => ({ name: t.name || '', employees: Array.isArray(t.employees) ? t.employees.map(e => typeof e === 'object' ? e.id : e) : [] }));
      }
    } else {
      tasks = [ { name: '', employees: [] } ];
    }
    state.ruleForm = {
      ...row,
      timeFrom,
      timeTo,
      tasks,
    };
  } else {
    state.ruleForm = {
      id: undefined,
      week: '',
      date: '',
      timeFrom: '',
      timeTo: '',
      employees: [],
      dates: [],
      shifts: [],
      device: '',
      note: '',
      tasks: [ { name: '', employees: [] } ],
    };
  }
  state.isShowDialog = true;
  ruleFormRef.value?.resetFields();
};

const cancel = () => {
  state.isShowDialog = false;
};

const addTask = () => {
  state.ruleForm.tasks.push({ name: '', employees: [] });
};

const removeTask = (idx: number) => {
  state.ruleForm.tasks.splice(idx, 1);
};

const submit = () => {
  ruleFormRef.value.validate((valid: boolean) => {
    if (!valid) return;
    const time = `${state.ruleForm.timeFrom} - ${state.ruleForm.timeTo}`;
    const newRecord = {
      id: Date.now() + Math.random(),
      week: state.ruleForm.week,
      date: new Date(state.ruleForm.date).toLocaleDateString('vi-VN'),
      time,
      device: state.ruleForm.device,
      note: state.ruleForm.note,
      tasks: state.ruleForm.tasks.map(t => ({ name: t.name, employees: t.employees })),
    };
    if (window.mockData) {
      window.mockData.push(newRecord);
    }
    ElMessage.success($t('lz.message.base.saveSuccess'));
    emits('handleQuery');
    state.isShowDialog = false;
  });
};

defineExpose({ openDialog });
</script>

<style scoped>

:deep(.el-calendar-day){
  height: 140px!important;
}
</style> 