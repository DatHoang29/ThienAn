// Author: Codex
// Description: Triển khai SseCancellation.
// Created date: 02/10/2026

namespace Shared.Utility.Sse;

/// <summary>
/// Waits between SSE work without throwing when the connection or producer stops.
/// </summary>
public static class SseCancellation
{
    /// <summary>
    /// Completes normally when the interval expires or <paramref name="ct"/> is cancelled.
    /// Callers must check the token before starting the next operation.
    /// </summary>
    public static async Task DelayAsync(TimeSpan delay, CancellationToken ct)
    {
        if (ct.IsCancellationRequested) return;

        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var timer = new Timer(static state => ((TaskCompletionSource)state!).TrySetResult(),
            completion, delay, Timeout.InfiniteTimeSpan);
        using var cancellation = ct.Register(static state => ((TaskCompletionSource)state!).TrySetResult(),
            completion);
        await completion.Task.ConfigureAwait(false);
    }
}
