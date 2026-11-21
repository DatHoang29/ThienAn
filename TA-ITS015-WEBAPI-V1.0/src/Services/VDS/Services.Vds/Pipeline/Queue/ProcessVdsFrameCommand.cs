using Services.Vds.Configuration;
using Services.Vds.Sources.Backfill;
using Services.Vds.Pipeline.Mapping;
using Services.Vds.Pipeline;
using Services.Vds.Pipeline.Quarantine;
using Services.Vds.Pipeline.State;
using Wolverine;

namespace Services.Vds.Pipeline.Queue;

/// <summary>
/// Author: KienLT
/// Description: Consumer mỏng của queue vds-data; lỗi persistence được để Wolverine retry.
/// Created date: 25/08/2026
/// </summary>
public sealed class ProcessVdsFrameCommandHandler : IWolverineHandler
{
    private readonly VdsFrameProcessor _processor;
    private readonly BackfillCompletionTracker _completion;
    private readonly IngestionCheckpointStore _checkpoint;
    private readonly IVdsConfigManager _config;
    private readonly VdsQuarantineStore _quarantine;
    private readonly ILogger<ProcessVdsFrameCommandHandler> _logger;

    public ProcessVdsFrameCommandHandler(
        VdsFrameProcessor processor,
        BackfillCompletionTracker completion,
        IngestionCheckpointStore checkpoint,
        IVdsConfigManager config,
        VdsQuarantineStore quarantine,
        ILogger<ProcessVdsFrameCommandHandler> logger)
    {
        _processor = processor;
        _completion = completion;
        _checkpoint = checkpoint;
        _config = config;
        _quarantine = quarantine;
        _logger = logger;
    }

    public async Task HandleAsync(
        ProcessVdsFrameCommand command,
        CancellationToken cancellationToken)
    {
        using var deadlineCts = CreateDeadlineTokenSource(
            command.ProcessingDeadlineUtc, cancellationToken);
        var processingToken = deadlineCts?.Token ?? cancellationToken;

        VdsFrameProcessResult result;
        try
        {
            result = await _processor.ProcessAsync(
                command.Json, processingToken, command.IsBackfill).ConfigureAwait(false);
        }
        catch when (!command.IsBackfill)
        {
            // Kể cả lỗi DB/checkpoint hết retry, frame live sau đó không được phép che mất khoảng trống.
            await MarkLiveGapAsync(command.Json).ConfigureAwait(false);
            throw;
        }

        if (!command.IsBackfill && result.Status == VdsFrameProcessStatus.Rejected)
        {
            // Bản tin vào vùng chờ TRƯỚC khi quyết định đánh dấu gap: đây là nơi duy nhất còn giữ được frame
            // live không xử lý được, vì lượt bù sau này chưa chắc gặp lại nó.
            _quarantine.Add(command.Json, result);

            // CHỈ đánh dấu gap cho bản tin THỬ LẠI ĐƯỢC. Trước 23/09/2026 mọi Rejected đều đánh dấu, và đó
            // là nửa còn lại của vòng lặp chết: dữ liệu sai vĩnh viễn sinh ra một khoảng pending mà lượt bù
            // nào cũng vướng đúng bản tin đó. Với dữ liệu sai thì không có gì để bù — bù lại cũng cho cùng
            // một kết quả.
            if (result.RejectKind == VdsRejectKind.Unmapped)
                await MarkLiveGapAsync(command.Json).ConfigureAwait(false);
            else
                _logger.LogWarning(
                    "[Ingestion] Bản tin live event={EventId} sai vĩnh viễn nên KHÔNG đánh dấu gap: {Reason}",
                    result.VendorEventId, result.Reason);
        }

        // Chỉ complete sau khi processor đã insert và ghi checkpoint thành công. Nếu processor ném lỗi,
        // Wolverine retry command và receipt vẫn pending để coordinator không xác nhận coverage giả.
        if (command.IsBackfill && !string.IsNullOrWhiteSpace(command.CompletionId))
            _completion.Complete(command.CompletionId, result);
    }

    private async Task MarkLiveGapAsync(string rawJson)
    {
        var runtime = _config.Current;
        var backfill = runtime.Backfill;
        var nowUtc = DateTime.UtcNow;

        var envelope = VdsEnvelopeCodec.TryParse(rawJson, out _);
        var eventUtc = envelope is null
            ? null
            : VdsTrafficDataMapper.ResolveDetectTime(envelope);
        var anchorUtc = eventUtc ?? _checkpoint.Current.LastOccurredUtc ?? nowUtc;

        if (anchorUtc > nowUtc) anchorUtc = nowUtc;

        var fromUtc = anchorUtc.AddSeconds(-backfill.EffectiveOverlapSeconds);

        await _checkpoint.MarkBackfillStartedAsync(
            fromUtc, CancellationToken.None).ConfigureAwait(false);

        _logger.LogWarning(
            "[Ingestion] Đã đánh dấu gap live để backfill lại từ {From:O}.", fromUtc);
    }

    private static CancellationTokenSource? CreateDeadlineTokenSource(
        DateTime? deadlineUtc,
        CancellationToken cancellationToken)
    {
        if (!deadlineUtc.HasValue) return null;

        var normalizedDeadline = deadlineUtc.Value.Kind switch
        {
            DateTimeKind.Utc => deadlineUtc.Value,
            DateTimeKind.Local => deadlineUtc.Value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(deadlineUtc.Value, DateTimeKind.Utc),
        };

        var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var remaining = normalizedDeadline - DateTime.UtcNow;
        if (remaining <= TimeSpan.Zero)
            source.Cancel();
        else
            source.CancelAfter(remaining);

        return source;
    }
}

/// <summary>
/// Author: KienLT
/// Description: Một frame JSON thô đi qua local queue tuần tự vds-data.
/// Created date: 25/08/2026
/// </summary>
/// <param name="Json">Nội dung JSON sau tiền tố <c>data:</c> của SSE, hoặc payload từ injector.</param>
/// <param name="IsBackfill">Cho biết frame đến từ API history thay vì nguồn live.</param>
/// <param name="CompletionId">Correlation id nội bộ để backfill chờ insert và checkpoint hoàn tất.</param>
/// <param name="ProcessingDeadlineUtc">Deadline UTC của frame backfill, được handler truyền xuống DB.</param>
public sealed record ProcessVdsFrameCommand(
    string Json,
    bool IsBackfill = false,
    string? CompletionId = null,
    DateTime? ProcessingDeadlineUtc = null);
