using Modules.CfgSystem.Infrastructure.Services.Refresh;
using Modules.DataTransporter.ITS.Infrastructure.Services;
using Modules.CfgSystem.Core.Refresh;
using Modules.TMS.Controllers.ZoneEquipment.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Exceptions;
using Modules.TMS.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;

namespace Modules.TMS.Controllers.ZoneEquipment.Commands
{
    public class ZoneEquipmentCommandHandler : IWolverineHandler
    {
        private BaseRepository<TmsZoneEquipment> _zoneEquipmentRep;
        private BaseCacheService _cache;
        private RefreshModifyService _refreshModify;

        public ZoneEquipmentCommandHandler(
            BaseRepository<TmsZoneEquipment> zoneEquipmentRep,
            BaseCacheService cache,
            RefreshModifyService refreshModify)
        {
            _zoneEquipmentRep = zoneEquipmentRep;
            _cache = cache;
            _refreshModify = refreshModify;
        }

        /// <summary>
        /// Description: Thêm mới một ZoneEquipment mapping
        /// </summary>
        public async Task HandleAsync(AddZoneEquipmentInput command)
        {
            if (await _zoneEquipmentRep.IsAnyAsync(ze =>
                ze.ZoneId == command.ZoneId && ze.EquipmentId == command.EquipmentId && ze.IsDelete == null))
                throw Oops.Oh(BaseMsg.BaseException.Exist, "Equipment đã được mapping với Zone này");

            command.CreateUId = null;
            await _zoneEquipmentRep.InsertWithDiffLogAsync(command.Adapt<TmsZoneEquipment>());
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsZoneEquipment);
            _cache.RemoveByPrefixKey(CacheConst.Vds.VdsEventSnapshot); // snapshot sự kiện VDS dựng từ danh mục này
            // Ma refresh do route 'catalog:zoneequipment' trong SysConfigData quyet dinh.
            await NotifyAsync(command.ZoneId, "add");
        }

        /// <summary>
        /// Description: Cập nhật một ZoneEquipment mapping
        /// </summary>
        public async Task HandleAsync(UpdateZoneEquipmentInput command)
        {
            var entity = await _zoneEquipmentRep.GetByIdAsync(command.ID)
                ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            if (await _zoneEquipmentRep.IsAnyAsync(ze =>
                ze.ZoneId == command.ZoneId && ze.EquipmentId == command.EquipmentId
                && ze.ID != command.ID && ze.IsDelete == null))
                throw Oops.Oh(BaseMsg.BaseException.Exist, "Equipment đã được mapping với Zone này");

            await _zoneEquipmentRep.UpdateWithDiffLogAsync(command.Adapt(entity));
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsZoneEquipment);
            _cache.RemoveByPrefixKey(CacheConst.Vds.VdsEventSnapshot); // snapshot sự kiện VDS dựng từ danh mục này
            // Ma refresh do route 'catalog:zoneequipment' trong SysConfigData quyet dinh.
            await NotifyAsync(command.ID, "update");
        }

        /// <summary>
        /// Description: Xóa một ZoneEquipment mapping
        /// </summary>
        public async Task HandleAsync(DeleteZoneEquipmentInput command)
        {
            var entity = await _zoneEquipmentRep.GetFirstAsync(ze => ze.ID == command.ID)
                ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            await _zoneEquipmentRep.DeleteAsync(entity);
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsZoneEquipment);
            _cache.RemoveByPrefixKey(CacheConst.Vds.VdsEventSnapshot); // snapshot sự kiện VDS dựng từ danh mục này
            // Ma refresh do route 'catalog:zoneequipment' trong SysConfigData quyet dinh.
            await NotifyAsync(command.ID, "delete");
        }

        /// <summary>
        /// Description: Xóa nhiều ZoneEquipment mapping
        /// </summary>
        public async Task HandleAsync(List<DeleteZoneEquipmentInput> command)
        {
            var ids = command.Select(u => u.ID).ToList();

            var list = _zoneEquipmentRep.AsQueryable().Where(u => ids.Contains(u.ID));

            if (list?.Count() != ids.Count)
                throw Oops.Oh(BaseMsg.BaseException.DataNotMatch);

            await _zoneEquipmentRep.DeleteAsync(l => ids.Contains(l.ID));
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsZoneEquipment);
            _cache.RemoveByPrefixKey(CacheConst.Vds.VdsEventSnapshot); // snapshot sự kiện VDS dựng từ danh mục này
            // Ma refresh do route 'catalog:zoneequipment' trong SysConfigData quyet dinh.
            await NotifyAsync(null, "batch-delete");
        }

        /// <summary>
        /// Description: Xác nhận và lưu danh sách mapping từ auto-map preview
        /// </summary>
        public async Task HandleAsync(ConfirmZoneEquipmentMappingInput command)
        {
            if (command.ClearExistingMappings && !string.IsNullOrWhiteSpace(command.ZoneId))
            {
                await _zoneEquipmentRep.DeleteAsync(
                    ze => ze.ZoneId == command.ZoneId);
            }

            foreach (var mapping in command.Mappings)
            {
                if (await _zoneEquipmentRep.IsAnyAsync(ze =>
                    ze.ZoneId == mapping.ZoneId && ze.EquipmentId == mapping.EquipmentId && ze.IsDelete == null))
                    continue;

                mapping.CreateUId = null;
                await _zoneEquipmentRep.InsertWithDiffLogAsync(mapping.Adapt<TmsZoneEquipment>());
            }

            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsZoneEquipment);
            _cache.RemoveByPrefixKey(CacheConst.Vds.VdsEventSnapshot); // snapshot sự kiện VDS dựng từ danh mục này
            // Ma refresh do route 'catalog:zoneequipment' trong SysConfigData quyet dinh.
            await NotifyAsync(command.ZoneId, "confirm-mapping");
        }

        /// <summary>Handler chi khai bao cai gi vua doi; RefreshModifyService tra route trong DB roi bat NATS.</summary>
        private Task NotifyAsync(string? key, string action)
            => this._refreshModify.NotifyAsync(new RefreshModifyRequest("catalog", "zoneequipment", key, action));
    }
}
