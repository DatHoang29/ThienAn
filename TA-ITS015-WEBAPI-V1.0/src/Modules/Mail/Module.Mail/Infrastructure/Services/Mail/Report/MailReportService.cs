using Microsoft.Extensions.Logging;
using Shared.DTO.Enums;
using Modules.Mail.Core.Entities;


namespace Modules.Mail.Infrastructure.Services.Mail.Report
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Nạp danh mục báo cáo gửi email (bảng RptEmailReport + tên nhóm từ SysPlugin) thành một
    ///              RptReportCatalog để tra tên / nhóm / trạng thái theo mã. Nhớ kết quả trong scope: một request
    ///              hay một lần chạy job chỉ đọc DB một lần.
    /// Created date: 01/10/2026
    /// </summary>
    public class RptEmailReportService : IScoped
    {
        private readonly BaseRepository<MailReport> _reportRep;
        private readonly ILogger<RptEmailReportService> _logger;
        private Task<RptReportCatalog>? _catalog;

        public RptEmailReportService(BaseRepository<MailReport> reportRep, ILogger<RptEmailReportService> logger)
        {
            _reportRep = reportRep;
            _logger = logger;
        }

        public Task<RptReportCatalog> LoadAsync() => _catalog ??= LoadCoreAsync();

        private async Task<RptReportCatalog> LoadCoreAsync()
        {
            var rows = await _reportRep.AsQueryable()
                .Where(u => u.IsDelete == null)
                .OrderBy(u => u.OrderNo)
                .ToListAsync();

            var reports = rows
                .Where(x => !string.IsNullOrWhiteSpace(x.Code))
                .GroupBy(x => x.Code.Trim(), StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

            return new RptReportCatalog(reports, await LoadGroupNamesAsync());
        }

        /// <summary>Tên nhóm báo cáo = SysPlugin.Name theo Code. Lỗi đọc thì chỉ mất tên nhóm, không chặn gửi thư.</summary>
        private async Task<Dictionary<string, string>> LoadGroupNamesAsync()
        {
            try
            {
                var rows = await _reportRep.Context.Queryable<SysPluginNameRow>()
                    .AS("SysPlugin")
                    .Where(u => u.IsDelete == null)
                    .ToListAsync();

                return rows
                    .Where(x => !string.IsNullOrWhiteSpace(x.Code))
                    .GroupBy(x => x.Code!.Trim(), StringComparer.Ordinal)
                    .ToDictionary(g => g.Key, g => g.First().Name?.Trim() ?? g.Key, StringComparer.Ordinal);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không đọc được tên nhóm báo cáo (SysPlugin)");
                return new Dictionary<string, string>(StringComparer.Ordinal);
            }
        }

        /// <summary>Ba cột cần của bảng SysPlugin (phân hệ System) — không tham chiếu entity của phân hệ khác.</summary>
        public class SysPluginNameRow
        {
            public string? Code { get; set; }
            public string? Name { get; set; }
            public DateTime? IsDelete { get; set; }
        }
    }

    /// <summary>
    /// Description: Danh mục báo cáo đã nạp — tra theo mã báo cáo.
    /// </summary>
    public sealed class RptReportCatalog
    {
        private readonly IReadOnlyDictionary<string, MailReport> _reports;
        private readonly IReadOnlyDictionary<string, string> _groupNames;

        public RptReportCatalog(IReadOnlyDictionary<string, MailReport> reports, IReadOnlyDictionary<string, string> groupNames)
        {
            _reports = reports;
            _groupNames = groupNames;
        }

        /// <summary>Dòng danh mục của mã; null khi không có.</summary>
        public MailReport? Get(string? code)
            => !string.IsNullOrWhiteSpace(code) && _reports.TryGetValue(code.Trim(), out var r) ? r : null;

        /// <summary>Tên hiển thị; chưa khai tên thì trả lại mã.</summary>
        public string Name(string? code)
        {
            if (string.IsNullOrWhiteSpace(code)) return string.Empty;
            return Get(code) is { } r && !string.IsNullOrWhiteSpace(r.Name) ? r.Name : code;
        }

        /// <summary>Tên nhóm (SysPlugin.Name theo PluginCode); thiếu tên thì trả PluginCode.</summary>
        public string? Group(string? code)
        {
            var plugin = Get(code)?.PluginCode?.Trim();
            if (string.IsNullOrEmpty(plugin)) return null;
            return _groupNames.TryGetValue(plugin, out var name) && !string.IsNullOrWhiteSpace(name) ? name : plugin;
        }

        /// <summary>Đặt lịch / gửi được: có trong danh mục, đang bật VÀ đã có nhánh kết xuất đúng phân hệ trong code.</summary>
        public bool IsActive(string? code)
            => Get(code) is { } r
               && r.Status != BaseEnums.StatusEnum.Disable
               && RptReportCode.IsSupported(r.PluginCode, code);
    }
}
