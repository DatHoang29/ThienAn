using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SqlSugar;
using Shared.QuestHa.Infrastructure.Services.Journal.Statements;
using Shared.QuestHa.Infrastructure.Services.Journal.Mapping;
using static Shared.QuestHa.Infrastructure.Services.Journal.Mapping.QuestJournalMapper;
using static Shared.QuestHa.Infrastructure.Services.Journal.Statements.QuestJournalParameter;

namespace Shared.QuestHa.Infrastructure.Services.Journal;

/// <summary>
/// Author: KienLT
/// Description: Journal trên SQL Server qua SqlSugar (Ado + T-SQL viết tay để kiểm soát transaction,
/// rowversion và MIN_ACTIVE_ROWVERSION()).
///
/// <para><b>Thứ tự commit.</b> Append lấy <c>sp_getapplock</c> độc quyền theo transaction ⇒ IDENTITY, rowversion
/// và COMMIT diễn ra tuần tự, nên "Seq nhỏ hơn đã commit trước Seq lớn hơn" đúng tuyệt đối. Không có khoá này,
/// 2 phiên song song có thể nhận Seq 10/11 nhưng RowVer đảo thứ tự; khi đó điều kiện
/// <c>RowVer &lt; MIN_ACTIVE_ROWVERSION()</c> vẫn cho Seq 11 đi qua lúc Seq 10 chưa commit và cursor nhảy qua 10
/// vĩnh viễn. Điều kiện MIN_ACTIVE_ROWVERSION vẫn giữ làm lớp chặn thứ hai.</para>
/// Created date: 09/10/2026
/// </summary>
internal sealed class QuestJournalService : IJournalStore, IAsyncDisposable
{
    // Độ dài cột QuestOutbox (sql/journal.sql) — kiểm trước khi xếp hàng để lô không hỏng vì 1 dòng.
    private const int TargetTableMaxLength = 128;
    // public để QuestNodeCompareService kiểm EventKey trước khi vá (một nguồn sự thật cho độ dài cột QuestOutbox.EventKey).
    public const int EventKeyMaxLength = 256;

    private readonly string _connectionString;
    private readonly int _timeoutSeconds;
    private readonly TimeProvider _clock;
    // cùng chính sách tuổi backup cho catalog và purge.
    private readonly JournalMaintenanceOptions _maintenance;
    private readonly QuestJournalPurgeService _purge;
    private readonly QuestJournalBackupCatalogService _backupCatalog;
    private readonly QuestJournalExportCatalogService _exportCatalog;
    private readonly QuestJournalStatusService _status;
    private readonly QuestJournalAppendService _append;
    private readonly QuestJournalBatchWorker? _batcher;
    private long _lastPoolClearTicks;
    private readonly ILogger _logger;

    // nhận chính sách bảo vệ backup dùng chung với dịch vụ maintenance.
    /// <summary>Khởi tạo journal với timeout, đồng hồ và chính sách batch/bảo trì.</summary>
    public QuestJournalService(string connectionString, int commandTimeoutSeconds = 10, TimeProvider? clock = null, JournalOptions? journal = null, JournalMaintenanceOptions? maintenance = null,
        ILogger<QuestJournalService>? logger = null, Func<JournalMaintenanceOptions>? maintenanceSource = null)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Journal connection string is required.", nameof(connectionString));
        _connectionString = connectionString;
        _timeoutSeconds = commandTimeoutSeconds;
        _logger = logger ?? (ILogger)NullLogger.Instance;
        _clock = clock ?? TimeProvider.System;
        // không suy tuổi theo cấu hình khác giữa purge và catalog.
        _maintenance = maintenance ?? new JournalMaintenanceOptions();
        _purge = new QuestJournalPurgeService(connectionString, commandTimeoutSeconds, Db);
        // maintenanceSource = snapshot vận hành của host: ProtectedBackupDays đổi lúc chạy áp cho lượt đọc catalog kế tiếp.
        var maintenanceNow = maintenanceSource ?? (() => _maintenance);
        _backupCatalog = new QuestJournalBackupCatalogService(Db, maintenanceNow, _clock);
        _exportCatalog = new QuestJournalExportCatalogService(Db);
        _status = new QuestJournalStatusService(Db, _clock);
        _append = new QuestJournalAppendService(Db);
        journal ??= new JournalOptions();
        if (journal.AppendBatching)
            // Lô một dòng dùng INSERT đơn để tránh chi phí JSON không cần thiết.
            _batcher = new QuestJournalBatchWorker(WriteQueuedAsync, journal.AppendMaxBatch, journal.AppendQueueCapacity);
    }

    /// <summary>thống kê gom lô (null khi tắt batching).</summary>
    public (long Batches, long Appended, long Rejected, int LargestBatch)? BatchStats => _batcher?.Stats;

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _batcher?.DisposeAsync() ?? ValueTask.CompletedTask;

    private SqlSugarClient Db(CancellationToken ct)
    {
        var db = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = _connectionString,
            DbType = SqlSugar.DbType.SqlServer,
            IsAutoCloseConnection = true,
            // mặc định SqlSugar ghép câu tiếng Trung + tiếng Anh vào message lỗi.
            LanguageType = LanguageType.English,
        });
        db.Ado.CommandTimeOut = _timeoutSeconds;
        db.Ado.CancellationToken = ct;
        // OFFLINE→ONLINE đã tái hiện pool blocking; không retry lệnh có thể đã COMMIT.
        db.Aop.OnError = error =>
        {
            // Debug chỉ nhận exception đã làm sạch (không object exception).
            _logger.LogSafeDebug(error, "[QuestHa.Journal] Chi tiết lỗi SQL.");
            if (!error.Message.Contains("Connection open error", StringComparison.OrdinalIgnoreCase)) return;
            if (db.Ado.Connection is not SqlConnection { State: ConnectionState.Closed } connection) return;
            var now = Environment.TickCount64;
            var previous = Interlocked.Read(ref _lastPoolClearTicks);
            if (now - previous >= 1000 && Interlocked.CompareExchange(ref _lastPoolClearTicks, now, previous) == previous)
                SqlConnection.ClearPool(connection);
        };
        return db;
    }

    // ================================================================================== nhóm ghi/xem journal

    /// <inheritdoc />
    public Task<JournalAppendOutput> AppendAsync(QuestTableSpec table, object payload, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(table);
        ArgumentNullException.ThrowIfNull(payload);

        var json = payload as string ?? JsonSerializer.Serialize(payload, payload.GetType(), QuestPayloadHelper.JsonOptions);

        // Kiểm tra kiểu NGAY khi append: payload sai do lỗi lập trình thì báo caller, không đưa vào journal.
        QuestRow row;
        try
        {
            row = QuestPayloadHelper.ToRow(table, 0, json);
        }
        catch (PayloadConversionException ex)
        {
            throw new ArgumentException($"Payload không khớp spec bảng {table.Name}: {ex.Message}", nameof(payload), ex);
        }

        var entry = new JournalAppendInput(
            table.Name,
            QuestPayloadHelper.KeyOf(table, row),
            (DateTime)row.Values[table.DesignatedIndex]!,
            json,
            table.SchemaVer,
            QuestPayloadHelper.VersionOf(table, row));

        return AppendRawAsync(entry, ct);
    }

    /// <inheritdoc />
    public Task<JournalAppendOutput> AppendRawAsync(JournalAppendInput entry, CancellationToken ct = default)
    {
        Validate(entry);
        return _batcher is null ? AppendSingleAsync(entry, ct) : _batcher.AppendAsync(entry, ct);
    }

    /// <summary>
    /// ghi cả lô trong 1 transaction (caller đã có sẵn lô, vd worker VDS) — bỏ qua hàng đợi gom lô.
    /// ACK theo đúng thứ tự <paramref name="entries"/>; lỗi ⇒ cả lô CHƯA ACK.
    /// </summary>
    public async Task<IReadOnlyList<JournalAppendOutput>> AppendRawBatchAsync(IReadOnlyList<JournalAppendInput> entries, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entries);
        foreach (var e in entries) Validate(e);
        if (entries.Count == 0) return Array.Empty<JournalAppendOutput>();
        try
        {
            return await AppendBatchSqlAsync(entries, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new JournalAppendException($"Journal CHƯA ACK (SQL lỗi, lô {entries.Count} dòng): {QuestErrorClassifierHelper.SafeDescribe(ex)}", ex);
        }
    }

    private static void Validate(JournalAppendInput entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (string.IsNullOrEmpty(entry.TargetTable) || entry.TargetTable.Length > TargetTableMaxLength)
            throw new ArgumentException($"TargetTable rỗng hoặc dài quá {TargetTableMaxLength} ký tự.", nameof(entry));
        if (string.IsNullOrEmpty(entry.EventKey) || entry.EventKey.Length > EventKeyMaxLength)
            throw new ArgumentException($"EventKey rỗng hoặc dài quá {EventKeyMaxLength} ký tự.", nameof(entry));
        if (entry.Payload is null) throw new ArgumentException("Payload null.", nameof(entry));
    }

    /// <summary>
    /// Executor của hàng đợi gom lô. Lô 1 dòng (caller gọi tuần tự, vd VdsFrameProcessor 1 luồng) đi câu INSERT đơn:
    /// câu SQL lô (OPENJSON + biến bảng) nặng hơn ⇒ đo SQL local 27/10/2026: tuần tự 134 ev/s qua SQL lô so với
    /// 389 ev/s qua INSERT đơn. Hai đường cùng applock và cùng ngữ nghĩa Superseded nên đổi đường không đổi kết quả.
    /// </summary>
    // Lời gọi tuần tự từ Services.Vds cần tránh chi phí gom lô không cần thiết.
    private async Task<IReadOnlyList<JournalAppendOutput>> WriteQueuedAsync(IReadOnlyList<JournalAppendInput> entries, CancellationToken ct) =>
        entries.Count == 1
            ? new[] { await AppendSingleAsync(entries[0], ct).ConfigureAwait(false) }
            : await AppendBatchSqlAsync(entries, ct).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<JournalStatsOutput> GetStatsAsync(CancellationToken ct = default)
    {
        var db = Db(ct);
        var head = (await db.Ado.SqlQueryAsync<StatsDb>(QuestJournalSql.StatsHead)).Single();
        var cursors = await db.Ado.SqlQueryAsync<CursorDb>(QuestJournalSql.StatsCursors);
        return QuestJournalMapper.ToStats(head, cursors);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<JournalRowOutput>> GetLatestAsync(int take = 10, CancellationToken ct = default)
    {
        var rows = await Db(ct).Ado.SqlQueryAsync<OutboxDb>(
            QuestJournalSql.GetLatest,
            P("@take", Math.Clamp(take, 1, 1000)));
        return rows.Select(ToRow).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<QuarantineRowOutput>> GetQuarantineAsync(int take = 50, CancellationToken ct = default)
    {
        var rows = await Db(ct).Ado.SqlQueryAsync<QuarantineDb>(
            QuestJournalSql.GetQuarantine,
            P("@take", Math.Clamp(take, 1, 1000)));
        return rows.Select(QuestJournalMapper.ToQuarantine).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<JournalRowOutput>> ReadBatchAsync(long afterSeq, int max, CancellationToken ct = default)
    {
        var rows = await Db(ct).Ado.SqlQueryAsync<OutboxDb>(
            QuestJournalSql.ReadBatch,
            P("@max", Math.Clamp(max, 1, 10_000)), P("@after", afterSeq));
        return rows.Select(ToRow).ToList();
    }

    /// <inheritdoc />
    public async Task<NodeCursorOutput> GetCursorAsync(string node, CancellationToken ct = default)
    {
        var r = (await Db(ct).Ado.SqlQueryAsync<CursorDb>(QuestJournalSql.GetCursor, P("@node", node))).Single();
        return QuestJournalMapper.ToCursor(r);
    }

    /// <inheritdoc />
    public async Task<bool> TryAdvanceCursorAsync(string node, long expected, long newSeq, long safeSeq, CancellationToken ct = default)
    {
        var n = await Db(ct).Ado.GetIntAsync(QuestJournalSql.TryAdvanceCursor, P("@node", node), P("@expected", expected), P("@new", newSeq), P("@safe", safeSeq));
        return n == 1;
    }

    /// <inheritdoc />
    public Task TryRaiseSafeSeqAsync(string node, long safeSeq, CancellationToken ct = default) =>
        Db(ct).Ado.ExecuteCommandAsync(
            QuestJournalSql.TryRaiseSafeSeq,
            P("@node", node), P("@safe", safeSeq));

    /// <inheritdoc />
    public async Task<(long From, long To)?> RewindToSafeAsync(string node, CancellationToken ct = default)
    {
        var rows = await Db(ct).Ado.SqlQueryAsync<RewindDb>(QuestJournalSql.RewindToSafe, P("@node", node));
        return rows.Count == 0 ? null : (rows[0].OldSeq, rows[0].NewSeq);
    }

    /// <inheritdoc />
    public async Task<long> LowerCursorAsync(string node, long seq, string by, CancellationToken ct = default)
    {
        await GetCursorAsync(node, ct);
        return await Db(ct).Ado.GetLongAsync(QuestJournalSql.LowerCursor, new[] { P("@node", node), P("@seq", Math.Max(0, seq)), P("@by", by) });
    }

    /// <inheritdoc />
    public async Task SetCursorAsync(string node, long seq, string by, CancellationToken ct = default)
    {
        await GetCursorAsync(node, ct);
        await Db(ct).Ado.ExecuteCommandAsync(
            QuestJournalSql.SetCursor,
            P("@node", node), P("@seq", Math.Max(0, seq)), P("@by", by));
    }

    /// <inheritdoc />
    // phân biệt SQL đọc head với coverage khi lỗi journal xảy ra sát Fence; không retry/bỏ kiểm tra.
    public Task<NodeLagOutput> GetLagAsync(long cursor, CancellationToken ct = default) =>
        !_logger.IsEnabled(LogLevel.Debug) ? ReadLagCoreAsync(cursor, ct)
            : TraceReadAsync("GetLag/head", null, () => ReadLagCoreAsync(cursor, ct), ct);

    private async Task<NodeLagOutput> ReadLagCoreAsync(long cursor, CancellationToken ct)
    {
        var r = (await Db(ct).Ado.SqlQueryAsync<LagDb>(QuestJournalSql.ReadLagCore, P("@cursor", cursor))).Single();
        return QuestJournalMapper.ToLag(r);
    }

    /// <inheritdoc />
    public Task QuarantineAsync(long seq, string node, string error, CancellationToken ct = default)
    {
        return Db(ct).Ado.ExecuteCommandAsync(QuestJournalSql.Quarantine, P("@seq", seq), P("@node", node), P("@err", Trim(error, 2000)));
    }

    /// <inheritdoc />
    public async Task<IReadOnlySet<long>> GetQuarantinedSeqsAsync(string node, long fromSeq, long toSeq, CancellationToken ct = default)
    {
        var rows = await Db(ct).Ado.SqlQueryAsync<long>(
            QuestJournalSql.GetQuarantinedSeqs,
            P("@node", node), P("@from", fromSeq), P("@to", toSeq));
        return rows.ToHashSet();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<OpenQuarantineOutput>> GetOpenQuarantineAsync(long fromSeq, long toSeq, CancellationToken ct = default)
    {
        var rows = await Db(ct).Ado.SqlQueryAsync<OpenQuarantineDb>(
            QuestJournalSql.GetOpenQuarantine,
            P("@from", fromSeq), P("@to", toSeq));
        return rows.Select(QuestJournalMapper.ToOpenQuarantine).ToList();
    }

    /// <inheritdoc />
    public async Task<bool> ResolveQuarantineAsync(long seq, string node, int expectedAttempts, string by, CancellationToken ct = default)
    {
        // Không SET NOCOUNT ON: cần số dòng ảnh hưởng (CAS theo thế hệ Attempts).
        var n = await Db(ct).Ado.ExecuteCommandAsync(
            QuestJournalSql.ResolveQuarantine,
            P("@seq", seq), P("@node", node), P("@expected", expectedAttempts), P("@by", Trim(by, 64)));
        return n == 1;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<JournalRowOutput>> ReadWindowAsync(string table, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
        => ReadWindowAsync(table, fromUtc, toUtc, long.MaxValue, ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<JournalRowOutput>> ReadWindowAsync(string table, DateTime fromUtc, DateTime toUtc, long maxSeq, CancellationToken ct = default)
    {
        var rows = await Db(ct).Ado.SqlQueryAsync<OutboxDb>(
            QuestJournalSql.ReadWindow,
            P("@t", table), P("@from", fromUtc), P("@to", toUtc), P("@maxSeq", maxSeq));
        return rows.Select(ToRow).ToList();
    }

    /// <inheritdoc />
    public async Task<DateTime?> GetCreatedUtcAsync(long seq, CancellationToken ct = default)
    {
        var rows = await Db(ct).Ado.SqlQueryAsync<DateTime>(QuestJournalSql.GetCreatedUtc, P("@seq", seq));
        return rows.Count == 0 ? null : Utc(rows[0]);
    }

    // ranh giới purge bền phát hiện cả journal rỗng và lỗ phía sau dòng quarantine còn giữ.
    /// <inheritdoc />
    public async Task<bool> HasJournalGapAsync(long watermark, CancellationToken ct = default)
    {
        var values = await Db(ct).Ado.SqlQueryAsync<int>(QuestJournalSql.HasJournalGap, P("@watermark", watermark));
        if (values.Count != 1) throw new InvalidOperationException("Thiếu QuestJournalPurgeState; chạy migration biên purge journal.");
        return values[0] != 0;
    }

    // copy có thể kéo dài qua biên tuổi backup; ghim cursor và kiểm gap cùng transaction với purge.
    /// <inheritdoc />
    public async Task SetRestoreCursorAsync(string node, long watermark, CancellationToken ct = default)
    {
        await Db(ct).Ado.ExecuteCommandAsync(QuestJournalSql.SetRestoreCursor, P("@node", node), P("@seq", watermark));
    }

    // ================================================================================== mapping

    /// <inheritdoc />
    // thời gian/operation/node đủ đối chiếu với Fence, không in connection string hoặc payload.
    public Task<NodeReadCoverageOutput> ReadCoverageAsync(string node, CancellationToken ct = default) =>
        !_logger.IsEnabled(LogLevel.Debug) ? ReadCoverageCoreAsync(node, ct)
            : TraceReadAsync("ReadCoverage", node, () => ReadCoverageCoreAsync(node, ct), ct);

    private async Task<NodeReadCoverageOutput> ReadCoverageCoreAsync(string node, CancellationToken ct)
    {
        // thiếu status/cursor không tự tạo ở đường đọc; Worker chịu trách nhiệm khởi tạo.
        var rows = await Db(ct).Ado.SqlQueryAsync<CoverageDb>(QuestJournalSql.ReadCoverageCore, P("@node", node));
        var row = rows.SingleOrDefault();
        return QuestJournalMapper.ToCoverage(row);
    }

    private async Task<T> TraceReadAsync<T>(string operation, string? node, Func<Task<T>> read, CancellationToken ct)
    {
        var started = System.Diagnostics.Stopwatch.GetTimestamp();
        try
        {
            var value = await read().ConfigureAwait(false);
            // chỉ log SQL đọc chậm/lỗi, không nhân log trên mọi nhịp poll khỏe.
            var elapsed = System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            if (elapsed >= 250 && _logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("[QuestHa.Journal] Read end operation={Operation}, node={Node}, elapsedMs={ElapsedMs}, succeeded=True.",
                    operation, node, elapsed);
            return value;
        }
        catch (Exception ex)
        {
            // Debug chỉ nhận exception đã làm sạch (không object exception).
            _logger.LogSafeDebug(ex, "[QuestHa.Journal] Read end operation={Operation}, node={Node}, elapsedMs={ElapsedMs}, succeeded=False, cancelled={Cancelled}, error={Error}.",
                operation, node, System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds, ct.IsCancellationRequested,
                QuestErrorClassifierHelper.SafeDescribe(ex));
            throw;
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<BackupRecordOutput>> GetBackupsAsync(string? node = null, CancellationToken ct = default) =>
        _backupCatalog.GetBackupsAsync(node, ct);

    /// <inheritdoc />
    public Task WriteStatusAsync(NodeStatusOutput s, CancellationToken ct = default) =>
        _status.WriteStatusAsync(s, ct);

    /// <inheritdoc />
    public Task<IReadOnlyList<NodeStatusOutput>> ReadStatusesAsync(CancellationToken ct = default) =>
        _status.ReadStatusesAsync(ct);

    /// <inheritdoc />
    public Task WriteReconcileResultAsync(string node, bool ok, CancellationToken ct = default) =>
        _status.WriteReconcileResultAsync(node, ok, ct);

    /// <inheritdoc />
    public Task<NodeRecoveryOutput> GetRecoveryAsync(string node, CancellationToken ct = default) =>
        _status.GetRecoveryAsync(node, ct);

    /// <inheritdoc />
    public Task<long> BeginRecoveryAsync(string node, string by, CancellationToken ct = default) =>
        _status.BeginRecoveryAsync(node, by, ct);

    /// <inheritdoc />
    public Task<bool> MarkRecoveryCoveredAsync(string node, long generation, CancellationToken ct = default) =>
        _status.MarkRecoveryCoveredAsync(node, generation, ct);

    /// <inheritdoc />
    public Task<bool> CompleteRecoveryAsync(string node, long generation, CancellationToken ct = default) =>
        _status.CompleteRecoveryAsync(node, generation, ct);

    /// <inheritdoc />
    public Task SetManualFenceAsync(string node, bool fenced, string by, CancellationToken ct = default) =>
        _status.SetManualFenceAsync(node, fenced, by, ct);

    /// <inheritdoc />
    public Task<bool> TryClearManualFenceAsync(string node, long generation, bool replayReady, CancellationToken ct = default) =>
        _status.TryClearManualFenceAsync(node, generation, replayReady, ct);

    /// <inheritdoc />
    public Task<bool?> WriteReconcileResultAsync(string node, bool ok, long observedGeneration, DateTime readStartedSqlUtc, CancellationToken ct = default) =>
        _status.WriteReconcileResultAsync(node, ok, observedGeneration, readStartedSqlUtc, ct);

    /// <inheritdoc />
    public Task AddBackupAsync(BackupRecordOutput r, CancellationToken ct = default) =>
        _backupCatalog.AddBackupAsync(r, ct);

    /// <inheritdoc />
    public Task<BackupRecordOutput?> GetBackupAsync(Guid backupId, CancellationToken ct = default) =>
        _backupCatalog.GetBackupAsync(backupId, ct);

    /// <inheritdoc />
    public Task<IReadOnlyList<BackupRecordOutput>> ListBackupsAsync(CancellationToken ct = default) =>
        _backupCatalog.ListBackupsAsync(ct);

    /// <inheritdoc />
    public Task<bool> SetBackupRetiredAsync(Guid id, bool retired, bool dryRun = true, CancellationToken ct = default) =>
        _backupCatalog.SetBackupRetiredAsync(id, retired, dryRun, ct);

    /// <inheritdoc />
    public Task<IReadOnlyList<QuestExportRecordOutput>> ListExportsAsync(string? table = null, DateOnly? fromDay = null, DateOnly? toDay = null,
        CancellationToken ct = default) => _exportCatalog.ListExportsAsync(table, fromDay, toDay, ct);

    /// <inheritdoc />
    public Task AddExportAsync(QuestExportRecordOutput record, CancellationToken ct = default) => _exportCatalog.AddExportAsync(record, ct);

    /// <inheritdoc />
    public Task<int> RetireExportsAsync(IReadOnlyList<Guid> exportIds, CancellationToken ct = default) =>
        _exportCatalog.RetireExportsAsync(exportIds, ct);

    /// <inheritdoc />
    public Task<IReadOnlyList<int>> ReadExportLateHoursAsync(string table, DateTime fromUtc, DateTime toUtc, long afterSeq, long maxSeq,
        CancellationToken ct = default) => _exportCatalog.ReadLateHoursAsync(table, fromUtc, toUtc, afterSeq, maxSeq, ct);

    /// <inheritdoc />
    public Task<JournalBacklogOutput> ReadJournalBacklogAsync(IReadOnlyList<string> nodes, CancellationToken ct = default) =>
        _purge.ReadJournalBacklogAsync(nodes, ct);

    /// <inheritdoc />
    public Task<JournalBacklogOutput> ReadJournalBacklogAsync(IReadOnlyList<string> nodes, long pendingCountLimit,
        CancellationToken ct = default) =>
        _purge.ReadJournalBacklogAsync(nodes, pendingCountLimit, ct);

    /// <inheritdoc />
    public Task<JournalPurgeOutput> PurgeJournalAsync(IReadOnlyList<string> nodes, JournalMaintenanceOptions options,
        bool dryRun = true, CancellationToken ct = default) =>
        _purge.PurgeJournalAsync(nodes, options, dryRun, ct);

    /// <inheritdoc />
    public Task<JournalCursorRetireOutput> RetireJournalCursorAsync(string node, IReadOnlyList<string> configuredNodes,
        bool dryRun = true, CancellationToken ct = default) =>
        _purge.RetireJournalCursorAsync(node, configuredNodes, dryRun, ct);

    /// <inheritdoc />
    public Task<JournalCursorRetireOutput> RetireJournalCursorAsync(string node, IReadOnlyList<string> configuredNodes,
        int staleMinutes, bool forceLive = false, bool dryRun = true, CancellationToken ct = default) =>
        _purge.RetireJournalCursorAsync(node, configuredNodes, staleMinutes, forceLive, dryRun, ct);

    private Task<IReadOnlyList<JournalAppendOutput>> AppendBatchSqlAsync(IReadOnlyList<JournalAppendInput> entries, CancellationToken ct) =>
        _append.AppendBatchSqlAsync(entries, ct);

    private Task<JournalAppendOutput> AppendSingleAsync(JournalAppendInput entry, CancellationToken ct) =>
        _append.AppendSingleAsync(entry, ct);
}
