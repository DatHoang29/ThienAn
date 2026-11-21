namespace Shared.QuestHa.Infrastructure.Services.Replication.Rules;

/// <summary>
/// Author: KienLT
/// Description: Quyết định trạng thái và lý do chuyển trạng thái từ ảnh cờ, journal và WAL.
/// Created date: 31/10/2026
/// </summary>
internal static class QuestReplicatorReadinessRules
{
    /// <summary>
    /// Author: KienLT
    /// Description: Ảnh cờ của replicator được lấy trong cùng khoá với snapshot.
    /// Created date: 31/10/2026
    /// </summary>
    internal readonly record struct Readiness(bool Connected, bool EverConnected, bool PauseRequested, bool RewindPending, string? RewindWhy,
        bool WalSuspended, bool WalConfirmed, DateTime? WalPendingSince, long Epoch, long WalConfirmedEpoch);

    internal static bool WalReady(Readiness flags, IReadOnlyDictionary<string, QuestWalProgressRules.Progress> progress, int tableCount, DateTime now, TimeSpan maxLag)
    {
        if (flags.WalSuspended) return false;
        var progressing = progress.Count == tableCount &&
            progress.Values.All(p => QuestWalProgressRules.IsReady(p, now, maxLag));
        return (flags.WalConfirmed && (flags.WalPendingSince is null || now - flags.WalPendingSince.Value <= maxLag)) || progressing;
    }

    internal static QuestNodeStateEnum State(NodeStatusOutput before, string? fence, Readiness flags, bool walReady, TimeSpan maxLag)
    {
        QuestNodeStateEnum state;
        if (fence is not null) state = QuestNodeStateEnum.Fenced;
        else if (!flags.Connected) state = flags.EverConnected || before.LastError is not null ? QuestNodeStateEnum.Down : QuestNodeStateEnum.Unknown;
        else if (flags.PauseRequested) state = QuestNodeStateEnum.CatchingUp;
        else if (flags.RewindPending) state = QuestNodeStateEnum.CatchingUp; // chưa rewind xong
        else if (!walReady) state = QuestNodeStateEnum.CatchingUp; // WAL chưa apply
        else if (before.LagSeq == 0 || before.LagSeconds <= maxLag.TotalSeconds) state = QuestNodeStateEnum.Ready;
        else state = QuestNodeStateEnum.CatchingUp;
        return state;
    }

    // mô tả nguyên nhân quyết định sẵn có, không thay ngưỡng Ready hoặc rào coverage.
    internal static string StateReason(NodeStatusOutput status, Readiness flags, bool walReady, IEnumerable<string> tableNames, IReadOnlyDictionary<string, QuestWalProgressRules.Progress> walProgress, DateTime now, TimeSpan maxLag)
    {
        if (status.FenceReason is not null) return status.FenceReason;
        if (!flags.Connected) return "mất kết nối node hoặc chưa liên lạc được";
        if (flags.PauseRequested) return "replicator đang pause";
        if (flags.RewindPending) return $"chờ rewind ({flags.RewindWhy}), epoch={flags.Epoch}";
        if (!walReady)
        {
            foreach (var table in tableNames)
            {
                if (!walProgress.TryGetValue(table, out var progress))
                    return $"chưa có probe WAL {table} cho epoch={flags.Epoch}";
                if (!progress.Valid) return $"probe WAL {table} không hợp lệ (thiếu bảng/suspended/txn âm)";
                if (!progress.Confirmed) return $"WAL {table} chưa chứng minh apply sau reconnect/txn lùi, epoch={flags.Epoch}";
                if (now - progress.ProbedAt > maxLag)
                    return $"probe WAL {table} cũ {(now - progress.ProbedAt).TotalSeconds:0.###} s";
                if (progress.Pending.Count > 0 && now - progress.Pending[0].Since > maxLag)
                    return $"WAL chưa áp bảng {table}, tuổi {(now - progress.Pending[0].Since).TotalSeconds:0.###} s, checkpoints={progress.Pending.Count}";
            }
            return $"chưa xác nhận WAL cho epoch={flags.Epoch}, confirmedEpoch={flags.WalConfirmedEpoch}";
        }
        if (status.State == QuestNodeStateEnum.Ready)
            return $"journal trong ngưỡng (lag={status.LagSeq}, tuổi={status.LagSeconds:0.###} s), WAL đủ bằng chứng, epoch={flags.Epoch}";
        return $"journal lag={status.LagSeq}, tuổi={status.LagSeconds:0.###} s > ReadyMaxLag={maxLag.TotalSeconds:0.###} s";
    }

}
