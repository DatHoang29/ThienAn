namespace ITS.Toll.Core.Configuration;

/// <summary>
/// Cấu hình gốc cho công cụ test API thu phí. Nạp từ bảng SysOpConfig (nhóm toll_config) qua <see cref="TollOpConfigReader"/>.
/// Các giá trị khởi tạo của thuộc tính bên dưới chỉ là fallback khi tham số tương ứng bị thiếu trong cấu hình.
/// Author: ChuongLP
/// Created date: 24/08/2026
/// </summary>
public sealed class TollOptions
{

    /// <summary>
    /// URL gốc dùng khi <see cref="TollApiEndpoint.Url"/> là đường dẫn tương đối.
    /// Nếu endpoint đã là URL tuyệt đối thì bỏ qua giá trị này.
    /// </summary>
    public string? BaseUrl { get; set; }

    /// <summary>Thời gian chờ tối đa mỗi lần gọi (giây).</summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>Định dạng ngày khi truyền FromDate/ToDate lên query string.</summary>
    public string DateFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";

    /// <summary>Chu kỳ (giây) cho chế độ tự động gọi tất cả API đang bật.</summary>
    public int AutoCallIntervalSeconds { get; set; } = 60;

    /// <summary>Danh sách API cần test. Nguồn là bảng SysOpConfig (nhóm toll_config) — KHÔNG hard-code trong code.</summary>
    public List<TollApiEndpoint> Endpoints { get; set; } = new();
}
