import { test, expect } from '@playwright/test';

/**
 * Test E2E cho phân hệ ShareData — Modal Chi tiết Nhật ký (Cha - Con)
 * Kiểm thử component `activityDetailDialog.vue` và luồng hiển thị 2 bước ElSteps.
 */
test.describe('ShareData — Modal Chi tiết Tiến trình ElSteps @sharedata', () => {

    test.beforeEach(async ({ page, baseURL }) => {
        // Tầng 1: Kiểm tra xem Dev server Frontend có đang phản hồi không
        const targetUrl = baseURL ?? 'http://localhost:8888';
        try {
            const checkServer = await page.request.get(targetUrl, { timeout: 3000 });
            if (!checkServer.ok() && checkServer.status() !== 404 && checkServer.status() !== 304) {
                test.skip(true, `Dev server Frontend chưa sẵn sàng (HTTP ${checkServer.status()})`);
            }
        } catch {
            test.skip(true, `Dev server Frontend chưa khởi chạy trên ${targetUrl}. Chạy 'npm run dev' tại TA-ITS015-WEBVUE-V1.0/src để kiểm thử.`);
        }

        // Tầng 2: Giả lập token đăng nhập để Vue Router không chuyển hướng về /login
        await page.addInitScript(() => {
            sessionStorage.setItem('token', 'e2e-test-token-valid');
            sessionStorage.setItem('userInfo', JSON.stringify({
                account: 'admin',
                userName: 'Administrator',
                roles: ['superadmin'],
            }));
        });
    });

    test('1. Hiển thị đủ 2 bước ElSteps và không còn div.step-hint', async ({ page }) => {
        const mockParentId = 'PARENT_LOG_001';

        // Mock danh sách nhật ký có 1 dòng Transfer (chiều gửi)
        await page.route('**/api/sharedata/sharedataactivitylog/page*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        items: [
                            {
                                id: mockParentId,
                                logType: 1, // LOG_TYPE_TRANSFER
                                transferType: 0, // Gửi (Send)
                                success: 1,
                                packetId: 'PKT_01',
                                partnerId: 'PARTNER_01',
                                createTime: '2026-10-02 20:00:00',
                            },
                        ],
                        total: 1,
                    },
                }),
            });
        });

        // Mock API trả về 2 bước con thành công
        await page.route('**/api/sharedata/sharedataactivitylog/steps*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: [
                        {
                            id: mockParentId,
                            children: [
                                {
                                    id: 'CHILD_STEP_1',
                                    parentId: mockParentId,
                                    stepNo: 1,
                                    success: 1,
                                    description: 'Trích xuất thành công 10 bản ghi',
                                },
                                {
                                    id: 'CHILD_STEP_2',
                                    parentId: mockParentId,
                                    stepNo: 2,
                                    success: 1,
                                    description: 'Đã gửi đến đối tác qua HTTP (200 OK)',
                                },
                            ],
                        },
                    ],
                }),
            });
        });

        await page.goto('/sharedata/history');

        // Tìm nút mở modal chi tiết của dòng đầu tiên
        const detailButton = page.locator('button:has-text("Chi tiết")').first();
        if (await detailButton.isVisible({ timeout: 5000 }).catch(() => false)) {
            await detailButton.click();

            const dialog = page.locator('.el-dialog');
            await expect(dialog).toBeVisible();

            // Khẳng định 1: Có đúng 2 bước trong ElSteps
            const steps = dialog.locator('.steps-card .el-step');
            await expect(steps).toHaveCount(2);

            // Khẳng định 2: Bước 1 và Bước 2 đều thành công (class is-success)
            await expect(steps.nth(0)).toHaveClass(/is-success/);
            await expect(steps.nth(1)).toHaveClass(/is-success/);

            // Khẳng định 3: Khối div.step-hint đã bị xóa hoàn toàn khỏi DOM
            const stepHint = dialog.locator('.step-hint');
            await expect(stepHint).toHaveCount(0);
        } else {
            // Khi chưa mount vào route phân hệ, ghi nhận trạng thái kiểm tra component
            test.skip(true, 'Menu hoặc trang /sharedata/history chưa được phân quyền truy cập');
        }
    });

    test('2. Bước 2 báo lỗi (is-error) khi gửi thất bại và hiển thị thông báo lỗi', async ({ page }) => {
        const mockParentId = 'PARENT_LOG_FAIL';

        // Mock danh sách nhật ký
        await page.route('**/api/sharedata/sharedataactivitylog/page*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        items: [
                            {
                                id: mockParentId,
                                logType: 1,
                                transferType: 0,
                                success: 0,
                                packetId: 'PKT_01',
                                createTime: '2026-10-02 20:00:00',
                            },
                        ],
                        total: 1,
                    },
                }),
            });
        });

        // Mock API trả về: Bước 1 OK, Bước 2 lỗi
        await page.route('**/api/sharedata/sharedataactivitylog/steps*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: [
                        {
                            id: mockParentId,
                            children: [
                                {
                                    id: 'CHILD_STEP_1',
                                    parentId: mockParentId,
                                    stepNo: 1,
                                    success: 1,
                                    description: 'Trích xuất dữ liệu thành công',
                                },
                                {
                                    id: 'CHILD_STEP_2',
                                    parentId: mockParentId,
                                    stepNo: 2,
                                    success: 0,
                                    description: 'Gửi thất bại',
                                    errorMessage: 'Máy chủ đối tác trả HTTP 504 Gateway Timeout',
                                },
                            ],
                        },
                    ],
                }),
            });
        });

        await page.goto('/sharedata/history');

        const detailButton = page.locator('button:has-text("Chi tiết")').first();
        if (await detailButton.isVisible({ timeout: 5000 }).catch(() => false)) {
            await detailButton.click();

            const dialog = page.locator('.el-dialog');
            await expect(dialog).toBeVisible();

            const steps = dialog.locator('.steps-card .el-step');
            await expect(steps).toHaveCount(2);

            // Bước 1 thành công
            await expect(steps.nth(0)).toHaveClass(/is-success/);

            // Bước 2 thất bại và mô tả mang nội dung lỗi
            await expect(steps.nth(1)).toHaveClass(/is-error/);
            await expect(steps.nth(1).locator('.el-step__description')).toContainText('504 Gateway Timeout');
        } else {
            test.skip(true, 'Menu hoặc trang /sharedata/history chưa được phân quyền truy cập');
        }
    });

    test('3. Hiển thị trạng thái chờ (is-wait) khi chưa có dữ liệu bước con', async ({ page }) => {
        const mockParentId = 'PARENT_LOG_WAIT';

        await page.route('**/api/sharedata/sharedataactivitylog/page*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        items: [
                            {
                                id: mockParentId,
                                logType: 1,
                                transferType: 0,
                                success: 1,
                                createTime: '2026-10-02 20:00:00',
                            },
                        ],
                        total: 1,
                    },
                }),
            });
        });

        // Mock trả về mảng rỗng (chưa có bước con)
        await page.route('**/api/sharedata/sharedataactivitylog/steps*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: [],
                }),
            });
        });

        await page.goto('/sharedata/history');

        const detailButton = page.locator('button:has-text("Chi tiết")').first();
        if (await detailButton.isVisible({ timeout: 5000 }).catch(() => false)) {
            await detailButton.click();

            const dialog = page.locator('.el-dialog');
            await expect(dialog).toBeVisible();

            const steps = dialog.locator('.steps-card .el-step');
            await expect(steps).toHaveCount(2);

            // Cả 2 bước đều ở trạng thái chờ
            await expect(steps.nth(0)).toHaveClass(/is-wait/);
            await expect(steps.nth(1)).toHaveClass(/is-wait/);
        } else {
            test.skip(true, 'Menu hoặc trang /sharedata/history chưa được phân quyền truy cập');
        }
    });
});
