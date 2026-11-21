<template>
  <div class="maintenance-guide-tab">
    <!-- Filter and Actions -->
    <el-card shadow="hover" :body-style="{ padding: '10px' }">
      <div style="display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 10px;">
        <div style="display: flex; gap: 10px; flex-wrap: wrap;">
          <!-- Equipment Type Filter -->
          <el-select
            v-model="state.filter.equipmentTypeId"
            :placeholder="$t('lz.label.tmsMaintenanceSchedule.selectEquipmentType')"
            clearable
            filterable
            style="width: 200px;"
            @change="handleQuery"
          >
            <el-option
              v-for="item in state.equipmentTypeList"
              :key="item.id"
              :label="item.name"
              :value="item.id"
            />
          </el-select>

          <!-- Frequency Filter -->
          <el-select
            v-model="state.filter.frequency"
            :placeholder="$t('lz.label.tmsMaintenanceSchedule.selectFrequency')"
            clearable
            style="width: 150px;"
            @change="handleQuery"
          >
            <el-option
              v-for="item in frequencyOptions"
              :key="item.value"
              :label="item.label"
              :value="item.value"
            />
          </el-select>

          <!-- Status Filter -->
          <el-select
            v-model="state.filter.status"
            :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.base.status') })"
            clearable
            style="width: 120px;"
            @change="handleQuery"
          >
            <el-option :label="$t('lz.label.base.active')" :value="1" />
            <el-option :label="$t('lz.label.base.inactive')" :value="0" />
          </el-select>

          <!-- Search -->
          <el-input
            v-model="state.filter.name"
            :placeholder="$t('lz.label.tmsDutySchedule.searchPlaceholder')"
            clearable
            style="width: 200px;"
            @keyup.enter="handleQuery"
          >
            <template #prefix>
              <el-icon><ele-Search /></el-icon>
            </template>
          </el-input>
        </div>

        <el-button type="primary" @click="handleAdd">
          <el-icon><ele-Plus /></el-icon>
          {{ $t('lz.button.base.add') }}
        </el-button>
      </div>
    </el-card>

    <!-- Data Table -->
    <el-card shadow="hover" style="margin-top: 10px;" :body-style="{ padding: '10px' }">
      <vxe-grid
        ref="xGrid"
        class="xGrid-style"
        v-bind="gridOptions"
        v-on="gridEvents"
      >
        <template #toolbar_tools>
          <TableHeader
            :show-add="false"
            :show-delete="false"
            @refresh="handleQuery"
            @custom-column="handleCustomColumn"
            :loading="state.loading"
          />
        </template>

        <template #empty>
          <el-empty :image-size="200" :description="$t('lz.label.base.noData')" />
        </template>

        <template #equipmentType_default="{ row }">
          {{ row.equipmentTypeName || getEquipmentTypeName(row.equipmentTypeId) }}
        </template>

        <template #manufacturer_default="{ row }">
          {{ row.manufacturer || $t('lz.label.tmsMaintenanceSchedule.allManufacturers') }}
        </template>

        <template #frequency_default="{ row }">
          <el-tag size="small">{{ getFrequencyLabel(row.frequency) }} {{ row.frequencyValue > 1 ? `(${row.frequencyValue})` : '' }}</el-tag>
        </template>

        <template #estimatedDuration_default="{ row }">
          {{ row.estimatedDuration ? `${row.estimatedDuration} ${$t('lz.label.tmsMaintenanceSchedule.minutes')}` : '-' }}
        </template>

        <template #priority_default="{ row }">
          <el-tag :type="getPriorityType(row.priority)" size="small">
            {{ getPriorityLabel(row.priority) }}
          </el-tag>
        </template>

        <template #status_default="{ row }">
          <el-tag :type="row.status === 1 ? 'success' : 'info'" size="small">
            {{ row.status === 1 ? $t('lz.label.base.active') : $t('lz.label.base.inactive') }}
          </el-tag>
        </template>

        <template #row_buttons="{ row }">
          <el-tooltip :content="$t('lz.button.base.detail')" placement="top">
            <el-button type="info" link size="small" @click="handlePreviewGuide(row)">
              <el-icon><ele-View /></el-icon>
            </el-button>
          </el-tooltip>
          <el-tooltip :content="$t('lz.button.base.edit')" placement="top" :auto-close="1000">
            <el-button type="primary" link size="small" @click="handleEdit(row)">
              <el-icon><ele-Edit /></el-icon>
            </el-button>
          </el-tooltip>
          <el-tooltip :content="$t('lz.button.base.delete')" placement="top" :auto-close="1000">
            <el-button type="danger" link size="small" @click="handleDelete(row)">
              <el-icon><ele-Delete /></el-icon>
            </el-button>
          </el-tooltip>
        </template>
      </vxe-grid>

      <!-- Pagination -->
      <div style="display: flex; justify-content: flex-end; margin-top: 10px;">
        <el-pagination
          v-model:current-page="state.pagination.page"
          v-model:page-size="state.pagination.pageSize"
          :page-sizes="[10, 20, 50, 100]"
          :total="state.pagination.total"
          layout="total, sizes, prev, pager, next, jumper"
          @size-change="handleQuery"
          @current-change="handleQuery"
        />
      </div>
    </el-card>

    <!-- Add/Edit Dialog -->
    <MaintenanceGuideFormDialog
      v-model="state.dialogVisible"
      :operate-type="state.operateType"
      :initial-data="state.formInitialData"
      :equipment-type-list="state.equipmentTypeList"
      @success="handleQuery"
    />

    <!-- Guide Preview Dialog -->
    <GuidePreviewDialog
      v-model="state.guidePreviewVisible"
      :guide-id="state.guidePreviewId"
      :guide-data="state.selectedGuide"
      :equipment-type-list="state.equipmentTypeList"
    />
  </div>
</template>

<script lang="ts" setup>
import { reactive, ref, computed, onMounted, defineAsyncComponent } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { getAPI } from '/@/utils/axios-utils';
import { TmsMaintenanceGuideApi, TmsEquipmentTypeApi } from '/@/api-services/tms';
import type { MaintenanceGuideOutput } from '/@/api-services/tms/models';
import { VxeGridInstance, VxeGridListeners } from 'vxe-table';
import MaintenanceGuideFormDialog from './maintenanceGuideFormDialog.vue';
import GuidePreviewDialog from './guidePreviewDialog.vue';
import { BaseConfigTypeEnum } from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';

const TableHeader = defineAsyncComponent(() => import('/@/components/advanced/table-header-operation.vue'));

const { t: $t } = useI18n();
const baseConfigStore = useBaseConfig();
const { getConfigDataByCode } = baseConfigStore;
const xGrid = ref<VxeGridInstance>();

// State
const state = reactive({
  loading: false,
  dialogVisible: false,
  operateType: 'add' as 'add' | 'edit',
  formInitialData: undefined as any,
  tableData: [] as MaintenanceGuideOutput[],
  equipmentTypeList: [] as any[],
  filter: {
    equipmentTypeId: undefined as string | undefined,
    frequency: undefined as string | undefined,
    status: undefined as number | undefined,
    name: '',
  },
  pagination: {
    page: 1,
    pageSize: 20,
    total: 0,
  },
  // Guide preview
  guidePreviewVisible: false,
  guidePreviewId: undefined as string | undefined,
  selectedGuide: null as MaintenanceGuideOutput | null,
});

// Grid options
const gridOptions = reactive({
  id: 'maintenanceGuide',
  border: true,
  stripe: true,
  round: true,
  showOverflow: true,
  loading: false,
  height: 600,
  rowConfig: {
    keyField: 'id',
    isHover: true,
  },
  columnConfig: {
    resizable: true,
    isCurrent: false,
  },
  customConfig: {
    storage: {
      visible: true,
      resizable: true,
    },
  },
  data: [] as MaintenanceGuideOutput[],
  columns: [
    { type: 'seq' as const, width: 60, title: $t('lz.label.base.idx'), align: 'center' as const },
    { field: 'name', title: $t('lz.label.tmsMaintenanceSchedule.guideName'), minWidth: 200 },
    { field: 'equipmentTypeName', title: $t('lz.entity.tms.equipmentType'), width: 150, slots: { default: 'equipmentType_default' } },
    { field: 'manufacturer', title: $t('lz.label.tmsMaintenanceSchedule.manufacturer'), width: 120, slots: { default: 'manufacturer_default' } },
    { field: 'frequency', title: $t('lz.label.tmsMaintenanceSchedule.frequency'), width: 120, align: 'center' as const, slots: { default: 'frequency_default' } },
    { field: 'estimatedDuration', title: $t('lz.label.tmsMaintenanceSchedule.estimatedDuration'), width: 120, align: 'center' as const, slots: { default: 'estimatedDuration_default' } },
    { field: 'priority', title: $t('lz.entity.base.priority'), width: 100, align: 'center' as const, slots: { default: 'priority_default' } },
    { field: 'status', title: $t('lz.entity.base.status'), width: 100, align: 'center' as const, slots: { default: 'status_default' } },
    { title: $t('lz.label.base.action'), width: 150, align: 'center' as const, fixed: 'right' as const, slots: { default: 'row_buttons' } },
  ],
  toolbarConfig: {
    slots: {
      tools: 'toolbar_tools',
    },
  },
});

const gridEvents: VxeGridListeners = {
  toolbarToolClick() {
    // Handle toolbar clicks if needed
  },
};

// Handle custom column for grid
const handleCustomColumn = () => {
  xGrid.value?.openCustom();
};

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

// Methods
const getFrequencyLabel = (frequency: string) => {
  const key = normalizeFrequencyKey(frequency);
  const option = frequencyOptions.value.find(o => o.value === key);
  return option?.label || frequency;
};

const getPriorityLabel = (priority: string) => {
  const key = normalizePriorityKey(priority);
  const option = priorityOptions.value.find(o => o.value === key);
  return option?.label || priority;
};

const getPriorityType = (priority: string): 'danger' | 'warning' | 'success' | 'info' => {
  const key = normalizePriorityKey(priority);
  switch (key) {
    case 'high': return 'danger';
    case 'medium': return 'warning';
    case 'low': return 'success';
    default: return 'info';
  }
};

const getEquipmentTypeName = (typeId: string | null | undefined) => {
  if (!typeId) return '-';
  const type = state.equipmentTypeList.find(t => t.id === typeId);
  return type?.name || '-';
};

const loadEquipmentTypes = async () => {
  try {
    const res = await getAPI(TmsEquipmentTypeApi).apiTmsTmsequipmenttypeListGet();
    if (res.data.succeeded) {
      state.equipmentTypeList = res.data.result || [];
    }
  } catch (error) {
    console.error('Error loading equipment types:', error);
  }
};

const handleQuery = async () => {
  state.loading = true;
  gridOptions.loading = true;
  try {
    const res = await getAPI(TmsMaintenanceGuideApi).apiTmsTmsmaintenanceguideGetpagePost({
      page: state.pagination.page,
      pageSize: state.pagination.pageSize,
      equipmentTypeId: state.filter.equipmentTypeId || undefined,
      frequency: state.filter.frequency || undefined,
      status: state.filter.status,
      name: state.filter.name || undefined,
    });
    if (res.data.succeeded && res.data.result) {
      state.tableData = res.data.result.records || [];
      state.pagination.total = res.data.result.total || 0;
      // Sync data to grid
      gridOptions.data = state.tableData;
    }
  } catch (error: any) {
    console.error('Error loading guides:', error);
    ElMessage.error(error?.message || $t('lz.message.base.acctionFail'));
  } finally {
    state.loading = false;
    gridOptions.loading = false;
  }
};

const resetForm = () => {
  state.formInitialData = undefined;
};

const handleAdd = () => {
  resetForm();
  state.operateType = 'add';
  state.dialogVisible = true;
};

const handleEdit = (row: MaintenanceGuideOutput) => {
  state.operateType = 'edit';

  // Console Ninja: inspect API row shape
  try {
    console.log('[MaintenanceGuideTab] handleEdit row', {
      id: row.id,
      name: row.name,
      checklistItemsType: typeof (row as any).checklistItems,
      checklistItemsLen: Array.isArray((row as any).checklistItems) ? (row as any).checklistItems.length : undefined,
      checklistType: typeof (row as any).checklist,
      checklistPreview: typeof (row as any).checklist === 'string' ? (row as any).checklist.slice(0, 200) : (row as any).checklist,
    });
  } catch {
    // ignore
  }

  const normalizeChecklistItem = (item: any) => {
    const task = (item?.task ?? item?.Task ?? '') as string;
    const required = (item?.required ?? item?.Required ?? false) as boolean;
    const note = (item?.note ?? item?.Note ?? undefined) as string | undefined;
    return {
      task: typeof task === 'string' ? task : '',
      required: !!required,
      note: typeof note === 'string' ? note : undefined,
    };
  };
  
  // Parse checklistItems - from checklistItems array or from checklist JSON string
  let checklistItems: { task: string; required: boolean; note?: string }[] = [];
  if (row.checklistItems && row.checklistItems.length > 0) {
    checklistItems = row.checklistItems.map((item: any) => normalizeChecklistItem(item));
  } else if (row.checklist) {
    try {
      const parsed = typeof row.checklist === 'string' ? JSON.parse(row.checklist) : row.checklist;
      if (Array.isArray(parsed)) {
        checklistItems = parsed.map((item: any) => normalizeChecklistItem(item));
      }
    } catch (e) {
      console.error('Error parsing checklist:', e);
    }
  }

  try {
    console.log('[MaintenanceGuideTab] handleEdit checklist normalized', {
      id: row.id,
      count: checklistItems.length,
      first: checklistItems[0],
    });
  } catch {
    // ignore
  }
  
  // Parse documents - from documents JSON string
  let documents: any[] = [];
  if (row.documents) {
    try {
      const parsed = typeof row.documents === 'string' ? JSON.parse(row.documents) : row.documents;
      if (Array.isArray(parsed)) {
        documents = parsed;
      }
    } catch (e) {
      console.error('Error parsing documents:', e);
    }
  }
  
  state.formInitialData = {
    id: row.id || '',
    equipmentTypeId: row.equipmentTypeId || '',
    manufacturer: row.manufacturer || '',
    name: row.name || '',
    description: row.description || '',
    frequency: normalizeFrequencyKey(row.frequency) || 'monthly',
    frequencyValue: row.frequencyValue || 1,
    estimatedDuration: row.estimatedDuration || 60,
    priority: normalizePriorityKey(row.priority) || 'medium',
    status: row.status ?? 1,
    remark: row.remark || '',
    checklistItems,
    documents,
  };
  state.dialogVisible = true;
};

// Preview guide - open GuidePreviewDialog
const handlePreviewGuide = (row: MaintenanceGuideOutput) => {
  state.guidePreviewId = row.id || undefined;
  state.selectedGuide = row;
  state.guidePreviewVisible = true;
};

const handleDelete = async (row: MaintenanceGuideOutput) => {
  try {
    await ElMessageBox.confirm(
      $t('lz.message.tmsMaintenanceSchedule.confirmDeleteGuide'),
      $t('lz.message.tmsDutySchedule.confirmDelete'),
      {
        confirmButtonText: $t('lz.button.base.delete'),
        cancelButtonText: $t('lz.button.base.cancel'),
        type: 'warning',
      }
    );

    await getAPI(TmsMaintenanceGuideApi).apiTmsTmsmaintenanceguideDeletePost({ id: row.id! });
    ElMessage.success($t('lz.message.base.deleteSuccess'));
    await handleQuery();
  } catch (error: any) {
    if (error !== 'cancel') {
      console.error('Error deleting guide:', error);
      ElMessage.error(error?.message || $t('lz.message.base.acctionFail'));
    }
  }
};

// Init
onMounted(async () => {
  await loadEquipmentTypes();
  await handleQuery();
});
</script>

<style lang="scss" scoped>
.maintenance-guide-tab {
  height: 100%;
}
</style>
