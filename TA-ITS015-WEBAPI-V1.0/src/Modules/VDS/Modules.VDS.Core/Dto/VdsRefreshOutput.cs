namespace Modules.VDS.Core.Dto;

/// <summary>
/// Author: KienLT
/// Description: Kết quả gửi refresh sau khi ghi dữ liệu — hợp đồng TRUNG LẬP, dùng cho mọi feature VDS.
///
/// <para>Đặt ở <c>Modules.VDS.Core/Dto</c> vì nhiều feature cùng trả về hình dạng này. Trước 22/09/2026
/// <c>SegmentSaveOutput</c> kế thừa <c>ConfigurationSaveOutput</c> — feature Segment phụ thuộc feature
/// Configuration chỉ để lấy ba trường chung, không có quan hệ nghiệp vụ nào.</para>
/// Created date: 22/09/2026
/// </summary>
public class VdsRefreshOutput
{
    /// <summary>Kết quả gửi trên từng kênh transport.</summary>
    public Dictionary<string, bool> Refresh { get; set; } = new();

    /// <summary>
    /// Các tiến trình phản hồi đã áp dụng thay đổi hay chưa. Null là chưa có phản hồi;
    /// danh sách Services chỉ gồm tiến trình phản hồi trong thời gian chờ, không khẳng định mọi tiến trình đang chạy.
    /// </summary>
    public bool? Applied { get; set; }

    public List<VdsRefreshAcknowledgement> Services { get; set; } = new();
}
