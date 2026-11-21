<template>
  <el-dialog v-model="state.isShowDialog" :title="title" width="1000px" append-to-body destroy-on-close>
    <div class="mb20">
      <el-button type="primary" @click="handleAdd">{{ $t('lz.button.tms.addEquipment') }}</el-button>
    </div>
    <vxe-grid ref="xGrid" class="xGrid-style custom-grid" v-bind="options">
      <template #empty>
        <el-empty :image-size="200" />
      </template>
    </vxe-grid>

    <!-- Dialog thêm/sửa thiết bị -->
    <el-dialog v-model="state.isShowEditDialog" :title="state.editTitle" width="600px" append-to-body>
      <el-form :model="state.ruleForm" ref="ruleFormRef" label-width="120px">
        <el-row :gutter="10">
          <el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
            <el-form-item :label="$t('lz.entity.tms.equipmentCode')" prop="code" :rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.tms.equipmentCode') }), trigger: 'blur' }]">
              <el-input v-model="state.ruleForm.code" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.tms.equipmentCode') })" />
            </el-form-item>
          </el-col>
          <el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
            <el-form-item :label="$t('lz.entity.tms.equipmentName')" prop="name" :rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.tms.equipmentName') }), trigger: 'blur' }]">
              <el-input v-model="state.ruleForm.name" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.tms.equipmentName') })" />
            </el-form-item>
          </el-col>
          <el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
            <el-form-item :label="$t('lz.entity.tms.installationLocation')" prop="location" :rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.tms.installationLocation') }), trigger: 'blur' }]">
              <el-input v-model="state.ruleForm.location" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.tms.installationLocation') })" />
            </el-form-item>
          </el-col>
          <el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
            <el-form-item :label="$t('lz.entity.tms.maintenanceDate')" prop="maintenanceDate" :rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.tms.maintenanceDate') }), trigger: 'change' }]">
              <el-date-picker v-model="state.ruleForm.maintenanceDate" type="date" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.tms.maintenanceDate') })" class="w100" />
            </el-form-item>
          </el-col>
          <el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
            <el-form-item :label="$t('lz.label.base.notification')" prop="notification">
              <el-input v-model="state.ruleForm.notification" type="textarea" :rows="3" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.label.base.notification') })" />
            </el-form-item>
          </el-col>
        </el-row>
      </el-form>
      <template #footer>
        <div class="dialog-footer">
          <el-button @click="cancelEdit">{{ $t('lz.button.base.cancel') }}</el-button>
          <el-button type="primary" @click="submitEdit">{{ $t('lz.button.base.save') }}</el-button>
        </div>
      </template>
    </el-dialog>
  </el-dialog>
</template>

<script lang="ts" setup>
import { useI18n } from 'vue-i18n';


import { reactive, ref } from 'vue';
import { VxeGridInstance, VxeGridPropTypes } from 'vxe-table';
import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';
import { ElMessage } from 'element-plus';

const { t: $t } = useI18n();

interface Props {
  title: string;
  equipmentType: any;
}

const props = defineProps<Props>();
const emits = defineEmits(['handleQuery']);

const xGrid = ref<VxeGridInstance>();
const ruleFormRef = ref();

interface Equipment {
  id?: number;
  code: string;
  name: string;
  type: string;
  typeName: string;
  location: string;
  maintenanceDate: string;
  notification: string;
  addedTime: string;
}

const state = reactive({
  isShowDialog: false,
  isShowEditDialog: false,
  editTitle: '',
  editType: 'add',
  ruleForm: {
    id: undefined,
    code: '',
    name: '',
    type: '',
    typeName: '',
    location: '',
    maintenanceDate: '',
    notification: '',
    addedTime: '',
  } as Equipment,
  data: [
    {
      id: 1,
      code: 'CCTV001',
      name: 'CCTV001',
      type: 'CCTV',
      typeName: 'Camera giám sát',
      location: 'KM100+000',
      maintenanceDate: '2024-06-01',
      notification: 'Đang hoạt động tốt',
      addedTime: '2024-05-01 08:00:00'
    },
    {
      id: 2,
      code: 'CCTV006',
      name: 'CCTV006',
      type: 'CCTV',
      typeName: 'Camera giám sát',
      location: 'KM102+000',
      maintenanceDate: '2024-06-02',
      notification: 'Thiết bị không hoạt động',
      addedTime: '2024-05-02 09:00:00'
    },
    {
      id: 3,
      code: 'CCTV011',
      name: 'CCTV011',
      type: 'CCTV',
      typeName: 'Camera giám sát',
      location: 'KM104+000',
      maintenanceDate: '2024-06-03',
      notification: 'Báo lỗi: Fault',
      addedTime: '2024-05-03 10:00:00'
    },
    {
      id: 4,
      code: 'CCTV007',
      name: 'CCTV007',
      type: 'CCTV',
      typeName: 'Camera giám sát',
      location: 'KM102+000',
      maintenanceDate: '2024-06-04',
      notification: 'Đang hoạt động',
      addedTime: '2024-05-04 11:00:00'
    },
    {
      id: 5,
      code: 'CCTV016',
      name: 'CCTV016',
      type: 'CCTV',
      typeName: 'Camera giám sát',
      location: 'KM106+000',
      maintenanceDate: '2024-06-05',
      notification: 'Không thể kết nối',
      addedTime: '2024-05-05 12:00:00'
    }
  ] as Equipment[],
});

const options = useVxeTable<any>(
  {
    id: 'danhSachThietBi',
    name: 'DanhSachThietBi',
    columns: [
      { field: 'code', title: $t('lz.entity.tms.equipmentCode'), minWidth: 120, showOverflow: 'tooltip', sortable: true },
      { field: 'name', title: $t('lz.entity.tms.equipmentName'), minWidth: 200, showOverflow: 'tooltip', sortable: true },
      { field: 'typeName', title: $t('lz.entity.tms.equipmentType'), minWidth: 180, showOverflow: 'tooltip' },
      { field: 'location', title: $t('lz.entity.tms.installationLocation'), minWidth: 200, showOverflow: 'tooltip' },
      { field: 'addedTime', title: $t('lz.entity.base.createTime'), minWidth: 150, showOverflow: 'tooltip', sortable: true },
      { field: 'maintenanceDate', title: $t('lz.entity.tms.maintenanceDate'), minWidth: 150, showOverflow: 'tooltip', sortable: true },
      { field: 'notification', title: $t('lz.label.base.notification'), minWidth: 200, showOverflow: 'tooltip' },
      { title: $t('lz.label.base.action'), fixed: 'right', width: 120, showOverflow: true, slots: { default: 'row_buttons' } },
    ],
  },
  {
    proxyConfig: { autoLoad: true, ajax: { query: ({ page, sort }) => handleQueryApi(page, sort) } },
    toolbarConfig: { export: false },
    rowConfig: { height: 40 },
    height: '450px'
  }
);

// Open dialog
const openDialog = (equipmentType: any) => {
  state.isShowDialog = true;
  handleQuery();
};

// Close dialog
const closeDialog = () => {
  state.isShowDialog = false;
};

// Handle query
const handleQuery = async () => {
  await xGrid.value?.commitProxy('reload') ;
};

// Handle query API
const handleQueryApi = async (page: VxeGridPropTypes.ProxyAjaxQueryPageParams, sort: VxeGridPropTypes.ProxyAjaxQuerySortCheckedParams) => {
  // TODO: Replace with actual API call
  return { data: { result: { records: state.data, total: state.data.length } } };
};

// Handle add
const handleAdd = () => {
  state.editTitle = $t('lz.button.tms.addEquipment');
  state.editType = 'add';
  state.ruleForm = {
    id: undefined,
    code: '',
    name: '',
    type: '',
    typeName: '',
    location: '',
    maintenanceDate: '',
    notification: '',
    addedTime: new Date().toLocaleString('vi-VN'),
  };
  state.isShowEditDialog = true;
};

// Handle edit
const handleEdit = (row: any) => {
  state.editTitle = $t('lz.label.tms.editEquipment');
  state.editType = 'edit';
  state.ruleForm = { ...row };
  state.isShowEditDialog = true;
};

// Handle delete
const handleDelete = (row: any) => {
  const idx = state.data.findIndex(item => item.id === row.id);
  if (idx !== -1) {
    state.data.splice(idx, 1);
    handleQuery();
    ElMessage.success($t('lz.message.base.deleteSuccess'));
  }
};

// Cancel edit
const cancelEdit = () => {
  state.isShowEditDialog = false;
};

// Submit edit
const submitEdit = () => {
  ruleFormRef.value.validate(async (valid: boolean) => {
    if (!valid) return;
    
    if (state.editType === 'add') {
      const newId = Date.now();
      state.ruleForm.id = newId;
      state.data.push({ ...state.ruleForm });
    } else {
      const idx = state.data.findIndex(item => item.id === state.ruleForm.id);
      if (idx !== -1) {
        state.data[idx] = { ...state.ruleForm };
      }
    }
    
    ElMessage.success($t('lz.message.base.saveSuccess'));
    state.isShowEditDialog = false;
    handleQuery();
  });
};

// Export object
defineExpose({ openDialog });
</script>

<style scoped>
.mb20 {
  margin-bottom: 20px;
}
</style> 