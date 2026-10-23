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
async function retypeSql(page: Page, text: string) {
    await page.locator('.sql-monaco-editor .view-lines').first().click();
    await page.keyboard.press('End');
    for (let i = 0; i < 160; i++) await page.keyboard.press('Backspace');
    await page.keyboard.type(text, { delay: 150 });
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
            r.fulfill(json({ code: 200, result: [{ columnName: 'Speed', dataType: 'int' }, { columnName: 'UpdateTime', dataType: 'datetime' }] }))
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

    test('5. Chuỗi ngôn ngữ chứa "@" phải viết {\'@\'} để vue-i18n không báo Invalid linked format', async () => {
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
