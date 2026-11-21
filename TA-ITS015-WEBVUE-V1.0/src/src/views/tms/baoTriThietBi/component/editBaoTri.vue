<template>
  <div class="tms-lichsu-tuanduong-modal-container">
    <el-dialog v-model="state.isShowDialog" draggable :close-on-click-modal="false" width="800px" append-to-body destroy-on-close>
      <template #header>
        <div>
          <el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle">
            <ele-Edit />
          </el-icon>
          <span>{{ props.title }}</span>
        </div>
      </template>
      <el-form :model="state.ruleForm" ref="ruleFormRef" label-width="120px">
        <el-row :gutter="10">
          <el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
            <el-form-item :label="$t('lz.entity.tms.equipmentType')" prop="type">
              <el-select v-model="state.ruleForm.type" filterable clearable class="w100" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.tms.equipmentType') })" @change="handleTypeChange">
                <el-option v-for="item in state.typeList" :key="item.id" :label="item.name" :value="item.id" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
            <el-form-item :label="$t('lz.entity.tms.equipment')" prop="equipmentId">
              <el-select v-model="state.ruleForm.equipmentId" filterable clearable class="w100" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.tms.equipment') })">
                <el-option v-for="item in state.equipmentList" :key="item.id" :label="item.name" :value="item.id" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
            <el-form-item :label="$t('lz.entity.tms.equipmentCode')" prop="code">
              <el-input v-model="state.ruleForm.code" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.tms.equipmentCode') })" />
            </el-form-item>
          </el-col>
          <el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
            <el-form-item :label="$t('lz.entity.tms.maintenanceFrequency')" prop="maintenanceFrequency">
              <el-select v-model="state.ruleForm.maintenanceFrequency" filterable clearable class="w100" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.tms.maintenanceFrequency') })">
                <el-option v-for="item in state.frequencyList" :key="item.value" :label="item.label" :value="item.value" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
            <el-form-item :label="$t('lz.entity.tms.maintenanceDuration')" prop="maintenanceTime">
              <el-input-number v-model="state.ruleForm.maintenanceTime" :min="1" :max="24" class="w100" :placeholder="$t('lz.label.tms.maintenanceDurationPlaceholder')" />
            </el-form-item>
          </el-col>
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


import { reactive, ref, watch } from 'vue';
import { ElMessage } from 'element-plus';

const { t: $t } = useI18n();

interface Props {
  title: string;
  operateType: string;
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
    type: '',
    equipmentId: '',
    code: '',
    maintenanceFrequency: '',
    maintenanceTime: 1,
  },
  typeList: [
    { id: 1, name: 'Máy bơm' },
    { id: 2, name: 'Hệ thống lọc' },
    { id: 3, name: 'Tủ điện' },
    { id: 4, name: 'Van chính' },
    { id: 5, name: 'Máy bơm 2' },
    { id: 6, name: 'Hệ thống lọc 2' },
    { id: 7, name: 'Tủ điện 2' },
    { id: 8, name: 'Van phụ' },
  ],
  equipmentList: [] as any[],
  frequencyList: [
    { value: 'daily', label: $t('lz.label.base.daily') },
    { value: 'weekly', label: $t('lz.label.base.weekly') },
    { value: 'monthly', label: $t('lz.label.base.monthly') },
    { value: 'quarterly', label: $t('lz.label.base.quarterly') },
    { value: 'yearly', label: $t('lz.label.base.yearly') },
  ],
});

const handleTypeChange = (value: number | string) => {
  const typeId = typeof value === 'string' ? parseInt(value) : value;
  if (typeId) {
    // Filter equipment list based on selected type
    state.equipmentList = [
      { id: 1, name: 'Máy bơm chính', type: 1 },
      { id: 2, name: 'Máy bơm dự phòng', type: 1 },
      { id: 3, name: 'Hệ thống lọc nước', type: 2 },
      { id: 4, name: 'Hệ thống lọc nước phụ', type: 2 },
      { id: 5, name: 'Tủ điện điều khiển', type: 3 },
      { id: 6, name: 'Tủ điện phụ', type: 3 },
      { id: 7, name: 'Van điều khiển chính', type: 4 },
      { id: 8, name: 'Van điều khiển phụ', type: 4 },
    ].filter(eq => eq.type === typeId);
  } else {
    state.equipmentList = [];
  }
  state.ruleForm.equipmentId = '';
};

// Watch for type changes to update equipment list
watch(() => state.ruleForm.type, (newType) => {
  handleTypeChange(newType);
});

const openDialog = (row: any) => {
  state.ruleForm = {
    type: row.type || '',
    equipmentId: row.id || '',
    code: row.code || '',
    maintenanceFrequency: row.maintenanceFrequency || '',
    maintenanceTime: row.maintenanceTime || 1,
  };
  state.isShowDialog = true;
  ruleFormRef.value?.resetFields();
};

const cancel = () => {
  state.isShowDialog = false;
};

const submit = () => {
  ruleFormRef.value.validate(async (valid: boolean) => {
    if (!valid) return;
    // TODO: Call API to save equipment info
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
