using Modules.VDS.Infrastructure.Services.TrafficData.Rules;
using FluentValidation;
using Microsoft.Extensions.Localization;
using Modules.VDS.Controllers.TrafficData.Validators;
using Modules.VDS.Controllers.TrafficSummary.Dto;
using Modules.VDS.Core.Constants;
using Shared.Utility.Counting;
using Modules.VDS.Core.Exceptions;

namespace Modules.VDS.Controllers.TrafficSummary.Validators
{
    /// <summary>
    /// Author: KienLT
    /// Description: Luật cửa sổ thời gian và bộ mã chu kỳ / phạm vi cho truy vấn tổng hợp.
    ///
    /// <para><b>Lỗ đã bịt:</b> trước 22/09/2026 <c>PeriodType</c> KHÔNG được kiểm ở bất cứ đâu — hằng
    /// <see cref="VdsSummaryPeriodConst.IsValid"/> đã có nhưng không ai gọi, nên một giá trị lạ đi thẳng xuống
    /// tầng đọc và trả về rỗng thay vì báo lỗi. <c>ScopeType</c> cũng vậy: handler chỉ so nó với
    /// <c>Equipment</c>/<c>Zone</c> để kiểm bộ lọc chéo, mã ngoài dải thì không nhánh nào bắt.</para>
    ///
    /// <para>Cửa sổ dùng trần <c>AggregateMaxHours</c> vì đây là truy vấn gộp. Việc chặn bộ lọc chéo
    /// (scope là camera nhưng lại truyền <c>zoneId</c>) vẫn ở handler vì phải bảo vệ cả đường message bus.</para>
    /// Created date: 22/09/2026
    /// </summary>
    public class TrafficSummaryValidator : AbstractValidator<TrafficSummaryInput>
    {
        public TrafficSummaryValidator(IStringLocalizer localizer)
        {
            VdsTrafficTimeWindowRules.Apply(this, localizer, x => x.FromDate, x => x.ToDate,
                VdsTrafficTimeWindowRules.AggregateMaxHours);

            RuleFor(x => x.PeriodType)
                .Must(VdsSummaryPeriodConst.IsValid)
                    .WithName(_ => localizer[BaseMsg.Vds.Entity.PeriodType])
                    .WithMessage(_ => localizer[BaseMsg.BaseValidation.MustBeValidValue]);

            RuleFor(x => x.ScopeType)
                .Must(IsValidScope)
                    .WithName(_ => localizer[BaseMsg.Vds.Entity.ScopeType])
                    .WithMessage(_ => localizer[BaseMsg.BaseValidation.MustBeValidValue]);
        }

        /// <summary>
        /// Hai phạm vi mà <c>VdsTrafficSummaryService.ReadAsync</c> THỰC SỰ đọc được.
        ///
        /// <para><b>Cố ý KHÔNG nhận <c>TrafficCountScopes.Direction</c></b> dù hằng đó tồn tại: bảng
        /// <c>TmsTrafficSummary</c> không có dòng nào với <c>ScopeType = direction</c>, và service từ chối
        /// thẳng bằng <c>SummaryPeriodScopeInvalid</c>. Bản đầu của validator này (22/09/2026) nhận cả ba nên
        /// request <c>scopeType=direction</c> qua được validation rồi mới bị business layer chặn — hai tầng
        /// báo hai chuyện khác nhau cho cùng một request sai. Nếu sau này cần <c>direction</c> thì phải có
        /// nguồn tổng hợp theo hướng trước, KHÔNG cộng dồn dòng của scope khác vì sẽ ra số sai.</para>
        /// </summary>
        private static bool IsValidScope(string? scope)
            => scope is TrafficCountScopes.Equipment or TrafficCountScopes.Zone;
    }
}
