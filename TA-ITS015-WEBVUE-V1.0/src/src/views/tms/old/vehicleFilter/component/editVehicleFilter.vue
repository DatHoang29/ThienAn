<template>
	<div class="editVehicleFilter-container">
		<el-dialog v-model="state.isShowDialog" draggable :close-on-click-modal="false" width="700px">
			<template #header>
				<div style="color: #fff">
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"><ele-Edit /></el-icon>
					<span> {{ props.title }} </span>
				</div>
			</template>
			<el-form :model="state.editForm" ref="editFormRef" label-width="auto"  label-position="left">
				<el-row :gutter="10">
					<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.tms.licensePlate')" prop="licensePlate" :rules="[{ required: true, message: $t('lz.validation.base.required', {PropertyName: $t('lz.entity.tms.licensePlate')}), trigger: 'blur' }, { max: EntityFieldLength.Length32, message: $t('lz.validation.base.maxlength', {PropertyName: $t('lz.entity.tms.licensePlate'), MaxLength: EntityFieldLength.Length32}), trigger: 'blur'}]">
							<el-input v-model="state.editForm.licensePlate" :placeholder="$t('lz.label.base.pleaseType', {input: $t('lz.entity.tms.licensePlate')})" :maxlength="EntityFieldLength.Length32" show-word-limit clearable />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.base.type')" prop="type" :rules="[{ required: true, message: $t('lz.validation.base.required', {PropertyName: $t('lz.entity.base.type')}), trigger: 'blur' }]">
							<el-select v-model="state.editForm.type" class="w100" :placeholder="$t('lz.entity.base.type')">
								<el-option v-for="item in state.filterTypeList" :label="item.name" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.tmsVehicleFilter.startDate')" prop="startDate" :rules="[{ required: true, message: $t('lz.validation.base.required', {PropertyName: $t('lz.entity.tmsVehicleFilter.startDate')}), trigger: 'blur' }]">
								<el-date-picker
									v-model="state.editForm.startDate"
									type="date"
									:format="DateTimeFormatEnum.DateDefault"
									value-format="YYYY-MM-DDT00:00:00"
									:placeholder="$t('lz.label.base.pleaseType', {input: $t('lz.entity.tmsVehicleFilter.startDate')})"
									clearable
									class="w100"
								/>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.tmsVehicleFilter.endDate')" prop="endDate" :rules="[{ required: true, message: $t('lz.validation.base.required', {PropertyName: $t('lz.entity.tmsVehicleFilter.endDate')}), trigger: 'blur' }]">
								<el-date-picker
									v-model="state.editForm.endDate"
									type="date"
									:format="DateTimeFormatEnum.DateDefault"
									value-format="YYYY-MM-DDT00:00:00"
									:placeholder="$t('lz.label.base.pleaseType', {input: $t('lz.entity.tmsVehicleFilter.endDate')})"
									clearable
									class="w100"
								/>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.tmsVehicleFilter.alarm')" prop="alarm" :rules="[{ required: true, message: $t('lz.validation.base.required', {PropertyName: $t('lz.entity.tmsVehicleFilter.alarm')}), trigger: 'blur' }]">
							<el-select v-model="state.editForm.alarm" class="w100">
								<el-option v-for="item in state.filterWarningList" :label="item.name" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.base.status')" prop="status" :rules="[{ required: true, message: $t('lz.validation.base.required', {PropertyName: $t('lz.entity.base.status')}), trigger: 'blur' }]">
							<el-radio-group v-model="state.editForm.status" v-for="item in getConfigDataByCode(BaseConfigTypeEnum.SysStatusType)" :key="item.code">
								<el-radio :value="parseInt(item.code)" class="mr5">{{ item.name }}</el-radio>
							</el-radio-group>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item :label="$t('lz.entity.base.remark')" prop="remark" :rules="[{ max: EntityFieldLength.Length256, message: $t('lz.validation.base.maxlength', {PropertyName: $t('lz.entity.base.remark'), MaxLength: EntityFieldLength.Length256}), trigger: 'blur'}]">
							<el-input v-model="state.editForm.remark" :placeholder="$t('lz.label.base.pleaseType', {input: $t('lz.entity.base.remark')})" type="textarea" rows="4" :maxlength="EntityFieldLength.Length256" show-word-limit clearable />
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>
			<template #footer>
				<span class="dialog-footer">
					<el-button @click="cancel">{{ $t('lz.button.base.cancel') }}</el-button>
					<el-button type="primary" @click="submit" :disabled="state.isProcessing">{{ $t('lz.button.base.confirm') }}</el-button>
				</span>
			</template>
		</el-dialog>
	</div>
</template>

<script lang="ts" setup>

	import { reactive, ref, onMounted } from 'vue';
	import { getAPI } from '/@/utils/axios-utils';
	import { OperateType } from '/@/types/constants';
	import { ElMessage } from 'element-plus';
	import { useI18n } from 'vue-i18n';

	import { TmsVehicleFilterApi } from '/@/api-services/tms';
	import { UpdateTmsVehicleFilterInput } from '/@/api-services/tms';
	import { DateTimeFormatEnum } from '/@/types/enums/baseEnum';

	import { useBaseConfig } from '/@/stores/baseConfig';
	import { BaseConfigTypeEnum } from '/@/types/enums';
	
	const getConfigDataByCode = useBaseConfig().getConfigDataByCode;

	const { t: $t } = useI18n();
	interface Props {
		operateType: OperateType;
		title: string;
	}
	
	import { EntityFieldLength } from '/@/types/enums/baseEnum';

	const props = defineProps<Props>();
	const emits = defineEmits(['handleQuery']);

	const editFormRef = ref();
	const state = reactive({
		isShowDialog: false,
		isProcessing: false,
		editForm: {} as UpdateTmsVehicleFilterInput,
		filterTypeList: [] as any,
		filterWarningList: [] as any,
	});

	onMounted(async () => {
		state.filterTypeList = getConfigDataByCode(BaseConfigTypeEnum.TmsVehicleFilterType);
		state.filterWarningList = getConfigDataByCode(BaseConfigTypeEnum.TmsVehicleFilterWarning);
	});

	const openDialog = (row: any) => {
		editFormRef.value?.resetFields();
		state.editForm = JSON.parse(JSON.stringify(row));
		state.isShowDialog = true;
	};

	const closeDialog = () => {
		emits('handleQuery');
		state.isShowDialog = false;
	};

	const cancel = () => {
		state.isShowDialog = false;
	};

	const submit = () => {
		editFormRef.value.validate(async (valid: boolean) => {
			
			if (!valid) return;

			try{
				state.isProcessing = true;

				//Edit
				if (props.operateType == 'edit') {
					await getAPI(TmsVehicleFilterApi).apiTmsTmsvehiclefilterUpdatePost(state.editForm);
				}
				//Copy or Add
				else if (props.operateType == 'copy' || props.operateType == 'add') {
					state.editForm.id = '';

					await getAPI(TmsVehicleFilterApi).apiTmsTmsvehiclefilterAddPost(state.editForm);
				}

				ElMessage.success($t('lz.message.base.processSuccess'));
				
				closeDialog();
			}
			catch(error){}
			finally{
				state.isProcessing = false;
			}
		});
	};

	defineExpose({ openDialog });

</script>
