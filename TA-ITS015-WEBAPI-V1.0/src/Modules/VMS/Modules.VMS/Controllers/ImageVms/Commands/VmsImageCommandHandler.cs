using Mapster;
using Modules.VMS.Controllers.ImageVms.Dto;
using Modules.VMS.Core.Entities;
using Modules.VMS.Core.Exceptions;
using Modules.VMS.Infrastructure;
using Modules.VMS.Infrastructure.Services.Imaging;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;
using Wolverine;

namespace Modules.VMS.Controllers.ImageVms.Commands
{
    /// <summary>
    /// Author: ThangNN
    /// Created date: 01/12/2024
    /// </summary>
    public class VmsImageCommandHandler : IWolverineHandler
    {
        private BaseRepository<VmsImage> _imageRepo;
        private BaseCacheService _cache;
        private ImageService _imageService;

        public VmsImageCommandHandler(BaseRepository<VmsImage> imageRepo, BaseCacheService cache, ImageService imageService)
        {
            this._cache = cache;
            this._imageRepo = imageRepo;
            this._imageService = imageService;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Thêm một record
        /// Created date: 01/12/2024
        /// </summary>
        public async Task HandleAsync(AddVmsImageInput command)
        {
            command.Code = command.Code?.Trim(); // ChuongLP - 02/04/2025 - Loại bỏ khoảng trắng ở đầu 
            command.Name = command.Name?.Trim(); // ChuongLP - 02/04/2025 - Loại bỏ khoảng trắng ở đầu 

            // Mã hình ảnh phải duy nhất toàn cục
            if (await this._imageRepo.IsAnyAsync(img => img.IsDelete == null && img.Code == command.Code))
                throw Oops.Oh(BaseMsg.BaseException.Exist, BaseMsg.BaseEntity.Code);

            // Create new vmsImage object
            var img = command.Adapt<VmsImage>();

            // Upload new image
            if (this._imageService.IsBase64String(command.Url))
                img.Url = await this._imageService.SaveImageAsync(command.Url, img.ID);
            else
                img.Url = string.Empty;

            await this._imageRepo.InsertWithDiffLogAsync(img);
            _cache.RemoveByPrefixKey(CacheConst.Vms.VmsImage); // ChuongLP - 01/04/2026 - Xóa cache sau khi thêm mới record
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Cập nhật một record
        /// Created date: 01/12/2024
        /// </summary>
        public async Task HandleAsync(UpdateVmsImageInput command)
        {
            command.Code = command.Code?.Trim(); // ChuongLP - 02/04/2025 - Loại bỏ khoảng trắng ở đầu 
            command.Name = command.Name?.Trim(); // ChuongLP - 02/04/2025 - Loại bỏ khoảng trắng ở đầu 

            // Mã hình ảnh phải duy nhất toàn cục (bỏ qua chính bản ghi đang sửa)
            if (await this._imageRepo.IsAnyAsync(img => img.IsDelete == null && img.Code == command.Code && img.ID != command.ID))
                throw Oops.Oh(BaseMsg.BaseException.Exist, BaseMsg.BaseEntity.Code);

            var img = await this._imageRepo.GetByIdAsync(command.ID);
            if (img == null)
                throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            // Upload new image
            if(this._imageService.IsBase64String(command.Url))
                command.Url = await this._imageService.SaveImageAsync(command.Url, img.ID);

            await this._imageRepo.UpdateWithDiffLogAsync(command.Adapt(img));

            _cache.RemoveByPrefixKey(CacheConst.Vms.VmsImage); // ChuongLP - 01/04/2026 - Xóa cache sau khi cập nhật record
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Xóa một record
        /// Created date: 01/12/2024
        /// </summary>
        public async Task HandleAsync(DeleteVmsImageInput command)
        {
            var img = await this._imageRepo.GetFirstAsync(img => img.ID == command.ID);
            if (img == null)
                throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            await this._imageRepo.SoftDeleteAsync<VmsImage>(img);

            _cache.RemoveByPrefixKey(CacheConst.Vms.VmsImage); // ChuongLP - 01/04/2026 - Xóa cache sau khi xóa record
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Xóa nhiều record
        /// Created date: 01/12/2024
        /// </summary>
        public async Task HandleAsync(List<DeleteVmsImageInput> command)
        {
            List<string> ids = command.Select(item  => item.ID).ToList();
            List<VmsImage> imgList = await this._imageRepo.AsQueryable().Where(img => ids.Contains(img.ID)).ToListAsync();

            if (imgList?.Count() != ids.Count)
                throw Oops.Oh(BaseMsg.BaseException.DataNotMatch);

            await this._imageRepo.SoftDeleteAsync<VmsImage>(img => ids.Contains(img.ID));

            _cache.RemoveByPrefixKey(CacheConst.Vms.VmsImage); // ChuongLP - 01/04/2026 - Xóa cache sau khi xóa record
        }
    }
}
