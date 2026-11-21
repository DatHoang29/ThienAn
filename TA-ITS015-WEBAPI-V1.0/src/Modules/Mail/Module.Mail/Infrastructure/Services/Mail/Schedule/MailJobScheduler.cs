using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shared.DTO.Enums;
using Modules.Mail.Core.Constants;
using Modules.Mail.Core.Dto.Render;
using Modules.Mail.Core.Entities;
using Modules.Mail.Core.Helper;

using Modules.Mail.Infrastructure.Services.Mail.Delivery;
using Modules.Mail.Infrastructure.Services.Mail.Log;

namespace Modules.Mail.Infrastructure.Services.Mail.Schedule
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Đồng bộ lịch gửi báo cáo (RptEmailSchedule) với Hangfire
    /// Created date: 02/10/2026
    /// </summary>
    public class RptEmailJobScheduler : IScoped
    {
        private readonly BaseRepository<MailSchedule> _scheduleRep;
        private readonly ILogger<RptEmailJobScheduler> _logger;
        private readonly bool _enabled;

        public RptEmailJobScheduler(
            BaseRepository<MailSchedule> scheduleRep,
            IConfiguration configuration,
            ILogger<RptEmailJobScheduler> logger)
        {
            _scheduleRep = scheduleRep;
            _logger = logger;
            _enabled = IsHangfireEnabled(configuration);
        }

        public static bool IsHangfireEnabled(IConfiguration configuration)
            => string.Equals(configuration["Hangfire:Enable"], "true", StringComparison.OrdinalIgnoreCase);

        /// <summary>Mã recurring job của một lịch.</summary>
        public static string JobIdOf(string scheduleId) => MailConst.JobIdPrefix + scheduleId;

        /// <summary>Xoá job của một lịch — gọi được từ trong chính job (RunScheduleAsync).</summary>
        public static void RemoveJob(string scheduleId) => RecurringJob.RemoveIfExists(JobIdOf(scheduleId));

        /// <summary>Lịch đang bật thì tạo / cập nhật job theo quy tắc chạy hiện tại; tắt / xoá thì gỡ job.</summary>
        public void Register(MailSchedule schedule)
        {
            if (!_enabled) return;

            try
            {
                var cron = schedule.IsDelete == null && schedule.Status == BaseEnums.StatusEnum.Enable
                    ? RptScheduleTime.ToCron(MailRptScheduleRule.From(schedule))
                    : null;

                if (cron == null)
                {
                    RemoveJob(schedule.ID);
                    return;
                }

                var scheduleId = schedule.ID;
                RecurringJob.AddOrUpdate<RptEmailSendService>(
                    recurringJobId: JobIdOf(scheduleId),
                    methodCall: service => service.RunScheduleAsync(scheduleId, CancellationToken.None),
                    cronExpression: cron,
                    options: new RecurringJobOptions { TimeZone = TimeZoneInfo.Local });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không đăng ký được job Hangfire cho lịch gửi báo cáo {Code}", schedule.Code);
            }
        }

        /// <summary>Gỡ job của các lịch đã xoá.</summary>
        public void Remove(IEnumerable<string> scheduleIds)
        {
            if (!_enabled) return;

            foreach (var id in scheduleIds)
            {
                try { RemoveJob(id); }
                catch (Exception ex) { _logger.LogError(ex, "Không gỡ được job Hangfire của lịch {Id}", id); }
            }
        }

        /// <summary>Job dọn nhật ký gửi cũ (RptEmailLogService.CleanupAsync), hằng ngày 02:17.</summary>
        public void RegisterLogCleanupJob()
        {
            if (!_enabled) return;

            try
            {
                RecurringJob.AddOrUpdate<RptEmailLogService>(
                    recurringJobId: MailConst.LogCleanupJobId,
                    methodCall: service => service.CleanupAsync(),
                    cronExpression: MailConst.LogCleanupCron,
                    options: new RecurringJobOptions { TimeZone = TimeZoneInfo.Local });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không đăng ký được job dọn nhật ký gửi mail");
            }
        }

        /// <summary>
        /// Đồng bộ toàn bộ lúc khởi động API: mọi lịch đang bật có job đúng cron, mọi job "rpt-email-*" không
        /// ứng với lịch đang bật (kể cả job quét chung cũ "rpt-email-scan") bị gỡ.
        /// </summary>
        public async Task SyncAllAsync()
        {
            if (!_enabled) return;

            var schedules = await _scheduleRep.AsQueryable()
                .Where(u => u.IsDelete == null && u.Status == BaseEnums.StatusEnum.Enable)
                .ToListAsync();

            foreach (var schedule in schedules)
                Register(schedule);

            var keep = schedules.Select(x => JobIdOf(x.ID)).ToHashSet(StringComparer.Ordinal);

            using var connection = JobStorage.Current.GetConnection();
            var orphans = connection.GetAllItemsFromSet("recurring-jobs")
                .Where(id => id.StartsWith(MailConst.JobIdPrefix, StringComparison.Ordinal) && !keep.Contains(id))
                .ToList();

            foreach (var id in orphans)
                RecurringJob.RemoveIfExists(id);

            _logger.LogInformation("Đồng bộ job gửi báo cáo: {Count} lịch đang bật, gỡ {Orphans} job thừa.",
                schedules.Count, orphans.Count);
        }
    }
}
