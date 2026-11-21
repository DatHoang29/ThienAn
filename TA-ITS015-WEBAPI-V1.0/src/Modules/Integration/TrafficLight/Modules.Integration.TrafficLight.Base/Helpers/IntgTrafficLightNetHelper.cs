using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Modules.Integration.TrafficLight.Base.Helpers;

/// <summary>
/// Tiện ích kiểm tra mạng cho Đèn tín hiệu giao thông.
/// Author: ChuongLP
/// Date: 05/10/2026
/// </summary>
public static class IntgTrafficLightNetHelper
{
    public static async Task<bool> PingAsync(string host, int timeoutMs = 2000)
    {
        if (string.IsNullOrWhiteSpace(host)) return false;
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(host.Trim(), timeoutMs).ConfigureAwait(false);
            return reply.Status == IPStatus.Success;
        }
        catch
        {
            return false;
        }
    }

    public static async Task<bool> TestPortAsync(string host, int port, int timeoutMs = 3000)
    {
        if (string.IsNullOrWhiteSpace(host) || port <= 0) return false;
        try
        {
            using var client = new TcpClient();
            using var cts = new CancellationTokenSource(timeoutMs);
            await client.ConnectAsync(host.Trim(), port, cts.Token).ConfigureAwait(false);
            return client.Connected;
        }
        catch
        {
            return false;
        }
    }
}
