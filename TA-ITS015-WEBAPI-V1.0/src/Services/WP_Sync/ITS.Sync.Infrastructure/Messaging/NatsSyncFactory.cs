using System;
using System.IO;
using NATS.Client.Core;

namespace ITS.Sync.Infrastructure.Messaging;

/// <summary>
/// Dựng tham số kết nối NATS từ cấu hình "Nats" của WP_Sync.
/// Tách riêng để <see cref="NatsSyncListener"/> và <see cref="SyncHeartbeatPublisher"/> dùng chung cơ chế Auth.
/// </summary>
internal static class NatsSyncFactory
{
    /// <summary>Tham số kết nối; tự kết nối lại vô hạn khi NATS server khởi động lại.</summary>
    public static NatsOpts BuildOpts(NatsSyncOptions options, string? clientNameSuffix = null)
    {
        var name = string.IsNullOrWhiteSpace(options.ClientName) ? "ITS.Sync" : options.ClientName;
        if (!string.IsNullOrWhiteSpace(clientNameSuffix)) name = $"{name}_{clientNameSuffix}";

        return new NatsOpts
        {
            Url = string.IsNullOrWhiteSpace(options.Url) ? "nats://127.0.0.1:4222" : options.Url,
            Name = name,
            AuthOpts = BuildAuthOpts(options),
            TlsOpts = BuildTlsOpts(options),
            WebSocketOpts = BuildWebSocketOpts(options),
            MaxReconnectRetry = -1,
            ConnectTimeout = TimeSpan.FromSeconds(10)
        };
    }

    /// <summary>Cấu hình WebSocket cho ws:// / wss:// — bỏ kiểm tra chứng chỉ khi InsecureSkipVerify (giống TollNatsFactory).</summary>
    public static NatsWebSocketOpts? BuildWebSocketOpts(NatsSyncOptions options)
    {
        if (!options.InsecureSkipVerify) return null;

        return new NatsWebSocketOpts
        {
            ConfigureClientWebSocketOptions = (uri, clientWsOpts, ct) =>
            {
                clientWsOpts.RemoteCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                return ValueTask.CompletedTask;
            }
        };
    }

    /// <summary>Cấu hình TLS cho tls:// / wss://, hỗ trợ InsecureSkipVerify (giống TollNatsFactory).</summary>
    public static NatsTlsOpts BuildTlsOpts(NatsSyncOptions options)
    {
        return options.InsecureSkipVerify
            ? new NatsTlsOpts { InsecureSkipVerify = true }
            : NatsTlsOpts.Default;
    }

    /// <summary>Dựng thông tin xác thực theo AuthMode (hạ tầng thật dùng JWT + creds file).</summary>
    public static NatsAuthOpts BuildAuthOpts(NatsSyncOptions options)
    {
        var mode = options.AuthMode?.Trim().ToUpperInvariant() ?? "NONE";

        switch (mode)
        {
            case "JWT":
                if (!string.IsNullOrWhiteSpace(options.Jwt) && !string.IsNullOrWhiteSpace(options.Seed))
                    return new NatsAuthOpts { Jwt = options.Jwt!.Trim(), Seed = options.Seed!.Trim() };

                var creds = ResolvePath(options.CredsFile);
                if (string.IsNullOrWhiteSpace(creds) || !File.Exists(creds))
                    throw new FileNotFoundException("Không tìm thấy file creds cho NATS (Nats:CredsFile).", creds);

                return new NatsAuthOpts { CredsFile = creds };

            case "NKEY":
                if (string.IsNullOrWhiteSpace(options.Seed))
                    throw new InvalidOperationException("NKEY auth cần Nats:Seed.");
                return new NatsAuthOpts { NKey = options.NKey?.Trim(), Seed = options.Seed!.Trim() };

            case "TOKEN":
                if (string.IsNullOrWhiteSpace(options.Token))
                    throw new InvalidOperationException("TOKEN auth cần Nats:Token.");
                return new NatsAuthOpts { Token = options.Token!.Trim() };

            case "USERPASS":
                if (string.IsNullOrWhiteSpace(options.Username))
                    throw new InvalidOperationException("USERPASS auth cần Nats:Username.");
                return new NatsAuthOpts { Username = options.Username!.Trim(), Password = options.Password };

            default:
                return NatsAuthOpts.Default;
        }
    }

    /// <summary>Chuẩn hóa đường dẫn tương đối về tuyệt đối tính từ thư mục chạy ứng dụng.</summary>
    public static string? ResolvePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        return Path.IsPathRooted(path) ? path : Path.Combine(AppContext.BaseDirectory, path);
    }
}
