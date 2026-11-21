namespace Shared.QuestHa.Exceptions;

/// <summary>
/// Author: KienLT
/// Description: Đường đọc có chứng nhận coverage (<c>QuestHaManager.ReadAsync</c>) không trả được dữ liệu đủ: journal SQL lỗi,
/// không node nào phủ head đã chốt, hoặc không còn node Ready. Host dịch sang lỗi "tạm thời, thử lại sau" của mình
/// (Worker/WebAPI: <c>QuestDbUnavailableException</c> → 503 + Retry-After). Shared.QuestHa không tham chiếu Shared.Utility
/// nên dùng lớp riêng. CỐ Ý không có inner exception: router không được nhầm lỗi SQL journal là lỗi kết nối node.
/// Created date: 30/10/2026
/// </summary>
public sealed class QuestHaUnavailableException : Exception
{
    /// <summary>Mã lỗi khi không còn node Ready để đọc.</summary>
    public const string NoNodeCode = "QuestHa";

    /// <summary>Mã lỗi khi không node nào phủ head đã chốt trong ngân sách CoverageWait.</summary>
    public const string NotCoveredCode = "QuestHaNotCovered";

    /// <summary>Mã lỗi khi journal SQL (head/coverage) không đọc được.</summary>
    public const string JournalUnavailableCode = "QuestHaJournalUnavailable";

    /// <summary>Tạo lỗi với mã, thời gian nên chờ và số lỗi liên tiếp (giữ đúng mã lỗi, thời gian chờ và bộ đếm của Worker).</summary>
    public QuestHaUnavailableException(string code, TimeSpan retryAfter, int consecutiveFailures)
        : base($"QuestHa read unavailable ({code}); retry after {retryAfter.TotalSeconds:0}s.")
    {
        Code = code;
        RetryAfter = retryAfter;
        ConsecutiveFailures = consecutiveFailures;
    }

    /// <summary>Mã lỗi: <see cref="NoNodeCode"/>, <see cref="NotCoveredCode"/> hoặc <see cref="JournalUnavailableCode"/>.</summary>
    public string Code { get; }

    /// <summary>Thời gian nên chờ trước khi thử lại.</summary>
    public TimeSpan RetryAfter { get; }

    /// <summary>Số lỗi liên tiếp báo cho host (0 khi chỉ thiếu coverage, không phải lỗi kết nối).</summary>
    public int ConsecutiveFailures { get; }
}
