using Modules.TMS.Controllers.EquipmentType.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Exceptions;
using Modules.TMS.Infrastructure;
using Shared.Core.Interfaces;
using Shared.Core.Utilities;
using Shared.DTO.Constants.Application;
using Shared.DTO.Upload;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;

namespace Modules.TMS.Controllers.EquipmentType.Commands
{
    /// <summary>
    /// Author: KhoaND
    /// Description: Lớp nghiệp vụ xử lý các lệnh vào của đối tượng tmsEquipmentType
    /// Created date: 11/08/2024
    /// </summary>
    public class EquipmentTypeCommandHandler : IWolverineHandler
    {
        private BaseRepository<TmsEquipmentType> _tmsEquipTypeRep;
        private BaseCacheService _cache;
        private IUploadService _uploadService;

        public EquipmentTypeCommandHandler(BaseRepository<TmsEquipmentType> tmsEquipTypeRep,
            BaseCacheService cache,
            IUploadService uploadService)
        {
            _tmsEquipTypeRep = tmsEquipTypeRep;
            _cache = cache;
            _uploadService = uploadService;

        }

        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm lưu thông tin xuống bảng tmsEquipmentType
        /// Created date: 11/08/2024
        /// SonTH fix
        /// </summary>
        public async Task HandleAsync(AddEquipmentTypesInput command)
        {
            command.Code = command.Code?.Trim(); // ChuongLP - 02/04/2025 - Loại bỏ khoảng trắng ở đầu và cuối mã loại thiết bị trước khi kiểm tra và lưu vào cơ sở dữ liệu

            if (await _tmsEquipTypeRep.IsAnyAsync(u => u.Code == command.Code))
                throw Oops.Oh(BaseMsg.BaseException.Exist, BaseMsg.BaseEntity.Code);
            command.CreateUId = null;
            await HandleUploadFile(command);
            await _tmsEquipTypeRep.InsertWithDiffLogAsync(command.Adapt<Core.Entities.TmsEquipmentType>());
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsEquipmentType);
        }

        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm cập nhật thông tin xuống bảng tmsEquipmentType
        /// Created date: 11/08/2024
        /// SonTH fix
        /// </summary>
        public async Task HandleAsync(UpdateEquipmentTypesInput command)
        {
            command.Code = command.Code?.Trim(); // ChuongLP - 02/04/2025 - Loại bỏ khoảng trắng ở đầu và cuối mã loại thiết bị trước khi kiểm tra và lưu vào cơ sở dữ liệu

            if (await _tmsEquipTypeRep.IsAnyAsync(u =>
                   u.Code == command.Code && u.ID != command.ID))
                throw Oops.Oh(BaseMsg.BaseException.Exist, BaseMsg.BaseEntity.Code);

            var entity = await _tmsEquipTypeRep.GetByIdAsync(command.ID)
                         ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            await HandleUploadFile(command);
            await _tmsEquipTypeRep.UpdateWithDiffLogAsync(command.Adapt(entity), false);

            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsEquipmentType);

        }

        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm xóa 1 record trong bảng tmsEquipmentType
        /// Created date: 11/08/2024
        /// SonTH fix
        /// </summary>
        public async Task HandleAsync(DeleteEquipmentTypesInput command)
        {
            var entity = await _tmsEquipTypeRep.GetFirstAsync(u => u.ID == command.ID)
                         ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            await _tmsEquipTypeRep.SoftDeleteAsync<TmsEquipmentType>(entity);

            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsEquipmentType);

        }

        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm xóa nhiều record trong bảng tmsEquipmentType
        /// Created date: 11/08/2024
        /// </summary>
        public async Task HandleAsync(List<DeleteEquipmentTypesInput> command)
        {
            var ids = command.Select(u => u.ID).ToList();

            var list = _tmsEquipTypeRep.AsQueryable().Where(u => ids.Contains(u.ID));

            if (list?.Count() != ids.Count)
                throw Oops.Oh(BaseMsg.BaseException.DataNotMatch);

            await _tmsEquipTypeRep.SoftDeleteAsync<Core.Entities.TmsEquipmentType>(l => ids.Contains(l.ID));
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsEquipmentType);

        }


        /// <summary>
        /// Description: Hàm xử lý upload file
        /// Author: SonTH
        /// Create Date: 2024/12/02
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private async Task HandleUploadFile(AddEquipmentTypesInput input)
        {

            //Xử lý hình ảnh defaultIcon nếu không null
            if (!string.IsNullOrEmpty(input.DefaultIcon))
            {
                string defaultIconPath = await _uploadService.UploadAsync(new BaseUploadInput()
                {
                    Data = input.DefaultIcon,
                    FolderPath = BaseUploadConfig.Folder.EquipmentType,
                    //Extension = ".png"

                });
                input.DefaultIcon = defaultIconPath;
            }

            // Xử lý hình ảnh ActiveIcon nếu không phải null
            if (!string.IsNullOrEmpty(input.ActiveIcon))
            {
                string activeIconPath = await _uploadService.UploadAsync(new BaseUploadInput()
                {
                    Data = input.ActiveIcon,
                    FolderPath = BaseUploadConfig.Folder.EquipmentType,
                    //Extension = ".png"


                });
                input.ActiveIcon = activeIconPath;
            }

            // Xử lý hình ảnh InactiveIcon nếu không phải null
            if (!string.IsNullOrEmpty(input.InactiveIcon))
            {
                string inactiveIconPath = await _uploadService.UploadAsync(new BaseUploadInput()
                {
                    Data = input.InactiveIcon,
                    FolderPath = BaseUploadConfig.Folder.EquipmentType,
                    //Extension = ".png"

                });
                input.InactiveIcon = inactiveIconPath;
            }

            if (!string.IsNullOrEmpty(input.FaultIcon))
            {
                string faultIconPath = await _uploadService.UploadAsync(new BaseUploadInput()
                {
                    Data = input.FaultIcon,
                    FolderPath = BaseUploadConfig.Folder.EquipmentType,
                    //Extension = ".png"

                });
                input.FaultIcon = faultIconPath;
            }

        }
    }
}
