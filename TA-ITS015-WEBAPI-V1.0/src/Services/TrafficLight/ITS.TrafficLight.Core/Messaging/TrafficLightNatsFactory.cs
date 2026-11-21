using NATS.Client.Core;

namespace ITS.TrafficLight.Core.Messaging;

/// <summary>
/// Dựng tham số kết nối NATS từ cấu hình cho Traffic Light Control Service.
/// Author: ChuongLP
/// Date: 05/10/2026
/// </summary>
public static class TrafficLightNatsFactory
{
    public static NatsOpts BuildOpts(TrafficLightControlNatsOption options, string? clientNameSuffix = null)
    {
        var name = string.IsNullOrWhiteSpace(options.ClientName) ? "ITS_TRAFFICLIGHT_SERVICE" : options.ClientName.Trim();
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

    public static string ResolveSubject(TrafficLightControlNatsOption options)
        => string.IsNullOrWhiteSpace(options.ControlSubject) ? "ta.its.control.trafficlight" : options.ControlSubject.Trim();

    public static NatsAuthOpts BuildAuthOpts(TrafficLightControlNatsOption options)
    {
        var mode = options.AuthMode?.Trim().ToUpperInvariant() ?? "NONE";

        switch (mode)
        {
            case "JWT":
                if (!string.IsNullOrWhiteSpace(options.Jwt) && !string.IsNullOrWhiteSpace(options.Seed))
                    return new NatsAuthOpts { Jwt = options.Jwt!.Trim(), Seed = options.Seed!.Trim() };

                var creds = ResolvePath(options.CredsFile);
                if (string.IsNullOrWhiteSpace(creds) || !File.Exists(creds))
                    throw new FileNotFoundException($"Không tìm thấy file creds NATS: {creds}");

                return new NatsAuthOpts { CredsFile = creds };

            case "NKEY":
                if (string.IsNullOrWhiteSpace(options.Seed))
                    throw new InvalidOperationException("NKEY auth cần Seed.");
                return new NatsAuthOpts { NKey = options.NKey?.Trim(), Seed = options.Seed!.Trim() };

            case "TOKEN":
                if (string.IsNullOrWhiteSpace(options.Token))
                    throw new InvalidOperationException("TOKEN auth cần Token.");
                return new NatsAuthOpts { Token = options.Token!.Trim() };

            case "USERPASS":
                if (string.IsNullOrWhiteSpace(options.Username))
                    throw new InvalidOperationException("USERPASS auth cần Username.");
                return new NatsAuthOpts { Username = options.Username!.Trim(), Password = options.Password };

            default:
                return NatsAuthOpts.Default;
        }
    }

    public static string? ResolvePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        return Path.IsPathRooted(path) ? path : Path.Combine(AppContext.BaseDirectory, path);
    }
}
