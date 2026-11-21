using Modules.TMS.Controllers.PatrolTeam.Dto;

namespace Modules.TMS.Controllers.PatrolTeam;

/// <summary>
/// Author: System
/// Description: Tms PatrolTeam Controller
/// Created date: 07/11/2025
/// </summary>
[ApiDescriptionSettings(GroupName, Order = 461)]
public class TmsPatrolTeamController : BaseController
{
    /// <summary>
    /// Get PatrolTeam list
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("Get list")]
    public async Task<List<PatrolTeamOutput>> GetList([FromQuery] PatrolTeamInput input)
    {
        return await MessBus.InvokeAsync<List<PatrolTeamOutput>>(input);
    }

    /// <summary>
    /// Add PatrolTeam
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("Add patrol team")]
    public async Task Add(AddPatrolTeamInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Update PatrolTeam
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("Update patrol team")]
    public async Task Update(UpdatePatrolTeamInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Delete PatrolTeam
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("Delete")]
    public async Task Delete(DeletePatrolTeamInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Batch delete PatrolTeams
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "BatchDelete"), HttpPost]
    [DisplayName("Batch delete")]
    public async Task BatchDelete(List<DeletePatrolTeamInput> input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Add member to patrol team
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "AddMember"), HttpPost]
    [DisplayName("Add member to team")]
    public async Task AddMember(AddMemberToTeamInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Remove member from patrol team
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "RemoveMember"), HttpPost]
    [DisplayName("Remove member from team")]
    public async Task RemoveMember(RemoveMemberFromTeamInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Update member role in patrol team
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "UpdateMemberRole"), HttpPost]
    [DisplayName("Update member role")]
    public async Task UpdateMemberRole(UpdateMemberRoleInput input)
    {
        await MessBus.InvokeAsync(input);
    }
}
