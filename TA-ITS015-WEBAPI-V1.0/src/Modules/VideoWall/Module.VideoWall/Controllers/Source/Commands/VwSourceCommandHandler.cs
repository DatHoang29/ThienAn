using Mapster;
using Module.VideoWall.Core.Constants;
using Module.VideoWall.Core.Dto.Source;
using Module.VideoWall.Core.Entities;
using Module.VideoWall.Core.Exceptions;
using Module.VideoWall.Core.Helper;
using Module.VideoWall.Infrastructure;
using Shared.Core.Extensions;
using Shared.DTO.Constants.Application;
using Shared.DTO.Constants.Localization;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;
using Wolverine;

namespace Module.VideoWall.Controllers.Source.Commands
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Xử lý thêm / sửa / xóa VwSource
    /// Created date: 2026-08-01
    /// </summary>
    public class VwSourceCommandHandler : IWolverineHandler
    {
        private readonly BaseRepository<VwSource> _vwSourceRep;
        private readonly BaseRepository<VwController> _vwControllerRep;
        private readonly BaseCacheService _cache;

        public VwSourceCommandHandler(BaseRepository<VwSource> vwSourceRep, BaseRepository<VwController> vwControllerRep, BaseCacheService cache)
        {
            _vwSourceRep = vwSourceRep;
            _vwControllerRep = vwControllerRep;
            _cache = cache;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Thêm mới 1 VwSource
        /// Created date: 2026-08-01
        /// </summary>
        public async Task HandleAsync(VwAddSourceInput command)
        {
            // 1. Kiểm tra trùng mã
            command.Code = command.Code?.Trim();
            if (await _vwSourceRep.IsAnyAsync(u => u.IsDelete == null && u.Code == command.Code))
                throw Oops.Oh(BaseLocaleManager.BaseException.Exist, Furion.Localization.L.Text[BaseLocaleManager.BaseEntity.Code]);

            // 2. Nguồn cục bộ phải nằm trên bộ điều khiển trung tâm (phải làm TRƯỚC
            //    CheckReferenceAsync để id vừa gán vẫn đi qua kiểm tồn tại)
            // TẠM TẮT PHÂN QUYỀN (25/09/2026) — bật lại: bỏ comment khối dưới.
            // await EnsureLocalSourceOnCenterAsync(command, command.SourceType);

            // 3. Kiểm tra khóa ngoại + cổng vào chưa gắn nguồn khác
            await CheckReferenceAsync(command);
            await EnsurePortNotUsedAsync(command.ControllerId, command.SignalNo, null);
            command.CreateUId = null;

            // 4. Ghi DB + xóa cache
            await _vwSourceRep.InsertWithDiffLogAsync(command.Adapt<VwSource>());
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwSource);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Cập nhật 1 VwSource
        /// Created date: 2026-08-01
        /// </summary>
        public async Task HandleAsync(VwUpdateSourceInput command)
        {
            // 1. Kiểm tra trùng mã với bản ghi khác
            command.Code = command.Code?.Trim();
            if (await _vwSourceRep.IsAnyAsync(u => u.IsDelete == null && u.Code == command.Code && u.ID != command.ID))
                throw Oops.Oh(BaseLocaleManager.BaseException.Exist, Furion.Localization.L.Text[BaseLocaleManager.BaseEntity.Code]);

            // 2. Lấy bản ghi gốc
            var entity = await _vwSourceRep.GetByIdAsync(command.ID)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            // 4. Nguồn cục bộ phải nằm trên bộ điều khiển trung tâm.
            //    Lấy SourceType hợp nhất: lệnh Update có thể không gửi lại trường này.
            // TẠM TẮT PHÂN QUYỀN (25/09/2026) — bật lại: bỏ comment khối dưới.
            // await EnsureLocalSourceOnCenterAsync(command, command.SourceType ?? entity.SourceType);

            // 5. Kiểm tra khóa ngoại + cổng vào chưa gắn nguồn khác
            await CheckReferenceAsync(command);
            await EnsurePortNotUsedAsync(command.ControllerId, command.SignalNo, command.ID);

            // 6. Map dữ liệu từ command vào bản ghi gốc.
            //    THIẾU BƯỚC NÀY thì Update trả về thành công nhưng DB không đổi gì.
            command.Adapt(entity);

            // 7. Ghi DB + xóa cache
            await _vwSourceRep.UpdateWithDiffLogAsync(entity, false);
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwSource);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Xóa 1 VwSource (xóa mềm)
        /// Created date: 2026-08-01
        /// </summary>
        public async Task HandleAsync(VwDeleteSourceInput command)
        {
            var entity = await _vwSourceRep.GetFirstAsync(u => u.ID == command.ID)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            await _vwSourceRep.SoftDeleteAsync<VwSource>(entity);
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwSource);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Xóa nhiều VwSource (xóa mềm)
        /// Created date: 2026-08-01
        /// </summary>
        public async Task HandleAsync(List<VwDeleteSourceInput> command)
        {
            var ids = command.Select(item => item.ID).ToList();

            var entities = await _vwSourceRep.AsQueryable()
                .Where(u => ids.Contains(u.ID))
                .ToListAsync();

            if (entities?.Count != ids.Count)
                throw Oops.Oh(BaseLocaleManager.BaseException.DataNotMatch);

            await _vwSourceRep.SoftDeleteAsync<VwSource>(u => ids.Contains(u.ID));
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwSource);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Kiểm tra các khóa ngoại của VwSource có tồn tại hay không
        /// Created date: 2026-08-01
        /// </summary>
        private async Task CheckReferenceAsync(VwAddSourceInput command)
        {
            if (!string.IsNullOrWhiteSpace(command.ControllerId)
                && !await _vwControllerRep.IsAnyAsync(u => u.IsDelete == null && u.ID == command.ControllerId))
                throw Oops.Oh(BaseLocaleManager.BaseException.NotExist, Furion.Localization.L.Text[BaseMsg.Vw.Entity.ControllerId]);
        }

        /// <summary>
        /// Description: Mỗi cổng vào (SignalNo = videoInputChannelID) trên 1 bộ điều khiển chỉ gắn 1 nguồn chưa xoá.
        ///              Chưa chọn cổng / chưa chọn bộ điều khiển ⇒ bỏ qua.
        /// Created date: 10/11/2026
        /// </summary>
        /// <param name="controllerId">Bộ điều khiển của nguồn.</param>
        /// <param name="signalNo">Mã kênh vào trên thiết bị.</param>
        /// <param name="excludeId">ID nguồn đang sửa (null khi thêm mới).</param>
        private async Task EnsurePortNotUsedAsync(string? controllerId, int? signalNo, string? excludeId)
        {
            if (string.IsNullOrWhiteSpace(controllerId) || signalNo == null)
                return;

            var other = await _vwSourceRep.GetFirstAsync(u => u.IsDelete == null
                && u.ControllerId == controllerId
                && u.SignalNo == signalNo
                && (excludeId == null || u.ID != excludeId));
            if (other != null)
                throw Oops.Oh(BaseMsg.Vw.Exception.SourcePortInUse, other.Name ?? other.Code);
        }

        /// <summary>
        /// Description: Đảm bảo nguồn cục bộ gắn vào bộ điều khiển trung tâm (tự gán nếu trống, chặn nếu trỏ bộ con).
        /// Created date: 09/09/2026
        /// </summary>
        /// <param name="command">Lệnh thêm/sửa nguồn tín hiệu.</param>
        /// <param name="effectiveSourceType">Loại nguồn tín hiệu đã hợp nhất.</param>
        private async Task EnsureLocalSourceOnCenterAsync(VwAddSourceInput command, string? effectiveSourceType)
        {
            if (!VwSourceProfile.IsLocal(effectiveSourceType))
                return;

            if (string.IsNullOrWhiteSpace(command.ControllerId))
            {
                var center = await _vwControllerRep.AsQueryable()
                    .Where(u => u.IsDelete == null)
                    .WhereIsCenter()
                    .OrderByDescending(u => u.CreateTime)
                    .FirstAsync()
                    ?? throw Oops.Oh(BaseMsg.Vw.Exception.CentralControllerNotConfigured);

                command.ControllerId = center.ID;
                return;
            }

            // Controller không tồn tại thì để CheckReferenceAsync báo NotExist, không đổi thành
            // lỗi "gắn sai bộ điều khiển" gây khó hiểu.
            var controller = await _vwControllerRep.GetFirstAsync(u => u.IsDelete == null && u.ID == command.ControllerId);
            if (VwControllerTopology.IsSub(controller))
                throw Oops.Oh(BaseMsg.Vw.Exception.LocalSourceOnlyCentralController);
        }
    }
}
