using Modules.TMS.Controllers.Map.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Exceptions;
using Modules.TMS.Infrastructure;
using Modules.TMS.Infrastructure.Services.Maps;
using Shared.Core.Interfaces;
using Shared.Core.Utilities;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;

namespace Modules.TMS.Controllers.Map.Commands
{
    /// <summary>
    /// Author: KhoaND
    /// Description: Lớp nghiệp vụ xử lý các lệnh vào của đối tượng tmsMap
    /// Created date: 09/08/2024
    /// </summary>
    public class MapCommandHandler : IWolverineHandler
    {
        private BaseRepository<TmsMap> _tmsMapRep;
        private BaseCacheService _cache;
        private UserManager _userManager;
        private IUploadService _uploadService;
        private MapService _mapService;

        public MapCommandHandler(BaseRepository<TmsMap> tmsMapRep, BaseCacheService cache, UserManager userManager, IUploadService service, MapService mapService)
        {
            _tmsMapRep = tmsMapRep;
            _cache = cache;
            _userManager = userManager;
            _uploadService = service;
            _mapService = mapService;
        }

        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm lưu thông tin xuống bảng tmsMap
        /// Created date: 09/08/2024
        /// </summary>
        public async Task HandleAsync(AddMapInput command)
        {
            command.Code = command.Code?.Trim(); // ChuongLP - 02/04/2025 - Loại bỏ khoảng trắng ở đầu và cuối mã bản đồ trước khi kiểm tra và lưu vào cơ sở dữ liệu

            if (await _tmsMapRep.IsAnyAsync(u => u.Code == command.Code))
                throw Oops.Oh(BaseMsg.BaseException.Exist, BaseMsg.BaseEntity.CodeOrName);
            command.CreateUId = null;
            if (command.IsDefault == (int)BaseEnums.DefaultEnum.Default)
            {
                await _tmsMapRep.AsUpdateable().SetColumns(u => new TmsMap() { IsDefault = (int)BaseEnums.DefaultEnum.None }).EnableDiffLogEvent().ExecuteCommandAsync();
                // Retrieve all records where IsDefault is 1 and ID is not the current command ID
                //var recordsToUpdate = await _tmsMapRep.GetListAsync(u => u.IsDefault == 1 && u.ID != command.ID);

                //// Update IsDefault to 0 for each of these records
                //foreach (var record in recordsToUpdate)
                //{
                //    record.IsDefault = 0;
                //    await _tmsMapRep.UpdateAsync(record); // Assuming UpdateAsync takes a TmsMap object
                //}
            }
            await HandleUploadMap(command);

            var entity = command.Adapt<TmsMap>();
            await _tmsMapRep.InsertWithDiffLogAsync(entity);
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsMap); // ChuongLP - 01/14/2026 Xóa cache sau khi thêm mới bản ghi

            // Xử lý copy file cấu hình vị trí
            if (command.IsCopyLocationConfig)
            {
                await _mapService.CopyFileLocationConfig(command, entity.ID);
            }
        }
        /// <summary>
        /// Author: KhoaND, ChuongLP
        /// Description: Hàm cập nhật thông tin xuống bảng tmsMap
        /// Created date: 09/08/2024
        /// </summary>
        public async Task HandleAsync(UpdateMapInput command)
        {
            command.Code = command.Code?.Trim(); // ChuongLP - 02/04/2025 - Loại bỏ khoảng trắng ở đầu và cuối mã bản đồ trước khi kiểm tra và lưu vào cơ sở dữ liệu

            // Check if a duplicate entry exists with the same Name and Code but a different ID
            if (await _tmsMapRep.IsAnyAsync(u => (u.Name == command.Name || u.Code == command.Code) && u.ID != command.ID))
                throw Oops.Oh(BaseMsg.BaseException.Exist, BaseMsg.BaseEntity.CodeOrName);

            // Fetch the tmsMap record or throw an error if not found
            var tmsMap = await _tmsMapRep.GetByIdAsync(command.ID)
                         ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            // Code check trạng thái  - ChuongLP - 06/04/2025 - Kiểm tra trạng thái hiện tại của bản đồ trước khi cập nhật
            if (tmsMap.IsDefault == (int)BaseEnums.DefaultEnum.Default && command.Status == BaseEnums.StatusEnum.Disable && command.IsDefault == (int)BaseEnums.DefaultEnum.Default)
            {
                // Check 1: Nếu đang là bản đồ mặc định thì không được phép tắt
                // (Ưu tiên check cái này trước để báo lỗi rõ ràng)
                throw Oops.Oh(BaseMsg.TmsMap.Exception.ItCannotTurnedOff); //Bản đồ đang được sử dụng mặc định, không thể chuyển về trạng thái không sử dụng.
            }

            // Nếu bản đồ đang (hoặc chuyển sang) trạng thái không dùng thì không được phép bật mặc định - ChuongLP - 13/04/2026
            if (command.IsDefault == (int)BaseEnums.DefaultEnum.Default && command.Status == BaseEnums.StatusEnum.Disable)
            {
                throw Oops.Oh(BaseMsg.TmsMap.Exception.NoSetMapToDefaultToUnusedState);
                // Hoặc dùng: throw Oops.Oh(BaseMsg.TmsMap.Exception.CannotSetDefaultDisabledMap);
            }

            // Check 2: Nếu chuyển từ Enable sang Disable, kiểm tra xem có phải bản ghi Enable cuối cùng không
            if (tmsMap.IsDefault == (int)BaseEnums.DefaultEnum.Default && command.IsDefault == (int)BaseEnums.DefaultEnum.None)
            {
                bool isAnyOtherEnabled = await _tmsMapRep.IsAnyAsync(u => u.IsDefault == (int)BaseEnums.DefaultEnum.Default && u.ID != command.ID);
                if (!isAnyOtherEnabled)
                {
                    throw Oops.Oh(BaseMsg.TmsMap.Exception.MapUseByDefaultCannotBeReverted);  //Không thể tắt vì phải có ít nhất một bản đồ đang hoạt động.
                }
            }
            // If command.IsDefault is 1, update all other records' IsDefault to 0
            if (command.IsDefault == (int)BaseEnums.DefaultEnum.Default)
            {
                await _tmsMapRep.AsUpdateable().SetColumns(u => new TmsMap() { IsDefault = (int)BaseEnums.DefaultEnum.None }).EnableDiffLogEvent().ExecuteCommandAsync();
            }
            await HandleUploadMap(command);
            // Update the tmsMap record and log the changes
            await _tmsMapRep.UpdateWithDiffLogAsync(command.Adapt(tmsMap), false);
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsMap); // ChuongLP - 01/14/2026 Xóa cache sau khi cập nhật bản ghi
        }


        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm xóa 1 record trong bảng tmsMap
        /// Created date: 09/08/2024
        /// </summary>

        public async Task HandleAsync(DeleteMapInput command)
        {
            var tmsMap = await _tmsMapRep.GetFirstAsync(u => u.ID == command.ID)
                ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            // ChuongLP - 15/04/2026 - Kiểm tra không cho phép xóa bản đồ đang mặc định
            if (tmsMap.IsDefault == (int)BaseEnums.DefaultEnum.Default)
                throw Oops.Oh(BaseMsg.TmsMap.Exception.CannotDeleteDefaultMap);

            if (!_userManager.SuperAdmin && tmsMap.CreateUId != _userManager.UserId)
                throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            if (tmsMap.IsDefault == (int)BaseEnums.DefaultEnum.Default)//TODO set lại enum
                throw Oops.Oh(BaseMsg.TmsMap.Exception.MapDefaultCanNotDelete
                ); //Bản đồ đang được sử dụng mặc định, không thể xóa.

            await _tmsMapRep.SoftDeleteAsync<TmsMap>(tmsMap);
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsMap); // ChuongLP - 01/14/2026 Xóa cache sau khi xóa bản ghi
        }

        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm xóa nhiều record trong bảng tmsMap
        /// Created date: 09/08/2024
        /// </summary>
        public async Task HandleAsync(List<DeleteMapInput> command)
        {
            var ids = command.Select(u => u.ID).ToList();

            var list = _tmsMapRep.AsQueryable().Where(u => ids.Contains(u.ID));

            if (list?.Count() != ids.Count)
                throw Oops.Oh(BaseMsg.BaseException.DataNotMatch);

            if (list.Any(x => x.IsDefault == (int)BaseEnums.DefaultEnum.Default)) //TODO set lại enum
                throw Oops.Oh(BaseMsg.TmsMap.Exception.MapDefaultCanNotDelete
                ); //Bản đồ đang được sử dụng mặc định, không thể xóa.



            await _tmsMapRep.SoftDeleteAsync<TmsMap>(l => ids.Contains(l.ID));
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsMap); // ChuongLP - 01/14/2026 Xóa cache sau khi xóa bản ghi
        }

        private async Task HandleUploadMap(AddMapInput command)
        {
            //Neu la hinh anh server
            if (command.Type == "image") //TODO: set lai
            {
                var file = await _uploadService.UploadAsync(new BaseUploadInput()
                {
                    Data = command.Url,
                    FolderPath = "Map" //TODO quy dinh lai sau
                });
                command.Url = file;
            }
        }
    }
}
