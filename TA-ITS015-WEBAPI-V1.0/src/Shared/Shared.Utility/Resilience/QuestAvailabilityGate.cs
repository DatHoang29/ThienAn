using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace Shared.Utility.Resilience;

/// <summary>
/// Author: KienLT
/// Description: Ném khi cửa QuestDB đang ĐÓNG — lượt gọi bị bỏ NGAY, không chạm tới socket.
///
/// Đây là lỗi "tạm thời, thử lại sau", không phải lỗi dữ liệu: mọi chỗ bắt nó chỉ cần log và để lượt sau
/// làm lại. Riêng đường ingestion thì cứ để ném ra để handler đánh dấu gap rồi backfill bù về sau.
///
/// <para><b>Vị trí dùng chung.</b> Đi qua ba chỗ: <c>Services.Vds/Shared/Persistence</c> (chỉ worker) →
/// <c>Modules.VDS.Core/Persistence</c> (worker + WebAPI) → <c>Shared.Utility/Resilience</c> (23/09/2026).
/// Lần chuyển cuối là vì lớp này KHÔNG có gì của VDS: chỉ <c>System.Net.Sockets</c> và
/// <c>Microsoft.Extensions.Logging</c>. Để nó trong Core của một module nghĩa là mọi project muốn dùng cầu
/// dao đều phải tham chiếu cả module VDS — và đó là lý do <c>Modules.TMS</c> phải kéo
/// <c>Modules.VDS.Core</c> vào.</para>
/// Created date: 26/08/2026 (Core 03/09/2026, Shared.Utility 23/09/2026)
/// </summary>
public sealed class QuestDbUnavailableException : Exception
{
    public QuestDbUnavailableException(string connectionId, TimeSpan retryAfter, int consecutiveFailures)
        : base($"QuestDB '{connectionId}' đang được coi là không dùng được " +
               $"({consecutiveFailures} lần lỗi kết nối liên tiếp); bỏ lượt này, thử lại sau " +
               $"{retryAfter.TotalSeconds:0}s.")
    {
        ConnectionId = connectionId;
        RetryAfter = retryAfter;
        ConsecutiveFailures = consecutiveFailures;
    }

    public string ConnectionId { get; }

    public TimeSpan RetryAfter { get; }

    public int ConsecutiveFailures { get; }
}

/// <summary>
/// Author: KienLT
/// Description: Cầu dao (circuit breaker) cho MỌI lượt gọi QuestDB.
///
/// <para><b>Vì sao phải có.</b> Đo được ngày 26/08/2026 với máy chủ QuestDB không tới được: một lượt gọi
/// qua SqlSugar chặn <b>5 phút 16 giây</b> rồi mới báo lỗi, dù connection string khai <c>Timeout=30</c> và
/// lời gọi có <see cref="CancellationToken"/> hết hạn sau 10 giây. Nguyên nhân đọc từ stack:
/// <c>QuestDBProvider.CheckConnection()</c> gọi <c>NpgsqlConnection.Open()</c> bản ĐỒNG BỘ nên không nhận
/// token, và hạn kết nối của Npgsql cũng không chặn được lượt mở đó. Không có cầu dao thì QuestDB xuống
/// đồng nghĩa: mỗi lượt gọi giữ luồng 5 phút — worker thì nghẽn queue tuần tự, WebAPI thì cạn thread pool
/// và kéo sập cả API không liên quan.</para>
///
/// <para><b>Chỉ ĐÓNG cửa khi lỗi là lỗi KẾT NỐI.</b> Lỗi câu lệnh (thiếu bảng, sai kiểu, sai SQL) phải
/// tiếp tục nổi lên ở từng lượt gọi — chặn hết sau một câu SQL sai là che mất lỗi thật và làm hỏng cả
/// những tính năng đang chạy đúng.</para>
///
/// <para>Trạng thái: <b>mở</b> (bình thường, cho đi) → lỗi kết nối ⇒ <b>đóng</b> (bỏ ngay mọi lượt tới
/// hạn nghỉ) → hết hạn nghỉ ⇒ cho ĐÚNG MỘT lượt đi thăm dò (các lượt khác vẫn bị bỏ, kể cả khi lượt thăm
/// dò treo hàng phút) → thăm dò thành công ⇒ mở lại.</para>
///
/// Singleton toàn tiến trình: nó mô tả một sự thật duy nhất — máy chủ QuestDB có tới được hay không — nên
/// mọi repository phải dùng chung một cầu dao.
/// Created date: 26/08/2026 (Core 03/09/2026, Shared.Utility 23/09/2026)
/// </summary>
public sealed class QuestAvailabilityGate
{
    private readonly object _sync = new();
    private readonly ILogger<QuestAvailabilityGate> _logger;

    private bool _closed;
    private bool _probeInFlight;
    private int _consecutiveFailures;
    private DateTime _retryAtUtc;

    public QuestAvailabilityGate(ILogger<QuestAvailabilityGate> logger)
    {
        _logger = logger;
    }

    /// <summary>true = cửa đang đóng, mọi lượt gọi bị bỏ ngay.</summary>
    public bool IsClosed
    {
        get { lock (_sync) return _closed; }
    }

    /// <summary>
    /// Xin phép gọi thật.
    /// </summary>
    /// <returns>true = được đi. false = bỏ lượt này, <paramref name="retryAfter"/> là thời gian còn lại.</returns>
    public bool TryEnter(DateTime nowUtc, out TimeSpan retryAfter, out int consecutiveFailures)
    {
        lock (_sync)
        {
            consecutiveFailures = _consecutiveFailures;
            retryAfter = TimeSpan.Zero;

            if (!_closed) return true;

            // Đang có lượt thăm dò chạy: các lượt khác phải bỏ, KHÔNG được xếp hàng chờ. Lượt thăm dò có
            // thể treo hàng phút, xếp hàng là quay lại đúng cái treo mà cầu dao muốn tránh.
            if (_probeInFlight || nowUtc < _retryAtUtc)
            {
                var left = _retryAtUtc - nowUtc;
                retryAfter = left > TimeSpan.Zero ? left : TimeSpan.Zero;
                return false;
            }

            _probeInFlight = true;
            return true;
        }
    }

    /// <summary>Lượt gọi thành công ⇒ mở cửa.</summary>
    public void ReportSuccess()
    {
        bool recovered;
        int failures;

        lock (_sync)
        {
            recovered = _closed;
            failures = _consecutiveFailures;
            _closed = false;
            _probeInFlight = false;
            _consecutiveFailures = 0;
        }

        // Log NGOÀI lock: NLog ghi file, giữ lock trong lúc ghi là chặn cả đường dữ liệu.
        if (recovered)
        {
            _logger.LogInformation(
                "[QuestDb] Đã dùng lại được sau {Failures} lần lỗi kết nối liên tiếp.", failures);
        }
    }

    /// <summary>
    /// Lượt gọi lỗi nhưng KHÔNG phải lỗi kết nối (SQL sai, thiếu bảng, huỷ theo yêu cầu): giữ nguyên
    /// trạng thái cửa, chỉ nhả suất thăm dò.
    /// </summary>
    public void ReportUnrelatedFailure()
    {
        lock (_sync) _probeInFlight = false;
    }

    /// <summary>Lỗi kết nối ⇒ đóng cửa tới <c>nowUtc + cooldown</c>.</summary>
    public void ReportConnectivityFailure(DateTime nowUtc, TimeSpan cooldown)
    {
        bool justClosed;
        int failures;

        lock (_sync)
        {
            justClosed = !_closed;
            _closed = true;
            _probeInFlight = false;
            _consecutiveFailures++;
            failures = _consecutiveFailures;
            _retryAtUtc = nowUtc + cooldown;
        }

        if (justClosed)
        {
            _logger.LogError(
                "[QuestDb] Coi như KHÔNG dùng được — bỏ mọi lượt gọi trong {Cooldown}s tới rồi thăm dò " +
                "lại. Không chặn ở đây thì mỗi lượt gọi treo vài phút.",
                cooldown.TotalSeconds);
        }
        else
        {
            _logger.LogWarning(
                "[QuestDb] Thăm dò lại vẫn lỗi ({Failures} lần liên tiếp); nghỉ thêm {Cooldown}s.",
                failures, cooldown.TotalSeconds);
        }
    }

    /// <summary>
    /// Ngoại lệ này có phải "không nối được máy chủ" hay không.
    ///
    /// <para><c>InvalidOperationException("Connection is not open")</c> nằm trong danh sách vì đó chính là
    /// câu mà SqlSugar/Npgsql trả về khi máy chủ QuestDB không tới được: <c>QuestDBProvider</c> nuốt lỗi
    /// của lượt <c>Open()</c> rồi để <c>NpgsqlCommand</c> báo lại là kết nối chưa mở. Đo được 26/08/2026;
    /// không nhận diện câu này thì cầu dao gần như không bao giờ đóng.</para>
    /// </summary>
    public static bool IsConnectivityFailure(Exception? ex)
    {
        for (var e = ex; e is not null; e = e.InnerException)
        {
            if (e is SocketException or TimeoutException or IOException) return true;

            if (e is InvalidOperationException
                && e.Message.Contains("Connection is not open", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
