<template>
	<el-dialog v-model="state.isShowDialog" draggable :close-on-click-modal="false" width="700px">
		<template #header>
			<div style="color: #fff">
				<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"><ele-Edit /></el-icon>
				<span> {{ props.title }} </span>
			</div>
		</template>
		<!-- Thông tin bản tin chỉ để đối chiếu: vị trí, thời điểm, camera do BE đọc lại từ bản tin gốc. -->
		<el-descriptions v-if="state.row" :column="2" border size="small" class="mb20">
			<el-descriptions-item :label="$t('lz.entity.tms.detectTime')">{{ detectTimeText }}</el-descriptions-item>
			<el-descriptions-item :label="$t('lz.label.tms.detectedEvent')">{{ props.monitor.eventTypeName(state.row) }}</el-descriptions-item>
			<el-descriptions-item :label="$t('lz.label.tms.equipmentCamera')">{{ props.monitor.equipmentName(state.row) }}</el-descriptions-item>
			<el-descriptions-item :label="$t('lz.entity.tms.direction')">{{ props.monitor.configName(BaseConfigTypeEnum.TmsDirectionType, state.row.direction) }}</el-descriptions-item>
			<el-descriptions-item :label="$t('lz.entity.tms.roadPosition')" :span="2">{{ props.monitor.routePosition(state.row) }}</el-descriptions-item>
			<el-descriptions-item :label="$t('lz.entity.tms.licensePlate')">{{ state.row.licensePlate || '—' }}</el-descriptions-item>
			<el-descriptions-item :label="$t('lz.entity.tms.speed')">{{ state.row.speed == null ? '—' : `${state.row.speed} km/h` }}</el-descriptions-item>
		</el-descriptions>
		<el-form :model="state.editForm" ref="editFormRef" label-width="auto" label-position="left">
			<el-row :gutter="10">
				<!-- Loại sự cố cố định theo loại đã ánh xạ của sự kiện (BE đọc lại từ bản tin), không chọn loại khác. -->
				<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
					<el-form-item :label="$t('lz.entity.tmsIncident.eventTypeId')">
						<el-tag type="danger" effect="plain">{{ state.row ? props.monitor.eventTypeName(state.row) : '—' }}</el-tag>
					</el-form-item>
				</el-col>
				<!-- Xử lý: Đăng ký rồi theo luồng cũ (có kịch bản tự động thì đếm ngược, không thì thủ công).
				     Ghi nhận: tạo thẳng ở trạng thái kết thúc, không chạy kịch bản/điều khiển thiết bị. -->
				<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
					<el-form-item :label="$t('lz.label.tmsIncident.createMode')" prop="mode">
						<el-radio-group v-model="state.editForm.mode">
							<el-radio-button :value="CreateMode.Process">{{ $t('lz.label.tmsIncident.createModeProcess') }}</el-radio-button>
							<el-radio-button :value="CreateMode.Record">{{ $t('lz.label.tmsIncident.createModeRecord') }}</el-radio-button>
						</el-radio-group>
						<div class="create-incident-note">
							{{ state.editForm.mode === CreateMode.Process ? $t('lz.label.tmsIncident.createModeProcessHint') : $t('lz.label.tmsIncident.createModeRecordHint') }}
						</div>
						<div v-if="state.editForm.mode === CreateMode.Process && isOldEvent" class="create-incident-hint">
							{{ $t('lz.label.tmsIncident.createModeProcessOldHint', { age: ageText }) }}
						</div>
					</el-form-item>
				</el-col>
				<el-col v-if="state.editForm.mode === CreateMode.Record" :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
					<el-form-item :label="$t('lz.label.tmsIncident.recordState')" prop="recordState">
						<el-radio-group v-model="state.editForm.recordState">
							<el-radio v-for="item in recordStates" :key="item" :value="item">{{ $t(stateLabelKeys[item]) }}</el-radio>
						</el-radio-group>
					</el-form-item>
				</el-col>
				<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
					<el-form-item
						:label="$t('lz.entity.base.desc')"
						prop="description"
						:rules="[{ max: EntityFieldLength.Length512, message: $t('lz.validation.base.maxlength', { PropertyName: $t('lz.entity.base.desc'), MaxLength: EntityFieldLength.Length512 }), trigger: 'blur' }]"
					>
						<el-input
							v-model="state.editForm.description"
							type="textarea"
							:rows="2"
							:placeholder="$t('lz.label.tmsIncident.fromEventDescHint')"
							:maxlength="EntityFieldLength.Length512"
							show-word-limit
						/>
					</el-form-item>
				</el-col>
				<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
					<el-form-item
						:label="$t('lz.entity.base.remark')"
						prop="remark"
						:rules="[{ max: EntityFieldLength.Length512, message: $t('lz.validation.base.maxlength', { PropertyName: $t('lz.entity.base.remark'), MaxLength: EntityFieldLength.Length512 }), trigger: 'blur' }]"
					>
						<el-input v-model="state.editForm.remark" type="textarea" :rows="2" :maxlength="EntityFieldLength.Length512" show-word-limit />
					</el-form-item>
				</el-col>
			</el-row>
		</el-form>
		<template #footer>
			<span class="dialog-footer">
				<el-button @click="cancel">{{ $t('lz.button.base.cancel') }}</el-button>
				<el-button type="primary" @click="submit" :disabled="state.isProcessing">{{ $t('lz.button.tmsIncident.createIncident') }}</el-button>
			</span>
		</template>
	</el-dialog>
</template>

<script setup lang="ts" name="tmsEventCreateIncident">
import { computed, nextTick, reactive, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { useDateFormat } from '@vueuse/core';
import { ElMessage, ElMessageBox } from 'element-plus';
import { TmsIncidentApi } from '/@/api-services/tms';
import type { IncidentFromVdsEventOutput } from '/@/api-services/tms';
import { getAPI } from '/@/utils/axios-utils';
import { vdsTimestamp } from '/@/utils/vdsTimestamp';
import { BaseConfigTypeEnum } from '/@/types/enums';
import { DateTimeFormatEnum, EntityFieldLength } from '/@/types/enums/baseEnum';
import { TmsIncidentStateEnum } from '/@/types/enums/tmsEnum';
import { createIncidentAutomationMaxAgeMinutes } from '../eventModel';
import type { EventRow } from '../eventModel';
import type { EventMonitor } from '../useEventMonitor';
import { stateLabelKeys } from '../../incident/incidentModel';

// Khớp IncidentConst.FromVdsEvent.Mode phía BE.
const CreateMode = { Process: 'process', Record: 'record' } as const;
type CreateModeValue = (typeof CreateMode)[keyof typeof CreateMode];
// Trạng thái kết thúc được phép khi chỉ ghi nhận (TmsIncidentFromVdsEventRules.RecordStates).
const recordStates = [TmsIncidentStateEnum.Finished, TmsIncidentStateEnum.Skipped, TmsIncidentStateEnum.Canceled];

interface Props {
	title: string;
	monitor: EventMonitor;
}
const props = defineProps<Props>();
const emits = defineEmits<{
	(e: 'created', result: IncidentFromVdsEventOutput): void;
	(e: 'openExisting', result: IncidentFromVdsEventOutput): void;
}>();
const { t: $t } = useI18n();
const editFormRef = ref();
const state = reactive({
	isShowDialog: false,
	isProcessing: false,
	row: null as EventRow | null,
	editForm: { mode: CreateMode.Process as CreateModeValue, recordState: TmsIncidentStateEnum.Finished as string, description: '', remark: '' },
});

const detectDate = computed(() => (state.row?.detectTime ? new Date(vdsTimestamp(state.row.detectTime)) : null));
const detectTimeText = computed(() => (detectDate.value && Number.isFinite(detectDate.value.getTime()) ? useDateFormat(detectDate.value, DateTimeFormatEnum.DateTimeDefault).value : '—'));
const ageMinutes = computed(() => (detectDate.value ? Math.max(0, Math.floor((Date.now() - detectDate.value.getTime()) / 60000)) : 0));
const isOldEvent = computed(() => ageMinutes.value > createIncidentAutomationMaxAgeMinutes);
// "1355 phút" khó đọc: đổi sang giờ/ngày.
const ageText = computed(() => {
	const minutes = ageMinutes.value;
	if (minutes < 60) return $t('lz.label.tmsIncident.ageMinutes', { n: minutes });
	if (minutes < 24 * 60) return $t('lz.label.tmsIncident.ageHours', { n: Math.floor(minutes / 60) });
	return $t('lz.label.tmsIncident.ageDays', { n: Math.floor(minutes / (24 * 60)) });
});

const openDialog = async (row: EventRow) => {
	state.row = { ...row };
	// Sự kiện đã cũ: mặc định Ghi nhận (Hoàn thành) để không chạy kịch bản/đổi biển báo bây giờ cho sự việc đã qua.
	state.editForm = {
		mode: isOldEvent.value ? CreateMode.Record : CreateMode.Process,
		recordState: TmsIncidentStateEnum.Finished,
		description: '',
		remark: '',
	};
	state.isShowDialog = true;
	await nextTick();
	editFormRef.value?.clearValidate();
};

const cancel = () => {
	state.isShowDialog = false;
};

const submit = () =>
	editFormRef.value?.validate(async (valid: boolean) => {
		if (!valid) return;
		await send(false);
	});

async function send(force: boolean) {
	const row = state.row;
	if (!row) return;
	let result: IncidentFromVdsEventOutput | undefined;
	try {
		state.isProcessing = true;
		const response = await getAPI(TmsIncidentApi).apiTmsTmsincidentCreatefromvdseventPost({
			vendorEventId: Number(row.vendorEventId),
			// Giữ nguyên chuỗi UTC có microsecond (giống API ảnh VDS): BE đọc bản tin theo khóa chính xác.
			detectTime: vdsTimestamp(row.detectTime!) as unknown as Date,
			mode: state.editForm.mode,
			recordState: state.editForm.mode === CreateMode.Record ? Number(state.editForm.recordState) : undefined,
			description: state.editForm.description.trim() || undefined,
			remark: state.editForm.remark.trim() || undefined,
			force,
		});
		result = response.data.result ?? undefined;
	} catch {
		return;
	} finally {
		state.isProcessing = false;
	}
	if (!result) return;

	if (result.conflictIncidentId) {
		await confirmConflict(result);
		return;
	}
	if (result.alreadyExists) ElMessage.info($t('lz.message.tmsIncident.fromEventExists', { code: result.code ?? '' }));
	else if (result.automationScheduled) ElMessage.success($t('lz.message.tmsIncident.fromEventScheduled', { code: result.code ?? '' }));
	else ElMessage.success($t('lz.message.tmsIncident.fromEventCreated', { code: result.code ?? '' }));
	state.isShowDialog = false;
	emits('created', result);
}

// Đang có sự cố mở cùng loại, cùng vị trí: người vận hành chọn mở sự cố đó hoặc vẫn tạo mới.
async function confirmConflict(result: IncidentFromVdsEventOutput) {
	try {
		await ElMessageBox.confirm($t('lz.message.tmsIncident.fromEventConflict', { code: result.conflictCode ?? '' }), props.title, {
			type: 'warning',
			confirmButtonText: $t('lz.button.tmsIncident.createAnyway'),
			cancelButtonText: $t('lz.button.tmsIncident.openExisting'),
			distinguishCancelAndClose: true,
		});
		await send(true);
	} catch (action) {
		if (action !== 'cancel') return;
		state.isShowDialog = false;
		emits('openExisting', { incidentId: result.conflictIncidentId, code: result.conflictCode, startDate: result.conflictStartDate });
	}
}

defineExpose({ openDialog });
</script>

<style scoped lang="scss">
.create-incident-note,
.create-incident-hint {
	width: 100%;
	margin-top: 6px;
	line-height: 1.4;
	font-size: var(--el-font-size-small);
	color: var(--el-text-color-secondary);
}

.create-incident-hint {
	color: var(--el-color-warning);
}
</style>
