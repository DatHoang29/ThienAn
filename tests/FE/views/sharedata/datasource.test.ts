import { test, expect } from '@playwright/test';
import { mockAuth } from '../../setup';

/**
 * Test E2E cho phân hệ ShareData — Quản lý Gói tin & Trường gói tin (dataSource/index.vue):
 * - Kéo dãn cột, kéo thanh chia 2 pane, cột Hành động cố định bên phải, bảng/phân trang co giãn theo chiều cao.
 * - Hộp thoại form/confirm khi bấm ra ngoài; form thêm gói.
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

        const fieldResizers = fieldGrid.locator('.vxe-table--header-wrapper .vxe-header--column .vxe-resizable');
        await expect(fieldResizers.first()).toBeAttached();
        const fieldResizerCount = await fieldResizers.count();
        expect(fieldResizerCount).toBeGreaterThan(0);

        // Kéo dãn cột trên bảng Trường gói tin (chờ layout ổn định: bảng nằm dưới phần SQL nên chỉ cần vị trí không còn đổi và nằm trong màn hình)
        const targetFieldCol = fieldGrid.locator('.vxe-table--header-wrapper .vxe-header--column:has(.vxe-resizable):visible').first();
        let lastFieldY = -1;
        await expect(async () => {
            const box = await targetFieldCol.boundingBox();
            expect(box).not.toBeNull();
            expect(box!.y).toBeLessThan(page.viewportSize()!.height - 50);
            const prev = lastFieldY;
            lastFieldY = box!.y;
            expect(Math.abs(box!.y - prev)).toBeLessThan(1);
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
        await expect(packetTable).toBeVisible({ timeout: 15_000 });

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

    /**
     * Description: Luồng khóa nối đuôi @lastKey (Adaptive UI):
     *              - Tab Mẫu chung chỉ hiện 3 cột tham số (ẩn Mốc đang chạy), hiển thị badge tự động tịnh tiến + tag khớp __lastKey + ô Mốc bắt đầu;
     *              - Mở ngăn kéo xem mốc đang chạy của các đối tác, đặt lại mốc riêng cho 1 đối tác qua API resetlastsend;
     *              - Chuyển sang tab Đối tác riêng: đóng ngăn kéo, hiện cột Mốc đang chạy trực tiếp trên bảng tham số.
     * Created date: 27/10/2026
     */
    test('51. Tham số khóa nối đuôi @lastKey: giao diện thích ứng Mẫu chung vs Đối tác, ngăn kéo xem và đặt lại mốc chạy theo đối tác', async ({ page }) => {
        const compositeSql =
            "SELECT TOP (@pageSize) Code AS zoneCode, CONVERT(varchar(23), UpdateTime, 121) + '_' + ID AS __lastKey " +
            "FROM TmsZone WHERE (CONVERT(varchar(23), UpdateTime, 121) + '_' + ID) > @lastKey ORDER BY __lastKey ASC";
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

        let partnerACursor: string | null = '2026-10-27 10:05:00.000_Z02';
        const resetPayloads: Array<{ packetCode?: string; partnerId?: string }> = [];

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
                savedValue: it.code === 'lastKey' && partnerId === 'p_a' ? partnerACursor : null,
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

        await page.route('**/api/sharedata/sharedatapacket/resetlastsend*', (route) => {
            const body = route.request().postDataJSON() as { packetCode?: string; partnerId?: string };
            resetPayloads.push(body);
            if (body?.partnerId === 'p_a') partnerACursor = null;
            return route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({ code: 200, result: true }),
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
        await expect(mainTable.locator('.svp-key-input input')).toHaveValue('2026-10-27 09:00:00.000_');

        // 2. Chuyển sang tab Đối tác riêng CSGT: bảng tham số hiện đủ 5 cột (gồm So với mẫu + Mốc đang chạy)
        const partnerTab = page.locator('.sql-partner-tabs .el-tabs__item').filter({ hasText: 'CSGT' }).first();
        await expect(partnerTab).toBeVisible();
        await partnerTab.click();

        await expect(mainTable.locator('thead th')).toHaveCount(5);
        await expect(mainTable.locator('.svp-saved-code')).toHaveText('2026-10-27 10:05:00.000_Z02');

        // 3. Đặt lại mốc của CSGT (p_a)
        await mainTable.locator('.svp-reset-btn').click();
        const confirmBtn = page.locator('.el-message-box__btns .el-button--primary');
        await expect(confirmBtn).toBeVisible();
        await confirmBtn.click();

        await expect.poll(() => resetPayloads.length).toBe(1);
        expect(resetPayloads[0]).toEqual({ packetCode: '101_commonData', partnerId: 'p_a' });
        await expect(mainTable.locator('.svp-saved-code')).toHaveCount(0);
    });

    /**
     * Description: Khi bấm vào gói tin trên bảng bên trái (xGridPacket):
     *              - Bảng chi tiết bên phải nạp thành công, không bị lỗi cú pháp i18n (SyntaxError: Invalid linked format) hay crash trắng màn hình;
     *              - Khối tham số SQL hiển thị đầy đủ, nút Hướng dẫn thiết lập mở popover hiển thị 4 bước mà không bị lỗi.
     * Created date: 28/10/2026
     */
    test('52. Bấm vào gói tin bên table trái: không bị crash trắng màn hình bên phải và popover hướng dẫn thiết lập mở bình thường', async ({ page }) => {
        const pageErrors: string[] = [];
        page.on('pageerror', (err) => pageErrors.push(err.message));

        const compositeSql =
            "SELECT TOP (@pageSize) Code AS zoneCode, CONVERT(varchar(23), UpdateTime, 121) + '_' + ID AS __lastKey " +
            "FROM TmsZone WHERE (CONVERT(varchar(23), UpdateTime, 121) + '_' + ID) > @lastKey ORDER BY __lastKey ASC";
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

        // Nút Hướng dẫn thiết lập hiển thị và mở được popover mà không ném SyntaxError
        const guideBtn = panel.locator('.svp-guide-btn');
        await expect(guideBtn).toBeVisible();
        await guideBtn.click();

        // Popover nội dung hướng dẫn hiển thị 4 bước
        const guideSteps = page.locator('.svp-guide-popper .svp-guide-step');
        await expect(guideSteps).toHaveCount(4);
        await expect(guideSteps.nth(2)).toContainText('@pageSize');

        // Tuyệt đối không có bất kỳ lỗi Uncaught SyntaxError / pageerror nào
        expect(pageErrors.filter((e) => e.includes('SyntaxError') || e.includes('Invalid linked format'))).toEqual([]);
    });

    /**
     * Description: Điều tra hiện tượng giật width của thanh active-bar (.el-tabs__active-bar) khi chuyển tab đối tác:
     *              - Bấm từ "Mẫu chung" sang tab đối tác "ddlt";
     *              - Quan sát biến thiên width và transform của thanh gạch dưới (.el-tabs__active-bar);
     *              - Phát hiện nguyên nhân giật (nhiều lần thay đổi width đột ngột hoặc animation bị đứt gãy).
     * Created date: 28/10/2026
     */
    test('53. Điều tra hiện tượng giật width của thanh active-bar khi chuyển tab đối tác', async ({ page }) => {
        const compositeSql =
            "SELECT TOP (@pageSize) Code AS zoneCode, CONVERT(varchar(23), UpdateTime, 121) + '_' + ID AS __lastKey " +
            "FROM TmsZone WHERE (CONVERT(varchar(23), UpdateTime, 121) + '_' + ID) > @lastKey ORDER BY __lastKey ASC";

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

        const partnerTabs = page.locator('.sql-partner-tabs');
        await expect(partnerTabs).toBeVisible({ timeout: 10_000 });

        const ddltTab = partnerTabs.locator('.el-tabs__item').filter({ hasText: 'ddlt' }).first();
        await expect(ddltTab).toBeVisible({ timeout: 10_000 });

        const ddtl2Tab = partnerTabs.locator('.el-tabs__item').filter({ hasText: 'DDTL-2' }).first();
        const stdTab = partnerTabs.locator('.el-tabs__item').filter({ hasText: 'Mẫu chung' }).first();

        // Cài đặt bộ theo dõi (monitor) ghi lại hình học và style của active-bar và tabs
        const logHandle = await page.evaluateHandle(() => {
            const records: any[] = [];
            const styleMutations: any[] = [];
            (window as any).__barRecords = records;
            (window as any).__styleMutations = styleMutations;

            const tabs = document.querySelector('.sql-partner-tabs');
            const bar = tabs?.querySelector('.el-tabs__active-bar') as HTMLElement;
            const items = () => Array.from(tabs?.querySelectorAll('.el-tabs__item') || []) as HTMLElement[];

            let start = performance.now();
            let recording = true;

            // MutationObserver theo dõi mọi lần style của bar bị ghi đè
            if (bar) {
                const mo = new MutationObserver((mutations) => {
                    for (const m of mutations) {
                        if (m.type === 'attributes' && m.attributeName === 'style') {
                            const now = performance.now() - start;
                            styleMutations.push({
                                t: Math.round(now),
                                widthStyle: bar.style.width,
                                transformStyle: bar.style.transform,
                                rectWidth: Math.round(bar.getBoundingClientRect().width * 100) / 100,
                                rectLeft: Math.round(bar.getBoundingClientRect().left * 100) / 100,
                            });
                        }
                    }
                });
                mo.observe(bar, { attributes: true, attributeFilter: ['style'] });
            }

            const capture = () => {
                if (!recording) return;
                const now = performance.now() - start;
                const barRect = bar ? bar.getBoundingClientRect() : null;
                const csBar = bar ? window.getComputedStyle(bar) : null;
                const itemSnapshots = items().map((it) => {
                    const cs = window.getComputedStyle(it);
                    return {
                        id: it.id,
                        text: it.textContent?.trim(),
                        isActive: it.classList.contains('is-active'),
                        clientWidth: it.clientWidth,
                        scrollWidth: it.scrollWidth,
                        paddingLeft: cs.paddingLeft,
                        paddingRight: cs.paddingRight,
                        rectWidth: Math.round(it.getBoundingClientRect().width * 100) / 100,
                    };
                });

                records.push({
                    t: Math.round(now),
                    barWidthStyle: bar?.style.width,
                    barTransformStyle: bar?.style.transform,
                    computedWidth: csBar?.width,
                    rectWidth: barRect ? Math.round(barRect.width * 100) / 100 : null,
                    rectLeft: barRect ? Math.round(barRect.left * 100) / 100 : null,
                    items: itemSnapshots,
                });
                requestAnimationFrame(capture);
            };
            requestAnimationFrame(capture);

            return {
                stop() {
                    recording = false;
                    return records;
                },
            };
        });

        console.log('--- Bấm ddlt ---');
        await ddltTab.click();
        await page.waitForTimeout(600);

        console.log('--- Bấm DDTL-2 ---');
        await ddtl2Tab.click();
        await page.waitForTimeout(600);

        console.log('--- Bấm Mẫu chung ---');
        await stdTab.click();
        await page.waitForTimeout(600);

        // Lấy toàn bộ records và mutations
        const { records, mutations } = await logHandle.evaluate((m) => {
            const recs = m.stop();
            return {
                records: recs,
                mutations: (window as any).__styleMutations || [],
            };
        });

        console.log('=== MUTATION OBSERVER STYLE ASSIGNMENTS ===');
        console.log('Total style mutations:', mutations.length);
        console.log('Mutations:', mutations);

        console.log('=== ITEMS COMPUTED PADDING & WIDTH ===');
        console.log('Initial items:', JSON.stringify(records[0]?.items, null, 2));
        console.log('Final items:', JSON.stringify(records[records.length - 1]?.items, null, 2));

        console.log('=== TỔNG HỢP CÁC LẦN BAR.STYLE THAY ĐỔI ===');
        const styleChanges: any[] = [];
        let prevWidthStyle = '';
        let prevTransformStyle = '';
        for (const r of records) {
            if (r.barWidthStyle !== prevWidthStyle || r.barTransformStyle !== prevTransformStyle) {
                styleChanges.push({
                    t: r.t,
                    barWStyle: r.barWidthStyle,
                    barTrStyle: r.barTransformStyle,
                    rectWidth: r.rectWidth,
                    rectLeft: r.rectLeft,
                });
                prevWidthStyle = r.barWidthStyle;
                prevTransformStyle = r.barTransformStyle;
            }
        }
        console.log('Style changes:', styleChanges);

        console.log('=== PHÂN TÍCH ĐỘ MƯỢT CỦA WIDTH (DELTA QUA CÁC FRAME) ===');
        const deltas: { t: number; fromW: number; toW: number; delta: number; dt: number }[] = [];
        for (let i = 1; i < records.length; i++) {
            const prev = records[i - 1];
            const curr = records[i];
            const dW = Math.abs((curr.rectWidth || 0) - (prev.rectWidth || 0));
            const dt = curr.t - prev.t;
            if (dW > 0.1) {
                deltas.push({
                    t: curr.t,
                    fromW: prev.rectWidth,
                    toW: curr.rectWidth,
                    delta: Math.round(dW * 100) / 100,
                    dt,
                });
            }
        }
        console.log('Deltas có thay đổi width:', deltas);
        const maxDelta = deltas.reduce((m, d) => Math.max(m, d.delta), 0);
        console.log('Max delta width trong 1 frame:', maxDelta);

        // Khẳng định: không còn cú giật width đột ngột (> 10px / frame do nghẽn main thread)
        expect(maxDelta).toBeLessThan(10);

        // Khẳng định: kích thước dừng ổn định của thanh active-bar khớp chính xác với tab đang chọn
        const lastRecord = records[records.length - 1];
        expect(lastRecord.barWidthStyle).toBe('67px');
    });

    /**
     * Description: Điều tra lỗi bấm gói 102 (102_cctvData) sau khi xem gói 101/103:
     *              - Bấm gói 103, sau đó bấm gói 101, rồi bấm gói 102;
     *              - Toàn bộ API trả về 200 OK;
     *              - Kiểm tra lớp phủ loading (.solid-loading-mask / .el-loading-mask) phải biến mất hoàn toàn
     *                và danh sách trường của gói 102 hiển thị đúng; không bị kẹt spinner loading.
     * Created date: 28/10/2026
     */
    test('54. Điều tra lỗi bấm gói 102: API trả về 200 nhưng điều tra trạng thái loading', async ({ page }) => {
        const pageErrors: string[] = [];
        page.on('pageerror', (err) => pageErrors.push(err.message));

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

        // Chờ sau khi các API đã trả về: mặt nạ loading phải biến mất hoàn toàn
        const solidLoading = page.locator('.solid-loading-mask');
        await expect(solidLoading).toHaveCount(0, { timeout: 5_000 });
        await expect(detailsWrapper).not.toHaveClass(/is-switching/);

        // Danh sách trường của gói 102 phải hiển thị (cameraCode), không còn là fromLocationKm
        await expect(fieldsGrid).toContainText('cameraCode', { timeout: 5_000 });
        await expect(fieldsGrid).not.toContainText('fromLocationKm');

        expect(pageErrors).toEqual([]);
    });
});
