using Modules.TMS.Controllers.DutySchedule.Dto;
using Modules.TMS.Core.Constants;
using Modules.TMS.Core.Entities;
using Modules.TMS.Infrastructure;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;

namespace Modules.TMS.Controllers.DutySchedule.Queries
{
    /// <summary>
    /// Author: System
    /// Description: Handler xử lý các query của Duty Schedule History
    /// Created date: 25/11/2025
    /// </summary>
    public class DutyScheduleHistoryQueryHandler : IWolverineHandler
    {
        private readonly BaseRepository<TmsDutyScheduleHistory> _historyRep;
        private readonly BaseRepository<TmsDutySchedule> _dutyScheduleRep;
        private readonly UserManager _user;

        public DutyScheduleHistoryQueryHandler(
            BaseRepository<TmsDutyScheduleHistory> historyRep,
            BaseRepository<TmsDutySchedule> dutyScheduleRep,
            UserManager user)
        {
            _historyRep = historyRep;
            _dutyScheduleRep = dutyScheduleRep;
            _user = user;
        }

        /// <summary>
        /// Lấy danh sách lịch sử thay đổi (không phân trang)
        /// </summary>
        public async Task<List<DutyScheduleHistoryOutput>> HandleAsync(DutyScheduleHistoryQueryInput query)
        {
            var q = _historyRep.AsQueryable();

            // Filter by DutyScheduleId
            if (!string.IsNullOrWhiteSpace(query.DutyScheduleId))
                q = q.Where(h => h.DutyScheduleId == query.DutyScheduleId);

            // Filter by ActionType
            if (!string.IsNullOrWhiteSpace(query.ActionType))
                q = q.Where(h => h.ActionType == query.ActionType);

            // Filter by ActionBy
            if (!string.IsNullOrWhiteSpace(query.ActionBy))
                q = q.Where(h => h.ActionBy == query.ActionBy);

            // Filter by AffectedUserId
            if (!string.IsNullOrWhiteSpace(query.AffectedUserId))
                q = q.Where(h => h.AffectedUserId == query.AffectedUserId);

            // Filter by date range
            if (query.FromDate.HasValue)
                q = q.Where(h => h.CreateTime >= query.FromDate.Value);

            if (query.ToDate.HasValue)
                q = q.Where(h => h.CreateTime <= query.ToDate.Value);

            var histories = await q
                .OrderByDescending(h => h.CreateTime)
                .Take(100) // Giới hạn 100 records khi không phân trang
                .ToListAsync();

            return await MapToOutput(histories);
        }

        /// <summary>
        /// Lấy danh sách lịch sử thay đổi có phân trang
        /// </summary>
        public async Task<SqlSugarPagedList<DutyScheduleHistoryOutput>> HandleAsync(PageDutyScheduleHistoryInput query)
        {
            var q = _historyRep.AsQueryable();

            // Filter by DutyScheduleId
            if (!string.IsNullOrWhiteSpace(query.DutyScheduleId))
                q = q.Where(h => h.DutyScheduleId == query.DutyScheduleId);

            // Filter by ActionType
            if (!string.IsNullOrWhiteSpace(query.ActionType))
                q = q.Where(h => h.ActionType == query.ActionType);

            // Filter by ActionBy
            if (!string.IsNullOrWhiteSpace(query.ActionBy))
                q = q.Where(h => h.ActionBy == query.ActionBy);

            // Filter by AffectedUserId
            if (!string.IsNullOrWhiteSpace(query.AffectedUserId))
                q = q.Where(h => h.AffectedUserId == query.AffectedUserId);

            // Filter by date range
            if (query.FromDate.HasValue)
                q = q.Where(h => h.CreateTime >= query.FromDate.Value);

            if (query.ToDate.HasValue)
                q = q.Where(h => h.CreateTime <= query.ToDate.Value);

            // Get paged result
            var pagedResult = await q
                .Select(h => new DutyScheduleHistoryOutput(), true)
                .OrderBuilder(query, "", "CreateTime", false)
                .ToPagedListAsync(query.Page, query.PageSize);

            // Load additional data
            if (pagedResult.Records != null && pagedResult.Records.Any())
            {
                var scheduleIds = pagedResult.Records
                    .Select(h => h.DutyScheduleId)
                    .Distinct()
                    .ToList();

                var schedules = await _dutyScheduleRep.AsQueryable()
                    .Where(s => scheduleIds.Contains(s.ID))
                    .Select(s => new { s.ID, s.Name })
                    .ToListAsync();

                foreach (var item in pagedResult.Records)
                {
                    var schedule = schedules.FirstOrDefault(s => s.ID == item.DutyScheduleId);
                    item.DutyScheduleName = schedule?.Name;
                    item.ActionTypeDisplay = GetActionTypeDisplay(item.ActionType);
                }
            }

            return pagedResult;
        }

        /// <summary>
        /// Map entities to output DTOs
        /// </summary>
        private async Task<List<DutyScheduleHistoryOutput>> MapToOutput(List<TmsDutyScheduleHistory> histories)
        {
            if (!histories.Any())
                return new List<DutyScheduleHistoryOutput>();

            // Lấy tất cả scheduleIds (bao gồm cả RelatedScheduleId)
            var scheduleIds = histories.Select(h => h.DutyScheduleId).ToList();
            var relatedScheduleIds = histories
                .Where(h => !string.IsNullOrWhiteSpace(h.RelatedScheduleId))
                .Select(h => h.RelatedScheduleId!)
                .ToList();
            var allScheduleIds = scheduleIds.Concat(relatedScheduleIds).Distinct().ToList();

            var schedules = await _dutyScheduleRep.AsQueryable()
                .Where(s => allScheduleIds.Contains(s.ID))
                .Select(s => new { s.ID, s.Name, s.Code })
                .ToListAsync();

            var result = new List<DutyScheduleHistoryOutput>();

            foreach (var history in histories)
            {
                var output = history.Adapt<DutyScheduleHistoryOutput>();
                var schedule = schedules.FirstOrDefault(s => s.ID == history.DutyScheduleId);
                output.DutyScheduleName = schedule?.Name ?? schedule?.Code;

                // Map RelatedScheduleName nếu có
                if (!string.IsNullOrWhiteSpace(history.RelatedScheduleId))
                {
                    var relatedSchedule = schedules.FirstOrDefault(s => s.ID == history.RelatedScheduleId);
                    output.RelatedScheduleName = relatedSchedule?.Name ?? relatedSchedule?.Code;
                }

                output.ActionTypeDisplay = GetActionTypeDisplay(history.ActionType);
                result.Add(output);
            }

            return result;
        }

        /// <summary>
        /// Lấy mô tả hiển thị cho loại thay đổi
        /// </summary>
        private string GetActionTypeDisplay(string actionType)
        {
            return actionType switch
            {
                DutyScheduleConst.HistoryActionType.Create => "Tạo mới",
                DutyScheduleConst.HistoryActionType.Update => "Cập nhật",
                DutyScheduleConst.HistoryActionType.Delete => "Xóa",
                DutyScheduleConst.HistoryActionType.AddMember => "Thêm thành viên",
                DutyScheduleConst.HistoryActionType.RemoveMember => "Xóa thành viên",
                DutyScheduleConst.HistoryActionType.ChangeRole => "Đổi vai trò",
                DutyScheduleConst.HistoryActionType.Transfer => "Chuyển lịch",
                DutyScheduleConst.HistoryActionType.Swap => "Đổi ca",
                DutyScheduleConst.HistoryActionType.Replace => "Thay thế",
                DutyScheduleConst.HistoryActionType.Absent => "Vắng mặt",
                DutyScheduleConst.HistoryActionType.Approve => "Phê duyệt",
                DutyScheduleConst.HistoryActionType.Reject => "Từ chối",
                DutyScheduleConst.HistoryActionType.Cancel => "Hủy",
                _ => actionType
            };
        }
    }
}
