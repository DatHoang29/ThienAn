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

        // Bảng mốc đối tác hiển thị trực tiếp ở tab Mẫu chung khi có đối tác
        const partnerDrawer = panel.locator('.svp-partner-cursor-drawer');
        await expect(partnerDrawer).toBeVisible({ timeout: 10_000 });
        const partnerCursorTable = partnerDrawer.locator('.svp-table');
        await expect(partnerCursorTable).toBeVisible();
        await expect(partnerCursorTable.locator('tbody tr')).toHaveCount(2);

        // 2. Chuyển sang tab Đối tác riêng CSGT: bảng tham số thêm cột So mẫu chung (4 cột), mốc đang chạy hiện ở dòng .svp-progress dưới bảng
        const partnerTab = page.locator('.sql-partner-tabs .el-tabs__item').filter({ hasText: 'CSGT' }).first();
        await expect(partnerTab).toBeVisible();
        await partnerTab.click();

        await expect(mainTable.locator('thead th')).toHaveCount(4);
        await expect(panel.locator('.svp-saved-code')).toHaveText('2026-10-27 10:05:00.000_Z02');

        // 3. Đặt lại mốc của CSGT (p_a)
        await panel.locator('.svp-reset-btn').click();
        const confirmBtn = page.locator('.el-message-box__btns .el-button--primary');
        await expect(confirmBtn).toBeVisible();
        await confirmBtn.click();

        await expect.poll(() => resetPayloads.length).toBe(1);
        expect(resetPayloads[0]).toEqual({ packetCode: '101_commonData', partnerId: 'p_a' });
        await expect(panel.locator('.svp-saved-code')).toHaveCount(0);
    });

    /**
     * Description: Khi bấm vào gói tin trên bảng bên trái (xGridPacket):
     *              - Bảng chi tiết bên phải nạp thành công, không bị lỗi cú pháp i18n (SyntaxError: Invalid linked format) hay crash trắng màn hình;
     *              - Khối tham số SQL hiển thị đầy đủ, dòng giải thích mốc hiển thị bình thường.
     * Created date: 28/10/2026
     */
    test('52. Bấm vào gói tin bên table trái: không bị crash trắng màn hình bên phải và dòng giải thích mốc hiển thị bình thường', async ({ page }) => {
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

        // Dải kiểm nhanh mốc hiển thị với icon bóng đèn và các chip quy chuẩn mốc không rớt dòng
        const checks = panel.locator('.svp-checks');
        await expect(checks).toBeVisible();

        // Icon bóng đèn (ele-Opportunity) hiển thị trong phần lead của dải kiểm nhanh
        const bulb = checks.locator('.svp-check-bulb');
        await expect(bulb).toBeVisible();

        // Các chip kiểm tra quy chuẩn hiển thị gọn gàng
        const chips = checks.locator('.svp-check-chip');
        expect(await chips.count()).toBeGreaterThanOrEqual(3);

        // Hover vào lead mở popover quy chuẩn
        await checks.locator('.svp-check-lead').hover();
        const popover = page.locator('.svp-popover-content');
        await expect(popover).toBeVisible({ timeout: 5_000 });

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
     * Modified date: 29/10/2026 — giữ phản hồi gói 102 tới khi kiểm xong khóa cuộn (mock trả ngay thì lúc lăn chuột gói đã nạp xong)
     */
    test('54. Điều tra lỗi bấm gói 102: API trả về 200 nhưng điều tra trạng thái loading', async ({ page }) => {
        const pageErrors: string[] = [];
        page.on('pageerror', (err) => pageErrors.push(err.message));

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
        page.on('pageerror', (err) => pageErrors.push(err.message));

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
        page.on('pageerror', (err) => pageErrors.push(err.message));

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
        const successToast = page.locator('.el-message--success');
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

    test('60. Đặt mốc gửi dữ liệu cho đối tác (SetLastSend) từ bảng mốc đối tác', async ({ page }) => {
        const pageErrors: string[] = [];
        page.on('pageerror', (err) => pageErrors.push(err.message));

        const cursorSql =
            "SELECT TOP (@pageSize) ID AS zoneId, UpdateTime AS __lastTime, ID AS __lastKey FROM TmsZone " +
            "WHERE UpdateTime >= ISNULL(@lastTime, '17530101') AND (@lastTime IS NULL OR UpdateTime > @lastTime OR ID > @lastKey) ORDER BY UpdateTime ASC, ID ASC";

        await page.route('**/api/sharedata/sharedatapacket/getsqldetail*', (route) =>
            route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        extract: {
                            extractSql: cursorSql,
                            status: 1,
                            fields: [{ key: 'zoneId', type: 'string', label: 'Mã khu vực', required: true }],
                        },
                        variables: {
                            items: [
                                { code: 'lastTime', valueKind: 'Builtin', value: 'lastTime', dataType: 'DateTime', orderNo: 1 },
                                { code: 'lastKey', valueKind: 'Builtin', value: 'lastKey', dataType: 'String', orderNo: 2 },
                            ],
                        },
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
                        { partnerId: 'p_ddlt', partnerCode: 'ddlt', partnerName: 'Đồng Đăng - Trà Lĩnh', isCustom: false },
                        { partnerId: 'p_none', partnerCode: 'p_none', partnerName: 'Chưa có mốc', isCustom: false },
                    ],
                }),
            })
        );

        await page.route('**/api/sharedata/sharedatapacket/getsqlvariables*', (route) => {
            const url = route.request().url();
            const isNone = url.includes('p_none');
            return route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        items: isNone
                            ? [
                                  { code: 'lastTime', valueKind: 'Builtin', value: 'lastTime', dataType: 'DateTime', savedValue: null },
                                  { code: 'lastKey', valueKind: 'Builtin', value: 'lastKey', dataType: 'String', savedValue: null },
                              ]
                            : [
                                  { code: 'lastTime', valueKind: 'Builtin', value: 'lastTime', dataType: 'DateTime', savedValue: '2026-10-28 08:00:00' },
                                  { code: 'lastKey', valueKind: 'Builtin', value: 'lastKey', dataType: 'String', savedValue: 'k_100' },
                              ],
                    },
                }),
            });
        });

        let setLastSendPayload: any = null;
        await page.route('**/api/sharedata/sharedatapacket/setlastsend*', async (route) => {
            setLastSendPayload = route.request().postDataJSON();
            await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ code: 200, result: true }) });
        });

        await page.setViewportSize({ width: 1600, height: 950 });
        await page.goto('/#/sharedata/dataSource');

        const firstRow = page.locator('.packet-col .vxe-table--main-wrapper .vxe-body--row').first();
        await expect(firstRow).toBeVisible();
        await firstRow.click();

        // Chờ bảng mốc đối tác hiển thị
        const drawer = page.locator('.svp-partner-cursor-drawer');
        await expect(drawer).toBeVisible();

        // Kiểm tra cell thao tác có layout cách nhau (.svp-action-cell) và tooltip giải thích khi disabled / enabled
        const actionCell1 = drawer.locator('.svp-action-cell').first();
        await expect(actionCell1).toBeVisible();
        const resetBtn1 = actionCell1.locator('.svp-reset-btn');
        await expect(resetBtn1).toBeVisible();
        await expect(resetBtn1).toBeEnabled();

        // Row 2 chưa có mốc: nút Đặt lại bị disabled và hover hiển thị tooltip giải thích
        const actionCell2 = drawer.locator('.svp-action-cell').nth(1);
        await expect(actionCell2).toBeVisible();
        const resetBtn2 = actionCell2.locator('.svp-reset-btn');
        await expect(resetBtn2).toBeVisible();
        await expect(resetBtn2).toBeDisabled();

        // Hover vào wrapper nút Đặt lại khi disabled để kiểm tra tooltip giải thích lý do disable
        await actionCell2.locator('.svp-btn-wrapper').nth(1).hover();
        const disabledTooltip = page.locator('.el-popper:visible', { hasText: 'Chưa có mốc gửi dữ liệu để đặt lại' });
        await expect(disabledTooltip).toBeVisible({ timeout: 5_000 });

        // Bấm nút "✎ Đặt mốc"
        const setCursorBtn = drawer.locator('.svp-set-cursor-btn').first();
        await expect(setCursorBtn).toBeVisible();
        await setCursorBtn.click();

        // Dialog đặt mốc hiện ra
        const dialog = page.locator('.svp-set-cursor-dialog');
        await expect(dialog).toBeVisible();

        // Nhập LastKey mới
        const keyInput = dialog.locator('.el-form-item', { hasText: 'Mốc khóa' }).locator('input');
        await keyInput.fill('k_new_200');

        // Bấm Xác nhận
        await dialog.locator('.el-dialog__footer .el-button--primary').click();
        await expect(dialog).toBeHidden();

        // Toast thành công hiển thị
        const successToast = page.locator('.el-message--success');
        await expect(successToast).toBeVisible();

        // Kiểm tra payload gửi lên API SetLastSend
        expect(setLastSendPayload).toBeTruthy();
        expect(setLastSendPayload.packetCode).toBe('101_commonData');
        expect(setLastSendPayload.partnerId).toBe('p_ddlt');
        expect(setLastSendPayload.lastKey).toBe('k_new_200');

        expect(pageErrors).toEqual([]);
    });

    test('57. Lọc theo đối tác dùng chung (isCustom=false) thì ở tab Mẫu chung, không sinh tab tạm và không có khóa i18n thô', async ({ page }) => {
        const pageErrors: string[] = [];
        page.on('pageerror', (err) => pageErrors.push(err.message));

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

        // Kiểm tra thanh tab: Mẫu chung đang active
        const stdTab = page.locator('.sql-partner-tabs .el-tabs__item', { hasText: 'Mẫu chung' });
        await expect(stdTab).toBeVisible();
        await expect(stdTab).toHaveClass(/is-active/);

        // Tiêu đề câu truy vấn là Mẫu chung, KHÔNG được hiện tiêu đề riêng của TTCSDL
        const editorTitle = page.locator('.sql-section .sql-editor-title');
        await expect(editorTitle).toBeVisible();
        await expect(editorTitle).toContainText(/mẫu chung/i);
        await expect(editorTitle).not.toContainText('TTCSDL');

        // Đối tác có isCustom = true (DDTL-2) có tab riêng
        const ddtl2Tab = page.locator('.sql-partner-tabs .el-tabs__item', { hasText: 'DDTL-2' });
        await expect(ddtl2Tab).toBeVisible();

        // Đối tác dùng chung (TTCSDL) KHÔNG có tab riêng
        const ttcsdlTab = page.locator('.sql-partner-tabs .el-tabs__item', { hasText: 'TTCSDL' });
        await expect(ttcsdlTab).toHaveCount(0);

        // Chuyển sang tab DDTL-2: Tiêu đề chuyển sang riêng của DDTL-2, tab TTCSDL vẫn không bao giờ xuất hiện
        await ddtl2Tab.click();
        await expect(ddtl2Tab).toHaveClass(/is-active/);
        await expect(editorTitle).toContainText('DDTL-2');
        await expect(ttcsdlTab).toHaveCount(0);

        // Quay lại Mẫu chung
        await stdTab.click();
        await expect(stdTab).toHaveClass(/is-active/);
        await expect(editorTitle).toContainText(/mẫu chung/i);

        // Bảng mốc đối tác hiển thị đầy đủ
        const partnerDrawer = page.locator('.svp-partner-cursor-drawer');
        await expect(partnerDrawer).toBeVisible();

        // Nút đặt mốc đã dịch đầy đủ (chứa chữ "Đặt mốc", không chứa chuỗi thô "lz.")
        const setCursorBtn = partnerDrawer.locator('.svp-set-cursor-btn').first();
        await expect(setCursorBtn).toBeVisible();
        const btnText = await setCursorBtn.textContent();
        expect(btnText).toContain('Đặt mốc');
        expect(btnText).not.toContain('lz.');

        // Không có bất kỳ lỗi JavaScript / pageerror nào
        expect(pageErrors).toEqual([]);
    });

    test('58. Khi đổi tab đối tác, vùng soạn thảo SQL và các khối nội dung được che hoàn toàn dưới nền trắng solid-loading-mask', async ({ page }) => {
        const pageErrors: string[] = [];
        page.on('pageerror', (err) => pageErrors.push(err.message));

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

        // Chuyển sang tab DDTL-2
        const ddtl2Tab = page.locator('.sql-partner-tabs .el-tabs__item', { hasText: 'DDTL-2' });
        await expect(ddtl2Tab).toBeVisible();

        // Click tab đối tác DDTL-2 và kiểm tra trạng thái loading che phủ
        await ddtl2Tab.click();

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
        page.on('pageerror', (err) => pageErrors.push(err.message));

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
        // - Không có loading trên bảng mốc đối tác .svp-table
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
     * Description: Đặt mốc ở tab Mẫu chung theo cấu hình của từng đối tác (mẫu chung chỉ dùng khóa):
     *              - Đối tác có cấu hình riêng 2 cột: hộp thoại hiện ô mốc thời gian, điền sẵn mốc đang chạy, gửi kèm cả thời gian và khóa;
     *              - Đối tác dùng mẫu chung: chỉ có ô khóa (tối đa 256 ký tự), bỏ trống thì nút Xác nhận bị khóa, không gửi mốc thời gian.
     * Created date: 29/10/2026
     */
    test('61. Đặt mốc ở tab Mẫu chung: ô nhập theo cấu hình của từng đối tác, bỏ trống mốc chính thì khóa nút Xác nhận', async ({ page }) => {
        const keyOnlySql = 'SELECT TOP (@pageSize) ID AS zoneId, ID AS __lastKey FROM TmsZone WHERE ID > @lastKey ORDER BY ID ASC';
        const twoColumnSql =
            'SELECT TOP (@pageSize) ID AS zoneId, UpdateTime AS __lastTime, ID AS __lastKey FROM TmsZone ' +
            "WHERE UpdateTime >= ISNULL(@lastTime, '17530101') AND (@lastTime IS NULL OR UpdateTime > @lastTime OR ID > @lastKey) ORDER BY UpdateTime ASC, ID ASC";
        const zoneField = { key: 'zoneId', type: 'string', label: 'Mã khu vực', required: true };
        const pageSizeItem = { code: 'pageSize', valueKind: 'Const', value: '100', dataType: 'Int', orderNo: 1 };
        const keyItem = { code: 'lastKey', valueKind: 'Builtin', value: 'lastKey', dataType: 'String', orderNo: 2 };
        const timeItem = { code: 'lastTime', valueKind: 'Builtin', value: 'lastTime', dataType: 'DateTime', orderNo: 3 };
        const setPayloads: any[] = [];

        await page.route('**/api/sharedata/sharedatapacket/getsqldetail*', (route) =>
            route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        extract: { extractSql: keyOnlySql, status: 1, fields: [zoneField] },
                        variables: { items: [pageSizeItem, keyItem] },
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
                        { partnerId: 'p_custom2', partnerCode: 'CUSTOM2', partnerName: 'Đối tác cấu hình riêng', isCustom: true },
                        { partnerId: 'p_shared1', partnerCode: 'SHARED1', partnerName: 'Đối tác dùng mẫu chung', isCustom: false },
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
                    result: { isCustom: true, customSql: twoColumnSql, standardSql: keyOnlySql, fields: [zoneField], commonFields: [zoneField] },
                }),
            })
        );

        // Bộ biến theo cấu hình của từng đối tác: CUSTOM2 chạy câu 2 cột riêng, SHARED1 dùng mẫu chung chỉ khóa
        await page.route('**/api/sharedata/sharedatapacket/getsqlvariables*', (route) => {
            const url = new URL(route.request().url());
            const partnerId = url.searchParams.get('PartnerId') ?? url.searchParams.get('partnerId');
            const items =
                partnerId === 'p_custom2'
                    ? [pageSizeItem, { ...timeItem, savedValue: '2026-10-28 08:00:00.000' }, { ...keyItem, savedValue: 'k_100' }]
                    : [pageSizeItem, keyItem];
            return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ code: 200, result: { items } }) });
        });

        await page.route('**/api/sharedata/sharedatapacket/setlastsend*', (route) => {
            setPayloads.push(route.request().postDataJSON());
            return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ code: 200, result: null }) });
        });

        await page.setViewportSize({ width: 1600, height: 950 });
        await page.goto('/#/sharedata/dataSource');

        const firstRow = page.locator('.packet-col .vxe-table--main-wrapper .vxe-body--row').first();
        await expect(firstRow).toBeVisible({ timeout: 30_000 });
        await firstRow.click();

        const drawer = page.locator('.svp-partner-cursor-drawer');
        await expect(drawer.locator('tbody tr')).toHaveCount(2, { timeout: 10_000 });
        const dialog = page.locator('.svp-set-cursor-dialog');
        const confirmBtn = dialog.locator('.el-dialog__footer .el-button--primary');
        const keyInput = dialog.locator('.el-form-item', { hasText: 'Mốc khóa' }).locator('input');

        // 1. CUSTOM2 (cấu hình riêng 2 cột): có ô mốc thời gian dù mẫu chung chỉ dùng khóa, điền sẵn mốc đang chạy
        await drawer.locator('tbody tr', { hasText: 'CUSTOM2' }).locator('.svp-set-cursor-btn').click();
        await expect(dialog).toBeVisible();
        await expect(dialog.locator('.el-date-editor input')).toHaveValue('28/10/2026 08:00:00');
        await expect(keyInput).toHaveValue('k_100');
        await keyInput.fill('k_200');
        await confirmBtn.click();
        await expect(dialog).toBeHidden();
        await expect.poll(() => setPayloads.length).toBe(1);
        expect(setPayloads[0]).toEqual({ packetCode: '101_commonData', partnerId: 'p_custom2', lastTime: '2026-10-28 08:00:00.000', lastKey: 'k_200' });

        // 2. SHARED1 (mẫu chung chỉ khóa): chỉ có ô khóa tối đa 256 ký tự; bỏ trống thì không Xác nhận được, không gửi mốc thời gian
        await drawer.locator('tbody tr', { hasText: 'SHARED1' }).locator('.svp-set-cursor-btn').click();
        await expect(dialog).toBeVisible();
        await expect(dialog.locator('.el-date-editor')).toHaveCount(0);
        await expect(keyInput).toHaveAttribute('maxlength', '256');
        await expect(confirmBtn).toBeDisabled();
        await keyInput.fill('k_300');
        await expect(confirmBtn).toBeEnabled();
        await confirmBtn.click();
        await expect.poll(() => setPayloads.length).toBe(2);
        expect(setPayloads[1]).toEqual({ packetCode: '101_commonData', partnerId: 'p_shared1', lastTime: null, lastKey: 'k_300' });
    });

    /**
     * Description: Đặt mốc ở tab đối tác khi bảng tham số đang sửa dở (chưa lưu): không nạp lại bảng tham số nên giữ nguyên phần đang sửa,
     *              dải Mốc đang chạy hiện ngay mốc vừa đặt.
     * Created date: 29/10/2026
     */
    test('62. Đặt mốc ở tab đối tác khi bảng tham số đang sửa dở: giữ phần đang sửa, dải Mốc đang chạy hiện mốc mới', async ({ page }) => {
        const twoColumnSql =
            'SELECT TOP (@pageSize) ID AS zoneId, UpdateTime AS __lastTime, ID AS __lastKey FROM TmsZone ' +
            "WHERE UpdateTime >= ISNULL(@lastTime, '17530101') AND (@lastTime IS NULL OR UpdateTime > @lastTime OR ID > @lastKey) ORDER BY UpdateTime ASC, ID ASC";
        const zoneField = { key: 'zoneId', type: 'string', label: 'Mã khu vực', required: true };
        const commonItems = [
            { code: 'pageSize', valueKind: 'Const', value: '100', dataType: 'Int', orderNo: 1 },
            { code: 'lastTime', valueKind: 'Builtin', value: 'lastTime', dataType: 'DateTime', orderNo: 2 },
            { code: 'lastKey', valueKind: 'Builtin', value: 'lastKey', dataType: 'String', orderNo: 3 },
        ];
        const savedValues: Record<string, string> = { lastTime: '2026-10-28 08:00:00.000', lastKey: 'k_100' };
        const setPayloads: any[] = [];
        let partnerVariableCalls = 0;

        await page.route('**/api/sharedata/sharedatapacket/getsqldetail*', (route) =>
            route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: {
                        extract: { extractSql: twoColumnSql, status: 1, fields: [zoneField] },
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
                    result: [{ partnerId: 'p_custom2', partnerCode: 'CUSTOM2', partnerName: 'Đối tác cấu hình riêng', isCustom: true }],
                }),
            })
        );

        await page.route('**/api/sharedata/sharedatapacket/getpartnerextractsql*', (route) =>
            route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({
                    code: 200,
                    result: { isCustom: true, customSql: twoColumnSql, standardSql: twoColumnSql, fields: [zoneField], commonFields: [zoneField] },
                }),
            })
        );

        await page.route('**/api/sharedata/sharedatapacket/getsqlvariables*', (route) => {
            const url = new URL(route.request().url());
            const partnerId = url.searchParams.get('PartnerId') ?? url.searchParams.get('partnerId');
            if (partnerId === 'p_custom2') partnerVariableCalls++;
            const items = commonItems.map((it) => ({ ...it, savedValue: partnerId === 'p_custom2' ? (savedValues[it.code] ?? null) : null }));
            return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ code: 200, result: { items } }) });
        });

        await page.route('**/api/sharedata/sharedatapacket/setlastsend*', (route) => {
            setPayloads.push(route.request().postDataJSON());
            return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ code: 200, result: null }) });
        });

        await page.setViewportSize({ width: 1600, height: 950 });
        await page.goto('/#/sharedata/dataSource');

        const firstRow = page.locator('.packet-col .vxe-table--main-wrapper .vxe-body--row').first();
        await expect(firstRow).toBeVisible({ timeout: 30_000 });
        await firstRow.click();

        const partnerTab = page.locator('.sql-partner-tabs .el-tabs__item').filter({ hasText: 'CUSTOM2' }).first();
        await expect(partnerTab).toBeVisible({ timeout: 10_000 });
        await partnerTab.click();

        const panel = page.locator('.sql-variable-panel');
        const progress = panel.locator('.svp-progress');
        await expect(progress.locator('.svp-saved-code', { hasText: 'k_100' })).toBeVisible({ timeout: 10_000 });

        // Sửa dở bảng tham số (chưa lưu): số dòng mỗi lượt 100 -> 200
        const pageSizeInput = panel.locator('.svp-const-input input');
        await pageSizeInput.fill('200');
        const callsBeforeSet = partnerVariableCalls;

        await progress.locator('.el-button--primary').click();
        const dialog = page.locator('.svp-set-cursor-dialog');
        await expect(dialog).toBeVisible();
        await dialog.locator('.el-form-item', { hasText: 'Mốc khóa' }).locator('input').fill('k_300');
        await dialog.locator('.el-dialog__footer .el-button--primary').click();
        await expect(dialog).toBeHidden();
        await expect.poll(() => setPayloads.length).toBe(1);
        expect(setPayloads[0]).toEqual({ packetCode: '101_commonData', partnerId: 'p_custom2', lastTime: '2026-10-28 08:00:00.000', lastKey: 'k_300' });

        // Dải Mốc đang chạy hiện ngay mốc mới; phần đang sửa còn nguyên vì không nạp lại bảng tham số
        await expect(progress.locator('.svp-saved-code', { hasText: 'k_300' })).toBeVisible();
        await expect(pageSizeInput).toHaveValue('200');
        expect(partnerVariableCalls).toBe(callsBeforeSet);
    });

    /**
     * Description: Bảng tham số dạng hai cột mà @lastKey chọn nhầm nguồn "theo thời gian" (không còn tham số nào lấy mốc theo key):
     *              dải kiểm nhanh cảnh báo; chọn lại nguồn "Nối tiếp lần trước (theo key)" cho @lastKey thì mục kiểm chuyển đạt,
     *              ô giá trị của @lastKey thôi hiện chọn Toàn bộ / Từ ngày.
     * Created date: 29/10/2026
     */
    test('63. Dạng hai cột mà @lastKey chọn nhầm nguồn theo thời gian: dải kiểm nhanh cảnh báo, chọn lại nguồn theo key thì đạt', async ({ page }) => {
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
                                // Chọn nhầm: @lastKey lấy nguồn theo thời gian
                                { code: 'lastKey', valueKind: 'Builtin', value: 'lastTime', dataType: 'DateTime', orderNo: 3 },
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
        const keySourceCheck = panel.locator('.svp-check-chip', { hasText: 'Nguồn key' });
        await expect(keySourceCheck).toHaveClass(/is-warn/, { timeout: 10_000 });

        // Chọn lại nguồn "Nối tiếp lần trước (theo key)" cho @lastKey
        const lastKeyRow = panel.locator('.svp-table').first().locator('tbody tr', { hasText: '@lastKey' });
        await expect(lastKeyRow.locator('.svp-time-radio-group')).toBeVisible();
        await lastKeyRow.locator('.svp-source-select').click();
        await page.locator('.el-popper.svp-source-popper:visible .el-select-dropdown__item', { hasText: 'Nối tiếp lần trước (theo key)' }).click();

        await expect(keySourceCheck).toHaveClass(/is-pass/);
        await expect(lastKeyRow.locator('.svp-no-input-text')).toBeVisible();
        await expect(lastKeyRow.locator('.svp-time-radio-group')).toHaveCount(0);
    });

    /**
     * Description: Câu gộp hai nguồn, mỗi nguồn một nhóm mốc (biến @lastTime_<nhóm> / @lastKey_<nhóm>, cột __lastTime_<nhóm> / __lastKey_<nhóm>):
     *              dải Quy chuẩn mốc đạt mục Cột mốc và Nguồn key theo từng nhóm; bảng mốc đối tác hiện mốc của từng nhóm;
     *              hộp Đặt mốc có ô riêng từng nhóm và gửi mỗi nhóm một lần kèm cursorKey.
     * Created date: 30/10/2026
     */
    test('64. Câu gộp nhiều nguồn có nhóm mốc: dải kiểm đạt, bảng mốc hiện từng nhóm, Đặt mốc gửi riêng từng nhóm', async ({ page }) => {
        const groupSql =
            'SELECT TOP (@pageSize) u.rowCode, u.__lastTime_pkt, u.__lastKey_pkt, u.__lastTime_toll, u.__lastKey_toll FROM (' +
            'SELECT p.Code AS rowCode, p.UpdateTime AS sortTime, p.ID AS sortKey, p.UpdateTime AS __lastTime_pkt, p.ID AS __lastKey_pkt, ' +
            'CAST(NULL AS datetime) AS __lastTime_toll, CAST(NULL AS nvarchar(64)) AS __lastKey_toll FROM ShareDataPacket p ' +
            "WHERE p.UpdateTime >= ISNULL(@lastTime_pkt, '17530101') AND (@lastTime_pkt IS NULL OR p.UpdateTime > @lastTime_pkt OR p.ID > @lastKey_pkt) " +
            'UNION ALL SELECT t.SourceId, t.SourceWriteDate, t.ID, NULL, NULL, t.SourceWriteDate, t.ID FROM TollPriceList t ' +
            "WHERE t.SourceWriteDate >= ISNULL(@lastTime_toll, '17530101') AND (@lastTime_toll IS NULL OR t.SourceWriteDate > @lastTime_toll OR t.ID > @lastKey_toll)" +
            ') u ORDER BY u.sortTime ASC, u.sortKey ASC';
        const groupItems = [
            { code: 'pageSize', valueKind: 'Const', value: '100', dataType: 'Int', orderNo: 1 },
            { code: 'lastTime_pkt', valueKind: 'Builtin', value: 'lastTime', dataType: 'DateTime', orderNo: 2 },
            { code: 'lastKey_pkt', valueKind: 'Builtin', value: 'lastKey', dataType: 'String', orderNo: 3 },
            { code: 'lastTime_toll', valueKind: 'Builtin', value: 'lastTime', dataType: 'DateTime', orderNo: 4 },
            { code: 'lastKey_toll', valueKind: 'Builtin', value: 'lastKey', dataType: 'String', orderNo: 5 },
        ];
        const savedValues: Record<string, string> = {
            lastTime_pkt: '2026-10-27 09:30:00.000',
            lastKey_pkt: 'a2',
            lastTime_toll: '2026-10-27 09:20:00.000',
            lastKey_toll: 't9',
        };
        const setPayloads: any[] = [];

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

        await page.route('**/api/sharedata/sharedatapacket/getsqlvariables*', (route) => {
            const url = new URL(route.request().url());
            const partnerId = url.searchParams.get('PartnerId') ?? url.searchParams.get('partnerId');
            const items = groupItems.map((it) => ({ ...it, savedValue: partnerId === 'p_grp' ? (savedValues[it.code] ?? null) : null }));
            return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ code: 200, result: { items } }) });
        });

        await page.route('**/api/sharedata/sharedatapacket/setlastsend*', (route) => {
            setPayloads.push(route.request().postDataJSON());
            return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ code: 200, result: null }) });
        });

        await page.setViewportSize({ width: 1600, height: 950 });
        await page.goto('/#/sharedata/dataSource');

        const firstRow = page.locator('.packet-col .vxe-table--main-wrapper .vxe-body--row').first();
        await expect(firstRow).toBeVisible({ timeout: 30_000 });
        await firstRow.click();

        // Dải Quy chuẩn mốc: có cột mốc của từng nhóm, mỗi nhóm có biến lấy nguồn theo key
        const panel = page.locator('.sql-variable-panel');
        await expect(panel.locator('.svp-check-chip', { hasText: 'Cột mốc' })).toHaveClass(/is-pass/, { timeout: 10_000 });
        await expect(panel.locator('.svp-check-chip', { hasText: 'Nguồn key' })).toHaveClass(/is-pass/);

        // Bảng mốc đối tác hiện mốc của từng nhóm
        const partnerRow = page.locator('.svp-partner-cursor-drawer tbody tr', { hasText: 'GRP' });
        await expect(partnerRow).toContainText('pkt: 2026-10-27 09:30:00.000 · a2');
        await expect(partnerRow).toContainText('toll: 2026-10-27 09:20:00.000 · t9');

        // Hộp Đặt mốc: mỗi nhóm một khối ô nhập; sửa mốc khóa nhóm toll rồi Xác nhận
        await partnerRow.locator('.svp-set-cursor-btn').click();
        const dialog = page.locator('.svp-set-cursor-dialog');
        await expect(dialog.locator('.svp-cursor-section')).toHaveCount(2);
        const tollSection = dialog.locator('.svp-cursor-section', { hasText: '__lastKey_toll' });
        await tollSection.locator('.el-form-item', { hasText: '__lastKey_toll' }).locator('input').fill('t10');
        await dialog.locator('.el-dialog__footer .el-button--primary').click();
        await expect(dialog).toBeHidden();

        await expect.poll(() => setPayloads.length).toBe(2);
        expect(setPayloads).toContainEqual({ packetCode: '101_commonData', partnerId: 'p_grp', cursorKey: 'pkt', lastTime: '2026-10-27 09:30:00.000', lastKey: 'a2' });
        expect(setPayloads).toContainEqual({ packetCode: '101_commonData', partnerId: 'p_grp', cursorKey: 'toll', lastTime: '2026-10-27 09:20:00.000', lastKey: 't10' });
    });
});
