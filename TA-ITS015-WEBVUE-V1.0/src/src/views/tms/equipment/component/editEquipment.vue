<template>
	<div class="tms-equipment-container">
		<el-dialog v-model="state.isShowDialog" draggable :close-on-click-modal="false" width="700px" destroy-on-close @close="onDialogClose" @opened="onDialogOpened">
			<template #header>
				<div style="color: #fff">
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Edit />
					</el-icon>
					<span> {{ props.title }} </span>
				</div>
			</template>			
			<el-tabs v-loading="state.loading" v-model="state.selectedTabName">
				<el-tab-pane :label="$t('lz.label.base.basicInfo')" style="height: 450px; overflow-y: auto; overflow-x: hidden" name="tab1">
					<el-form :model="state.ruleForm" ref="ruleFormRef" label-width="auto"  label-position="left" autocomplete="off" >
						<el-row :gutter="10" >
							<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
								<el-form-item :label="$t('lz.entity.base.code')" prop="code"
									 :rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.base.code') }), trigger: 'blur' }]">
									<el-input v-model="state.ruleForm.code" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.code')})"
										clearable />
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
								<el-form-item :label="$t('lz.entity.base.name')" prop="name"
									:rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.base.name') }), trigger: 'blur' }]">
									<el-input v-model="state.ruleForm.name" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.name')})"
										clearable />
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
								<el-form-item :label="$t('lz.entity.tms.equipmentType')" prop="equipmentTypeId">
									<el-select v-model="state.ruleForm.equipmentTypeId" :placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.entity.tms.equipmentType')})" filterable clearable class="w100">
										<el-option 
											v-for="item in typeOptions" :key="item.id" :label="item.isDelete != null ? `${item.name}` : item.name" :value="item.id"
											:disabled="item.isDelete != null || item.status !== (StatusTypeEnum.Enable as number)"
										>
											<span :style="{ color: item.isDelete != null ? 'var(--el-text-color-secondary)' : 'inherit' , textDecoration: item.isDelete != null ? 'line-through' : 'none' }">
												{{ item.isDelete != null ? `${item.name}` : item.name }}
											</span>
										</el-option>
									</el-select>
								</el-form-item>
							</el-col>
						
							<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
								<el-form-item :label="$t('lz.entity.tms.equipmentState')" prop="equipmentState">
									<el-select v-model="state.ruleForm.equipmentState" :placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.entity.tms.equipmentState')})" filterable clearable class="w100">
										<el-option v-for="item in getConfigDataByCode(BaseConfigTypeEnum.TmsEquipmentState)" :key="item.code" :label="item.name" :value="parseInt(item.code)" />
									</el-select>
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="$t('lz.entity.base.status')" prop="status"
										:rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.base.status') }), trigger: 'change' }]">
										<el-radio-group v-model="state.ruleForm.status"> <el-radio 
												v-for="item in getConfigDataByCode(BaseConfigTypeEnum.SysStatusType)"  :key="item.code" :value="parseInt(item.code)" class="mr5">
												{{ item.name }}
											</el-radio>
										</el-radio-group>
									</el-form-item>
								</el-col>
							<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
								<el-form-item :label="$t('lz.entity.base.ipAddress')" prop="ip"
									:rules="[{ required: false, message: $t('lz.validation.base.required',{PropertyName: $t('lz.entity.base.ipAddress')}), trigger: 'blur' }]">
									<el-input v-model="state.ruleForm.ip" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.ipAddress')})"
										clearable />
								</el-form-item>
							</el-col>
							
							<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
								<el-form-item :label="$t('lz.entity.base.port')" prop="port"
									:rules="[{ required: false, message: $t('lz.validation.base.required',{PropertyName: $t('lz.entity.base.port')}), trigger: 'blur' }]">
									<el-input v-model="state.ruleForm.port" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.port')})"
										clearable />
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
								<el-form-item :label="$t('lz.entity.base.username')" prop="username"
									:rules="[{ required: false, message: $t('lz.validation.base.required',{PropertyName: $t('lz.entity.base.username')}), trigger: 'blur' }]">
									<el-input v-model="state.ruleForm.username" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.username')})"
										clearable autocomplete="off"  />
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
								<el-form-item 
									:label="$t('lz.entity.base.password')" 
									prop="password"
									:rules="[{ required: false, message: $t('lz.validation.base.required',{PropertyName: $t('lz.entity.base.password')}), trigger: 'blur' }]">
									<el-input 
									type="password" 
									autocomplete="off"
									v-model="state.ruleForm.password" 
									:placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.password')})"
									clearable 
									show-password
									/>
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
								<el-form-item :label="$t('lz.entity.base.url')" prop="url"
									:rules="[{ required: false, message: $t('lz.validation.base.required',{PropertyName: $t('lz.entity.base.url')}), trigger: 'blur' }, { message: $t('lz.validation.base.maxlength', {PropertyName: $t('lz.entity.base.url'), MaxLength: 128}), trigger: 'blur'}]">
									<el-input v-model="state.ruleForm.url" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.url')})"
										clearable />
								</el-form-item>
							</el-col>
								<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="$t('lz.entity.tms.isScan')" prop="isScan">
									<el-checkbox v-model="state.ruleForm.isScan" />
								</el-form-item>
							</el-col>
						</el-row>
					</el-form>
				</el-tab-pane>
				<el-tab-pane :label="$t('lz.label.base.position')" style="height: 450px; overflow-y: auto; overflow-x: hidden" name="tab2">
					<el-form :model="state.ruleForm" ref="ruleFormRef" label-width="auto"  label-position="left" >
						<el-row :gutter="10" >
							<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
								<el-form-item :label="$t('lz.entity.tms.lane')" prop="laneId">
									<el-select v-model="state.ruleForm.laneId" :placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.entity.tms.lane')})" filterable clearable class="w100">
										<el-option v-for="item in getConfigDataByCode(BaseConfigTypeEnum.TmsLaneType)" :key="item.code" :label="item.name" :value="item.code" />
									</el-select>
								</el-form-item>

							</el-col>
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="$t('lz.entity.tms.kmNumber')" prop="kmNumber">
									<el-input-number v-model="state.ruleForm.kmNumber" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.tms.kmNumber')})" class="w100" :min="0"/>
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="$t('lz.entity.tms.metNumber')" prop="metNumber">
									<el-input-number v-model="state.ruleForm.metNumber" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.tms.metNumber')})" class="w100" :min="0" :max="999"/>
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
								<el-form-item :label="$t('lz.entity.tms.direction')" prop="directionId">
									<el-select v-model="state.ruleForm.directionId" :placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.entity.tms.direction')})" filterable clearable class="w100">
										<el-option v-for="item in getConfigDataByCode(BaseConfigTypeEnum.TmsDirectionType)" :key="item.code" :label="item.name" :value="parseInt(item.code)" />
									</el-select>
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
								<el-form-item :label="$t('lz.entity.tms.activeDirection')" prop="activeDirection">
									<el-select v-model="state.ruleForm.activeDirection" :placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.entity.tms.activeDirection')})" filterable clearable class="w100">
										<el-option v-for="item in getConfigDataByCode(BaseConfigTypeEnum.TmsActiveDirectionType)" :key="item.code" :label="item.name" :value="parseInt(item.code)" />
									</el-select>

								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
								<el-form-item :label="$t('lz.entity.tms.location')" prop="location">
									<el-select v-model="state.ruleForm.location" :placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.entity.tms.location')})" filterable clearable class="w100">
										<el-option v-for="item in getConfigDataByCode(BaseConfigTypeEnum.TmsZoneLocation)" :key="item.code" :label="item.name" :value="item.code" />
									</el-select>
								</el-form-item>

							</el-col>
						</el-row>
					</el-form>
				</el-tab-pane>
				<el-tab-pane :label="$t('lz.label.base.otherInfo')" style="height: 450px; overflow-y: auto; overflow-x: hidden" name="tab3">
					<el-form :model="state.ruleForm" ref="ruleFormRef" label-width="auto"  label-position="left" >
						<el-row :gutter="10" >
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="$t('lz.entity.base.orderNo')" prop="orderNo">
									<el-input-number v-model="state.ruleForm.orderNo" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.orderNo')})" class="w100" :min="0"/>
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
								<el-form-item :label="$t('lz.entity.base.remark')">
									<el-input v-model="state.ruleForm.remark"
										:placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.remark')})" clearable type="textarea" />
								</el-form-item>
							</el-col>
						</el-row>
					</el-form>
				</el-tab-pane>
				<!-- Tab cấu hình metadata dạng key-value, lưu JSON vào cột config -->
				<el-tab-pane :label="$t('lz.label.tms.config')" style="height: 450px; overflow-y: auto; overflow-x: hidden" name="tab4">
					<el-form :model="state" ref="configFormRef" label-width="auto" label-position="left">
						<el-row :gutter="10">
							<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
								<el-button type="primary" icon="ele-Plus" size="small" @click="addConfigRow">{{ $t('lz.button.base.addConfiguration') }}</el-button>
							</el-col>
							<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
								<template v-if="state.configList != undefined && state.configList.length > 0">
									<el-row :gutter="10" v-for="(item, index) in state.configList" :key="index">
										<el-col :xs="24" :sm="11" :md="11" :lg="11" :xl="11" class="mb20">
											<el-form-item :label="$t('lz.entity.base.key')" :prop="`configList[${index}].key`"
												:rules="[{ required: true, message: $t('lz.validation.base.required', {PropertyName: 'Key'}), trigger: 'blur' }]">
												<template #label>
													<el-button icon="ele-Delete" type="danger" circle plain size="small" @click="deleteConfigRow(index)" />
													<span class="ml5">{{ $t('lz.entity.base.key') }}</span>
												</template>
												<el-input v-model="state.configList[index].key" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.key') })" clearable />
											</el-form-item>
										</el-col>
										<el-col :xs="24" :sm="13" :md="13" :lg="13" :xl="13" class="mb20">
											<el-form-item :label="$t('lz.entity.base.value')" :prop="`configList[${index}].value`"
												:rules="[{ required: true, message: $t('lz.validation.base.required', {PropertyName: 'Value'}), trigger: 'blur' }]">
												<el-input v-model="state.configList[index].value" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.value') })" clearable />
											</el-form-item>
										</el-col>
									</el-row>
								</template>
								<el-empty :image-size="70" :description="$t('lz.label.base.noData')" v-else></el-empty>
							</el-col>
						</el-row>
					</el-form>
				</el-tab-pane>
			</el-tabs>
			<template #footer>
				<span class="dialog-footer">
					<el-button @click="cancel">{{ $t('lz.button.base.cancel') }}</el-button>
					<el-button type="primary" @click="submit">{{ $t('lz.button.base.confirm') }}</el-button>
				</span>
			</template>
		</el-dialog>
	</div>
</template>

<script lang="ts" setup name="tmsEditEquip">
import { computed, nextTick, onMounted, reactive, ref } from 'vue';
import { getAPI } from '/@/utils/axios-utils';
import { TmsEquipmentApi, TmsEquipmentTypeApi } from '/@/api-services/tms';
import { UpdateEquipmentInput } from '/@/api-services/tms';
import { OperateType } from '/@/types/constants';
import { ElMessage } from 'element-plus';

import { BaseConfigTypeEnum, StatusTypeEnum } from '/@/types/enums/baseEnum';
import { useBaseConfig } from '/@/stores/baseConfig';
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;

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
const configFormRef = ref();
const state = reactive({
	isShowDialog: false,
	ruleForm: {} as UpdateEquipmentInput,
	typeList: [] as any,
	tabActiveTab: 'tab1',
	loading: false,
	selectedTabName: 'tab1',
	configList: [] as { key: string; value: string }[],
});

// Danh sách chọn chỉ gồm loại thiết bị chưa xoá VÀ đang hoạt động (không còn dòng xám/vô hiệu), cả thêm mới và cập nhật.
// Ngoại lệ duy nhất: thiết bị đang sửa gắn loại đang tạm ngưng (chưa xoá) thì giữ đúng loại đó để ô chọn hiện tên thay vì mã id.
// Danh sách vẫn tải kèm bản ghi đã xoá (includeDelete) chỉ để nhận biết thiết bị đang gắn loại đã xoá và bỏ giá trị đó khỏi form.
const typeOptions = computed(() =>
	(state.typeList || []).filter(
		(item: any) => item.isDelete == null && (item.status === (StatusTypeEnum.Enable as number) || item.id === state.ruleForm.equipmentTypeId)
	)
);

onMounted(async () => {
	let resDicData = await getAPI(TmsEquipmentTypeApi).apiTmsTmsequipmenttypeListGet( undefined, undefined, undefined, undefined, undefined, true as any);
	state.typeList = resDicData.data.result;
});

// Clear validation khi đóng dialog
const onDialogClose = () => {
	ruleFormRef.value?.clearValidate();
	configFormRef.value?.clearValidate();
};

// Clear validation khi dialog mở hoàn tất
const onDialogOpened = () => {
	nextTick(() => {
		ruleFormRef.value?.clearValidate();
		configFormRef.value?.clearValidate();
	});
};

// Open dialog
const openDialog = (row: any) => {
	ruleFormRef.value?.clearValidate();
	configFormRef.value?.clearValidate();

	state.ruleForm = JSON.parse(JSON.stringify(row ?? {}));
	// Thiết bị đang gắn loại đã xoá: không hiển thị loại đó (ô trống, bắt chọn lại loại còn dùng)
	if (state.ruleForm.equipmentTypeId && (state.typeList || []).some((item: any) => item.id === state.ruleForm.equipmentTypeId && item.isDelete != null)) {
		state.ruleForm.equipmentTypeId = undefined as any;
	}
	// Tích chọn sẵn khi thêm mới
    if (props.operateType === 'add' || state.ruleForm.isScan === undefined || state.ruleForm.status === null) {
        state.ruleForm.isScan = row?.isScan ?? true;
		state.ruleForm.status = row?.status ?? (StatusTypeEnum.Enable as number);
    }

	// Parse config JSON thành danh sách key-value
	state.configList = parseConfigToList(state.ruleForm.config);
	state.selectedTabName = 'tab1';
	state.isShowDialog = true;

	nextTick(() => {
		ruleFormRef.value?.clearValidate();
		configFormRef.value?.clearValidate();
	});
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
		// Serialize configList thành JSON string lưu vào config
		state.ruleForm.config = serializeListToConfig(state.configList);
		//Edit
		if (props.operateType == 'edit') {
			await getAPI(TmsEquipmentApi).apiTmsTmsequipmentUpdatePost(state.ruleForm);
		}
		//Copy or Add
		else if (props.operateType == 'copy' || props.operateType == 'add') {
			state.ruleForm.id = '';
			await getAPI(TmsEquipmentApi).apiTmsTmsequipmentAddPost(state.ruleForm);

		}
		ElMessage.success($t('lz.message.base.saveSuccess'));
		closeDialog();
	});
};

// --- Config key-value helpers ---

/** Parse JSON string từ config thành mảng key-value */
const parseConfigToList = (configJson: string | null | undefined): { key: string; value: string }[] => {
	if (!configJson) return [];
	try {
		const obj = JSON.parse(configJson);
		return Object.entries(obj).map(([key, value]) => ({ key, value: String(value ?? '') }));
	} catch {
		return [];
	}
};

/** Serialize mảng key-value thành JSON string */
const serializeListToConfig = (list: { key: string; value: string }[]): string | null => {
	if (!list || list.length === 0) return null;
	const obj: Record<string, string> = {};
	list.forEach((item) => {
		if (item.key) {
			obj[item.key] = item.value;
		}
	});
	return Object.keys(obj).length > 0 ? JSON.stringify(obj) : null;
};

/** Thêm 1 dòng cấu hình mới */
const addConfigRow = () => {
	state.configList.push({ key: '', value: '' });
};

/** Xoá 1 dòng cấu hình */
const deleteConfigRow = (index: number) => {
	state.configList.splice(index, 1);
};

// Export object
defineExpose({ openDialog });
</script>
