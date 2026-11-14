import { test, expect, type Route } from '@playwright/test';
import { mockAuth, readVxeColumnAligns, expectedColumnAligns } from '../../setup';

/**
 * Test E2E cho phân hệ ShareData — trang Nguồn sự kiện (eventSource/index.vue):
 * nạp trang không vỡ, khung chính hiển thị, không lộ khóa dịch thô.
 */

/** Lỗi của khung layout dùng chung (layout/main/defaults.vue gọi layoutMainScrollbarRef khi chưa gắn) nằm ngoài ShareData: không tính vào lỗi trang. */
const isSharedLayoutError = (message: string) => message.includes('layoutMainScrollbarRef');

const fulfillJson = (route: Route, result: unknown) =>
    route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ code: 200, result }) });

test.describe('ShareData — Trang Nguồn sự kiện (eventSource) @sharedata', () => {
    test('1. Mở trang: thanh tìm kiếm và bảng hiển thị, không có lỗi trang, không lộ khóa dịch thô', async ({ context, page }) => {
        const pageErrors: string[] = [];
        page.on('pageerror', (err) => {
            if (!isSharedLayoutError(err.message)) pageErrors.push(err.message);
        });
        await mockAuth(context, page, { userInfo: { defaultMenu: '/sharedata/eventSource' } });
        await page.route('**/api/system/sysmenu/loginmenutree*', (route) =>
            fulfillJson(route, [
                {
                    id: 'menu_sharedata',
                    pid: '0',
                    path: '/sharedata',
                    name: 'sharedata',
                    component: 'layout/routerView/parent',
                    meta: { title: 'lz.menu.sharedata', isLink: '', isHide: false, isKeepAlive: true, isAffix: false, isIframe: false, roles: ['admin'], icon: 'ele-Share' },
                    children: [
                        {
                            id: 'menu_sharedata_event',
                            pid: 'menu_sharedata',
                            path: '/sharedata/eventSource',
                            name: 'sharedataEventSource',
                            component: 'sharedata/eventSource/index',
                            meta: { title: 'Nguồn sự kiện', isLink: '', isHide: false, isKeepAlive: true, isAffix: false, isIframe: false, roles: ['admin'], icon: 'ele-Bell' },
                        },
                    ],
                },
            ])
        );

        await page.setViewportSize({ width: 1600, height: 900 });
        await page.goto('/#/sharedata/eventSource');

        const pageRoot = page.locator('.esh-page');
        await expect(pageRoot).toBeVisible({ timeout: 30_000 });
        await expect(pageRoot.locator('.vxe-grid')).toBeVisible();
        await expect(pageRoot.locator('.esh-hint')).toContainText('Danh mục nguồn sự kiện');
        await expect(pageRoot.locator('.vxe-header--column', { hasText: 'lz.' })).toHaveCount(0);
        // Chờ bảng nạp xong rồi mới kiểm lỗi: lỗi của lần nạp bảng đầu tiên xảy ra sau khi khung trang hiện
        await page.waitForTimeout(1500);
        expect(pageErrors).toEqual([]);
    });

    test('2. Bảng 50 dòng cuộn tới đáy thấy đủ dòng 50 ở cả cột cố định STT và Thao tác', async ({ context, page }) => {
        test.slow();
        await mockAuth(context, page, { userInfo: { defaultMenu: '/sharedata/eventSource' } });
        await page.route('**/api/system/sysmenu/loginmenutree*', (route) =>
            fulfillJson(route, [
                {
                    id: 'menu_sharedata', pid: '0', path: '/sharedata', name: 'sharedata', component: 'layout/routerView/parent',
                    meta: { title: 'lz.menu.sharedata', isLink: '', isHide: false, isKeepAlive: true, isAffix: false, isIframe: false, roles: ['admin'], icon: 'ele-Share' },
                    children: [
                        {
                            id: 'menu_sharedata_event', pid: 'menu_sharedata', path: '/sharedata/eventSource', name: 'sharedataEventSource', component: 'sharedata/eventSource/index',
                            meta: { title: 'Nguồn sự kiện', isLink: '', isHide: false, isKeepAlive: true, isAffix: false, isIframe: false, roles: ['admin'], icon: 'ele-Bell' },
                        },
                    ],
                },
            ])
        );
        await page.setViewportSize({ width: 1280, height: 620 });
        await page.goto('/#/sharedata/eventSource');
        await expect(page.locator('.esh-page .vxe-grid')).toBeVisible({ timeout: 30_000 });

        /* The list API is not wired yet (handleQueryApi is a stub), so feed 50 rows through the grid's own loadData. */
        await page.evaluate(() => {
            const rows = Array.from({ length: 50 }, (_, i) => ({
                id: `ES_${i + 1}`, code: `EVT_XE_QUA_TRAM_${i + 1}`, name: `Sự kiện phương tiện qua trạm cửa khẩu Hữu Nghị số ${i + 1}`,
                subject: `sharedata.event.vehicle.pass.huunghi.${i + 1}`, datatypeCode: '101_commonData',
                description: `Phát sự kiện khi phương tiện qua làn ra vào cửa khẩu Hữu Nghị - Chi Lăng lần ${i + 1}`,
            }));
            let vm = (document.querySelector('.esh-page .vxe-grid') as any).__vueParentComponent;
            while (vm && typeof vm.proxy?.loadData !== 'function') vm = vm.parent;
            return vm.proxy.loadData(rows);
        });

        const body = page.locator('.vxe-table--main-wrapper .vxe-table--body-wrapper').first();
        await expect(body.locator('tr.vxe-body--row')).toHaveCount(50, { timeout: 30_000 });
        /* Wait for the search panel layout to settle before aiming the wheel. */
        await page.waitForTimeout(1000);
        const box = (await body.boundingBox())!;
        await page.mouse.move(box.x + box.width / 2, box.y + box.height / 2);
        for (let i = 0; i < 40; i++) await page.mouse.wheel(0, 400);
        await page.waitForTimeout(500);

        /* Check every body pane: main, fixed-left (STT) and fixed-right (Thao tác). */
        const panes = await page.evaluate(() =>
            [...document.querySelectorAll('.full-table .vxe-table--body-wrapper')].map((el) => {
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
        expect(panes.some((p) => p.pane.includes('fixed-'))).toBe(true);
        for (const p of panes) expect(p.hiddenPx, `dòng 50 bị che ở ${JSON.stringify(p)}`).toBeLessThanOrEqual(1);
    });

    test('3. Căn lề cột: tiêu đề giữa; Mã, Tên, Subject, Loại dữ liệu, Mô tả căn trái; STT, Thao tác căn giữa', async ({ context, page }) => {
        await mockAuth(context, page, { userInfo: { defaultMenu: '/sharedata/eventSource' } });
        await page.route('**/api/system/sysmenu/loginmenutree*', (route) =>
            fulfillJson(route, [
                {
                    id: 'menu_sharedata', pid: '0', path: '/sharedata', name: 'sharedata', component: 'layout/routerView/parent',
                    meta: { title: 'lz.menu.sharedata', isLink: '', isHide: false, isKeepAlive: true, isAffix: false, isIframe: false, roles: ['admin'], icon: 'ele-Share' },
                    children: [
                        {
                            id: 'menu_sharedata_event', pid: 'menu_sharedata', path: '/sharedata/eventSource', name: 'sharedataEventSource', component: 'sharedata/eventSource/index',
                            meta: { title: 'Nguồn sự kiện', isLink: '', isHide: false, isKeepAlive: true, isAffix: false, isIframe: false, roles: ['admin'], icon: 'ele-Bell' },
                        },
                    ],
                },
            ])
        );
        await page.setViewportSize({ width: 1600, height: 900 });
        await page.goto('/#/sharedata/eventSource');
        await expect(page.locator('.esh-page .vxe-grid')).toBeVisible({ timeout: 30_000 });
        await page.evaluate(() => {
            const rows = [
                { id: 'ES_1', code: 'EVT_XE_QUA_TRAM', name: 'Sự kiện phương tiện qua trạm', subject: 'sharedata.event.vehicle.pass', datatypeCode: '101_commonData', description: 'Phát khi xe qua làn' },
                { id: 'ES_2', code: 'E2', name: 'Ngắn', subject: 's.e', datatypeCode: '110_wpData', description: 'M' },
            ];
            let vm = (document.querySelector('.esh-page .vxe-grid') as any).__vueParentComponent;
            while (vm && typeof vm.proxy?.loadData !== 'function') vm = vm.parent;
            return vm.proxy.loadData(rows);
        });
        await expect(page.locator('.vxe-table--main-wrapper tr.vxe-body--row')).toHaveCount(2, { timeout: 30_000 });

        const actual = await readVxeColumnAligns(page, '.esh-page .vxe-grid');
        expect(actual).toEqual(expectedColumnAligns(['Mã', 'Tên sự kiện', 'Subject (NATS)', 'Gói tin', 'Mô tả'], ['STT', 'Thao tác']));
    });
});
