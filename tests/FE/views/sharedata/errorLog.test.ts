import { test, expect } from '@playwright/test';
import { mockAuth } from '../../setup';

/**
 * Test E2E cho phân hệ ShareData — Nhật ký cảnh báo lỗi (errorLog/index.vue)
 * và Drawer chi tiết cảnh báo (alertDetailDrawer.vue):
 * - Cột Mô tả mã lỗi tra cứu từ danh mục mã lỗi ESH.
 * - Drawer chi tiết hiển thị đầy đủ Mô tả mã lỗi và Hướng xử lý.
 */
test.describe('ShareData — Nhật ký cảnh báo lỗi (errorLog) @sharedata', () => {

    test.beforeEach(async ({ context, page }) => {
        await mockAuth(context, page, {
            userInfo: {
                defaultMenu: '/sharedata/errorLog',
            },
        });

        // Mock API Login Menu Tree chứa route cho errorLog
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
                            meta: {
                                title: 'lz.menu.sharedata',
                                isLink: '',
                                isHide: false,
                                isKeepAlive: true,
                                isAffix: false,
                                isIframe: false,
                                roles: ['admin'],
                                icon: 'ele-Share',
                            },
                            children: [
                                {
                                    id: 'menu_sharedata_errorlog',
                                    pid: 'menu_sharedata',
                                    path: '/sharedata/errorLog',
                                    name: 'sharedataErrorLog',
                                    component: 'sharedata/errorLog/index',
                                    meta: {
                                        title: 'lz.menu.sharedataErrorLog',
                                        isLink: '',
                                        isHide: false,
                                        isKeepAlive: true,
                                        isAffix: false,
                                        isIframe: false,
                                        roles: ['admin'],
                                        icon: 'ele-Warning',
                                    },
                                },
                            ],
                        },
                    ],
                }),
            });
        });
    });

    test('1. Hiển thị bảng nhật ký lỗi với cột Mô tả mã lỗi ESH và mở Drawer chi tiết hiển thị Hướng xử lý', async ({ page }) => {
        // Mock API trả về danh mục cấu hình hệ thống (BaseConfig) cho sharedata_error_code
        await page.route('**/api/cfgsystem/sysconfigtype/alldatalist*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        sharedata_error_code: [
                            {
                                code: 'ESH-1201',
                                name: 'Không tìm thấy bộ mã CodeSet',
                                remark: 'Kiểm tra bảng ShareDataCodeSet và gán bộ mã cho trường dữ liệu',
                                status: 1,
                            },
                        ],
                    },
                }),
            });
        });

        // Mock API danh sách đối tác
        await page.route('**/api/sharedata/sharedatapartner/list*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: [
                        { id: 'PARTNER_01', code: 'CSGT', name: 'Đối tác Cục CSGT' },
                    ],
                }),
            });
        });

        // Mock API danh sách gói tin
        await page.route('**/api/sharedata/sharedatapacket/list*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: [
                        { id: 'PKT_01', code: '101', name: '101_commonData' },
                    ],
                }),
            });
        });

        // Mock API summary
        await page.route('**/api/sharedata/sharedataalertlog/summary*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        unackedCount: 1,
                        totalCount: 1,
                    },
                }),
            });
        });

        // Mock API phân trang Nhật ký cảnh báo lỗi
        await page.route('**/api/sharedata/sharedataalertlog/page*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        records: [
                            {
                                id: 'ALERT_ROW_1',
                                alertCode: 'ESH-1201',
                                alertSource: 1,
                                severity: 2,
                                partnerId: 'PARTNER_01',
                                datatypeId: '101',
                                message: 'CodeSet CS_STATUS not found for field status',
                                occurredAt: '2026-10-14T08:00:00Z',
                            },
                        ],
                        total: 1,
                    },
                }),
            });
        });

        await page.goto('/sharedata/errorLog');

        // Bảng dữ liệu phải hiển thị
        const grid = page.locator('.full-table');
        await expect(grid).toBeVisible({ timeout: 15_000 });

        // Khẳng định mã lỗi và mô tả mã lỗi hiển thị trên hàng
        const alertCodeCell = page.locator('.vxe-body--row:has-text("ESH-1201")');
        await expect(alertCodeCell).toBeVisible();
        await expect(alertCodeCell).toContainText('Không tìm thấy bộ mã CodeSet');

        // Bấm nút Chi tiết ở cột cố định bên phải để mở Drawer
        const detailBtn = page.locator('.vxe-table--fixed-right-wrapper button').first();
        await expect(detailBtn).toBeVisible();
        await detailBtn.click();

        // Drawer chi tiết mở ra
        const drawer = page.locator('.el-drawer.open');
        await expect(drawer).toBeVisible({ timeout: 5000 });

        // Khẳng định Drawer hiển thị đầy đủ thông tin: Mã lỗi, Mô tả mã lỗi, Hướng xử lý, Nội dung
        await expect(drawer.locator('.el-descriptions')).toContainText('ESH-1201');
        await expect(drawer.locator('.el-descriptions')).toContainText('Không tìm thấy bộ mã CodeSet');
        await expect(drawer.locator('.el-descriptions')).toContainText('Kiểm tra bảng ShareDataCodeSet');
        await expect(drawer.locator('.el-descriptions')).toContainText('CodeSet CS_STATUS not found');

        // Rule 20.26: bấm ra ngoài lớp phủ (tọa độ 5, 5) thì Drawer đóng
        await page.mouse.click(5, 5);
        await expect(drawer).toBeHidden();

        // Mở lại rồi đóng bằng nút đóng
        await detailBtn.click();
        await expect(drawer).toBeVisible({ timeout: 5000 });
        await drawer.locator('.el-drawer__close-btn').click();
        await expect(drawer).toBeHidden();
    });

    test('2. Bộ lọc Thời gian giống Lịch sử chia sẻ: chọn ngày giờ DD/MM/YYYY HH:mm:ss, mặc định từ 00:00:00 tới giờ hiện tại và gửi đúng giờ đã chọn', async ({ page }) => {
        await page.clock.setFixedTime(new Date(2026, 9, 27, 20, 54, 48));
        const ok = (result: unknown) => ({ status: 200, contentType: 'application/json', body: JSON.stringify({ code: 200, result }) });
        await page.route('**/api/cfgsystem/sysconfigtype/alldatalist*', (r) => r.fulfill(ok({})));
        await page.route('**/api/sharedata/sharedatapartner/list*', (r) => r.fulfill(ok([])));
        await page.route('**/api/sharedata/sharedataalertlog/summary*', (r) => r.fulfill(ok({ unackedCount: 0, totalCount: 0 })));
        const pageBodies: any[] = [];
        await page.route('**/api/sharedata/sharedataalertlog/page*', (r) => {
            pageBodies.push(r.request().postDataJSON());
            return r.fulfill(ok({ records: [], total: 0 }));
        });

        await page.goto('/sharedata/errorLog');

        const inputs = page.locator('.el-date-editor input');
        await expect(inputs).toHaveCount(2, { timeout: 30_000 });
        const [from, to] = [await inputs.nth(0).inputValue(), await inputs.nth(1).inputValue()];
        expect(from).toMatch(/^\d{2}\/\d{2}\/\d{4} 00:00:00$/);
        expect(to).toBe('27/10/2026 20:54:48');

        await expect.poll(() => pageBodies.length, { timeout: 15_000 }).toBeGreaterThan(0);
        const toDate = new Date(pageBodies[0].toDate);
        expect([toDate.getHours(), toDate.getMinutes(), toDate.getSeconds()]).toEqual([20, 54, 48]);
    });


    test('3. Bộ lọc Thời gian hiện đủ ngày giờ DD/MM/YYYY HH:mm:ss ở cả hai ô, không bị khuất ở mọi độ rộng màn hình từ 768px tới 1920px', async ({ page }) => {
        const ok = (result: unknown) => ({ status: 200, contentType: 'application/json', body: JSON.stringify({ code: 200, result }) });
        await page.route('**/api/cfgsystem/sysconfigtype/alldatalist*', (r) => r.fulfill(ok({})));
        await page.route('**/api/sharedata/sharedatapartner/list*', (r) => r.fulfill(ok([])));
        await page.route('**/api/sharedata/sharedataalertlog/summary*', (r) => r.fulfill(ok({ unackedCount: 0, totalCount: 0 })));
        await page.route('**/api/sharedata/sharedataalertlog/page*', (r) => r.fulfill(ok({ records: [], total: 0 })));
        await page.goto('/sharedata/errorLog');
        for (const width of [1920, 1600, 1440, 1366, 1280, 1024, 768]) {
            await page.setViewportSize({ width, height: 900 });
            await page.waitForTimeout(300);
            const inputs = page.locator('.el-date-editor .el-range-input');
            await expect(inputs).toHaveCount(2, { timeout: 30_000 });
            for (let i = 0; i < 2; i++) {
                const { value, scroll, client } = await inputs.nth(i).evaluate((el: HTMLInputElement) => ({ value: el.value, scroll: el.scrollWidth, client: el.clientWidth }));
                expect(value, `ô ${i + 1} ở ${width}px`).toMatch(/^\d{2}\/\d{2}\/\d{4} \d{2}:\d{2}:\d{2}$/);
                expect(scroll, `ô ${i + 1} ở ${width}px bị cắt chữ "${value}"`).toBeLessThanOrEqual(client);
            }
            // Các ô lọc khác (chọn / nhập) không bị bóp: đủ rộng và chữ gợi ý không bị cắt thành "T.."
            const narrow = await page.locator('.el-form').first().evaluate((form) =>
                [...form.querySelectorAll('.el-select__wrapper, .el-input__wrapper:not(.el-range-editor)')]
                    .filter((el) => !el.closest('.el-date-editor'))
                    .map((el) => {
                        const ph = el.querySelector('.el-select__placeholder span, .el-select__placeholder') as HTMLElement | null;
                        const input = el.querySelector('input.el-input__inner') as HTMLInputElement | null;
                        const ctx = document.createElement('canvas').getContext('2d')!;
                        if (input) { const cs = getComputedStyle(input); ctx.font = `${cs.fontWeight} ${cs.fontSize} ${cs.fontFamily}`; }
                        const inputCut = !ph && !!input?.placeholder && ctx.measureText(input.placeholder).width > input.clientWidth - 2;
                        return { w: Math.round(el.getBoundingClientRect().width), cut: (ph ? ph.scrollWidth > ph.clientWidth + 1 : false) || inputCut, text: ph?.textContent?.trim() ?? (el.querySelector('input') as HTMLInputElement | null)?.placeholder };
                    })
                    .filter((x) => x.w < 120 || x.cut)
            );
            expect(narrow, `ô lọc bị bóp ở ${width}px`).toEqual([]);
        }
    });

});
