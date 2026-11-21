using Modules.VDS.Infrastructure.Services.TrafficData.Rules;
using FluentValidation;
using Microsoft.Extensions.Localization;
using Modules.VDS.Core.Exceptions;
using Modules.VDS.Controllers.TrafficData.Dto;

namespace Modules.VDS.Controllers.TrafficData.Validators
{
    /// <summary>
    /// Author: KienLT
    /// Description: Validator cho các truy vấn TrafficData trên QuestDB.
    ///
    /// <para>Luật thời gian và phân trang nằm ở <see cref="VdsTrafficTimeWindowRules"/> — bộ luật đó dùng chung
    /// với 5 feature khác, đừng sửa để chỉ vừa TrafficData.</para>
    /// Created date: 03/09/2026
    /// </summary>
    public class PageTrafficDataValidator : AbstractValidator<PageTrafficDataInput>
    {
        public PageTrafficDataValidator(IStringLocalizer localizer)
        {
            VdsTrafficTimeWindowRules.Apply(this, localizer, x => x.FromDate, x => x.ToDate,
                VdsTrafficTimeWindowRules.RawListMaxHours);
            VdsTrafficTimeWindowRules.ApplyPaging(this, localizer, x => x.Page, x => x.PageSize);
            RuleFor(x => x).Must(x => x.LengthFrom == null || x.LengthTo == null || x.LengthFrom <= x.LengthTo)
                .WithMessage(_ => localizer[BaseMsg.Vds.Exception.LengthFromGreaterThanTo]);
            RuleFor(x => x).Must(x => x.SpeedFrom == null || x.SpeedTo == null || x.SpeedFrom <= x.SpeedTo)
                .WithMessage(_ => localizer[BaseMsg.Vds.Exception.SpeedFromGreaterThanTo]);
        }
    }

    public class TrafficDataListValidator : AbstractValidator<TrafficDataListInput>
    {
        public TrafficDataListValidator(IStringLocalizer localizer)
        {
            VdsTrafficTimeWindowRules.Apply(this, localizer, x => x.FromDate, x => x.ToDate,
                VdsTrafficTimeWindowRules.RawListMaxHours);
            RuleFor(x => x).Must(x => x.LengthFrom == null || x.LengthTo == null || x.LengthFrom <= x.LengthTo)
                .WithMessage(_ => localizer[BaseMsg.Vds.Exception.LengthFromGreaterThanTo]);
            RuleFor(x => x).Must(x => x.SpeedFrom == null || x.SpeedTo == null || x.SpeedFrom <= x.SpeedTo)
                .WithMessage(_ => localizer[BaseMsg.Vds.Exception.SpeedFromGreaterThanTo]);
        }
    }

    public class TrafficDataHourVolumeValidator : AbstractValidator<TrafficDataHourVolumeInput>
    {
        public TrafficDataHourVolumeValidator(IStringLocalizer localizer)
            => VdsTrafficTimeWindowRules.Apply(this, localizer, x => x.FromDate, x => x.ToDate,
                VdsTrafficTimeWindowRules.AggregateMaxHours);
    }

    public class TrafficDataDayVolumeValidator : AbstractValidator<TrafficDataDayVolumeInput>
    {
        public TrafficDataDayVolumeValidator(IStringLocalizer localizer)
            => VdsTrafficTimeWindowRules.Apply(this, localizer, x => x.FromDate, x => x.ToDate,
                VdsTrafficTimeWindowRules.AggregateMaxHours);
    }

    public class TrafficDataAverageValidator : AbstractValidator<TrafficDataAverageInput>
    {
        public TrafficDataAverageValidator(IStringLocalizer localizer)
            => VdsTrafficTimeWindowRules.Apply(this, localizer, x => x.FromDate, x => x.ToDate,
                VdsTrafficTimeWindowRules.AggregateMaxHours);
    }

    /// <summary>
    /// Breakdown gộp ở DB (không kéo raw) nên cho cửa sổ tới 60 ngày (theo yêu cầu 03/09/2026).
    /// </summary>
    public class TrafficDataVolumeBreakdownValidator : AbstractValidator<TrafficDataVolumeBreakdownInput>
    {
        public const int MonthMaxHours = 60 * 24;

        public TrafficDataVolumeBreakdownValidator(IStringLocalizer localizer)
            => VdsTrafficTimeWindowRules.Apply(this, localizer, x => x.FromDate, x => x.ToDate, MonthMaxHours);
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Luật cửa sổ thời gian cho đường in-process báo cáo vượt tốc độ (TMS.Report). Cùng trần 60 ngày
    /// với danh sách thô; trần chặt hơn của báo cáo do bên TMS.Report tự kiểm.
    /// Created date: 16/10/2026
    /// </summary>
    public class TrafficDataOverSpeedValidator : AbstractValidator<Shared.DTO.Dtos.Vds.VdsOverSpeedRequest>
    {
        public TrafficDataOverSpeedValidator(IStringLocalizer localizer)
            => VdsTrafficTimeWindowRules.Apply(this, localizer, x => x.FromDate, x => x.ToDate,
                VdsTrafficTimeWindowRules.RawListMaxHours);
    }
}
