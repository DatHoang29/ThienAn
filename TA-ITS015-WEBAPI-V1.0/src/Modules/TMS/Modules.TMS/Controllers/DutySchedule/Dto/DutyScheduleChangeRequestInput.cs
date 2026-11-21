using Modules.TMS.Core.Entities;
using Shared.Core.Utilities;

namespace Modules.TMS.Controllers.DutySchedule.Dto;

/// <summary>
/// Input để tạo yêu cầu thay đổi lịch trực
/// Author: System
/// Created date: 19/11/2025
/// </summary>
public class CreateChangeRequestInput
{
    /// <summary>
    /// ID lịch trực
    /// </summary>
    public string DutyScheduleId { get; set; } = string.Empty;

    /// <summary>
    /// Loại yêu cầu: Swap (đổi ca), Absent (xin nghỉ), Replace (thay thế), Transfer (chuyển lịch)
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// ID người liên quan:
    /// - Swap: Người cần trao đổi lịch (bắt buộc)
    /// - Replace: Người bị thay thế (bắt buộc, phải có trong lịch)
    /// </summary>
    public string? TargetUserId { get; set; }

    /// <summary>
    /// ID người thay thế (chỉ dùng cho Replace):
    /// - Nếu không chỉ định: Người đang thao tác sẽ là người thay thế
    /// - Nếu chỉ định: Người được chọn sẽ thay thế vào vị trí
    /// </summary>
    public string? ReplacerId { get; set; }

    /// <summary>
    /// Vai trò mới trong lịch đích (leader/member) - Áp dụng cho Transfer
    /// </summary>
    public string? NewRole { get; set; }

    /// <summary>
    /// ID lịch trực đích (với loại Transfer - chuyển sang lịch khác)
    /// </summary>
    public string? TargetScheduleId { get; set; }

    /// <summary>
    /// Có tạo lịch trực mới không (nếu không chọn TargetScheduleId)
    /// </summary>
    public bool CreateNewSchedule { get; set; }

    /// <summary>
    /// Thông tin lịch trực mới (nếu CreateNewSchedule = true)
    /// </summary>
    public NewScheduleInfo? NewScheduleInfo { get; set; }

    /// <summary>
    /// Lý do yêu cầu
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Ghi chú
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// Thông tin lịch trực mới khi transfer
/// </summary>
public class NewScheduleInfo
{
    /// <summary>
    /// Tên lịch trực
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Thời gian bắt đầu
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Thời gian kết thúc
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// ID đội tuần tra
    /// </summary>
    public string? PatrolTeamId { get; set; }

    /// <summary>
    /// Vai trò trong lịch mới (leader/member)
    /// </summary>
    public string? Role { get; set; }

    /// <summary>
    /// Ghi chú
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// Input để phê duyệt/từ chối yêu cầu thay đổi
/// Author: System
/// Created date: 19/11/2025
/// </summary>
public class ApproveChangeRequestInput
{
    /// <summary>
    /// ID yêu cầu thay đổi
    /// </summary>
    public string ChangeRequestId { get; set; } = string.Empty;

    /// <summary>
    /// Trạng thái: Approved, Rejected
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Ghi chú phê duyệt
    /// </summary>
    public string? ApprovalNote { get; set; }
}

/// <summary>
/// Input để hủy yêu cầu thay đổi (người yêu cầu)
/// Author: System
/// Created date: 19/11/2025
/// </summary>
public class CancelChangeRequestInput
{
    /// <summary>
    /// ID yêu cầu thay đổi
    /// </summary>
    public string ChangeRequestId { get; set; } = string.Empty;
}

/// <summary>
/// Input để lấy danh sách yêu cầu thay đổi
/// Author: System
/// Created date: 19/11/2025
/// </summary>
public class ChangeRequestQueryInput
{
    /// <summary>
    /// ID lịch trực (optional)
    /// </summary>
    public string? DutyScheduleId { get; set; }

    /// <summary>
    /// ID người yêu cầu (optional)
    /// </summary>
    public string? RequesterId { get; set; }

    /// <summary>
    /// Loại yêu cầu (optional)
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Trạng thái (optional)
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Chế độ xem: MyRequests (tôi tạo), PendingApproval (chờ tôi duyệt), All (tất cả)
    /// </summary>
    public ChangeRequestViewMode ViewMode { get; set; } = ChangeRequestViewMode.All;

    /// <summary>
    /// Thời gian bắt đầu từ (lọc theo CreateTime)
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// Thời gian kết thúc đến (lọc theo CreateTime)
    /// </summary>
    public DateTime? ToDate { get; set; }
}

/// <summary>
/// Input phân trang cho danh sách yêu cầu thay đổi
/// Author: System
/// Created date: 25/11/2025
/// </summary>
public class PageChangeRequestInput : BasePageInput
{
    /// <summary>
    /// ID lịch trực (optional)
    /// </summary>
    public string? DutyScheduleId { get; set; }

    /// <summary>
    /// ID người yêu cầu (optional)
    /// </summary>
    public string? RequesterId { get; set; }

    /// <summary>
    /// Loại yêu cầu (optional)
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Trạng thái (optional)
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Chế độ xem: MyRequests (tôi tạo), PendingApproval (chờ tôi duyệt), All (tất cả)
    /// </summary>
    public ChangeRequestViewMode ViewMode { get; set; } = ChangeRequestViewMode.All;

    /// <summary>
    /// Thời gian bắt đầu từ (lọc theo CreateTime)
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// Thời gian kết thúc đến (lọc theo CreateTime)
    /// </summary>
    public DateTime? ToDate { get; set; }
}

/// <summary>
/// Chế độ xem danh sách yêu cầu thay đổi
/// </summary>
public enum ChangeRequestViewMode
{
    /// <summary>
    /// Tất cả yêu cầu
    /// </summary>
    All = 0,

    /// <summary>
    /// Yêu cầu do tôi tạo
    /// </summary>
    MyRequests = 1,

    /// <summary>
    /// Yêu cầu chờ tôi phê duyệt
    /// </summary>
    PendingApproval = 2
}

/// <summary>
/// Output cho yêu cầu thay đổi lịch trực
/// Author: System
/// Created date: 19/11/2025
/// </summary>
public class DutyScheduleChangeRequestOutput : TmsDutyScheduleChangeRequest
{
    /// <summary>
    /// Thông tin lịch trực
    /// </summary>
    public DutyScheduleOutput? DutySchedule { get; set; }

    /// <summary>
    /// Tên người yêu cầu (người tạo request)
    /// </summary>
    public string? RequesterName { get; set; }

    /// <summary>
    /// Tên người liên quan:
    /// - Swap: Người trao đổi lịch
    /// - Replace: Người bị thay thế
    /// </summary>
    public string? TargetUserName { get; set; }

    /// <summary>
    /// Tên người thay thế (chỉ dùng cho Replace)
    /// </summary>
    public string? ReplacerName { get; set; }

    /// <summary>
    /// Tên người phê duyệt
    /// </summary>
    public string? ApproverName { get; set; }
}

/// <summary>
/// Input để lấy danh sách lịch trực có thể chuyển đến (không conflict)
/// Author: System
/// Created date: 20/11/2025
/// Updated: 01/12/2025 - UserId là bắt buộc, thêm IncludePastSchedules
/// </summary>
public class GetAvailableSchedulesInput
{
    /// <summary>
    /// ID lịch trực hiện tại (để tránh)
    /// </summary>
    [Required]
    public string CurrentScheduleId { get; set; } = string.Empty;

    /// <summary>
    /// Username của người dùng cần chuyển (BẮT BUỘC)
    /// </summary>
    [Required]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Khoảng thời gian tìm kiếm (số ngày từ thời điểm bắt đầu của lịch hiện tại, mặc định 30)
    /// </summary>
    public int DateRangeDays { get; set; } = 30;

    /// <summary>
    /// Bao gồm cả lịch đã bắt đầu nhưng chưa kết thúc (mặc định true)
    /// </summary>
    public bool IncludeOngoingSchedules { get; set; } = true;
}

/// <summary>
/// Output lịch trực có thể chuyển đến
/// </summary>
public class AvailableScheduleOutput
{
    /// <summary>
    /// Thông tin lịch trực
    /// </summary>
    public DutyScheduleOutput? Schedule { get; set; }

    /// <summary>
    /// Có conflict thời gian với các lịch khác không (không tính lịch hiện tại vì sẽ rời)
    /// </summary>
    public bool HasTimeConflict { get; set; }

    /// <summary>
    /// Thời gian trùng với lịch hiện tại (chỉ để tham khảo, không ảnh hưởng CanJoin)
    /// </summary>
    public bool HasTimeOverlapWithCurrent { get; set; }

    /// <summary>
    /// Lý do conflict (nếu có)
    /// </summary>
    public string? ConflictReason { get; set; }

    /// <summary>
    /// Có thể tham gia không
    /// </summary>
    public bool CanJoin { get; set; }

    /// <summary>
    /// Mức độ ưu tiên (high/medium/normal)
    /// </summary>
    public string Priority { get; set; } = "normal";

    /// <summary>
    /// Số lượng thành viên hiện tại
    /// </summary>
    public int CurrentMemberCount { get; set; }

    /// <summary>
    /// Số lượng leader
    /// </summary>
    public int LeaderCount { get; set; }

    /// <summary>
    /// Số lượng member
    /// </summary>
    public int MemberCount { get; set; }

    /// <summary>
    /// Có cùng PatrolTeam không
    /// </summary>
    public bool IsSamePatrolTeam { get; set; }
}

#region History DTOs

/// <summary>
/// Input để lấy danh sách lịch sử thay đổi lịch trực
/// Author: System
/// Created date: 25/11/2025
/// </summary>
public class DutyScheduleHistoryQueryInput
{
    /// <summary>
    /// ID lịch trực (optional - nếu không có sẽ lấy tất cả)
    /// </summary>
    public string? DutyScheduleId { get; set; }

    /// <summary>
    /// Loại thay đổi (optional)
    /// </summary>
    public string? ActionType { get; set; }

    /// <summary>
    /// Người thực hiện (optional)
    /// </summary>
    public string? ActionBy { get; set; }

    /// <summary>
    /// Người bị ảnh hưởng (optional)
    /// </summary>
    public string? AffectedUserId { get; set; }

    /// <summary>
    /// Thời gian bắt đầu (optional)
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// Thời gian kết thúc (optional)
    /// </summary>
    public DateTime? ToDate { get; set; }
}

/// <summary>
/// Input để lấy danh sách lịch sử có phân trang
/// Author: System
/// Created date: 25/11/2025
/// </summary>
public class PageDutyScheduleHistoryInput : BasePageInput
{
    /// <summary>
    /// ID lịch trực (optional - nếu không có sẽ lấy tất cả)
    /// </summary>
    public string? DutyScheduleId { get; set; }

    /// <summary>
    /// Loại thay đổi (optional)
    /// </summary>
    public string? ActionType { get; set; }

    /// <summary>
    /// Người thực hiện (optional)
    /// </summary>
    public string? ActionBy { get; set; }

    /// <summary>
    /// Người bị ảnh hưởng (optional)
    /// </summary>
    public string? AffectedUserId { get; set; }

    /// <summary>
    /// Thời gian bắt đầu (optional)
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// Thời gian kết thúc (optional)
    /// </summary>
    public DateTime? ToDate { get; set; }
}

/// <summary>
/// Output lịch sử thay đổi lịch trực
/// Author: System
/// Created date: 25/11/2025
/// Updated: 02/12/2025 - Thêm RelatedScheduleName
/// </summary>
public class DutyScheduleHistoryOutput : TmsDutyScheduleHistory
{
    /// <summary>
    /// Tên lịch trực
    /// </summary>
    public string? DutyScheduleName { get; set; }

    /// <summary>
    /// Tên lịch trực liên quan (với Swap, Transfer)
    /// </summary>
    public string? RelatedScheduleName { get; set; }

    /// <summary>
    /// Tên người thực hiện
    /// </summary>
    public string? ActionByName { get; set; }

    /// <summary>
    /// Tên người bị ảnh hưởng
    /// </summary>
    public string? AffectedUserName { get; set; }

    /// <summary>
    /// Mô tả loại thay đổi (hiển thị)
    /// </summary>
    public string? ActionTypeDisplay { get; set; }
}

#endregion
