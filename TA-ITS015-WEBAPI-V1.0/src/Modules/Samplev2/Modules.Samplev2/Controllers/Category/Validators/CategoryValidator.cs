using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using Modules.Samplev2.Controllers.Category.Dto;
using Modules.Samplev2.Core.Exceptions;

namespace Modules.Samplev2.Controllers.Category.Validators
{
    public class AddCategoryValidator:AbstractValidator<AddCategoryInput>
    {
        public AddCategoryValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.Name).NotEmpty().WithName(x => localizer[BaseMsg.SampCategory.Entity.Name]).WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .MaximumLength(50).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.Code).NotEmpty().WithName(x => localizer[BaseMsg.BaseEntity.Code]).WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .MaximumLength(50).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.Description).MaximumLength(256).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
        }
       
    }

    public class UpdateCategoryValidator:AbstractValidator<UpdateCategoryInput>
    {
        public UpdateCategoryValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.Name).NotEmpty().WithName(x => localizer[BaseMsg.SampCategory.Entity.Name]).WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .MaximumLength(50).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.Code).NotEmpty().WithName(x => localizer[BaseMsg.BaseEntity.Code]).WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .MaximumLength(50).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.Description).MaximumLength(256).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

        }
    }

    public class DeleteCategoryValidator:AbstractValidator<DeleteCategoryInput>
    {
          public DeleteCategoryValidator(IStringLocalizer localizer)
            {
                RuleFor(x => x.ID).NotEmpty()
                    .WithName(x => localizer[BaseMsg.BaseEntity.ID])
                    .WithMessage(x => localizer[BaseMsg.BaseValidation.Required]);
            }
    }
}
