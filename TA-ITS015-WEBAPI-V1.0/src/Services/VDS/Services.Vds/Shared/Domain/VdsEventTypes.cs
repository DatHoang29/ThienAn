namespace Services.Vds.Shared.Domain;

/// <summary>
/// Author: KienLT
/// Description: Bộ tên loại sự kiện chuẩn của VDS API v1.8 và phép quy đổi ba tên legacy v1.0.
/// Created date: 25/08/2026
/// </summary>
public static class VdsEventTypes
{
    public const string Normal = "Normal";
    public const string ReverseDriving = "Reverse Driving";
    public const string Statistic = "Statistic";
    public const string Congestion = "Congestion";
    public const string Speeding = "Speeding";
    public const string SlowMoving = "Slow Moving";
    public const string Pedestrian = "Pedestrian";
    public const string Motorcycle = "Motorcycle";
    public const string Conflagration = "Conflagration";
    public const string StoppedVehicle = "Stopped Vehicle";
    public const string TrafficAccident = "Traffic Accident";
    public const string WrongLane = "Wrong Lane";
    public const string OccupyEmergencyLane = "Occupy Emergency Lane";
    public const string IllegalParking = "Illegal Parking";
    public const string FallenObject = "Fallen Object";
    public const string RestrictedVisibility = "Restricted Visibility";
    public const string Animal = "Animal";

    public const string LegacyNormal = "Regular";
    public const string LegacyStoppedVehicle = "Stopped Suddenly";
    public const string LegacyFallenObject = "Abandoned Object";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Normal,
        ReverseDriving,
        Statistic,
        Congestion,
        Speeding,
        SlowMoving,
        Pedestrian,
        Motorcycle,
        Conflagration,
        StoppedVehicle,
        TrafficAccident,
        WrongLane,
        OccupyEmergencyLane,
        IllegalParking,
        FallenObject,
        RestrictedVisibility, Animal,
    };

    /// <summary>
    /// 15 loại là SỰ CỐ: toàn bộ <see cref="All"/> trừ <see cref="Normal"/> (lượt xe bình thường) và
    /// <see cref="Statistic"/> (bản tin thống kê) — hai loại mà đặc tả ghi rõ "không phải sự cố".
    /// Dùng làm mặc định cho danh sách loại được nâng sự cố.
    /// </summary>
    public static readonly IReadOnlyList<string> Incidents = new[]
    {
        ReverseDriving, Congestion, Speeding, SlowMoving, Pedestrian, Motorcycle, Conflagration,
        StoppedVehicle, TrafficAccident, WrongLane, OccupyEmergencyLane, IllegalParking, FallenObject,
        RestrictedVisibility, Animal,
    };

    private static readonly IReadOnlyDictionary<string, string> Aliases =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [LegacyNormal] = Normal,
            [LegacyStoppedVehicle] = StoppedVehicle,
            [LegacyFallenObject] = FallenObject,
        };

    /// <summary>
    /// Cắt khoảng trắng và đổi tên legacy về tên v1.3. Loại chưa biết được giữ nguyên để tầng
    /// ingestion có thể log rồi vẫn lưu raw thay vì làm mất dữ liệu của firmware mới.
    /// </summary>
    public static string Canonical(string? type)
    {
        if (string.IsNullOrWhiteSpace(type)) return string.Empty;

        var value = type.Trim();
        return Aliases.TryGetValue(value, out var canonical) ? canonical : value;
    }

    public static bool IsKnown(string? type) =>
        !string.IsNullOrWhiteSpace(type) && All.Contains(Canonical(type));
}
