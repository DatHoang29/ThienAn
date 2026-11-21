<template>
	<div class="editVehicleRegistration-container">
		<el-dialog v-model="state.isShowDialog" draggable :close-on-click-modal="false" width="700px">
			<template #header>
				<div style="color: #fff">
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"><ele-Edit /></el-icon>
					<span> {{ props.title }} </span>
				</div>
			</template>
			<el-form :model="state.editForm" ref="editFormRef" label-width="auto" label-position="left">
				<el-tabs class="demo-tabs">
					<el-tab-pane>
						<template #label>
							<span class="custom-tabs-label">
								{{ $t('lz.label.base.summary') }}
							</span>
						</template>
						<el-row :gutter="10">
							<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="$t('lz.entity.tmsVehicleRegistration.dataType')" prop="dataType" :rules="[{ required: true, message: $t('lz.validation.base.required', {PropertyName: $t('lz.entity.tmsVehicleRegistration.dataType')}), trigger: 'blur' }]">
									<el-select v-model="state.editForm.dataType" class="w100">
										<el-option v-for="item in state.dataTypeList" :label="item.name" :value="item.code" :disabled="item.code != TmsVehicleRegistrationTypeEnum.Manual.toString()" />
									</el-select>
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="$t('lz.entity.tms.licensePlate')" prop="licensePlate"
								:rules="[{ required: true, message: $t('lz.validation.base.required', {PropertyName: $t('lz.entity.tms.licensePlate')}), trigger: 'blur' }, { max: EntityFieldLength.Length32, message: $t('lz.validation.base.maxlength', {PropertyName: $t('lz.entity.tms.licensePlate'), MaxLength: EntityFieldLength.Length32}), trigger: 'blur'}]">
									<el-input v-model="state.editForm.licensePlate"
										:placeholder="$t('lz.label.base.pleaseType', {input: $t('lz.entity.tms.licensePlate')})"
										:maxlength="EntityFieldLength.Length32" show-word-limit clearable
										@input="onLicensePlateInput" />
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="$t('lz.entity.tms.vehicleType')" prop="vehicleType" :rules="[{ max: EntityFieldLength.Length64, message: $t('lz.validation.base.maxlength', {PropertyName: $t('lz.entity.tms.vehicleType'), MaxLength: EntityFieldLength.Length64}), trigger: 'blur'}]">
									<el-input v-model="state.editForm.vehicleType" :placeholder="$t('lz.label.base.pleaseType', {input: $t('lz.entity.tms.vehicleType')})" :maxlength="EntityFieldLength.Length64" show-word-limit clearable/>
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="$t('lz.entity.tmsVehicleRegistration.brand')" prop="brand" :rules="[{ max: EntityFieldLength.Length64, message: $t('lz.validation.base.maxlength', {PropertyName: $t('lz.entity.tmsVehicleRegistration.brand'), MaxLength: EntityFieldLength.Length64}), trigger: 'blur'}]">
									<el-input v-model="state.editForm.brand" :placeholder="$t('lz.label.base.pleaseType', {input: $t('lz.entity.tmsVehicleRegistration.brand')})"  :maxlength="EntityFieldLength.Length64" show-word-limit clearable />
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
								<el-form-item :label="$t('lz.entity.tmsVehicleRegistration.owner')" prop="owner" ::rules="[{ max: EntityFieldLength.Length64, message: $t('lz.validation.base.maxlength', {PropertyName: $t('lz.entity.tmsVehicleRegistration.owner'), MaxLength: EntityFieldLength.Length64}), trigger: 'blur'}]">
									<el-input v-model="state.editForm.owner" :placeholder="$t('lz.label.base.pleaseType', {input: $t('lz.entity.tmsVehicleRegistration.owner')})" :maxlength="EntityFieldLength.Length64" show-word-limit clearable />
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
								<el-form-item :label="$t('lz.entity.base.address')" prop="address" :rules="[{ max: EntityFieldLength.Length128, message: $t('lz.validation.base.maxlength', {PropertyName: $t('lz.entity.base.address'), MaxLength: EntityFieldLength.Length128}), trigger: 'blur'}]">
									<el-input v-model="state.editForm.address" :placeholder="$t('lz.label.base.pleaseType', {input: $t('lz.entity.base.address')})" :maxlength="EntityFieldLength.Length128" show-word-limit clearable />
								</el-form-item>
							</el-col>
						</el-row>
					</el-tab-pane>
					<el-tab-pane :label="$t('lz.label.base.extend')">
						<el-row :gutter="10">
							<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="$t('lz.entity.tmsVehicleRegistration.engineNumber')" prop="engineNumber" :rules="[{ max: EntityFieldLength.Length64, message: $t('lz.validation.base.maxlength', {PropertyName: $t('lz.entity.tmsVehicleRegistration.engineNumber'), MaxLength: EntityFieldLength.Length64}), trigger: 'blur'}]">
									<el-input v-model="state.editForm.engineNumber" :placeholder="$t('lz.label.base.pleaseType', {input: $t('lz.entity.tmsVehicleRegistration.engineNumber')})" :maxlength="EntityFieldLength.Length64" show-word-limit clearable />
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="$t('lz.entity.tmsVehicleRegistration.chassisNumber')" prop="chassisNumber" :rules="[{ max: EntityFieldLength.Length64, message: $t('lz.validation.base.maxlength', {PropertyName: $t('lz.entity.tmsVehicleRegistration.chassisNumber'), MaxLength: EntityFieldLength.Length64}), trigger: 'blur'}]">
									<el-input v-model="state.editForm.chassisNumber" :placeholder="$t('lz.label.base.pleaseType', {input: $t('lz.entity.tmsVehicleRegistration.chassisNumber')})" :maxlength="EntityFieldLength.Length64" show-word-limit clearable />
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="$t('lz.entity.tmsVehicleRegistration.overallDimensions')" prop="overallDimensions" :rules="[{ max: EntityFieldLength.Length64, message: $t('lz.validation.base.maxlength', {PropertyName: $t('lz.entity.tmsVehicleRegistration.overallDimensions'), MaxLength: EntityFieldLength.Length64}), trigger: 'blur'}]">
									<el-input v-model="state.editForm.overallDimensions" :placeholder="$t('lz.label.base.pleaseType', {input: $t('lz.entity.tmsVehicleRegistration.overallDimensions')})" :maxlength="EntityFieldLength.Length64" show-word-limit clearable />
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="$t('lz.entity.tmsVehicleRegistration.containerDimensions')" prop="containerDimensions" :rules="[{ max: EntityFieldLength.Length64, message: $t('lz.validation.base.maxlength', {PropertyName: $t('lz.entity.tmsVehicleRegistration.containerDimensions'), MaxLength: EntityFieldLength.Length64}), trigger: 'blur'}]">
									<el-input v-model="state.editForm.containerDimensions" :placeholder="$t('lz.label.base.pleaseType', {input: $t('lz.entity.tmsVehicleRegistration.containerDimensions')})" :maxlength="EntityFieldLength.Length64" show-word-limit clearable />
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="$t('lz.entity.tmsVehicleRegistration.seat')" prop="seat" :rules="[{ max: EntityFieldLength.Length32, message: $t('lz.validation.base.maxlength', {PropertyName: $t('lz.entity.base.name'), MaxLength: EntityFieldLength.Length32}), trigger: 'blur'}]">
									<el-input v-model="state.editForm.seat" :placeholder="$t('lz.label.base.pleaseType', {input: $t('lz.entity.tmsVehicleRegistration.seat')})" :maxlength="EntityFieldLength.Length32" show-word-limit clearable />
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="$t('lz.entity.tmsVehicleRegistration.inspectionStamp')" prop="inspectionStamp" :rules="[{ max: EntityFieldLength.Length64, message: $t('lz.validation.base.maxlength', {PropertyName: $t('lz.entity.tmsVehicleRegistration.inspectionStamp'), MaxLength: EntityFieldLength.Length64}), trigger: 'blur'}]">
									<el-input v-model="state.editForm.inspectionStamp" :placeholder="$t('lz.label.base.pleaseType', {input: $t('lz.entity.tmsVehicleRegistration.inspectionStamp')})" :maxlength="EntityFieldLength.Length64" show-word-limit clearable />
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="$t('lz.entity.tmsVehicleRegistration.dateRegistrations')" prop="dateRegistrations"
								:rules="[{ validator: validateDateRegistrations, trigger: 'change' }]">
									<el-date-picker
										v-model="state.editForm.dateRegistrations"
										type="date"
										:format="DateTimeFormatEnum.DateDefault"
										value-format="YYYY-MM-DDT00:00:00"
										:placeholder="$t('lz.label.base.pleaseType', {input: $t('lz.entity.tmsVehicleRegistration.dateRegistrations')})"
										class="w100"
										clearable
										@change="onDateRegistrationsChange"
									/>
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="$t('lz.entity.tmsVehicleRegistration.expirationDate')" prop="expirationDate"
								:rules="[{ validator: validateExpirationDate, trigger: 'change' }]">
									<el-date-picker
										v-model="state.editForm.expirationDate"
										type="date"
										:format="DateTimeFormatEnum.DateDefault"
										value-format="YYYY-MM-DDT00:00:00"
										:placeholder="$t('lz.label.base.pleaseType', {input: $t('lz.entity.tmsVehicleRegistration.expirationDate')})"
										class="w100"
										clearable
										@change="onExpirationDateChange"
									/>
								</el-form-item>
							</el-col>
						</el-row>
					</el-tab-pane>
				</el-tabs>
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

<script lang="ts" setup name="tmseditVehicleRegistration">

	import { reactive, ref, onMounted } from 'vue';
	import { getAPI } from '/@/utils/axios-utils';
	import { OperateType } from '/@/types/constants';
	import { ElMessage } from 'element-plus';
	import { useI18n } from 'vue-i18n';

	import { TmsVehicleRegistrationApi } from '/@/api-services/tms';
	import { UpdateTmsVehicleRegistrationInput } from '/@/api-services/tms';

	const { t: $t } = useI18n();
	interface Props {
		operateType: OperateType;
		title: string;
	}

	import { useBaseConfig } from '/@/stores/baseConfig';
	import { BaseConfigTypeEnum } from '/@/types/enums';
	
	const getConfigDataByCode = useBaseConfig().getConfigDataByCode;

	import { DateTimeFormatEnum, EntityFieldLength } from '/@/types/enums/baseEnum';
	import { TmsVehicleRegistrationTypeEnum } from '/@/types/enums';

	const props = defineProps<Props>();
	const emits = defineEmits(['handleQuery']);

	const editFormRef = ref();
	const state = reactive({
		isShowDialog: false,
		isProcessing: false,
		editForm: {} as UpdateTmsVehicleRegistrationInput,
		dataTypeList: [] as any
	});

	const onLicensePlateInput = (val: string) => {
    // chỉ giữ chữ và số, tự động in hoa
    state.editForm.licensePlate = (val ?? '').replace(/[^A-Za-z0-9]/g, '').toUpperCase();
}	;

	// Validator: Ngay kiem dinh khong duoc lon hon Ngay het han
	const validateDateRegistrations = (_rule: any, _value: any, callback: Function) => {
		if (
			state.editForm.dateRegistrations &&
			state.editForm.expirationDate &&
			new Date(state.editForm.dateRegistrations) > new Date(state.editForm.expirationDate)
		) {
			callback(new Error($t('lz.validation.tmsVehicleRegistration.dateRegistrationsExceedsExpiration')));
		} else {
			callback();
		}
	};

	// Validator: Ngay het han khong duoc nho hon Ngay kiem dinh
	const validateExpirationDate = (_rule: any, _value: any, callback: Function) => {
		if (
			state.editForm.dateRegistrations &&
			state.editForm.expirationDate &&
			new Date(state.editForm.expirationDate) < new Date(state.editForm.dateRegistrations)
		) {
			callback(new Error($t('lz.validation.tmsVehicleRegistration.expirationDateBeforeDateRegistrations')));
		} else {
			callback();
		}
	};

	// Re-validate khi ngay kiem dinh thay doi
	const onDateRegistrationsChange = () => {
		if (state.editForm.expirationDate) {
			editFormRef.value?.validateField('expirationDate');
		}
	};

	// Re-validate khi ngay het han thay doi
	const onExpirationDateChange = () => {
		if (state.editForm.dateRegistrations) {
			editFormRef.value?.validateField('dateRegistrations');
		}
	};
	onMounted(async () => {
		state.dataTypeList = getConfigDataByCode(BaseConfigTypeEnum.TmsVehicleRegistrationType);
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
		if (
			state.editForm.dateRegistrations &&
			state.editForm.expirationDate &&
			new Date(state.editForm.dateRegistrations) > new Date(state.editForm.expirationDate)
		) {
			ElMessage.error($t('lz.validation.tmsVehicleRegistration.dateRegistrationsExceedsExpiration'));
			return;
		}

		editFormRef.value.validate(async (valid: boolean) => {
			
			if (!valid) return;

			try{
				state.isProcessing = true;

				//Edit
				if (props.operateType == 'edit') {
					await getAPI(TmsVehicleRegistrationApi).apiTmsTmsvehicleregistrationUpdatePost(state.editForm);
				}
				//Copy or Add
				else if (props.operateType == 'copy' || props.operateType == 'add') {
					state.editForm.id = '';

					await getAPI(TmsVehicleRegistrationApi).apiTmsTmsvehicleregistrationAddPost(state.editForm);
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
