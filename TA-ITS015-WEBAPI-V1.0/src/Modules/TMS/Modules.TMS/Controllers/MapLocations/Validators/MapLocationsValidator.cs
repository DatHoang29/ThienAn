using FluentValidation;
using Microsoft.Extensions.Localization;
using Modules.TMS.Controllers.MapLocations.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Exceptions;
using Shared.DTO.Constants.Application;

namespace Modules.TMS.Controllers.MapLocations.Validators
{
    /// <summary>
    /// Author: TuanHA
    /// Created date: 15/01/2025
    /// </summary>
    public class TmsMapLocationValidator : AbstractValidator<TmsMapLocation>
    {
        public TmsMapLocationValidator(IStringLocalizer localizer, bool isCreate = true)
        {
            RuleFor(data => data.Name)
              .NotEmpty()
              .WithName(x => localizer[BaseMsg.BaseEntity.Name])
              .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
              .When(x => isCreate)
              .MaximumLength(EntityConst.Length64)
              .WithName(x => localizer[BaseMsg.BaseEntity.Name])
              .WithMessage(localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(data => data.MapId)
             .NotEmpty()
             .WithName(x => localizer[BaseMsg.TmsMapLocation.Entity.MapId])
             .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
             .When(x => isCreate)
             .MaximumLength(EntityConst.KeyFieldLength)
             .WithName(x => localizer[BaseMsg.TmsMapLocation.Entity.MapId])
             .WithMessage(localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(data => data.Status)
               .NotEmpty()
               .WithName(x => localizer[BaseMsg.BaseEntity.Status])
               .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
               .When(x => (x.Status != null && !isCreate) || isCreate)
               .IsInEnum()
               .WithMessage(localizer[BaseMsg.BaseValidation.MustBeValidValue]);

            RuleFor(data => data.Config)
                .MaximumLength(EntityConst.Length512)
                .WithName(x => localizer[BaseMsg.TmsMapLocation.Entity.Config])
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
    public class AddTmsMapLocationValidator : AbstractValidator<AddTmsMapLocationInput>
    {
        public AddTmsMapLocationValidator(IStringLocalizer localizer)
        {
            Include(new TmsMapLocationValidator(localizer));

        }
    }

    /// <summary>
    /// Author: TuanHA
    /// Created date: 15/01/2025
    /// </summary>
    public class UpdateTmsMapLocationValidator : AbstractValidator<UpdateTmsMapLocationInput>
    {
        public UpdateTmsMapLocationValidator(IStringLocalizer localizer)
        {
            Include(new TmsMapLocationValidator(localizer, false));
        }
    }

    /// <summary>
    /// Author: TuanHA
    /// Created date: 15/01/2025
    /// </summary>
    public class DeleteTmsMapLocationValidator : AbstractValidator<DeleteTmsMapLocationInput>
    {
        public DeleteTmsMapLocationValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                .WithName(x => localizer[BaseMsg.BaseEntity.ID])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.Required]);

        }
    }
}
