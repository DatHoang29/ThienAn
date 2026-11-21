using Hangfire;
using Microsoft.Extensions.Logging;
using Shared.DTO.Enums;
using Shared.Utility.Utilities;
using Modules.Mail.Core.Constants;
using Modules.Mail.Core.Dto.Smtp;
using Modules.Mail.Core.Entities;

using Modules.Mail.Infrastructure.Services.Mail.Config;

namespace Modules.Mail.Infrastructure.Services.Mail.Log
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Nhật ký gửi mail (RptEmailLog) — chỗ DUY NHẤT ghi và dọn bảng này:
    ///                - thư gửi theo lịch / gửi ngay (RptEmailSendService dựng bản ghi bằng NewLog rồi gọi WriteAsync);
    ///                - mốc bị bỏ qua vì quá hạn gửi bù (WriteSkippedAsync);
    ///                - thư gửi thử cấu hình (WriteTestAsync);
    ///                - dọn log cũ hơn mail_log_retention_days (job Hangfire "mail-log-cleanup").
    ///              Lỗi ghi log chỉ ghi cảnh báo, không làm hỏng việc gửi hay việc đánh dấu đã chạy.
    /// Created date: 02/10/2026
    /// </summary>
    public class RptEmailLogService : IScoped
    {
        private readonly BaseRepository<MailLog> _logRep;
        private readonly MailConfigService _configService;
        private readonly ILogger<RptEmailLogService> _logger;

        public RptEmailLogService(
            BaseRepository<MailLog> logRep,
            MailConfigService configService,
            ILogger<RptEmailLogService> logger)
        {
            _logRep = logRep;
            _configService = configService;
            _logger = logger;
        }

        /// <summary>
        /// Bản ghi log mới với các trường chung. Người nhận chụp lại từ lịch lúc gửi — lịch sửa sau không làm sai
        /// lịch sử.
        /// </summary>
        public static MailLog NewLog(MailSchedule? schedule, BaseEnums.TriggerType trigger, DateTime now) => new()
        {
            ScheduleId = schedule?.ID,
            TriggerType = trigger,
            OccurredAt = now,
            Recipients = schedule?.Recipients,
            Cc = schedule?.Cc,
            Bcc = schedule?.Bcc,
            TemplateCode = schedule?.TemplateCode,
        };

        /// <summary>Ghi một bản ghi log: sinh mã, cắt chuỗi theo độ dài cột.</summary>
        public async Task WriteAsync(MailLog log)
        {
            try
            {
                log.Code = $"{MailConst.LogCodePrefix}{log.OccurredAt:yyMMddHHmmssfff}";
                log.Recipients = Truncate(log.Recipients, 1024);
                log.Cc = Truncate(log.Cc, 1024);
                log.Bcc = Truncate(log.Bcc, 1024);
                log.Subject = Truncate(log.Subject, 256);
                log.FileName = Truncate(log.FileName, 256);
                log.Remark = Truncate(log.Remark, 256);
                log.CreateUId = null;

                await _logRep.InsertAsync(log);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không ghi được nhật ký gửi mail (lịch {ScheduleId})", log.ScheduleId);
            }
        }

        /// <summary>
        /// Mốc bị BỎ QUA vì quá hạn gửi bù (server tắt lâu, Hangfire chạy bù khi bật lại) — để vận hành thấy "kỳ đó
        /// không gửi và vì sao". SuccessEnums chỉ có Success / Fail nên ghi Fail, lý do ở ErrorMessage.
        /// </summary>
        public Task WriteSkippedAsync(MailSchedule schedule, DateTime due, DateTime now, int maxCatchUpHours)
        {
            var log = NewLog(schedule, BaseEnums.TriggerType.Schedule, now);
            log.Status = BaseEnums.SuccessEnums.Fail;
            log.Attempts = 0;
            log.ErrorMessage = $"Bỏ qua — mốc {due:dd/MM/yyyy HH:mm} đã quá hạn hơn {maxCatchUpHours} giờ (mail_max_catchup_hours), không gửi bù.";
            return WriteAsync(log);
        }

        /// <summary>Một lần gửi thử cấu hình (không thuộc lịch).</summary>
        public Task WriteTestAsync(string to, string subject, MailSendResult result, string? fileName, long byteSize)
        {
            var log = NewLog(null, BaseEnums.TriggerType.Test, BaseUtil.TimeServer.GetTimeServer());
            log.Recipients = to;
            log.Subject = subject;
            log.FileName = fileName;
            log.ByteSize = byteSize;
            log.Status = result.Success ? BaseEnums.SuccessEnums.Success : BaseEnums.SuccessEnums.Fail;
            log.Attempts = result.Attempts;
            log.ErrorMessage = result.ErrorMessage;
            return WriteAsync(log);
        }

        /// <summary>
        /// Điểm vào job "mail-log-cleanup": XOÁ CỨNG log có OccurredAt cũ hơn mail_log_retention_days ngày
        /// (bảng log chỉ để tra cứu, không cần giữ vết đã xoá). 0 = giữ mãi.
        /// </summary>
        [AutomaticRetry(Attempts = 0)]
        public async Task CleanupAsync()
        {
            var days = (await _configService.GetAsync()).LogRetentionDays;
            if (days <= 0) return;

            var cutoff = BaseUtil.TimeServer.GetTimeServer().Date.AddDays(-days);
            var deleted = await _logRep.Context.Deleteable<MailLog>()
                .Where(u => u.OccurredAt < cutoff)
                .ExecuteCommandAsync();

            _logger.LogInformation("Dọn nhật ký gửi mail: xoá {Count} dòng trước {Cutoff:dd/MM/yyyy} (giữ {Days} ngày).",
                deleted, cutoff, days);
        }

        private static string? Truncate(string? value, int max)
            => string.IsNullOrEmpty(value) || value.Length <= max ? value : value[..max];
    }
}
