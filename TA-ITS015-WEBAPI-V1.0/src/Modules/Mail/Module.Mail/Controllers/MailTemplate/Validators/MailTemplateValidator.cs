using FluentValidation;
using Microsoft.Extensions.Localization;
using Shared.DTO.Constants.Application;
using Shared.DTO.Constants.Localization;
using Modules.Mail.Core.Dto.TemplateMail;

namespace Modules.Mail.Controllers.EmailTemplate.Validators
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Rule dùng chung cho Add và Update mẫu email.
    ///              Lớp generic để AddValidatorsFromAssembly KHÔNG tự đăng ký nó (chỉ dùng qua Include) —
    ///              tránh mỗi lỗi bị báo hai lần.
    /// Created date: 12/10/2026
    /// </summary>
    public class RptEmailTemplateRules<T> : AbstractValidator<T> where T : MailAddTemplateInput
    {
        public RptEmailTemplateRules(IStringLocalizer localizer)
        {
            // Độ dài 64 = MailSchedule.TemplateCode
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
    /// Description: Validator cho thêm mới mẫu email
    /// Created date: 12/10/2026
    /// </summary>
    public class RptAddEmailTemplateValidator : AbstractValidator<MailAddTemplateInput>
    {
        public RptAddEmailTemplateValidator(IStringLocalizer localizer)
        {
            Include(new RptEmailTemplateRules<MailAddTemplateInput>(localizer));
        }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Validator cho cập nhật mẫu email
    /// Created date: 12/10/2026
    /// </summary>
    public class RptUpdateEmailTemplateValidator : AbstractValidator<MailUpdateTemplateInput>
    {
        public RptUpdateEmailTemplateValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(localizer[BaseLocaleManager.BaseEntity.ID])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required]);

            Include(new RptEmailTemplateRules<MailUpdateTemplateInput>(localizer));
        }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Validator cho xóa mẫu email
    /// Created date: 12/10/2026
    /// </summary>
    public class RptDeleteEmailTemplateValidator : AbstractValidator<MailDeleteTemplateInput>
    {
        public RptDeleteEmailTemplateValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(localizer[BaseLocaleManager.BaseEntity.ID])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required]);
        }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Validator cho bật / tắt mẫu email
    /// Created date: 12/10/2026
    /// </summary>
    public class RptToggleEmailTemplateValidator : AbstractValidator<MailToggleTemplateInput>
    {
        public RptToggleEmailTemplateValidator(IStringLocalizer localizer)
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
