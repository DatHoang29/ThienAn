using Modules.VDS.Infrastructure.Services.TrafficData.Rules;
using FluentValidation;
using Microsoft.Extensions.Localization;
using Modules.VDS.Core.Exceptions;
using Modules.VDS.Controllers.TrafficData.Validators;
using Modules.VDS.Controllers.TrafficStatistic.Dto;

namespace Modules.VDS.Controllers.TrafficStatistic.Validators
{
    /// <summary>
    /// Author: KienLT
    /// Description: Validator cho các truy vấn thống kê chu kỳ. Bắt buộc FromDate/ToDate + trần cửa sổ,
    /// dùng lại <see cref="VdsTrafficTimeWindowRules"/> của phần TrafficData. Thống kê là đọc bản ghi (không
    /// gộp nặng) nên dùng trần raw 24h.
    ///
    /// <c>latestbyequipment</c> thêm ràng buộc EquipmentId bắt buộc: bản ghi mới nhất phải theo đúng một
    /// thiết bị, không có thì câu truy vấn trở thành "mới nhất toàn tuyến" — sai ngữ nghĩa.
    /// Created date: 03/09/2026
    /// </summary>
    public class PageTrafficStatisticValidator : AbstractValidator<PageTrafficStatisticInput>
    {
        public PageTrafficStatisticValidator(IStringLocalizer localizer)
        {
            VdsTrafficTimeWindowRules.Apply(this, localizer, x => x.FromDate, x => x.ToDate,
                VdsTrafficTimeWindowRules.RawListMaxHours);

            // Trần phân trang: thiếu nó thì client đặt PageSize bao nhiêu cũng được và một request kéo trọn
            // bảng QuestDB. PageTrafficDataValidator đã có trần này từ đầu; thống nhất 22/09/2026.
            VdsTrafficTimeWindowRules.ApplyPaging(this, localizer, x => x.Page, x => x.PageSize);
        }
    }

    public class TrafficStatisticListValidator : AbstractValidator<TrafficStatisticListInput>
    {
        public TrafficStatisticListValidator(IStringLocalizer localizer)
            => VdsTrafficTimeWindowRules.Apply(this, localizer, x => x.FromDate, x => x.ToDate,
                VdsTrafficTimeWindowRules.RawListMaxHours);
    }

    public class TrafficStatisticByDateValidator : AbstractValidator<TrafficStatisticByDateInput>
    {
        public TrafficStatisticByDateValidator(IStringLocalizer localizer)
            => VdsTrafficTimeWindowRules.Apply(this, localizer, x => x.FromDate, x => x.ToDate,
                VdsTrafficTimeWindowRules.RawListMaxHours);
    }

    public class TrafficStatisticByEquipmentValidator : AbstractValidator<TrafficStatisticByEquipmentInput>
    {
        public TrafficStatisticByEquipmentValidator(IStringLocalizer localizer)
        {
            VdsTrafficTimeWindowRules.Apply(this, localizer, x => x.FromDate, x => x.ToDate,
                VdsTrafficTimeWindowRules.RawListMaxHours);

            RuleFor(x => x.EquipmentId)
                .NotEmpty()
                .WithName(_ => localizer[BaseMsg.BaseEntity.ID])
                .WithMessage(_ => localizer[BaseMsg.BaseValidation.Required]);
        }
    }
}
