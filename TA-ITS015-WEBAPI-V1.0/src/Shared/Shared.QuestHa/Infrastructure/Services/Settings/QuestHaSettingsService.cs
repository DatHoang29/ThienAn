using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.QuestHa.Infrastructure.Services.Settings.Rules;

namespace Shared.QuestHa.Infrastructure.Services.Settings;

/// <summary>
/// Author: KienLT
/// Description: Snapshot tham số vận hành dùng chung của một host. Service đọc <see cref="Current"/> MỘT LẦN đầu mỗi vòng/lượt
/// (health, bảo trì journal, lịch backup, router, đọc có coverage) thay vì chụp options lúc khởi tạo. Đổi bằng thay nguyên
/// object (<see cref="Volatile.Write{T}"/>) nên không ai thấy nửa bộ cũ nửa bộ mới; vòng đang chạy giữ bộ đã chụp tới hết vòng.
/// Thứ tự "đọc nguồn → áp RAM" giữa nhiều luồng (khởi động, hook sau lưu, refresh định kỳ/NATS) giữ bằng vé đọc đơn điệu:
/// bên đọc lấy vé TRƯỚC khi truy vấn, kết quả mang vé nhỏ hơn vé của bộ đang chạy bị bỏ (lượt đọc sau luôn thấy dữ liệu mới hơn).
/// Created date: 31/10/2026
/// </summary>
internal sealed class QuestHaSettingsService
{
    private readonly QuestHaOptions _baseline;
    private readonly ILogger _logger;
    private readonly object _applyLock = new();
    private QuestHaOptions _current;
    private long _issuedTicket;
    // Vé của bộ đang chạy; 0 = cấu hình khởi động. Chỉ đổi trong _applyLock.
    private long _appliedTicket;

    /// <summary><paramref name="baseline"/> = cấu hình khởi động (appsettings); bộ đang chạy ban đầu chính là nó.</summary>
    public QuestHaSettingsService(QuestHaOptions baseline, ILogger<QuestHaSettingsService>? logger = null)
    {
        _baseline = baseline ?? throw new ArgumentNullException(nameof(baseline));
        _current = baseline;
        _logger = logger ?? (ILogger)NullLogger.Instance;
    }

    /// <summary>Bộ đang chạy. Đọc một lần vào biến cục bộ cho cả vòng; hai lần đọc có thể rơi vào hai bộ khác nhau.</summary>
    public QuestHaOptions Current => Volatile.Read(ref _current);

    /// <summary>Phát vé đọc tăng dần; bên gọi lấy vé ngay TRƯỚC khi truy vấn nguồn cấu hình rồi trả lại cùng kết quả cho <see cref="Apply(QuestHaRuntimeOptions, long, out bool)"/>.</summary>
    public long BeginRead() => Interlocked.Increment(ref _issuedTicket);

    /// <summary>Áp gói coi như vừa đọc xong (vé mới nhất). Xem <see cref="Apply(QuestHaRuntimeOptions, long, out bool)"/>.</summary>
    public bool Apply(QuestHaRuntimeOptions runtime) => Apply(runtime, BeginRead(), out _);

    /// <summary>
    /// Gộp <paramref name="runtime"/> lên cấu hình khởi động (thuộc tính null ⇒ giá trị khởi động), kiểm cả gói rồi thay snapshot.
    /// <paramref name="readTicket"/> nhỏ hơn vé của bộ đang chạy ⇒ kết quả đọc cũ: bỏ, <paramref name="superseded"/> = true, trả false.
    /// Một giá trị sai ⇒ từ chối CẢ gói, giữ bộ đang chạy, log Warning và trả false. Thành công ⇒ log Information các khoá đổi.
    /// Vé chưa từng phát ⇒ <see cref="ArgumentOutOfRangeException"/>.
    /// </summary>
    public bool Apply(QuestHaRuntimeOptions runtime, long readTicket, out bool superseded)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        if (readTicket <= 0 || readTicket > Volatile.Read(ref _issuedTicket))
            throw new ArgumentOutOfRangeException(nameof(readTicket), readTicket, "Vé đọc chưa được phát (gọi BeginRead trước khi truy vấn).");
        lock (_applyLock)
        {
            superseded = readTicket < _appliedTicket;
            if (superseded)
            {
                _logger.LogDebug("[QuestHa] Bỏ kết quả đọc tham số vận hành cũ (vé {Ticket} < vé đang chạy {Applied}).", readTicket, _appliedTicket);
                return false;
            }

            var errors = QuestHaSettingsRules.ValidateEffective(_baseline, runtime, out var candidate);
            if (candidate is null)
            {
                _logger.LogWarning("[QuestHa] Từ chối gói tham số vận hành, giữ bộ đang chạy: {Errors}", string.Join("; ", errors));
                return false;
            }

            var changes = QuestHaSettingsRules.Diff(QuestHaSettingsRules.FromOptions(Current), QuestHaSettingsRules.FromOptions(candidate));
            _appliedTicket = readTicket;
            Volatile.Write(ref _current, candidate);
            if (changes.Count > 0)
                _logger.LogInformation("[QuestHa] Áp tham số vận hành (hiệu lực từ vòng sau): {Changes}", string.Join("; ", changes));
            else
                _logger.LogDebug("[QuestHa] Gói tham số vận hành không đổi giá trị nào.");
            return true;
        }
    }
}
