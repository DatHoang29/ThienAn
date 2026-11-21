using Shared.Core.Utilities;

namespace Modules.TMS.Controllers.MaintenanceSchedule.Dto;

#region MaintenanceSchedule Filter Enums

/// <summary>
/// Chế độ lọc danh sách lịch bảo trì
/// </summary>
public enum MaintenanceScheduleFilterMode
{
    /// <summary>
    /// Tất cả lịch
    /// </summary>
    All = 0,

    /// <summary>
    /// Quản lý: Chỉ lịch do người dùng hiện tại tạo
    /// </summary>
    CreatedByMe = 1,

    /// <summary>
    /// Tham gia: Chỉ lịch tôi được phân công
    /// </summary>
    AssignedToMe = 2
}

#endregion

#region MaintenanceSchedule Query DTOs

/// <summary>
/// Input lấy chi tiết lịch bảo trì theo ID
/// Author: System
/// Created date: 11/12/2025
/// </summary>
public class GetMaintenanceScheduleByIdInput
{
    /// <summary>
    /// ID lịch bảo trì
    /// </summary>
    [Required]
    public string ID { get; set; } = string.Empty;
}

/// <summary>
/// Input để lấy danh sách lịch bảo trì
/// Author: System
/// Created date: 10/12/2025
/// </summary>
public class MaintenanceScheduleInput
{
    /// <summary>
    /// Tên lịch bảo trì
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Ngày bắt đầu (từ)
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// Ngày kết thúc (đến)
    /// </summary>
    public DateTime? ToDate { get; set; }

    /// <summary>
    /// Trạng thái: Draft, Scheduled, InProgress, Completed, Cancelled
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Mức ưu tiên: High, Medium, Low
    /// </summary>
    public string? Priority { get; set; }

    /// <summary>
    /// ID loại thiết bị (lọc theo thiết bị trong lịch)
    /// </summary>
    public string? EquipmentTypeId { get; set; }

    /// <summary>
    /// ID thiết bị cụ thể
    /// </summary>
    public string? EquipmentId { get; set; }

    /// <summary>
    /// ID người được phân công
    /// </summary>
    public string? AssignedUserId { get; set; }

    /// <summary>
    /// Chế độ lọc: All (0), CreatedByMe (1), AssignedToMe (2)
    /// </summary>
    public MaintenanceScheduleFilterMode FilterMode { get; set; } = MaintenanceScheduleFilterMode.All;
}

/// <summary>
/// Input phân trang cho danh sách lịch bảo trì
/// Author: System
/// Created date: 10/12/2025
/// </summary>
public class PageMaintenanceScheduleInput : BasePageInput
{
    /// <summary>
    /// Tên lịch bảo trì
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Ngày bắt đầu (từ)
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// Ngày kết thúc (đến)
    /// </summary>
    public DateTime? ToDate { get; set; }

    /// <summary>
    /// Trạng thái
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Mức ưu tiên
    /// </summary>
    public string? Priority { get; set; }

    /// <summary>
    /// ID loại thiết bị
    /// </summary>
    public string? EquipmentTypeId { get; set; }

    /// <summary>
    /// ID thiết bị cụ thể
    /// </summary>
    public string? EquipmentId { get; set; }

    /// <summary>
    /// ID người được phân công
    /// </summary>
    public string? AssignedUserId { get; set; }

    /// <summary>
    /// Chế độ lọc
    /// </summary>
    public MaintenanceScheduleFilterMode FilterMode { get; set; } = MaintenanceScheduleFilterMode.All;
}

/// <summary>
/// Input lấy lịch bảo trì theo tuần
/// Author: System
/// Created date: 10/12/2025
/// </summary>
public class GetByWeekInput
{
    /// <summary>
    /// Ngày bất kỳ trong tuần cần xem
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Trạng thái
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Mức ưu tiên
    /// </summary>
    public string? Priority { get; set; }

    /// <summary>
    /// Chế độ lọc
    /// </summary>
    public MaintenanceScheduleFilterMode FilterMode { get; set; } = MaintenanceScheduleFilterMode.All;
}

/// <summary>
/// Input lấy lịch bảo trì theo tháng
/// Author: System
/// Created date: 10/12/2025
/// </summary>
public class GetByMonthInput
{
    /// <summary>
    /// Năm
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Tháng (1-12)
    /// </summary>
    public int Month { get; set; }

    /// <summary>
    /// Trạng thái
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Mức ưu tiên
    /// </summary>
    public string? Priority { get; set; }

    /// <summary>
    /// Chế độ lọc
    /// </summary>
    public MaintenanceScheduleFilterMode FilterMode { get; set; } = MaintenanceScheduleFilterMode.All;
}

/// <summary>
/// Input lấy lịch bảo trì theo năm (summary)
/// Author: System
/// Created date: 10/12/2025
/// </summary>
public class GetByYearInput
{
    /// <summary>
    /// Năm
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Trạng thái
    /// </summary>
    public string? Status { get; set; }
}

/// <summary>
/// Input lấy lịch bảo trì sắp tới
/// Author: System
/// Created date: 10/12/2025
/// </summary>
public class GetUpcomingMaintenanceSchedulesInput
{
    /// <summary>
    /// ID người dùng (nếu null thì lấy của user hiện tại)
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Số lượng lịch tối đa (mặc định 20)
    /// </summary>
    public int MaxCount { get; set; } = 20;

    /// <summary>
    /// Số ngày tối đa (mặc định 7)
    /// </summary>
    public int MaxDays { get; set; } = 7;

    /// <summary>
    /// Chỉ lấy lịch đang active
    /// </summary>
    public bool ActiveOnly { get; set; } = true;
}

#endregion

#region MaintenanceSchedule Command DTOs

/// <summary>
/// Input thêm mới lịch bảo trì
/// Author: System
/// Created date: 10/12/2025
/// </summary>
public class AddMaintenanceScheduleInput
{
    /// <summary>
    /// Tên lịch bảo trì
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Ngày dự kiến bắt đầu
    /// </summary>
    [Required]
    public DateTime ScheduledDate { get; set; }

    /// <summary>
    /// Ngày hạn hoàn thành
    /// </summary>
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Mức ưu tiên: High, Medium, Low
    /// </summary>
    public string Priority { get; set; } = "Medium";

    /// <summary>
    /// Số ngày nhắc trước
    /// </summary>
    public int? ReminderDaysBefore { get; set; } = 1;

    /// <summary>
    /// Trạng thái ban đầu: Draft, Scheduled
    /// </summary>
    public string Status { get; set; } = "Draft";

    /// <summary>
    /// Ghi chú
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// Danh sách thiết bị cần bảo trì
    /// </summary>
    public List<ScheduleEquipmentInput>? Equipments { get; set; }

    /// <summary>
    /// Danh sách công việc/phân công
    /// </summary>
    public List<MaintenanceTaskInput>? Tasks { get; set; }
}

/// <summary>
/// Input cập nhật lịch bảo trì
/// Author: System
/// Created date: 10/12/2025
/// </summary>
public class UpdateMaintenanceScheduleInput : AddMaintenanceScheduleInput
{
    /// <summary>
    /// ID lịch bảo trì cần cập nhật
    /// </summary>
    [Required]
    public string ID { get; set; } = string.Empty;
}

/// <summary>
/// Input xóa lịch bảo trì
/// Author: System
/// Created date: 10/12/2025
/// </summary>
public class DeleteMaintenanceScheduleInput : BaseIdInput
{
}

/// <summary>
/// Input cập nhật trạng thái lịch bảo trì
/// Author: System
/// Created date: 10/12/2025
/// </summary>
public class UpdateMaintenanceScheduleStatusInput
{
    /// <summary>
    /// ID lịch bảo trì
    /// </summary>
    [Required]
    public string ID { get; set; } = string.Empty;

    /// <summary>
    /// Trạng thái mới: Draft, Scheduled, InProgress, Completed, Cancelled
    /// </summary>
    [Required]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Kết quả (khi hoàn thành)
    /// </summary>
    public string? Result { get; set; }

    /// <summary>
    /// Lý do (khi hủy)
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// Input thiết bị trong lịch bảo trì
/// Author: System
/// Created date: 10/12/2025
/// </summary>
public class ScheduleEquipmentInput
{
    /// <summary>
    /// ID thiết bị
    /// </summary>
    [Required]
    public string EquipmentId { get; set; } = string.Empty;

    /// <summary>
    /// ID hướng dẫn bảo trì áp dụng (optional - hệ thống sẽ tự suggest)
    /// </summary>
    public string? MaintenanceGuideId { get; set; }

    /// <summary>
    /// Ghi chú
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// Input thêm thiết bị vào lịch bảo trì
/// Author: System
/// Created date: 10/12/2025
/// </summary>
public class AddEquipmentToScheduleInput
{
    /// <summary>
    /// ID lịch bảo trì
    /// </summary>
    [Required]
    public string MaintenanceScheduleId { get; set; } = string.Empty;

    /// <summary>
    /// ID thiết bị
    /// </summary>
    [Required]
    public string EquipmentId { get; set; } = string.Empty;

    /// <summary>
    /// ID hướng dẫn bảo trì
    /// </summary>
    public string? MaintenanceGuideId { get; set; }

    /// <summary>
    /// Ghi chú
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// Input xóa thiết bị khỏi lịch bảo trì
/// Author: System
/// Created date: 10/12/2025
/// </summary>
public class RemoveEquipmentFromScheduleInput
{
    /// <summary>
    /// ID record thiết bị trong lịch (TmsMaintenanceScheduleEquipment.ID)
    /// </summary>
    [Required]
    public string ScheduleEquipmentId { get; set; } = string.Empty;

    /// <summary>
    /// Lý do xóa
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// Input cập nhật trạng thái thiết bị trong lịch
/// Author: System
/// Created date: 10/12/2025
/// </summary>
public class UpdateEquipmentStatusInput
{
    /// <summary>
    /// ID record thiết bị trong lịch
    /// </summary>
    [Required]
    public string ScheduleEquipmentId { get; set; } = string.Empty;

    /// <summary>
    /// Trạng thái mới: Pending, InProgress, Completed, Skipped
    /// </summary>
    [Required]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Kết quả bảo trì
    /// </summary>
    public string? Result { get; set; }

    /// <summary>
    /// Checklist đã hoàn thành
    /// </summary>
    public List<ChecklistItem>? CompletedChecklist { get; set; }

    /// <summary>
    /// Ghi chú
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// Input xác nhận đã kiểm tra thiết bị trong lịch
/// Author: System
/// Created date: 20/12/2025
/// </summary>
public class ConfirmEquipmentCheckedInput
{
    /// <summary>
    /// ID record thiết bị trong lịch (TmsMaintenanceScheduleEquipment.ID)
    /// </summary>
    [Required]
    public string ScheduleEquipmentId { get; set; } = string.Empty;

    /// <summary>
    /// Trạng thái kiểm tra: pending | checked | skipped (lower-case)
    /// Nếu null sẽ fallback theo IsChecked.
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Đã kiểm tra hay chưa (tương thích ngược)
    /// </summary>
    public bool IsChecked { get; set; } = true;

    /// <summary>
    /// Ghi chú khi kiểm tra
    /// </summary>
    public string? CheckedNote { get; set; }
}

/// <summary>
/// Input công việc/phân công trong lịch bảo trì
/// Author: System
/// Created date: 10/12/2025
/// </summary>
public class MaintenanceTaskInput
{
    /// <summary>
    /// ID người được phân công
    /// </summary>
    [Required]
    public string AssignedUserId { get; set; } = string.Empty;

    /// <summary>
    /// ID thiết bị cụ thể (null = phụ trách tất cả)
    /// </summary>
    public string? ScheduleEquipmentId { get; set; }

    /// <summary>
    /// Tên công việc
    /// </summary>
    public string? TaskName { get; set; }

    /// <summary>
    /// Mô tả công việc
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Vai trò: Leader, Member
    /// </summary>
    public string? Role { get; set; }

    /// <summary>
    /// Ghi chú
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// Input thêm công việc/phân công vào lịch
/// Author: System
/// Created date: 10/12/2025
/// </summary>
public class AddTaskToScheduleInput
{
    /// <summary>
    /// ID lịch bảo trì
    /// </summary>
    [Required]
    public string MaintenanceScheduleId { get; set; } = string.Empty;

    /// <summary>
    /// ID người được phân công
    /// </summary>
    [Required]
    public string AssignedUserId { get; set; } = string.Empty;

    /// <summary>
    /// ID thiết bị cụ thể
    /// </summary>
    public string? ScheduleEquipmentId { get; set; }

    /// <summary>
    /// Tên công việc
    /// </summary>
    public string? TaskName { get; set; }

    /// <summary>
    /// Mô tả công việc
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Vai trò: Leader, Member
    /// </summary>
    public string? Role { get; set; }

    /// <summary>
    /// Ghi chú
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// Input xóa công việc/phân công khỏi lịch
/// Author: System
/// Created date: 10/12/2025
/// </summary>
public class RemoveTaskFromScheduleInput
{
    /// <summary>
    /// ID công việc
    /// </summary>
    [Required]
    public string TaskId { get; set; } = string.Empty;

    /// <summary>
    /// Lý do xóa
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// Input cập nhật trạng thái công việc
/// Author: System
/// Created date: 10/12/2025
/// </summary>
public class UpdateTaskStatusInput
{
    /// <summary>
    /// ID công việc
    /// </summary>
    [Required]
    public string TaskId { get; set; } = string.Empty;

    /// <summary>
    /// Trạng thái mới (lưu DB dạng chữ thường, cách nhau bởi '_'):
    /// - cho_thuc_hien
    /// - da_thuc_hien
    /// - bo_qua
    /// </summary>
    [Required]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Kết quả công việc
    /// </summary>
    public string? Result { get; set; }

    /// <summary>
    /// Ghi chú
    /// </summary>
    public string? Remark { get; set; }
}

#endregion

#region History DTOs

/// <summary>
/// Input lấy lịch sử thay đổi lịch bảo trì
/// Author: System
/// Created date: 10/12/2025
/// </summary>
public class MaintenanceHistoryQueryInput
{
    /// <summary>
    /// ID lịch bảo trì
    /// </summary>
    public string? MaintenanceScheduleId { get; set; }

    /// <summary>
    /// Loại thay đổi
    /// </summary>
    public string? ActionType { get; set; }

    /// <summary>
    /// Người thực hiện
    /// </summary>
    public string? ActionBy { get; set; }

    /// <summary>
    /// Thời gian bắt đầu
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// Thời gian kết thúc
    /// </summary>
    public DateTime? ToDate { get; set; }
}

/// <summary>
/// Input phân trang cho lịch sử
/// Author: System
/// Created date: 10/12/2025
/// </summary>
public class PageMaintenanceHistoryInput : BasePageInput
{
    /// <summary>
    /// ID lịch bảo trì
    /// </summary>
    public string? MaintenanceScheduleId { get; set; }

    /// <summary>
    /// Loại thay đổi
    /// </summary>
    public string? ActionType { get; set; }

    /// <summary>
    /// Người thực hiện
    /// </summary>
    public string? ActionBy { get; set; }

    /// <summary>
    /// Thời gian bắt đầu
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// Thời gian kết thúc
    /// </summary>
    public DateTime? ToDate { get; set; }
}

#endregion
