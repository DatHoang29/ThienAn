using System.Net.NetworkInformation;
using System.Net.Sockets;
using Modules.Integration.Barrier.Base.Constants;

namespace Modules.Integration.Barrier.Base.Helpers;

/// <summary>
/// Kiểm tra mạng tới barrier. Thay NetworkHelper.fn_PingHost của CLVH.
/// Đặt ở Base (không dùng BaseHelper.NetworkHp của Shared.Utility vì kéo Furion) — xem IntgVmsNetHelper.
/// Không ném exception: mọi lỗi đều trả false.
/// Author: ChuongLP
/// Date: 05-10-2026
/// </summary>
public static class IntgBarrierNetHelper
{
    /// <summary>Ping ICMP — đúng cách CLVH kiểm tra trước khi điều khiển.</summary>
    public static async Task<bool> PingAsync(string? ip, int timeoutMs = IntgBarrierConstant.DefaultPingTimeoutMs)
    {
        if (string.IsNullOrWhiteSpace(ip)) return false;
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(ip.Trim(), timeoutMs).ConfigureAwait(false);
            return reply.Status == IPStatus.Success;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Thử mở TCP tới cổng (dùng cho check-connect khi mạng chặn ICMP).</summary>
    public static async Task<bool> CheckTcpAsync(string? ip, int port, int timeoutMs = IntgBarrierConstant.DefaultPingTimeoutMs,
                                                 CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(ip) || port <= 0) return false;
        try
        {
            using var client = new TcpClient();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeoutMs);
            await client.ConnectAsync(ip.Trim(), port, cts.Token).ConfigureAwait(false);
            return client.Connected;
        }
        catch
        {
            return false;
        }
    }
}
