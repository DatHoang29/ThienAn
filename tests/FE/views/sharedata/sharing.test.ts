import { test, expect } from '@playwright/test';
import type { BrowserContext, Page } from '@playwright/test';
import { mockAuth, resolveWebVueSrc } from '../../setup';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

/**
 * Test Suite kiểm chứng 10 lỗi giao diện và logic tại phân hệ ShareData (trang sharing & dataSource):
 * 1. Tooltip 'sendImmediatelyHint' ngắt dòng (max-width: 280px, white-space: normal).
 * 2. Hiệu ứng loading (v-loading) trên 3 panel của trang sharing.
 * 3. Căn giữa theo trục dọc thanh tìm kiếm ở dataSource/index.vue.
 * 4. Chỉ hỗ trợ WebAPI trong modal đối tác, disable các kênh còn lại (File, Kafka, FTP).
 * 5. Ngôn ngữ thuần Việt cho gợi ý: "Chiều gửi", "Chiều nhận" (không lẫn Inbound / Kênh hiện tại).
 * 6. Ô ghi chú trong modal đối tác bỏ margin-bottom và tăng rows lên 2.
 * 7. Tiêu đề modal: "Tạo mới tên đối tác" (chữ "tên" viết thường).
 * 8. Trạng thái loading trên nút Xác nhận khi gọi API lưu đối tác.
 * 9. Logic EndPointApiUrl (/api/receive): FE tự động trim + prefix '/' và BE validator chấp nhận.
 * 10. Khoảng cách giữa icon và chữ Chi tiết trong cột audit (vxe-cell c--tooltip).
 */

const editSubPath = resolveWebVueSrc('views/sharedata/sharing/component/editSubscription.vue');
const sharingIndexPath = resolveWebVueSrc('views/sharedata/sharing/index.vue');
const dataSourceIndexPath = resolveWebVueSrc('views/sharedata/dataSource/index.vue');
const editPartnerPath = resolveWebVueSrc('views/sharedata/sharing/component/editPartner.vue');
const viVnPath = resolveWebVueSrc('i18n/lang/vi-vn.json');
const enUsPath = resolveWebVueSrc('i18n/lang/en-us.json');
const modifyRecordPath = resolveWebVueSrc('components/table/modifyRecord.vue');
const subscriptionTablePath = resolveWebVueSrc('views/sharedata/sharing/component/subscriptionTable.vue');
const appScssPath = resolveWebVueSrc('theme/app.scss');
const loadingScssPath = resolveWebVueSrc('views/sharedata/style/loading.scss');
const partnerValidatorPath = path.resolve(__dirname, '../../../../TA-ITS015-WEBAPI-V1.0/src/Modules/ShareData/Module.ShareData/Controllers/Partner/Validators/PartnerValidator.cs');

test.describe('ShareData — Kiểm thử 10 lỗi giao diện và logic (Sharing & DataSource)', () => {
	test('Bug 1 — Tooltip "Gửi ngay khi có dữ liệu mới" có cấu hình ngắt dòng (max-width và white-space normal)', async () => {
		const content = fs.readFileSync(editSubPath, 'utf-8');
		expect(content).toContain('sendImmediatelyHint');
		expect(content).toContain('max-width: 280px');
		expect(content).toContain('white-space: normal');
	});

	test('Bug 2 — Hiệu ứng v-loading khi gọi API trên 3 panel chính của trang sharing', async () => {
		const content = fs.readFileSync(sharingIndexPath, 'utf-8');
		// 1. Thẻ đối tác bên trái
		expect(content).toMatch(/<el-card[^>]*v-loading="loading"[^>]*class="eshare-body__left"/);
		// 2. Thẻ chi tiết phiên bên phải trên
		expect(content).toMatch(/<el-card[^>]*v-loading="loading"[^>]*:body-style="\{ padding: '12px 14px' \}"/);
		// 3. Thẻ bảng đăng ký bên phải dưới
		expect(content).toMatch(/<el-card[^>]*v-loading="loading"[^>]*class="sub-card"/);
		// 4. State loading và xử lý trong onMounted & handlePartnerQuery
		expect(content).toContain('const loading = ref(false)');
		expect(content).toMatch(/loading\.value = true/);
		expect(content).toMatch(/loading\.value = false/);
	});

	test('Bug 3 — Thanh tìm kiếm gói tin ở dataSource/index.vue căn giữa theo trục dọc', async () => {
		const content = fs.readFileSync(dataSourceIndexPath, 'utf-8');
		// 1. Không còn class mb5 trên el-col
		expect(content).not.toMatch(/<el-col class="mb5"[^>]*>\s*<el-form-item :label="\$t\('lz\.entity\.base\.code'\)"/);
		expect(content).not.toMatch(/<el-col class="mb5"[^>]*>\s*<el-form-item :label="\$t\('lz\.entity\.base\.name'\)"/);
		// 2. Card body padding đều
		expect(content).toContain(":body-style=\"{ padding: '6px 8px' }\"");
		// 3. SCSS căn giữa
		expect(content).toContain('&.search-card-packet');
		expect(content).toContain('align-items: center;');
	});

	test('Bug 4 — Kênh kết nối: Mở hỗ trợ đầy đủ các kênh WebAPI, File, Kafka, FTP trong modal đối tác', async () => {
		const content = fs.readFileSync(editPartnerPath, 'utf-8');
		// Option API không bị disabled
		expect(content).toMatch(/<el-option [^>]*value="API"[^>]*\/>/);
		expect(content).not.toMatch(/value="API"[^>]*:disabled="true"/);
		// Option File không bị disabled
		expect(content).toMatch(/<el-option [^>]*value="File"[^>]*\/>/);
		expect(content).not.toMatch(/value="File"[^>]*:disabled="true"/);
		// Option Kafka không bị disabled
		expect(content).toMatch(/<el-option [^>]*value="Kafka"[^>]*\/>/);
		expect(content).not.toMatch(/value="Kafka"[^>]*:disabled="true"/);
		// Option FTP không bị disabled
		expect(content).toMatch(/<el-option [^>]*value="FTP"[^>]*\/>/);
		expect(content).not.toMatch(/value="FTP"[^>]*:disabled="true"/);
	});


	test('Bug 5 — Nhãn gợi ý nhóm cấu hình thuần Việt và thuần Anh (Rule 20.13: Cấm song ngữ lai)', async () => {
		const viVn = JSON.parse(fs.readFileSync(viVnPath, 'utf-8'));
		const enUs = JSON.parse(fs.readFileSync(enUsPath, 'utf-8'));

		// vi-vn.json: Thuần Việt, không chứa "(Kênh hiện tại)" hay "(Inbound)"
		expect(viVn['lz.label.sharedataConnectionEditor.groupOutbound']).toBe('Chiều gửi');
		expect(viVn['lz.label.sharedataConnectionEditor.groupInbound']).toBe('Chiều nhận');

		// en-us.json: Thuần Anh
		expect(enUs['lz.label.sharedataConnectionEditor.groupOutbound']).toBe('Outbound');
		expect(enUs['lz.label.sharedataConnectionEditor.groupInbound']).toBe('Inbound');
	});

	test('Bug 6 — Ô ghi chú trong modal đối tác bỏ margin-bottom và tăng rows', async () => {
		const content = fs.readFileSync(editPartnerPath, 'utf-8');
		// Cột ghi chú không có class mb20
		expect(content).toMatch(/<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24">\s*<el-form-item :label="\$t\('lz\.entity\.sharedataPartner\.remark'\)"/);
		// Form item có style margin-bottom: 0
		expect(content).toContain('style="margin-bottom: 0;"');
		// Input textarea có :rows="2" hoặc :rows="3"
		expect(content).toMatch(/<el-input[^>]*type="textarea"[^>]*:rows="[23]"/);
	});

	test('Bug 7 — Tiêu đề "Tạo mới tên đối tác" có chữ "tên" viết thường', async () => {
		const viVn = JSON.parse(fs.readFileSync(viVnPath, 'utf-8'));
		expect(viVn['lz.label.sharedataPartner.name']).toBe('tên đối tác');

		// Kiểm tra tiêu đề tạo mới
		const template = viVn['lz.label.base.titleAdd'] || 'Tạo mới {name}';
		const title = template.replace('{name}', viVn['lz.label.sharedataPartner.name']);
		expect(title).toBe('Tạo mới tên đối tác');

		// Form field label dùng lz.entity.sharedataPartner.name ("Tên đối tác")
		expect(viVn['lz.entity.sharedataPartner.name']).toBe('Tên đối tác');
		const partnerContent = fs.readFileSync(editPartnerPath, 'utf-8');
		expect(partnerContent).toContain(":label=\"$t('lz.entity.sharedataPartner.name')\"");
	});

	test('Bug 8 — Nút Xác nhận có trạng thái loading khi gọi API lưu đối tác', async () => {
		const content = fs.readFileSync(editPartnerPath, 'utf-8');
		expect(content).toMatch(/<el-button type="primary" @click="submit"[^>]*:loading="state\.loading"/);
	});

	test('Bug 9 — Logic EndPointApiUrl (/api/receive): FE tự động trim + prefix "/" và BE Validator chấp nhận', async () => {
		const feContent = fs.readFileSync(editPartnerPath, 'utf-8');
		// FE: Có bước chuẩn hóa và tự động thêm "/" nếu là relative path
		expect(feContent).toContain('payload.endPointApiUrl');
		expect(feContent).toContain("ep = '/' + ep");

		// BE: PartnerValidator.cs chấp nhận relative path bắt đầu bằng "/" hoặc absolute URL
		if (fs.existsSync(partnerValidatorPath)) {
			const beContent = fs.readFileSync(partnerValidatorPath, 'utf-8');
			expect(beContent).toContain('EndPointApiUrl');
			expect(beContent).toMatch(/v!\.Trim\(\)\.StartsWith\("\/"\)/);
		}
	});

	test('Bug 10 — Khoảng cách giữa icon và nội dung "Chi tiết" trong ô vxe-cell c--tooltip', async () => {
		const modifyRecordContent = fs.readFileSync(modifyRecordPath, 'utf-8');
		// modifyRecord có icon với class mr4 và cursor-default có display inline-flex + gap
		expect(modifyRecordContent).toContain('class="mr4"');
		expect(modifyRecordContent).toContain('display: inline-flex');
		expect(modifyRecordContent).toContain('gap: 4px');

		// subscriptionTable.vue có style áp dụng cho vxe-cell.c--tooltip
		const subTableContent = fs.readFileSync(subscriptionTablePath, 'utf-8');
		expect(subTableContent).toContain('.vxe-cell.c--tooltip');
		expect(subTableContent).toContain('gap: 4px');
	});

	test('Bug 11 — Chuẩn hóa style loading 8-dots spinner đóng gói trực tiếp trong component sharing/index.vue', async () => {
		const content = fs.readFileSync(sharingIndexPath, 'utf-8');
		// 1. Cả 3 card trang sharing đều cấu hình :element-loading-text="$t('lz.label.base.loading')"
		expect(content).toMatch(/<el-card[^>]*v-loading="loading"[^>]*:element-loading-text="\$t\('lz\.label\.base\.loading'\)"[^>]*class="eshare-body__left"/);
		expect(content).toMatch(/<el-card[^>]*v-loading="loading"[^>]*:element-loading-text="\$t\('lz\.label\.base\.loading'\)"[^>]*:body-style="\{ padding: '12px 14px' \}"/);
		expect(content).toMatch(/<el-card[^>]*v-loading="loading"[^>]*:element-loading-text="\$t\('lz\.label\.base\.loading'\)"[^>]*class="sub-card"/);

		// 2. Style loading 8-dots spinner viết trực tiếp bên trong <style scoped> của sharing/index.vue
		expect(content).toContain(':deep(.el-loading-mask)');
		expect(content).toContain('mask-image');
		expect(content).toContain('sharedata-roll');
		expect(content).toContain('Đang tải...');

		// 3. app.scss và loading.scss KHÔNG còn tồn tại/chứa style phân hệ ShareData
		const appScssContent = fs.readFileSync(appScssPath, 'utf-8');
		expect(appScssContent).not.toContain('.eshare-sharing');
		expect(appScssContent).not.toContain('sharedata-roll');
		expect(fs.existsSync(loadingScssPath)).toBe(false);

		// 4. View trang sharing KHÔNG còn import loading.scss
		expect(content).not.toContain("@import '../style/loading.scss';");
	});
});

test.describe('E2E Runtime UI Check trên http://localhost:8888/#/shareData/sharing @e2e', () => {
	const setupSharingMocks = async (context: BrowserContext, page: Page) => {
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
							id: 'partner_1',
							code: 'P_REST',
							name: 'Đối tác REST',
							address: '127.0.0.1',
							port: 5005,
							endPointApiUrl: '/api/receive',
							connectionType: 'API',
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
				body: JSON.stringify({
					code: 200,
					result: [],
				}),
			});
		});

		await page.route('**/api/sharedata/sharedatasubscription/list*', async (route) => {
			await route.fulfill({
				status: 200,
				contentType: 'application/json',
				body: JSON.stringify({
					code: 200,
					result: [
						{
							id: 'sub_1',
							partnerId: 'partner_1',
							datatypeId: '101',
							mode: 1,
							direction: 1,
							state: 1,
							createTime: '2026-10-17T08:00:00Z',
						},
					],
				}),
			});
		});
	};

	test('TC-E2E — Truy cập http://localhost:8888/#/shareData/sharing và kiểm tra UI modal đối tác', async ({ context, page }) => {
		await setupSharingMocks(context, page);
		await page.goto('/#/sharedata/sharing');

		// 1. Kiểm tra 3 card hiển thị trên trang sharing
		const partnerCard = page.locator('.eshare-body__left');
		await expect(partnerCard).toBeVisible({ timeout: 15000 });

		// 2. Mở dialog Thêm đối tác
		const addBtn = page.locator('.ptn-list__head button:has-text("Thêm")');
		await expect(addBtn).toBeVisible();
		await addBtn.click();

		const dialog = page.locator('.sharedata-edit-partner-container .el-dialog');
		await expect(dialog).toBeVisible();

		// 3. Kiểm tra tiêu đề dialog: "Tạo mới tên đối tác" (Bug 7)
		const dialogHeader = dialog.locator('.el-dialog__header');
		await expect(dialogHeader).toContainText('Tạo mới tên đối tác');

		// 4. Kiểm tra ô Ghi chú: có rows="2" (Bug 6)
		const remarkInput = dialog.locator('textarea');
		await expect(remarkInput).toBeVisible();
		const rowsAttr = await remarkInput.getAttribute('rows');
		expect(Number(rowsAttr)).toBeGreaterThanOrEqual(2);

		// 5. Kiểm tra nút Xác nhận tồn tại và sẵn sàng (Bug 8)
		const confirmBtn = dialog.locator('.dialog-footer button.el-button--primary');
		await expect(confirmBtn).toBeVisible();
		await expect(confirmBtn).toContainText('Xác nhận');

		// 6. Đóng dialog
		const cancelBtn = dialog.locator('.dialog-footer button:has-text("Hủy")');
		await cancelBtn.click();
		await expect(dialog).toBeHidden();
	});
});
