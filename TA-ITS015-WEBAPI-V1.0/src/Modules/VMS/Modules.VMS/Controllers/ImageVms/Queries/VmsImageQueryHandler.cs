using Modules.VMS.Controllers.ImageVms.Dto;
using Modules.VMS.Core.Entities;
using Modules.VMS.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;
using Wolverine;

namespace Modules.VMS.Controllers.ImageVms.Queries
{
    /// <summary>
    /// Author: ThangNN
    /// Created date: 01/12/2024
    /// </summary>
    public class VmsImageQueryHandler : IWolverineHandler
    {
        private BaseRepository<VmsImage> _imageRepo;
        private BaseCacheService _cache;


        public VmsImageQueryHandler(BaseRepository<VmsImage> imageRepo, BaseCacheService cache)
        {
            this._cache = cache;
            this._imageRepo = imageRepo;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Lis theo phân trang
        /// Created date: 01/12/2024
        /// </summary>
        public async Task<SqlSugarPagedList<PageVmsImageOutput>> HandleAsync(PageVmsImageInput command)
        {
            var imgList = await this._imageRepo.AsQueryable()
                .WhereIF(!string.IsNullOrEmpty(command.Name), img => img.Name != null && img.Name.Contains(command.Name, StringComparison.OrdinalIgnoreCase))
                .WhereIF(!string.IsNullOrEmpty(command.Code), img => img.Code != null && img.Code.Contains(command.Code, StringComparison.OrdinalIgnoreCase))
                .WhereIF(!string.IsNullOrEmpty(command.Type), img => img.Type != null && img.Type.Equals(command.Type, StringComparison.OrdinalIgnoreCase))
                .WhereIF(!string.IsNullOrEmpty(command.EquipmentTypeId), img => img.EquipmentTypeId == command.EquipmentTypeId)
                .WhereIF(!string.IsNullOrEmpty(command.Lang), img => img.Lang != null && img.Lang.Equals(command.Lang, StringComparison.OrdinalIgnoreCase))
                .WhereIF(command.Status != null, u => u.Status == command.Status)
                .Where(img => img.IsDelete == null)
                .Select(img => new PageVmsImageOutput() { }, true)
                .OrderBuilder(command, "", "CreateTime", false)
                .ToPagedListAsync(command.Page, command.PageSize);

            return imgList;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: List theo tìm kiếm
        /// Created date: 01/12/2024
        /// </summary>
        public async Task<List<VmsImageOutput>> HandleAsync(VmsImageInput command)
        {
            var imgList = await this._cache.GetOrAdd(CacheConst.Vms.VmsImage, _ =>
            {
                return this._imageRepo.AsQueryable()
                    .Select(img => new VmsImageOutput() { }, true)
                    .OrderBy(img => img.OrderNo)
                    .ToListAsync();
            });

            var result = imgList?
                .WhereIF(!string.IsNullOrEmpty(command.Name), img => img.Name != null && img.Name.Contains(command.Name, StringComparison.OrdinalIgnoreCase))
                .WhereIF(!string.IsNullOrEmpty(command.Code), img => img.Code != null && img.Code.Contains(command.Code, StringComparison.OrdinalIgnoreCase))
                .WhereIF(!string.IsNullOrEmpty(command.Type), img => img.Type != null && img.Type.Equals(command.Type, StringComparison.OrdinalIgnoreCase))
                .WhereIF(!string.IsNullOrEmpty(command.EquipmentTypeId), img => img.EquipmentTypeId == command.EquipmentTypeId)
                .WhereIF(!string.IsNullOrEmpty(command.Lang), img => img.Lang != null && img.Lang.Equals(command.Lang, StringComparison.OrdinalIgnoreCase))
                .WhereIF(command.Status != null, u => u.Status == command.Status)
                .Where(img => img.Status == BaseEnums.StatusEnum.Enable && img.IsDelete == null)
                .ToList();

            return result;
        }

        /// <summary>
        /// Author: ThangNN
        /// Created date: 01/12/2024
        /// Description: Thông tin chi tiết
        /// </summary>
        public async Task<VmsImageOutput> HandleAsync(VmsImageSingleInput command)
        {
            var imgList = await this._cache.GetOrAdd(CacheConst.Vms.VmsImage, _ =>
            {
                return this._imageRepo.AsQueryable()
                    .Select(img => new VmsImageOutput() { }, true)
                    .OrderBy(img => img.OrderNo)
                    .ToListAsync();
            });

            var result = imgList?
                .WhereIF(!string.IsNullOrEmpty(command.Id), msg => msg.ID == command.Id)
                .WhereIF(!string.IsNullOrEmpty(command.Code), msg => msg.Code == command.Code)
                .FirstOrDefault();

            return result;
        }
    }
}
