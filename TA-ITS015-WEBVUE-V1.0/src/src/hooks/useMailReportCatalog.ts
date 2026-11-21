// Danh mục báo cáo gửi email (bảng RptEmailReport) và nhóm báo cáo (SysPlugin) — dùng chung cho:
//   - views/mail/baoCaoGuiMail  : màn CRUD danh mục (ô lọc / chọn nhóm, cột tên nhóm)
//   - views/toll/lichGuiMail    : lọc lịch theo báo cáo, ô chọn báo cáo trong form lịch (gom theo nhóm)
//
// Thay cho config SysConfigData loại `toll_report` (BaseConfigTypeEnum.TollReportList) — đã bỏ ở backend.
// Báo cáo đặt lịch được = dòng đang bật trong RptEmailReport VÀ đã có nhánh kết xuất ở backend; mã chưa
// hỗ trợ thì backend báo lỗi lúc lưu lịch.

import { getAPI } from '/@/utils/axios-utils';
import { RptEmailReportApi, RptEmailScheduleApi } from '/@/api-services/mail';
import type { MailReportOutput } from '/@/api-services/mail';
import { SysPluginApi } from '/@/api-services/system';
import type { PluginOutput } from '/@/api-services/system';
import { StatusTypeEnum } from '/@/types/enums';

export interface MailReportGroup {
	code: string;
	name: string;
	reports: MailReportOutput[];
}


let reportCache: MailReportOutput[] | null = null;
let reportInflight: Promise<MailReportOutput[]> | null = null;
let pluginCache: PluginOutput[] | null = null;
let pluginInflight: Promise<PluginOutput[]> | null = null;

async function loadMailReports(): Promise<MailReportOutput[]> {
	if (reportCache) return reportCache;
	if (reportInflight) return reportInflight;

	reportInflight = (async () => {
		try {
			const res = await getAPI(RptEmailReportApi).apiMailRptemailreportListGet();
			reportCache = res.data.result ?? [];
			return reportCache;
		} catch (error) {
			console.error('[Mail] Không tải được danh mục báo cáo gửi email:', error);
			return [];
		} finally {
			reportInflight = null;
		}
	})();

	return reportInflight;
}

async function loadMailReportPlugins(): Promise<PluginOutput[]> {
	if (pluginCache) return pluginCache;
	if (pluginInflight) return pluginInflight;

	pluginInflight = (async () => {
		try {
			const res = await getAPI(SysPluginApi).apiSystemSyspluginListGet();
			const seen = new Set<string>();
			pluginCache = (res.data.result ?? [])
				.filter((x) => !x.isDelete && !!x.code?.trim())
				// SysPlugin có thể trùng mã (bản ghi cũ) — giữ bản đầu tiên
				.filter((x) => {
					const code = x.code!.trim();
					if (seen.has(code)) return false;
					seen.add(code);
					return true;
				})
				.sort((a, b) => (a.name ?? a.code ?? '').localeCompare(b.name ?? b.code ?? '', 'vi'));
			return pluginCache;
		} catch (error) {
			console.error('[Mail] Không tải được danh sách nhóm báo cáo (SysPlugin):', error);
			return [];
		} finally {
			pluginInflight = null;
		}
	})();

	return pluginInflight;
}

function clearMailReportCache() {
	reportCache = null;
}

let addressCache: string[] | null = null;
let addressInflight: Promise<string[]> | null = null;

/**
 * Description: Sổ địa chỉ email (bảng RptEmailAddress — backend tự thêm khi lưu lịch) để gợi ý ở ô To / CC / BCC
 *              và ô lọc người nhận của nhật ký. Nạp một lần, lọc ở client.
 */
async function loadMailAddresses(): Promise<string[]> {
	if (addressCache) return addressCache;
	if (addressInflight) return addressInflight;

	addressInflight = (async () => {
		try {
			const res = await getAPI(RptEmailScheduleApi).apiMailRptemailscheduleAddressesGet();
			addressCache = res.data.result ?? [];
			return addressCache;
		} catch (error) {
			console.error('[Mail] Không tải được sổ địa chỉ email:', error);
			return [];
		} finally {
			addressInflight = null;
		}
	})();

	return addressInflight;
}

/** Description: Lưu lịch xong thì bỏ cache để lần sau gợi ý cả địa chỉ vừa thêm. */
function clearMailAddressCache() {
	addressCache = null;
}

/** Description: Gợi ý các địa chỉ chứa keyword (không phân biệt hoa thường), bỏ các địa chỉ trong exclude. */
function filterMailAddresses(book: string[], keyword?: string | null, exclude: string[] = [], limit = 8): string[] {
	const q = (keyword ?? '').trim().toLowerCase();
	const used = new Set(exclude.map((x) => x.toLowerCase()));
	return book.filter((x) => (!q || x.toLowerCase().includes(q)) && !used.has(x.toLowerCase())).slice(0, limit);
}

/** Description: Báo cáo đang bật (status rỗng coi là bật, khớp backend). */
const isActiveReport = (r: MailReportOutput) => (r.status as number | undefined) !== StatusTypeEnum.Disable || null;

/** Description: Tên báo cáo theo mã; mã không có trong danh mục thì hiện chính mã đó. */
const reportNameOf = (reports: MailReportOutput[], code?: string | null) =>
	(code && reports.find((r) => r.code === code)?.name) || code || '';

/** Description: Tên nhóm theo mã plugin. */
const pluginNameOf = (plugins: PluginOutput[], code?: string | null) =>
	(code && plugins.find((p) => p.code === code)?.name) || code || '';

/**
 * Description: Gom báo cáo theo nhóm cho el-option-group, giữ thứ tự OrderNo trong nhóm.
 *              Nhóm không có trong SysPlugin vẫn hiện (tên = mã) để không mất báo cáo.
 */
function groupReports(reports: MailReportOutput[], plugins: PluginOutput[]): MailReportGroup[] {
	const groups = new Map<string, MailReportGroup>();
	reports.forEach((r) => {
		const code = r.pluginCode ?? '';
		if (!groups.has(code)) groups.set(code, { code, name: pluginNameOf(plugins, code), reports: [] });
		groups.get(code)!.reports.push(r);
	});
	groups.forEach((g) => g.reports.sort((a, b) => (a.orderNo ?? 0) - (b.orderNo ?? 0)));
	return [...groups.values()].sort((a, b) => a.name.localeCompare(b.name, 'vi'));
}

export function useMailReportCatalog() {
	return {
		loadMailReports,
		loadMailReportPlugins,
		clearMailReportCache,
		loadMailAddresses,
		clearMailAddressCache,
		filterMailAddresses,
		isActiveReport,
		reportNameOf,
		pluginNameOf,
		groupReports,
	};
}
