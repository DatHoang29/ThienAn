import { test, expect } from '@playwright/test';
import type { Page } from '@playwright/test';
import fs from 'node:fs';
import { mockAuth, resolveWebvuePath } from '../../setup';

/**
 * Test E2E cho phân hệ ShareData — Khung soạn SQL của màn Nguồn dữ liệu (dataSource/component/sqlEditor.vue):
 * - Gợi ý khi gõ SQL: từ khóa, mẫu câu T-SQL, bảng của gói tin (Monaco 0.55.1, không dùng thư viện SQL ngoài).
 * - Bảng "Trường trích xuất" chỉ liệt kê cột có trong câu SQL, không thêm dòng cho trường đã khai mà SQL không có.
 * - Nút Lưu mặc định khóa, chỉ mở khi nội dung SQL đã khác bản đã tải.
 * - Quét tĩnh file ngôn ngữ: chuỗi chứa "@" phải viết {'@'}, nếu không vue-i18n báo "Invalid linked format" và làm đổ cả bộ gợi ý.
 */

const json = (body: unknown) => ({ status: 200, contentType: 'application/json', body: JSON.stringify(body) });

const SQL_ONE_COLUMN = 'SELECT TOP 100 td.Speed AS speed FROM TmsTrafficData td';

/** Danh sách trường đang lưu của gói (ShareDataSql.FieldsJson). */
const SAVED_FIELDS = [
    { key: 'speed', type: 'number', dbType: 'int', label: 'Tốc độ', required: false },
    { key: 'message', type: 'string', dbType: 'nvarchar(256)', label: 'Thông tin trạng thái', required: true },
];

/** Mở màn Nguồn dữ liệu (một trang, không còn tab), chọn gói tin đầu tiên và chờ khung soạn thảo SQL sẵn sàng. */
async function openSqlCenter(page: Page) {
    await page.setViewportSize({ width: 1600, height: 900 });
    await page.goto('/#/sharedata/dataSource');
    await page.locator('.packet-col .vxe-body--row').first().click({ timeout: 60_000 });
    await page.locator('.sql-monaco-editor .monaco-editor').first().waitFor({ timeout: 60_000 });
    await page.waitForTimeout(1500);
}

/** Xóa sạch nội dung khung soạn thảo (một dòng) bằng phím, rồi gõ lại văn bản. */
async function retypeSql(page: Page, text: string, delay = 150) {
    await page.locator('.sql-monaco-editor .view-lines').first().click();
    await page.keyboard.press('End');
    for (let i = 0; i < 160; i++) await page.keyboard.press('Backspace');
    if (text) await page.keyboard.type(text, { delay });
}


const suggestionLabels = (page: Page) =>
    page.evaluate(() => [...document.querySelectorAll('.suggest-widget .monaco-list-row')].map((r) => r.getAttribute('aria-label') || r.textContent || ''));

test.describe('ShareData — Khung soạn SQL (sqlEditor) @sharedata', () => {
    test.beforeEach(async ({ context, page }) => {
        await mockAuth(context, page, { userInfo: { defaultMenu: '/sharedata/dataSource', account: 'admin', accountType: '111' } as any });

        await page.route('**/api/system/sysmenu/loginmenutree*', (r) =>
            r.fulfill(
                json({
                    code: 200,
                    result: [
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
                    ],
                })
            )
        );
        await page.route('**/api/sharedata/sharedatapacket/page*', (r) => r.fulfill(json({ code: 200, result: { records: [{ id: 'PKT_01', code: '101_commonData', name: 'Gói tin dùng chung 101', status: 1 }], total: 1 } })));
        await page.route('**/api/sharedata/sharedatapacket/isinuse*', (r) => r.fulfill(json({ code: 200, result: false })));
        await page.route('**/api/sharedata/sharedatamapping/page*', (r) => r.fulfill(json({ code: 200, result: { records: [], total: 0 } })));
        // Danh sách trường lưu cùng câu SQL (FieldsJson): speed có trong SQL, message là trường đã khai từ trước
        await page.route('**/api/sharedata/sharedatapacket/getextractsql*', (r) => r.fulfill(json({ code: 200, result: { extractSql: SQL_ONE_COLUMN, status: 1, fields: SAVED_FIELDS } })));
        // Bảng tham số và câu ghi chiều nhận: mặc định rỗng (ca nào cần thì khai lại route, route khai sau được dùng trước)
        await page.route('**/api/sharedata/sharedatapacket/getsqlvariables*', (r) => r.fulfill(json({ code: 200, result: { items: [] } })));
        await page.route('**/api/sharedata/sharedatapacket/getwritesqls*', (r) => r.fulfill(json({ code: 200, result: { packetCode: '101_commonData', items: [] } })));
        await page.route('**/api/sharedata/sharedatapacket/parseextractcolumns*', (r) => r.fulfill(json({ code: 200, result: { isValid: true, columns: ['speed'] } })));
        await page.route('**/api/sharedata/sharedatapacket/getmetadatatables*', (r) => r.fulfill(json({ code: 200, result: [{ id: 'tbl-traffic', tableName: 'TmsTrafficData' }, { id: 'tbl-zone', tableName: 'TmsZone' }] })));
        await page.route('**/api/sharedata/sharedatapacket/getmetadatafields*', (r) =>
            r.fulfill(json({ code: 200, result: [{ columnName: 'Id', dataType: 'bigint' }, { columnName: 'Speed', dataType: 'int' }, { columnName: 'UpdateTime', dataType: 'datetime' }] }))
        );
    });

    const suggestCases = [
        { name: '1. Gõ SEL gợi ý từ khóa SELECT và các mẫu câu select-*', text: 'SEL', expected: ['SELECT, Keyword', 'select-incremental, Snippet'] },
        { name: '2. Gõ FROM Tms gợi ý tên bảng nguồn của gói tin', text: 'SELECT * FROM Tms', expected: ['TmsZone, Class'] },
    ];

    for (const c of suggestCases) {
        test(c.name, async ({ page }) => {
            await openSqlCenter(page);
            await retypeSql(page, c.text);
            await expect.poll(() => suggestionLabels(page), { timeout: 10_000 }).toEqual(expect.arrayContaining(c.expected.map((label) => expect.stringContaining(label))));
        });
    }

    /**
     * Description: Gõ @ chỉ gợi ý biến đã khai ở bảng Tham số câu truy vấn (không còn biến hệ thống cố định):
     *              chưa khai biến nào thì không gợi ý @ gì, khai @since thì gợi ý đúng @since.
     * Created date: 25/10/2026
     */
    const variableSuggestCases = [
        { name: '28. Chưa khai biến: gõ @ không gợi ý biến nào', items: [] as any[], expected: [] as string[] },
        {
            name: '29. Đã khai @since: gõ @ gợi ý đúng @since',
            items: [{ code: 'since', dataType: 'DateTime', valueKind: 'Builtin', value: 'lastTime' }],
            expected: ['@since'],
        },
    ];

    for (const c of variableSuggestCases) {
        test(c.name, async ({ page }) => {
            await page.route('**/api/sharedata/sharedatapacket/getsqlvariables*', (r) => r.fulfill(json({ code: 200, result: { items: c.items } })));
            await openSqlCenter(page);
            await retypeSql(page, 'SELECT * FROM TmsZone WHERE UpdateTime > @');
            await page.keyboard.press('Control+Space');
            await page.waitForTimeout(1500);
            const variables = (await suggestionLabels(page)).filter((label) => label.startsWith('@'));
            expect(variables.map((label) => label.split(',')[0])).toEqual(c.expected);
        });
    }

    test('3. Khung SQL không còn bảng Trường trích xuất; danh sách trường chỉ có trường đã khai, không có nhóm Chưa khai khi SQL chỉ trả cột đã khai', async ({ page }) => {
        await openSqlCenter(page);
        await expect(page.locator('.field-compare-table')).toHaveCount(0);
        const fields = page.locator('.fields-section .vxe-table--main-wrapper');
        await expect(fields).toContainText('speed');
        await expect(fields).toContainText('message');
        await expect(fields).not.toContainText('Chưa khai');
    });

    test('4. Nút Lưu ở màn Mẫu chung khóa mặc định, mở khi sửa SQL', async ({ page }) => {
        await openSqlCenter(page);
        const save = page.locator('.sql-toolbar-bar .el-button--primary').last();
        await expect(save).toBeDisabled();
        await page.locator('.sql-monaco-editor .view-lines').first().click();
        await page.keyboard.press('End');
        await page.keyboard.type(' ', { delay: 100 });
        await page.keyboard.type('WHERE 1 = 1', { delay: 50 });
        await expect(save).toBeEnabled({ timeout: 10_000 });
    });

    /**
     * Description: Một nút Lưu cho cả câu SQL và bảng Tham số câu truy vấn: chỉ gọi API của phần có thay đổi;
     *              đổi cả hai thì lưu bảng biến trước rồi mới lưu câu SQL (để câu SQL không bị báo biến chưa khai).
     * Created date: 25/10/2026
     */
    const singleSaveCases = [
        { name: '30. Chỉ thêm biến: nút Lưu mở, chỉ gọi API lưu biến', editSql: false, expected: ['variables'] },
        { name: '31. Sửa SQL và thêm biến: lưu biến trước rồi lưu SQL', editSql: true, expected: ['variables', 'sql'] },
    ];

    for (const c of singleSaveCases) {
        test(c.name, async ({ page }) => {
            const calls: string[] = [];
            await page.route('**/api/sharedata/sharedatapacket/getsqlvariables*', (r) => r.fulfill(json({ code: 200, result: { items: [] } })));
            await page.route('**/api/sharedata/sharedatapacket/updatesqlvariables*', async (r) => {
                calls.push('variables');
                await r.fulfill(json({ code: 200, result: { items: r.request().postDataJSON().items } }));
            });
            await page.route('**/api/sharedata/sharedatapacket/updateextractsql*', async (r) => {
                calls.push('sql');
                await r.fulfill(json({ code: 200, type: 'success', result: { status: 1 } }));
            });
            await openSqlCenter(page);
            const save = page.locator('.sql-toolbar-bar .el-button--primary').last();
            await expect(save).toBeDisabled();

            await page.locator('.sql-variable-panel .svp-header .el-button', { hasText: 'Thêm' }).click();
            await page.locator('.sql-variable-panel .el-table__body input').first().fill('since');
            await page.locator('.sql-variable-panel .el-table__body .el-select').first().click();
            await page.locator('.svp-source-popper .el-select-dropdown__item', { hasText: 'Mốc thời gian lần trước' }).click();
            if (c.editSql) {
                await page.locator('.sql-monaco-editor .view-lines').first().click();
                await page.keyboard.press('End');
                await page.keyboard.type(' WHERE 1 = 1', { delay: 50 });
            }
            await expect(save).toBeEnabled({ timeout: 10_000 });
            await save.click();
            await expect.poll(() => calls, { timeout: 10_000 }).toEqual(c.expected);
        });
    }

    test('11. Đối tác chưa có SQL riêng: nút Lưu bấm được ngay, tạo SQL riêng đúng nội dung mẫu chung', async ({ page }) => {
        let saved: any = null;
        await page.route('**/api/sharedata/sharedatapacket/getpartnersqllist*', (r) =>
            r.fulfill(json({ code: 200, result: [{ partnerId: 'P1', partnerCode: 'DT-DAT', partnerName: 'Đạt', isCustom: false }] }))
        );
        await page.route('**/api/sharedata/sharedatapacket/getpartnerextractsql*', (r) =>
            r.fulfill(json({ code: 200, result: { partnerId: 'P1', packetCode: '101_commonData', standardSql: SQL_ONE_COLUMN, customSql: null, isCustom: false, hasDifferences: false } }))
        );
        await page.route('**/api/sharedata/sharedatapacket/updatepartnerextractsql*', async (r) => {
            saved = r.request().postDataJSON();
            await r.fulfill(json({ code: 200, type: 'success', result: true }));
        });
        await openSqlCenter(page);
        await page.locator('.sql-partner-tabs-bar .el-radio-button', { hasText: 'Đối tác' }).click();
        await page.locator('.partner-sql-table .el-button[aria-label="Tạo SQL riêng"]').first().click();

        // Chưa sửa gì vẫn tạo được: nút không bị khóa
        const create = page.locator('.sql-toolbar-bar .el-button--primary').last();
        await expect(create).toHaveText('Lưu');
        await expect(create).toBeEnabled({ timeout: 15_000 });
        await create.click();
        await expect.poll(() => saved?.extractSql, { timeout: 10_000 }).toBe(SQL_ONE_COLUMN);
        expect(saved.partnerId).toBe('P1');
    });

    test('12. Đối tác đã có SQL riêng: nút Lưu khóa cho tới khi sửa', async ({ page }) => {
        await page.route('**/api/sharedata/sharedatapacket/getpartnersqllist*', (r) =>
            r.fulfill(json({ code: 200, result: [{ partnerId: 'P1', partnerCode: 'DT-DAT', partnerName: 'Đạt', isCustom: true, updateTime: '2026-10-22T10:00:00' }] }))
        );
        await page.route('**/api/sharedata/sharedatapacket/getpartnerextractsql*', (r) =>
            r.fulfill(json({ code: 200, result: { partnerId: 'P1', packetCode: '101_commonData', standardSql: SQL_ONE_COLUMN, customSql: `${SQL_ONE_COLUMN} WHERE 1 = 1`, isCustom: true, hasDifferences: true } }))
        );
        await openSqlCenter(page);
        await page.locator('.sql-partner-tabs-bar .el-radio-button', { hasText: 'Đối tác' }).click();
        await page.locator('.partner-sql-table .el-button[aria-label="Mở"]').first().click();

        const save = page.locator('.sql-toolbar-bar .el-button--primary').last();
        await expect(save).toHaveText('Lưu');
        await expect(save).toBeDisabled({ timeout: 15_000 });
        await page.locator('.sql-monaco-editor .view-lines').first().click();
        await page.keyboard.press('End');
        await page.keyboard.type(' ', { delay: 100 });
        await page.keyboard.type('AND 2 = 2', { delay: 50 });
        await expect(save).toBeEnabled({ timeout: 10_000 });
    });

    test('13. Chọn nhiều đối tác có SQL riêng rồi đưa về mẫu chung một lượt', async ({ page }) => {
        const resetIds: string[] = [];
        const partners = [
            { partnerId: 'P1', partnerCode: 'DT-A', partnerName: 'Đối tác A', isCustom: true, updateTime: '2026-10-22T10:00:00' },
            { partnerId: 'P2', partnerCode: 'DT-B', partnerName: 'Đối tác B', isCustom: true, updateTime: '2026-10-22T10:05:00' },
            { partnerId: 'P3', partnerCode: 'DT-C', partnerName: 'Đối tác C', isCustom: false },
        ];
        await page.route('**/api/sharedata/sharedatapacket/getpartnersqllist*', (r) =>
            r.fulfill(json({ code: 200, result: partners.map((p) => ({ ...p, isCustom: p.isCustom && !resetIds.includes(p.partnerId), updateTime: resetIds.includes(p.partnerId) ? null : p.updateTime })) }))
        );
        await page.route('**/api/sharedata/sharedatapacket/resetpartnerextractsql*', async (r) => {
            resetIds.push(r.request().postDataJSON().partnerId);
            // Làm chậm để kịp thấy trạng thái đang xóa
            await new Promise((resolve) => setTimeout(resolve, 600));
            await r.fulfill(json({ code: 200, type: 'success', result: true }));
        });
        await openSqlCenter(page);
        await page.locator('.sql-partner-tabs-bar .el-radio-button', { hasText: 'Đối tác' }).click();

        const bulk = page.locator('.partner-sql-toolbar .el-button');
        await expect(bulk).toBeDisabled();
        // Đối tác dùng chung không chọn được; ô ở tiêu đề chỉ chọn các đối tác có SQL riêng
        await expect(page.locator('.partner-sql-table .el-table__body .el-checkbox.is-disabled')).toHaveCount(1);
        await page.locator('.partner-sql-table .el-table__header .el-checkbox').click();
        await expect(bulk).toBeEnabled();
        await expect(bulk).toContainText('(2)');

        await bulk.click();
        const confirm = page.locator('.el-message-box .el-button--primary');
        await confirm.click();
        // Đang xóa: hộp còn mở và nút Xác nhận xoay
        await expect(confirm).toHaveClass(/is-loading/);
        await expect.poll(() => resetIds.slice().sort(), { timeout: 15_000 }).toEqual(['P1', 'P2']);
        // Xóa xong hộp mới đóng
        await expect(page.locator('.el-message-box')).toHaveCount(0, { timeout: 15_000 });
        // Sau khi tải lại, hai đối tác đã về mẫu chung và không còn dòng nào được chọn
        await expect(page.locator('.partner-sql-table .el-table__body .el-tag--primary')).toHaveCount(0, { timeout: 15_000 });
        await expect(bulk).toBeDisabled();
    });

    test('15. Một trang: không còn hai tab, SQL và danh sách trường cùng hiện; Thu gọn ẩn khung soạn thảo, trạng thái nhớ sau khi tải lại', async ({ page }) => {
        await openSqlCenter(page);
        await expect(page.getByRole('tab')).toHaveCount(0);
        const editor = page.locator('.sql-section .sql-monaco-editor');
        await expect(editor).toBeVisible();
        await expect(page.locator('.fields-section .xGrid-style')).toBeVisible();
        await expect(page.locator('.fields-section')).toContainText('(2)');

        // Nút Thu gọn ở thanh trên khung soạn thảo; nút Lưu và danh sách trường vẫn thấy
        const toggle = page.locator('.sql-editor-bar .el-button');
        await toggle.click();
        await expect(editor).toBeHidden();
        await expect(page.locator('.sql-toolbar-bar .el-button--primary').last()).toBeVisible();
        await expect(page.locator('.fields-section .xGrid-style')).toBeVisible();

        await page.reload();
        await page.locator('.packet-col .vxe-body--row').first().click({ timeout: 60_000 });
        await expect(page.locator('.sql-editor-bar')).toBeVisible({ timeout: 30_000 });
        await expect(editor).toBeHidden();
        await toggle.click();
        await expect(page.locator('.sql-section .sql-monaco-editor .monaco-editor').first()).toBeVisible({ timeout: 30_000 });
    });

    test('16. Lọc theo đối tác: bảng gói tin chỉ còn gói của đối tác, có cột Nguồn SQL, tự mở SQL của đối tác', async ({ page }) => {
        const pageBodies: any[] = [];
        await page.route('**/api/sharedata/sharedatapartner/list*', (r) => r.fulfill(json({ code: 200, result: [{ id: 'P1', code: 'DT-E', name: 'Đối tác E' }] })));
        await page.route('**/api/sharedata/sharedatapacket/page*', (r) => {
            const body = r.request().postDataJSON();
            pageBodies.push(body);
            const records = body?.partnerId
                ? [
                      { id: 'PKT_01', code: '101_commonData', name: 'Gói tin dùng chung 101', status: 1, isPartnerCustomSql: false },
                      { id: 'PKT_08', code: '108_custom', name: 'Gói tin 108', status: 1, isPartnerCustomSql: true },
                  ]
                : [{ id: 'PKT_01', code: '101_commonData', name: 'Gói tin dùng chung 101', status: 1 }];
            return r.fulfill(json({ code: 200, result: { records, total: records.length } }));
        });
        await page.route('**/api/sharedata/sharedatapacket/getpartnersqllist*', (r) =>
            r.fulfill(json({ code: 200, result: [{ partnerId: 'P1', partnerCode: 'DT-E', partnerName: 'Đối tác E', isCustom: false }] }))
        );
        await page.route('**/api/sharedata/sharedatapacket/getpartnerextractsql*', (r) =>
            r.fulfill(json({ code: 200, result: { partnerId: 'P1', packetCode: '101_commonData', standardSql: SQL_ONE_COLUMN, customSql: null, isCustom: false, hasDifferences: false } }))
        );
        await openSqlCenter(page);

        // Chưa lọc: không có cột Nguồn SQL
        await expect(page.locator('.packet-col .vxe-header--column', { hasText: 'Nguồn SQL' })).toHaveCount(0);

        await page.locator('.packet-partner-filter .el-select').click();
        await page.getByRole('option', { name: /Đối tác E/ }).click();

        await expect.poll(() => pageBodies.at(-1)?.partnerId, { timeout: 15_000 }).toBe('P1');
        // Chỉ đếm bảng chính: cột cố định trái / phải của vxe-table render thêm hai bảng trùng dòng
        const rows = page.locator('.packet-col .vxe-table--main-wrapper .vxe-body--row');
        await expect(rows).toHaveCount(2, { timeout: 15_000 });
        await expect(page.locator('.packet-col .vxe-table--main-wrapper .vxe-header--column', { hasText: 'Nguồn SQL' })).toBeVisible();
        await expect(rows.nth(0)).toContainText('Dùng chung');
        await expect(rows.nth(1)).toContainText('SQL riêng');
        // Gói đầu tiên tự được chọn và mở thẳng SQL của đối tác đang lọc
        await expect(page.locator('.sql-crumb-title')).toContainText('Đối tác E', { timeout: 15_000 });
    });

    test('17. Không phải SuperAdmin: ẩn phần SQL và các nút thêm / sửa / xóa gói tin, trường', async ({ page }) => {
        await page.addInitScript(() => {
            const key = 'tacwebcore:userInfo';
            const user = JSON.parse(window.sessionStorage.getItem(key) || '{}');
            window.sessionStorage.setItem(key, JSON.stringify({ ...user, account: 'staff', accountType: '1', authBtnList: [] }));
        });
        // Tài khoản thường phải có danh sách nút được cấp (buttons, store đọc thành authBtnList; chỉ SuperAdmin 111 được bỏ qua),
        // nếu thiếu thì chỉ thị v-auth báo lỗi làm bảng kẹt Loading
        const staff = { account: 'staff', accountType: '1', defaultMenu: '/sharedata/dataSource', buttons: [] as string[], authBtnList: [] as string[] };
        await page.route('**/api/system/sysuser/info*', (r) => r.fulfill(json({ code: 200, result: staff })));
        await page.route('**/api/system/sysauth/userinfo*', (r) => r.fulfill(json({ code: 200, result: staff })));
        await page.setViewportSize({ width: 1600, height: 900 });
        await page.goto('/#/sharedata/dataSource');
        await page.locator('.packet-col .vxe-body--row').first().click({ timeout: 60_000 });

        await expect(page.locator('.fields-section .xGrid-style')).toBeVisible({ timeout: 30_000 });
        await expect(page.locator('.sql-section')).toHaveCount(0);
        await expect(page.locator('.packet-partner-filter')).toHaveCount(0);
        await expect(page.locator('.el-button[aria-label="Thêm mới"]')).toHaveCount(0);
        await expect(page.locator('.vxe-body--row .el-button[aria-label="Sửa"]')).toHaveCount(0);
        await expect(page.locator('.vxe-body--row .el-button[aria-label="Xóa"]')).toHaveCount(0);
    });

    test('20. Bảng gói tin không còn nút Tìm kiếm / Đặt lại, tự tìm sau 500ms kể từ lần gõ cuối, Enter tìm ngay', async ({ page }) => {
        const bodies: any[] = [];
        await page.route('**/api/sharedata/sharedatapacket/page*', (r) => {
            bodies.push(r.request().postDataJSON());
            return r.fulfill(json({ code: 200, result: { records: [{ id: 'PKT_01', code: '101_commonData', name: 'Gói tin dùng chung 101', status: 1 }], total: 1 } }));
        });
        await openSqlCenter(page);
        const card = page.locator('.search-card-packet');
        await expect(card.getByRole('button', { name: /Tìm kiếm|Đặt lại/ })).toHaveCount(0);

        const before = bodies.length;
        const code = card.locator('input').first();
        await code.pressSequentially('10', { delay: 100 });
        // 300ms sau lần gõ cuối vẫn chưa gọi, sau 500ms mới gọi đúng một lần
        await page.waitForTimeout(300);
        expect(bodies.length).toBe(before);
        await expect.poll(() => bodies.length, { timeout: 3000 }).toBe(before + 1);
        expect(bodies.at(-1).code).toBe('10');

        // Enter tìm ngay, không chờ hết 500ms
        await code.pressSequentially('1', { delay: 30 });
        await code.press('Enter');
        await expect.poll(() => bodies.at(-1)?.code, { timeout: 400 }).toBe('101');
    });

    test('22. Khung soạn SQL giãn hết chiều cao phần SQL: thanh kéo chiều cao nằm sát đáy, không để khoảng trống phía dưới', async ({ page }) => {
        await openSqlCenter(page);
        const scroller = (await page.locator('.sql-editor-section').boundingBox())!;
        const resizer = (await page.locator('.sql-editor-resizer').boundingBox())!;
        expect(scroller.y + scroller.height - (resizer.y + resizer.height)).toBeLessThan(12);
    });

    test('27. Lấy cột của bảng nguồn gửi mã định danh bảng (tableId) trên URL, không gửi tên bảng', async ({ page }) => {
        const urls: string[] = [];
        await page.route('**/api/sharedata/sharedatapacket/getmetadatafields*', async (r) => {
            urls.push(r.request().url());
            await r.fulfill(json({ code: 200, result: [{ columnName: 'Speed', dataType: 'int' }] }));
        });
        await openSqlCenter(page);
        await expect.poll(() => urls.length, { timeout: 20_000 }).toBeGreaterThan(0);
        for (const url of urls) {
            const query = new URL(url).searchParams;
            // Hàm API sinh từ Swagger gửi khoá TableId; máy chủ đọc khoá truy vấn không phân biệt hoa thường
            expect(query.get('tableId') ?? query.get('TableId')).toBe('tbl-traffic');
            expect(query.has('tableName') || query.has('TableName')).toBe(false);
            expect(url).not.toContain('TmsTrafficData');
        }
    });

    /**
     * Description: Danh sách trường sinh từ câu SQL: bấm Dò cột trả về thì danh sách thay theo cột câu SQL trả về (chưa ghi) và nút Lưu mở,
     *              kể cả khi câu SQL không đổi; bấm Lưu gửi câu SQL kèm phần sửa trên từng trường (thay cho Khai tất cả / tự khai khi lưu).
     * Created date: 26/10/2026
     */
    test('25. Dò cột trả về: danh sách trường thay theo câu SQL, Lưu gửi kèm danh sách trường', async ({ page }) => {
        let saved: any = null;
        await page.route('**/api/sharedata/sharedatapacket/describecolumns*', (r) =>
            r.fulfill(
                json({
                    code: 200,
                    result: {
                        isValid: true,
                        fields: [...SAVED_FIELDS, { key: 'extraCol', type: 'string', dbType: 'nvarchar(64)' }],
                        added: ['extraCol'],
                        lost: [],
                        typeChanged: [],
                        affectedMappings: [],
                    },
                })
            )
        );
        await page.route('**/api/sharedata/sharedatapacket/updateextractsql*', async (r) => {
            saved = r.request().postDataJSON();
            await r.fulfill(json({ code: 200, result: { status: 1, fields: [...SAVED_FIELDS, { key: 'extraCol', type: 'string' }] } }));
        });
        await openSqlCenter(page);
        const save = page.locator('.sql-toolbar-bar .el-button--primary').last();
        await expect(save).toBeDisabled();

        await page.getByRole('button', { name: 'Dò cột trả về' }).click();
        const fields = page.locator('.fields-section .vxe-table--main-wrapper');
        await expect(fields).toContainText('extraCol', { timeout: 15_000 });
        await expect(page.locator('.fields-section')).toContainText('Dò được 3 cột');
        await expect(save).toBeEnabled({ timeout: 10_000 });

        await save.click();
        await expect.poll(() => saved?.extractSql, { timeout: 10_000 }).toBe(SQL_ONE_COLUMN);
        expect(saved.fields.map((f: any) => f.key)).toEqual(['speed', 'message', 'extraCol']);
        expect(saved.fields.find((f: any) => f.key === 'message').required).toBe(true);
    });

    /**
     * Description: Ghi ngược (chiều nhận): thêm câu ghi, kiểm an toàn hiện cảnh báo và khoá bản ghi dò được; nút Lưu chung mở,
     *              bấm Lưu hỏi xác nhận vì còn cảnh báo rồi gửi cả danh sách câu ghi theo thứ tự.
     * Created date: 26/10/2026
     */
    test('32. Ghi ngược: thêm câu ghi, kiểm an toàn, Lưu chung gửi danh sách câu ghi', async ({ page }) => {
        let saved: any = null;
        await page.route('**/api/sharedata/sharedatapacket/validatewritesql*', (r) =>
            r.fulfill(
                json({
                    code: 200,
                    result: { isValid: true, keyFields: ['speed'], checks: [{ level: 'Warning', message: 'Câu MERGE chưa có OUTPUT $action' }] },
                })
            )
        );
        await page.route('**/api/sharedata/sharedatapacket/updatewritesqls*', async (r) => {
            saved = r.request().postDataJSON();
            await r.fulfill(json({ code: 200, result: { packetCode: '101_commonData', items: saved.items.map((i: any, n: number) => ({ ...i, id: `W${n + 1}`, orderNo: n + 1, keyFields: ['speed'] })) } }));
        });
        await openSqlCenter(page);
        const panel = page.locator('.write-sql-panel');
        await expect(panel).toContainText('Chỉ gửi');
        const save = page.locator('.sql-toolbar-bar .el-button--primary').last();

        await panel.getByRole('button', { name: 'Thêm câu ghi' }).click();
        await panel.locator('.wsp-table .el-table__body input').first().fill('dbo.TmsTrafficData');
        await panel.locator('.wsp-editor textarea').fill('MERGE dbo.TmsTrafficData AS t USING (SELECT speed FROM OPENJSON(@records) WITH (speed int)) AS s ON t.Speed = s.speed WHEN MATCHED THEN UPDATE SET t.Speed = s.speed;');
        await expect(save).toBeEnabled({ timeout: 10_000 });

        await panel.getByRole('button', { name: 'Kiểm an toàn câu ghi' }).click();
        await expect(panel.locator('.wsp-check-warning')).toContainText('OUTPUT $action');
        await expect(panel.locator('.wsp-key-tag')).toContainText('speed');

        await save.click();
        await page.locator('.el-message-box .el-button--primary').click();
        await expect.poll(() => saved?.items?.length, { timeout: 10_000 }).toBe(1);
        expect(saved.packetCode).toBe('101_commonData');
        expect(saved.items[0].tableName).toBe('dbo.TmsTrafficData');
        await expect(panel).toContainText('1 câu ghi');
    });

    /**
     * Description: Chạy thử câu ghi: hộp chạy thử gửi dữ liệu mẫu cùng các câu đang soạn, hiện số dòng thêm / sửa từng câu (máy chủ quay lui sau khi chạy).
     * Created date: 26/10/2026
     */
    test('33. Ghi ngược: chạy thử gửi dữ liệu mẫu và hiện số dòng thêm / sửa', async ({ page }) => {
        let tried: any = null;
        await page.route('**/api/sharedata/sharedatapacket/getwritesqls*', (r) =>
            r.fulfill(
                json({
                    code: 200,
                    result: {
                        packetCode: '101_commonData',
                        items: [{ id: 'W1', orderNo: 1, tableName: 'dbo.TmsTrafficData', writeSql: 'MERGE dbo.TmsTrafficData AS t USING (SELECT 1 AS x) AS s ON 1 = 0 WHEN NOT MATCHED THEN INSERT (Speed) VALUES (1) OUTPUT $action;', status: 1, keyFields: [] }],
                    },
                })
            )
        );
        await page.route('**/api/sharedata/sharedatapacket/trywritesql*', async (r) => {
            tried = r.request().postDataJSON();
            await r.fulfill(json({ code: 200, result: { isSuccess: true, recordCount: 1, results: [{ orderNo: 1, tableName: 'dbo.TmsTrafficData', inserted: 1, updated: 0 }] } }));
        });
        await openSqlCenter(page);
        const panel = page.locator('.write-sql-panel');
        await panel.getByRole('button', { name: 'Chạy thử' }).click();
        const dialog = page.locator('.wsp-try-dialog');
        await expect(dialog).toBeVisible();
        await dialog.locator('textarea').fill('[{ "speed": 50 }]');
        await dialog.locator('.dialog-footer .el-button--primary').click();

        await expect.poll(() => tried?.recordsJson, { timeout: 10_000 }).toBe('[{ "speed": 50 }]');
        expect(tried.items).toHaveLength(1);
        await expect(dialog.locator('.wsp-try-table')).toContainText('dbo.TmsTrafficData');
        await expect(dialog.locator('.wsp-try-table .el-table__body')).toContainText('1');
    });

    /**
     * Description: Danh sách gói tin có cột Ghi ngược hiện số câu ghi chiều nhận của gói (0 = Chỉ gửi);
     *              khối Ghi ngược chỉ có ở màn mẫu chung, sang SQL riêng của đối tác thì ẩn vì câu ghi đi theo gói.
     * Created date: 26/10/2026
     */
    test('34. Ghi ngược: cột số câu ghi ở danh sách gói, ẩn khối câu ghi khi sửa SQL riêng của đối tác', async ({ page }) => {
        await page.route('**/api/sharedata/sharedatapacket/page*', (r) =>
            r.fulfill(json({ code: 200, result: { records: [{ id: 'PKT_01', code: '101_commonData', name: 'Gói tin dùng chung 101', status: 1, writeSqlCount: 3 }], total: 1 } }))
        );
        await page.route('**/api/sharedata/sharedatapacket/getpartnersqllist*', (r) =>
            r.fulfill(json({ code: 200, result: [{ partnerId: 'P1', partnerCode: 'DT-DAT', partnerName: 'Đạt', isCustom: false }] }))
        );
        await page.route('**/api/sharedata/sharedatapacket/getpartnerextractsql*', (r) =>
            r.fulfill(json({ code: 200, result: { partnerId: 'P1', packetCode: '101_commonData', standardSql: SQL_ONE_COLUMN, customSql: null, isCustom: false, hasDifferences: false } }))
        );
        await openSqlCenter(page);
        await expect(page.locator('.packet-col .vxe-table--main-wrapper .vxe-header--column', { hasText: 'Ghi ngược' })).toBeVisible();
        await expect(page.locator('.packet-col .vxe-table--main-wrapper .vxe-body--row').first()).toContainText('3 câu ghi');
        await expect(page.locator('.write-sql-panel')).toBeVisible();

        await page.locator('.sql-partner-tabs-bar .el-radio-button', { hasText: 'Đối tác' }).click();
        await page.locator('.partner-sql-table .el-button[aria-label="Tạo SQL riêng"]').first().click();
        await expect(page.locator('.write-sql-panel')).toHaveCount(0, { timeout: 15_000 });
    });

    test('14. Chuỗi ngôn ngữ chứa "@" phải viết {\'@\'} để vue-i18n không báo Invalid linked format', async () => {
        const dir = resolveWebvuePath('i18n/lang/its/sharedata');
        for (const file of ['vi-vn.json', 'en-us.json']) {
            const messages = JSON.parse(fs.readFileSync(`${dir}/${file}`, 'utf8')) as Record<string, string>;
            const bad = Object.entries(messages)
                .filter(([, value]) => typeof value === 'string' && /(?<!')@(?!')/.test(value))
                .map(([key]) => key);
            expect(bad, `${file}: chuỗi chứa "@" trần (cần viết {'@'}): ${bad.join(', ')}`).toEqual([]);
        }
    });
});
