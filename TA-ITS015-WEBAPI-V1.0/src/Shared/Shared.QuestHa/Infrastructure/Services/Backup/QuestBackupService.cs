using System.Runtime.ExceptionServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Shared.QuestHa.Infrastructure.Services.Backup;

/// <summary>
/// Author: KienLT
/// Description: Backup nguội / restore 1 node (Windows không có CHECKPOINT).
///
/// <para><b>Backup:</b> Fence (router bỏ node) → Pause worker (chờ lô đang chạy xong) → StopClean → watermark →
/// chép root → đếm lại file/byte (Verified) → ghi QuestBackupCatalog → Start + chờ node trả lời → Resume → Unfence.
/// Watermark LUÔN = min(SafeSeq, LastSeq): trên Windows không chứng minh được "đúng thế hệ tiến trình
/// + flush xong" khi dừng, nên không bao giờ cấp LastSeq; restore phát lại thêm phần SafeSeq..LastSeq (vài giây),
/// an toàn nhờ DEDUP UPSERT KEYS.
/// lỗi/huỷ backup dùng token độc lập có trần để start lại vì root không đổi;
/// chỉ mở rào sau khi xác minh chạy lại. Start thất bại giữ Fenced và ghi Error.</para>
///
/// <para><b>Restore:</b> BeginRecovery LƯU BỀN (thế hệ g; lỗi ⇒ huỷ, chưa đụng gì) → Fence → Pause →
/// StopClean (lỗi thì Kill) → đổi
/// tên root sang <c>{root}.prerestore-{stamp}</c> → chép backup → cursor = watermark (SafeSeq = watermark, xoá lịch
/// sử giao) → Start + chờ node trả lời (bảng có, WAL không suspend) → BÀN GIAO thế hệ cho worker (ArmRecovery,
/// trước đó worker chỉ giữ rào, không Mark) → Resume → Unfence. BẤT KỲ bước nào lỗi
/// hoặc bị huỷ ⇒ KHÔNG start/resume/unfence: node giữ Fenced, worker pause, ghi timeline bước lỗi + trạng thái
/// tiến trình; chạy lại restore để sửa (Begin mới tăng thế hệ). Sau Unfence node vẫn ngoài tuyến đọc cho tới khi worker
/// bắt kịp + WAL apply (MarkRecoveryCovered g) + đối soát OK đúng thế hệ g (CompleteRecovery g) — pha lưu bền trong SQL nên
/// sống qua restart process (<see cref="QuestReplicatorService.BeginRecoveryAsync"/>). Journal phải còn đủ từ watermark.</para>
/// Created date: 09/10/2026
/// </summary>
internal sealed class QuestBackupService
{
    private readonly IJournalStore _store;
    private readonly IReadOnlyDictionary<string, QuestReplicatorService> _replicators;
    private readonly IReadOnlyDictionary<string, INodeControl> _controls;
    private readonly IReadOnlyDictionary<string, IQuestNodeClient> _clients;
    private readonly IReadOnlyList<QuestTableSpec> _tables;
    private readonly BackupOptions _options;
    private readonly Func<BackupOptions> _runtime;
    private readonly QuestHaTimelineService? _timeline;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    // dấu node dừng và owner lock tồn tại ngoài RAM của host.
    private readonly QuestBackupRecoveryService _recovery;
    private readonly Dictionary<string, long> _replayReady = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Khởi tạo điều phối backup/restore trên cùng các replicator đang sở hữu node.</summary>
    public QuestBackupService(
        IJournalStore store,
        IEnumerable<QuestReplicatorService> replicators,
        IEnumerable<INodeControl> controls,
        IEnumerable<IQuestNodeClient> clients,
        IEnumerable<QuestTableSpec> tables,
        BackupOptions options,
        QuestHaTimelineService? timeline = null,
        ILogger<QuestBackupService>? logger = null,
        Func<BackupOptions>? runtime = null)
    {
        _store = store;
        _replicators = replicators.ToDictionary(r => r.Node, StringComparer.OrdinalIgnoreCase);
        _controls = controls.ToDictionary(c => c.Node, StringComparer.OrdinalIgnoreCase);
        _clients = clients.ToDictionary(c => c.Node, StringComparer.OrdinalIgnoreCase);
        _tables = tables.ToList();
        _options = options;
        // runtime = snapshot vận hành của host; mỗi thao tác chụp một lần lúc bắt đầu nên đổi lịch/quota không cắt ngang bản đang chép.
        _runtime = runtime ?? (() => _options);
        // dấu flush bền cho phép host mới bật lại node sau crash.
        _recovery = new QuestBackupRecoveryService(options);
        _timeline = timeline;
        _logger = logger ?? (ILogger)NullLogger.Instance;
    }

    /// <summary>true khi đang có backup/restore chạy (mỗi lúc chỉ 1 thao tác).</summary>
    public bool IsBusy => _gate.CurrentCount == 0;

    // recovery startup giữ cùng gate và owner lock, không chạy chồng backup.
    /// <summary>Start lại node có dấu backup dừng; giữ dấu và rào nếu phục hồi lỗi.</summary>
    public async Task RecoverInterruptedAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            using var owner = _recovery.AcquireOwner();
            foreach (var (node, control) in _controls)
            {
                try
                {
                    if (!_recovery.WasStopped(control)) continue;
                    var (rep, _) = Resolve(node);
                    rep.Fence("phục hồi backup bị ngắt");
                    await rep.PauseAsync(ct);
                    await ReopenAsync(node, rep, control, ct);
                    // lỗi dọn không rào lại node đã phục hồi; giữ dấu để startup sau thử lại.
                    await CleanupInterruptedAsync(node, control, ct);
                    Publish(node, QuestHaEventKindEnum.Error, "Đã start lại node do backup trước bị ngắt; đã kiểm catalog và bản dở dang.");
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
                catch (Exception ex)
                {
                    // lỗi một node không ngăn host ingest trên journal/node còn lại.
                    if (_replicators.TryGetValue(node, out var rep)) rep.Fence("phục hồi backup lỗi; giữ dấu dừng");
                    // Debug chỉ nhận exception đã làm sạch (không object exception).
                    _logger.LogSafeDebug(ex, "[QuestHa.Backup] Chi tiết phục hồi {Node}.", node);
                    Publish(node, QuestHaEventKindEnum.Error, $"Phục hồi backup {node} lỗi {QuestErrorClassifierHelper.SafeDescribe(ex)}; giữ dấu và rào, host tiếp tục.");
                }
            }
            // catalog có thể đã mất/reset; chỉ cảnh báo các bản owned khác, tuyệt đối không tự xoá.
            foreach (var (node, _) in _controls)
            {
                try
                {
                    var root = _options.RootFor(node);
                    if (string.IsNullOrWhiteSpace(root)) continue;
                    var catalog = await _store.ListBackupsAsync(ct);
                    foreach (var orphan in QuestBackupStorageService.FindOrphans(node, root, catalog))
                        _logger.LogWarning("[QuestHa.Backup] Bản owned ngoài catalog {Node}/{BackupId}, Path={Path}; giữ nguyên để vận hành đối chiếu.", node, orphan.BackupId, orphan.Path);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
                catch (Exception ex) { _logger.LogSafeWarning(ex, "[QuestHa.Backup] Không kiểm được bản ngoài catalog của {Node}.", node); }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            foreach (var rep in _replicators.Values) rep.Fence("không đọc được trạng thái backup bền");
            // Debug chỉ nhận exception đã làm sạch (không object exception).
            _logger.LogSafeDebug(ex, "[QuestHa.Backup] Chi tiết khoá phục hồi.");
            Publish("*", QuestHaEventKindEnum.Error, $"Không mở được trạng thái backup: {QuestErrorClassifierHelper.SafeDescribe(ex)}; giữ rào, host tiếp tục.");
        }
        finally { _gate.Release(); }
    }

    // cô lập lỗi xoá khỏi recovery node, dấu vẫn còn trên đĩa tới lượt sau.
    private async Task CleanupInterruptedAsync(string node, INodeControl control, CancellationToken ct)
    {
        try
        {
            var record = _recovery.ReadInterruptedBackup(control);
            if (record is not null)
            {
                if (!string.Equals(record.Node, node, StringComparison.OrdinalIgnoreCase)) throw new IOException("Node không trùng dấu backup.");
                var root = _options.RootFor(node);
                if (string.IsNullOrWhiteSpace(root)) throw new IOException("Chưa cấu hình BackupRoot để dọn bản bị ngắt.");
                await QuestBackupStorageService.CleanupInterruptedAsync(record, root, _store, ct);
            }
            _recovery.Clear(control);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            _logger.LogSafeWarning(ex, "[QuestHa.Backup] Chưa dọn được bản bị ngắt của {Node}; giữ dấu để thử lại, node đã chạy lại.", node);
            _timeline?.Publish("Backup", node, QuestHaEventKindEnum.Error,
                $"Chưa dọn được bản backup bị ngắt: {QuestErrorClassifierHelper.SafeDescribe(ex)}; giữ dấu để thử lại, node đã chạy lại.");
        }
    }

    // preview dùng cùng tiền kiểm catalog/dung lượng/quyền ghi như apply.
    /// <summary>Kiểm backup không dừng node; phát hiện thiếu migration, root, dung lượng và peer.</summary>
    public async Task PreflightAsync(string node, CancellationToken ct = default)
    {
        var options = _runtime();
        options.Validate();
        var (_, control) = Resolve(node);
        _ = await _store.ListBackupsAsync(ct);
        if (string.IsNullOrWhiteSpace(options.RootFor(node))) throw new InvalidOperationException("Chưa cấu hình BackupRoot.");
        CheckCopyBudget(QuestBackupStorageService.Preflight(control.RootPath, options.RootFor(node), options.MinimumFreeBytes, ct), options);
        await EnsurePeerReadyAsync(node, ct);
    }

    /// <summary>Kiểm node còn lại qua trạng thái replicator và probe mới, trước khi dừng node đích.</summary>
    public async Task EnsurePeerReadyAsync(string node, CancellationToken ct)
    {
        foreach (var peer in _replicators.Values.Where(r => !r.Node.Equals(node, StringComparison.OrdinalIgnoreCase)))
        {
            if (!QuestBackupScheduleRules.OtherReady(node, new[] { peer.Snapshot }) || peer.IsRecovering || !_clients.TryGetValue(peer.Node, out var client)) continue;
            try
            {
                var ready = true;
                foreach (var table in _tables)
                {
                    var probe = await client.ProbeAsync(table, ct);
                    ready &= probe.TableExists && !probe.WalSuspended;
                }
                if (ready && QuestBackupScheduleRules.OtherReady(node, new[] { peer.Snapshot }) && !peer.IsRecovering) return;
            }
            catch (Exception) when (!ct.IsCancellationRequested) { }
        }
        // phân biệt tiền điều kiện peer với lỗi backup để lịch không mất lượt thử.
        var error = new InvalidOperationException("Không có node còn lại đang Ready và đọc được; không dừng node để backup.");
        error.Data["QuestHa.Backup.WaitForPeer"] = true;
        throw error;
    }

    /// <summary>Retain dùng cùng gate backup/restore; không xóa file đang phục hồi.</summary>
    public async Task<int> RetainAsync(bool dryRun = true, CancellationToken ct = default)
    {
        var options = _runtime();
        options.Validate();
        if (!await _gate.WaitAsync(0, ct)) throw new InvalidOperationException("Đang backup/restore.");
        try { return await new QuestBackupStorageService().RetainAsync(_store, options, dryRun, ct); }
        finally { _gate.Release(); }
    }

    /// <summary>Backup nguội một node: dừng node, sao chép root vào <c>BackupRoot</c>, ghi catalog kèm watermark, khởi động lại node.</summary>
    // gọi tay mặc định Manual; lịch truyền manual=false để quota không lẫn nhau.
    public async Task<BackupRecordOutput> BackupColdAsync(string node, CancellationToken ct = default, bool requirePeerReady = true, bool weekly = false, bool enforceWindow = false, bool manual = true)
    {
        // Chụp tham số một lần cho cả lượt: đổi lúc chạy áp từ lượt sau.
        var options = _runtime();
        options.Validate();
        var (rep, control) = Resolve(node);
        // Đường dẫn backup phải cấu hình tường minh để tránh ghi nhầm thư mục của môi trường khác.
        if (string.IsNullOrWhiteSpace(options.RootFor(node)))
            throw new InvalidOperationException("Chưa cấu hình QuestHa:Backup:BackupRoot.");
        if (string.IsNullOrWhiteSpace(node) || node.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || node is "." or "..") throw new ArgumentException("Tên node không hợp lệ.");
        var id = Guid.NewGuid();
        var dest = Path.Combine(options.RootFor(node), node, $"{DateTime.UtcNow:yyyyMMdd-HHmmss}-{id:N}");
        dest = Path.GetFullPath(dest);
        if (!await _gate.WaitAsync(0, ct)) throw new InvalidOperationException("Đang có backup/restore khác chạy.");
        var step = "chuẩn bị";
        var reopened = false;
        var touched = false;
        var stopAttempted = false;
        // mọi I/O sau stop có deadline; cleanup dùng token độc lập để start ngay khi deadline/host huỷ.
        using var operation = CancellationTokenSource.CreateLinkedTokenSource(ct);
        ct = operation.Token;
        FileStream? owner = null;
        try
        {
            owner = _recovery.AcquireOwner();
            // không ghi đè dấu đang chờ dọn của lượt trước.
            if (_recovery.WasStopped(control)) throw new InvalidOperationException("Còn dấu backup bị ngắt; chạy phục hồi trước lượt backup mới.");
            _ = await _store.ListBackupsAsync(ct); // RetiredUtc phải hiện diện TRƯỚC downtime (kể cả IPC).
            // chỉ cấp watermark = cursor khi node đang chạy VÀ worker đã chứng minh phủ tới cursor (Ready: đã
            // rewind sau lần (re)connect, bắt kịp, WAL apply, không trong phục hồi). Node vừa crash: đĩa có thể thiếu
            // so với cursor (nosync) ⇒ từ chối, không rào/không dừng gì.
            step = "kiểm tra điều kiện";
            var st0 = await FreshStatusAsync(control, "khi kiểm điều kiện", ct);
            if (!st0.IsRunning)
                throw new InvalidOperationException(
                    $"Node {node} không chạy (có thể vừa crash) — dữ liệu trên đĩa có thể thiếu so với cursor; khởi động node, chờ phát lại tới Ready rồi mới backup nguội.");
            var snap = rep.Snapshot;
            if (snap.State != QuestNodeStateEnum.Ready || rep.IsRecovering)
                throw new InvalidOperationException(
                    $"Node {node} chưa Ready ({snap.State}{(rep.IsRecovering ? ", đang phục hồi" : "")}) — chưa chứng minh dữ liệu trên đĩa phủ tới cursor; chờ phục hồi xong rồi backup nguội.");

            CheckCopyBudget(QuestBackupStorageService.Preflight(control.RootPath, options.RootFor(node), options.MinimumFreeBytes, ct), options);
            if (requirePeerReady) await EnsurePeerReadyAsync(node, ct);
            touched = true;
            Publish(node, QuestHaEventKindEnum.BackupStarted, $"Backup nguội node {node} → {dest}");
            rep.Fence("backup nguội");

            BackupRecordOutput? record = null;
            ExceptionDispatchInfo? failure = null;
            try
            {
                step = "pause worker";
                await rep.PauseAsync(ct);
                step = "dừng node";
                if (enforceWindow && !QuestBackupScheduleRules.InWindow(DateTime.UtcNow.ToLocalTime(), options)) throw new InvalidOperationException("Đã hết cửa sổ backup.");
                if (requirePeerReady) await EnsurePeerReadyAsync(node, ct);
                _recovery.MarkStopped(control, new BackupRecordOutput(id, node, 0, DateTime.UtcNow, dest, 0, false, DateTime.UtcNow));
                operation.CancelAfter(options.MaxStoppedDuration);
                stopAttempted = true;
                await StopAsync(control, allowKill: false, ct);

                // watermark LUÔN = min(SafeSeq, LastSeq). Dừng "sạch" không chứng minh được phần
                // SafeSeq..LastSeq đã bền trên đĩa (PID tái dùng, crash sau CTRL_C trước flush) ⇒ không cấp LastSeq;
                // restore phát lại phần dư, DEDUP xử lý trùng. min phòng cả trường hợp (không thể) LastSeq < SafeSeq.
                step = "đọc watermark";
                var cursor = await _store.GetCursorAsync(node, ct);
                var watermark = Math.Min(cursor.SafeSeq, cursor.LastSeq);
                var wmTs = await _store.GetCreatedUtcAsync(watermark, ct) ?? DateTime.UtcNow;

                step = "chép root";
                var src = Path.GetFullPath(control.RootPath);
                Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                QuestBackupStorageService.MarkOwned(new BackupRecordOutput(id, node, 0, DateTime.UtcNow, dest, 0, false, DateTime.UtcNow), weekly, manual);
                // đồng hồ rẻ mỗi chunk; kiểm snapshot rồi probe mạng tối đa một lần/5s.
                var checkPeer = requirePeerReady ? CreatePeerCheck(node) : null;
                var (files, bytes) = await CopyDirectoryAsync(src, dest, ct, checkPeer);
                var (vf, vb) = Measure(dest, ct);
                var manifest = await QuestBackupStorageService.VerifyAsync(src, dest, ct, checkPeer);
                if (requirePeerReady) await EnsurePeerReadyAsync(node, ct); // chốt mới trước catalog/start lại
                var verified = vf == files && vb == bytes;
                if (!verified) throw new IOException("Backup không khớp số file/kích thước.");

                step = "ghi catalog";
                record = new BackupRecordOutput(id, node, watermark, wmTs, dest, bytes, verified, DateTime.UtcNow);
                // manifest bền phải có TRƯỚC catalog Verified; restore kiểm lại theo nó.
                QuestBackupStorageService.WriteManifest(record, manifest);
                QuestBackupStorageService.MarkOwned(record, weekly, manual);
                await _store.AddBackupAsync(record, ct);
                Publish(node, QuestHaEventKindEnum.BackupFinished,
                    $"Backup {node} xong: watermark Seq {watermark} (min(SafeSeq {cursor.SafeSeq}, LastSeq {cursor.LastSeq})), {files} file, {bytes / 1024.0 / 1024.0:0.0} MiB, verified={verified}");
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw; // catch ngoài khởi động lại bằng token độc lập.
            }
            catch (Exception ex)
            {
                // Backup không đụng root ⇒ vẫn start lại node để không rào vĩnh viễn; lỗi gốc ném lại sau đó.
                failure = ExceptionDispatchInfo.Capture(ex);
            }

            ct.ThrowIfCancellationRequested(); // huỷ sau catalog cũng đi qua cleanup độc lập.
            step = failure is null ? "start lại node" : $"start lại node (sau lỗi ở bước '{step}')";
            await ReopenAsync(node, rep, control, ct);
            reopened = true;
            // lỗi copy/catalog phải dọn xong mới bỏ dấu retry; Verified luôn giữ nguyên.
            if (failure is null) _recovery.Clear(control);
            failure?.Throw();
            return record!;
        }
        catch (OperationCanceledException ex) when (ct.IsCancellationRequested)
        {
            // deadline tiêu hao lượt đã stop; scheduler vẫn truyền huỷ host ra ngoài.
            ex.Data["QuestHa.Backup.StopAttempted"] = stopAttempted;
            await CleanupBackupAsync();
            await PublishAbortAsync("Backup", node, control, step, null, cancelled: true, reopened, touched);
            throw;
        }
        catch (Exception ex)
        {
            // lịch chỉ trừ lượt khi đã thực sự gọi stop, không đốt quota vì tiền kiểm.
            ex.Data["QuestHa.Backup.StopAttempted"] = stopAttempted;
            await CleanupBackupAsync();
            await PublishAbortAsync("Backup", node, control, step, ex, cancelled: false, reopened, touched);
            throw;
        }
        finally
        {
            owner?.Dispose();
            _gate.Release();
        }

        async Task CleanupBackupAsync()
        {
            if (!touched) return;
            // backup không đổi root; huỷ host/copy phải phục hồi bằng token độc lập có trần.
            using var cleanup = new CancellationTokenSource(options.NodeStartTimeout + options.NodeStopTimeout);
            try
            {
                if (!reopened)
                {
                    await rep.PauseAsync(cleanup.Token);
                    await ReopenAsync(node, rep, control, cleanup.Token);
                    reopened = true;
                }
            }
            catch (Exception error)
            {
                Publish(node, QuestHaEventKindEnum.Error, $"Không khởi động lại được node {node} sau backup: {error.GetType().Name}; giữ rào, cần xử lý vận hành.");
            }
            try
            {
                // dọn sau khi start thành công, kiểm mọi Verified cùng path trước xoá.
                if (reopened && _recovery.WasStopped(control)) await CleanupInterruptedAsync(node, control, cleanup.Token);
            }
            catch (Exception error)
            { Publish(node, QuestHaEventKindEnum.Error, $"Chưa dọn được backup dở dang {id}: {error.GetType().Name}; giữ file để xử lý vận hành."); }
        }
    }

    /// <summary>Restore node từ bản backup: rào node, chép lại root, đặt cursor = watermark rồi để replicator phát lại phần còn thiếu.</summary>
    // cưỡng bức phải tường minh; node giữ phục hồi/rào bền để không chứng nhận dữ liệu thiếu.
    public async Task RestoreAsync(string node, Guid backupId, CancellationToken ct = default, bool allowJournalGap = false)
    {
        var (rep, control) = Resolve(node);
        var backup = await ValidateRestoreAsync(node, backupId, ct, allowJournalGap);

        if (!await _gate.WaitAsync(0, ct)) throw new InvalidOperationException("Đang có backup/restore khác chạy.");
        var step = "chuẩn bị";
        var touched = false;
        try
        {
            // kiểm lại journal dưới gate trước mọi thay đổi root/cursor.
            // băm lại nội dung theo manifest dưới gate, TRƯỚC BeginRecovery/rào/dừng node:
            // backup hỏng sau lúc Verified bị từ chối khi node vẫn chạy bình thường.
            step = "kiểm nội dung backup";
            backup = await ValidateRestoreAsync(node, backupId, ct, allowJournalGap, verifyContent: true);
            // phục hồi LƯU BỀN phải bắt đầu được TRƯỚC khi đụng vào bất cứ thứ gì (lỗi ⇒ huỷ, root nguyên vẹn).
            step = "bắt đầu phục hồi (SQL)";
            var gen = await rep.BeginRecoveryAsync("restore", ct);
            _replayReady.Remove(node);
            if (allowJournalGap) await rep.SetManualFenceAsync(true, "restore --allow-journal-gap; bắt buộc backfill", ct);
            touched = true;
            Publish(node, QuestHaEventKindEnum.RestoreStarted, $"Restore node {node} từ {backup.Path} (watermark Seq {backup.WatermarkSeq}, thế hệ phục hồi {gen})");
            rep.Fence("restore");

            step = "pause worker";
            await rep.PauseAsync(ct);
            step = "dừng node";
            await StopAsync(control, allowKill: true, ct);

            step = "đổi root";
            var root = Path.GetFullPath(control.RootPath).TrimEnd(Path.DirectorySeparatorChar);
            GuardRoot(root, backup.Path);
            // giữ root gốc của mọi lần restore lỗi, không xoá lối quay về ở lần thử kế.
            var aside = $"{root}.prerestore-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}";
            if (Directory.Exists(root)) Directory.Move(root, aside);

            step = "chép backup";
            // restore cũng ngắt được khi đang chép một file lớn.
            var (copiedFiles, copiedBytes) = await CopyDirectoryAsync(backup.Path, root, ct);
            // bản chép vào root phải đủ số file/byte theo manifest trước khi đặt cursor/bàn giao.
            if (QuestBackupStorageService.ManifestTotals(backup) is not { } totals || totals.Files != copiedFiles || totals.Bytes != copiedBytes)
                throw new IOException("Bản chép vào root không khớp manifest backup (số file/kích thước).");

            step = "đặt cursor";
            // purge chen trong lúc copy phải làm restore thất bại có rào, không được nhảy qua lỗ.
            if (allowJournalGap) await _store.SetCursorAsync(node, backup.WatermarkSeq, "restore --allow-journal-gap", ct);
            else await _store.SetRestoreCursorAsync(node, backup.WatermarkSeq, ct);
            rep.ResetDeliveryHistory("restore"); // SafeSeq không dựa vào lịch sử giao trước restore
            Publish(node, QuestHaEventKindEnum.CursorReset, $"Restore {node}: cursor = watermark {backup.WatermarkSeq}; root cũ để ở {aside}");

            step = "start node";
            if (allowJournalGap)
            {
                await StartAndWaitAsync(node, control, ct);
                rep.NotifyNodeRestarted();
                Publish(node, QuestHaEventKindEnum.Error, "Restore cưỡng bức đã chép root; giữ pause/rào bền và Phase=Restoring. Bắt buộc backfill; chạy lại restore hợp lệ để xác nhận trước khi Ready.");
            }
            else
            {
                await ReopenAsync(node, rep, control, ct, armGeneration: gen);
                _replayReady[node] = gen;
            }

            Publish(node, QuestHaEventKindEnum.RestoreFinished,
                allowJournalGap ? $"Restore {node} cần backfill, chưa xác nhận (thế hệ {gen}); KHÔNG tự Ready." :
                $"Restore {node} xong (thế hệ {gen}) — worker phát lại phần sau watermark; node ngoài tuyến đọc tới khi bắt kịp + WAL apply (MarkRecoveryCovered) + đối soát OK đúng thế hệ (CompleteRecovery)");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            await PublishAbortAsync("Restore", node, control, step, null, cancelled: true, reopened: false, touched);
            throw;
        }
        catch (Exception ex)
        {
            await PublishAbortAsync("Restore", node, control, step, ex, cancelled: false, reopened: false, touched);
            throw;
        }
        finally
        {
            _gate.Release();
        }
    }

    // cùng kiểm tra cho preview IPC và restore thực thi; không đổi root/cursor.
    // Băm lại nội dung theo manifest dưới gate để preview và restore kiểm cùng một bản backup.
    /// <summary>
    /// Từ chối backup chưa verify, retired hoặc journal thiếu; force luôn yêu cầu backfill. <paramref name="verifyContent"/>
    /// = true: kiểm thêm tập file/kích thước/SHA-256 theo manifest lưu lúc tạo backup — thiếu manifest hoặc lệch ⇒
    /// <see cref="InvalidDataException"/>. Chỉ đọc, không chạm node/cursor.
    /// </summary>
    public async Task<BackupRecordOutput> ValidateRestoreAsync(string node, Guid backupId, CancellationToken ct = default, bool allowJournalGap = false,
        bool verifyContent = false)
    {
        _ = Resolve(node);
        var backup = await _store.GetBackupAsync(backupId, ct)
                     ?? throw new InvalidOperationException($"Không tìm thấy backup {backupId}.");
        if (!string.Equals(backup.Node, node, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Backup {backupId} thuộc node {backup.Node}, không phải {node}.");
        var requiresBackfill = backup.RetiredUtc is not null || await _store.HasJournalGapAsync(backup.WatermarkSeq, ct);
        if (requiresBackfill && !allowJournalGap)
            throw new InvalidOperationException("Backup retired hoặc journal có khoảng trống; từ chối restore. Chỉ cưỡng bức bằng --allow-journal-gap và bắt buộc backfill.");
        if (!backup.Verified) throw new InvalidOperationException("Backup chưa Verified; từ chối restore.");
        if (allowJournalGap)
            Publish(node, QuestHaEventKindEnum.Error, "WARNING --allow-journal-gap: bắt buộc backfill và xác nhận phục hồi; node KHÔNG tự Ready.");
        if (!Directory.Exists(backup.Path)) throw new DirectoryNotFoundException(backup.Path);
        if (verifyContent)
        {
            var (files, bytes) = await QuestBackupStorageService.VerifyManifestAsync(backup, ct);
            Publish(node, QuestHaEventKindEnum.Info, $"Backup {backupId}: nội dung khớp manifest ({files} file, {bytes / 1024.0 / 1024.0:0.0} MiB).");
        }

        return backup;
    }

    /// <summary>
    /// Start + chờ node sẵn sàng; CHỈ khi thành công mới resume/unfence. Lỗi/huỷ ⇒ ném, node giữ Fenced. Restore
    /// (<paramref name="armGeneration"/>): root mới + cursor đã đặt + node đã mở lại ⇒ BÀN GIAO thế hệ cho worker
    /// (<see cref="QuestReplicatorService.ArmRecovery"/>) — trước đó worker chỉ giữ rào, không được Mark.
    /// </summary>
    private async Task ReopenAsync(string node, QuestReplicatorService rep, INodeControl control, CancellationToken ct, long? armGeneration = null)
    {
        await StartAndWaitAsync(node, control, ct);
        // huỷ có thể tới trong lúc await cuối mà tác vụ vẫn hoàn tất thành công ⇒ kiểm lại ngay trước khi mở
        // lại trạng thái; đã huỷ thì giữ pause + rào.
        ct.ThrowIfCancellationRequested();
        rep.NotifyNodeRestarted();
        if (armGeneration is { } g && !rep.ArmRecovery(g))
            throw new InvalidOperationException("Thế hệ phục hồi thay đổi, không bàn giao hoặc mở rào.");
        rep.Resume();
        rep.Unfence();
    }

    private (QuestReplicatorService, INodeControl) Resolve(string node)
    {
        if (!_replicators.TryGetValue(node, out var rep)) throw new ArgumentException($"Không có replicator cho node {node}.");
        if (!_controls.TryGetValue(node, out var control))
            throw new InvalidOperationException($"Chưa đăng ký INodeControl cho node {node}.");
        return (rep, control);
    }

    /// <summary>Preview mặc định; chỉ gỡ ManualFence theo thế hệ khi None hoặc strict restore trong host này đã bàn giao đầy đủ.</summary>
    public async Task UnfenceAsync(string node, bool apply = false, CancellationToken ct = default)
    {
        if (!await _gate.WaitAsync(0, ct)) throw new InvalidOperationException("Đang backup/restore.");
        try
        {
            var (rep, _) = Resolve(node);
            var recovery = await _store.GetRecoveryAsync(node, ct);
            var ready = _replayReady.TryGetValue(node, out var generation) && generation == recovery.Generation;
            if (recovery.Phase != NodeRecoveryPhaseEnum.None && !ready)
                throw new InvalidOperationException("Không gỡ rào phục hồi chưa xác minh: giữ .prerestore-*, backfill rồi tạo backup Verified có journal đủ và chạy strict restore thành công trước node-unfence.");
            if (ready && await _store.HasJournalGapAsync((await _store.GetCursorAsync(node, ct)).LastSeq, ct))
                throw new InvalidOperationException("Journal đã có khoảng trống sau strict restore; giữ ManualFence.");
            if (!apply) return;
            if (!await _store.TryClearManualFenceAsync(node, recovery.Generation, ready, ct))
                throw new InvalidOperationException("Thế hệ/pha đổi; giữ rào, chạy lại preview.");
            // Đọc lại SQL ở nhịp sau; chỉ mở rào trong RAM, không sửa pha/cursor hoặc chứng nhận Ready.
            rep.Unfence();
            rep.Resume();
            Publish(node, QuestHaEventKindEnum.Info, "node-unfence: đã gỡ ManualFence tường minh; Worker vẫn phải phủ journal, WAL và đối soát trước Ready.");
        }
        finally { _gate.Release(); }
    }

    private static void GuardRoot(string root, string backupPath)
    {
        var full = Path.GetFullPath(root);
        if (Path.GetPathRoot(full)!.TrimEnd(Path.DirectorySeparatorChar).Length >= full.Length)
            throw new InvalidOperationException($"RootPath không hợp lệ: {full}");
        var bp = Path.GetFullPath(backupPath);
        if (bp.StartsWith(full + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Backup nằm trong root node — không restore.");
    }

    /// <summary>
    /// dừng hẳn trước khi chép/đổi root. Mọi kiểm đều qua <see cref="INodeControl.StatusFreshAsync"/> (không
    /// cache). Sau stop (và sau kill nếu được phép) trạng thái mới phải là KHÔNG chạy; còn chạy hoặc không đọc được (Unknown)
    /// ⇒ ném, caller huỷ, không chép.
    /// </summary>
    private async Task StopAsync(INodeControl control, bool allowKill, CancellationToken ct)
    {
        var st = await FreshStatusAsync(control, "trước khi dừng", ct);
        if (!st.IsRunning) return;

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(_options.NodeStopTimeout);
        try
        {
            await control.StopCleanAsync(cts.Token);
        }
        catch (Exception ex) when (allowKill && !ct.IsCancellationRequested)
        {
            _logger.LogSafeWarning(ex, "[QuestHa.Backup] Stop sạch {Node} lỗi, kill.", control.Node);
            await control.KillAsync(ct);
        }

        if (!(await FreshStatusAsync(control, "sau khi dừng", ct)).IsRunning) return;
        if (!allowKill) throw new InvalidOperationException($"Node {control.Node} chưa dừng hẳn — huỷ (không chép khi đang chạy).");
        await control.KillAsync(ct);
        if ((await FreshStatusAsync(control, "sau khi kill", ct)).IsRunning)
            throw new InvalidOperationException($"Node {control.Node} vẫn chạy sau kill — huỷ (không đổi root khi đang chạy).");
    }

    /// <summary>Trạng thái tiến trình đọc mới; lỗi đọc (không phải huỷ) ⇒ Unknown ⇒ <see cref="InvalidOperationException"/>.</summary>
    private static async Task<NodeProcessOutput> FreshStatusAsync(INodeControl control, string when, CancellationToken ct)
    {
        try
        {
            return await control.StatusFreshAsync(ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Node {control.Node}: không đọc được trạng thái tiến trình {when} (Unknown) — huỷ, không chép: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Start (nếu chưa chạy) rồi chờ node trả lời PG với mọi bảng tồn tại và WAL không suspend, trong
    /// <see cref="BackupOptions.NodeStartTimeout"/>. Không đạt ⇒ <see cref="InvalidOperationException"/>; huỷ ⇒
    /// <see cref="OperationCanceledException"/>. Không nuốt lỗi.
    /// </summary>
    private async Task StartAndWaitAsync(string node, INodeControl control, CancellationToken ct)
    {
        if (!_clients.TryGetValue(node, out var client))
            throw new InvalidOperationException($"Không có node client cho {node} — không kiểm được node đã sẵn sàng.");

        using var startBudget = CancellationTokenSource.CreateLinkedTokenSource(ct);
        startBudget.CancelAfter(_options.NodeStartTimeout);
        try { await control.EnsureStartedAsync(startBudget.Token); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        { throw new InvalidOperationException($"Node {node} không khởi động trong NodeStartTimeout."); }

        var deadline = DateTime.UtcNow + _options.NodeStartTimeout;
        var last = "chưa thăm dò được";
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                string? problem = null;
                foreach (var t in _tables)
                {
                    var p = await client.ProbeAsync(t, ct);
                    if (!p.TableExists || p.WalSuspended)
                    {
                        problem = p.Error ?? $"bảng {t.Name} chưa sẵn sàng";
                        break;
                    }
                }

                if (problem is null)
                {
                    ct.ThrowIfCancellationRequested(); // probe khoẻ nhưng caller đã huỷ trong lúc chờ
                    return;
                }
                last = problem;
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                // Debug chỉ nhận exception đã làm sạch (không object exception).
                _logger.LogSafeDebug(ex, "[QuestHa.Backup] Chi tiết probe khởi động {Node}.", node);
                last = QuestErrorClassifierHelper.SafeDescribe(ex);
            }

            if (DateTime.UtcNow >= deadline)
                throw new InvalidOperationException(
                    $"Node {node} chưa sẵn sàng sau {_options.NodeStartTimeout.TotalSeconds:0}s khi start lại: {last}");
            await Task.Delay(500, ct);
        }
    }

    /// <summary>Ghi timeline khi thao tác lỗi/huỷ: bước, lỗi, trạng thái rào/worker và tiến trình để lại.</summary>
    private async Task PublishAbortAsync(string op, string node, INodeControl control, string step, Exception? ex, bool cancelled, bool reopened, bool touched = true)
    {
        string proc;
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            var st = await control.StatusAsync(cts.Token);
            proc = st.IsRunning ? $"tiến trình đang chạy (pid {st.Pid?.ToString() ?? "?"})" : "tiến trình đã dừng";
        }
        catch (Exception sx)
        {
            proc = $"không đọc được trạng thái tiến trình ({QuestErrorClassifierHelper.SafeDescribe(sx)})";
        }

        // Debug chỉ nhận exception đã làm sạch (không object exception).
        if (ex is not null) _logger.LogSafeDebug(ex, "[QuestHa.Backup] Chi tiết {Operation} ở {Step}.", op, step);
        var what = cancelled ? $"bị huỷ ở bước '{step}'" : $"LỖI ở bước '{step}': {(ex is null ? "Unknown" : QuestErrorClassifierHelper.SafeDescribe(ex))}";
        var left = !touched
            ? "từ chối trước khi bắt đầu, không rào/không dừng node"
            : reopened
                ? "node đã start lại, worker resume, đã mở rào"
                : (step.StartsWith("start", StringComparison.Ordinal) ? "KHÔNG resume/unfence" : "KHÔNG start/resume/unfence")
                  + " — node giữ Fenced, worker pause" + (op == "Restore" ? ", chưa nhận đọc; giữ các root .prerestore-*; khôi phục từ backup còn đủ journal hoặc backfill rồi chạy lại restore hợp lệ. node-unfence chỉ được dùng sau khi Phase=None" : "; kiểm tra rồi start/mở rào tay");
        Publish(node, QuestHaEventKindEnum.Error, $"{op} {node} {what} — {left}; {proc}.");
    }

    // kiểm token mỗi chunk 128 KiB; peer điều tiết theo thời gian để không gây tải SQL.
    internal static async Task<(long Files, long Bytes)> CopyDirectoryAsync(string src, string dst, CancellationToken ct,
        Func<CancellationToken, Task>? checkPeer = null)
    {
        checkPeer = QuestBackupStorageService.ThrottlePeerCheck(checkPeer);
        long files = 0, bytes = 0;
        var srcFull = Path.GetFullPath(src);
        QuestBackupStorageService.RejectLinks(dst);
        var sourceFiles = QuestBackupStorageService.Files(srcFull, ct).ToArray();
        Directory.CreateDirectory(dst);
        foreach (var dir in Directory.EnumerateDirectories(srcFull, "*", SearchOption.AllDirectories))
        {
            ct.ThrowIfCancellationRequested();
            Directory.CreateDirectory(Path.Combine(dst, Path.GetRelativePath(srcFull, dir)));
        }
        var buffer = new byte[128 * 1024];
        foreach (var file in sourceFiles)
        {
            ct.ThrowIfCancellationRequested();
            var target = Path.Combine(dst, Path.GetRelativePath(srcFull, file));
            await using var input = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, buffer.Length, FileOptions.Asynchronous | FileOptions.SequentialScan);
            await using var output = new FileStream(target, FileMode.Create, FileAccess.Write, FileShare.None, buffer.Length, FileOptions.Asynchronous);
            int read;
            while ((read = await input.ReadAsync(buffer, ct)) > 0)
            {
                if (checkPeer is not null) await checkPeer(ct);
                await output.WriteAsync(buffer.AsMemory(0, read), ct);
                bytes += read;
            }
            files++;
        }

        return (files, bytes);
    }

    // đo bản đã stop vẫn nghiêm ngặt, kiểm deadline giữa các file.
    internal static (long Files, long Bytes) Measure(string dir, CancellationToken ct = default)
    {
        long files = 0, bytes = 0;
        foreach (var f in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
        {
            ct.ThrowIfCancellationRequested();
            files++;
            bytes += new FileInfo(f).Length;
        }

        return (files, bytes);
    }

    private void Publish(string node, QuestHaEventKindEnum kind, string msg)
    {
        if (kind == QuestHaEventKindEnum.Error) _logger.LogError("[QuestHa.Backup] {Msg}", msg);
        else _logger.LogInformation("[QuestHa.Backup] {Msg}", msg);
        _timeline?.Publish("Backup", node, kind, msg);
    }

    // ba lượt I/O và dự phòng 25%; deadline vẫn bảo vệ root đang tăng.
    private static void CheckCopyBudget(long bytes, BackupOptions options)
    {
        var estimate = bytes / (double)options.MinThroughputBytesPerSecond * 3 * 1.25;
        if (estimate > options.MaxStoppedDuration.TotalSeconds)
            throw new InvalidOperationException($"Ước lượng copy + hash {estimate:0.##}s vượt MaxStoppedDuration; từ chối trước khi dừng node.");
    }

    private Func<CancellationToken, Task> CreatePeerCheck(string node) => async token =>
    {
        token.ThrowIfCancellationRequested();
        if (!QuestBackupScheduleRules.OtherReady(node, _replicators.Values.Where(r => !r.IsRecovering).Select(r => r.Snapshot).ToArray()))
            throw new InvalidOperationException("Peer không còn Ready; huỷ backup để start lại node.");
        await EnsurePeerReadyAsync(node, token);
    };
}
