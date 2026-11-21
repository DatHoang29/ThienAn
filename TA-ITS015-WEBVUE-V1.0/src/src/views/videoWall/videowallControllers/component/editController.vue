<template>
	<div class="videowall-controller-container">
		<el-dialog v-model="state.isShowDialog" draggable :close-on-click-modal="false" width="700px">
			<template #header>
				<div style="color: #fff">
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Edit /> </el-icon>
					<span> {{ props.title }} </span>
				</div>
			</template>
			<!-- 1 form bọc 2 tab ⇒ kiểm tra hợp lệ cả 2 tab trong 1 lần; lỗi ở tab nào thì mở tab đó -->
			<el-form :model="state.ruleForm" ref="ruleFormRef" label-width="auto" label-position="left" autocomplete="off">
				<el-tabs v-loading="state.loading" v-model="state.selectedTabName">
					<el-tab-pane :label="$t('lz.label.base.basicInfo')" style="height: 450px; overflow-y: auto; overflow-x: hidden" name="tab1">
						<el-row :gutter="10">
							<!-- Vai trò: bộ phân phối (chỉ làm cha) / bộ hiển thị (bắt buộc có cha là bộ phân phối) / bộ độc lập (không cha, không con) -->
							<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
								<el-form-item :label="$t('lz.entity.videoWall.role')" prop="role"
									:rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.videoWall.role') }), trigger: 'change' }]">
									<el-radio-group v-model="state.ruleForm.role" @change="onRoleChange">
										<el-radio v-for="item in getConfigDataByCode(BaseConfigTypeEnum.VideoWallControllerRole)" :key="item.code" :value="item.code">{{ item.name }}</el-radio>
									</el-radio-group>
								</el-form-item>
							</el-col>
							<el-col v-if="state.ruleForm.role === VW_ROLE.Display" :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
								<el-form-item :label="$t('lz.entity.videoWall.parentController')" prop="parentControllerId"
									:rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.videoWall.parentController') }), trigger: 'change' }]">
									<el-select v-model="state.ruleForm.parentControllerId" :placeholder="$t('lz.label.videoWall.selectDistribution')" filterable class="w100">
										<el-option v-for="item in parentCandidates" :key="item.id!" :label="`${vwCtrlCode(item.code)} — ${item.name}`" :value="item.id!" />
									</el-select>
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="$t('lz.entity.base.code')" prop="code"
									:rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.base.code') }), trigger: 'blur' }]">
									<el-input v-model="state.ruleForm.code" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.code') })" :disabled="props.operateType === 'edit'" clearable />
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="$t('lz.entity.base.name')" prop="name"
									:rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.base.name') }), trigger: 'blur' }, { max: 64, message: $t('lz.validation.base.maxlength', { PropertyName: $t('lz.entity.base.name'), MaxLength: 64 }), trigger: 'blur' }]">
									<el-input v-model="state.ruleForm.name" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.name') })" clearable />
								</el-form-item>
							</el-col>
							<!-- Ẩn vùng phủ (OriginCol/OriginRow/CoverCols/CoverRows): BE không dùng, vùng phủ suy ra từ VwScreen.
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="$t('lz.entity.videoWall.originCol')" prop="originCol">
									<el-input-number ref="originColRef" v-model="state.ruleForm.originCol" class="w100" :min="0" :max="ORIGIN_COL_MAX"
										@input="(v: number | null) => clampInputNumber(v, { min: 0, max: ORIGIN_COL_MAX }, originColRef, (n) => (state.ruleForm.originCol = n))" :precision="0" controls-position="right" />
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="$t('lz.entity.videoWall.originRow')" prop="originRow">
									<el-input-number ref="originRowRef" v-model="state.ruleForm.originRow" class="w100" :min="0" :max="ORIGIN_ROW_MAX"
										@input="(v: number | null) => clampInputNumber(v, { min: 0, max: ORIGIN_ROW_MAX }, originRowRef, (n) => (state.ruleForm.originRow = n))" :precision="0" controls-position="right" />
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="$t('lz.entity.videoWall.coverCols')" prop="coverCols">
									<el-input-number ref="coverColsRef" v-model="state.ruleForm.coverCols" class="w100" :min="1" :max="VideoWallEnums.WALL_COLS"
										@input="(v: number | null) => clampInputNumber(v, { max: VideoWallEnums.WALL_COLS }, coverColsRef, (n) => (state.ruleForm.coverCols = n))" :precision="0" controls-position="right" />
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="$t('lz.entity.videoWall.coverRows')" prop="coverRows">
									<el-input-number ref="coverRowsRef" v-model="state.ruleForm.coverRows" class="w100" :min="1" :max="VideoWallEnums.WALL_ROWS"
										@input="(v: number | null) => clampInputNumber(v, { max: VideoWallEnums.WALL_ROWS }, coverRowsRef, (n) => (state.ruleForm.coverRows = n))" :precision="0" controls-position="right" />
								</el-form-item>
							</el-col>
							-->
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="$t('lz.entity.videoWall.inputSlotsNumber')" prop="inputSlotsNumber">
									<el-input-number ref="inputSlotsRef" v-model="state.ruleForm.inputSlotsNumber" class="w100" :min="1" :max="SLOTS_MAX"
										@input="(v: number | null) => clampInputNumber(v, { max: SLOTS_MAX }, inputSlotsRef, (n) => (state.ruleForm.inputSlotsNumber = n))" :precision="0" controls-position="right" />
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="$t('lz.entity.videoWall.slotsNumber')" prop="slotsNumber">
									<el-input-number ref="slotsRef" v-model="state.ruleForm.slotsNumber" class="w100" :min="1" :max="SLOTS_MAX"
										@input="(v: number | null) => clampInputNumber(v, { max: SLOTS_MAX }, slotsRef, (n) => (state.ruleForm.slotsNumber = n))" :precision="0" controls-position="right" />
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="$t('lz.entity.videoWall.colorHex')" prop="colorHex">
									<!-- Màu nhận diện: viền ô màn hình / cổng ăn theo màu này ⇒ gợi ý màu không trùng màu trạng thái -->
									<!-- Chỉ chọn trong 10 màu cho sẵn (không nhập mã màu tự do, không dùng màu hệ thống) -->
									<div class="color-swatches">
										<button v-for="c in VW_CTRL_COLOR_PRESETS" :key="c" type="button" class="color-swatch"
											:class="{ 'is-active': state.ruleForm.colorHex?.toUpperCase() === c }" :style="{ background: c }" :title="c"
											@click="state.ruleForm.colorHex = c">
											<el-icon v-if="state.ruleForm.colorHex?.toUpperCase() === c"><ele-Check /></el-icon>
										</button>
									</div>
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="$t('lz.entity.base.status')" prop="status">
									<el-radio-group v-model="state.ruleForm.status" v-for="item in getConfigDataByCode(BaseConfigTypeEnum.SysStatusType)" :key="item.code">
										<el-radio :value="parseInt(item.code)" class="mr5">{{ item.name }}</el-radio>
									</el-radio-group>
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
								<el-form-item :label="$t('lz.entity.base.remark')" prop="remark" :rules="[{ max: 256, message: $t('lz.validation.base.maxlength', { PropertyName: $t('lz.entity.base.remark'), MaxLength: 256 }), trigger: 'blur' }]">
									<el-input v-model="state.ruleForm.remark" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.remark') })" clearable type="textarea" />
								</el-form-item>
							</el-col>
						</el-row>
					</el-tab-pane>
					<!-- Thông tin thiết bị: lấy từ thiết bị TMS (thiết bị tham chiếu) — model / kết nối chỉ đọc -->
					<el-tab-pane :label="$t('lz.label.videoWall.deviceInfo')" style="height: 450px; overflow-y: auto; overflow-x: hidden" name="tab2">
						<el-row :gutter="10">
							<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
								<el-form-item :label="$t('lz.entity.videoWall.refEquipment')" prop="refId"
									:rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.videoWall.refEquipment') }), trigger: 'change' }]">
									<!-- Bắt buộc: IP / tài khoản / mật khẩu lấy từ thiết bị (BE chép khi lưu) ⇒ khai báo thiết bị ở trang Thiết bị trước -->
									<el-select v-model="state.ruleForm.refId" :placeholder="$t('lz.label.videoWall.selectRefEquipment')" :loading="state.equipmentLoading" filterable clearable class="w100" @change="onEquipmentChange">
										<el-option v-for="item in state.equipmentList" :key="item.id!" :label="`${item.code} — ${item.name}`" :value="item.id!" :disabled="linkedEquipmentIds.has(item.id!)">
											<span>{{ item.code }} — {{ item.name }}</span>
											<span v-if="linkedEquipmentIds.has(item.id!)" style="float: right; color: var(--el-text-color-secondary); font-size: 12px">{{ $t('lz.label.videoWall.equipmentLinked') }}</span>
										</el-option>
									</el-select>
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<!-- Model: thông tin định danh do hệ thống đọc về từ thiết bị (biên bản 22/10 mục 1.4) ⇒ chỉ đọc, BE không nhận từ form -->
								<el-form-item :label="$t('lz.entity.videoWall.model')">
									<el-input :model-value="modelText" :placeholder="$t('lz.label.videoWall.modelReadFromDevice')" disabled />
								</el-form-item>
							</el-col>
							<!-- Thông tin kết nối: chỉ đọc, theo thiết bị đang chọn (mật khẩu không hiển thị) -->
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="$t('lz.entity.base.ipAddress')">
									<el-input :model-value="connection.ip" disabled />
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="$t('lz.entity.base.port')">
									<el-input :model-value="connection.port" disabled />
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="$t('lz.entity.base.account')">
									<el-input :model-value="connection.account" disabled />
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="$t('lz.entity.base.password')">
									<el-input :model-value="connection.hasPassword ? '••••••••' : ''" disabled />
								</el-form-item>
							</el-col>
							<el-col :span="24" class="mb20">
								<div class="connection-hint">
									<el-icon><ele-InfoFilled /></el-icon>
									<span>{{ $t('lz.message.videoWall.connectionFromEquipment') }}</span>
								</div>
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

<script lang="ts" setup name="videoWallEditController">
import { computed, reactive, ref } from 'vue';
import { ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { getAPI } from '/@/utils/axios-utils';
import { VwControllerApi } from '/@/api-services/videoWall';
import { VwUpdateControllerInput } from '/@/api-services/videoWall';
import { TmsEquipmentApi, TmsEquipmentTypeApi } from '/@/api-services/tms';
import type { EquipmentOutput } from '/@/api-services/tms';
import { TmsEquipmentType } from '/@/types/enums';
import { OperateType } from '/@/types/constants';
import { BaseConfigTypeEnum } from '/@/types/enums/baseEnum';
import { useBaseConfig } from '/@/stores/baseConfig';
import { clampInputNumber } from '/@/utils/inputNumber';
import { useVideoWallController, vwCtrlCode, VW_CTRL_COLOR_PRESETS, VW_DEFAULT_CTRL_COLOR, VW_ROLE } from '/@/stores/its/videoWallController';

const { getConfigDataByCode, getConfigItemByCode } = useBaseConfig();

/** Model hiển thị: tên trong danh mục model nếu có, không thì mã model thô đọc từ thiết bị. */
const modelText = computed(() => {
	const code = state.ruleForm.model;
	if (!code) return '';
	return (getConfigItemByCode(BaseConfigTypeEnum.VideoWallControllerModel, code) as { name?: string } | undefined)?.name || code;
});
const { t: $t } = useI18n();

interface Props {
	/** the type of operation */
	operateType: OperateType;
	title: string;
}
const props = defineProps<Props>();
const emits = defineEmits(['handleQuery']);

const vwCtrl = useVideoWallController();

// /** Cột/hàng bắt đầu tính từ 0 nên phải bé hơn số cột/hàng của tường (cột 0..7, hàng 0..3). */
// const ORIGIN_COL_MAX = VideoWallEnums.WALL_COLS - 1;
// const ORIGIN_ROW_MAX = VideoWallEnums.WALL_ROWS - 1;
/** Số khe vào/ra: lớn hơn 0 và bé hơn hoặc bằng 32. */
const SLOTS_MAX = 32;

// const originColRef = ref();
// const originRowRef = ref();
// const coverColsRef = ref();
// const coverRowsRef = ref();
const inputSlotsRef = ref();
const slotsRef = ref();

const ruleFormRef = ref();
const state = reactive({
	isShowDialog: false,
	loading: false,
	selectedTabName: 'tab1',
	ruleForm: {} as VwUpdateControllerInput,
	equipmentLoading: false,
	equipmentList: [] as EquipmentOutput[],
});

const linkedEquipmentIds = computed(() => new Set(vwCtrl.controllers.filter((c) => c.refId && c.id !== state.ruleForm.id).map((c) => c.refId!)));

const loadEquipments = async () => {
	state.equipmentLoading = true;
	try {
		const resType = await getAPI(TmsEquipmentTypeApi).apiTmsTmsequipmenttypeListGet(undefined, TmsEquipmentType.VideoWallController);
		const typeIds = (resType.data.result ?? []).filter((t) => t.code === TmsEquipmentType.VideoWallController).map((t) => t.id!);
		if (!typeIds.length) {
			state.equipmentList = [];
			return;
		}
		const resList = await Promise.all(typeIds.map((id) => getAPI(TmsEquipmentApi).apiTmsTmsequipmentListGet(undefined, undefined, undefined, undefined, undefined, undefined, undefined, id)));
		state.equipmentList = resList.flatMap((r) => r.data.result ?? []);
	} finally {
		state.equipmentLoading = false;
	}
};

const selectedEquipment = computed(() => state.equipmentList.find((e) => e.id === state.ruleForm.refId));

/** Thông tin kết nối hiển thị (chỉ đọc): theo thiết bị đang chọn — BE chép đúng các giá trị này khi lưu. */
const connection = computed(() => {
	const eq = selectedEquipment.value;
	if (!eq) return { ip: '', port: '', account: '', hasPassword: false };
	return { ip: eq.ip ?? '', port: eq.port ?? '', account: eq.username ?? '', hasPassword: !!eq.password };
});

/** Chọn thiết bị ⇒ điền sẵn Mã / Tên khi còn trống (Mã chỉ sửa được lúc thêm mới). */
const onEquipmentChange = () => {
	const eq = selectedEquipment.value;
	if (!eq) return;
	if (!state.ruleForm.code && props.operateType !== 'edit') state.ruleForm.code = eq.code;
	if (!state.ruleForm.name) state.ruleForm.name = eq.name;
};

/** Bộ cha chỉ được là bộ phân phối. */
const parentCandidates = computed(() => vwCtrl.controllers.filter((c) => c.role === VW_ROLE.Distribution && c.id !== state.ruleForm.id));

/** Chỉ bộ hiển thị có cha ⇒ đổi sang vai trò khác thì bỏ cha. */
const onRoleChange = () => {
	if (state.ruleForm.role !== VW_ROLE.Display) state.ruleForm.parentControllerId = null;
};

const loadControllers = async () => {
	await vwCtrl.load();
};

const openDialog = async (row: any) => {
	ruleFormRef.value?.resetFields();
	state.ruleForm = JSON.parse(JSON.stringify(row));
	if (!state.ruleForm.colorHex) state.ruleForm.colorHex = VW_DEFAULT_CTRL_COLOR;
	// Một thiết bị chỉ gắn được 1 bộ điều khiển ⇒ bản sao phải chọn lại thiết bị tham chiếu.
	if (props.operateType == 'copy') state.ruleForm.refId = null;
	state.selectedTabName = 'tab1';
	state.isShowDialog = true;
	state.loading = true;
	try {
		await Promise.all([loadControllers(), loadEquipments()]);
	} finally {
		state.loading = false;
	}
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
	ruleFormRef.value.validate(async (valid: boolean, invalid?: Record<string, unknown>) => {
		if (!valid) {
			// Lỗi ở thiết bị tham chiếu (tab Thông tin thiết bị) ⇒ mở đúng tab đó
			const fields = Object.keys(invalid ?? {});
			state.selectedTabName = fields.length && fields.every((k) => k === 'refId') ? 'tab2' : 'tab1';
			return;
		}
		// Chỉ bộ hiển thị có cha; ô trống ⇒ null.
		if (state.ruleForm.role !== VW_ROLE.Display || !state.ruleForm.parentControllerId) state.ruleForm.parentControllerId = null;
		// Thông tin kết nối do BE chép từ thiết bị ⇒ không gửi lên.
		state.ruleForm.ip = null;
		state.ruleForm.port = null;
		state.ruleForm.account = null;
		state.ruleForm.passWord = null;
		//Edit
		if (props.operateType == 'edit') {
			await getAPI(VwControllerApi).apiVideowallVwcontrollerUpdatePost(state.ruleForm);
		}
		//Copy or Add
		else if (props.operateType == 'copy' || props.operateType == 'add') {
			state.ruleForm.id = '';
			// Trạng thái phát do BE quản, bản sao không kế thừa.
			state.ruleForm.activeSceneId = null;
			state.ruleForm.activeSceneAt = null;
			await getAPI(VwControllerApi).apiVideowallVwcontrollerAddPost(state.ruleForm);
		}
		ElMessage.success($t('lz.message.base.saveSuccess'));
		// Nạp lại store để các trang VideoWall khác thấy thay đổi (tên, mã, màu...).
		vwCtrl.load(true).catch(() => {});
		closeDialog();
	});
};

// Export object
defineExpose({ openDialog });
</script>

<style scoped>
/* 10 màu: 2 hàng × 5 ô */
.color-swatches {
	display: grid;
	grid-template-columns: repeat(5, 24px);
	gap: 6px;
}
.color-swatch {
	display: inline-flex;
	align-items: center;
	justify-content: center;
	width: 24px;
	height: 24px;
	padding: 0;
	border: 2px solid transparent;
	border-radius: 4px;
	color: #fff;
	cursor: pointer;
	box-shadow: 0 0 0 1px var(--el-border-color);
}
.color-swatch.is-active {
	border-color: #fff;
	box-shadow: 0 0 0 2px var(--el-text-color-primary);
}
.connection-hint {
	display: flex;
	align-items: center;
	gap: 6px;
	min-height: 32px;
	font-size: 12px;
	line-height: 1.4;
	color: var(--el-text-color-secondary);
}
</style>
