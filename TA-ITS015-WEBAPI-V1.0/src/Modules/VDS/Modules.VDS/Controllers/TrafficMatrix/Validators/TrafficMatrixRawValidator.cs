using Modules.VDS.Infrastructure.Services.TrafficData.Rules;
using FluentValidation;
using Microsoft.Extensions.Localization;
using Modules.VDS.Controllers.TrafficData.Validators;
using Modules.VDS.Controllers.TrafficMatrix.Dto;

namespace Modules.VDS.Controllers.TrafficMatrix.Validators
{
    /// <summary>
    /// Author: KienLT
    /// Description: Validator ma trận thô: bắt buộc From/To + trần 48h (aggregate). Ma trận một ngày nằm
    /// gọn trong trần này; TMS gọi thường truyền đúng một ngày.
    /// Created date: 03/09/2026
    /// </summary>
    public class TrafficMatrixRawValidator : AbstractValidator<TrafficMatrixRawInput>
    {
        public TrafficMatrixRawValidator(IStringLocalizer localizer)
            => VdsTrafficTimeWindowRules.Apply(this, localizer, x => x.FromDate, x => x.ToDate,
                VdsTrafficTimeWindowRules.AggregateMaxHours);
    }
}
