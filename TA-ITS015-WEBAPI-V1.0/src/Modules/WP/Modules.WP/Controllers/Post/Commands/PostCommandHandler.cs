    using Mapster;
    using Modules.WP.Controllers.Post.Dto;
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

namespace Modules.WP.Controllers.Post.Commands
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Xử lý các lệnh ghi (Add/Update/Delete/Upload Image) cho bài viết hệ thống WP
    /// Created date: 03/07/2026
    /// </summary>
    public class PostCommandHandler : IWolverineHandler
    {
        private BaseRepository<WpPost> _postRep;
        private BaseCacheService _cache;
        private UserManager _userManager;
        private IUploadService _uploadService;

        public PostCommandHandler(BaseRepository<WpPost> postRep, BaseCacheService cache, UserManager userManager, IUploadService uploadService)
        {
            _postRep = postRep;
            _cache = cache;
            _userManager = userManager;
            _uploadService = uploadService;
        }
        /// <summary>
        /// Author: HieuNV
        /// Description: Thêm mới 1 Post
        /// Created date: 14/08/2026
        /// </summary>

        public async Task HandleAsync(AddPostInput command)
        {
            command.Title = command.Title?.Trim();
            command.Author = command.Author?.Trim();
            command.Code = command.Code?.Trim();

            if (string.IsNullOrWhiteSpace(command.Code))
            {
                throw Oops.Oh(BaseMsg.BaseValidation.Required, Furion.Localization.L.Text[BaseMsg.BaseEntity.Code]);
            }
            command.IsDraft ??= BaseEnums.StatusEnum.Disable;
            var isExist = await _postRep.AsQueryable()
                .ClearFilter()
                .AnyAsync(u => u.Code == command.Code && u.IsDelete == null);

            if (isExist)
            {
                throw Oops.Oh(BaseMsg.BaseException.Exist, Furion.Localization.L.Text[BaseMsg.BaseEntity.Code]);
            }
            var isHero = command.IsHero == BaseEnums.HeroEnum.Enable;
            if (isHero && command.IsDraft != BaseEnums.StatusEnum.Enable)
            {
                throw Oops.Oh(BaseMsg.WP.Exception.HeroPostMustBePublished);
            }
            command.CreateUId = null;
            await HandleUploadThumbnail(command);
            // Chỉ có 1 bài nổi bật: bài mới bật nổi bật thì gỡ nổi bật bài hiện tại (giống Update/ChangeStatus)
            if (isHero)
            {
                await _postRep.AsUpdateable()
                    .SetColumns(x => new WpPost { IsHero = BaseEnums.HeroEnum.Disable })
                    .Where(x => x.IsHero == BaseEnums.HeroEnum.Enable)
                    .ExecuteCommandAsync();
            }
            await _postRep.InsertWithDiffLogAsync(command.Adapt<WpPost>());
            _cache.RemoveByPrefixKey(CacheConst.WP.WpPost);
        }
        /// <summary>
        /// Author: HieuNV
        /// Description: Cập nhật 1 Post
        /// Created date: 14/08/2026
        /// </summary>


        public async Task HandleAsync(UpdatePostInput command)
        {
            command.Title = command.Title?.Trim();
            command.Author = command.Author?.Trim();
            command.Code = command.Code?.Trim();

            if (string.IsNullOrWhiteSpace(command.Code))
            {
                throw Oops.Oh(BaseMsg.BaseValidation.Required, Furion.Localization.L.Text[BaseMsg.BaseEntity.Code]);
            }
            var isExist = await _postRep.AsQueryable()
                .ClearFilter()
                .AnyAsync(u => u.Code == command.Code && u.ID != command.ID);

            if (isExist)
            {
                throw Oops.Oh(BaseMsg.BaseException.Exist, Furion.Localization.L.Text[BaseMsg.BaseEntity.Code]);
            }
            var sysPos = await _postRep.AsQueryable()
               .ClearFilter()
               .Where(u => u.ID == command.ID)
               .FirstAsync()
               ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);
            command.IsDraft ??= BaseEnums.StatusEnum.Disable;
            if (command.IsHero == BaseEnums.HeroEnum.Enable)
            {
                if (command.IsDraft != BaseEnums.StatusEnum.Enable)
                {
                    throw Oops.Oh(BaseMsg.WP.Exception.HeroPostMustBePublished);
                }
                await _postRep.AsUpdateable()
                    .SetColumns(x => new WpPost
                    {
                        IsHero = BaseEnums.HeroEnum.Disable
                    })
                    .Where(x =>
                        x.IsHero == BaseEnums.HeroEnum.Enable &&
                        x.ID != command.ID)
                    .ExecuteCommandAsync();
                sysPos.IsHero = BaseEnums.HeroEnum.Enable;
            }
            else
            {
                var isOnlyHero = await _postRep.AsQueryable()
                    .Where(x => x.IsHero == BaseEnums.HeroEnum.Enable)
                    .CountAsync();

                if (sysPos.IsHero == BaseEnums.HeroEnum.Enable && isOnlyHero == 1)
                {
                    throw Oops.Oh(BaseMsg.WP.Exception.MustKeepOneHeroPost);
                }

                sysPos.IsHero = BaseEnums.HeroEnum.Disable;
            }
            await HandleUploadThumbnail(command);
            await _postRep.UpdateWithDiffLogAsync(command.Adapt(sysPos));
            _cache.RemoveByPrefixKey(CacheConst.WP.WpPost);
        }
        /// <summary>
        /// Author: HieuNV
        /// Description: Xóa 1 Post (xóa mềm)
        /// Created date: 14/08/2026
        /// </summary>

        public async Task HandleAsync(DeletePostInput command)
        {
            var sysPost = await _postRep.GetFirstAsync(u => u.ID == command.ID)
                 ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            if (sysPost.IsHero == BaseEnums.HeroEnum.Enable)
                throw Oops.Oh(BaseMsg.WP.Exception.CannotDeleteHeroPost);

            await _postRep.SoftDeleteAsync<WpPost>(sysPost);
        }
        /// <summary>
        /// Author: HieuNV
        /// Description: Xóa nhiều Post (xóa mềm)
        /// Created date: 14/08/2026
        /// </summary>

        public async Task HandleAsync(List<DeletePostInput> command)
        {
            var ids = command.Select(u => u.ID).ToList();

            var list = _postRep.AsQueryable().Where(u => ids.Contains(u.ID));

            if (list?.Count() != ids.Count)
                throw Oops.Oh(BaseMsg.BaseException.DataNotMatch);

            var hasHeroRecord = await _postRep.AsQueryable()
                .AnyAsync(u => ids.Contains(u.ID) && u.IsHero == BaseEnums.HeroEnum.Enable);
            if (hasHeroRecord)
                throw Oops.Oh(BaseMsg.WP.Exception.CannotDeleteHeroPost);

            await _postRep.SoftDeleteAsync<WpPost>(l => ids.Contains(l.ID));
            _cache.RemoveByPrefixKey(CacheConst.WP.WpPost);

        }
        /// <summary>
        /// Author: HieuNV
        /// Description: Thêm mới 1 Post
        /// Created date: 14/08/2026
        /// </summary>



        private async Task HandleUploadThumbnail(AddPostInput input)
        {
            if (!string.IsNullOrEmpty(input.Thumbnail))
            {
                string thumbnailPath = await _uploadService.UploadAsync(new BaseUploadInput()
                {
                    Data = input.Thumbnail,
                    FolderPath = BaseUploadConfig.WP.Thumbnail,
                    //Extension = ".png"

                });
                input.Thumbnail = thumbnailPath;
            }
        }
        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy danh sách Upload Post Image theo tiêu chí lọc
        /// Created date: 14/08/2026
        /// </summary>
        public async Task<PostUploadImageOutput> HandleAsync(UploadPostImageInput command)
        {
            if (command.File == null)
                throw Oops.Oh(BaseMsg.WP.Exception.InvalidFile);

            var url = await _uploadService.UploadAsync(
                command.File,
                BaseUploadConfig.WP.Image,
                command.File.FileName
            );

            return new PostUploadImageOutput
            {
                Url = url,
                Name = command.File.FileName
            };
        }
        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy danh sách Change Status Post theo tiêu chí lọc
        /// Created date: 14/08/2026
        /// </summary>
        public async Task<WpChangeStatusPostOutput> HandleAsync(WpChangeStatusPostInput command)
        {
            var entity = await _postRep.AsQueryable()
                .ClearFilter()
                .FirstAsync(x => x.ID == command.ID)
                ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);
            if (command.IsHero == BaseEnums.HeroEnum.Enable)
            {
                if (entity.IsDraft != BaseEnums.StatusEnum.Enable)
                {
                    throw Oops.Oh(BaseMsg.WP.Exception.CannotFeatureArchivedPost);
                }
                await _postRep.AsUpdateable()
                     .Where(x =>
                        x.IsHero == BaseEnums.HeroEnum.Enable &&
                        x.ID != command.ID)
                    .SetColumns(x => new WpPost
                    {
                        IsHero = BaseEnums.HeroEnum.Disable
                    })
                    .ExecuteCommandAsync();
                entity.IsHero = BaseEnums.HeroEnum.Enable;
            }
            else
            {
                var isOnlyHero = await _postRep.AsQueryable()
                    .Where(x => x.IsHero == BaseEnums.HeroEnum.Enable)
                    .CountAsync();

                if (entity.IsHero == BaseEnums.HeroEnum.Enable && isOnlyHero == 1)
                {
                    throw Oops.Oh(BaseMsg.WP.Exception.MustKeepOneHeroPost);
                }

                entity.IsHero = BaseEnums.HeroEnum.Disable;
            }

            await _postRep.UpdateAsync(entity);

            _cache.RemoveByPrefixKey(CacheConst.WP.WpPost);

            return new WpChangeStatusPostOutput
            {
                ID = entity.ID,
                IsHero = entity.IsHero.Value
            };
        }
    }
}
            
