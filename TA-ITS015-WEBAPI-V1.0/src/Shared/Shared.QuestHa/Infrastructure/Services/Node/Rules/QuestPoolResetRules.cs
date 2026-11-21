namespace Shared.QuestHa.Infrastructure.Services.Node.Rules;

/// <summary>
/// Author: KienLT
/// Description: Giới hạn xoá pool từng node; lỗi lặp không làm đóng liên tục các kết nối đang dùng.
/// Created date: 30/10/2026
/// </summary>
internal static class QuestPoolResetRules
{
    /// <summary>Cho xoá lần đầu hoặc sau ít nhất một giây; đồng hồ lùi giữ chặn tới mốc cũ.</summary>
    internal static bool ShouldReset(DateTime? lastReset, DateTime now) =>
        lastReset is null || now - lastReset.Value >= TimeSpan.FromSeconds(1);
}
