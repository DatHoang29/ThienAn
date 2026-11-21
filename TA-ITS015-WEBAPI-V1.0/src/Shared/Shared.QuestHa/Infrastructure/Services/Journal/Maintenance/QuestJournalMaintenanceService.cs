using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Shared.QuestHa.Infrastructure.Services.Journal.Maintenance;

/// <summary>
/// Author: KienLT
/// Description: Cửa gọi cho worker/CLI/giám sát; Critical chỉ cảnh báo có throttle, append luôn độc lập.
/// Created date: 28/10/2026
/// </summary>
internal sealed class QuestJournalMaintenanceService
{
    private readonly IJournalStore _store;
    private readonly QuestHaOptions _options;
    private readonly TimeProvider _clock;
    private readonly QuestHaTimelineService _timeline;
    private readonly ILogger<QuestJournalMaintenanceService> _logger;
    private readonly object _alertSync = new();
    private DateTime? _lastAlertUtc;
    private JournalBacklogLevelEnum _lastLevel;
    private string? _purgeAgeWarning;
    private readonly QuestHaSettingsService? _settings;

    /// <summary>Tạo singleton; Nodes phải đúng tập replicator Worker, không gồm SourceNode Shadow.</summary>
    public QuestJournalMaintenanceService(IJournalStore store, QuestHaOptions options, TimeProvider? clock = null,
        QuestHaTimelineService? timeline = null, ILogger<QuestJournalMaintenanceService>? logger = null,
        QuestHaSettingsService? settings = null)
    {
        _settings = settings;
        _store = store;
        _options = options;
        _clock = clock ?? TimeProvider.System;
        _timeline = timeline ?? new QuestHaTimelineService(clock: _clock);
        _logger = logger ?? NullLogger<QuestJournalMaintenanceService>.Instance;
    }

    /// <summary>Đo/phân mức backlog; log và timeline có throttle, tăng mức cảnh báo phát ngay.</summary>
    public async Task<JournalBacklogOutput> GetBacklogAsync(CancellationToken ct = default)
    {
        // Chụp ngưỡng một lần cho cả lượt đo và cảnh báo.
        var maintenance = Maintenance;
        maintenance.Validate();
        var critical = maintenance.CriticalPendingRows;
        var measured = await _store.ReadJournalBacklogAsync(_options.Nodes.Select(n => n.Name).ToArray(),
            critical == long.MaxValue ? critical : critical + 1, ct).ConfigureAwait(false);
        var result = QuestJournalBacklogRules.Evaluate(measured, maintenance);
        var ageWarning = Volatile.Read(ref _purgeAgeWarning);
        if (ageWarning is not null)
            result = result with
            {
                Level = result.Level < JournalBacklogLevelEnum.Warn ? JournalBacklogLevelEnum.Warn : result.Level,
                Reasons = result.Reasons.Append("Warn: " + ageWarning).ToArray(),
            };
        lock (_alertSync)
        {
            var now = _clock.GetUtcNow().UtcDateTime;
            var publish = result.Level != JournalBacklogLevelEnum.Ok &&
                (_lastAlertUtc is null || result.Level > _lastLevel || now - _lastAlertUtc >= maintenance.AlertThrottle);
            _lastLevel = result.Level;
            if (publish)
            {
                _lastAlertUtc = now;
                var message = string.Join("; ", result.Reasons);
                if (result.Level == JournalBacklogLevelEnum.Critical) _logger.LogError("[QuestHa] Backlog {Reasons}", message);
                else _logger.LogWarning("[QuestHa] Backlog {Reasons}", message);
                _timeline.Publish("JournalBacklog", null,
                    result.Level == JournalBacklogLevelEnum.Critical ? QuestHaEventKindEnum.Error : QuestHaEventKindEnum.Info, message);
            }
        }
        return result;
    }

    /// <summary>Dọn tay mặc định dry-run, áp mọi giới hạn/khoá SQL; Enabled chỉ điều khiển lịch nền.</summary>
    public Task<JournalPurgeOutput> PurgeAsync(bool dryRun = true, CancellationToken ct = default) => PurgeAsync(dryRun, Maintenance, ct);

    /// <summary>Như <see cref="PurgeAsync(bool, CancellationToken)"/> với chính sách đã chụp sẵn của vòng gọi (worker nền).</summary>
    internal async Task<JournalPurgeOutput> PurgeAsync(bool dryRun, JournalMaintenanceOptions maintenance, CancellationToken ct)
    {
        // Lượt purge giữ nguyên giới hạn đã chụp tới hết lượt; đổi tham số áp từ lượt sau.
        maintenance.Validate();
        if (_options.Nodes.Count == 0) return new(dryRun, 0, 0, false, null, "NoAssignedNodes");
        if (_options.WriteMode == QuestWriteModeEnum.Direct)
            return new(dryRun, 0, 0, false, null, "DirectWriteMode");
        var result = await _store.PurgeJournalAsync(_options.Nodes.Select(n => n.Name).ToArray(), maintenance, dryRun, ct).ConfigureAwait(false);
        // Giữ chẩn đoán gần nhất cho backlog của singleton; lượt thành công kế tiếp xoá cảnh báo tuổi.
        // Lý do chặn khác không chứng minh được bất thường đã hết nên vẫn giữ cảnh báo.
        if (result.Reason?.StartsWith("NonMonotonicCreatedUtc:", StringComparison.Ordinal) == true || result.Reason is null)
            Volatile.Write(ref _purgeAgeWarning, result.Reason);
        return result;
    }

    /// <summary>Chính sách bảo trì đang chạy (snapshot vận hành nếu có, không thì cấu hình khởi động).</summary>
    internal JournalMaintenanceOptions Maintenance => _settings?.Current.JournalMaintenance ?? _options.JournalMaintenance;

    /// <summary>Gỡ cursor ngoài tập đích cấu hình; operator phải dừng replicator cũ trước khi apply.</summary>
    public Task<JournalCursorRetireOutput> RetireCursorAsync(string node, bool dryRun = true, CancellationToken ct = default) =>
        _store.RetireJournalCursorAsync(node, _options.Nodes.Select(n => n.Name).ToArray(), dryRun, ct);
}
