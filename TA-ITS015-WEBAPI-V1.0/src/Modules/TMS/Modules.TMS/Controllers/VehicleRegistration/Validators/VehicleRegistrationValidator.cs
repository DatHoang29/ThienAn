using FluentValidation;
using Microsoft.Extensions.Localization;
using Modules.TMS.Controllers.VehicleRegistration.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Exceptions;
using Shared.DTO.Constants.Application;

namespace Modules.TMS.Controllers.VehicleRegistration.Validators
{

    //TmsVehicleRegistration {
    //	ID varchar(64) pk
    //    LicensePlate string (32) null
    //	VehicleType string (64) null
    //	Brand string (64) null
    //	YearManufacture string (64) null
    //	Owner string (64) null
    //	Address string (128) null
    //	EngineNumber string (64) null
    //	ChassisNumber string (64) null
    //	OverallDimensions string (64) null
    //	ContainerDimensions string (64) null
    //	Seat string (32) null
    //	InspectionStamp string (64) null
    //	DateRegistrations datetime null
    //	ExpirationDate datetime null
    //	DataType string (16) null
    //}

    /// <summary>
    /// Author: Ngọc Thắng
    /// Created date: 19/08/2024
    /// </summary>
    public class VehicleRegistrationValidator : AbstractValidator<TmsVehicleRegistration>
    {
        public VehicleRegistrationValidator(IStringLocalizer lz, bool isCreate = true)
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

            RuleFor(x => x.VehicleType)
                .MaximumLength(EntityConst.Length64)
                    .WithName(x => lz[BaseMsg.TmsVehicleRegistration.Entity.VehicleType])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Brand)
                .MaximumLength(EntityConst.Length64)
                    .WithName(x => lz[BaseMsg.TmsVehicleRegistration.Entity.Brand])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.YearManufacture)
                .MaximumLength(EntityConst.Length64)
                    .WithName(x => lz[BaseMsg.TmsVehicleRegistration.Entity.YearManufacture])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Owner)
                .MaximumLength(EntityConst.Length64)
                    .WithName(x => lz[BaseMsg.TmsVehicleRegistration.Entity.Owner])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Address)
                .MaximumLength(EntityConst.Length128)
                    .WithName(x => lz[BaseMsg.TmsVehicleRegistration.Entity.Address])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.EngineNumber)
                .MaximumLength(EntityConst.Length64)
                    .WithName(x => lz[BaseMsg.TmsVehicleRegistration.Entity.EngineNumber])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.ChassisNumber)
                .MaximumLength(EntityConst.Length64)
                    .WithName(x => lz[BaseMsg.TmsVehicleRegistration.Entity.ChassisNumber])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.OverallDimensions)
                .MaximumLength(EntityConst.Length64)
                    .WithName(x => lz[BaseMsg.TmsVehicleRegistration.Entity.OverallDimensions])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.ContainerDimensions)
                .MaximumLength(EntityConst.Length64)
                    .WithName(x => lz[BaseMsg.TmsVehicleRegistration.Entity.ContainerDimensions])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.DataType)
                .MaximumLength(EntityConst.ConfigFieldLength)
                    .WithName(x => lz[BaseMsg.TmsVehicleRegistration.Entity.DataType])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Seat)
                .MaximumLength(EntityConst.Length32)
                    .WithName(x => lz[BaseMsg.TmsVehicleRegistration.Entity.Seat])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.InspectionStamp)
                .MaximumLength(EntityConst.Length64)
                    .WithName(x => lz[BaseMsg.TmsVehicleRegistration.Entity.InspectionStamp])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.DateRegistrations)
                .LessThanOrEqualTo(x => x.ExpirationDate!.Value)
                .When(x => x.DateRegistrations.HasValue && x.ExpirationDate.HasValue)
                .WithName(x => lz[BaseMsg.TmsVehicleRegistration.Entity.DateRegistrations])
                .WithMessage(x => lz[BaseMsg.TmsVehicleRegistration.Validation.DateRegistrationsExceedsExpiration]);

            RuleFor(x => x.ExpirationDate)
                .GreaterThanOrEqualTo(x => x.DateRegistrations!.Value)
                .When(x => x.DateRegistrations.HasValue && x.ExpirationDate.HasValue)
                .WithName(x => lz[BaseMsg.TmsVehicleRegistration.Entity.ExpirationDate])
                .WithMessage(x => lz[BaseMsg.TmsVehicleRegistration.Validation.ExpirationDateBeforeDateRegistrations]);
        }
    }

    /// <summary>
    /// Author: Ngọc Thắng
    /// Created date: 19/08/2024
    /// </summary>
    public class AddVehicleRegistrationValidator : AbstractValidator<AddTmsVehicleRegistrationInput>
    {
        public AddVehicleRegistrationValidator(IStringLocalizer localizer)
        {
            Include(new VehicleRegistrationValidator(localizer));
        }
    }

    /// <summary>
    /// Author: Ngọc Thắng
    /// Created date: 19/08/2024
    /// </summary>
    public class UpdateVehicleRegistrationValidator : AbstractValidator<UpdateTmsVehicleRegistrationInput>
    {
        public UpdateVehicleRegistrationValidator(IStringLocalizer localizer)
        {
            Include(new VehicleRegistrationValidator(localizer, false));
        }
    }

    /// <summary>
    /// Author: Ngọc Thắng
    /// Created date: 19/08/2024
    /// </summary>
    public class DeleteVehicleRegistrationValidator : AbstractValidator<DeleteTmsVehicleRegistrationInput>
    {
        public DeleteVehicleRegistrationValidator(IStringLocalizer lz)
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
