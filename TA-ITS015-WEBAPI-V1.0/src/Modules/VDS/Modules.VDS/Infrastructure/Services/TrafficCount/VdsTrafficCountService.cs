using Shared.QuestHa;
using Modules.VDS.Infrastructure.Services.Persistence.Quest;
using Shared.Utility.Counting;
using Modules.VDS.Core.Entities;
using Modules.VDS.Core.Exceptions;

// KienLT - 27/10/2026: dựng truy vấn trên client do GuardAsync cấp để hỗ trợ router HA.
namespace Modules.VDS.Infrastructure.Services.TrafficCount
{
    /// <summary>
    /// Author: KienLT
    /// Description: Số ĐẾM XE cho WebAPI, tính từ QuestDB bằng <c>SAMPLE BY</c> qua
    /// <see cref="CountQueryBuilder"/> DÙNG CHUNG với worker, gộp bằng <see cref="CountSliceFolder"/> DÙNG
    /// CHUNG. Nhờ vậy số của WebAPI và worker khớp tuyệt đối (yêu cầu §5.4 của kế hoạch).
    ///
    /// Đi qua cầu dao <see cref="VdsRepository{T}"/> để QuestDB xuống không treo request.
    /// <c>IScoped</c> của Furion.
    /// </summary>
    public class VdsTrafficCountService : VdsRepository<VdsTrafficData>, IScoped
    {
        // KienLT - 31/10/2026: đọc HA qua QuestHaManager.ReadAsync (có chứng nhận coverage), không qua router trần.
        public VdsTrafficCountService(QuestAvailabilityGate gate, QuestHaManager? questHa = null) : base(gate, questHa)
        {
        }

        /// <summary>
        /// Đọc các ô đếm trong <c>[fromUtc, toUtc)</c> theo <paramref name="scopeType"/>
        /// (equipment/zone), gộp về <see cref="TrafficCountSlice"/>.
        /// </summary>
        /// <typeparam name="TFilter">Bộ lọc vừa là <see cref="ICountQueryFilter"/> vừa là scope camera.</typeparam>
        public async Task<List<TrafficCountSlice>> ReadSlicesAsync<TFilter>(
            TFilter filter, int periodMinutes, string scopeType, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
            where TFilter : ICountQueryFilter, CountSliceFolder.IEquipmentScope
        {
            if (scopeType is not (TrafficCountScopes.Equipment or TrafficCountScopes.Zone)) throw Oops.Oh(BaseMsg.Vds.Exception.ScopeTypeInvalid);
            var period = TrafficCountMath.NormalizePeriod(periodMinutes);
            if (period > 60 || 60 % period != 0) throw Oops.Oh(BaseMsg.Vds.Exception.CountPeriodInvalid);
            var from = TrafficCountMath.FloorPeriod(fromUtc, period);
            if (from >= toUtc) return new List<TrafficCountSlice>();

            var query = CountQueryBuilder.Build(filter, period, from, toUtc);
            var rows = await GuardAsync(async questDb =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var db = questDb;
                var previous = db.Ado.CancellationToken;
                db.Ado.CancellationToken = cancellationToken;
                try { return await db.Ado.SqlQueryAsync<CountSampleRow>(query.Sql, query.Parameters); }
                finally { db.Ado.CancellationToken = previous; }
                // KienLT - 30/10/2026: token tới cả bước đọc journal/chọn node/mở lease, không chỉ lệnh Ado cuối.
            }, cancellationToken);

            return CountSliceFolder.Fold(rows, period, filter, scopeType);
        }
    }
}
