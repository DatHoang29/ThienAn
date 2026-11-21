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
            <el-col :xs="24" class="mb20">
              <el-form-item :label="$t('lz.entity.tms.shift')" prop="shift" :rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.tms.shift') }), trigger: 'change' }]">
                <el-select v-model="state.ruleForm.shift" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.tms.shift') })" filterable clearable class="w100">
                  <el-option v-for="item in state.shiftList" :key="item.id" :label="item.name" :value="item.name" />
                </el-select>
              </el-form-item>
            </el-col>
            <el-col :xs="24" class="mb20">
              <el-form-item :label="$t('lz.entity.tms.employee')" prop="employees" :rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.tms.employee') }), trigger: 'change' }]">
                <el-select v-model="state.ruleForm.employees" multiple filterable clearable class="w100" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.tms.employee') })">
                  <el-option v-for="item in state.employeeList" :key="item.id" :label="item.name" :value="item" />
                </el-select>
              </el-form-item>
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
    shift: '',
    employees: [],
    dates: [],
    shifts: [],
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
      shift: '',
    };
  } else {
    state.ruleForm = row ? { ...row, employees: [...row.employees] } : {
      id: undefined,
      week: '',
      date: '',
      shift: '',
      employees: [],
      dates: [],
      shifts: [],
    };
  }
  state.isShowDialog = true;
  ruleFormRef.value?.resetFields();
};

const cancel = () => {
  state.isShowDialog = false;
};

const submit = () => {
  ruleFormRef.value.validate((valid: boolean) => {
    if (!valid) return;
    if (props.multi) {
      // Đăng ký nhiều ngày, nhiều ca, nhiều nhân viên
      // Tạo bản ghi cho từng ngày, từng ca
      const newRecords = [];
      for (const date of state.ruleForm.dates) {
        const dateObj = new Date(date);
        const dateStr = dateObj.toLocaleDateString('vi-VN');
        for (const shift of state.ruleForm.shifts) {
          newRecords.push({
            id: Date.now() + Math.random(),
            week: state.ruleForm.week,
            date: dateStr,
            shift,
            employees: [...state.ruleForm.employees],
          });
        }
      }
      // Thêm vào mockData toàn cục nếu có (demo)
      if (window.mockData) {
        window.mockData.push(...newRecords);
      }
      ElMessage.success($t('lz.message.base.registerSuccess'));
      emits('handleQuery');
      state.isShowDialog = false;
      return;
    }
    // Đăng ký 1 ngày/ca
    ElMessage.success($t('lz.message.base.saveSuccess'));
    emits('handleQuery');
    state.isShowDialog = false;
  });
};

defineExpose({ openDialog });
</script>

<style scoped>
.tms-lichsu-tuanduong-modal-container {
  /* Style cho modal */
}
</style> 