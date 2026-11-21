using System.Collections.Concurrent;
using Services.Vds.Pipeline;

namespace Services.Vds.Sources.Backfill;

public sealed record BackfillFrameReceipt(
    string CompletionId,
    Task<VdsFrameProcessResult> Completion);

/// <summary>
/// Author: KienLT
/// Description: Kênh acknowledgement chỉ trong tiến trình giữa coordinator và Wolverine handler.
/// Restart làm mất receipt nhưng pendingBackfillFromUtc vẫn còn trên đĩa, nên lượt sau replay an toàn.
/// Created date: 26/08/2026
/// </summary>
public sealed class BackfillCompletionTracker
{
    private readonly ConcurrentDictionary<string, TaskCompletionSource<VdsFrameProcessResult>> _pending =
        new(StringComparer.Ordinal);

    public BackfillFrameReceipt Register()
    {
        var id = Guid.NewGuid().ToString("N");
        var source = new TaskCompletionSource<VdsFrameProcessResult>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        if (!_pending.TryAdd(id, source))
            throw new InvalidOperationException("Không đăng ký được completion id cho backfill.");

        return new BackfillFrameReceipt(id, source.Task);
    }

    public void Complete(string completionId, VdsFrameProcessResult result)
    {
        if (_pending.TryRemove(completionId, out var source))
            source.TrySetResult(result);
    }

    public void Forget(string completionId)
    {
        _pending.TryRemove(completionId, out _);
    }
}
