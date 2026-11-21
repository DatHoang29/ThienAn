using System.Net;
using System.Text;
using Services.Vds.Configuration;
using Services.Vds.Catalog;
using Services.Vds.Manager;
using Services.Vds.Sources.Backfill;
using Services.Vds.Pipeline.Queue;
using Services.Vds.Pipeline;

namespace Services.Vds.Sources;

/// <summary>
/// Author: KienLT
/// Description: Nguồn SSE tùy chọn. Mặc định không mở kết nối; khi bật sẽ đưa mọi khối data live vào
/// <c>VdsFrameDispatcher</c> và ĐẶT YÊU CẦU bù dữ liệu cho worker nền — không tự chạy lượt bù.
///
/// Do <c>VdsServiceManager</c> gọi qua <see cref="IVdsEventSource"/>, KHÔNG còn tự chạy như
/// <c>BackgroundService</c>: danh mục camera phải nạp xong trước bản tin đầu tiên, và nguồn phải đóng
/// trước khi host drain hàng đợi Wolverine.
/// Created date: 25/08/2026
/// </summary>
public sealed class SseEventSource : IVdsEventSource
{
    private static readonly TimeSpan ReadyPollInterval = TimeSpan.FromSeconds(2);

    private readonly IVdsConfigManager _config;
    private readonly CameraManager _cameras;
    private readonly BackfillRequestTrigger _backfillRequest;
    private readonly VdsFrameDispatcher _dispatcher;
    private readonly ILogger<SseEventSource> _logger;

    private string? _lastInvalidBaseUrl;
    private DateTime? _connectedUtc;
    private bool _hasConnectedOnce;
    private int _notReadyLogged;
    private int _insecureHttpLogged;
    private int _selfSignedLogged;
    private int _requireSuccessWarned;

    public SseEventSource(
        IVdsConfigManager config,
        CameraManager cameras,
        BackfillRequestTrigger backfillRequest,
        VdsFrameDispatcher dispatcher,
        ILogger<SseEventSource> logger)
    {
        _config = config;
        _cameras = cameras;
        _backfillRequest = backfillRequest;
        _dispatcher = dispatcher;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken stoppingToken)
    {
        var attempt = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            var runtime = _config.Current;
            var options = runtime.Ingestion.Clone();
            if (!options.SseEnabled)
            {
                attempt = 0;
                await DelayAsync(ReadyPollInterval, stoppingToken).ConfigureAwait(false);
                continue;
            }

            if (!runtime.QuestEnabled || _cameras.Count == 0)
            {
                if (Interlocked.Exchange(ref _notReadyLogged, 1) == 0)
                {
                    _logger.LogWarning(
                        "[SSE] Chưa mở nguồn: QuestDB enabled={QuestEnabled}, camera={CameraCount}.",
                        runtime.QuestEnabled, _cameras.Count);
                }

                attempt = 0;
                await DelayAsync(ReadyPollInterval, stoppingToken).ConfigureAwait(false);
                continue;
            }
            Interlocked.Exchange(ref _notReadyLogged, 0);

            if (!TryBuildStreamUrl(options, out var streamUrl))
            {
                var invalidKey = $"{options.ApiBaseUrl}|{options.AllowInsecureHttp}";
                if (!string.Equals(_lastInvalidBaseUrl, invalidKey, StringComparison.Ordinal))
                {
                    _lastInvalidBaseUrl = invalidKey;
                    _logger.LogError(
                        "[SSE] ApiBaseUrl không hợp lệ hoặc dùng HTTP khi AllowInsecureHttp=false; " +
                        "chờ cấu hình được sửa.");
                }

                await DelayAsync(TimeSpan.FromSeconds(5), stoppingToken).ConfigureAwait(false);
                continue;
            }

            _lastInvalidBaseUrl = null;
            _connectedUtc = null;
            try
            {
                var disabledByConfig = await ListenAsync(
                    options, streamUrl!, stoppingToken).ConfigureAwait(false);
                if (disabledByConfig)
                {
                    attempt = 0;
                    continue;
                }

                if (!stoppingToken.IsCancellationRequested)
                    throw new EndOfStreamException("Máy chủ đã đóng luồng SSE.");
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                if (_connectedUtc.HasValue
                    && DateTime.UtcNow - _connectedUtc.Value >= TimeSpan.FromSeconds(30))
                {
                    attempt = 0;
                }

                attempt++;
                var delay = Backoff(options, attempt);
                _logger.LogError(ex,
                    "[SSE] Mất kết nối; thử lại sau {DelayMs}ms.", delay.TotalMilliseconds);
                await DelayAsync(delay, stoppingToken).ConfigureAwait(false);
            }
        }
    }

    /// <returns>true khi người vận hành tắt SSE trong lúc kết nối.</returns>
    internal static async Task<string?> ReadLineWithTimeoutAsync(StreamReader reader, TimeSpan timeout, CancellationToken ct)
    {
        using var idle = CancellationTokenSource.CreateLinkedTokenSource(ct);
        idle.CancelAfter(timeout);
        try { return await reader.ReadLineAsync(idle.Token).ConfigureAwait(false); }
        catch (OperationCanceledException ex) when (!ct.IsCancellationRequested)
        { throw new TimeoutException("SSE did not send a line/heartbeat before the idle deadline.", ex); }
    }

    private async Task<bool> ListenAsync(
        IngestionOptions options, string streamUrl, CancellationToken ct)
    {
        using var handler = new HttpClientHandler();
        if (options.AllowSelfSignedCert)
        {
            if (Interlocked.Exchange(ref _selfSignedLogged, 1) == 0)
                _logger.LogWarning("[SSE] Đang bỏ kiểm tra chứng thư HTTPS theo cấu hình.");

            handler.ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        }

        if (streamUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            && Interlocked.Exchange(ref _insecureHttpLogged, 1) == 0)
        {
            _logger.LogWarning("[SSE] Đang dùng HTTP không mã hóa theo AllowInsecureHttp=true.");
        }

        using var http = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan };
        using var request = new HttpRequestMessage(HttpMethod.Get, streamUrl);
        request.Headers.Accept.ParseAdd("text/event-stream");
        options.ApplyAuth(request);

        using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        connectCts.CancelAfter(TimeSpan.FromSeconds(options.EffectiveConnectTimeoutSeconds));
        using var response = await http.SendAsync(
            request, HttpCompletionOption.ResponseHeadersRead, connectCts.Token).ConfigureAwait(false);
        connectCts.CancelAfter(Timeout.InfiniteTimeSpan);

        LogAuthenticationFailure(response.StatusCode);
        response.EnsureSuccessStatusCode();
        _connectedUtc = DateTime.UtcNow;

        // Mở kết nối rồi ĐẶT YÊU CẦU bù, KHÔNG chờ lượt bù xong mới đọc live.
        //
        // ĐỔI HÀNH VI 23/09/2026. Bản cũ gọi thẳng BackfillCoordinator ở đây với lý do "để frame history được
        // enqueue trước frame live". Lý do đó không còn đứng được: mọi tầng dưới đã tự chống lệch thứ tự —
        // QuestDB chống trùng theo (detectTime, vendorEventId), TmsIncident có UNIQUE theo EventId,
        // VehicleDedupGate so TRỊ TUYỆT ĐỐI khoảng thời gian, còn trạng thái vùng và cảnh báo biển số thì bỏ
        // qua frame bù. Trong khi cái giá phải trả là toàn bộ dữ liệu live: một lỗi trong lượt bù thoát ra
        // vòng kết nối và dừng cả nguồn (đo được 2 ngày không thu được bản tin nào).
        var trigger = _hasConnectedOnce ? BackfillTrigger.Reconnect : BackfillTrigger.Startup;
        WarnIfRequireSuccessBeforeLive();
        _backfillRequest.Request(trigger);
        _hasConnectedOnce = true;

        _logger.LogInformation(
            "[SSE] Đã kết nối; settle={Settle}s, sources={Sources}.",
            options.EffectiveSettleSeconds,
            string.IsNullOrWhiteSpace(options.Sources) ? "(tất cả)" : options.Sources);

        await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var data = new StringBuilder();

        while (!ct.IsCancellationRequested)
        {
            if (!_config.Current.Ingestion.SseEnabled) return true;

            var line = await ReadLineWithTimeoutAsync(reader,
                TimeSpan.FromSeconds(_config.Current.Ingestion.EffectiveSseIdleTimeoutSeconds), ct).ConfigureAwait(false);
            if (line is null) return false;
            if (line.StartsWith(':')) continue;

            if (line.Length == 0)
            {
                if (data.Length == 0) continue;

                var frame = data.ToString();
                while (!ct.IsCancellationRequested)
                {
                    if (!await WaitUntilReadyAsync(ct).ConfigureAwait(false)) return true;

                    if (await _dispatcher.DispatchAsync(
                            frame, isBackfill: false, ct).ConfigureAwait(false))
                    {
                        data.Clear();
                        break;
                    }

                    await DelayAsync(TimeSpan.FromMilliseconds(500), ct).ConfigureAwait(false);
                }

                continue;
            }

            if (!line.StartsWith("data:", StringComparison.Ordinal)) continue;

            var fragment = line.AsSpan(5).TrimStart();
            var separatorLength = data.Length > 0 ? Environment.NewLine.Length : 0;
            if (data.Length + separatorLength + fragment.Length > options.EffectiveMaxFrameCharacters)
            {
                throw new InvalidDataException(
                    $"SSE frame vượt giới hạn {options.EffectiveMaxFrameCharacters} ký tự.");
            }

            if (data.Length > 0) data.AppendLine();
            data.Append(fragment);
        }

        return false;
    }

    /// <summary>
    /// Cảnh báo MỘT lần nếu cấu hình còn đặt <c>Backfill:RequireSuccessBeforeLive=true</c>.
    ///
    /// <para>Khoá này mất tác dụng từ 23/09/2026: lượt bù không còn nằm trên đường kết nối nên không có gì để
    /// "chặn live" nữa. Giữ key trong cấu hình để file appsettings cũ không lỗi bind, nhưng phải nói rõ ra —
    /// im lặng bỏ qua một công tắc mà người vận hành tin là đang có hiệu lực còn tệ hơn.</para>
    /// </summary>
    private void WarnIfRequireSuccessBeforeLive()
    {
        if (!_config.Current.Backfill.RequireSuccessBeforeLive) return;
        if (Interlocked.Exchange(ref _requireSuccessWarned, 1) != 0) return;

        _logger.LogWarning(
            "[Backfill] Backfill:RequireSuccessBeforeLive=true KHÔNG còn tác dụng — từ 23/09/2026 lượt bù " +
            "chạy nền và không bao giờ chặn đường live. Bỏ khoá này khỏi cấu hình để tránh hiểu sai.");
    }

    private async Task<bool> WaitUntilReadyAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var runtime = _config.Current;
            if (!runtime.Ingestion.SseEnabled) return false;
            if (runtime.QuestEnabled && _cameras.Count > 0) return true;
            await DelayAsync(ReadyPollInterval, ct).ConfigureAwait(false);
        }

        return false;
    }

    private void LogAuthenticationFailure(HttpStatusCode statusCode)
    {
        if (statusCode == HttpStatusCode.Unauthorized)
            _logger.LogError("[SSE] Máy chủ trả 401 — API key sai hoặc đã bị thu hồi.");
        else if (statusCode == HttpStatusCode.Forbidden)
            _logger.LogError("[SSE] Máy chủ trả 403 — API key/IP không có quyền đọc events.");
    }

    internal static bool TryBuildStreamUrl(
        IngestionOptions options, out string? streamUrl)
    {
        streamUrl = null;
        if (!Uri.TryCreate(options.ApiBaseUrl?.Trim(), UriKind.Absolute, out var baseUri)
            || baseUri.Scheme is not ("http" or "https")
            || (baseUri.Scheme == "http" && !options.AllowInsecureHttp))
        {
            return false;
        }

        var baseUrl = options.ApiBaseUrl.TrimEnd('/');
        var path = "/" + (string.IsNullOrWhiteSpace(options.StreamPath)
            ? "api/v1/events/stream"
            : options.StreamPath.TrimStart('/'));
        if (!Uri.TryCreate(baseUrl + path, UriKind.Absolute, out var combined)) return false;

        var builder = new UriBuilder(combined);
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(options.Sources))
            query.Add("sources=" + Uri.EscapeDataString(options.Sources.Trim()));
        query.Add("settle=" + options.EffectiveSettleSeconds);
        builder.Query = string.Join("&", query);
        streamUrl = builder.Uri.AbsoluteUri;
        return true;
    }

    private static TimeSpan Backoff(IngestionOptions options, int attempt)
    {
        var exponent = Math.Min(20, Math.Max(0, attempt - 1));
        var milliseconds = Math.Min(
            options.EffectiveReconnectMaxSeconds * 1_000d,
            options.EffectiveReconnectInitialMs * Math.Pow(2, exponent));
        return TimeSpan.FromMilliseconds(milliseconds);
    }

    private static async Task DelayAsync(TimeSpan delay, CancellationToken ct)
    {
        try
        {
            await Task.Delay(delay, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Host đang dừng.
        }
    }
}
