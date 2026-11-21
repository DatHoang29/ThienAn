<template>
  <div class="tms-equiptype-container">
    <el-dialog v-model="state.isShowDialog" draggable :close-on-click-modal="false" width="700px">
      <template #header>
        <div style="color: #fff">
          <el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle">
            <ele-Edit />
          </el-icon>
          <span> {{ props.title }} </span>
        </div>
      </template>
      <el-form :model="state.ruleForm" ref="ruleFormRef" label-width="auto" label-position="left">
        <el-row :gutter="10">
          <el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
            <el-form-item :label="$t('lz.entity.base.code')" prop="code"
              :rules="[{ required: true, message: $t('lz.validation.base.required',{PropertyName: $t('lz.entity.base.code')}), trigger: 'blur' }]">
              <el-input v-model="state.ruleForm.code" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.code')})"
                clearable />
            </el-form-item>
          </el-col>
          <el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
            <el-form-item :label="$t('lz.entity.base.name')" prop="name"
              :rules="[{ required: true, message: $t('lz.validation.base.required',{PropertyName: $t('lz.entity.base.name')}), trigger: 'blur' }]">
              <el-input v-model="state.ruleForm.name" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.name')})"
                clearable />
            </el-form-item>
          </el-col>
          <el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
            <el-form-item :label="$t('lz.entity.tms.module')" prop="moduleId"
              :rules="[{ required: false, message: $t('lz.validation.base.required',{PropertyName: $t('lz.entity.tms.module')}), trigger: 'blur' }]">
              <el-select v-model="state.ruleForm.moduleId" :placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.entity.tms.module')})" filterable clearable class="w100">
                <el-option 
                  v-for="item in state.ModuleList" 
                  :key="item.id" 
                  :label="item.isDelete != null ? `${item.name} (${$t('lz.label.base.deleted')})` : item.name" 
                  :value="item.id" 
                  :disabled="item.status !== (StatusTypeEnum.Enable as number) || item.isDelete != null"
                >
                  <span :style="{ color: item.isDelete != null ? 'var(--el-text-color-secondary)' : 'inherit', textDecoration: item.isDelete != null ? 'line-through' : 'none' }">
                    {{ item.isDelete != null ? `${item.name} (${$t('lz.label.base.deleted')})` : item.name }}
                  </span>
                </el-option>
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
            <el-form-item :label="$t('lz.entity.base.size')" prop="size"
              :rules="[{ required: false, message: $t('lz.validation.base.required',{PropertyName: $t('lz.entity.base.size')}), trigger: 'blur' }]"
              >
              <el-select v-model="state.ruleForm.size" :placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.entity.base.size')})" filterable clearable class="w100">
                <el-option v-for="item in getConfigDataByCode(BaseConfigTypeEnum.TmsEquipIconSize)" :key="item.code" :label="item.name" :value="item.code" />
              </el-select>
            </el-form-item>
          </el-col>
          <!-- Mã modal: quyết định modal xem/điều khiển mở trên bigscreen; bỏ trống = modal thông tin thiết bị mặc định -->
          <el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
            <el-form-item :label="$t('lz.entity.tmsEquipmentType.modalCode')" prop="modalCode">
              <el-select v-model="state.ruleForm.modalCode" :placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.entity.tmsEquipmentType.modalCode')})" filterable clearable class="w100">
                <el-option v-for="item in getConfigDataByCode(BaseConfigTypeEnum.TmsEquipmentModalType)" :key="item.code" :label="item.name" :value="item.code" />
              </el-select>
            </el-form-item>
          </el-col>

          <el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
            <el-form-item :label="$t('lz.entity.base.status')" prop="status"
              :rules="[{ required: false, message: $t('lz.validation.base.required',{PropertyName: $t('lz.entity.base.status')}), trigger: 'blur' }]">
              <el-radio-group v-model="state.ruleForm.status" v-for="item in getConfigDataByCode(BaseConfigTypeEnum.SysStatusType)" :key="item.value">
                <el-radio :value="parseInt(item.code)" class="mr5">{{ item.name }}</el-radio>
              </el-radio-group>
            </el-form-item>
          </el-col>
        <el-col :xs="12" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
          <el-form-item :label="$t('lz.entity.tmsEquipmentType.defaultIcon')">
            <div class="icon-upload-wrapper">
              <el-upload
                ref="defaultUploadRef"
                class="avatar-uploader"
                :show-file-list="false"
                :auto-upload="false"
                accept=".jpg,.png,.svg"
                action=""
                :limit="1"
                :on-change="(file: any) => handleUploadChange(file, 'defaultIcon')"
              >
                <img v-if="state.ruleForm.defaultIcon" :src="state.defaultIcon" class="avatar" />
                <SvgIcon v-else class="avatar-uploader-icon" name="ele-Plus" :size="28" />
              </el-upload>

              <div v-if="state.ruleForm.defaultIcon" class="icon-delete-btn"
                @click.stop="removeIcon('defaultIcon')"
              >
                ×
              </div>
            </div>
          </el-form-item>
        </el-col>

        <el-col :xs="12" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
          <el-form-item :label="$t('lz.entity.tmsEquipmentType.activeIcon')">
            <div class="icon-upload-wrapper">
              <el-upload
                ref="activeUploadRef"
                class="avatar-uploader"
                :show-file-list="false"
                :auto-upload="false"
                accept=".jpg,.png,.svg"
                action=""
                :limit="1"
                :on-change="(file: any) => handleUploadChange(file, 'activeIcon')"
              >
                <img v-if="state.ruleForm.activeIcon" :src="state.activeIcon" class="avatar" />
                <SvgIcon v-else class="avatar-uploader-icon" name="ele-Plus" :size="28" />
              </el-upload>

              <div v-if="state.ruleForm.activeIcon" class="icon-delete-btn"
                @click.stop="removeIcon('activeIcon')"
              >
                ×
              </div>
            </div>
          </el-form-item>
        </el-col>

        <el-col :xs="12" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
          <el-form-item :label="$t('lz.entity.tmsEquipmentType.inactiveIcon')">
            <div class="icon-upload-wrapper">
              <el-upload
                ref="inactiveUploadRef"
                class="avatar-uploader"
                :show-file-list="false"
                :auto-upload="false"
                accept=".jpg,.png,.svg"
                action=""
                :limit="1"
                :on-change="(file: any) => handleUploadChange(file, 'inactiveIcon')"
              >
                <img v-if="state.ruleForm.inactiveIcon" :src="state.inactiveIcon" class="avatar" />
                <SvgIcon v-else class="avatar-uploader-icon" name="ele-Plus" :size="28" />
              </el-upload>

              <div v-if="state.ruleForm.inactiveIcon" class="icon-delete-btn"
                @click.stop="removeIcon('inactiveIcon')"
              >
                ×
              </div>
            </div>
          </el-form-item>
        </el-col>

        <el-col :xs="12" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
          <el-form-item :label="$t('lz.entity.tmsEquipmentType.faultIcon')">
            <div class="icon-upload-wrapper">
              <el-upload
                ref="faultUploadRef"
                class="avatar-uploader"
                :show-file-list="false"
                :auto-upload="false"
                accept=".jpg,.png,.svg"
                action=""
                :limit="1"
                :on-change="(file: any) => handleUploadChange(file, 'faultIcon')"
              >
                <img v-if="state.ruleForm.faultIcon" :src="state.faultIcon" class="avatar" />
                <SvgIcon v-else class="avatar-uploader-icon" name="ele-Plus" :size="28" />
              </el-upload>

              <div v-if="state.ruleForm.faultIcon" class="icon-delete-btn"
                @click.stop="removeIcon('faultIcon')"
              >
                ×
              </div>
            </div>
          </el-form-item>
        </el-col>


		  	<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
					<el-form-item :label="$t('lz.entity.base.remark')">
						<el-input v-model="state.ruleForm.remark" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.remark')})" clearable  type="textarea" />
					</el-form-item>
			</el-col>
        </el-row>
      </el-form>
      <template #footer>
        <span class="dialog-footer">
          <el-button @click="cancel">{{ $t('lz.button.base.cancel') }}</el-button>
          <el-button type="primary" @click="submit">{{ $t('lz.button.base.confirm') }}</el-button>
        </span>
      </template>
    </el-dialog>
  </div>
</template>

<script lang="ts" setup name="tmsEditEquipType">
import { computed, onMounted, reactive, ref } from 'vue';
import { getAPI } from '/@/utils/axios-utils';
import { TmsEquipmentTypeApi, TmsModulesApi } from '/@/api-services/tms';
import { UpdateEquipmentTypesInput } from '/@/api-services/tms';
import { OperateType } from '/@/types/constants';
import { ElMessage, UploadInstance } from 'element-plus';
import { useItsMediaServerStore } from '/@/stores/its/itsMediaServer';

import { BaseConfigTypeEnum, StatusTypeEnum } from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;

const defaultUploadRef = ref<UploadInstance>();
const activeUploadRef = ref<UploadInstance>();
const inactiveUploadRef = ref<UploadInstance>();
const faultUploadRef = ref<UploadInstance>();

import { useI18n } from 'vue-i18n';
const { t: $t } = useI18n();

interface Props {
  /** the type of operation */
  operateType: OperateType;
  	title: string;

}

const props = defineProps<Props>();
const emits = defineEmits(['handleQuery']);

const ruleFormRef = ref();
const state = reactive({
  isShowDialog: false,
  file: undefined as any, 
  ruleForm: {} as UpdateEquipmentTypesInput,
  ModuleList: [] as any[],
  baseUrl: '',
  activeIcon: '',
  inactiveIcon: '',
  faultIcon: '',
  defaultIcon: '',
});


const loadModuleList = async () => {
  let resDicData = await getAPI(TmsModulesApi).apiTmsTmsmodulesListGet();
  state.ModuleList = resDicData.data.result || [];
};

onMounted(async () => {
  await loadModuleList();
  // Địa chỉ file server dùng chung (chỉ gọi API 1 lần cho toàn app)
  state.baseUrl = await useItsMediaServerStore().ensureLoaded();
});

const beforeUpload = (file: File) => {
  // Return false to prevent auto upload
  return false;
};
const handleUploadChange = (file: any, iconType: string) => {
  const reader = new FileReader();
  reader.readAsDataURL(file.raw);

  reader.onload = () => {
    const base64String = reader.result as string;

    switch (iconType) {
      case 'defaultIcon':
        defaultUploadRef.value?.clearFiles();
        state.ruleForm.defaultIcon = base64String;
        state.defaultIcon = base64String;
        break;
      case 'activeIcon':
        activeUploadRef.value?.clearFiles();
        state.ruleForm.activeIcon = base64String;
        state.activeIcon = base64String;
        break;
      case 'inactiveIcon':
        inactiveUploadRef.value?.clearFiles();
        state.ruleForm.inactiveIcon = base64String;
        state.inactiveIcon = base64String;
        break;
      case 'faultIcon':
        faultUploadRef.value?.clearFiles();
        state.ruleForm.faultIcon = base64String;
        state.faultIcon = base64String;
        break;
      default:
        break;
    }
  };

  reader.onerror = (error) => {
    console.error('Error converting file to base64:', error);
  };
};

const removeIcon = (iconType: string) => {
  switch (iconType) {
    case 'defaultIcon':
      state.ruleForm.defaultIcon = '';
      state.defaultIcon = '';
      defaultUploadRef.value?.clearFiles();
      break;
    case 'activeIcon':
      state.ruleForm.activeIcon = '';
      state.activeIcon = '';
      activeUploadRef.value?.clearFiles();
      break;
    case 'inactiveIcon':
      state.ruleForm.inactiveIcon = '';
      state.inactiveIcon = '';
      inactiveUploadRef.value?.clearFiles();
      break;
    case 'faultIcon':
      state.ruleForm.faultIcon = '';
      state.faultIcon = '';
      faultUploadRef.value?.clearFiles();
      break;
    default:
      break;
  }
};

const openDialog = async (row: any) => {
  if (!state.ModuleList || state.ModuleList.length === 0) {
    await loadModuleList();
  }
  state.ruleForm = JSON.parse(JSON.stringify(row));
  state.isShowDialog = true;
  // Add base url
  state.defaultIcon = state.ruleForm.defaultIcon ? state.baseUrl + '/' + state.ruleForm.defaultIcon : '';
  state.activeIcon = state.ruleForm.activeIcon ? state.baseUrl + '/' + state.ruleForm.activeIcon : '';
  state.inactiveIcon = state.ruleForm.inactiveIcon ? state.baseUrl + '/' + state.ruleForm.inactiveIcon : '';
  state.faultIcon = state.ruleForm.faultIcon ? state.baseUrl + '/' + state.ruleForm.faultIcon : '';
  ruleFormRef.value?.resetFields();
};

const closeDialog = () => {
  emits('handleQuery');
  state.isShowDialog = false;
};

const cancel = () => {
  state.isShowDialog = false;
};

const submit = () => {
  ruleFormRef.value.validate(async (valid: boolean) => {
    if (!valid) return;
    console.log('submit', state.ruleForm);
    // Edit
    if (props.operateType === 'edit') {
      await getAPI(TmsEquipmentTypeApi).apiTmsTmsequipmenttypeUpdatePost({ ...state.ruleForm });
    }
    // Copy or Add
    else if (props.operateType === 'copy' || props.operateType === 'add') {
      state.ruleForm.id = '';
      await getAPI(TmsEquipmentTypeApi).apiTmsTmsequipmenttypeAddPost({ ...state.ruleForm });
    }
    ElMessage.success($t('lz.message.base.saveSuccess'));
    closeDialog();
  });
};

defineExpose({ openDialog });
</script>



<style scoped>
.avatar-uploader .avatar {
	width: 100px;
	height: 100px;
	display: block;
	object-fit: contain;
}

:deep(.avatar-uploader) .el-upload {
	border: 1px dashed var(--el-border-color);
	cursor: pointer;
	position: relative;
	overflow: hidden;
	transition: var(--el-transition-duration-fast);
}

:deep(.avatar-uploader) .el-upload:hover {
	border-color: var(--el-color-primary);
}

.el-icon.avatar-uploader-icon {
	color: #8c939d;
	width: 100px;
	height: 100px;
	text-align: center;
}

.icon-upload-wrapper {
	position: relative;
	display: inline-block;
}

.icon-delete-btn {
	position: absolute;
	top: 6px;
	right: 6px;
	width: 20px;
	height: 20px;
	border-radius: 50%;
	background: rgb(0, 0, 0);
	color: #fff;
	font-size: 14px;
	line-height: 20px;
	text-align: center;
	cursor: pointer;
	z-index: 10;
	user-select: none;
}

.icon-delete-btn:hover {
	background: rgba(255, 0, 0, 0.8);
}
</style>

