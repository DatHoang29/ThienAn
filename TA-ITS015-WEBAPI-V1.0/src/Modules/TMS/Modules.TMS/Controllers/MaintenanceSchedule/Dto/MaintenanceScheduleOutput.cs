using Modules.TMS.Core.Entities;

namespace Modules.TMS.Controllers.MaintenanceSchedule.Dto;

/// <summary>
/// Output lịch bảo trì
/// Author: System
/// Created date: 10/12/2025
/// </summary>
public class MaintenanceScheduleOutput : TmsMaintenanceSchedule
{
    /// <summary>
    /// Tên người tạo
    /// </summary>
    public string? CreateUserName { get; set; }

    /// <summary>
    /// Số lượng thiết bị trong lịch
    /// </summary>
    public int EquipmentCount { get; set; }

    /// <summary>
    /// Số lượng thiết bị đã hoàn thành
    /// </summary>
    public int CompletedEquipmentCount { get; set; }

    /// <summary>
    /// Số lượng công việc/phân công
    /// </summary>
    public int TaskCount { get; set; }

    /// <summary>
    /// Số lượng công việc đã hoàn thành
    /// </summary>
    public int CompletedTaskCount { get; set; }

    /// <summary>
    /// Tóm tắt thiết bị theo loại
    /// </summary>
    public List<EquipmentTypeSummary>? EquipmentSummary { get; set; }

    /// <summary>
    /// Danh sách thiết bị
    /// </summary>
    public List<ScheduleEquipmentOutput>? Equipments { get; set; }

    /// <summary>
    /// Danh sách công việc/phân công
    /// </summary>
    public List<MaintenanceTaskOutput>? Tasks { get; set; }
}

/// <summary>
/// Output chi tiết thiết bị trong lịch bảo trì
/// Author: System
/// Created date: 10/12/2025
/// </summary>
public class ScheduleEquipmentOutput : TmsMaintenanceScheduleEquipment
{
    /// <summary>
    /// Tên thiết bị
    /// </summary>
    public string? EquipmentName { get; set; }

    /// <summary>
    /// Mã thiết bị
    /// </summary>
    public string? EquipmentCode { get; set; }

    /// <summary>
    /// Tên loại thiết bị
    /// </summary>
    public string? EquipmentTypeName { get; set; }

    /// <summary>
    /// ID loại thiết bị
    /// </summary>
    public string? EquipmentTypeId { get; set; }

    /// <summary>
    /// Hãng sản xuất
    /// </summary>
    public string? Manufacturer { get; set; }

    /// <summary>
    /// Vị trí (KM)
    /// </summary>
    public int? KmNumber { get; set; }

    /// <summary>
    /// Trạng thái hiện tại của thiết bị
    /// </summary>
    public int? EquipmentState { get; set; }

    /// <summary>
    /// Tên hướng dẫn bảo trì
    /// </summary>
    public string? MaintenanceGuideName { get; set; }

    /// <summary>
    /// Checklist từ guide (đã parse)
    /// </summary>
    public List<ChecklistItem>? Checklist { get; set; }

    /// <summary>
    /// Checklist đã hoàn thành (đã parse)
    /// </summary>
    public List<ChecklistItem>? ChecklistCompleted { get; set; }

    /// <summary>
    /// Tên người hoàn thành
    /// </summary>
    public string? CompletedByName { get; set; }
}

/// <summary>
/// Output công việc/phân công
/// Author: System
/// Created date: 10/12/2025
/// </summary>
public class MaintenanceTaskOutput : TmsMaintenanceTask
{
    /// <summary>
    /// Tên người được phân công
    /// </summary>
    public string? AssignedUserName { get; set; }

    /// <summary>
    /// Tên thiết bị được phân công (nếu có)
    /// </summary>
    public string? EquipmentName { get; set; }
}

/// <summary>
/// Tóm tắt thiết bị theo loại
/// Author: System
/// Created date: 10/12/2025
/// </summary>
public class EquipmentTypeSummary
{
    /// <summary>
    /// ID loại thiết bị
    /// </summary>
    public string? EquipmentTypeId { get; set; }

    /// <summary>
    /// Tên loại thiết bị
    /// </summary>
    public string? EquipmentTypeName { get; set; }

    /// <summary>
    /// Số lượng
    /// </summary>
    public int Count { get; set; }
}

/// <summary>
/// Output lịch sử thay đổi
/// Author: System
/// Created date: 10/12/2025
/// </summary>
public class MaintenanceHistoryOutput : TmsMaintenanceHistory
{
    /// <summary>
    /// Tên lịch bảo trì
    /// </summary>
    public string? MaintenanceScheduleName { get; set; }

    /// <summary>
    /// Tên người thực hiện
    /// </summary>
    public string? ActionByName { get; set; }

    /// <summary>
    /// Tên người bị ảnh hưởng
    /// </summary>
    public string? AffectedUserName { get; set; }

    /// <summary>
    /// Tên thiết bị bị ảnh hưởng
    /// </summary>
    public string? AffectedEquipmentName { get; set; }

    /// <summary>
    /// Mô tả loại thay đổi (hiển thị)
    /// </summary>
    public string? ActionTypeDisplay { get; set; }
}

/// <summary>
/// Output tổng hợp theo tháng/năm
/// Author: System
/// Created date: 10/12/2025
/// </summary>
public class ScheduleCalendarSummary
{
    /// <summary>
    /// Tổng số lịch
    /// </summary>
    public int Total { get; set; }

    /// <summary>
    /// Thống kê theo trạng thái
    /// </summary>
    public Dictionary<string, int>? ByStatus { get; set; }

    /// <summary>
    /// Thống kê theo mức ưu tiên
    /// </summary>
    public Dictionary<string, int>? ByPriority { get; set; }

    /// <summary>
    /// Thống kê theo ngày (cho calendar)
    /// </summary>
    public List<DaySummary>? ByDay { get; set; }
}

/// <summary>
/// Tổng hợp theo ngày
/// </summary>
public class DaySummary
{
    /// <summary>
    /// Ngày
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Số lượng lịch
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Có lịch High priority không
    /// </summary>
    public bool HasHighPriority { get; set; }
}

/// <summary>
/// Response cho GetByMonth
/// Author: System
/// Created date: 10/12/2025
/// </summary>
public class GetByMonthResponse
{
    /// <summary>
    /// Danh sách lịch bảo trì
    /// </summary>
    public List<MaintenanceScheduleOutput>? Data { get; set; }

    /// <summary>
    /// Tổng hợp
    /// </summary>
    public ScheduleCalendarSummary? Summary { get; set; }
}
