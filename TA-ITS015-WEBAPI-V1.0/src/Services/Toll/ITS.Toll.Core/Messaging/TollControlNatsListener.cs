using System.Text.Json;
using ITS.Toll.Core.Logging;
using ITS.Toll.Core.Services;
using NATS.Client.Core;

namespace ITS.Toll.Core.Messaging;

/// <summary>
/// Lắng nghe kênh NATS ta.its.control.toll để lấy dữ liệu thu phí THỦ CÔNG theo yêu cầu từ FE/BE.
/// Luồng: FE bấm nút -> BE phát {Type="request", RequestId, DataTypes[], FromDate?, ToDate?} lên kênh.
///   -> Dịch vụ nhận, với TỪNG loại dữ liệu gọi lại <see cref="TollCallBackService.RepullRangeAsync"/>
///      (đúng luồng pull hiện có: đọc endpoint SysOpConfig -> gọi API TCS -> lưu DB).
///   -> Dịch vụ phát {Type="result", RequestId, Success, Message, Items[], tổng số bản ghi} lên CÙNG kênh
///      để FE (subscribe trực tiếp) hiển thị thành công/lỗi.
/// Dùng chung 1 kênh 2 chiều nên dịch vụ BỎ QUA các gói Type != "request" (tránh tự xử lý lại gói result của mình).
/// Tự kết nối lại khi mất kết nối; không ném exception ra ngoài vòng lặp.
/// Lớp này KHÔNG phụ thuộc host cụ thể: ITS.Toll.Worker bọc bằng BackgroundService; ITS.Toll.Wpf gọi
/// trực tiếp Start()/Stop() lúc mở/đóng form. Mọi diễn tiến ghi qua <see cref="TollLogger"/> nên form WPF
/// (đang lắng nghe TollLogger.EntryLogged) sẽ tự hiển thị log NATS lên Nhật ký.
/// Author: ChuongLP
/// Created date: 14/09/2026
/// </summary>
public sealed class TollControlNatsListener : IAsyncDisposable
{
    private readonly TollControlNatsOptions _options;
    private readonly TollCallBackService _callback;
    private readonly TollLogger _logger;

    private static readonly JsonSerializerOptions JsonRead = new(JsonSerializerDefaults.Web);
    private static readonly JsonSerializerOptions JsonWrite = new() { WriteIndented = false };

    private CancellationTokenSource? _cts;
    private Task? _loop;

    public TollControlNatsListener(TollControlNatsOptions options, TollCallBackService callback, TollLogger logger)
    {
        _options = options;
        _callback = callback;
        _logger = logger;
    }

    /// <summary>Đang lắng nghe hay không.</summary>
    public bool IsListening => _loop is { IsCompleted: false };

    /// <summary>Bắt đầu lắng nghe (không chặn). Gọi lại nhiều lần an toàn — nếu đang chạy thì bỏ qua.</summary>
    public void Start()
    {
        if (IsListening) return;

        if (!_options.Enabled)
        {
            _logger.Info(LogCategory.Sync, "[NATS] Lắng nghe điều khiển đang TẮT (Nats:Enabled = false).");
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
        var subject = string.IsNullOrWhiteSpace(_options.ControlSubject) ? "ta.its.control.toll" : _options.ControlSubject.Trim();
        var retry = TimeSpan.FromSeconds(_options.RetryIntervalSeconds > 0 ? _options.RetryIntervalSeconds : 5);

        while (!token.IsCancellationRequested)
        {
            try
            {
                _logger.Info(LogCategory.Sync, $"[NATS] Đang kết nối {_options.Url} ...");

                await using var conn = new NatsConnection(TollNatsFactory.BuildOpts(_options));
                await conn.ConnectAsync().ConfigureAwait(false);

                _logger.Success(LogCategory.Sync, $"[NATS] ĐÃ KẾT NỐI {_options.Url}. Lắng nghe kênh '{subject}'.");

                await foreach (var msg in conn.SubscribeAsync<string>(subject, cancellationToken: token).ConfigureAwait(false))
                {
                    if (token.IsCancellationRequested) break;
                    await HandleMessageAsync(conn, subject, msg.Data, token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.Error(LogCategory.Sync, $"[NATS] Kết nối/lắng nghe lỗi: {ex.Message}. Thử lại sau {retry.TotalSeconds:0}s.");
            }

            if (token.IsCancellationRequested) break;

            try { await Task.Delay(retry, token).ConfigureAwait(false); }
            catch (OperationCanceledException) { break; }
        }

        _logger.Info(LogCategory.Sync, "[NATS] Đã dừng lắng nghe điều khiển.");
    }

    /// <summary>Phân tích gói tin; chỉ xử lý Type="request". Với từng loại dữ liệu gọi lại luồng pull rồi phát kết quả.</summary>
    private async Task HandleMessageAsync(NatsConnection conn, string subject, string? payload, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(payload)) return;

        TollControlRequest? req;
        try
        {
            req = JsonSerializer.Deserialize<TollControlRequest>(payload, JsonRead);
        }
        catch (Exception ex)
        {
            _logger.Error(LogCategory.Sync, $"[NATS] Gói tin không đọc được JSON: {ex.Message}");
            return;
        }

        if (req is null) return;

        // Dùng chung 1 kênh 2 chiều: bỏ qua mọi gói không phải yêu cầu (vd result do chính dịch vụ phát).
        if (!string.Equals(req.Type?.Trim(), "request", StringComparison.OrdinalIgnoreCase))
            return;

        var dataTypes = (req.DataTypes ?? new List<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var result = new TollControlResult { RequestId = req.RequestId };

        if (dataTypes.Count == 0)
        {
            result.Success = false;
            result.Message = "Yêu cầu không có loại dữ liệu cần lấy.";
            await PublishResultAsync(conn, subject, result, ct).ConfigureAwait(false);
            return;
        }

        // Chuẩn hóa khoảng thời gian: mặc định lùi lại DefaultLookbackHours nếu FE không gửi (dùng cho loại cần khoảng thời gian).
        var lookback = _options.DefaultLookbackHours > 0 ? _options.DefaultLookbackHours : 24;
        var to = req.ToDate ?? DateTime.Now;
        var from = req.FromDate ?? to.AddHours(-lookback);

        _logger.Info(LogCategory.Sync,
            $"[NATS] Nhận yêu cầu lấy dữ liệu thủ công · RequestId={req.RequestId} · [{string.Join(", ", dataTypes)}] · {from:yyyy-MM-dd HH:mm:ss} … {to:yyyy-MM-dd HH:mm:ss}");

        // Phát "ack" ngay lập tức để FE biết Worker đang online và đã nhận request.
        // FE dùng để phân biệt: không nhận ack (sau ~8s) → Worker có thể offline;
        // nhận ack nhưng result về chậm → Worker đang xử lý (TCS nhiều dữ liệu / chậm).
        var ack = new TollControlAck { RequestId = req.RequestId, DataTypes = dataTypes };
        await PublishAckAsync(conn, subject, ack, ct).ConfigureAwait(false);

        foreach (var dataType in dataTypes)
        {
            var item = new TollControlResultItem { DataType = dataType };
            try
            {
                var save = await _callback.RepullRangeAsync(dataType, from, to, ct).ConfigureAwait(false);
                item.Ok = save.Ok;
                item.Message = save.Message;
                item.Received = save.Received;
                item.Inserted = save.Inserted;
                item.Updated = save.Updated;
                item.Unchanged = save.Unchanged;
                item.Failed = save.Failed;
                item.Deleted = save.Deleted;
            }
            catch (Exception ex)
            {
                item.Ok = false;
                item.Message = ex.Message;
                _logger.Error(LogCategory.Sync, $"[NATS] Lỗi lấy [{dataType}]: {ex.Message}");
            }

            result.Items.Add(item);
            result.Received += item.Received;
            result.Inserted += item.Inserted;
            result.Updated += item.Updated;
            result.Unchanged += item.Unchanged;
            result.Failed += item.Failed;
            result.Deleted += item.Deleted;
        }

        result.Success = result.Items.All(i => i.Ok);
        result.Message = result.Success
            ? $"Lấy dữ liệu thành công: +{result.Inserted} mới, ~{result.Updated} cập nhật, -{result.Deleted} đã xóa (tổng {result.Received} bản ghi)."
            : $"Có loại dữ liệu lấy lỗi: {string.Join("; ", result.Items.Where(i => !i.Ok).Select(i => $"{i.DataType}: {i.Message}"))}";

        await PublishResultAsync(conn, subject, result, ct).ConfigureAwait(false);
    }

    /// <summary>Phát kết quả (Type="result") lên cùng kênh cho FE.</summary>
    private async Task PublishResultAsync(NatsConnection conn, string subject, TollControlResult result, CancellationToken ct)
    {
        try
        {
            var json = JsonSerializer.Serialize(result, JsonWrite);
            await conn.PublishAsync(subject, json, cancellationToken: ct).ConfigureAwait(false);

            if (result.Success)
                _logger.Success(LogCategory.Sync, $"[NATS] Đã phát kết quả · RequestId={result.RequestId} · {result.Message}");
            else
                _logger.Error(LogCategory.Sync, $"[NATS] Đã phát kết quả (LỖI) · RequestId={result.RequestId} · {result.Message}");
        }
        catch (Exception ex)
        {
            _logger.Error(LogCategory.Sync, $"[NATS] Không phát được kết quả · RequestId={result.RequestId}: {ex.Message}");
        }
    }

    /// <summary>
    /// Phát xác nhận (Type="ack") ngay khi nhận request hợp lệ, trước khi bắt đầu xử lý từng DataType.
    /// Lỗi phát ack không dừng luồng xử lý chính — FE chỉ mất phân biệt "offline vs đang xử lý",
    /// kết quả cuối vẫn được phát qua PublishResultAsync.
    /// </summary>
    private async Task PublishAckAsync(NatsConnection conn, string subject, TollControlAck ack, CancellationToken ct)
    {
        try
        {
            var json = JsonSerializer.Serialize(ack, JsonWrite);
            await conn.PublishAsync(subject, json, cancellationToken: ct).ConfigureAwait(false);
            _logger.Info(LogCategory.Sync, $"[NATS] Đã phát ACK · RequestId={ack.RequestId} · {ack.DataTypes.Count} loại dữ liệu");
        }
        catch (Exception ex)
        {
            _logger.Warning(LogCategory.Sync, $"[NATS] Không phát được ACK · RequestId={ack.RequestId}: {ex.Message}");
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
    }
}
