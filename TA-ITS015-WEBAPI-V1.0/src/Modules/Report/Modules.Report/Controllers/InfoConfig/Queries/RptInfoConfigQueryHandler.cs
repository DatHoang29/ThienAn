using Modules.Report.Core.Constants;
using Modules.Report.Core.Dto.InfoConfig;
using Modules.Report.Core.Entities;
using Modules.Report.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;

namespace Modules.Report.Controllers.InfoConfig.Queries
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Truy vấn cấu hình thông tin báo cáo
    /// Created date: 27/07/2026
    /// </summary>
    public class RptInfoConfigQueryHandler : IWolverineHandler
    {
        private BaseRepository<RptInfoConfig> _infoConfigRepo;
        private BaseCacheService _cache;

        public RptInfoConfigQueryHandler(BaseRepository<RptInfoConfig> infoConfigRepo, BaseCacheService cache)
        {
            _infoConfigRepo = infoConfigRepo;
            _cache = cache;
        }

        /// <summary>
        /// Danh sách theo phân trang
        /// Author: ChuongLP
        /// Date: 2026-07-27
        /// </summary>
        public async Task<SqlSugarPagedList<PageRptInfoConfigOutput>> HandleAsync(PageRptInfoConfigInput command)
        {
            return await _infoConfigRepo.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.ReportCode), ic => ic.ReportCode != null && ic.ReportCode.Contains(command.ReportCode!))
                .WhereIF(!string.IsNullOrWhiteSpace(command.GroupCode), ic => ic.GroupCode == command.GroupCode)
                .WhereIF(!string.IsNullOrWhiteSpace(command.Code), ic => ic.Code != null && ic.Code.Contains(command.Code!))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Name), ic => ic.Name != null && ic.Name.Contains(command.Name!))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Value), ic => ic.Value != null && ic.Value.Contains(command.Value!))
                .Select(ic => new PageRptInfoConfigOutput() { }, true)
                .OrderBuilder(command, "", "OrderNo", false)
                .ToPagedListAsync(command.Page, command.PageSize);
        }

        /// <summary>
        /// Danh sách theo điều kiện tìm kiếm (có cache)
        /// Author: ChuongLP
        /// Date: 2026-07-27
        /// </summary>
        public async Task<List<RptInfoConfigOutput>> HandleAsync(RptInfoConfigInput command)
        {
            var list = await GetEnabledListAsync();

            return list
                .WhereIF(!string.IsNullOrWhiteSpace(command.ReportCode), ic => ic.ReportCode == command.ReportCode)
                .WhereIF(!string.IsNullOrWhiteSpace(command.GroupCode), ic => ic.GroupCode == command.GroupCode)
                .WhereIF(!string.IsNullOrWhiteSpace(command.Code), ic => ic.Code == command.Code)
                .WhereIF(!string.IsNullOrWhiteSpace(command.Name), ic => ic.Name != null && ic.Name.Contains(command.Name!, StringComparison.OrdinalIgnoreCase))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Value), ic => ic.Value != null && ic.Value.Contains(command.Value!, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        /// <summary>
        /// Lấy một record theo id hoặc theo bộ khóa ReportCode + GroupCode + Code
        /// Author: ChuongLP
        /// Date: 2026-07-27
        /// </summary>
        public async Task<RptInfoConfigOutput?> HandleAsync(RptInfoConfigSingleInput command)
        {
            var list = await GetEnabledListAsync();

            return list
                .WhereIF(!string.IsNullOrWhiteSpace(command.Id), ic => ic.ID == command.Id)
                .WhereIF(!string.IsNullOrWhiteSpace(command.ReportCode), ic => ic.ReportCode == command.ReportCode)
                .WhereIF(!string.IsNullOrWhiteSpace(command.GroupCode), ic => ic.GroupCode == command.GroupCode)
                .WhereIF(!string.IsNullOrWhiteSpace(command.Code), ic => ic.Code == command.Code)
                .FirstOrDefault();
        }

        /// <summary>
        /// Lấy cấu hình đã gom nhóm để truyền vào file báo cáo.
        /// Cấu hình riêng của báo cáo ghi đè cấu hình dùng chung ('*' hoặc rỗng).
        /// Author: ChuongLP
        /// Date: 2026-07-27
        /// </summary>
        public async Task<RptInfoConfigDataOutput> HandleAsync(RptInfoConfigDataInput command)
        {
            var list = await GetEnabledListAsync();
            var result = new RptInfoConfigDataOutput();

            // Cấu hình dùng chung nạp trước, cấu hình riêng nạp sau để ghi đè
            var globalItems = list.Where(ic => string.IsNullOrWhiteSpace(ic.ReportCode) || ic.ReportCode == RptInfoConfigConst.GlobalReportCode);
            var ownItems = string.IsNullOrWhiteSpace(command.ReportCode)
                ? Enumerable.Empty<RptInfoConfigOutput>()
                : list.Where(ic => ic.ReportCode == command.ReportCode);

            foreach (var item in globalItems.Concat(ownItems).OrderBy(ic => ic.OrderNo))
            {
                if (string.IsNullOrWhiteSpace(item.GroupCode) || string.IsNullOrWhiteSpace(item.Code))
                    continue;

                if (!result.TryGetValue(item.GroupCode, out var group))
                {
                    group = new Dictionary<string, string?>();
                    result[item.GroupCode] = group;
                }

                group[item.Code] = item.Value;
            }

            return result;
        }

        /// <summary>
        /// Lấy toàn bộ cấu hình đang hoạt động (dùng cache theo prefix rpt_info_config)
        /// Author: ChuongLP
        /// Date: 2026-07-27
        /// </summary>
        private async Task<List<RptInfoConfigOutput>> GetEnabledListAsync()
        {
            var list = await _cache.GetOrAdd(CacheConst.Rpt.RptInfoConfig, _ =>
            {
                return _infoConfigRepo.AsQueryable()
                    .Where(ic => ic.IsDelete == null && ic.Status == BaseEnums.StatusEnum.Enable)
                    .Select(ic => new RptInfoConfigOutput() { }, true)
                    .OrderBy(ic => ic.OrderNo)
                    .ToListAsync();
            });

            return list ?? new List<RptInfoConfigOutput>();
        }
    }
}
