using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ITS.Sync.Core.Abstractions;
using ITS.Sync.Core.Models;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;

namespace ITS.Sync.Infrastructure.Messaging;

/// <summary>
/// Lắng nghe tín hiệu NATS và kích hoạt đồng bộ MỘT bảng lẻ (không theo định kỳ).
///
/// Ý tưởng: tái sử dụng luôn các subject nghiệp vụ đang có. Ví dụ khi FE cập nhật sự cố,
/// hệ thống đã bắn "ta.its.data.incident" để buộc FE fetch lại; ta subscribe cùng subject đó
/// và map sang bảng "TmsIncident" để đồng bộ ngay — bên publish KHÔNG phải đổi gì.
///
/// Mỗi subject cấu hình riêng chế độ:
/// - "jetstream": consumer BỀN, tín hiệu phát lúc service tắt vẫn được xử lý khi chạy lại.
/// - "pubsub": subscribe thường, tín hiệu phát khi service tắt sẽ bị mất.
///
/// Tên bảng lấy theo thứ tự: Table trong cấu hình → payload JSON ("table"/"tableName",
/// tự bóc envelope Data/Result) → token cuối của subject.
///
/// Không ném exception ra ngoài — mọi lỗi đều ghi log và tự kết nối lại.
/// </summary>
public sealed class NatsSyncListener : IAsyncDisposable
{
    private readonly NatsSyncOptions _options;
    private readonly ISyncController _controller;
    private readonly ILogger<NatsSyncListener> _logger;

    private CancellationTokenSource? _cts;
    private Task? _loop;

    /// <summary>Thời điểm bắt đầu lần đồng bộ gần nhất theo bảng — dùng để gộp tín hiệu dồn dập.</summary>
    private readonly ConcurrentDictionary<string, DateTime> _lastRunAt = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>True khi đã kết nối được NATS và đang lắng nghe.</summary>
    public bool IsConnected { get; private set; }

    /// <summary>Mô tả trạng thái kết nối gần nhất (để host hiển thị).</summary>
    public string Status { get; private set; } = "Chưa khởi động";

    public NatsSyncListener(
        NatsSyncOptions options,
        ISyncController controller,
        ILogger<NatsSyncListener> logger)
    {
        _options = options;
        _controller = controller;
        _logger = logger;
    }

    public bool IsListening => _loop is { IsCompleted: false };

    /// <summary>
    /// Bắt đầu lắng nghe tất cả subject đã cấu hình. Báo cáo/lỗi mỗi lần đồng bộ đi qua
    /// <see cref="IProgress{T}"/> giống vòng lặp định kỳ, để host tự hiển thị/ghi log.
    /// </summary>
    public void Start(
        IProgress<SyncRunReport>? onCycle = null,
        IProgress<Exception>? onError = null,
        IProgress<string>? onStatus = null)
    {
        if (!_options.Enabled)
        {
            SetStatus("Đã tắt (Nats:Enabled = false)", false, onStatus);
            _logger.LogInformation("NATS listener đang TẮT (Nats:Enabled = false).");
            return;
        }

        var subs = _options.Subscriptions
            .Where(s => !string.IsNullOrWhiteSpace(s.Subject))
            .ToList();

        if (subs.Count == 0)
        {
            SetStatus("Lỗi cấu hình: Nats:Subscriptions rỗng", false, onStatus);
            _logger.LogWarning("Nats:Subscriptions rỗng — không có subject nào để lắng nghe.");
            return;
        }

        Stop();

        var cts = new CancellationTokenSource();
        _cts = cts;
        _loop = Task.Run(() => ConnectLoopAsync(subs, cts.Token, onCycle, onError, onStatus), CancellationToken.None);
    }

    /// <summary>Cập nhật trạng thái và báo cho host (nếu có).</summary>
    private void SetStatus(string status, bool connected, IProgress<string>? onStatus)
    {
        Status = status;
        IsConnected = connected;
        onStatus?.Report(status);
    }

    /// <summary>Dừng lắng nghe.</summary>
    public void Stop()
    {
        try { _cts?.Cancel(); } catch (ObjectDisposedException) { }
        _cts?.Dispose();
        _cts = null;
        _loop = null;
        IsConnected = false;
    }

    /// <summary>Giữ kết nối; mỗi subject chạy một vòng lặp đọc riêng trên cùng connection.</summary>
    private async Task ConnectLoopAsync(
        List<NatsSyncSubscription> subs,
        CancellationToken token,
        IProgress<SyncRunReport>? onCycle,
        IProgress<Exception>? onError,
        IProgress<string>? onStatus)
    {
        var retry = TimeSpan.FromSeconds(_options.RetryIntervalSeconds > 0 ? _options.RetryIntervalSeconds : 5);

        while (!token.IsCancellationRequested)
        {
            try
            {
                SetStatus($"Đang kết nối {_options.Url}...", false, onStatus);
                _logger.LogInformation("NATS: đang kết nối {Url}...", _options.Url);

                await using var conn = new NatsConnection(BuildOpts());
                await conn.ConnectAsync().ConfigureAwait(false);

                var subjectList = string.Join(", ", subs.Select(s =>
                    string.IsNullOrWhiteSpace(s.Table) ? s.Subject : $"{s.Subject} -> {s.Table}"));

                SetStatus($"Đã kết nối {_options.Url} | {subs.Count} subject: {subjectList}", true, onStatus);
                _logger.LogInformation("NATS ĐÃ KẾT NỐI {Url}. Lắng nghe {Count} subject: {Subjects}",
                    _options.Url, subs.Count, subjectList);

                var tasks = subs
                    .Select(s => ListenSubjectAsync(conn, s, onCycle, onError, onStatus, token))
                    .ToArray();

                // Nếu một luồng lỗi/đứt, thoát ra để tạo lại connection cho tất cả.
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                SetStatus($"LỖI: {ex.Message} (thử lại sau {retry.TotalSeconds:0}s)", false, onStatus);
                _logger.LogError(ex, "NATS KẾT NỐI THẤT BẠI: {Message}. Thử lại sau {Seconds}s.",
                    ex.Message, retry.TotalSeconds);
            }

            if (token.IsCancellationRequested) break;

            try { await Task.Delay(retry, token).ConfigureAwait(false); }
            catch (OperationCanceledException) { break; }
        }

        SetStatus("Đã dừng", false, onStatus);
        _logger.LogInformation("NATS listener đã dừng.");
    }

    private Task ListenSubjectAsync(
        NatsConnection conn,
        NatsSyncSubscription sub,
        IProgress<SyncRunReport>? onCycle,
        IProgress<Exception>? onError,
        IProgress<string>? onStatus,
        CancellationToken token)
    {
        return string.Equals(sub.Mode?.Trim(), "jetstream", StringComparison.OrdinalIgnoreCase)
            ? ConsumeJetStreamAsync(conn, sub, onCycle, onError, onStatus, token)
            : SubscribeCoreAsync(conn, sub, onCycle, onError, onStatus, token);
    }

    /// <summary>Subscribe thường (pubsub).</summary>
    private async Task SubscribeCoreAsync(
        NatsConnection conn,
        NatsSyncSubscription sub,
        IProgress<SyncRunReport>? onCycle,
        IProgress<Exception>? onError,
        IProgress<string>? onStatus,
        CancellationToken token)
    {
        var stream = string.IsNullOrWhiteSpace(sub.QueueGroup)
            ? conn.SubscribeAsync<string>(sub.Subject, cancellationToken: token)
            : conn.SubscribeAsync<string>(sub.Subject, queueGroup: sub.QueueGroup, cancellationToken: token);

        await foreach (var msg in stream.ConfigureAwait(false))
        {
            if (token.IsCancellationRequested) break;
            await HandleMessageAsync(sub, msg.Subject, msg.Data, onCycle, onError, onStatus, token)
                .ConfigureAwait(false);
        }
    }

    /// <summary>JetStream durable consumer — tín hiệu không bị mất khi service tắt.</summary>
    private async Task ConsumeJetStreamAsync(
        NatsConnection conn,
        NatsSyncSubscription sub,
        IProgress<SyncRunReport>? onCycle,
        IProgress<Exception>? onError,
        IProgress<string>? onStatus,
        CancellationToken token)
    {
        var js = new NatsJSContext(conn);

        var streamName = sub.StreamName;
        if (string.IsNullOrWhiteSpace(streamName))
        {
            await foreach (var name in js.ListStreamNamesAsync(sub.Subject, token).ConfigureAwait(false))
            {
                streamName = name;
                break;
            }
        }

        if (string.IsNullOrWhiteSpace(streamName))
            throw new InvalidOperationException(
                $"Không tìm thấy JetStream stream cho subject '{sub.Subject}'. " +
                "Khai báo Nats:Subscriptions[].StreamName hoặc kiểm tra lại subject.");

        var durable = string.IsNullOrWhiteSpace(sub.DurableName)
            ? $"its-sync-{sub.Subject.Replace('.', '-').Replace('>', 'x').Replace('*', 'x')}"
            : sub.DurableName!;

        var consumer = await js.CreateOrUpdateConsumerAsync(streamName, new ConsumerConfig
        {
            DurableName = durable,
            FilterSubject = sub.Subject,
            AckPolicy = ConsumerConfigAckPolicy.Explicit
        }, token).ConfigureAwait(false);

        _logger.LogInformation("JetStream consumer '{Durable}' trên stream '{Stream}' (subject '{Subject}').",
            durable, streamName, sub.Subject);

        await foreach (var msg in consumer.ConsumeAsync<string>(cancellationToken: token).ConfigureAwait(false))
        {
            if (token.IsCancellationRequested) break;

            try
            {
                await HandleMessageAsync(sub, msg.Subject, msg.Data, onCycle, onError, onStatus, token)
                    .ConfigureAwait(false);
            }
            finally
            {
                // Ack kể cả khi đồng bộ lỗi (lỗi đã báo qua onError/log),
                // tránh để NATS gửi lại vô hạn.
                await msg.AckAsync(cancellationToken: token).ConfigureAwait(false);
            }
        }
    }

    /// <summary>Xác định bảng cần đồng bộ rồi chạy, có gộp tín hiệu dồn dập (debounce).</summary>
    private async Task HandleMessageAsync(
        NatsSyncSubscription sub,
        string subject,
        string? payload,
        IProgress<SyncRunReport>? onCycle,
        IProgress<Exception>? onError,
        IProgress<string>? onStatus,
        CancellationToken token)
    {
        string? table;
        try
        {
            table = !string.IsNullOrWhiteSpace(sub.Table)
                ? sub.Table!.Trim()
                : ResolveTableName(subject, payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Không đọc được tín hiệu NATS (subject '{Subject}').", subject);
            return;
        }

        if (string.IsNullOrWhiteSpace(table))
        {
            var warn = $"Tín hiệu '{subject}' không xác định được tên bảng — bỏ qua.";
            _logger.LogWarning(warn);
            onStatus?.Report(warn);
            return;
        }

        if (_options.AllowedTables.Count > 0 &&
            !_options.AllowedTables.Any(t => string.Equals(t, table, StringComparison.OrdinalIgnoreCase)))
        {
            var warn = $"Bảng '{table}' không nằm trong Nats:AllowedTables — bỏ qua.";
            _logger.LogWarning(warn);
            onStatus?.Report(warn);
            return;
        }

        // Gộp tín hiệu dồn dập: bỏ qua nếu bảng này vừa được đồng bộ trong khoảng debounce.
        if (sub.DebounceSeconds > 0
            && _lastRunAt.TryGetValue(table, out var last)
            && (DateTime.UtcNow - last) < TimeSpan.FromSeconds(sub.DebounceSeconds))
        {
            _logger.LogDebug("Bỏ qua tín hiệu '{Subject}' — bảng '{Table}' vừa đồng bộ xong.", subject, table);
            return;
        }

        _lastRunAt[table] = DateTime.UtcNow;

        var msg = $"Tín hiệu '{subject}' → đồng bộ bảng '{table}'.";
        _logger.LogInformation("Tín hiệu '{Subject}' → đồng bộ bảng '{Table}'.", subject, table);
        // Báo cả lên host (WPF hiển thị vào Nhật ký chung) để thấy được trên UI, không chỉ ở log.
        onStatus?.Report($"Tín hiệu '{subject}' → đồng bộ bảng '{table}'");
        onStatus?.Report(msg);

        await _controller.RunTableOnceAsync(table, onCycle, onError, token).ConfigureAwait(false);
    }

    private NatsOpts BuildOpts()
    {
        return new NatsOpts
        {
            Url = string.IsNullOrWhiteSpace(_options.Url) ? "nats://127.0.0.1:4222" : _options.Url,
            Name = string.IsNullOrWhiteSpace(_options.ClientName) ? "ITS.Sync" : _options.ClientName,
            AuthOpts = BuildAuthOpts(),
            MaxReconnectRetry = -1,
            ConnectTimeout = TimeSpan.FromSeconds(10)
        };
    }

    /// <summary>Dựng thông tin xác thực theo AuthMode (hạ tầng thật dùng JWT + creds file).</summary>
    private NatsAuthOpts BuildAuthOpts()
    {
        var mode = _options.AuthMode?.Trim().ToUpperInvariant() ?? "NONE";

        switch (mode)
        {
            case "JWT":
                if (!string.IsNullOrWhiteSpace(_options.Jwt) && !string.IsNullOrWhiteSpace(_options.Seed))
                    return new NatsAuthOpts { Jwt = _options.Jwt!.Trim(), Seed = _options.Seed!.Trim() };

                var creds = ResolvePath(_options.CredsFile);
                if (string.IsNullOrWhiteSpace(creds) || !File.Exists(creds))
                    throw new FileNotFoundException("Không tìm thấy file creds cho NATS (Nats:CredsFile).", creds);

                return new NatsAuthOpts { CredsFile = creds };

            case "NKEY":
                if (string.IsNullOrWhiteSpace(_options.Seed))
                    throw new InvalidOperationException("NKEY auth cần Nats:Seed.");
                return new NatsAuthOpts { NKey = _options.NKey?.Trim(), Seed = _options.Seed!.Trim() };

            case "TOKEN":
                if (string.IsNullOrWhiteSpace(_options.Token))
                    throw new InvalidOperationException("TOKEN auth cần Nats:Token.");
                return new NatsAuthOpts { Token = _options.Token!.Trim() };

            case "USERPASS":
                if (string.IsNullOrWhiteSpace(_options.Username))
                    throw new InvalidOperationException("USERPASS auth cần Nats:Username.");
                return new NatsAuthOpts { Username = _options.Username!.Trim(), Password = _options.Password };

            default:
                return NatsAuthOpts.Default;
        }
    }

    private static string? ResolvePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        return Path.IsPathRooted(path) ? path : Path.Combine(AppContext.BaseDirectory, path);
    }

    /// <summary>
    /// Rút tên bảng từ payload hoặc subject. Tự bóc envelope nhiều lớp
    /// (Data/data/BroadcastObject/Result/...) theo style broadcaster của hệ thống.
    /// </summary>
    internal static string? ResolveTableName(string subject, string? payload)
    {
        if (!string.IsNullOrWhiteSpace(payload))
        {
            var text = payload.Trim();

            if (text.StartsWith('{') || text.StartsWith('['))
            {
                using var doc = JsonDocument.Parse(text);
                var found = FindTableProperty(doc.RootElement, depth: 0);
                if (!string.IsNullOrWhiteSpace(found)) return found;
            }
            else
            {
                var plain = text.Trim('"');
                if (!string.IsNullOrWhiteSpace(plain)) return plain;
            }
        }

        return subject?.Split('.', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
    }

    /// <summary>Tìm đệ quy thuộc tính "table"/"tableName" trong JSON (giới hạn độ sâu để an toàn).</summary>
    private static string? FindTableProperty(JsonElement element, int depth)
    {
        if (depth > 6) return null;

        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var prop in element.EnumerateObject())
                {
                    if ((prop.NameEquals("table") || prop.NameEquals("tableName"))
                        && prop.Value.ValueKind == JsonValueKind.String)
                    {
                        var value = prop.Value.GetString();
                        if (!string.IsNullOrWhiteSpace(value)) return value.Trim();
                    }
                }

                foreach (var prop in element.EnumerateObject())
                {
                    var nested = FindTableProperty(prop.Value, depth + 1);
                    if (!string.IsNullOrWhiteSpace(nested)) return nested;
                }
                return null;

            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    var nested = FindTableProperty(item, depth + 1);
                    if (!string.IsNullOrWhiteSpace(nested)) return nested;
                }
                return null;

            default:
                return null;
        }
    }

    public ValueTask DisposeAsync()
    {
        Stop();
        return ValueTask.CompletedTask;
    }
}
