using Services.Shared.Messaging;

namespace Services.Vds.Shared.Messaging;

/// <summary>
/// Author: KienLT
/// Description: Lớp CHÍNH SÁCH mỏng bọc quanh <see cref="TransportManager"/> cho mọi điểm publish của
/// VDS. Không phải abstraction thứ hai — nó không định nghĩa lại transport, chỉ áp ba quy tắc mà mọi
/// điểm publish đều cần:
///
/// <list type="number">
/// <item><b>Chưa cấu hình/chưa kết nối NATS ⇒ bỏ qua êm.</b>
/// <see cref="TransportManager.PublishAsync"/> NÉM khi chưa kết nối, mà service phải chạy được ở chế độ
/// không NATS (dev, harness). Vì vậy chặn trước và chỉ cảnh báo MỘT lần thay vì mỗi bản tin.</item>
/// <item><b>Publish thất bại không được làm đứt pipeline.</b> Dữ liệu đã ghi DB xong rồi; mất một bản
/// tin broadcast không được phép làm mất bản ghi hoặc làm Wolverine retry cả frame.</item>
/// <item><b>Tự kết nối khi cần.</b> Điểm publish đầu tiên có thể chạy trước khi subscriber cấu hình kịp
/// kết nối; <c>ConnectAsync</c> là idempotent nên gọi ở đây an toàn.</item>
/// </list>
/// Created date: 26/08/2026
/// </summary>
public sealed class VdsPublisher
{
    private readonly TransportManager _transport;
    private readonly ILogger<VdsPublisher> _logger;

    private int _warnedNotConnected;
    private int _warnedConnectFailed;

    public VdsPublisher(TransportManager transport, ILogger<VdsPublisher> logger)
    {
        _transport = transport;
        _logger = logger;
    }

    /// <summary>Đã kết nối transport hay chưa (chưa khai <c>Nats:Url</c> ⇒ false).</summary>
    public bool IsConnected => _transport.IsConnected;

    /// <summary>
    /// Publish một payload. KHÔNG bao giờ ném. Trả false khi bỏ qua (chưa kết nối) hoặc thất bại.
    /// Cancellation của host được tôn trọng: trả false, không biến thành lỗi.
    /// </summary>
    public async Task<bool> SendAsync(string subject, object payload, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(subject) || payload is null) return false;
        if (ct.IsCancellationRequested) return false;

        if (!_transport.IsConnected && !await TryConnectAsync(ct).ConfigureAwait(false))
            return false;

        try
        {
            var sent = await _transport.PublishAsync(subject, payload, ct).ConfigureAwait(false);
            if (!sent)
                _logger.LogWarning("[Publish] {Subject} trả về false.", subject);
            else
                _logger.LogDebug("[Publish] {Subject}", subject);

            return sent;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Publish] {Subject} thất bại; bỏ qua bản tin này (dữ liệu đã ghi DB).", subject);
            return false;
        }
    }

    private async Task<bool> TryConnectAsync(CancellationToken ct)
    {
        try
        {
            await _transport.ConnectAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception ex)
        {
            if (Interlocked.Exchange(ref _warnedConnectFailed, 1) == 0)
            {
                _logger.LogError(ex,
                    "[Publish] Không kết nối được NATS; bỏ qua mọi publish cho tới khi kết nối lại được.");
            }

            return false;
        }

        if (_transport.IsConnected)
        {
            // Cho phép cảnh báo lại nếu sau này mất kết nối.
            Interlocked.Exchange(ref _warnedNotConnected, 0);
            Interlocked.Exchange(ref _warnedConnectFailed, 0);
            return true;
        }

        if (Interlocked.Exchange(ref _warnedNotConnected, 1) == 0)
        {
            _logger.LogWarning(
                "[Publish] Chưa khai Nats:Url trong Configuration/DataTransporter.json — " +
                "bỏ qua mọi publish. Dữ liệu vẫn được ghi DB bình thường.");
        }

        return false;
    }
}
