using Modules.TMS.Controllers.EventType.Dto;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.TMS.Controllers.EventType;

/// <summary>
/// TMS Event Type Controller
/// </summary>
[ApiDescriptionSettings(GroupName, Order = 460)]
public class TmsEventTypeController : BaseController
{

    public TmsEventTypeController()
    {

    }

    /// <summary>
    /// Get Tms Event Type pagination list
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("Get  pagination list")]
    public async Task<SqlSugarPagedList<PageEventTypeOutput>> Page(PageEventTypeInput input)
    {
        return await MessBus.InvokeAsync<SqlSugarPagedList<PageEventTypeOutput>>(input);
    }

    /// <summary>
    /// Get Tms Event Type list 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("Get  list")]
    public async Task<List<EventTypeOutput>> GetList([FromQuery] EventTypeInput input)
    {
        return await MessBus.InvokeAsync<List<EventTypeOutput>>(input);
    }

    /// <summary>
    /// Add Tms Event Type
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("Add ")]
    public async Task AddTmsEventType(AddEventTypeInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Update Tms Event Type 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("Update ")]
    public async Task UpdateTmsEventType(UpdateEventTypeInput input)
    {
        await MessBus.InvokeAsync(input);

    }

    /// <summary>
    /// Delete Tms Event Type 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("Delete ")]
    public async Task DeleteTmsEventType(DeleteEventTypeInput input)
    {
        await MessBus.InvokeAsync(input);

    }

    /// <summary>
    /// Batch delete Tms Event Types 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "BatchDelete"), HttpPost]
    [DisplayName("Batch delete ")]
    public async Task BatchDeleteTmsEventType(List<DeleteEventTypeInput> input)
    {
        await MessBus.InvokeAsync(input);
    }



}
