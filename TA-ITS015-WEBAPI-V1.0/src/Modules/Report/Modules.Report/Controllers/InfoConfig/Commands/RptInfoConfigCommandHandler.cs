using Modules.Report.Core.Dto.InfoConfig;
using Modules.Report.Core.Entities;
using Modules.Report.Core.Exceptions;
using Modules.Report.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;

namespace Modules.Report.Controllers.InfoConfig.Commands
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Xử lý thêm/sửa/xóa cấu hình thông tin báo cáo
    /// Created date: 27/07/2026
    /// </summary>
    public class RptInfoConfigCommandHandler : IWolverineHandler
    {
        private BaseRepository<RptInfoConfig> _infoConfigRepo;
        private BaseCacheService _cache;

        public RptInfoConfigCommandHandler(BaseRepository<RptInfoConfig> infoConfigRepo, BaseCacheService cache)
        {
            _infoConfigRepo = infoConfigRepo;
            _cache = cache;
        }

        /// <summary>
        /// Thêm mới một cấu hình
        /// Author: ChuongLP
        /// Date: 2026-07-27
        /// </summary>
        public async Task HandleAsync(AddRptInfoConfigInput command)
        {
            if (await IsDuplicateAsync(command, null))
                throw Oops.Oh(BaseMsg.RptInfoConfig.Exception.DuplicateKey);

            await _infoConfigRepo.InsertWithDiffLogAsync(command.Adapt<RptInfoConfig>());

            _cache.RemoveByPrefixKey(CacheConst.Rpt.RptInfoConfig);
        }

        /// <summary>
        /// Cập nhật một cấu hình
        /// Author: ChuongLP
        /// Date: 2026-07-27
        /// </summary>
        public async Task HandleAsync(UpdateRptInfoConfigInput command)
        {
            if (await IsDuplicateAsync(command, command.ID))
                throw Oops.Oh(BaseMsg.RptInfoConfig.Exception.DuplicateKey);

            var entity = await _infoConfigRepo.GetByIdAsync(command.ID);
            if (entity == null)
                throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            await _infoConfigRepo.UpdateWithDiffLogAsync(command.Adapt(entity));

            _cache.RemoveByPrefixKey(CacheConst.Rpt.RptInfoConfig);
        }

        /// <summary>
        /// Xóa một cấu hình
        /// Author: ChuongLP
        /// Date: 2026-07-27
        /// </summary>
        public async Task HandleAsync(DeleteRptInfoConfigInput command)
        {
            var entity = await _infoConfigRepo.GetFirstAsync(ic => ic.ID == command.ID);
            if (entity == null)
                throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            if (entity.IsSysConfig == true)
                throw Oops.Oh(BaseMsg.BaseException.CannotDelete);

            await _infoConfigRepo.SoftDeleteAsync<RptInfoConfig>(entity);

            _cache.RemoveByPrefixKey(CacheConst.Rpt.RptInfoConfig);
        }

        /// <summary>
        /// Xóa nhiều cấu hình
        /// Author: ChuongLP
        /// Date: 2026-07-27
        /// </summary>
        public async Task HandleAsync(List<DeleteRptInfoConfigInput> command)
        {
            var ids = command.Select(item => item.ID).ToList();
            var list = await _infoConfigRepo.AsQueryable().Where(ic => ids.Contains(ic.ID)).ToListAsync();

            if (list?.Count != ids.Count)
                throw Oops.Oh(BaseMsg.BaseException.DataNotMatch);

            if (list.Any(ic => ic.IsSysConfig == true))
                throw Oops.Oh(BaseMsg.BaseException.CannotDelete);

            await _infoConfigRepo.SoftDeleteAsync<RptInfoConfig>(ic => ids.Contains(ic.ID));

            _cache.RemoveByPrefixKey(CacheConst.Rpt.RptInfoConfig);
        }

        /// <summary>
        /// Kiểm tra trùng bộ khóa ReportCode + GroupCode + Code
        /// Author: ChuongLP
        /// Date: 2026-07-27
        /// </summary>
        private async Task<bool> IsDuplicateAsync(RptInfoConfig input, string? excludeId)
        {
            var reportCode = string.IsNullOrWhiteSpace(input.ReportCode) ? null : input.ReportCode.Trim();

            return await _infoConfigRepo.IsAnyAsync(ic =>
                ic.IsDelete == null
                && ic.GroupCode == input.GroupCode
                && ic.Code == input.Code
                && ic.ReportCode == reportCode
                && (excludeId == null || ic.ID != excludeId));
        }
    }
}
