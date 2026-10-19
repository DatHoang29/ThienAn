import { test, expect } from '@playwright/test';
import { resolveWebVueSrc } from '../../setup';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const editorComponentPath = resolveWebVueSrc('views/sharedata/sharing/component/connectionConfigEditor.vue');
const formComponentPath = resolveWebVueSrc('views/sharedata/sharing/component/connectionConfigForm.vue');
const editPartnerPath = resolveWebVueSrc('views/sharedata/sharing/component/editPartner.vue');
const viVnPath = resolveWebVueSrc('i18n/lang/vi-vn.json');
const enUsPath = resolveWebVueSrc('i18n/lang/en-us.json');

/**
 * Mô phỏng logic lõi của connectionConfigEditor.vue để kiểm thử thuần túy
 */
interface ConfigRow {
	id: string;
	key: string;
	value: string;
	showSecret: boolean;
}

const COLUMN_RESERVED_KEYS = [
	'baseurl',
	'url',
	'host',
	'server',
	'address',
	'ip',
	'port',
	'endpoint',
	'path',
	'endpointapiurl',
];

const SUGGESTED_KEYS_BY_CHANNEL: Record<string, string[]> = {
	API: ['TimeoutSeconds', 'BypassSsl', 'AuthType', 'Token', 'HeaderName', 'ApiKey', 'Username', 'Password'],
	FILE: ['DirectoryPath', 'FileNamePattern', 'Format'],
	KAFKA: ['BootstrapServers', 'Topic', 'ClientId', 'SecurityProtocol', 'SaslMechanism', 'SaslUsername', 'SaslPassword'],
	FTP: ['RemotePath', 'Username', 'Password', 'UseSsl', 'Format', 'TimeoutSeconds'],
};

const COMMON_KEYS = ['Enabled', 'Encryptor', 'EncryptionKeyRef'];

let rowCounter = 0;
const genRowId = () => `row_${Date.now()}_${++rowCounter}`;

function parseJsonToList(jsonStr: string | null | undefined): ConfigRow[] {
	if (!jsonStr || !jsonStr.trim()) return [];
	try {
		const parsed = JSON.parse(jsonStr.trim());
		if (typeof parsed !== 'object' || parsed === null || Array.isArray(parsed)) return [];
		return Object.entries(parsed).map(([k, v]) => ({
			id: genRowId(),
			key: k,
			value: v != null ? String(v) : '',
			showSecret: false,
		}));
	} catch {
		return [];
	}
}

function serializeListToJson(list: ConfigRow[]): string | null {
	if (!list || list.length === 0) return null;
	const obj: Record<string, string> = {};
	let count = 0;
	list.forEach((row) => {
		const trimmedKey = row.key?.trim();
		if (trimmedKey) {
			obj[trimmedKey] = row.value ?? '';
			count++;
		}
	});
	return count > 0 ? JSON.stringify(obj) : null;
}

function getKeyError(items: ConfigRow[], index: number, t: (k: string) => string): string | null {
	const row = items[index];
	if (!row) return null;
	const keyTrimmed = row.key?.trim() ?? '';
	if (!keyTrimmed) {
		return t('lz.label.sharedataConnectionEditor.errKeyRequired');
	}
	const lower = keyTrimmed.toLowerCase();
	if (COLUMN_RESERVED_KEYS.includes(lower)) {
		return t('lz.label.sharedataConnectionEditor.errKeyReserved');
	}
	for (let i = 0; i < items.length; i++) {
		if (i !== index) {
			const otherKey = items[i].key?.trim().toLowerCase();
			if (otherKey === lower) {
				return t('lz.label.sharedataConnectionEditor.errKeyDuplicate');
			}
		}
	}
	return null;
}

function getKeyWarning(items: ConfigRow[], index: number, t: (k: string) => string): string | null {
	const row = items[index];
	if (!row) return null;
	const keyTrimmed = row.key?.trim() ?? '';
	if (!keyTrimmed) return null;
	const lower = keyTrimmed.toLowerCase();
	if (lower === 'outputpath') {
		return t('lz.label.sharedataConnectionEditor.errKeyOutputPath');
	}
	if (lower === 'filenamepattern') {
		const val = (row.value ?? '').toLowerCase();
		if (val && !val.includes('{serial}') && !val.includes('{timestamp}')) {
			return t('lz.label.sharedataConnectionEditor.errFileNamePatternMissingVariable');
		}
	}
	return null;
}

function addSuggestedKeys(items: ConfigRow[], connectionType: string): number {
	const channel = (connectionType || 'API').toUpperCase();
	const channelKeys = SUGGESTED_KEYS_BY_CHANNEL[channel] || SUGGESTED_KEYS_BY_CHANNEL.API;
	const allSuggestions = [...channelKeys, ...COMMON_KEYS];

	const existingKeys = new Set(items.map((i) => i.key.trim().toLowerCase()));
	let addedCount = 0;

	allSuggestions.forEach((sugKey) => {
		if (!existingKeys.has(sugKey.toLowerCase())) {
			let defaultValue = '';
			if (sugKey.toLowerCase() === 'filenamepattern') {
				defaultValue = '{packet}_{partner}_{timestamp}_{serial}';
			}
			items.push({
				id: genRowId(),
				key: sugKey,
				value: defaultValue,
				showSecret: false,
			});
			existingKeys.add(sugKey.toLowerCase());
			addedCount++;
		}
	});
	return addedCount;
}

const mockI18n = (key: string): string => {
	const dict: Record<string, string> = {
		'lz.label.sharedataConnectionEditor.errKeyRequired': 'Tên thuộc tính không được để trống',
		'lz.label.sharedataConnectionEditor.errKeyReserved': 'Địa chỉ và cổng nhập ở trường Địa chỉ/Cổng',
		'lz.label.sharedataConnectionEditor.errKeyDuplicate': 'Tên thuộc tính bị trùng lặp',
		'lz.label.sharedataConnectionEditor.errHasInvalidKeys': 'Có thuộc tính cấu hình không hợp lệ, vui lòng kiểm tra lại',
		'lz.label.sharedataConnectionEditor.errKeyOutputPath': 'Thuộc tính [OutputPath] đã bị bãi bỏ. Vui lòng sử dụng [DirectoryPath] và [FileNamePattern]',
		'lz.label.sharedataConnectionEditor.errFileNamePatternMissingVariable': 'FileNamePattern phải chứa ít nhất {serial} hoặc {timestamp} để tránh ghi đè tệp',
	};
	return dict[key] || key;
};

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
			headerName: '',
			apiKey: '',
			username: '',
			password: '',
			format: '',
			timeoutSeconds: null as any,
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
			timeoutSeconds: null as any,
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
	const common = ['Encryptor', 'EncryptionKeyRef', 'InboundEncryptor', 'DecryptionKeyRef'];
	if (channel === 'API') {
		return [
			...common,
			'AuthType', 'Token', 'HeaderName', 'ApiKeyHeader', 'ApiKey', 'Username',
			'Password', 'Format', 'TimeoutSeconds', 'HttpTimeoutSec', 'TimeoutSec',
			'BypassSsl', 'IgnoreSsl', 'Insecure', 'SuccessField', 'ErrorMessageField', 'ErrorCodeField', 'ResponseContract'
		];
	}
	if (channel === 'File') {
		return [...common, 'DirectoryPath', 'BasePath', 'FileNamePattern', 'Format'];
	}
	if (channel === 'FTP') {
		return [...common, 'Enabled', 'RemotePath', 'Username', 'User', 'Password', 'Pass', 'UseSsl', 'EnableSsl', 'Format', 'TimeoutSeconds', 'FileName'];
	}
	if (channel === 'Kafka') {
		return [...common, 'Enabled', 'BootstrapServers', 'Topic', 'Channel', 'ClientId', 'SecurityProtocol', 'SaslMechanism', 'SaslUsername', 'SaslPassword', 'SslCaLocation', 'TimeoutMs'];
	}
	return common;
}

function parseJsonToFormLogic(jsonStr: string | null | undefined, channel: string, state: FormStateModel) {
	if (!jsonStr || !jsonStr.trim()) {
		state.api.timeoutSeconds = null as any;
		state.ftp.timeoutSeconds = null as any;
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
	const apiUser = resolveField('Username');
	if (apiUser != null) state.api.username = apiUser;
	const apiPass = resolveField('Password');
	if (apiPass != null) state.api.password = apiPass;
	const apiFmt = resolveField('Format');
	if (apiFmt != null) state.api.format = apiFmt;
	const apiTimeout = resolveField('TimeoutSeconds', ['HttpTimeoutSec', 'TimeoutSec']);
	if (apiTimeout != null && !Number.isNaN(Number(apiTimeout))) {
		const num = parseInt(apiTimeout, 10);
		if (num > 0) state.api.timeoutSeconds = num;
		else state.api.timeoutSeconds = null as any;
	} else {
		state.api.timeoutSeconds = null as any;
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
	const ftpUser = resolveField('Username', ['User']);
	if (ftpUser != null) state.ftp.username = ftpUser;
	const ftpPass = resolveField('Password', ['Pass']);
	if (ftpPass != null) state.ftp.password = ftpPass;
	const ftpSsl = resolveField('UseSsl', ['EnableSsl']);
	if (ftpSsl != null) state.ftp.useSsl = ftpSsl.toLowerCase() === 'true';
	const ftpFmt = resolveField('Format');
	if (ftpFmt != null) state.ftp.format = ftpFmt;
	const ftpEnabled = resolveField('Enabled');
	if (ftpEnabled != null) state.ftp.enabled = ftpEnabled.toLowerCase() !== 'false';
	const ftpTimeout = resolveField('TimeoutSeconds');
	if (ftpTimeout != null && !Number.isNaN(Number(ftpTimeout))) {
		const num = parseInt(ftpTimeout, 10);
		if (num > 0) state.ftp.timeoutSeconds = num;
		else state.ftp.timeoutSeconds = null as any;
	} else {
		state.ftp.timeoutSeconds = null as any;
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
		}
		if (auth === 'Bearer') {
			if (state.api.token) out['Token'] = state.api.token;
		} else if (auth === 'ApiKey') {
			if (state.api.headerName?.trim()) out['HeaderName'] = state.api.headerName.trim();
			if (state.api.apiKey) out['ApiKey'] = state.api.apiKey;
		} else if (auth === 'Basic') {
			if (state.api.username?.trim()) out['Username'] = state.api.username.trim();
			if (state.api.password) out['Password'] = state.api.password;
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
		if (state.ftp.timeoutSeconds != null && state.ftp.timeoutSeconds > 0) out['TimeoutSeconds'] = String(state.ftp.timeoutSeconds);
		if (state.ftp.fileName?.trim()) out['FileName'] = state.ftp.fileName.trim();
	} else if (channel === 'Kafka') {
		if (state.kafka.bootstrapServers?.trim()) out['BootstrapServers'] = state.kafka.bootstrapServers.trim();
		if (state.kafka.topic?.trim()) out['Topic'] = state.kafka.topic.trim();
		if (state.kafka.clientId?.trim()) out['ClientId'] = state.kafka.clientId.trim();
		if (state.kafka.securityProtocol?.trim()) out['SecurityProtocol'] = state.kafka.securityProtocol.trim();
		if (state.kafka.saslMechanism?.trim()) out['SaslMechanism'] = state.kafka.saslMechanism.trim();
		if (state.kafka.saslUsername?.trim()) out['SaslUsername'] = state.kafka.saslUsername.trim();
		if (state.kafka.saslPassword) out['SaslPassword'] = state.kafka.saslPassword;
		if (state.kafka.sslCaLocation?.trim()) out['SslCaLocation'] = state.kafka.sslCaLocation.trim();
		if (!state.kafka.enabled) out['Enabled'] = 'false';
		if (state.kafka.timeoutMs != null && state.kafka.timeoutMs > 0) out['TimeoutMs'] = String(state.kafka.timeoutMs);
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

test.describe('ShareData — Cấu hình kết nối đối tác theo mẫu VMS (R11) @sharedata', () => {

	test('TC1 — Kiểm tra tĩnh mã nguồn connectionConfigEditor.vue và editPartner.vue (Rules 9, 11, 20, 21)', async () => {
		expect(fs.existsSync(editorComponentPath), 'File connectionConfigEditor.vue phải tồn tại').toBeTruthy();
		expect(fs.existsSync(editPartnerPath), 'File editPartner.vue phải tồn tại').toBeTruthy();

		const editorContent = fs.readFileSync(editorComponentPath, 'utf-8');
		const partnerContent = fs.readFileSync(editPartnerPath, 'utf-8');

		// Rule 20.14: Tên component camelCase
		expect(path.basename(editorComponentPath)).toBe('connectionConfigEditor.vue');
		expect(editorContent).toContain('name="connectionConfigEditor"');

		// Rule 20.6 / Rule 9: CẤM !important trong CSS
		expect(editorContent.includes('!important'), 'connectionConfigEditor.vue không được chứa !important').toBeFalsy();
		expect(partnerContent.includes('!important'), 'editPartner.vue không được chứa !important').toBeFalsy();

		// Rule 20.8 / Rule 11: CẤM toán tử phủ định kép !!
		expect(editorContent.includes('!!'), 'connectionConfigEditor.vue không được chứa !!').toBeFalsy();
		expect(partnerContent.includes('!!'), 'editPartner.vue không được chứa !!').toBeFalsy();

		// editPartner.vue phải nhúng connectionConfigForm và bỏ các biến/methods cũ
		expect(partnerContent).toContain('<connection-config-form');
		expect(partnerContent).not.toContain('configMode');
		expect(partnerContent).not.toContain('buildConnectionConfigJson');
		expect(partnerContent).not.toContain('parseConnectionConfigToJson');
		expect(partnerContent).not.toContain('rawJsonConfig');

		// Kiểm tra không hardcode string: tất cả label/button/placeholder đều dùng $t
		expect(editorContent).toContain("$t('lz.button.sharedataConnectionEditor.addConfig')");
		expect(editorContent).toContain("$t('lz.button.sharedataConnectionEditor.addSuggestedKeys')");
		expect(editorContent).toContain("$t('lz.placeholder.sharedataConnectionEditor.key')");
		expect(editorContent).toContain("$t('lz.placeholder.sharedataConnectionEditor.value')");
		expect(editorContent).toContain("$t('lz.label.sharedataConnectionEditor.noConfig')");
	});

	test('TC2 — Kiểm tra tệp i18n vi-vn.json và en-us.json đầy đủ các khóa cần thiết', async () => {
		expect(fs.existsSync(viVnPath)).toBeTruthy();
		expect(fs.existsSync(enUsPath)).toBeTruthy();

		const viVn = JSON.parse(fs.readFileSync(viVnPath, 'utf-8'));
		const enUs = JSON.parse(fs.readFileSync(enUsPath, 'utf-8'));

		const requiredKeys = [
			'lz.button.sharedataConnectionEditor.addConfig',
			'lz.button.sharedataConnectionEditor.addSuggestedKeys',
			'lz.placeholder.sharedataConnectionEditor.key',
			'lz.placeholder.sharedataConnectionEditor.value',
			'lz.label.sharedataConnectionEditor.noConfig',
			'lz.label.sharedataConnectionEditor.errKeyRequired',
			'lz.label.sharedataConnectionEditor.errKeyReserved',
			'lz.label.sharedataConnectionEditor.errKeyDuplicate',
			'lz.label.sharedataConnectionEditor.errHasInvalidKeys',
			'lz.label.sharedataPartner.typeKafka',
			'lz.label.sharedataPartner.typeFtp',
		];

		for (const key of requiredKeys) {
			expect(viVn[key], `vi-vn.json thiếu key ${key}`).toBeTruthy();
			expect(enUs[key], `en-us.json thiếu key ${key}`).toBeTruthy();
		}

		// Rule 20.13: Cấm nhãn UI song ngữ lai tiếng Anh trong ngoặc ở vi-vn
		expect(viVn['lz.button.sharedataConnectionEditor.addSuggestedKeys']).toBe('Thêm key gợi ý');
		expect(viVn['lz.label.sharedataConnectionEditor.errKeyReserved']).toBe('Địa chỉ và cổng nhập ở trường Địa chỉ/Cổng');
	});

	test('TC3 — Key lạ (custom/unusual keys) vẫn còn sau khi lưu rồi mở lại', async () => {
		const originalJson = JSON.stringify({
			CustomHeader: 'X-Special-Value',
			CustomTimeout: '99',
			OrganizationUnit: 'Division-4',
		});

		// Parse JSON thành danh sách items
		const items = parseJsonToList(originalJson);
		expect(items.length).toBe(3);
		expect(items.find((i) => i.key === 'CustomHeader')?.value).toBe('X-Special-Value');
		expect(items.find((i) => i.key === 'CustomTimeout')?.value).toBe('99');
		expect(items.find((i) => i.key === 'OrganizationUnit')?.value).toBe('Division-4');

		// Serialize ngược lại JSON để lưu
		const savedJson = serializeListToJson(items);
		expect(savedJson).toBeTruthy();

		// Mở lại từ savedJson
		const reloadedItems = parseJsonToList(savedJson);
		expect(reloadedItems.length).toBe(3);
		expect(reloadedItems.find((i) => i.key === 'CustomHeader')?.value).toBe('X-Special-Value');
		expect(reloadedItems.find((i) => i.key === 'CustomTimeout')?.value).toBe('99');
		expect(reloadedItems.find((i) => i.key === 'OrganizationUnit')?.value).toBe('Division-4');
	});

	test('TC4 — Mở đối tác A (có token) rồi mở đối tác B (không có token) thì B không mang token của A', async () => {
		// Giả lập mở Partner A với Bearer token
		const partnerAConfig = JSON.stringify({
			AuthType: 'Bearer',
			Token: 'token_secret_A_12345',
		});
		let currentItems = parseJsonToList(partnerAConfig);
		expect(currentItems.length).toBe(2);
		expect(currentItems.find((i) => i.key === 'Token')?.value).toBe('token_secret_A_12345');

		// Giả lập mở Partner B với config rỗng hoặc null
		const partnerBConfig: string | null = null;
		currentItems = parseJsonToList(partnerBConfig);
		expect(currentItems.length).toBe(0);
		expect(currentItems.find((i) => i.key === 'Token')).toBeUndefined();

		// Giả lập mở Partner C với config khác
		const partnerCConfig = JSON.stringify({
			DirectoryPath: '/mnt/storage/outbound',
		});
		currentItems = parseJsonToList(partnerCConfig);
		expect(currentItems.length).toBe(1);
		expect(currentItems[0].key).toBe('DirectoryPath');
		expect(currentItems.find((i) => i.key === 'Token')).toBeUndefined();
	});

	test('TC5 — Đối tác Kafka lưu xong vẫn đủ key và các thông số cần thiết', async () => {
		const items: ConfigRow[] = [];
		// Bấm nút thêm key gợi ý cho kênh Kafka
		const added = addSuggestedKeys(items, 'Kafka');
		expect(added).toBe(10); // 7 Kafka keys + 3 Common keys

		// Điền các giá trị vào
		const valMap: Record<string, string> = {
			BootstrapServers: '10.10.8.200:9092,10.10.8.201:9092',
			Topic: 'its.sharedata.traffic.v1',
			ClientId: 'sharedata-agent-client',
			SecurityProtocol: 'SaslPlaintext',
			SaslMechanism: 'PLAIN',
			SaslUsername: 'kafka_user',
			SaslPassword: 'kafka_password_secret',
			Enabled: 'true',
			Encryptor: 'None',
			EncryptionKeyRef: 'kms-key-01',
		};

		items.forEach((item) => {
			if (valMap[item.key]) {
				item.value = valMap[item.key];
			}
		});

		// Serialize to JSON
		const serialized = serializeListToJson(items);
		expect(serialized).toBeTruthy();

		const parsed = JSON.parse(serialized!);
		expect(parsed.BootstrapServers).toBe('10.10.8.200:9092,10.10.8.201:9092');
		expect(parsed.Topic).toBe('its.sharedata.traffic.v1');
		expect(parsed.ClientId).toBe('sharedata-agent-client');
		expect(parsed.SecurityProtocol).toBe('SaslPlaintext');
		expect(parsed.SaslUsername).toBe('kafka_user');
		expect(parsed.SaslPassword).toBe('kafka_password_secret');
		expect(parsed.Encryptor).toBe('None');
	});

	test('TC6 — Nhập BaseUrl hoặc bất kỳ key dành cho cột thì bị báo lỗi reserved', async () => {
		const items: ConfigRow[] = [
			{ id: '1', key: 'BaseUrl', value: 'http://example.com', showSecret: false },
			{ id: '2', key: 'port', value: '8080', showSecret: false },
			{ id: '3', key: 'endpoint', value: '/api/v1', showSecret: false },
			{ id: '4', key: 'Address', value: '10.0.0.1', showSecret: false },
			{ id: '5', key: 'TimeoutSeconds', value: '30', showSecret: false },
		];

		// BaseUrl (hàng 0) phải bị báo lỗi
		const errBaseUrl = getKeyError(items, 0, mockI18n);
		expect(errBaseUrl).toBe('Địa chỉ và cổng nhập ở trường Địa chỉ/Cổng');

		// port (hàng 1) phải bị báo lỗi
		const errPort = getKeyError(items, 1, mockI18n);
		expect(errPort).toBe('Địa chỉ và cổng nhập ở trường Địa chỉ/Cổng');

		// endpoint (hàng 2) phải bị báo lỗi
		const errEndpoint = getKeyError(items, 2, mockI18n);
		expect(errEndpoint).toBe('Địa chỉ và cổng nhập ở trường Địa chỉ/Cổng');

		// Address (hàng 3) phải bị báo lỗi
		const errAddress = getKeyError(items, 3, mockI18n);
		expect(errAddress).toBe('Địa chỉ và cổng nhập ở trường Địa chỉ/Cổng');

		// TimeoutSeconds (hàng 4) hợp lệ, không có lỗi
		const errTimeout = getKeyError(items, 4, mockI18n);
		expect(errTimeout).toBeNull();
	});

	test('TC7 — Kiểm tra validation: báo lỗi khi key rỗng hoặc key trùng lặp', async () => {
		const items: ConfigRow[] = [
			{ id: '1', key: 'Token', value: 'val1', showSecret: false },
			{ id: '2', key: 'token', value: 'val2', showSecret: false }, // trùng lặp không phân biệt hoa thường
			{ id: '3', key: '   ', value: 'val3', showSecret: false },  // rỗng
		];

		// Hàng 0 trùng với hàng 1
		expect(getKeyError(items, 0, mockI18n)).toBe('Tên thuộc tính bị trùng lặp');
		// Hàng 1 trùng với hàng 0
		expect(getKeyError(items, 1, mockI18n)).toBe('Tên thuộc tính bị trùng lặp');
		// Hàng 2 rỗng
		expect(getKeyError(items, 2, mockI18n)).toBe('Tên thuộc tính không được để trống');
	});

	test('TC8 — "Thêm key gợi ý" không duplicate key đã có sẵn', async () => {
		const items: ConfigRow[] = [
			{ id: '1', key: 'TimeoutSeconds', value: '60', showSecret: false },
			{ id: '2', key: 'BypassSsl', value: 'true', showSecret: false },
		];

		const added = addSuggestedKeys(items, 'API');
		// Gợi ý API có 8 keys + 3 common keys = 11 keys tổng cộng. Đã có 2 keys => chỉ thêm 9 keys
		expect(added).toBe(9);

		// Giá trị của TimeoutSeconds đã có trước đó phải giữ nguyên 60
		const timeoutRow = items.find((i) => i.key === 'TimeoutSeconds');
		expect(timeoutRow?.value).toBe('60');

		// Kiểm tra không có bất kỳ key nào bị trùng lặp
		const keySet = new Set<string>();
		items.forEach((item) => {
			const lower = item.key.toLowerCase();
			expect(keySet.has(lower), `Key ${item.key} bị xuất hiện nhiều hơn 1 lần`).toBeFalsy();
			keySet.add(lower);
		});

		// Bấm thêm lần nữa -> không được thêm dòng nào
		const addedSecondTime = addSuggestedKeys(items, 'API');
		expect(addedSecondTime).toBe(0);
	});

	test('TC9 — Chọn nhóm "Chiều nhận" thì có đủ key giải mã InboundEncryptor và DecryptionKeyRef', async () => {
		const editorContent = fs.readFileSync(editorComponentPath, 'utf-8');

		// Kiểm tra editor component có khai báo INBOUND_KEYS với InboundEncryptor và DecryptionKeyRef
		const expectedInboundKeys = [
			'InboundEncryptor',
			'DecryptionKeyRef',
		];

		for (const key of expectedInboundKeys) {
			expect(editorContent).toContain(key);
		}

		// Kiểm tra dropdown menu gợi ý nhóm Chiều nhận tồn tại
		expect(editorContent).toContain('command="inbound"');
		expect(editorContent).toContain('lz.label.sharedataConnectionEditor.groupInbound');
	});

	test('TC10 — "ArchiveEnabled" không còn trong COMMON_KEYS của connectionConfigEditor.vue và không được gợi ý', async () => {
		const editorContent = fs.readFileSync(editorComponentPath, 'utf-8');
		expect(editorContent).not.toContain("'ArchiveEnabled'");

		const items: ConfigRow[] = [];
		addSuggestedKeys(items, 'API');
		const archiveRow = items.find((i) => i.key.toLowerCase() === 'archiveenabled');
		expect(archiveRow).toBeUndefined();
	});

	test('TC11 — RV-12: Nhập OutputPath và FileNamePattern thiếu token đổi từ lỗi sang CẢNH BÁO', async () => {
		const itemsInvalid: ConfigRow[] = [
			{ id: '1', key: 'OutputPath', value: '/mnt/out/file.json', showSecret: false },
			{ id: '2', key: 'FileNamePattern', value: 'static_name.json', showSecret: false },
		];

		// Không coi là lỗi chặn lưu (getKeyError trả về null)
		expect(getKeyError(itemsInvalid, 0, mockI18n)).toBeNull();
		expect(getKeyError(itemsInvalid, 1, mockI18n)).toBeNull();

		// OutputPath (hàng 0) hiển thị thông điệp cảnh báo bãi bỏ
		const warnOutputPath = getKeyWarning(itemsInvalid, 0, mockI18n);
		expect(warnOutputPath).toBe('Thuộc tính [OutputPath] đã bị bãi bỏ. Vui lòng sử dụng [DirectoryPath] và [FileNamePattern]');

		// FileNamePattern thiếu biến (hàng 1) hiển thị thông điệp cảnh báo
		const warnMissingVar = getKeyWarning(itemsInvalid, 1, mockI18n);
		expect(warnMissingVar).toBe('FileNamePattern phải chứa ít nhất {serial} hoặc {timestamp} để tránh ghi đè tệp');

		// FileNamePattern hợp lệ (mảng riêng) -> không có cảnh báo
		const itemsValid: ConfigRow[] = [
			{ id: '3', key: 'FileNamePattern', value: 'data_{timestamp}_{serial}.json', showSecret: false },
		];
		const warnValidPattern = getKeyWarning(itemsValid, 0, mockI18n);
		expect(warnValidPattern).toBeNull();
	});

	test('TC12 — RV-13: JSON lỗi định dạng không bị xóa âm thầm khi lưu', async () => {
		const editorContent = fs.readFileSync(editorComponentPath, 'utf-8');
		// Component phải có biến rawInvalidJson, alert cảnh báo lỗi cú pháp và nút xóa lỗi
		expect(editorContent).toContain('rawInvalidJson');
		expect(editorContent).toContain('lz.label.sharedataConnectionEditor.errInvalidJson');
		expect(editorContent).toContain('clearInvalidJson');

		// Giả lập đối tác có chuỗi JSON lỗi: {abc
		const invalidJson = '{abc';
		const items = parseJsonToList(invalidJson);
		// Parse thất bại -> danh sách rỗng
		expect(items.length).toBe(0);

		// Logic giữ nguyên chuỗi cũ khi lưu nếu có rawInvalidJson
		let rawInvalid: string | null = invalidJson;
		const emitVal = rawInvalid ? rawInvalid : serializeListToJson(items);
		expect(emitVal).toBe('{abc');

		// Chỉ khi người dùng chủ động bấm xóa cấu hình lỗi thì rawInvalid mới về null
		rawInvalid = null;
		const emitAfterClear = rawInvalid ? rawInvalid : serializeListToJson(items);
		expect(emitAfterClear).toBeNull();
	});

	test('TC13 — Modal editPartner.vue có width tối thiểu 880px, ô ghi chú rows="3" và padding-bottom dialog body bằng 0', async () => {
		const partnerContent = fs.readFileSync(editPartnerPath, 'utf-8');

		// Kiểm tra dialog có width >= 880px
		const widthMatch = partnerContent.match(/<el-dialog[^>]*width="(\d+)px"/);
		expect(widthMatch).not.toBeNull();
		const widthValue = Number(widthMatch![1]);
		expect(widthValue).toBeGreaterThanOrEqual(880);

		// Kiểm tra class định danh cho dialog để style không bị ảnh hưởng Teleport
		expect(partnerContent).toContain('class="sharedata-edit-partner-dialog"');

		// Kiểm tra ô remark có :rows="3"
		expect(partnerContent).toMatch(/<el-input[^>]*v-model="state\.ruleForm\.remark"[^>]*:rows="3"/);

		// Kiểm tra padding-bottom: 0 cho body dialog
		expect(partnerContent).toContain('padding-bottom: 0');
		expect(partnerContent).toMatch(/:global\(\.sharedata-edit-partner-dialog\s+\.el-dialog__body\)/);
	});
});

test.describe('ShareData — Form cấu hình kết nối đối tác (connectionConfigForm) @sharedata', () => {

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

		// Mở hỗ trợ đầy đủ các kênh WebAPI (1), File (2), FTP (3), FTPS (4), Kafka (5) trong modal đối tác
		expect(partnerContent).toContain(':value="1"');
		expect(partnerContent).toContain(':value="2"');
		expect(partnerContent).toContain(':value="3"');
		expect(partnerContent).toContain(':value="4"');
		expect(partnerContent).toContain(':value="5"');
		expect(partnerContent).not.toContain(':value="2" :disabled="true"');

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
			'lz.label.sharedataConnectionForm.summaryDefault',
			'lz.label.sharedataConnectionForm.summaryCustomCount',
			'lz.label.sharedataConnectionForm.summaryExtraCount',
			'lz.label.sharedataConnectionForm.summaryEncrypted',
			'lz.label.sharedataConnectionForm.summaryNoEncryption',
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
		expect(viVn['lz.label.sharedataPartner.typeFtp']).toBe('Máy chủ FTP');
		expect(enUs['lz.label.sharedataPartner.typeFtp']).toBe('FTP Server');
		expect(viVn['lz.label.sharedataPartner.typeFtps']).toBe('Máy chủ FTPS');
		expect(enUs['lz.label.sharedataPartner.typeFtps']).toBe('FTPS Server');
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

		// Trạng thái mặc định: tất cả ô rỗng/null -> emit null (đối tác chưa cấu hình mở ra lưu giữ nguyên null)
		const defaultOut = serializeFormLogic(state, 'API', null);
		expect(defaultOut).toBeNull();

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

		// Khi AuthType là None và các trường khác rỗng -> emit null
		state.api.authType = 'None';
		state.api.token = '';
		state.api.timeoutSeconds = null as any;
		state.api.bypassSsl = false;
		const outNoAuth = serializeFormLogic(state, 'API', null);
		expect(outNoAuth).toBeNull();
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

	test('TC9 — Kênh Kafka: Ghi nhận mọi ô có giá trị của kênh đang chọn, không phụ thuộc SecurityProtocol', async () => {
		const state = createDefaultFormState();
		state.kafka.bootstrapServers = '10.10.8.200:9092';
		state.kafka.topic = 'its.traffic';

		// 1. Plaintext nhưng người dùng đã nhập SASL/SSL -> vẫn ghi ra JSON để không mất dữ liệu âm thầm
		state.kafka.securityProtocol = 'Plaintext';
		state.kafka.saslUsername = 'user';
		state.kafka.saslPassword = 'pwd';
		state.kafka.sslCaLocation = '/cert.pem';
		const outPlain = serializeFormLogic(state, 'Kafka', null);
		const parsedPlain = JSON.parse(outPlain!);
		expect(parsedPlain.SecurityProtocol).toBe('Plaintext');
		expect(parsedPlain.SaslUsername).toBe('user');
		expect(parsedPlain.SaslPassword).toBe('pwd');
		expect(parsedPlain.SslCaLocation).toBe('/cert.pem');

		// 2. SaslSsl -> ghi cả SaslMechanism, SaslUsername, SaslPassword và SslCaLocation
		state.kafka.securityProtocol = 'SaslSsl';
		state.kafka.saslMechanism = 'ScramSha256';
		const outSaslSsl = serializeFormLogic(state, 'Kafka', null);
		const parsedSaslSsl = JSON.parse(outSaslSsl!);
		expect(parsedSaslSsl.SecurityProtocol).toBe('SaslSsl');
		expect(parsedSaslSsl.SaslMechanism).toBe('ScramSha256');
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

	test('TC11 — Form cấu hình kết nối: Bố cục 2 cột (:sm="12"), Placeholder mặc định (30/15/5000), không tự ghi đè khi chưa nhập', async () => {
		const formContent = fs.readFileSync(formComponentPath, 'utf-8');

		// 1. Kiểm tra default timeoutSeconds: null trong formState của connectionConfigForm.vue (chỉ serialize khi người dùng nhập)
		expect(formContent).toMatch(/api:\s*\{[\s\S]*?timeoutSeconds:\s*null/);
		expect(formContent).toMatch(/ftp:\s*\{[\s\S]*?timeoutSeconds:\s*null/);
		expect(formContent).toMatch(/kafka:\s*\{[\s\S]*?timeoutMs:\s*null/);

		// 2. Kiểm tra các el-col trong channel-api là bố cục 2 cột (:sm="12")
		const apiSectionMatch = formContent.match(/<div v-if="normalizedChannel === 'API'" class="channel-fields channel-api">([\s\S]*?)<\/div>\s*<!-- ── 2\. KÊNH FILE/);
		expect(apiSectionMatch, 'Phải tìm thấy section channel-api').toBeTruthy();
		const apiSection = apiSectionMatch![1];

		// Có chia cột :sm="12" cho các ô thông số ngắn
		expect(apiSection.includes(':sm="12"')).toBeTruthy();

		// 3. Kiểm tra các placeholder mặc định của backend handler
		expect(formContent).toContain('lz.placeholder.sharedataConnectionForm.timeoutApi');
		expect(formContent).toContain('lz.placeholder.sharedataConnectionForm.timeoutFtp');
		expect(formContent).toContain('lz.placeholder.sharedataConnectionForm.timeoutKafka');

		// 4. Kiểm tra logic parseJsonToForm mặc định timeoutSeconds = null khi không truyền timeout
		const state = createDefaultFormState();
		expect(state.api.timeoutSeconds).toBeNull();
		expect(state.ftp.timeoutSeconds).toBeNull();

		// Parse json không có timeout -> vẫn giữ null (để hiển thị placeholder, không serialize nếu người dùng không gõ)
		parseJsonToFormLogic(JSON.stringify({ AuthType: 'Bearer', Token: 'abc' }), 'API', state);
		expect(state.api.timeoutSeconds).toBeNull();

		// Parse json rỗng -> reset về null
		state.api.timeoutSeconds = 99;
		state.ftp.timeoutSeconds = 99;
		parseJsonToFormLogic('', 'API', state);
		expect(state.api.timeoutSeconds).toBeNull();
		expect(state.ftp.timeoutSeconds).toBeNull();
	});

	test('TC12 — Hỗ trợ ConnectionTypeEnum số nguyên (1..5) và tương thích ngược chuỗi trên FE', async () => {
		// 1. Kiểm tra baseEnum.ts có ConnectionTypeEnum
		const baseEnumPath = resolveWebVueSrc('types/enums/baseEnum.ts');
		expect(fs.existsSync(baseEnumPath)).toBeTruthy();
		const baseEnumContent = fs.readFileSync(baseEnumPath, 'utf-8');
		expect(baseEnumContent).toContain('export enum ConnectionTypeEnum');
		expect(baseEnumContent).toMatch(/Api\s*=\s*1/);
		expect(baseEnumContent).toMatch(/File\s*=\s*2/);
		expect(baseEnumContent).toMatch(/Ftp\s*=\s*3/);
		expect(baseEnumContent).toMatch(/Ftps\s*=\s*4/);
		expect(baseEnumContent).toMatch(/Kafka\s*=\s*5/);

		// 2. Kiểm tra sharedataFeConfig.ts re-export ConnectionTypeEnum
		const feConfigPath = resolveWebVueSrc('types/constants/sharedataFeConfig.ts');
		const feConfigContent = fs.readFileSync(feConfigPath, 'utf-8');
		expect(feConfigContent).toContain('ConnectionTypeEnum');

		// 3. Kiểm tra connectionConfigForm.vue hỗ trợ props.connectionType nhận string | number | null
		const formContent = fs.readFileSync(formComponentPath, 'utf-8');
		expect(formContent).toContain('connectionType?: string | number | null');

		// 4. Kiểm tra connectionConfigEditor.vue nhận prop connectionType dạng [String, Number]
		const editorContent = fs.readFileSync(editorComponentPath, 'utf-8');
		expect(editorContent).toContain('type: [String, Number]');

		// 5. Kiểm tra editPartner.vue có đầy đủ 5 option enum số (1=Api, 2=File, 3=Ftp, 4=Ftps, 5=Kafka)
		const partnerContent = fs.readFileSync(editPartnerPath, 'utf-8');
		expect(partnerContent).toContain(':value="1"');
		expect(partnerContent).toContain(':value="2"');
		expect(partnerContent).toContain(':value="3"');
		expect(partnerContent).toContain(':value="4"');
		expect(partnerContent).toContain(':value="5"');

		// 6. Kiểm tra hàm fallback chuyển chuỗi legacy sang số nguyên enum trong editPartner.vue
		expect(partnerContent).toContain('ConnectionTypeEnum.Api');
		expect(partnerContent).toContain('ConnectionTypeEnum.File');
		expect(partnerContent).toContain('ConnectionTypeEnum.Ftp');
		expect(partnerContent).toContain('ConnectionTypeEnum.Ftps');
		expect(partnerContent).toContain('ConnectionTypeEnum.Kafka');
	});

	test('TC13 — Form cấu hình kết nối: Bỏ chế độ raw, ẩn hiện ô xác thực theo AuthType, cách ly JSON và lưu Tham số bổ sung', async () => {
		const formContent = fs.readFileSync(formComponentPath, 'utf-8');

		// (1) Không còn nút chuyển chế độ (chỉ còn form, không có raw mode)
		expect(formContent).not.toContain('bottom-action-bar');
		expect(formContent).not.toContain('toggleMode');
		expect(formContent).not.toContain('switchToRaw');
		expect(formContent).not.toContain('switchToForm');
		expect(formContent).not.toContain('raw-mode-container');
		expect(formContent).not.toContain('rawEditorRef');
		// Vẫn giữ rawInvalidJson để khôi phục khi JSON hỏng
		expect(formContent).toContain('rawInvalidJson');

		// (2) Auth None -> không có ô Token / Khóa API / Mật khẩu (dùng v-if cho từng AuthType)
		expect(formContent).toMatch(/v-if="formState\.api\.authType === 'Bearer'/);
		expect(formContent).toMatch(/v-if="formState\.api\.authType === 'ApiKey'/);
		expect(formContent).toMatch(/v-if="formState\.api\.authType === 'Basic'/);
		expect(formContent).not.toContain('noteNotUsedWithAuth');
		expect(formContent).not.toContain('warnNoAuthWithCreds');

		// (3) Kiểm tra các khối điều kiện hiển thị ô nhập liệu theo AuthType
		expect(formContent).toContain('v-model="formState.api.token"');
		expect(formContent).toContain('v-model="formState.api.headerName"');
		expect(formContent).toContain('v-model="formState.api.apiKey"');
		expect(formContent).toContain('v-model="formState.api.username"');
		expect(formContent).toContain('v-model="formState.api.password"');

		// (4) Đổi phương thức rồi lưu -> JSON chỉ chứa khóa của phương thức hiện tại
		const state = createDefaultFormState();
		state.api.token = 'bearer_token_abc';
		state.api.headerName = 'X-Custom-Key';
		state.api.apiKey = 'api_key_xyz';
		state.api.username = 'basic_user';
		state.api.password = 'basic_pass';

		// 4a. Khi chọn Bearer: JSON chỉ có AuthType và Token
		state.api.authType = 'Bearer';
		const outBearer = serializeFormLogic(state, 'API', null);
		expect(outBearer).toBeTruthy();
		const parsedBearer = JSON.parse(outBearer!);
		expect(parsedBearer.AuthType).toBe('Bearer');
		expect(parsedBearer.Token).toBe('bearer_token_abc');
		expect(parsedBearer.HeaderName).toBeUndefined();
		expect(parsedBearer.ApiKey).toBeUndefined();
		expect(parsedBearer.Username).toBeUndefined();
		expect(parsedBearer.Password).toBeUndefined();

		// 4b. Đổi sang ApiKey: JSON chỉ có AuthType, HeaderName và ApiKey
		state.api.authType = 'ApiKey';
		const outApiKey = serializeFormLogic(state, 'API', null);
		expect(outApiKey).toBeTruthy();
		const parsedApiKey = JSON.parse(outApiKey!);
		expect(parsedApiKey.AuthType).toBe('ApiKey');
		expect(parsedApiKey.HeaderName).toBe('X-Custom-Key');
		expect(parsedApiKey.ApiKey).toBe('api_key_xyz');
		expect(parsedApiKey.Token).toBeUndefined();
		expect(parsedApiKey.Username).toBeUndefined();
		expect(parsedApiKey.Password).toBeUndefined();

		// 4c. Đổi sang Basic: JSON chỉ có AuthType, Username và Password
		state.api.authType = 'Basic';
		const outBasic = serializeFormLogic(state, 'API', null);
		expect(outBasic).toBeTruthy();
		const parsedBasic = JSON.parse(outBasic!);
		expect(parsedBasic.AuthType).toBe('Basic');
		expect(parsedBasic.Username).toBe('basic_user');
		expect(parsedBasic.Password).toBe('basic_pass');
		expect(parsedBasic.Token).toBeUndefined();
		expect(parsedBasic.HeaderName).toBeUndefined();
		expect(parsedBasic.ApiKey).toBeUndefined();

		// 4d. Đổi sang None: JSON không chứa bất kỳ khóa xác thực nào
		state.api.authType = 'None';
		const outNone = serializeFormLogic(state, 'API', null);
		expect(outNone).toBeNull();

		// (5) Tham số bổ sung thêm 1 dòng -> có trong JSON
		const extra = { CustomDebugFlag: '1', CustomHeaderPrefix: 'MyPrefix' };
		const outWithExtra = serializeFormLogic(state, 'API', extra);
		expect(outWithExtra).toBeTruthy();
		const parsedWithExtra = JSON.parse(outWithExtra!);
		expect(parsedWithExtra.CustomDebugFlag).toBe('1');
		expect(parsedWithExtra.CustomHeaderPrefix).toBe('MyPrefix');

		// Kiểm tra mục Tham số bổ sung (collapseExtra) trong template vẫn nhúng connection-config-editor
		expect(formContent).toMatch(/<connection-config-editor[\s\S]*?v-model="extraParamsJson"/);
	});
});

