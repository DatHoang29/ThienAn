<template>
	<div class="connection-config-form">
		<!-- Cảnh báo lỗi cú pháp JSON khi không thể phân tích cú pháp chuỗi cấu hình -->
		<el-alert
			v-if="rawInvalidJson"
			type="error"
			:closable="false"
			show-icon
			class="mb12"
			:title="$t('lz.label.sharedataConnectionEditor.errInvalidJson')"
		>
			<template #default>
				<div class="invalid-json-content mt5">
					<code class="raw-json-block">{{ rawInvalidJson }}</code>
					<div class="mt5">
						<el-button
							type="danger"
							size="small"
							plain
							:disabled="disabled"
							@click="clearInvalidJson"
						>
							{{ $t('lz.button.sharedataConnectionEditor.clearInvalidJson') }}
						</el-button>
					</div>
				</div>
			</template>
		</el-alert>

		<!-- DẠNG BIỂU MẪU (FORM VIEW) -->
		<div v-if="!rawInvalidJson" class="form-mode-container">
			<el-form label-position="top">
				<!-- ── 1. KÊNH API ── -->
				<div v-if="normalizedChannel === 'API'" class="channel-fields channel-api">
					<!-- Khối Xác thực -->
					<div class="section-title">
						{{ $t('lz.label.sharedataConnectionForm.sectionAuth') }}
					</div>

					<el-row :gutter="12">
						<el-col :xs="24" :sm="12" class="mb12">
							<el-form-item :label="$t('lz.label.sharedataConnectionForm.authType')">
								<el-select
									v-model="formState.api.authType"
									class="w100"
									:disabled="disabled"
									@change="onFieldChanged"
								>
									<el-option
										v-if="formState.api.authType && !VALID_AUTH_TYPES.includes(formState.api.authType)"
										:label="formState.api.authType"
										:value="formState.api.authType"
									/>
									<el-option :label="$t('lz.label.sharedataConnectionForm.authNone')" value="None" />
									<el-option :label="$t('lz.label.sharedataConnectionForm.authBearer')" value="Bearer" />
									<el-option :label="$t('lz.label.sharedataConnectionForm.authApiKey')" value="ApiKey" />
									<el-option :label="$t('lz.label.sharedataConnectionForm.authBasic')" value="Basic" />
								</el-select>
								<div v-if="formState.api.authType && !VALID_AUTH_TYPES.includes(formState.api.authType)" class="warning-text">
									{{ $t('lz.label.sharedataConnectionForm.warnInvalidEnum') }}
								</div>
							</el-form-item>
						</el-col>

						<!-- Bearer: chỉ hiện Token -->
						<el-col v-if="formState.api.authType === 'Bearer'" :xs="24" :sm="12" class="mb12">
							<el-form-item :label="$t('lz.label.sharedataConnectionForm.token')">
								<el-input
									v-model="formState.api.token"
									:type="showSecrets.apiToken ? 'text' : 'password'"
									:placeholder="$t('lz.label.sharedataConnectionForm.tokenPlaceholder')"
									clearable
									:disabled="disabled"
									@input="onFieldChanged"
								>
									<template #suffix>
										<el-icon class="cursor-pointer" @click="showSecrets.apiToken = !showSecrets.apiToken">
											<ele-View v-if="showSecrets.apiToken" />
											<ele-Hide v-else />
										</el-icon>
									</template>
								</el-input>
								<div v-if="!formState.api.token?.trim()" class="warning-text">
									{{ $t('lz.label.sharedataConnectionForm.warnTokenRequired') }}
								</div>
							</el-form-item>
						</el-col>

						<!-- ApiKey: hiện HeaderName và ApiKey -->
						<template v-if="formState.api.authType === 'ApiKey'">
							<el-col :xs="24" :sm="12" class="mb12">
								<el-form-item :label="$t('lz.label.sharedataConnectionForm.headerName')">
									<el-input
										v-model="formState.api.headerName"
										:placeholder="$t('lz.label.sharedataConnectionForm.headerNamePlaceholder')"
										clearable
										:disabled="disabled"
										@input="onFieldChanged"
									/>
								</el-form-item>
							</el-col>

							<el-col :xs="24" :sm="12" class="mb12">
								<el-form-item :label="$t('lz.label.sharedataConnectionForm.apiKey')">
									<el-input
										v-model="formState.api.apiKey"
										:type="showSecrets.apiKey ? 'text' : 'password'"
										:placeholder="$t('lz.label.sharedataConnectionForm.apiKeyPlaceholder')"
										clearable
										:disabled="disabled"
										@input="onFieldChanged"
									>
										<template #suffix>
											<el-icon class="cursor-pointer" @click="showSecrets.apiKey = !showSecrets.apiKey">
												<ele-View v-if="showSecrets.apiKey" />
												<ele-Hide v-else />
											</el-icon>
										</template>
									</el-input>
									<div v-if="!formState.api.apiKey?.trim()" class="warning-text">
										{{ $t('lz.label.sharedataConnectionForm.warnApiKeyRequired') }}
									</div>
								</el-form-item>
							</el-col>
						</template>

						<!-- Basic: hiện Username và Password -->
						<template v-if="formState.api.authType === 'Basic'">
							<el-col :xs="24" :sm="12" class="mb12">
								<el-form-item :label="$t('lz.label.sharedataConnectionForm.username')">
									<el-input
										v-model="formState.api.username"
										:placeholder="$t('lz.label.sharedataConnectionForm.usernamePlaceholder')"
										clearable
										:disabled="disabled"
										@input="onFieldChanged"
									/>
									<div v-if="!formState.api.username?.trim()" class="warning-text">
										{{ $t('lz.label.sharedataConnectionForm.warnUsernameRequired') }}
									</div>
								</el-form-item>
							</el-col>

							<el-col :xs="24" :sm="12" class="mb12">
								<el-form-item :label="$t('lz.label.sharedataConnectionForm.password')">
									<el-input
										v-model="formState.api.password"
										:type="showSecrets.apiPassword ? 'text' : 'password'"
										:placeholder="$t('lz.label.sharedataConnectionForm.passwordPlaceholder')"
										clearable
										:disabled="disabled"
										@input="onFieldChanged"
									>
										<template #suffix>
											<el-icon class="cursor-pointer" @click="showSecrets.apiPassword = !showSecrets.apiPassword">
												<ele-View v-if="showSecrets.apiPassword" />
												<ele-Hide v-else />
											</el-icon>
										</template>
									</el-input>
									<div v-if="!formState.api.password?.trim()" class="warning-text">
										{{ $t('lz.label.sharedataConnectionForm.warnPasswordRequired') }}
									</div>
								</el-form-item>
							</el-col>
						</template>
					</el-row>

					<!-- Khối Gửi dữ liệu -->
					<div class="section-title">
						{{ $t('lz.label.sharedataConnectionForm.sectionData') }}
					</div>

					<el-row :gutter="12">
						<el-col :xs="24" :sm="12" class="mb12">
							<el-form-item :label="$t('lz.label.sharedataConnectionForm.format')">
								<el-select
									v-model="formState.api.format"
									class="w100"
									clearable
									:placeholder="$t('lz.label.sharedataConnectionForm.formatJson')"
									:disabled="disabled"
									@change="onFieldChanged"
								>
									<el-option :label="$t('lz.label.sharedataConnectionForm.formatJson')" value="Json" />
									<el-option :label="$t('lz.label.sharedataConnectionForm.formatCsv')" value="Csv" />
								</el-select>
							</el-form-item>
						</el-col>

						<el-col :xs="24" :sm="12" class="mb12">
							<el-form-item :label="$t('lz.label.sharedataConnectionForm.timeoutSeconds')">
								<el-input-number
									v-model="formState.api.timeoutSeconds"
									:min="1"
									:max="300"
									:step="1"
									class="w100"
									controls-position="right"
									:placeholder="$t('lz.label.sharedataConnectionForm.timeoutApi')"
									:disabled="disabled"
									@change="onFieldChanged"
								/>
								<div
									v-if="formState.api.timeoutSeconds != null && (formState.api.timeoutSeconds <= 0 || !Number.isInteger(Number(formState.api.timeoutSeconds)))"
									class="warning-text"
								>
									{{ $t('lz.label.sharedataConnectionForm.warnTimeoutPositiveInt') }}
								</div>
							</el-form-item>
						</el-col>

						<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb12">
							<el-form-item :label="$t('lz.label.sharedataConnectionForm.bypassSsl')">
								<el-switch
									v-model="formState.api.bypassSsl"
									:disabled="disabled"
									@change="onFieldChanged"
								/>
							</el-form-item>
						</el-col>
					</el-row>
				</div>

				<!-- ── 2. KÊNH FTP ── -->
				<div v-else-if="normalizedChannel === 'FTP'" class="channel-fields channel-ftp">
					<div class="section-title">
						{{ $t('lz.label.sharedataConnectionForm.sectionFtpConn') }}
					</div>

					<el-row :gutter="12">
						<el-col :xs="24" :sm="12" class="mb12">
							<el-form-item :label="$t('lz.label.sharedataConnectionForm.username')">
								<el-input
									v-model="formState.ftp.username"
									:placeholder="$t('lz.label.sharedataConnectionForm.usernamePlaceholder')"
									clearable
									:disabled="disabled"
									@input="onFieldChanged"
								/>
							</el-form-item>
						</el-col>
						<el-col :xs="24" :sm="12" class="mb12">
							<el-form-item :label="$t('lz.label.sharedataConnectionForm.password')">
								<el-input
									v-model="formState.ftp.password"
									:type="showSecrets.ftpPassword ? 'text' : 'password'"
									:placeholder="$t('lz.label.sharedataConnectionForm.passwordPlaceholder')"
									clearable
									:disabled="disabled"
									@input="onFieldChanged"
								>
									<template #suffix>
										<el-icon class="cursor-pointer" @click="showSecrets.ftpPassword = !showSecrets.ftpPassword">
											<ele-View v-if="showSecrets.ftpPassword" />
											<ele-Hide v-else />
										</el-icon>
									</template>
								</el-input>
							</el-form-item>
						</el-col>
						<el-col :xs="24" :sm="12" class="mb12">
							<el-form-item :label="$t('lz.label.sharedataConnectionForm.useSsl')">
								<el-switch
									v-model="formState.ftp.useSsl"
									:disabled="disabled"
									@change="onFieldChanged"
								/>
							</el-form-item>
						</el-col>
						<el-col :xs="24" :sm="12" class="mb12">
							<el-form-item :label="$t('lz.label.sharedataConnectionForm.timeoutSeconds')">
								<el-input-number
									v-model="formState.ftp.timeoutSeconds"
									:min="1"
									:max="120"
									:step="1"
									class="w100"
									controls-position="right"
									:placeholder="$t('lz.label.sharedataConnectionForm.timeoutFtp')"
									:disabled="disabled"
									@change="onFieldChanged"
								/>
								<div
									v-if="formState.ftp.timeoutSeconds != null && (formState.ftp.timeoutSeconds < 1 || formState.ftp.timeoutSeconds > 120)"
									class="warning-text"
								>
									{{ $t('lz.label.sharedataConnectionForm.warnFtpTimeoutRange') }}
								</div>
							</el-form-item>
						</el-col>
					</el-row>

					<div class="section-title">
						{{ $t('lz.label.sharedataConnectionForm.sectionData') }}
					</div>

					<el-row :gutter="12">
						<el-col :xs="24" :sm="12" class="mb12">
							<el-form-item :label="$t('lz.label.sharedataConnectionForm.remotePath')">
								<el-input
									v-model="formState.ftp.remotePath"
									:placeholder="$t('lz.label.sharedataConnectionForm.remotePathPlaceholder')"
									clearable
									:disabled="disabled"
									@input="onFieldChanged"
								/>
							</el-form-item>
						</el-col>
						<el-col :xs="24" :sm="12" class="mb12">
							<el-form-item :label="$t('lz.label.sharedataConnectionForm.format')">
								<el-select
									v-model="formState.ftp.format"
									class="w100"
									clearable
									:placeholder="$t('lz.label.sharedataConnectionForm.formatJson')"
									:disabled="disabled"
									@change="onFieldChanged"
								>
									<el-option :label="$t('lz.label.sharedataConnectionForm.formatJson')" value="Json" />
									<el-option :label="$t('lz.label.sharedataConnectionForm.formatCsv')" value="Csv" />
								</el-select>
							</el-form-item>
						</el-col>
						<el-col :xs="24" :sm="12" class="mb12">
							<el-form-item>
								<template #label>
									<span>{{ $t('lz.label.sharedataConnectionForm.fileName') }}</span>
									<el-tooltip
										placement="top"
										effect="dark"
										popper-class="connection-config-tooltip"
									>
										<template #content>
											<div>
												<div>{{ $t('lz.label.sharedataConnectionForm.fileNameHint') }}</div>
												<div style="color: #e6a23c; margin-top: 4px">{{ $t('lz.label.sharedataConnectionForm.fileNameHintFixed') }}</div>
											</div>
										</template>
										<el-icon class="label-help-icon"><ele-Warning /></el-icon>
									</el-tooltip>
								</template>
								<el-input
									v-model="formState.ftp.fileName"
									:placeholder="$t('lz.label.sharedataConnectionForm.fileNamePlaceholder')"
									clearable
									:disabled="disabled"
									@input="onFieldChanged"
								/>
							</el-form-item>
						</el-col>
					</el-row>
				</div>

				<!-- ── 3. KÊNH KAFKA ── -->
				<div v-else-if="normalizedChannel === 'Kafka'" class="channel-fields channel-kafka">
					<!-- Khối Kết nối & Giao thức -->
					<div class="section-title">
						{{ $t('lz.label.sharedataConnectionForm.sectionKafkaConn') }}
					</div>

					<el-row :gutter="12">
						<el-col :xs="24" :sm="12" class="mb12">
							<el-form-item :label="$t('lz.label.sharedataConnectionForm.bootstrapServers')">
								<el-input
									v-model="formState.kafka.bootstrapServers"
									:placeholder="$t('lz.label.sharedataConnectionForm.bootstrapServersHint')"
									clearable
									:disabled="disabled"
									@input="onFieldChanged"
								/>
							</el-form-item>
						</el-col>
						<el-col :xs="24" :sm="12" class="mb12">
							<el-form-item :label="$t('lz.label.sharedataConnectionForm.topic')">
								<el-input
									v-model="formState.kafka.topic"
									:placeholder="$t('lz.label.sharedataConnectionForm.topicHint')"
									clearable
									:disabled="disabled"
									@input="onFieldChanged"
								/>
							</el-form-item>
						</el-col>
						<el-col :xs="24" :sm="12" class="mb12">
							<el-form-item :label="$t('lz.label.sharedataConnectionForm.clientId')">
								<el-input
									v-model="formState.kafka.clientId"
									:placeholder="$t('lz.label.sharedataConnectionForm.clientIdPlaceholder')"
									clearable
									:disabled="disabled"
									@input="onFieldChanged"
								/>
							</el-form-item>
						</el-col>
						<el-col :xs="24" :sm="12" class="mb12">
							<el-form-item :label="$t('lz.label.sharedataConnectionForm.securityProtocol')">
								<el-select
									v-model="formState.kafka.securityProtocol"
									class="w100"
									clearable
									:disabled="disabled"
									@change="onFieldChanged"
								>
									<el-option
										v-if="formState.kafka.securityProtocol && !VALID_SECURITY_PROTOCOLS.includes(formState.kafka.securityProtocol)"
										:label="formState.kafka.securityProtocol"
										:value="formState.kafka.securityProtocol"
									/>
									<el-option :label="$t('lz.label.sharedataConnectionForm.protoPlaintext')" value="Plaintext" />
									<el-option :label="$t('lz.label.sharedataConnectionForm.protoSsl')" value="Ssl" />
									<el-option :label="$t('lz.label.sharedataConnectionForm.protoSaslPlaintext')" value="SaslPlaintext" />
									<el-option :label="$t('lz.label.sharedataConnectionForm.protoSaslSsl')" value="SaslSsl" />
								</el-select>
								<div
									v-if="formState.kafka.securityProtocol && !VALID_SECURITY_PROTOCOLS.includes(formState.kafka.securityProtocol)"
									class="warning-text"
								>
									{{ $t('lz.label.sharedataConnectionForm.warnInvalidEnum') }}
								</div>
							</el-form-item>
						</el-col>
					</el-row>

					<!-- Khối Xác thực SASL & SSL -->
					<div class="section-title">
						{{ $t('lz.label.sharedataConnectionForm.sectionKafkaSasl') }}
					</div>

					<el-row :gutter="12">
						<el-col :xs="24" :sm="12" class="mb12">
							<el-form-item :label="$t('lz.label.sharedataConnectionForm.saslMechanism')">
								<el-select
									v-model="formState.kafka.saslMechanism"
									class="w100"
									clearable
									:disabled="disabled"
									@change="onFieldChanged"
								>
									<el-option
										v-if="formState.kafka.saslMechanism && !VALID_SASL_MECHANISMS.includes(formState.kafka.saslMechanism)"
										:label="formState.kafka.saslMechanism"
										:value="formState.kafka.saslMechanism"
									/>
									<el-option label="PLAIN" value="Plain" />
									<el-option label="SCRAM-SHA-256" value="ScramSha256" />
									<el-option label="SCRAM-SHA-512" value="ScramSha512" />
									<el-option label="GSSAPI" value="Gssapi" />
									<el-option label="OAuthBearer" value="OAuthBearer" />
								</el-select>
								<div
									v-if="formState.kafka.saslMechanism && !VALID_SASL_MECHANISMS.includes(formState.kafka.saslMechanism)"
									class="warning-text"
								>
									{{ $t('lz.label.sharedataConnectionForm.warnInvalidSaslMechanism') }}
								</div>
							</el-form-item>
						</el-col>

						<el-col :xs="24" :sm="12" class="mb12">
							<el-form-item :label="$t('lz.label.sharedataConnectionForm.saslUsername')">
								<el-input
									v-model="formState.kafka.saslUsername"
									:placeholder="$t('lz.label.sharedataConnectionForm.usernamePlaceholder')"
									clearable
									:disabled="disabled"
									@input="onFieldChanged"
								/>
								<div v-if="hasSaslProtocol && !formState.kafka.saslUsername?.trim()" class="warning-text">
									{{ $t('lz.label.sharedataConnectionForm.warnSaslUserRequired') }}
								</div>
							</el-form-item>
						</el-col>

						<el-col :xs="24" :sm="12" class="mb12">
							<el-form-item :label="$t('lz.label.sharedataConnectionForm.saslPassword')">
								<el-input
									v-model="formState.kafka.saslPassword"
									:type="showSecrets.kafkaSaslPassword ? 'text' : 'password'"
									:placeholder="$t('lz.label.sharedataConnectionForm.passwordPlaceholder')"
									clearable
									:disabled="disabled"
									@input="onFieldChanged"
								>
									<template #suffix>
										<el-icon class="cursor-pointer" @click="showSecrets.kafkaSaslPassword = !showSecrets.kafkaSaslPassword">
											<ele-View v-if="showSecrets.kafkaSaslPassword" />
											<ele-Hide v-else />
										</el-icon>
									</template>
								</el-input>
								<div v-if="hasSaslProtocol && !formState.kafka.saslPassword?.trim()" class="warning-text">
									{{ $t('lz.label.sharedataConnectionForm.warnSaslPassRequired') }}
								</div>
							</el-form-item>
						</el-col>

						<el-col :xs="24" :sm="12" class="mb12">
							<el-form-item :label="$t('lz.label.sharedataConnectionForm.sslCaLocation')">
								<el-input
									v-model="formState.kafka.sslCaLocation"
									:placeholder="$t('lz.label.sharedataConnectionForm.sslCaLocationPlaceholder')"
									clearable
									:disabled="disabled"
									@input="onFieldChanged"
								/>
							</el-form-item>
						</el-col>
					</el-row>

					<!-- Khối Gửi dữ liệu Kafka -->
					<div class="section-title">
						{{ $t('lz.label.sharedataConnectionForm.sectionData') }}
					</div>

					<el-row :gutter="12">
						<el-col :xs="24" :sm="12" class="mb12">
							<el-form-item :label="$t('lz.label.sharedataConnectionForm.timeoutMs')">
								<el-input-number
									v-model="formState.kafka.timeoutMs"
									:min="500"
									:max="60000"
									:step="500"
									class="w100"
									controls-position="right"
									:placeholder="$t('lz.label.sharedataConnectionForm.timeoutKafka')"
									:disabled="disabled"
									@change="onFieldChanged"
								/>
								<div
									v-if="formState.kafka.timeoutMs != null && (formState.kafka.timeoutMs < 500 || formState.kafka.timeoutMs > 60000)"
									class="warning-text"
								>
									{{ $t('lz.label.sharedataConnectionForm.warnKafkaTimeoutRange') }}
								</div>
							</el-form-item>
						</el-col>
					</el-row>
				</div>

				<div v-if="preservedParamsCount > 0" class="preserved-params-hint mt12">
					<el-icon class="mr4 text-muted"><ele-InfoFilled /></el-icon>
					<span class="text-muted">{{ $t('lz.label.sharedataConnectionForm.preservedParamsNotice', { count: preservedParamsCount }) }}</span>
				</div>
			</el-form>
		</div>
	</div>
</template>

<script lang="ts" setup name="connectionConfigForm">
import { computed, reactive, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';

interface Props {
	modelValue?: string | null;
	connectionType?: string | number | null;
	disabled?: boolean;
}
const props = withDefaults(defineProps<Props>(), {
	modelValue: '',
	connectionType: 'API',
	disabled: false,
});

const emit = defineEmits<{
	(e: 'update:modelValue', value: string | null): void;
}>();

const { t } = useI18n();

const rawInvalidJson = ref<string | null>(null);
const extraParamsJson = ref<string | null>(null);

let lastEmittedJson: string | null = null;

const showSecrets = reactive({
	apiToken: false,
	apiKey: false,
	apiPassword: false,
	ftpPassword: false,
	kafkaSaslPassword: false,
});

const VALID_AUTH_TYPES = ['None', 'Bearer', 'ApiKey', 'Basic'];
const VALID_SECURITY_PROTOCOLS = ['Plaintext', 'Ssl', 'SaslPlaintext', 'SaslSsl'];
const VALID_SASL_MECHANISMS = ['Plain', 'ScramSha256', 'ScramSha512', 'Gssapi', 'OAuthBearer'];
const VALID_ENCRYPTORS = ['None', 'AesGcm'];

function createInitialFormState() {
	return {
		api: {
			authType: 'None',
			token: '',
			headerName: '',
			apiKey: '',
			username: '',
			password: '',
			format: 'Json',
			timeoutSeconds: 30 as number | null,
			bypassSsl: false,
		},
		ftp: {
			remotePath: '',
			username: '',
			password: '',
			useSsl: false,
			format: 'Json',
			timeoutSeconds: 30 as number | null,
			fileName: '',
		},
		kafka: {
			bootstrapServers: '',
			topic: '',
			clientId: '',
			securityProtocol: 'Plaintext',
			saslMechanism: 'Plain',
			saslUsername: '',
			saslPassword: '',
			sslCaLocation: '',
			timeoutMs: null as number | null,
		},
		encryption: {
			encryptor: 'None',
			encryptionKeyRef: '',
			inboundEncryptor: 'None',
			decryptionKeyRef: '',
		},
	};
}

const formState = reactive(createInitialFormState());

const normalizeChannel = (val?: string | number | null): string => {
	if (val == null) return 'API';
	if (typeof val === 'number') {
		if (val === 1) return 'API';
		if (val === 3 || val === 4) return 'FTP';
		if (val === 5) return 'Kafka';
		return 'API';
	}
	const str = String(val).trim();
	if (!str) return 'API';
	if (str === '1') return 'API';
	if (str === '3' || str === '4') return 'FTP';
	if (str === '5') return 'Kafka';
	const upper = str.toUpperCase();
	if (upper === 'API' || upper === 'REST' || upper === 'HTTP' || upper === 'WEBAPI') return 'API';
	if (upper === 'FTP' || upper === 'FTPS') return 'FTP';
	if (upper === 'KAFKA') return 'Kafka';
	return 'API';
};

const normalizedChannel = computed(() => normalizeChannel(props.connectionType));

const hasSaslProtocol = computed(() => {
	const proto = formState.kafka.securityProtocol;
	return proto === 'SaslPlaintext' || proto === 'SaslSsl';
});

const currentManagedKeys = computed(() => {
	const common = [
		'Encryptor',
		'EncryptionKeyRef',
		'InboundEncryptor',
		'DecryptionKeyRef',
	];

	const ch = normalizedChannel.value;
	if (ch === 'API') {
		return [
			...common,
			'AuthType',
			'Token',
			'HeaderName',
			'ApiKeyHeader',
			'ApiKey',
			'Username',
			'Password',
			'Format',
			'TimeoutSeconds',
			'HttpTimeoutSec',
			'TimeoutSec',
			'BypassSsl',
			'IgnoreSsl',
			'Insecure',
			'SuccessField',
			'ErrorMessageField',
			'ErrorCodeField',
			'ResponseContract',
		];
	}
	if (ch === 'FTP') {
		return [
			...common,
			'Enabled',
			'RemotePath',
			'Username',
			'User',
			'Password',
			'Pass',
			'UseSsl',
			'EnableSsl',
			'Format',
			'TimeoutSeconds',
			'FileName',
		];
	}
	if (ch === 'Kafka') {
		return [
			...common,
			'Enabled',
			'BootstrapServers',
			'Topic',
			'Channel',
			'ClientId',
			'SecurityProtocol',
			'SaslMechanism',
			'SaslUsername',
			'SaslPassword',
			'SslCaLocation',
			'TimeoutMs',
		];
	}
	return common;
});

const clearInvalidJson = () => {
	rawInvalidJson.value = null;
	lastEmittedJson = null;
	emit('update:modelValue', null);
};

const resetAllState = () => {
	const initial = createInitialFormState();
	Object.assign(formState.api, initial.api);
	Object.assign(formState.ftp, initial.ftp);
	Object.assign(formState.kafka, initial.kafka);
	Object.assign(formState.encryption, initial.encryption);
	showSecrets.apiToken = false;
	showSecrets.apiKey = false;
	showSecrets.apiPassword = false;
	showSecrets.ftpPassword = false;
	showSecrets.kafkaSaslPassword = false;
	extraParamsJson.value = null;
	rawInvalidJson.value = null;
};

const parseJsonToForm = (jsonStr?: string | null) => {
	if (!jsonStr || !jsonStr.trim()) {
		extraParamsJson.value = null;
		rawInvalidJson.value = null;
		return;
	}

	const trimmed = jsonStr.trim();
	let parsed: Record<string, any>;
	try {
		parsed = JSON.parse(trimmed);
		if (typeof parsed !== 'object' || parsed === null || Array.isArray(parsed)) {
			rawInvalidJson.value = jsonStr;
			return;
		}
		rawInvalidJson.value = null;
	} catch {
		rawInvalidJson.value = jsonStr;
		return;
	}

	const extraObj: Record<string, string> = {};
	const managedSet = new Set(currentManagedKeys.value.map((k) => k.toLowerCase()));

	const inputMap = new Map<string, { originalKey: string; value: any }>();
	Object.entries(parsed).forEach(([k, v]) => {
		inputMap.set(k.trim().toLowerCase(), { originalKey: k, value: v });
	});

	const resolveFieldWithAliases = (canonicalKey: string, aliasKeys: string[] = []): string | null => {
		const canLower = canonicalKey.toLowerCase();
		const hasCanonical = inputMap.has(canLower);
		const foundAliases = aliasKeys.filter((a) => inputMap.has(a.toLowerCase()));

		if (hasCanonical) {
			const canVal = inputMap.get(canLower)!.value;
			foundAliases.forEach((aKey) => {
				const aInfo = inputMap.get(aKey.toLowerCase())!;
				extraObj[aInfo.originalKey] = aInfo.value != null ? String(aInfo.value) : '';
			});
			return canVal != null ? String(canVal) : '';
		}

		if (foundAliases.length > 0) {
			const firstAlias = foundAliases[0];
			const firstVal = inputMap.get(firstAlias.toLowerCase())!.value;
			for (let i = 1; i < foundAliases.length; i++) {
				const extraAlias = foundAliases[i];
				const aInfo = inputMap.get(extraAlias.toLowerCase())!;
				extraObj[aInfo.originalKey] = aInfo.value != null ? String(aInfo.value) : '';
			}
			return firstVal != null ? String(firstVal) : '';
		}

		return null;
	};

	// 1. Phân giải API
	const apiAuth = resolveFieldWithAliases('AuthType');
	if (apiAuth != null) formState.api.authType = apiAuth;
	const apiToken = resolveFieldWithAliases('Token');
	if (apiToken != null) formState.api.token = apiToken;
	const apiHeader = resolveFieldWithAliases('HeaderName', ['ApiKeyHeader']);
	if (apiHeader != null) formState.api.headerName = apiHeader;
	const apiApiKey = resolveFieldWithAliases('ApiKey');
	if (apiApiKey != null) formState.api.apiKey = apiApiKey;
	const apiUser = resolveFieldWithAliases('Username');
	if (apiUser != null) formState.api.username = apiUser;
	const apiPass = resolveFieldWithAliases('Password');
	if (apiPass != null) formState.api.password = apiPass;
	const apiFmt = resolveFieldWithAliases('Format');
	formState.api.format = (apiFmt != null && apiFmt.trim() !== '') ? apiFmt : 'Json';
	const apiTimeout = resolveFieldWithAliases('TimeoutSeconds', ['HttpTimeoutSec', 'TimeoutSec']);
	if (apiTimeout != null && !Number.isNaN(Number(apiTimeout))) {
		const num = parseInt(apiTimeout, 10);
		formState.api.timeoutSeconds = num > 0 ? num : 30;
	} else {
		formState.api.timeoutSeconds = 30;
	}
	const apiBypass = resolveFieldWithAliases('BypassSsl', ['IgnoreSsl', 'Insecure']);
	if (apiBypass != null) formState.api.bypassSsl = apiBypass.toLowerCase() === 'true';

	// 2. Phân giải FTP
	const ftpRemote = resolveFieldWithAliases('RemotePath');
	if (ftpRemote != null) formState.ftp.remotePath = ftpRemote;
	const ftpUser = resolveFieldWithAliases('Username', ['User']);
	if (ftpUser != null) formState.ftp.username = ftpUser;
	const ftpPass = resolveFieldWithAliases('Password', ['Pass']);
	if (ftpPass != null) formState.ftp.password = ftpPass;
	const ftpSsl = resolveFieldWithAliases('UseSsl', ['EnableSsl']);
	if (ftpSsl != null) {
		formState.ftp.useSsl = ftpSsl.toLowerCase() === 'true';
	} else if (props.connectionType === 4 || props.connectionType === '4' || String(props.connectionType ?? '').trim().toUpperCase() === 'FTPS') {
		formState.ftp.useSsl = true;
	}
	const ftpFmt = resolveFieldWithAliases('Format');
	formState.ftp.format = (ftpFmt != null && ftpFmt.trim() !== '') ? ftpFmt : 'Json';
	const ftpTimeout = resolveFieldWithAliases('TimeoutSeconds');
	if (ftpTimeout != null && !Number.isNaN(Number(ftpTimeout))) {
		const num = parseInt(ftpTimeout, 10);
		formState.ftp.timeoutSeconds = num > 0 ? num : 30;
	} else {
		formState.ftp.timeoutSeconds = 30;
	}
	const ftpFileName = resolveFieldWithAliases('FileName');
	if (ftpFileName != null) formState.ftp.fileName = ftpFileName;

	// 3. Phân giải Kafka
	const kafkaServers = resolveFieldWithAliases('BootstrapServers');
	if (kafkaServers != null) formState.kafka.bootstrapServers = kafkaServers;
	const kafkaTopic = resolveFieldWithAliases('Topic', ['Channel']);
	if (kafkaTopic != null) formState.kafka.topic = kafkaTopic;
	const kafkaClient = resolveFieldWithAliases('ClientId');
	if (kafkaClient != null) formState.kafka.clientId = kafkaClient;
	const kafkaProto = resolveFieldWithAliases('SecurityProtocol');
	if (kafkaProto != null) formState.kafka.securityProtocol = kafkaProto;
	const kafkaSaslMech = resolveFieldWithAliases('SaslMechanism');
	if (kafkaSaslMech != null) formState.kafka.saslMechanism = kafkaSaslMech;
	const kafkaSaslUser = resolveFieldWithAliases('SaslUsername');
	if (kafkaSaslUser != null) formState.kafka.saslUsername = kafkaSaslUser;
	const kafkaSaslPass = resolveFieldWithAliases('SaslPassword');
	if (kafkaSaslPass != null) formState.kafka.saslPassword = kafkaSaslPass;
	const kafkaSslCa = resolveFieldWithAliases('SslCaLocation');
	if (kafkaSslCa != null) formState.kafka.sslCaLocation = kafkaSslCa;
	const kafkaTimeout = resolveFieldWithAliases('TimeoutMs');
	if (kafkaTimeout != null && !Number.isNaN(Number(kafkaTimeout))) {
		const num = parseInt(kafkaTimeout, 10);
		if (num > 0) formState.kafka.timeoutMs = num;
	}

	// 4. Phân giải Mã hóa chung
	const encOut = resolveFieldWithAliases('Encryptor');
	if (encOut != null) formState.encryption.encryptor = encOut || 'None';
	const encOutKey = resolveFieldWithAliases('EncryptionKeyRef');
	if (encOutKey != null) formState.encryption.encryptionKeyRef = encOutKey;
	const encIn = resolveFieldWithAliases('InboundEncryptor');
	if (encIn != null) formState.encryption.inboundEncryptor = encIn || 'None';
	const encInKey = resolveFieldWithAliases('DecryptionKeyRef');
	if (encInKey != null) formState.encryption.decryptionKeyRef = encInKey;

	// Gom các khóa lạ (không nằm trong danh sách trường của kênh hiện tại) vào extraObj
	Object.entries(parsed).forEach(([k, v]) => {
		const lower = k.trim().toLowerCase();
		if (!managedSet.has(lower)) {
			extraObj[k] = v != null ? String(v) : '';
		}
	});

	if (Object.keys(extraObj).length > 0) {
		extraParamsJson.value = JSON.stringify(extraObj);
	} else {
		extraParamsJson.value = null;
	}
};

const serializeFormToJson = (): string | null => {
	const ch = normalizedChannel.value;
	const out: Record<string, string> = {};

	if (ch === 'API') {
		const auth = formState.api.authType;
		if (auth && auth !== 'None') {
			out['AuthType'] = auth;
		}
		if (auth === 'Bearer') {
			if (formState.api.token) out['Token'] = formState.api.token;
		} else if (auth === 'ApiKey') {
			if (formState.api.headerName?.trim()) out['HeaderName'] = formState.api.headerName.trim();
			if (formState.api.apiKey) out['ApiKey'] = formState.api.apiKey;
		} else if (auth === 'Basic') {
			if (formState.api.username?.trim()) out['Username'] = formState.api.username.trim();
			if (formState.api.password) out['Password'] = formState.api.password;
		}
		if (formState.api.format) out['Format'] = formState.api.format;
		if (formState.api.timeoutSeconds != null && formState.api.timeoutSeconds > 0) {
			out['TimeoutSeconds'] = String(formState.api.timeoutSeconds);
		}
		if (formState.api.bypassSsl) out['BypassSsl'] = 'true';
	} else if (ch === 'FTP') {
		if (formState.ftp.remotePath?.trim()) out['RemotePath'] = formState.ftp.remotePath.trim();
		if (formState.ftp.username?.trim()) out['Username'] = formState.ftp.username.trim();
		if (formState.ftp.password) out['Password'] = formState.ftp.password;
		if (formState.ftp.useSsl) out['UseSsl'] = 'true';
		if (formState.ftp.format) out['Format'] = formState.ftp.format;
		if (formState.ftp.timeoutSeconds != null && formState.ftp.timeoutSeconds > 0) {
			out['TimeoutSeconds'] = String(formState.ftp.timeoutSeconds);
		}
		if (formState.ftp.fileName?.trim()) out['FileName'] = formState.ftp.fileName.trim();
	} else if (ch === 'Kafka') {
		if (formState.kafka.bootstrapServers?.trim()) out['BootstrapServers'] = formState.kafka.bootstrapServers.trim();
		if (formState.kafka.topic?.trim()) out['Topic'] = formState.kafka.topic.trim();
		if (formState.kafka.clientId?.trim()) out['ClientId'] = formState.kafka.clientId.trim();
		if (formState.kafka.securityProtocol?.trim()) out['SecurityProtocol'] = formState.kafka.securityProtocol.trim();
		if (formState.kafka.saslMechanism?.trim()) out['SaslMechanism'] = formState.kafka.saslMechanism.trim();
		if (formState.kafka.saslUsername?.trim()) out['SaslUsername'] = formState.kafka.saslUsername.trim();
		if (formState.kafka.saslPassword) out['SaslPassword'] = formState.kafka.saslPassword;
		if (formState.kafka.sslCaLocation?.trim()) out['SslCaLocation'] = formState.kafka.sslCaLocation.trim();
		if (formState.kafka.timeoutMs != null && formState.kafka.timeoutMs > 0) {
			out['TimeoutMs'] = String(formState.kafka.timeoutMs);
		}
	}

	// Mã hóa dùng chung
	if (formState.encryption.encryptor && formState.encryption.encryptor !== 'None') {
		out['Encryptor'] = formState.encryption.encryptor;
	}
	if (formState.encryption.encryptionKeyRef?.trim()) {
		out['EncryptionKeyRef'] = formState.encryption.encryptionKeyRef.trim();
	}
	if (formState.encryption.inboundEncryptor && formState.encryption.inboundEncryptor !== 'None') {
		out['InboundEncryptor'] = formState.encryption.inboundEncryptor;
	}
	if (formState.encryption.decryptionKeyRef?.trim()) {
		out['DecryptionKeyRef'] = formState.encryption.decryptionKeyRef.trim();
	}

	// Gộp các Tham số bổ sung (nếu có)
	if (extraParamsJson.value && extraParamsJson.value.trim()) {
		try {
			const extra = JSON.parse(extraParamsJson.value.trim());
			if (typeof extra === 'object' && extra !== null && !Array.isArray(extra)) {
				Object.entries(extra).forEach(([k, v]) => {
					const trimmedK = k.trim();
					if (trimmedK) {
						out[trimmedK] = v != null ? String(v) : '';
					}
				});
			}
		} catch {
			// Bỏ qua lỗi parse extra
		}
	}

	const keys = Object.keys(out);
	return keys.length > 0 ? JSON.stringify(out) : null;
};

const onFieldChanged = () => {
	if (rawInvalidJson.value) return;
	const json = serializeFormToJson();
	lastEmittedJson = json;
	emit('update:modelValue', json);
};

watch(
	() => props.modelValue,
	(newVal) => {
		const currentJson = serializeFormToJson();
		if (newVal === lastEmittedJson || (newVal === null && currentJson === null) || (newVal?.trim() === currentJson?.trim())) {
			return;
		}
		resetAllState();
		if (newVal && newVal.trim()) {
			parseJsonToForm(newVal);
		}
	},
	{ immediate: true }
);

watch(
	() => props.connectionType,
	(_newVal, oldVal) => {
		if (oldVal !== undefined && !rawInvalidJson.value) {
			const json = serializeFormToJson();
			if (json !== lastEmittedJson) {
				lastEmittedJson = json;
				emit('update:modelValue', json);
			}
		}
	}
);

const preservedParamsCount = computed(() => {
	let count = 0;
	if (formState.encryption.encryptor && formState.encryption.encryptor !== 'None') count++;
	if (formState.encryption.encryptionKeyRef?.trim()) count++;
	if (formState.encryption.inboundEncryptor && formState.encryption.inboundEncryptor !== 'None') count++;
	if (formState.encryption.decryptionKeyRef?.trim()) count++;
	if (extraParamsJson.value && extraParamsJson.value.trim()) {
		try {
			const parsed = JSON.parse(extraParamsJson.value.trim());
			if (typeof parsed === 'object' && parsed !== null && !Array.isArray(parsed)) {
				count += Object.keys(parsed).length;
			}
		} catch {
			// ignore
		}
	}
	return count;
});

const hasErrors = (): boolean => {
	return Boolean(rawInvalidJson.value);
};

defineExpose({
	hasErrors,
	preservedParamsCount,
});
</script>

<style lang="scss">
/* Tooltips render in <body>: styled here, not in the scoped block */
.el-popper.connection-config-tooltip {
	box-sizing: border-box;
	width: max-content;
	max-width: 28vw;
	line-height: 1.5;
	white-space: pre-line;
	overflow-wrap: anywhere;
}
</style>

<style scoped lang="scss">
.connection-config-form {
	width: 100%;

	.section-title {
		font-size: 13px;
		font-weight: 600;
		color: var(--el-text-color-primary);
		border-left: 3px solid var(--el-color-primary);
		padding-left: 8px;
		margin: 12px 0 10px 0;
	}

	.channel-fields {
		width: 100%;
	}

	.custom-collapse {
		border-top: none;
		:deep(.el-collapse-item__header) {
			font-size: 13px;
			font-weight: 500;
			color: var(--el-text-color-regular);
			height: 36px;
			line-height: 36px;
		}
		:deep(.el-collapse-item__content) {
			padding-bottom: 8px;
		}
	}

	.collapse-title-row {
		display: flex;
		align-items: center;
		gap: 8px;
		width: 100%;
	}

	.collapse-summary {
		font-size: 12px;
		font-weight: 400;
		color: var(--el-text-color-secondary);
	}

	.invalid-json-content {
		font-size: 12px;
		.raw-json-block {
			display: block;
			background: var(--el-fill-color-light);
			padding: 6px 10px;
			border-radius: 4px;
			word-break: break-all;
			font-family: monospace;
		}
	}

	.cursor-pointer {
		cursor: pointer;
	}

	.warning-text {
		color: var(--el-color-warning);
		font-size: 11px;
		line-height: 14px;
		margin-top: 2px;
	}

	.text-muted {
		color: var(--el-text-color-secondary);
		font-size: 12px;
	}

	.mb12 {
		margin-bottom: 12px;
	}
	.mt5 {
		margin-top: 5px;
	}
	.w100 {
		width: 100%;
	}

	.collapse-item-desc {
		font-size: 12px;
		line-height: 1.5;
	}

	.preserved-params-hint {
		padding: 6px 10px;
		background: var(--el-fill-color-light);
		border-radius: 4px;
		display: flex;
		align-items: center;
		gap: 6px;
		font-size: 12px;
		line-height: 1.4;
	}

	.mr4 {
		margin-right: 4px;
	}

	.mt12 {
		margin-top: 12px;
	}

	.label-help-icon {
		margin-left: 4px;
		vertical-align: middle;
		cursor: help;
		color: var(--el-text-color-secondary);
	}
}
</style>
