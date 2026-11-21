using Modules.TMS.Core.Entities;

namespace Modules.TMS.Controllers.PatrolTeam.Dto;

/// <summary>
/// Author: System
/// Created date: 07/11/2025
/// Description: Output for PatrolTeam
/// </summary>
public class PatrolTeamOutput : TmsPatrolTeam
{
    /// <summary>
    /// Danh sách thành viên trong đội
    /// </summary>
    public List<PatrolTeamMemberOutput>? Members { get; set; }
}
