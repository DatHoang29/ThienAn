using Shared.Utility.Serialization;
using Modules.VDS.Controllers.TrafficData.Dto;
using Modules.VDS.Core.Dto;

namespace Modules.VDS.Controllers.Event.Dto;

/// <summary>
/// Author: KienLT
/// Description: Sự kiện VDS, sự cố liên quan và khóa tra media.
///
/// <para><b>Không còn trả entity từ 22/09/2026.</b> <c>Data</c> trước đây là entity <c>VdsTrafficData</c> và
/// <c>Incidents</c> là <c>List&lt;TmsIncident&gt;</c> — cả hai kế thừa lớp entity nên lộ <c>TenantId</c>,
/// <c>IsDelete</c>, <c>RowStatus</c>, <c>CreateUId</c>, <c>UpdateUId</c> ra ngoài API. Nay dùng
/// <see cref="TrafficDataOutput"/> và <see cref="VdsLinkedIncidentOutput"/>: cùng tập trường nghiệp vụ,
/// bỏ phần hạ tầng.</para>
/// <para><c>Data</c> dùng lại <see cref="TrafficDataOutput"/> của feature TrafficData thay vì khai một lớp
/// 16 trường y hệt: hai feature đã cố ý chung đường đọc (phân trang sự kiện dispatch thẳng sang handler của
/// TrafficData, chỉ khác cờ <c>EventsOnly</c>), nên chung DTO là đúng bản chất chứ không phải kết dính thừa.
/// Kèm theo đó <c>DetectTime</c> nay đi qua <c>UtcDateTimeConverter</c> như mọi mốc thời gian khác của
/// module — trước đây entity serialize thô nên lệch định dạng với chính API phân trang.</para>
/// Created date: 08/09/2026
/// </summary>
public class EventDetailOutput
{
    public TrafficDataOutput Data { get; set; } = new();
    public List<VdsLinkedIncidentOutput> Incidents { get; set; } = new();
    public EventMediaOutput Media { get; set; } = new();
}

/// <summary>
/// Author: KienLT
/// Description: Khóa kép tra media; danh sách kind không cam kết file đã tồn tại.
/// Created date: 08/09/2026
/// </summary>
public class EventMediaOutput
{
    public long EventId { get; set; }
    [System.Text.Json.Serialization.JsonConverter(typeof(UtcDateTimeConverter))]
    public DateTime DetectTime { get; set; }
    public string[] Kinds { get; set; } = [];
}

/// <summary>
/// Author: KienLT
/// Description: Các sự kiện (TmsEventType nhóm event) còn trong thời gian hiển thị.
///
/// <para>Sự kiện KHÔNG có hồ sơ <c>TmsIncident</c> (từ 19/10/2026): nguồn là bản ghi thô QuestDB
/// (<c>source = aid</c>) và hành trình vượt tốc độ trung bình (<c>source = journey</c>). Hết thời gian hiển thị thì
/// tự rơi khỏi snapshot — không có trạng thái nào phải đóng.</para>
/// Created date: 19/10/2026
/// </summary>
public class EventSnapshotOutput
{
    [System.Text.Json.Serialization.JsonConverter(typeof(UtcDateTimeConverter))]
    public DateTime ComputedAt { get; set; }

    public List<EventSnapshotItemOutput> Items { get; set; } = [];
}

/// <summary>
/// Author: KienLT
/// Description: Một nhóm sự kiện đang hiển thị. Bản tin AID gom theo (camera, loại, biển số); hành trình mỗi chuyến
/// một item. <c>Key</c> ổn định suốt thời gian hiển thị để FE không báo lại khi nhóm có bản tin mới.
/// <para><c>Source</c> là aid hoặc journey. <c>VendorEventId</c> và <c>DetectTime</c> của bản tin mới nhất
/// (hành trình: bản tin camera cuối nếu tìm thấy) là khóa tra Detail/Media để lấy ảnh.
/// <c>Speed</c> là tốc độ tức thời AID, null với hành trình; hành trình dùng <c>AvgSpeedKmh</c>.</para>
/// Created date: 19/10/2026
/// </summary>
public class EventSnapshotItemOutput
{
    public string Key { get; set; } = "";

    public string Source { get; set; } = "";

    public string? VendorEventId { get; set; }

    [System.Text.Json.Serialization.JsonConverter(typeof(UtcDateTimeConverter))]
    public DateTime DetectTime { get; set; }

    public string? JourneyId { get; set; }
    public double? AvgSpeedKmh { get; set; }
    public string? FromEquipmentId { get; set; }
    public string? FromEquipmentName { get; set; }
    public int? DistanceMeter { get; set; }
    public int? TravelSeconds { get; set; }

    public string? EventTypeCode { get; set; }
    public string? EventTypeName { get; set; }
    public string? Icon { get; set; }
    public string? WarningType { get; set; }
    public int? Priority { get; set; }
    public string? AidEventType { get; set; }

    public string? EquipmentId { get; set; }
    public string? EquipmentName { get; set; }
    public string? Location { get; set; }
    public string? LocationName { get; set; }
    public string? InfluenceScope { get; set; }
    public string? InfluenceScopeName { get; set; }
    public int? KmNumber { get; set; }
    public int? MetNumber { get; set; }
    public string? Lane { get; set; }

    public string? LicensePlate { get; set; }
    public string? PlateColor { get; set; }

    public double? Speed { get; set; }
    public double? SpeedMax { get; set; }
    public double? SpeedMin { get; set; }

    public int Count { get; set; }

    [System.Text.Json.Serialization.JsonConverter(typeof(UtcDateTimeConverter))]
    public DateTime FirstDetectTime { get; set; }

    [System.Text.Json.Serialization.JsonConverter(typeof(UtcDateTimeConverter))]
    public DateTime LastDetectTime { get; set; }

    [System.Text.Json.Serialization.JsonConverter(typeof(UtcDateTimeConverter))]
    public DateTime DisplayExpiresAt { get; set; }
}
