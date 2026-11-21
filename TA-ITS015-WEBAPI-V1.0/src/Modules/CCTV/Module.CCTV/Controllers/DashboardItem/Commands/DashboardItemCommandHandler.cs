using Mapster;
using Modules.CCTV.Controllers.DashboardItem.Dto;
using Modules.CCTV.Core.Entities;
using Modules.CCTV.Core.Exceptions;
using Modules.TMS.Core.Entities;
using Modules.TMS.Infrastructure;
using Shared.Core.Extensions;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;
using Wolverine;

namespace Module.CCTV.Controllers.Dashboard.Commands
{
    public class DashboardItemCommandHandler : IWolverineHandler
    {
        private BaseRepository<CctvDashboardItem> _cctvDashboardItemRep;
        private BaseRepository<CctvDashboard> _cctvDashboardRep;
        private BaseRepository<TmsEquipment> _equipmentRep;
        private BaseCacheService _cache;

        public DashboardItemCommandHandler(BaseRepository<CctvDashboardItem> cctvDashboardItemRep, BaseRepository<CctvDashboard> cctvDashboardRep, BaseRepository<TmsEquipment> equipmentRep, BaseCacheService cache)
        {
            _cctvDashboardItemRep = cctvDashboardItemRep;
            _cctvDashboardRep = cctvDashboardRep;
            _equipmentRep = equipmentRep;
            _cache = cache;
        }

        /// <summary>
        /// Quy đổi Ip → EquipmentId cho các payload chỉ truyền Ip (bỏ trống EquipmentId).
        /// Gom tất cả Ip lại tra 1 lần để tránh N query.
        /// Author: ChuongLP
        /// Date: 2026-07-28
        /// </summary>
        private async Task ResolveEquipmentIdsByIpAsync(List<DashboardItemPayload> items)
        {
            var ips = items
                .Where(i => string.IsNullOrWhiteSpace(i.EquipmentId) && !string.IsNullOrWhiteSpace(i.Ip))
                .Select(i => i.Ip!)
                .Distinct()
                .ToList();

            if (!ips.Any())
                return;

            var map = (await _equipmentRep.AsQueryable()
                    .Where(x => x.Ip != null && ips.Contains(x.Ip))
                    .Select(x => new { x.ID, x.Ip })
                    .ToListAsync())
                .GroupBy(x => x.Ip!)
                .ToDictionary(g => g.Key, g => g.First().ID);

            foreach (var item in items)
            {
                if (string.IsNullOrWhiteSpace(item.EquipmentId)
                    && !string.IsNullOrWhiteSpace(item.Ip)
                    && map.TryGetValue(item.Ip, out var eqId))
                    item.EquipmentId = eqId;
            }
        }

        /// <summary>
        /// Thêm mới 1 record vào bảng CctvDashboardItem
        /// Author: ChuongLP
        /// Date: 2026-06-03
        /// </summary>
        public async Task HandleAsync(AddDashboardItemInput command)
        {
            // Bước 1 Kiểm tra DashboardId đã tồn tại chưa
            if (!await _cctvDashboardRep.IsAnyAsync(u => u.ID == command.DashboardId))
                throw Oops.Oh(BaseMsg.BaseException.NotExist);

            if (!command.Items.Any())
                return;

            var now = DateTime.Now;
            var userName = App.User.GetUsername();
            // Quy đổi Ip → EquipmentId cho các item chỉ truyền Ip (đặt sau khi đã có Items).
            await ResolveEquipmentIdsByIpAsync(command.Items);

            // Bước 2 Thêm mới các record vào bảng CctvDashboardItem
            var entities = command.Items.Select(item => new CctvDashboardItem
            {
                ID = Guid.NewGuid().ToString(),
                DashboardId = command.DashboardId,
                EquipmentId = item.EquipmentId,
                X = item.X,
                Y = item.Y,
                W = item.W,
                H = item.H,
                CreateUId = userName,
                CreateTime = now,
            }).ToList();

            // Bước 3 Lưu vào database
            await RunInTransactionAsync(async () =>
            {
                foreach (var item in entities)
                    await _cctvDashboardItemRep.InsertWithDiffLogAsync(item);
            });
            _cache.RemoveByPrefixKey(CacheConst.Cctv.CctvDashboardItem);
        }

        /// <summary>
        /// Chỉnh sửa 1 record trong bảng CctvDashboardItem
        /// Author: ChuongLP
        /// Date: 2026-06-03
        /// </summary>
        public async Task HandleAsync(UpdateDashboardItemInput command)
        {
            // Kiểm tra Dashboard tồn tại không
            if (!await _cctvDashboardRep.IsAnyAsync(u => u.ID == command.DashboardId))
                throw Oops.Oh(BaseMsg.BaseException.NotExist);

            var now = DateTime.Now;
            var username = App.User.GetUsername();

            // (Update 21/10/2026) Quy đổi Ip → EquipmentId TRƯỚC khi xóa, rồi xóa + insert lại trong 1 transaction:
            // lỗi giữa chừng → rollback, bố cục cũ còn nguyên (trước đây có thể mất sạch item).
            await ResolveEquipmentIdsByIpAsync(command.Items);

            var entities = command.Items.Select(item => new CctvDashboardItem
            {
                ID = Guid.NewGuid().ToString(),
                DashboardId = command.DashboardId,
                EquipmentId = item.EquipmentId,
                X = item.X,
                Y = item.Y,
                W = item.W,
                H = item.H,
                CreateUId = username,
                CreateTime = now,
            }).ToList();

            await RunInTransactionAsync(async () =>
            {
                // Bước 1: Xóa toàn bộ item cũ của dashboard này
                await _cctvDashboardItemRep.DeleteAsync(u => u.DashboardId == command.DashboardId);

                // Bước 2: Insert lại toàn bộ items FE gửi lên (rỗng = layout không còn camera)
                foreach (var item in entities)
                    await _cctvDashboardItemRep.InsertWithDiffLogAsync(item);
            });

            _cache.RemoveByPrefixKey(CacheConst.Cctv.CctvDashboardItem);
        }

        /// <summary>
        /// Hàm xóa 1 record trong bảng CctvDashboardItem
        /// Author: ChuongLP
        /// Date: 2026-06-03
        /// </summary>  
        public async Task HandleAsync(DeleteDashboardItemInput command)
        {
            var cctvDashboardItemRep = await _cctvDashboardItemRep.GetByIdAsync(command.ID)
                         ?? throw Oops.Oh(BaseMsg.BaseException.NotExist);

            await _cctvDashboardItemRep.DeleteAsync(l => l.ID == cctvDashboardItemRep.ID);
            _cache.RemoveByPrefixKey(CacheConst.Cctv.CctvDashboardItem);
        }

        /// <summary>
        /// Chạy các thao tác ghi trong 1 transaction (mẫu giống ShareData MappingCommandHandler).
        /// Date: 21/10/2026
        /// </summary>
        private async Task RunInTransactionAsync(Func<Task> action)
        {
            var db = _cctvDashboardItemRep.Context;
            await db.AsTenant().BeginTranAsync();
            try
            {
                await action();
                await db.AsTenant().CommitTranAsync();
            }
            catch
            {
                await db.AsTenant().RollbackTranAsync();
                throw;
            }
        }
    }
}
