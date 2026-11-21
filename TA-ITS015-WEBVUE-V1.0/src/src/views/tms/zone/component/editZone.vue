<template>
	<div class="tms-zone-container">
		<el-dialog v-model="state.isShowDialog" draggable :close-on-click-modal="false" width="700px">
			<template #header>
				<div style="color: #fff">
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Edit /> </el-icon>
					<span> {{ props.title }} </span>
				</div>
			</template>
			<el-form :model="state.ruleForm" ref="ruleFormRef" label-width="auto" label-position="left">
				<el-row :gutter="10">
					<!-- Khu vực cha: full width -->
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item :label="$t('lz.entity.tmsZone.pid')">
							<el-cascader
								:options="props.zoneData"
								:props="{ checkStrictly: true, emitPath: false, value: 'id', label: 'name' }"
								:placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.entity.tmsZone.pid')})"
								clearable
								class="w100"
								v-model="state.ruleForm.pid"
							>
								<template #default="{ node, data }">
									<span>{{ data.name }}</span>
									<span v-if="!node.isLeaf"> ({{ data.children.length }}) </span>
								</template>
							</el-cascader>
						</el-form-item>
					</el-col>

					<!-- Mã: bắt buộc -->
					<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.base.code')" prop="code"
							:rules="[
								{ required: true, message: $t('lz.validation.base.required', {PropertyName: $t('lz.entity.base.code')}), trigger: 'blur' },
								{ max: EntityFieldLength.Length64, message: $t('lz.validation.base.maxlength', {PropertyName: $t('lz.entity.base.code'), MaxLength: EntityFieldLength.Length64}), trigger: 'blur' }
							]">
							<el-input v-model="state.ruleForm.code" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.code')})" :maxlength="EntityFieldLength.Length64" show-word-limit clearable />
						</el-form-item>
					</el-col>

					<!-- Tên: bắt buộc -->
					<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.base.name')" prop="name"
							:rules="[
								{ required: true, message: $t('lz.validation.base.required', {PropertyName: $t('lz.entity.base.name')}), trigger: 'blur' },
								{ max: EntityFieldLength.Length128, message: $t('lz.validation.base.maxlength', {PropertyName: $t('lz.entity.base.name'), MaxLength: EntityFieldLength.Length128}), trigger: 'blur' }
							]">
							<el-input v-model="state.ruleForm.name" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.name')})" :maxlength="EntityFieldLength.Length128" show-word-limit clearable />
						</el-form-item>
					</el-col>

					<!-- Loại khu vực (type) — phân biệt với Vị trí bên dưới -->
					<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.tmsZone.type')" prop="type">
							<el-select v-model="state.ruleForm.type" :placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.entity.tmsZone.type')})" clearable class="w100">
								<el-option v-for="item in getConfigDataByCode(BaseConfigTypeEnum.TmsZoneType)" :key="item.code" :label="item.name" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>

					<!-- Vị trí (location) -->
					<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.tms.location')" prop="location">
							<el-select v-model="state.ruleForm.location" :placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.entity.tms.location')})" clearable class="w100">
								<el-option v-for="item in getConfigDataByCode(BaseConfigTypeEnum.TmsZoneLocation)" :key="item.code" :label="item.name" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>

					<!-- Hướng (laneId) — đổi từ "Làn" sang "Hướng" cho đúng thực tế dự án -->
					<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.tms.direction')">
							<el-select v-model="state.ruleForm.laneId" :placeholder="$t('lz.label.base.pleaseSelect',{input: $t('lz.entity.tms.direction')})" clearable class="w100">
								<el-option v-for="item in getConfigDataByCode(BaseConfigTypeEnum.TmsDirectionType)" :key="item.code" :label="item.name" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>

					<!-- Trạng thái -->
					<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.base.status')">
							<el-radio-group v-model="state.ruleForm.status">
								<el-radio v-for="item in getConfigDataByCode(BaseConfigTypeEnum.SysStatusType)" :key="item.code" :value="parseInt(item.code)" class="mr5">{{ item.name }}</el-radio>
							</el-radio-group>
						</el-form-item>
					</el-col>

					<!-- Từ Km và Đến Km trên cùng 1 dòng -->
					<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.tms.fromKmNumber')">
							<el-row :gutter="5">
								<el-col :span="12">
									<el-input-number
										v-model="state.ruleForm.fromKmNumber"
										:min="0"
										:placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.tms.kmNumber')})"
										class="w100"
									/>
								</el-col>
								<el-col :span="12">
									<el-input-number
										v-model="state.ruleForm.fromMetNumber"
										:min="0"
										:max="999"
										:placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.tms.metNumber')})"
										class="w100"
									/>
								</el-col>
							</el-row>
						</el-form-item>
					</el-col>

					<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.tms.toKmNumber')">
							<el-row :gutter="5">
								<el-col :span="12">
									<el-input-number
										v-model="state.ruleForm.toKmNumber"
										:min="0"
										:placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.tms.kmNumber')})"
										class="w100"
									/>
								</el-col>
								<el-col :span="12">
									<el-input-number
										v-model="state.ruleForm.toMetNumber"
										:min="0"
										:max="999"
										:placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.tms.metNumber')})"
										class="w100"
									/>
								</el-col>
							</el-row>
						</el-form-item>
					</el-col>

					<!-- Ngưỡng tốc độ của zone: để trống = dùng mặc định theo hướng (VdsConfig). Cấu hình nhiều zone một lúc ở tab "Tốc độ min/max". -->
					<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.tmsZone.minSpeed')" prop="minSpeed" :rules="speedRules()">
							<el-input-number v-model="state.ruleForm.minSpeed" :min="1" :max="ZONE_SPEED_MAX_KMH" :precision="0" :value-on-clear="null" :placeholder="$t('lz.label.tmsZone.sourceDirection')" class="w100" />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.tmsZone.maxSpeed')" prop="maxSpeed" :rules="speedRules()">
							<el-input-number v-model="state.ruleForm.maxSpeed" :min="1" :max="ZONE_SPEED_MAX_KMH" :precision="0" :value-on-clear="null" :placeholder="$t('lz.label.tmsZone.sourceDirection')" class="w100" />
						</el-form-item>
					</el-col>

					<!-- Ghi chú: full width -->
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item :label="$t('lz.entity.base.remark')">
							<el-input v-model="state.ruleForm.remark" :placeholder="$t('lz.label.base.pleaseType',{input: $t('lz.entity.base.remark')})" clearable type="textarea" rows="4" />
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

<script lang="ts" setup name="tmsEditZone">
import { onMounted, reactive, ref } from 'vue';
import { OperateType } from '/@/types/constants';
import { getAPI } from '/@/utils/axios-utils';
import { TmsZoneApi, UpdateZoneInput } from '/@/api-services/tms';
import { ElMessage } from 'element-plus';
import { useBaseConfig } from '/@/stores/baseConfig';
import { BaseConfigTypeEnum } from '/@/types/enums';
import { EntityFieldLength } from '/@/types/enums/baseEnum';
import { ZONE_SPEED_MAX_KMH } from '/@/services/tms/zone-speed';
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
import { useI18n } from 'vue-i18n';
const { t: $t } = useI18n();

interface Props {
	title: String,
	zoneData: Array<TmsZoneApi>,
	operateType: OperateType
}

const props = defineProps<Props>();
const emits = defineEmits(['handleQuery']);

const ruleFormRef = ref();
const state = reactive({
	isShowDialog: false,
	isProcessing: false,
	ruleForm: {} as UpdateZoneInput,
});

/** Min ≤ Max khi cả hai cùng khai; trống là hợp lệ (dùng mặc định hướng). */
const speedRules = () => [{
	trigger: 'change',
	validator: (_rule: any, _value: any, callback: (error?: Error) => void) => {
		const { minSpeed, maxSpeed } = state.ruleForm;
		if (minSpeed != null && maxSpeed != null && minSpeed > maxSpeed)
			callback(new Error($t('lz.validation.tmsZone.minNotGreaterThanMax')));
		else callback();
	},
}];

onMounted(async () => {});

// Open dialog: reset trước rồi gán data, tránh giữ lỗi của lần mở trước
const openDialog = (row: any) => {
	ruleFormRef.value?.resetFields();
	state.ruleForm = JSON.parse(JSON.stringify(row));
	state.isShowDialog = true;
};

// Close dialog: sau khi lưu thành công mới emit handleQuery
const closeDialog = () => {
	emits('handleQuery');
	state.isShowDialog = false;
};

// Cancel: chỉ đóng dialog, không cần reload lưới
const cancel = () => {
	state.isShowDialog = false;
};

// Submit
const submit = () => {
	ruleFormRef.value.validate(async (valid: boolean) => {
		if (!valid) return;

		try {
			state.isProcessing = true;

			if (props.operateType == 'edit') {
				await getAPI(TmsZoneApi).apiTmsTmszoneUpdatePost(state.ruleForm);
			}
			// Copy hoặc Add: phải xoá id để tránh update đè bản gốc
			else if (props.operateType == 'copy' || props.operateType == 'add') {
				state.ruleForm.id = '';
				await getAPI(TmsZoneApi).apiTmsTmszoneAddPost(state.ruleForm);
			}

			ElMessage.success($t('lz.message.base.saveSuccess'));
			closeDialog();
		}
		catch (error) {}
		finally {
			state.isProcessing = false;
		}
	});
};

defineExpose({ openDialog });
</script>
