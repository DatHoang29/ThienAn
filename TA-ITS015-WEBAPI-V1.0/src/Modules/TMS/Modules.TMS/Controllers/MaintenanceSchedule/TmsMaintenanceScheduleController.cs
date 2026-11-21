using Modules.TMS.Controllers.MaintenanceSchedule.Dto;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.TMS.Controllers.MaintenanceSchedule;

/// <summary>
/// Author: System
/// Description: Tms Maintenance Schedule Controller - Quản lý lịch bảo trì duy tu
/// Created date: 10/12/2025
/// </summary>
[ApiDescriptionSettings(GroupName, Order = 471)]
public class TmsMaintenanceScheduleController : BaseController
{
    #region Query APIs

    /// <summary>
    /// Lấy danh sách lịch bảo trì
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "GetList"), HttpPost]
    [DisplayName("Get maintenance schedule list")]
    public async Task<List<MaintenanceScheduleOutput>> GetList(MaintenanceScheduleInput input)
    {
        return await MessBus.InvokeAsync<List<MaintenanceScheduleOutput>>(input);
    }

    /// <summary>
    /// Lấy danh sách lịch bảo trì có phân trang
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "GetPage"), HttpPost]
    [DisplayName("Get maintenance schedule list with pagination")]
    public async Task<SqlSugarPagedList<MaintenanceScheduleOutput>> GetPage(PageMaintenanceScheduleInput input)
    {
        return await MessBus.InvokeAsync<SqlSugarPagedList<MaintenanceScheduleOutput>>(input);
    }

    /// <summary>
    /// Lấy lịch bảo trì theo tuần
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpGet("GetByWeek")]
    [DisplayName("Get schedules by week")]
    public async Task<List<MaintenanceScheduleOutput>> GetByWeek([FromQuery] GetByWeekInput input)
    {
        return await MessBus.InvokeAsync<List<MaintenanceScheduleOutput>>(input);
    }

    /// <summary>
    /// Lấy lịch bảo trì theo tháng (kèm summary)
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpGet("GetByMonth")]
    [DisplayName("Get schedules by month")]
    public async Task<GetByMonthResponse> GetByMonth([FromQuery] GetByMonthInput input)
    {
        return await MessBus.InvokeAsync<GetByMonthResponse>(input);
    }

    /// <summary>
    /// Lấy lịch bảo trì theo năm (summary only)
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpGet("GetByYear")]
    [DisplayName("Get schedules by year")]
    public async Task<ScheduleCalendarSummary> GetByYear([FromQuery] GetByYearInput input)
    {
        return await MessBus.InvokeAsync<ScheduleCalendarSummary>(input);
    }

    /// <summary>
    /// Lấy danh sách lịch bảo trì sắp tới
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("GetUpcoming")]
    [DisplayName("Get upcoming schedules")]
    public async Task<List<MaintenanceScheduleOutput>> GetUpcoming(GetUpcomingMaintenanceSchedulesInput input)
    {
        return await MessBus.InvokeAsync<List<MaintenanceScheduleOutput>>(input);
    }

    /// <summary>
    /// Lấy chi tiết lịch bảo trì
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("GetById/{id}")]
    [DisplayName("Get maintenance schedule by id")]
    public async Task<MaintenanceScheduleOutput?> GetMaintenanceById(string id)
    {
        return await MessBus.InvokeAsync<MaintenanceScheduleOutput?>(new GetMaintenanceScheduleByIdInput { ID = id });
    }

    #endregion

    #region Command APIs


    /// <summary>
    /// Thêm lịch bảo trì mới
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("Add maintenance schedule")]
    public async Task AddMaintenance(AddMaintenanceScheduleInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Cập nhật lịch bảo trì
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("Update maintenance schedule")]
    public async Task UpdateMaintenance(UpdateMaintenanceScheduleInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Xóa lịch bảo trì
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("Delete maintenance schedule")]
    public async Task DeleteMaintenance(DeleteMaintenanceScheduleInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Xóa nhiều lịch bảo trì
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "BatchDelete"), HttpPost]
    [DisplayName("Batch delete maintenance schedules")]
    public async Task BatchDeleteMaintenance(List<DeleteMaintenanceScheduleInput> input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Cập nhật trạng thái lịch bảo trì
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("UpdateStatus")]
    [DisplayName("Update schedule status")]
    public async Task UpdateMaintenanceStatus(UpdateMaintenanceScheduleStatusInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    #endregion

    #region Equipment APIs

    /// <summary>
    /// Thêm thiết bị vào lịch bảo trì
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("AddEquipment")]
    [DisplayName("Add equipment to schedule")]
    public async Task AddMaintenanceEquipment(AddEquipmentToScheduleInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Xóa thiết bị khỏi lịch bảo trì
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("RemoveEquipment")]
    [DisplayName("Remove equipment from schedule")]
    public async Task RemoveMaintenanceEquipment(RemoveEquipmentFromScheduleInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Cập nhật trạng thái thiết bị trong lịch
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("UpdateEquipmentStatus")]
    [DisplayName("Update equipment status")]
    public async Task UpdateMaintenanceEquipmentStatus(UpdateEquipmentStatusInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Xác nhận đã kiểm tra thiết bị trong lịch + ghi chú
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("ConfirmEquipmentChecked")]
    [DisplayName("Confirm equipment checked")]
    public async Task ConfirmEquipmentChecked(ConfirmEquipmentCheckedInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    #endregion

    #region Task APIs

    /// <summary>
    /// Thêm công việc/phân công vào lịch
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("AddTask")]
    [DisplayName("Add task to schedule")]
    public async Task AddMaintenanceTask(AddTaskToScheduleInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Xóa công việc/phân công khỏi lịch
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("RemoveTask")]
    [DisplayName("Remove task from schedule")]
    public async Task RemoveMaintenanceTask(RemoveTaskFromScheduleInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Cập nhật trạng thái công việc
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("UpdateTaskStatus")]
    [DisplayName("Update task status")]
    public async Task UpdateMaintenanceTaskStatus(UpdateTaskStatusInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    #endregion

    #region History APIs

    /// <summary>
    /// Lấy lịch sử thay đổi lịch bảo trì
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("GetHistory")]
    [DisplayName("Get maintenance schedule history")]
    public async Task<List<MaintenanceHistoryOutput>> GetMaintenanceHistory(MaintenanceHistoryQueryInput input)
    {
        return await MessBus.InvokeAsync<List<MaintenanceHistoryOutput>>(input);
    }

    /// <summary>
    /// Lấy lịch sử có phân trang
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("PageHistory")]
    [DisplayName("Get maintenance schedule history with pagination")]
    public async Task<SqlSugarPagedList<MaintenanceHistoryOutput>> PageMaintenanceHistory(PageMaintenanceHistoryInput input)
    {
        return await MessBus.InvokeAsync<SqlSugarPagedList<MaintenanceHistoryOutput>>(input);
    }

    #endregion
}
