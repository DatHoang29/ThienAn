<template>
	<el-dialog v-model="state.isShowDialog" draggable :close-on-click-modal="false" width="900px">
		<template #header>
			<div style="color: #fff">
				<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"><ele-Edit /></el-icon>
				<span> {{ props.title }} </span>
			</div>
		</template>
		<el-form :model="state.editForm" ref="editFormRef" label-width="auto" label-position="left">
			<el-alert type="info" :closable="false" show-icon class="mb20" :title="$t('lz.label.tmsIncidentAutomationConfig.legend')" />

			<el-row :gutter="10">
				<!-- Loại sự kiện: khóa khi sửa để tránh đổi khóa duy nhất -->
				<el-col :xs="24" :sm="24" :md="16" :lg="16" :xl="16" class="mb20">
					<el-form-item :label="$t('lz.entity.tms.eventType')" prop="eventTypeId"
						:rules="[{ required: true, message: $t('lz.validation.base.required', {PropertyName: $t('lz.entity.tms.eventType')}), trigger: 'change' }]">
						<el-select
							v-model="state.editForm.eventTypeId"
							filterable
							class="w100"
							:disabled="props.operateType == 'edit'"
							:placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.tms.eventType') })"
							@change="onEventTypeChange"
						>
							<el-option v-for="item in state.eventTypeList" :key="item.id" :label="item.name" :value="item.id" />
						</el-select>
					</el-form-item>
				</el-col>

                <el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
                    <el-form-item :label="$t('lz.label.tmsIncidentAutomationConfig.unclaimedFinish')">
                        <el-switch v-model="state.editForm.isUnclaimedAutoFinishEnabled" @change="onUnclaimedChange" />
                    </el-form-item>
                    <el-form-item
                        v-if="state.editForm.isUnclaimedAutoFinishEnabled"
                        :label="$t('lz.label.tmsIncidentAutomationConfig.unclaimedWait')"
                        prop="unclaimedAutoFinishAfterSeconds"
                        :rules="[
                            { required: true, message: $t('lz.validation.base.required', {PropertyName: $t('lz.label.tmsIncidentAutomationConfig.unclaimedWait')}), trigger: ['blur', 'change'] },
                            { validator: validateUnclaimedAutoFinishSeconds, trigger: ['blur', 'change'] }
                        ]"
                    >
                        <el-input-number v-integer v-model="state.editForm.unclaimedAutoFinishAfterSeconds" :min="1" :max="604800" :step="5" :precision="0" @keydown="handleKeyDownInteger" />
                    </el-form-item>
                </el-col>

				<!-- Kịch bản tự động: chỉ liệt kê plan cùng loại sự kiện + chế độ Automatic/Both -->
				<el-col :xs="24" :sm="24" :md="16" :lg="16" :xl="16" class="mb20">
					<el-form-item :label="$t('lz.entity.tmsTrafficPlan.trafficPlan')" prop="trafficPlanId">
						<el-select
							v-model="state.editForm.trafficPlanId"
							filterable
							clearable
							class="w100"
							:disabled="!state.editForm.eventTypeId || state.editForm.isUnclaimedAutoFinishEnabled"
							:placeholder="$t('lz.label.tmsIncidentAutomationConfig.selectAutoPlan')"
						>
							<el-option v-for="item in availablePlans" :key="item.id" :label="item.name" :value="item.id" />
						</el-select>
						<div v-if="state.editForm.eventTypeId && !availablePlans.length" class="form-hint form-hint--warning">
							{{ $t('lz.label.tmsIncidentAutomationConfig.noAutoPlanHint') }}
						</div>
					</el-form-item>
				</el-col>

				<!-- Nguồn phát hiện được phép tự động (port từ SonTH 1c9d506b, nhánh chưa từng merge vào dev).
					 Để trống = mọi nguồn. Backend lưu chuỗi mã cách nhau dấu phẩy nên quy đổi qua computed. -->
				<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
					<el-form-item :label="$t('lz.entity.tmsIncidentAutomationConfig.allowedSources')" prop="allowedSources">
						<el-select
							v-model="allowedSourceList"
							multiple
							collapse-tags
							collapse-tags-tooltip
							clearable
							class="w100"
							:placeholder="$t('lz.label.tmsIncidentAutomationConfig.allowedSourcesAllHint')"
						>
							<el-option
								v-for="item in getConfigDataByCode(BaseConfigTypeEnum.TmsSourceDetectIncident)"
								:key="item.code"
								:label="item.name"
								:value="item.code"
							/>
						</el-select>
						<div class="form-hint">
							{{ allowedSourceList.length ? $t('lz.label.tmsIncidentAutomationConfig.allowedSourcesSelectedHint') : $t('lz.label.tmsIncidentAutomationConfig.allowedSourcesAllHint') }}
						</div>
					</el-form-item>
				</el-col>

				<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
					<el-form-item :label="$t('lz.entity.tmsIncidentAutomationConfig.triggerState')" prop="triggerState"
						:rules="[{ required: true, message: $t('lz.validation.base.required', {PropertyName: $t('lz.entity.tmsIncidentAutomationConfig.triggerState')}), trigger: 'change' }]">
						<el-select v-model="state.editForm.triggerState" class="w100">
							<el-option
								v-for="item in getConfigDataByCode(BaseConfigTypeEnum.TmsIncidentAutomationTriggerState)"
								:key="item.code"
								:label="item.name"
								:value="item.code"
							/>
						</el-select>
					</el-form-item>
				</el-col>

				<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
					<el-form-item :label="$t('lz.entity.tmsIncidentAutomationConfig.warningDelaySeconds')" prop="warningDelaySeconds"
						:rules="[
							{ required: true, message: $t('lz.validation.base.required', {PropertyName: $t('lz.entity.tmsIncidentAutomationConfig.warningDelaySeconds')}), trigger: 'blur' },
							{ validator: validateInteger, trigger: ['blur', 'change'] }
						]">
						<el-input-number v-integer v-model="state.editForm.warningDelaySeconds" :min="0" :max="86400" :step="10" :precision="0" class="w100" controls-position="right" @keydown="handleKeyDownInteger" />
					</el-form-item>
				</el-col>

				<!-- Trigger từ Registration bắt buộc bật tự xác minh (ràng buộc phía backend) -->
				<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
					<el-form-item :label="$t('lz.entity.tmsIncidentAutomationConfig.autoVerifyIncident')" prop="autoVerifyIncident">
						<el-switch v-model="state.editForm.autoVerifyIncident" :disabled="state.editForm.triggerState == 'register'" />
						<div v-if="state.editForm.triggerState == 'register'" class="form-hint">
							{{ $t('lz.label.tmsIncidentAutomationConfig.autoVerifyRequiredHint') }}
						</div>
					</el-form-item>
				</el-col>

				<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
					<el-form-item :label="$t('lz.entity.tmsIncidentAutomationConfig.isAutoExecuteEnabled')" prop="isAutoExecuteEnabled">
						<el-switch v-model="state.editForm.isAutoExecuteEnabled" :disabled="state.editForm.isUnclaimedAutoFinishEnabled" />
					</el-form-item>
				</el-col>

				<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
					<el-form-item :label="$t('lz.label.tmsIncidentAutomationConfig.finishAfterPlan')" prop="isAutoFinishEnabled">
						<el-switch v-model="state.editForm.isAutoFinishEnabled" :disabled="state.editForm.isUnclaimedAutoFinishEnabled" @change="onAutoFinishChange" />
					</el-form-item>
				</el-col>

				<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
					<el-form-item
						:label="$t('lz.label.tmsIncidentAutomationConfig.planHold')"
						prop="autoFinishAfterSeconds"
						:rules="[
							{ required: state.editForm.isAutoFinishEnabled, message: $t('lz.validation.base.required', {PropertyName: $t('lz.entity.tmsIncidentAutomationConfig.autoFinishAfterSeconds')}), trigger: ['blur', 'change'] },
							{ validator: validateAutoFinishAfterSeconds, trigger: ['blur', 'change'] }
						]"
					>
						<el-input-number
							v-integer
							v-model="state.editForm.autoFinishAfterSeconds"
							:min="1"
							:max="604800"
							:precision="0"
							:step="60"
							class="w100"
							controls-position="right"
							:disabled="!state.editForm.isAutoFinishEnabled"
							@keydown="handleKeyDownInteger"
						/>
					</el-form-item>
				</el-col>

				<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
					<el-form-item :label="$t('lz.entity.tmsIncidentAutomationConfig.failurePolicy')" prop="failurePolicy">
						<el-select v-model="state.editForm.failurePolicy" class="w100">
							<el-option
								v-for="item in getConfigDataByCode(BaseConfigTypeEnum.TmsIncidentAutomationFailurePolicy)"
								:key="item.code"
								:label="item.name"
								:value="item.code"
							/>
						</el-select>
					</el-form-item>
				</el-col>

				<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
					<el-form-item :label="$t('lz.entity.tmsIncidentAutomationConfig.resetPolicy')" prop="resetPolicy">
						<el-select v-model="state.editForm.resetPolicy" class="w100">
							<el-option
								v-for="item in getConfigDataByCode(BaseConfigTypeEnum.TmsIncidentAutomationResetPolicy)"
								:key="item.code"
								:label="item.name"
								:value="item.code"
							/>
						</el-select>
					</el-form-item>
				</el-col>

				<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">
					<el-form-item :label="$t('lz.entity.base.status')" prop="status">
						<el-radio-group v-model="state.editForm.status">
							<el-radio v-for="item in getConfigDataByCode(BaseConfigTypeEnum.SysStatusType)" :key="item.code" :value="parseInt(item.code)" class="mr5">
								{{ item.name }}
							</el-radio>
						</el-radio-group>
					</el-form-item>
				</el-col>

				<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
					<el-form-item :label="$t('lz.entity.base.remark')" prop="remark"
						:rules="[{ max: EntityFieldLength.Length256, message: $t('lz.validation.base.maxlength', {PropertyName: $t('lz.entity.base.remark'), MaxLength: EntityFieldLength.Length256}), trigger: 'blur'}]">
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
</template>

<script lang="ts" setup name="tmsEditIncidentAutomationConfig">

import { computed, reactive, ref, watch } from 'vue';
import { getAPI } from '/@/utils/axios-utils';
import { OperateType } from '/@/types/constants';
import { ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';

import { TmsIncidentAutomationConfigApi, TmsEventTypeApi, TmsTrafficPlanApi } from '/@/api-services/tms';
import { UpdateIncidentAutomationConfigInput } from '/@/api-services/tms';

import { useBaseConfig } from '/@/stores/baseConfig';
import { BaseConfigTypeEnum } from '/@/types/enums';

const getConfigDataByCode = useBaseConfig().getConfigDataByCode;

import { EntityFieldLength } from '/@/types/enums/baseEnum';

const { t: $t } = useI18n();
interface Props {
	operateType: OperateType;
	title: string;
}

const props = defineProps<Props>();
const emits = defineEmits(['handleQuery']);

const editFormRef = ref();

// Directive chặn triệt để dấu thập phân, dấu phẩy, e, E, +, - ngay tại input DOM gốc
const setupIntegerInput = (el: HTMLElement) => {
	if ((el as any)._integerSetupDone) return;
	const input = el.querySelector('input');
	if (!input) return;

	const isInvalidKey = (e: KeyboardEvent) => {
		return (
			e.key === '.' ||
			e.key === ',' ||
			e.key === 'Decimal' ||
			e.code === 'Period' ||
			e.code === 'NumpadDecimal' ||
			e.code === 'Comma' ||
			e.keyCode === 190 ||
			e.keyCode === 110 ||
			e.keyCode === 188 ||
			e.key === 'e' ||
			e.key === 'E' ||
			e.key === '+' ||
			e.key === '-'
		);
	};

	const onKeydown = (e: KeyboardEvent) => {
		if (isInvalidKey(e)) {
			e.preventDefault();
			e.stopPropagation();
		}
	};

	const onBeforeInput = (e: any) => {
		if (e.data && /[.,eE+\-]/.test(e.data)) {
			e.preventDefault();
			e.stopPropagation();
		}
	};

	const onInput = () => {
		if (input.value && /[.,eE+\-]/.test(input.value)) {
			input.value = input.value.replace(/[^\d]/g, '');
			input.dispatchEvent(new Event('input'));
		}
	};

	const onPaste = (e: ClipboardEvent) => {
		const text = (e.clipboardData || (window as any).clipboardData)?.getData('text') || '';
		if (/[^\d]/.test(text.trim())) {
			e.preventDefault();
			const clean = text.replace(/[^\d]/g, '');
			if (clean) {
				document.execCommand?.('insertText', false, clean);
			}
		}
	};

	input.addEventListener('keydown', onKeydown, true);
	input.addEventListener('beforeinput', onBeforeInput, true);
	input.addEventListener('input', onInput, true);
	input.addEventListener('paste', onPaste, true);

	(el as any)._integerSetupDone = true;
	(el as any)._cleanupInteger = () => {
		input.removeEventListener('keydown', onKeydown, true);
		input.removeEventListener('beforeinput', onBeforeInput, true);
		input.removeEventListener('input', onInput, true);
		input.removeEventListener('paste', onPaste, true);
		(el as any)._integerSetupDone = false;
	};
};

const cleanupIntegerInput = (el: HTMLElement) => {
	if ((el as any)._cleanupInteger) {
		(el as any)._cleanupInteger();
	}
};

const vInteger = {
	mounted(el: HTMLElement) {
		setupIntegerInput(el);
	},
	updated(el: HTMLElement) {
		setupIntegerInput(el);
	},
	unmounted(el: HTMLElement) {
		cleanupIntegerInput(el);
	}
};

// Chặn gõ dấu thập phân (chấm, phẩy) và các ký tự không phải số nguyên
const handleKeyDownInteger = (event: KeyboardEvent) => {
	if (
		event.key === '.' ||
		event.key === ',' ||
		event.key === 'Decimal' ||
		event.code === 'Period' ||
		event.code === 'NumpadDecimal' ||
		event.code === 'Comma' ||
		event.keyCode === 190 ||
		event.keyCode === 110 ||
		event.keyCode === 188 ||
		['e', 'E', '-', '+'].includes(event.key)
	) {
		event.preventDefault();
		event.stopPropagation();
	}
};

// Validate chỉ cho phép số nguyên
const validateInteger = (_rule: any, value: any, callback: any) => {
	if (value !== undefined && value !== null && value !== '' && !Number.isInteger(Number(value))) {
		callback(new Error($t('lz.validation.base.integer')));
	} else {
		callback();
	}
};

// Validate thời gian tự kết thúc sau (giây)
const validateAutoFinishAfterSeconds = (_rule: any, value: any, callback: any) => {
	if (state.editForm.isAutoFinishEnabled) {
		if (value === undefined || value === null || value === '') {
			callback(new Error($t('lz.validation.base.required', { PropertyName: $t('lz.entity.tmsIncidentAutomationConfig.autoFinishAfterSeconds') })));
			return;
		}
		if (!Number.isInteger(Number(value))) {
			callback(new Error($t('lz.validation.base.integer')));
			return;
		}
		if (Number(value) < 1) {
			callback(new Error($t('lz.validation.tmsIncidentAutomationConfig.positiveAutoFinish')));
			return;
		}
	}
	callback();
};

// Validate thời gian chờ hoàn thành (giây)
const validateUnclaimedAutoFinishSeconds = (_rule: any, value: any, callback: any) => {
	if (state.editForm.isUnclaimedAutoFinishEnabled) {
		if (value === undefined || value === null || value === '') {
			callback(new Error($t('lz.validation.base.required', { PropertyName: $t('lz.label.tmsIncidentAutomationConfig.unclaimedWait') })));
			return;
		}
		if (!Number.isInteger(Number(value))) {
			callback(new Error($t('lz.validation.base.integer')));
			return;
		}
		if (Number(value) < 1 || Number(value) > 604800) {
			callback(new Error($t('lz.validation.tmsIncidentAutomationConfig.completionWaitRange')));
			return;
		}
	}
	callback();
};

const state = reactive({
	isShowDialog: false,
	isProcessing: false,
	editForm: {} as UpdateIncidentAutomationConfigInput & { isUnclaimedAutoFinishEnabled?: boolean; unclaimedAutoFinishAfterSeconds?: number | null; unclaimedAutoFinishFromUtc?: string | null },
	eventTypeList: [] as any,
	trafficPlanList: [] as any
});


const onUnclaimedChange = (enabled: boolean) => {
    if (!enabled) {
        editFormRef.value?.clearValidate('unclaimedAutoFinishAfterSeconds');
        return;
    }
    state.editForm.isAutoExecuteEnabled = false;
    state.editForm.trafficPlanId = undefined;
    state.editForm.isAutoFinishEnabled = false;
    state.editForm.unclaimedAutoFinishAfterSeconds ??= 30;
};

const onAutoFinishChange = (enabled: boolean) => {
    if (!enabled) {
        editFormRef.value?.clearValidate('autoFinishAfterSeconds');
    } else {
        state.editForm.autoFinishAfterSeconds ??= 60;
    }
};

// Chỉ cho chọn kịch bản cùng loại sự kiện, đang Enable và dùng được cho tự động (Automatic/Both)
const availablePlans = computed(() => {
	if (!state.editForm.eventTypeId) return [];
	return state.trafficPlanList.filter(
		(plan: any) => plan.eventTypeId == state.editForm.eventTypeId && plan.status == 1 && (plan.executionMode == 'Automatic' || plan.executionMode == 'Both')
	);
});

/** Mã nguồn VDS trong danh mục tms_source_detect. */
const VDS_SOURCE = 'vds';

/**
 * Author: SonTH (port KienLT - 17/10/2026)
 * Description: Quy đổi giữa chuỗi allowedSources của backend (các mã cách nhau bởi dấu phẩy) và mảng
 *              mà el-select multiple cần. Mảng rỗng ghi về null - backend hiểu null/rỗng là "mọi nguồn".
 */
const allowedSourceList = computed<string[]>({
	get: () =>
		(state.editForm.allowedSources ?? '')
			.split(',')
			.map((code: string) => code.trim())
			.filter((code: string) => code.length > 0),
	set: (list: string[]) => {
		state.editForm.allowedSources = list && list.length ? list.join(',') : null;
	},
});

// Trigger Registration bắt buộc bật tự xác minh (ràng buộc backend)
watch(
	() => state.editForm.triggerState,
	(value) => {
		if (value == 'register') state.editForm.autoVerifyIncident = true;
	}
);

// Đổi loại sự kiện -> reset kịch bản đã chọn vì danh sách phụ thuộc loại sự kiện
const onEventTypeChange = () => {
	state.editForm.trafficPlanId = undefined;
    state.editForm.isUnclaimedAutoFinishEnabled = false;
};

// Nạp dữ liệu tham chiếu
const loadReferenceData = async () => {
	const [eventTypes, plans] = await Promise.all([
		getAPI(TmsEventTypeApi).apiTmsTmseventtypeListGet(),
		getAPI(TmsTrafficPlanApi).apiTmsTmstrafficplanListGet(),
	]);
	state.eventTypeList = (eventTypes.data.result ?? []).filter((item: any) => item.code !== 'unknown');
	state.trafficPlanList = plans.data.result ?? [];
};

const getDefaultConfigCode = (configTypeCode: BaseConfigTypeEnum, fallback: string) => {
	const options = getConfigDataByCode(configTypeCode);
	return options.find((item: any) => item.code === fallback)?.code ?? options[0]?.code ?? fallback;
};

const openDialog = async (row: any) => {
	editFormRef.value?.resetFields();
	state.editForm = JSON.parse(JSON.stringify(row ?? {}));

	state.editForm.isUnclaimedAutoFinishEnabled ??= false;
    state.editForm.isAutoExecuteEnabled ??= false;
    state.editForm.isAutoFinishEnabled ??= false;
    state.editForm.warningDelaySeconds ??= 30;
    state.editForm.status ??= 1;

	if (state.editForm.warningDelaySeconds !== undefined && state.editForm.warningDelaySeconds !== null) {
		state.editForm.warningDelaySeconds = Math.round(Number(state.editForm.warningDelaySeconds));
	}
	if (state.editForm.autoFinishAfterSeconds !== undefined && state.editForm.autoFinishAfterSeconds !== null) {
		state.editForm.autoFinishAfterSeconds = Math.round(Number(state.editForm.autoFinishAfterSeconds));
	}
	if (state.editForm.unclaimedAutoFinishAfterSeconds !== undefined && state.editForm.unclaimedAutoFinishAfterSeconds !== null) {
		state.editForm.unclaimedAutoFinishAfterSeconds = Math.round(Number(state.editForm.unclaimedAutoFinishAfterSeconds));
	}
    // Giá trị mặc định cho bản ghi mới
	state.editForm.triggerState = state.editForm.triggerState ?? getDefaultConfigCode(BaseConfigTypeEnum.TmsIncidentAutomationTriggerState, 'verify');
	state.editForm.failurePolicy = state.editForm.failurePolicy ?? getDefaultConfigCode(BaseConfigTypeEnum.TmsIncidentAutomationFailurePolicy, 'stop-reset');
	state.editForm.resetPolicy = state.editForm.resetPolicy ?? getDefaultConfigCode(BaseConfigTypeEnum.TmsIncidentAutomationResetPolicy, 'reset-default');

	state.isShowDialog = true;
	await loadReferenceData();
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
        if (state.editForm.isUnclaimedAutoFinishEnabled && (!Number.isInteger(state.editForm.unclaimedAutoFinishAfterSeconds) || Number(state.editForm.unclaimedAutoFinishAfterSeconds) < 1 || Number(state.editForm.unclaimedAutoFinishAfterSeconds) > 604800)) {
            ElMessage.warning($t('lz.validation.tmsIncidentAutomationConfig.unclaimedFinish'));
            return;
        }

		// Tự kết thúc khi không ai nhận chỉ áp cho sự kiện VDS: đã chọn nguồn thì phải có VDS (ràng buộc backend)
		if (state.editForm.isUnclaimedAutoFinishEnabled && allowedSourceList.value.length
			&& !allowedSourceList.value.some((code) => code.toLowerCase() === VDS_SOURCE)) {
			ElMessage.warning($t('lz.validation.tmsIncidentAutomationConfig.unclaimedFinishRequiresVds'));
			return;
		}

		// Bật tự động thực thi thì bắt buộc phải chọn kịch bản
		if (state.editForm.isAutoExecuteEnabled && !state.editForm.trafficPlanId) {
			ElMessage.warning($t('lz.message.tmsIncidentAutomationConfig.trafficPlanRequired'));
			return;
		}

		if (state.editForm.warningDelaySeconds !== undefined && state.editForm.warningDelaySeconds !== null) {
			state.editForm.warningDelaySeconds = Math.round(Number(state.editForm.warningDelaySeconds));
		}
		if (state.editForm.autoFinishAfterSeconds !== undefined && state.editForm.autoFinishAfterSeconds !== null) {
			state.editForm.autoFinishAfterSeconds = Math.round(Number(state.editForm.autoFinishAfterSeconds));
		}
		if (state.editForm.unclaimedAutoFinishAfterSeconds !== undefined && state.editForm.unclaimedAutoFinishAfterSeconds !== null) {
			state.editForm.unclaimedAutoFinishAfterSeconds = Math.round(Number(state.editForm.unclaimedAutoFinishAfterSeconds));
		}

		try{
			state.isProcessing = true;

			//Edit
			if (props.operateType == 'edit') {
				await getAPI(TmsIncidentAutomationConfigApi).apiTmsTmsincidentautomationconfigUpdatePost(state.editForm);
			}
			//Copy or Add
			else if (props.operateType == 'copy' || props.operateType == 'add') {
				state.editForm.id = '';

				await getAPI(TmsIncidentAutomationConfigApi).apiTmsTmsincidentautomationconfigAddPost(state.editForm);
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

<style scoped lang="scss">
.form-hint {
	font-size: 12px;
	line-height: 1.4;
	color: var(--el-text-color-secondary);

	&--warning {
		color: var(--el-color-warning);
	}
}
</style>
