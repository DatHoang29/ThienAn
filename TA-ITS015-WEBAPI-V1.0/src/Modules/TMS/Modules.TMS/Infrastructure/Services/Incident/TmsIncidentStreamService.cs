using Modules.TMS.Infrastructure.Services.Incident.Snapshot;
using Modules.TMS.Core.Dto.Incident;
using Modules.TMS.Infrastructure.Services.TrafficInfo;
using Shared.Utility.Sse;

namespace Modules.TMS.Infrastructure.Services.Incident
{
    /// <summary>
    /// Author: KienLT
    /// Description: Dựng snapshot cho hai luồng SSE của sự cố, có đệm ngắn hạn.
    ///
    /// <para><b>Vì sao có lớp này.</b> Trước 22/09/2026 toàn bộ phần dựng snapshot — băm khoá, tra đệm, gọi
    /// message bus, deserialize lại input mỗi vòng poll — nằm trong <c>TmsIncidentSnapshotController</c>.
    /// Controller theo mẫu TMS chỉ được dispatch.</para>
    ///
    /// <para><c>IScoped</c> chứ không singleton: lớp này giữ <c>IMessageBus</c> và
    /// <see cref="TrafficInfoService"/> — đều theo scope. Từ 13/10/2026 lớp này được Wolverine dựng cho
    /// <c>IncidentSnapshotQueryHandler</c> ở mỗi lượt gọi (controller chỉ dispatch). Phần dùng chung giữa các request là
    /// <see cref="IncidentSnapshotCache"/>, và nó mới là singleton.</para>
    ///
    /// <para><b>Vòng poll SSE của TMS nằm TRONG request</b> (<c>SseResult</c> chạy như một action result), nên
    /// giữ service theo scope là an toàn — khác với hub fan-out của VDS, nơi vòng lấy dữ liệu sống lâu hơn
    /// request và buộc phải tự tạo scope.</para>
    /// Created date: 22/09/2026
    /// </summary>
    public class TmsIncidentStreamService : IScoped
    {
        /// <summary>Phân biệt loại snapshot trong khoá đệm.</summary>
        private const string ListDiscriminator = "monitor-list";

        /// <summary>Phân biệt loại snapshot icon trong khoá đệm.</summary>
        private const string IconDiscriminator = "icons";

        private IMessageBus _messageBus;
        private TrafficInfoService _trafficInfo;
        private IncidentSnapshotCache _cache;

        public TmsIncidentStreamService(IMessageBus messageBus, TrafficInfoService trafficInfo, IncidentSnapshotCache cache)
        {
            this._messageBus = messageBus;
            this._trafficInfo = trafficInfo;
            this._cache = cache;
        }

        /// <summary>
        /// Khoá đệm của luồng trang sự cố cho người gọi hiện tại.
        ///
        /// <para>Tách riêng để bước mở stream (<c>IncidentSnapshotQueryHandler</c>) tính MỘT LẦN, không tính lại mỗi vòng poll.</para>
        /// </summary>
        public static string ListKey(System.Security.Claims.ClaimsPrincipal? user, string queryJson)
            => IncidentSnapshotCache.KeyFor(user, ListDiscriminator, queryJson);

        /// <summary>Khoá đệm của luồng icon cho người gọi hiện tại.</summary>
        public static string IconKey(System.Security.Claims.ClaimsPrincipal? user)
            => IncidentSnapshotCache.KeyFor(user, IconDiscriminator);

        /// <summary>
        /// Snapshot một trang sự cố.
        /// </summary>
        /// <param name="key">Khoá đệm đã tính từ <see cref="ListKey"/>.</param>
        /// <param name="queryJson">
        /// Bộ lọc dưới dạng JSON. Phải dựng LẠI input từ chuỗi này mỗi vòng poll vì cả
        /// <see cref="TmsIncidentPageRules.Normalize"/> và <c>IncidentQueryHandler</c> đều SỬA input tại chỗ
        /// (<c>Field</c>, <c>Order</c>, <c>FromDate</c>, <c>ToDate</c>); dùng lại đối tượng đã bị sửa thì vòng
        /// sau chạy trên tham số khác vòng trước.
        /// </param>
        public async Task<object> ListSnapshotAsync(string key, string queryJson, CancellationToken cancellationToken)
        {
            using var diagnostic = SseDiagnosticOperation.Begin("incident-snapshot-fetch", cancellationToken);
            diagnostic.Phase = "cache-wait";
            try
            {
                var snapshot = await this._cache.GetOrAddAsync(key, async ct =>
                {
                    var query = System.Text.Json.JsonSerializer.Deserialize<TmsIncidentMonitorInput>(queryJson)!;
                    diagnostic.Phase = "message-bus";
                    var result = await this._messageBus.InvokeAsync<TmsIncidentMonitorOutput>(query, ct);
                    return new { items = result.Items, statistics = result.Statistics, includeEvents = query.IncludeEvents, includeIncidents = query.IncludeIncidents };
                }, cancellationToken);
                diagnostic.Complete();
                return snapshot;
            }
            catch (OperationCanceledException ex) { diagnostic.Cancelled(ex); throw; }
            catch (Exception ex) { diagnostic.Failed(ex); throw; }
        }

        /// <summary>Snapshot danh sách icon sự cố trên bản đồ.</summary>
        public Task<object> IconSnapshotAsync(string key, CancellationToken cancellationToken)
            => this._cache.GetOrAddAsync(key, async _ =>
            {
                var icons = (await this._trafficInfo.GetIncident()).OrderBy(x => x.IncidentId).ToList();
                return new { icons };
            }, cancellationToken);
    }
}
