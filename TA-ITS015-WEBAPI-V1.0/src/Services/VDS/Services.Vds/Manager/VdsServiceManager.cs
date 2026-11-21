using Services.Shared.Messaging;
using Services.Vds.Catalog;
using Services.Vds.Configuration;
using Services.Vds.Features.VehicleFilter;
using Services.Vds.Features.Journey;
using Services.Vds.Pipeline.Queue;
using Services.Vds.Sources.Backfill;

namespace Services.Vds.Manager;

/// <summary>
/// Author: KienLT
/// Description: Điều phối vòng đời pipeline VDS. Đây là <b>hợp đồng duy nhất giữa host và service</b>:
/// cả <c>Services.Vds.Worker</c> (chạy nền) và <c>Services.Vds.Wpf</c> (giao diện) chỉ nói chuyện qua
/// <see cref="IServiceManager"/>, không biết bên trong có gì.
///
/// <para><b>Init → Start → Stop là TƯỜNG MINH, không dựa vào thứ tự đăng ký DI.</b> Trước đây việc nạp
/// danh mục và bù dữ liệu là hai <c>IHostedService</c>, nên thứ tự chạy phụ thuộc thứ tự đăng ký — một
/// ràng buộc vô hình. Đó cũng là cái đã gây sự cố ngày 26/08/2026: hosted service chạy TUẦN TỰ nên phép
/// kiểm kết nối QuestDB chặn mọi service đăng ký sau nó 5 phút 16 giây. Vòng đời tường minh làm thứ tự
/// trở thành thứ đọc được trong code.</para>
///
/// <para>Chia partial theo nhóm chức năng theo đúng khuôn <see cref="IServiceManager"/> chỉ định:</para>
/// <list type="bullet">
/// <item><term>VdsServiceManager.cs</term><description> vòng đời: Init/Start/Stop + State.</description></item>
/// <item><term>VdsServiceManager.Simulation.cs</term><description> bơm frame và sinh batch giả lập.</description></item>
/// </list>
///
/// <para><b>Thứ tự dừng quan trọng</b>: host phải gọi <see cref="StopAsync"/> TRƯỚC
/// <c>host.StopAsync()</c>. Nguồn đóng trước, bản tin đã nằm trong hàng đợi được Wolverine drain sau —
/// làm ngược lại thì bản tin mới vẫn chảy vào một hàng đợi đang đóng.</para>
///
/// Việc theo CHU KỲ (quét trạng thái vùng, nạp lại danh sách biển, ghép hành trình, nạp lại danh mục) và
/// việc theo HÀNG ĐỢI (tải media, gọi API lập lịch sự cố, phát số đếm) KHÔNG nằm ở đây — chúng độc lập và
/// tự lặp nên vẫn là hosted service.
/// Created date: 26/08/2026
/// </summary>
public sealed partial class VdsServiceManager : IVdsServiceManager, IAsyncDisposable
{
    private readonly Services.Vds.Pipeline.VdsFrameProcessor? _processor;
    private readonly CameraManager _cameras;
    private readonly ZoneManager _zones;
    private readonly OpConfigManager _opConfig;
    private readonly CatalogRefresher _refresher;
    private readonly BackfillCoordinator _backfill;
    private readonly IVdsEventSource _source;
    private readonly VdsFrameDispatcher _dispatcher;
    private readonly TransportManager _transport;
    private readonly IVdsConfigManager _config;
    private readonly ILogger<VdsServiceManager> _logger;

    private CancellationTokenSource? _cts;
    private Task? _producer;

    public VdsServiceManager(
        CameraManager cameras,
        ZoneManager zones,
        OpConfigManager opConfig,
        CatalogRefresher refresher,
        BackfillCoordinator backfill,
        IVdsEventSource source,
        VdsFrameDispatcher dispatcher,
        TransportManager transport,
        IVdsConfigManager config,
        ILogger<VdsServiceManager> logger, Services.Vds.Pipeline.VdsFrameProcessor? processor = null)
    {
        _processor = processor;
        _cameras = cameras;
        _zones = zones;
        _opConfig = opConfig;
        _refresher = refresher;
        _backfill = backfill;
        _source = source;
        _dispatcher = dispatcher;
        _transport = transport;
        _config = config;
        _logger = logger;
    }

    /// <summary>Vòng đời một chiều: Created → Initialized → Running → Stopped.</summary>
    public VdsServiceManagerState State { get; private set; } = VdsServiceManagerState.Created;

    /// <summary>
    /// Nạp mọi thứ pipeline cần TRƯỚC khi bản tin đầu tiên vào, rồi bù dữ liệu lịch sử.
    ///
    /// Mức fail-closed khác nhau theo từng danh mục, vì hậu quả khác nhau:
    /// <list type="bullet">
    /// <item><b>Camera</b>: bắt buộc. Mọi frame chỉ mang <c>cameraIp</c> nên cache rỗng ⇒ bỏ toàn bộ dữ
    /// liệu. Lỗi hoặc rỗng phải làm khởi động thất bại để supervisor retry.</item>
    /// <item><b>Vùng</b>: bắt buộc nạp được, nhưng RỖNG thì vẫn chạy — chỉ mất cột <c>zoneId</c> và các
    /// truy vấn theo vùng, dữ liệu thô vẫn đúng. Lỗi truy vấn thì chặn để không ghi cả khối dữ liệu
    /// thiếu vùng.</item>
    /// <item><b>Đoạn A→B, danh mục hiển thị, cấu hình vận hành, danh sách biển</b>: không chặn. Thiếu
    /// chúng chỉ ảnh hưởng phần hành trình, tên sự cố và cảnh báo biển, không ảnh hưởng dữ liệu thô.</item>
    /// </list>
    /// </summary>
    public async Task InitAsync(CancellationToken cancellationToken = default)
    {
        // NATS: idempotent, tự bỏ qua khi chưa khai Nats:Url. Kết nối thất bại KHÔNG chặn khởi động —
        // VdsPublisher tự thử lại ở điểm publish và bỏ qua êm trong lúc chưa nối được.
        try
        {
            await _transport.ConnectAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[VDS] Không kết nối được NATS; pipeline vẫn chạy, tạm thời không publish.");
        }

        await _cameras.LoadAsync(cancellationToken).ConfigureAwait(false);
        if (_cameras.Count == 0)
        {
            _logger.LogWarning("Danh mục camera VDS rỗng; chờ cấu hình camera được bật và tải lại.");
        }

        // Nạp vùng TRƯỚC khi mở ingestion: nếu để sau, các frame đầu tiên sẽ ghi với zoneId rỗng và
        // phải backfill lại cột đó.
        await _zones.LoadAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "[Catalog] Sẵn sàng trước khi mở ingestion: {Cameras} camera, {ZoneCameras} camera có vùng.",
            _cameras.Count, _zones.Count);

        // Ba danh mục dưới đây tự xử lý lỗi bên trong và giữ snapshot cũ/rỗng.
        // Các mảng KHÔNG chặn khởi động (đoạn A→B, danh mục hiển thị, mapping, cấu hình vận hành, danh sách
        // biển) nạp qua CÙNG hàm refresh với job định kỳ và đường NATS — xem CatalogRefresher. Nạp trước
        // bản tin đầu tiên để lượt xe đầu đã đối chiếu được, không phải chờ hết một chu kỳ job.
        await _refresher.RefreshSoftPartsAsync(cancellationToken).ConfigureAwait(false);

        // Đẩy chu kỳ thống kê xuống thiết bị PHẢI đứng sau LoadAsync — trước đó chu kỳ còn là giá trị mặc
        // định trong appsettings, đẩy sớm là ghi đè giá trị người vận hành đặt trên web bằng giá trị cũ.
        // Mặc định không gọi (Service:Statistic:PushPeriodToDevice = false).
        await _opConfig.PushStatsConfigAsync(cancellationToken).ConfigureAwait(false);

        if (_processor != null) await _processor.RecoverPendingAsync(cancellationToken).ConfigureAwait(false);
        await RunStartupBackfillAsync(cancellationToken).ConfigureAwait(false);

        State = VdsServiceManagerState.Initialized;
    }

    /// <summary>Mở nguồn bản tin. Idempotent khi đã Running.</summary>
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (State == VdsServiceManagerState.Running) return Task.CompletedTask;

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // Task.Run là BẮT BUỘC, không phải để "chạy song song": nó cắt SynchronizationContext của luồng
        // gọi. WPF gọi StartAsync trên luồng UI; nếu chạy trực tiếp thì mọi await trong nguồn sẽ resume
        // trên Dispatcher, và khi đóng cửa sổ Dispatcher tắt trước ⇒ continuation không bao giờ chạy ⇒
        // task producer không hoàn thành ⇒ StopAsync treo tới hết timeout của host.
        _producer = Task.Run(() => Task.WhenAll(RunProducerAsync(_cts.Token), RecoverLoopAsync(_cts.Token)), CancellationToken.None);
        State = VdsServiceManagerState.Running;

        var ingestion = _config.Current.Ingestion;
        _logger.LogInformation(
            "[VDS] Start. Nguồn SSE: {Sse}; queue {Queue} (trần {Cap} bản tin).",
            ingestion.SseEnabled ? "bật" : "tắt", VdsQueues.Data, ingestion.EffectiveQueueCapacity);

        return Task.CompletedTask;
    }

    private async Task RecoverLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(30), ct).ConfigureAwait(false);
                if (_processor != null) await _processor.RecoverPendingAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
            catch (Exception ex) { _logger.LogError(ex, "[Recovery] Pending incident work remains on disk for retry."); }
        }
    }

    /// <summary>Đóng nguồn rồi chờ producer thoát. Idempotent khi đã Stopped.</summary>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (State != VdsServiceManagerState.Running)
        {
            State = VdsServiceManagerState.Stopped;   // idempotent, kể cả khi chưa từng Start
            return;
        }

        State = VdsServiceManagerState.Stopped;
        if (_cts is not null) await _cts.CancelAsync().ConfigureAwait(false);

        if (_producer is not null)
        {
            try
            {
                await _producer.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Host hết hạn chờ; nguồn sẽ tự thoát theo token.
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[VDS] Nguồn bản tin dừng kèm lỗi.");
            }
        }

        // Bản tin đã nằm trong hàng đợi được Wolverine drain khi host.StopAsync() chạy — đó là lý do host
        // phải dừng manager TRƯỚC rồi mới dừng host.
        _logger.LogInformation("[VDS] Stopped.");
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
        _cts?.Dispose();
    }

    /// <summary>
    /// Bù dữ liệu lịch sử lúc khởi động.
    ///
    /// Khi SSE đang bật thì KHÔNG bù ở đây: nguồn SSE tự bù ngay sau khi mở kết nối thành công, để không
    /// có khoảng hở giữa "bù xong" và "bắt đầu nhận live".
    /// </summary>
    private async Task RunStartupBackfillAsync(CancellationToken ct)
    {
        var runtime = _config.Current;
        var backfill = runtime.Backfill;
        if (!backfill.Enabled || !backfill.BackfillOnStartup) return;

        if (runtime.Ingestion.SseEnabled)
        {
            _logger.LogInformation(
                "[Backfill] Startup được trì hoãn tới khi SSE kết nối thành công.");
            return;
        }

        try
        {
            await _backfill.RunAsync(BackfillTrigger.Startup, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (!backfill.RequireSuccessBeforeLive)
        {
            _logger.LogError(ex,
                "[Backfill] Startup thất bại nhưng RequireSuccessBeforeLive=false; vẫn tiếp tục.");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex,
                "[Backfill] Startup thất bại; dừng khởi động để supervisor retry, tránh bỏ khoảng dữ liệu.");
            throw;
        }
    }

    private async Task RunProducerAsync(CancellationToken ct)
    {
        try
        {
            await _source.RunAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[VDS] Nguồn bản tin dừng bất thường.");
        }

        _logger.LogInformation("[VDS] Nguồn bản tin đã dừng.");
    }
}
