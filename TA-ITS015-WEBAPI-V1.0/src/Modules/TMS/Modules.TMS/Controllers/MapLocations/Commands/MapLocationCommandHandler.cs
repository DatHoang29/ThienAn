using Modules.TMS.Controllers.MapLocations.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Exceptions;
using Modules.TMS.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;

namespace Modules.TMS.Controllers.MapLocations.Commands
{
    /// <summary>
    /// Author: KhoaND
    /// Description: Lớp nghiệp vụ xử lý các lệnh vào của đối tượng tmsMapLocation
    /// Created date: 11/08/2024
    /// </summary>
    public class MapLocationCommandHandler : IWolverineHandler
    {
        private BaseRepository<TmsMapLocation> _tmsMapLocationRep;
        private BaseCacheService _cache;
        private UserManager _userManager;

        public MapLocationCommandHandler(BaseRepository<TmsMapLocation> tmsMapLocationRep, BaseCacheService cache, UserManager userManager)
        {
            _tmsMapLocationRep = tmsMapLocationRep;
            _cache = cache;
            _userManager = userManager;
        }
        /// <summary>
        /// Author: KhoaND, ChuongLP
        /// Description: Hàm lưu thông tin xuống bảng tmsMapLocation
        /// Created date: 11/08/2024, 15/09/2026
        /// </summary>
        public async Task HandleAsync(AddTmsMapLocationInput command)
        {
            //command.Code = command.Code?.Trim(); // ChuongLP - 02/04/2025 - Loại bỏ khoảng trắng ở đầu 
            // ChuongLP - 15/09/2026 - Kiểm tra xem bản ghi đã tồn tại chưa, nếu tồn tại thì update, nếu chưa tồn tại thì insert
            var existed = await _tmsMapLocationRep.GetFirstAsync(u => u.MapId == command.MapId); 

            if (existed == null)
            {
                command.CreateUId = null;
                await _tmsMapLocationRep.InsertWithDiffLogAsync(command.Adapt<TmsMapLocation>());
            }
            else
            {
                command.ID = existed.ID;
                await _tmsMapLocationRep.UpdateWithDiffLogAsync(command.Adapt(existed));
            }
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsMapLocation); // ChuongLP - 01/04/2026 Xóa cache sau khi thêm mới bản ghi
        }


        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm cập nhật thông tin xuống bảng tmsMapLocation
        /// Created date: 11/08/2024
        /// </summary>
        public async Task HandleAsync(UpdateTmsMapLocationInput command)
        {
            //if (await _tmsMapLocationRep.IsAnyAsync(u => u.Code == command.Code && u.ID != command.ID))
            //    throw Oops.Oh(BaseMsg.BaseException.Exist, BaseMsg.BaseEntity.CodeOrName);

            var tmsMapLocation = await _tmsMapLocationRep.GetFirstAsync(u => u.MapId == command.MapId);

            if (tmsMapLocation == null)
            {
                // Create
                command.CreateUId = null;
                await _tmsMapLocationRep.InsertWithDiffLogAsync(command.Adapt<TmsMapLocation>());
                _cache.RemoveByPrefixKey(CacheConst.Tms.TmsMapLocation); // ChuongLP - 01/04/2026 Xóa cache sau khi thêm mới bản ghi
            }
            else
            {
                // Update
                tmsMapLocation.Config = command.Config;

                if (!_userManager.SuperAdmin && tmsMapLocation.CreateUId != _userManager.UserId)
                    throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

                command.ID = tmsMapLocation.ID;   // giữ đúng ID bản ghi, tránh Adapt ghi đè ID rỗng
                await _tmsMapLocationRep.UpdateWithDiffLogAsync(command.Adapt(tmsMapLocation));
                _cache.RemoveByPrefixKey(CacheConst.Tms.TmsMapLocation); // ChuongLP - 01/04/2026 Xóa cache sau khi cập nhật bản ghi
            }
        }
        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm xóa 1 record trong bảng tmsMapLocation
        /// Created date: 11/08/2024
        /// </summary>

        public async Task HandleAsync(DeleteTmsMapLocationInput command)
        {
            var tmsMapLocation = await _tmsMapLocationRep.GetFirstAsync(u => u.ID == command.ID)
                ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            //        if (!_userManager.SuperAdmin && tmsMapLocation.CreateUId != _userManager.UserId)
            //throw Oops.Oh(BaseMsg.BaseException.DataNotExist);


            await _tmsMapLocationRep.SoftDeleteAsync<TmsMapLocation>(tmsMapLocation);
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsMapLocation); // ChuongLP - 01/04/2026 Xóa cache sau khi xóa bản ghi
        }
        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm xóa nhiều record trong bảng tmsMapLocation
        /// Created date: 11/08/2024
        /// </summary>
        public async Task HandleAsync(List<DeleteTmsMapLocationInput> command)
        {
            var ids = command.Select(u => u.ID).ToList();

            var list = _tmsMapLocationRep.AsQueryable().Where(u => ids.Contains(u.ID));

            if (list?.Count() != ids.Count) throw Oops.Oh(ErrorCodeEnum.D6003);


            await _tmsMapLocationRep.SoftDeleteAsync<TmsMapLocation>(l => ids.Contains(l.ID));
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsMapLocation); // ChuongLP - 01/04/2026 Xóa cache sau khi xóa bản ghi
        }
    }
}
