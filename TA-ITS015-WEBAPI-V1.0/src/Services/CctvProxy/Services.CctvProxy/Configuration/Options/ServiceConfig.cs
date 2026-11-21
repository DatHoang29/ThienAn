namespace Services.CctvProxy.Configuration.Options;

/// <summary>
/// Author: kien.le
/// Description: Cấu hình runtime của Cctv Proxy service (section "Service" trong appsettings).
/// Section <see cref="Cctv"/> được nạp lại từ SysOpConfig (nhóm cctv_config) qua CctvOptionsLoader,
/// giá trị trong file chỉ là fallback khi DB lỗi.
/// Created date: 06/10/2026
/// </summary>
public sealed class ServiceConfig
{
    public string Name { get; set; } = "CctvProxy";

    /// <summary>ConfigId kết nối chứa bảng SysOpConfig (khớp một ConfigId trong Database.json).</summary>
    public string DataConnectionId { get; set; } = "Default";

    /// <summary>Chu kỳ nạp lại cấu hình (giây). 0 = không reload.</summary>
    public int ConfigReloadSeconds { get; set; } = 60;

    public ProxyOptions Proxy { get; set; } = new();

    public CctvOptions Cctv { get; set; } = new();

    public PublicSnapshotOptions Public { get; set; } = new();
}
