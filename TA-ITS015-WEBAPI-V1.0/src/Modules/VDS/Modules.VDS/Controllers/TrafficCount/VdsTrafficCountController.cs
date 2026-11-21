using Microsoft.AspNetCore.Authorization;
using Shared.Utility.Sse;
using Modules.VDS.Controllers.TrafficCount.Dto;
using Shared.Infrastructure.Sse;

namespace Modules.VDS.Controllers.TrafficCount
{
    /// <summary>
    /// Author: KienLT
    /// Description: API số đếm xe VDS (tính từ QuestDB bằng SAMPLE BY). Thay khái niệm bảng
    /// <c>TmsTrafficCount</c> — không dựng lại bảng đếm, tính lại từ bảng thô mỗi lần.
    ///
    /// Route: <c>api/vds/vdstrafficcount/*</c>, GroupName VDS.
    /// Created date: 03/09/2026
    /// </summary>
    [ApiController]
    [ApiDescriptionSettings(GroupName, Order = 585)]
    public class VdsTrafficCountController : BaseController
    {
        /// <summary>
        /// Stream VDS traffic count over Server-Sent Events
        /// </summary>
        /// <param name="refresh">Chu kỳ gửi lại, tính bằng giây.</param>
        /// <param name="windowMinutes">Độ dài cửa sổ trượt, tính bằng phút.</param>
        /// <param name="equipmentId">Lọc theo thiết bị.</param>
        /// <param name="zoneId">Lọc theo vùng.</param>
        /// <param name="direction">Lọc theo hướng.</param>
        /// <returns>Luồng SSE, mỗi nhịp là một <see cref="TrafficCountSseOutput"/> đầy đủ.</returns>
        [HttpGet("Stream")]
        [Sse("count", HeartbeatSeconds = 0)]
        [Authorize(Policy = SnapshotSseAuthorization.Policy)]
        [DisplayName("Traffic count stream (SSE)")]
        public SseResult Stream(
            [Range(1, 60)] int refresh = 5, [Range(1, 1440)] int windowMinutes = 60,
            string? equipmentId = null, string? zoneId = null, string? direction = null)
        {
            // Cửa sổ tính lại mỗi nhịp: stream phải trượt theo hiện tại, không đóng băng mốc lúc mở kết nối.
            // Closure chỉ bắt các giá trị THAM SỐ, và lấy message bus từ scope mà hub cấp — không được bắt
            // MessBus của request vì vòng dùng chung sống lâu hơn request này.
            return Shared.Utility.Sse.SnapshotSse.Poll(
                $"count|w={windowMinutes}|e={equipmentId}|z={zoneId}|d={direction}",
                async (sp, ct) =>
                {
                    var to = DateTime.UtcNow;
                    var from = to.AddMinutes(-windowMinutes);
                    var bus = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions
                        .GetRequiredService<Wolverine.IMessageBus>(sp);
                    var data = await bus.InvokeAsync<TrafficCountSummaryOutput>(new TrafficCountSummaryInput
                    { FromDate = from, ToDate = to, EquipmentId = equipmentId, ZoneId = zoneId, Direction = direction }, ct);
                    return new TrafficCountSseOutput { ComputedAt = DateTime.UtcNow, From = from, To = to, Data = data };
                }, refresh,
                // So đổi CHỈ trên khối số liệu: ComputedAt và cửa sổ From/To tính lại mỗi nhịp nên nếu so cả
                // đối tượng thì không bao giờ "không đổi", và heartbeat không bao giờ phát.
                changeKey: x => x.Data);
        }

        /// <summary>
        /// Get VDS traffic count slices
        /// </summary>
        /// <param name="input">Cửa sổ thời gian, phạm vi và chu kỳ ô đếm.</param>
        /// <returns>Các ô đếm trong cửa sổ.</returns>
        [HttpPost("Slices")]
        [DisplayName("Traffic count slices")]
        public async Task<List<TrafficCountSliceOutput>> Slices(TrafficCountSliceInput input)
        {
            return await MessBus.InvokeAsync<List<TrafficCountSliceOutput>>(input);
        }

        /// <summary>
        /// Get VDS traffic count summary
        /// </summary>
        /// <param name="input">Cửa sổ thời gian và phạm vi.</param>
        /// <returns>Một khối gộp toàn cửa sổ.</returns>
        [HttpPost("Summary")]
        [DisplayName("Traffic count summary")]
        public async Task<TrafficCountSummaryOutput> Summary(TrafficCountSummaryInput input)
        {
            return await MessBus.InvokeAsync<TrafficCountSummaryOutput>(input);
        }

        /// <summary>
        /// Get VDS traffic count hourly series
        /// </summary>
        /// <param name="input">Ngày cần xem và phạm vi.</param>
        /// <returns>Chuỗi 24 giờ của ngày đó.</returns>
        [HttpPost("Hourly")]
        [DisplayName("Traffic count hourly series")]
        public async Task<List<TrafficCountHourlyOutput>> Hourly(TrafficCountHourlyInput input)
        {
            return await MessBus.InvokeAsync<List<TrafficCountHourlyOutput>>(input);
        }
    }
}
