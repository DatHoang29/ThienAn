using Modules.Mail.Core.Constants;
using Modules.Mail.Core.Dto.Report;

namespace Modules.Mail.Infrastructure.Services.Mail.Report
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Danh mục tổng các báo cáo ĐÃ CÓ NHÁNH KẾT XUẤT trong code — ghép từ {Phân hệ}Reports.List.
    ///
    ///              Một báo cáo đặt lịch được khi có trong CẢ HAI: dòng đang bật ở bảng RptEmailReport và khai ở
    ///              {Phân hệ}Reports của đúng PluginCode của dòng đó. Mã báo cáo phải DUY NHẤT trên toàn hệ
    ///              (RptEmailScheduleItem chỉ lưu mã) — trùng thì ném lỗi ngay khi nạp class.
    /// Created date: 14/08/2026
    /// </summary>
    public static class RptReportCode
    {
        /// <summary>Thêm phân hệ: thêm ..{Phân hệ}Reports.List (và một nhánh trong ReportExporter.BuildInput).</summary>
        public static readonly IReadOnlyList<MailRptReportInfo> Catalog =
        [
            ..TollReports.List,
            // ..WpReports.List,
        ];

        private static readonly Dictionary<string, MailRptReportInfo> ByCode = BuildIndex();

        public static MailRptReportInfo? Find(string? reportCode)
            => !string.IsNullOrWhiteSpace(reportCode) && ByCode.TryGetValue(reportCode.Trim(), out var info) ? info : null;

        /// <summary>Mã đã có nhánh kết xuất VÀ thuộc đúng phân hệ <paramref name="pluginCode"/> (RptEmailReport.PluginCode).</summary>
        public static bool IsSupported(string? pluginCode, string? reportCode)
            => Find(reportCode) is { } info
               && string.Equals(info.Module, pluginCode?.Trim(), StringComparison.Ordinal);

        /// <summary>Kỳ cố định của báo cáo; null khi báo cáo theo tần suất lịch hoặc mã chưa hỗ trợ.</summary>
        public static string? FixedPeriodOf(string? reportCode) => Find(reportCode)?.PeriodType;

        /// <summary>
        /// Loại kỳ của một báo cáo trong lịch có tần suất <paramref name="frequency"/>:
        ///   - báo cáo có kỳ cố định → kỳ đó, kể cả khi lệch tần suất (không chặn, form chỉ cảnh báo);
        ///   - báo cáo nhận khoảng ngày → kỳ = tần suất lịch (giá trị lạ / rỗng thì tháng);
        ///   - mã chưa hỗ trợ → <paramref name="fallback"/> (cột PeriodType đã lưu), rỗng nữa thì tháng.
        /// </summary>
        public static string ResolvePeriodType(string? reportCode, string? frequency, string? fallback = null)
        {
            var info = Find(reportCode);
            if (info == null)
                return string.IsNullOrWhiteSpace(fallback) ? MailRptPeriodType.Month : fallback;

            if (info.PeriodType != null) return info.PeriodType;

            var f = frequency?.Trim().ToLowerInvariant();
            return !string.IsNullOrEmpty(f) && MailRptPeriodType.All.Contains(f) ? f : MailRptPeriodType.Month;
        }

        /// <summary>Tên tệp rút gọn từ mã: "1.3_DTTT" → "DTTT".</summary>
        public static string ToFileName(string? reportCode)
        {
            if (string.IsNullOrWhiteSpace(reportCode)) return "BaoCao";

            var idx = reportCode.IndexOf('_');
            var token = idx >= 0 && idx < reportCode.Length - 1 ? reportCode[(idx + 1)..] : reportCode;

            return token.Replace('.', '_');
        }

        private static Dictionary<string, MailRptReportInfo> BuildIndex()
        {
            var map = new Dictionary<string, MailRptReportInfo>(StringComparer.Ordinal);
            foreach (var info in Catalog)
            {
                if (!map.TryAdd(info.Code, info))
                    throw new InvalidOperationException(
                        $"Mã báo cáo gửi email [{info.Code}] bị khai trùng ({map[info.Code].Module} và {info.Module})");
            }
            return map;
        }
    }
}
