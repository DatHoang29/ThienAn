using Modules.TMS.Core.Entities;

namespace Modules.TMS.Controllers.PatrolTeam.Dto;

public class AddMemberToTeamInput
{
    [Required]
    public string PatrolTeamId { get; set; } = string.Empty;

    [Required]
    public string UserId { get; set; } = string.Empty;

    public string? Role { get; set; }

    public string? Remark { get; set; }
}

public class RemoveMemberFromTeamInput
{
    [Required]
    public string MemberId { get; set; } = string.Empty;
}

public class UpdateMemberRoleInput
{
    [Required]
    public string MemberId { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = string.Empty;
}

public class PatrolTeamMemberOutput : TmsPatrolTeamMember
{
}
