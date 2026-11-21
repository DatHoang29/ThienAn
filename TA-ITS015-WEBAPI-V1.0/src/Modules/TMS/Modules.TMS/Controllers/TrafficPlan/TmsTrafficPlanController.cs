using Modules.TMS.Controllers.TrafficPlan.Dto;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.TMS.Controllers.TrafficPlan;

/// <summary>
/// System TmsTrafficPlanition 
/// </summary>
[ApiDescriptionSettings(GroupName, Order = 460)]
public class TmsTrafficPlanController : BaseController
{
    public TmsTrafficPlanController()
    {

    }

    /// <summary>
    /// Get TmsTrafficPlanition pagination list
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("Get  pagination list")]
    public async Task<SqlSugarPagedList<PageTmsTrafficPlanOutput>> Page(PageTmsTrafficPlanInput input)
    {
        return await MessBus.InvokeAsync<SqlSugarPagedList<PageTmsTrafficPlanOutput>>(input);
    }

    /// <summary>
    /// Get TmsTrafficPlanition list 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("Get  list")]
    public async Task<List<TrafficPlanOutput>> GetList([FromQuery] TrafficPlanInput input)
    {
        return await MessBus.InvokeAsync<List<TrafficPlanOutput>>(input);
    }

    /// <summary>
    /// Add TmsTrafficPlanition
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("Add ")]
    public async Task AddTmsTrafficPlan(AddTmsTrafficPlanInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Update TmsTrafficPlanition 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("Update ")]
    public async Task UpdateTmsTrafficPlan(UpdateTmsTrafficPlanInput input)
    {
        await MessBus.InvokeAsync(input);

    }

    /// <summary>
    /// Delete TmsTrafficPlanition 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("Delete ")]
    public async Task DeleteTmsTrafficPlan(DeleteTmsTrafficPlanInput input)
    {
        await MessBus.InvokeAsync(input);

    }

    /// <summary>
    /// Batch delete TmsTrafficPlanitions 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "BatchDelete"), HttpPost]
    [DisplayName("Batch delete ")]
    public async Task BatchDeleteTmsTrafficPlan(List<DeleteTmsTrafficPlanInput> input)
    {
        await MessBus.InvokeAsync(input);
    }


    /// <summary>
    /// Author: ThangNN
    /// Description: Lấy danh sách kịch bản hợp lệ (là kịch bản đã có map workdetail và status hợp lệ)
    /// </summary>
    [HttpPost("ListValidTrafficPlans")]
    [DisplayName("List the valid trafficPlan-list")]
    public async Task<List<TrafficPlanOutput>> GetList(TmsTrafficPlanValidInput input)
    {
        return await MessBus.InvokeAsync<List<TrafficPlanOutput>>(input);
    }
}
