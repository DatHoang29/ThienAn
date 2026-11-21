namespace ITS.Toll.Core.Configuration;

/// <summary>
/// Cấu hình service Toll (bind từ section "Toll" trong appsettings.json).
/// Chuỗi kết nối DB nằm ở section "DbConnection" (DbConnectionOptions của Services.Shared.Runtime), KHÔNG ở đây.
/// Author: ChuongLP
/// Created date: 26/08/2026
/// </summary>
public sealed class TollServiceOptions
{
    public const string SectionName = "Toll";

    /// <summary>TenantId gán cho bản ghi mới do công cụ tạo.</summary>
    public string TenantId { get; set; } = "Default";

    /// <summary>
    /// Thư mục lưu vết file (con trỏ resume + history). Tương đối tính từ thư mục chạy exe.
    /// Mặc định "State" → {exe}/State/{cursor,history}.
    /// </summary>
    public string StatePath { get; set; } = "State";

    /// <summary>
    /// Cửa sổ chồng lấn (phút) khi cuộn cửa sổ theo watermark: From = watermark - Overlap.
    /// Giúp không sót bản ghi về trễ; upsert theo khóa tự nhiên khử trùng phần chồng lấn.
    /// </summary>
    public int OverlapMinutes { get; set; } = 5;

    /// <summary>
    /// Lần chạy đầu (chưa có watermark) lấy lùi lại bao nhiêu giờ tính từ hiện tại. Mặc định 24h.
    /// </summary>
    public int InitialLookbackHours { get; set; } = 24;
}
