using FluentValidation;
using Microsoft.Extensions.Localization;
using Modules.WP.Controllers.Menu.Dto;
using Modules.WP.Core.Exceptions;

namespace Modules.WP.Controllers.Menu.Validators
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Validate dữ liệu Add/Update/Delete cho menu hệ thống WP (Menu)
    /// Created date: 03/07/2026
    /// </summary>
    public class AddMenuValidator : AbstractValidator<WpAddMenuInput>
    {

        public AddMenuValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.Name).NotEmpty()
                .WithName(x => localizer[BaseMsg.WP.Entity.Name])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.Required]);
            RuleFor(x => x.OrderNo)
                .GreaterThanOrEqualTo(0)
                    .WithName(x => localizer[BaseMsg.BaseEntity.OrderNo])
                    .WithMessage(x => localizer[BaseMsg.BaseValidation.GreaterThanOrEqual])
                .When(x => x.OrderNo.HasValue);
        }
    }
    /// <summary>
    /// Author: HieuNV
    /// Description: Kiểm tra dữ liệu đầu vào của Update Menu
    /// Created date: 14/08/2026
    /// </summary>

    public class UpdateMenuValidator : AbstractValidator<WpUpdateMenuInput>
    {
        public UpdateMenuValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID).NotEmpty()
                .WithName(x => localizer[BaseMsg.BaseEntity.ID])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.Required]);
            RuleFor(x => x.Name).NotEmpty()
                .WithName(x => localizer[BaseMsg.WP.Entity.Name])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.Required]);
            RuleFor(x => x.OrderNo)
                .GreaterThanOrEqualTo(0)
                    .WithName(x => localizer[BaseMsg.BaseEntity.OrderNo])
                    .WithMessage(x => localizer[BaseMsg.BaseValidation.GreaterThanOrEqual])
                .When(x => x.OrderNo.HasValue);
        }
    }
    /// <summary>
    /// Author: HieuNV
    /// Description: Kiểm tra dữ liệu đầu vào của Delete Menu
    /// Created date: 14/08/2026
    /// </summary>

    public class DeleteMenuValidator : AbstractValidator<WpDeleteMenuInput>
    {
        public DeleteMenuValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID).NotEmpty()
                .WithName(x => localizer[BaseMsg.BaseEntity.ID])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.Required]);
        }
    }
}
