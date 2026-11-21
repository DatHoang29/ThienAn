using NATS.Client.Core;

namespace ITS.Barrier.Core.Messaging;

/// <summary>
/// Dựng tham số kết nối NATS từ cấu hình cho Barrier Control Service.
/// Tự kết nối lại vô hạn khi NATS server khởi động lại.
/// Author: ChuongLP
/// Date: 05/10/2026
/// </summary>
public static class BarrierNatsFactory
{
    /// <summary>Tham số kết nối; tự kết nối lại vô hạn khi NATS server khởi động lại.</summary>
    public static NatsOpts BuildOpts(BarrierControlNatsOption options, string? clientNameSuffix = null)
    {
        var name = string.IsNullOrWhiteSpace(options.ClientName) ? "ITS_BARRIER_SERVICE" : options.ClientName.Trim();
        if (!string.IsNullOrWhiteSpace(clientNameSuffix)) name = $"{name}_{clientNameSuffix}";

        return new NatsOpts
        {
            Url = string.IsNullOrWhiteSpace(options.Url) ? "nats://127.0.0.1:4222" : options.Url,
            Name = name,
            AuthOpts = BuildAuthOpts(options),
            MaxReconnectRetry = -1,
            ConnectTimeout = TimeSpan.FromSeconds(10),
        };
    }

    /// <summary>Kênh điều khiển, có giá trị mặc định khi cấu hình để trống.</summary>
    public static string ResolveSubject(BarrierControlNatsOption options)
        => string.IsNullOrWhiteSpace(options.ControlSubject) ? "ta.its.control.barrier" : options.ControlSubject.Trim();

    /// <summary>Dựng thông tin xác thực theo AuthMode (hạ tầng thật dùng JWT + creds file).</summary>
    public static NatsAuthOpts BuildAuthOpts(BarrierControlNatsOption options)
    {
        var mode = options.AuthMode?.Trim().ToUpperInvariant() ?? "NONE";

        switch (mode)
        {
            case "JWT":
                if (!string.IsNullOrWhiteSpace(options.Jwt) && !string.IsNullOrWhiteSpace(options.Seed))
                    return new NatsAuthOpts { Jwt = options.Jwt!.Trim(), Seed = options.Seed!.Trim() };

                var creds = ResolvePath(options.CredsFile);
                if (string.IsNullOrWhiteSpace(creds) || !File.Exists(creds))
                    throw new FileNotFoundException(
                        $"Không tìm thấy file creds NATS. Nats:CredsFile='{options.CredsFile}', " +
                        $"đường dẫn tra cứu thực tế='{creds}', thư mục chạy (BaseDirectory)='{AppContext.BaseDirectory}'. " +
                        "Hãy đặt file nats.creds đúng vào '<thư mục chứa .exe>\\Configuration\\nats.creds' " +
                        "(hoặc khai báo Nats:CredsFile là đường dẫn tuyệt đối).");

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
