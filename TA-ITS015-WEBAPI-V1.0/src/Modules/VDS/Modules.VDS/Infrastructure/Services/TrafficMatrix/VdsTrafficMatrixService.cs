using Shared.QuestHa;
using Modules.VDS.Infrastructure.Services.Persistence.Quest;
using Modules.VDS.Infrastructure.Services.TrafficMatrix.Sql;
using Shared.Utility.Utilities;
using Modules.VDS.Core.Entities;
using Shared.DTO.Dtos.Vds;

// KienLT - 27/10/2026: dựng truy vấn trên client do GuardAsync cấp để hỗ trợ router HA.
namespace Modules.VDS.Infrastructure.Services.TrafficMatrix
{
    /// <summary>
    /// Author: KienLT
    /// Description: Ma trận thô VDS trên QuestDB, đi qua cầu dao. Trả các ô <c>(zone, direction, hour)</c>
    /// dạng <see cref="VdsTrafficMatrixRow"/>; việc gom cây zone + thang màu để lại TMS (quyết định 3b).
    /// <c>IScoped</c> của Furion.
    /// </summary>
    public class VdsTrafficMatrixService : VdsRepository<VdsTrafficData>, IScoped
    {
        // KienLT - 31/10/2026: đọc HA qua QuestHaManager.ReadAsync (có chứng nhận coverage), không qua router trần.
        public VdsTrafficMatrixService(QuestAvailabilityGate gate, QuestHaManager? questHa = null) : base(gate, questHa)
        {
        }

        /// <summary>Ô kết quả SAMPLE BY (BucketTime giữ nguyên để suy Hour theo giờ server).</summary>
        private sealed class MatrixSampleRow
        {
            public DateTime BucketTime { get; set; }
            public string? ZoneId { get; set; }
            public string? Direction { get; set; }
            public int VehicleCount { get; set; }
            public double? SpeedSum { get; set; }
            public int SpeedSampleCount { get; set; }
        }

        public async Task<List<VdsTrafficMatrixRow>> QueryHourlyAsync(MatrixQueryBuilder.MatrixFilter filter)
        {
            var query = MatrixQueryBuilder.Build(filter);
            var rows = await GuardAsync(questDb =>
                questDb.Ado.SqlQueryAsync<MatrixSampleRow>(query.Sql, query.Parameters));

            return rows.Select(r => new VdsTrafficMatrixRow
            {
                ZoneId = r.ZoneId,
                Direction = r.Direction,
                BucketTime = BaseUtil.TimeServer.ToUtc(r.BucketTime),
                Hour = BaseUtil.TimeServer.ToServerTime(r.BucketTime).Hour,
                VehicleCount = r.VehicleCount,
                SpeedSum = r.SpeedSum,
                SpeedSampleCount = r.SpeedSampleCount,
            }).ToList();
        }
    }
}
