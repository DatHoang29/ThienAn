using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.QuestHa.Infrastructure.Services.Health.Rules;

namespace Shared.QuestHa.Infrastructure.Services.Health;

/// <summary>
/// Author: KienLT
/// Description: Tổng hợp sức khoẻ QuestDB HA từ journal: trạng thái từng node CẤU HÌNH, độ trễ, WAL, đối soát, backup, quarantine.
/// Chỉ ĐỌC journal (không chạm QuestDB node) nên gọi được từ mọi host, kể cả WebAPI reader-only.
/// Số đo journal lấy từ metadata (không COUNT toàn bảng) để chu kỳ 30 s không tốn theo kích thước journal.
/// Created date: 28/10/2026
/// </summary>
internal sealed class QuestHaHealthService
{
    private readonly IJournalStore _store;
    private readonly QuestHaOptions _config;
    private readonly TimeProvider _clock;
    private readonly ILogger _logger;
    private readonly QuestHaSettingsService? _settings;
    private readonly IReadOnlyList<string> _tables;

    /// <summary>Khởi tạo dịch vụ giám sát; <paramref name="settings"/> có thì ngưỡng đọc từ snapshot vận hành mỗi lượt.</summary>
    public QuestHaHealthService(IJournalStore store, QuestHaOptions config, TimeProvider clock, ILogger? logger = null,
        QuestHaSettingsService? settings = null, IReadOnlyList<QuestTableSpec>? tables = null)
    {
        _tables = tables?.Select(t => t.Name).ToArray() ?? Array.Empty<string>();
        _store = store;
        _config = config;
        _settings = settings;
        _clock = clock;
        _logger = logger ?? NullLogger.Instance;
    }

    /// <summary>
    /// Đánh giá sức khoẻ hiện tại. Không ném lỗi khi journal không đọc được (lỗi SQL/IO/timeout): trả mức Critical
    /// (<c>journal.unreachable</c>) để bên gọi xử lý thống nhất. Lỗi khác (lập trình) vẫn được ném để lộ ra.
    /// Chỉ lỗi huỷ (<paramref name="ct"/>) được ném lại nguyên.
    /// </summary>
    public async Task<QuestHaHealthOutput> CheckAsync(CancellationToken ct = default)
    {
        // Chụp ngưỡng một lần cho cả lượt đánh giá.
        var current = _settings?.Current ?? _config;
        var health = current.Health;
        try
        {
            var statuses = await _store.ReadStatusesAsync(ct).ConfigureAwait(false);
            var backlog = await _store.ReadJournalBacklogAsync(_config.Nodes.Select(n => n.Name).ToArray(), ct).ConfigureAwait(false);
            var backups = await _store.GetBackupsAsync(null, ct).ConfigureAwait(false);
            var (exports, exportError) = current.Export.Enabled ? await ReadExportsAsync(ct).ConfigureAwait(false) : (null, null);
            var result = QuestHaHealthRules.Evaluate(_clock.GetUtcNow().UtcDateTime, _config.Nodes.Select(n => n.Name).ToArray(), statuses,
                backlog.TotalRows, (int)Math.Min(int.MaxValue, backlog.OpenQuarantineCount), backups, health,
                exports, current.Export, _tables);
            if (exportError is null) return result;
            // Catalog xuất không đọc được (thiếu migration?) không làm cả journal thành "không đọc được".
            var reasons = result.Reasons.Append(new(QuestHaHealthLevelEnum.Critical, "export.catalogUnreadable",
                $"Không đọc được catalog bản xuất Parquet ({exportError}); chạy migration 20261102_QuestJournal_Add_ExportCatalog.sql.", null)).ToList();
            return result with { Level = QuestHaHealthRules.Worst(reasons), Reasons = reasons };
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is Microsoft.Data.SqlClient.SqlException or SqlSugar.SqlSugarException or TimeoutException or IOException or InvalidOperationException)
        {
            // Chỉ TÊN kiểu + mã lỗi: thông điệp lỗi SQL có thể chứa chuỗi kết nối. Chi tiết đầy đủ ở Debug.
            // SqlSugar bọc lỗi mở kết nối: lấy mã từ SqlException bên trong nếu có.
            var code = (ex as Microsoft.Data.SqlClient.SqlException ?? ex.InnerException as Microsoft.Data.SqlClient.SqlException) is { } sql ? $" #{sql.Number}" : "";
            _logger.LogWarning("[QuestHa] Không đọc được journal để giám sát: {Type}{Code}.", ex.GetType().Name, code);
            // Debug chỉ nhận exception đã làm sạch (không object exception).
            _logger.LogSafeDebug(ex, "[QuestHa] Chi tiết lỗi đọc journal.");
            return QuestHaHealthRules.JournalUnreachable(_clock.GetUtcNow().UtcDateTime, ex.GetType().Name + code);
        }
    }

    private async Task<(IReadOnlyList<QuestExportRecordOutput>? Exports, string? Error)> ReadExportsAsync(CancellationToken ct)
    {
        try { return (await _store.ListExportsAsync(null, null, null, ct).ConfigureAwait(false), null); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ex is Microsoft.Data.SqlClient.SqlException or SqlSugar.SqlSugarException or TimeoutException or IOException or NotSupportedException)
        {
            _logger.LogWarning("[QuestHa] Không đọc được catalog bản xuất: {Type}.", ex.GetType().Name);
            return (null, ex.GetType().Name);
        }
    }
}
