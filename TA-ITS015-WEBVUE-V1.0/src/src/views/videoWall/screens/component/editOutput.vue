<template>
	<div class="videowall-output-container">
		<el-dialog v-model="state.isShowDialog" draggable :close-on-click-modal="false" width="700px">
			<template #header>
				<div style="color: #fff">
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Edit /> </el-icon>
					<span> {{ props.title }} </span>
				</div>
			</template>
			<!-- 1 form bọc 2 tab ⇒ kiểm tra hợp lệ cả 2 tab trong 1 lần; lỗi ở tab nào thì mở tab đó -->
			<el-form :model="state.ruleForm" ref="ruleFormRef" label-width="auto" label-position="left" autocomplete="off">
				<el-tabs v-model="state.selectedTabName">
					<el-tab-pane :label="$t('lz.label.base.basicInfo')" style="height: 320px; overflow-y: auto; overflow-x: hidden" name="tab1">
						<el-row :gutter="10">
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="$t('lz.entity.base.code')" prop="code"
									:rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.base.code') }), trigger: 'blur' }, { max: 64, message: $t('lz.validation.base.maxlength', { PropertyName: $t('lz.entity.base.code'), MaxLength: 64 }), trigger: 'blur' }]">
									<el-input v-model="state.ruleForm.code" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.code') })" :disabled="props.operateType === 'edit'" clearable />
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="$t('lz.entity.base.name')" prop="name"
									:rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.base.name') }), trigger: 'blur' }, { max: 64, message: $t('lz.validation.base.maxlength', { PropertyName: $t('lz.entity.base.name'), MaxLength: 64 }), trigger: 'blur' }]">
									<el-input v-model="state.ruleForm.name" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.name') })" clearable />
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="$t('lz.entity.base.status')" prop="status">
									<el-radio-group v-model="state.ruleForm.status" v-for="item in getConfigDataByCode(BaseConfigTypeEnum.SysStatusType)" :key="item.code">
										<el-radio :value="parseInt(item.code)" class="mr5">{{ item.name }}</el-radio>
									</el-radio-group>
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="$t('lz.entity.base.orderNo')" prop="orderNo">
									<el-input-number v-model="state.ruleForm.orderNo" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.orderNo') })" class="w100" :min="0" :precision="0" />
								</el-form-item>
							</el-col>
							<!-- Vị trí = ô của cổng đang cắm (tự tính từ thiết bị); ở đây chỉ hiển thị -->
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item>
									<template #label>
										{{ $t('lz.entity.videoWall.position') }}
										<el-tooltip :content="$t('lz.label.videoWall.cellLabelHint')" placement="top">
											<el-icon class="pos-help"><ele-QuestionFilled /></el-icon>
										</el-tooltip>
									</template>
									<el-tag v-if="state.ruleForm.gridRow != null && state.ruleForm.gridCol != null" type="info">
										{{ cellLabel(state.ruleForm.blockNo, state.ruleForm.gridRow, state.ruleForm.gridCol, topology.isMultiBlock) }}
									</el-tag>
									<span v-else class="pos-hint">{{ $t('lz.label.videoWall.notPlaced') }}</span>
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
								<el-form-item :label="$t('lz.entity.base.remark')" prop="remark" :rules="[{ max: 256, message: $t('lz.validation.base.maxlength', { PropertyName: $t('lz.entity.base.remark'), MaxLength: 256 }), trigger: 'blur' }]">
									<el-input v-model="state.ruleForm.remark" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.remark') })" clearable type="textarea" />
								</el-form-item>
							</el-col>
						</el-row>
					</el-tab-pane>
					<!-- Thông tin thiết bị: đấu nối bộ điều khiển / cổng ra + thông số tấm nền -->
					<el-tab-pane :label="$t('lz.label.videoWall.deviceInfo')" style="height: 320px; overflow-y: auto; overflow-x: hidden" name="tab2">
						<el-row :gutter="10">
							<!-- Cổng gán cho màn: gán ở Sơ đồ màn, ở đây chỉ xem -->
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="$t('lz.entity.videoWall.outputPort')">
									<el-tag v-if="state.ruleForm.controllerOutputId" type="info">{{ [vwCtrl.label(state.ruleForm.controllerId), state.ruleForm.outputName].filter(Boolean).join(' · ') }}</el-tag>
									<span v-else class="pos-hint">{{ $t('lz.label.videoWall.screenInStock') }}</span>
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<!-- Serial do người dùng khai báo (serial thiết bị báo ghi riêng, khác thì cảnh báo lệch ở Sơ đồ màn) -->
								<el-form-item :label="$t('lz.entity.videoWall.serialNumber')" prop="serialNumber" :rules="[{ max: 64, message: $t('lz.validation.base.maxlength', { PropertyName: $t('lz.entity.videoWall.serialNumber'), MaxLength: 64 }), trigger: 'blur' }]">
									<el-input v-model="state.ruleForm.serialNumber" clearable />
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="$t('lz.entity.videoWall.panelSize')" prop="panelSize" :rules="rangeRules('lz.entity.videoWall.panelSize', PANEL_LIMITS.panelSize)">
									<el-input-number ref="panelSizeRef" v-model="state.ruleForm.panelSize" class="w100" :min="PANEL_LIMITS.panelSize.min" :max="PANEL_LIMITS.panelSize.max"
										@input="(v: number | null) => clampInputNumber(v, { max: PANEL_LIMITS.panelSize.max }, panelSizeRef, (n) => (state.ruleForm.panelSize = n))"
										@blur="fillDefault('panelSize')" :precision="0" controls-position="right" />
								</el-form-item>
							</el-col>
							<!-- Tỉ lệ panel; khác tỉ lệ ô của bố cục thì chỉ cảnh báo -->
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="$t('lz.entity.videoWall.cellRatio')" prop="cellRatio">
									<el-select v-model="state.ruleForm.cellRatio" class="w100">
										<el-option v-for="r in CELL_RATIOS" :key="r" :value="r" :label="r" />
									</el-select>
									<div v-if="isCellRatioMismatch(state.ruleForm.cellRatio, topology.cellRatio)" class="eo-ratio-warn">
										<el-icon><ele-WarningFilled /></el-icon>
										{{ $t('lz.label.videoWall.cellRatioMismatch', { screen: state.ruleForm.cellRatio, wall: topology.cellRatio }) }}
									</div>
								</el-form-item>
							</el-col>
						</el-row>
					</el-tab-pane>
				</el-tabs>
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

<script lang="ts" setup name="videoWallEditOutput">
import { reactive, ref } from 'vue';
import { ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { getAPI } from '/@/utils/axios-utils';
import { VwScreenApi } from '/@/api-services/videoWall';
import { VwPageScreenOutput } from '/@/api-services/videoWall';
import { OperateType } from '/@/types/constants';
import { BaseConfigTypeEnum } from '/@/types/enums/baseEnum';
import { useBaseConfig } from '/@/stores/baseConfig';
import { CELL_RATIOS, cellLabel, isCellRatioMismatch, useVideoWallTopology } from '/@/stores/its/videoWallTopology';
import { clampInputNumber } from '/@/utils/inputNumber';
import { useVideoWallController } from '/@/stores/its/videoWallController';

const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
const { t: $t } = useI18n();
const topology = useVideoWallTopology();
const vwCtrl = useVideoWallController();

interface Props {
	/** the type of operation */
	operateType: OperateType;
	title: string;
}
const props = defineProps<Props>();
const emits = defineEmits(['handleQuery']);

const panelSizeRef = ref();

/**
 * Giới hạn thực tế + giá trị mặc định của panel.
 * Mặc định = panel chuẩn của hệ thống (55 inch); tối thiểu 10 inch.
 */
const PANEL_LIMITS = {
	panelSize: { min: 10, max: 120, default: 55 },
};
type PanelField = keyof typeof PANEL_LIMITS;

/** Ô bị xoá trống (hoặc dữ liệu cũ = 0) ⇒ điền lại giá trị mặc định, người dùng vẫn sửa được. */
const fillDefault = (field: PanelField) => {
	const v = state.ruleForm[field];
	if (v == null || v <= 0) state.ruleForm[field] = PANEL_LIMITS[field].default;
};

/** Bắt buộc nhập + nằm trong khoảng [min, max]. */
const rangeRules = (labelKey: string, limit: { min: number; max: number }) => [
	{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t(labelKey) }), trigger: ['blur', 'change'] },
	{
		type: 'number',
		min: limit.min,
		max: limit.max,
		message: $t('lz.validation.base.range', { PropertyName: $t(labelKey), Min: limit.min, Max: limit.max }),
		trigger: ['blur', 'change'],
	},
];

const ruleFormRef = ref();
const state = reactive({
	isShowDialog: false,
	selectedTabName: 'tab1',
	/** Dòng từ danh sách (kèm cổng đang cắm, chỉ đọc) — BE chỉ nhận các trường của màn. */
	ruleForm: {} as VwPageScreenOutput,
});

// Open dialog
const openDialog = (row: any) => {
	ruleFormRef.value?.resetFields();
	state.ruleForm = JSON.parse(JSON.stringify(row));
	(Object.keys(PANEL_LIMITS) as PanelField[]).forEach(fillDefault);
	// Màn chưa khai báo tỉ lệ ⇒ lấy tỉ lệ ô của bố cục đang dùng
	state.ruleForm.cellRatio ||= topology.cellRatio;
	state.selectedTabName = 'tab1';
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

/** Trường thuộc tab Thông tin thiết bị. */
const DEVICE_FIELDS = ['serialNumber', 'panelSize', 'cellRatio'];

// Submit
const submit = () => {
	ruleFormRef.value.validate(async (valid: boolean, invalid?: Record<string, unknown>) => {
		if (!valid) {
			// Lỗi chỉ ở trường thiết bị ⇒ mở tab Thông tin thiết bị
			const fields = Object.keys(invalid ?? {});
			state.selectedTabName = fields.length && fields.every((k) => DEVICE_FIELDS.includes(k)) ? 'tab2' : 'tab1';
			return;
		}
		// Cổng đang cắm / tình trạng do đồng bộ thiết bị ghi; thêm / sao chép = màn trong kho
		const f = state.ruleForm;
		//Edit
		if (props.operateType == 'edit') {
			await getAPI(VwScreenApi).apiVideowallVwscreenUpdatePost(f);
		}
		//Copy or Add
		else if (props.operateType == 'copy' || props.operateType == 'add') {
			f.id = '';
			// Trạng thái do Worker ghi, bản sao không kế thừa.
			f.screenState = undefined;
			f.lastCheckedAt = null;
			f.controllerOutputId = null;
			// Serial là duy nhất ⇒ bản sao để trống
			f.serialNumber = props.operateType === 'copy' ? null : f.serialNumber || null;
			await getAPI(VwScreenApi).apiVideowallVwscreenAddPost(f);
		}
		ElMessage.success($t('lz.message.base.saveSuccess'));
		closeDialog();
	});
};

// Export object
defineExpose({ openDialog });
</script>

<style scoped lang="scss">
.eo-ratio-warn {
	display: flex;
	align-items: center;
	gap: 4px;
	width: 100%;
	font-size: 12px;
	line-height: 1.4;
	color: var(--el-color-warning);
}
.pos-help {
	margin-left: 2px;
	vertical-align: -2px;
	color: var(--el-text-color-secondary);
	cursor: help;
}
.pos-hint {
	font-size: 12px;
	color: var(--el-text-color-secondary);
}
</style>
