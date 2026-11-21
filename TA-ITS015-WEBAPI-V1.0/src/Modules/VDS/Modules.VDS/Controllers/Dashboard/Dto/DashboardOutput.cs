using Shared.Utility.Serialization;
using Shared.Utility.Helpers;

namespace Modules.VDS.Controllers.Dashboard.Dto;

/// <summary>
/// Author: KienLT
/// Description: Metadata chung của snapshot đếm VDS, không phải response wrapper.
/// Created date: 08/09/2026
/// </summary>
public class DashboardSnapshotOutput
{
    public string Schema { get; set; } = "";
    public string Mode { get; set; } = "snapshot";
    [System.Text.Json.Serialization.JsonConverter(typeof(UtcDateTimeConverter))]
    public DateTime ComputedAt { get; set; }
    public string Source { get; set; } = "VdsTrafficData";
    public List<string> CountedEventTypes { get; set; } = new();
    public bool CountsAllEventTypes { get; set; }
    public bool CountsUnknownPlate { get; set; }
    public int PeriodMinutes { get; set; }
    public DashboardWindowsOutput Window { get; set; } = new();
}

/// <summary>
/// Author: KienLT
/// Description: Snapshot trung tâm theo camera và nhóm hướng cấu hình.
/// Created date: 08/09/2026
/// </summary>
public class DashboardCenterOutput : DashboardSnapshotOutput
{
    public List<DashboardCameraOutput> Cameras { get; set; } = new();
    public List<DashboardDirectionOutput> Directions { get; set; } = new();
}

/// <summary>
/// Author: KienLT
/// Description: Snapshot theo vùng, giữ schema vds.traffic/v2.
/// Created date: 08/09/2026
/// </summary>
public class DashboardZoneOutput : DashboardSnapshotOutput
{
    public List<DashboardZoneRowOutput> Zones { get; set; } = new();
}

/// <summary>
/// Author: KienLT
/// Description: Cửa sổ 1 giờ, 24 giờ và ngày nghiệp vụ hiện tại.
/// Created date: 08/09/2026
/// </summary>
public class DashboardWindowsOutput
{
    public DashboardIntervalOutput Last1h { get; set; } = new();
    public DashboardIntervalOutput Last24h { get; set; } = new();
    public DashboardIntervalOutput Today { get; set; } = new();
}

/// <summary>
/// Author: KienLT
/// Description: Biên thời gian UTC của một cửa sổ dashboard.
/// Created date: 08/09/2026
/// </summary>
public class DashboardIntervalOutput
{
    [System.Text.Json.Serialization.JsonConverter(typeof(UtcDateTimeConverter))]
    public DateTime From { get; set; }
    [System.Text.Json.Serialization.JsonConverter(typeof(UtcDateTimeConverter))]
    public DateTime To { get; set; }
}

/// <summary>
/// Author: KienLT
/// Description: Khối số đếm và tốc độ, giữ nhóm other và giá trị chưa biết null.
/// Created date: 08/09/2026
/// </summary>
public class DashboardCountOutput
{
    public int Short { get; set; }
    public int Medium { get; set; }
    public int Long { get; set; }
    public int Other { get; set; }
    public int Total { get; set; }
    public double? AvgSpeed { get; set; }
    public double? MinSpeed { get; set; }
    public double? MaxSpeed { get; set; }
}

/// <summary>
/// Author: KienLT
/// Description: Một giờ trong chuỗi đếm; không bổ sung min/max ngoài JSON hiện hữu.
/// Created date: 08/09/2026
/// </summary>
public class DashboardHourlyOutput
{
    [System.Text.Json.Serialization.JsonConverter(typeof(UtcDateTimeConverter))]
    public DateTime Hour { get; set; }
    public int Short { get; set; }
    public int Medium { get; set; }
    public int Long { get; set; }
    public int Other { get; set; }
    public int Total { get; set; }
    public double? AvgSpeed { get; set; }
    public string Origin { get; set; } = "db";
}

/// <summary>
/// Author: KienLT
/// Description: Tốc độ gần nhất và thời điểm đo.
/// Created date: 08/09/2026
/// </summary>
public class DashboardSpeedOutput
{
    public double? Latest { get; set; }
    [System.Text.Json.Serialization.JsonConverter(typeof(UtcDateTimeConverter))]
    public DateTime? LatestAt { get; set; }
}

/// <summary>
/// Author: KienLT
/// Description: Tốc độ hành trình A→B của hướng, trung bình trong một giờ gần nhất.
/// Created date: 08/09/2026
/// </summary>
public class DashboardDirectionSpeedOutput : DashboardSpeedOutput
{
    public string? LatestEquipmentId { get; set; }
    public double? Last1hAvgSpeed { get; set; }
    public IReadOnlyList<string> EquipmentIds { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Author: KienLT
/// Description: Các khối số đếm và chart dùng chung giữa camera, nhóm hướng và vùng.
/// Created date: 08/09/2026
/// </summary>
public class DashboardMetricsOutput
{
    public DashboardCountOutput Last1h { get; set; } = new();
    public DashboardCountOutput Last24h { get; set; } = new();
    public DashboardCountOutput Today { get; set; } = new();
    public List<DashboardHourlyOutput> FlowByHour { get; set; } = new();
    public EChartHelper.EChartResponse Chart { get; set; } = new();
}

/// <summary>
/// Author: KienLT
/// Description: Số đếm và thông tin một camera VDS.
/// Created date: 08/09/2026
/// </summary>
public class DashboardCameraOutput : DashboardMetricsOutput
{
    public string EquipmentId { get; set; } = "";
    public string? Code { get; set; }
    public string? Name { get; set; }
    public int? Km { get; set; }
    public int? Met { get; set; }
    public string? IpAddress { get; set; }
    public string? ZoneId { get; set; }
    public string? DirectionId { get; set; }
    public string? DirectionKey { get; set; }
    public DashboardSpeedOutput Speed { get; set; } = new();
}

/// <summary>
/// Author: KienLT
/// Description: Số đếm theo tập camera của một nhóm hướng đã cấu hình.
/// Created date: 08/09/2026
/// </summary>
public class DashboardDirectionOutput : DashboardMetricsOutput
{
    public string DirectionKey { get; set; } = "";
    public string? Name { get; set; }
    public string[] EquipmentIds { get; set; } = [];
    public DashboardDirectionSpeedOutput Speed { get; set; } = new();
}

/// <summary>
/// Author: KienLT
/// Description: Số đếm của vùng và danh sách camera liên kết.
/// Created date: 08/09/2026
/// </summary>
public class DashboardZoneRowOutput : DashboardMetricsOutput
{
    public string ZoneId { get; set; } = "";
    public List<string?> EquipmentIds { get; set; } = new();
    public DashboardSpeedOutput Speed { get; set; } = new();
}
