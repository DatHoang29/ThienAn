using Modules.TMS.Controllers.TrafficStatistics.Dto;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.TMS.Controllers.TrafficStatistics;

/// <summary>
/// System tmsTrafficStatisticition 
/// </summary>
[ApiDescriptionSettings(GroupName, Order = 460)]
public class tmsTrafficStatisticController : BaseController
{

    /// <summary>
    /// Get tmsTrafficStatisticition pagination list
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("Get  pagination list")]
    public async Task<SqlSugarPagedList<PagetmsTrafficStatisticOutput>> Page(PagetmsTrafficStatisticInput input)
    {
        return await MessBus.InvokeAsync<SqlSugarPagedList<PagetmsTrafficStatisticOutput>>(input);
    }

    /// <summary>
    /// Get tmsTrafficStatisticition list 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("Get  list")]
    public async Task<List<TrafficStatisticOutput>> GetList([FromQuery] TrafficStatisticInput input)
    {
        return await MessBus.InvokeAsync<List<TrafficStatisticOutput>>(input);
    }

    /// <summary>
    /// Get tmsTrafficStatisticition list 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("Get by Equipment")]
    [ApiDescriptionSettings(Name = "GetByEquipment"), HttpPost]
    public async Task<TrafficStatisticOutput> GetByEquipment([FromQuery] TrafficStatisticByEquipmentInput input)
    {
        return await MessBus.InvokeAsync<TrafficStatisticOutput>(input);
    }

    /// <summary>
    /// Add tmsTrafficStatisticition
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("Add ")]
    public async Task AddtmsTrafficStatistic(AddTmsTrafficStatisticInput input)
    {

        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Update tmsTrafficStatisticition 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("Update ")]
    public async Task UpdatetmsTrafficStatistic(UpdateTmsTrafficStatisticInput input)
    {

        await MessBus.InvokeAsync(input);

    }

    /// <summary>
    /// Delete tmsTrafficStatisticition 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("Delete ")]
    public async Task DeletetmsTrafficStatistic(DeleteTmsTrafficStatisticInput input)
    {
        await MessBus.InvokeAsync(input);

    }

    /// <summary>
    /// Batch delete tmsTrafficStatisticitions 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "BatchDelete"), HttpPost]
    [DisplayName("Batch delete ")]
    public async Task BatchDeletetmsTrafficStatistic(List<DeleteTmsTrafficStatisticInput> input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Author: ThangNN
    /// Created Date: 24/02/2025
    /// </summary>
    [HttpPost("ListByDate")]
    [DisplayName("List based on date")]
    public async Task<List<TrafficStatisticOutput>> ListByDate(TrafficStatisticByDateInput input)
    {
        return await MessBus.InvokeAsync<List<TrafficStatisticOutput>>(input);
    }

}
