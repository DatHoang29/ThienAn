<template>
  <div class="vms-priority-container">
    <el-dialog 
      v-model="state.isShowDialog" 
      draggable 
      :close-on-click-modal="false" 
      width="650px"
      style="z-index: 1000;"
      body-class="custom-dialog-body"
      class="vms-priority-dialog"
    >
      <template #header>
        <div style="color: #fff">
          <!-- <el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle">
            <ele-Warning />
          </el-icon> -->
          <span>{{ $t('lz.message.base.warning') }}</span>
        </div>
      </template>
      
      <div class="warning-content">
        <div class="fixed-header">
          <div class="warning-message">
            <p>{{ $t('lz.message.vmsControl.warningLowerPriority',{newPriority: state.vmsNewData.priority}) }}</p>
            <p>{{ $t('lz.message.vmsControl.pleaseSelectVmsToUpdate') }}</p>
          </div>
          
          <div class="selection-controls">
            <el-checkbox
              v-model="selectAll"
              :indeterminate="isIndeterminate"
              @change="handleSelectAll"
            >{{ $t('lz.button.base.selectAll') }}</el-checkbox>
          </div>
        </div>

        <div class="scrollable-content">
          <div class="vms-list">
            <div v-for="vms in filteredVmsList?.sort((a,b)=> a.code.localeCompare(b.code))" :key="vms.equipmentId" class="vms-item">
              <div class="vms-header">
                <div class="vms-header-content">
                  <el-checkbox 
                    v-model="selectedItems[vms.equipmentId]"
                    @change="(val: boolean) => handleSingleSelect(vms.equipmentId, val)"
                  >
                    {{ vms.code }} ({{`Km${vms?.kmNumber}+${vms?.metNumber}`}})
                  </el-checkbox>
                  <el-icon 
                    class="el el-Home" 
                    :class="{ 'is-active': expandedItems[vms.equipmentId] }"
                    style="width:80%;display: flex;flex-direction: row-reverse;justify-content: flex-start;"
                    @click="() => toggleCollapse(vms.equipmentId)"
                  >
                    <ele-ArrowDown />
                  </el-icon>
                </div>
              </div>
              
              <div v-show="expandedItems[vms.equipmentId]" class="comparison-container">
                <div class="data-card">
                  <div class="priority-badge">{{ $t('lz.label.vms.priority') }}: {{ vms.priority }}</div>
                  <div class="image-container">
                    <img
                      :src="buildImageUrl(vms.url)"
                      alt="Current VMS Image"
                      class="preview-image"
                      @error="onImageError"
                    />
                  </div>
                </div>
                

                <div v-if="selectedItems[vms.equipmentId]" class="arrow-container">
                      <el-icon><ele-ArrowRight /></el-icon>
                  </div>
                  <div v-if="selectedItems[vms.equipmentId]" class="data-card">
                    <div class="priority-badge">{{ $t('lz.label.vms.priority') }}: {{ state.vmsNewData.priority }}</div>
                    <div class="image-container">
                      <img
                        :src="buildImageUrl(state.vmsNewData.url)"
                        alt="New VMS Image"
                        class="preview-image"
                        @error="onImageError"
                      />
                    </div>
                  </div>

                <!-- <div v-for="value in state.scheduleList"  :key="value.url">
                  <div v-if="selectedItems[vms.equipmentId] && value.url" class="arrow-container">
                      <el-icon><ele-ArrowRight /></el-icon>
                  </div>
                  <div v-if="selectedItems[vms.equipmentId] && value.url" class="data-card">
                    <div class="image-container">
                      <img 
                        v-if="`${state.baseUrl}/${value.url}`" 
                        :src="`${state.baseUrl}/${value.url}`" 
                        alt="New VMS Image"
                        class="preview-image"
                      />
                      <div v-else class="no-image">{{ $t('lz.label.base.noData') }}</div>
                    </div>
                  </div>
                </div> -->
                  
              
              </div>
            </div>
          </div>
        </div>
      </div>
      
      <template #footer>
        <span class="dialog-footer">
          <el-button @click="cancel">{{ $t('lz.button.base.cancel') }}</el-button>
          <el-button type="primary" @click="confirm" :disabled="!hasSelectedItems">
            {{ $t('lz.button.base.confirm') }}
          </el-button>
        </span>
      </template>
    </el-dialog>
  </div>
</template>

<script lang="ts" setup name="vmsPriority">
import { reactive, computed, ref, defineEmits } from 'vue';
import { useI18n } from 'vue-i18n';
import { AddControlInput } from '/@/api-services/vms';

const { t: $t } = useI18n();
const emits = defineEmits(['handleConfirm']);

// Đường dẫn ảnh "No image" trên server (ghép với baseUrl khi hiển thị).
// Ảnh này nằm cùng nơi chứa ảnh VMS (serve qua baseUrl), không phải asset bundle.
const NO_IMAGE_PATH = 'Upload/NoImage.jpg';

interface VmsItem {
  equipmentId: string | number;
  code: string;
  url: string;
  priority: number;
  kmNumber: any,
  metNumber: any
}

interface VmsNewData { 
  url: string;
  priority: number;
}

interface ScheduleData {   
    url: string;
    timeToLive: number;
}

interface StateType {
  isShowDialog: boolean;
  currentList: VmsItem[];
  vmsNewData: VmsNewData;
  baseUrl: string;
  scheduleList:  Array<ScheduleData>;
}

const state = reactive<StateType>( {
  isShowDialog: false,
  currentList: [] as any,
  vmsNewData: {} as any,
  baseUrl: '' as string,
  scheduleList: [] as Array<ScheduleData>
});

// Use refs for independent tracking
const selectedItems = ref<Record<string | number, boolean>>({});
const expandedItems = ref<Record<string | number, boolean>>({});
const selectAll = ref(false);
const isIndeterminate = ref(false);

// Xử lý lấy danh sách vms có priority thấp hơn priority mới
const filteredVmsList = computed(() => {
  return state.currentList.filter(vms => Number(vms.priority) < Number(state.vmsNewData.priority));
});

// Get all items with higher priority
const higherPriorityItems = computed(() => {
  return state.currentList.filter(vms => Number(vms.priority) > Number(state.vmsNewData.priority));
});

// Update the indeterminate state of the select all checkbox
const updateIndeterminateState = () => {
  if (filteredVmsList.value.length === 0) {
    isIndeterminate.value = false;
    return;
  }
  const selectedCount = filteredVmsList.value.filter(vms => selectedItems.value[vms.equipmentId]).length;
  const totalCount = filteredVmsList.value.length;
  
  if (selectedCount === 0) {
    selectAll.value = false;
    isIndeterminate.value = false;
  } else if (selectedCount === totalCount) {
    selectAll.value = true;
    isIndeterminate.value = false;
  } else {
    selectAll.value = false;
    isIndeterminate.value = true;
  }
};

// Check if any items are selected
const hasSelectedItems = computed(() => {
  return Object.values(selectedItems.value).some(selected => selected);
});

// URL đầy đủ của ảnh "No image" (baseUrl + path). Dùng làm fallback khi thiếu ảnh / ảnh lỗi.
const noImageUrl = computed(() => `${state.baseUrl}/${NO_IMAGE_PATH}`);

// Build URL ảnh: có url thì ghép baseUrl, không có thì trả về ảnh No image.
const buildImageUrl = (url?: string) => {
  if (!url) return noImageUrl.value;
  if (url.startsWith('data:') || url.startsWith('http')) return url;  // base64 / absolute -> giữ nguyên
  return `${state.baseUrl}/${url}`;
};
// Xử lý tự động mở rộng collapse khi priority của vms hiện tại <= 5 hoặc priority của vms hiện tại và vms mới khác nhau chênh lệch nhau hơn 5
const shouldAutoExpand = (vms: VmsItem): boolean => {
  const priorityDiff = Math.abs(Number(vms.priority) - Number(state.vmsNewData.priority));
  return Number(vms.priority) <= 5 || priorityDiff > 5;
};

// Handle select all functionality
const handleSelectAll = (value: boolean) => {
  const updatedSelectedItems = { ...selectedItems.value };
  filteredVmsList.value.forEach(vms => {
    updatedSelectedItems[vms.equipmentId] = value;
  });
  selectedItems.value = updatedSelectedItems;
  selectAll.value = value;
  isIndeterminate.value = false;
};

// Handle individual item selection
const handleSingleSelect = (equipmentId: string | number, checked: boolean) => {
  const updatedSelectedItems = { ...selectedItems.value, [equipmentId]: checked };
  selectedItems.value = updatedSelectedItems;
  updateIndeterminateState();
};

// Toggle the collapse state of an item
const toggleCollapse = (equipmentId: string | number) => {
  const updatedExpandedItems = { ...expandedItems.value, [equipmentId]: !expandedItems.value[equipmentId] };
  expandedItems.value = updatedExpandedItems;
};

// Fallback về ảnh "No image" khi ảnh gốc lỗi (404, đường dẫn hỏng...)
const onImageError = (event: Event) => {
  const target = event.target as HTMLImageElement;
  if (target.src !== noImageUrl.value) target.src = noImageUrl.value;
};

// Open the dialog with the current VMS data
const openDialog = (currentList: VmsItem[], addControlInput: AddControlInput, baseUrl: string, listEquipment: any, newImageBase64: string) => {
  console.log(currentList);
  state.baseUrl = baseUrl;   
  
  const priority = Number(addControlInput.priority); // ép về số để so sánh (tránh lỗi so sánh chuỗi "10" < "9")
  const hasLowerPriority = currentList.some(vms => priority > Number(vms.priority)); // Kiểm tra xem có vms nào có priority thấp hơn priority mới không
  
  // Get list image in schedule
  const {scheduleList} = {...addControlInput} // Lấy scheduleList từ addControlInput, nếu có thì gán vào state, nếu không có thì sử dụng url từ addControlInput để hiển thị ảnh mới trong dialog
  if(scheduleList) // Nếu có scheduleList thì gán vào state, nếu không có thì sử dụng url từ addControlInput để hiển thị ảnh mới trong dialog
  {
    state.scheduleList = JSON.parse(JSON.stringify(scheduleList)) as Array<ScheduleData>
    state.vmsNewData.url = state.scheduleList[0]?.url || newImageBase64 || ''; 
  }
  else // Nếu không có scheduleList thì sử dụng url từ addControlInput để hiển thị ảnh mới trong dialog
  {
    state.vmsNewData.url =  (addControlInput.url as string) || newImageBase64 || '';
  } 

  if (hasLowerPriority) // Nếu có ít nhất một VMS có priority thấp hơn, mới hiển thị dialog để chọn
  {    
    state.vmsNewData.priority = priority;  
     
    // Reset states
    selectedItems.value = {};
    expandedItems.value = {};
    
    // Initialize states
    currentList.forEach(vms => {
      const equipment = listEquipment.find((item:any) => item.id == vms.equipmentId)
      if(equipment)
      {
          vms.kmNumber = equipment.kmNumber
          vms.metNumber = equipment.metNumber
      }
      if (Number(vms.priority) < priority) 
      {
        selectedItems.value[vms.equipmentId] = true;
        expandedItems.value[vms.equipmentId] = shouldAutoExpand(vms);
      }
    });
    
    // Update current list
    state.currentList = [...currentList];

    updateIndeterminateState();
    state.isShowDialog = true;
    return true;
  }
  
  return false;
};

// Close the dialog and reset states
const closeDialog = () => {
  state.isShowDialog = false;
  selectedItems.value = {};
  expandedItems.value = {};
  selectAll.value = false;
  isIndeterminate.value = false;
};

// Handle cancel action


const cancel = () => {
  emits('handleConfirm', []);
  closeDialog();
};

// Handle confirm action
const confirm = () => {
  // Lấy danh sách id của các vms có priority thấp hơn được chọn
  const selectedLowerPriorityIds = Object.entries(selectedItems.value)
    .filter(([_, selected]) => selected)
    .map(([id]) => id);
    
  // Tìm các VMS đang có nội dung ưu tiên cao hơn nội dung mới.
  const higherPriorityIds = higherPriorityItems.value.map(vms => vms.equipmentId);
  
  // Kết hợp danh sách id của các vms có priority thấp hơn được chọn và các vms có priority cao hơn
  const allSelectedIds = [...selectedLowerPriorityIds, ...higherPriorityIds];
  
  emits('handleConfirm', allSelectedIds);
  closeDialog();
};

defineExpose({ openDialog });
</script>

<style scoped>
/* Dialog co theo nội dung: không ép chiều cao cố định -> footer (Bỏ qua/Xác nhận) luôn nằm
   ngay dưới nội dung ở góc phải, không dư khoảng trống. Danh sách dài thì cuộn trong .scrollable-content. */
.vms-priority-dialog :deep(.el-dialog__body) {
  padding: 0 !important;
}

.warning-content {
  display: flex;
  flex-direction: column;
}

.fixed-header {
  position: sticky;
  top: 0;
  background: white;
  z-index: 2;
  padding: 10px 10px 0;
  border-bottom: 1px solid #ebeef5;
}

.warning-message {
  color: #e6a23c;
  font-weight: bold;
  margin-bottom: 15px;
}

.selection-controls {
  margin-bottom: 15px;
}

.scrollable-content {
  /* Ngắn thì co theo nội dung; dài thì cuộn trong, không kéo dài dialog vô hạn. */
  max-height: 50vh;
  overflow-y: auto;
  padding: 10px;
}

.vms-list {
  display: flex;
  flex-direction: column;
  gap: 5px;
}

.vms-item {
  border: 1px solid #dcdfe6;
  border-radius: 4px;
  padding: 10px;
  background: white;
}

.vms-header {
  margin-bottom: 10px;
}

.vms-header-content {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.collapse-icon {
  transition: transform 0.3s;
  cursor: pointer;
  padding: 5px;
  border-radius: 4px;
}

.collapse-icon:hover {
  background-color: #f5f7fa;
}

.collapse-icon.is-active {
  transform: rotate(180deg);
}

.comparison-container {
  display: flex;
  align-items: center;
  gap: 5px;
  margin-top: 10px;
  transition: all 0.3s ease;
}

.arrow-container {
  display: flex;
  align-items: center;
  justify-content: center;
  color: #409EFF;
  font-size: 20px;
}

.data-card {
  flex: 1;
  background-color: #f8f8f8;
  border-radius: 4px;
  padding: 10px;
  text-align: center;
}

.priority-badge {
  display: inline-block;
  margin: 0 auto 6px;
  padding: 2px 10px;
  border-radius: 12px;
  background-color: var(--el-color-primary);
  color: #fff;
  font-size: 12px;
  font-weight: 600;
  text-align: center;
  white-space: nowrap;
}

.image-container {
  display: flex;
  justify-content: center;
  align-items: center;
  width: 100%;
}

.preview-image {
  width: 100%;          /* chiếm hết bề ngang card */
  height: auto;         /* cao tự động theo tỉ lệ ảnh */
  object-fit: contain;  /* giữ nguyên tỉ lệ, không méo */
  display: block;
}
</style>