using Microsoft.Extensions.Localization;
using Modules.TMS.Controllers.DutySchedule.Dto;
using Modules.TMS.Core.Constants;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Exceptions;
using Modules.TMS.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;

namespace Modules.TMS.Controllers.DutySchedule.Commands
{
    /// <summary>
    /// Author: System
    /// Description: Handler xử lý các lệnh thay đổi lịch trực (Change Request)
    /// Created date: 19/11/2025
    /// </summary>
    public class DutyScheduleChangeRequestCommandHandler : IWolverineHandler
    {
        private BaseRepository<TmsDutyScheduleChangeRequest> _changeRequestRep;
        private BaseRepository<TmsDutySchedule> _dutyScheduleRep;
        private BaseRepository<TmsDutyScheduleDetail> _detailRep;
        private BaseRepository<TmsDutyScheduleHistory> _historyRep;
        private UserManager _user;
        private BaseCacheService _cache;
        private readonly IStringLocalizer _localizer;

        public DutyScheduleChangeRequestCommandHandler(
            BaseRepository<TmsDutyScheduleChangeRequest> changeRequestRep,
            BaseRepository<TmsDutySchedule> dutyScheduleRep,
            BaseRepository<TmsDutyScheduleDetail> detailRep,
            BaseRepository<TmsDutyScheduleHistory> historyRep,
            UserManager user,
            BaseCacheService cache,
            IStringLocalizer localizer)
        {
            _changeRequestRep = changeRequestRep;
            _dutyScheduleRep = dutyScheduleRep;
            _detailRep = detailRep;
            _historyRep = historyRep;
            _user = user;
            _cache = cache;
            _localizer = localizer;
        }

        /// <summary>
        /// Lấy danh sách lịch trực của user cụ thể (để chọn khi Swap giữa 2 lịch)
        /// </summary>
        public async Task<List<UserScheduleOutput>> HandleAsync(GetUserSchedulesInput query)
        {
            // Get all schedules that user is participating in  
            var userDetails = await _detailRep.GetListAsync(d => d.UserId == query.UserId);

            if (query.ActiveOnly)
            {
                userDetails = userDetails.Where(d => d.Status == DutyScheduleConst.DetailStatus.Join).ToList();
            }

            if (!userDetails.Any())
            {
                return new List<UserScheduleOutput>();
            }

            var scheduleIds = userDetails.Select(d => d.DutyScheduleId).Distinct().ToList();
            var schedules = await _dutyScheduleRep.AsQueryable().ToListAsync();
            schedules = schedules.Where(s => scheduleIds.Contains(s.ID)).ToList();

            // Check time conflict with requester's schedule if provided
            TmsDutySchedule? mySchedule = null;
            if (!string.IsNullOrEmpty(query.MyScheduleId))
            {
                mySchedule = await _dutyScheduleRep.GetByIdAsync(query.MyScheduleId);
            }

            // Map to output
            var result = schedules.Select(schedule =>
            {
                var detail = userDetails.First(d => d.DutyScheduleId == schedule.ID);

                var hasConflict = false;
                var conflictReason = string.Empty;

                // Check time conflict with my schedule
                if (mySchedule != null)
                {
                    if (schedule.StartTime <= mySchedule.EndTime && schedule.EndTime >= mySchedule.StartTime)
                    {
                        hasConflict = true;
                        conflictReason = _localizer[BaseMsg.TmsDutySchedule.Message.TimeConflict];
                    }
                }

                return new UserScheduleOutput
                {
                    Schedule = schedule.Adapt<DutyScheduleOutput>(),
                    Role = detail.Role,
                    Status = detail.Status,
                    HasTimeConflict = hasConflict,
                    CanSwap = !hasConflict && detail.Status == DutyScheduleConst.DetailStatus.Join,
                    ConflictReason = conflictReason
                };
            }).OrderBy(s => s.Schedule?.StartTime).ToList();

            return result;
        }        /// <summary>
                 /// Tạo yêu cầu thay đổi lịch trực
                 /// </summary>
        public async Task HandleAsync(CreateChangeRequestInput command)
        {
            var userId = _user.Account ?? throw Oops.Oh(BaseMsg.TmsDutySchedule.Exception.UserNotIdentified);

            // Kiểm tra lịch trực tồn tại
            var schedule = await _dutyScheduleRep.GetByIdAsync(command.DutyScheduleId)
                ?? throw Oops.Oh(BaseMsg.TmsDutySchedule.Exception.NotFound);

            // Validate theo loại yêu cầu
            if (command.Type == DutyScheduleConst.ChangeRequestType.Swap)
            {
                // Swap giữa 2 lịch: BẮT BUỘC phải có TargetUserId và TargetScheduleId
                if (string.IsNullOrWhiteSpace(command.TargetUserId))
                    throw Oops.Oh(BaseMsg.TmsDutySchedule.Validation.TargetUserRequired);

                if (string.IsNullOrWhiteSpace(command.TargetScheduleId))
                    throw Oops.Oh(BaseMsg.TmsDutySchedule.Validation.TargetScheduleRequired);

                // Validate target schedule exists
                var targetSchedule = await _dutyScheduleRep.GetByIdAsync(command.TargetScheduleId);
                if (targetSchedule == null)
                    throw Oops.Oh(BaseMsg.TmsDutySchedule.Exception.TargetScheduleNotFound);

                // Check time conflict
                if (schedule.StartTime <= targetSchedule.EndTime && targetSchedule.StartTime <= schedule.EndTime)
                {
                    throw Oops.Oh(BaseMsg.TmsDutySchedule.Exception.TimeConflictCannotSwap);
                }

                // Validate target user is in target schedule
                var targetInSchedule = await _detailRep.IsAnyAsync(d =>
                    d.DutyScheduleId == command.TargetScheduleId &&
                    d.UserId == command.TargetUserId &&
                    d.Status == DutyScheduleConst.DetailStatus.Join);

                if (!targetInSchedule)
                    throw Oops.Oh(BaseMsg.TmsDutySchedule.Exception.TargetNotInSchedule);

                // Validate requester is not already in target schedule
                var requesterInTarget = await _detailRep.IsAnyAsync(d =>
                    d.DutyScheduleId == command.TargetScheduleId &&
                    d.UserId == userId);

                if (requesterInTarget)
                    throw Oops.Oh(BaseMsg.TmsDutySchedule.Exception.AlreadyInTargetSchedule);
            }
            else if (command.Type == DutyScheduleConst.ChangeRequestType.Replace)
            {
                // Replace: Bất kỳ ai cũng có thể tạo yêu cầu thay thế người trong lịch
                // - TargetUserId: Người bị thay thế (bắt buộc, phải có trong lịch với status 'join')
                // - ReplacerId: Người thay thế (tùy chọn, nếu không chọn thì dùng người đang thao tác)

                // Validate: TargetUserId là bắt buộc
                if (string.IsNullOrWhiteSpace(command.TargetUserId))
                    throw Oops.Oh(BaseMsg.TmsDutySchedule.Validation.ReplaceTargetRequired);

                // Validate: Người bị thay thế phải có trong lịch trực với status 'join'
                var targetDetail = await _detailRep.AsQueryable()
                    .Where(d => d.DutyScheduleId == command.DutyScheduleId && d.UserId == command.TargetUserId)
                    .FirstAsync();

                if (targetDetail == null)
                    throw Oops.Oh(BaseMsg.TmsDutySchedule.Exception.TargetNotInThisSchedule);

                if (targetDetail.Status != DutyScheduleConst.DetailStatus.Join)
                    throw Oops.Oh(BaseMsg.TmsDutyScheduleDetail.Validation.TargetUserStatusInvalid);

                // Xác định người thay thế: nếu không chỉ định thì dùng người đang thao tác
                var actualReplacerId = !string.IsNullOrWhiteSpace(command.ReplacerId) ? command.ReplacerId : userId;

                // Validate: không thể chọn người bị thay thế làm người thay thế
                if (actualReplacerId == command.TargetUserId)
                    throw Oops.Oh(BaseMsg.TmsDutySchedule.Exception.CannotReplaceSelf);

                // Kiểm tra người thay thế có hợp lệ không
                var replacerCheck = await CheckReplacerEligibility(actualReplacerId, command.DutyScheduleId, command.TargetUserId);
                if (replacerCheck.HasConflict)
                    throw Oops.Oh(replacerCheck.ConflictReason);

                // Lưu actualReplacerId để dùng khi tạo request
                command.ReplacerId = actualReplacerId;
            }
            else if (command.Type == DutyScheduleConst.ChangeRequestType.Transfer)
            {
                // Validate Transfer: phải có TargetScheduleId hoặc CreateNewSchedule
                if (string.IsNullOrWhiteSpace(command.TargetScheduleId) && !command.CreateNewSchedule)
                    throw Oops.Oh(BaseMsg.TmsDutySchedule.Validation.TransferTargetOrNewRequired);

                if (!string.IsNullOrWhiteSpace(command.TargetScheduleId))
                {
                    // Kiểm tra lịch trực đích tồn tại
                    var targetSchedule = await _dutyScheduleRep.GetByIdAsync(command.TargetScheduleId);
                    if (targetSchedule == null)
                        throw Oops.Oh(BaseMsg.TmsDutySchedule.Exception.TargetScheduleNotFound);

                    // Kiểm tra conflict thời gian
                    if (schedule.StartTime < targetSchedule.EndTime && targetSchedule.StartTime < schedule.EndTime)
                    {
                        // Có overlap thời gian - cảnh báo nhưng vẫn cho phép
                        // Frontend có thể hiển thị cảnh báo này
                    }

                    // Kiểm tra user đã có trong lịch đích chưa
                    var alreadyInTarget = await _detailRep.IsAnyAsync(d =>
                        d.DutyScheduleId == command.TargetScheduleId &&
                        d.UserId == userId);

                    if (alreadyInTarget)
                        throw Oops.Oh(BaseMsg.TmsDutySchedule.Exception.AlreadyInTargetSchedule);
                }
                else if (command.CreateNewSchedule)
                {
                    // Validate thông tin lịch mới
                    if (command.NewScheduleInfo == null)
                        throw Oops.Oh(BaseMsg.TmsDutySchedule.Validation.NewScheduleInfoRequired);

                    if (command.NewScheduleInfo.StartTime >= command.NewScheduleInfo.EndTime)
                        throw Oops.Oh(BaseMsg.TmsDutySchedule.Validation.EndTimeBeforeStartTime);

                    // Validate vai trò trong lịch mới
                    if (!string.IsNullOrWhiteSpace(command.NewScheduleInfo.Role))
                    {
                        if (command.NewScheduleInfo.Role != DutyScheduleConst.Role.Leader &&
                            command.NewScheduleInfo.Role != DutyScheduleConst.Role.Member)
                            throw Oops.Oh(BaseMsg.TmsDutyScheduleDetail.Validation.RoleInvalid);
                    }
                }
            }

            // Kiểm tra đã có yêu cầu pending chưa
            var hasPendingRequest = await _changeRequestRep.IsAnyAsync(r =>
                r.DutyScheduleId == command.DutyScheduleId &&
                r.RequesterId == userId &&
                r.Status == DutyScheduleConst.ChangeRequestStatus.Pending);

            if (hasPendingRequest)
                throw Oops.Oh(BaseMsg.TmsDutySchedule.Exception.PendingRequestExists);

            // Tạo yêu cầu
            var changeRequest = new TmsDutyScheduleChangeRequest
            {
                DutyScheduleId = command.DutyScheduleId,
                Type = command.Type,
                RequesterId = userId,  // Người tạo request
                TargetUserId = command.TargetUserId,  // Swap: người trao đổi, Replace: người bị thay thế
                ReplacerId = command.ReplacerId,  // Replace: người thay thế (đã được gán actualReplacerId)
                NewRole = command.NewRole,  // Vai trò mới (nếu có)
                TargetScheduleId = command.TargetScheduleId,  // ID lịch trực đích (với Transfer/Swap)
                Reason = command.Reason,
                Status = DutyScheduleConst.ChangeRequestStatus.Pending,
                Remark = command.Remark
            };

            await _changeRequestRep.InsertAsync(changeRequest);
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsDutySchedule);
        }

        /// <summary>
        /// Phê duyệt/Từ chối yêu cầu thay đổi
        /// </summary>
        public async Task HandleAsync(ApproveChangeRequestInput command)
        {
            var username = _user.Account ?? throw Oops.Oh(BaseMsg.TmsDutySchedule.Exception.UserNotIdentified);

            // Chuẩn hóa Status input (Approved -> approve, Rejected -> reject)
            var normalizedStatus = command.Status.ToLower() switch
            {
                "approved" => DutyScheduleConst.ChangeRequestStatus.Approved,
                "approve" => DutyScheduleConst.ChangeRequestStatus.Approved,
                "rejected" => DutyScheduleConst.ChangeRequestStatus.Rejected,
                "reject" => DutyScheduleConst.ChangeRequestStatus.Rejected,
                _ => command.Status
            };

            // Validate status
            if (normalizedStatus != DutyScheduleConst.ChangeRequestStatus.Approved &&
                normalizedStatus != DutyScheduleConst.ChangeRequestStatus.Rejected)
            {
                throw Oops.Oh(BaseMsg.TmsDutySchedule.Validation.ApproveStatusInvalid);
            }

            // Lấy yêu cầu
            var changeRequest = await _changeRequestRep.GetByIdAsync(command.ChangeRequestId)
                ?? throw Oops.Oh(BaseMsg.TmsDutySchedule.Exception.ChangeRequestNotFound);

            if (changeRequest.Status != DutyScheduleConst.ChangeRequestStatus.Pending)
                throw Oops.Oh(BaseMsg.TmsDutySchedule.Exception.ChangeRequestAlreadyProcessed);

            // Lấy thông tin lịch trực
            var schedule = await _dutyScheduleRep.GetByIdAsync(changeRequest.DutyScheduleId)
                ?? throw Oops.Oh(BaseMsg.TmsDutySchedule.Exception.NotFound);

            // Kiểm tra quyền phê duyệt: người tạo lịch hoặc đội trưởng của lịch
            var canApprove = schedule.CreateUId == username;

            if (!canApprove)
            {
                // Kiểm tra xem user có phải là Leader trong lịch này không
                var isLeader = await _detailRep.IsAnyAsync(d =>
                    d.DutyScheduleId == changeRequest.DutyScheduleId &&
                    d.UserId == username &&
                    d.Role == DutyScheduleConst.Role.Leader);

                canApprove = isLeader;
            }

            if (!canApprove)
                throw Oops.Oh(BaseMsg.TmsDutySchedule.Exception.ChangeRequestNoApprovePermission);

            var db = _changeRequestRep.Context;
            await db.AsTenant().BeginTranAsync();

            try
            {
                // Cập nhật trạng thái yêu cầu (sử dụng normalizedStatus)
                changeRequest.Status = normalizedStatus;
                changeRequest.ApproverId = username;  // Lưu username vào ApproverId
                changeRequest.ApprovedTime = DateTime.Now;
                changeRequest.ApprovalNote = command.ApprovalNote;

                await _changeRequestRep.UpdateAsync(changeRequest);

                // Nếu phê duyệt, cập nhật lịch trực
                if (normalizedStatus == DutyScheduleConst.ChangeRequestStatus.Approved)
                {
                    await ApplyChangeToSchedule(changeRequest);
                }

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
        /// Hủy yêu cầu thay đổi (người yêu cầu)
        /// </summary>
        public async Task HandleAsync(CancelChangeRequestInput command)
        {
            var username = _user.Account ?? throw Oops.Oh(BaseMsg.TmsDutySchedule.Exception.UserNotIdentified);

            var changeRequest = await _changeRequestRep.GetByIdAsync(command.ChangeRequestId)
                ?? throw Oops.Oh(BaseMsg.TmsDutySchedule.Exception.ChangeRequestNotFound);

            if (changeRequest.RequesterId != username)
                throw Oops.Oh(BaseMsg.TmsDutySchedule.Exception.ChangeRequestNoCancelPermission);

            if (changeRequest.Status != DutyScheduleConst.ChangeRequestStatus.Pending)
                throw Oops.Oh(BaseMsg.TmsDutySchedule.Exception.ChangeRequestOnlyPendingCancel);

            changeRequest.Status = DutyScheduleConst.ChangeRequestStatus.Cancelled;
            await _changeRequestRep.UpdateAsync(changeRequest);
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsDutySchedule);
        }

        /// <summary>
        /// Áp dụng thay đổi vào lịch trực sau khi phê duyệt
        /// </summary>
        private async Task ApplyChangeToSchedule(TmsDutyScheduleChangeRequest changeRequest)
        {
            var username = _user.Account ?? string.Empty;

            // Replace có logic riêng: TargetUserId = người bị thay thế, không phải requester
            if (changeRequest.Type == DutyScheduleConst.ChangeRequestType.Replace)
            {
                await ApplyReplaceChange(changeRequest, username);
                return;
            }

            // Transfer có logic riêng: có thể chuyển nhân sự khác (không phải requester)
            if (changeRequest.Type == DutyScheduleConst.ChangeRequestType.Transfer)
            {
                await ApplyTransferChange(changeRequest, username);
                return;
            }

            // Các loại khác (Absent, Swap): requester phải có trong lịch
            var requesterDetail = await _detailRep.GetFirstAsync(d =>
                d.DutyScheduleId == changeRequest.DutyScheduleId &&
                d.UserId == changeRequest.RequesterId);

            if (requesterDetail == null)
                throw Oops.Oh(BaseMsg.TmsDutySchedule.Exception.RequesterNotInSchedule);

            // Lưu dữ liệu cũ để ghi lịch sử
            var oldDataJson = System.Text.Json.JsonSerializer.Serialize(requesterDetail);

            switch (changeRequest.Type)
            {
                case var type when type == DutyScheduleConst.ChangeRequestType.Absent:
                    // Vắng mặt: UPDATE Status thành 'absent'
                    requesterDetail.Status = DutyScheduleConst.DetailStatus.Absent;
                    requesterDetail.Reason = changeRequest.Reason;
                    await _detailRep.UpdateAsync(requesterDetail);

                    // Ghi lịch sử
                    await LogHistory(
                        changeRequest.DutyScheduleId,
                        changeRequest.ID,
                        DutyScheduleConst.HistoryActionType.Absent,
                        username,
                        changeRequest.RequesterId,
                        oldDataJson,
                        System.Text.Json.JsonSerializer.Serialize(requesterDetail),
                        _localizer[BaseMsg.TmsDutySchedule.Message.HistoryAbsent, changeRequest.RequesterId],
                        changeRequest.Reason
                    );
                    break;

                case var type when type == DutyScheduleConst.ChangeRequestType.Swap:
                    // Trao đổi lịch trực giữa 2 người ở 2 lịch khác nhau
                    if (string.IsNullOrWhiteSpace(changeRequest.TargetUserId) || string.IsNullOrWhiteSpace(changeRequest.TargetScheduleId))
                    {
                        throw Oops.Oh(BaseMsg.TmsDutySchedule.Validation.SwapRequiresTargetAndSchedule);
                    }

                    // Get target user's detail in target schedule
                    var targetDetail = await _detailRep.GetFirstAsync(d =>
                        d.DutyScheduleId == changeRequest.TargetScheduleId &&
                        d.UserId == changeRequest.TargetUserId &&
                        d.Status == DutyScheduleConst.DetailStatus.Join);

                    if (targetDetail == null)
                    {
                        throw Oops.Oh(BaseMsg.TmsDutySchedule.Exception.TargetNotInSchedule);
                    }

                    var oldTargetJson = System.Text.Json.JsonSerializer.Serialize(targetDetail);
                    var oldRequesterScheduleId = requesterDetail.DutyScheduleId;
                    var oldTargetScheduleId = targetDetail.DutyScheduleId;
                    var oldRequesterRole = requesterDetail.Role;
                    var oldTargetRole = targetDetail.Role;

                    // Swap: Requester → Target's schedule (giữ role của target)
                    requesterDetail.DutyScheduleId = changeRequest.TargetScheduleId;
                    requesterDetail.Role = oldTargetRole;
                    requesterDetail.SwappedFromUserId = changeRequest.TargetUserId;
                    requesterDetail.Reason = changeRequest.Reason;

                    await _detailRep.Context.Updateable(requesterDetail)
                        .UpdateColumns(d => new { d.DutyScheduleId, d.Role, d.SwappedFromUserId, d.Reason, d.UpdateTime, d.UpdateUId })
                        .ExecuteCommandAsync();

                    // Swap: Target → Requester's schedule (giữ role của requester)
                    targetDetail.DutyScheduleId = oldRequesterScheduleId;
                    targetDetail.Role = oldRequesterRole;
                    targetDetail.SwappedFromUserId = changeRequest.RequesterId;
                    targetDetail.Reason = changeRequest.Reason;

                    await _detailRep.Context.Updateable(targetDetail)
                        .UpdateColumns(d => new { d.DutyScheduleId, d.Role, d.SwappedFromUserId, d.Reason, d.UpdateTime, d.UpdateUId })
                        .ExecuteCommandAsync();

                    var newRequesterJson = System.Text.Json.JsonSerializer.Serialize(requesterDetail);
                    var newTargetJson = System.Text.Json.JsonSerializer.Serialize(targetDetail);

                    // Ghi log cho lịch của requester (cũ) - với RelatedScheduleId là lịch target
                    await LogHistory(
                        oldRequesterScheduleId,
                        changeRequest.ID,
                        DutyScheduleConst.HistoryActionType.Swap,
                        username,
                        changeRequest.RequesterId,
                        oldDataJson,
                        newTargetJson,
                        _localizer[BaseMsg.TmsDutySchedule.Message.HistorySwap, changeRequest.RequesterId, changeRequest.TargetUserId],
                        changeRequest.Reason,
                        oldTargetScheduleId  // RelatedScheduleId = lịch của target
                    );

                    // Ghi log cho lịch của target (cũ) - với RelatedScheduleId là lịch requester
                    await LogHistory(
                        oldTargetScheduleId,
                        changeRequest.ID,
                        DutyScheduleConst.HistoryActionType.Swap,
                        username,
                        changeRequest.TargetUserId,
                        oldTargetJson,
                        newRequesterJson,
                        _localizer[BaseMsg.TmsDutySchedule.Message.HistorySwap, changeRequest.TargetUserId, changeRequest.RequesterId],
                        changeRequest.Reason,
                        oldRequesterScheduleId  // RelatedScheduleId = lịch của requester
                    );
                    break;

                case var type when type == DutyScheduleConst.ChangeRequestType.Replace:
                    // Replace được xử lý riêng bên trên
                    break;

                case var type when type == DutyScheduleConst.ChangeRequestType.Transfer:
                    // Transfer được xử lý riêng bên trên
                    break;

                default:
                    throw Oops.Oh(BaseMsg.TmsDutySchedule.Validation.ChangeRequestTypeInvalid);
            }
        }

        /// <summary>
        /// Xử lý riêng cho Transfer: TargetUserId = người được chuyển (nếu không có thì dùng RequesterId)
        /// </summary>
        private async Task ApplyTransferChange(TmsDutyScheduleChangeRequest changeRequest, string username)
        {
            if (string.IsNullOrWhiteSpace(changeRequest.TargetScheduleId))
                throw Oops.Oh(BaseMsg.TmsDutySchedule.Exception.TransferNoTargetSchedule);

            // Xác định người được chuyển: ưu tiên TargetUserId, nếu không có thì dùng RequesterId
            var transferUserId = !string.IsNullOrWhiteSpace(changeRequest.TargetUserId)
                ? changeRequest.TargetUserId
                : changeRequest.RequesterId;

            // Lấy thông tin người được chuyển trong lịch hiện tại
            var userDetail = await _detailRep.GetFirstAsync(d =>
                d.DutyScheduleId == changeRequest.DutyScheduleId &&
                d.UserId == transferUserId);

            if (userDetail == null)
                throw Oops.Oh(BaseMsg.TmsDutySchedule.Exception.TransferUserNotFound);

            var oldDataJson = System.Text.Json.JsonSerializer.Serialize(userDetail);
            var oldScheduleId = userDetail.DutyScheduleId;

            // Xác định vai trò trong lịch mới: ưu tiên NewRole, nếu không có thì giữ nguyên
            var newRole = !string.IsNullOrWhiteSpace(changeRequest.NewRole)
                ? changeRequest.NewRole
                : userDetail.Role;

            // UPDATE các trường: chuyển sang lịch mới
            userDetail.DutyScheduleId = changeRequest.TargetScheduleId;
            userDetail.Role = newRole;
            userDetail.Status = DutyScheduleConst.DetailStatus.Join;
            userDetail.Reason = changeRequest.Reason;
            userDetail.SwappedFromUserId = null; // Reset swap info khi chuyển lịch

            // Sử dụng UpdateColumns để chỉ định rõ các cột cần update
            var updateResult = await _detailRep.Context.Updateable(userDetail)
                .UpdateColumns(d => new
                {
                    d.DutyScheduleId,
                    d.Role,
                    d.Status,
                    d.Reason,
                    d.SwappedFromUserId,
                    d.UpdateTime,
                    d.UpdateUId
                })
                .ExecuteCommandAsync();

            if (updateResult == 0)
            {
                throw Oops.Oh(BaseMsg.TmsDutySchedule.Exception.UpdateScheduleIdFailed);
            }

            var newDataJson = System.Text.Json.JsonSerializer.Serialize(userDetail);

            // Ghi log cho lịch cũ (nguồn) - với RelatedScheduleId là lịch đích
            await LogHistory(
                oldScheduleId,
                changeRequest.ID,
                DutyScheduleConst.HistoryActionType.Transfer,
                username,
                transferUserId,
                oldDataJson,
                newDataJson,
                _localizer[BaseMsg.TmsDutySchedule.Message.HistoryTransferOut, transferUserId, newRole],
                changeRequest.Reason,
                changeRequest.TargetScheduleId  // RelatedScheduleId = lịch đích
            );

            // Ghi log cho lịch mới (đích) - với RelatedScheduleId là lịch nguồn
            await LogHistory(
                changeRequest.TargetScheduleId,
                changeRequest.ID,
                DutyScheduleConst.HistoryActionType.Transfer,
                username,
                transferUserId,
                oldDataJson,
                newDataJson,
                _localizer[BaseMsg.TmsDutySchedule.Message.HistoryTransferIn, transferUserId, newRole],
                changeRequest.Reason,
                oldScheduleId  // RelatedScheduleId = lịch nguồn
            );
        }

        /// <summary>
        /// Xử lý riêng cho Replace: TargetUserId = người bị thay thế, ReplacerId = người thay thế
        /// </summary>
        private async Task ApplyReplaceChange(TmsDutyScheduleChangeRequest changeRequest, string username)
        {
            // Replace: TargetUserId = người bị thay thế, ReplacerId = người thay thế
            if (string.IsNullOrWhiteSpace(changeRequest.TargetUserId))
                throw Oops.Oh(BaseMsg.TmsDutySchedule.Exception.ReplaceNoTargetUser);

            if (string.IsNullOrWhiteSpace(changeRequest.ReplacerId))
                throw Oops.Oh(BaseMsg.TmsDutySchedule.Exception.ReplaceNoReplacer);

            // Lấy thông tin người bị thay thế
            var targetDetail = await _detailRep.GetFirstAsync(d =>
                d.DutyScheduleId == changeRequest.DutyScheduleId &&
                d.UserId == changeRequest.TargetUserId);

            if (targetDetail == null)
                throw Oops.Oh(BaseMsg.TmsDutySchedule.Exception.ReplaceTargetNotFound);

            var targetRole = targetDetail.Role;
            var oldTargetJson = System.Text.Json.JsonSerializer.Serialize(targetDetail);

            // XÓA người bị thay thế khỏi lịch trực
            await _detailRep.DeleteAsync(targetDetail);

            // Kiểm tra người thay thế đã có trong lịch chưa
            var replacerDetail = await _detailRep.GetFirstAsync(d =>
                d.DutyScheduleId == changeRequest.DutyScheduleId &&
                d.UserId == changeRequest.ReplacerId);

            string? newReplacerJson = null;

            if (replacerDetail != null && replacerDetail.Status == DutyScheduleConst.DetailStatus.Join)
            {
                // Người thay thế đã có trong lịch với status 'join' - cập nhật role nếu cần
                if (replacerDetail.Role != targetRole)
                {
                    replacerDetail.Role = targetRole;
                    replacerDetail.Reason = changeRequest.Reason;
                    await _detailRep.UpdateAsync(replacerDetail);
                }
                newReplacerJson = System.Text.Json.JsonSerializer.Serialize(replacerDetail);
            }
            else if (replacerDetail != null)
            {
                // Người thay thế đã có record nhưng status không phải 'join' - update lại
                replacerDetail.Role = targetRole;
                replacerDetail.Status = DutyScheduleConst.DetailStatus.Join;
                replacerDetail.Reason = changeRequest.Reason;
                await _detailRep.UpdateAsync(replacerDetail);
                newReplacerJson = System.Text.Json.JsonSerializer.Serialize(replacerDetail);
            }
            else
            {
                // Người thay thế chưa có trong lịch - thêm mới
                var newReplacerDetail = new TmsDutyScheduleDetail
                {
                    DutyScheduleId = changeRequest.DutyScheduleId,
                    UserId = changeRequest.ReplacerId,
                    Role = targetRole,
                    Status = DutyScheduleConst.DetailStatus.Join,
                    Reason = changeRequest.Reason
                };
                await _detailRep.InsertAsync(newReplacerDetail);
                newReplacerJson = System.Text.Json.JsonSerializer.Serialize(newReplacerDetail);
            }

            // Ghi 1 log duy nhất cho thay thế: bao gồm cả người bị thay thế và người thay thế
            await LogHistory(
                changeRequest.DutyScheduleId,
                changeRequest.ID,
                DutyScheduleConst.HistoryActionType.Replace,
                username,
                changeRequest.TargetUserId, // AffectedUserId = người bị thay thế
                oldTargetJson,
                newReplacerJson,
                _localizer[BaseMsg.TmsDutySchedule.Message.HistoryReplace, changeRequest.TargetUserId, changeRequest.ReplacerId, targetRole],
                changeRequest.Reason
            );
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

        #region Rule-based Validation Methods

        /// <summary>
        /// Kiểm tra xung đột lịch trực của user với lịch trực chỉ định
        /// Author: System
        /// Created date: 25/11/2025
        /// </summary>
        /// <param name="userId">ID người dùng cần kiểm tra</param>
        /// <param name="scheduleId">ID lịch trực cần kiểm tra xung đột</param>
        /// <param name="excludeScheduleId">ID lịch trực cần loại trừ (optional - dùng khi transfer/swap)</param>
        /// <returns>ConflictCheckResult với thông tin chi tiết</returns>
        private async Task<ConflictCheckResult> CheckUserScheduleConflict(
            string userId,
            string scheduleId,
            string? excludeScheduleId = null)
        {
            var result = new ConflictCheckResult { HasConflict = false };

            // Lấy thông tin lịch trực đích
            var targetSchedule = await _dutyScheduleRep.GetByIdAsync(scheduleId);
            if (targetSchedule == null)
            {
                result.HasConflict = true;
                result.ConflictReason = _localizer[BaseMsg.TmsDutySchedule.Exception.NotFound];
                return result;
            }

            // Lấy tất cả lịch trực mà user đang tham gia (Status = 'join')
            var userDetails = await _detailRep.AsQueryable()
                .Where(d => d.UserId == userId && d.Status == DutyScheduleConst.DetailStatus.Join)
                .ToListAsync();

            if (!userDetails.Any())
            {
                return result; // Không có lịch nào, không có xung đột
            }

            var userScheduleIds = userDetails
                .Select(d => d.DutyScheduleId)
                .Where(id => id != excludeScheduleId) // Loại trừ lịch nếu cần
                .Distinct()
                .ToList();

            if (!userScheduleIds.Any())
            {
                return result;
            }

            // Lấy thông tin các lịch trực của user
            var userSchedules = await _dutyScheduleRep.AsQueryable()
                .Where(s => userScheduleIds.Contains(s.ID))
                .ToListAsync();

            // Kiểm tra xung đột thời gian
            foreach (var schedule in userSchedules)
            {
                if (schedule.StartTime < targetSchedule.EndTime && targetSchedule.StartTime < schedule.EndTime)
                {
                    result.HasConflict = true;
                    result.ConflictingScheduleId = schedule.ID;
                    result.ConflictingScheduleName = schedule.Name;
                    result.ConflictReason = _localizer[BaseMsg.TmsDutySchedule.Message.TimeConflict];
                    return result;
                }
            }

            return result;
        }

        /// <summary>
        /// Kiểm tra user có đang tham gia trong lịch trực hay không
        /// </summary>
        private async Task<bool> IsUserInSchedule(string userId, string scheduleId, bool activeOnly = true)
        {
            if (activeOnly)
            {
                return await _detailRep.IsAnyAsync(d =>
                    d.DutyScheduleId == scheduleId &&
                    d.UserId == userId &&
                    d.Status == DutyScheduleConst.DetailStatus.Join);
            }

            return await _detailRep.IsAnyAsync(d =>
                d.DutyScheduleId == scheduleId &&
                d.UserId == userId);
        }

        /// <summary>
        /// Lấy thông tin chi tiết của user trong lịch trực
        /// </summary>
        private async Task<TmsDutyScheduleDetail?> GetUserScheduleDetail(string userId, string scheduleId, bool activeOnly = true)
        {
            if (activeOnly)
            {
                return await _detailRep.GetFirstAsync(d =>
                    d.DutyScheduleId == scheduleId &&
                    d.UserId == userId &&
                    d.Status == DutyScheduleConst.DetailStatus.Join);
            }

            return await _detailRep.GetFirstAsync(d =>
                d.DutyScheduleId == scheduleId &&
                d.UserId == userId);
        }

        /// <summary>
        /// Kiểm tra người thay thế có hợp lệ để thay vào lịch trực không
        /// Author: System
        /// Created date: 25/11/2025
        /// </summary>
        /// <param name="replacerId">ID người thay thế</param>
        /// <param name="scheduleId">ID lịch trực cần thay vào</param>
        /// <param name="targetUserId">ID người bị thay thế (để loại trừ khi check)</param>
        /// <returns>ConflictCheckResult với thông tin chi tiết</returns>
        private async Task<ConflictCheckResult> CheckReplacerEligibility(
            string replacerId,
            string scheduleId,
            string? targetUserId = null)
        {
            var result = new ConflictCheckResult { HasConflict = false };

            // Lấy thông tin lịch trực đích
            var targetSchedule = await _dutyScheduleRep.GetByIdAsync(scheduleId);
            if (targetSchedule == null)
            {
                result.HasConflict = true;
                result.ConflictReason = _localizer[BaseMsg.TmsDutySchedule.Exception.NotFound];
                return result;
            }

            // Check 1: Người thay thế đã có trong lịch này chưa (với status 'join')
            var replacerInSchedule = await _detailRep.GetFirstAsync(d =>
                d.DutyScheduleId == scheduleId &&
                d.UserId == replacerId &&
                d.Status == DutyScheduleConst.DetailStatus.Join);

            if (replacerInSchedule != null)
            {
                result.HasConflict = true;
                result.ConflictingScheduleId = scheduleId;
                result.ConflictingScheduleName = targetSchedule.Name;
                result.ConflictReason = _localizer[BaseMsg.TmsDutySchedule.Exception.AlreadyInTargetSchedule];
                return result;
            }

            // Check 2: Người thay thế có lịch khác trùng giờ không
            var userDetails = await _detailRep.AsQueryable()
                .Where(d => d.UserId == replacerId && d.Status == DutyScheduleConst.DetailStatus.Join)
                .ToListAsync();

            if (userDetails.Any())
            {
                var userScheduleIds = userDetails
                    .Select(d => d.DutyScheduleId)
                    .Where(id => id != scheduleId) // Loại trừ lịch đích
                    .Distinct()
                    .ToList();

                if (userScheduleIds.Any())
                {
                    var userSchedules = await _dutyScheduleRep.AsQueryable()
                        .Where(s => userScheduleIds.Contains(s.ID))
                        .ToListAsync();

                    foreach (var schedule in userSchedules)
                    {
                        // Kiểm tra xung đột thời gian
                        if (schedule.StartTime < targetSchedule.EndTime && targetSchedule.StartTime < schedule.EndTime)
                        {
                            result.HasConflict = true;
                            result.ConflictingScheduleId = schedule.ID;
                            result.ConflictingScheduleName = schedule.Name;
                            result.ConflictReason = _localizer[BaseMsg.TmsDutySchedule.Message.TimeConflict];
                            return result;
                        }
                    }
                }
            }

            return result;
        }

        #endregion
    }

    /// <summary>
    /// Kết quả kiểm tra xung đột lịch trực
    /// </summary>
    public class ConflictCheckResult
    {
        public bool HasConflict { get; set; }
        public string? ConflictingScheduleId { get; set; }
        public string? ConflictingScheduleName { get; set; }
        public string? ConflictReason { get; set; }
    }
}
