using FluentValidation;
using Microsoft.Extensions.Localization;
using Modules.TMS.Controllers.VehicleFilter.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Exceptions;
using Shared.DTO.Constants.Application;

namespace Modules.TMS.Controllers.VehicleFilter.Validators
{
    //TmsVehicleFilter {
    //	ID varchar(64) pk
    //    LicensePlate string (32) null
    //	StartDate datetime null
    //	EndDate datetime null
    //	Type string (16) null
    //	Alarm string (16) null
    //	Status integer null
    //	Remark string (256) null
    //}

    /// <summary>
    /// Author: ThangNN
    /// Description: Kiểm tra dữ liệu insert-update TmsVehicleFilter
    /// Created date: 13/11/2024
    /// </summary>
    public class VehicleFilterValidator : AbstractValidator<TmsVehicleFilter>
    {
        public VehicleFilterValidator(IStringLocalizer lz, bool isCreate = true)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(x => lz[BaseMsg.BaseEntity.ID])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.Required])
                    .When(x => !isCreate)
                .MaximumLength(EntityConst.KeyFieldLength)
                    .WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.LicensePlate)
                .NotEmpty()
                    .WithName(x => lz[BaseMsg.TmsVehicleFilter.Entity.LicensePlate])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.Required])
                    .When(x => isCreate || (!isCreate && x.LicensePlate != null))
                .MaximumLength(EntityConst.Length32)
                    .WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.StartDate)
                .LessThanOrEqualTo(x => x.EndDate)
                    .WithName(x => lz[BaseMsg.TmsVehicleFilter.Entity.StartDate])
                    // Toán tử là LessThanOrEqualTo nên thông báo phải là LessThanOrEqual; trước đây dùng
                    // LessThan nên câu báo lỗi nói "nhỏ hơn" trong khi luật cho phép bằng.
                    .WithMessage(x => lz[BaseMsg.BaseValidation.LessThanOrEqual])
                    .When(x => x.StartDate != null && x.EndDate != null);

            RuleFor(x => x.Remark)
                .MaximumLength(EntityConst.Length256)
                    .WithName(x => lz[BaseMsg.BaseEntity.Remark])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);

            // Điều kiện When phải xét CHÍNH Status: trước đây xét Code nên update chỉ gửi Status
            // (Code = null) sẽ bỏ qua cả NotEmpty lẫn IsInEnum.
            RuleFor(x => x.Status)
                .NotEmpty()
                    .WithName(x => lz[BaseMsg.BaseEntity.Status])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.Required])
                .IsInEnum()
                    .WithMessage(lz[BaseMsg.BaseValidation.MustBeValidValue])
                .When(x => isCreate || (!isCreate && x.Status != null));
        }
    }

    /// <summary>
    /// Author: ThangNN
    /// Created date: 13/11/2024
    /// Description: Kiểm tra dữ liệu insert
    /// </summary>
    public class AddVehicleFilterValidator : AbstractValidator<AddTmsVehicleFilterInput>
    {
        public AddVehicleFilterValidator(IStringLocalizer localizer)
        {
            Include(new VehicleFilterValidator(localizer));
        }
    }

    /// <summary>
    /// Author: ThangNN
    /// Created date: 13/11/2024
    /// Description: Kiểm tra dữ liệu update
    /// </summary>
    public class UpdateVehicleFilterValidator : AbstractValidator<UpdateTmsVehicleFilterInput>
    {
        public UpdateVehicleFilterValidator(IStringLocalizer localizer)
        {
            Include(new VehicleFilterValidator(localizer, false));
        }
    }

    /// <summary>
    /// Author: ThangNN
    /// Created date: 13/11/2024
    /// Description: Kiểm tra dữ liệu delete
    /// </summary>
    public class DeleteVehicleFilterValidator : AbstractValidator<DeleteTmsVehicleFilterInput>
    {
        public DeleteVehicleFilterValidator(IStringLocalizer lz)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(x => lz[BaseMsg.BaseEntity.ID])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.Required])
                .MaximumLength(EntityConst.KeyFieldLength)
                    .WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);
        }
    }
}
