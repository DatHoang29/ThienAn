import { test, expect, type BrowserContext, type Page } from '@playwright/test';
import { mockAuth, resolveWebVueSrc } from '../../setup';
import fs from 'node:fs';
import path from 'node:path';

/**
 * Test E2E & Static Check cho Dialog "Chỉnh sửa bộ mã" (editCodeSet.vue)
 * Phân hệ ShareData > CodeSet
 *
 * Kiểm tra lỗi UI ở dòng "Giá trị mặc định":
 * - .cs-default-hint xuống dòng tự nhiên (white-space: normal, display: block).
 * - Dòng mặc định .cs-default-row tự giãn chiều cao (height: auto) để hiển thị trọn vẹn cả câu, không bị cắt nửa chữ.
 * - Cuộn toàn bộ modal ở cấp form ngoài (el-form), không tạo thanh scroll con thừa trong vxe-table.
 * - Không tạo thanh scroll con kỳ cục trong ô.
 * - Giữ nguyên nền vàng .cs-default-row.
 * - Không có !important (P0 rule).
 * - Hỗ trợ cả tiếng Việt lẫn tiếng Anh không bị cắt chữ.
 */

const editCodeSetPath = resolveWebVueSrc('views/sharedata/codeSet/component/editCodeSet.vue');

test.describe('ShareData — Dialog Chỉnh sửa bộ mã (editCodeSet.vue) @sharedata', () => {

    test('1. Kiểm tra tĩnh CSS và template của editCodeSet.vue', async () => {
        expect(fs.existsSync(editCodeSetPath)).toBeTruthy();
        const content = fs.readFileSync(editCodeSetPath, 'utf-8');

        // P0 Rule: Không được chứa !important
        expect(content.includes('!important')).toBeFalsy();

        // Nền vàng của dòng mặc định phải được giữ nguyên
        expect(content).toContain('.cs-values :deep(.cs-default-row)');
        expect(content).toContain('background-color: var(--el-color-warning-light-9)');

        // Cho phép dòng mặc định tự tăng chiều cao theo nội dung
        expect(content).toContain('.cs-values :deep(.cs-default-row .vxe-body--column)');
        expect(content).toContain('height: auto');
        expect(content).toContain('.cs-values :deep(.cs-default-row .vxe-cell)');
        expect(content).toContain('max-height: none');
        expect(content).toContain('overflow: visible');

        // Bảng con vxe-table không được gán max-height="300" (để không sinh thanh scroll con thứ 2)
        expect(content).not.toContain('max-height="300"');

        // Cột displayName phải có :show-overflow="false" để tắt c--tooltip / col--ellipsis
        expect(content).toContain(':show-overflow="false"');

        // Cấp ngoài el-form quản lý cuộn duy nhất của modal
        expect(content).toContain('style="max-height: 560px; overflow-y: auto; overflow-x: hidden"');

        // Gỡ bỏ triệt để max-height khi có class c--tooltip hoặc col--ellipsis
        expect(content).toContain('.cs-values :deep(.cs-default-row .vxe-cell.c--tooltip)');
        expect(content).toContain('.cs-values.size--mini :deep(.cs-default-row .vxe-body--column.col--ellipsis > .vxe-cell)');

        // Kiểm tra template: .cs-default-hint hiển thị trong dòng mặc định
        const hasHint = /<span[^>]*class="cs-default-hint"[^>]*>/.test(content);
        expect(hasHint, 'Template phải có span .cs-default-hint').toBeTruthy();

        // Kiểm tra CSS: .cs-default-hint phải có white-space normal, word-break break-word, display block
        const cssMatch = content.match(/\.cs-default-hint\s*\{([^}]+)\}/);
        expect(cssMatch, 'CSS phải có class .cs-default-hint').not.toBeNull();
        const cssBody = cssMatch![1];

        expect(cssBody).toContain('white-space: normal');
        expect(cssBody).toContain('word-break: break-word');
        expect(cssBody).toContain('display: block');
        expect(cssBody).not.toContain('overflow-y: auto'); // Không tạo scrollbar trong ô
    });

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

        test('2. Kiểm tra giao diện tiếng Việt: tự xuống dòng tăng chiều cao hàng, hiển thị trọn vẹn', async ({ context, page }) => {
            await setupCodeSetMock(context, page, 'vi-vn');
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

        test('3. Kiểm tra giao diện tiếng Anh: tự xuống dòng tăng chiều cao hàng, hiển thị trọn vẹn', async ({ context, page }) => {
            await setupCodeSetMock(context, page, 'en-us');
            await page.goto('/#/sharedata/codeSet');

            await page.locator('.loading-next').waitFor({ state: 'detached', timeout: 30000 }).catch(() => {});

            const addButton = page.locator('button:has(.ele-Plus), .vxe-toolbar button, .el-button').filter({ hasText: /Thêm|Add/ }).first();
            await expect(addButton).toBeVisible({ timeout: 60000 });
            await addButton.click();

            const dialog = page.locator('.el-overlay:not([style*="display: none"]) .el-dialog');
            await expect(dialog).toBeVisible({ timeout: 10000 });

            const defaultRow = dialog.locator('.cs-default-row');
            await expect(defaultRow).toBeVisible({ timeout: 10000 });

            const defaultHint = defaultRow.locator('.cs-default-hint');
            await expect(defaultHint).toBeVisible();

            const styles = await defaultHint.evaluate((el) => {
                const cs = window.getComputedStyle(el);
                return {
                    whiteSpace: cs.whiteSpace,
                    display: cs.display,
                    clientHeight: el.clientHeight,
                    scrollHeight: el.scrollHeight,
                };
            });

            expect(styles.whiteSpace).toBe('normal');
            expect(styles.display).toBe('block');
            expect(styles.clientHeight).toBeGreaterThanOrEqual(28);
            expect(styles.clientHeight).toBe(styles.scrollHeight);

            const table = dialog.locator('.vxe-table.cs-values');
            const tableMaxHeight = await table.evaluate((el) => window.getComputedStyle(el).maxHeight);
            expect(tableMaxHeight).not.toBe('300px');
        });

        test('4. Dialog editCodeSet và ElMessageBox.confirm KHÔNG đóng khi click ra ngoài', async ({ context, page }) => {
            await setupCodeSetMock(context, page, 'vi-vn');
            await page.goto('/#/sharedata/codeSet');

            await page.locator('.loading-next').waitFor({ state: 'detached', timeout: 30000 }).catch(() => {});

            // 1. Kiểm tra editCodeSet dialog
            const addButton = page.locator('button:has(.ele-Plus), .vxe-toolbar button, .el-button').filter({ hasText: /Thêm|Add/ }).first();
            await expect(addButton).toBeVisible({ timeout: 60000 });
            await addButton.click();

            const dialog = page.locator('.el-overlay:not([style*="display: none"]) .el-dialog');
            await expect(dialog).toBeVisible({ timeout: 10000 });

            // Click outside tại tọa độ (5, 5)
            await page.mouse.click(5, 5);
            await page.waitForTimeout(300);
            await expect(dialog).toBeVisible();

            // Đóng dialog bằng nút Hủy
            const cancelBtn = dialog.locator('.dialog-footer button:has-text("Hủy"), .dialog-footer button:has-text("Cancel")').first();
            await cancelBtn.click();
            await expect(dialog).toBeHidden({ timeout: 5000 });

            // 2. Kiểm tra ElMessageBox.confirm khi bấm Xóa trên hàng
            const deleteBtn = page.locator('.vxe-body--row button.el-button--danger').first();
            if (await deleteBtn.isVisible()) {
                await deleteBtn.click();
                const msgBox = page.locator('.el-message-box');
                await expect(msgBox).toBeVisible({ timeout: 5000 });

                // Click outside tại tọa độ (5, 5)
                await page.mouse.click(5, 5);
                await page.waitForTimeout(300);
                await expect(msgBox).toBeVisible();

                // Đóng bằng nút Hủy
                await msgBox.locator('button:has-text("Hủy"), button:has-text("Cancel")').first().click();
                await expect(msgBox).toBeHidden({ timeout: 5000 });
            }
        });
    });
});
