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

        // Fallback: Chặn mọi request /api/** lọt ra server thật gây lỗi hoặc treo proxy
        await page.route('**/api/**', async (route) => {
            await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ code: 200, result: null }) });
        });

        // 3. Mock API Auth UserInfo
        const mockUserInfo = {
            code: 200,
            result: {
                account: 'admin',
                realName: 'Administrator',
                accountType: '111',
                roles: ['admin'],
                authBtnList: ['*'],
                defaultMenu: '/sharedata/history',
            },
        };
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

    const stepCases: {
        name: string;
        id: string;
        success: number;
        children: Record<string, unknown>[];
        heads: RegExp[];
        step2Text?: string;
    }[] = [
        {
            name: 'hai bước thành công (is-success)',
            id: 'PARENT_LOG_SUCCESS',
            success: 1,
            children: [
                { id: 'CHILD_STEP_1', stepNo: 1, stepNbr: 1, success: 1, description: 'Trích xuất thành công 10 bản ghi' },
                { id: 'CHILD_STEP_2', stepNo: 2, stepNbr: 2, success: 1, description: 'Đã gửi đến đối tác qua HTTP (200 OK)' },
            ],
            heads: [/is-success/, /is-success/],
        },
        {
            name: 'bước 2 gửi thất bại (is-error) kèm thông báo lỗi',
            id: 'PARENT_LOG_FAIL',
            success: 0,
            children: [
                { id: 'CHILD_STEP_1', stepNo: 1, stepNbr: 1, success: 1, description: 'Trích xuất dữ liệu thành công' },
                { id: 'CHILD_STEP_2', stepNo: 2, stepNbr: 2, success: 0, description: 'Gửi thất bại', errorMessage: 'Máy chủ đối tác trả HTTP 504 Gateway Timeout' },
            ],
            heads: [/is-success/, /is-error/],
            step2Text: '504 Gateway Timeout',
        },
    ];

    for (const c of stepCases) {
        test(`1. ElSteps 2 bước hiển thị đúng trạng thái: ${c.name}`, async ({ page }) => {
            test.slow(); // Vite khởi động & compile on-demand lần đầu cần nhiều thời gian trên Windows

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
                                    id: c.id,
                                    logType: 1, // LOG_TYPE_TRANSFER
                                    transferType: 0, // Chiều gửi (Send)
                                    success: c.success,
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

            // Mock API bước con (mảng rỗng khi chưa có bước con)
            await page.route('**/api/sharedata/sharedataactivitylog/steps*', async (route) => {
                await route.fulfill({
                    status: 200,
                    contentType: 'application/json',
                    body: JSON.stringify({
                        code: 200,
                        result: c.children.length
                            ? [{ id: c.id, children: c.children.map((ch) => ({ ...ch, parentId: c.id })) }]
                            : [],
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

            // Có đúng 2 bước trong ElSteps, trạng thái từng bước theo dữ liệu bước con
            const steps = dialog.locator('.steps-card .el-step');
            await expect(steps).toHaveCount(2);
            for (let i = 0; i < c.heads.length; i++) {
                await expect(steps.nth(i).locator('.el-step__head')).toHaveClass(c.heads[i]);
            }

            // Bước 2 thất bại thì mô tả mang nội dung lỗi
            if (c.step2Text) {
                await expect(steps.nth(1).locator('.el-step__description')).toContainText(c.step2Text);
            }
        });
    }

    test('2. Lượt truyền nhận bị lỗi ngay từ đầu và không có bước con: hiển thị khối rỗng và tiêu đề không có (2 bước)', async ({ page }) => {
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

    test('3. Ẩn trường Địa chỉ IP, gộp nhãn Serial / Số gói khi trùng nhau, và định dạng JSON', async ({ page }) => {
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
        const formatItem = dialog.locator('tr:has(.el-descriptions__label:has-text("Định dạng"))');
        await expect(formatItem).toContainText('JSON');

        // 4. Khẳng định modal rộng 1080px và nhãn "Đường dẫn" không bị ngắt dòng (white-space: nowrap)
        await expect(dialog).toHaveCSS('width', '1080px');
        const evidenceLabel = dialog.locator('.evidence-descriptions .evidence-label').first();
        await expect(evidenceLabel).toBeVisible();
        await expect(evidenceLabel).toContainText('Đường dẫn');
        await expect(evidenceLabel).toHaveCSS('white-space', 'nowrap');
        const evidencePath = dialog.locator('.evidence-descriptions .evidence-path-text');
        await expect(evidencePath).toBeVisible();
        await expect(evidencePath).toContainText('/data/export/packet_7496.json');
    });

    test('4. Hộp chỉ xem chi tiết đóng khi click ra ngoài (Rule 20.26) hoặc phím Esc', async ({ page }) => {
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

        // Click ra ngoài vùng nội dung modal (vào overlay tại tọa độ 5, 5)
        await page.mouse.click(5, 5);
        await page.waitForTimeout(300);

        // Hộp thoại chỉ xem đóng khi click ra ngoài theo Rule 20.26
        await expect(dialog).toBeHidden({ timeout: 5000 });
    });

    test('5. Hiển thị Tab cấp trên (.history-tabs) chuyển đổi mượt mà với active-bar trượt chuẩn TMS', async ({ page }) => {
        test.slow();
        await page.route('**/api/sharedata/sharedataactivitylog/page*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        records: [
                            {
                                id: 'LOG_ROW_1',
                                logType: 1,
                                transferDirection: 0,
                                transferType: 0,
                                action: 1,
                                targetName: 'Đối tác Cục CSGT',
                                description: 'Cập nhật cấu hình đối tác',
                                changedFields: 'address, port',
                                operatorName: 'Administrator',
                                success: 1,
                                datatypeId: 'PKT_01',
                                partnerName: 'Đối tác Cục CSGT',
                                serialNbr: '1024',
                                pduType: 'd2d',
                                format: 'JSON',
                                byteSize: 512,
                                recordCount: 5,
                                occurredAt: '2026-10-12T09:30:00Z',
                            },
                        ],
                        total: 1,
                    },
                }),
            });
        });

        await page.goto('/sharedata/history');

        const historyTabs = page.locator('.history-tabs');
        await expect(historyTabs).toBeVisible({ timeout: 30000 });
        // Chuẩn TMS: Line tabs tiêu chuẩn, không dùng border-card
        await expect(historyTabs).not.toHaveClass(/el-tabs--border-card/);
        await expect(page.locator('.loading-next')).toHaveCount(0, { timeout: 10000 });

        const configTab = historyTabs.getByRole('tab', { name: /Cấu hình/i });
        const transferTab = historyTabs.getByRole('tab', { name: /Truyền nhận/i });
        await expect(configTab).toHaveClass(/is-active/);

        // Kiểm tra thanh gạch dưới active-bar chuẩn TMS di chuyển (move) khi đổi tab
        const activeBar = historyTabs.locator('.el-tabs__active-bar');
        await expect(activeBar).toBeVisible();

        const initialBarBox = await activeBar.boundingBox();
        expect(initialBarBox).not.toBeNull();

        // Chuyển sang tab Truyền nhận: active-bar di chuyển (move) sang vị trí của transferTab
        await transferTab.click();
        await expect(transferTab).toHaveClass(/is-active/);
        await page.waitForTimeout(300);

        const movedBarBox = await activeBar.boundingBox();
        expect(movedBarBox).not.toBeNull();
        expect(movedBarBox!.x).toBeGreaterThan(initialBarBox!.x + 20);
        await expect(page.locator('.stat-row .stat-card')).toHaveCount(5);

        // Chuyển lại tab Cấu hình: active-bar di chuyển ngược lại khớp toạ độ ban đầu
        await configTab.click();
        await expect(configTab).toHaveClass(/is-active/);
        await page.waitForTimeout(300);

        const backBarBox = await activeBar.boundingBox();
        expect(backBarBox).not.toBeNull();
        expect(Math.abs(backBarBox!.x - initialBarBox!.x)).toBeLessThanOrEqual(1);
        await expect(page.locator('.stat-row .stat-card')).toHaveCount(3);

        await page.screenshot({ path: 'test-results/snapshot-4-history-top-tabs.png', fullPage: false });
    });

    test('6. Giao diện chẩn đoán lỗi trong modal chi tiết không bị trùng lặp chuỗi lỗi, hiển thị mã lỗi và nút sao chép', async ({ page }) => {
        const mockParentId = 'PARENT_LOG_DUP_TEST';
        const rawErrorMsg = 'An error occurred while sending the request.';

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
                                packetId: 'PKT_DUP_01',
                                partnerId: 'PARTNER_DUP_01',
                                occurredAt: '2026-10-14T15:00:00Z',
                                description: `Lỗi gửi gói tin loại 13 với đối tác "BQP": ${rawErrorMsg}`,
                                errorMessage: rawErrorMsg,
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
                                    stepNbr: 1,
                                    success: 1,
                                    description: 'Trích xuất dữ liệu thành công',
                                },
                                {
                                    id: 'CHILD_STEP_2',
                                    parentId: mockParentId,
                                    stepNo: 2,
                                    stepNbr: 2,
                                    success: 0,
                                    description: `Ánh xạ hoặc gửi dữ liệu thất bại: ${rawErrorMsg}`,
                                    errorMessage: rawErrorMsg,
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

        // 1. Kiểm tra mô tả chung ở el-descriptions phía trên: đã được làm sạch, không bị nối chuỗi lỗi lặp lại
        const contentRow = dialog.locator('tr:has(.el-descriptions__label:has-text("Nội dung"))');
        await expect(contentRow).toBeVisible();
        await expect(contentRow).not.toContainText(rawErrorMsg);
        await expect(contentRow).toContainText('Lỗi gửi gói tin loại 13 với đối tác "BQP"');

        // 2. Kiểm tra ô Thông báo lỗi có nút copy
        const errorRow = dialog.locator('.main-err-wrapper');
        await expect(errorRow).toBeVisible();
        await expect(errorRow.locator('.err-text')).toContainText(rawErrorMsg);
        await expect(errorRow.locator('.copy-err-btn')).toBeVisible();

        // 3. Kiểm tra Bước 2 trong ElSteps:
        const steps = dialog.locator('.steps-card .el-step');
        await expect(steps).toHaveCount(2);

        // Mô tả bước (.step-desc-text) đã làm sạch, không lặp lại chuỗi raw error
        const step2Desc = steps.nth(1).locator('.step-desc-text');
        await expect(step2Desc).not.toContainText(rawErrorMsg);
        await expect(step2Desc).toContainText('Ánh xạ hoặc gửi dữ liệu thất bại');

        // Khối Diagnostic Callout (.step-err-box): Chứa tiêu đề thân thiện, gợi ý, raw error code và nút copy
        const errBox = steps.nth(1).locator('.step-err-box');
        await expect(errBox).toBeVisible();
        await expect(errBox.locator('.step-err-title')).toContainText('Không thể kết nối tới máy chủ đối tác');
        await expect(errBox.locator('.step-err-hint')).toBeVisible();
        await expect(errBox.locator('.raw-code')).toContainText(rawErrorMsg);
        await expect(errBox.locator('.err-copy-btn')).toBeVisible();
    });

    test('7. Cắt gọn lỗi 200 ký tự an toàn surrogate pair kèm Xem toàn bộ / Thu gọn, và hiển thị đỏ "0 bản ghi" cho bước không chạy được khi bước trước lỗi', async ({ page }) => {
        const mockParentId = 'PARENT_LOG_STEP1_FAIL_LONG';
        // Tạo chuỗi lỗi 500 ký tự có surrogate pair emoji ngay vị trí cắt 199..200
        const prefix199 = 'E'.repeat(199);
        const emojiPair = '\uD83D\uDE00'; // 2 UTF-16 code units tại index 199 và 200
        const tail = 'X'.repeat(500 - 199 - 2);
        const longError500 = `${prefix199}${emojiPair}${tail}`;
        expect(longError500.length).toBe(500);

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
                                transferDirection: 0, // Chiều gửi 3 bước
                                transferType: 0,
                                success: 0,
                                packetId: 'PKT_01',
                                partnerId: 'PARTNER_01',
                                errorMessage: longError500,
                                occurredAt: '2026-10-18T10:00:00Z',
                            },
                        ],
                        total: 1,
                    },
                }),
            });
        });

        // Chỉ có Bước 1 chạy và thất bại; Bước 2 và Bước 3 không có bản ghi
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
                                    id: 'CHILD_STEP_1_FAIL',
                                    parentId: mockParentId,
                                    stepNo: 1,
                                    stepNbr: 1,
                                    success: 0,
                                    description: 'Trích xuất dữ liệu thất bại',
                                    errorMessage: longError500,
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
        await expect(steps).toHaveCount(3);

        // 1. Bước 1: mã lỗi gốc bị cắt gọn (<= 201 ký tự kèm dấu …), không vỡ surrogate pair
        const rawCode1 = steps.nth(0).locator('.raw-code');
        await expect(rawCode1).toBeVisible();
        const truncatedText = (await rawCode1.textContent()) ?? '';
        expect(truncatedText.endsWith('…')).toBe(true);
        expect(truncatedText.length).toBeLessThanOrEqual(201);
        expect(truncatedText.includes('\uFFFD')).toBe(false);

        // Bấm "Xem toàn bộ" -> hiện đủ 500 ký tự; bấm "Thu gọn" -> về lại bản cắt gọn
        const toggleBtn = steps.nth(0).locator('.raw-toggle-btn');
        await expect(toggleBtn).toContainText('Xem toàn bộ');
        await toggleBtn.click();
        await expect(toggleBtn).toContainText('Thu gọn');
        const expandedText = (await rawCode1.textContent()) ?? '';
        expect(expandedText).toBe(longError500);

        await toggleBtn.click();
        await expect(toggleBtn).toContainText('Xem toàn bộ');

        // 2. Bước 2 và Bước 3 không có bản ghi khi lượt chạy lỗi -> trạng thái is-error và phụ đề đỏ "0 bản ghi"
        await expect(steps.nth(1).locator('.el-step__head')).toHaveClass(/is-error/);
        await expect(steps.nth(1).locator('.step-subtitle')).toHaveClass(/is-err/);
        await expect(steps.nth(1).locator('.step-subtitle')).toHaveText('0 bản ghi');

        await expect(steps.nth(2).locator('.el-step__head')).toHaveClass(/is-error/);
        await expect(steps.nth(2).locator('.step-subtitle')).toHaveClass(/is-err/);
        await expect(steps.nth(2).locator('.step-subtitle')).toHaveText('0 bản ghi');
    });

    test('8. Bảng Trước / Sau hiển thị ngày giờ ISO theo DD/MM/YYYY HH:mm:ss, chuỗi khác giữ nguyên văn', async ({ page }) => {
        await page.route('**/api/sharedata/sharedataactivitylog/page*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        records: [
                            {
                                id: 'LOG_DATE_DIFF',
                                logType: 0,
                                action: 1,
                                targetName: 'Đăng ký gói 101',
                                changedFields: 'NextTimeRun, Code',
                                beforeJson: JSON.stringify({ NextTimeRun: '2026-10-22T09:15:30', Code: '20261022' }),
                                afterJson: JSON.stringify({ NextTimeRun: '2026-10-22T10:05:00.123', Code: '20261023' }),
                                success: 1,
                                occurredAt: '2026-10-22T09:20:00Z',
                            },
                        ],
                        total: 1,
                    },
                }),
            });
        });

        await page.goto('/sharedata/history');

        const detailButton = page.locator('button:has-text("Chi tiết")').first();
        await expect(detailButton).toBeVisible({ timeout: 30000 });
        await detailButton.click();

        const dialog = page.locator('.el-dialog');
        await expect(dialog).toBeVisible();

        const timeRow = dialog.locator('.el-table__body tr', { hasText: 'NextTimeRun' });
        await expect(timeRow.locator('td').nth(1)).toHaveText('22/10/2026 09:15:30');
        await expect(timeRow.locator('td').nth(2)).toHaveText('22/10/2026 10:05:00');

        const codeRow = dialog.locator('.el-table__body tr', { hasText: 'Code' });
        await expect(codeRow.locator('td').nth(1)).toHaveText('20261022');
        await expect(codeRow.locator('td').nth(2)).toHaveText('20261023');
    });
});
