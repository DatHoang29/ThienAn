using System.Text.Json;
using SqlSugar;
using Shared.QuestHa.Infrastructure.Services.Journal.Mapping;
using Shared.QuestHa.Infrastructure.Services.Journal.Statements;
using static Shared.QuestHa.Infrastructure.Services.Journal.Statements.QuestJournalParameter;
using static Shared.QuestHa.Infrastructure.Services.Journal.Mapping.QuestJournalMapper;

namespace Shared.QuestHa.Infrastructure.Services.Journal;

/// <summary>
/// Author: KienLT
/// Description: Catalog bản xuất Parquet trong DB journal (cần migration 20261102_QuestJournal_Add_ExportCatalog.sql).
/// Created date: 02/11/2026
/// </summary>
internal sealed class QuestJournalExportCatalogService
{
    private readonly Func<CancellationToken, SqlSugarClient> _db;

    internal QuestJournalExportCatalogService(Func<CancellationToken, SqlSugarClient> db) => _db = db;

    internal async Task<IReadOnlyList<QuestExportRecordOutput>> ListExportsAsync(string? table, DateOnly? from, DateOnly? to, CancellationToken ct)
    {
        var rows = await _db(ct).Ado.SqlQueryAsync<ExportDb>(QuestJournalExportSql.ListExports,
            new SugarParameter("@table", (object?)table ?? DBNull.Value, System.Data.DbType.String),
            new SugarParameter("@from", from is { } f ? f.ToDateTime(TimeOnly.MinValue) : DBNull.Value, System.Data.DbType.Date),
            new SugarParameter("@to", to is { } t ? t.ToDateTime(TimeOnly.MinValue) : DBNull.Value, System.Data.DbType.Date));
        return rows.Select(r => new QuestExportRecordOutput(r.ExportId, r.TableName, DateOnly.FromDateTime(r.DayLocal), Utc(r.FromUtc),
            Utc(r.ToUtc), r.Node, r.WatermarkSeq, r.RowTotal, r.Bytes, r.Path, r.Verified, Utc(r.CreatedUtc), Utc(r.RetiredUtc))).ToList();
    }

    internal Task AddExportAsync(QuestExportRecordOutput r, CancellationToken ct) =>
        _db(ct).Ado.ExecuteCommandAsync(QuestJournalExportSql.AddExport,
            P("@id", r.ExportId), P("@table", r.TableName),
            new SugarParameter("@day", r.DayLocal.ToDateTime(TimeOnly.MinValue), System.Data.DbType.Date),
            P("@from", r.FromUtc), P("@to", r.ToUtc), P("@node", r.Node), P("@seq", r.WatermarkSeq), P("@rows", r.Rows),
            P("@bytes", r.Bytes), P("@path", Trim(r.Path, 500)), P("@ver", r.Verified));

    internal async Task<int> RetireExportsAsync(IReadOnlyList<Guid> ids, CancellationToken ct)
    {
        if (ids.Count == 0) return 0;
        return await _db(ct).Ado.GetIntAsync(QuestJournalExportSql.RetireExports, P("@ids", JsonSerializer.Serialize(ids)));
    }

    internal async Task<IReadOnlyList<int>> ReadLateHoursAsync(string table, DateTime fromUtc, DateTime toUtc, long afterSeq, long maxSeq,
        CancellationToken ct)
    {
        if (maxSeq <= afterSeq) return Array.Empty<int>();
        var hours = await _db(ct).Ado.SqlQueryAsync<int>(QuestJournalExportSql.LateHours, P("@table", table), P("@from", fromUtc),
            P("@to", toUtc), P("@after", afterSeq), P("@max", maxSeq));
        return hours.OrderBy(h => h).ToList();
    }
}
