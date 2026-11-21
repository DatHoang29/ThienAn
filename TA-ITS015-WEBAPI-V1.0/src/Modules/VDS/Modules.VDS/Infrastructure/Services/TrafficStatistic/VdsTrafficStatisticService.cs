using Shared.QuestHa;
using Modules.VDS.Infrastructure.Services.Persistence.Quest;
using Modules.VDS.Core.Entities;

// KienLT - 27/10/2026: dựng truy vấn trên client do GuardAsync cấp để hỗ trợ router HA.
namespace Modules.VDS.Infrastructure.Services.TrafficStatistic
{
    /// <summary>
    /// Author: KienLT
    /// Description: Repository số liệu thống kê chu kỳ trên QuestDB, đi qua cầu dao
    /// <see cref="VdsRepository{T}"/>.
    ///
    /// <para>Các truy vấn ở đây là LINQ THUẦN (where + order + paging + select) — KHÔNG có <c>SAMPLE BY</c>
    /// nên SqlSugar dịch đúng, dùng LINQ bình thường. Chỉ cần bọc qua <c>GuardAsync</c> để QuestDB xuống
    /// không treo request. Handler gọi <c>GuardAsync(() =&gt; AsQueryable()...)</c>.</para>
    ///
    /// <c>IScoped</c> của Furion: vòng đời theo request.
    /// </summary>
    public class VdsTrafficStatisticService : VdsRepository<VdsTrafficStatistic>, IScoped
    {
        // KienLT - 31/10/2026: đọc HA qua QuestHaManager.ReadAsync (có chứng nhận coverage), không qua router trần.
        public VdsTrafficStatisticService(QuestAvailabilityGate gate, QuestHaManager? questHa = null) : base(gate, questHa)
        {
        }
    }
}
