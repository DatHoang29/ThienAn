import { test, expect, type Page, type Route } from '@playwright/test';
import { mockAuth, readVxeColumnAligns, expectedColumnAligns } from '../../setup';

/**
 * Test E2E cho phân hệ ShareData — Quản lý Gói tin & Trường gói tin (dataSource/index.vue):
 * - Kéo dãn cột, kéo thanh chia 2 pane, cột Hành động cố định bên phải, bảng/phân trang co giãn theo chiều cao.
 * - Hộp thoại form/confirm khi bấm ra ngoài; form thêm gói.
 */

/** Lỗi của khung layout dùng chung (layout/main/defaults.vue gọi layoutMainScrollbarRef khi chưa gắn) nằm ngoài ShareData: không tính vào lỗi trang. */
const isSharedLayoutError = (message: string) => message.includes('layoutMainScrollbarRef');

/** Chọn phạm vi SQL (Mẫu chung hoặc đối tác có cấu hình riêng) qua ô el-select.partner-tab-jump. */
const selectPartnerScope = async (page: Page, labelOrCode: string) => {
    const select = page.locator('.partner-tab-jump');
    await expect(select).toBeVisible({ timeout: 10_000 });
    await select.click();
    const option = page.locator('.partner-tab-jump-popper:visible .el-select-dropdown__item', { hasText: labelOrCode }).first();
    await expect(option).toBeVisible({ timeout: 5_000 });
    await option.click();
};

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
                        extractSql: 'SELECT TOP 100 td.Speed AS speed FROM TmsTrafficData td',
                        status: 1,
                        fields: [{ key: 'speed', type: 'number', label: 'Tốc độ', required: true }],
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

        // Bảng câu ghi chiều nhận và bảng tham số của gói: mock rỗng để không gọi ra máy chủ thật
        await page.route('**/api/sharedata/sharedatapacket/getwritesqls*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({ code: 200, result: { packetCode: '101_commonData', writeSql: '', affectedTables: [], detectedTables: [], keyFields: [] } }),
            });
        });
        await page.route('**/api/sharedata/sharedatapacket/getsqlvariables*', async (route) => {
            await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ code: 200, result: { items: [] } }) });
        });
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
        // Kéo cột Tên gói tin (cột co giãn), không kéo cột STT
        const targetCol = packetGrid.locator('.vxe-table--main-wrapper .vxe-table--header-wrapper .vxe-header--column:has(.vxe-resizable):visible').filter({ hasText: 'Tên gói tin' }).first();
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

        // 2. Bảng Trường gói tin nằm cùng trang, dưới phần SQL (tải chậm: phải bấm chọn gói mới nạp chi tiết)
        await packetGrid.locator('.vxe-table--main-wrapper .vxe-body--row').first().click();
        const fieldGrid = page.locator('.field-col .vxe-grid');
        await expect(fieldGrid).toBeVisible({ timeout: 10_000 });
        await expect(page.locator('.packet-details-wrapper.is-switching')).toHaveCount(0, { timeout: 10_000 });

        const fieldResizers = fieldGrid.locator('.vxe-table--header-wrapper .vxe-header--column .vxe-resizable');
        await expect(fieldResizers.first()).toBeAttached();
        const fieldResizerCount = await fieldResizers.count();
        expect(fieldResizerCount).toBeGreaterThan(0);

        // Kéo dãn cột trên bảng Trường gói tin (chờ layout ổn định: bảng nằm dưới phần SQL nên chỉ cần vị trí không còn đổi và nằm trong màn hình)
        const targetFieldCol = fieldGrid.locator('.vxe-table--header-wrapper .vxe-header--column:has(.vxe-resizable):visible').first();
        let lastFieldBox = { x: -1, y: -1, width: -1 };
        await expect(async () => {
            const box = await targetFieldCol.boundingBox();
            expect(box).not.toBeNull();
            expect(box!.y).toBeLessThan(page.viewportSize()!.height - 50);
            const prev = lastFieldBox;
            lastFieldBox = { x: box!.x, y: box!.y, width: box!.width };
            expect(Math.abs(box!.x - prev.x)).toBeLessThan(1);
            expect(Math.abs(box!.y - prev.y)).toBeLessThan(1);
            expect(Math.abs(box!.width - prev.width)).toBeLessThan(1);
        }).toPass({ timeout: 10_000 });

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
        await expect(packetTable).toBeVisible({ timeout: 30_000 });

        // Kiểm tra thanh phân trang vxe-pager ở đáy có chiều cao cố định 40px và bám sát đáy card
        const pager = page.locator('.packet-col .vxe-pager');
        await expect(pager).toBeVisible();
        // Độ lệch giữa đáy pager và đáy card không vượt quá padding (khoảng trống thừa < 10px).
        // Đo lại tới khi bố cục ổn định: lúc trang còn đang dựng, card có thể tạm nằm sai chỗ.
        const pagerGap = async () => {
            const card = await page.locator('.packet-col .full-table').boundingBox();
            const pg = await pager.boundingBox();
            if (!card || !pg) return 999;
            return card.y + card.height - (pg.y + pg.height);
        };
        await expect.poll(pagerGap, { timeout: 10_000 }).toBeLessThanOrEqual(10);
        const pagerBox = await pager.boundingBox();
        expect(pagerBox!.height).toBeCloseTo(40, 1);
        const gridBox = await page.locator('.packet-col .vxe-grid').boundingBox();
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

        // Tải lại trang: độ rộng khung trái đã kéo vẫn được giữ nguyên từ localStorage
        await page.reload();
        await expect(packetGrid).toBeVisible({ timeout: 30_000 });
        const reloadedPacketBox = await packetPane.boundingBox();
        expect(reloadedPacketBox).not.toBeNull();
        expect(Math.abs(reloadedPacketBox!.width - newPacketBox!.width)).toBeLessThanOrEqual(5);
    });

    test('19. Kiểm tra cột Hành động cố định bên phải (fixed: right) và chống đứt gãy bố cục khi kéo resize cột', async ({ page }) => {
        await page.setViewportSize({ width: 1600, height: 900 });
        await page.goto('/#/sharedata/dataSource');

        const packetGrid = page.locator('.packet-col .vxe-grid');
        await expect(packetGrid).toBeVisible({ timeout: 30_000 });

        // Chờ các hàng dữ liệu bảng gói tin hiển thị
        const packetRows = packetGrid.locator('.vxe-body--row');
        await expect(packetRows.first()).toBeVisible({ timeout: 10_000 });

        // 3. Lúc mở trang bảng vừa khít khung (không cuộn ngang, không thừa khoảng trắng bên phải): cột Hành động sát mép phải.
        //    Bảng không tràn ngang thì vxe không dựng khối fixed-right, nên lấy cột Hành động ở bảng chính.
        const actionHeader = packetGrid.locator('.vxe-table--main-wrapper .vxe-header--column').filter({ hasText: 'Hành động' }).first();
        await expect(actionHeader).toBeVisible();
        const gridBox = await packetGrid.boundingBox();
        const actionBox = await actionHeader.boundingBox();
        expect(gridBox).not.toBeNull();
        expect(actionBox).not.toBeNull();
        const rightEdgeDiff = Math.abs((actionBox!.x + actionBox!.width) - (gridBox!.x + gridBox!.width));
        expect(rightEdgeDiff).toBeLessThanOrEqual(5);
        const noHScroll = await packetGrid.evaluate((g) => {
            const body = g.querySelector('.vxe-table--main-wrapper .vxe-table--body-wrapper') as HTMLElement;
            return body.scrollWidth <= body.clientWidth + 1;
        });
        expect(noHScroll).toBe(true);

        // Các nút thao tác (copy, edit, delete) ở dòng đầu hiển thị được
        const firstRowActionButtons = packetGrid.locator('.vxe-table--main-wrapper .vxe-body--row').first().locator('button');
        await expect(firstRowActionButtons.first()).toBeVisible();
        // Ô Hành động đủ rộng cho 3 nút, không bị cắt "…"
        const actionCellFits = await packetGrid.evaluate((g) =>
            [...g.querySelectorAll('.vxe-table--main-wrapper .vxe-body--row')].slice(0, 3).every((r) => {
                const cell = r.querySelector('.vxe-body--column:last-child .vxe-cell') as HTMLElement;
                return cell.scrollWidth <= cell.clientWidth + 1;
            })
        );
        expect(actionCellFits).toBe(true);
        // Dòng giữ chiều cao cố định như cũ (không co theo nội dung)
        const rowHeights = await packetGrid.evaluate((g) => [...g.querySelectorAll('.vxe-table--main-wrapper .vxe-body--row')].map((r) => Math.round(r.getBoundingClientRect().height)));
        expect(Math.min(...rowHeights)).toBeGreaterThanOrEqual(34);

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

        // Sau khi kéo resize, cột Hành động vẫn hiện đủ (bảng chính hoặc khối fixed-right khi tràn ngang)
        await expect(packetGrid.locator('.vxe-header--column:visible').filter({ hasText: 'Hành động' }).first()).toBeVisible();

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
        await expect(packetDialog).toBeVisible({ timeout: 15_000 });

        // Click outside tại tọa độ (5, 5) khi chưa sửa gì -> đóng ngay không hỏi
        await page.mouse.click(5, 5);
        await expect(packetDialog).toBeHidden({ timeout: 15_000 });

        // 2. Mở lại, sửa một ô (nhập mã gói) -> bấm ra ngoài -> đóng ngay, không hỏi "Có thay đổi chưa lưu"
        await addPacketBtn.click();
        await expect(packetDialog).toBeVisible({ timeout: 15_000 });

        const codeInput = packetDialog.locator('input').first();
        await codeInput.fill('TEST_CODE');

        await page.mouse.click(5, 5);
        const confirmBox = page.locator('.el-message-box');
        await expect(packetDialog).toBeHidden({ timeout: 15_000 });
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
            await expect(msgBox).toBeVisible({ timeout: 15_000 });

            // Click outside -> đóng confirm mà không gọi API delete
            await page.mouse.click(5, 5);
            await expect(msgBox).toBeHidden({ timeout: 15_000 });
            expect(deleteApiCalled).toBe(false);
        }
    });

    test('49. Form thêm gói: ô Loại gói hiện gợi ý không đổi được sau khi tạo (Item C)', async ({ page }) => {
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

        // Mở dialog Thêm mới gói tin (chờ bảng có dữ liệu để trang dựng xong; bấm lại nếu lần đầu rơi vào lúc còn đang tải)
        await expect(packetGrid.locator('.vxe-body--row').first()).toBeVisible({ timeout: 30_000 });
        const btnAddPacket = page.locator('.packet-col .el-button').filter({ hasText: /Thêm|Tạo/i }).first();
        const dialog = page.locator('.edit-packet-container .el-dialog');
        await expect(async () => {
            if (!(await dialog.isVisible())) await btnAddPacket.click();
            await expect(dialog).toBeVisible({ timeout: 3000 });
        }).toPass({ timeout: 20_000 });

        // Kiểm tra ô Loại gói (thứ tự) hiển thị và có hint
        const hint = dialog.locator('.packet-order-hint');
        await expect(hint).toBeVisible();
        await expect(hint).toContainText(/sau khi tạo không đổi loại gói được/i);
    });

    /**
     * Author: DatHQ
     * Description: Tắt gói khi còn đăng ký bật (khớp chặn ở máy chủ): mở hộp sửa gói thì ô Tắt khóa; đăng ký bật sau lúc mở hộp thì lúc lưu báo còn đăng ký, trả về trạng thái cũ và không gọi API sửa gói.
     * Created date: 05/11/2026
     */
    for (const c of [
        { name: '49b. Mở hộp sửa gói khi còn đăng ký bật: ô Tắt bị khóa', activeOnOpen: true },
        { name: '49c. Đăng ký bật sau lúc mở hộp: chọn Tắt rồi lưu thì báo còn đăng ký, không gọi API sửa gói', activeOnOpen: false },
    ]) {
        test(c.name, async ({ page }) => {
            let activeSubs: unknown[] = c.activeOnOpen ? [{ id: 'S1', partnerId: 'P1', direction: 0, state: 1 }] : [];
            const updateCalls: unknown[] = [];
            const fulfill = (route: Route, result: unknown) => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ code: 200, result }) });
            await page.route('**/api/cfgsystem/sysconfigtype/alldatalist*', (route) =>
                fulfill(route, { status_type: [{ code: '1', name: 'Bật' }, { code: '0', name: 'Tắt' }] })
            );
            await page.route('**/api/sharedata/sharedatasubscription/list*', (route) => fulfill(route, activeSubs));
            await page.route('**/api/sharedata/sharedatapacket/page*', (route) =>
                fulfill(route, { records: [{ id: 'PKT_01', code: '101_commonData', name: 'Gói tin dùng chung 101', packetVersion: '1.0', orderNo: 101, status: 1 }], total: 1 })
            );
            await page.route('**/api/sharedata/sharedatapacket/update', (route) => {
                updateCalls.push(route.request().postDataJSON());
                return fulfill(route, true);
            });

            await page.setViewportSize({ width: 1600, height: 900 });
            await page.goto('/#/sharedata/dataSource');
            const row = page.locator('.packet-col .vxe-body--row').first();
            await expect(row).toBeVisible({ timeout: 30_000 });
            const editButton = row.locator('.el-button--primary').first();
            await expect(editButton).toBeEnabled({ timeout: 15_000 });
            await editButton.click();

            const dialog = page.locator('.edit-packet-container .el-dialog');
            await expect(dialog).toBeVisible();
            const offRadio = dialog.locator('.el-radio', { hasText: 'Tắt' });
            if (c.activeOnOpen) {
                await expect(offRadio).toHaveClass(/is-disabled/, { timeout: 10_000 });
                return;
            }

            await expect(offRadio).not.toHaveClass(/is-disabled/);
            await offRadio.click();
            activeSubs = [{ id: 'S1', partnerId: 'P1', direction: 1, state: 1 }];
            await dialog.locator('.el-dialog__footer .el-button--primary').click();
            await expect(page.locator('.el-message--warning').last()).toContainText('Vui lòng tắt đăng ký trước khi tắt gói tin', { timeout: 10_000 });
            await expect(dialog.locator('.el-radio', { hasText: 'Bật' })).toHaveClass(/is-checked/);
            expect(updateCalls).toHaveLength(0);
        });
    }

    /**
     * Author: DatHQ
     * Description: Mở hộp sửa hoặc sao chép gói ngay sau khi vào trang: ô Loại gói giữ đúng loại của gói nguồn, không bị đặt về 101.
     * Created date: 05/11/2026
     */
    for (const c of [
        { name: '49d. Sửa gói loại 105 ngay sau khi vào trang: ô Loại gói hiện đúng 105', button: '.el-button--primary' },
        { name: '49e. Sao chép gói loại 105 ngay sau khi vào trang: ô Loại gói giữ đúng 105', button: '.el-button--warning' },
    ]) {
        test(c.name, async ({ page }) => {
            await page.route('**/api/sharedata/sharedatapacket/page*', (route) =>
                route.fulfill({
                    status: 200,
                    contentType: 'application/json',
                    body: JSON.stringify({ code: 200, result: { records: [{ id: 'PKT_05', code: '105_rfid', name: 'Gói 105', packetVersion: '1.0', orderNo: 105, status: 1 }], total: 1 } }),
                })
            );

            await page.setViewportSize({ width: 1600, height: 900 });
            await page.goto('/#/sharedata/dataSource');
            const row = page.locator('.packet-col .vxe-body--row').first();
            await expect(row).toBeVisible({ timeout: 30_000 });
            const button = row.locator(c.button).first();
            await expect(button).toBeEnabled({ timeout: 15_000 });
            await button.click();

            const dialog = page.locator('.edit-packet-container .el-dialog');
            await expect(dialog).toBeVisible();
            await expect(dialog.locator('.el-form-item', { hasText: 'Loại gói' }).locator('.el-select')).toContainText('105');
        });
    }

    /**
     * Description: Tải chậm (biên bản 27/10): mở trang không tự chọn gói đầu tiên; khung phải hiện khung trống (el-empty), chưa gọi API chi tiết;
     *              bấm chọn gói mới gọi API nạp câu truy vấn.
     * Created date: 27/10/2026
     */
    test('50. Mở trang không tự chọn gói, khung phải trống; bấm gói mới nạp chi tiết', async ({ page }) => {
        let detailCalls = 0;
        await page.route('**/api/sharedata/sharedatapacket/getextractsql*', (r) => {
            detailCalls++;
            return r.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ code: 200, result: { extractSql: 'SELECT 1 AS a', status: 1, fields: [] } }) });
        });
        await page.setViewportSize({ width: 1600, height: 900 });
        await page.goto('/#/sharedata/dataSource');
        const firstRow = page.locator('.packet-col .vxe-table--main-wrapper .vxe-body--row').first();
        await expect(firstRow).toBeVisible({ timeout: 30_000 });
        await page.waitForTimeout(1500);
        await expect(page.locator('.field-col .empty-placeholder')).toBeVisible();
        expect(detailCalls).toBe(0);

        await firstRow.click();
        await expect(page.locator('.field-col .empty-placeholder')).toHaveCount(0, { timeout: 10_000 });
        await expect.poll(() => detailCalls, { timeout: 10_000 }).toBeGreaterThan(0);
    });

    test('50b. Đổi từ tab khác về Cấu hình gói tin: khung phải ở trạng thái trống, không hiện sẵn câu truy vấn cũ', async ({ page }) => {
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
                                {
                                    id: 'menu_sharedata_mapping',
                                    pid: 'menu_sharedata',
                                    path: '/shareData/mapping',
                                    name: 'sharedataMapping',
                                    component: 'sharedata/mapping/index',
                                    meta: { title: 'lz.menu.sharedataMapping', isLink: '', isHide: false, isKeepAlive: true, isAffix: false, isIframe: false, roles: ['admin'], icon: 'ele-Connection' },
                                },
                            ],
                        },
                    ],
                }),
            });
        });
        await page.route('**/api/sharedata/sharedatapacket/getpartnersqllist*', (r) =>
            r.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ code: 200, result: [] }) })
        );
        await page.route('**/api/sharedata/sharedataconfig/pagelist*', (r) =>
            r.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ code: 200, result: { rows: [], total: 0 } }) })
        );

        await page.setViewportSize({ width: 1600, height: 900 });
        await page.goto('/#/sharedata/dataSource');
        const firstRow = page.locator('.packet-col .vxe-table--main-wrapper .vxe-body--row').first();
        await expect(firstRow).toBeVisible({ timeout: 30_000 });

        // Chọn 1 gói: khung phải nạp chi tiết
        await firstRow.click();
        await expect(page.locator('.field-col .packet-details-wrapper')).toBeVisible({ timeout: 10_000 });

        // Chuyển sang tab Ánh xạ dữ liệu (nạp danh sách đối tác, gói tin, đăng ký khi mở) rồi quay lại Cấu hình gói tin
        const emptyList = (r: any) => r.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ code: 200, result: [] }) });
        await page.route('**/api/sharedata/sharedatapartner/list*', emptyList);
        await page.route('**/api/sharedata/sharedatapacket/list*', emptyList);
        await page.route('**/api/sharedata/sharedatasubscription/list*', emptyList);
        await page.goto('/#/shareData/mapping');
        await expect(page.locator('#sharedata-mapping-search')).toBeVisible({ timeout: 15_000 });
        await page.goto('/#/sharedata/dataSource');

        // Khung phải quay về trạng thái trống chờ chọn gói, không hiện sẵn truy vấn cũ
        await expect(page.locator('.field-col .empty-placeholder')).toBeVisible({ timeout: 10_000 });
        await expect(page.locator('.field-col .packet-details-wrapper')).toHaveCount(0);
    });

    /**
     * Description: Luồng khóa nối đuôi @lastKey (Adaptive UI):
     *              - Tab Mẫu chung chỉ hiện 3 cột tham số (ẩn Mốc đang chạy), hiển thị badge tự động tịnh tiến + tag khớp __lastKey + ô Mốc bắt đầu;
     *              - Mở ngăn kéo xem mốc đang chạy của các đối tác, đặt lại mốc riêng cho 1 đối tác qua API resetlastsend;
     *              - Chuyển sang tab Đối tác riêng: đóng ngăn kéo, hiện cột Mốc đang chạy trực tiếp trên bảng tham số.
     *              Ô Mốc lần đầu đã đặt có chữ "Từ" đứng trước ngày giờ, không cần nhãn riêng.
     * Created date: 27/10/2026
     * Modified date: 04/11/2026
     */
    test('51. Tham số khóa nối đuôi @lastKey: giao diện thích ứng Mẫu chung vs Đối tác', async ({ page }) => {
        const compositeSql =
            "SELECT TOP (@pageSize) Code AS zoneCode, CONVERT(varchar(23), COALESCE(UpdateTime, CreateTime), 121) + '_' + ID AS __lastKey " +
            "FROM TmsZone WHERE (CONVERT(varchar(23), COALESCE(UpdateTime, CreateTime), 121) + '_' + ID) > @lastKey ORDER BY __lastKey ASC";
        const commonItems = [
            {
                code: 'lastKey',
                dataType: 'String',
                valueKind: 'Builtin',
                value: 'lastKey',
                defaultValue: '2026-10-27 09:00:00.000_',
                savedValue: null,
            },
            {
                code: 'pageSize',
                dataType: 'Int',
                valueKind: 'Const',
                value: '100',
                defaultValue: null,
                savedValue: null,
            },
        ];

        await page.route('**/api/sharedata/sharedatapacket/getsqldetail*', (route) =>
            route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        extract: {
                            extractSql: compositeSql,
                            status: 1,
                            fields: [{ key: 'zoneCode', type: 'string', label: 'Mã khu vực', required: true }],
                        },
                        variables: { items: commonItems },
                        write: { packetCode: '101_commonData', writeSql: '', affectedTables: [], detectedTables: [], keyFields: [] },
                    },
                }),
            })
        );

        await page.route('**/api/sharedata/sharedatapacket/getpartnersqllist*', (route) =>
            route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: [
                        { partnerId: 'p_a', partnerCode: 'CSGT', partnerName: 'Cục CSGT', isCustom: true, updateTime: '2026-10-27T10:00:00Z' },
                        { partnerId: 'p_b', partnerCode: 'VDOT', partnerName: 'Sở GTVT', isCustom: false, updateTime: null },
                    ],
                }),
            })
        );

        await page.route('**/api/sharedata/sharedatapacket/getsqlvariables*', (route) => {
            const url = new URL(route.request().url());
            const partnerId = url.searchParams.get('PartnerId') ?? url.searchParams.get('partnerId');
            const items = commonItems.map((it) => ({
                ...it,
                savedValue: it.code === 'lastKey' && partnerId === 'p_a' ? '2026-10-27 10:05:00.000_Z02' : null,
            }));
            return route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({ code: 200, result: { items } }),
            });
        });

        await page.route('**/api/sharedata/sharedatapacket/getpartnerextractsql*', async (route) => {
            // Giả lập độ trễ mạng thực tế 250ms như trên server
            await new Promise((resolve) => setTimeout(resolve, 250));
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        isCustom: true,
                        customSql: compositeSql,
                        standardSql: compositeSql,
                        fields: [{ key: 'zoneCode', type: 'string', label: 'Mã khu vực', required: true }],
                        commonFields: [{ key: 'zoneCode', type: 'string', label: 'Mã khu vực', required: true }],
                    },
                }),
            });
        });

        await page.setViewportSize({ width: 1600, height: 950 });
        await page.goto('/#/sharedata/dataSource');

        const firstRow = page.locator('.packet-col .vxe-table--main-wrapper .vxe-body--row').first();
        await expect(firstRow).toBeVisible({ timeout: 30_000 });
        await firstRow.click();

        const panel = page.locator('.sql-variable-panel');
        await expect(panel).toBeVisible({ timeout: 10_000 });

        // 1. Tab Mẫu chung: bảng chính chỉ có 3 cột (không có cột Mốc đang chạy), hiện tham số @lastKey và ô nhập mốc bắt đầu
        const mainTable = panel.locator('.svp-table').first();
        await expect(mainTable.locator('thead th')).toHaveCount(3);
        await expect(mainTable.locator('.svp-param-code').first()).toHaveText('@lastKey');
        await expect(mainTable.locator('.cvi-picker input')).toHaveValue('27/10/2026 09:00:00');
        await expect(mainTable.locator('.cvi-from')).toHaveText('Đối tác chưa có mốc: từ');
        await expect(mainTable.locator('.cvi-prefix-label')).toHaveCount(0);

        // 2. Chuyển sang phạm vi Đối tác riêng CSGT: bảng tham số thêm cột So mẫu chung (4 cột)
        await selectPartnerScope(page, 'CSGT');

        await expect(mainTable.locator('thead th')).toHaveCount(4);
        // Bảng đủ 4 cột vẫn không phải cuộn ngang
        // Bảng tính lại độ rộng cột sau khi thêm cột So mẫu chung nên chờ bố cục ổn định
        await expect
            .poll(() => mainTable.locator('.el-table__body-wrapper .el-scrollbar__wrap').first().evaluate((el) => el.scrollWidth - el.clientWidth), { timeout: 5_000 })
            .toBeLessThanOrEqual(1);
    });

    /**
     * Description: Cột giá trị của @lastKey chỉ đặt mốc bắt đầu ở cả tab Mẫu chung lẫn tab đối tác: đối tác đã có mốc đang chạy
     *              (kể cả khi đăng ký gửi đang bật) vẫn hiện ô ngày chọn được, không hiện mốc đang chạy, không có nút sửa, không gọi API đặt mốc;
     *              tab đối tác đã có mốc thì khóa ô Mốc lần đầu, ô ghi đã có mốc, rê chuột vào biểu tượng i xem giải thích và Mốc lần đầu đang lưu (không nêu mốc đang chạy);
     *              tab Mẫu chung và đối tác chưa có mốc thì ô chọn được. Ô không có nhãn riêng, không có biểu tượng i: chữ trong ô tự nói nghĩa;
     *              tiêu đề cột So mẫu chung nằm trên một dòng.
     * Created date: 03/11/2026
     * Modified date: 04/11/2026
     */
    const cursorCellCases = [
        { name: '72a. Đối tác đã có mốc đang chạy: ô Mốc lần đầu khóa, ghi đã có mốc, không hiện Mốc lần đầu đã đặt và mốc đang chạy', savedValue: '2026-10-27 10:05:00.000_Z02', startValue: '2026-10-28 00:00:00.000_', locked: false },
        { name: '72b. Đối tác đã có mốc và đăng ký gửi đang bật: ô Mốc lần đầu khóa, không hiện mốc đang chạy, không có nút sửa', savedValue: '2026-10-27 10:05:00.000_Z02', startValue: null, locked: true },
        { name: '72c. Đối tác chưa có mốc: ô Mốc lần đầu chọn được, không có nút sửa', savedValue: null, startValue: null, locked: false },
    ];
    for (const c of cursorCellCases) {
        test(c.name, async ({ page }) => {
            const sql =
                "SELECT TOP (@pageSize) Code AS zoneCode, CONVERT(varchar(23), COALESCE(UpdateTime, CreateTime), 121) + '_' + ID AS __lastKey " +
                "FROM TmsZone WHERE CONVERT(varchar(23), COALESCE(UpdateTime, CreateTime), 121) + '_' + ID > @lastKey ORDER BY __lastKey ASC";
            const items = (savedValue: string | null, defaultValue: string | null = null) => [
                { code: 'pageSize', dataType: 'Int', valueKind: 'Const', value: '100', defaultValue: null, savedValue: null },
                { code: 'lastKey', dataType: 'String', valueKind: 'Builtin', value: 'lastKey', defaultValue, savedValue },
            ];
            const fulfill = (route: any, result: unknown) => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ code: 200, result }) });
            const setPayloads: any[] = [];

            await page.route('**/api/sharedata/sharedatapacket/getsqldetail*', (route) =>
                fulfill(route, {
                    extract: { extractSql: sql, status: 1, fields: [{ key: 'zoneCode', type: 'string', label: 'Mã khu vực', required: true }] },
                    variables: { items: items(null) },
                    write: { packetCode: '101_commonData', writeSql: '', affectedTables: [], detectedTables: [], keyFields: [] },
                })
            );
            await page.route('**/api/sharedata/sharedatapacket/getpartnersqllist*', (route) =>
                fulfill(route, [{ partnerId: 'p_a', partnerCode: 'CSGT', partnerName: 'Cục CSGT', isCustom: true, updateTime: '2026-10-27T10:00:00Z' }])
            );
            await page.route('**/api/sharedata/sharedatapacket/getsqlvariables*', (route) => {
                const url = new URL(route.request().url());
                const partnerId = url.searchParams.get('PartnerId') ?? url.searchParams.get('partnerId');
                return fulfill(route, { items: partnerId === 'p_a' ? items(c.savedValue, c.startValue) : items(null) });
            });
            await page.route('**/api/sharedata/sharedatapacket/getpartnerextractsql*', (route) =>
                fulfill(route, { isCustom: true, customSql: sql, standardSql: sql, fields: [{ key: 'zoneCode', type: 'string', label: 'Mã khu vực', required: true }], commonFields: [] })
            );
            await page.route('**/api/sharedata/sharedatasubscription/list*', (route) => fulfill(route, c.locked ? [{ partnerId: 'p_a', direction: 0, datatypeId: 'PKT_01' }] : []));
            await page.route('**/api/sharedata/sharedatapacket/setlastsend*', (route) => {
                setPayloads.push(route.request().postDataJSON());
                return fulfill(route, null);
            });

            await page.setViewportSize({ width: 1600, height: 950 });
            await page.goto('/#/sharedata/dataSource');
            const firstRow = page.locator('.packet-col .vxe-table--main-wrapper .vxe-body--row').first();
            await expect(firstRow).toBeVisible({ timeout: 30_000 });
            await firstRow.click();

            const sqlSection = page.locator('.sql-section');
            const fieldsSection = page.locator('.fields-section');
            const fieldEditBtn = page.locator('.field-edit-btn').first();
            const panel = page.locator('.sql-variable-panel');
            const svpCard = panel.locator('.svp-card').first();
            const mainTable = panel.locator('.svp-table').first();
            const pageSizeInput = mainTable.locator('.svp-page-size-input input');
            const picker = mainTable.locator('.cvi-picker input');
            const editBtn = mainTable.locator('.cvi-edit');

            // Tab Mẫu chung: chỉ đặt mốc bắt đầu, để trống là gửi toàn bộ dữ liệu; khi đăng ký gửi đang bật thì cả 3 mục 1, 2, 3 khóa mờ và vô hiệu hóa thao tác
            if (c.locked) {
                await expect(picker).toBeDisabled({ timeout: 10_000 });
                await expect(pageSizeInput).toBeDisabled();
                await expect(sqlSection).toHaveClass(/is-scope-locked/);
                await expect(sqlSection.locator('.sql-editor-container')).toHaveClass(/is-readonly/);
                await expect(sqlSection.locator('.sql-monaco-section')).toHaveClass(/is-readonly/);
                await expect(fieldsSection).toHaveClass(/is-scope-locked/);
                await expect(fieldEditBtn).toBeDisabled();
                await expect(svpCard).toHaveClass(/is-scope-locked/);
            } else {
                await expect(picker).toBeEnabled({ timeout: 10_000 });
                await expect(pageSizeInput).toBeEnabled();
                await expect(sqlSection).not.toHaveClass(/is-scope-locked/);
                await expect(sqlSection.locator('.sql-editor-container')).not.toHaveClass(/is-readonly/);
                await expect(sqlSection.locator('.sql-monaco-section')).not.toHaveClass(/is-readonly/);
                await expect(fieldsSection).not.toHaveClass(/is-scope-locked/);
                await expect(fieldEditBtn).toBeEnabled();
                await expect(svpCard).not.toHaveClass(/is-scope-locked/);
            }
            await expect(picker).toHaveValue('');
            await expect(picker).toHaveAttribute('placeholder', 'Đối tác chưa có mốc: gửi toàn bộ');
            await expect(editBtn).toHaveCount(0);
            await expect(mainTable.locator('.cvi-modes')).toHaveCount(0);
            await expect(mainTable.locator('.cvi-info')).toHaveCount(0);

            await selectPartnerScope(page, 'CSGT');
            await expect(mainTable.locator('thead th')).toHaveCount(4);
            if (c.locked) {
                await expect(sqlSection).toHaveClass(/is-scope-locked/);
                await expect(fieldsSection).toHaveClass(/is-scope-locked/);
                await expect(fieldEditBtn).toBeDisabled();
                await expect(svpCard).toHaveClass(/is-scope-locked/);
                await expect(pageSizeInput).toBeDisabled();
            } else {
                await expect(sqlSection).not.toHaveClass(/is-scope-locked/);
                await expect(fieldsSection).not.toHaveClass(/is-scope-locked/);
                await expect(fieldEditBtn).toBeEnabled();
                await expect(svpCard).not.toHaveClass(/is-scope-locked/);
                await expect(pageSizeInput).toBeEnabled();
            }

            // Tiêu đề So mẫu chung nằm trên một dòng như cột Tham số, không bị cắt chữ
            const headCells = mainTable.locator('thead th .cell');
            const compareHead = await headCells.nth(0).boundingBox();
            const nameHead = await headCells.nth(1).boundingBox();
            expect(compareHead!.height).toBe(nameHead!.height);
            expect(await headCells.nth(0).evaluate((el) => el.scrollWidth <= el.clientWidth)).toBe(true);

            // Ô không có nhãn riêng, không có biểu tượng i; Mốc lần đầu đã đặt không hiện trong ô khóa để khỏi nhầm là mốc đang chạy
            await expect(picker).toHaveValue('');
            await expect(mainTable.locator('.cvi-prefix-label')).toHaveCount(0);
            await expect(mainTable.locator('.cvi-info')).toHaveCount(0);
            await expect(mainTable.locator('.cvi-from')).toHaveCount(0);
            if (c.savedValue) {
                // Đã có mốc: Mốc lần đầu không còn tác dụng nên khóa ô, chữ trong ô nói rõ trạng thái
                await expect(picker).toBeDisabled();
                await expect(picker).toHaveAttribute('placeholder', 'Đã có mốc, gửi tiếp');
            } else {
                await expect(picker).toBeEnabled();
                await expect(picker).toHaveAttribute('placeholder', 'Gửi toàn bộ dữ liệu');
            }
            await expect(editBtn).toHaveCount(0);
            await expect(mainTable).not.toContainText('Z02');
            await expect(page.locator('.sd-set-cursor-dialog')).toHaveCount(0);
            expect(setPayloads).toHaveLength(0);
        });
    }

    /**
     * Description: Chuyển từ tab đối tác đã có mốc sang tab đối tác chưa có mốc (như lúc vừa tạo cấu hình riêng cho đối tác mới):
     *              bảng tham số nạp đúng bộ biến của đối tác đang mở, ô Mốc lần đầu chọn được, không mang trạng thái đã có mốc của tab trước.
     *              Chuyển ngược lại thì hiện đủ bộ biến của đối tác đó, kể cả khi bộ biến về trước câu SQL của tab.
     * Created date: 04/11/2026
     */
    test('72d. Chuyển qua lại giữa đối tác có mốc và đối tác chưa có mốc: bảng tham số đúng bộ biến của tab đang mở', async ({ page }) => {
        const key = "CONVERT(varchar(23), COALESCE(UpdateTime, CreateTime), 121) + '_' + ID";
        const sql = `SELECT TOP (@pageSize) Code AS zoneCode, ${key} AS __lastKey FROM TmsZone WHERE ${key} > @lastKey ORDER BY __lastKey ASC`;
        const noPageSql = `SELECT Code AS zoneCode, ${key} AS __lastKey FROM TmsZone WHERE ${key} > @lastKey ORDER BY __lastKey ASC`;
        const lastKeyItem = (savedValue: string | null) => ({ code: 'lastKey', dataType: 'String', valueKind: 'Builtin', value: 'lastKey', defaultValue: null, savedValue });
        const items = (partnerId: string | null) =>
            partnerId === 'p_a'
                ? [{ code: 'pageSize', dataType: 'Int', valueKind: 'Const', value: '5', defaultValue: null, savedValue: null }, lastKeyItem('2026-10-27 10:05:00.000_Z02')]
                : [lastKeyItem(null)];
        const fulfill = (route: any, result: unknown) => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ code: 200, result }) });
        const variablePartners: string[] = [];

        await page.route('**/api/sharedata/sharedatapacket/getsqldetail*', (route) =>
            fulfill(route, {
                extract: { extractSql: noPageSql, status: 1, fields: [{ key: 'zoneCode', type: 'string', label: 'Mã khu vực', required: true }] },
                variables: { items: items(null) },
                write: { packetCode: '101_commonData', writeSql: '', affectedTables: [], detectedTables: [], keyFields: [] },
            })
        );
        await page.route('**/api/sharedata/sharedatapacket/getpartnersqllist*', (route) =>
            fulfill(route, [
                { partnerId: 'p_a', partnerCode: 'CSGT', partnerName: 'Cục CSGT', isCustom: true, updateTime: '2026-10-27T10:00:00Z' },
                { partnerId: 'p_b', partnerCode: 'VETC', partnerName: 'Đối tác VETC', isCustom: true, updateTime: '2026-10-27T10:00:00Z' },
            ])
        );
        await page.route('**/api/sharedata/sharedatapacket/getsqlvariables*', (route) => {
            const url = new URL(route.request().url());
            const partnerId = url.searchParams.get('PartnerId') ?? url.searchParams.get('partnerId');
            // Bộ biến mẫu chung (không có đối tác) nạp thêm để so nên không tính
            if (partnerId) variablePartners.push(partnerId);
            return fulfill(route, { items: items(partnerId) });
        });
        // Câu SQL về sau bộ biến: bộ biến của tab mới không được lọc theo câu SQL của tab cũ
        await page.route('**/api/sharedata/sharedatapacket/getpartnerextractsql*', async (route) => {
            await new Promise((resolve) => setTimeout(resolve, 300));
            const url = new URL(route.request().url());
            const partnerId = url.searchParams.get('PartnerId') ?? url.searchParams.get('partnerId');
            return fulfill(route, {
                isCustom: true,
                customSql: partnerId === 'p_a' ? sql : noPageSql,
                standardSql: noPageSql,
                fields: [{ key: 'zoneCode', type: 'string', label: 'Mã khu vực', required: true }],
                commonFields: [],
            });
        });
        await page.route('**/api/sharedata/sharedatasubscription/list*', (route) => fulfill(route, []));

        await page.setViewportSize({ width: 1600, height: 950 });
        await page.goto('/#/sharedata/dataSource');
        const firstRow = page.locator('.packet-col .vxe-table--main-wrapper .vxe-body--row').first();
        await expect(firstRow).toBeVisible({ timeout: 30_000 });
        await firstRow.click();

        const table = page.locator('.sql-variable-panel .svp-table').first();
        const picker = table.locator('.cvi-picker input');
        const paramCodes = table.locator('.svp-param-code');
        const expectHasCursorTab = async () => {
            await expect(paramCodes).toHaveText(['@pageSize', '@lastKey'], { timeout: 10_000 });
            await expect(table.locator('.el-input-number input')).toHaveValue('5');
            await expect(picker).toBeDisabled();
            await expect(picker).toHaveAttribute('placeholder', 'Đã có mốc, gửi tiếp');
            expect(variablePartners.at(-1)).toBe('p_a');
        };
        const expectNoCursorTab = async () => {
            await expect(paramCodes).toHaveText(['@lastKey'], { timeout: 10_000 });
            await expect(picker).toBeEnabled();
            await expect(picker).toHaveAttribute('placeholder', 'Gửi toàn bộ dữ liệu');
            expect(variablePartners.at(-1)).toBe('p_b');
        };

        await selectPartnerScope(page, 'CSGT');
        await expectHasCursorTab();
        await selectPartnerScope(page, 'VETC');
        await expectNoCursorTab();
        await selectPartnerScope(page, 'CSGT');
        await expectHasCursorTab();
    });

    /**
     * Description: Khi bấm vào gói tin trên bảng bên trái (xGridPacket):
     *              - Bảng chi tiết bên phải nạp thành công, không bị lỗi cú pháp i18n (SyntaxError: Invalid linked format) hay crash trắng màn hình;
     *              - Khối tham số SQL hiển thị đầy đủ, dòng giải thích mốc hiển thị bình thường.
     * Created date: 28/10/2026
     */
    test('52. Bấm vào gói tin bên table trái: không bị crash trắng màn hình bên phải và dòng giải thích mốc hiển thị bình thường', async ({ page }) => {
        const pageErrors: string[] = [];
        page.on('pageerror', (err) => {
            if (!isSharedLayoutError(err.message)) pageErrors.push(err.message);
        });

        const compositeSql =
            "SELECT TOP (@pageSize) Code AS zoneCode, CONVERT(varchar(23), COALESCE(UpdateTime, CreateTime), 121) + '_' + ID AS __lastKey " +
            "FROM TmsZone WHERE (CONVERT(varchar(23), COALESCE(UpdateTime, CreateTime), 121) + '_' + ID) > @lastKey ORDER BY __lastKey ASC";
        const commonItems = [
            {
                code: 'lastKey',
                dataType: 'String',
                valueKind: 'Builtin',
                value: 'lastKey',
                defaultValue: '2026-10-27 09:00:00.000_',
                savedValue: null,
            },
            {
                code: 'pageSize',
                dataType: 'Int',
                valueKind: 'Const',
                value: '100',
                defaultValue: null,
                savedValue: null,
            },
        ];

        await page.route('**/api/sharedata/sharedatapacket/getsqldetail*', (route) =>
            route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        extract: {
                            extractSql: compositeSql,
                            status: 1,
                            fields: [{ key: 'zoneCode', type: 'string', label: 'Mã khu vực', required: true }],
                        },
                        variables: { items: commonItems },
                        write: { packetCode: '101_commonData', writeSql: '', affectedTables: [], detectedTables: [], keyFields: [] },
                    },
                }),
            })
        );

        await page.route('**/api/sharedata/sharedatapacket/getpartnersqllist*', (route) =>
            route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: [],
                }),
            })
        );

        await page.route('**/api/sharedata/sharedatapacket/getsqlvariables*', (route) =>
            route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({ code: 200, result: { items: commonItems } }),
            })
        );

        await page.route('**/api/sharedata/sharedatapacket/updatesqlvariables*', (route) =>
            route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        items: [
                            {
                                code: 'pageSize',
                                dataType: 'Int',
                                valueKind: 'Const',
                                value: '200',
                                defaultValue: null,
                                savedValue: null,
                            },
                            {
                                code: 'lastKey',
                                dataType: 'String',
                                valueKind: 'Builtin',
                                value: 'lastKey',
                                defaultValue: null,
                                savedValue: null,
                            },
                        ],
                    },
                }),
            })
        );

        await page.setViewportSize({ width: 1600, height: 950 });
        await page.goto('/#/sharedata/dataSource');

        // Bảng gói tin bên trái xuất hiện
        const firstRow = page.locator('.packet-col .vxe-table--main-wrapper .vxe-body--row').first();
        await expect(firstRow).toBeVisible({ timeout: 30_000 });

        // Click vào gói tin bên trái
        await firstRow.click();

        // Kiểm tra bên phải không bị trắng màn hình (không còn empty-placeholder, packet-details-wrapper hiển thị)
        const detailsWrapper = page.locator('.packet-details-wrapper');
        await expect(detailsWrapper).toBeVisible({ timeout: 10_000 });

        // SQL variable panel hiển thị bình thường
        const panel = page.locator('.sql-variable-panel');
        await expect(panel).toBeVisible({ timeout: 10_000 });

        // Dải kiểm nhanh mốc nằm ở tiêu đề mục 3 (bảng tham số), có icon bóng đèn và lead quy chuẩn mốc
        const checks = panel.locator('.svp-params-card .svp-header .svp-checks');
        await expect(checks).toBeVisible();

        // Icon bóng đèn (ele-Opportunity) hiển thị trong phần lead của dải kiểm nhanh
        const bulb = checks.locator('.svp-check-bulb');
        await expect(bulb).toBeVisible();

        // Dải chip bên ngoài (Hình 2) đã được bỏ đi theo yêu cầu để đưa vào popup tiết kiệm không gian
        await expect(checks.locator('.svp-check-chips')).toHaveCount(0);

        // Hover vào lead mở popover quy chuẩn chứa toàn bộ tiêu chuẩn
        await checks.locator('.svp-check-lead').hover();
        const popover = page.locator('.svp-popover-content');
        await expect(popover).toBeVisible({ timeout: 5_000 });

        // Các mục kiểm tra quy chuẩn hiển thị trong popup, chỉ dùng xanh lá hoặc đỏ
        const items = popover.locator('.svp-popover-item');
        expect(await items.count()).toBeGreaterThanOrEqual(3);
        await expect(items.first()).toHaveClass(/is-pass|is-fail/);

        // Tuyệt đối không có bất kỳ lỗi Uncaught SyntaxError / pageerror nào
        expect(pageErrors.filter((e) => e.includes('SyntaxError') || e.includes('Invalid linked format'))).toEqual([]);
    });

    /**
     * Description: Quy chuẩn SQL nối mốc — bóng đèn chỉ sáng vàng khi đạt đủ mọi tiêu chí, chưa đủ thì tắt (xám);
     *              trong popup từng tiêu chí tự mang màu riêng: đạt xanh lá, chưa đạt đỏ (không tô đỏ cả danh sách).
     * Created date: 30/10/2026
     */
    const cursorCheckCases = [
        {
            name: 'đạt đủ 3/3',
            orderBy: 'ORDER BY __lastKey ASC',
            leadText: '(3/3)',
            lit: true,
            itemStates: ['is-pass', 'is-pass', 'is-pass'],
        },
        {
            name: 'chưa đạt ORDER BY DESC 2/3',
            orderBy: 'ORDER BY __lastKey DESC',
            leadText: '(2/3)',
            lit: false,
            itemStates: ['is-pass', 'is-pass', 'is-fail'],
        },
    ];

    for (const c of cursorCheckCases) {
        test(`65. Quy chuẩn mốc ${c.name}: bóng đèn ${c.lit ? 'sáng vàng' : 'tắt'}, mỗi tiêu chí xanh/đỏ theo kết quả riêng`, async ({ page }) => {
            const sql =
                "SELECT TOP (@pageSize) Code AS zoneCode, CONVERT(varchar(23), COALESCE(UpdateTime, CreateTime), 121) + '_' + ID AS __lastKey " +
                `FROM TmsZone WHERE (CONVERT(varchar(23), COALESCE(UpdateTime, CreateTime), 121) + '_' + ID) > @lastKey ${c.orderBy}`;
            const items = [
                { code: 'lastKey', dataType: 'String', valueKind: 'Builtin', value: 'lastKey', defaultValue: null, savedValue: null },
                { code: 'pageSize', dataType: 'Int', valueKind: 'Const', value: '100', defaultValue: null, savedValue: null },
            ];
            await page.route('**/api/sharedata/sharedatapacket/getsqldetail*', (route) =>
                route.fulfill({
                    status: 200,
                    contentType: 'application/json',
                    body: JSON.stringify({
                        code: 200,
                        result: {
                            extract: { extractSql: sql, status: 1, fields: [{ key: 'zoneCode', type: 'string', label: 'Mã khu vực', required: true }] },
                            variables: { items },
                            write: { packetCode: '101_commonData', writeSql: '', affectedTables: [], detectedTables: [], keyFields: [] },
                        },
                    }),
                })
            );
            await page.route('**/api/sharedata/sharedatapacket/getpartnersqllist*', (route) =>
                route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ code: 200, result: [] }) })
            );
            await page.route('**/api/sharedata/sharedatapacket/getsqlvariables*', (route) =>
                route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ code: 200, result: { items } }) })
            );

            await page.setViewportSize({ width: 1600, height: 950 });
            await page.goto('/#/sharedata/dataSource');
            const firstRow = page.locator('.packet-col .vxe-table--main-wrapper .vxe-body--row').first();
            await expect(firstRow).toBeVisible({ timeout: 30_000 });
            await firstRow.click();

            const lead = page.locator('.sql-variable-panel .svp-check-lead');
            await expect(lead).toContainText(c.leadText, { timeout: 10_000 });

            const litColor = 'rgb(245, 196, 0)';
            const bulbColor = await lead.locator('.svp-check-bulb').evaluate((el) => getComputedStyle(el).color);
            if (c.lit) expect(bulbColor).toBe(litColor);
            else expect(bulbColor).not.toBe(litColor);

            await lead.hover();
            const popover = page.locator('.svp-check-popover .svp-popover-content');
            await expect(popover).toBeVisible({ timeout: 5_000 });

            const titleBulb = popover.locator('.svp-popover-title .svp-check-bulb');
            await expect(titleBulb).toHaveClass(c.lit ? /is-lit/ : /is-unlit/);
            const titleBulbColor = await titleBulb.evaluate((el) => getComputedStyle(el).color);
            if (c.lit) expect(titleBulbColor).toBe(litColor);
            else expect(titleBulbColor).not.toBe(litColor);

            const rows = popover.locator('.svp-popover-item');
            await expect(rows).toHaveCount(c.itemStates.length);
            const [successColor, dangerColor] = await page.evaluate(() => {
                const s = getComputedStyle(document.documentElement);
                const probe = document.createElement('span');
                document.body.appendChild(probe);
                probe.style.color = s.getPropertyValue('--el-color-success').trim();
                const success = getComputedStyle(probe).color;
                probe.style.color = s.getPropertyValue('--el-color-danger').trim();
                const danger = getComputedStyle(probe).color;
                probe.remove();
                return [success, danger];
            });
            for (let i = 0; i < c.itemStates.length; i++) {
                const row = rows.nth(i);
                await expect(row).toHaveClass(new RegExp(c.itemStates[i]));
                const expected = c.itemStates[i] === 'is-pass' ? successColor : dangerColor;
                expect(await row.evaluate((el) => getComputedStyle(el).color)).toBe(expected);
                expect(await row.locator('.svp-check-icon').evaluate((el) => getComputedStyle(el).color)).toBe(expected);
            }
        });
    }

    /**
     * Description: Báo lỗi SQL hiển thị rõ tên trường bị lỗi (không để trống sau dấu hai chấm),
     *              khi có thay đổi chưa lưu bấm chuyển tab đối tác (DDLT-2) thì chặn ngay từ đầu
     *              và hộp thoại cảnh báo (.lost-keys-confirm-box) đặt icon warning kế bên chữ 'Thông báo'.
     * Created date: 30/10/2026
     */
    test('66. Chặn chuyển tab đối tác khi có thay đổi chưa lưu, báo lỗi SQL kèm tên trường và hộp thoại confirm có icon cạnh Thông báo', async ({ page }) => {
        let returnError = true;
        const testSql = "SELECT TOP (@pageSize) zs.ID AS zoneStatusId, zs.Name AS zoneName FROM TmsZoneStatus zs WHERE zs.ID > @lastKey ORDER BY zs.ID ASC";

        const commonItems = [
            {
                code: 'pageSize',
                dataType: 'Int',
                valueKind: 'Const',
                value: '100',
                defaultValue: null,
                savedValue: null,
            },
            {
                code: 'lastKey',
                dataType: 'String',
                valueKind: 'Builtin',
                value: 'lastKey',
                defaultValue: null,
                savedValue: null,
            },
        ];

        await page.route('**/api/sharedata/sharedatapacket/getsqldetail*', (route) =>
            route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        extract: {
                            extractSql: testSql,
                            status: 1,
                            fields: [
                                { key: 'zoneStatusId', type: 'string', label: 'ID trạng thái' },
                                { key: 'zoneName', type: 'string', label: 'Tên trạng thái' },
                            ],
                        },
                        variables: { items: commonItems },
                        write: { packetCode: '101_commonData', writeSql: '', affectedTables: [], detectedTables: [], keyFields: [] },
                    },
                }),
            })
        );

        await page.route('**/api/sharedata/sharedatapacket/getextractsql*', (route) =>
            route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        extractSql: testSql,
                        status: 1,
                        fields: [
                            { key: 'zoneStatusId', type: 'string', label: 'ID trạng thái' },
                            { key: 'zoneName', type: 'string', label: 'Tên trạng thái' },
                        ],
                    },
                }),
            })
        );

        await page.route('**/api/sharedata/sharedatapacket/getpartnersqllist*', (route) =>
            route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: [
                        { partnerId: 'p_ddlt2', partnerCode: 'DDLT-2', partnerName: 'Đồng Đăng - Trà Lĩnh 2', isCustom: true },
                    ],
                }),
            })
        );

        await page.route('**/api/sharedata/sharedatapacket/getsqlvariables*', (route) =>
            route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({ code: 200, result: { items: commonItems } }),
            })
        );

        await page.route('**/api/sharedata/sharedatapacket/updatesqlvariables*', (route) =>
            route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        items: [
                            {
                                code: 'pageSize',
                                dataType: 'Int',
                                valueKind: 'Const',
                                value: '200',
                                defaultValue: null,
                                savedValue: null,
                            },
                            {
                                code: 'lastKey',
                                dataType: 'String',
                                valueKind: 'Builtin',
                                value: 'lastKey',
                                defaultValue: null,
                                savedValue: null,
                            },
                        ],
                    },
                }),
            })
        );

        // Mock updateextractsql: lần đầu trả về 400 sqlDatabaseExecutionError; lần sau trả về requiresConfirm
        await page.route('**/api/sharedata/sharedatapacket/updateextractsql*', async (route) => {
            if (returnError) {
                await route.fulfill({
                    status: 400,
                    contentType: 'application/json',
                    body: JSON.stringify({
                        code: 400,
                        message: 'lz.validation.sharedata.sqlDatabaseExecutionError',
                    }),
                });
                return;
            }
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        requiresConfirm: true,
                        lost: ['fromLocationKm'],
                        affectedMappings: [
                            { mappingCode: 'MAP01', mappingName: 'Hồ sơ 1', keys: ['fromLocationKm'] },
                        ],
                    },
                }),
            });
        });

        // Mock parseextractcolumns returning Invalid column name 'Name'
        await page.route('**/api/sharedata/sharedatapacket/parseextractcolumns*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        isValid: false,
                        errorMessage: "Cú pháp SQL không hợp lệ trên cơ sở dữ liệu: Invalid column name 'Name'.",
                        errorLine: 1,
                        errorColumn: 45,
                        columns: [],
                    },
                }),
            });
        });

        await page.setViewportSize({ width: 1600, height: 950 });
        await page.goto('/#/sharedata/dataSource');

        // Bấm chọn gói tin đầu tiên
        const packetRow = page.locator('.packet-col .vxe-table--main-wrapper .vxe-body--row').first();
        await expect(packetRow).toBeVisible({ timeout: 30_000 });
        await packetRow.click();
        await page.waitForTimeout(600);

        // Chờ ô chọn phạm vi SQL hiển thị Mẫu chung
        const scopeSelect = page.locator('.partner-tab-jump');
        await expect(scopeSelect).toBeVisible({ timeout: 10_000 });
        await expect(scopeSelect).toContainText('Mẫu chung');

        // 1. Sửa tham số để tạo trạng thái chưa lưu (dirty)
        const paramInput = page.locator('.sql-variable-panel .el-table__body .svp-const-input input').first();
        await paramInput.scrollIntoViewIfNeeded();
        await expect(paramInput).toBeVisible({ timeout: 10_000 });
        const domInfo = await page.evaluate(() => {
            const table = document.querySelector('.sql-variable-panel .svp-table');
            if (!table) return 'NO_TABLE';
            const trs = Array.from(table.querySelectorAll('tbody tr'));
            const lastTr = trs[trs.length - 1];
            return {
                tableRect: table.getBoundingClientRect(),
                lastTrRect: lastTr?.getBoundingClientRect(),
                diff: table.getBoundingClientRect().bottom - (lastTr ? lastTr.getBoundingClientRect().bottom : 0),
            };
        });
        console.log('REAL_TABLE_GAP:', JSON.stringify(domInfo, null, 2));
        await page.locator('.sql-variable-panel').screenshot({ path: '/tmp/panel_real.png' });
        await paramInput.fill('200');
        await page.waitForTimeout(300);

        // 2. Chọn sang đối tác DDLT-2 -> Phải chặn ngay từ đầu, hỏi cảnh báo
        await scopeSelect.click();
        const ddlt2Option = page.locator('.partner-tab-jump-popper:visible .el-select-dropdown__item', { hasText: 'DDLT-2' }).first();
        await expect(ddlt2Option).toBeVisible({ timeout: 5000 });
        await ddlt2Option.click();
        await page.waitForTimeout(300);

        // Khẳng định: Phạm vi hiện tại VẪN LÀ Mẫu chung (chưa nhảy sang DDLT-2)
        await expect(scopeSelect).toContainText('Mẫu chung');

        // Hộp thoại confirm cảnh báo chuyển tab xuất hiện
        const tabLeaveConfirmBox = page.locator('.lost-keys-confirm-box');
        await expect(tabLeaveConfirmBox).toBeVisible({ timeout: 5000 });
        await expect(tabLeaveConfirmBox).toContainText('Nội dung câu lệnh SQL đã thay đổi nhưng chưa được lưu');

        // Icon warning nằm bên trong header kế bên chữ Thông báo, nằm bên trái (không bị center, không trôi ở body)
        const tabTitle = tabLeaveConfirmBox.locator('.el-message-box__title');
        await expect(tabTitle).toContainText('Thông báo');
        await expect(tabTitle.locator('.el-message-box__status')).toBeVisible();
        await expect(tabTitle).toHaveCSS('justify-content', 'flex-start');
        await expect(tabLeaveConfirmBox.locator('.el-message-box__container > .el-message-box__status')).toHaveCount(0);

        // Bấm nút Bỏ qua -> Hộp thoại đóng, phạm vi vẫn ở Mẫu chung (không nhảy sang DDLT-2)
        const cancelTabBtn = tabLeaveConfirmBox.locator('.el-message-box__btns button').first();
        await cancelTabBtn.click();
        await expect(tabLeaveConfirmBox).toBeHidden({ timeout: 3000 });
        await expect(scopeSelect).toContainText('Mẫu chung');

        // 3. Thay đổi câu SQL và bấm Lưu & phân tích -> nhận lỗi SQL kèm tên trường 'Name' (không bị cụt dấu ':')
        await page.locator('.monaco-editor').first().click();
        await page.keyboard.press('End');
        await page.keyboard.type(' -- test');
        await page.waitForTimeout(600);

        const saveBtn = page.locator('.sql-toolbar-right button').filter({ hasText: 'Lưu & phân tích' }).first();
        await expect(saveBtn).toBeEnabled({ timeout: 5000 });
        await saveBtn.click();
        await page.waitForTimeout(600);

        const errorAlert = page.locator('.sql-error-alert');
        await expect(errorAlert).toBeVisible({ timeout: 5000 });
        const errorText = await errorAlert.innerText();
        expect(errorText).toContain('Name');
        expect(errorText).toContain("Cột 'Name' không tồn tại trên cơ sở dữ liệu.");
        expect(errorText).not.toMatch(/Cú pháp SQL không hợp lệ trên cơ sở dữ liệu:\s*$/);

        // 4. Lần lưu thứ hai (requiresConfirm) -> hộp thoại cảnh báo mất trường xuất hiện
        returnError = false;
        await page.locator('.monaco-editor').first().click();
        await page.keyboard.press('End');
        await page.keyboard.type('2');
        await page.waitForTimeout(600);

        await expect(saveBtn).toBeEnabled({ timeout: 5000 });
        await saveBtn.click();

        const lostKeysConfirmBox = page.locator('.lost-keys-confirm-box');
        await expect(lostKeysConfirmBox).toBeVisible({ timeout: 5000 });

        // Header có icon warning kế bên tiêu đề Thông báo nằm bên trái (không bị center)
        const lostTitle = lostKeysConfirmBox.locator('.el-message-box__title');
        await expect(lostTitle).toContainText('Thông báo');
        await expect(lostTitle.locator('.el-message-box__status')).toBeVisible();
        await expect(lostTitle).toHaveCSS('justify-content', 'flex-start');
        await expect(lostKeysConfirmBox.locator('.el-message-box__container > .el-message-box__status')).toHaveCount(0);

        // Đóng hộp thoại
        const cancelBtn = lostKeysConfirmBox.locator('.el-message-box__btns button').first();
        await cancelBtn.click();
        await expect(lostKeysConfirmBox).toBeHidden({ timeout: 3000 });
    });

    /**
     * Description: Chuyển đổi qua lại giữa Mẫu chung và các đối tác qua ô Chọn đối tác (.partner-tab-jump):
     *              - Bấm chọn đối tác ddlt: nạp đúng câu SQL riêng của đối tác;
     *              - Bấm chọn đối tác DDTL-2: nạp đúng câu SQL riêng của DDTL-2;
     *              - Bấm chọn lại Mẫu chung: nạp lại câu SQL mẫu chung.
     * Created date: 28/10/2026
     * Modified date: 11/11/2026
     */
    test('53. Chuyển đổi qua lại giữa Mẫu chung và các đối tác qua ô Chọn đối tác', async ({ page }) => {
        const compositeSql =
            "SELECT TOP (@pageSize) Code AS zoneCode, CONVERT(varchar(23), COALESCE(UpdateTime, CreateTime), 121) + '_' + ID AS __lastKey " +
            "FROM TmsZone WHERE (CONVERT(varchar(23), COALESCE(UpdateTime, CreateTime), 121) + '_' + ID) > @lastKey ORDER BY __lastKey ASC";

        await page.route('**/api/sharedata/sharedatapacket/getsqldetail*', (route) =>
            route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        extract: {
                            extractSql: compositeSql,
                            status: 1,
                            fields: [{ key: 'zoneCode', type: 'string', label: 'Mã khu vực', required: true }],
                        },
                        variables: { items: [] },
                        write: { packetCode: '101_commonData', writeSql: '', affectedTables: [], detectedTables: [], keyFields: [] },
                    },
                }),
            })
        );

        await page.route('**/api/sharedata/sharedatapacket/getpartnersqllist*', (route) =>
            route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: [
                        { partnerId: 'p_ddlt', partnerCode: 'ddlt', partnerName: 'ddlt', isCustom: true, updateTime: '2026-10-28T10:00:00Z' },
                        { partnerId: 'p_ddtl2', partnerCode: 'DDTL-2', partnerName: 'DDTL-2', isCustom: true, updateTime: '2026-10-28T10:05:00Z' },
                    ],
                }),
            })
        );

        await page.route('**/api/sharedata/sharedatapacket/getpartnerextractsql*', async (route) => {
            const url = new URL(route.request().url());
            const partnerId = url.searchParams.get('PartnerId') ?? url.searchParams.get('partnerId');
            const customSql = partnerId === 'p_ddlt' ? `${compositeSql} -- ddlt` : `${compositeSql} -- DDTL-2`;
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        isCustom: true,
                        customSql,
                        standardSql: compositeSql,
                        fields: [{ key: 'zoneCode', type: 'string', label: 'Mã khu vực', required: true }],
                        commonFields: [{ key: 'zoneCode', type: 'string', label: 'Mã khu vực', required: true }],
                    },
                }),
            });
        });

        await page.setViewportSize({ width: 1600, height: 950 });
        await page.goto('/#/sharedata/dataSource');

        const firstRow = page.locator('.packet-col .vxe-table--main-wrapper .vxe-body--row').first();
        await expect(firstRow).toBeVisible({ timeout: 30_000 });
        await firstRow.click();

        const scopeSelect = page.locator('.partner-tab-jump');
        await expect(scopeSelect).toBeVisible({ timeout: 10_000 });
        await expect(scopeSelect).toContainText('Mẫu chung');

        // Bấm ddlt
        await selectPartnerScope(page, 'ddlt');
        await expect(scopeSelect).toContainText('ddlt');
        await expect(page.locator('.sql-editor-section .monaco-editor').first()).toContainText('ddlt', { timeout: 10_000 });

        // Bấm DDTL-2
        await selectPartnerScope(page, 'DDTL-2');
        await expect(scopeSelect).toContainText('DDTL-2');
        await expect(page.locator('.sql-editor-section .monaco-editor').first()).toContainText('DDTL-2', { timeout: 10_000 });

        // Bấm Mẫu chung
        await selectPartnerScope(page, 'Mẫu chung');
        await expect(scopeSelect).toContainText('Mẫu chung');
    });

    /**
     * Description: Điều tra lỗi bấm gói 102 (102_cctvData) sau khi xem gói 101/103:
     *              - Bấm gói 103, sau đó bấm gói 101, rồi bấm gói 102;
     *              - Toàn bộ API trả về 200 OK;
     *              - Kiểm tra lớp phủ loading (.solid-loading-mask / .el-loading-mask) phải biến mất hoàn toàn
     *                và danh sách trường của gói 102 hiển thị đúng; không bị kẹt spinner loading.
     * Created date: 28/10/2026
     * Modified date: 29/10/2026 — giữ phản hồi gói 102 tới khi kiểm xong khóa cuộn (mock trả ngay thì lúc lăn chuột gói đã nạp xong)
     */
    test('54. Điều tra lỗi bấm gói 102: API trả về 200 nhưng điều tra trạng thái loading', async ({ page }) => {
        const pageErrors: string[] = [];
        page.on('pageerror', (err) => {
            if (!isSharedLayoutError(err.message)) pageErrors.push(err.message);
        });

        let release102!: () => void;
        const hold102 = new Promise<void>((resolve) => {
            release102 = () => resolve();
        });

        await page.route('**/api/sharedata/sharedatapacket/page*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        records: [
                            { id: 'PKT_01', code: '101_commonData', name: 'Gói 101 - Dữ liệu dùng chung', status: 1 },
                            { id: 'PKT_02', code: '102_cctvData', name: 'Gói 102 - Dữ liệu camera giám sát (CCTV)', status: 1 },
                            { id: 'PKT_03', code: '103_vdsData', name: 'Gói 103 - Dữ liệu đếm xe (VDS)', status: 1 },
                        ],
                        total: 3,
                    },
                }),
            });
        });

        await page.route('**/api/sharedata/sharedatapacket/getsqldetail*', async (route) => {
            const url = new URL(route.request().url());
            const packetCode = url.searchParams.get('PacketCode') || '';

            if (packetCode === '102_cctvData') {
                await hold102;
                await route.fulfill({
                    status: 200,
                    contentType: 'application/json',
                    body: JSON.stringify({
                        code: 200,
                        result: {
                            extract: {
                                id: 'EXT_102',
                                packetCode: '102_cctvData',
                                extractSql: 'SELECT e.Code AS cameraCode, c.Name AS cameraName FROM CctvDevice c',
                                status: 1,
                                fields: [
                                    { key: 'cameraCode', type: 'string', label: 'Mã camera', required: true },
                                    { key: 'cameraName', type: 'string', label: 'Tên camera', required: false },
                                ],
                            },
                            variables: { packetCode: '102_cctvData', items: [] },
                            write: { packetCode: '102_cctvData', writeSql: '', affectedTables: [], detectedTables: [], keyFields: [] },
                        },
                    }),
                });
            } else if (packetCode === '101_commonData') {
                await route.fulfill({
                    status: 200,
                    contentType: 'application/json',
                    body: JSON.stringify({
                        code: 200,
                        result: {
                            extract: {
                                id: 'EXT_101',
                                packetCode: '101_commonData',
                                extractSql: 'SELECT z.FromKmNumber AS fromLocationKm, z.FromMetNumber AS fromLocationMet FROM TmsZone z',
                                status: 1,
                                fields: [
                                    { key: 'fromLocationKm', type: 'number', label: 'Lý trình đầu (km)', required: false },
                                    { key: 'fromLocationMet', type: 'number', label: 'Lý trình đầu (m)', required: false },
                                ],
                            },
                            variables: { packetCode: '101_commonData', items: [] },
                            write: { packetCode: '101_commonData', writeSql: '', affectedTables: [], detectedTables: [], keyFields: [] },
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
                            extract: {
                                id: 'EXT_103',
                                packetCode: '103_vdsData',
                                extractSql: 'SELECT td.ID AS detectionId FROM TmsTrafficData td',
                                status: 1,
                                fields: [
                                    { key: 'detectionId', type: 'string', label: 'Mã phát hiện', required: true },
                                ],
                            },
                            variables: { packetCode: '103_vdsData', items: [] },
                            write: { packetCode: '103_vdsData', writeSql: '', affectedTables: [], detectedTables: [], keyFields: [] },
                        },
                    }),
                });
            }
        });

        await page.route('**/api/sharedata/sharedatapacket/getpartnersqllist*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: [],
                }),
            });
        });

        await page.setViewportSize({ width: 1600, height: 950 });
        await page.goto('/#/sharedata/dataSource');

        const rows = page.locator('.packet-col .vxe-table--main-wrapper .vxe-body--row');
        await expect(rows).toHaveCount(3, { timeout: 30_000 });

        // 1. Bấm gói 103
        await rows.nth(2).click();
        await page.waitForTimeout(500);

        // 2. Bấm gói 101
        await rows.nth(0).click();
        // Chờ dữ liệu gói 101 hiện (fromLocationKm)
        const fieldsGrid = page.locator('.saved-fields-pane');
        await expect(fieldsGrid).toContainText('fromLocationKm', { timeout: 10_000 });

        // Cuộn xuống tận đáy panel chi tiết gói 101
        const detailsWrapper = page.locator('.packet-details-wrapper');
        await detailsWrapper.evaluate((el) => {
            el.scrollTop = 500;
        });

        // 3. Bấm gói 102
        await rows.nth(1).click();

        // Khẳng định: ngay khi bấm đổi gói, scrollTop lập tức được reset về 0 và khoá cuộn
        const switchingScrollTop = await detailsWrapper.evaluate((el) => el.scrollTop);
        expect(switchingScrollTop).toBe(0);

        // Thử cuộn chuột xuống trong lúc đang đổi gói -> scrollTop vẫn bị khoá ở 0
        await detailsWrapper.hover();
        await page.mouse.wheel(0, 400);
        const lockedScrollTop = await detailsWrapper.evaluate((el) => el.scrollTop);
        expect(lockedScrollTop).toBe(0);

        // Cho API gói 102 trả về: mặt nạ loading phải biến mất hoàn toàn
        release102();
        const solidLoading = page.locator('.solid-loading-mask');
        await expect(solidLoading).toHaveCount(0, { timeout: 5_000 });
        await expect(detailsWrapper).not.toHaveClass(/is-switching/);

        // Danh sách trường của gói 102 phải hiển thị (cameraCode), không còn là fromLocationKm
        await expect(fieldsGrid).toContainText('cameraCode', { timeout: 5_000 });
        await expect(fieldsGrid).not.toContainText('fromLocationKm');

        expect(pageErrors).toEqual([]);
    });

    /**
     * Description: Khung soạn thảo SqlEditor: khi nạp câu truy vấn SQL dài nhiều dòng lần đầu (hoặc khi đổi gói tin),
     *              khung soạn thảo luôn hiển thị từ dòng 1 ở trên cùng, không bị tự cuộn xuống cuối (scrollTop = 0, dòng 1 nằm trong vùng nhìn thấy).
     * Created date: 29/10/2026
     */
    test('55. Khung soạn thảo SqlEditor: nạp câu SQL dài nhiều dòng hiển thị từ dòng 1 ở trên cùng, không tự cuộn xuống đáy', async ({ page }) => {
        const pageErrors: string[] = [];
        page.on('pageerror', (err) => {
            if (!isSharedLayoutError(err.message)) pageErrors.push(err.message);
        });

        const longMultiLineSql = [
            'SELECT',
            '    i.ID AS transactionId,',
            '    i.TollStationCode AS stationCode,',
            '    i.LaneNumber AS laneNo,',
            '    i.PlateEdit AS plateNumber,',
            '    i.PlateLpr AS lprPlateNumber,',
            '    i.VehicleType AS vehicleCategory,',
            '    i.Price AS tollFee,',
            '    vr.Brand AS vehicleBrand,',
            '    vr.Owner AS vehicleOwner',
            'FROM TollTransactionIn i',
            'LEFT JOIN TmsVehicleRegistration vr',
            '    ON NULLIF(ISNULL(NULLIF(i.PlateEdit, \'\'), NULLIF(i.PlateLpr, \'\')), \'\') = vr.LicensePlate',
            '    AND vr.IsDelete IS NULL',
            'WHERE i.IsDelete IS NULL',
            '    AND (@lastTime IS NULL OR i.TransactionDateTime > @lastTime)',
            'ORDER BY i.TransactionDateTime ASC',
        ].join('\n');

        await page.route('**/api/sharedata/sharedatapacket/page*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        records: [
                            { id: 'PKT_01', code: '101_commonData', name: 'Gói 101 - Dữ liệu dùng chung', status: 1 },
                        ],
                        total: 1,
                    },
                }),
            });
        });

        await page.route('**/api/sharedata/sharedatapacket/getsqldetail*', (route) =>
            route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        extract: {
                            extractSql: longMultiLineSql,
                            status: 1,
                            fields: [{ key: 'transactionId', type: 'string', label: 'Mã giao dịch', required: true }],
                        },
                        variables: { items: [] },
                        write: { packetCode: '101_commonData', writeSql: '', affectedTables: [], detectedTables: [], keyFields: [] },
                    },
                }),
            })
        );

        await page.route('**/api/sharedata/sharedatapacket/getpartnersqllist*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({ code: 200, result: [] }),
            });
        });

        await page.setViewportSize({ width: 1600, height: 950 });
        await page.goto('/#/sharedata/dataSource');

        const firstRow = page.locator('.packet-col .vxe-table--main-wrapper .vxe-body--row').first();
        await expect(firstRow).toBeVisible({ timeout: 30_000 });
        await firstRow.click();

        // Chờ Monaco editor hiển thị và nạp xong câu truy vấn
        const editorContainer = page.locator('.sql-editor-section .sql-monaco-editor');
        await expect(editorContainer).toBeVisible({ timeout: 15_000 });

        // Chờ editor nạp xong nội dung (có dòng SELECT)
        const firstLine = editorContainer.locator('.view-line').first();
        await expect(firstLine).toContainText('SELECT', { timeout: 10_000 });

        // Khẳng định: dòng đầu tiên trong khung nhìn là dòng 1 (SELECT), không phải dòng 10 hay dòng 17 (ORDER BY)
        const firstGutterLine = editorContainer.locator('.margin-view-overlays .line-numbers').first();
        await expect(firstGutterLine).toHaveText('1');

        // Khẳng định: thanh cuộn dọc đang ở đỉnh (top: 0px)
        const scrollbarSlider = editorContainer.locator('.scrollbar.vertical .slider');
        const sliderTop = await scrollbarSlider.evaluate((el: HTMLElement) => parseInt(el.style.top || '0', 10));
        expect(sliderTop).toBe(0);

        expect(pageErrors).toEqual([]);
    });

    /**
     * Description: Chỉnh sửa trường gói tin: Hỗ trợ chọn nhóm có sẵn (như Dữ liệu xuất, Header) hoặc tạo nhóm mới ngay trong dialog
     * Created date: 29/10/2026
     */
    test('56. Chỉnh sửa trường: chọn nhóm có sẵn (Dữ liệu xuất, Header) hoặc nhập nhóm mới và lưu', async ({ page }) => {
        const pageErrors: string[] = [];
        page.on('pageerror', (err) => {
            if (!isSharedLayoutError(err.message)) pageErrors.push(err.message);
        });

        let savedUpdate: any = null;

        await page.route('**/api/sharedata/sharedatapacket/page*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        records: [
                            { id: 'PKT_104', code: '104_weatherData', name: 'Gói 104 - Dữ liệu thời tiết', status: 1 },
                        ],
                        total: 1,
                    },
                }),
            });
        });

        await page.route('**/api/sharedata/sharedatapacket/getsqldetail*', (route) =>
            route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        extract: {
                            extractSql: 'SELECT sessionid, temperature, humidity FROM TmsWeather',
                            status: 1,
                            fields: [
                                { key: 'sessionid', type: 'string', label: 'Phiên làm việc', required: true, group: 'Header' },
                                { key: 'temperature', type: 'number', label: 'Nhiệt độ mặt đường', required: false, group: 'Dữ liệu xuất' },
                                { key: 'humidity', type: 'number', label: 'Độ ẩm không khí', required: false, group: null },
                            ],
                        },
                        variables: { items: [] },
                        write: { packetCode: '104_weatherData', writeSql: '', affectedTables: [], detectedTables: [], keyFields: [] },
                    },
                }),
            })
        );

        await page.route('**/api/sharedata/sharedatapacket/getpartnersqllist*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({ code: 200, result: [] }),
            });
        });

        await page.route('**/api/sharedata/sharedatapacket/updateextractsql*', async (route) => {
            savedUpdate = route.request().postDataJSON();
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        status: 1,
                        fields: savedUpdate.fields || [],
                    },
                }),
            });
        });

        await page.setViewportSize({ width: 1600, height: 950 });
        await page.goto('/#/sharedata/dataSource');

        const firstPacket = page.locator('.packet-col .vxe-table--main-wrapper .vxe-body--row').first();
        await expect(firstPacket).toBeVisible({ timeout: 30_000 });
        await firstPacket.click();

        // Kiểm tra cây trường dữ liệu đã nhóm theo "Header" và "Dữ liệu xuất"
        const fieldTree = page.locator('.fields-section .vxe-table--main-wrapper');
        await expect(fieldTree).toContainText('Header', { timeout: 15_000 });
        await expect(fieldTree).toContainText('Dữ liệu xuất', { timeout: 15_000 });

        // Mở dialog chỉnh sửa trường humidity (chưa có nhóm)
        const editBtns = page.locator('.field-edit-btn');
        await expect(editBtns.first()).toBeVisible({ timeout: 10_000 });
        await editBtns.last().click();

        const editDialog = page.locator('.field-edit-dialog');
        await expect(editDialog).toBeVisible();

        // Kiểm tra trường Nhóm có sẵn các option từ gói tin 104 ('Header', 'Dữ liệu xuất')
        const groupFormItem = editDialog.locator('.el-form-item', { hasText: 'Nhóm' });
        await expect(groupFormItem).toBeVisible();

        // Nút icon Thêm nhóm hiển thị rõ ràng
        const addGroupBtn = groupFormItem.locator('.field-add-group-btn');
        await expect(addGroupBtn).toBeVisible();

        const groupSelect = groupFormItem.locator('.el-select');
        await groupSelect.click();

        // Dropdown hiện ra danh sách nhóm có sẵn
        const popper = page.locator('.el-select-dropdown:visible');
        await expect(popper).toContainText('Header');
        await expect(popper).toContainText('Dữ liệu xuất');

        // Chọn nhóm "Dữ liệu xuất"
        await popper.locator('.el-select-dropdown__item', { hasText: 'Dữ liệu xuất' }).click();

        // Bấm Lưu
        await editDialog.locator('.dialog-footer .el-button--primary').click();
        await expect(editDialog).toBeHidden();

        // Thông báo thành công hiển thị đúng ngữ cảnh chỉnh sửa trường dữ liệu, không thông báo sai "Lưu cấu hình SQL"
        // Lọc theo chữ: thông báo kết nối NATS cũng là el-message--success
        const successToast = page.locator('.el-message--success').filter({ hasText: 'trường dữ liệu' });
        await expect(successToast).toBeVisible();
        await expect(successToast).toContainText('trường dữ liệu');
        await expect(successToast).not.toContainText('cấu hình SQL');

        // Xác nhận payload gửi lên BE có chứa group đã chọn
        expect(savedUpdate).toBeTruthy();
        const savedHumidity = savedUpdate.fields.find((f: any) => f.key === 'humidity');
        expect(savedHumidity).toBeTruthy();
        expect(savedHumidity.group).toBe('Dữ liệu xuất');

        // Mở lại dialog để thêm nhóm mới bằng nút Add Group
        await editBtns.first().click();
        await expect(editDialog).toBeVisible();

        await addGroupBtn.click();
        const msgBox = page.locator('.el-message-box');
        await expect(msgBox).toBeVisible();
        await msgBox.locator('input').fill('Nhóm môi trường');
        await msgBox.locator('.el-message-box__btns .el-button--primary').click();
        await expect(msgBox).toBeHidden();

        // Bấm Lưu
        await editDialog.locator('.dialog-footer .el-button--primary').click();
        await expect(editDialog).toBeHidden();

        const savedSession = savedUpdate.fields.find((f: any) => f.key === 'sessionid');
        expect(savedSession).toBeTruthy();
        expect(savedSession.group).toBe('Nhóm môi trường');

        expect(pageErrors).toEqual([]);
    });

    test('57. Lọc theo đối tác dùng chung (isCustom=false) thì ở tab Mẫu chung, không sinh tab tạm và không có khóa i18n thô', async ({ page }) => {
        const pageErrors: string[] = [];
        page.on('pageerror', (err) => {
            if (!isSharedLayoutError(err.message)) pageErrors.push(err.message);
        });

        await page.route('**/api/sharedata/sharedatapartner/list*', (route) =>
            route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: [
                        { id: 'p_ttcsdl', code: 'TTCSDL', name: 'TTCSDL' },
                        { id: 'p_ddtl2', code: 'DDTL-2', name: 'Đồng Đăng - Trà Lĩnh 2' },
                    ],
                }),
            })
        );

        await page.route('**/api/sharedata/sharedatapacket/getsqldetail*', (route) =>
            route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        extract: { extractSql: 'SELECT TOP (@pageSize) 1 AS col', status: 1, fields: [] },
                        variables: { items: [{ code: 'pageSize', valueKind: 'Const', value: '100', dataType: 'Int', orderNo: 1 }] },
                        write: { packetCode: '101_commonData', writeSql: '', affectedTables: [], detectedTables: [], keyFields: [] },
                    },
                }),
            })
        );

        await page.route('**/api/sharedata/sharedatapacket/getpartnersqllist*', (route) =>
            route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: [
                        { partnerId: 'p_ttcsdl', partnerCode: 'TTCSDL', partnerName: 'TTCSDL', isCustom: false },
                        { partnerId: 'p_ddtl2', partnerCode: 'DDTL-2', partnerName: 'Đồng Đăng - Trà Lĩnh 2', isCustom: true },
                    ],
                }),
            })
        );

        await page.route('**/api/sharedata/sharedatapacket/getpartnerextractsql*', (route) =>
            route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        partnerId: 'p_ddtl2',
                        packetCode: '101_commonData',
                        isCustom: true,
                        customSql: 'SELECT TOP (@pageSize) 2 AS colCustom',
                        fields: [],
                    },
                }),
            })
        );

        await page.route('**/api/sharedata/sharedatapacket/getextractsql*', (route) =>
            route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: { extractSql: 'SELECT TOP (@pageSize) 1 AS col', status: 1 },
                }),
            })
        );

        await page.setViewportSize({ width: 1600, height: 950 });
        await page.goto('/#/sharedata/dataSource');

        // Chọn đối tác dùng chung TTCSDL trong bộ lọc đối tác bên trái
        const partnerFilter = page.locator('.packet-partner-filter .el-select');
        if (await partnerFilter.count() > 0) {
            await partnerFilter.click();
            const ttcsdlOption = page.locator('.el-select-dropdown__item', { hasText: 'TTCSDL' }).first();
            if (await ttcsdlOption.count() > 0) {
                await ttcsdlOption.click();
            }
        }

        const firstRow = page.locator('.packet-col .vxe-table--main-wrapper .vxe-body--row').first();
        await expect(firstRow).toBeVisible();
        await firstRow.click();

        // Kiểm tra ô chọn phạm vi: Mẫu chung đang được chọn
        const scopeSelect = page.locator('.partner-tab-jump');
        await expect(scopeSelect).toBeVisible();
        await expect(scopeSelect).toContainText('Mẫu chung');

        // Tiêu đề câu truy vấn là Mẫu chung, KHÔNG được hiện tiêu đề riêng của TTCSDL
        const editorTitle = page.locator('.sql-section .sql-editor-title');
        await expect(editorTitle).toBeVisible();
        await expect(editorTitle).toContainText(/mẫu chung/i);
        await expect(editorTitle).not.toContainText('TTCSDL');

        // Mở dropdown kiểm tra: DDTL-2 có mặt, TTCSDL KHÔNG có mặt (vì dùng chung)
        await scopeSelect.click();
        const ddlt2Opt = page.locator('.partner-tab-jump-popper:visible .el-select-dropdown__item', { hasText: 'DDTL-2' });
        const ttcsdlOpt = page.locator('.partner-tab-jump-popper:visible .el-select-dropdown__item', { hasText: 'TTCSDL' });
        await expect(ddlt2Opt).toBeVisible();
        await expect(ttcsdlOpt).toHaveCount(0);

        // Chuyển sang DDTL-2: Tiêu đề chuyển sang riêng của DDTL-2
        await ddlt2Opt.click();
        await expect(scopeSelect).toContainText('DDTL-2');
        await expect(editorTitle).toContainText('DDTL-2');

        // Quay lại Mẫu chung
        await selectPartnerScope(page, 'Mẫu chung');
        await expect(scopeSelect).toContainText('Mẫu chung');
        await expect(editorTitle).toContainText(/mẫu chung/i);

        // Không có bất kỳ lỗi JavaScript / pageerror nào
        expect(pageErrors).toEqual([]);
    });

    test('58. Khi đổi tab đối tác, vùng soạn thảo SQL và các khối nội dung được che hoàn toàn dưới nền trắng solid-loading-mask', async ({ page }) => {
        const pageErrors: string[] = [];
        page.on('pageerror', (err) => {
            if (!isSharedLayoutError(err.message)) pageErrors.push(err.message);
        });

        await page.route('**/api/sharedata/sharedatapacket/getsqldetail*', (route) =>
            route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        extract: { extractSql: 'SELECT TOP (@pageSize) 1 AS col', status: 1, fields: [] },
                        variables: { items: [{ code: 'pageSize', valueKind: 'Const', value: '100', dataType: 'Int', orderNo: 1 }] },
                        write: { packetCode: '101_commonData', writeSql: '', affectedTables: [], detectedTables: [], keyFields: [] },
                    },
                }),
            })
        );

        await page.route('**/api/sharedata/sharedatapacket/getpartnersqllist*', (route) =>
            route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: [
                        { partnerId: 'p_ddtl2', partnerCode: 'DDTL-2', partnerName: 'Đồng Đăng - Trà Lĩnh 2', isCustom: true },
                    ],
                }),
            })
        );

        // Đủ dài để lớp che còn hiện lúc kiểm, kể cả khi máy chạy test bận
        let partnerSqlDelay = 1500;
        await page.route('**/api/sharedata/sharedatapacket/getpartnerextractsql*', async (route) => {
            if (partnerSqlDelay > 0) await new Promise((r) => setTimeout(r, partnerSqlDelay));
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        partnerId: 'p_ddtl2',
                        packetCode: '101_commonData',
                        isCustom: true,
                        customSql: 'SELECT TOP (@pageSize) 2 AS colCustom',
                        fields: [{ key: 'colCustom', type: 'int', label: 'Cột riêng' }],
                    },
                }),
            });
        });

        await page.setViewportSize({ width: 1600, height: 950 });
        await page.goto('/#/sharedata/dataSource');

        const firstRow = page.locator('.packet-col .vxe-table--main-wrapper .vxe-body--row').first();
        await expect(firstRow).toBeVisible();
        await firstRow.click();

        const sendRegionsWrapper = page.locator('.send-regions-wrapper');
        await expect(sendRegionsWrapper).toBeVisible();

        // Chuyển sang đối tác DDTL-2 và kiểm tra trạng thái loading che phủ
        await selectPartnerScope(page, 'DDTL-2');

        // Khi đang nạp dữ liệu: wrapper phải có lớp is-scope-loading và mask che phủ
        await expect(sendRegionsWrapper).toHaveClass(/is-scope-loading/, { timeout: 1000 });
        const mask = sendRegionsWrapper.locator('.solid-loading-mask');
        await expect(mask).toBeVisible();

        // Kiểm tra phần tử con (chẳng hạn editor) bị ẩn bởi visibility: hidden
        const editorSection = sendRegionsWrapper.locator('.sql-section');
        if (await editorSection.count() > 0) {
            const isHidden = await editorSection.evaluate((el) => window.getComputedStyle(el).visibility === 'hidden');
            expect(isHidden).toBe(true);
        }

        // Chờ tải xong: is-scope-loading gỡ bỏ, nội dung hiển thị bình thường
        await expect(sendRegionsWrapper).not.toHaveClass(/is-scope-loading/, { timeout: 5000 });
        await expect(mask).toBeHidden({ timeout: 5000 });

        expect(pageErrors).toEqual([]);
    });

    test('59. Khi chọn gói hoặc chuyển tab: chỉ hiển thị đúng 1 loading mask nền trắng ngoài cùng, không hiển thị loading con bên trong các bảng hoặc mục con', async ({ page }) => {
        const pageErrors: string[] = [];
        page.on('pageerror', (err) => {
            if (!isSharedLayoutError(err.message)) pageErrors.push(err.message);
        });

        await page.route('**/api/sharedata/sharedatapacket/getsqldetail*', async (route) => {
            await new Promise((r) => setTimeout(r, 200));
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        extract: { extractSql: 'SELECT TOP (@pageSize) 1 AS col', status: 1, fields: [] },
                        variables: { items: [{ code: 'pageSize', valueKind: 'Const', value: '100', dataType: 'Int', orderNo: 1 }] },
                        write: { packetCode: '101_commonData', writeSql: '', affectedTables: [], detectedTables: [], keyFields: [] },
                    },
                }),
            });
        });

        await page.route('**/api/sharedata/sharedatapacket/getpartnersqllist*', async (route) => {
            await new Promise((r) => setTimeout(r, 200));
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: [
                        { partnerId: 'p_ddtl2', partnerCode: 'DDTL-2', partnerName: 'Đồng Đăng - Trà Lĩnh 2', isCustom: true },
                        { partnerId: 'p_ttcsdl', partnerCode: 'TTCSDL', partnerName: 'TTCSDL', isCustom: false },
                    ],
                }),
            });
        });

        await page.route('**/api/sharedata/sharedatapacket/getsqlvariables*', async (route) => {
            await new Promise((r) => setTimeout(r, 200));
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        items: [{ code: 'pageSize', valueKind: 'Const', value: '100', dataType: 'Int', savedValue: '100' }],
                    },
                }),
            });
        });

        await page.setViewportSize({ width: 1600, height: 950 });
        await page.goto('/#/sharedata/dataSource');

        const firstRow = page.locator('.packet-col .vxe-table--main-wrapper .vxe-body--row').first();
        await expect(firstRow).toBeVisible();
        await firstRow.click();

        // 1. Kiểm tra loading ngoài: .packet-details-wrapper hiển thị loading mask nền trắng
        const packetWrapper = page.locator('.packet-details-wrapper');
        await expect(packetWrapper).toBeVisible();

        // 2. Kiểm tra KHÔNG có bất kỳ loading con nào bên trong:
        // - Không có loading trên panel biến .sql-variable-panel
        // - Không có loading trên bảng tham số .svp-table
        // - Không có loading trên bảng trường dữ liệu .vxe-table (Vùng 2)
        // - Không có loading trên panel câu ghi .write-sql-panel
        const variablePanelMask = page.locator('.sql-variable-panel > .el-loading-mask');
        const svpTableMask = page.locator('.svp-table > .el-loading-mask');
        const writeSqlMask = page.locator('.write-sql-panel > .el-loading-mask');
        const fieldTableLoading = page.locator('.saved-fields-grid-wrapper .vxe-table--loading');

        expect(await variablePanelMask.count()).toBe(0);
        expect(await svpTableMask.count()).toBe(0);
        expect(await writeSqlMask.count()).toBe(0);
        expect(await fieldTableLoading.count()).toBe(0);

        // Chờ tải xong
        await expect(packetWrapper.locator('> .solid-loading-mask')).toBeHidden({ timeout: 5000 });

        // Sau khi tải xong, tất cả các mục con 1, 2, 3 đều hiển thị đầy đủ
        await expect(page.locator('.sql-section')).toBeVisible();
        await expect(page.locator('.saved-fields-grid-wrapper')).toBeVisible();
        await expect(page.locator('.sql-variable-panel')).toBeVisible();

        // Và vẫn không có loading con nào bên trong
        expect(await variablePanelMask.count()).toBe(0);
        expect(await svpTableMask.count()).toBe(0);
        expect(await writeSqlMask.count()).toBe(0);
        expect(await fieldTableLoading.count()).toBe(0);

        expect(pageErrors).toEqual([]);
    });

    /**
     * Description: Câu sai cấu trúc (__lastKey chưa ghép thời gian, còn @lastTime): dải Quy chuẩn mốc chưa đạt kèm cách sửa; cột Nguồn chỉ là chữ theo tên biến, ô ngày khóa,
     *              không có câu nhắc dạng trước đó; bấm Sửa câu ở dòng @lastKey thì đạt 3/3, dòng @lastTime tự bỏ, ô ngày chọn được.
     *              Bảng chọn ngày gọn (không quá 360 x 340 px), lịch không tràn khung.
     * Created date: 29/10/2026
     * Modified date: 04/11/2026
     */
    test('63. Câu sai cấu trúc: không có ô chọn nguồn, ô ngày khóa, bấm Sửa câu thì đạt quy chuẩn và chọn được ngày', async ({ page }) => {
        const twoColumnSql =
            'SELECT TOP (@pageSize) ID AS zoneId, UpdateTime AS __lastTime, ID AS __lastKey FROM TmsZone ' +
            "WHERE UpdateTime >= ISNULL(@lastTime, '17530101') AND (@lastTime IS NULL OR UpdateTime > @lastTime OR ID > @lastKey) ORDER BY UpdateTime ASC, ID ASC";

        await page.route('**/api/sharedata/sharedatapacket/getsqldetail*', (route) =>
            route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        extract: { extractSql: twoColumnSql, status: 1, fields: [{ key: 'zoneId', type: 'string', label: 'Mã khu vực', required: true }] },
                        variables: {
                            items: [
                                { code: 'pageSize', valueKind: 'Const', value: '100', dataType: 'Int', orderNo: 1 },
                                { code: 'lastTime', valueKind: 'Builtin', value: 'lastTime', dataType: 'DateTime', orderNo: 2 },
                                { code: 'lastKey', valueKind: 'Builtin', value: 'lastKey', dataType: 'String', orderNo: 3 },
                            ],
                        },
                        write: { packetCode: '101_commonData', writeSql: '', affectedTables: [], detectedTables: [], keyFields: [] },
                    },
                }),
            })
        );

        await page.route('**/api/sharedata/sharedatapacket/getpartnersqllist*', (route) =>
            route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ code: 200, result: [] }) })
        );

        await page.setViewportSize({ width: 1600, height: 950 });
        await page.goto('/#/sharedata/dataSource');

        const firstRow = page.locator('.packet-col .vxe-table--main-wrapper .vxe-body--row').first();
        await expect(firstRow).toBeVisible({ timeout: 30_000 });
        await firstRow.click();

        const panel = page.locator('.sql-variable-panel');
        const lead = panel.locator('.svp-check-lead');
        await expect(lead).toContainText('(0/3)', { timeout: 10_000 });
        await lead.hover();
        const popover = page.locator('.svp-popover-content');
        await expect(popover.locator('.svp-popover-item')).toHaveCount(3);
        // Mục chưa đạt chỉ rõ cách sửa, lấy đúng cột trong câu đang soạn
        await expect(popover.locator('.svp-fix-code').first()).toContainText("CONVERT(varchar(23), COALESCE(UpdateTime, CreateTime), 121) + '_' + ID AS __lastKey");
        await expect(popover.locator('.svp-fix-code').nth(1)).toContainText("WHERE CONVERT(varchar(23), COALESCE(UpdateTime, CreateTime), 121) + '_' + ID > @lastKey");
        await expect(popover.locator('.svp-fix-code').nth(2)).toContainText('ORDER BY __lastKey ASC');
        await expect(popover.locator('.svp-popover-item', { hasText: 'Có cột mốc __lastKey' })).toHaveClass(/is-fail/);
        await expect(popover.locator('.svp-popover-item', { hasText: 'Chỉ lọc mốc theo @lastKey' })).toHaveClass(/is-fail/);

        const table = panel.locator('.svp-table').first();
        await expect(panel.locator('.svp-inline-banner')).toHaveCount(0);

        const lastTimeRow = table.locator('tbody tr', { hasText: '@lastTime' });
        const lastKeyRow = table.locator('tbody tr', { hasText: '@lastKey' });
        await expect(lastTimeRow).toContainText('tự bỏ khi sửa câu');
        await expect(lastKeyRow).toContainText('Theo Mốc đang chạy');
        await expect(lastKeyRow.locator('.svp-source-col-tag')).toHaveCount(0);
        // Kiểm tra ô chọn Nguồn giá trị (.svp-source-select) của @lastKey hiển thị trên đúng 1 dòng, không bị rớt dòng
        const isSourceSingleLine = await lastKeyRow.locator('.svp-source-select .el-select__placeholder').evaluate((el) => {
            const style = window.getComputedStyle(el);
            return style.whiteSpace === 'nowrap' && el.getBoundingClientRect().height <= 28;
        });
        expect(isSourceSingleLine).toBe(true);
        await expect(table.locator('tbody tr', { hasText: '@pageSize' })).toContainText('Giá trị nhập');
        await expect(table.locator('.svp-source-select')).toHaveCount(3);
        await expect(lastKeyRow.locator('.cvi-picker input')).toBeDisabled();
        await expect(lastKeyRow.locator('.cvi-picker input')).toHaveAttribute('placeholder', /Cần sửa câu/);
        await expect(lastKeyRow.locator('.cvi-fix')).toHaveCount(0);
        await expect(panel).not.toContainText('không còn dùng');
        await expect(panel).not.toContainText('Gửi toàn bộ dữ liệu');

        // Trong popup chỉ có duy nhất 1 nút Sửa câu (không có Sửa tất cả + Sửa từng mục gây rối)
        await expect(popover.locator('button')).toHaveCount(1);
        await popover.locator('.svp-fix-all').click();
        await expect(lead).toContainText('(3/3)', { timeout: 10_000 });
        await expect(lastTimeRow).toHaveCount(0);
        await expect(lastKeyRow.locator('.cvi-prefix-label')).toHaveCount(0);
        await expect(lastKeyRow.locator('.cvi-picker')).not.toHaveClass(/is-all-data/);
        await expect(lastKeyRow.locator('.cvi-picker input')).toBeEnabled();
        await expect(lastKeyRow.locator('.cvi-picker input')).toHaveAttribute('placeholder', 'Đối tác chưa có mốc: gửi toàn bộ');
        const isValueSingleLine = await lastKeyRow.locator('.cvi-wrap').evaluate((el) => {
            const style = window.getComputedStyle(el);
            return style.flexWrap === 'nowrap' && el.getBoundingClientRect().height <= 32;
        });
        expect(isValueSingleLine).toBe(true);

        // Mở lịch chọn ngày: các mốc chọn nhanh bên trái (Đầu ngày hôm nay, Đầu ngày hôm qua...) nằm trọn trên 1 dòng, không rớt dòng
        await lastKeyRow.locator('.cvi-picker input').click();
        const shortcuts = page.locator('.cvi-date-popper:visible .el-picker-panel__shortcut');
        await expect(shortcuts.first()).toBeVisible();
        const allShortcutsSingleLine = await shortcuts.evaluateAll((els) =>
            els.every((el) => window.getComputedStyle(el).whiteSpace === 'nowrap' && el.getBoundingClientRect().height <= 32 && el.scrollWidth <= el.clientWidth + 1)
        );
        expect(allShortcutsSingleLine).toBe(true);
        const datePanel = page.locator('.cvi-date-popper:visible .el-date-picker');
        const datePanelBox = await datePanel.boundingBox();
        expect(datePanelBox!.width).toBeLessThanOrEqual(360);
        expect(datePanelBox!.height).toBeLessThanOrEqual(340);
        const calendarFits = await datePanel.locator('.el-picker-panel__content').evaluate((el) => el.scrollWidth <= el.clientWidth + 1);
        expect(calendarFits).toBe(true);
    });

    /**
     * Description: Câu đúng chuẩn, có thêm @lastTime là điều kiện riêng của người dùng: dòng @lastTime là Giá trị nhập, có ô nhập, đạt đủ 3/3 và ô ngày của @lastKey chọn được.
     * Created date: 03/11/2026
     */
    test('63b. Câu đúng chuẩn có thêm @lastTime riêng: là Giá trị nhập, đạt quy chuẩn', async ({ page }) => {
        const sql =
            "SELECT TOP (@pageSize) ID AS zoneId, CONVERT(varchar(23), COALESCE(UpdateTime, CreateTime), 121) + '_' + ID AS __lastKey FROM TmsZone " +
            "WHERE CONVERT(varchar(23), COALESCE(UpdateTime, CreateTime), 121) + '_' + ID > @lastKey AND UpdateTime >= @lastTime ORDER BY __lastKey ASC";
        const fulfill = (route: any, result: unknown) => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ code: 200, result }) });
        await page.route('**/api/sharedata/sharedatapacket/getsqldetail*', (route) =>
            fulfill(route, {
                extract: { extractSql: sql, status: 1, fields: [{ key: 'zoneId', type: 'string', label: 'Mã khu vực', required: true }] },
                variables: {
                    items: [
                        { code: 'pageSize', valueKind: 'Const', value: '100', dataType: 'Int', orderNo: 1 },
                        { code: 'lastTime', valueKind: 'Builtin', value: '', dataType: '', orderNo: 2 },
                        { code: 'lastKey', valueKind: 'Builtin', value: 'lastKey', dataType: 'String', orderNo: 3 },
                    ],
                },
                write: { packetCode: '101_commonData', writeSql: '', affectedTables: [], detectedTables: [], keyFields: [] },
            })
        );
        await page.route('**/api/sharedata/sharedatapacket/getpartnersqllist*', (route) => fulfill(route, []));

        await page.setViewportSize({ width: 1600, height: 950 });
        await page.goto('/#/sharedata/dataSource');
        const firstRow = page.locator('.packet-col .vxe-table--main-wrapper .vxe-body--row').first();
        await expect(firstRow).toBeVisible({ timeout: 30_000 });
        await firstRow.click();

        const panel = page.locator('.sql-variable-panel');
        const lead = panel.locator('.svp-check-lead');
        const table = panel.locator('.svp-table').first();
        const lastTimeRow = table.locator('tbody tr', { hasText: '@lastTime' });
        await expect(lead).toContainText('(3/3)', { timeout: 10_000 });
        await expect(lastTimeRow).toContainText('Giá trị nhập');
        await expect(lastTimeRow.locator('.svp-const-input input')).toBeVisible();
        await expect(table.locator('tbody tr', { hasText: '@lastKey' }).locator('.cvi-picker input')).toBeEnabled();

        // Khi câu SQL mẫu chung không còn @lastTime (chỉ có @pageSize và @lastKey) nhưng VariablesJson cũ vẫn còn lastTime: bảng tham số tự lọc bỏ @lastTime
        const sqlWithoutLastTime =
            "SELECT TOP (@pageSize) ID AS zoneId, CONVERT(varchar(23), COALESCE(UpdateTime, CreateTime), 121) + '_' + ID AS __lastKey FROM TmsZone " +
            "WHERE CONVERT(varchar(23), COALESCE(UpdateTime, CreateTime), 121) + '_' + ID > @lastKey ORDER BY __lastKey ASC";
        await page.route('**/api/sharedata/sharedatapacket/getsqldetail*', (route) =>
            fulfill(route, {
                extract: { extractSql: sqlWithoutLastTime, status: 1, fields: [{ key: 'zoneId', type: 'string', label: 'Mã khu vực', required: true }] },
                variables: {
                    items: [
                        { code: 'pageSize', valueKind: 'Const', value: '100', dataType: 'Int', orderNo: 1 },
                        { code: 'lastTime', valueKind: 'Builtin', value: 'lastTime', dataType: 'DateTime', orderNo: 2 },
                        { code: 'lastKey', valueKind: 'Builtin', value: 'lastKey', dataType: 'String', orderNo: 3 },
                    ],
                },
                write: { packetCode: '101_commonData', writeSql: '', affectedTables: [], detectedTables: [], keyFields: [] },
            })
        );
        await page.reload();
        await expect(firstRow).toBeVisible({ timeout: 30_000 });
        await firstRow.click();
        await expect(lead).toContainText('(3/3)', { timeout: 10_000 });
        await expect(table.locator('tbody tr', { hasText: '@lastTime' })).toHaveCount(0);
        await expect(table.locator('.svp-param-code')).toHaveText(['@pageSize', '@lastKey']);
    });

    /**
     * Description: Tab cấu hình riêng của đối tác: dữ liệu cũ để @lastKey là Giá trị nhập thì Quy chuẩn mốc còn 2/3 và cột Nguồn còn chọn được;
     *              bấm Sửa câu trong popup thì @lastKey trở lại Theo Mốc đang chạy, đạt đủ 3/3 và cột Nguồn khóa
     *              (đổi @lastKey sang Giá trị nhập làm câu nối đuôi gửi lại cùng dữ liệu mỗi lượt).
     * Created date: 06/11/2026
     * Modified date: 08/11/2026
     */
    test('63c. Cấu hình riêng: @lastKey là Giá trị nhập thì Sửa câu đưa về Theo Mốc đang chạy, đạt 3/3 và khóa cột Nguồn', async ({ page }) => {
        const key = "CONVERT(varchar(23), COALESCE(zs.UpdateTime, zs.CreateTime), 121) + '_' + zs.ID";
        const sql = `SELECT TOP (@pageSize) ${key} AS __lastKey, zs.ID AS zoneStatusId FROM TmsZoneStatus zs WHERE ${key} > @lastKey ORDER BY __lastKey ASC`;
        const items = [
            { code: 'pageSize', dataType: 'Int', valueKind: 'Const', value: '100', defaultValue: null, savedValue: null },
            { code: 'lastKey', dataType: 'String', valueKind: 'Const', value: '', defaultValue: null, savedValue: null },
        ];
        const fields = [{ key: 'zoneStatusId', type: 'string', label: 'Mã trạng thái', required: true }];
        const fulfill = (route: any, result: unknown) => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ code: 200, result }) });
        const pageErrors: string[] = [];
        page.on('pageerror', (err) => pageErrors.push(err.message));

        await page.route('**/api/sharedata/sharedatapacket/getsqldetail*', (route) =>
            fulfill(route, {
                extract: { extractSql: sql, status: 1, fields },
                variables: { items },
                write: { packetCode: '101_commonData', writeSql: '', affectedTables: [], detectedTables: [], keyFields: [] },
            })
        );
        await page.route('**/api/sharedata/sharedatapacket/getpartnersqllist*', (route) =>
            fulfill(route, [{ partnerId: 'p_pc08', partnerCode: 'PC08', partnerName: 'Phòng Cảnh sát giao thông', isCustom: true, updateTime: '2026-11-06T07:00:00Z' }])
        );
        await page.route('**/api/sharedata/sharedatapacket/getsqlvariables*', (route) => fulfill(route, { items }));
        await page.route('**/api/sharedata/sharedatapacket/getpartnerextractsql*', (route) =>
            fulfill(route, { isCustom: true, customSql: sql, standardSql: sql, fields, commonFields: fields })
        );
        await page.route('**/api/sharedata/sharedatasubscription/list*', (route) => fulfill(route, []));

        await page.setViewportSize({ width: 1600, height: 950 });
        await page.goto('/#/sharedata/dataSource');
        const firstRow = page.locator('.packet-col .vxe-table--main-wrapper .vxe-body--row').first();
        await expect(firstRow).toBeVisible({ timeout: 30_000 });
        await firstRow.click();
        await selectPartnerScope(page, 'PC08');

        const panel = page.locator('.sql-variable-panel');
        const lead = panel.locator('.svp-check-lead');
        const lastKeyRow = panel.locator('.svp-table').first().locator('tbody tr', { hasText: '@lastKey' });
        const pageSizeRow = panel.locator('.svp-table').first().locator('tbody tr', { hasText: '@pageSize' });
        const lastKeySource = lastKeyRow.locator('.svp-source-select .el-select__wrapper');
        await expect(lead).toContainText('(2/3)', { timeout: 10_000 });
        await expect(lastKeyRow).toContainText('Giá trị nhập');
        await expect(lastKeySource).not.toHaveClass(/is-disabled/);

        // Bấm Sửa câu trong popup Quy chuẩn mốc
        await lead.hover();
        const popover = page.locator('.svp-popover-content');
        await expect(popover.locator('.svp-popover-item', { hasText: 'Chỉ lọc mốc theo @lastKey' })).toHaveClass(/is-fail/);
        await popover.locator('.svp-fix-all').click();

        await expect(lead).toContainText('(3/3)', { timeout: 10_000 });
        await expect(lastKeyRow).toContainText('Theo Mốc đang chạy');
        await expect(lastKeySource).toHaveClass(/is-disabled/);

        // Icon nguồn: mốc (cam) khác giá trị nhập (xanh)
        const colorOf = (el: Element) => getComputedStyle(el).color;
        const keyColor = await lastKeyRow.locator('.svp-source-prefix').evaluate(colorOf);
        const constColor = await pageSizeRow.locator('.svp-source-prefix').evaluate(colorOf);
        expect(keyColor).not.toBe(constColor);
        expect(pageErrors).toEqual([]);
    });

    /**
     * Description: Câu gộp hai nguồn dạng một chuỗi, mỗi nguồn một nhóm mốc (biến @lastKey_<nhóm>, cột __lastKey_<nhóm>): dải Quy chuẩn mốc đạt đủ 3/3.
     * Created date: 30/10/2026
     * Modified date: 03/11/2026
     */
    test('64. Câu gộp nhiều nguồn có nhóm mốc: dải kiểm đạt', async ({ page }) => {
        const groupSql =
            'SELECT TOP (@pageSize) u.rowCode, u.__lastKey_pkt, u.__lastKey_toll FROM (' +
            "SELECT p.Code AS rowCode, CONVERT(varchar(23), p.UpdateTime, 121) + '_' + p.ID AS sortKey, CONVERT(varchar(23), p.UpdateTime, 121) + '_' + p.ID AS __lastKey_pkt, " +
            'CAST(NULL AS nvarchar(100)) AS __lastKey_toll FROM ShareDataPacket p ' +
            "WHERE CONVERT(varchar(23), p.UpdateTime, 121) + '_' + p.ID > @lastKey_pkt " +
            "UNION ALL SELECT t.SourceId, CONVERT(varchar(23), t.SourceWriteDate, 121) + '_' + t.ID, NULL, CONVERT(varchar(23), t.SourceWriteDate, 121) + '_' + t.ID FROM TollPriceList t " +
            "WHERE CONVERT(varchar(23), t.SourceWriteDate, 121) + '_' + t.ID > @lastKey_toll" +
            ') u ORDER BY u.sortKey ASC';
        const groupItems = [
            { code: 'pageSize', valueKind: 'Const', value: '100', dataType: 'Int', orderNo: 1 },
            { code: 'lastKey_pkt', valueKind: 'Builtin', value: 'lastKey', dataType: 'String', orderNo: 2 },
            { code: 'lastKey_toll', valueKind: 'Builtin', value: 'lastKey', dataType: 'String', orderNo: 3 },
        ];

        await page.route('**/api/sharedata/sharedatapacket/getsqldetail*', (route) =>
            route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        extract: { extractSql: groupSql, status: 1, fields: [{ key: 'rowCode', type: 'string', label: 'Mã dòng', required: true }] },
                        variables: { items: groupItems },
                        write: { packetCode: '101_commonData', writeSql: '', affectedTables: [], detectedTables: [], keyFields: [] },
                    },
                }),
            })
        );

        await page.route('**/api/sharedata/sharedatapacket/getpartnersqllist*', (route) =>
            route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: [{ partnerId: 'p_grp', partnerCode: 'GRP', partnerName: 'Đối tác nhiều nguồn', isCustom: false }],
                }),
            })
        );

        await page.setViewportSize({ width: 1600, height: 950 });
        await page.goto('/#/sharedata/dataSource');

        const firstRow = page.locator('.packet-col .vxe-table--main-wrapper .vxe-body--row').first();
        await expect(firstRow).toBeVisible({ timeout: 30_000 });
        await firstRow.click();

        await expect(page.locator('.sql-variable-panel .svp-check-lead')).toContainText('(3/3)', { timeout: 10_000 });
    });

    /**
     * Description: Bấm Lưu & phân tích ở gói tin này rồi chuyển sang gói tin khác: API lưu của gói cũ hoàn thành không ghi đè dữ liệu và không làm vô hiệu hóa nút Lưu của gói mới.
     * Created date: 06/11/2026
     */
    test('65. Chuyển gói tin trong khi đang lưu SQL: API gói cũ không ghi đè và không khóa nút Lưu gói mới', async ({ page }) => {
        await page.route('**/api/sharedata/sharedatapacket/page*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        records: [
                            { id: 'PKT_01', code: '101_commonData', name: 'Gói 101 - Dữ liệu dùng chung', status: 1 },
                            { id: 'PKT_02', code: '102_trafficData', name: 'Gói 102 - Dữ liệu giao thông', status: 1 },
                        ],
                        total: 2,
                    },
                }),
            });
        });

        await page.route('**/api/sharedata/sharedatapacket/getsqldetail*', async (route) => {
            const url = new URL(route.request().url());
            const packetCode = url.searchParams.get('PacketCode') || '';

            if (packetCode === '101_commonData') {
                await route.fulfill({
                    status: 200,
                    contentType: 'application/json',
                    body: JSON.stringify({
                        code: 200,
                        result: {
                            extract: {
                                id: 'EXT_101',
                                packetCode: '101_commonData',
                                extractSql: 'SELECT TOP 100 s.Speed AS speed FROM TmsSpeed s',
                                status: 1,
                                fields: [{ key: 'speed', type: 'number', label: 'Tốc độ', required: true }],
                            },
                            variables: { packetCode: '101_commonData', items: [] },
                            write: { packetCode: '101_commonData', writeSql: '', affectedTables: [], detectedTables: [], keyFields: [] },
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
                            extract: {
                                id: 'EXT_102',
                                packetCode: '102_trafficData',
                                extractSql: 'SELECT TOP 50 v.PlateNumber AS plateNumber FROM TmsVehicle v',
                                status: 1,
                                fields: [],
                            },
                            variables: { packetCode: '102_trafficData', items: [] },
                            write: { packetCode: '102_trafficData', writeSql: '', affectedTables: [], detectedTables: [], keyFields: [] },
                        },
                    }),
                });
            }
        });

        await page.route('**/api/sharedata/sharedatapacket/getpartnersqllist*', async (route) => {
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({ code: 200, result: [] }),
            });
        });

        let resolveSave: (() => void) | null = null;
        const saveHold = new Promise<void>((resolve) => {
            resolveSave = resolve;
        });

        await page.route('**/api/sharedata/sharedatapacket/updateextractsql*', async (route) => {
            await saveHold;
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        status: 1,
                        fields: [{ key: 'speed', type: 'number', label: 'Tốc độ', required: true }],
                    },
                }),
            });
        });

        await page.setViewportSize({ width: 1600, height: 950 });
        await page.goto('/#/sharedata/dataSource');

        const rows = page.locator('.packet-col .vxe-table--main-wrapper .vxe-body--row');
        await expect(rows).toHaveCount(2, { timeout: 30_000 });

        // 1. Chọn gói 101
        await rows.nth(0).click();
        const saveBtn = page.locator('.sql-toolbar-right button').filter({ hasText: 'Lưu & phân tích' }).first();

        // 2. Thay đổi câu SQL của gói 101 để kích hoạt trạng thái sửa (isDirty)
        await page.locator('.monaco-editor').first().click();
        await page.keyboard.press('End');
        await page.keyboard.type(' -- edit 101');
        await page.waitForTimeout(500);

        await expect(saveBtn).toBeEnabled({ timeout: 5000 });

        // 3. Bấm Lưu & phân tích cho gói 101 (API updateextractsql đang bị giữ bởi saveHold)
        await saveBtn.click();

        // 4. Lập tức click sang gói 102
        await rows.nth(1).click();
        const confirmBox = page.locator('.lost-keys-confirm-box');
        if (await confirmBox.isVisible({ timeout: 2000 }).catch(() => false)) {
            await confirmBox.locator('.el-button--primary').click();
        }

        // Chờ gói 102 nạp xong: gói 102 có câu SQL nhưng chưa có trường nào (fields: []) nên nút Lưu & phân tích phải ENABLED
        await expect(saveBtn).toBeEnabled({ timeout: 10_000 });

        // 5. Cho API lưu của gói 101 hoàn tất phản hồi
        resolveSave?.();
        await page.waitForTimeout(600);

        // 6. Kiểm tra:
        // - Nút Lưu & phân tích ở gói 102 VẪN PHẢI ENABLED (không bị disable bởi response gói 101)
        await expect(saveBtn).toBeEnabled({ timeout: 5000 });
        await expect(saveBtn).not.toHaveClass(/is-loading/);

        // - Danh sách trường của gói 102 không bị ghi đè trường 'speed' từ gói 101
        const speedInPane = page.locator('.saved-fields-pane').filter({ hasText: 'speed' });
        await expect(speedInPane).toHaveCount(0);
    });

    /**
     * Description: Khi tạo mới gói tin trên màn Cấu hình gói tin: bảng gói tin đồng bộ xong dữ liệu mới trước khi đóng hộp thoại
     *              và hiện thông báo thành công, đồng thời không quét lại trạng thái sử dụng (checkPacketUsage) của các gói tin cũ
     *              hay chặn hiển thị dòng mới, giúp gói mới xuất hiện ngay lập tức khi báo thành công.
     * Created date: 09/11/2026
     */
    test('66. Tạo mới gói tin: hiển thị ngay trên bảng cùng lúc báo thành công, không bị trễ 1-2s do quét lại trạng thái gói cũ', async ({ page }) => {
        const packets = [
            { id: 'PKT_01', code: '101_commonData', name: 'Gói 101 - Dữ liệu giao thông chung', orderNo: 101, status: 1, createTime: '2026-10-01T00:00:00Z' },
            { id: 'PKT_02', code: '102_cctvData', name: 'Gói 102 - Dữ liệu camera giám sát', orderNo: 102, status: 1, createTime: '2026-10-01T00:01:00Z' },
        ];
        let isAdded = false;
        let mappingCallsAfterAdd = 0;

        await page.route('**/api/cfgsystem/sysconfigtype/alldatalist*', (route) =>
            route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({ code: 200, result: { status_type: [{ code: '1', name: 'Bật' }, { code: '0', name: 'Tắt' }] } }),
            })
        );

        await page.route('**/api/sharedata/sharedatapacket/page*', async (route) => {
            if (isAdded) {
                await new Promise((r) => setTimeout(r, 150));
            }
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        records: packets,
                        total: packets.length,
                    },
                }),
            });
        });

        await page.route('**/api/sharedata/sharedatamapping/page*', async (route) => {
            if (isAdded) {
                mappingCallsAfterAdd++;
                await new Promise((r) => setTimeout(r, 1500));
            }
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({ code: 200, result: { records: [], total: 0 } }),
            });
        });

        await page.route('**/api/sharedata/sharedatapacket/add*', async (route) => {
            const body = route.request().postDataJSON();
            isAdded = true;
            packets.push({
                id: 'PKT_NEW',
                code: body.code,
                name: body.name,
                orderNo: body.orderNo ?? 101,
                status: body.status ?? 1,
                createTime: '2026-11-09T04:00:00Z',
            });
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({ code: 200, result: 'PKT_NEW' }),
            });
        });

        await page.setViewportSize({ width: 1600, height: 900 });
        await page.goto('/#/sharedata/dataSource');

        const rows = page.locator('.packet-col .vxe-table--main-wrapper .vxe-body--row');
        await expect(rows).toHaveCount(2, { timeout: 30_000 });

        // Mở hộp thoại Thêm mới gói tin
        const btnAddPacket = page.locator('.packet-col .el-button').filter({ hasText: /Thêm|Tạo/i }).first();
        const dialog = page.locator('.edit-packet-container .el-dialog');
        await expect(async () => {
            if (!(await dialog.isVisible())) await btnAddPacket.click();
            await expect(dialog).toBeVisible({ timeout: 3000 });
        }).toPass({ timeout: 20_000 });

        // Nhập Mã = test, Tên = test rồi bấm Xác nhận
        const inputs = dialog.locator('.el-input input');
        await inputs.nth(0).fill('test');
        await inputs.nth(1).fill('test');
        await dialog.locator('.el-dialog__footer .el-button--primary').click();

        // Ngay khoảnh khắc thông báo thành công xuất hiện, dòng gói mới 'test' phải ĐÃ có mặt trên bảng (không bị trễ 1-2s)
        // Lọc theo chữ: thông báo kết nối NATS cũng là el-message--success
        const successMsg = page.locator('.el-message--success').filter({ hasText: 'Thực hiện thành công' }).last();
        await expect(successMsg).toBeVisible({ timeout: 10_000 });

        const rowCountAtToast = await rows.count();
        expect(rowCountAtToast).toBe(3);
        await expect(rows.nth(2)).toContainText('test');
        // Chỉ kiểm tra ngầm đúng 1 gói mới, không quét lại 2 gói cũ đã biết trạng thái
        expect(mappingCallsAfterAdd).toBeLessThanOrEqual(1);
    });

    /**
     * Description: Dữ liệu gói nhiều đối tác (tái hiện kiểm tải 52 đối tác trên staging), tự sinh mỗi lần chạy qua API giả lập, mã không trùng giữa các lần chạy:
     *              một gói, 30 đối tác, 15 đối tác có cấu hình riêng xen kẽ đối tác dùng chung; câu riêng của mỗi đối tác mang mã đối tác đó.
     * Created date: 11/11/2026
     */
    const seedManyPartnerPacket = async (page: Page) => {
        const runId = Date.now().toString(36).toUpperCase();
        const packetCode = `KT_${runId}`;
        const standardSql = 'SELECT TOP 100 z.Code AS zoneCode FROM TmsZone z';
        const fields = [{ key: 'zoneCode', type: 'string', label: 'Mã khu vực' }];
        const partners = Array.from({ length: 30 }, (_, i) => {
            const no = String(i + 1).padStart(3, '0');
            return {
                partnerId: `P_${runId}_${no}`,
                partnerCode: `KT${no}-${runId.slice(-4)}`,
                partnerName: `Kiểm tải ${no}`,
                isCustom: i % 2 === 1,
                updateTime: new Date(Date.UTC(2026, 10, 11, 8, i)).toISOString(),
            };
        });
        const json = (result: unknown) => ({ status: 200, contentType: 'application/json', body: JSON.stringify({ code: 200, result }) });

        await page.route('**/api/sharedata/sharedatapacket/page*', (route) =>
            route.fulfill(
                json({
                    records: [
                        {
                            id: `PKT_${runId}`,
                            code: packetCode,
                            name: `Gói kiểm tải ${runId}`,
                            status: 1,
                            partnerCount: partners.length,
                            partnerNames: partners.map((p) => p.partnerCode),
                        },
                    ],
                    total: 1,
                })
            )
        );
        await page.route('**/api/sharedata/sharedatapacket/getsqldetail*', (route) =>
            route.fulfill(
                json({
                    extract: { extractSql: standardSql, status: 1, fields },
                    variables: { items: [] },
                    write: { packetCode, writeSql: '', affectedTables: [], detectedTables: [], keyFields: [] },
                })
            )
        );
        await page.route('**/api/sharedata/sharedatapacket/getpartnersqllist*', (route) => route.fulfill(json(partners)));
        await page.route('**/api/sharedata/sharedatapacket/getpartnerextractsql*', (route) => {
            const partnerId = new URL(route.request().url()).searchParams.get('PartnerId');
            const partner = partners.find((p) => p.partnerId === partnerId);
            const customSql = `SELECT TOP 100 z.Code AS zoneCode, N'${partner?.partnerCode}' AS partnerTag FROM TmsZone z`;
            return route.fulfill(json({ isCustom: true, customSql, standardSql, fields, commonFields: fields }));
        });

        return { partners, customPartners: partners.filter((p) => p.isCustom) };
    };

    /**
     * Description: Dòng "Đối tác dùng gói" của gói nhiều đối tác không đẩy Vùng 1, 2, 3 xuống: mặc định chỉ hiện tóm tắt
     *              và 10 thẻ đầu (cấu hình riêng xếp trước), "Xem tất cả" hiện đủ, "Thu gọn" về như cũ.
     * Created date: 11/11/2026
     */
    test('79a. Gói nhiều đối tác: dòng Đối tác dùng gói mặc định chỉ hiện tóm tắt và 10 thẻ (cấu hình riêng trước), Xem tất cả hiện đủ, Thu gọn trở lại', async ({ page }) => {
        const pageErrors: string[] = [];
        page.on('pageerror', (err) => {
            if (!isSharedLayoutError(err.message)) pageErrors.push(err.message);
        });

        // BƯỚC 1: Setup — một gói 30 đối tác, 15 đối tác có cấu hình riêng, cả 30 đối tác đều đang bật đăng ký gửi
        const { partners, customPartners } = await seedManyPartnerPacket(page);
        const sharedCount = partners.length - customPartners.length;
        await page.route('**/api/sharedata/sharedatasubscription/list*', (route) =>
            route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: partners.map((p, idx) => ({
                        id: `SUB_${idx + 1}`,
                        partnerId: p.partnerId,
                        partnerCode: p.partnerCode,
                        partnerName: p.partnerName,
                        direction: 0,
                        status: 1,
                    })),
                }),
            })
        );

        // BƯỚC 2: Navigate — mở màn Cấu hình gói tin, chọn gói
        await page.setViewportSize({ width: 1600, height: 900 });
        await page.goto('/#/sharedata/dataSource');
        const firstRow = page.locator('.packet-col .vxe-table--main-wrapper .vxe-body--row').first();
        await expect(firstRow).toBeVisible({ timeout: 30_000 });
        await firstRow.click();

        const usage = page.locator('.packet-partner-usage');
        const tags = usage.locator('.el-tag');
        const toggle = usage.locator('.partner-usage-toggle');

        // BƯỚC 4: Mặc định — tóm tắt đủ số, chỉ 10 thẻ và đều là đối tác cấu hình riêng (xếp trước), nút Xem tất cả kèm tổng số đối tác
        await expect(usage.locator('.usage-summary')).toHaveText(`${customPartners.length} cấu hình riêng · ${sharedCount} dùng chung`, { timeout: 15_000 });
        await expect(tags).toHaveCount(10);
        await expect(tags.filter({ hasText: 'Dùng chung' })).toHaveCount(0);
        await expect(tags.first()).toContainText(customPartners[0].partnerCode);
        await expect(toggle).toHaveText(`Xem tất cả (${partners.length})`);

        // BƯỚC 3: Bấm Xem tất cả — hiện đủ 30 thẻ, nút đổi thành Thu gọn
        await toggle.click();
        await expect(tags).toHaveCount(partners.length);
        await expect(tags.filter({ hasText: 'Dùng chung' })).toHaveCount(sharedCount);
        await expect(toggle).toHaveText('Thu gọn');

        // Bấm Thu gọn — về lại 10 thẻ như lúc đầu
        await toggle.click();
        await expect(tags).toHaveCount(10);
        await expect(toggle).toHaveText(`Xem tất cả (${partners.length})`);

        // Hộp cảnh báo khoá câu SQL (khi nhiều đối tác bật đăng ký): mặc định thu gọn 5 đối tác đầu + Xem tất cả (30), bấm mở rộng hiện đủ 30 đối tác rồi Thu gọn lại
        const lockAlert = page.locator('.send-direction-container .sql-lock-alert');
        const lockToggle = lockAlert.locator('.sql-lock-toggle');
        const lastPartnerName = partners[partners.length - 1].partnerName;
        await expect(lockAlert).toBeVisible();
        await expect(lockAlert).toContainText(partners[0].partnerName);
        await expect(lockAlert).toContainText(partners[4].partnerName);
        await expect(lockAlert).not.toContainText(lastPartnerName);
        await expect(lockToggle).toHaveText(`Xem tất cả (${partners.length})`);

        await lockToggle.click();
        await expect(lockAlert).toContainText(lastPartnerName);
        await expect(lockToggle).toHaveText('Thu gọn');

        await lockToggle.click();
        await expect(lockAlert).not.toContainText(lastPartnerName);
        await expect(lockToggle).toHaveText(`Xem tất cả (${partners.length})`);

        // Tooltip cột Đối tác của bảng Gói tin: có min-width và giới hạn bề rộng theo tỷ lệ màn hình (<= 28vw), không tràn ngang toàn màn hình
        const partnerCountTag = firstRow.locator('.partner-count-tag');
        await partnerCountTag.hover();
        const partnerTooltip = page.locator('.el-popper.partner-count-tooltip');
        await expect(partnerTooltip).toBeVisible();
        const tooltipBox = (await partnerTooltip.boundingBox())!;
        expect(tooltipBox.width).toBeGreaterThanOrEqual(160);
        expect(tooltipBox.width).toBeLessThanOrEqual(Math.ceil(1600 * 0.28) + 2);
        expect(pageErrors).toEqual([]);
    });

    /**
     * Description: Gói nhiều đối tác có cấu hình riêng chọn phạm vi SQL bằng ô chọn "Chọn đối tác:" (Mẫu chung + từng đối tác), ô hẹp 180px,
     *              rê chuột hiện tooltip tên đầy đủ; tìm và chọn được đối tác cuối, mở đúng câu riêng; đang sửa dở thì hỏi, bấm Hủy thì ở lại.
     *              Ô chọn, nút Tạo cấu hình riêng và nút Lưu & phân tích nằm trọn trong khung nhìn.
     * Created date: 11/11/2026
     */
    test('79b. Gói nhiều đối tác: ô Chọn đối tác có nhãn, hẹp, tooltip tên đầy đủ, chọn được đối tác cuối và vẫn hỏi khi đang sửa dở', async ({ page }) => {
        const pageErrors: string[] = [];
        page.on('pageerror', (err) => {
            if (!isSharedLayoutError(err.message)) pageErrors.push(err.message);
        });

        // BƯỚC 1: Setup — một gói 30 đối tác, 15 đối tác có cấu hình riêng
        const { customPartners } = await seedManyPartnerPacket(page);
        const firstCustom = customPartners[0];
        const lastCustom = customPartners[customPartners.length - 1];

        // BƯỚC 2: Navigate — mở màn Cấu hình gói tin, chọn gói
        await page.setViewportSize({ width: 1600, height: 900 });
        await page.goto('/#/sharedata/dataSource');
        const firstRow = page.locator('.packet-col .vxe-table--main-wrapper .vxe-body--row').first();
        await expect(firstRow).toBeVisible({ timeout: 30_000 });
        await firstRow.click();

        const jumpSelect = page.locator('.partner-tab-jump');
        const jumpOption = (text: string) => page.locator('.partner-tab-jump-popper .el-select-dropdown__item', { hasText: text });
        const toolbarCode = page.locator('.sql-toolbar-left .partner-toolbar-code');
        const editorLines = page.locator('.sql-editor-section .sql-monaco-editor .view-lines');
        const addPartnerBtn = page.locator('.add-partner-tab-btn');

        // BƯỚC 4: Ô chọn và các nút nằm trọn trong khung nhìn
        await expect(jumpSelect).toBeVisible({ timeout: 15_000 });
        await expect(jumpSelect).toBeInViewport({ ratio: 1 });
        await expect(addPartnerBtn).toBeInViewport({ ratio: 1 });
        await expect(page.locator('.sql-toolbar-right button').filter({ hasText: 'Lưu & phân tích' })).toBeInViewport({ ratio: 1 });
        const jumpBox = (await jumpSelect.boundingBox())!;
        expect(Math.round(jumpBox.width)).toBe(180);

        // BƯỚC 3: Mở ô chọn — có Mẫu chung và đủ đối tác cấu hình riêng; gõ tìm rồi chọn đối tác cuối
        await jumpSelect.click();
        await expect(page.locator('.partner-tab-jump-popper .el-select-dropdown__item')).toHaveCount(customPartners.length + 1);
        await page.keyboard.type(lastCustom.partnerCode);
        await jumpOption(lastCustom.partnerCode).click();

        // BƯỚC 4: Mở đúng câu riêng của đối tác cuối; rê chuột ô chọn hiện tooltip tên đầy đủ
        await expect(toolbarCode).toHaveText(lastCustom.partnerCode);
        await expect(editorLines).toContainText(lastCustom.partnerCode, { timeout: 10_000 });
        await jumpSelect.hover();
        const scopeTooltip = page.locator('.el-popper.is-dark', { hasText: lastCustom.partnerCode });
        await expect(scopeTooltip).toBeVisible({ timeout: 3000 });
        await expect(scopeTooltip).toContainText(lastCustom.partnerName);

        // Đang sửa dở mà chọn đối tác khác thì hỏi, bấm Hủy thì ở lại đối tác cũ
        await page.locator('.sql-editor-section .monaco-editor').first().click();
        await page.keyboard.press('End');
        await page.keyboard.type(' -- test');
        await jumpSelect.click();
        await page.keyboard.type(firstCustom.partnerCode);
        await jumpOption(firstCustom.partnerCode).click();
        const leaveConfirmBox = page.locator('.lost-keys-confirm-box');
        await expect(leaveConfirmBox).toBeVisible({ timeout: 5000 });
        await leaveConfirmBox.locator('.el-message-box__btns button').first().click();
        await expect(leaveConfirmBox).toBeHidden({ timeout: 3000 });
        await expect(toolbarCode).toHaveText(lastCustom.partnerCode);

        // Tab Nhận về dùng cùng kiểu ô chọn: rộng 180px, rê chuột hiện tooltip tên phạm vi đang chọn
        await page.locator('.direction-tabs .el-tabs__item').nth(1).click();
        const writeSelect = page.locator('.wsp-scope-select');
        await expect(writeSelect).toBeVisible({ timeout: 10_000 });
        await expect(writeSelect).toBeInViewport({ ratio: 1 });
        const writeSelectBox = (await writeSelect.boundingBox())!;
        expect(Math.round(writeSelectBox.width)).toBe(180);
        await writeSelect.hover();
        await expect(page.locator('.el-popper.is-dark', { hasText: 'Mẫu chung' })).toBeVisible({ timeout: 3000 });
        expect(pageErrors).toEqual([]);
    });

    test('80. Bảng Gói tin trang 50 dòng cuộn tới đáy thấy đủ dòng 50 ở cả cột cố định STT và Hành động', async ({ page }) => {
        test.slow();
        const ok = (result: unknown) => ({ status: 200, contentType: 'application/json', body: JSON.stringify({ code: 200, result }) });
        const records = Array.from({ length: 50 }, (_, i) => ({
            id: `PKT_${i + 1}`, code: `${101 + i}_commonData`, name: `Dữ liệu giao thông tuyến cao tốc Hữu Nghị - Chi Lăng gói ${i + 1}`, status: 1,
            partnerCount: 3, writeSqlCount: 2, createUserName: 'Quản trị hệ thống', createTime: '2026-11-13T14:58:17', updateUserName: 'Quản trị hệ thống', updateTime: '2026-11-13T14:58:17',
        }));
        await page.route('**/api/sharedata/sharedatapacket/page*', (r) => r.fulfill(ok({ records, total: 120 })));
        await page.setViewportSize({ width: 1024, height: 620 });
        await page.goto('/#/sharedata/dataSource');

        const body = page.locator('.packet-col .vxe-table--main-wrapper .vxe-table--body-wrapper').first();
        await expect(body.locator('tr.vxe-body--row')).toHaveCount(50, { timeout: 30_000 });
        const box = (await body.boundingBox())!;
        await page.mouse.move(box.x + box.width / 2, box.y + box.height / 2);
        for (let i = 0; i < 40; i++) await page.mouse.wheel(0, 400);
        await page.waitForTimeout(500);

        /* Check every body pane: main, fixed-left (STT) and fixed-right (Hành động). */
        const panes = await page.evaluate(() =>
            [...document.querySelectorAll('.packet-col .vxe-table--body-wrapper')].map((el) => {
                const b = el as HTMLElement;
                const rows = b.querySelectorAll('tr.vxe-body--row');
                const last = rows[rows.length - 1].getBoundingClientRect();
                /* Visible area = intersection of the pane with every clipping ancestor. */
                let bottom = b.getBoundingClientRect().bottom;
                for (let p = b.parentElement; p; p = p.parentElement) {
                    if (getComputedStyle(p).overflowY !== 'visible') bottom = Math.min(bottom, p.getBoundingClientRect().bottom);
                }
                return { pane: b.className, hiddenPx: Math.round(last.bottom - bottom) };
            })
        );
        expect(panes.some((p) => p.pane.includes('fixed-left'))).toBe(true);
        for (const p of panes) expect(p.hiddenPx, `dòng 50 bị che ở ${JSON.stringify(p)}`).toBeLessThanOrEqual(1);
    });

    test('81. Căn lề cột bảng Gói tin: tiêu đề giữa; Tên gói, Mã gói căn trái; STT, số đối tác, số câu ghi, thông tin, hành động căn giữa', async ({ page }) => {
        const ok = (result: unknown) => ({ status: 200, contentType: 'application/json', body: JSON.stringify({ code: 200, result }) });
        const records = [
            { id: 'PKT_1', code: 'KT038_110WPDATA_IN', name: 'KT038 - 110_wpData - Nhận về', status: 1, partnerCount: 3, writeSqlCount: 2 },
            { id: 'PKT_2', code: 'P2', name: 'Ngắn', status: 0, partnerCount: 0, writeSqlCount: 0 },
        ];
        await page.route('**/api/sharedata/sharedatapacket/page*', (r) => r.fulfill(ok({ records, total: 2 })));
        await page.setViewportSize({ width: 1920, height: 900 });
        await page.goto('/#/sharedata/dataSource');
        await expect(page.locator('.packet-col .vxe-table--main-wrapper tr.vxe-body--row')).toHaveCount(2, { timeout: 30_000 });

        const actual = await readVxeColumnAligns(page, '.packet-col .vxe-grid');
        expect(actual).toEqual(expectedColumnAligns(['Tên gói tin', 'Mã'], ['STT', 'Đối tác', 'Gửi / Nhận', 'Thông tin', 'Hành động']));
    });
});
