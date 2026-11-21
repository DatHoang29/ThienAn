using Modules.TMS.Controllers.EventType.Dto;
using Modules.TMS.Core.Constants;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Exceptions;
using Modules.TMS.Infrastructure;
using Shared.Core.Interfaces;
using Shared.Core.Utilities;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;
using Shared.DTO.Upload;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;

namespace Modules.TMS.Controllers.EventType.Commands
{
    /// <summary>
    /// Author: KhoaND
    /// Description: Lớp nghiệp vụ xử lý các lệnh vào của đối tượng tmsEventType
    /// Created date: 11/08/2024
    /// </summary>
    public class EventTypeCommandHandler : IWolverineHandler
    {
        private BaseRepository<TmsEventType> _tmsEventTypeRep;
        private BaseCacheService _cache;
        private UserManager _userManager;
        private IUploadService _uploadService;

        public EventTypeCommandHandler(BaseRepository<TmsEventType> tmsEventTypeRep, BaseCacheService cache, UserManager userManager, IUploadService uploadService)
        {
            _tmsEventTypeRep = tmsEventTypeRep;
            _cache = cache;
            _userManager = userManager;
            _uploadService = uploadService;
        }
        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm lưu thông tin xuống bảng tmsEventType
        /// Created date: 11/08/2024
        /// </summary>
        public async Task HandleAsync(AddEventTypeInput command)
        {
            if (IncidentEventGroupConst.Normalize(command.Type) is null)
                throw Oops.Oh(BaseMsg.BaseValidation.MustBeValidValue, BaseMsg.BaseEntity.Type);

            command.Code = command.Code?.Trim(); // ChuongLP - 02/04/2025 - Loại bỏ khoảng trắng ở đầu và cuối mã loại sự kiện trước khi kiểm tra và lưu vào cơ sở dữ liệu

            if (await _tmsEventTypeRep.IsAnyAsync(u => u.Code == command.Code))
                throw Oops.Oh(BaseMsg.BaseException.Exist, BaseMsg.BaseEntity.Code);
            await HandleUploadFile(command);
            command.CreateUId = null;
            var entity = command.Adapt<TmsEventType>();
            entity.ModuleId = string.IsNullOrWhiteSpace(command.ModuleId) ? null : command.ModuleId.Trim();
            entity.Size = string.IsNullOrWhiteSpace(command.Size) ? null : command.Size.Trim();
            entity.Priority = command.Priority;
            entity.Remark = string.IsNullOrWhiteSpace(command.Remark) ? null : command.Remark.Trim();
            if (string.IsNullOrWhiteSpace(command.Url)) entity.Url = null;
            await _tmsEventTypeRep.InsertWithDiffLogAsync(entity);
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsEventType); // ChuongLP: Xóa cache sau khi thêm mới dữ liệu  
            _cache.RemoveByPrefixKey(CacheConst.Vds.VdsEventSnapshot); // snapshot sự kiện VDS dựng từ danh mục này

        }
        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm cập nhật thông tin xuống bảng tmsEventType
        /// Created date: 11/08/2024
        /// </summary>
        public async Task HandleAsync(UpdateEventTypeInput command)
        {
            if (IncidentEventGroupConst.Normalize(command.Type) is null)
                throw Oops.Oh(BaseMsg.BaseValidation.MustBeValidValue, BaseMsg.BaseEntity.Type);

            command.Code = command.Code?.Trim(); // ChuongLP - 02/04/2025 - Loại bỏ khoảng trắng ở đầu và cuối mã loại sự kiện trước khi kiểm tra và lưu vào cơ sở dữ liệu

            if (await _tmsEventTypeRep.IsAnyAsync(u => u.Code == command.Code && u.ID != command.ID))
                throw Oops.Oh(BaseMsg.BaseException.Exist, BaseMsg.BaseEntity.Code);

            var entity = await _tmsEventTypeRep.GetByIdAsync(command.ID)
                ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);
            await HandleUploadFile(command);
            //if (!_userManager.SuperAdmin && sysPos.CreateUId != _userManager.UserId)
            //    throw Oops.Oh(tmsZone.BaseException.DataNotExist);
            command.Adapt(entity);
            entity.ModuleId = string.IsNullOrWhiteSpace(command.ModuleId) ? null : command.ModuleId.Trim();
            entity.Size = string.IsNullOrWhiteSpace(command.Size) ? null : command.Size.Trim();
            entity.Priority = command.Priority;
            entity.Remark = string.IsNullOrWhiteSpace(command.Remark) ? null : command.Remark.Trim();
            if (string.IsNullOrWhiteSpace(command.Url)) entity.Url = null;
            await _tmsEventTypeRep.UpdateWithDiffLogAsync(entity);
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsEventType); // ChuongLP: Xóa cache sau khi cập nhật dữ liệu
            _cache.RemoveByPrefixKey(CacheConst.Vds.VdsEventSnapshot); // snapshot sự kiện VDS dựng từ danh mục này
        }
        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm xóa 1 record trong bảng tmsEventType
        /// Created date: 11/08/2024
        /// </summary>

        public async Task HandleAsync(DeleteEventTypeInput command)
        {
            var entity = await _tmsEventTypeRep.GetFirstAsync(u => u.ID == command.ID)
                ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);
            //if (!_userManager.SuperAdmin && sysPos.CreateUId != _userManager.UserId)
            //    throw Oops.Oh(ErrorCodeEnum.D6002);

            if (entity.Status == BaseEnums.StatusEnum.Enable)
                throw Oops.Oh(BaseMsg.BaseException.DataIsInUse);

            await _tmsEventTypeRep.SoftDeleteAsync<TmsEventType>(entity);
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsEventType); //ChuongLP: Xóa cache sau khi xóa dữ liệu
            _cache.RemoveByPrefixKey(CacheConst.Vds.VdsEventSnapshot); // snapshot sự kiện VDS dựng từ danh mục này
        }
        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm xóa nhiều record trong bảng tmsEventType
        /// Created date: 11/08/2024
        /// </summary>
        public async Task HandleAsync(List<DeleteEventTypeInput> command)
        {
            var ids = command.Select(u => u.ID).ToList();

            var list = await _tmsEventTypeRep.AsQueryable().Where(u => ids.Contains(u.ID)).ToListAsync();

            if (list?.Count != ids.Count)
                throw Oops.Oh(BaseMsg.BaseException.DataNotMatch);

            if (list.Any(u => u.Status == BaseEnums.StatusEnum.Enable))
                throw Oops.Oh(BaseMsg.BaseException.DataIsInUse);

            await _tmsEventTypeRep.SoftDeleteAsync<TmsEventType>(l => ids.Contains(l.ID));
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsEventType); // ChuongLP: Xóa cache sau khi xóa dữ liệu
            _cache.RemoveByPrefixKey(CacheConst.Vds.VdsEventSnapshot); // snapshot sự kiện VDS dựng từ danh mục này
        }


        /// <summary>
        /// Description: Hàm xử lý upload file
        /// Author: SonTH
        /// Create Date: 2024/12/02
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private async Task HandleUploadFile(AddEventTypeInput input)
        {

            //Xử lý hình ảnh defaultIcon nếu không null
            if (!string.IsNullOrEmpty(input.Url))
            {
                string defaultIconPath = await _uploadService.UploadAsync(new BaseUploadInput()
                {
                    Data = input.Url,
                    FolderPath = BaseUploadConfig.Folder.EventType,
                    //Extension = ".svg"

                });
                input.Url = defaultIconPath;
            }



        }
    }
}
