import { test, expect, type Locator, type Page, type Route } from '@playwright/test';
import { mockAuth } from '../../setup';

/**
 * Test E2E cho phân hệ ShareData — trang Mốc gửi (cursor/index.vue):
 * xem mốc từng đối tác theo gói, đặt mốc, đặt lại mốc và khóa khi đăng ký gửi đang bật.
 */

/** Lỗi của khung layout dùng chung (layout/main/defaults.vue gọi layoutMainScrollbarRef khi chưa gắn) nằm ngoài ShareData: không tính vào lỗi trang. */
const isSharedLayoutError = (message: string) => message.includes('layoutMainScrollbarRef');

const fulfillJson = (route: Route, result: unknown) =>
    route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ code: 200, result }) });

const pageSizeItem = { code: 'pageSize', valueKind: 'Const', value: '100', dataType: 'Int' };
const keyItem = (savedValue: string | null, code = 'lastKey') => ({ code, valueKind: 'Builtin', value: 'lastKey', dataType: 'String', savedValue });

const PLAIN_KEY_SQL = 'SELECT TOP (@pageSize) ID AS __lastKey FROM TmsZone WHERE ID > @lastKey ORDER BY __lastKey ASC';
const TIME_KEY_SQL = "SELECT TOP (@pageSize) CONVERT(varchar(23), UpdateTime, 121) + '_' + ID AS __lastKey FROM TmsZone WHERE CONVERT(varchar(23), UpdateTime, 121) + '_' + ID > @lastKey ORDER BY __lastKey ASC";

const partnerIdOf = (route: Route) => {
    const url = new URL(route.request().url());
    return url.searchParams.get('PartnerId') ?? url.searchParams.get('partnerId') ?? '';
};
const packetCodeOf = (route: Route) => {
    const url = new URL(route.request().url());
    return url.searchParams.get('PacketCode') ?? url.searchParams.get('packetCode') ?? '';
};

const mockPartners = (page: Page, partners: unknown[]) => page.route('**/api/sharedata/sharedatapacket/getpartnersqllist*', (route) => fulfillJson(route, partners));
const mockVariables = (page: Page, itemsOf: (partnerId: string, route: Route) => unknown[]) =>
    page.route('**/api/sharedata/sharedatapacket/getsqlvariables*', (route) => fulfillJson(route, { items: itemsOf(partnerIdOf(route), route) }));
const captureSetLastSend = async (page: Page) => {
    const payloads: any[] = [];
    await page.route('**/api/sharedata/sharedatapacket/setlastsend*', (route) => {
        payloads.push(route.request().postDataJSON());
        return fulfillJson(route, null);
    });
    return payloads;
};

const pickTime = async (page: Page, scope: Locator, text: string) => {
    await scope.locator('.cvi-picker input').fill(text);
    await page.keyboard.press('Enter');
};

const gridRow = (page: Page, text: string) => page.locator('.sharedata-cursor-container .vxe-table--main-wrapper .vxe-body--row', { hasText: text });
const gotoCursorPage = async (page: Page) => {
    await page.setViewportSize({ width: 1600, height: 950 });
    await page.goto('/#/sharedata/cursor');
    await expect(page.locator('.sharedata-cursor-container')).toBeVisible({ timeout: 30_000 });
};

test.describe('ShareData — Trang Mốc gửi (cursor) @sharedata', () => {
    let pageErrors: string[];

    test.beforeEach(async ({ context, page }) => {
        pageErrors = [];
        page.on('pageerror', (err) => {
            if (!isSharedLayoutError(err.message)) pageErrors.push(err.message);
        });

        await mockAuth(context, page, { userInfo: { defaultMenu: '/sharedata/cursor' } });

        await page.route('**/api/system/sysmenu/loginmenutree*', (route) =>
            fulfillJson(route, [
                {
                    id: 'menu_sharedata',
                    pid: '0',
                    path: '/sharedata',
                    name: 'sharedata',
                    component: 'layout/routerView/parent',
                    meta: { title: 'lz.menu.sharedata', isLink: '', isHide: false, isKeepAlive: true, isAffix: false, isIframe: false, roles: ['admin'], icon: 'ele-Share' },
                    children: [
                        {
                            id: 'menu_sharedata_cursor',
                            pid: 'menu_sharedata',
                            path: '/sharedata/cursor',
                            name: 'shareDataCursor',
                            component: 'sharedata/cursor/index',
                            meta: { title: 'Mốc gửi', isLink: '', isHide: false, isKeepAlive: true, isAffix: false, isIframe: false, roles: ['admin'], icon: 'ele-Flag' },
                        },
                    ],
                },
            ])
        );

        await page.route('**/api/sharedata/sharedatapacket/page*', (route) =>
            fulfillJson(route, {
                records: [
                    { id: 'PKT_01', code: '101', name: 'Gói 101', status: 1 },
                    { id: 'PKT_02', code: '102', name: 'Gói 102', status: 1 },
                ],
                total: 2,
            })
        );
        await page.route('**/api/sharedata/sharedatasubscription/list*', (route) => fulfillJson(route, []));
        await page.route('**/api/sharedata/sharedatapacket/getpartnerextractsql*', (route) => fulfillJson(route, { standardSql: TIME_KEY_SQL }));
    });

    test.afterEach(() => {
        expect(pageErrors).toEqual([]);
    });

    test('1. Xem mốc từng đối tác, đặt mốc rồi đặt lại mốc: bảng nạp lại đúng trạng thái sau mỗi thao tác', async ({ page }) => {
        const saved: Record<string, string | null> = { p_a: '2026-10-27 10:05:00.000_Z02', p_b: null };
        const resetPayloads: any[] = [];
        await mockPartners(page, [
            { partnerId: 'p_a', partnerCode: 'CSGT', partnerName: 'Cục CSGT', isCustom: true },
            { partnerId: 'p_b', partnerCode: 'VDOT', partnerName: 'Sở GTVT', isCustom: false },
        ]);
        await mockVariables(page, (partnerId) => [pageSizeItem, keyItem(saved[partnerId] ?? null)]);
        await page.route('**/api/sharedata/sharedatapacket/setlastsend*', (route) => {
            const body = route.request().postDataJSON();
            saved[body.partnerId] = body.lastKey;
            return fulfillJson(route, null);
        });
        await page.route('**/api/sharedata/sharedatapacket/resetlastsend*', (route) => {
            const body = route.request().postDataJSON();
            resetPayloads.push(body);
            saved[body.partnerId] = null;
            return fulfillJson(route, true);
        });

        await gotoCursorPage(page);

        // Bảng: đối tác có mốc hiện mốc, đối tác chưa có mốc nói rõ lần gửi tới lấy từ đâu; chế độ SQL theo cấu hình đối tác
        const rowA = gridRow(page, 'CSGT');
        const rowB = gridRow(page, 'VDOT');
        await expect(rowA).toContainText('2026-10-27 10:05:00.000_Z02', { timeout: 10_000 });
        await expect(rowA).toContainText('Cục CSGT');
        await expect(rowB).toContainText('lần gửi tới lấy theo tham số của gói tin');

        // Chưa có mốc thì không đặt lại được, rê chuột thấy lý do
        await expect(rowB.locator('.sd-cursor-reset-btn')).toBeDisabled();
        await rowB.locator('.sd-cursor-btn-wrapper').nth(1).hover();
        await expect(page.locator('.el-popper:visible', { hasText: 'Chưa có mốc gửi dữ liệu để đặt lại' })).toBeVisible({ timeout: 5_000 });

        // Đặt mốc cho VDOT: nút đã dịch, gửi đúng gói và đối tác, bảng hiện mốc mới
        const setBtn = rowB.locator('.sd-cursor-set-btn');
        await expect(setBtn).toContainText('Đặt mốc');
        await setBtn.click();
        const dialog = page.locator('.sd-set-cursor-dialog');
        await expect(dialog).toBeVisible();
        await pickTime(page, dialog, '02/11/2026 08:00:00');
        await dialog.locator('.el-dialog__footer .el-button--primary').click();
        await expect(dialog).toBeHidden();
        await expect(page.locator('.el-message--success')).toBeVisible();
        await expect(rowB).toContainText('2026-11-02 08:00:00.000_');

        // Đặt lại mốc của CSGT: có xác nhận, gửi đúng đối tác, bảng về trạng thái chưa có mốc
        await rowA.locator('.sd-cursor-reset-btn').click();
        await page.locator('.el-message-box__btns .el-button--primary').click();
        await expect.poll(() => resetPayloads.length).toBe(1);
        expect(resetPayloads[0]).toEqual({ packetCode: '101', partnerId: 'p_a' });
        await expect(rowA).toContainText('lần gửi tới lấy theo tham số của gói tin');
    });

    const directionCases = [
        { name: 'chiều gửi', direction: 0, locked: true },
        { name: 'hai chiều', direction: 2, locked: true },
        { name: 'chiều nhận', direction: 1, locked: false },
    ];
    for (const { name, direction, locked } of directionCases) {
        test(`2. Đăng ký đang bật ${name}: ${locked ? 'khóa' : 'không khóa'} Đặt mốc và Đặt lại của đối tác đó`, async ({ page }) => {
            await page.route('**/api/sharedata/sharedatasubscription/list*', (route) => fulfillJson(route, [{ partnerId: 'p_a', direction }]));
            await mockPartners(page, [
                { partnerId: 'p_a', partnerCode: 'CSGT', partnerName: 'Cục CSGT', isCustom: false },
                { partnerId: 'p_b', partnerCode: 'VDOT', partnerName: 'Sở GTVT', isCustom: false },
            ]);
            await mockVariables(page, () => [keyItem('k_100')]);

            await gotoCursorPage(page);
            const rowA = gridRow(page, 'CSGT');
            await expect(rowA).toContainText('k_100', { timeout: 10_000 });

            if (locked) {
                await expect(rowA.locator('.sd-cursor-set-btn')).toBeDisabled();
                await expect(rowA.locator('.sd-cursor-reset-btn')).toBeDisabled();
                await rowA.locator('.sd-cursor-btn-wrapper').first().hover();
                await expect(page.locator('.el-popper:visible', { hasText: 'Tắt đăng ký trước khi đặt mốc' })).toBeVisible({ timeout: 5_000 });
            } else {
                await expect(rowA.locator('.sd-cursor-set-btn')).toBeEnabled();
                await expect(rowA.locator('.sd-cursor-reset-btn')).toBeEnabled();
            }

            // Đối tác không có đăng ký luôn đặt được
            const rowB = gridRow(page, 'VDOT');
            await expect(rowB.locator('.sd-cursor-set-btn')).toBeEnabled();
            await expect(rowB.locator('.sd-cursor-reset-btn')).toBeEnabled();
        });
    }

    test('3. Câu một chuỗi: hộp Đặt mốc chỉ có ô ngày, hiện mốc đang chạy, chưa đổi thì không Xác nhận được', async ({ page }) => {
        await mockPartners(page, [{ partnerId: 'p_shared1', partnerCode: 'SHARED1', partnerName: 'Đối tác dùng mẫu chung', isCustom: false }]);
        await mockVariables(page, () => [pageSizeItem, keyItem('2026-10-28 08:00:00.000_k_1')]);
        const setPayloads = await captureSetLastSend(page);

        await gotoCursorPage(page);
        await expect(gridRow(page, 'SHARED1')).toContainText('k_1', { timeout: 10_000 });

        const dialog = page.locator('.sd-set-cursor-dialog');
        const confirmBtn = dialog.locator('.el-dialog__footer .el-button--primary');

        // Câu một chuỗi: không có ô gõ tay; hộp hiện mốc đang chạy và điền sẵn ngày của mốc đó, chưa đổi thì không Xác nhận được
        await gridRow(page, 'SHARED1').locator('.sd-cursor-set-btn').click();
        await expect(dialog).toBeVisible();
        await expect(dialog.locator('input[type="text"]:not(.el-date-editor input)')).toHaveCount(0);
        await expect(dialog.locator('.sd-cursor-current')).toContainText('2026-10-28 08:00:00.000_k_1');
        await expect(dialog.locator('.cvi-picker input')).toHaveValue('28/10/2026 08:00:00');
        await expect(confirmBtn).toBeDisabled();
        await pickTime(page, dialog, '29/10/2026 08:00:00');
        await expect(confirmBtn).toBeEnabled();
        await confirmBtn.click();
        await expect.poll(() => setPayloads.length).toBe(1);
        expect(setPayloads[0]).toEqual({ packetCode: '101', partnerId: 'p_shared1', lastKey: '2026-10-29 08:00:00.000_' });
    });

    test('7. Đặt mốc: câu ghép kiểu 121 chọn ngày giờ thì tự ghép chuỗi mốc; câu dạng khác không đặt được, báo cần sửa câu', async ({ page }) => {
        await mockPartners(page, [
            { partnerId: 'p_time', partnerCode: 'TIME1', partnerName: 'Đối tác ghép thời gian', isCustom: true },
            { partnerId: 'p_plain', partnerCode: 'PLAIN1', partnerName: 'Đối tác chỉ khóa', isCustom: true },
        ]);
        await mockVariables(page, () => [pageSizeItem, keyItem(null)]);
        await page.route('**/api/sharedata/sharedatapacket/getpartnerextractsql*', (route) =>
            fulfillJson(route, { customSql: partnerIdOf(route) === 'p_time' ? TIME_KEY_SQL : PLAIN_KEY_SQL })
        );
        const setPayloads = await captureSetLastSend(page);

        await gotoCursorPage(page);
        const dialog = page.locator('.sd-set-cursor-dialog');
        await gridRow(page, 'TIME1').locator('.sd-cursor-set-btn').click();
        await pickTime(page, dialog, '29/10/2026 08:30:00');
        await dialog.locator('.el-dialog__footer .el-button--primary').click();
        await expect.poll(() => setPayloads.length).toBe(1);
        expect(setPayloads[0]).toEqual({ packetCode: '101', partnerId: 'p_time', lastKey: '2026-10-29 08:30:00.000_' });

        await expect(dialog).toBeHidden();
        await gridRow(page, 'PLAIN1').locator('.sd-cursor-set-btn').click();
        await expect(dialog.locator('.sd-cursor-no-time')).toBeVisible();
        await expect(dialog.locator('.cvi-picker')).toHaveCount(0);
        await expect(dialog.locator('.el-dialog__footer .el-button--primary')).toBeDisabled();
    });

    test('4. Câu gộp nhiều nguồn: bảng hiện mốc từng nhóm, Đặt mốc gửi riêng từng nhóm kèm cursorKey', async ({ page }) => {
        await mockPartners(page, [{ partnerId: 'p_grp', partnerCode: 'GRP', partnerName: 'Đối tác nhiều nguồn', isCustom: false }]);
        await mockVariables(page, () => [pageSizeItem, keyItem('2026-10-27 09:30:00.000_a2', 'lastKey_pkt'), keyItem('2026-10-27 09:20:00.000_t9', 'lastKey_toll')]);
        const setPayloads = await captureSetLastSend(page);

        await gotoCursorPage(page);
        const row = gridRow(page, 'GRP');
        await expect(row).toContainText('pkt: 2026-10-27 09:30:00.000_a2', { timeout: 10_000 });
        await expect(row).toContainText('toll: 2026-10-27 09:20:00.000_t9');

        await row.locator('.sd-cursor-set-btn').click();
        const dialog = page.locator('.sd-set-cursor-dialog');
        await expect(dialog.locator('.sd-cursor-section')).toHaveCount(2);
        const tollSection = dialog.locator('.sd-cursor-section', { hasText: '__lastKey_toll' });
        await pickTime(page, tollSection, '27/10/2026 09:25:00');
        await dialog.locator('.el-dialog__footer .el-button--primary').click();
        await expect(dialog).toBeHidden();

        // Nhóm không chọn ngày giờ thì giữ mốc đang chạy
        await expect.poll(() => setPayloads.length).toBe(1);
        expect(setPayloads[0]).toEqual({ packetCode: '101', partnerId: 'p_grp', cursorKey: 'toll', lastKey: '2026-10-27 09:25:00.000_' });
    });

    test('6. Bấm Đặt mốc khi mã hộp thoại còn đang tải: hộp thoại vẫn mở, không mất cú bấm', async ({ page }) => {
        await mockPartners(page, [{ partnerId: 'p_a', partnerCode: 'CSGT', partnerName: 'Cục CSGT', isCustom: false }]);
        await mockVariables(page, () => [keyItem('k_100')]);
        await page.route('**/views/sharedata/cursor/component/setCursorDialog.vue*', async (route) => {
            await new Promise((resolve) => setTimeout(resolve, 3_000));
            await route.continue();
        });

        await gotoCursorPage(page);
        const row = gridRow(page, 'CSGT');
        await expect(row).toContainText('k_100', { timeout: 30_000 });

        await row.locator('.sd-cursor-set-btn').click();
        await expect(page.locator('.sd-set-cursor-dialog')).toBeVisible({ timeout: 10_000 });
    });

    test('5. Đổi gói tin: bảng nạp lại đúng đối tác và mốc của gói mới, đặt mốc gửi theo gói đang chọn', async ({ page }) => {
        await page.route('**/api/sharedata/sharedatapacket/getpartnersqllist*', (route) => {
            const code = packetCodeOf(route) || route.request().url();
            return fulfillJson(
                route,
                code.includes('102') ? [{ partnerId: 'p_b', partnerCode: 'VDOT', partnerName: 'Sở GTVT', isCustom: false }] : [{ partnerId: 'p_a', partnerCode: 'CSGT', partnerName: 'Cục CSGT', isCustom: false }]
            );
        });
        await mockVariables(page, (partnerId) => [keyItem(partnerId === 'p_a' ? 'k_101' : 'k_102')]);
        const setPayloads = await captureSetLastSend(page);

        await gotoCursorPage(page);
        await expect(gridRow(page, 'CSGT')).toContainText('k_101', { timeout: 10_000 });

        await page.locator('.sd-cursor-packet').click();
        await page.locator('.el-select-dropdown__item:visible', { hasText: '102' }).click();
        await expect(gridRow(page, 'VDOT')).toContainText('k_102', { timeout: 10_000 });
        await expect(gridRow(page, 'CSGT')).toHaveCount(0);

        await gridRow(page, 'VDOT').locator('.sd-cursor-set-btn').click();
        const dialog = page.locator('.sd-set-cursor-dialog');
        await pickTime(page, dialog, '29/10/2026 09:00:00');
        await dialog.locator('.el-dialog__footer .el-button--primary').click();
        await expect.poll(() => setPayloads.length).toBe(1);
        expect(setPayloads[0]).toEqual({ packetCode: '102', partnerId: 'p_b', lastKey: '2026-10-29 09:00:00.000_' });
    });
});
