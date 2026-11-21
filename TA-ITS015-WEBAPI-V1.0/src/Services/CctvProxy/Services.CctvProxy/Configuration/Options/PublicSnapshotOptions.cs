namespace Services.CctvProxy.Configuration.Options;

/// <summary>
/// Author: kien.le
/// Description: Cấu hình xem ảnh camera công khai cho web người dân (section "Service:Public"), KHÔNG cần đăng nhập/ticket.
/// Danh sách equipment được công khai nằm ở SysOpConfig (GroupCode = cctv_public_equipment_ids, mỗi dòng Value = equipmentId).
/// Created date: 06/10/2026
/// </summary>
public sealed class PublicSnapshotOptions
{
    /// <summary>Bật/tắt endpoint /public/equipments/{equipmentId}/snapshot.jpg.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Kích thước ảnh cố định (người dân không được tự chọn — tránh vượt cache). Trùng với ảnh Hangfire chụp.</summary>
    public string SnapshotSize { get; set; } = "640x360";

    /// <summary>Thời gian (giây) dùng chung một ảnh cho mọi người xem cùng camera — NxVMS bị gọi tối đa 1 lần / chu kỳ / camera.</summary>
    public int CacheSeconds { get; set; } = 5;

    /// <summary>Thời gian (giây) cache danh sách equipment công khai + ánh xạ equipment → camera (đọc từ DB).</summary>
    public int DirectoryCacheSeconds { get; set; } = 60;

    /// <summary>
    /// Thư mục thumbnail của WebAPI (wwwroot/CCTV/snapshot). Có giá trị thì:
    /// - ảnh mới lấy được sẽ GHI ĐÈ file thumbnail tại đây + cập nhật CctvDevice.SnapshotTime (FE nội bộ thấy ảnh mới nhất);
    /// - khi NxVMS lỗi / camera offline thì trả ảnh thumbnail tại đây (fallback).
    /// Để trống = không ghi đè và không fallback. Hỗ trợ đường dẫn mạng (\\server\share\...).
    /// </summary>
    public string WebApiSnapshotDirectory { get; set; } = string.Empty;

    /// <summary>Số request tối đa mỗi phút cho một IP (0 = không giới hạn). Đứng sau reverse proxy/CDN thì mọi người chung 1 IP.</summary>
    public int RateLimitPerMinute { get; set; } = 600;
}
