using FluentValidation;
using Modules.TMS.Core.Constants;
using Microsoft.Extensions.Localization;
using Modules.TMS.Controllers.EventType.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Exceptions;
using Shared.DTO.Constants.Application;

namespace Modules.TMS.Controllers.EventType.Validators
{
    /// <summary>
    /// Author: TuanHA
    /// Created date: 15/01/2025
    /// </summary>
    public class EventType : AbstractValidator<TmsEventType>
    {
        public EventType(IStringLocalizer localizer, bool isCreate = true)
        {
            RuleFor(x => x.Name)
               .NotEmpty()
               .WithName(x => localizer[BaseMsg.BaseEntity.Name])
               .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
               .When(x => isCreate)
               .MaximumLength(EntityConst.Length64)
               .WithName(x => localizer[BaseMsg.BaseEntity.Name])
               .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Size)
               .MaximumLength(EntityConst.ConfigFieldLength)
               .WithName(x => localizer[BaseMsg.BaseEntity.Size])
               .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Type)
               .NotEmpty()
               .WithName(x => localizer[BaseMsg.BaseEntity.Type])
               .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
               .When(x => isCreate)
               .MaximumLength(EntityConst.ConfigFieldLength)
               .WithName(x => localizer[BaseMsg.BaseEntity.Type])
               .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Type)
                .Must(value => IncidentEventGroupConst.Normalize(value) != null)
                .WithName(x => localizer[BaseMsg.BaseEntity.Type])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MustBeValidValue]);

            RuleFor(x => x.WarningType)
               .NotEmpty()
               .WithName(x => localizer[BaseMsg.EventType.Entity.WarningType])
               .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
               .When(x => isCreate)
               .MaximumLength(EntityConst.ConfigFieldLength)
               .WithName(x => localizer[BaseMsg.EventType.Entity.WarningType])
               .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Status)
               .NotEmpty()
               .WithName(x => localizer[BaseMsg.BaseEntity.Status])
               .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
               .When(x => (x.Status != null && !isCreate) || isCreate)
               .IsInEnum()
               .WithMessage(localizer[BaseMsg.BaseValidation.MustBeValidValue]);

            RuleFor(x => x.Priority)
               .GreaterThanOrEqualTo(0)
               .WithName(x => localizer[BaseMsg.EventType.Entity.Priority])
               .WithMessage(x => localizer[BaseMsg.BaseValidation.GreaterThanOrEqual]);

            RuleFor(x => x.Remark)
                .MaximumLength(EntityConst.Length256)
                .WithName(x => localizer[BaseMsg.BaseEntity.Remark])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
        }
    }

    /// <summary>
    /// Author: TuanHA
    /// Created date: 15/01/2025
    /// </summary>
    public class AddEventTypeValidator : AbstractValidator<AddEventTypeInput>
    {
        public AddEventTypeValidator(IStringLocalizer localizer)
        {
            Include(new EventType(localizer));
        }
    }

    /// <summary>
    /// Author: TuanHA
    /// Created date: 15/01/2025
    /// </summary>
    public class UpdateEventTypeValidator : AbstractValidator<UpdateEventTypeInput>
    {
        public UpdateEventTypeValidator(IStringLocalizer localizer)
        {
            Include(new EventType(localizer, false));
        }
    }

    /// <summary>
    /// Author: TuanHA
    /// Created date: 15/01/2025
    /// </summary>
    public class DeleteEventTypeValidator : AbstractValidator<DeleteEventTypeInput>
    {
        public DeleteEventTypeValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                .WithName(x => localizer[BaseMsg.BaseEntity.ID])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.Required]);

        }
    }
}
