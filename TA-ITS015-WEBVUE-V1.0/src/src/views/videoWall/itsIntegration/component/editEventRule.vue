<template>
	<div class="videowall-event-rule-container">
		<el-dialog v-model="state.isShowDialog" draggable :close-on-click-modal="false" width="700px">
			<template #header>
				<div style="color: #fff">
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Edit /> </el-icon>
					<span> {{ props.title }} </span>
				</div>
			</template>
			<el-form :model="state.ruleForm" ref="ruleFormRef" label-width="auto" label-position="left" autocomplete="off">
				<el-row :gutter="10">
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.base.code')" prop="code"
							:rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.base.code') }), trigger: 'blur' }, { max: 64, message: $t('lz.validation.base.maxlength', { PropertyName: $t('lz.entity.base.code'), MaxLength: 64 }), trigger: 'blur' }]">
							<el-input v-model="state.ruleForm.code" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.code') })" :disabled="props.operateType === 'edit'" clearable />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.videoWall.eventType')" prop="eventTypeId"
							:rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.videoWall.eventType') }), trigger: 'change' }]">
							<el-select v-model="state.ruleForm.eventTypeId" :placeholder="$t('lz.label.videoWall.selectEventType')" filterable clearable class="w100">
								<el-option v-for="evt in props.eventTypes" :key="evt.id!" :label="evt.name!" :value="evt.id!" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.videoWall.eventSource')" prop="eventSource"
							:rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.videoWall.eventSource') }), trigger: 'change' }]">
							<el-select v-model="state.ruleForm.eventSource" :placeholder="$t('lz.label.videoWall.selectEventSource')" class="w100">
								<el-option v-for="src in getConfigDataByCode(BaseConfigTypeEnum.VideoWallEventSource)" :key="src.code" :label="src.name" :value="src.code" />
							</el-select>
						</el-form-item>
					</el-col>
					
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
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
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<!-- BE lưu Priority dạng chuỗi và chọn theo alphabet tăng dần ⇒ chỉ HIGH/NORMAL. -->
						<el-form-item :label="$t('lz.entity.base.priority')" prop="priority"
							:rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.base.priority') }), trigger: 'change' }]">
							<el-select v-model="state.ruleForm.priority" :placeholder="$t('lz.label.videoWall.selectPriority')" class="w100">
								<el-option v-for="p in getConfigDataByCode(BaseConfigTypeEnum.VideoWallEventPriority)" :key="p.code" :label="p.name" :value="p.code" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
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

<script lang="ts" setup name="videoWallEditEventRule">
import { computed, reactive, ref, watchEffect } from 'vue';
import { ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { getAPI } from '/@/utils/axios-utils';
import { VwEventRuleApi } from '/@/api-services/videoWall';
import { StatusEnum, VwUpdateEventRuleInput, VwSceneOutput } from '/@/api-services/videoWall';
import type { EventTypeOutput } from '/@/api-services/tms';
import { OperateType } from '/@/types/constants';
import { BaseConfigTypeEnum } from '/@/types/enums/baseEnum';
import { useBaseConfig } from '/@/stores/baseConfig';
import { useVideoWallTopology } from '/@/stores/its/videoWallTopology';

const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
const { t: $t } = useI18n();

interface Props {
	/** the type of operation */
	operateType: OperateType;
	title: string;
	/** Toàn bộ kịch bản chưa xoá, mọi trạng thái (trang cha nạp lại mỗi lần mở dialog). */
	scenes: VwSceneOutput[];
	/** Danh mục loại sự kiện TMS (trang cha nạp sẵn). */
	eventTypes: EventTypeOutput[];
}
const props = defineProps<Props>();

/** Bố cục đang dùng (trang cha nạp lại trước khi mở dialog) — kịch bản thuộc bố cục khác không gắn được vào lịch / quy tắc. */
const topology = useVideoWallTopology();
const isOldLayoutScene = (s: VwSceneOutput) => !!topology.topology?.id && s.wallTopologyId !== topology.topology.id;
const emits = defineEmits(['handleQuery']);

const ruleFormRef = ref();
const state = reactive({
	isShowDialog: false,
	ruleForm: {} as VwUpdateEventRuleInput,
});

const targetScene = computed(() => props.scenes.find((s) => s.id === state.ruleForm.targetSceneId));
/** Kịch bản Bật + kịch bản đang gắn (dù đã tắt / bố cục cũ). */
const sceneOptions = computed(() => props.scenes.filter((s) => s.status === StatusEnum.NUMBER_1 || s.id === state.ruleForm.targetSceneId));
/** Kịch bản đích thuộc bố cục cũ hoặc đã bị xoá ⇒ quy tắc không được bật. */
const targetLocked = computed(() => !!state.ruleForm.targetSceneId && (!targetScene.value || isOldLayoutScene(targetScene.value)));
watchEffect(() => {
	if (targetLocked.value) state.ruleForm.status = StatusEnum.NUMBER_0;
});

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

// Submit
const submit = () => {
	ruleFormRef.value.validate(async (valid: boolean) => {
		if (!valid) return;
		//Edit
		if (props.operateType == 'edit') {
			await getAPI(VwEventRuleApi).apiVideowallVweventruleUpdatePost(state.ruleForm);
		}
		//Copy or Add
		else if (props.operateType == 'copy' || props.operateType == 'add') {
			state.ruleForm.id = '';
			await getAPI(VwEventRuleApi).apiVideowallVweventruleAddPost(state.ruleForm);
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
