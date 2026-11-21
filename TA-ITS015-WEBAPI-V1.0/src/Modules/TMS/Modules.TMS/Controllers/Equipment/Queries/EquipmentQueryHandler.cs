using Modules.TMS.Controllers.Equipment.Dto;
using Modules.TMS.Core.Dto.Equipment;
using Modules.TMS.Core.Entities;
using Modules.TMS.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;
using static Shared.DTO.Enums.BaseEnums;

namespace Modules.TMS.Controllers.Equipment.Queries
{
    /// <summary>
    /// Author: ThangNN
    /// Description: Lớp nghiệp vụ xử lý các truy vấn CRUD cơ bản (Page/List/Single/ListByIds) của tmsEquipment.
    /// Các truy vấn liên quan đến vị trí/zone được tách ra EquipmentPositionQueryHandler.
    /// Created date: 09/08/2024
    /// </summary>
    public class EquipmentQueryHandler
    {
        private BaseRepository<TmsEquipment> _tmsEquipRep;
        private BaseCacheService _cache;

        public EquipmentQueryHandler(BaseRepository<TmsEquipment> tmsEquipRep,
            BaseCacheService cache)
        {
            _tmsEquipRep = tmsEquipRep;
            _cache = cache;
        }

        public async Task<SqlSugarPagedList<PageEquipmentOutput>> HandleAsync(PageEquipmentInput command)
        {
            if (string.Equals(command.Order, "desc", StringComparison.OrdinalIgnoreCase))
            {
                command.DescStr = "desc";
            }

            return await _tmsEquipRep.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.Name), u => u.Name.Contains(command.Name))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Code), u => u.Code.Contains(command.Code))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Ip), u => u.Ip.Contains(command.Ip))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Port), u => u.Port.Contains(command.Port))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Location), u => u.Location.ToString().Contains(command.Location))
                .WhereIF(!string.IsNullOrWhiteSpace(command.DirectionId), u => u.DirectionId.ToString().Contains(command.DirectionId))
                .WhereIF(!string.IsNullOrWhiteSpace(command.EquipmentState), u => u.EquipmentState.ToString().Contains(command.EquipmentState))
                .WhereIF(!string.IsNullOrWhiteSpace(command.EquipmentTypeId), u => u.EquipmentTypeId.Contains(command.EquipmentTypeId))
                .WhereIF(command.Status != null, u => u.Status == command.Status)
                .WhereIF(command.EquipmentVisible != null, u => u.EquipmentVisible == command.EquipmentVisible)
                .Select((u) => new PageEquipmentOutput()
                {
                }, true).OrderBuilder(command, "", BaseConst.SortFieldConst.UpdateOrCreateTime)
                .ToPagedListAsync(command.Page, command.PageSize);
        }


        public async Task<List<EquipmentOutput>> HandleAsync(EquipmentInput command)
        {

            var list = _cache.GetOrAdd($"{CacheConst.Tms.TmsEquipment}:{command.Name}", _ =>
            {
                return _tmsEquipRep.AsQueryable()
                    .Select((u) => new EquipmentOutput()
                    {
                    }, true)
                    .OrderBy(u => u.CreateTime).ToListAsync().Result;

            });

            var tvrList = list.AsQueryable()
              .WhereIF(!string.IsNullOrWhiteSpace(command.Code), u => u.Code.Contains(command.Code))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Ip), u => u.Ip.Contains(command.Ip))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Port), u => u.Port.Contains(command.Port))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Location), u => u.Location.ToString().Contains(command.Location))
                .WhereIF(!string.IsNullOrWhiteSpace(command.DirectionId), u => u.DirectionId.ToString().Contains(command.DirectionId))
                .WhereIF(!string.IsNullOrWhiteSpace(command.EquipmentState), u => u.EquipmentState != null && u.EquipmentState.ToString().Contains(command.EquipmentState))
                .WhereIF(!string.IsNullOrWhiteSpace(command.EquipmentTypeId), u => u.EquipmentTypeId != null && u.EquipmentTypeId.Contains(command.EquipmentTypeId))
                .Where(u => u.Status.HasValue && u.Status.Value == StatusEnum.Enable) // Sửa ở đây
                .WhereIF(command.Status != null, u => u.Status == command.Status)
             .OrderBy(st => st.CreateTime)
             .ToList();

            return tvrList;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Lấy thông tin theo id hoặc code
        /// Created date: 09/08/2024
        /// </summary>
        public async Task<TmsEquipmentDataOutput> HandleAsync(TmsEquipmentSingleInput command)
        {
            var equipment = await this._tmsEquipRep.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.Id), te => te.ID == command.Id)
                .WhereIF(!string.IsNullOrWhiteSpace(command.Code), te => te.Code == command.Code)
                .Where(te => te.Status == StatusEnum.Enable && te.IsDelete == null)
                .Select(te => new TmsEquipmentDataOutput(), true)
                .FirstAsync();

            return equipment;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: List theo danh sách id
        /// Created date: 19/08/2024
        /// </summary>
        public async Task<List<TmsEquipmentDataOutput>> HandleAsync(TmsEquipmentIdListInput command)
        {
            var idList = command.EquipmentIdList?.Trim().Split(",").Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item.Trim()).ToList() ?? null;

            if (idList == null || idList.Count == 0)
                return null;

            var eList = await this._tmsEquipRep.AsQueryable()
                .Where(e => idList.Contains(e.ID))
                .Where(e => e.Status == StatusEnum.Enable && e.IsDelete == null)
                .Select(e => new TmsEquipmentDataOutput() { }, true)
                .ToListAsync();

            return eList;
        }

        /// <summary>
        /// Author: ChuongLP
        /// Description: Lấy danh sách thiết bị theo danh sách ID (dùng cross-module qua MessBus để
        ///              kiểm tra thiết bị có tồn tại hay không, thay cho BaseApiClient).
        ///              Chỉ trả về thiết bị đang Enable và chưa bị xóa (đồng bộ với API list cũ).
        /// Created date: 05/08/2026
        /// </summary>
        public async Task<List<TmsEquipment>> HandleAsync(GetEquipmentByIdsInput command)
        {
            if (command.EquipmentIds == null || command.EquipmentIds.Count == 0)
                return new List<TmsEquipment>();

            return await _tmsEquipRep.AsQueryable()
                .Where(e => command.EquipmentIds.Contains(e.ID))
                .Where(e => e.Status == StatusEnum.Enable && e.IsDelete == null)
                .ToListAsync();
        }

        /// <summary>
        /// Author: ChuongLP
        /// Description: Lấy toàn bộ thiết bị (Enable, chưa xóa) qua MessBus - dùng cho FMS enrich vị trí
        ///              (Km/Met) của cây topology, thay cho BaseApiClient.
        /// Created date: 2026-09-11
        /// </summary>
        public async Task<List<TmsEquipment>> HandleAsync(GetAllEquipmentInput command)
        {
            return await _tmsEquipRep.AsQueryable()
                .Where(e => e.Status == StatusEnum.Enable && e.IsDelete == null)
                .ToListAsync();
        }
    }
}
