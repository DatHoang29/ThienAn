using ITS.VMS.Core.Logging;
using NATS.Client.Core;

namespace ITS.VMS.Core.Messaging;

/// <summary>
/// Phát heartbeat định kỳ lên NATS để FE giám sát dịch vụ VMS Online/Offline.
/// Mỗi <see cref="VmsControlNatsOption.HeartbeatIntervalSeconds"/> giây gửi 1 gói { data = "ok" }
/// lên subject <see cref="VmsControlNatsOption.HeartbeatSubject"/> (mặc định "ta.its.heartbeat.vms").
/// Dùng CÙNG cấu hình "Nats" (Url/Auth) với <see cref="VmsControlNatsListener"/> nhưng mở kết nối riêng
/// để không phụ thuộc vòng lặp subscribe của listener. Không phụ thuộc host: Worker bọc bằng BackgroundService,
/// WPF có thể gọi trực tiếp Start()/StopAsync(). Không ném exception ra ngoài vòng lặp; tự kết nối lại khi lỗi.
/// Author: TAC Team
/// Created date: 28/09/2026
/// </summary>
public sealed class VmsHeartbeatPublisher : IAsyncDisposable
{
    private readonly VmsControlNatsOption _options;
    private readonly VmsLogger _logger;

    private CancellationTokenSource? _cts;
    private Task? _loop;

    public VmsHeartbeatPublisher(VmsControlNatsOption options, VmsLogger logger)
    {
        _options = options;
        _logger = logger;
    }

    /// <summary>Đang chạy hay không.</summary>
    public bool IsRunning => _loop is { IsCompleted: false };

    /// <summary>Bắt đầu phát heartbeat (không chặn). Gọi lại nhiều lần an toàn.</summary>
    public void Start()
    {
        if (IsRunning) return;

        if (!_options.HeartbeatEnabled)
        {
            _logger.Info(LogCategory.Control, "[NATS] Heartbeat đang TẮT (Nats:HeartbeatEnabled = false).");
            return;
        }

        _cts = new CancellationTokenSource();
        _loop = Task.Run(() => RunAsync(_cts.Token));
    }

    /// <summary>Yêu cầu dừng (không chặn).</summary>
    public void Stop()
    {
        try { _cts?.Cancel(); } catch (ObjectDisposedException) { }
    }

    /// <summary>Dừng và chờ vòng lặp kết thúc hẳn.</summary>
    public async Task StopAsync()
    {
        Stop();
        if (_loop is not null)
        {
            try { await _loop.ConfigureAwait(false); } catch { /* đã ghi log bên trong */ }
        }
        _cts?.Dispose();
        _cts = null;
        _loop = null;
    }

    private async Task RunAsync(CancellationToken token)
    {
        var subject = string.IsNullOrWhiteSpace(_options.HeartbeatSubject)
            ? "ta.its.heartbeat.vms"
            : _options.HeartbeatSubject.Trim();
        var interval = TimeSpan.FromSeconds(_options.HeartbeatIntervalSeconds > 0 ? _options.HeartbeatIntervalSeconds : 30);
        var retry = TimeSpan.FromSeconds(_options.RetryIntervalSeconds > 0 ? _options.RetryIntervalSeconds : 5);

        // Gói tin tối giản { data = "ok" } — FE chỉ cần biết có gói đến trên subject để tính Online.
        var payload = System.Text.Json.JsonSerializer.Serialize(new { data = "ok" });

        while (!token.IsCancellationRequested)
        {
            try
            {
                _logger.Info(LogCategory.Control, $"[NATS] Heartbeat đang kết nối {_options.Url} ...");

                await using var conn = new NatsConnection(VmsNatsFactory.BuildOpts(_options, "HB"));
                await conn.ConnectAsync().ConfigureAwait(false);

                _logger.Success(LogCategory.Control, $"[NATS] Heartbeat ĐÃ KẾT NỐI {_options.Url}. Phát '{subject}' mỗi {interval.TotalSeconds:0}s.");

                while (!token.IsCancellationRequested)
                {
                    await conn.PublishAsync(subject, payload, cancellationToken: token).ConfigureAwait(false);

                    try { await Task.Delay(interval, token).ConfigureAwait(false); }
                    catch (OperationCanceledException) { break; }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.Error(LogCategory.Control, $"[NATS] Heartbeat lỗi kết nối/phát: {ex.Message}. Thử lại sau {retry.TotalSeconds:0}s.");
            }

            if (token.IsCancellationRequested) break;

            try { await Task.Delay(retry, token).ConfigureAwait(false); }
            catch (OperationCanceledException) { break; }
        }

        _logger.Info(LogCategory.Control, "[NATS] Đã dừng phát heartbeat.");
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
    }
}
