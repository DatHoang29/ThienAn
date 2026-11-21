using Modules.VDS.Core.Constants;
using Services.Vds.Configuration;
using Services.Vds.Features.Events;
using Services.Vds.Shared.Domain;

namespace Services.Vds.Features.Journey;

/// <summary>
/// Author: KienLT
/// Description: Luật "hành trình A→B vượt tốc độ cho phép ⇒ sinh sự kiện và nâng sự cố". Hàm THUẦN, không
/// I/O, không DI — kiểm được trực tiếp, cùng dạng với <see cref="JourneyMatcher"/> và
/// <see cref="VdsEventRules"/>.
///
/// <para><b>Vì sao tách khỏi <c>JourneyJob</c>.</b> Trước 25/09/2026 quyết định này là một biểu thức nằm
/// giữa vòng lặp ghi bảng của <c>JourneyJob</c>, còn loại sự cố và công thức khoá thì nằm rải trong
/// <c>IncidentPromoter.PromoteAverageSpeedAsync</c>. Muốn biết "vì sao chuyến này không sinh sự cố" phải đọc
/// cả hai chỗ, và muốn đổi sang loại sự cố riêng thì phải sửa bốn vị trí hằng chuỗi. Gom về đây để câu trả
/// lời nằm ở một nơi.</para>
///
/// <para><b>Trả <see cref="EventDecision"/> chứ không trả <c>bool</c>.</b> Dùng lại đúng kiểu mà
/// <see cref="VdsEventRules"/> đang trả, nên tầng trên xử lý hai luồng sự kiện theo cùng một cách, và mỗi
/// lần bỏ qua đều có <c>SkipReason</c> ghi được vào log thay vì im lặng.</para>
///
/// <para><b>Loại sự cố hiện dùng chung với quá tốc độ tức thời</b> (<c>Speeding</c>). Tên loại, slug điều
/// kiện và công thức <c>EventId</c> nằm ở <see cref="VdsJourneyEventConst"/> trong
/// <c>Modules.VDS.Core</c> — đổi sang loại riêng về sau chỉ sửa ở đó, và bên đọc của WebAPI tự khớp theo.</para>
///
/// <para><b>Fail-closed ba lớp</b>: tắt cấu hình sự kiện, chưa khai ngưỡng, hoặc chuyến không hợp lệ thì
/// KHÔNG sinh sự cố. Đặc biệt là lớp thứ ba: chuyến có <c>IsValid = false</c> nghĩa là thời gian di chuyển
/// ra ngoài biên đoạn, mà thời gian quá NGẮN thường là hai xe khác nhau bị đọc trùng biển chứ không phải một
/// xe chạy rất nhanh — nâng sự cố từ đó là vu oan.</para>
/// Created date: 25/09/2026
/// </summary>
public static class JourneyOverSpeedRules
{
    /// <summary>Lý do bỏ qua — chuỗi ổn định để ghi log và đếm, cùng lối đặt tên với <see cref="VdsEventRules"/>.</summary>
    public static class SkipReasons
    {
        /// <summary>Cấu hình sự kiện đang tắt toàn cục.</summary>
        public const string EventsDisabled = "events_disabled";

        /// <summary>Chưa khai tốc độ cho phép, hoặc khai giá trị không dương.</summary>
        public const string SpeedLimitNotConfigured = "speed_limit_not_configured";

        /// <summary>Chuyến không hợp lệ: thời gian di chuyển ngoài biên đoạn.</summary>
        public const string JourneyOutOfBounds = "journey_out_of_travel_bounds";

        /// <summary>Không tính được tốc độ (thiếu quãng đường hoặc thời gian).</summary>
        public const string SpeedNotAvailable = "journey_speed_not_available";

        /// <summary>Tốc độ trung bình còn trong mức cho phép.</summary>
        public const string WithinSpeedLimit = "within_speed_limit";
    }

    /// <summary>
    /// Quyết định cho MỘT chuyến A→B đã ghép.
    ///
    /// <para>Vượt ngưỡng dùng so sánh <b>lớn hơn hẳn</b> (<c>&gt;</c>), không phải <c>&gt;=</c>: chạy đúng
    /// bằng tốc độ cho phép không phải vi phạm. Giữ nguyên phép so sánh của bản cũ trong <c>JourneyJob</c>
    /// để tập sự cố sinh ra không đổi khi chuyển sang dùng hàm này.</para>
    /// </summary>
    /// <param name="candidate">Chuyến A→B do <see cref="JourneyMatcher"/> ghép.</param>
    /// <param name="allowedSpeedKmh">
    /// Tốc độ cho phép của đoạn (km/h). <c>null</c> hoặc <c>&lt;= 0</c> ⇒ coi như chưa khai và bỏ qua.
    /// </param>
    /// <param name="eventsEnabled">Cờ bật/tắt toàn cục của tầng sự kiện.</param>
    public static EventDecision Decide(JourneyCandidate candidate, double? allowedSpeedKmh, bool eventsEnabled)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        if (!eventsEnabled) return EventDecision.Skip(SkipReasons.EventsDisabled);
        if (allowedSpeedKmh is not double limit || limit <= 0)
            return EventDecision.Skip(SkipReasons.SpeedLimitNotConfigured);

        // Chuyến bất thường có thể cho tốc độ tính ra rất cao mà không phải vi phạm thật.
        if (!candidate.IsValid) return EventDecision.Skip(SkipReasons.JourneyOutOfBounds);

        if (candidate.TravelSeconds <= 0 || candidate.DistanceMeter <= 0 || double.IsNaN(candidate.AvgSpeedKmh)
            || double.IsInfinity(candidate.AvgSpeedKmh))
            return EventDecision.Skip(SkipReasons.SpeedNotAvailable);

        if (candidate.AvgSpeedKmh <= limit) return EventDecision.Skip(SkipReasons.WithinSpeedLimit);

        return EventDecision
            .Event(VdsJourneyEventConst.ConditionCode)
            .PromoteToIncident()
            with { SubKey = VdsJourneyEventConst.DedupSubKey(candidate.LicensePlate) };
    }

    /// <summary>
    /// Tốc độ cho phép của chuyến A→B = GIÁ TRỊ LỚN NHẤT trong ngưỡng max hiệu lực của camera A và camera B
    /// (bên gọi đã phân giải: vùng chính → mặc định hướng). Lấy lớn nhất để không vu oan xe chạy đúng luật ở
    /// đầu đoạn được phép nhanh hơn. Giá trị null/không dương/không hữu hạn bị bỏ; không còn giá trị nào ⇒
    /// null (⇒ <see cref="SkipReasons.SpeedLimitNotConfigured"/>). Hàm THUẦN.
    /// </summary>
    public static double? AllowedSpeedKmh(double? fromCameraMaxKmh, double? toCameraMaxKmh)
    {
        double? result = null;
        foreach (var value in new[] { fromCameraMaxKmh, toCameraMaxKmh })
        {
            if (value is not double v || !double.IsFinite(v) || v <= 0) continue;
            if (result is null || v > result) result = v;
        }

        // Missing camera limits must behave identically on Worker and WPF.
        return result;
    }

    /// <summary>
    /// <c>TmsIncident.EventId</c> tất định của chuyến — uỷ quyền cho
    /// <see cref="VdsJourneyEventConst.EventId"/> để worker và WebAPI dùng đúng một công thức.
    /// </summary>
    public static string EventId(JourneyCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        return VdsJourneyEventConst.EventId(candidate.SegmentId, candidate.LicensePlate, candidate.FromTime);
    }
}
