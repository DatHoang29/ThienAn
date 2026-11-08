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
            items: [{ code: 'since', dataType: 'String', valueKind: 'Builtin', value: 'lastKey' }],
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
     *              đổi cả hai thì bộ biến gửi kèm câu SQL trong một lần lưu (31/10: trước đây lưu bảng biến trước, câu bị từ chối vẫn để lại bộ biến mới).
     * Created date: 25/10/2026
     */
    const singleSaveCases = [
        { name: '30b. Biến Giá trị nhập (Const): nhập giá trị, nút Lưu mở, chỉ gọi API lưu biến, gửi valueKind Const', editSql: false, expected: ['variables'] },
        { name: '31. Sửa SQL và thêm biến: bộ biến gửi kèm câu SQL trong một lần lưu', editSql: true, expected: ['sql'] },
    ];

    for (const c of singleSaveCases) {
        test(c.name, async ({ page }) => {
            const calls: string[] = [];
            let savedVars: any[] = [];
            const initialItems = c.editSql ? [] : [{ code: 'pageSize', dataType: 'Int', valueKind: 'Const', value: '100' }];
            await page.route('**/api/sharedata/sharedatapacket/getsqlvariables*', (r) => r.fulfill(json({ code: 200, result: { items: initialItems } })));
            await page.route('**/api/sharedata/sharedatapacket/updatesqlvariables*', async (r) => {
                calls.push('variables');
                savedVars = r.request().postDataJSON().items;
                await r.fulfill(json({ code: 200, result: { items: savedVars } }));
            });
            await page.route('**/api/sharedata/sharedatapacket/updateextractsql*', async (r) => {
                calls.push('sql');
                savedVars = r.request().postDataJSON().variables ?? savedVars;
                await r.fulfill(json({ code: 200, type: 'success', result: { status: 1 } }));
            });
            await openSqlCenter(page);
            const save = page.locator('.sql-toolbar-bar .el-button--primary').last();
            await expect(save).toBeDisabled();
            // Khối 3 không còn nút Thêm mới: tham số được trích từ @tênBiến trong câu SQL khi bấm Lưu & phân tích
            await expect(page.locator('.sql-variable-panel .svp-header .el-button:not(.svp-collapse-btn)')).toHaveCount(0);

            if (c.editSql) {
                await page.locator('.sql-monaco-editor .view-lines').first().click();
                await page.keyboard.press('End');
                await page.keyboard.type(' WHERE 1 = 1 OR @lastKey IS NULL', { delay: 50 });
                await page.keyboard.press('Escape');
            } else {
                await page.locator('.sql-variable-panel .el-table__body .svp-const-input input').first().fill('500');
                await page.keyboard.press('Tab');
            }
            await expect(save).toBeEnabled({ timeout: 10_000 });
            await save.click();
            await expect.poll(() => calls, { timeout: 10_000 }).toEqual(c.expected);
            if (c.editSql) {
                expect(savedVars[0]).toMatchObject({ code: 'lastKey', dataType: 'String', valueKind: 'Builtin', value: 'lastKey' });
            } else {
                expect(savedVars[0]).toMatchObject({ code: 'pageSize', dataType: 'Int', valueKind: 'Const', value: '500' });
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
        // Lần gửi đầu không kèm force: máy chủ cần bỏ mốc đang chạy thì trả về để hỏi trước (ca 11c)
        expect(saved.force).toBeFalsy();
        await expect(page.locator('.sql-partner-tabs .el-tabs__item.is-active')).toContainText('DT-DAT');
        await expect(page.locator('.sql-partner-tabs .el-tabs__item').last()).toContainText('DT-DAT');
    });

    /**
     * Author: DatHQ
     * Description: Tạo cấu hình riêng chép từ câu của đối tác khác có công thức cột mốc khác câu đối tác đang chạy: máy chủ báo
     *              mốc đang chạy sẽ bị bỏ thì hỏi xác nhận kèm tên đối tác. Hủy thì không ghi gì (không lưu bộ biến riêng, không gửi force,
     *              không mở tab); đồng ý thì gửi lại với force, bộ biến của đối tác nguồn đi kèm câu SQL trong cùng một lần lưu.
     * Created date: 06/11/2026
     */
    test('11c. Sao chép cấu hình riêng khác công thức cột mốc: hỏi xác nhận bỏ mốc, hủy thì không ghi gì, đồng ý thì gửi lại với force', async ({ page }) => {
        const VDOT_SQL = `${SQL_ONE_COLUMN} WHERE td.Speed > 0`;
        const VDOT_VARS = [
            { code: 'lastKey', dataType: 'String', valueKind: 'Builtin', value: 'lastKey', defaultValue: '2026-11-01 00:00:00.000_' },
            { code: 'pageSize', dataType: 'Int', valueKind: 'Const', value: '50' },
        ];
        let created = false;
        const partnerIdOf = (url: string) => {
            const query = new URL(url).searchParams;
            return query.get('partnerId') ?? query.get('PartnerId');
        };
        await page.route('**/api/sharedata/sharedatapacket/getpartnersqllist*', (r) =>
            r.fulfill(
                json({
                    code: 200,
                    result: [
                        { partnerId: 'P1', partnerCode: 'DT-DAT', partnerName: 'Đạt', isCustom: created },
                        { partnerId: 'P2', partnerCode: 'VDOT', partnerName: 'VDOT', isCustom: true },
                    ],
                })
            )
        );
        await page.route('**/api/sharedata/sharedatapacket/getpartnerextractsql*', (r) => {
            const partnerId = partnerIdOf(r.request().url());
            const customSql = partnerId === 'P2' || created ? VDOT_SQL : null;
            return r.fulfill(
                json({
                    code: 200,
                    result: { partnerId, packetCode: '101_commonData', standardSql: SQL_ONE_COLUMN, customSql, isCustom: Boolean(customSql), hasDifferences: Boolean(customSql), fields: SAVED_FIELDS },
                })
            );
        });
        await page.route('**/api/sharedata/sharedatapacket/getsqlvariables*', (r) =>
            r.fulfill(json({ code: 200, result: { items: partnerIdOf(r.request().url()) === 'P2' ? VDOT_VARS : [] } }))
        );
        const variableWrites: any[] = [];
        await page.route('**/api/sharedata/sharedatapacket/updatesqlvariables*', async (r) => {
            variableWrites.push(r.request().postDataJSON());
            await r.fulfill(json({ code: 200, result: { items: [] } }));
        });
        const saveBodies: any[] = [];
        await page.route('**/api/sharedata/sharedatapacket/updatepartnerextractsql*', (r) => {
            const body = r.request().postDataJSON();
            saveBodies.push(body);
            if (body.force) created = true;
            return r.fulfill(
                json({
                    code: 200,
                    result: body.force
                        ? { partnerId: 'P1', isCustom: true, customSql: body.extractSql, standardSql: SQL_ONE_COLUMN, fields: SAVED_FIELDS }
                        : { partnerId: 'P1', isCustom: false, requiresConfirm: true, lost: [], affectedMappings: [], cursorResetPartners: [{ partnerId: 'P1', partnerCode: 'DT-DAT', partnerName: 'Đạt' }] },
                })
            );
        });
        await openSqlCenter(page);
        const dialog = page.locator('.partner-setup-dialog');
        const confirmBox = page.locator('.el-message-box', { has: page.locator('.cursor-reset-confirm') });
        const createFromVdot = async () => {
            await page.locator('.add-partner-tab-btn').click();
            await expect(dialog).toBeVisible();
            await dialog.locator('.el-radio', { hasText: 'Cấu hình riêng của đối tác' }).click();
            await expect(dialog.locator('.setup-other-select')).toContainText('VDOT');
            await dialog.locator('.dialog-footer .el-button--primary').click();
            await expect(dialog).toBeHidden();
        };

        // Hủy: không gửi lại với force, không lưu bộ biến riêng, không mở tab đối tác
        await createFromVdot();
        await expect(confirmBox).toContainText('Đạt', { timeout: 10_000 });
        await confirmBox.locator('.el-message-box__btns .el-button').first().click();
        await expect(confirmBox).toBeHidden();
        expect(saveBodies).toHaveLength(1);
        expect(saveBodies[0].force).toBeFalsy();
        expect(variableWrites).toHaveLength(0);
        await expect(page.locator('.sql-partner-tabs .el-tabs__item', { hasText: 'DT-DAT' })).toHaveCount(0);

        // Đồng ý: gửi lại với force, bộ biến của VDOT đi kèm câu SQL trong cùng lần lưu
        await createFromVdot();
        await expect(confirmBox).toContainText('Đạt', { timeout: 10_000 });
        await confirmBox.locator('.el-message-box__btns .el-button--primary').click();
        await expect.poll(() => saveBodies.length, { timeout: 10_000 }).toBe(3);
        expect(saveBodies[2]).toMatchObject({ partnerId: 'P1', extractSql: VDOT_SQL, force: true });
        expect(saveBodies[2].variables).toEqual([
            expect.objectContaining({ code: 'lastKey', valueKind: 'Builtin', defaultValue: '2026-11-01 00:00:00.000_' }),
            expect.objectContaining({ code: 'pageSize', valueKind: 'Const', value: '50' }),
        ]);
        expect(variableWrites).toHaveLength(0);
        await expect(page.locator('.sql-partner-tabs .el-tabs__item.is-active')).toContainText('DT-DAT');
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

    /**
     * Author: DatHQ
     * Description: Dùng lại mẫu chung khi mẫu chung khác công thức cột mốc: máy chủ trả requiresConfirm, màn hình hỏi kèm tên đối tác bị xóa mốc;
     *              hủy thì không gửi lại, đồng ý thì gửi lại với force.
     * Created date: 05/11/2026
     */
    test('13b. Dùng lại mẫu chung khác công thức cột mốc: hỏi xác nhận kèm tên đối tác, hủy thì thôi, đồng ý thì gửi lại với force', async ({ page }) => {
        const resetBodies: any[] = [];
        await page.route('**/api/sharedata/sharedatapacket/getpartnersqllist*', (r) =>
            r.fulfill(json({ code: 200, result: [{ partnerId: 'P1', partnerCode: 'DT-A', partnerName: 'Đối tác A', isCustom: !resetBodies.some((b) => b.force) }] }))
        );
        await page.route('**/api/sharedata/sharedatapacket/getpartnerextractsql*', (r) =>
            r.fulfill(json({ code: 200, result: { partnerId: 'P1', packetCode: '101_commonData', standardSql: SQL_ONE_COLUMN, customSql: `${SQL_ONE_COLUMN} WHERE 1 = 1`, isCustom: true, hasDifferences: true, commonFields: SAVED_FIELDS, fields: SAVED_FIELDS } }))
        );
        await page.route('**/api/sharedata/sharedatapacket/resetpartnerextractsql*', (r) => {
            const body = r.request().postDataJSON();
            resetBodies.push(body);
            return r.fulfill(
                json({
                    code: 200,
                    result: body.force
                        ? { partnerId: 'P1', isCustom: false }
                        : { partnerId: 'P1', isCustom: true, requiresConfirm: true, lost: [], affectedMappings: [], cursorResetPartners: [{ partnerId: 'P1', partnerCode: 'DT-A', partnerName: 'Đối tác A' }] },
                })
            );
        });
        await openSqlCenter(page);
        await page.locator('.sql-partner-tabs .el-tabs__item', { hasText: 'DT-A' }).click();
        const resetButton = page.locator('.sql-toolbar-bar .el-button', { hasText: 'Dùng lại mẫu chung' });
        const firstBox = page.locator('.el-message-box', { hasText: 'Dùng lại' }).or(page.locator('.lost-keys-confirm-box')).first();
        const cursorBox = page.locator('.el-message-box', { has: page.locator('.cursor-reset-confirm') });

        // Hủy ở hộp xác nhận xóa mốc: không gửi lại, vẫn ở tab đối tác
        await resetButton.click();
        await firstBox.locator('.el-button--primary').click();
        await expect(cursorBox).toContainText('Đối tác A', { timeout: 10_000 });
        expect(resetBodies).toHaveLength(1);
        expect(resetBodies[0].force).toBeFalsy();
        await cursorBox.locator('.el-message-box__btns .el-button').first().click();
        await expect(cursorBox).toBeHidden();
        expect(resetBodies).toHaveLength(1);
        await expect(page.locator('.sql-partner-tabs .el-tabs__item.is-active')).toContainText('DT-A');

        // Đồng ý: gửi lại với force
        await resetButton.click();
        await firstBox.locator('.el-button--primary').click();
        await expect(cursorBox).toContainText('Đối tác A', { timeout: 10_000 });
        await cursorBox.locator('.el-message-box__btns .el-button--primary').click();
        await expect.poll(() => resetBodies.length, { timeout: 10_000 }).toBe(3);
        expect(resetBodies[2].force).toBe(true);
    });

    /**
     * Author: DatHQ
     * Description: Đăng ký đang bật khóa phần chiều gửi đúng phạm vi, khớp chặn ESH-2306 của máy chủ: tab Mẫu chung khóa khi còn bất kỳ đăng ký gửi nào của gói đang bật (kể cả hai chiều),
     *              đăng ký chiều nhận không khóa; tab đối tác chỉ khóa theo đúng đối tác đó. Khi khóa: có dòng cảnh báo nêu tên đối tác, nút Lưu & phân tích và Dùng lại mẫu chung tắt, ô Nguồn giá trị tắt.
     * Created date: 05/11/2026
     */
    const lockCases = [
        { name: '76a. Tab Mẫu chung, đăng ký gửi của đối tác có cấu hình riêng đang bật: khóa, nêu tên đối tác', tab: null, direction: 0, locked: true },
        { name: '76b. Tab Mẫu chung, đăng ký hai chiều đang bật: khóa', tab: null, direction: 2, locked: true },
        { name: '76c. Tab Mẫu chung, chỉ có đăng ký chiều nhận đang bật: không khóa', tab: null, direction: 1, locked: false },
        { name: '76d. Tab đối tác có đăng ký gửi đang bật: khóa', tab: 'DT-B', direction: 0, locked: true },
        { name: '76e. Tab đối tác khác đối tác có đăng ký đang bật: không khóa', tab: 'DT-A', direction: 0, locked: false },
    ];
    for (const c of lockCases) {
        test(c.name, async ({ page }) => {
            const LOCK_SQL = 'SELECT TOP 100 td.Speed AS speed FROM TmsTrafficData td WHERE td.Lane = @lane';
            await page.route('**/api/sharedata/sharedatasubscription/list*', (r) =>
                r.fulfill(json({ code: 200, result: [{ id: 'S1', partnerId: 'P2', partnerName: 'Đối tác B', partnerCode: 'DT-B', direction: c.direction, state: 1 }] }))
            );
            await page.route('**/api/sharedata/sharedatapacket/getextractsql*', (r) => r.fulfill(json({ code: 200, result: { extractSql: LOCK_SQL, status: 1, fields: SAVED_FIELDS } })));
            await page.route('**/api/sharedata/sharedatapacket/getpartnersqllist*', (r) =>
                r.fulfill(
                    json({
                        code: 200,
                        result: [
                            { partnerId: 'P1', partnerCode: 'DT-A', partnerName: 'Đối tác A', isCustom: true },
                            { partnerId: 'P2', partnerCode: 'DT-B', partnerName: 'Đối tác B', isCustom: true },
                        ],
                    })
                )
            );
            await page.route('**/api/sharedata/sharedatapacket/getpartnerextractsql*', (r) =>
                r.fulfill(json({ code: 200, result: { packetCode: '101_commonData', standardSql: LOCK_SQL, customSql: `${LOCK_SQL} AND 1 = 1`, isCustom: true, hasDifferences: true, commonFields: SAVED_FIELDS, fields: SAVED_FIELDS } }))
            );
            await page.route('**/api/sharedata/sharedatapacket/getsqlvariables*', (r) =>
                r.fulfill(json({ code: 200, result: { items: [{ code: 'lane', valueKind: 'Const', dataType: 'String', value: 'L1' }] } }))
            );
            await openSqlCenter(page);
            if (c.tab) await page.locator('.sql-partner-tabs .el-tabs__item', { hasText: c.tab }).click();
            await page.waitForTimeout(800);

            const alert = page.locator('.send-regions-wrapper .el-alert--warning');
            const save = page.locator('.sql-toolbar-bar .el-button--primary').last();
            const sourceSelect = page.locator('.sql-variable-panel .svp-source-select .el-select__wrapper').first();
            await page.locator('.sql-section .sql-raw-textarea-fallback textarea').fill(`${LOCK_SQL} AND 2 = 2`);
            // Khung soạn đẩy nội dung lên trang sau 450ms kể từ lần gõ cuối
            await page.waitForTimeout(600);

            if (c.locked) {
                await expect(alert).toContainText('Đối tác B');
                await expect(save).toBeDisabled();
                await expect(sourceSelect).toHaveClass(/is-disabled/);
            } else {
                await expect(alert).toHaveCount(0);
                await expect(save).toBeEnabled();
                await expect(sourceSelect).not.toHaveClass(/is-disabled/);
            }
            if (c.tab) {
                const resetButton = page.locator('.sql-toolbar-bar .el-button', { hasText: 'Dùng lại mẫu chung' });
                if (c.locked) await expect(resetButton).toBeDisabled();
                else await expect(resetButton).toBeEnabled();
            }
        });
    }

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
        await expect(rows.nth(0)).toContainText('Cấu hình riêng');
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
        expect(editorBox.height).toBeGreaterThan(150);
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
     * Author: DatHQ
     * Description: Câu ghi chiều nhận khóa theo đăng ký nhận, khớp chặn ESH-2306 của máy chủ: còn đăng ký nhận hoặc hai chiều đang bật thì có dòng cảnh báo nêu tên đối tác và nút Lưu tắt;
     *              chỉ có đăng ký gửi đang bật thì không khóa phần này.
     * Created date: 05/11/2026
     */
    const writeLockCases = [
        { name: '78a. Chiều nhận, còn đăng ký nhận đang bật: khóa nút Lưu, nêu tên đối tác', direction: 1, locked: true },
        { name: '78b. Chiều nhận, còn đăng ký hai chiều đang bật: khóa nút Lưu', direction: 2, locked: true },
        { name: '78c. Chiều nhận, chỉ có đăng ký gửi đang bật: không khóa', direction: 0, locked: false },
    ];
    for (const c of writeLockCases) {
        test(c.name, async ({ page }) => {
            await page.route('**/api/sharedata/sharedatapacket/validatewritesql*', (r) =>
                r.fulfill(json({ code: 200, result: { isValid: true, detectedTables: ['dbo.TmsTrafficData'], keyFields: ['speed'], checks: [] } }))
            );
            await page.route('**/api/sharedata/sharedatasubscription/list*', (r) =>
                r.fulfill(json({ code: 200, result: [{ id: 'S1', partnerId: 'P2', partnerName: 'Đối tác B', partnerCode: 'DT-B', direction: c.direction, state: 1 }] }))
            );
            await openSqlCenter(page);
            await page.locator('.direction-tabs .el-tabs__item', { hasText: 'Nhận về' }).click();
            const panel = page.locator('.write-sql-panel');
            await expect(panel).toBeVisible();
            await panel.getByRole('button', { name: 'Chèn mẫu MERGE' }).click();
            await expect(panel.locator('.wsp-detected-tag')).toContainText('dbo.TmsTrafficData', { timeout: 10_000 });

            const save = panel.locator('.wsp-save-btn');
            const alert = panel.locator('.el-alert--warning');
            if (c.locked) {
                await expect(alert).toContainText('Đối tác B');
                await expect(save).toBeDisabled();
            } else {
                await expect(alert).toHaveCount(0);
                await expect(save).toBeEnabled({ timeout: 10_000 });
            }
        });
    }

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
        { name: '40. Khối 2 đã thu gọn từ trước và gói chưa có trường nào: giữ thu gọn như người dùng chọn', fields: [] as typeof SAVED_FIELDS, expectOpen: false },
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


    test('40c. Khối 2 thu gọn: bấm nút thu gọn/mở rộng chuyển đổi trạng thái đóng mở bình thường kể cả khi chưa có trường', async ({ page }) => {
        await page.route('**/api/sharedata/sharedatapacket/getextractsql*', (r) =>
            r.fulfill(json({ code: 200, result: { extractSql: SQL_ONE_COLUMN, status: 1, fields: [] } }))
        );
        await openSqlCenter(page);

        const fieldsSection = page.locator('.fields-section');
        const wrapper = fieldsSection.locator('.saved-fields-grid-wrapper');
        const collapseBlockBtn = fieldsSection.locator('.fields-bar-actions .el-button').last();
        await expect(wrapper).toBeVisible({ timeout: 5000 });

        // Bấm thu gọn khi chưa có trường -> phải thu gọn được
        await collapseBlockBtn.click();
        await expect(wrapper).toBeHidden({ timeout: 5000 });
        await expect(collapseBlockBtn).toHaveAttribute('aria-label', 'Mở rộng');

        // Bấm mở rộng lại -> mở ra
        await collapseBlockBtn.click();
        await expect(wrapper).toBeVisible({ timeout: 5000 });
        await expect(collapseBlockBtn).toHaveAttribute('aria-label', 'Thu gọn');
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


    /**
     * Description: Lưu câu SQL đổi công thức cột mốc khi đối tác đang có mốc nối đuôi: máy chủ chưa lưu, trả requiresConfirm kèm đối tác
     *              sẽ bị đặt lại mốc; màn hình hỏi xác nhận nêu tên đối tác, đồng ý thì gửi lại với force = true.
     * Created date: 29/10/2026
     */
    test('59. Lưu câu SQL đổi công thức cột mốc khi đối tác đang có mốc: hỏi xác nhận kèm tên đối tác, đồng ý thì gửi lại với force', async ({ page }) => {
        await page.route('**/api/sharedata/sharedatapacket/describecolumns*', (r) =>
            r.fulfill(json({ code: 200, result: { isValid: true, fields: SAVED_FIELDS, added: [], lost: [], typeChanged: [], affectedMappings: [] } }))
        );
        const saveBodies: any[] = [];
        await page.route('**/api/sharedata/sharedatapacket/updateextractsql*', (r) => {
            const body = r.request().postDataJSON();
            saveBodies.push(body);
            return r.fulfill(
                json({
                    code: 200,
                    result: body?.force
                        ? { extractSql: SQL_ONE_COLUMN, status: 1, fields: SAVED_FIELDS, warnings: [] }
                        : {
                              extractSql: SQL_ONE_COLUMN,
                              status: 1,
                              fields: SAVED_FIELDS,
                              requiresConfirm: true,
                              lost: [],
                              affectedMappings: [],
                              cursorResetPartners: [{ partnerId: 'P_A', partnerCode: 'DT-A', partnerName: 'Đối tác A' }],
                          },
                })
            );
        });
        await openSqlCenter(page);
        const save = page.locator('.sql-toolbar-bar .el-button--primary').last();
        await page.locator('.sql-section .sql-raw-textarea-fallback textarea').fill(`${SQL_ONE_COLUMN}\n-- đổi công thức mốc`);
        await expect(save).toBeEnabled({ timeout: 10_000 });
        await save.click();

        const confirmBox = page.locator('.el-message-box');
        await expect(confirmBox).toBeVisible({ timeout: 10_000 });
        await expect(confirmBox.locator('.cursor-reset-confirm')).toContainText('đổi công thức hoặc bảng nguồn của cột mốc');
        await expect(confirmBox.locator('.cursor-reset-confirm')).toContainText('Mốc đang chạy');
        await expect(confirmBox.locator('.cursor-reset-confirm')).toContainText('Mốc bắt đầu');
        await expect(confirmBox.locator('.cursor-reset-confirm')).toContainText('Đối tác A');
        expect(saveBodies).toHaveLength(1);
        expect(saveBodies[0].force).toBeFalsy();

        await confirmBox.locator('.el-message-box__btns .el-button--primary').click();
        await expect.poll(() => saveBodies.length, { timeout: 10_000 }).toBe(2);
        expect(saveBodies[1].force).toBe(true);
    });

    /**
     * Author: DatHQ
     * Description: Lưu câu riêng ở tab đối tác đổi công thức cột mốc: hỏi xác nhận kèm tên đối tác; hủy thì không gửi lại, đồng ý thì gửi lại với force.
     * Created date: 05/11/2026
     */
    test('59b. Tab đối tác lưu câu đổi công thức cột mốc: hỏi xác nhận, hủy thì thôi, đồng ý thì gửi lại với force', async ({ page }) => {
        await page.route('**/api/sharedata/sharedatapacket/describecolumns*', (r) =>
            r.fulfill(json({ code: 200, result: { isValid: true, fields: SAVED_FIELDS, added: [], lost: [], typeChanged: [], affectedMappings: [] } }))
        );
        await page.route('**/api/sharedata/sharedatapacket/getpartnersqllist*', (r) =>
            r.fulfill(json({ code: 200, result: [{ partnerId: 'P_A', partnerCode: 'DT-A', partnerName: 'Đối tác A', isCustom: true }] }))
        );
        await page.route('**/api/sharedata/sharedatapacket/getpartnerextractsql*', (r) =>
            r.fulfill(json({ code: 200, result: { partnerId: 'P_A', packetCode: '101_commonData', standardSql: SQL_ONE_COLUMN, customSql: `${SQL_ONE_COLUMN} WHERE 1 = 1`, isCustom: true, hasDifferences: true, commonFields: SAVED_FIELDS, fields: SAVED_FIELDS } }))
        );
        const saveBodies: any[] = [];
        await page.route('**/api/sharedata/sharedatapacket/updatepartnerextractsql*', (r) => {
            const body = r.request().postDataJSON();
            saveBodies.push(body);
            return r.fulfill(
                json({
                    code: 200,
                    result: body.force
                        ? { partnerId: 'P_A', isCustom: true, customSql: body.extractSql, standardSql: SQL_ONE_COLUMN, fields: SAVED_FIELDS, commonFields: SAVED_FIELDS }
                        : { partnerId: 'P_A', isCustom: true, requiresConfirm: true, lost: [], affectedMappings: [], cursorResetPartners: [{ partnerId: 'P_A', partnerCode: 'DT-A', partnerName: 'Đối tác A' }] },
                })
            );
        });
        await openSqlCenter(page);
        await page.locator('.sql-partner-tabs .el-tabs__item', { hasText: 'DT-A' }).click();
        const save = page.locator('.sql-toolbar-bar .el-button--primary').last();
        await expect(save).toBeDisabled({ timeout: 15_000 });
        await page.locator('.sql-section .sql-raw-textarea-fallback textarea').fill(`${SQL_ONE_COLUMN} WHERE 2 = 2`);
        await page.waitForTimeout(600);
        await expect(save).toBeEnabled({ timeout: 10_000 });
        const confirmBox = page.locator('.el-message-box', { has: page.locator('.cursor-reset-confirm') });

        // Hủy: không gửi lại, câu đang soạn giữ nguyên để sửa tiếp
        await save.click();
        await expect(confirmBox).toContainText('Đối tác A', { timeout: 10_000 });
        await confirmBox.locator('.el-message-box__btns .el-button').first().click();
        await expect(confirmBox).toBeHidden();
        expect(saveBodies).toHaveLength(1);
        expect(saveBodies[0].force).toBeFalsy();

        // Đồng ý: gửi lại với force
        await save.click();
        await expect(confirmBox).toContainText('Đối tác A', { timeout: 10_000 });
        await confirmBox.locator('.el-message-box__btns .el-button--primary').click();
        await expect.poll(() => saveBodies.length, { timeout: 10_000 }).toBe(3);
        expect(saveBodies[2].force).toBe(true);
        expect(saveBodies[2].partnerId).toBe('P_A');
    });

    /**
     * Description: Lưu & phân tích câu SQL dùng biến mốc của nhóm (@lastKey_a, @lastKey_b): biến mới tự chọn nguồn theo tiền tố
     *              như @lastKey, và bộ biến gửi kèm câu SQL trong cùng một lần lưu
     *              (không gọi API lưu bộ biến riêng trước câu SQL, câu bị từ chối thì bộ biến cũng không lưu).
     * Created date: 31/10/2026
     * Modified date: 03/11/2026
     */
    test('67. Lưu & phân tích SQL có biến mốc nhóm: biến tự chọn nguồn theo tiền tố và gửi kèm câu SQL trong một lần lưu', async ({ page }) => {
        await page.route('**/api/sharedata/sharedatapacket/describecolumns*', (r) =>
            r.fulfill(json({ code: 200, result: { isValid: true, fields: SAVED_FIELDS, added: [], lost: [], typeChanged: [], affectedMappings: [] } }))
        );
        const saveBodies: any[] = [];
        const variableSaves: any[] = [];
        await page.route('**/api/sharedata/sharedatapacket/updateextractsql*', (r) => {
            saveBodies.push(r.request().postDataJSON());
            return r.fulfill(json({ code: 200, result: { extractSql: SQL_ONE_COLUMN, status: 1, fields: SAVED_FIELDS, warnings: [] } }));
        });
        await page.route('**/api/sharedata/sharedatapacket/updatesqlvariables*', (r) => {
            variableSaves.push(r.request().postDataJSON());
            return r.fulfill(json({ code: 200, result: { items: [] } }));
        });
        await page.route('**/api/sharedata/sharedatapacket/getsqlvariables*', (r) =>
            r.fulfill(json({ code: 200, result: { items: saveBodies.at(-1)?.variables ?? [] } }))
        );
        await openSqlCenter(page);
        const save = page.locator('.sql-toolbar-bar .el-button--primary').last();
        await page
            .locator('.sql-section .sql-raw-textarea-fallback textarea')
            .fill(
                "SELECT TOP 100 x.ID AS zoneId, CONVERT(varchar(23), x.UpdateTime, 121) + '_' + x.ID AS __lastKey_a, CONVERT(varchar(23), x.CreateTime, 121) + '_' + x.ID AS __lastKey_b FROM TmsZone x " +
                    "WHERE CONVERT(varchar(23), x.UpdateTime, 121) + '_' + x.ID > @lastKey_a OR CONVERT(varchar(23), x.CreateTime, 121) + '_' + x.ID > @lastKey_b"
            );
        await expect(save).toBeEnabled({ timeout: 10_000 });
        await save.click();

        await expect.poll(() => saveBodies.length, { timeout: 10_000 }).toBe(1);
        expect(saveBodies[0].variables).toEqual([
            expect.objectContaining({ code: 'lastKey_a', valueKind: 'Builtin', value: 'lastKey' }),
            expect.objectContaining({ code: 'lastKey_b', valueKind: 'Builtin', value: 'lastKey' }),
        ]);
        expect(variableSaves).toHaveLength(0);
    });

    /**
     * Author: DatHQ
     * Description: Đổi từ câu một nhóm sang câu hai nhóm mốc ở mục Mẫu câu: Lưu & phân tích thì hai biến @lastKey_upd, @lastKey_new tự có nguồn, dòng @lastKey cũ tự bỏ, không gửi lên máy chủ.
     * Created date: 05/11/2026
     */
    test('67b. Đổi sang câu hai nhóm mốc: hai biến tự có nguồn, dòng @lastKey cũ tự bỏ', async ({ page }) => {
        await page.route('**/api/sharedata/sharedatapacket/describecolumns*', (r) =>
            r.fulfill(json({ code: 200, result: { isValid: true, fields: SAVED_FIELDS, added: [], lost: [], typeChanged: [], affectedMappings: [] } }))
        );
        const saveBodies: any[] = [];
        await page.route('**/api/sharedata/sharedatapacket/updateextractsql*', (r) => {
            const body = r.request().postDataJSON();
            saveBodies.push(body);
            return r.fulfill(json({ code: 200, result: { extractSql: body.extractSql, status: 1, fields: SAVED_FIELDS, warnings: [] } }));
        });
        const oneKeyFormula = "CONVERT(varchar(23), COALESCE(x.UpdateTime, x.CreateTime), 121) + '_' + x.ID";
        const oneKeySql = `SELECT TOP 100 x.ID AS zoneId, ${oneKeyFormula} AS __lastKey FROM TmsZone x WHERE ${oneKeyFormula} > @lastKey ORDER BY __lastKey ASC`;
        await page.route('**/api/sharedata/sharedatapacket/getextractsql*', (r) => r.fulfill(json({ code: 200, result: { extractSql: oneKeySql, status: 1, fields: SAVED_FIELDS } })));
        await page.route('**/api/sharedata/sharedatapacket/getsqlvariables*', (r) =>
            r.fulfill(json({ code: 200, result: { items: saveBodies.at(-1)?.variables ?? [{ code: 'lastKey', valueKind: 'Builtin', value: 'lastKey', dataType: 'String' }] } }))
        );
        await openSqlCenter(page);
        const panel = page.locator('.sql-variable-panel');
        const table = panel.locator('.svp-table').first();
        await expect(table.locator('tbody tr', { hasText: '@lastKey' })).toHaveCount(1, { timeout: 10_000 });
        const save = page.locator('.sql-toolbar-bar .el-button--primary').last();

        await page.locator('.sql-section .sql-raw-textarea-fallback textarea').fill(
            'SELECT TOP 100 u.zoneId, u.__lastKey_upd, u.__lastKey_new FROM (' +
                "SELECT x.ID AS zoneId, CONVERT(varchar(23), x.UpdateTime, 121) + '_' + x.ID AS sortKey, CONVERT(varchar(23), x.UpdateTime, 121) + '_' + x.ID AS __lastKey_upd, CAST(NULL AS nvarchar(100)) AS __lastKey_new FROM TmsZone x " +
                "WHERE CONVERT(varchar(23), x.UpdateTime, 121) + '_' + x.ID > @lastKey_upd " +
                "UNION ALL SELECT y.ID, CONVERT(varchar(23), y.CreateTime, 121) + '_' + y.ID, NULL, CONVERT(varchar(23), y.CreateTime, 121) + '_' + y.ID FROM TmsZone y " +
                "WHERE y.UpdateTime IS NULL AND CONVERT(varchar(23), y.CreateTime, 121) + '_' + y.ID > @lastKey_new" +
                ') u ORDER BY u.sortKey ASC'
        );
        await page.waitForTimeout(600);
        await expect(save).toBeEnabled({ timeout: 10_000 });
        await save.click();

        await expect.poll(() => saveBodies.length, { timeout: 10_000 }).toBe(1);
        expect(saveBodies[0].variables).toEqual([
            expect.objectContaining({ code: 'lastKey_upd', valueKind: 'Builtin', value: 'lastKey' }),
            expect.objectContaining({ code: 'lastKey_new', valueKind: 'Builtin', value: 'lastKey' }),
        ]);
        await expect(table.locator('tbody tr', { hasText: '@lastKey_upd' })).toBeVisible();
        await expect(table.locator('tbody tr', { hasText: /@lastKey(?!_)/ })).toHaveCount(0);
    });

    /**
     * Author: DatHQ
     * Description: Đọc kết quả lưu: máy chủ từ chối thì báo đỏ ngay trên khung soạn bằng câu đã dịch, không lộ mã lỗi, khung soạn giữ câu đang soạn;
     *              sửa lại lưu được thì báo xanh và hết báo đỏ.
     * Created date: 05/11/2026
     */
    test('77. Lưu & phân tích: bị từ chối thì báo đỏ câu đã dịch và giữ câu đang soạn; sửa lại lưu được thì báo xanh, hết báo đỏ', async ({ page }) => {
        await page.route('**/api/sharedata/sharedatapacket/describecolumns*', (r) =>
            r.fulfill(json({ code: 200, result: { isValid: true, fields: SAVED_FIELDS, added: [], lost: [], typeChanged: [], affectedMappings: [] } }))
        );
        let reject = true;
        await page.route('**/api/sharedata/sharedatapacket/updateextractsql*', (r) => {
            if (reject) {
                return r.fulfill({ status: 400, contentType: 'application/json', body: JSON.stringify({ code: 400, message: 'lz.validation.sharedata.sqlIncrementalKeyFilterInclusive' }) });
            }
            return r.fulfill(json({ code: 200, result: { extractSql: r.request().postDataJSON().extractSql, status: 1, fields: SAVED_FIELDS, warnings: [] } }));
        });
        await openSqlCenter(page);
        const editor = page.locator('.sql-section .sql-raw-textarea-fallback textarea');
        const save = page.locator('.sql-toolbar-bar .el-button--primary').last();
        const errorAlert = page.locator('.sql-error-alert');

        const rejectedSql = `${SQL_ONE_COLUMN} WHERE td.Speed > 0`;
        await editor.fill(rejectedSql);
        await page.waitForTimeout(600);
        await save.click();
        await expect(errorAlert).toContainText('So cột mốc với biến mốc phải dùng dấu >', { timeout: 10_000 });
        await expect(errorAlert).not.toContainText('lz.validation');
        await expect(editor).toHaveValue(rejectedSql);

        reject = false;
        await editor.fill(`${SQL_ONE_COLUMN} WHERE td.Speed > 1`);
        await page.waitForTimeout(600);
        await save.click();
        await expect(page.locator('.el-message--success').last()).toContainText('Lưu cấu hình SQL trích xuất thành công', { timeout: 10_000 });
        await expect(errorAlert).toHaveCount(0);
    });

    /**
     * Description: Dán câu sai cấu trúc (__lastKey chưa ghép thời gian, còn @lastTime): Lưu & phân tích thì báo cần sửa câu, dòng @lastTime ghi tự bỏ khi sửa câu, không gọi API lưu;
     *              sửa câu theo một chuỗi thì dòng @lastTime tự bỏ và lưu được, bộ biến gửi lên không còn @lastTime.
     * Created date: 03/11/2026
     */
    test('69. Câu sai cấu trúc: báo cần sửa câu, không lưu được; sửa theo một chuỗi thì lưu được', async ({ page }) => {
        await page.route('**/api/sharedata/sharedatapacket/describecolumns*', (r) =>
            r.fulfill(json({ code: 200, result: { isValid: true, fields: SAVED_FIELDS, added: [], lost: [], typeChanged: [], affectedMappings: [] } }))
        );
        const saveBodies: any[] = [];
        await page.route('**/api/sharedata/sharedatapacket/updateextractsql*', (r) => {
            saveBodies.push(r.request().postDataJSON());
            return r.fulfill(json({ code: 200, result: { extractSql: SQL_ONE_COLUMN, status: 1, fields: SAVED_FIELDS, warnings: [] } }));
        });
        await page.route('**/api/sharedata/sharedatapacket/getsqlvariables*', (r) =>
            r.fulfill(json({ code: 200, result: { items: saveBodies.at(-1)?.variables ?? [] } }))
        );
        await openSqlCenter(page);
        const editor = page.locator('.sql-section .sql-raw-textarea-fallback textarea');
        const save = page.locator('.sql-toolbar-bar .el-button--primary').last();

        await editor.fill(
            'SELECT TOP 100 x.ID AS zoneId, x.UpdateTime AS __lastTime, x.ID AS __lastKey FROM TmsZone x ' +
                'WHERE @lastTime IS NULL OR x.UpdateTime > @lastTime OR (x.UpdateTime = @lastTime AND x.ID > @lastKey) ORDER BY x.UpdateTime ASC, x.ID ASC'
        );
        await expect(save).toBeEnabled({ timeout: 10_000 });
        await save.click();

        const panel = page.locator('.sql-variable-panel');
        const lastTimeRow = panel.locator('.svp-table').first().locator('tbody tr', { hasText: '@lastTime' });
        await expect(lastTimeRow).toContainText('tự bỏ khi sửa câu', { timeout: 10_000 });
        await expect(panel).not.toContainText('không còn dùng');
        await expect(page.locator('.el-message--warning')).toContainText('quy chuẩn mốc');
        expect(saveBodies).toHaveLength(0);

        await editor.fill(
            "SELECT TOP 100 x.ID AS zoneId, CONVERT(varchar(23), COALESCE(x.UpdateTime, x.CreateTime), 121) + '_' + x.ID AS __lastKey FROM TmsZone x " +
                "WHERE CONVERT(varchar(23), COALESCE(x.UpdateTime, x.CreateTime), 121) + '_' + x.ID > @lastKey ORDER BY __lastKey ASC"
        );
        // Khung soạn đẩy nội dung lên trang sau 450ms kể từ lần gõ cuối
        await page.waitForTimeout(600);
        await save.click();

        await expect.poll(() => saveBodies.length, { timeout: 10_000 }).toBe(1);
        expect(saveBodies[0].variables.map((v: any) => v.code)).toEqual(['lastKey']);
        await expect(panel.locator('.svp-table').first().locator('tbody tr', { hasText: '@lastTime' })).toHaveCount(0);
    });

    /**
     * Description: Cột giá trị của bảng tham số: @pageSize tự điền 100 nên Lưu & phân tích lưu ngay; câu ghép kiểu 121 chọn ngày giờ thì tự ghép chuỗi mốc bắt đầu, để trống là gửi toàn bộ dữ liệu;
     *              không có ô gõ tay; câu dạng khác không chọn được ngày, giá trị đã lưu chỉ hiện để xem và xóa được.
     * Created date: 03/11/2026
     */
    /**
     * Description: Nguồn theo tên biến, không có ô chọn. Biến tên khác trong câu không có __lastKey là giá trị nhập, lưu bình thường;
     *              câu có __lastKey mà lọc bằng biến tên khác thì chặn lưu (máy chủ sẽ coi là gửi toàn bộ), bấm Sửa đổi thành @lastKey rồi lưu được;
     *              biến tên khác đã lưu nguồn mốc vẫn giữ nguyên.
     * Created date: 03/11/2026
     */
    test('73. Biến tên khác @lastKey: câu thường là giá trị nhập; câu có __lastKey thì chặn lưu, Sửa đổi thành @lastKey', async ({ page }) => {
        await page.route('**/api/sharedata/sharedatapacket/describecolumns*', (r) =>
            r.fulfill(json({ code: 200, result: { isValid: true, fields: SAVED_FIELDS, added: [], lost: [], typeChanged: [], affectedMappings: [] } }))
        );
        const saveBodies: any[] = [];
        await page.route('**/api/sharedata/sharedatapacket/updateextractsql*', (r) => {
            const body = r.request().postDataJSON();
            saveBodies.push(body);
            return r.fulfill(json({ code: 200, result: { extractSql: body.extractSql, status: 1, fields: SAVED_FIELDS, warnings: [] } }));
        });
        await page.route('**/api/sharedata/sharedatapacket/getsqlvariables*', (r) =>
            r.fulfill(json({ code: 200, result: { items: saveBodies.at(-1)?.variables ?? [] } }))
        );
        await openSqlCenter(page);
        const editor = page.locator('.sql-section .sql-raw-textarea-fallback textarea');
        const save = page.locator('.sql-toolbar-bar .el-button--primary').last();
        const panel = page.locator('.sql-variable-panel');
        const table = panel.locator('.svp-table').first();

        // Câu thường: biến tên khác mặc định chọn Giá trị nhập trong el-select, không có dải Quy chuẩn mốc
        await editor.fill('SELECT x.ID AS zoneId FROM TmsZone x WHERE x.Code = @maTram');
        await page.waitForTimeout(600);
        await save.click();
        const maTramRow = table.locator('tbody tr', { hasText: '@maTram' });
        await expect(maTramRow).toContainText('Giá trị nhập', { timeout: 10_000 });
        // Biến lạ chưa có giá trị: lần bấm đầu chỉ trích biến và báo nhập giá trị, chưa lưu
        await expect(page.locator('.el-message--warning').last()).toContainText('Sửa các dòng tham số đang báo lỗi trước khi lưu.');
        expect(saveBodies).toHaveLength(0);
        await expect(maTramRow.locator('.svp-source-select')).toBeVisible();
        await expect(panel.locator('.svp-check-lead')).toHaveCount(0);
        await maTramRow.locator('.svp-const-input input').fill('TRAM01');
        await save.click();
        await expect.poll(() => saveBodies.length, { timeout: 10_000 }).toBe(1);
        expect(saveBodies[0].variables).toEqual([expect.objectContaining({ code: 'maTram', valueKind: 'Const', value: 'TRAM01' })]);

        // Câu có __lastKey mà lọc bằng @since (mặc định đang là Giá trị nhập): chặn lưu, mục lọc chưa đạt
        const formula = "CONVERT(varchar(23), COALESCE(x.UpdateTime, x.CreateTime), 121) + '_' + x.ID";
        await editor.fill(`SELECT x.ID AS zoneId, ${formula} AS __lastKey FROM TmsZone x WHERE ${formula} > @since ORDER BY __lastKey ASC`);
        await page.waitForTimeout(600);
        await save.click();
        await expect(page.locator('.el-message--warning').last()).toContainText('quy chuẩn mốc', { timeout: 10_000 });
        expect(saveBodies).toHaveLength(1);
        const lead = panel.locator('.svp-check-lead');
        await expect(lead).toContainText('(2/3)');

        // Người dùng có thể chọn nguồn "Theo Mốc đang chạy" cho @since trên dropdown Nguồn giá trị -> đạt (3/3) ngay
        const sinceRow = table.locator('tbody tr', { hasText: '@since' });
        await sinceRow.locator('.svp-source-select').click();
        const popper = page.locator('.svp-source-popper:visible');
        await expect(popper.locator('.svp-source-opt')).toHaveCount(2);
        await popper.locator('.el-select-dropdown__item', { hasText: 'Theo Mốc đang chạy' }).click();
        await expect(lead).toContainText('(3/3)');
        await expect(sinceRow.locator('.cvi-picker input')).toBeEnabled();

        // Chuyển lại Giá trị nhập rồi bấm Sửa câu trong popover: đổi điều kiện thành @lastKey, dòng @since tự bỏ, lưu được
        await sinceRow.locator('.svp-source-select').click();
        await page.locator('.svp-source-popper:visible .el-select-dropdown__item', { hasText: 'Giá trị nhập' }).click();
        await expect(lead).toContainText('(2/3)');
        await lead.hover();
        await page.locator('.svp-popover-content .svp-fix-all').click();
        await expect(editor).toHaveValue(new RegExp(`> @lastKey`));
        await expect(lead).toContainText('(3/3)');
        await expect(table.locator('tbody tr', { hasText: '@since' })).toHaveCount(0);
        await expect(table.locator('tbody tr', { hasText: '@lastKey' })).toContainText('Theo Mốc đang chạy');
        await save.click();
        await expect.poll(() => saveBodies.length, { timeout: 10_000 }).toBe(2);
        expect(saveBodies[1].extractSql).not.toContain('@since');
    });

    /**
     * Description: Biến tên khác đã lưu nguồn mốc vẫn giữ nguyên để câu đang chạy không đổi; biến đã lưu nguồn khác chuyển thành giá trị nhập.
     * Created date: 03/11/2026
     */
    test('74. Cấu hình đã lưu: @since nguồn mốc giữ nguyên, biến nguồn khác thành giá trị nhập', async ({ page }) => {
        const formula = "CONVERT(varchar(23), COALESCE(UpdateTime, CreateTime), 121) + '_' + ID";
        const sql = `SELECT ID AS zoneId, ${formula} AS __lastKey FROM TmsZone WHERE ${formula} > @since AND CreateTime >= @yearStart ORDER BY __lastKey ASC`;
        await page.route('**/api/sharedata/sharedatapacket/getsqlvariables*', (r) =>
            r.fulfill(
                json({
                    code: 200,
                    result: {
                        items: [
                            { code: 'since', dataType: 'String', valueKind: 'Builtin', value: 'lastKey' },
                            { code: 'yearStart', dataType: 'DateTime', valueKind: 'Builtin', value: 'startOfYear' },
                        ],
                    },
                })
            )
        );
        await page.route('**/api/sharedata/sharedatapacket/getextractsql*', (r) => r.fulfill(json({ code: 200, result: { extractSql: sql, status: 1, fields: SAVED_FIELDS } })));
        await openSqlCenter(page);
        const table = page.locator('.sql-variable-panel .svp-table').first();
        await expect(table.locator('tbody tr', { hasText: '@since' })).toContainText('Theo Mốc đang chạy', { timeout: 10_000 });
        await expect(table.locator('tbody tr', { hasText: '@yearStart' })).toContainText('Giá trị nhập');
        await expect(table.locator('tbody tr', { hasText: '@yearStart' }).locator('.svp-const-input input')).toHaveValue('');
        await expect(page.locator('.sql-variable-panel .svp-check-lead')).toContainText('(3/3)');
    });

    /**
     * Description: Thu gọn bảng tham số giống khối trường dữ liệu: bấm thanh tiêu đề hoặc mũi tên, dải Quy chuẩn mốc vẫn hiện, nhớ trạng thái khi tải lại trang;
     *              lưu mà còn dòng lỗi thì tự mở ra để thấy dòng cần sửa.
     * Created date: 03/11/2026
     */
    test('75. Thu gọn bảng tham số: bấm thanh tiêu đề, nhớ trạng thái, lưu có lỗi thì tự mở', async ({ page }) => {
        await page.route('**/api/sharedata/sharedatapacket/describecolumns*', (r) =>
            r.fulfill(json({ code: 200, result: { isValid: true, fields: SAVED_FIELDS, added: [], lost: [], typeChanged: [], affectedMappings: [] } }))
        );
        await openSqlCenter(page);
        const panel = page.locator('.sql-variable-panel');
        const table = panel.locator('.svp-table').first();
        const collapseBtn = panel.locator('.svp-collapse-btn');

        await expect(table).toBeVisible({ timeout: 10_000 });
        await collapseBtn.click();
        await expect(table).toBeHidden();
        await panel.locator('.svp-title').click();
        await expect(table).toBeVisible();
        await panel.locator('.svp-title').click();
        await expect(table).toBeHidden();

        await page.reload();
        await page.locator('.packet-col .vxe-body--row').first().click({ timeout: 60_000 });
        await expect(page.locator('.sql-variable-panel .svp-title')).toBeVisible({ timeout: 30_000 });
        await expect(table).toBeHidden();

        // Lưu có dòng chưa nhập giá trị: tự mở bảng để thấy dòng cần sửa
        await page.locator('.sql-section .sql-raw-textarea-fallback textarea').fill('SELECT x.ID AS zoneId FROM TmsZone x WHERE x.Code = @maTram');
        await page.waitForTimeout(600);
        await page.locator('.sql-toolbar-bar .el-button--primary').last().click();
        await expect(table).toBeVisible({ timeout: 10_000 });
    });

    test('71. Quy chuẩn mốc: bấm Sửa tất cả tự sửa câu trong khung soạn, giữ điều kiện khác và cột __lastTime người dùng viết', async ({ page }) => {
        await page.route('**/api/sharedata/sharedatapacket/describecolumns*', (r) =>
            r.fulfill(json({ code: 200, result: { isValid: true, fields: SAVED_FIELDS, added: [], lost: [], typeChanged: [], affectedMappings: [] } }))
        );
        await openSqlCenter(page);
        const editor = page.locator('.sql-section .sql-raw-textarea-fallback textarea');
        const panel = page.locator('.sql-variable-panel');
        const lead = panel.locator('.svp-check-lead');
        const save = page.locator('.sql-toolbar-bar .el-button--primary').last();

        await editor.fill(
            'SELECT TOP (@pageSize)\n    zs.UpdateTime AS __lastTime,\n    zs.ID AS __lastKey,\n    zs.ZoneId AS zoneId\nFROM dbo.TmsZoneStatus zs\n' +
                "WHERE zs.UpdateTime >= ISNULL(@lastTime, '17530101')\n  AND zs.ZoneId IS NOT NULL\n  AND (@lastTime IS NULL OR zs.UpdateTime > @lastTime OR zs.ID > @lastKey)\nORDER BY zs.UpdateTime ASC, zs.ID ASC"
        );
        await page.waitForTimeout(600);
        await save.click();
        await expect(lead).toContainText('(0/3)', { timeout: 10_000 });

        await lead.hover();
        // Popover co theo nội dung, câu SQL dài tự ngắt dòng thay vì kéo rộng
        await expect(page.locator('.svp-popover-content .svp-fix-code').first()).toBeVisible();
        const popoverBox = await page.locator('.svp-check-popover:visible').boundingBox();
        expect(popoverBox!.width).toBeLessThanOrEqual(400);
        await page.locator('.svp-popover-content .svp-fix-all').click();

        // UpdateTime trống khi thêm mới: công thức lấy CreateTime thay vào
        const formula = "CONVERT(varchar(23), COALESCE(zs.UpdateTime, zs.CreateTime), 121) + '_' + zs.ID";
        await expect(editor).toHaveValue(
            'SELECT TOP (@pageSize)\n    zs.UpdateTime AS __lastTime,\n' + `    ${formula} AS __lastKey,\n    zs.ZoneId AS zoneId\nFROM dbo.TmsZoneStatus zs\n` +
                `WHERE ${formula} > @lastKey\n  AND zs.ZoneId IS NOT NULL\nORDER BY __lastKey ASC`
        );
        await expect(lead).toContainText('(3/3)');
        await expect(panel.locator('.svp-table').first().locator('tbody tr', { hasText: '@lastTime' })).toHaveCount(0);
        await expect(panel.locator('.svp-table').first().locator('tbody tr', { hasText: '@lastKey' })).toHaveCount(1);
        await expect(panel.locator('.cvi-picker input')).toBeEnabled();
    });

    /**
     * Description: Cột mốc khác công thức PA1: UpdateTime trơn (dòng mới thêm chưa có UpdateTime nên không được gửi), cột chuỗi trơn, ép số sang chuỗi, cột thời gian khác không có CreateTime, phần khóa không phải cột ID:
     *              Quy chuẩn mốc chưa đạt, khóa ô ngày; bấm Sửa câu thì cả cột mốc lẫn WHERE về đúng công thức, phần khóa là cột ID, đạt 3/3 và chọn được ngày.
     * Created date: 04/11/2026
     * Modified date: 05/11/2026
     */
    const standardFormula = "CONVERT(varchar(23), COALESCE(x.UpdateTime, x.CreateTime), 121) + '_' + x.ID";
    const cursorFormulaCases = [
        { name: '71b. Quy chuẩn mốc: cột mốc ghép từ UpdateTime trơn chưa đạt, Sửa câu đổi sang COALESCE(UpdateTime, CreateTime)', keyExpr: "CONVERT(varchar(23), x.UpdateTime, 121) + '_' + x.ID", reason: 'chưa có UpdateTime', fixed: standardFormula },
        { name: '71c. Quy chuẩn mốc: cột mốc chỉ có UpdateTime chưa đạt, Sửa câu ghép thêm COALESCE(UpdateTime, CreateTime) và khóa', keyExpr: 'x.UpdateTime', reason: 'chưa có UpdateTime', fixed: standardFormula },
        { name: '71d. Quy chuẩn mốc: cột mốc là cột chuỗi trơn chưa đạt, Sửa câu ghép thời gian trước cột đó', keyExpr: 'x.ID', reason: 'Thay cột mốc bằng', fixed: standardFormula },
        { name: '71e. Quy chuẩn mốc: cột mốc ép số sang chuỗi chưa đạt, Sửa câu đổi về công thức có cột ID', keyExpr: 'CAST(x.FromKmNumber AS varchar(10))', reason: 'Thay cột mốc bằng', fixed: standardFormula },
        { name: '71f. Quy chuẩn mốc: phần thời gian không có CreateTime chưa đạt, Sửa câu bọc thêm CreateTime', keyExpr: "CONVERT(varchar(23), x.DetectTime, 121) + '_' + x.ID", reason: 'Thay cột mốc bằng', fixed: "CONVERT(varchar(23), COALESCE(x.DetectTime, x.CreateTime), 121) + '_' + x.ID" },
        { name: '71g. Quy chuẩn mốc: phần khóa là cột có thể trống chưa đạt, Sửa câu đổi phần khóa thành cột ID, giữ phần thời gian', keyExpr: "CONVERT(varchar(23), COALESCE(x.UpdateTime, x.CreateTime), 121) + '_' + x.Name", reason: 'Thay cột mốc bằng', fixed: standardFormula },
    ];

    for (const c of cursorFormulaCases) {
        test(c.name, async ({ page }) => {
            await page.route('**/api/sharedata/sharedatapacket/describecolumns*', (r) =>
                r.fulfill(json({ code: 200, result: { isValid: true, fields: SAVED_FIELDS, added: [], lost: [], typeChanged: [], affectedMappings: [] } }))
            );
            const saveBodies: any[] = [];
            await page.route('**/api/sharedata/sharedatapacket/updateextractsql*', (r) => {
                const body = r.request().postDataJSON();
                saveBodies.push(body);
                return r.fulfill(json({ code: 200, result: { extractSql: body.extractSql, status: 1, fields: SAVED_FIELDS, warnings: [] } }));
            });
            await page.route('**/api/sharedata/sharedatapacket/getsqlvariables*', (r) =>
                r.fulfill(json({ code: 200, result: { items: saveBodies.at(-1)?.variables ?? [] } }))
            );
            await openSqlCenter(page);
            const editor = page.locator('.sql-section .sql-raw-textarea-fallback textarea');
            const panel = page.locator('.sql-variable-panel');
            const lead = panel.locator('.svp-check-lead');
            const save = page.locator('.sql-toolbar-bar .el-button--primary').last();

            await editor.fill(`SELECT TOP (@pageSize) x.ID AS zoneId, ${c.keyExpr} AS __lastKey FROM TmsZone x WHERE ${c.keyExpr} > @lastKey ORDER BY __lastKey ASC`);
            await page.waitForTimeout(600);
            await save.click();
            await expect(lead).toContainText('(2/3)', { timeout: 10_000 });
            await expect(panel.locator('.cvi-picker input')).toBeDisabled();

            await lead.hover();
            const columnsItem = page.locator('.svp-popover-content .svp-popover-item', { hasText: 'Có cột mốc __lastKey' });
            await expect(columnsItem).toHaveClass(/is-fail/);
            await expect(columnsItem).toContainText(c.reason);
            await expect(columnsItem.locator('.svp-fix-code')).toContainText(c.fixed);
            await page.locator('.svp-popover-content .svp-fix-all').click();

            await expect(editor).toHaveValue(`SELECT TOP (@pageSize) x.ID AS zoneId, ${c.fixed} AS __lastKey FROM TmsZone x WHERE ${c.fixed} > @lastKey\nORDER BY __lastKey ASC`);
            await expect(lead).toContainText('(3/3)');
            await expect(panel.locator('.cvi-picker input')).toBeEnabled();
        });
    }

    test('70. Mốc bắt đầu: chọn ngày giờ tự ghép chuỗi, không có ô gõ tay; @pageSize mặc định 100', async ({ page }) => {
        await page.route('**/api/sharedata/sharedatapacket/describecolumns*', (r) =>
            r.fulfill(json({ code: 200, result: { isValid: true, fields: SAVED_FIELDS, added: [], lost: [], typeChanged: [], affectedMappings: [] } }))
        );
        const saveBodies: any[] = [];
        await page.route('**/api/sharedata/sharedatapacket/updateextractsql*', (r) => {
            const body = r.request().postDataJSON();
            saveBodies.push(body);
            return r.fulfill(json({ code: 200, result: { extractSql: body.extractSql, status: 1, fields: SAVED_FIELDS, warnings: [] } }));
        });
        const variableSaves: any[] = [];
        await page.route('**/api/sharedata/sharedatapacket/updatesqlvariables*', (r) => {
            const body = r.request().postDataJSON();
            variableSaves.push(body);
            return r.fulfill(json({ code: 200, result: { items: body.items } }));
        });
        await page.route('**/api/sharedata/sharedatapacket/getsqlvariables*', (r) =>
            r.fulfill(json({ code: 200, result: { items: variableSaves.at(-1)?.items ?? saveBodies.at(-1)?.variables ?? [] } }))
        );
        await openSqlCenter(page);
        const editor = page.locator('.sql-section .sql-raw-textarea-fallback textarea');
        const save = page.locator('.sql-toolbar-bar .el-button--primary').last();
        const panel = page.locator('.sql-variable-panel');
        const picker = panel.locator('.cvi-picker input');

        await editor.fill(
            "SELECT TOP (@pageSize) x.ID AS zoneId, CONVERT(varchar(23), COALESCE(x.UpdateTime, x.CreateTime), 121) + '_' + x.ID AS __lastKey FROM TmsZone x " +
                "WHERE CONVERT(varchar(23), COALESCE(x.UpdateTime, x.CreateTime), 121) + '_' + x.ID > @lastKey ORDER BY __lastKey ASC"
        );
        // Khung soạn đẩy nội dung lên trang sau 450ms kể từ lần gõ cuối
        await page.waitForTimeout(600);
        await save.click();
        await expect.poll(() => saveBodies.length, { timeout: 10_000 }).toBe(1);
        expect(saveBodies[0].variables).toEqual([
            expect.objectContaining({ code: 'pageSize', valueKind: 'Const', value: '100' }),
            expect.objectContaining({ code: 'lastKey', valueKind: 'Builtin', value: 'lastKey', defaultValue: null }),
        ]);

        // Chưa có mốc: ô ngày trống là gửi toàn bộ dữ liệu; nút có sẵn của ô ngày lấy giờ hiện tại bằng một lần bấm
        await expect(picker).toHaveValue('', { timeout: 10_000 });
        await expect(picker).toHaveAttribute('placeholder', 'Đối tác chưa có mốc: gửi toàn bộ');
        await picker.click();
        await page.locator('.el-picker-panel:visible .el-picker-panel__link-btn').first().click();
        await expect(picker).toHaveValue(/^\d{2}\/\d{2}\/\d{4} \d{2}:\d{2}:\d{2}$/);

        await picker.fill('28/10/2026 00:00:00');
        await page.keyboard.press('Enter');
        await save.click();
        await expect.poll(() => variableSaves.length, { timeout: 10_000 }).toBe(1);
        expect(variableSaves[0].items[1]).toMatchObject({ code: 'lastKey', defaultValue: '2026-10-28 00:00:00.000_' });

        await editor.fill('SELECT TOP (@pageSize) x.ID AS zoneId, x.ID AS __lastKey FROM TmsZone x WHERE x.ID > @lastKey ORDER BY __lastKey ASC');
        // Cột mốc khác công thức: khóa ô ngày cho tới khi sửa câu, không có ô gõ tay và biểu tượng thông tin
        await expect(picker).toBeDisabled({ timeout: 10_000 });
        await expect(panel.locator('.svp-key-input input[type="text"]:not([disabled])')).toHaveCount(0);
        await expect(panel.locator('.cvi-info')).toHaveCount(0);
    });

    /**
     * Description: Biến trong TOP (@biến) là biến cỡ trang dù đặt tên gì: bấm Lưu & phân tích thì @test là số nguyên mặc định 100, ô nhập là ô số, nguồn giá trị bị khóa;
     *              biến thường ngoài TOP vẫn là ô nhập chữ có thể chọn nguồn.
     * Created date: 04/11/2026
     */
    for (const topVariable of ['test', 'rowLimit']) {
        test(`70c. Biến trong TOP (@${topVariable}) luôn là số nguyên mặc định 100, nguồn giá trị bị khóa`, async ({ page }) => {
            await page.route('**/api/sharedata/sharedatapacket/describecolumns*', (r) =>
                r.fulfill(json({ code: 200, result: { isValid: true, fields: SAVED_FIELDS, added: [], lost: [], typeChanged: [], affectedMappings: [] } }))
            );
            const saveBodies: any[] = [];
            await page.route('**/api/sharedata/sharedatapacket/updateextractsql*', (r) => {
                const body = r.request().postDataJSON();
                saveBodies.push(body);
                return r.fulfill(json({ code: 200, result: { extractSql: body.extractSql, status: 1, fields: SAVED_FIELDS, warnings: [] } }));
            });
            await page.route('**/api/sharedata/sharedatapacket/getsqlvariables*', (r) =>
                r.fulfill(json({ code: 200, result: { items: saveBodies.at(-1)?.variables ?? [] } }))
            );
            await openSqlCenter(page);
            const editor = page.locator('.sql-section .sql-raw-textarea-fallback textarea');
            const panel = page.locator('.sql-variable-panel');
            const row = panel.locator('tbody tr', { hasText: `@${topVariable}` });

            await editor.fill(
                `SELECT TOP (@${topVariable}) x.ID AS zoneId, x.ID AS __lastKey FROM TmsZone x WHERE x.ID > @lastKey AND x.Code <> @skipCode ORDER BY __lastKey ASC`
            );
            await page.waitForTimeout(600);
            await page.locator('.sql-toolbar-bar .el-button--primary').last().click();

            await expect(panel.locator('.svp-param-code')).toHaveText([`@${topVariable}`, '@lastKey', '@skipCode'], { timeout: 10_000 });
            await expect(row.locator('.svp-source-select .el-select__wrapper')).toHaveClass(/is-disabled/);
            await expect(row.locator('.el-input-number input')).toHaveValue('100');

            await row.locator('.svp-source-cell').hover();
            const tooltip = page.locator('.el-popper:visible', { hasText: 'Biến phân trang TOP' });
            await expect(tooltip).toBeVisible({ timeout: 5_000 });
            await expect(tooltip).toContainText('Biến phân trang TOP (@biến) bắt buộc là số nguyên lớn hơn 0');

            // Biến thường ngoài TOP: ô nhập chữ, nguồn giá trị chọn được
            const normalRow = panel.locator('tbody tr', { hasText: '@skipCode' });
            await expect(normalRow.locator('.svp-source-select .el-select__wrapper')).not.toHaveClass(/is-disabled/);
            await expect(normalRow.locator('.el-input-number')).toHaveCount(0);
        });
    }

    /**
     * Description: Mẫu gợi ý select-incremental chèn câu nối đuôi dạng một chuỗi ghép (thời gian + '_' + khóa AS __lastKey, lọc theo @lastKey, sắp theo __lastKey):
     *              bấm Lưu & phân tích thì bảng tham số có @pageSize và @lastKey, không có @lastTime; @pageSize tự điền 100 nên lưu thành công,
     *              ô ngày ở dòng @lastKey ghi rõ để trống là gửi toàn bộ dữ liệu, không cần biểu tượng thông tin.
     * Created date: 31/10/2026
     * Modified date: 04/11/2026
     */
    test('68. Chèn mẫu gợi ý select-incremental rồi Lưu & phân tích: lưu thành công, bảng tham số có @pageSize và @lastKey, không có @lastTime', async ({ page }) => {
        let savedVariables: any[] = [];
        const saveBodies: any[] = [];
        await page.route('**/api/sharedata/sharedatapacket/updateextractsql*', (r) => {
            const body = r.request().postDataJSON();
            saveBodies.push(body);
            savedVariables = body?.variables ?? savedVariables;
            return r.fulfill(json({ code: 200, result: { extractSql: SQL_ONE_COLUMN, status: 1, fields: SAVED_FIELDS, warnings: [] } }));
        });
        await page.route('**/api/sharedata/sharedatapacket/getsqlvariables*', (r) => r.fulfill(json({ code: 200, result: { items: savedVariables } })));
        await openSqlCenter(page);
        await retypeSql(page, 'SEL');
        await page.locator('.suggest-widget .monaco-list-row', { hasText: 'select-incremental' }).click({ timeout: 10_000 });
        await page.keyboard.press('Escape');
        // Khung soạn đẩy nội dung lên trang sau 450ms kể từ lần gõ cuối
        await page.waitForTimeout(600);
        const save = page.locator('.sql-toolbar-bar .el-button--primary').last();
        await expect(save).toBeEnabled({ timeout: 10_000 });
        await save.click();

        // Danh sách chính xác hai biến nên @lastTime không được xuất hiện
        const paramCodes = page.locator('.sql-variable-panel .svp-params-card .svp-param-code');
        await expect(paramCodes).toHaveText(['@pageSize', '@lastKey'], { timeout: 10_000 });

        // @pageSize tự điền 100 nên lần bấm đầu lưu luôn
        await expect(page.locator('.el-message--success')).toBeVisible({ timeout: 10_000 });
        expect(saveBodies).toHaveLength(1);
        expect(saveBodies[0].extractSql.replace(/\s+/g, ' ').trim()).toBe(
            "SELECT TOP (@pageSize) t.Column AS aliasName, CONVERT(varchar(23), COALESCE(t.UpdateTime, t.CreateTime), 121) + '_' + t.ID AS __lastKey " +
                "FROM TableName t WHERE CONVERT(varchar(23), COALESCE(t.UpdateTime, t.CreateTime), 121) + '_' + t.ID > @lastKey ORDER BY __lastKey ASC"
        );
        expect(saveBodies[0].variables).toEqual([
            expect.objectContaining({ code: 'pageSize', valueKind: 'Const', value: '100' }),
            expect.objectContaining({ code: 'lastKey', valueKind: 'Builtin', value: 'lastKey' }),
        ]);

        await expect(paramCodes).toHaveText(['@pageSize', '@lastKey']);
        const lastKeyRow = page.locator('.sql-variable-panel .svp-params-card tbody tr', { hasText: '@lastKey' });
        await expect(lastKeyRow.locator('.cvi-picker input')).toHaveAttribute('placeholder', 'Đối tác chưa có mốc: gửi toàn bộ');
        await expect(lastKeyRow.locator('.cvi-info')).toHaveCount(0);
    });

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
