using Microsoft.Extensions.Localization;
using Modules.TMS.Controllers.MaintenanceSchedule.Dto;
using Modules.TMS.Core.Constants;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Exceptions;
using Modules.TMS.Infrastructure;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;
using Shared.Utility.Utilities;

namespace Modules.TMS.Controllers.MaintenanceSchedule.Commands;

/// <summary>
/// Author: System
/// Description: Handler xử lý các lệnh của MaintenanceSchedule
/// Created date: 10/12/2025
/// </summary>
public class MaintenanceScheduleCommandHandler : IWolverineHandler
{
    private BaseRepository<TmsMaintenanceSchedule> _scheduleRep;
    private BaseRepository<TmsMaintenanceScheduleEquipment> _equipmentRep;
    private BaseRepository<TmsMaintenanceTask> _taskRep;
    private BaseRepository<TmsMaintenanceHistory> _historyRep;
    private BaseRepository<TmsMaintenanceGuide> _guideRep;
    private BaseRepository<TmsEquipment> _tmsEquipmentRep;
    private BaseRepository<TmsEquipmentType> _equipmentTypeRep;
    private UserManager _user;
    private BaseCacheService _cache;
    private readonly IStringLocalizer _localizer;

    private const string CachePrefix = "TmsMaintenanceSchedule";

    private static readonly HashSet<string> ScheduleStatusRegisterVariants = new(StringComparer.OrdinalIgnoreCase)
    {
        MaintenanceConst.ScheduleStatus.Register,
        MaintenanceConst.ScheduleStatus.Draft,
        MaintenanceConst.ScheduleStatus.LegacyDraft,
        MaintenanceConst.ScheduleStatus.LegacyDraftLower
    };

    private static readonly HashSet<string> ScheduleStatusConfirmVariants = new(StringComparer.OrdinalIgnoreCase)
    {
        MaintenanceConst.ScheduleStatus.Confirm,
        MaintenanceConst.ScheduleStatus.Scheduled,
        MaintenanceConst.ScheduleStatus.LegacyScheduled,
        MaintenanceConst.ScheduleStatus.LegacyScheduleLower,
        MaintenanceConst.ScheduleStatus.LegacyScheduledLower
    };

    private static readonly HashSet<string> ScheduleStatusProcessVariants = new(StringComparer.OrdinalIgnoreCase)
    {
        MaintenanceConst.ScheduleStatus.Process,
        MaintenanceConst.ScheduleStatus.InProgress,
        MaintenanceConst.ScheduleStatus.LegacyInProgress,
        MaintenanceConst.ScheduleStatus.LegacyInProgressUnderscore,
        MaintenanceConst.ScheduleStatus.LegacyInProgressLower
    };

    private static readonly HashSet<string> ScheduleStatusFinishedVariants = new(StringComparer.OrdinalIgnoreCase)
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

    public MaintenanceScheduleCommandHandler(
        BaseRepository<TmsMaintenanceSchedule> scheduleRep,
        BaseRepository<TmsMaintenanceScheduleEquipment> equipmentRep,
        BaseRepository<TmsMaintenanceTask> taskRep,
        BaseRepository<TmsMaintenanceHistory> historyRep,
        BaseRepository<TmsMaintenanceGuide> guideRep,
        BaseRepository<TmsEquipment> tmsEquipmentRep,
        BaseRepository<TmsEquipmentType> equipmentTypeRep,
        UserManager user,
        BaseCacheService cache,
        IStringLocalizer localizer)
    {
        _scheduleRep = scheduleRep;
        _equipmentRep = equipmentRep;
        _taskRep = taskRep;
        _historyRep = historyRep;
        _guideRep = guideRep;
        _tmsEquipmentRep = tmsEquipmentRep;
        _equipmentTypeRep = equipmentTypeRep;
        _user = user;
        _cache = cache;
        _localizer = localizer;
    }

    /// <summary>
    /// Thêm mới lịch bảo trì
    /// </summary>
    public async Task HandleAsync(AddMaintenanceScheduleInput command)
    {
        var username = _user.Account ?? "system";

        var scheduledDateServer = BaseUtil.TimeServer.GetTimeServer(command.ScheduledDate);
        if (scheduledDateServer.HasValue)
            command.ScheduledDate = scheduledDateServer.Value;
        if (command.DueDate.HasValue)
        {
            var dueDateServer = BaseUtil.TimeServer.GetTimeServer(command.DueDate.Value);
            if (dueDateServer.HasValue)
                command.DueDate = dueDateServer.Value;
            // Validate DueDate >= ScheduledDate
            if (command.DueDate < command.ScheduledDate)
                throw Oops.Oh(BaseMsg.TmsMaintenanceSchedule.Validation.DueDateBeforeScheduledDate);
        }

        var db = _scheduleRep.Context;
        await db.AsTenant().BeginTranAsync();

        try
        {
            // Create schedule
            command.Status = CanonicalScheduleStatus(command.Status);
            command.Priority = CanonicalPriority(command.Priority);

            var schedule = new TmsMaintenanceSchedule
            {
                Name = command.Name,
                ScheduledDate = command.ScheduledDate,
                DueDate = command.DueDate,
                Priority = command.Priority,
                ReminderDaysBefore = command.ReminderDaysBefore,
                Status = command.Status,
                Remark = command.Remark
            };

            // Auto-generate Code: MS-{yyyyMMddHHmmssfff}-{random4}
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
            var randomSuffix = Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper();
            schedule.Code = $"MS-{timestamp}-{randomSuffix}";

            await _scheduleRep.InsertWithDiffLogAsync(schedule);

            // Insert equipments
            if (command.Equipments != null && command.Equipments.Any())
            {
                var equipmentIds = command.Equipments.Select(e => e.EquipmentId).Distinct().ToList();
                var tmsEquipments = await _tmsEquipmentRep.AsQueryable()
                    .Where(e => equipmentIds.Contains(e.ID))
                    .ToListAsync();

                if (tmsEquipments.Count != equipmentIds.Count)
                    throw Oops.Oh(BaseMsg.TmsMaintenanceSchedule.Exception.EquipmentNotFound);

                var equipmentById = tmsEquipments.ToDictionary(e => e.ID, e => e);

                var typeIds = tmsEquipments.Select(e => e.EquipmentTypeId).Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
                var types = await _equipmentTypeRep.AsQueryable()
                    .Where(t => typeIds.Contains(t.ID))
                    .ToListAsync();
                var typeNameById = types.ToDictionary(t => t.ID, t => t.Name);

                var equipments = new List<TmsMaintenanceScheduleEquipment>();
                foreach (var eq in command.Equipments)
                {
                    if (!equipmentById.TryGetValue(eq.EquipmentId, out var tmsEquipment))
                        throw Oops.Oh(BaseMsg.TmsMaintenanceSchedule.Exception.EquipmentNotFound);

                    var typeId = tmsEquipment.EquipmentTypeId;
                    var typeName = (!string.IsNullOrEmpty(typeId) && typeNameById.TryGetValue(typeId, out var tmpName))
                        ? tmpName
                        : null;

                    equipments.Add(new TmsMaintenanceScheduleEquipment
                    {
                        MaintenanceScheduleId = schedule.ID,
                        EquipmentId = eq.EquipmentId,
                        MaintenanceGuideId = eq.MaintenanceGuideId,
                        Status = MaintenanceConst.EquipmentStatus.Pending,
                        Remark = eq.Remark,

                        EquipmentTypeIdSnapshot = typeId,
                        EquipmentTypeNameSnapshot = typeName,
                        EquipmentStateSnapshot = tmsEquipment.EquipmentState,

                        IsChecked = false,
                        CheckedAt = null,
                        CheckedBy = null,
                        CheckedNote = null
                    });
                }
                await _equipmentRep.InsertRangeAsync(equipments);
            }

            // Insert tasks
            if (command.Tasks != null && command.Tasks.Any())
            {
                var tasks = command.Tasks.Select(t => new TmsMaintenanceTask
                {
                    MaintenanceScheduleId = schedule.ID,
                    ScheduleEquipmentId = t.ScheduleEquipmentId,
                    AssignedUserId = t.AssignedUserId,
                    TaskName = t.TaskName,
                    Description = t.Description,
                    Role = CanonicalRole(t.Role),
                    Status = MaintenanceConst.TaskStatus.Pending,
                    Remark = t.Remark
                }).ToList();
                await _taskRep.InsertRangeAsync(tasks);
            }

            // Log history
            await LogHistory(
                schedule.ID,
                MaintenanceConst.HistoryActionType.Create,
                username,
                null,
                null,
                System.Text.Json.JsonSerializer.Serialize(schedule),
                _localizer[BaseMsg.TmsMaintenanceSchedule.Message.HistoryCreated, schedule.Name, schedule.Code],
                null
            );

            await db.AsTenant().CommitTranAsync();
            _cache.RemoveByPrefixKey(CachePrefix);
        }
        catch
        {
            await db.AsTenant().RollbackTranAsync();
            throw;
        }
    }

    /// <summary>
    /// Cập nhật lịch bảo trì
    /// </summary>
    public async Task HandleAsync(UpdateMaintenanceScheduleInput command)
    {
        var username = _user.Account ?? "system";

        var entity = await _scheduleRep.GetByIdAsync(command.ID)
                     ?? throw Oops.Oh(BaseMsg.TmsMaintenanceSchedule.Exception.NotFound);

        // Không cho sửa nếu đã Completed hoặc Cancelled
        if (IsScheduleCompletedOrCancelled(entity.Status))
            throw Oops.Oh(BaseMsg.TmsMaintenanceSchedule.Exception.CannotUpdateCompletedOrCancelled);

        var oldData = System.Text.Json.JsonSerializer.Serialize(entity);

        var scheduledDateServer = BaseUtil.TimeServer.GetTimeServer(command.ScheduledDate);
        if (scheduledDateServer.HasValue)
            command.ScheduledDate = scheduledDateServer.Value;
        if (command.DueDate.HasValue)
        {
            var dueDateServer = BaseUtil.TimeServer.GetTimeServer(command.DueDate.Value);
            if (dueDateServer.HasValue)
                command.DueDate = dueDateServer.Value;
            if (command.DueDate < command.ScheduledDate)
                throw Oops.Oh(BaseMsg.TmsMaintenanceSchedule.Validation.DueDateBeforeScheduledDate);
        }

        var db = _scheduleRep.Context;
        await db.AsTenant().BeginTranAsync();

        try
        {
            // Update schedule info
            entity.Name = command.Name;
            entity.ScheduledDate = command.ScheduledDate;
            entity.DueDate = command.DueDate;
            entity.Priority = CanonicalPriority(command.Priority);
            entity.ReminderDaysBefore = command.ReminderDaysBefore;
            entity.Status = CanonicalScheduleStatus(command.Status);
            entity.Remark = command.Remark;

            await _scheduleRep.UpdateWithDiffLogAsync(entity);

            // Update equipments: Delete all and re-insert
            if (command.Equipments != null)
            {
                await _equipmentRep.DeleteAsync(e => e.MaintenanceScheduleId == command.ID);

                if (command.Equipments.Any())
                {
                    var equipmentIds = command.Equipments.Select(e => e.EquipmentId).Distinct().ToList();
                    var tmsEquipments = await _tmsEquipmentRep.AsQueryable()
                        .Where(e => equipmentIds.Contains(e.ID))
                        .ToListAsync();

                    if (tmsEquipments.Count != equipmentIds.Count)
                        throw Oops.Oh(BaseMsg.TmsMaintenanceSchedule.Exception.EquipmentNotFound);

                    var equipmentById = tmsEquipments.ToDictionary(e => e.ID, e => e);

                    var typeIds = tmsEquipments.Select(e => e.EquipmentTypeId).Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
                    var types = await _equipmentTypeRep.AsQueryable()
                        .Where(t => typeIds.Contains(t.ID))
                        .ToListAsync();
                    var typeNameById = types.ToDictionary(t => t.ID, t => t.Name);

                    var equipments = new List<TmsMaintenanceScheduleEquipment>();
                    foreach (var eq in command.Equipments)
                    {
                        if (!equipmentById.TryGetValue(eq.EquipmentId, out var tmsEquipment))
                            throw Oops.Oh(BaseMsg.TmsMaintenanceSchedule.Exception.EquipmentNotFound);

                        var typeId = tmsEquipment.EquipmentTypeId;
                        var typeName = (!string.IsNullOrEmpty(typeId) && typeNameById.TryGetValue(typeId, out var tmpName))
                            ? tmpName
                            : null;

                        equipments.Add(new TmsMaintenanceScheduleEquipment
                        {
                            MaintenanceScheduleId = command.ID,
                            EquipmentId = eq.EquipmentId,
                            MaintenanceGuideId = eq.MaintenanceGuideId,
                            Status = MaintenanceConst.EquipmentStatus.Pending,
                            Remark = eq.Remark,

                            EquipmentTypeIdSnapshot = typeId,
                            EquipmentTypeNameSnapshot = typeName,
                            EquipmentStateSnapshot = tmsEquipment.EquipmentState,

                            IsChecked = false,
                            CheckedAt = null,
                            CheckedBy = null,
                            CheckedNote = null
                        });
                    }
                    await _equipmentRep.InsertRangeAsync(equipments);
                }
            }

            // Update tasks: Delete all and re-insert
            if (command.Tasks != null)
            {
                await _taskRep.DeleteAsync(t => t.MaintenanceScheduleId == command.ID);

                if (command.Tasks.Any())
                {
                    var tasks = command.Tasks.Select(t => new TmsMaintenanceTask
                    {
                        MaintenanceScheduleId = command.ID,
                        ScheduleEquipmentId = t.ScheduleEquipmentId,
                        AssignedUserId = t.AssignedUserId,
                        TaskName = t.TaskName,
                        Description = t.Description,
                        Role = CanonicalRole(t.Role),
                        Status = MaintenanceConst.TaskStatus.Pending,
                        Remark = t.Remark
                    }).ToList();
                    await _taskRep.InsertRangeAsync(tasks);
                }
            }

            // Log history
            await LogHistory(
                command.ID,
                MaintenanceConst.HistoryActionType.Update,
                username,
                null,
                oldData,
                System.Text.Json.JsonSerializer.Serialize(entity),
                _localizer[BaseMsg.TmsMaintenanceSchedule.Message.HistoryUpdated, entity.Name],
                null
            );

            await db.AsTenant().CommitTranAsync();
            _cache.RemoveByPrefixKey(CachePrefix);
        }
        catch
        {
            await db.AsTenant().RollbackTranAsync();
            throw;
        }
    }

    /// <summary>
    /// Xóa lịch bảo trì
    /// </summary>
    public async Task HandleAsync(DeleteMaintenanceScheduleInput command)
    {
        var username = _user.Account ?? "system";

        var entity = await _scheduleRep.GetByIdAsync(command.ID)
                     ?? throw Oops.Oh(BaseMsg.TmsMaintenanceSchedule.Exception.NotFound);

        // Chỉ cho phép xóa khi ở trạng thái Register
        var currentStatus = CanonicalScheduleStatus(entity.Status);
        if (currentStatus != MaintenanceConst.ScheduleStatus.Register)
            throw Oops.Oh(BaseMsg.TmsMaintenanceSchedule.Exception.CannotDeleteActiveSchedule);

        var db = _scheduleRep.Context;
        await db.AsTenant().BeginTranAsync();

        try
        {
            // Soft delete related records
            await _equipmentRep.SoftDeleteAsync<TmsMaintenanceScheduleEquipment>(e => e.MaintenanceScheduleId == command.ID);
            await _taskRep.SoftDeleteAsync<TmsMaintenanceTask>(t => t.MaintenanceScheduleId == command.ID);
            await _scheduleRep.SoftDeleteAsync<TmsMaintenanceSchedule>(s => s.ID == command.ID);

            // Log history
            await LogHistory(
                command.ID,
                MaintenanceConst.HistoryActionType.Delete,
                username,
                null,
                System.Text.Json.JsonSerializer.Serialize(entity),
                null,
                _localizer[BaseMsg.TmsMaintenanceSchedule.Message.HistoryDeleted, entity.Name, entity.Code],
                null
            );

            await db.AsTenant().CommitTranAsync();
            _cache.RemoveByPrefixKey(CachePrefix);
        }
        catch
        {
            await db.AsTenant().RollbackTranAsync();
            throw;
        }
    }

    /// <summary>
    /// Xóa nhiều lịch bảo trì
    /// </summary>
    public async Task HandleAsync(List<DeleteMaintenanceScheduleInput> commands)
    {
        foreach (var command in commands)
        {
            await HandleAsync(command);
        }
    }

    /// <summary>
    /// Cập nhật trạng thái lịch bảo trì
    /// </summary>
    public async Task HandleAsync(UpdateMaintenanceScheduleStatusInput command)
    {
        var username = _user.Account ?? "system";

        var entity = await _scheduleRep.GetByIdAsync(command.ID)
                     ?? throw Oops.Oh(BaseMsg.TmsMaintenanceSchedule.Exception.NotFound);

        var oldStatus = entity.Status;

        var normalizedOldStatus = CanonicalScheduleStatus(oldStatus);
        var normalizedNewStatus = CanonicalScheduleStatus(command.Status);

        // Validate status transition
        ValidateStatusTransition(normalizedOldStatus, normalizedNewStatus);

        entity.Status = normalizedNewStatus;

        if (normalizedNewStatus == MaintenanceConst.ScheduleStatus.Finish)
        {
            entity.CompletedDate = DateTime.Now;
            entity.Result = command.Result;
        }

        await _scheduleRep.UpdateWithDiffLogAsync(entity);

        // Log history
        await LogHistory(
            command.ID,
            MaintenanceConst.HistoryActionType.StatusChange,
            username,
            null,
            System.Text.Json.JsonSerializer.Serialize(new { Status = normalizedOldStatus }),
            System.Text.Json.JsonSerializer.Serialize(new { Status = normalizedNewStatus }),
            _localizer[BaseMsg.TmsMaintenanceSchedule.Message.HistoryStatusChanged, normalizedOldStatus, normalizedNewStatus],
            command.Reason
        );

        _cache.RemoveByPrefixKey(CachePrefix);
    }

    /// <summary>
    /// Thêm thiết bị vào lịch bảo trì
    /// </summary>
    public async Task HandleAsync(AddEquipmentToScheduleInput command)
    {
        var username = _user.Account ?? "system";

        var schedule = await _scheduleRep.GetByIdAsync(command.MaintenanceScheduleId)
                       ?? throw Oops.Oh(BaseMsg.TmsMaintenanceSchedule.Exception.NotFound);

        // Validate equipment exists + load snapshot
        var tmsEquipment = await _tmsEquipmentRep.GetByIdAsync(command.EquipmentId);
        if (tmsEquipment == null)
            throw Oops.Oh(BaseMsg.TmsMaintenanceSchedule.Exception.EquipmentNotFound);

        string? typeName = null;
        if (!string.IsNullOrEmpty(tmsEquipment.EquipmentTypeId))
        {
            var type = await _equipmentTypeRep.GetByIdAsync(tmsEquipment.EquipmentTypeId);
            typeName = type?.Name;
        }

        // Check duplicate
        var duplicateExists = await _equipmentRep.IsAnyAsync(e =>
            e.MaintenanceScheduleId == command.MaintenanceScheduleId &&
            e.EquipmentId == command.EquipmentId);
        if (duplicateExists)
            throw Oops.Oh(BaseMsg.TmsMaintenanceSchedule.Exception.EquipmentAlreadyInSchedule);

        var equipment = new TmsMaintenanceScheduleEquipment
        {
            MaintenanceScheduleId = command.MaintenanceScheduleId,
            EquipmentId = command.EquipmentId,
            MaintenanceGuideId = command.MaintenanceGuideId,
            Status = MaintenanceConst.EquipmentStatus.Pending,
            Remark = command.Remark,

            EquipmentTypeIdSnapshot = tmsEquipment.EquipmentTypeId,
            EquipmentTypeNameSnapshot = typeName,
            EquipmentStateSnapshot = tmsEquipment.EquipmentState,

            IsChecked = false,
            CheckedAt = null,
            CheckedBy = null,
            CheckedNote = null
        };

        await _equipmentRep.InsertAsync(equipment);

        // Log history
        await LogHistory(
            command.MaintenanceScheduleId,
            MaintenanceConst.HistoryActionType.AddEquipment,
            username,
            command.EquipmentId,
            null,
            System.Text.Json.JsonSerializer.Serialize(equipment),
            _localizer[BaseMsg.TmsMaintenanceSchedule.Message.HistoryAddEquipment],
            null
        );

        _cache.RemoveByPrefixKey(CachePrefix);
    }

    /// <summary>
    /// Xóa thiết bị khỏi lịch bảo trì
    /// </summary>
    public async Task HandleAsync(RemoveEquipmentFromScheduleInput command)
    {
        var username = _user.Account ?? "system";

        var equipment = await _equipmentRep.GetByIdAsync(command.ScheduleEquipmentId)
                        ?? throw Oops.Oh(BaseMsg.TmsMaintenanceSchedule.Exception.ScheduleEquipmentNotFound);

        await _equipmentRep.SoftDeleteAsync<TmsMaintenanceScheduleEquipment>(e => e.ID == command.ScheduleEquipmentId);

        // Log history
        await LogHistory(
            equipment.MaintenanceScheduleId,
            MaintenanceConst.HistoryActionType.RemoveEquipment,
            username,
            equipment.EquipmentId,
            System.Text.Json.JsonSerializer.Serialize(equipment),
            null,
            _localizer[BaseMsg.TmsMaintenanceSchedule.Message.HistoryRemoveEquipment],
            command.Reason
        );

        _cache.RemoveByPrefixKey(CachePrefix);
    }

    /// <summary>
    /// Cập nhật trạng thái thiết bị trong lịch
    /// </summary>
    public async Task HandleAsync(UpdateEquipmentStatusInput command)
    {
        var username = _user.Account ?? "system";

        var equipment = await _equipmentRep.GetByIdAsync(command.ScheduleEquipmentId)
                        ?? throw Oops.Oh(BaseMsg.TmsMaintenanceSchedule.Exception.ScheduleEquipmentNotFound);

        var schedule = await _scheduleRep.GetByIdAsync(equipment.MaintenanceScheduleId)
                       ?? throw Oops.Oh(BaseMsg.TmsMaintenanceSchedule.Exception.NotFound);

        if (!IsScheduleInProgress(schedule.Status))
            throw Oops.Oh(BaseMsg.TmsMaintenanceSchedule.Validation.ScheduleMustBeInProgress);

        var oldStatus = equipment.Status;

        equipment.Status = CanonicalEquipmentStatus(command.Status);
        equipment.Result = command.Result;
        equipment.Remark = command.Remark;

        if (equipment.Status == MaintenanceConst.EquipmentStatus.Completed)
        {
            equipment.CompletedDate = DateTime.Now;
            equipment.CompletedBy = username;
        }

        if (command.CompletedChecklist != null)
        {
            equipment.CompletedChecklist = JsonConvert.SerializeObject(command.CompletedChecklist);
        }

        await _equipmentRep.UpdateAsync(equipment);

        // Log history
        await LogHistory(
            equipment.MaintenanceScheduleId,
            MaintenanceConst.HistoryActionType.UpdateEquipmentStatus,
            username,
            equipment.EquipmentId,
            System.Text.Json.JsonSerializer.Serialize(new { Status = oldStatus }),
            System.Text.Json.JsonSerializer.Serialize(new { Status = equipment.Status, equipment.Result, equipment.Remark }),
            _localizer[BaseMsg.TmsMaintenanceSchedule.Message.HistoryEquipmentStatusChanged, oldStatus, equipment.Status],
            command.Remark
        );

        // Check if all equipments are completed → auto complete schedule
        await CheckAndAutoCompleteSchedule(equipment.MaintenanceScheduleId, username);

        _cache.RemoveByPrefixKey(CachePrefix);
    }

    /// <summary>
    /// Xác nhận đã kiểm tra thiết bị trong lịch
    /// </summary>
    public async Task HandleAsync(ConfirmEquipmentCheckedInput command)
    {
        var username = _user.Account ?? "system";

        var equipment = await _equipmentRep.GetByIdAsync(command.ScheduleEquipmentId)
                        ?? throw Oops.Oh(BaseMsg.TmsMaintenanceSchedule.Exception.ScheduleEquipmentNotFound);

        var schedule = await _scheduleRep.GetByIdAsync(equipment.MaintenanceScheduleId)
                       ?? throw Oops.Oh(BaseMsg.TmsMaintenanceSchedule.Exception.NotFound);

        if (!IsScheduleInProgress(schedule.Status))
            throw Oops.Oh(BaseMsg.TmsMaintenanceSchedule.Validation.EquipmentCheckRequiresInProgress);

        var oldData = JsonConvert.SerializeObject(new
        {
            equipment.Status,
            equipment.IsChecked,
            equipment.CheckedAt,
            equipment.CheckedBy,
            equipment.CheckedNote,
            equipment.CompletedDate,
            equipment.CompletedBy
        });

        var checkStatus = command.Status?.Trim().ToLowerInvariant();
        if (!string.IsNullOrEmpty(checkStatus))
        {
            switch (checkStatus)
            {
                case "pending":
                    equipment.Status = MaintenanceConst.EquipmentStatus.Pending;
                    equipment.IsChecked = null;
                    equipment.CheckedAt = null;
                    equipment.CheckedBy = null;
                    equipment.CheckedNote = null;
                    equipment.CompletedDate = null;
                    equipment.CompletedBy = null;
                    break;
                case "checked":
                    equipment.Status = MaintenanceConst.EquipmentStatus.Completed;
                    equipment.IsChecked = true;
                    equipment.CheckedAt = DateTime.Now;
                    equipment.CheckedBy = username;
                    equipment.CheckedNote = command.CheckedNote;
                    equipment.CompletedDate = DateTime.Now;
                    equipment.CompletedBy = username;
                    break;
                case "skipped":
                    equipment.Status = MaintenanceConst.EquipmentStatus.Skipped;
                    equipment.IsChecked = false;
                    equipment.CheckedAt = DateTime.Now;
                    equipment.CheckedBy = username;
                    equipment.CheckedNote = command.CheckedNote;
                    equipment.CompletedDate = DateTime.Now;
                    equipment.CompletedBy = username;
                    break;
                default:
                    throw Oops.Oh(BaseMsg.TmsMaintenanceScheduleEquipment.Validation.StatusInvalid);
            }
        }
        else
        {
            // Backward-compatible behavior: infer status from IsChecked
            equipment.IsChecked = command.IsChecked;
            if (command.IsChecked)
            {
                equipment.Status = MaintenanceConst.EquipmentStatus.Completed;
                equipment.CheckedAt = DateTime.Now;
                equipment.CheckedBy = username;
                equipment.CheckedNote = command.CheckedNote;
                equipment.CompletedDate = DateTime.Now;
                equipment.CompletedBy = username;
            }
            else
            {
                equipment.Status = MaintenanceConst.EquipmentStatus.Pending;
                equipment.CheckedAt = null;
                equipment.CheckedBy = null;
                equipment.CheckedNote = null;
                equipment.CompletedDate = null;
                equipment.CompletedBy = null;
            }
        }

        await _equipmentRep.UpdateAsync(equipment);

        // Check if all equipments are completed/skipped → auto complete schedule
        await CheckAndAutoCompleteSchedule(equipment.MaintenanceScheduleId, username);

        await LogHistory(
            equipment.MaintenanceScheduleId,
            MaintenanceConst.HistoryActionType.ConfirmEquipmentChecked,
            username,
            equipment.EquipmentId,
            oldData,
            JsonConvert.SerializeObject(new
            {
                equipment.Status,
                equipment.IsChecked,
                equipment.CheckedAt,
                equipment.CheckedBy,
                equipment.CheckedNote,
                equipment.CompletedDate,
                equipment.CompletedBy
            }),
            equipment.Status switch
            {
                MaintenanceConst.EquipmentStatus.Completed => _localizer[BaseMsg.TmsMaintenanceSchedule.Message.HistoryEquipmentChecked],
                MaintenanceConst.EquipmentStatus.Skipped => _localizer[BaseMsg.TmsMaintenanceSchedule.Message.HistoryEquipmentSkipped],
                _ => _localizer[BaseMsg.TmsMaintenanceSchedule.Message.HistoryEquipmentPending]
            },
            command.CheckedNote
        );

        _cache.RemoveByPrefixKey(CachePrefix);
    }

    /// <summary>
    /// Thêm công việc/phân công vào lịch
    /// </summary>
    public async Task HandleAsync(AddTaskToScheduleInput command)
    {
        var username = _user.Account ?? "system";

        var schedule = await _scheduleRep.GetByIdAsync(command.MaintenanceScheduleId)
                       ?? throw Oops.Oh(BaseMsg.TmsMaintenanceSchedule.Exception.NotFound);

        var task = new TmsMaintenanceTask
        {
            MaintenanceScheduleId = command.MaintenanceScheduleId,
            ScheduleEquipmentId = command.ScheduleEquipmentId,
            AssignedUserId = command.AssignedUserId,
            TaskName = command.TaskName,
            Description = command.Description,
            Role = CanonicalRole(command.Role),
            Status = MaintenanceConst.TaskStatus.Pending,
            Remark = command.Remark
        };

        await _taskRep.InsertAsync(task);

        // Log history
        await LogHistory(
            command.MaintenanceScheduleId,
            MaintenanceConst.HistoryActionType.AssignTask,
            username,
            null,
            null,
            System.Text.Json.JsonSerializer.Serialize(task),
            _localizer[BaseMsg.TmsMaintenanceSchedule.Message.HistoryAssignTask],
            null,
            command.AssignedUserId
        );

        _cache.RemoveByPrefixKey(CachePrefix);
    }

    /// <summary>
    /// Xóa công việc/phân công khỏi lịch
    /// </summary>
    public async Task HandleAsync(RemoveTaskFromScheduleInput command)
    {
        var username = _user.Account ?? "system";

        var task = await _taskRep.GetByIdAsync(command.TaskId)
                   ?? throw Oops.Oh(BaseMsg.TmsMaintenanceSchedule.Exception.TaskNotFound);

        await _taskRep.SoftDeleteAsync<TmsMaintenanceTask>(t => t.ID == command.TaskId);

        // Log history
        await LogHistory(
            task.MaintenanceScheduleId,
            MaintenanceConst.HistoryActionType.RemoveTask,
            username,
            null,
            System.Text.Json.JsonSerializer.Serialize(task),
            null,
            _localizer[BaseMsg.TmsMaintenanceSchedule.Message.HistoryRemoveTask],
            command.Reason,
            task.AssignedUserId
        );

        _cache.RemoveByPrefixKey(CachePrefix);
    }

    /// <summary>
    /// Cập nhật trạng thái công việc
    /// </summary>
    public async Task HandleAsync(UpdateTaskStatusInput command)
    {
        var username = _user.Account ?? "system";

        var task = await _taskRep.GetByIdAsync(command.TaskId)
                   ?? throw Oops.Oh(BaseMsg.TmsMaintenanceSchedule.Exception.TaskNotFound);

        var schedule = await _scheduleRep.GetByIdAsync(task.MaintenanceScheduleId)
                       ?? throw Oops.Oh(BaseMsg.TmsMaintenanceSchedule.Exception.NotFound);

        if (!IsScheduleInProgress(schedule.Status))
            throw Oops.Oh(BaseMsg.TmsMaintenanceSchedule.Validation.TaskStatusRequiresInProgress);

        var oldStatus = task.Status;
        string newStatus;
        try
        {
            newStatus = CanonicalTaskStatusOrThrow(command.Status);
        }
        catch (ArgumentException ex)
        {
            throw Oops.Oh(BaseMsg.TmsMaintenanceSchedule.Validation.TaskStatusInvalid, ex);
        }

        task.Status = newStatus;
        task.Result = command.Result;
        task.Remark = command.Remark;

        if (newStatus == MaintenanceConst.TaskStatus.Pending)
        {
            // reset timestamps when returning to pending
            task.StartTime = null;
            task.EndTime = null;
        }
        else
        {
            // mark started on first non-pending
            if (!task.StartTime.HasValue)
                task.StartTime = DateTime.Now;

            // mark ended when completed or skipped
            if (newStatus == MaintenanceConst.TaskStatus.Completed || newStatus == MaintenanceConst.TaskStatus.Skipped)
                task.EndTime = DateTime.Now;
        }

        await _taskRep.UpdateAsync(task);

        // Log history
        await LogHistory(
            task.MaintenanceScheduleId,
            MaintenanceConst.HistoryActionType.CompleteTask,
            username,
            null,
            System.Text.Json.JsonSerializer.Serialize(new { Status = oldStatus }),
            System.Text.Json.JsonSerializer.Serialize(new { Status = newStatus, task.Result, task.Remark }),
            _localizer[BaseMsg.TmsMaintenanceSchedule.Message.HistoryTaskStatusChanged, oldStatus, newStatus],
            command.Remark,
            task.AssignedUserId
        );

        _cache.RemoveByPrefixKey(CachePrefix);
    }

    #region Private Methods

    private static string CanonicalScheduleStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return MaintenanceConst.ScheduleStatus.Register;

        var value = status.Trim();

        if (ScheduleStatusRegisterVariants.Contains(value)) return MaintenanceConst.ScheduleStatus.Register;
        if (ScheduleStatusConfirmVariants.Contains(value)) return MaintenanceConst.ScheduleStatus.Confirm;
        if (ScheduleStatusProcessVariants.Contains(value)) return MaintenanceConst.ScheduleStatus.Process;
        if (ScheduleStatusFinishedVariants.Contains(value)) return MaintenanceConst.ScheduleStatus.Finish;
        if (ScheduleStatusCancelledVariants.Contains(value)) return MaintenanceConst.ScheduleStatus.Cancelled;
        if (ScheduleStatusOverdueVariants.Contains(value)) return MaintenanceConst.ScheduleStatus.Overdue;

        return value;
    }

    private static bool IsScheduleInProgress(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return false;
        var value = status.Trim();
        return ScheduleStatusProcessVariants.Contains(value) || ScheduleStatusOverdueVariants.Contains(value);
    }

    private static bool IsScheduleCompletedOrCancelled(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return false;

        var value = status.Trim();
        return ScheduleStatusFinishedVariants.Contains(value) || ScheduleStatusCancelledVariants.Contains(value);
    }

    private static string CanonicalPriority(string? priority)
    {
        if (string.IsNullOrWhiteSpace(priority)) return MaintenanceConst.Priority.Medium;

        var value = priority.Trim();
        if (PriorityHighVariants.Contains(value)) return MaintenanceConst.Priority.High;
        if (PriorityMediumVariants.Contains(value)) return MaintenanceConst.Priority.Medium;
        if (PriorityLowVariants.Contains(value)) return MaintenanceConst.Priority.Low;

        return value;
    }

    private static string CanonicalRole(string? role)
    {
        if (string.IsNullOrWhiteSpace(role)) return MaintenanceConst.Role.Member;

        var value = role.Trim();
        if (RoleLeaderVariants.Contains(value)) return MaintenanceConst.Role.Leader;
        if (RoleMemberVariants.Contains(value)) return MaintenanceConst.Role.Member;

        return MaintenanceConst.Role.Member;
    }

    private static string CanonicalEquipmentStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return MaintenanceConst.EquipmentStatus.Pending;

        var value = status.Trim();

        if (EquipmentStatusCompletedVariants.Contains(value)) return MaintenanceConst.EquipmentStatus.Completed;
        if (EquipmentStatusSkippedVariants.Contains(value)) return MaintenanceConst.EquipmentStatus.Skipped;

        if (string.Equals(value, MaintenanceConst.EquipmentStatus.Pending, StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, MaintenanceConst.EquipmentStatus.LegacyPending, StringComparison.OrdinalIgnoreCase))
            return MaintenanceConst.EquipmentStatus.Pending;

        if (string.Equals(value, MaintenanceConst.EquipmentStatus.InProgress, StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, MaintenanceConst.EquipmentStatus.LegacyInProgress, StringComparison.OrdinalIgnoreCase))
            return MaintenanceConst.EquipmentStatus.InProgress;

        return value;
    }

    private static string CanonicalTaskStatusOrThrow(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return MaintenanceConst.TaskStatus.Pending;

        var value = status.Trim();

        if (TaskStatusPendingVariants.Contains(value)) return MaintenanceConst.TaskStatus.Pending;
        if (TaskStatusCompletedVariants.Contains(value)) return MaintenanceConst.TaskStatus.Completed;
        if (TaskStatusSkippedVariants.Contains(value)) return MaintenanceConst.TaskStatus.Skipped;

        throw new ArgumentException($"Invalid task status: '{value}'.");
    }

    private void ValidateStatusTransition(string currentStatus, string newStatus)
    {
        // Always allow cancelling from any other status
        if (newStatus == MaintenanceConst.ScheduleStatus.Cancelled && currentStatus != MaintenanceConst.ScheduleStatus.Cancelled)
            return;

        // Always allow marking overdue from non-terminal statuses
        if (newStatus == MaintenanceConst.ScheduleStatus.Overdue
            && currentStatus != MaintenanceConst.ScheduleStatus.Cancelled
            && currentStatus != MaintenanceConst.ScheduleStatus.Finish)
            return;

        // Valid transitions:
        // Register -> Confirm, Cancelled, Overdue
        // Confirm -> Process, Cancelled, Overdue
        // Process -> Finish, Cancelled, Overdue
        // Overdue -> Process, Finish, Cancelled
        // Finish -> (no transition)
        // Cancelled -> (no transition)

        var validTransitions = new Dictionary<string, List<string>>
        {
            { MaintenanceConst.ScheduleStatus.Register, new List<string> { MaintenanceConst.ScheduleStatus.Confirm, MaintenanceConst.ScheduleStatus.Cancelled, MaintenanceConst.ScheduleStatus.Overdue } },
            { MaintenanceConst.ScheduleStatus.Confirm, new List<string> { MaintenanceConst.ScheduleStatus.Process, MaintenanceConst.ScheduleStatus.Cancelled, MaintenanceConst.ScheduleStatus.Overdue } },
            { MaintenanceConst.ScheduleStatus.Process, new List<string> { MaintenanceConst.ScheduleStatus.Finish, MaintenanceConst.ScheduleStatus.Cancelled, MaintenanceConst.ScheduleStatus.Overdue } },
            { MaintenanceConst.ScheduleStatus.Overdue, new List<string> { MaintenanceConst.ScheduleStatus.Process, MaintenanceConst.ScheduleStatus.Finish, MaintenanceConst.ScheduleStatus.Cancelled } },
            { MaintenanceConst.ScheduleStatus.Finish, new List<string>() },
            { MaintenanceConst.ScheduleStatus.Cancelled, new List<string>() }
        };

        if (!validTransitions.ContainsKey(currentStatus) ||
            !validTransitions[currentStatus].Contains(newStatus))
        {
            throw Oops.Oh(BaseMsg.TmsMaintenanceSchedule.Validation.InvalidStatusTransition);
        }
    }

    private async Task CheckAndAutoCompleteSchedule(string scheduleId, string username)
    {
        var allEquipments = await _equipmentRep.AsQueryable()
            .Where(e => e.MaintenanceScheduleId == scheduleId)
            .ToListAsync();

        if (allEquipments.All(e =>
                !string.IsNullOrEmpty(e.Status) &&
                (EquipmentStatusCompletedVariants.Contains(e.Status) || EquipmentStatusSkippedVariants.Contains(e.Status))))
        {
            var schedule = await _scheduleRep.GetByIdAsync(scheduleId);
            if (schedule != null && IsScheduleInProgress(schedule.Status))
            {
                schedule.Status = MaintenanceConst.ScheduleStatus.Finish;
                schedule.CompletedDate = DateTime.Now;
                await _scheduleRep.UpdateAsync(schedule);

                // Log history
                await LogHistory(
                    scheduleId,
                    MaintenanceConst.HistoryActionType.StatusChange,
                    username,
                    null,
                    null,
                    System.Text.Json.JsonSerializer.Serialize(new { Status = MaintenanceConst.ScheduleStatus.Finish }),
                    _localizer[BaseMsg.TmsMaintenanceSchedule.Message.HistoryStatusChanged, MaintenanceConst.ScheduleStatus.Process, MaintenanceConst.ScheduleStatus.Finish],
                    null
                );
            }
        }
    }

    private async Task LogHistory(
        string scheduleId,
        string actionType,
        string actionBy,
        string? affectedEquipmentId,
        string? oldData,
        string? newData,
        string? description,
        string? reason,
        string? affectedUserId = null)
    {
        var history = new TmsMaintenanceHistory
        {
            MaintenanceScheduleId = scheduleId,
            ActionType = actionType,
            ActionBy = actionBy,
            AffectedEquipmentId = affectedEquipmentId,
            AffectedUserId = affectedUserId,
            OldData = oldData,
            NewData = newData,
            Description = description,
            Reason = reason
        };

        await _historyRep.InsertAsync(history);
    }

    #endregion
}
