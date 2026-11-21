using Module.ShareData.Core.Dto.CodeSet;
using Module.ShareData.Core.Entities;
using Module.ShareData.Infrastructure;
using Module.ShareData.Infrastructure.Services.ShareDataCodeSetService;
using Shared.DTO.Constants.Localization;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Wolverine;

namespace Module.ShareData.Controllers.CodeSet.Queries
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Xử lý truy vấn Bộ mã quy đổi (ShareDataCodeSet)
    /// Created date: 2026-08-21
    /// </summary>
    public class CodeSetQueryHandler : IWolverineHandler
    {
        private readonly BaseRepository<ShareDataCodeSet> _codeSetRep;
        private readonly BaseRepository<ShareDataMapping> _mappingRep;
        private readonly BaseRepository<ShareDataSubscription> _subscriptionRep;

        public CodeSetQueryHandler(
            BaseRepository<ShareDataCodeSet> codeSetRep,
            BaseRepository<ShareDataMapping> mappingRep,
            BaseRepository<ShareDataSubscription> subscriptionRep)
        {
            _codeSetRep = codeSetRep;
            _mappingRep = mappingRep;
            _subscriptionRep = subscriptionRep;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy danh sách bộ mã theo phân trang
        /// Created date: 2026-08-21
        /// </summary>
        public async Task<SqlSugarPagedList<ShareDataPageCodeSetOutput>> HandleAsync(ShareDataPageCodeSetInput command)
        {
            var pagedList = await _codeSetRep.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.Code), u => u.Code != null && u.Code.Contains(command.Code!))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Name), u => u.Name != null && u.Name.Contains(command.Name!))
                .WhereIF(command.Status.HasValue, u => u.Status == command.Status)
                .Select(u => new ShareDataPageCodeSetOutput() { }, true)
                .OrderBuilder(command, "")
                .ToPagedListAsync(command.Page, command.PageSize);

            var records = pagedList.Records?.ToList() ?? new List<ShareDataPageCodeSetOutput>();
            if (records.Count > 0)
            {
                foreach (var item in records)
                {
                    item.ValueCount = ShareDataCodeSetValueReader.Parse(item.ValuesJson).Values.Count;
                    item.ValuesJson = null;
                }
            }

            return pagedList;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy danh sách bộ mã theo tiêu chí lọc (không phân trang).
        /// Created date: 2026-08-21
        /// </summary>
        public async Task<List<ShareDataCodeSetOutput>> HandleAsync(ShareDataCodeSetInput command)
        {
            var list = await _codeSetRep.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.ID), u => u.ID == command.ID)
                .WhereIF(!string.IsNullOrWhiteSpace(command.Code), u => u.Code == command.Code)
                .WhereIF(!string.IsNullOrWhiteSpace(command.Name), u => u.Name != null && u.Name.Contains(command.Name!))
                .Select(u => new ShareDataCodeSetOutput() { }, true)
                .OrderBy(u => u.Code)
                .ToListAsync();

            var includeValues = command.IncludeValues ?? false;

            foreach (var item in list)
            {
                var (values, defaultSourceValue, defaultPartnerValue) = ShareDataCodeSetValueReader.Parse(item.ValuesJson);
                item.ValueCount = values.Count;
                item.Values = includeValues ? values : null;
                item.DefaultSourceValue = includeValues ? defaultSourceValue : null;
                item.DefaultPartnerValue = includeValues ? defaultPartnerValue : null;
                item.ValuesJson = null;
            }

            return list;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy chi tiết 1 bộ mã
        /// Created date: 2026-08-21
        /// </summary>
        public async Task<ShareDataCodeSetOutput> HandleAsync(ShareDataIdCodeSetInput command)
        {
            var entity = await _codeSetRep.AsQueryable()
                .Where(u => u.ID == command.ID)
                .Select(u => new ShareDataCodeSetOutput() { }, true)
                .FirstAsync()
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            var (values, defaultSourceValue, defaultPartnerValue) = ShareDataCodeSetValueReader.Parse(entity.ValuesJson);
            entity.Values = values;
            entity.ValueCount = values.Count;
            entity.DefaultSourceValue = defaultSourceValue;
            entity.DefaultPartnerValue = defaultPartnerValue;
            entity.ValuesJson = null;
            entity.InUseMappings = await ShareDataCodeSetUsageReader.FindInUseMappingsAsync(_mappingRep, _subscriptionRep, entity.Code);
            return entity;
        }
    }
}
