using Modules.VDS.Infrastructure.Services.TrafficData.Rules;
using FluentValidation;
using Microsoft.Extensions.Localization;
using Modules.VDS.Core.Exceptions;
using Modules.VDS.Controllers.TrafficData.Validators;
using Modules.VDS.Controllers.TrafficCount.Dto;

namespace Modules.VDS.Controllers.TrafficCount.Validators
{
    /// <summary>
    /// Author: KienLT
    /// Description: Validator cho số đếm xe. slices/summary bắt buộc From/To + trần 48h (aggregate).
    /// hourly bắt buộc ValueDate (cửa sổ cố định 24h nên chỉ cần một mốc).
    /// Created date: 03/09/2026
    /// </summary>
    public class TrafficCountSliceValidator : AbstractValidator<TrafficCountSliceInput>
    {
        public TrafficCountSliceValidator(IStringLocalizer localizer)
            => VdsTrafficTimeWindowRules.Apply(this, localizer, x => x.FromDate, x => x.ToDate,
                VdsTrafficTimeWindowRules.AggregateMaxHours);
    }

    public class TrafficCountSummaryValidator : AbstractValidator<TrafficCountSummaryInput>
    {
        public TrafficCountSummaryValidator(IStringLocalizer localizer)
            => VdsTrafficTimeWindowRules.Apply(this, localizer, x => x.FromDate, x => x.ToDate,
                VdsTrafficTimeWindowRules.AggregateMaxHours);
    }

    public class TrafficCountHourlyValidator : AbstractValidator<TrafficCountHourlyInput>
    {
        public TrafficCountHourlyValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ValueDate)
                .NotNull()
                .WithName(_ => localizer[BaseMsg.Vds.Entity.ValueDate])
                .WithMessage(_ => localizer[BaseMsg.BaseValidation.Required]);
        }
    }
}
