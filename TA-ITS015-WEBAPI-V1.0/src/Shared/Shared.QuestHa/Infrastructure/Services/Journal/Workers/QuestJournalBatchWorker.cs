using System.Threading.Channels;

namespace Shared.QuestHa.Infrastructure.Services.Journal.Workers;

/// <summary>
/// Author: KienLT
/// Description: gom các append đang chờ thành lô, 1 writer ghi tuần tự từng lô (group commit).
/// <list type="bullet">
/// <item>Không chờ cho đầy lô: writer lấy NGAY những gì đang xếp hàng (tối đa MaxBatch). Lúc tải thấp lô = 1 dòng,
///   độ trễ như cũ; lúc tải cao các append dồn trong lúc lô trước đang ghi sẽ đi chung lô sau.</item>
/// <item>ACK chỉ trả SAU KHI lô commit (executor trả về). Lô lỗi ⇒ MỌI append trong lô nhận
///   <see cref="JournalAppendException"/> (không ACK, caller gửi lại) — dữ liệu đã được kiểm trước khi xếp hàng
///   nên lô lỗi là lỗi SQL/kết nối chung, không phải một dòng hỏng.</item>
/// <item>Hàng đợi có trần: đầy ⇒ từ chối ngay, không treo.</item>
/// <item>Caller huỷ (ct) TRƯỚC khi writer lấy ⇒ bỏ khỏi lô. Huỷ SAU khi đã vào lô ⇒ caller nhận huỷ nhưng dòng
///   vẫn có thể commit (giống huỷ giữa lệnh SQL ở đường ghi đơn) — dedup theo khoá ở node xử lý bản gửi lại.</item>
/// <item>Dispose: ngừng nhận, chờ ghi nốt trong hạn drain; quá hạn ⇒ huỷ executor và KẾT THÚC NGAY mọi append còn
///   treo (lô đang ghi + hàng đợi) bằng <see cref="ObjectDisposedException"/>, không chờ executor phản hồi huỷ.
///   Lô đang ghi lúc đó vẫn có thể commit dù caller đã nhận lỗi; ACK muộn bị bỏ (TCS đã kết thúc).
///   Tín hiệu huỷ cho executor phát SAU khi đã kết thúc pending và trên luồng nền: callback huỷ của driver
///   (chạy đồng bộ trong Cancel) có kẹt hay ném lỗi cũng không giữ shutdown.</item>
/// </list>
/// Created date: 27/10/2026
/// </summary>
internal sealed class QuestJournalBatchWorker : IAsyncDisposable
{
    /// <summary>
    /// Author: KienLT
    /// Description: Một yêu cầu append đang chờ lô: entry và TaskCompletionSource trả ACK cho caller.
    /// Created date: 27/10/2026
    /// </summary>
    private sealed class Pending(JournalAppendInput entry)
    {
        public JournalAppendInput Entry { get; } = entry;
        public TaskCompletionSource<JournalAppendOutput> Tcs { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public CancellationTokenRegistration Registration;
    }

    private readonly Func<IReadOnlyList<JournalAppendInput>, CancellationToken, Task<IReadOnlyList<JournalAppendOutput>>> _execute;
    private readonly Channel<Pending> _queue;
    private readonly int _maxBatch;
    private readonly int _capacity;
    // Token huỷ cho executor. Dispose sau khi writer VÀ việc phát huỷ đều xong (executor quá hạn có thể còn dùng token).
    private readonly CancellationTokenSource _stop = new();
    private readonly Task _writer;
    private readonly object _sync = new();
    private List<Pending>? _inflight; // lô writer đang ghi — shutdown quá hạn kết thúc hộ
    private bool _abandoned;          // shutdown quá hạn đã quét (đặt dưới _sync) ⇒ writer không gọi executor nữa
    private Task? _shutdown;
    private long _batches;
    private long _appended;
    private long _rejected;
    private int _largestBatch;
    private bool _closed;

    /// <summary>Khởi tạo bộ gộp lô append journal với executor, giới hạn lô/hàng đợi và đồng hồ.</summary>
    public QuestJournalBatchWorker(
        Func<IReadOnlyList<JournalAppendInput>, CancellationToken, Task<IReadOnlyList<JournalAppendOutput>>> execute,
        int maxBatch,
        int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxBatch, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
        _execute = execute;
        _maxBatch = maxBatch;
        _capacity = capacity;
        _queue = Channel.CreateBounded<Pending>(new BoundedChannelOptions(capacity)
        {
            SingleReader = false, // shutdown quá hạn rút hàng đợi song song với writer còn kẹt
            FullMode = BoundedChannelFullMode.Wait, // chỉ dùng TryWrite ⇒ đầy = false, không chờ
        });
        _writer = Task.Run(WriteLoopAsync);
    }

    /// <summary>Số lô đã ghi thành công / số dòng đã ACK / số append bị từ chối do đầy / lô lớn nhất.</summary>
    public (long Batches, long Appended, long Rejected, int LargestBatch) Stats =>
        (Interlocked.Read(ref _batches), Interlocked.Read(ref _appended), Interlocked.Read(ref _rejected), Volatile.Read(ref _largestBatch));

    /// <summary>Xếp một append vào hàng đợi gom lô; task hoàn tất (ACK) chỉ sau khi lô chứa nó commit. Hàng đợi đầy ⇒ ném ngay JournalAppendException.</summary>
    public Task<JournalAppendOutput> AppendAsync(JournalAppendInput entry, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ct.ThrowIfCancellationRequested();

        var p = new Pending(entry);
        if (ct.CanBeCanceled) p.Registration = ct.Register(static s => ((Pending)s!).Tcs.TrySetCanceled(), p);
        if (!_queue.Writer.TryWrite(p))
        {
            p.Registration.Dispose();
            if (Volatile.Read(ref _closed)) throw new ObjectDisposedException(nameof(QuestJournalBatchWorker));
            Interlocked.Increment(ref _rejected);
            throw new JournalAppendException(
                $"Journal CHƯA ACK: hàng đợi ghi đầy ({_capacity} append chờ) — giảm tải hoặc gửi lại sau.");
        }

        return p.Tcs.Task;
    }

    private async Task WriteLoopAsync()
    {
        try
        {
            // Không truyền token: hàng đợi đã TryComplete và rỗng ⇒ false ⇒ thoát vòng.
            while (await _queue.Reader.WaitToReadAsync().ConfigureAwait(false))
            {
                // List mới mỗi lô: shutdown quá hạn đọc _inflight trong khi writer có thể đã sang lô sau.
                var batch = new List<Pending>(Math.Min(_maxBatch, 64));
                var entries = new List<JournalAppendInput>(batch.Capacity);
                while (batch.Count < _maxBatch && _queue.Reader.TryRead(out var p))
                {
                    if (p.Tcs.Task.IsCompleted) { p.Registration.Dispose(); continue; } // caller đã huỷ
                    batch.Add(p);
                    entries.Add(p.Entry);
                }

                if (batch.Count == 0) continue;
                bool abandoned;
                lock (_sync)
                {
                    // Cùng khoá với lượt quét của shutdown: hoặc shutdown thấy lô này trong _inflight, hoặc writer thấy
                    // _abandoned ở đây ⇒ lô đã lấy khỏi hàng đợi không bao giờ bị bỏ lơ.
                    abandoned = _abandoned;
                    if (!abandoned) _inflight = batch;
                }

                if (abandoned)
                {
                    FailBatch(batch);
                    continue;
                }

                try
                {
                    await WriteBatchAsync(batch, entries).ConfigureAwait(false);
                }
                finally
                {
                    lock (_sync) _inflight = null;
                }
            }
        }
        finally
        {
            FailQueued();
        }
    }

    private void FailQueued()
    {
        var disposed = new ObjectDisposedException(nameof(QuestJournalBatchWorker));
        while (_queue.Reader.TryRead(out var p))
        {
            p.Registration.Dispose();
            p.Tcs.TrySetException(disposed);
        }
    }

    private static void FailBatch(List<Pending> batch)
    {
        var disposed = new ObjectDisposedException(nameof(QuestJournalBatchWorker));
        foreach (var p in batch)
        {
            p.Registration.Dispose();
            p.Tcs.TrySetException(disposed);
        }
    }

    private async Task WriteBatchAsync(List<Pending> batch, List<JournalAppendInput> entries)
    {
        try
        {
            var acks = await _execute(entries, _stop.Token).ConfigureAwait(false);
            if (acks.Count != batch.Count)
                throw new InvalidOperationException($"Journal trả {acks.Count} ACK cho lô {batch.Count} dòng.");

            var acked = 0;
            for (var i = 0; i < batch.Count; i++)
                if (batch[i].Tcs.TrySetResult(acks[i])) acked++; // false = caller đã huỷ / shutdown đã kết thúc hộ
            Interlocked.Increment(ref _batches);
            Interlocked.Add(ref _appended, acked);
            if (batch.Count > Volatile.Read(ref _largestBatch)) Volatile.Write(ref _largestBatch, batch.Count);
        }
        catch (Exception ex)
        {
            // thông điệp CHƯA ACK được caller log; không ghép SQL provider text vào đó.
            Exception error = ex is OperationCanceledException && Volatile.Read(ref _abandoned)
                ? new ObjectDisposedException(nameof(QuestJournalBatchWorker))
                : ex as JournalAppendException
                  ?? new JournalAppendException($"Journal CHƯA ACK (SQL lỗi, lô {batch.Count} dòng): {QuestErrorClassifierHelper.SafeDescribe(ex)}", ex);
            foreach (var p in batch) p.Tcs.TrySetException(error);
        }
        finally
        {
            foreach (var p in batch) p.Registration.Dispose();
        }
    }

    /// <summary>Ngừng nhận; ghi nốt hàng đợi trong 10 s rồi huỷ phần còn lại (xem <see cref="DisposeAsync(TimeSpan)"/>).</summary>
    public async ValueTask DisposeAsync() => await DisposeAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);

    /// <summary>
    /// mọi lời gọi (kể cả đồng thời) chờ CÙNG một tiến trình tắt; hạn drain của lời gọi đầu quyết định.
    /// </summary>
    public Task DisposeAsync(TimeSpan drainTimeout)
    {
        // Task.Run: thân shutdown không chạy đồng bộ trong khoá (kể cả drainTimeout = 0).
        lock (_sync) return _shutdown ??= Task.Run(() => ShutdownAsync(drainTimeout));
    }

    /// <summary>Writer đã thoát hẳn (test chờ thay vì Delay; executor kẹt ⇒ không bao giờ xong).</summary>
    internal Task WriterCompletion => _writer;

    private async Task ShutdownAsync(TimeSpan drainTimeout)
    {
        Volatile.Write(ref _closed, true);
        _queue.Writer.TryComplete();
        try
        {
            await _writer.WaitAsync(drainTimeout).ConfigureAwait(false);
            _stop.Dispose();
            return;
        }
        catch (TimeoutException)
        {
        }

        // quá hạn ⇒ tự kết thúc mọi append còn treo TRƯỚC, không chờ executor.
        Pending[] inflight;
        lock (_sync)
        {
            _abandoned = true;
            inflight = _inflight?.ToArray() ?? Array.Empty<Pending>();
        }

        var disposed = new ObjectDisposedException(nameof(QuestJournalBatchWorker));
        foreach (var p in inflight) p.Tcs.TrySetException(disposed); // registration: writer dispose khi lô kết thúc
        FailQueued();

        // Rồi mới báo huỷ cho executor, trên luồng nền: callback huỷ kẹt/ném lỗi không giữ shutdown; lỗi được quan sát.
        var cancel = Task.Run(() =>
        {
            try { _stop.Cancel(); }
            catch (AggregateException) { /* lỗi của callback huỷ ngoại lai — pending đã kết thúc ở trên */ }
        });
        _ = Task.WhenAll(_writer, cancel).ContinueWith(_ => _stop.Dispose(), TaskScheduler.Default);
    }
}
