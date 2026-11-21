using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;

namespace ITS.Sync.Infrastructure.Messaging;

/// <summary>
/// Phát heartbeat định kỳ lên NATS để FE giám sát dịch vụ WP_Sync Online/Offline.
/// Mỗi <see cref="NatsSyncOptions.HeartbeatIntervalSeconds"/> giây gửi 1 gói { data = "ok" }
/// lên subject <see cref="NatsSyncOptions.HeartbeatSubject"/> (mặc định "ta.its.heartbeat.wpsync").
/// Dùng CÙNG cấu hình "Nats" (Url/Auth) với <see cref="NatsSyncListener"/> nhưng mở kết nối riêng
/// để không phụ thuộc vòng lặp subscribe của listener. Không phụ thuộc host: Worker bọc bằng BackgroundService,
/// WPF gọi trực tiếp Start()/Stop(). Không ném exception ra ngoài vòng lặp; tự kết nối lại khi lỗi.
/// </summary>
public sealed class SyncHeartbeatPublisher : IAsyncDisposable
{
    private readonly NatsSyncOptions _options;
    private readonly ILogger<SyncHeartbeatPublisher> _logger;

    private CancellationTokenSource? _cts;
    private Task? _loop;

    public SyncHeartbeatPublisher(NatsSyncOptions options, ILogger<SyncHeartbeatPublisher> logger)
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
            _logger.LogInformation("NATS heartbeat đang TẮT (Nats:HeartbeatEnabled = false).");
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
            ? "ta.its.heartbeat.wpsync"
            : _options.HeartbeatSubject.Trim();
        var interval = TimeSpan.FromSeconds(_options.HeartbeatIntervalSeconds > 0 ? _options.HeartbeatIntervalSeconds : 30);
        var retry = TimeSpan.FromSeconds(_options.RetryIntervalSeconds > 0 ? _options.RetryIntervalSeconds : 5);

        // Gói tin tối giản { data = "ok" } — FE chỉ cần biết có gói đến trên subject để tính Online.
        var payload = JsonSerializer.Serialize(new { data = "ok" });

        while (!token.IsCancellationRequested)
        {
            try
            {
                await using var conn = new NatsConnection(NatsSyncFactory.BuildOpts(_options, "HB"));
                await conn.ConnectAsync().ConfigureAwait(false);

                _logger.LogInformation("NATS heartbeat ĐÃ KẾT NỐI {Url}. Phát '{Subject}' mỗi {Seconds}s.",
                    _options.Url, subject, interval.TotalSeconds);

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
                _logger.LogError(ex, "NATS heartbeat lỗi kết nối/phát: {Message}. Thử lại sau {Seconds}s.",
                    ex.Message, retry.TotalSeconds);
            }

            if (token.IsCancellationRequested) break;

            try { await Task.Delay(retry, token).ConfigureAwait(false); }
            catch (OperationCanceledException) { break; }
        }

        _logger.LogInformation("NATS heartbeat đã dừng.");
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
    }
}
