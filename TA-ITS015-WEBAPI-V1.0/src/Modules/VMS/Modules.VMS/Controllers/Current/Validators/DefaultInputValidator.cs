using FluentValidation;
using Microsoft.Extensions.Localization;
using Modules.VMS.Controllers.Current.Dto;
using Modules.VMS.Core.Entities;
using Modules.VMS.Core.Exceptions;
using Shared.DTO.Constants.Application;

namespace Modules.VMS.Controllers.Current.Validators
{
    public class DefaultInputValidator : AbstractValidator<VmsDefaultCurrentInput>
    {
        public DefaultInputValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.EquipmentId)
                .NotEmpty()
                .WithName(x => localizer[BaseMsg.BaseEntity.ID])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.Required]);

            RuleFor(x => x.DefaultRowData)
                .MaximumLength(EntityConst.Length1024)
                .WithName(x => localizer[BaseMsg.VmsCurrent.Entity.DefaultRowData])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.DefaultUrl)
                .MaximumLength(EntityConst.Length512)
                .WithName(x => localizer[BaseMsg.VmsCurrent.Entity.DefaultUrl])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength])
                .When(x => !string.IsNullOrEmpty(x.DefaultUrl) && !x.DefaultUrl.StartsWith("data:")); // bỏ qua khi là base64
        }
    }

    public class DeleteVmsDefaultCurrentInputValidator : AbstractValidator<DeleteVmsDefaultCurrentInput>
    {
        public DeleteVmsDefaultCurrentInputValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                .WithName(x => localizer[BaseMsg.BaseEntity.ID])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.Required]);
        }
    }
}
