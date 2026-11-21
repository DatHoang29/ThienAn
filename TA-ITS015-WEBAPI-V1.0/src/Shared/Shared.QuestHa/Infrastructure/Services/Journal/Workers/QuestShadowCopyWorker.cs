using System.Diagnostics;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace Shared.QuestHa.Infrastructure.Services.Journal.Workers;

/// <summary>
/// Author: KienLT
/// Description: Sao chép Shadow có trần vào journal (WriteMode=Shadow), không để SQL journal chặn pipeline đã ghi thành công
/// node chính. Nằm trong thư viện để QuestHaManager.CopyToShadow uỷ quyền; consumer không thấy trực tiếp.
/// Created date: 27/10/2026
/// </summary>
internal sealed class QuestShadowCopyWorker : BackgroundService
{
    private readonly IJournalStore _journal;
    private readonly ILogger<QuestShadowCopyWorker> _logger;
    private readonly Channel<(QuestTableSpec Table, string Payload)> _queue;
    private readonly int _maxInFlight;
    private long _failures;
    private long _appended;
    private long _pending;
    private long _lastWarning;

    /// <summary>Khởi tạo sao chép Shadow vào journal từ options QuestHa và logger.</summary>
    public QuestShadowCopyWorker(IJournalStore journal, IOptions<QuestHaOptions> options,
        ILogger<QuestShadowCopyWorker> logger)
    {
        _journal = journal;
        _logger = logger;
        // song song chỉ khi gom lô BẬT (lượt chờ là mục trong hàng đợi gom lô — rẻ,
        // đủ để dồn một lô đầy). Tắt gom lô ⇒ mỗi lượt = 1 kết nối + 1 transaction + applock toàn cục ⇒ chạy tuần tự,
        // tránh bão kết nối đúng lúc bật chế độ khẩn cấp.
        _maxInFlight = options.Value.Journal.AppendBatching ? Math.Max(1, options.Value.Journal.AppendMaxBatch) : 1;
        _queue = Channel.CreateBounded<(QuestTableSpec, string)>(new BoundedChannelOptions(
            options.Value.Journal.AppendQueueCapacity)
        {
            SingleReader = true,
            FullMode = BoundedChannelFullMode.Wait,
            AllowSynchronousContinuations = false
        });
    }

    /// <summary>Tổng lỗi serialize/append hoặc lượt bị từ chối do đầy hàng đợi/đang dừng.</summary>
    public long FailureCount => Interlocked.Read(ref _failures);
    /// <summary>Số bản sao đã được journal xác nhận commit.</summary>
    public long AppendedCount => Interlocked.Read(ref _appended);
    /// <summary>Số bản sao đang chờ hoặc đang append; chưa chắc đã bền vững.</summary>
    public long PendingCount => Interlocked.Read(ref _pending);

    /// <summary>Chụp nguyên giá trị sau ghi trực tiếp; không chờ SQL, không ném lỗi Shadow ra pipeline.</summary>
    public void Append(QuestTableSpec table, object entity)
    {
        try
        {
            // Snapshot ngay: downstream có thể sửa entity sau khi InsertAsync trả về.
            // AppendAsync hỗ trợ payload JSON; dùng đúng serializer của Shared.QuestHa, không Canonicalize lại.
            var payload = JsonSerializer.Serialize(entity, entity.GetType(), QuestPayloadHelper.JsonOptions);
            Interlocked.Increment(ref _pending);
            if (_queue.Writer.TryWrite((table, payload))) return;
            Interlocked.Decrement(ref _pending);
            RecordFailure("QueueFullOrStopped");
        }
        catch (Exception ex)
        {
            RecordFailure(ex.GetType().Name);
        }
    }

    /// <inheritdoc />
    // cho tối đa AppendMaxBatch lượt append chạy song song thay vì chờ từng lượt — chờ tuần tự
    // thì thông lượng = 1 / độ trễ 1 lượt ghi SQL (SQL qua mạng > ~3,4 ms là không theo kịp ~294 ev/s, hàng đợi đầy).
    // Thứ tự journal vẫn giữ: AppendAsync xếp vào hàng đợi gom lô ĐỒNG BỘ trước khi await.
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var slots = new SemaphoreSlim(_maxInFlight); // không Dispose: lượt append đang chạy có thể Release sau cùng
        while (true)
        {
            // Lấy chỗ TRƯỚC rồi mới rút khỏi hàng đợi: tổng bản sao giữ trong RAM = trần hàng đợi + số lượt đang chạy.
            await slots.WaitAsync(stoppingToken).ConfigureAwait(false);
            if (!await _queue.Reader.WaitToReadAsync(stoppingToken).ConfigureAwait(false) || !_queue.Reader.TryRead(out var item))
            {
                slots.Release();
                if (_queue.Reader.Completion.IsCompleted) break;
                continue;
            }

            Task append;
            try
            {
                append = _journal.AppendAsync(item.Table, item.Payload, stoppingToken);
            }
            catch (Exception ex) // payload không khớp spec ném ngay khi gọi
            {
                RecordFailure(ex.GetType().Name);
                Interlocked.Decrement(ref _pending);
                slots.Release();
                continue;
            }

            _ = ObserveAsync(append, slots);
        }

        // Hàng đợi đã đóng (StopAsync): chờ các lượt đang chạy trả hết chỗ.
        for (var i = 0; i < _maxInFlight; i++) await slots.WaitAsync(stoppingToken).ConfigureAwait(false);
    }

    private async Task ObserveAsync(Task append, SemaphoreSlim slots)
    {
        try
        {
            await append.ConfigureAwait(false);
            Interlocked.Increment(ref _appended);
        }
        catch (Exception ex)
        {
            RecordFailure(ex.GetType().Name);
        }
        finally
        {
            Interlocked.Decrement(ref _pending);
            slots.Release();
        }
    }

    /// <summary>Ngừng nhận và vét hàng đợi trong ngân sách dừng host; crash có thể mất bản sao RAM.</summary>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _queue.Writer.TryComplete();
        try
        {
            if (ExecuteTask is not null)
                await ExecuteTask.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // đếm ĐỦ số bản sao bỏ lại và log KHÔNG qua throttle — lúc tắt mà
            // bị throttle nuốt thì đối soát A/B không truy được vì sao hao hụt.
            var dropped = PendingCount;
            Interlocked.Add(ref _failures, Math.Max(1, dropped));
            try
            {
                _logger.LogWarning("QuestHa Shadow: dừng quá hạn, bỏ lại {Dropped} bản sao chưa vào journal; tổng lỗi {FailureCount}. Node chính không mất dữ liệu.",
                    dropped, FailureCount);
            }
            catch (Exception)
            {
                // Logger lỗi không được làm hỏng việc dừng host.
            }
        }
        finally
        {
            await base.StopAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private void RecordFailure(string kind)
    {
        var failures = Interlocked.Increment(ref _failures);
        var now = Stopwatch.GetTimestamp();
        var previous = Interlocked.Read(ref _lastWarning);
        if (previous != 0 && Stopwatch.GetElapsedTime(previous, now) < TimeSpan.FromSeconds(30)) return;
        if (Interlocked.CompareExchange(ref _lastWarning, now, previous) != previous) return;
        // Không ghi message/exception SQL: có thể chứa connection string hoặc payload nhạy cảm.
        try
        {
            _logger.LogWarning("QuestHa Shadow: bản sao journal lỗi ({Kind}); tổng lỗi {FailureCount}, đang chờ {PendingCount}. Node chính đã ghi thành công.",
                kind, failures, PendingCount);
        }
        catch (Exception)
        {
            // Logger lỗi cũng không được kích hoạt Wolverine retry sau khi đã ghi node chính.
        }
    }
}
