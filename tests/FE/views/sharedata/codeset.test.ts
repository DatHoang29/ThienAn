import { test, expect, type BrowserContext, type Page } from '@playwright/test';
import { mockAuth } from '../../setup';

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
    });
});
