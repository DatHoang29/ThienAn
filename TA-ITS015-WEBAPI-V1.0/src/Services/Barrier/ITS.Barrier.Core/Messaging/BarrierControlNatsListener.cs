using System.Collections.Concurrent;
using System.Text.Json;
using ITS.Barrier.Core.Integration;
using ITS.Barrier.Core.Logging;
using NATS.Client.Core;

namespace ITS.Barrier.Core.Messaging;

/// <summary>
/// Lắng nghe các gói tin điều khiển trên kênh NATS ta.its.control.barrier.
/// Nhận lệnh Request -> Xử lý song song qua BarrierDeviceExecutor -> Bắn lại gói tin Result lên cùng kênh.
/// Author: ChuongLP
/// Date: 05/10/2026
/// </summary>
public sealed class BarrierControlNatsListener : IAsyncDisposable
{
    private readonly BarrierControlNatsOption _options;
    private readonly BarrierDeviceExecutor _executor;
    private readonly BarrierLogger _logger;

    private static readonly JsonSerializerOptions JsonRead = new(JsonSerializerDefaults.Web);
    private static readonly JsonSerializerOptions JsonWrite = new(JsonSerializerDefaults.Web) { WriteIndented = false };

    private CancellationTokenSource? _cts;
    private Task? _loop;

    public event Action<BarrierControlRequest>? RequestReceived;
    public event Action<BarrierControlResult>? ResultPublished;

    public BarrierControlNatsListener(BarrierControlNatsOption options, BarrierDeviceExecutor executor, BarrierLogger logger)
    {
        _options = options;
        _executor = executor;
        _logger = logger;
    }

    /// <summary>Đang lắng nghe hay không.</summary>
    public bool IsListening => _loop is { IsCompleted: false };

    /// <summary>
    /// Bắt đầu lắng nghe (không chặn).
    /// </summary>
    public void Start()
    {
        if (IsListening) return;

        if (!_options.Enabled)
        {
            _logger.Info("[NATS] Lắng nghe điều khiển Barrier đang TẮT (Nats:Enabled = false).");
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
            try { await _loop.ConfigureAwait(false); } catch { /* log handled inside */ }
        }
        _cts?.Dispose();
        _cts = null;
        _loop = null;
    }

    /// <summary>
    /// Vòng lặp lắng nghe kênh NATS. Tự kết nối lại vô hạn khi mất kết nối.
    /// </summary>
    private async Task RunAsync(CancellationToken token)
    {
        var subject = BarrierNatsFactory.ResolveSubject(_options);
        var retry = TimeSpan.FromSeconds(_options.RetryIntervalSeconds > 0 ? _options.RetryIntervalSeconds : 5);

        while (!token.IsCancellationRequested)
        {
            try
            {
                _logger.Info($"[NATS] Đang kết nối {_options.Url} ...");

                await using var conn = new NatsConnection(BarrierNatsFactory.BuildOpts(_options));
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

        _logger.Info("[NATS] Đã dừng lắng nghe điều khiển Barrier.");
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

        BarrierControlRequest? req;
        try
        {
            req = JsonSerializer.Deserialize<BarrierControlRequest>(payload, JsonRead);
        }
        catch (Exception ex)
        {
            _logger.Error($"[NATS] Gói tin không đọc được JSON: {ex.Message}");
            return;
        }

        if (req is null) return;

        // Dùng chung 1 kênh 2 chiều: bỏ qua mọi gói không phải request (tránh tự xử lý gói result của mình)
        if (!string.Equals(req.Type?.Trim(), BarrierControlType.Request, StringComparison.OrdinalIgnoreCase))
            return;

        var action = string.IsNullOrWhiteSpace(req.Action) ? BarrierControlAction.Open : req.Action.Trim().ToLowerInvariant();
        req.Action = action;
        var targets = req.Targets?.Where(t => t is not null).ToList() ?? new List<BarrierControlTarget>();

        RequestReceived?.Invoke(req);

        var result = new BarrierControlResult
        {
            RequestId = req.RequestId,
            Action = action,
            Type = BarrierControlType.Result
        };

        if (targets.Count == 0)
        {
            result.Success = false;
            result.Message = "Yêu cầu không có thiết bị barrier nào.";
            await PublishResultAsync(conn, subject, result, ct).ConfigureAwait(false);
            return;
        }

        _logger.Info($"[NATS] Nhận yêu cầu · RequestId={req.RequestId} · Action={action} · {targets.Count} thiết bị" +
                     (req.Simulate ? " · GIẢ LẬP" : string.Empty));

        var bag = new ConcurrentBag<BarrierTargetResult>();

        if (req.Simulate)
        {
            foreach (var t in targets)
            {
                bag.Add(new BarrierTargetResult
                {
                    EquipmentId = t.EquipmentId ?? string.Empty,
                    Success = true,
                    Message = "Giả lập thành công (simulate = true)."
                });
            }
        }
        else
        {
            var parallel = new ParallelOptions
            {
                MaxDegreeOfParallelism = _options.MaxParallelDevice > 0 ? _options.MaxParallelDevice : 4,
                CancellationToken = ct
            };

            await Parallel.ForEachAsync(targets, parallel, async (target, c) =>
            {
                using var devCts = CancellationTokenSource.CreateLinkedTokenSource(c);
                devCts.CancelAfter(TimeSpan.FromSeconds(_options.DeviceTimeoutSeconds > 0 ? _options.DeviceTimeoutSeconds : 10));

                var one = await _executor.ExecuteAsync(action, target, req.Simulate, devCts.Token).ConfigureAwait(false);
                bag.Add(one);
            }).ConfigureAwait(false);
        }

        result.Results = targets
            .Select(t => bag.FirstOrDefault(r => r.EquipmentId == (t.EquipmentId ?? string.Empty))
                         ?? new BarrierTargetResult { EquipmentId = t.EquipmentId ?? string.Empty, Success = false, Message = "Không có kết quả." })
            .ToList();

        var okCount = result.Results.Count(r => r.Success);
        result.Success = okCount == result.Results.Count;
        result.Message = $"Đã xử lý {result.Results.Count} barrier (Thành công: {okCount}, Thất bại: {result.Results.Count - okCount})";
        result.FinishedAt = DateTime.Now;

        ResultPublished?.Invoke(result);
        await PublishResultAsync(conn, subject, result, ct).ConfigureAwait(false);
    }

    private async Task PublishResultAsync(NatsConnection conn, string subject, BarrierControlResult result, CancellationToken ct)
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
