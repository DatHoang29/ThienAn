using FluentValidation;
using Microsoft.Extensions.Localization;
using Modules.TMS.Controllers.TrafficMatrix.Dto;
using Modules.TMS.Core.Exceptions;
using Shared.DTO.Constants.Application;

namespace Modules.TMS.Controllers.TrafficMatrix.Validators
{
    /// <summary>
    /// Author: KienLT
    /// Description: Validate tham số bảng ma trận theo giờ.
    /// Created date: 11/08/2026
    /// </summary>
    public class TrafficMatrixValidator : AbstractValidator<TrafficMatrixInput>
    {
        public TrafficMatrixValidator(IStringLocalizer localizer)
        {
            RuleFor(data => data.ZoneId)
                .MaximumLength(EntityConst.KeyFieldLength)
                .WithName(x => localizer[BaseMsg.Tms.Entity.ZoneId])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(data => data.ZoneType)
                .MaximumLength(EntityConst.ConfigFieldLength)
                .WithName(x => localizer[BaseMsg.TmsZone.Entity.Type])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(data => data.Direction)
                .MaximumLength(EntityConst.ConfigFieldLength)
                .WithName(x => localizer[BaseMsg.Tms.Entity.Direction])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(data => data.VehicleType)
                .MaximumLength(EntityConst.ConfigFieldLength)
                .WithName(x => localizer[BaseMsg.TmsTrafficMatrix.Entity.VehicleType])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
        }
    }
}
