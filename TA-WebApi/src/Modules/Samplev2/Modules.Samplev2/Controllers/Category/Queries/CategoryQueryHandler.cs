
using Modules.Samplev2.Controllers.Category.Dto;
using Modules.Samplev2.Core.Entities;
using Modules.Samplev2.Infrastructure;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;

namespace Modules.Samplev2.Controllers.Category.Queries
{
    public class CategoryQueryHandler
    {
        private BaseRepository<SampCategory> _categoryRep;
        private BaseCacheService _cache;
        public CategoryQueryHandler(BaseRepository<SampCategory> categoryRep, BaseCacheService cache
        )
        {
            _categoryRep = categoryRep;
            _cache = cache;
        }

        public async Task<SqlSugarPagedList<PageCategoryOutput>> HandleAsync(PageCategoryInput command)
        {
            return await _categoryRep.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.Name), u => u.Name.Contains(command.Name))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Description), u => u.Description.Contains(command.Description))
                .Select((u) => new PageCategoryOutput()
                {
                }, true)
                .OrderBuilder(command, "")
                .ToPagedListAsync(command.Page, command.PageSize);
        }
        public async Task<List<CategoryOutput>> HandleAsync(CategoryInput command)
        {
            var list = _cache.GetOrAdd($"sampCategory:{command.Name}", _ =>
            {
                return _categoryRep.AsQueryable()
                    .Select((u) => new CategoryOutput()
                    {
                    }, true)
                    .OrderBy(u => u.CreateTime).ToListAsync().Result;
            });

            list = list.WhereIF(!string.IsNullOrEmpty(command.Name), u => u.Name.Contains(command.Name))
                       .WhereIF(!string.IsNullOrEmpty(command.Description), u => u.Description.Contains(command.Description)).ToList();
            return list;
        }


    }
}
