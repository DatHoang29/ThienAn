using Modules.DataTransporter.ITS.Infrastructure.Services;
using Modules.TMS.Controllers.Equipment.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Exceptions;
using Modules.TMS.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;
using Shared.Infrastructure.Services;

namespace Modules.TMS.Controllers.Equipment.Commands
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Lớp nghiệp vụ xử lý bật/ẩn hiển thị (visible) hàng loạt cho thiết bị tmsEquipment.
    /// Tách riêng khỏi EquipmentCommandHandler vì đây là nghiệp vụ cập nhật hàng loạt (bulk), khác
    /// với luồng CRUD cơ bản Add/Update/Delete.
    /// Created date: 09/08/2024
    /// </summary>
    public class EquipmentVisibilityCommandHandler : IWolverineHandler
    {
        private BaseRepository<TmsEquipment> _tmsEquipRep;
        private BaseCacheService _cache;

        public EquipmentVisibilityCommandHandler(BaseRepository<TmsEquipment> tmsEquipRep, BaseCacheService cache)
        {
            _tmsEquipRep = tmsEquipRep;
            _cache = cache;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Hàm thay đổi visible của nhiều record trong bảng tmsEquipment
        /// Created date: 09/08/2024
        /// </summary>
        public async Task HandleAsync(List<UpdateEquipmentVisibleInput> command)
        {
            var ids = command.Select(u => u.ID).ToList();
            if (ids.Count == 0) return;
            var count = await _tmsEquipRep.AsQueryable()
                .Where(u => ids.Contains(u.ID))
                .CountAsync();

            if (count != ids.Count)
                throw Oops.Oh(BaseMsg.BaseException.DataNotMatch);

            await _tmsEquipRep.AsUpdateable()
                .Where(u => ids.Contains(u.ID))
                .SetColumns(u => u.EquipmentVisible == (int)BaseEnums.TmsEquipmentVisibleStateEnum.Visible)
                .ExecuteCommandAsync();

            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsEquipment);
            ItsDataTransporter.BroadcastData(ItsDataTransporterChannel.EquipmentVisible, command);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Hàm thay đổi invisible của nhiều record trong bảng tmsEquipment
        /// Created date: 09/08/2024
        /// </summary>
        public async Task HandleAsync(List<UpdateEquipmentInVisibleInput> command)
        {
            var ids = command.Select(u => u.ID).ToList();
            if (ids.Count == 0) return;
            var count = await _tmsEquipRep.AsQueryable()
                .Where(u => ids.Contains(u.ID))
                .CountAsync();

            if (count != ids.Count)
                throw Oops.Oh(BaseMsg.BaseException.DataNotMatch);

            await _tmsEquipRep.AsUpdateable()
                .Where(u => ids.Contains(u.ID))
                .SetColumns(u => u.EquipmentVisible == (int)BaseEnums.TmsEquipmentVisibleStateEnum.Invisible)
                .ExecuteCommandAsync();

            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsEquipment);
            ItsDataTransporter.BroadcastData(ItsDataTransporterChannel.EquipmentVisible, command);
        }
    }
}
