namespace Services.Vds.Wpf.Simulation;

/// <summary>
/// Giá trị mặc định của tab mô phỏng Regular (bind section <c>Simulator</c> trong appsettings).
/// Người dùng vẫn sửa được trực tiếp trên UI; đây chỉ là giá trị nạp sẵn khi mở cửa sổ.
/// </summary>
public sealed class SimulatorOptions
{
    public const string SectionName = "Simulator";

    /// <summary>Gắn bộ ảnh full/vehicle/plate mẫu vào bản tin WPF chưa có media.</summary>
    public bool IncludeDefaultImages { get; set; } = true;

    /// <summary>Khoảng cách tối thiểu giữa hai lượt xe của cùng một camera (giây).</summary>
    public int MinIntervalSeconds { get; set; } = 5;

    /// <summary>Khoảng cách tối đa giữa hai lượt xe của cùng một camera (giây).</summary>
    public int MaxIntervalSeconds { get; set; } = 10;

    // Tốc độ KHÔNG khai ở đây (08/08/2026): mỗi lượt xe lấy ngưỡng theo HƯỚNG của camera vừa chọn từ
    // VdsConfig (vds_route_{dir}_min_speed / _max_speed) qua VdsConfigManager, nên xe sinh ra luôn nằm
    // trong ngưỡng. Khai thêm ở đây sẽ thành nguồn thứ hai và sớm muộn lệch nhau.

    /// <summary>Chiều dài xe tối thiểu (mét) — quyết định nhóm cỡ short/medium/long.</summary>
    public double MinLengthMeter { get; set; } = 3.5;

    /// <summary>Chiều dài xe tối đa (mét).</summary>
    public double MaxLengthMeter { get; set; } = 16.0;

    /// <summary>Chu kỳ gửi bản tin Statistic (phút).</summary>
    public int StatisticPeriodMinutes { get; set; } = 1;

    /// <summary>
    /// Tên hiển thị của hướng tuyến, khoá là <c>TmsEquipment.DirectionId</c>.
    /// Mặc định theo danh mục <c>SysConfigData</c> loại <c>direction_config_type</c>.
    /// Dùng để nhóm danh sách camera trên UI cho trực quan.
    /// </summary>
    public Dictionary<string, string> DirectionNames { get; set; } = new()
    {
        ["0"] = "Tất cả hướng",
        ["1"] = "Phải tuyến",
        ["2"] = "Trái tuyến"
    };

    /// <summary>Nhãn dùng khi thiết bị chưa khai hướng.</summary>
    public string UnknownDirectionName { get; set; } = "Chưa khai hướng";

    /// <summary>Tên hiển thị của một DirectionId; không có trong danh mục thì ghi kèm mã gốc.</summary>
    public string DirectionLabel(string? directionId)
    {
        if (string.IsNullOrWhiteSpace(directionId)) return UnknownDirectionName;
        var key = directionId.Trim();
        return DirectionNames.TryGetValue(key, out var name) ? name : $"Hướng {key}";
    }
}
