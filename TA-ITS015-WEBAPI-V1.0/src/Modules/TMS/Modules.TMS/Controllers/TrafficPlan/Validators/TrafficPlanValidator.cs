using FluentValidation;
using Microsoft.Extensions.Localization;
using Modules.TMS.Controllers.TrafficPlan.Dto;
using Modules.TMS.Core.Constants;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Exceptions;
using Shared.DTO.Constants.Application;

namespace Modules.TMS.Controllers.TrafficPlan.Validators
{
    /// <summary>
    /// Author: TuanHA
    /// Created date: 15/01/2025
    /// </summary>
    public class TmsTrafficPlanValidator : AbstractValidator<TmsTrafficPlans>
    {
        public TmsTrafficPlanValidator(IStringLocalizer localizer, bool isCreate = true)
        {
            RuleFor(x => x.Code)
                .NotEmpty()
                .WithName(x => localizer[BaseMsg.BaseEntity.Code])
                .When(x => (x.Code != null && !isCreate) || isCreate)
                .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .MaximumLength(EntityConst.Length64)
                .WithName(x => localizer[BaseMsg.BaseEntity.Code])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Name)
                .NotEmpty()
                .WithName(x => localizer[BaseMsg.BaseEntity.Name])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .When(x => isCreate)
                .MaximumLength(EntityConst.Length64)
                .WithName(x => localizer[BaseMsg.BaseEntity.Name])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.EventTypeId)
                .NotEmpty()
                .WithName(x => localizer[BaseMsg.TrafficPlan.Entity.EventTypeId])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .When(x => isCreate)
                .MaximumLength(EntityConst.KeyFieldLength)
                .WithName(x => localizer[BaseMsg.TrafficPlan.Entity.EventTypeId])
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

            RuleFor(x => x.ExecutionMode)
                .Must(value => AllowedExecutionModes.Contains(value))
                .WithName(x => localizer[BaseMsg.TrafficPlan.Entity.ExecutionMode])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MustBeValidValue])
                .When(x => !string.IsNullOrWhiteSpace(x.ExecutionMode));
        }

        private static readonly string[] AllowedExecutionModes =
        {
            TrafficPlanConst.ExecutionMode.Manual,
            TrafficPlanConst.ExecutionMode.Automatic,
            TrafficPlanConst.ExecutionMode.Both
        };
    }

    /// <summary>
    /// Author: TuanHA
    /// Created date: 15/01/2025
    /// </summary>
    public class AddTmsTrafficPlanValidator : AbstractValidator<AddTmsTrafficPlanInput>
    {
        public AddTmsTrafficPlanValidator(IStringLocalizer localizer)
        {
            Include(new TmsTrafficPlanValidator(localizer));
        }
    }

    /// <summary>
    /// Author: TuanHA
    /// Created date: 15/01/2025
    /// </summary>
    public class UpdateTmsTrafficPlanValidator : AbstractValidator<UpdateTmsTrafficPlanInput>
    {
        public UpdateTmsTrafficPlanValidator(IStringLocalizer localizer)
        {
            Include(new TmsTrafficPlanValidator(localizer, false));
        }
    }

    /// <summary>
    /// Author: TuanHA
    /// Created date: 15/01/2025
    /// </summary>
    public class DeleteTmsTrafficPlanValidator : AbstractValidator<DeleteTmsTrafficPlanInput>
    {
        public DeleteTmsTrafficPlanValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                .WithName(x => localizer[BaseMsg.BaseEntity.ID])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.Required]);
        }
    }
}
