using Module.ShareData.Core.Dto.Mapping;
using Module.ShareData.Core.Entities;
using Module.ShareData.Infrastructure;
using Module.ShareData.Infrastructure.Services.ShareDataMappingReader;
using Shared.DTO.Constants.Localization;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Wolverine;

namespace Module.ShareData.Controllers.Mapping.Queries
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Xử lý truy vấn Phễu lọc (ShareDataMapping).
    /// Created date: 2026-08-21
    /// </summary>
    public class MappingQueryHandler : IWolverineHandler
    {
        private readonly BaseRepository<ShareDataMapping> _mappingRep;

        public MappingQueryHandler(BaseRepository<ShareDataMapping> mappingRep)
        {
            _mappingRep = mappingRep;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy danh sách phễu lọc theo phân trang.
        /// Created date: 2026-08-21
        /// </summary>
        public async Task<SqlSugarPagedList<ShareDataPageMappingOutput>> HandleAsync(ShareDataPageMappingInput command)
        {
            var pagedList = await _mappingRep.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.Code), u => u.Code != null && u.Code.Contains(command.Code!))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Name), u => u.Name != null && u.Name.Contains(command.Name!))
                .WhereIF(!string.IsNullOrWhiteSpace(command.PartnerId), u => u.PartnerId == command.PartnerId)
                .WhereIF(!string.IsNullOrWhiteSpace(command.DatatypeId), u => u.DatatypeId == command.DatatypeId)
                .WhereIF(command.Direction.HasValue, u => u.Direction == command.Direction)
                .WhereIF(command.IsActive.HasValue, u => u.IsActive == command.IsActive)
                .Select(u => new ShareDataPageMappingOutput() {}, true)
                .OrderBuilder(command, "")
                .ToPagedListAsync(command.Page, command.PageSize);

            foreach (var item in pagedList.Records ?? [])
            {
                item.MappedFieldCount = ShapeReader.CountMappedFields(item.TargetShapeJson);
                item.TargetShapeJson = null;
            }

            return pagedList;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy danh sách phễu lọc theo tiêu chí lọc (không phân trang).
        /// Created date: 2026-08-21
        /// </summary>
        public async Task<List<ShareDataMappingOutput>> HandleAsync(ShareDataMappingInput command)
        {
            var list = await _mappingRep.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.ID), u => u.ID == command.ID)
                .WhereIF(!string.IsNullOrWhiteSpace(command.Code), u => u.Code == command.Code)
                .WhereIF(!string.IsNullOrWhiteSpace(command.PartnerId), u => u.PartnerId == command.PartnerId)
                .WhereIF(!string.IsNullOrWhiteSpace(command.DatatypeId), u => u.DatatypeId == command.DatatypeId)
                .WhereIF(command.Direction.HasValue, u => u.Direction == command.Direction)
                .WhereIF(command.IsActive.HasValue, u => u.IsActive == command.IsActive)
                .Select(u => new ShareDataMappingOutput() {}, true)
                .OrderBy(u => u.CreateTime, OrderByType.Desc)
                .ToListAsync();

            foreach (var item in list)
            {
                item.MappedFieldCount = ShapeReader.CountMappedFields(item.TargetShapeJson);
                item.TargetShapeJson = null;
            }

            return list;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy chi tiết 1 phễu lọc
        ///  Created date: 2026-08-21
        /// </summary>
        public async Task<ShareDataMappingOutput> HandleAsync(ShareDataIdMappingInput command)
        {
            var entity = await _mappingRep.AsQueryable()
                .Where(u => u.ID == command.ID)
                .Select(u => new ShareDataMappingOutput() { }, true)
                .FirstAsync()
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            entity.MappedFieldCount = ShapeReader.CountMappedFields(entity.TargetShapeJson);

            return entity;
        }
    }
}
