using Modules.TOLL.Core.Entities;
using Modules.WP.Controllers.PriceListDetail.Dto;
using Modules.WP.Infrastructure;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;
using SqlSugar;

namespace Modules.WP.Controllers.PriceListDetail.Queries
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Xử lý các truy vấn đọc chi tiết bảng giá hệ thống WP (PriceListDetail)
    /// Created date: 03/07/2026
    /// Update: 07/10/2026 - Đọc trực tiếp bảng TollPriceListDetail; điền thêm tên bảng giá, tuyến,
    ///     mã/tên trạm vào-ra (TollRoad + TollStation) và tên loại xe (TollMasterData, DataType = VehicleType).
    /// </summary>
    public class PriceListDetailQueryHandler
    {
        private const string VehicleTypeDataType = "VehicleType";

        private readonly BaseRepository<TollPriceListDetail> _priceListDetailRep;
        private readonly BaseRepository<TollPriceList> _priceListRep;
        private readonly BaseRepository<TollRoad> _roadRep;
        private readonly BaseRepository<TollStation> _stationRep;
        private readonly BaseRepository<TollMasterData> _masterRep;
        private readonly BaseCacheService _cache;

        public PriceListDetailQueryHandler(
            BaseRepository<TollPriceListDetail> priceListDetailRep,
            BaseRepository<TollPriceList> priceListRep,
            BaseRepository<TollRoad> roadRep,
            BaseRepository<TollStation> stationRep,
            BaseRepository<TollMasterData> masterRep,
            BaseCacheService cache)
        {
            _priceListDetailRep = priceListDetailRep;
            _priceListRep = priceListRep;
            _roadRep = roadRep;
            _stationRep = stationRep;
            _masterRep = masterRep;
            _cache = cache;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy danh sách Price List Detail theo phân trang
        /// Created date: 14/08/2026
        /// </summary>
        public async Task<SqlSugarPagedList<WpPagePriceListDetailOutput>> HandleAsync(WpPagePriceListDetailInput command)
        {
            // Chỉ cột Giá được sắp xếp theo lựa chọn của người dùng (field = "price"); các cột khác giữ thứ tự mặc định.
            var query = BuildQuery(command.PriceListSourceId, command.VehicleTypeSourceId,
                    command.RoadSourceId, command.Keyword, command.Active);
            if (string.Equals(command.Field, "price", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(command.Order))
            {
                var desc = command.Order.StartsWith("desc", StringComparison.OrdinalIgnoreCase);
                query = query.OrderBy(u => u.Price, desc ? OrderByType.Desc : OrderByType.Asc);
            }

            var page = await ApplyDefaultOrder(query)
                .Select(u => new WpPagePriceListDetailOutput(), true)
                .ToPagedListAsync(command.Page, command.PageSize);

            if (page.Records != null && page.Records.Any())
            {
                var lookup = await LoadLookupAsync();
                foreach (var item in page.Records) Fill(item, lookup);
            }
            return page;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy danh sách Price List Detail theo tiêu chí lọc
        /// Created date: 14/08/2026
        /// </summary>
        public async Task<List<WpPriceListDetailOutput>> HandleAsync(WpPriceListDetailInput command)
        {
            var list = await ApplyDefaultOrder(BuildQuery(command.PriceListSourceId, command.VehicleTypeSourceId,
                    command.RoadSourceId, command.Keyword, command.Active))
                .Select(u => new WpPriceListDetailOutput(), true)
                .ToListAsync();

            if (list.Count > 0)
            {
                var lookup = await LoadLookupAsync();
                foreach (var item in list) Fill(item, lookup);
            }
            return list;
        }

        // ================== helper ==================

        private ISugarQueryable<TollPriceListDetail> BuildQuery(
            string? priceListSourceId, string? vehicleTypeSourceId, string? roadSourceId, string? keyword, bool? active)
        {
            return _priceListDetailRep.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(priceListSourceId), u => u.PriceListSourceId == priceListSourceId)
                .WhereIF(!string.IsNullOrWhiteSpace(vehicleTypeSourceId), u => u.VehicleTypeSourceId == vehicleTypeSourceId)
                .WhereIF(!string.IsNullOrWhiteSpace(roadSourceId), u => u.RoadSourceId == roadSourceId)
                .WhereIF(!string.IsNullOrWhiteSpace(keyword), u => u.Code.Contains(keyword) || u.Name.Contains(keyword))
                .WhereIF(active.HasValue, u => u.Active == active);
        }

        /// <summary>Thứ tự mặc định: loại xe → tuyến → mã (nối sau sắp xếp theo Giá nếu có).</summary>
        private static ISugarQueryable<TollPriceListDetail> ApplyDefaultOrder(ISugarQueryable<TollPriceListDetail> query)
            => query.OrderBy(u => u.VehicleTypeSourceId)
                    .OrderBy(u => u.RoadSourceId)
                    .OrderBy(u => u.Code);

        /// <summary>Dữ liệu tra cứu — các bảng danh mục rất nhỏ nên nạp 1 lần mỗi request.</summary>
        private sealed class Lookup
        {
            public Dictionary<string, string?> PriceListNameByCode { get; init; } = new();
            public Dictionary<string, TollRoad> RoadById { get; init; } = new();
            public Dictionary<string, string?> StationNameById { get; init; } = new();
            public Dictionary<string, string?> VehicleNameById { get; init; } = new();
        }

        private async Task<Lookup> LoadLookupAsync()
        {
            var priceLists = await _priceListRep.AsQueryable().Select(u => new { u.SourceId, u.Name }).ToListAsync();
            var roads = await _roadRep.AsQueryable().ToListAsync();
            var stations = await _stationRep.AsQueryable().Select(s => new { s.StationId, s.Name }).ToListAsync();
            var vehicles = await _masterRep.AsQueryable()
                .Where(m => m.DataType == VehicleTypeDataType)
                .Select(m => new { m.SourceId, m.Name })
                .ToListAsync();

            return new Lookup
            {
                PriceListNameByCode = priceLists.Where(p => !string.IsNullOrEmpty(p.SourceId))
                    .GroupBy(p => p.SourceId!).ToDictionary(g => g.Key, g => g.First().Name),
                RoadById = roads.Where(r => !string.IsNullOrEmpty(r.SourceId))
                    .GroupBy(r => r.SourceId!).ToDictionary(g => g.Key, g => g.First()),
                StationNameById = stations.Where(s => !string.IsNullOrEmpty(s.StationId))
                    .GroupBy(s => s.StationId!).ToDictionary(g => g.Key, g => g.First().Name),
                VehicleNameById = vehicles.Where(v => !string.IsNullOrEmpty(v.SourceId))
                    .GroupBy(v => v.SourceId!).ToDictionary(g => g.Key, g => g.First().Name),
            };
        }

        /// <summary>Điền các trường hiển thị cho 1 bản ghi.</summary>
        private static void Fill(WpPagePriceListDetailOutput item, Lookup lookup)
        {
            if (!string.IsNullOrEmpty(item.PriceListSourceId)
                && lookup.PriceListNameByCode.TryGetValue(item.PriceListSourceId, out var plName))
                item.PriceListName = plName;

            if (!string.IsNullOrEmpty(item.RoadSourceId)
                && lookup.RoadById.TryGetValue(item.RoadSourceId, out var road))
            {
                item.RoadName = road.Name;
                item.StationIdIn = road.StationIdIn;
                item.StationIdOut = road.StationIdOut;
                item.StationInName = StationName(road.StationIdIn, lookup);
                item.StationOutName = StationName(road.StationIdOut, lookup);
            }

            if (!string.IsNullOrEmpty(item.VehicleTypeSourceId)
                && lookup.VehicleNameById.TryGetValue(item.VehicleTypeSourceId, out var vName))
                item.VehicleTypeName = vName;
        }

        private static string? StationName(string? stationId, Lookup lookup)
            => !string.IsNullOrEmpty(stationId) && lookup.StationNameById.TryGetValue(stationId, out var n) && n != null
                ? n
                : stationId;
    }
}
