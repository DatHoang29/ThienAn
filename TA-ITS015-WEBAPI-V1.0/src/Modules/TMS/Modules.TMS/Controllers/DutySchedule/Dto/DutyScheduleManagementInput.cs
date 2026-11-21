namespace Modules.TMS.Controllers.DutySchedule.Dto;

/// <summary>
/// Input để đổi ca giữa 2 người ở 2 lịch trực khác nhau
/// Author: System
/// Created date: 21/11/2025
/// </summary>
public class CrossScheduleSwapInput
{
    /// <summary>
    /// ID lịch trực của người yêu cầu
    /// </summary>
    public string MyScheduleId { get; set; } = string.Empty;

    /// <summary>
    /// ID lịch trực của người được đổi
    /// </summary>
    public string TargetScheduleId { get; set; } = string.Empty;

    /// <summary>
    /// ID người được đổi (username)
    /// </summary>
    public string TargetUserId { get; set; } = string.Empty;

    /// <summary>
    /// Lý do đổi ca
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Ghi chú
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// Output danh sách lịch trực của người dùng cụ thể (để hiển thị khi chọn người đổi)
/// </summary>
public class UserScheduleOutput
{
    /// <summary>
    /// Thông tin lịch trực
    /// </summary>
    public DutyScheduleOutput? Schedule { get; set; }

    /// <summary>
    /// Vai trò của user trong lịch này
    /// </summary>
    public string? Role { get; set; }

    /// <summary>
    /// Trạng thái của user trong lịch này
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Có conflict thời gian với lịch của người yêu cầu không
    /// </summary>
    public bool HasTimeConflict { get; set; }

    /// <summary>
    /// Có thể đổi ca không
    /// </summary>
    public bool CanSwap { get; set; }

    /// <summary>
    /// Lý do không thể đổi (nếu có)
    /// </summary>
    public string? ConflictReason { get; set; }
}

/// <summary>
/// Input để lấy danh sách lịch trực của user
/// </summary>
public class GetUserSchedulesInput
{
    /// <summary>
    /// Username của người cần xem lịch
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// ID lịch trực của người yêu cầu (để check conflict)
    /// </summary>
    public string? MyScheduleId { get; set; }

    /// <summary>
    /// Chỉ lấy lịch đang active (Status = 'join')
    /// </summary>
    public bool ActiveOnly { get; set; } = true;
}

/// <summary>
/// Input để thay đổi vai trò nhân sự trong lịch trực
/// </summary>
public class UpdateMemberRoleInput
{
    /// <summary>
    /// ID lịch trực
    /// </summary>
    public string DutyScheduleId { get; set; } = string.Empty;

    /// <summary>
    /// ID nhân sự cần thay đổi vai trò
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Vai trò mới (leader/member)
    /// </summary>
    public string NewRole { get; set; } = string.Empty;

    /// <summary>
    /// Lý do thay đổi
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// Input để thêm nhân sự mới vào lịch trực
/// </summary>
public class AddMemberToScheduleInput
{
    /// <summary>
    /// ID lịch trực
    /// </summary>
    public string DutyScheduleId { get; set; } = string.Empty;

    /// <summary>
    /// Danh sách ID nhân sự cần thêm (username)
    /// </summary>
    public List<string> UserIds { get; set; } = new();

    /// <summary>
    /// Vai trò mặc định cho nhân sự mới (leader/member)
    /// </summary>
    public string DefaultRole { get; set; } = "member";

    /// <summary>
    /// Lý do thêm
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// Input để xóa nhân sự khỏi lịch trực
/// </summary>
public class RemoveMemberFromScheduleInput
{
    /// <summary>
    /// ID lịch trực
    /// </summary>
    public string DutyScheduleId { get; set; } = string.Empty;

    /// <summary>
    /// ID nhân sự cần xóa
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Lý do xóa
    /// </summary>
    public string? Reason { get; set; }
}
