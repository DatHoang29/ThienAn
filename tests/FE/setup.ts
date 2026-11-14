import type { BrowserContext, Page } from '@playwright/test';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { MOCK_TOKEN, DEFAULT_THEME_CONFIG, DEFAULT_MOCK_USER } from './constants';
import type { MockUserInfo, MockAuthOptions } from './interfaces';

export * from './constants';
export * from './interfaces';

const __setupFilename = fileURLToPath(import.meta.url);
const __setupDirname = path.dirname(__setupFilename);

/**
 * Trợ thủ chuyển đổi đường dẫn tới thư mục nguồn Frontend (TA-ITS015-WEBVUE-V1.0/src/src)
 * Giúp các file test không cần viết lặp lại chuỗi `../../../../TA-ITS015-WEBVUE-V1.0/src/src`
 */
export const resolveWebvuePath = (...subpaths: string[]) =>
    path.resolve(__setupDirname, '../../TA-ITS015-WEBVUE-V1.0/src/src', ...subpaths);

export const resolveWebVueSrc = resolveWebvuePath;


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
            body: JSON.stringify({ code: 200, result: {} }),
        });
    });

    await page.route('**/api/cfgsystem/sysconfigtype/alldatalist*', async (route) => {
        await route.fulfill({
            status: 200,
            contentType: 'application/json',
            body: JSON.stringify({ code: 200, result: {} }),
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

/**
 * Đọc căn lề thực tế (computed text-align) của các cột trong vxe-grid / vxe-table nằm trong `scope`.
 * Trả về { [tiêu đề cột]: '<căn tiêu đề>|<căn ô dòng đầu>' }, ví dụ { 'Mã': 'center|left' }.
 * Cột tìm theo tiêu đề hiển thị, ô thân ghép với tiêu đề qua thuộc tính colid của vxe.
 */
export async function readVxeColumnAligns(page: Page, scope: string): Promise<Record<string, string>> {
    return page.locator(scope).first().evaluate((root) => {
        const main = root.querySelector('.vxe-table--main-wrapper') ?? root;
        const out: Record<string, string> = {};
        for (const th of main.querySelectorAll('.vxe-header--column')) {
            const title = (th.querySelector('.vxe-cell--title') ?? th).textContent?.trim() ?? '';
            const td = main.querySelector(`.vxe-body--row .vxe-body--column[colid="${th.getAttribute('colid')}"] .vxe-cell`);
            const headerCell = th.querySelector('.vxe-cell') ?? th;
            out[title] = `${getComputedStyle(headerCell).textAlign}|${td ? getComputedStyle(td).textAlign : 'no-row'}`;
        }
        return out;
    });
}

/**
 * Như readVxeColumnAligns nhưng cho el-table: { [nhãn cột]: '<căn tiêu đề>|<căn ô dòng đầu>' }.
 * Ô thân ghép với tiêu đề qua class cột el-table_<n>_column_<m>.
 */
export async function readElTableColumnAligns(page: Page, scope: string): Promise<Record<string, string>> {
    return page.locator(scope).first().evaluate((root) => {
        const out: Record<string, string> = {};
        for (const th of root.querySelectorAll('.el-table__header th')) {
            const columnClass = [...th.classList].find((c) => /^el-table_\d+_column_\d+$/.test(c));
            const td = columnClass ? root.querySelector(`.el-table__body tr td.${columnClass} .cell`) : null;
            const headerCell = th.querySelector('.cell') ?? th;
            out[headerCell.textContent?.trim() ?? ''] = `${getComputedStyle(headerCell).textAlign}|${td ? getComputedStyle(td).textAlign : 'no-row'}`;
        }
        return out;
    });
}

/**
 * Kết quả mong đợi theo quy chuẩn: mọi tiêu đề căn giữa; cột chữ (mã, tên, mô tả…) căn trái;
 * cột trạng thái, thời gian, số, hành động căn giữa.
 */
export function expectedColumnAligns(left: string[], center: string[]): Record<string, string> {
    return Object.fromEntries([...left.map((t) => [t, 'center|left']), ...center.map((t) => [t, 'center|center'])]);
}
