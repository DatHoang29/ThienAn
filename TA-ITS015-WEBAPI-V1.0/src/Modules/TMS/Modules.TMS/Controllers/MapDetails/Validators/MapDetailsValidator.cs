using FluentValidation;
using Microsoft.Extensions.Localization;
using Modules.TMS.Controllers.MapDetails.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Exceptions;
using Shared.DTO.Constants.Application;

namespace Modules.TMS.Controllers.MapDetails.Validators
{
    /// <summary>
    /// Author: TuanHA
    /// Created date: 15/01/2025
    /// </summary>
    public class TmsMapDetailValidator : AbstractValidator<TmsMapDetail>
    {
        public TmsMapDetailValidator(IStringLocalizer localizer, bool isCreate = true)
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithName(x => localizer[BaseMsg.BaseEntity.Name])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .MaximumLength(EntityConst.Length64)
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.MapLocationId)
                .MaximumLength(EntityConst.Length64)
                .WithName(x => localizer[BaseMsg.TmsMapDetail.Entity.MapId])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.RefId)
                .MaximumLength(EntityConst.Length64)
                .WithName(x => localizer[BaseMsg.TmsMapDetail.Entity.RefId])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.RefType)
                .MaximumLength(EntityConst.Length32)
                .WithName(x => localizer[BaseMsg.TmsMapDetail.Entity.RefType])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.CoordY)
                .MaximumLength(EntityConst.Length32)
                .WithName(x => localizer[BaseMsg.TmsMapDetail.Entity.CoordY])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.CoordX)
                .MaximumLength(EntityConst.Length32)
                .WithName(x => localizer[BaseMsg.TmsMapDetail.Entity.CoordX])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(data => data.Remark)
                .MaximumLength(EntityConst.Length256)
                .WithName(x => localizer[BaseMsg.BaseEntity.Remark])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

        }
    }

    /// <summary>
    /// Author: TuanHA
    /// Created date: 15/01/2025
    /// </summary>
    public class AddTmsMapDetailValidator : AbstractValidator<AddTmsMapDetailInput>
    {
        public AddTmsMapDetailValidator(IStringLocalizer localizer)
        {
            Include(new TmsMapDetailValidator(localizer));
        }
    }

    /// <summary>
    /// Author: TuanHA
    /// Created date: 15/01/2025
    /// </summary>
    public class UpdateTmsMapDetailValidator : AbstractValidator<UpdateTmsMapDetailInput>
    {
        public UpdateTmsMapDetailValidator(IStringLocalizer localizer)
        {
            Include(new TmsMapDetailValidator(localizer, false));
        }
    }

    /// <summary>
    /// Author: TuanHA
    /// Created date: 15/01/2025
    /// </summary>
    public class DeleteTmsMapDetailValidator : AbstractValidator<DeleteTmsMapDetailInput>
    {
        public DeleteTmsMapDetailValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                .WithName(x => localizer[BaseMsg.BaseEntity.ID])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.Required]);
        }

    }
}