<template>
	<div class="edit-rpt-infoconfig-container">
		<el-dialog v-model="state.isShowDialog" draggable :close-on-click-modal="false" width="720px">
			<template #header>
				<div style="color: #fff">
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"><ele-Edit /></el-icon>
					<span> {{ props.title }} </span>
				</div>
			</template>
			<el-form :model="state.editForm" ref="editFormRef" label-width="auto" label-position="left">
				<el-row :gutter="10">
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item
							:label="$t('lz.entity.rptInfoConfig.reportCode')"
							prop="reportCode"
							:rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.rptInfoConfig.reportCode') }), trigger: 'change' }]"
						>
							<el-select v-model="state.editForm.reportCode" filterable class="w100" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.rptInfoConfig.reportCode') })">
								<el-option v-for="item in props.reportList" :key="item.code" :label="item.label" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item
							:label="$t('lz.entity.rptInfoConfig.groupCode')"
							prop="groupCode"
							:rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.rptInfoConfig.groupCode') }), trigger: 'change' }]"
						>
							<el-select v-model="state.editForm.groupCode" filterable class="w100" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.rptInfoConfig.groupCode') })">
								<el-option v-for="item in state.groupCodeList" :key="item.code" :label="item.name" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item
							:label="$t('lz.entity.base.code')"
							prop="code"
							:rules="[
								{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.base.code') }), trigger: 'blur' },
								{
									max: EntityFieldLength.Length128,
									message: $t('lz.validation.base.maxlength', { PropertyName: $t('lz.entity.base.code'), MaxLength: EntityFieldLength.Length128 }),
									trigger: 'blur',
								},
							]"
						>
							<el-input
								v-model="state.editForm.code"
								:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.code') })"
								:maxlength="EntityFieldLength.Length128"
								show-word-limit
								clearable
							/>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item
							:label="$t('lz.entity.base.name')"
							prop="name"
							:rules="[
								{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.base.name') }), trigger: 'blur' },
								{
									max: EntityFieldLength.Length128,
									message: $t('lz.validation.base.maxlength', { PropertyName: $t('lz.entity.base.name'), MaxLength: EntityFieldLength.Length128 }),
									trigger: 'blur',
								},
							]"
						>
							<el-input
								v-model="state.editForm.name"
								:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.name') })"
								:maxlength="EntityFieldLength.Length128"
								show-word-limit
								clearable
							/>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.rptInfoConfig.valueType')" prop="valueType">
							<el-select v-model="state.editForm.valueType" class="w100" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.rptInfoConfig.valueType') })">
								<el-option v-for="item in state.valueTypeList" :key="item.code" :label="item.name" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.base.orderNo')" prop="orderNo">
							<el-input-number v-model="state.editForm.orderNo" :min="0" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.orderNo') })" class="w100" />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item
							:label="$t('lz.entity.rptInfoConfig.value')"
							prop="value"
							:rules="[
								{
									max: EntityFieldLength.Length2048,
									message: $t('lz.validation.base.maxlength', { PropertyName: $t('lz.entity.rptInfoConfig.value'), MaxLength: EntityFieldLength.Length2048 }),
									trigger: 'blur',
								},
							]"
						>
							<el-input
								v-model="state.editForm.value"
								type="textarea"
								:rows="3"
								:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.rptInfoConfig.value') })"
								:maxlength="EntityFieldLength.Length2048"
								show-word-limit
								clearable
							/>
						</el-form-item>
					</el-col>
					<el-col v-if="isImageValue" :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item :label="$t('lz.label.base.preview')">
							<el-image v-if="state.editForm.value" :src="state.editForm.value" fit="contain" style="max-height: 80px" />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.base.status')" prop="status">
							<el-radio-group v-model="state.editForm.status">
								<el-radio v-for="item in getConfigDataByCode(BaseConfigTypeEnum.SysStatusType)" :key="item.code" :value="parseInt(item.code)" class="mr5">
									{{ item.name }}
								</el-radio>
							</el-radio-group>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.rptInfoConfig.isSysConfig')" prop="isSysConfig">
							<el-radio-group v-model="state.editForm.isSysConfig">
								<el-radio v-for="item in state.booleanList" :key="String(item.code)" :value="item.code" class="mr5">{{ item.name }}</el-radio>
							</el-radio-group>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item
							:label="$t('lz.entity.base.remark')"
							prop="remark"
							:rules="[
								{
									max: EntityFieldLength.Length1024,
									message: $t('lz.validation.base.maxlength', { PropertyName: $t('lz.entity.base.remark'), MaxLength: EntityFieldLength.Length1024 }),
									trigger: 'blur',
								},
							]"
						>
							<el-input
								v-model="state.editForm.remark"
								type="textarea"
								:rows="3"
								:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.remark') })"
								:maxlength="EntityFieldLength.Length1024"
								show-word-limit
								clearable
							/>
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

<script lang="ts" setup name="rptEditInfoConfig">
import { computed, onMounted, reactive, ref } from 'vue';
import { ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { getAPI } from '/@/utils/axios-utils';
import { OperateType } from '/@/types/constants';
import { RptInfoConfigApi } from '/@/api-services/report';
import { UpdateRptInfoConfigInput } from '/@/api-services/report';
import { BaseConfigTypeEnum, EntityFieldLength, RptInfoValueTypeEnum } from '/@/types/enums/baseEnum';
import { useBaseConfig } from '/@/stores/baseConfig';

const { t: $t } = useI18n();
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;

interface ReportItem {
	code: string;
	label: string;
}

interface Props {
	operateType: OperateType;
	title: string;
	reportList: ReportItem[];
}

const props = defineProps<Props>();

interface Emits {
	(e: 'handleQuery'): void;
}
const emits = defineEmits<Emits>();

const editFormRef = ref();
const state = reactive({
	isShowDialog: false,
	isProcessing: false,
	editForm: {} as UpdateRptInfoConfigInput,
	groupCodeList: [] as any,
	booleanList: [] as any,
	valueTypeList: [] as any,
});

// Hiển thị preview khi giá trị là hình ảnh
const isImageValue = computed(() => state.editForm.valueType === RptInfoValueTypeEnum.Image);

onMounted(async () => {
	state.groupCodeList = getConfigDataByCode(BaseConfigTypeEnum.RptInfoConfigType);

	state.booleanList = JSON.parse(JSON.stringify(getConfigDataByCode(BaseConfigTypeEnum.SysBooleanType) ?? []));
	state.booleanList.forEach((item: any) => {
		item.code = item.code.toString() == 'true';
	});

	// Kiểu giá trị lấy từ cấu hình hệ thống (DB), Code khớp RptInfoValueTypeEnum
	state.valueTypeList = getConfigDataByCode(BaseConfigTypeEnum.RptInfoValueType) ?? [];
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

		try {
			state.isProcessing = true;

			//Edit
			if (props.operateType == 'edit') {
				await getAPI(RptInfoConfigApi).apiReportRptinfoconfigUpdatePost(state.editForm);
			}
			//Copy or Add
			else if (props.operateType == 'copy' || props.operateType == 'add') {
				state.editForm.id = '';

				await getAPI(RptInfoConfigApi).apiReportRptinfoconfigAddPost(state.editForm);
			}

			ElMessage.success($t('lz.message.base.processSuccess'));

			closeDialog();
		} catch (error) {
			console.error('submit(...)', error);
		} finally {
			state.isProcessing = false;
		}
	});
};

// Export object
defineExpose({ openDialog });
</script>
