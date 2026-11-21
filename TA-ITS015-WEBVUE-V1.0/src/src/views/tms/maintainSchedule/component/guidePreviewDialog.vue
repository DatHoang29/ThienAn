<template>
  <el-dialog
    v-model="visible"
    :title="$t('lz.label.tmsMaintenanceSchedule.guidePreview')"
    width="750px"
    :close-on-click-modal="true"
    class="guide-preview-dialog"
  >
    <template #header>
      <div style="color: #fff; display: flex; align-items: center;">
        <el-icon size="18" style="margin-right: 8px;"><ele-Document /></el-icon>
        <span>{{ $t('lz.label.tmsMaintenanceSchedule.guidePreview') }}</span>
      </div>
    </template>
    
    <div v-loading="loading" class="guide-preview-content">
      <template v-if="guide">
        <!-- Guide Header Card -->
        <div class="guide-header-card">
          <div class="guide-title-section">
            <h3 class="guide-name">{{ guide.name }}</h3>
            <div class="guide-meta">
              <el-tag :type="guide.status === 1 ? 'success' : 'info'" size="small" effect="dark">
                {{ guide.status === 1 ? $t('lz.label.base.active') : $t('lz.label.base.inactive') }}
              </el-tag>
              <el-tag :type="getPriorityTagType(guide.priority as string)" size="small">
                {{ getPriorityLabel(guide.priority as string) }}
              </el-tag>
            </div>
          </div>
          
          <div class="guide-stats">
            <div class="stat-item">
              <el-icon class="stat-icon" :size="20"><ele-Monitor /></el-icon>
              <div class="stat-info">
                <span class="stat-label">{{ $t('lz.entity.tms.equipmentType') }}</span>
                <span class="stat-value">{{ getEquipmentTypeName(guide.equipmentTypeId as string)  }}</span>
              </div>
            </div>
            <div class="stat-item">
              <el-icon class="stat-icon" :size="20"><ele-Timer /></el-icon>
              <div class="stat-info">
                <span class="stat-label">{{ $t('lz.label.tmsMaintenanceSchedule.frequency') }}</span>
                <span class="stat-value">{{ getFrequencyLabel(guide.frequency as string) }}</span>
              </div>
            </div>
            <div class="stat-item">
              <el-icon class="stat-icon" :size="20"><ele-Clock /></el-icon>
              <div class="stat-info">
                <span class="stat-label">{{ $t('lz.label.tmsMaintenanceSchedule.estimatedDuration') }}</span>
                <span class="stat-value">{{ guide.estimatedDuration ? `${guide.estimatedDuration} ${$t('lz.label.tmsMaintenanceSchedule.minutes')}` : '-' }}</span>
              </div>
            </div>
            <div class="stat-item" v-if="guide.manufacturer">
              <el-icon class="stat-icon" :size="20"><ele-OfficeBuilding /></el-icon>
              <div class="stat-info">
                <span class="stat-label">{{ $t('lz.label.tmsMaintenanceSchedule.manufacturer') }}</span>
                <span class="stat-value">{{ guide.manufacturer }}</span>
              </div>
            </div>
          </div>
        </div>
        
        <!-- Description Section -->
        <div v-if="guide.description" class="guide-section">
          <div class="section-header">
            <el-icon :size="18"><ele-InfoFilled /></el-icon>
            <span>{{ $t('lz.entity.base.desc') }}</span>
          </div>
          <div class="section-content description-content">
            {{ guide.description }}
          </div>
        </div>
        
        <!-- Checklist Section -->
        <div v-if="parsedChecklistItems && parsedChecklistItems.length > 0" class="guide-section">
          <div class="section-header">
            <el-icon :size="18"><ele-List /></el-icon>
            <span>{{ $t('lz.label.tmsMaintenanceSchedule.checklist') }}</span>
            <el-badge :value="parsedChecklistItems.length" type="primary" class="checklist-count" />
          </div>
          <div class="section-content">
            <div class="checklist-items">
              <div 
                v-for="(item, index) in parsedChecklistItems" 
                :key="index"
                class="checklist-item"
                :class="{ 'required': item.required }"
              >
                <span class="item-index">{{ index + 1 }}</span>
                <span class="item-task">{{ item.task }}</span>
                <el-tag v-if="item.required" type="danger" size="small" class="required-tag">
                  {{ $t('lz.label.tmsMaintenanceSchedule.required') }}
                </el-tag>
              </div>
            </div>
          </div>
        </div>
        
        <!-- Documents Section -->
        <div v-if="parsedDocuments && parsedDocuments.length > 0" class="guide-section">
          <div class="section-header">
            <el-icon :size="18"><ele-Folder /></el-icon>
            <span>{{ $t('lz.label.tmsMaintenanceSchedule.relatedDocuments') }}</span>
            <el-badge :value="parsedDocuments.length" type="success" class="checklist-count" />
          </div>
          <div class="section-content">
            <div class="document-grid">
              <div 
                v-for="doc in parsedDocuments" 
                :key="doc.id || doc.fileName"
                class="document-card"
                @click="handleOpenDocument(doc)"
              >
                <el-icon class="doc-icon" :size="28"><ele-Document /></el-icon>
                <span class="doc-name">{{ doc.fileName || doc.name  }}</span>
                <el-icon class="open-icon" :size="14"><ele-TopRight /></el-icon>
              </div>
            </div>
          </div>
        </div>
        
        <el-empty 
          v-if="!guide.description && (!parsedChecklistItems || parsedChecklistItems.length === 0) && (!parsedDocuments || parsedDocuments.length === 0)" 
          :description="$t('lz.label.tmsMaintenanceSchedule.noGuideDetails')" 
          :image-size="80" 
        />
      </template>
      <el-empty v-else :description="$t('lz.label.base.noData')" />
    </div>
    
    <template #footer>
      <el-button type="primary" @click="visible = false">{{ $t('lz.button.base.close') }}</el-button>
    </template>
  </el-dialog>
</template>

<script lang="ts" setup>
import { computed, ref, watch, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { getAPI } from '/@/utils/axios-utils';
import { TmsMaintenanceGuideApi } from '/@/api-services/tms';
import { useItsMediaServerStore } from '/@/stores/its/itsMediaServer';
import type { MaintenanceGuideOutput } from '/@/api-services/tms/models';
import { BaseConfigTypeEnum } from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';

const { t: $t } = useI18n();
const baseConfigStore = useBaseConfig();
const { getConfigDataByCode } = baseConfigStore;

// Props
const props = defineProps<{
  modelValue: boolean;
  guideId?: string;
  guideData?: MaintenanceGuideOutput | null;
  equipmentTypeList?: any[];
}>();

// Emits
const emit = defineEmits<{
  (e: 'update:modelValue', value: boolean): void;
}>();

// State
const loading = ref(false);
const guide = ref<MaintenanceGuideOutput | null>(null);
const fileServerUrl = ref('');

const priorityOptions = computed(() => {
  const cfg = getConfigDataByCode(BaseConfigTypeEnum.TmsMaintenancePriority) || [];
  return cfg.map(i => ({ value: i.code, label: i.name }));
});

const frequencyOptions = computed(() => {
  const cfg = getConfigDataByCode(BaseConfigTypeEnum.TmsMaintenanceFrequency) || [];
  return cfg.map(i => ({ value: i.code, label: i.name }));
});

// Load file server URL on mount
onMounted(async () => {
  try {
    // Địa chỉ file server dùng chung (chỉ gọi API 1 lần cho toàn app)
    fileServerUrl.value = await useItsMediaServerStore().ensureLoaded();
  } catch (error) {
    console.error('Error loading file server URL:', error);
  }
});

// Computed
const visible = computed({
  get: () => props.modelValue,
  set: (val) => emit('update:modelValue', val),
});

type ChecklistItemLike = {
  task?: string;
  Task?: string;
  required?: boolean;
  Required?: boolean;
  note?: string;
  Note?: string;
};

const normalizeChecklistItem = (item: ChecklistItemLike | null | undefined) => {
  const task = (item?.task ?? item?.Task ?? '') as string;
  const required = (item?.required ?? item?.Required ?? false) as boolean;
  const note = (item?.note ?? item?.Note ?? undefined) as string | undefined;
  return {
    task: typeof task === 'string' ? task : '',
    required: !!required,
    note: typeof note === 'string' ? note : undefined,
  };
};

// Parse checklist items - from checklistItems array or from checklist JSON string
const parsedChecklistItems = computed(() => {
  if (!guide.value) return [];
  
  // Prefer checklistItems array from API
  if (Array.isArray((guide.value as any).checklistItems) && (guide.value as any).checklistItems.length > 0) {
    return ((guide.value as any).checklistItems as ChecklistItemLike[])
      .map(normalizeChecklistItem)
      .filter(i => i.task && i.task.trim().length > 0);
  }

  // Fallback: parse from checklist JSON string/object
  const rawChecklist = (guide.value as any).checklist;
  if (!rawChecklist) return [];
  try {
    const parsed = typeof rawChecklist === 'string' ? JSON.parse(rawChecklist) : rawChecklist;
    if (!Array.isArray(parsed)) return [];
    return (parsed as ChecklistItemLike[])
      .map(normalizeChecklistItem)
      .filter(i => i.task && i.task.trim().length > 0);
  } catch (e) {
    console.error('Error parsing checklist:', e);
    return [];
  }
});

// Parse documents - from documents JSON string or array
const parsedDocuments = computed(() => {
  if (!guide.value) return [];
  
  const docs = guide.value.documents;
  if (!docs) return [];
  
  // If already an array, return it
  if (Array.isArray(docs)) return docs;
  
  // Try to parse from JSON string
  if (typeof docs === 'string') {
    try {
      const parsed = JSON.parse(docs);
      return Array.isArray(parsed) ? parsed : [];
    } catch (e) {
      console.error('Error parsing documents:', e);
      return [];
    }
  }
  
  return [];
});

// Watch for guideId changes to load guide data
watch(() => props.guideId, async (newGuideId) => {
  if (newGuideId && props.modelValue) {
    await loadGuide(newGuideId);
  }
}, { immediate: true });

// Watch for guideData prop
watch(() => props.guideData, (newData) => {
  guide.value = newData || null;
}, { immediate: true });

// Watch for dialog visibility
watch(() => props.modelValue, async (isVisible) => {
  if (isVisible) {
    if (props.guideData) {
      guide.value = props.guideData;
    } else if (props.guideId) {
      await loadGuide(props.guideId);
    }

    // Console Ninja debug (runs once per open)
    try {
      console.log('[GuidePreviewDialog] open checklist debug', {
        guideId: props.guideId,
        hasChecklistItems: Array.isArray((guide.value as any)?.checklistItems),
        checklistItemsLen: Array.isArray((guide.value as any)?.checklistItems) ? (guide.value as any).checklistItems.length : undefined,
        checklistType: typeof (guide.value as any)?.checklist,
        checklistPreview: typeof (guide.value as any)?.checklist === 'string' ? (guide.value as any).checklist.slice(0, 200) : (guide.value as any)?.checklist,
        parsedLen: parsedChecklistItems.value.length,
        firstParsed: parsedChecklistItems.value[0],
      });
    } catch {
      // ignore
    }
  }
});

// Methods
const loadGuide = async (guideId: string) => {
  try {
    loading.value = true;
    const res = await getAPI(TmsMaintenanceGuideApi).apiTmsTmsmaintenanceguideGetbyidIdGet(guideId);
    if (res.data.succeeded && res.data.result) {
      guide.value = res.data.result;
    }
  } catch (error) {
    console.error('Error loading guide:', error);
    guide.value = null;
  } finally {
    loading.value = false;
  }
};

const getEquipmentTypeName = (typeId: string) => {
  if (!props.equipmentTypeList) return '-';
  const type = props.equipmentTypeList.find(t => t.id === typeId);
  return type?.name || '-';
};

const getPriorityTagType = (priority: string): 'danger' | 'warning' | 'success' | 'info' => {
  const key = (priority || '').toString().trim().toLowerCase();
  switch (key) {
    case 'high': return 'danger';
    case 'medium': return 'warning';
    case 'low': return 'success';
    default: return 'info';
  }
};

const getPriorityLabel = (priority: string) => {
  const key = (priority || '').toString().trim().toLowerCase();
  const option = priorityOptions.value.find(o => o.value === key);
  return option?.label || priority || '-';
};

const getFrequencyLabel = (frequency: string) => {
  const key = (frequency || '').toString().trim().toLowerCase();
  const option = frequencyOptions.value.find(o => o.value === key);
  return option?.label || frequency || '-';
};

const handleOpenDocument = (doc: any) => {
  console.log('Opening document:', doc);
  const fileUrl = doc.filePath || doc.url || doc.path;
  if (fileUrl) {
    // Use file server URL from config, similar to editMap.vue
    let fullUrl = fileUrl;
    if (!fileUrl.startsWith('http')) {
      // Use file server base URL
      const baseUrl = fileServerUrl.value || window.__env__.VITE_API_URL;
      fullUrl = `${baseUrl}${fileUrl.startsWith('/') ? '' : '/'}${fileUrl}`;
    }
    console.log('Full URL:', fullUrl);
    window.open(fullUrl, '_blank');
  } else {
    console.warn('No file URL found in document:', doc);
  }
};

// Expose methods for parent to call
defineExpose({
  loadGuide,
});
</script>

<style lang="scss" scoped>
.guide-preview-content {
  min-height: 200px;
}

.guide-header-card {
  background: linear-gradient(135deg, var(--el-color-primary-light-9) 0%, var(--el-fill-color-light) 100%);
  border-radius: 12px;
  padding: 20px;
  margin-bottom: 20px;
  
  .guide-title-section {
    margin-bottom: 16px;
    
    .guide-name {
      margin: 0 0 8px 0;
      font-size: 18px;
      font-weight: 600;
      color: var(--el-text-color-primary);
    }
    
    .guide-meta {
      display: flex;
      gap: 8px;
    }
  }
  
  .guide-stats {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
    gap: 16px;
    
    .stat-item {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 12px;
      background: rgba(255, 255, 255, 0.8);
      border-radius: 8px;
      
      .stat-icon {
        color: var(--el-color-primary);
      }
      
      .stat-info {
        display: flex;
        flex-direction: column;
        
        .stat-label {
          font-size: 12px;
          color: var(--el-text-color-secondary);
        }
        
        .stat-value {
          font-size: 14px;
          font-weight: 500;
          color: var(--el-text-color-primary);
        }
      }
    }
  }
}

.guide-section {
  margin-bottom: 20px;
  border: 1px solid var(--el-border-color-light);
  border-radius: 8px;
  overflow: hidden;
  
  .section-header {
    display: flex;
    align-items: center;
    gap: 8px;
    padding: 12px 16px;
    background: var(--el-fill-color-light);
    font-weight: 600;
    color: var(--el-text-color-primary);
    
    .checklist-count {
      margin-left: 8px;
    }
  }
  
  .section-content {
    padding: 16px;
    
    &.description-content {
      white-space: pre-wrap;
      color: var(--el-text-color-regular);
      line-height: 1.6;
    }
  }
}

.checklist-items {
  .checklist-item {
    display: flex;
    align-items: center;
    gap: 12px;
    padding: 10px 12px;
    border-radius: 6px;
    margin-bottom: 8px;
    background: var(--el-fill-color-lighter);
    transition: background-color 0.2s;
    
    &:last-child {
      margin-bottom: 0;
    }
    
    &:hover {
      background: var(--el-fill-color);
    }
    
    &.required {
      border-left: 3px solid var(--el-color-danger);
    }
    
    .item-index {
      width: 24px;
      height: 24px;
      display: flex;
      align-items: center;
      justify-content: center;
      background: var(--el-color-primary);
      color: #fff;
      border-radius: 50%;
      font-size: 12px;
      font-weight: 600;
      flex-shrink: 0;
    }
    
    .item-task {
      flex: 1;
      color: var(--el-text-color-primary);
    }
    
    .required-tag {
      flex-shrink: 0;
    }
  }
}

.document-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
  gap: 12px;
  
  .document-card {
    display: flex;
    align-items: center;
    gap: 10px;
    padding: 12px;
    background: var(--el-fill-color-lighter);
    border-radius: 8px;
    cursor: pointer;
    transition: all 0.2s;
    
    &:hover {
      background: var(--el-fill-color);
      transform: translateY(-2px);
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
    }
    
    .doc-icon {
      color: var(--el-color-primary);
      flex-shrink: 0;
    }
    
    .doc-name {
      flex: 1;
      font-size: 13px;
      color: var(--el-text-color-primary);
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }
    
    .open-icon {
      color: var(--el-text-color-secondary);
      flex-shrink: 0;
    }
  }
}
</style>
