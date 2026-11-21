using FluentValidation;
using Microsoft.Extensions.Localization;
using Modules.TMS.Controllers.WorkDetail.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Exceptions;
using Shared.DTO.Constants.Application;

namespace Modules.TMS.Controllers.WorkDetail.Validators
{
    /// <summary>
    /// Author: TuanHA
    /// Created date: 15/01/2025
    /// </summary>
    public class WorkDetailValidator : AbstractValidator<TmsWorkDetail>
    {
        public WorkDetailValidator(IStringLocalizer localizer, bool isCreate = true)
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithName(x => localizer[BaseMsg.BaseEntity.Name])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .MaximumLength(EntityConst.Length64)
                .WithName(x => localizer[BaseMsg.BaseEntity.Name])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Code)
                .NotEmpty()
                .WithName(x => localizer[BaseMsg.BaseEntity.Code])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .MaximumLength(EntityConst.Length64)
                .WithName(x => localizer[BaseMsg.BaseEntity.Code])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Status)
               .NotEmpty()
               .WithName(x => localizer[BaseMsg.BaseEntity.Status])
               .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
               .When(x => (x.Status != null && !isCreate) || isCreate)
               .IsInEnum()
               .WithMessage(localizer[BaseMsg.BaseValidation.MustBeValidValue]);

            RuleFor(x => x.WorkType)
                .MaximumLength(EntityConst.ConfigFieldLength)
                .WithName(x => localizer[BaseMsg.TmsWorkDetail.Entity.WorkType])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.WorkMode)
                .MaximumLength(EntityConst.ConfigFieldLength)
                .WithName(x => localizer[BaseMsg.TmsWorkDetail.Entity.WorkMode])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.ControlType)
                .MaximumLength(EntityConst.ConfigFieldLength)
                .WithName(x => localizer[BaseMsg.TmsWorkDetail.Entity.ControlType])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Remark)
                .MaximumLength(EntityConst.Length256)
                .WithName(x => localizer[BaseMsg.BaseEntity.Remark])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);


        }
    }

    //TODO: check again

    /// <summary>
    /// Author: TuanHA
    /// Created date: 15/01/2025
    /// </summary>
    public class AddWorkDetailValidator : AbstractValidator<AddWorkDetailInput>
    {
        public AddWorkDetailValidator(IStringLocalizer localizer)
        {
            Include(new WorkDetailValidator(localizer));
        }
    }

    /// <summary>
    /// Author: TuanHA
    /// Created date: 15/01/2025
    /// </summary>
    public class UpdateWorkDetailValidator : AbstractValidator<UpdateWorkDetailInput>
    {
        public UpdateWorkDetailValidator(IStringLocalizer localizer)
        {
            Include(new WorkDetailValidator(localizer, false));

        }
    }

    /// <summary>
    /// Author: TuanHA
    /// Created date: 15/01/2025
    /// </summary>
    public class DeleteWorkDetailValidator : AbstractValidator<DeleteWorkDetailInput>
    {
        public DeleteWorkDetailValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                .WithName(x => localizer[BaseMsg.BaseEntity.ID])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.Required]);
        }
    }
}
