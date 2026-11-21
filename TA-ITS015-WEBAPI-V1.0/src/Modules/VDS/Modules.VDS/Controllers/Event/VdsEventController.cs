using Microsoft.AspNetCore.Authorization;
using Modules.VDS.Controllers.Event.Dto;
using Modules.VDS.Controllers.TrafficData.Dto;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Sse;
using Shared.Utility.Sse;

namespace Modules.VDS.Controllers.Event;

/// <summary>
/// Author: KienLT
/// Description: API sự kiện VDS: danh sách phân trang và chi tiết một bản tin theo eventId + detectTime.
/// Created date: 21/09/2026
/// </summary>
[ApiDescriptionSettings(GroupName, Order = 589)]
[ApiController]
public class VdsEventController : BaseController
{
    /// <summary>
    /// Get VDS event pagination list
    /// </summary>
    /// <param name="input">Bộ lọc dùng chung với TrafficData; cờ EventsOnly do controller đặt.</param>
    /// <returns>Trang danh sách sự kiện.</returns>
    [DisplayName("Event paged list")]
    public async Task<SqlSugarPagedList<PageTrafficDataOutput>> Page(PageTrafficDataInput input)
    {
        input.EventsOnly = true;
        return await MessBus.InvokeAsync<SqlSugarPagedList<PageTrafficDataOutput>>(input);
    }

    /// <summary>
    /// Get VDS event detail
    /// </summary>
    /// <param name="eventId">Mã bản tin của vendor.</param>
    /// <param name="detectTime">Mốc phát hiện (UTC) — cùng eventId tạo thành khóa kép trên QuestDB.</param>
    /// <returns>Bản tin, các sự cố liên quan và khóa tra media.</returns>
    /// <remarks>
    /// <c>[Required]</c> ở đây CHỈ để OpenAPI đánh dấu hai tham số là bắt buộc — nó không chặn được gì vì
    /// value type không nullable luôn có giá trị. Miền hợp lệ do <c>VdsEventKeyRules</c> định nghĩa và
    /// <c>VdsRecordReaderService</c> kiểm.
    /// </remarks>
    [HttpGet("Detail")]
    [DisplayName("Event detail")]
    public async Task<EventDetailOutput> Detail([Required] long eventId, [Required] DateTime detectTime)
    {
        return await MessBus.InvokeAsync<EventDetailOutput>(new EventDetailInput { EventId = eventId, DetectTime = detectTime });
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Phát snapshot sự kiện VDS đang hiển thị qua SSE.
    /// Created date: 19/10/2026
    /// </summary>
    /// <param name="refresh">Chu kỳ gửi lại, tính bằng giây.</param>
    /// <param name="zoneId">Lọc theo vùng.</param>
    /// <param name="equipmentId">Lọc theo camera.</param>
    /// <param name="direction">Lọc theo hướng.</param>
    /// <returns>Luồng SSE, mỗi nhịp là một <see cref="EventSnapshotOutput"/> đầy đủ.</returns>
    /// <remarks>
    /// sự kiện (TmsEventType nhóm event) không còn ghi TmsIncident, màn hình nhận chúng ở đây.
    /// Dùng hub chung của VDS: snapshot không phụ thuộc quyền từng người nên mỗi nhịp chỉ MỘT truy vấn cho mọi
    /// người xem cùng bộ lọc.
    /// </remarks>
    [HttpGet("Stream")]
    [Sse("vdsevent", HeartbeatSeconds = 0)]
    [Authorize(Policy = SnapshotSseAuthorization.Policy)]
    [DisplayName("Displayed event stream (SSE)")]
    public SseResult Stream([Range(1, 60)] int refresh = 3, string? zoneId = null, string? equipmentId = null, string? direction = null)
    {
        // Lấy message bus từ scope mà hub cấp, không bắt MessBus của request: vòng dùng chung sống lâu hơn request.
        return SnapshotSse.Poll(
            $"vdsevent|z={zoneId}|e={equipmentId}|d={direction}",
            async (sp, ct) =>
            {
                var bus = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions
                    .GetRequiredService<Wolverine.IMessageBus>(sp);
                return await bus.InvokeAsync<EventSnapshotOutput>(
                    new EventSnapshotInput { ZoneId = zoneId, EquipmentId = equipmentId, Direction = direction }, ct);
            }, refresh,
            // So đổi CHỈ trên danh sách: ComputedAt tính lại mỗi nhịp.
            changeKey: x => x.Items);
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Lấy snapshot sự kiện VDS còn trong thời gian hiển thị.
    /// Created date: 19/10/2026
    /// </summary>
    /// <param name="zoneId">Lọc theo vùng.</param>
    /// <param name="equipmentId">Lọc theo camera.</param>
    /// <param name="direction">Lọc theo hướng.</param>
    /// <param name="cancellationToken">Token hủy của request.</param>
    /// <returns>Các sự kiện còn trong thời gian hiển thị tại thời điểm gọi.</returns>
    [HttpGet("Snapshot")]
    [DisplayName("Displayed event snapshot")]
    public async Task<EventSnapshotOutput> Snapshot(string? zoneId = null, string? equipmentId = null, string? direction = null,
        CancellationToken cancellationToken = default)
    {
        return await MessBus.InvokeAsync<EventSnapshotOutput>(
            new EventSnapshotInput { ZoneId = zoneId, EquipmentId = equipmentId, Direction = direction }, cancellationToken);
    }
}
