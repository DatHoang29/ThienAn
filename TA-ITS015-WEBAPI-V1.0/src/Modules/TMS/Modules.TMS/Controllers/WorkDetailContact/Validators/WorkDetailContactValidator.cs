using FluentValidation;
using Microsoft.Extensions.Localization;
using Modules.TMS.Controllers.WorkDetailContact.Dto;
using Modules.TMS.Core.Exceptions;
using Shared.DTO.Constants.Application;

namespace Modules.TMS.Controllers.WorkDetailContact.Validators
{

    public class WorkDetailContactValidator : AbstractValidator<AddTmsWorkDetailContactInput>
    {
        public WorkDetailContactValidator(IStringLocalizer<AddTmsWorkDetailContactInput> localizer, bool isCreate = true)
        {
            RuleFor(x => x.WorkDetailId)
                .MaximumLength(EntityConst.KeyFieldLength)
                    .WithName(x => localizer[BaseMsg.Tms.Entity.WorkDetailId])
                    .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.WorkContactId)
                .MaximumLength(EntityConst.KeyFieldLength)
                    .WithName(x => localizer[BaseMsg.Tms.Entity.WorkContactId])
                    .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.OrderNo)
                .GreaterThanOrEqualTo(0)
                    .WithName(x => localizer[BaseMsg.BaseEntity.OrderNo])
                    .WithMessage(x => localizer[BaseMsg.BaseValidation.GreaterThanOrEqual]);

        }
    }

    public class AddWorkDetailContactValidator : AbstractValidator<AddTmsWorkDetailContactInput>
    {
        public AddWorkDetailContactValidator(IStringLocalizer<AddTmsWorkDetailContactInput> localizer)
        {
            Include(new WorkDetailContactValidator(localizer));

        }
    }

    public class UpdateWorkDetailContactValidator : AbstractValidator<UpdateTmsWorkDetailContactInput>
    {
        public UpdateWorkDetailContactValidator(IStringLocalizer<AddTmsWorkDetailContactInput> localizer)
        {
            Include(new WorkDetailContactValidator(localizer, false));

        }
    }

    public class DeleteWorkDetailContactValidator : AbstractValidator<DeleteTmsWorkDetailContactInput>
    {
        public DeleteWorkDetailContactValidator(IStringLocalizer<AddTmsWorkDetailContactInput> localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(x => localizer[BaseMsg.BaseEntity.ID])
                    .WithMessage(x => localizer[BaseMsg.BaseValidation.Required]);

        }
    }
}
