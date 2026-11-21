using Modules.TMS.Infrastructure.Services.Incident.Mapping;
using Modules.TMS.Core.Constants;
using Modules.TMS.Core.Entities;

namespace Modules.TMS.Infrastructure.Services.Incident.Classification
{
    /// <summary>
    /// Author: KienLT
    /// Description: Bù cột <c>EventGroup</c> cho bản ghi LEGACY chưa có snapshot phân loại; bản ghi đã chốt
    /// nhóm thì giữ nguyên.
    ///
    /// <para><b>Vì sao tách khỏi <c>IncidentRuntimeFields</c>:</b> bản cũ là hàm <c>static</c> và bên trong tự
    /// dựng <c>new SqlSugarRepository&lt;TmsEventType&gt;()</c>. Cách đó bỏ qua DI — repository lấy client từ
    /// biến STATIC của <c>SqlSugarSetup</c> nên vẫn chạy, nhưng không theo vòng đời scope của request, không
    /// thay thế được khi test, và lệch mẫu của TMS (mọi service đều <c>IScoped</c> + inject
    /// <c>BaseRepository&lt;T&gt;</c>).</para>
    ///
    /// <para><c>IncidentRuntimeFields.DeadlineAfterCreation</c> vẫn là static và ĐÚNG như vậy: nó là hàm thuần, chỉ tính toán
    /// trên tham số truyền vào, không chạm DB.</para>
    /// Created date: 22/09/2026
    /// </summary>
    public class IncidentGroupResolver : IScoped
    {
        private BaseRepository<TmsEventType> _eventTypeRepo;

        public IncidentGroupResolver(BaseRepository<TmsEventType> eventTypeRepo)
        {
            this._eventTypeRepo = eventTypeRepo;
        }

        /// <summary>
        /// Điền <c>EventGroup</c> cho các bản ghi còn để trống, suy từ <c>TmsEventType.Type</c>.
        ///
        /// <para>Chỉ truy DB khi thật sự có bản ghi thiếu nhóm.</para>
        /// </summary>
        public async Task PopulateGroupsAsync(IEnumerable<TmsIncident> incidents)
        {
            var rows = incidents.Where(x => x.EventGroup == null).ToList();
            if (rows.Count == 0) return;

            var ids = rows.Select(x => x.EventTypeId).Where(x => x != null).Distinct().ToList();
            if (ids.Count == 0) return;

            var types = await this._eventTypeRepo.AsQueryable()
                .Where(t => ids.Contains(t.ID) && t.IsDelete == null).ToListAsync();
            var groups = types.ToDictionary(t => t.ID, t => IncidentEventGroupConst.Normalize(t.Type) ?? "unknown");

            foreach (var row in rows)
                row.EventGroup = row.EventTypeId != null && groups.TryGetValue(row.EventTypeId, out var group) ? group : null;
        }
    }
}
