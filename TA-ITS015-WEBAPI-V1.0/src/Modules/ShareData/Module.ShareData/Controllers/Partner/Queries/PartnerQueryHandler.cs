using Mapster;
using Module.ShareData.Core.Constants;
using Module.ShareData.Core.Dto.Partner;
using Module.ShareData.Core.Entities;
using Module.ShareData.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.DTO.Constants.Localization;
using Shared.DTO.Enums;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;
using Wolverine;

namespace Module.ShareData.Controllers.Partner.Queries
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Xử lý truy vấn dữ liệu Đối tác chia sẻ (ShareDataPartner)
    /// Created date: 2026-08-04
    /// </summary>
    public class PartnerQueryHandler : IWolverineHandler
    {
        private readonly BaseRepository<ShareDataPartner> _partnerRep;
        private readonly BaseCacheService _cache;

        public PartnerQueryHandler(BaseRepository<ShareDataPartner> partnerRep, BaseCacheService cache)
        {
            _partnerRep = partnerRep;
            _cache = cache;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy danh sách đối tác theo phân trang
        /// Created date: 2026-08-04
        /// </summary>
        public async Task<SqlSugarPagedList<ShareDataPagePartnerOutput>> HandleAsync(ShareDataPagePartnerInput command)
        {
            var pagedList = await _partnerRep.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.Code), u => u.Code != null && u.Code.Contains(command.Code))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Name), u => u.Name != null && u.Name.Contains(command.Name))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Address), u => u.Address != null && u.Address.Contains(command.Address))
                .WhereIF(command.Status.HasValue, u => u.Status == command.Status)
                .WhereIF(command.SessionState.HasValue, u => u.SessionState == command.SessionState)
                .Select(u => new ShareDataPagePartnerOutput() { }, true)
                .OrderBuilder(command, "")
                .ToPagedListAsync(command.Page, command.PageSize);

            return pagedList;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy toàn bộ danh sách đối tác theo tiêu chí lọc
        /// Created date: 2026-08-04
        /// </summary>
        public async Task<List<ShareDataPartnerOutput>> HandleAsync(ShareDataPartnerInput command)
        {
            var entities = _cache.GetOrAdd(CacheConst.ShareData.ShareDataPartner, _ =>
            {
                return _partnerRep.AsQueryable()
                    .Select((u) => new ShareDataPartnerOutput()
                    {
                    }, true)
                    .OrderBy(u => u.OrderNo)
                    .OrderBy(u => u.Code)
                    .ToListAsync().Result;
            });


            var filtered = entities
                .WhereIF(!string.IsNullOrWhiteSpace(command.ID), u => u.ID == command.ID)
                .WhereIF(!string.IsNullOrWhiteSpace(command.Code), u => !string.IsNullOrWhiteSpace(u.Code) && u.Code!.Contains(command.Code!, StringComparison.OrdinalIgnoreCase))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Name), u => !string.IsNullOrWhiteSpace(u.Name) && u.Name!.Contains(command.Name!, StringComparison.OrdinalIgnoreCase))
                .WhereIF(command.Status.HasValue, u => u.Status == command.Status)
                .WhereIF(command.SessionState.HasValue, u => u.SessionState == command.SessionState)
                .OrderBy(u => u.OrderNo)
                .ToList();

            return filtered;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy chi tiết 1 đối tác theo ID
        /// Created date: 2026-08-04
        /// </summary>
        public async Task<ShareDataPartnerOutput> HandleAsync(ShareDataIdPartnerInput command)
        {
            var entity = await _partnerRep.GetByIdAsync(command.ID)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            return entity.Adapt<ShareDataPartnerOutput>();
        }
    }
}
