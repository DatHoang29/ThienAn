<template>
	<div class="videowall-schedule-container">
		<el-dialog v-model="state.isShowDialog" draggable :close-on-click-modal="false" width="700px">
			<template #header>
				<div style="color: #fff">
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Edit /> </el-icon>
					<span> {{ props.title }} </span>
				</div>
			</template>
			<el-form :model="state.ruleForm" :rules="scheduleRules" ref="ruleFormRef" label-width="auto" label-position="left" autocomplete="off">
				<el-row :gutter="10">
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.base.code')" prop="code"
							:rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.base.code') }), trigger: 'blur' }, { max: 64, message: $t('lz.validation.base.maxlength', { PropertyName: $t('lz.entity.base.code'), MaxLength: 64 }), trigger: 'blur' }]">
							<el-input v-model="state.ruleForm.code" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.code') })" :disabled="props.operateType === 'edit'" clearable />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.base.name')" prop="name"
							:rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.base.name') }), trigger: 'blur' }, { max: 128, message: $t('lz.validation.base.maxlength', { PropertyName: $t('lz.entity.base.name'), MaxLength: 128 }), trigger: 'blur' }]">
							<el-input v-model="state.ruleForm.name" :placeholder="$t('lz.label.videoWall.scheduleNameExample')" clearable />
						</el-form-item>
					</el-col>
					<!-- Loại lịch, ngày trong tuần (hàng tuần), giờ chạy (hàng ngày / hàng tuần), ngày giờ (một lần) -->
					<Scheduler
						v-model:kind="scheduleKind"
						v-model:start-time="state.ruleForm.time"
						v-model:days-of-week="daysOfWeek"
						v-model:once-date="state.ruleForm.onceDate"
						:allowed-kinds="SCHEDULE_KINDS"
						:days-of-week-kinds="['weekly']"
						:labels="schedulerLabels"
						kind-prop="scheduleType"
						days-of-week-prop="weekdays"
						time-prop="time"
						once-date-prop="onceDate"
					/>
					<!-- Hành động + Kịch bản hiển thị chung 1 hàng riêng: bọc hàng con để không bị kéo lên cạnh "Giờ chạy" (nửa hàng) của Scheduler -->
					<el-col :span="24">
						<el-row :gutter="10">
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="$t('lz.label.base.action')" prop="action"
									:rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.label.base.action') }), trigger: 'change' }]">
									<el-radio-group v-model="state.ruleForm.action" @change="onActionChange">
										<el-radio v-for="item in SCHEDULE_ACTIONS" :key="item.value" :value="item.value">{{ item.label }}</el-radio>
									</el-radio-group>
								</el-form-item>
							</el-col>
							<el-col v-if="state.ruleForm.action === 'activate_scene'" :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="$t('lz.entity.videoWall.targetScene')" prop="targetSceneId"
									:rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.videoWall.targetScene') }), trigger: 'change' }]">
									<el-select v-model="state.ruleForm.targetSceneId" :placeholder="$t('lz.label.videoWall.selectScene')" filterable clearable class="w100">
										<!-- Chỉ chọn được kịch bản Bật của bố cục đang dùng; kịch bản đang gắn mà đã tắt / bố cục cũ vẫn hiện (xám) để thấy đúng tên -->
										<el-option v-for="s in sceneOptions" :key="s.id!" :label="`${s.code} — ${s.name}`" :value="s.id!" :disabled="isOldLayoutScene(s) || s.status !== StatusEnum.NUMBER_1">
											<span>{{ `${s.code} — ${s.name}` }}</span>
											<el-tag v-if="isOldLayoutScene(s)" size="small" type="info" class="ml5">{{ $t('lz.label.videoWall.oldLayout') }}</el-tag>
											<el-tag v-else-if="s.status !== StatusEnum.NUMBER_1" size="small" type="info" class="ml5">{{ $t('lz.label.videoWall.disabled') }}</el-tag>
										</el-option>
										<!-- Kịch bản đang gắn đã bị xoá ⇒ hiện nhãn thay cho ID thô -->
										<el-option v-if="state.ruleForm.targetSceneId && !targetScene" :value="state.ruleForm.targetSceneId" :label="$t('lz.label.videoWall.sceneDeleted')" disabled />
									</el-select>
								</el-form-item>
							</el-col>
						</el-row>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb30">
						<el-form-item :label="$t('lz.entity.base.status')" prop="status">
							<!-- Kịch bản đích thuộc bố cục cũ / đã xoá ⇒ luôn Không dùng, khoá "Dùng" -->
							<el-radio-group v-model="state.ruleForm.status" v-for="item in getConfigDataByCode(BaseConfigTypeEnum.SysStatusType)" :key="item.code">
								<el-radio :value="parseInt(item.code)" :disabled="targetLocked && parseInt(item.code) === StatusEnum.NUMBER_1" class="mr5">{{ item.name }}</el-radio>
							</el-radio-group>
							<div v-if="targetLocked" class="vw-locked-hint">{{ $t('lz.message.videoWall.targetSceneLocked') }}</div>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item :label="$t('lz.entity.base.remark')" prop="remark" :rules="[{ max: 256, message: $t('lz.validation.base.maxlength', { PropertyName: $t('lz.entity.base.remark'), MaxLength: 256 }), trigger: 'blur' }]">
							<el-input v-model="state.ruleForm.remark" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.remark') })" clearable type="textarea" />
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

<script lang="ts" setup name="videoWallEditSchedule">
import { computed, reactive, ref, watchEffect } from 'vue';
import { ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { getAPI } from '/@/utils/axios-utils';
import { VwScheduleApi } from '/@/api-services/videoWall';
import { StatusEnum, VwUpdateScheduleInput, VwSceneOutput } from '/@/api-services/videoWall';
import { OperateType } from '/@/types/constants';
import { BaseConfigTypeEnum } from '/@/types/enums/baseEnum';
import { useBaseConfig } from '/@/stores/baseConfig';
import { useVideoWallTopology } from '/@/stores/its/videoWallTopology';
import Scheduler, { type DayOfWeek, type ScheduleKind, type SchedulerLabels } from '/@/components/scheduler/index.vue';
import { SCHEDULE_TYPES, SCHEDULE_TYPE, SCHEDULE_ACTIONS, parseWeekdays, weekdaysToDaysOfWeek, daysOfWeekToWeekdays } from './scheduleOptions';

const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
const { t: $t } = useI18n();

interface Props {
	/** the type of operation */
	operateType: OperateType;
	title: string;
	/** Toàn bộ kịch bản chưa xoá, mọi trạng thái (trang cha nạp lại mỗi lần mở dialog). */
	scenes: VwSceneOutput[];
}
const props = defineProps<Props>();

/** Bố cục đang dùng (trang cha nạp lại trước khi mở dialog) — kịch bản thuộc bố cục khác không gắn được vào lịch / quy tắc. */
const topology = useVideoWallTopology();
const isOldLayoutScene = (s: VwSceneOutput) => !!topology.topology?.id && s.wallTopologyId !== topology.topology.id;
const emits = defineEmits(['handleQuery']);

const ruleFormRef = ref();
const state = reactive({
	isShowDialog: false,
	ruleForm: {} as VwUpdateScheduleInput,
});

const targetScene = computed(() => props.scenes.find((s) => s.id === state.ruleForm.targetSceneId));
/** Kịch bản Bật + kịch bản đang gắn (dù đã tắt / bố cục cũ). */
const sceneOptions = computed(() => props.scenes.filter((s) => s.status === StatusEnum.NUMBER_1 || s.id === state.ruleForm.targetSceneId));
/** Lịch "Chuyển kịch bản" có kịch bản đích thuộc bố cục cũ hoặc đã bị xoá ⇒ không được bật. */
const targetLocked = computed(
	() => state.ruleForm.action === 'activate_scene' && !!state.ruleForm.targetSceneId && (!targetScene.value || isOldLayoutScene(targetScene.value))
);
watchEffect(() => {
	if (targetLocked.value) state.ruleForm.status = StatusEnum.NUMBER_0;
});

/** Kiểu lịch cho chọn trên Scheduler, theo thứ tự SCHEDULE_TYPES. */
const SCHEDULE_KINDS = SCHEDULE_TYPES.map((t) => t.kind);

/** Kiểu lịch của Scheduler ↔ `ruleForm.scheduleType` (số theo BE). */
const scheduleKind = computed<ScheduleKind | undefined>({
	get: () => SCHEDULE_TYPES.find((t) => t.value === state.ruleForm.scheduleType)?.kind,
	set: (kind) => (state.ruleForm.scheduleType = SCHEDULE_TYPES.find((t) => t.kind === kind)?.value),
});

/** Ngày trong tuần của Scheduler ↔ `ruleForm.weekdays` (CSV). */
const daysOfWeek = computed<DayOfWeek[]>({
	get: () => weekdaysToDaysOfWeek(state.ruleForm.weekdays),
	set: (days) => (state.ruleForm.weekdays = daysOfWeekToWeekdays(days)),
});

const schedulerLabels = computed<Partial<SchedulerLabels>>(() => ({
	kind: $t('lz.entity.videoWall.scheduleType'),
	...Object.fromEntries(SCHEDULE_TYPES.map((t) => [t.kind, t.label])),
	daysOfWeek: $t('lz.entity.videoWall.weekdays'),
	time: $t('lz.entity.videoWall.runAt'),
	timePlaceholder: $t('lz.label.videoWall.selectTime'),
	onceDate: $t('lz.entity.videoWall.onceDate'),
	onceDatePlaceholder: $t('lz.label.videoWall.selectDateTime'),
}));

const validateWeekdays = (_rule: any, _value: any, callback: (e?: Error) => void) => {
	if (parseWeekdays(state.ruleForm.weekdays).length === 0) callback(new Error($t('lz.message.videoWall.selectAtLeastOneDay')));
	else callback();
};

// Rule của các trường lịch nằm trong Scheduler (gắn theo kind-prop / days-of-week-prop / time-prop / once-date-prop).
const scheduleRules = computed(() => ({
	scheduleType: [{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.videoWall.scheduleType') }), trigger: 'change' }],
	weekdays: [{ validator: validateWeekdays, trigger: 'change' }],
	time: [{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.videoWall.runAt') }), trigger: 'change' }],
	onceDate: [{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.videoWall.onceDate') }), trigger: 'change' }],
}));

// Bỏ chọn "Chuyển kịch bản" thì không cần kịch bản hiển thị.
const onActionChange = (value: any) => {
	if (value !== 'activate_scene') state.ruleForm.targetSceneId = null;
};

// Open dialog
const openDialog = (row: any) => {
	ruleFormRef.value?.resetFields();
	state.ruleForm = JSON.parse(JSON.stringify(row));
	state.isShowDialog = true;
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

// Chỉ giữ trường ứng với loại lịch đang chọn.
const normalize = (f: VwUpdateScheduleInput) => {
	const type = f.scheduleType;
	if (type !== SCHEDULE_TYPE.Weekly) f.weekdays = null;
	if (type !== SCHEDULE_TYPE.Daily && type !== SCHEDULE_TYPE.Weekly) f.time = null;
	if (type !== SCHEDULE_TYPE.Once) f.onceDate = null;
	if (f.action !== 'activate_scene') f.targetSceneId = null;
	// nextRun / lastRun do BE tính theo job Hangfire, FE không gửi
	f.nextRun = null;
};

// Submit
const submit = () => {
	ruleFormRef.value.validate(async (valid: boolean) => {
		if (!valid) return;
		const f = state.ruleForm;
		normalize(f);
		//Edit
		if (props.operateType == 'edit') {
			await getAPI(VwScheduleApi).apiVideowallVwscheduleUpdatePost(f);
		}
		//Copy or Add
		else if (props.operateType == 'copy' || props.operateType == 'add') {
			f.id = '';
			f.lastRun = null;
			await getAPI(VwScheduleApi).apiVideowallVwscheduleAddPost(f);
		}
		ElMessage.success($t('lz.message.base.saveSuccess'));
		closeDialog();
	});
};

// Export object
defineExpose({ openDialog });
</script>

<style scoped>
.vw-locked-hint {
	width: 100%;
	font-size: 12px;
	line-height: 1.4;
	color: var(--el-color-warning);
}
</style>
