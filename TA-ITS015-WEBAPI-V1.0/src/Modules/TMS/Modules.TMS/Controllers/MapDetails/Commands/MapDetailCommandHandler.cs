using Modules.TMS.Controllers.MapDetails.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Exceptions;
using Modules.TMS.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;

namespace Modules.TMS.Controllers.MapDetails.Commands
{
    /// <summary>
    /// Author: KhoaND
    /// Description: Lớp nghiệp vụ xử lý các lệnh vào của đối tượng TmsMapDetail
    /// Created date: 11/08/2024
    /// </summary>
    public class MapDetailCommandHandler : IWolverineHandler
    {
        private BaseRepository<TmsMapDetail> _TmsMapDetailRep;
        private BaseCacheService _cache;
        private UserManager _userManager;

        public MapDetailCommandHandler(BaseRepository<TmsMapDetail> TmsMapDetailRep, BaseCacheService cache, UserManager userManager)
        {
            _TmsMapDetailRep = TmsMapDetailRep;
            _cache = cache;
            _userManager = userManager;
        }
        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm lưu thông tin xuống bảng TmsMapDetail
        /// Created date: 11/08/2024
        /// </summary>
        public async Task HandleAsync(AddTmsMapDetailInput command)
        {
            command.Code = command.Code?.Trim(); // ChuongLP - 02/04/2025 - Loại bỏ khoảng trắng ở đầu

            //if (await _TmsMapDetailRep.IsAnyAsync(u => u.Name == command.Name && u.Code == command.Code))
            //    throw Oops.Oh(ErrorCodeEnum.D6000);
            command.CreateUId = null;
            await _TmsMapDetailRep.InsertWithDiffLogAsync(command.Adapt<TmsMapDetail>());
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsMapDetail); // ChuongLP - 01/04/2026 Xóa cache sau khi thêm mới bản ghi
        }
        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm cập nhật thông tin xuống bảng TmsMapDetail
        /// Created date: 11/08/2024
        /// </summary>
        public async Task HandleAsync(UpdateTmsMapDetailInput command)
        {
            command.Code = command.Code?.Trim(); // ChuongLP - 02/04/2025 - Loại bỏ khoảng trắng ở đầu

            if (await _TmsMapDetailRep.IsAnyAsync(u => u.Code == command.Code && u.ID != command.ID))
                throw Oops.Oh(BaseMsg.BaseException.Exist, BaseMsg.BaseEntity.CodeOrName);

            var tmsMapDetail = await _TmsMapDetailRep.GetByIdAsync(command.ID)
                ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            //if (!_userManager.SuperAdmin && tmsMapDetail.CreateUId != _userManager.UserId)
            //             throw Oops.Oh(ErrorCodeEnum.D6002);

            await _TmsMapDetailRep.UpdateWithDiffLogAsync(command.Adapt(tmsMapDetail));
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsMapDetail); // ChuongLP - 01/04/2026 Xóa cache sau khi cập nhật bản ghi
        }
        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm xóa 1 record trong bảng TmsMapDetail
        /// Created date: 11/08/2024
        /// </summary>

        public async Task HandleAsync(DeleteTmsMapDetailInput command)
        {
            var tmsMapDetail = await _TmsMapDetailRep.GetFirstAsync(u => u.ID == command.ID)
                        ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            //if (!_userManager.SuperAdmin && tmsMapDetail.CreateUId != _userManager.UserId)
            //             throw Oops.Oh(ErrorCodeEnum.D6002);

            //await _TmsMapDetailRep.DeletemapDetailContacts(tmsMapDetail);

        }
        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm xóa nhiều record trong bảng TmsMapDetail
        /// Created date: 11/08/2024
        /// </summary>
        public async Task HandleAsync(List<DeleteTmsMapDetailInput> command)
        {
            var ids = command.Select(u => u.ID).ToList();

            var list = _TmsMapDetailRep.AsQueryable().Where(u => ids.Contains(u.ID));

            if (list?.Count() != ids.Count) throw Oops.Oh(ErrorCodeEnum.D6003);


            await _TmsMapDetailRep.SoftDeleteAsync<TmsMapDetail>(l => ids.Contains(l.ID));
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsMapDetail); // ChuongLP - 01/04/2026 Xóa cache sau khi xóa bản ghi
        }
    }
}
