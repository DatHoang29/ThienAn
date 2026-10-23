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

const SQL_ONE_COLUMN = 'SELECT TOP (@pageSize) td.Speed AS speed FROM TmsTrafficData td';

/** Mở màn Nguồn dữ liệu, chọn gói tin đầu tiên, sang tab Cấu hình SQL và chờ khung soạn thảo sẵn sàng. */
async function openSqlCenter(page: Page) {
    await page.setViewportSize({ width: 1600, height: 900 });
    await page.goto('/#/sharedata/dataSource');
    await page.locator('.packet-col .vxe-body--row').first().click({ timeout: 60_000 });
    await page.getByRole('tab', { name: /Cấu hình SQL/ }).click({ timeout: 30_000 });
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

const editorText = (page: Page) => page.locator('.sql-monaco-editor .view-lines').first();
const chipOf = (page: Page, name: string) => page.locator('.query-parameter-panel-card .param-chip-item', { hasText: name });

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
        // Trường đã khai: speed có trong SQL, message thì SQL không có — bảng so khớp không được tạo dòng cho message
        await page.route('**/api/sharedata/sharedatapacketfield/page*', (r) =>
            r.fulfill(
                json({
                    code: 200,
                    result: {
                        records: [
                            { id: 'F1', datatypeId: 'PKT_01', aliasFieldKey: 'speed', name: 'Tốc độ', fieldType: 'int', status: 1, isRequired: 0 },
                            { id: 'F2', datatypeId: 'PKT_01', aliasFieldKey: 'message', name: 'Thông tin trạng thái', fieldType: 'string', status: 1, isRequired: 1 },
                        ],
                        total: 2,
                    },
                })
            )
        );
        await page.route('**/api/sharedata/sharedatapacketfield/list*', (r) =>
            r.fulfill(
                json({
                    code: 200,
                    result: [
                        { id: 'F1', datatypeId: 'PKT_01', aliasFieldKey: 'speed', name: 'Tốc độ', fieldType: 'int', status: 1, isRequired: 0 },
                        { id: 'F2', datatypeId: 'PKT_01', aliasFieldKey: 'message', name: 'Thông tin trạng thái', fieldType: 'string', status: 1, isRequired: 1 },
                    ],
                })
            )
        );
        await page.route('**/api/sharedata/sharedatapacket/isinuse*', (r) => r.fulfill(json({ code: 200, result: false })));
        await page.route('**/api/sharedata/sharedatamapping/page*', (r) => r.fulfill(json({ code: 200, result: { records: [], total: 0 } })));
        await page.route('**/api/sharedata/sharedatapacket/getextractsql*', (r) => r.fulfill(json({ code: 200, result: { extractSql: SQL_ONE_COLUMN, status: 1, syncMode: 1, maxLookBackDays: null } })));
        await page.route('**/api/sharedata/sharedatapacket/parseextractcolumns*', (r) => r.fulfill(json({ code: 200, result: { isValid: true, columns: ['speed'] } })));
        await page.route('**/api/sharedata/sharedatapacket/getmetadatatables*', (r) => r.fulfill(json({ code: 200, result: [{ tableName: 'TmsTrafficData' }, { tableName: 'TmsZone' }] })));
        await page.route('**/api/sharedata/sharedatapacket/getmetadatafields*', (r) =>
            r.fulfill(json({ code: 200, result: [{ columnName: 'Id', dataType: 'bigint' }, { columnName: 'Speed', dataType: 'int' }, { columnName: 'UpdateTime', dataType: 'datetime' }] }))
        );
    });

    test('1. Gõ SEL gợi ý từ khóa SELECT và các mẫu câu select-*', async ({ page }) => {
        await openSqlCenter(page);
        await retypeSql(page, 'SEL');
        await expect.poll(() => suggestionLabels(page), { timeout: 10_000 }).toEqual(expect.arrayContaining([expect.stringContaining('SELECT, Keyword'), expect.stringContaining('select-incremental, Snippet')]));
    });

    test('2. Gõ FROM Tms gợi ý tên bảng nguồn của gói tin', async ({ page }) => {
        await openSqlCenter(page);
        await retypeSql(page, 'SELECT * FROM Tms');
        await expect.poll(() => suggestionLabels(page), { timeout: 10_000 }).toEqual(expect.arrayContaining([expect.stringContaining('TmsZone, Class')]));
    });

    test('3. Bảng Trường trích xuất chỉ có cột của câu SQL, không có dòng cho trường SQL không trả về', async ({ page }) => {
        await openSqlCenter(page);
        const table = page.locator('.field-compare-table');
        await expect(table).toBeVisible({ timeout: 15_000 });
        await expect(table.locator('tbody tr')).toHaveCount(1);
        await expect(table).toContainText('speed');
        await expect(table).toContainText('Khớp');
        await expect(table).not.toContainText('message');
        await expect(page.locator('.field-compare-summary .summary-missing')).toHaveCount(0);
        await expect(page.locator('.field-compare-summary')).toContainText('Khớp 1/1');
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

    test('5. Biến hệ thống luôn hiện 3 biến, biến chưa dùng trong SQL hiện mờ; @fromDate, @toDate chỉ hiện khi SQL dùng', async ({ page }) => {
        await openSqlCenter(page);
        const chips = page.locator('.query-parameter-panel-card .param-chip-item');
        const unused = page.locator('.query-parameter-panel-card .param-chip-item.is-unused');
        // SQL mẫu chỉ dùng @pageSize: 3 biến vẫn hiện đủ, 2 biến còn lại mờ
        await expect(chips).toHaveCount(3);
        await expect(chips.locator('.param-name-code')).toHaveText(['@pageSize', '@lastTime', '@lastKey']);
        await expect(unused).toHaveCount(2);

        // SQL không dùng biến nào: bảng không biến mất, cả 3 biến đều mờ
        await retypeSql(page, 'SELECT 1');
        await expect(chips).toHaveCount(3);
        await expect(unused).toHaveCount(3, { timeout: 10_000 });

        // SQL dùng @fromDate: thêm một thẻ xem ý nghĩa
        await retypeSql(page, 'SELECT 1 WHERE @fromDate IS NULL', 30);
        await expect(chips).toHaveCount(4, { timeout: 10_000 });
        await expect(chipOf(page, '@fromDate')).toBeVisible();
    });

    test('6. Bấm biến khi câu SQL còn dở dang chỉ chèn tên biến vào vị trí con trỏ', async ({ page }) => {
        await openSqlCenter(page);
        await retypeSql(page, 'SELECT ');
        await chipOf(page, '@lastKey').click();
        await expect(editorText(page)).toContainText('SELECT @lastKey');
        // Biến sau khi chèn đã dùng trong SQL nên hết mờ
        await expect(chipOf(page, '@lastKey')).not.toHaveClass(/is-unused/, { timeout: 10_000 });
    });

    test('7. Bấm biến khi khung SQL trống chèn mẫu câu có ô điền, chỉ chứa đúng biến được bấm', async ({ page }) => {
        await openSqlCenter(page);
        await retypeSql(page, '');
        await chipOf(page, '@lastTime').click();
        await expect(editorText(page)).toContainText('WHERE');
        await expect(editorText(page)).toContainText('> @lastTime');
        await expect(editorText(page)).not.toContainText('@lastKey');
        await expect(editorText(page)).not.toContainText('TOP (@pageSize)');
        await expect(editorText(page)).not.toContainText('__lastTime');
    });

    test('8. Bấm @lastTime khi đã có SELECT ... FROM chỉ thêm điều kiện theo cột UpdateTime vào WHERE', async ({ page }) => {
        await openSqlCenter(page);
        await retypeSql(page, 'SELECT td.Speed AS speed FROM TmsTrafficData td', 30);
        await chipOf(page, '@lastTime').click();
        const text = editorText(page);
        await expect(text).toContainText('WHERE td.UpdateTime > @lastTime', { timeout: 15_000 });
        await expect(text).not.toContainText('@lastKey');
        await expect(text).not.toContainText('TOP (@pageSize)');
        await expect(text).not.toContainText('ORDER BY');
    });

    test('9. Bấm @lastKey chỉ thêm điều kiện theo cột Id; bấm @pageSize chỉ thêm TOP (@pageSize) và con trỏ nằm ngay sau đó', async ({ page }) => {
        await openSqlCenter(page);
        await retypeSql(page, 'SELECT td.Speed AS speed FROM TmsTrafficData td', 30);
        await chipOf(page, '@lastKey').click();
        await expect(editorText(page)).toContainText('WHERE td.Id > @lastKey', { timeout: 15_000 });
        await expect(editorText(page)).not.toContainText('@lastTime');

        await chipOf(page, '@pageSize').click();
        await expect(editorText(page)).toContainText('SELECT TOP (@pageSize) td.Speed AS speed', { timeout: 15_000 });
        // Con trỏ nằm cuối "SELECT TOP (@pageSize)|": gõ thêm một ký tự thì ký tự đó phải dính ngay sau dấu ngoặc đóng
        await page.keyboard.type('Z');
        await expect(editorText(page)).toContainText('SELECT TOP (@pageSize)Z');
        await page.keyboard.press('Backspace');

        // Bấm lại biến đã có trong câu thì không thêm lần hai
        await chipOf(page, '@pageSize').click();
        await expect(editorText(page)).not.toContainText('TOP (@pageSize) TOP');
    });

    test('10. Thanh Biến hệ thống dính ở đầu vùng cuộn, không trôi mất khi cuộn xuống', async ({ page }) => {
        await openSqlCenter(page);
        await page.setViewportSize({ width: 1600, height: 480 });
        await page.waitForTimeout(500);
        const scroller = page.locator('.sql-editor-section');
        const overflow = await scroller.evaluate((el) => el.scrollHeight - el.clientHeight);
        expect(overflow, 'vùng cuộn phải có nội dung dài hơn khung thì mới kiểm được').toBeGreaterThan(20);
        await scroller.evaluate((el) => {
            el.scrollTop = el.scrollHeight;
        });
        await page.waitForTimeout(300);
        const chip = await chipOf(page, '@pageSize').boundingBox();
        const box = await scroller.boundingBox();
        expect(chip!.y).toBeGreaterThanOrEqual(box!.y - 1);
        expect(chip!.y - box!.y).toBeLessThan(40);
    });

    test('11. Đối tác chưa có SQL riêng: "Tạo SQL riêng" bấm được ngay, lưu đúng nội dung mẫu chung', async ({ page }) => {
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
        await expect(create).toContainText('Tạo SQL riêng');
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
        await expect(save).toContainText('Lưu SQL riêng');
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
