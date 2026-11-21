using Modules.TMS.Core.Entities;
using Shared.Core.Utilities;

namespace Modules.TMS.Controllers.DutySchedule.Dto;

/// <summary>
/// Chế độ lọc danh sách lịch trực
/// </summary>
public enum DutyScheduleFilterMode
{
    /// <summary>
    /// Tất cả lịch (nếu có UserId thì tìm theo người dùng đó)
    /// </summary>
    All = 0,

    /// <summary>
    /// Quản lý: Chỉ lịch do người dùng hiện tại tạo
    /// </summary>
    CreatedByMe = 1,

    /// <summary>
    /// Tham gia: Chỉ lịch tôi tham gia với vai trò thành viên hoặc đội trưởng (không bao gồm lịch tạo)
    /// </summary>
    Related = 2
}

/// <summary>
/// Author: System
/// Created date: 07/11/2025
/// Updated: 11/11/2025 - Added FilterMode
/// Description: Input for DutySchedule list/filter
/// </summary>
public class DutyScheduleInput
{
    /// <summary>
    /// Tên ca trực
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Thời gian bắt đầu (từ)
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// Thời gian kết thúc (đến)
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// ID người dùng (dùng cho filter theo user cụ thể)
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Trạng thái
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Chế độ lọc: All (0), CreatedByMe (1), Related (2)
    /// </summary>
    public DutyScheduleFilterMode FilterMode { get; set; } = DutyScheduleFilterMode.All;
}

/// <summary>
/// Author: System
/// Created date: 21/11/2025
/// Description: Input for getting upcoming schedules
/// </summary>
public class GetUpcomingSchedulesInput
{
    /// <summary>
    /// ID người dùng (nếu null thì lấy của user hiện tại)
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Số lượng lịch trực tối đa (mặc định 20)
    /// </summary>
    public int MaxCount { get; set; } = 20;

    /// <summary>
    /// Số ngày tối đa (mặc định 7 ngày)
    /// </summary>
    public int MaxDays { get; set; } = 7;

    /// <summary>
    /// Chỉ lấy lịch đang active (Status = 'join')
    /// </summary>
    public bool ActiveOnly { get; set; } = true;
}

/// <summary>
/// Author: System
/// Created date: 07/11/2025
/// Updated: 11/11/2025 - Added FilterMode
/// Description: Input for getting schedule by week
/// </summary>
public class GetByWeekInput
{
    /// <summary>
    /// Ngày bất kỳ trong tuần cần xem
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// ID người dùng (dùng cho filter theo user cụ thể)
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Chế độ lọc: All (0), CreatedByMe (1), Related (2)
    /// </summary>
    public DutyScheduleFilterMode FilterMode { get; set; } = DutyScheduleFilterMode.All;

    /// <summary>
    /// Trạng thái
    /// </summary>
    public string? Status { get; set; }

}

/// <summary>
/// Author: System
/// Created date: 07/11/2025
/// Description: DTO for Add DutySchedule
/// </summary>
public class AddDutyScheduleInput : TmsDutySchedule
{
    /// <summary>
    /// Danh sách chi tiết người dùng
    /// </summary>
    public List<DutyScheduleDetailInput>? Details { get; set; }
}

/// <summary>
/// Author: System
/// Created date: 07/11/2025
/// Description: DTO for Update DutySchedule
/// </summary>
public class UpdateDutyScheduleInput : AddDutyScheduleInput
{
}

/// <summary>
/// Author: System
/// Created date: 27/11/2025
/// Description: Input cập nhật trạng thái lịch trực
/// </summary>
public class UpdateScheduleStatusInput
{
    /// <summary>
    /// ID lịch trực
    /// </summary>
    [Required]
    public string ID { get; set; } = string.Empty;

    /// <summary>
    /// Trạng thái mới
    /// </summary>
    [Required]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Lý do thay đổi trạng thái
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// Author: System
/// Created date: 27/11/2025
/// Description: Input cập nhật thời gian lịch trực
/// </summary>
public class UpdateScheduleTimeInput
{
    /// <summary>
    /// ID lịch trực
    /// </summary>
    [Required]
    public string ID { get; set; } = string.Empty;

    /// <summary>
    /// Thời gian bắt đầu mới
    /// </summary>
    [Required]
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Thời gian kết thúc mới
    /// </summary>
    [Required]
    public DateTime EndTime { get; set; }

    /// <summary>
    /// Lý do thay đổi thời gian
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// Author: System
/// Created date: 27/11/2025
/// Description: Input cập nhật thông tin cơ bản lịch trực
/// </summary>
public class UpdateScheduleInfoInput
{
    /// <summary>
    /// ID lịch trực
    /// </summary>
    [Required]
    public string ID { get; set; } = string.Empty;

    /// <summary>
    /// Tên lịch trực mới (optional)
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Đội tuần tra mới (optional)
    /// </summary>
    public string? PatrolTeamId { get; set; }

    /// <summary>
    /// Ghi chú mới (optional)
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// Lý do thay đổi
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// Author: System
/// Created date: 07/11/2025
/// Description: DTO for Delete DutySchedule
/// </summary>
public class DeleteDutyScheduleInput : BaseIdInput
{
}

/// <summary>
/// Author: System
/// Created date: 07/11/2025
/// Description: Input for adding user to schedule
/// </summary>
public class AddUserToScheduleInput
{
    /// <summary>
    /// ID lịch trực
    /// </summary>
    [Required]
    public string DutyScheduleId { get; set; } = string.Empty;

    /// <summary>
    /// ID người dùng
    /// </summary>
    [Required]
    public string UserId { get; set; } = string.Empty;

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
/// Author: System
/// Created date: 07/11/2025
/// Description: Input for removing user from schedule
/// </summary>
public class RemoveUserFromScheduleInput
{
    /// <summary>
    /// ID detail cần xóa
    /// </summary>
    [Required]
    public string DetailId { get; set; } = string.Empty;

    /// <summary>
    /// Lý do xóa
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// Author: System
/// Created date: 07/11/2025
/// Description: Input for updating user role
/// </summary>
public class UpdateUserRoleInput
{
    /// <summary>
    /// ID detail cần cập nhật
    /// </summary>
    [Required]
    public string DetailId { get; set; } = string.Empty;

    /// <summary>
    /// Vai trò mới: leader, member
    /// </summary>
    [Required]
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Lý do thay đổi
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// Author: System
/// Created date: 07/11/2025
/// Description: Detail input (for Add/Update schedule)
/// </summary>
public class DutyScheduleDetailInput
{
    /// <summary>
    /// ID người dùng
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Vai trò: Leader, Member
    /// </summary>
    public string? Role { get; set; }

    /// <summary>
    /// Trạng thái
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Ghi chú
    /// </summary>
    public string? Remark { get; set; }
}
