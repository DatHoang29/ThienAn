using Module.TOLL.Core.Dto.ChiTietBangGia;
using Modules.TOLL.Core.Entities;
using Modules.TOLL.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;

namespace Modules.TOLL.Controllers.ChiTietBangGia.Queries
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Truy vấn danh sách chi tiết bảng giá thu phí (bảng TollPriceListDetail)
    /// Created date: 08/08/2026
    /// </summary>
    public class ChiTietBangGiaQueryHandler : IWolverineHandler
    {
        private BaseRepository<TollPriceListDetail> _chiTietBangGiaRep;
        private BaseCacheService _cache;

        public ChiTietBangGiaQueryHandler(BaseRepository<TollPriceListDetail> chiTietBangGiaRep, BaseCacheService cache)
        {
            _chiTietBangGiaRep = chiTietBangGiaRep;
            _cache = cache;
        }

        /// <summary>
        /// Danh sách chi tiết bảng giá theo điều kiện tìm kiếm (phân trang)
        /// Author: ChuongLP
        /// Date: 08/08/2026
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<SqlSugarPagedList<ChiTietBangGiaOutput>> HandleAsync(PageChiTietBangGiaInput input)
        {
            if (string.Equals(input.Order, "desc", StringComparison.OrdinalIgnoreCase))
            {
                input.DescStr = "desc";
            }

            return await BuildQuery(input.Code, input.Name, input.PriceListSourceId, input.VehicleTypeSourceId, input.Active)
                .OrderBuilder(input, "", BaseConst.SortFieldConst.UpdateOrCreateTime)
                .ToPagedListAsync(input.Page, input.PageSize);
        }

        /// <summary>
        /// Toàn bộ danh sách chi tiết bảng giá theo điều kiện (không phân trang)
        /// Author: ChuongLP
        /// Date: 08/08/2026
        /// Update: 28/09/2026 - Lấy từ cache (CacheConst.Toll.TollPriceListDetail) rồi lọc trong bộ nhớ;
        ///     cache được xoá khi bấm "Lấy dữ liệu" (TollCacheService, DataType = Master).
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public Task<List<ChiTietBangGiaOutput>> HandleAsync(ChiTietBangGiaInput input)
        {
            var list = _cache.GetOrAdd(CacheConst.Toll.TollPriceListDetail, _ =>
            {
                return BuildQuery(null, null, null, null, null)
                    .OrderBy(a => a.Code)
                    .ToList();
            }) ?? new List<ChiTietBangGiaOutput>();

            var result = list
                .WhereIF(!string.IsNullOrWhiteSpace(input.Code),
                    a => a.Code != null && a.Code.Contains(input.Code!, StringComparison.OrdinalIgnoreCase))
                .WhereIF(!string.IsNullOrWhiteSpace(input.Name),
                    a => a.Name != null && a.Name.Contains(input.Name!, StringComparison.OrdinalIgnoreCase))
                .WhereIF(!string.IsNullOrWhiteSpace(input.PriceListSourceId), a => a.PriceListSourceId == input.PriceListSourceId)
                .WhereIF(!string.IsNullOrWhiteSpace(input.VehicleTypeSourceId), a => a.VehicleTypeSourceId == input.VehicleTypeSourceId)
                .WhereIF(input.Active.HasValue, a => a.Active == input.Active)
                .ToList();

            return Task.FromResult(result);
        }

        /// <summary>
        /// Dựng câu truy vấn dùng chung cho cả 2 chế độ (phân trang / danh sách)
        /// </summary>
        private ISugarQueryable<ChiTietBangGiaOutput> BuildQuery(string? code, string? name, string? priceListSourceId, string? vehicleTypeSourceId, bool? active)
        {
            return _chiTietBangGiaRep.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(code), a => a.Code.Contains(code))
                .WhereIF(!string.IsNullOrWhiteSpace(name), a => a.Name.Contains(name))
                .WhereIF(!string.IsNullOrWhiteSpace(priceListSourceId), a => a.PriceListSourceId == priceListSourceId)
                .WhereIF(!string.IsNullOrWhiteSpace(vehicleTypeSourceId), a => a.VehicleTypeSourceId == vehicleTypeSourceId)
                .WhereIF(active.HasValue, a => a.Active == active)
                .Select(a => new ChiTietBangGiaOutput
                {
                    ID = a.ID,
                    SourceId = a.SourceId,
                    Code = a.Code,
                    Name = a.Name,
                    Price = a.Price,
                    VehicleTypeSourceId = a.VehicleTypeSourceId,
                    TicketTypeSourceId = a.TicketTypeSourceId,
                    PriceTypeSourceId = a.PriceTypeSourceId,
                    PriceListSourceId = a.PriceListSourceId,
                    RoadSourceId = a.RoadSourceId,
                    Active = a.Active,
                    Note = a.Note,
                    SyncTime = a.SyncTime
                }, true);
        }
    }
}
