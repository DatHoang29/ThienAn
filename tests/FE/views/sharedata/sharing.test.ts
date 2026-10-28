import { test, expect } from '@playwright/test';
import { mockAuth, resolveWebVueSrc } from '../../setup';
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
const sharingIndexPath = resolveWebVueSrc('views/sharedata/sharing/index.vue');
const editSubscriptionPath = resolveWebVueSrc('views/sharedata/sharing/component/editSubscription.vue');
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

	test('Bug — Chống 2 toast lỗi chồng lấn khi bật/tắt đăng ký và kết nối đối tác (sharing/index.vue)', async () => {
		const sharingContent = fs.readFileSync(sharingIndexPath, 'utf-8');

		// 1. handleToggle không gọi thêm ElMessage.error trong catch (axios-utils.ts đã hiển thị)
		const toggleFnMatch = sharingContent.match(/async function handleToggle[\s\S]*?\} catch[\s\S]*?\}/);
		expect(toggleFnMatch).not.toBeNull();
		expect(toggleFnMatch![0]).not.toMatch(/ElMessage\.error\(/);

		// 2. handleConnect không gọi thêm ElMessage.error trong catch
		const connectFnMatch = sharingContent.match(/async function handleConnect[\s\S]*?\} catch[\s\S]*?\}/);
		expect(connectFnMatch).not.toBeNull();
		expect(connectFnMatch![0]).not.toMatch(/ElMessage\.error\(/);
	});

	test('Bug — Bỏ khối UI cấu hình SQL trong dialog Sửa/Tạo đăng ký (editSubscription.vue) & Cascading Cleanup', async () => {
		const editSubContent = fs.readFileSync(editSubscriptionPath, 'utf-8');

		// 1. Template: Đã bỏ toàn bộ alert hiển thị SQL gói tin và link mở SQL Center
		expect(editSubContent).not.toContain('openSqlCenter');
		expect(editSubContent).not.toContain('sqlScopeTitle');
		expect(editSubContent).not.toContain('packetSqlConfigured');

		// 2. Script: Cascading cleanup toàn bộ dead state và methods
		expect(editSubContent).not.toContain('sqlScope:');
		expect(editSubContent).not.toContain('sqlLoading:');
		expect(editSubContent).not.toContain('resolveExtractSql');
		expect(editSubContent).not.toContain("from 'vue-router'");
	});

	test('Bug — Ô cổng (Port) không bị khoá cứng khi chỉnh sửa đối tác đã ngắt kết nối (editPartner.vue)', async () => {
		const partnerContent = fs.readFileSync(editPartnerPath, 'utf-8');

		// 1. Ô nhập cổng (Port) không bị khoá cứng bởi effectiveOperateType === 'edit'
		const portItemMatch = partnerContent.match(/<el-form-item[^>]*prop="port"[^>]*>([\s\S]*?)<\/el-form-item>/);
		expect(portItemMatch).not.toBeNull();
		expect(portItemMatch![1]).not.toContain("effectiveOperateType === 'edit'");
		expect(portItemMatch![1]).not.toContain('portLocked');

		// 2. Toàn bộ form dùng chung cơ chế :disabled="isLocked" khi đối tác đang kết nối hoặc có đăng ký đang chạy
		expect(partnerContent).toContain(':disabled="isLocked"');
	});

	test('TC — Quét toàn bộ tệp i18n của màn hình Chia sẻ dữ liệu, không có ký tự @ trần gây lỗi Invalid linked format', async () => {
		const viVn = JSON.parse(fs.readFileSync(viVnSharedataPath, 'utf-8'));
		const enUs = JSON.parse(fs.readFileSync(enUsSharedataPath, 'utf-8'));

		for (const [lang, dict] of [['vi-vn', viVn], ['en-us', enUs]] as const) {
			for (const [k, v] of Object.entries(dict)) {
				if (typeof v === 'string') {
					const stripped = v.replaceAll("{'@'}", '');
					expect(stripped, `${lang} ${k} có @ chưa thoát`).not.toContain('@');
				}
			}
		}
	});
});
