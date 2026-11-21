using Microsoft.AspNetCore.Authorization;
using Shared.Utility.Sse;
using Modules.VDS.Controllers.TrafficStatistic.Dto;
using Modules.VDS.Infrastructure;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Sse;

namespace Modules.VDS.Controllers.TrafficStatistic
{
    /// <summary>
    /// Author: KienLT
    /// Description: API số liệu thống kê chu kỳ VDS (đọc QuestDB). Thay 4 endpoint ĐỌC của
    /// <c>api/tms/tmsTrafficStatistic</c>. KHÔNG có Add/Update/Delete: worker là nguồn ghi duy nhất.
    ///
    /// Route: <c>api/vds/vdstrafficstatistic/*</c>, GroupName VDS.
    /// Created date: 03/09/2026
    /// </summary>
    [ApiController]
    [ApiDescriptionSettings(GroupName, Order = 586)]
    public class VdsTrafficStatisticController : BaseController
    {
        /// <summary>
        /// Stream VDS traffic statistic snapshot over Server-Sent Events
        /// </summary>
        /// <param name="refresh">Chu kỳ gửi lại, tính bằng giây.</param>
        /// <param name="windowMinutes">Độ dài cửa sổ trượt, tính bằng phút.</param>
        /// <param name="equipmentId">Lọc theo thiết bị.</param>
        /// <param name="zoneId">Lọc theo vùng.</param>
        /// <param name="direction">Lọc theo hướng.</param>
        /// <returns>Luồng SSE, mỗi nhịp là một snapshot với cửa sổ tính lại theo hiện tại.</returns>
        [HttpGet("Stream")]
        [Sse("statistic", HeartbeatSeconds = 0)]
        [Authorize(Policy = SnapshotSseAuthorization.Policy)]
        [DisplayName("Traffic statistic stream (SSE)")]
        public SseResult Stream([Range(1, 60)] int refresh = 5, [Range(1, 1440)] int windowMinutes = 60,
            string? equipmentId = null, string? zoneId = null, string? direction = null)
        {
            // Cửa sổ tính lại mỗi nhịp: stream phải trượt theo hiện tại, không đóng băng mốc lúc mở kết nối.
            // Lấy message bus từ scope mà hub cấp, không bắt MessBus của request: vòng dùng chung sống lâu hơn.
            return SnapshotSse.Poll(
                $"statistic|w={windowMinutes}|e={equipmentId}|z={zoneId}|d={direction}",
                async (sp, ct) =>
                {
                    var to = DateTime.UtcNow;
                    var bus = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions
                        .GetRequiredService<Wolverine.IMessageBus>(sp);
                    return await bus.InvokeAsync<StatisticSnapshotOutput>(new StatisticSnapshotInput
                    {
                        FromDate = to.AddMinutes(-windowMinutes), ToDate = to,
                        EquipmentId = equipmentId, ZoneId = zoneId, Direction = direction
                    }, ct);
                }, refresh,
                // So đổi CHỈ trên các dòng số liệu: ComputedAt tính lại mỗi nhịp.
                changeKey: x => x.Rows);
        }

        /// <summary>
        /// Get VDS traffic statistic snapshot
        /// </summary>
        /// <param name="input">Cửa sổ thời gian và bộ lọc thiết bị/vùng/hướng.</param>
        /// <param name="cancellationToken">Token hủy của request.</param>
        /// <returns>Một dòng mới nhất cho mỗi cặp (thiết bị, làn) trong cửa sổ, kèm cờ quá hạn.</returns>
        [HttpPost("Snapshot")]
        [DisplayName("Traffic statistic snapshot")]
        public async Task<StatisticSnapshotOutput> Snapshot(StatisticSnapshotInput input, CancellationToken cancellationToken = default)
        {
            return await MessBus.InvokeAsync<StatisticSnapshotOutput>(input, cancellationToken);
        }

        /// <summary>
        /// Get VDS traffic statistic pagination list
        /// </summary>
        /// <param name="input">Cửa sổ thời gian và bộ lọc thiết bị/vùng/hướng/làn.</param>
        /// <returns>Trang số liệu thống kê.</returns>
        [DisplayName("Traffic statistic paged list")]
        public async Task<SqlSugarPagedList<TrafficStatisticOutput>> Page(PageTrafficStatisticInput input)
        {
            return await MessBus.InvokeAsync<SqlSugarPagedList<TrafficStatisticOutput>>(input);
        }

        /// <summary>
        /// Get VDS traffic statistic list
        /// </summary>
        /// <param name="input">Cửa sổ thời gian và bộ lọc thiết bị/vùng/hướng/làn.</param>
        /// <returns>Danh sách số liệu thống kê, không phân trang.</returns>
        [DisplayName("Traffic statistic list")]
        public async Task<List<TrafficStatisticOutput>> GetList([FromQuery] TrafficStatisticListInput input)
        {
            return await MessBus.InvokeAsync<List<TrafficStatisticOutput>>(input);
        }

        /// <summary>
        /// Get VDS traffic statistic by date range
        /// </summary>
        /// <param name="input">Khoảng ngày và bộ lọc vùng/thiết bị — dùng cho hai report FastReport.</param>
        /// <returns>Danh sách số liệu thống kê trong khoảng ngày.</returns>
        [HttpPost("ListByDate")]
        [DisplayName("Traffic statistic by date")]
        public async Task<List<TrafficStatisticOutput>> ListByDate(TrafficStatisticByDateInput input)
        {
            return await MessBus.InvokeAsync<List<TrafficStatisticOutput>>(input);
        }

        /// <summary>
        /// Get latest VDS traffic statistic of one equipment
        /// </summary>
        /// <param name="input">Thiết bị và cửa sổ thời gian; cửa sổ là BẮT BUỘC vì QuestDB phân vùng theo ngày.</param>
        /// <returns>Bản ghi mới nhất trong cửa sổ, hoặc null nếu không có.</returns>
        [HttpPost("LatestByEquipment")]
        [DisplayName("Latest traffic statistic by equipment")]
        public async Task<TrafficStatisticOutput?> LatestByEquipment(TrafficStatisticByEquipmentInput input)
        {
            return await MessBus.InvokeAsync<TrafficStatisticOutput?>(input);
        }
    }

}
