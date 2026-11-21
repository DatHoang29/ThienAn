using Modules.TMS.Infrastructure.Services.Zone.Rules;
using Modules.CfgSystem.Infrastructure.Services.Refresh;
using Modules.DataTransporter.ITS.Infrastructure.Services;
using Modules.CfgSystem.Core.Refresh;
using Modules.TMS.Controllers.Zone.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Exceptions;
using Modules.TMS.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;
using Shared.Core.Extensions;
using Shared.Utility.Utilities;

namespace Modules.TMS.Controllers.Zone.Commands
{
    /// <summary>
    /// Author: KhoaND
    /// Description: Lớp nghiệp vụ xử lý các lệnh vào của đối tượng tmsZone
    /// Created date: 11/08/2024
    /// </summary>
    public class ZoneCommandHandler : IWolverineHandler
    {
        private BaseRepository<TmsZone> _tmsZoneRep;
        private BaseRepository<TmsMapLocation> _tmsMapLocationRep;
        private BaseRepository<TmsZoneEquipment> _zoneEquipmentRep;
        private BaseCacheService _cache;
        private RefreshModifyService _refreshModify;

        public ZoneCommandHandler(BaseRepository<TmsZone> tmsZoneRep, BaseRepository<TmsMapLocation> tmsMapLocationRep,
            BaseRepository<TmsZoneEquipment> zoneEquipmentRep, BaseCacheService cache, RefreshModifyService refreshModify)
        {
            _tmsZoneRep = tmsZoneRep;
            _tmsMapLocationRep = tmsMapLocationRep;
            _zoneEquipmentRep = zoneEquipmentRep;
            _cache = cache;
            _refreshModify = refreshModify;
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Kiểm tra có khu vực nào đang được sử dụng trên bản đồ không (geojson của TmsMapLocation có chứa id zone)
        /// </summary>
        // KienLT - 26/10/2026: nhận nhiều ID (cả nhánh con cháu khi xoá dây chuyền), gộp một truy vấn OR.
        private async Task<bool> IsAnyZoneInUseAsync(IReadOnlyCollection<string> zoneIds)
        {
            var ids = zoneIds.Where(id => !string.IsNullOrWhiteSpace(id)).ToList();
            if (ids.Count == 0) return false;

            var anyId = Expressionable.Create<TmsMapLocation>();
            foreach (var id in ids) anyId.Or(m => m.GeoJson!.Contains(id));
            return await _tmsMapLocationRep.AsQueryable().Where(m => m.GeoJson != null).Where(anyId.ToExpression()).AnyAsync();
        }

        /// <summary>
        /// Author: KienLT
        /// Description: Xoá các zone được chọn + MỌI zone con cháu (xoá mềm) và liên kết thiết bị của chúng, trong một
        /// giao dịch. Zone nào trong nhánh đang dùng trên bản đồ ⇒ không xoá gì. FE đã hỏi xác nhận kèm số zone con.
        /// Created date: 26/10/2026
        /// </summary>
        private async Task DeleteWithDescendantsAsync(IReadOnlyCollection<string> selectedIds, string? notifyKey, string action)
        {
            var zones = await _tmsZoneRep.AsQueryable().Where(z => z.IsDelete == null)
                .Select(z => new TmsZone { ID = z.ID, Pid = z.Pid }).ToListAsync();
            var ids = TmsZoneDeleteRules.WithDescendants(selectedIds, zones.Select(z => (z.ID, z.Pid)));

            if (await IsAnyZoneInUseAsync(ids))
                throw Oops.Oh(BaseMsg.TmsZone.Exception.ZoneInUse);

            var db = _tmsZoneRep.Context;
            await db.Ado.BeginTranAsync();
            try
            {
                await _tmsZoneRep.SoftDeleteAsync<TmsZone>(z => ids.Contains(z.ID));
                // Liên kết của zone đã xoá vẫn được tính khi chọn zone chính của camera (xem ZoneSpeedConfigQueryHandler) ⇒ xoá theo.
                await _zoneEquipmentRep.DeleteAsync(ze => ids.Contains(ze.ZoneId!));
                await db.Ado.CommitTranAsync();
            }
            catch
            {
                await db.Ado.RollbackTranAsync();
                throw;
            }

            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsZone);
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsZoneEquipment);
            _cache.RemoveByPrefixKey(CacheConst.Vds.VdsEventSnapshot); // snapshot sự kiện VDS dựng từ danh mục zone/thiết bị
            // Ma refresh do route 'catalog:zone' / 'catalog:zoneequipment' trong SysConfigData quyet dinh.
            await NotifyAsync(notifyKey, action);
            await _refreshModify.NotifyAsync(new RefreshModifyRequest("catalog", "zoneequipment", notifyKey, action));
        }
        /// <summary>
        /// Author: KhoaND SonTH
        /// Description: Hàm lưu thông tin xuống bảng tmsZone
        /// Created date: 11/08/2024
        /// </summary>
        public async Task HandleAsync(AddZoneInput command)
        {
            command.Code = command.Code?.Trim(); // ChuongLP - 02/04/2025 - Loại bỏ khoảng trắng ở đầu 

            if (await _tmsZoneRep.IsAnyAsync(u => u.Code == command.Code))
                throw Oops.Oh(BaseMsg.BaseException.Exist, Furion.Localization.L.Text[BaseMsg.BaseEntity.CodeOrName]);
            command.CreateUId = null;

            await _tmsZoneRep.InsertWithDiffLogAsync(command.Adapt<TmsZone>());
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsZone);
            // Ma refresh do route 'catalog:zone' trong SysConfigData quyet dinh.
            await NotifyAsync(command.Code, "add");
        }
        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm cập nhật thông tin xuống bảng tmsZone
        /// Created date: 11/08/2024
        /// </summary>
        public async Task HandleAsync(UpdateZoneInput command)
        {
            command.Code = command.Code?.Trim(); // ChuongLP - 02/04/2025 - Loại bỏ khoảng trắng ở đầu 

            if (await _tmsZoneRep.IsAnyAsync(u => (u.Code == command.Code) && u.ID != command.ID))
                throw Oops.Oh(BaseMsg.BaseException.Exist, Furion.Localization.L.Text[BaseMsg.BaseEntity.CodeOrName]);

            var entity = await _tmsZoneRep.GetByIdAsync(command.ID)
                         ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            await _tmsZoneRep.UpdateWithDiffLogAsync(command.Adapt(entity), false);
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsZone);
            // Ma refresh do route 'catalog:zone' trong SysConfigData quyet dinh.
            await NotifyAsync(command.ID, "update");
        }
        /// <summary>
        /// Author: KhoaND SonTH
        /// Description: Hàm xóa 1 record trong bảng tmsZone
        /// Created date: 11/08/2024
        /// </summary>
        public async Task HandleAsync(DeleteZoneInput command)
        {
            _ = await _tmsZoneRep.GetFirstAsync(u => u.ID == command.ID)
                ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            // KienLT - 26/10/2026: xoá cả nhánh con cháu (trước chỉ xoá dòng này ⇒ zone con mồ côi).
            // Không cho xóa khi khu vực (hoặc con cháu) đang được sử dụng trên bản đồ.
            await DeleteWithDescendantsAsync([command.ID], command.ID, "delete");
        }
        /// <summary>
        /// Author: KhoaND SonTH
        /// Description: Hàm xóa nhiều record trong bảng tmsZone
        /// Created date: 11/08/2024
        /// </summary>
        public async Task HandleAsync(List<DeleteZoneInput> command)
        {
            var ids = command.Select(u => u.ID).ToList();

            var list = _tmsZoneRep.AsQueryable().Where(u => ids.Contains(u.ID));

            if (list?.Count() != ids.Count)
                throw Oops.Oh(BaseMsg.BaseException.DataNotMatch);

            // KienLT - 26/10/2026: xoá cả nhánh con cháu; khu vực nào (kể cả con cháu) đang dùng trên bản đồ ⇒ không xoá.
            await DeleteWithDescendantsAsync(ids, null, "batch-delete");
        }

        /// <summary>Kiểm toàn bộ batch trước khi ghi; chỉ ghi bốn cột và notify một lần sau commit.</summary>
        public async Task<BatchUpdateZoneSpeedOutput> HandleAsync(BatchUpdateZoneSpeedInput command)
        {
            if (!TmsZoneSpeedBatchRules.IsValid(command)) throw Oops.Oh(BaseMsg.BaseException.DataNotMatch);
            var items = command.Items!;
            var ids = items.Select(i => i.Id!.Trim()).ToList();
            var db = _tmsZoneRep.Context;
            await db.Ado.BeginTranAsync(System.Data.IsolationLevel.Serializable);
            try
            {
                var zones = await _tmsZoneRep.AsQueryable()
                    .Where(z => z.IsDelete == null && ids.Contains(z.ID))
                    .Select(z => new TmsZone { ID = z.ID, TenantId = z.TenantId }).ToListAsync();
                if (zones.Count != ids.Count) throw Oops.Oh(BaseMsg.BaseException.DataNotExist);
                var byId = zones.ToDictionary(z => z.ID, StringComparer.OrdinalIgnoreCase);
                if (ids.Any(id => !byId.ContainsKey(id))) throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

                // SetColumns (biểu thức) không đi qua AOP tự điền UpdateTime của framework ⇒ gán tay, dùng giờ server.
                var updateTime = BaseUtil.TimeServer.GetTimeServer();
                var updateUId = App.User?.GetUsername();
                foreach (var item in items.OrderBy(i => i.Id, StringComparer.OrdinalIgnoreCase))
                {
                    var zone = byId[item.Id!.Trim()];
                    var min = (float?)item.MinSpeed;
                    var max = (float?)item.MaxSpeed;
                    var updated = await _tmsZoneRep.AsUpdateable()
                        .SetColumns(z => new TmsZone
                        {
                            MinSpeed = min, MaxSpeed = max, UpdateTime = updateTime, UpdateUId = updateUId
                        })
                        .Where(z => z.ID == zone.ID && z.TenantId == zone.TenantId && z.IsDelete == null)
                        .EnableDiffLogEvent()
                        .ExecuteCommandAsync();
                    if (updated != 1) throw Oops.Oh(BaseMsg.BaseException.DataNotMatch);
                }
                await db.Ado.CommitTranAsync();
            }
            catch
            {
                await db.Ado.RollbackTranAsync();
                throw;
            }
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsZone);
            await NotifyAsync(null, "batch-update");
            return new BatchUpdateZoneSpeedOutput { Updated = items.Count };
        }

        /// <summary>Handler chi khai bao cai gi vua doi; RefreshModifyService tra route trong DB roi bat NATS.</summary>
        private Task NotifyAsync(string? key, string action)
            => this._refreshModify.NotifyAsync(new RefreshModifyRequest("catalog", "zone", key, action));
    }
}
