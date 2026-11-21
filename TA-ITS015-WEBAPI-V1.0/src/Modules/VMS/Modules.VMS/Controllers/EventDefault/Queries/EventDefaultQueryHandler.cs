using Modules.VMS.Controllers.EventDefault.Dto;
using Modules.VMS.Core.Entities;
using Modules.VMS.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;

namespace Modules.VMS.Controllers.EventDefault.Queries
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Query lấy danh sách nội dung mặc định VMS theo sự kiện sự cố.
    /// Created date: 15/07/2026
    /// </summary>
    public class EventDefaultQueryHandler : IScoped
    {
        private readonly BaseRepository<VmsEventDefault> _baseRepository;
        private BaseCacheService _cache;

        public EventDefaultQueryHandler(BaseRepository<VmsEventDefault> baseRepository, BaseCacheService cache)
        {
            _baseRepository = baseRepository;
            _cache = cache;
        }

        /// <summary>
        /// Author: 
        /// Description: Phân trang danh sách VmsEventDefault (trả đủ cột: RowData, Url, ContentType, DefaultConfig).
        /// </summary>
        /// <param name="command">Đối tượng PageVmsEventDefaultInput</param>
        /// <returns>Danh sách phân trang PageVmsEventDefaultOutput</returns>
        public async Task<SqlSugarPagedList<PageVmsEventDefaultOutput>> HandleAsync(PageVmsEventDefaultInput command)
        {
            var list = await _baseRepository.AsQueryable()
                .WhereIF(!string.IsNullOrEmpty(command.Code), x => x.Code != null && x.Code.Contains(command.Code))
                .WhereIF(!string.IsNullOrEmpty(command.Name), x => x.Name != null && x.Name.Contains(command.Name))
                .WhereIF(!string.IsNullOrEmpty(command.EventTypeId), x => x.EventTypeId == command.EventTypeId)
                .Select((x) => new PageVmsEventDefaultOutput() { }, true)
                .OrderBuilder(command, "", "CreateTime", false)
                .ToPagedListAsync(command.Page, command.PageSize);

            return list;
        }

        /// <summary>
        /// Author: ChuongLP
        /// Date: 2026-07-15
        /// Description: Tìm kiếm danh sách (không phân trang) VmsEventDefault theo điều kiện.
        /// </summary>
        /// <param name="command">Đối tượng VmsEventDefaultListInput</param>
        /// <returns>Danh sách VmsEventDefaultOutput</returns>
        public async Task<List<VmsEventDefaultOutput>> HandleAsync(VmsEventDefaultListInput command)
        {
            var eventList = await this._cache.GetOrAdd(CacheConst.Vms.VmseventDefault, _ =>
            {
                return this._baseRepository.AsQueryable()
                    .Select(msg => new VmsEventDefaultOutput() { }, true)
                    .OrderBy(msg => msg.CreateTime)
                    .ToListAsync();
            });

            var list = eventList?
                .WhereIF(!string.IsNullOrEmpty(command.ID), x => x.ID == command.ID)
                .WhereIF(!string.IsNullOrEmpty(command.Code), x => x.Code != null && x.Code.Contains(command.Code))
                .WhereIF(!string.IsNullOrEmpty(command.Name), x => x.Name != null && x.Name.Contains(command.Name))
                .WhereIF(!string.IsNullOrEmpty(command.EventTypeId), x => x.EventTypeId == command.EventTypeId)
                .ToList();

            return list;
        }
    }
}
