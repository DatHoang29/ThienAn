using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Infrastructure.Middlewares;
using Shared.Infrastructure.Sse;
using Shared.Utility.Sse;

/// <summary>Portable runtime and HTTP regressions for the production SSE connection helpers.</summary>
public static class ConnectionRegressionTests
{
    public static async Task RunAllAsync()
    {
        var firstChance = new ConcurrentQueue<string>();
        void OnFirstChance(object? _, FirstChanceExceptionEventArgs args)
        {
            var stack = args.Exception.StackTrace ?? "";
            if (args.Exception is OperationCanceledException &&
                (stack.Contains("Shared.Utility.Sse", StringComparison.Ordinal) ||
                 stack.Contains("Shared.Infrastructure.Sse", StringComparison.Ordinal)))
                firstChance.Enqueue(args.Exception.GetType().Name + " " + stack);
        }
        AppDomain.CurrentDomain.FirstChanceException += OnFirstChance;
        try
        {
            await VerifyDirectExecutionAsync(firstChance);
            await VerifyHttpExecutionAsync(firstChance);
        }
        finally { AppDomain.CurrentDomain.FirstChanceException -= OnFirstChance; }
    }

    private static async Task VerifyDirectExecutionAsync(ConcurrentQueue<string> firstChance)
    {
        await ExecuteAsync(SseConnection.Stream((_, _) => Task.CompletedTask), NewContext());
        Assert(firstChance.IsEmpty, "Normal connection completion threw a cancellation exception.");

        var callbackError = new InvalidOperationException("Expected live callback failure.");
        Exception? observed = null;
        try { await ExecuteAsync(SseConnection.Stream((_, _) => Task.FromException(callbackError)), NewContext()); }
        catch (Exception error) { observed = error; }
        Assert(ReferenceEquals(observed, callbackError), "A live callback failure was swallowed or replaced.");
        Assert(firstChance.IsEmpty, "Callback failure shutdown threw a cancellation exception.");

        var transportError = new IOException("Expected heartbeat transport failure.");
        var callbackStopped = false;
        var context = NewContext();
        context.Response.Body = new ThrowingStream(transportError);
        observed = null;
        try
        {
            await ExecuteAsync(SseConnection.Stream(async (_, token) =>
            {
                try { await SseCancellation.DelayAsync(TimeSpan.FromMinutes(1), token); }
                finally { callbackStopped = true; }
            }, TimeSpan.FromMilliseconds(20)), context).WaitAsync(TimeSpan.FromSeconds(3));
        }
        catch (Exception error) { observed = error; }
        Assert(callbackStopped, "Heartbeat transport failure left the callback running.");
        Assert(ReferenceEquals(observed, transportError), "Heartbeat transport failure was swallowed or replaced.");
        Assert(firstChance.IsEmpty, "Heartbeat failure shutdown threw a cancellation exception.");
        Console.WriteLine("PASS Connection: normal close is quiet; live callback and heartbeat transport errors remain observable.");
    }

    private static async Task VerifyHttpExecutionAsync(ConcurrentQueue<string> firstChance)
    {
        var idleDone = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var heartbeatDone = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var lastFetchCount = 0;
        Task<SseFrame> FetchLast(IServiceProvider _, CancellationToken __)
        {
            Interlocked.Increment(ref lastFetchCount);
            return Task.FromResult(SseFrame.Ok("terminal").WithEvent("done").Last());
        }
        Task<string> FetchIdle(IServiceProvider _, CancellationToken __)
            => Task.FromResult("idle payload");
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
        await using var app = builder.Build();
        app.UseMiddleware<SseMiddleware>();
        app.MapGet("/last", async context =>
        {
            var result = SnapshotSse.Poll("regression:last", FetchLast, 1);
            await ExecuteAsync(result, context);
        }).WithMetadata(new SseAttribute { HeartbeatSeconds = 0 });
        app.MapGet("/idle", async context =>
        {
            try
            {
                await ExecuteAsync(SnapshotSse.Poll("regression:idle",
                    FetchIdle, 1), context);
            }
            finally { idleDone.TrySetResult(); }
        }).WithMetadata(new SseAttribute { HeartbeatSeconds = 0 });
        app.MapGet("/heartbeat", async context =>
        {
            try
            {
                // No data write occurs: the default 15-second heartbeat must run independently.
                await ExecuteAsync(SseConnection.Stream((_, token) =>
                    SseCancellation.DelayAsync(TimeSpan.FromMinutes(1), token)), context);
            }
            finally { heartbeatDone.TrySetResult(); }
        }).WithMetadata(new SseAttribute { HeartbeatSeconds = 0 });

        await app.StartAsync();
        try
        {
            var address = app.Services.GetRequiredService<IServer>().Features
                .Get<IServerAddressesFeature>()!.Addresses.Single();
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            var last = await client.GetStringAsync(address + "/last");
            Assert(lastFetchCount == 1 && last.Contains("event: done", StringComparison.Ordinal) &&
                last.Contains("terminal", StringComparison.Ordinal),
                "Terminal SnapshotSse frame did not close the HTTP response after one fetch.");
            Assert(firstChance.IsEmpty, "Terminal SnapshotSse shutdown threw a cancellation exception.");

            await ReceiveThenDisconnectAsync(address + "/idle", "idle payload", TimeSpan.FromSeconds(3));
            await idleDone.Task.WaitAsync(TimeSpan.FromSeconds(3));
            Assert(firstChance.IsEmpty, "Idle client disconnect threw an SSE cancellation exception.");

            var elapsed = System.Diagnostics.Stopwatch.StartNew();
            var heartbeatText = await ReceiveThenDisconnectAsync(address + "/heartbeat", "\"heartbeat\":true", TimeSpan.FromSeconds(20));
            await heartbeatDone.Task.WaitAsync(TimeSpan.FromSeconds(3));
            Assert(heartbeatText.Contains("event: heartbeat", StringComparison.Ordinal),
                "Heartbeat payload could replace the SharedWorker's cached business snapshot.");
            Assert(elapsed.Elapsed >= TimeSpan.FromSeconds(14) && elapsed.Elapsed < TimeSpan.FromSeconds(20),
                "Default heartbeat did not fire at approximately 15 seconds during a blocked callback.");
            Assert(firstChance.IsEmpty, "Heartbeat client disconnect threw an SSE cancellation exception.");
            Console.WriteLine("PASS HTTP: terminal frame closes after one fetch; idle disconnect is quiet; independent default heartbeat arrives at 15 seconds.");
        }
        finally { await app.StopAsync(); }
    }

    private static DefaultHttpContext NewContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.SetEndpoint(new Endpoint(_ => Task.CompletedTask,
            new EndpointMetadataCollection(new SseAttribute { HeartbeatSeconds = 0 }), "regression"));
        return context;
    }

    private static Task ExecuteAsync(SseResult result, HttpContext context)
        => ((IActionResult)result).ExecuteResultAsync(new ActionContext { HttpContext = context });

    private static async Task<string> ReceiveThenDisconnectAsync(string address, string marker, TimeSpan timeout)
    {
        var uri = new Uri(address);
        using var socket = new TcpClient();
        using var deadline = new CancellationTokenSource(timeout);
        await socket.ConnectAsync(uri.Host, uri.Port, deadline.Token);
        await using var stream = socket.GetStream();
        await stream.WriteAsync(Encoding.ASCII.GetBytes("GET " + uri.PathAndQuery + " HTTP/1.1\r\nHost: " +
            uri.Authority + "\r\nConnection: close\r\n\r\n"), deadline.Token);
        var buffer = new byte[4096];
        var received = new StringBuilder();
        while (!received.ToString().Contains(marker, StringComparison.Ordinal))
        {
            var read = await stream.ReadAsync(buffer, deadline.Token);
            Assert(read != 0, "SSE response ended before the expected frame.");
            received.Append(Encoding.UTF8.GetString(buffer, 0, read));
        }
        return received.ToString();
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class ThrowingStream(IOException failure) : MemoryStream
    {
        public override void Write(byte[] buffer, int offset, int count) => throw failure;
        public override void Write(ReadOnlySpan<byte> buffer) => throw failure;
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => Task.FromException(failure);
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            => ValueTask.FromException(failure);
    }
}
