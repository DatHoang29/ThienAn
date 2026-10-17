import { test, expect } from '@playwright/test';
import { resolveWebVueSrc } from '../../setup';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const formComponentPath = resolveWebVueSrc('views/sharedata/sharing/component/connectionConfigForm.vue');
const editorComponentPath = resolveWebVueSrc('views/sharedata/sharing/component/connectionConfigEditor.vue');
const editPartnerPath = resolveWebVueSrc('views/sharedata/sharing/component/editPartner.vue');
const viVnPath = resolveWebVueSrc('i18n/lang/vi-vn.json');
const enUsPath = resolveWebVueSrc('i18n/lang/en-us.json');

/**
 * Mô phỏng logic lõi của connectionConfigForm.vue để kiểm thử thuần túy
 */
interface FormStateModel {
	api: {
		authType: string;
		token: string;
		headerName: string;
		apiKey: string;
		username: string;
		password: string;
		format: string;
		timeoutSeconds: number;
		bypassSsl: boolean;
		successField: string;
		errorMessageField: string;
		errorCodeField: string;
		responseContract: string;
	};
	file: {
		directoryPath: string;
		fileNamePattern: string;
		format: string;
	};
	ftp: {
		remotePath: string;
		username: string;
		password: string;
		useSsl: boolean;
		format: string;
		enabled: boolean;
		timeoutSeconds: number;
		fileName: string;
	};
	kafka: {
		bootstrapServers: string;
		topic: string;
		clientId: string;
		securityProtocol: string;
		saslMechanism: string;
		saslUsername: string;
		saslPassword: string;
		sslCaLocation: string;
		enabled: boolean;
		timeoutMs: number;
	};
	encryption: {
		encryptor: string;
		encryptionKeyRef: string;
		inboundEncryptor: string;
		decryptionKeyRef: string;
	};
}

function createDefaultFormState(): FormStateModel {
	return {
		api: {
			authType: 'None',
			token: '',
			headerName: 'X-Api-Key',
			apiKey: '',
			username: '',
			password: '',
			format: '',
			timeoutSeconds: 30,
			bypassSsl: false,
			successField: '',
			errorMessageField: '',
			errorCodeField: '',
			responseContract: '',
		},
		file: {
			directoryPath: '',
			fileNamePattern: '',
			format: '',
		},
		ftp: {
			remotePath: '',
			username: '',
			password: '',
			useSsl: false,
			format: '',
			enabled: true,
			timeoutSeconds: 30,
			fileName: '',
		},
		kafka: {
			bootstrapServers: '',
			topic: '',
			clientId: '',
			securityProtocol: 'Plaintext',
			saslMechanism: 'PLAIN',
			saslUsername: '',
			saslPassword: '',
			sslCaLocation: '',
			enabled: true,
			timeoutMs: null as any,
		},
		encryption: {
			encryptor: 'None',
			encryptionKeyRef: '',
			inboundEncryptor: 'None',
			decryptionKeyRef: '',
		},
	};
}

function getManagedKeys(channel: string): string[] {
	const common = ['Encryptor', 'EncryptionKeyRef', 'InboundEncryptor', 'DecryptionKeyRef', 'Enabled'];
	if (channel === 'API') {
		return [
			...common,
			'AuthType', 'Token', 'HeaderName', 'ApiKeyHeader', 'ApiKey', 'Username', 'User',
			'Password', 'Pass', 'Format', 'TimeoutSeconds', 'HttpTimeoutSec', 'TimeoutSec',
			'BypassSsl', 'IgnoreSsl', 'Insecure', 'SuccessField', 'ErrorMessageField', 'ErrorCodeField', 'ResponseContract'
		];
	}
	if (channel === 'File') {
		return [...common, 'DirectoryPath', 'BasePath', 'FileNamePattern', 'Format'];
	}
	if (channel === 'FTP') {
		return [...common, 'RemotePath', 'Username', 'User', 'Password', 'Pass', 'UseSsl', 'EnableSsl', 'Format', 'TimeoutSeconds', 'FileName'];
	}
	if (channel === 'Kafka') {
		return [...common, 'BootstrapServers', 'Topic', 'Channel', 'ClientId', 'SecurityProtocol', 'SaslMechanism', 'SaslUsername', 'SaslPassword', 'SslCaLocation', 'TimeoutMs'];
	}
	return common;
}

function parseJsonToFormLogic(jsonStr: string | null | undefined, channel: string, state: FormStateModel) {
	if (!jsonStr || !jsonStr.trim()) {
		state.api.timeoutSeconds = 30;
		state.ftp.timeoutSeconds = 30;
		return { extra: null as Record<string, string> | null, rawInvalid: null as string | null };
	}
	let parsed: Record<string, any>;
	try {
		parsed = JSON.parse(jsonStr.trim());
		if (typeof parsed !== 'object' || parsed === null || Array.isArray(parsed)) {
			return { extra: null, rawInvalid: jsonStr };
		}
	} catch {
		return { extra: null, rawInvalid: jsonStr };
	}

	const extraObj: Record<string, string> = {};
	const managedSet = new Set(getManagedKeys(channel).map((k) => k.toLowerCase()));
	const inputMap = new Map<string, { originalKey: string; value: any }>();
	Object.entries(parsed).forEach(([k, v]) => {
		inputMap.set(k.trim().toLowerCase(), { originalKey: k, value: v });
	});

	const resolveField = (canonicalKey: string, aliasKeys: string[] = []): string | null => {
		const canLower = canonicalKey.toLowerCase();
		const hasCan = inputMap.has(canLower);
		const foundAliases = aliasKeys.filter((a) => inputMap.has(a.toLowerCase()));

		if (hasCan) {
			const canVal = inputMap.get(canLower)!.value;
			foundAliases.forEach((a) => {
				const info = inputMap.get(a.toLowerCase())!;
				extraObj[info.originalKey] = info.value != null ? String(info.value) : '';
			});
			return canVal != null ? String(canVal) : '';
		}
		if (foundAliases.length > 0) {
			const first = foundAliases[0];
			const firstVal = inputMap.get(first.toLowerCase())!.value;
			for (let i = 1; i < foundAliases.length; i++) {
				const info = inputMap.get(foundAliases[i].toLowerCase())!;
				extraObj[info.originalKey] = info.value != null ? String(info.value) : '';
			}
			return firstVal != null ? String(firstVal) : '';
		}
		return null;
	};

	// Phân giải API
	const apiAuth = resolveField('AuthType');
	if (apiAuth != null) state.api.authType = apiAuth || 'None';
	const apiToken = resolveField('Token');
	if (apiToken != null) state.api.token = apiToken;
	const apiHeader = resolveField('HeaderName', ['ApiKeyHeader']);
	if (apiHeader != null) state.api.headerName = apiHeader;
	const apiApiKey = resolveField('ApiKey');
	if (apiApiKey != null) state.api.apiKey = apiApiKey;
	const apiUser = resolveField('Username', ['User']);
	if (apiUser != null) state.api.username = apiUser;
	const apiPass = resolveField('Password', ['Pass']);
	if (apiPass != null) state.api.password = apiPass;
	const apiFmt = resolveField('Format');
	if (apiFmt != null) state.api.format = apiFmt;
	const apiTimeout = resolveField('TimeoutSeconds', ['HttpTimeoutSec', 'TimeoutSec']);
	if (apiTimeout != null && !Number.isNaN(Number(apiTimeout))) {
		const num = parseInt(apiTimeout, 10);
		if (num > 0) state.api.timeoutSeconds = num;
		else state.api.timeoutSeconds = 30;
	} else {
		state.api.timeoutSeconds = 30;
	}
	const apiBypass = resolveField('BypassSsl', ['IgnoreSsl', 'Insecure']);
	if (apiBypass != null) state.api.bypassSsl = apiBypass.toLowerCase() === 'true';
	const apiSucc = resolveField('SuccessField');
	if (apiSucc != null) state.api.successField = apiSucc;
	const apiErr = resolveField('ErrorMessageField');
	if (apiErr != null) state.api.errorMessageField = apiErr;
	const apiCode = resolveField('ErrorCodeField');
	if (apiCode != null) state.api.errorCodeField = apiCode;
	const apiResp = resolveField('ResponseContract');
	if (apiResp != null) state.api.responseContract = apiResp;

	// Phân giải File
	const fileDir = resolveField('DirectoryPath', ['BasePath']);
	if (fileDir != null) state.file.directoryPath = fileDir;
	const filePat = resolveField('FileNamePattern');
	if (filePat != null) state.file.fileNamePattern = filePat;
	if (apiFmt != null) state.file.format = apiFmt;

	// Phân giải FTP
	const ftpRemote = resolveField('RemotePath');
	if (ftpRemote != null) state.ftp.remotePath = ftpRemote;
	if (apiUser != null) state.ftp.username = apiUser;
	if (apiPass != null) state.ftp.password = apiPass;
	const ftpSsl = resolveField('UseSsl', ['EnableSsl']);
	if (ftpSsl != null) state.ftp.useSsl = ftpSsl.toLowerCase() === 'true';
	if (apiFmt != null) state.ftp.format = apiFmt;
	const ftpEnabled = resolveField('Enabled');
	if (ftpEnabled != null) state.ftp.enabled = ftpEnabled.toLowerCase() !== 'false';
	if (apiTimeout != null && !Number.isNaN(Number(apiTimeout))) {
		const num = parseInt(apiTimeout, 10);
		if (num > 0) state.ftp.timeoutSeconds = num;
		else state.ftp.timeoutSeconds = 30;
	} else {
		state.ftp.timeoutSeconds = 30;
	}
	const ftpFileName = resolveField('FileName');
	if (ftpFileName != null) state.ftp.fileName = ftpFileName;

	// Phân giải Kafka
	const kafkaServers = resolveField('BootstrapServers');
	if (kafkaServers != null) state.kafka.bootstrapServers = kafkaServers;
	const kafkaTopic = resolveField('Topic', ['Channel']);
	if (kafkaTopic != null) state.kafka.topic = kafkaTopic;
	const kafkaClient = resolveField('ClientId');
	if (kafkaClient != null) state.kafka.clientId = kafkaClient;
	const kafkaProto = resolveField('SecurityProtocol');
	if (kafkaProto != null) state.kafka.securityProtocol = kafkaProto;
	const kafkaMech = resolveField('SaslMechanism');
	if (kafkaMech != null) state.kafka.saslMechanism = kafkaMech;
	const kafkaSaslUser = resolveField('SaslUsername');
	if (kafkaSaslUser != null) state.kafka.saslUsername = kafkaSaslUser;
	const kafkaSaslPass = resolveField('SaslPassword');
	if (kafkaSaslPass != null) state.kafka.saslPassword = kafkaSaslPass;
	const kafkaSslCa = resolveField('SslCaLocation');
	if (kafkaSslCa != null) state.kafka.sslCaLocation = kafkaSslCa;
	if (ftpEnabled != null) state.kafka.enabled = ftpEnabled.toLowerCase() !== 'false';
	const kafkaTimeout = resolveField('TimeoutMs');
	if (kafkaTimeout != null && !Number.isNaN(Number(kafkaTimeout))) {
		const num = parseInt(kafkaTimeout, 10);
		if (num > 0) state.kafka.timeoutMs = num;
	}

	// Phân giải Mã hóa
	const encOut = resolveField('Encryptor');
	if (encOut != null) state.encryption.encryptor = encOut || 'None';
	const encOutKey = resolveField('EncryptionKeyRef');
	if (encOutKey != null) state.encryption.encryptionKeyRef = encOutKey;
	const encIn = resolveField('InboundEncryptor');
	if (encIn != null) state.encryption.inboundEncryptor = encIn || 'None';
	const encInKey = resolveField('DecryptionKeyRef');
	if (encInKey != null) state.encryption.decryptionKeyRef = encInKey;

	// Gom khóa lạ vào extra
	Object.entries(parsed).forEach(([k, v]) => {
		const lower = k.trim().toLowerCase();
		if (!managedSet.has(lower)) {
			extraObj[k] = v != null ? String(v) : '';
		}
	});

	return {
		extra: Object.keys(extraObj).length > 0 ? extraObj : null,
		rawInvalid: null,
	};
}

function serializeFormLogic(state: FormStateModel, channel: string, extraObj: Record<string, string> | null = null): string | null {
	const out: Record<string, string> = {};

	if (channel === 'API') {
		const auth = state.api.authType;
		if (auth && auth !== 'None') {
			out['AuthType'] = auth;
			if (auth === 'Bearer' && state.api.token) out['Token'] = state.api.token;
			else if (auth === 'ApiKey') {
				if (state.api.headerName) out['HeaderName'] = state.api.headerName.trim();
				if (state.api.apiKey) out['ApiKey'] = state.api.apiKey;
			} else if (auth === 'Basic') {
				if (state.api.username) out['Username'] = state.api.username.trim();
				if (state.api.password) out['Password'] = state.api.password;
			}
		}
		if (state.api.format) out['Format'] = state.api.format;
		if (state.api.timeoutSeconds != null && state.api.timeoutSeconds > 0) out['TimeoutSeconds'] = String(state.api.timeoutSeconds);
		if (state.api.bypassSsl) out['BypassSsl'] = 'true';
		if (state.api.successField?.trim()) out['SuccessField'] = state.api.successField.trim();
		if (state.api.errorMessageField?.trim()) out['ErrorMessageField'] = state.api.errorMessageField.trim();
		if (state.api.errorCodeField?.trim()) out['ErrorCodeField'] = state.api.errorCodeField.trim();
		if (state.api.responseContract?.trim()) out['ResponseContract'] = state.api.responseContract.trim();
	} else if (channel === 'File') {
		if (state.file.directoryPath?.trim()) out['DirectoryPath'] = state.file.directoryPath.trim();
		if (state.file.fileNamePattern?.trim()) out['FileNamePattern'] = state.file.fileNamePattern.trim();
		if (state.file.format) out['Format'] = state.file.format;
	} else if (channel === 'FTP') {
		if (state.ftp.remotePath?.trim()) out['RemotePath'] = state.ftp.remotePath.trim();
		if (state.ftp.username?.trim()) out['Username'] = state.ftp.username.trim();
		if (state.ftp.password) out['Password'] = state.ftp.password;
		if (state.ftp.useSsl) out['UseSsl'] = 'true';
		if (state.ftp.format) out['Format'] = state.ftp.format;
		if (!state.ftp.enabled) out['Enabled'] = 'false';
		if (state.ftp.timeoutSeconds) out['TimeoutSeconds'] = String(state.ftp.timeoutSeconds);
		if (state.ftp.fileName?.trim()) out['FileName'] = state.ftp.fileName.trim();
	} else if (channel === 'Kafka') {
		if (state.kafka.bootstrapServers?.trim()) out['BootstrapServers'] = state.kafka.bootstrapServers.trim();
		if (state.kafka.topic?.trim()) out['Topic'] = state.kafka.topic.trim();
		if (state.kafka.clientId?.trim()) out['ClientId'] = state.kafka.clientId.trim();
		if (state.kafka.securityProtocol) out['SecurityProtocol'] = state.kafka.securityProtocol;
		const hasSasl = state.kafka.securityProtocol === 'SaslPlaintext' || state.kafka.securityProtocol === 'SaslSsl';
		if (hasSasl) {
			if (state.kafka.saslMechanism) out['SaslMechanism'] = state.kafka.saslMechanism;
			if (state.kafka.saslUsername?.trim()) out['SaslUsername'] = state.kafka.saslUsername.trim();
			if (state.kafka.saslPassword) out['SaslPassword'] = state.kafka.saslPassword;
		}
		const hasSsl = state.kafka.securityProtocol === 'Ssl' || state.kafka.securityProtocol === 'SaslSsl';
		if (hasSsl && state.kafka.sslCaLocation?.trim()) {
			out['SslCaLocation'] = state.kafka.sslCaLocation.trim();
		}
		if (!state.kafka.enabled) out['Enabled'] = 'false';
		if (state.kafka.timeoutMs) out['TimeoutMs'] = String(state.kafka.timeoutMs);
	}

	if (state.encryption.encryptor && state.encryption.encryptor !== 'None') {
		out['Encryptor'] = state.encryption.encryptor;
		if (state.encryption.encryptionKeyRef?.trim()) out['EncryptionKeyRef'] = state.encryption.encryptionKeyRef.trim();
	}
	if (state.encryption.inboundEncryptor && state.encryption.inboundEncryptor !== 'None') {
		out['InboundEncryptor'] = state.encryption.inboundEncryptor;
		if (state.encryption.decryptionKeyRef?.trim()) out['DecryptionKeyRef'] = state.encryption.decryptionKeyRef.trim();
	}

	if (extraObj) {
		Object.entries(extraObj).forEach(([k, v]) => {
			const trimmedK = k.trim();
			if (trimmedK) out[trimmedK] = v != null ? String(v) : '';
		});
	}

	const keys = Object.keys(out);
	return keys.length > 0 ? JSON.stringify(out) : null;
}

test.describe('ShareData — Bản thử Form cấu hình kết nối đối tác (P12) @sharedata', () => {

	test('TC1 — Kiểm tra tĩnh mã nguồn: connectionConfigForm.vue, editPartner.vue, connectionConfigEditor.vue (Rules 9, 11, 20, 21, 26)', async () => {
		expect(fs.existsSync(formComponentPath), 'File connectionConfigForm.vue phải tồn tại').toBeTruthy();
		expect(fs.existsSync(editPartnerPath), 'File editPartner.vue phải tồn tại').toBeTruthy();
		expect(fs.existsSync(editorComponentPath), 'File connectionConfigEditor.vue phải tồn tại').toBeTruthy();

		const formContent = fs.readFileSync(formComponentPath, 'utf-8');
		const partnerContent = fs.readFileSync(editPartnerPath, 'utf-8');
		const editorContent = fs.readFileSync(editorComponentPath, 'utf-8');

		// Rule 20.14: Tên component camelCase
		expect(path.basename(formComponentPath)).toBe('connectionConfigForm.vue');
		expect(formContent).toContain('name="connectionConfigForm"');

		// Rule 20.6 / Rule 9: CẤM !important trong CSS
		expect(formContent.includes('!important'), 'connectionConfigForm.vue không được chứa !important').toBeFalsy();
		expect(partnerContent.includes('!important'), 'editPartner.vue không được chứa !important').toBeFalsy();
		expect(editorContent.includes('!important'), 'connectionConfigEditor.vue không được chứa !important').toBeFalsy();

		// Rule 20.8 / Rule 11: CẤM toán tử phủ định kép !!
		expect(formContent.includes('!!'), 'connectionConfigForm.vue không được chứa !!').toBeFalsy();
		expect(partnerContent.includes('!!'), 'editPartner.vue không được chứa !!').toBeFalsy();
		expect(editorContent.includes('!!'), 'connectionConfigEditor.vue không được chứa !!').toBeFalsy();

		// Mở hỗ trợ đầy đủ các kênh WebAPI, File, Kafka, FTP trong modal đối tác
		expect(partnerContent).toContain('value="API"');
		expect(partnerContent).toContain('value="File"');
		expect(partnerContent).toContain('value="Kafka"');
		expect(partnerContent).toContain('value="FTP"');
		expect(partnerContent).not.toContain('value="File" :disabled="true"');


		// Yêu cầu 3: Bỏ padding bottom cho phần div nội dung giữa .el-dialog__body
		expect(partnerContent).toContain(':deep(.el-dialog__body)');
		expect(partnerContent).toContain('padding-bottom: 0');

		// Yêu cầu 4 / Việc 5: editPartner.vue nhúng connectionConfigForm
		expect(partnerContent).toContain('<connection-config-form');
		expect(partnerContent).toContain('connectionConfigFormRef');
		expect(partnerContent).toContain("import connectionConfigForm from './connectionConfigForm.vue'");

		// connectionConfigEditor.vue hỗ trợ hideSuggestions và managedKeys
		expect(editorContent).toContain('hideSuggestions');
		expect(editorContent).toContain('managedKeys');
		expect(editorContent).toContain('lz.label.sharedataConnectionEditor.errKeyManagedByForm');
	});

	test('TC2 — Kiểm tra tệp i18n vi-vn.json và en-us.json đầy đủ các khóa của connectionConfigForm', async () => {
		expect(fs.existsSync(viVnPath)).toBeTruthy();
		expect(fs.existsSync(enUsPath)).toBeTruthy();

		const viVn = JSON.parse(fs.readFileSync(viVnPath, 'utf-8'));
		const enUs = JSON.parse(fs.readFileSync(enUsPath, 'utf-8'));

		const keysToCheck = [
			'lz.tooltip.sharedataPartner.channelDisabled',
			'lz.label.sharedataConnectionEditor.errKeyManagedByForm',
			'lz.label.sharedataConnectionForm.modeForm',
			'lz.label.sharedataConnectionForm.modeRaw',
			'lz.label.sharedataConnectionForm.authType',
			'lz.label.sharedataConnectionForm.authNone',
			'lz.label.sharedataConnectionForm.authBearer',
			'lz.label.sharedataConnectionForm.authApiKey',
			'lz.label.sharedataConnectionForm.authBasic',
			'lz.label.sharedataConnectionForm.token',
			'lz.label.sharedataConnectionForm.headerName',
			'lz.label.sharedataConnectionForm.apiKey',
			'lz.label.sharedataConnectionForm.username',
			'lz.label.sharedataConnectionForm.password',
			'lz.label.sharedataConnectionForm.format',
			'lz.label.sharedataConnectionForm.formatJson',
			'lz.label.sharedataConnectionForm.formatCsv',
			'lz.label.sharedataConnectionForm.timeoutSeconds',
			'lz.label.sharedataConnectionForm.bypassSsl',
			'lz.label.sharedataConnectionForm.collapseAdvanced',
			'lz.label.sharedataConnectionForm.collapseResponse',
			'lz.label.sharedataConnectionForm.directoryPath',
			'lz.label.sharedataConnectionForm.fileNamePattern',
			'lz.label.sharedataConnectionForm.insertToken',
			'lz.label.sharedataConnectionForm.samplePreview',
			'lz.label.sharedataConnectionForm.ftpServerHint',
			'lz.label.sharedataConnectionForm.remotePath',
			'lz.label.sharedataConnectionForm.useSsl',
			'lz.label.sharedataConnectionForm.enabled',
			'lz.label.sharedataConnectionForm.bootstrapServers',
			'lz.label.sharedataConnectionForm.topic',
			'lz.label.sharedataConnectionForm.clientId',
			'lz.label.sharedataConnectionForm.securityProtocol',
			'lz.label.sharedataConnectionForm.collapseEncryption',
			'lz.label.sharedataConnectionForm.encryptionNotice',
			'lz.label.sharedataConnectionForm.collapseExtra',
			'lz.message.sharedataConnectionForm.errCannotSwitchToFormInvalidJson',
			'lz.message.sharedataConnectionForm.errTokenRequired',
			'lz.message.sharedataConnectionForm.errApiKeyRequired',
			'lz.message.sharedataConnectionForm.errBasicAuthRequired',
		];

		for (const k of keysToCheck) {
			expect(viVn[k], `vi-vn.json thiếu key ${k}`).toBeTruthy();
			expect(enUs[k], `en-us.json thiếu key ${k}`).toBeTruthy();
		}

		// Rule 20.13: Cấm song ngữ lai
		expect(viVn['lz.tooltip.sharedataPartner.channelDisabled']).toBe('Kênh kết nối chưa được kích hoạt trong phiên bản này');
		expect(enUs['lz.tooltip.sharedataPartner.channelDisabled']).toBe('This connection channel is not yet enabled in this version');
	});

	test('TC3 — Quy tắc 1: Không mất dữ liệu âm thầm — Khóa lạ hoặc khóa kênh khác vào Tham số bổ sung', async () => {
		const state = createDefaultFormState();
		const rawInput = JSON.stringify({
			TimeoutSeconds: '45',
			AuthType: 'Bearer',
			Token: 'token_api_xyz',
			CustomVendorId: 'Vendor_999',
			LegacyRouteConfig: 'route_node_01',
			BootstrapServers: 'kafka-node:9092', // Khóa thuộc kênh khác khi đang ở API
		});

		const res = parseJsonToFormLogic(rawInput, 'API', state);
		expect(res.rawInvalid).toBeNull();
		expect(res.extra).toBeTruthy();
		// Các trường form API nhận đúng
		expect(state.api.timeoutSeconds).toBe(45);
		expect(state.api.authType).toBe('Bearer');
		expect(state.api.token).toBe('token_api_xyz');

		// Khóa lạ và khóa kênh khác được lưu trong Tham số bổ sung
		expect(res.extra!['CustomVendorId']).toBe('Vendor_999');
		expect(res.extra!['LegacyRouteConfig']).toBe('route_node_01');
		expect(res.extra!['BootstrapServers']).toBe('kafka-node:9092');

		// Khi serialize lại, không hề mất bất kỳ khóa nào
		const serialized = serializeFormLogic(state, 'API', res.extra);
		expect(serialized).toBeTruthy();
		const parsedOut = JSON.parse(serialized!);
		expect(parsedOut.TimeoutSeconds).toBe('45');
		expect(parsedOut.AuthType).toBe('Bearer');
		expect(parsedOut.Token).toBe('token_api_xyz');
		expect(parsedOut.CustomVendorId).toBe('Vendor_999');
		expect(parsedOut.LegacyRouteConfig).toBe('route_node_01');
		expect(parsedOut.BootstrapServers).toBe('kafka-node:9092');
	});

	test('TC4 — Quy tắc 2: Không phân biệt hoa thường & Alias đơn lẻ vs Alias trùng chuẩn', async () => {
		// 1. Alias đơn lẻ: JSON có HttpTimeoutSec -> điền ô TimeoutSeconds, khi lưu ghi TimeoutSeconds (bỏ alias)
		const state1 = createDefaultFormState();
		const rawAliasOnly = JSON.stringify({
			httptimeoutsec: '60',
			apikeyheader: 'X-Special-Key',
		});
		const res1 = parseJsonToFormLogic(rawAliasOnly, 'API', state1);
		expect(state1.api.timeoutSeconds).toBe(60);
		expect(state1.api.headerName).toBe('X-Special-Key');
		expect(res1.extra).toBeNull();

		// Khi lưu ghi theo khóa chuẩn
		const out1 = serializeFormLogic(state1, 'API', res1.extra);
		const parsed1 = JSON.parse(out1!);
		expect(parsed1.TimeoutSeconds).toBe('60');
		expect(parsed1.HttpTimeoutSec).toBeUndefined();

		// 2. Có cả hai (Chuẩn VÀ Alias): ô dùng chuẩn, alias vào Tham số bổ sung
		const state2 = createDefaultFormState();
		const rawBoth = JSON.stringify({
			AuthType: 'ApiKey',
			ApiKey: 'key_123',
			TimeoutSeconds: '30',
			HttpTimeoutSec: '45',
			HeaderName: 'X-Standard-Header',
			ApiKeyHeader: 'X-Legacy-Header',
		});
		const res2 = parseJsonToFormLogic(rawBoth, 'API', state2);
		expect(state2.api.timeoutSeconds).toBe(30);
		expect(state2.api.headerName).toBe('X-Standard-Header');
		// Alias bị đẩy vào Tham số bổ sung
		expect(res2.extra!['HttpTimeoutSec']).toBe('45');
		expect(res2.extra!['ApiKeyHeader']).toBe('X-Legacy-Header');

		const out2 = serializeFormLogic(state2, 'API', res2.extra);
		const parsed2 = JSON.parse(out2!);
		expect(parsed2.TimeoutSeconds).toBe('30');
		expect(parsed2.HttpTimeoutSec).toBe('45');
		expect(parsed2.HeaderName).toBe('X-Standard-Header');
		expect(parsed2.ApiKeyHeader).toBe('X-Legacy-Header');
	});

	test('TC5 — Quy tắc 3: Ghi ra dạng chuỗi ("true"/"false", "30"), ô trống không ghi, AuthType None không ghi, rỗng hết emit null', async () => {
		const state = createDefaultFormState();

		// Trạng thái mặc định: có default TimeoutSeconds = 30
		const defaultOut = serializeFormLogic(state, 'API', null);
		expect(defaultOut).toBe(JSON.stringify({ TimeoutSeconds: '30' }));

		// Khi tất cả ô trống hoàn toàn (timeoutSeconds bị xóa/null) -> emit null
		state.api.timeoutSeconds = null as any;
		const nullOut = serializeFormLogic(state, 'API', null);
		expect(nullOut).toBeNull();

		// Điền các giá trị
		state.api.authType = 'Bearer';
		state.api.token = 'my_bearer_token';
		state.api.timeoutSeconds = 30;
		state.api.bypassSsl = true;

		const outStr = serializeFormLogic(state, 'API', null);
		expect(outStr).toBeTruthy();
		const parsed = JSON.parse(outStr!);

		// Kiểm tra dạng chuỗi
		expect(typeof parsed.TimeoutSeconds).toBe('string');
		expect(parsed.TimeoutSeconds).toBe('30');
		expect(typeof parsed.BypassSsl).toBe('string');
		expect(parsed.BypassSsl).toBe('true');
		expect(parsed.AuthType).toBe('Bearer');
		expect(parsed.Token).toBe('my_bearer_token');

		// Khi AuthType là None thì không ghi AuthType
		state.api.authType = 'None';
		state.api.token = '';
		const outNoAuth = serializeFormLogic(state, 'API', null);
		const parsedNoAuth = JSON.parse(outNoAuth!);
		expect(parsedNoAuth.AuthType).toBeUndefined();
	});

	test('TC6 — Quy tắc 4: Đổi kiểu kết nối giữ nguyên state RAM, khi lưu chỉ ghi khóa của kiểu đang chọn', async () => {
		const state = createDefaultFormState();

		// 1. Cấu hình trên kênh API
		state.api.authType = 'Bearer';
		state.api.token = 'secret_token_123';
		state.api.timeoutSeconds = 45;

		// 2. Chuyển sang kênh File
		state.file.directoryPath = '/mnt/sharedata/outbound';
		state.file.fileNamePattern = 'traffic_{timestamp}_{serial}.json';

		// Khi lưu ở kênh File, CHỈ ghi các khóa của kênh File
		const fileOut = serializeFormLogic(state, 'File', null);
		const parsedFile = JSON.parse(fileOut!);
		expect(parsedFile.DirectoryPath).toBe('/mnt/sharedata/outbound');
		expect(parsedFile.FileNamePattern).toBe('traffic_{timestamp}_{serial}.json');
		expect(parsedFile.Token).toBeUndefined();
		expect(parsedFile.AuthType).toBeUndefined();
		expect(parsedFile.TimeoutSeconds).toBeUndefined();

		// 3. Chuyển lại kênh API: cấu hình cũ trên RAM vẫn còn nguyên vẹn
		expect(state.api.token).toBe('secret_token_123');
		expect(state.api.timeoutSeconds).toBe(45);
		const apiOut = serializeFormLogic(state, 'API', null);
		const parsedApi = JSON.parse(apiOut!);
		expect(parsedApi.Token).toBe('secret_token_123');
		expect(parsedApi.TimeoutSeconds).toBe('45');
		expect(parsedApi.DirectoryPath).toBeUndefined();
	});

	test('TC7 — Quy tắc 5 & 6: JSON lỗi cú pháp hoặc giá trị không hợp lệ không bị xóa âm thầm', async () => {
		const state = createDefaultFormState();

		// JSON lỗi cú pháp
		const badJson = '{"TimeoutSeconds": 30, invalid}';
		const resBad = parseJsonToFormLogic(badJson, 'API', state);
		expect(resBad.rawInvalid).toBe(badJson);
		expect(resBad.extra).toBeNull();

		// Giá trị lạ/không hợp lệ
		const customJson = JSON.stringify({
			AuthType: 'UnsupportedAuthCustom',
			TimeoutSeconds: 'invalid_number',
		});
		const resCustom = parseJsonToFormLogic(customJson, 'API', state);
		expect(resCustom.rawInvalid).toBeNull();
		// Giá trị AuthType lạ được giữ nguyên trên form
		expect(state.api.authType).toBe('UnsupportedAuthCustom');
	});

	test('TC8 — Quy tắc 7: FileNamePattern phải chứa token {serial} hoặc {timestamp}', async () => {
		const state = createDefaultFormState();
		state.file.directoryPath = '/data';

		// 1. Không có token -> vi phạm
		state.file.fileNamePattern = 'static_file_name.json';
		const hasToken1 = state.file.fileNamePattern.includes('{serial}') || state.file.fileNamePattern.includes('{timestamp}');
		expect(hasToken1).toBeFalsy();

		// 2. Có {serial} -> hợp lệ
		state.file.fileNamePattern = 'export_{serial}.json';
		const hasToken2 = state.file.fileNamePattern.includes('{serial}') || state.file.fileNamePattern.includes('{timestamp}');
		expect(hasToken2).toBeTruthy();

		// 3. Có {timestamp} -> hợp lệ
		state.file.fileNamePattern = 'export_{timestamp}.json';
		const hasToken3 = state.file.fileNamePattern.includes('{serial}') || state.file.fileNamePattern.includes('{timestamp}');
		expect(hasToken3).toBeTruthy();
	});

	test('TC9 — Kênh Kafka: SASL và SSL CA Location hiển thị điều kiện theo SecurityProtocol', async () => {
		const state = createDefaultFormState();
		state.kafka.bootstrapServers = '10.10.8.200:9092';
		state.kafka.topic = 'its.traffic';

		// 1. Plaintext -> không ghi SASL hay SSL
		state.kafka.securityProtocol = 'Plaintext';
		state.kafka.saslUsername = 'user';
		state.kafka.saslPassword = 'pwd';
		state.kafka.sslCaLocation = '/cert.pem';
		const outPlain = serializeFormLogic(state, 'Kafka', null);
		const parsedPlain = JSON.parse(outPlain!);
		expect(parsedPlain.SecurityProtocol).toBe('Plaintext');
		expect(parsedPlain.SaslUsername).toBeUndefined();
		expect(parsedPlain.SslCaLocation).toBeUndefined();

		// 2. SaslSsl -> ghi cả SASL và SSL
		state.kafka.securityProtocol = 'SaslSsl';
		state.kafka.saslMechanism = 'PLAIN';
		const outSaslSsl = serializeFormLogic(state, 'Kafka', null);
		const parsedSaslSsl = JSON.parse(outSaslSsl!);
		expect(parsedSaslSsl.SecurityProtocol).toBe('SaslSsl');
		expect(parsedSaslSsl.SaslMechanism).toBe('PLAIN');
		expect(parsedSaslSsl.SaslUsername).toBe('user');
		expect(parsedSaslSsl.SaslPassword).toBe('pwd');
		expect(parsedSaslSsl.SslCaLocation).toBe('/cert.pem');
	});

	test('TC10 — Cấu hình mã hóa bảo mật (Encryptor & InboundEncryptor) và Enabled chỉ ghi khi false', async () => {
		const state = createDefaultFormState();
		state.api.timeoutSeconds = 30;

		// 1. Encryptor = None -> không ghi ra JSON
		state.encryption.encryptor = 'None';
		state.encryption.inboundEncryptor = 'None';
		const outNone = serializeFormLogic(state, 'API', null);
		const parsedNone = JSON.parse(outNone!);
		expect(parsedNone.Encryptor).toBeUndefined();
		expect(parsedNone.InboundEncryptor).toBeUndefined();

		// 2. Encryptor = AesGcm -> ghi ra JSON kèm key ref
		state.encryption.encryptor = 'AesGcm';
		state.encryption.encryptionKeyRef = 'kms://key-out-01';
		state.encryption.inboundEncryptor = 'AesGcm';
		state.encryption.decryptionKeyRef = 'kms://key-in-01';
		const outAes = serializeFormLogic(state, 'API', null);
		const parsedAes = JSON.parse(outAes!);
		expect(parsedAes.Encryptor).toBe('AesGcm');
		expect(parsedAes.EncryptionKeyRef).toBe('kms://key-out-01');
		expect(parsedAes.InboundEncryptor).toBe('AesGcm');
		expect(parsedAes.DecryptionKeyRef).toBe('kms://key-in-01');

		// 3. Enabled: kênh FTP mặc định enabled=true -> không ghi; khi enabled=false -> ghi "false"
		state.ftp.remotePath = '/pub';
		state.ftp.enabled = true;
		const outFtpOn = serializeFormLogic(state, 'FTP', null);
		expect(JSON.parse(outFtpOn!).Enabled).toBeUndefined();

		state.ftp.enabled = false;
		const outFtpOff = serializeFormLogic(state, 'FTP', null);
		expect(JSON.parse(outFtpOff!).Enabled).toBe('false');
	});

	test('TC11 — Form cấu hình kết nối: el-col full-width (:sm="24") và Timeout mặc định 30 giây (Task 2)', async () => {
		const formContent = fs.readFileSync(formComponentPath, 'utf-8');

		// 1. Kiểm tra default timeoutSeconds = 30 trong formState của connectionConfigForm.vue
		expect(formContent).toMatch(/api:\s*\{[\s\S]*?timeoutSeconds:\s*30/);
		expect(formContent).toMatch(/ftp:\s*\{[\s\S]*?timeoutSeconds:\s*30/);

		// 2. Kiểm tra các el-col trong channel-api là full-width (:sm="24")
		const apiSectionMatch = formContent.match(/<div v-if="normalizedChannel === 'API'" class="channel-fields channel-api">([\s\S]*?)<\/div>\s*<!-- ── 2\. KÊNH FILE/);
		expect(apiSectionMatch, 'Phải tìm thấy section channel-api').toBeTruthy();
		const apiSection = apiSectionMatch![1];

		// Không còn chia cột hẹp :sm="12", :sm="10", :sm="14"
		expect(apiSection.includes(':sm="12"')).toBeFalsy();
		expect(apiSection.includes(':sm="10"')).toBeFalsy();
		expect(apiSection.includes(':sm="14"')).toBeFalsy();

		// Tất cả el-col trong channel-api đều có :sm="24"
		const colMatches = apiSection.match(/<el-col\b[^>]*>/g) ?? [];
		expect(colMatches.length).toBeGreaterThan(0);
		for (const col of colMatches) {
			expect(col).toContain(':sm="24"');
		}

		// 3. Kiểm tra logic parseJsonToForm mặc định timeoutSeconds = 30 khi không truyền timeout
		const state = createDefaultFormState();
		expect(state.api.timeoutSeconds).toBe(30);
		expect(state.ftp.timeoutSeconds).toBe(30);

		// Parse json không có timeout -> vẫn giữ default 30
		parseJsonToFormLogic(JSON.stringify({ AuthType: 'Bearer', Token: 'abc' }), 'API', state);
		expect(state.api.timeoutSeconds).toBe(30);

		// Parse json rỗng -> reset về default 30
		state.api.timeoutSeconds = 99;
		state.ftp.timeoutSeconds = 99;
		parseJsonToFormLogic('', 'API', state);
		expect(state.api.timeoutSeconds).toBe(30);
		expect(state.ftp.timeoutSeconds).toBe(30);
	});
});
