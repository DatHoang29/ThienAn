using Modules.TMS.Controllers.DutySchedule.Dto;
using Modules.TMS.Core.Constants;
using Modules.TMS.Core.Entities;
using Modules.TMS.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;

namespace Modules.TMS.Controllers.DutySchedule.Queries
{
    /// <summary>
    /// Author: System
    /// Description: Handler xử lý các query của Change Request
    /// Created date: 19/11/2025
    /// </summary>
    public class DutyScheduleChangeRequestQueryHandler : IWolverineHandler
    {
        private BaseRepository<TmsDutyScheduleChangeRequest> _changeRequestRep;
        private BaseRepository<TmsDutySchedule> _dutyScheduleRep;
        private BaseRepository<TmsDutyScheduleDetail> _detailRep;
        private UserManager _user;
        private BaseCacheService _cache;

        public DutyScheduleChangeRequestQueryHandler(
            BaseRepository<TmsDutyScheduleChangeRequest> changeRequestRep,
            BaseRepository<TmsDutySchedule> dutyScheduleRep,
            BaseRepository<TmsDutyScheduleDetail> detailRep,
            UserManager user,
            BaseCacheService cache)
        {
            _changeRequestRep = changeRequestRep;
            _dutyScheduleRep = dutyScheduleRep;
            _detailRep = detailRep;
            _user = user;
            _cache = cache;
        }

        /// <summary>
        /// Lấy danh sách yêu cầu thay đổi lịch trực
        /// </summary>
        public async Task<List<DutyScheduleChangeRequestOutput>> HandleAsync(ChangeRequestQueryInput command)
        {
            var username = _user.Account ?? string.Empty;

            var query = _changeRequestRep.AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(command.DutyScheduleId))
                query = query.Where(r => r.DutyScheduleId == command.DutyScheduleId);

            if (!string.IsNullOrWhiteSpace(command.RequesterId))
                query = query.Where(r => r.RequesterId == command.RequesterId);

            if (!string.IsNullOrWhiteSpace(command.Type))
                query = query.Where(r => r.Type == command.Type);

            if (!string.IsNullOrWhiteSpace(command.Status))
                query = query.Where(r => r.Status == command.Status);

            // Apply ViewMode
            if (command.ViewMode == ChangeRequestViewMode.MyRequests && !string.IsNullOrWhiteSpace(username))
            {
                // Yêu cầu do tôi tạo
                query = query.Where(r => r.RequesterId == username);
            }
            else if (command.ViewMode == ChangeRequestViewMode.PendingApproval && !string.IsNullOrWhiteSpace(username))
            {
                // Yêu cầu chờ tôi duyệt: lịch do tôi tạo hoặc tôi là Leader

                // Lấy danh sách lịch do tôi tạo
                var myScheduleIds = await _dutyScheduleRep.AsQueryable()
                    .Where(s => s.CreateUId == username)
                    .Select(s => s.ID)
                    .ToListAsync();

                // Lấy danh sách lịch tôi là Leader
                var leaderScheduleIds = await _detailRep.AsQueryable()
                    .Where(d => d.UserId == username && d.Role == DutyScheduleConst.Role.Leader)
                    .Select(d => d.DutyScheduleId)
                    .ToListAsync();

                var approvalScheduleIds = myScheduleIds.Union(leaderScheduleIds).Distinct().ToList();

                query = query.Where(r =>
                    r.Status == DutyScheduleConst.ChangeRequestStatus.Pending &&
                    approvalScheduleIds.Contains(r.DutyScheduleId)
                    //&& r.RequesterId != username // Loại trừ yêu cầu của chính mình
                    );
            }

            // Filter by date range
            if (command.FromDate.HasValue)
                query = query.Where(r => r.CreateTime >= command.FromDate.Value);

            if (command.ToDate.HasValue)
                query = query.Where(r => r.CreateTime <= command.ToDate.Value);

            var requests = await query
                .OrderByDescending(r => r.CreateTime)
                .ToListAsync();

            // Map to output với thông tin bổ sung
            var result = new List<DutyScheduleChangeRequestOutput>();

            foreach (var request in requests)
            {
                var output = request.Adapt<DutyScheduleChangeRequestOutput>();

                // Lấy thông tin lịch trực
                var schedule = await _dutyScheduleRep.GetByIdAsync(request.DutyScheduleId);
                if (schedule != null)
                {
                    output.DutySchedule = schedule.Adapt<DutyScheduleOutput>();

                    // Load details cho schedule
                    var details = await _detailRep.AsQueryable()
                        .Where(d => d.DutyScheduleId == schedule.ID)
                        .ToListAsync();

                    output.DutySchedule.Details = details.Select(d => d.Adapt<DutyScheduleDetailOutput>()).ToList();
                }

                // TODO: Load user names từ SysUser nếu có
                // output.RequesterName = ...
                // output.TargetUserName = ...
                // output.ApproverName = ...

                result.Add(output);
            }

            return result;
        }

        /// <summary>
        /// Lấy danh sách yêu cầu thay đổi có phân trang
        /// Author: System
        /// Created date: 25/11/2025
        /// </summary>
        public async Task<SqlSugarPagedList<DutyScheduleChangeRequestOutput>> HandleAsync(PageChangeRequestInput command)
        {
            var username = _user.Account ?? string.Empty;

            var query = _changeRequestRep.AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(command.DutyScheduleId))
                query = query.Where(r => r.DutyScheduleId == command.DutyScheduleId);

            if (!string.IsNullOrWhiteSpace(command.RequesterId))
                query = query.Where(r => r.RequesterId == command.RequesterId);

            if (!string.IsNullOrWhiteSpace(command.Type))
                query = query.Where(r => r.Type == command.Type);

            if (!string.IsNullOrWhiteSpace(command.Status))
                query = query.Where(r => r.Status == command.Status);

            // Filter by date range
            if (command.FromDate.HasValue)
                query = query.Where(r => r.CreateTime >= command.FromDate.Value);

            if (command.ToDate.HasValue)
                query = query.Where(r => r.CreateTime <= command.ToDate.Value);

            // Apply ViewMode
            if (command.ViewMode == ChangeRequestViewMode.MyRequests && !string.IsNullOrWhiteSpace(username))
            {
                query = query.Where(r => r.RequesterId == username);
            }
            else if (command.ViewMode == ChangeRequestViewMode.PendingApproval && !string.IsNullOrWhiteSpace(username))
            {
                var myScheduleIds = await _dutyScheduleRep.AsQueryable()
                    .Where(s => s.CreateUId == username)
                    .Select(s => s.ID)
                    .ToListAsync();

                var leaderScheduleIds = await _detailRep.AsQueryable()
                    .Where(d => d.UserId == username && d.Role == DutyScheduleConst.Role.Leader)
                    .Select(d => d.DutyScheduleId)
                    .ToListAsync();

                var approvalScheduleIds = myScheduleIds.Union(leaderScheduleIds).Distinct().ToList();

                query = query.Where(r =>
                    r.Status == DutyScheduleConst.ChangeRequestStatus.Pending &&
                    approvalScheduleIds.Contains(r.DutyScheduleId)
                );
            }

            // Get paged result
            var pagedRequests = await query
                .Select(r => new DutyScheduleChangeRequestOutput(), true)
                .OrderBuilder(command, "", BaseConst.SortFieldConst.UpdateOrCreateTime)
                .ToPagedListAsync(command.Page, command.PageSize);

            // Load additional data for each item
            if (pagedRequests.Records != null && pagedRequests.Records.Any())
            {
                var scheduleIds = pagedRequests.Records.Select(r => r.DutyScheduleId).Distinct().ToList();
                var schedules = await _dutyScheduleRep.AsQueryable()
                    .Where(s => scheduleIds.Contains(s.ID))
                    .ToListAsync();

                var details = await _detailRep.AsQueryable()
                    .Where(d => scheduleIds.Contains(d.DutyScheduleId))
                    .ToListAsync();

                foreach (var item in pagedRequests.Records)
                {
                    var schedule = schedules.FirstOrDefault(s => s.ID == item.DutyScheduleId);
                    if (schedule != null)
                    {
                        item.DutySchedule = schedule.Adapt<DutyScheduleOutput>();
                        item.DutySchedule.Details = details
                            .Where(d => d.DutyScheduleId == schedule.ID)
                            .Select(d => d.Adapt<DutyScheduleDetailOutput>())
                            .ToList();
                    }
                }
            }

            return pagedRequests;
        }

        /// <summary>
        /// Lấy chi tiết một yêu cầu thay đổi
        /// </summary>
        public async Task<DutyScheduleChangeRequestOutput?> HandleAsync(GetChangeRequestByIdInput command)
        {
            var request = await _changeRequestRep.GetByIdAsync(command.Id);
            if (request == null)
                return null;

            var output = request.Adapt<DutyScheduleChangeRequestOutput>();

            // Lấy thông tin lịch trực
            var schedule = await _dutyScheduleRep.GetByIdAsync(request.DutyScheduleId);
            if (schedule != null)
            {
                output.DutySchedule = schedule.Adapt<DutyScheduleOutput>();

                var details = await _detailRep.AsQueryable()
                    .Where(d => d.DutyScheduleId == schedule.ID)
                    .ToListAsync();

                output.DutySchedule.Details = details.Select(d => d.Adapt<DutyScheduleDetailOutput>()).ToList();
            }

            return output;
        }

        /// <summary>
        /// Lấy danh sách lịch trực có thể chuyển đến (không conflict) với rule-based conditions
        /// </summary>
        public async Task<List<AvailableScheduleOutput>> HandleAsync(GetAvailableSchedulesInput command)
        {
            // UserId là bắt buộc - phải chỉ định nhân sự cụ thể
            if (string.IsNullOrWhiteSpace(command.UserId))
                throw Oops.Oh("Vui lòng chỉ định nhân sự cần chuyển lịch");

            var username = command.UserId;

            // Lấy lịch trực hiện tại
            var currentSchedule = await _dutyScheduleRep.GetByIdAsync(command.CurrentScheduleId);
            if (currentSchedule == null)
                throw Oops.Oh("Không tìm thấy lịch trực hiện tại");

            // Lấy thông tin user trong lịch hiện tại
            var currentUserDetail = await _detailRep.GetFirstAsync(d =>
                d.DutyScheduleId == command.CurrentScheduleId &&
                d.UserId == username);

            if (currentUserDetail == null)
                throw Oops.Oh($"Nhân sự '{username}' không có trong lịch trực này");

            // Lấy danh sách lịch trực trong khoảng thời gian
            // Bắt đầu từ thời gian của lịch hiện tại (không phải từ now)
            var fromDate = command.IncludeOngoingSchedules
                ? currentSchedule.StartTime  // Lấy từ thời gian bắt đầu của lịch hiện tại
                : DateTime.Now;
            var toDate = currentSchedule.StartTime.AddDays(command.DateRangeDays);

            // Lấy lịch: không phải lịch hiện tại, chưa hủy, và thời gian phù hợp
            // - StartTime <= toDate (bắt đầu trước ngày kết thúc khoảng tìm)
            // - EndTime >= fromDate (kết thúc sau ngày bắt đầu khoảng tìm) - tức là lịch chưa kết thúc
            var schedules = await _dutyScheduleRep.AsQueryable()
                .Where(s => s.ID != command.CurrentScheduleId &&
                           s.Status != DutyScheduleConst.ScheduleStatus.Cancel &&
                           s.EndTime >= fromDate &&  // Lịch chưa kết thúc tại thời điểm bắt đầu lịch hiện tại
                           s.StartTime <= toDate)    // Lịch bắt đầu trong khoảng thời gian tìm kiếm
                .OrderBy(s => s.StartTime)
                .ToListAsync();

            // Lấy tất cả lịch trực mà user đang tham gia (để kiểm tra conflict)
            var userSchedules = await _detailRep.AsQueryable()
                .Where(d => d.UserId == username)
                .Select(d => new { d.DutyScheduleId, d.Role })
                .ToListAsync();

            var userScheduleIds = userSchedules.Select(u => u.DutyScheduleId).ToList();
            var userExistingSchedules = await _dutyScheduleRep.AsQueryable()
                .Where(s => userScheduleIds.Contains(s.ID) && s.ID != command.CurrentScheduleId)
                .ToListAsync();

            var result = new List<AvailableScheduleOutput>();

            foreach (var schedule in schedules)
            {
                var output = new AvailableScheduleOutput
                {
                    Schedule = schedule.Adapt<DutyScheduleOutput>()
                };

                var conflicts = new List<string>();
                var canJoin = true;

                // Rule 1: Kiểm tra user đã có trong lịch này chưa
                var alreadyInSchedule = await _detailRep.IsAnyAsync(d =>
                    d.DutyScheduleId == schedule.ID &&
                    d.UserId == username);

                if (alreadyInSchedule)
                {
                    canJoin = false;
                    conflicts.Add("Nhân sự này đã có trong lịch trực đích");
                }

                // Rule 2: Với Transfer, user sẽ RỜI lịch hiện tại nên KHÔNG cần check conflict với lịch hiện tại
                // Chỉ hiển thị thông tin tham khảo về thời gian
                bool hasTimeOverlapWithCurrent = currentSchedule.StartTime < schedule.EndTime &&
                                                 schedule.StartTime < currentSchedule.EndTime;
                // Không set canJoin = false vì user sẽ rời lịch hiện tại

                // Rule 3: Kiểm tra conflict với các lịch KHÁC mà user đang tham gia (ngoại trừ lịch hiện tại)
                foreach (var existingSchedule in userExistingSchedules)
                {
                    bool hasTimeConflict = existingSchedule.StartTime < schedule.EndTime &&
                                          schedule.StartTime < existingSchedule.EndTime;
                    if (hasTimeConflict)
                    {
                        canJoin = false;
                        conflicts.Add($"Trùng thời gian với lịch khác '{existingSchedule.Name}' ({existingSchedule.StartTime:dd/MM HH:mm} - {existingSchedule.EndTime:dd/MM HH:mm})");
                    }
                }

                // Rule 4: Kiểm tra số lượng thành viên tối đa (nếu có giới hạn)
                var currentMemberCount = await _detailRep.AsQueryable()
                    .Where(d => d.DutyScheduleId == schedule.ID)
                    .CountAsync();

                // Giả sử max 10 người/lịch (có thể config)
                const int maxMembersPerSchedule = 10;
                if (currentMemberCount >= maxMembersPerSchedule)
                {
                    canJoin = false;
                    conflicts.Add($"Lịch đã đủ số lượng thành viên ({currentMemberCount}/{maxMembersPerSchedule})");
                }

                // Rule 5: Kiểm tra khoảng cách thời gian hợp lý (ít nhất 8 giờ nghỉ giữa 2 ca)
                const int minRestHours = 8;
                foreach (var existingSchedule in userExistingSchedules)
                {
                    var timeDiff = (schedule.StartTime - existingSchedule.EndTime).TotalHours;
                    var timeDiffReverse = (existingSchedule.StartTime - schedule.EndTime).TotalHours;

                    if ((timeDiff > 0 && timeDiff < minRestHours) || (timeDiffReverse > 0 && timeDiffReverse < minRestHours))
                    {
                        canJoin = false;
                        conflicts.Add($"Không đủ thời gian nghỉ giữa các ca (tối thiểu {minRestHours} giờ)");
                        break;
                    }
                }

                // Rule 6: Kiểm tra PatrolTeam (nếu cùng team thì ưu tiên)
                var isSamePatrolTeam = !string.IsNullOrWhiteSpace(currentSchedule.PatrolTeamId) &&
                                      !string.IsNullOrWhiteSpace(schedule.PatrolTeamId) &&
                                      currentSchedule.PatrolTeamId == schedule.PatrolTeamId;

                // Rule 7: Kiểm tra vai trò phù hợp
                var leaderCount = await _detailRep.AsQueryable()
                    .Where(d => d.DutyScheduleId == schedule.ID && d.Role == DutyScheduleConst.Role.Leader)
                    .CountAsync();

                var memberCount = currentMemberCount - leaderCount;

                // Nếu user hiện tại là Leader, ưu tiên lịch thiếu Leader
                if (currentUserDetail.Role == DutyScheduleConst.Role.Leader && leaderCount == 0)
                {
                    output.Priority = "high"; // Lịch cần Leader
                }
                else if (isSamePatrolTeam)
                {
                    output.Priority = "medium"; // Cùng PatrolTeam
                }
                else
                {
                    output.Priority = "normal";
                }

                // Load details cho schedule
                var details = await _detailRep.AsQueryable()
                    .Where(d => d.DutyScheduleId == schedule.ID)
                    .ToListAsync();

                if (output.Schedule != null)
                {
                    output.Schedule.Details = details.Select(d => d.Adapt<DutyScheduleDetailOutput>()).ToList();
                }

                // Set kết quả
                output.HasTimeConflict = conflicts.Any();
                output.HasTimeOverlapWithCurrent = hasTimeOverlapWithCurrent;
                output.ConflictReason = conflicts.Any() ? string.Join("; ", conflicts) : null;
                output.CanJoin = canJoin && !conflicts.Any();
                output.CurrentMemberCount = currentMemberCount;
                output.LeaderCount = leaderCount;
                output.MemberCount = memberCount;
                output.IsSamePatrolTeam = isSamePatrolTeam;

                result.Add(output);
            }

            // Sắp xếp: Ưu tiên lịch có thể join, không conflict, cùng team
            result = result.OrderByDescending(r => r.CanJoin)
                          .ThenByDescending(r => r.IsSamePatrolTeam)
                          .ThenBy(r => r.Schedule?.StartTime)
                          .ToList();

            return result;
        }
    }

    /// <summary>
    /// Input để lấy chi tiết một yêu cầu
    /// </summary>
    public class GetChangeRequestByIdInput
    {
        public string Id { get; set; } = string.Empty;
    }
}
