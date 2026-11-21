using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.QuestHa.Infrastructure.Services.Health;

namespace Shared.QuestHa.Infrastructure.Services.Health.Workers;

/// <summary>
/// Author: KienLT
/// Description: Đánh giá sức khoẻ định kỳ và cảnh báo (log + timeline). Tăng mức báo ngay; đổi lý do cùng mức hoặc hạ mức chỉ báo
/// sau <see cref="QuestHaHealthOptions.MinAlertGap"/>; xấu kéo dài nhắc theo <see cref="QuestHaHealthOptions.RepeatWarningEvery"/>.
/// Chỉ cảnh báo, không tự sửa hay xoá gì. Chạy ở host sở hữu replicator, không chạy khi Direct hoặc không có node.
/// Created date: 28/10/2026
/// </summary>
internal sealed class QuestHaHealthWorker : BackgroundService
{
    private readonly QuestHaHealthService _health;
    private readonly QuestHaHealthOptions _options;
    private readonly QuestHaOptions _config;
    private readonly QuestHaTimelineService _timeline;
    private readonly TimeProvider _clock;
    private readonly ILogger<QuestHaHealthWorker> _logger;
    private readonly QuestHaSettingsService? _settings;

    private QuestHaHealthLevelEnum _lastLevel = QuestHaHealthLevelEnum.Ok;
    private string _lastCodes = "";
    private DateTime _lastWarnUtc = DateTime.MinValue;

    /// <summary>Khởi tạo worker giám sát.</summary>
    public QuestHaHealthWorker(QuestHaHealthService health, QuestHaHealthOptions options, IOptions<QuestHaOptions> config,
        QuestHaTimelineService timeline, TimeProvider clock, ILogger<QuestHaHealthWorker> logger, QuestHaSettingsService? settings = null)
    {
        _settings = settings;
        _health = health;
        _options = options;
        _config = config.Value;
        _timeline = timeline;
        _clock = clock;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Direct/không node: không có gì để giám sát (không mở kết nối journal).
        if (_config.WriteMode == QuestWriteModeEnum.Direct || _config.Nodes.Count == 0) return;

        // Chờ replicator kịp ghi nhịp sống đầu tiên: nếu không, dòng status cũ của phiên trước báo Critical giả lúc khởi động.
        try { await Task.Delay(Current.HeartbeatStaleAfter, stoppingToken).ConfigureAwait(false); }
        catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                Report(await _health.CheckAsync(stoppingToken).ConfigureAwait(false));
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                // Giám sát lỗi không được làm rơi host.
                // chi tiết SQL chỉ ở Debug, không đưa tài khoản/kết nối vào cảnh báo.
                _logger.LogSafeWarning(ex, "[QuestHa] Vòng giám sát lỗi.");
            }

            // Chu kỳ đọc lại mỗi vòng: đổi CheckInterval có hiệu lực từ lượt chờ kế tiếp.
            var interval = Current.CheckInterval;
            var every = interval > TimeSpan.Zero ? interval : TimeSpan.FromSeconds(30);
            try { await Task.Delay(every, stoppingToken).ConfigureAwait(false); }
            catch (OperationCanceledException) { return; }
        }
    }

    private QuestHaHealthOptions Current => _settings?.Current.Health ?? _options;

    /// <summary>Quyết định có cảnh báo không (xem mô tả lớp). Trạng thái "đã biết" chỉ cập nhật khi đã báo hoặc về Ok.</summary>
    internal void Report(QuestHaHealthOutput h)
    {
        var now = _clock.GetUtcNow().UtcDateTime;
        // Chụp một lần cho cả lượt báo: MinAlertGap/RepeatWarningEvery đổi được lúc chạy (VdsConfig).
        var options = Current;
        var codes = string.Join(',', h.Reasons.Select(r => r.Code + ":" + r.Node).OrderBy(x => x, StringComparer.Ordinal));
        var changed = h.Level != _lastLevel || codes != _lastCodes;

        if (h.Level == QuestHaHealthLevelEnum.Ok)
        {
            if (_lastLevel != QuestHaHealthLevelEnum.Ok)
            {
                // Chống nhảy Ok<->Warn: chưa đủ MinAlertGap kể từ lần báo trước thì chờ lượt sau (giữ nguyên trạng thái đã biết).
                if (_clock.GetUtcNow().UtcDateTime - _lastWarnUtc < options.MinAlertGap) return;
                _lastWarnUtc = _clock.GetUtcNow().UtcDateTime;
                _logger.LogInformation("[QuestHa] Sức khoẻ đã trở lại bình thường.");
                _timeline.Publish("Health", null, QuestHaEventKindEnum.Info, "Sức khoẻ QuestDB HA trở lại bình thường.");
            }
            _lastLevel = h.Level;
            _lastCodes = codes;
            return;
        }

        var escalated = h.Level == QuestHaHealthLevelEnum.Critical && _lastLevel != QuestHaHealthLevelEnum.Critical;
        var gapPassed = now - _lastWarnUtc >= options.MinAlertGap;
        var repeatDue = now - _lastWarnUtc >= options.RepeatWarningEvery;
        if (!(escalated || (changed && gapPassed) || repeatDue)) return;

        var text = string.Join(" | ", h.Reasons.Where(r => r.Level == h.Level).Select(r => r.Message));
        if (h.Level == QuestHaHealthLevelEnum.Critical) _logger.LogError("[QuestHa] CRITICAL: {Text}", text);
        else _logger.LogWarning("[QuestHa] WARN: {Text}", text);
        _timeline.Publish("Health", null,
            h.Level == QuestHaHealthLevelEnum.Critical ? QuestHaEventKindEnum.Error : QuestHaEventKindEnum.Info, $"{h.Level}: {text}");
        _lastWarnUtc = now;
        _lastLevel = h.Level;
        _lastCodes = codes;
    }
}
