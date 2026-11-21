using Modules.TMS.Core.Dto.IncidentAutomationConfig;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.TMS.Controllers.IncidentAutomationConfig;

/// <summary>
/// Author: SonTH
/// Description: TMS Incident Automation Config Controller
///              Quản lý cấu hình tự động xử lý sự kiện/sự cố theo loại sự kiện.
/// Created date: 24/07/2026
/// </summary>
[ApiDescriptionSettings(GroupName, Order = 456)]
public class TmsIncidentAutomationConfigController : BaseController
{
    public TmsIncidentAutomationConfigController()
    {
    }

    /// <summary>
    /// Get Tms Incident Automation Config pagination list
    /// </summary>
    [DisplayName("Get  pagination list")]
    public async Task<SqlSugarPagedList<PageIncidentAutomationConfigOutput>> Page(PageIncidentAutomationConfigInput input)
    {
        return await MessBus.InvokeAsync<SqlSugarPagedList<PageIncidentAutomationConfigOutput>>(input);
    }

    /// <summary>
    /// Get Tms Incident Automation Config list
    /// </summary>
    [DisplayName("Get  list")]
    public async Task<List<IncidentAutomationConfigOutput>> GetList([FromQuery] IncidentAutomationConfigInput input)
    {
        return await MessBus.InvokeAsync<List<IncidentAutomationConfigOutput>>(input);
    }

    /// <summary>
    /// Author: SonTH
    /// Description: Lấy cấu hình tự động theo một loại sự kiện cụ thể (dùng cho màn hình quản lý loại sự kiện)
    /// </summary>
    [HttpPost("GetByEventType")]
    [DisplayName("Get automation config by event type")]
    public async Task<IncidentAutomationConfigOutput> GetByEventType(GetIncidentAutomationConfigByEventTypeInput input)
    {
        return await MessBus.InvokeAsync<IncidentAutomationConfigOutput>(input);
    }

    /// <summary>
    /// Add Tms Incident Automation Config
    /// </summary>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("Add ")]
    public async Task AddTmsIncidentAutomationConfig(AddIncidentAutomationConfigInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Update Tms Incident Automation Config
    /// </summary>
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("Update ")]
    public async Task UpdateTmsIncidentAutomationConfig(UpdateIncidentAutomationConfigInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Delete Tms Incident Automation Config
    /// </summary>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("Delete ")]
    public async Task DeleteTmsIncidentAutomationConfig(DeleteIncidentAutomationConfigInput input)
    {
        await MessBus.InvokeAsync(input);
    }
}
