using Microsoft.Extensions.Logging;
using Shared.QuestHa.Infrastructure.Services.Replication.Snapshot;
using Shared.QuestHa.Infrastructure.Services.Replication.Delivery;

namespace Shared.QuestHa.Infrastructure.Services.Replication;

/// <summary>
/// Author: KienLT
/// Description: Giao lô theo bảng, cô lập lỗi dữ liệu và ghi quarantine trước khi cursor được tiến.
/// Created date: 31/10/2026
/// </summary>
internal sealed class QuestReplicatorDeliveryService
{
    private readonly QuestReplicatorState _state;
    private readonly IJournalStore _store;
    private readonly IQuestNodeClient _client;
    private readonly IReadOnlyDictionary<string, QuestTableSpec> _tables;
    private readonly ReplicatorOptions _options;
    private readonly TimeProvider _clock;
    private readonly QuestHaTimelineService? _timeline;
    private readonly ILogger _logger;
    private readonly Func<CancellationToken, Task<bool>> _probeAsync;
    private readonly Action<string> _markWritten;
    private readonly Action<string> _setError;

    internal QuestReplicatorDeliveryService(QuestReplicatorState state, IJournalStore store, IQuestNodeClient client, IReadOnlyDictionary<string, QuestTableSpec> tables, ReplicatorOptions options, TimeProvider clock, QuestHaTimelineService? timeline, ILogger logger, Func<CancellationToken, Task<bool>> probe, Action<string> markWritten, Action<string> setError)
    {
        _state = state;
        _store = store;
        _client = client;
        _tables = tables;
        _options = options;
        _clock = clock;
        _timeline = timeline;
        _logger = logger;
        _probeAsync = probe;
        _markWritten = markWritten;
        _setError = setError;
    }

    private string Node => _client.Node;

    internal async Task<DeliverOutcome> DeliverAsync(IReadOnlyList<JournalRowOutput> batch, CancellationToken ct)
    {
        // Giữ thứ tự theo Seq trong từng bảng; các bảng giao lần lượt.
        foreach (var group in batch.Where(r => !r.Superseded).GroupBy(r => r.TargetTable, StringComparer.OrdinalIgnoreCase))
        {
            if (!_tables.TryGetValue(group.Key, out var spec))
            {
                foreach (var r in group) await QuarantineAsync(r.Seq, $"Không có spec cho bảng '{group.Key}'.", ct);
                continue;
            }

            var rows = new List<QuestRow>();
            foreach (var r in group)
            {
                try
                {
                    rows.Add(QuestPayloadHelper.ToRow(spec, r.Seq, r.Payload));
                }
                catch (PayloadConversionException ex)
                {
                    // lỗi chuyển payload do thư viện tạo không phải provider SQL; giữ tên cột để sửa dữ liệu.
                    await QuarantineAsync(r.Seq, QuestErrorClassifierHelper.Describe(ex, 300), ct);
                }
            }

            var merged = QuestBatchMergeHelper.MergeLastPerKey(spec, rows);
            if (merged.Count == 0) continue;

            try
            {
                await _client.WriteBatchAsync(spec, merged, ct);
                _markWritten(spec.Name);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                if (await HandleWriteFailureAsync(spec, merged, ex, ct) == DeliverOutcome.Fenced)
                    return DeliverOutcome.Fenced;
            }
        }

        return DeliverOutcome.Delivered;
    }

    internal async Task<DeliverOutcome> HandleWriteFailureAsync(QuestTableSpec spec, IReadOnlyList<QuestRow> rows, Exception ex, CancellationToken ct)
    {
        // Debug chỉ nhận exception đã làm sạch (không object exception).
        _logger.LogSafeDebug(ex, "[QuestHa.Replicator] Chi tiết ghi {Table} trên {Node}.", spec.Name, Node);
        var kind = QuestErrorClassifierHelper.Classify(ex);
        if (kind == QuestErrorKindEnum.Connectivity) throw new NodeConnectivityException(ex);

        // Xác nhận lại node trước khi đổ lỗi cho dữ liệu: thiếu bảng / WAL suspend thì rào node, không quarantine.
        if (kind == QuestErrorKindEnum.Node || !await _probeAsync(ct))
        {
            // Probe thấy thiếu bảng/WAL suspend ⇒ rào theo probe (đã ghi timeline khi đổi), không thêm rào ghi.
            if (_state.AutoFence is not null) return DeliverOutcome.Fenced;
            SetWriteFence($"Ghi {spec.Name} lỗi phía node: {QuestErrorClassifierHelper.SafeDescribe(ex)}");
            return DeliverOutcome.Fenced;
        }

        // Lỗi dữ liệu: cô lập từng dòng.
        foreach (var row in rows)
        {
            try
            {
                await _client.WriteBatchAsync(spec, new[] { row }, ct);
                _markWritten(spec.Name);
            }
            catch (Exception rex) when (!ct.IsCancellationRequested)
            {
                // Chỉ log exception đã làm sạch để tránh lộ thông tin kết nối.
                _logger.LogSafeDebug(rex, "[QuestHa.Replicator] Chi tiết ghi dòng trên {Node}.", Node);
                var rk = QuestErrorClassifierHelper.Classify(rex);
                if (rk == QuestErrorKindEnum.Connectivity) throw new NodeConnectivityException(rex);
                if (rk == QuestErrorKindEnum.Node)
                {
                    SetWriteFence($"Ghi {spec.Name} lỗi phía node: {QuestErrorClassifierHelper.SafeDescribe(rex)}");
                    return DeliverOutcome.Fenced;
                }

                await QuarantineAsync(row.Seq, QuestErrorClassifierHelper.SafeDescribe(rex), ct);
            }
        }

        return DeliverOutcome.Delivered;
    }

    /// <summary>đặt/gia hạn rào ghi với backoff luỹ thừa; chỉ ghi timeline khi bắt đầu hoặc đổi thông điệp.</summary>
    internal void SetWriteFence(string message)
    {
        var now = _clock.GetUtcNow().UtcDateTime;
        string? publish = null;
        if (_state.WriteFence is null)
        {
            _state.WriteFenceBackoff = _options.BackoffInitial;
            _state.WriteFenceRepeats = 0;
            publish = $"{Node} bị rào: {message} (thử lại theo backoff {_options.BackoffInitial.TotalSeconds:0.#}→{_options.BackoffMax.TotalSeconds:0.#}s)";
        }
        else
        {
            _state.WriteFenceRepeats++;
            var next = TimeSpan.FromTicks(Math.Min(_options.BackoffMax.Ticks, _state.WriteFenceBackoff.Ticks * 2));
            _state.WriteFenceBackoff = next < _options.BackoffInitial ? _options.BackoffInitial : next;
            if (!string.Equals(message, _state.WriteFence, StringComparison.Ordinal))
                publish = $"{Node} bị rào (lỗi mới sau {_state.WriteFenceRepeats} lần thử lại): {message}";
        }

        _state.WriteFence = message;
        _state.WriteFenceRetryAt = now + _state.WriteFenceBackoff;
        _setError(message);
        if (publish is not null) _timeline?.Publish("Replicator", Node, QuestHaEventKindEnum.Error, publish);
    }

    internal void ClearWriteFence()
    {
        if (_state.WriteFence is null) return;
        var msg = $"{Node}: hết rào ghi sau {_state.WriteFenceRepeats} lần thử lại (lỗi cuối: {_state.WriteFence})";
        _state.WriteFence = null;
        _state.WriteFenceRepeats = 0;
        _state.WriteFenceBackoff = _options.BackoffInitial;
        _logger.LogInformation("[QuestHa.Replicator] {Msg}", msg);
        _timeline?.Publish("Replicator", Node, QuestHaEventKindEnum.Info, msg);
    }

    internal async Task QuarantineAsync(long seq, string error, CancellationToken ct)
    {
        try
        {
            await _store.QuarantineAsync(seq, Node, error, ct);
        }
        catch (Exception ex)
        {
            // Không ghi được quarantine thì KHÔNG được tiến cursor qua dòng này (sẽ mất dấu) ⇒ ném lỗi journal.
            throw new JournalException(ex);
        }

        Interlocked.Increment(ref _state.Quarantined);
        _timeline?.Publish("Replicator", Node, QuestHaEventKindEnum.Quarantined, $"{Node}: Seq {seq} vào quarantine — {error}");
    }

}
