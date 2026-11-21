using Modules.TMS.Controllers.MaintenanceSchedule.Dto;
using Modules.TMS.Core.Constants;
using Modules.TMS.Core.Entities;
using Modules.TMS.Infrastructure;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;

namespace Modules.TMS.Controllers.MaintenanceSchedule.Queries;

/// <summary>
/// Author: System
/// Description: Handler xử lý các truy vấn của MaintenanceSchedule
/// Created date: 10/12/2025
/// </summary>
public class MaintenanceScheduleQueryHandler : IWolverineHandler
{
    private BaseRepository<TmsMaintenanceSchedule> _scheduleRep;
    private BaseRepository<TmsMaintenanceScheduleEquipment> _scheduleEquipmentRep;
    private BaseRepository<TmsMaintenanceTask> _taskRep;
    private BaseRepository<TmsMaintenanceGuide> _guideRep;
    private BaseRepository<TmsMaintenanceHistory> _historyRep;
    private BaseRepository<TmsEquipment> _equipmentRep;
    private BaseRepository<TmsEquipmentType> _equipmentTypeRep;
    private UserManager _user;
    private BaseCacheService _cache;

    private const string CachePrefix = "TmsMaintenanceSchedule";

    private static readonly HashSet<string> ScheduleStatusDraftVariants = new(StringComparer.OrdinalIgnoreCase)
    {
        MaintenanceConst.ScheduleStatus.Register,
        MaintenanceConst.ScheduleStatus.Draft,
        MaintenanceConst.ScheduleStatus.LegacyDraft,
        MaintenanceConst.ScheduleStatus.LegacyDraftLower
    };

    private static readonly HashSet<string> ScheduleStatusScheduledVariants = new(StringComparer.OrdinalIgnoreCase)
    {
        MaintenanceConst.ScheduleStatus.Confirm,
        MaintenanceConst.ScheduleStatus.Scheduled,
        MaintenanceConst.ScheduleStatus.LegacyScheduled,
        MaintenanceConst.ScheduleStatus.LegacyScheduleLower,
        MaintenanceConst.ScheduleStatus.LegacyScheduledLower
    };

    private static readonly HashSet<string> ScheduleStatusInProgressVariants = new(StringComparer.OrdinalIgnoreCase)
    {
        MaintenanceConst.ScheduleStatus.Process,
        MaintenanceConst.ScheduleStatus.InProgress,
        MaintenanceConst.ScheduleStatus.LegacyInProgress,
        MaintenanceConst.ScheduleStatus.LegacyInProgressUnderscore,
        MaintenanceConst.ScheduleStatus.LegacyInProgressLower
    };

    private static readonly HashSet<string> ScheduleStatusCompletedVariants = new(StringComparer.OrdinalIgnoreCase)
    {
        MaintenanceConst.ScheduleStatus.Finish,
        MaintenanceConst.ScheduleStatus.Completed,
        MaintenanceConst.ScheduleStatus.LegacyCompleted,
        MaintenanceConst.ScheduleStatus.LegacyCompletedLower
    };

    private static readonly HashSet<string> ScheduleStatusCancelledVariants = new(StringComparer.OrdinalIgnoreCase)
    {
        MaintenanceConst.ScheduleStatus.Cancel,
        MaintenanceConst.ScheduleStatus.Cancelled,
        MaintenanceConst.ScheduleStatus.LegacyCancelled,
        MaintenanceConst.ScheduleStatus.LegacyCanceled,
        MaintenanceConst.ScheduleStatus.LegacyCancelledLower,
        MaintenanceConst.ScheduleStatus.LegacyCanceledLower
    };

    private static readonly HashSet<string> ScheduleStatusOverdueVariants = new(StringComparer.OrdinalIgnoreCase)
    {
        MaintenanceConst.ScheduleStatus.Overdue,
        "Overdue"
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

    private static readonly HashSet<string> EquipmentStatusCompletedVariants = new(StringComparer.OrdinalIgnoreCase)
    {
        MaintenanceConst.EquipmentStatus.Completed,
        MaintenanceConst.EquipmentStatus.LegacyCompleted,
        MaintenanceConst.EquipmentStatus.LegacyCompletedLower,
        MaintenanceConst.EquipmentStatus.LegacyFinishLower
    };

    private static readonly HashSet<string> EquipmentStatusSkippedVariants = new(StringComparer.OrdinalIgnoreCase)
    {
        MaintenanceConst.EquipmentStatus.Skipped,
        MaintenanceConst.EquipmentStatus.LegacySkipped
    };

    private static readonly HashSet<string> TaskStatusCompletedVariants = new(StringComparer.OrdinalIgnoreCase)
    {
        MaintenanceConst.TaskStatus.Completed,
        MaintenanceConst.TaskStatus.LegacyCompleted,
        MaintenanceConst.TaskStatus.LegacyCompletedLower,
        MaintenanceConst.TaskStatus.LegacyVnCompleted
    };

    private static readonly HashSet<string> TaskStatusSkippedVariants = new(StringComparer.OrdinalIgnoreCase)
    {
        MaintenanceConst.TaskStatus.Skipped,
        MaintenanceConst.TaskStatus.LegacySkipped,
        MaintenanceConst.TaskStatus.LegacyVnSkipped
    };

    private static readonly HashSet<string> TaskStatusPendingVariants = new(StringComparer.OrdinalIgnoreCase)
    {
        MaintenanceConst.TaskStatus.Pending,
        MaintenanceConst.TaskStatus.LegacyPending,
        MaintenanceConst.TaskStatus.LegacyInProgress,
        MaintenanceConst.TaskStatus.LegacyInProgressLower,
        MaintenanceConst.TaskStatus.LegacyVnPending
    };

    private static readonly HashSet<string> RoleLeaderVariants = new(StringComparer.OrdinalIgnoreCase)
    {
        MaintenanceConst.Role.Leader,
        MaintenanceConst.Role.LegacyLeader
    };

    private static readonly HashSet<string> RoleMemberVariants = new(StringComparer.OrdinalIgnoreCase)
    {
        MaintenanceConst.Role.Member,
        MaintenanceConst.Role.LegacyMember
    };

    private static readonly HashSet<string> HistoryCreateVariants = new(StringComparer.OrdinalIgnoreCase)
    {
        MaintenanceConst.HistoryActionType.Create,
        MaintenanceConst.HistoryActionType.LegacyCreate
    };

    private static readonly HashSet<string> HistoryUpdateVariants = new(StringComparer.OrdinalIgnoreCase)
    {
        MaintenanceConst.HistoryActionType.Update,
        MaintenanceConst.HistoryActionType.LegacyUpdate
    };

    private static readonly HashSet<string> HistoryDeleteVariants = new(StringComparer.OrdinalIgnoreCase)
    {
        MaintenanceConst.HistoryActionType.Delete,
        MaintenanceConst.HistoryActionType.LegacyDelete
    };

    private static readonly HashSet<string> HistoryStatusChangeVariants = new(StringComparer.OrdinalIgnoreCase)
    {
        MaintenanceConst.HistoryActionType.StatusChange,
        MaintenanceConst.HistoryActionType.LegacyStatusChange
    };

    private static readonly HashSet<string> HistoryAddEquipmentVariants = new(StringComparer.OrdinalIgnoreCase)
    {
        MaintenanceConst.HistoryActionType.AddEquipment,
        MaintenanceConst.HistoryActionType.LegacyAddEquipment
    };

    private static readonly HashSet<string> HistoryRemoveEquipmentVariants = new(StringComparer.OrdinalIgnoreCase)
    {
        MaintenanceConst.HistoryActionType.RemoveEquipment,
        MaintenanceConst.HistoryActionType.LegacyRemoveEquipment
    };

    private static readonly HashSet<string> HistoryUpdateEquipmentStatusVariants = new(StringComparer.OrdinalIgnoreCase)
    {
        MaintenanceConst.HistoryActionType.UpdateEquipmentStatus,
        MaintenanceConst.HistoryActionType.LegacyUpdateEquipmentStatus
    };

    private static readonly HashSet<string> HistoryConfirmEquipmentCheckedVariants = new(StringComparer.OrdinalIgnoreCase)
    {
        MaintenanceConst.HistoryActionType.ConfirmEquipmentChecked,
        MaintenanceConst.HistoryActionType.LegacyConfirmEquipmentChecked
    };

    private static readonly HashSet<string> HistoryAssignTaskVariants = new(StringComparer.OrdinalIgnoreCase)
    {
        MaintenanceConst.HistoryActionType.AssignTask,
        MaintenanceConst.HistoryActionType.LegacyAssignTask
    };

    private static readonly HashSet<string> HistoryRemoveTaskVariants = new(StringComparer.OrdinalIgnoreCase)
    {
        MaintenanceConst.HistoryActionType.RemoveTask,
        MaintenanceConst.HistoryActionType.LegacyRemoveTask
    };

    private static readonly HashSet<string> HistoryCompleteTaskVariants = new(StringComparer.OrdinalIgnoreCase)
    {
        MaintenanceConst.HistoryActionType.CompleteTask,
        MaintenanceConst.HistoryActionType.LegacyCompleteTask
    };

    public MaintenanceScheduleQueryHandler(
        BaseRepository<TmsMaintenanceSchedule> scheduleRep,
        BaseRepository<TmsMaintenanceScheduleEquipment> scheduleEquipmentRep,
        BaseRepository<TmsMaintenanceTask> taskRep,
        BaseRepository<TmsMaintenanceGuide> guideRep,
        BaseRepository<TmsMaintenanceHistory> historyRep,
        BaseRepository<TmsEquipment> equipmentRep,
        BaseRepository<TmsEquipmentType> equipmentTypeRep,
        UserManager user,
        BaseCacheService cache)
    {
        _scheduleRep = scheduleRep;
        _scheduleEquipmentRep = scheduleEquipmentRep;
        _taskRep = taskRep;
        _guideRep = guideRep;
        _historyRep = historyRep;
        _equipmentRep = equipmentRep;
        _equipmentTypeRep = equipmentTypeRep;
        _user = user;
        _cache = cache;
    }

    /// <summary>
    /// Lấy danh sách lịch bảo trì
    /// </summary>
    public async Task<List<MaintenanceScheduleOutput>> HandleAsync(MaintenanceScheduleInput query)
    {
        var currentUser = _user.Account ?? "";

        List<string>? statusVariants = null;
        if (!string.IsNullOrEmpty(query.Status))
        {
            statusVariants = ScheduleStatusDraftVariants.Contains(query.Status)
                ? ScheduleStatusDraftVariants.ToList()
                : ScheduleStatusScheduledVariants.Contains(query.Status)
                    ? ScheduleStatusScheduledVariants.ToList()
                    : ScheduleStatusInProgressVariants.Contains(query.Status)
                        ? ScheduleStatusInProgressVariants.ToList()
                        : ScheduleStatusCompletedVariants.Contains(query.Status)
                            ? ScheduleStatusCompletedVariants.ToList()
                            : ScheduleStatusCancelledVariants.Contains(query.Status)
                                ? ScheduleStatusCancelledVariants.ToList()
                                : new List<string> { query.Status };
        }

        List<string>? priorityVariants = null;
        if (!string.IsNullOrEmpty(query.Priority))
        {
            priorityVariants = PriorityHighVariants.Contains(query.Priority)
                ? PriorityHighVariants.ToList()
                : PriorityMediumVariants.Contains(query.Priority)
                    ? PriorityMediumVariants.ToList()
                    : PriorityLowVariants.Contains(query.Priority)
                        ? PriorityLowVariants.ToList()
                        : new List<string> { query.Priority };
        }

        var baseQuery = _scheduleRep.AsQueryable()
            .WhereIF(!string.IsNullOrEmpty(query.Name), u => u.Name!.Contains(query.Name!))
            .WhereIF(query.FromDate.HasValue, u => u.ScheduledDate >= query.FromDate)
            .WhereIF(query.ToDate.HasValue, u => u.ScheduledDate <= query.ToDate)
            .WhereIF(statusVariants != null && statusVariants.Any(), u => statusVariants.Contains(u.Status))
            .WhereIF(priorityVariants != null && priorityVariants.Any(), u => priorityVariants.Contains(u.Priority));

        // Filter by equipment
        if (!string.IsNullOrEmpty(query.EquipmentId) || !string.IsNullOrEmpty(query.EquipmentTypeId))
        {
            var scheduleIds = await GetScheduleIdsByEquipment(query.EquipmentId, query.EquipmentTypeId);
            baseQuery = baseQuery.Where(u => scheduleIds.Contains(u.ID));
        }

        // Filter by assigned user
        if (!string.IsNullOrEmpty(query.AssignedUserId))
        {
            var scheduleIds = await GetScheduleIdsByUser(query.AssignedUserId);
            baseQuery = baseQuery.Where(u => scheduleIds.Contains(u.ID));
        }

        // Filter mode
        switch (query.FilterMode)
        {
            case MaintenanceScheduleFilterMode.CreatedByMe:
                baseQuery = baseQuery.Where(u => u.CreateUId == currentUser);
                break;
            case MaintenanceScheduleFilterMode.AssignedToMe:
                var myScheduleIds = await GetScheduleIdsByUser(currentUser);
                baseQuery = baseQuery.Where(u => myScheduleIds.Contains(u.ID));
                break;
        }

        var result = await baseQuery
            .OrderByDescending(u => u.ScheduledDate)
            .ToListAsync();

        return await MapToOutput(result, includeDetails: false);
    }

    /// <summary>
    /// Lấy danh sách có phân trang
    /// </summary>
    public async Task<SqlSugarPagedList<MaintenanceScheduleOutput>> HandleAsync(PageMaintenanceScheduleInput query)
    {
        var currentUser = _user.Account ?? "";

        List<string>? statusVariants = null;
        if (!string.IsNullOrEmpty(query.Status))
        {
            statusVariants = ScheduleStatusDraftVariants.Contains(query.Status)
                ? ScheduleStatusDraftVariants.ToList()
                : ScheduleStatusScheduledVariants.Contains(query.Status)
                    ? ScheduleStatusScheduledVariants.ToList()
                    : ScheduleStatusInProgressVariants.Contains(query.Status)
                        ? ScheduleStatusInProgressVariants.ToList()
                        : ScheduleStatusCompletedVariants.Contains(query.Status)
                            ? ScheduleStatusCompletedVariants.ToList()
                            : ScheduleStatusCancelledVariants.Contains(query.Status)
                                ? ScheduleStatusCancelledVariants.ToList()
                                : new List<string> { query.Status };
        }

        List<string>? priorityVariants = null;
        if (!string.IsNullOrEmpty(query.Priority))
        {
            priorityVariants = PriorityHighVariants.Contains(query.Priority)
                ? PriorityHighVariants.ToList()
                : PriorityMediumVariants.Contains(query.Priority)
                    ? PriorityMediumVariants.ToList()
                    : PriorityLowVariants.Contains(query.Priority)
                        ? PriorityLowVariants.ToList()
                        : new List<string> { query.Priority };
        }

        var baseQuery = _scheduleRep.AsQueryable()
            .WhereIF(!string.IsNullOrEmpty(query.Name), u => u.Name!.Contains(query.Name!))
            .WhereIF(query.FromDate.HasValue, u => u.ScheduledDate >= query.FromDate)
            .WhereIF(query.ToDate.HasValue, u => u.ScheduledDate <= query.ToDate)
            .WhereIF(statusVariants != null && statusVariants.Any(), u => statusVariants.Contains(u.Status))
            .WhereIF(priorityVariants != null && priorityVariants.Any(), u => priorityVariants.Contains(u.Priority));

        // Filter by equipment
        if (!string.IsNullOrEmpty(query.EquipmentId) || !string.IsNullOrEmpty(query.EquipmentTypeId))
        {
            var scheduleIds = await GetScheduleIdsByEquipment(query.EquipmentId, query.EquipmentTypeId);
            baseQuery = baseQuery.Where(u => scheduleIds.Contains(u.ID));
        }

        // Filter by assigned user
        if (!string.IsNullOrEmpty(query.AssignedUserId))
        {
            var scheduleIds = await GetScheduleIdsByUser(query.AssignedUserId);
            baseQuery = baseQuery.Where(u => scheduleIds.Contains(u.ID));
        }

        // Filter mode
        switch (query.FilterMode)
        {
            case MaintenanceScheduleFilterMode.CreatedByMe:
                baseQuery = baseQuery.Where(u => u.CreateUId == currentUser);
                break;
            case MaintenanceScheduleFilterMode.AssignedToMe:
                var myScheduleIds = await GetScheduleIdsByUser(currentUser);
                baseQuery = baseQuery.Where(u => myScheduleIds.Contains(u.ID));
                break;
        }

        var pagedList = await baseQuery
            .OrderByDescending(u => u.ScheduledDate)
            .Select(u => new MaintenanceScheduleOutput() { }, true)
            .ToPagedListAsync(query.Page, query.PageSize);

        return pagedList;
    }

    /// <summary>
    /// Lấy lịch bảo trì theo tuần
    /// </summary>
    public async Task<List<MaintenanceScheduleOutput>> HandleAsync(GetByWeekInput query)
    {
        var currentUser = _user.Account ?? "";

        // Calculate week start/end
        var dayOfWeek = (int)query.Date.DayOfWeek;
        var weekStart = query.Date.AddDays(-dayOfWeek).Date; // Sunday
        var weekEnd = weekStart.AddDays(7).AddSeconds(-1);

        List<string>? statusVariants = null;
        if (!string.IsNullOrEmpty(query.Status))
        {
            statusVariants = ScheduleStatusDraftVariants.Contains(query.Status)
                ? ScheduleStatusDraftVariants.ToList()
                : ScheduleStatusScheduledVariants.Contains(query.Status)
                    ? ScheduleStatusScheduledVariants.ToList()
                    : ScheduleStatusInProgressVariants.Contains(query.Status)
                        ? ScheduleStatusInProgressVariants.ToList()
                        : ScheduleStatusCompletedVariants.Contains(query.Status)
                            ? ScheduleStatusCompletedVariants.ToList()
                            : ScheduleStatusCancelledVariants.Contains(query.Status)
                                ? ScheduleStatusCancelledVariants.ToList()
                                : new List<string> { query.Status };
        }

        List<string>? priorityVariants = null;
        if (!string.IsNullOrEmpty(query.Priority))
        {
            priorityVariants = PriorityHighVariants.Contains(query.Priority)
                ? PriorityHighVariants.ToList()
                : PriorityMediumVariants.Contains(query.Priority)
                    ? PriorityMediumVariants.ToList()
                    : PriorityLowVariants.Contains(query.Priority)
                        ? PriorityLowVariants.ToList()
                        : new List<string> { query.Priority };
        }

        var baseQuery = _scheduleRep.AsQueryable()
            .Where(u => u.ScheduledDate >= weekStart && u.ScheduledDate <= weekEnd)
            .WhereIF(statusVariants != null && statusVariants.Any(), u => statusVariants.Contains(u.Status))
            .WhereIF(priorityVariants != null && priorityVariants.Any(), u => priorityVariants.Contains(u.Priority));

        // Filter mode
        switch (query.FilterMode)
        {
            case MaintenanceScheduleFilterMode.CreatedByMe:
                baseQuery = baseQuery.Where(u => u.CreateUId == currentUser);
                break;
            case MaintenanceScheduleFilterMode.AssignedToMe:
                var myScheduleIds = await GetScheduleIdsByUser(currentUser);
                baseQuery = baseQuery.Where(u => myScheduleIds.Contains(u.ID));
                break;
        }

        var result = await baseQuery
            .OrderBy(u => u.ScheduledDate)
            .ToListAsync();

        return await MapToOutput(result, includeDetails: false);
    }

    /// <summary>
    /// Lấy lịch bảo trì theo tháng (kèm summary)
    /// </summary>
    public async Task<GetByMonthResponse> HandleAsync(GetByMonthInput query)
    {
        var currentUser = _user.Account ?? "";

        var monthStart = new DateTime(query.Year, query.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddSeconds(-1);

        List<string>? statusVariants = null;
        if (!string.IsNullOrEmpty(query.Status))
        {
            statusVariants = ScheduleStatusDraftVariants.Contains(query.Status)
                ? ScheduleStatusDraftVariants.ToList()
                : ScheduleStatusScheduledVariants.Contains(query.Status)
                    ? ScheduleStatusScheduledVariants.ToList()
                    : ScheduleStatusInProgressVariants.Contains(query.Status)
                        ? ScheduleStatusInProgressVariants.ToList()
                        : ScheduleStatusCompletedVariants.Contains(query.Status)
                            ? ScheduleStatusCompletedVariants.ToList()
                            : ScheduleStatusCancelledVariants.Contains(query.Status)
                                ? ScheduleStatusCancelledVariants.ToList()
                                : new List<string> { query.Status };
        }

        List<string>? priorityVariants = null;
        if (!string.IsNullOrEmpty(query.Priority))
        {
            priorityVariants = PriorityHighVariants.Contains(query.Priority)
                ? PriorityHighVariants.ToList()
                : PriorityMediumVariants.Contains(query.Priority)
                    ? PriorityMediumVariants.ToList()
                    : PriorityLowVariants.Contains(query.Priority)
                        ? PriorityLowVariants.ToList()
                        : new List<string> { query.Priority };
        }

        var baseQuery = _scheduleRep.AsQueryable()
            .Where(u => u.ScheduledDate >= monthStart && u.ScheduledDate <= monthEnd)
            .WhereIF(statusVariants != null && statusVariants.Any(), u => statusVariants.Contains(u.Status))
            .WhereIF(priorityVariants != null && priorityVariants.Any(), u => priorityVariants.Contains(u.Priority));

        // Filter mode
        switch (query.FilterMode)
        {
            case MaintenanceScheduleFilterMode.CreatedByMe:
                baseQuery = baseQuery.Where(u => u.CreateUId == currentUser);
                break;
            case MaintenanceScheduleFilterMode.AssignedToMe:
                var myScheduleIds = await GetScheduleIdsByUser(currentUser);
                baseQuery = baseQuery.Where(u => myScheduleIds.Contains(u.ID));
                break;
        }

        var schedules = await baseQuery
            .OrderBy(u => u.ScheduledDate)
            .ToListAsync();

        var data = await MapToOutput(schedules, includeDetails: false);

        // Build summary
        var summary = new ScheduleCalendarSummary
        {
            Total = schedules.Count,
            ByStatus = schedules.GroupBy(s =>
                    string.IsNullOrEmpty(s.Status)
                        ? "Unknown"
                        : ScheduleStatusDraftVariants.Contains(s.Status)
                            ? MaintenanceConst.ScheduleStatus.Draft
                            : ScheduleStatusScheduledVariants.Contains(s.Status)
                                ? MaintenanceConst.ScheduleStatus.Scheduled
                                : ScheduleStatusInProgressVariants.Contains(s.Status)
                                    ? MaintenanceConst.ScheduleStatus.InProgress
                                    : ScheduleStatusCompletedVariants.Contains(s.Status)
                                        ? MaintenanceConst.ScheduleStatus.Completed
                                        : ScheduleStatusOverdueVariants.Contains(s.Status)
                                            ? MaintenanceConst.ScheduleStatus.Overdue
                                        : ScheduleStatusCancelledVariants.Contains(s.Status)
                                            ? MaintenanceConst.ScheduleStatus.Cancelled
                                            : s.Status)
                .ToDictionary(g => g.Key, g => g.Count()),
            ByPriority = schedules.GroupBy(s =>
                    string.IsNullOrEmpty(s.Priority)
                        ? "Unknown"
                        : PriorityHighVariants.Contains(s.Priority)
                            ? MaintenanceConst.Priority.High
                            : PriorityMediumVariants.Contains(s.Priority)
                                ? MaintenanceConst.Priority.Medium
                                : PriorityLowVariants.Contains(s.Priority)
                                    ? MaintenanceConst.Priority.Low
                                    : s.Priority)
                .ToDictionary(g => g.Key, g => g.Count()),
            ByDay = schedules.GroupBy(s => s.ScheduledDate.Date)
                .Select(g => new DaySummary
                {
                    Date = g.Key,
                    Count = g.Count(),
                    HasHighPriority = g.Any(s => !string.IsNullOrEmpty(s.Priority) && PriorityHighVariants.Contains(s.Priority))
                })
                .OrderBy(d => d.Date)
                .ToList()
        };

        return new GetByMonthResponse
        {
            Data = data,
            Summary = summary
        };
    }

    /// <summary>
    /// Lấy lịch bảo trì theo năm (summary only)
    /// </summary>
    public async Task<ScheduleCalendarSummary> HandleAsync(GetByYearInput query)
    {
        var yearStart = new DateTime(query.Year, 1, 1);
        var yearEnd = yearStart.AddYears(1).AddSeconds(-1);

        List<string>? statusVariants = null;
        if (!string.IsNullOrEmpty(query.Status))
        {
            statusVariants = ScheduleStatusDraftVariants.Contains(query.Status)
                ? ScheduleStatusDraftVariants.ToList()
                : ScheduleStatusScheduledVariants.Contains(query.Status)
                    ? ScheduleStatusScheduledVariants.ToList()
                    : ScheduleStatusInProgressVariants.Contains(query.Status)
                        ? ScheduleStatusInProgressVariants.ToList()
                        : ScheduleStatusCompletedVariants.Contains(query.Status)
                            ? ScheduleStatusCompletedVariants.ToList()
                            : ScheduleStatusCancelledVariants.Contains(query.Status)
                                ? ScheduleStatusCancelledVariants.ToList()
                                : new List<string> { query.Status };
        }

        var schedules = await _scheduleRep.AsQueryable()
            .Where(u => u.ScheduledDate >= yearStart && u.ScheduledDate <= yearEnd)
            .WhereIF(statusVariants != null && statusVariants.Any(), u => statusVariants.Contains(u.Status))
            .ToListAsync();

        return new ScheduleCalendarSummary
        {
            Total = schedules.Count,
            ByStatus = schedules.GroupBy(s => s.Status ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.Count()),
            ByPriority = schedules.GroupBy(s => s.Priority ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.Count()),
            ByDay = schedules.GroupBy(s => s.ScheduledDate.Date)
                .Select(g => new DaySummary
                {
                    Date = g.Key,
                    Count = g.Count(),
                    HasHighPriority = g.Any(s => !string.IsNullOrEmpty(s.Priority) && PriorityHighVariants.Contains(s.Priority))
                })
                .OrderBy(d => d.Date)
                .ToList()
        };
    }

    /// <summary>
    /// Lấy lịch bảo trì sắp tới
    /// </summary>
    public async Task<List<MaintenanceScheduleOutput>> HandleAsync(GetUpcomingMaintenanceSchedulesInput query)
    {
        var userId = query.UserId ?? _user.Account ?? "";
        var now = DateTime.Now;
        var maxDate = now.AddDays(query.MaxDays);

        var myScheduleIds = await GetScheduleIdsByUser(userId);

        var baseQuery = _scheduleRep.AsQueryable()
            .Where(u => myScheduleIds.Contains(u.ID))
            .Where(u => u.ScheduledDate >= now && u.ScheduledDate <= maxDate);

        if (query.ActiveOnly)
        {
            baseQuery = baseQuery.Where(u =>
                u.Status == MaintenanceConst.ScheduleStatus.Scheduled ||
                u.Status == MaintenanceConst.ScheduleStatus.InProgress);
        }

        var result = await baseQuery
            .OrderBy(u => u.ScheduledDate)
            .Take(query.MaxCount)
            .ToListAsync();

        return await MapToOutput(result, includeDetails: false);
    }

    /// <summary>
    /// Lấy chi tiết lịch bảo trì theo ID
    /// </summary>
    public async Task<MaintenanceScheduleOutput?> HandleAsync(GetMaintenanceScheduleByIdInput query)
    {
        var entity = await _scheduleRep.GetByIdAsync(query.ID);
        if (entity == null) return null;

        var outputs = await MapToOutput(new List<TmsMaintenanceSchedule> { entity }, includeDetails: true);
        return outputs.FirstOrDefault();
    }

    #region Private Methods

    private async Task<List<string>> GetScheduleIdsByEquipment(string? equipmentId, string? equipmentTypeId)
    {
        var query = _scheduleEquipmentRep.AsQueryable();

        if (!string.IsNullOrEmpty(equipmentId))
        {
            query = query.Where(e => e.EquipmentId == equipmentId);
        }
        else if (!string.IsNullOrEmpty(equipmentTypeId))
        {
            var equipmentIds = await _equipmentRep.AsQueryable()
                .Where(e => e.EquipmentTypeId == equipmentTypeId)
                .Select(e => e.ID)
                .ToListAsync();

            query = query.Where(e => equipmentIds.Contains(e.EquipmentId));
        }

        return await query
            .Select(e => e.MaintenanceScheduleId)
            .Distinct()
            .ToListAsync();
    }

    private async Task<List<string>> GetScheduleIdsByUser(string userId)
    {
        return await _taskRep.AsQueryable()
            .Where(t => t.AssignedUserId == userId)
            .Select(t => t.MaintenanceScheduleId)
            .Distinct()
            .ToListAsync();
    }

    private async Task<List<MaintenanceScheduleOutput>> MapToOutput(List<TmsMaintenanceSchedule> schedules, bool includeDetails)
    {
        var result = new List<MaintenanceScheduleOutput>();

        foreach (var schedule in schedules)
        {
            var output = schedule.Adapt<MaintenanceScheduleOutput>();

            if (!string.IsNullOrEmpty(output.Status))
            {
                output.Status = ScheduleStatusDraftVariants.Contains(output.Status)
                    ? MaintenanceConst.ScheduleStatus.Draft
                    : ScheduleStatusScheduledVariants.Contains(output.Status)
                        ? MaintenanceConst.ScheduleStatus.Scheduled
                        : ScheduleStatusInProgressVariants.Contains(output.Status)
                            ? MaintenanceConst.ScheduleStatus.InProgress
                            : ScheduleStatusCompletedVariants.Contains(output.Status)
                                ? MaintenanceConst.ScheduleStatus.Completed
                                : ScheduleStatusOverdueVariants.Contains(output.Status)
                                    ? MaintenanceConst.ScheduleStatus.Overdue
                                : ScheduleStatusCancelledVariants.Contains(output.Status)
                                    ? MaintenanceConst.ScheduleStatus.Cancelled
                                    : output.Status;
            }

            if (!string.IsNullOrEmpty(output.Priority))
            {
                output.Priority = PriorityHighVariants.Contains(output.Priority)
                    ? MaintenanceConst.Priority.High
                    : PriorityMediumVariants.Contains(output.Priority)
                        ? MaintenanceConst.Priority.Medium
                        : PriorityLowVariants.Contains(output.Priority)
                            ? MaintenanceConst.Priority.Low
                            : output.Priority;
            }

            // Get equipment count
            var equipments = await _scheduleEquipmentRep.AsQueryable()
                .Where(e => e.MaintenanceScheduleId == schedule.ID)
                .ToListAsync();

            output.EquipmentCount = equipments.Count;
            output.CompletedEquipmentCount = equipments.Count(e =>
                !string.IsNullOrEmpty(e.Status) &&
                (EquipmentStatusCompletedVariants.Contains(e.Status) || EquipmentStatusSkippedVariants.Contains(e.Status)));

            // Get task count
            var tasks = await _taskRep.AsQueryable()
                .Where(t => t.MaintenanceScheduleId == schedule.ID)
                .ToListAsync();

            output.TaskCount = tasks.Count;
            output.CompletedTaskCount = tasks.Count(t =>
                !string.IsNullOrEmpty(t.Status) &&
                (TaskStatusCompletedVariants.Contains(t.Status) || TaskStatusSkippedVariants.Contains(t.Status)));

            // Build equipment type summary
            if (equipments.Any())
            {
                // Prefer snapshot type when available (historical correctness)
                var typeIdByEquipmentId = new Dictionary<string, string?>();
                var typeNameByEquipmentId = new Dictionary<string, string?>();
                var missingEquipmentIds = new List<string>();

                foreach (var e in equipments)
                {
                    if (!string.IsNullOrEmpty(e.EquipmentTypeIdSnapshot))
                    {
                        typeIdByEquipmentId[e.EquipmentId] = e.EquipmentTypeIdSnapshot;
                        typeNameByEquipmentId[e.EquipmentId] = e.EquipmentTypeNameSnapshot;
                    }
                    else
                    {
                        missingEquipmentIds.Add(e.EquipmentId);
                    }
                }

                if (missingEquipmentIds.Any())
                {
                    var currentEquipments = await _equipmentRep.AsQueryable()
                        .Where(e => missingEquipmentIds.Contains(e.ID))
                        .Select(e => new { e.ID, e.EquipmentTypeId })
                        .ToListAsync();

                    foreach (var ce in currentEquipments)
                    {
                        typeIdByEquipmentId[ce.ID] = ce.EquipmentTypeId;
                    }
                }

                var usedTypeIds = typeIdByEquipmentId.Values
                    .Where(id => !string.IsNullOrEmpty(id))
                    .Distinct()
                    .ToList();

                var types = await _equipmentTypeRep.AsQueryable()
                    .Where(t => usedTypeIds.Contains(t.ID))
                    .Select(t => new { t.ID, t.Name })
                    .ToListAsync();
                var typeNameById = types.ToDictionary(t => t.ID, t => t.Name);

                output.EquipmentSummary = equipments
                    .GroupBy(e => typeIdByEquipmentId.TryGetValue(e.EquipmentId, out var typeId) ? typeId : null)
                    .Select(g =>
                    {
                        var typeId = g.Key;
                        string? snapshotTypeName = null;
                        foreach (var eq in g)
                        {
                            if (typeNameByEquipmentId.TryGetValue(eq.EquipmentId, out var n) && !string.IsNullOrEmpty(n))
                            {
                                snapshotTypeName = n;
                                break;
                            }
                        }

                        string? resolvedTypeName = snapshotTypeName;
                        if (string.IsNullOrEmpty(resolvedTypeName) && !string.IsNullOrEmpty(typeId) && typeNameById.TryGetValue(typeId, out var n2))
                            resolvedTypeName = n2;

                        return new EquipmentTypeSummary
                        {
                            EquipmentTypeId = typeId,
                            EquipmentTypeName = resolvedTypeName,
                            Count = g.Count()
                        };
                    })
                    .ToList();
            }

            // Include details if requested
            if (includeDetails)
            {
                output.Equipments = await GetEquipmentDetails(equipments);
                output.Tasks = await GetTaskDetails(tasks);
            }

            result.Add(output);
        }

        return result;
    }

    private async Task<List<ScheduleEquipmentOutput>> GetEquipmentDetails(List<TmsMaintenanceScheduleEquipment> equipments)
    {
        var result = new List<ScheduleEquipmentOutput>();

        foreach (var eq in equipments)
        {
            var output = eq.Adapt<ScheduleEquipmentOutput>();

            if (!string.IsNullOrEmpty(output.Status))
            {
                output.Status = EquipmentStatusCompletedVariants.Contains(output.Status)
                    ? MaintenanceConst.EquipmentStatus.Completed
                    : EquipmentStatusSkippedVariants.Contains(output.Status)
                        ? MaintenanceConst.EquipmentStatus.Skipped
                        : string.Equals(output.Status, MaintenanceConst.EquipmentStatus.Pending, StringComparison.OrdinalIgnoreCase)
                            || string.Equals(output.Status, MaintenanceConst.EquipmentStatus.LegacyPending, StringComparison.OrdinalIgnoreCase)
                            ? MaintenanceConst.EquipmentStatus.Pending
                            : string.Equals(output.Status, MaintenanceConst.EquipmentStatus.InProgress, StringComparison.OrdinalIgnoreCase)
                                || string.Equals(output.Status, MaintenanceConst.EquipmentStatus.LegacyInProgress, StringComparison.OrdinalIgnoreCase)
                                ? MaintenanceConst.EquipmentStatus.InProgress
                                : output.Status;
            }

            // Get equipment info (name/manufacturer/km) from current master
            var tmsEquipment = await _equipmentRep.GetByIdAsync(eq.EquipmentId);
            if (tmsEquipment != null)
            {
                output.EquipmentName = tmsEquipment.Name;
                output.EquipmentCode = tmsEquipment.Code;
                output.Manufacturer = tmsEquipment.Manufacturer;
                output.KmNumber = tmsEquipment.KmNumber;
            }

            // Prefer snapshot type/state for historical correctness
            if (!string.IsNullOrEmpty(eq.EquipmentTypeIdSnapshot))
            {
                output.EquipmentTypeId = eq.EquipmentTypeIdSnapshot;
                output.EquipmentTypeName = eq.EquipmentTypeNameSnapshot;

                if (string.IsNullOrEmpty(output.EquipmentTypeName))
                {
                    var type = await _equipmentTypeRep.GetByIdAsync(eq.EquipmentTypeIdSnapshot);
                    output.EquipmentTypeName = type?.Name;
                }
            }
            else if (tmsEquipment != null)
            {
                output.EquipmentTypeId = tmsEquipment.EquipmentTypeId;
                if (!string.IsNullOrEmpty(tmsEquipment.EquipmentTypeId))
                {
                    var type = await _equipmentTypeRep.GetByIdAsync(tmsEquipment.EquipmentTypeId);
                    output.EquipmentTypeName = type?.Name;
                }
            }

            if (eq.EquipmentStateSnapshot.HasValue)
            {
                output.EquipmentState = eq.EquipmentStateSnapshot;
            }
            else if (tmsEquipment != null)
            {
                output.EquipmentState = tmsEquipment.EquipmentState;
            }

            // Get guide info
            if (!string.IsNullOrEmpty(eq.MaintenanceGuideId))
            {
                var guide = await _guideRep.GetByIdAsync(eq.MaintenanceGuideId);
                if (guide != null)
                {
                    output.MaintenanceGuideName = guide.Name;

                    // Parse checklist
                    if (!string.IsNullOrEmpty(guide.Checklist))
                    {
                        try
                        {
                            output.Checklist = JsonConvert.DeserializeObject<List<ChecklistItem>>(guide.Checklist);
                        }
                        catch { }
                    }
                }
            }

            // Parse completed checklist
            if (!string.IsNullOrEmpty(eq.CompletedChecklist))
            {
                try
                {
                    output.ChecklistCompleted = JsonConvert.DeserializeObject<List<ChecklistItem>>(eq.CompletedChecklist);
                }
                catch { }
            }

            result.Add(output);
        }

        return result;
    }

    private async Task<List<MaintenanceTaskOutput>> GetTaskDetails(List<TmsMaintenanceTask> tasks)
    {
        var result = new List<MaintenanceTaskOutput>();

        foreach (var task in tasks)
        {
            var output = task.Adapt<MaintenanceTaskOutput>();

            if (!string.IsNullOrEmpty(output.Role))
            {
                output.Role = RoleLeaderVariants.Contains(output.Role)
                    ? MaintenanceConst.Role.Leader
                    : RoleMemberVariants.Contains(output.Role)
                        ? MaintenanceConst.Role.Member
                        : output.Role;
            }
            else
            {
                output.Role = MaintenanceConst.Role.Member;
            }

            if (string.IsNullOrEmpty(output.Status))
            {
                output.Status = MaintenanceConst.TaskStatus.Pending;
            }
            else
            {
                output.Status = TaskStatusCompletedVariants.Contains(output.Status)
                    ? MaintenanceConst.TaskStatus.Completed
                    : TaskStatusSkippedVariants.Contains(output.Status)
                        ? MaintenanceConst.TaskStatus.Skipped
                        : TaskStatusPendingVariants.Contains(output.Status)
                            ? MaintenanceConst.TaskStatus.Pending
                            : MaintenanceConst.TaskStatus.Pending;
            }

            // TODO: Get user name from user service
            output.AssignedUserName = task.AssignedUserId;

            // Get equipment name if assigned to specific equipment
            if (!string.IsNullOrEmpty(task.ScheduleEquipmentId))
            {
                var scheduleEquipment = await _scheduleEquipmentRep.GetByIdAsync(task.ScheduleEquipmentId);
                if (scheduleEquipment != null)
                {
                    var equipment = await _equipmentRep.GetByIdAsync(scheduleEquipment.EquipmentId);
                    output.EquipmentName = equipment?.Name;
                }
            }

            result.Add(output);
        }

        return result;
    }

    #endregion

    #region History Query Handlers

    /// <summary>
    /// Lấy danh sách lịch sử thay đổi lịch bảo trì
    /// </summary>
    public async Task<List<MaintenanceHistoryOutput>> HandleAsync(MaintenanceHistoryQueryInput query)
    {
        List<string>? actionTypeVariants = null;
        if (!string.IsNullOrEmpty(query.ActionType))
        {
            actionTypeVariants = HistoryCreateVariants.Contains(query.ActionType)
                ? HistoryCreateVariants.ToList()
                : HistoryUpdateVariants.Contains(query.ActionType)
                    ? HistoryUpdateVariants.ToList()
                    : HistoryDeleteVariants.Contains(query.ActionType)
                        ? HistoryDeleteVariants.ToList()
                        : HistoryStatusChangeVariants.Contains(query.ActionType)
                            ? HistoryStatusChangeVariants.ToList()
                            : HistoryAddEquipmentVariants.Contains(query.ActionType)
                                ? HistoryAddEquipmentVariants.ToList()
                                : HistoryRemoveEquipmentVariants.Contains(query.ActionType)
                                    ? HistoryRemoveEquipmentVariants.ToList()
                                    : HistoryUpdateEquipmentStatusVariants.Contains(query.ActionType)
                                        ? HistoryUpdateEquipmentStatusVariants.ToList()
                                        : HistoryConfirmEquipmentCheckedVariants.Contains(query.ActionType)
                                            ? HistoryConfirmEquipmentCheckedVariants.ToList()
                                            : HistoryAssignTaskVariants.Contains(query.ActionType)
                                                ? HistoryAssignTaskVariants.ToList()
                                                : HistoryRemoveTaskVariants.Contains(query.ActionType)
                                                    ? HistoryRemoveTaskVariants.ToList()
                                                    : HistoryCompleteTaskVariants.Contains(query.ActionType)
                                                        ? HistoryCompleteTaskVariants.ToList()
                                                        : new List<string> { query.ActionType };
        }

        var result = await _historyRep.AsQueryable()
            .WhereIF(!string.IsNullOrEmpty(query.MaintenanceScheduleId), u => u.MaintenanceScheduleId == query.MaintenanceScheduleId)
            .WhereIF(actionTypeVariants != null && actionTypeVariants.Any(), u => actionTypeVariants.Contains(u.ActionType))
            .WhereIF(!string.IsNullOrEmpty(query.ActionBy), u => u.ActionBy == query.ActionBy)
            .WhereIF(query.FromDate.HasValue, u => u.CreateTime >= query.FromDate)
            .WhereIF(query.ToDate.HasValue, u => u.CreateTime <= query.ToDate)
            .OrderByDescending(u => u.CreateTime)
            .Select(u => new MaintenanceHistoryOutput() { }, true)
            .ToListAsync();

        return result;
    }

    /// <summary>
    /// Lấy danh sách lịch sử có phân trang
    /// </summary>
    public async Task<SqlSugarPagedList<MaintenanceHistoryOutput>> HandleAsync(PageMaintenanceHistoryInput query)
    {
        List<string>? actionTypeVariants = null;
        if (!string.IsNullOrEmpty(query.ActionType))
        {
            actionTypeVariants = HistoryCreateVariants.Contains(query.ActionType)
                ? HistoryCreateVariants.ToList()
                : HistoryUpdateVariants.Contains(query.ActionType)
                    ? HistoryUpdateVariants.ToList()
                    : HistoryDeleteVariants.Contains(query.ActionType)
                        ? HistoryDeleteVariants.ToList()
                        : HistoryStatusChangeVariants.Contains(query.ActionType)
                            ? HistoryStatusChangeVariants.ToList()
                            : HistoryAddEquipmentVariants.Contains(query.ActionType)
                                ? HistoryAddEquipmentVariants.ToList()
                                : HistoryRemoveEquipmentVariants.Contains(query.ActionType)
                                    ? HistoryRemoveEquipmentVariants.ToList()
                                    : HistoryUpdateEquipmentStatusVariants.Contains(query.ActionType)
                                        ? HistoryUpdateEquipmentStatusVariants.ToList()
                                        : HistoryConfirmEquipmentCheckedVariants.Contains(query.ActionType)
                                            ? HistoryConfirmEquipmentCheckedVariants.ToList()
                                            : HistoryAssignTaskVariants.Contains(query.ActionType)
                                                ? HistoryAssignTaskVariants.ToList()
                                                : HistoryRemoveTaskVariants.Contains(query.ActionType)
                                                    ? HistoryRemoveTaskVariants.ToList()
                                                    : HistoryCompleteTaskVariants.Contains(query.ActionType)
                                                        ? HistoryCompleteTaskVariants.ToList()
                                                        : new List<string> { query.ActionType };
        }

        var pagedList = await _historyRep.AsQueryable()
            .WhereIF(!string.IsNullOrEmpty(query.MaintenanceScheduleId), u => u.MaintenanceScheduleId == query.MaintenanceScheduleId)
            .WhereIF(actionTypeVariants != null && actionTypeVariants.Any(), u => actionTypeVariants.Contains(u.ActionType))
            .WhereIF(!string.IsNullOrEmpty(query.ActionBy), u => u.ActionBy == query.ActionBy)
            .WhereIF(query.FromDate.HasValue, u => u.CreateTime >= query.FromDate)
            .WhereIF(query.ToDate.HasValue, u => u.CreateTime <= query.ToDate)
            .OrderByDescending(u => u.CreateTime)
            .Select(u => new MaintenanceHistoryOutput() { }, true)
            .ToPagedListAsync(query.Page, query.PageSize);

        return pagedList;
    }

    #endregion
}
