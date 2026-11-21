using Modules.TMS.Controllers.MaintenanceSchedule.Dto;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.TMS.Controllers.MaintenanceSchedule;

/// <summary>
/// Author: System
/// Description: Tms Maintenance Alert Controller - Quản lý cảnh báo bảo trì
/// Created date: 10/12/2025
/// </summary>
[ApiDescriptionSettings(GroupName, Order = 472)]
public class TmsMaintenanceAlertController : BaseController
{
    #region Alert APIs

    /// <summary>
    /// Lấy danh sách cảnh báo có phân trang
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "GetList"), HttpPost]
    [DisplayName("Get alert list with pagination")]
    public async Task<SqlSugarPagedList<MaintenanceAlertListOutput>> GetList(GetMaintenanceAlertListInput input)
    {
        return await MessBus.InvokeAsync<SqlSugarPagedList<MaintenanceAlertListOutput>>(input);
    }

    /// <summary>
    /// Lấy chi tiết cảnh báo
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("GetById/{id}")]
    [DisplayName("Get alert by id")]
    public async Task<MaintenanceAlertDetailOutput> GetById(string id)
    {
        return await MessBus.InvokeAsync<MaintenanceAlertDetailOutput>(new GetMaintenanceAlertByIdInput { ID = id });
    }

    /// <summary>
    /// Đếm số cảnh báo theo trạng thái
    /// </summary>
    /// <returns></returns>
    [HttpGet("GetCountByStatus")]
    [DisplayName("Get alert count by status")]
    public async Task<AlertCountByStatusOutput> GetCountByStatus()
    {
        return await MessBus.InvokeAsync<AlertCountByStatusOutput>(new GetAlertCountByStatusInput());
    }

    /// <summary>
    /// Xác nhận cảnh báo
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("Acknowledge")]
    [DisplayName("Acknowledge alert")]
    public async Task Acknowledge(AcknowledgeAlertInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Bỏ qua cảnh báo
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("Dismiss")]
    [DisplayName("Dismiss alert")]
    public async Task Dismiss(DismissAlertInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Tạo lịch bảo trì từ cảnh báo
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("CreateScheduleFromAlert")]
    [DisplayName("Create schedule from alert")]
    public async Task<long> CreateScheduleFromAlert(CreateScheduleFromAlertInput input)
    {
        return await MessBus.InvokeAsync<long>(input);
    }

    /// <summary>
    /// Tạo cảnh báo thủ công
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("CreateManualAlert")]
    [DisplayName("Create manual alert")]
    public async Task<long> CreateManualAlert(CreateManualAlertInput input)
    {
        return await MessBus.InvokeAsync<long>(input);
    }

    #endregion
}
