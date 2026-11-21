<template>
	<div class="videowall-source-container">
		<el-dialog v-model="state.isShowDialog" draggable :close-on-click-modal="false" width="700px">
			<template #header>
				<div style="color: #fff">
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Edit /> </el-icon>
					<span> {{ props.title }} </span>
				</div>
			</template>
			<el-form :model="state.ruleForm" ref="ruleFormRef" label-width="auto" label-position="left" autocomplete="off">
				<el-row :gutter="10">
					<!-- Thông tin chung: mã, tên, bộ điều khiển (đứng trước cụm tín hiệu vì Cổng vào phụ thuộc bộ điều khiển) -->
					<el-col :span="24">
						<el-divider content-position="left">{{ $t('lz.label.base.basicInfo') }}</el-divider>
					</el-col>
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
					<!-- Tín hiệu cục bộ / Liên kết controller: bắt buộc chọn bộ điều khiển; Luồng IP cũng bắt buộc (không còn nguồn "dùng chung") -->
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item :label="$t('lz.entity.videoWall.controller')" prop="controllerId"
							:rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.videoWall.controller') }), trigger: 'change' }]">
							<!-- Cây bộ điều khiển cha → con (utils/videoWall/controllerTree.ts); chọn được cả bộ cha như trước (checkStrictly) -->
							<el-cascader v-model="state.ruleForm.controllerId" :options="controllerTree" :props="{ ...CONTROLLER_CASCADER_PROPS, checkStrictly: true }"
								:placeholder="$t('lz.label.videoWall.selectController')" filterable clearable class="w100" popper-class="vw-controller-cascader"
								@change="onControllerChange" />
						</el-form-item>
					</el-col>
					<!-- Tín hiệu đầu vào: Loại nguồn → Loại tín hiệu → Cổng vào → Độ phân giải → URL liền một cụm (không chèn bộ điều khiển vào giữa) -->
					<el-col :span="24">
						<el-divider content-position="left">{{ $t('lz.label.videoWall.inputSignalGroup') }}</el-divider>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.videoWall.sourceType')" prop="sourceType"
							:rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.videoWall.sourceType') }), trigger: 'change' }]">
							<el-select v-model="state.ruleForm.sourceType" :placeholder="$t('lz.label.videoWall.selectSourceType')" filterable clearable class="w100" @change="onSourceTypeChange">
								<el-option v-for="item in getConfigDataByCode(BaseConfigTypeEnum.VideoWallSourceType)" :key="item.code" :label="item.name" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col v-if="sourceType" :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.videoWall.signalType')" prop="signalType">
							<el-select v-model="state.ruleForm.signalType" :placeholder="$t('lz.label.videoWall.selectSignalType')" filterable clearable class="w100">
								<el-option v-for="item in signalTypeOptions" :key="item.code" :label="item.name" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col v-if="needsInputPort" :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<!-- Cổng vào vật lý đọc trực tiếp từ thiết bị (GET inputs/channels qua service VideoWall).
							 Giá trị lưu = videoInputChannelID (VwSource.SignalNo) — chính là mã service gửi xuống khi dựng cửa sổ. -->
						<!-- Không bắt buộc (12/11/2026): chưa có service / thiết bị ⇒ khai nguồn trước, chọn cổng sau -->
						<el-form-item :label="$t('lz.entity.videoWall.inputPort')" prop="signalNo">
							<div class="w100" style="display: flex; gap: 6px">
								<el-select v-model="state.ruleForm.signalNo" :placeholder="$t('lz.label.videoWall.selectInputPort')" filterable clearable style="flex: 1"
									:disabled="!state.ruleForm.controllerId" :loading="channelState.loading" @change="onChannelChange">
									<!-- Nhóm theo card vào; mỗi cổng: chấm tín hiệu + tên + loại cổng + trạng thái -->
									<el-option-group v-for="group in channelGroups" :key="group.key" :label="group.label">
										<!-- Cổng đã khai cho nguồn khác: xám + không chọn được, vẫn báo trạng thái tín hiệu -->
										<el-option v-for="ch in group.options" :key="ch.id" :label="ch.label" :value="ch.id" :disabled="!!ch.usedBy">
											<div class="vw-port-option" :class="{ 'is-used': ch.usedBy }">
												<span class="vw-port-dot" :class="ch.hasSignal ? 'is-on' : 'is-off'" />
												<span class="vw-port-name">{{ ch.name }}</span>
												<el-tag v-if="ch.portType" size="small" type="info" effect="plain" disable-transitions>{{ ch.portType }}</el-tag>
												<span v-if="ch.usedBy" class="vw-port-used" :title="ch.usedBy">{{ $t('lz.label.videoWall.portUsedBy', { name: ch.usedBy }) }}</span>
												<span class="vw-port-status" :class="ch.hasSignal ? 'is-on' : 'is-off'">{{ ch.statusText }}</span>
											</div>
										</el-option>
									</el-option-group>
									<!-- Ô đã chọn: chấm tín hiệu + tên cổng -->
									<template #label="{ value }">
										<span class="vw-port-option">
											<span class="vw-port-dot" :class="channelById.get(value)?.hasSignal ? 'is-on' : 'is-off'" />
											<span>{{ channelById.get(value)?.label ?? value }}</span>
										</span>
									</template>
								</el-select>
								<el-button :icon="Refresh" :loading="channelState.loading" :disabled="!state.ruleForm.controllerId" @click="loadChannels" />
							</div>
							<div v-if="channelState.error" class="el-form-item__error" style="position: static">{{ channelState.error }}</div>
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
						<el-form-item :label="$t('lz.entity.videoWall.maxResolution')" prop="maxResolution" :rules="[{ max: 128, message: $t('lz.validation.base.maxlength', { PropertyName: $t('lz.entity.videoWall.maxResolution'), MaxLength: 128 }), trigger: 'blur' }]">
							<!-- Danh mục videowall_resolution (Danh mục cấu hình); giá trị cũ không có trong danh mục vẫn hiện nguyên chuỗi -->
							<el-select v-model="state.ruleForm.maxResolution" :placeholder="$t('lz.label.videoWall.selectResolution')" filterable clearable class="w100">
								<el-option v-for="item in getConfigDataByCode(BaseConfigTypeEnum.VideoWallResolution)" :key="item.code" :label="item.name" :value="item.code" />
							</el-select>
						</el-form-item>
					</el-col>
					<el-col v-if="hasUrl" :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item :label="$t('lz.entity.base.url')" prop="url"
							:rules="[
								{ required: isIpStream, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.base.url') }), trigger: 'blur' },
								{ max: URL_MAX_LENGTH, message: $t('lz.validation.base.maxlength', { PropertyName: $t('lz.entity.base.url'), MaxLength: URL_MAX_LENGTH }), trigger: 'blur' },
							]">
							<el-input v-model="state.ruleForm.url" :placeholder="isIpStream ? 'rtsp://...' : $t('lz.label.videoWall.clientUrl')" clearable />
						</el-form-item>
					</el-col>
					<!-- Vận hành -->
					<el-col :span="24">
						<el-divider content-position="left">{{ $t('lz.entity.videoWall.moreInformation') }}</el-divider>
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

<script lang="ts" setup name="videoWallEditSource">
import { buildControllerTree, CONTROLLER_CASCADER_PROPS } from '/@/utils/videoWall/controllerTree';
import { computed, reactive, ref } from 'vue';
import { ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { getAPI } from '/@/utils/axios-utils';
import { Refresh } from '@element-plus/icons-vue';
import { VwSourceApi, VwDeviceSetupApi } from '/@/api-services/videoWall';
import { VwUpdateSourceInput, VwControllerOutput } from '/@/api-services/videoWall';
import { useVideoWallCommandResult } from '/@/transporter/hooks/useVideoWallCommandResult';
import { OperateType } from '/@/types/constants';
import { BaseConfigTypeEnum, VideoWallSourceTypeEnum } from '/@/types/enums/baseEnum';
import { useBaseConfig } from '/@/stores/baseConfig';

const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
const { t: $t } = useI18n();

interface Props {
	/** the type of operation */
	operateType: OperateType;
	title: string;
	/** Danh sách bộ điều khiển (trang cha nạp sẵn). */
	controllers: VwControllerOutput[];
}
const props = defineProps<Props>();
const emits = defineEmits(['handleQuery']);

/** Cây bộ điều khiển cha → con cho ô chọn bộ điều khiển. */
const controllerTree = computed(() => buildControllerTree(props.controllers));

/** Độ dài tối đa của URL nguồn — khớp BE VwSourceValidator (Length128). */
const URL_MAX_LENGTH = 128;

const ruleFormRef = ref();

/** Cổng vào vật lý đọc từ thiết bị (VwISAPIInputChannel, đã camelCase). */
interface DeviceInputChannel {
	id: number;
	name?: string;
	portType?: string;
	signalStatus?: string;
	portInBoard?: { boardId?: number; portId?: number };
}
const { waitFor } = useVideoWallCommandResult();
const channelState = reactive({ loading: false, error: '', channels: [] as DeviceInputChannel[], usedBy: {} as Record<number, string> });

/** Cổng (SignalNo) đã khai cho nguồn KHÁC trên cùng bộ điều khiển ⇒ tên nguồn. Lỗi tải không chặn chọn cổng. */
const loadUsedPorts = async (controllerId: string) => {
	const res = await getAPI(VwSourceApi).apiVideowallVwsourceListGet(undefined, undefined, controllerId);
	const selfId = props.operateType === 'edit' ? state.ruleForm.id : null;
	const usedBy: Record<number, string> = {};
	for (const s of res.data.result ?? []) {
		if (s.controllerId !== controllerId || s.signalNo == null || s.id === selfId) continue;
		usedBy[s.signalNo] = usedBy[s.signalNo] ? `${usedBy[s.signalNo]}, ${s.name}` : s.name ?? s.code ?? '';
	}
	return usedBy;
};
/** Chống ghi đè khi đổi bộ điều khiển liên tục: chỉ nhận kết quả của lần gọi mới nhất. */
let channelRequestSeq = 0;

/** Đọc cổng vào vật lý của bộ điều khiển đang chọn (service VideoWall trả kết quả qua NATS theo MessageId). */
const loadChannels = async () => {
	const controllerId = state.ruleForm.controllerId;
	const seq = ++channelRequestSeq;
	channelState.channels = [];
	channelState.usedBy = {};
	channelState.error = '';
	if (!controllerId) return;
	channelState.loading = true;
	loadUsedPorts(controllerId)
		.then((usedBy) => {
			if (seq !== channelRequestSeq) return;
			channelState.usedBy = usedBy;
			// Sao chép / thêm từ bản ghi có sẵn: cổng mang theo đã thuộc nguồn khác ⇒ bỏ chọn, bắt người dùng chọn cổng trống
			const current = state.ruleForm.signalNo;
			if (current != null && usedBy[current]) {
				state.ruleForm.signalNo = null;
				state.ruleForm.inputPort = null;
			}
		})
		.catch(() => {});
	try {
		const res = await getAPI(VwDeviceSetupApi).apiVideowallVwdevicesetupInputchannelsPost({ id: controllerId });
		const accepted = res.data.result;
		if (!accepted?.accepted || !accepted.messageId) throw new Error($t('lz.message.videoWall.commandRejected'));
		const result = await waitFor(accepted.messageId, 30_000);
		if (seq !== channelRequestSeq) return;
		if (!result.success) throw new Error(result.errorMessage || $t('lz.message.videoWall.commandRejected'));
		const list: DeviceInputChannel[] = result.data?.data?.videoInputChannel ?? [];
		channelState.channels = [...list].sort((a, b) => a.id - b.id);
	} catch (e: any) {
		if (seq !== channelRequestSeq) return;
		channelState.error = e?.message === 'timeout' ? $t('lz.message.videoWall.serviceTimeout') : e?.message;
	} finally {
		if (seq === channelRequestSeq) channelState.loading = false;
	}
};

/** Tuỳ chọn cổng: "Input 1-2 · HDMI · Có tín hiệu". Kênh đã lưu mà thiết bị không trả ⇒ vẫn hiện để không mất giá trị. */
interface ChannelOption {
	id: number;
	name: string;
	/** Chữ hiện trong ô đã chọn / dùng để lọc. */
	label: string;
	portType?: string;
	hasSignal: boolean;
	statusText: string;
	board: number | null;
	/** Tên nguồn khác đã khai cổng này (rỗng = còn trống). */
	usedBy?: string;
}
const channelOptions = computed<ChannelOption[]>(() => {
	const options: ChannelOption[] = channelState.channels.map((ch) => {
		const hasSignal = ch.signalStatus === 'signal';
		const name = ch.name ?? `#${ch.id}`;
		return {
			id: ch.id,
			name,
			label: ch.portType ? `${name} (${ch.portType})` : name,
			portType: ch.portType,
			hasSignal,
			statusText: hasSignal ? $t('lz.label.videoWall.hasSignal') : $t('lz.label.videoWall.noSignal'),
			board: ch.portInBoard?.boardId ?? null,
			usedBy: channelState.usedBy[ch.id],
		};
	});
	const saved = state.ruleForm.signalNo;
	if (saved != null && !options.some((o) => o.id === saved)) {
		const text = `#${saved}`;
		options.unshift({ id: saved, name: text, label: text, hasSignal: false, statusText: $t('lz.label.videoWall.savedChannel'), board: null });
	}
	return options;
});
const channelById = computed(() => new Map(channelOptions.value.map((o) => [o.id, o])));
/** Nhóm theo card vào (boardID); kênh không rõ card (VD kênh đã lưu) đứng nhóm đầu. */
const channelGroups = computed(() => {
	const groups = new Map<string, { key: string; label: string; options: ChannelOption[] }>();
	for (const o of channelOptions.value) {
		const key = o.board == null ? 'other' : `b${o.board}`;
		if (!groups.has(key))
			groups.set(key, { key, label: o.board == null ? $t('lz.label.videoWall.savedChannel') : $t('lz.label.videoWall.inputBoardN', { board: o.board }), options: [] });
		groups.get(key)!.options.push(o);
	}
	return [...groups.values()];
});

/** InputPort giữ số thứ tự cổng (1..N theo mã kênh) để các màn hình cũ vẫn hiển thị "Cổng vào N". */
const onChannelChange = (id: number | null) => {
	const index = channelState.channels.findIndex((ch) => ch.id === id);
	state.ruleForm.inputPort = index >= 0 ? index + 1 : null;
};
const state = reactive({
	isShowDialog: false,
	ruleForm: {} as VwUpdateSourceInput,
});

/*
 * Trường hiển thị theo loại nguồn (danh sách loại lấy từ danh mục videowall_source_type,
 * còn quy tắc hiển thị gắn với mã loại — cùng cách làm với form Menu hệ thống):
 *   - local_signal (Tín hiệu cục bộ)    : Bộ điều khiển (bắt buộc), Cổng vào (bắt buộc),
 *                                       Loại tín hiệu, URL (tuỳ chọn — trang client đang mở).
 *   - cascade_link (Liên kết controller): Bộ điều khiển (bắt buộc), Cổng vào (bắt buộc), Loại tín hiệu.
 *   Chưa phân biệt bộ trung tâm / bộ con: ô Bộ điều khiển liệt kê tất cả.
 *   - ip_stream (Camera IP)           : URL (bắt buộc), Loại tín hiệu, Bộ điều khiển (bỏ trống = dùng chung).
 *   - loại khác (dữ liệu cũ)          : Loại tín hiệu, Bộ điều khiển.
 */
const sourceType = computed(() => state.ruleForm.sourceType);
const isLocalSignal = computed(() => sourceType.value === VideoWallSourceTypeEnum.LocalSignal);
const isCascadeLink = computed(() => sourceType.value === VideoWallSourceTypeEnum.CascadeLink);
const isIpStream = computed(() => sourceType.value === VideoWallSourceTypeEnum.IpStream);
/** Nguồn đi qua cổng vào vật lý ⇒ bắt buộc chọn bộ điều khiển + cổng vào. */
const needsInputPort = computed(() => isLocalSignal.value || isCascadeLink.value);
const hasUrl = computed(() => isIpStream.value);


/**
 * Loại tín hiệu cho chọn — lọc theo `Value` của danh mục videowall_signal_type
 * (danh sách mã loại nguồn, cách nhau dấu phẩy, VD "local_signal,cascade_link").
 * Danh mục chưa khai `Value` cho mục nào thì không lọc.
 */
const signalTypeOptions = computed(() => {
	const all = getConfigDataByCode(BaseConfigTypeEnum.VideoWallSignalType) as { code: string; name: string; value?: string | null }[];
	if (!all.some((x) => x.value)) return all;
	return all.filter((x) => (x.value ?? '').split(',').map((s) => s.trim()).includes(sourceType.value ?? ''));
});

// Đổi loại nguồn: bỏ các giá trị không còn hợp lệ với loại mới.
const onSourceTypeChange = () => {
	const form = state.ruleForm;
	if (form.signalType && !signalTypeOptions.value.some((x) => x.code === form.signalType)) form.signalType = null;
	if (!form.signalType && signalTypeOptions.value.length === 1) form.signalType = signalTypeOptions.value[0].code;
	if (!needsInputPort.value) {
		form.inputPort = null;
		form.signalNo = null;
	} else if (!channelState.channels.length && !channelState.loading) void loadChannels();
	if (!hasUrl.value) form.url = null;
	ruleFormRef.value?.clearValidate();
};

// Đổi bộ điều khiển: cổng vào cũ không còn đúng ⇒ đọc lại cổng của bộ mới.
const onControllerChange = () => {
	state.ruleForm.inputPort = null;
	state.ruleForm.signalNo = null;
	if (needsInputPort.value) void loadChannels();
};

// Open dialog
const openDialog = (row: any) => {
	ruleFormRef.value?.resetFields();
	state.ruleForm = JSON.parse(JSON.stringify(row));
	channelRequestSeq++;
	channelState.channels = [];
	channelState.error = '';
	channelState.loading = false;
	state.isShowDialog = true;
	if (needsInputPort.value && state.ruleForm.controllerId) void loadChannels();
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
			await getAPI(VwSourceApi).apiVideowallVwsourceUpdatePost(state.ruleForm);
		}
		//Copy or Add
		else if (props.operateType == 'copy' || props.operateType == 'add') {
			state.ruleForm.id = '';
			await getAPI(VwSourceApi).apiVideowallVwsourceAddPost(state.ruleForm);
		}
		ElMessage.success($t('lz.message.base.saveSuccess'));
		closeDialog();
	});
};

// Export object
defineExpose({ openDialog });
</script>

<style scoped lang="scss">
.vw-port-option {
	display: flex;
	align-items: center;
	gap: 8px;
	width: 100%;
}
.vw-port-dot {
	flex: none;
	width: 8px;
	height: 8px;
	border-radius: 50%;
	&.is-on {
		background: var(--el-color-success);
		box-shadow: 0 0 0 3px var(--el-color-success-light-8);
	}
	&.is-off {
		background: var(--el-text-color-placeholder);
	}
}
.vw-port-name {
	font-weight: 500;
	min-width: 72px;
}
.vw-port-used {
	font-size: 12px;
	color: var(--el-text-color-placeholder);
	max-width: 160px;
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}
.vw-port-option.is-used {
	.vw-port-name {
		color: var(--el-text-color-placeholder);
		font-weight: 400;
	}
	.vw-port-dot {
		opacity: 0.55;
	}
}
.vw-port-status {
	margin-left: auto;
	font-size: 12px;
	&.is-on {
		color: var(--el-color-success);
	}
	&.is-off {
		color: var(--el-text-color-secondary);
	}
}
</style>
