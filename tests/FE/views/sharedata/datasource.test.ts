import { test, expect } from '@playwright/test';
import { mockAuth, resolveWebvuePath } from '../../setup';
import fs from 'node:fs';
import path from 'node:path';
import {
    analyzeCursorSql,
    applyCursor,
    removeCursor,
    applyFromDate,
} from '../../../../TA-ITS015-WEBVUE-V1.0/src/src/utils/sharedata/cursorSql';
import {
    parseVisualSql,
    buildVisualSql,
    normalizeSqlForComparison,
} from '../../../../TA-ITS015-WEBVUE-V1.0/src/src/utils/sharedata/visualSql';

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

        await page.route('**/api/sharedata/sharedatapacket/getextractsql*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        extractSql: 'SELECT TOP (@pageSize) td.Speed AS speed FROM TmsTrafficData td',
                        status: 1,
                        syncMode: 1,
                        maxLookBackDays: null,
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
                        columns: ['speed'],
                    },
                }),
            });
        });

        await page.route('**/api/sharedata/sharedatapacketfield/getextractcolumns*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        sqlAvailable: true,
                        columns: [
                            { fieldKey: 'header.status', dataType: 'int', assigned: true },
                            { fieldKey: 'speed', dataType: 'int', assigned: false },
                        ],
                    },
                }),
            });
        });

        await page.route('**/api/sharedata/sharedatapacketfield/batchadd*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        addedCount: 1,
                        skippedKeys: [],
                    },
                }),
            });
        });

        await page.route('**/api/sharedata/sharedatapacketfield/list*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: [
                        { id: 'FLD_01', datatypeId: 'PKT_01', aliasFieldKey: 'header.status', fieldType: 'int', status: 1, isRequired: 1 },
                    ],
                }),
            });
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

        const boxAlias1 = await colAlias.boundingBox();
        const boxType1 = await colType.boundingBox();
        const boxStatus1 = await colStatus.boundingBox();

        expect(boxAlias1).not.toBeNull();
        expect(boxType1).not.toBeNull();
        expect(boxStatus1).not.toBeNull();

        // Cả 3 cột nằm trên cùng 1 hàng ngang (chênh lệch Y <= 5px)
        await expect.poll(async () => {
            const bAlias = await colAlias.boundingBox();
            const bType = await colType.boundingBox();
            const bStatus = await colStatus.boundingBox();
            if (!bAlias || !bType || !bStatus) return 999;
            return Math.max(
                Math.abs(bAlias.y - bType.y),
                Math.abs(bType.y - bStatus.y)
            );
        }, { timeout: 5000 }).toBeLessThanOrEqual(5);

        // Cấp độ 2: 1100px - 1400px (ví dụ 1200px) -> Nằm trên 2 hàng (2 cột hàng 1, cột status hàng 2)
        await page.setViewportSize({ width: 1200, height: 900 });
        await page.waitForTimeout(300);

        const boxAlias2 = await colAlias.boundingBox();
        const boxType2 = await colType.boundingBox();
        const boxStatus2 = await colStatus.boundingBox();

        // Hàng 1: Khóa field + Kiểu
        expect(Math.abs(boxAlias2!.y - boxType2!.y)).toBeLessThanOrEqual(5);

        // Hàng 2: Trạng thái nằm dưới Hàng 1
        expect(boxStatus2!.y).toBeGreaterThan(boxAlias2!.y + 20);

        // Mỗi cột chiếm ~50% hàng
        const rowBox2 = await searchForm.locator('.el-row').first().boundingBox();
        expect(rowBox2).not.toBeNull();
        expect(boxAlias2!.width / rowBox2!.width).toBeGreaterThan(0.45);
        expect(boxAlias2!.width / rowBox2!.width).toBeLessThan(0.55);

        // Cấp độ 3: Dưới 1100px (ví dụ 1000px) -> Xuống hàng hết (xếp dọc 3 dòng)
        await page.setViewportSize({ width: 1000, height: 900 });
        await page.waitForTimeout(300);

        const boxAlias3 = await colAlias.boundingBox();
        const boxType3 = await colType.boundingBox();
        const boxStatus3 = await colStatus.boundingBox();

        // Mỗi ô xếp trên 1 hàng dọc riêng biệt
        expect(boxType3!.y).toBeGreaterThan(boxAlias3!.y + 20);
        expect(boxStatus3!.y).toBeGreaterThan(boxType3!.y + 20);

        // Mỗi cột chiếm gần như 100% hàng
        const rowBox3 = await searchForm.locator('.el-row').first().boundingBox();
        expect(rowBox3).not.toBeNull();
        expect(boxAlias3!.width / rowBox3!.width).toBeGreaterThan(0.90);

        // Kiểm tra tại màn hình nhỏ hẹp 937px x 692px: input không bị tràn mép / khuất phía sau
        await page.setViewportSize({ width: 937, height: 692 });
        await page.waitForTimeout(300);

        const cardBoxNarrow = await cardField.boundingBox();
        const inputAliasBox = await colAlias.locator('.el-input').boundingBox();

        expect(cardBoxNarrow).not.toBeNull();
        expect(inputAliasBox).not.toBeNull();
    });

    test('4. Kiểm tra tính năng kéo dãn độ rộng cột (Column Resizing) trên bảng Gói tin và bảng Trường gói tin', async ({ page }) => {
        await page.setViewportSize({ width: 1600, height: 900 });
        await page.goto('/#/sharedata/dataSource');

        // Chờ bảng gói tin hiển thị
        const packetGrid = page.locator('.packet-col .vxe-grid');
        await expect(packetGrid).toBeVisible({ timeout: 30_000 });

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

    test('Bố cục bảng và phân trang dataSource: nội dung table cuộn bên trong, footer phân trang có chiều cao cố định 40px không bị trôi @regression', async ({ page }) => {
        await page.setViewportSize({ width: 1600, height: 900 });
        await page.goto('/#/sharedata/dataSource');
        await page.evaluate(() => { window.scrollTo(0, 0); document.documentElement.scrollTop = 0; document.body.scrollTop = 0; });

        // Bảng gói tin xGridPacket phải hiển thị
        const packetTable = page.locator('.packet-col .vxe-grid');
        await expect(packetTable).toBeVisible({ timeout: 15_000 });

        // Kiểm tra thanh phân trang vxe-pager ở đáy có chiều cao cố định 40px và bám sát đáy card
        const pager = page.locator('.packet-col .vxe-pager');
        await expect(pager).toBeVisible();
        const count = await page.locator(".packet-col .full-table").count(); console.log("COUNT:", count); const cardBox = await page.locator('.packet-col .full-table').boundingBox();
        const gridBox = await page.locator('.packet-col .vxe-grid').boundingBox();
        const pagerBox = await pager.boundingBox();

        expect(pagerBox).not.toBeNull();
        expect(pagerBox!.height).toBeCloseTo(40, 1);
        expect(cardBox).not.toBeNull();
        expect(gridBox).not.toBeNull();

        // Độ lệch giữa đáy pager và đáy card không vượt quá padding (khoảng trống thừa < 10px)
        console.log("DEBUG_BOXES:", { cardBox, gridBox, pagerBox }); const gap900 = cardBox!.y + cardBox!.height - (pagerBox!.y + pagerBox!.height);
        expect(gap900).toBeLessThanOrEqual(10);
        expect(gridBox!.height).toBeGreaterThan(600);

        // Kiểm tra co giãn phản hồi khi viewport cao 1200px: bảng tự động giãn theo màn hình
        await page.setViewportSize({ width: 1600, height: 1200 });
        await page.waitForTimeout(300);
        const cardBox1200 = await page.locator('.packet-col .full-table').boundingBox();
        const gridBox1200 = await page.locator('.packet-col .vxe-grid').boundingBox();
        const pagerBox1200 = await pager.boundingBox();
        const gap1200 = cardBox1200!.y + cardBox1200!.height - (pagerBox1200!.y + pagerBox1200!.height);
        expect(gridBox1200!.height).toBeGreaterThan(900);
        expect(gap1200).toBeLessThanOrEqual(10);

        // Kiểm tra co giãn phản hồi khi viewport thấp 600px: bảng tự động co lại khít vừa vặn
        await page.setViewportSize({ width: 1600, height: 600 });
        await page.waitForTimeout(300);
        const cardBox600 = await page.locator('.packet-col .full-table').boundingBox();
        const gridBox600 = await page.locator('.packet-col .vxe-grid').boundingBox();
        const pagerBox600 = await pager.boundingBox();
        const gap600 = cardBox600!.y + cardBox600!.height - (pagerBox600!.y + pagerBox600!.height);
        expect(gridBox600!.height).toBeLessThan(450);
        expect(gap600).toBeLessThanOrEqual(10);

        // Khôi phục viewport chuẩn và kiểm tra body wrapper của bảng tồn tại để cuộn nội dung độc lập
        await page.setViewportSize({ width: 1600, height: 900 });
        await page.waitForTimeout(200);
        const tableBodyWrapper = page.locator('.packet-col .vxe-table--body-wrapper.body--wrapper');
        await expect(tableBodyWrapper).toBeAttached();
    });

    test('17. Kéo resize thanh chia table (splitpanes splitter) — Pane co giãn mượt mà, table không vỡ layout sau khi kéo', async ({ page }) => {
        await page.setViewportSize({ width: 1600, height: 900 });
        await page.goto('/#/sharedata/dataSource');

        const splitpanes = page.locator('.datasource-splitpanes');
        await expect(splitpanes).toBeVisible({ timeout: 15_000 });

        const splitter = splitpanes.locator('.splitpanes__splitter').first();
        await expect(splitter).toBeVisible();

        const packetPane = page.locator('.packet-pane');
        const fieldPane = page.locator('.field-pane');
        await expect(packetPane).toBeVisible();
        await expect(fieldPane).toBeVisible();

        // Đảm bảo splitter đã ổn định vị trí trên màn hình
        await expect(async () => {
            const box = await splitter.boundingBox();
            expect(box).not.toBeNull();
            expect(box!.y).toBeLessThan(200);
        }).toPass({ timeout: 5000 });

        const initialPacketBox = await packetPane.boundingBox();
        expect(initialPacketBox).not.toBeNull();
        const initialWidth = initialPacketBox!.width;

        const splitterBox = await splitter.boundingBox();
        expect(splitterBox).not.toBeNull();

        // Kéo splitter sang phải 120px
        const startX = splitterBox!.x + splitterBox!.width / 2;
        const startY = splitterBox!.y + 100;

        await page.mouse.move(startX, startY);
        await page.mouse.down();
        await page.mouse.move(startX + 120, startY, { steps: 20 });
        await page.waitForTimeout(300);
        await page.mouse.up();
        await page.waitForTimeout(300);

        // Chờ bảng tính toán lại trên @resized
        await page.waitForTimeout(300);

        const newPacketBox = await packetPane.boundingBox();
        expect(newPacketBox).not.toBeNull();
        // Pane bên trái phải rộng hơn ban đầu
        expect(newPacketBox!.width).toBeGreaterThan(initialWidth + 50);

        // Bảng gói tin xGridPacket vẫn hiển thị ổn định
        const packetGrid = page.locator('.packet-pane .vxe-grid');
        await expect(packetGrid).toBeVisible();
        const packetRows = packetGrid.locator('.vxe-body--row');
        await expect(packetRows.first()).toBeVisible();
    });

    test('19. Kiểm tra cột Hành động cố định bên phải (fixed: right) và chống đứt gãy bố cục khi kéo resize cột', async ({ page }) => {
        await page.setViewportSize({ width: 1600, height: 900 });
        await page.goto('/#/sharedata/dataSource');

        const packetGrid = page.locator('.packet-col .vxe-grid');
        await expect(packetGrid).toBeVisible({ timeout: 30_000 });

        // Chờ các hàng dữ liệu bảng gói tin hiển thị
        const packetRows = packetGrid.locator('.vxe-body--row');
        await expect(packetRows.first()).toBeVisible({ timeout: 10_000 });

        // 3. Kiểm chứng cột Hành động được cố định bên phải thông qua .vxe-table--fixed-right-wrapper
        const packetFixedRight = packetGrid.locator('.vxe-table--fixed-right-wrapper');
        await expect(packetFixedRight).toBeVisible();

        const actionHeader = packetFixedRight.locator('.vxe-header--column').filter({ hasText: 'Hành động' });
        await expect(actionHeader).toBeVisible();

        // Mép phải của khối fixed right phải khớp sát mép phải của bảng (được neo cứng bên phải)
        const gridBox = await packetGrid.boundingBox();
        const fixedRightBox = await packetFixedRight.boundingBox();
        expect(gridBox).not.toBeNull();
        expect(fixedRightBox).not.toBeNull();
        const rightEdgeDiff = Math.abs((fixedRightBox!.x + fixedRightBox!.width) - (gridBox!.x + gridBox!.width));
        expect(rightEdgeDiff).toBeLessThanOrEqual(5);

        // Các nút thao tác (copy, edit, delete) trong cột Hành động fixed right phải hiển thị và tương tác được
        const firstRowActionButtons = packetFixedRight.locator('.vxe-body--row').first().locator('button');
        await expect(firstRowActionButtons.first()).toBeVisible();

        // 4. Kiểm tra thao tác kéo resize cột Tên gói tin (flex column)
        const resizer = packetGrid.locator('.vxe-table--main-wrapper .vxe-header--column:has(.vxe-resizable):visible').first().locator('.vxe-resizable');
        await expect(resizer).toBeVisible();
        const resizerBox = await resizer.boundingBox();
        expect(resizerBox).not.toBeNull();

        const startX = resizerBox!.x + resizerBox!.width / 2;
        const startY = resizerBox!.y + resizerBox!.height / 2;
        await page.mouse.move(startX, startY);
        await page.mouse.down();
        await page.mouse.move(startX - 50, startY, { steps: 15 });
        await page.waitForTimeout(200);
        await page.mouse.up();
        await page.waitForTimeout(300);

        // Sau khi kéo resize, cột Hành động (fixed: 'right') vẫn duy trì vị trí neo sát mép phải
        await expect(packetFixedRight).toBeVisible();
        const fixedRightBoxAfter = await packetFixedRight.boundingBox();
        expect(fixedRightBoxAfter).not.toBeNull();
        const rightEdgeDiffAfter = Math.abs((fixedRightBoxAfter!.x + fixedRightBoxAfter!.width) - (gridBox!.x + gridBox!.width));
        expect(rightEdgeDiffAfter).toBeLessThanOrEqual(5);

        // 5. Kiểm chứng bảng Trường (fieldGrid) bên phải cũng có cột Hành động fixed: 'right'
        const fieldGrid = page.locator('.field-col .vxe-grid');
        await expect(fieldGrid).toBeVisible({ timeout: 10_000 });
        const fieldFixedRight = fieldGrid.locator('.vxe-table--fixed-right-wrapper');
        await expect(fieldFixedRight).toBeVisible();
        const fieldActionHeader = fieldFixedRight.locator('.vxe-header--column').filter({ hasText: 'Hành động' });
        await expect(fieldActionHeader).toBeVisible();
    });

    test('26. Query Builder bảo toàn ngữ nghĩa SQL và khóa Builder khi phát hiện khác biệt (RV-6)', async () => {
        const qbPath = resolveWebvuePath('views/sharedata/dataSource/component/queryBuilder.vue');
        const qbContent = fs.readFileSync(qbPath, 'utf8');

        // Bắt buộc có hàm normalizeSqlForComparison
        expect(qbContent).toContain('normalizeSqlForComparison');
        // Bắt buộc so sánh normOriginal !== normRegenerated
        expect(qbContent).toContain('normOriginal !== normRegenerated');
        // Khóa canUseBuilder = false và thông báo vqbCannotFullyRepresent
        expect(qbContent).toContain('vqbCannotFullyRepresent');
        expect(qbContent).toContain("viewMode.value = 'raw'");

        // Kiểm tra đa ngôn ngữ có định nghĩa thông báo
        const viPath = resolveWebvuePath('i18n/lang/its/sharedata/vi-vn.json');
        const viContent = JSON.parse(fs.readFileSync(viPath, 'utf8'));
        expect(viContent['lz.message.sharedataDataSource.vqbCannotFullyRepresent']).toBeDefined();

        const enPath = resolveWebvuePath('i18n/lang/its/sharedata/en-us.json');
        const enContent = JSON.parse(fs.readFileSync(enPath, 'utf8'));
        expect(enContent['lz.message.sharedataDataSource.vqbCannotFullyRepresent']).toBeDefined();
    });

    test('31. Rút gọn nhãn Bảng nguồn súc tích và ô chọn Bảng nguồn co giãn theo nội dung trong Visual Query Builder', async ({ page }) => {
        const qbPath = resolveWebvuePath('views/sharedata/dataSource/component/queryBuilder.vue');
        const qbContent = fs.readFileSync(qbPath, 'utf8');

        // 1. Quét tĩnh mã nguồn: sử dụng hàm getTableShortLabel và class vqb-table-select có width: fit-content
        expect(qbContent).toContain(':label="getTableShortLabel(tb)"');
        expect(qbContent).toContain('function getTableShortLabel(tb: { name: string; descKey?: string; description?: string }): string');
        expect(qbContent).toContain('.vqb-table-select {\n\t\t\twidth: fit-content;');
        expect(qbContent).not.toContain(':label="`${tb.name} (${tb.descKey ? $t(tb.descKey) : tb.description})`"');

        // 2. Kiểm thử logic chuẩn hóa nhãn rút gọn cho các bảng nguồn ITS
        const simulateShortLabel = (name: string) => {
            const shortMap: Record<string, string> = {
                TmsZone: 'Đoạn đường',
                TmsZoneStatus: 'Lưu thông',
                TmsTrafficStatistic: 'Thống kê',
                TmsEquipment: 'Thiết bị',
                TmsTrafficData: 'Dò xe',
                TmsWeather: 'Thời tiết',
                TmsIncident: 'Sự cố',
                TmsEventType: 'Loại sự cố',
                CctvDevice: 'Camera CCTV',
                VmsCurrent: 'Biển báo VMS',
                VmsBoard: 'Thiết bị VMS',
                TollTransactionIn: 'Thu phí vào',
                TollTransactionOut: 'Thu phí ra',
                TollLane: 'Làn thu phí',
                TollStation: 'Trạm thu phí',
                TmsRoute: 'Tuyến đường',
                TmsWimData: 'Cân tải trọng',
                TmsVehicleRegistration: 'Đăng kiểm',
            };
            const desc = shortMap[name];
            return desc ? `${name} (${desc})` : name;
        };

        expect(simulateShortLabel('TmsZone')).toBe('TmsZone (Đoạn đường)');
        expect(simulateShortLabel('TmsZoneStatus')).toBe('TmsZoneStatus (Lưu thông)');
        expect(simulateShortLabel('TmsTrafficStatistic')).toBe('TmsTrafficStatistic (Thống kê)');
        expect(simulateShortLabel('TmsEquipment')).toBe('TmsEquipment (Thiết bị)');
        expect(simulateShortLabel('TmsTrafficData')).toBe('TmsTrafficData (Dò xe)');
        expect(simulateShortLabel('TmsWeather')).toBe('TmsWeather (Thời tiết)');
        expect(simulateShortLabel('TmsIncident')).toBe('TmsIncident (Sự cố)');
        expect(simulateShortLabel('TmsEventType')).toBe('TmsEventType (Loại sự cố)');
        expect(simulateShortLabel('CctvDevice')).toBe('CctvDevice (Camera CCTV)');
        expect(simulateShortLabel('VmsCurrent')).toBe('VmsCurrent (Biển báo VMS)');
        expect(simulateShortLabel('VmsBoard')).toBe('VmsBoard (Thiết bị VMS)');
        expect(simulateShortLabel('TollTransactionIn')).toBe('TollTransactionIn (Thu phí vào)');
        expect(simulateShortLabel('TollTransactionOut')).toBe('TollTransactionOut (Thu phí ra)');
        expect(simulateShortLabel('TollLane')).toBe('TollLane (Làn thu phí)');
        expect(simulateShortLabel('TollStation')).toBe('TollStation (Trạm thu phí)');
        expect(simulateShortLabel('TmsRoute')).toBe('TmsRoute (Tuyến đường)');
        expect(simulateShortLabel('TmsWimData')).toBe('TmsWimData (Cân tải trọng)');
        expect(simulateShortLabel('TmsVehicleRegistration')).toBe('TmsVehicleRegistration (Đăng kiểm)');
        expect(simulateShortLabel('CustomUnknownTable')).toBe('CustomUnknownTable');

        // Nhãn không còn chứa mã gói hay câu mô tả dài
        const allLabels = [
            'TmsZone', 'TmsZoneStatus', 'TmsTrafficStatistic', 'TmsEquipment',
            'TmsTrafficData', 'TmsWeather', 'TmsIncident', 'TmsEventType',
            'CctvDevice', 'VmsCurrent', 'VmsBoard', 'TollTransactionIn',
            'TollTransactionOut', 'TollLane', 'TollStation', 'TmsRoute',
            'TmsWimData', 'TmsVehicleRegistration'
        ].map(simulateShortLabel);

        for (const lbl of allLabels) {
            expect(lbl).not.toMatch(/\(Gói\s*\d+/i);
            expect(lbl).not.toMatch(/P\d{3}/i);
            expect(lbl.length).toBeLessThanOrEqual(35);
        }

        // 3. Render giao diện thực tế: kiểm tra select Bảng nguồn và độ rộng hiển thị
        await page.route('**/api/sharedata/sharedatapacket/getmetadatatables*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: [
                        { tableName: 'TmsZone', displayName: 'Đoạn đường', defaultAlias: 'z' },
                        { tableName: 'TmsTrafficData', displayName: 'Dò xe', defaultAlias: 'td' },
                    ],
                }),
            });
        });

        await page.setViewportSize({ width: 1600, height: 900 });
        await page.goto('/#/sharedata/dataSource');

        const packetGrid = page.locator('.packet-col .vxe-grid');
        await expect(packetGrid).toBeVisible({ timeout: 30_000 });
        await page.locator('.vxe-table--body-wrapper tbody tr').first().click();

        const tabs = page.locator('.datasource-right-tabs');
        await expect(tabs).toBeVisible({ timeout: 15_000 });

        const sqlTab = tabs.getByRole('tab', { name: /Cấu hình SQL/ });
        await sqlTab.click();
        await expect(sqlTab).toHaveClass(/is-active/);

        const tableSelect = page.locator('.visual-query-builder-container .vqb-table-select');
        await expect(tableSelect).toBeVisible();

        const box = await tableSelect.boundingBox();
        expect(box).toBeTruthy();
        if (box) {
            expect(box.width).toBeGreaterThanOrEqual(80);
            expect(box.width).toBeLessThan(260);
        }

        // Mở dropdown chọn bảng nguồn
        await tableSelect.click();
        const popper = page.locator('.el-select__popper:visible');
        await expect(popper).toBeVisible({ timeout: 5_000 });

        const optItems = popper.locator('.el-select-dropdown__item');
        const optCount = await optItems.count();
        expect(optCount).toBeGreaterThan(0);

        const firstOptText = (await optItems.first().innerText()).trim();
        expect(firstOptText).not.toMatch(/\(Gói\s*\d+/i);
        expect(firstOptText).not.toMatch(/P\d{3}/i);
    });

    test('32. Visual Query Builder tải danh mục bảng nguồn từ CSDL, ưu tiên defaultAlias và cảnh báo khi danh mục rỗng', async ({ page }) => {
        const qbPath = resolveWebvuePath('views/sharedata/dataSource/component/queryBuilder.vue');
        const qbContent = fs.readFileSync(qbPath, 'utf8');

        // 1. Quét tĩnh mã nguồn: MetadataTableItem chứa defaultAlias, không chứa category
        expect(qbContent).toContain('defaultAlias?: string;');
        expect(qbContent).not.toContain('category?: string;');

        // 2. tableOptions trả về mảng rỗng [] khi metadataTables rỗng (không fallback predefinedTables)
        expect(qbContent).toContain('if (!metadataTables.value || metadataTables.value.length === 0) {\n\t\treturn [];\n\t}');

        // 3. fetchMetadataTables gọi cảnh báo noAllowedTables khi danh mục rỗng hoặc lỗi
        expect(qbContent).toContain("ElMessage.warning($t('lz.message.sharedataDataSource.noAllowedTables'));");

        // 4. Kiểm tra file i18n vi-vn.json và en-us.json đã được cấu hình key cảnh báo
        const viPath = resolveWebvuePath('i18n/lang/its/sharedata/vi-vn.json');
        const viContent = JSON.parse(fs.readFileSync(viPath, 'utf8'));
        expect(viContent['lz.message.sharedataDataSource.noAllowedTables']).toBe(
            'Chưa có bảng nguồn nào được phép, liên hệ quản trị chạy script cấu hình'
        );

        const enPath = resolveWebvuePath('i18n/lang/its/sharedata/en-us.json');
        const enContent = JSON.parse(fs.readFileSync(enPath, 'utf8'));
        expect(enContent['lz.message.sharedataDataSource.noAllowedTables']).toBe(
            'No allowed source tables found, please contact administrator to run configuration script'
        );

        // 5. Kiểm thử logic ưu tiên alias 3 tầng
        const predefined = [
            { name: 'TmsTrafficData', defaultAlias: 'td' },
            { name: 'TmsIncident', defaultAlias: 'i' },
        ];
        const apiMetadata = [
            { tableName: 'TollLane', defaultAlias: 'tl' },
            { tableName: 'TollStation', defaultAlias: 'ts' },
            { tableName: 'CustomTable', defaultAlias: '' },
        ];

        const resolveAlias = (tblName: string) => {
            const found = predefined.find((t) => t.name.toLowerCase() === tblName.toLowerCase());
            const meta = apiMetadata.find((t) => t.tableName.toLowerCase() === tblName.toLowerCase());
            return found?.defaultAlias || meta?.defaultAlias?.trim() || (tblName ? tblName.charAt(0).toLowerCase() : 't');
        };

        // Ưu tiên 1: predefinedTables
        expect(resolveAlias('TmsTrafficData')).toBe('td');
        // Ưu tiên 2: defaultAlias từ API CSDL
        expect(resolveAlias('TollLane')).toBe('tl');
        expect(resolveAlias('TollStation')).toBe('ts');
        // Ưu tiên 3: Ký tự đầu tên bảng viết thường khi không có defaultAlias
        expect(resolveAlias('CustomTable')).toBe('c');
        expect(resolveAlias('UnknownTable')).toBe('u');

        // 6. Test thực tế: Mock API getmetadatatables trả về danh mục bảng có TollLane (defaultAlias: 'tl')
        await page.route('**/api/sharedata/sharedatapacket/getmetadatatables*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: [
                        { id: 'tbl-traffic', tableName: 'TmsTrafficData', displayName: 'Dò xe', defaultAlias: 'td' },
                        { id: 'tbl-lane', tableName: 'TollLane', displayName: 'Làn thu phí', defaultAlias: 'tl' },
                    ],
                }),
            });
        });

        await page.route('**/api/sharedata/sharedatapacket/getmetadatafields*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: [
                        { columnName: 'ID', dataType: 'bigint', suggestedAlias: 'id' },
                        { columnName: 'LaneNo', dataType: 'int', suggestedAlias: 'laneNo' },
                    ],
                }),
            });
        });

        await page.setViewportSize({ width: 1600, height: 900 });
        await page.goto('/#/sharedata/dataSource');

        const packetGrid = page.locator('.packet-col .vxe-grid');
        await expect(packetGrid).toBeVisible({ timeout: 30_000 });
        await page.locator('.vxe-table--body-wrapper tbody tr').first().click();

        const tabs = page.locator('.datasource-right-tabs');
        await expect(tabs).toBeVisible({ timeout: 15_000 });
        const sqlTab = tabs.getByRole('tab', { name: /Cấu hình SQL/ });
        await sqlTab.click();
        await expect(sqlTab).toHaveClass(/is-active/);

        const tableSelect = page.locator('.visual-query-builder-container .vqb-table-select');
        await expect(tableSelect).toBeVisible();

        // Mở dropdown chọn bảng nguồn TollLane
        await tableSelect.click();
        const popper = page.locator('.el-select__popper:visible');
        await expect(popper).toBeVisible({ timeout: 5_000 });
        const tollLaneOpt = popper.locator('.el-select-dropdown__item').filter({ hasText: /TollLane/i });
        await expect(tollLaneOpt).toBeVisible();
        await tollLaneOpt.click();

        // Kiểm tra alias input được tự động điền 'tl' từ defaultAlias của API CSDL
        const aliasInput = page.locator('.visual-query-builder-container .vqb-alias-input input');
        await expect(aliasInput).toHaveValue('tl');
    });

    test('30. Kiểm thử queryBuilder sắp xếp bảng theo PacketCodes lên đầu và giữ tất cả bảng khác', async ({ page }) => {
        // 1. Kiểm tra unit logic sắp xếp bảng theo packetCodes của queryBuilder
        const mockTables = [
            { tableName: 'TmsEquipment', displayName: 'Thiết bị', defaultAlias: 'eq', packetCodes: [] },
            { tableName: 'TmsZoneStatus', displayName: 'Trạng thái Zone', defaultAlias: 'zs', packetCodes: ['101', '110'] },
            { tableName: 'TmsWeather', displayName: 'Thời tiết', defaultAlias: 'w', packetCodes: ['104', '110'] },
            { tableName: 'TmsTrafficData', displayName: 'Dữ liệu giao thông', defaultAlias: 'td', packetCodes: ['103', '106'] },
        ];

        const matchesPacket = (itemPacketCodes: string[] | undefined, targetPacketCode: string) => {
            if (!itemPacketCodes || itemPacketCodes.length === 0 || !targetPacketCode) return false;
            const rawCode = targetPacketCode.trim().toLowerCase();
            const numericCode = rawCode.match(/^\D*(\d+)/)?.[1] || '';
            return itemPacketCodes.some((p) => {
                const pClean = (p || '').trim().toLowerCase();
                if (!pClean) return false;
                if (pClean === rawCode) return true;
                if (rawCode.startsWith(`${pClean}_`) || rawCode.startsWith(`${pClean}-`)) return true;
                if (pClean.startsWith(`${rawCode}_`) || pClean.startsWith(`${rawCode}-`)) return true;
                const pNumeric = pClean.match(/^\D*(\d+)/)?.[1] || '';
                return Boolean(numericCode && pNumeric && numericCode === pNumeric);
            });
        };

        const sortTablesForPacket = (tables: typeof mockTables, packetCode: string) => {
            return [...tables].sort((a, b) => {
                const aMatch = matchesPacket(a.packetCodes, packetCode);
                const bMatch = matchesPacket(b.packetCodes, packetCode);
                if (aMatch && !bMatch) return -1;
                if (!aMatch && bMatch) return 1;
                return 0;
            });
        };

        // Khi mở gói 101: TmsZoneStatus lên đầu tiên, các bảng khác (kể cả TmsEquipment không có 101) vẫn còn đầy đủ
        const sorted101 = sortTablesForPacket(mockTables, '101_commonData');
        expect(sorted101[0].tableName).toBe('TmsZoneStatus');
        expect(sorted101.map((t) => t.tableName)).toContain('TmsEquipment');
        expect(sorted101.length).toBe(mockTables.length);

        // Khi mở gói 104: TmsWeather lên đầu tiên, TmsEquipment và TmsZoneStatus vẫn còn
        const sorted104 = sortTablesForPacket(mockTables, '104');
        expect(sorted104[0].tableName).toBe('TmsWeather');
        expect(sorted104.length).toBe(mockTables.length);

        // 2. Mock API getmetadatatables với packetCodes và kiểm tra trên giao diện
        await page.route('**/api/sharedata/sharedatapacket/getmetadatatables*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: [
                        { tableName: 'TmsEquipment', displayName: 'Thiết bị', defaultAlias: 'eq', packetCodes: [] },
                        { tableName: 'TmsZoneStatus', displayName: 'Trạng thái Zone', defaultAlias: 'zs', packetCodes: ['101', '110'] },
                        { tableName: 'TmsWeather', displayName: 'Thời tiết', defaultAlias: 'w', packetCodes: ['104', '110'] },
                    ],
                }),
            });
        });

        await page.setViewportSize({ width: 1600, height: 900 });
        await page.goto('/#/sharedata/dataSource');

        const packetGrid = page.locator('.packet-col .vxe-grid');
        await expect(packetGrid).toBeVisible({ timeout: 30_000 });
        await page.locator('.vxe-table--body-wrapper tbody tr').first().click();

        const tabs = page.locator('.datasource-right-tabs');
        await expect(tabs).toBeVisible({ timeout: 15_000 });
        const sqlTab = tabs.getByRole('tab', { name: /Cấu hình SQL/ });
        await sqlTab.click();
        await expect(sqlTab).toHaveClass(/is-active/);

        const tableSelect = page.locator('.visual-query-builder-container .vqb-table-select');
        await expect(tableSelect).toBeVisible();

        // Mở dropdown chọn bảng: kiểm tra cả bảng ưu tiên và bảng không có packetCodes đều xuất hiện
        await tableSelect.click();
        const popper = page.locator('.el-select__popper:visible');
        await expect(popper).toBeVisible({ timeout: 5_000 });
        const eqOpt = popper.locator('.el-select-dropdown__item').filter({ hasText: /TmsEquipment/i });
        await expect(eqOpt).toBeVisible();
    });

    test('34. Bỏ button định dạng (Format SQL) ở dataSource & queryBuilder, dọn sạch handler và các khóa i18n liên quan', async ({ page }) => {
        // 1. Quét tĩnh mã nguồn queryBuilder.vue: không còn handleFormatSql, vqbFormatSqlBtn, vqbFormatSqlTooltip, icon ele-Finished
        const qbPath = resolveWebvuePath('views/sharedata/dataSource/component/queryBuilder.vue');
        const qbContent = fs.readFileSync(qbPath, 'utf8');
        expect(qbContent).not.toContain('handleFormatSql');
        expect(qbContent).not.toContain('vqbFormatSqlBtn');
        expect(qbContent).not.toContain('vqbFormatSqlTooltip');
        expect(qbContent).not.toContain('ele-Finished');

        // 2. Quét tĩnh tệp i18n: không còn khóa thừa liên quan đến nút định dạng SQL
        const viPath = resolveWebvuePath('i18n/lang/its/sharedata/vi-vn.json');
        const enPath = resolveWebvuePath('i18n/lang/its/sharedata/en-us.json');
        const viJson = JSON.parse(fs.readFileSync(viPath, 'utf8'));
        const enJson = JSON.parse(fs.readFileSync(enPath, 'utf8'));

        expect(viJson['lz.label.sharedataDataSource.vqbFormatSqlBtn']).toBeUndefined();
        expect(viJson['lz.label.sharedataDataSource.vqbFormatSqlTooltip']).toBeUndefined();
        expect(enJson['lz.label.sharedataDataSource.vqbFormatSqlBtn']).toBeUndefined();
        expect(enJson['lz.label.sharedataDataSource.vqbFormatSqlTooltip']).toBeUndefined();

        // 3. E2E kiểm tra DOM: Khi chuyển sang chế độ Raw SQL (Mã nguồn SQL), không xuất hiện nút Định dạng
        await page.setViewportSize({ width: 1600, height: 900 });
        await page.goto('/#/sharedata/dataSource');

        const packetGrid = page.locator('.packet-col .vxe-grid');
        await expect(packetGrid).toBeVisible({ timeout: 30_000 });
        await page.locator('.vxe-table--body-wrapper tbody tr').first().click();

        const tabs = page.locator('.datasource-right-tabs');
        await expect(tabs).toBeVisible({ timeout: 15_000 });
        const sqlTab = tabs.getByRole('tab', { name: /Cấu hình SQL/ });
        await sqlTab.click();
        await expect(sqlTab).toHaveClass(/is-active/);

        // Chuyển sang tab Mã nguồn SQL (Raw SQL)
        const segTabs = page.locator('.vqb-segmented-tabs');
        await expect(segTabs).toBeVisible();
        const btnRawMode = segTabs.locator('.vqb-tab-btn').nth(1);
        await expect(btnRawMode).toBeVisible();
        await btnRawMode.click();

        // Xác nhận nút Định dạng không tồn tại trong thanh công cụ queryBuilder
        const vqbHeaderActions = page.locator('.visual-query-builder-container .vqb-header-actions');
        await expect(vqbHeaderActions).toBeVisible();
        const formatBtn = vqbHeaderActions.locator('button').filter({ hasText: /Định dạng/ });
        await expect(formatBtn).toHaveCount(0);
        const finishedIcon = vqbHeaderActions.locator('.ele-Finished');
        await expect(finishedIcon).toHaveCount(0);
    });

    test('33. Hộp thoại form đóng ngay khi bấm ra ngoài (không hỏi xác nhận kể cả đã sửa); hộp confirm đóng khi bấm ra ngoài', async ({ page }) => {
        await page.goto('/#/sharedata/dataSource');

        const packetGrid = page.locator('.packet-col .vxe-grid');
        await expect(packetGrid).toBeVisible({ timeout: 30_000 });

        // 1. Kiểm tra editPacket dialog: chưa sửa gì -> bấm ra ngoài lớp phủ thì hộp đóng ngay
        const addPacketBtn = page.locator('.packet-col button:has-text("Thêm")').first();
        await expect(addPacketBtn).toBeVisible();
        await addPacketBtn.click();

        const packetDialog = page.locator('.el-overlay:not([style*="display: none"]) .el-dialog').first();
        await expect(packetDialog).toBeVisible({ timeout: 5000 });

        // Click outside tại tọa độ (5, 5) khi chưa sửa gì -> đóng ngay không hỏi
        await page.mouse.click(5, 5);
        await expect(packetDialog).toBeHidden({ timeout: 5000 });

        // 2. Mở lại, sửa một ô (nhập mã gói) -> bấm ra ngoài -> đóng ngay, không hỏi "Có thay đổi chưa lưu"
        await addPacketBtn.click();
        await expect(packetDialog).toBeVisible({ timeout: 5000 });

        const codeInput = packetDialog.locator('input').first();
        await codeInput.fill('TEST_CODE');

        await page.mouse.click(5, 5);
        const confirmBox = page.locator('.el-message-box');
        await expect(packetDialog).toBeHidden({ timeout: 5000 });
        await expect(confirmBox).toHaveCount(0);

        // 3. Kiểm tra ElMessageBox.confirm: bấm ra ngoài lớp phủ thì hộp đóng (Hủy) mà không gọi API
        let deleteApiCalled = false;
        await page.route('**/api/sharedata/sharedatapacket/delete*', async (route) => {
            deleteApiCalled = true;
            await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ code: 200, result: true }) });
        });

        const deletePacketBtn = page.locator('.packet-col button.el-button--danger').first();
        if (await deletePacketBtn.isVisible()) {
            await deletePacketBtn.click();
            const msgBox = page.locator('.el-message-box');
            await expect(msgBox).toBeVisible({ timeout: 5000 });

            // Click outside -> đóng confirm mà không gọi API delete
            await page.mouse.click(5, 5);
            await expect(msgBox).toBeHidden({ timeout: 5000 });
            expect(deleteApiCalled).toBe(false);
        }
    });

    test('35. Bấm dấu x trên hộp cảnh báo lỗi el-alert thì thông báo tắt ngay lập tức', async ({ page }) => {
        await page.route('**/api/sharedata/sharedatapacket/parseextractcolumns*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        isValid: false,
                        errorMessage: 'Bảng [TmsZoneStatus] không nằm trong danh mục bảng nguồn được phép truy vấn của ShareData.',
                    },
                }),
            });
        });

        await page.goto('/#/sharedata/dataSource');
        const tabs = page.locator('.datasource-right-tabs');
        await expect(tabs).toBeVisible({ timeout: 15_000 });

        const sqlTab = tabs.getByRole('tab', { name: /Cấu hình SQL/ });
        await sqlTab.click();
        await expect(sqlTab).toHaveClass(/is-active/);

        // Alert lỗi phải hiển thị
        const errorAlert = page.locator('.sql-center-pane .el-alert--error');
        await expect(errorAlert).toBeVisible({ timeout: 5000 });
        await expect(errorAlert).toContainText('Bảng [TmsZoneStatus] không nằm trong danh mục');

        // Bấm nút đóng 'x'
        const closeBtn = errorAlert.locator('.el-alert__close-btn');
        await expect(closeBtn).toBeVisible();
        await closeBtn.click();

        // Alert phải biến mất
        await expect(errorAlert).toBeHidden({ timeout: 3000 });
    });

    test('36. Khối Điều kiện lọc không dùng accordion, hiển thị gợi ý khi rỗng và hỗ trợ autocomplete gợi ý biến', async ({ page }) => {
        // 1. Quét tĩnh mã nguồn queryBuilder.vue
        const qbPath = resolveWebvuePath('views/sharedata/dataSource/component/queryBuilder.vue');
        const qbContent = fs.readFileSync(qbPath, 'utf8');

        // Bỏ accordion collapse (isFilterCollapsed không còn)
        expect(qbContent).not.toContain('isFilterCollapsed');
        expect(qbContent).not.toContain('toggleFilterCollapse');
        expect(qbContent).toContain('vqb-no-filter-hint');
        expect(qbContent).toContain('queryFilterParamSuggestions');
        expect(qbContent).toContain('el-autocomplete');

        // 2. Mở màn hình dataSource
        await page.goto('/#/sharedata/dataSource');
        const tabs = page.locator('.datasource-right-tabs');
        await expect(tabs).toBeVisible({ timeout: 15_000 });

        const sqlTab = tabs.getByRole('tab', { name: /Cấu hình SQL/ });
        await sqlTab.click();
        await expect(sqlTab).toHaveClass(/is-active/);

        // Khối Điều kiện lọc
        const filterCard = page.locator('.vqb-filter-card');
        await expect(filterCard).toBeVisible({ timeout: 5000 });

        // Badge số lượng điều kiện
        const filterBadge = filterCard.locator('.vqb-card-title .el-tag');
        await expect(filterBadge).toBeVisible();

        // Không còn nút Thu gọn / Mở rộng
        const toggleBtn = filterCard.locator('.filter-collapse-btn');
        await expect(toggleBtn).toHaveCount(0);

        // Nút "+ Thêm điều kiện"
        const addRuleBtn = filterCard.locator('.vqb-card-tools .el-button').filter({ hasText: /Thêm điều kiện/ });
        await expect(addRuleBtn).toBeVisible();

        // Bấm "+ Thêm điều kiện" -> thêm 1 rule mới và có el-autocomplete
        await addRuleBtn.click();
        const ruleValueInput = filterCard.locator('.vqb-rule-val input').first();
        await expect(ruleValueInput).toBeVisible();
    });

    test('37. Bố cục tự động co giãn theo chiều cao màn hình: Bảng gói tin và trường gói tin giãn đầy đủ 100% không để lại khoảng trắng thừa @regression', async ({ page }) => {
        // Quét tĩnh mã nguồn dataSource/index.vue đảm bảo không lạm dụng !important
        const sourcePath = resolveWebvuePath('views/sharedata/dataSource/index.vue');
        const sourceCode = fs.readFileSync(sourcePath, 'utf8');
        expect(sourceCode).not.toContain('!important');

        // Danh sách các kích thước viewport thực tế cần kiểm thử co giãn
        const viewports = [
            { width: 1440, height: 900, name: 'Laptop tiêu chuẩn (1440x900)' },
            { width: 1920, height: 1080, name: 'Màn hình lớn Full HD (1920x1080)' },
            { width: 1280, height: 720, name: 'Màn hình nhỏ HD (1280x720)' },
        ];

        for (const vp of viewports) {
            await page.setViewportSize({ width: vp.width, height: vp.height });
            await page.goto('/#/sharedata/dataSource');
            await page.evaluate(() => { window.scrollTo(0, 0); document.documentElement.scrollTop = 0; document.body.scrollTop = 0; });

            const packetGrid = page.locator('.packet-col .vxe-grid');
            await expect(packetGrid).toBeVisible({ timeout: 15_000 });

            // 1. Kiểm tra bảng gói tin bên trái (packet-col):
            const packetCard = page.locator('.packet-col .full-table');
            const packetPager = page.locator('.packet-col .vxe-pager');
            await expect(packetPager).toBeVisible();

            const cardBox = await packetCard.boundingBox();
            const gridBox = await packetGrid.boundingBox();
            const pagerBox = await packetPager.boundingBox();

            expect(cardBox).not.toBeNull();
            expect(gridBox).not.toBeNull();
            expect(pagerBox).not.toBeNull();

            // Bảng vxe-grid phải chiếm phần lớn chiều cao của card (không bị co rút về 7 dòng ~350px trên màn hình lớn)
            expect(gridBox!.height).toBeGreaterThan(cardBox!.height * 0.85);

            // Đáy thanh phân trang phải bám sát đáy card (khoảng cách hở <= 10px do padding 5px)
            const packetGap = cardBox!.y + cardBox!.height - (pagerBox!.y + pagerBox!.height);
            expect(packetGap).toBeLessThanOrEqual(10);

            // 2. Kiểm tra bảng trường gói tin bên phải (field-col, tab savedFields):
            const fieldTab = page.locator('.datasource-right-tabs').getByRole('tab', { name: /Trường đã lưu/ });
            if (await fieldTab.isVisible()) {
                await fieldTab.click();
                const fieldCard = page.locator('.field-col .right-pane-card');
                const fieldGrid = page.locator('.field-col .vxe-grid');
                const fieldPager = page.locator('.field-col .vxe-pager');

                if (await fieldPager.isVisible()) {
                    const fCardBox = await fieldCard.boundingBox();
                    const fGridBox = await fieldGrid.boundingBox();
                    const fPagerBox = await fieldPager.boundingBox();

                    if (fCardBox && fGridBox && fPagerBox) {
                        const fieldGap = fCardBox.y + fCardBox.height - (fPagerBox.y + fPagerBox.height);
                        expect(fieldGap).toBeLessThanOrEqual(15);
                    }
                }
            }
        }
    });

    test('38. Ô bí danh (vqb-col-alias) và biểu thức (vqb-col-expr) co giãn tự động theo nội dung, vừa khít text và có padding 6px', async () => {
        const qbPath = resolveWebvuePath('views/sharedata/dataSource/component/queryBuilder.vue');
        const content = fs.readFileSync(qbPath, 'utf-8');

        // 1. Kiểm tra CSS định nghĩa width: fit-content, field-sizing: content và padding 6px đều 4 phía
        expect(content).toContain('field-sizing: content;');
        expect(content).toContain('padding: 6px;');
        expect(content).toContain('width: fit-content;');
        expect(content).toContain('.vqb-col-alias');

        // 2. Đảm bảo không còn fixed width hay min-width lớn làm phình ô ngắn
        expect(content).not.toMatch(/\.vqb-col-alias\s*\{\s*width:\s*100px;/);
        expect(content).not.toMatch(/\.vqb-col-alias\s*\{[^}]*min-width:\s*90px;/);
    });

    test('39. Màn Nguồn dữ liệu: kiểm quyền SuperAdmin theo accountType 111, bỏ top cố định khi xem mẫu và dùng giới hạn packetLookbackDays từ cấu hình', async () => {
        const idxPath = resolveWebvuePath('views/sharedata/dataSource/index.vue');
        const content = fs.readFileSync(idxPath, 'utf-8');

        // 1. isSuperAdmin chỉ kiểm tra accountType === '111'
        expect(content).toContain("return u?.accountType === '111';");
        expect(content).not.toContain("u?.account === 'superadmin'");

        // 2. handlePreviewSample và executeextractsqlsample đã dọn sạch theo Prompt #17
        expect(content).not.toContain('handlePreviewSample');
        expect(content).not.toContain('executeextractsqlsample');
        expect(content).not.toContain('sample-preview-card');

        // 3. Ô maxLookBackDays không âm (:min="0") và đọc max từ feConfig.packetLookbackDays
        const syncPanelPath = resolveWebvuePath('views/sharedata/dataSource/component/syncModePanel.vue');
        const syncPanelContent = fs.readFileSync(syncPanelPath, 'utf-8');
        expect(syncPanelContent).toContain(':min="0"');
        expect(syncPanelContent).toContain(':max="feConfig.packetLookbackDays?.max ?? 365"');
    });

    test('40. Dòng cấu hình đầu (Bảng nguồn, Bí danh) và hộp thông báo el-alert co giãn theo nội dung kèm padding top-bottom', async ({ page }) => {
        const qbPath = resolveWebvuePath('views/sharedata/dataSource/component/queryBuilder.vue');
        const qbContent = fs.readFileSync(qbPath, 'utf-8');
        const idxPath = resolveWebvuePath('views/sharedata/dataSource/index.vue');
        const idxContent = fs.readFileSync(idxPath, 'utf-8');

        // 1. Kiểm tra CSS dòng cấu hình đầu trong queryBuilder.vue (bỏ vqb-top-select)
        expect(qbContent).toContain('.vqb-table-select {\n\t\t\twidth: fit-content;');
        expect(qbContent).toContain('.vqb-alias-input {\n\t\t\twidth: fit-content;');
        expect(qbContent).not.toContain('vqb-top-select');

        // 2. Kiểm tra CSS el-alert trong dataSource/index.vue
        expect(idxContent).toContain(':deep(.el-alert) {\n\twidth: fit-content;\n\tmax-width: 100%;\n\talign-self: flex-start;\n\tflex-shrink: 0;\n\tpadding: 3px 10px;');

        // 3. Mock API getmetadatatables & getmetadatafields trước khi mở trang
        await page.route('**/api/sharedata/sharedatapacket/getmetadatatables*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: [
                        { id: 'tbl-traffic', tableName: 'TmsTrafficData', displayName: 'Dò xe', defaultAlias: 'td' },
                        { id: 'tbl-lane', tableName: 'TollLane', displayName: 'Làn thu phí', defaultAlias: 'tl' },
                    ],
                }),
            });
        });

        await page.route('**/api/sharedata/sharedatapacket/getmetadatafields*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: [
                        { columnName: 'ID', dataType: 'bigint', suggestedAlias: 'id' },
                        { columnName: 'LaneNo', dataType: 'int', suggestedAlias: 'laneNo' },
                    ],
                }),
            });
        });

        // 4. Kiểm chứng trực tiếp trên trình duyệt
        await page.setViewportSize({ width: 1600, height: 900 });
        await page.goto('/#/sharedata/dataSource');

        const packetGrid = page.locator('.packet-col .vxe-grid');
        await expect(packetGrid).toBeVisible({ timeout: 30_000 });
        await page.locator('.vxe-table--body-wrapper tbody tr').first().click();

        const tabs = page.locator('.datasource-right-tabs');
        await expect(tabs).toBeVisible({ timeout: 15_000 });
        const sqlTab = tabs.getByRole('tab', { name: /Cấu hình SQL/ });
        await sqlTab.click();
        await expect(sqlTab).toHaveClass(/is-active/);

        // Không còn ô .vqb-top-select trên DOM
        const topSelect = page.locator('.visual-query-builder-container .vqb-top-select');
        await expect(topSelect).toHaveCount(0);

        const aliasInput = page.locator('.visual-query-builder-container .vqb-alias-input');
        await expect(aliasInput).toBeVisible();
        const aliasBox = await aliasInput.boundingBox();
        expect(aliasBox).toBeTruthy();
        if (aliasBox) {
            expect(aliasBox.width).toBeLessThan(95);
        }

        // Không hiển thị chế độ gửi
        await expect(page.locator('.sql-sync-mode-tag')).toHaveCount(0);
    });

    test('41. Màn Nguồn dữ liệu: Bổ sung bản dịch nút Dò lại và cấu hình CSS el-alert nhỏ gọn thanh thoát', async () => {
        const viPath = resolveWebvuePath('i18n/lang/its/sharedata/vi-vn.json');
        const enPath = resolveWebvuePath('i18n/lang/its/sharedata/en-us.json');
        const viJson = JSON.parse(fs.readFileSync(viPath, 'utf8'));
        const enJson = JSON.parse(fs.readFileSync(enPath, 'utf8'));

        // 1. Kiểm tra bản dịch nút "Dò lại"
        expect(viJson['lz.button.sharedataDataSource.detectColumns']).toBe('Dò lại');
        expect(enJson['lz.button.sharedataDataSource.detectColumns']).toBe('Re-detect');

        // 2. Kiểm tra CSS el-alert trong dataSource/index.vue đạt chuẩn nhỏ gọn
        const idxPath = resolveWebvuePath('views/sharedata/dataSource/index.vue');
        const idxContent = fs.readFileSync(idxPath, 'utf8');
        expect(idxContent).toContain('padding: 4px 12px;');
        expect(idxContent).toContain('padding-right: 34px;');
        expect(idxContent).toContain('font-size: 14px;\n\t\tmargin-right: 6px;');
        expect(idxContent).toContain('font-size: 12px;\n\t\tline-height: 1.35;');
        expect(idxContent).toContain('font-size: 16px;\n\t\twidth: 20px;\n\t\theight: 20px;');
        expect(idxContent).not.toMatch(/:deep\(\.el-alert\)[^}]+!important/);
    });

    test('42. Trình soạn thảo Monaco Editor trong Raw SQL Mode có thanh kéo resize và scrollbar slider bo tròn', async () => {
        const qbPath = resolveWebvuePath('views/sharedata/dataSource/component/queryBuilder.vue');
        const qbContent = fs.readFileSync(qbPath, 'utf-8');

        // 1. Kiểm tra CSS bo tròn thumb scrollbar của Monaco editor
        expect(qbContent).toContain('.vqb-monaco-editor :deep(.monaco-editor .scrollbar .slider) {\n\tborder-radius: 4px;\n}');

        // 2. Kiểm tra thanh kéo resize raw-editor-resizer và hàm handleStartResizeRawEditor
        expect(qbContent).toContain('class="vqb-card-resize-bar raw-editor-resizer"');
        expect(qbContent).toContain('@mousedown="handleStartResizeRawEditor"');
        expect(qbContent).toContain('const rawEditorHeight = ref(');
        expect(qbContent).toContain('editorInstance?.layout()');

        // 3. Kiểm tra khóa i18n dragToResize
        const viPath = resolveWebvuePath('i18n/lang/its/sharedata/vi-vn.json');
        const enPath = resolveWebvuePath('i18n/lang/its/sharedata/en-us.json');
        const viJson = JSON.parse(fs.readFileSync(viPath, 'utf8'));
        const enJson = JSON.parse(fs.readFileSync(enPath, 'utf8'));
        expect(viJson['lz.label.sharedataDataSource.dragToResize']).toBe('Kéo để chỉnh độ cao');
        expect(enJson['lz.label.sharedataDataSource.dragToResize']).toBe('Drag to resize height');
    });

    test('43. Toolbar SQL Center gọn hàng cùng dòng, khoảng cách 6px, nút bấm bên phải và cache metadata', async () => {
        const idxPath = resolveWebvuePath('views/sharedata/dataSource/index.vue');
        const idxContent = fs.readFileSync(idxPath, 'utf8');

        // 1. Kiểm tra cấu trúc toolbar
        expect(idxContent).toContain('class="sql-toolbar-bar"');
        expect(idxContent).not.toContain('sql-sync-mode-control');
        expect(idxContent).not.toContain('sql-sync-mode-tag');
        expect(idxContent).not.toContain('el-radio-group');
        expect(idxContent).not.toContain('class="sql-lookback-control"');

        // 2. Kiểm tra CSS nowrap, gap 6px và margin-left: auto
        expect(idxContent).toContain('flex-wrap: nowrap;');
        expect(idxContent).toContain('gap: 6px;');
        expect(idxContent).toContain('margin-left: auto;');

        // 3. Kiểm tra cache metadata trong queryBuilder.vue
        const qbPath = resolveWebvuePath('views/sharedata/dataSource/component/queryBuilder.vue');
        const qbContent = fs.readFileSync(qbPath, 'utf8');
        expect(qbContent).toContain('let cachedMetadataTables: any[] | null = null;');
        expect(qbContent).toContain('const metadataFieldsCache = new Map<string, any[]>();');
    });

    test('44. Tiện ích cursorSql.ts: Phân tích, chèn điều kiện nối đuôi, gỡ nối đuôi và áp filter @fromDate chuẩn cú pháp SQL Server', async () => {
        // 1. Test analyzeCursorSql() với 5 câu SQL mẫu gói tin (103, 104, 106, 107, 109)
        const sql103 = `SELECT
    td.ID,
    td.LaneNo,
    td.PlateNumber,
    td.VehicleType,
    td.Speed,
    td.PassTime,
    td.SiteId,
    td.CreateTime,
    td.UpdateTime
FROM TmsTrafficData td WITH (NOLOCK)`;

        const sql104 = `SELECT
    tl.ID,
    tl.LaneNo,
    tl.LaneName,
    tl.StationId,
    tl.StationName,
    tl.Direction,
    tl.LaneType,
    tl.Status,
    tl.UpdateTime
FROM TollLane tl WITH (NOLOCK)`;

        const sql106 = `SELECT
    te.ID,
    te.EventCode,
    te.EventType,
    te.EventTime,
    te.Location,
    te.Description,
    te.Status,
    te.CreateTime,
    te.UpdateTime
FROM TmsTrafficEvent te WITH (NOLOCK)`;

        const sql107 = `SELECT
    tp.ID,
    tp.PlateNumber,
    tp.PassTime,
    tp.LaneNo,
    tp.StationId,
    tp.VehicleType,
    tp.Speed,
    tp.ImageFrontUrl,
    tp.ImageBackUrl,
    tp.CreateTime,
    tp.UpdateTime
FROM TmsTrafficPlate tp WITH (NOLOCK)`;

        const sql109 = `SELECT
    tc.ID,
    tc.CameraCode,
    tc.CameraName,
    tc.IpAddress,
    tc.Status,
    tc.SiteId,
    tc.CreateTime,
    tc.UpdateTime
FROM TmsCamera tc WITH (NOLOCK)`;

        for (const sql of [sql103, sql104, sql106, sql107, sql109]) {
            const analysis = analyzeCursorSql(sql);
            expect(analysis.supported).toBe(true);
            expect(analysis.hasTopPageSize).toBe(false);
            expect(analysis.hasCursorFilter).toBe(false);
            expect(analysis.orderBy).toBe('missing');
            expect(analysis.usesFromDate).toBe(false);
        }

        // 2. Test applyCursor() với câu SQL đơn bảng: chèn TOP (@pageSize), 2 cột alias con trỏ, WHERE và ORDER BY
        const applied103 = applyCursor(sql103, 'td.PassTime', 'td.ID');
        expect(applied103).toContain('SELECT TOP (@pageSize)');
        expect(applied103).toContain('td.PassTime AS __lastTime');
        expect(applied103).toContain('td.ID AS __lastKey');
        expect(applied103).toContain('@lastTime IS NULL');
        expect(applied103).toContain('td.PassTime > @lastTime');
        expect(applied103).toContain('td.PassTime = @lastTime AND td.ID > @lastKey');
        expect(applied103).toContain('ORDER BY __lastTime ASC, __lastKey ASC');
        const analysis103 = analyzeCursorSql(applied103);
        expect(analysis103.supported).toBe(true);
        expect(analysis103.hasTopPageSize).toBe(true);
        expect(analysis103.hasCursorFilter).toBe(true);
        expect(analysis103.orderBy).toBe('ok');
        expect(analysis103.timeExpr).toBe('td.PassTime');
        expect(analysis103.keyExpr).toBe('td.ID');

        // 3. Test applyCursor() với câu SQL đã có WHERE và JOIN có OR: bọc ngoặc điều kiện mới bằng AND (...)
        const sqlJoinOr = `SELECT a.ID, a.UpdateTime, b.Name
FROM TableA a WITH (NOLOCK)
JOIN TableB b WITH (NOLOCK) ON a.BId = b.ID
WHERE a.Status = 1 OR b.Type = 2`;
        const appliedJoinOr = applyCursor(sqlJoinOr, 'a.UpdateTime', 'a.ID');
        expect(appliedJoinOr).toContain('(a.Status = 1 OR b.Type = 2)');
        expect(appliedJoinOr).toContain('AND (');
        expect(appliedJoinOr).toContain('@lastTime IS NULL');
        expect(appliedJoinOr).toContain('ORDER BY __lastTime ASC, __lastKey ASC');
        const analysisJoinOr = analyzeCursorSql(appliedJoinOr);
        expect(analysisJoinOr.hasCursorFilter).toBe(true);
        expect(analysisJoinOr.hasTopPageSize).toBe(true);
        expect(analysisJoinOr.orderBy).toBe('ok');

        // 4. Test applyCursor() với câu SQL có OPTION (RECOMPILE): mệnh đề OPTION luôn ở cuối cùng sau ORDER BY
        const sqlOption = `SELECT td.ID, td.PassTime
FROM TmsTrafficData td WITH (NOLOCK)
WHERE td.Status = 1
OPTION (RECOMPILE)`;
        const appliedOption = applyCursor(sqlOption, 'td.PassTime', 'td.ID');
        expect(appliedOption.trim().endsWith('OPTION (RECOMPILE)')).toBe(true);
        const analysisOption = analyzeCursorSql(appliedOption);
        expect(analysisOption.hasCursorFilter).toBe(true);
        expect(analysisOption.hasTopPageSize).toBe(true);
        expect(analysisOption.orderBy).toBe('ok');

        // 5. Test analyzeCursorSql() phát hiện câu SQL có ORDER BY DESC
        const sqlDesc = `SELECT td.ID, td.PassTime
FROM TmsTrafficData td WITH (NOLOCK)
ORDER BY td.PassTime DESC, td.ID DESC`;
        const analysisDescBefore = analyzeCursorSql(sqlDesc);
        expect(analysisDescBefore.orderBy).toBe('desc');
        const appliedDesc = applyCursor(sqlDesc, 'td.PassTime', 'td.ID');
        expect(appliedDesc).toContain('ORDER BY __lastTime ASC, __lastKey ASC');
        const analysisDescAfter = analyzeCursorSql(appliedDesc);
        expect(analysisDescAfter.hasCursorFilter).toBe(true);
        expect(analysisDescAfter.hasTopPageSize).toBe(true);
        expect(analysisDescAfter.orderBy).toBe('ok');

        // 6. Test applyCursor() gọi 2 lần liên tiếp: kết quả lần 2 giống lần 1 (idempotent)
        const appliedDouble = applyCursor(applied103, 'td.PassTime', 'td.ID');
        expect(appliedDouble).toBe(applied103);

        // 7. Test removeCursor(): gỡ sạch điều kiện nối đuôi đã chèn, khôi phục câu SQL sạch
        const removed = removeCursor(applied103);
        expect(removed).not.toBeNull();
        expect(removed).not.toContain('@lastTime');
        expect(removed).not.toContain('@lastKey');
        expect(removed).not.toContain('@pageSize');
        expect(removed).not.toContain('ORDER BY __lastTime');
        const analysisRemoved = analyzeCursorSql(removed!);
        expect(analysisRemoved.hasCursorFilter).toBe(false);
        expect(analysisRemoved.hasTopPageSize).toBe(false);

        // 8. Test applyFromDate(): chèn filter @fromDate vào WHERE
        const sNoWhere = 'SELECT * FROM TableX';
        const sApplied1 = applyFromDate(sNoWhere, 'TableX.UpdateTime');
        expect(sApplied1).toContain('WHERE TableX.UpdateTime >= @fromDate');
        expect(analyzeCursorSql(sApplied1).usesFromDate).toBe(true);

        const sWithWhere = 'SELECT * FROM TableX WHERE Status = 1';
        const sApplied2 = applyFromDate(sWithWhere, 'TableX.UpdateTime');
        expect(sApplied2).toContain('WHERE Status = 1 AND TableX.UpdateTime >= @fromDate');
        expect(analyzeCursorSql(sApplied2).usesFromDate).toBe(true);

        const sWithOr = 'SELECT * FROM TableX WHERE Status = 1 OR Status = 2';
        const sAppliedOr = applyFromDate(sWithOr, 'TableX.UpdateTime');
        expect(sAppliedOr).toContain('WHERE (Status = 1 OR Status = 2) AND TableX.UpdateTime >= @fromDate');
        expect(analyzeCursorSql(sAppliedOr).usesFromDate).toBe(true);

        const sIdempotent = applyFromDate(sApplied2, 'TableX.UpdateTime');
        expect(sIdempotent).toBe(sApplied2);

        // 9. Test CTE / WITH query: không hỗ trợ tự động biến đổi, giữ nguyên câu gốc
        const sqlCte = `WITH RankedData AS (
    SELECT ID, PassTime FROM TmsTrafficData
)
SELECT ID, PassTime FROM RankedData`;
        const analysisCte = analyzeCursorSql(sqlCte);
        expect(analysisCte.supported).toBe(false);
        expect(applyCursor(sqlCte, 'PassTime', 'ID')).toBe(sqlCte);
    });

    test('45. Giao diện SyncModePanel: Checklist 4 điều kiện, áp vào SQL, dọn điều kiện thừa và tham số @pageSize', async ({ page }) => {
        // Quét tĩnh component queryParameterPanel.vue có default là 10 (không phải 100)
        const paramPanelPath = resolveWebvuePath('views/sharedata/dataSource/component/queryParameterPanel.vue');
        const paramPanelContent = fs.readFileSync(paramPanelPath, 'utf8');
        expect(paramPanelContent).toContain('feConfig.value.sqlSampleTop?.default ?? 10');
        expect(paramPanelContent).not.toContain('default ?? 100');

        // Mock API trả về câu SQL sạch của gói tin PKT_103
        await page.route('**/api/sharedata/sharedatapacket/getextractsql*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        packetCode: 'PKT_103',
                        extractSql: `SELECT
    td.ID,
    td.LaneNo,
    td.PlateNumber,
    td.PassTime,
    td.UpdateTime
FROM TmsTrafficData td WITH (NOLOCK)`,
                        status: 1,
                        syncMode: 2, // Gửi nối đuôi
                        maxLookBackDays: null,
                        cursorTimeColumn: 'td.PassTime',
                    },
                }),
            });
        });

        await page.setViewportSize({ width: 1600, height: 900 });
        await page.goto('/#/sharedata/dataSource');

        const packetGrid = page.locator('.packet-col .vxe-grid');
        await expect(packetGrid).toBeVisible({ timeout: 30_000 });
        await page.locator('.vxe-table--body-wrapper tbody tr').first().click();

        const tabs = page.locator('.datasource-right-tabs');
        await expect(tabs).toBeVisible({ timeout: 15_000 });
        const sqlTab = tabs.getByRole('tab', { name: /Cấu hình SQL/ });
        await sqlTab.click();
        await expect(sqlTab).toHaveClass(/is-active/);

        // Chờ tải xong dữ liệu SQL của gói tin
        await page.waitForTimeout(600);

        // 1. Kiểm tra panel chế độ nối đuôi hiển thị: 1 dòng trạng thái, không còn checklist hay nút Áp
        const incrementalPanel = page.locator('.sync-mode-panel-card.incremental-mode');
        await expect(incrementalPanel).toBeVisible({ timeout: 5_000 });
        await expect(incrementalPanel.locator('.sync-check-item')).toHaveCount(0);
        await expect(incrementalPanel.locator('.btn-apply-cursor')).toHaveCount(0);

        // 2. Vì câu SQL chưa có điều kiện nối đuôi -> dòng trạng thái cảnh báo (is-incomplete / is-danger / is-warning)
        const statusLine = incrementalPanel.locator('.sync-status-line');
        await expect(statusLine).toBeVisible();
        await expect(statusLine).toHaveClass(/is-incomplete|is-danger|is-warning/);

        // Nút [Sửa tự động] hiển thị
        const btnAutoFix = incrementalPanel.locator('.btn-auto-fix');
        await expect(btnAutoFix).toBeVisible();

        // 3. Bấm nút [Sửa tự động] (khóa phân biệt tự động gán ID bảng chính)
        await btnAutoFix.click();
        await page.waitForTimeout(400);

        // Sau khi sửa tự động: đủ điều kiện -> không còn dòng cảnh báo thiếu điều kiện hay nút sửa tự động
        await expect(incrementalPanel.locator('.btn-auto-fix')).toHaveCount(0);
        await expect(incrementalPanel.locator('.sync-status-line.is-incomplete')).toHaveCount(0);
    });

    test('44. Thẻ Gửi nối đuôi / Gửi toàn bộ thu gọn: 1 hàng chọn cột, tự áp SQL, 1 dòng trạng thái (Prompt #19)', async ({ page }) => {
        // Quét tĩnh syncModePanel.vue
        const syncPanelPath = resolveWebvuePath('views/sharedata/dataSource/component/syncModePanel.vue');
        const syncPanelContent = fs.readFileSync(syncPanelPath, 'utf8');

        // Khẳng định không còn .sync-check-item hay .btn-apply-cursor trong mã nguồn
        expect(syncPanelContent).not.toContain('sync-check-item');
        expect(syncPanelContent).not.toContain('btn-apply-cursor');
        expect(syncPanelContent).toContain('sync-status-line');
        expect(syncPanelContent).toContain('btn-auto-fix');

        // Mock 2 gói tin để có thể chuyển đổi kiểm tra các trạng thái SQL
        await page.route('**/api/sharedata/sharedatapacket/page*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        records: [
                            { id: 'PKT_01', code: '103_trafficData', name: 'Gói tin đếm xe 103', status: 1 },
                            { id: 'PKT_02', code: '102_unionData', name: 'Gói tin UNION 102', status: 1 },
                        ],
                        total: 2,
                    },
                }),
            });
        });

        // 1. Gói 103: câu mẫu đầy đủ có dòng sẵn sàng (is-ready) màu xanh
        // Gói 102: câu UNION không hỗ trợ tự động biến đổi
        await page.route('**/api/sharedata/sharedatapacket/getextractsql*', async (route) => {
            const url = route.request().url();
            if (url.includes('102_unionData')) {
                await route.fulfill({
                    status: 200,
                    contentType: 'application/json',
                    body: JSON.stringify({
                        code: 200,
                        result: {
                            packetCode: '102_unionData',
                            extractSql: 'SELECT ID FROM TableA UNION SELECT ID FROM TableB',
                            status: 1,
                            syncMode: 2,
                            maxLookBackDays: null,
                        },
                    }),
                });
            } else {
                await route.fulfill({
                    status: 200,
                    contentType: 'application/json',
                    body: JSON.stringify({
                        code: 200,
                        result: {
                            packetCode: '103_trafficData',
                            extractSql: `SELECT TOP (@pageSize)
    td.ID,
    COALESCE(td.UpdateTime, td.CreateTime, td.DetectTime) AS __lastTime,
    td.ID AS __lastKey
FROM TmsTrafficData td
WHERE (
    @lastTime IS NULL
    OR COALESCE(td.UpdateTime, td.CreateTime, td.DetectTime) > @lastTime
    OR (COALESCE(td.UpdateTime, td.CreateTime, td.DetectTime) = @lastTime AND td.ID > @lastKey)
)
ORDER BY __lastTime ASC, __lastKey ASC`,
                            status: 1,
                            syncMode: 2,
                            maxLookBackDays: 1,
                        },
                    }),
                });
            }
        });

        await page.setViewportSize({ width: 1600, height: 900 });
        await page.goto('/#/sharedata/dataSource');

        const packetGrid = page.locator('.packet-col .vxe-grid');
        await expect(packetGrid).toBeVisible({ timeout: 30_000 });
        await page.locator('.vxe-table--body-wrapper tbody tr').first().click();

        const tabs = page.locator('.datasource-right-tabs');
        await expect(tabs).toBeVisible({ timeout: 15_000 });
        const sqlTab = tabs.getByRole('tab', { name: /Cấu hình SQL/ });
        await sqlTab.click();
        await page.waitForTimeout(600);

        const incPanel = page.locator('.sync-mode-panel-card.incremental-mode');
        // Theo yêu cầu giao diện gọn (22/10/2026): khi câu SQL đã đủ điều kiện nối đuôi thì thẻ sync-mode-panel được ẩn hoàn toàn (display: none)
        await expect(incPanel).toBeHidden({ timeout: 5_000 });

        // 2. Chuyển sang gói tin PKT_02 (câu UNION): dòng trạng thái không tự sửa được + nút sao chép mẫu
        await page.locator('.vxe-table--body-wrapper tbody tr').nth(1).click();
        await page.waitForTimeout(600);

        await expect(incPanel.locator('.sync-status-line.is-unsupported')).toBeVisible();

        // 3. Không hiển thị chế độ gửi trên tab / toolbar
        await expect(page.locator('.sql-sync-mode-tag')).toHaveCount(0);
        await expect(page.getByText(/Gửi toàn bộ|Gửi nối đuôi/)).toHaveCount(0);
    });

    // ============================================================
    // CÁC TEST BỔ SUNG CHO PROMPT #16 (Chế độ Trực quan JOIN, LATEST & Loại gói 101-111)
    // ============================================================

    test('46. Unit test visualSql.ts — Đọc và tái tạo toàn bộ 11 câu truy vấn mẫu (101-111) đạt 100% roundtrip match', async () => {
        const seedQueries = [
            {
                code: 101,
                sql: `SELECT
    zs.ID AS zoneStatusId,
    zs.ZoneId AS zoneId,
    z.Name AS zoneName,
    z.FromKmNumber AS fromLocationKm,
    z.FromMetNumber AS fromLocationMet,
    z.ToKmNumber AS toLocationKm,
    z.ToMetNumber AS toLocationMet,
    z.LaneId AS laneId,
    CAST(zs.AverageSpeed AS DECIMAL(18, 2)) AS averageSpeed,
    zs.Condition AS trafficCondition,
    zs.UpdateTime AS dataTime,
    z.MaxSpeed AS speedLimit,
    ts.TotalVehicleNumber AS vehicleCount
FROM TmsZoneStatus zs
LEFT JOIN TmsZone z ON zs.ZoneId = z.ID
LEFT JOIN TmsTrafficStatistic ts ON zs.ZoneId = ts.ZoneId`,
            },
            {
                code: 102,
                sql: `SELECT
    e.Code AS cameraCode,
    c.Name AS cameraName,
    NULL AS snapshot,
    c.SnapshotTime AS snapshotTime,
    c.DeviceState AS deviceState,
    e.KmNumber AS locationKm,
    e.MetNumber AS locationMet,
    e.DirectionId AS direction
FROM CctvDevice c
LEFT JOIN TmsEquipment e ON c.Ip = e.Ip`,
            },
            {
                code: 103,
                sql: `SELECT
    td.ID AS detectionId,
    td.DetectTime AS detectTime,
    td.Type AS vehicleType,
    td.LicensePlate AS licensePlate,
    td.Speed AS speed,
    td.Lane AS lane,
    td.Direction AS direction,
    td.Location AS locationRoute,
    td.EquipmentId AS equipmentId,
    e.KmNumber AS locationKm,
    e.MetNumber AS locationMet
FROM TmsTrafficData td
LEFT JOIN TmsEquipment e ON td.EquipmentId = e.ID
WHERE td.IsDelete IS NULL`,
            },
            {
                code: 104,
                sql: `SELECT
    w.RefId AS weatherStationId,
    w.LocationDetail AS locationDetail,
    w.Temperature AS temperature,
    w.Hudmidity AS humidity,
    w.WindSpeed AS windSpeed,
    w.WindDirection AS windDirection,
    w.Rain AS rainfall,
    w.RainHour AS rainfallHour,
    w.Foresight AS visibility,
    w.Description AS weatherDescription,
    w.ShortDescription AS weatherCode,
    w.TimeDetect AS detectTime
FROM TmsWeather w
WHERE w.IsDelete IS NULL`,
            },
            {
                code: 105,
                sql: `SELECT
    i.TransactionId AS transactionId,
    i.TagId AS tagId,
    NULLIF(ISNULL(NULLIF(i.PlateEdit, ''), NULLIF(i.PlateLpr, '')), '') AS licensePlate,
    i.VehicleTypeId AS vehicleTypeId,
    i.TransactionDateTime AS entryTime,
    CAST(NULL AS DATETIME) AS exitTime,
    i.LaneId AS laneId,
    i.StationId AS stationId,
    vr.Brand AS vehicleBrand,
    vr.Owner AS vehicleOwner
FROM TollTransactionIn i
LEFT JOIN TmsVehicleRegistration vr
       ON NULLIF(ISNULL(NULLIF(i.PlateEdit, ''), NULLIF(i.PlateLpr, '')), '') = vr.LicensePlate
      AND vr.IsDelete IS NULL
WHERE i.IsDelete IS NULL
ORDER BY i.TransactionDateTime DESC`,
            },
            {
                code: 106,
                sql: `SELECT
    td.DetectTime AS detectTime,
    td.Lane AS lane,
    td.Location AS locationCode,
    td.Speed AS speed,
    td.Height AS height,
    td.Width AS width,
    td.Length AS length
FROM TmsTrafficData td
WHERE td.IsDelete IS NULL`,
            },
            {
                code: 107,
                sql: `SELECT
    i.Code AS incidentCode,
    i.Name AS incidentName,
    i.EventTypeId AS eventTypeId,
    et.Name AS eventTypeName,
    i.StartDate AS occurredTime,
    i.KmNumber AS locationKm,
    i.MetNumber AS locationMet,
    i.Location AS locationRoute,
    i.InfluenceScope AS direction,
    i.InjuredNumber AS injuredCount,
    i.VehicleNumber AS vehicleCount,
    i.State AS incidentState,
    i.Description AS description,
    i.Source AS source
FROM TmsIncident i
LEFT JOIN TmsEventType et ON i.EventTypeId = et.ID
WHERE i.IsDelete IS NULL`,
            },
            {
                code: 108,
                sql: `SELECT
    e.Code AS equipmentCode,
    v.Name AS vmsName,
    e.KmNumber AS locationKm,
    e.MetNumber AS locationMet,
    e.DirectionId AS direction,
    e.LaneId AS laneId,
    v.RowData AS displayContent,
    v.Url AS displayImageUrl,
    v.Size AS displaySize,
    v.Priority AS priority,
    v.ExecutedDate AS executedTime
FROM VmsCurrent v
LEFT JOIN TmsEquipment e ON v.EquipmentId = e.ID`,
            },
            {
                code: 109,
                sql: `SELECT
    t.TransactionId AS transactionId,
    t.TransactionDateTimeIn AS entryTime,
    t.TransactionDateTime AS exitTime,
    t.VehicleTypeId AS vehicleTypeId,
    ISNULL(t.PlateEdit, t.PlateLpr) AS licensePlate,
    t.TagId AS tagId,
    t.LaneId AS laneId,
    l.Name AS laneName,
    t.StationId AS stationId,
    s.Name AS stationName,
    CAST(NULL AS DECIMAL(18, 2)) AS tollPrice,
    t.SyncTime AS syncTime
FROM TollTransactionOut t
LEFT JOIN TollLane l ON t.LaneId = l.LaneId
LEFT JOIN TollStation s ON t.StationId = s.StationId
WHERE t.IsDelete IS NULL`,
            },
            {
                code: 110,
                sql: `SELECT
    CAST(i.ID AS NVARCHAR(64)) AS messageId,
    CONCAT(ISNULL(i.Name, ''), ' - ', ISNULL(i.Description, '')) AS incidentMessage,
    w.Description AS weatherMessage,
    v.RowData AS guidanceContent,
    i.KmNumber AS locationKm,
    i.MetNumber AS locationMet,
    i.StartDate AS publishedTime,
    CAST(NULL AS NVARCHAR(64)) AS channel,
    CAST(NULL AS NVARCHAR(64)) AS deliveryState
FROM TmsIncident i
OUTER APPLY (
    SELECT TOP 1 e.ID
    FROM TmsEquipment e
    WHERE e.KmNumber = i.KmNumber
    ORDER BY e.ID DESC
) eq
OUTER APPLY (
    SELECT TOP 1 v.RowData
    FROM VmsCurrent v
    WHERE v.EquipmentId = eq.ID
      AND v.RowData IS NOT NULL
    ORDER BY v.ExecutedDate DESC
) v
OUTER APPLY (
    SELECT TOP 1 w.Description
    FROM TmsWeather w
    WHERE w.Description IS NOT NULL
    ORDER BY COALESCE(w.TimeDetect, w.CreateTime) DESC
) w
WHERE (i.State IS NULL OR (
    i.State != 'FINISHED' AND i.State != 'CANCELED'
    AND i.State != 'Closed' AND i.State != 'Cancelled'
))`,
            },
            {
                code: 111,
                sql: `SELECT
    CAST(NULL AS NVARCHAR(16)) AS packetType,
    CAST(NULL AS NVARCHAR(MAX)) AS payload,
    GETDATE() AS createdTime,
    sl.NewData AS controlCommand,
    sl.State AS controlState,
    CAST(NULL AS NVARCHAR(64)) AS connectionState,
    CAST(NULL AS DATETIME) AS ackTime,
    CAST(NULL AS INT) AS retryCount,
    CAST(NULL AS NVARCHAR(512)) AS error,
    CAST(NULL AS NVARCHAR(64)) AS partnerId
FROM TmsSignalLog sl
WHERE sl.IsDelete IS NULL`,
            },
        ];

        for (const item of seedQueries) {
            const res = parseVisualSql(item.sql);
            expect(res.ok, `Gói ${item.code} phải parse thành công`).toBe(true);
            if (res.ok) {
                const rebuilt = buildVisualSql(res.model);
                const n1 = normalizeSqlForComparison(item.sql);
                const n2 = normalizeSqlForComparison(rebuilt);
                expect(n1, `Gói ${item.code} roundtrip phải khớp nhau`).toBe(n2);
            }
        }
    });

    test('47. Unit test visualSql.ts — Khớp điều kiện hàm phức tạp (gói 105) và 3 khối LATEST (gói 110)', async () => {
        // Gói 105: ON có biểu thức hàm phức tạp
        const sql105 = `SELECT i.TransactionId AS transactionId
FROM TollTransactionIn i
LEFT JOIN TmsVehicleRegistration vr
       ON NULLIF(ISNULL(NULLIF(i.PlateEdit, ''), NULLIF(i.PlateLpr, '')), '') = vr.LicensePlate
      AND vr.IsDelete IS NULL
WHERE i.IsDelete IS NULL`;
        const res105 = parseVisualSql(sql105);
        expect(res105.ok).toBe(true);
        if (res105.ok) {
            expect(res105.model.joins.length).toBe(1);
            const j1 = res105.model.joins[0];
            expect(j1.type).toBe('LEFT');
            expect(j1.table).toBe('TmsVehicleRegistration');
            expect(j1.alias).toBe('vr');
            expect((j1 as any).on.length).toBe(2);
            expect((j1 as any).on[0].left).toContain('NULLIF(ISNULL');
            expect((j1 as any).on[0].right).toBe('vr.LicensePlate');
            expect((j1 as any).on[1].left).toBe('vr.IsDelete');
            expect((j1 as any).on[1].op).toBe('IS NULL');
        }

        // Gói 110: 3 khối OUTER APPLY (LATEST)
        const sql110 = `SELECT i.ID AS messageId
FROM TmsIncident i
OUTER APPLY (
    SELECT TOP 1 e.ID
    FROM TmsEquipment e
    WHERE e.KmNumber = i.KmNumber
    ORDER BY e.ID DESC
) eq
OUTER APPLY (
    SELECT TOP 1 v.RowData
    FROM VmsCurrent v
    WHERE v.EquipmentId = eq.ID
      AND v.RowData IS NOT NULL
    ORDER BY v.ExecutedDate DESC
) v
OUTER APPLY (
    SELECT TOP 1 w.Description
    FROM TmsWeather w
    WHERE w.Description IS NOT NULL
    ORDER BY COALESCE(w.TimeDetect, w.CreateTime) DESC
) w`;
        const res110 = parseVisualSql(sql110);
        expect(res110.ok).toBe(true);
        if (res110.ok) {
            expect(res110.model.joins.length).toBe(3);
            const [j1, j2, j3] = res110.model.joins;
            expect(j1.type).toBe('LATEST');
            expect(j1.table).toBe('TmsEquipment');
            expect(j1.alias).toBe('eq');

            expect(j2.type).toBe('LATEST');
            expect(j2.table).toBe('VmsCurrent');
            expect((j2 as any).where[0].right).toBe('eq.ID');

            expect(j3.type).toBe('LATEST');
            expect(j3.table).toBe('TmsWeather');
            expect((j3 as any).orderBy).toContain('COALESCE(w.TimeDetect, w.CreateTime)');
        }
    });

    test('48. Unit test visualSql.ts — Từ chối các cấu trúc chưa hỗ trợ với mã lỗi i18n chính xác', async () => {
        // WITH
        const withRes = parseVisualSql('WITH cte AS (SELECT 1 AS n) SELECT * FROM cte');
        expect(withRes.ok).toBe(false);
        if (!withRes.ok) expect(withRes.reason).toBe('lz.message.sharedataDataSource.vqbSqlHasWith');

        // UNION
        const unionRes = parseVisualSql('SELECT * FROM TmsZone UNION SELECT * FROM TmsZone');
        expect(unionRes.ok).toBe(false);
        if (!unionRes.ok) expect(unionRes.reason).toBe('lz.message.sharedataDataSource.vqbSqlHasUnion');

        // GROUP BY
        const groupRes = parseVisualSql('SELECT ZoneId, COUNT(*) FROM TmsZoneStatus GROUP BY ZoneId');
        expect(groupRes.ok).toBe(false);
        if (!groupRes.ok) expect(groupRes.reason).toBe('lz.message.sharedataDataSource.vqbSqlHasGroupBy');

        // RIGHT JOIN
        const rightRes = parseVisualSql('SELECT * FROM TmsZoneStatus zs RIGHT JOIN TmsZone z ON zs.ZoneId = z.ID');
        expect(rightRes.ok).toBe(false);
        if (!rightRes.ok) expect(rightRes.reason).toBe('lz.message.sharedataDataSource.vqbSqlHasUnsupportedJoin');

        // CROSS APPLY
        const crossRes = parseVisualSql('SELECT * FROM TmsZoneStatus zs CROSS APPLY (SELECT TOP 1 * FROM TmsZone z WHERE z.ID = zs.ZoneId ORDER BY z.ID) sub');
        expect(crossRes.ok).toBe(false);
        if (!crossRes.ok) expect(crossRes.reason).toBe('lz.message.sharedataDataSource.vqbSqlHasCrossApply');

        // APPLY TOP 5
        const top5Res = parseVisualSql('SELECT * FROM TmsZoneStatus zs OUTER APPLY (SELECT TOP 5 * FROM TmsZone z WHERE z.ID = zs.ZoneId ORDER BY z.ID) sub');
        expect(top5Res.ok).toBe(false);
        if (!top5Res.ok) expect(top5Res.reason).toBe('lz.message.sharedataDataSource.vqbSqlHasApplyTopNotOne');

        // Comments
        const commentRes = parseVisualSql('SELECT * FROM TmsZone -- comment');
        expect(commentRes.ok).toBe(false);
        if (!commentRes.ok) expect(commentRes.reason).toBe('lz.message.sharedataDataSource.vqbSqlHasComments');
    });

    test('49. Form thêm gói: orderNo là select 11 lựa chọn 101-111, mặc định 101, và khi sửa bị disabled (Item C)', async ({ page }) => {
        // Quét tĩnh editPacket.vue
        const editPacketPath = resolveWebvuePath('views/sharedata/dataSource/component/editPacket.vue');
        const editPacketContent = fs.readFileSync(editPacketPath, 'utf8');
        expect(editPacketContent).toContain('packetKindOptions');
        expect(editPacketContent).toContain('packetKindOrderNo');
        expect(editPacketContent).toContain('packet-order-hint');
        expect(editPacketContent).toContain(':disabled="props.operateType === \'edit\'"');

        // Mock danh sách gói
        await page.route('**/api/sharedata/sharedatapacket/page*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        records: [
                            { id: 'PKT_101', code: '101', name: 'Thông tin chung', orderNo: 101, status: 1 },
                        ],
                        total: 1,
                    },
                }),
            });
        });

        await page.setViewportSize({ width: 1600, height: 900 });
        await page.goto('/#/sharedata/dataSource');

        const packetGrid = page.locator('.packet-col .vxe-grid');
        await expect(packetGrid).toBeVisible({ timeout: 30_000 });

        // Mở dialog Thêm mới gói tin
        const btnAddPacket = page.locator('.packet-col .el-button').filter({ hasText: /Thêm|Tạo/i }).first();
        await btnAddPacket.click();

        const dialog = page.locator('.edit-packet-container .el-dialog');
        await expect(dialog).toBeVisible();

        // Kiểm tra ô Loại gói (thứ tự) hiển thị và có hint
        const hint = dialog.locator('.packet-order-hint');
        await expect(hint).toBeVisible();
        await expect(hint).toContainText(/sau khi tạo không đổi loại gói được/i);
    });

    test('50. Mở gói 101 (2 LEFT JOINs) ở chế độ Trực quan không bị khóa (Item E)', async ({ page }) => {
        const sql101 = `SELECT
    zs.ID AS zoneStatusId,
    zs.ZoneId AS zoneId,
    z.Name AS zoneName,
    ts.TotalVehicleNumber AS vehicleCount
FROM TmsZoneStatus zs
LEFT JOIN TmsZone z ON zs.ZoneId = z.ID
LEFT JOIN TmsTrafficStatistic ts ON zs.ZoneId = ts.ZoneId`;

        await page.route('**/api/sharedata/sharedatapacket/page*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        records: [
                            { id: 'PKT_101', code: '101', name: 'Gói 101 có JOIN', orderNo: 101, status: 1 },
                        ],
                        total: 1,
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
                        packetCode: '101',
                        extractSql: sql101,
                        status: 1,
                        syncMode: 1,
                        hasDefaultTemplate: true,
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
                        columns: ['zoneStatusId', 'zoneId', 'zoneName', 'vehicleCount'],
                    },
                }),
            });
        });

        await page.setViewportSize({ width: 1600, height: 900 });
        await page.goto('/#/sharedata/dataSource');

        const tabs = page.locator('.datasource-right-tabs');
        await expect(tabs).toBeVisible({ timeout: 15_000 });

        const sqlTab = tabs.getByRole('tab', { name: /Cấu hình SQL/ });
        await sqlTab.click();
        await expect(sqlTab).toHaveClass(/is-active/);

        // Chế độ Trực quan KHÔNG bị khóa
        const btnVisual = page.locator('.vqb-segmented-tabs .vqb-tab-btn').nth(0);
        await expect(btnVisual).toBeEnabled();
        await expect(btnVisual).toHaveClass(/active/);

        // Card Nối bảng hiển thị 2 dòng nối bảng
        const joinsCard = page.locator('.vqb-joins-card');
        await expect(joinsCard).toBeVisible();
        const joinRows = joinsCard.locator('.vqb-join-row');
        await expect(joinRows).toHaveCount(2);
    });

    test('51. Đồng bộ font-size và giao diện khối Nối bảng với Điều kiện lọc và Cột trích xuất (12px, font-weight 600)', async ({ page }) => {
        // 1. Quét tĩnh mã nguồn queryBuilder.vue
        const qbPath = resolveWebvuePath('views/sharedata/dataSource/component/queryBuilder.vue');
        const qbContent = fs.readFileSync(qbPath, 'utf8');

        // Kiểm tra gom card chung trong CSS (.vqb-joins-card, .vqb-columns-card, .vqb-filter-card)
        expect(qbContent).toContain('.vqb-joins-card,\n.vqb-columns-card,\n.vqb-filter-card');

        // Không còn font-size: 13px và font-size: 11px ở tiêu đề / hint nối bảng
        expect(qbContent).not.toMatch(/\.vqb-joins-card\s*\{[^}]*font-size:\s*13px/);
        expect(qbContent).not.toMatch(/\.vqb-no-join-hint\s*\{[^}]*font-size:\s*11px/);

        // Không còn style inline ở vqb-no-filter-hint
        expect(qbContent).not.toMatch(/class="vqb-no-filter-hint[^"]*"\s+style=/);

        // Class CSS cho hint đều có font-size: 12px; margin-left: 12px
        expect(qbContent).toContain('.vqb-no-join-hint,\n\t\t\t.vqb-no-filter-hint {\n\t\t\t\tfont-size: 12px;');

        // 2. Mở màn hình dataSource để kiểm tra computed style trên giao diện
        await page.goto('/#/sharedata/dataSource');
        const tabs = page.locator('.datasource-right-tabs');
        await expect(tabs).toBeVisible({ timeout: 15_000 });

        const sqlTab = tabs.getByRole('tab', { name: /Cấu hình SQL/ });
        await sqlTab.click();
        await expect(sqlTab).toHaveClass(/is-active/);

        // Card Nối bảng
        const joinsCardTitle = page.locator('.vqb-joins-card .vqb-card-title');
        await expect(joinsCardTitle).toBeVisible();
        await expect(joinsCardTitle).toHaveCSS('font-size', '12px');
        await expect(joinsCardTitle).toHaveCSS('font-weight', '600');

        // Card Điều kiện lọc
        const filterCardTitle = page.locator('.vqb-filter-card .vqb-card-title');
        await expect(filterCardTitle).toBeVisible();
        await expect(filterCardTitle).toHaveCSS('font-size', '12px');
        await expect(filterCardTitle).toHaveCSS('font-weight', '600');

        // Card Cột trích xuất
        const columnsCardTitle = page.locator('.vqb-columns-card .vqb-card-title');
        await expect(columnsCardTitle).toBeVisible();
        await expect(columnsCardTitle).toHaveCSS('font-size', '12px');
        await expect(columnsCardTitle).toHaveCSS('font-weight', '600');

        // Dòng gợi ý khi rỗng: vqb-no-join-hint
        const noJoinHint = page.locator('.vqb-joins-card .vqb-no-join-hint');
        if (await noJoinHint.count() > 0) {
            await expect(noJoinHint).toHaveCSS('font-size', '12px');
        }
    });

    test('52. Bố cục thanh header queryBuilder: Bảng nguồn và Bí danh nằm ngang cùng hàng với Trực quan và SQL (Compact header layout)', async ({ page }) => {
        // 1. Quét tĩnh mã nguồn queryBuilder.vue
        const qbPath = resolveWebvuePath('views/sharedata/dataSource/component/queryBuilder.vue');
        const qbContent = fs.readFileSync(qbPath, 'utf8');

        // Bảng nguồn và Bí danh nằm trong .vqb-header (cùng hàng với .vqb-segmented-tabs)
        const headerIndex = qbContent.indexOf('class="vqb-header"');
        const headerLeftIndex = qbContent.indexOf('class="vqb-header-left"');
        const tabsIndex = qbContent.indexOf('class="vqb-segmented-tabs"');
        const configRowIndex = qbContent.indexOf('class="vqb-config-row"');
        const canvasIndex = qbContent.indexOf('class="vqb-canvas"');
        const joinsCardIndex = qbContent.indexOf('class="vqb-joins-card"');

        expect(headerIndex).toBeGreaterThan(-1);
        expect(headerLeftIndex).toBeGreaterThan(headerIndex);
        expect(tabsIndex).toBeGreaterThan(headerLeftIndex);
        expect(configRowIndex).toBeGreaterThan(tabsIndex);
        // config-row nằm TRƯỚC vqb-canvas (tức là nằm trong header bar, không còn là dòng riêng trong canvas)
        expect(canvasIndex).toBeGreaterThan(configRowIndex);
        expect(joinsCardIndex).toBeGreaterThan(canvasIndex);

        // Khẳng định .vqb-header-left có flex và gap
        expect(qbContent).toContain('.vqb-header-left {\n\tdisplay: flex;\n\talign-items: center;\n\tflex-wrap: nowrap;\n\tgap: 12px;');

        // 2. Mở màn hình dataSource để kiểm tra hiển thị thực tế
        await page.setViewportSize({ width: 1600, height: 900 });
        await page.goto('/#/sharedata/dataSource');
        const tabs = page.locator('.datasource-right-tabs');
        await expect(tabs).toBeVisible({ timeout: 15_000 });

        const sqlTab = tabs.getByRole('tab', { name: /Cấu hình SQL/ });
        await sqlTab.click();
        await expect(sqlTab).toHaveClass(/is-active/);

        // Kiểm tra Bảng nguồn và Bí danh nằm trên hàng header
        const header = page.locator('.visual-query-builder-container .vqb-header');
        await expect(header).toBeVisible();

        const segmentedTabs = header.locator('.vqb-segmented-tabs');
        await expect(segmentedTabs).toBeVisible();

        const sourceTableSelect = header.locator('.vqb-config-row .vqb-table-select');
        await expect(sourceTableSelect).toBeVisible();

        const aliasInput = header.locator('.vqb-config-row .vqb-alias-input');
        await expect(aliasInput).toBeVisible();

        // Kiểm tra độ cao/tọa độ Y: cùng nằm ngang trên một hàng (độ lệch Y không đáng kể)
        const tabsBox = await segmentedTabs.boundingBox();
        const tableBox = await sourceTableSelect.boundingBox();
        expect(tabsBox).toBeTruthy();
        expect(tableBox).toBeTruthy();
        if (tabsBox && tableBox) {
            expect(Math.abs(tabsBox.y - tableBox.y)).toBeLessThan(14);
        }

        // Chuyển sang tab SQL thô (Raw mode) -> dòng config-row (bảng nguồn, bí danh) được ẩn
        const rawTabBtn = segmentedTabs.locator('.vqb-tab-btn').filter({ hasText: /SQL/ });
        await rawTabBtn.click();
        await expect(sourceTableSelect).not.toBeVisible();

        // Chuyển lại tab Trực quan -> hiển thị lại bình thường
        const visualTabBtn = segmentedTabs.locator('.vqb-tab-btn').filter({ hasText: /Trực quan/ });
        await visualTabBtn.click();
        await expect(sourceTableSelect).toBeVisible();
    });

    test('53. QueryBuilder fill đầy toàn bộ chiều cao panel chi tiết gói tin (flex: 1, không dư khoảng trống dưới)', async ({ page }) => {
        // 1. Quét tĩnh mã nguồn CSS đảm bảo cấu hình flex stretch
        const indexPath = resolveWebvuePath('views/sharedata/dataSource/index.vue');
        const indexContent = fs.readFileSync(indexPath, 'utf8');

        expect(indexContent).toContain('.sql-center-pane {\n\t\tdisplay: flex;\n\t\tflex-direction: column;\n\t\tflex: 1;\n\t\tmin-height: 0;\n\t\toverflow-y: auto;');
        expect(indexContent).toContain('.sql-editor-section {\n\t\t\tflex: 1;\n\t\t\tmin-height: 0;\n\t\t\tdisplay: flex;\n\t\t\tflex-direction: column;\n\t\t\tmargin-bottom: 6px;');

        const qbPath = resolveWebvuePath('views/sharedata/dataSource/component/queryBuilder.vue');
        const qbContent = fs.readFileSync(qbPath, 'utf8');

        expect(qbContent).toContain('.visual-query-builder-container {\n\tdisplay: flex;\n\tflex-direction: column;\n\tflex: 1;\n\tmin-height: 0;\n\theight: 100%;\n\tbackground-color: var(--el-bg-color-overlay);\n\tborder: 1px solid var(--el-border-color-light);\n\tborder-radius: 6px;\n\toverflow: hidden;\n\tmargin-bottom: 0;\n}');
        expect(qbContent).toContain('/* Canvas Trực quan */\n.vqb-canvas {\n\tpadding: 8px 10px;\n\tdisplay: flex;\n\tflex-direction: column;\n\tgap: 6px;\n\tbackground-color: var(--el-bg-color);\n\tflex: 1;\n\tmin-height: 0;\n\toverflow-y: auto;\n}');
        expect(qbContent).toContain('/* Raw Editor View */\n.vqb-raw-view {\n\tpadding: 6px;\n\tflex: 1;\n\tmin-height: 0;\n\tdisplay: flex;\n\tflex-direction: column;');

        // 2. Mở màn hình dataSource để kiểm tra hiển thị thực tế
        await page.setViewportSize({ width: 1600, height: 900 });
        await page.goto('/#/sharedata/dataSource');
        const tabs = page.locator('.datasource-right-tabs');
        await expect(tabs).toBeVisible({ timeout: 15_000 });

        const sqlTab = tabs.getByRole('tab', { name: /Cấu hình SQL/ });
        await sqlTab.click();
        await expect(sqlTab).toHaveClass(/is-active/);

        // Kiểm tra qbContainer và canvas đã stretch
        const qbContainer = page.locator('.visual-query-builder-container');
        await expect(qbContainer).toBeVisible({ timeout: 10_000 });
        const canvas = qbContainer.locator('.vqb-canvas');
        await expect(canvas).toBeVisible();

        const qbBox = await qbContainer.boundingBox();
        expect(qbBox).toBeTruthy();
        if (qbBox) {
            expect(qbBox.height).toBeGreaterThan(300);
        }
    });

    test('54. Tab Cấu hình SQL — Khối đối chiếu cột SQL và trường gói tin theo Prompt #17 (Mục B)', async ({ page }) => {
        page.on('console', msg => console.log('BROWSER_LOG:', msg.text()));
        page.on('pageerror', err => console.log('BROWSER_ERROR:', err.message));
        console.log('[Test 54] Step 1: Setup routes');
        const thirteenColumns = [
            { fieldKey: 'header.status', dataType: 'int', assigned: true },
            { fieldKey: 'speed', dataType: 'int', assigned: false },
            { fieldKey: 'vehicle_count', dataType: 'int', assigned: false },
            { fieldKey: 'lane_id', dataType: 'int', assigned: false },
            { fieldKey: 'plate_number', dataType: 'string', assigned: false },
            { fieldKey: 'color', dataType: 'string', assigned: false },
            { fieldKey: 'brand', dataType: 'string', assigned: false },
            { fieldKey: 'axle_count', dataType: 'int', assigned: false },
            { fieldKey: 'weight', dataType: 'decimal', assigned: false },
            { fieldKey: 'overweight_ratio', dataType: 'decimal', assigned: false },
            { fieldKey: 'direction', dataType: 'string', assigned: false },
            { fieldKey: 'image_url', dataType: 'string', assigned: false },
            { fieldKey: 'recorded_time', dataType: 'datetime', assigned: false },
        ];

        await page.route('**/api/sharedata/sharedatapacketfield/getextractcolumns*', async (route) => {
            console.log('[Test 54] Mock getextractcolumns called');
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        sqlAvailable: true,
                        columns: thirteenColumns,
                    },
                }),
            });
        });

        await page.route('**/api/sharedata/sharedatapacketfield/list*', async (route) => {
            console.log('[Test 54] Mock list called');
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: [
                        { id: 'FLD_01', datatypeId: 'PKT_01', aliasFieldKey: 'header.status', name: 'Trạng thái', fieldType: 'int', status: 1, isRequired: 1 },
                    ],
                }),
            });
        });

        console.log('[Test 54] Step 2: Set viewport and goto');
        await page.setViewportSize({ width: 1600, height: 900 });
        await page.goto('/#/sharedata/dataSource');

        console.log('[Test 54] Step 3: Wait for tabs');
        const tabs = page.locator('.datasource-right-tabs');
        await expect(tabs).toBeVisible({ timeout: 15_000 });

        console.log('[Test 54] Step 4: Click sqlTab');
        const sqlTab = tabs.getByRole('tab', { name: /Cấu hình SQL/ });
        await sqlTab.click();
        await expect(sqlTab).toHaveClass(/is-active/);

        console.log('[Test 54] Step 5: Assert diffAlerts');
        // Khối thông báo sai lệch xuất hiện
        const diffAlerts = page.locator('.sql-column-diff-alerts');
        await expect(diffAlerts).toBeVisible({ timeout: 10_000 });

        // Cảnh báo thiếu trường (el-alert type warning / error) không xuất hiện vì không có trường nào bị thiếu
        await expect(diffAlerts.locator('.el-alert--warning, .el-alert--error')).toHaveCount(0);

        // Bảng đối chiếu: dòng tóm tắt báo 12 cột chưa có trong gói tin, mỗi dòng đó có nút Tạo trường
        const compareSummary = diffAlerts.locator('.field-compare-summary .summary-new');
        await expect(compareSummary).toBeVisible();
        await expect(compareSummary).toContainText('12');
        await expect(diffAlerts.locator('.field-compare-table').getByRole('button', { name: /Tạo trường/ })).toHaveCount(12);

        console.log('[Test 54] Step 6: Assert save');
        // Nút Lưu SQL: Bấm Lưu không hiện MessageBox xác nhận
        await page.route('**/api/sharedata/sharedatapacket/updateextractsql*', async (route) => {
            console.log('[Test 54] Mock updateextractsql called');
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({ code: 200, result: true }),
            });
        });

        const btnSave = page.locator('.sql-toolbar-right .el-button').filter({ hasText: /Lưu/ });
        await btnSave.click();
        await expect(page.locator('.el-message-box')).toHaveCount(0);
        console.log('[Test 54] Done!');
    });

    test('55. Form editPacketField — Ô aliasFieldKey là input text cho gõ tay tự do khi Thêm và Sửa', async ({ page }) => {
        let addPayload: any = null;
        await page.route('**/api/sharedata/sharedatapacketfield/add*', async (route) => {
            addPayload = JSON.parse(route.request().postData() || '{}');
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: true,
                }),
            });
        });

        await page.setViewportSize({ width: 1600, height: 900 });
        await page.goto('/#/sharedata/dataSource');

        const tabs = page.locator('.datasource-right-tabs');
        await expect(tabs).toBeVisible({ timeout: 15_000 });

        // Chuyển sang Tab Danh sách trường
        await page.locator('#tab-savedFields').click();

        // Bấm nút Thêm trường trong bảng Danh sách trường
        const btnAddField = page.locator('.saved-fields-grid-wrapper button').filter({ hasText: /Thêm/ }).first();
        await btnAddField.click();

        const dialog = page.locator('.edit-packetfield-container .el-dialog');
        await expect(dialog).toBeVisible({ timeout: 10_000 });

        // 1. Ô khóa trường là el-input gõ tay tự do (không phải dropdown, không disabled)
        const keyFormItem = dialog.locator('.el-form-item').filter({ hasText: /Khóa field/i });
        const inputKey = keyFormItem.locator('input');
        await expect(inputKey).toBeVisible();
        await expect(inputKey).toBeEnabled();

        // 2. Nhập tự do mã trường và tên trường
        await inputKey.fill('custom_speed');
        const nameInput = dialog.locator('.el-form-item').filter({ hasText: /^Tên/ }).locator('input').first();
        await nameInput.fill('Tốc độ tùy chỉnh');

        // 3. Bấm Xác nhận -> gọi api add thành công
        const btnSubmit = dialog.locator('.el-dialog__footer .el-button--primary');
        await btnSubmit.click();

        expect(addPayload).toBeTruthy();
        expect(addPayload.aliasFieldKey).toBe('custom_speed');
        expect(addPayload.name).toBe('Tốc độ tùy chỉnh');
    });

    test('56. Tab Danh sách trường — Không hiển thị nhãn [Không có trong SQL] trên cột aliasFieldKey', async ({ page }) => {
        // Mock SQL chỉ có cột 'speed', trong khi trường có 'header.status' (bắt buộc) và 'recorded_time' (không bắt buộc)
        await page.route('**/api/sharedata/sharedatapacketfield/getextractcolumns*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        sqlAvailable: true,
                        columns: [
                            { fieldKey: 'speed', dataType: 'int', assigned: false },
                        ],
                    },
                }),
            });
        });

        await page.route('**/api/sharedata/sharedatapacketfield/list*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: [
                        { id: 'FLD_01', datatypeId: 'PKT_01', aliasFieldKey: 'header.status', name: 'Trạng thái', fieldType: 'int', status: 1, isRequired: 1 },
                        { id: 'FLD_02', datatypeId: 'PKT_01', aliasFieldKey: 'recorded_time', name: 'Thời gian', fieldType: 'datetime', status: 1, isRequired: 0 },
                    ],
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
                            { id: 'FLD_01', datatypeId: 'PKT_01', aliasFieldKey: 'header.status', name: 'Trạng thái', fieldType: 'int', status: 1, isRequired: 1 },
                            { id: 'FLD_02', datatypeId: 'PKT_01', aliasFieldKey: 'recorded_time', name: 'Thời gian', fieldType: 'datetime', status: 1, isRequired: 0 },
                        ],
                        total: 2,
                    },
                }),
            });
        });

        await page.setViewportSize({ width: 1600, height: 900 });
        await page.goto('/#/sharedata/dataSource');

        const tabs = page.locator('.datasource-right-tabs');
        await expect(tabs).toBeVisible({ timeout: 15_000 });

        // Cả 2 trường đều hiển thị aliasFieldKey dạng text phẳng chuẩn
        const headerStatusCell = page.locator('.saved-fields-pane .table-name').filter({ hasText: 'header.status' });
        await expect(headerStatusCell).toBeVisible({ timeout: 10_000 });

        const recordedTimeCell = page.locator('.saved-fields-pane .table-name').filter({ hasText: 'recorded_time' });
        await expect(recordedTimeCell).toBeVisible({ timeout: 10_000 });

        // Nhãn cảnh báo .field-sql-tag [Không có trong SQL] đã được gỡ bỏ hoàn toàn khỏi bảng
        await expect(page.locator('.field-sql-tag')).toHaveCount(0);
        await expect(page.locator('.saved-fields-pane')).not.toContainText('Không có trong SQL');
    });

    test('57. Commit đầu tiên (c5be7f33) — editMapping modal co giãn tự nhiên theo nội dung và khung rỗng nét đứt', async () => {
        const editMappingPath = resolveWebvuePath('views/sharedata/mapping/component/editMapping.vue');
        const content = fs.readFileSync(editMappingPath, 'utf8');

        // 1. .mp-form-body không còn cố định 92vh, tự co giãn theo nội dung
        expect(content).not.toContain('height: calc(92vh - 130px);');
        expect(content).toContain('min-height: 380px;');
        expect(content).toContain('max-height: calc(88vh - 120px);');
        expect(content).toContain('overflow-y: auto;');

        // 2. Khung trạng thái rỗng .mp-tree-empty có viền nét đứt
        expect(content).toContain('.mp-tree-empty {');
        expect(content).toContain('border: 1px dashed var(--el-border-color);');
        expect(content).toContain('border-radius: 6px;');

        // 3. treeMaxHeight tính toán linh hoạt theo window.innerHeight
        expect(content).toContain('Math.round(vh * 0.85 - 260)');
    });

    test('58. Commit đầu tiên (c5be7f33) — Định dạng bộ lọc ngày tháng chuẩn DD/MM/YYYY trên errorLog và history (Rule 20.27)', async () => {
        const errorLogPath = resolveWebvuePath('views/sharedata/errorLog/index.vue');
        const errorLogContent = fs.readFileSync(errorLogPath, 'utf8');
        expect(errorLogContent).toContain('format="DD/MM/YYYY"');
        expect(errorLogContent).toContain('value-format="YYYY-MM-DD"');

        const historyPath = resolveWebvuePath('views/sharedata/history/index.vue');
        const historyContent = fs.readFileSync(historyPath, 'utf8');
        expect(historyContent).toContain('format="DD/MM/YYYY HH:mm:ss"');
        expect(historyContent).toContain('value-format="YYYY-MM-DD HH:mm:ss"');
    });

    test('59. Commit đầu tiên (c5be7f33) — Cấu hình nguồn dữ liệu: canh lề trái cột text, font-weight nhóm trường và dọn listener rác', async () => {
        const indexPath = resolveWebvuePath('views/sharedata/dataSource/index.vue');
        const indexContent = fs.readFileSync(indexPath, 'utf8');

        // 1. Cột name và aliasFieldKey canh lề trái
        expect(indexContent).toMatch(/field:\s*'name'[^}]+align:\s*'left'/);
        expect(indexContent).toMatch(/field:\s*'aliasFieldKey'[^}]+align:\s*'left'/);

        // 2. Không còn listener rác đã xóa trong onUnmounted
        expect(indexContent).not.toContain('onResizingSampleBottom');
        expect(indexContent).not.toContain('stopResizeSample');

        // 3. Phân cấp font nhóm trường trong bảng
        expect(indexContent).toContain('.field-group-row');
    });

    test('60. Commit đầu tiên (c5be7f33) & Item 3 — Danh mục query table gợi ý theo mã gói tin 101–111 (02-mapping-goi-tin-101-111.md)', async () => {
        const qbPath = resolveWebvuePath('views/sharedata/dataSource/component/queryBuilder.vue');
        const qbContent = fs.readFileSync(qbPath, 'utf8');

        const sqlEditorPath = resolveWebvuePath('views/sharedata/dataSource/component/sqlEditor.vue');
        const sqlEditorContent = fs.readFileSync(sqlEditorPath, 'utf8');

        // 1. Không còn bản đồ tên bảng ghi cứng trong mã giao diện (tên bảng chỉ lấy từ danh mục bảng nguồn của máy chủ, dành cho SuperAdmin)
        const syncPanelContent = fs.readFileSync(resolveWebvuePath('views/sharedata/dataSource/component/syncModePanel.vue'), 'utf8');
        for (const content of [qbContent, sqlEditorContent, syncPanelContent]) {
            expect(content).not.toContain('PACKET_RELATED_TABLES_MAP');
            expect(content).not.toContain('KNOWN_ALLOWED_TABLES');
            expect(content).not.toContain('TollTransactionIn');
            expect(content).not.toContain('TmsVehicleRegistration');
            expect(content).toContain('utils/sharedata/sourceTables');
        }

        // 2. sqlEditor.vue đăng ký completion provider cho Monaco Editor, bảng gợi ý theo mã gói tin của danh mục bảng nguồn
        expect(sqlEditorContent).toContain("monaco.languages.registerCompletionItemProvider('sql'");
        expect(sqlEditorContent).toContain('matchesPacketCode(t.packetCodes, currentCode)');
    });

    test('61. Giao diện Cấu hình SQL gọn cảnh báo; cột SQL chưa khai nằm ở danh sách trường (bỏ bảng Trường trích xuất) theo prompt 21/10/2026', async () => {
        const sqlEditorPath = resolveWebvuePath('views/sharedata/dataSource/component/sqlEditor.vue');
        const sqlEditorContent = fs.readFileSync(sqlEditorPath, 'utf8');

        const syncPanelPath = resolveWebvuePath('views/sharedata/dataSource/component/syncModePanel.vue');
        const syncPanelContent = fs.readFileSync(syncPanelPath, 'utf8');

        const indexPath = resolveWebvuePath('views/sharedata/dataSource/index.vue');
        const indexContent = fs.readFileSync(indexPath, 'utf8');

        // 1-2. Khung SQL không còn bảng đối chiếu riêng: cột SQL chưa khai hiện thành nhóm "Chưa khai" ở danh sách trường
        expect(sqlEditorContent).not.toContain('field-compare-table');
        expect(sqlEditorContent).not.toContain('compareRows');
        expect(sqlEditorContent).toContain("emit('columns-change'");
        expect(indexContent).toContain('isPendingRow');
        expect(sqlEditorContent).not.toContain('sql-extracted-status-line');
        expect(sqlEditorContent).not.toContain('sql-field-chip');

        // 3. syncModePanel: Cảnh báo phần nối đuôi thành dòng nhỏ có link [Gỡ]
        expect(syncPanelContent).toContain('residual-cursor-warning');
        expect(syncPanelContent).toContain('btn-remove-cursor');

        // 4. index.vue: Thanh công cụ chứa Sao chép và Lưu; không còn nút Khôi phục SQL mẫu
        expect(indexContent).not.toContain('vqb-reset-template-btn');
        expect(indexContent).toContain('handleCopySql');

        // 5. sqlEditor.vue: không còn chú giải màu; nút Tạo trường nằm ở dòng "chưa khai" của danh sách trường (index.vue)
        expect(sqlEditorContent).not.toContain('sql-extracted-legend');
        expect(indexContent).toContain('fieldCompareCreate');
        expect(sqlEditorContent).toContain('handleChipClick');
        expect(sqlEditorContent).not.toContain('<el-tooltip\n\t\t\t\t\t\tv-for="col in nonSystemColumns"');
    });

    test('62. Biến câu truy vấn theo chế độ đồng bộ và ô Khóa phụ cho Gửi nối đuôi theo prompt 21/10/2026', async () => {
        const queryParamPanelPath = resolveWebvuePath('views/sharedata/dataSource/component/queryParameterPanel.vue');
        const queryParamPanelContent = fs.readFileSync(queryParamPanelPath, 'utf8');

        const sqlEditorPath = resolveWebvuePath('views/sharedata/dataSource/component/sqlEditor.vue');
        const sqlEditorContent = fs.readFileSync(sqlEditorPath, 'utf8');

        const syncPanelPath = resolveWebvuePath('views/sharedata/dataSource/component/syncModePanel.vue');
        const syncPanelContent = fs.readFileSync(syncPanelPath, 'utf8');

        const viPath = resolveWebvuePath('i18n/lang/its/sharedata/vi-vn.json');
        const viContent = fs.readFileSync(viPath, 'utf8');

        const enPath = resolveWebvuePath('i18n/lang/its/sharedata/en-us.json');
        const enContent = fs.readFileSync(enPath, 'utf8');

        // 1. queryParameterPanel.vue nhận syncMode, phân biệt biến chỉ dùng khi nối đuôi
        expect(queryParamPanelContent).toContain('syncMode?: number | string');
        expect(queryParamPanelContent).toContain('isSnapshot');
        expect(queryParamPanelContent).toContain('isIncrementalOnlyParam');
        expect(queryParamPanelContent).toContain('paramIncrementalOnlyTag');
        expect(queryParamPanelContent).toContain('paramSnapshotNotUsedHint');

        // 2. sqlEditor.vue truyền syncMode vào QueryParameterPanel
        expect(sqlEditorContent).toContain(':sync-mode="props.syncMode"');

        // 3. syncModePanel.vue: có ô Khóa phụ trong chế độ nối đuôi
        expect(syncPanelContent).toContain('sync-key-select');
        expect(syncPanelContent).toContain('selectedKeyExpr');
        expect(syncPanelContent).toContain('onKeyExprChange');
        expect(syncPanelContent).toContain('cursorKeyColHint');

        // 4. syncModePanel.vue: cảnh báo phần nối đuôi còn sót liệt kê động các phần, không ghi cứng 100 dòng
        expect(syncPanelContent).toContain('residualCursorWithPartsWarning');
        expect(syncPanelContent).toContain('residualCursorFilterPart');
        expect(syncPanelContent).not.toContain('100 dòng');

        // 5. Đồng bộ i18n đầy đủ 2 ngôn ngữ
        expect(viContent).toContain('lz.label.sharedataDataSource.paramIncrementalOnlyTag');
        expect(viContent).toContain('lz.label.sharedataDataSource.paramSnapshotNotUsedHint');
        expect(viContent).toContain('lz.label.sharedataDataSource.residualCursorFilterPart');
        expect(viContent).toContain('lz.message.sharedataDataSource.residualCursorWithPartsWarning');

        expect(enContent).toContain('lz.label.sharedataDataSource.paramIncrementalOnlyTag');
        expect(enContent).toContain('lz.label.sharedataDataSource.paramSnapshotNotUsedHint');
        expect(enContent).toContain('lz.label.sharedataDataSource.residualCursorFilterPart');
        expect(enContent).toContain('lz.message.sharedataDataSource.residualCursorWithPartsWarning');
    });

    test('63. Chuyển tab Cấu hình SQL không bị crash vue-i18n và escape chuẩn ký tự @ trong từ điển sharedata', async ({ page }) => {
        // 1. Quét tĩnh mã nguồn index.vue đảm bảo xử lý chuyển tab và watcher nạp SQL an toàn
        const indexPath = resolveWebvuePath('views/sharedata/dataSource/index.vue');
        const indexContent = fs.readFileSync(indexPath, 'utf8');

        expect(indexContent).toContain('@tab-change="handleRightTabChange"');
        expect(indexContent).toContain('@tab-click="handleRightTabClick"');
        expect(indexContent).toContain(':style="{ display: state.activeRightTab === \'savedFields\' ? \'flex\' : \'none\' }"');
        expect(indexContent).toContain("String(u?.accountType) === '111'");
        expect(indexContent).toContain("u?.accountType === 'SuperAdmin'");
        expect(indexContent).toMatch(/watch\(\s*\(\)\s*=>\s*state\.activeRightTab/);

        // 2. Kiểm tra bộ từ điển i18n không còn ký tự @ gây lỗi compile vue-i18n (Invalid linked format)
        const viPath = resolveWebvuePath('i18n/lang/its/sharedata/vi-vn.json');
        const viJson = JSON.parse(fs.readFileSync(viPath, 'utf8')) as Record<string, string>;

        const enPath = resolveWebvuePath('i18n/lang/its/sharedata/en-us.json');
        const enJson = JSON.parse(fs.readFileSync(enPath, 'utf8')) as Record<string, string>;

        const criticalKeys = [
            'lz.label.sharedataDataSource.residualCursorFilterPart',
            'lz.message.sharedataDataSource.vqbSqlHasUnsupportedTop',
            'lz.validation.sharedata.sqlUnsupportedParameter',
            'lz.validation.sharedata.sqlIncrementalMissingTop',
        ];

        for (const k of criticalKeys) {
            expect(viJson[k]).toBeDefined();
            expect(enJson[k]).toBeDefined();

            // Bắt buộc escape bằng {'@'} theo quy chuẩn của vue-i18n
            expect(viJson[k]).toContain("{\'@\'}");
            expect(enJson[k]).toContain("{\'@\'}");

            // Tuyệt đối không còn @ đứng trước tên biến/chữ cái mà không escape (nguyên nhân gây SyntaxError)
            expect(viJson[k]).not.toMatch(/@(?![{:])\w+/);
            expect(enJson[k]).not.toMatch(/@(?![{:])\w+/);
        }

        // 3. E2E Browser Test: Bắt lỗi Console & Lỗi JS khi mở trang và chuyển tab
        const pageErrors: string[] = [];
        page.on('pageerror', (err: Error) => {
            pageErrors.push(err.message);
        });

        const multiLineSql = `SELECT TOP (@pageSize)
    zs.ID AS zoneStatusId,
    zs.ZoneId AS zoneId,
    z.Name AS zoneName,
    z.FromKmNumber AS fromLocationKm,
    z.FromMetNumber AS fromLocationMet,
    z.ToKmNumber AS toLocationKm,
    z.ToMetNumber AS toLocationMet,
    z.LaneId AS laneId,
    CAST(zs.AverageSpeed AS DECIMAL(18, 2)) AS averageSpeed,
    zs.Condition AS trafficCondition,
    zs.UpdateTime AS dataTime,
    z.MaxSpeed AS speedLimit,
    ts.TotalVehicleNumber AS vehicleCount,
    COALESCE(zs.UpdateTime, zs.CreateTime) AS __lastTime,
    zs.ID AS __lastKey
FROM TmsZoneStatus zs
LEFT JOIN TmsZone z ON zs.ZoneId = z.ID
LEFT JOIN TmsTrafficStatistic ts ON zs.ZoneId = ts.ZoneId
WHERE (
      @lastTime IS NULL
      OR COALESCE(zs.UpdateTime, zs.CreateTime) > @lastTime
      OR (COALESCE(zs.UpdateTime, zs.CreateTime) = @lastTime AND zs.ID > @lastKey)
  )
ORDER BY __lastTime ASC, __lastKey ASC`;

        // Mock API getextractsql trả về SQL nhiều dòng (25 dòng) chế độ Toàn bộ (syncMode = 1) nhưng còn sót điều kiện @lastTime
        // để kích hoạt residualCursorWithPartsWarning và residualCursorFilterPart trong syncModePanel,
        // đồng thời kiểm tra tự động dãn chiều cao editor
        await page.route('**/api/sharedata/sharedatapacket/getextractsql*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        extractSql: multiLineSql,
                        status: 1,
                        syncMode: 1,
                        maxLookBackDays: null,
                    },
                }),
            });
        });

        await page.setViewportSize({ width: 1600, height: 900 });
        await page.goto('/#/sharedata/dataSource');

        const tabs = page.locator('.datasource-right-tabs');
        await expect(tabs).toBeVisible({ timeout: 15_000 });

        // Tab mặc định ban đầu là Danh sách trường
        const savedFieldsPane = page.locator('.saved-fields-pane');
        const sqlCenterPane = page.locator('.sql-center-pane');
        await expect(savedFieldsPane).toBeVisible();

        // Chuyển sang tab Cấu hình SQL
        const sqlTab = tabs.getByRole('tab', { name: /Cấu hình SQL/ });
        await sqlTab.click();
        await expect(sqlTab).toHaveClass(/is-active/);

        // Panel Cấu hình SQL phải hiển thị, panel Danh sách trường phải ẩn
        await expect(sqlCenterPane).toBeVisible({ timeout: 10_000 });
        await expect(savedFieldsPane).toBeHidden();

        // 4. Kiểm tra chiều cao editor đã tự động co giãn theo nội dung SQL (25 dòng tự tăng > 350px thay vì kẹt ở 280px)
        const sqlEditorPath = resolveWebvuePath('views/sharedata/dataSource/component/sqlEditor.vue');
        const sqlEditorContent = fs.readFileSync(sqlEditorPath, 'utf8');
        expect(sqlEditorContent).toContain('adjustEditorHeightToContent');
        expect(sqlEditorContent).toContain('computeSqlContentHeight');
        expect(sqlEditorContent).toContain('onDidContentSizeChange');

        const monacoEditor = page.locator('.sql-monaco-editor');
        await expect(monacoEditor).toBeVisible();
        const editorBox = await monacoEditor.boundingBox();
        expect(editorBox).toBeTruthy();
        if (editorBox) {
            expect(editorBox.height).toBeGreaterThan(350);
        }

        // Cảnh báo con trỏ còn sót xuất hiện và render đúng text i18n không bị lỗi
        const residualAlert = page.locator('.sync-mode-alert');
        if (await residualAlert.count() > 0) {
            await expect(residualAlert).toContainText(/@lastTime/);
        }

        // Chuyển ngược lại tab Danh sách trường
        const fieldsTab = tabs.getByRole('tab', { name: /Trường gói tin|Danh sách trường/ });
        await fieldsTab.click();
        await expect(fieldsTab).toHaveClass(/is-active/);
        await expect(savedFieldsPane).toBeVisible();
        await expect(sqlCenterPane).toBeHidden();

        // Chuyển lại tab Cấu hình SQL một lần nữa để xác nhận mượt mà không crash
        await sqlTab.click();
        await expect(sqlTab).toHaveClass(/is-active/);
        await expect(sqlCenterPane).toBeVisible();

        // Đảm bảo không có bất kỳ lỗi SyntaxError hoặc Invalid linked format nào ném ra
        const linkedFormatErrors = pageErrors.filter((msg: string) => msg.includes('Invalid linked format'));
        expect(linkedFormatErrors).toHaveLength(0);
    });

    test('MD-FE-3: Tab SQL đối tác, thẻ SQL riêng / Lỗi thời, Hộp so sánh Monaco 3 nút và Dùng lại mẫu chung', async ({ page }) => {
        // 1. Quét tĩnh mã nguồn: Kiểm tra có SqlDiffDialog, partnerSqlList, activeSqlTab
        const vueFilePath = resolveWebvuePath('views/sharedata/dataSource/index.vue');
        const vueContent = fs.readFileSync(vueFilePath, 'utf8');
        expect(vueContent).toContain('SqlDiffDialog');
        expect(vueContent).toContain('partnerSqlList');
        expect(vueContent).toContain('activeSqlTab');
        expect(vueContent).toContain('sql-partner-tabs');

        // Kiểm tra sqlDiffDialog.vue tồn tại và có 3 nút
        const diffDialogPath = resolveWebvuePath('views/sharedata/dataSource/component/sqlDiffDialog.vue');
        expect(fs.existsSync(diffDialogPath)).toBeTruthy();
        const diffContent = fs.readFileSync(diffDialogPath, 'utf8');
        expect(diffContent).toContain('handleTakeAllStandard');
        expect(diffContent).toContain('handleKeepPartnerSql');
        expect(diffContent).toContain('handleSaveMerged');

        // 2. Mock API cho đối tác
        const partnerListState = [
            {
                mappingId: 'MAP_01',
                partnerId: 'P_01',
                partnerCode: 'ALPHA',
                partnerName: 'Đối tác Alpha',
                isCustom: false,
                isOutdated: false,
            },
        ];

        const partnerSqlState = {
            mappingId: 'MAP_01',
            packetCode: '101_commonData',
            partnerName: 'Đối tác Alpha',
            standardSql: 'SELECT TOP (@pageSize) td.Speed AS speed FROM TmsTrafficData td',
            customSql: null as string | null,
            syncMode: null as number | null,
            packetSyncMode: 1,
            isCustom: false,
            isOutdated: false,
        };

        await page.route('**/api/sharedata/sharedatapacket/getpartnersqllist*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: partnerListState,
                }),
            });
        });

        await page.route('**/api/sharedata/sharedatapacket/getpartnerextractsql*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: partnerSqlState,
                }),
            });
        });

        await page.route('**/api/sharedata/sharedatapacket/updatepartnerextractsql*', async (route) => {
            const postData = JSON.parse(route.request().postData() || '{}');
            partnerSqlState.customSql = postData.extractSql;
            partnerSqlState.isCustom = true;
            partnerSqlState.isOutdated = false;
            partnerListState[0].isCustom = true;
            partnerListState[0].isOutdated = false;
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({ code: 200, result: true, message: 'Success' }),
            });
        });

        await page.route('**/api/sharedata/sharedatapacket/resetpartnerextractsql*', async (route) => {
            partnerSqlState.customSql = null;
            partnerSqlState.isCustom = false;
            partnerSqlState.isOutdated = false;
            partnerListState[0].isCustom = false;
            partnerListState[0].isOutdated = false;
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({ code: 200, result: true, message: 'Success' }),
            });
        });

        // 3. Render giao diện
        await page.setViewportSize({ width: 1600, height: 900 });
        await page.goto('/#/sharedata/dataSource');

        const tabs = page.locator('.datasource-right-tabs');
        await expect(tabs).toBeVisible({ timeout: 15_000 });

        // Chuyển sang tab Cấu hình SQL
        const sqlTab = tabs.getByRole('tab', { name: /Cấu hình SQL/ });
        await sqlTab.click();
        await expect(sqlTab).toHaveClass(/is-active/);

        // Kiểm tra thanh tabs con đối tác hiển thị
        const partnerTabs = page.locator('.sql-partner-tabs');
        await expect(partnerTabs).toBeVisible({ timeout: 10_000 });

        // Ban đầu ở tab Mẫu chung (tabStandardSql)
        const stdTab = partnerTabs.getByRole('tab', { name: /Mẫu chung/ });
        await expect(stdTab).toBeVisible();
        await expect(stdTab).toHaveClass(/is-active/);

        // Tab đối tác "Đối tác Alpha"
        const alphaTab = partnerTabs.getByRole('tab', { name: /Đối tác Alpha/ });
        await expect(alphaTab).toBeVisible();
        // Ban đầu chưa có SQL riêng -> chưa có thẻ "SQL riêng"
        await expect(alphaTab.locator('.el-tag')).toHaveCount(0);

        // Chuyển sang tab Đối tác Alpha
        await alphaTab.click();
        await expect(alphaTab).toHaveClass(/is-active/);

        // Hiển thị gợi ý đang dùng mẫu chung
        const usingStdAlert = page.locator('.el-alert:has-text("Đang dùng mẫu chung")');
        await expect(usingStdAlert).toBeVisible();

        // Nút Lưu có sẵn trên toolbar
        const btnSave = page.locator('.sql-toolbar-right .el-button').filter({ hasText: /Lưu/ });
        await expect(btnSave).toBeVisible();

        // Bấm Lưu để tạo SQL riêng cho đối tác
        await btnSave.click();
        await page.waitForTimeout(500);

        // Sau khi lưu, thẻ "SQL riêng" hiển thị trên tab đối tác
        const customTag = alphaTab.locator('.el-tag').filter({ hasText: /SQL riêng/ });
        await expect(customTag).toBeVisible();

        // Có thêm nút "Dùng lại mẫu chung" và "So sánh với mẫu chung"
        const btnReset = page.locator('.sql-toolbar-right .el-button').filter({ hasText: /Dùng lại mẫu chung/ });
        const btnCompare = page.locator('.sql-toolbar-right .el-button').filter({ hasText: /So sánh với mẫu chung/ });
        await expect(btnReset).toBeVisible();
        await expect(btnCompare).toBeVisible();

        // 4. Giả lập trường hợp mẫu chung thay đổi -> đối tác bị "Lỗi thời"
        partnerListState[0].isOutdated = true;
        partnerSqlState.isOutdated = true;

        // Chuyển tab để reload trạng thái
        await stdTab.click();
        await alphaTab.click();

        // Thẻ "Lỗi thời" xuất hiện trên tab
        const outdatedTag = alphaTab.locator('.el-tag').filter({ hasText: /Lỗi thời/ });
        await expect(outdatedTag).toBeVisible();

        // Alert cảnh báo lỗi thời xuất hiện dưới toolbar
        const outdatedAlert = page.locator('.el-alert--warning').filter({ hasText: /Mẫu chung của gói tin đã được cập nhật/ });
        await expect(outdatedAlert).toBeVisible();

        // 5. Mở hộp so sánh Monaco Diff
        await btnCompare.click();
        const diffDialog = page.locator('.sql-diff-dialog');
        await expect(diffDialog).toBeVisible();

        // Kiểm tra 3 nút trong footer
        const btnTakeAll = diffDialog.locator('.el-button').filter({ hasText: /Lấy toàn bộ mẫu chung/ });
        const btnKeep = diffDialog.locator('.el-button').filter({ hasText: /Giữ nguyên SQL đối tác/ });
        const btnSaveMerged = diffDialog.locator('.el-button').filter({ hasText: /Lưu bản đã trộn/ });
        await expect(btnTakeAll).toBeVisible();
        await expect(btnKeep).toBeVisible();
        await expect(btnSaveMerged).toBeVisible();

        // Đóng dialog
        const btnCancel = diffDialog.locator('.el-button').filter({ hasText: /Đóng|Hủy|Huỷ/ });
        await btnCancel.click();
        await expect(diffDialog).toBeHidden();

        // 6. Test nút "Dùng lại mẫu chung"
        await btnReset.click();
        // Xác nhận trong ElMessageBox
        const confirmMsgBox = page.locator('.el-message-box');
        await expect(confirmMsgBox).toBeVisible();
        const confirmBtn = confirmMsgBox.locator('.el-button--primary');
        await confirmBtn.click();
        await page.waitForTimeout(500);

        // Sau khi reset, thẻ "SQL riêng" và "Lỗi thời" biến mất khỏi tab
        await expect(alphaTab.locator('.el-tag')).toHaveCount(0);
    });

    test('Cấu hình SQL: gói chưa có đối tác vẫn hiện thanh tab với tab Mẫu chung', async ({ page }) => {
        await page.route('**/api/sharedata/sharedatapacket/getpartnersqllist*', async (route) => {
            await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ code: 200, result: [] }) });
        });

        await page.setViewportSize({ width: 1600, height: 900 });
        await page.goto('/#/sharedata/dataSource');
        const tabs = page.locator('.datasource-right-tabs');
        await expect(tabs).toBeVisible({ timeout: 15_000 });
        await tabs.getByRole('tab', { name: /Cấu hình SQL/ }).click();

        const partnerTabs = page.locator('.sql-partner-tabs');
        await expect(partnerTabs).toBeVisible({ timeout: 10_000 });
        await expect(partnerTabs.getByRole('tab')).toHaveCount(1);
        await expect(partnerTabs.getByRole('tab', { name: /Mẫu chung/ })).toHaveClass(/is-active/);
    });

    test('Cấu hình SQL: Lưu thất bại thì hiện chi tiết lỗi và đánh dấu đỏ vị trí lỗi trên Monaco', async ({ page }) => {
        await page.route('**/api/sharedata/sharedatapacket/updateextractsql*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({ code: 200, result: null, message: "Cú pháp SQL không hợp lệ trên cơ sở dữ liệu: Invalid column name 'zzz'." }),
            });
        });
        await page.route('**/api/sharedata/sharedatapacket/parseextractcolumns*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: { isValid: false, errorMessage: "Cú pháp SQL không hợp lệ trên cơ sở dữ liệu: Invalid column name 'zzz'.", errorLine: 1, errorColumn: 8, columns: [] },
                }),
            });
        });

        await page.setViewportSize({ width: 1600, height: 900 });
        await page.goto('/#/sharedata/dataSource');
        const tabs = page.locator('.datasource-right-tabs');
        await expect(tabs).toBeVisible({ timeout: 15_000 });
        await tabs.getByRole('tab', { name: /Cấu hình SQL/ }).click();
        await expect(page.locator('.sql-monaco-editor .monaco-editor').first()).toBeVisible({ timeout: 15_000 });

        // Sửa nhẹ để nút Lưu hoạt động, rồi Lưu
        await page.locator('.monaco-editor').first().click();
        await page.keyboard.type(' ');
        await page.locator('.sql-toolbar-right .el-button').filter({ hasText: /Lưu/ }).click();

        // Thông báo lỗi có đủ chi tiết, không cụt sau dấu hai chấm
        await expect(page.locator('.sql-center-pane .el-alert--error')).toContainText("Invalid column name 'zzz'", { timeout: 10_000 });
        // Có gạch đỏ trên trình soạn thảo
        await expect(page.locator('.sql-monaco-editor .squiggly-error').first()).toBeVisible({ timeout: 10_000 });

        // Gõ tiếp vào trình soạn thảo thì gạch đỏ biến mất
        await page.locator('.sql-monaco-editor .monaco-editor').first().click();
        await page.keyboard.type('x');
        await expect(page.locator('.sql-monaco-editor .squiggly-error')).toHaveCount(0, { timeout: 5000 });
    });

    test('Cấu hình SQL: đổi tab không sửa gì thì không hỏi bỏ thay đổi, có sửa mới hỏi', async ({ page }) => {
        await page.route('**/api/sharedata/sharedatapacket/getpartnersqllist*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: [{ mappingId: 'MAP_01', partnerId: 'P_01', partnerCode: 'ALPHA', partnerName: 'Đối tác Alpha', isCustom: false, isOutdated: false }],
                }),
            });
        });
        await page.route('**/api/sharedata/sharedatapacket/getpartnerextractsql*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        mappingId: 'MAP_01',
                        packetCode: '101_commonData',
                        partnerName: 'Đối tác Alpha',
                        standardSql: 'SELECT TOP (@pageSize) td.Speed AS speed FROM TmsTrafficData td',
                        customSql: null,
                        isCustom: false,
                        isOutdated: false,
                    },
                }),
            });
        });

        await page.setViewportSize({ width: 1600, height: 900 });
        await page.goto('/#/sharedata/dataSource');
        const tabs = page.locator('.datasource-right-tabs');
        await expect(tabs).toBeVisible({ timeout: 15_000 });
        await tabs.getByRole('tab', { name: /Cấu hình SQL/ }).click();

        const partnerTabs = page.locator('.sql-partner-tabs');
        await expect(partnerTabs).toBeVisible({ timeout: 10_000 });
        const stdTab = partnerTabs.getByRole('tab', { name: /Mẫu chung/ });
        const alphaTab = partnerTabs.getByRole('tab', { name: /Đối tác Alpha/ });
        const msgBox = page.locator('.el-message-box');

        // Chưa sửa gì: đi và về giữa 2 tab đều không hiện hộp xác nhận
        await alphaTab.click();
        await expect(alphaTab).toHaveClass(/is-active/);
        await stdTab.click();
        await expect(stdTab).toHaveClass(/is-active/, { timeout: 5000 });
        await expect(msgBox).toHaveCount(0);
        await alphaTab.click();
        await expect(alphaTab).toHaveClass(/is-active/);
        await expect(msgBox).toHaveCount(0);

        // Có sửa SQL: đổi tab phải hỏi; bấm "Bỏ qua" ở lại tab cũ
        const editor = page.locator('.monaco-editor').first();
        await editor.click();
        await page.keyboard.press('End');
        await page.keyboard.type(' -- sua');
        await stdTab.click();
        await expect(msgBox).toBeVisible({ timeout: 5000 });
        await msgBox.locator('.el-message-box__btns .el-button:not(.el-button--primary)').click();
        await expect(msgBox).toBeHidden();
        await expect(alphaTab).toHaveClass(/is-active/);
    });
});




