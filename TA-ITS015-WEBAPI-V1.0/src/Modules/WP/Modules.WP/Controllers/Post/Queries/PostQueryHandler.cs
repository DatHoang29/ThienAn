
using Modules.WP.Controllers.Post.Dto;
using Modules.WP.Core.Entities;
using Modules.WP.Core.Exceptions;
using Modules.WP.Infrastructure;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;
using Shared.Utility.Helpers;
using Shared.Utility.Utilities;
using SqlSugar;

namespace Modules.WP.Controllers.Post.Queries
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Xử lý các truy vấn đọc bài viết hệ thống WP (Post)
    /// Created date: 03/07/2026
    /// </summary>
    public class PostQueryHandler
    {
        private BaseRepository<WpPost> _postRep;
        private BaseCacheService _cache;

        public PostQueryHandler(BaseRepository<WpPost> postRep, BaseCacheService cache)
        {
            _postRep = postRep;
            _cache = cache;
        }
        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy danh sách Post theo phân trang
        /// Created date: 14/08/2026
        /// </summary>

        public async Task<SqlSugarPagedList<PagePostOutput>> HandleAsync(PagePostInput command)
            {
            return await _postRep.AsQueryable()
                //.WhereIF(!string.IsNullOrWhiteSpace(command.Title), u => u.Title.Contains(command.Title))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Code), u => u.Code.Contains(command.Code))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Category), u => u.Category.Contains(command.Category))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Author), u => u.Author.Contains(command.Author))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Title), u => u.Title.Contains(command.Title))
                .WhereIF(command.IsDraft != null, u => u.IsDraft == command.IsDraft)
                .WhereIF(command.IsHero != null, u => u.IsHero == command.IsHero)
                .Select((u) => new PagePostOutput()
                {
                }, true)
                .OrderBuilder(command, "")
                .ToPagedListAsync(command.Page, command.PageSize);
            }
        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy danh sách Post theo tiêu chí lọc
        /// Created date: 14/08/2026
        /// </summary>
        public async Task<List<PostOutput>> HandleAsync(PostInput command)
        {
            return await _postRep.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.Code), u => u.Code.Contains(command.Code))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Category), u => u.Category.Contains(command.Category))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Author), u => u.Author.Contains(command.Author))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Title), u => u.Title.Contains(command.Title))
                .WhereIF(command.IsDraft != null, u => u.IsDraft == command.IsDraft)
                .WhereIF(command.IsHero != null, u => u.IsHero == command.IsHero)
                .Select((u) => new PostOutput()
                {
                }, true)
                .OrderBy(u => u.CreateTime)
                .ToListAsync();
        }
        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy danh sách Post Detail theo tiêu chí lọc
        /// Created date: 14/08/2026
        /// </summary>

        public async Task<PostDetailOutput> HandleAsync(PostDetailInput command)
        {
            if (string.IsNullOrWhiteSpace(command.ID))
                throw Oops.Oh(BaseMsg.BaseException.NotExist, BaseMsg.BaseEntity.Data);

            return await _postRep.AsQueryable()
                .ClearFilter()
                .WhereIF(!string.IsNullOrWhiteSpace(command.ID), u => u.ID.Contains(command.ID))
                .WhereIF(command.IsDraft != null, u => u.IsDraft == command.IsDraft)
                .Select((u) => new PostDetailOutput()
                {
                }, true)
                .FirstAsync();
        }
    }
}
