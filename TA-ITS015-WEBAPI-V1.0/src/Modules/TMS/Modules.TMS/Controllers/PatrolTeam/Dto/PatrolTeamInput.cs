using Modules.TMS.Core.Entities;
using Shared.Core.Utilities;

namespace Modules.TMS.Controllers.PatrolTeam.Dto;

/// <summary>
/// Author: System
/// Created date: 07/11/2025
/// Description: Input for PatrolTeam list/filter
/// </summary>
public class PatrolTeamInput
{
    /// <summary>
    /// Tên đội tuần tra
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Mã đội
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// Trạng thái
    /// </summary>
    public int? Status { get; set; }
}

/// <summary>
/// Author: System
/// Created date: 07/11/2025
/// Description: DTO for Add PatrolTeam
/// </summary>
public class AddPatrolTeamInput : TmsPatrolTeam
{
    /// <summary>
    /// Danh sách thành viên (optional)
    /// </summary>
    public List<PatrolTeamMemberItem>? Members { get; set; }
}

/// <summary>
/// Chi tiết thành viên đội tuần tra
/// </summary>
public class PatrolTeamMemberItem
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
    /// Ghi chú
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// Author: System
/// Created date: 07/11/2025
/// Description: DTO for Update PatrolTeam
/// </summary>
public class UpdatePatrolTeamInput : AddPatrolTeamInput
{
}

/// <summary>
/// Author: System
/// Created date: 07/11/2025
/// Description: DTO for Delete PatrolTeam
/// </summary>
public class DeletePatrolTeamInput : BaseIdInput
{
}
