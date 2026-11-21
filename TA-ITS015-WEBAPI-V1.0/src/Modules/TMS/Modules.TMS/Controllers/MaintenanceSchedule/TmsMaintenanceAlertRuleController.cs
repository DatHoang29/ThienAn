using Modules.TMS.Controllers.MaintenanceSchedule.Dto;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.TMS.Controllers.MaintenanceSchedule;

/// <summary>
/// Author: System
/// Description: Tms Maintenance Alert Rule Controller - Quản lý cấu hình rule cảnh báo
/// Created date: 10/12/2025
/// </summary>
[ApiDescriptionSettings(GroupName, Order = 473)]
public class TmsMaintenanceAlertRuleController : BaseController
{
    /// <summary>
    /// Lấy danh sách rule cảnh báo có phân trang
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "GetList"), HttpPost]
    [DisplayName("Get alert rule list")]
    public async Task<SqlSugarPagedList<MaintenanceAlertRuleListOutput>> GetList(GetMaintenanceAlertRuleListInput input)
    {
        return await MessBus.InvokeAsync<SqlSugarPagedList<MaintenanceAlertRuleListOutput>>(input);
    }

    /// <summary>
    /// Lấy chi tiết rule
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("GetById/{id}")]
    [DisplayName("Get alert rule by id")]
    public async Task<MaintenanceAlertRuleDetailOutput> GetById(string id)
    {
        return await MessBus.InvokeAsync<MaintenanceAlertRuleDetailOutput>(new GetMaintenanceAlertRuleByIdInput { ID = id });
    }

    /// <summary>
    /// Lấy tất cả rule đang active
    /// </summary>
    /// <returns></returns>
    [HttpGet("GetActiveRules")]
    [DisplayName("Get active alert rules")]
    public async Task<List<MaintenanceAlertRuleListOutput>> GetActiveRules()
    {
        return await MessBus.InvokeAsync<List<MaintenanceAlertRuleListOutput>>(new GetActiveAlertRulesInput());
    }

    /// <summary>
    /// Thêm rule cảnh báo mới
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("Add alert rule")]
    public async Task Add(AddMaintenanceAlertRuleInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Cập nhật rule cảnh báo
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("Update alert rule")]
    public async Task Update(UpdateMaintenanceAlertRuleInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Xóa rule cảnh báo
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("Delete alert rule")]
    public async Task Delete(DeleteMaintenanceAlertRuleInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Kích hoạt rule cảnh báo
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpPost("Activate/{id}")]
    [DisplayName("Activate alert rule")]
    public async Task Activate(string id)
    {
        await MessBus.InvokeAsync(new ActivateAlertRuleInput { ID = id });
    }

    /// <summary>
    /// Hủy kích hoạt rule cảnh báo
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpPost("Deactivate/{id}")]
    [DisplayName("Deactivate alert rule")]
    public async Task Deactivate(string id)
    {
        await MessBus.InvokeAsync(new DeactivateAlertRuleInput { ID = id });
    }
}
