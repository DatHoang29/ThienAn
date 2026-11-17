import { test, expect, type Locator, type Page, type Route } from '@playwright/test';
import { mockAuth } from '../../setup';

/**
 * Test E2E cho phân hệ ShareData — khối 4 "Mốc đang chạy" ở trang Cấu hình gói tin (dataSource/component/cursorBlock.vue):
 * mốc đang chạy theo từng đối tác trong phạm vi tab đang mở (tab Mẫu chung: mọi đối tác dùng mẫu chung, mỗi đối tác một dòng; tab đối tác: đúng đối tác đó),
 * Đặt mốc, Đặt lại và khóa khi đăng ký gửi đang bật.
 */

/** Lỗi của khung layout dùng chung (layout/main/defaults.vue gọi layoutMainScrollbarRef khi chưa gắn) nằm ngoài ShareData: không tính vào lỗi trang. */
const isSharedLayoutError = (message: string) => message.includes('layoutMainScrollbarRef');

const json = (body: unknown) => ({ status: 200, contentType: 'application/json', body: JSON.stringify(body) });
const fulfillJson = (route: Route, result: unknown) => route.fulfill(json({ code: 200, result }));

const PACKET_CODE = '101_commonData';
const CURSOR_KEY = "CONVERT(varchar(8), COALESCE(UpdateTime, CreateTime), 112) + REPLACE(CONVERT(varchar(12), COALESCE(UpdateTime, CreateTime), 114), ':', '') + '_' + ID";
const CURSOR_SQL = `SELECT TOP (@pageSize) ID AS zoneId, ${CURSOR_KEY} AS __lastKey FROM TmsZone WHERE ${CURSOR_KEY} > @lastKey ORDER BY __lastKey ASC`;
const SNAPSHOT_SQL = 'SELECT TOP (@pageSize) ID AS zoneId FROM TmsZone';
const CURSOR_VALUE_ERROR = 'Mốc gồm 8–17 chữ số năm, tháng, ngày, giờ, phút, giây, mili giây viết liền, ngày giờ phải có thật; muốn ghép đuôi thì viết đủ 17 số rồi thêm _<đuôi>.';

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
/** Ô mốc của khối 4 hiện nguyên chuỗi mốc đang lưu, không tách ngày giờ hay mã dòng. */
const expectCursorCell = async (scope: Locator, value: string, timeout = 10_000) => {
    await expect(scope.locator('.sd-cursor-value')).toHaveText(value, { timeout });
    await expect(scope).not.toContainText('Đã gửi tới');
};
/** Gõ mốc vào ô chữ (hộp Đặt mốc hoặc ô Mốc bắt đầu). */
const typeCursor = async (scope: Locator, text: string) => {
    await scope.locator('.cvi-input input').fill(text);
};
const selectPartnerScope = async (page: Page, labelOrCode: string) => {
    const select = page.locator('.partner-tab-jump');
    await expect(select).toBeVisible({ timeout: 10_000 });
    await select.click();
    const option = page.locator('.partner-tab-jump-popper:visible .el-select-dropdown__item', { hasText: labelOrCode }).first();
    await expect(option).toBeVisible({ timeout: 5_000 });
    await option.click();
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
     * Description: Tab Mẫu chung: khối 4 hiện mốc của mọi đối tác dùng mẫu chung, mỗi đối tác một dòng (đối tác có cấu hình riêng nằm ở tab của họ), hiện mốc đang chạy,
     *              chưa có mốc thì nói lượt gửi tới lấy từ Mốc bắt đầu; Đặt mốc cho đối tác chưa có mốc thì không báo hệ quả,
     *              Đặt lại hỏi rõ sẽ gửi lại từ Mốc bắt đầu, bảng nạp lại đúng sau mỗi thao tác.
     * Created date: 07/11/2026
     * Modified date: 16/11/2026
     */
    test('1. Tab Mẫu chung: khối 4 hiện từng đối tác dùng mẫu chung kèm mốc đang chạy (chưa có thì nói lượt gửi tới lấy từ đâu); Đặt mốc, Đặt lại nạp lại đúng', async ({ page }) => {
        const saved: Record<string, string | null> = { p_a: '20261027100500000_Z02', p_b: null };
        const resetPayloads: any[] = [];
        await mockExtractSql(page, CURSOR_SQL);
        await mockPartners(page, [
            { partnerId: 'p_a', partnerCode: 'CSGT', partnerName: 'Cục CSGT', isCustom: false },
            { partnerId: 'p_b', partnerCode: 'VDOT', partnerName: 'Sở GTVT', isCustom: false },
            { partnerId: 'p_c', partnerCode: 'RIENG', partnerName: 'Đối tác cấu hình riêng', isCustom: true },
        ]);
        // Mẫu chung có Mốc bắt đầu 22/10/2026
        await mockVariables(page, (partnerId) => [pageSizeItem, keyItem(partnerId ? saved[partnerId] ?? null : null, '20261022')]);
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
        // Có mốc thì hiện nguyên chuỗi mốc; chưa có mốc thì nói lượt gửi tới lấy từ Mốc bắt đầu (nguyên chuỗi)
        await expectCursorCell(rowA, '20261027100500000_Z02');
        await expect(rowB.locator('.sd-cursor-line')).toHaveText('Chưa có, lượt gửi tới lấy từ Mốc bắt đầu 20261022');
        await expect(blockRow(page, 'RIENG')).toHaveCount(0);

        // Chưa có mốc thì không đặt lại được, rê chuột thấy lý do
        await expect(rowB.locator('.sd-cursor-reset-btn')).toBeDisabled();
        await rowB.locator('.sd-cursor-btn-wrapper').nth(1).hover();
        await expect(page.locator('.el-popper:visible', { hasText: 'Chưa có mốc gửi dữ liệu để đặt lại' })).toBeVisible({ timeout: 5_000 });

        // Đặt mốc cho VDOT (chưa có mốc đang chạy): ô trống, gõ mốc là chọn chỗ bắt đầu, không báo gửi trùng hay bỏ qua
        await rowB.locator('.sd-cursor-set-btn').click();
        const dialog = page.locator('.sd-set-cursor-dialog');
        await expect(dialog).toBeVisible();
        await expect(dialog.locator('.cvi-input input')).toHaveValue('');
        await typeCursor(dialog, '20261102080000000');
        await expect(dialog.locator('.sd-cursor-effect')).toHaveCount(0);
        await dialog.locator('.el-dialog__footer .el-button--primary').click();
        await expect(dialog).toBeHidden();
        await expect(page.locator('.el-message--success')).toContainText('Đã cập nhật mốc gửi dữ liệu cho đối tác.');
        await expectCursorCell(rowB, '20261102080000000');

        // Đặt lại CSGT: hỏi rõ gửi lại từ Mốc bắt đầu và bị gửi trùng, gửi đúng gói và đối tác, bảng về trạng thái chưa có mốc
        await rowA.locator('.sd-cursor-reset-btn').click();
        await expect(page.locator('.el-message-box')).toContainText('Bỏ Mốc đang chạy của Cục CSGT? Lượt gửi tới gửi lại từ Mốc bắt đầu 20261022, phần đã gửi sẽ bị gửi trùng.');
        await page.locator('.el-message-box__btns .el-button--primary').click();
        await expect.poll(() => resetPayloads.length).toBe(1);
        expect(resetPayloads[0]).toEqual({ packetCode: PACKET_CODE, partnerId: 'p_a' });
        await expect(page.locator('.el-message--success').last()).toContainText('Đã bỏ Mốc đang chạy.');
        await expect(rowA.locator('.sd-cursor-line')).toHaveText('Chưa có, lượt gửi tới lấy từ Mốc bắt đầu 20261022');
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
        await mockVariables(page, (partnerId) => [pageSizeItem, keyItem(partnerId === 'P1' ? '20261101090000000_A1' : null)]);
        await page.route('**/api/sharedata/sharedatapacket/getpartnerextractsql*', (route) => {
            const partnerId = partnerIdOf(route);
            const isCustom = partnerId === 'P1';
            return fulfillJson(route, { partnerId, packetCode: PACKET_CODE, standardSql: CURSOR_SQL, customSql: isCustom ? CURSOR_SQL : null, isCustom, fields: [] });
        });

        await openSqlCenter(page);
        await expect(block(page)).toContainText('4. Mốc đang chạy của đối tác dùng mẫu chung', { timeout: 15_000 });
        await expect(blockRow(page, 'VDOT')).toHaveCount(1, { timeout: 10_000 });
        await expect(blockRow(page, 'DT-DAT')).toHaveCount(0);

        await selectPartnerScope(page, 'DT-DAT');
        await expect(block(page)).toContainText('4. Mốc đang chạy của Đạt', { timeout: 15_000 });
        await expect(block(page).locator('.el-table__body tr')).toHaveCount(1);
        await expectCursorCell(blockRow(page, 'DT-DAT'), '20261101090000000_A1');
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
            await mockVariables(page, (partnerId) => [keyItem(partnerId ? '20261030070000000_k100' : null)]);

            await openSqlCenter(page);
            const rowA = blockRow(page, 'CSGT');
            await expectCursorCell(rowA, '20261030070000000_k100', 15_000);

            const scbCard = block(page).locator('.scb-card');
            // Chỉ CSGT có đăng ký: cả khối không mờ, chỉ dòng của CSGT khóa
            await expect(scbCard).not.toHaveClass(/is-scope-locked/);
            if (locked) {
                await expect(rowA).toHaveClass(/is-row-locked/);
                await expect(rowA.locator('.sd-cursor-set-btn')).toBeDisabled();
                await expect(rowA.locator('.sd-cursor-reset-btn')).toBeDisabled();
                await rowA.locator('.sd-cursor-btn-wrapper').first().hover();
                await expect(page.locator('.el-popper:visible', { hasText: 'Tắt đăng ký trước khi đặt mốc' })).toBeVisible({ timeout: 5_000 });
            } else {
                await expect(rowA).not.toHaveClass(/is-row-locked/);
                await expect(rowA.locator('.sd-cursor-set-btn')).toBeEnabled();
                await expect(rowA.locator('.sd-cursor-reset-btn')).toBeEnabled();
            }

            // Đối tác không có đăng ký luôn đặt được và dòng không bị khóa mờ
            const rowB = blockRow(page, 'VDOT');
            await expect(rowB).not.toHaveClass(/is-row-locked/);
            await expect(rowB.locator('.sd-cursor-set-btn')).toBeEnabled();
            await expect(rowB.locator('.sd-cursor-reset-btn')).toBeEnabled();
        });
    }

    /**
     * Author: DatHQ
     * Description: Hộp Đặt mốc: tiêu đề nói rõ là Mốc đang chạy, hiện mốc đang chạy và Mốc bắt đầu, chưa đổi thì không Xác nhận được;
     *              lùi mốc báo phần đã gửi bị gửi trùng, tiến mốc báo phần chưa gửi bị bỏ qua (so chuỗi, hiện nguyên chuỗi mốc).
     * Created date: 07/11/2026
     * Modified date: 16/11/2026
     */
    test('4. Hộp Đặt mốc: hiện mốc đang chạy, chưa đổi thì không Xác nhận được, lùi mốc báo gửi trùng, tiến mốc báo bỏ qua', async ({ page }) => {
        await mockExtractSql(page, CURSOR_SQL);
        await mockPartners(page, [{ partnerId: 'p_shared1', partnerCode: 'SHARED1', partnerName: 'Đối tác dùng mẫu chung', isCustom: false }]);
        await mockVariables(page, (partnerId) => [pageSizeItem, keyItem(partnerId ? '20261028080000000_k_1' : null)]);
        const setPayloads = await captureSetLastSend(page);

        await openSqlCenter(page);
        await expectCursorCell(blockRow(page, 'SHARED1'), '20261028080000000_k_1', 15_000);

        const dialog = page.locator('.sd-set-cursor-dialog');
        const confirmBtn = dialog.locator('.el-dialog__footer .el-button--primary');
        await blockRow(page, 'SHARED1').locator('.sd-cursor-set-btn').click();
        await expect(dialog).toBeVisible();
        await expect(dialog.locator('.el-dialog__header')).toContainText('Đặt Mốc đang chạy — Đối tác dùng mẫu chung');
        await expect(dialog.locator('.el-dialog__footer .el-button')).toHaveCount(2);
        await expect(dialog.locator('.el-dialog__footer')).not.toContainText('Đặt lại');
        await expect(dialog.locator('.sd-cursor-current')).toHaveText('Mốc đang chạy: 20261028080000000_k_1');
        await expect(dialog.locator('.el-form-item')).toHaveCount(2);
        await expect(dialog.locator('.cvi-input input')).toHaveValue('20261028080000000_k_1');
        await expect(confirmBtn).toBeDisabled();
        await expect(dialog.locator('.sd-cursor-effect')).toHaveCount(0);
        await typeCursor(dialog, '20261027080000000');
        await expect(dialog.locator('.sd-cursor-effect')).toHaveText('Gửi lại dữ liệu từ 20261027080000000 tới 20261028080000000_k_1: đối tác nhận trùng phần này.');
        await typeCursor(dialog, '20261029');
        await expect(dialog.locator('.sd-cursor-effect')).toHaveText('Bỏ qua dữ liệu từ 20261028080000000_k_1 tới 20261029: phần này không bao giờ được gửi.');
        await expect(confirmBtn).toBeEnabled();
        await confirmBtn.click();
        await expect.poll(() => setPayloads.length).toBe(1);
        expect(setPayloads[0]).toEqual({ packetCode: PACKET_CODE, partnerId: 'p_shared1', lastKey: '20261029' });
    });

    /**
     * Author: DatHQ
     * Description: Câu gộp nhiều nguồn: khối 4 hiện mốc từng nhóm, Đặt mốc chỉ gửi nhóm có đổi, kèm cursorKey của nhóm.
     * Created date: 07/11/2026
     * Modified date: 16/11/2026
     */
    test('5. Câu gộp nhiều nguồn: khối 4 hiện mốc từng nhóm, Đặt mốc gửi riêng nhóm có đổi kèm cursorKey', async ({ page }) => {
        await mockExtractSql(page, CURSOR_SQL);
        await mockPartners(page, [{ partnerId: 'p_grp', partnerCode: 'GRP', partnerName: 'Đối tác nhiều nguồn', isCustom: false }]);
        await mockVariables(page, (partnerId) => [
            pageSizeItem,
            keyItem(partnerId ? '20261027093000000_a2' : null, null, 'lastKey_pkt'),
            keyItem(partnerId ? '20261027092000000_t9' : null, null, 'lastKey_toll'),
        ]);
        const setPayloads = await captureSetLastSend(page);

        await openSqlCenter(page);
        const row = blockRow(page, 'GRP');
        const pktLine = row.locator('.sd-cursor-saved .sd-cursor-line', { hasText: 'pkt:' });
        const tollLine = row.locator('.sd-cursor-saved .sd-cursor-line', { hasText: 'toll:' });
        await expectCursorCell(pktLine, '20261027093000000_a2', 15_000);
        await expectCursorCell(tollLine, '20261027092000000_t9');

        await row.locator('.sd-cursor-set-btn').click();
        const dialog = page.locator('.sd-set-cursor-dialog');
        await expect(dialog.locator('.sd-cursor-section')).toHaveCount(2);
        const tollSection = dialog.locator('.sd-cursor-section', { hasText: '__lastKey_toll' });
        await typeCursor(tollSection, '20261027092500000');
        await dialog.locator('.el-dialog__footer .el-button--primary').click();
        await expect(dialog).toBeHidden();
        await expect.poll(() => setPayloads.length).toBe(1);
        expect(setPayloads[0]).toEqual({ packetCode: PACKET_CODE, partnerId: 'p_grp', cursorKey: 'toll', lastKey: '20261027092500000' });
    });

    /**
     * Author: DatHQ
     * Description: Hộp Đặt mốc là ô chữ điền sẵn nguyên chuỗi mốc đang chạy, sửa tự do: nhận tiền tố ngày 20260101 và gửi đúng chuỗi đã gõ (bỏ khoảng trắng hai đầu);
     *              dạng cũ có dấu gạch và hai chấm, ngày giờ không có thật, đuôi khi chưa đủ 17 số thì báo dưới ô và khóa Xác nhận; ô trống cũng khóa Xác nhận.
     *              Nút chọn nhanh điền sẵn tiền tố ngày yyyyMMdd (đầu tháng là yyyyMM01).
     * Created date: 07/11/2026
     * Modified date: 16/11/2026
     */
    test('6. Hộp Đặt mốc: điền sẵn mốc đang chạy, nhận 20260101, chặn dạng cũ và ngày giờ không có thật, ô trống không Xác nhận được', async ({ page }) => {
        await mockExtractSql(page, CURSOR_SQL);
        await mockPartners(page, [{ partnerId: 'p_a', partnerCode: 'CSGT', partnerName: 'Cục CSGT', isCustom: false }]);
        await mockVariables(page, (partnerId) => [pageSizeItem, keyItem(partnerId ? '20261028080000000_k_1' : null)]);
        const setPayloads = await captureSetLastSend(page);

        await openSqlCenter(page);
        await blockRow(page, 'CSGT').locator('.sd-cursor-set-btn').click({ timeout: 15_000 });
        const dialog = page.locator('.sd-set-cursor-dialog');
        const input = dialog.locator('.cvi-input input');
        const error = dialog.locator('.cvi-error');
        const confirmBtn = dialog.locator('.el-dialog__footer .el-button--primary');
        await expect(input).toHaveValue('20261028080000000_k_1');
        await expect(input).toHaveAttribute('placeholder', 'Ví dụ 20260101 hoặc 20261116102001123_<đuôi>');
        await expect(error).toHaveCount(0);
        await expect(confirmBtn).toBeDisabled();

        // Dạng cũ, ngày 30/02, giờ 24, đuôi khi chưa đủ 17 số, thiếu chữ số: báo dưới ô, không Xác nhận được, không báo hệ quả
        for (const invalid of ['2026-01-01 00:00:00.000_x', '20260230', '2026010124', '2026010112_x', '2026011']) {
            await typeCursor(dialog, invalid);
            await expect(error).toHaveText(CURSOR_VALUE_ERROR);
            await expect(confirmBtn).toBeDisabled();
            await expect(dialog.locator('.sd-cursor-effect')).toHaveCount(0);
        }

        // Ô trống: không báo lỗi nhưng không có gì để lưu
        await typeCursor(dialog, '');
        await expect(error).toHaveCount(0);
        await expect(confirmBtn).toBeDisabled();

        // Nút chọn nhanh đầu tháng điền tiền tố yyyyMM01
        const now = new Date();
        const monthStart = `${now.getFullYear()}${String(now.getMonth() + 1).padStart(2, '0')}01`;
        await dialog.locator('.cvi-shortcut-btn').click();
        await page.locator('.el-dropdown-menu__item:visible', { hasText: 'Đầu tháng này' }).click();
        await expect(input).toHaveValue(monthStart);
        await expect(confirmBtn).toBeEnabled();

        // Tiền tố ngày 20260101: hợp lệ, báo gửi trùng vì lùi so với mốc đang chạy, gửi đúng chuỗi đã gõ
        await typeCursor(dialog, ' 20260101 ');
        await expect(error).toHaveCount(0);
        await expect(dialog.locator('.sd-cursor-effect')).toHaveText('Gửi lại dữ liệu từ 20260101 tới 20261028080000000_k_1: đối tác nhận trùng phần này.');
        await expect(confirmBtn).toBeEnabled();
        await confirmBtn.click();
        await expect(dialog).toBeHidden();
        await expect.poll(() => setPayloads.length).toBe(1);
        expect(setPayloads[0]).toEqual({ packetCode: PACKET_CODE, partnerId: 'p_a', lastKey: '20260101' });
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
     * Modified date: 16/11/2026
     */
    test('8. Khối 4 vừa khung ở màn 1366px, không cuộn ngang; chữ trong bảng cùng một màu', async ({ page }, testInfo) => {
        await mockExtractSql(page, CURSOR_SQL);
        await mockPartners(page, [
            { partnerId: 'p_a', partnerCode: 'PC08', partnerName: 'Phòng Cảnh sát giao thông (PC08)', isCustom: false },
            { partnerId: 'p_b', partnerCode: 'TTDHT', partnerName: 'Trung tâm điều hành tuyến', isCustom: false },
        ]);
        await mockVariables(page, (partnerId) => [
            pageSizeItem,
            keyItem(partnerId === 'p_a' ? '20261027100500000_0dcdde08-134b-45cb-898b-59fd11a42504' : null, '20261022'),
        ]);

        await openSqlCenter(page, { width: 1366, height: 768 });
        await expectCursorCell(blockRow(page, 'PC08'), '20261027100500000_0dcdde08-134b-45cb-898b-59fd11a42504', 15_000);
        await expect(blockRow(page, 'TTDHT')).toContainText('Chưa có, lượt gửi tới lấy từ Mốc bắt đầu 20261022');
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

    /**
     * Author: DatHQ
     * Description: Hộp Đặt mốc với mốc đang chạy dài (ngày giờ kèm ID dạng GUID): mốc xuống dòng trong khung, đọc đủ chuỗi, hộp không có thanh cuộn ngang.
     * Created date: 10/11/2026
     * Modified date: 16/11/2026
     */
    test('8b. Hộp Đặt mốc: mốc dài (ngày giờ + GUID) xuống dòng trong khung, không cuộn ngang', async ({ page }) => {
        const longKey = '20261109140136890_c29ede08-497b-4959-8ee9-88faf326c7dc';
        await mockExtractSql(page, CURSOR_SQL);
        await mockPartners(page, [{ partnerId: 'p_a', partnerCode: 'PC08', partnerName: 'Phòng Cảnh sát giao thông (PC08)', isCustom: false }]);
        await mockVariables(page, (partnerId) => [pageSizeItem, keyItem(partnerId ? longKey : null)]);

        await openSqlCenter(page, { width: 1366, height: 768 });
        await blockRow(page, 'PC08').locator('.sd-cursor-set-btn').click({ timeout: 15_000 });
        const dialog = page.locator('.sd-set-cursor-dialog');
        await expect(dialog.locator('.sd-cursor-current')).toContainText(longKey);
        await expect(dialog.locator('.cvi-input input')).toHaveValue(longKey);

        const fit = await dialog.locator('.el-dialog__body').evaluate((body) => {
            const tag = body.querySelector('.scd-tag-mono') as HTMLElement;
            const bodyBox = body.getBoundingClientRect();
            const tagBox = tag.getBoundingClientRect();
            return { overflowX: body.scrollWidth - body.clientWidth, tagOutside: tagBox.right - bodyBox.right };
        });
        expect(fit.overflowX).toBeLessThanOrEqual(1);
        expect(fit.tagOutside).toBeLessThanOrEqual(0);
    });

    /**
     * Author: DatHQ
     * Description: Ô mốc khối 4 hiện nguyên chuỗi mốc đang lưu: mốc Worker ghi (17 số + GUID), mốc đặt tay theo ngày, chuỗi dạng khác đều hiện đúng như trong CSDL;
     *              không còn chữ "Đã gửi tới", ngày DD/MM/YYYY, chữ "(đặt tay)" hay tooltip mã dòng; chuỗi dài xuống dòng trong ô, bảng không cuộn ngang.
     * Created date: 14/11/2026
     * Modified date: 16/11/2026
     */
    test('8c. Ô mốc khối 4 hiện nguyên chuỗi mốc đang lưu, không tách ngày giờ, không tooltip mã dòng', async ({ page }) => {
        const saved: Record<string, string> = {
            p_auto: '20261112154341477_bf9ede08-a752-414b-84e5-1bfb38f2c68b',
            p_manual: '20261112',
            p_old: '2026-11-12 11:59:53.970_',
            p_odd: 'Z99-khong-dung-dang',
        };
        await mockExtractSql(page, CURSOR_SQL);
        await mockPartners(page, [
            { partnerId: 'p_auto', partnerCode: 'KT033', partnerName: 'Kiểm tải 033', isCustom: false },
            { partnerId: 'p_manual', partnerCode: 'KT002', partnerName: 'Kiểm tải 002', isCustom: false },
            { partnerId: 'p_old', partnerCode: 'KT005', partnerName: 'Kiểm tải 005', isCustom: false },
            { partnerId: 'p_odd', partnerCode: 'KT044', partnerName: 'Kiểm tải 044', isCustom: false },
        ]);
        await mockVariables(page, (partnerId) => [pageSizeItem, keyItem(partnerId ? saved[partnerId] ?? null : null)]);

        await openSqlCenter(page, { width: 1366, height: 768 });
        await expectCursorCell(blockRow(page, 'KT033'), saved.p_auto, 15_000);
        await expectCursorCell(blockRow(page, 'KT002'), saved.p_manual);
        await expectCursorCell(blockRow(page, 'KT005'), saved.p_old);
        await expectCursorCell(blockRow(page, 'KT044'), saved.p_odd);
        await expect(block(page)).not.toContainText('(đặt tay)');
        await expect(block(page)).not.toContainText('12/11/2026');

        // Rê chuột vào ô mốc không hiện tooltip mã dòng
        await blockRow(page, 'KT033').locator('.sd-cursor-value').hover();
        await page.waitForTimeout(500);
        await expect(page.locator('.cursor-block-tooltip:visible')).toHaveCount(0);

        const overflowX = await block(page)
            .locator('.el-table__body-wrapper')
            .evaluate((el) => {
                const wrap = (el.querySelector('.el-scrollbar__wrap') as HTMLElement | null) ?? (el as HTMLElement);
                return wrap.scrollWidth - wrap.clientWidth;
            });
        expect(overflowX).toBeLessThanOrEqual(1);
    });

    /**
     * Author: DatHQ
     * Description: Tab đối tác: Đặt lại ở khối 4 thì ô Mốc bắt đầu ở khối 3 mở ngay (đối tác không còn mốc), Đặt mốc thì khóa lại ngay,
     *              không cần tải lại trang; phần đang sửa chưa lưu ở khối 3 vẫn giữ.
     * Created date: 08/11/2026
     * Modified date: 16/11/2026
     */
    test('9. Tab đối tác: Đặt lại, Đặt mốc ở khối 4 thì ô Mốc bắt đầu ở khối 3 mở hoặc khóa ngay, giữ phần đang sửa', async ({ page }) => {
        const saved: Record<string, string | null> = { p_a: '20261027100500000_Z02' };
        await mockExtractSql(page, CURSOR_SQL);
        await mockPartners(page, [{ partnerId: 'p_a', partnerCode: 'CSGT', partnerName: 'Cục CSGT', isCustom: true }]);
        await mockVariables(page, (partnerId) => [pageSizeItem, keyItem(partnerId ? saved[partnerId] ?? null : null)]);
        await page.route('**/api/sharedata/sharedatapacket/getpartnerextractsql*', (route) =>
            fulfillJson(route, { partnerId: partnerIdOf(route), packetCode: PACKET_CODE, standardSql: CURSOR_SQL, customSql: CURSOR_SQL, isCustom: true, fields: [] })
        );
        await page.route('**/api/sharedata/sharedatapacket/resetlastsend*', (route) => {
            saved[route.request().postDataJSON().partnerId] = null;
            return fulfillJson(route, true);
        });
        await page.route('**/api/sharedata/sharedatapacket/setlastsend*', (route) => {
            const body = route.request().postDataJSON();
            saved[body.partnerId] = body.lastKey;
            return fulfillJson(route, null);
        });

        await openSqlCenter(page);
        await selectPartnerScope(page, 'CSGT');
        const variables = page.locator('.sql-variable-panel .svp-table').first();
        const startInput = variables.locator('tbody tr', { hasText: '@lastKey' }).locator('.cvi-input input');
        const pageSizeInput = variables.locator('tbody tr', { hasText: '@pageSize' }).locator('.el-input-number input');
        const row = blockRow(page, 'CSGT');
        await expectCursorCell(row, '20261027100500000_Z02', 15_000);
        await expect(startInput).toBeDisabled();
        await expect(startInput).toHaveAttribute('placeholder', 'Đã có mốc, gửi tiếp');

        // Sửa khối 3 mà chưa lưu
        await pageSizeInput.fill('50');
        await pageSizeInput.press('Tab');
        await expect(pageSizeInput).toHaveValue('50');

        // Đặt lại: đối tác không còn mốc nên ô Mốc bắt đầu mở ngay
        await row.locator('.sd-cursor-reset-btn').click();
        await page.locator('.el-message-box__btns .el-button--primary').click();
        await expect(row.locator('.sd-cursor-line')).toHaveText('Chưa có, lượt gửi tới lấy toàn bộ dữ liệu');
        await expect(startInput).toBeEnabled();
        await expect(startInput).toHaveAttribute('placeholder', 'Ví dụ 20260101 hoặc 20261116102001123_<đuôi>');
        await expect(pageSizeInput).toHaveValue('50');

        // Đặt mốc: đối tác có mốc lại nên ô Mốc bắt đầu khóa ngay
        await row.locator('.sd-cursor-set-btn').click();
        const dialog = page.locator('.sd-set-cursor-dialog');
        await expect(dialog).toBeVisible();
        await typeCursor(dialog, '20261102080000000');
        await dialog.locator('.el-dialog__footer .el-button--primary').click();
        await expect(dialog).toBeHidden();
        await expectCursorCell(row, '20261102080000000');
        await expect(startInput).toBeDisabled();
        await expect(startInput).toHaveAttribute('placeholder', 'Đã có mốc, gửi tiếp');
        await expect(pageSizeInput).toHaveValue('50');
    });

    /**
     * Author: DatHQ
     * Description: Khi chọn gói tin hoặc chuyển tab cấu hình SQL, mặt nạ loading chung chờ cả khối 4 (Mốc đang chạy)
     *              tải xong biến của đối tác rồi mới tắt để toàn bộ thông tin gói và mốc hiển thị đồng loạt.
     * Created date: 09/11/2026
     * Modified date: 16/11/2026
     */
    test('10. Chọn gói tin và chuyển tab: mặt nạ loading chung chờ khối 4 tải xong mốc của đối tác mới hiển thị đồng loạt', async ({ page }) => {
        let releasePartnerCursor!: () => void;
        let partnerCursorPending = new Promise<void>((resolve) => {
            releasePartnerCursor = resolve;
        });
        let partnerCursorCalls = 0;

        await mockExtractSql(page, CURSOR_SQL);
        await mockPartners(page, [
            { partnerId: 'p_shared', partnerCode: 'PC08', partnerName: 'Phòng Cảnh sát giao thông (PC08)', isCustom: false },
            { partnerId: 'p_custom', partnerCode: 'VDOT', partnerName: 'Sở GTVT', isCustom: true },
        ]);
        await page.route('**/api/sharedata/sharedatapacket/getsqlvariables*', async (route) => {
            const partnerId = partnerIdOf(route);
            if (partnerId === 'p_shared') {
                partnerCursorCalls++;
                await partnerCursorPending;
                return fulfillJson(route, { items: [pageSizeItem, keyItem('20261102020200000_', '20261022')] });
            }
            return fulfillJson(route, { items: [pageSizeItem, keyItem(partnerId === 'p_custom' ? '20261103030300000_V1' : null, '20261022')] });
        });
        await page.route('**/api/sharedata/sharedatapacket/getpartnerextractsql*', (route) => {
            const partnerId = partnerIdOf(route);
            const isCustom = partnerId === 'p_custom';
            return fulfillJson(route, { partnerId, packetCode: PACKET_CODE, standardSql: CURSOR_SQL, customSql: isCustom ? CURSOR_SQL : null, isCustom, fields: [] });
        });

        await page.setViewportSize({ width: 1600, height: 1000 });
        await page.goto('/#/sharedata/dataSource');
        await page.locator('.packet-col .vxe-body--row').first().click({ timeout: 60_000 });

        // Khối 1–3 đã tải xong nhưng khối 4 vẫn đang chờ API mốc của đối tác p_shared: mặt nạ loading chung phải giữ nguyên
        await expect.poll(() => partnerCursorCalls, { timeout: 15_000 }).toBe(1);
        const packetMask = page.locator('.packet-details-wrapper > .el-loading-mask');
        await expect(packetMask).toBeVisible();

        // Khi khối 4 nhận được mốc của đối tác thì mặt nạ loading chung tắt và dòng mốc hiển thị ngay
        releasePartnerCursor();
        await expect(packetMask).toBeHidden({ timeout: 10_000 });
        await expectCursorCell(blockRow(page, 'PC08'), '20261102020200000_');

        // Chuyển sang đối tác VDOT rồi quay lại Mẫu chung khi API mốc của khối 4 phản hồi chậm: mặt nạ chuyển phạm vi giữ cho tới khi khối 4 nạp xong
        await selectPartnerScope(page, 'VDOT');
        await expectCursorCell(blockRow(page, 'VDOT'), '20261103030300000_V1');

        partnerCursorPending = new Promise<void>((resolve) => {
            releasePartnerCursor = resolve;
        });
        await selectPartnerScope(page, 'Mẫu chung');
        await expect.poll(() => partnerCursorCalls, { timeout: 15_000 }).toBe(2);
        const scopeMask = page.locator('.send-regions-wrapper > .solid-loading-mask');
        await expect(scopeMask).toBeVisible();

        releasePartnerCursor();
        await expect(scopeMask).toBeHidden({ timeout: 10_000 });
        await expectCursorCell(blockRow(page, 'PC08'), '20261102020200000_');
    });

    /**
     * Author: DatHQ
     * Description: Tab Mẫu chung nhiều đối tác: khối 4 hiện hết đối tác dùng mẫu chung, mỗi đối tác một dòng, không có ô chọn đối tác, tải mốc của từng đối tác;
     *              đối tác có đăng ký gửi đang bật thì chỉ dòng của họ khóa, mọi đối tác trong bảng đều khóa thì cả khối mờ.
     *              Tab đối tác có cấu hình riêng chỉ một dòng của đối tác đó.
     * Created date: 13/11/2026
     * Modified date: 16/11/2026
     */
    test('11. Tab Mẫu chung: hiện hết đối tác dùng mẫu chung, không có ô chọn; khóa theo từng dòng, mọi đối tác khóa thì cả khối mờ', async ({ page }) => {
        const cursorCalls: string[] = [];
        await page.route('**/api/sharedata/sharedatasubscription/list*', (route) =>
            fulfillJson(route, [
                { partnerId: 'p_b', direction: 0 },
                { partnerId: 'p_d', direction: 0 },
            ])
        );
        await mockExtractSql(page, CURSOR_SQL);
        await mockPartners(page, [
            { partnerId: 'p_a', partnerCode: 'CSGT', partnerName: 'Cục CSGT', isCustom: false },
            { partnerId: 'p_b', partnerCode: 'VDOT', partnerName: 'Sở GTVT', isCustom: false },
            { partnerId: 'p_c', partnerCode: 'PC08', partnerName: 'Phòng Cảnh sát giao thông', isCustom: false },
            { partnerId: 'p_d', partnerCode: 'RIENG', partnerName: 'Đối tác cấu hình riêng', isCustom: true },
        ]);
        await page.route('**/api/sharedata/sharedatapacket/getsqlvariables*', (route) => {
            const partnerId = partnerIdOf(route);
            if (partnerId) cursorCalls.push(partnerId);
            return fulfillJson(route, { items: [pageSizeItem, keyItem(partnerId ? `2026110${cursorCalls.length}080000000_${partnerId}` : null)] });
        });
        await page.route('**/api/sharedata/sharedatapacket/getpartnerextractsql*', (route) => {
            const partnerId = partnerIdOf(route);
            const isCustom = partnerId === 'p_d';
            return fulfillJson(route, { partnerId, packetCode: PACKET_CODE, standardSql: CURSOR_SQL, customSql: isCustom ? CURSOR_SQL : null, isCustom, fields: [] });
        });

        await openSqlCenter(page);
        await expect(block(page).locator('.el-table__body tr')).toHaveCount(3, { timeout: 15_000 });
        await expect(block(page).locator('.scb-header .el-select')).toHaveCount(0);
        await expect(block(page).locator('.scb-badge-count')).toHaveText('3');
        await expect(blockRow(page, 'RIENG')).toHaveCount(0);
        // Mỗi dòng hiện mốc của đúng đối tác đó (đuôi mốc là mã đối tác)
        await expect(blockRow(page, 'CSGT').locator('.sd-cursor-value')).toHaveText(/^2026110\d080000000_p_a$/);
        await expect(blockRow(page, 'VDOT').locator('.sd-cursor-value')).toHaveText(/^2026110\d080000000_p_b$/);
        await expect(blockRow(page, 'PC08').locator('.sd-cursor-value')).toHaveText(/^2026110\d080000000_p_c$/);
        const cursorPartnersLoaded = () => [...new Set(cursorCalls.filter((id) => id !== 'p_d'))].sort();
        await expect.poll(cursorPartnersLoaded).toEqual(['p_a', 'p_b', 'p_c']);

        // Chỉ VDOT có đăng ký gửi đang bật: dòng VDOT khóa, dòng khác đặt được, cả khối không mờ
        const scbCard = block(page).locator('.scb-card');
        await expect(blockRow(page, 'VDOT')).toHaveClass(/is-row-locked/);
        await expect(blockRow(page, 'VDOT').locator('.sd-cursor-set-btn')).toBeDisabled();
        await expect(blockRow(page, 'CSGT')).not.toHaveClass(/is-row-locked/);
        await expect(blockRow(page, 'CSGT').locator('.sd-cursor-set-btn')).toBeEnabled();
        await expect(scbCard).not.toHaveClass(/is-scope-locked/);

        // Tab đối tác có cấu hình riêng đang bật đăng ký: chỉ dòng của đối tác đó, mọi dòng đều khóa nên cả khối mờ
        await selectPartnerScope(page, 'RIENG');
        await expect(block(page)).toContainText('4. Mốc đang chạy của Đối tác cấu hình riêng', { timeout: 15_000 });
        await expect(block(page).locator('.el-table__body tr')).toHaveCount(1);
        await expect(blockRow(page, 'RIENG')).toHaveCount(1);
        await expect(scbCard).toHaveClass(/is-scope-locked/);
    });
});
