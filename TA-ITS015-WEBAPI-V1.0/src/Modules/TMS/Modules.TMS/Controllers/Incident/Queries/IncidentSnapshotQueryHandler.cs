using Modules.TMS.Infrastructure.Services.Incident.Rules;
using Modules.TMS.Core.Dto.Incident;
using Modules.TMS.Infrastructure.Services.Incident;

namespace Modules.TMS.Controllers.Incident.Queries
{
    /// <summary>
    /// Author: KienLT
    /// Description: Query handler cho hai luồng SSE của sự cố (danh sách và icon bản đồ).
    ///
    /// <para>Hai bước:</para>
    /// <list type="bullet">
    /// <item><b>Mở stream</b> (<see cref="IncidentListStreamOpenInput"/>, <see cref="IncidentIconStreamOpenInput"/>):
    /// kiểm bộ lọc và tính khoá đệm theo <c>App.User</c>. Controller gọi bước này trong action, trước khi trả
    /// <c>SseResult</c>, để lỗi bộ lọc vẫn là response lỗi thường.</item>
    /// <item><b>Mỗi vòng poll</b> (<see cref="IncidentListSnapshotInput"/>, <see cref="IncidentIconSnapshotInput"/>):
    /// lấy snapshot qua <see cref="TmsIncidentStreamService"/> với khoá đã tính sẵn.</item>
    /// </list>
    /// Created date: 13/10/2026
    /// </summary>
    public class IncidentSnapshotQueryHandler : IWolverineHandler
    {
        private TmsIncidentStreamService _stream;

        public IncidentSnapshotQueryHandler(TmsIncidentStreamService stream)
        {
            this._stream = stream;
        }

        /// <summary>Kiểm bộ lọc danh sách (ném lỗi như trước) và tính khoá đệm cho người gọi hiện tại.</summary>
        public Task<IncidentStreamOpenOutput> HandleAsync(IncidentListStreamOpenInput command)
        {
            var input = command.Query;
            TmsIncidentMonitorRules.Query(input);
            var queryJson = System.Text.Json.JsonSerializer.Serialize(input);
            return Task.FromResult(new IncidentStreamOpenOutput
            {
                Key = TmsIncidentStreamService.ListKey(App.User, queryJson),
                QueryJson = queryJson
            });
        }

        /// <summary>Tính khoá đệm icon cho người gọi hiện tại.</summary>
        public Task<IncidentStreamOpenOutput> HandleAsync(IncidentIconStreamOpenInput command)
        {
            return Task.FromResult(new IncidentStreamOpenOutput { Key = TmsIncidentStreamService.IconKey(App.User) });
        }

        /// <summary>Một snapshot trang sự cố (có đệm ngắn hạn).</summary>
        public async Task<IncidentSnapshotPayloadOutput> HandleAsync(IncidentListSnapshotInput command, CancellationToken cancellationToken)
        {
            var payload = await this._stream.ListSnapshotAsync(command.Key, command.QueryJson, cancellationToken);
            return new IncidentSnapshotPayloadOutput { Payload = payload };
        }

        /// <summary>Một snapshot icon sự cố trên bản đồ (có đệm ngắn hạn).</summary>
        public async Task<IncidentSnapshotPayloadOutput> HandleAsync(IncidentIconSnapshotInput command, CancellationToken cancellationToken)
        {
            var payload = await this._stream.IconSnapshotAsync(command.Key, cancellationToken);
            return new IncidentSnapshotPayloadOutput { Payload = payload };
        }
    }
}
