import { test, expect } from '@playwright/test';
import { resolveWebVueSrc } from '../../setup';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

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

        const mockUserInfo = {
            code: 200,
            result: {
                account: 'admin',
                realName: 'Administrator',
                accountType: '111',
                roles: ['admin'],
                authBtnList: ['*', 'eshMapping:add', 'eshMapping:update', 'eshMapping:delete'],
                defaultMenu: '/sharedata/mapping',
            },
        };

        // Fallback: Chặn mọi request /api/** lọt ra server thật gây lỗi 401
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
        await page.route('**/api/cfgsystem/sysopconfig/alldatalist*', async (route) => {
            await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ code: 200, result: {} }) });
        });
        await page.route('**/api/cfgsystem/sysconfigtype/alldatalist*', async (route) => {
            await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ code: 200, result: {} }) });
        });
        await page.route('**/api/system/sysconfig/loadclientterm*', async (route) => {
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
        test.slow();
        await page.goto('/#/sharedata/mapping');
        const addBtn = page.locator('button:has(.ele-Plus), button:has-text("Thêm mới"), button:has-text("Thêm")').first();
        await expect(addBtn).toBeVisible({ timeout: 60000 });
        await addBtn.click();

        const dialog = page.locator('.mp-dialog');
        await expect(dialog).toBeVisible();

        // 1. Tab "Thông tin chung" (mặc định đang mở): chọn Đối tác trước
        // Sau khi chọn đối tác, handlePartnerChange gọi apiSharedataSharedatasubscriptionListGet.
        // Cần chờ response này xong để ô Gói tin có dữ liệu.
        const partnerSelect = dialog.locator('.el-select').first();
        await partnerSelect.click();

        const subResponsePromise = page.waitForResponse(
            (resp) => resp.url().includes('/api/sharedata/sharedatasubscription/list') && resp.status() === 200
        );
        await page.locator('.el-popper:visible .el-select-dropdown__item', { hasText: 'DT01' }).first().click();
        await subResponsePromise;
        await page.waitForTimeout(300); // Đợi Vue computed availablePacketList cập nhật

        // 2. Chọn Gói tin — ô select thứ 2 trong form (sau Đối tác, radio Chiều không phải el-select).
        const packetSelect = dialog.locator('.el-select').nth(1);
        await packetSelect.click();
        await page.locator('.el-popper:visible .el-select-dropdown__item', { hasText: '101_commonData' }).first().click();
        await page.waitForTimeout(200);

        // 3. Chuyển sang tab "Ánh xạ"
        await dialog.getByRole('tab', { name: 'Ánh xạ' }).click();
        await page.waitForTimeout(300);
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

    function partnerJsonTextarea(pageOrDialog: import('@playwright/test').Locator) {
        return pageOrDialog.locator('.mp-pane textarea').first();
    }

    test('EC1 — Thêm mới: Phân tích JSON 30 trường, tiêu đề 3 tab phải còn trong khung nhìn', async ({ page }) => {
        const dialog = await openAddDialogAndReachMappingTab(page);
        await expect(tabsHeaderLocator(page)).toBeVisible();

        await partnerJsonTextarea(dialog).fill(genBigJson(30));
        await dialog.getByRole('button', { name: 'Phân tích' }).click();
        await page.waitForTimeout(500); // chờ el-tree remount + layout ổn định

        await page.screenshot({ path: 'test-results/EC1-after-analyze.png', fullPage: false });
        expect(await isHeaderWithinFormViewport(page)).toBe(true);
    });

    test('EC2 — Phân tích 2 lần liên tiếp (JSON khác nhau), tiêu đề vẫn phải còn trong khung nhìn', async ({ page }) => {
        const dialog = await openAddDialogAndReachMappingTab(page);
        const textarea = partnerJsonTextarea(dialog);
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
        await partnerJsonTextarea(dialog).fill(genBigJson(3));
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

        await page.goto('/#/sharedata/mapping');
        const copyBtn = page.locator('.vxe-body--row button:has(.ele-CopyDocument), .vxe-body--row button.el-button--warning').first();
        await expect(copyBtn).toBeVisible({ timeout: 60000 });
        await copyBtn.click();

        const dialog = page.locator('.mp-dialog');
        await expect(dialog).toBeVisible();
        await page.waitForTimeout(800); // chờ openDialog() load xong + parsePartnerJson({silent:true}) dựng cây

        await page.screenshot({ path: 'test-results/EC4-after-copy-open.png', fullPage: false });
        expect(await isHeaderWithinFormViewport(page)).toBe(true);
    });

    test('EC5 — Cuộn lên đầu sau khi mất tiêu đề: tiêu đề phải xuất hiện lại', async ({ page }) => {
        const dialog = await openAddDialogAndReachMappingTab(page);
        await partnerJsonTextarea(dialog).fill(genBigJson(30));
        await dialog.getByRole('button', { name: 'Phân tích' }).click();
        await page.waitForTimeout(500);

        await scrollFormLocator(page).evaluate((el) => { el.scrollTop = 0; });
        await page.waitForTimeout(200);

        await page.screenshot({ path: 'test-results/EC5-after-scroll-to-top.png', fullPage: false });
        expect(await isHeaderWithinFormViewport(page)).toBe(true);
    });

    test('EC6 — Dialog vẫn kéo được qua thanh tiêu đề thật dù tab header có thể đang khó thấy', async ({ page }) => {
        const dialog = await openAddDialogAndReachMappingTab(page);
        await partnerJsonTextarea(dialog).fill(genBigJson(30));
        await dialog.getByRole('button', { name: 'Phân tích' }).click();
        await page.waitForTimeout(500);

        const dragHandle = page.locator('.mp-dialog .el-dialog__header');
        const before = await dialog.boundingBox();
        const handleBox = await dragHandle.boundingBox();
        if (!before || !handleBox) throw new Error('Không lấy được vị trí dialog/drag-handle');

        await page.mouse.move(handleBox.x + 120, handleBox.y + handleBox.height / 2);
        await page.mouse.down();
        await page.mouse.move(handleBox.x + 250, handleBox.y + 100, { steps: 10 });
        await page.mouse.up();
        await page.waitForTimeout(200);

        const after = await dialog.boundingBox();
        if (!after) throw new Error('Dialog biến mất sau khi kéo');

        // Dialog phải DI CHUYỂN (toạ độ đổi) — xác nhận draggable vẫn hoạt động, độc lập với tab header.
        expect(Math.abs(after.x - before.x) + Math.abs(after.y - before.y)).toBeGreaterThan(20);
    });

    test('EC7 (Tái hiện bug) — Khi cuộn form xuống xem các trường, tiêu đề 3 tab BẮT BUỘC phải còn trong khung nhìn (Chưa sticky sẽ FAIL)', async ({ page }) => {
        const dialog = await openAddDialogAndReachMappingTab(page);
        await partnerJsonTextarea(dialog).fill(genBigJson(30));
        await dialog.getByRole('button', { name: 'Phân tích' }).click();
        await page.waitForTimeout(500);

        // Mô phỏng thao tác thực tế của tester: Cuộn form xuống 200px để cấu hình danh sách trường bên dưới
        await scrollFormLocator(page).evaluate((el) => { el.scrollTop = 200; });
        await page.waitForTimeout(300);

        await page.screenshot({ path: 'test-results/EC7-after-scroll-down.png', fullPage: false });

        // TIÊU CHÍ: Tiêu đề 3 tab BẮT BUỘC phải còn nằm trong khung nhìn của form.
        // Khi CHƯA có position: sticky, headerBox.y bị cuộn lên âm so với formBox.y -> PHẢI FAIL (RED).
        expect(await isHeaderWithinFormViewport(page)).toBe(true);
    });

    test('EC8 (Tái hiện bug) — Dòng báo lỗi trùng mã không được bị khuất hoặc đè lấn lên ô Tên ở hàng dưới', async ({ page }) => {
        test.slow();
        // Mock list trả về có phần tử để kích hoạt lỗi generatedCodeExists khi submit
        await page.route('**/api/sharedata/sharedatamapping/list*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({ code: 200, result: [{ id: 'EXIST_01', code: 'DT01_101COMMONDATA_OUT' }] }),
            });
        });

        await page.goto('/#/sharedata/mapping');
        const addBtn = page.locator('button:has(.ele-Plus), button:has-text("Thêm mới"), button:has-text("Thêm")').first();
        await expect(addBtn).toBeVisible({ timeout: 60000 });
        await addBtn.click();

        const dialog = page.locator('.mp-dialog');
        await expect(dialog).toBeVisible();

        // 1. Chọn Đối tác DT01
        const partnerSelect = dialog.locator('.el-select').first();
        await partnerSelect.click();
        const subResponsePromise = page.waitForResponse(
            (resp) => resp.url().includes('/api/sharedata/sharedatasubscription/list') && resp.status() === 200
        );
        await page.locator('.el-popper:visible .el-select-dropdown__item', { hasText: 'DT01' }).first().click();
        await subResponsePromise;
        await page.waitForTimeout(300);

        // 2. Chọn Gói tin 101_commonData
        const packetSelect = dialog.locator('.el-select').nth(1);
        await packetSelect.click();
        await page.locator('.el-popper:visible .el-select-dropdown__item', { hasText: '101_commonData' }).first().click();
        await page.waitForTimeout(200);

        // 3. Nhập Tên: 'test'
        const nameInput = dialog.locator('.mp-name-item input');
        await nameInput.fill('test');

        // 4. Chuyển sang tab Ánh xạ để điền JSON và bấm Xác nhận
        await dialog.getByRole('tab', { name: 'Ánh xạ' }).click();
        await page.waitForTimeout(300);
        await partnerJsonTextarea(dialog).fill(genBigJson(3));
        await dialog.getByRole('button', { name: 'Phân tích' }).click();
        await page.waitForTimeout(500);

        // Bấm Xác nhận (Submit) -> Sẽ kích hoạt kiểm tra trùng mã -> Mock trả về có phần tử -> form chuyển về tab info với state.codeError
        await dialog.getByRole('button', { name: 'Xác nhận' }).click();
        await page.waitForTimeout(500);

        // Chụp ảnh bằng chứng
        await page.screenshot({ path: 'test-results/EC8-code-error-overlap.png', fullPage: false });

        // Kiểm tra dòng báo lỗi trùng mã:
        const codeItem = dialog.locator('.mp-code-item');
        const errorEl = codeItem.locator('.el-form-item__error');
        await expect(errorEl).toBeVisible();

        const errorBox = await errorEl.boundingBox();
        const nameItemBox = await dialog.locator('.mp-name-item').boundingBox();

        expect(errorBox).not.toBeNull();
        expect(nameItemBox).not.toBeNull();

        // Đáy của dòng báo lỗi (y + height) KHÔNG ĐƯỢC vượt quá đỉnh của trường Tên (nameItemBox.y)!
        // Khi bị đè lấn / che khuất: errorBox.y + errorBox.height > nameItemBox.y -> TEST SẼ FAIL (RED)!
        expect(errorBox!.y + errorBox!.height).toBeLessThanOrEqual(nameItemBox!.y);
    });

    test('EC9 — Kiểm tra hiển thị style modal editMapping đúng chuẩn khi không dùng !important', async ({ page }) => {
        const dialog = await openAddDialogAndReachMappingTab(page);
        await expect(dialog).toBeVisible();

        const dialogBox = await dialog.boundingBox();
        expect(dialogBox).not.toBeNull();
        expect(dialogBox!.width).toBeLessThanOrEqual(1400);

        const maxWidth = await dialog.evaluate((el) => window.getComputedStyle(el).maxWidth);
        expect(maxWidth).toBe('1400px');
    });

    test('EC10 (Tái hiện bug 30, 32) — Mở modal, kéo modal lên top rồi chuyển sang tab Ánh xạ: Header dialog (.el-dialog__header) BẮT BUỘC không bị mất khỏi màn hình và vẫn kéo thả được', async ({ page }) => {
        // Vào trang và mở modal trực tiếp bằng nút Thêm mới (cùng component editMapping.vue với nút Sao chép)
        await page.goto('/#/sharedata/mapping');
        const addBtn = page.locator('button:has(.ele-Plus), button:has-text("Thêm mới"), button:has-text("Thêm")').first();
        await expect(addBtn).toBeVisible({ timeout: 5000 });
        await addBtn.click();

        const dialog = page.locator('.mp-dialog');
        await expect(dialog).toBeVisible({ timeout: 3000 });

        // 1. Tại tab Thông tin chung: Tìm drag handle của dialog (.el-dialog__header)
        const dragHandle = dialog.locator('.el-dialog__header');
        await expect(dragHandle).toBeVisible();
        const handleBox = await dragHandle.boundingBox();
        expect(handleBox).not.toBeNull();

        // 2. Kéo modal lên sát đỉnh viewport (giả lập thao tác kéo modal lên cao)
        console.log(`[TEST EC10] handleBox ban đầu: y = ${handleBox!.y}, x = ${handleBox!.x}`);
        await page.mouse.move(handleBox!.x + handleBox!.width / 2, handleBox!.y + handleBox!.height / 2);
        await page.mouse.down();
        await page.mouse.move(handleBox!.x + handleBox!.width / 2, handleBox!.y - 100, { steps: 5 });
        await page.mouse.up();
        const boxAfterDrag1 = await dragHandle.boundingBox();
        console.log(`[TEST EC10] boxAfterDrag1: y = ${boxAfterDrag1!.y}`);

        // 3. Chuyển sang tab "Ánh xạ"
        await dialog.getByRole('tab', { name: 'Ánh xạ' }).click();

        // 4. BẮT BUỘC: Thanh tiêu đề dialog (.el-dialog__header) phải nằm trong viewport màn hình (y >= 0)
        const headerAfterSwitch = await dragHandle.boundingBox();
        expect(headerAfterSwitch).not.toBeNull();
        expect(headerAfterSwitch!.y).toBeGreaterThanOrEqual(0);
        await expect(dragHandle).toBeVisible();

        // 5. Kiểm tra vẫn kéo thả được modal sau khi chuyển tab
        const beforeDragY = headerAfterSwitch!.y;
        await page.mouse.move(headerAfterSwitch!.x + headerAfterSwitch!.width / 2, headerAfterSwitch!.y + headerAfterSwitch!.height / 2);
        await page.mouse.down();
        await page.mouse.move(headerAfterSwitch!.x + headerAfterSwitch!.width / 2, headerAfterSwitch!.y + 60, { steps: 5 });
        await page.mouse.up();

        const movedHeaderBox = await dragHandle.boundingBox();
        expect(movedHeaderBox).not.toBeNull();
        console.log(`[TEST EC10] beforeDragY = ${beforeDragY}, movedHeaderBox.y = ${movedHeaderBox!.y}`);
        expect(movedHeaderBox!.y).toBeGreaterThan(beforeDragY);
    });

    test('EC11 (Kiểm tra bug giật toạ độ khi kéo) — Sau khi chuyển sang tab Ánh xạ, thao tác kéo chuột BẮT BUỘC di chuyển mượt mà không bị giật toạ độ', async ({ page }) => {
        await page.goto('/#/sharedata/mapping');
        const addBtn = page.locator('button:has(.ele-Plus), button:has-text("Thêm mới"), button:has-text("Thêm")').first();
        await expect(addBtn).toBeVisible({ timeout: 60000 });
        await addBtn.click();

        const dialog = page.locator('.mp-dialog');
        await expect(dialog).toBeVisible({ timeout: 5000 });
        const dragHandle = dialog.locator('.el-dialog__header');
        await expect(dragHandle).toBeVisible();

        // 1. Kéo modal lên sát mép trên viewport ở tab Thông tin chung
        const handleBox = await dragHandle.boundingBox();
        expect(handleBox).not.toBeNull();
        await page.mouse.move(handleBox!.x + handleBox!.width / 2, handleBox!.y + handleBox!.height / 2);
        await page.mouse.down();
        await page.mouse.move(handleBox!.x + handleBox!.width / 2, handleBox!.y - 100, { steps: 5 });
        await page.mouse.up();

        // 2. Chuyển sang tab "Ánh xạ"
        await dialog.getByRole('tab', { name: 'Ánh xạ' }).click();
        await page.waitForTimeout(300);

        const headerAfterSwitch = await dragHandle.boundingBox();
        expect(headerAfterSwitch).not.toBeNull();
        expect(headerAfterSwitch!.y).toBeGreaterThanOrEqual(0);

        // 3. Thao tác kéo chuột ở tab Ánh xạ: kéo chuột xuống 60px
        const beforeDragY = headerAfterSwitch!.y;
        await page.mouse.move(headerAfterSwitch!.x + headerAfterSwitch!.width / 2, headerAfterSwitch!.y + headerAfterSwitch!.height / 2);
        await page.mouse.down();
        await page.mouse.move(headerAfterSwitch!.x + headerAfterSwitch!.width / 2, headerAfterSwitch!.y + 60, { steps: 5 });
        await page.mouse.up();

        const movedHeaderBox = await dragHandle.boundingBox();
        expect(movedHeaderBox).not.toBeNull();
        // Xác nhận vị trí header di chuyển tịnh tiến xuống dưới, KHÔNG bị giật ngược
        expect(movedHeaderBox!.y).toBeGreaterThan(beforeDragY);
    });

    test('EC12 — Kiểm tra phân cấp 2 tầng Tab trong editMapping.vue: Tab cấp trên (.mp-direction-tabs) trượt active-bar chuẩn TMS và Sub-tab (.mp-tabs line sticky)', async ({ page }) => {
        test.slow();
        await page.goto('/#/sharedata/mapping');
        const addBtn = page.locator('button:has(.ele-Plus), button:has-text("Thêm mới"), button:has-text("Thêm")').first();
        await expect(addBtn).toBeVisible({ timeout: 60000 });
        await addBtn.click();

        const dialog = page.locator('.mp-dialog');
        await expect(dialog).toBeVisible({ timeout: 5000 });

        // 1. Kiểm tra Tầng 1: Tab cấp trên (.mp-direction-tabs) chuẩn TMS với 3 chiều và active-bar trượt
        const topTabs = dialog.locator('.mp-direction-tabs');
        await expect(topTabs).toBeVisible();
        await expect(topTabs).not.toHaveClass(/el-tabs--border-card/);

        const tabOutbound = topTabs.getByRole('tab', { name: 'Gửi đi' });
        const tabInbound = topTabs.getByRole('tab', { name: 'Nhận về' });
        const tabBoth = topTabs.getByRole('tab', { name: 'Hai chiều' });
        await expect(tabOutbound).toHaveClass(/is-active/);

        const topActiveBar = topTabs.locator('.el-tabs__active-bar');
        await expect(topActiveBar).toBeVisible();
        const initialTopBarBox = await topActiveBar.boundingBox();
        expect(initialTopBarBox).not.toBeNull();

        await tabInbound.click();
        await expect(tabInbound).toHaveClass(/is-active/);
        await page.waitForTimeout(300);
        const movedTopBarBox = await topActiveBar.boundingBox();
        expect(movedTopBarBox).not.toBeNull();
        expect(movedTopBarBox!.x).toBeGreaterThan(initialTopBarBox!.x + 20);

        await tabOutbound.click();
        await expect(tabOutbound).toHaveClass(/is-active/);
        await page.waitForTimeout(300);

        await expect(tabInbound).toBeVisible();
        await expect(tabBoth).toBeVisible();

        // 2. Kiểm tra Tầng 2: Sub-tab (.mp-tabs) kiểu line (không phải border-card) và sticky header
        const subTabs = dialog.locator('.mp-tabs');
        await expect(subTabs).toBeVisible();
        await expect(subTabs).not.toHaveClass(/el-tabs--border-card/);
        const subHeaderPos = await subTabs.locator('.el-tabs__header').evaluate((el) => window.getComputedStyle(el).position);
        expect(subHeaderPos).toBe('sticky');

        // Chọn Đối tác + Gói tin ở tab Thông tin chung để hiển thị mã sinh tự động
        const partnerSelect = dialog.locator('.el-select').first();
        await partnerSelect.click();
        const subResponsePromise = page.waitForResponse(
            (resp) => resp.url().includes('/api/sharedata/sharedatasubscription/list') && resp.status() === 200
        );
        await page.locator('.el-popper:visible .el-select-dropdown__item', { hasText: 'DT01' }).first().click();
        await subResponsePromise;
        await page.waitForTimeout(300);

        const packetSelect = dialog.locator('.el-select').nth(1);
        await expect(packetSelect).not.toHaveClass(/is-disabled/);
        await packetSelect.click();
        await page.locator('.el-popper:visible .el-select-dropdown__item', { hasText: '101_commonData' }).first().click();
        await page.waitForTimeout(200);

        // Chụp snapshot Modal Hồ sơ ánh xạ ở Sub-tab Thông tin chung
        await page.screenshot({ path: 'test-results/snapshot-3a-modal-mapping-general-tab.png', fullPage: false });

        const activeBar = subTabs.locator('.el-tabs__active-bar');
        await expect(activeBar).toBeVisible();
        const initialBarBox = await activeBar.boundingBox();
        expect(initialBarBox).not.toBeNull();
        expect(initialBarBox!.width).toBeGreaterThan(0);

        // Chuyển sang Sub-tab Ánh xạ, phân tích JSON và cuộn xuống để thấy sticky sub-tab dưới top-level tab
        const mappingSubTab = subTabs.getByRole('tab', { name: 'Ánh xạ' });
        await mappingSubTab.click();
        await expect(mappingSubTab).toHaveClass(/is-active/);
        await page.waitForTimeout(300);

        // Kiểm tra thanh gạch dưới là thanh trượt di chuyển (move slider) theo toạ độ X sang phải về phía tab Ánh xạ
        const movedBarBox = await activeBar.boundingBox();
        expect(movedBarBox).not.toBeNull();
        expect(movedBarBox!.x).toBeGreaterThan(initialBarBox!.x + 20);

        // Đảm bảo không dùng pseudo-element ::after scaleX để vẽ gạch chân giả lập (không tăng width từ tâm)
        const subTabAfter = await mappingSubTab.evaluate((el) => {
            const after = window.getComputedStyle(el, '::after');
            return after.content === 'none' || after.height === 'auto' || after.height === '0px';
        });
        expect(subTabAfter).toBe(true);

        // Chuyển tiếp sang Sub-tab Dữ liệu gửi thử để kiểm tra thanh tiếp tục trượt di chuyển sang phải
        const testSubTab = subTabs.getByRole('tab', { name: 'Dữ liệu gửi thử' });
        await testSubTab.click();
        await expect(testSubTab).toHaveClass(/is-active/);
        await page.waitForTimeout(300);
        const testBarBox = await activeBar.boundingBox();
        expect(testBarBox).not.toBeNull();
        expect(testBarBox!.x).toBeGreaterThan(movedBarBox!.x + 20);

        // Chuyển ngược lại về tab Ánh xạ để tiếp tục các bước phân tích JSON tiếp theo
        await mappingSubTab.click();
        await expect(mappingSubTab).toHaveClass(/is-active/);
        await page.waitForTimeout(300);
        const backBarBox = await activeBar.boundingBox();
        expect(backBarBox).not.toBeNull();
        expect(backBarBox!.x).toBeLessThan(testBarBox!.x - 20);

        await partnerJsonTextarea(dialog).fill(genBigJson(20));
        await dialog.getByRole('button', { name: 'Phân tích' }).click();
        await page.waitForTimeout(500);
        await scrollFormLocator(page).evaluate((el) => { el.scrollTop = 120; });
        await page.waitForTimeout(200);

        await page.screenshot({ path: 'test-results/snapshot-3b-modal-mapping-subtab-sticky.png', fullPage: false });
        expect(await isHeaderWithinFormViewport(page)).toBe(true);
    });

    test('EC13 — Kiểm tra hồ sơ ánh xạ đang dùng đặt trực tiếp tại mapping/index.vue (Colocation chuẩn Clean Code)', async () => {
        // 1. Kiểm tra logic thuần túy của hàm isMappingInUse
        const isMappingInUseFn = (
            mapping: { isActive?: boolean | null; partnerId?: string | null; datatypeId?: string | null; direction?: number | null },
            activeSubscriptions: Array<{ partnerId?: string | null; datatypeId?: string | null; direction?: number | null }>
        ): boolean => {
            if (mapping.isActive !== true) return false;
            if (!mapping.partnerId || !mapping.datatypeId) return false;

            return activeSubscriptions.some((sub) => {
                if (sub.partnerId !== mapping.partnerId) return false;
                if (sub.datatypeId !== mapping.datatypeId) return false;
                return mapping.direction === 2 || sub.direction === mapping.direction;
            });
        };

        const activeSubs = [
            { partnerId: 'DT01', datatypeId: '101', direction: 0 },
            { partnerId: 'DT02', datatypeId: '102', direction: 1 },
        ];

        // Case 1: isActive = false -> false (không bị coi là đang dùng)
        expect(isMappingInUseFn({ isActive: false, partnerId: 'DT01', datatypeId: '101', direction: 0 }, activeSubs)).toBe(false);

        // Case 2: thiếu partnerId hoặc datatypeId -> false
        expect(isMappingInUseFn({ isActive: true, partnerId: '', datatypeId: '101', direction: 0 }, activeSubs)).toBe(false);
        expect(isMappingInUseFn({ isActive: true, partnerId: 'DT01', datatypeId: '', direction: 0 }, activeSubs)).toBe(false);

        // Case 3: danh sách activeSubscriptions rỗng -> false
        expect(isMappingInUseFn({ isActive: true, partnerId: 'DT01', datatypeId: '101', direction: 0 }, [])).toBe(false);

        // Case 4: khớp đầy đủ partnerId, datatypeId, direction -> true
        expect(isMappingInUseFn({ isActive: true, partnerId: 'DT01', datatypeId: '101', direction: 0 }, activeSubs)).toBe(true);
        expect(isMappingInUseFn({ isActive: true, partnerId: 'DT02', datatypeId: '102', direction: 1 }, activeSubs)).toBe(true);

        // Case 5: khác direction (mapping direction = 1 Inbound, sub direction = 0 Outbound) -> false
        expect(isMappingInUseFn({ isActive: true, partnerId: 'DT01', datatypeId: '101', direction: 1 }, activeSubs)).toBe(false);

        // Case 6: mapping direction = 2 (Cả hai chiều) -> khớp với bất kỳ subscription nào cùng partner + datatype -> true
        expect(isMappingInUseFn({ isActive: true, partnerId: 'DT01', datatypeId: '101', direction: 2 }, activeSubs)).toBe(true);

        // Case 7: khác partnerId hoặc khác datatypeId -> false
        expect(isMappingInUseFn({ isActive: true, partnerId: 'DT99', datatypeId: '101', direction: 0 }, activeSubs)).toBe(false);
        expect(isMappingInUseFn({ isActive: true, partnerId: 'DT01', datatypeId: '999', direction: 0 }, activeSubs)).toBe(false);

        // 2. Quét tĩnh tệp views/sharedata/mapping/index.vue và đảm bảo mappingUsage.ts đã được xóa sạch
        const mappingUsagePath = resolveWebVueSrc('utils/sharedata/mappingUsage.ts');
        expect(fs.existsSync(mappingUsagePath)).toBe(false);

        const mappingIndexPath = resolveWebVueSrc('views/sharedata/mapping/index.vue');
        const mappingIndexContent = fs.readFileSync(mappingIndexPath, 'utf8');
        expect(mappingIndexContent).toContain('const isMappingInUse');
        expect(mappingIndexContent).toContain('const getActiveSubscriptions');
        expect(mappingIndexContent).toContain('mapping.direction === Direction.NUMBER_2');
        expect(mappingIndexContent).not.toContain('mappingUsage');
        expect(mappingIndexContent).not.toContain('!important');
    });
});


