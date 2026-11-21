using Microsoft.AspNetCore.Hosting;
using Modules.TMS.Controllers.SignalLogs.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Infrastructure.Services.SignalLog;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;

namespace Modules.TMS.Controllers.SignalLogs;

/// <summary>
/// System TmsSignalLog 
/// </summary>
[ApiDescriptionSettings(GroupName, Order = 460)]
public class TmsSignalLogController : BaseController
{
    private readonly IWebHostEnvironment _env;
    private SignalLogService _tmsSignalLogService;
    private UserManager _userManager;
    public TmsSignalLogController(IWebHostEnvironment env, SignalLogService tmsSignalLogService, UserManager userManager)
    {
        _env = env;
        _tmsSignalLogService = tmsSignalLogService;
        _userManager = userManager;
    }

    /// <summary>
    /// Get TmsSignalLog pagination list
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("Get  pagination list")]
    public async Task<SqlSugarPagedList<PageTmsSignalLogsOutput>> Page(PageTmsSignalLogsInput input)
    {
        return await MessBus.InvokeAsync<SqlSugarPagedList<PageTmsSignalLogsOutput>>(input);
    }

    /// <summary>
    /// Get TmsSignalLog list 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("Get  list")]
    public async Task<List<SignalLogsOutput>> GetList([FromQuery] SignalLogsInput input)
    {
        return await MessBus.InvokeAsync<List<SignalLogsOutput>>(input);
    }

    /// <summary>
    /// Add TmsModuleition
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    [DisplayName("Add ")]
    public async Task AddTmsSignalLog(AddTmsSignalLogInput input)
    {
        // Gửi yêu cầu qua Message Bus
        await MessBus.InvokeAsync(input);
    }


    /// <summary>
    /// Update TmsSignalLog 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    [DisplayName("Update ")]
    public async Task UpdateTmsSignalLog(UpdateTmsSignalLogInput input)
    {


        // Gửi yêu cầu qua Message Bus
        await MessBus.InvokeAsync(input);
    }

    [HttpPost("stopProcess")]
    [DisplayName("Retrieve a list of SysConfigData based on field code")]
    public async Task logLastImageAsync(SignalLog request)
    {
        try
        {
            var lastLog = await _tmsSignalLogService.GetLatestSignalLogByEquipmentIDAsync(request.vmsId);
            switch (request.type)
            {
                case "0":
                    var tmsSignal = new AddTmsSignalLogInput
                    {
                        NewData = request.data,
                        OldData = lastLog?.NewData,
                        EquipmentId = request.vmsId,
                        NewUrl = request.imageUrl,
                        OldUrl = lastLog?.NewUrl,
                        Type = "Dừng Lập Lịch",
                        ExecuteBy = "Schedule",
                        ExecuteTime = DateTime.Now,
                        CreateUId = "Schedule",
                        State = "success",
                    };
                    await MessBus.InvokeAsync(tmsSignal);

                    break;
                case "3":
                    tmsSignal = new AddTmsSignalLogInput
                    {
                        NewData = request.data,
                        OldData = lastLog?.NewData, // Using null-conditional operator
                        EquipmentId = request.vmsId,
                        OldUrl = request.OldUrl,
                        NewUrl = request.imageUrl,
                        TimeToLive = request.timetolive,
                        Type = "Lập Lịch",
                        ExecuteBy = "Schedule",
                        ExecuteTime = DateTime.Now,
                        CreateUId = "Schedule",
                        State = "success",

                    };
                    await MessBus.InvokeAsync(tmsSignal);

                    break;
                case "Reset":
                    tmsSignal = new AddTmsSignalLogInput
                    {
                        NewData = request.data,
                        OldData = lastLog?.NewData, // Using null-conditional operator
                        EquipmentId = request.vmsId,
                        NewUrl = request.imageUrl,
                        OldUrl = lastLog?.NewUrl,
                        Type = "Reset Incident",
                        ExecuteBy = _userManager?.Account ?? "System",
                        State = "success",
                        ExecuteTime = DateTime.Now,
                        CreateUId = _userManager?.Account ?? "System",
                    };
                    await MessBus.InvokeAsync(tmsSignal);

                    break;
                default:
                    tmsSignal = new AddTmsSignalLogInput
                    {
                        NewData = request.data,
                        OldData = lastLog?.NewData, // Using null-conditional operator
                        EquipmentId = request.vmsId,
                        NewUrl = request.imageUrl,
                        OldUrl = lastLog?.NewUrl,
                        Type = "manual",
                        ExecuteBy = _userManager?.Account ?? "System",
                        ExecuteTime = DateTime.Now,
                        CreateUId = _userManager?.Account ?? "System",
                        State = "success",
                        IncidentId = request.incidentId,
                    };
                    await MessBus.InvokeAsync(tmsSignal);

                    break;
            }

            Console.WriteLine($"Logged last image for VMS {request.vmsId} with ImageUrl: {request.imageUrl}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error logging last image: {ex.Message}");
        }
    }
    /// <summary>
    /// Delete TmsSignalLog 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    [DisplayName("Delete ")]
    public async Task DeleteTmsSignalLog(DeleteTmsSignalLogInput input)
    {
        await MessBus.InvokeAsync(input);

    }

    /// <summary>
    /// Batch delete TmsModuleitions 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [ApiDescriptionSettings(Name = "BatchDelete"), HttpPost]
    [DisplayName("Batch delete ")]
    public async Task BatchDeleteTmsSignalLog(List<DeleteTmsSignalLogInput> input)
    {
        await MessBus.InvokeAsync(input);
    }
    [HttpGet("Incident")]
    [DisplayName("Get  list")]
    public async Task<List<TmsSignalLog>> GetIncident(string incident)
    {
        return await _tmsSignalLogService.GetIncident(incident);
    }

}
