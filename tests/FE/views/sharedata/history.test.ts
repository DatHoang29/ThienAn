import { test, expect } from '@playwright/test';

/**
 * Test E2E cho phân hệ ShareData — Modal Chi tiết Nhật ký (Cha - Con)
 * Kiểm thử component `activityDetailDialog.vue` và luồng hiển thị 2 bước ElSteps.
 */
test.describe('ShareData — Modal Chi tiết Tiến trình ElSteps @sharedata', () => {

    test.beforeEach(async ({ context, page }) => {
        // 1. Cung cấp Cookie token chuẩn định dạng JWT (Header.Payload.Signature)
        // Payload chứa exp = 4102444800 (năm 2100) để decryptJWT() trong axios-utils không ném lỗi
        const mockToken = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjQxMDI0NDQ4MDAsIm5hbWUiOiJhZG1pbiJ9.mock_signature';
        await context.addCookies([
            {
                name: 'token',
                value: mockToken,
                url: 'http://localhost:8888',
            },
        ]);

        // 2. Cung cấp LocalStorage & SessionStorage với prefix 'tacwebcore'
        await page.addInitScript((token) => {
            document.cookie = `token=${token}; path=/;`;
            const pkg = 'tacwebcore';
            window.localStorage.setItem(`${pkg}:access-token`, JSON.stringify(token));
            window.sessionStorage.setItem(`${pkg}:token`, JSON.stringify(token));
            window.sessionStorage.setItem(`${pkg}:userInfo`, JSON.stringify({
                account: 'admin',
                realName: 'Administrator',
                roles: ['admin'],
                authBtnList: ['*'],
                defaultMenu: '/sharedata/history',
            }));
        }, mockToken);

        // 3. Mock API Auth UserInfo: /api/system/sysuser/info (POST)
        await page.route('**/api/system/sysuser/info*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        account: 'admin',
                        realName: 'Administrator',
                        roles: ['admin'],
                        authBtnList: ['*'],
                    },
                }),
            });
        });

        // 4. Mock API Menu Tree: /api/system/sysmenu/loginmenutree (GET)
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
                                    id: 'menu_sharedata_history',
                                    pid: 'menu_sharedata',
                                    path: '/sharedata/history',
                                    name: 'sharedataHistory',
                                    component: 'sharedata/history/index',
                                    meta: {
                                        title: 'lz.menu.sharedataHistory',
                                        isLink: '',
                                        isHide: false,
                                        isKeepAlive: true,
                                        isAffix: false,
                                        isIframe: false,
                                        roles: ['admin'],
                                        icon: 'ele-Document',
                                    },
                                },
                            ],
                        },
                    ],
                }),
            });
        });

        // 5. Mock Summary thống kê nhật ký (tránh lỗi handleQueryApi trong index.vue)
        await page.route('**/api/sharedata/sharedataactivitylog/summary*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        total: 1,
                        successCount: 1,
                        failedCount: 0,
                        sentCount: 1,
                        receivedCount: 0,
                    },
                }),
            });
        });

        // 6. Mock các API danh mục phụ trợ
        await page.route('**/api/sharedata/sharedatapacket/list*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({ code: 200, result: [{ id: 'PKT_01', name: 'Gói tin Phương tiện' }] }),
            });
        });

        await page.route('**/api/sharedata/sharedatapartner/list*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({ code: 200, result: [{ id: 'PARTNER_01', name: 'Đối tác Cục CSGT' }] }),
            });
        });

        await page.route('**/api/sysConst/list*', async (route) => {
            await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ code: 200, result: [] }) });
        });
        await page.route('**/api/sysDictData/dataList*', async (route) => {
            await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ code: 200, result: [] }) });
        });
    });

    test('1. Hiển thị đủ 2 bước ElSteps và không còn div.step-hint', async ({ page }) => {
        test.slow(); // Vite khởi động & compile on-demand lần đầu cần nhiều thời gian trên Windows
        const mockParentId = 'PARENT_LOG_SUCCESS';

        // Mock danh sách nhật ký có 1 dòng Transfer (chiều gửi)
        await page.route('**/api/sharedata/sharedataactivitylog/page*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        records: [
                            {
                                id: mockParentId,
                                logType: 1, // LOG_TYPE_TRANSFER
                                transferType: 0, // Chiều gửi (Send)
                                success: 1,
                                packetId: 'PKT_01',
                                partnerId: 'PARTNER_01',
                                occurredAt: '2026-10-02T20:00:00Z',
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
        await expect(detailButton).toBeVisible({ timeout: 30000 });
        await detailButton.click();

        const dialog = page.locator('.el-dialog');
        await expect(dialog).toBeVisible();

        // Khẳng định 1: Có đúng 2 bước trong ElSteps
        const steps = dialog.locator('.steps-card .el-step');
        await expect(steps).toHaveCount(2);

        // Khẳng định 2: Bước 1 và Bước 2 đều thành công (class is-success trên .el-step__head)
        await expect(steps.nth(0).locator('.el-step__head')).toHaveClass(/is-success/);
        await expect(steps.nth(1).locator('.el-step__head')).toHaveClass(/is-success/);

        // Khẳng định 3: Khối div.step-hint đã bị xóa hoàn toàn khỏi DOM
        const stepHint = dialog.locator('.step-hint');
        await expect(stepHint).toHaveCount(0);
    });

    test('2. Bước 2 báo lỗi (is-error) khi gửi thất bại và hiển thị thông báo lỗi', async ({ page }) => {
        const mockParentId = 'PARENT_LOG_FAIL';

        await page.route('**/api/sharedata/sharedataactivitylog/page*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        records: [
                            {
                                id: mockParentId,
                                logType: 1,
                                transferType: 0,
                                success: 0,
                                packetId: 'PKT_01',
                                partnerId: 'PARTNER_01',
                                occurredAt: '2026-10-02T20:00:00Z',
                            },
                        ],
                        total: 1,
                    },
                }),
            });
        });

        // Mock API: Bước 1 OK, Bước 2 lỗi
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
        await expect(detailButton).toBeVisible({ timeout: 30000 });
        await detailButton.click();

        const dialog = page.locator('.el-dialog');
        await expect(dialog).toBeVisible();

        const steps = dialog.locator('.steps-card .el-step');
        await expect(steps).toHaveCount(2);

        // Bước 1 thành công
        await expect(steps.nth(0).locator('.el-step__head')).toHaveClass(/is-success/);

        // Bước 2 thất bại và mô tả mang nội dung lỗi
        await expect(steps.nth(1).locator('.el-step__head')).toHaveClass(/is-error/);
        await expect(steps.nth(1).locator('.el-step__description')).toContainText('504 Gateway Timeout');
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
                        records: [
                            {
                                id: mockParentId,
                                logType: 1,
                                transferType: 0,
                                success: 1,
                                packetId: 'PKT_01',
                                partnerId: 'PARTNER_01',
                                occurredAt: '2026-10-02T20:00:00Z',
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
        await expect(detailButton).toBeVisible({ timeout: 30000 });
        await detailButton.click();

        const dialog = page.locator('.el-dialog');
        await expect(dialog).toBeVisible();

        const steps = dialog.locator('.steps-card .el-step');
        await expect(steps).toHaveCount(2);

        // Cả 2 bước đều ở trạng thái chờ
        await expect(steps.nth(0).locator('.el-step__head')).toHaveClass(/is-wait/);
        await expect(steps.nth(1).locator('.el-step__head')).toHaveClass(/is-wait/);
    });

    test('4. Lượt truyền nhận bị lỗi ngay từ đầu và không có bước con: hiển thị khối rỗng và tiêu đề không có (2 bước)', async ({ page }) => {
        const mockParentId = 'PARENT_LOG_FAIL_NO_STEPS';

        await page.route('**/api/sharedata/sharedataactivitylog/page*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        records: [
                            {
                                id: mockParentId,
                                logType: 1, // LOG_TYPE_TRANSFER
                                transferDirection: 0,
                                transferType: 0,
                                success: 0, // Thất bại
                                packetId: 'PKT_01',
                                partnerId: 'PARTNER_01',
                                errorMessage: 'Không tìm thấy phễu lọc chiều gửi đang bật cho gói tin',
                                occurredAt: '2026-10-05T08:00:00Z',
                            },
                        ],
                        total: 1,
                    },
                }),
            });
        });

        // Mock trả về mảng rỗng (không có bước con nào thực thi)
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
        await expect(detailButton).toBeVisible({ timeout: 30000 });
        await detailButton.click();

        const dialog = page.locator('.el-dialog');
        await expect(dialog).toBeVisible();

        // 1. Tiêu đề khối tiến trình không chứa "(2 bước)"
        const processTitle = dialog.locator('.sect-title:has(.el-icon):has-text("Tiến trình")');
        await expect(processTitle).not.toContainText('2 bước');
        await expect(processTitle).toContainText('Tiến trình xử lý');

        // 2. Thẻ .steps-card có class is-empty
        const stepsCard = dialog.locator('.steps-card');
        await expect(stepsCard).toHaveClass(/is-empty/);

        // 3. Khối .empty-process-simple xuất hiện với text "Không có tiến trình xử lý"
        const emptyBlock = stepsCard.locator('.empty-process-simple');
        await expect(emptyBlock).toBeVisible();
        await expect(emptyBlock).toContainText('Không có tiến trình xử lý');
        await expect(emptyBlock.locator('.empty-icon')).toBeVisible();

        // 4. Không render el-steps
        await expect(stepsCard.locator('.el-steps')).toHaveCount(0);
    });

    test('5. Ẩn trường Địa chỉ IP, gộp nhãn Serial / Số gói khi trùng nhau, và định dạng JSON', async ({ page }) => {
        const mockParentId = 'PARENT_LOG_FIELD_CHECK';

        await page.route('**/api/sharedata/sharedataactivitylog/page*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        records: [
                            {
                                id: mockParentId,
                                logType: 1,
                                transferDirection: 0,
                                transferType: 0,
                                success: 1,
                                packetId: 'PKT_01',
                                partnerId: 'PARTNER_01',
                                serialNbr: '7496',
                                packetNbr: '7496', // Trùng serialNbr
                                format: null,
                                filePath: '/data/export/packet_7496.json',
                                occurredAt: '2026-10-05T08:00:00Z',
                            },
                        ],
                        total: 1,
                    },
                }),
            });
        });

        await page.route('**/api/sharedata/sharedataactivitylog/steps*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({ code: 200, result: [] }),
            });
        });

        await page.goto('/sharedata/history');

        const detailButton = page.locator('button:has-text("Chi tiết")').first();
        await expect(detailButton).toBeVisible({ timeout: 30000 });
        await detailButton.click();

        const dialog = page.locator('.el-dialog');
        await expect(dialog).toBeVisible();

        // 1. Khẳng định trường "Địa chỉ IP" bị ẩn hoàn toàn khỏi modal
        await expect(dialog.locator('text="Địa chỉ IP"')).toHaveCount(0);

        // 2. Khẳng định nhãn gộp "Serial / Số gói" xuất hiện và mang giá trị 7496
        const serialMergedCell = dialog.locator('.el-descriptions__label:has-text("Serial / Số gói")');
        await expect(serialMergedCell).toBeVisible();

        // 3. Khẳng định trường Định dạng nhận diện file .json thành JSON
        const formatItem = dialog.locator('.el-descriptions__item:has(.el-descriptions__label:has-text("Định dạng"))');
        await expect(formatItem).toContainText('JSON');
    });

    test('6. Đóng modal chi tiết khi click outside vào overlay', async ({ page }) => {
        const mockParentId = 'PARENT_LOG_CLICK_OUTSIDE';

        await page.route('**/api/sharedata/sharedataactivitylog/page*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        records: [
                            {
                                id: mockParentId,
                                logType: 1,
                                transferDirection: 0,
                                transferType: 0,
                                success: 1,
                                packetId: 'PKT_01',
                                partnerId: 'PARTNER_01',
                                occurredAt: '2026-10-05T08:00:00Z',
                            },
                        ],
                        total: 1,
                    },
                }),
            });
        });

        await page.route('**/api/sharedata/sharedataactivitylog/steps*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({ code: 200, result: [] }),
            });
        });

        await page.goto('/sharedata/history');

        const detailButton = page.locator('button:has-text("Chi tiết")').first();
        await expect(detailButton).toBeVisible({ timeout: 30000 });
        await detailButton.click();

        const dialog = page.locator('.el-dialog');
        await expect(dialog).toBeVisible();

        // Click vào góc ngoài của overlay để kiểm tra tính năng đóng outside
        const overlay = page.locator('.el-overlay');
        await overlay.click({ position: { x: 10, y: 10 } });

        // Modal phải đóng lại và không còn hiển thị
        await expect(dialog).toBeHidden({ timeout: 5000 });
    });
});
