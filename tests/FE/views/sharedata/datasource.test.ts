import { test, expect } from '@playwright/test';
import { mockAuth } from '../../setup';
import fs from 'node:fs';
import path from 'node:path';

/**
 * Test E2E cho phân hệ ShareData — Quản lý Gói tin & Trường gói tin (dataSource/index.vue)
 * Kiểm thử bố cục đáp ứng (responsive layout) của search card "Trường gói tin" (.search-card-field):
 * - Bỏ mọi timeout dài, chỉ dùng timeout mặc định nhanh gọn.
 * - Kiểm tra Khóa field không bị chiếm 50% bề ngang card; cả 3 ô và cụm nút nằm gọn trên 1 hàng trên màn hình rộng.
 * - Kiểm tra container query xếp dọc khi màn hình hẹp (< 600px).
 * - Quét tĩnh mã nguồn CSS đảm bảo không có !important.
 */

test.describe('ShareData — Layout tìm kiếm Trường gói tin (dataSource) @sharedata', () => {

    test.beforeEach(async ({ context, page }) => {
        await mockAuth(context, page, {
            userInfo: {
                defaultMenu: '/sharedata/dataSource',
            },
        });

        // 2. Mock API Menu Tree cho Dynamic Router
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
                                    id: 'menu_sharedata_datasource',
                                    pid: 'menu_sharedata',
                                    path: '/sharedata/dataSource',
                                    name: 'sharedataDataSource',
                                    component: 'sharedata/dataSource/index',
                                    meta: { title: 'lz.menu.sharedataDataSource', isLink: '', isHide: false, isKeepAlive: true, isAffix: false, isIframe: false, roles: ['admin'], icon: 'ele-Coin' },
                                },
                            ],
                        },
                    ],
                }),
            });
        });

        // 3. Mock API danh mục gói tin & trường gói tin
        await page.route('**/api/sharedata/sharedatapacket/page*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        records: [
                            { id: 'PKT_01', code: '101_commonData', name: 'Gói tin dùng chung 101', status: 1 },
                        ],
                        total: 1,
                    },
                }),
            });
        });

        await page.route('**/api/sharedata/sharedatapacketfield/page*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        records: [
                            { id: 'FLD_01', datatypeId: 'PKT_01', aliasFieldKey: 'header.status', fieldType: 'int', status: 1, isRequired: 1 },
                        ],
                        total: 1,
                    },
                }),
            });
        });

        await page.route('**/api/sharedata/sharedatapacket/isinuse*', async (route) => {
            await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ code: 200, result: false }) });
        });

        await page.route('**/api/sharedata/sharedatamapping/page*', async (route) => {
            await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ code: 200, result: { records: [], total: 0 } }) });
        });
        await page.route('**/api/sharedata/sharedatasubscription/list*', async (route) => {
            await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ code: 200, result: [] }) });
        });
        await page.route('**/api/sysDictData/dataList*', async (route) => {
            await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ code: 200, result: [] }) });
        });
        await page.route('**/api/sysConst/list*', async (route) => {
            await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ code: 200, result: [] }) });
        });
    });

    test('1. Kiểm tra bố cục đáp ứng card Trường gói tin qua 3 cấp độ màn hình (>1400px, 1100px-1400px, <1100px)', async ({ page }) => {
        // Cấp độ 1: Màn hình rộng > 1400px (1600x900) -> 1 hàng ngang duy nhất
        await page.setViewportSize({ width: 1600, height: 900 });
        await page.goto('/sharedata/dataSource');

        const cardField = page.locator('.search-card-field');
        await expect(cardField).toBeVisible({ timeout: 30_000 });

        const searchForm = cardField.locator('.search-form');
        const formCols = searchForm.locator('.el-row > .el-col');
        await expect(formCols).toHaveCount(4);

        const colAlias = cardField.locator('.field-col-alias');
        const colType = cardField.locator('.field-col-type');
        const colStatus = cardField.locator('.field-col-status');
        const colBtn = cardField.locator('.field-col-btn');

        const boxAlias1 = await colAlias.boundingBox();
        const boxType1 = await colType.boundingBox();
        const boxStatus1 = await colStatus.boundingBox();
        const boxBtn1 = await colBtn.boundingBox();

        expect(boxAlias1).not.toBeNull();
        expect(boxType1).not.toBeNull();
        expect(boxStatus1).not.toBeNull();
        expect(boxBtn1).not.toBeNull();

        // Cả 4 cột nằm trên cùng 1 hàng ngang (chênh lệch Y <= 5px)
        expect(Math.abs(boxAlias1!.y - boxType1!.y)).toBeLessThanOrEqual(5);
        expect(Math.abs(boxType1!.y - boxStatus1!.y)).toBeLessThanOrEqual(5);
        expect(Math.abs(boxStatus1!.y - boxBtn1!.y)).toBeLessThanOrEqual(5);

        // Divider hiển thị ngăn cách ô Trạng thái và cụm nút
        const divider = colBtn.locator('.search-divider');
        await expect(divider).toBeVisible();

        // Cấp độ 2: 1100px - 1400px (ví dụ 1200px) -> Nằm trên 2 hàng (2x2)
        await page.setViewportSize({ width: 1200, height: 900 });
        await page.waitForTimeout(300);

        const boxAlias2 = await colAlias.boundingBox();
        const boxType2 = await colType.boundingBox();
        const boxStatus2 = await colStatus.boundingBox();
        const boxBtn2 = await colBtn.boundingBox();

        // Hàng 1: Khóa field + Kiểu
        expect(Math.abs(boxAlias2!.y - boxType2!.y)).toBeLessThanOrEqual(5);

        // Hàng 2: Trạng thái + Nút
        expect(Math.abs(boxStatus2!.y - boxBtn2!.y)).toBeLessThanOrEqual(5);

        // Hàng 2 nằm dưới Hàng 1
        expect(boxStatus2!.y).toBeGreaterThan(boxAlias2!.y + 20);

        // Mỗi cột chiếm ~50% hàng
        const rowBox2 = await searchForm.locator('.el-row').first().boundingBox();
        expect(rowBox2).not.toBeNull();
        expect(boxAlias2!.width / rowBox2!.width).toBeGreaterThan(0.45);
        expect(boxAlias2!.width / rowBox2!.width).toBeLessThan(0.55);

        // Divider ẩn đi trên 2 hàng
        await expect(divider).toBeHidden();

        // Cấp độ 3: Dưới 1100px (ví dụ 1000px) -> Xuống hàng hết (xếp dọc 4 dòng)
        await page.setViewportSize({ width: 1000, height: 900 });
        await page.waitForTimeout(300);

        const boxAlias3 = await colAlias.boundingBox();
        const boxType3 = await colType.boundingBox();
        const boxStatus3 = await colStatus.boundingBox();
        const boxBtn3 = await colBtn.boundingBox();

        // Mỗi ô xếp trên 1 hàng dọc riêng biệt
        expect(boxType3!.y).toBeGreaterThan(boxAlias3!.y + 20);
        expect(boxStatus3!.y).toBeGreaterThan(boxType3!.y + 20);
        expect(boxBtn3!.y).toBeGreaterThan(boxStatus3!.y + 20);

        // Mỗi cột chiếm gần như 100% hàng
        const rowBox3 = await searchForm.locator('.el-row').first().boundingBox();
        expect(rowBox3).not.toBeNull();
        expect(boxAlias3!.width / rowBox3!.width).toBeGreaterThan(0.90);
    });

    test('2. Không có bất kỳ luật !important nào trong toàn bộ CSS/SCSS của phân hệ ShareData', async () => {
        const sharedataDir = fs.existsSync(path.resolve(process.cwd(), 'TA-ITS015-WEBVUE-V1.0/src/src/views/sharedata'))
            ? path.resolve(process.cwd(), 'TA-ITS015-WEBVUE-V1.0/src/src/views/sharedata')
            : path.resolve(process.cwd(), '../../TA-ITS015-WEBVUE-V1.0/src/src/views/sharedata');

        expect(fs.existsSync(sharedataDir)).toBeTruthy();

        const files: string[] = [];
        function scanDir(dir: string) {
            for (const item of fs.readdirSync(dir)) {
                const fullPath = path.join(dir, item);
                if (fs.statSync(fullPath).isDirectory()) {
                    scanDir(fullPath);
                } else if (fullPath.endsWith('.vue') || fullPath.endsWith('.scss') || fullPath.endsWith('.css')) {
                    files.push(fullPath);
                }
            }
        }

        scanDir(sharedataDir);
        const filesWithImportant: string[] = [];
        for (const file of files) {
            const content = fs.readFileSync(file, 'utf-8');
            if (content.includes('!important')) {
                filesWithImportant.push(file);
            }
        }

        expect(filesWithImportant, `Phát hiện !important tại: ${filesWithImportant.join(', ')}`).toEqual([]);
    });

    test('3. Bản thử R5 (Cấu hình nguồn dữ liệu 3 bước): kiểm chứng toàn diện lọc VDS/Thời tiết, ẩn chi tiết kỹ thuật, xác thực điều kiện, đa ngôn ngữ VI/EN và màn hình hẹp', async ({ page }) => {
        const consoleErrors: string[] = [];
        const pageErrors: string[] = [];
        page.on('console', (msg) => {
            if (msg.type() === 'error') consoleErrors.push(msg.text());
        });
        page.on('pageerror', (err) => {
            pageErrors.push(err.message);
        });

        await page.setViewportSize({ width: 1440, height: 900 });
        await page.goto('/sharedata-query-prototype.html');

        const dialog = page.locator('.r5-query-prototype-dialog');
        await expect(dialog).toBeVisible();
        await expect(page.locator('.prototype-badge')).toBeVisible();

        // 1. Mặc định ẩn Chi tiết kỹ thuật: không xuất hiện tên bảng/SQL/khóa kỹ thuật trong luồng chính
        await expect(dialog.locator('.source-meta-strip')).toHaveCount(0);
        await expect(dialog.locator('.field-key-code')).toHaveCount(0);
        await expect(dialog).not.toContainText('TmsTrafficData');
        await expect(dialog).not.toContainText('SELECT TOP');

        // Kiểm tra cả tab Danh sách cột gói tin khi Chi tiết kỹ thuật tắt
        await dialog.locator('.el-tabs__item').filter({ hasText: 'Danh sách cột gói tin' }).click();
        await expect(dialog.locator('.preview-columns-table')).toBeVisible();
        await expect(dialog.locator('.preview-columns-table')).not.toContainText('td.ID');
        await dialog.locator('.el-tabs__item').filter({ hasText: 'Dữ liệu xem trước' }).click();

        // 2. Nguồn VDS với điều kiện mặc định: Tất cả = 1 dòng, Bất kỳ = 5 dòng
        const previewTableRows = dialog.locator('.preview-data-table .el-table__body-wrapper tbody tr');
        await expect(previewTableRows).toHaveCount(1);

        const matchAllRadio = dialog.locator('.match-mode-group .el-radio-button').nth(0);
        const matchAnyRadio = dialog.locator('.match-mode-group .el-radio-button').nth(1);
        await matchAnyRadio.click();
        await expect(previewTableRows).toHaveCount(5);

        // 3. Bỏ hết trường -> chặn xác nhận; chọn lại trường bắt buộc -> mở khóa xác nhận
        const confirmBtn = page.locator('.btn-apply-prototype');
        while ((await dialog.locator('.field-checklist .el-checkbox.is-checked').count()) > 0) {
            await dialog.locator('.field-checklist .el-checkbox.is-checked').first().click();
        }
        await expect(confirmBtn).toBeDisabled();
        await dialog.locator('.btn-select-required-fields').click();
        await expect(dialog.locator('.summary-fields-tag')).toContainText('5/9');
        await expect(confirmBtn).toBeEnabled();

        // 4. Đổi nguồn sang Thời tiết: đặt lại trường (8/8) và điều kiện mặc định (Tất cả = 2 dòng, Bất kỳ = 3 dòng)
        await dialog.locator('.source-select').click();
        await page.locator('.el-select-dropdown__item:visible').filter({ hasText: 'Quan trắc thời tiết trạm ITS' }).first().click();
        await expect(dialog.locator('.summary-fields-tag')).toContainText('8/8');
        await expect(previewTableRows).toHaveCount(2);
        await matchAnyRadio.click();
        await expect(previewTableRows).toHaveCount(3);
        await matchAllRadio.click();
        await expect(previewTableRows).toHaveCount(2);

        // 5. Đổi trường trong dòng lọc -> đặt lại toán tử/giá trị cũ và báo thiếu giá trị; tắt điều kiện đó -> không áp dụng
        const firstRule = dialog.locator('.condition-row').first();
        await firstRule.locator('.rule-field-select').click();
        await page.locator('.el-select-dropdown__item:visible').filter({ hasText: 'Vị trí đặt trạm' }).first().click();
        await expect(firstRule.locator('.rule-value-input input')).toHaveValue('');
        await expect(confirmBtn).toBeDisabled();

        // Tắt điều kiện vừa đổi (đang trống giá trị) -> hết lỗi, còn 1 điều kiện (visibility <= 400) khớp 3 dòng
        await firstRule.locator('.rule-enable-switch').click();
        await expect(confirmBtn).toBeEnabled();
        await expect(previewTableRows).toHaveCount(3);

        // 6. Xóa hết điều kiện -> lấy toàn bộ 4 dòng mẫu; giảm giới hạn xem trước xuống 2 -> còn 2 dòng
        while ((await dialog.locator('.btn-remove-condition').count()) > 0) {
            await dialog.locator('.btn-remove-condition').first().click();
        }
        await expect(dialog.locator('.empty-condition-hint')).toBeVisible();
        await expect(previewTableRows).toHaveCount(4);

        const limitInput = dialog.locator('.preview-limit-input input');
        await limitInput.fill('2');
        await limitInput.press('Enter');
        await expect(previewTableRows).toHaveCount(2);

        // 7. Mở Chi tiết kỹ thuật -> hiển thị bảng nguồn, khóa trường và tab SQL tham số hóa
        await dialog.locator('.el-checkbox').filter({ hasText: 'Chi tiết kỹ thuật' }).click();
        await expect(dialog.locator('.source-meta-strip')).toContainText('TmsWeather w');
        await dialog.locator('.el-tabs__item').filter({ hasText: 'Truy vấn & Cấu hình sinh tự động' }).click();
        await expect(dialog.locator('.generated-sql-block')).toContainText('SELECT TOP (2)');

        // 8. Đóng/mở lại dialog và kiểm tra màn hình hẹp (768x900)
        await page.locator('.prototype-dialog-footer .el-button').first().click();
        await expect(dialog).toBeHidden();
        await page.locator('.r5-preview-open').click();
        await expect(dialog).toBeVisible();

        await page.setViewportSize({ width: 768, height: 900 });
        const configColBox = await dialog.locator('.config-column').boundingBox();
        const previewColBox = await dialog.locator('.preview-column').boundingBox();
        expect(configColBox).not.toBeNull();
        expect(previewColBox).not.toBeNull();
        expect(previewColBox!.y).toBeGreaterThan(configColBox!.y + 100);

        // 9. Kiểm tra tiếng Anh (en-us)
        await page.setViewportSize({ width: 1440, height: 900 });
        await page.goto('/sharedata-query-prototype.html?lang=en-us');
        await expect(dialog).toBeVisible();
        await expect(page.locator('.header-title-text')).toHaveText('Configure Packet Data Source');
        await expect(dialog).toContainText('Step 1: Select Data');
        await expect(dialog).toContainText('All conditions');
        await expect(dialog).not.toContainText('lz.');

        expect(consoleErrors).toEqual([]);
        expect(pageErrors).toEqual([]);
    });
});
