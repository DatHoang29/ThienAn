import { test, expect } from '@playwright/test';
import { resolveWebVueSrc } from '../../setup';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

/**
 * Test Suite kiểm chứng các lỗi giao diện và logic tại phân hệ ShareData (trang sharing):
 * 5. Ngôn ngữ thuần Việt cho gợi ý: "Chiều gửi", "Chiều nhận" (không lẫn Inbound / Kênh hiện tại).
 * 9. Logic EndPointApiUrl (/api/receive): FE tự động trim + prefix '/' và BE validator chấp nhận.
 * 12. Slot #row_datatype và packetNameOf chống hiển thị Snowflake ID thô.
 */

const editPartnerPath = resolveWebVueSrc('views/sharedata/sharing/component/editPartner.vue');
const viVnPath = resolveWebVueSrc('i18n/lang/vi-vn.json');
const viVnSharedataPath = resolveWebVueSrc('i18n/lang/its/sharedata/vi-vn.json');
const enUsPath = resolveWebVueSrc('i18n/lang/en-us.json');
const enUsSharedataPath = resolveWebVueSrc('i18n/lang/its/sharedata/en-us.json');
const readMergedViVn = () => ({
	...JSON.parse(fs.readFileSync(viVnPath, 'utf-8')),
	...JSON.parse(fs.readFileSync(viVnSharedataPath, 'utf-8')),
});
const readMergedEnUs = () => ({
	...JSON.parse(fs.readFileSync(enUsPath, 'utf-8')),
	...JSON.parse(fs.readFileSync(enUsSharedataPath, 'utf-8')),
});
const subscriptionTablePath = resolveWebVueSrc('views/sharedata/sharing/component/subscriptionTable.vue');
const partnerValidatorPath = path.resolve(__dirname, '../../../../TA-ITS015-WEBAPI-V1.0/src/Modules/ShareData/Module.ShareData/Controllers/Partner/Validators/PartnerValidator.cs');

test.describe('ShareData — Kiểm thử lỗi giao diện và logic (Sharing)', () => {
	test('Bug 5 — Nhãn gợi ý nhóm cấu hình thuần Việt và thuần Anh (Rule 20.13: Cấm song ngữ lai)', async () => {
		const viVn = readMergedViVn();
		const enUs = readMergedEnUs();

		// vi-vn.json: Thuần Việt, không chứa "(Kênh hiện tại)" hay "(Inbound)"
		expect(viVn['lz.label.sharedataConnectionEditor.groupOutbound']).toBe('Chiều gửi');
		expect(viVn['lz.label.sharedataConnectionEditor.groupInbound']).toBe('Chiều nhận');

		// en-us.json: Thuần Anh
		expect(enUs['lz.label.sharedataConnectionEditor.groupOutbound']).toBe('Outbound');
		expect(enUs['lz.label.sharedataConnectionEditor.groupInbound']).toBe('Inbound');
	});

	test('Bug 9 — Logic EndPointApiUrl (/api/receive): FE tự động trim + prefix "/" và BE Validator chấp nhận', async () => {
		const feContent = fs.readFileSync(editPartnerPath, 'utf-8');
		// FE: Có bước chuẩn hóa và tự động thêm "/" nếu là relative path
		expect(feContent).toContain('payload.endPointApiUrl');
		expect(feContent).toContain("ep = '/' + ep");

		// BE: PartnerValidator.cs chấp nhận relative path bắt đầu bằng "/" hoặc absolute URL
		if (fs.existsSync(partnerValidatorPath)) {
			const beContent = fs.readFileSync(partnerValidatorPath, 'utf-8');
			expect(beContent).toContain('EndPointApiUrl');
			expect(beContent).toMatch(/v!\.Trim\(\)\.StartsWith\("\/"\)/);
		}
	});

	test('Bug 12 — Reactive slot #row_datatype và packetNameOf chống hiển thị Snowflake ID thô', async () => {
		const labelHelperPath = resolveWebVueSrc('utils/sharedata/labelHelper.ts');
		const helperContent = fs.readFileSync(labelHelperPath, 'utf-8');
		const subTableContent = fs.readFileSync(subscriptionTablePath, 'utf-8');

		// 1. labelHelper.ts: xử lý an toàn ép kiểu String(id).trim() và fallback khi list rỗng
		expect(helperContent).toContain('String(id).trim()');
		expect(helperContent).toContain('!Array.isArray(list) || list.length === 0');

		// 2. subscriptionTable.vue: có template slot #row_datatype để Vue Reactivity tự re-render khi packetList nạp xong
		expect(subTableContent).toContain('<template #row_datatype="{ row }">');
		expect(subTableContent).toContain("slots: { default: 'row_datatype' }");

		// 3. subscriptionTable.vue: có watcher theo dõi packetList để cập nhật options.data
		expect(subTableContent).toContain('watch(packetList, () => {');
	});
});
