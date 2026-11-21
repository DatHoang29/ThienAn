using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Security.Claims;
using System.Threading.Channels;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Utility.Sse;

public static class HubDisconnectRegressionTests
{
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(5);
    private static SnapshotSseScope Scope() => new("tenant-a", "2",
        new ClaimsPrincipal(new ClaimsIdentity("test")));
    private static TaskCompletionSource<T> Signal<T>() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    public static async Task RunAllAsync()
    {
        await AlreadyCancelledDoesNotStartAProducer();
        await LastSubscriberEndsNormally();
        await DisconnectingOneSubscriberPreservesTheOther();
        await FetchFailureStillReportsUnavailable();
        await UnexpectedFetchCancellationStillReportsUnavailable();
        await ChannelFailureStillPropagates();
        await DelayCancelsWithoutAnException();
    }

    private static ServiceProvider Provider()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        return services.BuildServiceProvider();
    }

    private static SnapshotSseHub Hub(ServiceProvider provider, TestLogger logger)
        => new(provider.GetRequiredService<IServiceScopeFactory>(), logger)
        { TickUnit = TimeSpan.FromMilliseconds(10) };

    private static async Task AlreadyCancelledDoesNotStartAProducer()
    {
        using var provider = Provider();
        var logger = new TestLogger();
        var hub = Hub(provider, logger);
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        var fetched = false;
        await using var stream = hub.SubscribeAsync(Scope(), "f", 1, (_, _) =>
        {
            fetched = true;
            return Task.FromResult<object>(1);
        }, ct: cancelled.Token).GetAsyncEnumerator();
        Check(!await stream.MoveNextAsync(), "An already disconnected client received data.");
        Check(!fetched && hub.ActiveGroupCount == 0, "An already disconnected client started a producer.");
    }

    private static async Task LastSubscriberEndsNormally()
    {
        using var provider = Provider();
        var logger = new TestLogger();
        var hub = Hub(provider, logger);
        using var cancelled = new CancellationTokenSource();
        using var observed = new CancellationExceptions();
        await using var stream = hub.SubscribeAsync(Scope(), "f", 1,
            (_, _) => Task.FromResult<object>(1), ct: cancelled.Token).GetAsyncEnumerator();
        Check(await stream.MoveNextAsync(), "The initial snapshot was missing.");
        var pending = stream.MoveNextAsync().AsTask();
        cancelled.Cancel();
        Check(!await pending.WaitAsync(Limit), "The disconnected stream did not complete normally.");
        await logger.Stopped.Task.WaitAsync(Limit);
        Check(hub.ActiveGroupCount == 0, "The last disconnect left a live group.");
        Check(observed.Count == 0, "The last disconnect raised a first-chance cancellation exception.");
    }

    private static async Task DisconnectingOneSubscriberPreservesTheOther()
    {
        using var provider = Provider();
        var logger = new TestLogger();
        var hub = Hub(provider, logger);
        using var cancelled = new CancellationTokenSource();
        using var observed = new CancellationExceptions();
        var entered = Signal<bool>();
        var release = Signal<object>();
        var calls = 0;
        async Task<object> Fetch(IServiceProvider _, CancellationToken __)
        {
            if (Interlocked.Increment(ref calls) == 1) return 1;
            entered.TrySetResult(true);
            return await release.Task;
        }
        var scope = Scope();
        await using var leaving = hub.SubscribeAsync(scope, "f", 1, Fetch,
            ct: cancelled.Token).GetAsyncEnumerator();
        Check(await leaving.MoveNextAsync(), "The initial snapshot was missing.");
        await using var staying = hub.SubscribeAsync(scope, "f", 1, Fetch).GetAsyncEnumerator();
        Check(await staying.MoveNextAsync(), "The shared latest snapshot was missing.");
        var leavingRead = leaving.MoveNextAsync().AsTask();
        var stayingRead = staying.MoveNextAsync().AsTask();
        await entered.Task.WaitAsync(Limit);
        cancelled.Cancel();
        Check(!await leavingRead.WaitAsync(Limit), "The disconnected subscriber remained open.");
        Check(hub.SubscriberCount($"{scope.DataScopeKey}|r=1|f") == 1,
            "Disconnecting one subscriber removed the other subscriber.");
        release.TrySetResult(2);
        Check(await stayingRead.WaitAsync(Limit) && Equals(staying.Current, 2),
            "The remaining subscriber did not receive the next snapshot.");
        await staying.DisposeAsync();
        await logger.Stopped.Task.WaitAsync(Limit);
        Check(hub.ActiveGroupCount == 0, "The remaining subscriber left a live group after disposal.");
        Check(observed.Count == 0, "A subscriber disconnect raised a first-chance cancellation exception.");
    }

    private static async Task FetchFailureStillReportsUnavailable()
        => await FetchErrorIsReported(new InvalidOperationException("real fetch failure"));

    private static async Task UnexpectedFetchCancellationStillReportsUnavailable()
        => await FetchErrorIsReported(new OperationCanceledException("unrelated fetch cancellation"));

    private static async Task FetchErrorIsReported(Exception fault)
    {
        using var provider = Provider();
        var logger = new TestLogger();
        var hub = Hub(provider, logger);
        await using var stream = hub.SubscribeAsync(Scope(), "f", 1,
            (_, _) => Task.FromException<object>(fault)).GetAsyncEnumerator();
        Check(await stream.MoveNextAsync(), "A failed fetch did not emit a control frame.");
        Check(stream.Current.GetType().Name == "UnavailableMarker", "A failed fetch lost its unavailable signal.");
        Check(logger.Exceptions.Any(ex => ReferenceEquals(ex, fault)), "A real fetch error was hidden from logging.");
        await stream.DisposeAsync();
        await logger.Stopped.Task.WaitAsync(Limit);
    }

    private static async Task ChannelFailureStillPropagates()
    {
        using var provider = Provider();
        var logger = new TestLogger();
        var hub = Hub(provider, logger);
        var scope = Scope();
        await using var stream = hub.SubscribeAsync(scope, "f", 1,
            (_, _) => Task.FromResult<object>(1)).GetAsyncEnumerator();
        Check(await stream.MoveNextAsync(), "The initial snapshot was missing.");
        var pending = stream.MoveNextAsync().AsTask();
        var groups = (IDictionary)typeof(SnapshotSseHub).GetField("_groups", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(hub)!;
        var group = groups[$"{scope.DataScopeKey}|r=1|f"]!;
        var subscribers = (IEnumerable)group.GetType().GetProperty("Subscribers")!.GetValue(group)!;
        var channel = subscribers.Cast<Channel<object>>().Single();
        var fault = new InvalidOperationException("real channel failure");
        channel.Writer.TryComplete(fault);
        try
        {
            await pending.WaitAsync(Limit);
            throw new InvalidOperationException("A real channel error was suppressed.");
        }
        catch (InvalidOperationException ex) when (ReferenceEquals(ex, fault)) { }
        await logger.Stopped.Task.WaitAsync(Limit);
        Check(hub.ActiveGroupCount == 0, "A failed subscriber left a live group.");
    }

    private static async Task DelayCancelsWithoutAnException()
    {
        using var cancelled = new CancellationTokenSource();
        using var observed = new CancellationExceptions();
        var waiting = SseCancellation.DelayAsync(TimeSpan.FromMinutes(1), cancelled.Token);
        Check(!waiting.IsCompleted, "A live delay completed prematurely.");
        cancelled.Cancel();
        await waiting.WaitAsync(Limit);
        await SseCancellation.DelayAsync(TimeSpan.FromMinutes(1), cancelled.Token);
        Check(observed.Count == 0, "A cancelled delay raised a first-chance cancellation exception.");
    }

    private sealed class TestLogger : ILogger<SnapshotSseHub>
    {
        public TaskCompletionSource<bool> Stopped { get; } = Signal<bool>();
        public ConcurrentQueue<Exception> Exceptions { get; } = new();
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (exception is not null) Exceptions.Enqueue(exception);
            if (formatter(state, exception).Contains(" dung:", StringComparison.Ordinal)) Stopped.TrySetResult(true);
        }
    }

    private sealed class CancellationExceptions : IDisposable
    {
        private int _count;
        public int Count => Volatile.Read(ref _count);
        public CancellationExceptions() => AppDomain.CurrentDomain.FirstChanceException += Observe;
        private void Observe(object? sender, FirstChanceExceptionEventArgs args)
        {
            if (args.Exception is OperationCanceledException) Interlocked.Increment(ref _count);
        }
        public void Dispose() => AppDomain.CurrentDomain.FirstChanceException -= Observe;
    }
}
