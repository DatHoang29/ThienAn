using System.Data;
using System.Globalization;
using Npgsql;
using SqlSugar;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Shared.QuestHa.Infrastructure.Services.Node;

/// <summary>
/// Author: KienLT
/// Description: Truy cập 1 node QuestDB qua PG wire bằng SqlSugar (DbType.QuestDB) + Npgsql 5.0.18 —
/// cùng stack production để lỗi kết nối/treo hành xử như thật (xem QuestAvailabilityGate.cs:44).
/// Tên bảng/cột lấy từ <see cref="QuestTableSpec"/> (hằng trong code, không phải input người dùng) và luôn
/// được quote; mọi giá trị đi bằng parameter.
/// Created date: 09/10/2026
/// </summary>
internal sealed class QuestNodeClientService : IQuestNodeClient
{
    private readonly string _connectionString;
    private readonly int _timeoutSeconds;
    // router và replicator dùng cùng client/node để giới hạn xoá đúng pool một lần/giây.
    private readonly object _poolSync = new();
    private DateTime? _lastPoolReset;
    // log đúng node/lý do và hạn mức, chỉ dựng log khi Debug bật.
    private readonly ILogger _logger;
    private readonly TimeProvider _clock;

    /// <inheritdoc />
    public void ResetConnectionPool() => ResetConnectionPool("không nêu lý do");

    /// <inheritdoc />
    public void ResetConnectionPool(string reason)
    {
        lock (_poolSync)
        {
            var now = _clock.GetUtcNow().UtcDateTime;
            if (!Rules.QuestPoolResetRules.ShouldReset(_lastPoolReset, now))
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                    _logger.LogDebug("[QuestHa.Node] ResetConnectionPool {Node}: reason={Reason}, skipped=True (giới hạn một lần/giây).", Node, reason);
                return;
            }
            using var connection = new NpgsqlConnection(_connectionString);
            try { NpgsqlConnection.ClearPool(connection); }
            catch (Exception ex)
            {
                _logger.LogSafeWarning(ex, "[QuestHa.Node] ResetConnectionPool lỗi {Node}, reason={Reason}.", Node, reason);
                throw;
            }
            _lastPoolReset = now;
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("[QuestHa.Node] ResetConnectionPool {Node}: reason={Reason}, skipped=False.", Node, reason);
        }
    }

    /// <summary>Khởi tạo client PG của một node, logger và đồng hồ dùng cho hạn mức reset pool.</summary>
    public QuestNodeClientService(QuestNodeOptions node, int commandTimeoutSeconds = 10,
        ILogger<QuestNodeClientService>? logger = null, TimeProvider? clock = null)
    {
        if (string.IsNullOrWhiteSpace(node.Name)) throw new ArgumentException("Node name is required.");
        if (string.IsNullOrWhiteSpace(node.PgConnectionString))
            throw new ArgumentException($"PgConnectionString is required for node {node.Name}.");

        Node = node.Name;
        _logger = logger ?? (ILogger)NullLogger.Instance;
        _clock = clock ?? TimeProvider.System;
        _timeoutSeconds = commandTimeoutSeconds;
        _connectionString = EnsureNoTypeLoading(node.PgConnectionString);
    }

    public string Node { get; }

    /// <summary>Thiếu <c>ServerCompatibilityMode=NoTypeLoading</c> ⇒ Npgsql tải catalog kiểu và tham số chuỗi lỗi.</summary>
    internal static string EnsureNoTypeLoading(string cs) =>
        cs.Contains("ServerCompatibilityMode", StringComparison.OrdinalIgnoreCase)
            ? cs
            : cs.TrimEnd(';') + ";ServerCompatibilityMode=NoTypeLoading";

    /// <inheritdoc />
    public ISqlSugarClient CreateDb() => Db(default);

    /// <summary>: lease của router đi cùng đường mở kết nối với Probe/Write — node chết báo lỗi ~2 s.</summary>
    public async Task<QuestNodeLease> OpenLeaseAsync(CancellationToken ct = default)
    {
        var conn = await OpenAsync(ct).ConfigureAwait(false);
        try
        {
            return new QuestNodeLease(Node, Db(ct, conn)) { Connection = conn };
        }
        catch
        {
            // chưa giao được cho lease ⇒ router không dispose hộ; đóng ngay để không giữ suất pool.
            await conn.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    private SqlSugarClient Db(CancellationToken ct, NpgsqlConnection? open = null)
    {
        var db = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = _connectionString,
            DbType = SqlSugar.DbType.QuestDB,
            // kết nối mở sẵn ⇒ caller tự đóng (using); SqlSugar không được đóng/mở lại giữa các lệnh.
            IsAutoCloseConnection = open is null,
            // mặc định SqlSugar ghép câu tiếng Trung + tiếng Anh vào message lỗi.
            LanguageType = LanguageType.English,
        });
        db.Ado.CommandTimeOut = _timeoutSeconds;
        db.Ado.CancellationToken = ct;
        if (open is not null) db.Ado.Connection = open;
        return db;
    }

    /// <summary>
    /// mở kết nối PG TRƯỚC khi đưa cho SqlSugar. <c>QuestDBProvider.CheckConnection</c> của SqlSugar 5.1.4
    /// gọi <c>Connection.Open()</c> ĐỒNG BỘ tới 15 lần và nuốt lỗi ⇒ node đã dừng mất ~15 × Timeout (~30 s với Timeout=2)
    /// rồi mới ném "Connection is not open", và chặn thread gọi suốt thời gian đó. Mở ở đây bằng <c>OpenAsync</c>: MỘT lần,
    /// giới hạn bởi Timeout của chuỗi kết nối, không chặn thread; lỗi giữ nguyên kiểu Npgsql (classifier nhận "Exception
    /// while connecting" là lỗi kết nối). Kết nối đã Open ⇒ CheckConnection không mở lại.
    /// </summary>
    private async Task<NpgsqlConnection> OpenAsync(CancellationToken ct)
    {
        var conn = new NpgsqlConnection(_connectionString);
        try
        {
            await conn.OpenAsync(ct).ConfigureAwait(false);
            return conn;
        }
        catch
        {
            await conn.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    private static string Q(string ident) => "\"" + ident.Replace("\"", "\"\"") + "\"";

    private static string Lit(string s) => "'" + s.Replace("'", "''") + "'";

    /// <inheritdoc />
    public async Task<NodeProbeOutput> ProbeAsync(QuestTableSpec table, CancellationToken ct = default)
    {
        await using var conn = await OpenAsync(ct).ConfigureAwait(false);
        var db = Db(ct, conn);
        await db.Ado.GetIntAsync("SELECT 1");
        var dt = await db.Ado.GetDataTableAsync(
            "SELECT suspended, writerTxn, sequencerTxn FROM wal_tables() WHERE name = " + Lit(table.Name));
        if (dt.Rows.Count == 0)
            return new NodeProbeOutput(false, false, 0, 0, $"Bảng {table.Name} không tồn tại (hoặc không phải WAL).");

        var r = dt.Rows[0];
        var suspended = Convert.ToBoolean(r["suspended"], CultureInfo.InvariantCulture);
        var writer = Convert.ToInt64(r["writerTxn"], CultureInfo.InvariantCulture);
        var seq = Convert.ToInt64(r["sequencerTxn"], CultureInfo.InvariantCulture);
        return new NodeProbeOutput(true, suspended, writer, seq, suspended ? $"WAL của {table.Name} bị SUSPENDED." : null);
    }

    /// <inheritdoc />
    public async Task WriteBatchAsync(QuestTableSpec table, IReadOnlyList<QuestRow> rows, CancellationToken ct = default)
    {
        if (rows.Count == 0) return;

        var cols = string.Join(", ", table.Columns.Select(c => Q(c.Name)));
        var sb = new System.Text.StringBuilder(64 + rows.Count * table.Columns.Count * 8);
        sb.Append("INSERT INTO ").Append(Q(table.Name)).Append(" (").Append(cols).Append(") VALUES ");

        var ps = new List<SugarParameter>(rows.Count * table.Columns.Count);
        for (var r = 0; r < rows.Count; r++)
        {
            if (r > 0) sb.Append(", ");
            sb.Append('(');
            for (var c = 0; c < table.Columns.Count; c++)
            {
                if (c > 0) sb.Append(", ");
                var name = $"@p{r}_{c}";
                sb.Append(name);
                ps.Add(Param(name, table.Columns[c], rows[r].Values[c]));
            }

            sb.Append(')');
        }

        await using var conn = await OpenAsync(ct).ConfigureAwait(false);
        await Db(ct, conn).Ado.ExecuteCommandAsync(sb.ToString(), ps.ToArray());
    }

    private static SugarParameter Param(string name, QuestColumn col, object? v)
    {
        var type = col.Type switch
        {
            QuestColumnTypeEnum.Timestamp => System.Data.DbType.DateTime,
            QuestColumnTypeEnum.Long => System.Data.DbType.Int64,
            QuestColumnTypeEnum.Int => System.Data.DbType.Int32,
            QuestColumnTypeEnum.Short => System.Data.DbType.Int16, // cột SHORT của VDS
            QuestColumnTypeEnum.Double => System.Data.DbType.Double,
            QuestColumnTypeEnum.Boolean => System.Data.DbType.Boolean,
            _ => System.Data.DbType.String,
        };
        object value = v switch
        {
            null => DBNull.Value,
            // Npgsql 5: DateTime gửi dạng timestamp (không tz). Giá trị đã là UTC.
            DateTime d => DateTime.SpecifyKind(QuestPayloadHelper.NormalizeTs(d), DateTimeKind.Unspecified),
            _ => v,
        };
        return new SugarParameter(name, value, type);
    }

    /// <inheritdoc />
    public async Task<long> CountAsync(QuestTableSpec table, CancellationToken ct = default)
    {
        await using var conn = await OpenAsync(ct).ConfigureAwait(false);
        return await Db(ct, conn).Ado.GetLongAsync("SELECT count() FROM " + Q(table.Name));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> LatestAsync(
        QuestTableSpec table, int take = 10, CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 1000);
        var cols = string.Join(", ", table.Columns.Select(c => Q(c.Name)));
        await using var conn = await OpenAsync(ct).ConfigureAwait(false);
        var dt = await Db(ct, conn).Ado.GetDataTableAsync(
            $"SELECT {cols} FROM {Q(table.Name)} ORDER BY {Q(table.Designated.Name)} DESC LIMIT {take}");

        var result = new List<IReadOnlyDictionary<string, object?>>(dt.Rows.Count);
        foreach (DataRow row in dt.Rows)
        {
            var d = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var c in table.Columns)
            {
                var idx = dt.Columns.IndexOf(c.Name);
                d[c.Name] = idx < 0 ? null : QuestPayloadHelper.NormalizeFromDb(c, row[idx]);
            }

            result.Add(d);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<QuestRow>> ReadRangeAsync(
        QuestTableSpec table, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
    {
        var cols = string.Join(", ", table.Columns.Select(c => Q(c.Name)));
        var ts = Q(table.Designated.Name);
        await using var conn = await OpenAsync(ct).ConfigureAwait(false);
        var dt = await Db(ct, conn).Ado.GetDataTableAsync(
            $"SELECT {cols} FROM {Q(table.Name)} WHERE {ts} >= @from AND {ts} < @to",
            new SugarParameter("@from", DateTime.SpecifyKind(QuestPayloadHelper.NormalizeTs(fromUtc), DateTimeKind.Unspecified), System.Data.DbType.DateTime),
            new SugarParameter("@to", DateTime.SpecifyKind(QuestPayloadHelper.NormalizeTs(toUtc), DateTimeKind.Unspecified), System.Data.DbType.DateTime));

        var seqIdx = table.Columns.ToList().FindIndex(c => c.FromJournalSeq);
        var idx = table.Columns.Select(c => dt.Columns.IndexOf(c.Name)).ToArray();
        var result = new List<QuestRow>(dt.Rows.Count);
        foreach (DataRow row in dt.Rows)
        {
            var values = new object?[table.Columns.Count];
            for (var i = 0; i < values.Length; i++)
                values[i] = idx[i] < 0 ? null : QuestPayloadHelper.NormalizeFromDb(table.Columns[i], row[idx[i]]);
            var seq = seqIdx >= 0 && values[seqIdx] is long s ? s : 0;
            result.Add(new QuestRow(seq, values));
        }

        return result;
    }

    // bản cũ so writerTxn với sequencerTxn MỚI của từng probe nên dưới tải ghi liên tục
    // (100,101),(101,102)… không bao giờ đúng; nay chốt target ở probe đầu (QuestWalWaitRules).
    /// <inheritdoc />
    public Task<bool> WaitWalAppliedAsync(QuestTableSpec table, TimeSpan timeout, CancellationToken ct = default) =>
        WaitWalAppliedAsync(token => ProbeAsync(table, token), timeout, TimeSpan.FromMilliseconds(100), ct);

    /// <summary>
    /// Vòng chờ WAL theo mốc cố định: probe đầu chốt target = sequencerTxn, sau đó chờ writerTxn ≥ target tới hết
    /// <paramref name="timeout"/> (luôn có ít nhất một probe). Bảng mất/suspended/txn lùi ⇒ false ngay. Lỗi probe ném ra.
    /// Caller phải chốt head/cursor TRƯỚC khi gọi để mọi row cần đọc đã nằm trong WAL lúc probe đầu.
    /// </summary>
    internal static async Task<bool> WaitWalAppliedAsync(Func<CancellationToken, Task<NodeProbeOutput>> probe,
        TimeSpan timeout, TimeSpan pollInterval, CancellationToken ct)
    {
        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        Rules.QuestWalWaitRules.Baseline? baseline = null;
        while (true)
        {
            var p = await probe(ct).ConfigureAwait(false);
            switch (Rules.QuestWalWaitRules.Evaluate(baseline, p, out var next))
            {
                case Rules.QuestWalWaitRules.Decision.Applied: return true;
                case Rules.QuestWalWaitRules.Decision.Invalid: return false;
            }
            baseline = next;
            var remaining = timeout - elapsed.Elapsed;
            if (remaining <= TimeSpan.Zero) return false;
            await Task.Delay(remaining < pollInterval ? remaining : pollInterval, ct).ConfigureAwait(false);
        }
    }
}
