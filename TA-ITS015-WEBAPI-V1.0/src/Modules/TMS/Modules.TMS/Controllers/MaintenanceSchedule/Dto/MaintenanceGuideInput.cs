using Modules.TMS.Core.Entities;
using Shared.Core.Utilities;

namespace Modules.TMS.Controllers.MaintenanceSchedule.Dto;

#region MaintenanceGuide DTOs

/// <summary>
/// Input để lấy danh sách hướng dẫn bảo trì
/// Author: System
/// Created date: 10/12/2025
/// </summary>
public class MaintenanceGuideInput
{
    /// <summary>
    /// Tên hướng dẫn
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// ID loại thiết bị
    /// </summary>
    public string? EquipmentTypeId { get; set; }

    /// <summary>
    /// Hãng sản xuất
    /// </summary>
    public string? Manufacturer { get; set; }

    /// <summary>
    /// Tần suất: Daily, Weekly, Monthly, Quarterly, Yearly
    /// </summary>
    public string? Frequency { get; set; }

    /// <summary>
    /// Mức ưu tiên: High, Medium, Low
    /// </summary>
    public string? Priority { get; set; }

    /// <summary>
    /// Trạng thái: 0 = Inactive, 1 = Active
    /// </summary>
    public int? Status { get; set; }
}

/// <summary>
/// Input phân trang cho danh sách hướng dẫn bảo trì
/// Author: System
/// Created date: 10/12/2025
/// </summary>
public class PageMaintenanceGuideInput : BasePageInput
{
    /// <summary>
    /// Tên hướng dẫn
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// ID loại thiết bị
    /// </summary>
    public string? EquipmentTypeId { get; set; }

    /// <summary>
    /// Hãng sản xuất
    /// </summary>
    public string? Manufacturer { get; set; }

    /// <summary>
    /// Tần suất
    /// </summary>
    public string? Frequency { get; set; }

    /// <summary>
    /// Mức ưu tiên
    /// </summary>
    public string? Priority { get; set; }

    /// <summary>
    /// Trạng thái
    /// </summary>
    public int? Status { get; set; }
}

/// <summary>
/// Input thêm mới hướng dẫn bảo trì
/// Author: System
/// Created date: 10/12/2025
/// </summary>
public class AddMaintenanceGuideInput
{
    /// <summary>
    /// ID loại thiết bị (bắt buộc)
    /// </summary>
    [Required]
    public string EquipmentTypeId { get; set; } = string.Empty;

    /// <summary>
    /// Hãng sản xuất (null = áp dụng tất cả hãng)
    /// </summary>
    public string? Manufacturer { get; set; }

    /// <summary>
    /// Tên hướng dẫn
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Mô tả chi tiết
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Tần suất: Daily, Weekly, Monthly, Quarterly, Yearly
    /// </summary>
    [Required]
    public string Frequency { get; set; } = "Monthly";

    /// <summary>
    /// Giá trị tần suất (vd: 2 tuần, 3 tháng)
    /// </summary>
    public int FrequencyValue { get; set; } = 1;

    /// <summary>
    /// Thời gian dự kiến (phút)
    /// </summary>
    public int? EstimatedDuration { get; set; }

    /// <summary>
    /// Danh sách các bước thực hiện
    /// </summary>
    public List<ChecklistItem>? Checklist { get; set; }

    /// <summary>
    /// Tài liệu đính kèm (JSON array URLs)
    /// </summary>
    public string? Documents { get; set; }

    /// <summary>
    /// Mức ưu tiên mặc định: High, Medium, Low
    /// </summary>
    public string? Priority { get; set; } = "Medium";

    /// <summary>
    /// Trạng thái: 0 = Inactive, 1 = Active
    /// </summary>
    public int? Status { get; set; } = 1;

    /// <summary>
    /// Ghi chú
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// Input cập nhật hướng dẫn bảo trì
/// Author: System
/// Created date: 10/12/2025
/// </summary>
public class UpdateMaintenanceGuideInput : AddMaintenanceGuideInput
{
    /// <summary>
    /// ID hướng dẫn cần cập nhật
    /// </summary>
    [Required]
    public string ID { get; set; } = string.Empty;
}

/// <summary>
/// Input xóa hướng dẫn bảo trì
/// Author: System
/// Created date: 10/12/2025
/// </summary>
public class DeleteMaintenanceGuideInput : BaseIdInput
{
}

/// <summary>
/// Input upload file tài liệu cho hướng dẫn bảo trì
/// Author: System
/// Created date: 12/12/2025
/// </summary>
public class UploadMaintenanceGuideDocumentInput
{
    /// <summary>
    /// ID hướng dẫn bảo trì
    /// </summary>
    [Required]
    public string MaintenanceGuideId { get; set; } = string.Empty;

    /// <summary>
    /// File tài liệu (IFormFile)
    /// </summary>
    [Required]
    public Microsoft.AspNetCore.Http.IFormFile File { get; set; } = null!;
}

/// <summary>
/// Output file tài liệu đính kèm
/// Author: System
/// Created date: 12/12/2025
/// </summary>
public class GuideDocumentInfo
{
    /// <summary>
    /// Đường dẫn file tương đối
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Tên file gốc
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Ngày upload
    /// </summary>
    public DateTime UploadedAt { get; set; }
}

/// <summary>
/// Input lấy hướng dẫn bảo trì phù hợp cho thiết bị
/// Author: System
/// Created date: 10/12/2025
/// </summary>
public class GetGuideByEquipmentInput
{
    /// <summary>
    /// ID thiết bị (nếu có sẽ tự lấy EquipmentTypeId và Manufacturer từ thiết bị)
    /// </summary>
    public string? EquipmentId { get; set; }

    /// <summary>
    /// ID loại thiết bị (dùng khi không có EquipmentId)
    /// </summary>
    public string? EquipmentTypeId { get; set; }

    /// <summary>
    /// Hãng sản xuất (dùng khi không có EquipmentId)
    /// </summary>
    public string? Manufacturer { get; set; }
}

/// <summary>
/// Một bước trong checklist
/// Author: System
/// Created date: 10/12/2025
/// </summary>
public class ChecklistItem
{
    /// <summary>
    /// Nội dung công việc (có thể chứa HTML)
    /// </summary>
    public string Task { get; set; } = string.Empty;

    /// <summary>
    /// Có bắt buộc không
    /// </summary>
    public bool Required { get; set; } = true;

    /// <summary>
    /// Ghi chú (nếu có, có thể chứa HTML)
    /// </summary>
    public string? Note { get; set; }
}

/// <summary>
/// Output hướng dẫn bảo trì
/// Author: System
/// Created date: 10/12/2025
/// </summary>
public class MaintenanceGuideOutput : TmsMaintenanceGuide
{
    /// <summary>
    /// Tên loại thiết bị
    /// </summary>
    public string? EquipmentTypeName { get; set; }

    /// <summary>
    /// Danh sách checklist (đã parse từ JSON)
    /// </summary>
    public List<ChecklistItem>? ChecklistItems { get; set; }

    /// <summary>
    /// Loại match: exact (khớp cả type và manufacturer) / generic (chỉ khớp type)
    /// </summary>
    public string? MatchType { get; set; }
}

#endregion
