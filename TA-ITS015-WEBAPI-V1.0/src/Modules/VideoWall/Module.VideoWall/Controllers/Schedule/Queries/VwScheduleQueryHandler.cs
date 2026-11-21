using Mapster;
using Module.VideoWall.Core.Dto.Schedule;
using Module.VideoWall.Core.Entities;
using Module.VideoWall.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.DTO.Constants.Localization;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;
using Wolverine;

namespace Module.VideoWall.Controllers.Schedule.Queries
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Xử lý truy vấn dữ liệu VwSchedule
    /// Created date: 2026-08-01
    /// </summary>
    public class VwScheduleQueryHandler : IWolverineHandler
    {
        private readonly BaseRepository<VwSchedule> _vwScheduleRep;
        private readonly BaseCacheService _cache;

        public VwScheduleQueryHandler(BaseRepository<VwSchedule> vwScheduleRep, BaseCacheService cache)
        {
            _vwScheduleRep = vwScheduleRep;
            _cache = cache;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy danh sách VwSchedule theo phân trang
        /// Created date: 2026-08-01
        /// </summary>
        public async Task<SqlSugarPagedList<VwPageScheduleOutput>> HandleAsync(VwPageScheduleInput command)
        {
            var pagedList = await _vwScheduleRep.AsQueryable()
                // Ô tìm "Mã / Tên": khớp mã HOẶC tên
                .WhereIF(!string.IsNullOrWhiteSpace(command.Name), u => (u.Name != null && u.Name.Contains(command.Name!)) || (u.Code != null && u.Code.Contains(command.Name!)))
                .WhereIF(!string.IsNullOrWhiteSpace(command.TargetSceneId), u => u.TargetSceneId != null && u.TargetSceneId.Contains(command.TargetSceneId!))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Action), u => u.Action != null && u.Action.Contains(command.Action!))
                .WhereIF(command.Status != null, u => u.Status == command.Status)
                .Select(u => new VwPageScheduleOutput() { }, true)
                .OrderBuilder(command, "")
                .ToPagedListAsync(command.Page, command.PageSize);

            return pagedList;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy toàn bộ danh sách VwSchedule theo tiêu chí lọc (không phân trang)
        /// Created date: 2026-08-01
        /// </summary>
        public async Task<List<VwScheduleOutput>> HandleAsync(VwScheduleInput command)
        {
            // 1. Lấy dữ liệu thô từ Cache hoặc DB.
            var entities = _cache.GetOrAdd(CacheConst.Vw.VwSchedule, _ =>
            {
                return _vwScheduleRep.AsQueryable()
                .Select((u) => new VwScheduleOutput()
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
                .WhereIF(!string.IsNullOrWhiteSpace(command.TargetSceneId), u => !string.IsNullOrWhiteSpace(u.TargetSceneId) && u.TargetSceneId!.Contains(command.TargetSceneId!, StringComparison.OrdinalIgnoreCase))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Action), u => !string.IsNullOrWhiteSpace(u.Action) && u.Action!.Contains(command.Action!, StringComparison.OrdinalIgnoreCase))
                .WhereIF(command.Status != null, u => u.Status == command.Status)
                .OrderBy(st => st.CreateTime)
                .ToList();

            return filtered;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy chi tiết 1 VwSchedule theo ID
        /// Created date: 2026-08-01
        /// </summary>
        public async Task<VwScheduleOutput> HandleAsync(VwIdScheduleInput command)
        {
            var entity = await _vwScheduleRep.GetByIdAsync(command.ID)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            return entity.Adapt<VwScheduleOutput>();
        }
    }
}
