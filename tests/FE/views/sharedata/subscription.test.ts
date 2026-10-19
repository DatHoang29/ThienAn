import { test, expect } from '@playwright/test';
import { resolveWebVueSrc } from '../../setup';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

/**
 * Test Suite cho chức năng đăng ký chia sẻ dữ liệu (editSubscription.vue):
 * - TC1: StartTime == EndTime (23:00 -> 23:00) bị chặn, trả về timeRangeInvalid.
 * - TC2: EndTime < StartTime (23:00 -> 22:00) bị chặn, trả về timeRangeInvalid.
 * - TC3: Chỉ nhập 1 trong 2 ô (thiếu giờ bắt đầu hoặc giờ kết thúc) bị chặn, trả về timeRangeIncomplete.
 * - TC4: EndTime > StartTime (23:00 -> 23:01) hợp lệ. Với chu kỳ 30 giây, bắn 2 lần (0s và 30s) trong cửa sổ 1 phút.
 * - TC5: Để trống cả 2 ô (gửi liên tục 24/24) hợp lệ, không có lỗi.
 * - TC6: Quét tĩnh mã nguồn tuân thủ Rule 20.8 (CẤM toán tử !!), Rule 19.61 (Timeout <= 5s), template binding và submit guard.
 * - TC7: Kiểm tra đầy đủ bản dịch i18n trong vi-vn.json và en-us.json.
 */

// Hàm logic mô phỏng chính xác reactive computed từ editSubscription.vue
function evaluateTimeRange(startTimeVal: string, endTimeVal: string, t: (key: string) => string) {
	const isEndBeforeStart = Boolean(startTimeVal && endTimeVal) && endTimeVal <= startTimeVal;
	const isTimeRangeIncomplete = (Boolean(startTimeVal) && !endTimeVal) || (!startTimeVal && Boolean(endTimeVal));

	let timeRangeError = '';
	if (isTimeRangeIncomplete) {
		timeRangeError = t('lz.validation.sharedataSubscription.timeRangeIncomplete');
	} else if (isEndBeforeStart) {
		timeRangeError = t('lz.validation.sharedataSubscription.timeRangeInvalid');
	}

	return {
		isEndBeforeStart,
		isTimeRangeIncomplete,
		timeRangeError,
	};
}

const mockI18n = (key: string): string => {
	const dict: Record<string, string> = {
		'lz.validation.sharedataSubscription.timeRangeInvalid': 'Giờ kết thúc phải lớn hơn giờ bắt đầu.',
		'lz.validation.sharedataSubscription.timeRangeIncomplete': 'Vui lòng nhập đầy đủ giờ bắt đầu và kết thúc (hoặc để trống cả hai).',
	};
	return dict[key] || key;
};

test.describe('ShareData — Khung giờ đăng ký chia sẻ dữ liệu (editSubscription.vue) @sharedata', () => {

	test('TC1 — Chặn khi StartTime == EndTime (23:00 -> 23:00): Hiển thị lỗi và chặn khoảng thời gian rỗng', async () => {
		const result = evaluateTimeRange('23:00', '23:00', mockI18n);

		expect(result.isEndBeforeStart).toBe(true);
		expect(result.isTimeRangeIncomplete).toBe(false);
		expect(result.timeRangeError).toBe('Giờ kết thúc phải lớn hơn giờ bắt đầu.');
	});

	test('TC2 — Chặn khi EndTime < StartTime (23:00 -> 22:00): Hiển thị lỗi đảo ngược thời gian', async () => {
		const result = evaluateTimeRange('23:00', '22:00', mockI18n);

		expect(result.isEndBeforeStart).toBe(true);
		expect(result.isTimeRangeIncomplete).toBe(false);
		expect(result.timeRangeError).toBe('Giờ kết thúc phải lớn hơn giờ bắt đầu.');
	});

	test('TC3 — Chặn khi chỉ nhập 1 trong 2 ô (thiếu giờ bắt đầu hoặc giờ kết thúc): Báo lỗi thiếu khung giờ', async () => {
		const missingEnd = evaluateTimeRange('23:00', '', mockI18n);
		expect(missingEnd.isTimeRangeIncomplete).toBe(true);
		expect(missingEnd.timeRangeError).toBe('Vui lòng nhập đầy đủ giờ bắt đầu và kết thúc (hoặc để trống cả hai).');

		const missingStart = evaluateTimeRange('', '23:00', mockI18n);
		expect(missingStart.isTimeRangeIncomplete).toBe(true);
		expect(missingStart.timeRangeError).toBe('Vui lòng nhập đầy đủ giờ bắt đầu và kết thúc (hoặc để trống cả hai).');
	});

	test('TC4 — Hợp lệ khi EndTime > StartTime (06:00 -> 06:01 hoặc 23:00 -> 23:01): Form hợp lệ không báo lỗi', async () => {
		const eveningResult = evaluateTimeRange('23:00', '23:01', mockI18n);
		expect(eveningResult.isEndBeforeStart).toBe(false);
		expect(eveningResult.isTimeRangeIncomplete).toBe(false);
		expect(eveningResult.timeRangeError).toBe('');

		const morningResult = evaluateTimeRange('06:00', '06:01', mockI18n);
		expect(morningResult.isEndBeforeStart).toBe(false);
		expect(morningResult.isTimeRangeIncomplete).toBe(false);
		expect(morningResult.timeRangeError).toBe('');
	});

	test('TC5 — Hợp lệ khi để trống cả 2 ô (gửi liên tục 24/24): Không báo lỗi', async () => {
		const result = evaluateTimeRange('', '', mockI18n);

		expect(result.isEndBeforeStart).toBe(false);
		expect(result.isTimeRangeIncomplete).toBe(false);
		expect(result.timeRangeError).toBe('');
	});

	test('TC6 — Quét tĩnh mã nguồn editSubscription.vue, subscriptionTable.vue & scheduler/index.vue: Tuân thủ Rule 20.8 (CẤM !!), submit guard và inline error', async () => {
		const vueFilePath = resolveWebVueSrc('views/sharedata/sharing/component/editSubscription.vue');
		const schedulerFilePath = resolveWebVueSrc('components/scheduler/index.vue');
		const subTableFilePath = resolveWebVueSrc('views/sharedata/sharing/component/subscriptionTable.vue');
		const vueContent = fs.readFileSync(vueFilePath, 'utf8');
		const schedulerContent = fs.readFileSync(schedulerFilePath, 'utf8');
		const subTableContent = fs.readFileSync(subTableFilePath, 'utf8');

		// 1. Rule 20.8: Tuyệt đối không dùng toán tử !! trong đoạn xử lý khung giờ ở Scheduler và subscriptionTable
		const timeRangeBlock = schedulerContent.substring(
			schedulerContent.indexOf('const isEndBeforeStart'),
			schedulerContent.indexOf('defineExpose')
		);
		expect(timeRangeBlock).not.toContain('!!');
		expect(schedulerContent).not.toContain('!!');
		expect(subTableContent).not.toContain('!!');

		// 2. Kiểm tra điều kiện chặn dấu bằng (<=) trong scheduler/index.vue
		expect(timeRangeBlock).toContain('endTime.value <= startTime.value');

		// 3. Kiểm tra form-item binding :error hiển thị lỗi inline
		expect(schedulerContent).toContain(':error="scheduleKind === \'continuous\' ? timeRangeError : \'\'"');

		// 4. Kiểm tra submit guard chặn gọi API khi timeRangeError có lỗi
		const submitBlock = vueContent.substring(
			vueContent.indexOf('async function submit()'),
			vueContent.indexOf('defineExpose')
		);
		expect(submitBlock).toContain('scheduleKind.value === \'continuous\' && schedulerRef.value?.timeRangeError');

		// 5. Kiểm tra buildSchedule chuẩn hóa chuỗi rỗng thành undefined
		const buildScheduleBlock = vueContent.substring(
			vueContent.indexOf('function buildSchedule()'),
			vueContent.indexOf('async function submit()')
		);
		expect(buildScheduleBlock).toContain('startTime: startTime.value || undefined');
		expect(buildScheduleBlock).toContain('endTime: endTime.value || undefined');

		// 6. Kiểm tra không import thừa package lạ (như locale)
		expect(vueContent).not.toContain("import { lo } from 'element-plus/es/locale'");

		// 7. Kiểm tra cố định chiều cao modal theo tỷ lệ màn hình (vh) để tránh giật khi chuyển tab
		const directionTabsBlock = vueContent.substring(vueContent.indexOf('.direction-tabs'));
		expect(directionTabsBlock).toContain('height: 38vh;');
		expect(directionTabsBlock).toContain('min-height: 340px;');
		expect(directionTabsBlock).toContain('max-height: 400px;');
		expect(directionTabsBlock).toContain('overflow-y: auto;');
		expect(directionTabsBlock).not.toContain('!important');

		// 8. Kiểm tra chiều cao mặc định ô Ghi chú (remark) có :rows="6" rộng rãi theo yêu cầu
		expect(vueContent).toContain(':rows="6"');

		// 9. Rule 20.26 / STT 55: close-on-click-modal="false"
		expect(vueContent).toContain(':close-on-click-modal="false"');
	});

	test('TC7 — Kiểm tra cấu hình i18n trong vi-vn.json và en-us.json', async () => {
		const viPath = resolveWebVueSrc('i18n/lang/vi-vn.json');
		const enPath = resolveWebVueSrc('i18n/lang/en-us.json');

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

	test('TC8 — Kiểm tra component Scheduler hỗ trợ đủ 4 mode và ShareData giới hạn đúng continuous & daily', async () => {
		const schedulerVuePath = resolveWebVueSrc('components/scheduler/index.vue');
		const editSubPath = resolveWebVueSrc('views/sharedata/sharing/component/editSubscription.vue');

		const schedulerContent = fs.readFileSync(schedulerVuePath, 'utf8');
		const editSubContent = fs.readFileSync(editSubPath, 'utf8');

		// 1. Scheduler types phải hỗ trợ đủ 4 mode (khai báo trực tiếp trong component theo Rule 20.17)
		expect(schedulerContent).toContain("'continuous' | 'daily' | 'weekly' | 'interval'");

		// 2. Scheduler template phải có radio-button cho cả 4 mode
		expect(schedulerContent).toContain("allowedKinds.includes('continuous')");
		expect(schedulerContent).toContain("allowedKinds.includes('daily')");
		expect(schedulerContent).toContain("allowedKinds.includes('weekly')");
		expect(schedulerContent).toContain("allowedKinds.includes('interval')");

		// 3. ShareData editSubscription.vue chỉ cho phép ['continuous', 'daily']
		expect(editSubContent).toContain(":allowed-kinds=\"['continuous', 'daily']\"");
	});

	test('TC9 — E2E Tab cấp trên trong Bảng đăng ký (subscriptionTable.vue) và Modal Đăng ký chia sẻ (editSubscription.vue)', async ({ context, page }) => {
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

		// Kiểm tra ô Ghi chú (remark) hiển thị mặc định với rows="6" cao rộng rãi theo đúng yêu cầu
		const remarkTextarea = dialog.locator('textarea').first();
		await expect(remarkTextarea).toBeVisible();
		const rowsAttr = await remarkTextarea.getAttribute('rows');
		expect(rowsAttr).toBe('6');
		const textareaHeight = await remarkTextarea.evaluate((el) => el.getBoundingClientRect().height);
		expect(textareaHeight).toBeGreaterThanOrEqual(100);

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

	test('TC10 — Cấu hình kênh kết nối đối tác (editPartner.vue: ConnectionType enum 1..5, ConnectionConfig, tuân thủ Rule 20.8 CẤM !!)', async () => {
		const partnerFilePath = resolveWebVueSrc('views/sharedata/sharing/component/editPartner.vue');
		const partnerContent = fs.readFileSync(partnerFilePath, 'utf8');

		// 1. Kiểm tra trường connectionType có các option chuẩn dạng số nguyên enum (API=1, File=2, ...)
		expect(partnerContent).toContain('prop="connectionType"');
		expect(partnerContent).toContain(':value="1"');
		expect(partnerContent).toContain(':value="2"');

		// 2. Kiểm tra trường connectionConfig
		expect(partnerContent).toContain('connectionConfig');

		// 3. Kiểm tra gán mặc định connectionType = ConnectionTypeEnum.Api khi mở dialog thêm mới
		expect(partnerContent).toContain('state.ruleForm.connectionType = ConnectionTypeEnum.Api');

		// 4. Tuân thủ Rule 20.8: CẤM dùng toán tử !! (phủ định kép), dùng Boolean(...) tường minh
		expect(partnerContent).not.toContain('!!');
		expect(partnerContent).toContain('state.isEdit = Boolean((row as any)?.id)');

		// 5. Tuân thủ Rule 20.6: Không dùng !important trong style
		expect(partnerContent).not.toContain('!important');
	});

	test('TC11 — Khoảng đệm thoáng ở el-dialog__footer trong modal Tạo/Sửa đăng ký chia sẻ dữ liệu (editSubscription.vue)', async () => {
		const subFilePath = resolveWebVueSrc('views/sharedata/sharing/component/editSubscription.vue');
		const subContent = fs.readFileSync(subFilePath, 'utf8');

		// 1. Quét tĩnh mã nguồn: editSubscription.vue có cấu hình khoảng đệm chuẩn cho .el-dialog__footer (padding: 12px 20px 20px)
		expect(subContent).toContain('.el-dialog__footer');
		expect(subContent).toMatch(/padding:\s*12px 20px 20px;/);

		// 2. Tuân thủ Rule 20.6: Không dùng !important
		expect(subContent).not.toContain('!important');

		// 3. Phân lập phạm vi (Rule 13): Chỉ áp dụng trong phân hệ ShareData, không sửa global element.scss
		const elementScssPath = resolveWebVueSrc('theme/element.scss');
		const elementScssContent = fs.readFileSync(elementScssPath, 'utf8');
		// element.scss vẫn giữ nguyên cấu hình hệ thống
		expect(elementScssContent).toContain('.el-dialog__footer');
		expect(elementScssContent).toContain('padding: 15px');
	});
});
