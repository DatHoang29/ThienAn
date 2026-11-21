using Modules.TMS.Controllers.EquipmentType.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;

namespace Modules.TMS.Controllers.EquipmentType.Queries
{
    public class TmsEquipmentTypeQueryHandler : IWolverineHandler
    {
        private BaseRepository<TmsEquipmentType> _tmsEquipTypeRep;
        private BaseCacheService _cache;

        public TmsEquipmentTypeQueryHandler(BaseRepository<TmsEquipmentType> tmsEquipTypeRep, BaseCacheService cache)
        {
            _tmsEquipTypeRep = tmsEquipTypeRep;
            _cache = cache;
        }

        /// <summary>
        /// Hàm lấy danh sách loại thiết bị theo phân trang
        /// Author: SonTH
        /// Create date: 03/2025
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public async Task<SqlSugarPagedList<PageEquipmentTypesOutput>> HandleAsync(PageEquipmentTypesInput command)
        {
            return await _tmsEquipTypeRep.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.Name), u => u.Name != null && u.Name.Contains(command.Name))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Code), u => u.Code != null && u.Code.Contains(command.Code))
                .WhereIF(!string.IsNullOrWhiteSpace(command.ModuleId), u => u.ModuleId != null && u.ModuleId.Contains(command.ModuleId))
                .WhereIF(command.Status != null, u => u.Status == command.Status)
                .Select((u) => new PageEquipmentTypesOutput()
                {
                }, true).OrderBuilder(command, "", BaseConst.SortFieldConst.UpdateOrCreateTime)
                .ToPagedListAsync(command.Page, command.PageSize);
        }

        /// <summary>
        /// Hàm lấy danh sách loại thiết bị
        /// Author: SonTH
        /// Create date: 03/2025
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public async Task<List<EquipmentTypeOutput>> HandleAsync(EquipmentTypeInput command)
        {

            var list = _cache.GetOrAdd(CacheConst.Tms.TmsEquipmentType, _ =>
            {
                return _tmsEquipTypeRep.AsQueryable()
                    .ClearFilter() // Bỏ qua các filter đã được cấu hình trước đó
                    .Select((u) => new EquipmentTypeOutput()
                    {
                    }, true)
                    .OrderBy(u => u.CreateTime).ToListAsync().Result;

            });

            var tvrList = list
                // Nếu FE KHÔNG yêu cầu lấy dữ liệu đã xóa (IncludeDelete = false) -> chỉ trả về những dòng chưa bị xóa (IsDelete == null)
                .WhereIF(!command.IncludeDelete, u => u.IsDelete == null)
                .WhereIF(!string.IsNullOrWhiteSpace(command.Name), u => u.Name != null && u.Name.Contains(command.Name))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Code), u => u.Code != null && u.Code.Contains(command.Code))
                .WhereIF(!string.IsNullOrWhiteSpace(command.ModuleId), u => u.ModuleId != null && u.ModuleId.Contains(command.ModuleId))
                .WhereIF(command.Status != null, u => u.Status == command.Status)
              .OrderBy(st => st.CreateTime)
              .ToList();

            return tvrList;
        }
    }
}
