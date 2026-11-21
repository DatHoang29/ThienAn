<template>
	<div class="edit-service-monitor-container">
		<el-dialog v-model="state.isShowDialog" draggable :close-on-click-modal="false" width="700px">
			<template #header>
				<div style="color: #fff">
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"><ele-Edit /></el-icon>
					<span> {{ props.title }} </span>
				</div>
			</template>
			<el-form :model="state.editForm" ref="editFormRef" label-width="auto" label-position="left">
				<el-row :gutter="10">
					<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.base.name')" prop="name"
							:rules="[{ required: true, message: $t('lz.validation.base.required', {PropertyName: $t('lz.entity.base.name')}), trigger: 'blur' }, { max: EntityFieldLength.Length128, message: $t('lz.validation.base.maxlength', {PropertyName: $t('lz.entity.base.name'), MaxLength: EntityFieldLength.Length128}), trigger: 'blur'}]">
							<el-input v-model="state.editForm.name" :placeholder="$t('lz.label.base.pleaseType', {input: $t('lz.entity.base.name')})" :maxlength="EntityFieldLength.Length128" show-word-limit clearable />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.tmsServiceMonitor.module')" prop="module"
							:rules="[{ required: true, message: $t('lz.validation.base.required', {PropertyName: $t('lz.entity.tmsServiceMonitor.module')}), trigger: 'change' }]">
							<el-select v-model="state.editForm.module" filterable allow-create default-first-option
								:placeholder="$t('lz.label.base.pleaseSelect', {input: $t('lz.entity.tmsServiceMonitor.module')})" class="w100">
								<el-option v-for="item in moduleOptions" :key="item" :label="item" :value="item" />
							</el-select>
						</el-form-item>
					</el-col>
					<!-- Subject heartbeat: bắt buộc, nên bắt đầu bằng ta.its.heartbeat. để khớp wildcard FE đăng ký -->
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item :label="$t('lz.entity.tmsServiceMonitor.subject')" prop="subject"
							:rules="[{ required: true, message: $t('lz.validation.base.required', {PropertyName: $t('lz.entity.tmsServiceMonitor.subject')}), trigger: 'blur' }, { max: EntityFieldLength.Length128, message: $t('lz.validation.base.maxlength', {PropertyName: $t('lz.entity.tmsServiceMonitor.subject'), MaxLength: EntityFieldLength.Length128}), trigger: 'blur'}]">
							<el-input v-model="state.editForm.subject" placeholder="ta.its.heartbeat.toll" :maxlength="EntityFieldLength.Length128" show-word-limit clearable />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.tmsServiceMonitor.intervalSeconds')" prop="intervalSeconds"
							:rules="[{ required: true, message: $t('lz.validation.base.required', {PropertyName: $t('lz.entity.tmsServiceMonitor.intervalSeconds')}), trigger: 'blur' }]">
							<el-input-number v-model="state.editForm.intervalSeconds" :min="1" :max="3600" :step="5" controls-position="right" class="w100" />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.base.orderNo')" prop="orderNo">
							<el-input-number v-model="state.editForm.orderNo" :min="0" :max="99999" :step="10" controls-position="right" class="w100" />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.base.status')" prop="status">
							<el-radio-group v-model="state.editForm.status">
								<el-radio v-for="item in statusList" :key="item.code" :value="parseInt(item.code)" class="mr5">{{ item.name }}</el-radio>
							</el-radio-group>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item :label="$t('lz.entity.base.remark')" prop="remark"
							:rules="[{ max: EntityFieldLength.Length256, message: $t('lz.validation.base.maxlength', {PropertyName: $t('lz.entity.base.remark'), MaxLength: EntityFieldLength.Length256}), trigger: 'blur'}]">
							<el-input v-model="state.editForm.remark" :placeholder="$t('lz.label.base.pleaseType', {input: $t('lz.entity.base.remark')})" type="textarea" rows="3" :maxlength="EntityFieldLength.Length256" show-word-limit clearable />
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

<script lang="ts" setup name="tmsEditServiceMonitor">
import { reactive, ref, onMounted } from 'vue';
import { ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';

import { OperateType } from '/@/types/constants';
import { EntityFieldLength } from '/@/types/enums/baseEnum';
import { useBaseConfig } from '/@/stores/baseConfig';
import { BaseConfigTypeEnum } from '/@/types/enums';
import { useServiceMonitor, ServiceMonitorItem } from '/@/stores/its/serviceMonitor';

const { t: $t } = useI18n();
const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
const serviceMonitor = useServiceMonitor();

interface Props {
	operateType: OperateType;
	title: string;
}
const props = defineProps<Props>();
const emits = defineEmits(['handleQuery']);

const moduleOptions = ['toll', 'fms', 'vms', 'vds'];

const editFormRef = ref();
const state = reactive({
	isShowDialog: false,
	isProcessing: false,
	editForm: {} as ServiceMonitorItem,
});
const statusList = ref<any[]>([]);

onMounted(() => {
	statusList.value = getConfigDataByCode(BaseConfigTypeEnum.SysStatusType) || [];
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
	editFormRef.value.validate((valid: boolean) => {
		if (!valid) return;
		try {
			state.isProcessing = true;

			if (props.operateType == 'edit') {
				serviceMonitor.update(state.editForm);
			}
			// Copy hoặc Add: xoá id để store tự sinh mới (tránh ghi đè bản gốc khi copy).
			else if (props.operateType == 'copy' || props.operateType == 'add') {
				state.editForm.id = '';
				serviceMonitor.add(state.editForm);
			}

			ElMessage.success($t('lz.message.base.processSuccess'));
			closeDialog();
		} finally {
			state.isProcessing = false;
		}
	});
};

defineExpose({ openDialog });
</script>
