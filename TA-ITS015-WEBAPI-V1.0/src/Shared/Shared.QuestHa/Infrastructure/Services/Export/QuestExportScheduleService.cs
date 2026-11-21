using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.QuestHa.Infrastructure.Services.Export.Rules;

namespace Shared.QuestHa.Infrastructure.Services.Export;

/// <summary>
/// Author: KienLT
/// Description: Lịch xuất Parquet hằng ngày trong host Writer: từ giờ lịch (giờ server) xuất trọn HÔM QUA, kiểm lại N ngày trước (dữ liệu
/// đến muộn / ngày bị lỡ), rồi dọn thư mục hết hạn. Lỗi ⇒ thử lại sau RetryDelay, tối đa MaxAttempts trong ngày; DryRun ⇒ chỉ xem trước một lần/ngày.
/// Trạng thái lượt thử giữ trong bộ nhớ: restart host thì đếm lại (giờ đã xuất xong vẫn không xuất lại nhờ manifest/tiến độ trên đĩa).
/// Created date: 02/11/2026
/// </summary>
internal sealed class QuestExportScheduleService
{
    private readonly QuestExportService _export;
    private readonly Func<QuestHaOptions> _runtime;
    private readonly QuestHaTimelineService? _timeline;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private DateOnly? _day;
    private int _attempts;
    private DateTime? _lastFailureUtc;
    // Xem trước xong không chặn xuất thật cùng ngày: tắt DryRun giữa ngày thì lượt sau xuất thật luôn.
    private bool _previewDone;
    private bool _done;

    /// <summary>Lịch trên dịch vụ xuất của cùng host; <paramref name="runtime"/> = snapshot vận hành.</summary>
    public QuestExportScheduleService(QuestExportService export, Func<QuestHaOptions> runtime, QuestHaTimelineService? timeline = null,
        ILogger? logger = null)
    {
        _export = export;
        _runtime = runtime;
        _timeline = timeline;
        _logger = logger ?? NullLogger.Instance;
    }

    /// <summary>Số lần đã thử của ngày đang theo dõi (chẩn đoán/test).</summary>
    public int Attempts => _attempts;

    /// <summary>
    /// Chạy nếu đến hạn ở <paramref name="nowLocal"/> (giờ server) / <paramref name="nowUtc"/>. Trả true khi vừa chạy một lượt (thành công hay lỗi).
    /// Không ném lỗi xuất (log Warning + timeline); chỉ huỷ được ném lại.
    /// </summary>
    public async Task<bool> RunDueAsync(DateTime nowLocal, DateTime nowUtc, CancellationToken ct)
    {
        var options = _runtime().Export;
        if (!options.Enabled) return false;
        var due = QuestExportScheduleRules.DueDay(nowLocal, options.Schedule);
        if (due is null) return false;
        if (!await _gate.WaitAsync(0, ct).ConfigureAwait(false)) return false;
        try
        {
            if (_day != due) { _day = due; _attempts = 0; _lastFailureUtc = null; _done = false; _previewDone = false; }
            if (!QuestExportScheduleRules.ShouldAttempt(options.DryRun ? _done || _previewDone : _done, _attempts, _lastFailureUtc, nowUtc, options.MaxAttempts, options.RetryDelay))
                return false;
            _attempts++;
            var day = due.Value;
            if (options.DryRun)
            {
                var preview = await _export.ExportAsync(day, apply: false, ct).ConfigureAwait(false);
                var expired = await _export.RetainAsync(DateOnly.FromDateTime(nowLocal), apply: false, ct).ConfigureAwait(false);
                _logger.LogInformation("[QuestHa.Export] DryRun {Day:yyyy-MM-dd}: node {Node}, {Rows} dòng, ước {Bytes} byte, trống {Free}, hoãn={Reason}; {Expired} thư mục sẽ hết hạn.",
                    day, preview.Node, preview.Tables.Sum(t => t.Rows), preview.EstimatedBytes, preview.FreeBytes, preview.DeferredReason, expired);
                _timeline?.Publish("Export", preview.Node, QuestHaEventKindEnum.Info,
                    $"DryRun xuất {day:yyyy-MM-dd}: {preview.Tables.Sum(t => t.Rows)} dòng, node {preview.Node ?? "-"}{(preview.DeferredReason is null ? "" : ", hoãn " + preview.DeferredReason)}.");
                if (preview.DeferredReason is null) _previewDone = true;
                else _lastFailureUtc = nowUtc;
                return true;
            }

            var failures = new List<string>();
            var main = await _export.ExportAsync(day, apply: true, ct).ConfigureAwait(false);
            if (!main.Completed) failures.Add($"{day:yyyy-MM-dd}: {main.DeferredReason}");
            // Kiểm lại ngày trước chỉ khi node/coverage ổn (lượt chính không hoãn vì node/coverage).
            if (main.DeferredReason is null or "TableFailed")
                foreach (var previous in QuestExportScheduleRules.RecheckDays(day, options.RecheckDays))
                {
                    var recheck = await _export.ExportAsync(previous, apply: true, ct).ConfigureAwait(false);
                    if (!recheck.Completed) failures.Add($"{previous:yyyy-MM-dd}: {recheck.DeferredReason}");
                }
            var removed = await _export.RetainAsync(DateOnly.FromDateTime(nowLocal), apply: true, ct).ConfigureAwait(false);
            if (failures.Count == 0)
            {
                _done = true;
                _logger.LogInformation("[QuestHa.Export] Xuất {Day:yyyy-MM-dd} xong trên node {Node} (watermark {Seq}, {Rows} dòng, {Bytes} byte); xoá {Removed} thư mục hết hạn.",
                    day, main.Node, main.WatermarkSeq, main.Tables.Sum(t => t.Rows), main.Tables.Sum(t => t.Bytes), removed);
                _timeline?.Publish("Export", main.Node, QuestHaEventKindEnum.Info, $"Xuất {day:yyyy-MM-dd} xong ({main.Tables.Sum(t => t.Rows)} dòng).");
                return true;
            }
            Fail(nowUtc, options, string.Join("; ", failures));
            return true;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            _logger.LogSafeWarning(ex, "[QuestHa.Export] Lượt xuất lỗi.");
            Fail(nowUtc, options, ex.GetType().Name);
            return true;
        }
        finally { _gate.Release(); }
    }

    private void Fail(DateTime nowUtc, ExportOptions options, string reason)
    {
        _lastFailureUtc = nowUtc;
        var last = _attempts >= options.MaxAttempts;
        _logger.LogWarning("[QuestHa.Export] Lượt xuất {Day:yyyy-MM-dd} chưa xong (lần {Attempt}/{Max}): {Reason}.{Last}",
            _day, _attempts, options.MaxAttempts, reason, last ? " Hết lượt thử trong ngày." : $" Thử lại sau {options.RetryDelay}.");
        _timeline?.Publish("Export", null, QuestHaEventKindEnum.Error,
            $"Xuất {_day:yyyy-MM-dd} chưa xong (lần {_attempts}/{options.MaxAttempts}): {reason}");
    }
}
