using Mapster;
using Module.ShareData.Core.Constants;
using Module.ShareData.Core.Dto.Session;
using Module.ShareData.Core.Entities;
using Module.ShareData.Infrastructure;
using Shared.DTO.Constants.Localization;
using Shared.DTO.Enums;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Wolverine;

namespace Module.ShareData.Controllers.Session.Queries
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Xử lý truy vấn Phiên kết nối
    /// Created date: 2026-08-05
    /// </summary>
    public class SessionQueryHandler : IWolverineHandler
    {
        private readonly BaseRepository<ShareDataSession> _sessionRep;
        private readonly BaseRepository<ShareDataSubscription> _subscriptionRep;
        private readonly BaseRepository<ShareDataPartner> _partnerRep;

        public SessionQueryHandler(
            BaseRepository<ShareDataSession> sessionRep,
            BaseRepository<ShareDataSubscription> subscriptionRep,
            BaseRepository<ShareDataPartner> partnerRep)
        {
            _sessionRep = sessionRep;
            _subscriptionRep = subscriptionRep;
            _partnerRep = partnerRep;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy danh sách phiên kết nối theo phân trang
        /// Created date: 2026-08-05
        /// </summary>
        public async Task<SqlSugarPagedList<ShareDataPageSessionOutput>> HandleAsync(ShareDataPageSessionInput command)
        {
            return await _sessionRep.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.PartnerId), u => u.PartnerId == command.PartnerId)
                .WhereIF(command.Direction.HasValue, u => u.Direction == command.Direction)
                .WhereIF(command.State.HasValue, u => u.State == command.State)
                .WhereIF(command.FromDate != null, u => u.StartedAt >= command.FromDate)
                .WhereIF(command.ToDate != null, u => u.StartedAt <= command.ToDate)
                .Select(u => new ShareDataPageSessionOutput() { }, true)
                .OrderBuilder(command, "", "StartedAt", false)
                .ToPagedListAsync(command.Page, command.PageSize);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy danh sách phiên theo tiêu chí lọc.
        ///              LatestPerPartner = true (mặc định) chỉ giữ phiên mới nhất của mỗi đối tác —
        ///              đúng nhu cầu panel đối tác ở màn vận hành, tránh kéo cả lịch sử phiên.
        /// Created date: 2026-08-05
        /// </summary>
        public async Task<List<ShareDataSessionOutput>> HandleAsync(ShareDataSessionInput command)
        {
            var list = await _sessionRep.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.ID), u => u.ID == command.ID)
                .WhereIF(!string.IsNullOrWhiteSpace(command.PartnerId), u => u.PartnerId == command.PartnerId)
                .WhereIF(command.Direction.HasValue, u => u.Direction == command.Direction)
                .WhereIF(command.State.HasValue, u => u.State == command.State)
                .OrderBy(u => u.StartedAt, OrderByType.Desc)
                .Select(u => new ShareDataSessionOutput() { }, true)
                .ToListAsync();

            if (command.LatestPerPartner != false)
            {
                list = list
                    .GroupBy(u => u.PartnerId ?? string.Empty)
                    .Select(g => g.OrderByDescending(x => x.StartedAt).First())
                    .ToList();
            }

            return list;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy chi tiết 1 phiên kết nối theo ID
        /// Created date: 2026-08-05
        /// </summary>
        public async Task<ShareDataSessionOutput> HandleAsync(ShareDataIdSessionInput command)
        {
            var entity = await _sessionRep.GetByIdAsync(command.ID)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            return entity.Adapt<ShareDataSessionOutput>();
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Số liệu tổng hợp cho các thẻ thống kê đầu màn vận hành.
        ///              Gom về 2 truy vấn nhẹ rồi cộng trong bộ nhớ, vì số dòng ở đây rất nhỏ.
        /// Created date: 2026-08-05
        /// </summary>
        public async Task<ShareDataSummarySessionOutput> HandleAsync(ShareDataSummarySessionInput command)
        {
            var sessions = await _sessionRep.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.PartnerId), u => u.PartnerId == command.PartnerId)
                .Select(u => new { u.PartnerId, u.State, u.PacketsSent, u.PacketsRecv })
                .ToListAsync();

            var subscriptions = await _subscriptionRep.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.PartnerId), u => u.PartnerId == command.PartnerId)
                .Select(u => new { u.PartnerId, u.State, u.Direction })
                .ToListAsync();

            // Đối tác nào đang kết nối — xét cờ SessionState trên đối tác (nút Kết nối/Ngắt kết nối
            // của FE điều khiển) và phiên ACTIVE trong bảng phiên, giống IsPartnerConnectedAsync
            // ở ShareDataSubscriptionCommandHandler.
            var connectedPartnerIds = await _partnerRep.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.PartnerId), u => u.ID == command.PartnerId)
                .Where(u => u.SessionState == BaseEnums.SessionState.Connected)
                .Select(u => u.ID)
                .ToListAsync();

            var connected = new HashSet<string>(
                connectedPartnerIds
                    .Concat(sessions
                        .Where(s => s.State == BaseEnums.SessionFsmState.Active)
                        .Select(s => s.PartnerId))
                    .Where(id => !string.IsNullOrWhiteSpace(id))!);

            return new ShareDataSummarySessionOutput
            {
                ActiveSessions = sessions.Count(s => s.State == BaseEnums.SessionFsmState.Active),
                PacketsSent = sessions.Sum(s => s.PacketsSent ?? 0),
                PacketsRecv = sessions.Sum(s => s.PacketsRecv ?? 0),
                ActiveSubscriptions = subscriptions.Count(s => s.State == BaseEnums.SubSubscriptionState.Active
                    && s.PartnerId != null && connected.Contains(s.PartnerId)),
                PendingApprovals = subscriptions.Count(s => s.State == BaseEnums.SubSubscriptionState.Pending
                    && s.Direction == BaseEnums.Direction.Inbound),

                // TODO(BE): nối vào ShareDataAlertLog khi dựng màn "Cảnh báo & lỗi".
                UnackedAlerts = 0
            };
        }
    }
}
