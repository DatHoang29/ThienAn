using System;
using System.Net.Http;
using MediatR;

namespace Module.VideoWall.WPF.Api.Direct;

/// <summary>
/// Description: Factory khởi tạo các client kết nối trực tiếp thiết bị VideoWall (ISAPI, Connection, Orchestrator)
/// Created date: 13/09/2026
/// </summary>
public static class VwDirectClientFactory
{
    public static VwDirectISAPIClient CreateISAPIClient(VwDirectDeviceCredentials creds)
    {
        var primaryHandler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromSeconds(15),
            PooledConnectionIdleTimeout = TimeSpan.FromSeconds(5),
            ConnectTimeout = TimeSpan.FromSeconds(10),
            EnableMultipleHttp2Connections = false,
            SslOptions = new System.Net.Security.SslClientAuthenticationOptions
            {
                RemoteCertificateValidationCallback = delegate { return true; }
            }
        };

        var digestHandler = new VwDirectDigestHandler
        {
            InnerHandler = primaryHandler
        };

        var ip = string.IsNullOrWhiteSpace(creds.Ip) ? "127.0.0.1" : creds.Ip.Trim();
        var port = creds.Port <= 0 ? 80 : creds.Port;

        var connectHost = IsLocalHost(ip) ? "127.0.0.1" : ip;

        var httpClient = new HttpClient(digestHandler)
        {
            BaseAddress = new Uri($"http://{connectHost}:{port}"),
            Timeout = TimeSpan.FromSeconds(30),
        };

        return new VwDirectISAPIClient(httpClient, creds);
    }

    public static VwDirectDeviceConnectionClient CreateConnectionClient(
        VwDirectDeviceCredentials creds,
        IPublisher publisher)
    {
        var isApiClient = CreateISAPIClient(creds);
        return new VwDirectDeviceConnectionClient(isApiClient, publisher);
    }

    public static VwDirectSetupSceneOrchestrator CreateSetupSceneOrchestrator(VwDirectDeviceCredentials creds)
    {
        var isApiClient = CreateISAPIClient(creds);
        return new VwDirectSetupSceneOrchestrator(isApiClient);
    }

    public static bool IsLocalHost(string? host)
    {
        if (string.IsNullOrWhiteSpace(host))
            return true;

        var trimmed = host.Trim();
        if (trimmed.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            trimmed == "127.0.0.1" ||
            trimmed == "::1" ||
            trimmed == "0.0.0.0")
        {
            return true;
        }

        try
        {
            if (System.Net.IPAddress.TryParse(trimmed, out var ip))
            {
                if (System.Net.IPAddress.IsLoopback(ip))
                    return true;

                var hostAddresses = System.Net.Dns.GetHostAddresses(System.Net.Dns.GetHostName());
                foreach (var localIp in hostAddresses)
                {
                    if (localIp.Equals(ip))
                        return true;
                }
            }
            else if (trimmed.Equals(System.Net.Dns.GetHostName(), StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        catch
        {
        }

        return false;
    }
}
