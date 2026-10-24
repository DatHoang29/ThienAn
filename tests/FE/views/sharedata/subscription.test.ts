import { test, expect } from '@playwright/test';
import { resolveWebVueSrc } from '../../setup';
import fs from 'node:fs';

/**
 * Test Suite cho chức năng đăng ký chia sẻ dữ liệu (editSubscription.vue):
 * - TC1: Kiểm tra đầy đủ bản dịch i18n trong vi-vn.json và en-us.json.
 * - TC2: E2E tab Gửi đi / Nhận về trên bảng đăng ký và trong modal Đăng ký chia sẻ.
 */

test.describe('ShareData — Khung giờ đăng ký chia sẻ dữ liệu (editSubscription.vue) @sharedata', () => {

	test('TC1 — Kiểm tra cấu hình i18n trong vi-vn.json và en-us.json', async () => {
		const viPath = resolveWebVueSrc('i18n/lang/its/sharedata/vi-vn.json');
		const enPath = resolveWebVueSrc('i18n/lang/its/sharedata/en-us.json');

		const viJson = JSON.parse(fs.readFileSync(viPath, 'utf8'));
		const enJson = JSON.parse(fs.readFileSync(enPath, 'utf8'));

		expect(viJson['lz.validation.sharedataSubscription.timeRangeInvalid']).toBe(
			'Giờ kết thúc phải lớn hơn giờ bắt đầu.'
		);
		expect(viJson['lz.validation.sharedataSubscription.timeRangeIncomplete']).toBe(
			'Vui lòng nhập đầy đủ giờ bắt đầu và kết thúc (hoặc để trống cả hai).'
		);

		expect(enJson['lz.validation.sharedataSubscription.timeRangeInvalid']).toBe(
			'End time must be greater than start time.'
		);
		expect(enJson['lz.validation.sharedataSubscription.timeRangeIncomplete']).toBe(
			'Please enter both start and end times (or leave both blank).'
		);

		// Kiểm tra đủ 4 kiểu lập lịch dùng chung (continuous, daily, weekly, interval)
		expect(viJson['lz.label.sharedataSubscription.continuous']).toBe('Liên tục');
		expect(viJson['lz.label.sharedataSubscription.daily']).toBe('Hàng ngày');
		expect(viJson['lz.label.sharedataSubscription.weekly']).toBe('Hàng tuần');
		expect(viJson['lz.label.sharedataSubscription.interval']).toBe('Theo chu kỳ');

		expect(enJson['lz.label.sharedataSubscription.continuous']).toBe('Continuous');
		expect(enJson['lz.label.sharedataSubscription.daily']).toBe('Daily');
		expect(enJson['lz.label.sharedataSubscription.weekly']).toBe('Weekly');
		expect(enJson['lz.label.sharedataSubscription.interval']).toBe('Interval');
	});

	test('TC2 — E2E Tab cấp trên trong Bảng đăng ký (subscriptionTable.vue) và Modal Đăng ký chia sẻ (editSubscription.vue)', async ({ context, page }) => {
		test.slow();
		test.setTimeout(120_000);
		const mockToken = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjQxMDI0NDQ4MDAsIm5hbWUiOiJhZG1pbiJ9.mock_signature';
		await context.addCookies([{ name: 'token', value: mockToken, url: 'http://localhost:8888' }]);

		await page.addInitScript((token) => {
			document.cookie = `token=${token}; path=/;`;
			const pkg = 'tacwebcore';
			window.localStorage.setItem(`${pkg}:access-token`, JSON.stringify(token));
			window.sessionStorage.setItem(`${pkg}:token`, JSON.stringify(token));
			window.sessionStorage.setItem(`${pkg}:userInfo`, JSON.stringify({
				account: 'admin',
				realName: 'Administrator',
				accountType: '111',
				roles: ['admin'],
				authBtnList: ['*', 'sharedataSharing:add', 'sharedataSharing:update', 'sharedataSharing:delete'],
				defaultMenu: '/sharedata/sharing',
			}));
		}, mockToken);

		const mockUserInfo = {
			code: 200,
			result: {
				account: 'admin',
				realName: 'Administrator',
				accountType: '111',
				roles: ['admin'],
				authBtnList: ['*', 'sharedataSharing:add', 'sharedataSharing:update', 'sharedataSharing:delete'],
				defaultMenu: '/sharedata/sharing',
			},
		};

		await page.route('**/api/**', async (route) => {
			await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ code: 200, result: [] }) });
		});
		await page.route('**/api/system/sysauth/userinfo*', async (route) => {
			await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(mockUserInfo) });
		});
		await page.route('**/api/system/sysuser/info*', async (route) => {
			await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(mockUserInfo) });
		});
		await page.route('**/api/system/sysconfig/sysinfo*', async (route) => {
			await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ code: 200, result: {} }) });
		});
		await page.route('**/api/system/sysconfig/loadclientterm*', async (route) => {
			await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ code: 200, result: {} }) });
		});
		await page.route('**/api/cfgsystem/sysopconfig/alldatalist*', async (route) => {
			await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ code: 200, result: {} }) });
		});
		await page.route('**/api/cfgsystem/sysconfigtype/alldatalist*', async (route) => {
			await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ code: 200, result: {} }) });
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
									meta: { title: 'lz.menu.sharedataSharing', isLink: '', isHide: false, isKeepAlive: true, isAffix: false, isIframe: false, roles: ['admin'], icon: 'ele-Share' },
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
					result: [{ id: 'PARTNER_01', code: 'CSGT01', name: 'Cục CSGT Đường bộ', address: '10.10.1.25', port: 8443, status: 1, sessionState: 1 }],
				}),
			});
		});
		await page.route('**/api/sharedata/sharedatapacket/list*', async (route) => {
			await route.fulfill({
				status: 200,
				contentType: 'application/json',
				body: JSON.stringify({
					code: 200,
					result: [
						{ id: 'PKT_101', code: '101_commonData', name: 'Dữ liệu giao thông 101' },
						{ id: 'PKT_102', code: '102_eventData', name: 'Dữ liệu sự kiện 102' },
					],
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
						{ id: 'SUB_OUT_1', partnerId: 'PARTNER_01', datatypeId: 'PKT_101', direction: 0, mode: 1, state: 1, mappingState: 1, mappingName: 'Ánh xạ Gửi 101', schedule: { kind: 'continuous', intervalSeconds: 60 } },
						{ id: 'SUB_IN_1', partnerId: 'PARTNER_01', datatypeId: 'PKT_102', direction: 1, mode: 1, state: 1, mappingState: 1, mappingName: 'Ánh xạ Nhận 102' },
					],
				}),
			});
		});

		await page.goto('/#/sharedata/sharing');

		const subTabs = page.locator('.sub-direction-tabs');
		await expect(subTabs).toBeVisible({ timeout: 60000 });

		const outboundTab = subTabs.getByRole('tab', { name: 'Gửi đi' });
		const inboundTab = subTabs.getByRole('tab', { name: 'Nhận về' });
		await expect(outboundTab).toHaveClass(/is-active/);

		// Hàm đọc toạ độ dịch chuyển translateX thực tế của thanh active bar (.el-tabs__active-bar)
		const getTransformX = async (barLocator: ReturnType<typeof page.locator>) => {
			return await barLocator.evaluate((el) => {
				const style = window.getComputedStyle(el);
				return new DOMMatrix(style.transform).m41;
			});
		};

		// 1. Kiểm tra thanh active bar trên bảng: trượt di chuyển native translateX theo chuẩn TMS
		const subActiveBar = subTabs.locator('.el-tabs__active-bar');
		await expect(subActiveBar).toBeVisible();
		const subX1 = await getTransformX(subActiveBar);
		expect(subX1).toBe(0);

		await expect(page.locator('.loading-next')).toHaveCount(0, { timeout: 10000 });
		await page.waitForTimeout(300);

		// Chụp snapshot màn hình Chia sẻ dữ liệu với Tab cấp trên Gửi đi / Nhận về
		await page.screenshot({ path: 'test-results/snapshot-1-sharing-subscription-tabs.png', fullPage: false });

		// Chuyển sang tab Nhận về trên bảng: thanh active bar BẮT BUỘC di chuyển (move / translateX)
		await inboundTab.click();
		await expect(inboundTab).toHaveClass(/is-active/);
		await page.waitForTimeout(300);
		const subX2 = await getTransformX(subActiveBar);
		expect(subX2).toBeGreaterThan(50);

		// Bấm nút mở Modal Đăng ký chia sẻ
		const addBtn = page.locator('.sub-table .vxe-button--item button, .sub-table .vxe-toolbar button').first();
		await addBtn.click();

		const dialog = page.locator('.sharedata-edit-subscription-container .el-dialog');
		await expect(dialog).toBeVisible({ timeout: 5000 });

		// 2. Kiểm tra thanh active bar trong Modal (editSubscription.vue): trượt di chuyển native translateX chuẩn TMS
		const modalTabs = dialog.locator('.direction-tabs');
		const modalActiveBar = modalTabs.locator('.el-tabs__active-bar');
		await expect(modalActiveBar).toBeVisible();

		const modalInboundTab = modalTabs.getByRole('tab', { name: 'Nhận về' });
		await expect(modalInboundTab).toHaveClass(/is-active/);
		await page.waitForTimeout(400);
		await expect.poll(async () => getTransformX(modalActiveBar)).toBeGreaterThan(50);

		// Chuyển về tab Gửi đi trong modal: thanh active bar BẮT BUỘC di chuyển (move / translateX) về 0
		const modalOutboundTab = modalTabs.getByRole('tab', { name: 'Gửi đi' });
		await modalOutboundTab.click();
		await expect(modalOutboundTab).toHaveClass(/is-active/);
		await page.waitForTimeout(400);
		await expect.poll(async () => getTransformX(modalActiveBar)).toBeLessThanOrEqual(1);

		// Kiểm tra chiều cao cố định theo tỷ lệ màn hình (vh) không bị giật khi chuyển tab
		const tabContent = dialog.locator('.direction-tabs .el-tabs__content');
		await expect(tabContent).toBeVisible();
		const heightOutbound = await tabContent.evaluate((el) => el.getBoundingClientRect().height);
		expect(heightOutbound).toBeGreaterThanOrEqual(340);
		expect(heightOutbound).toBeLessThanOrEqual(400);

		// Chuyển lại sang tab Nhận về và đo chiều cao
		await modalInboundTab.click();
		await expect(modalInboundTab).toHaveClass(/is-active/);
		await page.waitForTimeout(200);
		const heightInbound = await tabContent.evaluate((el) => el.getBoundingClientRect().height);

		// Xác minh chiều cao giữa tab Gửi đi và Nhận về đồng nhất tuyệt đối, không bị giật/nhảy layout
		expect(Math.abs(heightOutbound - heightInbound)).toBeLessThanOrEqual(1);

		// Chuyển lại tab Gửi đi để chụp snapshot
		await modalOutboundTab.click();
		await expect(modalOutboundTab).toHaveClass(/is-active/);
		await page.waitForTimeout(200);

		// Kiểm tra ô Ghi chú (remark) hiển thị mặc định với rows="4" (đã giảm 2 dòng so với 6)
		const remarkTextarea = dialog.locator('textarea').first();
		await expect(remarkTextarea).toBeVisible();
		const rowsAttr = await remarkTextarea.getAttribute('rows');
		expect(rowsAttr).toBe('4');
		const textareaHeight = await remarkTextarea.evaluate((el) => el.getBoundingClientRect().height);
		expect(textareaHeight).toBeGreaterThanOrEqual(70);

		// Kiểm tra el-dialog__footer có khoảng đệm chuẩn và thoáng (padding-bottom >= 12px) để nút bấm không dính sát đáy dialog
		const dialogFooter = dialog.locator('.el-dialog__footer');
		await expect(dialogFooter).toBeVisible();
		const footerPadding = await dialogFooter.evaluate((el) => {
			const cs = window.getComputedStyle(el);
			return {
				top: parseFloat(cs.paddingTop),
				bottom: parseFloat(cs.paddingBottom),
				left: parseFloat(cs.paddingLeft),
				right: parseFloat(cs.paddingRight),
			};
		});
		expect(footerPadding.bottom).toBeGreaterThanOrEqual(12);
		expect(footerPadding.top).toBeGreaterThanOrEqual(12);

		// Kiểm tra bounding rect nút bấm footer ('Bỏ qua', 'Đăng ký') cách mép dưới dialog >= 12px
		const footerBtn = dialogFooter.locator('button').first();
		await expect(footerBtn).toBeVisible();
		const btnBox = await footerBtn.boundingBox();
		const dialogBox = await dialog.boundingBox();
		if (btnBox && dialogBox) {
			const bottomDistance = (dialogBox.y + dialogBox.height) - (btnBox.y + btnBox.height);
			expect(bottomDistance).toBeGreaterThanOrEqual(12);
		}

		await page.screenshot({ path: 'test-results/snapshot-2-modal-edit-subscription-tabs.png', fullPage: false });
	});
});
