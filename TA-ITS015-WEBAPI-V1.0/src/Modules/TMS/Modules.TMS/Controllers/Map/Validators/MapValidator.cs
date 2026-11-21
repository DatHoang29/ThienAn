using FluentValidation;
using Microsoft.Extensions.Localization;
using Modules.TMS.Controllers.Map.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Exceptions;
using Shared.DTO.Constants.Application;

namespace Modules.TMS.Controllers.Map.Validators
{

    /// <summary>
    /// Author: TuanHA
    /// Created date: 15/01/2025
    /// </summary>
    public class MapValidator : AbstractValidator<TmsMap>
    {
        public MapValidator(IStringLocalizer localizer, bool isCreate = true)
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithName(x => localizer[BaseMsg.BaseEntity.Name])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .When(x => isCreate)
                .MaximumLength(EntityConst.Length64)
                .WithName(x => localizer[BaseMsg.BaseEntity.Name])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Type)
               .NotEmpty()
               .WithName(x => localizer[BaseMsg.TmsMap.Entity.Type])
               .WithMessage(x => localizer[BaseMsg.BaseValidation.GreaterThanOrEqual]);

            RuleFor(x => x.Status)
               .NotEmpty()
               .WithName(x => localizer[BaseMsg.BaseEntity.Status])
               .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
               .When(x => isCreate)
               .When(x => (x.Status != null && !isCreate) || isCreate)
               .IsInEnum()
               .WithMessage(localizer[BaseMsg.BaseValidation.MustBeValidValue]);

            RuleFor(x => x.Width)
               .GreaterThanOrEqualTo(0)
               .WithName(x => localizer[BaseMsg.TmsMap.Entity.Width])
               .WithMessage(x => localizer[BaseMsg.BaseValidation.GreaterThanOrEqual]);

            RuleFor(x => x.Height)
               .GreaterThanOrEqualTo(0)
               .WithName(x => localizer[BaseMsg.TmsMap.Entity.Height])
               .WithMessage(x => localizer[BaseMsg.BaseValidation.GreaterThanOrEqual]);

            RuleFor(x => x.Mode)
               .NotEmpty()
               .WithName(x => localizer[BaseMsg.TmsMap.Entity.Mode])
               .WithMessage(x => localizer[BaseMsg.BaseValidation.GreaterThanOrEqual]);

            RuleFor(x => x.OrderNo)
               .GreaterThanOrEqualTo(0)
               .WithName(x => localizer[BaseMsg.BaseEntity.OrderNo])
               .WithMessage(x => localizer[BaseMsg.BaseValidation.GreaterThanOrEqual]);

            RuleFor(x => x.IsDefault)
               .GreaterThanOrEqualTo(0)
               .WithName(x => localizer[BaseMsg.TmsMap.Entity.IsDefault])
               .WithMessage(x => localizer[BaseMsg.BaseValidation.GreaterThanOrEqual])
               .WithName(x => localizer[BaseMsg.TmsMap.Entity.IsDefault])
               .WithMessage(x => localizer[BaseMsg.BaseValidation.GreaterThanOrEqual]);
        }
    }

    //TODO check lai

    /// <summary>
    /// Author: TuanHA
    /// Created date: 15/01/2025
    /// </summary>
    public class AddMapValidator : AbstractValidator<AddMapInput>
    {
        public AddMapValidator(IStringLocalizer localizer)
        {
            Include(new MapValidator(localizer));
        }

    }

    /// <summary>
    /// Author: TuanHA
    /// Created date: 15/01/2025
    /// </summary>
    public class UpdateMapValidator : AbstractValidator<UpdateMapInput>
    {
        public UpdateMapValidator(IStringLocalizer localizer)
        {
            Include(new MapValidator(localizer, false));
        }
    }

    /// <summary>
    /// Author: TuanHA
    /// Created date: 15/01/2025
    /// </summary>
    public class DeleteMapValidator : AbstractValidator<DeleteMapInput>
    {
        public DeleteMapValidator(IStringLocalizer localizer)
        {
            RuleFor(data => data.ID)
               .NotEmpty()
               .WithName(localizer[BaseMsg.BaseEntity.ID])
               .WithMessage(localizer[BaseMsg.BaseValidation.Required]);
        }
    }
}
