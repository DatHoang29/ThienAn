using FluentValidation;
using Microsoft.Extensions.Localization;
using Module.VideoWall.Core.Dto.EventRule;
using Shared.DTO.Constants.Application;
using Shared.DTO.Constants.Localization;

namespace Module.VideoWall.Controllers.EventRule.Validators
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Rule dùng chung cho Add và Update VwEventRule
    /// Created date: 2026-08-01
    /// </summary>
    public class VwEventRuleValidator : AbstractValidator<VwAddEventRuleInput>
    {
        public VwEventRuleValidator(IStringLocalizer localizer, bool isCreate = true)
        {
            RuleFor(x => x.Code)
                .NotEmpty()
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Code])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.Required])
                .When(x => (x.Code != null && !isCreate) || isCreate)
                .MaximumLength(EntityConst.Length64)
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Code])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.EventSource)
                .MaximumLength(EntityConst.Length128)
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Info])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.Priority)
                .MaximumLength(EntityConst.Length64)
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Info])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.Remark)
                .MaximumLength(EntityConst.Length256)
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Remark])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.Status)
                .NotEmpty()
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Status])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.Required])
                .When(x => (x.Status != null && !isCreate) || isCreate)
                .IsInEnum()
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MustBeValidValue]);
        }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Validator cho thêm mới VwEventRule
    /// Created date: 2026-08-01
    /// </summary>
    public class VwAddEventRuleValidator : AbstractValidator<VwAddEventRuleInput>
    {
        public VwAddEventRuleValidator(IStringLocalizer localizer)
        {
            Include(new VwEventRuleValidator(localizer));
        }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Validator cho cập nhật VwEventRule
    /// Created date: 2026-08-01
    /// </summary>
    public class VwUpdateEventRuleValidator : AbstractValidator<VwUpdateEventRuleInput>
    {
        public VwUpdateEventRuleValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(localizer[BaseLocaleManager.BaseEntity.ID])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required]);

            Include(new VwEventRuleValidator(localizer, isCreate: false));
        }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Validator cho xóa VwEventRule
    /// Created date: 2026-08-01
    /// </summary>
    public class VwDeleteEventRuleValidator : AbstractValidator<VwDeleteEventRuleInput>
    {
        public VwDeleteEventRuleValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(localizer[BaseLocaleManager.BaseEntity.ID])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required]);
        }
    }
}
