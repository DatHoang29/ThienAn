<template>
	<div class="sharedata-edit-partner-container">
		<el-dialog
			v-model="state.isShowDialog"
			draggable
			:close-on-click-modal="!state.loading"
			:close-on-press-escape="!state.loading"
			:show-close="!state.loading"
			destroy-on-close
			width="780px"
			class="sharedata-edit-partner-dialog"
		>
			<template #header>
				<div style="color: var(--el-color-white)">
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle">
						<ele-Edit />
					</el-icon>
					<span> {{ props.title }} </span>
				</div>
			</template>

			<!-- Khớp BE (PartnerCommandHandler Update): đang kết nối thì chỉ khoá tham số kết nối
			     (địa chỉ, cổng, endpoint, kiểu kết nối, cấu hình kết nối); tên, ghi chú vẫn sửa được. -->
			<el-alert v-if="isConnectionLocked" type="warning" :closable="false" show-icon style="margin-bottom: 10px"
				:title="$t('lz.label.sharedataPartner.connectionLockedConnected')" />

			<el-form :model="state.ruleForm" :rules="rules" ref="ruleFormRef" label-width="auto" label-position="left" autocomplete="off">
				<el-tabs v-model="state.activeTab" class="partner-tabs">
					<el-tab-pane :label="$t('lz.label.sharedataPartner.tabGeneral')" name="general">
						<el-row :gutter="10">
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="$t('lz.entity.sharedataPartner.code')" prop="code"
									:rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.sharedataPartner.code') }), trigger: 'blur' }]">
									<!-- Hộp thoại có thể mở ngay dưới con trỏ: tooltip đặt trên ô này đè lên tab, nên không giữ khi rê vào và không nhận chuột -->
									<el-tooltip effect="dark" :content="$t('lz.tooltip.sharedataPartner.codeLocked')" placement="top"
										:enterable="false" :popper-style="{ pointerEvents: 'none' }" :disabled="effectiveOperateType !== 'edit'">
										<el-input v-model="state.ruleForm.code" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.sharedataPartner.code') })"
											clearable :disabled="effectiveOperateType === 'edit'" />
									</el-tooltip>
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="$t('lz.entity.sharedataPartner.name')" prop="name"
									:rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.sharedataPartner.name') }), trigger: 'blur' }]">
									<el-input v-model="state.ruleForm.name" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.sharedataPartner.name') })" clearable />
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24">
								<el-form-item :label="$t('lz.entity.sharedataPartner.remark')" prop="remark" style="margin-bottom: 0;">
									<el-input v-model="state.ruleForm.remark" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.sharedataPartner.remark') })" clearable type="textarea" :rows="2" />
								</el-form-item>
							</el-col>
						</el-row>
					</el-tab-pane>

					<el-tab-pane :label="$t('lz.label.sharedataPartner.tabConnection')" name="connection">
						<el-row :gutter="10">
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="addressLabel" prop="address"
									:rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: addressLabel }), trigger: 'blur' }]">
									<el-input v-model="state.ruleForm.address" :placeholder="$t('lz.label.base.pleaseType', { input: addressLabel })" clearable :disabled="isConnectionLocked" />
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="isFtpType ? $t('lz.label.sharedataPartner.ftpPort') : $t('lz.entity.sharedataPartner.port')" prop="port"
									:rules="[{ required: true, message: $t('lz.validation.sharedataPartner.portRequired'), trigger: 'change' }]">
									<el-input-number v-model="state.ruleForm.port" :min="1" :max="65535" :step="1" :precision="0"
										class="w100" controls-position="right" :disabled="isConnectionLocked" />
								</el-form-item>
							</el-col>
							<el-col v-if="state.ruleForm.connectionType === 1" :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
								<el-form-item :label="$t('lz.entity.sharedataPartner.endPointApiUrl')" prop="endPointApiUrl">
									<el-input v-model="state.ruleForm.endPointApiUrl" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.sharedataPartner.endPointApiUrl') })" clearable :disabled="isConnectionLocked" />
									<el-tooltip v-if="Boolean(fullEndpointUrl)" effect="dark" :content="fullEndpointUrl" placement="top">
										<div class="full-endpoint-hint">
											{{ $t('lz.entity.sharedataPartner.fullAddress') }}: {{ fullEndpointUrl }}
										</div>
									</el-tooltip>
								</el-form-item>
							</el-col>
							<!-- Cấu hình kênh kết nối & Tham số Dictionary động -->
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="$t('lz.label.sharedataPartner.connectionType')" prop="connectionType">
									<el-select v-model="state.ruleForm.connectionType" class="w100" :placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.label.sharedataPartner.connectionType') })" :disabled="isConnectionLocked" @change="onConnectionTypeChange">
										<el-option :label="$t('lz.label.sharedataPartner.typeApi')" :value="1" />
										<el-option :label="$t('lz.label.sharedataPartner.typeFile')" :value="2" />
										<el-option :label="$t('lz.label.sharedataPartner.typeFtp')" :value="3" />
										<el-option :label="$t('lz.label.sharedataPartner.typeKafka')" :value="5" />
									</el-select>
								</el-form-item>
							</el-col>

							<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
								<el-divider content-position="left">
									{{ $t('lz.label.sharedataPartner.connectionConfig') }}
								</el-divider>
								<connection-config-form
									ref="connectionConfigFormRef"
									v-model="state.ruleForm.connectionConfig"
									:connection-type="state.ruleForm.connectionType"
									:disabled="isConnectionLocked"
								/>
							</el-col>
						</el-row>
					</el-tab-pane>
				</el-tabs>
			</el-form>

			<template #footer>
				<span class="dialog-footer">
					<el-button @click="cancel">{{ $t('lz.button.base.cancel') }}</el-button>
					<el-button type="primary" @click="submit" :loading="state.loading" :disabled="state.loading">{{ $t('lz.button.base.confirm') }}</el-button>
				</span>
			</template>
		</el-dialog>
	</div>
</template>

<script lang="ts" setup name="sharedataEditPartner">
import { computed, reactive, ref } from 'vue';
import type { FormInstance, FormRules } from 'element-plus';
import { ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { getAPI } from '/@/utils/axios-utils';
import { SessionState, ShareDataPartnerApi } from '/@/api-services/shareData';
import type { ShareDataPartnerOutput, ShareDataUpdatePartnerInput } from '/@/api-services/shareData';
import { OperateType } from '/@/types/constants';
import { ConnectionTypeEnum } from '/@/types/enums/baseEnum';
import connectionConfigForm from './connectionConfigForm.vue';

const { t } = useI18n();

interface Props {
	title: string;
	operateType: OperateType;
}
const props = defineProps<Props>();

interface Emits {
	(e: 'handleQuery'): void;
}
const emit = defineEmits<Emits>();

const ruleFormRef = ref<FormInstance>();
const connectionConfigFormRef = ref<InstanceType<typeof connectionConfigForm>>();

const state = reactive({
	isShowDialog: false,
	loading: false,
	isEdit: false,
	operateType: '' as OperateType,
	activeTab: 'general' as 'general' | 'connection',
	ruleForm: {} as ShareDataUpdatePartnerInput,
	/** Đối tác đang có phiên kết nối sống. */
	isConnected: false,
	/** Tham số kết nối gốc (chưa qua chuẩn hoá FTPS / định dạng lại JSON) — gửi trả nguyên khi đang khoá. */
	originalConnection: {} as Pick<ShareDataUpdatePartnerInput, 'address' | 'port' | 'endPointApiUrl' | 'connectionType' | 'connectionConfig'>,
});

const effectiveOperateType = computed(() => state.operateType || props.operateType);

/** Khớp BE: đang kết nối thì không đổi được tham số kết nối; đăng ký đang chạy không khoá form đối tác. */
const isConnectionLocked = computed(() => effectiveOperateType.value === 'edit' && state.isConnected);

// Phải giữ khớp với HttpDeliverySender.TryBuildEndpointUri
const fullEndpointUrl = computed(() => {
	if (state.ruleForm.connectionType !== 1) return '';
	let endpoint = (state.ruleForm.endPointApiUrl ?? '').trim();
	if (/^https?:\/\//i.test(endpoint)) return endpoint;
	const address = (state.ruleForm.address ?? '').trim();
	if (!address) return '';
	let scheme = 'http';
	let host = address;
	if (/^https:\/\//i.test(address)) {
		scheme = 'https';
		host = address.slice('https://'.length);
	} else if (/^http:\/\//i.test(address)) {
		host = address.slice('http://'.length);
	}
	host = host.replace(/\/+$/, '');
	if (!host) return '';
	const port = state.ruleForm.port;
	const portPart = port && port > 0 && !host.includes(':') ? `:${port}` : '';
	if (endpoint && !endpoint.startsWith('/')) endpoint = '/' + endpoint;
	return `${scheme}://${host}${portPart}${endpoint}`;
});

const rules: FormRules = {
	inboundApiUrl: [
		{
			validator: (_rule: any, value: string, callback: any) => {
				if (!value) return callback();
				try {
					const url = new URL(value);
					if (url.protocol !== 'http:' && url.protocol !== 'https:') {
						return callback(new Error(t('lz.validation.sharedataPartner.urlProtocol')));
					}
					return callback();
				} catch {
					return callback(new Error(t('lz.validation.sharedataPartner.urlInvalid')));
				}
			},
			trigger: 'blur',
		},
	],
	connectionConfig: [
		{
			validator: (_rule: any, _value: any, callback: any) => {
				if (connectionConfigFormRef.value?.hasErrors()) {
					return callback(new Error(t('lz.label.sharedataConnectionEditor.errHasInvalidKeys')));
				}
				return callback();
			},
			trigger: 'change',
		},
	],
};

function openDialog(row?: ShareDataPartnerOutput | Record<string, any>, operateType?: OperateType) {
	ruleFormRef.value?.resetFields();
	state.activeTab = 'general';
	state.operateType = operateType || (row?.id ? 'edit' : 'add');
	state.isEdit = Boolean((row as any)?.id);
	state.ruleForm = row ? JSON.parse(JSON.stringify(row)) : {};
	state.originalConnection = {
		address: row?.address,
		port: row?.port,
		endPointApiUrl: row?.endPointApiUrl,
		connectionType: row?.connectionType,
		connectionConfig: row?.connectionConfig,
	};
	if (!state.ruleForm.connectionType) {
		state.ruleForm.connectionType = ConnectionTypeEnum.Api;
	} else if (typeof state.ruleForm.connectionType === 'string') {
		const s = String(state.ruleForm.connectionType).toUpperCase();
		if (s === 'API') state.ruleForm.connectionType = ConnectionTypeEnum.Api;
		else if (s === 'FILE') state.ruleForm.connectionType = ConnectionTypeEnum.File;
		else if (s === 'FTP') state.ruleForm.connectionType = ConnectionTypeEnum.Ftp;
		else if (s === 'FTPS') state.ruleForm.connectionType = ConnectionTypeEnum.Ftps;
		else if (s === 'KAFKA') state.ruleForm.connectionType = ConnectionTypeEnum.Kafka;
		else state.ruleForm.connectionType = Number(state.ruleForm.connectionType) || ConnectionTypeEnum.Api;
	}
	let connConfig = row?.connectionConfig ?? null;
	if (state.ruleForm.connectionType === ConnectionTypeEnum.Ftps) {
		state.ruleForm.connectionType = ConnectionTypeEnum.Ftp;
		if (!connConfig || !connConfig.trim()) {
			connConfig = JSON.stringify({ UseSsl: 'true' });
		} else {
			try {
				const parsed = JSON.parse(connConfig);
				if (typeof parsed === 'object' && parsed !== null && !Array.isArray(parsed)) {
					const hasSslKey = Object.keys(parsed).some((k) => {
						const lower = k.trim().toLowerCase();
						return lower === 'usessl' || lower === 'enablessl';
					});
					if (!hasSslKey) {
						parsed.UseSsl = 'true';
						connConfig = JSON.stringify(parsed);
					}
				}
			} catch {
				// Giữ nguyên chuỗi JSON lỗi để form hiển thị cảnh báo
			}
		}
	}
	state.ruleForm.connectionConfig = connConfig;
	state.isConnected = effectiveOperateType.value === 'edit' && row?.sessionState === SessionState.NUMBER_1;
	state.isShowDialog = true;
}

// Close dialog
const closeDialog = () => {
	emit('handleQuery');
	state.isShowDialog = false;
};

// Cancel
const cancel = () => {
	state.isShowDialog = false;
};

const submit = async () => {
	if (!ruleFormRef.value) return;

	let invalidFields: Record<string, unknown> | undefined;
	const valid = await ruleFormRef.value
		.validate((isValid, fields) => {
			if (!isValid) invalidFields = fields;
		})
		.catch((fields: Record<string, unknown>) => {
			invalidFields = fields;
			return false;
		});

	if (!valid) {
		const errorProps = Object.keys(invalidFields ?? {});
		if (errorProps.some((p) => GENERAL_TAB_PROPS.has(p))) {
			state.activeTab = 'general';
		} else if (errorProps.length > 0 || connectionConfigFormRef.value?.hasErrors()) {
			state.activeTab = 'connection';
		}
		return;
	}

	if (connectionConfigFormRef.value?.hasErrors()) {
		state.activeTab = 'connection';
		ElMessage.error(t('lz.label.sharedataConnectionEditor.errHasInvalidKeys'));
		return;
	}

	state.loading = true;
	try {
		const payload = {
			...state.ruleForm,
		} as any;
		if (payload.connectionType != null) {
			payload.connectionType = Number(payload.connectionType) || ConnectionTypeEnum.Api;
		}
		if (payload.connectionType === ConnectionTypeEnum.Ftp && payload.connectionConfig) {
			try {
				const parsedConfig = JSON.parse(payload.connectionConfig);
				const useSslEntry = Object.entries(parsedConfig || {}).find(
					([k]) => k.trim().toLowerCase() === 'usessl' || k.trim().toLowerCase() === 'enablessl'
				);
				if (useSslEntry && String(useSslEntry[1]).trim().toLowerCase() === 'true') {
					payload.connectionType = ConnectionTypeEnum.Ftps;
				}
			} catch {
				// Bỏ qua nếu không phân tích được JSON
			}
		}
		if (payload.endPointApiUrl != null) {
			let ep = String(payload.endPointApiUrl).trim();
			if (ep && !ep.startsWith('/') && !ep.startsWith('http://') && !ep.startsWith('https://')) {
				ep = '/' + ep;
			}
			payload.endPointApiUrl = ep || null;
		}
		// Đang kết nối: gửi trả nguyên tham số kết nối gốc, tránh chuẩn hoá phía FE làm BE hiểu là đổi kết nối (PartnerSessionActive).
		if (isConnectionLocked.value) Object.assign(payload, state.originalConnection);
		if (effectiveOperateType.value === 'edit') {
			await getAPI(ShareDataPartnerApi).apiSharedataSharedatapartnerUpdatePost(payload);
		} else {
			payload.id = '';
			await getAPI(ShareDataPartnerApi).apiSharedataSharedatapartnerAddPost(payload);
		}
		ElMessage.success(t('lz.message.sharedataPartner.saveSuccess'));
		closeDialog();
	} finally {
		state.loading = false;
	}
};

const isFtpType = computed(() => state.ruleForm.connectionType === ConnectionTypeEnum.Ftp || state.ruleForm.connectionType === ConnectionTypeEnum.Ftps);
// Nhãn ô địa chỉ đổi theo kênh: dùng chung cho nhãn, placeholder và thông báo bắt buộc
const addressLabel = computed(() => (isFtpType.value ? t('lz.label.sharedataPartner.ftpHost') : t('lz.entity.sharedataPartner.address')));

const onConnectionTypeChange = () => {
	if (effectiveOperateType.value === 'edit') return;
	if (isFtpType.value && !state.ruleForm.port) state.ruleForm.port = 21;
};

const isFormValid = computed(() => {
	const codeValid = Boolean(state.ruleForm.code?.trim());
	const nameValid = Boolean(state.ruleForm.name?.trim());
	const addressValid = Boolean(state.ruleForm.address?.trim());
	const portValid = Boolean(state.ruleForm.port && state.ruleForm.port > 0 && state.ruleForm.port <= 65535);
	return codeValid && nameValid && addressValid && portValid;
});

const GENERAL_TAB_PROPS = new Set(['code', 'name', 'remark']);

defineExpose({ openDialog });
</script>

<style scoped lang="scss">
.mb10 {
	margin-bottom: 10px;
}
:deep(.el-dialog__body) {
	padding-bottom: 0;
}
:global(.sharedata-edit-partner-dialog .el-dialog__body),
:global(.el-overlay-dialog .el-dialog.sharedata-edit-partner-dialog .el-dialog__body) {
	padding-bottom: 0;
}
.full-endpoint-hint {
	width: 100%;
	margin-top: 4px;
	font-size: 12px;
	line-height: 1.4;
	color: var(--el-text-color-secondary);
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
}
.partner-tabs {
	:deep(.el-tabs__header) {
		margin-bottom: 14px;
	}
}
</style>
