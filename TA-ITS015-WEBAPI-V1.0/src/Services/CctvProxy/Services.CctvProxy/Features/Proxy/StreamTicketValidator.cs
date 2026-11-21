using Microsoft.AspNetCore.Http;
using Services.CctvProxy.Configuration;
using Services.Shared.Logging;
using Shared.Utility.Helpers;

namespace Services.CctvProxy.Features.Proxy;

/// <summary>
/// Author: kien.le
/// Description: Kiểm tra ticket (do WebAPI cấp sau khi đã xác thực JWT người dùng) trên request vào proxy:
/// chữ ký, hạn dùng, DeviceId khớp route, Kind khớp loại endpoint.
/// Created date: 06/10/2026
/// </summary>
public sealed class StreamTicketValidator(IConfigManager configManager)
{
    /// <summary>
    /// Kiểm tra ticket trên query <c>ticket</c> của request; sai thì ghi log cảnh báo kèm IP người gọi.
    /// </summary>
    /// <param name="context">Request vào proxy.</param>
    /// <param name="deviceId">DeviceId trên route — phải trùng DeviceId trong ticket.</param>
    /// <param name="kind">Loại endpoint (webrtc/media/image) — phải trùng Kind trong ticket.</param>
    /// <param name="error">Lý do từ chối (null nếu hợp lệ).</param>
    /// <returns>true nếu ticket hợp lệ.</returns>
    public bool TryValidate(HttpContext context, string deviceId, string kind, out string? error)
    {
        var ticket = context.Request.Query[BaseHelper.CctvStreamTicketHp.QueryName].ToString();
        if (!BaseHelper.CctvStreamTicketHp.TryValidate(
                ticket,
                configManager.Current.Cctv.TicketSecret,
                DateTimeOffset.UtcNow,
                out var payload,
                out error))
        {
            LogWarning(context, deviceId, error);
            return false;
        }

        if (!string.Equals(payload!.DeviceId, deviceId, StringComparison.OrdinalIgnoreCase))
        {
            error = "Ticket không dành cho thiết bị này";
            LogWarning(context, deviceId, error);
            return false;
        }

        if (!string.Equals(payload.Kind, kind, StringComparison.OrdinalIgnoreCase))
        {
            error = "Ticket không dành cho loại luồng này";
            LogWarning(context, deviceId, error);
            return false;
        }

        return true;
    }

    private static void LogWarning(HttpContext context, string deviceId, string? error) =>
        LogManager.Log(Microsoft.Extensions.Logging.LogLevel.Warning,
            $"Từ chối {context.Request.Path} (device {deviceId}) từ {context.Connection.RemoteIpAddress}: {error}",
            kind: LogKind.Main);
}
