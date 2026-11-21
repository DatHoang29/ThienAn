using Modules.TMS.Core.Dto.IncidentAutomationConfig;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Exceptions;
using Modules.TMS.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;

namespace Modules.TMS.Controllers.IncidentAutomationConfig.Queries
{
    /// <summary>
    /// Author: SonTH
    /// Description: Lớp nghiệp vụ truy vấn cấu hình tự động xử lý sự kiện/sự cố (TmsIncidentAutomationConfig).
    ///              Join TmsEventType + TmsTrafficPlan để trả kèm tên hiển thị cho grid.
    /// Created date: 24/07/2026
    /// </summary>
    public class IncidentAutomationConfigQueryHandler
    {
        private BaseRepository<TmsIncidentAutomationConfig> _configRep;
        private BaseCacheService _cache;

        public IncidentAutomationConfigQueryHandler(BaseRepository<TmsIncidentAutomationConfig> configRep, BaseCacheService cache)
        {
            _configRep = configRep;
            _cache = cache;
        }

        /// <summary>
        /// Phân trang danh sách cấu hình (kèm tên loại sự kiện + tên kịch bản).
        /// Author: SonTH
        /// Date: 2026-07-24
        /// </summary>
        public async Task<SqlSugarPagedList<PageIncidentAutomationConfigOutput>> HandleAsync(PageIncidentAutomationConfigInput command)
        {
            // Query join 3 bảng cùng kế thừa EntityTenant nên các cột audit (CreateTime, UpdateTime...)
            // và cả Code/Status đều trùng tên -> ORDER BY không có alias sẽ bị SQL Server báo
            // "Ambiguous column name". Vì vậy chuẩn hóa field sort với alias của bảng cấu hình.
            command.Field = NormalizeSortField(command.Field);

            return await _configRep.AsQueryable()
                .LeftJoin<TmsEventType>((c, ev) => c.EventTypeId == ev.ID)
                .LeftJoin<TmsTrafficPlans>((c, ev, p) => c.TrafficPlanId == p.ID)
                .WhereIF(!string.IsNullOrWhiteSpace(command.EventTypeId), (c, ev, p) => c.EventTypeId == command.EventTypeId)
                .WhereIF(command.IsAutoExecuteEnabled != null, (c, ev, p) => c.IsAutoExecuteEnabled == command.IsAutoExecuteEnabled)
                .WhereIF(command.Status != null, (c, ev, p) => c.Status == command.Status)
                .Select((c, ev, p) => new PageIncidentAutomationConfigOutput
                {
                    EventTypeName = ev.Name,
                    TrafficPlanName = p.Name
                }, true)
                .OrderBuilder(command, "", BaseConst.SortFieldConst.UpdateOrCreateTime.Select(f => $"c.{f}"))
                .ToPagedListAsync(command.Page, command.PageSize);
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Thêm alias bảng cấu hình ("c.") vào field sort để tránh lỗi ambiguous column khi join.
        ///              Các field chỉ tồn tại dưới dạng alias trong SELECT (EventTypeName, TrafficPlanName)
        ///              được giữ nguyên vì SQL Server cho phép ORDER BY theo alias của SELECT.
        /// Date: 2026-07-24
        /// </summary>
        private static string NormalizeSortField(string? field)
        {
            if (string.IsNullOrWhiteSpace(field))
                return string.Empty;

            // Field đã có alias (vd "c.CreateTime") thì giữ nguyên
            if (field.Contains('.'))
                return field;

            var selectAliasFields = new[]
            {
                nameof(PageIncidentAutomationConfigOutput.EventTypeName),
                nameof(PageIncidentAutomationConfigOutput.TrafficPlanName)
            };

            if (selectAliasFields.Contains(field, StringComparer.OrdinalIgnoreCase))
                return field;

            return $"c.{field}";
        }

        /// <summary>
        /// Danh sách cấu hình đầy đủ (không phân trang, có cache), kèm tên loại sự kiện + tên kịch bản.
        /// Author: SonTH
        /// Date: 2026-07-24
        /// </summary>
        public async Task<List<IncidentAutomationConfigOutput>> HandleAsync(IncidentAutomationConfigInput command)
        {
            var list = _cache.GetOrAdd(CacheConst.Tms.TmsIncidentAutomationConfig, _ =>
            {
                return _configRep.AsQueryable()
                    .LeftJoin<TmsEventType>((c, ev) => c.EventTypeId == ev.ID)
                    .LeftJoin<TmsTrafficPlans>((c, ev, p) => c.TrafficPlanId == p.ID)
                    // Dùng lambda 3 tham số để SqlSugar sinh ORDER BY kèm alias bảng cấu hình,
                    // tránh lỗi ambiguous column vì cả 3 bảng đều có cột CreateTime.
                    .OrderBy((c, ev, p) => c.CreateTime)
                    .Select((c, ev, p) => new IncidentAutomationConfigOutput
                    {
                        EventTypeName = ev.Name,
                        TrafficPlanName = p.Name
                    }, true)
                    .ToList();
            });

            return list.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.EventTypeId), c => c.EventTypeId == command.EventTypeId)
                .WhereIF(command.IsAutoExecuteEnabled != null, c => c.IsAutoExecuteEnabled == command.IsAutoExecuteEnabled)
                .WhereIF(command.Status != null, c => c.Status == command.Status)
                .OrderBy(c => c.CreateTime)
                .ToList();
        }

        /// <summary>
        /// Lấy cấu hình theo một loại sự kiện cụ thể (dùng cho màn hình quản lý loại sự kiện).
        /// Trả null nếu chưa có cấu hình cho loại sự kiện này.
        /// Author: SonTH
        /// Date: 2026-07-24
        /// </summary>
        public async Task<IncidentAutomationConfigOutput?> HandleAsync(GetIncidentAutomationConfigByEventTypeInput command)
        {
            return await _configRep.AsQueryable()
                .LeftJoin<TmsEventType>((c, ev) => c.EventTypeId == ev.ID)
                .LeftJoin<TmsTrafficPlans>((c, ev, p) => c.TrafficPlanId == p.ID)
                .Where((c, ev, p) => c.EventTypeId == command.EventTypeId)
                .Select((c, ev, p) => new IncidentAutomationConfigOutput
                {
                    EventTypeName = ev.Name,
                    TrafficPlanName = p.Name
                }, true)
                .FirstAsync();
        }
    }
}
