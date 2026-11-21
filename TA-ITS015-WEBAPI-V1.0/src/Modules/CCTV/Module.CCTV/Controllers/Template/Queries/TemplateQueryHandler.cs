using Modules.CCTV.Controllers.Template.Dto;
using Modules.System.Extensions;
using Modules.CCTV.Core.Entities;
using Modules.TMS.Infrastructure;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Services;
using Wolverine;

namespace Modules.CCTV.Controllers.Template.Queries
{
    public class TemplateQueryHandler : IWolverineHandler
    {
        private BaseRepository<CctvTemplate> _cctvTemplate;
        private BaseCacheService _cache;
        public TemplateQueryHandler(BaseRepository<CctvTemplate> cctvTemplate,BaseCacheService cache)
        {
            _cctvTemplate = cctvTemplate;
            _cache = cache;
        }


        /// <summary>
        /// Tìm kiếm danh sách Template theo tiêu chí lọc.
        /// Author: ChuongLP
        /// Date: 2026-06-04
        /// </summary>
        public async Task<List<TemplateCctvOutput>> HandleAsync(TemplateInput command)
        {
            // Cache lưu entity gốc (Items vẫn là JSON string)
            var entities = _cache.GetOrAdd($"{CacheConst.Cctv.CctvTemplate}", _ =>
            {
                return _cctvTemplate.AsQueryable()
                    .OrderBy(u => u.CreateTime).ToListAsync().Result;
            });

            var filtered = entities
                // Lọc in-memory nên Contains của C# mặc định phân biệt hoa/thường -> dùng OrdinalIgnoreCase để không phân biệt (đồng bộ với các module khác)
                .WhereIF(!string.IsNullOrWhiteSpace(command.Name), u => u.Name != null && u.Name.Contains(command.Name, StringComparison.OrdinalIgnoreCase))
                .WhereIF(command.Status.HasValue, u => u.Status == command.Status)
                .ToList();

            // Map entity → Output, deserialize Items tại đây
            return filtered.Select(u => new TemplateCctvOutput
            {
                ID = u.ID,
                Name = u.Name,
                Remark = u.Remark,
                Status = u.Status,
                Items = DeserializeItems(u.Item),   // ← convert JSON → List
                CreateTime = u.CreateTime,
                CreateUId = u.CreateUId,
                UpdateTime = u.UpdateTime,
                UpdateUId = u.UpdateUId,
            }).ToList();
        }

        /// <summary>
        /// Hàm xử lý deserialize JSON string trong trường Item thành List<TemplateSlot>
        /// Author: ChuongLP
        /// Date: 2026-06-04
        /// </summary>
        private static List<TemplateSlot> DeserializeItems(string? json)
        => string.IsNullOrWhiteSpace(json)
         ? new List<TemplateSlot>()
         : JsonConvert.DeserializeObject<List<TemplateSlot>>(json) ?? new List<TemplateSlot>();
    }
}
