using Hangfire;
using Microsoft.Extensions.Logging;
using Shared.DTO.Enums;
using Shared.Utility.Utilities;
using Modules.Mail.Core.Dto.Render;
using Modules.Mail.Core.Dto.Smtp;
using Modules.Mail.Core.Entities;
using Modules.Mail.Core.Exceptions;
using Modules.Mail.Core.Helper;

using Modules.Mail.Infrastructure.Smtp;
using Modules.Mail.Infrastructure.Services.Mail.Config;
using Modules.Mail.Infrastructure.Services.Mail.Log;
using Modules.Mail.Infrastructure.Services.Mail.Report;
using Modules.Mail.Infrastructure.Services.Mail.Schedule;
using Modules.Mail.Infrastructure.Services.Mail.Templates;

namespace Modules.Mail.Infrastructure.Services.Mail.Delivery
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Gửi báo cáo qua email theo lịch.
    ///
    ///              Mỗi lịch một recurring job Hangfire ("rpt-email-{ID lịch}", cron sinh từ quy tắc chạy — xem
    ///              RptEmailJobScheduler). Tới giờ Hangfire gọi RunScheduleAsync(id). Job vẫn tự hỏi lại "đã tới mốc nào,
    ///              mốc đó gửi chưa" (GetLastDueTime + LastRunKey) nên cron bắn thừa, bắn trễ hay chạy bù sau khi server
    ///              khởi động lại đều không gửi trùng.
    /// Created date: 17/08/2026
    /// </summary>
    public class RptEmailSendService : IScoped
    {
        private readonly BaseRepository<MailSchedule> _scheduleRep;
        private readonly BaseRepository<MailScheduleItem> _itemRep;
        private readonly IMailSender _mailSender;
        private readonly ReportExporter _reportExporter;
        private readonly MailConfigService _configService;
        private readonly MailTemplateStore _templates;
        private readonly RptEmailReportService _reportService;
        private readonly RptEmailLogService _logService;
        private readonly MailInlineImageService _inlineImages;
        private readonly ILogger<RptEmailSendService> _logger;

        public RptEmailSendService(
            BaseRepository<MailSchedule> scheduleRep,
            BaseRepository<MailScheduleItem> itemRep,
            IMailSender mailSender,
            ReportExporter reportExporter,
            MailConfigService configService,
            MailTemplateStore templates,
            RptEmailReportService reportService,
            RptEmailLogService logService,
            MailInlineImageService inlineImages,
            ILogger<RptEmailSendService> logger)
        {
            _scheduleRep = scheduleRep;
            _itemRep = itemRep;
            _mailSender = mailSender;
            _reportExporter = reportExporter;
            _configService = configService;
            _templates = templates;
            _reportService = reportService;
            _logService = logService;
            _inlineImages = inlineImages;
            _logger = logger;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Điểm vào của recurring job "rpt-email-{scheduleId}" — gửi MỘT lịch nếu đã tới mốc mà chưa gửi.
        ///              Phải công khai và nhận CancellationToken (Hangfire gọi qua biểu thức).
        ///                - Lịch đã xoá / tắt: xoá luôn job của nó (phòng DB và Hangfire lệch nhau) rồi thôi.
        ///                - Cron bắn thừa (ngày 28-31 với lịch ngày 29/30) hoặc mốc đã gửi: không làm gì.
        ///                - Mốc quá hạn hơn mail_max_catchup_hours: đánh dấu đã chạy, ghi log bỏ qua, không gửi.
        ///              AutomaticRetry = 0: lỗi gửi đã được MailSender thử lại và ghi log; để Hangfire thử lại cả job thì
        ///              có thể kết xuất / gửi lại nhiều lần.
        /// Created date: 02/10/2026
        /// </summary>
        [AutomaticRetry(Attempts = 0)]
        public async Task RunScheduleAsync(string scheduleId, CancellationToken ct = default)
        {
            var schedule = await _scheduleRep.GetFirstAsync(u => u.ID == scheduleId && u.IsDelete == null);

            if (schedule == null || schedule.Status != BaseEnums.StatusEnum.Enable)
            {
                RptEmailJobScheduler.RemoveJob(scheduleId);
                return;
            }

            try
            {
                var now = BaseUtil.TimeServer.GetTimeServer();
                var due = RptScheduleTime.GetLastDueTime(MailRptScheduleRule.From(schedule), now);

                // Tần suất khai sai (nhập tay vào DB) thì không có mốc nào để so
                if (due == null) return;

                var runKey = RptScheduleTime.GetRunKey(due);
                if (string.Equals(schedule.LastRunKey, runKey, StringComparison.Ordinal)) return;

                var maxCatchUpHours = (await _configService.GetAsync()).MaxCatchUpHours;
                if ((now - due.Value).TotalHours > maxCatchUpHours)
                {
                    _logger.LogWarning(
                        "Bỏ qua lịch gửi báo cáo {Code}: mốc {Due:dd/MM/yyyy HH:mm} đã quá hạn hơn {Hours} giờ.",
                        schedule.Code, due.Value, maxCatchUpHours);

                    await MarkRunAsync(schedule, runKey, now, null);
                    await _logService.WriteSkippedAsync(schedule, due.Value, now, maxCatchUpHours);
                    return;
                }

                await RunOneAsync(schedule, due.Value, runKey, BaseEnums.TriggerType.Schedule, now, ct);
            }
            catch (Exception ex)
            {
                // Không ném ra Hangfire (xem AutomaticRetry) — lỗi đã nằm trong log ứng dụng
                _logger.LogError(ex, "Lỗi khi xử lý lịch gửi báo cáo {Code}", schedule.Code);
            }
        }

        /// <summary>Gửi ngay một lịch (nút "Gửi ngay"). Kỳ dữ liệu tính theo thời điểm bấm.</summary>
        public async Task<bool> SendNowAsync(string scheduleId, CancellationToken ct = default)
        {
            var schedule = await _scheduleRep.GetFirstAsync(u => u.IsDelete == null && u.ID == scheduleId)
                ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            var now = BaseUtil.TimeServer.GetTimeServer();
            return await RunOneAsync(schedule, now, runKey: null, BaseEnums.TriggerType.Manual, now, ct);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Kết xuất các báo cáo của một lịch, dựng thư theo file mẫu đã chọn, gửi, ghi log và đánh dấu
        ///              đã chạy. Báo cáo lỗi / ngừng dùng thì bỏ qua báo cáo đó, các báo cáo khác vẫn gửi.
        /// Created date: 17/08/2026
        /// </summary>
        private async Task<bool> RunOneAsync(
            MailSchedule schedule, DateTime dueTime, string? runKey,
            BaseEnums.TriggerType trigger, DateTime now, CancellationToken ct)
        {
            var items = await _itemRep.AsQueryable()
                .Where(u => u.IsDelete == null && u.ScheduleId == schedule.ID)
                .OrderBy(u => u.OrderNo)
                .ToListAsync();

            var attachments = new List<MailAttachment>();
            var errors = new List<string>();
            var periods = new List<MailRptPeriod>();
            var lines = new List<MailReportLine>();   // bảng báo cáo in trong thư
            var notes = new List<string>();           // ghi chú phạm vi số liệu: khối {LuuY} + Remark của log

            // Kỳ tính LÚC GỬI (kỳ cố định trong code, hoặc theo tần suất lịch); cột PeriodType đã lưu chỉ là dự phòng
            // cho mã đã bị gỡ khỏi code.
            var catalog = await _reportService.LoadAsync();

            foreach (var item in items)
            {
                if (ct.IsCancellationRequested) break;

                var reportName = catalog.Name(item.ReportCode);
                var periodType = RptReportCode.ResolvePeriodType(item.ReportCode, schedule.Frequency, item.PeriodType);
                var period = MailRptPeriodResolver.ResolvePeriod(periodType, dueTime, item.PeriodOffset);
                periods.Add(period);

                try
                {
                    // Báo cáo bị tắt / xoá khỏi danh mục sau khi đặt lịch
                    if (!catalog.IsActive(item.ReportCode))
                        throw Oops.Oh(BaseMsg.Mail.Exception.ReportNotActive, item.ReportCode);

                    var attachment = await _reportExporter.RenderAsync(catalog.Get(item.ReportCode)!, item, period, ct);
                    attachments.Add(attachment);

                    var note = MailRptPeriodResolver.PeriodNote(schedule.Frequency, periodType, period, reportName);
                    if (note != null) notes.Add(note);

                    lines.Add(new MailReportLine
                    {
                        ReportName = reportName,
                        GroupName = catalog.Group(item.ReportCode),
                        PeriodText = period.Text,
                        FileName = attachment.FileName,
                    });
                }
                catch (Exception ex)
                {
                    errors.Add($"[{item.ReportCode}] {ex.Message}");
                    _logger.LogError(ex, "Kết xuất báo cáo {ReportCode} của lịch {Code} thất bại",
                        item.ReportCode, schedule.Code);
                }
            }

            MailSendResult result;
            string? subject = null;
            // Nội dung thư là file riêng của lịch (HTML soạn bằng wangEditor) — sửa mẫu về sau không ảnh hưởng
            var content = _templates.Read(schedule.ContentPath);

            if (attachments.Count == 0)
            {
                result = MailSendResult.Fail(errors.Count > 0
                    ? string.Join(" | ", errors)
                    : "Lịch chưa khai báo cáo nào để gửi.");
            }
            else if (content == null)
            {
                // Mất file trong wwwroot/Upload/Mail (deploy ghi đè thư mục Upload...), không phải lỗi dữ liệu nhập
                result = MailSendResult.Fail(Furion.Localization.L.Text[BaseMsg.Mail.Exception.ContentFileNotFound, schedule.ContentPath ?? string.Empty]);
            }
            else
            {
                // Tag về báo cáo tính trên các báo cáo KẾT XUẤT ĐƯỢC — tiêu đề không nói về báo cáo không có trong thư
                var ctx = new MailRenderContext
                {
                    ScheduleName = schedule.Name ?? schedule.Code,
                    Lines = lines,
                    SentAt = now,
                };

                subject = MailBodyRenderer.RenderSubject(schedule.Subject, ctx);

                // Ảnh của server trong nội dung đổi sang cid: và gửi kèm; KHÔNG tính vào tệp báo cáo khi ghi log
                var (body, inline) = _inlineImages.Embed(MailBodyRenderer.RenderBody(content, ctx));
                var recipients = MailAddressList.Parse(schedule.Recipients, schedule.Cc, schedule.Bcc);
                result = await _mailSender.SendAsync(recipients, subject, body, attachments.Concat(inline), ct);

                // Gửi được nhưng thiếu tệp thì vẫn phải coi là có vấn đề, không báo thành công trơn
                if (result.Success && errors.Count > 0)
                    result.ErrorMessage = $"Đã gửi {attachments.Count} tệp, {errors.Count} báo cáo lỗi: {string.Join(" | ", errors)}";
            }

            var log = RptEmailLogService.NewLog(schedule, trigger, now);
            log.TemplateCode = schedule.TemplateCode;
            log.Subject = subject;
            // Kỳ của cả thư: sớm nhất tới muộn nhất trong các báo cáo
            log.PeriodFrom = periods.Count > 0 ? periods.Min(x => x.From) : null;
            log.PeriodTo = periods.Count > 0 ? periods.Max(x => x.To) : null;
            // Cột ngắn nên nhiều báo cáo thì ghi MULTI, chi tiết đã có ở FileName
            log.ReportCode = items.Count == 1 ? items[0].ReportCode : (items.Count > 1 ? "MULTI" : null);
            log.FileName = string.Join("; ", attachments.Select(x => x.FileName));
            log.ByteSize = attachments.Sum(x => x.Content.LongLength);
            log.Status = result.Success ? BaseEnums.SuccessEnums.Success : BaseEnums.SuccessEnums.Fail;
            log.Attempts = result.Attempts;
            log.ErrorMessage = result.ErrorMessage;
            // Vận hành tra "vì sao thư thiếu số liệu" ở đây: kỳ ngắn hơn tần suất, kỳ tạm tính
            log.Remark = notes.Count > 0 ? string.Join(" | ", notes) : null;
            await _logService.WriteAsync(log);

            // Gửi thủ công (runKey = null) chỉ cập nhật vết chạy, không dập mốc chống trùng
            await MarkRunAsync(schedule, runKey, now, log.Status);

            return result.Success;
        }

        private async Task MarkRunAsync(
            MailSchedule schedule, string? runKey, DateTime now, BaseEnums.SuccessEnums? status)
        {
            if (runKey != null) schedule.LastRunKey = runKey;

            if (status != null)
            {
                schedule.LastTimeRun = now;
                schedule.LastStatus = status;
            }

            schedule.NextTimeRun = RptScheduleTime.GetNextDueTime(MailRptScheduleRule.From(schedule), now);

            await _scheduleRep.AsUpdateable(schedule)
                .UpdateColumns(u => new { u.LastRunKey, u.LastTimeRun, u.LastStatus, u.NextTimeRun })
                .ExecuteCommandAsync();
        }
    }
}
