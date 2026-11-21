using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using Modules.WP.Controllers.Category.Dto;
using Modules.WP.Core.Exceptions;

namespace Modules.WP.Controllers.Category.Validators
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Validate dữ liệu Add/Update/Delete cho danh mục hệ thống WP (Category)
    /// Created date: 03/07/2026
    /// </summary>
    public class AddCategoryValidator:AbstractValidator<WpAddCategoryInput>
    {
        public AddCategoryValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.Name).NotEmpty().WithName(x => localizer[BaseMsg.WP.Entity.Name]).WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .MaximumLength(256).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.Code).NotEmpty().WithName(x => localizer[BaseMsg.BaseEntity.Code]).WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .MaximumLength(64).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.Remark).MaximumLength(256).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.Author).MaximumLength(-1).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
        }
    }
    /// <summary>
    /// Author: HieuNV
    /// Description: Kiểm tra dữ liệu đầu vào của Update Category
    /// Created date: 14/08/2026
    /// </summary>

    public class UpdateCategoryValidator:AbstractValidator<WpUpdateCategoryInput>
    {
        public UpdateCategoryValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.Name).NotEmpty().WithName(x => localizer[BaseMsg.WP.Entity.Name]).WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .MaximumLength(512).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.Code).NotEmpty().WithName(x => localizer[BaseMsg.BaseEntity.Code]).WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .MaximumLength(64).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.Remark).MaximumLength(256).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.Author).MaximumLength(-1).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
        }
    }
    /// <summary>
    /// Author: HieuNV
    /// Description: Kiểm tra dữ liệu đầu vào của Delete Category
    /// Created date: 14/08/2026
    /// </summary>

    public class DeleteCategoryValidator:AbstractValidator<WpDeleteCategoryInput>
    {
          public DeleteCategoryValidator(IStringLocalizer localizer)
            {
                RuleFor(x => x.ID).NotEmpty()
                    .WithName(x => localizer[BaseMsg.BaseEntity.ID])
                    .WithMessage(x => localizer[BaseMsg.BaseValidation.Required]);
            }
    }
}
