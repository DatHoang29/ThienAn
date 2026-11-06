import { test, expect, type Route } from '@playwright/test';
import { mockAuth } from '../../setup';

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
});
