using Module.VideoWall.Core.Constants;
using Module.VideoWall.Core.Dto.ControllerOutput;
using Module.VideoWall.Core.Exceptions;
using Module.VideoWall.Core.Entities;
using Module.VideoWall.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.DTO.Constants.Localization;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;
using Wolverine;

namespace Module.VideoWall.Controllers.ControllerOutput.Commands
{
    /// <summary>
    /// Description: Sửa trường web quản lý của cổng ra. Trường thiết bị (tên cổng, ô, vị trí) chỉ đồng bộ mới đổi.
    /// Created date: 06/11/2026
    /// </summary>
    public class VwControllerOutputCommandHandler : IWolverineHandler
    {
        private readonly BaseRepository<VwControllerOutput> _vwOutputRep;
        private readonly BaseRepository<VwController> _vwControllerRep;
        private readonly BaseCacheService _cache;

        public VwControllerOutputCommandHandler(
            BaseRepository<VwControllerOutput> vwOutputRep,
            BaseRepository<VwController> vwControllerRep,
            BaseCacheService cache)
        {
            _vwOutputRep = vwOutputRep;
            _vwControllerRep = vwControllerRep;
            _cache = cache;
        }

        /// <summary>
        /// Cổng của bộ phân phối cấp cho bộ hiển thị nào: cổng phải thuộc bộ phân phối,
        /// bộ được cấp phải là bộ hiển thị con của chính bộ phân phối đó. null ⇒ bỏ khai báo.
        /// </summary>
        public async Task HandleAsync(VwSetFeedsControllerOutputInput command)
        {
            var entity = await _vwOutputRep.GetFirstAsync(u => u.IsDelete == null && u.ID == command.ID)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);
            var feeds = string.IsNullOrWhiteSpace(command.FeedsControllerId) ? null : command.FeedsControllerId.Trim();

            if (feeds != null)
            {
                var owner = await _vwControllerRep.GetFirstAsync(u => u.IsDelete == null && u.ID == entity.ControllerId);
                var ok = owner?.Role == VwControllerRole.Distribution
                         && await _vwControllerRep.IsAnyAsync(u => u.IsDelete == null && u.ID == feeds
                                                                   && u.Role == VwControllerRole.Display && u.ParentControllerId == owner.ID);
                if (!ok)
                    throw Oops.Oh(BaseMsg.Vw.Exception.FeedsControllerInvalid);
            }

            entity.FeedsControllerId = feeds;
            await _vwOutputRep.AsUpdateable(entity).UpdateColumns(u => new { u.FeedsControllerId }).ExecuteCommandAsync();
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwControllerOutput);
        }

        public async Task HandleAsync(VwUpdateControllerOutputInput command)
        {
            var entity = await _vwOutputRep.GetFirstAsync(u => u.IsDelete == null && u.ID == command.ID)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            // Gán tay (kể cả null) để xoá được tên hiển thị / ghi chú
            entity.DisplayName = string.IsNullOrWhiteSpace(command.DisplayName) ? null : command.DisplayName.Trim();
            entity.Remark = string.IsNullOrWhiteSpace(command.Remark) ? null : command.Remark.Trim();
            entity.Status = command.Status ?? entity.Status;

            await _vwOutputRep.UpdateWithDiffLogAsync(entity, false);
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwControllerOutput);
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwScreen);
        }
    }
}
