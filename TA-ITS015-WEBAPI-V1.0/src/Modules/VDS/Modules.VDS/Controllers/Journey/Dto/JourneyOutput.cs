using Shared.Utility.Serialization;
using Modules.VDS.Core.Dto;

namespace Modules.VDS.Controllers.Journey.Dto;

/// <summary>
/// Author: KienLT
/// Description: Hành trình và sự cố liên quan; giữ mốc camera A/B để tra cứu.
///
/// <para><b>Không còn trả entity từ 22/09/2026.</b> <c>Journey</c> trước đây là entity
/// <c>TmsTrafficJourney</c> và <c>Incidents</c> là <c>List&lt;TmsIncident&gt;</c> — cả hai kế thừa
/// <c>EntityTenant</c> nên lộ <c>TenantId</c>, <c>IsDelete</c>, <c>RowStatus</c>, <c>CreateUId</c>,
/// <c>UpdateUId</c> ra ngoài API.</para>
/// Created date: 08/09/2026
/// </summary>
public class JourneyDetailOutput
{
    public JourneyOutput Journey { get; set; } = new();
    public List<VdsLinkedIncidentOutput> Incidents { get; set; } = new();
    public JourneyRecordOutput FromRecord { get; set; } = new();
    public JourneyRecordOutput ToRecord { get; set; } = new();
}

/// <summary>
/// Author: KienLT
/// Description: Một hành trình A→B.
///
/// <para>Khai tường minh từng trường thay vì trả entity <c>TmsTrafficJourney</c>: entity kế thừa
/// <c>EntityTenant</c> nên kéo theo <c>TenantId</c>, <c>IsDelete</c>, <c>RowStatus</c>, <c>CreateUId</c>,
/// <c>UpdateUId</c> — không có việc gì cho màn hình hành trình.</para>
/// <para>Hai trường <c>FromAnalysisId</c>/<c>ToAnalysisId</c> của entity KHÔNG lộ ra: đó là khóa nội bộ trỏ
/// vào bản ghi phân tích của worker, client không tra được gì bằng chúng.</para>
/// Created date: 22/09/2026
/// </summary>
public class JourneyOutput
{
    public string ID { get; set; } = "";
    public string? SegmentId { get; set; }
    public string? LicensePlate { get; set; }
    public string? FromEquipmentId { get; set; }
    public string? ToEquipmentId { get; set; }
    [System.Text.Json.Serialization.JsonConverter(typeof(UtcDateTimeConverter))]
    public DateTime? FromTime { get; set; }
    [System.Text.Json.Serialization.JsonConverter(typeof(UtcDateTimeConverter))]
    public DateTime? ToTime { get; set; }
    public int? TravelSeconds { get; set; }
    public int? DistanceMeter { get; set; }
    public double? AvgSpeedKmh { get; set; }
    public string? Type { get; set; }
    public string? Direction { get; set; }
    public bool? IsValid { get; set; }
    public string? InvalidReason { get; set; }
    public double? SpeedLimitKmh { get; set; }
    public bool? IsOverSpeed { get; set; }
}

/// <summary>
/// Author: KienLT
/// Description: Một CHUYẾN của xe = các hành trình liên tiếp cùng biển số, cùng hướng (đoạn rẽ nối đuôi vẫn cùng
/// chuyến), không nghỉ quá ngưỡng <c>vds_journey_trip_gap</c> và không quay lại điểm đã qua. Dùng cho lưới gom nhóm và
/// sơ đồ lộ trình. Luật gom: <c>VdsJourneyTripRules</c>.
/// <para>Tổng km / thời gian / tốc độ TB tính trên <see cref="ChainJourneyIds"/> (chuỗi đoạn kề nhau nối đuôi), không cộng
/// các đoạn "nhảy cóc" worker sinh song song (vẫn liệt kê trong <see cref="Journeys"/>).</para>
/// <para><c>Key</c> gồm biển số và ID hành trình đầu chuyến. <c>ChainJourneyIds</c> theo thứ tự đi;
/// <c>Journeys</c> chứa mọi hành trình của chuyến, sắp theo <c>FromTime</c>.</para>
/// Created date: 21/10/2026
/// </summary>
public class JourneyTripOutput
{
    public string Key { get; set; } = "";
    public string? LicensePlate { get; set; }
    public string? StartEquipmentId { get; set; }
    public string? EndEquipmentId { get; set; }
    public string? StartDirection { get; set; }
    public string? EndDirection { get; set; }
    [System.Text.Json.Serialization.JsonConverter(typeof(UtcDateTimeConverter))]
    public DateTime? FromTime { get; set; }
    [System.Text.Json.Serialization.JsonConverter(typeof(UtcDateTimeConverter))]
    public DateTime? ToTime { get; set; }
    public int JourneyCount { get; set; }
    public int? DistanceMeter { get; set; }
    public int? TravelSeconds { get; set; }
    public double? AvgSpeedKmh { get; set; }
    public double? MaxSpeedKmh { get; set; }
    public bool HasOverSpeed { get; set; }
    public bool HasInvalid { get; set; }
    public List<string> ChainJourneyIds { get; set; } = [];
    public List<PageJourneyOutput> Journeys { get; set; } = [];
}

/// <summary>
/// Author: KienLT
/// Description: Một dòng hành trình A→B trả cho danh sách phân trang.
///
/// <para>Kế thừa <see cref="JourneyOutput"/> vì tập trường hiện y hệt bản chi tiết; giữ kiểu riêng để hai
/// đường có thể tách nhau về sau mà không phải đổi hình dạng JSON hôm nay.</para>
/// Created date: 22/09/2026
/// </summary>
public class PageJourneyOutput : JourneyOutput { }

/// <summary>
/// Author: KienLT
/// Description: Camera và mốc UTC của một đầu hành trình; không suy đoán vendor event ID.
/// Created date: 08/09/2026
/// </summary>
public class JourneyRecordOutput
{
    public string? EquipmentId { get; set; }
    [System.Text.Json.Serialization.JsonConverter(typeof(UtcDateTimeConverter))]
    public DateTime? DetectTime { get; set; }
}
