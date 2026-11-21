using Modules.TMS.Core.Dto.EquipmentEventRule;
using Modules.TMS.Core.Entities;
using Modules.TMS.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;

namespace Modules.TMS.Controllers.EquipmentEventRule.Queries
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Lớp nghiệp vụ truy vấn cấu hình hiển thị VMS/LCS theo loại sự kiện (TmsEquipmentEventRule).
    ///              Join TmsEventType + TmsEquipmentType để trả kèm tên hiển thị cho grid.
    /// Created date: 16/07/2026
    /// </summary>
    public class EquipmentEventRuleQueryHandler
    {
        private BaseRepository<TmsEquipmentEventRule> _ruleRep;
        private BaseCacheService _cache;

        public EquipmentEventRuleQueryHandler(BaseRepository<TmsEquipmentEventRule> ruleRep, BaseCacheService cache)
        {
            _ruleRep = ruleRep;
            _cache = cache;
        }

        /// <summary>
        /// Phân trang danh sách rule (kèm tên loại sự kiện + tên loại thiết bị).
        /// Author: ChuongLP
        /// Date: 2027-07-16
        /// </summary>
        public async Task<SqlSugarPagedList<PageEquipmentEventRuleOutput>> HandleAsync(PageEquipmentEventRuleInput command)
        {
            return await _ruleRep.AsQueryable()
                .LeftJoin<TmsEventType>((r, ev) => r.EventTypeId == ev.ID)
                .LeftJoin<TmsEquipmentType>((r, ev, eq) => r.EquipmentTypeId == eq.ID)
                .WhereIF(!string.IsNullOrWhiteSpace(command.EventTypeId), (r, ev, eq) => r.EventTypeId == command.EventTypeId)
                .WhereIF(!string.IsNullOrWhiteSpace(command.EquipmentTypeId), (r, ev, eq) => r.EquipmentTypeId == command.EquipmentTypeId)
                .WhereIF(!string.IsNullOrWhiteSpace(command.ProcessType), (r, ev, eq) => r.ProcessType == command.ProcessType)
                .WhereIF(command.Status != null, (r, ev, eq) => r.Status == command.Status)
                .Select((r, ev, eq) => new PageEquipmentEventRuleOutput
                {
                    EventTypeName = ev.Name,
                    EquipmentTypeName = eq.Name
                }, true)
                .OrderBuilder(command, "", BaseConst.SortFieldConst.UpdateOrCreateTime.Select(f => $"r.{f}"))
                .ToPagedListAsync(command.Page, command.PageSize);
        }

        /// <summary>
        /// Danh sách rule đầy đủ (không phân trang), có kèm tên loại sự kiện + tên loại thiết bị.
        /// Author: ChuongLP
        /// Date: 2027-07-16
        /// </summary>
        public async Task<List<EquipmentEventRuleOutput>> HandleAsync(EquipmentEventRuleInput command)
        {
            // await trong lambda thay vì .Result: .Result block thread của request, dưới tải cao
            // dễ gây nghẽn/deadlock thread pool.
            var list = await _cache.GetOrAdd(CacheConst.Tms.TmsEquipmentEventRule, async _ =>
            {
                return await _ruleRep.AsQueryable()
                    .ClearFilter() // Bỏ qua các filter đã được cấu hình trước đó
                    .Select((u) => new EquipmentEventRuleOutput()
                    {
                    }, true)
                    .OrderBy(u => u.CreateTime)
                    .ToListAsync();
            });

            var ruleList = list.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.EventTypeId), r => r.EventTypeId == command.EventTypeId)
                .WhereIF(!string.IsNullOrWhiteSpace(command.EquipmentTypeId), r => r.EquipmentTypeId == command.EquipmentTypeId)
                .WhereIF(!string.IsNullOrWhiteSpace(command.ProcessType), r => r.ProcessType == command.ProcessType)
                .WhereIF(command.Status != null, r => r.Status == command.Status)
                .OrderBy(r => r.CreateTime)
                .ToList();

            return ruleList;
        }
    }
}
