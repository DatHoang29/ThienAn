using Mapster;
using Module.VideoWall.Core.Dto.Controller;
using Module.VideoWall.Core.Entities;
using Module.VideoWall.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.DTO.Constants.Localization;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;
using Wolverine;
// Trùng tên với entity cổng ra (Entities.VwControllerOutput) ⇒ ở đây là DTO trả về bộ điều khiển
using VwControllerOutput = Module.VideoWall.Core.Dto.Controller.VwControllerOutput;

namespace Module.VideoWall.Controllers.Controller.Queries
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Xử lý truy vấn dữ liệu VwController
    /// Created date: 2026-08-01
    /// </summary>
    public class VwControllerQueryHandler : IWolverineHandler
    {
        private readonly BaseRepository<VwController> _vwControllerRep;
        private readonly BaseRepository<VwSource> _vwSourceRep;
        private readonly BaseCacheService _cache;

        public VwControllerQueryHandler(
            BaseRepository<VwController> vwControllerRep,
            BaseRepository<VwSource> vwSourceRep,
            BaseCacheService cache)
        {
            _vwControllerRep = vwControllerRep;
            _vwSourceRep = vwSourceRep;
            _cache = cache;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy danh sách VwController theo phân trang
        /// Created date: 2026-08-01
        /// </summary>
        public async Task<SqlSugarPagedList<VwPageControllerOutput>> HandleAsync(VwPageControllerInput command)
        {
            var pagedList = await _vwControllerRep.AsQueryable()
                // Ô tìm "Mã / Tên": khớp mã HOẶC tên
                .WhereIF(!string.IsNullOrWhiteSpace(command.Name), u => (u.Name != null && u.Name.Contains(command.Name!)) || (u.Code != null && u.Code.Contains(command.Name!)))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Model), u => u.Model != null && u.Model.Contains(command.Model!))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Chassis), u => u.Chassis != null && u.Chassis.Contains(command.Chassis!))
                .WhereIF(!string.IsNullOrWhiteSpace(command.RefId), u => u.RefId == command.RefId)
                .WhereIF(command.Status.HasValue, u => u.Status == command.Status)
                .Select(u => new VwPageControllerOutput() { }, true)
                .OrderBuilder(command, "")
                .ToPagedListAsync(command.Page, command.PageSize);

            // Mật khẩu là bản sao từ thiết bị, chỉ BE / Worker dùng ⇒ không trả về FE
            foreach (var item in pagedList.Records)
                item.PassWord = null;

            return pagedList;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy toàn bộ danh sách VwController theo tiêu chí lọc (không phân trang)
        /// Created date: 2026-08-01
        /// </summary>
        public async Task<List<VwControllerOutput>> HandleAsync(VwControllerInput command)
        {
            // 1. Lấy dữ liệu thô từ Cache hoặc DB
            var entities = _cache.GetOrAdd(CacheConst.Vw.VwController, _ =>
            {
                return _vwControllerRep.AsQueryable()
                .Select((u) => new VwControllerOutput()
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
                .WhereIF(!string.IsNullOrWhiteSpace(command.Model), u => !string.IsNullOrWhiteSpace(u.Model) && u.Model!.Contains(command.Model!, StringComparison.OrdinalIgnoreCase))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Chassis), u => !string.IsNullOrWhiteSpace(u.Chassis) && u.Chassis!.Contains(command.Chassis!, StringComparison.OrdinalIgnoreCase))
                .WhereIF(!string.IsNullOrWhiteSpace(command.RefId), u => u.RefId == command.RefId)
                .WhereIF(command.Status != null, u => u.Status == command.Status)
                .OrderBy(st => st.CreateTime)
                // Bản sao để không sửa object trong cache; không trả mật khẩu về FE
                .Select(u =>
                {
                    var output = u.Adapt<VwControllerOutput>();
                    output.PassWord = null;
                    return output;
                })
                .ToList();

            return filtered;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy chi tiết 1 VwController theo ID
        /// Created date: 2026-08-01
        /// </summary>
        public async Task<VwControllerOutput> HandleAsync(VwIdControllerInput command)
        {
            var entity = await _vwControllerRep.GetByIdAsync(command.ID)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            var output = entity.Adapt<VwControllerOutput>();
            output.PassWord = null;
            return output;
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Lấy danh sách kênh tín hiệu đầu vào từ DB (VwSource).
        ///              Dữ liệu được Worker tự đồng bộ sau mỗi lần Probe/SyncSources ISAPI thành công.
        ///              Không còn gọi NATS request-reply mỗi lần GET — đọc thẳng từ entity quan hệ có sẵn.
        /// Created date: 17/08/2026
        /// </summary>
        public async Task<List<VwGetInputChannelsControllerOutput>> HandleAsync(VwGetInputChannelsControllerInput command)
        {
            var entity = await _vwControllerRep.GetByIdAsync(command.ID)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            // Đọc từ VwSource - dữ liệu kênh tín hiệu vào đã được Worker đồng bộ
            var sources = await _vwSourceRep.AsQueryable()
                .Where(s => s.IsDelete == null && s.ControllerId == command.ID)
                .OrderBy(s => s.SignalNo)
                .ToListAsync();

            return sources.Select(s => new VwGetInputChannelsControllerOutput
            {
                Id = s.SignalNo ?? 0,
                Name = s.Name,
                VideoInputChannelAccessStatus = s.SignalStatus?.ToString()
            }).ToList();
        }
    }
}
