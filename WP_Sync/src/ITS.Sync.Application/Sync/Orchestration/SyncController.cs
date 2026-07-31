using System;
using System.Threading;
using System.Threading.Tasks;
using ITS.Sync.Core.Abstractions;
using ITS.Sync.Core.Models;
using ITS.Sync.Infrastructure.Persistence;

namespace ITS.Sync.Application.Sync;

/// <summary>
/// Bộ điều khiển đồng bộ dùng chung: sở hữu vòng lặp chạy định kỳ và ủy thác
/// việc chạy một chu kỳ cho <see cref="ISyncManager"/>.
/// Host (WPF/Worker) chỉ Init/Start/Stop và nhận báo cáo/lỗi qua IProgress&lt;T&gt;.
/// </summary>
public sealed class SyncController : ISyncController
{
    private readonly ISyncManager _syncManager;
    private readonly SqlSugarFactory _dbFactory;

    private CancellationTokenSource? _cts;
    private Task? _loop;
    private readonly object _gate = new();

    /// <summary>
    /// Tuần tự hóa MỌI lần chạy đồng bộ (chu kỳ định kỳ và chạy theo tín hiệu NATS),
    /// tránh hai luồng cùng ghi vào DB đích một lúc.
    /// </summary>
    private readonly SemaphoreSlim _runLock = new(1, 1);

    public SyncController(ISyncManager syncManager, SqlSugarFactory dbFactory)
    {
        _syncManager = syncManager;
        _dbFactory = dbFactory;
    }

    /// <summary>Đang chạy khi có task vòng lặp còn sống và chưa bị hủy.</summary>
    public bool IsRunning
    {
        get
        {
            lock (_gate)
            {
                return _loop is { IsCompleted: false } && _cts is { IsCancellationRequested: false };
            }
        }
    }

    /// <summary>
    /// Chuẩn bị trước khi chạy: kiểm tra kết nối DB nguồn ("Source") và đích ("Target").
    /// Không ném lỗi — trả kết quả để host quyết định có Start hay không.
    /// </summary>
    public async Task<SyncInitResult> InitAsync(CancellationToken ct = default)
    {
        var src = await _dbFactory.TestConnectionAsync("Source", ct).ConfigureAwait(false);
        var tgt = await _dbFactory.TestConnectionAsync("Target", ct).ConfigureAwait(false);

        return new SyncInitResult
        {
            SourceOk = src.Ok,
            SourceError = src.Error,
            TargetOk = tgt.Ok,
            TargetError = tgt.Error
        };
    }

    /// <inheritdoc />
    public async Task RunOnceAsync(
        IProgress<SyncRunReport>? onCycle = null,
        IProgress<Exception>? onError = null,
        CancellationToken ct = default)
    {
        await RunGuardedAsync(() => _syncManager.RunOnceAsync(ct), onCycle, onError, ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task RunTableOnceAsync(
        string tableName,
        IProgress<SyncRunReport>? onCycle = null,
        IProgress<Exception>? onError = null,
        CancellationToken ct = default)
    {
        await RunGuardedAsync(() => _syncManager.RunTableOnceAsync(tableName, ct), onCycle, onError, ct)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Chạy một tác vụ đồng bộ dưới khóa tuần tự (không để chu kỳ định kỳ và tín hiệu NATS
    /// ghi chồng lấn), nuốt exception và báo về host qua IProgress.
    /// </summary>
    private async Task RunGuardedAsync(
        Func<Task<SyncRunReport>> run,
        IProgress<SyncRunReport>? onCycle,
        IProgress<Exception>? onError,
        CancellationToken ct)
    {
        try
        {
            await _runLock.WaitAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        try
        {
            var report = await run().ConfigureAwait(false);
            onCycle?.Report(report);
        }
        catch (OperationCanceledException)
        {
            // Hủy chủ động — không coi là lỗi.
        }
        catch (Exception ex)
        {
            onError?.Report(ex);
        }
        finally
        {
            _runLock.Release();
        }
    }

    /// <inheritdoc />
    public void Start(
        int intervalSeconds,
        IProgress<SyncRunReport>? onCycle = null,
        IProgress<Exception>? onError = null)
    {
        if (intervalSeconds < 1) intervalSeconds = 1;

        lock (_gate)
        {
            // Hủy vòng lặp cũ (nếu có) trước khi tạo vòng lặp mới.
            CancelLocked();

            var cts = new CancellationTokenSource();
            _cts = cts;
            var interval = TimeSpan.FromSeconds(intervalSeconds);

            _loop = Task.Run(() => RunLoopAsync(interval, cts.Token, onCycle, onError));
        }
    }

    /// <inheritdoc />
    public void Stop()
    {
        lock (_gate)
        {
            CancelLocked();
        }
    }

    private async Task RunLoopAsync(
        TimeSpan interval,
        CancellationToken token,
        IProgress<SyncRunReport>? onCycle,
        IProgress<Exception>? onError)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                // RunOnceAsync đã tự nuốt exception thường và báo qua onError.
                await RunOnceAsync(onCycle, onError, token).ConfigureAwait(false);

                if (token.IsCancellationRequested) break;

                await Task.Delay(interval, token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Dừng sạch khi bị hủy.
        }
    }

    /// <summary>Hủy + dispose CTS hiện tại. Phải gọi bên trong lock(_gate).</summary>
    private void CancelLocked()
    {
        if (_cts is null) return;

        try
        {
            _cts.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Đã dispose — bỏ qua.
        }
        finally
        {
            _cts.Dispose();
            _cts = null;
            _loop = null;
        }
    }
}
