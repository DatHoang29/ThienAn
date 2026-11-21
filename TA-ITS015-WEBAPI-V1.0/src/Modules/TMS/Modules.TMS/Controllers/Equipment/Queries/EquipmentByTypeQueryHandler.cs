using Modules.TMS.Core.Dto.Equipment;
using Modules.TMS.Core.Entities;
using Modules.TMS.Infrastructure;
using static Shared.DTO.Enums.BaseEnums;

namespace Modules.TMS.Controllers.Equipment.Queries
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Lấy danh sách thiết bị theo mã loại thiết bị (vms/lcs...) qua MessBus.
    ///              Tách riêng khỏi EquipmentQueryHandler vì cần thêm repo loại thiết bị.
    ///              Dùng cho phân hệ VMS lấy trạng thái thiết bị định kỳ.
    /// Created date: 2026-08-20
    /// </summary>
    public class EquipmentByTypeQueryHandler
    {
        private readonly BaseRepository<TmsEquipment> _tmsEquipRep;
        private readonly BaseRepository<TmsEquipmentType> _tmsEquipTypeRep;

        public EquipmentByTypeQueryHandler(BaseRepository<TmsEquipment> tmsEquipRep,
            BaseRepository<TmsEquipmentType> tmsEquipTypeRep)
        {
            _tmsEquipRep = tmsEquipRep;
            _tmsEquipTypeRep = tmsEquipTypeRep;
        }

        public async Task<List<TmsEquipment>> HandleAsync(GetEquipmentByTypeCodesInput command)
        {
            if (command.TypeCodes == null || command.TypeCodes.Count == 0)
                return new List<TmsEquipment>();

            // Map mã loại thiết bị -> danh sách ID loại thiết bị
            var typeIds = await _tmsEquipTypeRep.AsQueryable()
                .Where(t => command.TypeCodes.Contains(t.Code))
                .Where(t => t.IsDelete == null)
                .Select(t => t.ID)
                .ToListAsync();

            if (typeIds == null || typeIds.Count == 0)
                return new List<TmsEquipment>();

            return await _tmsEquipRep.AsQueryable()
                .Where(e => typeIds.Contains(e.EquipmentTypeId))
                .Where(e => e.Status == StatusEnum.Enable && e.IsDelete == null)
                .ToListAsync();
        }
    }
}
