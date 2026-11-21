using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using SqlSugar;
using static Shared.QuestHa.Infrastructure.Services.Journal.Statements.QuestJournalParameter;
using Shared.QuestHa.Infrastructure.Services.Journal.Statements;
using Shared.QuestHa.Infrastructure.Services.Journal.Mapping;
using static Shared.QuestHa.Infrastructure.Services.Journal.Mapping.QuestJournalMapper;

namespace Shared.QuestHa.Infrastructure.Services.Journal;

/// <summary>
/// Author: KienLT
/// Description: Đo backlog, dọn journal và gỡ cursor trên cùng phiên/transaction bảo vệ.
/// Created date: 31/10/2026
/// </summary>
internal sealed class QuestJournalPurgeService
{
    private readonly string _connectionString;
    private readonly int _timeoutSeconds;
    private readonly Func<CancellationToken, SqlSugarClient> _db;
    internal QuestJournalPurgeService(string connectionString, int timeoutSeconds, Func<CancellationToken, SqlSugarClient> db)
    {
        _connectionString = connectionString;
        _timeoutSeconds = timeoutSeconds;
        _db = db;
    }

    // Không dùng applock QuestOutbox.Append và không sửa schema/IDENTITY.
    internal Task<JournalBacklogOutput> ReadJournalBacklogAsync(IReadOnlyList<string> nodes, CancellationToken ct = default) =>
        ReadJournalBacklogAsync(nodes, 1_000_001, ct);

    internal async Task<JournalBacklogOutput> ReadJournalBacklogAsync(IReadOnlyList<string> nodes, long pendingCountLimit,
        CancellationToken ct = default)
    {
        ValidateMaintenanceNodes(nodes);
        if (pendingCountLimit < 1) throw new ArgumentOutOfRangeException(nameof(pendingCountLimit));
        try
        {
            ct.ThrowIfCancellationRequested();
            using var db = _db(ct);
            var headers = await db.Ado.SqlQueryAsync<BacklogHeaderDb>(QuestJournalSql.BacklogHeader);
            ct.ThrowIfCancellationRequested();
            var head = headers.Single();
            // LastSeq+1 có thể là lỗ IDENTITY do rollback: lấy dòng thực tế đầu tiên sau LastSeq.
            var rows = await db.Ado.SqlQueryAsync<NodeBacklogDb>(QuestJournalSql.BacklogNodes, P("@nodes", JsonSerializer.Serialize(nodes)), P("@limit", pendingCountLimit));
            // SqlSugar có thể kết thúc truy vấn huỷ bằng tập rỗng thay vì ném lỗi.
            ct.ThrowIfCancellationRequested();
            return QuestJournalMapper.ToBacklog(head, rows, pendingCountLimit);
        }
        catch (SqlException ex) when (ct.IsCancellationRequested)
        {
            throw new OperationCanceledException("Journal backlog measurement cancelled.", ex, ct);
        }
    }

    internal async Task<JournalPurgeOutput> PurgeJournalAsync(IReadOnlyList<string> nodes, JournalMaintenanceOptions options,
        bool dryRun = true, CancellationToken ct = default)
    {
        options.Validate();
        ValidateMaintenanceNodes(nodes);
        if (nodes.Count == 0) return new(dryRun, 0, 0, false, null, "NoAssignedNodes");
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(ct);
        budget.CancelAfter(options.MaxRunDuration);
        var token = budget.Token;
        long rows = 0, after = 0;
        var batches = 0;
        JournalPurgeBoundaryOutput? boundary = null;
        try
        {
            // Cùng kết nối giữ Session lock và thực thi DELETE: mất phiên thì không thể tiếp tục xoá.
            var builder = new SqlConnectionStringBuilder(_connectionString) { Pooling = false, ConnectRetryCount = 0 };
            await using var connection = new SqlConnection(builder.ConnectionString);
            await connection.OpenAsync(token).ConfigureAwait(false);
            await using (var acquire = MaintenanceCommand(connection, null, QuestJournalSql.PurgeAcquireLock))
            {
                var code = Convert.ToInt32(await acquire.ExecuteScalarAsync(token).ConfigureAwait(false));
                if (code < 0) return new(dryRun, 0, 0, false, null, "PurgeAlreadyRunningOrLockUnavailable");
            }
            while (true)
            {
                token.ThrowIfCancellationRequested();
                // đọc mốc sơ bộ và TOP theo Seq ngoài transaction DELETE.
                // CreatedUtc sinh trong applock Append nên tăng theo Seq. Nếu đồng hồ SQL lùi hoặc dữ liệu
                // được sửa tay không đơn điệu, dừng ở dòng trẻ chỉ trì hoãn dọn, không cho phép xoá dòng trẻ.
                boundary = await ReadPurgeBoundaryAsync(connection, null, nodes, options, token).ConfigureAwait(false);
                if (boundary.MaxSeq is null)
                    return new(dryRun, rows, batches, false, boundary, boundary.BlockedReason);
                var probe = rows >= options.MaxRowsPerRun;
                var take = probe ? options.BatchSize : Math.Min(options.BatchSize, options.MaxRowsPerRun - (int)rows);
                var candidates = new List<long>();
                long? youngSeq = null;
                var finished = false;
                var scanned = 0;
                await using (var read = MaintenanceCommand(connection, null, QuestJournalSql.PurgeCandidates))
                {
                    read.Parameters.Add("@take", SqlDbType.Int).Value = take;
                    read.Parameters.Add("@after", SqlDbType.BigInt).Value = after;
                    read.Parameters.Add("@max", SqlDbType.BigInt).Value = boundary.MaxSeq.Value;
                    await using var reader = await read.ExecuteReaderAsync(token).ConfigureAwait(false);
                    while (await reader.ReadAsync(token).ConfigureAwait(false))
                    {
                        scanned++;
                        if (reader.GetDateTime(1) >= boundary.RetentionBeforeUtc || !reader.GetBoolean(2))
                        {
                            if (reader.GetDateTime(1) >= boundary.RetentionBeforeUtc) youngSeq = reader.GetInt64(0);
                            finished = true;
                            break;
                        }
                        candidates.Add(reader.GetInt64(0));
                    }
                }
                finished |= scanned < take;
                // probe hữu hạn sau dòng trẻ, trong đúng mốc cursor/backup;
                // không quét cả cửa sổ trẻ hoặc giữ khoá cursor chỉ để chẩn đoán đồng hồ bất thường.
                var stopReason = youngSeq is { } blockingSeq
                    ? await ReadNonMonotonicReasonAsync(connection, blockingSeq, boundary, token).ConfigureAwait(false) : null;
                if (candidates.Count == 0) return new(dryRun, rows, batches, false, boundary, stopReason);

                await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(token).ConfigureAwait(false);
                // Chuẩn bị đúng danh sách được xoá TRƯỚC khi giữ cursor/catalog. Khoá từng dòng outbox
                // và dải quarantine giữ CreatedUtc/RowVer/trạng thái cách ly không đổi tới COMMIT.
                // Việc compile/đọc index của bước này không được chặn replicator cập nhật cursor.
                var ready = new List<long>();
                // Export bật: thêm luật từng dòng (backup lạnh hoặc bản xuất Verified); cờ Exported cho biết dòng được bản xuất bao.
                var exportRule = boundary.ColdBackupMaxSeq is not null;
                var exported = new HashSet<long>();
                await using (var select = MaintenanceCommand(connection, transaction,
                    exportRule ? QuestJournalSql.PurgeReadyRowsExport : QuestJournalSql.PurgeReadyRows))
                {
                    select.Parameters.Add("@seqs", SqlDbType.NVarChar, -1).Value = JsonSerializer.Serialize(candidates);
                    select.Parameters.Add("@cutoff", SqlDbType.DateTime2).Value = boundary.RetentionBeforeUtc;
                    if (exportRule) select.Parameters.Add("@cold", SqlDbType.BigInt).Value = boundary.ColdBackupMaxSeq!.Value;
                    await using var reader = await select.ExecuteReaderAsync(token).ConfigureAwait(false);
                    while (await reader.ReadAsync(token).ConfigureAwait(false))
                    {
                        ready.Add(reader.GetInt64(0));
                        if (exportRule && reader.GetBoolean(1)) exported.Add(reader.GetInt64(0));
                    }
                }
                // Shared HOLDLOCK bảo vệ mọi cursor (kể cả insert ngoài cấu hình) và catalog tới COMMIT.
                // Chỉ giữ chúng quanh kiểm mốc + DELETE theo PK; không còn truy vấn lọc tuổi/quarantine ở đây.
                boundary = await ReadPurgeBoundaryAsync(connection, transaction, nodes, options, token).ConfigureAwait(false);
                if (boundary.MaxSeq is null)
                    return new(dryRun, rows, batches, false, boundary, boundary.BlockedReason);
                ready.RemoveAll(seq => seq > boundary.MaxSeq.Value);
                // Backup lạnh có thể đã retire giữa hai lần đọc: dòng không có bản xuất bao phải nằm dưới mốc lạnh đã khoá.
                if (exportRule) ready.RemoveAll(seq => !exported.Contains(seq) && seq > (boundary.ColdBackupMaxSeq ?? -1));
                long count = ready.Count;
                if (!dryRun && !probe && ready.Count > 0)
                {
                    await using var delete = MaintenanceCommand(connection, transaction, QuestJournalSql.PurgeDelete);
                    delete.Parameters.Add("@seqs", SqlDbType.NVarChar, -1).Value = JsonSerializer.Serialize(ready);
                    count = Convert.ToInt64(await delete.ExecuteScalarAsync(token).ConfigureAwait(false));
                }
                await transaction.CommitAsync(token).ConfigureAwait(false);
                if (probe && count > 0) return new(dryRun, rows, batches, true, boundary, "RowBudget");
                if (!probe) { rows += count; batches++; }
                after = candidates[^1]; // Bao gồm dòng quarantine: không lặp vô hạn trên dòng được bảo vệ.
                if (finished || after >= boundary.MaxSeq.Value) return new(dryRun, rows, batches, false, boundary, stopReason);
                await Task.Yield();
            }
        }
        catch (SqlException ex) when (ct.IsCancellationRequested)
        {
            throw new OperationCanceledException("Journal purge cancelled.", ex, ct);
        }
        catch (OperationCanceledException ex) when (ct.IsCancellationRequested)
        {
            throw new OperationCanceledException("Journal purge cancelled.", ex, ct);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested && budget.IsCancellationRequested)
        {
            return new(dryRun, rows, batches, true, boundary, "TimeBudget");
        }
        catch (SqlException) when (!ct.IsCancellationRequested && budget.IsCancellationRequested)
        {
            return new(dryRun, rows, batches, true, boundary, "TimeBudget");
        }
        catch (SqlException ex) when (ex.Number is 1205 or 1222)
        {
            return new(dryRun, rows, batches, false, boundary, "SqlContention");
        }
    }

    /// <summary>Đọc tối đa 500 dòng sau dòng trẻ để phát hiện tuổi đảo thứ tự, không đọc payload.</summary>
    private async Task<string?> ReadNonMonotonicReasonAsync(SqlConnection connection, long blockingSeq,
        JournalPurgeBoundaryOutput boundary, CancellationToken ct)
    {
        await using var read = MaintenanceCommand(connection, null, QuestJournalSql.ReadNonMonotonicReason);
        read.Parameters.Add("@blocking", SqlDbType.BigInt).Value = blockingSeq;
        read.Parameters.Add("@max", SqlDbType.BigInt).Value = boundary.MaxSeq!.Value;
        read.Parameters.Add("@cutoff", SqlDbType.DateTime2).Value = boundary.RetentionBeforeUtc;
        return await read.ExecuteScalarAsync(ct).ConfigureAwait(false) is null
            ? null : $"NonMonotonicCreatedUtc: BlockingSeq={blockingSeq}";
    }

    /// <summary>Đọc mọi cursor, chặn cursor ngoài cấu hình; HOLDLOCK chỉ khi kiểm lại trong transaction DELETE.</summary>
    private async Task<JournalPurgeBoundaryOutput> ReadPurgeBoundaryAsync(SqlConnection connection, SqlTransaction? transaction,
        IReadOnlyList<string> nodes, JournalMaintenanceOptions options, CancellationToken ct)
    {
        var cursors = new Dictionary<string, NodeCursorOutput>(StringComparer.OrdinalIgnoreCase);
        DateTime now;
        long? watermark;
        var hasExport = false;
        await using (var read = MaintenanceCommand(connection, transaction,
            (transaction is null ? QuestJournalSql.PurgeBoundary : QuestJournalSql.PurgeBoundaryLocked) +
            (!options.ExportCountsAsBackup ? "" : transaction is null ? QuestJournalSql.PurgeBoundaryExportFlag : QuestJournalSql.PurgeBoundaryExportFlagLocked)))
        {
            read.Parameters.Add("@days", SqlDbType.Int).Value = options.ProtectedBackupDays;
            read.Parameters.Add("@nodes", SqlDbType.NVarChar, -1).Value = JsonSerializer.Serialize(nodes);
            await using var reader = await read.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while (await reader.ReadAsync(ct).ConfigureAwait(false))
                cursors.Add(reader.GetString(0), new(reader.GetInt64(1), reader.GetInt64(2)));
            await reader.NextResultAsync(ct).ConfigureAwait(false);
            await reader.ReadAsync(ct).ConfigureAwait(false);
            now = Utc(reader.GetDateTime(0));
            watermark = reader.IsDBNull(1) ? null : reader.GetInt64(1);
            if (options.ExportCountsAsBackup && await reader.NextResultAsync(ct).ConfigureAwait(false) && await reader.ReadAsync(ct).ConfigureAwait(false))
                hasExport = reader.GetInt32(0) == 1;
        }
        // Thiếu cursor vẫn là lỗi ưu tiên như hợp đồng cũ; ngoài tập cấu hình không được âm thầm bỏ qua.
        var boundary = QuestJournalPurgeRules.Calculate(nodes.Select(n => cursors.GetValueOrDefault(n)).ToArray(),
            now, options.Retention, watermark, options.AllowPurgeWithoutVerifiedBackup, options.ExportCountsAsBackup, hasExport);
        if (boundary.BlockedReason == "MissingAssignedNodeCursor") return boundary;
        return cursors.Keys.Except(nodes, StringComparer.OrdinalIgnoreCase).Any()
            ? new(null, now - options.Retention, "UnconfiguredNodeCursor") : boundary;
    }

    internal Task<JournalCursorRetireOutput> RetireJournalCursorAsync(string node, IReadOnlyList<string> configuredNodes,
        bool dryRun = true, CancellationToken ct = default) =>
        RetireJournalCursorAsync(node, configuredNodes, 5, false, dryRun, ct);

    /// <summary>Gỡ cursor ngoài cấu hình; heartbeat mới hơn staleMinutes chặn cả xem trước/apply, trừ forceLive.</summary>
    internal async Task<JournalCursorRetireOutput> RetireJournalCursorAsync(string node, IReadOnlyList<string> configuredNodes,
        int staleMinutes, bool forceLive = false, bool dryRun = true, CancellationToken ct = default)
    {
        // dùng giờ SQL, khoá status (kể cả dải thiếu) tới COMMIT để tránh heartbeat chen vào DELETE.
        if (staleMinutes is < 1 or > 525600) throw new ArgumentOutOfRangeException(nameof(staleMinutes));
        ValidateMaintenanceNodes(configuredNodes);
        ValidateMaintenanceNodes(new[] { node });
        if (configuredNodes.Contains(node, StringComparer.OrdinalIgnoreCase))
            return new(dryRun, node, false, null, null, false, "ConfiguredNodeCursor");
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(ct).ConfigureAwait(false);
            await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(ct).ConfigureAwait(false);
            NodeCursorOutput? cursor = null;
            await using (var read = MaintenanceCommand(connection, transaction,
                QuestJournalSql.RetireCursorRead))
            {
                read.Parameters.Add("@node", SqlDbType.NVarChar, 32).Value = node;
                await using var reader = await read.ExecuteReaderAsync(ct).ConfigureAwait(false);
                if (await reader.ReadAsync(ct).ConfigureAwait(false)) cursor = new(reader.GetInt64(0), reader.GetInt64(1));
            }
            if (cursor is null) return new(dryRun, node, false, null, null, false, "CursorNotFound");
            if (!forceLive)
            {
                await using var live = MaintenanceCommand(connection, transaction, QuestJournalSql.RetireCursorHeartbeat);
                live.Parameters.Add("@node", SqlDbType.NVarChar, 32).Value = node;
                live.Parameters.Add("@stale", SqlDbType.Int).Value = staleMinutes;
                if (Convert.ToInt32(await live.ExecuteScalarAsync(ct).ConfigureAwait(false)) == 1)
                    return new(dryRun, node, true, cursor.LastSeq, cursor.SafeSeq, false, "LiveNodeHeartbeat");
            }
            if (!dryRun)
            {
                await using var delete = MaintenanceCommand(connection, transaction, QuestJournalSql.RetireCursorDelete);
                delete.Parameters.Add("@node", SqlDbType.NVarChar, 32).Value = node;
                await delete.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            }
            await transaction.CommitAsync(ct).ConfigureAwait(false);
            return new(dryRun, node, true, cursor.LastSeq, cursor.SafeSeq, !dryRun, null);
        }
        catch (SqlException ex) when (ct.IsCancellationRequested)
        {
            throw new OperationCanceledException("Journal cursor retirement cancelled.", ex, ct);
        }
    }

    private SqlCommand MaintenanceCommand(SqlConnection connection, SqlTransaction? transaction, string sql) =>
        new(sql, connection, transaction) { CommandTimeout = Math.Max(1, _timeoutSeconds) };

    private static void ValidateMaintenanceNodes(IReadOnlyList<string> nodes)
    {
        if (nodes.Any(n => string.IsNullOrWhiteSpace(n) || n.Length > 32) ||
            nodes.Distinct(StringComparer.OrdinalIgnoreCase).Count() != nodes.Count)
            throw new ArgumentException("Assigned journal node names must be unique, nonempty and at most 32 characters.", nameof(nodes));
    }

}
