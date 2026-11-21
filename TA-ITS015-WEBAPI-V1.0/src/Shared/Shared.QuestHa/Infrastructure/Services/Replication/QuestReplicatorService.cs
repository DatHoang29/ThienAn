using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.QuestHa.Infrastructure.Services.Replication.Snapshot;
using Shared.QuestHa.Infrastructure.Services.Replication.Delivery;
// Ready theo tuổi mốc WAL từng bảng khi ghi liên tục; coverage đọc vẫn kiểm WAL đầy đủ.
using Shared.QuestHa.Infrastructure.Services.Replication.Rules;

namespace Shared.QuestHa.Infrastructure.Services.Replication;

// phân biệt Ready vận hành theo tuổi WAL với rào WAL hoàn tất khi restore.
/// <summary>
/// Author: KienLT
/// Description: Worker giao journal → 1 node QuestDB. Một instance / node, chạy bằng
/// <see cref="RunAsync"/> (hosted service của Core gọi).
///
/// <para><b>Vòng lặp.</b> (0) thăm dò node khi cần → (1) đọc cursor từ SQL (luôn đọc lại: reconciler/restore có
/// thể đã đổi) → (2) đọc lô <c>Seq &gt; cursor AND RowVer &lt; MIN_ACTIVE_ROWVERSION()</c> → (3) chuyển payload,
/// lỗi kiểu ⇒ quarantine → (4) gộp cùng khoá giữ Seq lớn nhất → (5) INSERT nhiều dòng qua PG wire →
/// (6) CHỈ SAU ĐÓ mới tiến cursor có điều kiện <c>LastSeq = expected</c> → (7) heartbeat/lag.
/// Chết giữa (5) và (6) ⇒ lần sau giao lại cả lô; DEDUP hấp thụ (at-least-once).</para>
///
/// <para><b>Lỗi.</b> Kết nối ⇒ Down + backoff luỹ thừa, giữ cursor. Node/schema (thiếu bảng, WAL suspend) ⇒
/// Fenced, không quarantine. Dữ liệu ⇒ thử từng dòng để cô lập, dòng hỏng vào QuestQuarantine, phần còn lại
/// vẫn giao, cursor đi tiếp (journal gốc không bị xoá ⇒ không mất dữ liệu, sửa xong phát lại được).</para>
///
/// <para><b>Rewind.</b> Mỗi lần node từ Down/chưa từng liên lạc → kết nối được: <c>LastSeq = SafeSeq</c>.
/// SafeSeq = cursor tại thời điểm ≥ RewindOnReconnect trước (lịch sử giao giữ trong bộ nhớ, persist vào
/// QuestNodeCursor.SafeSeq). Với <c>cairo.commit.mode=nosync</c>, máy chết có thể mất commit đã ACK; phát lại
/// cửa sổ này + DEDUP bù lại.</para>
///
/// <para><b>Phục hồi:</b> Rewind lỗi giữ trạng thái chờ rewind tới khi thành công. Cursor đổi từ bên ngoài
/// (restore/hạ cursor) ⇒ xoá lịch sử giao. Ready kiểm tuổi các mốc WAL từng bảng; sau restore cần WAL hoàn tất
/// và thêm lượt đối soát OK đúng thế hệ ghi sau mốc bắt kịp. Pha phục hồi và rào thủ công LƯU BỀN trong
/// QuestNodeStatus (<see cref="BeginRecoveryAsync"/>, <see cref="SetManualFenceAsync"/>): worker đọc lại mỗi
/// ProbeInterval — sống qua restart process. Rào do ghi lỗi phía node
/// giữ tới khi ghi thử thành công, thử lại theo backoff, không lặp timeline.</para>
/// Created date: 09/10/2026
/// </summary>
internal sealed class QuestReplicatorService
{
    private readonly QuestReplicatorState _state = new();
    private readonly QuestReplicatorRecoveryService _recovery;
    private readonly QuestReplicatorProbeService _probe;
    private readonly QuestReplicatorDeliveryService _delivery;
    private readonly IJournalStore _store;
    private readonly IQuestNodeClient _client;
    private readonly IReadOnlyDictionary<string, QuestTableSpec> _tables;
    private readonly ReplicatorOptions _options;
    private readonly TimeProvider _clock;
    private readonly QuestHaTimelineService? _timeline;
    private readonly ILogger _logger;

    /// <summary>Khởi tạo replicator của một node: journal, client node, bảng nhân bản, options, đồng hồ, timeline và logger.</summary>
    public QuestReplicatorService(
        IJournalStore store,
        IQuestNodeClient client,
        IEnumerable<QuestTableSpec> tables,
        ReplicatorOptions options,
        TimeProvider? clock = null,
        QuestHaTimelineService? timeline = null,
        ILogger<QuestReplicatorService>? logger = null)
    {
        _store = store;
        _client = client;
        _tables = tables.ToDictionary(t => t.Name, StringComparer.OrdinalIgnoreCase);
        if (_tables.Count == 0) throw new ArgumentException("At least one table spec is required.", nameof(tables));
        _options = options;
        _clock = clock ?? TimeProvider.System;
        _timeline = timeline;
        _logger = logger ?? (ILogger)NullLogger.Instance;
        _state.Backoff = options.BackoffInitial;
        _state.Snapshot = new NodeStatusOutput { Node = client.Node, State = QuestNodeStateEnum.Unknown, HeartbeatUtc = _clock.GetUtcNow().UtcDateTime };
        _recovery = new QuestReplicatorRecoveryService(_state, _store, _client, _options, _clock, _timeline, _logger, UpdateSnapshot);
        _probe = new QuestReplicatorProbeService(_state, _store, _client, _tables, _options, _clock, _timeline, _logger, SetError);
        _delivery = new QuestReplicatorDeliveryService(_state, _store, _client, _tables, _options, _clock, _timeline, _logger, _probe.ProbeAndMaybeRewindAsync, _probe.MarkWritten, SetError);
    }

    public string Node => _client.Node;

    public NodeStatusOutput Snapshot
    {
        get { lock (_state.Sync) return _state.Snapshot; }
    }

    /// <summary>Trạng thái đổi (State hoặc Paused/FenceReason).</summary>
    public event EventHandler<NodeStatusOutput>? StateChanged;

    private static TaskCompletionSource NewTcs() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    // ============================================================================ điều khiển

    /// <summary>Dừng giao. Trả về khi lô đang chạy (nếu có) đã xong ⇒ sau đó cursor không đổi nữa.</summary>
    public async Task PauseAsync(CancellationToken ct = default)
    {
        Task? wait = null;
        lock (_state.Sync)
        {
            _state.PauseRequested = true;
            if (_state.Running && !_state.IdleAcked)
            {
                _state.PauseAck ??= NewTcs();
                wait = _state.PauseAck.Task;
            }
        }

        _state.Wake.Release();
        if (wait is not null) await wait.WaitAsync(ct);
        UpdateSnapshot(true);
        _timeline?.Publish("Replicator", Node, QuestHaEventKindEnum.Info, $"Worker {Node} PAUSE");
    }

    /// <summary>Bỏ tạm dừng giao cho node (tiếp tục vòng replicator).</summary>
    public void Resume()
    {
        lock (_state.Sync)
        {
            if (!_state.PauseRequested) return;
            _state.PauseRequested = false;
            _state.ResumeSignal.TrySetResult();
            _state.ResumeSignal = NewTcs();
        }

        _state.Wake.Release();
        UpdateSnapshot(true);
        _timeline?.Publish("Replicator", Node, QuestHaEventKindEnum.Info, $"Worker {Node} RESUME");
    }

    /// <summary>Rào node (backup/restore/thao tác tay): không giao, router không đọc.</summary>
    public void Fence(string reason)
    {
        lock (_state.Sync)
        {
            _state.RecMutation++;
            _state.ManualFence = string.IsNullOrWhiteSpace(reason) ? "manual" : reason;
        }

        UpdateSnapshot(true);
        _state.Wake.Release();
    }

    /// <summary>Gỡ rào ghi thủ công của node; lần giao kế tiếp sẽ probe lại trước khi ghi.</summary>
    public void Unfence()
    {
        lock (_state.Sync)
        {
            _state.RecMutation++;
            _state.ManualFence = null;
        }

        UpdateSnapshot(true);
        _state.Wake.Release();
    }

    /// <summary>Đánh thức ngay (vd vừa append journal) — giảm trễ dưới PollInterval.</summary>
    public void Wake() => _state.Wake.Release();

    /// <summary>
    /// true = node đang phục hồi (pha lưu bền ≠ None) hoặc chưa đọc được trạng thái phục hồi: router không đọc.
    /// </summary>
    public bool IsRecovering
    {
        get { lock (_state.Sync) return _state.Rec is null || _state.Rec.Phase != NodeRecoveryPhaseEnum.None; }
    }

    /// <summary>Trạng thái phục hồi lưu bền đọc gần nhất (null = chưa đọc được). Cho dashboard.</summary>
    public NodeRecoveryOutput? RecoveryState
    {
        get { lock (_state.Sync) return _state.Rec; }
    }

    /// <summary>
    /// Tiến trình node vừa được start lại (backup/restore). Worker bị pause trong lúc đó nên không thể biết node đã
    /// dừng sạch hay chết ⇒ coi như kết nối lại: rewind về SafeSeq (restore: SafeSeq = watermark ⇒ không lùi
    /// thêm) và WAL phải được xác nhận apply lại trước khi Ready; thăm dò ngay ở lượt kế.
    /// </summary>
    public void NotifyNodeRestarted()
    {
        // socket trong pool thuộc tiến trình QuestDB trước restart không còn dùng được.
        _client.ResetConnectionPool("NotifyNodeRestarted");
        lock (_state.Sync)
        {
            _state.Epoch++; // probe đang chạy (của tiến trình cũ) không được xác nhận WAL cho tiến trình mới
            _state.WalConfirmed = false;
            // restart không được dùng lại mốc WAL của tiến trình trước.
            _state.WalProgress.Clear();
            _state.WalWrittenSince.Clear();
            _state.RewindPending = true;
            _state.RewindWhy = "node vừa start lại (backup/restore)";
            _state.LastProbeUtc = DateTime.MinValue;
            _state.RecoveryReadDue = true;
        }

        UpdateSnapshot(true);
        _state.Wake.Release();
    }

    /// <summary>
    /// bỏ lịch sử giao (mốc SafeSeq) của thế hệ cursor cũ. Gọi khi cursor bị đặt lại từ bên ngoài
    /// (restore/reconciler) — worker cũng tự phát hiện khi đọc thấy cursor khác giá trị nó đã ghi.
    /// </summary>
    public void ResetDeliveryHistory(string why)
    {
        lock (_state.Sync)
        {
            _state.History.Clear();
            _state.KnownCursor = null;
        }

        _logger.LogInformation("[QuestHa.Replicator] {Node}: xoá lịch sử giao ({Why}).", Node, why);
    }

    // ============================================================================ vòng lặp

    /// <summary>Vòng replicator: đọc journal theo cursor, ghi node, tiến cursor, ghi heartbeat/lag cho tới khi huỷ.</summary>
    public async Task RunAsync(CancellationToken ct)
    {
        lock (_state.Sync) _state.Running = true;
        try
        {
            while (!ct.IsCancellationRequested)
            {
                if (await HandlePauseAsync(ct)) continue;

                var delay = await RunOnceAsync(ct);
                await PersistStatusAsync(ct);
                if (delay > TimeSpan.Zero) await IdleAsync(delay, ct);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
        finally
        {
            lock (_state.Sync)
            {
                _state.Running = false;
                _state.PauseAck?.TrySetResult();
                _state.PauseAck = null;
            }
        }
    }

    /// <summary>true = đang pause (đã chờ 1 nhịp heartbeat).</summary>
    private async Task<bool> HandlePauseAsync(CancellationToken ct)
    {
        Task resume;
        lock (_state.Sync)
        {
            if (!_state.PauseRequested) return false;
            _state.PauseAck?.TrySetResult();
            _state.PauseAck = null;
            resume = _state.ResumeSignal.Task;
        }

        // Vẫn ghi heartbeat khi pause để router/dashboard thấy CatchingUp (lag tăng) chứ không phải Down.
        await RefreshLagAsync(ct);
        UpdateSnapshot();
        await PersistStatusAsync(ct);
        await Task.WhenAny(resume, Task.Delay(_options.StatusWriteInterval, ct));
        ct.ThrowIfCancellationRequested();
        return true;
    }

    private async Task IdleAsync(TimeSpan delay, CancellationToken ct)
    {
        lock (_state.Sync)
        {
            _state.IdleAcked = true;
            if (_state.PauseRequested)
            {
                _state.PauseAck?.TrySetResult();
                _state.PauseAck = null;
            }
        }

        try
        {
            await _state.Wake.WaitAsync(delay, ct);
        }
        finally
        {
            lock (_state.Sync) _state.IdleAcked = false;
        }
    }

    /// <summary>Một nhịp. Trả về thời gian nghỉ trước nhịp kế (0 = chạy tiếp ngay vì còn backlog).</summary>
    internal async Task<TimeSpan> RunOnceAsync(CancellationToken ct)
    {
        // epoch lúc BẮT ĐẦU nhịp — nhịp bắt đầu trước handoff (ArmRecovery) không được Mark.
        long tickEpoch;
        lock (_state.Sync) tickEpoch = _state.Epoch;

        // đọc pha phục hồi/rào thủ công lưu bền (mỗi ProbeInterval hoặc ngay khi cần) trước mọi việc khác.
        await _recovery.RefreshRecoveryAsync(ct);

        bool blocked;
        lock (_state.Sync) blocked = _state.ManualFence is not null || _recovery.RecoveryBlocksDeliveryLocked();
        if (blocked)
        {
            UpdateSnapshot();
            return _options.PollInterval;
        }

        // đang rào do ghi lỗi và chưa tới hạn thử lại ⇒ không chạm node, không ghi timeline.
        var nowGate = _clock.GetUtcNow().UtcDateTime;
        if (_state.WriteFence is not null && nowGate < _state.WriteFenceRetryAt)
        {
            UpdateSnapshot();
            return _state.WriteFenceRetryAt - nowGate;
        }

        try
        {
            // (0) Thăm dò: lần đầu, sau lỗi, khi đang auto-fence, khi WAL chưa xác nhận apply, hoặc định kỳ.
            var now = _clock.GetUtcNow().UtcDateTime;
            if (!_state.Connected || _state.RewindPending || _state.AutoFence is not null || !_state.WalConfirmed || _state.WalPendingSince is not null
                || now - _state.LastProbeUtc >= _options.ProbeInterval)
            {
                if (!await _probe.ProbeAndMaybeRewindAsync(ct))
                {
                    UpdateSnapshot();
                    return _options.ProbeInterval;
                }
            }

            // (1)(2)
            var cursor = await _store.GetCursorAsync(Node, ct);
            ObserveCursor(cursor.LastSeq);
            var batch = await _store.ReadBatchAsync(cursor.LastSeq, _options.BatchSize, ct);
            if (batch.Count == 0)
            {
                _delivery.ClearWriteFence(); // không còn dòng nào chờ ghi (vd cursor bị đặt lại qua lô lỗi)
                await RaiseSafeSeqAsync(cursor, ct);
                await RefreshLagAsync(ct, cursor.LastSeq);
                await _recovery.CheckRecoveryAsync(tickEpoch, ct);
                UpdateSnapshot();
                return _options.PollInterval;
            }

            // (3)(4)(5)
            var outcome = await _delivery.DeliverAsync(batch, ct);
            if (outcome == DeliverOutcome.Fenced)
            {
                UpdateSnapshot();
                var left = _state.WriteFence is not null ? _state.WriteFenceRetryAt - _clock.GetUtcNow().UtcDateTime : TimeSpan.Zero;
                return left > TimeSpan.Zero ? left : _options.ProbeInterval;
            }

            _delivery.ClearWriteFence();

            // (6)
            var newSeq = batch[^1].Seq;
            var safe = ComputeSafeSeq(_clock.GetUtcNow().UtcDateTime);
            if (await _store.TryAdvanceCursorAsync(Node, cursor.LastSeq, newSeq, safe, ct))
            {
                lock (_state.Sync)
                {
                    _state.History.AddLast((_clock.GetUtcNow().UtcDateTime, newSeq));
                    _state.KnownCursor = newSeq;
                }

                Interlocked.Add(ref _state.Delivered, batch.Count);
                lock (_state.Sync) _state.Snapshot = _state.Snapshot with { LastDeliveredUtc = _clock.GetUtcNow().UtcDateTime };
            }
            else
            {
                // Cursor bị đổi từ ngoài (reconciler/restore) trong lúc giao: bỏ kết quả tiến cursor của lô
                // này (dữ liệu đã ghi vô hại nhờ DEDUP), xoá lịch sử để SafeSeq không vượt cursor mới.
                lock (_state.Sync)
                {
                    _state.History.Clear();
                    _state.KnownCursor = null;
                }

                _logger.LogInformation("[QuestHa.Replicator] {Node}: cursor đổi từ bên ngoài, đọc lại.", Node);
            }

            _state.Backoff = _options.BackoffInitial;
            await RefreshLagAsync(ct, newSeq);
            UpdateSnapshot();
            return batch.Count >= _options.BatchSize ? TimeSpan.Zero : TimeSpan.FromMilliseconds(1);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (NodeConnectivityException ex)
        {
            return OnConnectivityFailure(ex.InnerException ?? ex);
        }
        catch (Exception ex)
        {
            // Mọi lỗi KHÔNG đến từ node client (đã bọc NodeConnectivityException) là lỗi journal (SQL) hoặc bất
            // ngờ: giữ trạng thái node (SQL chết không có nghĩa node chết), ghi lỗi, backoff.
            var inner = ex is JournalException ? ex.InnerException! : ex;
            // status/timeline/log không chứa thông điệp SQL kèm tài khoản/máy.
            var msg = $"Journal/khác lỗi: {QuestErrorClassifierHelper.SafeDescribe(inner)}";
            SetError(msg);
            _logger.LogSafeWarning(ex, "[QuestHa.Replicator] {Node}: {Error}", Node, msg);
            UpdateSnapshot();
            return NextBackoff();
        }
    }

    private TimeSpan OnConnectivityFailure(Exception ex)
    {
        // lỗi ghi/probe vẫn Down, đồng thời bỏ các socket chết còn lại của riêng node.
        _client.ResetConnectionPool("lỗi kết nối probe/ghi");
        var wasConnected = _state.Connected;
        _state.Connected = false;
        SetError(QuestErrorClassifierHelper.SafeDescribe(ex));
        if (wasConnected)
        {
            _logger.LogSafeWarning(ex, "[QuestHa.Replicator] {Node} mất kết nối.", Node);
        }

        UpdateSnapshot();
        return NextBackoff();
    }

    private TimeSpan NextBackoff()
    {
        var d = _state.Backoff;
        var next = TimeSpan.FromTicks(Math.Min(_options.BackoffMax.Ticks, _state.Backoff.Ticks * 2));
        _state.Backoff = next < _options.BackoffInitial ? _options.BackoffInitial : next;
        return d;
    }

    /// <summary>
    /// cursor đọc được khác cursor worker đã biết ⇒ có restore/hạ cursor từ bên ngoài giữa hai lô ⇒ mốc lịch
    /// sử cũ không còn chứng minh gì cho thế hệ cursor mới (SafeSeq không được dựa vào tuổi lần giao trước).
    /// </summary>
    private void ObserveCursor(long lastSeq)
    {
        bool changed;
        lock (_state.Sync)
        {
            changed = _state.KnownCursor is long k && k != lastSeq;
            if (changed) _state.History.Clear();

            _state.KnownCursor = lastSeq;
        }

        if (changed)
            _logger.LogInformation("[QuestHa.Replicator] {Node}: cursor đổi từ bên ngoài ⇒ {Seq}, xoá lịch sử giao.", Node, lastSeq);
    }

    /// <summary>SafeSeq = cursor lớn nhất đã giao xong tại thời điểm ≤ now − RewindOnReconnect.</summary>
    internal long ComputeSafeSeq(DateTime now)
    {
        lock (_state.Sync)
        {
            var result = QuestReplicatorCursorRules.SafeSequence(_state.History, now, _options.RewindOnReconnect);
            for (var i = 0; i < result.RemoveCount; i++) _state.History.RemoveFirst();
            return result.SafeSeq;
        }
    }

    private async Task RaiseSafeSeqAsync(NodeCursorOutput cursor, CancellationToken ct)
    {
        var safe = ComputeSafeSeq(_clock.GetUtcNow().UtcDateTime);
        if (safe > cursor.SafeSeq && safe <= cursor.LastSeq)
        {
            try { await _store.TryRaiseSafeSeqAsync(Node, safe, ct); }
            catch (Exception ex) { throw new JournalException(ex); }
        }
    }

    private async Task RefreshLagAsync(CancellationToken ct, long? cursor = null)
    {
        try
        {
            var c = cursor ?? (await _store.GetCursorAsync(Node, ct)).LastSeq;
            var lag = await _store.GetLagAsync(c, ct);
            lock (_state.Sync)
            {
                _state.Snapshot = _state.Snapshot with
                {
                    LastSeq = c,
                    MaxSeq = lag.MaxSeq,
                    LagSeq = lag.Pending,
                    LagSeconds = lag.OldestPendingAgeSeconds,
                };
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            SetError($"Journal lỗi: {QuestErrorClassifierHelper.SafeDescribe(ex)}");
        }
    }

    private void SetError(string message)
    {
        lock (_state.Sync) _state.Snapshot = _state.Snapshot with { LastError = message, LastErrorUtc = _clock.GetUtcNow().UtcDateTime };
    }

    /// <summary>Tính State từ cờ hiện tại; phát StateChanged khi đổi.</summary>
    internal void UpdateSnapshot(bool force = false)
    {
        NodeStatusOutput before, after;
        // lý do dùng cùng ảnh/cùng khoá với quyết định State, chỉ dựng khi đổi trạng thái.
        string? transitionReason = null;
        lock (_state.Sync)
        {
            before = _state.Snapshot;
            var now = _clock.GetUtcNow().UtcDateTime;
            var fence = _state.ManualFence ?? _recovery.RecoveryFenceLocked() ?? _state.AutoFence ?? _state.WriteFence;
            var flags = ReadinessLocked();
            var walReady = QuestReplicatorReadinessRules.WalReady(flags, _state.WalProgress, _tables.Count, now, _options.ReadyMaxLag);
            var state = QuestReplicatorReadinessRules.State(before, fence, flags, walReady, _options.ReadyMaxLag);

            after = before with
            {
                State = state,
                Paused = _state.PauseRequested,
                FenceReason = fence,
                WalSuspended = _state.WalSuspended,
                HeartbeatUtc = now,
                ReadySinceUtc = state == QuestNodeStateEnum.Ready ? before.ReadySinceUtc ?? now : null,
                DeliveredTotal = Interlocked.Read(ref _state.Delivered),
                RewindCount = Interlocked.Read(ref _state.Rewinds),
                QuarantinedTotal = Interlocked.Read(ref _state.Quarantined),
            };
            _state.Snapshot = after;
            if (before.State != after.State)
                transitionReason = QuestReplicatorReadinessRules.StateReason(after, flags, walReady, _tables.Keys, _state.WalProgress, now, _options.ReadyMaxLag);
        }

        var changed = before.State != after.State || before.Paused != after.Paused || before.FenceReason != after.FenceReason;
        if (changed)
        {
            if (before.State != after.State)
            {
                // Information chỉ khi đổi State; Debug có lý do WAL/journal/epoch cụ thể.
                _logger.LogInformation("[QuestHa.Replicator] {Node}: {Before} → {After}: {Reason}", Node, before.State, after.State, transitionReason);
                if (_logger.IsEnabled(LogLevel.Debug))
                    _logger.LogDebug("[QuestHa.Replicator] Ready transition {Node}: {Before} → {After}: {Reason}", Node, before.State, after.State, transitionReason);
                _timeline?.Publish("Replicator", Node, QuestHaEventKindEnum.NodeStateChanged,
                    $"Node {Node}: {before.State} → {after.State} ({transitionReason})");
            }

            try { StateChanged?.Invoke(this, after); } catch { /* listener */ }
            _state.LastStatusWriteUtc = DateTime.MinValue; // ghi ngay
        }
        else if (force)
        {
            _state.LastStatusWriteUtc = DateTime.MinValue;
        }
    }

    private async Task PersistStatusAsync(CancellationToken ct)
    {
        var now = _clock.GetUtcNow().UtcDateTime;
        if (now - _state.LastStatusWriteUtc < _options.StatusWriteInterval) return;
        try
        {
            await _store.WriteStatusAsync(Snapshot, ct);
            _state.LastStatusWriteUtc = now;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Journal chết: router sẽ thấy heartbeat già ⇒ Down. Không làm chết worker.
            // Debug chỉ nhận exception đã làm sạch (không object exception).
            _logger.LogSafeDebug(ex, "[QuestHa.Replicator] {Node}: không ghi được status.", Node);
        }
    }

    /// <inheritdoc cref="QuestReplicatorRecoveryService.BeginRecoveryAsync" />
    public Task<long> BeginRecoveryAsync(string by, CancellationToken ct = default) => _recovery.BeginRecoveryAsync(by, ct);

    /// <inheritdoc cref="QuestReplicatorRecoveryService.ArmRecovery" />
    public bool ArmRecovery(long generation) => _recovery.ArmRecovery(generation);

    /// <inheritdoc cref="QuestReplicatorRecoveryService.SetManualFenceAsync" />
    public Task SetManualFenceAsync(bool fenced, string by, CancellationToken ct = default) => _recovery.SetManualFenceAsync(fenced, by, ct);

    private QuestReplicatorReadinessRules.Readiness ReadinessLocked() => new(
        _state.Connected, _state.EverConnected, _state.PauseRequested, _state.RewindPending, _state.RewindWhy,
        _state.WalSuspended, _state.WalConfirmed, _state.WalPendingSince, _state.Epoch, _state.WalConfirmedEpoch);
}
