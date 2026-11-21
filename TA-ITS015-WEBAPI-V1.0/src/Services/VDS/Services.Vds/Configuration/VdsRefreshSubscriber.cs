using System.Text.Json;
using Modules.VDS.Core.Dto;
using Microsoft.Extensions.Hosting;
using Services.Shared.Messaging;
using Services.Vds.Catalog;
using Services.Vds.Shared.Messaging;

namespace Services.Vds.Configuration;

/// <summary>
/// Author: KienLT
/// Description: Nghe subject DÙNG CHUNG <see cref="VdsSubjects.Refresh"/> (<c>ta.its.refresh</c>). Bên
/// ngoài chỉ bắn một MÃ (<see cref="RefreshMessage"/>); service nạp lại mảng dữ liệu tương ứng TỪ CSDL, các
/// bộ nạp tự ghi file cache và dùng ngay — KHÔNG cần restart.
///
/// <para>Việc nạp KHÔNG nằm ở đây mà ở <see cref="CatalogRefresher"/> — dùng chung với nạp lúc khởi động và
/// nạp theo chu kỳ. Lớp này chỉ làm hai việc: đọc mã và <b>quy mã sang phạm vi</b>
/// (<see cref="CatalogParts"/>).</para>
///
/// <para>Mã <see cref="RefreshCodes.VdsConfig"/> nạp camera, mapping và tham số VdsConfig;
/// mã <see cref="RefreshCodes.SysConfigData"/> nạp danh mục hiển thị SysConfig. Đây KHÔNG
/// còn là đường đẩy GIÁ TRỊ tham số xuống: nguồn sự thật là CSDL, service chỉ được yêu cầu đọc lại.</para>
///
/// Subject dùng chung nhiều module nên mã lạ được bỏ qua. <b>Không kết nối được NATS thì service vẫn
/// chạy</b>, chỉ mất khả năng refresh từ xa.
/// Created date: 04/09/2026
/// </summary>
public sealed class VdsRefreshSubscriber : BackgroundService
{
    private readonly IVdsConfigManager _config;
    private readonly TransportManager _transport;
    private readonly CatalogRefresher _refresher;
    private readonly ILogger<VdsRefreshSubscriber> _logger;

    public VdsRefreshSubscriber(
        IVdsConfigManager config,
        TransportManager transport,
        CatalogRefresher refresher,
        ILogger<VdsRefreshSubscriber> logger)
    {
        _config = config;
        _transport = transport;
        _refresher = refresher;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) => SubscriptionRecovery.RunAsync(
        async ct => { await _transport.ConnectAsync(ct); return _transport.IsConnected; },
        async ct => {
            await _transport.SubscribeAsync(VdsSubjects.Refresh, HandleAsync, ct);
            _logger.LogInformation("[Refresh] Đang nghe {Subject}.", VdsSubjects.Refresh);
        },
        ct => Task.Delay(TimeSpan.FromSeconds(5), ct),
        ex => _logger.LogError(ex, "[Refresh] Kết nối/subscribe lỗi; thử lại sau 5 giây."), stoppingToken);
    private async Task HandleAsync(string payload)
    {
        try
        {
            if (!RefreshMessage.TryParse(payload, out var code, out var error))
            {
                _logger.LogWarning(
                    "[Refresh] Bỏ bản tin refresh không hợp lệ ({Error}). Payload mong đợi: " +
                    "{Example}", error, "{\"code\":<số>}.");
                return;
            }

            string? requestId = null;
            using (var document = JsonDocument.Parse(payload))
            {
                if (document.RootElement.ValueKind == JsonValueKind.Object
                    && document.RootElement.TryGetProperty("requestId", out var request)
                    && Guid.TryParse(request.GetString(), out var parsed))
                {
                    if (!document.RootElement.TryGetProperty("tenantId", out var tenant)
                        || tenant.GetString() != _config.Current.TenantId) return;
                    requestId = parsed.ToString("N");
                }
            }
            var parts = PartsOf(code);
            if (parts == CatalogParts.None)
            {
                // Subject dùng chung: mã của module khác, VDS bỏ qua.
                _logger.LogDebug("[Refresh] Bỏ qua mã {Code} (VDS không xử lý).", code);
                return;
            }

            _logger.LogInformation("[Refresh] Nhận mã {Code} ⇒ nạp lại {Parts}.", code, parts);
            using var audit = new CatalogRefreshAudit();
            await _refresher.RefreshAsync(parts, CancellationToken.None).ConfigureAwait(false);
            if (requestId is not null)
                await _transport.PublishAsync(VdsRefreshAcknowledgement.Subject, new VdsRefreshAcknowledgement
                {
                    RequestId = requestId, TenantId = _config.Current.TenantId,
                    Instance = $"{System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name}:{Environment.MachineName}:{Environment.ProcessId}",
                    Applied = audit.Issues.Length == 0, Issues = audit.Issues, CompletedUtc = DateTime.UtcNow
                }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Refresh] Lỗi khi xử lý bản tin refresh; bỏ qua bản tin này.");
        }
    }

    /// <summary>
    /// Quy MÃ refresh sang PHẠM VI nạp. Đây là toàn bộ "switch case" của bảng mã — thêm mã mới thì thêm
    /// hằng ở <see cref="RefreshCodes"/> và một dòng ở đây.
    ///
    /// Mã lạ (module khác trên subject dùng chung) ⇒ <see cref="CatalogParts.None"/>.
    /// </summary>
    internal static CatalogParts PartsOf(int code) => code switch
    {
        RefreshCodes.All => CatalogParts.All,

        // Danh mục chung: vị trí, hướng và tên cỡ xe; mapping đã chuyển sang VdsConfig.
        RefreshCodes.SysConfigData => CatalogParts.ConfigData,

        // Mặc định thời gian hành trình nằm trong VdsConfig; nạp lại đoạn để áp dụng biên dự phòng.
        RefreshCodes.VdsConfig => CatalogParts.Cameras | CatalogParts.Zones | CatalogParts.Segments
            | CatalogParts.Mappings | CatalogParts.OpConfig,
        RefreshCodes.Equipment => CatalogParts.Cameras,
        RefreshCodes.Zone => CatalogParts.Zones,
        RefreshCodes.Segment => CatalogParts.Segments,
        RefreshCodes.VehicleFilter => CatalogParts.VehicleFilters,
        _ => CatalogParts.None,
    };
}
