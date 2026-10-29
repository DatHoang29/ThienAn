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
        await page.route('**/api/sharedata/sharedatapacket/getpartnersqllist*', (r) => r.fulfill(json({ code: 200, result: [] })));
        await page.route('**/api/sharedata/sharedatapacket/getsqlvariables*', (r) => r.fulfill(json({ code: 200, result: { items: [] } })));
        await page.route('**/api/sharedata/sharedatapacket/getwritesqls*', (r) =>
            r.fulfill(json({ code: 200, result: { packetCode: '101_commonData', writeSql: '', affectedTables: [], detectedTables: [], keyFields: [] } }))
        );
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
        // Bấm dòng nhóm thì thu gọn / mở nhóm (xem ca 37)
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
        { name: '30. Chỉ thêm biến hệ thống: nút Lưu mở, chỉ gọi API lưu biến', editSql: false, useConst: false, expected: ['variables'] },
        { name: '30b. Biến Giá trị nhập (Const): chọn kiểu Số nguyên, nhập giá trị, Lưu gửi valueKind Const', editSql: false, useConst: true, expected: ['variables'] },
        { name: '31. Sửa SQL và thêm biến: lưu biến trước rồi lưu SQL', editSql: true, useConst: false, expected: ['variables', 'sql'] },
    ];

    for (const c of singleSaveCases) {
        test(c.name, async ({ page }) => {
            const calls: string[] = [];
            let savedVars: any[] = [];
            const initialItems = c.editSql
                ? []
                : [{ code: c.useConst ? 'pageSize' : 'since', dataType: 'DateTime', valueKind: 'Builtin', value: 'now' }];
            await page.route('**/api/sharedata/sharedatapacket/getsqlvariables*', (r) => r.fulfill(json({ code: 200, result: { items: initialItems } })));
            await page.route('**/api/sharedata/sharedatapacket/updatesqlvariables*', async (r) => {
                calls.push('variables');
                savedVars = r.request().postDataJSON().items;
                await r.fulfill(json({ code: 200, result: { items: savedVars } }));
            });
            await page.route('**/api/sharedata/sharedatapacket/updateextractsql*', async (r) => {
                calls.push('sql');
                await r.fulfill(json({ code: 200, type: 'success', result: { status: 1 } }));
            });
            await openSqlCenter(page);
            const save = page.locator('.sql-toolbar-bar .el-button--primary').last();
            await expect(save).toBeDisabled();
            // Khối 3 không còn nút Thêm mới: tham số được trích từ @tênBiến trong câu SQL khi bấm Lưu & phân tích
            await expect(page.locator('.sql-variable-panel .svp-header .el-button')).toHaveCount(0);

            if (c.editSql) {
                await page.locator('.sql-monaco-editor .view-lines').first().click();
                await page.keyboard.press('End');
                await page.keyboard.type(' WHERE 1 = 1 OR @now IS NULL', { delay: 50 });
                await page.keyboard.press('Escape');
            } else if (c.useConst) {
                await page.locator('.sql-variable-panel .el-table__body .svp-source-cell .el-select').first().click();
                await page.locator('.svp-source-popper .el-select-dropdown__item', { hasText: 'Giá trị cố định' }).click();
                await expect(page.locator('.el-popper.svp-source-popper').first()).toBeHidden();
                await page.locator('.sql-variable-panel .el-table__body .svp-const-input input').first().fill('500');
            } else {
                await page.locator('.sql-variable-panel .el-table__body .svp-source-cell .el-select').first().click();
                await page.locator('.svp-source-popper .el-select-dropdown__item', { hasText: 'Lần trước (theo key)' }).click();
            }
            await expect(save).toBeEnabled({ timeout: 10_000 });
            await save.click();
            await expect.poll(() => calls, { timeout: 10_000 }).toEqual(c.expected);
            if (c.useConst) {
                expect(savedVars[0]).toMatchObject({ code: 'pageSize', dataType: 'Int', valueKind: 'Const', value: '500' });
            } else if (c.editSql) {
                expect(savedVars[0]).toMatchObject({ code: 'now', dataType: 'DateTime', valueKind: 'Builtin', value: 'now' });
            } else {
                expect(savedVars[0]).toMatchObject({ code: 'since', valueKind: 'Builtin', value: 'lastKey' });
            }
        });
    }

    test('11. Bấm + Đối tác: chọn đối tác chưa có SQL riêng và mẫu khởi tạo -> mở tab đối tác rồi Lưu tạo SQL riêng', async ({ page }) => {
        let saved: any = null;
        await page.route('**/api/sharedata/sharedatapacket/getpartnersqllist*', (r) =>
            r.fulfill(
                json({
                    code: 200,
                    result: [{ partnerId: 'P1', partnerCode: 'DT-DAT', partnerName: 'Đạt', isCustom: Boolean(saved) }],
                })
            )
        );
        await page.route('**/api/sharedata/sharedatapacket/getpartnerextractsql*', (r) =>
            r.fulfill(
                json({
                    code: 200,
                    result: {
                        partnerId: 'P1',
                        packetCode: '101_commonData',
                        standardSql: SQL_ONE_COLUMN,
                        customSql: saved ? SQL_ONE_COLUMN : null,
                        isCustom: Boolean(saved),
                        hasDifferences: false,
                        fields: SAVED_FIELDS,
                    },
                })
            )
        );
        await page.route('**/api/sharedata/sharedatapacket/updatepartnerextractsql*', async (r) => {
            saved = r.request().postDataJSON();
            await r.fulfill(json({ code: 200, type: 'success', result: true }));
        });
        await openSqlCenter(page);
        await expect(page.locator('.add-partner-tab-btn')).toHaveText('Tạo cấu hình riêng +');
        await expect(page.locator('.add-partner-tab-btn .el-icon')).toHaveCount(0);
        await page.locator('.add-partner-tab-btn').click();

        const dialog = page.locator('.partner-setup-dialog');
        await expect(dialog).toBeVisible();
        await expect(dialog.locator('.psd-header')).toHaveText('Thiết lập cấu hình riêng cho đối tác');
        await expect(dialog.locator('.alert.info')).toHaveText(
            'Đối tác sẽ có câu truy vấn, trường và tham số riêng.\nDùng nút So sánh với mẫu chung để xem khác biệt khi mẫu thay đổi.'
        );
        // Chưa đối tác nào có cấu hình riêng: lựa chọn sao chép từ đối tác bị khoá, mặc định lấy Mẫu chung
        await expect(dialog.locator('.el-radio', { hasText: 'Mẫu chung' })).toHaveClass(/is-checked/);
        await expect(dialog.locator('.el-radio', { hasText: 'Cấu hình riêng của đối tác' })).toHaveClass(/is-disabled/);
        await expect(dialog.locator('.dialog-footer .el-button--primary')).toHaveText('Tạo & mở tab');
        await dialog.locator('.dialog-footer .el-button--primary').click();
        await expect(dialog).toBeHidden();

        await expect.poll(() => saved?.extractSql, { timeout: 10_000 }).toBe(SQL_ONE_COLUMN);
        expect(saved.partnerId).toBe('P1');
        await expect(page.locator('.sql-partner-tabs .el-tabs__item.is-active')).toContainText('DT-DAT');
    });

    /**
     * Description: Hộp Thiết lập cấu hình riêng chọn "Cấu hình riêng của đối tác": bấm Tạo & mở tab tạo ngay SQL riêng cho đối tác mới
     *              bằng đúng câu SQL của đối tác nguồn và xếp tab đối tác mới ở cuối.
     * Created date: 27/10/2026
     */
    test('11b. Bấm + Đối tác, sao chép từ cấu hình riêng của đối tác khác -> tab mới nhận SQL của đối tác nguồn và Lưu đúng câu đó', async ({ page }) => {
        const VDOT_SQL = `${SQL_ONE_COLUMN} WHERE td.Speed > 0`;
        let saved: any = null;
        await page.route('**/api/sharedata/sharedatapacket/getpartnersqllist*', (r) =>
            r.fulfill(
                json({
                    code: 200,
                    result: [
                        { partnerId: 'P1', partnerCode: 'DT-DAT', partnerName: 'Đạt', isCustom: Boolean(saved) },
                        { partnerId: 'P2', partnerCode: 'VDOT', partnerName: 'VDOT', isCustom: true },
                    ],
                })
            )
        );
        await page.route('**/api/sharedata/sharedatapacket/getpartnerextractsql*', (r) => {
            const query = new URL(r.request().url()).searchParams;
            const partnerId = query.get('partnerId') ?? query.get('PartnerId');
            const isSource = partnerId === 'P2';
            const customSql = isSource ? VDOT_SQL : saved ? saved.extractSql : null;
            return r.fulfill(
                json({
                    code: 200,
                    result: { partnerId, packetCode: '101_commonData', standardSql: SQL_ONE_COLUMN, customSql, isCustom: Boolean(customSql), hasDifferences: Boolean(customSql), fields: SAVED_FIELDS },
                })
            );
        });
        await page.route('**/api/sharedata/sharedatapacket/updatepartnerextractsql*', async (r) => {
            saved = r.request().postDataJSON();
            await r.fulfill(json({ code: 200, type: 'success', result: true }));
        });
        await openSqlCenter(page);
        await page.locator('.add-partner-tab-btn').click();

        const dialog = page.locator('.partner-setup-dialog');
        await expect(dialog).toBeVisible();
        const customRadio = dialog.locator('.el-radio', { hasText: 'Cấu hình riêng của đối tác' });
        const otherSelect = dialog.locator('.setup-other-select');
        // Đang chọn Mẫu chung thì ô chọn đối tác nguồn bị khoá; chọn Cấu hình riêng thì mở và mặc định là VDOT
        await expect(otherSelect.locator('.el-select__wrapper')).toHaveClass(/is-disabled/);
        await customRadio.click();
        await expect(customRadio).toHaveClass(/is-checked/);
        await expect(otherSelect.locator('.el-select__wrapper')).not.toHaveClass(/is-disabled/);
        await expect(otherSelect).toContainText('VDOT');
        await dialog.locator('.dialog-footer .el-button--primary').click();
        await expect(dialog).toBeHidden();

        await expect.poll(() => saved?.extractSql, { timeout: 10_000 }).toBe(VDOT_SQL);
        expect(saved.partnerId).toBe('P1');
        await expect(page.locator('.sql-partner-tabs .el-tabs__item.is-active')).toContainText('DT-DAT');
        await expect(page.locator('.sql-partner-tabs .el-tabs__item').last()).toContainText('DT-DAT');
    });

    test('12. Đối tác đã có SQL riêng: hiện tab đối tác, nút Lưu khóa cho tới khi sửa, bảng trường hiện cột So mẫu chung', async ({ page }) => {
        await page.route('**/api/sharedata/sharedatapacket/getpartnersqllist*', (r) =>
            r.fulfill(json({ code: 200, result: [{ partnerId: 'P1', partnerCode: 'DT-DAT', partnerName: 'Đạt', isCustom: true, updateTime: '2026-10-22T10:00:00' }] }))
        );
        await page.route('**/api/sharedata/sharedatapacket/getpartnerextractsql*', (r) =>
            r.fulfill(
                json({
                    code: 200,
                    result: {
                        partnerId: 'P1',
                        packetCode: '101_commonData',
                        standardSql: SQL_ONE_COLUMN,
                        customSql: `${SQL_ONE_COLUMN} WHERE 1 = 1`,
                        isCustom: true,
                        hasDifferences: true,
                        commonFields: SAVED_FIELDS,
                        fields: [
                            { key: 'speed', type: 'string', dbType: 'nvarchar(64)', label: 'Tốc độ', required: false },
                            { key: 'partnerExtra', type: 'string', dbType: 'nvarchar(64)', label: 'Trường riêng', required: false },
                        ],
                    },
                })
            )
        );
        await openSqlCenter(page);
        await page.locator('.sql-partner-tabs .el-tabs__item', { hasText: 'DT-DAT' }).click();

        const save = page.locator('.sql-toolbar-bar .el-button--primary').last();
        await expect(save).toContainText('Lưu & phân tích');
        await expect(save).toBeDisabled({ timeout: 15_000 });

        // Toolbar bên trái hiện mã đối tác và chip phiên bản mẫu theo wireframe
        await expect(page.locator('.sql-toolbar-left')).toContainText('DT-DAT');

        // Bảng trường hiện cột "So mẫu chung" với đầy đủ trạng thái: Khác (speed), Thiếu (message), Mới (partnerExtra)
        const fieldsGrid = page.locator('.fields-section .vxe-table--main-wrapper');
        await expect(fieldsGrid.locator('.vxe-header--column', { hasText: 'So mẫu chung' })).toBeVisible();
        // Ký hiệu vòng tròn màu như wireframe: ≠ khác, - thiếu, + mới
        await expect(fieldsGrid.locator('.cmp-st.diff')).toHaveText('≠');
        await expect(fieldsGrid.locator('.cmp-st.missing')).toHaveText('-');
        await expect(fieldsGrid.locator('.cmp-st.new')).toHaveText('+');

        // Thanh tiêu đề khối trường: khối chú giải + nút bên phải không đè lên tiêu đề, kể cả khi màn hẹp
        for (const width of [1600, 1100]) {
            await page.setViewportSize({ width, height: 900 });
            await page.waitForTimeout(400);
            const left = (await page.locator('.fields-bar-left').boundingBox())!;
            const right = (await page.locator('.fields-bar-right').boundingBox())!;
            const sameRow = Math.abs(left.y - right.y) < left.height;
            if (sameRow) expect(left.x + left.width).toBeLessThanOrEqual(right.x + 1);
        }
        await page.setViewportSize({ width: 1600, height: 900 });

        await page.locator('.sql-monaco-editor .view-lines').first().click();
        await page.keyboard.press('End');
        await page.keyboard.type(' ', { delay: 100 });
        await page.keyboard.type('AND 2 = 2', { delay: 50 });
        await expect(save).toBeEnabled({ timeout: 10_000 });
    });

    test('13. So sánh tất cả đối tác (ma trận trường + diff SQL) và dùng lại mẫu chung từ tab đối tác', async ({ page }) => {
        const resetIds: string[] = [];
        const partners = [
            { partnerId: 'P1', partnerCode: 'DT-A', partnerName: 'Đối tác A', isCustom: true, updateTime: '2026-10-22T10:00:00' },
            { partnerId: 'P2', partnerCode: 'DT-B', partnerName: 'Đối tác B', isCustom: true, updateTime: '2026-10-22T10:05:00' },
        ];
        await page.route('**/api/sharedata/sharedatapacket/getpartnersqllist*', (r) =>
            r.fulfill(json({ code: 200, result: partners.map((p) => ({ ...p, isCustom: p.isCustom && !resetIds.includes(p.partnerId) })) }))
        );
        await page.route('**/api/sharedata/sharedatapacket/getpartnerextractsql*', (r) =>
            r.fulfill(
                json({
                    code: 200,
                    result: {
                        partnerId: 'P1',
                        packetCode: '101_commonData',
                        standardSql: SQL_ONE_COLUMN,
                        customSql: `${SQL_ONE_COLUMN} WHERE 1 = 1`,
                        isCustom: true,
                        hasDifferences: true,
                        commonFields: SAVED_FIELDS,
                        fields: SAVED_FIELDS,
                    },
                })
            )
        );
        await page.route('**/api/sharedata/sharedatapacket/resetpartnerextractsql*', async (r) => {
            resetIds.push(r.request().postDataJSON().partnerId);
            await r.fulfill(json({ code: 200, type: 'success', result: true }));
        });
        await openSqlCenter(page);

        const compareAllBtn = page.locator('.compare-all-btn', { hasText: 'So sánh tất cả (2)' });
        await expect(compareAllBtn).toBeVisible();
        await expect(compareAllBtn.locator('.el-icon')).toHaveCount(0);
        await compareAllBtn.click();
        const compareDialog = page.locator('.compare-all-dialog');
        await expect(compareDialog).toBeVisible();
        await expect(compareDialog.locator('.cad-table')).toContainText('DT-A');
        await expect(compareDialog.locator('.cad-table')).toContainText('DT-B');
        await compareDialog.locator('.el-dialog__headerbtn').click();
        await expect(compareDialog).toBeHidden();

        await page.locator('.sql-partner-tabs .el-tabs__item', { hasText: 'DT-A' }).click();
        await page.locator('.sql-toolbar-bar .el-button', { hasText: 'Dùng lại mẫu chung' }).click();
        await page.locator('.el-message-box .el-button--primary').click();
        await expect.poll(() => resetIds, { timeout: 10_000 }).toEqual(['P1']);
    });

    test('15. Hai tab Chiều gửi / Chiều nhận; ở Chiều gửi, SQL và danh sách trường cùng hiện; Thu gọn ẩn khung soạn thảo', async ({ page }) => {
        await openSqlCenter(page);
        await expect(page.locator('.direction-tabs .el-tabs__item')).toHaveCount(2);
        const editor = page.locator('.sql-section .sql-monaco-editor');
        await expect(editor).toBeVisible();
        await expect(page.locator('.fields-section .xGrid-style')).toBeVisible();
        await expect(page.locator('.fields-section')).toContainText('(2)');

        // Nút Thu gọn ở thanh trên khung soạn thảo; nút Lưu và danh sách trường vẫn thấy
        // Nút chỉ có icon, tên thao tác ở aria-label / tooltip
        const toggle = page.locator('.sql-section .sql-editor-bar-actions .el-button').last();
        await expect(toggle).toHaveAttribute('aria-label', 'Thu gọn');
        await toggle.click();
        await expect(editor).toBeHidden();
        await expect(page.locator('.sql-toolbar-bar .el-button--primary').last()).toBeVisible();
        await expect(page.locator('.fields-section .xGrid-style')).toBeVisible();

        await page.reload();
        await page.locator('.packet-col .vxe-body--row').first().click({ timeout: 60_000 });
        await expect(page.locator('.sql-section .sql-editor-bar')).toBeVisible({ timeout: 30_000 });
        await expect(editor).toBeHidden();
        await expect(toggle).toHaveAttribute('aria-label', 'Mở rộng');
        await toggle.click();
        await expect(page.locator('.sql-section .sql-monaco-editor .monaco-editor').first()).toBeVisible({ timeout: 30_000 });
    });

    test('16. Lọc theo đối tác: bảng gói tin chỉ còn gói của đối tác, có cột Nguồn SQL, tự chọn tab đối tác khi có SQL riêng', async ({ page }) => {
        const pageBodies: any[] = [];
        await page.route('**/api/sharedata/sharedatapartner/list*', (r) => r.fulfill(json({ code: 200, result: [{ id: 'P1', code: 'DT-E', name: 'Đối tác E' }] })));
        await page.route('**/api/sharedata/sharedatapacket/page*', (r) => {
            const body = r.request().postDataJSON();
            pageBodies.push(body);
            const records = body?.partnerId
                ? [
                      { id: 'PKT_01', code: '101_commonData', name: 'Gói tin dùng chung 101', status: 1, isPartnerCustomSql: true },
                      { id: 'PKT_08', code: '108_custom', name: 'Gói tin 108', status: 1, isPartnerCustomSql: false },
                  ]
                : [{ id: 'PKT_01', code: '101_commonData', name: 'Gói tin dùng chung 101', status: 1 }];
            return r.fulfill(json({ code: 200, result: { records, total: records.length } }));
        });
        await page.route('**/api/sharedata/sharedatapacket/getpartnersqllist*', (r) =>
            r.fulfill(json({ code: 200, result: [{ partnerId: 'P1', partnerCode: 'DT-E', partnerName: 'Đối tác E', isCustom: true }] }))
        );
        await page.route('**/api/sharedata/sharedatapacket/getpartnerextractsql*', (r) =>
            r.fulfill(json({ code: 200, result: { partnerId: 'P1', packetCode: '101_commonData', standardSql: SQL_ONE_COLUMN, customSql: SQL_ONE_COLUMN, isCustom: true, hasDifferences: false, fields: SAVED_FIELDS } }))
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
        await expect(rows.nth(0)).toContainText('SQL riêng');
        await expect(rows.nth(1)).toContainText('Dùng chung');
        // Tải chậm (27/10): không tự chọn gói; bấm gói có SQL riêng thì mở thẳng tab SQL của đối tác đang lọc
        await rows.nth(0).click();
        await expect(page.locator('.sql-partner-tabs .el-tabs__item.is-active')).toContainText('DT-E', { timeout: 15_000 });

        // Xóa ô lọc đối tác: thứ tự sắp xếp mặc định (orderNo asc) được bảo toàn, không bị xáo trộn
        await page.locator('.packet-partner-filter .el-select').hover();
        const clearBtn = page.locator('.packet-partner-filter .el-select__clear');
        if (await clearBtn.isVisible()) {
            await clearBtn.click();
        } else {
            await page.locator('.packet-partner-filter .el-select input').fill('');
            await page.locator('.packet-partner-filter .el-select input').press('Enter');
        }
        await expect.poll(() => pageBodies.at(-1)?.partnerId, { timeout: 15_000 }).toBeUndefined();
        await expect.poll(() => pageBodies.at(-1)?.field, { timeout: 15_000 }).toBe('orderNo');
        await expect.poll(() => pageBodies.at(-1)?.order, { timeout: 15_000 }).toBe('asc');
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

    test('22. Khung soạn SQL giới hạn chiều cao tự động tối đa 200px với câu SQL dài và thanh kéo nằm sát đáy', async ({ page }) => {
        const longSql = Array.from({ length: 28 }, (_, i) => (i === 0 ? 'SELECT TOP (@pageSize)' : i < 27 ? `    i.Col${i} AS col${i},` : '    i.ID AS id FROM TmsIncident i')).join('\n');
        await page.route('**/api/sharedata/sharedatapacket/getextractsql*', (r) =>
            r.fulfill(json({ code: 200, result: { extractSql: longSql, status: 1, fields: SAVED_FIELDS } }))
        );
        await openSqlCenter(page);
        const editorBox = (await page.locator('.sql-section .sql-monaco-editor').boundingBox())!;
        expect(editorBox.height).toBeLessThanOrEqual(200);
        const scroller = (await page.locator('.sql-editor-section').boundingBox())!;
        const resizer = (await page.locator('.sql-section .sql-editor-resizer').boundingBox())!;
        // Thanh kéo dính ngay dưới khung soạn và nằm sát đáy vùng cuộn (không còn dòng chú thích dư thừa)
        expect(Math.abs(resizer.y - (editorBox.y + editorBox.height))).toBeLessThan(2);
        expect(scroller.y + scroller.height - (resizer.y + resizer.height)).toBeLessThan(12);
    });

    test('27. Lấy cột của bảng nguồn gửi mã định danh bảng (Id) trên URL, không gửi tên bảng', async ({ page }) => {
        const urls: string[] = [];
        await page.route('**/api/sharedata/sharedatapacket/getmetadatafields*', async (r) => {
            urls.push(r.request().url());
            await r.fulfill(json({ code: 200, result: [{ columnName: 'Speed', dataType: 'int' }] }));
        });
        await openSqlCenter(page);
        await expect.poll(() => urls.length, { timeout: 20_000 }).toBeGreaterThan(0);
        for (const url of urls) {
            const query = new URL(url).searchParams;
            // Gửi khoá Id (không lộ tên khoá TableId); máy chủ đọc khoá truy vấn không phân biệt hoa thường
            expect(query.get('Id')).toBe('tbl-traffic');
            expect(query.get('TableId')).toBeNull();
            expect(query.has('tableName') || query.has('TableName')).toBe(false);
            expect(url).not.toContain('TmsTrafficData');
        }
    });

    /**
     * Description: Danh sách trường sinh từ câu SQL: lưu câu SQL gửi kèm danh sách trường, sửa qua popup Chỉnh sửa.
     * Created date: 26/10/2026
     */
    test('25. Lưu câu SQL và hộp Chỉnh sửa trường: danh sách trường chỉ xem trên lưới, sửa qua popup Chỉnh sửa, Lưu gửi kèm danh sách trường', async ({ page }) => {
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
            const fields = (saved.fields || []).map((f: any) => ({
                key: f.key,
                label: f.label || (f.key === 'speed' ? 'Tốc độ' : f.key === 'message' ? 'Thông tin trạng thái' : f.key),
                type: f.key === 'speed' ? 'number' : 'string',
                required: f.required ?? (f.key === 'message'),
                group: f.group ?? null,
            }));
            if (!fields.some((f: any) => f.key === 'extraCol')) {
                fields.push({ key: 'extraCol', label: 'extraCol', type: 'string', required: false, group: null });
            }
            await r.fulfill(json({ code: 200, result: { status: 1, fields } }));
        });
        await openSqlCenter(page);
        const save = page.locator('.sql-toolbar-bar .el-button--primary').last();
        await expect(save).toBeDisabled();

        // Gõ thêm vào câu SQL để nút Lưu mở
        const fields = page.locator('.fields-section .vxe-table--main-wrapper');
        const textarea = page.locator('.sql-section .sql-raw-textarea-fallback textarea');
        await textarea.fill(`${SQL_ONE_COLUMN}\n-- extraCol`);
        await expect(save).toBeEnabled({ timeout: 10_000 });

        await save.click();
        await expect.poll(() => saved?.fields?.map((f: any) => f.key), { timeout: 10_000 }).toEqual(['speed', 'message']);
        await expect(fields).toContainText('extraCol', { timeout: 15_000 });

        // Sau khi lưu cột dò xong, bấm nút Chỉnh sửa trường ở dòng mở popup FieldEditDialog để sửa nhãn và lưu ngay
        saved = null;
        const firstEditBtn = page.locator('.field-edit-btn').first();
        await expect(firstEditBtn).toHaveAttribute('aria-label', 'Chỉnh sửa');
        await expect(page.locator('.sql-editor-fullscreen-btn')).toHaveCount(0);
        await expect(firstEditBtn).toBeEnabled();
        await firstEditBtn.click();
        const editDialog = page.locator('.field-edit-dialog');
        await expect(editDialog).toBeVisible();
        await editDialog.locator('.el-form-item', { hasText: 'Tên hiển thị' }).locator('input').fill('Tốc độ xe');
        const groupItem = editDialog.locator('.el-form-item', { hasText: 'Nhóm' });
        await expect(groupItem).toBeVisible();
        const groupInput = groupItem.locator('.el-select input');
        await groupInput.fill('Dữ liệu xuất');
        await groupInput.press('Enter');
        await editDialog.locator('.dialog-footer .el-button--primary').click();
        await expect(editDialog).toBeHidden();

        await expect.poll(() => saved?.extractSql, { timeout: 10_000 }).toBe(`${SQL_ONE_COLUMN}\n-- extraCol`);
        expect(saved.fields.find((f: any) => f.key === 'speed').label).toBe('Tốc độ xe');
        expect(saved.fields.find((f: any) => f.key === 'speed').group).toBe('Dữ liệu xuất');
        expect(saved.fields.find((f: any) => f.key === 'message').required).toBe(true);
        await expect(fields).toContainText('Tốc độ xe');
        await expect(fields).toContainText('Dữ liệu xuất');
    });

    /**
     * Description: Chiều nhận (Ghi ngược): một khối SQL gộp nhiều bảng, tự dò bảng đích từ MERGE/INSERT/UPDATE vào Bảng bị ảnh hưởng (khóa 🔒),
     *              kiểm an toàn hiện cảnh báo và khoá bản ghi dò được; nút Lưu ở tab Chiều nhận hỏi xác nhận vì còn cảnh báo rồi gửi câu ghi + affectedTables.
     * Created date: 26/10/2026
     */
    test('32. Chiều nhận: một khối SQL gộp nhiều bảng, tự dò bảng bị ảnh hưởng, kiểm an toàn và Lưu câu ghi', async ({ page }) => {
        let saved: any = null;
        await page.route('**/api/sharedata/sharedatapacket/validatewritesql*', (r) =>
            r.fulfill(
                json({
                    code: 200,
                    result: {
                        isValid: true,
                        detectedTables: ['dbo.TmsTrafficData'],
                        keyFields: ['speed'],
                        checks: [{ level: 'Warning', message: 'Câu MERGE chưa có OUTPUT $action' }],
                    },
                })
            )
        );
        await page.route('**/api/sharedata/sharedatapacket/updatewritesqls*', async (r) => {
            saved = r.request().postDataJSON();
            await r.fulfill(
                json({
                    code: 200,
                    result: {
                        packetCode: '101_commonData',
                        writeSql: saved.writeSql,
                        affectedTables: saved.affectedTables,
                        detectedTables: saved.affectedTables,
                        keyFields: ['speed'],
                    },
                })
            );
        });
        await openSqlCenter(page);
        await expect(page.locator('.direction-tabs .el-tabs__item', { hasText: 'Nhận về' })).toContainText('chưa khai câu ghi');
        await page.locator('.direction-tabs .el-tabs__item', { hasText: 'Nhận về' }).click();
        await expect(page.locator('.write-sql-panel .wsp-header .el-button', { hasText: 'Mở rộng' })).toHaveCount(0);

        const panel = page.locator('.write-sql-panel');
        await expect(panel).toBeVisible();
        const save = panel.locator('.wsp-save-btn');
        await expect(save).toBeDisabled();

        // Bố cục chiều nhận: chỉ 3 nút (Chèn mẫu, Kiểm an toàn, Lưu), không còn Chạy thử / Ghi chú / ô chọn bảng tay;
        // Bảng bị ảnh hưởng nằm trên khung soạn, khung soạn là trình soạn Monaco như chiều gửi
        await expect(panel.locator('.wsp-header .el-button')).toHaveCount(3);
        await expect(panel.locator('.wsp-remark-row')).toHaveCount(0);
        await expect(panel.locator('.wsp-manual-select')).toHaveCount(0);
        await expect(panel.locator('.wsp-params')).toHaveCount(0);
        await expect(panel.locator('.wsp-editor .sql-monaco-editor')).toHaveCount(1);

        // Tham số hệ thống dùng được đưa vào icon tooltip bên cạnh tiêu đề câu ghi
        const editorHint = panel.locator('.wsp-editor .sql-editor-hint');
        await expect(editorHint).toBeVisible();
        await editorHint.hover();
        await expect(page.locator('.el-popper:visible')).toContainText('@records');
        const affectedY = (await panel.locator('.wsp-affected-section').boundingBox())!.y;
        const editorY = (await panel.locator('.wsp-editor').boundingBox())!.y;
        expect(affectedY).toBeLessThan(editorY);

        // Bấm Chèn mẫu MERGE để điền câu MERGE vào khung soạn của Chiều nhận -> tự dò bảng đích
        await panel.getByRole('button', { name: 'Chèn mẫu MERGE' }).click();
        await expect(panel.locator('.wsp-detected-tag')).toContainText('dbo.TmsTrafficData', { timeout: 10_000 });
        await expect(save).toBeEnabled({ timeout: 10_000 });

        await panel.getByRole('button', { name: 'Kiểm an toàn câu ghi' }).click();
        await expect(panel.locator('.wsp-check-warning')).toContainText('OUTPUT $action');
        await expect(panel.locator('.wsp-key-tag')).toContainText('speed');

        await save.click();
        await page.locator('.el-message-box .el-button--primary').click();
        await expect.poll(() => saved?.packetCode, { timeout: 10_000 }).toBe('101_commonData');
        expect(saved.affectedTables).toEqual(['dbo.TmsTrafficData']);
        expect(saved.writeSql).toContain('MERGE dbo.TmsTrafficData');
    });

    /**
     * Description: Chạy thử câu ghi ở tab Chiều nhận: hộp chạy thử gửi dữ liệu mẫu cùng khối câu ghi đang soạn, hiện tổng số dòng thêm / sửa (máy chủ quay lui sau khi chạy).
     * Created date: 26/10/2026
     */
    // Bỏ nút Chạy thử ở chiều nhận (yêu cầu 27/10); bật lại ca này khi mở lại nút
    test.skip('33. Chiều nhận: chạy thử gửi dữ liệu mẫu và hiện tổng số dòng thêm / sửa', async ({ page }) => {
        let tried: any = null;
        await page.route('**/api/sharedata/sharedatapacket/getwritesqls*', (r) =>
            r.fulfill(
                json({
                    code: 200,
                    result: {
                        packetCode: '101_commonData',
                        writeSql: 'MERGE dbo.TmsTrafficData AS t USING (SELECT 1 AS x) AS s ON 1 = 0 WHEN NOT MATCHED THEN INSERT (Speed) VALUES (1) OUTPUT $action;',
                        affectedTables: ['dbo.TmsTrafficData'],
                        detectedTables: ['dbo.TmsTrafficData'],
                        keyFields: [],
                    },
                })
            )
        );
        await page.route('**/api/sharedata/sharedatapacket/trywritesql*', async (r) => {
            tried = r.request().postDataJSON();
            await r.fulfill(json({ code: 200, result: { isSuccess: true, recordCount: 1, inserted: 1, updated: 0 } }));
        });
        await openSqlCenter(page);
        await page.locator('.direction-tabs .el-tabs__item', { hasText: 'Nhận về' }).click();

        const panel = page.locator('.write-sql-panel');
        await panel.getByRole('button', { name: 'Chạy thử' }).click();
        const dialog = page.locator('.wsp-try-dialog');
        await expect(dialog).toBeVisible();
        await dialog.locator('textarea').fill('[{ "speed": 50 }]');
        await dialog.locator('.dialog-footer .el-button--primary').click();

        await expect.poll(() => tried?.recordsJson, { timeout: 10_000 }).toBe('[{ "speed": 50 }]');
        expect(tried.affectedTables).toEqual(['dbo.TmsTrafficData']);
        await expect(dialog.locator('.wsp-try-table')).toContainText('dbo.TmsTrafficData');
        await expect(dialog.locator('.wsp-try-table')).toContainText('1');
    });

    /**
     * Description: Danh sách gói tin có cột Ghi ngược hiện "{count} bảng" của gói (0 = Chỉ gửi);
     *              tab Nhận về ở đầu vùng phải hiện "Nhận về · ghi vào {count} bảng" và đi theo gói (không phụ thuộc đối tác).
     * Created date: 26/10/2026
     */
    test('34. Chiều nhận: cột số bảng ở danh sách gói và nhãn tab Chiều nhận ({count} bảng)', async ({ page }) => {
        await page.route('**/api/sharedata/sharedatapacket/page*', (r) =>
            r.fulfill(json({ code: 200, result: { records: [{ id: 'PKT_01', code: '101_commonData', name: 'Gói tin dùng chung 101', status: 1, writeSqlCount: 3 }], total: 1 } }))
        );
        await openSqlCenter(page);
        await expect(page.locator('.packet-col .vxe-table--main-wrapper .vxe-header--column', { hasText: 'Gửi / Nhận' })).toBeVisible();
        await expect(page.locator('.packet-col .vxe-table--main-wrapper .vxe-body--row').first()).toContainText('3 bảng');
        await expect(page.locator('.direction-tabs .el-tabs__item', { hasText: 'Nhận về' })).toContainText('Nhận về · ghi vào 3 bảng');
    });

    /**
     * Description: Chuyển đổi qua lại giữa các gói tin khi chưa sửa gì không được hiện thông báo "thay đổi chưa lưu"
     *              (kể cả khi câu SQL có chứa biến tự động dò như @lastTime mà chưa lưu trên CSDL).
     * Created date: 26/10/2026
     */
    test('35. Chuyển đổi gói tin khi chưa sửa gì không hiện thông báo thay đổi chưa lưu', async ({ page }) => {
        const packets = [
            { id: 'PKT_01', code: '101_commonData', name: 'Gói 101 - Dữ liệu giao thông', status: 1 },
            { id: 'PKT_04', code: '104_weatherData', name: 'Gói 104 - Dữ liệu thời tiết', status: 1, writeSqlCount: 1 },
        ];
        await page.route('**/api/sharedata/sharedatapacket/page*', (r) =>
            r.fulfill(json({ code: 200, result: { records: packets, total: 2 } }))
        );
        // Gói 104 có SQL chứa biến @lastTime nhưng DB chưa lưu biến nào (items: [])
        await page.route('**/api/sharedata/sharedatapacket/getextractsql*', (r) => {
            const url = r.request().url();
            if (url.includes('104_weatherData')) {
                return r.fulfill(json({ code: 200, result: { extractSql: 'SELECT speed FROM TmsTraffic WHERE UpdateTime > @lastTime', status: 1, fields: SAVED_FIELDS } }));
            }
            return r.fulfill(json({ code: 200, result: { extractSql: SQL_ONE_COLUMN, status: 1, fields: SAVED_FIELDS } }));
        });
        await page.route('**/api/sharedata/sharedatapacket/getsqlvariables*', (r) =>
            r.fulfill(json({ code: 200, result: { items: [] } }))
        );
        await page.route('**/api/sharedata/sharedatapacket/getwritesqls*', (r) =>
            r.fulfill(json({ code: 200, result: { packetCode: '104_weatherData', writeSql: 'MERGE dbo.TmsWeather WITH (HOLDLOCK) AS t USING (SELECT 1 AS x) AS s ON 1 = 0 OUTPUT $action;', affectedTables: ['dbo.TmsWeather'], detectedTables: ['dbo.TmsWeather'], keyFields: [] } }))
        );

        await openSqlCenter(page);

        // Ban đầu mở Gói 101
        const rows = page.locator('.packet-col .vxe-table--main-wrapper .vxe-body--row');
        await expect(rows).toHaveCount(2);

        // Chuyển sang Gói 104
        await rows.nth(1).click();
        await expect(page.locator('.packet-header-bar .packet-title')).toContainText('Gói 104');

        // Chuyển sang tab Nhận về
        await page.locator('.direction-tabs .el-tabs__item', { hasText: 'Nhận về' }).click();
        await expect(page.locator('.write-sql-panel')).toBeVisible();

        // Bấm chuyển lại sang Gói 101 mà chưa chỉnh sửa bất kỳ nội dung nào
        await rows.nth(0).click();

        // Kỳ vọng: KHÔNG có popup cảnh báo "Gói tin hiện tại còn thay đổi chưa lưu" xuất hiện, chuyển thẳng sang Gói 101
        await expect(page.locator('.el-message-box')).toHaveCount(0);
        await expect(page.locator('.packet-header-bar .packet-title')).toContainText('Gói 101');
    });

    /**
     * Description: Kiểm tra toolbar hành động hiển thị nút "Lưu & phân tích", thông tin đối tác bên trái toolbar,
     *              và tiêu đề cột Khối 2 "Khoá (alias)", "Tên hiển thị" theo chuẩn wireframe.
     * Created date: 27/10/2026
     */
    test('36. Toolbar hiện "Lưu", thông tin đối tác bên trái, cột trường "Khoá (alias)" và "Tên hiển thị"', async ({ page }) => {
        await page.route('**/api/sharedata/sharedatapacket/getpartnersqllist*', (r) =>
            r.fulfill(
                json({
                    code: 200,
                    result: [{ partnerId: 'vdot', partnerCode: 'VDOT', partnerName: 'Cục Đường bộ Việt Nam', isCustom: true }],
                })
            )
        );
        await page.route('**/api/sharedata/sharedatapacket/getpartnerextractsql*', (r) =>
            r.fulfill(
                json({
                    code: 200,
                    result: {
                        partnerId: 'vdot',
                        packetCode: '101_commonData',
                        standardSql: SQL_ONE_COLUMN,
                        customSql: `${SQL_ONE_COLUMN} WHERE td.Speed > 0`,
                        isCustom: true,
                        hasDifferences: true,
                        baseVersion: 3,
                        templateVersion: 5,
                        commonFields: SAVED_FIELDS,
                        fields: SAVED_FIELDS,
                    },
                })
            )
        );
        await openSqlCenter(page);

        // Nút Lưu trên toolbar theo wireframe
        const saveBtn = page.locator('.sql-toolbar-bar .el-button--primary').last();
        await expect(saveBtn).toContainText('Lưu');

        // Bảng danh sách trường: tiêu đề cột là "Khoá (alias)" và "Tên hiển thị"
        const fieldsGrid = page.locator('.fields-section .vxe-table--main-wrapper');
        await expect(fieldsGrid.locator('.vxe-header--column', { hasText: 'Trường dữ liệu' })).toBeVisible();
        await expect(fieldsGrid.locator('.vxe-header--column', { hasText: 'Tên hiển thị' })).toBeVisible();

        // Chuyển sang tab VDOT
        await page.locator('.sql-partner-tabs .el-tabs__item', { hasText: 'VDOT' }).click();

        // Toolbar bên trái hiện thông tin đối tác và chip phiên bản mẫu
        const toolbarLeft = page.locator('.sql-toolbar-left');
        await expect(toolbarLeft.locator('.partner-toolbar-code')).toContainText('VDOT');
        await expect(toolbarLeft.locator('.partner-toolbar-name')).toContainText('Cục Đường bộ Việt Nam');
        await expect(toolbarLeft.locator('.chip-status')).toContainText('Dựa trên mẫu v3 · mẫu hiện tại v5');
    });

    test('37. Kiểm tra bật / tắt collapse ở Khối 2: nút Thu gọn khối và mũi tên collapse nhóm trường', async ({ page }) => {
        const GROUPED_FIELDS = [
            { key: 'amount', type: 'number', dbType: 'int', label: 'lưu lượng', required: true, group: 'report' },
        ];
        await page.route('**/api/sharedata/sharedatapacket/getextractsql*', (r) =>
            r.fulfill(json({ code: 200, result: { extractSql: 'SELECT td.Speed AS amount FROM TmsTrafficData td', status: 1, fields: GROUPED_FIELDS } }))
        );
        await openSqlCenter(page);

        // 1. Kiểm tra nhóm "report (1)" ban đầu mở và hiện dòng con "amount"
        const fieldsGrid = page.locator('.fields-section .vxe-table--main-wrapper');
        await expect(fieldsGrid.locator('.field-group-label', { hasText: 'report' })).toBeVisible();
        await expect(fieldsGrid.locator('.vxe-body--row', { hasText: 'amount' })).toBeVisible();

        // 2. Bấm vào nút/mũi tên thu gọn nhóm report (1)
        const groupRow = fieldsGrid.locator('.field-group-row').first();
        const treeBtn = groupRow.locator('.vxe-tree--btn-wrapper').first();
        const gridEl = page.locator('.fields-section .vxe-grid');
        const heightOpen = (await gridEl.boundingBox())!.height;
        // Nhóm đang mở: lưới giữ cao cố định 380px dù ít trường
        expect(heightOpen).toBeGreaterThanOrEqual(370);
        await treeBtn.click();

        // Dòng con "amount" phải được ẩn đi (tắt collapse nhóm) và lưới thấp lại theo số dòng còn hiện
        await expect(fieldsGrid.locator('.vxe-body--row', { hasText: 'amount' })).toBeHidden({ timeout: 3000 });
        await expect.poll(async () => (await gridEl.boundingBox())!.height, { timeout: 3000 }).toBeLessThan(heightOpen);

        // 3. Bấm mở lại nhóm report (1)
        await treeBtn.click();
        await expect(fieldsGrid.locator('.vxe-body--row', { hasText: 'amount' })).toBeVisible({ timeout: 3000 });

        // 4. Kiểm tra nút "Thu gọn" của cả Khối 2: Trường dữ liệu
        const fieldsSection = page.locator('.fields-section');
        const collapseBlockBtn = fieldsSection.locator('.fields-bar-actions .el-button').last();
        await expect(collapseBlockBtn).toHaveAttribute('aria-label', 'Thu gọn');
        await collapseBlockBtn.click();

        // Lưới trường bị ẩn và nút đổi thành "Mở rộng"
        await expect(fieldsSection.locator('.saved-fields-grid-wrapper')).toBeHidden({ timeout: 3000 });
        await expect(collapseBlockBtn).toHaveAttribute('aria-label', 'Mở rộng');

        // Bấm "Mở rộng" để mở lại Khối 2
        await collapseBlockBtn.click();
        await expect(fieldsSection.locator('.saved-fields-grid-wrapper')).toBeVisible({ timeout: 3000 });
        await expect(collapseBlockBtn).toHaveAttribute('aria-label', 'Thu gọn');
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

    /**
     * Description: Dòng "Đối tác dùng" luôn nằm hàng dưới tên gói và tự ngắt dòng khi tràn bề rộng: không đè, không cắt tên / mã gói,
     *              mọi thẻ đối tác nằm trọn trong khung (không cần cuộn ngang). Màn hẹp thì thẻ xuống nhiều hàng.
     * Created date: 27/10/2026
     */
    const partnerUsageCases = [
        { name: '38a. Đối tác dùng: màn rộng, mọi thẻ hiện đủ trong khung', width: 1600, expectWrap: false },
        { name: '38b. Đối tác dùng: màn hẹp, tự ngắt dòng, không thẻ nào bị khuất', width: 1100, expectWrap: true },
    ];

    for (const c of partnerUsageCases) {
        test(c.name, async ({ page }) => {
            const partners = [
                { partnerId: 'P1', partnerCode: 'TTCSDL', partnerName: 'Trung tâm cơ sở dữ liệu', isCustom: false },
                { partnerId: 'P2', partnerCode: 'ddlt', partnerName: 'Đồng Đăng - Trà Lĩnh', isCustom: true },
                { partnerId: 'P3', partnerCode: 'DDTL-2', partnerName: 'Đồng Đăng - Trà Lĩnh 2', isCustom: false },
                { partnerId: 'P4', partnerCode: 'SGTVT-HCM', partnerName: 'Sở GTVT TP. Hồ Chí Minh', isCustom: false },
                { partnerId: 'P5', partnerCode: 'BOT-PHAPVAN', partnerName: 'BOT Pháp Vân – Cầu Giẽ', isCustom: false },
            ];
            await page.route('**/api/sharedata/sharedatapacket/getpartnersqllist*', (r) => r.fulfill(json({ code: 200, result: partners })));
            await openSqlCenter(page);
            await page.setViewportSize({ width: c.width, height: 900 });
            await page.waitForTimeout(800);

            const bar = page.locator('.packet-header-bar');
            const tags = page.locator('.packet-partner-usage .el-tag');
            await expect(tags).toHaveCount(partners.length);

            const barBox = (await bar.boundingBox())!;
            // Nằm dưới dòng tên gói
            const titleBox = (await page.locator('.packet-header-title').boundingBox())!;
            const usageBox = (await page.locator('.packet-partner-usage').boundingBox())!;
            expect(usageBox.y).toBeGreaterThanOrEqual(titleBox.y + titleBox.height - 1);
            const tops = new Set<number>();
            for (let i = 0; i < partners.length; i++) {
                const box = (await tags.nth(i).boundingBox())!;
                // Thẻ nằm trọn trong khung tiêu đề, không bị cắt bên phải
                expect(box.x).toBeGreaterThanOrEqual(barBox.x - 1);
                expect(box.x + box.width).toBeLessThanOrEqual(barBox.x + barBox.width + 1);
                tops.add(Math.round(box.y));
            }
            // Khung không tràn ngang (không phải cuộn mới thấy) và mã gói không bị cắt
            const noOverflow = await page.evaluate(() => {
                const usage = document.querySelector('.packet-partner-usage') as HTMLElement;
                const code = document.querySelector('.packet-header-title .el-tag') as HTMLElement;
                return usage.scrollWidth <= usage.clientWidth + 1 && code.scrollWidth <= code.clientWidth + 1;
            });
            expect(noOverflow).toBe(true);
            if (c.expectWrap) expect(tops.size).toBeGreaterThan(1);
        });
    }

    /**
     * Description: Tab Mẫu chung / đối tác có hiệu ứng trượt thanh gạch dưới khi bấm đổi tab;
     *              chỉ tạm tắt hiệu ứng ngay lúc quay lại tab Chiều gửi (tránh trượt từ Mẫu chung sang tab đang chọn).
     * Created date: 27/10/2026
     */
    test('39. Tab đối tác có hiệu ứng trượt; quay lại Chiều gửi thì tạm tắt rồi bật lại', async ({ page }) => {
        await page.route('**/api/sharedata/sharedatapacket/getpartnersqllist*', (r) =>
            r.fulfill(json({ code: 200, result: [{ partnerId: 'P1', partnerCode: 'DT-DAT', partnerName: 'Đạt', isCustom: true }] }))
        );
        await openSqlCenter(page);
        const tabs = page.locator('.sql-partner-tabs').first();
        const barDuration = () => tabs.locator('.el-tabs__active-bar').evaluate((el) => getComputedStyle(el).transitionDuration);
        await expect.poll(barDuration).not.toBe('0s');

        await page.locator('.direction-tabs .el-tabs__item').nth(1).click();
        await page.locator('.direction-tabs .el-tabs__item').nth(0).click();
        await expect(tabs).toHaveClass(/no-bar-anim/);
        await expect.poll(barDuration, { timeout: 3000 }).not.toBe('0s');
        await expect(tabs).not.toHaveClass(/no-bar-anim/);
    });


    for (const c of [
        { name: '40. Khối 2 đã thu gọn từ trước nhưng gói chưa có trường nào: vẫn mở bảng, hiện tiêu đề cột và lời nhắc trống', fields: [] as typeof SAVED_FIELDS, expectOpen: true },
        { name: '40b. Khối 2 đã thu gọn từ trước và gói có trường: giữ thu gọn như người dùng chọn', fields: SAVED_FIELDS, expectOpen: false },
    ]) {
        test(c.name, async ({ page }) => {
            // Trạng thái "đã thu gọn Khối 2" lưu từ lần trước (Local: <tiền tố>:sharedataDataSource:fieldsCollapsed = true)
            await page.addInitScript(() => {
                const getItem = Storage.prototype.getItem;
                Storage.prototype.getItem = function (key: string) {
                    return key.endsWith('sharedataDataSource:fieldsCollapsed') ? 'true' : getItem.call(this, key);
                };
            });
            await page.route('**/api/sharedata/sharedatapacket/getextractsql*', (r) =>
                r.fulfill(json({ code: 200, result: { extractSql: SQL_ONE_COLUMN, status: 1, fields: c.fields } }))
            );
            await openSqlCenter(page);

            const fieldsSection = page.locator('.fields-section');
            const wrapper = fieldsSection.locator('.saved-fields-grid-wrapper');
            const collapseBlockBtn = fieldsSection.locator('.fields-bar-actions .el-button').last();
            if (c.expectOpen) {
                await expect(wrapper).toBeVisible({ timeout: 5000 });
                await expect(fieldsSection.locator('.vxe-header--column', { hasText: 'Trường dữ liệu' })).toBeVisible();
                await expect(fieldsSection.locator('.vxe-header--column', { hasText: 'Kiểu' })).toBeVisible();
                await expect(fieldsSection.locator('.el-empty:visible', { hasText: 'Gói tin này chưa có trường nào' })).toBeVisible();
                await expect(collapseBlockBtn).toHaveAttribute('aria-label', 'Thu gọn');
            } else {
                await expect(wrapper).toBeHidden({ timeout: 5000 });
                await expect(collapseBlockBtn).toHaveAttribute('aria-label', 'Mở rộng');
            }
        });
    }


    test('40c. Khối 2 đã thu gọn từ trước, gói chưa có trường: bấm Lưu & phân tích sinh ra trường thì bảng vẫn mở, không tự đóng', async ({ page }) => {
        await page.addInitScript(() => {
            const getItem = Storage.prototype.getItem;
            Storage.prototype.getItem = function (key: string) {
                if (key.endsWith('sharedataDataSource:fieldsCollapsed') && !sessionStorage.getItem('fc-seeded')) {
                    sessionStorage.setItem('fc-seeded', '1');
                    return 'true';
                }
                return getItem.call(this, key);
            };
        });
        await page.route('**/api/sharedata/sharedatapacket/getextractsql*', (r) =>
            r.fulfill(json({ code: 200, result: { extractSql: SQL_ONE_COLUMN, status: 1, fields: [] } }))
        );
        await page.route('**/api/sharedata/sharedatapacket/describecolumns*', (r) =>
            r.fulfill(json({ code: 200, result: { isValid: true, fields: SAVED_FIELDS, added: SAVED_FIELDS.map((x) => x.key), lost: [], typeChanged: [], affectedMappings: [] } }))
        );
        await page.route('**/api/sharedata/sharedatapacket/updateextractsql*', (r) =>
            r.fulfill(json({ code: 200, result: { extractSql: SQL_ONE_COLUMN, status: 1, fields: SAVED_FIELDS } }))
        );
        await openSqlCenter(page);

        const fieldsSection = page.locator('.fields-section');
        const wrapper = fieldsSection.locator('.saved-fields-grid-wrapper');
        await expect(wrapper).toBeVisible({ timeout: 5000 });

        // Gói có SQL nhưng chưa có trường: nút Lưu & phân tích mở sẵn
        const save = page.locator('.sql-toolbar-bar .el-button--primary').last();
        await expect(save).toBeEnabled({ timeout: 10_000 });
        await save.click();

        await expect(fieldsSection.locator('.fields-badge-count')).toHaveText(String(SAVED_FIELDS.length), { timeout: 10_000 });
        await expect(wrapper).toBeVisible();
        await expect(fieldsSection.locator('.vxe-body--row', { hasText: 'speed' })).toBeVisible();
        await expect(fieldsSection.locator('.fields-bar-actions .el-button').last()).toHaveAttribute('aria-label', 'Thu gọn');
    });


    test('41. Bấm gói: câu SQL, bảng tham số và khối câu ghi mẫu chung nạp chung một lần GetSqlDetail, không gọi ba API lẻ', async ({ page }) => {
        const calls: string[] = [];
        page.on('request', (req) => {
            const m = req.url().match(/\/api\/sharedata\/sharedatapacket\/(getsqldetail|getextractsql|getsqlvariables|getwritesqls)\b/);
            if (m) calls.push(m[1]);
        });
        await page.route('**/api/sharedata/sharedatapacket/getsqldetail*', (r) =>
            r.fulfill(
                json({
                    code: 200,
                    result: {
                        extract: { extractSql: SQL_ONE_COLUMN + ' WHERE td.CreateTime >= @yearStart', status: 1, fields: SAVED_FIELDS },
                        variables: { items: [{ code: 'yearStart', valueKind: 'Builtin', value: 'startofyear', dataType: 'DateTime', scope: 'common' }] },
                        write: { writeSql: 'MERGE dbo.TmsZone AS t USING (SELECT 1 AS id) s ON t.ID = s.id WHEN MATCHED THEN UPDATE SET t.Name = t.Name;', affectedTables: ['dbo.TmsZone'], detectedTables: ['dbo.TmsZone'], keyFields: ['id'] },
                    },
                })
            )
        );
        await openSqlCenter(page);

        await expect(page.locator('.fields-section .vxe-body--row', { hasText: 'speed' })).toBeVisible({ timeout: 15_000 });
        await expect(page.locator('.sql-variable-section').getByText('yearStart').first()).toBeVisible({ timeout: 15_000 });
        expect(calls.filter((c) => c === 'getsqldetail')).toHaveLength(1);
        expect(calls.filter((c) => c !== 'getsqldetail')).toEqual([]);
    });


    test('42. SQL riêng của đối tác chưa phân tích (chưa có trường): bảng trường rỗng, không so với mẫu, không hiện ký hiệu và chú giải', async ({ page }) => {
        await page.route('**/api/sharedata/sharedatapacket/getpartnersqllist*', (r) =>
            r.fulfill(json({ code: 200, result: [{ partnerId: 'P1', partnerCode: 'DT-DAT', partnerName: 'Đạt', isCustom: true, updateTime: '2026-10-22T10:00:00' }] }))
        );
        await page.route('**/api/sharedata/sharedatapacket/getpartnerextractsql*', (r) =>
            r.fulfill(
                json({
                    code: 200,
                    result: {
                        partnerId: 'P1',
                        packetCode: '101_commonData',
                        standardSql: SQL_ONE_COLUMN,
                        customSql: `${SQL_ONE_COLUMN} WHERE 1 = 1`,
                        isCustom: true,
                        hasDifferences: true,
                        commonFields: SAVED_FIELDS,
                        fields: [],
                    },
                })
            )
        );
        await openSqlCenter(page);
        await page.locator('.sql-partner-tabs .el-tabs__item', { hasText: 'DT-DAT' }).click();

        const fieldsSection = page.locator('.fields-section');
        await expect(fieldsSection.locator('.fields-badge-count')).toHaveText('0', { timeout: 10_000 });
        await expect(fieldsSection.locator('.vxe-header--column', { hasText: 'Trường dữ liệu' })).toBeVisible();
        await expect(fieldsSection.locator('.el-empty:visible', { hasText: 'Gói tin này chưa có trường nào' })).toBeVisible();
        await expect(fieldsSection.locator('.vxe-body--row')).toHaveCount(0);
        await expect(fieldsSection.locator('.cmp-st')).toHaveCount(0);
        await expect(fieldsSection.locator('.fields-compare-summary')).toHaveCount(0);
    });


    test('43. Lưu SQL nối đuôi có khóa __lastKey định dạng dễ sai thứ tự: lưu thành công nhưng hiện cảnh báo theo khóa máy chủ trả về', async ({ page }) => {
        await page.route('**/api/sharedata/sharedatapacket/describecolumns*', (r) =>
            r.fulfill(json({ code: 200, result: { isValid: true, fields: SAVED_FIELDS, added: [], lost: [], typeChanged: [], affectedMappings: [] } }))
        );
        await page.route('**/api/sharedata/sharedatapacket/updateextractsql*', (r) =>
            r.fulfill(
                json({
                    code: 200,
                    result: {
                        extractSql: SQL_ONE_COLUMN,
                        status: 1,
                        fields: SAVED_FIELDS,
                        warnings: ['lz.validation.sharedata.sqlLastKeyDateFormatUnsortable', 'lz.validation.sharedata.sqlLastKeyValueNotFixedWidth'],
                    },
                })
            )
        );
        await openSqlCenter(page);
        const save = page.locator('.sql-toolbar-bar .el-button--primary').last();
        await page.locator('.sql-section .sql-raw-textarea-fallback textarea').fill(`${SQL_ONE_COLUMN}\n-- sửa`);
        await expect(save).toBeEnabled({ timeout: 10_000 });
        await save.click();

        const notice = page.locator('.el-notification').filter({ hasText: 'Khóa nối đuôi [__lastKey]' });
        await expect(notice).toBeVisible({ timeout: 10_000 });
        await expect(notice).toContainText('năm-tháng-ngày');
        await expect(notice).toContainText('đệm 0');
    });


    for (const c of [
        {
            name: '44. Thiết lập khóa nối đuôi: chọn cột thời gian + mã kiểu số → chèn đủ cột __lastKey (đệm 0), điều kiện WHERE > @lastKey và ORDER BY __lastKey',
            sql: SQL_ONE_COLUMN,
            numeric: true,
            op: '>',
            expectKey: "CONVERT(varchar(23), td.UpdateTime, 121) + '_' + RIGHT('0000000000' + CAST(td.ID AS varchar(10)), 10) AS __lastKey",
            expectWhere: "WHERE CONVERT(varchar(23), td.UpdateTime, 121) + '_' + RIGHT('0000000000' + CAST(td.ID AS varchar(10)), 10) > @lastKey",
        },
        {
            name: '44b. Thiết lập khóa nối đuôi trên SQL đã có WHERE và ORDER BY khác: thêm AND vào WHERE, thay ORDER BY bằng __lastKey; mã kiểu chuỗi, toán tử >=',
            sql: 'SELECT TOP 100 td.Speed AS speed FROM TmsTrafficData td WHERE td.Speed > 0 ORDER BY td.Speed DESC',
            numeric: false,
            op: '>=',
            expectKey: "CONVERT(varchar(23), td.UpdateTime, 121) + '_' + td.ID AS __lastKey",
            expectWhere: "WHERE td.Speed > 0 AND CONVERT(varchar(23), td.UpdateTime, 121) + '_' + td.ID >= @lastKey",
        },
    ]) {
        test(c.name, async ({ page }) => {
            await page.route('**/api/sharedata/sharedatapacket/getextractsql*', (r) =>
                r.fulfill(json({ code: 200, result: { extractSql: c.sql, status: 1, fields: SAVED_FIELDS } }))
            );
            await page.route('**/api/sharedata/sharedatapacket/getsqlvariables*', (r) =>
                r.fulfill(json({ code: 200, result: { items: [{ code: 'lastKey', valueKind: 'Builtin', value: 'lastKey', dataType: 'String', scope: 'common' }] } }))
            );
            await openSqlCenter(page);

            await page.locator('.sql-variable-section .svp-lastkey-setup').first().click();
            const dialog = page.locator('.lks-dialog');
            await expect(dialog).toBeVisible();
            await dialog.locator('.lks-time input').fill('td.UpdateTime');
            await dialog.locator('.lks-id input').fill('td.ID');
            if (c.numeric) await dialog.locator('.lks-numeric').click();
            if (c.op === '>=') await dialog.locator('.lks-op .el-radio-button', { hasText: '>=' }).click();
            await dialog.locator('.lks-apply').click();
            await expect(dialog).toBeHidden();

            const sql = await page.locator('.sql-section .sql-raw-textarea-fallback textarea').inputValue();
            expect(sql).toContain(c.expectKey);
            expect(sql).toContain(c.expectWhere);
            expect(sql.trim().endsWith('ORDER BY __lastKey')).toBeTruthy();
            expect(sql).not.toContain('DESC');
            expect(sql).toContain('td.Speed AS speed');
        });
    }

    /**
     * Description: Chưa sửa gì mà đổi sang gói tin khác ở tab Nhận về thì không được hiện hộp "Gói tin hiện tại còn thay đổi chưa lưu",
     *              kể cả khi khối câu ghi nạp chậm hoặc nạp lỗi (trước đây mốc so sánh rỗng làm khối câu ghi bị coi là đã sửa).
     * Created date: 28/10/2026
     */
    for (const c of [
        { name: '56. Đổi gói ở tab Nhận về khi chưa sửa gì: khối câu ghi nạp chậm không báo chưa lưu', writeStatus: 200 },
        { name: '57. Đổi gói ở tab Nhận về khi chưa sửa gì: khối câu ghi nạp lỗi không báo chưa lưu', writeStatus: 500 },
    ]) {
        test(c.name, async ({ page }) => {
            await page.route('**/api/sharedata/sharedatapacket/page*', (r) =>
                r.fulfill(json({ code: 200, result: { records: [
                    { id: 'PKT_01', code: '101_commonData', name: 'Gói 101', status: 1 },
                    { id: 'PKT_02', code: '102_cctvData', name: 'Gói 102', status: 1 },
                ], total: 2 } }))
            );
            await page.route('**/api/sharedata/sharedatapacket/getwritesqls*', async (r) => {
                await new Promise((resolve) => setTimeout(resolve, 600));
                if (c.writeStatus !== 200) return r.fulfill({ status: c.writeStatus, contentType: 'application/json', body: JSON.stringify({ code: c.writeStatus, message: 'error' }) });
                return r.fulfill(json({ code: 200, result: { packetCode: '101_commonData', writeSql: '', affectedTables: [], detectedTables: [], keyFields: [] } }));
            });
            await openSqlCenter(page);
            await page.locator('.direction-tabs .el-tabs__item', { hasText: 'Nhận về' }).click();
            await page.waitForTimeout(1500);

            await page.locator('.packet-col .vxe-body--row').nth(1).click();

            await expect(page.locator('.el-message-box', { hasText: 'chưa lưu' })).toHaveCount(0);
            await expect(page.locator('.packet-col .vxe-body--row.row--current')).toContainText('102_cctvData');
        });
    }

    /**
     * Description: Chèn mẫu MERGE với đầy đủ các kiểu dữ liệu của trường gói tin (int, long, float, double, decimal, dateTime, bool, guid, string, number):
     *              sinh đúng kiểu SQL tương ứng trong OPENJSON(@records) WITH (...)
     * Created date: 28/10/2026
     */
    test('58. Chèn mẫu MERGE với đầy đủ các kiểu dữ liệu của trường gói tin: sinh đúng kiểu SQL trong OPENJSON(@records) WITH', async ({ page }) => {
        const typesToTest = [
            { key: 'id', type: 'string' },
            { key: 'colInt', type: 'int' },
            { key: 'colLong', type: 'long' },
            { key: 'colFloat', type: 'float' },
            { key: 'colDouble', type: 'double' },
            { key: 'colDecimal', type: 'decimal' },
            { key: 'colDateTime', type: 'dateTime' },
            { key: 'colBool', type: 'bool' },
            { key: 'colGuid', type: 'guid' },
            { key: 'colString', type: 'string' },
            { key: 'colNumber', type: 'number' },
        ];
        await page.route('**/api/sharedata/sharedatapacket/page*', (r) =>
            r.fulfill(json({ code: 200, result: { records: [{ id: 'PKT_01', code: '101_commonData', name: 'Gói 101', status: 1 }], total: 1 } }))
        );
        await page.route('**/api/sharedata/sharedatapacket/getsqldetail*', (r) =>
            r.fulfill(json({ code: 200, result: {
                extract: {
                    packetCode: '101_commonData',
                    extractSql: 'SELECT ID AS id FROM TmsZone',
                    fields: typesToTest.map((f) => ({ key: f.key, type: f.type, label: f.key, required: false })),
                },
                variables: { items: [] },
                write: { packetCode: '101_commonData', writeSql: '', affectedTables: [], detectedTables: [], keyFields: [] },
            } }))
        );
        await openSqlCenter(page);
        await page.locator('.direction-tabs .el-tabs__item', { hasText: 'Nhận về' }).click();

        const panel = page.locator('.write-sql-panel');
        await expect(panel).toBeVisible();

        // Bấm Chèn mẫu MERGE
        await panel.getByRole('button', { name: 'Chèn mẫu MERGE' }).click();

        // Kiểm tra từng kiểu dữ liệu đã được sinh chuẩn xác trong câu OPENJSON WITH trên trình soạn thảo
        const editor = panel.locator('.wsp-editor .sql-monaco-editor');
        await expect(editor).toBeVisible();
        await expect(editor).toContainText('colInt int', { timeout: 10_000 });
        await expect(editor).toContainText('colLong bigint');
        await expect(editor).toContainText('colFloat float');
        await expect(editor).toContainText('colDouble float');
        await expect(editor).toContainText('colDecimal decimal(18, 4)');
        await expect(editor).toContainText('colDateTime datetime2');
        await expect(editor).toContainText('colBool bit');
        await expect(editor).toContainText('colGuid uniqueidentifier');
        await expect(editor).toContainText('colString nvarchar(max)');
        await expect(editor).toContainText('colNumber decimal(18, 4)');
    });

});
