using Mapster;
using Module.ShareData.Core.Constants;
using Module.ShareData.Core.Dto.ActivityLog;
using Module.ShareData.Core.Entities;
using Module.ShareData.Infrastructure;
using Module.ShareData.Infrastructure.Services.ShareDataConfig;
using Shared.DTO.Constants.Localization;
using Shared.DTO.Enums;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Utility.Utilities;
using Wolverine;

namespace Module.ShareData.Controllers.ActivityLog.Queries
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Xử lý truy vấn Nhật ký hoạt động phân hệ Chia sẻ dữ liệu (ShareDataActivityLog)
    /// Created date: 2026-08-05
    /// </summary>
    public class ActivityLogQueryHandler : IWolverineHandler
    {
        private readonly BaseRepository<ShareDataActivityLog> _activityLogRep;
        private readonly ConfigService _configService;

        public ActivityLogQueryHandler(
            BaseRepository<ShareDataActivityLog> activityLogRep,
            ConfigService configService)
        {
            _activityLogRep = activityLogRep;
            _configService = configService;
        }
        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy nhật ký hoạt động theo phân trang.
        /// Created date: 2026-08-05
        /// </summary>
        public async Task<SqlSugarPagedList<ShareDataPageActivityLogOutput>> HandleAsync(ShareDataPageActivityLogInput command)
        {
            // FE gửi thời gian UTC+0 -> đổi về múi giờ server vì OccurredAt lưu theo giờ local.
            command.FromDate = BaseUtil.TimeServer.GetTimeServer(command.FromDate);
            command.ToDate = BaseUtil.TimeServer.GetTimeServer(command.ToDate);

            return await _activityLogRep.AsQueryable()
                .Where(u => u.ParentId == null)
                .WhereIF(command.LogType != null, u => u.LogType == command.LogType)
                .WhereIF(command.Action.HasValue, u => u.Action == command.Action)
                .WhereIF(command.TargetType.HasValue, u => u.TargetType == command.TargetType)
                .WhereIF(!string.IsNullOrWhiteSpace(command.TargetId), u => u.TargetId == command.TargetId)
                .WhereIF(!string.IsNullOrWhiteSpace(command.PartnerId), u => u.PartnerId == command.PartnerId)
                .WhereIF(!string.IsNullOrWhiteSpace(command.SubscriptionId), u => u.SubscriptionId == command.SubscriptionId)
                .WhereIF(!string.IsNullOrWhiteSpace(command.SessionId), u => u.SessionId == command.SessionId)
                .WhereIF(command.TransferDirection != null, u => u.TransferDirection == command.TransferDirection)
                .WhereIF(command.DatatypeId != null, u => u.DatatypeId == command.DatatypeId)
                .WhereIF(command.Success != null, u => u.Success == command.Success)
                .WhereIF(!string.IsNullOrWhiteSpace(command.OperatorName), u => u.OperatorName == command.OperatorName)
                .WhereIF(!string.IsNullOrWhiteSpace(command.Keyword), u => u.Description != null && u.Description.Contains(command.Keyword!))
                .WhereIF(command.FromDate != null, u => u.OccurredAt >= command.FromDate)
                .WhereIF(command.ToDate != null, u => u.OccurredAt <= command.ToDate)
                .Select(u => new ShareDataPageActivityLogOutput
                {
                }, true)
                .OrderBuilder(command, "")
                .ToPagedListAsync(command.Page, command.PageSize);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy nhật ký hoạt động theo tiêu chí lọc, có chặn trần số bản ghi.
        /// Created date: 2026-08-05
        /// </summary>
        public async Task<List<ShareDataActivityLogOutput>> HandleAsync(ShareDataActivityLogInput command)
        {
            var limit = await _configService.Get();
            var topN = command.TopN is > 0 ? command.TopN!.Value : limit.LogListDefaultTop;

            // FE gửi thời gian UTC+0 -> đổi về múi giờ server vì OccurredAt lưu theo giờ local.
            command.FromDate = BaseUtil.TimeServer.GetTimeServer(command.FromDate);
            command.ToDate = BaseUtil.TimeServer.GetTimeServer(command.ToDate);

            return await _activityLogRep.AsQueryable()
                .Where(u => u.ParentId == null)
                .WhereIF(!string.IsNullOrWhiteSpace(command.ID), u => u.ID == command.ID)
                .WhereIF(command.LogType != null, u => u.LogType == command.LogType)
                .WhereIF(command.Action.HasValue, u => u.Action == command.Action)
                .WhereIF(command.TargetType.HasValue, u => u.TargetType == command.TargetType)
                .WhereIF(!string.IsNullOrWhiteSpace(command.TargetId), u => u.TargetId == command.TargetId)
                .WhereIF(!string.IsNullOrWhiteSpace(command.PartnerId), u => u.PartnerId == command.PartnerId)
                .WhereIF(!string.IsNullOrWhiteSpace(command.SubscriptionId), u => u.SubscriptionId == command.SubscriptionId)
                .WhereIF(command.Success != null, u => u.Success == command.Success)
                .WhereIF(command.FromDate != null, u => u.OccurredAt >= command.FromDate)
                .WhereIF(command.ToDate != null, u => u.OccurredAt <= command.ToDate)
                .Select(u => new ShareDataActivityLogOutput() { }, true)
                .OrderBy(u => u.OccurredAt, OrderByType.Desc)
                .Take(topN)
                .ToListAsync();
        }
        /// <summary>
        /// Author: HieuNV
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
        /// Author: HieuNV
        /// Description: Thống kê nhanh theo kết quả và theo hành động.
        /// Created date: 2026-08-05
        /// </summary>
        public async Task<ShareDataSummaryActivityLogOutput> HandleAsync(ShareDataSummaryActivityLogInput command)
        {
            // FE gửi thời gian UTC+0 -> đổi về múi giờ server vì OccurredAt lưu theo giờ local.
            command.FromDate = BaseUtil.TimeServer.GetTimeServer(command.FromDate);
            command.ToDate = BaseUtil.TimeServer.GetTimeServer(command.ToDate);

            var rows = await _activityLogRep.AsQueryable()
                .Where(u => u.ParentId == null)
                .WhereIF(command.LogType != null, u => u.LogType == command.LogType)
                .WhereIF(!string.IsNullOrWhiteSpace(command.PartnerId), u => u.PartnerId == command.PartnerId)
                .WhereIF(command.FromDate != null, u => u.OccurredAt >= command.FromDate)
                .WhereIF(command.ToDate != null, u => u.OccurredAt <= command.ToDate)
                .GroupBy(u => new { u.Action, u.Success, u.TransferDirection })
                .Select(u => new
                {
                    u.Action,
                    u.Success,
                    u.TransferDirection,
                    Count = SqlFunc.AggregateCount(u.ID)
                })
                .ToListAsync();
            var result = new ShareDataSummaryActivityLogOutput
            {
                Total = rows.Sum(r => r.Count),
                SuccessCount = rows.Where(r => r.Success == BaseEnums.SuccessEnums.Success).Sum(r => r.Count),
                FailedCount = rows.Where(r => r.Success == BaseEnums.SuccessEnums.Fail).Sum(r => r.Count),
                SentCount = rows.Where(r => r.TransferDirection == BaseEnums.TransferDirection.SND).Sum(r => r.Count),
                ReceivedCount = rows.Where(r => r.TransferDirection == BaseEnums.TransferDirection.RCV).Sum(r => r.Count),
                ByAction = rows
                    .Where(r => r.Action != null)
                    .GroupBy(r => r.Action!.Value)
                    // Code trả về tên enum chứ không phải số, để FE khỏi phải tra bảng khi hiển thị.
                    .Select(g => new ShareDataActivityCountOutput { Code = g.Key.ToString(), Count = g.Sum(x => x.Count) })
                    .OrderByDescending(g => g.Count)
                    .ToList()
            };

            return result;
        }

        /// <summary>
        /// Description: Lấy cây một phiên truyền nhận: dòng cha kèm các dòng bước lồng trong Children.
        /// Created date: 02/10/2026
        /// </summary>
        public async Task<List<ShareDataActivityLogOutput>> HandleAsync(ShareDataStepActivityLogInput command)
        {
            var nodes = _activityLogRep.AsQueryable()
                .Where(u => u.ID == command.ID || u.ParentId == command.ID)
                .OrderBy(u => u.StepNbr);

            var tree = (await nodes.ToTreeAsync(u => u.Children, u => u.ParentId, null))
                .Adapt<List<ShareDataActivityLogOutput>>();

            if (tree.Count == 0)
                return (await nodes.ToListAsync()).Adapt<List<ShareDataActivityLogOutput>>();

            return tree;
        }
    }
}
