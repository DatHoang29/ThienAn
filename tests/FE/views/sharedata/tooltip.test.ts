import { test, expect, type BrowserContext, type Page } from '@playwright/test';
import { mockAuth } from '../../setup';

/**
 * Test E2E phân hệ ShareData — tooltip ngắn gọn, bề rộng theo tỷ lệ màn hình.
 * Mỗi ca mở một màn hoặc hộp thoại của ShareData, rê chuột lần lượt vào mọi chỗ có tooltip
 * (el-tooltip, el-popover mở khi rê chuột, ô bảng vxe / el-table bị cắt chữ) rồi đọc tooltip đang hiện:
 * - tooltip chữ cố định (khóa dịch) không quá 80 ký tự; tooltip hiện dữ liệu (giá trị ô, SQL, URL, thông báo lỗi) không giới hạn độ dài;
 * - bề rộng tooltip không quá 30% bề rộng màn hình và nằm trọn trong màn hình;
 * - chữ không tràn ngang trong tooltip (chuỗi liền dài như GUID, SQL, URL vẫn xuống dòng).
 * Chữ của title trên thẻ HTML (tooltip của trình duyệt) cũng không quá 80 ký tự.
 * Chạy ở 1920, 1366, 1024px; gom mọi chỗ sai vào một object để một lần chạy liệt kê đủ.
 */

const WIDTHS = [1920, 1366, 1024];
const MAX_TEXT = 80;
const MAX_WIDTH_RATIO = 0.3;
/**
 * Không phải tooltip của ShareData: ô chọn và ô ngày (bấm mới mở); thành phần dùng chung toàn ứng dụng là
 * hộp Thông tin sửa đổi (components/table/modifyRecord.vue) và nút Làm mới của thanh công cụ bảng (components/advanced/table-header-operation.vue).
 */
const NOT_SHAREDATA = '.el-select__wrapper, .el-date-editor, .el-text.cursor-default, .workspace-config-button';
const VISIBLE_DIALOG = '.el-overlay:not([style*="display: none"]) .el-dialog';

/** Chuỗi liền không có chỗ ngắt: tooltip dữ liệu phải tự xuống dòng thay vì tràn ngang. */
const LONG_TOKEN = `KT038_110WPDATA_IN_${'9F2A6C1E7B3D4E8FA1C25D6E7F8A9B0C'.repeat(3)}`;
const LONG_TEXT = `Không gửi được gói tin tới đối tác do máy chủ đối tác trả về lỗi xác thực, kiểm tra lại khóa truy cập rồi gửi lại ${LONG_TOKEN}`;
const LONG_NAME = 'Dữ liệu phương tiện qua làn trạm thu phí cửa khẩu Hữu Nghị - Chi Lăng gửi Cục Cảnh sát giao thông';

const AUTH_BUTTONS = [
    '*',
    'sharedataSharing:add', 'sharedataSharing:update', 'sharedataSharing:delete',
    'eshMapping:add', 'eshMapping:update', 'eshMapping:delete',
    'eshCanonical:add', 'eshCanonical:update', 'eshCanonical:delete', 'eshCanonical:page',
    'sharedataAlertLog:update',
];
const PAGES = [
    ['dataSource', 'sharedataDataSource', 'lz.menu.sharedataDataSource'],
    ['mapping', 'sharedataMapping', 'lz.menu.sharedataMapping'],
    ['codeSet', 'eshCanonical', 'lz.menu.sharedataCodeSet'],
    ['eventSource', 'sharedataEventSource', 'lz.menu.sharedataEventSource'],
    ['sharing', 'sharedataSharing', 'lz.menu.sharedataSharing'],
    ['history', 'sharedataHistory', 'lz.menu.sharedataHistory'],
    ['errorLog', 'sharedataErrorLog', 'lz.menu.sharedataErrorLog'],
] as const;
const menuMeta = (title: string) => ({ title, isLink: '', isHide: false, isKeepAlive: true, isAffix: false, isIframe: false, roles: ['admin'], icon: 'ele-Share' });
const MENU = [
    {
        id: 'menu_sharedata', pid: '0', path: '/sharedata', name: 'sharedata', component: 'layout/routerView/parent', meta: menuMeta('lz.menu.sharedata'),
        children: PAGES.map(([view, name, title]) => ({
            id: `menu_sharedata_${view}`, pid: 'menu_sharedata', path: `/sharedata/${view}`, name, component: `sharedata/${view}/index`, meta: menuMeta(title),
        })),
    },
];

const ok = (result: unknown) => ({ status: 200, contentType: 'application/json', body: JSON.stringify({ code: 200, result }) });

/** Đăng nhập giả lập đủ quyền, menu có đủ 7 màn ShareData. */
const signIn = async (context: BrowserContext, page: Page, view: (typeof PAGES)[number][0]) => {
    await mockAuth(context, page, { userInfo: { defaultMenu: `/sharedata/${view}`, authBtnList: AUTH_BUTTONS } });
    await page.route('**/api/system/sysmenu/loginmenutree*', (route) => route.fulfill(ok(MENU)));
};

/**
 * Một vùng cần quét: `scope` là vùng chứa; `data` là các chỗ có tooltip hiện dữ liệu hoặc danh sách (không giới hạn độ dài);
 * `mustShow` là các chỗ bắt buộc phải hiện tooltip khi rê chuột.
 */
type Area = { name: string; scope: string; data?: string; mustShow?: string[] };
type Trigger = { kind: 'static' | 'data'; via: 'el' | 'vxe' | 'table'; tags: string[] };
type Shot = { text: string; left: number; right: number; top: number; bottom: number; width: number; vw: number; vh: number; overflowX: boolean };

/** Đánh số mọi chỗ có tooltip trong vùng (thuộc tính data-tt) và đọc chữ title của các thẻ HTML. */
const markTriggers = ({ scope, data, tags, skip }: { scope: string; data: string; tags: string[]; skip: string }) => {
    document.querySelectorAll('[data-tt]').forEach((el) => el.removeAttribute('data-tt'));
    const root = document.querySelector(scope);
    const triggers: { kind: string; via: string; tags: string[] }[] = [];
    if (!root) return { triggers, titles: [] as { kind: string; text: string }[] };
    const shown = (el: Element) => {
        const r = el.getBoundingClientRect();
        return r.width > 0 && r.height > 0 && getComputedStyle(el).visibility !== 'hidden';
    };
    const add = (el: Element, kind: string, via: string) => {
        if (!shown(el) || el.hasAttribute('data-tt') || el.closest(skip)) return;
        el.setAttribute('data-tt', String(triggers.length));
        triggers.push({ kind, via, tags: tags.filter((t) => el.matches(t) || el.querySelector(t) !== null) });
    };
    root.querySelectorAll('.el-tooltip__trigger').forEach((el) => add(el, el.matches(data) ? 'data' : 'static', 'el'));
    root.querySelectorAll('td.col--ellipsis, th.col--ellipsis').forEach((td) => {
        const cell = td.querySelector('.vxe-tree-cell') ?? td.children[0];
        if (cell && cell.scrollWidth > cell.clientWidth) add(td, td.tagName === 'TH' ? 'static' : 'data', 'vxe');
    });
    root.querySelectorAll('.el-table__cell .cell.el-tooltip').forEach((cell) => {
        if (cell.scrollWidth > cell.clientWidth) add(cell, 'data', 'table');
    });
    const titles = [...root.querySelectorAll('[title]')]
        .filter((el) => shown(el) && (el.getAttribute('title') ?? '').trim() !== '')
        .map((el) => ({ kind: el.matches(data) ? 'data' : 'static', text: (el.getAttribute('title') ?? '').replace(/\s+/g, ' ').trim() }));
    return { triggers, titles };
};

/** Toạ độ giữa chỗ số `index` sau khi cuộn tới; null khi chỗ đó bị phần tử khác che. */
const pointAt = (index: number) => {
    const el = document.querySelector(`[data-tt="${index}"]`);
    if (!el) return null;
    el.scrollIntoView({ block: 'center', inline: 'nearest' });
    const r = el.getBoundingClientRect();
    const x = r.left + r.width / 2;
    const y = r.top + r.height / 2;
    const hit = document.elementFromPoint(x, y);
    return hit && (el === hit || el.contains(hit)) ? { x, y } : null;
};

/** Tooltip đang hiện của chỗ số `index`: el-tooltip / el-popover tìm theo aria-describedby, ô bảng tìm theo nội dung ô. */
const readShot = ({ index, via }: { index: number; via: string }) => {
    const trigger = document.querySelector(`[data-tt="${index}"]`);
    if (!trigger) return null;
    const flat = (s: string | null) => (s ?? '').replace(/\s+/g, ' ').trim();
    const shown = (el: Element) => {
        const cs = getComputedStyle(el);
        const r = el.getBoundingClientRect();
        return cs.display !== 'none' && cs.visibility !== 'hidden' && r.width > 0 && r.height > 0;
    };
    let popper: Element | null = null;
    if (via === 'el') {
        /* el-input, el-select chuyển aria-describedby xuống ô nhập bên trong */
        const owner = trigger.hasAttribute('aria-describedby') ? trigger : trigger.querySelector('input[aria-describedby], textarea[aria-describedby]');
        const id = owner?.getAttribute('aria-describedby');
        popper = id ? document.getElementById(id) : null;
    } else {
        const head = flat(trigger.textContent).slice(0, 20);
        const list = document.querySelectorAll(via === 'vxe' ? '.vxe-tooltip--wrapper.is--visible' : '.el-popper[role="tooltip"]');
        popper = [...list].find((p) => shown(p) && flat(p.textContent).includes(head)) ?? null;
    }
    if (!popper || !shown(popper)) return null;
    const r = popper.getBoundingClientRect();
    const parts = [popper, ...popper.querySelectorAll('*')].filter((n) => !n.closest('.el-popper__arrow, .vxe-tooltip--arrow'));
    const overflowX = parts.some((n) => n.scrollWidth > n.clientWidth + 1 && getComputedStyle(n).display !== 'inline');
    return {
        text: flat((popper as HTMLElement).innerText || popper.textContent),
        left: Math.round(r.left), right: Math.round(r.right), top: Math.round(r.top), bottom: Math.round(r.bottom), width: Math.round(r.width),
        vw: document.documentElement.clientWidth, vh: document.documentElement.clientHeight, overflowX,
    };
};

/** Rê chuột lần lượt vào từng chỗ có tooltip trong vùng, chờ tooltip đứng yên rồi đo. */
const sweep = async (page: Page, area: Area) => {
    const { triggers, titles } = await page.evaluate(markTriggers, { scope: area.scope, data: area.data ?? ':not(*)', tags: area.mustShow ?? [], skip: NOT_SHAREDATA });
    const shots: (Trigger & Shot)[] = [];
    for (const [index, trigger] of (triggers as Trigger[]).entries()) {
        const point = await page.evaluate(pointAt, index);
        if (!point) continue;
        await page.mouse.move(point.x, point.y);
        let shot: Shot | null = null;
        let previous = '';
        for (let tick = 0; tick < 12 && !shot; tick++) {
            await page.waitForTimeout(100);
            const now = await page.evaluate(readShot, { index, via: trigger.via });
            const key = JSON.stringify(now);
            if (now && key === previous) shot = now;
            previous = key;
        }
        await page.mouse.move(0, 0);
        if (shot) shots.push({ ...trigger, ...shot });
    }
    return { shots, titles };
};

/** Quét vùng ở mọi độ rộng màn hình, ghi các chỗ sai vào `found` (khóa = vùng + đầu câu tooltip). */
const audit = async (page: Page, area: Area, found: Record<string, string[]>) => {
    const note = (text: string, kind: string, problem: string) => {
        const key = `${area.name} › ${kind === 'data' ? '[dữ liệu] ' : ''}${text.slice(0, 60)}`;
        const list = (found[key] ??= []);
        if (!list.includes(problem)) list.push(problem);
    };
    for (const [round, width] of WIDTHS.entries()) {
        await page.setViewportSize({ width, height: 900 });
        await page.waitForTimeout(500);
        const { shots, titles } = await sweep(page, area);
        for (const s of shots) {
            const length = [...s.text].length;
            if (s.kind === 'static' && length > MAX_TEXT) note(s.text, s.kind, `dài ${length} ký tự`);
            if (s.width > s.vw * MAX_WIDTH_RATIO) note(s.text, s.kind, `${width}px: rộng ${s.width}px = ${Math.round((s.width / s.vw) * 100)}% màn hình`);
            if (s.left < -1 || s.top < -1 || s.right > s.vw + 1 || s.bottom > s.vh + 1) note(s.text, s.kind, `${width}px: lọt ra ngoài màn hình`);
            if (s.overflowX) note(s.text, s.kind, `${width}px: chữ tràn ngang`);
        }
        for (const t of titles) {
            if (t.kind === 'static' && [...t.text].length > MAX_TEXT) note(t.text, 'title', `title dài ${[...t.text].length} ký tự`);
        }
        if (round === 0) {
            const seen = new Set(shots.flatMap((s) => s.tags));
            for (const tag of area.mustShow ?? []) if (!seen.has(tag)) note(tag, 'static', 'rê chuột không thấy tooltip');
        }
    }
};

test.describe('ShareData — Tooltip ngắn gọn, bề rộng theo tỷ lệ màn hình @sharedata', () => {
    test.beforeEach(() => {
        test.setTimeout(420_000);
    });

    test('1. Cấu hình gói tin: bảng gói tin, khung soạn SQL, tham số, mốc đang chạy, so sánh đối tác và câu ghi chiều nhận', async ({ context, page }) => {
        const PACKET_CODE = '101_commonData';
        const CURSOR_SQL =
            "SELECT TOP (@pageSize) ID AS zoneId, CONVERT(varchar(23), COALESCE(UpdateTime, CreateTime), 121) + '_' + ID AS __lastKey FROM TmsZone " +
            "WHERE CONVERT(varchar(23), COALESCE(UpdateTime, CreateTime), 121) + '_' + ID > @lastKey ORDER BY __lastKey ASC";
        const partnerIdOf = (url: string) => new URL(url).searchParams.get('PartnerId') ?? new URL(url).searchParams.get('partnerId') ?? '';
        const partners = [
            { partnerId: 'p_a', partnerCode: 'CSGT', partnerName: 'Cục CSGT', isCustom: false },
            { partnerId: 'p_b', partnerCode: 'VDOT', partnerName: 'Sở GTVT', isCustom: false },
            { partnerId: 'p_c', partnerCode: 'RIENG', partnerName: 'Đối tác cấu hình riêng', isCustom: true },
        ];
        const commonFields = [{ key: 'zoneId', type: 'string', dbType: 'nvarchar(64)', label: 'Mã vùng', required: true }];
        /* SQL riêng của đối tác có thêm trường: bảng trường hiện ký hiệu so với mẫu chung và chú giải */
        const partnerFields = [...commonFields, { key: `zoneName_${LONG_TOKEN}`, type: 'string', dbType: 'nvarchar(256)', label: LONG_NAME, required: false }];
        await signIn(context, page, 'dataSource');
        await page.route('**/api/sharedata/sharedatapacket/page*', (r) =>
            r.fulfill(ok({
                records: [{ id: 'PKT_01', code: PACKET_CODE, name: LONG_NAME, status: 1, partnerCount: 12, partnerNames: Array.from({ length: 12 }, (_, i) => `Đối tác ${i + 1} ${LONG_NAME}`), writeSqlCount: 1 }],
                total: 1,
            }))
        );
        /* Gói đã có hồ sơ ánh xạ: nút Sửa / Xóa hiện lý do khóa */
        await page.route('**/api/sharedata/sharedatamapping/page*', (r) => r.fulfill(ok({ records: [{ id: 'MAP_01', datatypeId: 'PKT_01' }], total: 1 })));
        await page.route('**/api/sharedata/sharedatapacket/getsqldetail*', (r) => r.fulfill(ok(null)));
        await page.route('**/api/sharedata/sharedatapacket/getwritesqls*', (r) =>
            r.fulfill(ok({ packetCode: PACKET_CODE, writeSql: 'MERGE INTO TmsZone t USING @records s ON t.ID = s.zoneId WHEN NOT MATCHED THEN INSERT (ID) VALUES (s.zoneId);', affectedTables: ['TmsZone'], detectedTables: ['TmsZone'], keyFields: ['zoneId'] }))
        );
        await page.route('**/api/sharedata/sharedatapacket/parseextractcolumns*', (r) => r.fulfill(ok({ isValid: true, columns: ['zoneId'] })));
        await page.route('**/api/sharedata/sharedatapacket/getmetadatatables*', (r) => r.fulfill(ok([{ id: 'tbl-zone', tableName: 'TmsZone' }])));
        await page.route('**/api/sharedata/sharedatapacket/getmetadatafields*', (r) => r.fulfill(ok([{ columnName: 'ID', dataType: 'nvarchar' }])));
        await page.route('**/api/sharedata/sharedatasubscription/list*', (r) => r.fulfill(ok([])));
        await page.route('**/api/sharedata/sharedatapacket/getextractsql*', (r) => r.fulfill(ok({ extractSql: CURSOR_SQL, status: 1, fields: commonFields })));
        await page.route('**/api/sharedata/sharedatapacket/getpartnersqllist*', (r) => r.fulfill(ok(partners)));
        await page.route('**/api/sharedata/sharedatapacket/getpartnerextractsql*', (r) => {
            const partnerId = partnerIdOf(r.request().url());
            const isCustom = partnerId === 'p_c';
            return r.fulfill(ok({
                partnerId, packetCode: PACKET_CODE, standardSql: CURSOR_SQL, customSql: isCustom ? `${CURSOR_SQL} ` : null, isCustom, hasDifferences: isCustom,
                commonFields, fields: isCustom ? partnerFields : commonFields,
            }));
        });
        await page.route('**/api/sharedata/sharedatapacket/getsqlvariables*', (r) =>
            r.fulfill(ok({
                items: [
                    { code: 'pageSize', valueKind: 'Const', value: '100', dataType: 'Int' },
                    { code: 'lastKey', valueKind: 'Builtin', value: 'lastKey', dataType: 'String', savedValue: partnerIdOf(r.request().url()) ? `2026-10-27 10:05:00.000_${LONG_TOKEN}` : null, defaultValue: '2026-10-22 00:00:00.000_' },
                ],
            }))
        );

        await page.setViewportSize({ width: 1920, height: 900 });
        await page.goto('/#/sharedata/dataSource');
        await page.locator('.packet-col .vxe-body--row').first().click({ timeout: 60_000 });
        await page.locator('.sql-monaco-editor .monaco-editor').first().waitFor({ timeout: 60_000 });
        await expect(page.locator('.sd-cursor-block .el-table__body tr')).toHaveCount(1, { timeout: 15_000 });

        const found: Record<string, string[]> = {};
        await audit(page, {
            name: 'Cấu hình gói tin - chiều gửi',
            scope: '.layout-main',
            data: '.partner-count-tag, .partner-tab-jump, .svp-check-lead',
            mustShow: ['.partner-count-tag', '.vxe-body--row .el-button--danger', '.svp-check-lead', '.sd-cursor-reset-btn', '.svp-source-select'],
        }, found);

        await page.setViewportSize({ width: 1920, height: 900 });
        await page.locator('.compare-all-btn').click();
        await expect(page.locator('.compare-all-dialog .cad-table .cmp-st').first()).toBeVisible({ timeout: 15_000 });
        await audit(page, { name: 'So sánh tất cả đối tác', scope: '.compare-all-dialog', data: '.cmp-st', mustShow: ['.cad-table .cmp-st'] }, found);
        await page.locator('.compare-all-dialog .el-dialog__headerbtn').click();
        await expect(page.locator('.compare-all-dialog')).toBeHidden();

        await page.setViewportSize({ width: 1920, height: 900 });
        await page.locator('.partner-tab-jump').click();
        await page.locator('.partner-tab-jump-popper:visible .el-select-dropdown__item', { hasText: 'RIENG' }).first().click();
        await expect(page.locator('.fields-compare-summary .cmp-legend')).toHaveCount(4, { timeout: 15_000 });
        await audit(page, {
            name: 'Cấu hình gói tin - đối tác riêng',
            scope: '.layout-main',
            data: '.partner-count-tag, .partner-tab-jump, .svp-check-lead, .cmp-st',
            mustShow: ['.cmp-legend', '.cmp-cell-wrapper .cmp-st'],
        }, found);

        await page.locator('.direction-tabs .el-tabs__item').nth(1).click();
        await expect(page.locator('.write-sql-panel .wsp-header')).toBeVisible({ timeout: 15_000 });
        await audit(page, {
            name: 'Cấu hình gói tin - chiều nhận',
            scope: '.write-sql-panel',
            data: '.wsp-scope-select, .sql-editor-hint',
            mustShow: ['.wsp-title-group .wsp-hint', '.wsp-meta-label .wsp-hint', '.sql-editor-hint'],
        }, found);

        expect(found, 'tooltip quá dài, quá rộng hoặc tràn').toEqual({});
    });

    test('2. Ánh xạ dữ liệu: bảng hồ sơ đang dùng và xem trước dữ liệu gửi đi / nhận về', async ({ context, page }) => {
        await signIn(context, page, 'mapping');
        await page.route('**/api/sharedata/sharedatamapping/page*', (r) =>
            r.fulfill(ok({ records: [{ id: 'MAP_01', code: `MAP_${LONG_TOKEN}`, name: LONG_NAME, partnerId: 'PARTNER_01', datatypeId: 'PKT_01', direction: 0, isActive: true, mappedFieldCount: 2 }], total: 1 }))
        );
        await page.route('**/api/sharedata/sharedatamapping/list*', (r) => r.fulfill(ok([])));
        await page.route('**/api/sharedata/sharedatapartner/list*', (r) => r.fulfill(ok([{ id: 'PARTNER_01', code: 'DT01', name: 'Đối tác test' }])));
        await page.route('**/api/sharedata/sharedatapacket/list*', (r) => r.fulfill(ok([{ id: 'PKT_01', code: '101_commonData', name: 'Gói 101' }])));
        /* Hồ sơ đang được đăng ký chạy dùng: công tắc, nút Sửa / Xóa hiện lý do khóa */
        await page.route('**/api/sharedata/sharedatasubscription/list*', (r) => r.fulfill(ok([{ id: 'SUB_01', partnerId: 'PARTNER_01', datatypeId: 'PKT_01', direction: 0, state: 1 }])));
        await page.route('**/api/sharedata/sharedatacodeset/list*', (r) => r.fulfill(ok([])));
        await page.route('**/api/sharedata/sharedatamapping/byid*', (r) =>
            r.fulfill(ok({
                id: 'MAP_01', code: 'MAP01', name: 'Hồ sơ hai chiều', partnerId: 'PARTNER_01', datatypeId: 'PKT_01', direction: 2,
                partnerSchemaJson: JSON.stringify({ laneCode: 'KT038_110WPDATA_IN', plate: '29A-12345' }),
                targetShapeJson: JSON.stringify({ laneCode: { $field: 'LaneCode' }, plate: { $field: 'PlateNumber' } }),
            }))
        );
        await page.route('**/api/sharedata/sharedatapacket/getfields*', (r) =>
            r.fulfill(ok([
                { key: 'LaneCode', label: `Mã làn ${LONG_NAME}`, type: 'string' },
                { key: 'PlateNumber', label: 'Biển số', type: 'string' },
            ]))
        );
        await page.route('**/api/sharedata/sharedatapacket/sampledata*', (r) => r.fulfill(ok({ packetCode: '101_commonData', rows: [{ LaneCode: LONG_TOKEN, PlateNumber: '29A-12345' }] })));

        await page.setViewportSize({ width: 1920, height: 900 });
        await page.goto('/#/sharedata/mapping');
        await expect(page.locator('.full-table .vxe-body--row')).toHaveCount(1, { timeout: 60_000 });

        const found: Record<string, string[]> = {};
        await audit(page, { name: 'Ánh xạ dữ liệu', scope: '.layout-main', mustShow: ['.el-switch', '.el-button--danger'] }, found);

        await page.setViewportSize({ width: 1920, height: 900 });
        await page.locator('.vxe-body--row button.el-button--warning').first().click();
        const dialog = page.locator('.mp-dialog');
        await expect(dialog).toBeVisible();
        await dialog.getByRole('tab', { name: 'Dữ liệu gửi đi' }).click();
        await dialog.getByRole('button', { name: 'Lấy dữ liệu mẫu' }).click();
        await expect(dialog.locator('.send-preview .el-table__body tr')).toHaveCount(1);
        await audit(page, { name: 'Ánh xạ - dữ liệu gửi đi', scope: '.mp-dialog .send-preview', data: 'th .el-tooltip__trigger', mustShow: ['.mp-preview-key'] }, found);

        await dialog.getByRole('tab', { name: 'Dữ liệu nhận về' }).click();
        await dialog.getByRole('button', { name: 'Dùng JSON mẫu' }).click();
        await dialog.getByRole('button', { name: 'Phân tích dữ liệu nhận' }).click();
        await expect(dialog.locator('.receive-preview .el-table').nth(1).locator('.el-table__body tr')).toHaveCount(1);
        await audit(page, { name: 'Ánh xạ - dữ liệu nhận về', scope: '.mp-dialog .receive-preview', data: 'th .el-tooltip__trigger, .mp-preview-raw', mustShow: ['.mp-preview-key'] }, found);

        expect(found, 'tooltip quá dài, quá rộng hoặc tràn').toEqual({});
    });

    test('3. Bộ mã: bảng bộ mã và hộp sửa có dòng giá trị mặc định, giá trị khai trùng', async ({ context, page }) => {
        await signIn(context, page, 'codeSet');
        await page.route('**/api/sharedata/sharedatacodeset/page*', (r) =>
            r.fulfill(ok({ records: [{ id: 'CS_1', code: `CS_${LONG_TOKEN}`, name: LONG_NAME, scope: 1, valueCount: 2 }], total: 1 }))
        );
        await page.route('**/api/sharedata/sharedatacodeset/byid*', (r) =>
            r.fulfill(ok({
                id: 'CS_1', code: 'CS_TRANG_THAI', name: 'Bộ mã trạng thái', scope: 1, defaultSourceValue: '0', defaultPartnerValue: 'UNKNOWN',
                values: [
                    { sourceValue: '1', partnerValue: 'PASS', displayName: 'Qua trạm' },
                    { sourceValue: '1', partnerValue: 'PASS', displayName: 'Khai trùng' },
                ],
            }))
        );

        await page.setViewportSize({ width: 1920, height: 900 });
        await page.goto('/#/sharedata/codeSet');
        await expect(page.locator('.full-table .vxe-body--row')).toHaveCount(1, { timeout: 60_000 });

        const found: Record<string, string[]> = {};
        await audit(page, { name: 'Bộ mã', scope: '.layout-main', mustShow: ['.el-button--danger'] }, found);

        await page.setViewportSize({ width: 1920, height: 900 });
        await page.locator('.vxe-body--row button.el-button--primary').first().click();
        await expect(page.locator(`${VISIBLE_DIALOG} .cs-default-row`)).toBeVisible({ timeout: 15_000 });
        await audit(page, { name: 'Bộ mã - hộp sửa', scope: VISIBLE_DIALOG, mustShow: ['.el-tag--warning', '.cs-dup'] }, found);

        expect(found, 'tooltip quá dài, quá rộng hoặc tràn').toEqual({});
    });

    test('4. Nguồn sự kiện: bảng có mô tả và subject dài', async ({ context, page }) => {
        await signIn(context, page, 'eventSource');
        await page.setViewportSize({ width: 1920, height: 900 });
        await page.goto('/#/sharedata/eventSource');
        await expect(page.locator('.esh-page .vxe-grid')).toBeVisible({ timeout: 60_000 });
        /* Danh sách nguồn sự kiện chưa có API: nạp dòng qua loadData của bảng như trang làm */
        await page.evaluate(({ token, name }) => {
            const rows = [{ id: 'ES_1', code: `EVT_${token}`, name, subject: `sharedata.event.vehicle.pass.${token}`, datatypeCode: '101_commonData', description: `${name} ${token}` }];
            let vm = (document.querySelector('.esh-page .vxe-grid') as any).__vueParentComponent;
            while (vm && typeof vm.proxy?.loadData !== 'function') vm = vm.parent;
            return vm.proxy.loadData(rows);
        }, { token: LONG_TOKEN, name: LONG_NAME });
        await expect(page.locator('.vxe-table--main-wrapper tr.vxe-body--row')).toHaveCount(1, { timeout: 30_000 });

        const found: Record<string, string[]> = {};
        await audit(page, { name: 'Nguồn sự kiện', scope: '.layout-main', mustShow: ['td.col--ellipsis'] }, found);

        expect(found, 'tooltip quá dài, quá rộng hoặc tràn').toEqual({});
    });

    test('5. Cấu hình chia sẻ: danh sách đối tác, bảng đăng ký, hộp sửa đối tác và hộp tạo đăng ký', async ({ context, page }) => {
        await signIn(context, page, 'sharing');
        await page.route('**/api/sharedata/sharedatapartner/list*', (r) =>
            r.fulfill(ok([
                { id: 'P_API', code: 'CSGT01', name: `Cục Cảnh sát giao thông ${LONG_TOKEN}`, address: '10.10.1.25', port: 8443, connectionType: 1, endPointApiUrl: `/api/receive/${LONG_TOKEN}`, status: 1, sessionState: 1 },
                { id: 'P_FTP', code: 'FTP01', name: 'Đối tác FTPS', address: '10.10.1.26', port: 21, connectionType: 4, connectionConfig: JSON.stringify({ RemotePath: '/upload', Username: 'ftpuser' }), status: 1, sessionState: 0 },
            ]))
        );
        await page.route('**/api/sharedata/sharedatapacket/list*', (r) =>
            r.fulfill(ok([
                { id: 'PKT_101', code: '101_commonData', name: 'Dữ liệu giao thông 101' },
                { id: 'PKT_102', code: '102_vehicle', name: 'Dữ liệu phương tiện 102' },
                { id: 'PKT_103', code: '103_lane', name: 'Dữ liệu làn 103' },
            ]))
        );
        await page.route('**/api/sharedata/sharedatasession/list*', (r) => r.fulfill(ok([])));
        /* Đủ các trạng thái có lời nhắc: đang chạy, trùng đăng ký đang chạy, chưa có hồ sơ ánh xạ, hồ sơ đang tắt */
        await page.route('**/api/sharedata/sharedatasubscription/list*', (r) =>
            r.fulfill(ok([
                { id: 'SUB_RUN', partnerId: 'P_API', datatypeId: 'PKT_101', direction: 0, mode: 0, state: 1, mappingState: 1, mappingName: 'Ánh xạ gửi 101', schedule: { kind: 'continuous', intervalSeconds: 60 } },
                { id: 'SUB_DUP', partnerId: 'P_API', datatypeId: 'PKT_101', direction: 0, mode: 0, state: 2, mappingState: 1, mappingName: 'Ánh xạ gửi 101', schedule: { kind: 'continuous', intervalSeconds: 60 } },
                { id: 'SUB_NONE', partnerId: 'P_API', datatypeId: 'PKT_102', direction: 0, mode: 0, state: 2, mappingState: 0, schedule: { kind: 'continuous', intervalSeconds: 60 } },
                { id: 'SUB_OFF', partnerId: 'P_API', datatypeId: 'PKT_103', direction: 0, mode: 0, state: 2, mappingState: 2, mappingName: 'Ánh xạ đang tắt', schedule: { kind: 'continuous', intervalSeconds: 60 } },
            ]))
        );

        await page.setViewportSize({ width: 1920, height: 900 });
        await page.goto('/#/sharedata/sharing');
        await expect(page.locator('.sub-table .vxe-body--row')).toHaveCount(4, { timeout: 60_000 });

        const found: Record<string, string[]> = {};
        await audit(page, { name: 'Cấu hình chia sẻ', scope: '.layout-main', data: '.ptn-item__name', mustShow: ['.ptn-item__name', '.sub-table__mapping', '.el-switch'] }, found);

        await page.setViewportSize({ width: 1920, height: 900 });
        const partnerDialog = page.locator('.sharedata-edit-partner-container .el-dialog');
        await page.locator('.ptn-item').nth(1).getByRole('button', { name: 'Chỉnh sửa' }).click();
        await expect(partnerDialog).toBeVisible();
        await audit(page, { name: 'Sửa đối tác FTPS - thông tin chung', scope: '.sharedata-edit-partner-container .el-dialog', mustShow: ['.el-input.is-disabled'] }, found);
        await partnerDialog.getByRole('tab', { name: 'Kết nối' }).click();
        await expect(partnerDialog.locator('.channel-ftp')).toBeVisible();
        await audit(page, { name: 'Sửa đối tác FTPS - kết nối', scope: '.sharedata-edit-partner-container .el-dialog', mustShow: ['.label-help-icon'] }, found);
        await page.keyboard.press('Escape');
        await expect(partnerDialog).toBeHidden();

        await page.setViewportSize({ width: 1920, height: 900 });
        await page.locator('.ptn-item').first().getByRole('button', { name: 'Chỉnh sửa' }).click();
        await expect(partnerDialog).toBeVisible();
        await partnerDialog.getByRole('tab', { name: 'Kết nối' }).click();
        await expect(partnerDialog.locator('.full-endpoint-hint')).toBeVisible();
        await audit(page, { name: 'Sửa đối tác WebAPI - kết nối', scope: '.sharedata-edit-partner-container .el-dialog', data: '.full-endpoint-hint', mustShow: ['.full-endpoint-hint'] }, found);
        await page.keyboard.press('Escape');
        await expect(partnerDialog).toBeHidden();

        await page.setViewportSize({ width: 1920, height: 900 });
        await page.locator('.sub-table .vxe-toolbar .el-button--primary').first().click();
        await expect(page.locator(`${VISIBLE_DIALOG} .lbl-help`)).toBeVisible({ timeout: 15_000 });
        await audit(page, { name: 'Tạo đăng ký gửi', scope: VISIBLE_DIALOG, mustShow: ['.lbl-help'] }, found);

        expect(found, 'tooltip quá dài, quá rộng hoặc tràn').toEqual({});
    });

    test('6. Lịch sử chia sẻ: bảng lượt truyền nhận và hộp chi tiết có bước lỗi', async ({ context, page }) => {
        const logId = 'LOG_FAIL_01';
        await signIn(context, page, 'history');
        await page.route('**/api/sharedata/sharedataactivitylog/summary*', (r) => r.fulfill(ok({ total: 1, successCount: 0, failedCount: 1, sentCount: 1, receivedCount: 0 })));
        await page.route('**/api/sharedata/sharedatapacket/list*', (r) => r.fulfill(ok([{ id: 'PKT_01', name: LONG_NAME }])));
        await page.route('**/api/sharedata/sharedatapartner/list*', (r) => r.fulfill(ok([{ id: 'PARTNER_01', name: 'Đối tác Cục CSGT' }])));
        await page.route('**/api/sharedata/sharedataactivitylog/page*', (r) =>
            r.fulfill(ok({
                records: [{ id: logId, logType: 1, transferType: 0, success: 0, packetId: 'PKT_01', partnerId: 'PARTNER_01', occurredAt: '2026-11-13T08:00:00Z', description: LONG_TEXT, errorMessage: LONG_TEXT }],
                total: 1,
            }))
        );
        await page.route('**/api/sharedata/sharedataactivitylog/steps*', (r) =>
            r.fulfill(ok([{ id: logId, children: [{ id: 'STEP_1', parentId: logId, stepNo: 1, stepNbr: 1, success: 0, description: 'Trích xuất dữ liệu thất bại', errorMessage: LONG_TEXT }] }]))
        );

        await page.setViewportSize({ width: 1920, height: 900 });
        await page.goto('/#/sharedata/history');
        await expect(page.locator('.full-table .vxe-body--row').first()).toBeVisible({ timeout: 60_000 });

        const found: Record<string, string[]> = {};
        await audit(page, { name: 'Lịch sử chia sẻ', scope: '.layout-main', mustShow: ['td.col--ellipsis'] }, found);

        await page.setViewportSize({ width: 1920, height: 900 });
        await page.locator('button:has-text("Chi tiết")').first().click();
        await expect(page.locator(`${VISIBLE_DIALOG} .steps-card .el-step`).first()).toBeVisible({ timeout: 15_000 });
        await audit(page, { name: 'Lịch sử - chi tiết lượt', scope: VISIBLE_DIALOG, data: '.step-title-wrap', mustShow: ['.copy-err-btn', '.step-title-wrap'] }, found);

        expect(found, 'tooltip quá dài, quá rộng hoặc tràn').toEqual({});
    });

    test('7. Nhật ký cảnh báo lỗi: bảng có nội dung lỗi dài và cảnh báo đã xác nhận', async ({ context, page }) => {
        await signIn(context, page, 'errorLog');
        await page.route('**/api/sharedata/sharedatapartner/list*', (r) => r.fulfill(ok([{ id: 'PARTNER_01', code: 'KT038', name: 'Trạm KT038' }])));
        await page.route('**/api/sharedata/sharedataalertlog/summary*', (r) => r.fulfill(ok({ unackedCount: 1, totalCount: 2 })));
        await page.route('**/api/sharedata/sharedataalertlog/page*', (r) =>
            r.fulfill(ok({
                records: [
                    { id: 'ALERT_1', alertCode: 'ESH-1201', alertSource: 1, severity: 2, partnerId: 'PARTNER_01', datatypeId: LONG_TOKEN, message: LONG_TEXT, occurredAt: '2026-11-13T08:00:00Z', acknowledged: false },
                    { id: 'ALERT_2', alertCode: 'ESH-1201', alertSource: 1, severity: 2, partnerId: 'PARTNER_01', datatypeId: '101', message: 'CodeSet CS_STATUS not found', occurredAt: '2026-11-13T07:00:00Z', acknowledged: true, ackBy: 'Quản trị viên hệ thống', ackAt: '2026-11-13T08:30:00Z' },
                ],
                total: 2,
            }))
        );

        await page.setViewportSize({ width: 1920, height: 900 });
        await page.goto('/#/sharedata/errorLog');
        await expect(page.locator('.full-table .vxe-table--main-wrapper .vxe-body--row')).toHaveCount(2, { timeout: 60_000 });

        const found: Record<string, string[]> = {};
        await audit(page, { name: 'Nhật ký cảnh báo lỗi', scope: '.layout-main', data: '.el-tag--success', mustShow: ['td.col--ellipsis', '.el-tag--success'] }, found);

        expect(found, 'tooltip quá dài, quá rộng hoặc tràn').toEqual({});
    });
});
