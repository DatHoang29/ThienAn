using Furion.Localization;
using Mapster;
using Modules.WP.Controllers.Category.Dto;
using Modules.WP.Core.Entities;
using Modules.WP.Core.Exceptions;
using Modules.WP.Infrastructure;
using Shared.Core.Interfaces;
using Shared.Core.Utilities;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;
using Shared.DTO.Upload;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;
using Wolverine;

namespace Modules.WP.Controllers.Category.Commands
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Xử lý các lệnh ghi (Add/Update/Delete) cho danh mục hệ thống WP
    /// Created date: 03/07/2026
    /// </summary>
    public  class CategoryCommandHandler:IWolverineHandler
    {
        private BaseRepository<WpCategory> _categoryRep;
        private BaseRepository<WpPost> _postRep;
        private BaseCacheService _cache;
        private UserManager _userManager;

        public CategoryCommandHandler(BaseRepository<WpCategory> categoryRep, BaseRepository<WpPost> postRep, BaseCacheService cache, UserManager userManager)
        {
            _categoryRep = categoryRep;
            _postRep = postRep;
            _cache = cache;
            _userManager = userManager;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Kiểm tra chuyên mục có bài viết nào đang sử dụng hay không.
        /// Chỉ cho phép bật hiển thị nổi bật (IsHero) khi chuyên mục đã có bài viết.
        /// Created date: 14/08/2026
        /// </summary>
        private async Task EnsureCategoryHasPostAsync(string categoryId)
        {
            var hasPost = await _postRep.AsQueryable()
                .AnyAsync(p => p.Category == categoryId);

            if (!hasPost)
            {
                throw Oops.Oh(BaseMsg.WP.Exception.CategoryHasNoPost);
            }
        }
        /// <summary>
        /// Author: HieuNV
        /// Description: Thêm mới 1 Category
        /// Created date: 14/08/2026
        /// </summary>

        public async Task HandleAsync(WpAddCategoryInput command)
        {
            command.Name = command.Name?.Trim();
            command.Code = command.Code?.Trim();

            if (string.IsNullOrWhiteSpace(command.Code))
            {
                throw Oops.Oh(BaseMsg.BaseValidation.Required, BaseMsg.BaseEntity.Code);
            }
               var isExist = await _categoryRep.AsQueryable()
                .ClearFilter()
                .AnyAsync(u => u.Code == command.Code && u.IsDelete == null);

            if (isExist)
            {
                throw Oops.Oh(BaseMsg.BaseException.Exist, Furion.Localization.L.Text[BaseMsg.BaseEntity.Code]);
            }
            if (command.IsHero == BaseEnums.HeroEnum.Enable)
            {
                // Chuyên mục mới tạo chưa thể có bài viết nào sử dụng nên không được bật nổi bật ngay
                throw Oops.Oh(BaseMsg.WP.Exception.CategoryHasNoPost);
            }
            command.CreateUId = null;
            await _categoryRep.InsertWithDiffLogAsync(command.Adapt<WpCategory>());
            _cache.RemoveByPrefixKey(CacheConst.WP.WpCategory);
        }
        /// <summary>
        /// Author: HieuNV
        /// Description: Cập nhật 1 Category
        /// Created date: 14/08/2026
        /// </summary>


        public async Task HandleAsync(WpUpdateCategoryInput command)
        {
            command.Name = command.Name?.Trim();
            command.Code = command.Code?.Trim();
            command.Author = command.Author?.Trim();

            if (string.IsNullOrWhiteSpace(command.Code))
            {
                throw Oops.Oh(BaseMsg.BaseValidation.Required,L.Text[ BaseMsg.BaseEntity.Code]);
            }
            var isExist = await _categoryRep.AsQueryable()
                .AnyAsync(u =>  u.Code == command.Code && u.ID != command.ID);

            if (isExist)
            {
                throw Oops.Oh(BaseMsg.BaseException.Exist, Furion.Localization.L.Text[BaseMsg.BaseEntity.Code]);
            }
            var categoryPos = await _categoryRep.AsQueryable()
               .Where(u => u.ID == command.ID)
               .FirstAsync() 
               ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            if (command.IsHero == BaseEnums.HeroEnum.Enable)
            {
                // Chỉ cho bật nổi bật khi chuyên mục đã có bài viết sử dụng
                await EnsureCategoryHasPostAsync(command.ID);

                // Đảm bảo chỉ có 1 chuyên mục nổi bật: tắt hero của các chuyên mục khác
                await _categoryRep.AsUpdateable()
                    .SetColumns(x => new WpCategory
                    {
                        IsHero = BaseEnums.HeroEnum.Disable
                    })
                    .Where(x =>
                        x.IsHero == BaseEnums.HeroEnum.Enable &&
                        x.ID != command.ID)
                    .ExecuteCommandAsync();
                categoryPos.IsHero = BaseEnums.HeroEnum.Enable;
            }
            else
            {
                var isOnlyHero = await _categoryRep.AsQueryable()
                    .Where(x => x.IsHero == BaseEnums.HeroEnum.Enable)
                    .CountAsync();

                if (categoryPos.IsHero == BaseEnums.HeroEnum.Enable && isOnlyHero == 1)
                {
                    throw Oops.Oh(BaseMsg.WP.Exception.MustKeepOneHeroCategory);
                }

                categoryPos.IsHero = BaseEnums.HeroEnum.Disable;
            }
            await _categoryRep.UpdateWithDiffLogAsync(command.Adapt(categoryPos));
            _cache.RemoveByPrefixKey(CacheConst.WP.WpCategory);
        }
        /// <summary>
        /// Author: HieuNV
        /// Description: Xóa 1 Category (xóa mềm)
        /// Created date: 14/08/2026
        /// </summary>

        public async Task HandleAsync(WpDeleteCategoryInput command)
        {
            var sysCategory = await _categoryRep.GetFirstAsync(u => u.ID == command.ID) 
                 ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            if (sysCategory.IsHero == BaseEnums.HeroEnum.Enable)
                throw Oops.Oh(BaseMsg.WP.Exception.CannotDeleteHeroCategory);

            await _categoryRep.SoftDeleteAsync<WpCategory>(sysCategory);
            _cache.RemoveByPrefixKey(CacheConst.WP.WpCategory);
        }
        /// <summary>
        /// Author: HieuNV
        /// Description: Xóa nhiều Category (xóa mềm)
        /// Created date: 14/08/2026
        /// </summary>

        public async Task HandleAsync(List<WpDeleteCategoryInput> command)
        {
            var ids = command.Select(u => u.ID).ToList();

            var list = _categoryRep.AsQueryable().Where(u => ids.Contains(u.ID));

            if (list?.Count() != ids.Count)
                throw Oops.Oh(BaseMsg.BaseException.DataNotMatch);

            var hasHeroRecord = await _categoryRep.AsQueryable()
                .AnyAsync(u => ids.Contains(u.ID) && u.IsHero == BaseEnums.HeroEnum.Enable);
            if (hasHeroRecord)
                throw Oops.Oh(BaseMsg.WP.Exception.CannotDeleteHeroCategory);

            await _categoryRep.SoftDeleteAsync<WpCategory>(l => ids.Contains(l.ID));
            _cache.RemoveByPrefixKey(CacheConst.WP.WpCategory);
        }
        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy danh sách Change Status Category theo tiêu chí lọc
        /// Created date: 14/08/2026
        /// </summary>
        public async Task<WpChangeStatusCategoryOutput> HandleAsync(WpChangeStatusCategoryInput command)
        {
            var entity = await _categoryRep.AsQueryable()
                .ClearFilter()
                .FirstAsync(x => x.ID == command.ID)
                ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            if (command.IsHero == BaseEnums.HeroEnum.Enable)
            {
                // Chỉ cho bật nổi bật khi chuyên mục đã có bài viết sử dụng
                await EnsureCategoryHasPostAsync(command.ID);

                // Đảm bảo chỉ có 1 chuyên mục nổi bật: tắt hero của các chuyên mục khác
                await _categoryRep.AsUpdateable()
                     .Where(x =>
                        x.IsHero == BaseEnums.HeroEnum.Enable &&
                        x.ID != command.ID)
                    .SetColumns(x => new WpCategory
                    {
                        IsHero = BaseEnums.HeroEnum.Disable
                    })
                    .ExecuteCommandAsync();
                entity.IsHero = BaseEnums.HeroEnum.Enable;
            }
            else
            {
                var isOnlyHero = await _categoryRep.AsQueryable()
                    .Where(x => x.IsHero == BaseEnums.HeroEnum.Enable)
                    .CountAsync();

                if (entity.IsHero == BaseEnums.HeroEnum.Enable && isOnlyHero == 1)
                {
                    throw Oops.Oh(BaseMsg.WP.Exception.MustKeepOneHeroCategory);
                }

                entity.IsHero = BaseEnums.HeroEnum.Disable;
            }

            await _categoryRep.UpdateAsync(entity);

            _cache.RemoveByPrefixKey(CacheConst.WP.WpCategory);

            return new WpChangeStatusCategoryOutput
            {
                ID = entity.ID,
                IsHero = entity.IsHero.Value
            };
        }
    }
}
