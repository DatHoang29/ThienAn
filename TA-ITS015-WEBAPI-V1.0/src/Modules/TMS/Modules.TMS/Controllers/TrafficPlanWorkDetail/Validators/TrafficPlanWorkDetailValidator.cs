using FluentValidation;
using Microsoft.Extensions.Localization;
using Modules.TMS.Controllers.TrafficPlanWorkDetail.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Exceptions;
using Shared.DTO.Constants.Application;

namespace Modules.TMS.Controllers.TrafficPlanWorkDetail.Validators
{
    /// <summary>
    /// Author: TuanHA
    /// Created date: 15/01/2025
    /// </summary>
    public class TmsTrafficPlanWorkDetailValidator : AbstractValidator<TmsTrafficPlanWorkDetails>
    {
        public TmsTrafficPlanWorkDetailValidator(IStringLocalizer localizer, bool isCreate = true)
        {
            RuleFor(x => x.WorkDetailId)
                .NotEmpty()
                .WithName(x => localizer[BaseMsg.TmsTrafficPlanWorkDetail.Entity.WorkDetailId])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .When(x => isCreate)
                .MaximumLength(EntityConst.KeyFieldLength)
                .WithName(x => localizer[BaseMsg.TmsTrafficPlanWorkDetail.Entity.WorkDetailId])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.TrafficPlanId)
                .NotEmpty()
                .WithName(x => localizer[BaseMsg.TmsTrafficPlanWorkDetail.Entity.TrafficPlanId])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .When(x => isCreate)
                .MaximumLength(EntityConst.KeyFieldLength)
                .WithName(x => localizer[BaseMsg.TmsTrafficPlanWorkDetail.Entity.TrafficPlanId])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.ExecuteOrgId)
                .NotEmpty()
                .WithName(x => localizer[BaseMsg.TmsTrafficPlanWorkDetail.Entity.ExecuteOrgId])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .When(x => isCreate)
                .MaximumLength(EntityConst.KeyFieldLength)
                .WithName(x => localizer[BaseMsg.TmsTrafficPlanWorkDetail.Entity.ExecuteOrgId])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Step)
                .GreaterThanOrEqualTo(0)
                .WithName(x => localizer[BaseMsg.TmsTrafficPlanWorkDetail.Entity.Step])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.GreaterThanOrEqual]);

            RuleFor(x => x.OrderNo)
                 .GreaterThanOrEqualTo(0)
                 .WithName(x => localizer[BaseMsg.BaseEntity.OrderNo])
                 .WithMessage(x => localizer[BaseMsg.BaseValidation.GreaterThanOrEqual]);

            RuleFor(x => x.State)
                .MaximumLength(EntityConst.Length16)
                .WithName(x => localizer[BaseMsg.TmsTrafficPlanWorkDetail.Entity.State])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);


        }
    }

    /// <summary>
    /// Author: TuanHA
    /// Created date: 15/01/2025
    /// </summary>
    public class AddTmsTrafficPlanWorkDetailValidator : AbstractValidator<AddTmsTrafficPlanWorkDetailInput>
    {
        public AddTmsTrafficPlanWorkDetailValidator(IStringLocalizer localizer)
        {
            Include(new TmsTrafficPlanWorkDetailValidator(localizer));
        }
    }

    /// <summary>
    /// Author: TuanHA
    /// Created date: 15/01/2025
    /// </summary>
    public class UpdateTmsTrafficPlanWorkDetailValidator : AbstractValidator<UpdateTmsTrafficPlanWorkDetailInput>
    {
        public UpdateTmsTrafficPlanWorkDetailValidator(IStringLocalizer localizer)
        {

            Include(new TmsTrafficPlanWorkDetailValidator(localizer, false));
        }
    }

    /// <summary>
    /// Author: TuanHA
    /// Created date: 15/01/2025
    /// </summary>
    public class DeleteTmsTrafficPlanWorkDetailValidator : AbstractValidator<DeleteTmsTrafficPlanWorkDetailInput>
    {
        public DeleteTmsTrafficPlanWorkDetailValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                .WithName(x => localizer[BaseMsg.BaseEntity.ID])
                .WithMessage(localizer[BaseMsg.BaseValidation.Required]);
        }
    }
}
