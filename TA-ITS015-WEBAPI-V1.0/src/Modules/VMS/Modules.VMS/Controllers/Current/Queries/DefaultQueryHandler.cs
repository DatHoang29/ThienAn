using Modules.VMS.Controllers.Current.Dto;
using Modules.VMS.Core.Entities;
using Modules.VMS.Infrastructure;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.VMS.Controllers.Current.Queries
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Query lấy danh sách nội dung mặc định VMS (phân trang) cho trang quản lý.
    /// Created date: 14/07/2027
    /// </summary>
    public class DefaultQueryHandler : IScoped
    {
        private readonly BaseRepository<VmsCurrent> _baseRepository;

        public DefaultQueryHandler(BaseRepository<VmsCurrent> baseRepository)
        {
            _baseRepository = baseRepository;
        }

        /// <summary>
        /// Author: ChuongLP
        /// Description: Phân trang danh sách VmsCurrent phục vụ trang Quản lý nội dung mặc định.
        ///              Trả về đầy đủ cột (bao gồm DefaultRowData, DefaultUrl) để FE hiển thị + preview.
        /// </summary>
        /// <param name="command">Đối tượng PageVmsDefaultCurrentInput</param>
        /// <returns>Danh sách PageVmsDefaultCurrentOutput</returns>
        public async Task<SqlSugarPagedList<PageVmsDefaultCurrentOutput>> HandleAsync(PageVmsDefaultCurrentInput command)
        {
            var list = await _baseRepository.AsQueryable()
                .WhereIF(!string.IsNullOrEmpty(command.Code), x => x.Code != null && x.Code.Contains(command.Code))
                .WhereIF(!string.IsNullOrEmpty(command.Name), x => x.Name != null && x.Name.Contains(command.Name))
                .WhereIF(!string.IsNullOrEmpty(command.EquipmentId), x => x.EquipmentId == command.EquipmentId)
                .WhereIF(command.EquipmentIds != null && command.EquipmentIds.Count > 0, x => command.EquipmentIds.Contains(x.EquipmentId))
                .Select((x) => new PageVmsDefaultCurrentOutput() { }, true)
                .OrderBuilder(command, "", "CreateTime", false)
                .ToPagedListAsync(command.Page, command.PageSize);

            return list;
        }

        /// <summary>
        /// Author: ChuongLP
        /// Description: Tìm kiếm danh sách (không phân trang) nội dung mặc định VMS theo điều kiện.
        /// </summary>
        /// <param name="command">Đối tượng VmsDefaultCurrentListInput</param>
        /// <returns>Danh sách VmsDefaultCurrentOutput</returns>
        public async Task<List<VmsDefaultCurrentOutput>> HandleAsync(VmsDefaultCurrentListInput command)
        {
            var list = await _baseRepository.AsQueryable()
                .WhereIF(!string.IsNullOrEmpty(command.ID), x => x.ID == command.ID)
                .WhereIF(!string.IsNullOrEmpty(command.Code), x => x.Code != null && x.Code.Contains(command.Code))
                .WhereIF(!string.IsNullOrEmpty(command.Name), x => x.Name != null && x.Name.Contains(command.Name))
                .WhereIF(!string.IsNullOrEmpty(command.EquipmentId), x => x.EquipmentId == command.EquipmentId)
                .WhereIF(command.EquipmentIds != null && command.EquipmentIds.Count > 0, x => command.EquipmentIds.Contains(x.EquipmentId))
                .Select((x) => new VmsDefaultCurrentOutput() { }, true)
                .ToListAsync();

            return list;
        }
    }
}
