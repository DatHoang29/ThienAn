import { test, expect } from '@playwright/test';
import { mockAuth } from '../../setup';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { parsePacketNumber, resolvePacketPolicy } from '../../../../TA-ITS015-WEBVUE-V1.0/src/src/utils/sharedata/packetPolicy';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

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
        await page.goto('/#/sharedata/dataSource');

        const cardField = page.locator('.search-card-field');
        await expect(cardField).toBeVisible({ timeout: 30_000 });
        await page.waitForTimeout(300);

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
        await expect.poll(async () => {
            const bAlias = await colAlias.boundingBox();
            const bType = await colType.boundingBox();
            const bStatus = await colStatus.boundingBox();
            const bBtn = await colBtn.boundingBox();
            if (!bAlias || !bType || !bStatus || !bBtn) return 999;
            return Math.max(
                Math.abs(bAlias.y - bType.y),
                Math.abs(bType.y - bStatus.y),
                Math.abs(bStatus.y - bBtn.y)
            );
        }, { timeout: 5000 }).toBeLessThanOrEqual(5);

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

    test.skip('3. Bản thử R5 (Cấu hình nguồn dữ liệu 3 bước): kiểm chứng toàn diện lọc VDS/Thời tiết, ẩn chi tiết kỹ thuật, xác thực điều kiện, đa ngôn ngữ VI/EN và màn hình hẹp', async ({ page }) => {
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

    test('4. Kiểm tra tính năng kéo dãn độ rộng cột (Column Resizing) trên bảng Gói tin và bảng Trường gói tin', async ({ page }) => {
        await page.setViewportSize({ width: 1600, height: 900 });
        await page.goto('/#/sharedata/dataSource');

        // Chờ bảng gói tin hiển thị
        const packetGrid = page.locator('.packet-col .vxe-grid');
        await expect(packetGrid).toBeVisible({ timeout: 15_000 });

        // 1. Kiểm tra bảng Gói tin có các cột resizable (.vxe-resizable)
        const packetResizers = packetGrid.locator('.vxe-table--header-wrapper .vxe-header--column .vxe-resizable');
        await expect(packetResizers.first()).toBeAttached();
        const packetResizerCount = await packetResizers.count();
        expect(packetResizerCount).toBeGreaterThan(0);

        // Đảm bảo cột có resizer đã ổn định vị trí trên màn hình (y < 400)
        const targetCol = packetGrid.locator('.vxe-table--header-wrapper .vxe-header--column:has(.vxe-resizable):visible').first();
        await expect(async () => {
            const box = await targetCol.boundingBox();
            expect(box).not.toBeNull();
            expect(box!.y).toBeLessThan(400);
        }).toPass({ timeout: 5000 });

        const targetResizer = targetCol.locator('.vxe-resizable');
        const initialBox = await targetCol.boundingBox();
        expect(initialBox).not.toBeNull();
        const initialWidth = initialBox!.width;

        const resizerBox = await targetResizer.boundingBox();
        expect(resizerBox).not.toBeNull();

        const startX = resizerBox!.x + resizerBox!.width / 2;
        const startY = resizerBox!.y + resizerBox!.height / 2;
        await page.mouse.move(startX, startY);
        await page.mouse.down();
        await page.mouse.move(startX + 80, startY, { steps: 20 });
        await page.waitForTimeout(300);
        await page.mouse.up();
        await page.waitForTimeout(500);

        // Kiểm tra độ rộng cột đã thay đổi (lớn hơn ban đầu)
        const updatedBox = await targetCol.boundingBox();
        expect(updatedBox).not.toBeNull();
        expect(updatedBox!.width).toBeGreaterThan(initialWidth + 20);

        // 2. Chuyển sang tab Trường đã lưu (savedFields) và kiểm tra bảng Trường gói tin
        const savedFieldsBtn = page.locator('.packet-header-tabs-switch .el-radio-button').filter({ hasText: 'Trường' });
        if (await savedFieldsBtn.isVisible()) {
            await savedFieldsBtn.click();
        }

        const fieldGrid = page.locator('.field-col .vxe-grid');
        await expect(fieldGrid).toBeVisible({ timeout: 10_000 });

        const fieldResizers = fieldGrid.locator('.vxe-table--header-wrapper .vxe-header--column .vxe-resizable');
        await expect(fieldResizers.first()).toBeAttached();
        const fieldResizerCount = await fieldResizers.count();
        expect(fieldResizerCount).toBeGreaterThan(0);

        // Kéo dãn cột trên bảng Trường gói tin (chờ layout ổn định)
        const targetFieldCol = fieldGrid.locator('.vxe-table--header-wrapper .vxe-header--column:has(.vxe-resizable):visible').first();
        await expect(async () => {
            const box = await targetFieldCol.boundingBox();
            expect(box).not.toBeNull();
            expect(box!.y).toBeLessThan(400);
        }).toPass({ timeout: 5000 });

        const targetFieldResizer = targetFieldCol.locator('.vxe-resizable');
        const initialFieldBox = await targetFieldCol.boundingBox();
        expect(initialFieldBox).not.toBeNull();
        const initialFieldWidth = initialFieldBox!.width;

        const fieldResizerBox = await targetFieldResizer.boundingBox();
        expect(fieldResizerBox).not.toBeNull();

        const fieldStartX = fieldResizerBox!.x + fieldResizerBox!.width / 2;
        const fieldStartY = fieldResizerBox!.y + fieldResizerBox!.height / 2;
        await page.mouse.move(fieldStartX, fieldStartY);
        await page.mouse.down();
        await page.mouse.move(fieldStartX + 60, fieldStartY, { steps: 20 });
        await page.waitForTimeout(300);
        await page.mouse.up();
        await page.waitForTimeout(500);

        const updatedFieldBox = await targetFieldCol.boundingBox();
        expect(updatedFieldBox).not.toBeNull();
        expect(updatedFieldBox!.width).toBeGreaterThan(initialFieldWidth + 20);
    });

    test('5. Kiểm tra thanh phân cách Splitpanes giữa 2 bên (Gói tin và Chi tiết/Trường)', async ({ page }) => {
        await page.setViewportSize({ width: 1600, height: 900 });
        await page.goto('/#/sharedata/dataSource');

        const splitpanes = page.locator('.datasource-splitpanes');
        await expect(splitpanes).toBeVisible({ timeout: 15_000 });

        const splitter = page.locator('.splitpanes__splitter');
        await expect(splitter).toBeVisible();

        // Đảm bảo splitter đã ổn định vị trí trên màn hình (y < 200)
        await expect(async () => {
            const box = await splitter.boundingBox();
            expect(box).not.toBeNull();
            expect(box!.y).toBeLessThan(200);
        }).toPass({ timeout: 5000 });

        const leftPane = page.locator('.packet-pane');
        const initialBox = await leftPane.boundingBox();
        expect(initialBox).not.toBeNull();
        const initialWidth = initialBox!.width;

        const splitterBox = await splitter.boundingBox();
        expect(splitterBox).not.toBeNull();

        // Kéo thanh phân cách sang phải 120px
        const startX = splitterBox!.x + splitterBox!.width / 2;
        const startY = splitterBox!.y + 100;
        await page.mouse.move(startX, startY);
        await page.mouse.down();
        await page.mouse.move(startX + 120, startY, { steps: 20 });
        await page.waitForTimeout(300);
        await page.mouse.up();
        await page.waitForTimeout(300);

        const updatedBox = await leftPane.boundingBox();
        expect(updatedBox).not.toBeNull();
        expect(updatedBox!.width).toBeGreaterThan(initialWidth + 40);
    });

    test('6. Quét tĩnh & kiểm thử VisualQueryBuilder.vue: Dải xem trước SQL Tự động sinh dùng font chữ chuẩn và i18n đầy đủ', async () => {
        const vqbFilePath = path.resolve(
            __dirname,
            '../../../../TA-ITS015-WEBVUE-V1.0/src/src/views/sharedata/dataSource/component/visualQueryBuilder.vue'
        );
        const viPath = path.resolve(
            __dirname,
            '../../../../TA-ITS015-WEBVUE-V1.0/src/src/i18n/lang/vi-vn.json'
        );
        const enPath = path.resolve(
            __dirname,
            '../../../../TA-ITS015-WEBVUE-V1.0/src/src/i18n/lang/en-us.json'
        );

        const vqbContent = fs.readFileSync(vqbFilePath, 'utf8');
        const viJson = JSON.parse(fs.readFileSync(viPath, 'utf8'));
        const enJson = JSON.parse(fs.readFileSync(enPath, 'utf8'));

        // 1. Kiểm tra key i18n đầy đủ cho nhãn và placeholder
        expect(viJson['lz.label.sharedataDataSource.vqbGeneratedSql']).toBe('SQL Tự động sinh:');
        expect(viJson['lz.label.sharedataDataSource.vqbNoSqlConfigured']).toBe('-- Chưa cấu hình câu truy vấn --');

        expect(enJson['lz.label.sharedataDataSource.vqbGeneratedSql']).toBe('Auto-generated SQL:');
        expect(enJson['lz.label.sharedataDataSource.vqbNoSqlConfigured']).toBe('-- Query not configured --');

        // 2. Kiểm tra template dùng $t() và không hardcode chuỗi text
        expect(vqbContent).toContain("$t('lz.label.sharedataDataSource.vqbGeneratedSql')");
        expect(vqbContent).toContain("$t('lz.label.sharedataDataSource.vqbNoSqlConfigured')");

        // 3. Kiểm tra font chữ chuẩn trong CSS
        // Nhãn & placeholder rỗng dùng font chữ hệ thống chuẩn var(--el-font-family)
        expect(vqbContent).toContain('font-family: var(--el-font-family);');
        // Mã SQL dùng font chữ code chuẩn Consolas, Monaco, "Cascadia Mono", monospace
        expect(vqbContent).toContain('font-family: Consolas, Monaco, "Cascadia Mono", monospace;');

        // 4. Tuân thủ Rule 20.6 (CẤM !important)
        const stripCssBlock = vqbContent.substring(vqbContent.indexOf('.vqb-sql-preview-strip'));
        expect(stripCssBlock).not.toContain('!important');

        // 5. Kiểm tra phân tách trạng thái: có SQL hiện code, rỗng hiện placeholder nghiêng
        expect(vqbContent).toContain('vqb-sql-code');
        expect(vqbContent).toContain('vqb-sql-empty');

        // 6. Hiển thị Full SQL: Không bị cắt bớt bằng ellipsis, hỗ trợ xuống dòng pre-wrap & word-break
        expect(stripCssBlock).not.toContain('text-overflow: ellipsis');
        expect(stripCssBlock).toContain('white-space: pre-wrap');
        expect(stripCssBlock).toContain('word-break: break-all');
        expect(vqbContent).toContain('vqb-copy-btn');
    });

    test('7. Chống giật khi chuyển tab Cấu hình SQL <-> Danh sách trường: Loại bỏ border ẩn/hiện động của border-card', async ({ page }) => {
        const vueFilePath = path.resolve(
            __dirname,
            '../../../../TA-ITS015-WEBVUE-V1.0/src/src/views/sharedata/dataSource/index.vue'
        );
        const vueContent = fs.readFileSync(vueFilePath, 'utf8');

        // 1. Quét tĩnh CSS: kiểm tra .datasource-right-tabs dùng native active-bar chuẩn TMS, không dùng scaleX
        const rightTabsCss = vueContent.substring(vueContent.indexOf('.datasource-right-tabs'));
        expect(rightTabsCss).toContain('.el-tabs__active-bar');
        expect(rightTabsCss).not.toContain('scaleX');
        expect(rightTabsCss).not.toContain('!important');

        // 2. Render DOM thực tế: Mở màn hình dataSource
        await page.setViewportSize({ width: 1600, height: 900 });
        await page.goto('/#/sharedata/dataSource');

        const tabs = page.locator('.datasource-right-tabs');
        await expect(tabs).toBeVisible({ timeout: 15_000 });

        const sqlTab = tabs.getByRole('tab', { name: /Cấu hình SQL/ });
        const fieldsTab = tabs.getByRole('tab', { name: /Danh sách trường/ });
        await expect(sqlTab).toBeVisible();
        await expect(fieldsTab).toBeVisible();

        // 3. Tab Danh sách trường đã lưu BẮT BUỘC nằm ở vị trí đầu tiên (trái hơn Cấu hình SQL) và active mặc định
        const fieldsBox = await fieldsTab.boundingBox();
        const sqlBox = await sqlTab.boundingBox();
        expect(fieldsBox).not.toBeNull();
        expect(sqlBox).not.toBeNull();
        expect(fieldsBox!.x).toBeLessThan(sqlBox!.x);
        await expect(fieldsTab).toHaveClass(/is-active/);

        // 4. Kiểm tra thanh gạch dưới active-bar chuẩn TMS: di chuyển mượt mà (translateX) khi đổi tab
        const activeBar = tabs.locator('.el-tabs__active-bar');
        await expect(activeBar).toBeVisible();

        const initialBarBox = await activeBar.boundingBox();
        expect(initialBarBox).not.toBeNull();

        // 5. Chuyển sang sqlTab: active-bar di chuyển (move) sang vị trí của sqlTab
        await sqlTab.click();
        await expect(sqlTab).toHaveClass(/is-active/);
        await page.waitForTimeout(300);
        const movedBarBox = await activeBar.boundingBox();
        expect(movedBarBox).not.toBeNull();
        expect(movedBarBox!.x).toBeGreaterThan(initialBarBox!.x + 20);

        // 6. Chuyển lại fieldsTab: active-bar di chuyển ngược lại khớp toạ độ ban đầu
        await fieldsTab.click();
        await expect(fieldsTab).toHaveClass(/is-active/);
        await page.waitForTimeout(300);
        const backBarBox = await activeBar.boundingBox();
        expect(backBarBox).not.toBeNull();
        expect(Math.abs(backBarBox!.x - initialBarBox!.x)).toBeLessThanOrEqual(1);

        // 6. Kiểm tra ngắn gọn nhãn tab thành 'Cấu hình SQL' (bỏ & Dò cột) và bỏ title trùng lặp bên dưới
        expect(vueContent).not.toContain('class="sql-toolbar-left"');
        expect(vueContent).not.toContain("{{ $t('lz.label.sharedataDataSource.extractSqlConfig') }}");
        const viPath = path.resolve(__dirname, '../../../../TA-ITS015-WEBVUE-V1.0/src/src/i18n/lang/vi-vn.json');
        const enPath = path.resolve(__dirname, '../../../../TA-ITS015-WEBVUE-V1.0/src/src/i18n/lang/en-us.json');
        const viJson = JSON.parse(fs.readFileSync(viPath, 'utf8'));
        const enJson = JSON.parse(fs.readFileSync(enPath, 'utf8'));
        expect(viJson['lz.label.sharedataDataSource.tabSqlCenter']).toBe('Cấu hình SQL');
        expect(enJson['lz.label.sharedataDataSource.tabSqlCenter']).toBe('SQL Configuration');
        await expect(sqlTab).toContainText('Cấu hình SQL');
        await expect(sqlTab).not.toContainText('Dò cột');
    });

    test('8. Kéo thả resize height cho các table con (danh sách cột phát hiện & dữ liệu mẫu) trong tab Cấu hình SQL', async ({ page }) => {
        const vueFilePath = path.resolve(
            __dirname,
            '../../../../TA-ITS015-WEBVUE-V1.0/src/src/views/sharedata/dataSource/index.vue'
        );
        const vueContent = fs.readFileSync(vueFilePath, 'utf8');

        // 1. Quét tĩnh CSS & DOM code: kiểm tra sự hiện diện của thanh kéo ngang resizer (đã bỏ thanh bên dưới theo yêu cầu)
        expect(vueContent).toContain('table-resize-bar');
        expect(vueContent).toContain('detected-table-resizer-top');
        expect(vueContent).not.toContain('detected-table-resizer-bottom');
        expect(vueContent).toContain('sample-table-resizer');
        expect(vueContent).toContain('resizer-handle-line');
        expect(vueContent).toContain('cursor: row-resize');
        expect(vueContent).toContain('requestAnimationFrame');
        expect(vueContent).toContain('will-change: height');
        expect(vueContent).toContain('pointer-events: none');
        expect(vueContent).not.toContain('!important');

        // 2. Mock thêm API SQL Center để trả về dữ liệu mẫu và cột phát hiện
        await page.route('**/api/sharedata/sharedatapacket/parseextractcolumns*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        isValid: true,
                        columns: ['speed', 'vehicle_count', 'lane_id', 'recorded_time'],
                    },
                }),
            });
        });

        await page.route('**/api/sharedata/sharedatapacket/getextractsql*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        extractSql: 'SELECT TOP 10 td.Speed AS speed, td.Volume AS vehicle_count FROM TmsTrafficData td',
                        status: 1,
                        remark: 'SQL mặc định',
                    },
                }),
            });
        });

        await page.route('**/api/sharedata/sharedatapacket/executeextractsqlsample*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        isSuccess: true,
                        columns: ['speed', 'vehicle_count'],
                        rows: [
                            { speed: 60, vehicle_count: 12 },
                            { speed: 75, vehicle_count: 8 },
                        ],
                    },
                }),
            });
        });

        // 3. Mở trang dataSource
        await page.setViewportSize({ width: 1600, height: 900 });
        await page.goto('/#/sharedata/dataSource');

        // Chờ tabs hiển thị (tự động nạp gói tin đầu tiên)
        const tabs = page.locator('.datasource-right-tabs');
        await expect(tabs).toBeVisible({ timeout: 15_000 });

        // Chuyển sang tab Cấu hình SQL
        const sqlTab = tabs.getByRole('tab', { name: /Cấu hình SQL/ });
        await sqlTab.click();
        await expect(sqlTab).toHaveClass(/is-active/);

        // 4. Kiểm tra sự xuất hiện của thanh kéo trên và xác nhận đã bỏ thanh kéo dưới của bảng cột
        const topResizer = page.locator('.detected-table-resizer-top');
        await expect(topResizer).toBeVisible();
        await expect(page.locator('.detected-table-resizer-bottom')).toHaveCount(0);

        const tableWrapper = page.locator('.detected-columns-table-wrapper');
        await expect(tableWrapper).toBeVisible();
        await tableWrapper.scrollIntoViewIfNeeded();

        // Đo chiều cao ban đầu (mặc định 280px)
        const initialHeight = (await tableWrapper.boundingBox())?.height || 280;
        expect(initialHeight).toBeGreaterThanOrEqual(250);

        // 5. Thao tác kéo thả thanh resizer trên lên trên 80px để tăng chiều cao bảng VÀ đo đạc số khung hình thực tế (FPS)
        await topResizer.scrollIntoViewIfNeeded();
        const topBox = await topResizer.boundingBox();
        expect(topBox).not.toBeNull();

        // Kích hoạt đồng hồ đo khung hình (FPS & Frame Deltas) bằng requestAnimationFrame trong trình duyệt
        await page.evaluate(() => {
            window.__frameDeltas = [];
            window.__measuringFps = true;
            let lastTime = performance.now();

            function frameLoop(now: number) {
                if (!window.__measuringFps) return;
                const delta = now - lastTime;
                window.__frameDeltas?.push(delta);
                lastTime = now;
                requestAnimationFrame(frameLoop);
            }
            requestAnimationFrame(frameLoop);
        });

        // Di chuyển chuột qua 25 steps liên tục (kích hoạt chuỗi sự kiện mousemove dày đặc)
        await page.mouse.move(topBox!.x + topBox!.width / 2, topBox!.y + topBox!.height / 2);
        await page.mouse.down();
        await page.mouse.move(topBox!.x + topBox!.width / 2, topBox!.y + topBox!.height / 2 - 80, { steps: 25 });
        await page.mouse.up();
        await page.waitForTimeout(200);

        // Thu thập và kiểm chứng chỉ số hiệu năng thực tế từ trình duyệt
        const fpsMetrics = await page.evaluate(() => {
            window.__measuringFps = false;
            const deltas: number[] = window.__frameDeltas || [];
            if (!deltas.length) return { avgFps: 60, maxDelta: 16, frameCount: 0 };
            const avgDelta = deltas.reduce((a, b) => a + b, 0) / deltas.length;
            return {
                avgFps: Math.round(1000 / avgDelta),
                maxDelta: Math.max(...deltas),
                frameCount: deltas.length,
            };
        });

        // Khẳng định: trình duyệt liên tục vẽ khung hình, FPS đạt chuẩn mượt mà >= 40 fps và không bị khóa luồng
        expect(fpsMetrics.frameCount).toBeGreaterThan(0);
        expect(fpsMetrics.avgFps).toBeGreaterThanOrEqual(40);
        expect(fpsMetrics.maxDelta).toBeLessThan(120);

        const resizedHeight = (await tableWrapper.boundingBox())?.height || 0;
        expect(resizedHeight).toBeGreaterThan(initialHeight + 40);

        // 6. Kiểm tra không còn hiển thị pill chỉ số px và các nút preset (280/450/650) theo yêu cầu người dùng
        await expect(page.locator('.detected-columns-tools')).toHaveCount(0);
        await expect(page.locator('.height-indicator-tag')).toHaveCount(0);

        // Click đúp vào thanh resizer để reset chiều cao về mặc định 280px
        await topResizer.dblclick();
        await page.waitForTimeout(200);
        const resetHeight = (await tableWrapper.boundingBox())?.height || 0;
        expect(Math.abs(resetHeight - 280)).toBeLessThanOrEqual(5);

        // 7. Kiểm tra bảng Xem trước dữ liệu mẫu (bấm nút Xem dữ liệu mẫu)
        const btnPreview = page.locator('.sql-toolbar-right .el-button').filter({ hasText: /Xem dữ liệu mẫu/ });
        await btnPreview.click();

        const sampleCard = page.locator('.sample-preview-card');
        await expect(sampleCard).toBeVisible({ timeout: 5_000 });

        const sampleResizer = sampleCard.locator('.sample-table-resizer');
        await expect(sampleResizer).toBeVisible();
        await sampleResizer.scrollIntoViewIfNeeded();

        // Kéo thanh resizer bảng mẫu xuống 60px
        const sampleResizerBox = await sampleResizer.boundingBox();
        expect(sampleResizerBox).not.toBeNull();

        const sampleBody = sampleCard.locator('.sample-preview-body');
        const initialSampleHeight = (await sampleBody.boundingBox())?.height || 180;

        await page.mouse.move(sampleResizerBox!.x + sampleResizerBox!.width / 2, sampleResizerBox!.y + sampleResizerBox!.height / 2);
        await page.mouse.down();
        await page.mouse.move(sampleResizerBox!.x + sampleResizerBox!.width / 2, sampleResizerBox!.y + sampleResizerBox!.height / 2 + 60, { steps: 5 });
        await page.mouse.up();
        await page.waitForTimeout(200);

        const resizedSampleHeight = (await sampleBody.boundingBox())?.height || 0;
        expect(resizedSampleHeight).toBeGreaterThan(initialSampleHeight + 30);

        // Bảng dữ liệu mẫu cũng không còn hiển thị pill chỉ số và các nút preset
        await expect(sampleCard.locator('.height-indicator-tag')).toHaveCount(0);
        await expect(sampleCard.locator('.sample-preview-tools .el-button-group')).toHaveCount(0);

        // Nút Đóng của bảng mẫu vẫn hoạt động bình thường
        const btnCloseSample = sampleCard.locator('.sample-preview-tools .el-button').filter({ hasText: /Đóng/ });
        await expect(btnCloseSample).toBeVisible();
        await btnCloseSample.click();
        await expect(sampleCard).toBeHidden();
    });

    test('Quy chuẩn ngôn ngữ nhãn UI — Không viết song ngữ lai tiếng Anh trong ngoặc (Rule 20.13)', async ({ page }) => {
        // 1. Quét tĩnh mã nguồn: VisualQueryBuilder.vue, vi-vn.json, en-us.json
        const vqbPath = path.resolve(__dirname, '../../../../TA-ITS015-WEBVUE-V1.0/src/src/views/sharedata/dataSource/component/visualQueryBuilder.vue');
        const vqbContent = fs.readFileSync(vqbPath, 'utf-8');

        // CẤM xuất hiện các nhãn song ngữ lai tiếng Anh trong ngoặc trên template VisualQueryBuilder.vue
        expect(vqbContent).not.toContain('Trực quan (Builder)');
        expect(vqbContent).not.toContain('Mã nguồn SQL (Raw)');
        expect(vqbContent).not.toContain('Cột trích xuất (SELECT)');
        expect(vqbContent).not.toContain('Điều kiện lọc (WHERE)');
        expect(vqbContent).not.toContain('Sắp xếp (ORDER BY)');
        expect(vqbContent).not.toContain('Tăng dần (ASC)');
        expect(vqbContent).not.toContain('Giảm dần (DESC)');

        // Kiểm tra tệp i18n vi-vn.json phải là tiếng Việt thuần túy
        const viPath = path.resolve(__dirname, '../../../../TA-ITS015-WEBVUE-V1.0/src/src/i18n/lang/vi-vn.json');
        const viContent = JSON.parse(fs.readFileSync(viPath, 'utf-8'));
        expect(viContent['lz.label.sharedataDataSource.vqbVisualMode']).toBe('Trực quan');
        expect(viContent['lz.label.sharedataDataSource.vqbRawMode']).toBe('Mã nguồn SQL');

        // Kiểm tra tệp i18n en-us.json phải là tiếng Anh chuẩn
        const enPath = path.resolve(__dirname, '../../../../TA-ITS015-WEBVUE-V1.0/src/src/i18n/lang/en-us.json');
        const enContent = JSON.parse(fs.readFileSync(enPath, 'utf-8'));
        expect(enContent['lz.label.sharedataDataSource.vqbVisualMode']).toBe('Visual Builder');
        expect(enContent['lz.label.sharedataDataSource.vqbRawMode']).toBe('Raw SQL');

        // 2. Kiểm tra render DOM thực tế trên trang dataSource
        await page.setViewportSize({ width: 1600, height: 900 });
        await page.goto('/#/sharedata/dataSource');

        // Chờ tabs hiển thị (tự động nạp gói tin đầu tiên)
        const tabs = page.locator('.datasource-right-tabs');
        await expect(tabs).toBeVisible({ timeout: 15_000 });

        // Chuyển sang tab Cấu hình SQL
        const sqlTab = tabs.getByRole('tab', { name: /Cấu hình SQL/ });
        await sqlTab.click();
        await expect(sqlTab).toHaveClass(/is-active/);

        // Kiểm tra 2 tab nút chế độ Dual-View trong VisualQueryBuilder
        const segTabs = page.locator('.vqb-segmented-tabs');
        await expect(segTabs).toBeVisible();

        const btnVisual = segTabs.locator('.vqb-tab-btn').nth(0);
        const btnRaw = segTabs.locator('.vqb-tab-btn').nth(1);

        const visualText = (await btnVisual.innerText()).trim();
        const rawText = (await btnRaw.innerText()).trim();

        // Nhãn phải thuần tiếng Việt, không chứa "(Builder)" hay "(Raw)"
        expect(visualText).toBe('Trực quan');
        expect(visualText).not.toContain('Builder');
        expect(rawText).toBe('Mã nguồn SQL');
        expect(rawText).not.toContain('Raw');

        // Kiểm tra các card trong Canvas trực quan không chứa từ tiếng Anh trong ngoặc
        const colCardTitle = page.locator('.vqb-columns-card .vqb-card-title');
        await expect(colCardTitle).toBeVisible();
        const colText = await colCardTitle.innerText();
        expect(colText).toContain('Cột trích xuất:');
        expect(colText).not.toContain('(SELECT)');

        const filterCardTitle = page.locator('.vqb-filter-card .vqb-card-title');
        await expect(filterCardTitle).toBeVisible();
        const filterText = await filterCardTitle.innerText();
        expect(filterText).toContain('Điều kiện lọc:');
        expect(filterText).not.toContain('(WHERE)');

        const orderLabel = page.locator('.vqb-order-row .vqb-label');
        await expect(orderLabel).toBeVisible();
        const orderText = await orderLabel.innerText();
        expect(orderText).toContain('Sắp xếp:');
        expect(orderText).not.toContain('(ORDER BY)');
    });

    test('10. Đồng bộ 18 trường giữa Cấu hình SQL và Danh sách trường đã lưu, loại bỏ everright-filter khỏi package.json', async ({ page }) => {
        // 1. Quét tĩnh mã nguồn: package.json không còn phụ thuộc everright-filter
        const packageJsonPath = path.resolve(
            __dirname,
            '../../../../TA-ITS015-WEBVUE-V1.0/src/package.json'
        );
        const pkgContent = fs.readFileSync(packageJsonPath, 'utf8');
        expect(pkgContent).not.toContain('everright-filter');

        // Quét tĩnh VisualQueryBuilder.vue và index.vue: props, buttons, functions đồng bộ
        const vqbPath = path.resolve(
            __dirname,
            '../../../../TA-ITS015-WEBVUE-V1.0/src/src/views/sharedata/dataSource/component/visualQueryBuilder.vue'
        );
        const vqbContent = fs.readFileSync(vqbPath, 'utf8');
        expect(vqbContent).toContain('savedFields');
        expect(vqbContent).toContain('handleApplySavedFields');
        expect(vqbContent).toContain('vqbSavedFieldsBtn');

        const indexVuePath = path.resolve(
            __dirname,
            '../../../../TA-ITS015-WEBVUE-V1.0/src/src/views/sharedata/dataSource/index.vue'
        );
        const indexVueContent = fs.readFileSync(indexVuePath, 'utf8');
        expect(indexVueContent).toContain(':saved-fields="rawFlatFields"');
        expect(indexVueContent).toContain('handleGenerateSqlFromSavedFields');
        expect(indexVueContent).toContain('Sinh SQL từ trường đã lưu');

        // 2. Mock API danh sách 18 trường metadata trả về cho gói tin PKT_01
        const mock18Fields = [
            { id: 'FLD_01', datatypeId: 'PKT_01', aliasFieldKey: 'detectionId', fieldName: 'Mã định danh phát hiện', fieldType: 'long', status: 1 },
            { id: 'FLD_02', datatypeId: 'PKT_01', aliasFieldKey: 'detectTime', fieldName: 'Thời gian phát hiện', fieldType: 'datetime', status: 1 },
            { id: 'FLD_03', datatypeId: 'PKT_01', aliasFieldKey: 'vehicleType', fieldName: 'Loại phương tiện', fieldType: 'string', status: 1 },
            { id: 'FLD_04', datatypeId: 'PKT_01', aliasFieldKey: 'licensePlate', fieldName: 'Biển số xe', fieldType: 'string', status: 1 },
            { id: 'FLD_05', datatypeId: 'PKT_01', aliasFieldKey: 'speed', fieldName: 'Vận tốc (km/h)', fieldType: 'double', status: 1 },
            { id: 'FLD_06', datatypeId: 'PKT_01', aliasFieldKey: 'lane', fieldName: 'Làn đường', fieldType: 'int', status: 1 },
            { id: 'FLD_07', datatypeId: 'PKT_01', aliasFieldKey: 'direction', fieldName: 'Hướng di chuyển', fieldType: 'int', status: 1 },
            { id: 'FLD_08', datatypeId: 'PKT_01', aliasFieldKey: 'locationRoute', fieldName: 'Vị trí tuyến đường', fieldType: 'string', status: 1 },
            { id: 'FLD_09', datatypeId: 'PKT_01', aliasFieldKey: 'equipmentId', fieldName: 'Mã thiết bị đo', fieldType: 'string', status: 1 },
            { id: 'FLD_10', datatypeId: 'PKT_01', aliasFieldKey: 'height', fieldName: 'Chiều cao phương tiện', fieldType: 'double', status: 1 },
            { id: 'FLD_11', datatypeId: 'PKT_01', aliasFieldKey: 'width', fieldName: 'Chiều rộng phương tiện', fieldType: 'double', status: 1 },
            { id: 'FLD_12', datatypeId: 'PKT_01', aliasFieldKey: 'length', fieldName: 'Chiều dài phương tiện', fieldType: 'double', status: 1 },
            { id: 'FLD_13', datatypeId: 'PKT_01', aliasFieldKey: 'zoneId', fieldName: 'Mã khu vực/trạm', fieldType: 'string', status: 1 },
            { id: 'FLD_14', datatypeId: 'PKT_01', aliasFieldKey: 'ipAddress', fieldName: 'Địa chỉ IP thiết bị', fieldType: 'string', status: 1 },
            { id: 'FLD_15', datatypeId: 'PKT_01', aliasFieldKey: 'eventType', fieldName: 'Loại sự kiện giao thông', fieldType: 'string', status: 1 },
            { id: 'FLD_16', datatypeId: 'PKT_01', aliasFieldKey: 'plateColor', fieldName: 'Màu biển số', fieldType: 'string', status: 1 },
            { id: 'FLD_17', datatypeId: 'PKT_01', aliasFieldKey: 'vehicleBrand', fieldName: 'Hãng xe', fieldType: 'string', status: 1 },
            { id: 'FLD_18', datatypeId: 'PKT_01', aliasFieldKey: 'vehicleColor', fieldName: 'Màu sơn xe', fieldType: 'string', status: 1 },
        ];

        await page.route('**/api/sharedata/sharedatapacketfield/list*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: mock18Fields,
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
                        records: mock18Fields,
                        total: mock18Fields.length,
                    },
                }),
            });
        });

        await page.route('**/api/sharedata/sharedatapacket/parseextractcolumns*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        isValid: true,
                        columns: mock18Fields.map((f) => f.aliasFieldKey).concat(['__lastTime', '__rowid']),
                    },
                }),
            });
        });

        // 3. Mở màn hình dataSource
        await page.setViewportSize({ width: 1600, height: 900 });
        await page.goto('/#/sharedata/dataSource');

        // Chờ tabs hiển thị (tự động nạp gói tin đầu tiên)
        const tabs = page.locator('.datasource-right-tabs');
        await expect(tabs).toBeVisible({ timeout: 15_000 });

        // Chuyển sang tab Cấu hình SQL
        const sqlTab = tabs.getByRole('tab', { name: /Cấu hình SQL/ });
        await sqlTab.click();
        await expect(sqlTab).toHaveClass(/is-active/);

        // 4. Kiểm tra nút tiện ích "Sinh SQL từ trường đã lưu (18)" hiển thị trên toolbar SQL Center
        const btnGenSql = page.locator('.sql-toolbar-right .el-button').filter({ hasText: /Sinh SQL từ trường đã lưu/ });
        await expect(btnGenSql).toBeVisible();
        await expect(btnGenSql).toContainText('18');

        // 5. Kiểm tra nút "Cột đã lưu (18)" trong VisualQueryBuilder hiển thị
        const btnSavedInBuilder = page.locator('.vqb-header-actions .el-button').filter({ hasText: /Cột đã lưu/ });
        await expect(btnSavedInBuilder).toBeVisible();
        await expect(btnSavedInBuilder).toContainText('18');

        // 6. Nhấn nút "Sinh SQL từ trường đã lưu (18)"
        await btnGenSql.click();
        await page.waitForTimeout(400);

        // 7. Xác thực SQL được sinh ra chứa đủ các trường nghiệp vụ quan trọng và con trỏ
        const sqlPreview = page.locator('.vqb-sql-preview-strip');
        await expect(sqlPreview).toBeVisible();
        await expect(sqlPreview).toContainText('detectionId');
        await expect(sqlPreview).toContainText('detectTime');
        await expect(sqlPreview).toContainText('licensePlate');
        await expect(sqlPreview).toContainText('speed');
        await expect(sqlPreview).toContainText('__lastTime');
        await expect(sqlPreview).toContainText('__rowid');

        // 8. Bấm nút "Cột đã lưu (18)" trong Visual Builder để kiểm tra cập nhật danh sách cột trực quan
        await btnSavedInBuilder.click();
        await page.waitForTimeout(300);

        const colCountTag = page.locator('.vqb-columns-card .vqb-card-title .el-tag');
        await expect(colCountTag).toContainText('20 cột');
    });

    test('Hỗ trợ kéo resize 2 div con trong Visual Query Builder (Cột trích xuất và Điều kiện lọc)', async ({ page }) => {
        // 1. Quét tĩnh mã nguồn VisualQueryBuilder.vue để kiểm chứng các tính năng resize
        const vqbPath = path.resolve(
            __dirname,
            '../../../../TA-ITS015-WEBVUE-V1.0/src/src/views/sharedata/dataSource/component/visualQueryBuilder.vue'
        );
        const vqbContent = fs.readFileSync(vqbPath, 'utf8');

        // Kiểm chứng có 2 thanh resizer riêng biệt cho 2 card con
        expect(vqbContent).toContain('columns-card-resizer');
        expect(vqbContent).toContain('filter-card-resizer');
        expect(vqbContent).toContain('vqb-card-resize-bar');

        // Kiểm chứng các hàm xử lý kéo resize và reset
        expect(vqbContent).toContain('startResizeColumns');
        expect(vqbContent).toContain('resetColumnsHeight');
        expect(vqbContent).toContain('startResizeFilter');
        expect(vqbContent).toContain('resetFilterHeight');

        // Kiểm chứng CSS loại bỏ vạch gạch chéo resize native (resize: none) và tối ưu will-change
        expect(vqbContent).toContain('resize: none');
        expect(vqbContent).not.toContain('resize: vertical');
        expect(vqbContent).toContain('will-change: height');

        // Kiểm chứng logic loại bỏ dấu gạch thừa khi không có điều kiện tiếp theo
        expect(vqbContent).toContain('hasMultipleFilterItems');
        expect(vqbContent).toContain('margin-bottom: 14px');
        expect(vqbContent).toContain('&:not(:first-child)::before');

        // 2. Mở màn hình dataSource
        await page.setViewportSize({ width: 1600, height: 900 });
        await page.goto('/#/sharedata/dataSource');

        // Chờ tabs hiển thị (tự động nạp gói tin đầu tiên)
        const tabs = page.locator('.datasource-right-tabs');
        await expect(tabs).toBeVisible({ timeout: 15_000 });

        // Chuyển sang tab Cấu hình SQL
        const sqlTab = tabs.getByRole('tab', { name: /Cấu hình SQL/ });
        await sqlTab.click();
        await expect(sqlTab).toHaveClass(/is-active/);

        // 3. Kiểm tra thanh resizer và chức năng kéo của Card Cột trích xuất
        const colResizer = page.locator('.columns-card-resizer');
        await expect(colResizer).toBeVisible();
        await colResizer.scrollIntoViewIfNeeded();

        const colList = page.locator('.vqb-columns-list');
        await expect(colList).toBeVisible();
        const initialColHeight = (await colList.boundingBox())?.height || 105;

        // Kéo thanh resizer xuống 60px để tăng chiều cao
        const colResizerBox = await colResizer.boundingBox();
        expect(colResizerBox).not.toBeNull();

        await page.mouse.move(colResizerBox!.x + colResizerBox!.width / 2, colResizerBox!.y + colResizerBox!.height / 2);
        await page.mouse.down();
        await page.mouse.move(colResizerBox!.x + colResizerBox!.width / 2, colResizerBox!.y + colResizerBox!.height / 2 + 60, { steps: 5 });
        await page.mouse.up();
        await page.waitForTimeout(200);

        const resizedColHeight = (await colList.boundingBox())?.height || 0;
        expect(resizedColHeight).toBeGreaterThan(initialColHeight + 30);

        // Double click để reset về mặc định
        await colResizer.dblclick();
        await page.waitForTimeout(200);
        const resetColHeight = (await colList.boundingBox())?.height || 0;
        expect(Math.abs(resetColHeight - 105)).toBeLessThanOrEqual(10);

        // 4. Kiểm tra thanh resizer và chức năng kéo của Card Điều kiện lọc
        const filterResizer = page.locator('.filter-card-resizer');
        await expect(filterResizer).toBeVisible();
        await filterResizer.scrollIntoViewIfNeeded();

        const filterCanvas = page.locator('.vqb-filter-canvas');
        await expect(filterCanvas).toBeVisible();
        const initialFilterHeight = (await filterCanvas.boundingBox())?.height || 180;

        // Kéo thanh resizer xuống 50px để tăng chiều cao
        const filterResizerBox = await filterResizer.boundingBox();
        expect(filterResizerBox).not.toBeNull();

        await page.mouse.move(filterResizerBox!.x + filterResizerBox!.width / 2, filterResizerBox!.y + filterResizerBox!.height / 2);
        await page.mouse.down();
        await page.mouse.move(filterResizerBox!.x + filterResizerBox!.width / 2, filterResizerBox!.y + filterResizerBox!.height / 2 + 50, { steps: 5 });
        await page.mouse.up();
        await page.waitForTimeout(200);

        const resizedFilterHeight = (await filterCanvas.boundingBox())?.height || 0;
        expect(resizedFilterHeight).toBeGreaterThan(initialFilterHeight + 25);

        // Double click để reset về mặc định
        await filterResizer.dblclick();
        await page.waitForTimeout(200);
        const resetFilterHeight = (await filterCanvas.boundingBox())?.height || 0;
        expect(Math.abs(resetFilterHeight - 180)).toBeLessThanOrEqual(10);
    });

    test('11. Phân giải chính sách gói tin (resolvePacketPolicy & parsePacketNumber) và cấu hình chính sách trong editPacket.vue', async () => {
        // 1. Kiểm thử parsePacketNumber
        expect(parsePacketNumber('101')).toBe(101);
        expect(parsePacketNumber('101_commonData')).toBe(101);
        expect(parsePacketNumber('105_traffic_event')).toBe(105);
        expect(parsePacketNumber('111')).toBe(111);
        expect(parsePacketNumber('100')).toBeNull();
        expect(parsePacketNumber('112')).toBeNull();
        expect(parsePacketNumber('invalid_code')).toBeNull();
        expect(parsePacketNumber(null)).toBeNull();
        expect(parsePacketNumber(undefined)).toBeNull();

        // 2. Kiểm thử resolvePacketPolicy
        // Nhóm Snapshot: 101, 102, 105, 108 (Hiển thị nhãn 'Gửi full')
        expect(resolvePacketPolicy('101').policy).toBe('Snapshot');
        expect(resolvePacketPolicy('101').label).toBe('Gửi full');
        expect(resolvePacketPolicy('101').tagType).toBe('success');
        expect(resolvePacketPolicy('102').policy).toBe('Snapshot');
        expect(resolvePacketPolicy('105').policy).toBe('Snapshot');
        expect(resolvePacketPolicy('108').policy).toBe('Snapshot');

        // Nhóm AlwaysIncremental: 103, 104, 106, 107, 109 (Hiển thị nhãn 'Gửi nối đuôi')
        expect(resolvePacketPolicy('103').policy).toBe('AlwaysIncremental');
        expect(resolvePacketPolicy('103').label).toBe('Gửi nối đuôi');
        expect(resolvePacketPolicy('103').tagType).toBe('primary');
        expect(resolvePacketPolicy('104').policy).toBe('AlwaysIncremental');
        expect(resolvePacketPolicy('106').policy).toBe('AlwaysIncremental');
        expect(resolvePacketPolicy('107').policy).toBe('AlwaysIncremental');
        expect(resolvePacketPolicy('109').policy).toBe('AlwaysIncremental');

        // Nhóm NotReady: 110
        expect(resolvePacketPolicy('110').policy).toBe('NotReady');
        expect(resolvePacketPolicy('110').tagType).toBe('warning');

        // Nhóm Disabled: 111
        expect(resolvePacketPolicy('111').policy).toBe('Disabled');
        expect(resolvePacketPolicy('111').tagType).toBe('info');

        // Mã không hợp lệ / chưa xác định
        expect(resolvePacketPolicy(null).policy).toBe('NotReady');
        expect(resolvePacketPolicy('unknown_pkt').policy).toBe('NotReady');

        // 3. Quét tĩnh editPacket.vue để kiểm tra hiển thị chính sách
        const editPacketPath = path.resolve(
            __dirname,
            '../../../../TA-ITS015-WEBVUE-V1.0/src/src/views/sharedata/dataSource/component/editPacket.vue'
        );
        const editPacketContent = fs.readFileSync(editPacketPath, 'utf8');
        expect(editPacketContent).toContain('resolvePacketPolicy');
        expect(editPacketContent).toContain('currentPolicy');
        expect(editPacketContent).toContain('packet-policy-display');
        expect(editPacketContent).toContain('policy-desc');
        expect(editPacketContent).not.toContain('!important');
    });

    test('12. Quy chuẩn đặt tên tệp component — BẮT BUỘC dùng camelCase (chữ cái đầu viết thường), CẤM viết hoa PascalCase (Rule 20.14)', async () => {
        const sharedataDir = path.resolve(__dirname, '../../../../TA-ITS015-WEBVUE-V1.0/src/src/views/sharedata');
        const getVueComponentFiles = (dir: string): string[] => {
            let results: string[] = [];
            const list = fs.readdirSync(dir);
            list.forEach((file) => {
                const fullPath = path.join(dir, file);
                const stat = fs.statSync(fullPath);
                if (stat && stat.isDirectory()) {
                    results = results.concat(getVueComponentFiles(fullPath));
                } else if (file.endsWith('.vue') && dir.includes('component')) {
                    results.push(file);
                }
            });
            return results;
        };

        const vueFiles = getVueComponentFiles(sharedataDir);
        expect(vueFiles.length).toBeGreaterThan(0);
        for (const file of vueFiles) {
            const firstChar = file.charAt(0);
            // Ký tự đầu tiên phải là chữ thường (camelCase)
            expect(firstChar).toBe(firstChar.toLowerCase());
            // Cấm tuyệt đối PascalCase như VisualQueryBuilder.vue
            expect(file).not.toBe('VisualQueryBuilder.vue');
        }

        // Kiểm tra tệp visualQueryBuilder.vue tồn tại và có name attribute là camelCase
        const vqbPath = path.resolve(
            sharedataDir,
            'dataSource/component/visualQueryBuilder.vue'
        );
        expect(fs.existsSync(vqbPath)).toBe(true);
        const vqbContent = fs.readFileSync(vqbPath, 'utf8');
        expect(vqbContent).toContain('name="visualQueryBuilder"');
    });

    test('13. Quy chuẩn 100% i18n — CẤM hardcode text mẫu mặc định, nhãn, tooltip, ElMessage trong visualQueryBuilder.vue (Rule 20.15)', async () => {
        const sharedataDir = path.resolve(__dirname, '../../../../TA-ITS015-WEBVUE-V1.0/src/src/views/sharedata');
        const vqbPath = path.resolve(sharedataDir, 'dataSource/component/visualQueryBuilder.vue');
        expect(fs.existsSync(vqbPath)).toBe(true);

        const vqbContent = fs.readFileSync(vqbPath, 'utf8');

        // 1. Quét tĩnh: CẤM chuỗi cứng "Mẫu mặc định" trong template và script
        expect(vqbContent).not.toMatch(/>\s*Mẫu mặc định\s*</);
        expect(vqbContent).not.toContain("'Mẫu mặc định'");
        expect(vqbContent).not.toContain('"Mẫu mặc định"');

        // 2. Quét tĩnh: CẤM các chuỗi ElMessage hardcoded tiếng Việt
        expect(vqbContent).not.toContain('Đang giữ nguyên câu SQL hiện tại');
        expect(vqbContent).not.toContain('Chưa có trường nào được lưu trong danh sách');
        expect(vqbContent).not.toContain('Đã sao chép câu SQL');
        expect(vqbContent).not.toMatch(/Đã nạp .* cột từ danh sách/);

        // 3. Khẳng định operators sử dụng computed và $t
        expect(vqbContent).toContain("const operators = computed(() => [");
        expect(vqbContent).toContain("lz.label.sharedataDataSource.opEqual");
        expect(vqbContent).toContain("lz.label.sharedataDataSource.opIsNotNull");

        // 4. Khẳng định predefinedTables sử dụng descKey và $t
        expect(vqbContent).toContain("tblDescTrafficData");
        expect(vqbContent).toContain("tblDescTollTransaction");
        expect(vqbContent).toContain("$t(tb.descKey)");

        // 5. Kiểm tra tính đầy đủ và đồng bộ giữa vi-vn.json và en-us.json
        const viPath = path.resolve(__dirname, '../../../../TA-ITS015-WEBVUE-V1.0/src/src/i18n/lang/vi-vn.json');
        const enPath = path.resolve(__dirname, '../../../../TA-ITS015-WEBVUE-V1.0/src/src/i18n/lang/en-us.json');
        expect(fs.existsSync(viPath)).toBe(true);
        expect(fs.existsSync(enPath)).toBe(true);

        const viJson = JSON.parse(fs.readFileSync(viPath, 'utf8'));
        const enJson = JSON.parse(fs.readFileSync(enPath, 'utf8'));

        const requiredKeys = [
            'lz.label.sharedataDataSource.vqbVisualMode',
            'lz.label.sharedataDataSource.vqbRawMode',
            'lz.label.sharedataDataSource.vqbDefaultTemplate',
            'lz.label.sharedataDataSource.vqbResetToCatalogTooltip',
            'lz.message.sharedataDataSource.vqbKeepCurrentSql',
            'lz.message.sharedataDataSource.vqbNoSavedFields',
            'lz.message.sharedataDataSource.vqbLoadedSavedFields',
            'lz.message.sharedataDataSource.vqbCopiedSql',
            'lz.label.sharedataDataSource.opEqual',
            'lz.label.sharedataDataSource.opNotEqual',
            'lz.label.sharedataDataSource.opGreaterThan',
            'lz.label.sharedataDataSource.opGreaterOrEqual',
            'lz.label.sharedataDataSource.opLessThan',
            'lz.label.sharedataDataSource.opLessOrEqual',
            'lz.label.sharedataDataSource.opLike',
            'lz.label.sharedataDataSource.opNotLike',
            'lz.label.sharedataDataSource.opBetween',
            'lz.label.sharedataDataSource.opIn',
            'lz.label.sharedataDataSource.opNotIn',
            'lz.label.sharedataDataSource.opIsNull',
            'lz.label.sharedataDataSource.opIsNotNull',
            'lz.label.sharedataDataSource.tblDescTrafficData',
            'lz.label.sharedataDataSource.tblDescIncident',
            'lz.label.sharedataDataSource.tblDescWeather',
            'lz.label.sharedataDataSource.tblDescZoneStatus',
            'lz.label.sharedataDataSource.tblDescCctvDevice',
            'lz.label.sharedataDataSource.tblDescVmsCurrent',
            'lz.label.sharedataDataSource.tblDescTollTransaction',
        ];

        for (const k of requiredKeys) {
            expect(viJson[k], `Missing key in vi-vn.json: ${k}`).toBeTruthy();
            expect(enJson[k], `Missing key in en-us.json: ${k}`).toBeTruthy();
        }

        expect(viJson['lz.label.sharedataDataSource.vqbDefaultTemplate']).toBe('Mẫu mặc định');
        expect(enJson['lz.label.sharedataDataSource.vqbDefaultTemplate']).toBe('Default Template');
    });
});


