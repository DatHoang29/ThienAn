using Microsoft.Extensions.Localization;
using Modules.TMS.Controllers.DutySchedule.Dto;
using Modules.TMS.Core.Constants;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Exceptions;
using Modules.TMS.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;
using Shared.Utility.Utilities;

namespace Modules.TMS.Controllers.DutySchedule.Commands
{
    /// <summary>
    /// Author: System
    /// Description: Handler xử lý các lệnh của DutySchedule
    /// Created date: 07/11/2025
    /// </summary>
    public class DutyScheduleCommandHandler : IWolverineHandler
    {
        private BaseRepository<TmsDutySchedule> _dutyScheduleRep;
        private BaseRepository<TmsDutyScheduleDetail> _detailRep;
        private BaseRepository<TmsDutyScheduleHistory> _historyRep;
        private BaseRepository<TmsPatrolTeam> _patrolTeamRep;
        private UserManager _user;
        private BaseCacheService _cache;
        private IStringLocalizer _localizer;

        public DutyScheduleCommandHandler(
            BaseRepository<TmsDutySchedule> dutyScheduleRep,
            BaseRepository<TmsDutyScheduleDetail> detailRep,
            BaseRepository<TmsDutyScheduleHistory> historyRep,
            BaseRepository<TmsPatrolTeam> patrolTeamRep,
            UserManager user,
            BaseCacheService cache,
            IStringLocalizer localizer)
        {
            _dutyScheduleRep = dutyScheduleRep;
            _detailRep = detailRep;
            _historyRep = historyRep;
            _patrolTeamRep = patrolTeamRep;
            _user = user;
            _cache = cache;
            _localizer = localizer;
        }

        /// <summary>
        /// Author: System
        /// Description: Thêm mới lịch trực
        /// Created date: 07/11/2025
        /// </summary>
        public async Task HandleAsync(AddDutyScheduleInput command)
        {
            command.Code = command.Code?.Trim(); // ChuongLP - 02/04/2026 - Xử lý khoảng trắng ở đầu/cuối nếu có

            var username = _user.Account ?? "system";

            command.EndTime = (DateTime)BaseUtil.TimeServer.GetTimeServer(command.EndTime);
            command.StartTime = (DateTime)BaseUtil.TimeServer.GetTimeServer(command.StartTime);
            // Validate EndTime > StartTime
            if (command.EndTime <= command.StartTime)
                throw Oops.Oh(BaseMsg.TmsDutySchedule.Validation.EndTimeBeforeStartTime);

            // Validate PatrolTeam exists
            if (!string.IsNullOrEmpty(command.PatrolTeamId))
            {
                var patrolTeamExists = await _patrolTeamRep.IsAnyAsync(u => u.ID == command.PatrolTeamId);
                if (!patrolTeamExists)
                    throw Oops.Oh(BaseMsg.TmsDutySchedule.Exception.PatrolTeamNotFound);
            }

            command.CreateUId = null;

            // Begin transaction
            var db = _dutyScheduleRep.Context;
            await db.AsTenant().BeginTranAsync();

            try
            {
                // Insert schedule
                var schedule = command.Adapt<TmsDutySchedule>();

                // Auto-generate Code theo format: DS-yyyyMMddHHmmssfff-XXXX (unique với milliseconds + random)
                var timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                var randomSuffix = Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper();
                schedule.Code = $"DS-{timestamp}-{randomSuffix}";

                await _dutyScheduleRep.InsertWithDiffLogAsync(schedule);

                // Insert details
                if (command.Details != null && command.Details.Any())
                {
                    var details = command.Details.Select(d => new TmsDutyScheduleDetail
                    {
                        DutyScheduleId = schedule.ID,
                        UserId = d.UserId,  // UserId trong DTO chứa username
                        Role = d.Role ?? DutyScheduleConst.Role.Member,
                        Status = d.Status ?? DutyScheduleConst.DetailStatus.Join,
                        Remark = d.Remark
                    }).ToList();

                    await _detailRep.InsertRangeAsync(details);
                }

                // Ghi lịch sử tạo mới
                await LogHistory(
                    schedule.ID,
                    null,
                    DutyScheduleConst.HistoryActionType.Create,
                    username,
                    null,
                    null,
                    System.Text.Json.JsonSerializer.Serialize(schedule),
                    _localizer[BaseMsg.TmsDutySchedule.Message.HistoryCreated, schedule.Name, schedule.Code],
                    null
                );

                await db.AsTenant().CommitTranAsync();
                _cache.RemoveByPrefixKey(CacheConst.Tms.TmsDutySchedule);
            }
            catch
            {
                await db.AsTenant().RollbackTranAsync();
                throw;
            }
        }

        /// <summary>
        /// Author: System
        /// Description: Cập nhật lịch trực
        /// Created date: 07/11/2025
        /// </summary>
        public async Task HandleAsync(UpdateDutyScheduleInput command)
        {
            var username = _user.Account ?? "system";

            command.Code = command.Code?.Trim(); // ChuongLP - 02/04/2026 - Xử lý khoảng trắng ở đầu/cuối nếu có

            command.EndTime = (DateTime)BaseUtil.TimeServer.GetTimeServer(command.EndTime);
            command.StartTime = (DateTime)BaseUtil.TimeServer.GetTimeServer(command.StartTime);
            // Validate EndTime > StartTime
            if (command.EndTime < command.StartTime)
                throw Oops.Oh(BaseMsg.TmsDutySchedule.Validation.EndTimeBeforeStartTime);

            var entity = await _dutyScheduleRep.GetByIdAsync(command.ID)
                         ?? throw Oops.Oh(BaseMsg.TmsDutySchedule.Exception.NotFound);

            // Lưu dữ liệu cũ để so sánh
            var oldName = entity.Name;
            var oldStatus = entity.Status;
            var oldStartTime = entity.StartTime;
            var oldEndTime = entity.EndTime;
            var oldPatrolTeamId = entity.PatrolTeamId;
            var oldRemark = entity.Remark;
            var oldData = System.Text.Json.JsonSerializer.Serialize(entity);

            // Lấy danh sách details hiện tại để so sánh
            var oldDetails = await _detailRep.AsQueryable()
                .Where(d => d.DutyScheduleId == command.ID)
                .OrderBy(d => d.UserId)
                .Select(d => new { d.UserId, d.Role, d.Status })
                .ToListAsync();

            // Validate PatrolTeam exists
            if (!string.IsNullOrEmpty(command.PatrolTeamId))
            {
                var patrolTeamExists = await _patrolTeamRep.IsAnyAsync(u => u.ID == command.PatrolTeamId);
                if (!patrolTeamExists)
                    throw Oops.Oh(BaseMsg.TmsDutySchedule.Exception.PatrolTeamNotFound);
            }

            var db = _dutyScheduleRep.Context;
            await db.AsTenant().BeginTranAsync();

            try
            {
                // Update schedule
                var updatedEntity = command.Adapt(entity);
                await _dutyScheduleRep.UpdateWithDiffLogAsync(updatedEntity);

                // Update details: Delete all and re-insert
                bool detailsUpdated = false;
                if (command.Details != null)
                {
                    // So sánh danh sách mới với danh sách cũ
                    var newDetails = command.Details
                        .OrderBy(d => d.UserId)
                        .Select(d => new { d.UserId, Role = d.Role ?? DutyScheduleConst.Role.Member, Status = d.Status ?? DutyScheduleConst.DetailStatus.Join })
                        .ToList();

                    // Kiểm tra xem có thay đổi không
                    var oldDetailsJson = System.Text.Json.JsonSerializer.Serialize(oldDetails);
                    var newDetailsJson = System.Text.Json.JsonSerializer.Serialize(newDetails);
                    detailsUpdated = oldDetailsJson != newDetailsJson;

                    // Luôn xóa và insert lại (để đảm bảo dữ liệu đồng bộ)
                    await _detailRep.DeleteAsync(d => d.DutyScheduleId == command.ID);

                    if (command.Details.Any())
                    {
                        var details = command.Details.Select(d => new TmsDutyScheduleDetail
                        {
                            DutyScheduleId = command.ID,
                            UserId = d.UserId,
                            Role = d.Role ?? DutyScheduleConst.Role.Member,
                            Status = d.Status ?? DutyScheduleConst.DetailStatus.Join,
                            Remark = d.Remark
                        }).ToList();

                        await _detailRep.InsertRangeAsync(details);
                    }
                }

                var newData = System.Text.Json.JsonSerializer.Serialize(updatedEntity);

                // Ghi lịch sử RIÊNG BIỆT cho từng loại thay đổi
                await LogIndividualChanges(
                    command.ID,
                    username,
                    oldName, updatedEntity.Name,
                    oldStatus, updatedEntity.Status,
                    oldStartTime, updatedEntity.StartTime,
                    oldEndTime, updatedEntity.EndTime,
                    oldPatrolTeamId, updatedEntity.PatrolTeamId,
                    oldRemark, updatedEntity.Remark,
                    detailsUpdated,
                    oldData,
                    newData
                );

                await db.AsTenant().CommitTranAsync();
                _cache.RemoveByPrefixKey(CacheConst.Tms.TmsDutySchedule);
            }
            catch
            {
                await db.AsTenant().RollbackTranAsync();
                throw;
            }
        }

        /// <summary>
        /// Author: System
        /// Description: Xóa lịch trực
        /// Created date: 07/11/2025
        /// </summary>
        public async Task HandleAsync(DeleteDutyScheduleInput command)
        {
            var username = _user.Account ?? "system";

            var entity = await _dutyScheduleRep.GetFirstAsync(u => u.ID == command.ID)
                         ?? throw Oops.Oh(BaseMsg.TmsDutySchedule.Exception.NotFound);

            var oldData = System.Text.Json.JsonSerializer.Serialize(entity);

            var db = _dutyScheduleRep.Context;
            await db.AsTenant().BeginTranAsync();

            try
            {
                // Ghi lịch sử xóa (trước khi xóa)
                await LogHistory(
                    command.ID,
                    null,
                    DutyScheduleConst.HistoryActionType.Delete,
                    username,
                    null,
                    oldData,
                    null,
                    _localizer[BaseMsg.TmsDutySchedule.Message.HistoryDeleted, entity.Name, entity.Code],
                    null
                );

                // Soft delete schedule
                await _dutyScheduleRep.SoftDeleteAsync<TmsDutySchedule>(entity);

                // Soft delete details
                await _detailRep.SoftDeleteAsync<TmsDutyScheduleDetail>(d => d.DutyScheduleId == command.ID);

                await db.AsTenant().CommitTranAsync();
                _cache.RemoveByPrefixKey(CacheConst.Tms.TmsDutySchedule);
            }
            catch
            {
                await db.AsTenant().RollbackTranAsync();
                throw;
            }
        }

        /// <summary>
        /// Author: System
        /// Description: Xóa nhiều lịch trực
        /// Created date: 07/11/2025
        /// </summary>
        public async Task HandleAsync(List<DeleteDutyScheduleInput> command)
        {
            var username = _user.Account ?? "system";
            var ids = command.Select(u => u.ID).ToList();

            var schedules = await _dutyScheduleRep.AsQueryable()
                .Where(u => ids.Contains(u.ID))
                .ToListAsync();

            if (schedules?.Count != ids.Count)
                throw Oops.Oh(BaseMsg.BaseException.DataNotMatch);

            var db = _dutyScheduleRep.Context;
            await db.AsTenant().BeginTranAsync();

            try
            {
                // Ghi lịch sử xóa cho từng lịch trực
                foreach (var schedule in schedules)
                {
                    await LogHistory(
                        schedule.ID,
                        null,
                        DutyScheduleConst.HistoryActionType.Delete,
                        username,
                        null,
                        System.Text.Json.JsonSerializer.Serialize(schedule),
                        null,
                        _localizer[BaseMsg.TmsDutySchedule.Message.HistoryDeleted, schedule.Name, schedule.Code],
                        null
                    );
                }

                await _dutyScheduleRep.SoftDeleteAsync<TmsDutySchedule>(l => ids.Contains(l.ID));
                await _detailRep.SoftDeleteAsync<TmsDutyScheduleDetail>(d => ids.Contains(d.DutyScheduleId));

                await db.AsTenant().CommitTranAsync();
                _cache.RemoveByPrefixKey(CacheConst.Tms.TmsDutySchedule);
            }
            catch
            {
                await db.AsTenant().RollbackTranAsync();
                throw;
            }
        }

        /// <summary>
        /// Author: System
        /// Description: Cập nhật trạng thái lịch trực (Draft, InProgress, Completed, Cancelled)
        /// Created date: 27/11/2025
        /// </summary>
        public async Task HandleAsync(UpdateScheduleStatusInput command)
        {
            var username = _user.Account ?? "system";

            var entity = await _dutyScheduleRep.GetByIdAsync(command.ID)
                         ?? throw Oops.Oh(BaseMsg.TmsDutySchedule.Exception.NotFound);

            // Kiểm tra quyền: phải là người tạo lịch hoặc đội trưởng
            await ValidateManagerPermission(command.ID, entity.CreateUId, username);

            var oldStatus = entity.Status;

            // Không cần validate nếu status không thay đổi
            if (oldStatus == command.Status)
                return;

            // Cập nhật trạng thái
            entity.Status = command.Status;
            await _dutyScheduleRep.UpdateAsync(entity);

            // Ghi lịch sử
            await LogHistory(
                command.ID,
                null,
                DutyScheduleConst.HistoryActionType.UpdateStatus,
                username,
                null,
                System.Text.Json.JsonSerializer.Serialize(new { Status = oldStatus }),
                System.Text.Json.JsonSerializer.Serialize(new { Status = command.Status }),
                _localizer[BaseMsg.TmsDutySchedule.Message.HistoryStatusChanged, oldStatus, command.Status],
                command.Reason
            );

            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsDutySchedule);
        }

        /// <summary>
        /// Author: System
        /// Description: Thêm người dùng vào lịch trực (chỉ đội trưởng hoặc người tạo lịch)
        /// Created date: 07/11/2025
        /// </summary>
        public async Task HandleAsync(AddUserToScheduleInput command)
        {
            var username = _user.Account ?? throw Oops.Oh(BaseMsg.TmsDutySchedule.Exception.UserNotIdentified);

            // Check schedule exists
            var schedule = await _dutyScheduleRep.GetByIdAsync(command.DutyScheduleId)
                ?? throw Oops.Oh(BaseMsg.TmsDutySchedule.Exception.NotFound);

            // Kiểm tra quyền: phải là người tạo lịch hoặc đội trưởng
            await ValidateManagerPermission(command.DutyScheduleId, schedule.CreateUId, username);

            // Check user already in schedule (với status 'join')
            var existingDetail = await _detailRep.GetFirstAsync(d =>
                d.DutyScheduleId == command.DutyScheduleId && d.UserId == command.UserId);

            if (existingDetail != null && existingDetail.Status == DutyScheduleConst.DetailStatus.Join)
                throw Oops.Oh(BaseMsg.TmsDutyScheduleDetail.Exception.AlreadyExists);

            // Nếu đã có record nhưng status khác 'join' -> update lại
            if (existingDetail != null)
            {
                var oldData = System.Text.Json.JsonSerializer.Serialize(existingDetail);
                existingDetail.Role = command.Role ?? DutyScheduleConst.Role.Member;
                existingDetail.Status = DutyScheduleConst.DetailStatus.Join;
                existingDetail.Remark = command.Remark;
                await _detailRep.UpdateAsync(existingDetail);

                // Ghi lịch sử
                await LogHistory(
                    command.DutyScheduleId,
                    null,
                    DutyScheduleConst.HistoryActionType.AddMember,
                    username,
                    command.UserId,
                    oldData,
                    System.Text.Json.JsonSerializer.Serialize(existingDetail),
                    _localizer[BaseMsg.TmsDutySchedule.Message.HistoryAddMember, command.UserId, username],
                    command.Remark
                );
            }
            else
            {
                var detail = new TmsDutyScheduleDetail
                {
                    DutyScheduleId = command.DutyScheduleId,
                    UserId = command.UserId,
                    Role = command.Role ?? DutyScheduleConst.Role.Member,
                    Status = DutyScheduleConst.DetailStatus.Join,
                    Remark = command.Remark
                };

                await _detailRep.InsertAsync(detail);

                // Ghi lịch sử
                await LogHistory(
                    command.DutyScheduleId,
                    null,
                    DutyScheduleConst.HistoryActionType.AddMember,
                    username,
                    command.UserId,
                    null,
                    System.Text.Json.JsonSerializer.Serialize(detail),
                    _localizer[BaseMsg.TmsDutySchedule.Message.HistoryAddMember, command.UserId, username],
                    command.Remark
                );
            }

            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsDutySchedule);
        }

        /// <summary>
        /// Author: System
        /// Description: Xóa người dùng khỏi lịch trực (chỉ đội trưởng hoặc người tạo lịch)
        /// Created date: 07/11/2025
        /// </summary>
        public async Task HandleAsync(RemoveUserFromScheduleInput command)
        {
            var username = _user.Account ?? throw Oops.Oh(BaseMsg.TmsDutySchedule.Exception.UserNotIdentified);

            var detail = await _detailRep.GetByIdAsync(command.DetailId)
                         ?? throw Oops.Oh(BaseMsg.TmsDutyScheduleDetail.Exception.NotFound);

            // Lấy thông tin schedule để kiểm tra quyền
            var schedule = await _dutyScheduleRep.GetByIdAsync(detail.DutyScheduleId)
                ?? throw Oops.Oh(BaseMsg.TmsDutySchedule.Exception.NotFound);

            // Kiểm tra quyền: phải là người tạo lịch hoặc đội trưởng
            await ValidateManagerPermission(detail.DutyScheduleId, schedule.CreateUId, username);

            var oldData = System.Text.Json.JsonSerializer.Serialize(detail);
            var removedUserId = detail.UserId;

            // Xóa khỏi lịch trực (hard delete)
            await _detailRep.DeleteAsync(detail);

            // Ghi lịch sử
            await LogHistory(
                detail.DutyScheduleId,
                null,
                DutyScheduleConst.HistoryActionType.RemoveMember,
                username,
                removedUserId,
                oldData,
                null,
                _localizer[BaseMsg.TmsDutySchedule.Message.HistoryRemoveMember, removedUserId, username],
                command.Reason
            );

            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsDutySchedule);
        }

        /// <summary>
        /// Author: System
        /// Description: Cập nhật vai trò người dùng (chỉ đội trưởng hoặc người tạo lịch)
        /// Created date: 07/11/2025
        /// </summary>
        public async Task HandleAsync(UpdateUserRoleInput command)
        {
            var username = _user.Account ?? throw Oops.Oh(BaseMsg.TmsDutySchedule.Exception.UserNotIdentified);

            var detail = await _detailRep.GetByIdAsync(command.DetailId)
                         ?? throw Oops.Oh(BaseMsg.TmsDutyScheduleDetail.Exception.NotFound);

            // Lấy thông tin schedule để kiểm tra quyền
            var schedule = await _dutyScheduleRep.GetByIdAsync(detail.DutyScheduleId)
                ?? throw Oops.Oh(BaseMsg.TmsDutySchedule.Exception.NotFound);

            // Kiểm tra quyền: phải là người tạo lịch hoặc đội trưởng
            await ValidateManagerPermission(detail.DutyScheduleId, schedule.CreateUId, username);

            // Validate role
            if (command.Role != DutyScheduleConst.Role.Leader && command.Role != DutyScheduleConst.Role.Member)
                throw Oops.Oh(BaseMsg.TmsDutyScheduleDetail.Validation.RoleInvalid);

            var oldData = System.Text.Json.JsonSerializer.Serialize(detail);
            var oldRole = detail.Role;

            detail.Role = command.Role;
            await _detailRep.UpdateAsync(detail);

            // Ghi lịch sử
            await LogHistory(
                detail.DutyScheduleId,
                null,
                DutyScheduleConst.HistoryActionType.ChangeRole,
                username,
                detail.UserId,
                oldData,
                System.Text.Json.JsonSerializer.Serialize(detail),
                _localizer[BaseMsg.TmsDutySchedule.Message.HistoryRoleChanged, detail.UserId, oldRole, command.Role, username],
                command.Reason
            );

            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsDutySchedule);
        }

        #region Private Methods

        /// <summary>
        /// Kiểm tra quyền quản lý lịch trực (phải là người tạo hoặc đội trưởng)
        /// </summary>
        private async Task ValidateManagerPermission(string dutyScheduleId, string? creatorId, string currentUser)
        {
            // Kiểm tra nếu là người tạo lịch
            if (creatorId == currentUser)
                return;

            // Kiểm tra nếu là đội trưởng trong lịch này
            var isLeader = await _detailRep.IsAnyAsync(d =>
                d.DutyScheduleId == dutyScheduleId &&
                d.UserId == currentUser &&
                d.Role == DutyScheduleConst.Role.Leader &&
                d.Status == DutyScheduleConst.DetailStatus.Join);

            if (!isLeader)
                throw Oops.Oh(BaseMsg.TmsDutySchedule.Exception.NoManagerPermission);
        }

        /// <summary>
        /// Ghi lịch sử RIÊNG BIỆT cho từng loại thay đổi
        /// </summary>
        private async Task LogIndividualChanges(
            string dutyScheduleId,
            string username,
            string? oldName, string? newName,
            string? oldStatus, string? newStatus,
            DateTime oldStartTime, DateTime newStartTime,
            DateTime oldEndTime, DateTime newEndTime,
            string? oldPatrolTeamId, string? newPatrolTeamId,
            string? oldRemark, string? newRemark,
            bool detailsUpdated,
            string oldData,
            string newData)
        {
            // Thay đổi tên
            if (oldName != newName)
            {
                await LogHistory(
                    dutyScheduleId, null,
                    DutyScheduleConst.HistoryActionType.UpdateName,
                    username, null, oldData, newData,
                    _localizer[BaseMsg.TmsDutySchedule.Message.HistoryNameChanged, oldName, newName],
                    null
                );
            }

            // Thay đổi trạng thái
            if (oldStatus != newStatus)
            {
                await LogHistory(
                    dutyScheduleId, null,
                    DutyScheduleConst.HistoryActionType.UpdateStatus,
                    username, null, oldData, newData,
                    _localizer[BaseMsg.TmsDutySchedule.Message.HistoryStatusChanged, GetStatusDisplay(oldStatus), GetStatusDisplay(newStatus)],
                    null
                );
            }

            // Thay đổi thời gian bắt đầu
            if (oldStartTime != newStartTime)
            {
                await LogHistory(
                    dutyScheduleId, null,
                    DutyScheduleConst.HistoryActionType.UpdateTime,
                    username, null, oldData, newData,
                    _localizer[BaseMsg.TmsDutySchedule.Message.HistoryStartTimeChanged, oldStartTime.ToString("dd/MM/yyyy HH:mm"), newStartTime.ToString("dd/MM/yyyy HH:mm")],
                    null
                );
            }

            // Thay đổi thời gian kết thúc
            if (oldEndTime != newEndTime)
            {
                await LogHistory(
                    dutyScheduleId, null,
                    DutyScheduleConst.HistoryActionType.UpdateTime,
                    username, null, oldData, newData,
                    _localizer[BaseMsg.TmsDutySchedule.Message.HistoryEndTimeChanged, oldEndTime.ToString("dd/MM/yyyy HH:mm"), newEndTime.ToString("dd/MM/yyyy HH:mm")],
                    null
                );
            }

            // Thay đổi đội tuần tra
            if (oldPatrolTeamId != newPatrolTeamId)
            {
                await LogHistory(
                    dutyScheduleId, null,
                    DutyScheduleConst.HistoryActionType.UpdateTeam,
                    username, null, oldData, newData,
                    _localizer[BaseMsg.TmsDutySchedule.Message.HistoryTeamChanged],
                    null
                );
            }

            // Thay đổi ghi chú
            if (oldRemark != newRemark)
            {
                await LogHistory(
                    dutyScheduleId, null,
                    DutyScheduleConst.HistoryActionType.UpdateRemark,
                    username, null, oldData, newData,
                    _localizer[BaseMsg.TmsDutySchedule.Message.HistoryRemarkChanged],
                    null
                );
            }

            // Cập nhật danh sách thành viên
            if (detailsUpdated)
            {
                await LogHistory(
                    dutyScheduleId, null,
                    DutyScheduleConst.HistoryActionType.UpdateMembers,
                    username, null, oldData, newData,
                    _localizer[BaseMsg.TmsDutySchedule.Message.HistoryMembersChanged],
                    null
                );
            }
        }

        /// <summary>
        /// Lấy mô tả hiển thị cho trạng thái lịch trực
        /// </summary>
        private string GetStatusDisplay(string? status)
        {
            return status switch
            {
                "Active" or "active" => _localizer[BaseMsg.TmsDutySchedule.Message.StatusActive],
                "Inactive" or "inactive" => _localizer[BaseMsg.TmsDutySchedule.Message.StatusInactive],
                "Completed" or "completed" => _localizer[BaseMsg.TmsDutySchedule.Message.StatusCompleted],
                "Cancelled" or "cancelled" => _localizer[BaseMsg.TmsDutySchedule.Message.StatusCancelled],
                "Pending" or "pending" => _localizer[BaseMsg.TmsDutySchedule.Message.StatusPending],
                _ => status ?? _localizer[BaseMsg.TmsDutySchedule.Message.StatusUnknown]
            };
        }

        /// <summary>
        /// Ghi lịch sử thay đổi
        /// </summary>
        private async Task LogHistory(
            string dutyScheduleId,
            string? changeRequestId,
            string actionType,
            string actionBy,
            string? affectedUserId,
            string? oldData,
            string? newData,
            string? description,
            string? reason,
            string? relatedScheduleId = null)
        {
            var history = new TmsDutyScheduleHistory
            {
                DutyScheduleId = dutyScheduleId,
                ChangeRequestId = changeRequestId,
                RelatedScheduleId = relatedScheduleId,
                ActionType = actionType,
                ActionBy = actionBy,
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
}
