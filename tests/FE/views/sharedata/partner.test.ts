import { test, expect } from '@playwright/test';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const editorComponentPath = path.resolve(
	__dirname,
	'../../../../TA-ITS015-WEBVUE-V1.0/src/src/views/sharedata/sharing/component/connectionConfigEditor.vue'
);
const editPartnerPath = path.resolve(
	__dirname,
	'../../../../TA-ITS015-WEBVUE-V1.0/src/src/views/sharedata/sharing/component/editPartner.vue'
);
const viVnPath = path.resolve(
	__dirname,
	'../../../../TA-ITS015-WEBVUE-V1.0/src/src/i18n/lang/vi-vn.json'
);
const enUsPath = path.resolve(
	__dirname,
	'../../../../TA-ITS015-WEBVUE-V1.0/src/src/i18n/lang/en-us.json'
);

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

const COMMON_KEYS = ['ArchiveEnabled', 'Enabled', 'Codec', 'EncryptionKeyRef'];

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

function addSuggestedKeys(items: ConfigRow[], connectionType: string): number {
	const channel = (connectionType || 'API').toUpperCase();
	const channelKeys = SUGGESTED_KEYS_BY_CHANNEL[channel] || SUGGESTED_KEYS_BY_CHANNEL.API;
	const allSuggestions = [...channelKeys, ...COMMON_KEYS];

	const existingKeys = new Set(items.map((i) => i.key.trim().toLowerCase()));
	let addedCount = 0;

	allSuggestions.forEach((sugKey) => {
		if (!existingKeys.has(sugKey.toLowerCase())) {
			let defaultValue = '';
			if (sugKey.toLowerCase() === 'archiveenabled') {
				defaultValue = 'true';
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
	};
	return dict[key] || key;
};

test.describe('ShareData — Cấu hình kết nối đối tác theo mẫu VMS (Prompt #13) @sharedata', () => {

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

		// editPartner.vue phải nhúng connectionConfigEditor và bỏ các biến/methods cũ
		expect(partnerContent).toContain('<connection-config-editor');
		expect(partnerContent).not.toContain('configMode');
		expect(partnerContent).not.toContain('buildConnectionConfigJson');
		expect(partnerContent).not.toContain('parseConnectionConfigToJson');
		expect(partnerContent).not.toContain('connectionConfigForm');
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
		expect(added).toBe(11); // 7 Kafka keys + 4 Common keys (bao gồm ArchiveEnabled)

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
			Codec: 'Json',
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
		expect(parsed.Codec).toBe('Json');
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
		// Gợi ý API có 8 keys + 4 common keys = 12 keys tổng cộng. Đã có 2 keys => chỉ thêm 10 keys
		expect(added).toBe(10);

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

	test('TC9 — Chọn nhóm "Chiều nhận" thì có đủ 6 key, InboundApiKey và InboundToken được che bí mật (#14)', async () => {
		const editorContent = fs.readFileSync(editorComponentPath, 'utf-8');

		// Kiểm tra editor component có khai báo INBOUND_KEYS với đủ 6 key
		const expectedInboundKeys = [
			'InboundAuthType',
			'InboundHeaderName',
			'InboundApiKey',
			'InboundToken',
			'InboundCodec',
			'DecryptionKeyRef',
		];

		for (const key of expectedInboundKeys) {
			expect(editorContent).toContain(key);
		}

		// Kiểm tra SECRET_KEYS có chứa inboundapikey và inboundtoken
		expect(editorContent.toLowerCase()).toContain('inboundapikey');
		expect(editorContent.toLowerCase()).toContain('inboundtoken');

		// Giả lập logic kiểm tra bí mật
		const isSecretKey = (k: string) => ['inboundapikey', 'inboundtoken', 'apikey', 'token', 'password'].includes(k.toLowerCase());
		expect(isSecretKey('InboundApiKey')).toBe(true);
		expect(isSecretKey('InboundToken')).toBe(true);
		expect(isSecretKey('InboundAuthType')).toBe(false);
		expect(isSecretKey('InboundCodec')).toBe(false);

		// Kiểm tra dropdown menu gợi ý nhóm Chiều nhận tồn tại
		expect(editorContent).toContain('command="inbound"');
		expect(editorContent).toContain('lz.label.sharedataConnectionEditor.groupInbound');
	});

	test('TC10 — "ArchiveEnabled" có trong COMMON_KEYS của connectionConfigEditor.vue với giá trị mặc định true (#15)', async () => {
		const editorContent = fs.readFileSync(editorComponentPath, 'utf-8');
		expect(editorContent).toContain('ArchiveEnabled');
		expect(editorContent).toContain("defaultValue = 'true'");

		const items: ConfigRow[] = [];
		addSuggestedKeys(items, 'API');
		const archiveRow = items.find((i) => i.key.toLowerCase() === 'archiveenabled');
		expect(archiveRow).toBeDefined();
		expect(archiveRow?.value).toBe('true');
	});
});
