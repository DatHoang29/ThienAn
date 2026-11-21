using Mapster;
using Microsoft.Extensions.Logging;
using Shared.Core.Extensions;
using Shared.DTO.Constants.Application;
using Shared.DTO.Constants.Localization;
using Shared.DTO.Enums;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;
using Shared.Utility.Utilities;
using Wolverine;
using Modules.Mail.Core.Constants;
using Modules.Mail.Core.Dto.EmailSchedule;
using Modules.Mail.Core.Dto.Render;
using Modules.Mail.Core.Entities;
using Modules.Mail.Core.Exceptions;
using Modules.Mail.Core.Helper;
using Modules.Mail.Infrastructure;


using Modules.Mail.Infrastructure.Services.Mail.Config;
using Modules.Mail.Infrastructure.Services.Mail.Delivery;
using Modules.Mail.Infrastructure.Services.Mail.Report;
using Modules.Mail.Infrastructure.Services.Mail.Schedule;
using Modules.Mail.Infrastructure.Smtp;
using Modules.Mail.Infrastructure.Services.Mail.Templates;

namespace Modules.Mail.Controllers.EmailSchedule.Commands
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Xử lý thêm / sửa / xóa lịch gửi báo cáo qua email.
    /// Created date: 14/08/2026
    /// </summary>
    public class RptEmailScheduleCommandHandler : IWolverineHandler
    {
        private readonly BaseRepository<MailSchedule> _scheduleRep;
        private readonly BaseRepository<MailScheduleItem> _itemRep;
        private readonly BaseCacheService _cache;
        private readonly IMailSender _mailSender;
        private readonly BaseRepository<MailAddress> _addressRep;
        private readonly MailConfigService _configService;
        private readonly MailTemplateStore _templates;
        private readonly RptEmailReportService _reportService;
        private readonly RptEmailJobScheduler _jobScheduler;
        private readonly ILogger<RptEmailScheduleCommandHandler> _logger;

        public RptEmailScheduleCommandHandler(
            BaseRepository<MailSchedule> scheduleRep,
            BaseRepository<MailScheduleItem> itemRep,
            BaseCacheService cache,
            IMailSender mailSender,
            BaseRepository<MailAddress> addressRep,
            MailConfigService configService,
            MailTemplateStore templates,
            RptEmailReportService reportService,
            RptEmailJobScheduler jobScheduler,
            ILogger<RptEmailScheduleCommandHandler> logger)
        {
            _scheduleRep = scheduleRep;
            _itemRep = itemRep;
            _cache = cache;
            _mailSender = mailSender;
            _addressRep = addressRep;
            _configService = configService;
            _templates = templates;
            _reportService = reportService;
            _jobScheduler = jobScheduler;
            _logger = logger;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Thêm mới 1 lịch gửi báo cáo kèm danh sách báo cáo đính kèm
        /// Created date: 14/08/2026
        /// </summary>
        public async Task HandleAsync(MailAddScheduleInput command)
        {
            // 1. Mã lịch: người dùng không nhập thì tự sinh theo mốc thời gian
            command.Code = command.Code?.Trim();

            if (string.IsNullOrWhiteSpace(command.Code))
            {
                throw Oops.Oh(BaseMsg.Mail.Validation.FieldRequired, Furion.Localization.L.Text[BaseMsg.BaseEntity.Code]);
            }
            var isExist = await _scheduleRep.AsQueryable()
               .ClearFilter()
               .AnyAsync(u => u.Code == command.Code && u.IsDelete == null);

            if (isExist)
            {
                throw Oops.Oh(BaseMsg.BaseException.Exist, Furion.Localization.L.Text[BaseMsg.BaseEntity.Code]);
            }
            // 2. Chuẩn hoá + kiểm tra nghiệp vụ cho cả lịch và từng item
            await NormalizeAsync(command);

            var entity = command.Adapt<MailSchedule>();
            entity.CreateUId = null;

            StampRunMarkers(entity);

            try
            {
                _scheduleRep.Context.Ado.BeginTran();

                await _scheduleRep.InsertWithDiffLogAsync(entity);
                await InsertItemsAsync(entity.ID, command.Items);

                // Nội dung thư ghi ra file (cần ID nên ghi SAU khi insert), DB chỉ lưu đường dẫn.
                // Ghi file lỗi thì rollback cả lịch.
                entity.ContentPath = await _templates.WriteScheduleAsync(
                    entity.ID, BaseUtil.TimeServer.GetTimeServer(), command.Content!, currentPath: null);
                await _scheduleRep.Context.Updateable<MailSchedule>()
                    .SetColumns(u => u.ContentPath == entity.ContentPath)
                    .Where(u => u.ID == entity.ID)
                    .ExecuteCommandAsync();

                _scheduleRep.Context.Ado.CommitTran();
            }
            catch
            {
                // Không rollback thì transaction treo lại trên connection và làm hỏng các lệnh sau
                try { _scheduleRep.Context.Ado.RollbackTran(); } catch { /* không có tran đang mở */ }
                throw;
            }

            // Job Hangfire của lịch: tạo / cập nhật theo quy tắc chạy mới — SAU khi commit DB
            _jobScheduler.Register(entity);
            await AddToAddressBookAsync(entity);
            ClearCache();
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Cập nhật 1 lịch gửi báo cáo. Items gửi lên là toàn bộ danh sách mới.
        /// Created date: 14/08/2026
        /// </summary>
        public async Task HandleAsync(MailUpdateScheduleInput command)
        {
            command.Code = command.Code?.Trim();

            if (string.IsNullOrWhiteSpace(command.Code))
            {
                throw Oops.Oh(BaseMsg.Mail.Validation.FieldRequired, Furion.Localization.L.Text[BaseMsg.BaseEntity.Code]);
            }
            var isExist = await _scheduleRep.AsQueryable()
                .ClearFilter()
                .AnyAsync(u => u.Code == command.Code && u.ID != command.ID);

            if (isExist)
            {
                throw Oops.Oh(BaseMsg.BaseException.Exist, Furion.Localization.L.Text[BaseMsg.BaseEntity.Code]);
            }
            var entity = await _scheduleRep.GetByIdAsync(command.ID)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            await NormalizeAsync(command);

            // Map dữ liệu từ command vào bản ghi gốc.
            // THIẾU BƯỚC NÀY thì Update trả về thành công nhưng DB không đổi gì.
            command.Adapt(entity);

            // Ghi đè đúng file nội dung cũ của lịch (thư mục theo ngày tạo không đổi); lịch chưa có file thì tạo mới
            entity.ContentPath = await _templates.WriteScheduleAsync(
                entity.ID, entity.CreateTime == default ? BaseUtil.TimeServer.GetTimeServer() : entity.CreateTime,
                command.Content!, entity.ContentPath);

            StampRunMarkers(entity);

            try
            {
                _scheduleRep.Context.Ado.BeginTran();

                await _scheduleRep.UpdateWithDiffLogAsync(entity);

                // Xoá cứng item cũ rồi ghi lại: item chỉ là chi tiết của lịch, không cần giữ vết.
                // Dùng xoá cứng thay vì xoá mềm để bảng không phình sau nhiều lần sửa lịch.
                await _itemRep.DeleteAsync(u => u.ScheduleId == entity.ID);
                await InsertItemsAsync(entity.ID, command.Items);

                _scheduleRep.Context.Ado.CommitTran();
            }
            catch
            {
                try { _scheduleRep.Context.Ado.RollbackTran(); } catch { }
                throw;
            }

            // Job Hangfire của lịch: cập nhật cron theo quy tắc chạy mới — SAU khi commit DB
            _jobScheduler.Register(entity);
            await AddToAddressBookAsync(entity);
            ClearCache();
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Bật / tắt 1 lịch gửi báo cáo
        /// Created date: 14/08/2026
        /// </summary>
        public async Task HandleAsync(MailToggleScheduleInput command)
        {
            var entity = await _scheduleRep.GetByIdAsync(command.ID)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            entity.Status = command.Status;

            // Bật lại lịch phải dập lại mốc, nếu không job gửi của lịch sẽ thấy mốc đến hạn cũ chưa gửi
            // và phóng ngay một thư của kỳ đã qua từ lâu.
            if (command.Status == BaseEnums.StatusEnum.Enable)
                StampRunMarkers(entity);

            await _scheduleRep.UpdateWithDiffLogAsync(entity);

            // Bật thì tạo lại job, tắt thì gỡ job
            _jobScheduler.Register(entity);
            ClearCache();
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Xóa 1 lịch gửi báo cáo (xóa mềm lịch, xóa cứng item của nó)
        /// Created date: 14/08/2026
        /// </summary>
        public async Task HandleAsync(MailDeleteScheduleInput command)
        {
            var entity = await _scheduleRep.GetFirstAsync(u => u.ID == command.ID)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            try
            {
                _scheduleRep.Context.Ado.BeginTran();

                await _scheduleRep.SoftDeleteAsync<MailSchedule>(entity);
                await _itemRep.DeleteAsync(u => u.ScheduleId == entity.ID);

                _scheduleRep.Context.Ado.CommitTran();
            }
            catch
            {
                try { _scheduleRep.Context.Ado.RollbackTran(); } catch { }
                throw;
            }

            _jobScheduler.Remove(new[] { entity.ID });
            ClearCache();
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Xóa nhiều lịch gửi báo cáo
        /// Created date: 14/08/2026
        /// </summary>
        public async Task HandleAsync(List<MailDeleteScheduleInput> command)
        {
            var ids = command.Select(item => item.ID).ToList();

            var entities = await _scheduleRep.AsQueryable()
                .Where(u => ids.Contains(u.ID))
                .ToListAsync();

            if (entities?.Count != ids.Count)
                throw Oops.Oh(BaseLocaleManager.BaseException.DataNotMatch);

            try
            {
                _scheduleRep.Context.Ado.BeginTran();

                await _scheduleRep.SoftDeleteAsync<MailSchedule>(u => ids.Contains(u.ID));
                await _itemRep.DeleteAsync(u => ids.Contains(u.ScheduleId));

                _scheduleRep.Context.Ado.CommitTran();
            }
            catch
            {
                try { _scheduleRep.Context.Ado.RollbackTran(); } catch { }
                throw;
            }

            _jobScheduler.Remove(ids);
            ClearCache();
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Thêm địa chỉ To / CC / BCC của lịch vào sổ địa chỉ MailAddress (chỉ địa chỉ chưa có).
        ///              Chạy sau khi lưu xong; lỗi chỉ ghi cảnh báo, không ảnh hưởng việc lưu lịch.
        /// Created date: 02/10/2026
        /// </summary>
        private async Task AddToAddressBookAsync(MailSchedule entity)
        {
            try
            {
                var list = MailAddressList.Split(entity.Recipients)
                    .Concat(MailAddressList.Split(entity.Cc))
                    .Concat(MailAddressList.Split(entity.Bcc))
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim().ToLowerInvariant())
                    .Distinct(StringComparer.Ordinal)
                    .ToList();
                if (list.Count == 0) return;

                var existing = await _addressRep.AsQueryable()
                    .Where(u => u.IsDelete == null && list.Contains(u.Email))
                    .Select(u => u.Email)
                    .ToListAsync();

                var known = new HashSet<string>(existing.Select(x => x.ToLowerInvariant()), StringComparer.Ordinal);
                var missing = list
                    .Where(x => !known.Contains(x))
                    .Select(x => new MailAddress
                    {
                        Email = x,
                        Code = x.Length > 64 ? x[..64] : x,
                        CreateUId = null,
                    })
                    .ToList();

                if (missing.Count > 0)
                    await _addressRep.InsertRangeAsync(missing);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không ghi được sổ địa chỉ email (MailAddress)");
            }
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Ghi danh sách báo cáo đính kèm của một lịch
        /// Created date: 14/08/2026
        /// </summary>
        private async Task InsertItemsAsync(string scheduleId, List<MailScheduleItemInput> items)
        {
            var entities = new List<MailScheduleItem>();
            var order = 1;

            foreach (var item in items)
            {
                var entity = item.Adapt<MailScheduleItem>();
                entity.ScheduleId = scheduleId;
                entity.OrderNo = item.OrderNo ?? order;
                // Mã item không có ý nghĩa nghiệp vụ nhưng EntityTenant.Code là NOT NULL
                entity.Code = $"{scheduleId}_{order:00}";
                entity.CreateUId = null;

                entities.Add(entity);
                order++;
            }

            if (entities.Count > 0)
                await _itemRep.InsertRangeAsync(entities);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Dập mốc chạy cho lịch vừa lưu.
        ///
        ///              LastRunKey được gán bằng mốc đến hạn ngay TRƯỚC hiện tại và coi như "đã
        ///              gửi". Thiếu bước này thì tạo lịch hàng tháng vào ngày 15 với ngày chạy 01
        ///              sẽ bị job gửi của lịch gửi ngay một thư của mốc ngày 01 vừa qua.
        ///
        ///              NextTimeRun chỉ để hiển thị; job không đọc nên sai lệch cũng vô hại.
        /// Created date: 17/08/2026
        /// </summary>
        private static void StampRunMarkers(MailSchedule entity)
        {
            var now = BaseUtil.TimeServer.GetTimeServer();

            var rule = MailRptScheduleRule.From(entity);

            entity.LastRunKey = RptScheduleTime.GetRunKey(RptScheduleTime.GetLastDueTime(rule, now));
            entity.NextTimeRun = RptScheduleTime.GetNextDueTime(rule, now);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Xoá cache của cả lịch và item
        /// Created date: 14/08/2026
        /// </summary>
        private void ClearCache()
        {
            _cache.RemoveByPrefixKey(CacheConst.Mail.RptEmailSchedule);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Chuẩn hoá và kiểm tra nghiệp vụ, dùng chung cho Add và Update.
        /// Created date: 14/08/2026
        /// </summary>
        private async Task NormalizeAsync(MailAddScheduleInput command)
        {
            /* ── Phần lịch chạy ── */

            // Tần suất không còn suy từ báo cáo được nữa (một thư nhiều kỳ khác nhau),
            // nên bỏ trống thì mặc định hàng tháng chứ không đoán theo item.
            command.Frequency = string.IsNullOrWhiteSpace(command.Frequency)
                ? MailRptPeriodType.Month
                : command.Frequency.Trim().ToLowerInvariant();

            if (!MailRptPeriodType.All.Contains(command.Frequency))
                throw Oops.Oh(BaseMsg.Mail.Validation.FieldInvalid, Furion.Localization.L.Text[BaseMsg.Mail.Entity.Frequency]);

            NormalizeRunRule(command);

            command.HourOfDay = Math.Clamp(command.HourOfDay, 0, 23);
            command.MinuteOfHour = Math.Clamp(command.MinuteOfHour, 0, 59);

            /* ── Người nhận ── */

            // Loại địa chỉ sai định dạng NGAY BÂY GIỜ — để lọt xuống job thì lỗi chỉ hiện trong bảng log, người tạo
            // lịch không biết. Bỏ trùng GIỮA các ô (To → CC → BCC); giới hạn tính trên TỔNG ba ô (máy chủ SMTP đếm
            // mọi người nhận), cùng giới hạn MailSender dùng lúc gửi.
            var mail = MailAddressList.Parse(command.Recipients, command.Cc, command.Bcc);

            var invalid = mail.To.Concat(mail.Cc).Concat(mail.Bcc).Where(x => !_mailSender.IsValidEmail(x)).ToList();
            if (invalid.Count > 0)
                throw Oops.Oh(BaseMsg.Mail.Validation.InvalidRecipient, string.Join(", ", invalid));

            if (mail.To.Count == 0)
                throw Oops.Oh(BaseMsg.Mail.Validation.FieldRequired, Furion.Localization.L.Text[BaseMsg.Mail.Entity.Recipients]);

            var config = await _configService.GetAsync();
            var maxRecipients = config.MaxRecipientsPerMail;
            if (mail.Count > maxRecipients)
                throw Oops.Oh(BaseMsg.Mail.Validation.TooManyRecipients, maxRecipients);

            command.Recipients = MailAddressList.Join(mail.To)!;
            command.Cc = MailAddressList.Join(mail.Cc);
            command.Bcc = MailAddressList.Join(mail.Bcc);

            command.Status ??= BaseEnums.StatusEnum.Enable;

            /* ── Tiêu đề, nội dung thư ── */

            NormalizeContent(command);

            /* ── Danh sách báo cáo đính kèm ── */

            if (command.Items == null || command.Items.Count == 0)
                throw Oops.Oh(BaseMsg.Mail.Validation.ItemsRequired);

            var maxItems = config.MaxItemsPerSchedule;
            if (command.Items.Count > maxItems)
                throw Oops.Oh(BaseMsg.Mail.Validation.TooManyItems, maxItems);

            var catalog = await _reportService.LoadAsync();
            var periodOffsets = await _configService.GetPeriodOffsetsAsync();

            for (var i = 0; i < command.Items.Count; i++)
                NormalizeItem(command.Items[i], i + 1, catalog, periodOffsets, command.Frequency);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Chuẩn hoá tiêu đề và nội dung thư.
        ///
        ///              Tiêu đề bỏ trống thì lấy tiêu đề mặc định — cột Subject là NOT NULL. Nội dung thư (HTML từ
        ///              wangEditor) bắt buộc và được làm sạch trước khi ghi file. KHÔNG chặn tag lạ: tag do người dùng
        ///              kéo thả từ danh mục, tag gõ sai chỉ được cảnh báo ở bản xem trước. Mã mẫu chỉ là thông tin
        ///              hiển thị (nội dung đã chép sang lịch) nên không kiểm tra mẫu còn tồn tại.
        /// Created date: 29/09/2026
        /// </summary>
        private static void NormalizeContent(MailAddScheduleInput command)
        {
            command.TemplateCode = string.IsNullOrWhiteSpace(command.TemplateCode) ? null : command.TemplateCode.Trim();

            command.Subject = string.IsNullOrWhiteSpace(command.Subject)
                ? MailConst.DefaultSubject
                : command.Subject.Trim();

            command.Content = MailHtmlSanitizer.Sanitize(command.Content);
            if (string.IsNullOrWhiteSpace(command.Content))
                throw Oops.Oh(BaseMsg.Mail.Validation.FieldRequired, Furion.Localization.L.Text[BaseMsg.Mail.Entity.Content]);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Chuẩn hoá các trường "khi nào chạy" theo tần suất.
        ///
        ///              Trường không thuộc tần suất đang chọn bị XOÁ về null, không để lại giá trị cũ:
        ///              đổi lịch từ quý sang tuần mà còn MonthInPeriod = 3 thì đọc DB sẽ hiểu sai.
        ///                - day    : DaysOfWeek BẮT BUỘC ít nhất 1 thứ (không chọn = không chạy)
        ///                - week   : DaysOfWeek bắt buộc đúng 1 thứ
        ///                - month  : DayOfMonth 1-31
        ///                - quarter: DayOfMonth 1-31 + MonthInPeriod 1-3
        ///                - year   : DayOfMonth 1-31 + MonthInPeriod 1-12
        /// Created date: 30/09/2026
        /// </summary>
        private static void NormalizeRunRule(MailAddScheduleInput command)
        {
            var days = RptScheduleTime.ParseDaysOfWeek(command.DaysOfWeek);

            switch (command.Frequency)
            {
                case MailRptPeriodType.Day:
                    if (days.Count == 0)
                        throw Oops.Oh(BaseMsg.Mail.Validation.FieldRequired, Furion.Localization.L.Text[BaseMsg.Mail.Entity.DaysOfWeek]);

                    command.DayOfMonth = null;
                    command.MonthInPeriod = null;
                    // Lưu tường minh kể cả đủ 7 thứ ("1,2,3,4,5,6,7") — null chỉ còn ở lịch cũ
                    command.DaysOfWeek = RptScheduleTime.FormatDaysOfWeek(days);
                    break;

                case MailRptPeriodType.Week:
                    if (days.Count == 0)
                        throw Oops.Oh(BaseMsg.Mail.Validation.FieldRequired, Furion.Localization.L.Text[BaseMsg.Mail.Entity.DaysOfWeek]);
                    if (days.Count > 1)
                        throw Oops.Oh(BaseMsg.Mail.Validation.WeeklyDayOfWeekSingle);

                    command.DayOfMonth = null;
                    command.MonthInPeriod = null;
                    command.DaysOfWeek = RptScheduleTime.FormatDaysOfWeek(days);
                    break;

                case MailRptPeriodType.Quarter:
                case MailRptPeriodType.Year:
                    {
                        var maxMonth = command.Frequency == MailRptPeriodType.Quarter
                            ? MailConst.MonthsPerQuarter
                            : MailConst.MonthsPerYear;

                        if (command.MonthInPeriod is { } m && (m < 1 || m > maxMonth))
                            throw Oops.Oh(BaseMsg.Mail.Validation.FieldInvalid, Furion.Localization.L.Text[BaseMsg.Mail.Entity.MonthInPeriod]);

                        // Cho tới 31: job gửi của lịch tự dồn về ngày cuối tháng nên không mất tháng nào.
                        command.DayOfMonth = Math.Clamp(command.DayOfMonth ?? 1, 1, MailConst.MaxDayOfMonth);
                        command.MonthInPeriod = command.MonthInPeriod ?? 1;
                        command.DaysOfWeek = null;
                        break;
                    }

                default: // month
                    command.DayOfMonth = Math.Clamp(command.DayOfMonth ?? 1, 1, MailConst.MaxDayOfMonth);
                    command.MonthInPeriod = null;
                    command.DaysOfWeek = null;
                    break;
            }
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Chuẩn hoá một báo cáo đính kèm.
        /// Created date: 14/08/2026
        /// </summary>
        private static void NormalizeItem(MailScheduleItemInput item, int line, RptReportCatalog catalog,
            HashSet<int> periodOffsets, string? frequency)
        {
            item.ReportCode = item.ReportCode?.Trim() ?? string.Empty;

            // Phải có trong danh mục RptEmailReport (đang bật) VÀ đã có nhánh kết xuất trong code
            if (!catalog.IsActive(item.ReportCode))
                throw Oops.Oh(BaseMsg.Mail.Validation.ItemReportNotActive, line, item.ReportCode);

            // Loại kỳ không nhận từ client: báo cáo có kỳ cố định → kỳ đó (lệch tần suất KHÔNG chặn, form chỉ cảnh
            // báo); báo cáo nhận khoảng ngày → theo tần suất lịch. Người dùng chỉ chọn kỳ trước / hiện tại.
            item.PeriodType = RptReportCode.ResolvePeriodType(item.ReportCode, frequency);

            item.Format = string.IsNullOrWhiteSpace(item.Format)
                ? MailConst.FileFormat.Pdf
                : item.Format.Trim().ToLowerInvariant();

            // Kỳ hiện tại / kỳ trước: lưu đúng lựa chọn của người dùng, chỉ nhận giá trị khai ở mail_period
            if (!periodOffsets.Contains(item.PeriodOffset))
                throw Oops.Oh(BaseMsg.Mail.Validation.ItemPeriodOffsetInvalid, line, item.PeriodOffset);
        }
    }
}
