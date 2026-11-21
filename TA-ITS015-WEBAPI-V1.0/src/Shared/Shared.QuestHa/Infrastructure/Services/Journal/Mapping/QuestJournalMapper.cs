namespace Shared.QuestHa.Infrastructure.Services.Journal.Mapping;

/// <summary>
/// Author: KienLT
/// Description: Ánh xạ dữ liệu journal bằng hàm thuần; giờ đo SQL quy về đồng hồ caller.
/// Created date: 31/10/2026
/// </summary>
internal static class QuestJournalMapper
{
    internal static DateTime Utc(DateTime d) => DateTime.SpecifyKind(d, DateTimeKind.Utc);

    internal static DateTime? Utc(DateTime? d) => d is null ? null : Utc(d.Value);

    internal static JournalRowOutput ToRow(OutboxDb r) => new(
        r.Seq, r.TargetTable, r.EventKey, Utc(r.DesignatedTs), r.Payload, r.SchemaVer, r.Version, r.Superseded, Utc(r.CreatedUtc));

    // map cờ retired tường minh, giữ constructor/deconstruct tám trường.
    internal static BackupRecordOutput ToBackup(BackupDb r) => new(
        r.BackupId, r.Node, r.WatermarkSeq, Utc(r.WatermarkTs), r.Path, r.SizeBytes, r.Verified, Utc(r.CreatedUtc)) { RetiredUtc = Utc(r.RetiredUtc) };

    internal static JournalStatsOutput ToStats(StatsDb head, IReadOnlyList<CursorDb> cursors)
    {
        var byNode = cursors.ToDictionary(c => c.Node, c => c.LastSeq, StringComparer.OrdinalIgnoreCase);
        var pending = cursors.ToDictionary(c => c.Node, c => Math.Max(0, head.MaxSeq - c.LastSeq), StringComparer.OrdinalIgnoreCase);
        return new JournalStatsOutput(head.MaxSeq, head.TotalRows, head.QuarantineCount, byNode, pending);
    }

    internal static NodeStatusOutput ToStatus(StatusDb r, DateTime now) => new NodeStatusOutput
        {
            Node = r.Node,
            State = Enum.TryParse<QuestNodeStateEnum>(r.State, out var st) ? st : QuestNodeStateEnum.Unknown,
            Paused = r.Paused,
            FenceReason = r.FenceReason,
            WalSuspended = r.WalSuspended,
            LastSeq = r.LastSeq,
            MaxSeq = r.MaxSeq,
            LagSeq = r.LagSeq,
            LagSeconds = r.LagSeconds,
            // Quy đổi về đồng hồ local bằng TUỔI (đo trên đồng hồ SQL) ⇒ không phụ thuộc lệch giờ giữa các máy.
            HeartbeatUtc = r.HeartbeatAgeMs is null ? DateTime.MinValue : now - TimeSpan.FromMilliseconds(r.HeartbeatAgeMs.Value),
            ReadySinceUtc = Utc(r.ReadySinceUtc),
            LastDeliveredUtc = Utc(r.LastDeliveredUtc),
            LastError = r.LastError,
            LastErrorUtc = Utc(r.LastErrorUtc),
            LastReconcileOk = r.LastReconcileOk,
            LastReconcileUtc = r.ReconcileAgeMs is null ? null : now - TimeSpan.FromMilliseconds(r.ReconcileAgeMs.Value),
        };

    internal static QuarantineRowOutput ToQuarantine(QuarantineDb r) => new QuarantineRowOutput(r.Seq, r.Node, r.Error, Utc(r.FirstSeenUtc), Utc(r.LastSeenUtc), r.Attempts,
            r.ResolvedUtc is { } resolved ? Utc(resolved) : null);

    internal static NodeRecoveryOutput ToRecovery(RecoveryDb r) => new NodeRecoveryOutput(r.RecoveryGeneration, (NodeRecoveryPhaseEnum)r.RecoveryPhase, Utc(r.RecoveryCoveredUtc),
            r.ReconciledGeneration, Utc(r.LastReconcileUtc), r.LastReconcileOk, r.ManualFence, Utc(r.SqlNowUtc));

    internal static NodeCursorOutput ToCursor(CursorDb r) => new(r.LastSeq, r.SafeSeq);

    internal static NodeLagOutput ToLag(LagDb r) => new(r.MaxSeq, r.Pending, Math.Max(0, r.AgeSeconds));

    internal static OpenQuarantineOutput ToOpenQuarantine(OpenQuarantineDb r) => new(r.Seq, r.Node, r.Attempts, r.Superseded);

    internal static NodeReadCoverageOutput ToCoverage(CoverageDb? row) => row is null ? new(0, NodeRecoveryPhaseEnum.Restoring, true, -1, null) :
            new(row.Generation, (NodeRecoveryPhaseEnum)row.Phase, row.ManualFence, row.LastSeq, row.FirstQuarantinedSeq);

    internal static JournalAppendOutput ToAppend(AckDb r, JournalAppendInput entry) => new(r.Seq, entry.TargetTable, entry.EventKey,
        QuestPayloadHelper.NormalizeTs(entry.DesignatedTsUtc), entry.Version, r.Superseded, Utc(r.CreatedUtc));

    internal static JournalAppendOutput ToBatchAppend(BatchAckDb r, JournalAppendInput e, DateTime normalized) => new(r.Seq, e.TargetTable, e.EventKey, normalized, e.Version, r.Superseded, Utc(r.CreatedUtc));

    internal static JournalBacklogOutput ToBacklog(BacklogHeaderDb head, IReadOnlyList<NodeBacklogDb> rows, long pendingCountLimit) =>
        new(Utc(head.MeasuredUtc), head.TotalRows, head.JournalBytes, head.OpenQuarantineCount,
                rows.Select(r => new JournalNodeBacklogOutput(r.Node, r.CursorExists, r.LastSeq, r.SafeSeq,
                    r.PendingRows, Utc(r.OldestPendingCreatedUtc), TimeSpan.FromSeconds(Math.Max(0, r.AgeSeconds)))
                    { PendingRowsLowerBound = r.PendingRows >= pendingCountLimit }).ToArray(),
                JournalBacklogLevelEnum.Ok, Array.Empty<string>());
}
