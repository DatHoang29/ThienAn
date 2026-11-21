using System.Collections.Concurrent;
using System.Text.Json;
using ITS.TrafficLight.Core.Logging;
using NATS.Client.Core;

namespace ITS.TrafficLight.Core.Messaging;

/// <summary>
/// Bên GỬI lệnh trên kênh ta.its.control.trafficlight — đóng vai BE / WPF để gửi lệnh và nhận kết quả đối chiếu qua NATS.
/// Author: ChuongLP
/// Date: 05/10/2026
/// </summary>
public sealed class TrafficLightControlSender : IAsyncDisposable
{
    private readonly TrafficLightControlNatsOption _options;
    private readonly TrafficLightLogger _logger;

    private static readonly JsonSerializerOptions JsonRead = new(JsonSerializerDefaults.Web);
    private static readonly JsonSerializerOptions JsonWrite = new(JsonSerializerDefaults.Web) { WriteIndented = false };

    private readonly ConcurrentDictionary<string, TaskCompletionSource<TrafficLightControlResult>> _pending = new();
    private NatsConnection? _conn;
    private CancellationTokenSource? _cts;
    private Task? _loop;

    public event Action<string, string>? PacketObserved;

    public TrafficLightControlSender(TrafficLightControlNatsOption options, TrafficLightLogger logger)
    {
        _options = options;
        _logger = logger;
    }

    public bool IsConnected => _conn is not null && _loop is { IsCompleted: false };
    public string Subject => TrafficLightNatsFactory.ResolveSubject(_options);

    public async Task StartAsync()
    {
        if (IsConnected) return;

        _logger.Info($"[NATS SENDER] Đang kết nối tới {_options.Url} ...");

        var conn = new NatsConnection(TrafficLightNatsFactory.BuildOpts(_options, "SENDER"));
        await conn.ConnectAsync().ConfigureAwait(false);

        _conn = conn;
        _cts = new CancellationTokenSource();
        _loop = Task.Run(() => ObserveAsync(_cts.Token));

        _logger.Info($"[NATS SENDER] ĐÃ KẾT NỐI {_options.Url}. Theo dõi kênh '{Subject}'.");
    }

    public async Task StopAsync()
    {
        try { _cts?.Cancel(); } catch (ObjectDisposedException) { }

        if (_loop is not null)
        {
            try { await _loop.ConfigureAwait(false); } catch { }
        }

        if (_conn is not null)
        {
            try { await _conn.DisposeAsync().ConfigureAwait(false); } catch { }
            _conn = null;
        }

        _cts?.Dispose();
        _cts = null;
        _loop = null;

        _logger.Info("[NATS SENDER] Đã ngắt kết nối.");
    }

    public async Task<TrafficLightControlResult?> SendAsync(TrafficLightControlRequest request, int timeoutSeconds = 0)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        if (_conn is null) throw new InvalidOperationException("Chưa kết nối NATS. Vui lòng kết nối trước khi gửi.");

        request.Type = TrafficLightControlType.Request;
        if (string.IsNullOrWhiteSpace(request.RequestId)) request.RequestId = Guid.NewGuid().ToString("N");
        request.RequestTime = DateTime.Now;

        var wait = timeoutSeconds > 0 ? timeoutSeconds : Math.Max(5, _options.DeviceTimeoutSeconds) + 5;
        var tcs = new TaskCompletionSource<TrafficLightControlResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[request.RequestId] = tcs;

        try
        {
            var json = JsonSerializer.Serialize(request, JsonWrite);
            PacketObserved?.Invoke(request.Type, json);

            await _conn.PublishAsync(Subject, json).ConfigureAwait(false);
            _logger.Info($"[NATS SENDER] Đã gửi lệnh {request.Action} ({request.Color}) (RequestId={request.RequestId}, Targets={request.Targets.Count}, Simulate={request.Simulate})");

            using var delayCts = new CancellationTokenSource(TimeSpan.FromSeconds(wait));
            using (delayCts.Token.Register(() => tcs.TrySetCanceled()))
            {
                return await tcs.Task.ConfigureAwait(false);
            }
        }
        catch (TaskCanceledException)
        {
            _logger.Warn($"[NATS SENDER] Quá thời gian chờ {wait}s cho RequestId={request.RequestId}. Dịch vụ Worker chưa bật hoặc thiết bị timeout.");
            return null;
        }
        finally
        {
            _pending.TryRemove(request.RequestId, out _);
        }
    }

    private async Task ObserveAsync(CancellationToken token)
    {
        if (_conn is null) return;

        try
        {
            await foreach (var msg in _conn.SubscribeAsync<string>(Subject, cancellationToken: token).ConfigureAwait(false))
            {
                if (token.IsCancellationRequested) break;
                if (string.IsNullOrWhiteSpace(msg.Data)) continue;

                try
                {
                    using var doc = JsonDocument.Parse(msg.Data);
                    var type = doc.RootElement.TryGetProperty("type", out var prop)
                        ? prop.GetString()
                        : (doc.RootElement.TryGetProperty("Type", out prop) ? prop.GetString() : null);

                    PacketObserved?.Invoke(type ?? "unknown", msg.Data);

                    if (string.Equals(type, TrafficLightControlType.Result, StringComparison.OrdinalIgnoreCase))
                    {
                        var result = JsonSerializer.Deserialize<TrafficLightControlResult>(msg.Data, JsonRead);
                        if (result is not null && !string.IsNullOrWhiteSpace(result.RequestId)
                            && _pending.TryGetValue(result.RequestId, out var tcs))
                        {
                            tcs.TrySetResult(result);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"[NATS SENDER] Lỗi giải mã gói tin: {ex.Message}", ex);
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.Error($"[NATS SENDER] Vòng lặp quan sát bị ngắt: {ex.Message}", ex);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
    }
}
