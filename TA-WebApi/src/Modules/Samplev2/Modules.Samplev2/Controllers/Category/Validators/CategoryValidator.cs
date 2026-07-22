using FluentValidation;
using Microsoft.Extensions.Localization;
using Modules.Samplev2.Controllers.Category.Dto;
using Modules.Samplev2.Core.Exceptions;
using Shared.DTO.Constants.Application;

namespace Modules.Samplev2.Controllers.Category.Validators
{
    public class AddCategoryValidator : AbstractValidator<AddCategoryInput>
    {
        public AddCategoryValidator(IStringLocalizer<AddCategoryInput> localizer)
        {
            RuleFor(x => x.Name).NotEmpty().WithName(x => localizer[BaseMsg.SampCategory.Entity.Name]).WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .MaximumLength(EntityConst.Length64).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.Code).NotEmpty().WithName(x => localizer[BaseMsg.BaseEntity.Code]).WithMessage("Code is required")
                .MaximumLength(EntityConst.Length64).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.Description).MaximumLength(EntityConst.Length256).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
        }

    }

    public class UpdateCategoryValidator : AbstractValidator<UpdateCategoryInput>
    {
        public UpdateCategoryValidator(IStringLocalizer<UpdateCategoryInput> localizer)
        {
            RuleFor(x => x.Name).NotEmpty().WithName(x => localizer[BaseMsg.SampCategory.Entity.Name]).WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .MaximumLength(EntityConst.Length64).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.Code).NotEmpty().WithName(x => localizer[BaseMsg.BaseEntity.Code]).WithMessage("Code is required")
                .MaximumLength(EntityConst.Length64).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.Description).MaximumLength(EntityConst.Length256).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

        }
    }

    public class DeleteCategoryValidator : AbstractValidator<DeleteCategoryInput>
    {
        public DeleteCategoryValidator(IStringLocalizer<DeleteCategoryInput> localizer)
        {
            RuleFor(x => x.ID).NotEmpty()
                .WithName(x => localizer[BaseMsg.BaseEntity.ID])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.Required]);
        }
    }
}
