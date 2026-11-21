using Mapster;
using Module.ShareData.Core.Dto.Subscription;
using Module.ShareData.Core.Entities;
using Module.ShareData.Infrastructure;
using Module.ShareData.Infrastructure.Services.ShareDataMappingResolverService;
using Shared.DTO.Constants.Localization;
using Shared.DTO.Enums;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Wolverine;

namespace Module.ShareData.Controllers.Subscription.Queries
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Xử lý truy vấn dữ liệu Đăng ký chia sẻ (ShareDataSubscription)
    /// Created date: 2026-08-04
    /// </summary>
    public class SubscriptionQueryHandler : IWolverineHandler
    {
        private readonly BaseRepository<ShareDataSubscription> _subscriptionRep;
        private readonly ShareDataMappingResolver _mappingResolver;

        public SubscriptionQueryHandler(
            BaseRepository<ShareDataSubscription> subscriptionRep,
            ShareDataMappingResolver mappingResolver)
        {
            _subscriptionRep = subscriptionRep;
            _mappingResolver = mappingResolver;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy danh sách đăng ký theo phân trang
        /// Created date: 2026-08-04
        /// </summary>
        public async Task<SqlSugarPagedList<ShareDataPageSubscriptionOutput>> HandleAsync(ShareDataPageSubscriptionInput command)
        {
            var pagedList = await _subscriptionRep.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.PartnerId), u => u.PartnerId == command.PartnerId)
                .WhereIF(command.Direction.HasValue, u => u.Direction == command.Direction)
                .WhereIF(command.State.HasValue, u => u.State == command.State)
                .WhereIF(command.DatatypeId != null, u => u.DatatypeId == command.DatatypeId)
                .WhereIF(command.FromDate != null, u => u.RequestedAt >= command.FromDate)
                .WhereIF(command.ToDate != null, u => u.RequestedAt <= command.ToDate)
                .Select(u => new ShareDataPageSubscriptionOutput() { }, true)
                .OrderBuilder(command, "")
                .ToPagedListAsync(command.Page, command.PageSize);

            foreach (var item in pagedList.Records ?? [])
                item.Schedule = ParseSchedule(item.ScheduleJson);

            await FillMappingInfoAsync(pagedList.Records);

            return pagedList;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy toàn bộ danh sách đăng ký
        /// Created date: 2026-08-04
        /// </summary>
        public async Task<List<ShareDataSubscriptionOutput>> HandleAsync(ShareDataSubscriptionInput command)
        {
            var list = await _subscriptionRep.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.ID), u => u.ID == command.ID)
                .WhereIF(!string.IsNullOrWhiteSpace(command.PartnerId), u => u.PartnerId == command.PartnerId)
                .WhereIF(command.Direction.HasValue, u => u.Direction == command.Direction)
                .WhereIF(command.State.HasValue, u => u.State == command.State)
                .WhereIF(command.DatatypeId != null, u => u.DatatypeId == command.DatatypeId)
                .OrderBy(u => u.RequestedAt, OrderByType.Desc)
                .Select(u => new ShareDataSubscriptionOutput() { }, true)
                .ToListAsync();

            foreach (var item in list)
                item.Schedule = ParseSchedule(item.ScheduleJson);

            await FillMappingInfoAsync(list);

            return list;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy chi tiết 1 đăng ký theo ID
        /// Created date: 2026-08-04
        /// </summary>
        public async Task<ShareDataSubscriptionOutput> HandleAsync(ShareDataIdSubscriptionInput command)
        {
            var entity = await _subscriptionRep.AsQueryable()
                .Where(u => u.ID == command.ID)
                .Select(u => new ShareDataSubscriptionOutput() { }, true)
                .FirstAsync()
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            entity.Schedule = ParseSchedule(entity.ScheduleJson);
            await FillMappingInfoAsync(new[] { entity });
            return entity;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Nạp MappingState / MappingCode / MappingName cho danh sách đăng ký.
        ///
        ///              Đăng ký KHÔNG lưu liên kết tới hồ sơ ánh xạ — tình trạng được SUY SỐNG mỗi
        ///              lần truy vấn qua ShareDataMappingResolver, dùng đúng luật mà worker dùng
        ///              lúc chạy. Nhờ vậy lưới không bao giờ nói khác với thực tế gửi/nhận.
        ///
        ///              Resolver gom một truy vấn cho cả trang nên không có N+1.
        /// Created date: 2026-09-14
        /// </summary>
        private async Task FillMappingInfoAsync(IEnumerable<ShareDataSubscriptionOutput> items)
        {
            // Mặc định là chưa có hồ sơ: thiếu đối tác / gói tin / chiều thì không suy được,
            // và trạng thái an toàn phải là "không có" để cổng chặn bật đăng ký không cho qua.
            foreach (var item in items)
            {
                item.MappingState = BaseEnums.SubMappingState.NoMapping;
                item.MappingCode = null;
                item.MappingName = null;
            }

            var rows = items
                .Where(x => !string.IsNullOrWhiteSpace(x.PartnerId)
                         && !string.IsNullOrWhiteSpace(x.DatatypeId)
                         && x.Direction != null)
                .ToList();

            if (rows.Count == 0) return;

            var resolutions = await _mappingResolver.ResolveManyAsync(
                rows.Select(x => (x.PartnerId!, x.DatatypeId!, x.Direction!.Value)));

            foreach (var item in rows)
            {
                var key = ShareDataMappingResolver.BuildKey(item.PartnerId, item.DatatypeId, item.Direction);
                if (!resolutions.TryGetValue(key, out var resolution)) continue;

                item.MappingState = resolution.State;
                item.MappingCode = resolution.Code;
                item.MappingName = resolution.Name;
            }
        }

        /// <summary>
        /// Author: HieuNV
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
