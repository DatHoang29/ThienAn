using FluentValidation;
using Microsoft.Extensions.Localization;
using Modules.WP.Controllers.PriceListDetail.Dto;
using Modules.WP.Core.Exceptions;

namespace Modules.WP.Controllers.PriceListDetail.Validators
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Validate dữ liệu Add/Update/Delete cho chi tiết bảng giá hệ thống WP (PriceListDetail)
    /// Created date: 03/07/2026
    /// Update: 07/10/2026 - Theo cột TollPriceListDetail: PriceListSourceId, VehicleTypeSourceId, RoadSourceId, Price.
    /// </summary>
    public class AddPriceListDetailValidator : AbstractValidator<WpAddPriceListDetailInput>
    {
        public AddPriceListDetailValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.PriceListSourceId).NotEmpty().WithName(x => localizer[BaseMsg.BaseEntity.Code]).WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .MaximumLength(64).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.VehicleTypeSourceId).NotEmpty().WithName(x => localizer[BaseMsg.WP.Entity.VehicleType]).WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .MaximumLength(64).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.RoadSourceId).NotEmpty().WithName(x => localizer[BaseMsg.WP.Entity.EntryStation]).WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .MaximumLength(64).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.Price).NotNull().WithName(x => localizer[BaseMsg.WP.Entity.Price]).WithMessage(x => localizer[BaseMsg.BaseValidation.Required]);
        }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Kiểm tra dữ liệu đầu vào của Update Price List Detail
    /// Created date: 14/08/2026
    /// </summary>
    public class UpdatePriceListDetailValidator : AbstractValidator<WpUpdatePriceListDetailInput>
    {
        public UpdatePriceListDetailValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID).NotEmpty().WithName(x => localizer[BaseMsg.BaseEntity.ID]).WithMessage(x => localizer[BaseMsg.BaseValidation.Required]);
            RuleFor(x => x.PriceListSourceId).NotEmpty().WithName(x => localizer[BaseMsg.BaseEntity.Code]).WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .MaximumLength(64).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.VehicleTypeSourceId).NotEmpty().WithName(x => localizer[BaseMsg.WP.Entity.VehicleType]).WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .MaximumLength(64).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.RoadSourceId).NotEmpty().WithName(x => localizer[BaseMsg.WP.Entity.EntryStation]).WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .MaximumLength(64).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.Price).NotNull().WithName(x => localizer[BaseMsg.WP.Entity.Price]).WithMessage(x => localizer[BaseMsg.BaseValidation.Required]);
        }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Kiểm tra dữ liệu đầu vào của Delete Price List Detail
    /// Created date: 14/08/2026
    /// </summary>
    public class DeletePriceListDetailValidator : AbstractValidator<WpDeletePriceListDetailInput>
    {
        public DeletePriceListDetailValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID).NotEmpty()
                .WithName(x => localizer[BaseMsg.BaseEntity.ID])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.Required]);
        }
    }
}
