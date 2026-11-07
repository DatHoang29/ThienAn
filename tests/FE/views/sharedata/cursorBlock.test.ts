import { test, expect, type Locator, type Page, type Route } from '@playwright/test';
import { mockAuth } from '../../setup';

/**
 * Test E2E cho phân hệ ShareData — khối 4 "Mốc đang chạy" ở trang Cấu hình gói tin (dataSource/component/cursorBlock.vue):
 * mốc đang chạy theo từng đối tác trong phạm vi tab đang mở (tab Mẫu chung: đối tác dùng mẫu chung; tab đối tác: đúng đối tác đó),
 * Đặt mốc, Đặt lại và khóa khi đăng ký gửi đang bật.
 */

/** Lỗi của khung layout dùng chung (layout/main/defaults.vue gọi layoutMainScrollbarRef khi chưa gắn) nằm ngoài ShareData: không tính vào lỗi trang. */
const isSharedLayoutError = (message: string) => message.includes('layoutMainScrollbarRef');

const json = (body: unknown) => ({ status: 200, contentType: 'application/json', body: JSON.stringify(body) });
const fulfillJson = (route: Route, result: unknown) => route.fulfill(json({ code: 200, result }));

const PACKET_CODE = '101_commonData';
const CURSOR_SQL =
    "SELECT TOP (@pageSize) ID AS zoneId, CONVERT(varchar(23), COALESCE(UpdateTime, CreateTime), 121) + '_' + ID AS __lastKey FROM TmsZone " +
    "WHERE CONVERT(varchar(23), COALESCE(UpdateTime, CreateTime), 121) + '_' + ID > @lastKey ORDER BY __lastKey ASC";
const SNAPSHOT_SQL = 'SELECT TOP (@pageSize) ID AS zoneId FROM TmsZone';
const PLAIN_KEY_SQL = 'SELECT TOP (@pageSize) ID AS __lastKey FROM TmsZone WHERE ID > @lastKey ORDER BY __lastKey ASC';

const pageSizeItem = { code: 'pageSize', valueKind: 'Const', value: '100', dataType: 'Int' };
const keyItem = (savedValue: string | null, defaultValue: string | null = null, code = 'lastKey') => ({ code, valueKind: 'Builtin', value: 'lastKey', dataType: 'String', savedValue, defaultValue });

const partnerIdOf = (route: Route) => {
    const url = new URL(route.request().url());
    return url.searchParams.get('PartnerId') ?? url.searchParams.get('partnerId') ?? '';
};

const mockPartners = (page: Page, partners: unknown[]) => page.route('**/api/sharedata/sharedatapacket/getpartnersqllist*', (route) => fulfillJson(route, partners));
/** Bộ biến theo đối tác (partnerId rỗng là bộ biến mẫu chung của khối 3). */
const mockVariables = (page: Page, itemsOf: (partnerId: string) => unknown[]) =>
    page.route('**/api/sharedata/sharedatapacket/getsqlvariables*', (route) => fulfillJson(route, { items: itemsOf(partnerIdOf(route)) }));
const mockExtractSql = (page: Page, sql: string) =>
    page.route('**/api/sharedata/sharedatapacket/getextractsql*', (route) => fulfillJson(route, { extractSql: sql, status: 1, fields: [] }));
const captureSetLastSend = async (page: Page) => {
    const payloads: any[] = [];
    await page.route('**/api/sharedata/sharedatapacket/setlastsend*', (route) => {
        payloads.push(route.request().postDataJSON());
        return fulfillJson(route, null);
    });
    return payloads;
};

const block = (page: Page) => page.locator('.sd-cursor-block');
const blockRow = (page: Page, text: string) => block(page).locator('.el-table__body tr', { hasText: text });
const pickTime = async (page: Page, scope: Locator, text: string) => {
    await scope.locator('.cvi-picker input').fill(text);
    await page.keyboard.press('Enter');
};

/** Mở trang Cấu hình gói tin, chọn gói đầu tiên và chờ khung soạn SQL sẵn sàng. */
const openSqlCenter = async (page: Page, viewport = { width: 1600, height: 1000 }) => {
    await page.setViewportSize(viewport);
    await page.goto('/#/sharedata/dataSource');
    await page.locator('.packet-col .vxe-body--row').first().click({ timeout: 60_000 });
    await page.locator('.sql-monaco-editor .monaco-editor').first().waitFor({ timeout: 60_000 });
};

test.describe('ShareData — Khối 4 Mốc đang chạy (cursorBlock) @sharedata', () => {
    let pageErrors: string[];

    test.beforeEach(async ({ context, page }) => {
        pageErrors = [];
        page.on('pageerror', (err) => {
            if (!isSharedLayoutError(err.message)) pageErrors.push(err.message);
        });

        await mockAuth(context, page, { userInfo: { defaultMenu: '/sharedata/dataSource', account: 'admin', accountType: '111' } as any });
        await page.route('**/api/system/sysmenu/loginmenutree*', (route) =>
            fulfillJson(route, [
                {
                    id: 'm1',
                    pid: '0',
                    path: '/sharedata',
                    name: 'sharedata',
                    component: 'layout/routerView/parent',
                    meta: { title: 'lz.menu.sharedata', isLink: '', isHide: false, isKeepAlive: true, isAffix: false, isIframe: false, roles: ['admin'], icon: 'ele-Share' },
                    children: [
                        {
                            id: 'm2',
                            pid: 'm1',
                            path: '/sharedata/dataSource',
                            name: 'sharedataDataSource',
                            component: 'sharedata/dataSource/index',
                            meta: { title: 'lz.menu.sharedataDataSource', isLink: '', isHide: false, isKeepAlive: true, isAffix: false, isIframe: false, roles: ['admin'], icon: 'ele-Coin' },
                        },
                    ],
                },
            ])
        );
        await page.route('**/api/sharedata/sharedatapacket/page*', (route) =>
            fulfillJson(route, { records: [{ id: 'PKT_01', code: PACKET_CODE, name: 'Gói tin dùng chung 101', status: 1 }], total: 1 })
        );
        await page.route('**/api/sharedata/sharedatapacket/isinuse*', (route) => fulfillJson(route, false));
        await page.route('**/api/sharedata/sharedatamapping/page*', (route) => fulfillJson(route, { records: [], total: 0 }));
        await page.route('**/api/sharedata/sharedatapacket/getsqldetail*', (route) => fulfillJson(route, null));
        await page.route('**/api/sharedata/sharedatapacket/getwritesqls*', (route) =>
            fulfillJson(route, { packetCode: PACKET_CODE, writeSql: '', affectedTables: [], detectedTables: [], keyFields: [] })
        );
        await page.route('**/api/sharedata/sharedatapacket/parseextractcolumns*', (route) => fulfillJson(route, { isValid: true, columns: ['zoneId'] }));
        await page.route('**/api/sharedata/sharedatapacket/getmetadatatables*', (route) => fulfillJson(route, [{ id: 'tbl-zone', tableName: 'TmsZone' }]));
        await page.route('**/api/sharedata/sharedatapacket/getmetadatafields*', (route) => fulfillJson(route, [{ columnName: 'ID', dataType: 'nvarchar' }]));
        await page.route('**/api/sharedata/sharedatasubscription/list*', (route) => fulfillJson(route, []));
        await page.route('**/api/sharedata/sharedatapacket/getpartnerextractsql*', (route) =>
            fulfillJson(route, { partnerId: partnerIdOf(route), packetCode: PACKET_CODE, standardSql: CURSOR_SQL, customSql: null, isCustom: false, fields: [] })
        );
    });

    test.afterEach(() => {
        expect(pageErrors).toEqual([]);
    });

    /**
     * Author: DatHQ
     * Description: Tab Mẫu chung: khối 4 liệt kê đúng các đối tác dùng mẫu chung (đối tác có cấu hình riêng nằm ở tab của họ), hiện mốc đang chạy và lượt gửi tới;
     *              Đặt mốc cho đối tác chưa có mốc thì không báo hệ quả, Đặt lại hỏi rõ sẽ gửi lại từ Mốc bắt đầu, bảng nạp lại đúng sau mỗi thao tác.
     * Created date: 07/11/2026
     */
    test('1. Tab Mẫu chung: khối 4 liệt kê đối tác dùng mẫu chung kèm mốc đang chạy và lượt gửi tới; Đặt mốc, Đặt lại nạp lại đúng', async ({ page }) => {
        const saved: Record<string, string | null> = { p_a: '2026-10-27 10:05:00.000_Z02', p_b: null };
        const resetPayloads: any[] = [];
        await mockExtractSql(page, CURSOR_SQL);
        await mockPartners(page, [
            { partnerId: 'p_a', partnerCode: 'CSGT', partnerName: 'Cục CSGT', isCustom: false },
            { partnerId: 'p_b', partnerCode: 'VDOT', partnerName: 'Sở GTVT', isCustom: false },
            { partnerId: 'p_c', partnerCode: 'RIENG', partnerName: 'Đối tác cấu hình riêng', isCustom: true },
        ]);
        // Mẫu chung có Mốc bắt đầu 22/10/2026
        await mockVariables(page, (partnerId) => [pageSizeItem, keyItem(partnerId ? saved[partnerId] ?? null : null, '2026-10-22 00:00:00.000_')]);
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

        await openSqlCenter(page);
        await expect(block(page)).toContainText('4. Mốc đang chạy của đối tác dùng mẫu chung', { timeout: 15_000 });
        const rowA = blockRow(page, 'CSGT');
        const rowB = blockRow(page, 'VDOT');
        await expect(rowA).toContainText('27/10/2026 10:05:00.000', { timeout: 10_000 });
        await expect(rowA).toContainText('Z02');
        await expect(rowA).toContainText('Các dòng sau mốc đang chạy');
        await expect(rowB).toContainText('Chưa có mốc');
        await expect(rowB).toContainText('Từ 22/10/2026 00:00:00');
        await expect(rowB).toContainText('theo Mốc bắt đầu');
        await expect(blockRow(page, 'RIENG')).toHaveCount(0);

        // Chưa có mốc thì không đặt lại được, rê chuột thấy lý do
        await expect(rowB.locator('.sd-cursor-reset-btn')).toBeDisabled();
        await rowB.locator('.sd-cursor-btn-wrapper').nth(1).hover();
        await expect(page.locator('.el-popper:visible', { hasText: 'Chưa có mốc gửi dữ liệu để đặt lại' })).toBeVisible({ timeout: 5_000 });

        // Đặt mốc cho VDOT (chưa có mốc đang chạy): chọn ngày là chọn chỗ bắt đầu, không báo gửi trùng hay bỏ qua
        await rowB.locator('.sd-cursor-set-btn').click();
        const dialog = page.locator('.sd-set-cursor-dialog');
        await expect(dialog).toBeVisible();
        await pickTime(page, dialog, '02/11/2026 08:00:00');
        await expect(dialog.locator('.sd-cursor-effect')).toHaveCount(0);
        await dialog.locator('.el-dialog__footer .el-button--primary').click();
        await expect(dialog).toBeHidden();
        await expect(page.locator('.el-message--success')).toContainText('Đã cập nhật mốc gửi dữ liệu cho đối tác.');
        await expect(rowB).toContainText('02/11/2026 08:00:00.000');
        await expect(rowB).toContainText('đặt tay');
        await expect(rowB).toContainText('Các dòng sau mốc đang chạy');

        // Đặt lại CSGT: hỏi rõ gửi lại từ Mốc bắt đầu và bị gửi trùng, gửi đúng gói và đối tác, bảng về trạng thái chưa có mốc
        await rowA.locator('.sd-cursor-reset-btn').click();
        await expect(page.locator('.el-message-box')).toContainText(
            'Bỏ Mốc đang chạy của Cục CSGT? Lượt gửi tới gửi lại từ Mốc bắt đầu 22/10/2026 00:00:00, phần đã gửi sẽ bị gửi trùng.'
        );
        await page.locator('.el-message-box__btns .el-button--primary').click();
        await expect.poll(() => resetPayloads.length).toBe(1);
        expect(resetPayloads[0]).toEqual({ packetCode: PACKET_CODE, partnerId: 'p_a' });
        await expect(page.locator('.el-message--success').last()).toContainText('Đã bỏ Mốc đang chạy.');
        await expect(rowA).toContainText('Chưa có mốc');
        await expect(rowA).toContainText('Từ 22/10/2026 00:00:00');
    });

    /**
     * Author: DatHQ
     * Description: Đối tác có cấu hình riêng: mốc đang chạy của họ nằm ở tab của họ (khối 4 chỉ một dòng), không lẫn vào tab Mẫu chung.
     * Created date: 07/11/2026
     */
    test('2. Tab đối tác có cấu hình riêng: khối 4 chỉ có dòng của đối tác đó, tab Mẫu chung không có đối tác này', async ({ page }) => {
        await mockExtractSql(page, CURSOR_SQL);
        await mockPartners(page, [
            { partnerId: 'P1', partnerCode: 'DT-DAT', partnerName: 'Đạt', isCustom: true },
            { partnerId: 'P2', partnerCode: 'VDOT', partnerName: 'Sở GTVT', isCustom: false },
        ]);
        await mockVariables(page, (partnerId) => [pageSizeItem, keyItem(partnerId === 'P1' ? '2026-11-01 09:00:00.000_A1' : null)]);
        await page.route('**/api/sharedata/sharedatapacket/getpartnerextractsql*', (route) => {
            const partnerId = partnerIdOf(route);
            const isCustom = partnerId === 'P1';
            return fulfillJson(route, { partnerId, packetCode: PACKET_CODE, standardSql: CURSOR_SQL, customSql: isCustom ? CURSOR_SQL : null, isCustom, fields: [] });
        });

        await openSqlCenter(page);
        await expect(block(page)).toContainText('4. Mốc đang chạy của đối tác dùng mẫu chung', { timeout: 15_000 });
        await expect(blockRow(page, 'VDOT')).toHaveCount(1, { timeout: 10_000 });
        await expect(blockRow(page, 'DT-DAT')).toHaveCount(0);

        await page.locator('.sql-partner-tabs .el-tabs__item', { hasText: 'DT-DAT' }).click();
        await expect(block(page)).toContainText('4. Mốc đang chạy của Đạt', { timeout: 15_000 });
        await expect(block(page).locator('.el-table__body tr')).toHaveCount(1);
        await expect(blockRow(page, 'DT-DAT')).toContainText('01/11/2026 09:00:00.000');
    });

    const directionCases = [
        { name: 'chiều gửi', direction: 0, locked: true },
        { name: 'hai chiều', direction: 2, locked: true },
        { name: 'chiều nhận', direction: 1, locked: false },
    ];
    for (const { name, direction, locked } of directionCases) {
        test(`3. Đăng ký đang bật ${name}: ${locked ? 'khóa' : 'không khóa'} Đặt mốc và Đặt lại của đối tác đó`, async ({ page }) => {
            await page.route('**/api/sharedata/sharedatasubscription/list*', (route) => fulfillJson(route, [{ partnerId: 'p_a', direction }]));
            await mockExtractSql(page, CURSOR_SQL);
            await mockPartners(page, [
                { partnerId: 'p_a', partnerCode: 'CSGT', partnerName: 'Cục CSGT', isCustom: false },
                { partnerId: 'p_b', partnerCode: 'VDOT', partnerName: 'Sở GTVT', isCustom: false },
            ]);
            await mockVariables(page, (partnerId) => [keyItem(partnerId ? '2026-10-30 07:00:00.000_k100' : null)]);

            await openSqlCenter(page);
            const rowA = blockRow(page, 'CSGT');
            await expect(rowA).toContainText('k100', { timeout: 15_000 });

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
            const rowB = blockRow(page, 'VDOT');
            await expect(rowB.locator('.sd-cursor-set-btn')).toBeEnabled();
            await expect(rowB.locator('.sd-cursor-reset-btn')).toBeEnabled();
        });
    }

    /**
     * Author: DatHQ
     * Description: Hộp Đặt mốc: tiêu đề nói rõ là Mốc đang chạy, hiện mốc đang chạy và Mốc bắt đầu, chưa đổi thì không Xác nhận được;
     *              lùi mốc báo phần đã gửi bị gửi trùng, tiến mốc báo phần chưa gửi bị bỏ qua.
     * Created date: 07/11/2026
     */
    test('4. Hộp Đặt mốc: hiện mốc đang chạy, chưa đổi thì không Xác nhận được, lùi mốc báo gửi trùng, tiến mốc báo bỏ qua', async ({ page }) => {
        await mockExtractSql(page, CURSOR_SQL);
        await mockPartners(page, [{ partnerId: 'p_shared1', partnerCode: 'SHARED1', partnerName: 'Đối tác dùng mẫu chung', isCustom: false }]);
        await mockVariables(page, (partnerId) => [pageSizeItem, keyItem(partnerId ? '2026-10-28 08:00:00.000_k_1' : null)]);
        const setPayloads = await captureSetLastSend(page);

        await openSqlCenter(page);
        await expect(blockRow(page, 'SHARED1')).toContainText('k_1', { timeout: 15_000 });

        const dialog = page.locator('.sd-set-cursor-dialog');
        const confirmBtn = dialog.locator('.el-dialog__footer .el-button--primary');
        await blockRow(page, 'SHARED1').locator('.sd-cursor-set-btn').click();
        await expect(dialog).toBeVisible();
        await expect(dialog.locator('.el-dialog__header')).toContainText('Đặt Mốc đang chạy — Đối tác dùng mẫu chung');
        await expect(dialog.locator('.sd-cursor-current')).toContainText('28/10/2026 08:00:00.000 · k_1');
        await expect(dialog.locator('.sd-cursor-start')).toContainText('để trống, gửi toàn bộ dữ liệu');
        await expect(dialog.locator('.cvi-picker input')).toHaveValue('28/10/2026 08:00:00');
        await expect(confirmBtn).toBeDisabled();
        await expect(dialog.locator('.sd-cursor-effect')).toHaveCount(0);
        await pickTime(page, dialog, '27/10/2026 08:00:00');
        await expect(dialog.locator('.sd-cursor-effect')).toHaveText('Gửi lại dữ liệu từ 27/10/2026 08:00:00.000 tới 28/10/2026 08:00:00.000: đối tác nhận trùng phần này.');
        await pickTime(page, dialog, '29/10/2026 08:00:00');
        await expect(dialog.locator('.sd-cursor-effect')).toHaveText('Bỏ qua dữ liệu từ 28/10/2026 08:00:00.000 tới 29/10/2026 08:00:00.000: phần này không bao giờ được gửi.');
        await expect(confirmBtn).toBeEnabled();
        await confirmBtn.click();
        await expect.poll(() => setPayloads.length).toBe(1);
        expect(setPayloads[0]).toEqual({ packetCode: PACKET_CODE, partnerId: 'p_shared1', lastKey: '2026-10-29 08:00:00.000_' });
    });

    /**
     * Author: DatHQ
     * Description: Câu gộp nhiều nguồn: khối 4 hiện mốc từng nhóm, Đặt mốc chỉ gửi nhóm có đổi, kèm cursorKey của nhóm.
     * Created date: 07/11/2026
     */
    test('5. Câu gộp nhiều nguồn: khối 4 hiện mốc từng nhóm, Đặt mốc gửi riêng nhóm có đổi kèm cursorKey', async ({ page }) => {
        await mockExtractSql(page, CURSOR_SQL);
        await mockPartners(page, [{ partnerId: 'p_grp', partnerCode: 'GRP', partnerName: 'Đối tác nhiều nguồn', isCustom: false }]);
        await mockVariables(page, (partnerId) => [
            pageSizeItem,
            keyItem(partnerId ? '2026-10-27 09:30:00.000_a2' : null, null, 'lastKey_pkt'),
            keyItem(partnerId ? '2026-10-27 09:20:00.000_t9' : null, null, 'lastKey_toll'),
        ]);
        const setPayloads = await captureSetLastSend(page);

        await openSqlCenter(page);
        const row = blockRow(page, 'GRP');
        const pktLine = row.locator('.sd-cursor-saved .sd-cursor-line', { hasText: 'pkt:' });
        const tollLine = row.locator('.sd-cursor-saved .sd-cursor-line', { hasText: 'toll:' });
        await expect(pktLine).toContainText('27/10/2026 09:30:00.000', { timeout: 15_000 });
        await expect(pktLine).toContainText('a2');
        await expect(tollLine).toContainText('27/10/2026 09:20:00.000');
        await expect(tollLine).toContainText('t9');

        await row.locator('.sd-cursor-set-btn').click();
        const dialog = page.locator('.sd-set-cursor-dialog');
        await expect(dialog.locator('.sd-cursor-section')).toHaveCount(2);
        const tollSection = dialog.locator('.sd-cursor-section', { hasText: '__lastKey_toll' });
        await pickTime(page, tollSection, '27/10/2026 09:25:00');
        await dialog.locator('.el-dialog__footer .el-button--primary').click();
        await expect(dialog).toBeHidden();
        await expect.poll(() => setPayloads.length).toBe(1);
        expect(setPayloads[0]).toEqual({ packetCode: PACKET_CODE, partnerId: 'p_grp', cursorKey: 'toll', lastKey: '2026-10-27 09:25:00.000_' });
    });

    /**
     * Author: DatHQ
     * Description: Đặt mốc theo ngày giờ chỉ được khi câu đối tác đang chạy ghép cột mốc kiểu 121; câu dạng khác không có ô chọn ngày, không Xác nhận được.
     * Created date: 07/11/2026
     */
    test('6. Đặt mốc: câu ghép kiểu 121 chọn ngày giờ thì tự ghép chuỗi mốc; câu dạng khác không đặt được, báo cần sửa câu', async ({ page }) => {
        await mockExtractSql(page, CURSOR_SQL);
        await mockPartners(page, [
            { partnerId: 'p_time', partnerCode: 'TIME1', partnerName: 'Đối tác ghép thời gian', isCustom: false },
            { partnerId: 'p_plain', partnerCode: 'PLAIN1', partnerName: 'Đối tác chỉ khóa', isCustom: false },
        ]);
        await mockVariables(page, () => [pageSizeItem, keyItem(null)]);
        await page.route('**/api/sharedata/sharedatapacket/getpartnerextractsql*', (route) =>
            fulfillJson(route, { standardSql: partnerIdOf(route) === 'p_time' ? CURSOR_SQL : PLAIN_KEY_SQL })
        );
        const setPayloads = await captureSetLastSend(page);

        await openSqlCenter(page);
        const dialog = page.locator('.sd-set-cursor-dialog');
        await blockRow(page, 'TIME1').locator('.sd-cursor-set-btn').click({ timeout: 15_000 });
        await pickTime(page, dialog, '29/10/2026 08:30:00');
        await dialog.locator('.el-dialog__footer .el-button--primary').click();
        await expect.poll(() => setPayloads.length).toBe(1);
        expect(setPayloads[0]).toEqual({ packetCode: PACKET_CODE, partnerId: 'p_time', lastKey: '2026-10-29 08:30:00.000_' });

        await expect(dialog).toBeHidden();
        await blockRow(page, 'PLAIN1').locator('.sd-cursor-set-btn').click();
        await expect(dialog.locator('.sd-cursor-no-time')).toBeVisible();
        await expect(dialog.locator('.cvi-picker')).toHaveCount(0);
        await expect(dialog.locator('.el-dialog__footer .el-button--primary')).toBeDisabled();
    });

    /**
     * Author: DatHQ
     * Description: Câu không dùng mốc (gửi toàn bộ mỗi lượt) thì không có mốc đang chạy để quản lý: không hiện khối 4.
     * Created date: 07/11/2026
     */
    test('7. Câu không dùng mốc: không hiện khối 4', async ({ page }) => {
        await mockExtractSql(page, SNAPSHOT_SQL);
        await mockPartners(page, [{ partnerId: 'p_a', partnerCode: 'CSGT', partnerName: 'Cục CSGT', isCustom: false }]);
        await mockVariables(page, () => [pageSizeItem]);

        await openSqlCenter(page);
        await expect(page.locator('.sql-variable-panel .el-table__body')).toContainText('pageSize', { timeout: 15_000 });
        await expect(block(page)).toHaveCount(0);
    });

    /**
     * Author: DatHQ
     * Description: Khối 4 vừa khung chi tiết ở màn 1366px, bảng không cuộn ngang kể cả khi tên đối tác và mốc dài;
     *              chữ trong các cột (trừ nút thao tác) cùng một màu, không tô xanh đậm hay làm xám riêng phần nào.
     * Created date: 07/11/2026
     */
    test('8. Khối 4 vừa khung ở màn 1366px, không cuộn ngang; chữ trong bảng cùng một màu', async ({ page }, testInfo) => {
        await mockExtractSql(page, CURSOR_SQL);
        await mockPartners(page, [
            { partnerId: 'p_a', partnerCode: 'PC08', partnerName: 'Phòng Cảnh sát giao thông (PC08)', isCustom: false },
            { partnerId: 'p_b', partnerCode: 'TTDHT', partnerName: 'Trung tâm điều hành tuyến', isCustom: false },
        ]);
        await mockVariables(page, (partnerId) => [
            pageSizeItem,
            keyItem(partnerId === 'p_a' ? '2026-10-27 10:05:00.000_0dcdde08-134b-45cb-898b-59fd11a42504' : null, '2026-10-22 00:00:00.000_'),
        ]);

        await openSqlCenter(page, { width: 1366, height: 768 });
        await expect(blockRow(page, 'TTDHT')).toContainText('theo Mốc bắt đầu', { timeout: 15_000 });
        await expect(blockRow(page, 'PC08')).toContainText('0dcdde08-134b-45cb-898b-59fd11a42504');
        await expect(page.locator('.el-loading-mask:visible')).toHaveCount(0, { timeout: 15_000 });
        await block(page).scrollIntoViewIfNeeded();
        // Ảnh khối 4 ở thư mục kết quả test để so giao diện khi cần
        await page.screenshot({ path: testInfo.outputPath('khoi4-1366.png') });

        const overflowX = await block(page)
            .locator('.el-table__body-wrapper')
            .evaluate((el) => {
                const wrap = (el.querySelector('.el-scrollbar__wrap') as HTMLElement | null) ?? (el as HTMLElement);
                return wrap.scrollWidth - wrap.clientWidth;
            });
        expect(overflowX).toBeLessThanOrEqual(1);

        const colors = await block(page)
            .locator('.el-table__body tr td:not(:last-child) .cell')
            .evaluateAll((cells) => {
                const texts = cells.flatMap((cell) => [cell, ...Array.from(cell.querySelectorAll('span'))]).filter((el) => Array.from(el.childNodes).some((n) => n.nodeType === 3 && (n.textContent ?? '').trim()));
                return [...new Set(texts.map((el) => getComputedStyle(el).color))];
            });
        expect(colors).toHaveLength(1);
    });
});
