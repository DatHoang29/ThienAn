using Modules.TMS.Core.Entities;
using Shared.Core.Utilities;

namespace Modules.TMS.Controllers.MaintenanceSchedule.Dto;

#region MaintenanceAlert DTOs

/// <summary>
/// Input lấy danh sách cảnh báo
/// Author: System
/// Created date: 10/12/2025
/// </summary>
public class MaintenanceAlertInput
{
    /// <summary>
    /// ID thiết bị
    /// </summary>
    public string? EquipmentId { get; set; }

    /// <summary>
    /// ID loại thiết bị
    /// </summary>
    public string? EquipmentTypeId { get; set; }

    /// <summary>
    /// Loại cảnh báo: RuleTriggered, ManualRequest, ScheduleDue
    /// </summary>
    public string? AlertType { get; set; }

    /// <summary>
    /// Trạng thái: Pending, Acknowledged, Scheduled, Dismissed
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Mức ưu tiên: High, Medium, Low
    /// </summary>
    public string? Priority { get; set; }

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
/// Input phân trang cho cảnh báo
/// Author: System
/// Created date: 10/12/2025
/// </summary>
public class PageMaintenanceAlertInput : BasePageInput
{
    /// <summary>
    /// ID thiết bị
    /// </summary>
    public string? EquipmentId { get; set; }

    /// <summary>
    /// ID loại thiết bị
    /// </summary>
    public string? EquipmentTypeId { get; set; }

    /// <summary>
    /// Loại cảnh báo
    /// </summary>
    public string? AlertType { get; set; }

    /// <summary>
    /// Trạng thái
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Mức ưu tiên
    /// </summary>
    public string? Priority { get; set; }

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
/// Input xác nhận cảnh báo
/// Author: System
/// Created date: 10/12/2025
/// </summary>
public class AcknowledgeAlertInput
{
    /// <summary>
    /// ID cảnh báo
    /// </summary>
    [Required]
    public string ID { get; set; } = string.Empty;

    /// <summary>
    /// Ghi chú
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// Input bỏ qua cảnh báo
/// Author: System
/// Created date: 10/12/2025
/// </summary>
public class DismissAlertInput
{
    /// <summary>
    /// ID cảnh báo
    /// </summary>
    [Required]
    public string ID { get; set; } = string.Empty;

    /// <summary>
    /// Lý do bỏ qua
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// Input tạo lịch từ cảnh báo
/// Author: System
/// Created date: 10/12/2025
/// </summary>
public class CreateScheduleFromAlertInput
{
    /// <summary>
    /// ID cảnh báo
    /// </summary>
    [Required]
    public string AlertId { get; set; } = string.Empty;

    /// <summary>
    /// Tên lịch bảo trì
    /// </summary>
    public string? ScheduleName { get; set; }

    /// <summary>
    /// Mô tả
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Ngày dự kiến
    /// </summary>
    [Required]
    public DateTime ScheduledDate { get; set; }

    /// <summary>
    /// Ngày hạn
    /// </summary>
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Mức ưu tiên
    /// </summary>
    public string? Priority { get; set; }

    /// <summary>
    /// Danh sách ID người phân công
    /// </summary>
    public List<string>? AssignedUserIds { get; set; }

    /// <summary>
    /// Ghi chú
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// Output cảnh báo
/// Author: System
/// Created date: 10/12/2025
/// </summary>
public class MaintenanceAlertOutput : TmsMaintenanceAlert
{
    /// <summary>
    /// Tên thiết bị
    /// </summary>
    public string? EquipmentName { get; set; }

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
    /// Tên rule (nếu từ rule)
    /// </summary>
    public string? AlertRuleName { get; set; }

    /// <summary>
    /// Thông tin guide gợi ý
    /// </summary>
    public MaintenanceGuideOutput? SuggestedGuide { get; set; }

    /// <summary>
    /// Tên người xác nhận
    /// </summary>
    public string? AcknowledgedByName { get; set; }
}

#endregion

#region MaintenanceAlertRule DTOs

/// <summary>
/// Input lấy danh sách rule cảnh báo
/// Author: System
/// Created date: 10/12/2025
/// </summary>
public class MaintenanceAlertRuleInput
{
    /// <summary>
    /// Tên rule
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
    /// Đang hoạt động
    /// </summary>
    public bool? IsActive { get; set; }
}

/// <summary>
/// Input thêm rule cảnh báo
/// Author: System
/// Created date: 10/12/2025
/// </summary>
public class AddMaintenanceAlertRuleInput
{
    /// <summary>
    /// Tên rule
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// ID loại thiết bị (null = tất cả)
    /// </summary>
    public string? EquipmentTypeId { get; set; }

    /// <summary>
    /// Hãng sản xuất (null = tất cả)
    /// </summary>
    public string? Manufacturer { get; set; }

    /// <summary>
    /// Số lần lỗi tối thiểu
    /// </summary>
    [Required]
    public int ErrorThreshold { get; set; } = 3;

    /// <summary>
    /// Trong số ngày
    /// </summary>
    [Required]
    public int PeriodDays { get; set; } = 1;

    /// <summary>
    /// Mức ưu tiên cảnh báo
    /// </summary>
    public string AlertPriority { get; set; } = "Medium";

    /// <summary>
    /// ID guide gợi ý
    /// </summary>
    public string? SuggestedGuideId { get; set; }

    /// <summary>
    /// Trạng thái hoạt động
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Ghi chú
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// Input cập nhật rule cảnh báo
/// Author: System
/// Created date: 10/12/2025
/// </summary>
public class UpdateMaintenanceAlertRuleInput : AddMaintenanceAlertRuleInput
{
    /// <summary>
    /// ID rule
    /// </summary>
    [Required]
    public string ID { get; set; } = string.Empty;
}

/// <summary>
/// Input xóa rule cảnh báo
/// Author: System
/// Created date: 10/12/2025
/// </summary>
public class DeleteMaintenanceAlertRuleInput : BaseIdInput
{
}

/// <summary>
/// Input bật/tắt rule
/// Author: System
/// Created date: 10/12/2025
/// </summary>
public class ToggleAlertRuleActiveInput
{
    /// <summary>
    /// ID rule
    /// </summary>
    [Required]
    public string ID { get; set; } = string.Empty;

    /// <summary>
    /// Trạng thái mới
    /// </summary>
    public bool IsActive { get; set; }
}

/// <summary>
/// Output rule cảnh báo
/// Author: System
/// Created date: 10/12/2025
/// </summary>
public class MaintenanceAlertRuleOutput : TmsMaintenanceAlertRule
{
    /// <summary>
    /// Tên loại thiết bị
    /// </summary>
    public string? EquipmentTypeName { get; set; }

    /// <summary>
    /// Tên guide gợi ý
    /// </summary>
    public string? SuggestedGuideName { get; set; }
}

#endregion

#region Query Input DTOs

/// <summary>
/// Input lấy danh sách cảnh báo với phân trang
/// Author: System
/// Created date: 11/01/2025
/// </summary>
public class GetMaintenanceAlertListInput
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Status { get; set; }
    public string? AlertType { get; set; }
    public string? Priority { get; set; }
    public string? EquipmentId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

/// <summary>
/// Input lấy chi tiết cảnh báo
/// Author: System
/// Created date: 11/01/2025
/// </summary>
public class GetMaintenanceAlertByIdInput
{
    public string ID { get; set; } = string.Empty;
}

/// <summary>
/// Input đếm cảnh báo theo trạng thái
/// Author: System
/// Created date: 11/01/2025
/// </summary>
public class GetAlertCountByStatusInput
{
}

/// <summary>
/// Input lấy danh sách rule với phân trang
/// Author: System
/// Created date: 11/01/2025
/// </summary>
public class GetMaintenanceAlertRuleListInput
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Name { get; set; }
    public string? EquipmentTypeId { get; set; }
    public bool? IsActive { get; set; }
}

/// <summary>
/// Input lấy chi tiết rule
/// Author: System
/// Created date: 11/01/2025
/// </summary>
public class GetMaintenanceAlertRuleByIdInput
{
    public string ID { get; set; } = string.Empty;
}

/// <summary>
/// Input lấy tất cả rule đang active
/// Author: System
/// Created date: 11/01/2025
/// </summary>
public class GetActiveAlertRulesInput
{
}

/// <summary>
/// Input kích hoạt rule
/// Author: System
/// Created date: 11/01/2025
/// </summary>
public class ActivateAlertRuleInput
{
    public string ID { get; set; } = string.Empty;
}

/// <summary>
/// Input hủy kích hoạt rule
/// Author: System
/// Created date: 11/01/2025
/// </summary>
public class DeactivateAlertRuleInput
{
    public string ID { get; set; } = string.Empty;
}

/// <summary>
/// Input tạo cảnh báo thủ công
/// Author: System
/// Created date: 11/01/2025
/// </summary>
public class CreateManualAlertInput
{
    [Required]
    public string EquipmentId { get; set; } = string.Empty;

    public string? AlertMessage { get; set; }

    public string? Priority { get; set; }

    public string? SuggestedGuideId { get; set; }
}

#endregion

#region Output DTOs

/// <summary>
/// Output danh sách cảnh báo
/// Author: System
/// Created date: 11/01/2025
/// </summary>
public class MaintenanceAlertListOutput
{
    public string ID { get; set; } = string.Empty;
    public string EquipmentId { get; set; } = string.Empty;
    public string? EquipmentName { get; set; }
    public string? EquipmentCode { get; set; }
    public string? AlertRuleId { get; set; }
    public string? AlertType { get; set; }
    public string? Status { get; set; }
    public string? AlertMessage { get; set; }
    public int? ErrorCount { get; set; }
    public string? Priority { get; set; }
    public string? SuggestedGuideId { get; set; }
    public string? SuggestedGuideName { get; set; }
    public DateTime? AcknowledgedDate { get; set; }
    public DateTime CreatedTime { get; set; }
}

/// <summary>
/// Output chi tiết cảnh báo
/// Author: System
/// Created date: 11/01/2025
/// </summary>
public class MaintenanceAlertDetailOutput
{
    public string ID { get; set; } = string.Empty;
    public string EquipmentId { get; set; } = string.Empty;
    public EquipmentBasicOutput? Equipment { get; set; }
    public string? AlertRuleId { get; set; }
    public AlertRuleBasicOutput? AlertRule { get; set; }
    public string? AlertType { get; set; }
    public string? Status { get; set; }
    public string? AlertMessage { get; set; }
    public int? ErrorCount { get; set; }
    public int? ErrorPeriodDays { get; set; }
    public string? Priority { get; set; }
    public string? SuggestedGuideId { get; set; }
    public MaintenanceGuideBasicOutput? SuggestedGuide { get; set; }
    public DateTime? AcknowledgedDate { get; set; }
    public string? AcknowledgedByUserId { get; set; }
    public DateTime? DismissedDate { get; set; }
    public string? DismissedByUserId { get; set; }
    public string? DismissReason { get; set; }
    public string? ScheduleId { get; set; }
    public DateTime CreatedTime { get; set; }
}

/// <summary>
/// Output đếm cảnh báo theo trạng thái
/// Author: System
/// Created date: 11/01/2025
/// </summary>
public class AlertCountByStatusOutput
{
    public int TotalCount { get; set; }
    public int PendingCount { get; set; }
    public int AcknowledgedCount { get; set; }
    public int ScheduledCount { get; set; }
    public int DismissedCount { get; set; }
}

/// <summary>
/// Output danh sách rule
/// Author: System
/// Created date: 11/01/2025
/// </summary>
public class MaintenanceAlertRuleListOutput
{
    public string ID { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string EquipmentTypeId { get; set; } = string.Empty;
    public string? EquipmentTypeName { get; set; }
    public string? Manufacturer { get; set; }
    public int ErrorThreshold { get; set; }
    public int PeriodDays { get; set; }
    public string? AlertPriority { get; set; }
    public string? SuggestedGuideId { get; set; }
    public string? SuggestedGuideName { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// Output chi tiết rule
/// Author: System
/// Created date: 11/01/2025
/// </summary>
public class MaintenanceAlertRuleDetailOutput
{
    public string ID { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string EquipmentTypeId { get; set; } = string.Empty;
    public EquipmentTypeBasicOutput? EquipmentType { get; set; }
    public string? Manufacturer { get; set; }
    public int ErrorThreshold { get; set; }
    public int PeriodDays { get; set; }
    public string? AlertPriority { get; set; }
    public string? SuggestedGuideId { get; set; }
    public MaintenanceGuideBasicOutput? SuggestedGuide { get; set; }
    public bool IsActive { get; set; }
    public string? Remark { get; set; }
}

/// <summary>
/// Output thông tin cơ bản thiết bị
/// Author: System
/// Created date: 11/01/2025
/// </summary>
public class EquipmentBasicOutput
{
    public string ID { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? EquipmentTypeId { get; set; }
    public string? EquipmentTypeName { get; set; }
    public string? Manufacturer { get; set; }
    public int? EquipmentState { get; set; }
}

/// <summary>
/// Output thông tin cơ bản loại thiết bị
/// Author: System
/// Created date: 11/01/2025
/// </summary>
public class EquipmentTypeBasicOutput
{
    public string ID { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Name { get; set; }
}

/// <summary>
/// Output thông tin cơ bản rule
/// Author: System
/// Created date: 11/01/2025
/// </summary>
public class AlertRuleBasicOutput
{
    public string ID { get; set; } = string.Empty;
    public string? Name { get; set; }
    public int ErrorThreshold { get; set; }
    public int PeriodDays { get; set; }
}

/// <summary>
/// Output thông tin cơ bản guide
/// Author: System
/// Created date: 11/01/2025
/// </summary>
public class MaintenanceGuideBasicOutput
{
    public string ID { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Frequency { get; set; }
    public int? FrequencyValue { get; set; }
    public int? EstimatedDuration { get; set; }
}

#endregion
