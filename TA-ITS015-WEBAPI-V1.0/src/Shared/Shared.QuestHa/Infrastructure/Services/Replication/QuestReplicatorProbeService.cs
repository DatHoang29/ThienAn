using Microsoft.Extensions.Logging;
using Shared.QuestHa.Infrastructure.Services.Replication.Rules;
using Shared.QuestHa.Infrastructure.Services.Replication.Snapshot;

namespace Shared.QuestHa.Infrastructure.Services.Replication;

/// <summary>
/// Author: KienLT
/// Description: Thăm dò WAL và rewind cursor sau reconnect trên cùng trạng thái replicator.
/// Created date: 31/10/2026
/// </summary>
internal sealed class QuestReplicatorProbeService
{
    private readonly QuestReplicatorState _state;
    private readonly IJournalStore _store;
    private readonly IQuestNodeClient _client;
    private readonly IReadOnlyDictionary<string, QuestTableSpec> _tables;
    private readonly ReplicatorOptions _options;
    private readonly TimeProvider _clock;
    private readonly QuestHaTimelineService? _timeline;
    private readonly ILogger _logger;
    private readonly Action<string> _setError;

    internal QuestReplicatorProbeService(QuestReplicatorState state, IJournalStore store, IQuestNodeClient client, IReadOnlyDictionary<string, QuestTableSpec> tables, ReplicatorOptions options, TimeProvider clock, QuestHaTimelineService? timeline, ILogger logger, Action<string> setError)
    {
        _state = state;
        _store = store;
        _client = client;
        _tables = tables;
        _options = options;
        _clock = clock;
        _timeline = timeline;
        _logger = logger;
        _setError = setError;
    }

    private string Node => _client.Node;

    /// <summary>false = không giao được (Fenced/không kết nối).</summary>
    internal async Task<bool> ProbeAndMaybeRewindAsync(CancellationToken ct)
    {
        // reconnect phải probe bằng pool đã vô hiệu; không tác động pool node khác.
        if (!_state.Connected) _client.ResetConnectionPool("reconnect trước probe");
        // epoch lúc BẮT ĐẦU probe; Begin/Arm/NotifyNodeRestarted xảy ra trong lúc probe ⇒ kết quả không xác nhận WAL.
        long probeEpoch;
        lock (_state.Sync) probeEpoch = _state.Epoch;
        NodeProbeOutput? bad = null;
        var walSuspended = false;
        var walApplied = true;
        // giữ mẫu riêng từng bảng, bảng nhanh không che bảng đứng WAL.
        var probes = new Dictionary<string, NodeProbeOutput>(StringComparer.OrdinalIgnoreCase);
        try
        {
            foreach (var spec in _tables.Values)
            {
                var p = await _client.ProbeAsync(spec, ct);
                probes[spec.Name] = p;
                walSuspended |= p.WalSuspended;
                if (!p.TableExists || p.WalSuspended) bad ??= p;
                else if (p.WriterTxn < p.SequencerTxn) walApplied = false;
            }
        }
        catch (Exception ex)
        {
            // bảng sau lỗi không được làm mất mẫu đã probe xong của bảng trước.
            lock (_state.Sync) LogWalProbesLocked(probes, probeEpoch, accepted: false);
            if (QuestErrorClassifierHelper.IsConnectivity(ex)) throw new NodeConnectivityException(ex);
            throw;
        }

        var probedAt = _clock.GetUtcNow().UtcDateTime;
        _state.LastProbeUtc = probedAt;
        var wasConnected = _state.Connected;
        var firstContact = !_state.EverConnected;
        _state.Connected = true;
        _state.EverConnected = true;
        _state.WalSuspended = walSuspended;

        if (!wasConnected)
        {
            _state.Backoff = _options.BackoffInitial;
            _state.RewindPending = true;
            _state.RewindWhy = QuestReplicatorCursorRules.ReconnectReason(firstContact);
            lock (_state.Sync)
            {
                // Node (có thể) vừa restart: WAL phải được xác nhận lại.
                _state.WalConfirmed = false;
                // kết nối lại cần bằng chứng apply mới.
                _state.WalProgress.Clear();
                _state.WalWrittenSince.Clear();
            }
        }

        // xác nhận WAL đã apply. Probe chạy sau mọi lần ghi trước đó trong cùng vòng lặp ⇒ "apply" ở đây phủ
        // mọi dữ liệu worker đã ghi tới thời điểm này.
        lock (_state.Sync)
        {
            // chỉ nhận mốc của probe đúng epoch, cùng rào với xác nhận restore.
            if (probeEpoch == _state.Epoch)
                foreach (var (table, probe) in probes)
                {
                    _state.WalProgress.TryGetValue(table, out var previous);
                    // txn lùi là mất bằng chứng cũ, kể cả khi node chưa báo mất kết nối.
                    if (previous is not null && (probe.WriterTxn < previous.Writer || probe.SequencerTxn < previous.Sequencer))
                        _state.WalConfirmed = false;
                    DateTime? writtenSince = _state.WalWrittenSince.TryGetValue(table, out var written) ? written : null;
                    _state.WalProgress[table] = QuestWalProgressRules.Observe(previous, probe, probedAt, writtenSince);
                    if (probe.TableExists && !probe.WalSuspended && probe.WriterTxn >= probe.SequencerTxn)
                        _state.WalWrittenSince.Remove(table);
                }
            if (probeEpoch != _state.Epoch)
            {
                // Probe bắt đầu trước Begin/Arm/start lại node: không chứng minh gì cho root/tiến trình hiện tại.
            }
            else if (bad is null && walApplied)
            {
                _state.WalConfirmed = true;
                _state.WalConfirmedEpoch = probeEpoch;
                _state.WalPendingSince = null;
            }
            else if (bad is null)
            {
                _state.WalPendingSince ??= probedAt;
            }
            LogWalProbesLocked(probes, probeEpoch, accepted: probeEpoch == _state.Epoch);
        }

        if (_state.RewindPending)
        {
            // Lỗi SQL ở đây ném JournalException ⇒ cờ vẫn bật ⇒ lượt sau thăm dò + rewind lại.
            await RewindAsync(_state.RewindWhy ?? "phục hồi", ct);
            _state.RewindPending = false;
            _state.RewindWhy = null;
        }

        var prevFence = _state.AutoFence;
        _state.AutoFence = bad?.Error;
        if (_state.AutoFence is not null && prevFence != _state.AutoFence)
        {
            _setError(_state.AutoFence);
            _timeline?.Publish("Replicator", Node, QuestHaEventKindEnum.Error, $"{Node} bị rào: {_state.AutoFence}");
        }

        return _state.AutoFence is null;
    }

    // chỉ dựng tham số log khi Debug bật; mẫu bị bỏ vẫn có txn và checkpoint đang giữ.
    internal void LogWalProbesLocked(IReadOnlyDictionary<string, NodeProbeOutput> probes, long probeEpoch, bool accepted)
    {
        if (!_logger.IsEnabled(LogLevel.Debug)) return;
        var now = _clock.GetUtcNow().UtcDateTime;
        foreach (var (table, probe) in probes)
        {
            _state.WalProgress.TryGetValue(table, out var progress);
            var count = progress?.Pending.Count ?? 0;
            var oldestAge = count == 0 ? 0 : (now - progress!.Pending[0].Since).TotalSeconds;
            _logger.LogDebug("[QuestHa.Replicator] WAL {Node}/{Table}: writerTxn={WriterTxn}, sequencerTxn={SequencerTxn}, checkpoints={CheckpointCount}, oldestAgeSeconds={OldestAgeSeconds}, exists={TableExists}, suspended={Suspended}, probeEpoch={ProbeEpoch}, epoch={Epoch}, accepted={Accepted}.",
                Node, table, probe.WriterTxn, probe.SequencerTxn, count, oldestAge,
                probe.TableExists, probe.WalSuspended, probeEpoch, _state.Epoch, accepted);
        }
    }

    internal async Task RewindAsync(string why, CancellationToken ct)
    {
        try
        {
            var r = await _store.RewindToSafeAsync(Node, ct);
            lock (_state.Sync)
            {
                _state.History.Clear();
                _state.KnownCursor = null;
            }
            if (r is { } x)
            {
                Interlocked.Increment(ref _state.Rewinds);
                var msg = $"{Node}: {why} ⇒ lùi cursor {x.From} → {x.To} (phát lại {x.From - x.To} Seq)";
                _logger.LogInformation("[QuestHa.Replicator] {Msg}", msg);
                _timeline?.Publish("Replicator", Node, QuestHaEventKindEnum.CursorRewound, msg);
            }
        }
        catch (Exception ex)
        {
            throw new JournalException(ex);
        }
    }

    /// <summary>INSERT thành công chỉ là WAL đã commit; dữ liệu chưa chắc đọc thấy cho tới khi WAL apply.</summary>
    // giữ thêm tuổi ghi từng bảng để nhận diện WAL đứng ngay cả khi probe đầu tới muộn.
    internal void MarkWritten(string table)
    {
        lock (_state.Sync)
        {
            _state.WalPendingSince ??= _clock.GetUtcNow().UtcDateTime;
            _state.WalWrittenSince.TryAdd(table, _clock.GetUtcNow().UtcDateTime);
        }
    }

}
