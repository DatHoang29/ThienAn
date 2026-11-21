using Modules.CCTV.Controllers.DashboardItem.Dto;
using Modules.CCTV.Core.Entities;
using Modules.TMS.Core.Entities;
using Modules.TMS.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Services;
using Wolverine;

namespace Modules.CCTV.Controllers.DashboardItem.Queries
{
    public class DashboardItemQueryHandler : IWolverineHandler
    {
        private BaseRepository<CctvDashboardItem> _cctvDashboard;
        private BaseRepository<TmsEquipment> _equipmentRep;
        private BaseCacheService _cache;
        private IMessageBus _bus;
        public DashboardItemQueryHandler(BaseRepository<CctvDashboardItem> cctvDashboard, BaseRepository<TmsEquipment> equipmentRep, BaseCacheService cache, IMessageBus bus)
        {
            _cctvDashboard = cctvDashboard;
            _equipmentRep = equipmentRep;
            _cache = cache;
            _bus = bus;
        }


        /// <summary>
        /// Tìm kiếm danh sách Dashboard theo các tiêu chí lọc.
        /// Nếu không truyền gì → trả về toàn bộ danh sách.
        /// Author: ChuongLP
        /// Date: 2026-06-03
        /// </summary>
        /// <param name="command">truyền Id dashboard va Id thiết bị để tìm kiếm danh sách item tương ứng</param>
        /// <returns>Danh sách DashboardItemOutput <returns>
        public async Task<List<DashboardItemOutput>> HandleAsync(DashboardItemInput command)
        {
            var all = _cache.GetOrAdd($"{CacheConst.Cctv.CctvDashboardItem}", _ =>
            {
                return _cctvDashboard.AsQueryable()

                    .Select(u => new DashboardItemOutput()
                    {
                    }, true)
                    .OrderBy(u => u.CreateTime).ToListAsync().Result;
            });

            // Nếu FE lọc theo Ip → quy đổi Ip sang danh sách EquipmentId để lọc.
            List<string>? equipmentIdsByIp = null;
            if (!string.IsNullOrWhiteSpace(command.Ip))
            {
                equipmentIdsByIp = await _equipmentRep.AsQueryable()
                    .Where(x => x.Ip == command.Ip)
                    .Select(x => x.ID)
                    .ToListAsync();
            }

            var list = all
                .WhereIF(!string.IsNullOrWhiteSpace(command.DashboardId), u => u.DashboardId == command.DashboardId)
                .WhereIF(!string.IsNullOrWhiteSpace(command.EquipmentId), u => u.EquipmentId == command.EquipmentId)
                .WhereIF(equipmentIdsByIp != null, u => u.EquipmentId != null && equipmentIdsByIp!.Contains(u.EquipmentId))
                .ToList();

            // Enrich Ip cho từng item từ EquipmentId (1 query gom tất cả).
            await EnrichIpAsync(list);

            return list;
        }

        /// <summary>
        /// Gán Ip thiết bị cho từng item dựa trên EquipmentId → TmsEquipment.
        /// Author: ChuongLP
        /// Date: 2026-07-28
        /// </summary>
        private async Task EnrichIpAsync(List<DashboardItemOutput> list)
        {
            var equipmentIds = list
                .Where(x => !string.IsNullOrWhiteSpace(x.EquipmentId))
                .Select(x => x.EquipmentId!)
                .Distinct()
                .ToList();

            if (!equipmentIds.Any())
                return;

            var ipMap = (await _equipmentRep.AsQueryable()
                .Where(x => equipmentIds.Contains(x.ID))
                .Select(x => new { x.ID, x.Ip })
                .ToListAsync())
                .ToDictionary(x => x.ID, x => x.Ip);

            foreach (var item in list)
            {
                if (!string.IsNullOrWhiteSpace(item.EquipmentId)
                    && ipMap.TryGetValue(item.EquipmentId, out var ip))
                    item.Ip = ip;
            }
        }
    }
}
