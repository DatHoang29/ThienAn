using Modules.TMS.Controllers.Module.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Exceptions;
using Modules.TMS.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;

namespace Modules.TMS.Controllers.Module.Commands
{
    /// <summary>
    /// Author: KhoaND
    /// Description: Lớp nghiệp vụ xử lý các lệnh vào của đối tượng tmsModule
    /// Created date: 11/08/2024
    /// </summary>
    public class ModuleCommandHandler : IWolverineHandler
    {
        private BaseRepository<TmsModule> _tmsModuleRep;
        private BaseCacheService _cache;
        private UserManager _userManager;

        public ModuleCommandHandler(BaseRepository<TmsModule> tmsModuleRep, BaseCacheService cache, UserManager userManager)
        {
            _tmsModuleRep = tmsModuleRep;
            _cache = cache;
            _userManager = userManager;
        }
        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm lưu thông tin xuống bảng tmsModule
        /// Created date: 11/08/2024
        /// </summary>
        public async Task HandleAsync(AddTmsModuleInput command)
        {
            command.Code = command.Code?.Trim(); // ChuongLP - 02/04/2025 - Loại bỏ khoảng trắng
            command.Name = command.Name?.Trim(); // ChuongLP - 02/04/2025 - Loại bỏ khoảng trắng

            if (await _tmsModuleRep.IsAnyAsync(u => u.Name == command.Name && u.Code == command.Code))
                throw Oops.Oh(BaseMsg.BaseException.Exist, Furion.Localization.L.Text[BaseMsg.BaseEntity.CodeOrName]);
            command.CreateUId = null;
            await _tmsModuleRep.InsertWithDiffLogAsync(command.Adapt<TmsModule>());
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsModule); // ChuongLP - 01/04/2026 Xóa cache sau khi thêm mới bản ghi
        }
        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm cập nhật thông tin xuống bảng tmsModule
        /// Created date: 11/08/2024
        /// </summary>
        public async Task HandleAsync(UpdateTmsModuleInput command)
        {
            command.Code = command.Code?.Trim(); // ChuongLP - 02/04/2025 - Loại bỏ khoảng trắng
            command.Name = command.Name?.Trim(); // ChuongLP - 02/04/2025 - Loại bỏ khoảng trắng

            if (await _tmsModuleRep.IsAnyAsync(u => u.Name == command.Name && u.Code == command.Code && u.ID != command.ID))
                throw Oops.Oh(BaseMsg.BaseException.Exist, Furion.Localization.L.Text[BaseMsg.BaseEntity.CodeOrName]);

            var enitty = await _tmsModuleRep.GetByIdAsync(command.ID)
                         ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);
            //if (!_userManager.SuperAdmin && sysPos.CreateUId != _userManager.UserId)
            //    throw Oops.Oh(ErrorCodeEnum.D6002);
            await _tmsModuleRep.UpdateWithDiffLogAsync(command.Adapt(enitty));
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsModule); // ChuongLP - 01/04/2026 Xóa cache sau khi cập nhật bản ghi
        }
        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm xóa 1 record trong bảng tmsModule
        /// Created date: 11/08/2024
        /// </summary>

        public async Task HandleAsync(DeleteTmsModuleInput command)
        {
            var entity = await _tmsModuleRep.GetFirstAsync(u => u.ID == command.ID)
                         ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);
            //        if (!_userManager.SuperAdmin && sysPos.CreateUId != _userManager.UserId)
            //throw Oops.Oh(BaseMsg.BaseException.DataNotExist);


            await _tmsModuleRep.SoftDeleteAsync<TmsModule>(entity);
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsModule); // ChuongLP - 01/04/2026 Xóa cache sau khi xóa bản ghi
        }
        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm xóa nhiều record trong bảng tmsModule
        /// Created date: 11/08/2024
        /// </summary>
        public async Task HandleAsync(List<DeleteTmsModuleInput> command)
        {
            var ids = command.Select(u => u.ID).ToList();

            var list = _tmsModuleRep.AsQueryable().Where(u => ids.Contains(u.ID));

            if (list?.Count() != ids.Count)
                throw Oops.Oh(BaseMsg.BaseException.DataNotFound);


            await _tmsModuleRep.SoftDeleteAsync<TmsModule>(l => ids.Contains(l.ID));
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsModule); // ChuongLP - 01/04/2026 Xóa cache sau khi xóa bản ghi
        }
    }
}
