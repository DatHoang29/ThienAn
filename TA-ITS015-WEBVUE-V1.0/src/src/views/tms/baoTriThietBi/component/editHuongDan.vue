<template>
  <el-dialog v-model="state.isShowDialog" :title="title" width="800px" append-to-body destroy-on-close>
    <el-form :model="state.ruleForm" ref="ruleFormRef" label-width="120px">
      <el-row :gutter="10">
        <el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
          <el-form-item :label="$t('lz.entity.tms.equipmentName')">
            <el-input v-model="state.ruleForm.name" disabled />
          </el-form-item>
        </el-col>
        <el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
          <el-form-item :label="$t('lz.entity.tms.equipmentCode')">
            <el-input v-model="state.ruleForm.code" disabled />
          </el-form-item>
        </el-col>
        <el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
          <el-form-item :label="$t('lz.entity.tms.maintenanceFrequency')">
            <el-input v-model="state.ruleForm.maintenanceFrequency" disabled />
          </el-form-item>
        </el-col>
        <el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
          <el-form-item :label="$t('lz.entity.tms.maintenanceDuration')">
            <el-input v-model="state.ruleForm.maintenanceTime" disabled />
          </el-form-item>
        </el-col>
        <el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
          <el-form-item :label="$t('lz.entity.tms.maintenanceGuide')" prop="maintenanceGuide">
            <el-input v-model="state.ruleForm.maintenanceGuide" type="textarea" :rows="6" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.tms.maintenanceGuide') })" />
          </el-form-item>
        </el-col>
        <el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
          <el-form-item :label="$t('lz.label.base.attachments')">
            <el-upload
              v-model:file-list="state.fileList"
              action="#"
              :auto-upload="false"
              :on-preview="handlePreview"
              :on-remove="handleRemove"
              :on-exceed="handleExceed"
              :before-remove="beforeRemove"
              multiple
              :limit="5"
            >
              <el-button type="primary">{{ $t('lz.button.base.selectFiles') }}</el-button>
              <template #tip>
                <div class="el-upload__tip">
                  {{ $t('lz.message.tmsMaintenanceGuide.documentUploadHint') }}
                </div>
              </template>
            </el-upload>
          </el-form-item>
        </el-col>
      </el-row>
    </el-form>
    <template #footer>
      <div class="dialog-footer">
        <el-button @click="cancel">{{ $t('lz.button.base.cancel') }}</el-button>
        <el-button type="primary" @click="submit">{{ $t('lz.button.base.update') }}</el-button>
      </div>
    </template>
  </el-dialog>
</template>

<script lang="ts" setup>
import { useI18n } from 'vue-i18n';


import { reactive, ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import type { UploadProps, UploadUserFile } from 'element-plus';

const { t: $t } = useI18n();

interface Props {
  title: string;
  equipment: any;
}

const props = defineProps<Props>();
const emits = defineEmits(['handleQuery']);

const ruleFormRef = ref();
const state = reactive({
  isShowDialog: false,
  ruleForm: {
    name: '',
    code: '',
    maintenanceFrequency: '',
    maintenanceTime: '',
    maintenanceGuide: '',
  },
  fileList: [] as UploadUserFile[],
});

// Open dialog
const openDialog = (row: any) => {
  state.ruleForm = {
    name: row.name || '',
    code: row.code || '',
    maintenanceFrequency: row.maintenanceFrequency || '',
    maintenanceTime: row.maintenanceTime || '',
    maintenanceGuide: row.maintenanceGuide || '',
  };
  state.fileList = row.attachments || [];
  state.isShowDialog = true;
};

// Close dialog
const closeDialog = () => {
  emits('handleQuery');
  state.isShowDialog = false;
};

// Cancel
const cancel = () => {
  state.isShowDialog = false;
};

// Submit
const submit = () => {
  ruleFormRef.value.validate(async (valid: boolean) => {
    if (!valid) return;
    // TODO: Call API to save maintenance guide
    ElMessage.success($t('lz.message.base.updateSuccess'));
    closeDialog();
  });
};

// File handling
const handlePreview: UploadProps['onPreview'] = (file) => {
  console.log(file);
};

const handleRemove: UploadProps['onRemove'] = (file, uploadFiles) => {
  console.log(file, uploadFiles);
};

const handleExceed: UploadProps['onExceed'] = (files, uploadFiles) => {
  ElMessage.warning(
    `Chỉ được tải lên tối đa 5 file, ${files.length} file đã được chọn, ${uploadFiles.length} file đã tải lên`
  );
};

const beforeRemove: UploadProps['beforeRemove'] = (uploadFile, uploadFiles) => {
  return ElMessageBox.confirm(
    `Xác nhận xóa file ${uploadFile.name}?`
  ).then(
    () => true,
    () => false
  );
};

// Export object
defineExpose({ openDialog });
</script>

<style scoped>
.mb20 {
  margin-bottom: 20px;
}
</style> 