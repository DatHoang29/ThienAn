using Microsoft.AspNetCore.Authorization;
using Shared.Utility.Sse;
using Shared.Infrastructure.Sse;
using Modules.TMS.Controllers.ZoneStatus.Dto;

namespace Modules.TMS.Controllers.ZoneStatus;

/// <summary>
///  Zone Status Controller
/// </summary>
[ApiDescriptionSettings(GroupName, Order = 470)]
[ApiController]
public class TmsZoneStatusController : BaseController
{


    /// <summary>
    ///  Get zone status list
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("Get list")]
    public async Task<List<ZoneStatusOutput>> GetList([FromQuery] ZoneStatusInput input)
    {
        return await MessBus.InvokeAsync<List<ZoneStatusOutput>>(input);
    }


    /// <summary>
    /// Stream VDS zone status over Server-Sent Events
    /// </summary>
    /// <param name="zoneId">Lọc theo một vùng; bỏ trống là lấy mọi vùng.</param>
    /// <param name="refresh">Chu kỳ gửi lại, tính bằng giây.</param>
    /// <param name="staleAfterSeconds">Ngưỡng quá hạn; bỏ trống thì lấy theo cấu hình.</param>
    /// <returns>Luồng SSE, mỗi nhịp là một snapshot đầy đủ.</returns>
    [HttpGet("Stream")]
    [Sse("zone", HeartbeatSeconds = 0)]
    [Authorize(Policy = SnapshotSseAuthorization.Policy)]
    [DisplayName("Zone status stream (SSE)")]
    public SseResult Stream(string? zoneId, [Range(1, 60)] int refresh = 5, [Range(1, 86400)] int? staleAfterSeconds = null)
    {
        return SnapshotSse.Poll(
            $"zone|z={zoneId}|s={staleAfterSeconds}",
            async (sp, ct) =>
            {
                var bus = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions
                    .GetRequiredService<Wolverine.IMessageBus>(sp);
                return await bus.InvokeAsync<ZoneStatusSnapshotOutput>(
                    new ZoneStatusSnapshotInput { ZoneId = zoneId, StaleAfterSeconds = staleAfterSeconds }, ct);
            }, refresh,
            // So đổi CHỈ trên trạng thái vùng và ngưỡng quá hạn: ComputedAt đổi mỗi nhịp.
            changeKey: x => new { x.StaleAfterSeconds, x.Zones });
    }

    /// <summary>
    /// Get VDS zone status snapshot
    /// </summary>
    /// <param name="zoneId">Lọc theo một vùng; bỏ trống là lấy mọi vùng.</param>
    /// <param name="staleAfterSeconds">Ngưỡng quá hạn; bỏ trống thì lấy theo cấu hình.</param>
    /// <param name="cancellationToken">Token hủy của request.</param>
    /// <returns>Trạng thái các vùng tại thời điểm gọi, kèm cờ quá hạn.</returns>
    [HttpGet("Snapshot")]
    [DisplayName("Zone status snapshot")]
    public async Task<ZoneStatusSnapshotOutput> Snapshot(string? zoneId, [Range(1, 86400)] int? staleAfterSeconds = null,
        CancellationToken cancellationToken = default)
    {
        return await MessBus.InvokeAsync<ZoneStatusSnapshotOutput>(
            new ZoneStatusSnapshotInput { ZoneId = zoneId, StaleAfterSeconds = staleAfterSeconds }, cancellationToken);
    }
}
