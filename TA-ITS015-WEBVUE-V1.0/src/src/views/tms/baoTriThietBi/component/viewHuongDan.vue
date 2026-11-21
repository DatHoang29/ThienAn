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
          <el-form-item :label="$t('lz.entity.tms.maintenanceGuide')">
            <el-input v-model="state.ruleForm.maintenanceGuide" type="textarea" :rows="6" disabled />
          </el-form-item>
        </el-col>
        <el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
          <el-form-item :label="$t('lz.label.base.attachments')">
            <div v-if="state.fileList.length > 0">
              <div v-for="file in state.fileList" :key="file.uid" class="file-item">
                <el-link type="primary" @click="handleDownload(file)">{{ file.name }}</el-link>
              </div>
            </div>
            <el-empty v-else :description="$t('lz.label.base.noAttachments')" style="height:300px"/>
          </el-form-item>
        </el-col>
      </el-row>
    </el-form>
    <template #footer>
      <div class="dialog-footer">
        <el-button @click="cancel">{{ $t('lz.button.base.close') }}</el-button>
        <el-button type="primary" @click="handleEdit">{{ $t('lz.button.base.update') }}</el-button>
      </div>
    </template>
  </el-dialog>
</template>

<script lang="ts" setup>
import { useI18n } from 'vue-i18n';


import { reactive, ref } from 'vue';
import type { UploadUserFile } from 'element-plus';

const { t: $t } = useI18n();

interface Props {
  title: string;
  equipment: any;
}

const props = defineProps<Props>();
const emits = defineEmits(['handleQuery', 'handleEdit']);

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
  state.isShowDialog = false;
};

// Cancel
const cancel = () => {
  state.isShowDialog = false;
};

// Handle edit
const handleEdit = () => {
  emits('handleEdit', state.ruleForm);
  closeDialog();
};

// Handle file download
const handleDownload = (file: UploadUserFile) => {
  // TODO: Implement file download logic
  console.log('Download file:', file);
};

// Export object
defineExpose({ openDialog });
</script>

<style scoped>
.mb20 {
  margin-bottom: 20px;
}
.file-item {
  margin-bottom: 8px;
}
</style> 