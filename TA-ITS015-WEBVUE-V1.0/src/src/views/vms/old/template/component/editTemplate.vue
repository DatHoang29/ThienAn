<template>
  <div class="editVmsTemplate-container">
    <el-dialog v-model="state.isShowDialog" draggable :close-on-click-modal="false" width="1200px">
      <template #header>
        <div style="color: #fff">
          <el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle">
            <ele-Edit />
          </el-icon>
          <span> {{ props.title }} </span>
        </div>
      </template>
      <div class="dialog-content">
        <div class="main-content">
          <el-tabs v-loading="state.loading" v-model="state.selectedTabName">
            <el-tab-pane label="Template" name="form-edit">
              <el-form :model="state.editForm" ref="editFormRef" label-width="auto" label-position="left">
                <!-- <el-card class="template-size"> -->
                <el-row :gutter="10">
                  <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
                    <el-form-item :label="$t('lz.entity.base.code')" prop="code"
                      :rules="[{ required: true, message: $t('lz.validation.base.required',{PropertyName: $t('lz.entity.base.code')}), trigger: 'blur' }, 
                                { max: 64, message: $t('lz.validation.base.maxlength', {PropertyName: $t('lz.entity.base.code'), MaxLength: 64 }), trigger: 'blur' }]">
                      <el-input v-model="state.editForm.code" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.code')})" clearable />
                    </el-form-item>
                  </el-col>
                  <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
                    <el-form-item :label="$t('lz.entity.base.lang')" prop="lang"
                      :rules="[{ required: true, message: $t('lz.validation.base.required',{PropertyName: $t('lz.entity.base.lang')}), trigger: 'blur' }]">
                      <el-select v-model="state.editForm.lang" :placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.entity.base.lang')})" clearable class="w100">
                        <el-option v-for="item in getConfigDataByCode(BaseConfigTypeEnum.SysLanguageType)" :key="item.code" :label="item.name" :value="item.code" />
                      </el-select>
                    </el-form-item>
                  </el-col>
                </el-row>
                <el-row :gutter="10">
                  <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
                    <el-form-item :label="$t('lz.entity.base.name')" prop="name"
                      :rules="[{ required: true, message: $t('lz.validation.base.required',{PropertyName: $t('lz.entity.base.name')}), trigger: 'blur' }]">
                      <el-input v-model="state.editForm.name" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.name')})" clearable
                        maxlength="128" show-word-limit />
                    </el-form-item>
                  </el-col>

                  <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
                    <el-form-item :label="$t('lz.entity.vmsTemplate.size')" prop="size"
                      :rules="[{ required: true, message: $t('lz.validation.base.required',{PropertyName: $t('lz.entity.vmsTemplate.size')}), trigger: 'blur' }]">
                      <el-select v-model="state.editForm.size" :placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.entity.vmsTemplate.size')})" @change="updateSelectedSize">
                        <el-option v-for="size in state.templateSizes" :key="size.code" :label="size.name"
                          :value="size.code" />
                      </el-select>

                    </el-form-item>

                  </el-col>
                </el-row>
                <!-- </el-card> -->

                <el-tabs v-model="activeTab">
                  <el-tab-pane :label="$t('lz.label.vmsTemplate.textElement')" name="text">
                    <div v-for="(element, index) in textElements" :key="element.id" class="element-control">
                      <div class="element-header">
                        <h5>{{ element.label }}</h5>
                        <el-button @click="removeElement(element.id)" type="danger" size="small" icon="ele-Delete">{{ $t('lz.button.base.delete') }}</el-button>
                      </div>
                      <div class="element-content">
                        <el-input v-model="element.content" :placeholder="$t('lz.label.base.pleaseType',{input: element.placeholder})" />
                        <div class="element-position-size">
                          <div class="input-group">
                            <span class="input-label">{{ $t('lz.entity.vms.x') }}:</span>
                            <el-input-number v-model="element.x" :min="0"
                              :max="Math.max(0, selectedSize.width - element.width)" @change="updateElement" />
                          </div>
                          <div class="input-group">
                            <span class="input-label">{{ $t('lz.entity.vms.y') }}:</span>
                            <el-input-number v-model="element.y" :min="0"
                              :max="Math.max(0, selectedSize.height - element.height)" @change="updateElement" />
                          </div>
                        </div>
                        <div class="element-position-size">
                          <div class="input-group">
                            <span class="input-label">{{ $t('lz.entity.vms.width') }}:</span>
                            <el-input-number v-model="element.width" :min="20"
                              :max="Math.max(20, selectedSize.width - element.x)" @change="updateElement" />
                          </div>
                          <div class="input-group">
                            <span class="input-label">{{ $t('lz.entity.vms.height') }}:</span>
                            <el-input-number v-model="element.height" :min="20"
                              :max="Math.max(20, selectedSize.height - element.y)" @change="updateElement" />
                          </div>
                        </div>
                      </div>
                    </div>
                    <el-button @click="addTextElement" :disabled="textElementCount >= 3" icon="ele-Plus" type="primary">{{$t('lz.button.vms.addText')}}</el-button>
                  </el-tab-pane>

                  <el-tab-pane :label="$t('lz.label.vmsTemplate.imageElement')" name="image">
                    <div v-for="(element, index) in imageElements" :key="element.id" class="element-control">
                      <div class="element-header">
                        <h5>{{ element.label }}</h5>
                        <el-button @click="removeElement(element.id)" type="danger" size="small" icon="ele-Delete">{{$t('lz.button.base.delete')}}</el-button>
                      </div>
                      <div class="element-content ">
                        <div style="display: flex;" class="w100">
                          <el-select v-model="element.content" value-key="id" :placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.label.base.image')})"
                            @change="updateElement(element)">
                            <el-option v-for="item in state.vmsImgTemplateAll" :key="item.id" :label="item.name"
                              :value="item" style="height: 100%;">
                              <img :src="state.baseUrl + '/' + item.url" :alt="$t('lz.label.base.image')" class="uploaded-image"
                                style=" height: 100%; " />
                              <span>{{ item.name }} | {{ item.size }}px</span>
                            </el-option>
                          </el-select>
                        </div>

                        <div class="element-position-size">
                          <div class="input-group">
                            <span class="input-label">{{$t('lz.entity.vms.x')}}:</span>
                            <el-input-number v-model="element.x" :min="0"
                              :max="Math.max(0, selectedSize.width - element.width)" @change="updateElement" />
                          </div>
                          <div class="input-group">
                            <span class="input-label">{{$t('lz.entity.vms.y')}}:</span>
                            <el-input-number v-model="element.y" :min="0"
                              :max="Math.max(0, selectedSize.height - element.height)" @change="updateElement" />
                          </div>
                        </div>
                        <div class="element-position-size">
                          <div class="input-group">
                            <span class="input-label">{{$t('lz.entity.vms.width')}}:</span>
                            <el-input-number v-model="element.width" :min="20"
                              :max="Math.max(20, selectedSize.width - element.x)" @change="updateElement" />
                          </div>
                          <div class="input-group">
                            <span class="input-label">{{$t('lz.entity.vms.height')}}:</span>
                            <el-input-number v-model="element.height" :min="20"
                              :max="Math.max(20, selectedSize.height - element.y)" @change="updateElement" />
                          </div>
                        </div>
                      </div>
                      <div v-if="element.content" class="uploaded-image-preview">
                        <img
                          :src="isBase64(element.content) ? element.content : state.baseUrl + '/' + element.content.url"
                          class="uploaded-image" />
                        <span>{{ element.content.name }} | {{ element.content.size }}px</span>
                      </div>
                    </div>
                    <el-button @click="addImageElement" :disabled="imageElementCount >= 2" icon="ele-Plus" type="primary">{{$t('lz.button.vms.addImage')}}</el-button>
                  </el-tab-pane>
                </el-tabs>
                <!-- </el-card> -->
              </el-form>
            </el-tab-pane>
            <el-tab-pane :label="$t('lz.label.base.otherInfo')" name="other-info" style="height: 450px; overflow-y: auto; overflow-x: hidden">
              <el-form :model="state.editForm" ref="editFormInfoRef" label-width="auto"  label-position="left">
                <el-row :gutter="10">
                  <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
                    <el-form-item :label="$t('lz.entity.base.status')">
                      <el-radio-group v-model="state.editForm.status" v-for="item in getConfigDataByCode(BaseConfigTypeEnum.SysStatusType)" :key="item.code">
                        <el-radio :value="parseInt(item.code)" class="mr5">{{ item.name }}</el-radio>
                      </el-radio-group>

                    </el-form-item>
                  </el-col>
                  <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
                    <el-form-item :label="$t('lz.entity.base.orderNo')" prop="orderNo">
                      <el-input-number v-model="state.editForm.orderNo" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.orderNo')})" class="w100"
                        :min="0" />
                    </el-form-item>
                  </el-col>
                  <el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
                    <el-form-item :label="$t('lz.entity.base.remark')">
                      <el-input v-model="state.editForm.remark" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.remark')})"
                        clearable type="textarea" />
                    </el-form-item>
                  </el-col>
                </el-row>
              </el-form>
            </el-tab-pane>
          </el-tabs>
        </div>

        <div class="splitter" @mousedown="startResizeSplitter"></div>

        <div class="preview-container" :style="{ width: previewWidth + 'px' }">
          <el-card v-if="selectedTemplate" class="preview-panel">
            <div class="design-section">
              <h3>{{ $t('lz.label.vmsTemplate.designArea') }}</h3>
              <div class="design-wrapper">
                <div class="design-container" :style="designContainerStyle" ref="designContainer">
                  <div v-for="element in selectedTemplate.elements" :key="element.id" :style="getElementStyle(element)"
                    class="element" @mousedown="startDrag($event, element.id)" @mouseover="hoveredElement = element.id"
                    @mouseleave="hoveredElement = null">
                    <div class="element-content" :style="getElementContentStyle(element)">
                      <template v-if="element.type === 'text'">
                        {{ element.content }}
                      </template>
                      <template v-else-if="element.type === 'image'">
                        <img v-if="element.content"
                          :src="isBase64(element.content.url) ? element.content : state.baseUrl + '/' + element.content.url"
                          alt="Selected image" style="width: 100%; height: 100%; object-fit: cover;" />
                      </template>
                    </div>
                    <div class="element-label">{{ getElementLabel(element) }}</div>
                    <div v-if="hoveredElement === element.id" class="element-info">
                      {{ element.width }}x{{ element.height }} | X: {{ element.x }}, Y: {{ element.y }}
                    </div>
                    <div v-if="hoveredElement === element.id" class="element-actions">
                      <button @mousedown.stop="startDrag($event, element.id)"
                        :class="{ active: activeMode === 'drag' }">
                        <i class="iconfont icon-tuodong" style="font-size: 12px;"></i>
                      </button>
                      <button @mousedown.stop="startResize($event, element.id)"
                        :class="{ active: activeMode === 'resize' }">
                        <i class="iconfont icon-caijian" style="font-size: 12px;"></i>
                      </button>
                    </div>
                  </div>
                </div>
              </div>
            </div>
            <div class="preview-section" ref="previewContainer">
              <h3>{{ $t('lz.label.vmsTemplate.preview') }}</h3>
              <div class="preview-wrapper">
                <div class="preview-content" :style="previewContentStyle">
                  <div v-for="element in selectedTemplate.elements" :key="element.id"
                    :style="getElementStyle(element, true)">
                    <template v-if="element.type === 'text'" style="text-align: center;">
                      {{ element.content }}
                    </template>
                    <template v-else-if="element.type === 'image'">
                      <img v-if="element.content"
                        :src="isBase64(element.content.url) ? element.content.url : state.baseUrl + '/' + element.content.url"
                        alt="Selected image" style="width: 100%; height: 100%; object-fit: cover;" />
                    </template>
                  </div>
                </div>
              </div>
            </div>
          </el-card>
        </div>
      </div>
      <template #footer>
        <span class="dialog-footer">
          <el-button @click="cancel">{{ $t('lz.button.base.cancel') }}</el-button>
          <el-button type="primary" :loading="state.loading" @click="submit">{{ $t('lz.button.base.confirm') }}</el-button>
        </span>
      </template>
    </el-dialog>
  </div>
</template>

<script lang="ts" setup name="editVmsTemplate">
// Import libraries
import { ref, computed, onMounted, onUnmounted, reactive } from 'vue';
import { getAPI } from '/@/utils/axios-utils';
import { OperateType } from '/@/types/constants';
import { ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';
// Import API and models
import { VmsTemplateApi, VmsImageApi } from '/@/api-services/vms';
import { UpdateTemplateInput } from '/@/api-services/vms';
import { useItsMediaServerStore } from '/@/stores/its/itsMediaServer';

import { BaseConfigTypeEnum } from '/@/types/enums/baseEnum';
import { useBaseConfig } from '/@/stores/baseConfig';
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
const getConfigItemByCode = useBaseConfig().getConfigItemByCode;


// Define props and emits
interface Props {
  /** the type of operation */
  operateType: OperateType;
  title: string;
}
const props = defineProps<Props>();
const emits = defineEmits(['handleQuery']);

// Define reactive states
const activeTab = ref('text');
const templates = ref([
  { id: 1, name: 'Default', elements: [] as any }
]);
const selectedTemplate = ref(templates.value.find(template => template.id === 1));

const selectedSizeId = ref(null);
const hoveredElement = ref(null);
const activeMode = ref(null) as any;
const draggedElement = ref(null);
const startX = ref(0);
const startY = ref(0);
const designContainer = ref(null);
const previewContainer = ref(null);

const state = reactive({
  isShowDialog: false,
  editForm: {} as UpdateTemplateInput,
  templateSizes: [] as any,
  vmsImgTemplateAll: [] as any,
  baseUrl: '' as string,
  loading: false as boolean,
  selectedTabName: 'form-edit' as string,
});


// Lifecycle hooks
onMounted(async () => {
  try {

    state.templateSizes = getConfigDataByCode(BaseConfigTypeEnum.VmsImageSize);

    var response = await getAPI(VmsImageApi).apiVmsVmsimageListPost();
    // Template chỉ dùng hình Loại 'icon' (không hỗ trợ Loại 'image')
    state.vmsImgTemplateAll = response.data.result?.filter((item: any) => item.type === 'icon').map((item: any) => ({
      id: item.id,
      name: item.name,
      url: item.url,
      size: item.size
    }));

    // Địa chỉ file server dùng chung (chỉ gọi API 1 lần cho toàn app)
    state.baseUrl = await useItsMediaServerStore().ensureLoaded();

  } catch (error) {
    console.error('Error fetching data:', error);
  }
});

// Add cleanup in onUnmounted
onUnmounted(() => {
  document.removeEventListener('mousemove', handleResizeSplitter);
  document.removeEventListener('mouseup', stopResizeSplitter);
});




// Define template sizes

// Computed properties
const selectedSize = computed(() => {
  const size = state.templateSizes.find((size: any) => size.code === state.editForm.size);


  if (size && size.name) {
    const [width, height] = size.code.split('x').map((dim: any) => dim.trim());
    return {
      width: parseFloat(width),
      height: parseFloat(height),
    } as any;
  }

  return { width: null, height: null }; // Return null or default values if size is not found
});
const textElements = computed(() => selectedTemplate.value?.elements.filter((e: any) => e.type === 'text') || [] as any);
const imageElements = computed(() => selectedTemplate.value?.elements.filter((e: any) => e.type === 'image') || [] as any);
const textElementCount = computed(() => textElements.value.length);
const imageElementCount = computed(() => imageElements.value.length);

const designContainerStyle = computed(() => {
  if (!selectedTemplate.value || !selectedSize.value) return {};

  const containerWidth = previewWidth.value - 40; // Account for padding
  const scale = Math.min(1, containerWidth / selectedSize.value.width);

  return {
    width: `${selectedSize.value.width}px`,
    height: `${selectedSize.value.height}px`,
    position: 'relative',
    transform: `scale(${scale})`,
    transformOrigin: 'top left',
    margin: '0 auto'
  };
});

const previewContentStyle = computed(() => {
  if (!selectedSize.value) return {};

  const containerWidth = previewWidth.value - 40; // Account for padding
  const scale = Math.min(1, containerWidth / selectedSize.value.width);

  return {
    width: `${selectedSize.value.width}px`,
    height: `${selectedSize.value.height}px`,
    position: 'relative',
    transform: `scale(${scale})`,
    transformOrigin: 'top left',
    margin: '0 auto'
  };
});

// I18n
const { t: $t } = useI18n();
const getElementLabel = (element: any) => {
  return element.label;
};
const startDrag = (event: any, elementId: any) => {
  event.preventDefault();
  activeMode.value = 'drag';
  draggedElement.value = elementId;
  startX.value = event.clientX;
  startY.value = event.clientY;
  document.addEventListener('mousemove', onDrag);
  document.addEventListener('mouseup', stopDrag);
};
const onDrag = (event: any) => {
  if (draggedElement.value && selectedTemplate.value) {
    const element = selectedTemplate.value.elements.find((e: any) => e.id === draggedElement.value);
    if (element) {
      const dx = event.clientX - startX.value;
      const dy = event.clientY - startY.value;
      element.x = Math.max(0, Math.min(element.x + dx, selectedSize.value.width - element.width));
      element.y = Math.max(0, Math.min(element.y + dy, selectedSize.value.height - element.height));
      startX.value = event.clientX;
      startY.value = event.clientY;
      updateElement(element);
    }
  }
};
const isBase64 = (str: any) => {
  // Kiểm tra chuỗi có bắt đầu bằng định dạng Base64 hay không
  const base64Regex = /^data:image\/[a-zA-Z]+;base64,/;
  return base64Regex.test(str);
};
const stopDrag = () => {
  activeMode.value = null;
  draggedElement.value = null;
  document.removeEventListener('mousemove', onDrag);
  document.removeEventListener('mouseup', stopDrag);
};

const startResize = (event: any, elementId: any) => {
  event.preventDefault();
  activeMode.value = 'resize';
  draggedElement.value = elementId;
  startX.value = event.clientX;
  startY.value = event.clientY;
  document.addEventListener('mousemove', onResize);
  document.addEventListener('mouseup', stopResize);
};

const onResize = (event: any) => {
  if (draggedElement.value && selectedTemplate.value) {
    const element = selectedTemplate.value.elements.find((e: any) => e.id === draggedElement.value);
    if (element) {
      const dx = event.clientX - startX.value;
      const dy = event.clientY - startY.value;
      element.width = Math.max(20, Math.min(element.width + dx, selectedSize.value.width - element.x));
      element.height = Math.max(20, Math.min(element.height + dy, selectedSize.value.height - element.y));
      startX.value = event.clientX;
      startY.value = event.clientY;
      updateElement(element);
    }
  }
};

const stopResize = () => {
  activeMode.value = null;
  draggedElement.value = null;
  document.removeEventListener('mousemove', onResize);
  document.removeEventListener('mouseup', stopResize);
};

// Methods
const addTextElement = () => {
  if (selectedTemplate.value) {
    const newIndex = textElements.value.length + 1;
    const newElement = {
      id: `row${newIndex}`,
      type: 'text',
      content: '',
      x: 10,
      y: 10,
      width: 130,
      height: 20,
      label: $t('lz.label.vms.row') + ' ' + newIndex,
    };
    selectedTemplate.value.elements.push(newElement);
  }
};

const addImageElement = () => {
  if (selectedTemplate.value && selectedSize.value) {
    const newIndex = imageElements.value.length + 1;
    const size = Math.floor(selectedSize.value.width / 4);
    const newElement = {
      id: `image${newIndex}`,
      type: 'image',
      content: '',
      x: 10,
      y: 10,
      width: size,
      height: size,
      label: $t('lz.label.base.image') + ' ' + newIndex,
    };
    selectedTemplate.value.elements.push(newElement);
  }
};

const removeElement = (elementId: any) => {
  if (selectedTemplate.value) {
    selectedTemplate.value.elements = selectedTemplate.value.elements.filter((e: any) => e.id !== elementId);
  }
};
const getImageName = (element: any) => {
  return element.fileName || 'Uploaded image';
};
const uploadImage = (elementId: any, file: any) => {
  const reader = new FileReader();
  reader.onload = (e) => {
    const element = selectedTemplate.value?.elements.find((el: any) => el.id === elementId);
    if (element) {
      element.content = e.target?.result;
      element.fileName = file.name;
      updateElement(element);
    }
  };
  reader.readAsDataURL(file.raw);
};
const getElementStyle = (element: any, isPreview = false) => {
  const style: any = {
    position: 'absolute',
    left: `${element.x}px`,
    top: `${element.y}px`,
    width: `${element.width}px`,
    height: `${element.height}px`,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
  };

  if (!isPreview) {
    style.border = '1px solid #ccc';
  }

  return style;
};

const getElementContentStyle = (element: any) => {
  const style: any = {
    width: '100%',
    height: '100%',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    overflow: 'hidden',
  };

  if (element.type === 'text') {
    style.textAlign = 'center';
  }

  return style;
};

const updateElement = (item: any) => {
  if (selectedTemplate.value) {
    selectedTemplate.value = { ...selectedTemplate.value };
  }
};

// const exportDesignToJSON = () => {
//   const designData = JSON.stringify(selectedTemplate.value);
//   const blob = new Blob([designData], { type: 'application/json' });
//   const url = URL.createObjectURL(blob);

//   // Create a temporary link element to download the file
//   const a = document.createElement('a');
//   a.href = url;
//   a.download = 'design.json';
//   document.body.appendChild(a); // Append the link to the document body
//   a.click(); // Trigger the download
//   document.body.removeChild(a); // Remove the link from the document

//   // Optionally revoke the object URL after a timeout to avoid memory leaks
//   setTimeout(() => {
//     URL.revokeObjectURL(url);
//   }, 100); // Delay to ensure the file is downloaded

//   return url; // Return the Blob URL (not a file path)
// };

const importDesignFromJSON = async (id: any) => {
  try {
    if(id == undefined || id == '' || id == null) {
      //clear the selectedTemplate and selectedSizeId
      let template = templates.value.find(template => template.id === 1);
      if(template) {
        template.elements = [];
        selectedTemplate.value = template;
      }
      return;

    }
    // Call the API and wait for the response

    const response = await getAPI(VmsTemplateApi).apiVmsVmstemplateDatalistIdGet(id);


    // Check if the response contains data and result is an array
    if (response && response.data && Array.isArray(response.data.result)) {
      // Find the design in the result array that matches the given ID
      const importedDesign = response.data.result.find((item: any) => item.id === id);

      // Check if the design was found
      if (importedDesign && importedDesign.jsonData) {
        // Parse the jsonData from the found design
        const designData = JSON.parse(importedDesign.jsonData);

        // Update the selectedTemplate and selectedSizeId
        selectedTemplate.value = designData;
        selectedSizeId.value = state.templateSizes.find((size: any) =>
          size.width === designData.width && size.height === designData.height
        )?.id || state.templateSizes.code;

      } else {
        console.error('No jsonData found for the specified design ID.');
      }
    } else {
      console.error('Invalid response structure or no results found.');
    }
  } catch (error) {
    console.error('Error importing design:', error);
  }
};

const editFormRef = ref();
const editFormInfoRef = ref();
const openDialog = (row: any) => {
  state.editForm = JSON.parse(JSON.stringify(row));
  state.selectedTabName = 'form-edit';
  state.isShowDialog = true;
  editFormRef.value?.resetFields();
  editFormInfoRef.value?.resetFields();
  importDesignFromJSON(row.id);

};

const closeDialog = () => {
  emits('handleQuery');
  state.isShowDialog = false;
  state.editForm = {};
};
const updateSelectedSize = () => {
  if (selectedTemplate.value && selectedSize.value) {
    selectedTemplate.value.elements.forEach((element: any) => {
      element.x = Math.min(element.x, selectedSize.value.width - element.width);
      element.y = Math.min(element.y, selectedSize.value.height - element.height);
      element.width = Math.min(element.width, selectedSize.value.width - element.x);
      element.height = Math.min(element.height, selectedSize.value.height - element.y);
    });
    updateElement(selectedTemplate.value);
  }
};
const submit = () => {
  // Tránh spam click nếu đang trong quá trình lưu
  if (state.loading) return;

  editFormRef.value.validate((valid: boolean) => {
    if (!valid) return;

    editFormInfoRef.value.validate(async (validInfo: boolean) => {
      if (!validInfo) return;

      // Bắt đầu quá trình lưu -> Bật loading
      state.loading = true;

      try {
        // Set the jsonData of the template
        state.editForm.jsonContent = JSON.stringify(selectedTemplate.value);
        
        // Find the selected size by selectedSizeId
        const selectedSize = state.templateSizes.find((size: any) => size.code === selectedSizeId.value);
        if (selectedSize) {
          // Assign the size label to state.editForm.size
          state.editForm.size = selectedSize.label;
        } else {
          console.error('Selected size not found');
        }

        // Proceed with the API call based on operateType
        if (props.operateType === 'edit') {
          await getAPI(VmsTemplateApi).apiVmsVmstemplateUpdatePost(state.editForm);
        } else if (props.operateType === 'copy' || props.operateType === 'add') {
          state.editForm.id = '';
          await getAPI(VmsTemplateApi).apiVmsVmstemplateAddPost(state.editForm);
        }

        // Show success message and close dialog
        ElMessage.success($t('lz.message.base.saveSuccess'));
        closeDialog();
      } catch (error) {
        console.error('Lỗi khi lưu dữ liệu:', error);
        // Có thể thêm ElMessage.error() ở đây nếu BE trả về lỗi
      } finally {
        // Luôn luôn tắt loading sau khi API chạy xong (dù lỗi hay thành công)
        state.loading = false;
      }
    });
  });
};

// Cancel
const cancel = () => {
  state.isShowDialog = false;
};


// Export object
defineExpose({ openDialog });

// Add new reactive refs for preview panel resizing
const previewWidth = ref(500);
const isResizing = ref(false);
const initialX = ref(0);
const initialWidth = ref(0);

// Add new methods for handling resize
const startResizeSplitter = (e: MouseEvent) => {
  isResizing.value = true;
  initialX.value = e.clientX;
  initialWidth.value = previewWidth.value;

  document.addEventListener('mousemove', handleResizeSplitter);
  document.addEventListener('mouseup', stopResizeSplitter);
};

const handleResizeSplitter = (e: MouseEvent) => {
  if (!isResizing.value) return;

  const delta = initialX.value - e.clientX;
  const newWidth = Math.max(300, Math.min(800, initialWidth.value + delta));
  previewWidth.value = newWidth;
};

const stopResizeSplitter = () => {
  isResizing.value = false;
  document.removeEventListener('mousemove', handleResizeSplitter);
  document.removeEventListener('mouseup', stopResizeSplitter);
};


</script>

<style scoped>
.editVmsTemplate-container {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.dialog-content {
  display: flex;
  gap: 0;
  /* Remove gap since we're using splitter */
  height: 450px;
  user-select: none;
  /* Prevent text selection while resizing */
}

.main-content {
  flex: 1;
  overflow-y: auto;
  overflow-x: hidden;
  padding-right: 10px;
}

.preview-container {
  transition: width 0.1s ease;
  max-width: 800px;
  overflow: hidden;
}

.preview {
  height: 100%;
  overflow: auto;
}

.preview-content {
  transition: transform 0.1s ease;
}

.template-size,
.element-controls {
  margin-bottom: 20px;
}

.element-control {
  margin-bottom: 20px;
}

.element-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 10px;
}

.element-content {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.element-position-size {
  display: flex;
  flex-wrap: wrap;
  gap: 60px;
  margin-top: 10px;
}

.input-group {
  display: flex;
  align-items: center;
}

.input-label {
  margin-right: 5px;
  font-weight: bold;
}

.uploaded-image-preview {
  display: flex;
  align-items: center;
  margin-top: 10px;
  padding: 10px;
  /* background-color: #f0f0f0; */
  border-radius: 4px;
}

.uploaded-image {
  width: 100px;
  height: 100px;
  object-fit: cover;
  margin-right: 10px;
  border-radius: 4px;
}

.design-container,
.preview-content {
  border: 1px solid #ccc;
  margin: 0 auto;
  overflow: hidden;
}

.element {
  position: absolute;
  box-sizing: border-box;
}

.element:hover {
  border-color: #3a8ee6;
}

.element-content {
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;
}

.element-label {
  position: absolute;
  top: 0;
  left: 0;
  background-color: rgba(0, 0, 0, 0.3);
  color: white;
  font-size: 10px;
  padding: 2px 4px;
}

.element-info {
  position: absolute;
  bottom: 0;
  left: 0;
  right: 0;
  background-color: rgba(0, 0, 0, 0.5);
  color: white;
  font-size: 12px;
  padding: 2px 4px;
  text-align: center;
}

.element-actions {
  position: absolute;
  top: 0;
  right: 0;
  display: flex;
  gap: 4px;
}

.element-actions button {
  background-color: white;
  border: 1px solid #3a8ee6;
  color: #3a8ee6;
  cursor: pointer;
  padding: 2px;
  font-size: 12px;
}

.element-actions button.active {
  background-color: #3a8ee6;
  color: white;
}

.splitter {
  margin-left: 5px;
  margin-right: 5px;
  width: 4px;
  background-color: #e4e7ed;
  cursor: col-resize;
  transition: background-color 0.2s;
}

.splitter:hover {
  background-color: #409eff;
}


.design-container {
  background: white;
  transition: transform 0.1s ease;
}

.element-content {
  display: flex;
  align-items: center;
  justify-content: center;
  text-align: center;
  width: 100%;
  height: 100%;
}

.preview-panel {
  height: 100%;
  display: flex;
  flex-direction: column;
}

.design-section,
.preview-section {
  height: 50%;
  overflow: hidden;
  padding: 10px;
}

.design-wrapper,
.preview-wrapper {
  height: calc(100% - 30px);
  /* Subtract header height */
  overflow: hidden;
  /* display: flex; */
  justify-content: center;
  align-items: center;
}

.design-container,
.preview-content {
  border: 1px solid #ccc;
  margin: 0 auto;
  overflow: hidden;
}

h3 {
  margin: 0 0 10px 0;
  height: 20px;
}
</style>