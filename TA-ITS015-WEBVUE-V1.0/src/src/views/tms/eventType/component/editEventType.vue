<template>
	<div class="tms-eventtype-container">
		<el-dialog v-model="state.isShowDialog" draggable :close-on-click-modal="false" width="700px">
			<template #header>
				<div style="color: #fff">
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Edit />
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
					<el-col :xs="24" :sm="12" :md="12" :lg="24" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.base.type')" prop="type"
							:rules="[{ required: true, pattern: /^\s*(event|incident)\s*$/i, message: $t('lz.validation.tmsEventType.kind'), trigger: 'change' }]">
							<el-radio-group v-model="state.ruleForm.type">
								<el-radio v-for="item in props.typeList" :key="item.code" :value="item.code">
									{{ $t(`lz.param.event_group.${item.code}`, item.name) }}
								</el-radio>
							</el-radio-group>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.tmsEventType.warningType')">
							<el-radio-group v-model="state.ruleForm.warningType">
								<el-radio v-for="item in props.warningTypeList" :key="item.code" :value="item.code">
									{{ $t(`lz.param.event_warning_type.${item.code}`, item.name) }}
								</el-radio>
							</el-radio-group>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item :label="$t('lz.entity.tms.module')">
							<el-select v-model="state.ruleForm.moduleId" 
								:placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.entity.tms.module')})"
								filterable clearable class="w100">
								<el-option 
									v-for="item in props.moduleList" 
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
						<el-form-item :label="$t('lz.entity.base.size')" prop="size">
							<el-select v-model="state.ruleForm.size" :placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.entity.base.size')})" filterable clearable class="w100">
								<el-option v-for="item in getConfigDataByCode(BaseConfigTypeEnum.TmsEquipIconSize)" :key="item.code" :label="item.name" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item :label="$t('lz.entity.base.priority')">
							<el-input-number 
								id="inputNumber"
								v-model="state.ruleForm.priority" 
								:min="0" 
								:max="999"
								controls-position="right"
								size="medium"
								style="width: 100px;"
							></el-input-number>	
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.label.vms.playMode')">
							<el-checkbox
								v-model="state.ruleForm.defaultPlayMode"
								true-value="append"
								false-value="override"
							>
								{{ $t('lz.label.vms.playModeAppendCheckbox') }}
							</el-checkbox>
							<el-tooltip :content="$t('lz.label.vms.playModeTooltip')" placement="top">
								<el-icon class="ml5" style="vertical-align: middle; cursor: pointer; color: #909399;"><ele-QuestionFilled /></el-icon>
							</el-tooltip>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.base.status')">
							<el-radio-group v-model="state.ruleForm.status" v-for="item in getConfigDataByCode(BaseConfigTypeEnum.SysStatusType)" :key="item.code">
								<el-radio :value="parseInt(item.code)" class="mr5">{{ item.name }}</el-radio>
							</el-radio-group>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item :label="$t('lz.entity.base.remark')">
							<el-input v-model="state.ruleForm.remark" 
							:placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.remark')})" 
							clearable type="textarea" />
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
									:on-change="(file: any) => handleUploadChange(file, 'url')">
									<img v-if="state.ruleForm.url" :src="state.url" class="avatar" />
									<SvgIcon v-else class="avatar-uploader-icon" name="ele-Plus" :size="28" />
								</el-upload>
								<div v-if="state.ruleForm.url" class="icon-delete-btn" @click.stop="removeIcon('url')">
									×
								</div>
							</div>
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>
			<template #footer>
				<span class="dialog-footer">
					<el-button @click="cancel">{{ $t('lz.button.base.cancel') }}</el-button>
					<el-button type="primary" @click="submit">{{ $t('lz.button.base.submit') }}</el-button>
				</span>
			</template>
		</el-dialog>
	</div>
</template>

<script lang="ts" setup name="tmsEditEventType">
import { computed, onMounted, reactive, ref } from 'vue';
import { getAPI } from '/@/utils/axios-utils';
import { TmsEventTypeApi,  UpdateEventTypeInput } from '/@/api-services/tms';
import { OperateType } from '/@/types/constants';
import { ElMessage,UploadInstance} from 'element-plus';
import { useI18n } from 'vue-i18n';
import { useItsMediaServerStore } from '/@/stores/its/itsMediaServer';
import { useBaseConfig } from '/@/stores/baseConfig';
import { BaseConfigTypeEnum, StatusTypeEnum } from '/@/types/enums';
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;

const { t: $t } = useI18n();
interface Props {
	/** the type of operation */
	operateType: OperateType;
	title: string;
	typeList: any[];
	warningTypeList: any[];
	moduleList: any[];
}
const defaultUploadRef = ref<UploadInstance>();

const props = defineProps<Props>();
const emits = defineEmits(['handleQuery']);

const ruleFormRef = ref();
const state = reactive({
	isShowDialog: false,
	ruleForm: {} as UpdateEventTypeInput,
	url: '',
	baseUrl: '',

});


// Page initialization
onMounted(async () => {
  // Địa chỉ file server dùng chung (chỉ gọi API 1 lần cho toàn app)
  state.baseUrl = await useItsMediaServerStore().ensureLoaded();
});
// Open dialog
const openDialog = (row: any) => {
	state.ruleForm = JSON.parse(JSON.stringify(row));
	if (!state.ruleForm.defaultPlayMode) {
		state.ruleForm.defaultPlayMode = 'override';
	}
	state.isShowDialog = true;
	state.url = state.baseUrl + '/' + state.ruleForm.url;
	ruleFormRef.value?.resetFields();
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
		const payload = {
			...state.ruleForm,
			moduleId: state.ruleForm.moduleId || null,
			priority: state.ruleForm.priority !== undefined && state.ruleForm.priority !== null && (state.ruleForm.priority as any) !== '' ? state.ruleForm.priority : null,
			remark: state.ruleForm.remark || null,
		};
		//Edit
		if (props.operateType == 'edit') {
			await getAPI(TmsEventTypeApi).apiTmsTmseventtypeUpdatePost(payload);
		}
		//Copy or Add
		else if (props.operateType == 'copy' || props.operateType == 'add') {
			payload.id = '';
			await getAPI(TmsEventTypeApi).apiTmsTmseventtypeAddPost(payload);

		}
		ElMessage.success($t('lz.message.base.submitSuccess'));
		closeDialog();
	});
};

const handleUploadChange = (file: any, iconType: string) => {
  defaultUploadRef.value!.clearFiles();
  const reader = new FileReader();
  reader.readAsDataURL(file.raw);

  reader.onload = () => {
    const base64String = reader.result as string;

    switch (iconType) {
      case 'url':
        state.ruleForm.url = base64String;
        state.url = base64String;
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
    case 'url':
      state.ruleForm.url = '';
      state.url = '';
      defaultUploadRef.value?.clearFiles();
      break;
    default:
      break;
  }
};


// Export object
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