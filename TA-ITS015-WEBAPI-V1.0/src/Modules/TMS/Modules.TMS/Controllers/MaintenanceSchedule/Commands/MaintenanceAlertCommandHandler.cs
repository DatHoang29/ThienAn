using Microsoft.Extensions.Localization;
using Modules.TMS.Controllers.MaintenanceSchedule.Dto;
using Modules.TMS.Core.Constants;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Exceptions;
using Modules.TMS.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Services;

namespace Modules.TMS.Controllers.MaintenanceSchedule.Commands;

/// <summary>
/// Author: System
/// Description: Handler xử lý các lệnh của MaintenanceAlert
/// Created date: 11/01/2025
/// </summary>
public class MaintenanceAlertCommandHandler : IWolverineHandler
{
    private BaseRepository<TmsMaintenanceAlert> _alertRep;
    private BaseRepository<TmsMaintenanceSchedule> _scheduleRep;
    private BaseRepository<TmsMaintenanceScheduleEquipment> _scheduleEquipmentRep;
    private BaseRepository<TmsEquipment> _equipmentRep;
    private BaseRepository<TmsMaintenanceGuide> _guideRep;
    private UserManager _user;
    private BaseCacheService _cache;
    private readonly IStringLocalizer _localizer;

    private static readonly HashSet<string> AlertStatusPendingVariants = new(StringComparer.OrdinalIgnoreCase)
    {
        MaintenanceConst.AlertStatus.Pending,
        MaintenanceConst.AlertStatus.LegacyPending
    };

    private static readonly HashSet<string> AlertStatusAcknowledgedVariants = new(StringComparer.OrdinalIgnoreCase)
    {
        MaintenanceConst.AlertStatus.Acknowledged,
        MaintenanceConst.AlertStatus.LegacyAcknowledged
    };

    private static readonly HashSet<string> AlertStatusScheduledVariants = new(StringComparer.OrdinalIgnoreCase)
    {
        MaintenanceConst.AlertStatus.Scheduled,
        MaintenanceConst.AlertStatus.LegacyScheduled
    };

    private static readonly HashSet<string> AlertStatusDismissedVariants = new(StringComparer.OrdinalIgnoreCase)
    {
        MaintenanceConst.AlertStatus.Dismissed,
        MaintenanceConst.AlertStatus.LegacyDismissed
    };

    private static readonly HashSet<string> PriorityHighVariants = new(StringComparer.OrdinalIgnoreCase)
    {
        MaintenanceConst.Priority.High,
        MaintenanceConst.Priority.LegacyHigh
    };

    private static readonly HashSet<string> PriorityMediumVariants = new(StringComparer.OrdinalIgnoreCase)
    {
        MaintenanceConst.Priority.Medium,
        MaintenanceConst.Priority.LegacyMedium
    };

    private static readonly HashSet<string> PriorityLowVariants = new(StringComparer.OrdinalIgnoreCase)
    {
        MaintenanceConst.Priority.Low,
        MaintenanceConst.Priority.LegacyLow
    };

    public MaintenanceAlertCommandHandler(
        BaseRepository<TmsMaintenanceAlert> alertRep,
        BaseRepository<TmsMaintenanceSchedule> scheduleRep,
        BaseRepository<TmsMaintenanceScheduleEquipment> scheduleEquipmentRep,
        BaseRepository<TmsEquipment> equipmentRep,
        BaseRepository<TmsMaintenanceGuide> guideRep,
        UserManager user,
        BaseCacheService cache,
        IStringLocalizer localizer)
    {
        _alertRep = alertRep;
        _scheduleRep = scheduleRep;
        _scheduleEquipmentRep = scheduleEquipmentRep;
        _equipmentRep = equipmentRep;
        _guideRep = guideRep;
        _user = user;
        _cache = cache;
        _localizer = localizer;
    }

    /// <summary>
    /// Acknowledge alert - Đánh dấu đã xem cảnh báo
    /// </summary>
    public async Task HandleAsync(AcknowledgeAlertInput command)
    {
        var alert = await _alertRep.GetByIdAsync(command.ID)
                    ?? throw Oops.Oh(BaseMsg.TmsMaintenanceAlert.Exception.NotFound);

        if (string.IsNullOrEmpty(alert.Status) || !AlertStatusPendingVariants.Contains(alert.Status))
            throw Oops.Oh(BaseMsg.TmsMaintenanceAlert.Exception.AlreadyProcessed);

        alert.Status = MaintenanceConst.AlertStatus.Acknowledged;
        alert.AcknowledgedTime = DateTime.Now;
        alert.AcknowledgedBy = _user.UserId.ToString();

        await _alertRep.UpdateAsync(alert);
        _cache.RemoveByPrefixKey(CacheConst.Tms.TmsMaintenanceAlert);
    }

    /// <summary>
    /// Dismiss alert - Bỏ qua cảnh báo
    /// </summary>
    public async Task HandleAsync(DismissAlertInput command)
    {
        var alert = await _alertRep.GetByIdAsync(command.ID)
                    ?? throw Oops.Oh(BaseMsg.TmsMaintenanceAlert.Exception.NotFound);

        if (!string.IsNullOrEmpty(alert.Status) &&
            (AlertStatusScheduledVariants.Contains(alert.Status) || AlertStatusDismissedVariants.Contains(alert.Status)))
            throw Oops.Oh(BaseMsg.TmsMaintenanceAlert.Exception.AlreadyProcessed);

        alert.Status = MaintenanceConst.AlertStatus.Dismissed;
        alert.DismissedTime = DateTime.Now;
        alert.DismissedBy = _user.UserId.ToString();
        alert.DismissReason = command.Reason;

        await _alertRep.UpdateAsync(alert);
        _cache.RemoveByPrefixKey(CacheConst.Tms.TmsMaintenanceAlert);
    }

    /// <summary>
    /// Tạo lịch bảo trì từ cảnh báo
    /// </summary>
    public async Task<string> HandleAsync(CreateScheduleFromAlertInput command)
    {
        var alert = await _alertRep.GetByIdAsync(command.AlertId)
                    ?? throw Oops.Oh(BaseMsg.TmsMaintenanceAlert.Exception.NotFound);

        if (!string.IsNullOrEmpty(alert.Status) && AlertStatusScheduledVariants.Contains(alert.Status))
            throw Oops.Oh(BaseMsg.TmsMaintenanceAlert.Exception.AlreadyProcessed);

        // Validate equipment exists
        var equipment = await _equipmentRep.GetByIdAsync(alert.EquipmentId)
                        ?? throw Oops.Oh(BaseMsg.TmsMaintenanceAlert.Exception.EquipmentNotFound);

        // Find matching maintenance guide
        TmsMaintenanceGuide? guide = null;
        if (!string.IsNullOrEmpty(alert.SuggestedGuideId))
        {
            guide = await _guideRep.GetFirstAsync(g => g.ID == alert.SuggestedGuideId);
        }

        // If no suggested guide, try to find one based on equipment type and manufacturer
        if (guide == null)
        {
            // First try to find manufacturer-specific guide
            guide = await _guideRep.GetFirstAsync(g =>
                g.EquipmentTypeId == equipment.EquipmentTypeId &&
                g.Manufacturer == equipment.Manufacturer);

            // If not found, try generic guide for equipment type
            if (guide == null)
            {
                guide = await _guideRep.GetFirstAsync(g =>
                    g.EquipmentTypeId == equipment.EquipmentTypeId &&
                    g.Manufacturer == null);
            }
        }

        // Create maintenance schedule
        var schedule = new TmsMaintenanceSchedule
        {
            Code = $"MS-{DateTime.Now:yyyyMMddHHmmss}-{new Random().Next(1000, 9999)}",
            Name = command.ScheduleName ?? _localizer[BaseMsg.TmsMaintenanceAlert.Message.EmergencyScheduleName, equipment.Name],
            Remark = command.Description ?? _localizer[BaseMsg.TmsMaintenanceAlert.Message.ScheduleFromAlertDescription, alert.ID, alert.ErrorCount],
            ScheduledDate = command.ScheduledDate,
            DueDate = command.DueDate ?? command.ScheduledDate.AddDays(7),
            Priority = !string.IsNullOrEmpty(alert.Priority)
                ? (PriorityHighVariants.Contains(alert.Priority)
                    ? MaintenanceConst.Priority.High
                    : PriorityMediumVariants.Contains(alert.Priority)
                        ? MaintenanceConst.Priority.Medium
                        : PriorityLowVariants.Contains(alert.Priority)
                            ? MaintenanceConst.Priority.Low
                            : MaintenanceConst.Priority.High)
                : MaintenanceConst.Priority.High,
            Status = MaintenanceConst.ScheduleStatus.Register
        };

        await _scheduleRep.InsertAsync(schedule);

        // Add equipment to schedule
        var scheduleEquipment = new TmsMaintenanceScheduleEquipment
        {
            MaintenanceScheduleId = schedule.ID,
            EquipmentId = alert.EquipmentId,
            MaintenanceGuideId = guide?.ID,
            Status = MaintenanceConst.EquipmentStatus.Pending,
            Remark = _localizer[BaseMsg.TmsMaintenanceAlert.Message.EquipmentFromAlertRemark, alert.ErrorPeriodDays, alert.ErrorCount]
        };

        await _scheduleEquipmentRep.InsertAsync(scheduleEquipment);

        // Update alert status
        alert.Status = MaintenanceConst.AlertStatus.Scheduled;
        alert.ScheduleId = schedule.ID.ToString();
        await _alertRep.UpdateAsync(alert);

        _cache.RemoveByPrefixKey(CacheConst.Tms.TmsMaintenanceAlert);
        _cache.RemoveByPrefixKey(CacheConst.Tms.TmsMaintenanceSchedule);

        return schedule.ID;
    }

    /// <summary>
    /// Tạo cảnh báo thủ công
    /// </summary>
    public async Task<string> HandleAsync(CreateManualAlertInput command)
    {
        // Validate equipment exists
        var equipment = await _equipmentRep.GetByIdAsync(command.EquipmentId)
                        ?? throw Oops.Oh(BaseMsg.TmsMaintenanceAlert.Exception.EquipmentNotFound);

        var alert = new TmsMaintenanceAlert
        {
            EquipmentId = command.EquipmentId,
            AlertType = MaintenanceConst.AlertType.ManualRequest,
            Status = MaintenanceConst.AlertStatus.Pending,
            AlertMessage = command.AlertMessage,
            Title = command.AlertMessage ?? _localizer[BaseMsg.TmsMaintenanceAlert.Message.ManualAlertTitle],
            Priority = !string.IsNullOrEmpty(command.Priority)
                ? (PriorityHighVariants.Contains(command.Priority)
                    ? MaintenanceConst.Priority.High
                    : PriorityMediumVariants.Contains(command.Priority)
                        ? MaintenanceConst.Priority.Medium
                        : PriorityLowVariants.Contains(command.Priority)
                            ? MaintenanceConst.Priority.Low
                            : MaintenanceConst.Priority.Medium)
                : MaintenanceConst.Priority.Medium
        };

        // Find suggested guide if not provided
        if (!string.IsNullOrEmpty(command.SuggestedGuideId))
        {
            var guideExists = await _guideRep.IsAnyAsync(g => g.ID == command.SuggestedGuideId);
            if (guideExists)
                alert.SuggestedGuideId = command.SuggestedGuideId;
        }
        else
        {
            // Try to find guide for equipment
            var guide = await _guideRep.GetFirstAsync(g =>
                g.EquipmentTypeId == equipment.EquipmentTypeId &&
                (g.Manufacturer == equipment.Manufacturer || g.Manufacturer == null));
            if (guide != null)
                alert.SuggestedGuideId = guide.ID;
        }

        await _alertRep.InsertAsync(alert);
        _cache.RemoveByPrefixKey(CacheConst.Tms.TmsMaintenanceAlert);

        return alert.ID;
    }
}
