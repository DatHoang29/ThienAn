using Shared.Utility.Serialization;
using Modules.TMS.Core.Entities;

namespace Modules.VDS.Controllers.TrafficSummary.Dto;

/// <summary>
/// Author: KienLT
/// Description: Tổng hợp SQL kèm cửa sổ yêu cầu và coverage; không suy ra đầy đủ từ số dòng.
/// Created date: 08/09/2026
/// </summary>
public class TrafficSummaryOutput
{
    public string Source { get; set; } = "TmsTrafficSummary";
    public TrafficSummaryWindowOutput RequestedWindow { get; set; } = new();
    [System.Text.Json.Serialization.JsonConverter(typeof(UtcDateTimeConverter))]
    public DateTime? ComputedAt { get; set; }
    public List<TrafficSummaryCoverageOutput> Coverage { get; set; } = new();
    public string Completeness { get; set; } = "unverified";
    public List<TrafficSummaryRowOutput> Rows { get; set; } = new();
}

/// <summary>
/// Author: KienLT
/// Description: Một dòng tổng hợp theo chu kỳ/phạm vi.
///
/// <para>Khai tường minh từng trường thay vì trả entity <c>TmsTrafficSummary</c>: entity kế thừa
/// <c>EntityTenant</c> nên kéo theo <c>TenantId</c>, <c>IsDelete</c>, <c>RowStatus</c>, <c>CreateUId</c>,
/// <c>UpdateUId</c> ra ngoài API. Đây là chỗ CUỐI của module còn phơi entity — bị bỏ sót ở lượt 22/09/2026
/// vì bộ đếm lúc đó không có <c>TmsTrafficSummary</c> trong danh sách tên entity cần soát.</para>
/// <para>Giữ nguyên nullable của entity, kể cả các <c>SampleCount*</c>: chúng thật sự null với dữ liệu ghi
/// trước khi worker có cột mẫu, và ép về 0 sẽ làm bên nhận tưởng "đo được 0 mẫu".</para>
/// Created date: 22/09/2026
/// </summary>
public class TrafficSummaryRowOutput
{
    public string ID { get; set; } = "";
    public string? PeriodType { get; set; }
    [System.Text.Json.Serialization.JsonConverter(typeof(UtcDateTimeConverter))]
    public DateTime? FromTime { get; set; }
    [System.Text.Json.Serialization.JsonConverter(typeof(UtcDateTimeConverter))]
    public DateTime? ToTime { get; set; }
    public string? ScopeType { get; set; }
    public string? ScopeId { get; set; }
    public string? Direction { get; set; }
    public int? SVehicleNumber { get; set; }
    public int? MVehicleNumber { get; set; }
    public int? LVehicleNumber { get; set; }
    public int? OtherVehicleNumber { get; set; }
    public double? AvgSpeed { get; set; }
    public double? MinSpeed { get; set; }
    public double? MaxSpeed { get; set; }
    public double? AvgSpeedS { get; set; }
    public double? AvgSpeedM { get; set; }
    public double? AvgSpeedL { get; set; }
    public double? AvgSpeedOther { get; set; }
    public int? SampleCount { get; set; }
    public int? SampleCountS { get; set; }
    public int? SampleCountM { get; set; }
    public int? SampleCountL { get; set; }
    public int? SampleCountOther { get; set; }
    [System.Text.Json.Serialization.JsonConverter(typeof(UtcDateTimeConverter))]
    public DateTime? ComputedAt { get; set; }
}

/// <summary>
/// Author: KienLT
/// Description: Cửa sổ UTC yêu cầu, từ inclusive đến exclusive.
/// Created date: 08/09/2026
/// </summary>
public class TrafficSummaryWindowOutput
{
    [System.Text.Json.Serialization.JsonConverter(typeof(UtcDateTimeConverter))]
    public DateTime From { get; set; }
    [System.Text.Json.Serialization.JsonConverter(typeof(UtcDateTimeConverter))]
    public DateTime To { get; set; }
}

/// <summary>
/// Author: KienLT
/// Description: Các khoảng có row summary của một scope.
/// Created date: 08/09/2026
/// </summary>
public class TrafficSummaryCoverageOutput
{
    public string? ScopeId { get; set; }
    public List<TrafficSummaryIntervalOutput> Intervals { get; set; } = new();
}

/// <summary>
/// Author: KienLT
/// Description: Biên bucket lưu trong SQL; giữ nullable theo entity lịch sử.
/// Created date: 08/09/2026
/// </summary>
public class TrafficSummaryIntervalOutput
{
    [System.Text.Json.Serialization.JsonConverter(typeof(UtcDateTimeConverter))]
    public DateTime? From { get; set; }
    [System.Text.Json.Serialization.JsonConverter(typeof(UtcDateTimeConverter))]
    public DateTime? To { get; set; }
}
