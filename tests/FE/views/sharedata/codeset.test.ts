import { test, expect, type BrowserContext, type Page } from '@playwright/test';
import { mockAuth, readVxeColumnAligns, expectedColumnAligns } from '../../setup';

/**
 * Test E2E cho Dialog "Chỉnh sửa bộ mã" (editCodeSet.vue)
 * Phân hệ ShareData > CodeSet
 *
 * Kiểm tra lỗi UI ở dòng "Giá trị mặc định":
 * - .cs-default-hint xuống dòng tự nhiên (white-space: normal, display: block).
 * - Dòng mặc định .cs-default-row tự giãn chiều cao để hiển thị trọn vẹn cả câu, không bị cắt nửa chữ.
 * - Cuộn toàn bộ modal ở cấp form ngoài (el-form), không tạo thanh scroll con thừa trong vxe-table.
 * - Hỗ trợ cả tiếng Việt lẫn tiếng Anh không bị cắt chữ.
 */

test.describe('ShareData — Dialog Chỉnh sửa bộ mã (editCodeSet.vue) @sharedata', () => {

    test.describe('E2E Runtime Check', () => {
        const setupCodeSetMock = async (context: BrowserContext, page: Page, lang = 'vi-vn') => {
            await mockAuth(context, page, {
                language: lang,
                userInfo: {
                    authBtnList: ['*', 'eshCanonical:add', 'eshCanonical:update', 'eshCanonical:delete', 'eshCanonical:page'],
                    defaultMenu: '/sharedata/codeSet',
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
                                        id: 'menu_sharedata_codeset',
                                        pid: 'menu_sharedata',
                                        path: '/sharedata/codeSet',
                                        name: 'eshCanonical',
                                        component: 'sharedata/codeSet/index',
                                        meta: { title: 'lz.menu.sharedataCodeSet', isLink: '', isHide: false, isKeepAlive: true, isAffix: false, isIframe: false, roles: ['admin'], icon: 'ele-Folder' },
                                    },
                                ],
                            },
                        ],
                    }),
                });
            });

            await page.route('**/api/sharedata/sharedatacodeset/page*', async (route) => {
                await route.fulfill({
                    status: 200,
                    contentType: 'application/json',
                    body: JSON.stringify({
                        code: 200,
                        result: {
                            items: [
                                {
                                    id: 1001,
                                    code: 'CS_01',
                                    name: 'Bộ mã kiểm thử',
                                    scope: 1,
                                    valueCount: 1,
                                    values: [{ sourceValue: 'V1', partnerValue: 'P1', displayName: 'D1' }],
                                },
                            ],
                            total: 1,
                        },
                    }),
                });
            });

            await page.route('**/api/sharedata/sharedatacodeset/byid*', async (route) => {
                await route.fulfill({
                    status: 200,
                    contentType: 'application/json',
                    body: JSON.stringify({
                        code: 200,
                        result: {
                            id: 1001,
                            code: 'CS_01',
                            name: 'Bộ mã kiểm thử',
                            scope: 1,
                            defaultSourceValue: '',
                            defaultPartnerValue: '',
                            values: [{ sourceValue: 'V1', partnerValue: 'P1', displayName: 'D1' }],
                        },
                    }),
                });
            });
        };

        test.beforeEach(async () => {
            test.setTimeout(120_000);
        });

        const langCases = [
            { name: 'tiếng Việt', lang: 'vi-vn' },
            { name: 'tiếng Anh', lang: 'en-us' },
        ];

        for (const c of langCases) {
            test(`1. Kiểm tra giao diện ${c.name}: tự xuống dòng tăng chiều cao hàng, hiển thị trọn vẹn`, async ({ context, page }) => {
                await setupCodeSetMock(context, page, c.lang);
                await page.goto('/#/sharedata/codeSet');

                // Chờ loading ban đầu của Vue/Vite biến mất
                await page.locator('.loading-next').waitFor({ state: 'detached', timeout: 30000 }).catch(() => {});

                // Mở dialog bằng nút Thêm mới trong toolbar của bảng
                const addButton = page.locator('button:has(.ele-Plus), .vxe-toolbar button, .el-button').filter({ hasText: /Thêm|Add/ }).first();
                await expect(addButton).toBeVisible({ timeout: 60000 });
                await addButton.click();

                // Chờ dialog xuất hiện
                const dialog = page.locator('.el-overlay:not([style*="display: none"]) .el-dialog');
                await expect(dialog).toBeVisible({ timeout: 10000 });

                // Tìm hàng mặc định .cs-default-row trong vxe-table
                const defaultRow = dialog.locator('.cs-default-row');
                await expect(defaultRow).toBeVisible({ timeout: 10000 });

                // Tìm phần tử .cs-default-hint
                const defaultHint = defaultRow.locator('.cs-default-hint');
                await expect(defaultHint).toBeVisible();

                // Đọc computed style
                const styles = await defaultHint.evaluate((el) => {
                    const cs = window.getComputedStyle(el);
                    return {
                        whiteSpace: cs.whiteSpace,
                        display: cs.display,
                        clientHeight: el.clientHeight,
                        scrollHeight: el.scrollHeight,
                    };
                });

                // Yêu cầu: whiteSpace normal, display block
                expect(styles.whiteSpace).toBe('normal');
                expect(styles.display).toBe('block');

                // Chiều cao tự giãn theo nội dung để hiển thị đủ 2 dòng mà không bị cắt nửa chữ
                expect(styles.clientHeight).toBeGreaterThanOrEqual(28);
                expect(styles.clientHeight).toBe(styles.scrollHeight);

                // Bảng con không bị giới hạn max-height=300px (không sinh thanh scroll riêng)
                const table = dialog.locator('.vxe-table.cs-values');
                await expect(table).toBeVisible();
                const tableMaxHeight = await table.evaluate((el) => window.getComputedStyle(el).maxHeight);
                expect(tableMaxHeight).not.toBe('300px');

                // Cấp ngoài el-form quản lý thanh cuộn duy nhất
                const form = dialog.locator('form');
                const formOverflowY = await form.evaluate((el) => window.getComputedStyle(el).overflowY);
                expect(['auto', 'scroll']).toContain(formOverflowY);
            });
        }

        test('2. Trang 50 dòng cuộn tới đáy thấy đủ dòng 50 ở cả cột cố định chọn và Hành động', async ({ context, page }) => {
            await setupCodeSetMock(context, page);
            const records = Array.from({ length: 50 }, (_, i) => ({
                id: `CS_${i + 1}`, code: `CS_TRANG_THAI_PHUONG_TIEN_QUA_TRAM_${i + 1}`,
                name: `Bộ mã trạng thái phương tiện qua trạm thu phí cửa khẩu Hữu Nghị - Chi Lăng số ${i + 1}`,
                scope: 1, valueCount: 12, createTime: '2026-11-13 14:58:17', createUserName: 'admin', updateTime: '2026-11-13 15:02:44', updateUserName: 'admin',
            }));
            await page.route('**/api/sharedata/sharedatacodeset/page*', (r) =>
                r.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ code: 200, result: { records, total: 2048 } }) })
            );
            /* Narrow screen: columns overflow horizontally, so vxe-table renders the fixed panes. */
            await page.setViewportSize({ width: 700, height: 620 });
            await page.goto('/#/sharedata/codeSet');

            const body = page.locator('.vxe-table--main-wrapper .vxe-table--body-wrapper').first();
            await expect(body.locator('tr.vxe-body--row')).toHaveCount(50, { timeout: 60_000 });
            const box = (await body.boundingBox())!;
            await page.mouse.move(box.x + box.width / 2, box.y + box.height / 2);
            for (let i = 0; i < 40; i++) await page.mouse.wheel(0, 400);
            await page.waitForTimeout(500);

            /* Check every body pane: main, fixed-left (checkbox) and fixed-right (Hành động). */
            const panes = await page.evaluate(() =>
                [...document.querySelectorAll('.full-table .vxe-table--body-wrapper')].map((el) => {
                    const b = el as HTMLElement;
                    const rows = b.querySelectorAll('tr.vxe-body--row');
                    const last = rows[rows.length - 1].getBoundingClientRect();
                    /* Visible area = intersection of the pane with every clipping ancestor. */
                    let bottom = b.getBoundingClientRect().bottom;
                    for (let p = b.parentElement; p; p = p.parentElement) {
                        if (getComputedStyle(p).overflowY !== 'visible') bottom = Math.min(bottom, p.getBoundingClientRect().bottom);
                    }
                    return { pane: b.className, hiddenPx: Math.round(last.bottom - bottom) };
                })
            );
            expect(panes.some((p) => p.pane.includes('fixed-'))).toBe(true);
            for (const p of panes) expect(p.hiddenPx, `dòng 50 bị che ở ${JSON.stringify(p)}`).toBeLessThanOrEqual(1);
        });

        test('3. Căn lề cột: tiêu đề giữa; Mã, Tên căn trái; số giá trị, thông tin, hành động căn giữa; bảng giá trị trong dialog có tiêu đề giữa', async ({ context, page }) => {
            await setupCodeSetMock(context, page);
            const records = [
                { id: 'CS_1', code: 'lane_code', name: 'Mã làn đường cao tốc', scope: 1, valueCount: 12 },
                { id: 'CS_2', code: 'vehicle_status_at_border_gate', name: 'Trạng thái', scope: 1, valueCount: 3 },
            ];
            await page.route('**/api/sharedata/sharedatacodeset/page*', (r) =>
                r.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ code: 200, result: { records, total: 2 } }) })
            );
            await page.setViewportSize({ width: 1600, height: 900 });
            await page.goto('/#/sharedata/codeSet');
            await expect(page.locator('.vxe-table--main-wrapper tr.vxe-body--row')).toHaveCount(2, { timeout: 60_000 });

            const grid = await readVxeColumnAligns(page, '.full-table');
            expect(grid).toEqual(expectedColumnAligns(['Mã', 'Tên'], ['', 'Số giá trị', 'Thông tin', 'Hành động']));

            await page.locator('.vxe-toolbar button, .el-button').filter({ hasText: /Thêm|Add/ }).first().click();
            const dialog = page.locator('.el-overlay:not([style*="display: none"]) .el-dialog');
            await expect(dialog.locator('.vxe-table.cs-values')).toBeVisible({ timeout: 10_000 });
            const values = await readVxeColumnAligns(page, '.el-overlay:not([style*="display: none"]) .el-dialog .vxe-table.cs-values');
            expect(Object.values(values).map((v) => v.split('|')[0])).toEqual(Object.keys(values).map(() => 'center'));
        });
    });
});
