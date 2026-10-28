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
			keys.add(`lz.tooltip.sharedataSqlVariable.source_${s}`);
		}
		expect(sources).toEqual(['lastTime', 'lastKey', 'const', 'now']);

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
});
