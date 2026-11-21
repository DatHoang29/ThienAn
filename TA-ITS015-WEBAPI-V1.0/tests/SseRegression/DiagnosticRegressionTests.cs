using System.Text.Json;
using Microsoft.AspNetCore.Http;
using NLog;
using NLog.Config;
using NLog.Targets;
using Shared.Utility.Sse;

public static class DiagnosticRegressionTests
{
    public static void Run()
    {
        var previous = LogManager.Configuration;
        var memory = new MemoryTarget("sse-test") { Layout = "${message}" };
        var configuration = new LoggingConfiguration();
        configuration.AddRule(LogLevel.Info, LogLevel.Fatal, memory, "SSE.Diagnostics");
        LogManager.Configuration = configuration;
        try
        {
            using var cancelled = new CancellationTokenSource();
            var context = new DefaultHttpContext { TraceIdentifier = "test-request", RequestAborted = cancelled.Token };
            context.Request.Path = "/api/tms/tmsincidentsnapshot/stream";
            context.Request.QueryString = new QueryString("?Name=private-query");
            context.Request.Headers.Authorization = "Bearer private-token";
            context.Request.Headers[SseDiagnosticOperation.Header] = "sse-0123456789abcdef0123456789abcdef";
            using (var operation = SseDiagnosticOperation.Begin("incident-monitor-sql", cancelled.Token, context))
            {
                operation.Phase = "sql-read";
                cancelled.Cancel();
                operation.Cancelled(new OperationCanceledException(cancelled.Token));
            }
            var rows = memory.Logs.Select(line => JsonDocument.Parse(line["[SSE diagnostic] ".Length..])).ToList();
            try
            {
                var signal = rows.Single(row => row.RootElement.GetProperty("event").GetString() == "token-cancelled").RootElement;
                Assert(signal.GetProperty("phase").GetString() == "sql-read", "Cancellation phase was lost.");
                Assert(signal.GetProperty("requestAborted").GetBoolean() && signal.GetProperty("tokenCancelled").GetBoolean(), "Request cancellation was misclassified.");
                Assert(signal.GetProperty("connectionId").GetString() == "sse-0123456789abcdef0123456789abcdef", "FE correlation id was lost.");
                Assert(memory.Logs.All(line => !line.Contains("private-query") && !line.Contains("private-token")), "Diagnostics leaked request data.");
            }
            finally { rows.ForEach(row => row.Dispose()); }

            memory.Logs.Clear();
            using (var operation = SseDiagnosticOperation.Begin("incident-monitor-sql", CancellationToken.None, context))
                operation.Cancelled(new OperationCanceledException());
            Assert(memory.Logs.Any(line => line.Contains("\"event\":\"unexpected-cancellation\"")), "Unrequested cancellation was reported as expected.");
            Console.WriteLine("PASS Diagnostics: connection correlation, SQL phase, cancellation flags and request-data exclusion.");
        }
        finally { LogManager.Configuration = previous; }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
