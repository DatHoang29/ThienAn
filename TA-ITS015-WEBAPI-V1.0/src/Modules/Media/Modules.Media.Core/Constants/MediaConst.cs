namespace Modules.Media.Core.Constants;

/// <summary>
/// Description: Hằng số dùng chung của phân hệ Media (ITS-Media và WP-MediaPublic).
/// Created date: 08/10/2026
/// </summary>
public static class MediaConst
{
    /// <summary>GroupCode trong bảng SysOpConfig chứa toàn bộ cấu hình Media.</summary>
    public const string ConfigGroup = "media_config";

    /// <summary>Header mang khóa bí mật khi WP gọi sang ITS.</summary>
    public const string ApiKeyHeader = "X-Media-Key";

    /// <summary>Khóa cấu hình vai trò trong appsettings (Configuration/Media.json).</summary>
    public const string RoleConfigKey = "Media:Role";

    /// <summary>Tên phân hệ ảnh — dùng làm tiền tố khóa cấu hình (media_{subsystem}_xxx) và thư mục cache.</summary>
    public static class Subsystem
    {
        public const string Incident = "incident";
        public const string EventType = "eventtype";
        public const string EquipmentType = "equipmenttype";
        public const string Vms = "vms";
    }

    /// <summary>Vai trò của bản deploy.</summary>
    public static class Role
    {
        /// <summary>Site ITS: bật ItsMedia (nguồn ảnh).</summary>
        public const string Provider = "Provider";
        /// <summary>Site WP: bật MediaPublic (cổng cho FE Web Portal).</summary>
        public const string Public = "Public";
        /// <summary>Bật cả hai (môi trường dev/test chạy chung một BE).</summary>
        public const string Both = "Both";
    }

    /// <summary>Trạng thái của một ảnh trả về.</summary>
    public static class Status
    {
        /// <summary>Có ảnh.</summary>
        public const string Ready = "Ready";
        /// <summary>Camera chưa chụp xong (chỉ Incident).</summary>
        public const string PendingCapture = "PendingCapture";
        /// <summary>Nguồn không có file.</summary>
        public const string MissingSource = "MissingSource";
        /// <summary>Không tìm thấy bản ghi / không đủ điều kiện công khai.</summary>
        public const string NotFound = "NotFound";
        /// <summary>Đường dẫn là URL ngoài (http/https) — FE dùng thẳng Url.</summary>
        public const string External = "External";
        /// <summary>Phân hệ đang tắt trong cấu hình.</summary>
        public const string Disabled = "Disabled";
        /// <summary>Không gọi được ITS và chưa có cache.</summary>
        public const string Unavailable = "Unavailable";
    }
}
