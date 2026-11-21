using Microsoft.Extensions.Options;
using Modules.CfgSystem.Core.Dto.ConfigData;
using Modules.CfgSystem.Core.Dto.OpConfig;
using Wolverine;
using Modules.Mail.Core.Constants;
using Modules.Mail.Core.Dto.Smtp;

namespace Modules.Mail.Infrastructure.Services.Mail.Config
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Đọc cấu hình vận hành của phân hệ Mail (SysOpConfig nhóm mail_config, hỏi qua MessBus) ghép với
    ///              phần bí mật ở Mail.json. Service là scoped và nhớ kết quả trong scope: một request / một lần chạy
    ///              job chỉ hỏi CfgSystem một lần; sửa cấu hình thì request sau dùng ngay, không phải khởi động lại.
    /// Created date: 17/08/2026
    /// </summary>
    public class MailConfigService : IScoped
    {
        private readonly IMessageBus _bus;
        private readonly MailSettings _fileSettings;
        private Task<MailConfig>? _current;
        private Task<HashSet<int>>? _periodOffsets;

        public MailConfigService(IMessageBus bus, IOptions<MailSettings> fileSettings)
        {
            _bus = bus;
            _fileSettings = fileSettings.Value;
        }

        /// <summary>Cấu hình đang dùng. Thiếu khoá / giá trị sai thì lấy mặc định.</summary>
        public Task<MailConfig> GetAsync() => _current ??= LoadAsync();

        /// <summary>
        /// Các kỳ lấy số liệu được chọn — SysConfigData loại mail_period, Code là số. Chỉ nhận giá trị logic tính kỳ
        /// hiểu được (MailRptPeriodOffset.All); chưa khai thì dùng cả danh sách đó.
        /// </summary>
        public Task<HashSet<int>> GetPeriodOffsetsAsync() => _periodOffsets ??= LoadPeriodOffsetsAsync();

        private async Task<HashSet<int>> LoadPeriodOffsetsAsync()
        {
            var result = await _bus.InvokeAsync<List<SysConfigDataOutput>>(
                new SysConfigDataInput { ConfigTypeCode = MailConst.ConfigTypePeriodOffset });

            var offsets = (result ?? new List<SysConfigDataOutput>())
                .Select(x => int.TryParse(x.Code?.Trim(), out var v) ? v : (int?)null)
                .Where(v => v.HasValue && MailRptPeriodOffset.All.Contains(v.Value))
                .Select(v => v!.Value)
                .ToHashSet();

            return offsets.Count > 0 ? offsets : MailRptPeriodOffset.All.ToHashSet();
        }

        private async Task<MailConfig> LoadAsync()
        {
            var result = await _bus.InvokeAsync<List<SysOpConfigOutput>>(
                new SysOpConfigInput { GroupCode = MailConst.ConfigGroupCode });

            // Bản ghi thiếu Code là dữ liệu rác — để lọt vào ToDictionary sẽ ném lỗi
            var c = (result ?? new List<SysOpConfigOutput>())
                .Where(x => !string.IsNullOrWhiteSpace(x.Code))
                .GroupBy(x => x.Code!)
                .ToDictionary(g => g.Key, g => g.Last().Value ?? string.Empty);

            string? Str(string key) => c.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v) ? v.Trim() : null;
            int Int(string key, int def, int min = 0) => int.TryParse(Str(key), out var v) && v >= min ? v : def;
            bool Bool(string key, bool def) => bool.TryParse(Str(key), out var v) ? v : Str(key) switch { "1" => true, "0" => false, _ => def };

            return new MailConfig
            {
                Enable = Bool(MailConst.MailKey.Enable, false),
                Host = Str(MailConst.MailKey.SmtpHost) ?? string.Empty,
                Port = Int(MailConst.MailKey.SmtpPort, 587),
                SecureSocketOptions = _fileSettings.SecureSocketOptions,
                UserName = Str(MailConst.MailKey.SmtpUserName) ?? string.Empty,
                Password = _fileSettings.Password,
                RequireAuthentication = Bool(MailConst.MailKey.SmtpRequireAuth, true),
                FromName = Str(MailConst.MailKey.FromName) ?? string.Empty,
                SubjectPrefix = Str(MailConst.MailKey.SubjectPrefix),
                MaxRecipientsPerMail = Int(MailConst.MailKey.MaxRecipients, 20),
                MaxAttachmentMb = Int(MailConst.MailKey.MaxAttachmentMb, 20),
                TimeoutSeconds = Int(MailConst.MailKey.TimeoutSeconds, 60),
                RetryCount = Int(MailConst.MailKey.RetryCount, 2),
                RetryDelaySeconds = Int(MailConst.MailKey.RetryDelaySeconds, 30),
                BccAudit = Str(MailConst.MailKey.BccAudit),
                TestRecipient = Str(MailConst.MailKey.TestRecipient),

                MaxItemsPerSchedule = Int(MailConst.ConfigKeyMaxItems, MailConst.MaxItemsPerSchedule, min: 1),
                MaxCatchUpHours = Int(MailConst.ConfigKeyMaxCatchUpHours, MailConst.DefaultMaxCatchUpHours, min: 1),
                LogRetentionDays = Int(MailConst.ConfigKeyLogRetentionDays, MailConst.DefaultLogRetentionDays),
            };
        }
    }
}
