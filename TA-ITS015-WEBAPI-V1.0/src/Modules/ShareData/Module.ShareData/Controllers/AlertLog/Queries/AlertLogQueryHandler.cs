using Mapster;
using Module.ShareData.Core.Dto.AlertLog;
using Module.ShareData.Core.Entities;
using Module.ShareData.Infrastructure;
using Module.ShareData.Infrastructure.Services.ShareDataConfig;
using Shared.DTO.Constants.Localization;
using Shared.DTO.Enums;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Utility.Utilities;
using Wolverine;

namespace Module.ShareData.Controllers.AlertLog.Queries
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Xử lý truy vấn Cảnh báo và lỗi phân hệ Chia sẻ dữ liệu (ShareDataAlertLog)
    /// Created date: 2026-08-21
    /// </summary>
    public class AlertLogQueryHandler : IWolverineHandler
    {
        private readonly BaseRepository<ShareDataAlertLog> _alertLogRep;
        private readonly ConfigService _configService;

        public AlertLogQueryHandler(
            BaseRepository<ShareDataAlertLog> alertLogRep,
            ConfigService configService)
        {
            _alertLogRep = alertLogRep;
            _configService = configService;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy danh sách cảnh báo theo phân trang.
        ///              KHÔNG trả DetailJson để lưới nhẹ — xem chi tiết thì gọi GetById.
        /// Created date: 2026-08-21
        /// </summary>
        public async Task<SqlSugarPagedList<ShareDataPageAlertLogOutput>> HandleAsync(ShareDataPageAlertLogInput command)
        {
            // FE gửi thời gian UTC+0 -> đổi về múi giờ server vì OccurredAt lưu theo giờ local.
            command.FromDate = BaseUtil.TimeServer.GetTimeServer(command.FromDate);
            command.ToDate = BaseUtil.TimeServer.GetTimeServer(command.ToDate);

            var pagedList = await _alertLogRep.AsQueryable()
                .WhereIF(command.FromDate != null, u => u.OccurredAt >= command.FromDate)
                .WhereIF(command.ToDate != null, u => u.OccurredAt <= command.ToDate)
                .WhereIF(command.Severity.HasValue, u => u.Severity == command.Severity)
                .WhereIF(command.AlertSource.HasValue, u => u.AlertSource == command.AlertSource)
                .WhereIF(!string.IsNullOrWhiteSpace(command.AlertCode), u => u.AlertCode == command.AlertCode)
                .WhereIF(!string.IsNullOrWhiteSpace(command.PartnerId), u => u.PartnerId == command.PartnerId)
                .WhereIF(!string.IsNullOrWhiteSpace(command.SessionId), u => u.SessionId == command.SessionId)
                .WhereIF(!string.IsNullOrWhiteSpace(command.SubscriptionId), u => u.SubscriptionId == command.SubscriptionId)
                .WhereIF(!string.IsNullOrWhiteSpace(command.DatatypeId), u => u.DatatypeId == command.DatatypeId)
                // Cột Acknowledged để null với bản ghi cũ nên "chưa xác nhận" phải tính cả null.
                .WhereIF(command.Acknowledged == false, u => u.Acknowledged == null || u.Acknowledged == false)
                .WhereIF(command.Acknowledged == true, u => u.Acknowledged == true)
                .WhereIF(!string.IsNullOrWhiteSpace(command.Keyword), u =>
                    (u.Message != null && u.Message.Contains(command.Keyword!))
                    || (u.AlertCode != null && u.AlertCode.Contains(command.Keyword!)))
                .Select(u => new ShareDataPageAlertLogOutput
                {
                    ID = u.ID,
                    OccurredAt = u.OccurredAt,
                    Severity = u.Severity,
                    AlertSource = u.AlertSource,
                    AlertCode = u.AlertCode,
                    PartnerId = u.PartnerId,
                    SessionId = u.SessionId,
                    SubscriptionId = u.SubscriptionId,
                    DatatypeId = u.DatatypeId,
                    Message = u.Message,
                    Acknowledged = u.Acknowledged,
                    AckBy = u.AckBy,
                    AckAt = u.AckAt,
                    Remark = u.Remark,
                    CreateTime = u.CreateTime,
                    UpdateTime = u.UpdateTime,
                    IsDelete = u.IsDelete,
                })
                .OrderBuilder(command, "")
                .ToPagedListAsync(command.Page, command.PageSize);

            return pagedList;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy danh sách cảnh báo theo tiêu chí lọc, có chặn trần số bản ghi
        /// Created date: 2026-08-21
        /// </summary>
        public async Task<List<ShareDataAlertLogOutput>> HandleAsync(ShareDataAlertLogInput command)
        {
            var limit = await _configService.Get();
            var topN = command.TopN is > 0 ? command.TopN!.Value : limit.LogListDefaultTop;

            // FE gửi thời gian UTC+0 -> đổi về múi giờ server vì OccurredAt lưu theo giờ local.
            command.FromDate = BaseUtil.TimeServer.GetTimeServer(command.FromDate);
            command.ToDate = BaseUtil.TimeServer.GetTimeServer(command.ToDate);

            return await _alertLogRep.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.ID), u => u.ID == command.ID)
                .WhereIF(command.Severity.HasValue, u => u.Severity == command.Severity)
                .WhereIF(command.AlertSource.HasValue, u => u.AlertSource == command.AlertSource)
                .WhereIF(!string.IsNullOrWhiteSpace(command.AlertCode), u => u.AlertCode == command.AlertCode)
                .WhereIF(!string.IsNullOrWhiteSpace(command.PartnerId), u => u.PartnerId == command.PartnerId)
                .WhereIF(!string.IsNullOrWhiteSpace(command.DatatypeId), u => u.DatatypeId == command.DatatypeId)
                .WhereIF(command.Acknowledged == false, u => u.Acknowledged == null || u.Acknowledged == false)
                .WhereIF(command.Acknowledged == true, u => u.Acknowledged == true)
                .WhereIF(command.FromDate != null, u => u.OccurredAt >= command.FromDate)
                .WhereIF(command.ToDate != null, u => u.OccurredAt <= command.ToDate)
                .Select(u => new ShareDataAlertLogOutput() { }, true)
                .OrderBy(u => u.OccurredAt, OrderByType.Desc)
                .Take(topN)
                .ToListAsync();
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy chi tiết 1 cảnh báo (kèm DetailJson để dò lỗi)
        /// Created date: 2026-08-21
        /// </summary>
        public async Task<ShareDataAlertLogOutput> HandleAsync(ShareDataIdAlertLogInput command)
        {
            var entity = await _alertLogRep.AsQueryable()
                .Where(u => u.ID == command.ID)
                .Select(u => new ShareDataAlertLogOutput() { }, true)
                .FirstAsync()
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            return entity;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Đếm số cảnh báo chưa xác nhận xử lý
        /// Created date: 2026-08-21
        /// </summary>
        public async Task<int> HandleAsync(ShareDataCountUnackedAlertLogInput command)
        {
            return await _alertLogRep.AsQueryable()
                .Where(u => u.IsDelete == null && (u.Acknowledged == null || u.Acknowledged == false))
                .WhereIF(!string.IsNullOrWhiteSpace(command.PartnerId), u => u.PartnerId == command.PartnerId)
                .WhereIF(command.Severity.HasValue, u => u.Severity == command.Severity)
                .CountAsync();
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Thống kê nhanh theo mức độ và theo nguồn phát.
        ///              Gom nhóm tại DB rồi tổng hợp trong bộ nhớ để chỉ chạy 1 truy vấn.
        /// Created date: 2026-08-21
        /// </summary>
        public async Task<ShareDataSummaryAlertLogOutput> HandleAsync(ShareDataSummaryAlertLogInput command)
        {
            // FE gửi thời gian UTC+0 -> đổi về múi giờ server vì OccurredAt lưu theo giờ local.
            command.FromDate = BaseUtil.TimeServer.GetTimeServer(command.FromDate);
            command.ToDate = BaseUtil.TimeServer.GetTimeServer(command.ToDate);

            var rows = await _alertLogRep.AsQueryable()
                .Where(u => u.IsDelete == null)
                .WhereIF(!string.IsNullOrWhiteSpace(command.PartnerId), u => u.PartnerId == command.PartnerId)
                .WhereIF(command.AlertSource.HasValue, u => u.AlertSource == command.AlertSource)
                .WhereIF(command.FromDate != null, u => u.OccurredAt >= command.FromDate)
                .WhereIF(command.ToDate != null, u => u.OccurredAt <= command.ToDate)
                .GroupBy(u => new { u.Severity, u.AlertSource, u.Acknowledged })
                .Select(u => new
                {
                    u.Severity,
                    u.AlertSource,
                    u.Acknowledged,
                    Count = SqlFunc.AggregateCount(u.ID)
                })
                .ToListAsync();

            return new ShareDataSummaryAlertLogOutput
            {
                Total = rows.Sum(r => r.Count),
                WarningCount = rows.Where(r => r.Severity == BaseEnums.AlertSeverity.Warning).Sum(r => r.Count),
                ErrorCount = rows.Where(r => r.Severity == BaseEnums.AlertSeverity.Error).Sum(r => r.Count),
                UnackedCount = rows.Where(r => r.Acknowledged != true).Sum(r => r.Count),
                BySource = rows
                    .Where(r => r.AlertSource != null)
                    .GroupBy(r => r.AlertSource!.Value)
                    // Code trả về tên enum chứ không phải số, để FE khỏi phải tra bảng khi hiển thị.
                    .Select(g => new ShareDataAlertCountOutput { Code = g.Key.ToString(), Count = g.Sum(x => x.Count) })
                    .OrderByDescending(g => g.Count)
                    .ToList()
            };
        }
    }
}
