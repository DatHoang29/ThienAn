using FluentValidation;
using Microsoft.Extensions.Localization;
using Modules.WP.Controllers.Toll.Dto;
using Modules.WP.Core.Exceptions;

namespace Modules.WP.Controllers.Toll.Validators
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Validate dữ liệu Add/Update/Delete cho trạm thu phí hệ thống WP (Toll)
    /// Created date: 03/07/2026
    /// Update: 07/10/2026 - Theo cột TollStation: StationId (bắt buộc, ≤64), Name (bắt buộc, ≤64), RefId (≤64), Note/Remark.
    /// </summary>
    public class AddTollValidator : AbstractValidator<WpAddTollInput>
    {
        public AddTollValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.StationId).NotEmpty().WithName(x => localizer[BaseMsg.BaseEntity.Code]).WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .MaximumLength(64).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.Name).NotEmpty().WithName(x => localizer[BaseMsg.WP.Entity.Name]).WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .MaximumLength(64).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.RefId).MaximumLength(64).WithName(x => localizer[BaseMsg.WP.Entity.RefId]).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.Note).MaximumLength(256).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.Remark).MaximumLength(256).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
        }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Kiểm tra dữ liệu đầu vào của Update Toll
    /// Created date: 14/08/2026
    /// </summary>
    public class UpdateTollValidator : AbstractValidator<WpUpdateTollInput>
    {
        public UpdateTollValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID).NotEmpty().WithName(x => localizer[BaseMsg.BaseEntity.ID]).WithMessage(x => localizer[BaseMsg.BaseValidation.Required]);
            RuleFor(x => x.StationId).NotEmpty().WithName(x => localizer[BaseMsg.BaseEntity.Code]).WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .MaximumLength(64).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.Name).NotEmpty().WithName(x => localizer[BaseMsg.WP.Entity.Name]).WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .MaximumLength(64).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.RefId).MaximumLength(64).WithName(x => localizer[BaseMsg.WP.Entity.RefId]).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.Note).MaximumLength(256).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.Remark).MaximumLength(256).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
        }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Kiểm tra dữ liệu đầu vào của Delete Toll
    /// Created date: 14/08/2026
    /// </summary>
    public class DeleteTollValidator : AbstractValidator<WpDeleteTollInput>
    {
        public DeleteTollValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID).NotEmpty()
                .WithName(x => localizer[BaseMsg.BaseEntity.ID])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.Required]);
        }
    }
}
