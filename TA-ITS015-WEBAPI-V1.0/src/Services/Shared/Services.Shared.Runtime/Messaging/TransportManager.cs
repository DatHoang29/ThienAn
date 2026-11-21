using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Modules.DataTransporter.Natsio.Infrastructure.Configuration;
using Modules.DataTransporter.Natsio.Infrastructure.Handlers;

namespace Services.Shared.Messaging;

/// <summary>
/// Author: TAC Team
/// Description: Manager transport trừu tượng (hiện wrapper quanh NatsClient). Tên chung chung để
/// dễ thay transport (Kafka/MQTT...) sau này mà không đổi tên. Đọc config từ section "Nats" +
/// "Nats:Streams" trong DataTransporter.json (NatsRuntimeConfig + List&lt;NatsStreamConfig&gt;).
/// Created date: 16/07/2026
/// </summary>
public sealed class TransportManager : IAsyncDisposable
{
    private readonly NatsClient _client;
    private readonly NatsRuntimeConfig? _config;
    private readonly List<NatsStreamConfig>? _streams;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private readonly HashSet<string> _subscriptions = new(StringComparer.Ordinal);
    private readonly object _subscriptionLock = new();
    private bool _initialized;
    private bool _disposed;

    public TransportManager(IConfiguration configuration)
    {
        // Đọc config từ DataTransporter.json: section "Nats" (NatsRuntimeConfig) + "Nats:Streams".
        _config = configuration.GetSection("Nats").Get<NatsRuntimeConfig>();
        _streams = configuration.GetSection("Nats:Streams").Get<List<NatsStreamConfig>>();

        _client = new NatsClient();
    }

    public bool IsConnected => !_disposed && _client.IsConnected;

    /// <summary>Kết nối NATS + đăng ký streams/subjects theo config. Idempotent.</summary>
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_config is null || string.IsNullOrWhiteSpace(_config.Url))
            return; // NATS chưa cấu hình — bỏ qua.

        if (_initialized)
            return;

        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            if (_initialized)
                return;

            cancellationToken.ThrowIfCancellationRequested();
            await _client.InitializeAsync(_config, _streams);
            _initialized = true;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public Task SubscribeAsync(
        string subject,
        Func<string, Task> handler,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Subject is required.", nameof(subject));

        lock (_subscriptionLock)
        {
            if (_subscriptions.Add(subject))
                _client.Subscribe(subject, handler);
        }

        return Task.CompletedTask;
    }

    public async Task<bool> PublishAsync(
        string subject,
        object data,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();

        if (!IsConnected)
            throw new InvalidOperationException("Transport is not connected.");

        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Subject is required.", nameof(subject));

        return await _client.PublishAsync(subject, data);
    }

    public void Unsubscribe(string subject)
    {
        if (_disposed || string.IsNullOrWhiteSpace(subject))
            return;

        lock (_subscriptionLock)
        {
            if (!_subscriptions.Remove(subject))
                return;

            _client.Unsubscribe(subject);
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            return;

        cancellationToken.ThrowIfCancellationRequested();

        string[] subjects;
        lock (_subscriptionLock)
            subjects = _subscriptions.ToArray();

        foreach (var subject in subjects)
            Unsubscribe(subject);

        await _client.DisconnectAsync();
        _initialized = false;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        await DisconnectAsync();
        _disposed = true;
        _connectionLock.Dispose();
    }
}
