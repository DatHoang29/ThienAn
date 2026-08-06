using Mapster;
using Module.ShareData.Core.Constants;
using Module.ShareData.Core.Dto.ActivityLog;
using Module.ShareData.Core.Entities;
using Module.ShareData.Infrastructure;
using Shared.DTO.Constants.Localization;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Wolverine;

namespace Module.ShareData.Controllers.ActivityLog.Queries
{
    /// <summary>
    /// Description: Xử lý truy vấn Nhật ký hoạt động phân hệ Chia sẻ dữ liệu (ShareDataActivityLog)
    /// Created date: 2026-08-05
    /// </summary>
    public class ShareDataActivityLogQueryHandler : IWolverineHandler
    {
        /// <summary>Giới hạn mặc định khi lấy danh sách không phân trang.</summary>
        private const int DefaultTopN = 1000;

        private readonly BaseRepository<ShareDataActivityLog> _activityLogRep;

        public ShareDataActivityLogQueryHandler(BaseRepository<ShareDataActivityLog> activityLogRep)
        {
            _activityLogRep = activityLogRep;
        }

        /// <summary>
        /// Description: Lấy nhật ký hoạt động theo phân trang.
        ///              KHÔNG select BeforeJson / AfterJson để lưới nhẹ — xem chi tiết thì gọi GetById.
        /// Created date: 2026-08-05
        /// </summary>
        public async Task<SqlSugarPagedList<ShareDataPageActivityLogOutput>> HandleAsync(ShareDataPageActivityLogInput command)
        {
            return await _activityLogRep.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.LogType), u => u.LogType == command.LogType)
                .WhereIF(!string.IsNullOrWhiteSpace(command.Action), u => u.Action == command.Action)
                .WhereIF(!string.IsNullOrWhiteSpace(command.TargetType), u => u.TargetType == command.TargetType)
                .WhereIF(!string.IsNullOrWhiteSpace(command.TargetId), u => u.TargetId == command.TargetId)
                .WhereIF(!string.IsNullOrWhiteSpace(command.PartnerId), u => u.PartnerId == command.PartnerId)
                .WhereIF(!string.IsNullOrWhiteSpace(command.SubscriptionId), u => u.SubscriptionId == command.SubscriptionId)
                .WhereIF(!string.IsNullOrWhiteSpace(command.SessionId), u => u.SessionId == command.SessionId)
                .WhereIF(!string.IsNullOrWhiteSpace(command.TransferDirection), u => u.TransferDirection == command.TransferDirection)
                .WhereIF(command.DatatypeId != null, u => u.DatatypeId == command.DatatypeId)
                .WhereIF(!string.IsNullOrWhiteSpace(command.Status), u => u.Status == command.Status)
                .WhereIF(!string.IsNullOrWhiteSpace(command.OperatorName), u => u.OperatorName == command.OperatorName)
                .WhereIF(!string.IsNullOrWhiteSpace(command.Keyword), u => u.Description != null && u.Description.Contains(command.Keyword!))
                .WhereIF(command.FromDate != null, u => u.OccurredAt >= command.FromDate)
                .WhereIF(command.ToDate != null, u => u.OccurredAt <= command.ToDate)
                .Select(u => new ShareDataPageActivityLogOutput
                {
                    ID = u.ID,
                    LogType = u.LogType,
                    Action = u.Action,
                    OccurredAt = u.OccurredAt,
                    Status = u.Status,
                    Description = u.Description,
                    ErrorMessage = u.ErrorMessage,
                    TargetType = u.TargetType,
                    TargetId = u.TargetId,
                    TargetName = u.TargetName,
                    PartnerId = u.PartnerId,
                    PartnerName = u.PartnerName,
                    ChangedFields = u.ChangedFields,
                    SubscriptionId = u.SubscriptionId,
                    SessionId = u.SessionId,
                    TransferDirection = u.TransferDirection,
                    DatatypeId = u.DatatypeId,
                    SerialNbr = u.SerialNbr,
                    PacketNbr = u.PacketNbr,
                    PduType = u.PduType,
                    Format = u.Format,
                    ByteSize = u.ByteSize,
                    RecordCount = u.RecordCount,
                    FilePath = u.FilePath,
                    Hash = u.Hash,
                    OperatorName = u.OperatorName,
                    OperatorIp = u.OperatorIp,
                    CreateTime = u.CreateTime
                })
                .OrderBuilder(command, "", "OccurredAt", false)
                .ToPagedListAsync(command.Page, command.PageSize);
        }

        /// <summary>
        /// Description: Lấy nhật ký hoạt động theo tiêu chí lọc, có chặn trần số bản ghi.
        /// Created date: 2026-08-05
        /// </summary>
        public async Task<List<ShareDataActivityLogOutput>> HandleAsync(ShareDataActivityLogInput command)
        {
            var topN = command.TopN is > 0 ? command.TopN!.Value : DefaultTopN;

            return await _activityLogRep.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.ID), u => u.ID == command.ID)
                .WhereIF(!string.IsNullOrWhiteSpace(command.LogType), u => u.LogType == command.LogType)
                .WhereIF(!string.IsNullOrWhiteSpace(command.Action), u => u.Action == command.Action)
                .WhereIF(!string.IsNullOrWhiteSpace(command.TargetType), u => u.TargetType == command.TargetType)
                .WhereIF(!string.IsNullOrWhiteSpace(command.TargetId), u => u.TargetId == command.TargetId)
                .WhereIF(!string.IsNullOrWhiteSpace(command.PartnerId), u => u.PartnerId == command.PartnerId)
                .WhereIF(!string.IsNullOrWhiteSpace(command.SubscriptionId), u => u.SubscriptionId == command.SubscriptionId)
                .WhereIF(!string.IsNullOrWhiteSpace(command.Status), u => u.Status == command.Status)
                .WhereIF(command.FromDate != null, u => u.OccurredAt >= command.FromDate)
                .WhereIF(command.ToDate != null, u => u.OccurredAt <= command.ToDate)
                .Select(u => new ShareDataActivityLogOutput() { }, true)
                .OrderBy(u => u.OccurredAt, OrderByType.Desc)
                .Take(topN)
                .ToListAsync();
        }

        /// <summary>
        /// Description: Lấy chi tiết 1 bản ghi nhật ký (kèm BeforeJson / AfterJson)
        /// Created date: 2026-08-05
        /// </summary>
        public async Task<ShareDataActivityLogOutput> HandleAsync(ShareDataIdActivityLogInput command)
        {
            var entity = await _activityLogRep.GetByIdAsync(command.ID)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            return entity.Adapt<ShareDataActivityLogOutput>();
        }

        /// <summary>
        /// Description: Thống kê nhanh theo kết quả và theo hành động.
        ///              Gom nhóm tại DB rồi tổng hợp trong bộ nhớ để chỉ chạy 1 truy vấn.
        /// Created date: 2026-08-05
        /// </summary>
        public async Task<ShareDataSummaryActivityLogOutput> HandleAsync(ShareDataSummaryActivityLogInput command)
        {
            var rows = await _activityLogRep.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.LogType), u => u.LogType == command.LogType)
                .WhereIF(!string.IsNullOrWhiteSpace(command.PartnerId), u => u.PartnerId == command.PartnerId)
                .WhereIF(command.FromDate != null, u => u.OccurredAt >= command.FromDate)
                .WhereIF(command.ToDate != null, u => u.OccurredAt <= command.ToDate)
                .GroupBy(u => new { u.Action, u.Status, u.TransferDirection })
                .Select(u => new
                {
                    u.Action,
                    u.Status,
                    u.TransferDirection,
                    Count = SqlFunc.AggregateCount(u.ID)
                })
                .ToListAsync();

            var result = new ShareDataSummaryActivityLogOutput
            {
                Total = rows.Sum(r => r.Count),
                SuccessCount = rows.Where(r => r.Status == ShareDataConst.ActivityStatus.Success).Sum(r => r.Count),
                FailedCount = rows.Where(r => r.Status == ShareDataConst.ActivityStatus.Failed).Sum(r => r.Count),
                SentCount = rows.Where(r => r.TransferDirection == ShareDataConst.TransferDirection.Snd).Sum(r => r.Count),
                ReceivedCount = rows.Where(r => r.TransferDirection == ShareDataConst.TransferDirection.Rcv).Sum(r => r.Count),
                ByAction = rows
                    .Where(r => !string.IsNullOrWhiteSpace(r.Action))
                    .GroupBy(r => r.Action!)
                    .Select(g => new ShareDataActivityCountOutput { Code = g.Key, Count = g.Sum(x => x.Count) })
                    .OrderByDescending(g => g.Count)
                    .ToList()
            };

            return result;
        }
    }
}
