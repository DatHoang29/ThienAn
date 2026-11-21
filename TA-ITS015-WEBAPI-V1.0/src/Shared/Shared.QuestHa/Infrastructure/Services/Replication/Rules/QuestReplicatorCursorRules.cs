namespace Shared.QuestHa.Infrastructure.Services.Replication.Rules;

/// <summary>
/// Author: KienLT
/// Description: Chọn mốc SafeSeq theo tuổi giao và lý do rewind, không sửa lịch sử caller.
/// Created date: 31/10/2026
/// </summary>
internal static class QuestReplicatorCursorRules
{
    internal static (long SafeSeq, int RemoveCount) SafeSequence(IEnumerable<(DateTime At, long Seq)> history, DateTime now, TimeSpan rewindOnReconnect)
    {
        var limit = now - rewindOnReconnect;
        long safe = -1;
        var eligible = 0;
        foreach (var entry in history)
        {
            if (entry.At > limit) break;
            safe = entry.Seq;
            eligible++;
        }
        return (safe, Math.Max(0, eligible - 1));
    }

    internal static string ReconnectReason(bool firstContact) =>
        firstContact ? "lần liên lạc đầu tiên của worker" : "node kết nối lại";
}
