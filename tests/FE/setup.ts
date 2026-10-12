import type { BrowserContext, Page } from '@playwright/test';
import { MOCK_TOKEN, DEFAULT_THEME_CONFIG, DEFAULT_MOCK_USER } from './constants';
import type { MockUserInfo, MockAuthOptions } from './interfaces';

export * from './constants';
export * from './interfaces';

/**
 * Thiết lập môi trường phiên người dùng và mock các API hệ thống nền tảng
 * Giúp trình duyệt bỏ qua bước đăng nhập (/login) và vào thẳng trang chức năng
 */
export async function mockAuth(
    context: BrowserContext,
    page: Page,
    options: MockAuthOptions | string = {}
) {
    const opts: MockAuthOptions = typeof options === 'string' ? { language: options } : options;
    const token = opts.token ?? MOCK_TOKEN;
    const rawLang = opts.language ?? 'vi-VN';
    const normalizedLang = rawLang.toLowerCase() === 'en-us' ? 'en-US' : 'vi-VN';
    const userInfo: MockUserInfo = {
        ...DEFAULT_MOCK_USER,
        ...(opts.userInfo ?? {}),
    };
    const baseURL = opts.baseURL ?? 'http://localhost:8888';

    // 1. Thêm cookie đăng nhập
    await context.addCookies([
        { name: 'token', value: token, url: baseURL },
    ]);

    // 2. Chạy init script trước khi webapp khởi tạo
    await page.addInitScript(
        ({ token, language, userInfo, defaultTheme }: { token: string; language: string; userInfo: any; defaultTheme: any }) => {
            document.cookie = `token=${token}; path=/;`;
            const pkg = 'tacwebcore';
            window.localStorage.setItem(`${pkg}:access-token`, JSON.stringify(token));
            window.sessionStorage.setItem(`${pkg}:token`, JSON.stringify(token));
            window.localStorage.setItem(
                `${pkg}:themeConfig`,
                JSON.stringify({
                    ...defaultTheme,
                    globalI18n: language,
                })
            );
            window.localStorage.setItem(`${pkg}:oldLayout`, JSON.stringify('defaults'));
            window.sessionStorage.setItem(`${pkg}:userInfo`, JSON.stringify(userInfo));
        },
        { token, language: normalizedLang, userInfo, defaultTheme: DEFAULT_THEME_CONFIG }
    );

    // 3. Mock file cấu hình NATS (tránh lỗi popup cấu hình NATS khi khởi tạo)
    await page.route('**/Configuration/Nats.config.json*', async (route) => {
        await route.fulfill({
            status: 200,
            contentType: 'application/json',
            body: JSON.stringify({ Nats: { Url: 'ws://127.0.0.1:4222' } }),
        });
    });

    // 4. Mock fallback cho toàn bộ /api/** (để các request không bị treo hay lọt ra server thật)
    await page.route('**/api/**', async (route) => {
        await route.fulfill({
            status: 200,
            contentType: 'application/json',
            body: JSON.stringify({ code: 200, result: [] }),
        });
    });

    // 5. Mock các API thông tin tài khoản và cấu hình hệ thống cốt lõi
    await page.route('**/api/system/sysuser/info*', async (route) => {
        await route.fulfill({
            status: 200,
            contentType: 'application/json',
            body: JSON.stringify({ code: 200, result: userInfo }),
        });
    });

    await page.route('**/api/system/sysauth/userinfo*', async (route) => {
        await route.fulfill({
            status: 200,
            contentType: 'application/json',
            body: JSON.stringify({ code: 200, result: userInfo }),
        });
    });

    await page.route('**/api/system/sysconfig/sysinfo*', async (route) => {
        await route.fulfill({
            status: 200,
            contentType: 'application/json',
            body: JSON.stringify({ code: 200, result: {} }),
        });
    });

    await page.route('**/api/cfgsystem/sysopconfig/alldatalist*', async (route) => {
        await route.fulfill({
            status: 200,
            contentType: 'application/json',
            body: JSON.stringify({ code: 200, result: [] }),
        });
    });

    await page.route('**/api/cfgsystem/sysconfigtype/alldatalist*', async (route) => {
        await route.fulfill({
            status: 200,
            contentType: 'application/json',
            body: JSON.stringify({ code: 200, result: [] }),
        });
    });

    await page.route('**/api/system/sysconfig/loadclientterm*', async (route) => {
        await route.fulfill({
            status: 200,
            contentType: 'application/json',
            body: JSON.stringify({ code: 200, result: {} }),
        });
    });
}

/**
 * Bí danh tương thích ngược cho mockAuth
 */
export const setupMockContext = mockAuth;
