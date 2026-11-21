using FluentValidation;
using Microsoft.Extensions.Localization;
using Shared.DTO.Constants.Application;
using Shared.DTO.Constants.Localization;
using Modules.Mail.Core.Constants;
using Modules.Mail.Core.Dto.EmailSchedule;
using Modules.Mail.Core.Exceptions;
using Modules.Mail.Core.Helper;

namespace Modules.Mail.Controllers.EmailSchedule.Validators
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Rule dùng chung cho Add và Update lịch gửi báo cáo.
    ///              Chỉ kiểm định dạng từng trường; ràng buộc liên trường (kỳ có khớp báo cáo,
    ///              cron có dựng được) nằm ở CommandHandler.Normalize.
    /// Created date: 14/08/2026
    /// </summary>
    public class RptEmailScheduleValidator : AbstractValidator<MailAddScheduleInput>
    {
        public RptEmailScheduleValidator(IStringLocalizer localizer, bool isCreate = true)
        {
            RuleFor(x => x.Code)
                .MaximumLength(EntityConst.Length64).NotEmpty()
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Code])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.Name)
                .MaximumLength(EntityConst.Length128).NotEmpty()
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Name])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            // Danh sách báo cáo đính kèm phải có ít nhất một dòng — thư không có tệp nào thì vô nghĩa.
            // Nội dung từng dòng do CommandHandler.NormalizeItem kiểm, vì cần tra bảng mã báo cáo
            // và trả lỗi kèm số dòng.
            // Số dòng tối đa (SysOpConfig mail_max_items, đọc bất đồng bộ) do CommandHandler kiểm.
            RuleFor(x => x.Items)
                .NotEmpty()
                    .WithMessage(x => localizer[BaseMsg.Mail.Validation.ItemsRequired]);

            RuleFor(x => x.Frequency)
                .NotEmpty()
                    .WithName(x => localizer[BaseMsg.Mail.Entity.Frequency])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.Required])
                .When(x => (x.Frequency != null && !isCreate) || isCreate)
                .MaximumLength(EntityConst.Length16)
                    .WithName(x => localizer[BaseMsg.Mail.Entity.Frequency])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.Recipients)
                .NotEmpty()
                    .WithName(x => localizer[BaseMsg.Mail.Entity.Recipients])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.Required])
                .When(x => (x.Recipients != null && !isCreate) || isCreate)
                .MaximumLength(EntityConst.Length1024)
                    .WithName(x => localizer[BaseMsg.Mail.Entity.Recipients])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.Cc)
                .MaximumLength(EntityConst.Length1024)
                    .WithName(x => localizer[BaseMsg.Mail.Entity.Cc])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.Bcc)
                .MaximumLength(EntityConst.Length1024)
                    .WithName(x => localizer[BaseMsg.Mail.Entity.Bcc])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            // Tiêu đề KHÔNG bắt buộc: bỏ trống thì CommandHandler lấy MailConst.DefaultSubject
            RuleFor(x => x.Subject)
                .MaximumLength(EntityConst.Length256)
                    .WithName(x => localizer[BaseMsg.Mail.Entity.Subject])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            // Format / PeriodType đã chuyển xuống từng báo cáo đính kèm,
            // vì một thư có thể gồm nhiều báo cáo với tham số khác nhau.
            RuleForEach(x => x.Items).SetValidator(new RptEmailScheduleItemValidator(localizer));

            RuleFor(x => x.Remark)
                .MaximumLength(EntityConst.Length256)
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Remark])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.TemplateCode)
                .MaximumLength(EntityConst.Length64)
                    .WithName(x => localizer[BaseMsg.Mail.Entity.TemplateCode])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            // Nội dung thư bắt buộc — CommandHandler.NormalizeContent kiểm SAU khi làm sạch HTML
            // (nội dung chỉ có thẻ rỗng / script thì coi như trống).

            // Cho tới 31 vì job gửi tự dồn ngày về ngày cuối tháng.
            // Đây là điểm khác cron: cron không dồn ngày nên "0 8 31 * *" mất 5 tháng mỗi năm.
            RuleFor(x => x.DayOfMonth)
                .InclusiveBetween(1, MailConst.MaxDayOfMonth)
                    .WithName(localizer[BaseMsg.Mail.Entity.DayOfMonth])
                    .WithMessage(localizer[BaseMsg.Mail.Validation.DayOfMonthRange])
                .When(x => x.DayOfMonth.HasValue);

            // Miền 1-12 là miền rộng nhất (năm); quý chỉ 1-3 — phần đó CommandHandler.NormalizeRunRule kiểm
            // vì cần biết tần suất.
            RuleFor(x => x.MonthInPeriod)
                .InclusiveBetween(1, MailConst.MonthsPerYear)
                    .WithName(localizer[BaseMsg.Mail.Entity.MonthInPeriod])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MustBeValidValue])
                .When(x => x.MonthInPeriod.HasValue);

            RuleFor(x => x.DaysOfWeek)
                .MaximumLength(EntityConst.Length32)
                    .WithName(x => localizer[BaseMsg.Mail.Entity.DaysOfWeek])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength])
                .Must(RptScheduleTime.IsValidDaysOfWeek)
                    .WithName(x => localizer[BaseMsg.Mail.Entity.DaysOfWeek])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MustBeValidValue]);

            RuleFor(x => x.HourOfDay)
                .InclusiveBetween(0, 23)
                    .WithName(localizer[BaseMsg.Mail.Entity.HourOfDay])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MustBeValidValue]);

            RuleFor(x => x.MinuteOfHour)
                .InclusiveBetween(0, 59)
                    .WithName(localizer[BaseMsg.Mail.Entity.HourOfDay])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MustBeValidValue]);

            RuleFor(x => x.Status)
                .IsInEnum()
                    .WithName(localizer[BaseLocaleManager.BaseEntity.Status])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MustBeValidValue])
                .When(x => x.Status != null);

            RuleFor(x => x.OrderNo)
                .GreaterThanOrEqualTo(0)
                    .WithName(localizer[BaseLocaleManager.BaseEntity.OrderNo])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.GreaterThanOrEqual])
                .When(x => x.OrderNo.HasValue);

        }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Rule cho một báo cáo đính kèm.
    ///              Chỉ kiểm ĐỘ DÀI và MIỀN GIÁ TRỊ của từng trường. Các ràng buộc cần tra danh
    ///              mục (mã báo cáo có được hỗ trợ, kỳ có khớp báo cáo)
    ///              nằm ở CommandHandler.NormalizeItem để thông báo lỗi kèm được số dòng.
    /// Created date: 14/08/2026
    /// </summary>
    public class RptEmailScheduleItemValidator : AbstractValidator<MailScheduleItemInput>
    {
        public RptEmailScheduleItemValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ReportCode)
                .NotEmpty()
                    .WithName(x => localizer[BaseMsg.Mail.Entity.ReportCode])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.Required])
                .MaximumLength(EntityConst.Length32)
                    .WithName(x => localizer[BaseMsg.Mail.Entity.ReportCode])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.PeriodType)
                .MaximumLength(EntityConst.Length16)
                    .WithName(x => localizer[BaseMsg.Mail.Entity.PeriodType])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            // PeriodOffset: danh sách hợp lệ ở SysConfigData (mail_period) — kiểm tra ở handler (NormalizeItem)

            RuleFor(x => x.Format)
                .MaximumLength(EntityConst.Length8)
                    .WithName(x => localizer[BaseMsg.Mail.Entity.Format])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.Remark)
                .MaximumLength(EntityConst.Length256)
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Remark])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.OrderNo)
                .GreaterThanOrEqualTo(0)
                    .WithName(localizer[BaseLocaleManager.BaseEntity.OrderNo])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.GreaterThanOrEqual])
                .When(x => x.OrderNo.HasValue);
        }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Validator cho thêm mới lịch gửi báo cáo
    /// Created date: 14/08/2026
    /// </summary>
    public class RptAddEmailScheduleValidator : AbstractValidator<MailAddScheduleInput>
    {
        public RptAddEmailScheduleValidator(IStringLocalizer localizer)
        {
            Include(new RptEmailScheduleValidator(localizer));
        }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Validator cho cập nhật lịch gửi báo cáo
    /// Created date: 14/08/2026
    /// </summary>
    public class RptUpdateEmailScheduleValidator : AbstractValidator<MailUpdateScheduleInput>
    {
        public RptUpdateEmailScheduleValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(localizer[BaseLocaleManager.BaseEntity.ID])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required]);

            Include(new RptEmailScheduleValidator(localizer, isCreate: false));
        }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Validator cho xóa lịch gửi báo cáo
    /// Created date: 14/08/2026
    /// </summary>
    public class RptDeleteEmailScheduleValidator : AbstractValidator<MailDeleteScheduleInput>
    {
        public RptDeleteEmailScheduleValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(localizer[BaseLocaleManager.BaseEntity.ID])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required]);
        }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Validator cho bật / tắt lịch gửi báo cáo
    /// Created date: 14/08/2026
    /// </summary>
    public class RptToggleEmailScheduleValidator : AbstractValidator<MailToggleScheduleInput>
    {
        public RptToggleEmailScheduleValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(localizer[BaseLocaleManager.BaseEntity.ID])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required]);

            RuleFor(x => x.Status)
                .IsInEnum()
                    .WithName(localizer[BaseLocaleManager.BaseEntity.Status])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MustBeValidValue]);
        }
    }
}
