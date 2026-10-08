import { test, expect } from '@playwright/test';

/**
 * Test E2E cho phân hệ ShareData — nghi vấn Issue 30/32 (F16): thanh tiêu đề 3 tab
 * (.el-tabs__header trong .mp-tabs) của modal editMapping.vue có thể bị cuộn ra khỏi
 * khung nhìn của vùng cuộn cha (.el-form, max-height: calc(85vh - 120px)) khi nội dung
 * tab "Ánh xạ" phình to — do CSS không có position: sticky (editMapping.vue:2412) và
 * mỗi lần bấm "Phân tích" làm Vue remount toàn bộ <el-tree> (:key="state.treeVersion",
 * editMapping.vue:180+1328).
 */

function genBigJson(fieldCount: number): string {
    const obj: Record<string, any> = { header: {}, body: {} };
    for (let i = 0; i < fieldCount; i++) {
        const bucket = i % 2 === 0 ? obj.header : obj.body;
        bucket[`field${i}`] = `value_${i}`;
    }
    return JSON.stringify(obj);
}

test.describe('ShareData — Mất tiêu đề 3 tab trong editMapping.vue @sharedata', () => {

    test.beforeEach(async ({ context, page }) => {
        test.setTimeout(120_000);
        const mockToken = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjQxMDI0NDQ4MDAsIm5hbWUiOiJhZG1pbiJ9.mock_signature';
        await context.addCookies([
            { name: 'token', value: mockToken, url: 'http://localhost:8888' },
        ]);

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
                authBtnList: ['*', 'eshMapping:add', 'eshMapping:update', 'eshMapping:delete'],
                defaultMenu: '/sharedata/mapping',
            }));
        }, mockToken);

        await page.route('**/api/system/sysuser/info*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        account: 'admin',
                        realName: 'Administrator',
                        accountType: '111',
                        roles: ['admin'],
                        authBtnList: ['*', 'eshMapping:add', 'eshMapping:update', 'eshMapping:delete'],
                    },
                }),
            });
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
                                    id: 'menu_sharedata_mapping',
                                    pid: 'menu_sharedata',
                                    path: '/sharedata/mapping',
                                    name: 'sharedataMapping',
                                    component: 'sharedata/mapping/index',
                                    meta: { title: 'lz.menu.sharedataMapping', isLink: '', isHide: false, isKeepAlive: true, isAffix: false, isIframe: false, roles: ['admin'], icon: 'ele-Connection' },
                                },
                            ],
                        },
                    ],
                }),
            });
        });

        await page.route('**/api/sharedata/sharedatamapping/page*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({ code: 200, result: { records: [{ id: 'MAP_01', code: 'MAP01', name: 'Bản ánh xạ test' }], total: 1 } }),
            });
        });

        await page.route('**/api/sharedata/sharedatamapping/list*', async (route) => {
            await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ code: 200, result: [] }) });
        });

        await page.route('**/api/sharedata/sharedatapartner/list*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({ code: 200, result: [{ id: 'PARTNER_01', code: 'DT01', name: 'Đối tác test' }] }),
            });
        });

        await page.route('**/api/sharedata/sharedatapacket/list*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({ code: 200, result: [{ id: 'PKT_01', code: '101_commonData', name: 'Gói 101' }] }),
            });
        });

        await page.route('**/api/sharedata/sharedatasubscription/list*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: [{ id: 'SUB_01', partnerId: 'PARTNER_01', datatypeId: 'PKT_01', direction: 0, state: 1 }],
                }),
            });
        });

        await page.route('**/api/sharedata/sharedatapacketfield/list*', async (route) => {
            await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ code: 200, result: [] }) });
        });

        await page.route('**/api/sharedata/sharedatacodeset/list*', async (route) => {
            await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ code: 200, result: [] }) });
        });

        await page.route('**/api/sysConst/list*', async (route) => {
            await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ code: 200, result: [] }) });
        });
        await page.route('**/api/sysDictData/dataList*', async (route) => {
            await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ code: 200, result: [] }) });
        });
    });

    /**
     * Mở modal Thêm mới, chọn Đối tác + Gói tin (cả 2 field này nằm ở tab "Thông tin chung",
     * editMapping.vue dòng 22-29 và 48-57 — PHẢI chọn ở đây, KHÔNG nằm trong tab "Ánh xạ"),
     * rồi mới chuyển sang tab "Ánh xạ". Trạng thái chung cho EC1/2/3/5/6.
     */
    async function openAddDialogAndReachMappingTab(page: import('@playwright/test').Page) {
        await page.goto('/sharedata/mapping');
        const addBtn = page.locator('button:has(.ele-Plus), button:has-text("Thêm mới"), button:has-text("Thêm")').first();
        await expect(addBtn).toBeVisible({ timeout: 60000 });
        await addBtn.click();

        const dialog = page.locator('.mp-dialog');
        await expect(dialog).toBeVisible();

        // Tab "Thông tin chung" (mặc định đang mở): chọn Đối tác trước — chọn xong mới có
        // dữ liệu đăng ký để lọc Gói tin (handlePartnerChange → loadPartnerSubscriptions).
        await dialog.locator('.el-select').first().click();
        await page.locator('.el-popper:visible .el-select-dropdown__item', { hasText: 'DT01' }).first().click();

        // Chọn Gói tin — ô select thứ 2 trong form (sau Đối tác, radio Chiều không phải el-select).
        await dialog.locator('.el-select').nth(1).click();
        await page.locator('.el-popper:visible .el-select-dropdown__item', { hasText: '101_commonData' }).first().click();

        await dialog.getByRole('tab', { name: 'Ánh xạ' }).click();
        return dialog;
    }

    function tabsHeaderLocator(page: import('@playwright/test').Page) {
        return page.locator('.mp-dialog .mp-tabs .el-tabs__header');
    }

    function scrollFormLocator(page: import('@playwright/test').Page) {
        return page.locator('.mp-dialog .el-form').first();
    }

    /** true nếu top của header nằm trong [top, top+height] của vùng cuộn cha — tức còn nhìn thấy được. */
    async function isHeaderWithinFormViewport(page: import('@playwright/test').Page) {
        const formBox = await scrollFormLocator(page).boundingBox();
        const headerBox = await tabsHeaderLocator(page).boundingBox();
        if (!formBox || !headerBox) return false;
        return headerBox.y >= formBox.y - 1 && headerBox.y < formBox.y + formBox.height;
    }

    test('EC1 — Thêm mới: Phân tích JSON 30 trường, tiêu đề 3 tab phải còn trong khung nhìn', async ({ page }) => {
        const dialog = await openAddDialogAndReachMappingTab(page);
        await expect(tabsHeaderLocator(page)).toBeVisible();

        await dialog.locator('textarea.mp-json, .mp-json textarea').fill(genBigJson(30));
        await dialog.getByRole('button', { name: 'Phân tích' }).click();
        await page.waitForTimeout(500); // chờ el-tree remount + layout ổn định

        await page.screenshot({ path: 'test-results/EC1-after-analyze.png', fullPage: false });
        expect(await isHeaderWithinFormViewport(page)).toBe(true);
    });

    test('EC2 — Phân tích 2 lần liên tiếp (JSON khác nhau), tiêu đề vẫn phải còn trong khung nhìn', async ({ page }) => {
        const dialog = await openAddDialogAndReachMappingTab(page);
        const textarea = dialog.locator('textarea.mp-json, .mp-json textarea');
        const analyzeBtn = dialog.getByRole('button', { name: 'Phân tích' });

        await textarea.fill(genBigJson(20));
        await analyzeBtn.click();
        await page.waitForTimeout(500);

        await textarea.fill(genBigJson(40));
        await analyzeBtn.click();
        await page.waitForTimeout(500);

        await page.screenshot({ path: 'test-results/EC2-after-second-analyze.png', fullPage: false });
        expect(await isHeaderWithinFormViewport(page)).toBe(true);
    });

    test('EC3 — Baseline: Phân tích JSON chỉ 3 trường, tiêu đề phải còn trong khung nhìn', async ({ page }) => {
        const dialog = await openAddDialogAndReachMappingTab(page);
        await dialog.locator('textarea.mp-json, .mp-json textarea').fill(genBigJson(3));
        await dialog.getByRole('button', { name: 'Phân tích' }).click();
        await page.waitForTimeout(500);

        await page.screenshot({ path: 'test-results/EC3-baseline-small-json.png', fullPage: false });
        expect(await isHeaderWithinFormViewport(page)).toBe(true);
    });

    test('EC4 — Sao chép 1 bản ánh xạ có sẵn JSON 30 trường, tiêu đề phải còn trong khung nhìn ngay lúc mở', async ({ page }) => {
        await page.route('**/api/sharedata/sharedatamapping/byid*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        id: 'MAP_01',
                        code: 'MAP01',
                        partnerId: 'PARTNER_01',
                        datatypeId: 'PKT_01',
                        direction: 0,
                        partnerSchemaJson: genBigJson(30),
                        targetShapeJson: '',
                    },
                }),
            });
        });

        await page.goto('/sharedata/mapping', { waitUntil: 'domcontentloaded' });
        await page.locator('button[icon="ele-CopyDocument"], button:has(i.ele-CopyDocument), button:has(.ele-CopyDocument)').first().click()
            .catch(async () => {
                // Fallback: nút Sao chép chỉ có icon, không có text — click theo tooltip title.
                await page.locator('.el-tooltip__trigger', { hasText: '' }).nth(0).click();
            });

        const dialog = page.locator('.mp-dialog');
        await expect(dialog).toBeVisible();
        await page.waitForTimeout(800); // chờ openDialog() load xong + parsePartnerJson({silent:true}) dựng cây

        await page.screenshot({ path: 'test-results/EC4-after-copy-open.png', fullPage: false });
        expect(await isHeaderWithinFormViewport(page)).toBe(true);
    });

    test('EC5 — Cuộn lên đầu sau khi mất tiêu đề: tiêu đề phải xuất hiện lại', async ({ page }) => {
        const dialog = await openAddDialogAndReachMappingTab(page);
        await dialog.locator('textarea.mp-json, .mp-json textarea').fill(genBigJson(30));
        await dialog.getByRole('button', { name: 'Phân tích' }).click();
        await page.waitForTimeout(500);

        await scrollFormLocator(page).evaluate((el) => { el.scrollTop = 0; });
        await page.waitForTimeout(200);

        await page.screenshot({ path: 'test-results/EC5-after-scroll-to-top.png', fullPage: false });
        expect(await isHeaderWithinFormViewport(page)).toBe(true);
    });

    test('EC6 — Dialog vẫn kéo được qua thanh tiêu đề thật dù tab header có thể đang khó thấy', async ({ page }) => {
        const dialog = await openAddDialogAndReachMappingTab(page);
        await dialog.locator('textarea.mp-json, .mp-json textarea').fill(genBigJson(30));
        await dialog.getByRole('button', { name: 'Phân tích' }).click();
        await page.waitForTimeout(500);

        const dragHandle = page.locator('.mp-dialog .el-dialog__header');
        const before = await dialog.boundingBox();
        const handleBox = await dragHandle.boundingBox();
        if (!before || !handleBox) throw new Error('Không lấy được vị trí dialog/drag-handle');

        await page.mouse.move(handleBox.x + handleBox.width / 2, handleBox.y + handleBox.height / 2);
        await page.mouse.down();
        await page.mouse.move(handleBox.x + 150, handleBox.y + 100, { steps: 10 });
        await page.mouse.up();
        await page.waitForTimeout(200);

        const after = await dialog.boundingBox();
        if (!after) throw new Error('Dialog biến mất sau khi kéo');

        // Dialog phải DI CHUYỂN (toạ độ đổi) — xác nhận draggable vẫn hoạt động, độc lập với tab header.
        expect(Math.abs(after.x - before.x) + Math.abs(after.y - before.y)).toBeGreaterThan(20);
    });
});
