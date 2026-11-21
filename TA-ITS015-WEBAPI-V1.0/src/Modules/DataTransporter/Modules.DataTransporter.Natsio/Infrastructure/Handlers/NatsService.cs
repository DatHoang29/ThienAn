using System.Diagnostics;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using NATS.Net;
using Modules.DataTransporter.Natsio.Infrastructure.Configuration;
using Modules.DataTransporter.Natsio.Infrastructure.Security;

namespace Modules.DataTransporter.Natsio.Infrastructure.Handlers;

public class NatsService : IAsyncDisposable
{
    private static NatsService? _instance;
    public static NatsService Instance => _instance ??= new NatsService();

    private NatsConnection? _connection;
    private INatsJSContext? _js;
    private NatsRuntimeConfig? _lastConfig;
    private int _retryIntervalSeconds = 5;

    private readonly Dictionary<string, CancellationTokenSource> _subscriptions = new();
    private readonly Dictionary<string, CancellationTokenSource> _consumers = new();

    // Pending registrations for auto-reconnect
    private readonly List<(string subject, Delegate handler)> _pendingSubscribes = new();
    private readonly List<(string stream, string consumerName, string? filterSubject, Delegate handler)> _pendingConsumers = new();

    public bool IsConnected => _connection != null && _connection.ConnectionState == NatsConnectionState.Open;
    public string? CurrentUrl { get; private set; }
    public string? CurrentNKey { get; private set; }
    public INatsJSContext? JS => _js;

    private NatsService() { }

    #region Connection

    public async Task ConnectAsync(NatsRuntimeConfig config)
    {
        _lastConfig = config;
        _retryIntervalSeconds = config.RetryIntervalSeconds > 0 ? config.RetryIntervalSeconds : 5;
        if (IsConnected) return;

        var rawUrl = FirstNotEmpty(config.Url, "nats://127.0.0.1:4222");
        if (rawUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            rawUrl = "tls://" + rawUrl["https://".Length..];
            config.Url = rawUrl;
        }
        else if (rawUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            rawUrl = "nats://" + rawUrl["http://".Length..];
            config.Url = rawUrl;
        }

        var url = rawUrl;
        var clientName = FirstNotEmpty(config.ClientName, "WebApi");

        var tlsOpts = NatsAuthFactory.BuildTlsOpts(config);
        var authOpts = NatsAuthFactory.BuildAuthOpts(config, out var activeNKey);
        CurrentNKey = activeNKey;

        var opts = new NatsOpts
        {
            Url = url,
            Name = clientName,
            AuthOpts = authOpts,
            TlsOpts = tlsOpts,
            WebSocketOpts = new NatsWebSocketOpts
            {
                ConfigureClientWebSocketOptions = (uri, clientWsOpts, ct) =>
                {
                    if (config.InsecureSkipVerify)
                    {
                        clientWsOpts.RemoteCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                    }
                    return ValueTask.CompletedTask;
                }
            },
            MaxReconnectRetry = -1,
            ReconnectWaitMax = TimeSpan.FromSeconds(2),
            ConnectTimeout = TimeSpan.FromSeconds(5),
            CommandTimeout = TimeSpan.FromSeconds(5)
        };

        _connection = new NatsConnection(opts);

        try
        {
            Debug.WriteLine($"[Natsio] Connecting to {url}...");
            await _connection.ConnectAsync();
            CurrentUrl = url;
            _js = _connection.CreateJetStreamContext();
            Debug.WriteLine($"[Natsio] Connected. IsConnected={IsConnected}");
            ReplayPendingRegistrations();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Natsio] Connect failed: {ex.GetType().Name}: {ex.Message}. Auto-retry in background.");
            _js = null;
            _ = RetryConnectLoopAsync();
        }
    }

    private async Task RetryConnectLoopAsync()
    {
        while (!IsConnected && _connection != null)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_retryIntervalSeconds));
                Debug.WriteLine("[Natsio] Retrying connection...");
                await _connection.ConnectAsync();
                CurrentUrl = FirstNotEmpty(_lastConfig?.Url, "nats://127.0.0.1:4222");
                _js = _connection.CreateJetStreamContext();
                Debug.WriteLine($"[Natsio] Reconnected. IsConnected={IsConnected}");
                ReplayPendingRegistrations();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Natsio] Retry failed: {ex.Message}. Next retry in {_retryIntervalSeconds}s.");
            }
        }
    }

    private void ReplayPendingRegistrations()
    {
        if (!IsConnected) return;

        foreach (var (subject, handler) in _pendingSubscribes.ToArray())
        {
            if (handler is Action<string> syncHandler)
                StartSubscribeLoop(subject, syncHandler);
            else if (handler is Func<string, Task> asyncHandler)
                StartSubscribeLoop(subject, asyncHandler);
        }

        foreach (var (stream, consumerName, filterSubject, handler) in _pendingConsumers.ToArray())
        {
            if (handler is Action<string> syncHandler)
                StartConsumeLoop(stream, consumerName, filterSubject, syncHandler);
            else if (handler is Func<string, Task> asyncHandler)
                StartConsumeLoop(stream, consumerName, filterSubject, asyncHandler);
        }
    }

    public async Task DisconnectAsync()
    {
        CancelAllSubscriptions();
        await SafeDisposeConnectionAsync();
    }

    public async ValueTask DisposeAsync()
    {
        CancelAllSubscriptions();
        await SafeDisposeConnectionAsync();
    }

    #endregion

    #region Publish

    public async Task<bool> PublishAsync<T>(string subject, T data)
    {
        if (!IsConnected) return false;
        try
        {
            await _connection!.PublishAsync(subject, data);
            return true;
        }
        catch (Exception ex)
        {
            LogError("Publish", subject, ex);
            return false;
        }
    }

    #endregion

    #region Subscribe

    public IAsyncEnumerable<NatsMsg<T>> SubscribeAsync<T>(string subject)
    {
        if (!IsConnected) throw new InvalidOperationException("NATS is not connected.");
        return _connection!.SubscribeAsync<T>(subject);
    }

    public void Subscribe<T>(string subject, Action<T> handler)
    {
        _pendingSubscribes.RemoveAll(x => x.subject == subject);
        _pendingSubscribes.Add((subject, handler));

        if (!IsConnected) return;
        CancelExisting(subject, _subscriptions);
        StartSubscribeLoop(subject, handler);
    }

    public void Subscribe<T>(string subject, Func<T, Task> handler)
    {
        _pendingSubscribes.RemoveAll(x => x.subject == subject);
        _pendingSubscribes.Add((subject, handler));

        if (!IsConnected) return;
        CancelExisting(subject, _subscriptions);
        StartSubscribeLoop(subject, handler);
    }

    private void StartSubscribeLoop<T>(string subject, Action<T> handler)
    {
        var cts = new CancellationTokenSource();
        _subscriptions[subject] = cts;

        _ = Task.Run(async () =>
        {
            try
            {
                await foreach (var msg in _connection!.SubscribeAsync<T>(subject).WithCancellation(cts.Token))
                {
                    if (msg.Data != null)
                        handler(msg.Data);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                LogError("Subscribe", subject, ex);
            }
        }, cts.Token);
    }

    private void StartSubscribeLoop<T>(string subject, Func<T, Task> handler)
    {
        var cts = new CancellationTokenSource();
        _subscriptions[subject] = cts;

        _ = Task.Run(async () =>
        {
            try
            {
                await foreach (var msg in _connection!.SubscribeAsync<T>(subject).WithCancellation(cts.Token))
                {
                    if (msg.Data != null)
                        await handler(msg.Data);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                LogError("Subscribe", subject, ex);
            }
        }, cts.Token);
    }

    public void Unsubscribe(string subject)
    {
        _pendingSubscribes.RemoveAll(x => x.subject == subject);
        CancelExisting(subject, _subscriptions);
    }

    #endregion

    #region JetStream

    #region JetStream Publish

    public async Task<bool> JsPublishAsync<T>(string subject, T data)
    {
        if (!IsConnected || _js == null) return false;
        try
        {
            await _js.PublishAsync(subject, data);
            return true;
        }
        catch (Exception ex)
        {
            LogError("JS Publish", subject, ex);
            return false;
        }
    }

    #endregion

    #region JetStream Stream Management

    /// <summary>
    /// Tạo hoặc cập nhật stream. Trả về tên stream thực tế (có thể là stream đã tồn tại).
    /// </summary>
    public async Task<string?> CreateStreamAsync(string streamName, string[] subjects,
        string storage = "file",
        TimeSpan? maxAge = null)
    {
        if (!IsConnected || _js == null) return null;
        var config = new StreamConfig(streamName, subjects)
        {
            Storage = storage.Equals("memory", StringComparison.OrdinalIgnoreCase)
                ? StreamConfigStorage.Memory
                : StreamConfigStorage.File,
            Retention = StreamConfigRetention.Limits,
        };
        if (maxAge.HasValue)
            config.MaxAge = maxAge.Value;

        try
        {
            await _js!.CreateOrUpdateStreamAsync(config);
            Debug.WriteLine($"[Natsio] Stream '{streamName}' created/updated OK");
            return streamName;
        }
        catch (NatsJSApiException ex) when (IsStreamConflictError(ex))
        {
            // Tìm stream hiện có chứa subject này
            var existing = await FindStreamForSubjectAsync(subjects.FirstOrDefault() ?? "");
            if (existing != null)
            {
                Debug.WriteLine($"[Natsio] Using existing stream '{existing}' instead of '{streamName}'. ({ex.Error.Description})");
                return existing;
            }
            Debug.WriteLine($"[Natsio] Stream '{streamName}' conflict but no existing stream found. ({ex.Error.Description})");
            return null;
        }
        catch (Exception ex)
        {
            LogError("CreateStream", streamName, ex);
            return null;
        }
    }

    /// <summary>
    /// Tìm stream hiện có chứa subject given.
    /// </summary>
    public async Task<string?> FindStreamForSubjectAsync(string subject)
    {
        if (!IsConnected || _js == null || string.IsNullOrWhiteSpace(subject)) return null;
        try
        {
            await foreach (var stream in _js.ListStreamsAsync())
            {
                if (stream.Info.Config.Subjects != null &&
                    stream.Info.Config.Subjects.Any(s => SubjectMatches(s, subject)))
                {
                    return stream.Info.Config.Name;
                }
            }
        }
        catch { }
        return null;
    }

    private static bool SubjectMatches(string pattern, string subject)
    {
        if (pattern == subject) return true;
        if (pattern.EndsWith(".>"))
            return subject.StartsWith(pattern[..^2]) && subject.Length > pattern[..^2].Length;
        if (pattern.EndsWith(".*"))
            return subject.StartsWith(pattern[..^2]) && !subject[pattern[..^2].Length..].Contains('.');
        return false;
    }

    private static bool IsStreamConflictError(NatsJSApiException ex)
    {
        // 10054: storage type mismatch, 10148: subject overlap, various stream config conflicts
        return ex.Error.Code == 10054 || ex.Error.Code == 10148
            || (ex.Error.Description?.Contains("storage type") == true)
            || (ex.Error.Description?.Contains("overlap") == true)
            || (ex.Error.Description?.Contains("already in use") == true);
    }

    #endregion

    #region JetStream Consume

    public async IAsyncEnumerable<string> ConsumeFromStreamAsync(
        string stream, string consumerName,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken token)
    {
        if (!IsConnected || _js == null) yield break;

        var consumer = await _js!.CreateOrUpdateConsumerAsync(stream, new ConsumerConfig(consumerName)
        {
            AckPolicy = ConsumerConfigAckPolicy.Explicit
        });

        await foreach (var msg in consumer.ConsumeAsync<string>().WithCancellation(token))
        {
            await msg.AckAsync();
            yield return $"[{msg.Subject}] {msg.Data}";
        }
    }

    public void ConsumeStream<T>(string stream, string consumerName, Action<T> handler, string? filterSubject = null)
    {
        _pendingConsumers.RemoveAll(x => x.consumerName == consumerName);
        _pendingConsumers.Add((stream, consumerName, filterSubject, handler));

        if (!IsConnected || _js == null) return;
        CancelExisting(consumerName, _consumers);
        StartConsumeLoop(stream, consumerName, filterSubject, handler);
    }

    public void ConsumeStream<T>(string stream, string consumerName, Func<T, Task> handler, string? filterSubject = null)
    {
        _pendingConsumers.RemoveAll(x => x.consumerName == consumerName);
        _pendingConsumers.Add((stream, consumerName, filterSubject, handler));

        if (!IsConnected || _js == null) return;
        CancelExisting(consumerName, _consumers);
        StartConsumeLoop(stream, consumerName, filterSubject, handler);
    }

    private void StartConsumeLoop<T>(string stream, string consumerName, string? filterSubject, Action<T> handler)
    {
        var cts = new CancellationTokenSource();
        _consumers[consumerName] = cts;

        _ = Task.Run(async () =>
        {
            try
            {
                var cfg = new ConsumerConfig(consumerName)
                {
                    AckPolicy = ConsumerConfigAckPolicy.Explicit,
                    DeliverPolicy = ConsumerConfigDeliverPolicy.New,
                };
                if (!string.IsNullOrWhiteSpace(filterSubject))
                    cfg.FilterSubject = filterSubject;

                Debug.WriteLine($"[Natsio] Creating consumer '{consumerName}' on stream '{stream}', filter='{filterSubject}'");
                INatsJSConsumer consumer;
                try
                {
                    consumer = await _js!.CreateOrUpdateConsumerAsync(stream, cfg);
                }
                catch (NatsJSApiException ex) when (ex.Error.Code == 10148)
                {
                    // Consumer exists with different config → delete and recreate
                    Debug.WriteLine($"[Natsio] Consumer '{consumerName}' conflict, deleting and recreating...");
                    await _js.DeleteConsumerAsync(stream, consumerName);
                    consumer = await _js.CreateConsumerAsync(stream, cfg);
                }
                Debug.WriteLine($"[Natsio] Consumer '{consumerName}' ready. Waiting for messages...");

                await foreach (var msg in consumer.ConsumeAsync<T>().WithCancellation(cts.Token))
                {
                    await msg.AckAsync();
                    Debug.WriteLine($"[Natsio] ConsumeStream '{consumerName}' received on '{msg.Subject}'");
                    if (msg.Data != null)
                        handler(msg.Data);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                LogError("ConsumeStream", consumerName, ex);
            }
        }, cts.Token);
    }

    private void StartConsumeLoop<T>(string stream, string consumerName, string? filterSubject, Func<T, Task> handler)
    {
        var cts = new CancellationTokenSource();
        _consumers[consumerName] = cts;

        _ = Task.Run(async () =>
        {
            try
            {
                var cfg = new ConsumerConfig(consumerName)
                {
                    AckPolicy = ConsumerConfigAckPolicy.Explicit,
                    DeliverPolicy = ConsumerConfigDeliverPolicy.New,
                };
                if (!string.IsNullOrWhiteSpace(filterSubject))
                    cfg.FilterSubject = filterSubject;

                Debug.WriteLine($"[Natsio] Creating consumer '{consumerName}' on stream '{stream}', filter='{filterSubject}'");
                INatsJSConsumer consumer;
                try
                {
                    consumer = await _js!.CreateOrUpdateConsumerAsync(stream, cfg);
                }
                catch (NatsJSApiException ex) when (ex.Error.Code == 10148)
                {
                    Debug.WriteLine($"[Natsio] Consumer '{consumerName}' conflict, deleting and recreating...");
                    await _js.DeleteConsumerAsync(stream, consumerName);
                    consumer = await _js.CreateConsumerAsync(stream, cfg);
                }
                Debug.WriteLine($"[Natsio] Consumer '{consumerName}' ready. Waiting for messages...");

                await foreach (var msg in consumer.ConsumeAsync<T>().WithCancellation(cts.Token))
                {
                    await msg.AckAsync();
                    Debug.WriteLine($"[Natsio] ConsumeStream '{consumerName}' received on '{msg.Subject}'");
                    if (msg.Data != null)
                        await handler(msg.Data);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                LogError("ConsumeStream", consumerName, ex);
            }
        }, cts.Token);
    }

    public void StopConsume(string consumerName)
    {
        _pendingConsumers.RemoveAll(x => x.consumerName == consumerName);
        CancelExisting(consumerName, _consumers);
    }

    #endregion
    #endregion

    #region Private Helpers

    private static void LogError(string operation, string target, Exception ex)
    {
        Debug.WriteLine($"[Natsio] {operation} error on '{target}': {ex.Message}");
    }

    private static void CancelExisting(string key, Dictionary<string, CancellationTokenSource> dict)
    {
        if (dict.TryGetValue(key, out var existing))
        {
            existing.Cancel();
            existing.Dispose();
            dict.Remove(key);
        }
    }

    private void CancelAllSubscriptions()
    {
        foreach (var kv in _subscriptions)
        {
            kv.Value.Cancel();
            kv.Value.Dispose();
        }
        _subscriptions.Clear();

        foreach (var kv in _consumers)
        {
            kv.Value.Cancel();
            kv.Value.Dispose();
        }
        _consumers.Clear();
    }

    private async Task SafeDisposeConnectionAsync()
    {
        if (_connection != null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
        _js = null;
        CurrentUrl = null;
        CurrentNKey = null;
    }

    private static string FirstNotEmpty(params string?[] values)
    {
        foreach (var value in values)
            if (!string.IsNullOrWhiteSpace(value)) return value.Trim();
        return string.Empty;
    }

    #endregion
}
