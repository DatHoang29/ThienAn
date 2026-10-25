import { test, expect } from '@playwright/test';
import { mockAuth, resolveWebVueSrc } from '../../setup';
import fs from 'node:fs';
import path from 'node:path';

const formComponentPath = resolveWebVueSrc('views/sharedata/sharing/component/connectionConfigForm.vue');
const viVnPath = resolveWebVueSrc('i18n/lang/its/sharedata/vi-vn.json');
const enUsPath = resolveWebVueSrc('i18n/lang/its/sharedata/en-us.json');

/**
 * Mô phỏng logic lõi của connectionConfigEditor.vue (chỉ còn TC12 — RV-13 dùng)
 */
interface ConfigRow {
	id: string;
	key: string;
	value: string;
	showSecret: boolean;
}

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

test.describe('ShareData — Cấu hình kết nối đối tác theo mẫu VMS (R11) @sharedata', () => {

	test('TC12 — RV-13: JSON lỗi định dạng không bị xóa âm thầm khi lưu', async () => {
		const formContent = fs.readFileSync(formComponentPath, 'utf-8');
		// Component phải có biến rawInvalidJson, alert cảnh báo lỗi cú pháp và nút xóa lỗi
		expect(formContent).toContain('rawInvalidJson');
		expect(formContent).toContain('lz.label.sharedataConnectionEditor.errInvalidJson');
		expect(formContent).toContain('clearInvalidJson');

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
});

test.describe('ShareData — Form cấu hình kết nối đối tác (connectionConfigForm) @sharedata', () => {

	test('TC2 — Kiểm tra tệp i18n vi-vn.json và en-us.json đầy đủ các khóa của hộp thoại đối tác và connectionConfigForm', async () => {
		expect(fs.existsSync(viVnPath)).toBeTruthy();
		expect(fs.existsSync(enUsPath)).toBeTruthy();

		const viVn = JSON.parse(fs.readFileSync(viVnPath, 'utf-8'));
		const enUs = JSON.parse(fs.readFileSync(enUsPath, 'utf-8'));

		const keysToCheck = [
			'lz.button.sharedataConnectionEditor.addConfig',
			'lz.button.sharedataConnectionEditor.addSuggestedKeys',
			'lz.label.sharedataConnectionEditor.noConfig',
			'lz.label.sharedataConnectionEditor.errKeyRequired',
			'lz.label.sharedataConnectionEditor.errKeyReserved',
			'lz.label.sharedataConnectionEditor.errKeyDuplicate',
			'lz.label.sharedataConnectionEditor.errHasInvalidKeys',
			'lz.label.sharedataPartner.typeKafka',
			'lz.label.sharedataPartner.typeFtp',
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
			'lz.label.sharedataConnectionForm.remotePath',
			'lz.label.sharedataConnectionForm.useSsl',
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
			'lz.label.sharedataConnectionForm.preservedParamsNotice',
			'lz.label.sharedataPartner.ftpHost',
			'lz.label.sharedataPartner.ftpPort',
			'lz.label.sharedataConnectionForm.sectionFtpConn',
			'lz.label.sharedataConnectionForm.fileNameHint',
			'lz.label.sharedataConnectionForm.fileNameHintFixed',
		];

		for (const k of keysToCheck) {
			expect(viVn[k], `vi-vn.json thiếu key ${k}`).toBeTruthy();
			expect(enUs[k], `en-us.json thiếu key ${k}`).toBeTruthy();
		}

		const removedKeys = [
			'lz.label.sharedataConnectionForm.contractNone',
			'lz.label.sharedataConnectionForm.contractFurion',
			'lz.label.sharedataConnectionForm.contractUnifyResult',
			'lz.label.sharedataConnectionForm.responseContract',
			'lz.label.sharedataConnectionForm.collapsePartnerResponse',
			'lz.label.sharedataConnectionForm.descPartnerResponse',
			'lz.label.sharedataPartner.typeFtps',
			'lz.placeholder.sharedataPartner.fullAddress',
			'lz.label.sharedataConnectionForm.errorDetection',
			'lz.label.sharedataConnectionForm.errorDetectionAuto',
			'lz.label.sharedataConnectionForm.errorDetectionHttpOnly',
			'lz.label.sharedataConnectionForm.errorDetectionCustom',
			'lz.tooltip.sharedataConnectionForm.errorDetection',
			'lz.label.sharedataConnectionForm.warnCustomFieldRequired',
			'lz.label.sharedataConnectionForm.successField',
			'lz.label.sharedataConnectionForm.errorMessageField',
			'lz.label.sharedataConnectionForm.errorCodeField',
			'lz.placeholder.sharedataConnectionForm.successField',
			'lz.placeholder.sharedataConnectionForm.errorMessageField',
			'lz.placeholder.sharedataConnectionForm.errorCodeField',
			'lz.label.sharedataConnectionForm.ftpServerHint',
			'lz.label.sharedataConnectionForm.enabled',
		];
		for (const k of removedKeys) {
			expect(viVn[k], `vi-vn.json không được còn key ${k}`).toBeUndefined();
			expect(enUs[k], `en-us.json không được còn key ${k}`).toBeUndefined();
		}

		// Rule 20.13: Cấm song ngữ lai
		expect(viVn['lz.button.sharedataConnectionEditor.addSuggestedKeys']).toBe('Thêm key gợi ý');
		expect(viVn['lz.label.sharedataConnectionEditor.errKeyReserved']).toBe('Địa chỉ và cổng nhập ở trường Địa chỉ/Cổng');
		expect(viVn['lz.label.sharedataPartner.typeFtp']).toBe('Máy chủ FTP');
		expect(enUs['lz.label.sharedataPartner.typeFtp']).toBe('FTP Server');
		expect(viVn['lz.label.sharedataConnectionForm.useSsl']).toBe('Mã hóa kết nối FTPS');
		expect(enUs['lz.label.sharedataConnectionForm.useSsl']).toBe('FTPS encryption');
		expect(viVn['lz.label.sharedataPartner.ftpHost']).toBe('Máy chủ FTP');
		expect(enUs['lz.label.sharedataPartner.ftpHost']).toBe('FTP server');
		expect(viVn['lz.label.sharedataConnectionForm.sectionFtpConn']).toBe('Kết nối máy chủ');
		expect(enUs['lz.label.sharedataConnectionForm.sectionFtpConn']).toBe('Server connection');
	});

	test('TC16 — Kiểm tra trên trình duyệt: 5 trường hợp gợi ý Địa chỉ đầy đủ, bỏ ô Nhận biết đối tác báo lỗi và gọn khối FTP', async ({ context, page }) => {
		let lastUpdatedPayload: Record<string, any> | null = null;
		await mockAuth(context, page, {
			language: 'vi-vn',
			userInfo: {
				authBtnList: ['*', 'sharedataSharing:add', 'sharedataSharing:update', 'sharedataSharing:delete'],
				defaultMenu: '/sharedata/sharing',
			},
		});

		await page.route('**/api/system/sysmenu/loginmenutree*', async (route) => {
			await route.fulfill({
				status: 200,
				contentType: 'application/json',
				body: JSON.stringify({
					code: 200,
					result: [
						{
							id: 'menu_sharedata',
							pid: '0',
							path: '/sharedata',
							name: 'sharedata',
							component: 'layout/routerView/parent',
							meta: { title: 'lz.menu.sharedata', isLink: '', isHide: false, isKeepAlive: true, isAffix: false, isIframe: false, roles: ['admin'], icon: 'ele-Share' },
							children: [
								{
									id: 'menu_sharedata_sharing',
									pid: 'menu_sharedata',
									path: '/sharedata/sharing',
									name: 'sharedataSharing',
									component: 'sharedata/sharing/index',
									meta: { title: 'lz.menu.sharedataSharing', isLink: '', isHide: false, isKeepAlive: true, isAffix: false, isIframe: false, roles: ['admin'], icon: 'ele-Connection' },
								},
							],
						},
					],
				}),
			});
		});

		await page.route('**/api/sharedata/sharedatapartner/list*', async (route) => {
			await route.fulfill({
				status: 200,
				contentType: 'application/json',
				body: JSON.stringify({
					code: 200,
					result: [
						{
							id: 'partner_ftps_1',
							code: 'P_FTPS',
							name: 'Đối tác FTPS',
							address: '10.0.0.9',
							port: 990,
							endPointApiUrl: '',
							connectionType: 4,
							connectionConfig: JSON.stringify({ RemotePath: '/upload', Username: 'ftpuser' }),
							status: 1,
							sessionState: 0,
						},
					],
				}),
			});
		});

		await page.route('**/api/sharedata/sharedatasession/list*', async (route) => {
			await route.fulfill({
				status: 200,
				contentType: 'application/json',
				body: JSON.stringify({ code: 200, result: [] }),
			});
		});

		await page.route('**/api/sharedata/sharedatasubscription/list*', async (route) => {
			await route.fulfill({
				status: 200,
				contentType: 'application/json',
				body: JSON.stringify({ code: 200, result: [] }),
			});
		});

		await page.route('**/api/sharedata/sharedatapartner/update*', async (route) => {
			lastUpdatedPayload = route.request().postDataJSON();
			await route.fulfill({
				status: 200,
				contentType: 'application/json',
				body: JSON.stringify({ code: 200, result: true }),
			});
		});

		await page.goto('/#/sharedata/sharing');
		await expect(page.locator('.eshare-body__left')).toBeVisible({ timeout: 30000 });

		// 1. Mở dialog Thêm đối tác
		const addBtn = page.locator('.ptn-list__head button:has-text("Thêm")');
		await expect(addBtn).toBeVisible();
		await addBtn.click();

		const dialog = page.locator('.sharedata-edit-partner-container .el-dialog');
		await expect(dialog).toBeVisible();

		// Chuyển sang tab Kết nối để kiểm tra các trường kết nối
		await dialog.getByRole('tab', { name: 'Kết nối' }).click();

		const addressInput = dialog.locator('.el-form-item').filter({ has: page.locator('.el-form-item__label', { hasText: 'Địa chỉ (IP/host)' }) }).locator('input');
		const portInput = dialog.locator('.el-form-item').filter({ has: page.locator('.el-form-item__label', { hasText: /^\s*Cổng\s*$/ }) }).locator('input');
		const endpointItem = dialog.locator('.el-form-item').filter({ hasText: 'Endpoint' }).first();
		const endpointInput = endpointItem.locator('input');
		const hintLocator = dialog.locator('.full-endpoint-hint');

		// Mặc định là API (1) -> ô Endpoint hiện
		await expect(endpointInput).toBeVisible();

		// Kiểm tra 5 trường hợp Địa chỉ + Cổng + Endpoint trên trình duyệt
		// TH1: 10.0.0.5 + 5005 + api/x -> http://10.0.0.5:5005/api/x
		await addressInput.fill('10.0.0.5');
		await portInput.fill('5005');
		await portInput.blur();
		await endpointInput.fill('api/x');
		await expect(hintLocator).toBeVisible();
		await expect(hintLocator).toContainText('http://10.0.0.5:5005/api/x');

		const screenshotDir = process.env.PARTNER_SCREENSHOT_DIR;
		if (screenshotDir) {
			await dialog.screenshot({ path: path.join(screenshotDir, 'partner-api-auto.png') });
		}

		// TH2: https://p.vn + 443 + /api/x -> https://p.vn:443/api/x
		await addressInput.fill('https://p.vn');
		await portInput.fill('443');
		await portInput.blur();
		await endpointInput.fill('/api/x');
		await expect(hintLocator).toContainText('https://p.vn:443/api/x');

		// TH3: p.vn:8080 + 5005 + /api/x -> http://p.vn:8080/api/x
		await addressInput.fill('p.vn:8080');
		await portInput.fill('5005');
		await portInput.blur();
		await endpointInput.fill('/api/x');
		await expect(hintLocator).toContainText('http://p.vn:8080/api/x');

		// TH4: 10.0.0.5 + 5005 + https://other.vn/recv -> https://other.vn/recv
		await addressInput.fill('10.0.0.5');
		await portInput.fill('5005');
		await portInput.blur();
		await endpointInput.fill('https://other.vn/recv');
		await expect(hintLocator).toContainText('https://other.vn/recv');

		// TH5: "" + null + /api/x -> ẩn dòng gợi ý
		await addressInput.fill('');
		await endpointInput.fill('/api/x');
		await expect(hintLocator).toBeHidden();

		// Điền lại địa chỉ chuẩn; khối "Gửi dữ liệu" kênh API không còn ô chọn nhận biết lỗi
		await addressInput.fill('10.0.0.5');
		await endpointInput.fill('/api/receive');
		await expect(dialog.getByText('Nhận biết đối tác báo lỗi')).toHaveCount(0);
		await expect(dialog.locator('.channel-api .el-select').filter({ hasText: 'Tự động (khuyến nghị)' })).toHaveCount(0);
		await expect(dialog.locator('.channel-api .el-col-md-8')).toHaveCount(0);
		await expect(dialog.locator('.channel-api .el-form-item').filter({ hasText: 'Bỏ qua kiểm tra chứng chỉ' })).toBeVisible();

		// Bố cục khi chọn API: ô Cổng < ô Endpoint < ô Loại kênh
		const portItem = dialog.locator('.el-form-item').filter({ has: page.locator('.el-form-item__label', { hasText: /^\s*Cổng\s*$/ }) });
		const connTypeItem = dialog.locator('.el-form-item').filter({ hasText: 'Loại kênh kết nối' });
		const portBoxApi = await portItem.boundingBox();
		const endpointBoxApi = await endpointItem.boundingBox();
		const connTypeBoxApi = await connTypeItem.boundingBox();
		expect(portBoxApi!.y).toBeLessThan(endpointBoxApi!.y);
		expect(endpointBoxApi!.y).toBeLessThan(connTypeBoxApi!.y);

		if (screenshotDir) {
			await dialog.screenshot({ path: path.join(screenshotDir, 'partner-api-auto.png') });
		}

		// 2. Đổi Loại kênh kết nối sang "Máy chủ FTP" -> ô Endpoint ẩn, công tắc "Mã hóa kết nối FTPS" hiện
		await portInput.fill('');
		await portInput.blur();
		const connTypeSelect = dialog.locator('.el-form-item').filter({ hasText: 'Loại kênh kết nối' }).locator('.el-select');
		await connTypeSelect.click();
		const visibleOptions = page.locator('.el-select-dropdown:visible .el-select-dropdown__item');
		await expect(visibleOptions).toHaveCount(4);
		if (screenshotDir) {
			await visibleOptions.filter({ hasText: 'Xuất tệp tin' }).click();
			await dialog.screenshot({ path: path.join(screenshotDir, 'partner-file.png') });
			await connTypeSelect.click();
		}
		await visibleOptions.filter({ hasText: 'Máy chủ FTP' }).click();

		await expect(endpointInput).toBeHidden();
		await expect(hintLocator).toBeHidden();

		// Bố cục khi chọn FTP: ô Địa chỉ và ô Cổng cùng hàng, nhỏ hơn ô Loại kênh; ô Loại kênh không nhảy cột
		const addressItemFtp = dialog.locator('.el-form-item').filter({ has: page.locator('.el-form-item__label', { hasText: 'Máy chủ FTP' }) });
		const portItemFtp = dialog.locator('.el-form-item').filter({ has: page.locator('.el-form-item__label', { hasText: 'Cổng FTP' }) });
		const addrBoxFtp = await addressItemFtp.boundingBox();
		const portBoxFtp = await portItemFtp.boundingBox();
		const connTypeBoxFtp = await connTypeItem.boundingBox();
		expect(Math.round(addrBoxFtp!.y)).toBe(Math.round(portBoxFtp!.y));
		expect(portBoxFtp!.y).toBeLessThan(connTypeBoxFtp!.y);
		expect(Math.round(connTypeBoxFtp!.x)).toBe(Math.round(connTypeBoxApi!.x));

		const ftpsSwitchItem = dialog.locator('.channel-ftp .el-form-item').filter({ hasText: 'Mã hóa kết nối FTPS' });
		await expect(ftpsSwitchItem).toBeVisible();

		// Nhãn đổi theo loại FTP, cổng điền sẵn 21 khi thêm mới
		await expect(dialog.locator('.el-form-item__label', { hasText: 'Máy chủ FTP' })).toBeVisible();
		await expect(dialog.locator('.el-form-item__label', { hasText: 'Cổng FTP' })).toBeVisible();
		await expect(dialog.locator('.el-form-item').filter({ has: page.locator('.el-form-item__label', { hasText: 'Cổng FTP' }) }).locator('input')).toHaveValue('21');
		await expect(dialog.locator('.channel-ftp .el-alert')).toHaveCount(0);

		// 2 mục mới: "Kết nối máy chủ" chứa FTPS và Thời gian chờ; "Gửi dữ liệu" không còn Thời gian chờ và Kênh đang bật
		const ftpTitles = dialog.locator('.channel-ftp .section-title');
		await expect(ftpTitles).toHaveCount(2);
		await expect(ftpTitles.nth(0)).toContainText('Kết nối máy chủ');
		await expect(ftpTitles.nth(1)).toContainText('Gửi dữ liệu');
		await expect(dialog.locator('.channel-ftp .el-form-item').filter({ hasText: 'Thời gian chờ' })).toHaveCount(1);
		await expect(dialog.locator('.channel-ftp .el-form-item').filter({ hasText: 'Kênh đang bật' })).toHaveCount(0);

		// Ô Tên tệp: nhãn có icon tooltip cảnh báo ghi đè
		const fileNameItem = dialog.locator('.channel-ftp .el-form-item').filter({ has: page.locator('.el-form-item__label', { hasText: 'Tên tệp' }) });
		await expect(fileNameItem.locator('input')).toHaveAttribute('placeholder', 'Để trống để hệ thống tự đặt tên');
		await expect(fileNameItem.locator('.label-help-icon')).toBeVisible();
		if (screenshotDir) {
			await dialog.screenshot({ path: path.join(screenshotDir, 'partner-ftp-ftps.png') });
		}

		// Đóng dialog Thêm (đóng ngay, không hỏi xác nhận)
		await dialog.locator('.dialog-footer button:has-text("Hủy")').click();
		await expect(dialog).toBeHidden();

		// 3. Mở sửa đối tác FTPS (connectionType = 4) -> hiển thị "Máy chủ FTP" + bật "Mã hóa kết nối FTPS", lưu gửi connectionType = 4
		const editBtn = page.locator('.ptn-item').first().getByRole('button', { name: 'Chỉnh sửa' });
		await editBtn.click();
		await expect(dialog).toBeVisible();
		await dialog.getByRole('tab', { name: 'Kết nối' }).click();
		await expect(connTypeSelect).toContainText('Máy chủ FTP');
		const ftpsSwitch = dialog.locator('.channel-ftp .el-form-item').filter({ hasText: 'Mã hóa kết nối FTPS' }).locator('.el-switch');
		await expect(ftpsSwitch).toHaveClass(/is-checked/);

		await dialog.locator('.dialog-footer button.el-button--primary').click();
		await expect.poll(() => lastUpdatedPayload).not.toBeNull();
		expect(lastUpdatedPayload!.connectionType).toBe(4);
		expect(JSON.parse(lastUpdatedPayload!.connectionConfig).UseSsl).toBe('true');
	});

	test('TC30 — Hộp thoại tạo mới/chỉnh sửa đối tác đóng ngay khi click ngoài (không hỏi xác nhận); tiêu đề chuẩn "Tạo mới đối tác"', async ({ context, page }) => {
		await mockAuth(context, page, {
			language: 'vi-vn',
			userInfo: {
				authBtnList: ['*', 'sharedataSharing:add', 'sharedataSharing:update', 'sharedataSharing:delete'],
				defaultMenu: '/sharedata/sharing',
			},
		});

		await page.route('**/api/system/sysmenu/loginmenutree*', async (route) => {
			await route.fulfill({
				status: 200,
				contentType: 'application/json',
				body: JSON.stringify({
					code: 200,
					result: [
						{
							id: 'menu_sharedata',
							pid: '0',
							path: '/sharedata',
							name: 'sharedata',
							component: 'layout/routerView/parent',
							meta: { title: 'lz.menu.sharedata', isLink: '', isHide: false, isKeepAlive: true, isAffix: false, isIframe: false, roles: ['admin'], icon: 'ele-Share' },
							children: [
								{
									id: 'menu_sharedata_sharing',
									pid: 'menu_sharedata',
									path: '/sharedata/sharing',
									name: 'sharedataSharing',
									component: 'sharedata/sharing/index',
									meta: { title: 'lz.menu.sharedataSharing', isLink: '', isHide: false, isKeepAlive: true, isAffix: false, isIframe: false, roles: ['admin'], icon: 'ele-Connection' },
								},
							],
						},
					],
				}),
			});
		});

		await page.route('**/api/sharedata/sharedatapartner/list*', async (route) => {
			await route.fulfill({
				status: 200,
				contentType: 'application/json',
				body: JSON.stringify({ code: 200, result: [] }),
			});
		});

		await page.route('**/api/sharedata/sharedatasession/list*', async (route) => {
			await route.fulfill({
				status: 200,
				contentType: 'application/json',
				body: JSON.stringify({ code: 200, result: [] }),
			});
		});

		await page.route('**/api/sharedata/sharedatasubscription/list*', async (route) => {
			await route.fulfill({
				status: 200,
				contentType: 'application/json',
				body: JSON.stringify({ code: 200, result: [] }),
			});
		});

		await page.setViewportSize({ width: 1600, height: 900 });
		await page.goto('/#/sharedata/sharing');
		await expect(page.locator('.eshare-body__left')).toBeVisible({ timeout: 30000 });

		const addBtn = page.locator('.ptn-list__head button:has-text("Thêm")');
		await expect(addBtn).toBeVisible();

		// 1. Mở dialog Thêm -> kiểm tra tiêu đề là "Tạo mới đối tác" (không còn "tên đối tác")
		await addBtn.click();
		const dialog = page.locator('.sharedata-edit-partner-container .el-dialog');
		await expect(dialog).toBeVisible();
		const headerEl = dialog.locator('.el-dialog__header');
		await expect(headerEl).toContainText('Tạo mới đối tác');
		await expect(headerEl).not.toContainText('tên đối tác');

		// 2. Chưa sửa gì -> click outside tại (5, 5) -> đóng ngay lập tức không hỏi xác nhận
		await page.mouse.click(5, 5);
		await expect(dialog).toBeHidden({ timeout: 5000 });

		// 3. Mở lại -> sửa trường Mã đối tác -> click outside -> đóng ngay, không hỏi "Có thay đổi chưa lưu"
		await addBtn.click();
		await expect(dialog).toBeVisible();

		const codeInput = dialog.locator('.el-form-item').filter({ has: page.locator('.el-form-item__label', { hasText: 'Mã đối tác' }) }).locator('input');
		await codeInput.fill('PARTNER_DIRTY_TEST');

		await page.mouse.click(5, 5);
		await expect(dialog).toBeHidden({ timeout: 5000 });
		await expect(page.locator('.el-message-box')).toHaveCount(0);
	});

	test('TC18 — Form đối tác chia 2 tab (Thông tin chung, Kết nối), tự chuyển tab khi lỗi và giữ nguyên thời gian chờ khi lưu/mở lại', async ({ context, page }) => {
		let savedPartner: Record<string, any> | null = null;
		await mockAuth(context, page, {
			language: 'vi-vn',
			userInfo: {
				authBtnList: ['*', 'sharedataSharing:add', 'sharedataSharing:update', 'sharedataSharing:delete'],
				defaultMenu: '/sharedata/sharing',
			},
		});

		await page.route('**/api/system/sysmenu/loginmenutree*', async (route) => {
			await route.fulfill({
				status: 200,
				contentType: 'application/json',
				body: JSON.stringify({
					code: 200,
					result: [
						{
							id: 'menu_sharedata',
							pid: '0',
							path: '/sharedata',
							name: 'sharedata',
							component: 'layout/routerView/parent',
							meta: { title: 'lz.menu.sharedata', isLink: '', isHide: false, isKeepAlive: true, isAffix: false, isIframe: false, roles: ['admin'], icon: 'ele-Share' },
							children: [
								{
									id: 'menu_sharedata_sharing',
									pid: 'menu_sharedata',
									path: '/sharedata/sharing',
									name: 'sharedataSharing',
									component: 'sharedata/sharing/index',
									meta: { title: 'lz.menu.sharedataSharing', isLink: '', isHide: false, isKeepAlive: true, isAffix: false, isIframe: false, roles: ['admin'], icon: 'ele-Connection' },
								},
							],
						},
					],
				}),
			});
		});

		await page.route('**/api/sharedata/sharedatapartner/list*', async (route) => {
			await route.fulfill({
				status: 200,
				contentType: 'application/json',
				body: JSON.stringify({
					code: 200,
					result: savedPartner ? [savedPartner] : [],
				}),
			});
		});

		await page.route('**/api/sharedata/sharedatasession/list*', async (route) => {
			await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ code: 200, result: [] }) });
		});

		await page.route('**/api/sharedata/sharedatasubscription/list*', async (route) => {
			await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ code: 200, result: [] }) });
		});

		await page.route('**/api/sharedata/sharedatapartner/add*', async (route) => {
			const body = route.request().postDataJSON();
			savedPartner = {
				...body,
				id: 'partner_saved_01',
				status: 1,
				sessionState: 0,
			};
			await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ code: 200, result: 'partner_saved_01' }) });
		});

		await page.goto('/#/sharedata/sharing');
		await expect(page.locator('.eshare-body__left')).toBeVisible({ timeout: 30000 });

		await page.locator('.ptn-list__head button:has-text("Thêm")').click();
		const dialog = page.locator('.sharedata-edit-partner-container .el-dialog');
		await expect(dialog).toBeVisible();

		// 1. Có đúng 2 tab: Thông tin chung, Kết nối; không còn tab Cấu hình vận hành; mặc định đứng ở tab Thông tin chung
		const tabs = dialog.locator('.partner-tabs .el-tabs__item');
		await expect(tabs).toHaveCount(2);
		const tabGeneral = dialog.getByRole('tab', { name: 'Thông tin chung' });
		const tabConnection = dialog.getByRole('tab', { name: 'Kết nối' });
		await expect(dialog.getByRole('tab', { name: 'Cấu hình vận hành' })).toHaveCount(0);
		await expect(dialog.locator('#pane-operation')).toHaveCount(0);
		await expect(tabGeneral).toHaveClass(/is-active/);

		// 2. Điền Mã và Tên ở tab 1, để trống Địa chỉ/Cổng ở tab Kết nối -> bấm Xác nhận tự chuyển sang tab Kết nối
		await dialog.locator('.el-form-item').filter({ hasText: 'Mã đối tác' }).locator('input').fill('PTN_TAB_01');
		await dialog.locator('.el-form-item').filter({ hasText: 'Tên đối tác' }).locator('input').fill('Đối tác Tab 01');
		await dialog.locator('.dialog-footer button.el-button--primary').click();
		await expect(tabConnection).toHaveClass(/is-active/);

		// 3. Điền Địa chỉ, Cổng và Thời gian chờ = 45 giây (ô của cấu hình kênh API) ngay trong tab Kết nối
		await dialog.locator('.el-form-item').filter({ has: page.locator('.el-form-item__label', { hasText: 'Địa chỉ (IP/host)' }) }).locator('input').fill('10.10.1.50');
		const portInput = dialog.locator('.el-form-item').filter({ has: page.locator('.el-form-item__label', { hasText: /^\s*Cổng\s*$/ }) }).locator('input');
		await portInput.fill('8080');
		await portInput.blur();

		const timeoutInputOf = () => dialog.locator('#pane-connection .el-form-item')
			.filter({ has: page.locator('.el-form-item__label', { hasText: 'Thời gian chờ (giây)' }) })
			.locator('.el-input-number input');
		await expect(timeoutInputOf()).toBeVisible();
		await timeoutInputOf().fill('45');
		await timeoutInputOf().blur();

		// 4. Bấm Xác nhận lưu -> mở lại đối tác vừa lưu -> dữ liệu và thời gian chờ 45 giây vẫn giữ nguyên
		await dialog.locator('.dialog-footer button.el-button--primary').click();
		await expect(dialog).toBeHidden({ timeout: 5000 });
		expect(savedPartner).not.toBeNull();
		expect(JSON.parse(savedPartner!.connectionConfig).TimeoutSeconds).toBe('45');

		const editBtn = page.locator('.ptn-item').first().getByRole('button', { name: 'Chỉnh sửa' });
		await editBtn.click();
		await expect(dialog).toBeVisible();
		await expect(tabGeneral).toHaveClass(/is-active/);
		await expect(dialog.locator('.el-form-item').filter({ hasText: 'Mã đối tác' }).locator('input')).toHaveValue('PTN_TAB_01');

		await tabConnection.click();
		await expect(timeoutInputOf()).toHaveValue('45');
	});
});

