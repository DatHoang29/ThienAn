using SqlSugar;
using static Shared.QuestHa.Infrastructure.Services.Journal.Statements.QuestJournalParameter;
using Shared.QuestHa.Infrastructure.Services.Journal.Statements;
using Shared.QuestHa.Infrastructure.Services.Journal.Mapping;
using static Shared.QuestHa.Infrastructure.Services.Journal.Mapping.QuestJournalMapper;

namespace Shared.QuestHa.Infrastructure.Services.Journal;

/// <summary>
/// Author: KienLT
/// Description: Truy cập catalog backup với chính sách bảo vệ dùng chung của journal.
/// Created date: 31/10/2026
/// </summary>
internal sealed class QuestJournalBackupCatalogService
{
    private readonly Func<CancellationToken, SqlSugarClient> _db;
    private readonly Func<JournalMaintenanceOptions> _maintenance;
    private readonly TimeProvider _clock;
    internal QuestJournalBackupCatalogService(Func<CancellationToken, SqlSugarClient> db, Func<JournalMaintenanceOptions> maintenance, TimeProvider clock)
    {
        _db = db;
        _maintenance = maintenance;
        _clock = clock;
    }

    internal async Task<IReadOnlyList<BackupRecordOutput>> GetBackupsAsync(string? node = null, CancellationToken ct = default)
    {
        // RetiredUtc hiệu lực được suy theo cùng cửa sổ bảo vệ với SQL purge.
        var rows = await _db(ct).Ado.SqlQueryAsync<BackupDb>(
            QuestJournalSql.GetBackups,
            new SugarParameter("@node", (object?)node ?? DBNull.Value, System.Data.DbType.String));
        return QuestBackupProtectionRules.Apply(rows.Select(ToBackup).ToList(), _clock.GetUtcNow().UtcDateTime, _maintenance().ProtectedBackupDays);
    }

    // ghi cả RetiredUtc trong schema lifecycle, không fallback khi thiếu migration.
    internal Task AddBackupAsync(BackupRecordOutput r, CancellationToken ct = default) =>
        _db(ct).Ado.ExecuteCommandAsync(
            QuestJournalSql.AddBackup,
            P("@id", r.BackupId), P("@node", r.Node), P("@seq", r.WatermarkSeq), P("@ts", r.WatermarkTsUtc),
            P("@path", r.Path), P("@size", r.SizeBytes), P("@ver", r.Verified), P("@created", r.CreatedUtc), P("@retired", r.RetiredUtc));

    internal async Task<BackupRecordOutput?> GetBackupAsync(Guid backupId, CancellationToken ct = default)
    {
        // restore nhận cờ retired hiệu lực, kể cả retire theo tuổi.
        return (await GetBackupsAsync(ct: ct)).FirstOrDefault(b => b.BackupId == backupId);
    }

    // snapshot catalog để preflight phát hiện thiếu migration trước downtime.
    internal Task<IReadOnlyList<BackupRecordOutput>> ListBackupsAsync(CancellationToken ct = default) => GetBackupsAsync(ct: ct);

    // retire tường minh không được phá ghim của bản Verified mới nhất.
    /// <summary>Retire/unretire tường minh; bản Verified mới nhất luôn ghim và không được retire.</summary>
    internal async Task<bool> SetBackupRetiredAsync(Guid id, bool retired, bool dryRun = true, CancellationToken ct = default)
    {
        await _db(ct).Ado.ExecuteCommandAsync(QuestJournalSql.SetBackupRetired, P("@id", id), P("@retired", retired), P("@dry", dryRun));
        return !dryRun;
    }

}
