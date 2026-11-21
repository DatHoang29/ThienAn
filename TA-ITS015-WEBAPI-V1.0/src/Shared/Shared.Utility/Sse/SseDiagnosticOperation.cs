// Author: Codex
// Description: Correlates connection cancellation with the operation that was in flight. Never logs credentials or query data.
// Created date: 02/10/2026

using System.Diagnostics;
using System.Text.Json;
using Furion;
using Microsoft.AspNetCore.Http;
using NLog;

namespace Shared.Utility.Sse;

/// <summary>Correlates connection cancellation with the operation that was in flight. Never logs credentials or query data.</summary>
public sealed class SseDiagnosticOperation : IDisposable
{
    public const string Header = "X-SSE-Diagnostic-Id";
    private static readonly Logger Logger = LogManager.GetLogger("SSE.Diagnostics");
    private readonly Stopwatch _elapsed = Stopwatch.StartNew();
    private readonly CancellationToken _token;
    private readonly CancellationToken _requestAborted;
    private readonly CancellationTokenRegistration _registration;
    private readonly string _operation;
    private readonly string _connectionId;
    private readonly string? _requestId;
    private readonly string? _path;
    private string _outcome = "ended";

    public string Phase { get; set; } = "starting";

    public static SseDiagnosticOperation Begin(string operation, CancellationToken token, HttpContext? context = null)
        => new(operation, token, context ?? CurrentContext());

    private SseDiagnosticOperation(string operation, CancellationToken token, HttpContext? context)
    {
        _operation = operation;
        _token = token;
        _requestAborted = context?.RequestAborted ?? default;
        _requestId = context?.TraceIdentifier;
        _path = context?.Request.Path.Value;
        var supplied = context?.Request.Headers[Header].ToString();
        _connectionId = supplied is not null && supplied.StartsWith("sse-", StringComparison.Ordinal)
            && Guid.TryParseExact(supplied[4..], "N", out _) ? supplied : "request:" + (_requestId ?? "none");
        Event("operation-start");
        _registration = token.Register(() => Event("token-cancelled"));
    }

    public void Complete(int? rowCount = null)
    {
        _outcome = _token.IsCancellationRequested ? "cancelled" : "completed";
        Event("operation-" + _outcome, rowCount: rowCount);
    }

    public void Cancelled(OperationCanceledException error)
    {
        _outcome = _token.IsCancellationRequested ? "cancelled" : "unexpected-cancellation";
        Event(_outcome, error);
    }

    public void Failed(Exception error)
    {
        _outcome = "failed";
        Event("operation-failed", error);
    }

    public void Event(string name, Exception? error = null, int? rowCount = null)
    {
        if (!Logger.IsInfoEnabled) return;
        Logger.Info("[SSE diagnostic] " + JsonSerializer.Serialize(new
        {
            at = DateTimeOffset.UtcNow, operation = _operation, connectionId = _connectionId,
            requestId = _requestId, path = _path, @event = name, phase = Phase,
            elapsedMs = _elapsed.ElapsedMilliseconds, tokenCancelled = _token.IsCancellationRequested,
            requestAborted = _requestAborted.IsCancellationRequested, errorName = error?.GetType().Name,
            exceptionTokenMatches = error is OperationCanceledException cancelled ? (bool?)cancelled.CancellationToken.Equals(_token) : null,
            rowCount
        }));
    }

    public void Dispose()
    {
        _registration.Dispose();
        Event("operation-end:" + _outcome);
    }

    private static HttpContext? CurrentContext()
    {
        try { return App.HttpContext; }
        catch { return null; }
    }
}
