using Mapster;
using Module.VideoWall.Core.Dto.Source;
using Module.VideoWall.Core.Entities;
using Module.VideoWall.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.DTO.Constants.Localization;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;
using Wolverine;

namespace Module.VideoWall.Controllers.Source.Queries
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Xử lý truy vấn dữ liệu VwSource
    /// Created date: 2026-08-01
    /// </summary>
    public class VwSourceQueryHandler : IWolverineHandler
    {
        private readonly BaseRepository<VwSource> _vwSourceRep;
        private readonly BaseCacheService _cache;

        public VwSourceQueryHandler(BaseRepository<VwSource> vwSourceRep, BaseCacheService cache)
        {
            _vwSourceRep = vwSourceRep;
            _cache = cache;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy danh sách VwSource theo phân trang
        /// Created date: 2026-08-01
        /// </summary>
        public async Task<SqlSugarPagedList<VwPageSourceOutput>> HandleAsync(VwPageSourceInput command)
        {
            var pagedList = await _vwSourceRep.AsQueryable()
                // Ô tìm "Mã / Tên": khớp mã HOẶC tên
                .WhereIF(!string.IsNullOrWhiteSpace(command.Name), u => (u.Name != null && u.Name.Contains(command.Name!)) || (u.Code != null && u.Code.Contains(command.Name!)))
                .WhereIF(!string.IsNullOrWhiteSpace(command.ControllerId), u => u.ControllerId != null && u.ControllerId.Contains(command.ControllerId!))
                .WhereIF(!string.IsNullOrWhiteSpace(command.SourceType), u => u.SourceType != null && u.SourceType.Contains(command.SourceType!))
                .WhereIF(!string.IsNullOrWhiteSpace(command.SignalType), u => u.SignalType != null && u.SignalType.Contains(command.SignalType!))
                .WhereIF(command.InputPort is > 0, u => u.InputPort == command.InputPort)
                .WhereIF(command.Status != null, u => u.Status == command.Status)
                .WhereIF(command.SignalStatus != null, u => u.SignalStatus == command.SignalStatus)
                .Select(u => new VwPageSourceOutput() { }, true)
                .OrderBuilder(command, "")
                .ToPagedListAsync(command.Page, command.PageSize);

            return pagedList;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy toàn bộ danh sách VwSource theo tiêu chí lọc (không phân trang)
        /// Created date: 2026-08-01
        /// </summary>
        public async Task<List<VwSourceOutput>> HandleAsync(VwSourceInput command)
        {
            // 1. Lấy dữ liệu thô từ Cache hoặc DB
            var entities = _cache.GetOrAdd(CacheConst.Vw.VwSource, _ =>
            {
                return _vwSourceRep.AsQueryable()
                .Select((u) => new VwSourceOutput()
                {
                }, true)
                .OrderBy(u => u.CreateTime)
                .ToListAsync().Result;
            });

            // 2. Lọc theo tiêu chí tìm kiếm
            var filtered = entities.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.ID), u => u.ID == command.ID)
                // Khớp mã HOẶC tên (giống API phân trang)
                .WhereIF(!string.IsNullOrWhiteSpace(command.Name), u => (!string.IsNullOrWhiteSpace(u.Name) && u.Name!.Contains(command.Name!, StringComparison.OrdinalIgnoreCase))
                    || (!string.IsNullOrWhiteSpace(u.Code) && u.Code!.Contains(command.Name!, StringComparison.OrdinalIgnoreCase)))
                .WhereIF(!string.IsNullOrWhiteSpace(command.ControllerId), u => !string.IsNullOrWhiteSpace(u.ControllerId) && u.ControllerId!.Contains(command.ControllerId!, StringComparison.OrdinalIgnoreCase))
                .WhereIF(!string.IsNullOrWhiteSpace(command.SourceType), u => !string.IsNullOrWhiteSpace(u.SourceType) && u.SourceType!.Contains(command.SourceType!, StringComparison.OrdinalIgnoreCase))
                .WhereIF(!string.IsNullOrWhiteSpace(command.SignalType), u => !string.IsNullOrWhiteSpace(u.SignalType) && u.SignalType!.Contains(command.SignalType!, StringComparison.OrdinalIgnoreCase))
                .WhereIF(command.InputPort is > 0, u => u.InputPort == command.InputPort)
                .WhereIF(command.Status != null, u => u.Status == command.Status)
                .WhereIF(command.SignalStatus != null, u => u.SignalStatus == command.SignalStatus)
                .OrderBy(st => st.CreateTime)
                .ToList();

            return filtered;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy chi tiết 1 VwSource theo ID
        /// Created date: 2026-08-01
        /// </summary>
        public async Task<VwSourceOutput> HandleAsync(VwIdSourceInput command)
        {
            var entity = await _vwSourceRep.GetByIdAsync(command.ID)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            return entity.Adapt<VwSourceOutput>();
        }
    }
}
