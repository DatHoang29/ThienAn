using Modules.TMS.Controllers.DutySchedule.Dto;
using Modules.TMS.Core.Constants;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Exceptions;
using Modules.TMS.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;
using Shared.Utility.Utilities;

namespace Modules.TMS.Controllers.DutySchedule.Queries
{
    /// <summary>
    /// Author: System
    /// Description: Handler xử lý các query của DutySchedule
    /// Created date: 07/11/2025
    /// </summary>
    public class DutyScheduleQueryHandler : IWolverineHandler
    {
        private BaseRepository<TmsDutySchedule> _dutyScheduleRep;
        private BaseRepository<TmsDutyScheduleDetail> _detailRep;
        private UserManager _user;
        private BaseCacheService _cache;

        public DutyScheduleQueryHandler(
            BaseRepository<TmsDutySchedule> dutyScheduleRep,
            BaseRepository<TmsDutyScheduleDetail> detailRep,
            UserManager user,
            BaseCacheService cache)
        {
            _dutyScheduleRep = dutyScheduleRep;
            _detailRep = detailRep;
            _cache = cache;
            _user = user;
        }

        /// <summary>
        /// Author: System
        /// Description: Lấy danh sách lịch trực với FilterMode (All/CreatedByMe/Related)
        /// Created date: 07/11/2025
        /// Updated: 11/11/2025 - Added FilterMode support
        /// </summary>
        public async Task<List<DutyScheduleOutput>> HandleAsync(DutyScheduleInput command)
        {
            command.StartTime = BaseUtil.TimeServer.GetTimeServer(command.StartTime);
            command.EndTime = BaseUtil.TimeServer.GetTimeServer(command.EndTime);
            var cacheKey = $"{CacheConst.Tms.TmsDutySchedule}:list:{command.Name}:{command.StartTime}:{command.EndTime}:{command.UserId}:{command.Status}:{command.FilterMode}";

            //return await _cache.GetOrAdd(cacheKey, async _ =>
            //{
            var query = _dutyScheduleRep.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.Name), u => u.Name != null && u.Name.Contains(command.Name!))
                .WhereIF(command.StartTime.HasValue, u => u.StartTime >= command.StartTime!.Value)
                .WhereIF(command.EndTime.HasValue, u => u.EndTime <= command.EndTime!.Value)
                .WhereIF(!string.IsNullOrWhiteSpace(command.Status), u => u.Status == command.Status);

            // Get schedules
            var schedules = await query.ToListAsync();
            var scheduleIds = schedules.Select(s => s.ID).ToList();

            // Get all details for these schedules
            var allDetails = await _detailRep.AsQueryable()
                .Where(d => scheduleIds.Contains(d.DutyScheduleId))
                .ToListAsync();

            // Apply FilterMode
            var userId = command.UserId ?? string.Empty;

            // Đối với FilterMode Quản lý và Tham gia: nếu không có UserId thì lấy user hiện tại
            if ((command.FilterMode == DutyScheduleFilterMode.CreatedByMe ||
                 command.FilterMode == DutyScheduleFilterMode.Related) &&
                string.IsNullOrWhiteSpace(userId))
            {
                userId = _user.Account ?? string.Empty;
            }

            if (command.FilterMode == DutyScheduleFilterMode.CreatedByMe && !string.IsNullOrWhiteSpace(userId))
            {
                // Quản lý: Chỉ lịch do người dùng hiện tại tạo (CreateUId)
                schedules = schedules.Where(s => s.CreateUId == userId).ToList();
            }
            else if (command.FilterMode == DutyScheduleFilterMode.Related && !string.IsNullOrWhiteSpace(userId))
            {
                // Tham gia: Chỉ lịch tôi ĐANG tham gia (Status = 'join') với vai trò Leader hoặc Member
                // KHÔNG bao gồm: swap, absent
                var participantScheduleIds = allDetails
                    .Where(d => d.UserId == userId
                               //&&
                               //    d.Status == "join" && // Chỉ lấy những người đang active
                               //    (d.Role == "leader" || d.Role == "member")
                               )
                    .Select(d => d.DutyScheduleId)
                    .Distinct()
                    .ToList();

                schedules = schedules.Where(s => participantScheduleIds.Contains(s.ID)).ToList();
            }
            else if (command.FilterMode == DutyScheduleFilterMode.All && !string.IsNullOrWhiteSpace(userId))
            {
                // Tất cả: nếu có UserId thì tìm theo người dùng đó (tạo + tham gia ĐANG ACTIVE)
                var createdByUserIds = schedules
                    .Where(s => s.CreateUId == userId)
                    .Select(s => s.ID)
                    .ToList();

                var participantScheduleIds = allDetails
                    .Where(d => d.UserId == userId && d.Status == "join") // Chỉ lấy active
                    .Select(d => d.DutyScheduleId)
                    .Distinct()
                    .ToList();

                var allUserScheduleIds = createdByUserIds
                    .Union(participantScheduleIds)
                    .Distinct()
                    .ToList();

                schedules = schedules.Where(s => allUserScheduleIds.Contains(s.ID)).ToList();
            }
            // FilterMode.All không có UserId: lấy tất cả theo các điều kiện khác            // Order by StartTime DESC
            schedules = schedules.OrderByDescending(s => s.StartTime).ToList();

            // Map to output
            var result = schedules.Select(s =>
            {
                var output = s.Adapt<DutyScheduleOutput>();
                output.Details = allDetails
                    .Where(d => d.DutyScheduleId == s.ID)
                    .Select(d => d.Adapt<DutyScheduleDetailOutput>())
                    .ToList();
                return output;
            }).ToList();

            return result;
            //});
        }

        /// <summary>
        /// Author: System
        /// Description: Lấy danh sách lịch trực sắp tới của user (20 lịch gần nhất hoặc trong 7 ngày)
        /// Created date: 21/11/2025
        /// </summary>
        public async Task<List<DutyScheduleOutput>> HandleAsync(GetUpcomingSchedulesInput command)
        {
            var userId = command.UserId;

            // Nếu không có UserId thì lấy user hiện tại
            if (string.IsNullOrWhiteSpace(userId))
            {
                userId = _user.Account ?? throw Oops.Oh(BaseMsg.TmsDutySchedule.Exception.UserNotIdentified);
            }

            var now = DateTime.Now;
            var maxDate = now.AddDays(command.MaxDays);

            // Get user's schedule details (lịch mà user tham gia)
            var userDetails = await _detailRep.GetListAsync(d => d.UserId == userId);

            if (command.ActiveOnly)
            {
                userDetails = userDetails.Where(d => d.Status == DutyScheduleConst.DetailStatus.Join).ToList();
            }

            if (!userDetails.Any())
            {
                return new List<DutyScheduleOutput>();
            }

            var scheduleIds = userDetails.Select(d => d.DutyScheduleId).Distinct().ToList();

            // Get schedules that are upcoming (StartTime >= now)
            var query = _dutyScheduleRep.AsQueryable()
                .Where(s => scheduleIds.Contains(s.ID))
                .Where(s => s.StartTime >= now && s.StartTime <= maxDate)
                .OrderBy(s => s.StartTime);

            var schedules = await query.Take(command.MaxCount).ToListAsync();

            if (!schedules.Any())
            {
                return new List<DutyScheduleOutput>();
            }

            // Get details for these schedules
            var scheduleIdList = schedules.Select(s => s.ID).ToList();
            var allDetails = await _detailRep.AsQueryable()
                .Where(d => scheduleIdList.Contains(d.DutyScheduleId))
                .ToListAsync();

            // Map to output
            var result = schedules.Select(s =>
            {
                var output = s.Adapt<DutyScheduleOutput>();
                output.Details = allDetails
                    .Where(d => d.DutyScheduleId == s.ID)
                    .Select(d => d.Adapt<DutyScheduleDetailOutput>())
                    .ToList();
                return output;
            }).ToList();

            return result;
        }

        /// <summary>
        /// Author: System
        /// Description: Lấy lịch trực theo tuần
        /// Created date: 07/11/2025
        /// </summary>
        public async Task<List<DutyScheduleOutput>> HandleAsync(GetByWeekInput command)
        {

            // Calculate week start (Monday) and end (Sunday)
            var dayOfWeek = (int)command.Date.DayOfWeek;
            var diff = dayOfWeek == 0 ? -6 : 1 - dayOfWeek; // Monday = 1
            var weekStart = command.Date.Date.AddDays(diff);
            var weekEnd = weekStart.AddDays(7);

            var cacheKey = $"{CacheConst.Tms.TmsDutySchedule}:week:{weekStart:yyyy-MM-dd}:{command.UserId}:{command.FilterMode}";

            //return await _cache.GetOrAdd(cacheKey, async _ =>
            //{
            var query = _dutyScheduleRep.AsQueryable()
                .Where(u => u.StartTime >= weekStart && u.StartTime < weekEnd)
                .OrderBy(u => u.StartTime);

            var schedules = await query.ToListAsync();
            var scheduleIds = schedules.Select(s => s.ID).ToList();

            // Get all details
            var allDetails = await _detailRep.AsQueryable()
                .Where(d => scheduleIds.Contains(d.DutyScheduleId))
                .ToListAsync();

            // Apply FilterMode
            var userId = command.UserId ?? string.Empty;

            // Đối với FilterMode Quản lý và Tham gia: nếu không có UserId thì lấy user hiện tại
            if ((command.FilterMode == DutyScheduleFilterMode.CreatedByMe ||
                 command.FilterMode == DutyScheduleFilterMode.Related) &&
                string.IsNullOrWhiteSpace(userId))
            {
                userId = _user.Account ?? string.Empty;
            }

            if (command.FilterMode == DutyScheduleFilterMode.CreatedByMe && !string.IsNullOrWhiteSpace(userId))
            {
                // Quản lý: Chỉ lịch do người dùng hiện tại tạo (CreateUId)
                schedules = schedules.Where(s => s.CreateUId == userId).ToList();
            }
            else if (command.FilterMode == DutyScheduleFilterMode.Related && !string.IsNullOrWhiteSpace(userId))
            {
                // Tham gia: Chỉ lịch tôi tham gia với vai trò Leader hoặc Member (KHÔNG bao gồm lịch tạo)
                var participantScheduleIds = allDetails
                    .Where(d => d.UserId == userId &&
                               (d.Role == "Leader" || d.Role == "Member"))
                    .Select(d => d.DutyScheduleId)
                    .Distinct()
                    .ToList();

                schedules = schedules.Where(s => participantScheduleIds.Contains(s.ID)).ToList();
            }
            else if (command.FilterMode == DutyScheduleFilterMode.All && !string.IsNullOrWhiteSpace(userId))
            {
                // Tất cả: nếu có UserId thì tìm theo người dùng đó (tạo + tham gia)
                var createdByUserIds = schedules
                    .Where(s => s.CreateUId == userId)
                    .Select(s => s.ID)
                    .ToList();

                var participantScheduleIds = allDetails
                    .Where(d => d.UserId == userId)
                    .Select(d => d.DutyScheduleId)
                    .Distinct()
                    .ToList();

                var allUserScheduleIds = createdByUserIds
                    .Union(participantScheduleIds)
                    .Distinct()
                    .ToList();

                schedules = schedules.Where(s => allUserScheduleIds.Contains(s.ID)).ToList();
            }
            // FilterMode.All không có UserId: lấy tất cả

            // Map to output
            var result = schedules.Select(s =>
            {
                var output = s.Adapt<DutyScheduleOutput>();
                output.Details = allDetails
                    .Where(d => d.DutyScheduleId == s.ID)
                    .Select(d => d.Adapt<DutyScheduleDetailOutput>())
                    .ToList();
                return output;
            }).ToList();

            return result;
            //});
        }
    }
}
