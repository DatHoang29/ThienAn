using FluentValidation;
using Microsoft.Extensions.Localization;
using Modules.VDS.Core.Exceptions;

namespace Modules.VDS.Infrastructure.Services.TrafficData.Rules
{
    /// <summary>
    /// Author: KienLT
    /// Description: Luật thời gian và phân trang DÙNG CHUNG cho các truy vấn VDS đọc QuestDB.
    ///
    /// <para><b>Dùng chéo feature.</b> Tuy file nằm trong feature TrafficData, bộ luật này được 5 feature
    /// gọi: TrafficData, TrafficStatistic, TrafficCount, TrafficMatrix và TrafficSummary. Đổi hằng
    /// số hay đổi thông báo ở đây là đổi hành vi 400 của cả 5 nhóm API — không sửa một mình.</para>
    ///
    /// <para><b>Vì sao bắt buộc thời gian.</b> QuestDB phân vùng theo ngày; truy vấn KHÔNG có điều kiện
    /// <c>detectTime</c> sẽ quét mọi partition và server tự huỷ (<c>cancelled by user</c>). Bản cũ để
    /// <c>FromDate</c>/<c>ToDate</c> optional (<c>WhereIF(... != null)</c>) nên gọi thiếu là quét toàn bảng.</para>
    ///
    /// <para><b>Trần cửa sổ.</b> Từ 03/09/2026 mọi loại truy vấn dùng CÙNG một trần 60 ngày
    /// (<see cref="RawListMaxHours"/> = <see cref="AggregateMaxHours"/>). Mô tả cũ ghi "raw ≤ 24h,
    /// gộp ≤ 48h" là SAI so với hằng số — sửa mô tả, không sửa hằng số.</para>
    ///
    /// <para>Tách ra file riêng ngày 23/09/2026: trước đó class này nằm lẫn trong
    /// <c>TrafficDataValidator.cs</c>, nên 5 feature kia phải đọc một file tên validator để tìm bộ luật
    /// chung. Mẫu của repo là rule có file riêng — xem <c>TMS/Infrastructure/Services/Incident/Rules/
    /// TmsIncidentPageRules.cs</c>.</para>
    /// Created date: 03/09/2026
    /// </summary>
    internal static class VdsTrafficTimeWindowRules
    {
        // Trần cửa sổ: 60 ngày cho mọi loại truy vấn (theo yêu cầu 03/09/2026). QuestDB phân vùng theo
        // ngày; vẫn giữ MỘT trần hữu hạn để chặn truy vấn quét vô hạn (thiếu filter thời gian là quét cả
        // bảng và server tự huỷ). LƯU Ý: list/raw lấy-hết trên 60 ngày có thể trả rất nhiều dòng —
        // cân nhắc dùng page cho tra cứu rộng.
        internal const int RawListMaxHours = 60 * 24;
        internal const int AggregateMaxHours = 60 * 24;

        /// <summary>Gắn luật thời gian cho một input có FromDate/ToDate.</summary>
        internal static void Apply<T>(
            AbstractValidator<T> validator,
            IStringLocalizer localizer,
            Func<T, DateTime?> from,
            Func<T, DateTime?> to,
            int maxHours)
        {
            validator.RuleFor(x => from(x))
                .NotNull()
                .WithName(_ => localizer[BaseMsg.Vds.Entity.FromDate])
                .WithMessage(_ => localizer[BaseMsg.BaseValidation.Required]);

            validator.RuleFor(x => to(x))
                .NotNull()
                .WithName(_ => localizer[BaseMsg.Vds.Entity.ToDate])
                .WithMessage(_ => localizer[BaseMsg.BaseValidation.Required]);

            // ToDate phải sau FromDate.
            validator.RuleFor(x => x)
                .Must(x => from(x)!.Value < to(x)!.Value)
                .When(x => from(x).HasValue && to(x).HasValue)
                .WithName(_ => localizer[BaseMsg.Vds.Entity.TimeWindow])
                .WithMessage(_ => localizer[BaseMsg.Vds.Validation.ToDateAfterFromDate]);

            // Trần cửa sổ.
            validator.RuleFor(x => x)
                .Must(x => (to(x)!.Value - from(x)!.Value).TotalHours <= maxHours)
                .When(x => from(x).HasValue && to(x).HasValue && from(x)!.Value < to(x)!.Value)
                .WithName(_ => localizer[BaseMsg.Vds.Entity.TimeWindow])
                .WithMessage(_ => localizer[BaseMsg.Vds.Validation.TimeWindowTooLarge]);
        }

        /// <summary>Số bản ghi tối đa một trang cho mọi truy vấn phân trang của VDS.</summary>
        internal const int MaxPageSize = 500;

        /// <summary>
        /// Gắn trần phân trang cho một input có Page/PageSize.
        ///
        /// <para>Thiếu trần này thì client đặt <c>PageSize</c> bao nhiêu cũng được và một request kéo trọn
        /// bảng QuestDB về. Tách ra hàm dùng chung để hai input phân trang không thể lệch nhau — trước
        /// 22/09/2026 chỉ <c>PageTrafficDataInput</c> có trần, <c>PageTrafficStatisticInput</c> không.</para>
        /// </summary>
        internal static void ApplyPaging<T>(
            AbstractValidator<T> validator,
            IStringLocalizer localizer,
            Func<T, int> page,
            Func<T, int> pageSize)
        {
            validator.RuleFor(x => page(x))
                .GreaterThan(0)
                .WithName(_ => localizer[BaseMsg.Vds.Entity.Page])
                .WithMessage(_ => localizer[BaseMsg.BaseValidation.GreaterThan]);

            validator.RuleFor(x => pageSize(x))
                .InclusiveBetween(1, MaxPageSize)
                .WithName(_ => localizer[BaseMsg.Vds.Entity.PageSize])
                .WithMessage(_ => localizer[BaseMsg.Vds.Validation.PageSizeOutOfRange]);
        }
    }
}
