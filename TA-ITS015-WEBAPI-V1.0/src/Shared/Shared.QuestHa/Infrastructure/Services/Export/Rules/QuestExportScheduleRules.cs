using System.Globalization;

namespace Shared.QuestHa.Infrastructure.Services.Export.Rules;

/// <summary>
/// Author: KienLT
/// Description: Lịch xuất hằng ngày theo giờ server: từ giờ lịch trở đi, ngày cần xuất là HÔM QUA; thử lại theo RetryDelay, tối đa
/// MaxAttempts trong ngày; ngày mới thì đếm lại từ đầu. Ngày bị lỡ (hôm trước thử hết lượt) được lượt kiểm lại N ngày trước xuất bù.
/// Created date: 02/11/2026
/// </summary>
internal static class QuestExportScheduleRules
{
    /// <summary>Ngày cần xuất ở thời điểm <paramref name="nowLocal"/> (giờ server), null khi chưa tới giờ lịch.</summary>
    public static DateOnly? DueDay(DateTime nowLocal, string schedule)
    {
        var at = TimeOnly.ParseExact(schedule, "HH:mm", CultureInfo.InvariantCulture);
        return TimeOnly.FromDateTime(nowLocal) < at ? null : DateOnly.FromDateTime(nowLocal).AddDays(-1);
    }

    /// <summary>Được thử tiếp không: chưa xong, còn lượt, đã qua RetryDelay kể từ lần lỗi trước.</summary>
    public static bool ShouldAttempt(bool done, int attempts, DateTime? lastFailureUtc, DateTime nowUtc, int maxAttempts, TimeSpan retryDelay) =>
        !done && attempts < maxAttempts && (lastFailureUtc is null || nowUtc - lastFailureUtc.Value >= retryDelay);

    /// <summary>Các ngày kiểm lại trước <paramref name="day"/>: D-1 … D-<paramref name="recheckDays"/>.</summary>
    public static IReadOnlyList<DateOnly> RecheckDays(DateOnly day, int recheckDays) =>
        Enumerable.Range(1, Math.Max(0, recheckDays)).Select(i => day.AddDays(-i)).ToList();
}
