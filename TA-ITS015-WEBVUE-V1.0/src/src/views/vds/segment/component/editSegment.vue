<template>
	<el-dialog v-model="state.isShowDialog" width="700px" draggable :close-on-click-modal="false" :close-on-press-escape="!state.isProcessing" :show-close="!state.isProcessing">
		<template #header><div style="color: #fff"><el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"><ele-View v-if="viewing" /><ele-Edit v-else /></el-icon><span> {{ props.title }} </span></div></template>
		<el-alert v-if="error" :title="error" type="error" :closable="false" class="mb20" />
		<el-descriptions v-if="viewing" :column="2" border>
			<el-descriptions-item :label="$t('lz.label.vdsSegment.name')" :span="2">{{ state.editForm.name || '-' }}</el-descriptions-item>
			<el-descriptions-item :label="$t('lz.label.vdsSegment.fromCamera')" :span="2">{{ cameraName(state.editForm.fromEquipmentId) }}</el-descriptions-item>
			<el-descriptions-item :label="$t('lz.label.vdsSegment.toCamera')" :span="2">{{ cameraName(state.editForm.toEquipmentId) }}</el-descriptions-item>
			<el-descriptions-item :label="$t('lz.label.vdsSegment.direction')">{{ directionName(state.editForm.direction) }}</el-descriptions-item>
			<el-descriptions-item :label="$t('lz.entity.base.status')">{{ state.editForm.status === '1' ? $t('lz.label.vdsSegment.active') : $t('lz.label.vdsSegment.inactive') }}</el-descriptions-item>
			<el-descriptions-item :label="$t('lz.label.vdsSegment.distance')">{{ state.editForm.distanceMeter ?? '-' }} m</el-descriptions-item>
			<el-descriptions-item :label="$t('lz.entity.base.orderNo')">{{ state.editForm.orderNo ?? '-' }}</el-descriptions-item>
			<el-descriptions-item :label="$t('lz.label.vdsSegment.minTime')">{{ state.editForm.minTravelSeconds ?? '-' }} s</el-descriptions-item>
			<el-descriptions-item :label="$t('lz.label.vdsSegment.maxTime')">{{ state.editForm.maxTravelSeconds ?? '-' }} s</el-descriptions-item>
			<el-descriptions-item :label="$t('lz.entity.base.remark')" :span="2">{{ state.editForm.remark || '-' }}</el-descriptions-item>
		</el-descriptions>
		<el-form v-else ref="editFormRef" :model="state.editForm" label-width="auto" label-position="left" :disabled="state.isProcessing">
            <el-row :gutter="10">
			<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20"><el-form-item :label="$t('lz.label.vdsSegment.name')" prop="name" :rules="[{ required: true, whitespace: true, message: $t('lz.label.vdsSegment.requiredName'), trigger: 'blur' }, { max: EntityFieldLength.Length128, message: $t('lz.validation.base.maxlength', { PropertyName: $t('lz.label.vdsSegment.name'), MaxLength: EntityFieldLength.Length128 }), trigger: 'blur' }]"><el-input v-model="state.editForm.name" :maxlength="EntityFieldLength.Length128" show-word-limit /></el-form-item></el-col>
			<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20"><el-form-item :label="$t('lz.label.vdsSegment.fromCamera')" prop="fromEquipmentId" :rules="[{ required: true, message: $t('lz.label.vdsSegment.selectFrom'), trigger: 'change' }]">
				<el-select v-model="state.editForm.fromEquipmentId" filterable class="w100" :placeholder="$t('lz.label.vdsSegment.selectFrom')">
					<el-option v-for="camera in cameras" :key="camera.id" :value="camera.id" :label="cameraLabel(camera)" :disabled="camera.id === state.editForm.toEquipmentId" />
				</el-select>
			</el-form-item></el-col>
			<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20"><el-form-item :label="$t('lz.label.vdsSegment.toCamera')" prop="toEquipmentId" :rules="[{ required: true, message: $t('lz.label.vdsSegment.selectTo'), trigger: 'change' }]">
				<el-select v-model="state.editForm.toEquipmentId" filterable class="w100" :placeholder="$t('lz.label.vdsSegment.selectTo')">
					<el-option v-for="camera in cameras" :key="camera.id" :value="camera.id" :label="cameraLabel(camera)" :disabled="camera.id === state.editForm.fromEquipmentId" />
				</el-select>
			</el-form-item></el-col>
			<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20"><el-form-item :label="$t('lz.label.vdsSegment.direction')" prop="direction">
				<el-select v-model="state.editForm.direction" clearable class="w100" :placeholder="$t('lz.label.vdsSegment.selectDirection')">
					<el-option v-for="direction in directions" :key="String(direction.code)" :value="String(direction.code)" :label="direction.name" />
				</el-select>
			</el-form-item></el-col>
			<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20"><el-form-item :label="$t('lz.label.vdsSegment.distanceMeter')" prop="distanceMeter" :rules="[{ required: true, type: 'number', min: 1, max: 1000000, message: $t('lz.label.vdsSegment.distanceRange'), trigger: 'change' }]">
				<el-input-number v-model="state.editForm.distanceMeter" :min="1" :max="1000000" :precision="0" controls-position="right" class="w100" />
			</el-form-item></el-col>
			<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20"><el-form-item :label="$t('lz.label.vdsSegment.minSeconds')" prop="minTravelSeconds" :rules="[{ type: 'number', min: 1, max: 86400, message: $t('lz.label.vdsSegment.timeRange'), trigger: 'change' }]">
				<el-input-number v-model="state.editForm.minTravelSeconds" :min="1" :max="86400" :precision="0" controls-position="right" class="w100" :placeholder="$t('lz.label.vdsSegment.default')" />
			</el-form-item></el-col>
			<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20"><el-form-item :label="$t('lz.label.vdsSegment.maxSeconds')" prop="maxTravelSeconds" :rules="[{ type: 'number', min: 1, max: 86400, message: $t('lz.label.vdsSegment.timeRange'), trigger: 'change' }]">
				<el-input-number v-model="state.editForm.maxTravelSeconds" :min="1" :max="86400" :precision="0" controls-position="right" class="w100" :placeholder="$t('lz.label.vdsSegment.default')" />
			</el-form-item></el-col>
			<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20"><el-form-item :label="$t('lz.entity.base.status')" prop="status">
				<el-radio-group v-model="state.editForm.status"><el-radio value="1">{{ $t('lz.label.vdsSegment.active') }}</el-radio><el-radio value="0">{{ $t('lz.label.vdsSegment.inactive') }}</el-radio></el-radio-group>
			</el-form-item></el-col>
			<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20"><el-form-item :label="$t('lz.entity.base.orderNo')" prop="orderNo"><el-input-number v-model="state.editForm.orderNo" :min="0" :precision="0" class="w100" /></el-form-item></el-col>
			<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20"><el-form-item :label="$t('lz.entity.base.remark')" prop="remark"><el-input v-model="state.editForm.remark" type="textarea" :rows="2" :maxlength="EntityFieldLength.Length256" show-word-limit /></el-form-item></el-col>

            </el-row>
        </el-form>
		<template #footer>
            <span class="dialog-footer">
			<el-button :disabled="state.isProcessing" @click="state.isShowDialog = false">{{ viewing ? $t('lz.button.base.close') : $t('lz.button.base.cancel') }}</el-button>
			<el-button v-if="!viewing" type="primary" :disabled="state.isProcessing" @click="save">{{ $t('lz.button.base.confirm') }}</el-button>
            </span>
		</template>
	</el-dialog>
</template>

<script setup lang="ts" name="vdsEditSegment">
import { nextTick, reactive, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { EntityFieldLength } from '/@/types/enums/baseEnum';
const { t: $t } = useI18n();
import { ElMessage, type FormInstance } from 'element-plus';
import { VdsSegmentApi } from '/@/api-services/vds';
import type { AddSegmentInput, SegmentOutput, UpdateSegmentInput } from '/@/api-services/vds';
import { getAPI } from '/@/utils/axios-utils';
import { cameraLabel, type VdsCameraOption } from '../../catalog';

const props = defineProps<{ title: string; cameras: VdsCameraOption[]; directions: { code: string; name: string }[] }>();
const cameraName = (id?: string | null) => {
	const camera = props.cameras.find(item => item.id === id);
	return camera ? cameraLabel(camera) : id || '-';
};
const directionName = (code?: string | null) => props.directions.find(item => item.code === code)?.name ?? code ?? '-';
const emit = defineEmits<{ saved: [] }>();
const editing = ref(false);
const viewing = ref(false);
const error = ref('');
const editFormRef = ref<FormInstance>();
const state = reactive({ editForm: {} as UpdateSegmentInput, isShowDialog: false, isProcessing: false });

async function openDialog(row?: SegmentOutput, mode: 'edit' | 'view' = 'edit') {
	Object.assign(state.editForm, {
		id: row?.id ?? undefined,
		name: row?.name ?? '',
		fromEquipmentId: row?.fromEquipmentId ?? undefined,
		toEquipmentId: row?.toEquipmentId ?? undefined,
		direction: row?.direction ?? undefined,
		distanceMeter: row?.distanceMeter ?? undefined,
		minTravelSeconds: row?.minTravelSeconds ?? undefined,
		maxTravelSeconds: row?.maxTravelSeconds ?? undefined,
		status: row?.status ?? '1',
		orderNo: row?.orderNo ?? 0,
		remark: row?.remark ?? '',
	});
	editing.value = !!row?.id;
	viewing.value = mode === 'view';
	error.value = '';
	state.isShowDialog = true;
	await nextTick();
	editFormRef.value?.clearValidate();
}

async function save() {
	if (state.isProcessing || !(await editFormRef.value?.validate().catch(() => false))) return;
	if (state.editForm.fromEquipmentId === state.editForm.toEquipmentId) { error.value = $t('lz.label.vdsSegment.differentCameras'); return; }
	if (state.editForm.minTravelSeconds != null && state.editForm.maxTravelSeconds != null && state.editForm.minTravelSeconds > state.editForm.maxTravelSeconds) { error.value = $t('lz.label.vdsSegment.timeOrder'); return; }
	state.isProcessing = true;
	error.value = '';
	try {
		const payload: AddSegmentInput | UpdateSegmentInput = {
			...(editing.value ? { id: state.editForm.id } : {}),
			name: state.editForm.name?.trim(), fromEquipmentId: state.editForm.fromEquipmentId, toEquipmentId: state.editForm.toEquipmentId,
			direction: state.editForm.direction || null, distanceMeter: state.editForm.distanceMeter,
			minTravelSeconds: state.editForm.minTravelSeconds, maxTravelSeconds: state.editForm.maxTravelSeconds,
			status: state.editForm.status, orderNo: state.editForm.orderNo, remark: state.editForm.remark,
		};
		const api = getAPI(VdsSegmentApi);
		const response = editing.value ? await api.apiVdsVdssegmentUpdatePost(payload) : await api.apiVdsVdssegmentAddPost(payload);
		if (response.data.succeeded === false || response.data.result?.saved !== true) throw new Error(response.data.message || $t('lz.label.vdsSegment.saveFailed'));
		ElMessage.success($t('lz.label.vdsSegment.saved'));
		if (response.data.result.applied !== true) {
			const transports = Object.values(response.data.result.refresh ?? {});
			if (transports.length && transports.every(Boolean)) ElMessage.info($t('lz.label.vdsSegment.refreshSent'));
			else ElMessage.warning($t('lz.label.vdsSegment.refreshFailed'));
		}
		state.isShowDialog = false;
		emit('saved');
	} catch (cause) {
		error.value = cause instanceof Error ? cause.message : $t('lz.label.vdsSegment.saveFailed');
	} finally {
		state.isProcessing = false;
	}
}

defineExpose({ openDialog });
</script>
