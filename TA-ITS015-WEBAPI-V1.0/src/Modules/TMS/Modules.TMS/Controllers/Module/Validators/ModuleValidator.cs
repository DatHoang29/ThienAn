using FluentValidation;
using Microsoft.Extensions.Localization;
using Modules.TMS.Controllers.Module.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Exceptions;
using Shared.DTO.Constants.Application;

namespace Modules.TMS.Controllers.Module.Validators
{
    /// <summary>
    /// Author: TuanHA
    /// Created date: 15/01/2025
    /// </summary>
    public class TmsModuleValidator : AbstractValidator<TmsModule>
    {
        public TmsModuleValidator(IStringLocalizer localizer, bool isCreate = true)
        {
            RuleFor(x => x.KeyName)
                .NotEmpty()
                .WithName(x => localizer[BaseMsg.TmsModule.Entity.KeyName])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .When(x => isCreate)
                .MaximumLength(EntityConst.Length64)
                .WithName(x => localizer[BaseMsg.TmsModule.Entity.KeyName])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Name)
                .NotEmpty()
                .WithName(x => localizer[BaseMsg.BaseEntity.Name])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .When(x => isCreate)
                .MaximumLength(EntityConst.Length64)
                .WithName(x => localizer[BaseMsg.BaseEntity.Name])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Status)
               .NotEmpty()
               .WithName(x => localizer[BaseMsg.BaseEntity.Status])
               .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
               .When(x => (x.Status != null && !isCreate) || isCreate)
               .IsInEnum()
               .WithMessage(localizer[BaseMsg.BaseValidation.MustBeValidValue]);

            RuleFor(x => x.Remark)
                .MaximumLength(EntityConst.Length256)
                .WithName(x => localizer[BaseMsg.BaseEntity.Remark])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.PluginId)
                .MaximumLength(EntityConst.KeyFieldLength)
                .WithName(x => localizer[BaseMsg.TmsModule.Entity.PluginId])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.KeyName)
                .NotEmpty()
                    .WithName(x => localizer[BaseMsg.TmsModule.Entity.KeyName])
                    .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .MaximumLength(EntityConst.Length64)
                    .WithName(x => localizer[BaseMsg.TmsModule.Entity.KeyName])
                    .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Name)
                .NotEmpty()
                    .WithName(x => localizer[BaseMsg.BaseEntity.Name])
                    .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .MaximumLength(EntityConst.Length64)
                    .WithName(x => localizer[BaseMsg.BaseEntity.Name])
                    .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

        }
    }

    /// <summary>
    /// Author: TuanHA
    /// Created date: 15/01/2025
    /// </summary>
    public class AddTmsModuleValidator : AbstractValidator<AddTmsModuleInput>
    {
        public AddTmsModuleValidator(IStringLocalizer localizer)
        {
            Include(new TmsModuleValidator(localizer));
        }
    }

    /// <summary>
    /// Author: TuanHA
    /// Created date: 15/01/2025
    /// </summary>
    public class UpdateTmsModuleValidator : AbstractValidator<UpdateTmsModuleInput>
    {
        public UpdateTmsModuleValidator(IStringLocalizer localizer)
        {
            Include(new TmsModuleValidator(localizer, false));
        }
    }

    /// <summary>
    /// Author: TuanHA
    /// Created date: 15/01/2025
    /// </summary>
    public class DeleteTmsModuleValidator : AbstractValidator<DeleteTmsModuleInput>
    {
        public DeleteTmsModuleValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                .WithName(localizer[BaseMsg.BaseEntity.ID])
                .WithMessage(localizer[BaseMsg.BaseValidation.Required]);

        }
    }
}
