using Microsoft.Extensions.Logging;
using Shared.QuestHa.Infrastructure.Services.Replication.Snapshot;

namespace Shared.QuestHa.Infrastructure.Services.Replication;

/// <summary>
/// Author: KienLT
/// Description: Đọc và tiến pha phục hồi theo thế hệ, giữ quyền sở hữu và kiểm epoch khi bàn giao.
/// Created date: 31/10/2026
/// </summary>
internal sealed class QuestReplicatorRecoveryService
{
    private readonly QuestReplicatorState _state;
    private readonly IJournalStore _store;
    private readonly IQuestNodeClient _client;
    private readonly ReplicatorOptions _options;
    private readonly TimeProvider _clock;
    private readonly QuestHaTimelineService? _timeline;
    private readonly ILogger _logger;
    private readonly Action<bool> _updateSnapshot;

    internal QuestReplicatorRecoveryService(QuestReplicatorState state, IJournalStore store, IQuestNodeClient client, ReplicatorOptions options, TimeProvider clock, QuestHaTimelineService? timeline, ILogger logger, Action<bool> updateSnapshot)
    {
        _state = state;
        _store = store;
        _client = client;
        _options = options;
        _clock = clock;
        _timeline = timeline;
        _logger = logger;
        _updateSnapshot = updateSnapshot;
    }

    private string Node => _client.Node;

    /// <summary>
    /// bắt đầu phục hồi LƯU BỀN trước khi thay root — <see cref="IJournalStore.BeginRecoveryAsync"/>
    /// (Generation++, Phase=Restoring). Lỗi ⇒ ném, caller huỷ restore (chưa đụng root). Thành công ⇒ worker này GIỮ RÀO
    /// thế hệ g nhưng CHƯA được giao/Mark: root còn là root cũ. Chỉ sau <see cref="ArmRecovery"/> (orchestrator gọi
    /// khi đã thay root + SetCursor + mở lại node) worker mới phát lại tới đầu journal + WAL apply ⇒
    /// <c>MarkRecoveryCovered(g)</c> (AwaitReconcile); đối soát đúng thế hệ đọc sau CoveredUtc đạt ⇒ <c>CompleteRecovery(g)</c>
    /// ⇒ hết rào. <see cref="QuestReplicatorService.Unfence"/> không xoá trạng thái này. Đồng thời reset lịch sử giao.
    /// </summary>
    internal async Task<long> BeginRecoveryAsync(string by, CancellationToken ct = default)
    {
        // tăng cả trước lời gọi SQL — lượt đọc đang bay trong lúc ghi cũng bị bỏ.
        lock (_state.Sync) _state.RecMutation++;
        var gen = await _store.BeginRecoveryAsync(Node, by, ct);
        var now = _clock.GetUtcNow().UtcDateTime;
        lock (_state.Sync)
        {
            _state.RecMutation++;
            _state.Epoch++;
            _state.OwnedGen = gen;
            _state.ArmedGen = null;
            _state.ArmEpoch = long.MaxValue;
            _state.Rec = (_state.Rec ?? new NodeRecoveryOutput(0, NodeRecoveryPhaseEnum.None, null, null, null, null, false, now)) with
            {
                Generation = gen, Phase = NodeRecoveryPhaseEnum.Restoring, CoveredUtc = null, ReconciledGeneration = null,
            };
            _state.RecoveryReadDue = true;
            _state.History.Clear();
            _state.KnownCursor = null;
            _state.WalConfirmed = false;
            // root/thế hệ mới không được tái dùng bằng chứng WAL cũ.
            _state.WalProgress.Clear();
            _state.WalWrittenSince.Clear();
            _state.LastProbeUtc = DateTime.MinValue;
        }

        _updateSnapshot(true);
        _timeline?.Publish("Replicator", Node, QuestHaEventKindEnum.Info,
            $"{Node}: bắt đầu phục hồi thế hệ {gen} ({by}) — lưu bền; không nhận đọc cho tới khi bắt kịp + WAL apply + đối soát OK đúng thế hệ");
        return gen;
    }

    /// <summary>
    /// orchestrator BÀN GIAO thế hệ <paramref name="generation"/> SAU KHI root mới + cursor = watermark
    /// + node mở lại đều thành công. Tăng epoch: mọi nhịp/probe bắt đầu trước lúc này không được Mark g, không xác nhận WAL.
    /// false = worker không còn giữ thế hệ đó (bị thế hệ khác chen / pha đổi) ⇒ không bàn giao, giữ rào.
    /// </summary>
    internal bool ArmRecovery(long generation)
    {
        bool ok;
        lock (_state.Sync)
        {
            ok = _state.OwnedGen == generation && _state.Rec is { Phase: NodeRecoveryPhaseEnum.Restoring } s && s.Generation == generation;
            if (ok)
            {
                _state.RecMutation++;
                _state.Epoch++;
                _state.ArmedGen = generation;
                _state.ArmEpoch = _state.Epoch;
                _state.WalConfirmed = false;
                // probe trước handoff không chứng minh tiến độ trên root mới.
                _state.WalProgress.Clear();
                _state.WalWrittenSince.Clear();
                _state.LastProbeUtc = DateTime.MinValue;
                _state.RecoveryReadDue = true;
            }
        }

        _updateSnapshot(true);
        _state.Wake.Release();
        _timeline?.Publish("Replicator", Node, ok ? QuestHaEventKindEnum.Info : QuestHaEventKindEnum.Error,
            ok
                ? $"{Node}: bàn giao phục hồi thế hệ {generation} (root mới + cursor đã đặt) — bắt đầu phát lại, Mark sau khi bắt kịp + WAL apply"
                : $"{Node}: KHÔNG bàn giao được phục hồi thế hệ {generation} (worker không còn giữ thế hệ này) — giữ rào");
        return ok;
    }

    /// <summary>
    /// rào thủ công LƯU BỀN (<see cref="IJournalStore.SetManualFenceAsync"/>) — sống qua restart process. Rào ⇒
    /// không giao, không đọc. Lỗi SQL ⇒ ném (trạng thái không đổi).
    /// </summary>
    internal async Task SetManualFenceAsync(bool fenced, string by, CancellationToken ct = default)
    {
        lock (_state.Sync) _state.RecMutation++; // lượt đọc đang bay trong lúc ghi bị bỏ
        await _store.SetManualFenceAsync(Node, fenced, by, ct);
        lock (_state.Sync)
        {
            _state.RecMutation++;
            if (_state.Rec is not null) _state.Rec = _state.Rec with { ManualFence = fenced };
            _state.RecoveryReadDue = true;
        }

        _updateSnapshot(true);
        _state.Wake.Release();
        _timeline?.Publish("Replicator", Node, QuestHaEventKindEnum.Info, $"{Node}: {(fenced ? "RÀO" : "BỎ RÀO")} thủ công (lưu bền, {by})");
    }

    /// <summary>
    /// đọc trạng thái phục hồi lưu bền (tối đa 1 lần / ProbeInterval, hoặc ngay khi <c>_state.RecoveryReadDue</c>). Lỗi ⇒ giữ
    /// giá trị đã biết gần nhất (chưa từng đọc được ⇒ vẫn rào, fail-closed). Kết quả của lượt đọc bắt đầu trước một
    /// mutation cục bộ bị bỏ (áp kết quả theo thứ tự). Huỷ ⇒ ném.
    /// </summary>
    internal async Task RefreshRecoveryAsync(CancellationToken ct)
    {
        var now = _clock.GetUtcNow().UtcDateTime;
        long startMutation;
        lock (_state.Sync)
        {
            if (!_state.RecoveryReadDue && _state.Rec is not null && now - _state.LastRecoveryReadUtc < _options.ProbeInterval) return;
            _state.RecoveryReadDue = false;
            _state.LastRecoveryReadUtc = now;
            startMutation = _state.RecMutation;
        }

        NodeRecoveryOutput s;
        try
        {
            s = await _store.GetRecoveryAsync(Node, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            var msg = QuestErrorClassifierHelper.SafeDescribe(ex);
            bool first;
            lock (_state.Sync)
            {
                first = _state.RecoveryReadError is null;
                _state.RecoveryReadError = msg;
            }

            if (first) _logger.LogSafeWarning(ex, "[QuestHa.Replicator] {Node}: không đọc được trạng thái phục hồi — giữ trạng thái rào đã biết.", Node);
            return;
        }

        string? change = null;
        lock (_state.Sync)
        {
            if (_state.RecMutation != startMutation)
            {
                // lượt đọc bắt đầu trước một mutation cục bộ (Begin/SetManualFence/Arm/Mark/Complete/rào) ⇒ có thể
                // cũ hơn trạng thái đã biết: bỏ, không ghi đè cache/rào, không xoá ownership; đọc lại ở nhịp kế.
                _state.RecoveryReadDue = true;
                return;
            }

            var before = _state.Rec;
            _state.Rec = s;
            _state.RecoveryReadError = null;
            if (_state.OwnedGen is { } owned && (s.Generation != owned || s.Phase == NodeRecoveryPhaseEnum.None))
            {
                _state.OwnedGen = null;
                _state.ArmedGen = null;
                _state.ArmEpoch = long.MaxValue;
            }
            if (before is null || before.Generation != s.Generation || before.Phase != s.Phase || before.ManualFence != s.ManualFence)
                change = $"{Node}: trạng thái phục hồi lưu bền = thế hệ {s.Generation}, {s.Phase}{(s.ManualFence ? ", rào thủ công" : "")}";
        }

        if (change is not null && (s.Phase != NodeRecoveryPhaseEnum.None || s.ManualFence || s.Generation > 0))
        {
            _logger.LogInformation("[QuestHa.Replicator] {Msg}", change);
            _timeline?.Publish("Replicator", Node, QuestHaEventKindEnum.Info, change);
        }
    }

    /// <summary>
    /// không giao khi chưa đọc được trạng thái, rào thủ công, hoặc Restoring của thế hệ CHƯA được bàn giao cho worker
    /// này (Begin chỉ giữ rào — root vẫn là root cũ cho tới <see cref="ArmRecovery"/>).
    /// </summary>
    internal bool RecoveryBlocksDeliveryLocked() =>
        _state.Rec is null || _state.Rec.ManualFence || (_state.Rec.Phase == NodeRecoveryPhaseEnum.Restoring && _state.ArmedGen != _state.Rec.Generation);

    /// <summary>lý do rào từ trạng thái lưu bền (null = không rào). Ghi rõ pha + thế hệ.</summary>
    internal string? RecoveryFenceLocked()
    {
        if (_state.Rec is not { } s)
            return "chưa đọc được trạng thái phục hồi từ SQL — rào an toàn (fail-closed)"
                   + (_state.RecoveryReadError is null ? "" : $": {_state.RecoveryReadError}");
        if (s.ManualFence) return $"rào thủ công (lưu bền){(s.Phase == NodeRecoveryPhaseEnum.None ? "" : $"; phục hồi thế hệ {s.Generation} {s.Phase}")}";
        return s.Phase switch
        {
            NodeRecoveryPhaseEnum.Restoring when _state.ArmedGen == s.Generation =>
                $"đang phục hồi thế hệ {s.Generation} (Restoring) — phát lại tới đầu journal + WAL apply rồi chờ đối soát",
            NodeRecoveryPhaseEnum.Restoring when _state.OwnedGen == s.Generation =>
                $"đang thay root phục hồi thế hệ {s.Generation} (Restoring) — chưa bàn giao, không giao, không đọc",
            NodeRecoveryPhaseEnum.Restoring =>
                $"restore dở thế hệ {s.Generation} (Restoring) — không giao, không đọc; restore lại để tiếp tục",
            NodeRecoveryPhaseEnum.AwaitReconcile =>
                $"đang phục hồi thế hệ {s.Generation} (AwaitReconcile) — vẫn giao; chờ đối soát OK đúng thế hệ, đọc sau mốc bắt kịp",
            _ => null,
        };
    }

    /// <summary>
    /// tiến pha phục hồi lưu bền. Restoring của thế hệ ĐÃ BÀN GIAO + nhịp này bắt đầu sau handoff + đã
    /// bắt kịp journal + WAL xác nhận bởi probe bắt đầu sau handoff ⇒ <c>MarkRecoveryCovered(g)</c> (false = thế hệ/pha đã
    /// đổi ⇒ bỏ sở hữu, giữ rào). AwaitReconcile + đối soát OK đúng thế hệ g, ghi sau CoveredUtc ⇒
    /// <c>CompleteRecovery(g)</c> (CAS, idempotent) ⇒ hết rào. Đọc lại trạng thái ngay sau đó.
    /// </summary>
    internal async Task CheckRecoveryAsync(long tickEpoch, CancellationToken ct)
    {
        NodeRecoveryOutput? s;
        bool armed, caughtUp;
        lock (_state.Sync)
        {
            s = _state.Rec;
            armed = s is not null && _state.ArmedGen == s.Generation && _state.OwnedGen == s.Generation
                    && tickEpoch == _state.Epoch && tickEpoch >= _state.ArmEpoch && _state.WalConfirmedEpoch >= _state.ArmEpoch;
            caughtUp = _state.Snapshot.LagSeq == 0 && _state.Connected && !_state.RewindPending && _state.WalConfirmed && _state.WalPendingSince is null;
        }

        if (s is null) return;
        string? msg = null;
        try
        {
            if (s.Phase == NodeRecoveryPhaseEnum.Restoring && armed && caughtUp)
            {
                var ok = await _store.MarkRecoveryCoveredAsync(Node, s.Generation, ct);
                lock (_state.Sync)
                {
                    _state.RecMutation++;
                    if (!ok && _state.OwnedGen == s.Generation)
                    {
                        _state.OwnedGen = null;
                        _state.ArmedGen = null;
                        _state.ArmEpoch = long.MaxValue;
                    }

                    _state.RecoveryReadDue = true;
                }

                msg = ok
                    ? $"{Node}: phục hồi thế hệ {s.Generation} đã bắt kịp Seq {_state.Snapshot.LastSeq} + WAL apply ⇒ AwaitReconcile (chờ đối soát)"
                    : $"{Node}: MarkRecoveryCovered thế hệ {s.Generation} bị từ chối (thế hệ/pha đã đổi) — dừng phát lại, giữ rào";
            }
            else if (s is { Phase: NodeRecoveryPhaseEnum.AwaitReconcile, LastReconcileOk: true, LastReconcileUtc: { } at, CoveredUtc: { } covered }
                     && s.ReconciledGeneration == s.Generation && at > covered)
            {
                var ok = await _store.CompleteRecoveryAsync(Node, s.Generation, ct);
                lock (_state.Sync)
                {
                    _state.RecMutation++;
                    _state.RecoveryReadDue = true;
                }
                if (ok) msg = $"{Node}: hết phục hồi thế hệ {s.Generation} — đối soát OK lúc {at:HH:mm:ss}Z (sau mốc bắt kịp {covered:HH:mm:ss}Z)";
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new JournalException(ex);
        }

        if (msg is null) return;
        _logger.LogInformation("[QuestHa.Replicator] {Msg}", msg);
        _timeline?.Publish("Replicator", Node, QuestHaEventKindEnum.Info, msg);
        await RefreshRecoveryAsync(ct);
    }

}
