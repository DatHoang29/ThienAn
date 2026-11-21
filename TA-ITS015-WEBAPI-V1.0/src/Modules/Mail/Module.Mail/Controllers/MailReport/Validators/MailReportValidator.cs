using FluentValidation;
using Microsoft.Extensions.Localization;
using Shared.DTO.Constants.Application;
using Shared.DTO.Constants.Localization;
using Modules.Mail.Core.Dto.EmailReport;
using Modules.Mail.Core.Exceptions;

namespace Modules.Mail.Controllers.EmailReport.Validators
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Rule dùng chung cho Add và Update danh mục báo cáo gửi email.
    ///              Lớp generic để AddValidatorsFromAssembly KHÔNG tự đăng ký nó (chỉ dùng qua Include) —
    ///              tránh mỗi lỗi bị báo hai lần.
    /// Created date: 01/10/2026
    /// </summary>
    public class RptEmailReportRules<T> : AbstractValidator<T> where T : MailRptAddReportInput
    {
        public RptEmailReportRules(IStringLocalizer localizer)
        {
            // Độ dài 64 = RptEmailScheduleItem.ReportCode / RptManagement.Code
            RuleFor(x => x.Code)
                .NotEmpty()
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Code])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.Required])
                .MaximumLength(EntityConst.Length64)
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Code])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.Name)
                .NotEmpty()
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Name])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.Required])
                .MaximumLength(EntityConst.Length128)
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Name])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.PluginCode)
                .NotEmpty()
                    .WithName(x => localizer[BaseMsg.Mail.Entity.PluginCode])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.Required])
                .MaximumLength(EntityConst.Length64)
                    .WithName(x => localizer[BaseMsg.Mail.Entity.PluginCode])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);

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

            RuleFor(x => x.Remark)
                .MaximumLength(EntityConst.Length256)
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Remark])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);
        }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Validator cho thêm mới báo cáo gửi email
    /// Created date: 01/10/2026
    /// </summary>
    public class RptAddEmailReportValidator : AbstractValidator<MailRptAddReportInput>
    {
        public RptAddEmailReportValidator(IStringLocalizer localizer)
        {
            Include(new RptEmailReportRules<MailRptAddReportInput>(localizer));
        }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Validator cho cập nhật báo cáo gửi email
    /// Created date: 01/10/2026
    /// </summary>
    public class RptUpdateEmailReportValidator : AbstractValidator<MailRptUpdateReportInput>
    {
        public RptUpdateEmailReportValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(localizer[BaseLocaleManager.BaseEntity.ID])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required]);

            Include(new RptEmailReportRules<MailRptUpdateReportInput>(localizer));
        }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Validator cho xóa báo cáo gửi email
    /// Created date: 01/10/2026
    /// </summary>
    public class RptDeleteEmailReportValidator : AbstractValidator<MailRptDeleteReportInput>
    {
        public RptDeleteEmailReportValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(localizer[BaseLocaleManager.BaseEntity.ID])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required]);
        }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Validator cho bật / tắt báo cáo gửi email
    /// Created date: 01/10/2026
    /// </summary>
    public class RptToggleEmailReportValidator : AbstractValidator<MailRptToggleReportInput>
    {
        public RptToggleEmailReportValidator(IStringLocalizer localizer)
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
