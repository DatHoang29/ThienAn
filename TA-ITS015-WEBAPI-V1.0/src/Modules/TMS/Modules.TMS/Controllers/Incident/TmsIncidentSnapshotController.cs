using Modules.TMS.Infrastructure.Services.Incident.Snapshot;
using Microsoft.AspNetCore.Authorization;
using Modules.TMS.Core.Dto.Incident;
using Modules.TMS.Infrastructure.Services.Incident;
using Shared.Infrastructure.Sse;

namespace Modules.TMS.Controllers.Incident
{
    /// <summary>
    /// Author: KienLT
    /// Description: Hai luồng SSE của sự cố: trang danh sách và icon trên bản đồ.
    ///
    /// <para><b>Từ 22/09/2026 controller mỏng theo mẫu TMS.</b> Trước đó nó giữ <c>MemoryCache</c>, mảng
    /// <c>SemaphoreSlim</c>, tự băm khoá và tự dựng snapshot. Phần đệm nay ở
    /// <see cref="IncidentSnapshotCache"/> (singleton), phần dựng snapshot ở
    /// <see cref="TmsIncidentStreamService"/> (scoped).</para>
    ///
    /// <para>Khoá đệm tính MỘT LẦN khi mở stream, không tính lại mỗi vòng poll.</para>
    ///
    /// <para><b>Từ 13/10/2026 dispatch qua message bus</b> (<c>Queries/IncidentSnapshotQueryHandler</c>): bước mở
    /// stream (kiểm bộ lọc + tính khoá theo <c>App.User</c>) được await trong action trước khi trả
    /// <c>SseResult</c> — lỗi bộ lọc vẫn là response lỗi thường như trước; mỗi vòng poll gọi query snapshot.
    /// Vòng poll của TMS chạy TRONG request (không qua hub dùng chung của VDS) nên dùng message bus của request.
    /// KHÔNG chuyển sang <c>SnapshotSse</c>/hub: hub gom nhóm theo tenant + kiểu tài khoản, còn snapshot sự cố
    /// phụ thuộc quyền của TỪNG người dùng.</para>
    /// Created date: 22/09/2026
    /// </summary>
    [Authorize]
    [ApiController]
    public class TmsIncidentSnapshotController : BaseController
    {
        /// <summary>Chu kỳ gửi lại, tính bằng giây.</summary>
        private const int RefreshSeconds = 3;

        /// <summary>
        /// Stream incident list over Server-Sent Events
        /// </summary>
        /// <param name="input">Bộ lọc và tham số phân trang của danh sách sự cố.</param>
        /// <returns>Luồng SSE, chỉ gửi khi snapshot đổi, kèm heartbeat.</returns>
        [HttpGet("Stream")]
        [Sse("incident", HeartbeatSeconds = 0)]
        [Authorize(Policy = Shared.Utility.Sse.SnapshotSseAuthorization.Policy)]
        [DisplayName("Incident list stream (SSE)")]
        public async Task<SseResult> Stream([FromQuery] TmsIncidentMonitorInput input, CancellationToken cancellationToken)
        {
            // Lỗi bộ lọc phải ném ra TRƯỚC khi stream bắt đầu để client nhận response lỗi thường.
            var bus = MessBus;
            var open = await bus.InvokeAsync<IncidentStreamOpenOutput>(
                new IncidentListStreamOpenInput { Query = input }, cancellationToken);
            var snapshot = new IncidentListSnapshotInput { Key = open.Key, QueryJson = open.QueryJson! };
            return IncidentSnapshotSse.Poll<object>(
                async ct => (await bus.InvokeAsync<IncidentSnapshotPayloadOutput>(snapshot, ct)).Payload,
                RefreshSeconds, changesOnly: true);
        }

        /// <summary>
        /// Stream incident map icons over Server-Sent Events
        /// </summary>
        /// <returns>Luồng SSE danh sách icon, chỉ gửi khi đổi, kèm heartbeat.</returns>
        [HttpGet("IconStream")]
        [Sse("incident", HeartbeatSeconds = 0)]
        [Authorize(Policy = Shared.Utility.Sse.SnapshotSseAuthorization.Policy)]
        [DisplayName("Incident icon stream (SSE)")]
        public async Task<SseResult> IconStream(CancellationToken cancellationToken)
        {
            // Khoá icon tính từ App.User của request, MỘT LẦN khi mở stream.
            var bus = MessBus;
            var open = await bus.InvokeAsync<IncidentStreamOpenOutput>(
                new IncidentIconStreamOpenInput(), cancellationToken);
            var snapshot = new IncidentIconSnapshotInput { Key = open.Key };
            return IncidentSnapshotSse.Poll<object>(
                async ct => (await bus.InvokeAsync<IncidentSnapshotPayloadOutput>(snapshot, ct)).Payload,
                RefreshSeconds, changesOnly: true);
        }
    }
}
