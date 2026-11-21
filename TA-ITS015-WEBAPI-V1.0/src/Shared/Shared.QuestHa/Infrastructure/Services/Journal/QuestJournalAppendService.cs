using System.Text.Json;
using SqlSugar;
using static Shared.QuestHa.Infrastructure.Services.Journal.Statements.QuestJournalParameter;
using Shared.QuestHa.Infrastructure.Services.Journal.Statements;
using Shared.QuestHa.Infrastructure.Services.Journal.Mapping;
using static Shared.QuestHa.Infrastructure.Services.Journal.Mapping.QuestJournalMapper;

namespace Shared.QuestHa.Infrastructure.Services.Journal;

/// <summary>
/// Author: KienLT
/// Description: Ghi journal đơn/lô với cùng khoá append và thứ tự ACK.
/// Created date: 31/10/2026
/// </summary>
internal sealed class QuestJournalAppendService
{
    private readonly Func<CancellationToken, SqlSugarClient> _db;
    internal QuestJournalAppendService(Func<CancellationToken, SqlSugarClient> db) => _db = db;

    /// <summary>
    /// 1 transaction, khoá append 1 lần cho cả lô. Lô gửi bằng 1 tham số JSON (không vướng giới hạn 2.100
    /// tham số), parse vào biến bảng TRƯỚC khi lấy khoá. Superseded tính như ghi tuần tự từng dòng theo thứ tự lô:
    /// dòng i bị thay thế khi có bản cùng khoá, version lớn hơn, đã có trong journal (chưa superseded) HOẶC đứng
    /// trước trong lô (chứng minh tương đương: max version các dòng chưa superseded = max version mọi dòng trước).
    /// IDENTITY cấp theo ORDER BY Ord của INSERT…SELECT (SQL Server bảo đảm) ⇒ ghép Seq tăng dần với Ord tăng dần.
    /// </summary>
    internal async Task<IReadOnlyList<JournalAppendOutput>> AppendBatchSqlAsync(IReadOnlyList<JournalAppendInput> entries, CancellationToken ct)
    {

        var normalized = new DateTime[entries.Count];
        var buffer = new System.Buffers.ArrayBufferWriter<byte>(entries.Count * 256);
        using (var w = new Utf8JsonWriter(buffer))
        {
            w.WriteStartArray();
            for (var i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                normalized[i] = QuestPayloadHelper.NormalizeTs(e.DesignatedTsUtc);
                w.WriteStartObject();
                w.WriteNumber("o", i);
                w.WriteString("t", e.TargetTable);
                w.WriteString("k", e.EventKey);
                w.WriteString("ts", normalized[i].ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff", System.Globalization.CultureInfo.InvariantCulture));
                w.WriteString("p", e.Payload);
                w.WriteNumber("s", e.SchemaVer);
                if (e.Version is { } v) w.WriteNumber("v", v); else w.WriteNull("v");
                w.WriteEndObject();
            }

            w.WriteEndArray();
        }

        var json = System.Text.Encoding.UTF8.GetString(buffer.WrittenSpan);
        var rows = await _db(ct).Ado.SqlQueryAsync<BatchAckDb>(QuestJournalSql.AppendBatchSql,
            new SugarParameter("@Batch", json, System.Data.DbType.String) { Size = -1 });
        if (rows.Count != entries.Count)
            throw new InvalidOperationException($"Journal trả {rows.Count} ACK cho lô {entries.Count} dòng.");

        var acks = new JournalAppendOutput[entries.Count];
        foreach (var r in rows)
        {
            var e = entries[r.Ord];
            acks[r.Ord] = QuestJournalMapper.ToBatchAppend(r, e, normalized[r.Ord]);
        }

        return acks;
    }

    internal async Task<JournalAppendOutput> AppendSingleAsync(JournalAppendInput entry, CancellationToken ct)
    {

        try
        {
            var rows = await _db(ct).Ado.SqlQueryAsync<AckDb>(QuestJournalSql.AppendSingle,
                P("@TargetTable", entry.TargetTable),
                P("@EventKey", entry.EventKey),
                P("@DesignatedTs", QuestPayloadHelper.NormalizeTs(entry.DesignatedTsUtc)),
                P("@Payload", entry.Payload),
                P("@SchemaVer", entry.SchemaVer),
                new SugarParameter("@Version", (object?)entry.Version ?? DBNull.Value, System.Data.DbType.Int64));

            var r = rows.Single();
            return QuestJournalMapper.ToAppend(r, entry);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new JournalAppendException($"Journal CHƯA ACK (SQL lỗi): {QuestErrorClassifierHelper.SafeDescribe(ex)}", ex);
        }
    }

}
