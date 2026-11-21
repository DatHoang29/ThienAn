using Microsoft.AspNetCore.Authorization;
using Shared.Utility.Sse;
using Modules.VDS.Controllers.SSEDemo.Dto;
using Shared.Infrastructure.Sse;

namespace Modules.VDS.Controllers.SSEDemo;

/// <summary>
/// Author: KienLT
/// Description: Stream snapshot thiết bị qua Server-Sent Events.
///
/// <para>Tên class giữ nguyên <c>SSEDemoController</c> (không đổi thành <c>VdsSseDemoController</c> theo quy
/// ước tiền tố) vì route <c>/api/vds/ssedemo</c> đang được FE dùng thật tại
/// <c>views/tms/equipmentSSE/index.vue</c> qua <c>api-services/vdsSSE/apis/ssedemo-api.ts</c>.</para>
/// Created date: 10/09/2026
/// </summary>
[ApiController]
[ApiDescriptionSettings(GroupName, Order = 596)]
public class SSEDemoController : BaseController
{
    /// <summary>
    /// Stream equipment snapshot over Server-Sent Events
    /// </summary>
    /// <param name="input">Tên hiển thị và chu kỳ gửi lại.</param>
    /// <returns>Luồng SSE: gửi snapshot ngay lập tức rồi lặp theo chu kỳ client yêu cầu.</returns>
    [HttpGet]
    [Sse("snapshot", HeartbeatSeconds = 0)]
    [Authorize(Policy = SnapshotSseAuthorization.Policy)]
    [DisplayName("VDS - Stream snapshot thiết bị (SSE)")]
    public SseResult Get([FromQuery] SseDemoInput input)
    {
        return Shared.Utility.Sse.SnapshotSse.Poll(
            $"ssedemo-snapshot|{System.Text.Json.JsonSerializer.Serialize(input)}",
            async (sp, ct) =>
            {
                var bus = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions
                    .GetRequiredService<Wolverine.IMessageBus>(sp);
                return await bus.InvokeAsync<SseDemoSnapshotOutput>(input, ct);
            },
            input.Refresh,
            // So đổi CHỈ trên số liệu: Timestamp của vỏ bọc đổi mỗi nhịp.
            changeKey: x => new { x.Total, x.Data });
    }

    /// <summary>
    /// Stream equipment count with a handler-controlled frame
    /// </summary>
    /// <param name="input">Tên hiển thị và chu kỳ gửi lại.</param>
    /// <returns>Luồng SSE: wrapper, tên sự kiện và điều kiện dừng do handler quyết định.</returns>
    [HttpGet]
    [Sse("tick", HeartbeatSeconds = 0)]
    [Authorize(Policy = SnapshotSseAuthorization.Policy)]
    [DisplayName("VDS - Stream thiết bị với wrapper tùy chỉnh (SSE)")]
    public SseResult Custom([FromQuery] SseDemoTickInput input)
    {
        return Shared.Utility.Sse.SnapshotSse.Poll(
            $"ssedemo-tick|{System.Text.Json.JsonSerializer.Serialize(input)}",
            async (sp, ct) =>
            {
                var bus = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions
                    .GetRequiredService<Wolverine.IMessageBus>(sp);
                return await bus.InvokeAsync<SseFrame>(input, ct);
            },
            input.Refresh);
    }
}
