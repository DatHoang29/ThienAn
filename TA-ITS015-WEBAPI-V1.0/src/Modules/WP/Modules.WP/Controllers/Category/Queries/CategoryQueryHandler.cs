
using Modules.WP.Controllers.Category.Dto;
using Modules.WP.Core.Entities;
using Modules.WP.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;
using Shared.Utility.Helpers;
using Shared.Utility.Utilities;
using SqlSugar;

namespace Modules.WP.Controllers.Category.Queries
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Xử lý các truy vấn đọc danh mục hệ thống WP (Category)
    /// Created date: 03/07/2026
    /// </summary>
    public class CategoryQueryHandler
    {
        private BaseRepository<WpCategory> _categoryRep;
        private BaseCacheService _cache;

        public CategoryQueryHandler(BaseRepository<WpCategory> categoryRep, BaseCacheService cache)
        {
            _categoryRep = categoryRep;
            _cache = cache;
        }
       /// <summary>
       /// Author: HieuNV
       /// Description: Lấy danh sách Category theo phân trang
       /// Created date: 14/08/2026
       /// </summary>

       public async Task<SqlSugarPagedList<WpPageCategoryOutput>> HandleAsync(WpPageCategoryInput command)
            {
            return await _categoryRep.AsQueryable()
                //.WhereIF(!string.IsNullOrWhiteSpace(command.Title), u => u.Title.Contains(command.Title))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Code), u => u.Code.Contains(command.Code))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Name), u => u.Name.Contains(command.Name))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Author), u => u.Author.Contains(command.Author))
                .WhereIF(command.IsHero != null, u => u.IsHero == command.IsHero)
                .Select((u) => new WpPageCategoryOutput()
                {
                }, true)
                .OrderBuilder(command, "")
                .ToPagedListAsync(command.Page, command.PageSize);
            }
        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy danh sách Category theo tiêu chí lọc
        /// Created date: 14/08/2026
        /// </summary>
        public async Task<List<WpCategoryOutput>> HandleAsync(WpCategoryInput command)
        {
            var list = _cache.GetOrAdd(CacheConst.WP.WpCategory, _ =>
            {
                return _categoryRep.AsQueryable()
                    .ClearFilter()
                    .Select((u) => new WpCategoryOutput()
                    {
                    }, true)
                    .OrderBy(u => u.CreateTime).ToListAsync().Result;
            });
            var tvrList = list
                // Nếu FE KHÔNG yêu cầu lấy dữ liệu đã xóa (IncludeDelete = false) -> chỉ trả về những dòng chưa bị xóa (IsDelete == null)
                .WhereIF(!command.IncludeDelete, u => u.IsDelete == null)
                .WhereIF(!string.IsNullOrWhiteSpace(command.Code), u => u.Code != null && u.Code.Contains(command.Code))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Name), u => u.Name != null && u.Name.Contains(command.Name))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Author), u => u.Author!= null && u.Code.Contains(command.Author))
                .WhereIF(command.IsHero != null, u => u.IsHero == command.IsHero)
                .OrderBy(u => u.CreateTime)
                .ToList();
            return tvrList;
        }
    }
}
