using Modules.VMS.Controllers.Template.Dto;
using Modules.VMS.Core.Entities;
using Modules.VMS.Infrastructure;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;
using Wolverine;


namespace Modules.VMS.Controllers.Template.Queries
{
    /// <summary>
    /// Description: L?p nghi?p v? x? l? các truy v?n ra c?a ð?i tý?ng VmsTemplate
    /// Create date: 09/08/2024
    /// </summary>
    [WolverineHandler]
    public class TemplateQueryHandler : IScoped
    {
        private readonly BaseRepository<VmsTemplate> _baseRepository;

        public TemplateQueryHandler(BaseRepository<VmsTemplate> baseRepository)
        {
            //this._cache = cache;
            this._baseRepository = baseRepository;
        }

        /// <summary>
        /// Author: 
        /// Description: Hàm list danh sách theo ði?u ki?n t?m ki?m ho?c theo phân trang m?c ð?nh SqlSugar
        /// Created date: 09/08/2024
        /// </summary>
        public async Task<SqlSugarPagedList<PageTemplateOutput>> HandleAsync(PageTemplateInput command)
        {
            var imgList = await this._baseRepository.AsQueryable()
                .WhereIF(!string.IsNullOrEmpty(command.Name), img => img.Name != null && img.Name.Contains(command.Name ?? ""))
                .WhereIF(!string.IsNullOrEmpty(command.Code), img => img.Code != null && img.Code.Contains(command.Code ?? ""))
                .WhereIF(!string.IsNullOrEmpty(command.Lang), img => img.Lang == command.Lang)
                .WhereIF(command.Status != null, u => u.Status == command.Status)
                .Select((img) => new PageTemplateOutput() { }, true)
                .OrderBuilder(command,"","OrderNo",false)
                .ToPagedListAsync(command.Page, command.PageSize);

            return imgList;
        }

        /// <summary>
        /// Author: 
        /// Description: Hàm list danh sách theo giá tr? trý?ng "code" truy?n vào
        /// Created date: 09/08/2024
        /// </summary>
        public async Task<List<TemplateOutput>> HandleAsync(TemplateInput command)
        {
            // TODO: Cache

            var imgList = await this._baseRepository.AsQueryable()
                .WhereIF(!string.IsNullOrEmpty(command.ID), img => img.ID != null && img.ID.Contains(command.ID))
                .WhereIF(!string.IsNullOrEmpty(command.Name), img => img.Name != null && img.Name.Contains(command.Name ?? ""))
                .WhereIF(!string.IsNullOrEmpty(command.Code), img => img.Code != null && img.Code.Contains(command.Code ?? ""))
                .WhereIF(!string.IsNullOrEmpty(command.Lang), img => img.Lang == command.Lang)
                .WhereIF(command.Status != null, u => u.Status == command.Status)
                .Select((img) => new TemplateOutput() { }, true)
                .ToListAsync();

            return imgList;
        }
    }
}
