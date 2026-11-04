import { test, expect } from '@playwright/test';
import fs from 'node:fs';
import { resolveWebVueSrc } from '../../setup';

const panelPath = resolveWebVueSrc('views/sharedata/dataSource/component/sqlVariablePanel.vue');
const viVnPath = resolveWebVueSrc('i18n/lang/its/sharedata/vi-vn.json');
const enUsPath = resolveWebVueSrc('i18n/lang/its/sharedata/en-us.json');

/**
 * Description: Bảng tham số câu truy vấn — khóa dịch dùng trong component phải có đủ ở vi-vn và en-us, giá trị dịch không có @ trần
 *              (vue-i18n hiểu @ là cú pháp liên kết, báo lỗi biên dịch lúc hiển thị làm trắng màn hình chi tiết). Danh sách kiểu và nguồn "Lấy từ" đọc từ chính
 *              component nên thêm kiểu / nguồn mới là tự được kiểm.
 * Created date: 24/10/2026
 * Modified date: 28/10/2026
 */
test.describe('ShareData — Bảng tham số câu truy vấn (sqlVariablePanel) @sharedata', () => {
	test('TC1 — Mọi khóa dịch của component có đủ ở vi-vn và en-us, không có @ chưa thoát', () => {
		const panel = fs.readFileSync(panelPath, 'utf-8');
		const viVn = JSON.parse(fs.readFileSync(viVnPath, 'utf-8'));
		const enUs = JSON.parse(fs.readFileSync(enUsPath, 'utf-8'));

		// Khóa viết thẳng trong component (bao gồm cả các khóa chứa số như guideStep1Title)
		const keys = new Set<string>([...panel.matchAll(/'(lz\.[a-z]+\.sharedataSqlVariable\.[A-Za-z0-9_]+)'/g)].map((m) => m[1]));
		// Khóa ghép theo danh sách nguồn giá trị (const SOURCES = [...])
		const sourcesBlock = panel.match(/const SOURCES = \[([^\]]+)\];/)?.[1] ?? '';
		const sources = [...sourcesBlock.matchAll(/'(\w+)'/g)].map((m) => m[1]);
		for (const s of sources) {
			keys.add(`lz.label.sharedataSqlVariable.source_${s}`);
		}
		expect(sources).toEqual(['lastKey', 'const']);

		for (const k of keys) {
			expect(viVn[k], `vi-vn.json thiếu key ${k}`).toBeTruthy();
			expect(enUs[k], `en-us.json thiếu key ${k}`).toBeTruthy();
			for (const [lang, value] of [['vi-vn', viVn[k]], ['en-us', enUs[k]]]) {
				expect(String(value).replaceAll("{'@'}", ''), `${lang} ${k} có @ chưa thoát`).not.toContain('@');
			}
		}
	});

	test('TC2 — Toàn bộ tệp ngôn ngữ vi-vn và en-us của sharedata không chứa ký tự @ trần gây lỗi Invalid linked format làm trắng màn hình', () => {
		const viVn = JSON.parse(fs.readFileSync(viVnPath, 'utf-8'));
		const enUs = JSON.parse(fs.readFileSync(enUsPath, 'utf-8'));

		for (const [lang, dict] of [['vi-vn', viVn], ['en-us', enUs]] as const) {
			for (const [key, value] of Object.entries(dict)) {
				if (typeof value === 'string') {
					const stripped = value.replaceAll("{'@'}", '');
					expect(stripped, `${lang}.json khóa "${key}" chứa ký tự @ trần chưa thoát ({'@'}), gây lỗi biên dịch vue-i18n`).not.toContain('@');
				}
			}
		}
	});

	test('TC3 — Thiết kế bảng tham số phẳng: Lỗi đưa vào placeholder + viền đỏ + tooltip hover, không làm phình chiều cao dòng', () => {
		const panel = fs.readFileSync(panelPath, 'utf-8');

		// 1. Cột Nguồn giá trị: el-select chọn nguồn có icon prefix và mô tả trực quan trong popper
		expect(panel).toContain('class="svp-source-select"');
		expect(panel).toContain('popper-class="svp-source-popper"');
		expect(panel).toContain('class="svp-source-prefix"');

		// 2. Cột Giá trị cố định (el-input): dùng placeholder lỗi kết hợp el-tooltip, không có div svp-error bên dưới
		expect(panel).toContain(':placeholder="shownValueError(row) ||');
		expect(panel).toContain(':content="shownValueError(row)"');
		expect(panel).not.toContain('<div v-if="shownValueError(row)" class="svp-error">');

		// 3. Style CSS: đổi màu placeholder thành đỏ danger khi có class is-error
		expect(panel).toContain('.is-error :deep(.el-input__inner::placeholder)');
		expect(panel).toContain('color: var(--el-color-danger);');
	});

	test('TC4 — Quy chuẩn SQL nối mốc: Bỏ dải chip bên ngoài, toàn bộ đưa vào popup, chỉ dùng xanh lá hoặc đỏ (không màu cam)', () => {
		const panel = fs.readFileSync(panelPath, 'utf-8');

		// 1. Dải chip bên ngoài (Hình 2: svp-check-chips và svp-check-divider) đã bị loại bỏ khỏi template
		const template = panel.split('<script')[0] ?? '';
		expect(template).not.toContain('class="svp-check-chips"');
		expect(template).not.toContain('class="svp-check-divider"');

		// 2. Toàn bộ tiêu chí nằm trong popup (svp-popover-list qua cursorChecks)
		expect(template).toContain('class="svp-popover-list"');
		expect(template).toContain('v-for="check in cursorChecks"');
		expect(template).toContain("check.pass ? 'is-pass' : 'is-fail'");
		expect(panel).toContain('pass: checkCursorColumns.value');
		expect(panel).toContain('pass: checkCursorFilter.value');
		expect(panel).toContain('pass: checkOrderAsc.value');

		// 3. Biểu tượng đạt/chưa đạt chỉ dùng CircleCheckFilled và CircleCloseFilled
		expect(template).toContain('<ele-CircleCheckFilled v-if="check.pass" />');
		expect(template).toContain('<ele-CircleCloseFilled v-else />');

		// 4. Style lead và popup item: chỉ dùng xanh lá (is-pass) và đỏ (is-fail), không dùng màu cam #e6a23c
		const style = panel.split('<style')[1] ?? '';
		expect(style).not.toContain('#e6a23c');
		expect(style).toContain('&.is-pass {\n\t\t\tcolor: var(--el-color-success);');
		expect(style).toContain('&.is-fail,\n\t\t&.is-warn {\n\t\t\tcolor: var(--el-color-danger);');

		// 5. Khung ghi chú rà soát bên dưới (svp-popover-note / checkHint) và dải inline banner trùng lặp đã bị loại bỏ khỏi template và style
		expect(template).not.toContain('class="svp-popover-note"');
		expect(template).not.toContain('checkHint');
		expect(style).not.toContain('.svp-popover-note');
		expect(template).not.toContain('class="svp-inline-banner"');
		expect(template).not.toContain('class="svp-source-col-tag"');

		// 6. Cột Nguồn giá trị: el-select giữ trên 1 dòng (white-space: nowrap) và độ rộng tối thiểu đủ hiển thị trọn vẹn nhãn
		expect(template).toContain('min-width="230"');
		expect(style).toContain('.svp-source-select {');
		expect(style).toContain('white-space: nowrap;');
	});
});
