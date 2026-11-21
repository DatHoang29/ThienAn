namespace Shared.QuestHa.Infrastructure.Services.Routing.Breaker;

/// <summary>
/// Author: KienLT
/// Description: Breaker cục bộ của 1 node — cùng semantics với
/// <c>src/Shared/Shared.Utility/Resilience/QuestAvailabilityGate.cs</c> (mở → lỗi kết nối ⇒ đóng tới hết
/// cooldown → cho ĐÚNG MỘT lượt thăm dò, các lượt khác bị bỏ ngay → thành công ⇒ mở). Viết lại trong Core
/// (không tham chiếu Shared.Utility) vì Shared.Utility kéo Furion vào lab; khác biệt duy nhất: mỗi node 1
/// instance thay vì singleton toàn tiến trình, và có <see cref="IsBlocked"/> để router chọn node mà không
/// tiêu suất thăm dò.
/// Created date: 09/10/2026
/// </summary>
internal sealed class QuestNodeBreaker
{
    private readonly object _sync = new();
    private bool _tripped;
    private bool _probeInFlight;
    private int _failures;
    private DateTime _retryAtUtc;

    public bool IsTripped
    {
        get { lock (_sync) return _tripped; }
    }

    public int ConsecutiveFailures
    {
        get { lock (_sync) return _failures; }
    }

    /// <summary>true = lúc này không được gọi node (đang nghỉ hoặc đã có lượt thăm dò chạy).</summary>
    public bool IsBlocked(DateTime nowUtc)
    {
        lock (_sync) return _tripped && (_probeInFlight || nowUtc < _retryAtUtc);
    }

    /// <summary>Xin phép gọi thật; nếu đang tripped và hết nghỉ thì lượt này là lượt thăm dò.</summary>
    public bool TryEnter(DateTime nowUtc)
    {
        lock (_sync)
        {
            if (!_tripped) return true;
            if (_probeInFlight || nowUtc < _retryAtUtc) return false;
            _probeInFlight = true;
            return true;
        }
    }

    /// <summary>Lượt gọi thành công ⇒ đóng breaker, xoá đếm lỗi.</summary>
    public void ReportSuccess()
    {
        lock (_sync)
        {
            _tripped = false;
            _probeInFlight = false;
            _failures = 0;
        }
    }

    /// <summary>Lỗi không phải kết nối: giữ trạng thái, nhả suất thăm dò.</summary>
    public void ReportUnrelatedFailure()
    {
        lock (_sync) _probeInFlight = false;
    }

    /// <summary>Lỗi kết nối ⇒ mở breaker trong <paramref name="cooldown"/> tính từ <paramref name="nowUtc"/>.</summary>
    public void ReportConnectivityFailure(DateTime nowUtc, TimeSpan cooldown)
    {
        lock (_sync)
        {
            _tripped = true;
            _probeInFlight = false;
            _failures++;
            _retryAtUtc = nowUtc + cooldown;
        }
    }
}
