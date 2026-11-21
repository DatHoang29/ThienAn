using Modules.TMS.Controllers.DutySchedule.Dto;
using Modules.TMS.Controllers.DutySchedule.Queries;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.TMS.Controllers.DutySchedule;

/// <summary>
/// Author: System
/// Description: Tms DutySchedule Controller
/// Created date: 07/11/2025
/// </summary>
[ApiDescriptionSettings(GroupName, Order = 462)]
public class TmsDutyScheduleController : BaseController
{
    /// <summary>
    /// Get DutySchedule list (POST method để hỗ trợ filter phức tạp)
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "GetList"), HttpPost]
    [DisplayName("Get list")]
    public async Task<List<DutyScheduleOutput>> GetList(DutyScheduleInput input)
    {
        return await MessBus.InvokeAsync<List<DutyScheduleOutput>>(input);
    }

    /// <summary>
    /// Get DutySchedule by week
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpGet("GetByWeek")]
    [DisplayName("Get by week")]
    public async Task<List<DutyScheduleOutput>> GetByWeek([FromQuery] GetByWeekInput input)
    {
        return await MessBus.InvokeAsync<List<DutyScheduleOutput>>(input);
    }

    /// <summary>
    /// Get upcoming schedules (20 lịch gần nhất hoặc trong 7 ngày)
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("GetUpcoming")]
    [DisplayName("Get upcoming schedules")]
    public async Task<List<DutyScheduleOutput>> GetUpcoming(GetUpcomingSchedulesInput input)
    {
        return await MessBus.InvokeAsync<List<DutyScheduleOutput>>(input);
    }

    /// <summary>
    /// Add DutySchedule
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("Add duty schedule")]
    public async Task Add(AddDutyScheduleInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Update DutySchedule
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("Update duty schedule")]
    public async Task Update(UpdateDutyScheduleInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Delete DutySchedule
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("Delete")]
    public async Task Delete(DeleteDutyScheduleInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Batch delete DutySchedules
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "BatchDelete"), HttpPost]
    [DisplayName("Batch delete")]
    public async Task BatchDelete(List<DeleteDutyScheduleInput> input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Cập nhật trạng thái lịch trực (Draft, InProgress, Completed, Cancelled)
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("UpdateStatus")]
    [DisplayName("Update schedule status")]
    public async Task UpdateStatus(UpdateScheduleStatusInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Add user to duty schedule
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("AddUser")]
    [DisplayName("Add user to schedule")]
    public async Task AddUser(AddUserToScheduleInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Remove user from duty schedule
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("RemoveUser")]
    [DisplayName("Remove user from schedule")]
    public async Task RemoveUser(RemoveUserFromScheduleInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Update user role in duty schedule
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("UpdateUserRole")]
    [DisplayName("Update user role")]
    public async Task UpdateUserRole(UpdateUserRoleInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    #region Change Request APIs

    /// <summary>
    /// Tạo yêu cầu thay đổi lịch trực (swap/absent/replace)
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("CreateChangeRequest")]
    [DisplayName("Create change request")]
    public async Task CreateChangeRequest(CreateChangeRequestInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Lấy danh sách yêu cầu thay đổi lịch trực
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("GetChangeRequests")]
    [DisplayName("Get change requests")]
    public async Task<List<DutyScheduleChangeRequestOutput>> GetChangeRequests(ChangeRequestQueryInput input)
    {
        return await MessBus.InvokeAsync<List<DutyScheduleChangeRequestOutput>>(input);
    }

    /// <summary>
    /// Lấy danh sách yêu cầu thay đổi có phân trang
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("PageChangeRequests")]
    [DisplayName("Get change requests with pagination")]
    public async Task<SqlSugarPagedList<DutyScheduleChangeRequestOutput>> PageChangeRequests(PageChangeRequestInput input)
    {
        return await MessBus.InvokeAsync<SqlSugarPagedList<DutyScheduleChangeRequestOutput>>(input);
    }

    /// <summary>
    /// Gợi ý danh sách lịch trực phù hợp để chuyển đến (rule-based)
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("GetAvailableSchedules")]
    [DisplayName("Get available schedules for transfer")]
    public async Task<List<AvailableScheduleOutput>> GetAvailableSchedules(GetAvailableSchedulesInput input)
    {
        return await MessBus.InvokeAsync<List<AvailableScheduleOutput>>(input);
    }

    /// <summary>
    /// Lấy chi tiết một yêu cầu thay đổi
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("GetChangeRequest/{id}")]
    [DisplayName("Get change request by id")]
    public async Task<DutyScheduleChangeRequestOutput?> GetChangeRequest(string id)
    {
        return await MessBus.InvokeAsync<DutyScheduleChangeRequestOutput?>(new GetChangeRequestByIdInput { Id = id });
    }

    /// <summary>
    /// Phê duyệt/Từ chối yêu cầu thay đổi (dành cho người tạo lịch hoặc đội trưởng)
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("ApproveChangeRequest")]
    [DisplayName("Approve/Reject change request")]
    public async Task ApproveChangeRequest(ApproveChangeRequestInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>
    /// Hủy yêu cầu thay đổi (người yêu cầu)
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("CancelChangeRequest")]
    [DisplayName("Cancel change request")]
    public async Task CancelChangeRequest(CancelChangeRequestInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    #endregion

    #region History APIs

    /// <summary>
    /// Lấy danh sách lịch sử thay đổi lịch trực (không phân trang, giới hạn 100 records)
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("GetHistory")]
    [DisplayName("Get duty schedule history")]
    public async Task<List<DutyScheduleHistoryOutput>> GetHistory(DutyScheduleHistoryQueryInput input)
    {
        return await MessBus.InvokeAsync<List<DutyScheduleHistoryOutput>>(input);
    }

    /// <summary>
    /// Lấy danh sách lịch sử thay đổi có phân trang
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [HttpPost("PageHistory")]
    [DisplayName("Get duty schedule history with pagination")]
    public async Task<SqlSugarPagedList<DutyScheduleHistoryOutput>> PageHistory(PageDutyScheduleHistoryInput input)
    {
        return await MessBus.InvokeAsync<SqlSugarPagedList<DutyScheduleHistoryOutput>>(input);
    }

    #endregion
}
