using Shared.QuestHa;
using Modules.VDS.Infrastructure.Services.Persistence.Quest;
using Modules.VDS.Infrastructure.Services.TrafficData.Sql;
using Shared.Utility.Utilities;
using Modules.VDS.Core.Entities;
using Modules.VDS.Controllers.TrafficData.Dto;

// KienLT - 27/10/2026: dựng truy vấn trên client do GuardAsync cấp để hỗ trợ router HA.
namespace Modules.VDS.Infrastructure.Services.TrafficData
{
    /// <summary>
    /// Author: KienLT
    /// Description: Truy vấn lưu lượng xe trên QuestDB bằng SQL thô, đi qua cầu dao
    /// <see cref="VdsRepository{T}"/>.
    ///
    /// <para>Thay hai handler <c>volumebyhour</c>/<c>volumebyday</c> cũ vốn dùng
    /// <c>ISugarQueryable.SampleBy(...)</c> — cách đó KHÔNG sinh <c>SAMPLE BY</c> cho QuestDB nên gộp sai
    /// (đã chứng minh bằng <c>SampleBySqlGenerationTests</c>). Câu lệnh do <see cref="VolumeQueryBuilder"/>
    /// dựng (hàm thuần, có test).</para>
    ///
    /// <para><c>IScoped</c> của Furion: vòng đời theo request, không cần đăng ký tay.</para>
    /// </summary>
    public class VdsTrafficDataService : VdsRepository<VdsTrafficData>, IScoped
    {
        // KienLT - 31/10/2026: đọc HA qua QuestHaManager.ReadAsync (có chứng nhận coverage), không qua router trần.
        public VdsTrafficDataService(QuestAvailabilityGate gate, QuestHaManager? questHa = null) : base(gate, questHa)
        {
        }

        /// <summary>Một ô kết quả của <c>SAMPLE BY</c> — khớp cột SELECT của <see cref="VolumeQueryBuilder"/>.</summary>
        private sealed class VolumeRow
        {
            public DateTime BucketTime { get; set; }
            public string? ZoneId { get; set; }
            public string? Direction { get; set; }
            public string? Lane { get; set; }
            public string? VehicleSize { get; set; }
            public int VehicleCount { get; set; }
        }

        /// <summary>Lưu lượng theo GIỜ trong <c>[from, to)</c>. Mốc là UTC (đã convert ở handler).</summary>
        public Task<List<TrafficDataVolumeOutput>> VolumeByHourAsync(
            VolumeQueryBuilder.VolumeFilter filter, bool includeHour)
        {
            var query = VolumeQueryBuilder.BuildHourly(filter);
            return RunAsync(query, includeHour);
        }

        /// <summary>Lưu lượng theo NGÀY trong <c>[from, to)</c>.</summary>
        public Task<List<TrafficDataVolumeOutput>> VolumeByDayAsync(VolumeQueryBuilder.VolumeFilter filter)
        {
            var query = VolumeQueryBuilder.BuildDaily(filter);
            return RunAsync(query, includeHour: false);
        }

        private async Task<List<TrafficDataVolumeOutput>> RunAsync(
            VolumeQueryBuilder.VolumeQuery query, bool includeHour)
        {
            var rows = await GuardAsync(questDb =>
                questDb.Ado.SqlQueryAsync<VolumeRow>(query.Sql, query.Parameters));

            return rows.Select(r => new TrafficDataVolumeOutput
            {
                Zone = r.ZoneId,
                Direction = r.Direction,
                Lane = r.Lane,
                VehicleSize = r.VehicleSize,
                VehicleCount = r.VehicleCount,
                BucketTime = BaseUtil.TimeServer.ToUtc(r.BucketTime),
                // Nhãn giờ trên biểu đồ phải theo MÚI GIỜ VẬN HÀNH, không phải giờ UTC: người xem đọc "7h"
                // là 7 giờ sáng tại chỗ. Trước 22/09/2026 chỗ này lấy thẳng .Hour của mốc UTC nên lệch 7 tiếng
                // so với cùng khái niệm ở VdsTrafficMatrixService, vốn đã convert sang giờ local.
                // BucketTime ngay trên vẫn là timestamp UTC thật — hai trường khác vai, cố ý khác hệ quy chiếu.
                DetectHour = includeHour
                    ? BaseUtil.TimeServer.ToServerTime(r.BucketTime).Hour
                    : null,
            }).ToList();
        }

        /// <summary>Ô đếm giờ tách direction × vehicleSize, khớp SELECT của <see cref="VolumeBreakdownQueryBuilder"/>.</summary>
        private sealed class BreakdownRow
        {
            public DateTime BucketTime { get; set; }
            public string? Direction { get; set; }
            public string? VehicleSize { get; set; }
            public int VehicleCount { get; set; }
        }

        /// <summary>
        /// Đếm xe theo <c>(giờ, direction, vehicleSize)</c> trong <c>[from, to)</c> — cho report ngày/tháng.
        /// </summary>
        public async Task<List<Shared.DTO.Dtos.Vds.VdsTrafficVolumeBreakdownRow>> VolumeBreakdownAsync(
            DateTime fromUtc, DateTime toUtc, bool countAllEventTypes)
        {
            var query = VolumeBreakdownQueryBuilder.Build(fromUtc, toUtc, countAllEventTypes);
            var rows = await GuardAsync(questDb =>
                questDb.Ado.SqlQueryAsync<BreakdownRow>(query.Sql, query.Parameters));

            return rows.Select(r => new Shared.DTO.Dtos.Vds.VdsTrafficVolumeBreakdownRow
            {
                BucketTime = BaseUtil.TimeServer.ToUtc(r.BucketTime),
                Direction = r.Direction,
                VehicleSize = r.VehicleSize,
                VehicleCount = r.VehicleCount,
            }).ToList();
        }
    }
}
