using Mapster;
using Shared.DTO.Constants.Localization;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using SqlSugar;
using Wolverine;
using Modules.Mail.Core.Dto.LogMail;
using Modules.Mail.Core.Entities;
using Modules.Mail.Infrastructure;

namespace Modules.Mail.Controllers.LogMail.Queries
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Truy vấn nhật ký gửi mail (MailLog) — chỉ đọc.
    ///              Tên lịch lấy bằng truy vấn riêng có ClearFilter để lịch ĐÃ XOÁ MỀM vẫn hiện tên trong lịch sử.
    /// Created date: 02/10/2026
    /// </summary>
    public class MailLogQueryHandler : IWolverineHandler
    {
        private readonly BaseRepository<MailLog> _logRep;
        private readonly BaseRepository<MailSchedule> _scheduleRep;

        public MailLogQueryHandler(BaseRepository<MailLog> logRep, BaseRepository<MailSchedule> scheduleRep)
        {
            _logRep = logRep;
            _scheduleRep = scheduleRep;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Nhật ký gửi theo phân trang, mặc định mới nhất lên đầu.
        /// Created date: 02/10/2026
        /// </summary>
        public async Task<SqlSugarPagedList<MailLogOutput>> HandleAsync(MailPageLogInput command)
        {
            var code = command.Code?.Trim();
            var name = command.Name?.Trim();
            var recipient = command.Recipient?.Trim();
            var from = command.FromDate?.Date;
            var toExclusive = command.ToDate?.Date.AddDays(1);

            // Mã / tên lịch → đổi thành danh sách ID lịch (kể cả lịch đã xoá mềm, để log cũ vẫn lọc được)
            var hasScheduleFilter = !string.IsNullOrEmpty(code) || !string.IsNullOrEmpty(name);
            var scheduleIds = new List<string>();
            if (hasScheduleFilter)
            {
                scheduleIds = await _scheduleRep.AsQueryable().ClearFilter()
                    .WhereIF(!string.IsNullOrEmpty(code), s => s.Code.Contains(code!))
                    .WhereIF(!string.IsNullOrEmpty(name), s => s.Name != null && s.Name.Contains(name!))
                    .Select(s => s.ID)
                    .ToListAsync();

                // Chèn giá trị không thể trùng để SqlSugar không sinh ra "IN ()" (cùng cách RptEmailScheduleQueryHandler)
                if (scheduleIds.Count == 0) scheduleIds.Add("__rpt_no_match__");
            }

            var page = await _logRep.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.ScheduleId), u => u.ScheduleId == command.ScheduleId)
                .WhereIF(command.Status.HasValue, u => u.Status == command.Status)
                .WhereIF(command.TriggerType.HasValue, u => u.TriggerType == command.TriggerType)
                .WhereIF(from.HasValue, u => u.OccurredAt >= from)
                .WhereIF(toExclusive.HasValue, u => u.OccurredAt < toExclusive)
                .WhereIF(hasScheduleFilter, u => scheduleIds.Contains(u.ScheduleId!))
                .WhereIF(!string.IsNullOrEmpty(recipient), u => (u.Recipients != null && u.Recipients.Contains(recipient!))
                    || (u.Cc != null && u.Cc.Contains(recipient!))
                    || (u.Bcc != null && u.Bcc.Contains(recipient!)))
                .Select(u => new MailLogOutput() { }, true)
                .OrderByIF(string.IsNullOrWhiteSpace(command.Field), u => u.OccurredAt, OrderByType.Desc)
                .OrderBuilder(command, "")
                .ToPagedListAsync(command.Page, command.PageSize);

            await FillScheduleAsync(page.Records ?? Enumerable.Empty<MailLogOutput>());
            return page;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Chi tiết 1 lần gửi.
        /// Created date: 02/10/2026
        /// </summary>
        public async Task<MailLogOutput> HandleAsync(MailIdLogInput command)
        {
            var entity = await _logRep.GetFirstAsync(u => u.ID == command.ID)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            var output = entity.Adapt<MailLogOutput>();
            await FillScheduleAsync(new[] { output });
            return output;
        }

        /// <summary>Gắn mã / tên lịch cho các dòng log — một truy vấn cho cả trang.</summary>
        private async Task FillScheduleAsync(IEnumerable<MailLogOutput> rows)
        {
            var list = rows.ToList();
            var ids = list.Select(x => x.ScheduleId).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();
            if (ids.Count == 0) return;

            var schedules = await _scheduleRep.AsQueryable().ClearFilter()
                .Where(s => ids.Contains(s.ID))
                .Select(s => new { s.ID, s.Code, s.Name })
                .ToListAsync();
            var map = schedules.ToDictionary(x => x.ID, x => x);

            foreach (var row in list)
            {
                if (row.ScheduleId == null || !map.TryGetValue(row.ScheduleId, out var s)) continue;
                row.ScheduleCode = s.Code;
                row.ScheduleName = s.Name;
            }
        }
    }
}
