using System.ComponentModel.DataAnnotations;

namespace Modules.TMS.Core.Dto.Incident;

/// <summary>
/// Author: KienLT
/// Description: Người vận hành tạo sự cố từ một bản tin VDS đã có (tab Sự kiện). Chỉ gửi KHÓA bản tin; loại sự cố,
///              vị trí, thời điểm, camera, ảnh do BE đọc lại từ bản tin gốc nên không thể bị sửa từ giao diện.
///              Không kế thừa DTO nào khác (Wolverine có thể khớp handler theo kiểu cha).
/// Created date: 23/10/2026
/// </summary>
public class CreateIncidentFromVdsEventInput
{
    /// <summary>Id bản tin của thiết bị (VdsTrafficData.VendorEventId).</summary>
    [Required]
    public long VendorEventId { get; set; }

    /// <summary>Thời điểm phát hiện, UTC, giữ nguyên microsecond như FE nhận từ API sự kiện.</summary>
    [Required]
    public DateTime DetectTime { get; set; }

    /// <summary>
    /// KienLT - 26/10/2026: hình thức tạo (IncidentConst.FromVdsEvent.Mode):
    /// <c>process</c> = vào luồng xử lý (Đăng ký, có cấu hình tự động thì đếm ngược như sự cố VDS tự tạo);
    /// <c>record</c> = chỉ ghi nhận, tạo thẳng ở trạng thái kết thúc <see cref="RecordState"/>.
    /// </summary>
    [Required]
    public string? Mode { get; set; }

    /// <summary>Trạng thái kết thúc khi <see cref="Mode"/> = record: 4 Hoàn thành, 0 Bỏ qua, 5 Huỷ (TmsIncidentStateEnum).</summary>
    public int? RecordState { get; set; }

    /// <summary>Mô tả; bỏ trống thì BE dựng từ bản tin (biển số, tốc độ, làn).</summary>
    public string? Description { get; set; }

    public string? Remark { get; set; }

    /// <summary>Vẫn tạo dù đang có sự cố mở cùng loại, cùng vị trí (người vận hành đã xác nhận).</summary>
    public bool Force { get; set; }
}
