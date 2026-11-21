using FluentValidation;
using Microsoft.Extensions.Localization;
using Modules.Samplev2.Controllers.Category.Dto;
using Modules.Samplev2.Core.Exceptions;
using Shared.DTO.Constants.Application;

namespace Modules.Samplev2.Core.Validators
{
    public class CategoryValidator : AbstractValidator<AddCategoryInput>
    {
        public CategoryValidator(IStringLocalizer localizer, bool isCreate = true)
        {
            RuleFor(x => x.Description)
                .MaximumLength(EntityConst.Length256).WithName(x => localizer[BaseMsg.SampCategory.Entity.Description]).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            //When
            RuleFor(x => x.Name)
                .NotEmpty().WithName(x => localizer[BaseMsg.BaseEntity.Name]).WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .When(x => (x.Name != null && !isCreate) || isCreate)
                .MaximumLength(EntityConst.Length64).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Code)
                .NotEmpty().WithName(x => localizer[BaseMsg.BaseEntity.Code]).WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .When(x => (x.Code != null && !isCreate) || isCreate)
                .MaximumLength(EntityConst.Length256).WithName(x => localizer[BaseMsg.BaseEntity.Code]).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Status)
                .NotEmpty().WithName(x => localizer[BaseMsg.BaseEntity.Status]).WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .When(x => (x.Status != null && !isCreate) || isCreate)
                .IsInEnum().WithMessage(localizer[BaseMsg.BaseValidation.MustBeValidValue]);
        }

    }
    public class AddCategoryValidator : AbstractValidator<AddCategoryInput>
    {
        public AddCategoryValidator(IStringLocalizer localizer)
        {
            Include(new CategoryValidator(localizer));
        }

    }

    public class UpdateCategoryValidator : AbstractValidator<UpdateCategoryInput>
    {
        public UpdateCategoryValidator(IStringLocalizer localizer)
        {
            Include(new CategoryValidator(localizer));
        }
    }

    public class DeleteCategoryValidator : AbstractValidator<DeleteCategoryInput>
    {
        public DeleteCategoryValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID).NotEmpty()
                .WithName(x => localizer[BaseMsg.BaseEntity.ID]).WithMessage(x => localizer[BaseMsg.BaseValidation.Required]);

        }
    }
}
