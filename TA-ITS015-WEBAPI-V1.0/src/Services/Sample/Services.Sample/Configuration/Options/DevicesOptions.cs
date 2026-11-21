namespace Services.Sample.Configuration.Options;

/// <summary>
/// Author: TAC Team
/// Description: Cấu hình thu thập thiết bị (section "Service:Devices").
/// Created date: 16/07/2026
/// </summary>
public sealed class DevicesOptions
{
    /// <summary>Thư mục quét plugin codec khi khởi động (startup-only).</summary>
    public string PluginDirectory { get; set; } = "plugins";

    /// <summary>Worker tự bật luồng thu thập thiết bị khi true. WPF điều khiển bằng nút Start/Stop.</summary>
    public bool Enabled { get; set; }

    /// <summary>Chu kỳ acquire (giây) cho mỗi vòng thu thập Device A + Device B.</summary>
    public int IntervalSeconds { get; set; } = 3;

    /// <summary>Mã định danh Device A dùng cho demo.</summary>
    public string DeviceAId { get; set; } = "DEVICE-A-01";

    /// <summary>Mã định danh Device B dùng cho demo.</summary>
    public string DeviceBId { get; set; } = "DEVICE-B-01";
}
