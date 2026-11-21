using Modules.TMS.Core.Entities;

namespace Modules.Media.Infrastructure.Helpers;

/// <summary>
/// Description: Hàm tiện ích dùng chung của phân hệ Media.
/// Created date: 08/10/2026
/// </summary>
public static class MediaHelper
{
    /// <summary>ID hợp lệ: chữ, số, '_' và '-' (GUID, mã số).</summary>
    public const string IdPattern = "^[a-zA-Z0-9_-]+$";

    /// <summary>Phiên bản dữ liệu để làm khóa cache: UpdateTime, không có thì CreateTime.</summary>
    public static string Version(DateTime? updateTime, DateTime? createTime)
        => (updateTime ?? createTime)?.Ticks.ToString() ?? "0";

    /// <summary>
    /// Thời điểm chụp ảnh theo mốc trạng thái — giữ đúng quy tắc của
    /// IncidentSnapshotDispatcher.CaptureTime (module TMS, internal nên không gọi trực tiếp được).
    /// </summary>
    public static DateTime? IncidentCaptureTime(TmsIncident incident, int pictureType)
        => pictureType switch
        {
            1 => incident.CreatedDate ?? incident.StartDate,
            2 => incident.ValidatedDate,
            3 => incident.ApprovedDate,
            4 => incident.FinishedDate,
            _ => null
        };

    /// <summary>Đường dẫn là URL ngoài (http/https)?</summary>
    public static bool IsExternal(string? path)
        => !string.IsNullOrWhiteSpace(path)
           && (path.TrimStart().StartsWith("http://", StringComparison.OrdinalIgnoreCase)
               || path.TrimStart().StartsWith("https://", StringComparison.OrdinalIgnoreCase));

    /// <summary>Chuẩn hóa Field của EquipmentType: Default | Active | Inactive | Fault.</summary>
    public static string EquipmentTypeField(string? field)
        => field?.Trim().ToLowerInvariant() switch
        {
            "active" => "Active",
            "inactive" => "Inactive",
            "fault" => "Fault",
            _ => "Default"
        };

    /// <summary>Lấy đường dẫn icon theo Field.</summary>
    public static string? EquipmentTypePath(TmsEquipmentType entity, string field)
        => field switch
        {
            "Active" => entity.ActiveIcon,
            "Inactive" => entity.InactiveIcon,
            "Fault" => entity.FaultIcon,
            _ => entity.DefaultIcon
        };

    /// <summary>Chuẩn hóa Field của VMS: Url | DefaultUrl.</summary>
    public static string VmsField(string? field)
        => string.Equals(field?.Trim(), "DefaultUrl", StringComparison.OrdinalIgnoreCase) ? "DefaultUrl" : "Url";
}
