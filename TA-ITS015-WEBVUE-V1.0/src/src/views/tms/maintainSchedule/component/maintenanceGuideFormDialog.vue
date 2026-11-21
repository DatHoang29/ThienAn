<template>
  <el-dialog
    v-model="visible"
    :title="dialogTitle"
    width="700px"
    :close-on-click-modal="false"
    @close="handleClose"
  >
    <template #header>
      <div style="color: #fff; display: flex; align-items: center;">
        <el-icon size="16" style="margin-right: 5px;">
          <ele-Document />
        </el-icon>
        <span>{{ dialogTitle }}</span>
      </div>
    </template>

    <el-form
      ref="formRef"
      :model="formData"
      :rules="formRules"
      label-width="140px"
      label-position="left"
      :disabled="submitting"
    >
      <el-form-item :label="$t('lz.label.tmsMaintenanceSchedule.guideName')" prop="name">
        <el-input v-model="formData.name" :placeholder="$t('lz.label.tmsMaintenanceSchedule.guideNamePlaceholder')" />
      </el-form-item>

      <el-form-item :label="$t('lz.entity.tms.equipmentType')" prop="equipmentTypeId">
        <el-select
          v-model="formData.equipmentTypeId"
          :placeholder="$t('lz.label.tmsMaintenanceSchedule.selectEquipmentType')"
          filterable
          style="width: 100%;"
        >
          <el-option
            v-for="item in equipmentTypeList"
            :key="item.id"
            :label="item.name"
            :value="item.id"
          />
        </el-select>
      </el-form-item>

      <el-form-item :label="$t('lz.label.tmsMaintenanceSchedule.manufacturer')" prop="manufacturer">
        <el-input v-model="formData.manufacturer" :placeholder="$t('lz.label.tmsMaintenanceSchedule.manufacturerPlaceholder')" />
      </el-form-item>

      <el-form-item :label="$t('lz.entity.base.desc')" prop="description">
        <el-input v-model="formData.description" type="textarea" :rows="3" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.desc') })" />
      </el-form-item>

      <el-row :gutter="20" class="mb20">
        <el-col :span="12">
          <el-form-item :label="$t('lz.label.tmsMaintenanceSchedule.frequency')" prop="frequency">
            <el-select v-model="formData.frequency" style="width: 100%;">
              <el-option
                v-for="item in frequencyOptions"
                :key="item.value"
                :label="item.label"
                :value="item.value"
              />
            </el-select>
          </el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item :label="$t('lz.label.tmsMaintenanceSchedule.frequencyValue')" prop="frequencyValue">
            <el-input-number v-model="formData.frequencyValue" :min="1" :max="365" style="width: 100%;" />
          </el-form-item>
        </el-col>
      </el-row>

      <el-row :gutter="20" class="mb20">
        <el-col :span="12">
          <el-form-item :label="$t('lz.label.tmsMaintenanceSchedule.estimatedDuration')" prop="estimatedDuration">
            <el-input-number v-model="formData.estimatedDuration" :min="1" :max="9999" style="width: 100%;">
              <template #suffix>{{ $t('lz.label.tmsMaintenanceSchedule.minutes') }}</template>
            </el-input-number>
          </el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item :label="$t('lz.entity.base.priority')" prop="priority">
            <el-select v-model="formData.priority" style="width: 100%;">
              <el-option
                v-for="item in priorityOptions"
                :key="item.value"
                :label="item.label"
                :value="item.value"
              />
            </el-select>
          </el-form-item>
        </el-col>
      </el-row>
      <!-- Checklist Builder -->
      <el-form-item :label="$t('lz.label.tmsMaintenanceSchedule.checklist')" prop="checklistItems">
        <div class="checklist-builder">
          <div v-for="(item, index) in formData.checklistItems" :key="item.uid" class="checklist-item">
            <el-input
              v-model="item.task"
              :placeholder="$t('lz.label.tmsMaintenanceSchedule.checklistItemTask')"
              style="flex: 1;"
            />
            <el-checkbox v-model="item.required">{{ $t('lz.label.tmsMaintenanceSchedule.required') }}</el-checkbox>
            <el-button type="danger" link @click="removeChecklistItem(index)">
              <el-icon><ele-Delete /></el-icon>
            </el-button>
          </div>
          <el-button type="primary" link @click="addChecklistItem">
            <el-icon><ele-Plus /></el-icon>
            {{ $t('lz.label.tmsMaintenanceSchedule.addChecklistItem') }}
          </el-button>
        </div>
      </el-form-item>

      <!-- Documents Upload -->
      <el-form-item :label="$t('lz.label.tmsMaintenanceSchedule.relatedDocuments')" prop="documents">
        <div class="documents-uploader">
          <el-upload
            ref="uploadRef"
            v-model:file-list="formData.fileList"
            :auto-upload="false"
            :on-remove="handleFileRemove"
            :on-preview="handleFilePreview"
            :on-change="handleFileChange"
            multiple
            :limit="10"
            :on-exceed="handleExceed"
            accept=".pdf,.doc,.docx,.xls,.xlsx,.ppt,.pptx,.txt,.jpg,.jpeg,.png,.gif"
          >
            <el-button type="primary" plain size="small">
              <el-icon><ele-Upload /></el-icon>
              {{ $t('lz.label.tmsMaintenanceSchedule.uploadDocument') }}
            </el-button>
            <template #tip>
              <div class="el-upload__tip">
                {{ $t('lz.label.tmsMaintenanceSchedule.uploadDocumentTip') }}
              </div>
            </template>
          </el-upload>
        </div>
      </el-form-item>

      <el-form-item :label="$t('lz.entity.base.status')" prop="status">
        <el-switch
          v-model="formData.status"
          :active-value="1"
          :inactive-value="0"
          :active-text="$t('lz.label.base.active')"
          :inactive-text="$t('lz.label.base.inactive')"
        />
      </el-form-item>

      <el-form-item :label="$t('lz.entity.base.remark')" prop="remark">
        <el-input v-model="formData.remark" type="textarea" :rows="2" :placeholder="$t('lz.label.tmsDutySchedule.remarkPlaceholder')" />
      </el-form-item>
    </el-form>

    <template #footer>
      <el-button @click="handleClose" :disabled="submitting">
        {{ $t('lz.button.base.cancel') }}
      </el-button>
      <el-button type="primary" @click="handleSubmit" :loading="submitting">
        {{ $t('lz.button.base.save') }}
      </el-button>
    </template>
  </el-dialog>
</template>

<script lang="ts" setup>
import { reactive, ref, computed, watch } from 'vue';
import { ElMessage, type FormInstance, type FormRules, type UploadUserFile, type UploadFile } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { getAPI } from '/@/utils/axios-utils';
import { TmsMaintenanceGuideApi } from '/@/api-services/tms';
import type { AddMaintenanceGuideInput, UpdateMaintenanceGuideInput, ChecklistItem, GuideDocumentInfo } from '/@/api-services/tms/models';
import { BaseConfigTypeEnum } from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';

const { t: $t } = useI18n();
const baseConfigStore = useBaseConfig();
const { getConfigDataByCode } = baseConfigStore;

// Props
const props = defineProps<{
  modelValue: boolean;
  operateType: 'add' | 'edit';
  initialData?: {
    id?: string;
    equipmentTypeId?: string;
    manufacturer?: string;
    name?: string;
    description?: string;
    frequency?: string;
    frequencyValue?: number;
    estimatedDuration?: number;
    priority?: string;
    status?: number;
    remark?: string;
    checklistItems?: { order: number; task: string; required: boolean }[];
    documents?: { id?: string; fileName: string; url: string; size?: number }[];
  };
  equipmentTypeList: any[];
}>();

// Emits
const emit = defineEmits<{
  (e: 'update:modelValue', value: boolean): void;
  (e: 'success'): void;
}>();

// Refs
const formRef = ref<FormInstance>();
const uploadRef = ref();
const submitting = ref(false);
const pendingFiles = ref<File[]>([]); // Files waiting to upload after guide is created

// Computed
const visible = computed({
  get: () => props.modelValue,
  set: (val) => emit('update:modelValue', val),
});

const dialogTitle = computed(() => {
  return props.operateType === 'add' 
    ? $t('lz.label.tmsMaintenanceSchedule.addGuide')
    : $t('lz.label.tmsMaintenanceSchedule.editGuide');
});

// Form data
const formData = reactive({
  id: '',
  equipmentTypeId: '',
  manufacturer: '',
  name: '',
  description: '',
  frequency: 'monthly',
  frequencyValue: 1,
  estimatedDuration: 60,
  priority: 'medium',
  status: 1,
  remark: '',
  checklistItems: [] as { uid: string; task: string; required: boolean; note?: string }[],
  fileList: [] as UploadUserFile[],
  documents: [] as GuideDocumentInfo[],
});

const createUid = () => {
  try {
    // crypto.randomUUID is supported in modern browsers
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    const cryptoAny: any = crypto;
    if (cryptoAny?.randomUUID) return cryptoAny.randomUUID();
  } catch { }
  return `${Date.now().toString(36)}_${Math.random().toString(36).slice(2)}`;
};

type ChecklistLike = {
  // support both camelCase (API response) and PascalCase (stored JSON string)
  task?: string;
  Task?: string;
  required?: boolean;
  Required?: boolean;
  note?: string;
  Note?: string;
};

const normalizeChecklistItem = (item: ChecklistLike | null | undefined) => {
  const task = (item?.task ?? item?.Task ?? '') as string;
  const required = (item?.required ?? item?.Required ?? false) as boolean;
  const note = (item?.note ?? item?.Note ?? undefined) as string | undefined;
  return {
    task: typeof task === 'string' ? task : '',
    required: !!required,
    note: typeof note === 'string' ? note : undefined,
  };
};

const normalizeChecklistSource = (data: unknown): ChecklistLike[] => {
  if (!data || typeof data !== 'object') return [];

  // Preferred: already-parsed array passed in as `checklistItems`
  const anyData = data as any;
  const direct = anyData.checklistItems;
  if (Array.isArray(direct)) return direct as ChecklistLike[];

  // Fallbacks: sometimes API returns checklist as JSON string (or already-parsed array) in `checklist`
  const checklist = anyData.checklist;
  if (!checklist) return [];

  try {
    const parsed = typeof checklist === 'string' ? JSON.parse(checklist) : checklist;
    return Array.isArray(parsed) ? (parsed as ChecklistLike[]) : [];
  } catch (e) {
    console.warn('[MaintenanceGuideFormDialog] Failed to parse checklist', { checklist, e });
    return [];
  }
};

// Watch for initial data changes
watch(() => props.initialData, (newData) => {
  if (newData && props.modelValue) {
    loadInitialData(newData);
  }
}, { deep: true });

// Watch for dialog open to reset form if adding or reload data for editing
watch(() => props.modelValue, (isVisible) => {
  if (isVisible) {
    if (props.operateType === 'add' && !props.initialData?.equipmentTypeId) {
      resetForm();
    } else if (props.initialData) {
      // Force reload form data when opening for edit
      loadInitialData(props.initialData);
    }

    // Console Ninja-friendly debug
    try {
      console.log('[MaintenanceGuideFormDialog] open', {
        operateType: props.operateType,
        guideId: props.initialData?.id,
        checklistItemsLen: (props.initialData as any)?.checklistItems?.length,
        hasChecklistString: !!(props.initialData as any)?.checklist,
      });
    } catch {
      // ignore
    }
  }
});

// Options
const normalizeFrequencyKey = (frequency?: string | null) => (frequency || '').toString();

const normalizePriorityKey = (priority?: string | null) => (priority || '').toString();

const frequencyOptions = computed(() => {
  const cfg = getConfigDataByCode(BaseConfigTypeEnum.TmsMaintenanceFrequency) || [];
  return cfg.map(i => ({ value: i.code, label: i.name }));
});

const priorityOptions = computed(() => {
  const cfg = getConfigDataByCode(BaseConfigTypeEnum.TmsMaintenancePriority) || [];
  return cfg.map(i => ({ value: i.code, label: i.name }));
});

// Form rules
const formRules = computed<FormRules>(() => ({
  name: [{ required: true, message: $t('lz.message.tmsMaintenanceSchedule.pleaseInputGuideName'), trigger: 'blur' }],
  equipmentTypeId: [{ required: true, message: $t('lz.message.tmsMaintenanceSchedule.pleaseSelectEquipmentType'), trigger: 'change' }],
  frequency: [{ required: true, message: $t('lz.message.tmsMaintenanceSchedule.pleaseSelectFrequency'), trigger: 'change' }],
}));

// Methods
const resetForm = () => {
  formData.id = '';
  formData.equipmentTypeId = props.initialData?.equipmentTypeId || '';
  formData.manufacturer = '';
  formData.name = '';
  formData.description = '';
  formData.frequency = 'monthly';
  formData.frequencyValue = 1;
  formData.estimatedDuration = 60;
  formData.priority = 'medium';
  formData.status = 1;
  formData.remark = '';
  formData.checklistItems.splice(0, formData.checklistItems.length);
  formData.fileList.splice(0, formData.fileList.length);
  formData.documents.splice(0, formData.documents.length);
  pendingFiles.value = [];
};

// Load initial data into form
const loadInitialData = (data: typeof props.initialData) => {
  if (!data) return;
  
  formData.id = data.id || '';
  formData.equipmentTypeId = data.equipmentTypeId || '';
  formData.manufacturer = data.manufacturer || '';
  formData.name = data.name || '';
  formData.description = data.description || '';
  formData.frequency = normalizeFrequencyKey(data.frequency) || 'monthly';
  formData.frequencyValue = data.frequencyValue || 1;
  formData.estimatedDuration = data.estimatedDuration || 60;
  formData.priority = normalizePriorityKey(data.priority) || 'medium';
  formData.status = data.status ?? 1;
  formData.remark = data.remark || '';
  
  // Clear and re-populate checklist items (reactive arrays need splice + push)
  formData.checklistItems.splice(0, formData.checklistItems.length);
  const normalizedChecklist = normalizeChecklistSource(data);
  if (normalizedChecklist.length > 0) {
    normalizedChecklist.forEach((item, index) => {
      const normalized = normalizeChecklistItem(item);
      formData.checklistItems.push({
        uid: createUid(),
        task: normalized.task,
        required: normalized.required,
        note: normalized.note,
      });
    });
  }

  // Console Ninja-friendly debug
  try {
    console.log('[MaintenanceGuideFormDialog] loadInitialData checklist', {
      guideId: data.id,
      rawChecklistItems: (data as any)?.checklistItems,
      rawChecklist: (data as any)?.checklist,
      normalizedChecklist,
      formChecklistItems: formData.checklistItems,
    });
  } catch {
    // ignore
  }
  
  // Clear and re-populate documents (reactive arrays)
  formData.documents.splice(0, formData.documents.length);
  formData.fileList.splice(0, formData.fileList.length);
  
  if (data.documents && data.documents.length > 0) {
    data.documents.forEach((doc: any) => {
      formData.documents.push(doc as GuideDocumentInfo);
      formData.fileList.push({
        name: doc.fileName || doc.name,
        url: doc.filePath || doc.url,
        uid: doc.filePath || doc.fileName,
      } as UploadUserFile);
    });
  }
  
  // Clear pending files when loading existing data
  pendingFiles.value = [];
};

// Upload handlers - Manual upload using TMS API
const handleFileChange = (file: UploadFile) => {
  // Validate file size
  if (file.raw && file.raw.size / 1024 / 1024 > 10) {
    ElMessage.error($t('lz.message.base.fileTooLarge'));
    // Remove from fileList
    const index = formData.fileList.findIndex(f => f.uid === file.uid);
    if (index > -1) {
      formData.fileList.splice(index, 1);
    }
    return;
  }
  
  // Store raw file for later upload
  if (file.raw) {
    pendingFiles.value.push(file.raw);
  }
};

// Upload documents to a guide using FormData (IFormFile)
const uploadDocumentsToGuide = async (guideId: string): Promise<void> => {
  for (const file of pendingFiles.value) {
    try {
      // Swagger client exposes multipart upload as ...PostForm(maintenanceGuideId, file)
      const response = await getAPI(TmsMaintenanceGuideApi)
        .apiTmsTmsmaintenanceguideUploaddocumentPostForm(guideId, file);

      if (response.data?.result) {
        formData.documents.push(response.data.result as GuideDocumentInfo);
      }
    } catch (error) {
      console.error('Error uploading document:', error);
      ElMessage.error($t('lz.message.base.uploadFail') + `: ${file.name}`);
    }
  }
  pendingFiles.value = [];
};

const handleFileRemove = (file: UploadFile) => {
  // Remove from pending files (new files not yet uploaded)
  const pendingIndex = pendingFiles.value.findIndex(f => f.name === file.name);
  if (pendingIndex > -1) {
    pendingFiles.value.splice(pendingIndex, 1);
  }
  
  // Remove from documents array (existing uploaded files)
  const docIndex = formData.documents.findIndex(
    doc => doc.fileName === file.name || doc.filePath === file.url
  );
  if (docIndex > -1) {
    formData.documents.splice(docIndex, 1);
  }
};

const handleFilePreview = (file: UploadFile) => {
  const fileUrl = file.url || formData.documents.find(d => d.fileName === file.name)?.filePath;
  if (fileUrl) {
    window.open(fileUrl, '_blank');
  }
};

const handleExceed = () => {
  ElMessage.warning($t('lz.message.tmsMaintenanceSchedule.maxFilesExceeded'));
};

const addChecklistItem = () => {
  formData.checklistItems.push({
    uid: createUid(),
    task: '',
    required: false,
    note: undefined,
  });
};

const removeChecklistItem = (index: number) => {
  formData.checklistItems.splice(index, 1);
};

const handleClose = () => {
  visible.value = false;
  formRef.value?.resetFields();
};

const handleSubmit = async () => {
  if (!formRef.value) return;

  try {
    await formRef.value.validate();
    submitting.value = true;

    const checklist: ChecklistItem[] = formData.checklistItems
      .filter(item => item.task.trim())
      .map((item) => ({
        // Backend now stores only Task/Required/Note; extra fields (like order) are not needed.
        task: item.task,
        required: item.required,
        note: item.note,
      })) as any;

    if (props.operateType === 'add') {
      // Note: For new guides, files will need to be uploaded after editing the guide
      // because the Add API doesn't return the guide ID
      if (pendingFiles.value.length > 0) {
        ElMessage.warning($t('lz.message.tmsMaintenanceSchedule.uploadAfterCreate'));
      }
      
      const input: AddMaintenanceGuideInput = {
        equipmentTypeId: formData.equipmentTypeId,
        manufacturer: formData.manufacturer || undefined,
        name: formData.name,
        description: formData.description || undefined,
        frequency: formData.frequency,
        frequencyValue: formData.frequencyValue,
        estimatedDuration: formData.estimatedDuration,
        priority: formData.priority,
        status: formData.status,
        remark: formData.remark || undefined,
        checklist: checklist.length > 0 ? checklist : undefined,
      };
      await getAPI(TmsMaintenanceGuideApi).apiTmsTmsmaintenanceguideAddPost(input);
      ElMessage.success($t('lz.message.base.createSuccess'));
    } else {
      // For edit mode, upload new files first
      if (pendingFiles.value.length > 0) {
        await uploadDocumentsToGuide(formData.id);
      }
      
      // Prepare documents JSON string from existing + new uploads
      const documentsJson = formData.documents.length > 0 
        ? JSON.stringify(formData.documents) 
        : undefined;

      const input: UpdateMaintenanceGuideInput = {
        id: formData.id,
        equipmentTypeId: formData.equipmentTypeId,
        manufacturer: formData.manufacturer || undefined,
        name: formData.name,
        description: formData.description || undefined,
        frequency: formData.frequency,
        frequencyValue: formData.frequencyValue,
        estimatedDuration: formData.estimatedDuration,
        priority: formData.priority,
        status: formData.status,
        remark: formData.remark || undefined,
        checklist: checklist.length > 0 ? checklist : undefined,
        documents: documentsJson,
      };
      await getAPI(TmsMaintenanceGuideApi).apiTmsTmsmaintenanceguideUpdatePost(input);
      ElMessage.success($t('lz.message.base.updateSuccess'));
    }

    handleClose();
    emit('success');
  } catch (error: any) {
    if (error !== false) {
      console.error('Error saving guide:', error);
      ElMessage.error(error?.message || $t('lz.message.base.acctionFail'));
    }
  } finally {
    submitting.value = false;
  }
};
</script>

<style lang="scss" scoped>
.checklist-builder {
  width: 100%;
  
  .checklist-item {
    display: flex;
    align-items: center;
    gap: 10px;
    margin-bottom: 8px;
    padding: 8px;
    background: var(--el-fill-color-light);
    border-radius: 4px;
  }
}

.documents-uploader {
  width: 100%;
  
  .el-upload__tip {
    font-size: 12px;
    color: var(--el-text-color-secondary);
    margin-top: 5px;
  }
}
</style>
