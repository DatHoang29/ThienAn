using FluentValidation;
using Microsoft.Extensions.Localization;
using Module.VideoWall.Core.Dto.ControllerOutput;
using Shared.DTO.Constants.Application;
using Shared.DTO.Constants.Localization;

namespace Module.VideoWall.Controllers.ControllerOutput.Validators
{
    /// <summary>
    /// Description: Validator sửa cổng ra (chỉ trường web quản lý).
    /// Created date: 06/11/2026
    /// </summary>
    public class VwUpdateControllerOutputValidator : AbstractValidator<VwUpdateControllerOutputInput>
    {
        public VwUpdateControllerOutputValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(localizer[BaseLocaleManager.BaseEntity.ID])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required]);

            RuleFor(x => x.DisplayName)
                .MaximumLength(EntityConst.Length64)
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Name])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.Remark)
                .MaximumLength(EntityConst.Length256)
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Remark])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.Status)
                .IsInEnum()
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MustBeValidValue])
                .When(x => x.Status != null);
        }
    }
}
