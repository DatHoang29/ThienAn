using Services.Vds.Configuration;
using Services.Vds.Catalog;
using Services.Vds.Pipeline.Quarantine;
using Services.Vds.Pipeline.Queue;
using Services.Vds.Pipeline;
using Services.Vds.Pipeline.State;

namespace Services.Vds.Sources.Backfill;

public enum BackfillTrigger
{
    Startup,
    Reconnect,
    Manual,
    Recovery,
}

public sealed record BackfillRunResult(
    bool WasRun,
    int Pages,
    int Frames,
    DateTime? FromUtc,
    DateTime? ToUtc,
    string? SkipReason = null,
    int Quarantined = 0);

/// <summary>
/// Author: KienLT
/// Description: Tính cửa sổ bù từ checkpoint và đưa từng frame history vào cùng queue vds-data.
/// Chỉ xác nhận coverage sau khi handler đã insert và ghi checkpoint cho mọi frame.
/// Created date: 26/08/2026
/// </summary>
public sealed class BackfillCoordinator : IDisposable
{
    /// <summary>
    /// Số lượt thử lại khi compare-and-swap của checkpoint trượt vì đường live vừa ghi gap. Đủ lớn để chịu
    /// vài gap live liên tiếp, đủ nhỏ để không giữ một lượt bù ở đây mãi.
    /// </summary>
    private const int CasRetryLimit = 5;

    private readonly Services.Vds.Features.Journey.JourneyProgressStore _journeyProgress;
    private readonly SemaphoreSlim _runLock = new(1, 1);
    private readonly Services.Vds.Features.Summary.SummaryProgressStore _summaryProgress;
    private readonly IVdsConfigManager _config;
    private readonly CameraManager _cameras;
    private readonly IngestionCheckpointStore _checkpoint;
    private readonly HistoryBackfillClient _history;
    private readonly VdsFrameDispatcher _dispatcher;
    private readonly BackfillCompletionTracker _completion;
    private readonly VdsQuarantineStore _quarantine;
    private readonly ILogger<BackfillCoordinator> _logger;

    public BackfillCoordinator(
        IVdsConfigManager config,
        CameraManager cameras,
        IngestionCheckpointStore checkpoint,
        HistoryBackfillClient history,
        VdsFrameDispatcher dispatcher,
        BackfillCompletionTracker completion,
        VdsQuarantineStore quarantine,
        ILogger<BackfillCoordinator> logger, Services.Vds.Features.Summary.SummaryProgressStore summaryProgress, Services.Vds.Features.Journey.JourneyProgressStore? journeyProgress = null)
    {
        _journeyProgress = journeyProgress ?? new Services.Vds.Features.Journey.JourneyProgressStore(config);
        _config = config;
        _cameras = cameras;
        _checkpoint = checkpoint;
        _history = history;
        _dispatcher = dispatcher;
        _completion = completion;
        _quarantine = quarantine;
        _logger = logger;
        _summaryProgress = summaryProgress;
    }

    public async Task<BackfillRunResult> RunAsync(
        BackfillTrigger trigger, CancellationToken ct = default)
    {
        var initial = _config.Current;
        var initialOptions = initial.Backfill;
        var skipReason = SkipReason(initialOptions, trigger);
        if (skipReason is not null)
            return new BackfillRunResult(false, 0, 0, null, null, skipReason);

        await _runLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            // Snapshot lại sau khi lấy lock vì một lượt khác có thể vừa chạy xong hoặc cấu hình vừa đổi.
            var runtime = _config.Current;
            var backfill = runtime.Backfill.Clone();
            var ingestion = runtime.Ingestion.Clone();
            skipReason = SkipReason(backfill, trigger);
            if (skipReason is not null)
                return new BackfillRunResult(false, 0, 0, null, null, skipReason);

            if (!runtime.QuestEnabled)
                throw new InvalidOperationException("Không thể backfill khi QuestConnectionId đang rỗng.");
            if (_cameras.Count == 0)
                throw new InvalidOperationException("Không thể backfill khi camera cache chưa sẵn sàng.");

            var toUtc = DateTime.UtcNow;
            var checkpoint = _checkpoint.Current;
            if (trigger == BackfillTrigger.Recovery && checkpoint.PendingBackfillFromUtc == null)
                return new BackfillRunResult(false, 0, 0, null, null, "Gap already repaired");
            ValidateNotFuture("lastOccurredUtc", checkpoint.LastOccurredUtc, toUtc);
            ValidateNotFuture("pendingBackfillFromUtc", checkpoint.PendingBackfillFromUtc, toUtc);
            ValidateNotFuture("lastBackfillThroughUtc", checkpoint.LastBackfillThroughUtc, toUtc);

            var maxFromUtc = toUtc.AddHours(-backfill.EffectiveMaxHours);
            DateTime fromUtc;
            if (checkpoint.PendingBackfillFromUtc.HasValue)
            {
                // Không clamp hoặc trừ overlap lần nữa: đây là đầu cửa sổ dở dang đã durable.
                fromUtc = checkpoint.PendingBackfillFromUtc.Value;
                _logger.LogWarning(
                    "[Backfill] Tiếp tục cửa sổ dở dang từ {From:O}; DEDUP bảo vệ các frame replay.",
                    fromUtc);
            }
            else
            {
                var cursorUtc = checkpoint.LastBackfillThroughUtc ?? checkpoint.LastOccurredUtc;

                if (cursorUtc.HasValue)
                {
                    fromUtc = cursorUtc.Value.AddSeconds(-backfill.EffectiveOverlapSeconds);
                }
                else
                {
                    fromUtc = maxFromUtc;
                }
            }

            // Ghi pending trước request history đầu tiên và giữ revision để completion compare-and-swap.
            var pendingLease = await _checkpoint.MarkBackfillStartedAsync(
                fromUtc, ct).ConfigureAwait(false);
            fromUtc = pendingLease.FromUtc;
            ValidateNotFuture("pendingBackfillFromUtc", fromUtc, toUtc);

            if (fromUtc >= toUtc)
            {
                await CompleteWindowAsync(pendingLease, toUtc, ct).ConfigureAwait(false);
                return new BackfillRunResult(true, 0, 0, fromUtc, toUtc);
            }

            _logger.LogInformation(
                "[Backfill] Bắt đầu ({Trigger}) {From:O}..{To:O}; revision={Revision}, " +
                "checkpoint event={EventId}, at={At:O}.",
                trigger,
                fromUtc,
                toUtc,
                pendingLease.Revision,
                checkpoint.LastEventId,
                checkpoint.LastOccurredUtc);

            var pages = 0;
            var frames = 0;
            var quarantined = 0;
            var frameTimeout = TimeSpan.FromSeconds(
                backfill.EffectiveFrameProcessingTimeoutSeconds);

            for (var chunkFrom = fromUtc; chunkFrom < toUtc;)
            {
                var chunkTo = chunkFrom.AddHours(1) < toUtc ? chunkFrom.AddHours(1) : toUtc;
                await foreach (var page in _history.QueryPagesAsync(
                                   ingestion, backfill, chunkFrom, chunkTo, ct).ConfigureAwait(false))
                {
                    pages++;
                    var framesBeforePage = frames;
                    var quarantinedBeforePage = quarantined;
                    for (var index = 0; index < page.Frames.Count; index++)
                    {
                        ct.ThrowIfCancellationRequested();
                        var frame = page.Frames[index];
                        var frameOffset = page.Offset + index;
                        var receipt = _completion.Register();
                        using var frameCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                        frameCts.CancelAfter(frameTimeout);

                        try
                        {
                            var processingDeadlineUtc = DateTime.UtcNow.Add(frameTimeout);
                            var accepted = await _dispatcher.DispatchAsync(
                                frame,
                                isBackfill: true,
                                ct: frameCts.Token,
                                completionId: receipt.CompletionId,
                                processingDeadlineUtc: processingDeadlineUtc).ConfigureAwait(false);

                            if (!accepted)
                            {
                                ct.ThrowIfCancellationRequested();
                                if (frameCts.IsCancellationRequested)
                                {
                                    throw new TimeoutException(
                                        $"Hết {frameTimeout.TotalSeconds:0}s khi enqueue frame history " +
                                        $"page={page.PageNumber}, offset={frameOffset}.");
                                }

                                throw new InvalidOperationException(
                                    $"Dispatcher từ chối frame history ở page={page.PageNumber}, " +
                                    $"offset={frameOffset}.");
                            }

                            VdsFrameProcessResult result;
                            try
                            {
                                result = await receipt.Completion
                                    .WaitAsync(frameCts.Token)
                                    .ConfigureAwait(false);
                            }
                            catch (OperationCanceledException ex) when (!ct.IsCancellationRequested)
                            {
                                throw new TimeoutException(
                                    $"Hết {frameTimeout.TotalSeconds:0}s khi xử lý frame history " +
                                    $"page={page.PageNumber}, offset={frameOffset}.", ex);
                            }

                            switch (result.Status)
                            {
                                case VdsFrameProcessStatus.Persisted:
                                case VdsFrameProcessStatus.SkippedStatistic:
                                // Lượt trùng bị cửa chống trùng loại vẫn là frame ĐÃ XỬ LÝ XONG: dữ liệu không
                                // sai, ta chủ động không ghi. Coi là reject thì cả phiên bù sẽ dừng giữa đường.
                                case VdsFrameProcessStatus.SkippedDuplicate:
                                    frames++;
                                    break;

                                // ĐỔI HÀNH VI 23/09/2026 — trước đây nhánh này ném InvalidDataException.
                                //
                                // Lỗi đó thoát ra tới SseEventSource và vì RequireSuccessBeforeLive=true nên
                                // đường LIVE cũng dừng theo: đo trên máy thật, một bản tin của camera
                                // 10.10.12.15 chưa khai danh mục giữ worker đứng 2 ngày (pending revision lên
                                // 296) và không ghi được một bản tin mới nào. Một bản tin xấu KHÔNG được phép
                                // dừng cả đường dữ liệu.
                                //
                                // Giờ frame vào vùng chờ rồi lượt bù ĐI TIẾP. Không tính vào biến frames vì nó
                                // không được ghi — số ở log tổng kết phải là số bản tin thật sự vào bảng.
                                // Vùng chờ ghi thất bại thì Add ném, và lỗi đó ĐƯỢC PHÉP dừng phiên: lúc đó
                                // frame không còn nơi nào giữ, đi tiếp là mất dữ liệu im lặng.
                                case VdsFrameProcessStatus.Rejected:
                                    _quarantine.Add(frame, result);
                                    quarantined++;
                                    break;

                                default:
                                    throw new InvalidOperationException(
                                        $"Trạng thái xử lý frame history không hợp lệ: {result.Status}.");
                            }
                        }
                        finally
                        {
                            _completion.Forget(receipt.CompletionId);
                        }
                    }

                    _logger.LogInformation(
                        "[Backfill] Đã xử lý page {Page}: {Count} frame, ghi {Written}, vùng chờ {Quarantined} " +
                        "(tổng đã ghi {Total}).",
                        page.PageNumber, page.Frames.Count, frames - framesBeforePage,
                        quarantined - quarantinedBeforePage, frames);
                }

                _summaryProgress.Rewind(chunkFrom);
                _journeyProgress.Rewind(chunkFrom);
                pendingLease = await AdvanceChunkAsync(pendingLease, chunkTo, ct).ConfigureAwait(false);
                chunkFrom = chunkTo;
            }
            await CompleteWindowAsync(pendingLease, toUtc, ct).ConfigureAwait(false);

            if (quarantined > 0)
            {
                _logger.LogWarning(
                    "[Backfill] Có {Quarantined} bản tin vào vùng chờ trong lượt này; đang chờ danh mục: " +
                    "{Waiting}. Lượt bù VẪN hoàn tất — xem quarantine/index.json để đối soát.",
                    quarantined, _quarantine.ReplayableCount);
            }

            _logger.LogInformation(
                "[Backfill] Hoàn tất xử lý {Frames} frame/{Pages} page từ {From:O}..{To:O}; " +
                "đã ghi lastBackfillThroughUtc và xóa pending revision={Revision}.",
                frames, pages, fromUtc, toUtc, pendingLease.Revision);

            return new BackfillRunResult(true, pages, frames, fromUtc, toUtc, null, quarantined);
        }
        finally
        {
            _runLock.Release();
        }
    }

    private async Task CompleteWindowAsync(
        BackfillPendingLease pendingLease,
        DateTime toUtc,
        CancellationToken ct)
    {
        // Persist summary invalidation before acknowledging history coverage.
        _summaryProgress.Rewind(pendingLease.FromUtc);
        _journeyProgress.Rewind(pendingLease.FromUtc);

        var lease = pendingLease;
        for (var attempt = 1; attempt <= CasRetryLimit; attempt++)
        {
            if (await _checkpoint.TryCompleteBackfillAsync(lease, toUtc, ct).ConfigureAwait(false)) return;

            // Marker đổi nghĩa là đường LIVE vừa ghi một gap mới trong lúc ta đang bù — chuyện BÌNH THƯỜNG
            // từ 23/09/2026, khi live không còn phải chờ backfill xong mới chạy. Lấy lease mới rồi xác nhận
            // lại; xem chú thích ở AdvanceChunkAsync.
            lease = await RenewLeaseAsync(lease, attempt, "xác nhận hoàn tất cửa sổ", ct).ConfigureAwait(false);
        }

        throw new InvalidOperationException(
            $"Pending backfill đổi liên tục sau {CasRetryLimit} lượt thử xác nhận; giữ marker mới và " +
            "không xác nhận coverage để lượt sau bù lại.");
    }

    /// <summary>
    /// Ghi nhận đã bù xong một chunk, chịu được việc đường live ghi gap xen vào giữa.
    ///
    /// <para><b>Vì sao cần vòng thử lại.</b> <c>IngestionCheckpointStore</c> bảo vệ pending bằng
    /// compare-and-swap trên <c>(FromUtc, Revision)</c>, và <c>MarkLiveGapAsync</c> của đường live TĂNG
    /// revision. Trước 23/09/2026 hai đường không bao giờ chạy cùng lúc nên CAS không bao giờ trượt; từ khi
    /// live chạy song song với bù, mỗi gap live sẽ làm lượt bù đang chạy trượt CAS. Coi đó là lỗi thì mỗi gap
    /// live đánh đổ một phiên bù — đúng loại vòng lặp mà đợt sửa này dẹp.</para>
    ///
    /// <para>CAS trượt KHÔNG phải lỗi: đọc lại marker, lấy lease mới, xác nhận lại ĐÚNG chunk vừa xong
    /// (không quay lại từ đầu). Trần <see cref="CasRetryLimit"/> để một luồng ghi gap dồn dập không giữ ta ở
    /// đây mãi; hết trần thì ném, và pending còn nguyên nên lượt sau bù lại — DEDUP lo phần trùng.</para>
    /// </summary>
    private async Task<BackfillPendingLease> AdvanceChunkAsync(
        BackfillPendingLease pendingLease,
        DateTime chunkTo,
        CancellationToken ct)
    {
        var lease = pendingLease;
        for (var attempt = 1; attempt <= CasRetryLimit; attempt++)
        {
            var advanced = await _checkpoint
                .TryAdvanceBackfillAsync(lease, chunkTo, ct)
                .ConfigureAwait(false);
            if (advanced is not null) return advanced;

            lease = await RenewLeaseAsync(lease, attempt, "ghi nhận chunk", ct).ConfigureAwait(false);
        }

        throw new InvalidOperationException(
            $"Pending backfill đổi liên tục sau {CasRetryLimit} lượt thử ghi nhận chunk; giữ nguyên khoảng " +
            "pending để lượt sau bù lại.");
    }

    /// <summary>
    /// Lấy lease mới sau khi CAS trượt. Mốc đầu cửa sổ lấy từ marker hiện tại, nên nếu gap live vừa ghi CŨ
    /// HƠN cửa sổ đang bù thì lượt sau sẽ bù cả phần cũ hơn đó.
    /// </summary>
    private async Task<BackfillPendingLease> RenewLeaseAsync(
        BackfillPendingLease stale, int attempt, string action, CancellationToken ct)
    {
        var current = _checkpoint.Current;
        var fromUtc = current.PendingBackfillFromUtc ?? stale.FromUtc;
        var renewed = await _checkpoint.MarkBackfillStartedAsync(fromUtc, ct).ConfigureAwait(false);

        _logger.LogInformation(
            "[Backfill] Marker đổi trong lúc {Action} (live vừa ghi gap): revision {Stale} → {Renewed}, " +
            "from {From:O}. Thử lại lượt {Attempt}/{Limit}.",
            action, stale.Revision, renewed.Revision, renewed.FromUtc, attempt, CasRetryLimit);

        return renewed;
    }

    private static void ValidateNotFuture(
        string field,
        DateTime? value,
        DateTime toUtc)
    {
        if (value.HasValue && value.Value > toUtc)
        {
            throw new InvalidDataException(
                $"Checkpoint {field}={value.Value:O} nằm sau thời điểm backfill to={toUtc:O}.");
        }
    }

    private static string? SkipReason(BackfillOptions options, BackfillTrigger trigger)
    {
        if (!options.Enabled) return "Backfill:Enabled=false";
        if (trigger == BackfillTrigger.Startup && !options.BackfillOnStartup)
            return "BackfillOnStartup=false";
        if (trigger == BackfillTrigger.Reconnect && !options.BackfillOnReconnect)
            return "BackfillOnReconnect=false";
        return null;
    }

    public void Dispose() => _runLock.Dispose();
}
