namespace Modules.TMS.Infrastructure.Services.Incident.Snapshot;

/// <summary>
/// Author: KienLT
/// Description: Cấu hình thư mục gốc lưu ảnh sự cố (section <c>IncidentImages</c> trong appsettings).
/// Created date: 14/10/2026
/// </summary>
public sealed class IncidentImageOptions
{
    public const string Section = "IncidentImages";

    /// <summary>
    /// Thư mục gốc, ví dụ <c>\\10.10.8.30\ImageShared\ITS015\TmsImage\IncidentImage</c>. Bỏ trống thì dùng
    /// <c>wwwroot/tms/incident</c> như trước (môi trường dev không có share).
    /// </summary>
    public string? RootDirectory { get; set; }
}
