import { test, expect } from '@playwright/test';
import { mockAuth, resolveWebVueSrc } from '../../setup';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

/**
 * Test E2E & Quét tĩnh cho phân hệ ShareData — Nhật ký cảnh báo lỗi (errorLog/index.vue)
 * và Drawer chi tiết cảnh báo (alertDetailDrawer.vue):
 * - Quét tĩnh cấu hình cột alertCodeDesc và tra cứu BaseConfigTypeEnum.SharedataErrorCode.
 * - Kiểm tra drawer chi tiết hiển thị đầy đủ Mô tả mã lỗi (alertDesc) và Hướng xử lý (actionGuide).
 * - Tuân thủ Rule 20.6 (CẤM !important) và Rule 20.8 (CẤM !!).
 */
test.describe('ShareData — Nhật ký cảnh báo lỗi (errorLog) @sharedata', () => {

    test.beforeEach(async ({ context, page }) => {
        await mockAuth(context, page, {
            userInfo: {
                defaultMenu: '/sharedata/errorLog',
            },
        });
    });

    test('1. Quét tĩnh mã nguồn errorLog/index.vue & alertDetailDrawer.vue: Tra cứu mã lỗi ESH và không dùng !important', async () => {
        const errorLogPath = resolveWebVueSrc('views/sharedata/errorLog/index.vue');
        const drawerPath = resolveWebVueSrc('views/sharedata/errorLog/component/alertDetailDrawer.vue');

        const errorLogContent = fs.readFileSync(errorLogPath, 'utf8');
        const drawerContent = fs.readFileSync(drawerPath, 'utf8');

        // 1. Quét errorLog/index.vue
        expect(errorLogContent).toContain("field: 'alertCodeDesc'");
        expect(errorLogContent).toContain('alertCodeDescOf');
        expect(errorLogContent).toContain('BaseConfigTypeEnum.SharedataErrorCode');
        expect(errorLogContent).not.toContain('!important');

        // 2. Quét alertDetailDrawer.vue
        expect(drawerContent).toContain('alertInfo?.name');
        expect(drawerContent).toContain('alertInfo?.remark');
        expect(drawerContent).toContain('BaseConfigTypeEnum.SharedataErrorCode');
        expect(drawerContent).not.toContain('!important');
    });

    test('2. Hiển thị bảng nhật ký lỗi với cột Mô tả mã lỗi ESH và mở Drawer chi tiết hiển thị Hướng xử lý', async ({ page }) => {
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
    });
});
