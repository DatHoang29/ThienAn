using System.Collections.Concurrent;
using System.Text.Json;
using ITS.TrafficLight.Core.Integration;
using ITS.TrafficLight.Core.Logging;
using NATS.Client.Core;

namespace ITS.TrafficLight.Core.Messaging;

/// <summary>
/// Lắng nghe các gói tin điều khiển trên kênh NATS ta.its.control.trafficlight.
/// Nhận lệnh Request -> Xử lý song song qua TrafficLightDeviceExecutor -> Bắn lại gói tin Result lên cùng kênh.
/// Author: ChuongLP
/// Date: 05/10/2026
/// </summary>
public sealed class TrafficLightControlNatsListener : IAsyncDisposable
{
    private readonly TrafficLightControlNatsOption _options;
    private readonly TrafficLightDeviceExecutor _executor;
    private readonly TrafficLightLogger _logger;

    private static readonly JsonSerializerOptions JsonRead = new(JsonSerializerDefaults.Web);
    private static readonly JsonSerializerOptions JsonWrite = new(JsonSerializerDefaults.Web) { WriteIndented = false };

    private CancellationTokenSource? _cts;
    private Task? _loop;

    public event Action<TrafficLightControlRequest>? RequestReceived;
    public event Action<TrafficLightControlResult>? ResultPublished;

    public TrafficLightControlNatsListener(
        TrafficLightControlNatsOption options,
        TrafficLightDeviceExecutor executor,
        TrafficLightLogger logger)
    {
        _options = options;
        _executor = executor;
        _logger = logger;
    }

    public bool IsListening => _loop is { IsCompleted: false };

    public void Start()
    {
        if (IsListening) return;

        if (!_options.Enabled)
        {
            _logger.Info("[NATS] Lắng nghe điều khiển Đèn tín hiệu đang TẮT (Nats:Enabled = false).");
            return;
        }

        _cts = new CancellationTokenSource();
        _loop = Task.Run(() => RunAsync(_cts.Token));
    }

    public void Stop()
    {
        try { _cts?.Cancel(); } catch (ObjectDisposedException) { }
    }

    public async Task StopAsync()
    {
        Stop();
        if (_loop is not null)
        {
            try { await _loop.ConfigureAwait(false); } catch { }
        }
        _cts?.Dispose();
        _cts = null;
        _loop = null;
    }

    private async Task RunAsync(CancellationToken token)
    {
        var subject = TrafficLightNatsFactory.ResolveSubject(_options);
        var retry = TimeSpan.FromSeconds(_options.RetryIntervalSeconds > 0 ? _options.RetryIntervalSeconds : 5);

        while (!token.IsCancellationRequested)
        {
            try
            {
                _logger.Info($"[NATS] Đang kết nối {_options.Url} ...");

                await using var conn = new NatsConnection(TrafficLightNatsFactory.BuildOpts(_options));
                await conn.ConnectAsync().ConfigureAwait(false);

                _logger.Info($"[NATS] ĐÃ KẾT NỐI {_options.Url}. Lắng nghe kênh '{subject}'.");

                await foreach (var msg in conn.SubscribeAsync<string>(subject, cancellationToken: token).ConfigureAwait(false))
                {
                    if (token.IsCancellationRequested) break;
                    _ = HandleMessageSafeAsync(conn, subject, msg.Data, token);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.Error($"[NATS] Kết nối/lắng nghe lỗi: {ex.Message}. Thử lại sau {retry.TotalSeconds:0}s.", ex);
            }

            if (token.IsCancellationRequested) break;

            try { await Task.Delay(retry, token).ConfigureAwait(false); }
            catch (OperationCanceledException) { break; }
        }

        _logger.Info("[NATS] Đã dừng lắng nghe điều khiển Đèn tín hiệu.");
    }

    private async Task HandleMessageSafeAsync(NatsConnection conn, string subject, string? payload, CancellationToken ct)
    {
        try
        {
            await HandleMessageAsync(conn, subject, payload, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.Error($"[NATS] Xử lý gói tin lỗi: {ex.Message}", ex);
        }
    }

    private async Task HandleMessageAsync(NatsConnection conn, string subject, string? payload, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(payload)) return;

        TrafficLightControlRequest? req;
        try
        {
            req = JsonSerializer.Deserialize<TrafficLightControlRequest>(payload, JsonRead);
        }
        catch (Exception ex)
        {
            _logger.Error($"[NATS] Gói tin không đọc được JSON: {ex.Message}");
            return;
        }

        if (req is null) return;

        if (!string.Equals(req.Type?.Trim(), TrafficLightControlType.Request, StringComparison.OrdinalIgnoreCase))
            return;

        var action = string.IsNullOrWhiteSpace(req.Action) ? TrafficLightControlAction.SetColor : req.Action.Trim().ToLowerInvariant();
        req.Action = action;
        var targets = req.Targets?.Where(t => t is not null).ToList() ?? new List<TrafficLightControlTarget>();

        RequestReceived?.Invoke(req);

        var result = new TrafficLightControlResult
        {
            RequestId = req.RequestId,
            Action = action,
            Type = TrafficLightControlType.Result
        };

        if (targets.Count == 0)
        {
            result.Success = false;
            result.Message = "Yêu cầu không có đèn tín hiệu nào.";
            await PublishResultAsync(conn, subject, result, ct).ConfigureAwait(false);
            return;
        }

        _logger.Info($"[NATS] Nhận yêu cầu · RequestId={req.RequestId} · Action={action} · Màu={req.Color} · {targets.Count} đèn" +
                     (req.Simulate ? " · GIẢ LẬP" : string.Empty));

        var bag = new ConcurrentBag<TrafficLightTargetResult>();

        if (req.Simulate)
        {
            foreach (var t in targets)
            {
                bag.Add(new TrafficLightTargetResult
                {
                    EquipmentId = t.EquipmentId ?? string.Empty,
                    EquipmentCode = t.EquipmentCode ?? string.Empty,
                    Success = true,
                    Color = req.Color.ToString(),
                    Message = "Giả lập thành công (simulate = true)."
                });
            }
        }
        else
        {
            var parallel = new ParallelOptions
            {
                MaxDegreeOfParallelism = _options.MaxParallelDevice > 0 ? _options.MaxParallelDevice : 8,
                CancellationToken = ct
            };

            await Parallel.ForEachAsync(targets, parallel, async (target, c) =>
            {
                using var devCts = CancellationTokenSource.CreateLinkedTokenSource(c);
                devCts.CancelAfter(TimeSpan.FromSeconds(_options.DeviceTimeoutSeconds > 0 ? _options.DeviceTimeoutSeconds : 10));

                var one = await _executor.ExecuteAsync(action, req.Color, target, req.Simulate, devCts.Token).ConfigureAwait(false);
                bag.Add(one);
            }).ConfigureAwait(false);
        }

        result.Results = targets
            .Select(t => bag.FirstOrDefault(r => r.EquipmentId == (t.EquipmentId ?? string.Empty))
                         ?? new TrafficLightTargetResult { EquipmentId = t.EquipmentId ?? string.Empty, Success = false, Message = "Không có kết quả." })
            .ToList();

        var okCount = result.Results.Count(r => r.Success);
        result.Success = okCount == result.Results.Count;
        result.Message = $"Đã xử lý {result.Results.Count} đèn tín hiệu (Thành công: {okCount}, Thất bại: {result.Results.Count - okCount})";
        result.FinishedAt = DateTime.Now;

        ResultPublished?.Invoke(result);
        await PublishResultAsync(conn, subject, result, ct).ConfigureAwait(false);
    }

    private async Task PublishResultAsync(NatsConnection conn, string subject, TrafficLightControlResult result, CancellationToken ct)
    {
        try
        {
            var json = JsonSerializer.Serialize(result, JsonWrite);
            await conn.PublishAsync(subject, json, cancellationToken: ct).ConfigureAwait(false);
            _logger.Info($"[NATS] Đã phát kết quả · RequestId={result.RequestId} · Thành công={result.Success} · {result.Message}");
        }
        catch (Exception ex)
        {
            _logger.Error($"[NATS] Không phát được kết quả · RequestId={result.RequestId}: {ex.Message}", ex);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
    }
}
