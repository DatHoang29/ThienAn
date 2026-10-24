import { test, expect } from '@playwright/test';
import fs from 'node:fs';
import { resolveWebVueSrc } from '../../setup';

const panelPath = resolveWebVueSrc('views/sharedata/dataSource/component/sqlVariablePanel.vue');
const viVnPath = resolveWebVueSrc('i18n/lang/its/sharedata/vi-vn.json');
const enUsPath = resolveWebVueSrc('i18n/lang/its/sharedata/en-us.json');

/**
 * Description: Bảng tham số câu truy vấn — khóa dịch dùng trong component phải có đủ ở vi-vn và en-us, giá trị dịch không có @ trần
 *              (vue-i18n hiểu @ là cú pháp liên kết, báo lỗi biên dịch lúc hiển thị). Danh sách kiểu và nguồn "Lấy từ" đọc từ chính
 *              component nên thêm kiểu / nguồn mới là tự được kiểm.
 * Created date: 24/10/2026
 * Modified date: 24/10/2026 — bỏ các ca chỉ đọc lại mã nguồn rồi so chuỗi (không chứng minh tính năng chạy đúng), giữ ca khóa dịch
 */
test.describe('ShareData — Bảng tham số câu truy vấn (sqlVariablePanel) @sharedata', () => {
	test('TC1 — Mọi khóa dịch của component có đủ ở vi-vn và en-us, không có @ chưa thoát', () => {
		const panel = fs.readFileSync(panelPath, 'utf-8');
		const viVn = JSON.parse(fs.readFileSync(viVnPath, 'utf-8'));
		const enUs = JSON.parse(fs.readFileSync(enUsPath, 'utf-8'));

		// Khóa viết thẳng trong component
		const keys = new Set<string>([...panel.matchAll(/'(lz\.[a-z]+\.sharedataSqlVariable\.[A-Za-z_]+)'/g)].map((m) => m[1]));
		// Khóa ghép theo kiểu dữ liệu (const TYPE_X = 'Y') và theo nguồn "Lấy từ" (khóa của BUILTIN_TYPES + Const)
		const types = [...panel.matchAll(/const TYPE_[A-Z]+ = '(\w+)';/g)].map((m) => m[1]);
		const builtinBlock = panel.match(/const BUILTIN_TYPES[^{]*\{([\s\S]*?)\};/)?.[1] ?? '';
		const sources = [...builtinBlock.matchAll(/(\w+):/g)].map((m) => m[1]).concat('Const');
		for (const t of types) keys.add(`lz.label.sharedataSqlVariable.type${t}`);
		for (const s of sources) {
			keys.add(`lz.label.sharedataSqlVariable.source_${s}`);
			keys.add(`lz.tooltip.sharedataSqlVariable.source_${s}`);
		}
		expect(types.length).toBeGreaterThanOrEqual(6);
		expect(sources.length).toBeGreaterThanOrEqual(8);

		for (const k of keys) {
			expect(viVn[k], `vi-vn.json thiếu key ${k}`).toBeTruthy();
			expect(enUs[k], `en-us.json thiếu key ${k}`).toBeTruthy();
			for (const [lang, value] of [['vi-vn', viVn[k]], ['en-us', enUs[k]]]) {
				expect(String(value).replaceAll("{'@'}", ''), `${lang} ${k} có @ chưa thoát`).not.toContain('@');
			}
		}
	});
});
