using SqlSugar;
using static Shared.QuestHa.Infrastructure.Services.Journal.Statements.QuestJournalParameter;
using Shared.QuestHa.Infrastructure.Services.Journal.Statements;
using Shared.QuestHa.Infrastructure.Services.Journal.Mapping;
using static Shared.QuestHa.Infrastructure.Services.Journal.Mapping.QuestJournalMapper;

namespace Shared.QuestHa.Infrastructure.Services.Journal;

/// <summary>
/// Author: KienLT
/// Description: Lưu trạng thái node, rào và chứng nhận phục hồi theo thế hệ bằng CAS.
/// Created date: 31/10/2026
/// </summary>
internal sealed class QuestJournalStatusService
{
    private readonly Func<CancellationToken, SqlSugarClient> _db;
    private readonly TimeProvider _clock;
    internal QuestJournalStatusService(Func<CancellationToken, SqlSugarClient> db, TimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    internal Task WriteStatusAsync(NodeStatusOutput s, CancellationToken ct = default)
    {
        return _db(ct).Ado.ExecuteCommandAsync(QuestJournalSql.WriteStatus,
            P("@node", s.Node), P("@state", s.State.ToString()), P("@paused", s.Paused),
            new SugarParameter("@fence", (object?)s.FenceReason ?? DBNull.Value, System.Data.DbType.String),
            P("@wal", s.WalSuspended), P("@lastSeq", s.LastSeq), P("@maxSeq", s.MaxSeq), P("@lagSeq", s.LagSeq),
            P("@lagSec", s.LagSeconds),
            new SugarParameter("@readySince", (object?)s.ReadySinceUtc ?? DBNull.Value, System.Data.DbType.DateTime2),
            new SugarParameter("@delivered", (object?)s.LastDeliveredUtc ?? DBNull.Value, System.Data.DbType.DateTime2),
            new SugarParameter("@err", (object?)Trim(s.LastError, 2000) ?? DBNull.Value, System.Data.DbType.String),
            new SugarParameter("@errUtc", (object?)s.LastErrorUtc ?? DBNull.Value, System.Data.DbType.DateTime2));
    }

    internal async Task<IReadOnlyList<NodeStatusOutput>> ReadStatusesAsync(CancellationToken ct = default)
    {
        var now = _clock.GetUtcNow().UtcDateTime;
        var rows = await _db(ct).Ado.SqlQueryAsync<StatusDb>(QuestJournalSql.ReadStatuses);
        return rows.Select(r => QuestJournalMapper.ToStatus(r, now)).ToList();
    }

    internal Task WriteReconcileResultAsync(string node, bool ok, CancellationToken ct = default)
    {
        return _db(ct).Ado.ExecuteCommandAsync(QuestJournalSql.WriteReconcile, P("@node", node), P("@ok", ok));
    }

    internal async Task<NodeRecoveryOutput> GetRecoveryAsync(string node, CancellationToken ct = default)
    {
        var rows = await _db(ct).Ado.SqlQueryAsync<RecoveryDb>(QuestJournalSql.GetRecovery,
            P("@node", node));
        var r = rows.Single();
        return QuestJournalMapper.ToRecovery(r);
    }

    internal async Task<long> BeginRecoveryAsync(string node, string by, CancellationToken ct = default)
    {
        var rows = await _db(ct).Ado.SqlQueryAsync<long>(QuestJournalSql.BeginRecovery,
            P("@node", node));
        return rows.Single();
    }

    internal async Task<bool> MarkRecoveryCoveredAsync(string node, long generation, CancellationToken ct = default)
    {
        // Không SET NOCOUNT ON: cần số dòng ảnh hưởng (CAS theo thế hệ + pha).
        var n = await _db(ct).Ado.ExecuteCommandAsync(
            QuestJournalSql.MarkRecoveryCovered,
            P("@node", node), P("@gen", generation));
        return n == 1;
    }

    internal async Task<bool> CompleteRecoveryAsync(string node, long generation, CancellationToken ct = default)
    {
        var n = await _db(ct).Ado.ExecuteCommandAsync(
            QuestJournalSql.CompleteRecovery,
            P("@node", node), P("@gen", generation));
        return n == 1;
    }

    internal Task SetManualFenceAsync(string node, bool fenced, string by, CancellationToken ct = default) =>
        _db(ct).Ado.ExecuteCommandAsync(QuestJournalSql.SetManualFence,
            P("@node", node), P("@fenced", fenced));

    internal async Task<bool> TryClearManualFenceAsync(string node, long generation, bool replayReady, CancellationToken ct = default)
        => await _db(ct).Ado.ExecuteCommandAsync(QuestJournalSql.TryClearManualFence, P("@node", node), P("@gen", generation), P("@ready", replayReady)) == 1;

    internal async Task<bool?> WriteReconcileResultAsync(string node, bool ok, long observedGeneration, DateTime readStartedSqlUtc, CancellationToken ct = default)
    {
        // MỘT câu UPDATE nguyên tử: điều kiện thế hệ/mốc + kiểm quarantine nằm trong chính câu ghi.
        // Quarantine chặn = mở, chưa Superseded, và có ít nhất một node (trong QuestNodeStatus) KHÔNG quarantine mở Seq đó
        // (Seq quarantine ở mọi node = lỗi payload toàn cục, không chặn).
        // Membership: reconciler chỉ persist true khi GetRecoveryAsync (EnsureRow, dòng không bao giờ bị xoá) của MỌI node cấu hình đã
        // thành công ⇒ QuestNodeStatus ⊇ tập node cấu hình; node thừa trong bảng chỉ làm điều kiện bảo thủ hơn.
        var rows = await _db(ct).Ado.SqlQueryAsync<int>(QuestJournalSql.CertifyReconcile,
            P("@node", node), P("@ok", ok), P("@gen", observedGeneration), P("@readStarted", readStartedSqlUtc));
        return rows.Count == 0 ? null : rows[0] == 1;
    }

}
