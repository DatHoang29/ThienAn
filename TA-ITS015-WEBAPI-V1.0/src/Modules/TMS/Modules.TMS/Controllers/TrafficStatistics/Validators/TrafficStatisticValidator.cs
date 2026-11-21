using FluentValidation;
using Microsoft.Extensions.Localization;
using Modules.TMS.Controllers.TrafficStatistics.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Exceptions;
using Shared.DTO.Constants.Application;

namespace Modules.TMS.Controllers.TrafficStatistics.Validators
{
    /// <summary>
    /// Author: TuanHA
    /// Created date: 15/01/2025
    /// </summary>
    public class TmsTrafficStatisticValidator : AbstractValidator<TmsTrafficStatistic>
    {
        public TmsTrafficStatisticValidator(IStringLocalizer localizer, bool isCreate = true)
        {
            RuleFor(data => data.Direction)
                .MaximumLength(EntityConst.Length512)
                .WithName(x => localizer[BaseMsg.Tms.Entity.Direction])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(data => data.Location)
                .MaximumLength(EntityConst.Length16)
                .WithName(x => localizer[BaseMsg.TmsTrafficStatistic.Entity.Location])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(data => data.Remark)
                .MaximumLength(EntityConst.Length256)
                .WithName(x => localizer[BaseMsg.BaseEntity.Remark])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(data => data.IpAddress)
                .MaximumLength(EntityConst.Length256)
                .WithName(x => localizer[BaseMsg.TmsTrafficStatistic.Entity.IpAddress])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            // 27/07/2026 — 6 cột SpaceOccupy/TimeOccupy/SpaceHeadway/TimeHeadway/StopTime/QueueLength
            // đã đổi từ nvarchar(16) sang float (VDS V3) nên bỏ rule MaximumLength.
            RuleFor(data => data.SpaceOccupy)
                .GreaterThanOrEqualTo(0)
                .When(data => data.SpaceOccupy.HasValue)
                .WithName(x => localizer[BaseMsg.TmsTrafficStatistic.Entity.SpaceOccupy])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.GreaterThanOrEqual]);

            RuleFor(data => data.TimeOccupy)
                .GreaterThanOrEqualTo(0)
                .When(data => data.TimeOccupy.HasValue)
                .WithName(x => localizer[BaseMsg.TmsTrafficStatistic.Entity.TimeOccupy])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.GreaterThanOrEqual]);

            RuleFor(data => data.SpaceHeadway)
                .GreaterThanOrEqualTo(0)
                .When(data => data.SpaceHeadway.HasValue)
                .WithName(x => localizer[BaseMsg.TmsTrafficStatistic.Entity.SpaceHeadway])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.GreaterThanOrEqual]);

            RuleFor(data => data.TimeHeadway)
                .GreaterThanOrEqualTo(0)
                .When(data => data.TimeHeadway.HasValue)
                .WithName(x => localizer[BaseMsg.TmsTrafficStatistic.Entity.TimeHeadway])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.GreaterThanOrEqual]);

            RuleFor(data => data.StopTime)
                .GreaterThanOrEqualTo(0)
                .When(data => data.StopTime.HasValue)
                .WithName(x => localizer[BaseMsg.TmsTrafficStatistic.Entity.StopTime])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.GreaterThanOrEqual]);

            RuleFor(data => data.QueueLength)
                .GreaterThanOrEqualTo(0)
                .When(data => data.QueueLength.HasValue)
                .WithName(x => localizer[BaseMsg.TmsTrafficStatistic.Entity.QueueLength])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);


            // >=0
            RuleFor(x => x.KmNumber)
                .GreaterThanOrEqualTo(0)
                .WithName(x => localizer[BaseMsg.Tms.Entity.KmNumber])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.GreaterThanOrEqual]);

            // >=0
            RuleFor(x => x.MetNumber)
                .GreaterThanOrEqualTo(0)
                    .WithName(x => localizer[BaseMsg.Tms.Entity.MetNumber])
                    .WithMessage(x => localizer[BaseMsg.BaseValidation.GreaterThanOrEqual])
                .LessThan(1000)
                    .WithName(x => localizer[BaseMsg.TmsZone.Entity.ToMetNumber])
                    .WithMessage(x => localizer[BaseMsg.BaseValidation.LessThan]);

            // >=0
            RuleFor(x => x.AvgSpeed)
                .GreaterThanOrEqualTo(0)
                    .WithName(x => localizer[BaseMsg.TmsTrafficStatistic.Entity.AvgSpeed])
                    .WithMessage(x => localizer[BaseMsg.BaseValidation.GreaterThanOrEqual]);

        }
    }

    /// <summary>
    /// Author: TuanHA
    /// Created date: 15/01/2025
    /// </summary>
    public class AddTmsTrafficStatisticValidator : AbstractValidator<AddTmsTrafficStatisticInput>
    {
        public AddTmsTrafficStatisticValidator(IStringLocalizer localizer)
        {
            Include(new TmsTrafficStatisticValidator(localizer));
        }

    }

    /// <summary>
    /// Author: TuanHA
    /// Created date: 15/01/2025
    /// </summary>
    public class UpdateTmsTrafficStatisticValidator : AbstractValidator<UpdateTmsTrafficStatisticInput>
    {
        public UpdateTmsTrafficStatisticValidator(IStringLocalizer localizer)
        {
            Include(new TmsTrafficStatisticValidator(localizer, false));
        }
    }

    /// <summary>
    /// Author: TuanHA
    /// Created date: 15/01/2025
    /// </summary>
    public class DeleteTmsTrafficStatisticValidator : AbstractValidator<DeleteTmsTrafficStatisticInput>
    {
        public DeleteTmsTrafficStatisticValidator(IStringLocalizer localizer)
        {
            RuleFor(data => data.ID)
                .NotEmpty()
                .WithName(x => localizer[BaseMsg.BaseEntity.ID])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.Required]);
        }
    }
}
