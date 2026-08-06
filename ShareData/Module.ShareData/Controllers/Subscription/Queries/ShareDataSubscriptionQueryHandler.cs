using Mapster;
using Module.ShareData.Core.Dto.Subscription;
using Module.ShareData.Core.Entities;
using Module.ShareData.Infrastructure;
using Newtonsoft.Json;
using Shared.DTO.Constants.Localization;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Wolverine;

namespace Module.ShareData.Controllers.Subscription.Queries
{
    /// <summary>
    /// Description: Xử lý truy vấn dữ liệu Đăng ký chia sẻ (ShareDataSubscription)
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataSubscriptionQueryHandler : IWolverineHandler
    {
        private readonly BaseRepository<ShareDataSubscription> _subscriptionRep;

        public ShareDataSubscriptionQueryHandler(BaseRepository<ShareDataSubscription> subscriptionRep)
        {
            _subscriptionRep = subscriptionRep;
        }

        /// <summary>
        /// Description: Lấy danh sách đăng ký theo phân trang (kèm tên đối tác)
        /// Created date: 2026-08-04
        /// </summary>
        public async Task<SqlSugarPagedList<ShareDataPageSubscriptionOutput>> HandleAsync(ShareDataPageSubscriptionInput command)
        {
            var pagedList = await _subscriptionRep.AsQueryable()
                .LeftJoin<ShareDataPartner>((u, p) => u.PartnerId == p.ID)
                .WhereIF(!string.IsNullOrWhiteSpace(command.PartnerId), (u, p) => u.PartnerId == command.PartnerId)
                .WhereIF(!string.IsNullOrWhiteSpace(command.Direction), (u, p) => u.Direction == command.Direction)
                .WhereIF(!string.IsNullOrWhiteSpace(command.State), (u, p) => u.State == command.State)
                .WhereIF(!string.IsNullOrWhiteSpace(command.Mode), (u, p) => u.Mode == command.Mode)
                .WhereIF(command.DatatypeId != null, (u, p) => u.DatatypeId == command.DatatypeId)
                .WhereIF(command.FromDate != null, (u, p) => u.RequestedAt >= command.FromDate)
                .WhereIF(command.ToDate != null, (u, p) => u.RequestedAt <= command.ToDate)
                .Select((u, p) => new ShareDataPageSubscriptionOutput()
                {
                    PartnerName = p.Name,
                    PartnerCode = p.Code
                }, true)
                .OrderBuilder(command, "", "RequestedAt", false)
                .ToPagedListAsync(command.Page, command.PageSize);

            foreach (var item in pagedList.Records ?? Enumerable.Empty<ShareDataPageSubscriptionOutput>())
                item.Schedule = ParseSchedule(item.ScheduleJson);

            return pagedList;
        }

        /// <summary>
        /// Description: Lấy toàn bộ danh sách đăng ký theo tiêu chí lọc (không phân trang).
        ///              KHÔNG cache vì trạng thái đăng ký thay đổi liên tục theo thao tác vận hành.
        /// Created date: 2026-08-04
        /// </summary>
        public async Task<List<ShareDataSubscriptionOutput>> HandleAsync(ShareDataSubscriptionInput command)
        {
            var list = await _subscriptionRep.AsQueryable()
                .LeftJoin<ShareDataPartner>((u, p) => u.PartnerId == p.ID)
                .WhereIF(!string.IsNullOrWhiteSpace(command.ID), (u, p) => u.ID == command.ID)
                .WhereIF(!string.IsNullOrWhiteSpace(command.PartnerId), (u, p) => u.PartnerId == command.PartnerId)
                .WhereIF(!string.IsNullOrWhiteSpace(command.Direction), (u, p) => u.Direction == command.Direction)
                .WhereIF(!string.IsNullOrWhiteSpace(command.State), (u, p) => u.State == command.State)
                .WhereIF(!string.IsNullOrWhiteSpace(command.Mode), (u, p) => u.Mode == command.Mode)
                .WhereIF(command.DatatypeId != null, (u, p) => u.DatatypeId == command.DatatypeId)
                .OrderBy((u, p) => u.RequestedAt, OrderByType.Desc)
                .Select((u, p) => new ShareDataSubscriptionOutput()
                {
                    PartnerName = p.Name,
                    PartnerCode = p.Code
                }, true)
                .ToListAsync();

            foreach (var item in list)
                item.Schedule = ParseSchedule(item.ScheduleJson);

            return list;
        }

        /// <summary>
        /// Description: Lấy chi tiết 1 đăng ký theo ID
        /// Created date: 2026-08-04
        /// </summary>
        public async Task<ShareDataSubscriptionOutput> HandleAsync(ShareDataIdSubscriptionInput command)
        {
            var entity = await _subscriptionRep.AsQueryable()
                .LeftJoin<ShareDataPartner>((u, p) => u.PartnerId == p.ID)
                .Where((u, p) => u.ID == command.ID)
                .Select((u, p) => new ShareDataSubscriptionOutput()
                {
                    PartnerName = p.Name,
                    PartnerCode = p.Code
                }, true)
                .FirstAsync()
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            entity.Schedule = ParseSchedule(entity.ScheduleJson);
            return entity;
        }

        /// <summary>
        /// Description: Giải mã ScheduleJson thành đối tượng lịch gửi
        /// Created date: 2026-08-04
        /// </summary>
        private static ShareDataScheduleDto? ParseSchedule(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;

            try
            {
                return JsonConvert.DeserializeObject<ShareDataScheduleDto>(json);
            }
            catch
            {
                // Dữ liệu lịch hỏng không được làm chết cả danh sách.
                return null;
            }
        }
    }
}
