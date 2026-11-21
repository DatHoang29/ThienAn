using Modules.TMS.Controllers.MaintenanceSchedule.Dto;
using Modules.TMS.Core.Constants;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Exceptions;
using Modules.TMS.Infrastructure;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;

namespace Modules.TMS.Controllers.MaintenanceSchedule.Queries;

/// <summary>
/// Author: System
/// Description: Handler xử lý các truy vấn của MaintenanceAlert
/// Created date: 11/01/2025
/// </summary>
public class MaintenanceAlertQueryHandler : IWolverineHandler
{
    private BaseRepository<TmsMaintenanceAlert> _alertRep;
    private BaseRepository<TmsEquipment> _equipmentRep;
    private BaseRepository<TmsMaintenanceAlertRule> _ruleRep;
    private BaseRepository<TmsMaintenanceGuide> _guideRep;
    private UserManager _user;
    private BaseCacheService _cache;

    public MaintenanceAlertQueryHandler(
        BaseRepository<TmsMaintenanceAlert> alertRep,
        BaseRepository<TmsEquipment> equipmentRep,
        BaseRepository<TmsMaintenanceAlertRule> ruleRep,
        BaseRepository<TmsMaintenanceGuide> guideRep,
        UserManager user,
        BaseCacheService cache)
    {
        _alertRep = alertRep;
        _equipmentRep = equipmentRep;
        _ruleRep = ruleRep;
        _guideRep = guideRep;
        _user = user;
        _cache = cache;
    }

    /// <summary>
    /// Lấy danh sách cảnh báo với phân trang và filter
    /// </summary>
    public async Task<SqlSugarPagedList<MaintenanceAlertListOutput>> HandleAsync(GetMaintenanceAlertListInput command)
    {
        var pagedList = await _alertRep.AsQueryable()
            .WhereIF(!string.IsNullOrEmpty(command.Status), u => u.Status == command.Status)
            .WhereIF(!string.IsNullOrEmpty(command.AlertType), u => u.AlertType == command.AlertType)
            .WhereIF(!string.IsNullOrEmpty(command.Priority), u => u.Priority == command.Priority)
            .WhereIF(!string.IsNullOrEmpty(command.EquipmentId), u => u.EquipmentId == command.EquipmentId)
            .WhereIF(command.StartDate.HasValue, u => u.CreateTime >= command.StartDate)
            .WhereIF(command.EndDate.HasValue, u => u.CreateTime <= command.EndDate)
            .OrderByDescending(u => u.CreateTime)
            .Select(u => new MaintenanceAlertListOutput() { }, true)
            .ToPagedListAsync(command.Page, command.PageSize);

        return pagedList;
    }

    /// <summary>
    /// Lấy chi tiết cảnh báo
    /// </summary>
    public async Task<MaintenanceAlertDetailOutput> HandleAsync(GetMaintenanceAlertByIdInput command)
    {
        var alert = await _alertRep.GetByIdAsync(command.ID)
                    ?? throw Oops.Oh(BaseMsg.TmsMaintenanceAlert.Exception.NotFound);

        var output = alert.Adapt<MaintenanceAlertDetailOutput>();

        // Get equipment info
        var equipment = await _equipmentRep.GetByIdAsync(alert.EquipmentId);
        if (equipment != null)
        {
            output.Equipment = equipment.Adapt<EquipmentBasicOutput>();
        }

        // Get rule info if exists
        if (!string.IsNullOrEmpty(alert.AlertRuleId))
        {
            var rule = await _ruleRep.GetByIdAsync(alert.AlertRuleId);
            if (rule != null)
            {
                output.AlertRule = rule.Adapt<AlertRuleBasicOutput>();
            }
        }

        // Get guide info if exists
        if (!string.IsNullOrEmpty(alert.SuggestedGuideId))
        {
            var guide = await _guideRep.GetByIdAsync(alert.SuggestedGuideId);
            if (guide != null)
            {
                output.SuggestedGuide = guide.Adapt<MaintenanceGuideBasicOutput>();
            }
        }

        return output;
    }

    /// <summary>
    /// Lấy số lượng cảnh báo theo trạng thái
    /// </summary>
    public async Task<AlertCountByStatusOutput> HandleAsync(GetAlertCountByStatusInput command)
    {
        var output = new AlertCountByStatusOutput();

        output.PendingCount = await _alertRep.AsQueryable()
            .Where(u => u.Status == MaintenanceConst.AlertStatus.Pending)
            .CountAsync();

        output.AcknowledgedCount = await _alertRep.AsQueryable()
            .Where(u => u.Status == MaintenanceConst.AlertStatus.Acknowledged)
            .CountAsync();

        output.ScheduledCount = await _alertRep.AsQueryable()
            .Where(u => u.Status == MaintenanceConst.AlertStatus.Scheduled)
            .CountAsync();

        output.DismissedCount = await _alertRep.AsQueryable()
            .Where(u => u.Status == MaintenanceConst.AlertStatus.Dismissed)
            .CountAsync();

        output.TotalCount = output.PendingCount + output.AcknowledgedCount + output.ScheduledCount + output.DismissedCount;

        return output;
    }
}
